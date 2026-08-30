
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Drawing2D;
using Majorsilence.Forms.Drawing.Imaging;
using Majorsilence.Forms;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Net;
using System.Text;
using RdlMapFile.Resources;
using System.ComponentModel;

namespace Majorsilence.Reporting.RdlMapFile
{

	/// <summary>
	/// Control for providing a designer image of RDL.   Works directly off the RDL XML.
	/// </summary>
	internal class DesignXmlDraw: UserControl
	{
        public delegate void DrawEventHandler(DesignXmlDraw dxd);
        /// <summary>
        /// Hyperlink invoked when report item with hyperlink is clicked on.
        /// </summary>
        public event DrawEventHandler ZoomChange;
        public event DrawEventHandler XmlChange;
        public event DrawEventHandler SelectionChange;
        public event DrawEventHandler ToolChange;

		private XmlDocument _MapDoc;			// the map file XML document
        private Undo _Undo;					//  the undo object tied to the _MapDoc;
        private string _File=null;                   // the file name of the XML document; null if untitled
        private Point _MouseLocation;

        private Image _BackImage = null;
		float DpiX;
		float DpiY;

		// During drawing these are set
		Graphics g;
        private VScrollBar vScrollBar;
        private HScrollBar hScrollBar;
        private float _Zoom = 1;
        private List<XmlNode> _SelectedList = new List<XmlNode>();
        private bool _Modified=false;
        private ToolStripMenuItem _menuCopy=null;
        private ToolStripMenuItem _menuPaste =null;

        private ToolMode _Tool = ToolMode.Selection;
        internal enum ToolMode                  // The tool mode affect how mouse processing is handled
        {
            Selection,
            InsertPolygon,
            InsertLine,
            InsertText
        }

		internal DesignXmlDraw():base()
		{
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

            InitializeComponent();
			
			// force to double buffering for smoother drawing
			this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.UserPaint | 
				ControlStyles.AllPaintingInWmPaint,
				true);
        }
        internal XmlDocument MapDoc
        {
            get { return _MapDoc; }
        }
        
        public string MapSource
        {
            get
            {
                if (_MapDoc == null)
                    return null;

                // Convert the document into a string
                StringWriter sw = new StringWriter();
                XmlTextWriter xtw = new XmlTextWriter(sw);
                xtw.IndentChar = ' ';
                xtw.Indentation = 2;
                xtw.Formatting = Formatting.Indented;

                _MapDoc.WriteContentTo(xtw);
                xtw.Close();
                sw.Close();
                string result = sw.ToString();
                result = result.Replace("xmlns=\"\"", "");
                return result;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal string File
        {
            get { return _File; }
            set { _File = value; }
        }

        internal void SelectAll()
        {
            if (_MapDoc == null)
                return;

            _SelectedList.Clear();
            foreach (XmlNode xNodeLoop in GetRoot().ChildNodes)
                _SelectedList.Add(xNodeLoop);

            SignalSelectionChanged();
            this.Invalidate();
        }

        internal void SelectByKey(List<string> select)
        {
            if (_MapDoc == null)
                return;

            _SelectedList.Clear();

            foreach (XmlNode xNodeLoop in GetRoot().ChildNodes)
            {
                if (xNodeLoop.Name != "Polygon")
                    continue;

                string[] skeys = GetKeysInPolygon(xNodeLoop);
                foreach (string k in skeys)
                {
                    if (select.BinarySearch(k) >= 0)
                    {
                        _SelectedList.Add(xNodeLoop);
                        break;
                    }
                }
            }

            SignalSelectionChanged();
            this.Invalidate();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal ToolMode Tool
        {
            get { return _Tool; }
            set 
            {
                _Tool = value;
                if (_Tool != ToolMode.Selection)
                {
                    _SelectedList.Clear();
                    SignalSelectionChanged();                    
                    this.Invalidate();
                }   
            }
        }

#region Undo
        internal bool CanUndo
        {
            get 
            {
                return _Undo == null? false: _Undo.CanUndo; 
            }
        }
        internal void ClearUndo()
        {
            _Undo.Reset();
        }
        internal string UndoText
        {
            get { return _Undo == null? "": _Undo.Description; }
        }
        internal void Undo()
        {
            if (_Undo == null)
                return;
            _Undo.undo();

            // We need to validate the selected list
            XmlNode root = GetRoot();
            foreach (XmlNode sn in _SelectedList)
            {
                bool bFound = false;
                foreach (XmlNode n in root.ChildNodes)
                {
                    if (sn == n)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (!bFound)
                {
                    _SelectedList.Clear();
                    SignalSelectionChanged();
                    break;
                }
            }
            this.Invalidate();
        }

        internal void StartUndoGroup(String description)
        {
            if (_Undo == null)
                return;
            _Undo.StartUndoGroup(description);
        }

        internal void EndUndoGroup()
        {
            if (_Undo == null)
                return;
            _Undo.EndUndoGroup();
        }	
        #endregion

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Modified
        {
            get { return _Modified; }
            set { _Modified = value; }
        }

        public bool CanPaste()
        {
            if (MapDoc == null)
                return false;

            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null)
                return false;
            if (!iData.GetDataPresent(DataFormats.Text))
                return false;
            // Build the xml string in case it is a straight pasting of text
            string t = (string)iData.GetData(DataFormats.Text);
            if (!(t.StartsWith("<MapItems>") && t.EndsWith("</MapItems>")))
                return false;

            return true;
        }
        public void Copy()
        {
            if (SelectedList.Count == 0)
                return;
            StringBuilder sb = new StringBuilder("<MapItems>");
            foreach (XmlNode xn in SelectedList)
            {
                sb.Append(xn.OuterXml);
            }
            sb.Append("</MapItems>");
            Clipboard.SetDataObject(sb.ToString(), true);
        }

        /// <summary>
        /// Paste the contents of the clipboard into the map
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool Paste(Point offset)
        {
            if (MapDoc == null)
                return false;

            IDataObject iData = Clipboard.GetDataObject();
            if (iData == null)
                return false;
            if (!iData.GetDataPresent(DataFormats.Text))
                return false;
            // Build the xml string in case it is a straight pasting of text
            string t = (string)iData.GetData(DataFormats.Text);
            return Paste(offset, t);
        }

        public bool Paste(Point offset, string t)
        {
            if (!(t.StartsWith("<MapItems>") && t.EndsWith("</MapItems>")))
                return false;

            XmlDocumentFragment fDoc = MapDoc.CreateDocumentFragment();
            fDoc.InnerXml = t;

            // Get the node we need to paste into
            XmlNode mNode = GetNamedChildNode(fDoc, "MapItems");
            if (mNode == null)
                return false;

            // We need to normalize the positions in the map items. Simple as 1, 2, 3
            // 1) Find the left most object and the top most object
            // 2) Adjust all objects positions
            // 3) Move the nodes into the proper ReportItems collection

            // 1) Find the left most and top most objects
            int leftMost = int.MaxValue;
            int topMost = int.MaxValue;
            foreach (XmlNode mi in mNode.ChildNodes)
            {
                if (mi.NodeType != XmlNodeType.Element)
                    continue;
                switch (mi.Name)
                {
                    case "Polygon":
                    case "Lines":
                        Point[] pts = GetPolygon(mi, true);
                        foreach (Point pt in pts)
                        {
                            if (leftMost > pt.X)
                                leftMost = pt.X;
                            if (topMost > pt.Y)
                                topMost = pt.Y;
                        }
                        break;
                    case "Text":
                        Point p = GetTextPoint(mi, true);
                        if (leftMost > p.X)
                            leftMost = p.X;
                        if (topMost > p.Y)
                            topMost = p.Y;
                        break;
                    default:
                        break;
                }
            }
            // 2) Adjust all objects positions
            foreach (XmlNode mi in mNode.ChildNodes)
            {
                if (mi.NodeType != XmlNodeType.Element)
                    continue;
                switch (mi.Name)
                {
                    case "Polygon":
                    case "Lines":
                        Point[] pts = GetPolygon(mi, true);
                        for (int i = 0; i < pts.Length; i++ )
                        {
                            pts[i].X = pts[i].X - leftMost + offset.X;
                            pts[i].Y = pts[i].Y - topMost + offset.Y;
                        }
                        SetPoints(mi, pts);
                        break;
                    case "Text":
                        Point pt = GetTextPoint(mi, true);
                        pt.X = pt.X - leftMost + offset.X;
                        pt.Y = pt.Y - topMost + offset.Y;
                        string loc = string.Format("{0},{1}", pt.X, pt.Y);
                        this.SetElement(mi, "Location", loc);
                        break;
                    default:
                        break;
                }
            }

            // 3) Move the nodes into the main collection
            // Loop thru and put all the map items into the main document
            // This loop is a little strange because when a node is appended to
            //   the main document it is removed from the fragment.   Thus you
            //   must continually grab the first child until all the children have
            //   been removed.
            _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_Paste);
            _SelectedList.Clear();		// the new nodes end up selected
            XmlNode root = GetRoot();
            for (XmlNode mi = mNode.FirstChild; mi != null; mi = mNode.FirstChild)
            {
                root.AppendChild(mi);
                _SelectedList.Add(mi);
            }
            SignalXmlChanged();
            SignalSelectionChanged();
            Invalidate();
            _Undo.EndUndoGroup();
            return true;
        }

        public int ReducePointCount()
        {
            int rcount = 0;
            bool bFirst = true;
            foreach (XmlNode pn in SelectedList)
            {
                if (pn.Name != "Polygon")
                    continue;

                Point[] pts = GetPolygon(pn, true);         // get polygon points unadjusted by scroll
                if (pts == null || pts.Length <= 2)
                    continue;
                DPSimp dps = new DPSimp(pts);
                Point[] res = dps.GetDouglasPeuckerSimplified();
                if (pts.Length == res.Length)
                    continue;
                if (bFirst)
                {
                    _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_ReducePointCount);    // we have at least one change
                    bFirst = false;
                }
                rcount += (pts.Length - res.Length);
                dps = null;
                SetPoints(pn, res);
            }
            if (!bFirst)
            {
                _Undo.EndUndoGroup(true);
                SignalXmlChanged();
                this.Invalidate();
            }
            return rcount;
        }

        public void SizeSelected(float zoom)
        {
            bool bFirst = true;
            Point anchor=Point.Empty;
            List<XmlNode> empties = new List<XmlNode>();
            foreach (XmlNode pn in SelectedList)
            {
                if (pn.Name != "Polygon")
                    continue;

                Point[] pts = GetPolygon(pn, true);         // get polygon points unadjusted by scroll
                List<Point> ptsl = new List<Point>(pts.Length);
                if (bFirst)
                    anchor = new Point(pts[0].X, pts[0].Y);   // anchor point is first point in first selected polygon
                // Build a new list with zoomed polygons (sans duplicate points)
                ptsl.Add(anchor);
                for (int pi = 1; pi < pts.Length; pi++ )
                {
                    pts[pi].X = (int)((pts[pi].X - anchor.X) * zoom) + anchor.X;      // make point relative to anchor then resize it
                    pts[pi].Y = (int)((pts[pi].Y - anchor.Y) * zoom) + anchor.Y;
                    if (!(pts[pi].X == pts[pi - 1].X && pts[pi].Y == pts[pi - 1].Y))
                    {
                        ptsl.Add(pts[pi]);
                    }
                }
                if (bFirst)
                {
                    _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_Resize);    // we have at least one change
                    bFirst = false;
                }
                if (ptsl.Count > 2)
                    SetPoints(pn, ptsl.ToArray());
                else
                    empties.Add(pn);
            }
            if (!bFirst)
            {
                foreach (XmlNode pn in empties)
                {
                    SelectedList.Remove(pn);
                    this.Remove(pn);
                }
                _Undo.EndUndoGroup(true);
                SignalXmlChanged();
                if (empties.Count > 0)
                    SignalSelectionChanged();
                this.Invalidate();
            }
        }

        public List<XmlNode> SelectedList
        {
            get { return _SelectedList; }
        }
        public XmlNode SelectedItem
        {
            get
            {
                return _SelectedList.Count == 0 ? null : _SelectedList[0];
            }
        }
        

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom
        {
            get { return _Zoom; }
            set
            {
                if (_Zoom == value)
                    return;             // nothing to do
                _Zoom = value;
                if (ZoomChange != null)
                    ZoomChange(this);
                this.Refresh();
            }
        }

        internal void SetMapFile(string file)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = false;
                doc.Load(file);
                XmlNode xNode = doc.LastChild;
                if (xNode == null || xNode.Name != "MapData")
                    throw new Exception(Strings.DesignXmlDraw_Error_XMLNotMapData);
                
                _File = file;                           // ok we opened it
                _MapDoc = doc;                          // don't set new one until we know we've loaded the file
                _Undo = new Undo(_MapDoc, 300);
                _Undo.GroupsOnly = true;				// don't record changes that we don't group.
                _Modified = false;
                _SelectedList.Clear();
                SignalSelectionChanged();
                SetBackgroundImage(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("{2} {0}\n\n{1}", file, ex.Message, Strings.DesignXmlDraw_SetMapFile_Unable_to_open_file), Strings.DesignXmlDraw_SetMapFile_ErrorOpeningFile);
            }
            hScrollBar.Value = 0;
            vScrollBar.Value = 0;
            vScrollBar.Maximum = 1000;
            hScrollBar.Maximum = 1000;

            this.Invalidate();
        }

        internal void SetNew()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            doc.InnerXml = "<MapData></MapData>";

            _File = null;                           // ok we opened it
            _MapDoc = doc;                          // don't set new one until we know we've loaded the file
            _Undo = new Undo(_MapDoc, 300);
            _Undo.GroupsOnly = true;				// don't record changes that we don't group.
            _Modified = false;
            _SelectedList.Clear();
            SetBackgroundImage(null);
            hScrollBar.Value = 0;
            vScrollBar.Value = 0;
            vScrollBar.Maximum = 1000;
            hScrollBar.Maximum = 1000;

            this.Invalidate();
        }

		/// <summary>
		/// Need to override otherwise don't get key events for up/down/left/right
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		override protected bool IsInputKey(Keys keyData)
		{
            if (keyData == Keys.Escape)
                return false;
			return true;
		}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int incX = 0;
            int incY = 0;
            int vScroll = vScrollBar.Value;
            int hScroll = hScrollBar.Value;

            // Force scroll up and down
            if (e.KeyCode == Keys.Down)
            {
                incY = 1;
            }
            else if (e.KeyCode == Keys.Up)
            {
                incY = -1;
            }
            else if (e.KeyCode == Keys.Left)
            {
                incX = -1;
            }
            else if (e.KeyCode == Keys.Right)
            {
                incX = 1;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                vScroll = Math.Min(vScrollBar.Value + vScrollBar.LargeChange, vScrollBar.Maximum);
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                vScroll = Math.Max(vScrollBar.Value - vScrollBar.LargeChange, 0);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                return;
            }
            else if (e.KeyCode == Keys.Tab)
            { 
                bool bShiftOn = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                if (bShiftOn)
                    PreviousItem();
                else
                    NextItem();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                this.DeleteSelected();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Tool = ToolMode.Selection;          // force out of insertion mode
                SignalToolChanged();
                SetCursor(null);
                e.Handled = true;
            }

            bool bRefresh = false;
            if (vScroll != vScrollBar.Value)
            {
                vScrollBar.Value = Math.Max(vScroll, 0);
                bRefresh = true;
                e.Handled = true;
            }

            if (hScroll != hScrollBar.Value)
            {
                hScrollBar.Value = Math.Max(hScroll, 0);
                bRefresh = true;
                e.Handled = true;
            }

            if (incX != 0 || incY != 0)
            {
                MoveSelected(incX, incY);

                e.Handled = true;
            }

            if (bRefresh)
                this.Refresh();
        }

        private void MoveSelected(int incX, int incY)
        {
            if (_SelectedList.Count == 0)           // nothing to do
                return;
            if (incX == 0 && incY == 0)
                return;

            _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_Move);
            foreach (XmlNode n in _SelectedList)
            {
                MoveItem(n, incX, incY);
            }
            _Undo.EndUndoGroup(true);
            SignalXmlChanged();
            this.Invalidate();
        }

        private void MoveItem(XmlNode n, int incX, int incY)
        {
            switch (n.Name)
            {
                case "Polygon":
                    Point[] pts = GetPolygon(n, true);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < pts.Length; i++ )
                    {
                        if (i > 0)
                            sb.Append(',');
                        sb.AppendFormat("{0},{1}", pts[i].X+incX, pts[i].Y+incY);
                    }
                    this.SetElement(n, "Points", sb.ToString());
                    break;
                case "Lines":
                    Point[] line = GetLineCoord(n, true);
                    line[0].X += incX;
                    line[0].Y += incY;
                    line[1].X += incX;
                    line[1].Y += incY;
                    string lt = string.Format("{0},{1},{2},{3}", line[0].X, line[0].Y, line[1].X, line[1].Y);
                    this.SetElement(n, "Points", lt);
                    break;
                case "Text":
                    Point p = GetTextPoint(n, true);
                    p.X += incX;
                    p.Y += incY;
                    string t = string.Format("{0},{1}", p.X, p.Y);
                    this.SetElement(n, "Location", t);
                    break;
            }
        }
/// <summary>
/// Handle tabbing forward
/// </summary>
        void NextItem()
        {
            // Select next item in list
            XmlNode next = null;
            if (_SelectedList.Count == 0)
            {
                next = GetRoot().FirstChild;
            }
            else
            {
                next = SelectedItem.NextSibling;
                if (next == null)
                    next = GetRoot().FirstChild;
                _SelectedList.Clear();
            }
            if (next != null)
                _SelectedList.Add(next);
            SignalSelectionChanged();
            Invalidate();
        }
        /// <summary>
        /// Handle tabbing backward
        /// </summary>
        void PreviousItem()
        {
            // Select next item in list
            XmlNode next = null;
            if (_SelectedList.Count == 0)
            {
                next = GetRoot().LastChild;
            }
            else
            {
                next = SelectedItem.PreviousSibling;
                if (next == null)
                    next = GetRoot().LastChild;
                _SelectedList.Clear();
            }
            if (next != null)
                _SelectedList.Add(next);
            SignalSelectionChanged();
            Invalidate();
        }
        #region MouseHandling
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_MapDoc == null)
                return;
            _MouseLocation = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                switch (Tool)
                {
                    case ToolMode.Selection:
                        MouseDownSelection(e);
                        break;
                    case ToolMode.InsertPolygon:
                        MouseDownIPolygon(e);
                        break;
                    case ToolMode.InsertText:
                        MouseDownIText(e);
                        break;
                    case ToolMode.InsertLine:
                        MouseDownILine(e);
                        break;
                    default:
                        break;
                }
                
            }
            else if (e.Button == MouseButtons.Middle)
            {
            }
            else if (e.Button == MouseButtons.Right)
            {
                MouseDownContext(e);
            }
            SetCursor(e);
        }

        private void MouseDownContext(MouseEventArgs e)
        {
            ContextMenuStrip mc = new ContextMenuStrip();
            mc.Opening += mc_Popup;
            if (_menuCopy == null)
                _menuCopy = new ToolStripMenuItem("&Copy", (Image)null, mc_Copy);
            if (_menuPaste == null)
                _menuPaste = new ToolStripMenuItem("&Paste", (Image)null, mc_Paste);

            mc.Items.AddRange(
                new ToolStripMenuItem[] {_menuCopy, _menuPaste});
            if (_SelectedList.Count == 1)
            {
                // add in the select by key options
                mc.Items.Add(new ToolStripMenuItem("-"));
                string[] keys = GetKeysInPolygon(_SelectedList[0]);
                foreach (string k in keys)
                {
                    ToolStripMenuItem mi = new ToolStripMenuItem(string.Format("Select by key = {0}", k), (Image)null, mc_Keys);
                    mi.Tag = k;
                    mc.Items.Add(mi);
                }
            }
            mc.Show(this, e.Location);
        }

        void mc_Popup(object sender, CancelEventArgs e)
        {
            _menuPaste.Enabled = CanPaste();
            _menuCopy.Enabled = _SelectedList.Count > 0;
        }
        void mc_Copy(object sender, EventArgs e)
        {
            Copy();
        }
        
        void mc_Keys(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = sender as ToolStripMenuItem;
            if (mi == null)
                return;
            string key = mi.Tag as string;
            if (key == null)
                return;
            List<string> select = new List<string>(1);
            select.Add(key);

            SelectByKey(select);
        }

        void mc_Paste(object sender, EventArgs e)
        {
            Point np = new Point((int)(_MouseLocation.X / Zoom + hScrollBar.Value), (int)(_MouseLocation.Y / Zoom + vScrollBar.Value));
            Paste(np);
        }

        private void MouseDownILine(MouseEventArgs e)
        {
            _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_InsertLinePoint);
            Point np = new Point((int)(e.Location.X / Zoom + hScrollBar.Value), (int)(e.Location.Y / Zoom + vScrollBar.Value));
            if (_SelectedList.Count == 0)
            {
                string poly = string.Format("{0},{1},{2},{3}", np.X, np.Y, np.X, np.Y);
                XmlNode root = GetRoot();
                XmlNode xp = CreateElement(root, "Lines", null);
                CreateElement(xp, "Points", poly);
                _SelectedList.Add(xp);                  // make it selected
                SignalXmlChanged();
                SignalSelectionChanged();
            }
            else if (_SelectedList.Count == 1 && SelectedItem.Name == "Lines")
            {
                Point[] pts = GetLineCoord(SelectedItem, true);
                pts[1] = new Point(np.X, np.Y);
                SetPoints(SelectedItem, pts);
                SignalXmlChanged();
            }
            EndUndoGroup();
            this.Invalidate();          // should get bounding rectangle of rectangle
        }

        private void MouseDownIText(MouseEventArgs e)
        {
            _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_InsertText);
            _SelectedList.Clear();
            Point np = new Point((int)(e.Location.X / Zoom + hScrollBar.Value), (int)(e.Location.Y / Zoom + vScrollBar.Value));
            string loc = string.Format("{0},{1}", np.X, np.Y);
            XmlNode root = GetRoot();
            XmlNode xp = CreateElement(root, "Text", null);
            // <Value>DE</Value><Location>360,104</Location>
            CreateElement(xp, "Value", "Text");
            CreateElement(xp, "Location", loc);
            _SelectedList.Add(xp);                  // make it selected
            EndUndoGroup();

            SignalXmlChanged();
            SignalSelectionChanged();
            this.Invalidate();          // should get bounding rectangle of rectangle
        }

        private void MouseDownIPolygon(MouseEventArgs e)
        {
            _Undo.StartUndoGroup(Strings.DesignXmlDraw_Undo_InsertPolygonPoint);
            Point np = new Point((int) (e.Location.X / Zoom + hScrollBar.Value), (int)(e.Location.Y / Zoom + vScrollBar.Value));
            if (_SelectedList.Count == 0)
            {
                string poly = string.Format("{0},{1},{2},{3}", np.X, np.Y, np.X, np.Y);
                XmlNode root = GetRoot();
                XmlNode xp = CreateElement(root, "Polygon", null);
                CreateElement(xp, "Points", poly);
                _SelectedList.Add(xp);                  // make it selected
                SignalXmlChanged();
                SignalSelectionChanged();
            }
            else if (_SelectedList.Count == 1 && SelectedItem.Name == "Polygon")
            {
                Point[] pts = GetPolygon(SelectedItem, true);
                Point[] new_pts = new Point[pts.Length + 1];
                for (int i=0; i < pts.Length -1; i++)
                    new_pts[i] = pts[i];
                new_pts[pts.Length - 1] = new Point(np.X, np.Y);
                new_pts[pts.Length] = new_pts[0];
                SetPoints(SelectedItem, new_pts);
            }
            EndUndoGroup();
            this.Invalidate();          // should get bounding rectangle of rectangle
        }
        
        private void MouseDownSelection(MouseEventArgs e)
        {
            bool bCtrlOn = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            this.Capture = true;

            g = this.CreateGraphics();          // need graphic for hit testing of text
            try
            {
                // 1) check if we're hitting an item already selected
                XmlNode h = HitTest(_SelectedList, e.Location);
                if (h != null)
                {
                    XmlNode sib = HitTest(h, e.Location);       // check for items later in the list
                    if (sib != null)
                        h = sib;
                }
                else
                    h = HitTest(_MapDoc.LastChild.ChildNodes, e.Location);
                bool bChange = false;
                if (bCtrlOn)
                {
                    if (_SelectedList.Contains(h))
                    {
                        _SelectedList.Remove(h);
                        h = null;
                        bChange = true;
                    }
                }
                else
                {
                    if (_SelectedList.Count > 0)
                    {
                        bChange = true;
                        _SelectedList.Clear();
                    }
                }
                if (h != null)
                    _SelectedList.Add(h);
                if (bChange || h != null)
                {
                    this.Invalidate();              // should just invalidate the rectangle containing the polygon TODO
                    SignalSelectionChanged();
                }
            }
            finally
            {
                g.Dispose();
                g = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left)
            {
                switch (this.Tool)
                {
                    case ToolMode.Selection:
                        if (this.Capture)           // have we captured the mouse
                        {
                            MoveSelected((int)((e.Location.X  - _MouseLocation.X) /Zoom), (int)((e.Location.Y - _MouseLocation.Y)/Zoom));
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                // when middle mouse is down and mouse moves we scroll the window
                int vScroll = Math.Min(vScrollBar.Value + (e.Location.Y - _MouseLocation.Y), vScrollBar.Maximum);
                vScroll = Math.Max(vScroll, vScrollBar.Minimum);
                int hScroll = Math.Min(hScrollBar.Value + (e.Location.X - _MouseLocation.X), hScrollBar.Maximum);
                hScroll = Math.Max(hScroll, hScrollBar.Minimum);
                vScrollBar.Value = vScroll;
                hScrollBar.Value = hScroll;
                this.Invalidate();
            }

            _MouseLocation = e.Location;
            SetCursor(e);
        }

        private void SetCursor(MouseEventArgs e)
        {
            Cursor c = Cursors.Default;
            if (e == null)
            { }
            else if (e.Button == MouseButtons.Middle)
                c = Cursors.SizeAll;
            else
            {
                switch (this.Tool)
                {
                    case ToolMode.Selection:
                        c = Cursors.Default;
                        break;
                    default:
                        c = Cursors.Cross;
                        break;
                }
            }
            if (c != this.Cursor)
                this.Cursor = c;
        }
        private XmlNode HitTest(IEnumerable ienum, Point point)
        {
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in ienum)
            {
                if (IsHit(xNodeLoop, point))
                    return xNodeLoop;
            }
            return null;
        }
        /// <summary>
        /// HitTest for testing sibling XML nodes
        /// </summary>
        /// <param name="xNode"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private XmlNode HitTest(XmlNode xNode, Point point)
        {
            // Loop thru all the child nodes
            for (XmlNode xNodeLoop = xNode.NextSibling; xNodeLoop != null; xNodeLoop = xNodeLoop.NextSibling)
            {
                if (IsHit(xNodeLoop, point))
                    return xNodeLoop;
            }
            return null;
        }

        private bool IsHit(XmlNode xNode, Point point)
        {
            bool hit = false;
            if (xNode.NodeType != XmlNodeType.Element)
                return hit;
            switch (xNode.Name)
            {
                case "Polygon":
                    Point[] pts = GetPolygon(xNode, false);
                    ApplyZoom(pts);
                    if (PointInPolygon(pts, point))
                        hit = true;
                    break;
                case "Lines":
                    Point[] line = GetLineCoord(xNode, false);
                    ApplyZoom(line);
                    if (PointInLine(line, point))
                        hit = true;
                    break;
                case "Text":
                    Rectangle r = GetTextRect(xNode);
                    Rectangle r2 = new Rectangle((int)(r.Left * Zoom), (int)(r.Top * Zoom), (int)(r.Width * Zoom), (int)(r.Height * Zoom));
                    if (r2.Contains(point))
                        hit = true;
                    break;
            }
            return hit;
        }
        #endregion
        private void ApplyZoom(Point[] pts)
        {
            if (Zoom == 1)
                return;
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i].X = (int)(pts[i].X * Zoom);
                pts[i].Y = (int)(pts[i].Y * Zoom);
            }
            return;
        }
        
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_MapDoc == null)
                return;

            this.Capture = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int wvalue;
            bool bCtrlOn = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            if (bCtrlOn)
            {   // when ctrl key on and wheel rotated we zoom in or out
                float zoom = Zoom;

                if (e.Delta < 0)
                {
                    zoom -= .1f;
                    if (zoom < .1f)
                        zoom = .1f;
                }
                else
                {
                    zoom += .1f;
                    if (zoom > 10)
                        zoom = 10;
                }
                Zoom = zoom;
                return;
            }

            if (e.Delta < 0)
            {
                if (vScrollBar.Value < vScrollBar.Maximum)
                {
                    wvalue = vScrollBar.Value + vScrollBar.SmallChange;

                    vScrollBar.Value = Math.Min(vScrollBar.Maximum, wvalue);
                    this.Refresh();
                }
            }
            else
            {
                if (vScrollBar.Value > vScrollBar.Minimum)
                {
                    wvalue = vScrollBar.Value - vScrollBar.SmallChange;

                    vScrollBar.Value = Math.Max(vScrollBar.Minimum, wvalue);
                    this.Refresh();
                }
            }

        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_MapDoc == null)
            {
                e.Graphics.DrawString("Please open a map file!", new Font("Arial", 12), Brushes.Black, 1, 1);
                return;
            }
            g = null;
            try
            {
                g = e.Graphics;
                this.Draw(e.ClipRectangle);
            }
            finally
            {
                g = null;
            }
        }

        /// <summary>
		/// Draw the report definition
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hScroll">Horizontal scroll position</param>
		/// <param name="vScroll">Vertical scroll position</param>
		/// <param name="clipRectangle"></param>
		internal void Draw(System.Drawing.Rectangle cr)
		{
			g.PageUnit = GraphicsUnit.Pixel;
            g.FillRectangle(Brushes.White, cr);
            g.ScaleTransform(Zoom, Zoom);


            if (this._BackImage != null)
                g.DrawImage(this._BackImage, -hScrollBar.Value, -vScrollBar.Value);

            //_clip = new Rectangle(cr.X + hScrollBar.Value, 
            //    cr.Y + vScrollBar.Value, 
            //    cr.Width, 
            //    cr.Height);

            XmlNode xNode = GetRoot();
            if (xNode == null)
			{
                g.DrawString("MapData node not found", new Font("Arial", 12), Brushes.Black, new PointF(10, 10));
                return;
			}

			ProcessMap(xNode);

            ProcessSelected();
		}

        private XmlNode GetRoot()
        {
            if (_MapDoc == null)
                return null;
            XmlNode xNode = _MapDoc.LastChild;
            if (xNode == null || xNode.Name != "MapData")
                return null;
            return xNode;
        }

		// Process the map data
		private void ProcessMap(XmlNode xNode)
		{

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
                bool bSelected = _SelectedList.Contains(xNodeLoop);
				switch (xNodeLoop.Name)
				{
					case "Polygon":
						DrawPolygon(xNodeLoop, bSelected);
						break;
					case "Lines":
                        DrawLines(xNodeLoop, bSelected);
                        break;
					case "Text":
                        DrawText(xNodeLoop, bSelected);
                        break;
				}
			}
		}

        // Process the selected objects by drawing the points
        private void ProcessSelected()
        {
            bool bReset = false;
            try
            {
                // Loop thru all the child nodes
                foreach (XmlNode xNodeLoop in _SelectedList)
                {
                    Point[] pts = null;
                    switch (xNodeLoop.Name)
                    {
                        case "Polygon":
                            pts = GetPolygon(xNodeLoop, false);
                            if (pts == null)
                                bReset = true;
                            break;
                        case "Lines":
                            pts = GetLineCoord(xNodeLoop, false);
                            if (pts == null)
                                bReset = true;
                            break;
                        case "Text":
                            break;
                    }
                    if (pts == null)
                        continue;
                    foreach (Point p in pts)
                    {
                        g.DrawEllipse(Pens.Red, p.X - 2, p.Y - 2, 4, 4);
                    }
                }
            }
            catch
            {       // selection list isn't valid;  usually as a result of an undo
                bReset = true;
            }
            if (bReset)
            {
                // we ran into a problem; this can be caused by an "undo" affecting something that is selected
                // set to a neutral condition with nothing selected
                _SelectedList.Clear();
                SignalSelectionChanged();
                this.Invalidate();          // and we should repaint
            }
        }

		private void DrawLines(XmlNode xNode, bool bSelected)
		{
            try
            {
                Point[] coord = GetLineCoord(xNode, false);
                Pen p = bSelected ? Pens.Red : Pens.Black;
                g.DrawLine(p, coord[0].X, coord[0].Y, coord[1].X, coord[1].Y);
            }
            catch (Exception e)
            {
                g.DrawString(e.Message, new Font("Arial", 12), Brushes.Black, new PointF(0, 0));
            }
            finally
            {
            }

			return;
		}

        internal Point[] GetLineCoord(XmlNode xNode, bool bAsIs)
        {
            XmlNode v = GetNamedChildNode(xNode, "Points");
            string t = v == null ? "" : v.InnerText;

            string[] coord = t.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Point[] pts = new Point[2];
            pts[0].X = int.Parse(coord[0], NumberStyles.Integer) - (bAsIs? 0:hScrollBar.Value); // x1
            pts[0].Y = int.Parse(coord[1], NumberStyles.Integer) - (bAsIs? 0:vScrollBar.Value); // y1
            pts[1].X = int.Parse(coord[2], NumberStyles.Integer) - (bAsIs? 0:hScrollBar.Value); // x2
            pts[1].Y = int.Parse(coord[3], NumberStyles.Integer) - (bAsIs? 0:vScrollBar.Value); // y2

            return pts;
        }

        private void DrawPolygon(XmlNode xNode, bool bSelected)
        {
            try
            {
                Point[] pts = GetPolygon(xNode, false);
                Color fill = GetColor(GetNamedChildNode(xNode, "FillColor"));
                Pen p = bSelected ? Pens.Red : Pens.Black;
                g.DrawPolygon(p, pts);
                if (!fill.IsEmpty)
                {
                    Brush b = new SolidBrush(fill);
                    g.FillPolygon(b, pts);
                    b.Dispose();
                }
            }
            catch (Exception e)
            {
                g.DrawString(e.Message, new Font("Arial", 12), bSelected?Brushes.Red: Brushes.Black, new PointF(0, 0));
            }
            finally
            {
            }

            return;
        }

        private Color GetColor(XmlNode cNode)
        {
            if (cNode == null)
                return Color.Empty;
            string sc = cNode.InnerText;
            Color c = Color.Empty;
			try 
			{
				c = Majorsilence.Forms.Drawing.ColorTranslator.FromHtml(sc);
			}
			catch 
			{       // if bad color just ignore and handle as empty color
			}
			return c;
        }

        internal string[] GetKeysInPolygon(XmlNode pNode)
        {
            string k = this.GetElementValue(pNode, "Keys", "");
            string[] skeys = k.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return skeys;
        }

        internal Point[] GetPolygon(XmlNode pNode, bool bAsIs)
        {
            XmlNode v = GetNamedChildNode(pNode, "Points");
            if (v == null)
                return null;
            string t = v == null ? "" : v.InnerText;

            string[] coord = t.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            Point[] pts = new Point[coord.Length / 2];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i].X = int.Parse(coord[i * 2], NumberStyles.Integer) - (bAsIs? 0: hScrollBar.Value);
                pts[i].Y = int.Parse(coord[i * 2 + 1], NumberStyles.Integer) - (bAsIs ? 0 : vScrollBar.Value);
            }
            return pts;
        }
        
        internal void SetPoints(XmlNode pNode, Point[] pts)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pts.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.AppendFormat("{0},{1}", pts[i].X, pts[i].Y);
            }
            if (pNode.Name == "Polygon")
            {   // Make sure polygon is closed
                if (!(pts[0].X == pts[pts.Length-1].X &&
                    pts[0].Y == pts[pts.Length-1].Y))
                    sb.AppendFormat(",{0},{1}", pts[0].X, pts[0].Y);
            }
            SetElement(pNode, "Points", sb.ToString());
        }
 
        private void DrawText(XmlNode xNode, bool bSelected)
		{
            Font font = null;
            Brush br = null;
            try
            {
                XmlNode v = GetNamedChildNode(xNode, "Value");
                string t = v == null ? "" : v.InnerText;
                Color c = GetTextColor(xNode);
                br = new SolidBrush(c);

                Point p = GetTextPoint(xNode, false);

                font = GetTextFont(xNode);

                g.DrawString(t, font, br, p);
                if (bSelected)
                {
                    SizeF sz = g.MeasureString(t, font);
                    Rectangle r = new Rectangle(p.X, p.Y, (int)Math.Round(sz.Width, 0), (int)Math.Round(sz.Height, 0));
                    g.DrawRectangle(Pens.Red, r);
                }
            }
            catch (Exception e)
            {
                g.DrawString(e.Message, new Font("Arial", 12), Brushes.Black, new PointF(0, 0));
            }
            finally
            {
                if (font != null)
                    font.Dispose();
                if (br != null)
                    br.Dispose();
            }
            return;
		}

        internal Font GetTextFont(XmlNode xNode)
        {
            string face = GetTextFontFamily(xNode);
            float pts = GetTextFontSize(xNode);

            Majorsilence.Forms.Drawing.FontStyle fs = 0;
            if (IsTextFontWeightItalic(xNode))
                fs |= Majorsilence.Forms.Drawing.FontStyle.Italic;
            if (IsTextFontWeightBold(xNode))
                fs |= Majorsilence.Forms.Drawing.FontStyle.Bold;

            switch (GetTextDecoration(xNode))
            {
                case "Underline":
                    fs |= Majorsilence.Forms.Drawing.FontStyle.Underline;
                    break;
                case "LineThrough":
                    fs |= Majorsilence.Forms.Drawing.FontStyle.Strikeout;
                    break;
                default:
                    break;
            }

            Font font;
            try
            {
                font = new Font(face, pts, fs);	// si.FontSize already in points
            }
            catch     // fonts that don't exist can throw exception; but we don't want it to
            {
                font = new Font("Arial", pts, fs);  // if this throws exception; we'll let it
            }

            return font;
        }

        internal string GetTextDecoration(XmlNode xNode)
        {
            XmlNode cn = GetNamedChildNode(xNode, "TextDecoration");
            if (cn == null)
                return "None";
            return cn.InnerText;
        }
        internal string GetTextFontFamily(XmlNode xNode)
        {
            XmlNode cn = GetNamedChildNode(xNode, "FontFamily");
            if (cn == null)
                return "Arial";
            return cn.InnerText;
        }

        internal float GetTextFontSize(XmlNode xNode)
        {
            XmlNode cn = GetNamedChildNode(xNode, "FontSize");
            if (cn == null)
                return 8;

            try
            {
                float x = float.Parse(cn.InnerText, NumberFormatInfo.InvariantInfo);
                return x;
            }
            catch
            {
                return 8;
            }
        }
        internal bool IsTextFontWeightBold(XmlNode xNode)
        {
            XmlNode cn = GetNamedChildNode(xNode, "FontWeight");
            if (cn == null)
                return false;

            return cn.InnerText.ToLower() == "bold";
        }
        internal bool IsTextFontWeightItalic(XmlNode xNode)
        {
            XmlNode cn = GetNamedChildNode(xNode, "FontStyle");
            if (cn == null)
                return false;

            return cn.InnerText.ToLower() == "italic";
        }

        internal Color GetTextColor(XmlNode tNode)
        {
            XmlNode cn = GetNamedChildNode(tNode, "Color");

            Color c = GetColor(cn);
            return c.IsEmpty ? Color.Black : c;
        }

        internal Point GetTextPoint(XmlNode xNode, bool bAsIs)
        {
            try
            {
                XmlNode l = GetNamedChildNode(xNode, "Location");
                string[] coord = l.InnerText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                int x = int.Parse(coord[0], NumberStyles.Integer) - (bAsIs? 0:hScrollBar.Value);
                int y = int.Parse(coord[1], NumberStyles.Integer) - (bAsIs ? 0 : vScrollBar.Value);
                return new Point(x, y);
            }
            catch
            {
                return new Point(0, 0);     // failure
            }
        }
        private Rectangle GetTextRect(XmlNode xNode)
        {
            XmlNode v = GetNamedChildNode(xNode, "Value");
            string t = v == null ? "" : v.InnerText;

            Point p = GetTextPoint(xNode, false);

            if (g == null)
                return new Rectangle(p.X, p.Y, t.Length * 8, 20);

            Font font = null;
            try
            {
                string face = GetTextFontFamily(xNode);
                float pts = GetTextFontSize(xNode);
                font = new Font(face, pts);

                SizeF sz = g.MeasureString(t, font);
                return new Rectangle(p.X, p.Y, (int)Math.Round(sz.Width,0), (int)Math.Round(sz.Height,0));
            }
            finally
            {
                if (font != null)
                    font.Dispose();
            }
        }

        private void InitializeComponent()
        {
            this.DoubleBuffered = true;
            this.vScrollBar = new Majorsilence.Forms.VScrollBar();
            this.hScrollBar = new Majorsilence.Forms.HScrollBar();
            this.SuspendLayout();
            // 
            // vScrollBar
            // 
            this.vScrollBar.Dock = Majorsilence.Forms.DockStyle.Right;
            this.vScrollBar.Location = new System.Drawing.Point(133, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(17, 150);
            this.vScrollBar.SmallChange = 10;
            this.vScrollBar.TabIndex = 0;
            this.vScrollBar.Scroll += this.vScrollBar_Scroll;
            // 
            // hScrollBar
            // 
            this.hScrollBar.Dock = Majorsilence.Forms.DockStyle.Bottom;
            this.hScrollBar.Location = new System.Drawing.Point(0, 133);
            this.hScrollBar.Name = "hScrollBar";
            this.hScrollBar.Size = new System.Drawing.Size(133, 17);
            this.hScrollBar.SmallChange = 10;
            this.hScrollBar.TabIndex = 1;
            this.hScrollBar.Scroll += this.hScrollBar_Scroll;
            // 
            // DesignXmlDraw
            // 
            this.Controls.Add(this.hScrollBar);
            this.Controls.Add(this.vScrollBar);
            this.Name = "DesignXmlDraw";
            this.ResumeLayout(false);

        }

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (vScrollBar.IsDisposed)
                return;

            if (e.NewValue == e.OldValue)
                return;

            this.Refresh();
        }

        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (hScrollBar.IsDisposed)
                return;

            if (e.NewValue == e.OldValue)
                return;

            this.Refresh();
        }
        
        bool PointInLine(Point[] line, Point pt)
        {
            Pen p = new Pen(Color.Black, 4);
            GraphicsPath gp = new GraphicsPath();
            gp.AddLine(line[0], line[1]);
            gp.Widen(p);
            p.Dispose();

            return gp.IsVisible(pt);
        }

        /// <summary>
        /// PointInPolygon: uses ray casting algorithm ( http://en.wikipedia.org/wiki/Point_in_polygon )
        ///   could have used approach similar to PointInLine
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        bool PointInPolygon(Point[] poly, Point p)
        {
            Point p1, p2;
            bool bIn = false;
            if (poly.Length < 3)
            {
                return false;
            }

            Point op = new Point(poly[poly.Length - 1].X, poly[poly.Length - 1].Y);
            for (int i = 0; i < poly.Length; i++)
            {
                Point np = new Point(poly[i].X, poly[i].Y);
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


        internal void SetBackgroundImage(string fname)
        {
            if (fname == null)
            {
                if (_BackImage != null)
                {
                    _BackImage.Dispose();
                    _BackImage = null;
                }
                return;
            }

            Stream strm = null;
            Majorsilence.Forms.Drawing.Image im = null;

            try
            {
                if (fname.StartsWith("http:") ||
                    fname.StartsWith("file:") ||
                    fname.StartsWith("https:"))
                {
                    WebRequest wreq = WebRequest.Create(fname);
                    WebResponse wres = wreq.GetResponse();
                    strm = wres.GetResponseStream();
                }
                else
                    strm = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read);
                im = Majorsilence.Forms.Drawing.Image.FromStream(strm);
                if (_BackImage != null)
                    _BackImage.Dispose();
                _BackImage = im;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
            }
            this.Invalidate();
        }

        #region XML Manipulation
        internal void DeleteSelected()
        {
            if (_SelectedList.Count == 0)       // Nothing to do
                return;

            StartUndoGroup(Strings.DesignXmlDraw_Undo_Delete);
            foreach (XmlNode d in _SelectedList)
                Remove(d);
            EndUndoGroup();

            _SelectedList.Clear();
            SignalSelectionChanged();
            this.Refresh();
        }

        internal XmlElement CreateElement(XmlNode parent, string name, string val)
        {
            XmlElement node;
            
            node = _MapDoc.CreateElement(name);
            if (val != null)
                node.InnerText = val;
            parent.AppendChild(node);
            return node;
        }

        internal XmlNode GetNamedChildNode(XmlNode xNode, string name)
        {
            if (xNode == null)
                return null;

            foreach (XmlNode cNode in xNode.ChildNodes)
            {
                if (cNode.NodeType == XmlNodeType.Element &&
                    cNode.Name == name)
                    return cNode;
            }
            return null;
        }

        internal string GetElementValue(XmlNode parent, string name, string defaultV)
        {
            XmlNode node = this.GetNamedChildNode(parent, name);
            if (node == null)
                return defaultV;
            else
                return node.InnerText;
        }

        internal void Remove(XmlNode node)
        {
            if (node != null)
            {
                node.ParentNode.RemoveChild(node);
            }
            return;
        }

        internal XmlNode SetElement(XmlNode parent, string name, string val)
        {
            XmlNode node = this.GetNamedChildNode(parent, name);
            if (node == null)
                node = CreateElement(parent, name, val);
            else if (val != null)
                node.InnerText = val;
            return node;
        }

        #endregion

        internal void SignalXmlChanged()
        {
            if (XmlChange != null)
                XmlChange(this);
            Modified = true;
        }

        internal void SignalSelectionChanged()
        {
            if (SelectionChange != null)
                SelectionChange(this);
        }

        internal void SignalToolChanged()
        {
            if (ToolChange != null)
                ToolChange(this);
        }

        internal SortedList<string, string> GetAllKeys()
        {
            SortedList<string, string> keys = new SortedList<string, string>(_MapDoc.ChildNodes.Count);
            XmlNode root = GetRoot();
            if (root == null)
                return keys;

            foreach (XmlNode xNodeLoop in root.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                if (xNodeLoop.Name != "Polygon")
                    continue;
                // obtain the keys for the polygon
                string[] skeys = this.GetKeysInPolygon(xNodeLoop);
                foreach (string key in skeys)
                {
                    if (!keys.ContainsKey(key))
                        keys.Add(key, key);
                }
            }
            return keys;
        }
    }

}
