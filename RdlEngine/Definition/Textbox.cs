

using System;
using System.Xml;
using System.IO;
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The Textbox definition.  Inherits from ReportItem.
	///</summary>
	[Serializable]
	internal class Textbox : ReportItem
	{
		Expression _Value;	// (Variant) An expression, the value of which is
							// displayed in the text-box.
							// This can be a constant expression for constant labels.
		bool _CanGrow;		// Indicates the Textbox size can
							// increase to accommodate the contents
		bool _CanShrink;	// Indicates the Textbox size can
							// decrease to match the contents
		string _HideDuplicates;	// Indicates the item should be hidden
							//when the value of the expression
							//associated with the report item is the
							//same as the preceding instance. The
							//value of HideDuplicates is the name
							//of a grouping or data set over which
							//to apply the hiding. Each time a
							//new instance of that group is
							//encountered, the first instance of
							//this report item will not be hidden.
							//Rows on a previous page are
							//ignored for the purposes of hiding
							//duplicates. If the textbox is in a
							//table or matrix cell, only the text
							//will be hidden. The textbox will
							//remain to provide background and
							//border for the cell.
							//Ignored in matrix subtotals.
		ToggleImage _ToggleImage;	// Indicates the initial state of a
								// toggling image should one be
								// displayed as a part of the textbox.
		DataElementStyleEnum _DataElementStyle;	// Indicates whether textbox value
								// should render as an element or attribute: Auto (Default)
								// Auto uses the setting on the Report element.
		bool _IsToggle;		// Textbox is used to toggle a detail row
		bool _NoWrap;		// a box that may not grow prints each line whole, unwrapped; see NoWrapOf
		bool _TotalPagesLater;	// a body box reading Globals!TotalPages: run again once the page count is known
		List<string> _ExprReferences;	// array of names of expressions that reference this Textbox;
								//  only needed for page header/footer references 

        static readonly Regex HTMLEXPR = new Regex("(<expr>.+</expr>)");     // Split on all expressions.


		internal Textbox(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			_Value=null;
			_CanGrow=false;
			_CanShrink=false;
			_HideDuplicates=null;
			_ToggleImage=null;
			_DataElementStyle=DataElementStyleEnum.Auto;
		
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "value":
						_Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "cangrow":
						_CanGrow = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "canshrink":
						_CanShrink = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "hideduplicates":
						_HideDuplicates = xNodeLoop.InnerText;
						break;
					case "toggleimage":
						_ToggleImage = new ToggleImage(r, this, xNodeLoop);
						break;
					case "dataelementstyle":
						_DataElementStyle = Majorsilence.Reporting.Rdl.DataElementStyle.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Textbox element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			
			_NoWrap = NoWrapOf(Custom);

			if (_Value == null)
				OwnerReport.rl.LogError(8, "Textbox value not specified for " + (this.Name == null? "'name not specified'": this.Name.Nm));

			if (this.Name != null)
			{
				try
				{
					OwnerReport.LUReportItems.Add(this.Name.Nm, this);		// add to referenceable TextBoxes
				}
				catch		// Duplicate name
				{
					OwnerReport.rl.LogError(4, "Duplicate Textbox name '" + this.Name.Nm + "' ignored.");
				}
			}
		}

		// <Custom><NoWrap>true</NoWrap></Custom> asks that a box that may not grow print its value
		// without wrapping it: each line - the value splits only at its own line breaks - is drawn
		// whole on one line and cut at the box's sides, and lines below the box's height are cut
		// as before, so a one line box shows the first line. A box that may grow wraps as usual.
		private static bool NoWrapOf(Custom custom)
		{
			if (custom == null)
				return false;
			foreach (XmlNode xNode in custom.CustomXmlNode.ChildNodes)
			{
				if (xNode.Name == "NoWrap")
					return string.Equals(xNode.InnerText.Trim(), "true", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
            await base.FinalPass().ConfigureAwait(false);
            await _Value.FinalPass().ConfigureAwait(false);
            ValueIsConstant = await _Value.IsConstant().ConfigureAwait(false);   // read by DataElementOutput
			_TotalPagesLater = !InPageHeaderOrFooter() && _Value.Source != null &&
				_Value.Source.IndexOf("Globals!TotalPages", StringComparison.OrdinalIgnoreCase) >= 0;

			if (this.DataElementName == null && this.Name == null)
			{
				// no name or dataelementname; try using expression
				FunctionField ff = _Value.Expr as FunctionField;
				if (ff != null && ff.Fld != null)
				{
					this.DataElementName = ff.Fld.DataField;
                    this.Name = ff.Fld.Name; // Added 
				}
                
                FunctionAggr fa = _Value.Expr as FunctionAggr; 
                if (fa != null)
                {
                    FunctionField ff2 = fa.Expr as FunctionField;
                    if (ff2 != null && ff2.Fld != null)
                    {
                        this.DataElementName = ff2.Fld.DataField;
                        this.Name = ff2.Fld.Name; 
                    }
                }
			}

			if (_ToggleImage != null)
                await _ToggleImage.FinalPass().ConfigureAwait(false);

			if (_HideDuplicates != null)
			{
				object o = OwnerReport.LUAggrScope[_HideDuplicates];
				if (o == null)
				{
					OwnerReport.rl.LogError(4, "HideDuplicate '" +_HideDuplicates + "' is not a Group or DataSet name.   It will be ignored.");
					_HideDuplicates=null;
				}
				else if (o is Grouping)
				{	
					Grouping g = o as Grouping;
					g.AddHideDuplicates(this);
				}
				else if (o is DataSetDefn)
				{
					DataSetDefn ds = o as DataSetDefn;
					ds.AddHideDuplicates(this);
				}
			}
			return;
		}

		internal void AddExpressionReference(string name)
		{
			if (_ExprReferences == null)
				_ExprReferences = new List<string>();
			_ExprReferences.Add(name);
		}

		internal void RecordPageReference(Report rpt, Page p, Row r)
		{
			if (_ExprReferences == null)
				return;
			foreach (string refr in _ExprReferences)
			{
				p.AddPageExpressionRow(rpt, refr, r);
			}
		}

		internal void ResetPrevious(Report rpt)
		{
			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);
			ResetPrevious(tbr);
		}

		void ResetPrevious(TextboxRuntime tbr)
		{
			tbr.PreviousText=null;	
			tbr.PreviousPage=null;	
		}

		async override internal Task Run(IPresent ip, Row row)
		{
			Report rpt = ip.Report();
            await base.Run(ip, row).ConfigureAwait(false);

			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);

			tbr.RunCount++;		// Increment the run count
			if (!TryRunText(rpt, row, out string t))
				t = await RunTextAsync(rpt, row).ConfigureAwait(false);
			bool bDup =	RunTextIsDuplicate(tbr, t, null);
			if (bDup)
			{
				if (!(this.IsTableOrMatrixCell(rpt)))	// don't put out anything if not in Table or Matrix
					return;
				t = "";		// still need to put out the cell
			}
            await ip.Textbox(this, t, row).ConfigureAwait(false);

			if (!bDup)
				tbr.PreviousText=t;	// set for next time
		}

		override internal async Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(r, this);

			tbr.RunCount++;		// Increment the run count

            bool bHidden = await IsHidden(r, row).ConfigureAwait(false);

			SetPagePositionBegin(pgs);

			string t;
            if (bHidden)
                t = "";
            else
            {
                if (!TryRunText(r, row, out t))
                    t = await RunTextAsync(r, row).ConfigureAwait(false);	// get the text
            }

			bool bDup =	RunTextIsDuplicate(tbr, t, pgs.CurrentPage);
			if (bDup)
			{
				if (!(this.IsTableOrMatrixCell(r)))	// don't put out anything if not in Table or Matrix
					bHidden = true;
				t = "";		// still need to put out the cell
			}
			PageText pt;
			PageTextHtml pth=null;
			bool html = TryIsHtml(out bool pageIsHtml) ? pageIsHtml : await IsHtml(r, row).ConfigureAwait(false);
			if (!html && t != null)
				t = LineBreaksAsLineFeeds(t);
			if (!html && t != null && t.IndexOf('\t') >= 0)
				t = await ExpandTabs(r, pgs.G, row, t).ConfigureAwait(false);
			if (html)
				pt = pth = new PageTextHtml(t);
			else
				pt = new PageText(t);
            await SetPagePositionAndStyle(r, pt, row).ConfigureAwait(false);
			if (this.CanGrow && tbr.RunHeight == 0)	// when textbox is in a DataRegion this will already be called
			{
                await RunTextCalcHeight(r, pgs.G, row, pt as PageTextHtml).ConfigureAwait(false);
			}
			pt.H = Math.Max(pt.H, tbr.RunHeight);		// reset height
			if (pt.SI.BackgroundImage != null)
				pt.SI.BackgroundImage.H = pt.H;		//   and in the background image
			pt.CanGrow = this.CanGrow;
			// A box that may not grow keeps its text to its own width. Not a table or matrix
			// cell: its width is its column's rather than one chosen for the text, and a table
			// laid out in narrow columns relies on a cell's text running on over the empty cells
			// beside it - cut to the column, such a cell would print nothing at all.
			// A box asked not to wrap is cut at its sides wherever it is: it has asked for that.
			pt.NoWrap = _NoWrap && !this.CanGrow;
			pt.ClipSides = !this.CanGrow && (pt.NoWrap || !this.IsTableOrMatrixCell(r));

            // check TextAlign: if General then correct based on data type
            if (pt.SI.TextAlign == TextAlignEnum.General)
            {
                // The declared/static field type (e.g. from a DataReader schema or <rd:TypeName>) isn't known
                // for an in-memory DataTable pushed with no declared type, so prefer the actual runtime value's
                // type. TryEvaluate is already cached from the RunText above, so this costs nothing extra.
                TypeCode rtc = this.Value.GetTypeCode();
                if (!bHidden && TryEvaluate(r, row, out object alignVal) && alignVal != null)
                    rtc = Type.GetTypeCode(alignVal.GetType());

                if (DataType.IsNumeric(rtc) || rtc == TypeCode.DateTime)
                {
                    if (ReferenceEquals(pt.SI, tbr.GeneralStyle))
                        pt.SI = tbr.RightStyle;             // the shared constant style: its right aligned copy is shared too
                    else
                    {
                        StyleInfo general = pt.SI;
                        pt.SI = (StyleInfo) pt.SI.Clone();      // the style may be shared with other items
                        pt.SI.TextAlign = TextAlignEnum.Right;
                        tbr.GeneralStyle = general;
                        tbr.RightStyle = pt.SI;
                    }
                }
            }

            // Hidden objects don't affect the current page?
            if (!bHidden)
            {
                // Force page break if it doesn't fit on a page
                if (this.IsInBody &&                         // Only force page when object directly in body
                    pgs.CurrentPage.YOffset + pt.Y + pt.H >= pgs.BottomOfPage && // running off end of page
                    !pgs.CurrentPage.IsEmpty())                             // if page is already empty don't force new
                {	// force page break if it doesn't fit on the page
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                    if (this.YParents != null)
                        pt.Y = 0;
                }

                Page p = pgs.CurrentPage;
                RecordPageReference(r, p, row);			// save information for late page header/footer references
                if (this.CanSplitAcrossPages && pt.CanGrow && t.Length > 0 &&
                    p.YOffset + pt.Y + pt.H > pgs.BottomOfPage)
                {   // Issue #159: grown taller than the space any page can offer;
                    // split the text across pages instead of clipping the overflow
                    pt = await RunPageSplitOverflow(r, pgs, pt, row).ConfigureAwait(false);
                    p = pgs.CurrentPage;
                }
                else
                {
                    p.AddObject(pt);
                    if (_TotalPagesLater && !bDup && !html)
                        pgs.AddTotalPagesText(this, pt, row);
                }
                if (!bDup)
                {
                    tbr.PreviousText = t;	// previous text displayed
                    tbr.PreviousPage = p;	//  page previous text was shown on
                }
            }

			if (bHidden)
				SetPagePositionEndHidden(pgs, pt.Y + pt.H);
			else
				SetPagePositionEnd(pgs, pt.Y+pt.H);
			if (pth != null)
				pth.Reset();
			if (this.CanGrow && !await Value.IsConstant().ConfigureAwait(false))
			{
				tbr.RunHeight = 0;					// need to recalculate
			}
			return;
		}

		// Issue #159: called when a body textbox has grown taller than the space any
		// single page can offer (that case previously clipped the overflow). Splits
		// the text at whitespace boundaries into page-sized chunks, emitting one
		// PageText per page. Returns the last chunk (already added to its page).
		private async Task<PageText> RunPageSplitOverflow(Report rpt, Pages pgs, PageText pt, Row row)
		{
			Graphics g = pgs.G;
			float wrapPoints = this.WidthPoints(rpt);
			float padV = 0;
			if (this.Style != null)
			{
				wrapPoints -= await Style.EvalPaddingLeft(rpt, row).ConfigureAwait(false) + await Style.EvalPaddingRight(rpt, row).ConfigureAwait(false);
				padV = await Style.EvalPaddingTop(rpt, row).ConfigureAwait(false) + await Style.EvalPaddingBottom(rpt, row).ConfigureAwait(false);
			}
			bool html = pt is PageTextHtml;
			float lineH = await MeasureTextHeightPoints(rpt, g, "Xg", row, wrapPoints, html).ConfigureAwait(false);

			PageText chunk = pt;
			string remaining = pt.Text;
			int guard = 0;
			while (++guard < 1000)		// safety net; every iteration emits text or advances a page
			{
				Page p = pgs.CurrentPage;
				float availText = pgs.BottomOfPage - (p.YOffset + chunk.Y) - padV;
				float textH = await MeasureTextHeightPoints(rpt, g, remaining, row, wrapPoints, html).ConfigureAwait(false);
				bool freshPage = chunk.Y == 0 && p.YOffset <= OwnerReport.TopOfPage;

				if (textH <= availText)
				{	// the rest fits on this page; done
					SetChunkText(chunk, remaining);
					chunk.H = textH + padV;
					p.AddObject(chunk);
					return chunk;
				}

				if (availText < lineH && !freshPage)
				{	// not even one line fits here; move the whole chunk to a new page
					pgs.NextOrNew();
					pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
					RecordPageReference(rpt, pgs.CurrentPage, row);
					chunk.Y = 0;
					continue;
				}

				(string head, string tail) = await SplitTextToFit(rpt, g, remaining, row, wrapPoints, availText, html).ConfigureAwait(false);
				if (tail.Length == 0)
				{	// couldn't split (single unbreakable run); emit as-is
					SetChunkText(chunk, head);
					chunk.H = await MeasureTextHeightPoints(rpt, g, head, row, wrapPoints, html).ConfigureAwait(false) + padV;
					p.AddObject(chunk);
					return chunk;
				}

				PageText next = (PageText)chunk.Clone();	// clone BEFORE AddObject adjusts coordinates
				SetChunkText(chunk, head);
				chunk.H = await MeasureTextHeightPoints(rpt, g, head, row, wrapPoints, html).ConfigureAwait(false) + padV;
				p.AddObject(chunk);

				pgs.NextOrNew();
				pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
				RecordPageReference(rpt, pgs.CurrentPage, row);
				next.Y = 0;
				chunk = next;
				remaining = tail;
			}
			// guard tripped: emit whatever is left rather than looping forever
			SetChunkText(chunk, remaining);
			pgs.CurrentPage.AddObject(chunk);
			return chunk;
		}

		// Largest whitespace-bounded prefix of text whose wrapped height fits availPts.
		// Returns the whole text as head (empty tail) when no split point exists.
		// Html text is split only outside of tags; tags still open at the split are closed
		// at the end of the head and opened again at the start of the tail.
		private async Task<(string head, string tail)> SplitTextToFit(Report rpt, Graphics g, string text, Row row, float wrapPoints, float availPts, bool html)
		{
			// Plain text keeps only the positions (a long text has one per word); html needs the tags open at each.
			List<(int pos, string closers, string openers)> htmlBreaks = html ? HtmlSplitPoints(text) : null;
			int[] plainBreaks = html ? null : PlainSplitPoints(text);
			int count = html ? htmlBreaks.Count : plainBreaks.Length;
			if (count == 0)
				return (text, "");

			(int pos, string closers, string openers) BreakAt(int k) => html ? htmlBreaks[k] : (plainBreaks[k], "", "");

			int lo = 0, hi = count - 1, best = -1;
			while (lo <= hi)
			{
				int mid = (lo + hi) / 2;
				var probe = BreakAt(mid);
				float h = await MeasureTextHeightPoints(rpt, g, text.Substring(0, probe.pos) + probe.closers, row, wrapPoints, html).ConfigureAwait(false);
				if (h <= availPts)
				{
					best = mid;
					lo = mid + 1;
				}
				else
					hi = mid - 1;
			}
			var split = BreakAt(best < 0 ? 0 : best);	// force minimal progress when nothing fits
			return (text.Substring(0, split.pos) + split.closers, split.openers + text.Substring(split.pos).TrimStart());
		}

		private static int[] PlainSplitPoints(string text)
		{
			int count = 0;
			for (int i = 1; i < text.Length - 1; i++)
			{
				if (char.IsWhiteSpace(text[i]) && !char.IsWhiteSpace(text[i - 1]))
					count++;
			}
			if (count == 0)
				return Array.Empty<int>();

			var breaks = new int[count];
			int n = 0;
			for (int i = 1; i < text.Length - 1; i++)
			{
				if (char.IsWhiteSpace(text[i]) && !char.IsWhiteSpace(text[i - 1]))
					breaks[n++] = i;
			}
			return breaks;
		}

		private static readonly HashSet<string> HtmlBlockTags = new HashSet<string> { "p", "div", "li", "tr", "h1", "h2", "h3", "h4", "h5", "h6" };

		private static readonly HashSet<string> HtmlVoidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{ "br", "hr", "img", "input", "meta", "link", "col", "wbr" };

		// Places in html text where it may be split: whitespace between words, never inside a tag.
		// For each one the tags that are open there are returned as the closing tags to add to the
		// first part and the opening tags to start the second part with.
		private static List<(int pos, string closers, string openers)> HtmlSplitPoints(string text)
		{
			var breaks = new List<(int, string, string)>();
			var open = new List<(string name, string tag)>();
			// The tags open at a break only change at a tag, so consecutive breaks share the strings.
			string closers = "", openers = "";
			bool openChanged = false;
			StringBuilder sb = null;

			void AddBreak(int pos)
			{
				if (openChanged)
				{
					sb ??= new StringBuilder();
					sb.Clear();
					for (int k = open.Count - 1; k >= 0; k--)
						sb.Append("</").Append(open[k].name).Append('>');
					closers = sb.ToString();
					sb.Clear();
					foreach (var o in open)
						sb.Append(o.tag);
					openers = sb.ToString();
					openChanged = false;
				}
				breaks.Add((pos, closers, openers));
			}

			int i = 0;
			while (i < text.Length)
			{
				if (text[i] == '<')
				{
					int end = text.IndexOf('>', i);
					if (end < 0)
						break;
					string tag = text.Substring(i, end - i + 1);
					i = end + 1;
					if (tag.StartsWith("<!"))
						continue;

					bool closing = tag.Length > 1 && tag[1] == '/';
					int n = closing ? 2 : 1;
					int e = n;
					while (e < tag.Length && (char.IsLetterOrDigit(tag[e]) || tag[e] == ':'))
						e++;
					string name = tag.Substring(n, e - n).ToLowerInvariant();
					if (name.Length == 0)
						continue;

					if (closing)
					{
						int k = open.FindLastIndex(o => o.name == name);
						if (k >= 0)
						{
							open.RemoveRange(k, open.Count - k);
							openChanged = true;
						}
					}
					else if (!HtmlVoidTags.Contains(name) && !tag.EndsWith("/>"))
					{
						open.Add((name, tag));
						openChanged = true;
					}

					// the end of a paragraph, list item or line is the best place to split
					if (i < text.Length && (name == "br" || closing && HtmlBlockTags.Contains(name)))
						AddBreak(i);
					continue;
				}

				if (i > 0 && char.IsWhiteSpace(text[i]) && !char.IsWhiteSpace(text[i - 1]) && i < text.Length - 1)
					AddBreak(i);
				i++;
			}
			return breaks;
		}

		private static void SetChunkText(PageText chunk, string text)
		{
			chunk.Text = text;
			if (chunk is PageTextHtml pth)
				pth.Reset();		// the layout built for the previous text no longer applies
		}

		// The text with every line break a line feed. A carriage return on its own ends a line in
		// Crystal Reports and in Windows text controls, and a report converted from Crystal builds
		// its lines with Chr(13) alone; the engine broke lines only at a line feed, and ran such
		// lines together. A carriage return and a line feed together are one break.
		internal static string LineBreaksAsLineFeeds(string text) =>
			text.IndexOf('\r') < 0 ? text : text.Replace("\r\n", "\n").Replace('\r', '\n');

		private static object AsLineFeeds(object value) => value is string text ? LineBreaksAsLineFeeds(text) : value;

		// Tab stops every half inch from where the text starts, the default in Word and in
		// Crystal Reports. RDL has no tab stops, and a tab was drawn as whatever the font makes of
		// the character, usually nothing, so "Printed Date: 1/2/2026<tab><tab>Last modified: ..."
		// ran together.
		internal const float TabStopPoints = 36f;

		/// <summary>
		/// The text with each tab replaced by the spaces that bring what follows it to the next
		/// tab stop, measured in this textbox's own font: as close as a whole number of spaces
		/// gets, and at least one.
		/// </summary>
		/// <remarks>
		/// The measure is in whole pixels and carries a constant beyond the text (MeasureString
		/// adds a "w", and the padding GDI+ puts either side), so a width is the text's measure
		/// less an empty string's, and a space's width is taken from sixteen of them. Taken
		/// one at a time, each stop came out a few points early ("A" measured 13.5pt) and a
		/// space anywhere from 2.25 to 3pt against its 2.78.
		/// </remarks>
		internal async Task<string> ExpandTabs(Report rpt, Graphics g, Row row, string text)
		{
			const int SpaceSample = 16;
			float empty = await TextWidthPoints(rpt, g, row, "").ConfigureAwait(false);
			var result = new StringBuilder(text.Length + 16);
			string[] lines = text.Split('\n');
			for (int l = 0; l < lines.Length; l++)
			{
				if (l > 0)
					result.Append('\n');
				string[] parts = lines[l].Split('\t');
				var line = new StringBuilder(lines[l].Length + 16);
				for (int p = 0; p < parts.Length; p++)
				{
					line.Append(parts[p]);
					if (p == parts.Length - 1)
						break;
					string sofar = line.ToString();
					float measured = await TextWidthPoints(rpt, g, row, sofar).ConfigureAwait(false);
					float width = Math.Max(0, measured - empty);
					float space = (await TextWidthPoints(rpt, g, row, sofar + new string(' ', SpaceSample)).ConfigureAwait(false) - measured)
						/ SpaceSample;
					float stop = ((int) Math.Floor(width / TabStopPoints) + 1) * TabStopPoints;
					int spaces = space > 0 ? (int) Math.Round((stop - width) / space) : 1;
					line.Append(' ', Math.Max(1, spaces));
				}
				result.Append(line);
			}
			return result.ToString();
		}

		// Measured at ten times the font's size and scaled back: on the page's 96 dpi surface each
		// glyph's advance is a whole pixel, which made a 10pt Arial space 2.5pt against the 2.78
		// it is drawn at, and a tab after a dozen of them several points past its stop.
		private const float TabMeasureScale = 10f;

		private async Task<float> TextWidthPoints(Report rpt, Graphics g, Row row, string text)
		{
			Font font = this.Style != null
				? await Style.GetFont(rpt, row).ConfigureAwait(false)
				: new Font("Arial", 10);
			using (font)
			using (var big = new Font(font.Name, font.SizeInPoints * TabMeasureScale, font.Style))
			{
				System.Drawing.SizeF s = g.MeasureString(text + "w", big, int.MaxValue, StringFormat.GenericTypographic);
				return RSize.PointsFromPixels(g, s.Width) / TabMeasureScale;
			}
		}

		private async Task<float> MeasureTextHeightPoints(Report rpt, Graphics g, string text, Row row, float wrapPoints, bool html)
		{
			if (html)
			{
				var pth = new PageTextHtml(text);
				await SetPagePositionAndStyle(rpt, pth, row).ConfigureAwait(false);
				await pth.Build(g).ConfigureAwait(false);
				return pth.TotalHeight;
			}
			return this.Style != null
				? await Style.MeasureHeight(rpt, g, text, TypeCode.String, row, wrapPoints).ConfigureAwait(false)
				: await Style.MeasureHeightDefaults(rpt, g, text, TypeCode.String, row, wrapPoints).ConfigureAwait(false);
		}

		// routine to determine if text is considered to be a duplicate;
		//  ie: same as previous text and on same page
		private bool RunTextIsDuplicate(TextboxRuntime tbr, string t, Page p)
		{
			if (this._HideDuplicates == null)
				return false;
			if (t == tbr.PreviousText && p == tbr.PreviousPage)
				return true;

			return false;
		}

		internal Task<string> RunText(Report rpt, Row row)
		{
			return TryRunText(rpt, row, out string t) ? Task.FromResult(t) : RunTextAsync(rpt, row);
		}

		/// <summary>
		/// The text of the textbox without a Task, when everything it needs is a plain field or a constant (the
		/// usual table cell): the value, the Format, the Language, and no embedded expressions. False means
		/// <see cref="RunTextAsync"/> has to do it. The result is the same either way.
		/// </summary>
		internal bool TryRunText(Report rpt, Row row, out string t)
		{
			t = null;
			if (!TryEvaluate(rpt, row, out object o))
				return false;
			if (o is double d && Double.IsNaN(d))
				o = null;                           // suppress NaN, as RunTextAsync does
			if (!Style.TryGetFormatedString(rpt, Style, o, _Value.GetTypeCode(), out t))
				return false;
			if (!TryIsHtml(out bool isHtml))
				return false;
			return !(isHtml && t != null && t.Contains("<expr>"));   // dynamic expressions take the async path
		}

		internal async Task<string> RunTextAsync(Report rpt, Row row)
		{
			object o = await Evaluate(rpt, row).ConfigureAwait(false);
            // AJM 15082008: Suppress NaN from appearing in a textbox
            if (o is double) 
            {
                if (Double.IsNaN((double)o)) 
                { 
                    o = null; 
                } 
            }   
			string t = await Style.GetFormatedString(rpt, Style, row, o, _Value.GetTypeCode()).ConfigureAwait(false);
            if (await IsHtml(rpt, row).ConfigureAwait(false) && t != null && t.Contains("<expr>"))
            {
                string[] parts = HTMLEXPR.Split(t);
                StringBuilder sb = new StringBuilder(t.Length);
                foreach (string s in parts)
                {
                    if (s.StartsWith("<expr>") && s.EndsWith("</expr>"))
                    {
                        string expr = s.Substring(6, s.Length - 13);
                        DynamicExpression de = await DynamicExpression.CreateAsync(rpt, this, expr, row).ConfigureAwait(false);
                        sb.Append(await de.Evaluate(rpt, row).ConfigureAwait(false));
                    }
                    else
                        sb.Append(s);
                }
                t = sb.ToString();
            }
			return t;
		}

		internal ValueTask<float> RunTextCalcHeight(Report rpt, Graphics g, Row row)
		{
			return RunTextCalcHeight(rpt, g, row, null);
		}
		
		// ValueTask, and the constant parts (style, padding, value, format) taken without a Task when they can be:
		// a table of growing cells calls this for every cell of every row.
		internal async ValueTask<float> RunTextCalcHeight(Report rpt, Graphics g, Row row, PageTextHtml pth)
		{	// normally only called when CanGrow is true
			float height;

			if (await IsHidden(rpt, row).ConfigureAwait(false))
				return 0;

			object o = TryEvaluate(rpt, row, out object value) ? value : await Evaluate(rpt, row).ConfigureAwait(false);

			TypeCode tc = _Value.GetTypeCode();
			float wrapPoints = this.WidthPoints(rpt);		// the width between the paddings the text is drawn in

			Style.MeasureSetup setup = null;
			if (this.Style != null)
			{
				setup = Style.TryGetMeasureSetup(rpt) ?? await Style.GetMeasureSetup(rpt, row).ConfigureAwait(false);
				if (setup != null)
					wrapPoints -= setup.PadLeft + setup.PadRight;
				else
					wrapPoints -= await Style.EvalPaddingLeft(rpt, row).ConfigureAwait(false) + await Style.EvalPaddingRight(rpt, row).ConfigureAwait(false);

				if (TryIsHtml(out bool isHtml) ? isHtml : await IsHtml(rpt, row).ConfigureAwait(false))
				{
					if (pth == null)
					{
						pth = new PageTextHtml(o==null? "": o.ToString());
						await SetPagePositionAndStyle(rpt, pth, row).ConfigureAwait(false);
					}
					await pth.Build(g).ConfigureAwait(false);
					height = RSize.PointsFromPixels(g, RSize.PixelsFromPoints(pth.TotalHeight));
				}
				else if (!Style.TryMeasureHeight(rpt, g, o = AsLineFeeds(o), tc, wrapPoints, out height))
					height = await Style.MeasureHeight(rpt, g, o, tc, row, wrapPoints).ConfigureAwait(false);
			}
			else	// call the class static method
				height = await Style.MeasureHeightDefaults(rpt, g, AsLineFeeds(o), tc, row, wrapPoints).ConfigureAwait(false);

			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);
			tbr.RunHeight = height;
			if (Style != null)
				tbr.RunHeight += setup != null
					? setup.PadBottom + setup.PadTop
					: (await Style.EvalPaddingBottom(rpt, row).ConfigureAwait(false) + await Style.EvalPaddingTop(rpt, row).ConfigureAwait(false));
			return tbr.RunHeight;
		}

		internal Task<object> Evaluate(Report rpt, Row r)
		{
			return TryEvaluate(rpt, r, out object v) ? Task.FromResult(v) : EvaluateAsync(rpt, r);
		}

		// The value of the textbox for a row. The last row's value is kept (in the report's run state, not in this
		// definition: a definition may be shared by reports running at the same time) because one row asks several times.
		internal bool TryEvaluate(Report rpt, Row r, out object value)
		{
			if (rpt == null)
			{
				value = null;
				return false;
			}

			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);
			if (r != null && tbr.LastRow == r)
			{
				value = tbr.LastValue;
				return true;
			}

			if (_Value.TryEvaluate(rpt, r, out value))
			{
				tbr.LastRow = r;
				tbr.LastValue = value;
				return true;
			}
			return false;
		}

		private async Task<object> EvaluateAsync(Report rpt, Row r)
		{
			if (rpt == null)
				return await _Value.Evaluate(rpt, r).ConfigureAwait(false);

			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);
			if (r != null && tbr.LastRow == r)
				return tbr.LastValue;

			object value = await _Value.Evaluate(rpt, r).ConfigureAwait(false);
			tbr.LastRow = r;
			tbr.LastValue = value;
			return value;
		}

		internal Expression Value
		{
			get { return  _Value; }
			set {  _Value = value; }
		}

		// A tall textbox is only carried over to following pages when it sits directly in the body
		// or is a row of a list, which carries on below the text.
		private bool CanSplitAcrossPages
		{
			get { return this.IsInBody || this.Parent.Parent is List; }
		}

		internal bool CanGrow
		{
			get { return  _CanGrow; }
			set {  _CanGrow = value; }
		}

		internal bool CanShrink
		{
			get { return  _CanShrink; }
			set {  _CanShrink = value; }
		}

		internal string HideDuplicates
		{
			get { return  _HideDuplicates; }
			set {  _HideDuplicates = value; }
		}

		// Whether the Format says html, when the Format is absent or a constant.
		internal bool TryIsHtml(out bool isHtml)
		{
			isHtml = false;
			if (this.Style == null || this.Style.Format == null)
				return true;
			if (!this.Style.Format.TryEvaluateString(out string format))
				return false;
			isHtml = format != null && string.Equals(format, "html", StringComparison.OrdinalIgnoreCase);
			return true;
		}

		internal async Task<bool> IsHtml(Report rpt, Row row)
		{
			if (this.Style == null || this.Style.Format == null)
				return false;
			string format = await Style.Format.EvaluateString(rpt, row).ConfigureAwait(false);
			if (format == null)
				return false;
			return format.ToLower() == "html";
		}

		internal ToggleImage ToggleImage
		{
			get { return  _ToggleImage; }
			set {  _ToggleImage = value; }
		}

		internal bool IsToggle
		{
			get { return  _IsToggle; }
			set {  _IsToggle = value; }
		}

		internal int RunCount(Report rpt)
		{
			TextboxRuntime tbr = TextboxRuntime.GetTextboxRuntime(rpt, this);

			return  tbr.RunCount;
		}

		internal DataElementStyleEnum DataElementStyle
		{
			get 
			{
				if (_DataElementStyle == DataElementStyleEnum.Auto)	// auto means use report
					return OwnerReport.DataElementStyle;
				else
					return  _DataElementStyle; 
			}
			set {  _DataElementStyle = value; }
		}

	}

	class TextboxRuntime
	{
		internal int RunCount=0;			// number of times TextBox is rendered at runtime;
											//    used to generate unique names for toggling visibility
		internal float RunHeight=0;			// the runtime height (in points)
		internal string PreviousText=null;	// previous text displayed
		internal Page PreviousPage=null;	//  page previous text was shown on
		internal object LastObject=null;	// last object calculated
		internal Row LastRow=null;			// the row LastValue is the textbox's value for
		internal object LastValue=null;
		internal StyleInfo GeneralStyle=null;	// a numeric cell's style with TextAlign General ...
		internal StyleInfo RightStyle=null;		//   and its right aligned copy, reused while the style is the same instance

		static internal TextboxRuntime GetTextboxRuntime(Report rpt, Textbox tb)
		{
			TextboxRuntime tbr = rpt.Cache.Get(tb, "txtbox") as TextboxRuntime;
			if (tbr != null)
				return tbr;
			tbr = new TextboxRuntime();
			rpt.Cache.Add(tb, "txtbox", tbr);
			return tbr;
		}
	}
}
