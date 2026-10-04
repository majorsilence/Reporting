

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Threading.Tasks;


using Draw2 = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;

namespace Majorsilence.Reporting.Rdl
{

    ///<summary>
    /// Renders a report to HTML.   This handles some page formating but does not do true page formatting.
    ///</summary>
    internal class RenderRtf: IPresent
	{
        static readonly char[] HEXCHARS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        
        Report r;					// report
		StringWriter tw;			// temporary location where the output is going
		IStreamGen _sg;				// stream generater
        System.Collections.Generic.List<string> _Fonts;        // list of fonts used
        System.Collections.Generic.List<System.Drawing.Color> _Colors;         // list of colors used
        Draw2.Bitmap _bm=null;			// bm and
        Draw2.Graphics _g=null;			//		  g are needed when calculating string heights

        // some matrix generation variables
        int[] _MatrixColumnWidths;    // column widths for matrix
        int _MatrixHeaderRows;
        int _MatrixRows;            // # rows in matrix
        int _MatrixCols;
        int _MatrixCellSpan;        // work variable for matrixes
        MatrixCellEntry[,] _MatrixData;

		public RenderRtf(Report rep, IStreamGen sg)
		{
			r = rep;
			_sg = sg;					// We need this in future

			tw = new StringWriter();	// will hold the bulk of the RTF until we generate
            _Fonts = new System.Collections.Generic.List<string>();
            _Colors = new System.Collections.Generic.List<System.Drawing.Color>();
        }
        public void Dispose() 
		{
			// These should already be cleaned up; but in case of an unexpected error 
			//   these still need to be disposed of
			if (_bm != null)
				_bm.Dispose();
			if (_g != null)
				_g.Dispose();
		}

		public Report Report()
		{
			return r;
		}

		public bool IsPagingNeeded()
		{
			return false;
		}

		public void Start()		
		{
			return;
		}

		private Draw2.Graphics GetGraphics
		{
			get 
			{
				if (_g == null)
				{
					_bm = new Draw2.Bitmap(10, 10);
					_g = Draw2.Graphics.FromImage(_bm);
				}
				return _g;
			}
		}

		public async Task End()
		{
            TextWriter ftw = _sg.GetTextWriter();	// the final text writer location

            await ftw.WriteAsync(@"{\rtf1\ansi");      // start of the rtf file

            // information group
            PutInformationGroup(ftw);

            // page metrics
            PutPageMetrics(ftw);
            
            // do the font table
            PutFontTable(ftw);
            
            // do the color table
            PutColorTable(ftw);

            // write out the body of the rtf file
            ftw.Write(tw.ToString());
            tw.Close();
            tw = null;
            
            ftw.WriteLine(@"}");            // end of the rtf file

			if (_g != null)
			{
				_g.Dispose();
				_g = null;
			}
			if (_bm != null)
			{
				_bm.Dispose();
				_bm = null;
			}
			return;
		}

        private void PutPageMetrics(TextWriter ftw)
        {
            ftw.Write(@"\paperw{0}\paperh{1}\margl{2}\margr{3}\margt{4}\margb{5}",
                RSize.TwipsFromPoints(r.PageWidthPoints),
                RSize.TwipsFromPoints(r.PageHeightPoints),
                r.ReportDefinition.LeftMargin.Twips,
                r.ReportDefinition.RightMargin.Twips,
                r.ReportDefinition.TopMargin.Twips,
                r.ReportDefinition.BottomMargin.Twips);
        }

        private void PutColorTable(TextWriter ftw)
        {
            ftw.Write(@"{\colortbl;");
            foreach (System.Drawing.Color color in _Colors)
            {
                ftw.Write(@"\red{0}\green{1}\blue{2};", color.R, color.G, color.B);
            }
            ftw.Write("}");     // close out the fonttbl section
        }

        private void PutFontTable(TextWriter ftw)
        {
            ftw.Write(@"{\fonttbl");
            int ifont = 0;
            foreach (string font in _Fonts)
            {
                ftw.Write("{");
                string family = GetFontFamily(font);
                ftw.Write(@"\f{0}\f{1} {2};", ifont, family, font);
                ftw.Write("}");
                ifont++;
            }
            ftw.Write("}");     // close out the fonttbl section
        }

        private void PutInformationGroup(TextWriter ftw)
        {
            ftw.Write(@"{\info");
            if (r.Name != null)
            {
                ftw.Write(@"{\");
                ftw.Write(@"title {0}", r.Name);
                ftw.Write("}");
            }

            if (r.Author != null)
            {
                ftw.Write(@"{\");
                ftw.Write(@"author {0}", r.Author);
                ftw.Write("}");
            }

            if (r.Description != null)
            {
                ftw.Write(@"{\");
                ftw.Write(@"subject {0}", r.Description);
                ftw.Write("}");
            }

            ftw.Write("{");
            DateTime dt = DateTime.Now;
            ftw.Write(@"\creattime\yr{0}\mo{1}\dy{2}\hr{3}\min{4}\sec{5}",
                dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
            ftw.Write("}");

            ftw.Write(@"}");            // end of information group
        }

        private string GetFontFamily(string font)
        {
            //  RTF support following families
            /*
             \fnil Unknown or default fonts (the default) Not applicable
            \froman Roman, proportionally spaced serif fonts Times New Roman, Palatino
            \fswiss Swiss, proportionally spaced sans serif fonts Arial
            \fmodern Fixed-pitch serif and sans serif fonts Courier New, Pica
            \fscript Script fonts Cursive
            \fdecor Decorative fonts Old English, ITC Zapf Chancery
            \ftech Technical, symbol, and mathematical fonts Symbol
            \fbidi Arabic, Hebrew, or other bidirectional font Miriam 
            */

            font = font.ToLowerInvariant();
            if (font.StartsWith("arial") || font.StartsWith("helvetica"))
                return "swiss";
            if (font.StartsWith("times") || font.StartsWith("palatino"))
                return "roman";
            if (font.StartsWith("courier") || font.StartsWith("pica"))
                return "modern";
            if (font.StartsWith("cursive"))
                return "script";
            if (font.StartsWith("old english") || font.StartsWith("itc zapf"))
                return "decor";
            if (font.StartsWith("symbol"))
                return "tech";
            return "nil";
        }

		// Body: main container for the report
		public void BodyStart(Body b)
		{
		}

		public void BodyEnd(Body b)
		{
		}
		
		public void PageHeaderStart(PageHeader ph)
		{
		}

		public void PageHeaderEnd(PageHeader ph)
		{
		}
		
		public void PageFooterStart(PageFooter pf)
		{
		}

		public void PageFooterEnd(PageFooter pf)
		{
		}

		public async Task Textbox(Textbox tb, string t, Row row)
		{
            if (await tb.IsHtml(this.r, row))		    
            {                                   // just do escape chars > 128?
                t = RtfAnsi(t);
            }
            else
            {									
				// make all the characters readable
				t = EscapeText(t);
			}
			// determine if we're in a tablecell
            bool bCell = InTable(tb);

            if (t != "")
            {
                tw.Write("{");
                await DoStyle(tb.Style, row);
                tw.Write(t);
                tw.Write("}");
            }
            if (bCell)
                tw.Write(@"\cell");

		}

        private static bool InTable(ReportItem tb)
        {
            Type tp = tb.Parent.Parent.GetType();
            return (tp == typeof(TableCell));
               //(tp == typeof(TableCell) ||
//                tp == typeof(Corner) ||
//                tp == typeof(DynamicColumns) ||
//                tp == typeof(DynamicRows) ||
//                tp == typeof(StaticRow) ||
//                tp == typeof(StaticColumn) ||
//                tp == typeof(Subtotal) ||
                //tp == typeof(MatrixCell));
            //return bCell;
        }

        private string EscapeText(string s)
        {
			StringBuilder rs = new StringBuilder(s.Length);

			foreach (char c in s)
			{
                if (c == '{' || c == '}' || c == '\\')
                    rs.AppendFormat(@"\{0}", c);
                else if (c == '\n')         // newline?
                    rs.Append(@"\line");
                else if ((int)c <= 127)	// in ANSI range
                    rs.Append(c);
                else
                {
                    rs.AppendFormat(@"\u{0}?", (int)c);
                }
            }
			return rs.ToString();
		}

        private string RtfAnsi(string s)
        {
            StringBuilder rs = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if ((int)c <= 127)   // in ANSI range 
                    rs.Append(c);
                else
                    rs.Append(@"\u" + (int)c + "?");
            }
            return rs.ToString();
        } 

        private async Task DoStyle(Style style, Row row)
        {
            if (style == null)
                return;

            StyleInfo si = await style.GetStyleInfo(r, row);

//            tw.Write(@"\plain");        // reset current attributes

            // Handle the font
            if (!_Fonts.Contains(si.FontFamily))
                _Fonts.Add(si.FontFamily);
            int fc = _Fonts.IndexOf(si.FontFamily);

            tw.Write(@"\f{0} ", fc);

            if (si.IsFontBold())
                tw.Write(@"\b");
            if (si.FontStyle== FontStyleEnum.Italic)
                tw.Write(@"\i");
            switch (si.TextDecoration)
            {
                case TextDecorationEnum.Underline:
                    tw.Write(@"\ul");
                    break;
                case TextDecorationEnum.LineThrough:
                    tw.Write(@"\strike");
                    break;
                default:
                    break;
            }

            tw.Write(@"\fs{0}", (int) Math.Round(si.FontSize * 2,0));        // font size

            // Handle the color
            int ic;
            if (!_Colors.Contains(si.Color))
                _Colors.Add(si.Color);
            ic = _Colors.IndexOf(si.Color)+1;

            tw.Write(@"\cf{0} ", ic);
        }
        
		public async Task DataRegionNoRows(DataRegion d, string noRowsMsg)			// no rows in table
		{
			if (noRowsMsg == null)
				noRowsMsg = "";

			bool bTableCell = d.Parent.Parent.GetType() == typeof(TableCell);

            await DoStyle(d.Style, null);
            tw.Write(noRowsMsg);
			if (bTableCell)
			{
                tw.Write(@"\cell");
			}
		}

		// Lists
		public Task<bool> ListStart(List l, Row r)
		{
			return Task.FromResult(true);
		}

		public Task ListEnd(List l, Row r)
		{
            return Task.CompletedTask;
        }

		public Task ListEntryBegin(List l, Row r)
        {
            return Task.CompletedTask;
        }

		public void ListEntryEnd(List l, Row r)
		{
		}

		// Tables					// Report item table
		public Task<bool> TableStart(Table t, Row row)
		{
            tw.Write(@"\par{");
 
			return Task.FromResult(true);
		}

		public bool IsTableSortable(Table t)
		{
            return false;	// can't have tableGroups; must have 1 detail row
		}

		public Task TableEnd(Table t, Row row)
		{
            tw.Write(@"}");
            return Task.CompletedTask;
		}
 
		public void TableBodyStart(Table t, Row row)
		{
		}

		public void TableBodyEnd(Table t, Row row)
		{
		}

		public void TableFooterStart(Footer f, Row row)
		{
		}

		public void TableFooterEnd(Footer f, Row row)
		{
		}

		public void TableHeaderStart(Header h, Row row)
		{
		}

		public void TableHeaderEnd(Header h, Row row)
		{
		}

		public async Task TableRowStart(TableRow tr, Row row)
		{
            Table t = null;
            Header head = null;
            for (ReportLink rl = tr.Parent.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is Table)
                {
                    t = rl as Table;
                    break;
                }
                else if (rl is Header)
                    head = rl as Header;
            }
            if (t == null)
                return;

            tw.Write(@"\trowd \trql\trgaph108\trrh0\trleft236");
            if (head != null && head.RepeatOnNewPage)       // repeat table header on multiple pages
                tw.Write(@"\trhdr");

            int pos=0;

            int ci=0;
            foreach (TableColumn tc in t.TableColumns)
            {
                pos += tc.Width.Twips;
                string border=@"\clbrdrt\brdrth\clbrdrl\brdrs\clbrdrb\brdrs\clbrdrr\brdrs";
                if (ci < tr.TableCells.Items.Count)
                {
                    ReportItem ri = tr.TableCells.Items[ci].ReportItems[0];
                    if (ri.Style != null)
                    {
                        StyleInfo si = await ri.Style.GetStyleInfo(r, row);
                        border = string.Format(@"\clbrdrt\{0}\clbrdrl\{1}\clbrdrb\{2}\clbrdrr\{3}",
                            GetBorderStyle(si.BStyleTop),
                            GetBorderStyle(si.BStyleLeft),
                            GetBorderStyle(si.BStyleBottom),
                            GetBorderStyle(si.BStyleRight));

                    }
                } 
                tw.Write(@"{1}\cellx{0}", pos, border);
            }
            tw.Write(@"\pard \intbl");
		}

        private string GetBorderStyle(BorderStyleEnum borderStyleEnum)
        {
            string bs;
            /*
\brdrs Single-thickness border.
\brdrth Double-thickness border.
\brdrsh Shadowed border.
\brdrdb Double border.
\brdrdot Dotted border.
\brdrdash Dashed border.
\brdrhair Hairline border.
\brdrinset Inset border.
\brdrdashsm Dashed border (small).
\brdrdashd Dot-dashed border.
\brdrdashdd Dot-dot-dashed border.
\brdroutset Outset border.
             */
            switch (borderStyleEnum)
            {
                case BorderStyleEnum.Dashed:
                    bs = "brdrdash"; break;
                case BorderStyleEnum.Dotted:
                    bs = "brdrdot"; break;
                case BorderStyleEnum.Double:
                    bs = "brdrdb"; break;
                case BorderStyleEnum.Inset:
                    bs = "brdrinset"; break;
                case BorderStyleEnum.None:
                    bs = "brdrnil"; break;
                case BorderStyleEnum.Outset:
                    bs = "brdroutset"; break;
                case BorderStyleEnum.Ridge:
                case BorderStyleEnum.Solid:
                case BorderStyleEnum.Groove:
                default:
                    bs = "brdrs"; break;
            }
            return bs;
        }

		public void TableRowEnd(TableRow tr, Row row)
		{
			tw.WriteLine(@"\row");
		}

		public Task TableCellStart(TableCell t, Row row)
		{
			return Task.CompletedTask;
		}

		public void TableCellEnd(TableCell t, Row row)
		{
			return;
		}

        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)				// called first
		{
            _MatrixCellSpan = 0;
            _MatrixCols = maxCols;
            _MatrixRows = maxRows;
            _MatrixHeaderRows = headerRows;
            _MatrixData = matrix;

            float[] widths = m.ColumnWidths(this._MatrixData, maxCols);
            _MatrixColumnWidths = new int[maxCols];
            for (int i = 0; i < maxCols; i++)
                _MatrixColumnWidths[i] = RSize.TwipsFromPoints(widths[i]);

            tw.Write(@"\par{");
            return Task.FromResult(true);
		}

		public void MatrixColumns(Matrix m, MatrixColumns mc)	// called just after MatrixStart
		{
		}

		public async Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
		{
            _MatrixCellSpan = colSpan;      // save this so that we can put out the right number of \cell 

            if (column != 0)
                return;

            // Handle start of new row
            tw.Write(@"\trowd \trql\trgaph108\trrh0\trleft236");
            if (row < _MatrixHeaderRows)       // repeat table header on multiple pages
                tw.Write(@"\trhdr");

            int pos = 0;

            foreach (int width in _MatrixColumnWidths)
            {
                pos += width;
                string border;
                if (ri != null && ri.Style != null)
                {
                    StyleInfo si = await ri.Style.GetStyleInfo(this.r, r);
                    border = string.Format(@"\clbrdrt\{0}\clbrdrl\{1}\clbrdrb\{2}\clbrdrr\{3}",
                        GetBorderStyle(si.BStyleTop),
                        GetBorderStyle(si.BStyleLeft),
                        GetBorderStyle(si.BStyleBottom),
                        GetBorderStyle(si.BStyleRight));

                }
                else
                    border = @"\clbrdrt\brdrs\clbrdrl\brdrs\clbrdrb\brdrs\clbrdrr\brdrs";
                tw.Write(@"{1}\cellx{0}", pos, border);
            }
            tw.Write(@"\pard \intbl");

        }

        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
		{
            tw.Write(@"\cell");
            return Task.CompletedTask;
		}

		public void MatrixRowStart(Matrix m, int row, Row r)
		{
            // we handle RowStart when the column is 0 so that we have a ReportItem to figure out the border information
        }

		public void MatrixRowEnd(Matrix m, int row, Row r)
		{
			tw.WriteLine(@"\row");
		}

		public Task MatrixEnd(Matrix m, Row r)				// called last
		{
            _MatrixCellSpan = _MatrixCols = _MatrixRows = _MatrixHeaderRows = 0;
            _MatrixData = null;
            _MatrixColumnWidths = null;
            tw.WriteLine(@"}");
            return Task.CompletedTask;
		}

		public Task Chart(Chart c, Row row, ChartBase cb)
		{
           Draw2.Image im = cb.Image(r);
            
            PutImage(im, im.Width, im.Height);
            
            if (InTable(c))
                tw.Write(@"\cell");

            return Task.CompletedTask;
        }
        public Task Image(Image i, Row r, string mimeType, Stream ioin)
        {
            using (Draw2.Image im = Draw2.Image.FromStream(ioin))
            {
                PutImage(im, i.Width == null ? 0 : i.Width.PixelsX, i.Height == null ? 0 : i.Height.PixelsY);
            }

            if (InTable(i))
                tw.Write(@"\cell");
            //switch (i.Sizing)
            //{
            //    case ImageSizingEnum.AutoSize:
            //        break;          // this is right
            //    case ImageSizingEnum.Clip:
            //        break;          // not sure how to clip it    
            //    case ImageSizingEnum.Fit:
            //        if (h > 0)
            //            sw.Write(" height=\"{0}\"", h.ToString());
            //        if (w > 0)
            //            sw.Write(" width=\"{0}\"", w.ToString());
            //        break;
            //    case ImageSizingEnum.FitProportional:
            //        break;          // would have to create an image to handle this
            //}

            return Task.CompletedTask;
        }
        /// <summary>
        /// Put an image stream out.   Use by Chart and Image
        /// </summary>
        /// <param name="ioin"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
		void PutImage(Draw2.Image im, int width, int height)
		{
            MemoryStream ostrm = new MemoryStream();
            ImageFormat imf;
            imf = ImageFormat.Png;
            im.Save(ostrm, imf);
            byte[] ba = ostrm.ToArray();
            ostrm.Close();
            tw.Write('{');
            // convert height/width to twips
            // ???? not sure what picw pich units should really be??
            int h = GetTwipsFromPixels(height <= 0 ? im.Height : height);
            int w = GetTwipsFromPixels(width <= 0 ? im.Width : width);
            int imw_twip = GetTwipsFromPixels(im.Width);
            int imh_twip = GetTwipsFromPixels(im.Height);
            tw.Write(@"\pict\pngblip\picwgoal{2}\pichgoal{3} ",
                w, h, imw_twip, imh_twip);
            //tw.Write(@"\pict\jpegblip\picw423\pich423\picwgoal{2}\pichgoal{3} ",
            //    w, h, imw_twip, imh_twip);

            foreach (byte b in ba)
            {
                tw.Write(HEXCHARS[(byte)((b >> 4) & 0x0f)]);
                tw.Write(HEXCHARS[(byte)(b & 0x0f)]);
            }

            tw.Write('}');
            
            return;
		}

        private int GetTwipsFromPixels(int pixels)
        {
            return (int)Math.Round(RSize.PointsFromPixels(GetGraphics, pixels) * 20, 0);
        }

		public Task Line(Line l, Row r)
		{
			return Task.CompletedTask;
		}

		public Task<bool> RectangleStart(Rdl.Rectangle rect, Row r)
		{
			return Task.FromResult(true);
		}

		public Task RectangleEnd(Rdl.Rectangle rect, Row r)
		{
            return Task.CompletedTask;
        }

		// Subreport:  
		public Task Subreport(Subreport s, Row r)
		{
            return Task.CompletedTask;
        }
		public void GroupingStart(Grouping g)			// called at start of grouping
		{
		}
		public void GroupingInstanceStart(Grouping g)	// called at start for each grouping instance
		{
		}
		public void GroupingInstanceEnd(Grouping g)	// called at start for each grouping instance
		{
		}
		public void GroupingEnd(Grouping g)			// called at end of grouping
		{
		}
		public Task RunPages(Pages pgs)	// we don't have paging turned on for html
		{
            return Task.CompletedTask;
        }
	}
	
}
