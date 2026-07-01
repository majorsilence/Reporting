using System;
#if DRAWINGCOMPAT
using Drawing = Majorsilence.Drawing;
#else
using Drawing = System.Drawing;
#endif
using Majorsilence.Reporting.Rdl;

namespace RdlEngine.Render.ExcelConverter
{
    public class ExcelCellStyle
    {
        public ExcelCellStyle() { }

        public ExcelCellStyle(StyleInfo fromStyle)
        {
            SetBackgroundColor(fromStyle.BackgroundColor);

            SetBorderTop(fromStyle.BStyleTop, fromStyle.BWidthTop, fromStyle.BColorTop);
            SetBorderRight(fromStyle.BStyleRight, fromStyle.BWidthRight, fromStyle.BColorRight);
            SetBorderBottom(fromStyle.BStyleBottom, fromStyle.BWidthBottom, fromStyle.BColorBottom);
            SetBorderLeft(fromStyle.BStyleLeft, fromStyle.BWidthLeft, fromStyle.BColorLeft);

            SetVerticalAlign(fromStyle.VerticalAlign);
            SetTextAlign(fromStyle.TextAlign);

            FontName = fromStyle.FontFamily;
            FontSize = fromStyle.FontSize;
            SetFontWeight(fromStyle.FontWeight);
            SetFontStyle(fromStyle.FontStyle);
            SetTextDecoration(fromStyle.TextDecoration);
            SetFontColor(fromStyle.Color);
            SetWritingMode(fromStyle.WritingMode);
        }

        internal XlsxCellFormat ToXlsxFormat()
        {
            var fmt = new XlsxCellFormat();

            fmt.FontName = FontName ?? "Calibri";
            fmt.FontSizePt = FontSize > 0 ? FontSize - 1 : 10; // match NPOI behaviour: reduce by 1pt
            fmt.Bold = _fontWeight > 400;
            fmt.Italic = _fontStyle == FontStyleEnum.Italic;
            fmt.Strikeout = _textDecoration == TextDecorationEnum.LineThrough;
            fmt.Underline = _textDecoration == TextDecorationEnum.Underline;
            fmt.FontColor = ColorToTuple(_fontColor.IsEmpty ? Drawing.Color.Black : _fontColor);

            fmt.HasBackground = !_backgroundColor.IsEmpty;
            fmt.BackgroundColor = ColorToTuple(_backgroundColor.IsEmpty ? Drawing.Color.White : _backgroundColor);

            fmt.BorderTop = ConvertSide(_borderTop, _borderTopWidth, _borderTopColor);
            fmt.BorderRight = ConvertSide(_borderRight, _borderRightWidth, _borderRightColor);
            fmt.BorderBottom = ConvertSide(_borderBottom, _borderBottomWidth, _borderBottomColor);
            fmt.BorderLeft = ConvertSide(_borderLeft, _borderLeftWidth, _borderLeftColor);

            fmt.HorizontalAlign = ConvertHorizontalAlign(_horizontalAlign);
            fmt.VerticalAlign = ConvertVerticalAlign(_verticalAlign);
            fmt.WrapText = true;

            switch (_writingMode)
            {
                case WritingModeEnum.tb_rl: fmt.Rotation = -90; break;
                case WritingModeEnum.tb_lr: fmt.Rotation = 90; break;
                case WritingModeEnum.rl_bt: fmt.Rotation = 180; break;
                default: fmt.Rotation = 0; break;
            }

            return fmt;
        }

        // Font
        private short _fontWeight = 400;
        private FontStyleEnum _fontStyle;
        private TextDecorationEnum _textDecoration;
        private Drawing.Color _fontColor;
        private WritingModeEnum _writingMode;

        public float FontSize { get; set; }
        public string FontName { get; set; }

        public void SetFontWeight(FontWeightEnum weight) => _fontWeight = ConvertFontWeight(weight);
        public void SetFontStyle(FontStyleEnum style) => _fontStyle = style;
        public void SetTextDecoration(TextDecorationEnum td) => _textDecoration = td;
        public void SetFontColor(Drawing.Color color) => _fontColor = color;
        public void SetWritingMode(WritingModeEnum wm) => _writingMode = wm;

        // Background
        private Drawing.Color _backgroundColor;
        public void SetBackgroundColor(Drawing.Color color) => _backgroundColor = color;

        // Borders
        private BorderStyleEnum _borderTop;
        private Drawing.Color _borderTopColor;
        private short _borderTopWidth;

        private BorderStyleEnum _borderRight;
        private Drawing.Color _borderRightColor;
        private short _borderRightWidth;

        private BorderStyleEnum _borderBottom;
        private Drawing.Color _borderBottomColor;
        private short _borderBottomWidth;

        private BorderStyleEnum _borderLeft;
        private Drawing.Color _borderLeftColor;
        private short _borderLeftWidth;

        public void SetBorderTop(BorderStyleEnum style, float width, Drawing.Color color)
        { _borderTop = style; _borderTopWidth = (short)width; _borderTopColor = color; }

        public void SetBorderRight(BorderStyleEnum style, float width, Drawing.Color color)
        { _borderRight = style; _borderRightWidth = (short)width; _borderRightColor = color; }

        public void SetBorderBottom(BorderStyleEnum style, float width, Drawing.Color color)
        { _borderBottom = style; _borderBottomWidth = (short)width; _borderBottomColor = color; }

        public void SetBorderLeft(BorderStyleEnum style, float width, Drawing.Color color)
        { _borderLeft = style; _borderLeftWidth = (short)width; _borderLeftColor = color; }

        // Alignment
        private TextAlignEnum _horizontalAlign;
        private VerticalAlignEnum _verticalAlign;

        public void SetTextAlign(TextAlignEnum ta) => _horizontalAlign = ta;
        public void SetVerticalAlign(VerticalAlignEnum va) => _verticalAlign = va;

        // Converters
        private static XlsxBorderSide ConvertSide(BorderStyleEnum style, short width, Drawing.Color color)
        {
            string s = ConvertBorderStyle(style, width);
            if (string.IsNullOrEmpty(s)) return new XlsxBorderSide();
            return new XlsxBorderSide
            {
                Style = s,
                Color = ColorToTuple(color.IsEmpty ? Drawing.Color.Black : color)
            };
        }

        public static string ConvertBorderStyle(BorderStyleEnum style, short width)
        {
            switch (style)
            {
                case BorderStyleEnum.None: return "";
                case BorderStyleEnum.Solid:
                    if (width <= 1) return "thin";
                    if (width == 2) return "medium";
                    return "thick";
                case BorderStyleEnum.Dotted: return "dotted";
                case BorderStyleEnum.Dashed: return "dashed";
                case BorderStyleEnum.Double: return "double";
                default: return "hair";
            }
        }

        public static short ConvertFontWeight(FontWeightEnum weight)
        {
            switch (weight)
            {
                case FontWeightEnum.W100: return 100;
                case FontWeightEnum.W200: return 200;
                case FontWeightEnum.Lighter:
                case FontWeightEnum.W300: return 300;
                case FontWeightEnum.W500: return 500;
                case FontWeightEnum.W600: return 600;
                case FontWeightEnum.Bold:
                case FontWeightEnum.W700: return 700;
                case FontWeightEnum.W800: return 800;
                case FontWeightEnum.Bolder:
                case FontWeightEnum.W900: return 900;
                default: return 400;
            }
        }

        private static string ConvertVerticalAlign(VerticalAlignEnum va)
        {
            switch (va)
            {
                case VerticalAlignEnum.Middle: return "center";
                case VerticalAlignEnum.Bottom: return "bottom";
                default: return "top";
            }
        }

        private static string ConvertHorizontalAlign(TextAlignEnum ha)
        {
            switch (ha)
            {
                case TextAlignEnum.Center: return "center";
                case TextAlignEnum.Right: return "right";
                case TextAlignEnum.Justified: return "justify";
                default: return "left";
            }
        }

        private static (byte R, byte G, byte B) ColorToTuple(Drawing.Color c)
            => (c.R, c.G, c.B);
    }
}
