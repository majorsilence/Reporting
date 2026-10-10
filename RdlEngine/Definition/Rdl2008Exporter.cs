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
    /// one. Table and Matrix are not handled here; they need the larger Tablix mapping.
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
        public static bool NeedsListConversion (XmlDocument doc)
        {
            var root = doc?.DocumentElement;
            if (root == null || Array.IndexOf (ModernNamespaces, root.NamespaceURI) < 0)
                return false;

            var lists = new List<XmlElement> ();
            Collect (root, lists);
            return lists.Count > 0;
        }

        /// <summary>
        /// Rewrites every List in a 2008+ document as a Tablix, in place. A 2005 document, or
        /// one with no namespace, is left alone because List is valid there.
        /// </summary>
        /// <returns>The number of Lists converted.</returns>
        public static int ConvertListsToTablix (XmlDocument doc)
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
                list.ParentNode.ReplaceChild (BuildTablix (list), list);
                count++;
            }

            return count;
        }

        private static void Collect (XmlElement element, List<XmlElement> found)
        {
            foreach (XmlNode node in element.ChildNodes) {
                if (node is not XmlElement child)
                    continue;

                if (child.LocalName == "List" && child.NamespaceURI == element.NamespaceURI)
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
