// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// A rectangle in PDF points (top-left origin, matching <see cref="PdfCanvas"/>'s coordinate
    /// system) describing the region reserved for a <see cref="PdfLayout.Canvas"/> escape hatch.
    /// </summary>
    public readonly struct LayoutRect
    {
        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }

        public LayoutRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// A column width specification passed to <see cref="LayoutRow.Column(LayoutColumnWidth, Action{LayoutColumn})"/>.
    /// Create one with <see cref="LayoutColumn.Fixed"/>; use a plain <c>float</c> fraction
    /// (0–1) with the other <see cref="LayoutRow.Column(float, Action{LayoutColumn})"/> overload
    /// for width relative to the row's total width.
    /// </summary>
    public readonly struct LayoutColumnWidth
    {
        internal float Value { get; }
        internal bool IsFraction { get; }

        internal LayoutColumnWidth(float value, bool isFraction)
        {
            Value = value;
            IsFraction = isFraction;
        }
    }
}
