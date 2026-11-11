using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// An RDL 2008+ Tablix with column groups (or another shape a Table cannot run), read and
	/// laid out by the engine itself (RDLParser.NativeTablix, #427 phase 2).
	///</summary>
	/// <remarks>
	/// Both hierarchies are taken as they are: any mix of static members, groups and details, at
	/// any depth, on either axis. At run time each hierarchy is expanded against the data into its
	/// instances -- a static member once, a group once per group value, details once per row --
	/// and every body cell is evaluated over the rows of its row instance that are also in its
	/// column instance. A member's TablixHeader is a header cell spanning its instances; the
	/// TablixCorner fills the top left. The grid is then paged and rendered by the Matrix runtime,
	/// so every renderer handles it as it does a Matrix.
	/// </remarks>
	[Serializable]
	internal class TablixPivot : Matrix
	{
		/// <summary>Holds what a member or cell owns, so its expressions find the pivot as their region.</summary>
		private sealed class Link : ReportLink
		{
			internal Link(ReportDefn r, ReportLink p) : base(r, p) { }
		}

		private sealed class Member
		{
			internal Grouping Grouping;			// a group with expressions
			internal bool IsDetails;			// a group without: one instance per row
			internal Sorting Sorting;
			internal Visibility Visibility;
			internal ReportItems Header;		// TablixHeader contents
			internal RSize HeaderSize;
			internal int HeaderLevel = -1;
			internal List<Member> Children;		// null for a leaf
			internal int Leaf = -1;				// the body row or column a leaf stands for
		}

		private sealed class Axis
		{
			internal List<Member> Members = new List<Member>();
			internal int Leaves;
			internal int HeaderLevels;
			internal List<Grouping> Groupings = new List<Grouping>();
		}

		// A group instance a cell is in, and the ones outside it.
		private sealed class Chain
		{
			internal Grouping Grouping;
			internal BitArray Bits;
			internal Rows Rows;
			internal Chain Parent;
		}

		// An expanded leaf of a hierarchy, or a header over a run of them.
		private sealed class Instance
		{
			internal Member Member;
			internal BitArray Bits;
			internal Chain Chain;
			internal int First, Count;			// headers: the leaf instances they span
		}

		private sealed class CellEntry : MatrixCellEntry
		{
			internal Chain RowChain, ColumnChain;
			internal CellEntry(Rows d, ReportItem ri) : base(d, ri) { }
		}

		private sealed class CornerCell
		{
			internal int Row, Column, ColSpan;
			internal ReportItems Items;
		}

		static int _emptyCells;

		Axis _Rows = new Axis();
		Axis _Columns = new Axis();
		List<RSize> _RowHeights = new List<RSize>();
		List<RSize> _ColumnWidths = new List<RSize>();
		ReportItems[,] _Body;
		int[,] _BodySpan;
		List<CornerCell> _Corner = new List<CornerCell>();
		List<ReportLink> _Parts = new List<ReportLink>();		// everything to FinalPass

		internal TablixPivot(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode, true)
		{
			XmlNode body = null, columnHierarchy = null, rowHierarchy = null, corner = null;
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "tablixbody": body = xNodeLoop; break;
					case "tablixcolumnhierarchy": columnHierarchy = xNodeLoop; break;
					case "tablixrowhierarchy": rowHierarchy = xNodeLoop; break;
					case "tablixcorner": corner = xNodeLoop; break;
					// Interactive only, or a layout option paginated output does not need.
					case "repeatcolumnheaders":
					case "repeatrowheaders":
					case "fixedcolumnheaders":
					case "fixedrowheaders":
					case "groupsbeforerowheaders":
					case "layoutdirection":
						break;
					default:
						if (DataRegionElement(xNodeLoop))
							break;
						OwnerReport.rl.LogError(4, "Unknown Tablix element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			DataRegionFinish();
			if (body == null)
			{
				OwnerReport.rl.LogError(8, $"Tablix '{Name?.Nm}' has no TablixBody.");
				return;
			}

			foreach (XmlNode column in ChildrenNamed(Child(body, "TablixColumns"), "TablixColumn"))
				_ColumnWidths.Add(Size(Child(column, "Width"), "TablixColumn requires the Width element."));
			List<XmlNode> rows = ChildrenNamed(Child(body, "TablixRows"), "TablixRow");
			foreach (XmlNode row in rows)
				_RowHeights.Add(Size(Child(row, "Height"), "TablixRow requires the Height element."));

			ReadAxis(_Columns, Child(columnHierarchy, "TablixMembers"));
			ReadAxis(_Rows, Child(rowHierarchy, "TablixMembers"));
			if (_Columns.Leaves != _ColumnWidths.Count || _Rows.Leaves != _RowHeights.Count)
			{
				OwnerReport.rl.LogError(8, $"Tablix '{Name?.Nm}' has {_RowHeights.Count} row(s) and {_ColumnWidths.Count} column(s) " +
					$"but {_Rows.Leaves} row and {_Columns.Leaves} column hierarchy leaf member(s).");
				return;
			}

			_Body = new ReportItems[_RowHeights.Count, _ColumnWidths.Count];
			_BodySpan = new int[_RowHeights.Count, _ColumnWidths.Count];
			for (int i = 0; i < rows.Count; i++)
			{
				int c = 0;	// each TablixCell is a column; one without contents is covered by a span
				foreach (XmlNode cell in ChildrenNamed(Child(rows[i], "TablixCells"), "TablixCell"))
				{
					XmlNode contents = Child(cell, "CellContents");
					if (contents != null && c < _ColumnWidths.Count)
					{
						_Body[i, c] = Contents(contents);
						_BodySpan[i, c] = Math.Max(1, Math.Min(Span(contents, "ColSpan"), _ColumnWidths.Count - c));
					}
					c++;
				}
			}

			int cornerRow = 0;
			foreach (XmlNode cornerRowNode in ChildrenNamed(Child(corner, "TablixCornerRows"), "TablixCornerRow"))
			{
				int column = 0;
				foreach (XmlNode cornerCell in ChildrenNamed(cornerRowNode, "TablixCornerCell"))
				{
					XmlNode contents = Child(cornerCell, "CellContents");
					if (contents != null)
						_Corner.Add(new CornerCell { Row = cornerRow, Column = column, ColSpan = Math.Max(1, Span(contents, "ColSpan")), Items = Contents(contents) });
					column++;
				}
				cornerRow++;
			}
		}

		#region Definition

		private RSize Size(XmlNode node, string missing)
		{
			if (node != null)
				return new RSize(OwnerReport, node);
			OwnerReport.rl.LogError(8, missing);
			return new RSize(OwnerReport, "0in");
		}

		private static int Span(XmlNode contents, string name)
		{
			XmlNode n = Child(contents, name);
			return n == null ? 1 : XmlUtil.Integer(n.InnerText);
		}

		/// <summary>A cell's report item, in a MatrixCell of the pivot; a blank textbox when it has none.</summary>
		private ReportItems Contents(XmlNode contents)
		{
			bool hasItem = false;
			foreach (XmlNode n in contents.ChildNodes)
				if (n.NodeType == XmlNodeType.Element && n.Name != "ColSpan" && n.Name != "RowSpan")
					hasItem = true;
			XmlNode items = contents;
			if (!hasItem)
			{
				XmlDocument doc = contents.OwnerDocument;
				string ns = contents.NamespaceURI;
				XmlElement textbox = doc.CreateElement("Textbox", ns);
				textbox.SetAttribute("Name", "TablixPivotEmpty" + System.Threading.Interlocked.Increment(ref _emptyCells));
				textbox.AppendChild(doc.CreateElement("Value", ns));
				items = doc.CreateElement("CellContents", ns);
				items.AppendChild(textbox);
			}
			// A MatrixCell, as renderers and nested regions recognise a matrix's cells by it.
			var cell = new MatrixCell(OwnerReport, this);
			cell.ReportItems = new ReportItems(OwnerReport, cell, items, cellContents: true);
			_Parts.Add(cell);
			return cell.ReportItems;
		}

		private void ReadAxis(Axis axis, XmlNode tablixMembers)
		{
			int leaf = 0, levels = 0;
			axis.Members = ReadMembers(axis, tablixMembers, 0, ref leaf, ref levels);
			axis.Leaves = leaf;
			axis.HeaderLevels = levels;
		}

		private List<Member> ReadMembers(Axis axis, XmlNode tablixMembers, int level, ref int leaf, ref int levels)
		{
			var members = new List<Member>();
			foreach (XmlNode node in ChildrenNamed(tablixMembers, "TablixMember"))
			{
				var m = new Member();
				var link = new Link(OwnerReport, this);
				XmlNode group = Child(node, "Group");
				if (group != null)
				{
					if (Child(group, "GroupExpressions") != null)
					{
						m.Grouping = new Grouping(OwnerReport, link, group);
						axis.Groupings.Add(m.Grouping);
						_Parts.Add(m.Grouping);
					}
					else
						m.IsDetails = true;
				}
				XmlNode sort = Child(node, "SortExpressions");
				if (sort != null)
					_Parts.Add(m.Sorting = new Sorting(OwnerReport, link, sort));
				XmlNode visibility = Child(node, "Visibility");
				if (visibility != null)
					_Parts.Add(m.Visibility = new Visibility(OwnerReport, link, visibility));

				int inner = level;
				XmlNode header = Child(node, "TablixHeader");
				if (header != null)
				{
					m.HeaderSize = Size(Child(header, "Size"), "TablixHeader requires the Size element.");
					XmlNode contents = Child(header, "CellContents");
					m.Header = contents != null ? Contents(contents) : Contents(header.OwnerDocument.CreateElement("CellContents", header.NamespaceURI));
					m.HeaderLevel = level;
					inner = level + 1;
					levels = Math.Max(levels, inner);
				}

				XmlNode nested = Child(node, "TablixMembers");
				if (nested != null)
					m.Children = ReadMembers(axis, nested, inner, ref leaf, ref levels);
				else
					m.Leaf = leaf++;
				members.Add(m);
			}
			return members;
		}

		async override internal Task FinalPass()
		{
			if (Height == null)
			{	// as designed: the column header levels and the body rows
				float h = 0;
				foreach (RSize rs in _RowHeights)
					h += rs.Points;
				h += HeaderHeights(_Columns.Members);
				Height = new RSize(OwnerReport, h.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo) + "pt");
			}
			await base.FinalPass().ConfigureAwait(false);
			foreach (ReportLink part in _Parts)
				await part.FinalPass().ConfigureAwait(false);
		}

		private static float HeaderHeights(List<Member> members)
		{
			float most = 0;
			foreach (Member m in members)
			{
				float h = (m.HeaderSize?.Points ?? 0) + (m.Children == null ? 0 : HeaderHeights(m.Children));
				most = Math.Max(most, h);
			}
			return most;
		}

		internal override IEnumerable<Grouping> AllGroupings()
		{
			foreach (Grouping g in _Rows.Groupings)
				yield return g;
			foreach (Grouping g in _Columns.Groupings)
				yield return g;
		}

		private protected override int HeaderRowCount => _Columns.HeaderLevels;

		#endregion

		#region Run

		internal override async Task<(MatrixCellEntry[,], int numRows, int numCols)> RunBuild(Report rpt)
		{
			Rows full = GetFullData(rpt);
			int n = full.Data.Count;
			var all = new BitArray(n, true);

			var columns = new List<Instance>();
			var columnHeaders = new List<Instance>();
			await Expand(rpt, full, _Columns.Members, all, null, columns, columnHeaders).ConfigureAwait(false);
			var rows = new List<Instance>();
			var rowHeaders = new List<Instance>();
			await Expand(rpt, full, _Rows.Members, all, null, rows, rowHeaders).ConfigureAwait(false);

			int C = _Columns.HeaderLevels, R = _Rows.HeaderLevels;
			int maxRows = C + rows.Count, maxColumns = R + columns.Count;
			var grid = new MatrixCellEntry[maxRows, maxColumns];
			Rows RowsOf(BitArray bits) => new Rows(rpt, full, 0, n - 1, bits);
			float Width(int first, int count)
			{
				float w = 0;
				for (int i = first; i < first + count && i < columns.Count; i++)
					w += _ColumnWidths[columns[i].Member.Leaf].Points;
				return w;
			}
			float Height(int row) => _RowHeights[rows[row].Member.Leaf].Points;

			// The corner: as wide as the row header levels below it, as tall as the column header levels.
			float[] levelWidths = new float[R], levelHeights = new float[C];
			foreach (Instance h in rowHeaders)
				levelWidths[h.Member.HeaderLevel] = Math.Max(levelWidths[h.Member.HeaderLevel], h.Member.HeaderSize.Points);
			foreach (Instance h in columnHeaders)
				levelHeights[h.Member.HeaderLevel] = Math.Max(levelHeights[h.Member.HeaderLevel], h.Member.HeaderSize.Points);
			foreach (CornerCell cc in _Corner)
			{
				if (cc.Row >= C || cc.Column >= R)
					continue;
				int span = Math.Min(cc.ColSpan, R - cc.Column);
				float w = 0;
				for (int l = cc.Column; l < cc.Column + span; l++)
					w += levelWidths[l];
				grid[cc.Row, cc.Column] = new CellEntry(RowsOf(all), cc.Items.Items[0]) { ColSpan = span, Width = w, Height = levelHeights[cc.Row] };
			}

			foreach (Instance h in columnHeaders)
				grid[h.Member.HeaderLevel, R + h.First] = new CellEntry(RowsOf(h.Bits), h.Member.Header.Items[0])
				{ ColSpan = h.Count, Width = Width(h.First, h.Count), Height = h.Member.HeaderSize.Points, ColumnChain = h.Chain };

			foreach (Instance h in rowHeaders)
				grid[C + h.First, h.Member.HeaderLevel] = new CellEntry(RowsOf(h.Bits), h.Member.Header.Items[0])
				{ Width = h.Member.HeaderSize.Points, Height = Height(h.First), RowChain = h.Chain };

			for (int ri = 0; ri < rows.Count; ri++)
			{
				Instance row = rows[ri];
				for (int ci = 0; ci < columns.Count; ci++)
				{
					Instance column = columns[ci];
					ReportItems items = _Body[row.Member.Leaf, column.Member.Leaf];
					if (items == null)
						continue;	// covered by a span
					int span = Math.Min(_BodySpan[row.Member.Leaf, column.Member.Leaf], columns.Count - ci);
					var bits = ((BitArray)row.Bits.Clone()).And(column.Bits);
					grid[C + ri, R + ci] = new CellEntry(RowsOf(bits), items.Items[0])
					{ ColSpan = span, Width = Width(ci, span), Height = Height(ri), RowChain = row.Chain, ColumnChain = column.Chain };
					ci += span - 1;
				}
			}
			return (grid, maxRows, maxColumns);
		}

		/// <summary>
		/// Expands members over the rows in bits: a static member once, a group once per value, the
		/// details once per row. Leaves go to leaves; a member with a header, once it has any, to headers.
		/// </summary>
		private async Task Expand(Report rpt, Rows full, List<Member> members, BitArray bits, Chain chain,
			List<Instance> leaves, List<Instance> headers)
		{
			foreach (Member m in members)
			{
				var parts = new List<(BitArray Bits, Chain Chain)>();
				if (m.Grouping != null)
				{
					foreach (BitArray part in await Partition(rpt, full, m, bits).ConfigureAwait(false))
						parts.Add((part, new Chain { Grouping = m.Grouping, Bits = part, Parent = chain }));
				}
				else if (m.IsDetails)
				{
					for (int i = 0; i < bits.Length; i++)
					{
						if (!bits[i])
							continue;
						var one = new BitArray(bits.Length);
						one[i] = true;
						parts.Add((one, chain));
					}
				}
				else
					parts.Add((bits, chain));

				foreach (var (part, partChain) in parts)
				{
					if (m.Visibility != null && await Hidden(rpt, full, m, part, partChain).ConfigureAwait(false))
						continue;
					int first = leaves.Count;
					if (m.Children != null)
						await Expand(rpt, full, m.Children, part, partChain, leaves, headers).ConfigureAwait(false);
					else
						leaves.Add(new Instance { Member = m, Bits = part, Chain = partChain });
					if (m.Header != null && leaves.Count > first)
						headers.Add(new Instance { Member = m, Bits = part, Chain = partChain, First = first, Count = leaves.Count - first });
				}
			}
		}

		private static int FirstSet(BitArray bits)
		{
			for (int i = 0; i < bits.Length; i++)
				if (bits[i])
					return i;
			return -1;
		}

		/// <summary>Points the group, and the cell data, at the rows of an instance, for evaluating over it.</summary>
		private Row Enter(Report rpt, Rows full, BitArray bits, Chain chain)
		{
			var rows = new Rows(rpt, full, 0, full.Data.Count - 1, bits);
			SetMyData(rpt, rows);
			for (Chain c = chain; c != null; c = c.Parent)
			{
				c.Rows ??= new Rows(rpt, full, 0, full.Data.Count - 1, c.Bits);
				c.Grouping.SetRows(rpt, c.Rows);
				c.Grouping.SetInScope(rpt, true);
			}
			return rows.Data.Count > 0 ? rows.Data[0] : null;
		}

		private async Task<bool> Hidden(Report rpt, Rows full, Member m, BitArray bits, Chain chain)
		{
			Row row = Enter(rpt, full, bits, chain);
			return await m.Visibility.IsHidden(rpt, row).ConfigureAwait(false);
		}

		/// <summary>The group's instances among bits, ordered by its sort expressions, else by value.</summary>
		private async Task<List<BitArray>> Partition(Report rpt, Rows full, Member m, BitArray bits)
		{
			var keys = new List<string>();
			var byKey = new Dictionary<string, BitArray>();
			var sb = new StringBuilder();
			for (int i = 0; i < bits.Length; i++)
			{
				if (!bits[i])
					continue;
				sb.Clear();
				foreach (GroupExpression ge in m.Grouping.GroupExpressions.Items)
				{
					string v = await ge.Expression.EvaluateString(rpt, full.Data[i]).ConfigureAwait(false);
					sb.Append(string.IsNullOrEmpty(v) ? "￾" : v).Append('￿');	// as Matrix keys its groups
				}
				string key = sb.ToString();
				if (!byKey.TryGetValue(key, out BitArray part))
				{
					byKey[key] = part = new BitArray(bits.Length);
					keys.Add(key);
				}
				part[i] = true;
			}

			if (m.Sorting == null || keys.Count < 2)
			{
				keys.Sort((a, b) => a.CompareTo(b));
				return keys.ConvertAll(k => byKey[k]);
			}

			var sortBy = m.Sorting.Items;
			var sortKeys = new Dictionary<string, object[]>();
			foreach (string key in keys)
			{
				BitArray part = byKey[key];
				Row row = Enter(rpt, full, part, new Chain { Grouping = m.Grouping, Bits = part });
				var values = new object[sortBy.Count];
				for (int k = 0; k < sortBy.Count; k++)
					values[k] = await sortBy[k].SortExpression.Evaluate(rpt, row).ConfigureAwait(false);
				sortKeys[key] = values;
			}
			keys.Sort((a, b) =>
			{
				for (int k = 0; k < sortBy.Count; k++)
				{
					int rc = Filter.ApplyCompare(sortBy[k].SortExpression.Expr.GetTypeCode(), sortKeys[a][k], sortKeys[b][k]);
					if (rc != 0)
						return sortBy[k].Direction == SortDirectionEnum.Ascending ? rc : -rc;
				}
				return a.CompareTo(b);
			});
			return keys.ConvertAll(k => byKey[k]);
		}

		private protected override void SetGroupingValues(Report rpt, MatrixCellEntry mce)
		{
			// A group the cell is not inside of is totalled over: it sees the cell's rows.
			foreach (Grouping g in AllGroupings())
			{
				g.SetRows(rpt, mce.Data);
				g.SetInScope(rpt, false);
			}
			if (!(mce is CellEntry e))
				return;
			Rows full = GetFullData(rpt);
			foreach (Chain chain in new[] { e.RowChain, e.ColumnChain })
			{
				for (Chain c = chain; c != null; c = c.Parent)
				{
					c.Rows ??= new Rows(rpt, full, 0, full.Data.Count - 1, c.Bits);
					c.Grouping.SetRows(rpt, c.Rows);
					c.Grouping.SetInScope(rpt, true);
				}
			}
		}

		private protected override void ResetInScope(Report rpt)
		{
			foreach (Grouping g in AllGroupings())
				g.SetInScope(rpt, true);
		}

		#endregion

		#region Xml

		private static XmlNode Child(XmlNode parent, string localName)
		{
			if (parent == null)
				return null;
			foreach (XmlNode n in parent.ChildNodes)
				if (n.NodeType == XmlNodeType.Element && n.LocalName == localName)
					return n;
			return null;
		}

		private static List<XmlNode> ChildrenNamed(XmlNode parent, string localName)
		{
			var list = new List<XmlNode>();
			if (parent == null)
				return list;
			foreach (XmlNode n in parent.ChildNodes)
				if (n.NodeType == XmlNodeType.Element && n.LocalName == localName)
					list.Add(n);
			return list;
		}

		#endregion
	}
}
