using System;
using System.Collections.Generic;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Prepares a report the designer edited as RDL 2005 for saving into a 2008+ document.
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="Rdl2008Normalizer"/>. The designer's insert commands only know
    /// the 2005 vocabulary, so a List added to an RDLC (2008/2010/2016 namespace) report is
    /// written as a &lt;List&gt; element, which those schemas do not define -- Visual Studio and
    /// ReportViewer reject the file. A List is exactly a one-cell Tablix, so it is rewritten as
    /// one. A Table maps onto a Tablix whose row hierarchy carries its header, groups, details and
    /// footer. Matrix is not handled here yet.
    /// </remarks>
    public static class Rdl2008Exporter
    {
        private static readonly string[] ModernNamespaces =
        {
            "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition",
            "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition",
            "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition",
        };

        /// <summary>True when the document is 2008+ and contains a List that needs rewriting.</summary>
        public static bool NeedsConversion (XmlDocument doc)
        {
            var root = doc?.DocumentElement;
            if (root == null || Array.IndexOf (ModernNamespaces, root.NamespaceURI) < 0)
                return false;

            var lists = new List<XmlElement> ();
            Collect (root, lists);
            return lists.Count > 0;
        }

        /// <summary>
        /// Rewrites every List and Table in a 2008+ document as a Tablix, in place. A 2005
        /// document, or one with no namespace, is left alone because both are valid there.
        /// </summary>
        /// <returns>The number of Lists converted.</returns>
        public static int ConvertToTablix (XmlDocument doc)
        {
            var root = doc?.DocumentElement;
            if (root == null || Array.IndexOf (ModernNamespaces, root.NamespaceURI) < 0)
                return 0;

            // Innermost first: a converted outer List clones its contents, so converting the
            // outer one first would leave the nested List behind in the copy.
            var lists = new List<XmlElement> ();
            Collect (root, lists);
            lists.Reverse ();

            var count = 0;
            foreach (var list in lists) {
                list.ParentNode.ReplaceChild (list.LocalName == "Table" ? BuildTablixFromTable (list) : BuildTablix (list), list);
                count++;
            }

            return count;
        }

        private static void Collect (XmlElement element, List<XmlElement> found)
        {
            foreach (XmlNode node in element.ChildNodes) {
                if (node is not XmlElement child)
                    continue;

                if ((child.LocalName == "List" || child.LocalName == "Table") && child.NamespaceURI == element.NamespaceURI)
                    found.Add (child);

                Collect (child, found);
            }
        }

        private static XmlElement BuildTablix (XmlElement list)
        {
            var doc = list.OwnerDocument;
            var ns = list.NamespaceURI;
            var name = list.GetAttribute ("Name");

            XmlElement Make (string localName, string text = null)
            {
                var e = doc.CreateElement (localName, ns);
                if (text != null)
                    e.InnerText = text;
                return e;
            }

            var tablix = Make ("Tablix");
            if (name.Length > 0)
                tablix.SetAttribute ("Name", name);

            XmlElement reportItems = null;
            XmlElement grouping = null;
            XmlElement sorting = null;
            XmlElement style = null;
            string dataSetName = null;
            string width = "1in";
            string height = "1in";
            var passThrough = new List<XmlElement> ();

            foreach (XmlNode node in list.ChildNodes) {
                if (node is not XmlElement child)
                    continue;

                switch (child.LocalName) {
                    case "ReportItems": reportItems = child; break;
                    case "Grouping": grouping = child; break;
                    case "Sorting": sorting = child; break;
                    case "Style": style = child; break;
                    case "DataSetName": dataSetName = child.InnerText; break;
                    case "Width": width = child.InnerText; break;
                    case "Height": height = child.InnerText; break;
                    // Position, visibility, bookmarks, ZIndex, ...: same spelling in both versions.
                    default: passThrough.Add (child); break;
                }
            }

            // The list's items live in the single cell, wrapped in a Rectangle: a Tablix cell
            // holds one item, and a Rectangle is what keeps the children's relative positions.
            var rectangle = Make ("Rectangle");
            rectangle.SetAttribute ("Name", (name.Length > 0 ? name : "List") + "_Contents");
            if (reportItems != null)
                rectangle.AppendChild (reportItems.CloneNode (true));
            rectangle.AppendChild (Make ("KeepTogether", "true"));
            if (style != null)
                rectangle.AppendChild (style.CloneNode (true));

            var contents = Make ("CellContents");
            contents.AppendChild (rectangle);
            var cell = Make ("TablixCell");
            cell.AppendChild (contents);
            var cells = Make ("TablixCells");
            cells.AppendChild (cell);
            var row = Make ("TablixRow");
            row.AppendChild (Make ("Height", height));
            row.AppendChild (cells);
            var rows = Make ("TablixRows");
            rows.AppendChild (row);
            var column = Make ("TablixColumn");
            column.AppendChild (Make ("Width", width));
            var columns = Make ("TablixColumns");
            columns.AppendChild (column);
            var body = Make ("TablixBody");
            body.AppendChild (columns);
            body.AppendChild (rows);
            tablix.AppendChild (body);

            // One static column; the rows are where a List repeats.
            var columnMembers = Make ("TablixMembers");
            columnMembers.AppendChild (Make ("TablixMember"));
            var columnHierarchy = Make ("TablixColumnHierarchy");
            columnHierarchy.AppendChild (columnMembers);
            tablix.AppendChild (columnHierarchy);

            var rowMember = Make ("TablixMember");
            // A List with neither a grouping nor a dataset is one static cell; anything else
            // repeats, and a Group with no expressions is Tablix's "Details" (one per record).
            if (grouping != null || !string.IsNullOrEmpty (dataSetName)) {
                rowMember.AppendChild (BuildGroup (doc, ns, grouping, name));
                if (sorting != null)
                    rowMember.AppendChild (BuildSortExpressions (doc, ns, sorting));
            }
            var rowMembers = Make ("TablixMembers");
            rowMembers.AppendChild (rowMember);
            var rowHierarchy = Make ("TablixRowHierarchy");
            rowHierarchy.AppendChild (rowMembers);
            tablix.AppendChild (rowHierarchy);

            if (!string.IsNullOrEmpty (dataSetName))
                tablix.AppendChild (Make ("DataSetName", dataSetName));

            foreach (var child in passThrough)
                tablix.AppendChild (child.CloneNode (true));

            tablix.AppendChild (Make ("Height", height));
            tablix.AppendChild (Make ("Width", width));

            return tablix;
        }

        private static XmlElement El (XmlDocument doc, string ns, string name, string text = null)
        {
            var e = doc.CreateElement (name, ns);
            if (text != null)
                e.InnerText = text;
            return e;
        }

        private static XmlElement Child (XmlElement parent, string localName)
        {
            if (parent == null)
                return null;
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement e && e.LocalName == localName)
                    return e;
            }
            return null;
        }

        private static List<XmlElement> Kids (XmlElement parent, string localName)
        {
            var result = new List<XmlElement> ();
            if (parent == null)
                return result;
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement e && e.LocalName == localName)
                    result.Add (e);
            }
            return result;
        }

        private static XmlElement BuildTablixFromTable (XmlElement table)
        {
            var doc = table.OwnerDocument;
            var ns = table.NamespaceURI;
            var name = table.GetAttribute ("Name");
            var prefix = name.Length > 0 ? name : "Table";

            var tablix = El (doc, ns, "Tablix");
            if (name.Length > 0)
                tablix.SetAttribute ("Name", name);

            var columnWidths = Kids (Child (table, "TableColumns"), "TableColumn")
                .ConvertAll (c => Child (c, "Width")?.InnerText ?? "1in");
            var bodyRows = El (doc, ns, "TablixRows");

            XmlElement BuildRow (XmlElement tableRow)
            {
                var row = El (doc, ns, "TablixRow");
                row.AppendChild (El (doc, ns, "Height", Child (tableRow, "Height")?.InnerText ?? "0.25in"));
                var cells = El (doc, ns, "TablixCells");
                foreach (var tableCell in Kids (Child (tableRow, "TableCells"), "TableCell")) {
                    var contents = El (doc, ns, "CellContents");
                    var itemList = new List<XmlElement> ();
                    var reportItems = Child (tableCell, "ReportItems");
                    if (reportItems != null) {
                        foreach (XmlNode n in reportItems.ChildNodes) {
                            if (n is XmlElement item)
                                itemList.Add (item);
                        }
                    }
                    if (itemList.Count == 1) {
                        contents.AppendChild (itemList[0].CloneNode (true));
                    } else if (itemList.Count > 1) {
                        var rect = El (doc, ns, "Rectangle");
                        rect.SetAttribute ("Name", prefix + "_Cell" + bodyRows.ChildNodes.Count + "_" + cells.ChildNodes.Count);
                        var inner = El (doc, ns, "ReportItems");
                        foreach (var item in itemList)
                            inner.AppendChild (item.CloneNode (true));
                        rect.AppendChild (inner);
                        contents.AppendChild (rect);
                    }

                    var span = 1;
                    var colSpan = Child (tableCell, "ColSpan");
                    if (colSpan != null) {
                        contents.AppendChild (colSpan.CloneNode (true));
                        int.TryParse (colSpan.InnerText, out span);
                    }

                    var cell = El (doc, ns, "TablixCell");
                    cell.AppendChild (contents);
                    cells.AppendChild (cell);
                    // 2008 keeps a placeholder for each position a ColSpan covers.
                    for (var i = 1; i < span; i++)
                        cells.AppendChild (El (doc, ns, "TablixCell"));
                }
                row.AppendChild (cells);
                bodyRows.AppendChild (row);
                return row;
            }

            XmlElement StaticMember (XmlElement tableRow, string keepWithGroup, bool repeat)
            {
                BuildRow (tableRow);
                var member = El (doc, ns, "TablixMember");
                if (keepWithGroup != null)
                    member.AppendChild (El (doc, ns, "KeepWithGroup", keepWithGroup));
                if (repeat)
                    member.AppendChild (El (doc, ns, "RepeatOnNewPage", "true"));
                var visibility = Child (tableRow, "Visibility");
                if (visibility != null)
                    member.AppendChild (visibility.CloneNode (true));
                return member;
            }

            List<XmlElement> Section (XmlElement section, string keepWithGroup)
            {
                var members = new List<XmlElement> ();
                if (section == null)
                    return members;
                var repeat = string.Equals (Child (section, "RepeatOnNewPage")?.InnerText, "true", StringComparison.OrdinalIgnoreCase);
                foreach (var tableRow in Kids (Child (section, "TableRows"), "TableRow"))
                    members.Add (StaticMember (tableRow, keepWithGroup, repeat));
                return members;
            }

            var details = Child (table, "Details");
            var groups = Kids (Child (table, "TableGroups"), "TableGroup");

            List<XmlElement> Level (int index)
            {
                var result = new List<XmlElement> ();
                if (index == groups.Count) {
                    if (details == null)
                        return result;
                    var rowMembers = new List<XmlElement> ();
                    foreach (var tableRow in Kids (Child (details, "TableRows"), "TableRow"))
                        rowMembers.Add (StaticMember (tableRow, null, false));
                    var member = El (doc, ns, "TablixMember");
                    var detailsGrouping = Child (details, "Grouping");
                    member.AppendChild (BuildGroup (doc, ns, detailsGrouping, prefix));
                    var sorting = Child (details, "Sorting");
                    if (sorting != null)
                        member.AppendChild (BuildSortExpressions (doc, ns, sorting));
                    var visibility = Child (details, "Visibility");
                    if (visibility != null)
                        member.AppendChild (visibility.CloneNode (true));
                    if (rowMembers.Count > 1) {
                        var nested = El (doc, ns, "TablixMembers");
                        rowMembers.ForEach (m => nested.AppendChild (m));
                        member.AppendChild (nested);
                    }
                    result.Add (member);
                    return result;
                }

                var tableGroup = groups[index];
                var groupMember = El (doc, ns, "TablixMember");
                groupMember.AppendChild (BuildGroup (doc, ns, Child (tableGroup, "Grouping"), prefix + (index + 1)));
                var groupSorting = Child (tableGroup, "Sorting");
                if (groupSorting != null)
                    groupMember.AppendChild (BuildSortExpressions (doc, ns, groupSorting));
                var members = El (doc, ns, "TablixMembers");
                foreach (var m in Section (Child (tableGroup, "Header"), "After"))
                    members.AppendChild (m);
                foreach (var m in Level (index + 1))
                    members.AppendChild (m);
                foreach (var m in Section (Child (tableGroup, "Footer"), "Before"))
                    members.AppendChild (m);
                groupMember.AppendChild (members);
                result.Add (groupMember);
                return result;
            }

            // Rows are emitted in the order the hierarchy visits them, so build in that order.
            var rowMembersTop = El (doc, ns, "TablixMembers");
            foreach (var m in Section (Child (table, "Header"), "After"))
                rowMembersTop.AppendChild (m);
            foreach (var m in Level (0))
                rowMembersTop.AppendChild (m);
            foreach (var m in Section (Child (table, "Footer"), "Before"))
                rowMembersTop.AppendChild (m);

            var columns = El (doc, ns, "TablixColumns");
            var columnMembers = El (doc, ns, "TablixMembers");
            foreach (var width in columnWidths) {
                var column = El (doc, ns, "TablixColumn");
                column.AppendChild (El (doc, ns, "Width", width));
                columns.AppendChild (column);
                columnMembers.AppendChild (El (doc, ns, "TablixMember"));
            }

            var body = El (doc, ns, "TablixBody");
            body.AppendChild (columns);
            body.AppendChild (bodyRows);
            tablix.AppendChild (body);

            var columnHierarchy = El (doc, ns, "TablixColumnHierarchy");
            columnHierarchy.AppendChild (columnMembers);
            tablix.AppendChild (columnHierarchy);
            var rowHierarchy = El (doc, ns, "TablixRowHierarchy");
            rowHierarchy.AppendChild (rowMembersTop);
            tablix.AppendChild (rowHierarchy);

            string pageBreakAtStart = null, pageBreakAtEnd = null;
            foreach (XmlNode node in table.ChildNodes) {
                if (node is not XmlElement child)
                    continue;
                switch (child.LocalName) {
                    case "TableColumns":
                    case "Header":
                    case "Details":
                    case "Footer":
                    case "TableGroups":
                    case "FillPage":
                        break;
                    case "PageBreakAtStart": pageBreakAtStart = child.InnerText; break;
                    case "PageBreakAtEnd": pageBreakAtEnd = child.InnerText; break;
                    case "NoRows": tablix.AppendChild (El (doc, ns, "NoRowsMessage", child.InnerText)); break;
                    default: tablix.AppendChild (child.CloneNode (true)); break;
                }
            }

            var atStart = string.Equals (pageBreakAtStart, "true", StringComparison.OrdinalIgnoreCase);
            var atEnd = string.Equals (pageBreakAtEnd, "true", StringComparison.OrdinalIgnoreCase);
            if (atStart || atEnd) {
                var pageBreak = El (doc, ns, "PageBreak");
                pageBreak.AppendChild (El (doc, ns, "BreakLocation", atStart && atEnd ? "StartAndEnd" : atStart ? "Start" : "End"));
                tablix.AppendChild (pageBreak);
            }

            return tablix;
        }

        private static XmlElement BuildGroup (XmlDocument doc, string ns, XmlElement grouping, string listName)
        {
            var group = doc.CreateElement ("Group", ns);
            var groupName = grouping?.GetAttribute ("Name");
            if (string.IsNullOrEmpty (groupName))
                groupName = (listName.Length > 0 ? listName : "List") + "_Details_Group";
            group.SetAttribute ("Name", groupName);

            if (grouping == null)
                return group;

            string pageBreakAtStart = null, pageBreakAtEnd = null;
            foreach (XmlNode node in grouping.ChildNodes) {
                if (node is not XmlElement child)
                    continue;

                switch (child.LocalName) {
                    case "PageBreakAtStart": pageBreakAtStart = child.InnerText; break;
                    case "PageBreakAtEnd": pageBreakAtEnd = child.InnerText; break;
                    // GroupExpressions, Filters, Parent: unchanged in 2008.
                    default: group.AppendChild (child.CloneNode (true)); break;
                }
            }

            var atStart = string.Equals (pageBreakAtStart, "true", StringComparison.OrdinalIgnoreCase);
            var atEnd = string.Equals (pageBreakAtEnd, "true", StringComparison.OrdinalIgnoreCase);
            if (atStart || atEnd) {
                var pageBreak = doc.CreateElement ("PageBreak", ns);
                var location = doc.CreateElement ("BreakLocation", ns);
                location.InnerText = atStart && atEnd ? "StartAndEnd" : atStart ? "Start" : "End";
                pageBreak.AppendChild (location);
                group.AppendChild (pageBreak);
            }

            return group;
        }

        /// <summary>2005 Sorting/SortBy becomes 2008 SortExpressions/SortExpression/Value.</summary>
        private static XmlElement BuildSortExpressions (XmlDocument doc, string ns, XmlElement sorting)
        {
            var result = doc.CreateElement ("SortExpressions", ns);
            foreach (XmlNode node in sorting.ChildNodes) {
                if (node is not XmlElement sortBy || sortBy.LocalName != "SortBy")
                    continue;

                var sortExpression = doc.CreateElement ("SortExpression", ns);
                foreach (XmlNode part in sortBy.ChildNodes) {
                    if (part is not XmlElement p)
                        continue;

                    if (p.LocalName == "SortExpression") {
                        var value = doc.CreateElement ("Value", ns);
                        value.InnerText = p.InnerText;
                        sortExpression.AppendChild (value);
                    } else if (p.LocalName == "Direction") {
                        sortExpression.AppendChild (p.CloneNode (true));
                    }
                }
                result.AppendChild (sortExpression);
            }

            return result;
        }
    }
}
