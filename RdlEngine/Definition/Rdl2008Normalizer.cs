/* ====================================================================
    Copyright (C) 2004-2008  fyiReporting Software, LLC
    Copyright (C) 2011  Peter Gill <peter@majorsilence.com>

    This file is part of the fyiReporting RDL project.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Rewrites an RDL 2008/2016 document into the RDL 2005 shape the definition classes
    /// understand, before any of them see it.
    /// </summary>
    /// <remarks>
    /// The engine's definition tree models RDL 2005: data regions are Table/Matrix/List and a
    /// Textbox holds a single Value. RDL 2008 replaced all three regions with Tablix and gave
    /// Textbox a rich-text model (Paragraphs -> TextRuns), so a 2008 report parsed directly
    /// produces "unknown element" errors and renders empty. Normalising the XmlDocument up front
    /// keeps that knowledge in one place: the ~50 definition classes and every renderer (PDF,
    /// HTML, Excel, image) stay untouched and all gain 2008 support at once.
    ///
    /// Scope is deliberately "tier 1": a Tablix whose column hierarchy is entirely static, which
    /// is an ordinary table and maps onto Table. A dynamic column hierarchy is a pivot and belongs
    /// on Matrix; nested or recursive hierarchies on both axes have no 2005 equivalent at all.
    /// Both are left alone and reported, so they fail loudly rather than rendering wrongly.
    /// </remarks>
    internal static class Rdl2008Normalizer
    {
        private const string Rdl2005Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        // 2010 and 2016 only add elements on top of the 2008 layout for everything handled here,
        // so they normalise identically. RDLC files saved by recent Visual Studio are usually 2016.
        private static readonly string[] ModernNamespaces =
        {
            "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition",
            "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition",
            "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition",
        };

        /// <summary>
        /// True when the document declares a post-2005 report definition namespace, or uses no
        /// namespace but contains a Tablix (hand-written or tool-stripped files do occur).
        /// </summary>
        internal static bool NeedsNormalizing (XmlDocument doc)
        {
            var report = FindReportElement (doc);
            if (report == null)
                return false;

            foreach (var ns in ModernNamespaces) {
                if (string.Equals (report.NamespaceURI, ns, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return report.NamespaceURI.Length == 0
                && FindFirstDescendant (report, "Tablix") != null;
        }

        /// <summary>
        /// Rewrites the document in place. Safe to call on a 2005 document (it does nothing).
        /// </summary>
        internal static void Normalize (XmlDocument doc, ReportLog rl)
        {
            var report = FindReportElement (doc);
            if (report == null)
                return;

            UnwrapReportSections (report, rl);
            UnwrapPage (report);
            NormalizeBorders (report);
            NormalizeTextboxes (report, rl);
            NormalizeTablixes (report, rl);
            RemoveVersionOnlyElements (report);
        }

        /// <summary>
        /// RDL 2016 wraps the body and page setup in ReportSections/ReportSection; 2005 has a
        /// single implicit section hanging off Report.
        /// </summary>
        private static void UnwrapReportSections (XmlElement report, ReportLog rl)
        {
            var sections = FindChild (report, "ReportSections");
            if (sections == null)
                return;

            var sectionList = ChildrenNamed (sections, "ReportSection");

            // Multiple sections are separate page sequences with their own bodies, which the 2005
            // model cannot express; keeping the first is better than losing the report entirely.
            if (sectionList.Count > 1) {
                rl?.LogError (4, $"Report has {sectionList.Count} ReportSections; only the first is " +
                    "rendered. Multiple sections are not supported.");
            }

            if (sectionList.Count > 0) {
                foreach (var child in Children (sectionList[0])) {
                    sectionList[0].RemoveChild (child);
                    report.InsertBefore (child, sections);
                }
            }

            report.RemoveChild (sections);
        }

        private static XmlElement FindReportElement (XmlDocument doc)
        {
            for (var node = doc.LastChild; node != null; node = node.PreviousSibling) {
                if (node is XmlElement element && element.LocalName == "Report")
                    return element;
            }

            return null;
        }

        #region Page

        /// <summary>
        /// 2008 groups the page setup under a Page element; 2005 hangs it off Report directly.
        /// </summary>
        private static void UnwrapPage (XmlElement report)
        {
            var page = FindChild (report, "Page");
            if (page == null)
                return;

            // The Page element's own Style has no 2005 home and describes the printed page
            // border, which nothing in the 2005 tree consumes -- dropping it loses nothing.
            foreach (var child in Children (page)) {
                if (child.LocalName == "Style")
                    continue;

                page.RemoveChild (child);
                report.InsertBefore (child, page);
            }

            report.RemoveChild (page);
        }

        #endregion

        #region Borders

        // 2008 groups border properties by edge (Border, TopBorder, ...) each holding Color/Style/
        // Width. 2005 groups them the other way round: BorderColor/BorderStyle/BorderWidth, each
        // holding Default/Left/Right/Top/Bottom. Same information, transposed.
        private static readonly Dictionary<string, string> BorderEdges = new Dictionary<string, string> (StringComparer.Ordinal)
        {
            ["Border"] = "Default",
            ["LeftBorder"] = "Left",
            ["RightBorder"] = "Right",
            ["TopBorder"] = "Top",
            ["BottomBorder"] = "Bottom",
        };

        private static void NormalizeBorders (XmlElement root)
        {
            foreach (var style in FindDescendants (root, "Style"))
                NormalizeBorder (style);
        }

        private static void NormalizeBorder (XmlElement style)
        {
            var doc = style.OwnerDocument;
            var ns = style.NamespaceURI;

            foreach (var child in Children (style)) {
                if (!BorderEdges.TryGetValue (child.LocalName, out var edge))
                    continue;

                foreach (var property in Children (child)) {
                    // Color -> BorderColor, Style -> BorderStyle, Width -> BorderWidth.
                    var groupName = "Border" + property.LocalName;
                    if (property.LocalName != "Color" && property.LocalName != "Style" && property.LocalName != "Width")
                        continue;

                    var group = FindChild (style, groupName);
                    if (group == null) {
                        group = doc.CreateElement (groupName, ns);
                        style.AppendChild (group);
                    }

                    var existing = FindChild (group, edge);
                    if (existing != null)
                        group.RemoveChild (existing);

                    var target = doc.CreateElement (edge, ns);
                    target.InnerText = property.InnerText;
                    group.AppendChild (target);
                }

                style.RemoveChild (child);
            }
        }

        #endregion

        #region Textbox rich text

        /// <summary>
        /// Collapses the 2008 Paragraphs/TextRuns tree back to a single Value plus a merged Style.
        /// </summary>
        private static void NormalizeTextboxes (XmlElement root, ReportLog rl)
        {
            foreach (var textbox in FindDescendants (root, "Textbox"))
                NormalizeTextbox (textbox, rl);
        }

        private static void NormalizeTextbox (XmlElement textbox, ReportLog rl)
        {
            var paragraphs = FindChild (textbox, "Paragraphs");
            if (paragraphs == null)
                return;

            var texts = new List<string> ();
            XmlElement firstParagraphStyle = null;
            XmlElement firstRunStyle = null;
            var droppedRuns = false;

            foreach (var paragraph in ChildrenNamed (paragraphs, "Paragraph")) {
                firstParagraphStyle ??= FindChild (paragraph, "Style");

                var runs = FindChild (paragraph, "TextRuns");
                if (runs == null)
                    continue;

                var runValues = new List<string> ();
                var runIsExpression = false;

                foreach (var run in ChildrenNamed (runs, "TextRun")) {
                    firstRunStyle ??= FindChild (run, "Style");

                    var value = FindChild (run, "Value")?.InnerText ?? string.Empty;
                    if (value.StartsWith ("=", StringComparison.Ordinal))
                        runIsExpression = true;

                    runValues.Add (value);
                }

                if (runValues.Count == 0)
                    continue;

                // A 2005 Textbox has one Value, so several runs have to become one string. Plain
                // literals just concatenate; once any run is an expression the whole thing has to
                // become a single expression, with the literals quoted and joined by '&'.
                if (runValues.Count > 1) {
                    droppedRuns = true;
                    texts.Add (runIsExpression ? CombineAsExpression (runValues) : string.Concat (runValues));
                } else {
                    texts.Add (runValues[0]);
                }
            }

            if (droppedRuns) {
                // The text survives; only the per-run formatting is lost, since the merged Style
                // below can hold just one font/colour for the whole textbox.
                rl?.LogError (4, $"Textbox '{textbox.GetAttribute ("Name")}' has several TextRuns in a " +
                    "paragraph; their text is combined but per-run formatting is not supported.");
            }

            textbox.RemoveChild (paragraphs);

            var doc = textbox.OwnerDocument;
            var valueElement = doc.CreateElement ("Value", textbox.NamespaceURI);
            valueElement.InnerText = string.Join ("\n", texts);
            textbox.AppendChild (valueElement);

            // Least specific first: the box's own style is the base, the paragraph overrides it
            // (alignment), and the run wins outright (font, colour) -- the order the 2008 renderer
            // resolves them in.
            MergeStyleInto (textbox, firstParagraphStyle);
            MergeStyleInto (textbox, firstRunStyle);
        }

        /// <summary>
        /// Joins mixed literal/expression runs into one RDL expression, e.g. ["Total: ",
        /// "=Fields!X.Value"] becomes ="Total: " &amp; Fields!X.Value.
        /// </summary>
        private static string CombineAsExpression (List<string> runValues)
        {
            var sb = new StringBuilder ("=");

            for (var i = 0; i < runValues.Count; i++) {
                if (i > 0)
                    sb.Append (" & ");

                var value = runValues[i];
                if (value.StartsWith ("=", StringComparison.Ordinal))
                    sb.Append ('(').Append (value, 1, value.Length - 1).Append (')');
                else
                    sb.Append ('"').Append (value.Replace ("\"", "\"\"")).Append ('"');
            }

            return sb.ToString ();
        }

        /// <summary>
        /// Copies style properties into the textbox's own Style, with the incoming ones winning.
        /// </summary>
        private static void MergeStyleInto (XmlElement textbox, XmlElement incoming)
        {
            if (incoming == null || !incoming.HasChildNodes)
                return;

            var target = FindChild (textbox, "Style");
            if (target == null) {
                target = textbox.OwnerDocument.CreateElement ("Style", textbox.NamespaceURI);
                textbox.AppendChild (target);
            }

            foreach (var property in Children (incoming)) {
                var existing = FindChild (target, property.LocalName);
                if (existing != null)
                    target.RemoveChild (existing);

                target.AppendChild (property.CloneNode (true));
            }
        }

        #endregion

        #region Tablix

        private static void NormalizeTablixes (XmlElement root, ReportLog rl)
        {
            // Materialised first: the loop replaces nodes, which would invalidate a live walk.
            foreach (var tablix in FindDescendants (root, "Tablix")) {
                var table = TryConvertTablix (tablix, rl);
                if (table != null)
                    tablix.ParentNode.ReplaceChild (table, tablix);
            }
        }

        private static XmlElement TryConvertTablix (XmlElement tablix, ReportLog rl)
        {
            var name = tablix.GetAttribute ("Name");
            var body = FindChild (tablix, "TablixBody");
            if (body == null) {
                rl?.LogError (8, $"Tablix '{name}' has no TablixBody and was ignored.");
                return null;
            }

            if (HasDynamicMembers (FindChild (tablix, "TablixColumnHierarchy"))) {
                rl?.LogError (8, $"Tablix '{name}' has a dynamic column hierarchy (a pivot/matrix), " +
                    "which is not supported yet; the region was ignored.");
                return null;
            }

            var rows = ChildrenNamed (FindChild (body, "TablixRows"), "TablixRow");
            var placements = ClassifyRows (FindChild (tablix, "TablixRowHierarchy"), rows.Count, name, rl);
            if (placements == null)
                return null;

            var doc = tablix.OwnerDocument;
            var ns = tablix.NamespaceURI;
            var table = doc.CreateElement ("Table", ns);

            if (!string.IsNullOrEmpty (name))
                table.SetAttribute ("Name", name);

            // Position, size, style, DataSetName, visibility: identical elements in both versions.
            foreach (var child in Children (tablix)) {
                switch (child.LocalName) {
                    case "TablixBody":
                    case "TablixColumnHierarchy":
                    case "TablixRowHierarchy":
                    case "TablixCorner":
                        continue;
                    default:
                        table.AppendChild (child.CloneNode (true));
                        break;
                }
            }

            table.AppendChild (BuildTableColumns (doc, ns, FindChild (body, "TablixColumns")));

            AppendSection (table, doc, ns, "Header", rows, placements, RowPlacement.Header);
            AppendSection (table, doc, ns, "Details", rows, placements, RowPlacement.Detail);
            AppendSection (table, doc, ns, "Footer", rows, placements, RowPlacement.Footer);

            return table;
        }

        private enum RowPlacement { Header, Detail, Footer }

        /// <summary>
        /// Walks the row hierarchy and decides, for each Tablix row, whether it belongs in the
        /// table's header, detail or footer section.
        /// </summary>
        /// <remarks>
        /// Leaf members correspond to rows one-for-one, in document order. Anything inside a group
        /// repeats per record and so is detail -- which is what makes a "card" layout work, where
        /// a Details group nests a stack of static rows. Static members outside any group are
        /// header before the detail rows and footer after them.
        /// </remarks>
        private static List<RowPlacement> ClassifyRows (XmlElement hierarchy, int rowCount, string tablixName, ReportLog rl)
        {
            var placements = new List<RowPlacement> ();
            var seenDetail = false;

            void Walk (XmlElement members, bool insideGroup)
            {
                foreach (var member in ChildrenNamed (members, "TablixMember")) {
                    var group = FindChild (member, "Group");
                    var nested = FindChild (member, "TablixMembers");
                    var inGroup = insideGroup || group != null;

                    if (nested != null) {
                        Walk (nested, inGroup);
                        continue;
                    }

                    if (inGroup) {
                        placements.Add (RowPlacement.Detail);
                        seenDetail = true;
                    } else {
                        placements.Add (seenDetail ? RowPlacement.Footer : RowPlacement.Header);
                    }
                }
            }

            var top = FindChild (hierarchy, "TablixMembers");
            if (top != null)
                Walk (top, false);

            if (placements.Count != rowCount) {
                // A mismatch means a shape this pass does not model (adjacent groups, recursive
                // members). Guessing would silently reorder the report, so refuse.
                rl?.LogError (8, $"Tablix '{tablixName}' has {rowCount} row(s) but {placements.Count} " +
                    "row hierarchy leaf member(s); its layout is not supported yet and it was ignored.");
                return null;
            }

            return placements;
        }

        private static bool HasDynamicMembers (XmlElement hierarchy)
        {
            var members = FindChild (hierarchy, "TablixMembers");
            if (members == null)
                return false;

            foreach (var member in ChildrenNamed (members, "TablixMember")) {
                if (FindChild (member, "Group") != null)
                    return true;

                if (HasDynamicMembers (member))
                    return true;
            }

            return false;
        }

        private static XmlElement BuildTableColumns (XmlDocument doc, string ns, XmlElement tablixColumns)
        {
            var columns = doc.CreateElement ("TableColumns", ns);

            foreach (var tablixColumn in ChildrenNamed (tablixColumns, "TablixColumn")) {
                var column = doc.CreateElement ("TableColumn", ns);

                foreach (var child in Children (tablixColumn))
                    column.AppendChild (child.CloneNode (true));

                columns.AppendChild (column);
            }

            return columns;
        }

        private static void AppendSection (XmlElement table, XmlDocument doc, string ns, string sectionName,
            List<XmlElement> rows, List<RowPlacement> placements, RowPlacement wanted)
        {
            var tableRows = doc.CreateElement ("TableRows", ns);
            var any = false;

            for (var i = 0; i < rows.Count; i++) {
                if (placements[i] != wanted)
                    continue;

                tableRows.AppendChild (BuildTableRow (doc, ns, rows[i]));
                any = true;
            }

            if (!any)
                return;

            var section = doc.CreateElement (sectionName, ns);
            section.AppendChild (tableRows);
            table.AppendChild (section);
        }

        private static XmlElement BuildTableRow (XmlDocument doc, string ns, XmlElement tablixRow)
        {
            var row = doc.CreateElement ("TableRow", ns);
            var cells = doc.CreateElement ("TableCells", ns);

            foreach (var tablixCell in ChildrenNamed (FindChild (tablixRow, "TablixCells"), "TablixCell")) {
                var contents = FindChild (tablixCell, "CellContents");

                // A TablixCell with no CellContents at all is the placeholder for a position
                // already covered by an earlier cell's ColSpan. 2008 keeps those to make the grid
                // rectangular; 2005 omits them, and emitting one here would overrun the column
                // count. An empty-but-present CellContents is a genuinely blank cell, handled below.
                if (contents == null)
                    continue;

                var cell = doc.CreateElement ("TableCell", ns);
                var items = doc.CreateElement ("ReportItems", ns);

                foreach (var child in Children (contents)) {
                    // CellContents carries ColSpan alongside the item; 2005 puts it on the cell.
                    if (child.LocalName == "ColSpan" || child.LocalName == "RowSpan") {
                        cell.AppendChild (child.CloneNode (true));
                        continue;
                    }

                    items.AppendChild (child.CloneNode (true));
                }

                // A 2005 TableCell must hold exactly one report item. 2008 allows an empty cell
                // (spanned-over or just blank) and, in principle, several items; a Rectangle is
                // the 2005 way to say "one item that contains these".
                if (items.ChildNodes.Count == 0)
                    items.AppendChild (BuildEmptyTextbox (doc, ns));
                else if (items.ChildNodes.Count > 1)
                    items = WrapInRectangle (doc, ns, items);

                cell.AppendChild (items);
                cells.AppendChild (cell);
            }

            row.AppendChild (cells);

            var height = FindChild (tablixRow, "Height");
            if (height != null)
                row.AppendChild (height.CloneNode (true));

            var visibility = FindChild (tablixRow, "Visibility");
            if (visibility != null)
                row.AppendChild (visibility.CloneNode (true));

            return row;
        }

        private static int _generatedNameCounter;

        /// <summary>Placeholder for a cell 2008 left empty but 2005 requires an item in.</summary>
        private static XmlElement BuildEmptyTextbox (XmlDocument doc, string ns)
        {
            var textbox = doc.CreateElement ("Textbox", ns);
            textbox.SetAttribute ("Name", "RdlNormalizedEmpty" + (++_generatedNameCounter));

            var value = doc.CreateElement ("Value", ns);
            value.InnerText = string.Empty;
            textbox.AppendChild (value);

            return textbox;
        }

        private static XmlElement WrapInRectangle (XmlDocument doc, string ns, XmlElement items)
        {
            var rectangle = doc.CreateElement ("Rectangle", ns);
            rectangle.SetAttribute ("Name", "RdlNormalizedGroup" + (++_generatedNameCounter));
            rectangle.AppendChild (items);

            var wrapper = doc.CreateElement ("ReportItems", ns);
            wrapper.AppendChild (rectangle);

            return wrapper;
        }

        #endregion

        #region Cleanup

        // Bookkeeping the designer writes that has no 2005 counterpart. Left in place they are
        // harmless but noisy: every one becomes an "unknown element" entry in the report log.
        private static readonly HashSet<string> VersionOnlyElements = new HashSet<string> (StringComparer.Ordinal)
        {
            "ReportID",
            "ReportUnitType",
            "ReportParametersLayout",
            "DataSourceID",
            "DataSetInfo",
            "ConsumeContainerWhitespace",
            "AutoRefresh",
            "KeepTogether",
            "GridLayoutDefinition",
        };

        private static void RemoveVersionOnlyElements (XmlElement report)
        {
            foreach (var name in VersionOnlyElements) {
                foreach (var element in FindDescendants (report, name))
                    element.ParentNode?.RemoveChild (element);
            }
        }

        #endregion

        #region XML helpers

        private static XmlElement FindChild (XmlElement parent, string localName)
        {
            if (parent == null)
                return null;

            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement element && element.LocalName == localName)
                    return element;
            }

            return null;
        }

        private static List<XmlElement> Children (XmlElement parent)
        {
            var result = new List<XmlElement> ();
            if (parent == null)
                return result;

            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement element)
                    result.Add (element);
            }

            return result;
        }

        private static List<XmlElement> ChildrenNamed (XmlElement parent, string localName)
        {
            var result = new List<XmlElement> ();
            if (parent == null)
                return result;

            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement element && element.LocalName == localName)
                    result.Add (element);
            }

            return result;
        }

        private static XmlElement FindFirstDescendant (XmlElement root, string localName)
        {
            foreach (XmlNode node in root.ChildNodes) {
                if (node is not XmlElement element)
                    continue;

                if (element.LocalName == localName)
                    return element;

                var found = FindFirstDescendant (element, localName);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Depth-first list of matching descendants, materialised so callers can replace or remove
        /// the nodes they get back without disturbing the walk.
        /// </summary>
        private static List<XmlElement> FindDescendants (XmlElement root, string localName)
        {
            var result = new List<XmlElement> ();
            Collect (root);
            return result;

            void Collect (XmlElement element)
            {
                foreach (XmlNode node in element.ChildNodes) {
                    if (node is not XmlElement child)
                        continue;

                    if (child.LocalName == localName)
                        result.Add (child);

                    Collect (child);
                }
            }
        }

        #endregion
    }
}
