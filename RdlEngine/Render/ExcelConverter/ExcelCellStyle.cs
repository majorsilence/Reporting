using System;
#if DRAWINGCOMPAT
using Drawing = Majorsilence.Drawing;
#else
using Drawing = System.Drawing;
#endif
using System.Linq;
using Majorsilence.Reporting.Rdl;
using NPOI.OOXML.XSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace RdlEngine.Render.ExcelConverter
{
	public class ExcelCellStyle
	{
		public ExcelCellStyle()
		{
		}

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

		public void SetToStyle(XSSFCellStyle style)
		{
			style.SetFillForegroundColor(BackgroundColor);
			style.FillPattern = FillPattern.SolidForeground;

			style.BorderTop = BorderTop;
			style.SetTopBorderColor(BorderTopColor);
			style.BorderRight = BorderRight;
			style.SetRightBorderColor(BorderRightColor);
			style.BorderBottom = BorderBottom;
			style.SetBottomBorderColor(BorderBottomColor);
			style.BorderLeft = BorderLeft;
			style.SetLeftBorderColor(BorderLeftColor);

			style.VerticalAlignment = VerticalAlignment;
			style.Alignment = HorizontalAlignment;
			switch (_writingMode)
			{
				case WritingModeEnum.lr_tb:
					style.Rotation = 0;
					break;
				case WritingModeEnum.tb_rl:
					style.Rotation = -90;
					break;
				case WritingModeEnum.rl_bt:
					style.Rotation = 180;
					break;
				case WritingModeEnum.tb_lr:
					style.Rotation = 90;
					break;
				default:
					throw new ArgumentOutOfRangeException($"Writing mode {_writingMode} is not supported");
			}
		}

		public void SetToFont(XSSFFont font)
		{
			font.FontName = FontName;
			font.FontHeightInPoints = (short)FontSize;
			font.IsBold = FontWeight > 400;
			font.IsItalic = IsItalic;
			font.IsStrikeout = IsStrikeout;
			font.Underline = IsUnderline ? FontUnderlineType.Single : FontUnderlineType.None;
			font.SetColor(FontColor);
		}

		//font
		private FontWeightEnum fontWeight;
		public void SetFontWeight(FontWeightEnum weight)
		{
			fontWeight = weight;
		}
		public short FontWeight {
			get {
				return ConvertFontWeight(fontWeight);
			}
		}

		private FontStyleEnum fontStyle;
		public void SetFontStyle(FontStyleEnum style)
		{
			fontStyle = style;
		}
		public bool IsItalic {
			get{
				return fontStyle == FontStyleEnum.Italic;
			}
		}

		private TextDecorationEnum textDecoration;
		public void SetTextDecoration(TextDecorationEnum textDecor)
		{
			textDecoration = textDecor;
		}
		public bool IsStrikeout {
			get {
				return textDecoration == TextDecorationEnum.LineThrough;
			}
		}
		public bool IsUnderline {
			get {
				return textDecoration == TextDecorationEnum.Underline;
			}
		}

		public float FontSize { get; set; }
		public string FontName { get; set; }

		private Drawing.Color fontColor;
		public void SetFontColor(Drawing.Color color)
		{
			fontColor = color;
		}
		public XSSFColor FontColor {
			get {
				var color = ColorToByteArray(fontColor.IsEmpty ? Drawing.Color.Black : fontColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		//alignments

		private TextAlignEnum horizontalAlign;
		public void SetTextAlign(TextAlignEnum textAlign)
		{
			horizontalAlign = textAlign;
		}
		public HorizontalAlignment HorizontalAlignment {
			get {
				return ConvertHorizontalAlignment(horizontalAlign);
			}
		}

		private VerticalAlignEnum verticalAlign;
		public void SetVerticalAlign(VerticalAlignEnum vertAlign)
		{
			verticalAlign = vertAlign;
		}
		public VerticalAlignment VerticalAlignment {
			get {
				return ConvertVerticalAlignment(verticalAlign);
			}
		}

		//background
		private  Drawing.Color backgroundColor;
		public void SetBackgroundColor(Drawing.Color color)
		{
			backgroundColor = color;
		}
		public XSSFColor BackgroundColor {
			get
			{
				var color = ColorToByteArray(backgroundColor.IsEmpty ? Drawing.Color.White : backgroundColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		//Borders

		//top
		private Majorsilence.Reporting.Rdl.BorderStyleEnum borderTop;
		private Drawing.Color borderTopColor;
		private short borderTopWidth;

		public void SetBorderTop(Majorsilence.Reporting.Rdl.BorderStyleEnum style)
		{
			SetBorderTop(style, 1, Drawing.Color.Black);
		}

		public void SetBorderTop(Majorsilence.Reporting.Rdl.BorderStyleEnum style, float width, Drawing.Color color)
		{
			borderTopColor = color;
			borderTop = style;
			borderTopWidth = (short)width;
		}

		public XSSFColor BorderTopColor {
			get
			{
				var color = ColorToByteArray(borderTopColor.IsEmpty ? Drawing.Color.Black : borderTopColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		public BorderStyle BorderTop {
			get {
				return ConvertBorderStyle(borderTop, borderTopWidth);
			}
		}

		//right
		private Majorsilence.Reporting.Rdl.BorderStyleEnum borderRight;
		private Drawing.Color borderRightColor;
		private short borderRightWidth;

		public void SetBorderRight(Majorsilence.Reporting.Rdl.BorderStyleEnum style)
		{
			SetBorderRight(style, 1, Drawing.Color.Black);
		}

		public void SetBorderRight(Majorsilence.Reporting.Rdl.BorderStyleEnum style, float width, Drawing.Color color)
		{
			borderRightColor = color;
			borderRight = style;
			borderRightWidth = (short)width;
		}

		public XSSFColor BorderRightColor {
			get
			{
				var color = ColorToByteArray(borderRightColor.IsEmpty ? Drawing.Color.Black : borderRightColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		public BorderStyle BorderRight {
			get {
				return ConvertBorderStyle(borderRight, borderRightWidth);
			}
		}



		//bottom
		private Majorsilence.Reporting.Rdl.BorderStyleEnum borderBottom;
		private Drawing.Color borderBottomColor;
		private short borderBottomWidth;

		public void SetBorderBottom(Majorsilence.Reporting.Rdl.BorderStyleEnum style)
		{
			SetBorderBottom(style, 1, Drawing.Color.Black);
		}

		public void SetBorderBottom(Majorsilence.Reporting.Rdl.BorderStyleEnum style, float width, Drawing.Color color)
		{
			borderBottomColor = color;
			borderBottom = style;
			borderBottomWidth = (short)width;
		}

		public XSSFColor BorderBottomColor {
			get
			{
				var color = ColorToByteArray(borderBottomColor.IsEmpty ? Drawing.Color.Black : borderBottomColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		public BorderStyle BorderBottom {
			get {
				return ConvertBorderStyle(borderBottom, borderBottomWidth);
			}
		}

		//left
		private Majorsilence.Reporting.Rdl.BorderStyleEnum borderLeft;
		private Drawing.Color borderLeftColor;
		private short borderLeftWidth;

		public void SetBorderLeft(Majorsilence.Reporting.Rdl.BorderStyleEnum style)
		{
			SetBorderLeft(style, 1, Drawing.Color.Black);
		}

		public void SetBorderLeft(Majorsilence.Reporting.Rdl.BorderStyleEnum style, float width, Drawing.Color color)
		{
			borderLeftColor = color;
			borderLeft = style;
			borderLeftWidth = (short)width;
		}

		public XSSFColor BorderLeftColor {
			get
			{
				var color = ColorToByteArray(borderLeftColor.IsEmpty ? Drawing.Color.Black : borderLeftColor);
				return new XSSFColor(color, new DefaultIndexedColorMap());
			}
		}

		public BorderStyle BorderLeft {
			get {
				return ConvertBorderStyle(borderLeft, borderLeftWidth);
			}
		}

		private WritingModeEnum _writingMode;

		public void SetWritingMode(WritingModeEnum writingMode)
		{
			_writingMode = writingMode;
		}
		
		public bool CompareWithXSSFFont(XSSFFont font)
		{
			if(FontName != font.FontName) {
				return false;
			}
			if(FontWeight > 400 != font.IsBold) {
				return false;
			}
			if(Math.Abs(FontSize - font.FontHeightInPoints) > 0.01) {
				return false;
			}
			if(IsUnderline && font.Underline == FontUnderlineType.None) {
				return false;
			}
			if(IsStrikeout != font.IsStrikeout) {
				return false;
			}
			if(IsItalic != font.IsItalic) {
				return false;
			}
			if(!CompareColor(FontColor, font.GetXSSFColor())) {
				return false;
			}
			return true;
		}

		public bool CompareWithXSSFStyle(XSSFCellStyle style)
		{

			//background color
			if(!CompareColor(BackgroundColor, style.FillForegroundXSSFColor)) {
				return false;
			}

			//borders colors
			if(!CompareColor(BorderTopColor, style.TopBorderXSSFColor)) {
				return false;
			}
			if(!CompareColor(BorderRightColor, style.RightBorderXSSFColor)) {
				return false;
			}
			if(!CompareColor(BorderBottomColor, style.BottomBorderXSSFColor)) {
				return false;
			}
			if(!CompareColor(BorderLeftColor, style.LeftBorderXSSFColor)) {
				return false;
			}

			//borders styles
			if(BorderTop != style.BorderTop) {
				return false;
			}
			if(BorderRight != style.BorderRight) {
				return false;
			}
			if(BorderBottom != style.BorderBottom) {
				return false;
			}
			if(BorderLeft != style.BorderLeft) {
				return false;
			}

			//alignments
			if(HorizontalAlignment != style.Alignment) {
				return false;
			}
			if(VerticalAlignment != style.VerticalAlignment) {
				return false;
			}

			var font = style.GetFont();

			if(!CompareWithXSSFFont(font)) {
				return false;
			}

			return true;
		}

		private bool CompareColor(XSSFColor c1, XSSFColor c2)
		{
			if(c1 == null || c2 == null) {
				return false;
			}
			if(c1.RGB == null || c2.RGB == null) {
				if(c1.Indexed > 0 && c2.Indexed > 0) {
					return c1.Indexed == c2.Indexed;
				} else{
					return false;
				}
			}
			return c1.RGB.SequenceEqual(c2.RGB);
		}

		public BorderStyle ConvertBorderStyle(Majorsilence.Reporting.Rdl.BorderStyleEnum style, short width)
		{
			switch(style) {
				case Majorsilence.Reporting.Rdl.BorderStyleEnum.None:
					return BorderStyle.None;
				case Majorsilence.Reporting.Rdl.BorderStyleEnum.Solid:
					if(width <= 1) {
						return BorderStyle.Thin;
					}
					if(width == 2) {
						return BorderStyle.Medium;
					} else {
						return BorderStyle.Thick;
					}
				case Majorsilence.Reporting.Rdl.BorderStyleEnum.Dotted:
					return BorderStyle.Dotted;
				case Majorsilence.Reporting.Rdl.BorderStyleEnum.Dashed:
					return BorderStyle.Dashed;
				case Majorsilence.Reporting.Rdl.BorderStyleEnum.Double:
					return BorderStyle.Double;
				default:
					return BorderStyle.Hair;
			}
		}

		public short ConvertFontWeight(FontWeightEnum weight)
		{
			switch(weight) {
				case FontWeightEnum.W100:
					return 100;
				case FontWeightEnum.W200:
					return 200;
				case FontWeightEnum.Lighter:
				case FontWeightEnum.W300:
					return 300;
				case FontWeightEnum.W500:
					return 500;
				case FontWeightEnum.W600:
					return 600;
				case FontWeightEnum.Bold:
				case FontWeightEnum.W700:
					return 700;
				case FontWeightEnum.W800:
					return 800;
				case FontWeightEnum.Bolder:
				case FontWeightEnum.W900:
					return 900;
				case FontWeightEnum.Normal:
				case FontWeightEnum.W400:
				default:
					return 400;
			}
		}

		public VerticalAlignment ConvertVerticalAlignment(VerticalAlignEnum va)
		{
			switch(va) {
				case VerticalAlignEnum.Middle:
					return VerticalAlignment.Center;
				case VerticalAlignEnum.Bottom:
					return VerticalAlignment.Bottom;
				default:
					return VerticalAlignment.Top;
			}
		}

		public HorizontalAlignment ConvertHorizontalAlignment(TextAlignEnum ha)
		{
			switch(ha) {
				case TextAlignEnum.Center:
					return HorizontalAlignment.Center;
				case TextAlignEnum.Right:
					return HorizontalAlignment.Right;
				case TextAlignEnum.Justified:
					return HorizontalAlignment.Justify;
				default:
					return HorizontalAlignment.Left;
			}
		}
		
		private byte[] ColorToByteArray(Drawing.Color color)
		{
			return new byte[] { color.R, color.G, color.B, color.A };
		}
	}
}
