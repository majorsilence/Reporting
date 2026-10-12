// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Ambient state for one pagination/draw pass: the page being drawn (for page-number
    /// placeholders) and whether split rules such as orphan/widow control are relaxed.
    /// Thread-static so elements stay immutable and the engine stays reentrant per thread.
    /// </summary>
    internal static class LayoutContext
    {
        [ThreadStatic] private static int _pageNumber;
        [ThreadStatic] private static int _totalPages;
        [ThreadStatic] private static bool _relaxSplitting;

        public static int PageNumber => _pageNumber;
        public static int TotalPages => _totalPages;

        /// <summary>When true, orphan/widow control is off (used to place content on an otherwise empty page).</summary>
        public static bool RelaxSplitting => _relaxSplitting;

        public static void SetPage(int pageNumber, int totalPages)
        {
            _pageNumber = pageNumber;
            _totalPages = totalPages;
        }

        public static void ClearPage() => SetPage(0, 0);

        public static IDisposable Relax() => new RelaxScope();

        private sealed class RelaxScope : IDisposable
        {
            private readonly bool _previous;
            public RelaxScope() { _previous = _relaxSplitting; _relaxSplitting = true; }
            public void Dispose() => _relaxSplitting = _previous;
        }
    }
}
