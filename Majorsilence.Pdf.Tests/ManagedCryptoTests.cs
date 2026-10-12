// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Majorsilence.Pdf.Security;
using Majorsilence.Pdf.Security.Internal;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    /// <summary>
    /// The browser-wasm runtime has no MD5 or AES, so Majorsilence.Pdf.Security carries managed
    /// versions. These check them against known vectors and the platform implementations.
    /// </summary>
    [TestFixture]
    [NonParallelizable] // PlatformCrypto.ForceManaged is process-wide
    public class ManagedCryptoTests
    {
        private static string Hex(byte[] b) => string.Concat(b.Select(x => x.ToString("x2")));
        private static byte[] FromHex(string h) =>
            Enumerable.Range(0, h.Length / 2).Select(i => Convert.ToByte(h.Substring(i * 2, 2), 16)).ToArray();

        [TestCase("", "d41d8cd98f00b204e9800998ecf8427e")]
        [TestCase("a", "0cc175b9c0f1b6a831c399e269772661")]
        [TestCase("abc", "900150983cd24fb0d6963f7d28e17f72")]
        [TestCase("message digest", "f96b697d7cb7938d525a2f31aaf161d0")]
        public void ManagedMd5_MatchesRfc1321Vectors(string input, string expected)
        {
            byte[] data = Encoding.ASCII.GetBytes(input);
            Assert.That(Hex(ManagedMd5.ComputeHash(data, 0, data.Length)), Is.EqualTo(expected));
        }

        [Test]
        public void ManagedMd5_MatchesPlatform_AcrossPaddingBoundaries()
        {
            var rng = new Random(1234);
            for (int len = 0; len <= 300; len++)
            {
                var data = new byte[len + 5];
                rng.NextBytes(data);
                using var md5 = MD5.Create();
                Assert.That(ManagedMd5.ComputeHash(data, 3, len), Is.EqualTo(md5.ComputeHash(data, 3, len)), $"length {len}");
            }
        }

        [TestCase("000102030405060708090a0b0c0d0e0f", "69c4e0d86a7b0430d8cdb78070b4c55a")]
        [TestCase("000102030405060708090a0b0c0d0e0f1011121314151617", "dda97ca4864cdfe06eaf70a0ec0d7191")]
        [TestCase("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f", "8ea2b7ca516745bfeafc49904b496089")]
        public void ManagedAes_MatchesFips197Vectors(string key, string expected)
        {
            byte[] plain = FromHex("00112233445566778899aabbccddeeff");
            Assert.That(Hex(ManagedAes.EncryptEcb(FromHex(key), plain)), Is.EqualTo(expected));
        }

        // PDF 2.0 /Perms is one block of AES-256-ECB. The platform path does it as CBC with a zero IV, which must
        // give the FIPS 197 result, as must the managed path.
        [TestCase(false), TestCase(true)]
        public void AesEcbEncrypt_SingleBlock_MatchesFips197(bool forceManaged)
        {
            byte[] key = FromHex("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
            byte[] plain = FromHex("00112233445566778899aabbccddeeff");

            PlatformCrypto.ForceManaged = forceManaged;
            try
            {
                Assert.That(Hex(PlatformCrypto.AesEcbEncrypt(key, plain)), Is.EqualTo("8ea2b7ca516745bfeafc49904b496089"));
                Assert.Throws<ArgumentException>(() => PlatformCrypto.AesEcbEncrypt(key, new byte[32]));
            }
            finally { PlatformCrypto.ForceManaged = false; }
        }

        [TestCase(16, 0), TestCase(16, 1), TestCase(16, 15), TestCase(16, 16), TestCase(16, 100)]
        [TestCase(32, 0), TestCase(32, 17), TestCase(32, 4096)]
        public void ManagedAesCbc_MatchesPlatform(int keyLen, int dataLen)
        {
            var rng = new Random(keyLen * 1000 + dataLen);
            var key = new byte[keyLen]; var iv = new byte[16]; var data = new byte[dataLen];
            rng.NextBytes(key); rng.NextBytes(iv); rng.NextBytes(data);

            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            byte[] expected = aes.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);

            Assert.That(ManagedAes.EncryptCbc(key, iv, data, pkcs7: true), Is.EqualTo(expected));
        }

        [TestCase(PdfEncryptionVersion.AES128)]
        [TestCase(PdfEncryptionVersion.AES256)]
        public void EncryptedPdf_BuiltWithManagedCrypto_OpensWithThePassword(PdfEncryptionVersion version)
        {
            PlatformCrypto.ForceManaged = true;
            try
            {
                byte[] pdf = PdfDocument.Create()
                    .WithSecurity(PdfSecurity.Protect("user123", "owner456").WithEncryptionVersion(version))
                    .AddPage(PageSizes.A4, c => c.DrawText("Managed crypto text", 72, 100, TextStyle.Default.WithSize(14)))
                    .ToBytes();

                using var reader = UglyToad.PdfPig.PdfDocument.Open(pdf,
                    new UglyToad.PdfPig.ParsingOptions { Password = "user123" });
                Assert.That(reader.GetPage(1).Text, Does.Contain("Managed crypto text"));
            }
            finally { PlatformCrypto.ForceManaged = false; }
        }
    }
}
