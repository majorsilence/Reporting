// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Security.Internal
{
    /// <summary>
    /// Pure managed MD5 (RFC 1321). The browser-wasm runtime has no MD5 and throws from
    /// <c>MD5.Create()</c>, but the PDF standard security handler needs it for key derivation.
    /// MD5 is only used here as the algorithm the PDF specification mandates, not for anything
    /// that needs collision resistance.
    /// </summary>
    internal static class ManagedMd5
    {
        private static readonly int[] S =
        {
            7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
            5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20,
            4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
            6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21,
        };

        private static readonly uint[] K = BuildK();

        private static uint[] BuildK()
        {
            var k = new uint[64];
            for (int i = 0; i < 64; i++)
                k[i] = (uint)(long)Math.Floor(Math.Abs(Math.Sin(i + 1)) * 4294967296.0);
            return k;
        }

        internal static byte[] ComputeHash(byte[] data, int offset, int count)
        {
            uint a0 = 0x67452301, b0 = 0xefcdab89, c0 = 0x98badcfe, d0 = 0x10325476;

            int padded = ((count + 8) / 64 + 1) * 64;
            var msg = new byte[padded];
            Buffer.BlockCopy(data, offset, msg, 0, count);
            msg[count] = 0x80;
            ulong bits = (ulong)count * 8;
            for (int i = 0; i < 8; i++) msg[padded - 8 + i] = (byte)(bits >> (8 * i));

            var m = new uint[16];
            for (int chunk = 0; chunk < padded; chunk += 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    int p = chunk + i * 4;
                    m[i] = (uint)(msg[p] | msg[p + 1] << 8 | msg[p + 2] << 16 | msg[p + 3] << 24);
                }

                uint a = a0, b = b0, c = c0, d = d0;
                for (int i = 0; i < 64; i++)
                {
                    uint f; int g;
                    if (i < 16)      { f = (b & c) | (~b & d); g = i; }
                    else if (i < 32) { f = (d & b) | (~d & c); g = (5 * i + 1) % 16; }
                    else if (i < 48) { f = b ^ c ^ d;          g = (3 * i + 5) % 16; }
                    else             { f = c ^ (b | ~d);       g = (7 * i) % 16; }

                    f = f + a + K[i] + m[g];
                    a = d; d = c; c = b;
                    b = b + ((f << S[i]) | (f >> (32 - S[i])));
                }
                a0 += a; b0 += b; c0 += c; d0 += d;
            }

            var digest = new byte[16];
            Write(digest, 0, a0); Write(digest, 4, b0); Write(digest, 8, c0); Write(digest, 12, d0);
            return digest;
        }

        private static void Write(byte[] dst, int at, uint v)
        {
            dst[at] = (byte)v; dst[at + 1] = (byte)(v >> 8); dst[at + 2] = (byte)(v >> 16); dst[at + 3] = (byte)(v >> 24);
        }
    }
}
