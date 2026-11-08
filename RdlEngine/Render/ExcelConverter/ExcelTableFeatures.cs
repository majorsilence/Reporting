using System.Collections.Generic;
using System.Linq;
using Majorsilence.Excel;

namespace RdlEngine.Render.ExcelConverter
{
    /// <summary>
    /// Freezes the panes below a report's table header and puts an AutoFilter on it, when the sheet
    /// has exactly one table, so its layout is the usual header-over-rows list.
    /// </summary>
    internal static class ExcelTableFeatures
    {
        // Freezing more than this much of the sheet's top leaves little room to scroll.
        private const float MaxFrozenHeightPoints = 216f;

        public static void Apply(XlsxWriter workbook, ExcelCellsBuilder builder, IReadOnlyList<(int r1, int c1, int r2, int c2)> merges)
        {
            if (builder.Tables.Count != 1)
                return;
            var table = builder.Tables[0];
            if (table.HeaderRows.Count == 0 || table.DetailRows.Count == 0)
                return;

            int headerLast = table.HeaderRows.Max(r => builder.Rows.IndexOf(r));
            int dataLast = table.DetailRows.Max(r => builder.Rows.IndexOf(r));
            if (headerLast < 0 || dataLast <= headerLast)
                return;

            if (headerLast + 1 < builder.Rows.Count && builder.Rows[headerLast + 1].YPosition <= MaxFrozenHeightPoints)
                workbook.FreezePanes(headerLast + 1, 0);

            var headerCells = builder.Rows[headerLast].Cells.Where(c => c.ExcelTable == table).ToList();
            if (headerCells.Count == 0)
                return;
            int colFirst = headerCells.Min(c => builder.Columns.IndexOf(c.Column));
            int colLast = headerCells.Max(c => builder.Columns.IndexOf(c.Column) + builder.GetRightAttachCells(c));

            // Sorting or filtering rows that hold merged cells fails in Excel.
            bool mergedData = merges.Any(m => m.r2 > headerLast && m.r1 <= dataLast && m.c2 >= colFirst && m.c1 <= colLast);
            if (!mergedData)
                workbook.SetAutoFilter(headerLast, colFirst, dataLast, colLast);
        }
    }
}
