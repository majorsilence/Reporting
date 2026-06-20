// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Majorsilence.Pdf.Security.Internal
{
    // Two-pass PDF digital signature.
    // Pass 1: builds a placeholder dictionary with zeros for ByteRange / Contents.
    // Pass 2: seeks into the MemoryStream, fills ByteRange, signs, fills Contents.
    internal static class PdfSigner
    {
        // Bytes reserved for the DER-encoded PKCS#7 payload inside /Contents <>.
        // 32 KB covers a full certificate chain plus an RFC 3161 timestamp token.
        internal const int PlaceholderBytes = 32768;

        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        private static readonly byte[] BrMarker   = Latin1.GetBytes("[0000000000 ");
        private static readonly byte[] ContMarker = Latin1.GetBytes("/Contents <");

        // SHA-256 OID (2.16.840.1.101.3.4.2.1) DER-encoded for TSA requests.
        private static readonly byte[] Sha256OidDer =
            { 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01 };

        // id-aa-signatureTimeStampToken OID: 1.2.840.113549.1.9.16.2.14
        private static readonly Oid TsaUnsignedAttrOid =
            new Oid("1.2.840.113549.1.9.16.2.14");

        internal static byte[] BuildPlaceholder(PdfSignatureOptions opts)
        {
            var sb = new StringBuilder();
            sb.Append("<< /Type /Sig /Filter /Adobe.PPKLite /SubFilter /adbe.pkcs7.detached\n");
            sb.Append("   /ByteRange [0000000000 0000000000 0000000000 0000000000]\n");
            sb.Append($"   /Contents <{new string('0', PlaceholderBytes * 2)}>\n");
            if (!string.IsNullOrEmpty(opts.SignerName))
                sb.Append($"   /Name ({EscapePdf(opts.SignerName!)})\n");
            if (!string.IsNullOrEmpty(opts.Reason))
                sb.Append($"   /Reason ({EscapePdf(opts.Reason!)})\n");
            if (!string.IsNullOrEmpty(opts.Location))
                sb.Append($"   /Location ({EscapePdf(opts.Location!)})\n");
            sb.Append($"   /M (D:{DateTime.UtcNow:yyyyMMddHHmmss}Z)\n");
            sb.Append(">>");
            return Latin1.GetBytes(sb.ToString());
        }

        internal static void Fixup(
            MemoryStream ms,
            byte[]       sigPlaceholderBytes,
            long         sigBodyOffset,
            PdfSignatureOptions opts)
        {
            long totalLen = ms.Length;

            // Locate /ByteRange field
            int brInObj = IndexOf(sigPlaceholderBytes, BrMarker);
            if (brInObj < 0) throw new InvalidOperationException("ByteRange marker not found in sig object");
            long brPos = sigBodyOffset + brInObj + 1; // +1 to skip '['

            // Locate /Contents hex start
            int contInObj = IndexOf(sigPlaceholderBytes, ContMarker);
            if (contInObj < 0) throw new InvalidOperationException("/Contents marker not found in sig object");
            long contHexPos = sigBodyOffset + contInObj + ContMarker.Length;
            long contStart  = contHexPos - 1;                         // '<'
            long contEnd    = contHexPos + PlaceholderBytes * 2 + 1;  // past '>'

            // ByteRange covers [0..contStart) and [contEnd..totalLen)
            long r0 = 0, r1 = contStart, r2 = contEnd, r3 = totalLen - r2;

            ms.Position = brPos;
            WritePaddedDecimal(ms, r0, 10); ms.WriteByte((byte)' ');
            WritePaddedDecimal(ms, r1, 10); ms.WriteByte((byte)' ');
            WritePaddedDecimal(ms, r2, 10); ms.WriteByte((byte)' ');
            WritePaddedDecimal(ms, r3, 10);

            // Read the two signed byte-ranges
            byte[] range1 = new byte[r1], range2 = new byte[r3];
            ms.Position = 0; ReadAll(ms, range1);
            ms.Position = r2; ReadAll(ms, range2);

            var toSign = new byte[r1 + r3];
            Buffer.BlockCopy(range1, 0, toSign, 0,       (int)r1);
            Buffer.BlockCopy(range2, 0, toSign, (int)r1, (int)r3);

            byte[] sigDer = SignData(toSign, opts);
            if (sigDer.Length > PlaceholderBytes)
                throw new InvalidOperationException(
                    $"Signature ({sigDer.Length} B) exceeds reserved placeholder ({PlaceholderBytes} B).");

            // Write signature hex into /Contents placeholder
            ms.Position = contHexPos;
            foreach (byte b in sigDer)
            {
                ms.WriteByte(HexNibble(b >> 4));
                ms.WriteByte(HexNibble(b & 0xF));
            }
            // Remaining placeholder bytes stay '0' (written during placeholder build)
        }

        // ── PKCS#7 CMS signature ──────────────────────────────────────────────

        private static byte[] SignData(byte[] data, PdfSignatureOptions opts)
        {
            var ci  = new ContentInfo(new Oid("1.2.840.113549.1.7.1"), data);
            var cms = new SignedCms(ci, detached: true);
            var si  = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, opts.Certificate)
            {
                DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
                // ExcludeRoot embeds the signing cert + any intermediate CAs but omits
                // the root CA (relying parties already have it via their trust store).
                IncludeOption   = X509IncludeOption.ExcludeRoot,
            };
            si.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
            cms.ComputeSignature(si, silent: true);

            // Optionally embed an RFC 3161 timestamp so the signature remains
            // verifiable after the signing certificate expires.
            if (!string.IsNullOrEmpty(opts.TimestampAuthorityUrl))
            {
                byte[] cmsBytes    = cms.Encode();
                byte[] sigBytes    = ExtractSignatureValue(cmsBytes);
                byte[] tsaToken    = RequestTimestampToken(sigBytes, opts.TimestampAuthorityUrl!);
                cms.SignerInfos[0].AddUnsignedAttribute(
                    new AsnEncodedData(TsaUnsignedAttrOid, tsaToken));
            }

            return cms.Encode();
        }

        // ── RFC 3161 timestamp ────────────────────────────────────────────────

        private static byte[] RequestTimestampToken(byte[] signatureBytes, string tsaUrl)
        {
            byte[] hash;
            using (var sha = SHA256.Create()) hash = sha.ComputeHash(signatureBytes);

            byte[] req = BuildTsaRequest(hash);

            using var http    = new HttpClient();
            var body          = new ByteArrayContent(req);
            body.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");

            var resp = http.PostAsync(tsaUrl, body).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            byte[] raw = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return ExtractTimestampToken(raw);
        }

        // Builds a minimal DER-encoded RFC 3161 TimeStampReq.
        private static byte[] BuildTsaRequest(byte[] sha256Hash)
        {
            var nonce = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(nonce);

            return DerSeq(
                DerInt(new byte[] { 1 }),                                       // version
                DerSeq(                                                          // messageImprint
                    DerSeq(DerOid(Sha256OidDer), DerNull()),                    //   AlgorithmIdentifier
                    DerOctetStr(sha256Hash)                                      //   hashedMessage
                ),
                DerInt(nonce),                                                   // nonce
                DerBool(true)                                                    // certReq
            );
        }

        // Extracts the timeStampToken (ContentInfo TLV) from a RFC 3161 TimeStampResp.
        private static byte[] ExtractTimestampToken(byte[] resp)
        {
            int p = DerEnter(resp, 0);      // enter TimeStampResp SEQUENCE
            // Verify PKIStatus == 0 (granted) or 1 (grantedWithMods)
            int statusField = DerEnter(resp, p);        // enter PKIStatusInfo SEQUENCE
            byte[] statusValue = DerGetContent(resp, statusField); // status INTEGER value
            int status = statusValue.Length > 0 ? statusValue[0] : -1;
            if (status > 1)
                throw new CryptographicException(
                    $"TSA returned non-success status {status}; timestamp not granted.");
            p = DerSkip(resp, p);           // skip PKIStatusInfo
            return DerGetTLV(resp, p);      // timeStampToken = ContentInfo (full TLV)
        }

        // Navigates a CMS ContentInfo and extracts the raw signature bytes from the
        // first SignerInfo's `signature` OCTET STRING field.
        private static byte[] ExtractSignatureValue(byte[] cms)
        {
            int p = DerEnter(cms, 0);                                 // ContentInfo SEQUENCE
            p = DerSkip(cms, p);                                      // skip id-signedData OID
            p = DerEnter(cms, p);                                     // enter [0] EXPLICIT
            p = DerEnter(cms, p);                                     // SignedData SEQUENCE
            p = DerSkip(cms, p);                                      // skip version INTEGER
            p = DerSkip(cms, p);                                      // skip digestAlgorithms SET
            p = DerSkip(cms, p);                                      // skip encapContentInfo SEQUENCE
            if (cms[p] == 0xA0) p = DerSkip(cms, p);                 // skip optional [0] certificates
            if (cms[p] == 0xA1) p = DerSkip(cms, p);                 // skip optional [1] crls
            p = DerEnter(cms, p);                                     // signerInfos SET
            p = DerEnter(cms, p);                                     // first SignerInfo SEQUENCE
            p = DerSkip(cms, p);                                      // skip version INTEGER
            p = DerSkip(cms, p);                                      // skip sid
            p = DerSkip(cms, p);                                      // skip digestAlgorithm
            if (cms[p] == 0xA0) p = DerSkip(cms, p);                 // skip optional signedAttrs [0]
            p = DerSkip(cms, p);                                      // skip signatureAlgorithm
            return DerGetContent(cms, p);                             // signature OCTET STRING value
        }

        // ── minimal DER read helpers ──────────────────────────────────────────

        // Returns (contentStart, contentEnd) for the TLV at pos (does not consume tag).
        private static (int start, int end) DerTLContent(byte[] d, int pos)
        {
            pos++; // skip tag byte
            int len;
            if ((d[pos] & 0x80) == 0)
            {
                len = d[pos++];
            }
            else
            {
                int n = d[pos++] & 0x7F;
                len = 0;
                for (int i = 0; i < n; i++) len = (len << 8) | d[pos++];
            }
            return (pos, pos + len);
        }

        // Advance past the TLV at pos.
        private static int DerSkip(byte[] d, int pos) { var (_, end) = DerTLContent(d, pos); return end; }

        // Enter a constructed TLV — returns offset of its first child element.
        private static int DerEnter(byte[] d, int pos) => DerTLContent(d, pos).start;

        // Return a copy of the TLV's value bytes only.
        private static byte[] DerGetContent(byte[] d, int pos)
        {
            var (start, end) = DerTLContent(d, pos);
            var buf = new byte[end - start];
            Buffer.BlockCopy(d, start, buf, 0, buf.Length);
            return buf;
        }

        // Return the complete TLV bytes (tag + length + value).
        private static byte[] DerGetTLV(byte[] d, int pos)
        {
            var (_, end) = DerTLContent(d, pos);
            var buf = new byte[end - pos];
            Buffer.BlockCopy(d, pos, buf, 0, buf.Length);
            return buf;
        }

        // ── minimal DER write helpers ─────────────────────────────────────────

        private static byte[] DerTLV(byte tag, byte[] value)
        {
            int len = value.Length;
            byte[] hdr = len < 128 ? new[] { tag, (byte)len }
                       : len < 256 ? new[] { tag, (byte)0x81, (byte)len }
                       :             new[] { tag, (byte)0x82, (byte)(len >> 8), (byte)len };
            var result = new byte[hdr.Length + len];
            Buffer.BlockCopy(hdr,   0, result, 0,          hdr.Length);
            Buffer.BlockCopy(value, 0, result, hdr.Length, len);
            return result;
        }

        private static byte[] DerSeq(params byte[][] items)
        {
            int total = 0; foreach (var x in items) total += x.Length;
            var body = new byte[total]; int pos = 0;
            foreach (var x in items) { Buffer.BlockCopy(x, 0, body, pos, x.Length); pos += x.Length; }
            return DerTLV(0x30, body);
        }

        private static byte[] DerInt(byte[] value)
        {
            // Prepend 0x00 if the high bit would set the sign bit incorrectly.
            if ((value[0] & 0x80) != 0)
            {
                var padded = new byte[value.Length + 1];
                Buffer.BlockCopy(value, 0, padded, 1, value.Length);
                return DerTLV(0x02, padded);
            }
            return DerTLV(0x02, value);
        }

        private static byte[] DerOid(byte[] encodedOid) => DerTLV(0x06, encodedOid);
        private static byte[] DerOctetStr(byte[] value)  => DerTLV(0x04, value);
        private static byte[] DerNull()                   => new byte[] { 0x05, 0x00 };
        private static byte[] DerBool(bool v)             => new byte[] { 0x01, 0x01, v ? (byte)0xFF : (byte)0x00 };

        // ── stream / byte helpers ─────────────────────────────────────────────

        private static void WritePaddedDecimal(Stream s, long value, int width)
        {
            string str = value.ToString();
            for (int i = str.Length; i < width; i++) s.WriteByte((byte)'0');
            foreach (char c in str) s.WriteByte((byte)c);
        }

        private static byte HexNibble(int n) => (byte)(n < 10 ? '0' + n : 'a' + n - 10);

        private static void ReadAll(Stream s, byte[] buf)
        {
            int offset = 0;
            while (offset < buf.Length)
            {
                int n = s.Read(buf, offset, buf.Length - offset);
                if (n == 0) break;
                offset += n;
            }
        }

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            int limit = haystack.Length - needle.Length;
            for (int i = 0; i <= limit; i++)
            {
                bool ok = true;
                for (int j = 0; j < needle.Length; j++)
                    if (haystack[i + j] != needle[j]) { ok = false; break; }
                if (ok) return i;
            }
            return -1;
        }

        private static string EscapePdf(string s) =>
            s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
