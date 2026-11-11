using System;
using System.Collections.Generic;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// An RDL 2008+ Tablix, read by the engine itself rather than rewritten into a 2005 Table
	/// first (RDLParser.NativeTablix, #427 phase 2).
	///</summary>
	/// <remarks>
	/// So far only the table shape runs natively -- no column groups, and not the one-cell List
	/// shape -- and it runs on the Table runtime: the row hierarchy becomes the table's header,
	/// groups, details and footer, and a row group's TablixHeader becomes a leading column, the
	/// same plan Rdl2008Normalizer makes for a Table. The other shapes are still rewritten by the
	/// normaliser before parsing (Rdl2008Normalizer.BecomesTable decides which).
	/// </remarks>
	[Serializable]
	internal class Tablix : Table
	{
		/// <summary>A TablixMember: a group, or a static row or column, with the members inside it.</summary>
		private sealed class Member
		{
			internal XmlNode Group;					// null for a static member
			internal bool HasGroupExpressions;		// a Group without them is the details
			internal XmlNode Header;				// TablixHeader: a row group's label
			internal List<Member> Children;			// null for a leaf
			internal XmlNode Visibility;
			internal XmlNode SortExpressions;
			internal bool RepeatOnNewPage;
			internal bool AlwaysHidden;				// hidden with no toggle to show it
		}

		/// <summary>
		/// One level of a grouped row hierarchy: the static rows above and below, and either a
		/// nested group or the detail rows (Rdl2008Normalizer.RowLevel).
		/// </summary>
		private sealed class RowLevel
		{
			internal List<int> Header = new List<int>();
			internal List<int> Footer = new List<int>();
			internal List<Member> HeaderMembers = new List<Member>();
			internal Member GroupMember;
			internal RowLevel Inner;
			internal Member DetailsMember;
			internal List<int> Details = new List<int>();
			internal bool Body;
			internal Dictionary<int, Member> Members = new Dictionary<int, Member>();
		}

		private enum RowPlacement { Header, Detail, Footer }

		List<XmlNode> _Rows;				// the TablixRows
		List<List<XmlNode>> _Cells;			// each row's cells, the hoisted row headers first

		internal Tablix(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode, true)
		{
			XmlNode body = null, columnHierarchy = null, rowHierarchy = null, sortExpressions = null;
			bool repeatRowHeaders = false;

			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "tablixbody":
						body = xNodeLoop;
						break;
					case "tablixcolumnhierarchy":
						columnHierarchy = xNodeLoop;
						break;
					case "tablixrowhierarchy":
						rowHierarchy = xNodeLoop;
						break;
					case "sortexpressions":
						sortExpressions = xNodeLoop;
						break;
					case "repeatrowheaders":
						repeatRowHeaders = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					// The corner sits over row headers of column groups; a table shape has none.
					case "tablixcorner":
					// Interactive only: frozen panes and repeated column headers describe a
					// scrolling viewport, which paginated output does not have.
					case "repeatcolumnheaders":
					case "fixedcolumnheaders":
					case "fixedrowheaders":
						break;
					default:
						if (DataRegionElement(xNodeLoop))	// try at DataRegion level
							break;
						OwnerReport.rl.LogError(4, "Unknown Tablix element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}

			if (body == null)
				OwnerReport.rl.LogError(8, $"Tablix '{Name?.Nm}' has no TablixBody.");
			else
				Build(body, columnHierarchy, rowHierarchy, sortExpressions, repeatRowHeaders);
			FinishTable("Tablix");
		}

		private void Build(XmlNode body, XmlNode columnHierarchy, XmlNode rowHierarchy, XmlNode tablixSort, bool repeatRowHeaders)
		{
			_Rows = ChildrenNamed(Child(body, "TablixRows"), "TablixRow");
			List<Member> rowMembers = ReadMembers(Child(rowHierarchy, "TablixMembers"));
			List<Member> columnMembers = ReadMembers(Child(columnHierarchy, "TablixMembers"));

			int levels = HoistRowHeaders(rowMembers, out var headerCells, out var headerSizes);
			ReadCells(levels, headerCells);
			BuildColumns(Child(body, "TablixColumns"), columnMembers, levels, headerSizes);

			RowLevel grouped = PlanGroupedRows(rowMembers);
			if (grouped != null)
			{
				BuildGroupedSections(grouped, repeatRowHeaders, tablixSort);
				return;
			}

			var placements = ClassifyRows(rowMembers, out List<Member> leaves);
			if (placements == null)
				return;
			Member MemberOf(int i) => i < leaves.Count ? leaves[i] : null;

			var header = new Header(OwnerReport, this, repeatRowHeaders);
			header.TableRows = Rows(header, Indexes(placements, RowPlacement.Header), MemberOf);
			if (header.TableRows != null)
				Header = header;

			var details = new Details(OwnerReport, this);
			details.TableRows = Rows(details, Indexes(placements, RowPlacement.Detail), MemberOf);
			if (details.TableRows != null)
			{
				// 2008 hangs sorting off the data region, 2005 off the detail rows it sorts.
				if (tablixSort != null)
					details.Sorting = new Sorting(OwnerReport, details, tablixSort);
				Details = details;
			}

			var footer = new Footer(OwnerReport, this);
			footer.TableRows = Rows(footer, Indexes(placements, RowPlacement.Footer), MemberOf);
			if (footer.TableRows != null)
				Footer = footer;
		}

		#region Members

		private static List<Member> ReadMembers(XmlNode tablixMembers)
		{
			var members = new List<Member>();
			foreach (XmlNode node in ChildrenNamed(tablixMembers, "TablixMember"))
			{
				var m = new Member
				{
					Group = Child(node, "Group"),
					Header = Child(node, "TablixHeader"),
					Visibility = Child(node, "Visibility"),
					SortExpressions = Child(node, "SortExpressions"),
					RepeatOnNewPage = string.Equals(Child(node, "RepeatOnNewPage")?.InnerText?.Trim(), "true", StringComparison.OrdinalIgnoreCase),
				};
				m.HasGroupExpressions = Child(m.Group, "GroupExpressions") != null;
				m.AlwaysHidden = m.Visibility != null
					&& string.Equals(Child(m.Visibility, "Hidden")?.InnerText?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
					&& Child(m.Visibility, "ToggleItem") == null;
				XmlNode nested = Child(node, "TablixMembers");
				if (nested != null)
					m.Children = ReadMembers(nested);
				members.Add(m);
			}
			return members;
		}

		private static int LeafCount(Member m)
		{
			if (m.Children == null)
				return 1;
			int count = 0;
			foreach (Member c in m.Children)
				count += LeafCount(c);
			return count;
		}

		private static bool HasExpressionGroup(List<Member> members)
		{
			foreach (Member m in members)
				if (m.HasGroupExpressions || (m.Children != null && HasExpressionGroup(m.Children)))
					return true;
			return false;
		}

		#endregion

		#region Row headers and cells

		/// <summary>
		/// A row group's label is in a TablixHeader to the left of the body. Each header level is
		/// a leading column: the label in the first of its member's rows, the others blank.
		/// Returns the number of levels; none when the hierarchy does not match the rows.
		/// </summary>
		private int HoistRowHeaders(List<Member> rowMembers, out Dictionary<(int Row, int Column), XmlNode> cells,
			out Dictionary<int, XmlNode> sizes)
		{
			var c = new Dictionary<(int, int), XmlNode>();
			var s = new Dictionary<int, XmlNode>();
			int leaf = 0, levels = 0;

			void Walk(List<Member> members, int column)
			{
				foreach (Member m in members)
				{
					int inner = column, firstLeaf = leaf;
					if (m.Header != null)
					{
						if (!s.ContainsKey(column))
							s[column] = Child(m.Header, "Size");
						levels = Math.Max(levels, column + 1);
						inner = column + 1;
					}
					if (m.Children != null)
						Walk(m.Children, inner);
					else
						leaf++;
					if (m.Header != null)
						c[(firstLeaf, column)] = Child(m.Header, "CellContents");
				}
			}

			Walk(rowMembers, 0);
			cells = c;
			sizes = s;
			return leaf == _Rows.Count ? levels : 0;
		}

		private void ReadCells(int levels, Dictionary<(int Row, int Column), XmlNode> headerCells)
		{
			_Cells = new List<List<XmlNode>>();
			for (int r = 0; r < _Rows.Count; r++)
			{
				var cells = new List<XmlNode>();
				for (int c = 0; c < levels; c++)
					cells.Add(headerCells.TryGetValue((r, c), out XmlNode contents) && contents != null
						? contents : _Rows[r].OwnerDocument.CreateElement("CellContents", _Rows[r].NamespaceURI));
				// A TablixCell without CellContents is a position an earlier cell's ColSpan covers.
				foreach (XmlNode cell in ChildrenNamed(Child(_Rows[r], "TablixCells"), "TablixCell"))
				{
					XmlNode contents = Child(cell, "CellContents");
					if (contents != null)
						cells.Add(contents);
				}
				_Cells.Add(cells);
			}
		}

		private void BuildColumns(XmlNode tablixColumns, List<Member> columnMembers, int levels, Dictionary<int, XmlNode> headerSizes)
		{
			var columns = new TableColumns(OwnerReport, this);
			for (int c = 0; c < levels; c++)
			{
				XmlNode size = headerSizes.TryGetValue(c, out XmlNode s) ? s : null;
				columns.Items.Add(new TableColumn(OwnerReport, columns,
					size != null ? new RSize(OwnerReport, size) : new RSize(OwnerReport, "0in"), null));
			}
			// 2008 hangs a column's visibility on its hierarchy member.
			int index = 0;
			foreach (XmlNode column in ChildrenNamed(tablixColumns, "TablixColumn"))
			{
				XmlNode width = Child(column, "Width");
				XmlNode visibility = Child(column, "Visibility")
					?? (index < columnMembers.Count ? columnMembers[index].Visibility : null);
				index++;
				if (width == null)
				{
					OwnerReport.rl.LogError(8, "TablixColumn requires the Width element.");
					continue;
				}
				columns.Items.Add(new TableColumn(OwnerReport, columns, new RSize(OwnerReport, width), visibility));
			}
			if (columns.Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TablixColumns at least one TablixColumn is required.");
			else
				TableColumns = columns;
		}

		/// <summary>The rows at indexes, under parent; null when there are none.</summary>
		private TableRows Rows(ReportLink parent, List<int> indexes, Func<int, Member> memberOf)
		{
			if (indexes.Count == 0)
				return null;
			var rows = new TableRows(OwnerReport, parent);
			foreach (int i in indexes)
			{
				// 2008 hangs a row's visibility (a hidden total, a drill-down) on its member.
				XmlNode visibility = Child(_Rows[i], "Visibility") ?? memberOf(i)?.Visibility;
				rows.Add(new TableRow(OwnerReport, rows, Child(_Rows[i], "Height"), visibility, _Cells[i]));
			}
			rows.Complete();
			return rows;
		}

		private static List<int> Indexes(List<RowPlacement> placements, RowPlacement wanted)
		{
			var list = new List<int>();
			for (int i = 0; i < placements.Count; i++)
				if (placements[i] == wanted)
					list.Add(i);
			return list;
		}

		#endregion

		#region Flat rows

		/// <summary>
		/// Each row is header, detail or footer: a row inside any group repeats per record (so a
		/// details group nesting a stack of static rows is a "card"); static rows outside the
		/// groups are header before them and footer after.
		/// </summary>
		private List<RowPlacement> ClassifyRows(List<Member> rowMembers, out List<Member> leaves)
		{
			var placements = new List<RowPlacement>();
			var leafMembers = new List<Member>();
			bool seenDetail = false;

			void Walk(List<Member> members, bool insideGroup)
			{
				foreach (Member m in members)
				{
					bool inGroup = insideGroup || m.Group != null;
					if (m.Children != null)
					{
						Walk(m.Children, inGroup);
						continue;
					}
					leafMembers.Add(m);
					if (inGroup)
					{
						placements.Add(RowPlacement.Detail);
						seenDetail = true;
					}
					else
						placements.Add(seenDetail ? RowPlacement.Footer : RowPlacement.Header);
				}
			}

			Walk(rowMembers, false);
			leaves = leafMembers;
			if (placements.Count != _Rows.Count)
			{
				OwnerReport.rl.LogError(8, $"Tablix '{Name?.Nm}' has {_Rows.Count} row(s) but {placements.Count} " +
					"row hierarchy leaf member(s).");
				return null;
			}
			return placements;
		}

		#endregion

		#region Grouped rows

		/// <summary>
		/// A row hierarchy that is a single chain of groups: static rows, then one group (itself
		/// the same shape, or a lone row per group instance) or the detail rows, then static rows.
		/// Null for any other shape.
		/// </summary>
		private RowLevel PlanGroupedRows(List<Member> top)
		{
			if (top.Count == 0 || !HasExpressionGroup(top))
				return null;
			int next = 0;
			RowLevel level = ParseLevel(top, ref next);
			return level != null && next == _Rows.Count && level.GroupMember != null ? level : null;
		}

		private static RowLevel ParseLevel(List<Member> members, ref int next)
		{
			var level = new RowLevel();
			return ParseMembers(members, level, ref next) && level.Body ? level : null;
		}

		private static bool ParseMembers(List<Member> members, RowLevel level, ref int next)
		{
			foreach (Member m in members)
			{
				if (m.Group == null)
				{
					if (m.Children != null)
					{	// a row-header level: the members it wraps belong to this level
						if (!ParseMembers(m.Children, level, ref next))
							return false;
						continue;
					}
					level.Members[next] = m;
					(level.Body ? level.Footer : level.Header).Add(next++);
					if (!level.Body)
						level.HeaderMembers.Add(m);
					continue;
				}

				if (level.Body)
				{	// a second group beside the first renders only when it can never show
					if (!m.AlwaysHidden)
						return false;
					next += LeafCount(m);
					continue;
				}
				level.Body = true;

				if (m.HasGroupExpressions)
				{
					level.GroupMember = m;
					if (m.Children == null)
					{	// one row per group instance
						level.Inner = new RowLevel { Body = true };
						level.Inner.Members[next] = m;
						level.Inner.Header.Add(next++);
					}
					else
					{
						level.Inner = ParseLevel(m.Children, ref next);
						if (level.Inner == null)
							return false;
					}
				}
				else
				{	// details: its static children are the rows repeated per record
					level.DetailsMember = m;
					if (m.Children == null)
					{
						level.Members[next] = m;
						level.Details.Add(next++);
					}
					else
					{
						foreach (Member child in m.Children)
						{
							if (child.Group != null || child.Children != null)
								return false;
							level.Members[next] = child;
							level.Details.Add(next++);
						}
					}
				}
			}
			return true;
		}

		private void BuildGroupedSections(RowLevel top, bool repeatRowHeaders, XmlNode tablixSort)
		{
			bool Repeats(RowLevel level) => level.HeaderMembers.Exists(m => m.RepeatOnNewPage);
			Func<int, Member> MembersOf(RowLevel level) => i => level.Members.TryGetValue(i, out Member m) ? m : null;

			RowLevel innermost = top;
			while (innermost.Inner != null)
				innermost = innermost.Inner;

			var header = new Header(OwnerReport, this, repeatRowHeaders || Repeats(top));
			header.TableRows = Rows(header, top.Header, MembersOf(top));
			if (header.TableRows != null)
				Header = header;

			// A chain ending in a lone group row has no details: the table is its group headers.
			var details = new Details(OwnerReport, this);
			details.TableRows = Rows(details, innermost.Details, MembersOf(innermost));
			if (details.TableRows != null)
			{
				XmlNode sort = innermost.DetailsMember?.SortExpressions ?? tablixSort;
				if (sort != null)
					details.Sorting = new Sorting(OwnerReport, details, sort);
				Details = details;
			}

			var footer = new Footer(OwnerReport, this);
			footer.TableRows = Rows(footer, top.Footer, MembersOf(top));
			if (footer.TableRows != null)
				Footer = footer;

			var groups = new TableGroups(OwnerReport, this);
			for (RowLevel l = top; l.GroupMember != null; l = l.Inner)
			{
				var tg = new TableGroup(OwnerReport, groups);
				tg.Grouping = new Grouping(OwnerReport, tg, l.GroupMember.Group);
				if (l.GroupMember.SortExpressions != null)
					tg.Sorting = new Sorting(OwnerReport, tg, l.GroupMember.SortExpressions);

				// The group's own header and footer rows are in the level inside it.
				var gh = new Header(OwnerReport, tg, Repeats(l.Inner));
				gh.TableRows = Rows(gh, l.Inner.Header, MembersOf(l.Inner));
				if (gh.TableRows != null)
					tg.Header = gh;
				var gf = new Footer(OwnerReport, tg);
				gf.TableRows = Rows(gf, l.Inner.Footer, MembersOf(l.Inner));
				if (gf.TableRows != null)
					tg.Footer = gf;

				groups.Items.Add(tg);
			}
			TableGroups = groups;
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
