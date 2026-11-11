
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of specific reportitems (e.g. TextBoxs, Images, ...)
	///</summary>
	[Serializable]
    internal class ReportItems : ReportLink, IEnumerable
	{
		List<ReportItem> _Items;				// list of report items

		internal ReportItems(ReportDefn r, ReportLink p, XmlNode xNode) : this(r, p, xNode, false)
		{
		}

		/// <param name="cellContents">xNode is an RDL 2008+ CellContents, whose ColSpan and RowSpan
		/// belong to the cell holding it.</param>
		internal ReportItems(ReportDefn r, ReportLink p, XmlNode xNode, bool cellContents) : base(r, p)
		{
			ReportItem ri;
            _Items = new List<ReportItem>();

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "rectangle":
						ri = new Rectangle(r, this, xNodeLoop);
						break;
					case "line":
						ri = new Line(r, this, xNodeLoop);
						break;
					case "textbox":
						ri = new Textbox(r, this, xNodeLoop);
						break;
					case "image":
						ri = new Image(r, this, xNodeLoop);
						break;
					case "subreport":
						ri = new Subreport(r, this, xNodeLoop);
						break;
					// DataRegions: list, table, matrix, chart
					case "list": 
						ri = new List(r, this, xNodeLoop);
						break;
					case "table":
                    case "grid":
                    case "fyi:grid":
                        ri = new Table(r, this, xNodeLoop);
						break;
					case "matrix": 
						ri = new Matrix(r, this, xNodeLoop);
						break;
					case "chart": 
						ri = new Chart(r, this, xNodeLoop);
						break;
					case "tablix":		// RDL 2008+: run natively when the parser asks and the shape allows;
										// any other one left here is one the normaliser refused
						string runner = r.NativeTablix && xNodeLoop is XmlElement tx ? Rdl2008Normalizer.RunsNatively(tx) : null;
						if (runner == null)
							goto default;	// one the normaliser refused (and said why)
						if (runner == "Tablix")
							ri = new Tablix(r, this, xNodeLoop);
						else
							ri = new TablixPivot(r, this, xNodeLoop);
						break;
					case "colspan":
					case "rowspan":
						if (!cellContents)
							goto default;
						continue;
					case "chartexpression":		// For internal use only 
						ri = new ChartExpression(r, this, xNodeLoop);
						break;
                    case "customreportitem":
                        ri = new CustomReportItem(r, this, xNodeLoop);
                        break;
					default:
						ri=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ReportItems element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (ri != null)
				{
					_Items.Add(ri);
				}
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "At least one item must be in the ReportItems.");
			else
                _Items.TrimExcess();
		}

        internal ReportItem this[int i]
        {
            get
            {
                return _Items[i];
            }
        }
		
		async override internal Task FinalPass()
		{
			foreach (ReportItem ri in _Items)
			{		
				await ri.FinalPass().ConfigureAwait(false); 
			}
			// Sort on y, x (see ReportItem compare routine). OrderBy is stable where List.Sort is not:
			// items at the same top and left keep the report's order, which decides the one the items
			// below them are placed from and the one drawn over the other. List.Sort ordered them
			// by chance once there were more than 16 items, and adding or removing an unrelated item
			// could swap them.
			List<ReportItem> sorted = _Items.OrderBy(ri => ri).ToList();
			_Items.Clear();
			_Items.AddRange(sorted);

            for (int i = 0; i < _Items.Count; i++)
            {
                ReportItem ri = _Items[i];
                ri.PositioningFinalPass(i, _Items);
            }
            //foreach (ReportItem ri in _Items)	
            //    ri.PositioningFinalPass(_Items);

			return;
		}

		internal async Task Run(IPresent ip, Row row)
		{
			foreach (ReportItem ri in _Items)
			{
				await ri.Run(ip, row).ConfigureAwait(false);
			}
			return;
		}

		internal async Task RunPage(Pages pgs, Row row, float xOffset)
		{
			SetXOffset(pgs.Report, xOffset);
			foreach (ReportItem ri in _Items)
			{
                await ri.RunPage(pgs, row).ConfigureAwait(false);
			}
			return;
		}

        internal List<ReportItem> Items
		{
			get { return  _Items; }
		}

		internal float GetXOffset(Report rpt)
		{
			OFloat of = rpt.Cache.Get(this, "xoffset") as OFloat;
			return of == null? 0: of.f;
		}

		internal void SetXOffset(Report rpt, float f)
		{
			OFloat of = rpt.Cache.Get(this, "xoffset") as OFloat;
			if (of == null)
				rpt.Cache.Add(this, "xoffset", new OFloat(f));
			else
				of.f = f;
		}

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return _Items.GetEnumerator();
        }

        #endregion
    }
}
