

using System;
using System.Xml;
using System.IO;
using System.Text;
#if DRAWINGCOMPAT
using Draw = Majorsilence.Drawing;
using Drawing2D = Majorsilence.Drawing.Drawing2D;
#else
using Draw = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
#endif


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// StyleInfo (borders, fonts, background, padding, ...)
	///</summary>
	public class StyleInfo: ICloneable
	{
		// note: all sizes are expressed as points
		// _BorderColor
		/// <summary>
		/// Color of the left border
		/// </summary>
		public Draw.Color BColorLeft;		// (Color) Color of the left border
		/// <summary>
		/// Color of the right border
		/// </summary>
		public Draw.Color BColorRight;		// (Color) Color of the right border
		/// <summary>
		/// Color of the top border
		/// </summary>
		public Draw.Color BColorTop;		// (Color) Color of the top border
		/// <summary>
		/// Color of the bottom border
		/// </summary>
		public Draw.Color BColorBottom;	// (Color) Color of the bottom border
		// _BorderStyle
		/// <summary>
		/// Style of the left border
		/// </summary>
		public BorderStyleEnum BStyleLeft;	// (Enum BorderStyle) Style of the left border
		/// <summary>
		/// Style of the left border
		/// </summary>
		public BorderStyleEnum BStyleRight;	// (Enum BorderStyle) Style of the left border
		/// <summary>
		/// Style of the top border
		/// </summary>
		public BorderStyleEnum BStyleTop;		// (Enum BorderStyle) Style of the top border
		/// <summary>
		/// Style of the bottom border
		/// </summary>
		public BorderStyleEnum BStyleBottom;	// (Enum BorderStyle) Style of the bottom border
		// _BorderWdith
		/// <summary>
		/// Width of the left border. Max: 20 pt Min: 0.25 pt
		/// </summary>
		public float BWidthLeft;	//(Size) Width of the left border. Max: 20 pt Min: 0.25 pt
		/// <summary>
		/// Width of the right border. Max: 20 pt Min: 0.25 pt
		/// </summary>
		public float BWidthRight;	//(Size) Width of the right border. Max: 20 pt Min: 0.25 pt
		/// <summary>
		/// Width of the right border. Max: 20 pt Min: 0.25 pt
		/// </summary>
		public float BWidthTop;		//(Size) Width of the right border. Max: 20 pt Min: 0.25 pt
		/// <summary>
		/// Width of the bottom border. Max: 20 pt Min: 0.25 pt
		/// </summary>
		public float BWidthBottom;	//(Size) Width of the bottom border. Max: 20 pt Min: 0.25 pt

		/// <summary>
		/// Color of the background
		/// </summary>
		public Draw.Color BackgroundColor;			//(Color) Color of the background
        public string BackgroundColorText;		//(Textual Color) Color of the background
        /// <summary>
		/// The type of background gradient
		/// </summary>
		public BackgroundGradientTypeEnum BackgroundGradientType;	// The type of background gradient
		/// <summary>
		/// End color for the background gradient.
		/// </summary>
		
		/// <summary>
		/// The type of background pattern
		/// </summary>
		public patternTypeEnum PatternType;
		
		public Draw.Color BackgroundGradientEndColor;	//(Color) End color for the background gradient.
		/// <summary>
		/// A background image for the report item.
		/// </summary>
		public PageImage BackgroundImage;	// A background image for the report item.
		/// <summary>
		/// Font style Default: Normal
		/// </summary>
		public FontStyleEnum FontStyle;		// (Enum FontStyle) Font style Default: Normal
		/// <summary>
		/// Name of the font family Default: Arial
		/// </summary>
		private string _FontFamily;			//(string)Name of the font family Default: Arial -- allow comma separated value?
		/// <summary>
		/// Point size of the font
		/// </summary>
		public float FontSize;		//(Size) Point size of the font
		/// <summary>
		/// Thickness of the font
		/// </summary>
		public FontWeightEnum FontWeight;		//(Enum FontWeight) Thickness of the font
        /// <summary>
        /// Cell format in Excel07  Default: General
        /// </summary>
		public string _Format;			//WRP 28102008 Cell format string
		/// <summary>
		/// Special text formatting Default: none
		/// </summary>
		public TextDecorationEnum TextDecoration;	// (Enum TextDecoration) Special text formatting Default: none
		/// <summary>
		/// Horizontal alignment of the text Default: General
		/// </summary>
		public TextAlignEnum TextAlign;		// (Enum TextAlign) Horizontal alignment of the text Default: General
		/// <summary>
		/// Vertical alignment of the text Default: Top
		/// </summary>
		public VerticalAlignEnum VerticalAlign;	// (Enum VerticalAlign)	Vertical alignment of the text Default: Top
		/// <summary>
		/// The foreground color	Default: Black
		/// </summary>
		public Draw.Color Color;			// (Color) The foreground color	Default: Black
        public string ColorText;    // (Color-text)
        /// <summary>
		/// Padding between the left edge of the report item.
		/// </summary>
		public float PaddingLeft;	// (Size)Padding between the left edge of the report item.
		/// <summary>
		/// Padding between the right edge of the report item.
		/// </summary>
		public float PaddingRight;	// (Size) Padding between the right edge of the report item.
		/// <summary>
		/// Padding between the top edge of the report item.
		/// </summary>
		public float PaddingTop;		// (Size) Padding between the top edge of the report item.
		/// <summary>
		/// Padding between the bottom edge of the report item.
		/// </summary>
		public float PaddingBottom;	// (Size) Padding between the bottom edge of the report item.
		/// <summary>
		/// Height of a line of text.
		/// </summary>
		public float LineHeight;		// (Size) Height of a line of text
		/// <summary>
		/// Indicates whether text is written left-to-right (default)
		/// </summary>
		public DirectionEnum Direction;		// (Enum Direction) Indicates whether text is written left-to-right (default)
		/// <summary>
		/// Indicates the writing mode; e.g. left right top bottom or top bottom left right.
		/// </summary>
		public WritingModeEnum WritingMode;	// (Enum WritingMode) Indicates whether text is written
		/// <summary>
		/// The primary language of the text.
		/// </summary>
		public string Language;		// (Language) The primary language of the text.
		/// <summary>
		/// Unused.
		/// </summary>
		public UnicodeBiDirectionalEnum UnicodeBiDirectional;	// (Enum UnicodeBiDirection) 
		/// <summary>
		/// Calendar to use.
		/// </summary>
		public CalendarEnum Calendar;		// (Enum Calendar)
		/// <summary>
		/// The digit format to use.
		/// </summary>
		public string NumeralLanguage;	// (Language) The digit format to use as described by its
		/// <summary>
		/// The variant of the digit format to use.
		/// </summary>
		public int NumeralVariant;	//(Integer) The variant of the digit format to use.

		/// <summary>
		/// Constructor using all defaults for the style.
		/// </summary>
		public StyleInfo()
		{
			BColorLeft = BColorRight = BColorTop = BColorBottom = Draw.Color.Black;	// (Color) Color of the bottom border
			BStyleLeft = BStyleRight = BStyleTop = BStyleBottom = BorderStyleEnum.None;
			// _BorderWdith
			BWidthLeft = BWidthRight = BWidthTop = BWidthBottom = 1;

			BackgroundColor = Draw.Color.Empty;
            BackgroundColorText = string.Empty;
			BackgroundGradientType = BackgroundGradientTypeEnum.None;
			BackgroundGradientEndColor = Draw.Color.Empty;
			BackgroundImage = null;

			FontStyle = FontStyleEnum.Normal;
			_FontFamily = "Arial";
            //WRP 291008 numFmtId should be 0 (Zero) for General format - will be interpreted as a string
            //It has default values in Excel07 as per ECMA-376 standard (SEction 3.8.30) for Office Open XML Excel07
            _Format = "General";  
			FontSize = 10;
			FontWeight = FontWeightEnum.Normal;

			PatternType = patternTypeEnum.None;
			TextDecoration = TextDecorationEnum.None;
			TextAlign = TextAlignEnum.General;
			VerticalAlign = VerticalAlignEnum.Top;
			Color = Draw.Color.Black;
            ColorText = "Black";
            PaddingLeft = PaddingRight = PaddingTop = PaddingBottom = 0;
			LineHeight = 0;
			Direction = DirectionEnum.LTR;
			WritingMode = WritingModeEnum.lr_tb;
			Language = "en-US";
			UnicodeBiDirectional = UnicodeBiDirectionalEnum.Normal;
			Calendar = CalendarEnum.Gregorian;
			NumeralLanguage = Language;
			NumeralVariant=1;
		}
		/// <summary>
		/// Name of the font family Default: Arial
		/// </summary>
		public string FontFamily
		{
			get
			{
				int i = _FontFamily.IndexOf(",");
				return i > 0? _FontFamily.Substring(0, i): _FontFamily;
			}
			set { _FontFamily = value; }
		}
		/// <summary>
		/// Name of the font family Default: Arial.  Support list of families separated by ','.
		/// </summary>
		public string FontFamilyFull
		{
			get {return _FontFamily;}
		}
		/// <summary>
		/// Gets the FontFamily instance using the FontFamily string.  This supports lists of fonts.
		/// </summary>
		/// <returns></returns>
		public Draw.FontFamily GetFontFamily()
		{
			return GetFontFamily(_FontFamily);
		}

		/// <summary>
		/// Gets the FontFamily instance using the passed face name.  This supports lists of fonts.
		/// </summary>
		/// <returns></returns>
		static public Draw.FontFamily GetFontFamily(string fface)
		{
			string[] choices = fface.Split(',');
			Draw.FontFamily ff=null;
			foreach (string val in choices)
			{
				try 
				{
					string font=null;
					// TODO: should be better way than to hard code; could put in config file??
					switch (val.Trim().ToLower())
					{
						case "serif":
							font = "Times New Roman";
							break;
						case "sans-serif":
							font = "Arial";
							break;
						case "cursive":
							font = "Comic Sans MS";
							break;
						case "fantasy":
							font = "Impact";
							break;
						case "monospace":
                        case "courier":
							font = "Courier New";
							break;
						default:
							font = val;
							break;
					}
					ff = new Draw.FontFamily(font);
					if (ff != null)
						break;
				}
				catch {}	// if font doesn't exist we will go to the next
			}
			if (ff == null)
				ff = new Draw.FontFamily("Arial");
			return ff;
		}
		/// <summary>
		/// True if font is bold.
		/// </summary>
		/// <returns></returns>
		public bool IsFontBold()
		{
			switch(FontWeight)
			{
				case FontWeightEnum.Bold:
				case FontWeightEnum.Bolder:
				case FontWeightEnum.W500:
				case FontWeightEnum.W600:
				case FontWeightEnum.W700:
				case FontWeightEnum.W800:
				case FontWeightEnum.W900:
					return true;
				default:
					return false;
			}
		}


        public static string ToUpperFirstLetter(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }
            // convert to char array of the string
            char[] letters = source.ToCharArray();
            // upper case the first char
            letters[0] = char.ToUpper(letters[0]);
            // return the array made of the new char array
            return new string(letters);
        }

		/// <summary>
		/// Gets the enumerated font weight.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		static public FontWeightEnum GetFontWeight(string v, FontWeightEnum def)
		{
			FontWeightEnum fw;

            try
            {
                fw = (FontWeightEnum)System.Enum.Parse(typeof(FontWeightEnum), ToUpperFirstLetter(v));
            }
            catch
            {
                fw = def;
            }
            return fw; 
		}

		/// <summary>
		/// Returns the font style (normal or italic).
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static FontStyleEnum GetFontStyle(string v, FontStyleEnum def)
		{
			FontStyleEnum f;
            try
            {
                f = (FontStyleEnum)Enum.Parse(typeof(FontStyleEnum), v);
            }
            catch
            {
                f = def;
            }

            return f; 
		}

		/// <summary>
		/// Gets the background gradient type.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		static public BackgroundGradientTypeEnum GetBackgroundGradientType(string v, BackgroundGradientTypeEnum def)
		{
			BackgroundGradientTypeEnum gt;
            try
            {
                gt = (BackgroundGradientTypeEnum)Enum.Parse(typeof(BackgroundGradientTypeEnum), v);
            }
            catch
            {
                gt = def;
            }
            return gt; 

		}

		/// <summary>
		/// Gets the text decoration.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static TextDecorationEnum GetTextDecoration(string v, TextDecorationEnum def)
		{
			TextDecorationEnum td;
            try
            {
                td = (TextDecorationEnum)Enum.Parse(typeof(TextDecorationEnum), v);
            }
            catch
            {
                td = def;
            }

            return td; 
		}

		/// <summary>
		/// Gets the text alignment.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static TextAlignEnum GetTextAlign(string v, TextAlignEnum def)
		{
			TextAlignEnum ta;
            try
            {

                ta = (TextAlignEnum)Enum.Parse(typeof(TextAlignEnum), System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(v));
            }
            catch
            {
                ta = def;
            }
            return ta; 

		}

		/// <summary>
		/// Gets the vertical alignment.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static VerticalAlignEnum GetVerticalAlign(string v, VerticalAlignEnum def)
		{
			VerticalAlignEnum va;
            try
            {
                va = (VerticalAlignEnum)Enum.Parse(typeof(VerticalAlignEnum), v);
            }
            catch
            {
                va = def;
            }

            return va;
 
		}

		/// <summary>
		/// Gets the direction of the text.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static DirectionEnum GetDirection(string v, DirectionEnum def)
		{
			DirectionEnum d;
            try
            {
                d = (DirectionEnum)Enum.Parse(typeof(DirectionEnum), v);
            }
            catch
            {
                d = def;
            }
            return d; 


		}
		/// <summary>
		/// Gets the writing mode; e.g. left right top bottom or top bottom left right.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static WritingModeEnum GetWritingMode(string v, WritingModeEnum def)
		{
			WritingModeEnum w;

            try
            {
                if (v == "rl-tb" || v == "tb-rl")
                { // How the hell did it ever get saved as rl-tb?
                    v = "tb_rl";
                }
                else if (v == "lr-tb")
                {
                    v = "lr_tb";
                }
                else if (v == "tb-lr")
                { 
                    v = "tb_lr";
                }
                else if (v == "rl-bt")
                {
                    v = "rl_bt";
                }
                w = (WritingModeEnum)Enum.Parse(typeof(WritingModeEnum), v);
            }
            catch
            {
                w = def;
            }

            return w;
 
		}

		/// <summary>
		/// Gets the unicode BiDirectional.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static UnicodeBiDirectionalEnum GetUnicodeBiDirectional(string v, UnicodeBiDirectionalEnum def)
		{
			UnicodeBiDirectionalEnum u;
            try
            {
                u = (UnicodeBiDirectionalEnum)Enum.Parse(typeof(UnicodeBiDirectionalEnum), v);
            }
            catch
            {
                u = def;
            }
            return u; 

		}

		/// <summary>
		/// Gets the calendar (e.g. Gregorian, GregorianArabic, and so on)
		/// </summary>
		/// <param name="v"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		public static CalendarEnum GetCalendar(string v, CalendarEnum def)
		{
			CalendarEnum c;

            try
            {
                c = (CalendarEnum)Enum.Parse(typeof(CalendarEnum), v);
            }
            catch
            {
                c = def;
            }
            return c; 
		}
        // WRP 301008 return Excel07 format code as defined in section 3.8.30 of the ECMA-376 standard for Office Open XML Excel07 file formats
        public static int GetFormatCode (string val)
        {
            switch (val)
            {
                case "General":
                    return 0;
                case "0":
                    return 1;
                case "0.00":
                    return 2;
                case "#,##0":
                    return 3;
                case "#,##0.00":
                    return 4;
                case "0%":
                    return 9;
                case "p":
                case "P":
                case "0.00%":
                    return 10;
                case "0.00E+00":
                    return 11;
                case "# ?/?":
                    return 12;
                case " # ??/??":
                    return 13;
                case "mm-dd-yy":
                    return 14;
                case "d-mmm-yy":
                    return 15;
                case "d-mmm":
                    return 16;
                case "mmm-yy":
                    return 17;
                case "h:mm AM/PM":
                    return 18;
                case "h:mm:ss AM/PM":
                    return 19;
                case "h:mm":
                    return 20;
                case "h:mm:ss":
                    return 21;
                case "m/d/yy h:mm":
                    return 22;
                case "#,##0 ;(#,##0)":
                    return 37;
                case "#,##0 ;[Red](#,##0)":
                    return 38;
                case "#,##0.00;(#,##0.00)":
                    return 39;
                case "#,##0.00;[Red](#,##0.00)":
                    return 40;
                case "mm:ss":
                    return 45;
                case "[h]:mm:ss":
                    return 46;
                case "mmss.0":
                    return 47;
                case "##0.0E+0":
                    return 48;
                case "@":
                    return 49;
                default:
                    return 999;

            }
        }
		
		public static patternTypeEnum GetPatternType(Drawing2D.HatchStyle hs)
		{
			switch (hs)
			{
				case Drawing2D.HatchStyle.BackwardDiagonal:
					return patternTypeEnum.BackwardDiagonal;					
				case Drawing2D.HatchStyle.Cross:
					return patternTypeEnum.Cross;				
				case Drawing2D.HatchStyle.DarkDownwardDiagonal:
					return patternTypeEnum.DarkDownwardDiagonal;
				case Drawing2D.HatchStyle.DarkHorizontal:
					return patternTypeEnum.DarkHorizontal;
				case Drawing2D.HatchStyle.Vertical:
					return patternTypeEnum.Vertical;
				case Drawing2D.HatchStyle.LargeConfetti:
					return patternTypeEnum.LargeConfetti;
				case Drawing2D.HatchStyle.OutlinedDiamond:
					return patternTypeEnum.OutlinedDiamond;
				case Drawing2D.HatchStyle.SmallConfetti:
					return patternTypeEnum.SmallConfetti;
				case Drawing2D.HatchStyle.HorizontalBrick:
					return patternTypeEnum.HorizontalBrick;
				case Drawing2D.HatchStyle.LargeCheckerBoard:
					return patternTypeEnum.CheckerBoard;
				case Drawing2D.HatchStyle.SolidDiamond:
					return patternTypeEnum.SolidDiamond;
				case Drawing2D.HatchStyle.DiagonalBrick:
					return patternTypeEnum.DiagonalBrick;
				default:
					return patternTypeEnum.None;					
			}
		}
		
		#region ICloneable Members

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion
	}
	
	/// <summary>
	/// The types of patterns supported.
	/// </summary>
	public enum patternTypeEnum
	{
		None,
		LargeConfetti,
		Cross,
		DarkDownwardDiagonal,
		OutlinedDiamond,
		DarkHorizontal,
		SmallConfetti,
		HorizontalBrick,
		CheckerBoard,
		Vertical,
		SolidDiamond,
		DiagonalBrick,
		BackwardDiagonal
	}

	/// <summary>
	/// The types of background gradients supported.
	/// </summary>
	public enum BackgroundGradientTypeEnum
	{
		/// <summary>
		/// No gradient
		/// </summary>
		None,
		/// <summary>
		/// Left Right gradient
		/// </summary>
		LeftRight,
		/// <summary>
		/// Top Bottom gradient
		/// </summary>
		TopBottom,
		/// <summary>
		/// Center gradient
		/// </summary>
		Center,
		/// <summary>
		/// Diagonal Left gradient
		/// </summary>
		DiagonalLeft,
		/// <summary>
		/// Diagonal Right gradient
		/// </summary>
		DiagonalRight,
		/// <summary>
		/// Horizontal Center gradient
		/// </summary>
		HorizontalCenter,
		/// <summary>
		/// Vertical Center
		/// </summary>
		VerticalCenter
	}
	/// <summary>
	/// Font styles supported
	/// </summary>
	public enum FontStyleEnum
	{
		/// <summary>
		/// Normal font
		/// </summary>
		Normal,
		/// <summary>
		/// Italic font
		/// </summary>
		Italic
	}

	/// <summary>
	/// Potential font weights
	/// </summary>
	public enum FontWeightEnum
	{
		/// <summary>
		/// Lighter font
		/// </summary>
		Lighter,
		/// <summary>
		/// Normal font
		/// </summary>
		Normal,
		/// <summary>
		/// Bold font
		/// </summary>
		Bold,
		/// <summary>
		/// Bolder font
		/// </summary>
		Bolder,
		/// <summary>
		/// W100 font
		/// </summary>
		W100,
		/// <summary>
		/// W200 font
		/// </summary>
		W200,
		/// <summary>
		/// W300 font
		/// </summary>
		W300,
		/// <summary>
		/// W400 font
		/// </summary>
		W400,
		/// <summary>
		/// W500 font
		/// </summary>
		W500,
		/// <summary>
		/// W600 font
		/// </summary>
		W600,
		/// <summary>
		/// W700 font
		/// </summary>
		W700,
		/// <summary>
		/// W800 font
		/// </summary>
		W800,
		/// <summary>
		/// W900 font
		/// </summary>
		W900
	}

	public enum TextDecorationEnum
	{
		Underline,
		Overline,
		LineThrough,
		None
	}

	public enum TextAlignEnum
	{
		Left,
		Center,
		Right,
		General,
        Justified 
	}

	public enum VerticalAlignEnum
	{
		Top,
		Middle,
		Bottom
	}

	public enum DirectionEnum
	{
		LTR,				// left to right
		RTL					// right to left
	}

	public enum WritingModeEnum
	{
		lr_tb,				// left right - top bottom (0 degrees)
		tb_rl,				// top bottom - right left (90 degrees)
		rl_bt,				// right left - bottom top (180 degrees)
		tb_lr				// top bottom - left right (270 degrees)
	}

	public enum UnicodeBiDirectionalEnum
	{
		Normal,
		Embed,
		BiDi_Override
	}
		
	public enum CalendarEnum
	{
		Gregorian,
		GregorianArabic,
		GregorianMiddleEastFrench,
		GregorianTransliteratedEnglish,
		GregorianTransliteratedFrench,
		GregorianUSEnglish,
		Hebrew,
		Hijri,
		Japanese,
		Korea,
		Taiwan,
		ThaiBuddhist
	}
}
