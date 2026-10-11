// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Security.Internal
{
    /// <summary>
    /// Pure managed AES (FIPS 197) forward cipher with CBC and ECB modes, for runtimes whose
    /// <c>Aes.Create()</c> throws <see cref="PlatformNotSupportedException"/> (browser-wasm).
    /// Encryption only, which is all PDF writing needs. It is table-driven and not constant-time,
    /// so <see cref="PlatformCrypto"/> only selects it where the platform AES is unavailable.
    /// </summary>
    internal static class ManagedAes
    {
        private static readonly byte[] SBox = BuildSBox();

        private static byte[] BuildSBox()
        {
            var sbox = new byte[256];
            byte p = 1, q = 1;
            do
            {
                // multiply p by 3 in GF(2^8)
                p = (byte)(p ^ (p << 1) ^ ((p & 0x80) != 0 ? 0x1B : 0));
                // divide q by 3
                q ^= (byte)(q << 1);
                q ^= (byte)(q << 2);
                q ^= (byte)(q << 4);
                if ((q & 0x80) != 0) q ^= 0x09;

                int x = q ^ Rotl8(q, 1) ^ Rotl8(q, 2) ^ Rotl8(q, 3) ^ Rotl8(q, 4);
                sbox[p] = (byte)(x ^ 0x63);
            } while (p != 1);
            sbox[0] = 0x63;
            return sbox;
        }

        private static int Rotl8(byte x, int s) => ((x << s) | (x >> (8 - s))) & 0xFF;

        private static byte XTime(byte x) => (byte)((x << 1) ^ ((x & 0x80) != 0 ? 0x1B : 0));

        private static byte[] ExpandKey(byte[] key, out int rounds)
        {
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("AES key must be 16, 24 or 32 bytes.", nameof(key));

            int nk = key.Length / 4;
            rounds = nk + 6;
            var w = new byte[16 * (rounds + 1)];
            Buffer.BlockCopy(key, 0, w, 0, key.Length);

            byte rcon = 1;
            var t = new byte[4];
            for (int i = nk; i < 4 * (rounds + 1); i++)
            {
                Buffer.BlockCopy(w, (i - 1) * 4, t, 0, 4);
                if (i % nk == 0)
                {
                    byte first = t[0];
                    t[0] = (byte)(SBox[t[1]] ^ rcon);
                    t[1] = SBox[t[2]];
                    t[2] = SBox[t[3]];
                    t[3] = SBox[first];
                    rcon = XTime(rcon);
                }
                else if (nk > 6 && i % nk == 4)
                {
                    for (int j = 0; j < 4; j++) t[j] = SBox[t[j]];
                }
                for (int j = 0; j < 4; j++) w[i * 4 + j] = (byte)(w[(i - nk) * 4 + j] ^ t[j]);
            }
            return w;
        }

        private static void EncryptBlock(byte[] w, int rounds, byte[] state)
        {
            AddRoundKey(state, w, 0);
            for (int round = 1; round <= rounds; round++)
            {
                for (int i = 0; i < 16; i++) state[i] = SBox[state[i]];
                ShiftRows(state);
                if (round != rounds) MixColumns(state);
                AddRoundKey(state, w, round * 16);
            }
        }

        private static void AddRoundKey(byte[] s, byte[] w, int at)
        {
            for (int i = 0; i < 16; i++) s[i] ^= w[at + i];
        }

        private static void ShiftRows(byte[] s)
        {
            byte t;
            t = s[1]; s[1] = s[5]; s[5] = s[9]; s[9] = s[13]; s[13] = t;
            t = s[2]; s[2] = s[10]; s[10] = t; t = s[6]; s[6] = s[14]; s[14] = t;
            t = s[15]; s[15] = s[11]; s[11] = s[7]; s[7] = s[3]; s[3] = t;
        }

        private static void MixColumns(byte[] s)
        {
            for (int c = 0; c < 16; c += 4)
            {
                byte a0 = s[c], a1 = s[c + 1], a2 = s[c + 2], a3 = s[c + 3];
                byte all = (byte)(a0 ^ a1 ^ a2 ^ a3);
                s[c]     = (byte)(a0 ^ all ^ XTime((byte)(a0 ^ a1)));
                s[c + 1] = (byte)(a1 ^ all ^ XTime((byte)(a1 ^ a2)));
                s[c + 2] = (byte)(a2 ^ all ^ XTime((byte)(a2 ^ a3)));
                s[c + 3] = (byte)(a3 ^ all ^ XTime((byte)(a3 ^ a0)));
            }
        }

        /// <summary>AES-CBC encrypt. With <paramref name="pkcs7"/> false the input must be block-aligned.</summary>
        internal static byte[] EncryptCbc(byte[] key, byte[] iv, byte[] data, bool pkcs7)
        {
            if (iv.Length != 16) throw new ArgumentException("IV must be 16 bytes.", nameof(iv));
            byte[] input = Pad(data, pkcs7);
            byte[] w = ExpandKey(key, out int rounds);
            var output = new byte[input.Length];
            var state = new byte[16];
            var prev = (byte[])iv.Clone();
            for (int off = 0; off < input.Length; off += 16)
            {
                for (int i = 0; i < 16; i++) state[i] = (byte)(input[off + i] ^ prev[i]);
                EncryptBlock(w, rounds, state);
                Buffer.BlockCopy(state, 0, output, off, 16);
                Buffer.BlockCopy(state, 0, prev, 0, 16);
            }
            return output;
        }

        /// <summary>AES-ECB encrypt without padding; the input must be block-aligned.</summary>
        internal static byte[] EncryptEcb(byte[] key, byte[] data)
        {
            byte[] input = Pad(data, false);
            byte[] w = ExpandKey(key, out int rounds);
            var output = new byte[input.Length];
            var state = new byte[16];
            for (int off = 0; off < input.Length; off += 16)
            {
                Buffer.BlockCopy(input, off, state, 0, 16);
                EncryptBlock(w, rounds, state);
                Buffer.BlockCopy(state, 0, output, off, 16);
            }
            return output;
        }

        private static byte[] Pad(byte[] data, bool pkcs7)
        {
            if (!pkcs7)
            {
                if (data.Length % 16 != 0)
                    throw new ArgumentException("Data length must be a multiple of 16 when padding is disabled.", nameof(data));
                return data;
            }
            int pad = 16 - data.Length % 16;
            var padded = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, padded, 0, data.Length);
            for (int i = data.Length; i < padded.Length; i++) padded[i] = (byte)pad;
            return padded;
        }
    }
}
