// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.IO;
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
        internal const int PlaceholderBytes = 16384;

        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        private static readonly byte[] BrMarker   = Latin1.GetBytes("[0000000000 ");
        private static readonly byte[] ContMarker = Latin1.GetBytes("/Contents <");

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
            long contHexPos  = sigBodyOffset + contInObj + ContMarker.Length;
            long contStart   = contHexPos - 1;                         // '<'
            long contEnd     = contHexPos + PlaceholderBytes * 2 + 1;  // past '>'

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

            byte[] sigDer = SignData(toSign, opts.Certificate);
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

        private static byte[] SignData(byte[] data, X509Certificate2 cert)
        {
            var ci  = new ContentInfo(new Oid("1.2.840.113549.1.7.1"), data);
            var cms = new SignedCms(ci, detached: true);
            var si  = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert)
            {
                DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
                IncludeOption   = X509IncludeOption.EndCertOnly,
            };
            si.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
            cms.ComputeSignature(si, silent: true);
            return cms.Encode();
        }

        // ── helpers ───────────────────────────────────────────────────────────

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
