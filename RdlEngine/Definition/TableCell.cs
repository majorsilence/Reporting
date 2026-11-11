

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableCell definition and processing.
	///</summary>
	[Serializable]
	internal class TableCell : ReportLink
	{
		ReportItems _ReportItems;	// An element of the report layout (e.g. List, Textbox,
									// Line). This ReportItems collection must contain
									// exactly one ReportItem. The Top, Left, Height and
									// Width for this ReportItem are ignored. The
									// position is taken to be 0, 0 and the size to be 100%,
									// 100%. Pagebreaks on report items inside a
									// TableCell are ignored.
		int _ColSpan;	// Indicates the number of columns this cell spans.1
						// A ColSpan of 1 is the same as not specifying a ColSpan	
		int _RowSpan;	// Indicates the number of rows, in the same TableRows, this cell spans.
						// The positions it covers in the rows below have no TableCell of their own.

		// some bookkeeping fields
		Table _OwnerTable;			// Table that owns this column
		int _ColIndex;				// Column number within table; used for
									//    xrefing with other parts of table columns; e.g. column headers with details
		bool _InTableHeader;		// true if tablecell is part of header; simplifies HTML processing
		bool _InTableFooter;		// true if tablecell is part of footer; simplifies HTML processing
	
		internal TableCell(ReportDefn r, ReportLink p, XmlNode xNode, int colIndex) : base(r, p)
		{
			_ColIndex = colIndex;
			_ReportItems=null;
			_ColSpan=1;
			_RowSpan=1;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "ReportItems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					case "ColSpan":
						_ColSpan = XmlUtil.Integer(xNodeLoop.InnerText);
						break;
					case "RowSpan":
						_RowSpan = Math.Max(1, XmlUtil.Integer(xNodeLoop.InnerText));
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableCell element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			Finish();
		}

		static int _emptyCells;		// names the blank textboxes of empty Tablix cells

		/// <summary>
		/// A Tablix cell from its RDL 2008+ CellContents: the report item, and the ColSpan and
		/// RowSpan beside it. A cell left empty holds a blank textbox, as a table cell must hold
		/// one item.
		/// </summary>
		internal TableCell(ReportDefn r, ReportLink p, XmlNode cellContents, int colIndex, bool tablix) : base(r, p)
		{
			_ColIndex = colIndex;
			_ColSpan = 1;
			_RowSpan = 1;
			bool hasItem = false;
			foreach (XmlNode xNodeLoop in cellContents.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "ColSpan":
						_ColSpan = Math.Max(1, XmlUtil.Integer(xNodeLoop.InnerText));
						break;
					case "RowSpan":
						_RowSpan = Math.Max(1, XmlUtil.Integer(xNodeLoop.InnerText));
						break;
					default:
						hasItem = true;
						break;
				}
			}
			XmlNode items = cellContents;
			if (!hasItem)
			{
				XmlDocument doc = cellContents.OwnerDocument;
				string ns = cellContents.NamespaceURI;
				XmlElement textbox = doc.CreateElement("Textbox", ns);
				textbox.SetAttribute("Name", "TablixEmptyCell" + System.Threading.Interlocked.Increment(ref _emptyCells));
				textbox.AppendChild(doc.CreateElement("Value", ns));
				items = doc.CreateElement("CellContents", ns);
				items.AppendChild(textbox);
			}
			_ReportItems = new ReportItems(r, this, items, cellContents: true);
			Finish();
		}

		private void Finish()
		{
			// Must have exactly one ReportItems
			if (_ReportItems == null)
				OwnerReport.rl.LogError(8, "ReportItems element is required with a TableCell but not specified.");
			else if (_ReportItems.Items.Count != 1)
				OwnerReport.rl.LogError(8, "Only one element in ReportItems element is allowed within a TableCell.");

			// Obtain the tablecell's owner table;
			//		determine if tablecell is part of table header
			_InTableHeader = false;
			ReportLink rl;
			for (rl=this.Parent; rl != null; rl=rl.Parent)
			{
				if (rl is Table)
				{
					_OwnerTable = (Table) rl;
					break;
				}
				
				if (rl is Header && rl.Parent is Table)	// Header and parent is Table (not TableGroup)
				{
					_InTableHeader=true;
				}
				
				if (rl is Footer && rl.Parent is Table)	// Header and parent is Table (not TableGroup)
				{
					_InTableFooter=true;
				}
			}
			return;
		}
		
		async override internal Task FinalPass()
		{
            await _ReportItems.FinalPass().ConfigureAwait(false);
			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			// todo: this doesn't account for the affect of colspan correctly
			//   where if any of the spanned columns are visible the value would show??
			TableColumn tc = _OwnerTable.TableColumns[_ColIndex];
			if (await tc.IsHidden(ip.Report(), row).ConfigureAwait(false))	// column visible?
				return;													//  no nothing to do

			await ip.TableCellStart(this, row).ConfigureAwait(false);

            await _ReportItems.Items[0].Run(ip, row).ConfigureAwait(false);

			ip.TableCellEnd(this, row);
			return;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{
			// todo: this doesn't account for the affect of colspan correctly
			//   where if any of the spanned columns are visible the value would show??
			TableColumn tc = _OwnerTable.TableColumns[_ColIndex];
			if (await tc.IsHidden(pgs.Report, row).ConfigureAwait(false))	// column visible?
				return;                                                 //  no nothing to do

            await _ReportItems.Items[0].RunPage(pgs, row).ConfigureAwait(false);
			return;
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}

		internal Table OwnerTable
		{
			get { return _OwnerTable; }
		}

		internal int ColSpan
		{
			get { return  _ColSpan; }
			set {  _ColSpan = value; }
		}

		internal int ColIndex
		{
			get { return  _ColIndex; }
			set { _ColIndex = value; }	// TableRows moves a cell past the columns spanned from rows above
		}

		/// <summary>The number of rows this cell spans, counting its own; 1 when it spans none.</summary>
		internal int RowSpan
		{
			get { return _RowSpan; }
			set { _RowSpan = Math.Max(1, value); }
		}

		/// <summary>The row the cell is in (for a cell spanning rows, the first of them).</summary>
		internal TableRow Row
		{
			get { return Parent?.Parent as TableRow; }
		}

		/// <summary>
		/// The height the cell is laid out at: its row's, or for a cell spanning rows the
		/// rows' together. Each row's HeightOfRow must already have been called for the row.
		/// </summary>
		internal float HeightCalc(Report rpt)
		{
			TableRow tr = Row;
			if (_RowSpan <= 1 || !(tr?.Parent is TableRows trs))
				return tr == null ? 0 : tr.HeightCalc(rpt);
			int first = trs.Items.IndexOf(tr);
			int last = Math.Min(trs.Items.Count, first + _RowSpan);
			float height = 0;
			for (int i = first; i < last; i++)
				height += trs.Items[i].HeightCalc(rpt);
			return height;
		}

		internal bool InTableFooter
		{
			get { return  _InTableFooter; }
		}

		internal bool InTableHeader
		{
			get { return  _InTableHeader; }
		}
	}
}
