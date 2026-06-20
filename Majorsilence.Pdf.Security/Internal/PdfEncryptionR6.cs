// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Pdf.Security.Internal
{
    // Standard Security Handler Revision 6 (V=5, R=6, AES-256-CBC).
    // ISO 32000-2:2020 §7.6.4 (PDF 2.0).
    internal sealed class PdfEncryptionR6
    {
        private static readonly Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");
        private static readonly byte[] ZeroIV   = new byte[16];

        private readonly byte[] _fek;    // 32-byte file encryption key
        private readonly byte[] _O;      // 48 bytes → /O
        private readonly byte[] _OE;     // 32 bytes → /OE
        private readonly byte[] _U;      // 48 bytes → /U
        private readonly byte[] _UE;     // 32 bytes → /UE
        private readonly byte[] _Perms;  // 16 bytes → /Perms
        private readonly int    _P;

        private PdfEncryptionR6(byte[] fek, byte[] o, byte[] oe, byte[] u, byte[] ue, byte[] perms, int p)
        { _fek = fek; _O = o; _OE = oe; _U = u; _UE = ue; _Perms = perms; _P = p; }

        internal static PdfEncryptionR6 Create(PdfSecurity security)
        {
            // P: bits 1-2 must be 0; bits 7-8 and 13-32 must be 1 (Table 22).
            int p = unchecked((int)(0xFFFFF0C0u | (uint)security.Permissions));

            byte[] userPw  = PreparePassword(security.UserPassword);
            byte[] ownerPw = PreparePassword(security.OwnerPassword);

            // 32-byte random file encryption key
            byte[] fek = GetRandomBytes(32);

            // /U: 48 bytes = hash(userPw, UVS) ‖ UVS ‖ UKS
            byte[] uvs   = GetRandomBytes(8);
            byte[] uks   = GetRandomBytes(8);
            byte[] uHash = Algorithm2A(userPw, uvs, new byte[0]);
            byte[] U     = Concat3(uHash, uvs, uks);

            // /UE: AES-256-CBC(key=Algorithm2A(userPw, UKS), iv=zeros, FEK) — FEK is 32 bytes, no padding
            byte[] ueKey = Algorithm2A(userPw, uks, new byte[0]);
            byte[] UE    = AesCbc256NoPad(ueKey, ZeroIV, fek);

            // /O: 48 bytes = hash(ownerPw, OVS, /U) ‖ OVS ‖ OKS
            byte[] ovs   = GetRandomBytes(8);
            byte[] oks   = GetRandomBytes(8);
            byte[] oHash = Algorithm2A(ownerPw, ovs, U);
            byte[] O     = Concat3(oHash, ovs, oks);

            // /OE: AES-256-CBC(key=Algorithm2A(ownerPw, OKS, /U), iv=zeros, FEK)
            byte[] oeKey = Algorithm2A(ownerPw, oks, U);
            byte[] OE    = AesCbc256NoPad(oeKey, ZeroIV, fek);

            // /Perms: 16-byte plaintext encrypted with AES-256-ECB using FEK
            byte[] Perms = AesEcb256(fek, BuildPermsPlain(p));

            return new PdfEncryptionR6(fek, O, OE, U, UE, Perms, p);
        }

        // R=6: use FEK directly — no per-object key derivation.
        internal byte[] EncryptStream(int objNum, int genNum, byte[] plaintext) =>
            AesCbc256WithIv(_fek, plaintext);

        internal string EncryptPdfString(int objNum, int genNum, byte[] rawBytes)
        {
            byte[] ct = AesCbc256WithIv(_fek, rawBytes);
            var sb = new StringBuilder("<");
            foreach (byte b in ct) sb.Append(b.ToString("X2"));
            sb.Append('>');
            return sb.ToString();
        }

        internal byte[] BuildEncryptDict()
        {
            string dict =
                $"<< /Filter /Standard /V 5 /R 6 /Length 256\n" +
                $"   /O <{ToHex(_O)}>\n" +
                $"   /OE <{ToHex(_OE)}>\n" +
                $"   /U <{ToHex(_U)}>\n" +
                $"   /UE <{ToHex(_UE)}>\n" +
                $"   /Perms <{ToHex(_Perms)}>\n" +
                $"   /P {_P}\n" +
                $"   /EncryptMetadata true\n" +
                $"   /StmF /StdCF /StrF /StdCF\n" +
                $"   /CF << /StdCF << /AuthEvent /DocOpen /CFM /AESV3 /Length 32 >> >>\n" +
                $">>";
            return Latin1.GetBytes(dict);
        }

        // ── Algorithm 2.A (ISO 32000-2:2020 §7.6.4.3.4) ─────────────────────
        //
        // K1 is always 64×seq.Length bytes.  Since 64 = 4×16, K1 is always an
        // exact AES block multiple — so AES-CBC needs no padding here.
        //
        // Termination: run at least 64 rounds; stop when
        //   (last byte of E as unsigned) + 32 ≤ round.
        // Last byte ranges 0–255 → loop runs 64–287 rounds.

        private static byte[] Algorithm2A(byte[] password, byte[] salt, byte[] userKey)
        {
            byte[] K = Sha256(Concat3(password, salt, userKey));

            byte[] lastE = new byte[1];

            for (int round = 1; ; round++)
            {
                // K1 = (password ‖ K ‖ userKey) repeated 64 times
                byte[] seq = Concat3(password, K, userKey);
                byte[] K1  = Repeat64(seq);

                // E = AES-128-CBC(key=K[0..15], IV=K[16..31], K1)
                // No padding: K1 is always block-aligned (64×seq.Length, 64=4×16).
                byte[] kKey = new byte[16]; Buffer.BlockCopy(K, 0,  kKey, 0, 16);
                byte[] kIv  = new byte[16]; Buffer.BlockCopy(K, 16, kIv,  0, 16);
                byte[] E = AesCbc128NoPad(kKey, kIv, K1);
                lastE = E;

                // r = (first 16 bytes of E treated as big-endian 128-bit int) mod 3.
                // Since 256^k ≡ 1 (mod 3) for all k, this is just sum-of-bytes mod 3.
                int sum = 0;
                for (int i = 0; i < 16; i++) sum += E[i];
                int r = sum % 3;

                if (r == 0)      K = Sha256(K1);
                else if (r == 1) K = Sha384(K1);
                else             K = Sha512(K1);

                if (round >= 64 && ((lastE[lastE.Length - 1] & 0xFF) + 32) <= round)
                    break;
            }

            var result = new byte[32];
            Buffer.BlockCopy(K, 0, result, 0, 32);
            return result;
        }

        // ── password preparation ──────────────────────────────────────────────

        private static byte[] PreparePassword(string pw)
        {
            if (string.IsNullOrEmpty(pw)) return new byte[0];

            // PDF 2.0 §7.6.4.3.3: passwords are UTF-8, max 127 bytes.
            // Apply NFKC normalization — the common-case subset of SASLprep (RFC 4013)
            // that maps ligatures, fullwidth, and composed forms to canonical equivalents.
            string normalized = pw.Normalize(NormalizationForm.FormKC);
            byte[] bytes      = Encoding.UTF8.GetBytes(normalized);
            if (bytes.Length <= 127) return bytes;

            // Scan forward to find the largest prefix ≤ 127 bytes that does not cut
            // a multi-byte UTF-8 sequence mid-character.
            int pos = 0;
            while (pos < bytes.Length)
            {
                byte b = bytes[pos];
                int seqLen = (b & 0x80) == 0x00 ? 1
                           : (b & 0xE0) == 0xC0 ? 2
                           : (b & 0xF0) == 0xE0 ? 3
                           :                       4;
                if (pos + seqLen > 127) break;
                pos += seqLen;
            }

            var trimmed = new byte[pos];
            Buffer.BlockCopy(bytes, 0, trimmed, 0, pos);
            return trimmed;
        }

        // ── /Perms plaintext (ISO 32000-2:2020 §7.6.4.3.5 Algorithm 4) ──────

        private static byte[] BuildPermsPlain(int p)
        {
            var plain = new byte[16];
            // bytes 0–3: P as 32-bit signed little-endian
            plain[0] = (byte)p;         plain[1] = (byte)(p >> 8);
            plain[2] = (byte)(p >> 16); plain[3] = (byte)(p >> 24);
            // bytes 4–7: 0xFF (high 32 bits of 64-bit P, always 1)
            plain[4] = 0xFF; plain[5] = 0xFF; plain[6] = 0xFF; plain[7] = 0xFF;
            // byte 8: 'T' = EncryptMetadata true, 'F' = false
            plain[8]  = (byte)'T';
            // bytes 9–11: literal 'adb'
            plain[9]  = (byte)'a'; plain[10] = (byte)'d'; plain[11] = (byte)'b';
            // bytes 12–15: random
            byte[] rnd = GetRandomBytes(4);
            Buffer.BlockCopy(rnd, 0, plain, 12, 4);
            return plain;
        }

        // ── AES helpers ───────────────────────────────────────────────────────

        // Used in Algorithm 2.A only; K1 is always block-aligned.
        private static byte[] AesCbc128NoPad(byte[] key, byte[] iv, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(data, 0, data.Length);
        }

        // Used for /UE and /OE; FEK is always 32 bytes (2 blocks) — no padding needed.
        private static byte[] AesCbc256NoPad(byte[] key, byte[] iv, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.None;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(data, 0, data.Length);
        }

        // Stream/string encryption: random 16-byte IV prepended to PKCS7-padded ciphertext.
        private static byte[] AesCbc256WithIv(byte[] key, byte[] plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            byte[] iv = aes.IV;
            using var enc = aes.CreateEncryptor();
            byte[] ct = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
            var result = new byte[16 + ct.Length];
            Buffer.BlockCopy(iv, 0, result, 0,  16);
            Buffer.BlockCopy(ct, 0, result, 16, ct.Length);
            return result;
        }

        // Used for /Perms; input is always exactly one 16-byte block.
        private static byte[] AesEcb256(byte[] key, byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = key; aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None;
            using var enc = aes.CreateEncryptor();
            return enc.TransformFinalBlock(data, 0, data.Length);
        }

        // ── crypto primitives ─────────────────────────────────────────────────

        private static byte[] GetRandomBytes(int count)
        {
            var buf = new byte[count];
#if NET6_0_OR_GREATER
            RandomNumberGenerator.Fill(buf);
#else
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(buf);
#endif
            return buf;
        }

        private static byte[] Sha256(byte[] data) { using var h = SHA256.Create(); return h.ComputeHash(data); }
        private static byte[] Sha384(byte[] data) { using var h = SHA384.Create(); return h.ComputeHash(data); }
        private static byte[] Sha512(byte[] data) { using var h = SHA512.Create(); return h.ComputeHash(data); }

        // ── byte array helpers ────────────────────────────────────────────────

        private static byte[] Concat3(byte[] a, byte[] b, byte[] c)
        {
            var r = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, r, a.Length + b.Length, c.Length);
            return r;
        }

        private static byte[] Repeat64(byte[] seq)
        {
            var r = new byte[seq.Length * 64];
            for (int i = 0; i < 64; i++) Buffer.BlockCopy(seq, 0, r, i * seq.Length, seq.Length);
            return r;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
