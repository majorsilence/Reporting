

using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	///  Definition of footer rows for a table or group.
	///</summary>
	[Serializable]
	internal class Footer : ReportLink
	{
		TableRows _TableRows;	// The footer rows for the table or group
		bool _RepeatOnNewPage;	// Indicates this footer should be displayed on
								// each page that the table (or group) is displayed		
		int _BottomRows;		// The last rows of a table's footer that print at the foot of
								// every page the table is on (see Table.FooterRowsAtBottomOfPage)
	
		internal Footer(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_TableRows=null;
			_RepeatOnNewPage=false;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableRows":
						_TableRows = new TableRows(r, this, xNodeLoop);
						break;
					case "RepeatOnNewPage":
						_RepeatOnNewPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Footer element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_TableRows == null)
				OwnerReport.rl.LogError(8, "TableRows element is required with a Footer but not specified.");
		}
		
		async override internal Task FinalPass()
		{
            await _TableRows.FinalPass().ConfigureAwait(false);
			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
            await _TableRows.Run(ip, row).ConfigureAwait(false);
			return;
		}

		internal async Task RunPage(Pages pgs, Row row)
		{

			Page p = pgs.CurrentPage;
			if (p.YOffset + await HeightOfRows(pgs, row).ConfigureAwait(false) > pgs.BottomOfPage)
			{
                await OwnerTable.RunPageBottomRows(pgs, row, false).ConfigureAwait(false);
				p = OwnerTable.RunPageNew(pgs, p);
                await OwnerTable.RunPageHeader(pgs, row, false, null).ConfigureAwait(false);
			}
			if (_BottomRows == 0)
                await _TableRows.RunPage(pgs, row).ConfigureAwait(false);
			else
                await RunPageRows(pgs, row, 0, _TableRows.Items.Count - _BottomRows).ConfigureAwait(false);

			return;
		}

		// The bottom rows, which the table prints at the foot of each page itself.
		internal async Task RunPageBottomRows(Pages pgs, Row row)
		{
            await RunPageRows(pgs, row, _TableRows.Items.Count - _BottomRows, _TableRows.Items.Count).ConfigureAwait(false);
		}

		private async Task RunPageRows(Pages pgs, Row row, int start, int end)
		{
			for (int i = start; i < end; i++)
			{
				TableRow tr = _TableRows.Items[i];
                await tr.HeightOfRow(pgs, row).ConfigureAwait(false);	// sets the height the row is drawn at
                await tr.RunPage(pgs, row).ConfigureAwait(false);
			}
		}

		internal TableRows TableRows
		{
			get { return  _TableRows; }
			set {  _TableRows = value; }
		}

		// With bottom rows, the height of the rows above them: the rows that print once, after
		// the table's last row.
		internal async Task<float> HeightOfRows(Pages pgs, Row r)
		{
			if (_BottomRows > 0)
				return await HeightOfRows(pgs, r, 0, _TableRows.Items.Count - _BottomRows).ConfigureAwait(false);
			return await _TableRows.HeightOfRows(pgs, r).ConfigureAwait(false);
		}

		internal async Task<float> HeightOfBottomRows(Pages pgs, Row r)
		{
			return await HeightOfRows(pgs, r, _TableRows.Items.Count - _BottomRows, _TableRows.Items.Count).ConfigureAwait(false);
		}

		private async Task<float> HeightOfRows(Pages pgs, Row r, int start, int end)
		{
			float height = 0;
			for (int i = start; i < end; i++)
				height += await _TableRows.Items[i].HeightOfRow(pgs, r).ConfigureAwait(false);
			return height;
		}

		internal int BottomRows
		{
			get { return _BottomRows; }
			set { _BottomRows = value; }
		}

		internal bool RepeatOnNewPage
		{
			get { return  _RepeatOnNewPage; }
			set {  _RepeatOnNewPage = value; }
		}

		internal Table OwnerTable
		{
			get 
			{ 
				ReportLink rl = this.Parent;
				while (rl != null)
				{
					if (rl is Table)
						return rl as Table;
					rl = rl.Parent;
				}
				return null;
			}
		}
	}
}
