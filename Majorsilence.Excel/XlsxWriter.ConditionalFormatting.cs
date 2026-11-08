using System;
using System.Collections.Generic;
using System.Text;

namespace Majorsilence.Excel
{
    /// <summary>The formatting a conditional format rule applies on top of the cell's own; null leaves a property as it is.</summary>
    public class XlsxDifferentialFormat
    {
        public bool? Bold;
        public bool? Italic;
        public bool? Strikeout;
        public bool? Underline;
        public (byte R, byte G, byte B)? FontColor;
        public (byte R, byte G, byte B)? BackgroundColor;
        public string? NumberFormatCode;
    }

    public enum XlsxConditionOperator
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual,
        GreaterThanOrEqual,
        GreaterThan,
        Between,
        NotBetween,
    }

    public partial class XlsxWriter
    {
        private readonly List<DxfDef> _dxfs = new List<DxfDef>();

        /// <summary>
        /// Formats cells in the range whose value compares to formula1 (and formula2 for Between/NotBetween).
        /// Formulas are OOXML formula text without '=', e.g. "100", "$B$1" or "\"Done\"".
        /// </summary>
        public void AddConditionalFormat(int r1, int c1, int r2, int c2, XlsxConditionOperator op,
            string formula1, string? formula2, XlsxDifferentialFormat format, int sheetIndex = 0)
        {
            if (formula1 == null) throw new ArgumentNullException(nameof(formula1));
            if (format == null) throw new ArgumentNullException(nameof(format));
            bool range = op == XlsxConditionOperator.Between || op == XlsxConditionOperator.NotBetween;
            if (range && formula2 == null)
                throw new ArgumentException("Between and NotBetween need a second formula.", nameof(formula2));

            var formulas = range ? new[] { StripEquals(formula1), StripEquals(formula2!) } : new[] { StripEquals(formula1) };
            _sheets[sheetIndex].ConditionalFormats.Add(new ConditionalFormatEntry(r1, c1, r2, c2, "cellIs", OperatorName(op), formulas, GetOrAddDxf(format)));
        }

        public void AddConditionalFormat(int r1, int c1, int r2, int c2, XlsxConditionOperator op,
            double value1, XlsxDifferentialFormat format, int sheetIndex = 0)
            => AddConditionalFormat(r1, c1, r2, c2, op, FormatDouble(value1), null, format, sheetIndex);

        public void AddConditionalFormat(int r1, int c1, int r2, int c2, XlsxConditionOperator op,
            double value1, double value2, XlsxDifferentialFormat format, int sheetIndex = 0)
            => AddConditionalFormat(r1, c1, r2, c2, op, FormatDouble(value1), FormatDouble(value2), format, sheetIndex);

        /// <summary>
        /// Formats cells in the range where the formula is true. The formula is written relative to the
        /// range's top-left cell, e.g. "$C1&gt;100" over A1:D20 highlights whole rows by column C.
        /// </summary>
        public void AddConditionalFormatExpression(int r1, int c1, int r2, int c2, string formula,
            XlsxDifferentialFormat format, int sheetIndex = 0)
        {
            if (formula == null) throw new ArgumentNullException(nameof(formula));
            if (format == null) throw new ArgumentNullException(nameof(format));
            _sheets[sheetIndex].ConditionalFormats.Add(new ConditionalFormatEntry(r1, c1, r2, c2, "expression", null,
                new[] { StripEquals(formula) }, GetOrAddDxf(format)));
        }

        private static string StripEquals(string formula) =>
            formula.Length > 0 && formula[0] == '=' ? formula.Substring(1) : formula;

        private static string OperatorName(XlsxConditionOperator op) => op switch
        {
            XlsxConditionOperator.LessThan => "lessThan",
            XlsxConditionOperator.LessThanOrEqual => "lessThanOrEqual",
            XlsxConditionOperator.Equal => "equal",
            XlsxConditionOperator.NotEqual => "notEqual",
            XlsxConditionOperator.GreaterThanOrEqual => "greaterThanOrEqual",
            XlsxConditionOperator.GreaterThan => "greaterThan",
            XlsxConditionOperator.Between => "between",
            XlsxConditionOperator.NotBetween => "notBetween",
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };

        private int GetOrAddDxf(XlsxDifferentialFormat f)
        {
            var def = new DxfDef
            {
                Bold = f.Bold,
                Italic = f.Italic,
                Strikeout = f.Strikeout,
                Underline = f.Underline,
                FontColor = f.FontColor,
                BackgroundColor = f.BackgroundColor,
                NumFmtId = f.NumberFormatCode != null ? GetOrAddNumFmt(f.NumberFormatCode) : (int?)null,
                NumberFormatCode = f.NumberFormatCode,
            };
            return GetOrAdd(_dxfs, def);
        }

        // CT_Dxf child order: font, numFmt, fill, alignment, protection, border.
        private void AppendDxfs(StringBuilder sb)
        {
            if (_dxfs.Count == 0) return;
            sb.Append($"<dxfs count=\"{_dxfs.Count}\">");
            foreach (var d in _dxfs)
            {
                sb.Append("<dxf>");
                if (d.Bold.HasValue || d.Italic.HasValue || d.Strikeout.HasValue || d.Underline.HasValue || d.FontColor.HasValue)
                {
                    sb.Append("<font>");
                    if (d.Bold.HasValue) sb.Append(d.Bold.Value ? "<b/>" : "<b val=\"0\"/>");
                    if (d.Italic.HasValue) sb.Append(d.Italic.Value ? "<i/>" : "<i val=\"0\"/>");
                    if (d.Strikeout.HasValue) sb.Append(d.Strikeout.Value ? "<strike/>" : "<strike val=\"0\"/>");
                    if (d.FontColor.HasValue) sb.Append($"<color rgb=\"{Argb(d.FontColor.Value)}\"/>");
                    if (d.Underline.HasValue) sb.Append(d.Underline.Value ? "<u/>" : "<u val=\"none\"/>");
                    sb.Append("</font>");
                }
                if (d.NumFmtId.HasValue)
                    sb.Append($"<numFmt numFmtId=\"{d.NumFmtId.Value}\" formatCode=\"{XmlEscape(d.NumberFormatCode!)}\"/>");
                if (d.BackgroundColor.HasValue)
                    sb.Append($"<fill><patternFill><bgColor rgb=\"{Argb(d.BackgroundColor.Value)}\"/></patternFill></fill>");
                sb.Append("</dxf>");
            }
            sb.Append("</dxfs>");
        }

        private static string Argb((byte R, byte G, byte B) c) => $"FF{c.R:X2}{c.G:X2}{c.B:X2}";

        // Priorities are unique within a sheet; earlier rules win.
        private static void AppendConditionalFormatting(StringBuilder sb, Sheet sheet)
        {
            int priority = 1;
            foreach (var cf in sheet.ConditionalFormats)
            {
                string sqref = ColRef(cf.C1) + (cf.R1 + 1);
                if (cf.R1 != cf.R2 || cf.C1 != cf.C2)
                    sqref += ":" + ColRef(cf.C2) + (cf.R2 + 1);
                sb.Append($"<conditionalFormatting sqref=\"{sqref}\">");
                sb.Append($"<cfRule type=\"{cf.Type}\" dxfId=\"{cf.DxfId}\" priority=\"{priority++}\"");
                if (cf.Operator != null) sb.Append($" operator=\"{cf.Operator}\"");
                sb.Append(">");
                foreach (var f in cf.Formulas)
                    sb.Append($"<formula>{XmlEscape(f)}</formula>");
                sb.Append("</cfRule></conditionalFormatting>");
            }
        }

        private class DxfDef : IEquatable<DxfDef>
        {
            public bool? Bold, Italic, Strikeout, Underline;
            public (byte R, byte G, byte B)? FontColor, BackgroundColor;
            public int? NumFmtId;
            public string? NumberFormatCode;

            public bool Equals(DxfDef? o) =>
                o != null && Bold == o.Bold && Italic == o.Italic && Strikeout == o.Strikeout &&
                Underline == o.Underline && FontColor == o.FontColor && BackgroundColor == o.BackgroundColor &&
                NumFmtId == o.NumFmtId;
        }

        private class ConditionalFormatEntry
        {
            public readonly int R1, C1, R2, C2, DxfId;
            public readonly string Type;
            public readonly string? Operator;
            public readonly string[] Formulas;

            public ConditionalFormatEntry(int r1, int c1, int r2, int c2, string type, string? op, string[] formulas, int dxfId)
            {
                R1 = r1; C1 = c1; R2 = r2; C2 = c2; Type = type; Operator = op; Formulas = formulas; DxfId = dxfId;
            }
        }

        private partial class Sheet
        {
            public readonly List<ConditionalFormatEntry> ConditionalFormats = new List<ConditionalFormatEntry>();
        }
    }
}
