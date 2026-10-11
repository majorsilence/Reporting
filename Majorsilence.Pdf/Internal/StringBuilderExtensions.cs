// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Globalization;
using System.Text;

namespace Majorsilence.Pdf.Internal
{
    internal static class StringBuilderExtensions
    {
        /// <summary>
        /// Appends a number the way <see cref="PdfSerializer.Fmt"/> formats it ("0.###", invariant),
        /// without making a string for it on .NET 6 and later.
        /// </summary>
        internal static StringBuilder AppendNum(this StringBuilder sb, float v)
        {
#if NET6_0_OR_GREATER
            System.Span<char> buf = stackalloc char[32];
            if (v.TryFormat(buf, out int written, "0.###", CultureInfo.InvariantCulture))
                return sb.Append(buf.Slice(0, written));
#endif
            return sb.Append(PdfSerializer.Fmt(v));
        }
    }
}
