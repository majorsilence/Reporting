

using System;
using System.Collections;
using System.Collections.Generic;
using Draw = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;
using System.Text;
using System.IO;
using System.Net;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Net.Http;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl.Utility;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// PageTextHtml handles text that should to be formatted as HTML.  It only handles
    /// a subset of HTML (e.g. "<b>,<br>, ..."
    ///</summary>
    public class PageTextHtml : PageText, IEnumerable, ICloneable
	{
		List<PageItem> _items=null;
		Stack _StyleStack;				// work variable when processing styles
		float _TotalHeight=0;
		public PageTextHtml(string t) : base(t)
		{
		}

		/// <summary>
		/// Reset will force a recalculation of the embedded PageItems;
		/// </summary>
		public void Reset()
		{
			_items = null;
		}

		public float TotalHeight
		{
			get 
			{
				if (_items == null)
					throw new Exception(Strings.PageTextHtml_Error_BuildMethodMustCalledPriorTotalHeight);
				return _TotalHeight;
			}
		}

		public async Task Build(Draw.Graphics g)
		{
            Draw.Drawing2D.Matrix transform = g.Transform;
            try
            {
                g.ResetTransform();
                await BuildPrivate(g);
            }
            finally
            {
                g.Transform = transform;
            }
            return;
        }

		private async Task BuildPrivate(Draw.Graphics g)
        {
            PageText model = new PageText("");
            model.AllowSelect = false;
            model.Page = this.Page;
			model.HyperLink=null;
			model.Tooltip=null;
			int fontSizeModel = 3;

			if (_items != null)		// this has already been built
				return;
            _items = new List<PageItem>();
			_StyleStack = new Stack();

			// The first item is always a text box with the border and background attributes
			PageText pt = new PageText("");
            pt.AllowSelect = true;                // This item represents HTML item for selection in RdlViewer
            pt.Page = this.Page;
            pt.HtmlParent = this;
			pt.X = this.X;
			pt.Y = this.Y;
			pt.H = this.H;
			pt.W = this.W;
			pt.CanGrow = false;
			pt.SI = this.SI.Clone() as StyleInfo;
			pt.SI.PaddingBottom = pt.SI.PaddingLeft = pt.SI.PaddingRight = pt.SI.PaddingTop = 0;
			pt.SI.TextAlign = TextAlignEnum.Left;
			_items.Add(pt);

			// Now we create multiple items that represent what is in the box
			PageTextHtmlLexer hl = new PageTextHtmlLexer(this.Text);
			List<string> tokens = hl.Lex();

			float textWidth = this.W - pt.SI.PaddingLeft - pt.SI.PaddingRight;
			// Now set the default style for the rest of the members
			StyleInfo si = this.SI.Clone() as StyleInfo;
			si.BStyleBottom = si.BStyleLeft = si.BStyleRight = si.BStyleTop = BorderStyleEnum.None;
			pt.SI.TextAlign = TextAlignEnum.Left;
			pt.SI.VerticalAlign = VerticalAlignEnum.Top;
			si.BackgroundColor = System.Drawing.Color.Empty;
			si.BackgroundGradientType = BackgroundGradientTypeEnum.None;
			si.BackgroundImage = null;

			bool bFirstInLine=true;
			StringBuilder sb = new StringBuilder(); // this will hold the accumulating line
			float lineXPos=0;
			float xPos = 0;
			float yPos = 0;
			float maxLineHeight=0;
			float maxDescent=0;
			float descent;				// working value for descent
			System.Drawing.SizeF ms;
			bool bWhiteSpace=false;
			List<PageItem> lineItems = new List<PageItem>();
            bool bIsOrderedList = false;
			int? OlLiCount = null;	//Maintains number of OL items; zero indicates UL item (bullet)
			foreach (string token in tokens)
			{
				if (token[0] == PageTextHtmlLexer.HTMLCMD)		// indicates an HTML command
				{
					// we need to create a PageText since the styleinfo is changing
					if (sb.Length != 0)
					{
						pt = new PageText(sb.ToString());
                        pt.AllowSelect = false;
                        pt.Page = this.Page;
                        pt.HtmlParent = this;
						pt.HyperLink = model.HyperLink;
						pt.Tooltip = model.Tooltip;
						pt.NoClip = true;
						sb = new StringBuilder();
						pt.X = this.X + lineXPos;
						pt.Y = this.Y + yPos;
						pt.CanGrow = false;
						pt.SI = CurrentStyle(si).Clone() as StyleInfo;
						_items.Add(pt);
						lineItems.Add(pt);
						ms = this.MeasureString(pt.Text, pt.SI, g, out descent);
						maxDescent = Math.Max(maxDescent, descent);
						pt.W = ms.Width;
						pt.H = ms.Height;
						pt.Descent = descent;
						maxLineHeight = Math.Max(maxLineHeight, ms.Height);
						lineXPos = xPos;
					}
					// Now reset the styleinfo
					StyleInfo cs = CurrentStyle(si);
					string ltoken = token.Substring(1,Math.Min(token.Length-1,10)).ToLower();
					if (ltoken == "<b>" || ltoken == "<strong>")
						cs.FontWeight = FontWeightEnum.Bold;
					else if (ltoken == "</b>" || ltoken == "</strong>")
						cs.FontWeight = FontWeightEnum.Normal;
					else if (ltoken == "<i>" || ltoken == "<cite>" || ltoken == "<var>" || ltoken == "<em>")
						cs.FontStyle = FontStyleEnum.Italic;
					else if (ltoken == "</i>" || ltoken == "</cite>" || ltoken == "</var>" || ltoken == "</em>")
						cs.FontStyle = FontStyleEnum.Normal;
					else if (ltoken == "<code>" || ltoken == "<samp>")
						cs.FontFamily = "Courier New";
					else if (ltoken == "</code>" || ltoken == "</samp>")
						cs.FontFamily = this.SI.FontFamily;
					else if (ltoken == "<kbd>")
					{
						cs.FontFamily = "Courier New";
						cs.FontWeight = FontWeightEnum.Bold;
					}
					else if (ltoken == "</kdd>")
					{
						cs.FontFamily = this.SI.FontFamily;
						cs.FontWeight = FontWeightEnum.Normal;
					}
					else if (ltoken == "<big>")
					{	// big makes it bigger by 20% for each time over the baseline of 3
						fontSizeModel++;
						float inc = 1;
						for (int i=3; i < fontSizeModel; i++)
						{
							inc += .2f;
						}
						float h = this.SI.FontSize * inc;
						cs.FontSize = h;
					}
					else if (ltoken == "</big>")
					{	// undoes the effect of big
						fontSizeModel--;
						float inc = 1;
						for (int i=3; i < fontSizeModel; i++)
						{
							inc += .2f;
						}
						float h = this.SI.FontSize / inc;
						cs.FontSize = h;
					}
					else if (ltoken == "<small>")
					{	// small makes it smaller by 20% for each time under the baseline of 3
						fontSizeModel--;
						float inc = 1;
						for (int i=3; i > fontSizeModel; i--)
						{
							inc += .2f;
						}
						float h = this.SI.FontSize / inc;
						cs.FontSize = h;
					}
					else if (ltoken == "</small>")
					{	// undoes the effect of small
						fontSizeModel++;
						float inc = 1;
						for (int i=3; i > fontSizeModel; i--)
						{
							inc += .2f;
						}
						float h = this.SI.FontSize * inc;
						cs.FontSize = h;
					}
					else if (ltoken.StartsWith("<br"))
					{
						yPos += maxLineHeight;
						NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
						xPos = lineXPos = maxDescent = 0;
						bFirstInLine = true;
						bWhiteSpace = false;
					}
                    else if (ltoken.StartsWith("<hr"))
                    {   // Add a line
                        // Process existing line if any
                        yPos += maxLineHeight;
                        NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
                        maxLineHeight = xPos = lineXPos = maxDescent = 0;
                        bFirstInLine = true;
                        bWhiteSpace = false;

                        PageLine pl = new PageLine();
                        pl.AllowSelect = false;
                        pl.Page = this.Page;
                        const int horzLineHeight = 10;
                        pl.SI = cs.Clone() as StyleInfo;
                        pl.SI.BStyleLeft = BorderStyleEnum.Ridge;
                        pl.Y = pl.Y2 = this.Y + yPos + (horzLineHeight / 2);
                        pl.X = this.X;
                        pl.X2 = pl.X + this.W;
                        _items.Add(pl);
                        yPos += horzLineHeight;  // skip past horizontal line
                    }
                    else if (ltoken.StartsWith("<p"))
					{
						yPos += maxLineHeight * 2;
						NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
						maxLineHeight = xPos = lineXPos = maxDescent = 0;
						bFirstInLine = true;
						bWhiteSpace = false;
					}
					else if (ltoken.StartsWith("<a"))
					{
						BuildAnchor(token.Substring(1), cs, model);
					}
                    else if (ltoken.StartsWith("<img"))
                    {
                        PageImage pimg = await BuildImage(g, token.Substring(1), cs, model);
                        if (pimg != null)   // We got an image; add to process list
                        {
                            pimg.Y = this.Y + yPos;
                            pimg.X = this.X;
                            _items.Add(pimg);
                            yPos += pimg.H;	        // Increment y position
                            maxLineHeight = xPos = lineXPos = maxDescent = 0;
                            bFirstInLine = true;
                            bWhiteSpace = false;
                        }
                    }
                    else if (ltoken == "</a>")
                    {
                        model.HyperLink = model.Tooltip = null;
                        PopStyle();
                    }
                    else if (ltoken.StartsWith("<span"))
                    {
                        HandleStyle(token.Substring(1), si);
                    }
                    else if (ltoken == "</span>")
                    {   // we really should match span and font but it shouldn't matter very often?
                        PopStyle();
                    }
                    else if (ltoken.StartsWith("<font"))
                    {
                        HandleFont(token.Substring(1), si);
                    }
                    else if (ltoken == "</font>")
                    {   // we really should match span and font but it shouldn't matter very often?
                        PopStyle();
                    }
                    else if (ltoken.StartsWith("<ol") || ltoken.StartsWith("<ul"))
                    {
                        yPos += maxLineHeight;
                        NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
                        maxLineHeight = xPos = lineXPos = maxDescent = 0;
                        bFirstInLine = true;
                        bWhiteSpace = false;

                        bIsOrderedList = ltoken.StartsWith("<ol");
                    }
                    else if (ltoken.StartsWith("<li"))
                    {
                        yPos += maxLineHeight;
                        NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
                        maxLineHeight = xPos = lineXPos = maxDescent = 0;
                        bFirstInLine = true;
                        bWhiteSpace = false;

                        if (OlLiCount == null)
                            OlLiCount = 0;
                        if(bIsOrderedList)
                            OlLiCount++;
                    }
                    else if (ltoken.StartsWith("</ol") || ltoken.StartsWith("</ul"))
                    {
                        bIsOrderedList = false;
                        OlLiCount = null;
                    }                    
                    continue;
				}
				if (token == PageTextHtmlLexer.WHITESPACE)
				{
					if (!bFirstInLine)
						bWhiteSpace = true;
					continue;
				}

				if (token != PageTextHtmlLexer.EOF)
				{
					string ntoken;
                    if (token == PageTextHtmlLexer.NBSP.ToString())
                        ntoken = bWhiteSpace ? "  " : " ";
                    else
                        ntoken = bWhiteSpace ? " " + token : token;
                    ntoken = ntoken.Replace(PageTextHtmlLexer.NBSP, ' ');

					bWhiteSpace = false;			// can only use whitespace once
					ms = this.MeasureString(ntoken, CurrentStyle(si), g, out descent);
					if (xPos + ms.Width < textWidth)
					{
                        if (bFirstInLine)
                        {
                            if (OlLiCount == 0)                        //Adds UL bullet
                                sb.Append("•   ");
                            else if (OlLiCount > 0)                        //Adds OL numeric prefix
                                sb.AppendFormat("{0})   ", OlLiCount);
                        }

                        bFirstInLine = false;
						sb.Append(ntoken);

						maxDescent = Math.Max(maxDescent, descent);
						maxLineHeight = Math.Max(maxLineHeight, ms.Height);
						xPos += ms.Width;
						continue;
					}
				}
				else if (sb.Length == 0)	// EOF and no previous string means we're done
					continue;

				pt = new PageText(sb.ToString());
                pt.AllowSelect = false;
                pt.Page = this.Page;
                pt.HtmlParent = this;
                pt.NoClip = true;
				pt.HyperLink = model.HyperLink;
				pt.Tooltip = model.Tooltip;
				sb = new StringBuilder();
				sb.Append(token.Replace(PageTextHtmlLexer.NBSP, ' '));
				pt.SI = CurrentStyle(si).Clone() as StyleInfo;
				ms = this.MeasureString(pt.Text, pt.SI, g, out descent);
				pt.X = this.X + lineXPos;
				pt.Y = this.Y + yPos;
				pt.H = ms.Height;
				pt.W = ms.Width; 
				pt.Descent = descent;
				pt.CanGrow = false;
				_items.Add(pt);
				lineItems.Add(pt);
				maxDescent = Math.Max(maxDescent, descent);
				maxLineHeight = Math.Max(maxLineHeight, ms.Height);
				yPos += maxLineHeight;	// Increment y position
				NormalizeLineHeight(lineItems, maxLineHeight, maxDescent);
				lineXPos = maxLineHeight = maxDescent = 0;	// start line height over

				// Now set the xPos just after the current token
				ms = this.MeasureString(token, CurrentStyle(si), g, out descent);
				xPos = ms.Width;	 
			}
		
			_TotalHeight = yPos;		// set the calculated height of the result
			_StyleStack = null;
			return;
		}

		private void BuildAnchor(string token, StyleInfo oldsi, PageText model)
		{
			StyleInfo si= oldsi.Clone() as StyleInfo;	// always push a StyleInfo
			_StyleStack.Push(si);						//   since they will always be popped

			Hashtable ht = ParseHtmlCmd(token);

			string href = (string) ht["href"];
			if (href == null || href.Length < 1)
				return;
			model.HyperLink = model.Tooltip = href;
			si.TextDecoration = TextDecorationEnum.Underline;
			si.Color = System.Drawing.Color.Blue;
		}

        private async Task<PageImage> BuildImage(Draw.Graphics g, string token, StyleInfo oldsi, PageText model)
        {
            PageTextHtmlCmdLexer hc = new PageTextHtmlCmdLexer(token.Substring(4));
            Hashtable ht = hc.Lex();

            string src = (string)ht["src"];
            if (src == null || src.Length < 1)
                return null;

            string alt = (string)ht["alt"];

            string height = (string)ht["height"];
            string width = (string)ht["width"];
            string align = (string)ht["align"];

            Stream strm = null;
            Draw.Image im = null;
            PageImage pi = null;
            try
            {
                // Obtain the image stream
                if (src.StartsWith("http:") || src.StartsWith("file:") || src.StartsWith("https:"))
                {
                    using (HttpClient client = new HttpClient())
                    {
						client.AddMajorsilenceReportingUserAgent();
                        strm = await client.GetStreamAsync(src);
                    }
                }
                else
                {
                    strm = new FileStream(src, System.IO.FileMode.Open, FileAccess.Read);
                }

                im = Draw.Image.FromStream(strm);
                int h = im.Height;
                int w = im.Width;
                MemoryStream ostrm = new MemoryStream();
                ImageFormat imf;
                imf = ImageFormat.Jpeg;
                im.Save(ostrm, imf);
                byte[] ba = ostrm.ToArray();
                ostrm.Close();
                pi = new PageImage(imf, ba, w, h);
                pi.AllowSelect = false;
                pi.Page = this.Page;
                pi.HyperLink = model.HyperLink;
                pi.Tooltip = alt == null ? model.Tooltip : alt;
                pi.X = 0;
                pi.Y = 0;

                pi.W = RSize.PointsFromPixels(g, width != null ? Convert.ToInt32(width) : w);
                pi.H = RSize.PointsFromPixels(g, height != null ? Convert.ToInt32(height) : h);
                pi.SI = new StyleInfo();
            }
            catch
            {
                pi = null;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }

            return pi;
        }

		private StyleInfo CurrentStyle(StyleInfo def)
		{
			if (_StyleStack == null  || _StyleStack.Count == 0)
				return def;
			else
				return (StyleInfo) _StyleStack.Peek();
		}

		private void HandleStyle(string token, StyleInfo model)
		{
			StyleInfo si= model.Clone() as StyleInfo;	// always push a StyleInfo
			_StyleStack.Push(si);						//   since they will always be popped

			Hashtable ht = ParseHtmlCmd(token);
			string style = (string) ht["style"];

            HandleStyleString(style, si);

            return;
        }

        private void HandleFont(string token, StyleInfo model)
        {
            StyleInfo si = model.Clone() as StyleInfo;	// always push a StyleInfo
            _StyleStack.Push(si);						//   since they will always be popped

            PageTextHtmlCmdLexer hc = new PageTextHtmlCmdLexer(token.Substring(5));
            Hashtable ht = hc.Lex();

            string style = (string)ht["style"];
            HandleStyleString(style, si);

            string color = (string)ht["color"];
            if (color != null && color.Length > 0)
                si.Color = XmlUtil.ColorFromHtml(color, si.Color);

            string size = (string)ht["size"];
            if (size != null && size.Length > 0)
                HandleStyleFontSize(si, size);

            string face = (string)ht["face"];
            if (face != null && face.Length > 0)
                si.FontFamily = face;

            return;
        }

        private void HandleStyleString(string style, StyleInfo si)
        {
            if (style == null || style.Length < 1)
                return;

			string[] styleList = style.Split(new char[] {';'});

			foreach (string item in styleList)
			{
				string[] val = item.Split(new char[] {':'});
				if (val.Length != 2)
					continue;			// must be illegal syntax
				string tval = val[1].Trim();
				switch (val[0].ToLower().Trim())
				{
					case "background":
					case "background-color":
						si.BackgroundColor = XmlUtil.ColorFromHtml(tval, si.Color);
						break;
					case "color":
						si.Color = XmlUtil.ColorFromHtml(tval, si.Color);
						break;
					case "font-family":
						si.FontFamily = tval;
						break;
					case "font-size":
						HandleStyleFontSize(si, tval);
						break;
					case "font-style":
						if (tval == "italic")
							si.FontStyle = FontStyleEnum.Italic;
						break;
					case "font-weight":
						HandleStyleFontWeight(si, tval);
						break;
				}
			}
			
			return;
		}

		private void HandleStyleFontSize(StyleInfo si, string size)
		{
			try
			{
				int i = size.IndexOf("pt");

				if (i > 0)
				{
					size = size.Remove(i, 2);
					float n = (float) Convert.ToDouble(size);
					if (size[0] == '+')
						si.FontSize += n;
					else
						si.FontSize = n;
					return;
				}
				i = size.IndexOf("%");
				if (i > 0)
				{
					size = size.Remove(i, 1);
					float n = (float) Convert.ToDouble(size);
					si.FontSize = n*si.FontSize;
					return;
				}
				switch (size)
				{
					case "xx-small":
						si.FontSize = 6;
						break;
					case "x-small":
						si.FontSize = 8;
						break;
					case "small":
						si.FontSize = 10;
						break;
					case "medium":
						si.FontSize = 12;
						break;
					case "large":
						si.FontSize = 14;
						break;
					case "x-large":
						si.FontSize = 16;
						break;
					case "xx-large":
						si.FontSize = 18;
						break;
                    case "1":
                        si.FontSize = 8;
                        break;
                    case "2":
                        si.FontSize = 10;
                        break;
                    case "3":
                        si.FontSize = 12;
                        break;
                    case "4":
                        si.FontSize = 14;
                        break;
                    case "5":
                        si.FontSize = 18;
                        break;
                    case "6":
                        si.FontSize = 24;
                        break;
                    case "7":
                        si.FontSize = 36;
                        break;
                }
			}
			catch {}		// lots of user errors will cause an exception; ignore
			return;
		}

		private void HandleStyleFontWeight(StyleInfo si, string w)
		{
			try
			{
				switch (w)
				{
					case "bold":
						si.FontWeight = FontWeightEnum.Bold;
						break;
					case "bolder":
						if (si.FontWeight > FontWeightEnum.Bolder)
						{
							if (si.FontWeight < FontWeightEnum.W900)
								si.FontWeight++;
						}
						else if (si.FontWeight == FontWeightEnum.Normal)
							si.FontWeight = FontWeightEnum.W700;
						else if (si.FontWeight == FontWeightEnum.Bold)
							si.FontWeight = FontWeightEnum.W900;
						else if (si.FontWeight != FontWeightEnum.Bolder)
							si.FontWeight = FontWeightEnum.Normal;
						break;
					case "lighter":
						if (si.FontWeight > FontWeightEnum.Bolder)
						{
							if (si.FontWeight > FontWeightEnum.W100)
								si.FontWeight--;
						}
						else if (si.FontWeight == FontWeightEnum.Normal)
							si.FontWeight = FontWeightEnum.W300;
						else if (si.FontWeight == FontWeightEnum.Bold)
							si.FontWeight = FontWeightEnum.W400;
						else if (si.FontWeight != FontWeightEnum.Lighter)
							si.FontWeight = FontWeightEnum.Normal;
						break;
					case "normal":
						si.FontWeight = FontWeightEnum.Normal;
						break;
					case "100":
						si.FontWeight = FontWeightEnum.W100;
						break;
					case "200":
						si.FontWeight = FontWeightEnum.W200;
						break;
					case "300":
						si.FontWeight = FontWeightEnum.W300;
						break;
					case "400":
						si.FontWeight = FontWeightEnum.W400;
						break;
					case "500":
						si.FontWeight = FontWeightEnum.W500;
						break;
					case "600":
						si.FontWeight = FontWeightEnum.W600;
						break;
					case "700":
						si.FontWeight = FontWeightEnum.W700;
						break;
					case "800":
						si.FontWeight = FontWeightEnum.W800;
						break;
					case "900":
						si.FontWeight = FontWeightEnum.W900;
						break;
				}
			}
			catch {}		// lots of user errors will cause an exception; ignore
			return;
		}

		private void PopStyle()
		{
			if (_StyleStack != null && _StyleStack.Count > 0)
				_StyleStack.Pop();
		}

		private void NormalizeLineHeight(List<PageItem> lineItems, float maxLineHeight, float maxDescent)
		{
			foreach (PageItem pi in lineItems)
			{
				if (pi is PageText)
				{	// force the text to line up
					PageText pt = (PageText) pi;
					if (pt.H >= maxLineHeight)
						continue;

					pt.Y += maxLineHeight - pt.H;
					if (pt.Descent > 0 && pt.Descent < maxDescent)
						pt.Y -= (maxDescent - pt.Descent);
				}
			}
			lineItems.Clear();
		}

		private Hashtable ParseHtmlCmd(string token)
		{
			Hashtable ht = new Hashtable();
			// find the start and the end of the command
			int start = token.IndexOf(' ');	// look for first blank
			if (start < 0)
				return ht;
			int end = token.LastIndexOf('>');
			if (end < 0 || end <= start)
				return ht;
			string cmd = token.Substring(start, end - start);
			string[] keys = cmd.Split(new char[] {'='});
            if (keys == null || keys.Length < 2)
                return ht;
            try
            {
                for (int i = 0; i < keys.Length - 1; i += 2)
                {
                    // remove " from the value if any
                    string v = keys[i + 1];
                    if (v.Length > 0 && (v[0] == '"' || v[0] == '\''))
                        v = v.Substring(1);
                    if (v.Length > 0 && (v[v.Length - 1] == '"' || v[v.Length - 1] == '\''))
                        v = v.Substring(0, v.Length - 1);
                    // normalize key to lower case
                    string key = keys[i].ToLower().Trim();
                    ht.Add(key, v);
                }
            }
            catch { }   // there are any number of ill formed strings that could cause problems; keep what we've found
			return ht;
		}

		private System.Drawing.SizeF MeasureString(string s, StyleInfo si, Draw.Graphics g, out float descent)
		{
			Draw.Font drawFont=null;
			Draw.StringFormat drawFormat=null;
			System.Drawing.SizeF ms = System.Drawing.SizeF.Empty;
			descent = 0;				
			if (s == null || s.Length == 0)
				return ms;
			try
			{
				// STYLE
				Draw.FontStyle fs = 0;
				if (si.FontStyle == FontStyleEnum.Italic)
					fs |= Draw.FontStyle.Italic;

				// WEIGHT
				switch (si.FontWeight)
				{
					case FontWeightEnum.Bold:
					case FontWeightEnum.Bolder:
					case FontWeightEnum.W500:
					case FontWeightEnum.W600:
					case FontWeightEnum.W700:
					case FontWeightEnum.W800:
					case FontWeightEnum.W900:
						fs |= Draw.FontStyle.Bold;
						break;
					default:
						break;
				}
				try
				{
					Draw.FontFamily ff = si.GetFontFamily();
					drawFont = new Draw.Font(ff, si.FontSize, fs);
					// following algorithm comes from the C# Font Metrics documentation
					float descentPixel = si.FontSize * ff.GetCellDescent(fs) / ff.GetEmHeight(fs);
					descent = RSize.PointsFromPixels(g, descentPixel);
				}
				catch
				{
					drawFont = new Draw.Font("Arial", si.FontSize, fs);	// usually because font not found
					descent = 0;
				}
				drawFormat = new Draw.StringFormat();
				drawFormat.Alignment = Draw.StringAlignment.Near;

				Draw.CharacterRange[] cr = {new Draw.CharacterRange(0, s.Length)};
				drawFormat.SetMeasurableCharacterRanges(cr);
				Draw.Region[] rs = new Draw.Region[1];
				rs = g.MeasureCharacterRanges(s, drawFont, new System.Drawing.RectangleF(0,0,float.MaxValue,float.MaxValue),
					drawFormat);
				System.Drawing.RectangleF mr = rs[0].GetBounds(g);

				ms.Height = RSize.PointsFromPixels(g, mr.Height);	// convert to points from pixels
				ms.Width = RSize.PointsFromPixels(g, mr.Width);		// convert to points from pixels
				return ms;
			}
			finally
			{
				if (drawFont != null)
					drawFont.Dispose();
				if (drawFormat != null)
					drawFont.Dispose();
			}
		}
		
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
            if (_items == null)
                return null;
			return _items.GetEnumerator();
		}

		#endregion

		#region ICloneable Members

		new public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion
	}
}
