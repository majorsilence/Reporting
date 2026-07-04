// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Builds the columns of a single row inside a <see cref="PdfLayout.Row"/> call. Columns are
    /// packed left-to-right in the order <see cref="Column(float, Action{LayoutColumn})"/> is
    /// called; there is no gap between them and fractional widths should sum to at most 1.0.
    /// </summary>
    public sealed class LayoutRow
    {
        private readonly PdfCanvas _canvas;
        private readonly float _totalWidth;
        private readonly bool _measureOnly;
        private readonly float _originY;
        private float _cumulativeX;
        private float _maxHeight;

        internal LayoutRow(PdfCanvas canvas, float totalWidth, bool measureOnly, float originX = 0f, float originY = 0f)
        {
            _canvas = canvas;
            _totalWidth = totalWidth;
            _measureOnly = measureOnly;
            _cumulativeX = originX;
            _originY = originY;
        }

        internal float MeasuredHeight => _maxHeight;

        /// <summary>Add a column occupying <paramref name="widthFraction"/> of the row's total width.</summary>
        /// <param name="widthFraction">A value in (0, 1]. Fractions across all columns in the row should sum to at most 1.</param>
        public LayoutRow Column(float widthFraction, Action<LayoutColumn> configure)
        {
            if (widthFraction <= 0f || widthFraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(widthFraction),
                    "Column width fraction must be greater than 0 and no more than 1.");
            return AddColumn(_totalWidth * widthFraction, configure);
        }

        /// <summary>Add a column with a fixed width. Use <see cref="LayoutColumn.Fixed"/> to build the width.</summary>
        public LayoutRow Column(LayoutColumnWidth width, Action<LayoutColumn> configure)
        {
            float points = width.IsFraction ? _totalWidth * width.Value : width.Value;
            return AddColumn(points, configure);
        }

        private LayoutRow AddColumn(float widthPoints, Action<LayoutColumn> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var column = new LayoutColumn(_canvas, widthPoints, _measureOnly, _cumulativeX, _originY);
            configure(column);
            if (column.MeasuredHeight > _maxHeight) _maxHeight = column.MeasuredHeight;
            _cumulativeX += widthPoints;
            return this;
        }
    }
}
