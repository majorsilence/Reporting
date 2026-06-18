// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Pdf.Security.Internal
{
    // Standard Security Handler Revision 4 (V=4, R=4, AES-128-CBC).
    // PDF spec §7.6.3 / §7.6.5 (ISO 32000-1:2008).
    internal sealed class PdfEncryption
    {
        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        // Standard 32-byte password padding string (PDF spec Table 20).
        private static readonly byte[] Pad = {
            0x28,0xBF,0x4E,0x5E, 0x4E,0x75,0x8A,0x41,
            0x64,0x00,0x4E,0x56, 0xFF,0xFA,0x01,0x08,
            0x2E,0x2E,0x00,0xB6, 0xD0,0x68,0x3E,0x80,
            0x2F,0x0C,0xA9,0xFE, 0x64,0x53,0x69,0x7A,
        };

        private readonly byte[] _key;   // 16-byte file encryption key

        internal readonly byte[] OEntry;  // 32 bytes — /O in /Encrypt dict
        internal readonly byte[] UEntry;  // 32 bytes — /U in /Encrypt dict
        internal readonly int    P;       // signed int — /P in /Encrypt dict

        private PdfEncryption(byte[] key, byte[] o, byte[] u, int p)
        { _key = key; OEntry = o; UEntry = u; P = p; }

        internal static PdfEncryption Create(PdfSecurity security, byte[] fileId)
        {
            // P: bits 1-2 must be 0; bits 7-8 and 13-32 must be 1 (Table 22).
            int p = unchecked((int)(0xFFFFF0C0u | (uint)security.Permissions));
            byte[] o   = ComputeO(security.OwnerPassword, security.UserPassword);
            byte[] key = ComputeKey(security.UserPassword, o, p, fileId);
            byte[] u   = ComputeU(key, fileId);
            return new PdfEncryption(key, o, u, p);
        }

        // Encrypt a stream; returns 16-byte random IV prepended to ciphertext.
        internal byte[] EncryptStream(int objNum, int genNum, byte[] plaintext) =>
            AesEncryptWithIv(ObjectKey(objNum, genNum), plaintext);

        // Encrypt a PDF string and return a hex literal <...>.
        internal string EncryptPdfString(int objNum, int genNum, byte[] rawBytes)
        {
            byte[] ct = EncryptStream(objNum, genNum, rawBytes);
            var sb = new StringBuilder("<");
            foreach (byte b in ct) sb.Append(b.ToString("X2"));
            sb.Append('>');
            return sb.ToString();
        }

        // Build the /Encrypt dictionary body bytes (never encrypted itself).
        internal byte[] BuildEncryptDict()
        {
            string oHex = ToHex(OEntry);
            string uHex = ToHex(UEntry);
            string dict =
                $"<< /Filter /Standard /V 4 /R 4 /Length 128\n" +
                $"   /O <{oHex}>\n" +
                $"   /U <{uHex}>\n" +
                $"   /P {P}\n" +
                $"   /StmF /StdCF /StrF /StdCF\n" +
                $"   /CF << /StdCF << /AuthEvent /DocOpen /CFM /AESV2 /Length 16 >> >>\n" +
                $">>";
            return Latin1.GetBytes(dict);
        }

        // ── key derivation (PDF spec §7.6.3.3) ──────────────────────────────

        private static byte[] PadPw(string pw)
        {
            byte[] src    = Latin1.GetBytes(pw ?? "");
            var    result = new byte[32];
            int    n      = Math.Min(src.Length, 32);
            Buffer.BlockCopy(src, 0, result, 0, n);
            Buffer.BlockCopy(Pad, 0, result, n, 32 - n);
            return result;
        }

        private static byte[] ComputeO(string ownerPw, string userPw)
        {
            byte[] hash   = Md5(PadPw(ownerPw.Length > 0 ? ownerPw : userPw));
            for (int i = 0; i < 50; i++) hash = Md5(hash, 0, 16);
            byte[] result = Rc4.Crypt(hash, PadPw(userPw));
            for (int i = 1; i <= 19; i++)
            {
                byte[] xk = new byte[16];
                for (int k = 0; k < 16; k++) xk[k] = (byte)(hash[k] ^ i);
                result = Rc4.Crypt(xk, result);
            }
            return result; // 32 bytes
        }

        private static byte[] ComputeKey(string userPw, byte[] oEntry, int p, byte[] fileId)
        {
            using var md5 = MD5.Create();
            md5.TransformBlock(PadPw(userPw), 0, 32, null, 0);
            md5.TransformBlock(oEntry, 0, 32, null, 0);
            byte[] pb = BitConverter.GetBytes(p);
            md5.TransformBlock(pb, 0, 4, null, 0);
            md5.TransformBlock(fileId, 0, fileId.Length, null, 0);
            // Rev 4, EncryptMetadata=true: append 0xFFFFFFFF
            byte[] ffff = { 0xFF, 0xFF, 0xFF, 0xFF };
            md5.TransformFinalBlock(ffff, 0, 4);
            byte[] hash = md5.Hash!;
            for (int i = 0; i < 50; i++) hash = Md5(hash, 0, 16);
            var key = new byte[16];
            Buffer.BlockCopy(hash, 0, key, 0, 16);
            return key;
        }

        private static byte[] ComputeU(byte[] encKey, byte[] fileId)
        {
            byte[] src = new byte[32 + fileId.Length];
            Buffer.BlockCopy(Pad,    0, src, 0,  32);
            Buffer.BlockCopy(fileId, 0, src, 32, fileId.Length);
            byte[] result = Rc4.Crypt(encKey, Md5(src));
            for (int i = 1; i <= 19; i++)
            {
                byte[] xk = new byte[16];
                for (int k = 0; k < 16; k++) xk[k] = (byte)(encKey[k] ^ i);
                result = Rc4.Crypt(xk, result);
            }
            var u = new byte[32];
            Buffer.BlockCopy(result, 0, u, 0, 16);
            return u;
        }

        // Per-object key: MD5(encKey + objNum(3LE) + genNum(2LE) + "sAlT")
        private byte[] ObjectKey(int objNum, int genNum)
        {
            var data = new byte[16 + 3 + 2 + 4];
            Buffer.BlockCopy(_key, 0, data, 0, 16);
            data[16] = (byte)objNum;  data[17] = (byte)(objNum >> 8);  data[18] = (byte)(objNum >> 16);
            data[19] = (byte)genNum;  data[20] = (byte)(genNum >> 8);
            data[21] = 0x73; data[22] = 0x41; data[23] = 0x6C; data[24] = 0x54; // "sAlT"
            byte[] hash = Md5(data);
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

        private static byte[] Md5(byte[] data)
        {
            using var m = MD5.Create();
            return m.ComputeHash(data);
        }

        private static byte[] Md5(byte[] data, int offset, int count)
        {
            using var m = MD5.Create();
            return m.ComputeHash(data, offset, count);
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
