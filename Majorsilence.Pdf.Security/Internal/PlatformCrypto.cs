// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Majorsilence.Pdf.Security.Internal
{
    /// <summary>
    /// MD5 and AES for the encryption handlers. Uses the platform implementations everywhere
    /// except where they are missing (browser-wasm), where the managed ones are used instead.
    /// SHA-2 and the random number generator work in the browser and are used directly.
    /// </summary>
#pragma warning disable CA1416 // MD5 and Aes are only reached when UseManaged is false, i.e. not in the browser
    internal static class PlatformCrypto
    {
        internal static readonly bool IsBrowser = RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));

        /// <summary>Tests set this to run the managed path on a platform that has the real thing.</summary>
        internal static bool ForceManaged = false;

        private static bool UseManaged => IsBrowser || ForceManaged;

        internal static byte[] Md5(byte[] data) => Md5(data, 0, data.Length);

        internal static byte[] Md5(byte[] data, int offset, int count)
        {
            if (UseManaged) return ManagedMd5.ComputeHash(data, offset, count);
            using var m = MD5.Create();
            return m.ComputeHash(data, offset, count);
        }

        internal static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] data, bool pkcs7)
        {
            if (UseManaged) return ManagedAes.EncryptCbc(key, iv, data, pkcs7);
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC;
            aes.Padding = pkcs7 ? PaddingMode.PKCS7 : PaddingMode.None;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>AES-CBC with PKCS7 padding and a random IV prepended to the ciphertext.</summary>
        internal static byte[] AesCbcEncryptWithRandomIv(byte[] key, byte[] plaintext)
        {
            var iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);
            byte[] ct = AesCbcEncrypt(key, iv, plaintext, pkcs7: true);
            var result = new byte[16 + ct.Length];
            Buffer.BlockCopy(iv, 0, result, 0, 16);
            Buffer.BlockCopy(ct, 0, result, 16, ct.Length);
            return result;
        }

        internal static byte[] AesEcbEncrypt(byte[] key, byte[] data)
        {
            if (UseManaged) return ManagedAes.EncryptEcb(key, data);
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(data, 0, data.Length);
        }
    }
}
