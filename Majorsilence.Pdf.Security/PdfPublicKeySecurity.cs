// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Internal;

namespace Majorsilence.Pdf.Security
{
    /// <summary>
    /// Certificate-based (public-key) PDF encryption.
    /// Each recipient holds an X.509 certificate; only holders of the corresponding
    /// private keys can open the document (ISO 32000-2:2020 §7.6.5,
    /// <c>/Filter /Adobe.PubSec</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// var cert = new X509Certificate2("recipient.cer");
    /// PdfDocument.Create()
    ///     .WithPublicKeySecurity(PdfPublicKeySecurity.ForRecipients(cert))
    ///     .AddPage(PageSizes.A4, c => c.DrawText("Secret", 72, 100))
    ///     .Save("pubkey-encrypted.pdf");
    /// </code>
    /// </example>
    public sealed class PdfPublicKeySecurity
    {
        /// <summary>Permitted operations when the document is opened. Defaults to <see cref="PdfPermissions.All"/>.</summary>
        public PdfPermissions Permissions { get; }

        /// <summary>Recipient certificates whose holders can decrypt the document.</summary>
        public IReadOnlyList<X509Certificate2> Recipients { get; }

        private PdfPublicKeySecurity(PdfPermissions perms, IReadOnlyList<X509Certificate2> recipients)
        {
            Permissions = perms;
            Recipients  = recipients;
        }

        /// <summary>
        /// Create a descriptor that allows all listed certificate holders to open the document.
        /// </summary>
        public static PdfPublicKeySecurity ForRecipients(params X509Certificate2[] recipients)
        {
            if (recipients == null || recipients.Length == 0)
                throw new ArgumentException("At least one recipient certificate is required.", nameof(recipients));
            return new PdfPublicKeySecurity(PdfPermissions.All, recipients);
        }

        /// <summary>Return a copy with the specified permissions.</summary>
        public PdfPublicKeySecurity WithPermissions(PdfPermissions permissions)
            => new PdfPublicKeySecurity(permissions, Recipients);
    }

    // ── extension method ──────────────────────────────────────────────────────

    public static partial class PdfDocumentSecurityExtensions
    {
        /// <summary>
        /// Encrypt the document so that only holders of the specified X.509 certificates
        /// can open it (<c>/Filter /Adobe.PubSec</c>, V=4 / AES-128).
        /// Pass <c>null</c> to remove any previously configured encryption.
        /// </summary>
        public static PdfDocument WithPublicKeySecurity(
            this PdfDocument doc, PdfPublicKeySecurity? security)
        {
            doc.SetStreamEncryptor(
                security != null ? new Internal.PubKeyEncryptionProvider(security) : null);
            return doc;
        }
    }
}

// ── internal implementation ───────────────────────────────────────────────────

namespace Majorsilence.Pdf.Security.Internal
{
    // Implements IStreamEncryptor for Adobe.PubSec (V=4, R=4, AES-128-CBC).
    // Key derivation follows PDF spec ISO 32000-1:2008 §7.6.5.
    internal sealed class PubKeyEncryptionProvider : IStreamEncryptor
    {
        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        private readonly PdfPublicKeySecurity _security;

        // Session state set during Initialize()
        private byte[]?   _encKey;      // 16-byte file encryption key
        private byte[][]? _recipients;  // DER-encoded CMS EnvelopedData blobs per recipient

        internal PubKeyEncryptionProvider(PdfPublicKeySecurity security) =>
            _security = security;

        // ── IStreamEncryptor ──────────────────────────────────────────────────

        public void Initialize(byte[] fileId)
        {
            // 1. Generate a 20-byte random seed.
            byte[] seed = new byte[20];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(seed);

            // 2. /P value: bits 1-2 are 0; bits 7-8 and 13-32 are 1 (same mask as Standard).
            int p = unchecked((int)(0xFFFFF0C0u | (uint)_security.Permissions));

            // 3. Build the 24-byte plaintext for each recipient CMS blob: P (4 LE) + seed.
            byte[] pBytes    = { (byte)p, (byte)(p >> 8), (byte)(p >> 16), (byte)(p >> 24) };
            byte[] plaintext = new byte[24];
            Buffer.BlockCopy(pBytes, 0, plaintext, 0, 4);
            Buffer.BlockCopy(seed,   0, plaintext, 4, 20);

            // 4. Encrypt the plaintext blob to each recipient using CMS EnvelopedData.
            _recipients = new byte[_security.Recipients.Count][];
            for (int i = 0; i < _security.Recipients.Count; i++)
            {
                var ci    = new ContentInfo(new Oid("1.2.840.113549.1.7.1"), plaintext);
                var env   = new EnvelopedCms(ci);
                var recip = new CmsRecipientCollection(
                    new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber,
                                     _security.Recipients[i]));
                env.Encrypt(recip);
                _recipients[i] = env.Encode();
            }

            // 5. Derive the 16-byte file encryption key.
            //    FEK = first 16 bytes of SHA-1(seed || all_recipient_blobs_concatenated).
            //    Per ISO 32000-1 §7.6.5.3 Algorithm 24.
            using var sha = SHA1.Create();
            sha.TransformBlock(seed, 0, seed.Length, null, 0);
            for (int i = 0; i < _recipients!.Length; i++)
                sha.TransformBlock(_recipients[i], 0, _recipients[i].Length, null, 0);
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] digest = sha.Hash!;

            _encKey = new byte[16];
            Buffer.BlockCopy(digest, 0, _encKey, 0, 16);
        }

        public byte[] EncryptStream(int objNum, int genNum, byte[] data) =>
            AesEncryptWithIv(ObjectKey(objNum, genNum), data);

        public string EncryptPdfString(int objNum, int genNum, byte[] rawBytes)
        {
            byte[] ct = AesEncryptWithIv(ObjectKey(objNum, genNum), rawBytes);
            var sb = new StringBuilder("<");
            foreach (byte b in ct) sb.Append(b.ToString("X2"));
            sb.Append('>');
            return sb.ToString();
        }

        public byte[] BuildEncryptDict()
        {
            if (_recipients == null || _encKey == null)
                throw new InvalidOperationException("Initialize() must be called before BuildEncryptDict().");

            // Build /Recipients array: each entry is a hex-encoded DER blob.
            var recipArray = new StringBuilder("[");
            foreach (byte[] r in _recipients)
            {
                recipArray.Append('<');
                foreach (byte b in r) recipArray.Append(b.ToString("X2"));
                recipArray.Append('>');
            }
            recipArray.Append(']');

            int p = unchecked((int)(0xFFFFF0C0u | (uint)_security.Permissions));

            string dict =
                $"<< /Filter /Adobe.PubSec /V 4 /R 4 /Length 128\n" +
                $"   /P {p}\n" +
                $"   /Recipients {recipArray}\n" +
                $"   /StmF /DefaultCryptFilter /StrF /DefaultCryptFilter\n" +
                $"   /CF << /DefaultCryptFilter << /AuthEvent /DocOpen /CFM /AESV2 /Length 16 /Recipients {recipArray} >> >>\n" +
                $">>";
            return Latin1.GetBytes(dict);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        // Per-object encryption key: MD5(encKey + objNum(3LE) + genNum(2LE) + "sAlT").
        private byte[] ObjectKey(int objNum, int genNum)
        {
            var data = new byte[16 + 3 + 2 + 4];
            Buffer.BlockCopy(_encKey!, 0, data, 0, 16);
            data[16] = (byte)objNum;  data[17] = (byte)(objNum >> 8);  data[18] = (byte)(objNum >> 16);
            data[19] = (byte)genNum;  data[20] = (byte)(genNum >> 8);
            data[21] = 0x73; data[22] = 0x41; data[23] = 0x6C; data[24] = 0x54; // "sAlT"
            byte[] hash;
            using (var md5 = MD5.Create()) hash = md5.ComputeHash(data);
            var key = new byte[16];
            Buffer.BlockCopy(hash, 0, key, 0, 16);
            return key;
        }

        private static byte[] AesEncryptWithIv(byte[] key, byte[] plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            byte[] iv = aes.IV;
            using var enc = aes.CreateEncryptor();
            byte[] ct = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
            byte[] result = new byte[16 + ct.Length];
            Buffer.BlockCopy(iv, 0, result, 0,  16);
            Buffer.BlockCopy(ct, 0, result, 16, ct.Length);
            return result;
        }
    }
}
