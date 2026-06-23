

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Threading.Tasks;

#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
using Drawing2D = Majorsilence.Drawing.Drawing2D;
#else
using Draw2 = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
#endif
using System.Globalization;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Style (borders, fonts, background, padding, ...) of a ReportItem.
	///</summary>
	[Serializable]
	internal class Style : ReportLink
	{
		StyleBorderColor _BorderColor;	// Color of the border
		StyleBorderStyle _BorderStyle;	// Style of the border
		StyleBorderWidth _BorderWidth;	// Width of the border
		Expression _BackgroundColor;	//(Color) Color of the background
		// If omitted, the background is transparent
		Expression _BackgroundGradientType;	// The type of background gradient
		Expression _BackgroundGradientEndColor;	//(Color) End color for the background gradient. If
		// omitted, there is no gradient.
		StyleBackgroundImage _BackgroundImage;	// A background image for the report item.
		// If omitted, there is no background image.
		Expression _FontStyle;		// (Enum FontStyle) Font style Default: Normal
		Expression _FontFamily;		//(string)Name of the font family Default: Arial
		Expression _FontSize;		//(Size) Point size of the font
		// Default: 10 pt. Min: 1 pt. Max: 200 pt.
		Expression _FontWeight;		//(Enum FontWeight) Thickness of the font
		Expression _Format;			//(string) .NET Framework formatting string1
		//	Note: Locale-dependent currency
		//	formatting (format code �C�) is based on
		//	the language setting for the report item
		//	Locale-dependent date formatting is
		//	supported and should be based on the
		//	language property of the ReportItem.
		//	Default: No formatting.
		Expression _TextDecoration;	// (Enum TextDecoration) Special text formatting Default: none
		Expression _TextAlign;		// (Enum TextAlign) Horizontal alignment of the text Default: General
		
		Expression _VerticalAlign;	// (Enum VerticalAlign)	Vertical alignment of the text Default: Top
		Expression _Color;			// (Color) The foreground color	Default: Black
		Expression _PaddingLeft;	// (Size)Padding between the left edge of the
		// report item and its contents1
		// Default: 0 pt. Max: 1000 pt.
		Expression _PaddingRight;	// (Size) Padding between the right edge of the
		// report item and its contents
		// Default: 0 pt. Max: 1000 pt.
		Expression _PaddingTop;		// (Size) Padding between the top edge of the
		// report item and its contents
		// Default: 0 pt. Max: 1000 pt.
		Expression _PaddingBottom;	// (Size) Padding between the top edge of the
		//	report item and its contents
		// Default: 0 pt. Max: 1000 pt
		Expression _LineHeight;		// (Size) Height of a line of text
		// Default: Report output format determines
		// line height based on font size
		// Min: 1 pt. Max: 1000 pt.
		Expression _Direction;		// (Enum Direction) Indicates whether text is written left-to-right (default)
		// or right-to-left.
		// Does not impact the alignment of text
		// unless using General alignment.
		Expression _WritingMode;	// (Enum WritingMode) Indicates whether text is written
		// horizontally or vertically.
		Expression _Language;		// (Language) The primary language of the text.
		// Default is Report.Language.
		Expression _UnicodeBiDirectional;	// (Enum UnicodeBiDirection) 
		// Indicates the level of embedding with
		// respect to the Bi-directional algorithm. Default: normal
		Expression _Calendar;		// (Enum Calendar)
		//	Indicates the calendar to use for
		//	formatting dates. Must be compatible in
		//	.NET framework with the Language
		//	setting.
		Expression _NumeralLanguage;	// (Language) The digit format to use as described by its
		// primary language. Any language is legal.
		// Default is the Language property.
		Expression _NumeralVariant;	//(Integer) The variant of the digit format to use.
		// Currently defined values are:
		// 1: default, follow Unicode context rules
		// 2: 0123456789
		// 3: traditional digits for the script as
		//     defined in GDI+. Currently supported for:
		//		ar | bn | bo | fa | gu | hi | kn | kok | lo | mr |
		//		ms | or | pa | sa | ta | te | th | ur and variants.
		// 4: ko, ja, zh-CHS, zh-CHT only
		// 5: ko, ja, zh-CHS, zh-CHT only
		// 6: ko, ja, zh-CHS, zh-CHT only [Wide
		//     versions of regular digits]
		// 7: ko only
		bool _ConstantStyle;		//  true if all Style elements are constant

		internal Style(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_BorderColor=null;
			_BorderStyle=null;
			_BorderWidth=null;
			_BackgroundColor=null;
			_BackgroundGradientType=null;
			_BackgroundGradientEndColor=null;
			_BackgroundImage=null;
			_FontStyle=null;
			_FontFamily=null;
			_FontSize=null;
			_FontWeight=null;
			_Format=null;
			_TextDecoration=null;
			_TextAlign=null;
			_VerticalAlign=null;
			_Color=null;
			_PaddingLeft=null;
			_PaddingRight=null;
			_PaddingTop=null;
			_PaddingBottom=null;
			_LineHeight=null;
			_Direction=null;
			_WritingMode=null;
			_Language=null;
			_UnicodeBiDirectional=null;
			_Calendar=null;
			_NumeralLanguage=null;
			_NumeralVariant=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "bordercolor":
						_BorderColor = new StyleBorderColor(r, this, xNodeLoop);
						break;
					case "borderstyle":
						_BorderStyle = new StyleBorderStyle(r, this, xNodeLoop);
						break;
					case "borderwidth":
						_BorderWidth = new StyleBorderWidth(r, this, xNodeLoop);
						break;
					case "backgroundcolor":
						_BackgroundColor = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "backgroundgradienttype": 
						_BackgroundGradientType = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "backgroundgradientendcolor":
						_BackgroundGradientEndColor = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "backgroundimage":
						_BackgroundImage = new StyleBackgroundImage(r, this, xNodeLoop);
						break;
					case "fontstyle":
						_FontStyle = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "fontfamily":
						_FontFamily = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					case "fontsize":
						_FontSize = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "fontweight":
						_FontWeight = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "format":
						_Format =  new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					case "textdecoration":
						_TextDecoration = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "textalign":
						_TextAlign = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "verticalalign":
						_VerticalAlign = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "color":
						_Color =  new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "paddingleft":
						_PaddingLeft = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "paddingright":
						_PaddingRight = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "paddingtop":
						_PaddingTop =  new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "paddingbottom":
						_PaddingBottom = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "lineheight":
						_LineHeight = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
						break;
					case "direction":
						_Direction = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "writingmode":
						_WritingMode = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "language":
						_Language = new Expression(r, this, xNodeLoop, ExpressionType.Language);
						break;
					case "unicodebidirectional":
						_UnicodeBiDirectional = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "calendar":
						_Calendar = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "numerallanguage":
						_NumeralLanguage = new Expression(r, this, xNodeLoop, ExpressionType.Language);
						break;
					case "numeralvariant":
						_NumeralVariant = new Expression(r, this, xNodeLoop, ExpressionType.Integer);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Style element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_BorderColor != null)
				await _BorderColor.FinalPass();
			if (_BorderStyle != null)
                await _BorderStyle.FinalPass();
			if (_BorderWidth != null)
                await _BorderWidth.FinalPass();
			if (_BackgroundColor != null)
                await _BackgroundColor.FinalPass();
			if (_BackgroundGradientType != null)
                await _BackgroundGradientType.FinalPass();
			if (_BackgroundGradientEndColor != null)
                await _BackgroundGradientEndColor.FinalPass();
			if (_BackgroundImage != null)
                await _BackgroundImage.FinalPass();
			if (_FontStyle != null)
                await _FontStyle.FinalPass();
			if (_FontFamily != null)
                await _FontFamily.FinalPass();
			if (_FontSize != null)
                await _FontSize.FinalPass();
			if (_FontWeight != null)
                await _FontWeight.FinalPass();
			if (_Format != null)
                await _Format.FinalPass();
			if (_TextDecoration != null)
                await _TextDecoration.FinalPass();
			if (_TextAlign != null)
                await _TextAlign.FinalPass();
			if (_VerticalAlign != null)
                await _VerticalAlign.FinalPass();
			if (_Color != null)
                await _Color.FinalPass();
			if (_PaddingLeft != null)
                await _PaddingLeft.FinalPass();
			if (_PaddingRight != null)
                await _PaddingRight.FinalPass();
			if (_PaddingTop != null)
                await _PaddingTop.FinalPass();
			if (_PaddingBottom != null)
                await _PaddingBottom.FinalPass();
			if (_LineHeight != null)
                await _LineHeight.FinalPass();
			if (_Direction != null)
                await _Direction.FinalPass();
			if (_WritingMode != null)
                await _WritingMode.FinalPass();
			if (_Language != null)
                await _Language.FinalPass();
			if (_UnicodeBiDirectional != null)
                await _UnicodeBiDirectional.FinalPass();
			if (_Calendar != null)
                await _Calendar.FinalPass();
			if (_NumeralLanguage != null)
                await _NumeralLanguage.FinalPass();
			if (_NumeralVariant != null)
                await _NumeralVariant.FinalPass();

			_ConstantStyle = await this.IsConstant();
			return;
		}

		internal async Task DrawBackground(Report rpt, Draw2.Graphics g, Row r, Draw2.Rectangle rect)
		{
			Drawing2D.LinearGradientBrush linGrBrush = null;

			if (this.BackgroundGradientType != null &&
				this.BackgroundGradientEndColor != null &&
				this.BackgroundColor != null)
			{
				string bgt = await this.BackgroundGradientType.EvaluateString(rpt, r);
				string bgc = await this.BackgroundColor.EvaluateString(rpt, r);
				
				Draw2.Color c = XmlUtil.ColorFromHtml(bgc, Draw2.Color.White, rpt);

				string bgec = await this.BackgroundGradientEndColor.EvaluateString(rpt, r);
				Draw2.Color ec = XmlUtil.ColorFromHtml(bgec, Draw2.Color.White, rpt);

				switch (bgt)
				{
					case "LeftRight":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.Horizontal); 
						break;
					case "TopBottom":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.Vertical); 
						break;
					case "Center":	//??
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.Horizontal); 
						break;
					case "DiagonalLeft":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.ForwardDiagonal); 
						break;
					case "DiagonalRight":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.BackwardDiagonal); 
						break;
					case "HorizontalCenter":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.Horizontal); 
						break;
					case "VerticalCenter":
						linGrBrush = new Drawing2D.LinearGradientBrush(rect, c, ec, Drawing2D.LinearGradientMode.Vertical); 
						break;
					case "None":
					default:
						break;
				}
			}

			if (linGrBrush != null)
			{
				g.FillRectangle(linGrBrush, rect);
				linGrBrush.Dispose();
			}
			else
			{
				if (this.BackgroundColor != null)
				{
					string bgc = await this.BackgroundColor.EvaluateString(rpt, r);
					Draw2.Color c = XmlUtil.ColorFromHtml(bgc, Draw2.Color.White, rpt);

					using (Draw2.SolidBrush sb = new Draw2.SolidBrush(c)) {
						g.FillRectangle(sb, rect);
					}
				}
			}
		}
 
		internal async Task DrawBackgroundCircle(Report rpt, Draw2.Graphics g, Row r, Draw2.Rectangle rect)
		{
			// Don't use the gradient in this case (since it won't match) the rest of the 
			//    background.  (Routine is only used by ChartPie in the doughnut case.)
			if (this.BackgroundColor != null)
			{
				string bgc = await this.BackgroundColor.EvaluateString(rpt, r);
				Draw2.Color c = XmlUtil.ColorFromHtml(bgc, Draw2.Color.White, rpt);

				using (Draw2.SolidBrush sb = new Draw2.SolidBrush(c)) {
					g.FillEllipse(sb, rect);
					g.DrawEllipse(Draw2.Pens.Black, rect);
				}
			}
		}

		// Draw a border using the current style
		internal async Task DrawBorder(Report rpt, Draw2.Graphics g, Row r, Draw2.Rectangle rect)
		{
			if (this.BorderStyle == null)
				return;

			StyleBorderStyle bs = this.BorderStyle;

			// Create points for each part of rectangular border
			Draw2.Point tl = new Draw2.Point(rect.Left, rect.Top);
			Draw2.Point tr = new Draw2.Point(rect.Right, rect.Top);
			Draw2.Point bl = new Draw2.Point(rect.Left, rect.Bottom);
			Draw2.Point br = new Draw2.Point(rect.Right, rect.Bottom);
			// Determine characteristics for each line to be drawn
			BorderStyleEnum topBS, bottomBS, leftBS, rightBS;
			topBS = bottomBS = leftBS = rightBS = BorderStyleEnum.None;
			string v;			// temporary work value
			if (BorderStyle != null)
			{
				if (BorderStyle.Default != null)
				{
					v = await BorderStyle.Default.EvaluateString(rpt, r);
					topBS = bottomBS = leftBS = rightBS = StyleBorderStyle.GetBorderStyle(v, BorderStyleEnum.None);
				}
				if (BorderStyle.Top != null)
				{
					v = await BorderStyle.Top.EvaluateString(rpt, r);
					topBS = StyleBorderStyle.GetBorderStyle(v, topBS);
				}
				if (BorderStyle.Bottom != null)
				{
					v = await BorderStyle.Bottom.EvaluateString(rpt, r);
					bottomBS = StyleBorderStyle.GetBorderStyle(v, bottomBS);
				}
				if (BorderStyle.Left != null)
				{
					v = await BorderStyle.Left.EvaluateString(rpt, r);
					leftBS = StyleBorderStyle.GetBorderStyle(v, leftBS);
				}
				if (BorderStyle.Right != null)
				{
					v = await BorderStyle.Right.EvaluateString(rpt, r);
					rightBS = StyleBorderStyle.GetBorderStyle(v, rightBS);
				}
			}

			Draw2.Color topColor, bottomColor, leftColor, rightColor;
			topColor = bottomColor = leftColor = rightColor = Draw2.Color.Black;
			if (BorderColor != null)
			{
				if (BorderColor.Default != null)
				{
					v = await BorderColor.Default.EvaluateString(rpt, r);
					topColor = bottomColor = leftColor = rightColor = 
						XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
				}
				if (BorderColor.Top != null)
				{
					v = await BorderColor.Top.EvaluateString(rpt, r);
					topColor = XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
				}
				if (BorderColor.Bottom != null)
				{
					v = await BorderColor.Bottom.EvaluateString(rpt, r);
					bottomColor = XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
				}
				if (BorderColor.Left != null)
				{
					v = await BorderColor.Left.EvaluateString(rpt, r);
					leftColor = XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
				}
				if (BorderColor.Right != null)
				{
					v = await BorderColor.Right.EvaluateString(rpt, r);
					rightColor = XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
				}
			}

			int topWidth, bottomWidth, leftWidth, rightWidth;
			topWidth = bottomWidth = leftWidth = rightWidth = 1;
			if (BorderWidth != null)
			{
				if (BorderWidth.Default != null)
				{
					topWidth = bottomWidth = leftWidth = rightWidth = (int) new RSize(this.OwnerReport, await BorderWidth.Default.EvaluateString(rpt, r)).PixelsX;
				}
				if (BorderWidth.Top != null)
				{
					topWidth = (int) new RSize(this.OwnerReport, await BorderWidth.Top.EvaluateString(rpt, r)).PixelsX;
				}
				if (BorderWidth.Bottom != null)
				{
					bottomWidth = (int) new RSize(this.OwnerReport, await BorderWidth.Bottom.EvaluateString(rpt, r)).PixelsX;
				}
				if (BorderWidth.Left != null)
				{
					leftWidth = (int) new RSize(this.OwnerReport, await BorderWidth.Left.EvaluateString(rpt, r)).PixelsY;
				}
				if (BorderWidth.Right != null)
				{
					rightWidth = (int) new RSize(this.OwnerReport, await BorderWidth.Right.EvaluateString(rpt, r)).PixelsY;
				}
			}

			// top line
			if (topBS != BorderStyleEnum.None)
			{
				using (Draw2.Brush b = new Draw2.SolidBrush(topColor))
				using (Draw2.Pen p = new Draw2.Pen(b, topWidth))
				{
					DrawBorderDashStyle(p, topBS);
					g.DrawLine(p, tl, tr);
				}
			}
			// right line
			if (rightBS != BorderStyleEnum.None)
			{
				using (Draw2.Brush b = new Draw2.SolidBrush(rightColor))
				using (Draw2.Pen p = new Draw2.Pen(b, rightWidth))
				{
					DrawBorderDashStyle(p, rightBS);
					g.DrawLine(p, tr, br);
				}
			}
			// bottom line
			if (bottomBS != BorderStyleEnum.None)
			{
				using (Draw2.Brush b = new Draw2.SolidBrush(bottomColor))
				using (Draw2.Pen p = new Draw2.Pen(b, bottomWidth))
				{
					DrawBorderDashStyle(p, bottomBS);
					g.DrawLine(p, br, bl);
				}
			}
			// left line
			if (leftBS != BorderStyleEnum.None)
			{
				using (Draw2.Brush b = new Draw2.SolidBrush(leftColor))
				using (Draw2.Pen p = new Draw2.Pen(b, leftWidth))
				{
					DrawBorderDashStyle(p, leftBS);
					g.DrawLine(p, bl, tl);
				}
			}
		}

		private void DrawBorderDashStyle(Draw2.Pen p, BorderStyleEnum bs)
		{
			switch (bs)
			{
				case BorderStyleEnum.Dashed:
					p.DashStyle = Drawing2D.DashStyle.Dash;
					break;
				case BorderStyleEnum.Dotted:
					p.DashStyle = Drawing2D.DashStyle.Dot;
					break;
				case BorderStyleEnum.Double:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:	really need to create custom?
					break;
				case BorderStyleEnum.Groove:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:
					break;
				case BorderStyleEnum.Inset:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:
					break;
				case BorderStyleEnum.None:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// only happens for lines
					break;
				case BorderStyleEnum.Outset:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:
					break;
				case BorderStyleEnum.Ridge:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:
					break;
				case BorderStyleEnum.Solid:
					p.DashStyle = Drawing2D.DashStyle.Solid;
					break;
				case BorderStyleEnum.WindowInset:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// TODO:
					break;
				default:
					p.DashStyle = Drawing2D.DashStyle.Solid;		// really an error
					break;
			}
		}

		// Draw a line into the specified graphics object using the current style
		internal async Task DrawStyleLine(Report rpt, Draw2.Graphics g, Row r, Draw2.Point s, Draw2.Point e)
		{
			int width;
			Draw2.Color color;
			BorderStyleEnum bs;

			// Border Width default is used for the line width
			if (BorderWidth != null && BorderWidth.Default != null)
				width = (int) new RSize(this.OwnerReport, await BorderWidth.Default.EvaluateString(rpt, r)).PixelsX;
			else
				width = 1;

			// Border Color default is used for the line color
			if (BorderColor != null && BorderColor.Default != null)
			{
				string v = await BorderColor.Default.EvaluateString(rpt, r);
				color = XmlUtil.ColorFromHtml(v, Draw2.Color.Black, rpt);
			}
			else
				color = Draw2.Color.Black;
			
			if (BorderStyle != null && BorderStyle.Default != null)
			{
				string v = await BorderStyle.Default.EvaluateString(rpt, r);
				bs = StyleBorderStyle.GetBorderStyle(v, BorderStyleEnum.None);
			}
			else
				bs = BorderStyleEnum.Solid;

			using (var b = new Draw2.SolidBrush(color))
			using (var p = new Draw2.Pen(b, width))
			{
				DrawBorderDashStyle(p, bs);
				g.DrawLine(p, s, e);
			}
		}

		// Draw a string into the specified graphics object using the current style
		//  information
		internal async Task DrawString(Report rpt, Draw2.Graphics g, object o, TypeCode tc, Row r, Draw2.Rectangle rect)
		{
			// the string to draw
			var s = await Style.GetFormatedString(rpt, this, r, o, tc);
				
			using (Draw2.Font drawFont = await GetFont(rpt, r)) // Font we'll draw with
			using (Draw2.Brush drawBrush = await GetBrush(rpt, r)) // Brush we'll draw with
			using (Draw2.StringFormat drawFormat = await GetStringFormat(rpt, r)) // StringFormat we'll draw with
			{
				// Draw string
				drawFormat.FormatFlags |= Draw2.StringFormatFlags.NoWrap;
				g.DrawString(s, drawFont, drawBrush, rect, drawFormat);
			}
		}

		static internal void DrawStringDefaults(Draw2.Graphics g, object o, Draw2.Rectangle rect)
		{
			// Just use defaults to Create font and brush.
			using (var drawFont = new Draw2.Font("Arial", 10))
			using (var drawBrush = new Draw2.SolidBrush(Draw2.Color.Black)) 
			// Set format of string.
			using (var drawFormat = new Draw2.StringFormat())
			{
				drawFormat.Alignment = Draw2.StringAlignment.Center;

				// 06122007AJM Fixed so that long names are written vertically
				// need to add w to make slightly bigger
				Draw2.SizeF len = g.MeasureString(o.ToString() + "w", drawFont);
				if (len.Width > rect.Width)
				{
					drawFormat.FormatFlags = Draw2.StringFormatFlags.DirectionVertical;
					rect = (new Draw2.Rectangle(rect.X, rect.Y, rect.Width, (int)len.Width));
					drawFormat.Alignment = Draw2.StringAlignment.Near;
				}

				// Draw string to image
				g.DrawString(o.ToString(), drawFont, drawBrush, rect, drawFormat);
			}
		}

		// Calc size of a string with the specified graphics object using the current style
		//  information
		internal async Task<Draw2.Size> MeasureString(Report rpt, Draw2.Graphics g, object o, TypeCode tc, Row r, int maxWidth)
		{
			string s = await Style.GetFormatedString(rpt, this, r, o, tc); // the string to draw

			using (Draw2.Font drawFont = await GetFont(rpt, r)) // Font we'll draw with
			using (Draw2.StringFormat drawFormat = await GetStringFormat(rpt, r)) // StringFormat we'll draw with
			{
				// Measure string
				if (maxWidth == int.MaxValue)
					drawFormat.FormatFlags |= Draw2.StringFormatFlags.NoWrap;

				// 06122007AJM need to add w to make slightly bigger
				Draw2.SizeF ms = g.MeasureString(s + "w", drawFont, maxWidth, drawFormat);
				return new Draw2.Size((int) Math.Ceiling(ms.Width), 
					(int) Math.Ceiling(ms.Height));
			}
		}

		// Measure a string using the defaults for a Style font
		static internal async Task<Draw2.Size> MeasureStringDefaults(Report rpt, Draw2.Graphics g, object o, TypeCode tc, Row r, int maxWidth)
		{
			string s = await Style.GetFormatedString(rpt, null, r, o, tc); // the string to draw

			Draw2.Size size = Draw2.Size.Empty;
			using (Draw2.Font drawFont = new Draw2.Font("Arial", 10)) // Font we'll draw with
			using (Draw2.StringFormat drawFormat = new Draw2.StringFormat()) // StringFormat we'll draw with
			{
				drawFormat.Alignment = Draw2.StringAlignment.Near;

				// Measure string
				if (maxWidth == int.MaxValue)
					drawFormat.FormatFlags |= Draw2.StringFormatFlags.NoWrap;
                // 06122007AJM need to add w to make slightly bigger
                Draw2.SizeF ms = g.MeasureString(s + "w", drawFont, maxWidth, drawFormat);
				return new Draw2.Size((int) Math.Ceiling(ms.Width), 
					(int) Math.Ceiling(ms.Height));
			}
		}

		internal async Task<Draw2.Brush> GetBrush(Report rpt, Row r)
		{
			Draw2.Brush drawBrush;
			// Get the brush information
			if (this.Color != null)
			{
				string c = await this.Color.EvaluateString(rpt, r);
				Draw2.Color color = XmlUtil.ColorFromHtml(c, Draw2.Color.Black, rpt);
				drawBrush = new Draw2.SolidBrush(color);
			}
			else
				drawBrush = new Draw2.SolidBrush(Draw2.Color.Black);
			return drawBrush;
		}

		internal async Task<Draw2.Font> GetFont(Report rpt, Row r)
		{
			// Get the font information
			// FAMILY
			string ff;
			if (this.FontFamily != null)
				ff = await this.FontFamily.EvaluateString(rpt, r);
			else
				ff = "Arial";

			// STYLE
			Draw2.FontStyle fs = 0;
			if (this.FontStyle != null)
			{
				string fStyle = await this.FontStyle.EvaluateString(rpt, r);
				if (fStyle == "Italic")
					fs |= Draw2.FontStyle.Italic;
			}
			if (this.TextDecoration != null)
			{
				string td = await this.TextDecoration.EvaluateString(rpt, r);
				switch (td)
				{
					case "Underline":
						fs |= Draw2.FontStyle.Underline;
						break;
					case "Overline":	// Don't support this
						break;
					case "LineThrough":
						fs |= Draw2.FontStyle.Strikeout;
						break;
					case "None":
					default:
						break;
				}
			}

			// WEIGHT
			if (this.FontWeight != null)
			{
				string weight = await this.FontWeight.EvaluateString(rpt, r);
				switch(weight.ToLower())
				{
					case "bold":
					case "bolder":
					case "500":
					case "600":
					case "700":
					case "800":
					case "900":
						fs |= Draw2.FontStyle.Bold;
						break;
						// Nothing to do otherwise since we don't have finer gradations
					case "normal":
					case "lighter":
					case "100":
					case "200":
					case "300":
					case "400":
					default:
						break;
				}
			}

			// SIZE
			float size;			// Value is in points
			if (this.FontSize != null)
			{
				string lsize = await this.FontSize.EvaluateString(rpt, r);
				RSize rs = new RSize(this.OwnerReport, lsize);
				size = rs.Points;
			}
			else
				size = 10;
			
			Draw2.FontFamily fFamily = StyleInfo.GetFontFamily(ff);
			return new Draw2.Font(fFamily, size, fs);
		}
        
        internal async Task<Draw2.StringFormat> GetStringFormat(Report rpt, Row r)
        {
            return await GetStringFormat(rpt, r, Draw2.StringAlignment.Center);
        }

		internal async Task<Draw2.StringFormat> GetStringFormat(Report rpt, Row r, Draw2.StringAlignment defTextAlign)
		{
			// Set format of string.
			Draw2.StringFormat drawFormat = new Draw2.StringFormat();
			
			if (this.Direction != null)
			{
				string dir = await this.Direction.EvaluateString(rpt, r);
				if (dir == "RTL")
					drawFormat.FormatFlags |= Draw2.StringFormatFlags.DirectionRightToLeft;
			}
			if (this.WritingMode != null)
			{
				string wm = await this.WritingMode.EvaluateString(rpt, r);
				if (wm == "tb-rl")
					drawFormat.FormatFlags |= Draw2.StringFormatFlags.DirectionVertical;
			}

			if (this.TextAlign != null)
			{
				string ta = await this.TextAlign.EvaluateString(rpt, r);
				switch (ta.ToLower())
				{
					case "left":
						drawFormat.Alignment = Draw2.StringAlignment.Near;
						break;
					case "right":
						drawFormat.Alignment = Draw2.StringAlignment.Far;
						break;
					case "general":
                        drawFormat.Alignment = defTextAlign;
                        break;
                    case "center":
                    default:
						drawFormat.Alignment = Draw2.StringAlignment.Center;
						break;
				}
			}
			else
				drawFormat.Alignment = defTextAlign;

			if (this.VerticalAlign != null)
			{
				string va = await this.VerticalAlign.EvaluateString(rpt, r);
				switch (va.ToLower())
				{
					case "top":
					default:
						drawFormat.LineAlignment = Draw2.StringAlignment.Near;
						break;
					case "bottom":
						drawFormat.LineAlignment = Draw2.StringAlignment.Far;
						break;
					case "middle":
						drawFormat.LineAlignment = Draw2.StringAlignment.Center;
						break;
				}
			}
			else
				drawFormat.LineAlignment = Draw2.StringAlignment.Near;

			drawFormat.Trimming = Draw2.StringTrimming.None;
			return drawFormat;
		}

		// Generate a CSS string from the specified styles
		internal async Task<string> GetCSS(Report rpt, Row row, bool bDefaults)
		{
			WorkClass wc = GetWC(rpt);
			if (wc != null && wc.CssStyle != null)	// When CssStyle is available; style is a constant
				return wc.CssStyle;					//   The first time called bDefaults will affect all subsequant calls

			StringBuilder sb = new StringBuilder();

			if (this.Parent is Table || this.Parent is Matrix)
				sb.Append("border-collapse:collapse;");	// collapse the borders

			if (_BorderColor != null)
				sb.Append(await _BorderColor.GetCSS(rpt, row, bDefaults));
			else if (bDefaults)
				sb.Append(StyleBorderColor.GetCSSDefaults());

			if (_BorderStyle != null)
				sb.Append(await _BorderStyle.GetCSS(rpt, row, bDefaults));
			else if (bDefaults)
				sb.Append(StyleBorderStyle.GetCSSDefaults());

			if (_BorderWidth != null)
				sb.Append(await _BorderWidth.GetCSS(rpt, row, bDefaults));
			else if (bDefaults)
				sb.Append(StyleBorderWidth.GetCSSDefaults());

			if (_BackgroundColor != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "background-color:{0};", await _BackgroundColor.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("background-color:transparent;");

			if (_BackgroundImage != null)
				sb.Append(await _BackgroundImage.GetCSS(rpt, row, bDefaults));
			else if (bDefaults)
				sb.Append(StyleBackgroundImage.GetCSSDefaults());

			if (_FontStyle != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-style:{0};",await _FontStyle.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("font-style:normal;");

			if (_FontFamily != null)
			{
				var _ff = await _FontFamily.EvaluateString(rpt, row);
#if DRAWINGCOMPAT
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-family:{0};", Draw2.FontResourceLoader.GetCssFontStack(_ff));
#else
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-family:{0};", _ff);
#endif
			}
			else if (bDefaults)
#if DRAWINGCOMPAT
				sb.Append("font-family:" + Draw2.FontResourceLoader.GetCssFontStack("Arial") + ";");
#else
				sb.Append("font-family:Arial;");
#endif

			if (_FontSize != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-size:{0};", await _FontSize.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("font-size:10pt;");

			if (_FontWeight != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-weight:{0};",await _FontWeight.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("font-weight:normal;");

			if (_TextDecoration != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "text-decoration:{0};",await _TextDecoration.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("text-decoration:none;");

			if (_TextAlign != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "text-align:{0};",await _TextAlign.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("");	// no CSS default for text align

			if (_VerticalAlign != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "vertical-align:{0};",await _VerticalAlign.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("vertical-align:top;");

			if (_Color != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "color:{0};",await _Color.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("color:black;");

			if (_PaddingLeft != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-left:{0};",await _PaddingLeft.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("padding-left:0pt;");

			if (_PaddingRight != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-right:{0};",await _PaddingRight.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("padding-right:0pt;");

			if (_PaddingTop != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-top:{0};",await _PaddingTop.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("padding-top:0pt;");

			if (_PaddingBottom != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-bottom:{0};",await _PaddingBottom.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("padding-bottom:0pt;");

			if (_LineHeight != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "line-height:{0};",await _LineHeight.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("line-height:normal;");

			if (this._ConstantStyle)		// We'll only do this work once
			{								//   when all are constant
				wc.CssStyle = sb.ToString();
				return wc.CssStyle;
			}

			return sb.ToString();
		}

		// Generate an evaluated version of all the style parameters; used for page processing
		internal async Task<StyleInfo> GetStyleInfo(Report rpt, Row r)
		{
			WorkClass wc = GetWC(rpt);
			if (wc != null && wc.StyleInfo != null)		// When StyleInfo is available; style is a constant
			{
				return (StyleInfo) wc.StyleInfo.Clone();	// clone it because others can modify it after this		
			}

			StyleInfo si = new StyleInfo();

			if (this.BorderColor != null)
			{
				StyleBorderColor bc = this.BorderColor;
				si.BColorLeft = await bc.EvalLeft(rpt, r);
				si.BColorRight = await bc.EvalRight(rpt, r);
				si.BColorTop = await bc.EvalTop(rpt, r);
				si.BColorBottom = await bc.EvalBottom(rpt, r);
			}

			if (this.BorderStyle != null)
			{
				StyleBorderStyle bs = this.BorderStyle;
				si.BStyleLeft = await bs.EvalLeft(rpt, r);
				si.BStyleRight = await bs.EvalRight(rpt, r);
				si.BStyleTop = await bs.EvalTop(rpt, r);
				si.BStyleBottom = await bs.EvalBottom(rpt, r);
			}

			if (this.BorderWidth != null)
			{
				StyleBorderWidth bw = this.BorderWidth;
				si.BWidthLeft = await bw.EvalLeft(rpt, r);
				si.BWidthRight = await bw.EvalRight(rpt, r);
				si.BWidthTop = await bw.EvalTop(rpt, r);
				si.BWidthBottom = await bw.EvalBottom(rpt, r);
			}

			si.BackgroundColor = await this.EvalBackgroundColor(rpt, r);
			// When background color not specified; and reportitem part of table
			//   use the tables background color
			if (si.BackgroundColor == Draw2.Color.Empty)
			{
				ReportItem ri = this.Parent as ReportItem;
				if (ri != null)
				{
					if (ri.TC != null)
					{
						Table t = ri.TC.OwnerTable;
						if (t.Style != null)
							si.BackgroundColor = await t.Style.EvalBackgroundColor(rpt, r);
					}
				}
			}
			si.BackgroundGradientType = await this.EvalBackgroundGradientType(rpt, r);
			si.BackgroundGradientEndColor = await this.EvalBackgroundGradientEndColor(rpt, r);
			if (this._BackgroundImage != null)
			{
				si.BackgroundImage = await _BackgroundImage.GetPageImage(rpt, r);
			}
			else
				si.BackgroundImage = null;

			si.FontStyle = await this.EvalFontStyle(rpt, r);
			si.FontFamily = await this.EvalFontFamily(rpt, r);
			si.FontSize = await this.EvalFontSize(rpt, r);
			si.FontWeight = await this.EvalFontWeight(rpt, r);
			si._Format = await this.EvalFormat(rpt, r);			//(string) .NET Framework formatting string1
			si.TextDecoration = await this.EvalTextDecoration(rpt, r);
			si.TextAlign = await this.EvalTextAlign(rpt, r);
			si.VerticalAlign = await this.EvalVerticalAlign(rpt, r);
			si.Color = await this.EvalColor(rpt, r);
			si.PaddingLeft = await this.EvalPaddingLeft(rpt, r);
			si.PaddingRight = await this.EvalPaddingRight(rpt, r);
			si.PaddingTop = await this.EvalPaddingTop(rpt, r);
			si.PaddingBottom = await this.EvalPaddingBottom(rpt, r);
			si.LineHeight = await this.EvalLineHeight(rpt, r);
			si.Direction = await this.EvalDirection(rpt, r);
			si.WritingMode = await this.EvalWritingMode(rpt, r);
			si.Language = await this.EvalLanguage(rpt, r);
			si.UnicodeBiDirectional = await this.EvalUnicodeBiDirectional(rpt, r);
			si.Calendar = await this.EvalCalendar(rpt, r);
			si.NumeralLanguage = await this.EvalNumeralLanguage(rpt, r);
			si.NumeralVariant = await this.EvalNumeralVariant(rpt, r);

			if (this._ConstantStyle)		// We'll only do this work once
			{
				wc.StyleInfo = si;			//   when all are constant
				si = (StyleInfo) wc.StyleInfo.Clone();
			}

			return si;
		}

		// Format a string; passed a style but style may be null;
		static internal async Task<string> GetFormatedString(Report rpt, Style s, Row row, object o, TypeCode tc)
		{
			string t = null;
			if (o == null)
				return "";

			string format = null;
			try 
			{
                if (s != null && s.Format != null)
                {
                    format = await s.Format.EvaluateString(rpt, row);
                    if (format != null && format.Length > 0)
                    {
                        switch (tc)
                        {
                            case TypeCode.DateTime:
                                t = ((DateTime)o).ToString(format);
                                break;
                            case TypeCode.Int16:
                                t = ((short)o).ToString(format);
                                break;
                            case TypeCode.UInt16:
                                t = ((ushort)o).ToString(format);
                                break;
                            case TypeCode.Int32:
                                t = ((int)o).ToString(format);
                                break;
                            case TypeCode.UInt32:
                                t = ((uint)o).ToString(format);
                                break;
                            case TypeCode.Int64:
                                t = ((long)o).ToString(format);
                                break;
                            case TypeCode.UInt64:
                                t = ((ulong)o).ToString(format);
                                break;
                            case TypeCode.String:
                                t = (string)o;
                                break;
                            case TypeCode.Decimal:
                                t = ((decimal)o).ToString(format);
                                break;
                            case TypeCode.Single:
                                t = ((float)o).ToString(format);
                                break;
                            case TypeCode.Double:
                                t = ((double)o).ToString(format);
                                break;
                            default:
								t = (o is IFormattable fmt)
									? fmt.ToString(format, null)
									: o.ToString();
                                break;
                        }
                    }
                    else    
                        t = o.ToString();       // No format provided
                }
                else
                {   // No style provided
                    t = o.ToString();
                }
			}
			catch (Exception ex)
			{
				rpt.rl.LogError(1, string.Format("Value:{0} Format:{1} exception: {2}", o, format,
					ex.InnerException != null ? ex.InnerException.Message : ex.Message));
				t = o.ToString();       // probably type mismatch from expectation
			}
			return t;
		}

		private async Task<bool> IsConstant()
		{
			bool rc = true;

			if (_BorderColor != null)
				rc = await _BorderColor.IsConstant();

			if (!rc)
				return false;

			if (_BorderStyle != null)
				rc = await _BorderStyle.IsConstant();

			if (!rc)
				return false;

			if (_BorderWidth != null)
				rc = await _BorderWidth.IsConstant();

			if (!rc)
				return false;

			if (_BackgroundColor != null)
				rc = await _BackgroundColor.IsConstant();

			if (!rc)
				return false;

			if (_BackgroundImage != null)
				rc = await _BackgroundImage.IsConstant();

			if (!rc)
				return false;

			if (_FontStyle != null)
				rc = await _FontStyle.IsConstant();

			if (!rc)
				return false;

			if (_FontFamily != null)
				rc = await _FontFamily.IsConstant();

			if (!rc)
				return false;

			if (_FontSize != null)
				rc = await _FontSize.IsConstant();

			if (!rc)
				return false;

			if (_FontWeight != null)
				rc = await _FontWeight.IsConstant();

			if (!rc)
				return false;

			if (_TextDecoration != null)
				rc = await _TextDecoration.IsConstant();

			if (!rc)
				return false;

			if (_TextAlign != null)
				rc = await _TextAlign.IsConstant();

			if (!rc)
				return false;

			if (_VerticalAlign != null)
				rc = await _VerticalAlign.IsConstant();

			if (!rc)
				return false;

			if (_Color != null)
				rc = await _Color.IsConstant();

			if (!rc)
				return false;

			if (_PaddingLeft != null)
				rc = await _PaddingLeft.IsConstant();

			if (!rc)
				return false;

			if (_PaddingRight != null)
				rc = await _PaddingRight.IsConstant();

			if (!rc)
				return false;

			if (_PaddingTop != null)
				rc = await _PaddingTop.IsConstant();

			if (!rc)
				return false;

			if (_PaddingBottom != null)
				rc = await _PaddingBottom.IsConstant();

			if (!rc)
				return false;

			if (_LineHeight != null)
				rc = await _LineHeight.IsConstant();

			if (!rc)
				return false;

			return rc;
		}

		internal async Task<Draw2.Rectangle> PaddingAdjust(Report rpt, Row r, Draw2.Rectangle rect, bool bAddIn)
		{
			int pbottom = await this.EvalPaddingBottomPx(rpt, r);
			int ptop = await this.EvalPaddingTopPx(rpt, r);
			int pleft = await this.EvalPaddingLeftPx(rpt, r);
			int pright = await this.EvalPaddingRightPx(rpt, r);

			Draw2.Rectangle rt;
			if (bAddIn)		// add in when trying to size the object
				rt = new Draw2.Rectangle(rect.Left - pleft, rect.Top - ptop, 
					rect.Width + pleft + pright, rect.Height + ptop + pbottom);
			else			// otherwise you want the rectangle of the embedded object
				rt = new Draw2.Rectangle(rect.Left + pleft, rect.Top + ptop, 
					rect.Width - pleft - pright, rect.Height - ptop - pbottom);
			return rt;
		}

		internal StyleBorderColor BorderColor
		{
			get { return  _BorderColor; }
			set {  _BorderColor = value; }
		}

		internal StyleBorderStyle BorderStyle
		{
			get { return  _BorderStyle; }
			set {  _BorderStyle = value; }
		}

		internal StyleBorderWidth BorderWidth
		{
			get { return  _BorderWidth; }
			set {  _BorderWidth = value; }
		}

		internal Expression BackgroundColor
		{
			get { return  _BackgroundColor; }
			set {  _BackgroundColor = value; }
		}

		internal async Task<Draw2.Color> EvalBackgroundColor(Report rpt, Row row)
		{
			if (_BackgroundColor == null)
				return Draw2.Color.Empty;

			string c = await _BackgroundColor.EvaluateString(rpt, row);
			return XmlUtil.ColorFromHtml(c, Draw2.Color.Empty, rpt);
		}

		internal Expression BackgroundGradientType
		{
			get { return  _BackgroundGradientType; }
			set {  _BackgroundGradientType = value; }
		}

		internal async Task<BackgroundGradientTypeEnum> EvalBackgroundGradientType(Report rpt, Row r)
		{
			if (_BackgroundGradientType == null)
				return BackgroundGradientTypeEnum.None;

			string bgt = await _BackgroundGradientType.EvaluateString(rpt, r);
			return 	StyleInfo.GetBackgroundGradientType(bgt, BackgroundGradientTypeEnum.None);
		}

		internal Expression BackgroundGradientEndColor
		{
			get { return  _BackgroundGradientEndColor; }
			set {  _BackgroundGradientEndColor = value; }
		}

		internal async Task<Draw2.Color> EvalBackgroundGradientEndColor(Report rpt, Row r)
		{
			if (_BackgroundGradientEndColor == null)
				return Draw2.Color.Empty;

			string c = await _BackgroundGradientEndColor.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, Draw2.Color.Empty, rpt);
		}

		internal StyleBackgroundImage BackgroundImage
		{
			get { return  _BackgroundImage; }
			set {  _BackgroundImage = value; }
		}

		internal bool ConstantStyle
		{
			get { return _ConstantStyle; }
		}

		internal Expression FontStyle
		{
			get { return  _FontStyle; }
			set {  _FontStyle = value; }
		}

		internal async Task<bool> IsFontItalic(Report rpt, Row r)
		{
			if (await EvalFontStyle(rpt, r) == FontStyleEnum.Italic)
				return true;

			return false;
		}

		internal async Task<FontStyleEnum> EvalFontStyle(Report rpt, Row row)
		{
			if (_FontStyle == null)
				return FontStyleEnum.Normal;

			string fs = await _FontStyle.EvaluateString(rpt, row);
			return StyleInfo.GetFontStyle(fs, FontStyleEnum.Normal);
		}

		internal Expression FontFamily
		{
			get { return  _FontFamily; }
			set {  _FontFamily = value; }
		}

		internal async Task<string> EvalFontFamily(Report rpt, Row row)
		{
			if (_FontFamily == null)
				return "Arial";

			return await _FontFamily.EvaluateString(rpt, row);
		}

		internal Expression FontSize
		{
			get { return  _FontSize; }
			set {  _FontSize = value; }
		}

		internal async Task<float> EvalFontSize(Report rpt, Row row)
		{
			if (_FontSize == null)
				return 10;

			string pts;
			pts = await _FontSize.EvaluateString(rpt, row);
			RSize sz = new RSize(this.OwnerReport, pts);

			return sz.Points;
		}

		internal Expression FontWeight
		{
			get { return  _FontWeight; }
			set {  _FontWeight = value; }
		}

		internal async Task<FontWeightEnum> EvalFontWeight(Report rpt, Row row)
		{
			if (_FontWeight == null)
				return FontWeightEnum.Normal;

			string weight = await this.FontWeight.EvaluateString(rpt, row);
			return StyleInfo.GetFontWeight(weight, FontWeightEnum.Normal);
		}

		internal async Task<bool> IsFontBold(Report rpt, Row r)
		{
			if (this.FontWeight == null)
				return false;

			string weight = await this.FontWeight.EvaluateString(rpt, r);
			switch(weight.ToLower())
			{
				case "bold":
				case "bolder":
				case "500":
				case "600":
				case "700":
				case "800":
				case "900":
					return true;
				default:
					return false;
			}
		}

		internal Expression Format
		{
			get { return  _Format; }
			set {  _Format = value; }
		}

        internal async Task<string> EvalFormat(Report rpt, Row row)
        {
            if (_Format == null)
                return "General";
            
            string f = await _Format.EvaluateString(rpt, row);

            if (f == null || f.Length == 0)
                return "General";
            return f;
        }

        
        
		internal Expression TextDecoration
		{
			get { return  _TextDecoration; }
			set {  _TextDecoration = value; }
		}

		internal async Task<TextDecorationEnum> EvalTextDecoration(Report rpt, Row r)
		{
			if (_TextDecoration == null)
				return TextDecorationEnum.None;

			string td = await _TextDecoration.EvaluateString(rpt, r);
			return StyleInfo.GetTextDecoration(td, TextDecorationEnum.None);
		}

		internal Expression TextAlign
		{
			get { return  _TextAlign; }
			set {  _TextAlign = value; }
		}

		internal async Task<TextAlignEnum> EvalTextAlign(Report rpt, Row row)
		{
			if (_TextAlign == null)
				return TextAlignEnum.General;
	
			string a = await _TextAlign.EvaluateString(rpt, row);
			return StyleInfo.GetTextAlign(a, TextAlignEnum.General);
		}

		internal Expression VerticalAlign
		{
			get { return  _VerticalAlign; }
			set {  _VerticalAlign = value; }
		}

		internal async Task<VerticalAlignEnum> EvalVerticalAlign(Report rpt, Row row)
		{
			if (_VerticalAlign == null)
				return VerticalAlignEnum.Top;

			string v = await _VerticalAlign.EvaluateString(rpt, row);
			return StyleInfo.GetVerticalAlign(v, VerticalAlignEnum.Top);
		}

		internal Expression Color
		{
			get { return  _Color; }
			set {  _Color = value; }
		}

		internal async Task<Draw2.Color> EvalColor(Report rpt, Row row)
		{
			if (_Color == null)
				return Draw2.Color.Black;

			string c = await _Color.EvaluateString(rpt, row);
			return XmlUtil.ColorFromHtml(c, Draw2.Color.Black, rpt);
		}

		internal Expression PaddingLeft
		{
			get { return  _PaddingLeft; }
			set {  _PaddingLeft = value; }
		}

		internal async Task<float> EvalPaddingLeft(Report rpt, Row row)
		{
			if (_PaddingLeft == null)
				return 0;

			string v = await _PaddingLeft.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.Points;
		}

		internal async Task<int> EvalPaddingLeftPx(Report rpt, Row row)
		{
			if (_PaddingLeft == null)
				return 0;

			string v = await _PaddingLeft.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.PixelsX;
		}

		internal Expression PaddingRight
		{
			get { return  _PaddingRight; }
			set {  _PaddingRight = value; }
		}

		internal async Task<float> EvalPaddingRight(Report rpt, Row row)
		{
			if (_PaddingRight == null)
				return 0;

			string v = await _PaddingRight.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.Points;
		}

		internal async Task<int> EvalPaddingRightPx(Report rpt, Row row)
		{
			if (_PaddingRight == null)
				return 0;

			string v = await _PaddingRight.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.PixelsX;
		}

		internal Expression PaddingTop
		{
			get { return  _PaddingTop; }
			set {  _PaddingTop = value; }
		}

		internal async Task<float> EvalPaddingTop(Report rpt, Row row)
		{
			if (_PaddingTop == null)
				return 0;

			string v = await _PaddingTop.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.Points;
		}

		internal async Task<int> EvalPaddingTopPx(Report rpt, Row row)
		{
			if (_PaddingTop == null)
				return 0;

			string v = await _PaddingTop.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.PixelsY;
		}

		internal Expression PaddingBottom
		{
			get { return  _PaddingBottom; }
			set {  _PaddingBottom = value; }
		}

		internal async Task<float> EvalPaddingBottom(Report rpt, Row row)
		{
			if (_PaddingBottom == null)
				return 0;

			string v = await _PaddingBottom.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.Points;
		}

		internal async Task<int> EvalPaddingBottomPx(Report rpt, Row row)
		{
			if (_PaddingBottom == null)
				return 0;

			string v = await _PaddingBottom.EvaluateString(rpt, row);
			RSize rz = new RSize(OwnerReport, v);
			return rz.PixelsY;
		}

		internal Expression LineHeight
		{
			get { return  _LineHeight; }
			set {  _LineHeight = value; }
		}

		internal async Task<float> EvalLineHeight(Report rpt, Row r)
		{
			if (_LineHeight == null)
				return float.NaN;

			string sz = await _LineHeight.EvaluateString(rpt, r);
			RSize rz = new RSize(OwnerReport, sz);
			return rz.Points;
		}

		internal Expression Direction
		{
			get { return  _Direction; }
			set {  _Direction = value; }
		}

		internal async Task<DirectionEnum> EvalDirection(Report rpt, Row r)
		{
			if (_Direction == null)
				return DirectionEnum.LTR;

			string d = await _Direction.EvaluateString(rpt, r);
			return StyleInfo.GetDirection(d, DirectionEnum.LTR);
		}

		internal Expression WritingMode
		{
			get { return  _WritingMode; }
			set {  _WritingMode = value; }
		}

		internal async Task<WritingModeEnum> EvalWritingMode(Report rpt, Row r)
		{
			if (_WritingMode == null)
				return WritingModeEnum.lr_tb;

			string w = await _WritingMode.EvaluateString(rpt, r);

			return StyleInfo.GetWritingMode(w, WritingModeEnum.lr_tb ); 
		}

		internal Expression Language
		{
			get { return  _Language; }
			set {  _Language = value; }
		}

		internal async Task<string> EvalLanguage(Report rpt, Row r)
		{
			if (_Language == null)
				return await OwnerReport.EvalLanguage(rpt, r);

			return await _Language.EvaluateString(rpt, r);
		}

		internal Expression UnicodeBiDirectional
		{
			get { return  _UnicodeBiDirectional; }
			set {  _UnicodeBiDirectional = value; }
		}

		internal async Task<UnicodeBiDirectionalEnum> EvalUnicodeBiDirectional(Report rpt, Row r)
		{
			if (_UnicodeBiDirectional == null)
				return UnicodeBiDirectionalEnum.Normal;

			string u = await _UnicodeBiDirectional.EvaluateString(rpt, r);
			return StyleInfo.GetUnicodeBiDirectional(u, UnicodeBiDirectionalEnum.Normal);
		}

		internal Expression Calendar
		{
			get { return  _Calendar; }
			set {  _Calendar = value; }
		}

		internal async Task<CalendarEnum> EvalCalendar(Report rpt, Row r)
		{
			if (_Calendar == null)
				return CalendarEnum.Gregorian;

			string c = await _Calendar.EvaluateString(rpt, r);
			return StyleInfo.GetCalendar(c, CalendarEnum.Gregorian);
		}

		internal Expression NumeralLanguage
		{
			get { return  _NumeralLanguage; }
			set {  _NumeralLanguage = value; }
		}

		internal async Task<string> EvalNumeralLanguage(Report rpt, Row r)
		{
			if (_NumeralLanguage == null)
				return await EvalLanguage(rpt, r);

			return await _NumeralLanguage.EvaluateString(rpt, r);
		}

		internal Expression NumeralVariant
		{
			get { return  _NumeralVariant; }
			set {  _NumeralVariant = value; }
		}

		internal async Task<int> EvalNumeralVariant(Report rpt, Row r)
		{
			if (_NumeralVariant == null)
				return 1;

			int v = (int)await _NumeralVariant.EvaluateDouble(rpt, r);
			if (v < 1 || v > 7)		// correct for bad data
				v = 1;
			return v;
		}

		private WorkClass GetWC(Report rpt)
		{
			if (!this.ConstantStyle)
				return null;

			WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
			if (wc == null)
			{
				wc = new WorkClass();
				rpt.Cache.Add(this, "wc", wc);
			}
			return wc;
		}

		private void RemoveWC(Report rpt)
		{
			rpt.Cache.Remove(this, "wc");
		}

		class WorkClass
		{
			internal string CssStyle;		// When ConstantStyle is true; this will hold cache of css
			internal StyleInfo StyleInfo;	// When ConstantStyle is true; this will hold cache of StyleInfo
			internal WorkClass()
			{
				CssStyle = null;
				StyleInfo = null;
			}
		}

	}
}
