
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Drawing2D;
using Majorsilence.Forms;
using System.IO;
using System.Text;
using Majorsilence.Reporting.RdlViewer.Resources;
using Majorsilence.Reporting.Rdl;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlViewer
{
    /// <summary>
    /// PageDrawing draws to a graphics context the loaded Pages class.   This 
    /// class is usually created from running a RDL file thru the renderer.
    /// </summary>
    public class PageDrawing : UserControl
    {
        // RdlEngine's StyleInfo (si.BColorLeft, etc.) is always System.Drawing.Color, even under
        // DRAWINGCOMPAT (Majorsilence.Forms.Drawing.Common -- the NuGet package that replaced the
        // old in-repo Majorsilence.Drawing.Common compat layer -- has no Color type of its own, it
        // reuses System.Drawing.Color directly). This is now an identity conversion, kept as a
        // named bridge so call sites don't need touching if that ever changes again.
        private static System.Drawing.Color ToSysColor(System.Drawing.Color c) => c;

        /// <summary>
        /// Draws text with full justification (stretched inter-word spacing so each line except
        /// the last fills the layout rectangle's width) -- Majorsilence.Forms has no built-in
        /// justify StringAlignment, so this wraps words onto lines itself using MeasureString and
        /// distributes the leftover width evenly across each line's word gaps.
        /// </summary>
        private void DrawStringJustified(Graphics g, string text, Font font, Brush brush, RectangleF layout)
        {
            if (string.IsNullOrEmpty(text)) return;

            var words = text.Split(' ');
            var lines = new List<List<string>>();
            var currentLine = new List<string>();
            float currentWidth = 0f;
            float spaceWidth = g.MeasureString(" ", font).Width;

            foreach (var word in words)
            {
                float wordWidth = g.MeasureString(word, font).Width;
                float extra = currentLine.Count == 0 ? wordWidth : spaceWidth + wordWidth;
                if (currentLine.Count > 0 && currentWidth + extra > layout.Width)
                {
                    lines.Add(currentLine);
                    currentLine = new List<string>();
                    currentWidth = 0f;
                    extra = wordWidth;
                }
                currentLine.Add(word);
                currentWidth += extra;
            }
            if (currentLine.Count > 0) lines.Add(currentLine);

            float y = layout.Top;
            for (int li = 0; li < lines.Count; li++)
            {
                var lineWords = lines[li];
                bool isLastLine = li == lines.Count - 1;
                float lineHeight = g.MeasureString(string.Join(" ", lineWords), font).Height;

                if (isLastLine || lineWords.Count == 1)
                {
                    // Last line (or a single-word line) is left-aligned, not stretched --
                    // matches standard justify semantics (System.Drawing did the same).
                    g.DrawString(string.Join(" ", lineWords), font, brush, new PointF(layout.Left, y));
                }
                else
                {
                    float wordsWidth = 0f;
                    foreach (var w in lineWords) wordsWidth += g.MeasureString(w, font).Width;
                    float totalGap = layout.Width - wordsWidth;
                    float gapPerSpace = totalGap / (lineWords.Count - 1);

                    float x = layout.Left;
                    foreach (var w in lineWords)
                    {
                        g.DrawString(w, font, brush, new PointF(x, y));
                        x += g.MeasureString(w, font).Width + gapPerSpace;
                    }
                }

                y += lineHeight;
                if (y > layout.Bottom) break;
            }
        }

        private Pages _pgs;					// the pages of the report to view

        // During drawing these are set
        float _left;
        float _top;
        float _vScroll;
        float _hScroll;
        float DpiX;
        float DpiY;
        PageItem _HighlightItem = null;
        string _HighlightText = null;
        bool _HighlightCaseSensitive = false;
        bool _HighlightAll = false;
        Color _HighlightItemColor = Color.Aqua;
        Color _HighlightAllColor = Color.Fuchsia;
        Color _SelectItemColor = Color.DarkBlue;

        // Mouse handling
        List<HitListEntry> _HitList;
        float _LastZoom;
        ToolTip _tt;

        // Selection handling
        bool _bHaveMouse = false;       // have we captured the mouse
        bool _bSelect = false;              // use the selection tool
        private Point _ptRBOriginal = new Point();	// starting position of the mouse (rubber banding)
        private Point _ptRBLast = new Point();		//   last position of mouse (rubber banding)
        private Point _MousePosition = new Point();		// position of the mouse
        private List<PageItem> _SelectList;

        public PageDrawing() : this(null)
        {         
            this.DoubleBuffered = true;
        }
        public PageDrawing(Pages pgs)
        {
            this.DoubleBuffered = true;
            // Set up the tooltip
            _tt = new ToolTip();
            _tt.Active = false;
            _tt.ShowAlways = true;

            _HitList = new List<HitListEntry>();
            _SelectList = new List<PageItem>();
            _LastZoom = 1;

            _pgs = pgs;

            // Get our graphics DPI					   
            Graphics ga = null;
            try
            {
                ga = this.CreateGraphics();
                DpiX = ga.DpiX;
                DpiY = ga.DpiY;
            }
            catch
            {
                DpiX = DpiY = 96;
            }
            finally
            {
                if (ga != null)
                    ga.Dispose();
            }
            // force to double buffering for smoother drawing
            //this.SetStyle(ControlStyles.DoubleBuffer | 
            //    ControlStyles.UserPaint | 
            //    ControlStyles.AllPaintingInWmPaint,
            //    true);

            this.DoubleBuffered = true;
        }
        /// <summary>
        /// Enabling the SelectTool allows the user to select text and images.  Enabling or disabling
        /// the SelectTool also clears out the current selection.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool SelectTool
        {
            get { return _bSelect; }
            set
            {
                _bSelect = value;
                _SelectList.Clear();        // clear out the selection list
                this.Invalidate();          // force repaint to create hitlist of items on screen
            }
        }
        internal bool CanCopy
        {
            get
            {
                if (!SelectTool)
                    return false;
                return _SelectList.Count > 0;
            }
        }
        /// <summary>
        /// When the only selected object is a PageImage then SelectImage holds
        /// the Image value; otherwise it will be null;
        /// </summary>
        internal Image SelectImage
        {
            get
            {
                if (!SelectTool || _SelectList.Count != 1)
                    return null;
                PageImage pi = _SelectList[0] as PageImage;
                if (pi == null)
                    return null;

                Stream strm = null;
                Majorsilence.Forms.Drawing.Image im = null;
                try
                {
                    strm = new MemoryStream(pi.GetImageData());
                    im = Majorsilence.Forms.Drawing.Image.FromStream(strm);
                }
                finally
                {
                    if (strm != null)
                        strm.Close();
                }
                return im;
            }
        }
        /// <summary>
        /// The contents of the selected text.  Tab separate items on same y coordinate;
        /// newline separate items when y coordinate changes.   Order is based on user
        /// selection order.
        /// </summary>
        internal string SelectText
        {
            get
            {
                if (!SelectTool || _SelectList.Count == 0)
                    return null;

                StringBuilder sb = new StringBuilder();
                float lastY = float.MinValue;

                _SelectList.Sort(ComparePageItemByPageXY);
                foreach (PageItem pi in _SelectList)
                {
                    PageText pt = pi as PageText;
                    if (pt == null)
                        continue;
                    if (pt.HtmlParent != null)              // if an underlying PageText is selected we'll get it through the PageTextHtml
                        continue;
                    if (lastY != float.MinValue)            // if first time then don't need separator
                    {
                        if (pt.Y == lastY)
                            sb.Append('\t');                // same line put in a tab between values
                        else
                            sb.Append(Environment.NewLine); // force a new line
                    }
                    if (pt is PageTextHtml)
                        SelectTextHtml(pt as PageTextHtml, sb);
                    else
                        sb.Append(pt.Text);
                    lastY = pt.Y;
                }
                return sb.ToString();
            }
        }

        private void SelectTextHtml(PageTextHtml ph, StringBuilder sb)
        {
            bool bFirst = true;
            float lastY = float.MaxValue;
            foreach (PageItem pi in ph)
            {
                if (bFirst)                 // we ignore the contents of the first item
                {
                    bFirst = false;
                    continue;
                }
                PageText pt = pi as PageText;
                if (pt == null)
                    continue;
                if (pt.Y > lastY)           // we've gone to a new line; put a blank in between the text
                    sb.Append(' ');         //   this isn't always ideal: if user was just selecting html text
                //   then they might want to retain the new lines; but when selecting
                //   html page items and other page items new lines affect the table building

                sb.Append(pt.Text);         // append on this text
                lastY = pt.Y;
            }
            return;
        }

        private static int ComparePageItemByPageXY(PageItem pi1, PageItem pi2)
        {
            if (pi1.Page.Count != pi2.Page.Count)
                return pi1.Page.Count - pi2.Page.Count;

            if (pi1.Y != pi2.Y)
            {
                return Convert.ToInt32((pi1.Y - pi2.Y) * 1000);
            }
            return Convert.ToInt32((pi1.X - pi2.X) * 1000);
        }
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Pages Pgs
        {
            get { return _pgs; }
            set { _pgs = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color HighlightItemColor
        {
            get { return _HighlightItemColor; }
            set { _HighlightItemColor = value; }
        }
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal Color HighlightAllColor
        {
            get { return _HighlightAllColor; }
            set { _HighlightAllColor = value; }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left ||
                keyData == Keys.Right ||
                keyData == Keys.Up ||
                keyData == Keys.Down ||
                keyData == Keys.Home ||
                keyData == Keys.End ||
                keyData == Keys.PageDown ||
                keyData == Keys.PageUp)
                return true;
            return base.IsInputKey(keyData);
        }

        /// <summary>
        /// Draw- simple draw of an entire page.  Useful when printing or creating an image.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="page"></param>
        /// <param name="clipRectangle"></param>
        public async Task Draw(Graphics g, int page, System.Drawing.Rectangle clipRectangle, bool drawBackground, PointF pageOffset)
        {
            DpiX = g.DpiX;			 // this can change (e.g. printing graphics context)
            DpiY = g.DpiY;
            _HighlightText = null;
            _HighlightItem = null;
            _HighlightAll = false;
            _HighlightCaseSensitive = false;

            //			g.InterpolationMode = InterpolationMode.HighQualityBilinear;	// try to unfuzz charts
            g.PageUnit = GraphicsUnit.Pixel;
            g.ScaleTransform(1, 1);

            if (!pageOffset.IsEmpty)    // used when correcting for non-printable area on paper
            {
                g.TranslateTransform(pageOffset.X, pageOffset.Y);
            }

            _left = 0;
            _top = 0;
            _hScroll = 0;
            _vScroll = 0;

            RectangleF r = new RectangleF(clipRectangle.X, clipRectangle.Y,
                                            clipRectangle.Width, clipRectangle.Height);

            if (drawBackground)
                g.FillRectangle(Brushes.White, PixelsX(_left), PixelsY(_top),
                    PixelsX(_pgs.PageWidth), PixelsY(_pgs.PageHeight));

            await ProcessPage(g, _pgs[page], r, false);
        }
        /// <summary>
        /// Draw: accounting for scrolling and zoom factors
        /// </summary>
        /// <param name="g"></param>
        /// <param name="zoom"></param>
        /// <param name="leftOffset"></param>
        /// <param name="pageGap"></param>
        /// <param name="hScroll"></param>
        /// <param name="vScroll"></param>
        /// <param name="clipRectangle"></param>
        public async Task Draw(Graphics g, float zoom, float leftOffset, float pageGap,
            float hScroll, float vScroll,
            System.Drawing.Rectangle clipRectangle,
            PageItem highLightItem,
            string highLight, bool highLightCaseSensitive, bool highLightAll)
        {
            // init for mouse handling
            _HitList.Clear();			// remove all items from list
            _LastZoom = zoom;
            _HighlightItem = highLightItem;
            _HighlightText = highLight;
            _HighlightCaseSensitive = highLightCaseSensitive;
            _HighlightAll = highLightAll;

            if (_pgs == null)
            {	// No pages; means nothing to draw
                g.FillRectangle(Brushes.White, clipRectangle);
                return;
            }

            g.PageUnit = GraphicsUnit.Pixel;
            g.ScaleTransform(zoom, zoom);
            DpiX = g.DpiX;
            DpiY = g.DpiY;

            // Zoom affects how much will show on the screen.  Adjust our perceived clipping rectangle
            //  to account for it.
            RectangleF r;
            r = new RectangleF((clipRectangle.X) / zoom, (clipRectangle.Y) / zoom,
                (clipRectangle.Width) / zoom, (clipRectangle.Height) / zoom);

            // Calculate the top of the page
            int fpage = (int)(vScroll / (_pgs.PageHeight + pageGap));
            int lpage = (int)((vScroll + r.Height) / (_pgs.PageHeight + pageGap)) + 1;
            if (fpage >= _pgs.PageCount)
                return;
            if (lpage >= _pgs.PageCount)
                lpage = _pgs.PageCount - 1;

            _hScroll = hScroll;
            _left = leftOffset;
            _top = pageGap;
            // Loop thru the visible pages
            for (int p = fpage; p <= lpage; p++)
            {
                _vScroll = vScroll - p * (_pgs.PageHeight + pageGap);

                System.Drawing.Rectangle pr =
                    new System.Drawing.Rectangle((int)PixelsX(_left - _hScroll), (int)PixelsY(_top - _vScroll),
                                                    (int)PixelsX(_pgs.PageWidth), (int)PixelsY(_pgs.PageHeight));
                g.FillRectangle(Brushes.White, pr);

                await ProcessPage(g, _pgs[p], r, true);

                // Draw the page outline
                // Majorsilence.Forms.Drawing.Pen doesn't implement IDisposable (nothing to
                // release -- it's a thin SkiaSharp-backed value wrapper), so no `using`.
                {
                    Pen pn = new Pen(Brushes.Black, 1);
                    int z3 = Math.Min((int)(3f / zoom), 3);
                    if (z3 <= 0)
                        z3 = 1;
                    int z4 = Math.Min((int)(4f / zoom), 4);
                    if (z4 <= 0)
                        z4 = 1;
                    g.DrawRectangle(pn, pr);					// outline of page
                    g.FillRectangle(Brushes.Black,
                        pr.X + pr.Width, pr.Y + z3, z3, pr.Height);		// right side of page
                    g.FillRectangle(Brushes.Black,
                        pr.X + z3, pr.Y + pr.Height, pr.Width, z4);		// bottom of page
                }
            }
        }

        override protected void OnMouseDown(MouseEventArgs mea)
        {
            base.OnMouseDown(mea);			// allow base to process first
            _MousePosition = new Point(mea.X, mea.Y);

            if (MouseDownRubberBand(mea))
                return;

            HitListEntry hle = this.GetHitListEntry(mea);
            SetHitListCursor(hle);			// set the cursor based on the hit list entry

            if (mea.Button != MouseButtons.Left || hle == null)
                return;

            if (hle.pi.HyperLink != null)
            {
                RdlViewer rv = this.Parent as RdlViewer;
                bool bInvoke = true;
                if (rv != null)
                {
                    HyperlinkEventArgs hlea = new HyperlinkEventArgs(hle.pi.HyperLink);
                    rv.InvokeHyperlink(hlea);     // reset any mousemove
                    bInvoke = !hlea.Cancel;
                }
                try
                {
                    if (bInvoke)
                        System.Diagnostics.Process.Start(hle.pi.HyperLink);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("{3} {0}{1}{2}",
                        hle.pi.HyperLink, Environment.NewLine, ex.Message, Strings.PageDrawing_OnMouseDown_UnableLink), Strings.PageDrawing_OnMouseDown_HyperLinkError);
                }
            }
        }

        private bool MouseDownRubberBand(MouseEventArgs e)
        {
            if (!SelectTool)
                return false;

            if (e.Button != MouseButtons.Left)
            {
                return true;		// well no rubber band but it's been handled
            }

            // We have a rubber band operation
            _bHaveMouse = true;
            // keep the starting point of the rectangular rubber band
            this._ptRBOriginal.X = e.X;
            this._ptRBOriginal.Y = e.Y;
            // -1 indicates no previous rubber band
            this._ptRBLast.X = this._ptRBLast.Y = -1;
            this.Cursor = Cursors.Cross;		// use cross hair to indicate drawing

            return true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!this._bHaveMouse)
                return;

            // Handle the end of the rubber banding
            _bHaveMouse = false;
            // remove last rectangle if necessary
            bool bCtrlOn = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (this._ptRBLast.X != -1)
            {
                this.RubberBand(this._ptRBOriginal, this._ptRBLast);
                // Process the rectangle
                Rectangle r = RectFromPoints(this._ptRBOriginal, this._ptRBLast);
                if (!bCtrlOn)	                // we allow addition to selection
                {
                    _SelectList.Clear();        // remove prior selection when ctrl key not on
                }
                CreateSelectionList(r, bCtrlOn);         // create the selection list
            }
            else
            {   // no last rectangle but use still might have clicked on a pageItem
                if (!bCtrlOn)
                    _SelectList.Clear();        // remove prior selection when ctrl key not on (even when no prior mouse movement)
                Rectangle r = RectFromPoints(this._ptRBOriginal, this._ptRBOriginal);
                CreateSelectionList(r, bCtrlOn);         // create the selection list
            }
            // clear out the points for the next time
            _ptRBOriginal.X = _ptRBOriginal.Y = _ptRBLast.X = _ptRBLast.Y = -1;
            this.Invalidate();      // need to repaint since selection might have changed
        }

        override protected void OnMouseLeave(EventArgs ea)
        {
            _tt.Active = false;
        }

        override protected void OnMouseMove(MouseEventArgs mea)
        {
            if (SelectTool)
            {   // When selection we skip other cursor processing
                if (this.Cursor != Cursors.Cross)
                    this.Cursor = Cursors.Cross;

                if (!_bHaveMouse)
                    return;

                Point newMousePosition = new Point(mea.X, mea.Y);

                // we're rubber banding
                // If we drew previously; we'll draw again to remove old rectangle
                if (this._ptRBLast.X != -1)
                {
                    this.RubberBand(this._ptRBOriginal, _ptRBLast);
                }
                _MousePosition = newMousePosition;
                // Update last point;  but don't rubber band outside our client area
                if (newMousePosition.X < 0)
                    newMousePosition.X = 0;
                if (newMousePosition.X > this.Width)
                    newMousePosition.X = this.Width;
                if (newMousePosition.Y < 0)
                    newMousePosition.Y = 0;
                if (newMousePosition.Y > this.Height)
                    newMousePosition.Y = this.Height;
                _ptRBLast = newMousePosition;
                if (_ptRBLast.X < 0)
                    _ptRBLast.X = 0;
                if (_ptRBLast.Y < 0)
                    _ptRBLast.Y = 0;
                // Draw new lines.
                this.RubberBand(_ptRBOriginal, newMousePosition);
                this.Cursor = Cursors.Cross;		// use cross hair to indicate drawing
                return;
            }

            HitListEntry hle = this.GetHitListEntry(mea);
            SetHitListCursor(hle);
            SetHitListTooltip(hle);
            return;
        }

        // ControlPaint.DrawReversibleFrame doesn't exist in Majorsilence.Forms -- it's a classic
        // GDI+ XOR-mode direct-to-screen drawing technique with no equivalent in a
        // SkiaSharp/compositing-based renderer (most modern UI frameworks don't support XOR
        // drawing at all, since it's incompatible with GPU compositing). The selection *logic*
        // (CreateSelectionList, hit-testing) is entirely separate from this visual feedback and
        // still works correctly -- this is a documented UX-only gap: no live rectangle is drawn
        // while the mouse is being dragged, but releasing the mouse still selects the items
        // under the dragged area. Re-implementing this via the normal (buffered) Paint cycle --
        // tracking the current drag rectangle as state and drawing it in DrawPanelPaint -- is the
        // real fix; not done here for time.
        private void RubberBand(Point p1, Point p2)
        {
        }

        private void CreateSelectionList(Rectangle rc, bool bCtrlOn)
        {
            if (_HitList.Count <= 0)
                return;

            RectangleF rf = new RectangleF(rc.X / _LastZoom, rc.Y / _LastZoom, rc.Width / _LastZoom, rc.Height / _LastZoom);

            try
            {
                foreach (HitListEntry hle in this._HitList)
                {
                    if (!hle.rect.IntersectsWith(rf))
                        continue;
                    bool bInList = _SelectList.Contains(hle.pi);
                    if (bCtrlOn)
                    {
                        if (bInList)       // When ctrl is on we allow user to deselect item in list
                            _SelectList.Remove(hle.pi);
                        else
                            _SelectList.Add(hle.pi);
                    }
                    else
                    {
                        if (!bInList)
                            _SelectList.Add(hle.pi);
                    }
                }
            }
            catch
            {
                // can get synchronization error due to multi-threading; just skip the error
            }
            return;
        }

        private Rectangle RectFromPoints(Point p1, Point p2)
        {
            Rectangle r = new Rectangle();
            // set the width and x of rectangle
            if (p1.X < p2.X)
            {
                r.X = p1.X;
                r.Width = p2.X - p1.X;
            }
            else
            {
                r.X = p2.X;
                r.Width = p1.X - p2.X;
            }
            // set the height and y of rectangle
            if (p1.Y < p2.Y)
            {
                r.Y = p1.Y;
                r.Height = p2.Y - p1.Y;
            }
            else
            {
                r.Y = p2.Y;
                r.Height = p1.Y - p2.Y;
            }
            return r;
        }

        private void SetHitListCursor(HitListEntry hle)
        {
            Cursor c = Cursors.Default;
            if (hle == null)
            { }
            else if (hle.pi.HyperLink != null || hle.pi.BookmarkLink != null)
                c = Cursors.Hand;

            if (this.Cursor != c)
                this.Cursor = c;
        }

        private void SetHitListTooltip(HitListEntry hle)
        {
            if (hle == null || hle.pi.Tooltip == null)
                _tt.Active = false;
            else
            {
                _tt.SetToolTip(this, hle.pi.Tooltip);
                _tt.Active = true;
            }
        }

        private HitListEntry GetHitListEntry(MouseEventArgs mea)
        {
            if (_HitList.Count <= 0)
                return null;

            PointF p = new PointF(mea.X / _LastZoom, mea.Y / _LastZoom);
            try
            {
                foreach (HitListEntry hle in this._HitList)
                {
                    if (hle.Contains(p))
                        return hle;
                }
            }
            catch
            {
                // can get synchronization error due to multi-threading; just skip the error
            }
            return null;
        }

        internal float PixelsX(float x)
        {
            return (float)(x * DpiX / 72.0f);
        }

        internal float PixelsY(float y)
        {
            return (float)(y * DpiY / 72.0f);
        }

        // render all the objects in a page (or any composite object
        private async Task ProcessPage(Graphics g, IEnumerable p, RectangleF clipRect, bool bHitList)
        {
            // TODO: (Peter) Support can grow and can shrink
            foreach (PageItem pi in p)
            {
                if (pi is PageTextHtml)
                {	// PageTextHtml is actually a composite object (just like a page)
                    if (SelectTool && bHitList)
                    {
                        RectangleF hr = new RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll),
                                                                            PixelsX(pi.W), PixelsY(pi.H));
                        _HitList.Add(new HitListEntry(hr, pi));
                    }
                    await ProcessHtml(pi as PageTextHtml, g, clipRect, bHitList);
                    continue;
                }

                if (pi is PageLine)
                {
                    PageLine pl = pi as PageLine;
                    DrawLine(ToSysColor(pl.SI.BColorLeft), pl.SI.BStyleLeft, pl.SI.BWidthLeft,
                        g, PixelsX(pl.X + _left - _hScroll), PixelsY(pl.Y + _top - _vScroll),
                        PixelsX(pl.X2 + _left - _hScroll), PixelsY(pl.Y2 + _top - _vScroll));
                    continue;
                }


                RectangleF rect = new RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll),
                                                                    PixelsX(pi.W), PixelsY(pi.H));

                // Maintain the hit list
                if (bHitList)
                {
                    if (SelectTool)
                    {   // we need all PageText and PageImage items that have been displayed
                        if (pi is PageText || pi is PageImage)
                        {
                            _HitList.Add(new HitListEntry(rect, pi));
                        }
                    }
                    // Only care about items with links and tips
                    else if (pi.HyperLink != null || pi.BookmarkLink != null || pi.Tooltip != null)
                    {
                        HitListEntry hle;
                        if (pi is PagePolygon)
                            hle = new HitListEntry(pi as PagePolygon, _left - _hScroll, _top - _vScroll, this);
                        else
                            hle = new HitListEntry(rect, pi);
                        _HitList.Add(hle);
                    }
                }

                if ((pi is PagePolygon) || (pi is PageCurve))
                { // intentionally empty; polygon's rectangles aren't calculated
                }
                else if (!rect.IntersectsWith(clipRect))
                    continue;

                if (pi.SI.BackgroundImage != null)
                {	// put out any background image
                    PageImage i = pi.SI.BackgroundImage;
                    DrawImageBackground(i, pi.SI, g, rect);
                }

                if (pi is PageText)
                {
                    // TODO: enable can shrink, can grow
                    // 2005 spec file, page 9, in the text box has
                    // CanGrow and CanShrink
                    PageText pt = pi as PageText;
                    DrawString(pt, g, rect);
                }
                else if (pi is PageImage)
                {
                    PageImage i = pi as PageImage;
                    DrawImage(i, g, rect);
                }
                else if (pi is PageRectangle)
                {
                    this.DrawBackground(g, rect, pi.SI);
                }
                else if (pi is PageEllipse)
                {
                    PageEllipse pe = pi as PageEllipse;
                    DrawEllipse(pe, g, rect);
                }
                else if (pi is PagePie)
                {
                    PagePie pp = pi as PagePie;
                    DrawPie(pp, g, rect);
                }
                else if (pi is PagePolygon)
                {
                    PagePolygon ppo = pi as PagePolygon;
                    FillPolygon(ppo, g, rect);
                }
                else if (pi is PageCurve)
                {
                    PageCurve pc = pi as PageCurve;
                    // pc.Points is Majorsilence.Drawing.PointF[] (RdlEngine's DRAWINGCOMPAT
                    // type), not System.Drawing.PointF[].
                    var pts = new PointF[pc.Points.Length];
                    for (int pi2 = 0; pi2 < pc.Points.Length; pi2++)
                        pts[pi2] = new PointF(pc.Points[pi2].X, pc.Points[pi2].Y);
                    DrawCurve(ToSysColor(pc.SI.BColorLeft), pc.SI.BStyleLeft, pc.SI.BWidthLeft,
                        g, pts, pc.Offset, pc.Tension);
                }


                DrawBorder(pi, g, rect);
            }
        }

        private void DrawBackground(Graphics g, System.Drawing.RectangleF rect, StyleInfo si)
        {
            LinearGradientBrush linGrBrush = null;
            SolidBrush sb = null;
            HatchBrush hb = null;
            try
            {
                if (si.BackgroundGradientType != BackgroundGradientTypeEnum.None &&
                    !si.BackgroundGradientEndColor.IsEmpty &&
                    !si.BackgroundColor.IsEmpty)
                {
                    Color c = ToSysColor(si.BackgroundColor);
                    Color ec = ToSysColor(si.BackgroundGradientEndColor);

                    switch (si.BackgroundGradientType)
                    {
                        case BackgroundGradientTypeEnum.LeftRight:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.TopBottom:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                            break;
                        case BackgroundGradientTypeEnum.Center:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.DiagonalLeft:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.ForwardDiagonal);
                            break;
                        case BackgroundGradientTypeEnum.DiagonalRight:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.BackwardDiagonal);
                            break;
                        case BackgroundGradientTypeEnum.HorizontalCenter:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                            break;
                        case BackgroundGradientTypeEnum.VerticalCenter:
                            linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                            break;
                        default:
                            break;
                    }
                }
                if (si.PatternType != patternTypeEnum.None)
                {
                    switch (si.PatternType)
                    {
                        case patternTypeEnum.BackwardDiagonal:
                            hb = new HatchBrush(HatchStyle.BackwardDiagonal, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.CheckerBoard:
                            hb = new HatchBrush(HatchStyle.LargeCheckerBoard, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.Cross:
                            hb = new HatchBrush(HatchStyle.Cross, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.DarkDownwardDiagonal:
                            hb = new HatchBrush(HatchStyle.DarkDownwardDiagonal, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.DarkHorizontal:
                            hb = new HatchBrush(HatchStyle.DarkHorizontal, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.DiagonalBrick:
                            hb = new HatchBrush(HatchStyle.DiagonalBrick, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.HorizontalBrick:
                            hb = new HatchBrush(HatchStyle.HorizontalBrick, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.LargeConfetti:
                            hb = new HatchBrush(HatchStyle.LargeConfetti, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.OutlinedDiamond:
                            hb = new HatchBrush(HatchStyle.OutlinedDiamond, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.SmallConfetti:
                            hb = new HatchBrush(HatchStyle.SmallConfetti, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.SolidDiamond:
                            hb = new HatchBrush(HatchStyle.SolidDiamond, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                        case patternTypeEnum.Vertical:
                            hb = new HatchBrush(HatchStyle.Vertical, ToSysColor(si.Color), ToSysColor(si.BackgroundColor));
                            break;
                    }
                }

                if (linGrBrush != null)
                {
                    g.FillRectangle(linGrBrush, rect);
                    linGrBrush.Dispose();
                }
                else if (hb != null)
                {
                    g.FillRectangle(hb, rect);
                    hb.Dispose();
                }
                else if (!si.BackgroundColor.IsEmpty)
                {
                    sb = new SolidBrush(ToSysColor(si.BackgroundColor));
                    g.FillRectangle(sb, rect);
                    sb.Dispose();
                }
            }
            finally
            {
                if (linGrBrush != null)
                    linGrBrush.Dispose();
                if (sb != null)
                    sb.Dispose();
            }
            return;
        }

        private void DrawBorder(PageItem pi, Graphics g, RectangleF r)
        {
            if (pi.GetType().Name.Equals("PagePie")) return;
            if (r.Height <= 0 || r.Width <= 0)		// no bounding box to use
                return;

            StyleInfo si = pi.SI;

            DrawLine(ToSysColor(si.BColorTop), si.BStyleTop, si.BWidthTop, g, r.X, r.Y, r.Right, r.Y);

            DrawLine(ToSysColor(si.BColorRight), si.BStyleRight, si.BWidthRight, g, r.Right, r.Y, r.Right, r.Bottom);

            DrawLine(ToSysColor(si.BColorLeft), si.BStyleLeft, si.BWidthLeft, g, r.X, r.Y, r.X, r.Bottom);

            DrawLine(ToSysColor(si.BColorBottom), si.BStyleBottom, si.BWidthBottom, g, r.X, r.Bottom, r.Right, r.Bottom);

            return;

        }

        private void DrawImage(PageImage pi, Graphics g, RectangleF r)
        {
            Stream strm = null;
            Majorsilence.Forms.Drawing.Image im = null;
            try
            {
                strm = new MemoryStream(pi.GetImageData((int)r.Width, (int)r.Height));
                im = Majorsilence.Forms.Drawing.Image.FromStream(strm);
                DrawImageSized(pi, im, g, r);
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }

        }
        private void DrawImageBackground(PageImage pi, StyleInfo si, Graphics g, RectangleF r)
        {
            Stream strm = null;
            Majorsilence.Forms.Drawing.Image im = null;
            try
            {
                // http://www.fyireporting.com/forum/viewtopic.php?t=892
                //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 

                RectangleF r2;
                if (g.PageUnit == GraphicsUnit.Pixel)
                {
                    r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                    r.Top + (si.PaddingTop * g.DpiX) / 72,
                    r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                    r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
                }
                else
                {
                    // adjust drawing rectangle based on padding
                    r2 = new RectangleF(r.Left + si.PaddingLeft,
                    r.Top + si.PaddingTop,
                    r.Width - si.PaddingLeft - si.PaddingRight,
                    r.Height - si.PaddingTop - si.PaddingBottom);
                }

                strm = new MemoryStream(pi.GetImageData((int)r2.Width, (int)r2.Height));
                im = Majorsilence.Forms.Drawing.Image.FromStream(strm);

                int repeatX = 0;
                int repeatY = 0;
                switch (pi.Repeat)
                {
                    case ImageRepeat.Repeat:
                        repeatX = (int)Math.Floor(r2.Width / pi.SamplesW);
                        repeatY = (int)Math.Floor(r2.Height / pi.SamplesH);
                        break;
                    case ImageRepeat.RepeatX:
                        repeatX = (int)Math.Floor(r2.Width / pi.SamplesW);
                        repeatY = 1;
                        break;
                    case ImageRepeat.RepeatY:
                        repeatY = (int)Math.Floor(r2.Height / pi.SamplesH);
                        repeatX = 1;
                        break;
                    case ImageRepeat.NoRepeat:
                    default:
                        repeatX = repeatY = 1;
                        break;
                }

                //make sure the image is drawn at least 1 times 
                repeatX = Math.Max(repeatX, 1);
                repeatY = Math.Max(repeatY, 1);

                float startX = r2.Left;
                float startY = r2.Top;

                Region saveRegion = g.Clip;
                Region clipRegion = g.Clip.Clone();
                clipRegion.Intersect(r2);
                g.Clip = clipRegion;

                for (int i = 0; i < repeatX; i++)
                {
                    for (int j = 0; j < repeatY; j++)
                    {
                        float currX = startX + i * pi.SamplesW;
                        float currY = startY + j * pi.SamplesH;
                        g.DrawImage(im, new RectangleF(currX, currY, pi.SamplesW, pi.SamplesH));
                    }
                }
                g.Clip = saveRegion;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }
        }
        private void DrawImageSized(PageImage pi, Image im, Graphics g, RectangleF r)
        {
            float height, width;		// some work variables
            StyleInfo si = pi.SI;

            // adjust drawing rectangle based on padding

            // http://www.fyireporting.com/forum/viewtopic.php?t=892
            //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 

            RectangleF r2;
            if (g.PageUnit == GraphicsUnit.Pixel)
            {
                r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                r.Top + (si.PaddingTop * g.DpiX) / 72,
                r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
            }
            else
            {
                // adjust drawing rectangle based on padding
                r2 = new RectangleF(r.Left + si.PaddingLeft,
                r.Top + si.PaddingTop,
                r.Width - si.PaddingLeft - si.PaddingRight,
                r.Height - si.PaddingTop - si.PaddingBottom);
            } 

            Rectangle ir;	// int work rectangle
            ir = new Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                               Convert.ToInt32(r2.Width), Convert.ToInt32(r2.Height));
            switch (pi.Sizing)
            {
                case ImageSizingEnum.AutoSize:
                    // Note: GDI+ will stretch an image when you only provide
                    //  the left/top coordinates.  This seems pretty stupid since
                    //  it results in the image being out of focus even though
                    //  you don't want the image resized.
                    if (g.DpiX == im.HorizontalResolution &&
                        g.DpiY == im.VerticalResolution)
                    {
                        ir = new Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                                                        im.Width, im.Height);
                    }
                    g.DrawImage(im, ir);

                    break;
                case ImageSizingEnum.Clip:
                    Region saveRegion = g.Clip;
                    Region clipRegion = g.Clip.Clone();
                    clipRegion.Intersect(r2);
                    g.Clip = clipRegion;
                    if (g.DpiX == im.HorizontalResolution &&
                        g.DpiY == im.VerticalResolution)
                    {
                        ir = new Rectangle(Convert.ToInt32(r2.Left), Convert.ToInt32(r2.Top),
                                                        im.Width, im.Height);
                    }
                    g.DrawImage(im, ir);
                    g.Clip = saveRegion;
                    break;
                case ImageSizingEnum.FitProportional:
                    float ratioIm = (float)im.Height / (float)im.Width;
                    float ratioR = r2.Height / r2.Width;
                    height = r2.Height;
                    width = r2.Width;
                    if (ratioIm > ratioR)
                    {	// this means the rectangle width must be corrected
                        width = height * (1 / ratioIm);
                    }
                    else if (ratioIm < ratioR)
                    {	// this means the ractangle height must be corrected
                        height = width * ratioIm;
                    }
                    r2 = new RectangleF(r2.X, r2.Y, width, height);
                    g.DrawImage(im, r2);
                    break;
                case ImageSizingEnum.Fit:
                default:
                    g.DrawImage(im, r2);
                    break;
            }

            if (SelectTool && pi.AllowSelect && _SelectList.Contains(pi))
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(50, _SelectItemColor)), ir);
            }

            return;
        }

        private void DrawLine(Color c, BorderStyleEnum bs, float w, Graphics g,
                                float x, float y, float x2, float y2)
        {
            if (bs == BorderStyleEnum.None || c.IsEmpty || w <= 0)	// nothing to draw
                return;

            float widthPen = w;
            if (g.PageUnit == GraphicsUnit.Pixel)
                widthPen=Rdl.Utility.Measurement.PixelsFromPoints(w, g.DpiX);
            {
                Pen p = new Pen(c, widthPen);
                switch (bs)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }

                // No (Pen, float, float, float, float) overload exists -- only (Pen, Point, Point)
                // and (Pen, PointF, PointF).
                g.DrawLine(p, new PointF(x, y), new PointF(x2, y2));
            }

        }

       
        private void DrawCurve(Color c, BorderStyleEnum bs, float w, Graphics g,
                                PointF[] points, int Offset, float Tension)
        {
            if (bs == BorderStyleEnum.None || c.IsEmpty || w <= 0)	// nothing to draw
                return;

            Pen p = null;
            try
            {
                p = new Pen(c, w);
                switch (bs)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                PointF[] tmp = new PointF[points.Length];
                for (int i = 0; i < points.Length; i++)
                {

                    tmp[i].X = PixelsX(points[i].X + _left - _hScroll);
                    tmp[i].Y = PixelsY(points[i].Y + _top - _vScroll);
                }

                g.DrawCurve(p, tmp, Offset, tmp.Length - 1, Tension);
            }
            finally
            {
                p?.Dispose();
            }

        }


        private async Task ProcessHtml(PageTextHtml pth, Graphics g, RectangleF clipRect, bool bHitList)
        {
            // PageTextHtml.Build wants Majorsilence.Forms.Drawing.Graphics (RdlEngine's own
            // DRAWINGCOMPAT type, used elsewhere in the engine for HTML layout/measurement), not
            // Majorsilence.Forms.Graphics. It only needs this for text-measurement during layout,
            // not for actual drawing (the real draw happens below via ProcessPage using the real
            // screen/print `g`), so a throwaway 1x1 bitmap-backed Graphics is enough.
            using (var measureBitmap = new Majorsilence.Forms.Drawing.Bitmap(1, 1))
            using (var measureGraphics = Majorsilence.Forms.Drawing.Graphics.FromImage(measureBitmap))
            {
                await pth.Build(measureGraphics); // Builds the subobjects that make up the html
            }
            await this.ProcessPage(g, pth, clipRect, bHitList);
        }

        private void DrawEllipse(PageEllipse pe, Graphics g, RectangleF r)
        {
            StyleInfo si = pe.SI;
            if (!si.BackgroundColor.IsEmpty)
            {
                g.FillEllipse(new SolidBrush(ToSysColor(si.BackgroundColor)), r);
            }
            if (si.BStyleTop != BorderStyleEnum.None)
            {
                Pen p = new Pen(ToSysColor(si.BColorTop), si.BWidthTop);
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                g.DrawEllipse(p, r);
            }
        }

        private void FillPolygon(PagePolygon pp, Graphics g, RectangleF r)
        {

            StyleInfo si = pp.SI;
            PointF[] tmp = new PointF[pp.Points.Length];
            if (!si.BackgroundColor.IsEmpty)
            //RectangleF(PixelsX(pi.X + _left - _hScroll), PixelsY(pi.Y + _top - _vScroll), 
            //                                                                    PixelsX(pi.W), PixelsY(pi.H))           
            {
                for (int i = 0; i < pp.Points.Length; i++)
                {

                    tmp[i].X = PixelsX(pp.Points[i].X + _left - _hScroll);
                    tmp[i].Y = PixelsY(pp.Points[i].Y + _top - _vScroll);
                }
                g.FillPolygon(new SolidBrush(ToSysColor(si.BackgroundColor)), tmp);
            }
        }

        private void DrawPie(PagePie pp, Graphics g, RectangleF r)
        {
            StyleInfo si = pp.SI;
            if (!si.BackgroundColor.IsEmpty)
            {
                g.FillPie(new SolidBrush(ToSysColor(si.BackgroundColor)), (int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, (float)pp.StartAngle, (float)pp.SweepAngle);
            }

            if (si.BStyleTop != BorderStyleEnum.None)
            {
                Pen p = new Pen(ToSysColor(si.BColorTop), si.BWidthTop);
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        p.DashStyle = DashStyle.Dash;
                        break;
                    case BorderStyleEnum.Dotted:
                        p.DashStyle = DashStyle.Dot;
                        break;
                    case BorderStyleEnum.Double:
                    case BorderStyleEnum.Groove:
                    case BorderStyleEnum.Inset:
                    case BorderStyleEnum.Solid:
                    case BorderStyleEnum.Outset:
                    case BorderStyleEnum.Ridge:
                    case BorderStyleEnum.WindowInset:
                    default:
                        p.DashStyle = DashStyle.Solid;
                        break;
                }
                g.DrawPie(p, Rectangle.Round(r), pp.StartAngle, pp.SweepAngle);
            }
        }

        private void DrawString(PageText pt, Graphics g, RectangleF r)
        {
            StyleInfo si = pt.SI;
            string s = pt.Text;

            Font drawFont = null;
            StringFormat drawFormat = null;
            Brush drawBrush = null;
            try
            {
                // STYLE
                Majorsilence.Forms.Drawing.FontStyle fs = 0;
                if (si.FontStyle == FontStyleEnum.Italic)
                    fs |= Majorsilence.Forms.Drawing.FontStyle.Italic;

                switch (si.TextDecoration)
                {
                    case TextDecorationEnum.Underline:
                        fs |= Majorsilence.Forms.Drawing.FontStyle.Underline;
                        break;
                    case TextDecorationEnum.LineThrough:
                        fs |= Majorsilence.Forms.Drawing.FontStyle.Strikeout;
                        break;
                    case TextDecorationEnum.Overline:
                    case TextDecorationEnum.None:
                        break;
                }

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
                        fs |= Majorsilence.Forms.Drawing.FontStyle.Bold;
                        break;
                    default:
                        break;
                }
                try
                {
                    // si.GetFontFamily() is Majorsilence.Drawing.FontFamily (RdlEngine's own
                    // DRAWINGCOMPAT type), not Majorsilence.Forms.Drawing.FontFamily -- use its
                    // .Name against the (string, float, FontStyle) overload instead.
                    drawFont = new Font(si.GetFontFamily().Name, si.FontSize, fs);	// si.FontSize already in points
                }
                catch (ArgumentException)
                {
                    drawFont = new Font("Arial", si.FontSize, fs);	// if this fails we'll let the error pass thru
                }
                // ALIGNMENT
                drawFormat = new StringFormat();
                switch (si.TextAlign)
                {
                    case TextAlignEnum.Right:
                        drawFormat.Alignment = StringAlignment.Far;
                        break;
                    case TextAlignEnum.Center:
                        drawFormat.Alignment = StringAlignment.Center;
                        break;
                    case TextAlignEnum.Left:
                    default:
                        drawFormat.Alignment = StringAlignment.Near;
                        break;
                }
                if (pt.SI.WritingMode == WritingModeEnum.tb_rl)
                {
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
                }
                switch (si.VerticalAlign)
                {
                    case VerticalAlignEnum.Bottom:
                        drawFormat.LineAlignment = StringAlignment.Far;
                        break;
                    case VerticalAlignEnum.Middle:
                        drawFormat.LineAlignment = StringAlignment.Center;
                        break;
                    case VerticalAlignEnum.Top:
                    default:
                        drawFormat.LineAlignment = StringAlignment.Near;
                        break;
                }
                // draw the background 
                DrawBackground(g, r, si);

                // adjust drawing rectangle based on padding
                // http://www.fyireporting.com/forum/viewtopic.php?t=892
                //A.S.> convert pt to px if needed(when printing we need px, when draw preview - pt) 
                RectangleF r2;
                if (g.PageUnit == GraphicsUnit.Pixel)
                {
                    r2 = new RectangleF(r.Left + (si.PaddingLeft * g.DpiX) / 72,
                    r.Top + (si.PaddingTop * g.DpiX) / 72,
                    r.Width - ((si.PaddingLeft + si.PaddingRight) * g.DpiX) / 72,
                    r.Height - ((si.PaddingTop + si.PaddingBottom) * g.DpiX) / 72);
                }
                else
                {
                    // adjust drawing rectangle based on padding
                   r2 = new RectangleF(r.Left + si.PaddingLeft,
                   r.Top + si.PaddingTop,
                   r.Width - si.PaddingLeft - si.PaddingRight,
                   r.Height - si.PaddingTop - si.PaddingBottom);
                } 

                drawBrush = new SolidBrush(ToSysColor(si.Color));
                if (si.TextAlign == TextAlignEnum.Justified)
                {
                    // RdlEngine's own GraphicsExtended.DrawStringJustified helper is typed
                    // against System.Drawing/Majorsilence.Drawing (RdlEngine's own DRAWINGCOMPAT
                    // types), not Majorsilence.Forms.Graphics -- a third, unrelated Graphics type
                    // -- so it can't be reused here. Justify manually instead: distribute words
                    // across the line width by stretching the inter-word gaps, one line at a time.
                    DrawStringJustified(g, pt.Text, drawFont, drawBrush, r2);
                }
                else if (pt.NoClip)	// request not to clip text
                {
                    g.DrawString(pt.Text, drawFont, drawBrush, new PointF(r.Left, r.Top), drawFormat);
                    HighlightString(g, pt, new RectangleF(r.Left, r.Top, float.MaxValue, float.MaxValue),
                        drawFont, drawFormat);
                }
                else
                {
                    g.DrawString(pt.Text, drawFont, drawBrush, r2, drawFormat);
                    HighlightString(g, pt, r2, drawFont, drawFormat);
                }
                if (SelectTool)
                {
                    if (pt.AllowSelect && _SelectList.Contains(pt))
                        g.FillRectangle(new SolidBrush(Color.FromArgb(50, _SelectItemColor)), r2);
                }
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawFormat != null)
                    drawFont.Dispose();
                if (drawBrush != null)
                    drawBrush.Dispose();
            }
        }

        private void HighlightString(Graphics g, PageText dtext, RectangleF r, Font f, StringFormat sf)
        {
            if (_HighlightText == null || _HighlightText.Length == 0)
                return;         // nothing to highlight
            bool bhighlightItem = dtext == _HighlightItem ||
                    (_HighlightItem != null && dtext.HtmlParent == _HighlightItem);
            if (!(_HighlightAll || bhighlightItem))
                return;         // not highlighting all and not on current highlight item

            string hlt = _HighlightCaseSensitive ? _HighlightText : _HighlightText.ToLower();
            string text = _HighlightCaseSensitive ? dtext.Text : dtext.Text.ToLower();

            if (text.IndexOf(hlt) < 0)
                return;         // string not in text

            // Majorsilence.Forms.Drawing.StringFormat has no SetMeasurableCharacterRanges, and
            // Graphics has no MeasureCharacterRanges/FillRegion -- the exact GDI+ mechanism this
            // used to compute per-substring highlight regions across wrapped, multi-line text
            // doesn't exist. Approximate instead: measure the text width before each match and
            // the match itself with MeasureString, and draw a highlight rectangle at that X
            // offset. This is accurate for single-line text (the common case for report
            // textboxes) but won't correctly split a highlight that straddles a line wrap in a
            // CanGrow multi-line textbox -- a known, documented fidelity loss.
            try
            {
                if (r.Width <= 0 || r.Height <= 0)
                {
                    SizeF ts = g.MeasureString(dtext.Text, f);
                    r.Height = ts.Height;
                    r.Width = ts.Width;
                }

                int hlen = hlt.Length;
                int len = text.Length;
                int loc = text.IndexOf(hlt);
                Color hl = bhighlightItem ? _HighlightItemColor : _HighlightAllColor;
                using var highlightBrush = new SolidBrush(Color.FromArgb(50, hl));

                while (loc >= 0)
                {
                    string before = dtext.Text.Substring(0, loc);
                    string match = dtext.Text.Substring(loc, hlen);
                    float xOffset = g.MeasureString(before, f).Width;
                    float matchWidth = g.MeasureString(match, f).Width;

                    g.FillRectangle(highlightBrush, new RectangleF(r.Left + xOffset, r.Top, matchWidth, r.Height));

                    if (loc + hlen < len)
                        loc = text.IndexOf(hlt, loc + hlen);
                    else
                        loc = -1;
                }
            }
            catch { }   // if highlighting fails we don't care; need to continue
        }

        class HitListEntry
        {
            internal RectangleF rect;
            internal PageItem pi;
            internal PointF[] poly;
            internal HitListEntry(RectangleF r, PageItem pitem)
            {
                rect = r;
                pi = pitem;
                poly = null;
            }
            internal HitListEntry(PagePolygon pp, float x, float y, PageDrawing pd)
            {
                pi = pp;
                poly = new PointF[pp.Points.Length];
                for (int i = 0; i < pp.Points.Length; i++)
                {
                    poly[i].X = pd.PixelsX(pp.Points[i].X + x);
                    poly[i].Y = pd.PixelsY(pp.Points[i].Y + y);
                }
                rect = RectangleF.Empty;
            }
            /// <summary>
            /// Contains- determine whether point in the pageitem
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            internal bool Contains(PointF p)
            {
                return (pi is PagePolygon) ? PointInPolygon(p) : rect.Contains(p);
            }

            /// <summary>
            /// PointInPolygon: uses ray casting algorithm ( http://en.wikipedia.org/wiki/Point_in_polygon )
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            bool PointInPolygon(PointF p)
            {
                PointF p1, p2;
                bool bIn = false;
                if (poly.Length < 3)
                {
                    return false;
                }

                PointF op = new PointF(poly[poly.Length - 1].X, poly[poly.Length - 1].Y);
                for (int i = 0; i < poly.Length; i++)
                {
                    PointF np = new PointF(poly[i].X, poly[i].Y);
                    if (np.X > op.X)
                    {
                        p1 = op;
                        p2 = np;
                    }
                    else
                    {
                        p1 = np;
                        p2 = op;
                    }

                    if ((np.X < p.X) == (p.X <= op.X)
                        && (p.Y - p1.Y) * (p2.X - p1.X) < (p2.Y - p1.Y) * (p.X - p1.X))
                    {
                        bIn = !bIn;
                    }
                    op = np;
                }
                return bIn;
            }

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PageDrawing
            // 
            this.Name = "PageDrawing";
            this.ResumeLayout(false);

        }
    }
}
