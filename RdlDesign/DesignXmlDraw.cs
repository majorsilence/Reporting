using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.Rdl.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.ComponentModel;

namespace Majorsilence.Reporting.RdlDesign
{

	/// <summary>
	/// Control for providing a designer image of RDL.   Works directly off the RDL XML.
	/// </summary>
	public class DesignXmlDraw: UserControl
	{
		static internal readonly float POINTSIZED = 72.27f;
		static internal readonly decimal POINTSIZEM = 72.27m;
        readonly Color BANDBORDERCOLOR = Color.DimGray; //Josh: added for Band Border
        readonly BorderStyleEnum BANDBORDERSTYLE = BorderStyleEnum.Solid; //Josh: Band Border Style
        const float BANDBORDERWIDTH = 1f; //Josh: Band border width
        readonly Color AREABACKCOLOR = Color.LightGray; //Josh: added for area background color (behind page)
		readonly Color OUTWORKAREACOLOR = Color.Azure; //out work area background color (on page, but in margin)
		readonly Color OUTITEMCOLOR = Color.Salmon; //color of out work area parts of item 

		const float RADIUS = 4.0f;  // Increased from 2.5f for easier manipulation
        readonly Color BANDCOLOR = Color.LightGray;
		const int BANDHEIGHT = 12;              // height of band (e.g. body, pageheader, pagefooter) in pts
		const float LEFTGAP = 0f; // keep a gap on the left size of the screen
        // Various page measurements that we keep
        public float ReportVisualSize;
        public float ReportVisualSizeBase;
        // rWidth is now ReportVisualSizeBase i.e. the report real w size
        public float pHeight, pWidth;
		public float lMargin, rMargin, tMargin, bMargin;
		XmlNode bodyNode;
		XmlNode phNode;
		XmlNode pfNode;

		private XmlDocument rDoc;			// the reporting XML document
        private List<XmlNode> _SelectedReportItems = new List<XmlNode>();
		private ReportNames _ReportNames;	// holds the names of the report items
		float DpiX;
		float DpiY;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Folder { get; set; }

		// During drawing these are set
		Graphics g;
		float _vScroll;
		float _hScroll;
		RectangleF _clip;				

		// During hit testing
		PointF _HitPoint;
		RectangleF _HitRect;	

		// Durning GetRectangle 
		XmlNode _RectNode;
		RectangleF _GetRect;

        bool _ShowReportItemOutline=false;


		// For Scaling and AlignMent grid display
        public float SCALAX = 1;
        public float SCALAY = 1;
        public bool EnableDrawGriglia = false;
        public Size SizeGridPt = new Size(4, 4);
        public Size SizeGridTwips = new Size(8, 8);

        internal DesignXmlDraw():base()
		{
            this.DoubleBuffered = true;
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
			this.SetStyle(ControlStyles.DoubleBuffer | 
				ControlStyles.UserPaint | 
				ControlStyles.AllPaintingInWmPaint,
				true);
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

        internal Color SepColor
        {
            get { return BANDCOLOR; }
        }

        internal float SepHeight
        {
            get { return BANDHEIGHT; }
        }

        internal float PageHeaderHeight
        {
            get 
            {
                if (rDoc == null)
                    return 0;
                XmlNode r = rDoc.LastChild;
                XmlNode ph = this.GetNamedChildNode(r, "PageHeader");
                if (ph == null)
                    return 0;
                XmlNode h = this.GetNamedChildNode(ph, "Height");
                if (h == null)
                    return 0;
                float height = GetSize(h.InnerText);
                return height;
            }
        }

        internal float PageFooterHeight
        {
            get
            {
                if (rDoc == null)
                    return 0;
                XmlNode r = rDoc.LastChild;
                XmlNode ph = this.GetNamedChildNode(r, "PageFooter");
                if (ph == null)
                    return 0;
                XmlNode h = this.GetNamedChildNode(ph, "Height");
                if (h == null)
                    return 0;
                float height = GetSize(h.InnerText);
                return height;
            }
        }

        internal float BodyHeight
        {
            get
            {
                if (rDoc == null)
                    return 0;
                XmlNode r = rDoc.LastChild;
                XmlNode ph = this.GetNamedChildNode(r, "Body");
                if (ph == null)
                    return 0;
                XmlNode h = this.GetNamedChildNode(ph, "Height");
                if (h == null)
                    return 0;
                float height = GetSize(h.InnerText);
                return height;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal bool ShowReportItemOutline
        {
            get { return _ShowReportItemOutline; }
            set 
            {
                if (value != _ShowReportItemOutline)
                    this.Invalidate();

                _ShowReportItemOutline = value; 
            }
        }

         [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal ReportNames ReportNames
		{
			get 
			{
				if (_ReportNames == null && ReportDocument != null)
					_ReportNames = new ReportNames(rDoc);	// rebuild report names on demand
				
				return _ReportNames;
			}
			set {_ReportNames = value;}
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal XmlDocument ReportDocument
		{
			get {return rDoc;}
			set 
			{ 
				rDoc = value;
				if (rDoc != null)
				{
					ReportNames = null;		// this needs to get rebuilt
					ProcessReport(rDoc.LastChild);
					this.ClearSelected();
				}
				else
				{
					this._SelectedReportItems.Clear();
					ReportNames = null;
					this.ClearSelected();
				}
			}
		}

		internal XmlNode Body
		{
			get { return bodyNode; }
		}

		internal RectangleF GetRectangle(XmlNode xNode)
		{
			_RectNode = xNode;			// this is the rectangle we're trying to find;

			float yLoc=0;
			_GetRect = RectangleF.Empty;

			try
			{
				yLoc += GetRectReportPrimaryRegions(phNode, LEFTGAP, yLoc);
				yLoc += GetRectReportPrimaryRegions(bodyNode, LEFTGAP, yLoc);
				yLoc += GetRectReportPrimaryRegions(pfNode, LEFTGAP, yLoc);
			}
			catch (Exception e)
			{
				// this is the normal exit; we throw exception when node is found
				if (e.Message == "found it!")
					return _GetRect;
			}
			return RectangleF.Empty;
		}

		private float GetRectReportPrimaryRegions(XmlNode xNode, float xLoc, float yLoc)
		{
			if (xNode == null)
				return yLoc;

			XmlNode items=null;
			float height=0;
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Height":
						height = GetSize(xNodeLoop.InnerText);
						break;
					case "ReportItems":
						items = xNodeLoop;
						break;
				}
			}

			RectangleF b = new RectangleF(xLoc, yLoc, int.MaxValue, height);
			
			GetRectReportItems(items, b);			// now draw the report items

			return height+BANDHEIGHT;
		}

		private void GetRectReportItems(XmlNode xNode, RectangleF r)
		{
			if (xNode == null)
				return;

			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				RectangleF rir=RectangleF.Empty;
				switch (xNodeLoop.Name)
				{
					case "Textbox":
					case "Image":
					case "Subreport":
					case "Chart":
					case "Line":
                    case "CustomReportItem":
						rir = GetRectRI(xNodeLoop, r);
						break;
					case "Table":
                    case "fyi:Grid":
						rir = GetRectTable(xNodeLoop, r);
						break;
					case "Rectangle":
					case "List":
						rir = GetRectListRectangle(xNodeLoop, r);
						break;
					case "Matrix":
						rir = GetRectMatrix(xNodeLoop, r);
						break;
				}
				if (xNodeLoop == _RectNode)
				{
					_GetRect = rir;
					throw new RdlException("found it!");
				}
			}
		}

		private RectangleF GetRectRI(XmlNode xNode, RectangleF r)
		{
			RectangleF ir = GetReportItemRect(xNode, r);
			return ir;
		}

		private RectangleF GetRectListRectangle(XmlNode xNode, RectangleF r)
		{
			RectangleF listR = GetReportItemRect(xNode, r);

			XmlNode items = this.GetNamedChildNode(xNode, "ReportItems");

			if (items != null)
				GetRectReportItems(items, listR);

			return listR;
		}

		private RectangleF GetRectMatrix(XmlNode xNode, RectangleF r)
		{
			RectangleF mr = GetReportItemRect(xNode, r);		// get the matrix rectangle
			return mr;
		}






		private RectangleF GetRectTable(XmlNode xNode, RectangleF r)
		{
			RectangleF tr = GetReportItemRect(xNode, r);		// get the table rectangle

			// For Table width is really defined by the table columns
			float[] colWidths;
			colWidths = GetTableColumnWidths(GetNamedChildNode(xNode, "TableColumns"));
			// calc the total width
			float w=0;
			foreach (float cw in colWidths)
				w += cw;
			tr.Width = w;

			// For Table height is really defined the sum of the RowHeights
			List<XmlNode> trs = GetTableRows(xNode);
			tr.Height = GetTableRowsHeight(trs);

			// Loop thru the TableRows and the columns in each of them to get at the
			//  individual cell
			float yPos = tr.Y;
			foreach (XmlNode trow in trs)
			{
				XmlNode tcells=GetNamedChildNode(trow, "TableCells");

				float h = GetSize(GetNamedChildNode(trow, "Height").InnerText);

				float xPos = tr.X;
				int col=0;
				foreach (XmlNode tcell in tcells)
				{
					if (tcell.Name != "TableCell")
						continue;
					// Calculate width based on cell span
					float width = 0;
					int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));
					for (int i = 0; i < colSpan && col+i<colWidths.Length; i++)
					{
						width += colWidths[col+i];
					}

					RectangleF cellR = new RectangleF(xPos, yPos, width, h);
					GetRectReportItems(GetNamedChildNode(tcell, "ReportItems"), cellR);
					xPos += width;
					col+=colSpan;
				}
				yPos += h;
			}
			return tr;
		}

		internal XmlNode GetReportItemContainer(XmlNode xNode)
		{
			for (XmlNode lNode = xNode.ParentNode; lNode != null; lNode = lNode.ParentNode)
			{
				switch (lNode.Name)
				{
					case "List":
					case "Body":
					case "PageHeader":
					case "PageFooter":
					case "Rectangle":
					case "Table":
                    case "fyi:Grid":
                    case "Matrix":
						return lNode;
				}
			}

			return null;
		}

		internal XmlNode GetReportItemDataRegionContainer(XmlNode xNode)
		{
			for (XmlNode cont = GetReportItemContainer(xNode); 
				cont != null; 
				cont = GetReportItemContainer(cont))
			{
				if (IsDataRegion(cont))
					return cont;
			}
			return null;
		}

		internal bool IsDataRegion(XmlNode node)
		{
			if (node == null)
				return false;

			switch (node.Name)
			{
				case "List":
				case "Table":
                case "Matrix":
				case "Chart":
					return true;
				default:
					return false;
			}
		}

		internal string[] GetReportItemDataRegionFields(XmlNode xNode, bool bExpression)
		{
			XmlNode cNode = GetReportItemContainer(xNode);
			if (cNode == null ||
				cNode.Name == "Body" ||
				cNode.Name == "PageHeader" ||
				cNode.Name == "PageFooter")
				return null;

			string dsname = GetDataSetNameValue(cNode);
			string[] f = null;
			if (dsname != null)	// found it
				f = GetFields(dsname, bExpression);

			return f;
		}

		internal void ApplyStyleToSelected(string name, string v)
		{
			foreach (XmlNode n in _SelectedReportItems)
			{
				XmlNode sNode = this.GetCreateNamedChildNode(n, "Style");
				this.SetElement(sNode, name, v);
			}

			this.Invalidate();
		}
 
		/// <summary>
		/// Returns a collection of the DataSetNames
		/// </summary>
		internal object[] DataSetNames
		{
			get {return ReportNames.DataSetNames;}
		}
 
		/// <summary>
		/// Returns a collection of the DataSourceNames
		/// </summary>
		internal object[] DataSourceNames
		{
			get {return ReportNames.DataSourceNames;}
		}

		internal XmlNode DataSourceName(string dsn)
		{
			return ReportNames.DataSourceName(dsn);
		}

		/// <summary>
		/// Returns a collection of the Groupings
		/// </summary>
		internal object[] GroupingNames
		{
			get {return ReportNames.GroupingNames;}
		}

		internal string[] GetFields(string dataSetName, bool asExpression)
		{
			return ReportNames.GetFields(dataSetName, asExpression);
		}

        internal string GetReportParameterDefaultValue(string parameterExpression)
        {
            return ReportNames.GetReportParameterDefaultValue(parameterExpression);
        }

        internal string[] GetReportParameters(bool asExpression)
		{
			return ReportNames.GetReportParameters(asExpression);
		}

        /// <summary>
        /// EBN 30/03/2014
        /// </summary>
        /// <param name="asExpression"></param>
        /// <returns></returns>
        internal string[] GetReportModules(bool asExpression)
        {
            return ReportNames.GetReportModules(asExpression);
        }

        /// <summary>
        /// EBN 30/03/2014
        /// </summary>
        /// <param name="asExpression"></param>
        /// <returns></returns>
        internal string[] GetReportClasses(bool asExpression)
        {
            return ReportNames.GetReportClasses(asExpression);
        }


		internal PointF SelectionPosition(XmlNode xNode)
		{
			RectangleF r = this.GetReportItemRect(xNode);
			return new PointF(r.X, r.Y);
		}

		internal SizeF SelectionSize(XmlNode xNode)
		{
			SizeF rs = new SizeF(float.MinValue, float.MinValue);
			if (this.InTable(xNode))
			{
				XmlNode tcol = this.GetTableColumn(xNode);
				XmlNode tcell = this.GetTableCell(xNode);
				if (tcol != null && tcell != null)
				{
					int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));
					float width=0;
					while (colSpan > 0 && tcol != null)
					{
						XmlNode w = this.GetNamedChildNode(tcol, "Width");
						if (w != null)
							width += GetSize(w.InnerText);
						colSpan--;
						tcol = tcol.NextSibling;
					}
					if (width > 0)					
						rs.Width = width;
				}
				XmlNode tr = this.GetTableRow(xNode);
				if (tr != null)
				{
					XmlNode h = this.GetNamedChildNode(tr, "Height");
					if (h != null)
						rs.Height = GetSize(h.InnerText);
				}
			}
			else
			{
				RectangleF r = this.GetReportItemRect(xNode);
				rs.Width = r.Width;
				rs.Height = r.Height;
			}

			// we want both values or neither
			if (rs.Width == float.MinValue || rs.Height == float.MinValue)
				rs.Width = rs.Height = float.MinValue;
			return rs;
		}

		/// <summary>
		/// Adds the node to the selection unless the node is already there in which case it removes it
		/// </summary>
		/// <param name="node"></param>
		internal void AddRemoveSelection(XmlNode node)
		{
			if (_SelectedReportItems.IndexOf(node) >= 0)	
				_SelectedReportItems.Remove(node);			// remove from list if already in list
			else
				_SelectedReportItems.Add(node);				// add to list otherwise
			this.Invalidate();
		}

		internal void AddSelection(XmlNode node)
		{
			if (_SelectedReportItems.IndexOf(node) < 0)	
			{
				_SelectedReportItems.Add(node);				// add to list otherwise
			}
		}

		internal void ClearSelected()
		{
			if (_SelectedReportItems.Count > 0)
			{
				_SelectedReportItems.Clear();
				this.Invalidate();
			}
		}

		internal void DeleteSelected()
		{
			if (_SelectedReportItems.Count <= 0)
				return;

			foreach (XmlNode n in _SelectedReportItems)
			{
				DeleteReportItem(n);
			}

			_SelectedReportItems.Clear();
			this.Invalidate();
		}

		internal bool IsNodeSelected(XmlNode node)
		{
			bool bSelected=false;
			foreach (XmlNode lNode in this._SelectedReportItems)
			{
				if (lNode == node)
				{
					bSelected=true;
					break;
				}
			}
			return bSelected;
		}

		internal void RemoveSelection(XmlNode node)
		{
			_SelectedReportItems.Remove(node);
			this.Invalidate();
		}

		internal bool SelectNext(bool bReverse)
		{
			XmlNode sNode;
			if (_SelectedReportItems.Count > 0)
				sNode = _SelectedReportItems[0];
			else
				sNode = null;

			XmlNode nNode = bReverse? ReportNames.FindPrior(sNode): ReportNames.FindNext(sNode);
			if (nNode == null)
				return false;
			this.ClearSelected();
			this.AddSelection(nNode);
			return true;
		}

		static internal int CountChildren(XmlNode node, params string[] names)
		{
			return CountChildren(node, names, 0);
		}

		static private int CountChildren(XmlNode node, string[] names, int index)
		{
			int count = 0;
			foreach (XmlNode c in node.ChildNodes)
			{
				if (c.Name != names[index])
					continue;
				if (names.Length-1 == index)
					count++;
				else
					count += CountChildren(c, names, index+1);
			}
			return count;
		}

        internal XmlNode FindCreateNextInHierarchy(XmlNode xNode, params string[] names)
        {
            XmlNode rNode = xNode;
            foreach (string name in names)
            {
                XmlNode node = null;
                foreach (XmlNode cNode in rNode.ChildNodes)
                {
                    if (cNode.NodeType == XmlNodeType.Element &&
                        cNode.Name == name)
                    {
                        node = cNode;
                        break;
                    }
                }
                if (node == null)
                    node = this.CreateElement(rNode, name, null);
                
                rNode = node;
            }
            return rNode;
        }

		static internal XmlNode FindNextInHierarchy(XmlNode xNode, params string [] names)
		{
			XmlNode rNode=xNode;
			foreach (string name in names)
			{
				XmlNode node = null;
				foreach (XmlNode cNode in rNode.ChildNodes)
				{
					if (cNode.NodeType == XmlNodeType.Element && 
						cNode.Name == name)
					{
						node = cNode;
						break;
					}
				}
				rNode = node;
                if (rNode == null)
                    break;
			}
			return rNode;
		}

		internal bool AllowGroupOperationOnSelected
		{
			get
			{
				if (_SelectedReportItems.Count <= 1)
					return false;
				foreach (XmlNode xNode in SelectedList)
				{
					if (InMatrix(xNode) || InTable(xNode))
						return false;
				}
				return true;
			}
		}

		internal int SelectedCount
		{
			get {return _SelectedReportItems.Count;}
		}
        /// <summary>
        /// Changes the current selection;
        /// </summary>
        /// <param name="node">Report item node to select</param>
        /// <param name="bGroup">When true check if node is already selected</param>
		internal void SetSelection(XmlNode node, bool bGroup)
		{
			if (bGroup && _SelectedReportItems.IndexOf(node) >= 0)	// already part of selection
				return;
			_SelectedReportItems.Clear();					// clear out all selected
			_SelectedReportItems.Add(node);					//   and add in the new one
			this.Invalidate();
		}

		internal List<XmlNode> SelectedList
		{
			get {return _SelectedReportItems;}
		}

		// Adjust for scale
		internal float VerticalMax
		{
			get 
			{
				float TotaleH= GetSize(bodyNode, "Height") +
						GetSize(phNode, "Height") +
						GetSize(pfNode, "Height") +
					    BANDHEIGHT * 3 +
						3 * 10;     // plus about 3 lines
                return TotaleH * SCALAY;
            }
		}

		internal float HorizontalMax
		{
			get 
			{
                ProcessReport(rDoc.LastChild);                      // make sure pWidth and ReportVisualSize are up to date
				float hm = Math.Max(pWidth, ReportVisualSize);
				return Math.Max(hm, RightMost(rDoc.LastChild)+90);  // 90: just to give a little extra room on right
			}
		}
		/// <summary>
		/// Find the Right most (largest x) position of a report item
		/// </summary>
		/// <param name="xNode">Should be the "Report" node</param>
		/// <returns>x + width of rightmost object</returns>
		private float RightMost(XmlNode xNode)
		{
			float rm=0;			// current rightmost position

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Body":
					case "PageHeader":
					case "PageFooter":
						rm = Math.Max(rm, RightMostRI(GetNamedChildNode(xNodeLoop, "ReportItems")));
						break;
				}
			}
			return rm;
		}

		private float RightMostRI(XmlNode xNode)
		{
			if (xNode == null)
				return 0;

			float rm = 0;

			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				RectangleF r = GetReportItemRect(xNodeLoop);		// get the ReportItem rectangle

				switch (xNodeLoop.Name)
				{
					case "Table":
                    case "fyi:Grid":
                        // Table width is really defined by the table columns
						float[] colWidths;
						colWidths = GetTableColumnWidths(GetNamedChildNode(xNodeLoop, "TableColumns"));
						// calc the total width
						float w=0;
						foreach (float cw in colWidths)
							w += cw;
						rm = Math.Max(rm, r.Left + w);
						break;
					case "Matrix":
						MatrixView matrix = new MatrixView(this, xNodeLoop);
						rm = Math.Max(rm, r.Left + matrix.Width);
						break;
					default:
						rm = Math.Max(rm, r.Right);
						break;
				}
			}
			return rm;
		}

		/// <summary>
		/// Delete the matrix that contains the passed node
		/// </summary>
		/// <param name="node"></param>
		/// <returns>true if table is deleted</returns>
		internal bool DeleteMatrix(XmlNode node)
		{
			// Get the table
			XmlNode matrix = this.GetMatrixFromReportItem(node);
			if (matrix == null)
				return false;

			return DeleteReportItem(matrix);
		}

		/// <summary>
		/// Deletes the specified ReportItem node but ensures that the report remains syntactically correct.
		/// e.g. TableCells must contain a ReportItems which must contain a ReportItem
		/// e.g. The parent ReportsItems node must be deleted if this is the only node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>true when deleted; false when node is changed into Textbox with value = ""</returns>
		internal bool DeleteReportItem(XmlNode node)
		{
			bool rc = true;
			bool bRebuildNames=false;
			if (node.Name == "Table" ||
                node.Name == "fyi:Grid" ||
				node.Name == "List" ||
				node.Name == "Matrix" ||
				node.Name == "Rectangle")
				bRebuildNames = true;
			XmlNode reportItemsNode = node.ParentNode;
			if (reportItemsNode == null)
				return false;			// can't delete this; it is already deleted
			XmlNode pReportItems = reportItemsNode.ParentNode;
			if (pReportItems.Name == "TableCell")
			{	// Report item is part of a table; just convert it to an Textbox with no text
				rc = false;
				XmlNode styleNode = GetNamedChildNode(node, "Style");	// want to retain style if possible
				if (styleNode != null)
					styleNode = styleNode.CloneNode(true);
				reportItemsNode.RemoveChild(node);
				ReportNames.RemoveName(node);
				XmlElement tbnode = this.CreateElement(reportItemsNode,"Textbox", null);
				ReportNames.GenerateName(tbnode);
				XmlElement vnode = this.CreateElement(tbnode, "Value", "");
				if (styleNode != null)
					tbnode.AppendChild(styleNode);
			}
			else
			{
				reportItemsNode.RemoveChild(node);
				ReportNames.RemoveName(node);
				if (!reportItemsNode.HasChildNodes) 
				{	// ReportItems now has no nodes and needs to be removed
					pReportItems.RemoveChild(reportItemsNode);	
				}
			}
			if (bRebuildNames)
				ReportNames = null;			// this will force a rebuild when next needed

			return rc;
		}

		/// <summary>
		/// Delete the table that contains the passed node
		/// </summary>
		/// <param name="node"></param>
		/// <returns>true if table is deleted</returns>
		internal bool DeleteTable(XmlNode node)
		{
			// Get the table
			XmlNode table = this.GetTableFromReportItem(node);
			if (table == null)
				return false;

			return DeleteReportItem(table);
		}

		/// <summary>
		/// Draw the report definition
		/// </summary>
		/// <param name="g"></param>
		/// <param name="hScroll">Horizontal scroll position</param>
		/// <param name="vScroll">Vertical scroll position</param>
		/// <param name="clipRectangle"></param>
		internal async Task Draw(Graphics ag, float hScroll, float vScroll, System.Drawing.Rectangle clipRectangle)
		{
			g = ag;
			
			_hScroll = hScroll;
			_vScroll = vScroll;

			g.PageUnit = GraphicsUnit.Point;
            // Now use scale value
            g.ScaleTransform(SCALAX, SCALAY);

			_clip = new RectangleF(PointsX(clipRectangle.X) + _hScroll, 
				PointsY(clipRectangle.Y) + _vScroll, 
				PointsX(clipRectangle.Width), 
				PointsY(clipRectangle.Height));

			XmlNode xNode = rDoc.LastChild;
			if (xNode == null || xNode.Name != "Report")
			{
				throw new Exception(Strings.DesignXmlDraw_Error_NoReport);
			}

			ProcessReport(xNode);
            
			// Render the report
            DrawMargins(_clip); 

			float yLoc=0;
			
			yLoc += await DrawReportPrimaryRegions(phNode, LEFTGAP, yLoc, Strings.DesignXmlDraw_PageHeaderRegion_Title);
			yLoc += await DrawReportPrimaryRegions(bodyNode, LEFTGAP, yLoc, Strings.DesignXmlDraw_BodyRegion_Title);
			yLoc += await DrawReportPrimaryRegions(pfNode, LEFTGAP, yLoc, Strings.DesignXmlDraw_PageFooterRegion_Title);

		}

		// Process the report
		private void ProcessReport(XmlNode xNode)
		{
			bodyNode=null;
			phNode=null;
			pfNode=null;

			ReportVisualSize = pHeight = pWidth = lMargin = rMargin = tMargin = bMargin = 0;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Body":
						bodyNode = xNodeLoop;
						break;
					case "PageHeader":
						phNode = xNodeLoop;
						break;
					case "PageFooter":
						pfNode = xNodeLoop;
						break;
					case "Width":
                        ReportVisualSizeBase = GetSize(xNodeLoop.InnerText);
                        ReportVisualSize = ReportVisualSizeBase;
						break;
					case "PageHeight":
						pHeight = GetSize(xNodeLoop.InnerText);
						break;
					case "PageWidth":
						pWidth = GetSize(xNodeLoop.InnerText);
						break;
					case "LeftMargin":
						lMargin = GetSize(xNodeLoop.InnerText);
						break;
					case "RightMargin":
						rMargin = GetSize(xNodeLoop.InnerText);
						break;
					case "TopMargin":
						tMargin = GetSize(xNodeLoop.InnerText);
						break;
					case "BottomMargin":
						bMargin = GetSize(xNodeLoop.InnerText);
						break;
				}
			}

			// Set the default sizes (if not specified)
			if (pWidth == 0)
				pWidth = GetSize("8.5in");
			if (pHeight == 0)
				pHeight = GetSize("11in");
			if (ReportVisualSize == 0)
				ReportVisualSize = pWidth;

			if (phNode == null)
				phNode = CreatePrimaryRegion("PageHeader");
			if (pfNode == null)
				phNode = CreatePrimaryRegion("PageFooter");
			if (bodyNode == null)
				bodyNode = CreatePrimaryRegion("Body");

			return;
		}

		private XmlNode CreatePrimaryRegion(string name)
		{
			// Create a primary region: e.g. Page Header, Body, Page Footer
			XmlNode xNode = rDoc.CreateElement(name);
			
			// Add in the height element
			XmlNode hNode = rDoc.CreateElement("Height");
			hNode.InnerText = "0pt";
			xNode.AppendChild(hNode);

			// Now link it under the Report element
			XmlNode rNode = rDoc.LastChild;	
			rNode.AppendChild(xNode);
			return xNode;
		}

		private async Task<float> DrawReportPrimaryRegions(XmlNode xNode, float xLoc, float yLoc, string title)
		{
            RectangleF TempRect = new RectangleF();  // To be used on grid backgroung design

            if (xNode == null)
				return yLoc;

			XmlNode items=null;
			float CurrentSectionheight=float.MinValue;
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Height":
						CurrentSectionheight = GetSize(xNodeLoop.InnerText);
						break;
					case "ReportItems":
						items = xNodeLoop;
						break;
				}
			}
			if (CurrentSectionheight == float.MinValue)	
			{	// Shouldn't happen with correctly defined report; so create a Height element for the region
				this.CreateElement(xNode, "Height", "0pt");
				CurrentSectionheight = 0;
			}

            // Josh:
            // Added the effect of "paper" so that
            // the displayed page size would not extend
            // past the paper size selected. If a region
            // is extended past the end of the paper
            // the paper will no longer display and the
            // items placed will only be shown with an outline
            // in the "off-paper" area.
            //White "Paper" 
			StyleInfo si = new StyleInfo();

			// Entire Paper
			si.BackgroundColor = OUTWORKAREACOLOR;
			RectangleF b = new RectangleF(xLoc, yLoc + 1, /*PointsX(Width)*/(pWidth) /*+ _hScroll*/, /*height*/ ((CurrentSectionheight > TotalPageHeight /* - yLoc*/) ? TotalPageHeight/* - yLoc*/ : CurrentSectionheight));//displayHeight > 0 ? displayHeight : 0);
			DrawBackground(b, si);

			// Work area
			si.BackgroundColor = Color.White;
			b = new RectangleF(xLoc + lMargin, yLoc + 1, /*PointsX(Width)*/(pWidth - lMargin - rMargin) /*+ _hScroll*/, /*height*/ ((CurrentSectionheight > TotalPageHeight /* - yLoc*/) ? TotalPageHeight/* - yLoc*/ : CurrentSectionheight));//displayHeight > 0 ? displayHeight : 0);
            DrawBackground(b, si);

			// The alignment grid is displayed on Work area 

            // La griglia e' disegnata sulla superficie di lavoro e deve tenere conto dei valori dello scroll
            // perche' i valori dello scroll dei singoli items sono computati nelle varie routines che disegnano
            // la linea o il background
            //
            if (EnableDrawGriglia == true)
            {
                TempRect = b;
                // adjust coordinates for scrolling
                TempRect.X -= _hScroll - 1;
                TempRect.Y -= _vScroll - 1;
                // Use dot grid instead of line grid for better visibility
                DrawDotGrid(TempRect, SizeGridPt, Color.DarkGray);
            }


            //Edge of paper
            DrawLine(Color.Gray, BorderStyleEnum.Solid, 1, pWidth, yLoc + 1, pWidth, yLoc + CurrentSectionheight);
            //End "Paper"

            // Josh:
            // Draws the items before the band
            // so that the items will appear "below" the band.
            //Items
            await DrawReportItems(items, b); // now draw the report items
            //End Items 

            //Band and Text 
			RectangleF bm = new RectangleF(_hScroll,yLoc+CurrentSectionheight, PointsX(Width)+_hScroll, BANDHEIGHT);
			si.BackgroundColor = BANDCOLOR;
			si.FontFamily = "Arial";
			si.FontSize = 8;
			si.FontWeight = FontWeightEnum.Bold;

            //Josh: Added border styles for the dividing bars.
            //Makes it easier to see where page stops in a section.
            //Added padding on the left side of the text too due to the border line
            si.BStyleBottom = si.BStyleLeft = si.BStyleRight = si.BStyleTop = BANDBORDERSTYLE;
            si.BWidthBottom = si.BWidthLeft = si.BWidthRight = si.BWidthTop = BANDBORDERWIDTH;
            si.BColorBottom = si.BColorLeft = si.BColorRight = si.BColorTop = BANDBORDERCOLOR;
            si.PaddingLeft = BANDBORDERWIDTH + SCALAY*1f; 
			DrawString(title, si, bm);
            //End Band and Text 

			return CurrentSectionheight+BANDHEIGHT;
		}

		private async Task DrawReportItems(XmlNode xNode, RectangleF r)
		{
			if (xNode == null)
				return;

			IEnumerable olist;
			if (xNode.ChildNodes.Count > 1)
				olist = DrawReportItemsOrdered(xNode);		// Get list with ordered report items
			else
				olist = xNode.ChildNodes;

			foreach(XmlNode xNodeLoop in olist)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				RectangleF rir=RectangleF.Empty;
				switch (xNodeLoop.Name)
				{
					case "Textbox":
						rir = await DrawTextbox(xNodeLoop, r);
						break;
					case "Table":
                    case "fyi:Grid":
						rir = await DrawTable(xNodeLoop, r);
						break;
					case "Image":
						rir = await DrawImage(xNodeLoop, r);
						break;
                    case "CustomReportItem":
                        rir = await DrawCustomReportItem(xNodeLoop, r);
                        break;
					case "Rectangle":
						rir = await DrawRectangle(xNodeLoop, r);
						break;
					case "List":
						rir = await DrawList(xNodeLoop, r);
						break;
					case "Matrix":
						rir = await DrawMatrix(xNodeLoop, r);
						break;
					case "Subreport":
						rir = await DrawSubreport(xNodeLoop, r);
						break;
					case "Chart":
						rir = await DrawChart(xNodeLoop, r);
						break;
					case "Line":
						rir = await DrawLine(xNodeLoop, r);
						break;
				}
                // Josh:
                // Draws the item "Out of bounds" if the item is
                // "off paper".
                if (!r.Contains(rir))
                {
                    await DrawOutOfBounds(xNodeLoop, r);
                }
				if (!rir.IsEmpty)
				{
					if (this._SelectedReportItems.IndexOf(xNodeLoop) >= 0)
						DrawSelected(xNodeLoop, rir);
					else if (this.ShowReportItemOutline)
						DrawUnselectedOutline(xNodeLoop, rir);
				}
			}
		}

        // Josh: Modified so that items can be returned in reverse order as well
        // That way, items can be drawn on top of each other correctly.
        // This is useful when attempting to have text boxes over an image for example.
        private List<XmlNode> DrawReportItemsOrdered(XmlNode xNode)
        {
            return DrawReportItemsOrdered(xNode, false);
        }
        //Josh: Modified so that items can be returned in reverse order as well 
		private List<XmlNode> DrawReportItemsOrdered(XmlNode xNode, bool reverse)
		{
			// build the array
            List<XmlNode> al = new List<XmlNode>(xNode.ChildNodes.Count);
            foreach (XmlNode n in xNode.ChildNodes)
            {
                al.Add(n);
            }

            // Josh: See above comment.
            if (!reverse)
            {
                al.Sort(new ReportItemSorter(this));
            }
            else
            {
                al.Sort(new ReportItemReverseSorter(this));
            }

			return al;
		}

		public RectangleF GetReportItemRect(XmlNode xNode, RectangleF r)
		{
			RectangleF rir = GetReportItemRect(xNode);

			if (rir.Width == float.MinValue)
				rir.Width = r.Width - rir.Left;
			if (rir.Height == float.MinValue)
				rir.Height = r.Height - rir.Top;

			rir = new RectangleF(rir.Left + r.Left, rir.Top + r.Top , rir.Width, rir.Height);
            //rir.Intersect(r); //Josh: Removed so that the items will still display
            // an outline if they go farther than
            // the edge of the page
            return rir; 
		}

        // Josh: This method returns the rectangle to the Right of the paper border where the
        // item should be outlined only.
        private RectangleF GetOutOfBoundsRightReportItemRect(XmlNode xNode, RectangleF r)
        {
            RectangleF rir = GetReportItemRect(xNode);

            if (rir.Right + LEFTGAP + lMargin <= r.Right)
            {
                return rir;
            }
            else
            {
                if (rir.Width == float.MinValue)
                    rir.Width = r.Width - rir.Left;
                if (rir.Height == float.MinValue)
                    rir.Height = r.Height - rir.Top;

                RectangleF ibr = new RectangleF(rir.Left + r.Left, rir.Top + r.Top, rir.Width, rir.Height);

                ibr.Intersect(r);

                rir = new RectangleF(ibr.X + ibr.Width, ibr.Y, rir.Width - ibr.Width, rir.Height);
                return rir;
            }
        }

        // Josh: This method returns the rectangle to the Bottom of the paper border where the
        // item should be outlined only.
        private RectangleF GetOutOfBoundsBottomReportItemRect(XmlNode xNode, RectangleF r)
        {
            RectangleF rir = GetReportItemRect(xNode);

            if (rir.Bottom <= r.Bottom)
            {
                return rir;
            }
            else
            {
                if (rir.Width == float.MinValue)
                    rir.Width = r.Width - rir.Left;
                if (rir.Height == float.MinValue)
                    rir.Height = r.Height - rir.Top;

                RectangleF ibr = new RectangleF(rir.Left + r.Left, rir.Top + r.Top, rir.Width, rir.Height);

                ibr.Intersect(r);

                rir = new RectangleF(ibr.X, ibr.Y + ibr.Height, ibr.Width, rir.Height - ibr.Height);
                return rir;
            }
        } 

		/// <summary>
		/// Return the rectangle as specified by Left, Top, Height, Width elements 
		/// </summary>
		/// <param name="xNode"></param>
		/// <returns></returns>
		internal RectangleF GetReportItemRect(XmlNode xNode)
		{
			float t=0;
			float l=0;
			float w=float.MinValue;
			float h=float.MinValue;

			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Top":
						t = GetSize(xNodeLoop.InnerText);
						break;
					case "Left":
						l = GetSize(xNodeLoop.InnerText);
						break;
					case "Height":
						h = GetSize(xNodeLoop.InnerText);
						break;
					case "Width":
						w = GetSize(xNodeLoop.InnerText);
						break;
				}
			}

			RectangleF rir = new RectangleF(l, t, w, h);
			return rir;
		}
	
		private void GetLineEnds(XmlNode xNode, RectangleF r, out PointF l1, out PointF l2)
		{
			float x=0;
			float y=0;
			float w=0;
			float h=0;

			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Top":
						y = GetSize(xNodeLoop.InnerText);
						break;
					case "Left":
						x = GetSize(xNodeLoop.InnerText);
						break;
					case "Height":
						h = GetSize(xNodeLoop.InnerText);
						break;
					case "Width":
						w = GetSize(xNodeLoop.InnerText);
						break;
				}
			}

			l1 = new PointF(r.Left + x, r.Top + y);
			l2 = new PointF(l1.X+w, l1.Y+h);

			return;
		}

		private void SetReportItemHeightWidth(XmlNode xNode, float height, float width)
		{
			this.SetElement(xNode, "Height", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.##}pt", height));
			this.SetElement(xNode, "Width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.##}pt", width));
		}

		private void SetReportItemXY(XmlNode xNode, float x, float y)
		{
			this.SetElement(xNode, "Left", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.##}pt", x));
			this.SetElement(xNode, "Top", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.##}pt", y));
		}

		private void RemoveReportItemLTHW(XmlNode ri)
		{
			XmlNode w = this.GetNamedChildNode(ri, "Left");
			if (w != null)
				ri.RemoveChild(w);
			w = this.GetNamedChildNode(ri, "Top");
			if (w != null)
				ri.RemoveChild(w);
			w = this.GetNamedChildNode(ri, "Height");
			if (w != null)
				ri.RemoveChild(w);
			w = this.GetNamedChildNode(ri, "Width");
			if (w != null)
				ri.RemoveChild(w);
		}

		private async Task<RectangleF> DrawChart(XmlNode xNode, RectangleF r)
		{
			RectangleF ir = GetReportItemRect(xNode, r);
			XmlNode title = this.GetNamedChildNode(xNode, "Title");
			StyleInfo csi = await GetStyleInfo(xNode);
			csi.TextAlign = TextAlignEnum.Left;
			if (title != null)
			{
				DrawString("", csi, ir);	// for the chart background
				string caption = this.GetElementValue(title, "Caption", "");
				if (caption == "")
					caption = "Chart";
				else
					caption = "Chart: " + caption;
				// Blend the styles of the chart and the title; 
				StyleInfo tsi = await GetStyleInfo(title);
				csi.FontFamily = tsi.FontFamily;
				csi.FontSize = tsi.FontSize;
				csi.FontStyle = tsi.FontStyle;
				csi.FontWeight = tsi.FontWeight;
				csi.Color = tsi.Color;
				csi.TextAlign = TextAlignEnum.Left;
				DrawString(caption, csi, ir, false);
			}
			else
				DrawString(xNode.Name, csi, ir, false);
			return ir;
		}

        internal string GetCustomReportItemType(string type)
        {
            // type can be the type or it can be an rdl parameter
            // implement for both
            if (type.StartsWith("={?"))
            {
                // handle types like ={?BarcodeType}
                // lookup the pareamter
                string paramName = type.Substring(3, type.Length - 4);
                XmlNode pNode = this.GetNamedChildNode(rDoc.LastChild, "ReportParameters");
                if (pNode != null)
                {
                    foreach (XmlNode xNode in pNode.ChildNodes)
                    {
                        if (xNode.NodeType != XmlNodeType.Element || xNode.Name != "ReportParameter")
                            continue;
                        System.Xml.XmlAttribute na = xNode.Attributes["Name"];
                        if (na.Value == paramName)
                        {
                            XmlNode dvNode = this.GetNamedChildNode(xNode, "DefaultValue");
                            if (dvNode != null)
                            {
                                XmlNode vNode = this.GetNamedChildNode(dvNode, "Values");
                                if (vNode != null && vNode.HasChildNodes)
                                {
                                    type = vNode.FirstChild.InnerText;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return type;
        }
        
		private async Task<RectangleF> DrawCustomReportItem(XmlNode xNode, RectangleF r)
		{
			RectangleF ir = GetReportItemRect(xNode, r);
			if (!ir.IntersectsWith(_clip))
				return ir;

			StyleInfo si = await GetStyleInfo(xNode);

			XmlNode tNode = this.GetNamedChildNode(xNode, "Type");
			if (tNode == null)
			{	// shouldn't really ever happen
				DrawString("CustomReportItem requires type.", si, ir);
				return ir;		
			}
            string type = GetCustomReportItemType(tNode.InnerText);
           
            ICustomReportItem cri = null;
            Bitmap bm = null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(type);
                int width = (int)PixelsX(ir.Width - (si.PaddingLeft + si.PaddingRight));
                int height = (int)PixelsY(ir.Height - (si.PaddingTop + si.PaddingBottom));
                if (width <= 0)
                    width = 1;
                if (height <= 0)
                    height = 1;
                bm = new Bitmap(width, height);
                cri.DrawDesignerImage(ref bm);
                DrawImageSized(xNode,ImageSizingEnum.Clip, bm, si, ir);
                DrawBorder(si, ir);
            }
            catch
            {
                DrawString("CustomReportItem type is unknown.", si, ir);
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
                if (bm != null)
                    bm.Dispose();
            }

			return ir;
		}
        
        private async Task<RectangleF> DrawImage(XmlNode xNode, RectangleF r)
        {
            RectangleF ir = GetReportItemRect(xNode, r);
            if (!ir.IntersectsWith(_clip))
                return ir;

            StyleInfo si = await GetStyleInfo(xNode);

            XmlNode sNode = this.GetNamedChildNode(xNode, "Source");
            XmlNode vNode = this.GetNamedChildNode(xNode, "Value");
            if (sNode == null || vNode == null)
            {	// shouldn't really ever happen
                DrawString("Image with invalid source or value.", si, ir);
                return ir;
            }

            switch (sNode.InnerText)
            {
                case "External":
                    if (await DrawImageExternal(xNode, sNode, vNode, si, ir))
                        ir = GetReportItemRect(xNode, r);

                    DrawBorder(si, ir);
                    break;
                case "Embedded":
                    // Josh: adds base rectangle so the "paper" size is known when drawing image.
                    if (DrawImageEmbedded(xNode, sNode, vNode, si, ir, r))
                    {
                        ir = GetReportItemRect(xNode, r);
                    }

                    DrawBorder(si, ir);
                    break;
                case "Database":
                    DrawString(string.Format("Database Image: {0}.", vNode.InnerText), si, ir);
                    break;
                default:
                    DrawString(string.Format("Image, invalid source={0}.", sNode.InnerText), si, ir);
                    break;
            }
            return ir;
        }

        // Josh: adds base rectangle so the "paper" size is known when drawing image.
        private bool DrawImageEmbedded(XmlNode iNode, XmlNode sNode, XmlNode vNode, StyleInfo si, RectangleF r, RectangleF rBase) 
		{
			// First we need to find the embedded image list
			XmlNode emNode = this.GetNamedChildNode(rDoc.LastChild, "EmbeddedImages");
			if (emNode == null)
			{
				DrawString(string.Format("Image: embedded image {0} requested but there are no embedded images defined.",vNode.InnerText), si, r);
				return false;
			}
			// Next find the requested embedded image by name
			XmlNode eiNode=null;
			foreach (XmlNode xNode in emNode.ChildNodes)
			{
				if (xNode.NodeType != XmlNodeType.Element || xNode.Name != "EmbeddedImage")
					continue;
				System.Xml.XmlAttribute na = xNode.Attributes["Name"];
				if (na.Value == vNode.InnerText)
				{
					eiNode = xNode;
					break;
				}
			}
			if (eiNode == null)
			{
				DrawString(string.Format("Image: embedded image {0} not found.",vNode.InnerText), si, r);
				return false;
			}

			// Get the image data out 
			XmlNode id = this.GetNamedChildNode(eiNode, "ImageData");
			byte[] ba = Convert.FromBase64String(id.InnerText);
			
			Stream strm=null;
			System.Drawing.Image im=null;
			bool bResize=false;
			try 
			{
				strm = new MemoryStream(ba);
				im = System.Drawing.Image.FromStream(strm);	 
				// Draw based on sizing options
                // Josh: adds base rectangle so the "paper" size is known when drawing image.
                bResize = DrawImageSized(iNode, im, si, r, rBase); 
			}
			catch (Exception e)
			{
				DrawString(string.Format("Image: {0}",e.Message), si, r);
			}
			finally
			{
				if (strm != null)
					strm.Close();
				if (im != null)
					im.Dispose();
			}
			return bResize;
		}

        private System.Drawing.Image GetImageEmbedded(string emName)
        {
            // First we need to find the embedded image list
            XmlNode emNode = this.GetNamedChildNode(rDoc.LastChild, "EmbeddedImages");
            if (emNode == null)
                return null;            // no embedded images exist

            // Next find the requested embedded image by name
            XmlNode eiNode = null;
            foreach (XmlNode xNode in emNode.ChildNodes)
            {
                if (xNode.NodeType != XmlNodeType.Element || xNode.Name != "EmbeddedImage")
                    continue;
                System.Xml.XmlAttribute na = xNode.Attributes["Name"];
                if (na.Value == emName)
                {
                    eiNode = xNode;
                    break;
                }
            }
            if (eiNode == null)
                return null;    // no embedded image with that name

            // Get the image data out 
            XmlNode id = this.GetNamedChildNode(eiNode, "ImageData");
            byte[] ba = Convert.FromBase64String(id.InnerText);

            Stream strm = null;
            System.Drawing.Image im = null;
            try
            {
                strm = new MemoryStream(ba);
                im = System.Drawing.Image.FromStream(strm);
            }
            catch 
            {
                im = null;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
            }
            return im;
        }

        

        private async Task<bool> DrawImageExternal(XmlNode iNode, XmlNode sNode, XmlNode vNode, StyleInfo si, RectangleF r)
        {
            Stream strm = null;
            System.Drawing.Image im = null;
            bool bResize = false;
            try
            {
                if (vNode.InnerText[0] == '=')
                {   // Image is an expression; can't calculate at design time
                    DrawString(string.Format("Image: {0}", vNode.InnerText), si, r);
                }
                else
                {
                    // TODO: should probably put this into cached memory: instead of reading all the time
                    string fname = vNode.InnerText;
                    if (fname.StartsWith("http:") || fname.StartsWith("file:") || fname.StartsWith("https:"))
                    {

                        using HttpClient httpClient = new HttpClient();
						httpClient.AddMajorsilenceReportingUserAgent();
                        HttpResponseMessage response =await  httpClient.GetAsync(fname);
                        response.EnsureSuccessStatusCode();
                        strm = await response.Content.ReadAsStreamAsync();
                    }
                    else
                    {
                        strm = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    im = System.Drawing.Image.FromStream(strm);
                    // Draw based on sizing options
                    bResize = DrawImageSized(iNode, im, si, r);
                }
            }
            catch (Exception e)
            {
                DrawString(string.Format("Image: {0}", e.Message), si, r);
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }
            return bResize;
        }
        
        ImageSizingEnum GetSizing(XmlNode iNode)
        {
            XmlNode szNode = this.GetNamedChildNode(iNode, "Sizing");
            ImageSizingEnum ise = szNode == null ? ImageSizingEnum.AutoSize :
                ImageSizing.GetStyle(szNode.InnerText);
            return ise;
        }

        // Josh: adds the base rectange so the paper size is known.
        private bool DrawImageSized(XmlNode iNode, Image im, StyleInfo si, RectangleF r, RectangleF rBase)
        {
            return DrawImageSized(iNode, GetSizing(iNode), im, si, r, rBase);
        }

        private bool DrawImageSized(XmlNode iNode, ImageSizingEnum ise, Image im, StyleInfo si, RectangleF r)
        {
            return DrawImageSized(iNode, ise, im, si, r, r);
        } 

        private bool DrawImageSized(XmlNode iNode, Image im, StyleInfo si, RectangleF r)
        {
            return DrawImageSized(iNode, GetSizing(iNode), im, si, r);
        }

        // Josh: adds the base rectange so the paper size is known.
        private bool DrawImageSized(XmlNode iNode, ImageSizingEnum ise, Image im, StyleInfo si, RectangleF r, RectangleF rBase)
        {
			// calculate new rectangle based on padding and scroll
			RectangleF r2 = new RectangleF(r.Left + si.PaddingLeft - _hScroll,
				r.Top + si.PaddingTop - _vScroll,
				r.Width - si.PaddingLeft - si.PaddingRight,
				r.Height - si.PaddingTop - si.PaddingBottom);

			bool bResize = false;
			float height, width;		// some work variables
            Rectangle ir;	// int work rectangle
            GraphicsUnit gu;

            switch (ise)
			{
				case ImageSizingEnum.AutoSize:
                    // Note: GDI+ will stretch an image when you only provide
                    //  the left/top coordinates.  This seems pretty stupid since
                    //  it results in the image being out of focus even though
                    //  you don't want the image resized.
                    //					g.DrawImage(im, r2.Left, r2.Top);
                    // correct the height and width of the image: to match size of image
					width = PointsX(im.Width) + si.PaddingLeft + si.PaddingRight;
					height = PointsY(im.Height) + si.PaddingTop + si.PaddingBottom;
					this.SetReportItemHeightWidth(iNode, height, width);
                    
                    gu = g.PageUnit;
                    g.PageUnit = GraphicsUnit.Pixel;
                    ir = new Rectangle(PixelsX(r2.Left), PixelsY(r2.Top), im.Width, im.Height);
                    g.DrawImage(im, ir);
                    g.PageUnit = gu;
					bResize = true;
					break;
				case ImageSizingEnum.Clip:
					Region saveRegion = g.Clip;
					Region clipRegion = new Region(g.Clip.GetRegionData());
					//RectangleF r3 = new RectangleF(PointsX(r2.Left), PointsY(r2.Top), PointsX(r2.Width), PointsY(r2.Height));
					clipRegion.Intersect(r2);
					g.Clip = clipRegion;
                    gu = g.PageUnit;
                    g.PageUnit = GraphicsUnit.Pixel;
                    ir = new Rectangle(PixelsX(r2.Left), PixelsY(r2.Top), im.Width, im.Height);
                    g.DrawImage(im, ir);
                    g.PageUnit = gu;
                    g.Clip = saveRegion;
					break;
                case ImageSizingEnum.FitProportional: //Josh: Modified to allow
                    //accurate scaling of image
                    //when scrolling page.
                    // Previously, the image would
                    // "shrink" as it's box was
                    // scrolled off the page. 
					float ratioIm = (float) im.Height / (float) im.Width;
					float ratioR = r2.Height / r2.Width;
					height = r2.Height;
					width = r2.Width;
					if (ratioIm > ratioR)
					{	// this means the rectangle width must be corrected
						width = height * (1/ratioIm);
					}
					else if (ratioIm < ratioR)
					{	// this means the ractangle height must be corrected
						height = width * ratioIm;
					}

                    // Josh: creates another rectangle used to determine the area to draw
                    // so that the entire image is not "drawn" off screen and only the
                    // portion displayed is drawn.
                    RectangleF r3 = new RectangleF(r2.X, r2.Y, width, height);

                    width = (float)im.Width / (float)PixelsX(width);
                    height = (float)im.Height / (float)PixelsY(height);
                    r3.Y = rBase.Y;
                    r3.X = rBase.X;
                    r3.Intersect(rBase);
                    r3.Y = r2.Y;
                    r3.X = r2.X;

                    width = PixelsX(r3.Width) * width;
                    height = PixelsY(r3.Height) * height;

                    float startingX = PixelsX(/*(_hScroll > rBase.X) ? (_hScroll - */si.PaddingLeft/* + rBase.X) : 0*/);
                    float startingY = PixelsY(/*(_vScroll > rBase.Y) ? (_vScroll - */si.PaddingTop/* + rBase.Y) : 0*/);
                    g.DrawImage(im, r3,
                    new RectangleF(
                    startingX,
                    startingY,
                    (width > im.Width ? im.Width : width) - startingX,
                    (height > im.Height ? im.Height : height) - startingY),
                    GraphicsUnit.Pixel); 
					break;
				case ImageSizingEnum.Fit:
				default:
					g.DrawImage(im, r2);
					break;
			}
			return bResize;
		}

		private async Task<RectangleF> DrawList(XmlNode xNode, RectangleF r)
		{
			RectangleF listR = GetReportItemRect(xNode, r);
			StyleInfo si = await GetStyleInfo(xNode);
			DrawBackground(listR, si);
			DrawBorder(si, listR);

			XmlNode items = this.GetNamedChildNode(xNode, "ReportItems");

			if (items != null)
				await DrawReportItems(items, listR);

			return listR;
		}

		private async Task<RectangleF> DrawLine(XmlNode xNode, RectangleF r)
		{
			PointF l1;
			PointF l2;
					
			GetLineEnds(xNode, r, out l1, out l2);
			StyleInfo si = await GetStyleInfo(xNode);

			BorderStyleEnum ls = si.BStyleLeft;
			if (!(ls == BorderStyleEnum.Solid ||
				  ls == BorderStyleEnum.Dashed || 	
				  ls == BorderStyleEnum.Dotted))
				ls = BorderStyleEnum.Solid;

			DrawLine(si.BColorLeft, ls, si.BWidthLeft,  
				l1.X, l1.Y, l2.X, l2.Y);
			return r;
		}

		private async Task<RectangleF> DrawMatrix(XmlNode xNode, RectangleF r)
		{
			RectangleF mr = GetReportItemRect(xNode, r);		// get the matrix rectangle
			if (mr.IsEmpty)
				return mr;

			MatrixView matrix = new MatrixView(this, xNode);
			mr.Height = matrix.Height;
			mr.Width = matrix.Width;
			 
			float ypos = mr.Top;
			for (int row=0; row < matrix.Rows; row++)
			{
				float xpos = mr.Left;
				for (int col=0; col <matrix.Columns; col++)
				{
										
					MatrixItem mi = matrix[row, col];
					if (mi.ReportItem != null)
					{
						RectangleF cr = new RectangleF(xpos, ypos, mi.Width, mi.Height);
						await this.DrawReportItems(mi.ReportItem, cr);
					}
					float width = matrix[1,col].Width;
					xpos += width;
				}
				ypos += matrix[row, 1].Height;
			}

			return mr;
		}

		private async Task<RectangleF> DrawRectangle(XmlNode xNode, RectangleF r)
		{
			StyleInfo si = await GetStyleInfo(xNode);
			RectangleF ri = GetReportItemRect(xNode, r);
			DrawBackground(ri, si);
			DrawBorder(si, ri);

			XmlNode items = this.GetNamedChildNode(xNode, "ReportItems");

			if (items != null)
				await DrawReportItems(items, ri);
			
			return ri;
		}

		private void DrawSelected(XmlNode xNode, RectangleF r)
		{
			if (xNode.Name == "Line")
			{
				DrawSelectedLine(xNode, r);
				return;
			}

			StyleInfo si = new StyleInfo();

            XmlNode parent = GetReportItemDataRegionContainer(xNode);
         
            if (parent != null)
            {
                RectangleF rect = GetRectangle(parent);
                rect.X += lMargin;
                rect.Y += BANDBORDERWIDTH;
                DrawSelected(parent, rect);
            }
            si.BStyleBottom = si.BStyleLeft = si.BStyleTop = si.BStyleRight = BorderStyleEnum.Solid;
            si.BWidthBottom = si.BWidthLeft = si.BWidthRight = si.BWidthTop = 1;
            // Josh: Changed to DimGray (personal preference)
            si.BColorBottom = si.BColorLeft = si.BColorRight = si.BColorTop = Color.DimGray; //Color.LightGray; 

			if (!IsDataRegion(xNode) || xNode.Name == "List")
            {
                DrawBorder(si, r);
                DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
                    r.X - RADIUS, r.Y - RADIUS, RADIUS * 2, true);  // top left
                DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
                    r.X + r.Width - RADIUS, r.Y - RADIUS, RADIUS * 2, true);  // top right
                DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
                    r.X - RADIUS, r.Y + r.Height - RADIUS, RADIUS * 2, true);  // bottom left
                DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
                    r.X + r.Width - RADIUS, r.Y + r.Height - RADIUS, RADIUS * 2, true);  // bottom right
            }
            else
            {
                if (xNode.Name == "Table")
                {
                    HighLightTableRegion(xNode, r);
                }
            
            }

        }

        private void DrawUnselectedOutline(XmlNode xNode, RectangleF r)
        {
            // Draw a subtle outline for unselected items to make them visible
            if (xNode.Name == "Line")
                return;  // Lines already have their own rendering

            StyleInfo si = new StyleInfo();
            si.BStyleBottom = si.BStyleLeft = si.BStyleTop = si.BStyleRight = BorderStyleEnum.Dotted;
            si.BWidthBottom = si.BWidthLeft = si.BWidthRight = si.BWidthTop = 1;
            si.BColorBottom = si.BColorLeft = si.BColorRight = si.BColorTop = Color.LightGray;
            DrawBorder(si, r);
        }

        private void HighLightTableRegion(XmlNode xNode,RectangleF r)
        {
            Color[] defaultTableGroupColors =
            {
                Color.LightBlue,Color.LightGreen,Color.LightYellow,Color.Pink,Color.Orange,Color.Aquamarine
            };

            int descBandWitdh = 100;
            int descColumnsHeight = 10;

            StyleInfo siDefault = new StyleInfo();
            siDefault.BackgroundColor = Color.Aqua;
            siDefault.FontFamily = "Arial";
            siDefault.FontSize = 8;
            siDefault.FontWeight = FontWeightEnum.Bold;
            siDefault.BStyleBottom = siDefault.BStyleLeft = siDefault.BStyleRight = siDefault.BStyleTop = BorderStyleEnum.Solid;
            siDefault.BColorBottom = siDefault.BColorLeft = siDefault.BColorRight = siDefault.BColorTop =  Color.Black;
            
            RectangleF bandPos = r;
            bandPos.X = r.X + r.Width;
            bandPos.Width = descBandWitdh;


            StyleInfo si = (StyleInfo) siDefault.Clone();
            Dictionary<string, Color> assignedGroupColors = new Dictionary<string, Color>();

            Dictionary<string, List<XmlNode>> sections = GetTableSections(xNode);
            int colorGroupIndex = 0;
            foreach (var s in sections)
            {
                foreach (var p in s.Value)
                {
                    float h = GetSize(GetNamedChildNode(p, "Height").InnerText);
                    si = (StyleInfo)siDefault.Clone();
					// Scale Band Height
                    bandPos.Height = h*SCALAY;
                    if (s.Key.StartsWith("G"))
                    {
                        int sepGroup = s.Key.IndexOf('_');
                        string groupDesc = s.Key.Substring(sepGroup + 1);
                        if (groupDesc.Length > 0)
                        {
                            Color groupColor;
                            if (!assignedGroupColors.TryGetValue(groupDesc, out groupColor))
                            {

                                if (colorGroupIndex > defaultTableGroupColors.Length - 1)
                                    groupColor = defaultTableGroupColors[defaultTableGroupColors.Length-1];
                                else
                                    groupColor = defaultTableGroupColors[colorGroupIndex];

                                    assignedGroupColors.Add(groupDesc, groupColor);
                                    colorGroupIndex++;
                            }
                            si.BackgroundColor = groupColor;
                        }
                    }
                    DrawString(s.Key, si, bandPos);
                    bandPos.Y += h;
                }

            }

            //columns
            bandPos = r;
            bandPos.Y -= descColumnsHeight;
            bandPos.Height = descColumnsHeight;
            si = (StyleInfo)siDefault.Clone();
            si.TextAlign = TextAlignEnum.Center;
            DrawString("Columns", si, bandPos);

        }
        private void DrawSelectedLine(XmlNode xNode, RectangleF r)
		{
			PointF p1;
			PointF p2;

			this.GetLineEnds(xNode, r, out p1, out p2);

			DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
				p1.X - RADIUS, p1.Y - RADIUS, RADIUS*2, true);  
			DrawCircle(Color.Black, BorderStyleEnum.Solid, 1,
				p2.X + - RADIUS, p2.Y - RADIUS, RADIUS*2, true);
		}

        // Josh: Draws the "out of bounds" area of the "off paper" controls.
        private async Task DrawOutOfBounds(XmlNode xNode, RectangleF r)
        {
            StyleInfo si = await GetStyleInfo(xNode);
            RectangleF ri = GetOutOfBoundsRightReportItemRect(xNode, r);
			si.BackgroundColor = OUTITEMCOLOR;

            if (ri.Right > r.Right)//(!r.Contains(ri))
            {
                DrawBackground(ri, si);
            }

            ri = GetOutOfBoundsBottomReportItemRect(xNode, r);

            if (ri.Bottom > r.Bottom)
            {
                DrawBackground(ri, si);
            }
            return;
        }
 

		private async Task<RectangleF> DrawSubreport(XmlNode xNode, RectangleF r)
		{
			RectangleF tr = GetReportItemRect(xNode, r);		
			string subreport = this.GetElementValue(xNode, "ReportName", "<not specified>");
			string title = string.Format("Subreport: {0}", subreport);

			DrawString(title, await GetStyleInfo(xNode), tr, false);
			return tr;
		}

		private async Task<RectangleF> DrawTable(XmlNode xNode, RectangleF r)
		{
			RectangleF tr = GetReportItemRect(xNode, r);		// get the table rectangle
			if (tr.IsEmpty)
				return tr;

			// For Table width is really defined by the table columns
			float[] colWidths;
			colWidths = GetTableColumnWidths(GetNamedChildNode(xNode, "TableColumns"));
			// calc the total width
			float w=0;
			foreach (float cw in colWidths)
				w += cw;
			tr.Width = w;

			// For Table height is really defined the sum of the RowHeights
			List<XmlNode> trs = GetTableRows(xNode);
			tr.Height = GetTableRowsHeight(trs);

			DrawBackground(tr, await GetStyleInfo(xNode));

			// Loop thru the TableRows and the columns in each of them to get at the
			//  individual cell
			float yPos = tr.Y;
			foreach (XmlNode trow in trs)
			{
				XmlNode tcells=GetNamedChildNode(trow, "TableCells");

				float h = GetSize(GetNamedChildNode(trow, "Height").InnerText);

				float xPos = tr.X;
				int col=0;
				foreach (XmlNode tcell in tcells)
				{
					if (tcell.Name != "TableCell")
						continue;
					// Calculate width based on cell span
					float width = 0;
					int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));
					for (int i = 0; i < colSpan && col+i<colWidths.Length; i++)
					{
						width += colWidths[col+i];
					}

					RectangleF cellR = new RectangleF(xPos, yPos, width, h);
					await DrawReportItems(GetNamedChildNode(tcell, "ReportItems"), cellR);
					xPos += width;
					col+=colSpan;
				}
				yPos += h;
			}
			return tr;
		}

		private int GetTableColumnCount(XmlNode tblNode)
		{
			XmlNode tblCols = this.GetNamedChildNode(tblNode, "TableColumns");
			if (tblCols == null)
				return 0;
			int cols=0;
			foreach (XmlNode cNode in tblCols.ChildNodes)
			{
				if (cNode.Name == "TableColumn")
					cols++;
			}
			return cols;
		}
        
        private Dictionary<string, List<XmlNode>> GetTableSections(XmlNode xNode)
        {
            Dictionary<string, XmlNode> mainSections=new Dictionary<string, XmlNode>();
            Dictionary<string, List<XmlNode>> resultSections = new Dictionary<string, List<XmlNode>>();
            XmlNode tableNode;

            mainSections.Add("Header", null);
            mainSections.Add("Details", null);
            mainSections.Add("Footer", null);
            mainSections.Add("TableGroups", null);

            foreach (XmlNode cNode in xNode)
            {
                if (cNode.NodeType != XmlNodeType.Element)
                    continue;
                if (mainSections.ContainsKey(cNode.Name))
                {
                    mainSections[cNode.Name] = cNode;
                }
           }

           if (mainSections.TryGetValue("Header", out tableNode) && tableNode != null)
           {
                resultSections.Add("Header",GetTableRowsNodes(GetNamedChildNode(tableNode, "TableRows")));
           }

           if (mainSections.TryGetValue("TableGroups", out tableNode)&&tableNode!=null)
           {
                foreach(var tg in GetTableGroupRowsNodes(tableNode, "Header", (s) => "GH_" + s, false))
                    resultSections.Add(tg.Key,tg.Value);

           }

           if (mainSections.TryGetValue("Details", out tableNode) && tableNode != null)
           {
                resultSections.Add("Details", GetTableRowsNodes(GetNamedChildNode(tableNode, "TableRows")));
           }

           if (mainSections.TryGetValue("TableGroups", out tableNode) && tableNode != null)
           {
                foreach (var tg in GetTableGroupRowsNodes(tableNode, "Footer", (s) => "GF_" + s, true))
                    resultSections.Add(tg.Key, tg.Value);

           }

           if (mainSections.TryGetValue("Footer", out tableNode) && tableNode != null)
           {
               resultSections.Add("Footer", GetTableRowsNodes(GetNamedChildNode(tableNode, "TableRows")));
           }


            return resultSections;

        }
        
        private List<XmlNode> GetTableRows(XmlNode xNode)
        {
            List<XmlNode> trs = new List<XmlNode>();

            XmlNode tblGroups = null, header = null, footer = null, details = null;

            // Find the major groups that have TableRows
            foreach (XmlNode cNode in xNode)
            {
                if (cNode.NodeType != XmlNodeType.Element)
                    continue;
                switch (cNode.Name)
                {
                    case "Header":
                        header = cNode;
                        break;
                    case "Details":
                        details = cNode;
                        break;
                    case "Footer":
                        footer = cNode;
                        break;
                    case "TableGroups":
                        tblGroups = cNode;
                        break;
                }
            }
            GetTableRowsAdd(GetNamedChildNode(header, "TableRows"), trs);
            GetTableGroupsRows(tblGroups, trs, "Header", false);
            GetTableRowsAdd(GetNamedChildNode(details, "TableRows"), trs);
            GetTableGroupsRows(tblGroups, trs, "Footer", true);
            GetTableRowsAdd(GetNamedChildNode(footer, "TableRows"), trs);

            return trs;
        }

        private void GetTableGroupsRows(XmlNode xNode, List<XmlNode> trs, string name, bool reverse)
        {
            if (xNode == null)
                return;

			IEnumerable<XmlNode> childs;
			if (reverse) //Need for correct footer order in nested groups
				childs = xNode.ChildNodes.Cast<XmlNode>().Reverse();
			else
				childs = xNode.ChildNodes.Cast<XmlNode>();

            foreach (XmlNode xNodeLoop in childs)
            {
                if (xNodeLoop.NodeType == XmlNodeType.Element &&
                    xNodeLoop.Name == "TableGroup")
                {
                    XmlNode n = GetNamedChildNode(xNodeLoop, name);
                    if (n == null)
                        continue;
                    n = GetNamedChildNode(n, "TableRows");
                    if (n == null)
                        continue;

                    GetTableRowsAdd(n, trs);
                }
            }

        }

        private Dictionary<string,List<XmlNode>> GetTableGroupRowsNodes(XmlNode xNode,string sectionGroup,Func<string,string> groupNameKey, bool reverse)
        {
            Dictionary<string, List<XmlNode>> result = new Dictionary<string, List<XmlNode>>();

			IEnumerable<XmlNode> childs; 
			if (reverse) //Need for correct footer order in nested groups
				childs = xNode.ChildNodes.Cast<XmlNode>().Reverse();
			else
				childs = xNode.ChildNodes.Cast<XmlNode>();

            foreach (XmlNode xNodeLoop in childs)
            {
                if (xNodeLoop.NodeType == XmlNodeType.Element &&
                    xNodeLoop.Name == "TableGroup")
                {

                    XmlNode groupNode = GetNamedChildNode(xNodeLoop, "Grouping");
                    XmlAttribute groupName = groupNode.Attributes["Name"];
                    XmlNode headerNode = GetNamedChildNode(xNodeLoop, sectionGroup);
                    if (headerNode != null)
                    {
                        result.Add( groupNameKey(groupName.InnerText), GetTableRowsNodes(GetNamedChildNode(headerNode, "TableRows")));
                    }
                }
            }
            return result;
        }
        private void GetTableRowsAdd(XmlNode xNode, List<XmlNode> trs)
        {
            if (xNode == null)
                return;
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType == XmlNodeType.Element &&
                    xNodeLoop.Name == "TableRow")
                {
                    trs.Add(xNodeLoop);
                }
            }
        }
        private List<XmlNode> GetTableRowsNodes(XmlNode xNode)
        {
            if (xNode == null)
                return null;

            List<XmlNode> result = new List<XmlNode>();

            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType == XmlNodeType.Element &&
                    xNodeLoop.Name == "TableRow")
                {
                    result.Add( xNodeLoop);
                }
            }
            return result;
        }

        private float GetTableRowsHeight(List<XmlNode> trs)
		{
			float h=0;
			foreach (XmlNode tr in trs)
			{
				XmlNode cNode = GetNamedChildNode(tr, "Height");
				if (cNode != null)
					h += GetSize(cNode.InnerText);
			}
			return h;
		}

		private float[] GetTableColumnWidths(XmlNode xNode)
		{
			if (xNode == null)
				return new float[0];

            List<float> cl = new List<float>();
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType == XmlNodeType.Element && 
					xNodeLoop.Name == "TableColumn")
				{
					XmlNode cNode = GetNamedChildNode(xNodeLoop, "Width");
					if (cNode != null)
						cl.Add(GetSize(cNode.InnerText));
				}
			}
			float[] r = cl.ToArray();
			return r;
		}

		private async Task<RectangleF> DrawTextbox(XmlNode xNode, RectangleF r)
		{
			StyleInfo si = await GetStyleInfo(xNode);
			if (si.Color == Color.Empty)
				si.Color = Color.Black;

			XmlNode v = GetNamedChildNode(xNode, "Value");
			string t = v == null? "": v.InnerText;
			RectangleF ir = GetReportItemRect(xNode, r);
			DrawString(t, si, ir, !t.StartsWith("="));
			return ir;
		}

        private void DrawMargins(RectangleF behindPage)
        {
            StyleInfo si = new StyleInfo();
            si.BackgroundColor = AREABACKCOLOR;
            DrawBackground(behindPage, si);
        } 

        //private void DrawMargins(float lowerEdge) //Josh: Added Lower Edge so that the area would be filled behind the page. 
        //{
        //    // left margin
        //    // left margin //Removed Left Margin, as right margin will fill full beind page
        //    //RectangleF m = new RectangleF(0, 0, LEFTGAP, /*TotalPageHeight + LEFTGAP*/ lowerEdge); //Josh: Add a margin on bottom too. 
        //    StyleInfo si = new StyleInfo();
        //    si.BackgroundColor = Color.LightGray;
        //    si.BackgroundColor = AREABACKCOLOR; //Josh: Changed to use variable to ease page customizing. 

        //    //Full Margin // right margin
        //    RectangleF m = new RectangleF(/*pWidth - rMargin*/0, 0, PointsX(Width), /*TotalPageHeight + LEFTGAP*/ lowerEdge); //Josh: Add a margin on bottom too. 
        //    DrawBackground(m, si);
        //}

		private float TotalPageHeight
		{
			get {return pHeight;}	// eventually we'll need to add in the sizes of the separating bars
		}

        // "ReportParameter", "Name", pname
        internal XmlNode GetNamedChildNode(XmlNode xNode, string elementName, string attributeName)
        {
            // look for the parameter
            // e.g. <ReportParameters><ReportParameter Name="BarcodeType">...
            if (xNode == null)
                return null;
            foreach (XmlNode cNode in xNode.ChildNodes)
            {
                if (cNode.NodeType == XmlNodeType.Element &&
                    cNode.Name == elementName)
                {
                    System.Xml.XmlAttribute na = cNode.Attributes[attributeName];
                    if (na != null && na.Value == attributeName)
                        return cNode; // found it   
                }
            }
            return null;
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

		/// <summary>
		/// Returns the named child node; if not there it is created
		/// </summary>
		/// <param name="xNode"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal XmlNode GetCreateNamedChildNode(XmlNode xNode, string name)
		{
			if (xNode == null)		// Must have parent to create
				return null;

			XmlNode node = GetNamedChildNode(xNode, name);
			if (node == null)
				node = CreateElement(xNode, name, null);
			return node;
		}

		/// <summary>
		/// Returns the named child node; if not there it is created and the default is applied
		/// </summary>
		/// <param name="xNode"></param>
		/// <param name="name"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		internal XmlNode GetCreateNamedChildNode(XmlNode xNode, string name, string def)
		{
			if (xNode == null)		// Must have parent to create
				return null;

			XmlNode node = GetNamedChildNode(xNode, name);
			if (node == null)
			{
				node = CreateElement(xNode, name, null);
				if (def != null)
					node.InnerText = def;
			}

			return node;
		}

		internal async Task<StyleInfo> GetStyleInfo(XmlNode xNode)
		{
			StyleInfo si = new StyleInfo();
			XmlNode sNode= this.GetNamedChildNode(xNode, "Style");
			if (sNode == null)
				return si;

			foreach(XmlNode xNodeLoop in sNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "BorderColor":
						GetStyleInfoBorderColor(xNodeLoop, si);
						break;
					case "BorderStyle":
						GetStyleInfoBorderStyle(xNodeLoop, si);
						break;
					case "BorderWidth":
						GetStyleInfoBorderWidth(xNodeLoop, si);
						break;
					case "BackgroundColor":
						si.BackgroundColor = GetStyleColor(xNodeLoop.InnerText);
                        if (si.BackgroundColor.IsEmpty)
                            si.BackgroundColorText = xNodeLoop.InnerText;
                        break;
					case "BackgroundGradientType":
						si.BackgroundGradientType = StyleInfo.GetBackgroundGradientType(xNodeLoop.InnerText, BackgroundGradientTypeEnum.None);
						break;
					case "BackgroundGradientEndColor":
						si.BackgroundGradientEndColor = GetStyleColor(xNodeLoop.InnerText);
						break;
					case "BackgroundImage":
						await GetStyleInfoBackgroundImage(xNodeLoop, si);
						break;
					case "FontStyle":
						si.FontStyle = StyleInfo.GetFontStyle(xNodeLoop.InnerText, FontStyleEnum.Normal);
						break;
					case "FontFamily":
						si.FontFamily = xNodeLoop.InnerText[0] == '='? "Arial": xNodeLoop.InnerText;
						break;
					case "FontSize":
						si.FontSize = xNodeLoop.InnerText[0] == '='? 10: GetSize(xNodeLoop.InnerText);
						break;
					case "FontWeight":
						si.FontWeight = StyleInfo.GetFontWeight(xNodeLoop.InnerText, FontWeightEnum.Normal);
						break;
					case "Format":
						break;
					case "TextDecoration":
						si.TextDecoration = StyleInfo.GetTextDecoration(xNodeLoop.InnerText, TextDecorationEnum.None);
						break;
					case "TextAlign":
						si.TextAlign = StyleInfo.GetTextAlign(xNodeLoop.InnerText, TextAlignEnum.General);
						break;
					case "VerticalAlign":
						si.VerticalAlign = StyleInfo.GetVerticalAlign(xNodeLoop.InnerText, VerticalAlignEnum.Middle);
						break;
					case "Color":
						si.Color = GetStyleColor(xNodeLoop.InnerText);
                        if (si.Color.IsEmpty)
                            si.ColorText = xNodeLoop.InnerText;
						break;
					case "PaddingLeft":
						si.PaddingLeft = GetSize(xNodeLoop.InnerText);
						break;
					case "PaddingRight":
						si.PaddingRight = GetSize(xNodeLoop.InnerText);
						break;
					case "PaddingTop":
						si.PaddingTop = GetSize(xNodeLoop.InnerText);
						break;
					case "PaddingBottom":
						si.PaddingBottom = GetSize(xNodeLoop.InnerText);
						break;
					case "LineHeight":
						si.LineHeight = GetSize(xNodeLoop.InnerText); 
						break;
					case "Direction":
						si.Direction = StyleInfo.GetDirection(xNodeLoop.InnerText, DirectionEnum.LTR);
						break;
					case "WritingMode":
						si.WritingMode = StyleInfo.GetWritingMode(xNodeLoop.InnerText, WritingModeEnum.lr_tb);
						break;
					case "Language":
						si.Language = xNodeLoop.InnerText;
						break;
					case "UnicodeBiDi":
						si.UnicodeBiDirectional = StyleInfo.GetUnicodeBiDirectional(xNodeLoop.InnerText, UnicodeBiDirectionalEnum.Normal);
						break;
					case "Calendar":
						si.Calendar = StyleInfo.GetCalendar(xNodeLoop.InnerText, CalendarEnum.Gregorian);
						break;
					case "NumeralLanguage":
						break;
					case "NumeralVariant":
						break;
				}
			}
			return si;
		}

		private Color GetStyleColor(string c)
		{
			if (c == null || c.Length < 1 || c[0] == '=')
				return Color.Empty;
			Color clr;
			try
			{
				clr = ColorTranslator.FromHtml(c);
			}
			catch
			{
				clr = Color.White;
			}

			return clr;
		}

		private void GetStyleInfoBorderColor(XmlNode xNode, StyleInfo si)
		{
			Color dColor=Color.Black;
			si.BColorLeft = si.BColorRight = si.BColorTop = si.BColorBottom = Color.Empty;

			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Default":
						dColor = GetStyleColor(xNodeLoop.InnerText);
						break;
					case "Left":
						si.BColorLeft = GetStyleColor(xNodeLoop.InnerText);
						break;
					case "Right":
						si.BColorRight = GetStyleColor(xNodeLoop.InnerText);
						break;
					case "Top":
						si.BColorTop = GetStyleColor(xNodeLoop.InnerText);
						break;
					case "Bottom":
						si.BColorBottom = GetStyleColor(xNodeLoop.InnerText);
						break;
				}
			}
			if (si.BColorLeft == Color.Empty)
				si.BColorLeft = dColor;
			if (si.BColorRight == Color.Empty)
				si.BColorRight = dColor;
			if (si.BColorTop == Color.Empty)
				si.BColorTop = dColor;
			if (si.BColorBottom == Color.Empty)
				si.BColorBottom = dColor;
		}

		private void GetStyleInfoBorderStyle(XmlNode xNode, StyleInfo si)
		{
			BorderStyleEnum def = BorderStyleEnum.None;
			string l=null;
			string r=null;
			string t=null;
			string b=null;
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Default":
						def = GetBorderStyle(xNodeLoop.InnerText, BorderStyleEnum.None);
						break;
					case "Left":
						l = xNodeLoop.InnerText; 
						break;
					case "Right":
						r = xNodeLoop.InnerText; 
						break;
					case "Top":
						t = xNodeLoop.InnerText; 
						break;
					case "Bottom":
						b = xNodeLoop.InnerText; 
						break;
				}
			}
			si.BStyleLeft = l == null? def: GetBorderStyle(l, def);
			si.BStyleRight = r == null? def: GetBorderStyle(r, def);
			si.BStyleBottom = b == null? def: GetBorderStyle(b, def);
			si.BStyleTop = t == null? def: GetBorderStyle(t, def);
		}

		// return the BorderStyleEnum given a particular string value
		BorderStyleEnum GetBorderStyle(string v, BorderStyleEnum def)
		{
			BorderStyleEnum bs;
            try
            {
                bs = (BorderStyleEnum)Enum.Parse(typeof(BorderStyleEnum), v);
            }
            catch
            {
                bs = def;
            }
            return bs; 
		}

		private void GetStyleInfoBorderWidth(XmlNode xNode, StyleInfo si)
		{
			string l=null;
			string r=null;
			string t=null;
			string b=null;
			float def= GetSize("1pt");
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Default":
						def = GetSize(xNodeLoop.InnerText);
						break;
					case "Left":
						l = xNodeLoop.InnerText;
						break;
					case "Right":
						r = xNodeLoop.InnerText;
						break;
					case "Top":
						t = xNodeLoop.InnerText;
						break;
					case "Bottom":
						b = xNodeLoop.InnerText;
						break;
				}
			}
			si.BWidthTop = t == null? def: GetSize(t);
			si.BWidthBottom = b == null? def: GetSize(b);
			si.BWidthLeft = l == null? def: GetSize(l);
			si.BWidthRight = r == null? def: GetSize(r);
		}

        private async Task GetStyleInfoBackgroundImage(XmlNode xNode, StyleInfo si)
        {
            //  TODO: this is problematic since it require a PageImage
            Stream strm = null;
            System.Drawing.Image im = null;
            ImageRepeat repeat = ImageRepeat.Repeat;
            string source = null;
            string val = null;
            try
            {
                foreach (XmlNode xNodeLoop in xNode.ChildNodes)
                {
                    if (xNodeLoop.NodeType != XmlNodeType.Element)
                        continue;
                    switch (xNodeLoop.Name)
                    {	// TODO
                        case "Source":
                            if (!xNodeLoop.InnerText.StartsWith("="))
                            {
                                if (xNodeLoop.InnerText == "External" ||
                                    xNodeLoop.InnerText == "Embedded")
                                    source = xNodeLoop.InnerText;
                            }
                            break;
                        case "Value":
                            if (!xNodeLoop.InnerText.StartsWith("="))
                            {
                                val = xNodeLoop.InnerText;
                            }
                            break;
                        case "MIMEType":    // MimeType doesn't help us
                            break;
                        case "BackgroundRepeat":
                            switch (xNodeLoop.InnerText.ToLowerInvariant())
                            {
                                case "repeat":
                                    repeat = ImageRepeat.Repeat;
                                    break;
                                case "norepeat":
                                    repeat = ImageRepeat.NoRepeat;
                                    break;
                                case "repeatx":
                                    repeat = ImageRepeat.RepeatX;
                                    break;
                                case "repeaty":
                                    repeat = ImageRepeat.RepeatY;
                                    break;
                            }
                            break;
                    }
                }
                if (source == null || val == null)
                    return;     // don't have image to show in background (at least at design time)
                if (source == "External")
                {
                    if (val.StartsWith("http:") ||
                        val.StartsWith("file:") ||
                        val.StartsWith("https:"))
                    {
                        using HttpClient httpClient = new HttpClient();
						httpClient.AddMajorsilenceReportingUserAgent();
                        HttpResponseMessage response = await httpClient.GetAsync(val);
                        response.EnsureSuccessStatusCode();
                        strm = await response.Content.ReadAsStreamAsync();
                    }
                    else
                        strm = new FileStream(val, FileMode.Open, FileAccess.Read, FileShare.Read);
                    im = System.Drawing.Image.FromStream(strm);
                }
                else   // Embedded case
                {
                    im = GetImageEmbedded(val);
                }
                int height = im.Height;							// save height and width
                int width = im.Width;
                MemoryStream ostrm = new MemoryStream();
                System.Drawing.Imaging.ImageCodecInfo[] info;
                info = ImageCodecInfo.GetImageEncoders();
                EncoderParameters encoderParameters;
                encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                System.Drawing.Imaging.ImageCodecInfo codec = null;
                for (int i = 0; i < info.Length; i++)
                {
                    if (info[i].FormatDescription == "JPEG")
                    {
                        codec = info[i];
                        break;
                    }
                }
                im.Save(ostrm, codec, encoderParameters);

                byte[] ba = ostrm.ToArray();
                ostrm.Close();
                si.BackgroundImage = new PageImage(ImageFormat.Jpeg, ba, width, height);	// Create an image
                si.BackgroundImage.Repeat = repeat;
            }
            catch
            {
                // we just won't end up drawing the background image;
            }
            finally
            {
                if (strm != null)
                    strm.Close();
                if (im != null)
                    im.Dispose();
            }
        }

		internal float GetSize(XmlNode pNode, string name)
		{
			XmlNode xNode = this.GetNamedChildNode(pNode, name);
			if (xNode == null)
				return 0;
			return GetSize(xNode.InnerText);
		}

		static internal float GetSize(string t)
		{
			if (t == null || t.Length == 0 || t[0] == '=')
				return 0;

			// Size is specified in CSS Length Units
			// format is <decimal number nnn.nnn><optional space><unit>
			// in -> inches (1 inch = 2.54 cm)
			// cm -> centimeters (.01 meters)
			// mm -> millimeters (.001 meters)
			// pt -> points (1 point = 1/72 inches)
			// pc -> Picas (1 pica = 12 points)
			t = t.Trim();
			int space = t.LastIndexOf(' '); 
			string n="";					// number string
			string u="in";					// unit string
			decimal d;						// initial number
			try		// Convert.ToDecimal can be very picky
			{
				if (space != -1)	// any spaces
				{
					n = t.Substring(0,space).Trim();	// number string
					u = t.Substring(space).Trim();	// unit string
				}
				else if (t.Length >= 3)
				{
					n = t.Substring(0, t.Length-2);
					u = t.Substring(t.Length-2);
				}
				else
				{
					// Illegal unit
					return 0;
				}
				d = Convert.ToDecimal(n, NumberFormatInfo.InvariantInfo);
			}
			catch
			{
				return 0;
			}

			int size;
			switch(u)			// convert to millimeters
			{
				case "in":
					size = (int) (d * 2540m);
					break;
				case "cm":
					size = (int) (d * 1000m);
					break;
				case "mm":
					size = (int) (d * 100m);
					break;
				case "pt":
					return Convert.ToSingle(d);
				case "pc":
					size = (int) (d * (2540m / POINTSIZEM * 12m));
					break;
				default:	 
					// Illegal unit
					size = (int) (d * 2540m);
					break;
			}


			// and return as points
			return (float) ((double) size / 2540.0 * POINTSIZED);
		}

		private void DrawBackground(System.Drawing.RectangleF rect, StyleInfo si)
		{
			if (!rect.IntersectsWith(_clip))
				return;
			RectangleF dr = new RectangleF(rect.X - _hScroll, rect.Y - _vScroll, rect.Width, rect.Height);

			LinearGradientBrush linGrBrush = null;
			SolidBrush sb=null;
			try
			{
				if (si.BackgroundGradientType != BackgroundGradientTypeEnum.None &&
					!si.BackgroundGradientEndColor.IsEmpty &&
					!si.BackgroundColor.IsEmpty)
				{
					Color c = si.BackgroundColor;	
					Color ec = si.BackgroundGradientEndColor; 	

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

				if (linGrBrush != null)
				{
					g.FillRectangle(linGrBrush, dr);
				}
				else if (!si.BackgroundColor.IsEmpty)
				{
					sb = new SolidBrush(si.BackgroundColor);
					g.FillRectangle(sb, dr);
				}
			}
			finally
			{
				if (linGrBrush != null)
					linGrBrush.Dispose();
				if (sb != null)
					sb.Dispose();
			}

            if (si.BackgroundImage == null)
                return;
            // Handle any background image
            DrawImageBackground(si.BackgroundImage, si,rect);
			return;
		}

        private void DrawImageBackground(PageImage pi, StyleInfo si, RectangleF r)
        {
            Stream strm = null;
            System.Drawing.Image im = null;
            try
            {
                RectangleF r2 = new RectangleF(r.Left + si.PaddingLeft,
                    r.Top + si.PaddingTop,
                    r.Width - (si.PaddingLeft + si.PaddingRight),
                    r.Height - (si.PaddingTop + si.PaddingBottom));

                strm = new MemoryStream(pi.GetImageData((int)r2.Width, (int)r2.Height));
                im = System.Drawing.Image.FromStream(strm);

                int repeatX = 0;
                int repeatY = 0;
                float imW = PointsX(pi.SamplesW);
                float imH = PointsY(pi.SamplesH);
                switch (pi.Repeat)
                {
                    case ImageRepeat.Repeat:
                        repeatX = (int)Math.Floor(r2.Width / imW);
                        repeatY = (int)Math.Floor(r2.Height / imH);
                        break;
                    case ImageRepeat.RepeatX:
                        repeatX = (int)Math.Floor(r2.Width / imW);
                        repeatY = 1;
                        break;
                    case ImageRepeat.RepeatY:
                        repeatY = (int)Math.Floor(r2.Height / imH);
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

                RectangleF dr = new RectangleF(r2.X - _hScroll, r2.Y - _vScroll, r2.Width, r2.Height);
                float startX = dr.Left;
                float startY = dr.Top;

                Region saveRegion = g.Clip;
                Region clipRegion = new Region(g.Clip.GetRegionData());
 
                clipRegion.Intersect(dr);
                g.Clip = clipRegion;

                for (int i = 0; i < repeatX; i++)
                {
                    for (int j = 0; j < repeatY; j++)
                    {
                        float currX = startX + i * imW;
                        float currY = startY + j * imH;
                        g.DrawImage(im, new RectangleF(currX, currY, imW, imH));
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

		private void DrawBorder(StyleInfo si, RectangleF r)
		{
			if (!r.IntersectsWith(_clip))
				return;
			if (r.Height <= 0 || r.Width <= 0)		// no bounding box to use
				return;

			DrawLine(si.BColorTop, si.BStyleTop, si.BWidthTop, r.X, r.Y, r.Right, r.Y);

			DrawLine(si.BColorRight, si.BStyleRight, si.BWidthRight, r.Right, r.Y, r.Right, r.Bottom);
			
			DrawLine(si.BColorLeft, si.BStyleLeft, si.BWidthLeft, r.X, r.Y, r.X, r.Bottom);
			
			DrawLine(si.BColorBottom, si.BStyleBottom, si.BWidthBottom, r.X, r.Bottom, r.Right, r.Bottom);

			return;
			
		}

		private void DrawCircle(Color c, BorderStyleEnum bs, float penWidth, float x, float y, float d, bool bFill)  
		{
			if (bs == BorderStyleEnum.None || c.IsEmpty || d <= 0)	// nothing to draw
				return;

			// adjust coordinates for scrolling
			x -= _hScroll;
			y -= _vScroll;

			Pen p=null;
			try
			{
				p = new Pen(c, penWidth);
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
				if (bFill)
					g.FillEllipse(Brushes.Black, x, y, d, d); 
				else
					g.DrawEllipse(p, x, y, d, d);
			}
			finally
			{
				if (p != null)
					p.Dispose();
			}

		}

		private void DrawDotGrid(RectangleF rect, Size gridSize, Color dotColor)
		{
			// Draw a dot grid pattern within the specified rectangle
			if (gridSize.Width <= 0 || gridSize.Height <= 0)
				return;

			using (SolidBrush brush = new SolidBrush(dotColor))
			{
				float dotSize = 1.5f;
				for (float x = rect.X; x < rect.X + rect.Width; x += gridSize.Width)
				{
					for (float y = rect.Y; y < rect.Y + rect.Height; y += gridSize.Height)
					{
						g.FillEllipse(brush, x - dotSize / 2, y - dotSize / 2, dotSize, dotSize);
					}
				}
			}
		}

		private void DrawLine(Color c, BorderStyleEnum bs, float w,  
								float x, float y, float x2, float y2)
		{
            Color lc = c;
            if (this.ShowReportItemOutline)
            {   // force an outline
                lc = (bs == BorderStyleEnum.None || c.IsEmpty)? Color.LightGray : c;
                if (w <= 0)
                    w = 1;
            }
            else if (bs == BorderStyleEnum.None || c.IsEmpty || w <= 0)	// nothing to draw
            {
                return;
            }
			// adjust coordinates for scrolling
			x -= _hScroll;
			y -= _vScroll;
			x2 -= _hScroll;
			y2 -= _vScroll;

			Pen p=null;
			try
			{
				p = new Pen(lc, w);
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

				g.DrawLine(p,  x, y, x2, y2);
			}
			finally
			{
				if (p != null)
					p.Dispose();
			}

		}

		private void DrawString(string text, StyleInfo si, RectangleF r, bool bWrap = true)
		{
			if (!r.IntersectsWith(_clip))
				return;

			Font drawFont = null;
			StringFormat drawFormat = null;
			Brush drawBrush = null;
			var graphicsState = g.Save();

			try
			{
				// STYLE
				FontStyle fs = 0;
				if (si.FontStyle == FontStyleEnum.Italic)
					fs |= FontStyle.Italic;

				switch (si.TextDecoration)
				{
					case TextDecorationEnum.Underline:
						fs |= FontStyle.Underline;
						break;
					case TextDecorationEnum.LineThrough:
						fs |= FontStyle.Strikeout;
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
						fs |= FontStyle.Bold;
						break;
				}

				if (si.FontSize <= 0) // can't have zero length font; force to default
					si.FontSize = 10;
				try
				{
					drawFont = new Font(si.FontFamily, si.FontSize, fs); // si.FontSize already in points
				}
				catch (ArgumentException ae) // fonts that don't exist can throw exception; but we don't want it to
				{
					text = ae.Message; // show the error msg (allows report designer to see error)
					drawFont = new Font("Arial", si.FontSize, fs); // if this throws exception; we'll let it
				}

				// ALIGNMENT
				drawFormat = new StringFormat();
				if (!bWrap)
					drawFormat.FormatFlags |= StringFormatFlags.NoWrap;
				switch (si.TextAlign)
				{
					case TextAlignEnum.Left:
						drawFormat.Alignment = StringAlignment.Near;
						break;
					case TextAlignEnum.Center:
						drawFormat.Alignment = StringAlignment.Center;
						break;
					case TextAlignEnum.Right:
						drawFormat.Alignment = StringAlignment.Far;
						break;
				}

				switch (si.VerticalAlign)
				{
					case VerticalAlignEnum.Top:
						drawFormat.LineAlignment = StringAlignment.Near;
						break;
					case VerticalAlignEnum.Middle:
						drawFormat.LineAlignment = StringAlignment.Center;
						break;
					case VerticalAlignEnum.Bottom:
						drawFormat.LineAlignment = StringAlignment.Far;
						break;
				}

				DrawBackground(r, si);

				if (si.WritingMode == WritingModeEnum.tb_rl)
				{
					drawFormat.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
					drawFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
				}

				RectangleF drawRectangle;
				switch(si.WritingMode)
				{
					case WritingModeEnum.lr_tb:
					case WritingModeEnum.tb_rl:
						drawRectangle = new RectangleF(
							r.Left + si.PaddingLeft - _hScroll,
							r.Top + si.PaddingTop - _vScroll,
							r.Width - si.PaddingLeft - si.PaddingRight,
							r.Height - si.PaddingTop - si.PaddingBottom
						);
						break;
					case WritingModeEnum.tb_lr:
						drawRectangle = new RectangleF(
							-r.Top - r.Height + si.PaddingBottom,
							r.Left + si.PaddingLeft,
							r.Height - si.PaddingTop - si.PaddingBottom,
							r.Width - si.PaddingLeft - si.PaddingRight
						);
						break;
					default:
						throw new NotSupportedException($"Writing mode {si.WritingMode} is not supported");
				}

				if(si.WritingMode == WritingModeEnum.tb_lr)
				{
					g.RotateTransform(270);
				}

				drawBrush = new SolidBrush(si.Color);
				g.DrawString(text, drawFont, drawBrush, drawRectangle, drawFormat);
				g.Restore(graphicsState);
			}
			finally
			{
				drawFont?.Dispose();
				drawFormat?.Dispose();
				drawBrush?.Dispose();
			}

			DrawBorder(si, r); // Draw the border if needed
		}

		internal void PasteImage(XmlNode parent, System.Drawing.Bitmap img, PointF p)
		{
            // Josh: Adds a default ZIndex of 1 to paste above "background". 
			string t = string.Format(NumberFormatInfo.InvariantInfo,
                "<ReportItems><Image><Source>Embedded</Source><Height>{0:0.00}in</Height><Width>{1:0.00}in</Width><Sizing>FitProportional</Sizing><Zindex>1</ZIndex></Image></ReportItems>", 
									PointsY(img.Height)/POINTSIZED, PointsX(img.Width)/POINTSIZED);
			PasteReportItems(parent, t, p);
			if (_SelectedReportItems.Count != 1)	
				return;									// Paste must have failed
			XmlNode iNode = this._SelectedReportItems[0];		// Get the just pasted image
			
			// Get the name; we'll use that as the embedded image name as well
			XmlAttribute xAttr = iNode.Attributes["Name"];
			if (xAttr == null)
				return;
			string name = xAttr.Value;
			this.SetElement(iNode, "Value", name);

			XmlNode rNode = this.GetReportNode();
			XmlNode eimages = this.GetCreateNamedChildNode(rNode, "EmbeddedImages");
			XmlNode image = this.CreateElement(eimages, "EmbeddedImage", null);
			this.SetElementAttribute(image, "Name", name);
			this.CreateElement(image, "MIMEType", "image/png");		// always store in PNG format

			string imagedata=null;
			try
			{
				MemoryStream ostrm = new MemoryStream();
				ImageFormat imf = ImageFormat.Png;
				img.Save(ostrm, imf);
				byte[] ba = ostrm.ToArray();
				ostrm.Close();
				imagedata = Convert.ToBase64String(ba);
			}
			catch
			{
				imagedata=null;
			}
			finally
			{
				if (img != null)
					img.Dispose();
			}
			if (imagedata != null)
				this.CreateElement(image, "ImageData", imagedata);
			return;
		}

		/// <summary>
		/// Normally used to replace the contents of a cell in a table
		/// </summary>
		/// <param name="hl"></param>
		/// <param name="rItems"></param>
		internal void ReplaceReportItems(HitLocation hl, string rItems)
		{
			if (hl.HitNode == null)
				return;

			XmlNode repItems = hl.HitNode.ParentNode;
			if (repItems.Name != "ReportItems")
				return;

			XmlNode p = repItems.ParentNode;
			p.RemoveChild(repItems);

			PasteReportItems(p, rItems, hl.HitRelative);
		}

		internal void PasteReportItems(XmlNode parent, string rItems, PointF p)
		{
            if (this.ReportNames == null)                       // this will force names to be created before any paste
                return;

			bool bTableCell = parent.Name == "TableCell";		// inside of table no size should be specified

			XmlDocumentFragment fDoc = rDoc.CreateDocumentFragment();
			fDoc.InnerXml = rItems;

			XmlNode priNode = GetNamedChildNode(fDoc, "ReportItems");
			if (priNode == null)
				return;

			PasteValidate(parent, priNode);				// will throw exception if problem

			// Get the ReportItems node we need to paste into
			XmlNode riNode = this.GetCreateNamedChildNode(parent, "ReportItems");

			if (priNode.ChildNodes.Count > 1 && bTableCell)	// if we're trying to paste multiple items in a
															//   cell we need to put a rectangle over it
			{
				XmlNode rectNode = this.CreateElement(riNode, "Rectangle", null);
				ReportNames.GenerateName(rectNode);		// generate a new name
				riNode = this.CreateElement(rectNode, "ReportItems", null);
			}

			// We need to normalize the positions in the reportitems. Simple as 1, 2, 3
			// 1) Find the left most object and the top most object
			// 2) Adjust all objects positions
			// 3) Move the nodes into the proper ReportItems collection

			// 1) Find the left most and top most objects
			float left = float.MaxValue;
			float top = float.MaxValue;
			foreach (XmlNode ri in priNode)
			{
                if (ri.NodeType != XmlNodeType.Element)
                    continue;
                RectangleF rf = this.GetReportItemRect(ri);
				if (left > rf.Left)
					left = rf.Left;
				if (top > rf.Top)
					top = rf.Top;
			}
			// 2) Adjust all objects positions
			foreach (XmlNode ri in priNode)
			{
                if (ri.NodeType != XmlNodeType.Element)
                    continue;
				if (bTableCell)
				{
					RemoveReportItemLTHW(ri);
				}
				else
				{
					RectangleF rf = this.GetReportItemRect(ri);
					SetReportItemXY(ri, rf.Left - left + p.X, rf.Top - top + p.Y);
				}
			}

			// 3) Move the nodes into the proper ReportItems collection
			// Loop thru and put all the report items into the main document
			// This loop is a little strange because when a node is appended to
			//   the main document it is removed from the fragment.   Thus you
			//   must continually grab the first child until all the children have
			//   been removed.
			this.ClearSelected();		// the new nodes end up selected
			for(XmlNode ri = priNode.FirstChild; ri != null; ri = priNode.FirstChild)
			{
				riNode.AppendChild(ri);
				PasteNewNames(ri);
				this.AddSelection(ri);
			}
		}

		void PasteValidate(XmlNode parent, XmlNode pitems)
		{
			// check restriction on placing DataRegion or Subreport in pageheader or footer
			if (!this.InPageHeaderOrFooter(parent))
				return;

			PasteValidateRecurse(pitems);
		}

		void PasteValidateRecurse(XmlNode pitems)
		{
			if (!pitems.HasChildNodes)
				return;
			
			foreach (XmlNode item in pitems.ChildNodes)
			{
				if (this.IsDataRegion(item))
					throw new Exception(Strings.DesignXmlDraw_Error_DataRegionIntoHeaderFooter);
				if (item.Name == "Subreport")
					throw new Exception(Strings.DesignXmlDraw_Error_SubreportIntoHeaderFooter);
				XmlNode ritems = this.GetNamedChildNode(item, "ReportItems");
				if (ritems != null)
					PasteValidateRecurse(ritems);
			}
			return;
		}

		/// <summary>
		/// Normally used to replace the contents of a cell in a table with a Table, Matrix or Chart 
		/// </summary>
		/// <param name="hl"></param>
		/// <param name="rItems"></param>
		internal XmlNode ReplaceTableMatrixOrChart(HitLocation hl, string rTable)
		{
			if (hl.HitNode == null)
				return null;

			XmlNode repItems = hl.HitNode.ParentNode;
			if (repItems.Name != "ReportItems")
				return null;
			
			XmlNode p = repItems.ParentNode;
			p.RemoveChild(repItems);

			return PasteTableMatrixOrChart(p, rTable, hl.HitRelative);
		}

		internal XmlNode PasteTableMatrixOrChart(XmlNode parent, string sTable, PointF p)
		{
            if (this.ReportNames == null)                       // this will force names to be created before any paste
                return null;

            XmlDocumentFragment fDoc = rDoc.CreateDocumentFragment();
			try 
			{
				fDoc.InnerXml = sTable;
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, Strings.DesignXmlDraw_Show_XMLInvalid);
				return null;
			}

			string type;
			if (sTable.Substring(0, 6) == "<Table")
				type = "Table";
            else if (sTable.Substring(0, 9) == "<fyi:Grid")
                type = "fyi:Grid";
            else if (sTable.Substring(0, 7) == "<Matrix")
				type = "Matrix";
			else if (sTable.Substring(0, 6) == "<Chart")
				type = "Chart";
			else
				return null;

			XmlNode riNode = this.GetNamedChildNode(parent, "ReportItems");
			if (riNode == null)		// Node has to have ReportItems in order to do paste
				riNode = this.CreateElement(parent, "ReportItems", null);

			XmlNode tNode = GetNamedChildNode(fDoc, type);
			if (tNode == null)
				return null;

			bool bTableCell = parent.Name == "TableCell";		// inside of table no size should be specified
			if (bTableCell)
				RemoveReportItemLTHW(tNode);
			else
				SetReportItemXY(tNode, p.X, p.Y);

			riNode.AppendChild(tNode);		// move table into ReportItems collection

			this.ClearSelected();		// the new nodes end up selected

			// Need to go thru entire node regenerating all the ReportItem names in the table/matrix
			PasteNewNames(tNode);
			return tNode;
		}

		private void PasteNewNames(XmlNode node)
		{
			if (node == null)
				return;
			switch (node.Name)
			{
				case "Textbox":
				case "Image":
				case "Subreport":
				case "Line":
		        case "CustomReportItem":
					ReportNames.GenerateName(node);	// generate a new name
					return;
				case "Chart":
				case "Rectangle":
				case "Table":
                case "fyi:Grid":
				case "Matrix":
				case "List":
					ReportNames.GenerateName(node);	// generate a new name
					break;
				case "Style":		// just to limit some of the dead ends we might hit
				case "Filters":
					return;
				case "Grouping":		// need to assign name to groups too
					ReportNames.GenerateGroupingName(node);
					return;
			}
			foreach (XmlNode xNodeLoop in node.ChildNodes)
			{
				if (xNodeLoop.NodeType == XmlNodeType.Element)
					PasteNewNames(xNodeLoop);
			}
			return;
		}

		/// <summary>
		/// Every reportitem intersecting with the rectangle is added to the selection
		/// </summary>
		/// <param name="r">Rectangle used to test</param>
		/// <param name="hScroll">current horizontal scroll</param>
		/// <param name="vScroll">current vertical scroll</param>
		internal void SelectInRectangle(Rectangle r, float hScroll, float vScroll)
		{
			if (rDoc == null)
				return;

			_hScroll = hScroll;
			_vScroll = vScroll;
			ProcessReport(rDoc.LastChild);

			float bh = 0, hh = 0, fh = 0;

			_HitRect = new RectangleF(PointsX(r.X)+_hScroll - lMargin, PointsY(r.Y) + _vScroll,
										PointsX(r.Width), PointsY(r.Height));

			// If selected count changes then we need to repaint
			int selectedCount = this._SelectedReportItems.Count;

			// Check for hit in the header
			XmlNode hn =GetNamedChildNode(phNode, "Height");
			if (hn != null)
			{
				hh = GetSize(hn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, 0, PointsX(Width)+_hScroll, hh);
				if (rf.IntersectsWith(_HitRect))
				{
					XmlNode ri = GetNamedChildNode(phNode, "ReportItems");
					SelectInReportItems(ri, rf);
				}
			}

			// Check for hit in the body
			XmlNode bn =GetNamedChildNode(bodyNode, "Height");	 
			if (bn != null)
			{
				bh = GetSize(bn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, hh+BANDHEIGHT, PointsX(Width)+_hScroll, bh);
				if (rf.IntersectsWith(_HitRect))
				{
					XmlNode ri = GetNamedChildNode(bodyNode, "ReportItems");
					SelectInReportItems(ri, rf);
				}
			}
			// Check for hit in the footer
			XmlNode fn =GetNamedChildNode(pfNode, "Height");
			if (fn != null)
			{
				fh = GetSize(fn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, hh+BANDHEIGHT+bh+BANDHEIGHT, PointsX(Width)+_hScroll, fh);
				if (rf.IntersectsWith(_HitRect))
				{
					XmlNode ri = GetNamedChildNode(pfNode, "ReportItems");
					SelectInReportItems(ri, rf);
				}
			}

			if (selectedCount != this._SelectedReportItems.Count)
				this.Invalidate();

			return;
		}

		private void SelectInReportItems(XmlNode xNode, RectangleF r)
		{
			if (xNode == null)
				return;
			
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Textbox":
					case "Chart":
					case "Image":
					case "Subreport":
                    case "CustomReportItem":
						RectangleF rif = GetReportItemRect(xNodeLoop, r);
						if (rif.IntersectsWith(_HitRect))
							this.AddSelection(xNodeLoop);
						break;
					case "Line":
						// for line a simplification is to require endpoints to be in rectangle
                        // Josh: Done: more sophisticated line segment crosses rectangle but endpoints not in rect
                        // Josh: Implemented formula found on http://en.wikipedia.org/wiki/Line-line_intersection
                        // Detects collision of two lines and returns the point they intersect 
						PointF p1;
						PointF p2;
						this.GetLineEnds(xNodeLoop, r, out p1, out p2);
                        if (_HitRect.Contains(p1) || _HitRect.Contains(p2))
                        {
                            this.AddSelection(xNodeLoop);
                        }

                        if (AdvancedLineCollision(p1, p2, _HitRect))
                        {
                            this.AddSelection(xNodeLoop);
                        }
						break;
					case "Rectangle":
						SelectInRectangle(xNodeLoop, r);
						break;
					case "Table":
                    case "fyi:Grid":
						SelectInTable(xNodeLoop, r);
						break;
					case "Matrix":
						SelectInMatrix(xNodeLoop, r);
						break;
					case "List":
						SelectInList(xNodeLoop, r);
						break;
				}
			}
			return;
		}

        // Josh: Advanced Line Collision
        /// <summary>
        /// Tests each line segment of the selection rectangle against a line
        /// Also performs pre-calculations of formula portions that are consistent among all line segment tests
        /// </summary>
        /// <param name="p1">The beginning point of the line to test</param>
        /// <param name="p2">The ending point of the line to test</param>
        /// <param name="_hr">The HitRectangle to test for collisions with</param>
        /// <returns>True if the line intersects the rectangle, false otherwise</returns>
        private bool AdvancedLineCollision(PointF p1, PointF p2, RectangleF _hr)
        {
            //Test Top-Left to Bottom-Left
            PointF p3 = new PointF(_hr.Left, _hr.Top);
            PointF p4 = new PointF(_hr.Left, _hr.Bottom);
            if (IsIntersectingLineSegments(p1, p2, p3, p4))
                return true;

            //Test Top-Left to Top-Right
            p4 = new PointF(_hr.Right, _hr.Top);
            if (IsIntersectingLineSegments(p1, p2, p3, p4))
                return true;

            //Test Top-Right to Bottom-Right
            p3 = new PointF(_hr.Right, _hr.Bottom);
            if (IsIntersectingLineSegments(p1, p2, p3, p4))
                return true;

            //Test Bottom-Right to Bottom-Left
            p4 = new PointF(_hr.Left, _hr.Bottom);
            if (IsIntersectingLineSegments(p1, p2, p3, p4))
                return true;

            return false;
        }

        /// <summary>
        /// Test intersecting two lines
        /// </summary>
        /// <param name="a">Start line1</param>
        /// <param name="b">End line1</param>
        /// <param name="c">Start line2</param>
        /// <param name="d">End line2</param>
        /// <returns></returns>
        static bool IsIntersectingLineSegments(PointF a, PointF b, PointF c, PointF d)
        {
            float denominator = ((b.X - a.X) * (d.Y - c.Y)) - ((b.Y - a.Y) * (d.X - c.X));
            float numerator1 = ((a.Y - c.Y) * (d.X - c.X)) - ((a.X - c.X) * (d.Y - c.Y));
            float numerator2 = ((a.Y - c.Y) * (b.X - a.X)) - ((a.X - c.X) * (b.Y - a.Y));

            // Detect coincident lines (has a problem, read below)
            if (denominator == 0) return numerator1 == 0 && numerator2 == 0;

            float r = numerator1 / denominator;
            float s = numerator2 / denominator;

            return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
        }

        private void SelectInList(XmlNode xNode, RectangleF r)
		{
			RectangleF rif = GetReportItemRect(xNode, r);
			if (!rif.IntersectsWith(_HitRect))
				return;

			XmlNode ri = GetNamedChildNode(xNode, "ReportItems");
			if (ri == null)
				return;

			SelectInReportItems(ri, rif);
			if (this.SelectedCount == 0)			// if nothing inside selected select List itself
				this.AddSelection(xNode);
		}

		private void SelectInMatrix(XmlNode xNode, RectangleF r)
		{
			RectangleF mr = GetReportItemRect(xNode, r);		// get the matrix rectangle
			MatrixView matrix = new MatrixView(this, xNode);
			mr.Height = matrix.Height;
			mr.Width = matrix.Width;
			if (!mr.IntersectsWith(_HitRect))
				return;
			 
			float ypos = mr.Top;
			for (int row=0; row < matrix.Rows; row++)
			{
				float xpos = mr.Left;
				for (int col=0; col <matrix.Columns; col++)
				{
					MatrixItem mi = matrix[row, col];
					if (mi.ReportItem != null)
					{
						RectangleF cr = new RectangleF(xpos, ypos, mi.Width, mi.Height);
						SelectInReportItems(mi.ReportItem, cr);
					}
					float width = matrix[1,col].Width;
					xpos += width;
				}
				ypos += matrix[row, 1].Height;
			}

			return;
		}

		private void SelectInRectangle(XmlNode xNode, RectangleF r)
		{
			RectangleF rif = GetReportItemRect(xNode, r);
			if (!rif.IntersectsWith(_HitRect))
				return;

			XmlNode ri = GetNamedChildNode(xNode, "ReportItems");
			if (ri == null)
				return;

			SelectInReportItems(ri, rif);
			if (this.SelectedCount == 0)			// if nothing inside selected select Rectangle itself
				this.AddSelection(xNode);
		}

		private void SelectInTable(XmlNode xNode, RectangleF r)
		{
			RectangleF tr = GetReportItemRect(xNode, r);		// get the table rectangle

			// For Table width is really defined by the table columns
			float[] colWidths;
			colWidths = GetTableColumnWidths(GetNamedChildNode(xNode, "TableColumns"));
			// calc the total width
			float w=0;
			foreach (float cw in colWidths)
				w += cw;
			tr.Width = w;

			// For Table height is really defined the sum of the RowHeights
			List<XmlNode> trs = GetTableRows(xNode);
			tr.Height = GetTableRowsHeight(trs);

			if (!tr.IntersectsWith(_HitRect))
				return;

			// Loop thru the TableRows and the columns in each of them to get at the
			//  individual cell
			float yPos = tr.Y;
			foreach (XmlNode trow in trs)
			{
				XmlNode tcells=GetNamedChildNode(trow, "TableCells");

				float h = GetSize(GetNamedChildNode(trow, "Height").InnerText);

				float xPos = tr.X;
				int col=0;
				foreach (XmlNode tcell in tcells)
				{
					if (tcell.Name != "TableCell")
						continue;
					// Calculate width based on cell span
					float width = 0;
					int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));
					for (int i = 0; i < colSpan && col+i<colWidths.Length; i++)
					{
						width += colWidths[col+i];
					}

					RectangleF cellR = new RectangleF(xPos, yPos, width, h);
					if (cellR.IntersectsWith(_HitRect))
						this.SelectInReportItems(GetNamedChildNode(tcell, "ReportItems"), cellR);

					xPos += width;
					col+=colSpan;
				}
				yPos += h;
			}
			return;
		}
		
		internal bool TableColumnResize(XmlNode tcNode, int xInc)
		{
			if (tcNode == null || xInc == 0)
				return false;
			if (!(tcNode.Name == "TableColumn" ||
				  tcNode.Name == "RowGrouping" ||
				  tcNode.Name == "MatrixColumn"))
				return false;

			XmlNode w = this.GetCreateNamedChildNode(tcNode, "Width", "0pt");

			return this.ChangeWidth(w, xInc, RADIUS);
		}
		
		internal bool TableRowResize(XmlNode trNode, int yInc)
		{
			if (trNode == null || yInc == 0)
				return false;
			if (!(trNode.Name == "TableRow" || 
				  trNode.Name == "ColumnGrouping" ||
				  trNode.Name == "MatrixRow"))
				return false;

			XmlNode h = this.GetCreateNamedChildNode(trNode, "Height", "0pt");

			return this.ChangeHeight(h, yInc, RADIUS);
		}

		/// <summary>
		/// Returns the XmlNode of the report container that has the point: PageHeader, Body, List, PageFooter
		/// </summary>
		/// <param name="p">Location to look for in pixels</param>
		/// <param name="hScroll">Horizontal scroll position in points</param>
		/// <param name="vScroll">Vertical scroll position in points</param>
		internal HitLocation HitContainer(Point p, float hScroll, float vScroll)
		{
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) //Josh: Added so Report can be selected below items.
            {
                return HitNode(p, hScroll, vScroll, true);
            }
            else
            {
                return HitNode(p, hScroll, vScroll, false);
            }
		}

		/// <summary>
		/// Returns the XmlNode of the object hit
		/// </summary>
		/// <param name="p">Location to look for in pixels</param>
		/// <param name="hScroll">Horizontal scroll position in points</param>
		/// <param name="vScroll">Vertical scroll position in points</param>
		internal HitLocation HitNode(Point p, float hScroll, float vScroll)
		{
            return HitNode(p, hScroll, vScroll, false);
        }

        /// <summary>
        /// Returns the XmlNode of the object hit
        /// </summary>
        /// <param name="p">Location to look for in pixels</param>
        /// <param name="hScroll">Horizontal scroll position in points</param>
        /// <param name="vScroll">Vertical scroll position in points</param>
        /// <param name="baseOnly">True if base node is desired in place of report item</param>
        internal HitLocation HitNode(Point p, float hScroll, float vScroll, bool baseOnly)
        {
			if (rDoc == null)
				return null;

			_hScroll = hScroll;
			_vScroll = vScroll;

			ProcessReport(rDoc.LastChild);

			float bh = 0, hh = 0, fh = 0;

			_HitPoint = new PointF(PointsX(p.X)+_hScroll - lMargin, PointsY(p.Y) + _vScroll);

			// Check for hit in the header
			XmlNode hn =GetNamedChildNode(phNode, "Height");
			if (hn != null)
			{
				hh = GetSize(hn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, hh, PointsX(Width)+_hScroll, BANDHEIGHT);
				if (rf.Contains(_HitPoint))
					return new HitLocation(hn, phNode, HitLocationEnum.Inside, new PointF(0,0));
				if (_HitPoint.Y <= hh)
				{
					rf.Y = 0;
					rf.Height = hh;
                    return HitNodeTest(rf, _HitPoint, phNode, baseOnly); 
				}
			}

			// Check for hit in the body
			XmlNode bn =GetNamedChildNode(bodyNode, "Height");	 
			if (bn != null)
			{
				bh = GetSize(bn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, hh+BANDHEIGHT+bh, PointsX(Width)+_hScroll, BANDHEIGHT);
				if (rf.Contains(_HitPoint))
					return new HitLocation(bn, bodyNode, HitLocationEnum.Inside, new PointF(0,0));
				if (_HitPoint.Y <= hh+BANDHEIGHT+bh)
				{
					rf.Y = hh+BANDHEIGHT;
					rf.Height = bh;
                    return HitNodeTest(rf, _HitPoint, bodyNode, baseOnly); 
				}
			}
			// Check for hit in the footer
			XmlNode fn =GetNamedChildNode(pfNode, "Height");
			if (fn != null)
			{
				fh = GetSize(fn.InnerText);
				RectangleF rf = new RectangleF(LEFTGAP, hh+BANDHEIGHT+bh+BANDHEIGHT+fh, PointsX(Width)+_hScroll, BANDHEIGHT);
				if (rf.Contains(_HitPoint))
					return new HitLocation(fn, pfNode, HitLocationEnum.Inside, new PointF(0,0));
				if (_HitPoint.Y <= hh+BANDHEIGHT+bh+BANDHEIGHT+fh)
				{
					rf.Y = hh+BANDHEIGHT+bh+BANDHEIGHT;
					rf.Height = fh;
                    return HitNodeTest(rf, _HitPoint, pfNode, baseOnly);
				}
			}
			return null;
		}

        //Josh: Added to reduce code in previous function.
        /// <summary>
        /// Determines if the Hit Point is located in the indicated node
        /// and returns the report item or the base node hit.
        /// </summary>
        /// <param name="rf">The rectangle to test for the hit</param>
        /// <param name="_HitPoint">The location of the hit to test</param>
        /// <param name="node">The node being tested for a hit</param>
        /// <param name="baseOnly">True if only the base is to be returned instead of the item hit</param>
        /// <returns>A HitLocation if a hit was found, otherwise null</returns>
        internal HitLocation HitNodeTest(RectangleF rf, PointF _HitPoint, XmlNode node, bool baseOnly)
        {
            if (rf.Contains(_HitPoint))
            {
                XmlNode ri = GetNamedChildNode(node, "ReportItems");
                HitLocation ril = null;
                if (!baseOnly)
                {
                    ril = HitReportItems(ri, rf);
                }
                if (ril == null)
                {
                    return new HitLocation(node, node, HitLocationEnum.Inside,
                   new PointF(_HitPoint.X - rf.X, _HitPoint.Y - rf.Y));
                }
                else
                {
                    return ril;
                }
            }

            return null;
        }

		/// <summary>
		/// Changes the height of the node (if possible) using the specified increment
		/// </summary>
		/// <param name="b">The XmlNode that is the height property.</param>
		/// <param name="inc">The amount to increment in pixels</param>
		/// <returns>true if height was changed else false</returns>
		internal bool ChangeHeight(XmlNode b, int inc, float minValue)
		{
			if (b == null || inc == 0 || b.Name != "Height")
				return false;

			float h = GetSize(b.InnerText);
			h += PointsY(inc);
			if (h < minValue)
				h = minValue;

			string nv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", h);
			if (b.InnerText != nv)
			{
				b.InnerText = nv;
				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Changes the width of the node (if possible) using the specified increment
		/// </summary>
		/// <param name="b">The XmlNode that is the width property.</param>
		/// <param name="inc">The amount to increment in pixels</param>
		/// <returns>true if height was changed else false</returns>
		internal bool ChangeWidth(XmlNode b, int inc, float minValue)
		{
			if (b == null || inc == 0 || b.Name != "Width")
				return false;

			float w = GetSize(b.InnerText);
			w += PointsX(inc);
			if (w < minValue)
				w = minValue;

			string nv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", w);
			if (b.InnerText != nv)
			{
				b.InnerText = nv;
				return true;
			}
			else
				return false;
		}

		private HitLocation HitReportItems(XmlNode xNode, RectangleF r)
		{
			if (xNode == null)
				return null;
			
			HitLocation hnl=null;
            HitLocation hnlLoop = null; //Josh: Added to select highest zOrder Item. 
            foreach (XmlNode xNodeLoop in DrawReportItemsOrdered(xNode, true))//xNode.ChildNodes) 
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Textbox":
					case "Chart":
					case "Image":
					case "Subreport":
                    case "CustomReportItem":
                        hnlLoop = HitReportItem(xNodeLoop, r); 
						break;
					case "Rectangle":
                        hnlLoop = HitRectangle(xNodeLoop, r);
						break;
					case "Table":
                    case "fyi:Grid":
                        hnlLoop = HitTable(xNodeLoop, r); 
						break;
					case "Matrix":
                        hnlLoop = HitMatrix(xNodeLoop, r); 
						break;
					case "List":
                        hnlLoop = HitList(xNodeLoop, r); 
						break;
					case "Line":
                        hnlLoop = HitLine(xNodeLoop, r); 
						break;
				}
                if (hnlLoop != null)
                {
                    hnl = hnlLoop;//return hnl; //Josh: Changed to allow selecting of topmost item 
                    break;
                }
			}
            return hnl; //Josh: Changed to allow selecting of topmost item
		}

		private HitLocation HitList(XmlNode xNode, RectangleF r)
		{
			return HitRectangle(xNode, r);		// Same logic as Rectangle
		}

		private HitLocation HitLine(XmlNode xNode, RectangleF r)
		{
			PointF p1;
			PointF p2;
			this.GetLineEnds(xNode, r, out p1, out p2);

			RectangleF rifLoc;

			if (this.SelectedCount == 1 &&
				this._SelectedReportItems[0] == xNode)
			{
				// When selected node; we do special testing to determine if the location
				//   hits one of the special sizing locations

				rifLoc = new RectangleF(p1.X-RADIUS, p1.Y-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.LineLeft, new PointF(0,0));

				rifLoc = new RectangleF(p2.X-RADIUS, p2.Y-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.LineRight, new PointF(0,0));
			}
		
			// Construct a ploygon that surrounds the line
			PointF[] pg = new PointF[5];
			if (p1.X <= p2.X)
			{
				pg[0] = pg[4] = new PointF(p1.X-RADIUS, p1.Y-RADIUS);
				pg[1] = new PointF(p2.X+RADIUS, p2.Y-RADIUS);
				pg[2] = new PointF(p2.X+RADIUS, p2.Y+RADIUS);
				pg[3] = new PointF(p1.X-RADIUS, p1.Y+RADIUS);
			}
			else
			{
				pg[0] = pg[4] = new PointF(p2.X-RADIUS, p2.Y-RADIUS);
				pg[1] = new PointF(p1.X+RADIUS, p1.Y-RADIUS);
				pg[2] = new PointF(p1.X+RADIUS, p1.Y+RADIUS);
				pg[3] = new PointF(p2.X-RADIUS, p2.Y+RADIUS);
			}
			if (InsidePolygon(pg, _HitPoint))
			{
				bool bSelected = IsNodeSelected(xNode);
				HitLocation hl = new HitLocation(xNode, null, 
					bSelected? HitLocationEnum.Move: HitLocationEnum.Inside, new PointF(0,0));
				return hl;
			}
			else
				return null;
		}

		private bool InsidePolygon(PointF[] points, PointF p)
		{
			// see http://astronomy.swin.edu.au/~pbourke/geometry/insidepoly/ Solution 3
			for (int i=0; i < points.Length-1; i++)
			{
				if ((p.Y - points[i].Y)*(points[i+1].X - points[i].X) - 
					(p.X - points[i].X)*(points[i+1].Y - points[i].Y) < 0 )
					return false;
			}
			return true;
		}

		private HitLocation HitMatrix(XmlNode xNode, RectangleF r)
		{
			RectangleF mr = GetReportItemRect(xNode, r);		// get the matrix rectangle
			MatrixView matrix = new MatrixView(this, xNode);
			mr.Height = matrix.Height;
			mr.Width = matrix.Width;
			 
			if (!mr.Contains(_HitPoint))
				return null;

			// Check to see if column resize location
			HitLocation hl = HitMatrixColumnResize(xNode, matrix, mr);
			if (hl != null)
				return hl;

			// Check to see if Row resize location 
			hl = HitMatrixRowResize(xNode, matrix, mr);
			if (hl != null)
				return hl;

			float ypos = mr.Top;
			HitLocation result = null;
			for (int row=0; row < matrix.Rows && result == null; row++)
			{
				float xpos = mr.Left;
				for (int col=0; col <matrix.Columns && result == null; col++)
				{
										
					MatrixItem mi = matrix[row, col];
					if (mi.ReportItem != null)
					{
						RectangleF cr = new RectangleF(xpos, ypos, mi.Width, mi.Height);
						result = HitReportItems(mi.ReportItem, cr);
					}
					float width = matrix[1,col].Width;
					xpos += width;
				}
				ypos += matrix[row, 1].Height;
			}

			if (result == null)
				result = new HitLocation(xNode, xNode, HitLocationEnum.Inside, new PointF(0,0));
			else
				result.HitContainer = xNode;

			return result;
		}

		private HitLocation HitMatrixColumnResize(XmlNode xNode, MatrixView matrix, RectangleF r)
		{
			// Loop thru the matrix columns to see if point is over the column resize area
			int i;
			float xPos = r.Right-RADIUS;

			// Need to loop backwards thru the columns; so we can resize 0 length columns
			for (i = matrix.Columns-1; i >= 0; i--)
			{
				RectangleF cr = new RectangleF(xPos, r.Top, RADIUS * 2, r.Height);
				if (cr.Contains(_HitPoint))
					break;
				xPos -= matrix[1, i].Width;
			}
			if (i < 0)
				return null;

			XmlNode hNode=null;
			for (hNode=matrix[matrix.HeaderRows, i].ReportItem; hNode != null; hNode = hNode.ParentNode)
			{
				if (hNode.Name == "RowGrouping")
					break;
			}

			HitLocation result=null;
			if (hNode != null)
				result = new HitLocation(hNode, xNode, HitLocationEnum.TableColumnResize, new PointF(0,0));
			else
			{
				// find out which column it is.
				// 1) Get the report item found
				XmlNode ri = matrix[matrix.HeaderRows, i].ReportItem;
				// 2) Find its relative location in the MatrixCells
				XmlNode mcells = DesignXmlDraw.FindNextInHierarchy(xNode, "MatrixRows", "MatrixRow", "MatrixCells");
				if (mcells == null)
					return null;
				int offsetColumn=0;
				foreach (XmlNode mcell in mcells.ChildNodes)
				{
					if (mcell.Name != "MatrixCell")
						continue;
					XmlNode cri = DesignXmlDraw.FindNextInHierarchy(mcell, "ReportItems");
					if (ri == cri)
						break;
					offsetColumn++;
				}
				// 3) Now find the same relative location in MatrixColumns
				XmlNode mcols =	DesignXmlDraw.FindNextInHierarchy(xNode, "MatrixColumns");
				if (mcols == null)
					return null;
				XmlNode mc=null;
				foreach (XmlNode mcol in mcols.ChildNodes)
				{
					if (offsetColumn == 0)
					{
						mc = mcol;
						break;
					}
					offsetColumn--;
				}
				if (mc == null)		// Not found; just use the first one
					mc = DesignXmlDraw.FindNextInHierarchy(xNode, 
										"MatrixColumns", "MatrixColumn");
				if (mc != null)
					result = new HitLocation(mc, xNode, HitLocationEnum.TableColumnResize, new PointF(0,0));
			}
			return result;
		}

		private HitLocation HitMatrixRowResize(XmlNode xNode, MatrixView matrix, RectangleF r)
		{
			// Loop thru the matrix rows to see if point is over the row resize area
			float yPos = r.Bottom-RADIUS;

			int row;
			for (row = matrix.Rows-1; row >= 0; row--)
			{
				RectangleF mr = new RectangleF(r.Left, yPos, r.Width, RADIUS * 2);
				if (mr.Contains(_HitPoint))
					break;
				yPos -= matrix[row, 0].Height;
			}
			if (row < 0)
				return null;

			XmlNode hNode=null;
			for (hNode=matrix[row, matrix.HeaderColumns].ReportItem; hNode != null; hNode = hNode.ParentNode)
			{
				if (hNode.Name == "ColumnGrouping")
					break;
			}

			HitLocation result=null;
			if (hNode != null)
				result = new HitLocation(hNode, xNode, HitLocationEnum.TableRowResize, new PointF(0,0));
			else
			{
				// find out which row it is.
				// 1) Get the report item found
				XmlNode ri = matrix[row, matrix.HeaderColumns].ReportItem;
				// 2) Find its location in the MatrixRows
				XmlNode mrows = DesignXmlDraw.FindNextInHierarchy(xNode, "MatrixRows");
				if (mrows == null)
					return null;
				XmlNode mr=null;
				foreach (XmlNode mrow in mrows.ChildNodes)
				{
					if (mrow.Name != "MatrixRow")
						continue;
					// find the report item
					XmlNode mcells = DesignXmlDraw.FindNextInHierarchy(mrow, "MatrixCells");
					if (mcells == null)
						return null;
					foreach (XmlNode mcell in mcells.ChildNodes)
					{
						if (mcell.Name != "MatrixCell")
							continue;
						XmlNode cri = DesignXmlDraw.FindNextInHierarchy(mcell, "ReportItems");
						if (ri == cri)
						{
							mr = mrow;
							break;
						}
					}
					if (mr != null)
						break;
				}
				if (mr == null)		// Not found; just use the first one
				{
					mr = DesignXmlDraw.FindNextInHierarchy(xNode, 
						"MatrixRows", "MatrixRow");
				}
				if (mr != null)
					result = new HitLocation(mr, xNode, HitLocationEnum.TableRowResize, new PointF(0,0));
			}
			return result;
		}

		private HitLocation HitRectangle(XmlNode xNode, RectangleF r)
		{
			HitLocation hl = HitReportItem(xNode, r);
			if (hl == null)
				return null;

			if (hl.HitSpot != HitLocationEnum.Inside)	// if it didn't hit the inside
				return hl;								//   we just return the node (so it can be resized)
			hl.HitContainer = xNode;					// Rectangle is its own container

			RectangleF rif = GetReportItemRect(xNode, r);
			hl.HitRelative = new PointF(_HitPoint.X - rif.X, _HitPoint.Y - rif.Y);

			XmlNode ri = GetNamedChildNode(xNode, "ReportItems");
			if (ri == null)
				return hl;

			HitLocation hl2 = HitReportItems(ri, GetReportItemRect(xNode, r));

			return hl2 != null? hl2: hl;		// return either the rectangle or the embedded reportitem
		}

		private HitLocation HitReportItem(XmlNode xNode, RectangleF r)
		{
			RectangleF rif = GetReportItemRect(xNode, r);

			if (this.SelectedCount == 1 &&
				this._SelectedReportItems[0] == xNode)
			{
				// When selected node; we do special testing to determine if the location
				//   hits one of the special sizing locations

				// Try it in a big rectangle around all the sizing rectangles
				RectangleF rifLoc = new RectangleF(rif.X-RADIUS, rif.Y-RADIUS, rif.Width+ 2*RADIUS, rif.Height+2*RADIUS);
				if (!rifLoc.Contains(_HitPoint))	
					return null;

				// Top Left sizing
				rifLoc = new RectangleF(rif.X-RADIUS, rif.Y-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.TopLeft, new PointF(0,0));

				// Top Right sizing
				rifLoc = new RectangleF(rif.X+rif.Width-RADIUS, rif.Y-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.TopRight, new PointF(0,0));

				// Bottom Left sizing
				rifLoc = new RectangleF(rif.X-RADIUS, rif.Y+rif.Height-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.BottomLeft, new PointF(0,0));

				// Bottom Right sizing
				rifLoc = new RectangleF(rif.X+rif.Width-RADIUS, rif.Y+rif.Height-RADIUS, 2*RADIUS, 2*RADIUS);
				if (rifLoc.Contains(_HitPoint))	
					return new HitLocation(xNode, null, HitLocationEnum.BottomRight, new PointF(0,0));
			}
			
			// ok try the point in the rectangle now
			if (rif.Contains(_HitPoint))
			{
				bool bSelected = IsNodeSelected(xNode);
				HitLocation hl = new HitLocation(xNode, null, 
								bSelected? HitLocationEnum.Move: HitLocationEnum.Inside, new PointF(0,0));
				// Correct hit location (move or inside) for reportitems that contain moveable items
				if (bSelected && (xNode.Name == "List" || xNode.Name == "Rectangle"))
				{
					RectangleF innerRect = new RectangleF(rif.Left+RADIUS, rif.Top+RADIUS, rif.Width-RADIUS*2, rif.Height-RADIUS*2); 
					if (innerRect.Contains(_HitPoint))
						hl.HitSpot = HitLocationEnum.Inside;
				}
				return hl;
			}
			else
				return null;
		}

		private HitLocation HitTable(XmlNode xNode, RectangleF r)
		{
			RectangleF tr = GetReportItemRect(xNode, r);		// get the table rectangle

			// For Table width is really defined by the table columns
			float[] colWidths;
			colWidths = GetTableColumnWidths(GetNamedChildNode(xNode, "TableColumns"));
			// calc the total width
			float w=0;
			foreach (float cw in colWidths)
				w += cw;
			tr.Width = w;

			// For Table height is really defined the sum of the RowHeights
            List<XmlNode> trs = GetTableRows(xNode);
			tr.Height = GetTableRowsHeight(trs);

			// If not in the bigger rectangle; its not in any smaller rectangles
			if (!tr.Contains(_HitPoint))
				return null;

			// Check to see if column resize location
			HitLocation hl = HitTableColumnResize(xNode, tr, colWidths);
			if (hl != null)
				return hl;

			// Check to see if Row resize location 
			hl = HitTableRowResize(xNode, tr, trs);
			if (hl != null)
				return hl;

			// Loop thru the TableRows and the columns in each of them to get at the
			//  individual cell
			float yPos = tr.Y;
			foreach (XmlNode trow in trs)
			{
				XmlNode tcells=GetNamedChildNode(trow, "TableCells");

				float h = GetSize(GetNamedChildNode(trow, "Height").InnerText);

				float xPos = tr.X;
				int col=0;
				foreach (XmlNode tcell in tcells)
				{
					if (tcell.Name != "TableCell")
						continue;
					// Calculate width based on cell span
					float width = 0;
					int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));
					for (int i = 0; i < colSpan && col+i<colWidths.Length; i++)
					{
						width += colWidths[col+i];
					}

					RectangleF cellR = new RectangleF(xPos, yPos, width, h);
					if (cellR.Contains(_HitPoint))
					{
						HitLocation hnl = HitReportItems(GetNamedChildNode(tcell, "ReportItems"), cellR);
						if (hnl != null)
						{
							if (hnl.HitContainer == null)
								hnl.HitContainer = xNode;	// if not already in container; put in table
							return hnl;
						}
						else
							return new HitLocation(tcell, null, HitLocationEnum.Inside, new PointF(0,0));
					}
					xPos += width;
					col+=colSpan;
				}
				yPos += h;
			}
			return null;
		}

		private HitLocation HitTableColumnResize(XmlNode xNode, RectangleF r, float[] colWidths)
		{
			XmlNode tcols = this.GetNamedChildNode(xNode, "TableColumns");
			if (tcols == null)
				return null;

			// Loop thru the table columns to see if point is over the column resize area
			int i;
			XmlNode cn=null;
			float xPos = r.Right-RADIUS;

			// Need to loop backwards thru the columns; so we can resize 0 length columns
			for (i = colWidths.Length-1; i >= 0; i--)
			{
				RectangleF cr = new RectangleF(xPos, r.Top, RADIUS * 2, r.Height);
				if (cr.Contains(_HitPoint))
					break;
				xPos -= colWidths[i];
			}
			if (i < 0)
				return null;

			// Now find the node that matches this column
			int ci=0;
			foreach (XmlNode tcn in tcols.ChildNodes)
			{
				if (tcn.Name != "TableColumn")
					continue;
				if (ci++ == i)
				{
					cn = tcn;
					break;
				}
			}

			if (cn == null)			// really shouldn't happen
				return null;
			
			HitLocation hl = new HitLocation(cn, xNode, HitLocationEnum.TableColumnResize, new PointF(0,0));
			return hl;
		}

        private HitLocation HitTableRowResize(XmlNode xNode, RectangleF r, List<XmlNode> trs)
		{
			// Loop thru the table rows to see if point is over the row resize area
			XmlNode rn=null;
			float yPos = r.Bottom-RADIUS;

            List<XmlNode> reverse = new List<XmlNode>(trs);
			reverse.Reverse();
			foreach (XmlNode trn in reverse)
			{
				RectangleF tr = new RectangleF(r.Left, yPos, r.Width, RADIUS * 2);
				if (tr.Contains(_HitPoint))
				{
					rn = trn;
					break;
				}
				yPos -= GetSize(GetNamedChildNode(trn, "Height").InnerText);
			}
			if (rn == null)
				return null;
			
			HitLocation hl = new HitLocation(rn, xNode, HitLocationEnum.TableRowResize, new PointF(0,0));
			return hl;
		}

		internal bool MoveReportItem(XmlNode b, int xInc, int yInc)
		{
			if (b == null)
				return false;
			if (xInc == 0 && yInc == 0)
				return false;
			if (InTable(b))
			{
				while (b != null && !(b.Name == "Table" || b.Name == "fyi:Grid"))
				{
					b = b.ParentNode;
				}
				if (b == null)
					return false;
			}
			else if (InMatrix(b))
			{
				while (b != null && b.Name != "Matrix")
				{
					b = b.ParentNode;
				}
				if (b == null)
					return false;
			}

			XmlNode lNode = this.GetNamedChildNode(b, "Left");
			if (lNode == null)
				lNode = CreateElement(b, "Left", "0pt");
			XmlNode tNode = this.GetNamedChildNode(b, "Top");
			if (tNode == null)
				tNode = CreateElement(b, "Top", "0pt");

			// Assume nothing changed
			bool changed = false;

			// handle Left
			float l = GetSize(lNode.InnerText);
			l += PointsX(xInc);
			if (l < 0)
				l = 0;
			string lv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", l);
			if (lNode.InnerText != lv)
			{
				lNode.InnerText = lv;
				changed = true;
			}
            // handle top
			float t = GetSize(tNode.InnerText);
			t += PointsY(yInc);
			if (t < 0)
				t = 0;

			string tv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", t);
			if (tNode.InnerText != tv)
			{
				tNode.InnerText = tv;
				changed = true;
			}
			
			return changed;
		}

		internal bool MoveSelectedItems(int xInc, int yInc, HitLocationEnum hle)
		{
			bool rc=false;
			// Are we just moving the selected items
			if (hle == HitLocationEnum.Move)
			{
                List<XmlNode> ar = null;
				foreach (XmlNode ri in this._SelectedReportItems)
				{
					// Ensure we don't move table or matrixes multiple times depending on selection
					XmlNode tm = TMParent(ri);
					if (tm == null)
					{
						rc |= MoveReportItem(ri, xInc, yInc);
						continue;
					}
					if (ar == null)
                        ar = new List<XmlNode>();
					else if (ar.Contains(tm))
						continue;
					ar.Add(tm);
					rc |= MoveReportItem(tm, xInc, yInc);
				}
				return rc;
			}

			if (_SelectedReportItems.Count <= 0)
				return false;
			
			// Ok we need to resize the nodes
			rc = false;
			foreach (XmlNode xNode in _SelectedReportItems)
			{
				rc |= ResizeReportItem(xNode, xInc, yInc, hle);
			}
			return rc;
		}

		internal bool ResizeReportItem(XmlNode xNode, int xInc, int yInc, HitLocationEnum hle)
		{
			if (xInc == 0 && yInc == 0)
				return false;
			if (InTable(xNode) || InMatrix(xNode))
				return false;

			bool rc=false;

			float xIncPt = PointsX(xInc);
			float yIncPt = PointsY(yInc);
			XmlNode lNode = this.GetCreateNamedChildNode(xNode, "Left", "0pt");
			float l = GetSize(lNode.InnerText);
			XmlNode tNode = this.GetCreateNamedChildNode(xNode, "Top", "0pt");
			float t = GetSize(tNode.InnerText);
			XmlNode wNode = this.GetNamedChildNode(xNode, "Width");
			float w = wNode == null? float.MinValue: GetSize(wNode.InnerText);
			XmlNode hNode = this.GetNamedChildNode(xNode, "Height");
			float h = hNode == null? float.MinValue: GetSize(hNode.InnerText);

			switch (hle)
			{
				case HitLocationEnum.BottomLeft:
					// need to adjust x/y position and height/width
					if (wNode == null || hNode == null)
						return false;
					l += xIncPt;
					w -= xIncPt; 
					h += yIncPt;
					break;
				case HitLocationEnum.BottomMiddle:
					// need to adjust height
					if (hNode == null)
						return false;
					h += yIncPt;
					break;
				case HitLocationEnum.BottomRight:
					// need to adjust width and height
					if (wNode == null || hNode == null)
						return false;
					w += xIncPt; 
					h += yIncPt;
					break;
				case HitLocationEnum.LeftMiddle:
					// need to adjust x position and width
					if (wNode == null)
						return false;
					l += xIncPt;
					w -= xIncPt; 
					break;
				case HitLocationEnum.RightMiddle:
					// need to adjust width
					if (wNode == null)
						return false;
					w += xIncPt; 
					break;
				case HitLocationEnum.TopLeft:
					// need to adjust x,y position and height
					if (wNode == null || hNode == null)
						return false;
					l += xIncPt;
					w -= xIncPt; 
					t += yIncPt;
					h -= yIncPt;
					break;
				case HitLocationEnum.TopMiddle:
					// need to adjust y position and height
					if (hNode == null)
						return false;
					t += yIncPt;
					h -= yIncPt;
					break;
				case HitLocationEnum.TopRight:
					// need to adjust y position, width and height
					if (wNode == null || hNode == null)
						return false;
					w += xIncPt; 
					t += yIncPt;
					h -= yIncPt;
					break;
				case HitLocationEnum.LineLeft:
					if (l + xIncPt < 0)
					{
						xIncPt = -l;
						l = 0;
					}
					else
						l += xIncPt;
					if (t + yIncPt < 0)
					{
						yIncPt = -t;
						t = 0;
					}
					else
						t += yIncPt;
					w -= xIncPt;
					h -= yIncPt;
					break;
				case HitLocationEnum.LineRight:
					w += xIncPt;
					h += yIncPt;
					break;
			}

			// Normalize results
			if (l < 0)
			{
				w -= l;
				l = 0;
			}
			if (t < 0)
			{
				h -= t;
				t = 0;
			}
			if (xNode.Name == "Line")	
			{ // Line height and widths allowed to go negative but overall position can't be < 0
				if (t + h < 0)
					h = -t;
				if (l + w < 0)
					w = -l;
			}
			else
			{ // Don't let height and width go too small
				if (h < .1)
					h = .1f;
				if (w < .1)
					w = .1f;
			}
			string tv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", t);
			if (tNode.InnerText != tv)
			{
				tNode.InnerText = tv;
				rc = true;
			}

			string lv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", l);
			if (lNode.InnerText != lv)
			{
				lNode.InnerText = lv;
				rc = true;
			}

			string wv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", w);
			if (wNode != null && wNode.InnerText != wv)
			{
				wNode.InnerText = wv;
				rc = true;
			}

			string hv = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", h);
			if (hNode != null && hNode.InnerText != hv)
			{
				hNode.InnerText = hv;
				rc = true;
			}

			return rc;
		}

		internal XmlElement CreateElement(XmlNode parent, string name, string val)
		{
			XmlElement node;
			if (name.StartsWith("rd:"))	
			{
				string nms = rDoc.DocumentElement.GetNamespaceOfPrefix("rd");
				if (nms == null || nms.Length == 0)
					nms = DialogValidateRdl.SCHEMA2025;

				node = rDoc.CreateElement(name, nms);
			}
			else if (name.StartsWith("fyi:"))	
			{
				string nms = rDoc.DocumentElement.GetNamespaceOfPrefix("fyi");
				if (nms == null || nms.Length == 0)
					nms = DialogValidateRdl.SCHEMA2025;

				node = rDoc.CreateElement(name, nms);
			}
			else
				node = rDoc.CreateElement(name);
			if (val != null)
				node.InnerText = val;
			parent.AppendChild(node);
			return node;
		}

		/// <summary>
		/// Use for testing a name for validity.  If null is return name is valid.
		/// Otherwise a string describing the error is returned.
		/// </summary>
		/// <param name="xNode"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		internal string NameError(XmlNode xNode, string name)
		{
			return ReportNames.NameError(xNode, name);
		}

		internal string GroupingNameCheck(XmlNode xNode, string name)
		{
			return ReportNames.GroupingNameCheck(xNode, name);
		}

		internal bool SetName(XmlNode xNode, string name)
		{
			return ReportNames.ChangeName(xNode, name);
		}

		internal bool SetGroupName(XmlNode xNode, string name)
		{
			return ReportNames.ChangeGroupName(xNode, name);
		}

		internal void SetElementAttribute(XmlNode parent, string name, string val)
		{
			XmlAttribute attr = parent.Attributes[name];
			if (attr != null)
			{
				attr.Value = val;
			}
			else
			{
				attr = rDoc.CreateAttribute(name);
				attr.Value = val;
				parent.Attributes.Append(attr);
			}
			return;

		}

		internal void RemoveElement(XmlNode parent, string name)
		{
			XmlNode node = this.GetNamedChildNode(parent, name);
			if (node != null)
			{
				parent.RemoveChild(node);
			}
			return;
		}

        internal void RemoveElementAll(XmlNode parent, string name)
        {
            XmlNode node = this.GetNamedChildNode(parent, name);
            while (node != null)
            {
                parent.RemoveChild(node);
                node = this.GetNamedChildNode(parent, name);
            }
            return;
        }

		internal string GetDataSetNameValue(XmlNode dataRegion)
		{
			XmlNode dr = GetReportItemDataRegionContainer(dataRegion);
			if (dr != null)		// dataRegion is embedded in a dataregion if not null
				return GetDataSetNameValue(dr);
			
			XmlNode node = this.GetNamedChildNode(dataRegion, "DataSetName");
			if (node != null)
				return node.InnerText;
			// Need to find the single data set specified in report
			XmlNode rNode = this.GetReportNode();
			node = GetNamedChildNode(rNode, "DataSets");
			if (node == null)
				return "";
			node = GetNamedChildNode(node, "DataSet");

			if (node == null)
				return "";
			XmlAttribute xAttr = node.Attributes["Name"];
			if (xAttr == null)
				return "";
			return xAttr.Value;
		}

		internal string GetElementAttribute(XmlNode parent, string name, string def)
		{
			XmlAttribute attr = parent.Attributes[name];
			if (attr == null)
				return def;
			else
				return attr.Value;
		}

		internal string GetElementValue(XmlNode parent, string name, string defaultV)
		{
			XmlNode node = this.GetNamedChildNode(parent, name);
			if (node == null)
				return defaultV;
			else
				return node.InnerText;
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

		internal void SetTableCellColSpan(XmlNode tcell, string val)
		{
			string oldval = this.GetElementValue(tcell, "ColSpan", "1");
			if (oldval == val)		// if they're the same we're all done
				return;
			int iold = Convert.ToInt32(oldval);
			int inew = Convert.ToInt32(val);
			if (inew <= 0)		
				inew = 1;

			// find out the maximum column span
			int maxcolspan=0;
			for (XmlNode n = tcell; n != null; n = n.NextSibling)
			{
				if (n.Name == "TableCell")
				{
					string span = this.GetElementValue(n, "ColSpan", "1");
					maxcolspan+=Convert.ToInt32(span);
				}
			}
			if (inew > maxcolspan)
				inew = maxcolspan;

			int dif = iold - inew;
			if (dif == 0)			// string comparison isn't always correct; especially after correction
				return;

			XmlNode tcells = tcell.ParentNode;
			if (dif < 0)
			{	// we need to remove "dif" number of TableCell entries after this one
				for (XmlNode n = tcell.NextSibling; n != null && dif < 0; n = tcell.NextSibling)
				{
					if (n.Name == "TableCell")
					{
						tcells.RemoveChild(n);
						dif++;
					}
				}
			}
			else	// dif > 0
			{	// we need to create "dif" number of TableCell entries after this one
				while (dif > 0)
				{
					InsertTableColumn(tcells, tcell, false);
					dif--;
				}
			}

			// Finally set the new ColSpan value 
			this.SetElement(tcell, "ColSpan", inew.ToString());

			return;
		}

		internal XmlNode GetReportNode()
		{
			return rDoc.DocumentElement;
		}

		internal IEnumerable GetReportItems(string query)
		{
			XmlNodeList nodeList;
			XmlElement root = this.rDoc.DocumentElement;

			if (root.NamespaceURI != String.Empty)
			{
				if (query == null)
					query = "//Textbox|//Rectangle|//Image|//Subreport|//Chart|//Table|//List|//Line|//Matrix|" +
						"//rdl:Textbox|//rdl:Rectangle|//rdl:Image|//rdl:Subreport|//rdl:Chart|//rdl:Table|//rdl:List|//rdl:Line|//rdl:Matrix";
				else
				{	// need to search in the namespace as well
					string mquery = query.Replace("//", "//rdl:");
					query = query + "|" + mquery;
				}

				XmlNamespaceManager nsmgr = new XmlNamespaceManager(this.rDoc.NameTable);
				nsmgr.AddNamespace("rdl", root.NamespaceURI); //default namespace
				nodeList = root.SelectNodes(query, nsmgr);
			}
			else
			{
				if (query == null)
					query = "//Textbox|//Rectangle|//Image|//Subreport|//Chart|//Table|//List|//Line|//Matrix";
				nodeList = root.SelectNodes(query);
			}

			return nodeList;
		}

		internal bool DeleteChartGrouping(XmlNode dynamic)
		{
			if (dynamic == null)
				return false;

			XmlNode seriesOrCategory = dynamic.ParentNode;
			XmlNode groupings = seriesOrCategory.ParentNode;

			groupings.RemoveChild(seriesOrCategory);	// Remove the Grouping from Groups
			if (!groupings.HasChildNodes)		// If Groups has no children
			{									//   remove the SeriesGroupings or CategoryGroupings from Chart
				XmlNode pgroupings = groupings.ParentNode;
				pgroupings.RemoveChild(groupings);
				if (!pgroupings.HasChildNodes)
				{
					pgroupings.ParentNode.RemoveChild(pgroupings);
				}
			}
			return true;
		}

		internal bool DeleteChartGrouping(XmlNode chart, string gname)
		{
			if (chart == null || chart.Name != "Chart")
				return false;

			XmlNode group;
			group = GetChartGroupName(chart, gname, "SeriesGroupings", "SeriesGrouping", "DynamicSeries");
			if (group == null)	// if not there try the row groupings
				group = GetChartGroupName(chart, gname, "CategoryGroupings", "CategoryGrouping", "DynamicCategories");

			return DeleteChartGrouping(group);
		}

		internal string[] GetChartCategoryGroupNames(XmlNode chart)
		{
			if (chart == null || chart.Name != "Chart")
				return null;

			XmlNode catGroups = this.GetNamedChildNode(chart, "CategoryGroupings");
			if (catGroups == null)
				return null;

			List<string> ar = new List<string>();
			foreach (XmlNode cgroup in catGroups.ChildNodes)
			{
				if (cgroup.Name != "CategoryGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(cgroup, "DynamicCategories","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name != null)
					ar.Add(name);
			}
			if (ar.Count <= 0)
				return null;
			return ar.ToArray();
		}

		internal string[] GetChartSeriesGroupNames(XmlNode chart)
		{
			if (chart == null || chart.Name != "Chart")
				return null;

			XmlNode serGroups = this.GetNamedChildNode(chart, "SeriesGroupings");
			if (serGroups == null)
				return null;

            List<string> ar = new List<string>();
			foreach (XmlNode sgroup in serGroups.ChildNodes)
			{
				if (sgroup.Name != "SeriesGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(sgroup, "DynamicSeries","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name != null)
					ar.Add(name);
			}
			if (ar.Count <= 0)
				return null;
			return ar.ToArray();
		}

		internal XmlNode GetChartGrouping(XmlNode chart, string gname)
		{
			if (chart == null || chart.Name != "Chart")
				return null;

			// Try the series first
			XmlNode group;
			group = GetChartGroupName(chart, gname, "SeriesGroupings", "SeriesGrouping", "DynamicSeries");
			if (group == null)	// if not there try the row groupings
				group = GetChartGroupName(chart, gname, "CategoryGroupings", "CategoryGrouping", "DynamicCategories");
			return group;
		}

		XmlNode GetChartGroupName(XmlNode chart, string gname, string search1, string search2, string search3)
		{
			XmlNode serGroups = this.GetNamedChildNode(chart, search1);
			if (serGroups == null)
				return null;

			foreach (XmlNode sgroup in serGroups.ChildNodes)
			{
				if (sgroup.Name != search2)
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(sgroup, search3,"Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name == gname)
					return group;
			}
			return null;
		}

		internal XmlNode InsertChartCategoryGrouping(XmlNode chart)
		{
			if (chart == null || chart.Name != "Chart")
				return null;

			XmlNode catGroups = this.GetCreateNamedChildNode(chart, "CategoryGroupings");
			XmlElement cgrp = rDoc.CreateElement("CategoryGrouping");
			catGroups.AppendChild(cgrp);
			XmlElement dyncats = rDoc.CreateElement("DynamicCategories");
			cgrp.AppendChild(dyncats);
			XmlElement grp = rDoc.CreateElement("Grouping");
			dyncats.AppendChild(grp);

			return dyncats;
		}

		internal XmlNode InsertChartSeriesGrouping(XmlNode chart)
		{
			if (chart == null || chart.Name != "Chart")
				return null;

			XmlNode serGroups = this.GetCreateNamedChildNode(chart, "SeriesGroupings");
			XmlElement sgrp = rDoc.CreateElement("SeriesGrouping");
			serGroups.AppendChild(sgrp);
			XmlElement dynsers = rDoc.CreateElement("DynamicSeries");
			sgrp.AppendChild(dynsers);
			XmlElement grp = rDoc.CreateElement("Grouping");
			dynsers.AppendChild(grp);

			return dynsers;
		}

		internal bool InMatrix(XmlNode node)
		{
			XmlNode pNode = node.ParentNode;
			if (pNode == null || pNode.Name != "ReportItems")
				return false;
			pNode = pNode.ParentNode;
			if (pNode == null)
				return false;

			switch (pNode.Name)
			{
				case "MatrixCell":
				case "Subtotal":
				case "DynamicRows":
				case "DynamicColumns":
				case "StaticRows":
				case "StaticColumns":
				case "Corner":
					return true;
				default:
					return false;
			}
		}

		internal XmlNode GetMatrixFromReportItem(XmlNode riNode)
		{
			XmlNode matrix;
			for (matrix = riNode.ParentNode; matrix != null; matrix = matrix.ParentNode)
			{
				if (matrix.Name == "Matrix")
					break;
			}
			return matrix;
		}

		/// <summary>
		/// Get the list of column group names given a ReportItem in a matrix
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns></returns>
		internal string[] GetMatrixColumnGroupNames(XmlNode riNode)
		{
			XmlNode matrix = GetMatrixFromReportItem(riNode);
			if (matrix == null)
				return null;

			XmlNode colGroups = this.GetNamedChildNode(matrix, "ColumnGroupings");
			if (colGroups == null)
				return null;

            List<string> ar = new List<string>();
			foreach (XmlNode cgroup in colGroups.ChildNodes)
			{
				if (cgroup.Name != "ColumnGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(cgroup, "DynamicColumns","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name != null)
					ar.Add(name);
			}
			if (ar.Count <= 0)
				return null;
			return ar.ToArray();
		}

		/// <summary>
		/// Get the list of row group names given a ReportItem in a matrix
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns></returns>
		internal string[] GetMatrixRowGroupNames(XmlNode riNode)
		{
			XmlNode matrix = GetMatrixFromReportItem(riNode);
			if (matrix == null)
				return null;

			XmlNode colGroups = this.GetNamedChildNode(matrix, "RowGroupings");
			if (colGroups == null)
				return null;

            List<string> ar = new List<string>();
			foreach (XmlNode cgroup in colGroups.ChildNodes)
			{
				if (cgroup.Name != "RowGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(cgroup, "DynamicRows","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name != null)
					ar.Add(name);
			}
			if (ar.Count <= 0)
				return null;
			return ar.ToArray();
		}

		internal XmlNode InsertMatrixColumnGroup(XmlNode node)
		{
			XmlNode matrix = this.GetMatrixFromReportItem(node);
			if (matrix == null)
				return null;

			XmlNode colGroups = this.GetCreateNamedChildNode(matrix, "ColumnGroupings");
			// ColumnGrouping in ColumnGroupings
			XmlElement cgrp = rDoc.CreateElement("ColumnGrouping");
			colGroups.AppendChild(cgrp);
			this.SetElement(cgrp, "Height", ".25in");
			XmlElement dyncols = rDoc.CreateElement("DynamicColumns");
			cgrp.AppendChild(dyncols);
			// Grouping in DynamicColumns
			XmlElement grp = rDoc.CreateElement("Grouping");
			dyncols.AppendChild(grp);

			return dyncols;
		}

		internal XmlNode InsertMatrixRowGroup(XmlNode node)
		{
			XmlNode matrix = this.GetMatrixFromReportItem(node);
			if (matrix == null)
				return null;

			XmlNode rowGroups = this.GetCreateNamedChildNode(matrix, "RowGroupings");
			// ColumnGrouping in ColumnGroupings
			XmlElement rgrp = rDoc.CreateElement("RowGrouping");
			rowGroups.AppendChild(rgrp);
			this.SetElement(rgrp, "Width", "1in");
			XmlElement dynrows = rDoc.CreateElement("DynamicRows");
			rgrp.AppendChild(dynrows);
			// Grouping in DynamicRows
			XmlElement grp = rDoc.CreateElement("Grouping");
			dynrows.AppendChild(grp);

			return dynrows;
		}

		internal bool DeleteMatrixGroup(XmlNode dynamic)
		{
			XmlNode columnOrRow = dynamic.ParentNode;
			XmlNode groupings = columnOrRow.ParentNode;

			groupings.RemoveChild(columnOrRow);	// Remove the Grouping from Groups
			if (!groupings.HasChildNodes)		// If Groups has no children
			{									//   remove the ColumnGroupings from Matrix
				groupings.ParentNode.RemoveChild(groupings);
			}
			return true;
		}

		/// <summary>
		/// Delete the matrix group (ColumnGrouping or RowGrouping) given a ReportItem in a matrix and the name of the group
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns>true if the Group is deleted</returns>
		internal bool DeleteMatrixGroup(XmlNode riNode, string gname)
		{
			XmlNode matrix=this.GetMatrixFromReportItem(riNode);
			if (matrix == null)
				return false;

			// Try the column groupings first
			XmlNode group;
			group = GetMatrixColumnGroupFromName(matrix, gname);
			if (group == null)
				group = GetMatrixRowGroupFromName(matrix, gname);
			if (group == null)
				return false;

			XmlNode dynamic = group.ParentNode;
			XmlNode columnOrRow = dynamic.ParentNode;
			XmlNode groupings = columnOrRow.ParentNode;

			groupings.RemoveChild(columnOrRow);	// Remove the Grouping from Groups
			if (!groupings.HasChildNodes)		// If Groups has no children
			{									//   remove the TableGroups from Table
				matrix.RemoveChild(groupings);
			}
			return true;
		}

		internal XmlNode GetMatrixGroup(XmlNode riNode, string gname)
		{
			XmlNode matrix=this.GetMatrixFromReportItem(riNode);
			if (matrix == null)
				return null;

			// Try the column groupings first
			XmlNode group;
			group = GetMatrixColumnGroupFromName(matrix, gname);
			if (group == null)	// if not there try the row groupings
				group = GetMatrixRowGroupFromName(matrix, gname);
			return group;
		}

		internal XmlNode GetMatrixColumnGroupFromName(XmlNode matrix, string gname)
		{
			XmlNode colGroups = this.GetNamedChildNode(matrix, "ColumnGroupings");
			if (colGroups == null)
				return null;

			foreach (XmlNode cgroup in colGroups.ChildNodes)
			{
				if (cgroup.Name != "ColumnGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(cgroup, "DynamicColumns","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name == gname)
					return group;
			}
			return null;
		}

		internal XmlNode GetMatrixRowGroupFromName(XmlNode matrix, string gname)
		{
			XmlNode rowGroups = this.GetNamedChildNode(matrix, "RowGroupings");
			if (rowGroups == null)
				return null;

			foreach (XmlNode cgroup in rowGroups.ChildNodes)
			{
				if (cgroup.Name != "RowGrouping")
					continue;
				XmlNode group = DesignXmlDraw.FindNextInHierarchy(cgroup, "DynamicRows","Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name == gname)
					return group;
			}
			return null;
		}

		internal XmlNode GetTableFromReportItem(XmlNode riNode)
		{
			XmlNode table;
			for (table = riNode.ParentNode; table != null; table = table.ParentNode)
			{
				if (table.Name == "Table" || table.Name == "fyi:Grid")
					break;
			}
			return table;
		}
		/// <summary>
		/// Return TableCell that contains the specified reportitem
		/// </summary>
		/// <param name="node">ReportItem in a table row</param>
		/// <returns>null if not found</returns>
		internal XmlNode GetTableCell(XmlNode node)
		{
			// find the table cell
			XmlNode tcNode;
			for (tcNode = node.ParentNode; tcNode != null; tcNode = tcNode.ParentNode)
			{
				if (tcNode.Name == "TableCell")
					break;
			}
			return tcNode;
		}

		/// <summary>
		/// Return Table column that contains the specified reportitem
		/// </summary>
		/// <param name="node">ReportItem in a table row</param>
		/// <returns>null if not found</returns>
		internal XmlNode GetTableColumn(XmlNode node)
		{
			// find the table cell
			XmlNode tcNode = GetTableCell(node);
			if (tcNode == null)
				return null;

			// Get the table
            XmlNode table = GetTableFromReportItem(tcNode);
			if (table == null)
				return null;

			int col = GetTableColumnNumber(tcNode);

			XmlNode tcs = this.GetNamedChildNode(table, "TableColumns");
			if (tcs == null)
				return null;

			XmlNode savetc=null;
			foreach (XmlNode tc in tcs.ChildNodes)
			{
				if (tc.Name != "TableColumn")
					continue;

				if (col < 1)
				{
					savetc = tc;
					break;
				}
				col--;
			}
			return savetc;
		}

		/// <summary>
		/// Return TableRow that contains the specified reportitem
		/// </summary>
		/// <param name="node">ReportItem in a table row</param>
		/// <returns>null if not found</returns>
		internal XmlNode GetTableRow(XmlNode node)
		{
			// find the tablerow
			XmlNode trNode;
			for (trNode = node.ParentNode; trNode != null; trNode = trNode.ParentNode)
			{
				if (trNode.Name == "TableRow")
					break;
			}
			return trNode;
		}
		
		/// <summary>
		/// Delete the Table column that contains the specified reportitem
		/// </summary>
		/// <param name="node">ReportItem in a table row</param>
		/// <returns>true if deleted</returns>
		internal bool DeleteTableColumn(XmlNode node)
		{
			// find the table cell
			XmlNode tcNode;
			for (tcNode = node.ParentNode; tcNode != null; tcNode = tcNode.ParentNode)
			{
				if (tcNode.Name == "TableCell")
					break;
			}
			if (tcNode == null)
				return false;

			// Get the table
            XmlNode table = GetTableFromReportItem(tcNode);
			if (table == null)
				return false;

			if (GetTableColumnCount(table) <= 1)	// We're deleting the last column?
			{
				this.DeleteReportItem(table);		// yes; just get rid of the table
				return true;
			}

			// calculate the column number of this node
			int col = GetTableColumnNumber(tcNode);
			DeleteTableColumn(this.GetNamedChildNode(table, "TableColumns"), col);
			DeleteTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Header", "TableRows"), col);
			DeleteTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Details", "TableRows"), col);
			DeleteTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Footer", "TableRows"), col);
			XmlNode tGroups = this.GetNamedChildNode(table, "TableGroups");
			if (tGroups == null)
				return true;
			// run thru the table groups
			foreach (XmlNode tgrp in tGroups.ChildNodes)
			{
				if (tgrp.Name != "TableGroup")
					continue;
				DeleteTableColumn(DesignXmlDraw.FindNextInHierarchy(tgrp, "Header", "TableRows"), col);
				DeleteTableColumn(DesignXmlDraw.FindNextInHierarchy(tgrp, "Footer", "TableRows"), col);
			}

			return true;
		}

		private void DeleteTableColumn(XmlNode sNode, int col)
		{
			if (sNode == null)
				return;

			if (sNode.Name == "TableRows")
			{
				// Loop thru all the tablerows to get at the TableCells
				foreach (XmlNode tr in sNode.ChildNodes)
				{
					if (tr.Name == "TableRow")
						DeleteTableColumn(this.GetNamedChildNode(tr, "TableCells"), col);
				}
				return;
			}

			// We have either TableCells or TableColumns
			string name = "TableCell";
			if (sNode.Name == "TableColumns")
				name = "TableColumn";

			XmlNode del=null;
			foreach (XmlNode cNode in sNode.ChildNodes)
			{
				if (cNode.Name != name)
					continue;
				int colSpan = 1;
				if (name == "TableCell")
					colSpan = Convert.ToInt32(GetElementValue(cNode, "ColSpan", "1"));

				if (col - colSpan < 0)
				{
					if (colSpan == 1)
						del = cNode;
					else
						this.SetElement(cNode, "ColSpan", (colSpan - 1).ToString());
					break;
				}
				col -= colSpan;
			}
			if (del == null)
				return;
			sNode.RemoveChild(del);
			return;
		}

		private int GetTableColumnNumber(XmlNode tcell)
		{
			int col=0;
			XmlNode tcells = tcell.ParentNode;
			foreach (XmlNode cell in tcells.ChildNodes)
			{
				if (cell.Name != "TableCell")
					continue;
				if (cell == tcell)
					break;
				int colSpan = Convert.ToInt32(GetElementValue(cell, "ColSpan", "1"));
				col += colSpan;
			}
			return col;
		}
		
		/// <summary>
		/// Delete the TableRow that contains the specified reportitem
		/// </summary>
		/// <param name="node">ReportItem in a table row</param>
		/// <returns>true if deleted</returns>
		internal bool DeleteTableRow(XmlNode node)
		{
			XmlNode trNode;
			for (trNode = node.ParentNode; trNode != null; trNode = trNode.ParentNode)
			{
				if (trNode.Name == "TableRow")
					break;
			}
			if (trNode == null)
				return false;

			XmlNode trows = trNode.ParentNode;
			trows.RemoveChild(trNode);

			// If that was the last TableRow in TableRows we need to delete TableRows as well
			if (this.GetNamedChildNode(trows, "TableRow") != null)
				return true;		// we have another tablerow in this section
			XmlNode section = trows.ParentNode;  //  get the TableRows parent
			switch (section.Name)
			{
				case "Footer":
				case "Details":
				case "Header":
					break;
				default:			// anything other than footer, details, header doesn't require tablerows
					return true;
			}
			XmlNode table = section.ParentNode;	// get the parent
			table.RemoveChild(section);
			if (this.GetNamedChildNode(table, "Details") != null ||
				this.GetNamedChildNode(table, "Footer") != null ||
				this.GetNamedChildNode(table, "Header") != null)
				return true;

			// Also need to delete the table since no header, footer or detail
			this.DeleteReportItem(table);
			return true;
		}

		/// <summary>
		/// Delete the table group given a ReportItem in a table and the name of the group
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns>true if the TableGroup is deleted</returns>
		internal bool DeleteTableGroup(XmlNode riNode, string gname)
		{
			XmlNode table=this.GetTableFromReportItem(riNode);
			if (table == null)
				return false;

			XmlNode tblGroups = this.GetNamedChildNode(table, "TableGroups");
			if (tblGroups == null)
				return false;

			XmlNode tblGroup=null;
			foreach (XmlNode tgroup in tblGroups.ChildNodes)
			{
				if (tgroup.Name != "TableGroup")
					continue;
				XmlNode group = this.GetNamedChildNode(tgroup, "Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name == gname)
				{
					tblGroup = tgroup;
					break;
				}
			}

			if (tblGroup == null)
				return false;

			tblGroups.RemoveChild(tblGroup);	// Remove the TableGroup from TableGroups
			if (!tblGroups.HasChildNodes)		// If TableGroups has no children
			{									//   remove the TableGroups from Table
				table.RemoveChild(tblGroups);
			}
			return true;
		}

		/// <summary>
		/// Get the XmlNode of the table group given a ReportItem in a table and the name of the group
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns></returns>
		internal XmlNode GetTableGroup(XmlNode riNode, string gname)
		{
            XmlNode table = GetTableFromReportItem(riNode);
			if (table == null)
				return null;

			XmlNode tblGroups = this.GetNamedChildNode(table, "TableGroups");
			if (tblGroups == null)
				return null;

			foreach (XmlNode tgroup in tblGroups.ChildNodes)
			{
				if (tgroup.Name != "TableGroup")
					continue;
				XmlNode group = this.GetNamedChildNode(tgroup, "Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name == gname)
					return tgroup;
			}

			return null;
		}

		/// <summary>
		/// Get the list of table group names given a ReportItem in a table
		/// </summary>
		/// <param name="riNode"></param>
		/// <returns></returns>
		internal string[] GetTableGroupNames(XmlNode riNode)
		{
            XmlNode table = GetTableFromReportItem(riNode);
			if (table == null)
				return null;

			XmlNode tblGroups = this.GetNamedChildNode(table, "TableGroups");
			if (tblGroups == null)
				return null;

            List<string> ar = new List<string>();
			foreach (XmlNode tgroup in tblGroups.ChildNodes)
			{
				if (tgroup.Name != "TableGroup")
					continue;
				XmlNode group = this.GetNamedChildNode(tgroup, "Grouping");
				if (group == null)
					continue;
				string name = this.GetElementAttribute(group, "Name", null);
				if (name != null)
					ar.Add(name);
			}
			if (ar.Count <= 0)
				return null;
			return ar.ToArray();
		}

		/// <summary>
		/// Insert a table column before or after the column containing the specified ReportItem
		/// </summary>
		/// <param name="node">The reportitem node</param>
		/// <param name="before">If true row is inserted before this column otherwise it will go after.</param>
		/// <returns>true if the column was inserted</returns>
		internal bool InsertTableColumn(XmlNode node, bool before)
		{
			// find the table cell
			XmlNode tcNode;
			for (tcNode = node.ParentNode; tcNode != null; tcNode = tcNode.ParentNode)
			{
				if (tcNode.Name == "TableCell")
					break;
			}
			if (tcNode == null)
				return false;

			// Get the table
            XmlNode table = GetTableFromReportItem(tcNode);
			if (table == null)
				return false;

			// Get the table column 
			XmlNode refCol=null;
			int col = GetTableColumnNumber(tcNode);
			int ci=0;
			XmlNode tableColumns = this.GetNamedChildNode(table, "TableColumns");
			foreach (XmlNode tbCol in tableColumns.ChildNodes)
			{
				if (tbCol.Name != "TableColumn")
					continue;
				if (ci == col)
				{
					refCol = tbCol;
					break;
				}
				ci++;
			}
			if (refCol == null)
				return false;

			// insert the tablecolumn
			XmlElement newcol = rDoc.CreateElement("TableColumn");
			if (before)
				tableColumns.InsertBefore(newcol, refCol);
			else
				tableColumns.InsertAfter(newcol, refCol);
			this.SetElement(newcol, "Width", this.GetElementValue(refCol, "Width", "1in"));

			InsertTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Header", "TableRows"), col, before);
			InsertTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Details", "TableRows"), col, before);
			InsertTableColumn(DesignXmlDraw.FindNextInHierarchy(table, "Footer", "TableRows"), col, before);
			XmlNode tGroups = this.GetNamedChildNode(table, "TableGroups");
			if (tGroups == null)
				return true;
			// run thru the table groups
			foreach (XmlNode tgrp in tGroups.ChildNodes)
			{
				if (tgrp.Name != "TableGroup")
					continue;
				InsertTableColumn(DesignXmlDraw.FindNextInHierarchy(tgrp, "Header", "TableRows"), col, before);
				InsertTableColumn(DesignXmlDraw.FindNextInHierarchy(tgrp, "Footer", "TableRows"), col, before);
			}

			return true;
		}

		private void InsertTableColumn(XmlNode tcells, int col, bool before)
		{
			if (tcells == null)
				return;

			if (tcells.Name == "TableRows")
			{
				// Loop thru all the tablerows to get at the TableCells
				foreach (XmlNode tr in tcells.ChildNodes)
				{
					if (tr.Name == "TableRow")
						InsertTableColumn(this.GetNamedChildNode(tr, "TableCells"), col, before);
				}
				return;
			}

			// We have TableCells
			XmlNode refCell=null;
			foreach (XmlNode cNode in tcells.ChildNodes)
			{
				if (cNode.Name != "TableCell")
					continue;
				
				int	colSpan = Convert.ToInt32(GetElementValue(cNode, "ColSpan", "1"));

				if (col - colSpan < 0)
				{
					if (colSpan == 1 || (col == 0 && before))   // insert new column if very first and before
						refCell = cNode;						//   or if no colSpan has been requested
					else
						this.SetElement(cNode, "ColSpan", (colSpan + 1).ToString());
					break;
				}
				col -= colSpan;
			}
			if (refCell == null)
				return;

			InsertTableColumn(tcells, refCell, before);
		}

		private void InsertTableColumn(XmlNode tcells, XmlNode refCell, bool before)
		{
			// insert the TableCell
			XmlElement ntcell = rDoc.CreateElement("TableCell");
			if (before)
				tcells.InsertBefore(ntcell, refCell);
			else
				tcells.InsertAfter(ntcell, refCell);

			// ReportItems in TableCell
			XmlElement ris = rDoc.CreateElement("ReportItems");
			ntcell.AppendChild(ris);
			// TextBox in ReportItems
			XmlElement tbox = rDoc.CreateElement("Textbox");
			ReportNames.GenerateName(tbox);
			ris.AppendChild(tbox);

			XmlElement vnode = rDoc.CreateElement("Value");
			vnode.InnerText = "";
			tbox.AppendChild(vnode);
			// Copy style info if refCell contains a Textbox
			XmlNode styleNode = DesignXmlDraw.FindNextInHierarchy(refCell, "ReportItems", "Textbox", "Style");
			if (styleNode != null)
				tbox.AppendChild(styleNode.CloneNode(true));

			return;
		}
		/// <summary>
		/// Creates a new TableGroup and put it on the end of the chain
		/// </summary>
		/// <param name="node">ReportItem this is contained in the table</param>
		/// <returns>XmlNode of the new TableGroup</returns>
		internal XmlNode InsertTableGroup(XmlNode node)
		{
			XmlNode table = this.GetTableFromReportItem(node);
			if (table == null)
				return null;

			XmlNode tblGroups = this.GetCreateNamedChildNode(table, "TableGroups");
			// TableGroup in TableGroups
			XmlElement tgrp = rDoc.CreateElement("TableGroup");
			tblGroups.AppendChild(tgrp);
			// Grouping in TableGroup
			XmlElement grp = rDoc.CreateElement("Grouping");
			tgrp.AppendChild(grp);

			return tgrp;
		}

		/// <summary>
		/// Deletes the passed TableGroup; if last TableGroup in TableGroups then TableGroups also deleted
		/// </summary>
		/// <param name="node">XmlNode of TableGroup</param>
		internal void DeleteTableGroup(XmlNode tgrp)
		{
			if (tgrp == null || tgrp.Name != "TableGroup")	// make sure we have valid arguments
				return;
			XmlNode tblGroups = tgrp.ParentNode;

			tblGroups.RemoveChild(tgrp);
			if (this.GetNamedChildNode(tblGroups, "TableGroup") == null)
			{	// this was the last tablegroup
				XmlNode table = tblGroups.ParentNode;
				table.RemoveChild(tblGroups);
			}
		}

		/// <summary>
		/// Insert a table row before or after the TableRow containing the specified ReportItem
		/// </summary>
		/// <param name="node">The reportitem node</param>
		/// <param name="before">If true row is inserted before this TableRow otherwise it will go after.</param>
		/// <returns>true if the TableRow was inserted</returns>
		internal bool InsertTableRow(XmlNode node, bool before)
		{
			XmlNode trNode;
			for (trNode = node.ParentNode; trNode != null; trNode = trNode.ParentNode)
			{
				if (trNode.Name == "TableRow")
					break;
			}
			if (trNode == null)
				return false;

			XmlNode tcells = this.GetNamedChildNode(trNode, "TableCells");
			if (tcells == null)
				return false;

			XmlNode trows = trNode.ParentNode;
			XmlElement newrow = rDoc.CreateElement("TableRow");
			if (before)
				trows.InsertBefore(newrow, trNode);
			else
				trows.InsertAfter(newrow, trNode);

			XmlElement height = rDoc.CreateElement("Height");
			// use same height as reference row if possible
			XmlNode cheight = this.GetNamedChildNode(trNode, "Height");
			if (cheight != null)
				height.InnerText = cheight.InnerText;
			else
				height.InnerText = ".25in";
			newrow.AppendChild(height);
			XmlElement tablecells = rDoc.CreateElement("TableCells");
			newrow.AppendChild(tablecells);

			// loop thru the TableCell children of the reference tablerow;
			//  create a textbox for each column
			foreach (XmlNode tcell in tcells.ChildNodes)
			{
				if (tcell.Name != "TableCell")
					continue;
				XmlElement ntcell = rDoc.CreateElement("TableCell");
				tablecells.AppendChild(ntcell);
				XmlElement ris = rDoc.CreateElement("ReportItems");
				ntcell.AppendChild(ris);
				
				// Need to create a column for each spaned column
				int colSpan = Convert.ToInt32(GetElementValue(tcell, "ColSpan", "1"));

                // add ColSpan to match the source row
                if (colSpan > 1)
                {
                    XmlElement colSpanNode = rDoc.CreateElement("ColSpan");
                    colSpanNode.InnerText = colSpan.ToString();
                    ntcell.AppendChild(colSpanNode);
                }

                XmlNode styleNode = DesignXmlDraw.FindNextInHierarchy(tcell, "ReportItems", "Textbox", "Style");
                XmlElement tbox = rDoc.CreateElement("Textbox");
                ReportNames.GenerateName(tbox);
                ris.AppendChild(tbox);

                XmlElement vnode = rDoc.CreateElement("Value");
                vnode.InnerText = "";
                tbox.AppendChild(vnode);
                if (styleNode != null)
                    tbox.AppendChild(styleNode.CloneNode(true));

            }

			return true;
		}

		/// <summary>
		/// Insert a table row given a TableRows; uses TableColumns to create proper number of rows
		/// </summary>
		/// <param name="node">The reportitem node</param>
		/// <returns>true if the TableRow was inserted</returns>
		internal bool InsertTableRow(XmlNode tblRows)
		{
			if (tblRows == null)
				return false;

            XmlNode table = GetTableFromReportItem(tblRows);
			if (table == null)
				return false;

			XmlNode columns = this.GetNamedChildNode(table, "TableColumns");
			if (columns == null)
				return false;

			XmlElement newrow = this.CreateElement(tblRows, "TableRow", null);
			this.CreateElement(newrow, "Height", ".2in");			

			XmlElement tablecells = this.CreateElement(newrow, "TableCells", null);

			// loop thru the TableColumns children
			//  create a textbox for each column
			foreach (XmlNode col in columns.ChildNodes)
			{
				if (col.Name != "TableColumn")
					continue;
				XmlElement ntcell = this.CreateElement(tablecells, "TableCell", null);
				XmlElement ris = this.CreateElement(ntcell, "ReportItems", null);
				XmlElement tbox = this.CreateElement(ris, "Textbox", null);
				ReportNames.GenerateName(tbox);
				XmlElement style = this.CreateElement(tbox, "Style", null);
				XmlElement bstyle = this.CreateElement(style, "BorderStyle", null);
				this.SetElement(bstyle, "Default", "Solid");
				this.SetElement(tbox, "Value", "");
			}

			return true;
		}

		internal bool InPageHeaderOrFooter(XmlNode node)
		{
			for (XmlNode p = node; p != null; p = p.ParentNode)
			{
				if (node.Name == "PageHeader" || node.Name == "PageFooter")
					return true;
			}
			return false;
		}

		/// <summary>
		/// Checks to see if node is part of a table
		/// </summary>
		/// <param name="node">Usually a ReportItem</param>
		/// <returns>true if part of a table</returns>
		internal bool InTable(XmlNode node)
		{
			XmlNode pNode = node.ParentNode;
			if (pNode == null || pNode.Name != "ReportItems")
				return false;
			pNode = pNode.ParentNode;
			if (pNode == null || pNode.Name != "TableCell")
				return false;
			else
				return true;
		}

        /// <summary>
        /// Checks to see if node is part of a grid
        /// </summary>
        /// <param name="node">Usually a ReportItem</param>
        /// <returns>true if part of a Grid</returns>
        internal bool InGrid(XmlNode node)
        {
            for (XmlNode pNode = node.ParentNode; pNode != null; pNode = pNode.ParentNode)
            {
                if (pNode.Name == "fyi:Grid")
                    return true;
                if (pNode.Name == "Rectangle" || pNode.Name == "Table" || pNode.Name=="Matrix" || pNode.Name=="List")
                    return false;
            }
            return false;
        }

		/// <summary>
		/// Returns the node of the parent table (or matrix) or null
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		internal XmlNode TMParent(XmlNode node)
		{
			for (XmlNode pNode = node.ParentNode; pNode != null; pNode = pNode.ParentNode)
			{
				if (pNode.Name == "Table" || pNode.Name == "Matrix" || pNode.Name == "fyi:Grid")
					return pNode;
                if (pNode.Name == "Rectangle")
                    return null;
			}
			return null;
		}

		private int PixelsX(float x)		// points to pixels
		{
			return (int) (x * DpiX / POINTSIZED);
		}

		private int PixelsY(float y)
		{
			return (int) (y * DpiY / POINTSIZED);
		}

        private float PointsX(float x)		// pixels to points
        {
            return x * POINTSIZED / DpiX;
        }

        private float PointsY(float y)
        {
            return y * POINTSIZED / DpiY;
        }

		/// <summary>
		/// Return the name of the section where item is draw		
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public string SectionBelongsTo(XmlNode node)
		{
            var myList = new List<string>() { "PageHeader", "Body", "PageFooter" };
			node = node.ParentNode;
			string Name = node.Name;
			while(!myList.Any(x => x.Contains(Name)))
			{
				node = node.ParentNode;
				Name=node.Name;
			}
			return Name;
        }

		/// <summary>
		/// Return the Width of the node container
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public float WidthOfContainer(XmlNode node)
		{
			node = node.ParentNode.ParentNode;
			string Name = node.Name;
			var myList = new List<string>() { "PageHeader", "Body", "PageFooter" };
			if (myList.Any(x => x.Contains(Name)))
			{
					return pWidth-lMargin-rMargin;
			}
			//
			// Is not a section , but is a container for selection
			// Determine the Width
			//
			return GetRectangle(node).Width;	
		}
		/// <summary>
		/// Return then Height of node container
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public float HeightOfContainer(XmlNode node)
		{
			node = node.ParentNode.ParentNode;
			string Name = node.Name;
			switch (Name) {
				case "Body":
					return 0;
				case "PageHeader":
					return PageHeaderHeight;
				case "PageFooter":
					return PageFooterHeight;
				default:
					return GetRectangle(node).Height;
			}
		}















	}

	internal enum HitLocationEnum
	{
		Inside,
		// The following can only occur on a selected item
		Move,
		TopLeft,
		TopMiddle,
		TopRight,
		BottomLeft,
		BottomMiddle,
		BottomRight,
		LeftMiddle,
		RightMiddle,
		TableColumnResize,
		TableRowResize,
		LineLeft,
		LineRight
	}
	internal class HitLocation
	{
		internal XmlNode HitNode;
		internal XmlNode HitContainer;
		internal HitLocationEnum HitSpot=HitLocationEnum.Inside;
		internal PointF HitRelative;		// x,y location of object relative to first container; ie page header, body, page footer, list, rectangle
		internal HitLocation (XmlNode hn, XmlNode hc, HitLocationEnum location, PointF offset)
		{
			HitNode = hn;
			HitContainer = hc;
			HitSpot = location;
			HitRelative = offset;
		}
	}
	internal class ReportItemSorter: IComparer<XmlNode>
	{
		DesignXmlDraw _Draw;
		internal ReportItemSorter(DesignXmlDraw d)
		{
			_Draw = d;
		}

		#region IComparer Members

		public int Compare(XmlNode x, XmlNode y)
		{
			int xi = Convert.ToInt32(_Draw.GetElementValue(x, "ZIndex", "0"));
			int yi = Convert.ToInt32(_Draw.GetElementValue(y, "ZIndex", "0"));

			return xi-yi;
		}

		#endregion

	}

    internal class ReportItemReverseSorter : IComparer<XmlNode>
    {
        DesignXmlDraw _Draw;
        [System.Diagnostics.DebuggerStepThrough]
        internal ReportItemReverseSorter(DesignXmlDraw d)
        {
            _Draw = d;
        }

        #region IComparer Members

        [System.Diagnostics.DebuggerStepThrough]
        public int Compare(XmlNode x, XmlNode y)
        {
            int xi = Convert.ToInt32(_Draw.GetElementValue(x, "ZIndex", "0"));
            int yi = Convert.ToInt32(_Draw.GetElementValue(y, "ZIndex", "0"));

            return yi - xi;
        }

        #endregion

    } 
}
