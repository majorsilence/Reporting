
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// DesignCtl is a designer view of an RDL report
	/// </summary>
	public partial class DesignCtl
	{ 
		public delegate void OpenSubreportEventHandler(object source, SubReportEventArgs e);
        public delegate void HeightEventHandler(object source, HeightEventArgs e);
        public event System.EventHandler ReportChanged;
        public event System.EventHandler SelectionChanged;
		public event System.EventHandler SelectionMoved;
		public event System.EventHandler ReportItemInserted;
        public event System.EventHandler VerticalScrollChanged;
        public event System.EventHandler HorizontalScrollChanged;
        public event OpenSubreportEventHandler OpenSubreport;
        public event HeightEventHandler HeightChanged;
        bool _InPaint;						// to prevent recursively invoking paint
		// Scrollbars
		public VScrollBar _vScroll;
		public HScrollBar _hScroll;
		private float _DpiX;
		private float _DpiY;
		private XmlDocument _ReportDoc;		// the xml document we're editting
		private Undo _Undo;					//  the undo object tied to the _ReportDoc;
		private string _CurrentInsert;		// current object to insert; null if none

		// Mouse control
		private XmlNode _MouseDownNode;		// XmlNode affected by the mouse down event
		private HitLocationEnum _MouseDownLoc;	// hit location affected by the mouse down event
		private Point _MousePosition= new Point();		// position of the mouse
		private Point _ptRBOriginal = new Point();	// starting position of the mouse (rubber banding)
		private Point _ptRBLast = new Point();		//   last position of mouse (rubber banding)
		private bool _bHaveMouse;			// flag indicates we're rubber banding
		private bool _AdjustScroll = false;	// when adjusting band height we may need to adjust scroll bars

		public DesignXmlDraw _DrawPanel;		// the main drawing panel
        public RdlDesigner RdlDesigner;

        public float SCALEX = 1;
        public float SCALEY = 1;

        public DesignCtl()
		{
			InitializeComponent();
			// Get our graphics DPI					   
			Graphics g = null;			
			try
			{
				g = this.CreateGraphics(); 
				_DpiX = g.DpiX;
				_DpiY = g.DpiY;
			}
			catch
			{
				_DpiX = _DpiY = 96;
			}
			finally
			{
				if (g != null)
					g.Dispose();
			}

			// Handle the controls
			_vScroll = new VScrollBar();
			_vScroll.Scroll += this.VerticalScroll;
			_vScroll.Enabled = false;

			_hScroll = new HScrollBar();
			_hScroll.Scroll += this.HorizontalScroll;
			_hScroll.Enabled = false;

			_DrawPanel = new DesignXmlDraw();

			_DrawPanel.Paint += this.DrawPanelPaint;
			_DrawPanel.MouseUp += this.DrawPanelMouseUp;
			_DrawPanel.MouseDown += this.DrawPanelMouseDown;
			_DrawPanel.Resize += this.DrawPanelResize; 
			_DrawPanel.MouseWheel += DrawPanelMouseWheel;
			_DrawPanel.KeyDown += DrawPanelKeyDown;
			_DrawPanel.MouseMove += DrawPanelMouseMove;
			_DrawPanel.DoubleClick += DrawPanelDoubleClick;

			this.Layout += DesignCtl_Layout;
			this.SuspendLayout();		 

			// Must be added in this order for DockStyle to work correctly
			this.Controls.Add(_DrawPanel);
			this.Controls.Add(_vScroll);
			this.Controls.Add(_hScroll);

			this.ResumeLayout(false);

			BuildContextMenus();
		}

        public void SignalReportChanged()
        {
            ReportChanged(this, new EventArgs());
        }

        public void SignalSelectionMoved()
        {
            SelectionMoved(this, new EventArgs());
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string CurrentInsert
		{
			get {return _CurrentInsert; }
			set 
			{
				_CurrentInsert = value;
			}
		}

        public int VerticalScrollPosition
        {
            get { return _vScroll.Value; }
        }

        public int HorizontalScrollPosition
        {
            get { return _hScroll.Value; }
        }

        internal Color SepColor
        {
            get { return _DrawPanel.SepColor; }
        }

        internal float SepHeight
        {
            get { return _DrawPanel.SepHeight; }
        }

        internal float PageHeaderHeight
        {
            get {return _DrawPanel.PageHeaderHeight; }
        }

        internal float PageFooterHeight
        {
            get { return _DrawPanel.PageFooterHeight; }
        }

        internal float BodyHeight
        {
            get { return _DrawPanel.BodyHeight; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public XmlDocument ReportDocument
		{
			get {return _ReportDoc;}
			set 
			{
				_ReportDoc = value;
				if (_ReportDoc != null)
				{
					_Undo = new Undo(_ReportDoc, 300);
					_Undo.GroupsOnly = true;				// don't record changes that we don't group.
				}
				int selCount = _DrawPanel.SelectedCount;
				this._DrawPanel.ReportDocument = _ReportDoc;
				if (selCount > 0)	// changing report document forces change to selection
					SelectionChanged(this, new EventArgs());
			}
		}
 
		internal DesignXmlDraw DrawCtl
		{
			get {return _DrawPanel;}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string ReportSource
		{
			get 
			{
				if (_ReportDoc == null)
					return null;
				string result="";
				try
				{
					// Convert the document into a string
					StringWriter sw = new StringWriter();
                    var settings = new XmlWriterSettings
                    {
                        // RdlDesigner is only wired up when this DesignCtl is hosted inside the
                        // full MDI RdlDesigner shell (see RdlDesigner.CreateMDIChildAsync); guard
                        // against it being null (e.g. a standalone/test-hosted DesignCtl) instead
                        // of throwing here -- this was a silent NullReferenceException swallowed
                        // by the catch below, surfacing only as an empty ReportSource with no
                        // indication why (found via D6's designer round-trip test).
                        NewLineChars = RdlDesigner?.XmlNewLine == NewLineChar.Unix ? "\n" : "\r\n",
                        Indent = true,
                        IndentChars = "  "
                    };
                    var xw = XmlWriter.Create(sw, settings);
			
                    _ReportDoc.WriteContentTo(xw);
                    xw.Close();
					sw.Close();	
					result = sw.ToString();
					result = result.Replace("xmlns=\"\"", "");
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, Strings.DesignCtl_Show_UnableCreateRDL);
				}
				return result;
			}
			set 
			{
				if (value == null || value == "")
				{
					ReportDocument = null;
					return;
				}

				XmlDocument xDoc = new XmlDocument();
				
				xDoc.PreserveWhitespace = false;
				xDoc.LoadXml(value);	// this will throw an exception if invalid XML
				ReportDocument = xDoc;
			}
		}

        public void ClearUndo()
		{
			_Undo.Reset();
		}

        internal Undo UndoObject
        {
            get { return _Undo; }
        }
		public void Undo()
		{
			_Undo.undo();
			_DrawPanel.ReportNames = null;	// may not be required; but if reportitem deleted/inserted it must be
            // determine if any of the selected nodes has been affected
            bool clearSelect = false;
            foreach (XmlNode n in _DrawPanel.SelectedList)
            {
                // this is an imperfect test but it shows if the node has been unchained.
                if (n.ParentNode == null)
                {
                    clearSelect = true;
                    break;
                }
            }
            if (clearSelect)
                _DrawPanel.SelectedList.Clear();

			_DrawPanel.Invalidate();   
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowReportItemOutline
        {
            get { return _DrawPanel.ShowReportItemOutline; }
            set 
            {
                _DrawPanel.ShowReportItemOutline = value; 
            }
        }

		public void Redo()
		{
		}

		public string UndoDescription
		{
			get { return _Undo.Description; }
		}

		public bool CanUndo
		{
			get { return _Undo.CanUndo; }
		}

		public void StartUndoGroup(string description)
		{
			_Undo.StartUndoGroup(description);
		}

		public void EndUndoGroup(bool keepChanges)
		{
			_Undo.EndUndoGroup(keepChanges);
		}

        public void Align(TextAlignEnum ta)
        {
            if (_DrawPanel.SelectedCount < 1)
                return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);

            _Undo.EndUndoGroup();

            return;
        }

		public void AlignLefts()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
            
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			XmlNode l = _DrawPanel.GetNamedChildNode(model, "Left");
			string left = l == null? "0pt": l.InnerText;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	// we even reset the first one; in case the attribute wasn't specified
				_DrawPanel.SetElement(xNode, "Left", left);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void AlignRights()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
            
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF mrect = _DrawPanel.GetReportItemRect(model);	// size attributes in points
			if (mrect.Width == float.MinValue)
				return;			// model doesn't have width specified

			float mright = mrect.Left + mrect.Width;	// the right side of the model

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	
				if (xNode == model)
					continue;
				RectangleF nrect = _DrawPanel.GetReportItemRect(xNode);
				if (nrect.Width == float.MinValue)
					continue;

				float nleft = mright - nrect.Width;
				if (nleft < 0)
					nleft = 0;

				string left = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", nleft);
				_DrawPanel.SetElement(xNode, "Left", left);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void AlignCenters()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF mrect = _DrawPanel.GetReportItemRect(model);	// size attributes in points
			if (mrect.Width == float.MinValue)
				return;			// model doesn't have width specified

			float mc = mrect.Left + mrect.Width/2;	// the middle of the model

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	
				if (xNode == model)
					continue;
				RectangleF nrect = _DrawPanel.GetReportItemRect(xNode);
				if (nrect.Width == float.MinValue)
					continue;

				float nleft =  (mc - (nrect.Left + nrect.Width/2));
				nleft += nrect.Left;
				if (nleft < 0)
					nleft = 0;

				string left = string.Format(NumberFormatInfo.InvariantInfo,"{0:0.00}pt", nleft);
				_DrawPanel.SetElement(xNode, "Left", left);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}
		


		/// <summary>
		/// Align to center in a container a selected item.
		/// Items Tested : Simple TextBox , Rectangle , TextBox inside Rectangle , Line , Table
		/// For a selected Item the container can be a Section (PageHeader,Body,PageFooter) or another item (typ. rectangle)
		/// For Table alignment select only 1 cell 
		/// 
		/// </summary>
		public void HorizontalCenterInsideContainer()
		{
			// Get the selected node
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF mrect;
			float W;
			//
			// If item selected is inside Table
			//
			if (_DrawPanel.InTable(model) == true)
			{
				// Get parent table of the selected node
				XmlNode NodoTable = _DrawPanel.TMParent(model);
				// Get size of table
                mrect = _DrawPanel.GetRectangle(NodoTable);
                //
                // Get the size in point of the container and its center
                //
                W= _DrawPanel.WidthOfContainer(NodoTable) / 2;
                //
                // Calculate new Left position for table
                //
                float nleft = W - mrect.Width / 2;
                //
                // Set new left position of table
                //
                string left = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", nleft);
                _DrawPanel.SetElement(NodoTable, "Left", left);
            }
			else
			{
				// Get rectangle that contains the model
				mrect = _DrawPanel.GetReportItemRect(model);
				if (mrect.Width == float.MinValue) return;
                //
                // Get the size in point of the container and its center
                //
                W = _DrawPanel.WidthOfContainer(model) / 2;
                //
                // Calculate new Left position for model
                //
                float nleft = W - mrect.Width / 2;
                //
                // Set new left position of model
                //
                string left = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", nleft);
                _DrawPanel.SetElement(model, "Left", left);
            }
            ReportChanged(this, new EventArgs());
            _DrawPanel.Invalidate();
        }

		/// <summary>
		/// Vertical center alignment.
		/// Alignment can be done only on item inside a container (ie rectangle) or only PageHeader or Footer
		/// </summary>
		public void VerticalCenterInsideContainer()
		{
            // Get the selected node
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
            RectangleF mrect;
			float SectionHeight;
            float NewTop;
            //
            // If item selected is inside Table do nothing
            //
            if (_DrawPanel.InTable(model) == true)
			{
				return;
			}
			//
			// Get the Height of the model container
			//
			SectionHeight=_DrawPanel.HeightOfContainer(model);
			//
			// do nothing if invalid section Height
			//
			if(SectionHeight == 0) return;
			// Get rectangle that contains the model
			mrect = _DrawPanel.GetReportItemRect(model);
			// Calculate new Top for model
			NewTop=(SectionHeight- mrect.Height) / 2;
			string top = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", NewTop);
			_DrawPanel.SetElement(model, "Top", top);
			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();
		}



		public void AlignTops()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
            
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			XmlNode t = _DrawPanel.GetNamedChildNode(model, "Top");
			string top = t == null? "0pt": t.InnerText;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	// we even reset the first one; in case the attribute wasn't specified
				_DrawPanel.SetElement(xNode, "Top", top);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void AlignBottoms()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
            
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF mrect = _DrawPanel.GetReportItemRect(model);	// size attributes in points
			if (mrect.Height == float.MinValue)
				return;			// model doesn't have height specified

			float mbottom = mrect.Top + mrect.Height;	// the bottom side of the model

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	
				if (xNode == model)
					continue;
				RectangleF nrect = _DrawPanel.GetReportItemRect(xNode);
				if (nrect.Height == float.MinValue)
					continue;

				float ntop = mbottom - nrect.Height;
				if (ntop < 0)
					ntop = 0;

				string top = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", ntop);
				_DrawPanel.SetElement(xNode, "Top", top);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void AlignMiddles()
		{
            if (_DrawPanel.SelectedCount < 2)
                return;
            
            XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF mrect = _DrawPanel.GetReportItemRect(model);	// size attributes in points
			if (mrect.Height == float.MinValue)
				return;			// model doesn't have height specified

			float mc = mrect.Top + mrect.Height/2;	// the middle of the model

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Align);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	
				if (xNode == model)
					continue;
				RectangleF nrect = _DrawPanel.GetReportItemRect(xNode);
				if (nrect.Height == float.MinValue)
					continue;

				float ntop =  (mc - (nrect.Top + nrect.Height/2));
				ntop += nrect.Top;
				if (ntop < 0)
					ntop = 0;

				string top = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", ntop);
				_DrawPanel.SetElement(xNode, "Top", top);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void SizeHeights()
		{
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			XmlNode h = _DrawPanel.GetNamedChildNode(model, "Height");
			if (h == null)
				return;
			string height = h.InnerText;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Size);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	// we even reset the first one; in case the attribute wasn't specified
				_DrawPanel.SetElement(xNode, "Height", height);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void SizeWidths()
		{
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			XmlNode w = _DrawPanel.GetNamedChildNode(model, "Width");
			if (w == null)
				return;
			string width = w.InnerText;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Size);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	// we even reset the first one; in case the attribute wasn't specified
				_DrawPanel.SetElement(xNode, "Width", width);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void SizeBoth()
		{
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			XmlNode w = _DrawPanel.GetNamedChildNode(model, "Width");
			if (w == null)
				return;
			string width = w.InnerText;

			XmlNode h = _DrawPanel.GetNamedChildNode(model, "Height");
			if (h == null)
				return;
			string height = h.InnerText;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Size);
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{	// we even reset the first one; in case the attribute wasn't specified
				_DrawPanel.SetElement(xNode, "Height", height);
				_DrawPanel.SetElement(xNode, "Width", width);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		void HorzSpacing(float diff)
		{
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF rectm = _DrawPanel.GetReportItemRect(model);
			if (rectm.Width == float.MinValue)
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Spacing);
			float x = rectm.Left + rectm.Width + diff;
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{
				if (xNode == model)
					continue;
				string left = string.Format(NumberFormatInfo.InvariantInfo,"{0:0.00}pt", x);
				_DrawPanel.SetElement(xNode, "Left", left);
				RectangleF rectn = _DrawPanel.GetReportItemRect(xNode);
				if (rectn.Width == float.MinValue)
					rectn.Width = 77;
				x += (rectn.Width + diff);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		float HorzSpacingDiff()
		{
			float diff = 0;
			if (_DrawPanel.SelectedList.Count < 2)
				return diff;

			XmlNode m1 = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF r1 = _DrawPanel.GetReportItemRect(m1);
			if (r1.Width == float.MinValue)
				return diff;

			XmlNode m2 = _DrawPanel.SelectedList[1] as XmlNode;
			RectangleF r2 = _DrawPanel.GetReportItemRect(m2);

			diff = r2.Left - (r1.Left + r1.Width);
			if (diff < 0)
				diff = 0;
			return diff;
		}

		public void HorzSpacingMakeEqual()
		{
			if (_DrawPanel.SelectedList.Count < 2)
				return;

			HorzSpacing(HorzSpacingDiff());
		}

		public void HorzSpacingIncrease()
		{
			float diff = HorzSpacingDiff() + 8;
			HorzSpacing(diff);
		}

		public void HorzSpacingDecrease()
		{
			float diff = HorzSpacingDiff() - 8;
			if (diff < 0)
				diff = 0;
			HorzSpacing(diff);
		}

		public void HorzSpacingMakeZero()
		{
			HorzSpacing(0);
		}

		void VertSpacing(float diff)
		{
			XmlNode model = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF rectm = _DrawPanel.GetReportItemRect(model);
			if (rectm.Height == float.MinValue)
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Spacing);
			float y = rectm.Top + rectm.Height + diff;
			foreach (XmlNode xNode in _DrawPanel.SelectedList)
			{
				if (xNode == model)
					continue;
				string top = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", y);
				_DrawPanel.SetElement(xNode, "Top", top);
				RectangleF rectn = _DrawPanel.GetReportItemRect(xNode);
				if (rectn.Height == float.MinValue)
					rectn.Height = 16;
				y += (rectn.Height + diff);
			}
			_Undo.EndUndoGroup();

			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		float VertSpacingDiff()
		{
			float diff = 0;
			if (_DrawPanel.SelectedList.Count < 2)
				return diff;

			XmlNode m1 = _DrawPanel.SelectedList[0] as XmlNode;
			RectangleF r1 = _DrawPanel.GetReportItemRect(m1);
			if (r1.Height == float.MinValue)
				return diff;

			XmlNode m2 = _DrawPanel.SelectedList[1] as XmlNode;
			RectangleF r2 = _DrawPanel.GetReportItemRect(m2);

			diff = r2.Top - (r1.Top + r1.Height);
			if (diff < 0)
				diff = 0;
			return diff;
		}

		public void VertSpacingMakeEqual()
		{
			if (_DrawPanel.SelectedList.Count < 2)
				return;

			VertSpacing(VertSpacingDiff());
		}

		public void VertSpacingIncrease()
		{
			float diff = VertSpacingDiff() + 8;
			VertSpacing(diff);
		}

		public void VertSpacingDecrease()
		{
			float diff = VertSpacingDiff() - 8;
			if (diff < 0)
				diff = 0;
			VertSpacing(diff);
		}

		public void VertSpacingMakeZero()
		{
			VertSpacing(0);
		}

		public void SetPadding(string name, int diff)
		{
			if (_DrawPanel.SelectedList.Count < 1)
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Padding);
			foreach (XmlNode n in _DrawPanel.SelectedList)
			{
				XmlNode sNode = this._DrawPanel.GetCreateNamedChildNode(n, "Style");
				if (diff == 0)
					_DrawPanel.SetElement(sNode, name, "0pt");
				else
				{
					float pns = _DrawPanel.GetSize(sNode, name);
					pns += diff;
					if (pns < 0)
						pns = 0;
					string pad = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", pns);
					_DrawPanel.SetElement(sNode, name, pad);
				}
			}
			_Undo.EndUndoGroup();
			ReportChanged(this, new EventArgs());
			_DrawPanel.Invalidate();   
		}

		public void Cut()
		{
			if (_DrawPanel.SelectedCount <= 0)
				return;

			Clipboard.SetDataObject(GetCopy(), true);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Cut);
			_DrawPanel.DeleteSelected();
			_Undo.EndUndoGroup();
			SelectionChanged(this, new EventArgs());
		}

		public void Copy()
		{
			Clipboard.SetDataObject(GetCopy(), true);
		}

		public void Clear()
		{
			return;
		}

		public void Delete()
		{
			if (_DrawPanel.SelectedCount > 0)
			{
				_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Delete);
				_DrawPanel.DeleteSelected();
				_Undo.EndUndoGroup();

				ReportChanged(this, new EventArgs());
				SelectionChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
		}

		public void Paste()
		{
			DoPaste(null,new PointF(0,0));
		}

		public void SelectAll()
		{
			doSelectAll();
		}

		public int SelectionCount
		{
			get { return this._DrawPanel.SelectedCount; }
		}

		public string SelectedText
		{
			get 
			{
				if (_DrawPanel.SelectedCount == 0)
					return "";
				return GetCopy();
			}
		}
		
		public string SelectionName
		{
			get
			{
				if (_DrawPanel.SelectedCount == 0)
					return "";
				if (_DrawPanel.SelectedCount > 1)
					return Strings.DesignCtl_SelectionName_GroupSelection;

				XmlNode xNode = _DrawPanel.SelectedList[0];
				if (xNode.Name == "TableColumn" || xNode.Name == "TableRow")
					return "";

				var xAttr = xNode.Attributes != null ? xNode.Attributes["Name"] : null;

				if (xAttr == null)
					return Strings.DesignCtl_SelectionName_Unnamed;

				XmlNode cNode = _DrawPanel.GetReportItemContainer(xNode);
				if (cNode == null)
					return xAttr.Value;

				var cAttr = cNode.Attributes != null ? cNode.Attributes["Name"] : null;

				if (cAttr == null)
					return xAttr.Value + " " + Strings.DesignCtl_SelectionName_in + " " + cNode.Name;

				var title = xAttr.Value + " " + Strings.DesignCtl_SelectionName_in + " " + cNode.Name.Replace("fyi:", "") + " " + cAttr.Value;
				if (!(cNode.Name == "Table" || cNode.Name == "fyi:Grid"))
					return title;

				XmlNode pNode = xNode.ParentNode.ParentNode;
				if (pNode.Name != "TableCell")
					return title;

				XmlNode trNode = pNode.ParentNode.ParentNode;		// should be TableRow
				if (trNode.Name != "TableRow")
					return title;

				// Find the number of the TableRow -- e.g. 1st, 2nd, 3rd, ...
				int trNumber=1;
				foreach (XmlNode n in trNode.ParentNode.ChildNodes)
				{
					if (n == trNode)
						break;
					trNumber++;
				}

				pNode = trNode.ParentNode.ParentNode;			// Details, Header or Footer

				string rowTitle = trNumber > 1? string.Format("{0}({1})", pNode.Name, trNumber): pNode.Name;
 
				if (pNode.Name == "Details")
					return title + " " + rowTitle;

				// We've got a Header or a Footer; could be a group header/footer
				pNode = pNode.ParentNode;
				if (pNode.Name != "TableGroup")
					return title + " " + rowTitle;

				// We're in a group; find out the group name
				XmlNode gNode = this._DrawPanel.GetNamedChildNode(pNode, "Grouping");
				if (gNode == null)
					return title + " " + rowTitle;

				XmlAttribute gAttr = gNode.Attributes["Name"];
				if (gAttr == null)
					return title + " " + rowTitle;

				return title + ", " + Strings.DesignCtl_SelectionName_Group + " " + gAttr.Value + " " + rowTitle;
			}
		}
		
		public PointF SelectionPosition
		{
			get
			{
				if (_DrawPanel.SelectedCount == 0)
					return new PointF(float.MinValue,float.MinValue);
				XmlNode xNode = _DrawPanel.SelectedList[0];
				return _DrawPanel.SelectionPosition(xNode);
			}
		}

		public SizeF SelectionSize
		{
			get
			{
				if (_DrawPanel.SelectedCount == 0)
					return new SizeF(float.MinValue,float.MinValue);
				XmlNode xNode = _DrawPanel.SelectedList[0];
				return _DrawPanel.SelectionSize(xNode);
			}
		}
		
		
		public StyleInfo SelectedStyle
		{
			get
			{
				if (_DrawPanel.SelectedCount == 0)
					return null;
				XmlNode xNode = _DrawPanel.SelectedList[0];
				// HACK: async
				return Task.Run(async () => await _DrawPanel.GetStyleInfo(xNode)).GetAwaiter().GetResult();
			}
		}

		public void ApplyStyleToSelected(string name, string v)
		{
			if (_DrawPanel.SelectedCount == 0)
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Style);
			_DrawPanel.ApplyStyleToSelected(name, v);
			_Undo.EndUndoGroup(true);
			ReportChanged(this, new EventArgs());
		}

		public void SetSelectedText(string v)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			XmlNode tn = _DrawPanel.SelectedList[0] as XmlNode;
			if (tn == null || tn.Name != "Textbox")
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_TextboxValue);
			_DrawPanel.SetElement(tn, "Value", v);
			_Undo.EndUndoGroup(true);
			_DrawPanel.Invalidate();	// force a repaint
			ReportChanged(this, new EventArgs());
		}

		private void BuildContextMenus()
		{
			BuildContextMenusCustom(MenuDefaultInsert);
		}
        
        private void BuildContextMenusCustom(ToolStripMenuItem menuItem)
        {
            try
            {
                var sa = RdlEngineConfig.GetCustomReportTypes();
                if (sa == null || sa.Length == 0)
                    return;


	            var separator = menuItem.DropDownItems["CustomSeparator"];

				if (separator == null)
				{
					separator = new ToolStripSeparator { Name = "CustomSeparator" };
					menuItem.DropDownItems.Add(separator);       // put a separator
				}
				else
				{
					var index = menuItem.DropDownItems.IndexOf(separator) + 1;

					while (menuItem.DropDownItems.Count > index)
					{
						menuItem.DropDownItems.RemoveAt(index);						
					}
				}

                // Add the custom report items to the insert menu
                foreach (var m in sa)
                {
	                var mi = new ToolStripMenuItem(m + "...");
	                mi.Click += menuInsertCustomReportItem_Click;
                    mi.Tag = m;
					menuItem.DropDownItems.Add(mi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Strings.DesignCtl_ShowB_CustomReportItemError, ex.Message), Strings.DesignCtl_Show_Insert, MessageBoxButtons.OK);
            }   
        }

        private Bitmap _buffer;
        // HACK: async shenanigans
        bool doGraphicsDraw;
        private async void DrawPanelPaint(object sender, Majorsilence.Forms.PaintEventArgs e)
        {
            // HACK: async shenanigans
            if (doGraphicsDraw && _buffer != null)
            {
                e.Graphics.DrawImage(_buffer, 0, 0);
                _buffer.Dispose();
                _buffer = null;
                doGraphicsDraw = false;
            }
            else
            {
                // HACK: async shenanigans
                await Internal_DrawPanelPaint();
                doGraphicsDraw = true;
                // HACK: async shenanigans, force a repaint where e.Graphics is still valid
                _DrawPanel.Invalidate();       
            }
        }

        private async Task Internal_DrawPanelPaint()
        {
            // Only handle one paint at a time
            lock (this)
            {
                if (_InPaint)
                    return;
                _InPaint = true;
            }

            // Create a self-contained Graphics object
            _buffer = new Bitmap(Math.Max(1, _DrawPanel.Width), Math.Max(1, _DrawPanel.Height));
            using (Graphics g = Graphics.FromImage(_buffer))
            {
                try // never want to die in here
                {
                    if (this._ReportDoc == null) // if no report force the simplest one
                        CreateEmptyReportDoc();

                    //g.ClipBounds;
                    var clip = new Rectangle(PixelsX(_hScroll.Value), PixelsY(_vScroll.Value),
                        PixelsX(_DrawPanel.Width), PixelsY(_DrawPanel.Height));
                    // Draw the report asynchronously
                    await _DrawPanel.Draw(g, PointsX(_hScroll.Value), PointsY(_vScroll.Value), clip);
                }
                catch (Exception ex)
                { // don't want to kill process if we die -- put up some kind of error message
                    StringFormat format = new StringFormat();
                    string msg = string.Format("Error drawing report. Likely error in syntax. Switch to syntax and correct report syntax.{0}{1}{0}{2}",
                        Environment.NewLine, ex.Message, ex.StackTrace);
                    g.DrawString(msg, this.Font, Brushes.Black, new Rectangle(2, 2, this.Width, this.Height), format);
                }
            }

            lock (this)
            {
                _InPaint = false;
            }
        }

        private void CreateEmptyReportDoc()
		{
			XmlDocument xDoc = new XmlDocument();
			xDoc.PreserveWhitespace = false;
			xDoc.LoadXml("<Report><Body><Height>0pt</Height></Body></Report>");
			ReportDocument = xDoc;
		}

		private void DrawPanelResize(object sender, EventArgs e)
		{
			_DrawPanel.Refresh();
			SetScrollControls();			

		}

		internal float PointsX(float x)		// pixels to points
		{
            return x * DesignXmlDraw.POINTSIZED / _DpiX;
		}

		internal float PointsY(float y)
		{
            return y * DesignXmlDraw.POINTSIZED / _DpiY;
		}

		internal int PixelsX(float x)		// points to pixels
		{
            return (int)(x * _DpiX / DesignXmlDraw.POINTSIZED);
		}

		internal int PixelsY(float y)
		{
            return (int)(y * _DpiY / DesignXmlDraw.POINTSIZED);
		}


		internal void SetScrollControls()
		{
			if (_ReportDoc == null)		// nothing loaded; nothing to do
			{
				_vScroll.Enabled = _hScroll.Enabled = false;
				_vScroll.Value = _hScroll.Value = 0;
				return;
			}
			SetScrollControlsV();
			SetScrollControlsH();
		}

		private void SetScrollControlsV()
		{
			// calculate the vertical scroll needed
			int h = PixelsY(_DrawPanel.VerticalMax);	// size we need to show
			int sh = this.Height - _hScroll.Height;
			if (sh > h || sh < 0)
			{
				_vScroll.Enabled = false;
				if (_vScroll.Value != 0)
				{
					_vScroll.Value = 0;
					_DrawPanel.Invalidate();	// force a repaint
				}
				return;
			}

			_vScroll.Minimum = 0;
			_vScroll.Maximum = h;
			int sValue =  Math.Min(_vScroll.Value, _vScroll.Maximum);
			if (_vScroll.Value != sValue)
			{
				_vScroll.Value = sValue;
				_DrawPanel.Invalidate();		// force a repaint
			}
			_vScroll.LargeChange = sh;
			_vScroll.SmallChange = _vScroll.LargeChange / 5;
			_vScroll.Enabled = true;
			return;
		}

		private void SetScrollControlsH()
		{
			int w = PixelsX(_DrawPanel.HorizontalMax);
			int sw = this.Width - _hScroll.Width;
			if (sw > w)
			{
				_hScroll.Enabled = false;
				if (_hScroll.Value != 0)
				{
					_hScroll.Value = 0;
					_DrawPanel.Invalidate();
				}
				return;
			}
			_hScroll.Maximum = w;
			_hScroll.Minimum = 0;
			int sValue =  Math.Min(_hScroll.Value, _hScroll.Maximum);
			if (_hScroll.Value != sValue)
			{
				_hScroll.Value = sValue;
				_DrawPanel.Invalidate();
			}
			if (sw < 0)
				sw = 0;
			_hScroll.LargeChange = sw;
			_hScroll.SmallChange = _hScroll.LargeChange / 5;
			_hScroll.Enabled = true;
			_hScroll.Visible=true;
			return;
		}

		private new void HorizontalScroll(object sender, Majorsilence.Forms.ScrollEventArgs e)
		{
			if (e.NewValue == _hScroll.Value)	// don't need to scroll if already there
				return;

			_DrawPanel.Invalidate();
            if (HorizontalScrollChanged != null)
                HorizontalScrollChanged(this, new EventArgs());
        }

		private new void VerticalScroll(object sender, Majorsilence.Forms.ScrollEventArgs e)
		{
			if (e.NewValue == _vScroll.Value)	// don't need to scroll if already there
				return;

			_DrawPanel.Invalidate();
            if (VerticalScrollChanged != null)
                VerticalScrollChanged(this, new EventArgs());
		}

		private void DrawPanelMouseUp(object sender, MouseEventArgs E)
		{
            MouseEventArgsE e = new MouseEventArgsE(E, SCALEX, SCALEY);

            if (e.Button == MouseButtons.Left)
				_Undo.EndUndoGroup(true);

            if (_MouseDownNode != null && _MouseDownNode.Name == "Height")
                HeightChanged(this, new HeightEventArgs(_MouseDownNode, null));     // reset any mousemove
            
			_MouseDownNode = null;
			
			if (this._bHaveMouse)
			{	// Handle the end of the rubber banding
				_bHaveMouse = false;
				// remove last rectangle if necessary
				if (this._ptRBLast.X != -1)
				{
					this.DrawPanelRubberBand(this._ptRBOriginal, this._ptRBLast);
					// Process the rectangle
					Rectangle r = DrawPanelRectFromPoints(this._ptRBOriginal, this._ptRBLast);
					if ((Control.ModifierKeys & Keys.Control) != Keys.Control)	// we allow addition to selection
						_DrawPanel.ClearSelected();
					_DrawPanel.SelectInRectangle(r, PointsX(_hScroll.Value), PointsY(_vScroll.Value));
					SelectionChanged(this, new EventArgs());
				}
				// clear out the points for the next time
				_ptRBOriginal.X = _ptRBOriginal.Y = _ptRBLast.X = _ptRBLast.Y = -1;
			}
			else if (e.Button == MouseButtons.Right)
			{
				DrawPanelContextMenu(new Point(e.X, e.Y));
			}
			if (_AdjustScroll)
			{
				this.SetScrollControls();
				_AdjustScroll = false;
			}
		}

		private void DrawPanelContextMenu(Point p)
		{
			if (_DrawPanel.SelectedCount == 1)
			{
				XmlNode cNode = _DrawPanel.SelectedList[0];
				if (cNode.Name == "Chart")
					DrawPanelContextMenuChart(p, cNode);
				else if (cNode.Name == "Subreport")
					DrawPanelContextMenuSubreport(p, cNode);
				else if (_DrawPanel.InTable(cNode))
					DrawPanelContextMenuTable(p, cNode);
				else if (_DrawPanel.InMatrix(cNode))
					DrawPanelContextMenuMatrix(p, cNode);
				else
					DrawPanelContextMenuDefault(p);
			}
			else
				DrawPanelContextMenuDefault(p);
		}

		private void DrawPanelContextMenuChart(Point p, XmlNode riNode)
		{
			// Get the Category Groupings
			var catGroupNames = _DrawPanel.GetChartCategoryGroupNames(riNode);

			MenuChartEditCategoryGrouping.DropDownItems.Clear();
			MenuChartDeleteCategoryGrouping.DropDownItems.Clear();
			MenuChartEditCategoryGrouping.Enabled = catGroupNames != null;
			MenuChartDeleteCategoryGrouping.Enabled = catGroupNames != null;

			if (catGroupNames != null)
			{
				foreach (var gname in catGroupNames)
				{
					var em = new ToolStripMenuItem(gname);
					em.Click += menuChartEditGrouping_Click;
					MenuChartEditCategoryGrouping.DropDownItems.Add(em);

					em = new ToolStripMenuItem(gname);
					em.Click += menuChartDeleteGrouping_Click;
					MenuChartDeleteCategoryGrouping.DropDownItems.Add(em);
				}
			}

			// Get the Series Groupings
			var serGroupNames = _DrawPanel.GetChartSeriesGroupNames(riNode);

			MenuChartEditSeriesGrouping.DropDownItems.Clear();
			MenuChartDeleteSeriesGrouping.DropDownItems.Clear();
			MenuChartEditSeriesGrouping.Enabled = serGroupNames != null;
			MenuChartDeleteSeriesGrouping.Enabled = serGroupNames != null;

			if (serGroupNames != null)
			{
				foreach (var gname in serGroupNames)
				{
					var em = new ToolStripMenuItem(gname);
					em.Click += menuChartEditGrouping_Click;
					MenuChartEditSeriesGrouping.DropDownItems.Add(em);

					em = new ToolStripMenuItem(gname);
					em.Click += menuChartDeleteGrouping_Click;
					MenuChartDeleteSeriesGrouping.DropDownItems.Add(em);
				}
			}

			ContextMenuChart.Show(this, p);
		}

		private void DrawPanelContextMenuDefault(Point p)
		{
			ContextMenuDefault.Show(this, p);
		}

		private void DrawPanelContextMenuMatrix(Point p, XmlNode riNode)
		{
			// Get the column groupings
			var colGroupNames = _DrawPanel.GetMatrixColumnGroupNames(riNode);
			var rowGroupNames = _DrawPanel.GetMatrixRowGroupNames(riNode);

			MenuMatrixEditColumnGroup.DropDownItems.Clear();
			MenuMatrixDeleteColumnGroup.DropDownItems.Clear();
			MenuMatrixEditRowGroup.DropDownItems.Clear();
			MenuMatrixDeleteRowGroup.DropDownItems.Clear();

			MenuMatrixEditColumnGroup.Enabled = colGroupNames != null;
			MenuMatrixDeleteColumnGroup.Enabled = colGroupNames != null;
			MenuMatrixEditRowGroup.Enabled = rowGroupNames != null;
			MenuMatrixDeleteRowGroup.Enabled = rowGroupNames != null;

			if (colGroupNames != null)
			{
				foreach (var gname in colGroupNames)
				{
					var em = new ToolStripMenuItem(gname);
					em.Click += menuMatrixEditGroup_Click;
					MenuMatrixEditColumnGroup.DropDownItems.Add(em);

					em = new ToolStripMenuItem(gname);
					em.Click += menuMatrixDeleteGroup_Click;
					MenuMatrixDeleteColumnGroup.DropDownItems.Add(em);
				}
			}

			if (rowGroupNames != null)
			{
				foreach (var gname in rowGroupNames)
				{
					var em = new ToolStripMenuItem(gname);
					em.Click += menuMatrixEditGroup_Click;
					MenuMatrixEditRowGroup.DropDownItems.Add(em);

					em = new ToolStripMenuItem(gname);
					em.Click += menuMatrixDeleteGroup_Click;
					MenuMatrixDeleteRowGroup.DropDownItems.Add(em);
				}
			}

			ContextMenuMatrix.Show(this, p);
		}

		private void DrawPanelContextMenuSubreport(Point p, XmlNode sr)
		{
			ContextMenuSubreport.Show(this, p);
		}

		private void DrawPanelContextMenuTable(Point p, XmlNode riNode)
		{
			var bTable = !_DrawPanel.InGrid(riNode);
			var tblGroupNames = _DrawPanel.GetTableGroupNames(riNode);

			if (!bTable)
			{
				ContextMenuGrid.Show(this, p);
			}
			else
			{
				MenuTableEditGroup.DropDownItems.Clear();
				MenuTableDeleteGroup.DropDownItems.Clear();

				MenuTableEditGroup.Enabled = tblGroupNames != null;
				MenuTableDeleteGroup.Enabled = tblGroupNames != null;

				if (tblGroupNames != null)
				{
					foreach (var gname in tblGroupNames)
					{
						var em = new ToolStripMenuItem(gname);
						em.Click += menuTableEditGroup_Click;
						MenuTableEditGroup.DropDownItems.Add(em);

						em = new ToolStripMenuItem(gname);
						em.Click += menuTableDeleteGroup_Click;
						MenuTableDeleteGroup.DropDownItems.Add(em);
					}
				}

				ContextMenuTable.Show(this, p);
			}
		}

		private void DrawPanelMouseMove(object sender, MouseEventArgs E)
		{
            MouseEventArgsE e = new MouseEventArgsE(E, SCALEX, SCALEY);

            XmlNode b=null;					
			HitLocationEnum hle = HitLocationEnum.Inside;
			Point newMousePosition = new Point(e.X, e.Y);

			if (_bHaveMouse)
			{	// we're rubber banding
				// If we drew previously; we'll draw again to remove old rectangle
				if( this._ptRBLast.X != -1 )
				{
					this.DrawPanelRubberBand( this._ptRBOriginal, _ptRBLast );
				}
				_MousePosition = newMousePosition;
				// Update last point;  but don't rubber band outside our client area
				if (newMousePosition.X < 0)
					newMousePosition.X = 0;
				if (newMousePosition.X > _DrawPanel.Width)
					newMousePosition.X = _DrawPanel.Width;
				if (newMousePosition.Y < 0)
					newMousePosition.Y = 0;
				if (newMousePosition.Y > _DrawPanel.Height)
					newMousePosition.Y = _DrawPanel.Height;
				_ptRBLast = newMousePosition;
				if (_ptRBLast.X < 0)
					_ptRBLast.X = 0;
				if (_ptRBLast.Y < 0)
					_ptRBLast.Y = 0;
				// Draw new lines.
				this.DrawPanelRubberBand( _ptRBOriginal, newMousePosition );
				this.Cursor = Cursors.Cross;		// use cross hair to indicate drawing
				return;
			}
			else if (_MouseDownNode != null)
			{  
				if (e.Button != MouseButtons.Left)
					b = _MouseDownNode;
				else 
				{
					b = _MouseDownNode;
					switch (_MouseDownNode.Name)
					{
						case "TableColumn":
						case "RowGrouping":
						case "MatrixColumn":
							hle = HitLocationEnum.TableColumnResize;
							if (e.X == _MousePosition.X)
								break;

							if (_DrawPanel.TableColumnResize(_MouseDownNode, e.X - _MousePosition.X))
							{
								SelectionMoved(this, new EventArgs());
								ReportChanged(this, new EventArgs());
								_AdjustScroll = true;
								_DrawPanel.Invalidate();   
							}
							else	// trying to drag into invalid area; disallow
							{
								Cursor.Position = this.PointToScreen(_MousePosition);
								newMousePosition = this.PointToClient(Cursor.Position);
							}
							break;
						case "TableRow":
						case "ColumnGrouping":
						case "MatrixRow":
							hle = HitLocationEnum.TableRowResize;
							if (e.Y == _MousePosition.Y)
								break;
							if (_DrawPanel.TableRowResize(_MouseDownNode, e.Y - _MousePosition.Y))
							{
								SelectionMoved(this, new EventArgs());
								ReportChanged(this, new EventArgs());
								_DrawPanel.Invalidate();   
							}
							else	// trying to drag into invalid area; disallow
							{
								Cursor.Position = this.PointToScreen(_MousePosition);
								newMousePosition = this.PointToClient(Cursor.Position);
							}
							break;
						case "Height":
							if (e.Y == _MousePosition.Y)
								break;
							if (_DrawPanel.ChangeHeight(_MouseDownNode, e.Y - _MousePosition.Y, 0))
							{
								ReportChanged(this, new EventArgs());
                                HeightChanged(this, new HeightEventArgs(_MouseDownNode, b.InnerText));
								_DrawPanel.Invalidate();   
								_AdjustScroll = true;		// this will force scroll bars to be adjusted on MouseUp
							}
							else	// trying to drag into invalid area; disallow
							{
								Cursor.Position = this.PointToScreen(_MousePosition);
								newMousePosition = this.PointToClient(Cursor.Position);
							}
                            // Force scroll when off end of page
                            //if (e.Y > _DrawPanel.Height)
                            //{
                            //    int hs = _vScroll.Value + _vScroll.SmallChange;
                            //    _vScroll.Value = Math.Min(_vScroll.Maximum, hs);
                            //    _DrawPanel.Refresh();
                            //}

                            break;
						case "Textbox":
						case "Image":
						case "Rectangle":
						case "List":
						case "Table":
                        case "fyi:Grid":
						case "Matrix":
						case "Chart":
						case "Subreport":
						case "Line":
                        case "CustomReportItem":
							hle = this._MouseDownLoc;
							if (e.Y == _MousePosition.Y && e.X == _MousePosition.X)
								break;

							if (_DrawPanel.MoveSelectedItems(e.X - _MousePosition.X, e.Y - _MousePosition.Y, this._MouseDownLoc))
							{
								SelectionMoved(this, new EventArgs());
								ReportChanged(this, new EventArgs());
								_DrawPanel.Invalidate();   
								_AdjustScroll = true;
							}
							else	// trying to drag into invalid area; disallow
							{
								Cursor.Position = this.PointToScreen(_MousePosition);
								newMousePosition = this.PointToClient(Cursor.Position);
							}

							break;
					}
				}
			}
			else
			{
				HitLocation hl = _DrawPanel.HitNode(newMousePosition, PointsX(_hScroll.Value), PointsY(_vScroll.Value));
				if (hl != null)
				{
					b = hl.HitNode;
					hle = hl.HitSpot;
				}
			}

			_MousePosition = newMousePosition;
			DrawPanelSetCursor(b, hle);
		}

		private void DrawPanelMouseDown(object sender, MouseEventArgs E)
		{
            MouseEventArgsE e = new MouseEventArgsE(E, SCALEX, SCALEY);

            bool baseOnly = false; //Josh: Added so base form can be force selected for inserting/selecting
            //Hold shift to select the base form instead of the control the mouse is over.
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                baseOnly = true;
            } 

			_MousePosition = new Point(e.X, e.Y);
            HitLocation hl = _DrawPanel.HitNode(_MousePosition, PointsX(_hScroll.Value), PointsY(_vScroll.Value), baseOnly); 
			_MouseDownNode = hl == null? null: hl.HitNode;
			_MouseDownLoc = hl == null? HitLocationEnum.Inside: hl.HitSpot;

			if (DrawPanelMouseDownInsert(hl, sender, e))		// Handle ReportItem insertion
				return;

			if (e.Button == MouseButtons.Left)
				_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Move_Size);

			if (DrawPanelMouseDownRubberBand(sender, e))	// Handle rubber banding
				return;

			if (DrawPanelMouseDownTableColumnResize(sender, e))	// Handle column resize
				return;

			if (DrawPanelMouseDownTableRowResize(sender, e))	// Handle row resize
				return;
			
			if (_MouseDownNode.Name == "Height")
			{
				_DrawPanel.ClearSelected();
				SelectionChanged(this, new EventArgs());
                HeightChanged(this, new HeightEventArgs(_MouseDownNode, _MouseDownNode.InnerText)); // Set the height
            }
			else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
			{
				_DrawPanel.AddRemoveSelection(_MouseDownNode);
				SelectionChanged(this, new EventArgs());
			}
			else
			{
				_DrawPanel.SetSelection(_MouseDownNode, true);
				SelectionChanged(this, new EventArgs());
			}

			DrawPanelSetCursor(_MouseDownNode, hl.HitSpot);
		}

        internal void SetSelection(XmlNode node)
        {
            if (node == null)
                _DrawPanel.ClearSelected();
            else
                _DrawPanel.SetSelection(node, false);
            SelectionChanged(this, new EventArgs());
        }

		private bool DrawPanelMouseDownRubberBand(object sender, MouseEventArgsE e)
		{
            if (_MouseDownLoc != HitLocationEnum.Inside)
				return false;				// must hit inside a region

			// Now determine if object hit allows for rubber banding
			bool bRubber = false;
			bool bDeselect = true;
			if (_MouseDownNode == null)
				bRubber = true;
			else if (_MouseDownNode.Name == "List" || _MouseDownNode.Name == "Rectangle")
			{
				if (_DrawPanel.SelectedCount == 1 && _DrawPanel.IsNodeSelected(_MouseDownNode))
				{
					bRubber = true;
					bDeselect = false;
				}
			}
			else if (_MouseDownNode.Name == "Body" ||
				_MouseDownNode.Name == "PageHeader" ||
				_MouseDownNode.Name == "PageFooter")
				bRubber = true;
			
			if (!bRubber)
				return false;

			// We have a rubber band operation
			if (e.Button != MouseButtons.Left)
			{
				if (bDeselect)
				{
					_DrawPanel.ClearSelected();
					SelectionChanged(this, new EventArgs());
				}
				return true;		// well no rubber band but it's been handled
			}

			if ((Control.ModifierKeys & Keys.Control) != Keys.Control)	// we allow addition to selection
			{
				if (bDeselect)
				{
					_DrawPanel.ClearSelected();
					SelectionChanged(this, new EventArgs());
				}
			}
			_bHaveMouse = true;
			// keep the starting point of the rectangular rubber band
			this._ptRBOriginal.X = e.X;
			this._ptRBOriginal.Y = e.Y;
			// -1 indicates no previous rubber band
			this._ptRBLast.X = this._ptRBLast.Y = -1;
			this.Cursor = Cursors.Cross;		// use cross hair to indicate drawing

			return true;
		}

		private bool DrawPanelMouseDownTableColumnResize(object sender, MouseEventArgsE e)
		{
            if (_MouseDownNode == null ||							
				_MouseDownLoc != HitLocationEnum.TableColumnResize)
				return false;
			this.Cursor = Cursors.VSplit;
			return true;
		}

		private bool DrawPanelMouseDownTableRowResize(object sender, MouseEventArgsE e)
		{
            if (_MouseDownNode == null ||							
				_MouseDownLoc != HitLocationEnum.TableRowResize)
				return false;
			this.Cursor = Cursors.HSplit;
			return true;
		}

		private bool DrawPanelMouseDownInsert(HitLocation hl, object sender, MouseEventArgsE e)
		{
            if (!(_CurrentInsert != null &&		// should we be inserting?
				_MouseDownNode != null &&			
				(_MouseDownNode.Name == "List" ||
				_MouseDownNode.Name == "Rectangle" ||
				_MouseDownNode.Name == "Body" ||
				_MouseDownNode.Name == "PageHeader" ||
				_MouseDownNode.Name == "PageFooter")))
			{
				if (_CurrentInsert == null || _CurrentInsert == "Line" || hl == null || 
						hl.HitContainer == null || (!(hl.HitContainer.Name == "Table" || hl.HitContainer.Name=="fyi:Grid")))
				   return false;
				
				if (MessageBox.Show(Strings.DesignCtl_ShowB_WantReplaceCell, Strings.DesignCtl_Show_Insert, MessageBoxButtons.YesNo) != DialogResult.Yes)
					return false;
			}
			switch (_CurrentInsert)
			{
				case "Textbox":
					menuInsertTextbox_Click(sender, e);
					break;
				case "Chart":
					menuInsertChart_Click(sender, e);
					break;
				case "Rectangle":
					menuInsertRectangle_Click(sender, e);
					break;
				case "Table":
                case "fyi:Grid":
                    menuInsertTable_Click(sender, e);
					break;
                case "Matrix":
					menuInsertMatrix_Click(sender, e);
					break;
				case "List":
					menuInsertList_Click(sender, e);
					break;
				case "Line":
					menuInsertLine_Click(sender, e);
					break;
				case "Image":
					menuInsertImage_Click(sender, e);
					break;
				case "Subreport":
					menuInsertSubreport_Click(sender, e);
					break;
				default:
					var types = RdlEngineConfig.GetCustomReportTypes();
					if (types.Contains(_CurrentInsert))
					{
						menuInsertCustomReportItem(sender, e, _CurrentInsert);
					}
					break;
			}
			return true;
		}

		private void DrawPanelDoubleClick(object sender, EventArgs e)
		{
			menuProperties_Click();		// treat double click like a property menu click
		}

		// ControlPaint.DrawReversibleFrame doesn't exist in Majorsilence.Forms -- classic GDI+
		// XOR-mode direct-to-screen drawing with no equivalent in a SkiaSharp/compositing-based
		// renderer. Same documented gap and fix as RdlViewer.Forms/PageDrawing.cs's RubberBand
		// (D2): the selection logic is unaffected, only the live-drag visual feedback is gone.
		private void DrawPanelRubberBand(Point p1, Point p2)
		{
		}

		private Rectangle DrawPanelRectFromPoints(Point p1, Point p2)
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

		private void DrawPanelSetCursor(XmlNode node, HitLocationEnum hle)
		{
			Cursor c;
			if (node == null)
				c = Cursors.Arrow;
			else if (node.Name == "Height")
				c = Cursors.SizeNS;
			else if (hle == HitLocationEnum.TableColumnResize)	// doesn't need to be selected
				c = Cursors.VSplit;
			else if (hle == HitLocationEnum.TableRowResize)		// doesn't need to be selected 
				c = Cursors.HSplit;
			else if (this._DrawPanel.IsNodeSelected(node))
			{
				switch (hle)
				{
					case HitLocationEnum.BottomLeft:
					case HitLocationEnum.TopRight:
						c = Cursors.SizeNESW;
						break;
					case HitLocationEnum.LeftMiddle:
					case HitLocationEnum.RightMiddle:
						c = Cursors.SizeWE;
						break;
					case HitLocationEnum.BottomRight:
					case HitLocationEnum.TopLeft:
						c = Cursors.SizeNWSE;
						break;
					case HitLocationEnum.TopMiddle:
					case HitLocationEnum.BottomMiddle:
						c = Cursors.SizeNS;
						break;
					case HitLocationEnum.Move:
						c = Cursors.SizeAll;
						break;
					case HitLocationEnum.TableColumnResize:
						c = Cursors.VSplit;
						break;
					case HitLocationEnum.TableRowResize:
						c = Cursors.HSplit;
						break;
					case HitLocationEnum.LineLeft:
					case HitLocationEnum.LineRight:
				//		c = Cursors.Cross;			bug in C# runtime? Cross doesn't work!
						c = Cursors.Hand;
						break;
					case HitLocationEnum.Inside:
					default:
						c = Cursors.Arrow;
						break;
				}
			}
			else
				c = Cursors.Arrow;

			if (c != this.Cursor)
				this.Cursor = c;
		}

		private void DrawPanelMouseWheel(object sender, MouseEventArgs E)
		{
            MouseEventArgsE e = new MouseEventArgsE(E, SCALEX, SCALEY);

            if (!_vScroll.Enabled)
                return;                 // scroll not enabled
			int wvalue;
            bool bNotify = false;
			if (e.Delta < 0)
			{
				if (_vScroll.Value < _vScroll.Maximum)
				{
					wvalue = _vScroll.Value + _vScroll.SmallChange;

					_vScroll.Value = Math.Min(_vScroll.Maximum, wvalue);
                    bNotify = true;
				}
			}
			else 
			{
				if (_vScroll.Value > _vScroll.Minimum)
				{
					wvalue = _vScroll.Value - _vScroll.SmallChange;

					_vScroll.Value = Math.Max(_vScroll.Minimum, wvalue);
                    bNotify = true;
				}
			}
            if (bNotify)
            {
                if (VerticalScrollChanged != null)
                    VerticalScrollChanged(this, new EventArgs());
                _DrawPanel.Refresh();
            }
		}

		private void DrawPanelKeyDown(object sender, KeyEventArgs e)
		{
			int incX=0;
			int incY=0;
			int vScroll=_vScroll.Value;
			int hScroll=_hScroll.Value;

			// Force scroll up and down
			if (e.KeyCode == Keys.Down)  
			{
				incY=1;
			}
			else if (e.KeyCode == Keys.Up)
			{
				incY=-1;
			}
			else if (e.KeyCode == Keys.Left)
			{
				incX=-1;
			}
			else if (e.KeyCode == Keys.Right)
			{
				incX=1;
			}
			else if (e.KeyCode == Keys.PageDown)
			{
				vScroll = Math.Min(_vScroll.Value + _vScroll.LargeChange, _vScroll.Maximum);
			}
			else if (e.KeyCode == Keys.PageUp)
			{
				vScroll = Math.Max(_vScroll.Value - _vScroll.LargeChange, 0);
			}
			else if (e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				menuProperties_Click();
				return;
			}
			else if (e.KeyCode == Keys.Tab)
			{
				if (_DrawPanel.SelectNext((Control.ModifierKeys & Keys.Shift) == Keys.Shift))
				{
					RectangleF r = _DrawPanel.GetRectangle(_DrawPanel.SelectedList[0]);
					Rectangle nr = new Rectangle(PixelsX(r.X), PixelsY(r.Y), PixelsX(r.Width), PixelsY(r.Height));
					if (nr.Right > _hScroll.Value + Width - _vScroll.Width ||
						nr.Left < _hScroll.Value - _vScroll.Width)
						hScroll = Math.Min(nr.Left, _hScroll.Maximum);
					if (nr.Bottom > _vScroll.Value + Height - _hScroll.Height ||
						nr.Top < _vScroll.Value - _hScroll.Height)
						vScroll = Math.Min(nr.Top, _vScroll.Maximum);
					this.SelectionChanged(this, new EventArgs());
					_DrawPanel.Invalidate();   
				}
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Delete)
			{
				this.Delete();
				e.Handled = true;
			}
			
			bool bRefresh = false;
			if (vScroll != _vScroll.Value && _vScroll.Enabled)
			{
				_vScroll.Value = Math.Max(vScroll, 0);
				bRefresh = true;
				e.Handled = true;
			}

			if (hScroll != _hScroll.Value && _hScroll.Enabled)
			{
				_hScroll.Value = Math.Max(hScroll,0);
				bRefresh = true;
				e.Handled = true;
			}

			if (incX != 0 || incY != 0)
			{
				HitLocationEnum hle = HitLocationEnum.Move;
				if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)	// if shift key on resize
				{
					hle = incX != 0? HitLocationEnum.RightMiddle: HitLocationEnum.BottomMiddle;
				}
				_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Move);
				if (_DrawPanel.MoveSelectedItems(incX, incY, hle))
				{
					_Undo.EndUndoGroup(true);
					SelectionMoved(this, new EventArgs());
					ReportChanged(this, new EventArgs());
					_DrawPanel.Invalidate();   
				}
				else
					_Undo.EndUndoGroup(false);
				e.Handled = true;
			}

			if (bRefresh)
				_DrawPanel.Refresh();
		}

		private void DesignCtl_Layout(object sender, LayoutEventArgs e)
		{
			_DrawPanel.Location = new Point(0, 0);
			_DrawPanel.Width = this.Width - _vScroll.Width;
			_DrawPanel.Height = this.Height - _hScroll.Height;
			_hScroll.Location = new Point(0, this.Height - _hScroll.Height);
			_hScroll.Width = _DrawPanel.Width;
			_vScroll.Location = new Point(this.Width - _vScroll.Width, _DrawPanel.Location.Y);
			_vScroll.Height = _DrawPanel.Height;

		}

		
		private void menuCopy_Click(object sender, EventArgs e)
		{
			Clipboard.SetDataObject(GetCopy(), true);
		}
		
		private string GetCopy()
		{
            // When copying multiple report items that are in the same table/matrix 
            //   ask if you want to instead copy the table/matrix
            if (_DrawPanel.SelectedCount > 1)
            {
                XmlNode tn = _DrawPanel.TMParent(_DrawPanel.SelectedList[0]);
                bool bTorM = tn != null;

                for (int i=1; i < _DrawPanel.SelectedCount && bTorM; i++)
                {
                    if (tn != _DrawPanel.TMParent(_DrawPanel.SelectedList[i]))
                        bTorM = false;
                }
                if (bTorM)
                {   // all selected items are in the same table or matrix
                    if (MessageBox.Show(string.Format(Strings.DesignCtl_ShowB_WantSelect, tn.Name),
                        Strings.DesignCtl_ShowB_Copy, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        StringBuilder tb = new StringBuilder();
                        // Build XML representing the selected objects
                        tb.Append("<ReportItems>");		// surround by ReportItems element
                        tb.Append(tn.OuterXml);
                        tb.Append("</ReportItems>");
                        return tb.ToString();
                    }
                }
            }


			StringBuilder sb = new StringBuilder();
			// Build XML representing the selected objects
			sb.Append("<ReportItems>");		// surround by ReportItems element
			foreach (XmlNode riNode in _DrawPanel.SelectedList)
			{
				sb.Append(riNode.OuterXml);	
			}
			sb.Append("</ReportItems>");
			return sb.ToString();
		}
				
		private void menuPaste_Click(object sender, EventArgs e)
		{
			HitLocation hl = _DrawPanel.HitNode(_MousePosition, PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			XmlNode lNode = hl == null? null: hl.HitNode;

			if (lNode == null)
				return;
			if (!(lNode.Name == "List" ||
				lNode.Name == "Body" ||
				lNode.Name == "Rectangle" ||
				lNode.Name == "PageHeader" ||
				lNode.Name == "PageFooter"))
			{
				if (hl.HitContainer != null && (hl.HitContainer.Name == "Table" || hl.HitContainer.Name == "fyi:Grid"))
				{
				//	When table we need to replace the tablecell contents; ask first
					if (MessageBox.Show(Strings.DesignCtl_ShowB_WantReplaceCell, Strings.DesignCtl_Show_Paste, MessageBoxButtons.YesNo) != DialogResult.Yes)
						return;

					XmlNode repItems = lNode.ParentNode;
					if (repItems.Name != "ReportItems")
						return;

					XmlNode p = repItems.ParentNode;
					p.RemoveChild(repItems);

					DoPaste(p, hl.HitRelative);
				}
				return;
			}

			DoPaste(lNode, hl.HitRelative);
		}

		private void DoPaste(XmlNode lNode, PointF p)
		{
			IDataObject iData = Clipboard.GetDataObject();
			if (iData == null)
				return;
			if (!(iData.GetDataPresent(DataFormats.Text) ||
				  iData.GetDataPresent(DataFormats.Bitmap))) 
				return;

			if (lNode == null)
			{
				lNode = _DrawPanel.Body;
				if (lNode == null)
					return;
			}

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Paste);
			if (iData.GetDataPresent(DataFormats.Text))
			{
				// Build the xml string in case it is a straight pasting of text
				string t = (string) iData.GetData(DataFormats.Text);
				if (!(t.Length >=	27 && t.Substring(0, 13) == "<ReportItems>"))
				{
					t = t.Replace("&", "&amp;");
					t = t.Replace("<", "&lt;");
					// Josh: Added a default ZIndex of 1 so that a "Backgroud" image can be applied to the form.
                    t = string.Format("<ReportItems><Textbox><Height>12pt</Height><Width>1in</Width><Value>{0}</Value><ZIndex>1</ZIndex></Textbox></ReportItems>", t); 
				}
				// PasteReportItems throws exception when you try to paste an illegal object
				try	
				{
					_DrawPanel.PasteReportItems(lNode, t, p);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, Strings.DesignCtl_Show_Paste);
				}
			}
			else
			{
				// Build the xml string for an image; but we also need to put an
				//   embedded image into the report.
				Majorsilence.Forms.Drawing.Bitmap bo = (Majorsilence.Forms.Drawing.Bitmap) iData.GetData(DataFormats.Bitmap);

				_DrawPanel.PasteImage(lNode, bo, p);
			}
			_Undo.EndUndoGroup();
			_DrawPanel.Invalidate();
			ReportChanged(this, new EventArgs());
			SelectionChanged(this, new EventArgs());
		}
				
		private void menuDelete_Click(object sender, EventArgs e)
		{
			Delete();
		}
	
		private void menuInsertChart_Click(object sender, EventArgs e)
		{
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;

			// Charts aren't allowed in PageHeader or PageFooter
			if (_DrawPanel.InPageHeaderOrFooter(hl.HitContainer))
			{
				MessageBox.Show(Strings.DesignCtl_Show_ChartsInsert, Strings.DesignCtl_Show_Insert);
				return;
			}

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertChart);
			DialogNewChart dnc = new DialogNewChart(this._DrawPanel, hl.HitContainer);
            try
            {
                DialogResult dr = dnc.ShowDialog(this);
                if (dr != DialogResult.OK)
                {
                    _Undo.EndUndoGroup(false);
                    return;
                }
            }
            finally
            {
                dnc.Dispose();
            }
			XmlNode chart;
			if (hl.HitContainer.Name == "Table" || hl.HitContainer.Name == "fyi:Grid")
			{
				chart = _DrawPanel.ReplaceTableMatrixOrChart(hl, dnc.ChartXml);
			}
			else
				chart = _DrawPanel.PasteTableMatrixOrChart(hl.HitContainer, dnc.ChartXml, hl.HitRelative);

			if (chart == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}
			_Undo.EndUndoGroup(true);
			ReportChanged(this, new EventArgs());
			SelectionChanged(this, new EventArgs());
			ReportItemInserted(this, new EventArgs());
			_DrawPanel.Invalidate();   

			// Now bring up the property dialog
            //List<XmlNode> ar = new List<XmlNode>();
            //ar.Add(chart);
            //_Undo.StartUndoGroup("Dialog");
            //PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.ReportItems);
            //dr = pd.ShowDialog(this);
            //if (pd.Changed || dr == DialogResult.OK)
            //{
            //    _Undo.EndUndoGroup(true);
            //    ReportChanged(this, new EventArgs());
            //    _DrawPanel.Invalidate();   
            //}
            //else
            //    _Undo.EndUndoGroup(false);

			SetFocus();
		}
	
		private void menuInsertCustomReportItem_Click(object sender, EventArgs e)
		{
			var mi = (ToolStripMenuItem)sender;
			menuInsertCustomReportItem(sender, e, (string) mi.Tag);
		}

		private void menuInsertCustomReportItem(object sender, EventArgs e, string customName)
		{
			string ri;
			ICustomReportItem cri = null;
			try
			{
				cri = RdlEngineConfig.CreateCustomReportItem(customName);

				var criXml = cri.GetCustomReportItemXml().Trim();

				if (!(criXml.StartsWith("<CustomReportItem>") && criXml.EndsWith("</CustomReportItem>")))
				{
					MessageBox.Show(
						string.Format(Strings.DesignCtl_Show_CustomReportItem, customName, criXml), Strings.DesignCtl_Show_Insert);
					return;
				}

				ri = "<ReportItems>" +
					string.Format(criXml, customName) +     // substitute the type of custom report item
					"</ReportItems>";
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(Strings.DesignCtl_Show_CustomReportItemException, ex.Message), Strings.DesignCtl_Show_Insert);
				return;
			}
			finally
			{
				if (cri != null)
					cri.Dispose();
			}
			menuInsertReportItem(sender, e, ri);
		}

        private void menuInsertGrid_Click(object sender, EventArgs e)
        {
            string ri = @"<ReportItems>"+
"<fyi:Grid xmlns:fyi=\"http://www.fyireporting.com/schemas\" Name=\"Grid1\">" +
        @"<Style>
          <BorderStyle>
            <Default>Solid</Default>
          </BorderStyle>
          <BorderColor />
          <BorderWidth />
        </Style>
        <TableColumns>
          <TableColumn>
            <Width>83.7pt</Width>
          </TableColumn>
          <TableColumn>
            <Width>92.7pt</Width>
          </TableColumn>
          <TableColumn>
            <Width>93.7pt</Width>
          </TableColumn>
        </TableColumns>
        <Header>
          <TableRows>
            <TableRow>
              <Height>12 pt</Height>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox3'>
                      <Value>Grid Column 1</Value>
                      <Style>
                        <TextAlign>Center</TextAlign>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                        <FontWeight>Bold</FontWeight>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox4'>
                      <Value>Grid Column 2</Value>
                      <Style>
                        <TextAlign>Center</TextAlign>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                        <FontWeight>Bold</FontWeight>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox9'>
                      <Value>Grid Column 3</Value>
                      <Style>
                        <TextAlign>Center</TextAlign>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                        <FontWeight>Bold</FontWeight>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
          <RepeatOnNewPage>true</RepeatOnNewPage>
        </Header>
        <Details>
          <TableRows>
            <TableRow>
              <Height>12 pt</Height>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox10'>
                      <Value>r1c1</Value>
                      <CanGrow>true</CanGrow>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox11'>
                      <Value>r1c2</Value>
                      <CanGrow>true</CanGrow>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox12'>
                      <Value>r1c3</Value>
                      <CanGrow>true</CanGrow>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
            <TableRow>
              <Height>12 pt</Height>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox13'>
                      <Value>r2c1</Value>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox14'>
                      <Value>r2c2</Value>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                        <BorderColor />
                        <BorderWidth />
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox15'>
                      <Value>r2c3</Value>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
        <Left>515.68pt</Left>
        <Top>11.34pt</Top>
        <Footer>
          <TableRows>
            <TableRow>
              <Height>.2in</Height>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox16'>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                      <Value>Footer 1</Value>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox17'>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                      <Value>
                      </Value>
                    </Textbox>
                  </ReportItems>
                </TableCell>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='Textbox18'>
                      <Style>
                        <BorderStyle>
                          <Default>Solid</Default>
                        </BorderStyle>
                      </Style>
                      <Value>
                      </Value>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Footer>
      </fyi:Grid>
                </ReportItems>";
            menuInsertReportItem(sender, e, ri);
        }

		private void menuInsertLine_Click(object sender, EventArgs e)
		{
			string ri = "<ReportItems><Line><Height>0in</Height><Width>1in</Width><Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Line></ReportItems>";
			menuInsertReportItem(sender, e, ri);
		}
	
		private void menuInsertList_Click(object sender, EventArgs e)
		{
			string ri = "<ReportItems><List><Height>1.5in</Height><Width>1.5in</Width></List></ReportItems>";

			// Lists aren't allowed in PageHeader or PageFooter
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;
			if (_DrawPanel.InPageHeaderOrFooter(hl.HitContainer))
			{
				MessageBox.Show(Strings.DesignCtl_Show_ListsInBody, Strings.DesignCtl_Show_Insert);
				return;
			}
			menuInsertReportItem(hl, ri);
		}
	
		private void menuInsertImage_Click(object sender, EventArgs e)
		{
			string ri = "<ReportItems><Image><Height>1.5in</Height><Width>1.5in</Width></Image></ReportItems>";
			menuInsertReportItem(sender, e, ri);
		}
			
		private void menuInsertMatrix_Click(object sender, EventArgs e)
		{
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;

			// Matrixs aren't allowed in PageHeader or PageFooter
			if (_DrawPanel.InPageHeaderOrFooter(hl.HitContainer))
			{
				MessageBox.Show(Strings.DesignCtl_Show_MatrixsInBody, Strings.DesignCtl_Show_Insert);
				return;
			}

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertMatrix);
			DialogNewMatrix dnm = new DialogNewMatrix(this._DrawPanel, hl.HitContainer);
            try
            {
                DialogResult dr = dnm.ShowDialog(this);
                if (dr != DialogResult.OK)
                {
                    _Undo.EndUndoGroup(false);
                    return;
                }
            }
            finally
            {
                dnm.Dispose();
            }
			XmlNode matrix;
			if (hl.HitContainer.Name == "Table" || hl.HitContainer.Name == "fyi:Grid")
				matrix = _DrawPanel.ReplaceTableMatrixOrChart(hl, dnm.MatrixXml);
			else
				matrix = _DrawPanel.PasteTableMatrixOrChart(hl.HitContainer, dnm.MatrixXml, hl.HitRelative);
			if (matrix == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}
			_Undo.EndUndoGroup(true);
			ReportChanged(this, new EventArgs());
			SelectionChanged(this, new EventArgs());
			ReportItemInserted(this, new EventArgs());
			_DrawPanel.Invalidate();   

			// Now bring up the property dialog
            //List<XmlNode> ar = new List<XmlNode>();
            //ar.Add(matrix);
            //_Undo.StartUndoGroup("Dialog");
            //PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.ReportItems);
            //dr = pd.ShowDialog(this);
            //_Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
            //if (pd.Changed || dr == DialogResult.OK)
            //{
            //    ReportChanged(this, new EventArgs());
            //    _DrawPanel.Invalidate();   
            //}
			SetFocus();
		}
			
		private void menuInsertRectangle_Click(object sender, EventArgs e)
		{
			string ri = "<ReportItems><Rectangle><Height>1.5in</Height><Width>1.5in</Width></Rectangle></ReportItems>";
			menuInsertReportItem(sender, e, ri);
		}

		private void menuInsertReportItem(object sender, EventArgs e, string reportItem)
		{
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;

			menuInsertReportItem(hl, reportItem);
		}	
		
		private void menuInsertReportItem(HitLocation hl, string reportItem)
		{
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_Insert);
            try
            {
                if (hl.HitContainer.Name == "Table" || hl.HitContainer.Name == "fyi:Grid")
                {
                    _DrawPanel.ReplaceReportItems(hl, reportItem);
                }
                else
                    _DrawPanel.PasteReportItems(hl.HitContainer, reportItem, hl.HitRelative);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.DesignCtl_ShowC_IllegalInsertSyntax + Environment.NewLine + 
                    reportItem + Environment.NewLine + ex.Message);
                return;
            }
            finally
            {
                _Undo.EndUndoGroup(true);
            }
			_DrawPanel.Invalidate();
			ReportChanged(this, new EventArgs());
			SelectionChanged(this, new EventArgs());
			ReportItemInserted(this, new EventArgs());

            if (reportItem.Contains("<Image") || reportItem.Contains("<Subreport"))
	    		menuProperties_Click();     // bring up report dialog for images and subreports
		}
			
		private void menuInsertSubreport_Click(object sender, EventArgs e)
		{
			// Subreports aren't allowed in PageHeader or PageFooter
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;
			if (_DrawPanel.InPageHeaderOrFooter(hl.HitContainer))
			{
				MessageBox.Show(Strings.DesignCtl_Show_SubreportsInBody, Strings.DesignCtl_Show_Insert);
				return;
			}

			string ri = "<ReportItems><Subreport><Height>1.5in</Height><Width>1.5in</Width></Subreport></ReportItems>";
			menuInsertReportItem(hl, ri);
		}

		private void menuInsertTable_Click(object sender, EventArgs e)
		{
			HitLocation hl = _DrawPanel.HitContainer(_MousePosition, 
				PointsX(_hScroll.Value), PointsY(_vScroll.Value));
			if (hl == null || hl.HitContainer == null)
				return;
			
			// Tables aren't allowed in PageHeader or PageFooter
			if (_DrawPanel.InPageHeaderOrFooter(hl.HitContainer))
			{
				MessageBox.Show(Strings.DesignCtl_Show_TablesInBody, Strings.DesignCtl_Show_Insert);
				return;
			}

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTable);
			DialogNewTable dnt = new DialogNewTable(this._DrawPanel, hl.HitContainer);
            try
            {
                DialogResult dr = dnt.ShowDialog(this);
                if (dr != DialogResult.OK)
                {
                    _Undo.EndUndoGroup(false);
                    return;
                }
            }
            finally
            {
                dnt.Dispose();
            }
			XmlNode table;
			if (hl.HitContainer.Name == "Table" || hl.HitContainer.Name == "fyi:Grid")
				table = _DrawPanel.ReplaceTableMatrixOrChart(hl, dnt.TableXml);
			else
				table = _DrawPanel.PasteTableMatrixOrChart(hl.HitContainer, dnt.TableXml, hl.HitRelative);
			if (table == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}
			_Undo.EndUndoGroup(true);
			ReportChanged(this, new EventArgs());
			SelectionChanged(this, new EventArgs());
			ReportItemInserted(this, new EventArgs());
			_DrawPanel.Invalidate();   

			// Now bring up the property dialog
            //List<XmlNode> ar = new List<XmlNode>();
            //ar.Add(table);
            //_Undo.StartUndoGroup("Dialog");
            //PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.ReportItems);
            //dr = pd.ShowDialog(this);
            //_Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
            //if (pd.Changed || dr == DialogResult.OK)
            //{
            //    ReportChanged(this, new EventArgs());
            //    _DrawPanel.Invalidate();   
            //}
			SetFocus();
		}
	
		private void menuInsertTextbox_Click(object sender, EventArgs e)
		{
			// Josh: Added a default ZIndex of 1 so that a "Backgroud" image can be applied to the form.
            string ri = "<ReportItems><Textbox><Height>12pt</Height><Width>1in</Width><Value>Text</Value><ZIndex>1</ZIndex></Textbox></ReportItems>"; 
			menuInsertReportItem(sender, e, ri);
		}

		private void menuOpenSubreport_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedList.Count == 0)
				return;

			XmlNode sr = _DrawPanel.SelectedList[0];
			if (sr.Name != "Subreport")
				return;

			string name = _DrawPanel.GetElementValue(sr, "ReportName", "");
			if (name == null || name.Length == 0)
				return;
			
			if (OpenSubreport != null)
				OpenSubreport(this, new SubReportEventArgs(name));
		}

		private void menuSelectAll_Click(object sender, EventArgs e)
		{
			doSelectAll();
		}

		private void doSelectAll()
		{
			IEnumerable list = _DrawPanel.GetReportItems(null);		// get all the report items
			if (list == null)
				return;
			_DrawPanel.ClearSelected();

			foreach (XmlNode riNode in list)
			{
				if (riNode.Name == "Table" || riNode.Name == "fyi:Grid" || riNode.Name == "List" || riNode.Name == "Rectangle")
					continue;	// we'll just select all the sub objects in these containers
				
				_DrawPanel.AddSelection(riNode);
				SelectionChanged(this, new EventArgs());
			}
			_DrawPanel.Invalidate();
			return;
		}

		internal void menuProperties_Click(object sender, EventArgs e)
		{
			menuProperties_Click();
		}
		
		internal void menuProperties_Click()
		{
			if (_DrawPanel.SelectedCount > 0)
			{
				DoPropertyDialog(PropertyTypeEnum.ReportItems);
			}
			else
			{	// Put up the Report Property sheets
				DoPropertyDialog(PropertyTypeEnum.Report);
			}
			SelectionChanged(this, new EventArgs());
		}

		private void menuChartDeleteGrouping_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DeleteGrouping);
			bool bSuccess=false;
			if (_DrawPanel.DeleteChartGrouping(cNode, gname))
			{
				bSuccess = true;
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			_Undo.EndUndoGroup(bSuccess);
		}

		private void menuChartEditGrouping_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];

			XmlNode group = _DrawPanel.GetChartGrouping(cNode, gname);

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(group.ParentNode);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DialogGrouping);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                _Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuChartInsertCategoryGrouping_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertCategoryGrouping);

			XmlNode cNode = _DrawPanel.SelectedList[0];
			XmlNode colGroup = _DrawPanel.InsertChartCategoryGrouping(cNode);
			if (colGroup == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}
			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(colGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    _Undo.EndUndoGroup(true);
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
                else
                {
                    _DrawPanel.DeleteChartGrouping(colGroup);
                    _Undo.EndUndoGroup(false);
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuChartInsertSeriesGrouping_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertSeriesGrouping);
			XmlNode cNode = _DrawPanel.SelectedList[0];
			XmlNode colGroup = _DrawPanel.InsertChartSeriesGrouping(cNode);
			if (colGroup == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}
			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(colGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    _Undo.EndUndoGroup(true);
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
                else
                {
                    _DrawPanel.DeleteChartGrouping(colGroup);
                    _Undo.EndUndoGroup(false);
                }
            }
            finally
            {
                pd.Dispose();
            }
		}
				
		private void menuPropertiesLegend_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.ChartLegend);
		}
				
		private void menuPropertiesCategoryAxis_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.CategoryAxis);
		}
				
		private void menuPropertiesValueAxis_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.ValueAxis);
		}
				
		private void menuPropertiesCategoryAxisTitle_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.CategoryAxisTitle);
		}
				
		private void menuPropertiesValueAxisTitle_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.ValueAxisTitle);
		}
		// 20022008 AJM GJL - Second Y axis support
        private void menuPropertiesValueAxis2Title_Click(object sender, EventArgs e)
        {
            DoPropertyDialog(PropertyTypeEnum.ValueAxis2Title);
        }
				
		private void menuPropertiesChartTitle_Click(object sender, EventArgs e)
		{
			DoPropertyDialog(PropertyTypeEnum.ChartTitle);
		}

		private void menuMatrixProperties_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode riNode = _DrawPanel.SelectedList[0];
			XmlNode table = _DrawPanel.GetMatrixFromReportItem(riNode);
			if (table == null)
				return;

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(table);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixDialog);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.ReportItems);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                _Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuMatrixDelete_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixDelete);
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteMatrix(cNode))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuMatrixDeleteGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixDeleteGroup);

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteMatrixGroup(cNode, gname))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuMatrixEditGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];

			XmlNode group = _DrawPanel.GetMatrixGroup(cNode, gname);

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(group.ParentNode);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixEdit);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                _Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuMatrixInsertColumnGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			XmlNode colGroup = _DrawPanel.InsertMatrixColumnGroup(cNode);
			if (colGroup == null)
				return;

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(colGroup);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixInsertColumnGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    _Undo.EndUndoGroup(true);
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
                else
                {
                    _DrawPanel.DeleteMatrixGroup(colGroup);
                    _Undo.EndUndoGroup(false);
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuMatrixInsertRowGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode =  _DrawPanel.SelectedList[0];
			XmlNode rowGroup = _DrawPanel.InsertMatrixRowGroup(cNode);
			if (rowGroup == null)
				return;

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(rowGroup);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_MatrixInsertRowGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    _Undo.EndUndoGroup(true);
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
                else
                {
                    _DrawPanel.DeleteMatrixGroup(rowGroup);
                    _Undo.EndUndoGroup(false);
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuTableDeleteColumn_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode =  _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DeleteTableColumn);

			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteTableColumn(cNode))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuTableDelete_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode =  _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DeleteTable);
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteTable(cNode))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuTableDeleteRow_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DeleteTableRow);
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteTableRow(cNode))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuTableDeleteGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DeleteTableGroup);
			_DrawPanel.ClearSelected();
			this.SelectionChanged(this, new EventArgs());
			if (_DrawPanel.DeleteTableGroup(cNode, gname))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}

		private void menuTableEditGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;

			var menu = (ToolStripMenuItem)sender;

			var gname = menu.Text;

			XmlNode cNode = _DrawPanel.SelectedList[0];

			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_DialogTableGroupEdit);
			XmlNode tblGroup = _DrawPanel.GetTableGroup(cNode, gname);

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(tblGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                _Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuTableInsertColumnBefore_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTableColumn);
			if (_DrawPanel.InsertTableColumn(cNode, true))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}
				
		private void menuTableInsertColumnAfter_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTableColumn);
			if (_DrawPanel.InsertTableColumn(cNode, false))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}
				
		private void menuTableInsertGroup_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTableGroup);
			XmlNode tblGroup = _DrawPanel.InsertTableGroup(cNode);
			if (tblGroup == null)
			{
				_Undo.EndUndoGroup(false);
				return;
			}

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(tblGroup);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.Grouping);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    _Undo.EndUndoGroup(true);
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
                else
                {
                    _DrawPanel.DeleteTableGroup(tblGroup);
                    _Undo.EndUndoGroup(false);
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void menuTableInsertRowBefore_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode =  _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTableRow);

			if (_DrawPanel.InsertTableRow(cNode, true))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}
				
		private void menuTableInsertRowAfter_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode cNode = _DrawPanel.SelectedList[0];
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_InsertTableRow);
			if (_DrawPanel.InsertTableRow(cNode, false))
			{
				_Undo.EndUndoGroup(true);
				ReportChanged(this, new EventArgs());
				_DrawPanel.Invalidate();   
			}
			else
				_Undo.EndUndoGroup(false);
		}
				
		private void menuTableProperties_Click(object sender, EventArgs e)
		{
			if (_DrawPanel.SelectedCount != 1)
				return;
			XmlNode riNode = _DrawPanel.SelectedList[0];
			XmlNode table = _DrawPanel.GetTableFromReportItem(riNode);
			if (table == null)
				return;
			XmlNode tc = _DrawPanel.GetTableColumn(riNode);
			XmlNode tr = _DrawPanel.GetTableRow(riNode);

			List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
			ar.Add(table);
			_Undo.StartUndoGroup(Strings.DesignCtl_Undo_TableDialog);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, ar, PropertyTypeEnum.ReportItems, tc, tr);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                _Undo.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
		}

		private void DoPropertyDialog(PropertyTypeEnum type)
		{
			StartUndoGroup(Strings.DesignCtl_Undo_Dialog);
			PropertyDialog pd = new PropertyDialog(_DrawPanel, _DrawPanel.SelectedList, type);
            try
            {
                DialogResult dr = pd.ShowDialog(this);
                this.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    ReportChanged(this, new EventArgs());
                    _DrawPanel.Invalidate();
                }
            }
            finally
            {
                pd.Dispose();
            }
			SetFocus();
		}

		internal void SetFocus()
		{
			this._DrawPanel.Focus();
		}
				
		private void menuContext_Popup(object sender, EventArgs e)
		{
			var bEnable = _DrawPanel.SelectedCount > 0;

			MenuDefaultCopy.Enabled = bEnable;
			MenuDefaultDelete.Enabled = bEnable;

			var al = new List<XmlNode>();

			var iData = Clipboard.GetDataObject();

			if (iData == null)
				bEnable = false;
			else if (iData.GetDataPresent(al.GetType()))
				bEnable = true;
			else if (iData.GetDataPresent(DataFormats.Text)) 
				bEnable = true;
			else if (iData.GetDataPresent(DataFormats.Bitmap)) 
				bEnable = true;
			else
				bEnable = false;

			MenuDefaultPaste.Enabled = bEnable;
			MenuChartPaste.Enabled = bEnable;
			MenuGridPaste.Enabled = bEnable;
			MenuMatrixPaste.Enabled = bEnable;
			MenuSubreportPaste.Enabled = bEnable;
			MenuTablePaste.Enabled = bEnable;
		}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                _buffer?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

	public class SubReportEventArgs : EventArgs
	{
		string _name;			// name of subreport user has requested be opened
		public SubReportEventArgs(string name) : base()
		{
			_name = name;
		}

		public string SubReportName
		{
			get {return _name;}
		}
	}

    public class HeightEventArgs : EventArgs
    {
        XmlNode _node;          // current node
        string _height;			// current height
        public HeightEventArgs(XmlNode node, string height)
            : base()
        {
            _node = node;
            _height = height;
        }

        public XmlNode Node
        {
            get { return _node; }
        }
        public string Height
        {
            get { return _height; }
        }
    }
    /// <summary>
    /// Nuova classe embbedded nel controllo per passare gli eventi del mouse e i fattori di scala correnti
    /// </summary>

    public class MouseEventArgsE : EventArgs
    {
        private MouseButtons button;
        private int clicks;
        private int x;
        private int y;
        private int delta;
        private Point location;
        public MouseButtons Button => button;
        public int Clicks => clicks;
        public int X => x;
        public int Y => y;
        public int Delta => delta;
        public Point Location => location;

        /// <summary>
        /// Costruttore
        /// </summary>
        /// <param name="B"></param>
        /// <param name="ScaleX"></param>
        /// <param name="ScaleY"></param>
        public MouseEventArgsE(MouseEventArgs B, float ScaleX, float ScaleY)
        {
            this.button = B.Button;
            this.clicks = B.Clicks;
            this.x = (int)(B.X / ScaleX);
            this.y = (int)(B.Y / ScaleY);
            this.location = new Point(x, y);
            // Majorsilence.Forms.MouseEventArgs.Delta is a Point (X/Y wheel deltas), unlike
            // WinForms's int Delta (vertical-only); take the vertical component.
            this.delta = B.Delta.Y;
        }
    }
}
