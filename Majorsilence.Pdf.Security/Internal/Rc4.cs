// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Security.Internal
{
    // RC4 stream cipher — used only for computing O and U entries in the
    // Standard Security Handler (PDF spec §7.6.3).
    internal static class Rc4
    {
        internal static byte[] Crypt(byte[] key, byte[] data)
        {
            var s = new byte[256];
            for (int i = 0; i < 256; i++) s[i] = (byte)i;
            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + s[i] + key[i % key.Length]) & 255;
                byte tmp = s[i]; s[i] = s[j]; s[j] = tmp;
            }
            var result = new byte[data.Length];
            int a = 0, b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + 1) & 255;
                b = (b + s[a]) & 255;
                byte tmp = s[a]; s[a] = s[b]; s[b] = tmp;
                result[i] = (byte)(data[i] ^ s[(s[a] + s[b]) & 255]);
            }
            return result;
        }
    }
}
