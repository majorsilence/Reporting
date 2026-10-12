// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System;
using System.Collections.Generic;

namespace Majorsilence.Pdf.Layout
{
    /// <summary>
    /// Builds the columns of a single row inside a <see cref="PdfLayout.Row"/> call. Columns are
    /// packed left-to-right in the order <see cref="Column(float, Action{LayoutColumn})"/> is
    /// called; there is no gap between them and fractional widths should sum to at most 1.0.
    /// </summary>
    public sealed class LayoutRow
    {
        private readonly List<(LayoutColumnWidth, StackElement)> _columns = new List<(LayoutColumnWidth, StackElement)>();

        private LayoutRow() { }

        internal static LayoutElement Build(Action<LayoutRow> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var row = new LayoutRow();
            configure(row);
            return new RowElement(row._columns.ToArray());
        }

        /// <summary>Add a column occupying <paramref name="widthFraction"/> of the row's total width.</summary>
        /// <param name="widthFraction">A value in (0, 1]. Fractions across all columns in the row should sum to at most 1.</param>
        public LayoutRow Column(float widthFraction, Action<LayoutColumn> configure)
        {
            if (widthFraction <= 0f || widthFraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(widthFraction),
                    "Column width fraction must be greater than 0 and no more than 1.");
            return AddColumn(new LayoutColumnWidth(widthFraction, isFraction: true), configure);
        }

        /// <summary>Add a column with a fixed width. Use <see cref="LayoutColumn.Fixed"/> to build the width.</summary>
        public LayoutRow Column(LayoutColumnWidth width, Action<LayoutColumn> configure) =>
            AddColumn(width, configure);

        private LayoutRow AddColumn(LayoutColumnWidth width, Action<LayoutColumn> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));
            var column = new LayoutColumn();
            configure(column);
            _columns.Add((width, column.ToElement()));
            return this;
        }
    }
}
