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
            // Converting a Tablix clones its cell contents into the new Table, so a Tablix
            // nested inside another region re-enters the tree as an unconverted clone; repeat
            // until a scan finds nothing new. Refused conversions stay in the tree and are
            // tracked by reference so the loop terminates.
            var refused = new List<XmlElement> ();
            while (true) {
                // Materialised each pass: the loop replaces nodes, which would invalidate a live walk.
                var pending = new List<XmlElement> ();
                foreach (var tablix in FindDescendants (root, "Tablix")) {
                    if (!refused.Contains (tablix))
                        pending.Add (tablix);
                }
                if (pending.Count == 0)
                    return;

                foreach (var tablix in pending) {
                    var table = TryConvertTablix (tablix, rl);
                    if (table != null)
                        tablix.ParentNode.ReplaceChild (table, tablix);
                    else
                        refused.Add (tablix);
                }
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

            // A dynamic column hierarchy pivots on data, which Table cannot express; that shape
            // is 2005's Matrix.
            if (HasDynamicMembers (FindChild (tablix, "TablixColumnHierarchy")))
                return TryConvertTablixToMatrix (tablix, rl);

            var rows = ChildrenNamed (FindChild (body, "TablixRows"), "TablixRow");
            var placements = ClassifyRows (FindChild (tablix, "TablixRowHierarchy"), rows.Count, name, rl);
            if (placements == null)
                return null;

            var doc = tablix.OwnerDocument;
            var ns = tablix.NamespaceURI;
            var table = doc.CreateElement ("Table", ns);

            if (!string.IsNullOrEmpty (name))
                table.SetAttribute ("Name", name);

            // Position, size, style, DataSetName, visibility: identical elements in both
            // versions. What 2008 spells differently is translated below, and what has no
            // Table counterpart is dropped here — copying it through only earns an "unknown
            // element" warning per report and loses it just the same.
            XmlElement sortExpressions = null;
            XmlElement pageBreak = null;
            var repeatHeaderRows = false;
            foreach (var child in Children (tablix)) {
                switch (child.LocalName) {
                    case "TablixBody":
                    case "TablixColumnHierarchy":
                    case "TablixRowHierarchy":
                    case "TablixCorner":
                        continue;
                    // Table does express these, under 2005 names.
                    case "SortExpressions":
                        sortExpressions = child;
                        continue;
                    case "PageBreak":
                        pageBreak = child;
                        continue;
                    case "RepeatRowHeaders":
                        repeatHeaderRows = XmlUtil.Boolean (child.InnerText, rl);
                        continue;
                    // Interactive only: frozen panes and repeated COLUMN headers describe a
                    // scrolling viewport, which paginated output does not have.
                    case "RepeatColumnHeaders":
                    case "FixedColumnHeaders":
                    case "FixedRowHeaders":
                        continue;
                    default:
                        table.AppendChild (child.CloneNode (true));
                        break;
                }
            }

            if (pageBreak != null)
                AppendPageBreak (table, doc, ns, pageBreak);

            table.AppendChild (BuildTableColumns (doc, ns, FindChild (body, "TablixColumns")));

            // RepeatRowHeaders is what makes column headings reappear after a page break; a
            // long report without it reads as unlabelled columns from page two onward.
            AppendSection (table, doc, ns, "Header", rows, placements, RowPlacement.Header,
                repeatOnNewPage: repeatHeaderRows);
            // 2008 hangs sorting off the data region, 2005 off the detail rows it sorts.
            // Untranslated, the detail order is whatever the query happened to return, which
            // is neither what the report asked for nor what SSRS shows.
            AppendSection (table, doc, ns, "Details", rows, placements, RowPlacement.Detail,
                sorting: sortExpressions == null ? null : BuildSorting (doc, ns, sortExpressions));
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
            List<XmlElement> rows, List<RowPlacement> placements, RowPlacement wanted,
            bool repeatOnNewPage = false, XmlElement sorting = null)
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
            if (repeatOnNewPage) {
                var repeat = doc.CreateElement ("RepeatOnNewPage", ns);
                repeat.InnerText = "true";
                section.AppendChild (repeat);
            }
            if (sorting != null)
                section.AppendChild (sorting);
            table.AppendChild (section);
        }

        /// <summary>
        /// 2008's PageBreak/BreakLocation to the 2005 pair of booleans. "StartAndEnd" sets
        /// both; "None" sets neither, and is worth honouring rather than assuming the element's
        /// presence means a break was wanted.
        /// </summary>
        private static void AppendPageBreak (XmlElement table, XmlDocument doc, string ns, XmlElement pageBreak)
        {
            var location = FindChild (pageBreak, "BreakLocation")?.InnerText?.Trim();
            var atStart = string.Equals (location, "Start", StringComparison.OrdinalIgnoreCase)
                || string.Equals (location, "StartAndEnd", StringComparison.OrdinalIgnoreCase);
            var atEnd = string.Equals (location, "End", StringComparison.OrdinalIgnoreCase)
                || string.Equals (location, "StartAndEnd", StringComparison.OrdinalIgnoreCase);

            if (atStart) {
                var start = doc.CreateElement ("PageBreakAtStart", ns);
                start.InnerText = "true";
                table.AppendChild (start);
            }
            if (atEnd) {
                var end = doc.CreateElement ("PageBreakAtEnd", ns);
                end.InnerText = "true";
                table.AppendChild (end);
            }
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

        #region Matrix

        /// <summary>
        /// One level of a Tablix hierarchy read for Matrix conversion: either a single grouped
        /// member (a dynamic level) or a run of leaf static members (the innermost level).
        /// </summary>
        private sealed class AxisLevel
        {
            internal XmlElement Dynamic;
            internal List<XmlElement> Statics;
        }

        /// <summary>
        /// Converts a Tablix with a dynamic column hierarchy into a 2005 Matrix. The Matrix model
        /// only expresses "regular" pivots -- uniform grouping levels with at most one static leaf
        /// level per axis -- so ragged hierarchies (subtotal siblings, adjacent groups, stacked
        /// static headers) are refused loudly rather than converted approximately.
        /// </summary>
        private static XmlElement TryConvertTablixToMatrix (XmlElement tablix, ReportLog rl)
        {
            var name = tablix.GetAttribute ("Name");
            var body = FindChild (tablix, "TablixBody");

            var columnLevels = ReadAxisLevels (FindChild (tablix, "TablixColumnHierarchy"), "column", name, rl);
            if (columnLevels == null)
                return null;

            var rowLevels = ReadAxisLevels (FindChild (tablix, "TablixRowHierarchy"), "row", name, rl);
            if (rowLevels == null)
                return null;

            TrimBareLeafLevel (columnLevels);
            TrimBareLeafLevel (rowLevels);

            var bodyColumns = ChildrenNamed (FindChild (body, "TablixColumns"), "TablixColumn");
            var bodyRows = ChildrenNamed (FindChild (body, "TablixRows"), "TablixRow");

            if (bodyColumns.Count != LeafCount (columnLevels) || bodyRows.Count != LeafCount (rowLevels)) {
                rl?.LogError (8, $"Tablix '{name}' has a {bodyRows.Count}x{bodyColumns.Count} body but its " +
                    $"hierarchies describe {LeafCount (rowLevels)}x{LeafCount (columnLevels)} leaf cell(s); " +
                    "this pivot layout is not supported yet and the region was ignored.");
                return null;
            }

            // Matrix cells cannot span, and a placeholder for a spanned-over position would shift
            // every later cell in its row.
            foreach (var bodyRow in bodyRows) {
                foreach (var cell in ChildrenNamed (FindChild (bodyRow, "TablixCells"), "TablixCell")) {
                    if (FindChild (cell, "CellContents") == null) {
                        rl?.LogError (8, $"Tablix '{name}' has merged (spanned) cells in its pivot body; " +
                            "this layout is not supported yet and the region was ignored.");
                        return null;
                    }
                }
            }

            var doc = tablix.OwnerDocument;
            var ns = tablix.NamespaceURI;
            var matrix = doc.CreateElement ("Matrix", ns);

            if (!string.IsNullOrEmpty (name))
                matrix.SetAttribute ("Name", name);

            foreach (var child in Children (tablix)) {
                switch (child.LocalName) {
                    case "TablixBody":
                    case "TablixColumnHierarchy":
                    case "TablixRowHierarchy":
                    case "TablixCorner":
                    // Pagination hints with no Matrix counterpart; dropping them silently beats a
                    // per-report "unknown element" warning for each.
                    case "RepeatColumnHeaders":
                    case "RepeatRowHeaders":
                    case "FixedColumnHeaders":
                    case "FixedRowHeaders":
                        continue;
                    default:
                        matrix.AppendChild (child.CloneNode (true));
                        break;
                }
            }

            var corner = BuildCorner (doc, ns, FindChild (tablix, "TablixCorner"), rowLevels, columnLevels, rl, name);
            if (corner != null)
                matrix.AppendChild (corner);

            matrix.AppendChild (BuildGroupings (doc, ns, columnLevels, isColumnAxis: true));
            matrix.AppendChild (BuildGroupings (doc, ns, rowLevels, isColumnAxis: false));

            var matrixRows = doc.CreateElement ("MatrixRows", ns);
            foreach (var bodyRow in bodyRows) {
                var matrixRow = doc.CreateElement ("MatrixRow", ns);

                var height = FindChild (bodyRow, "Height");
                if (height != null)
                    matrixRow.AppendChild (height.CloneNode (true));

                var matrixCells = doc.CreateElement ("MatrixCells", ns);
                foreach (var cell in ChildrenNamed (FindChild (bodyRow, "TablixCells"), "TablixCell")) {
                    var matrixCell = doc.CreateElement ("MatrixCell", ns);
                    matrixCell.AppendChild (BuildSingleItem (doc, ns, FindChild (cell, "CellContents")));
                    matrixCells.AppendChild (matrixCell);
                }

                matrixRow.AppendChild (matrixCells);
                matrixRows.AppendChild (matrixRow);
            }
            matrix.AppendChild (matrixRows);

            var matrixColumns = doc.CreateElement ("MatrixColumns", ns);
            foreach (var bodyColumn in bodyColumns) {
                var matrixColumn = doc.CreateElement ("MatrixColumn", ns);

                var width = FindChild (bodyColumn, "Width");
                if (width != null)
                    matrixColumn.AppendChild (width.CloneNode (true));

                matrixColumns.AppendChild (matrixColumn);
            }
            matrix.AppendChild (matrixColumns);

            return matrix;
        }

        /// <summary>
        /// Reads a Tablix hierarchy as a list of uniform levels, or refuses (null) when the tree
        /// is ragged. Each level is either exactly one grouped member, or -- only at the innermost
        /// level -- a run of leaf static members.
        /// </summary>
        private static List<AxisLevel> ReadAxisLevels (XmlElement hierarchy, string axis, string tablixName, ReportLog rl)
        {
            var levels = new List<AxisLevel> ();
            var top = FindChild (hierarchy, "TablixMembers");
            var members = top == null ? new List<XmlElement> () : ChildrenNamed (top, "TablixMember");

            while (members.Count > 0) {
                var allLeafStatic = true;
                foreach (var member in members) {
                    if (FindChild (member, "Group") != null || FindChild (member, "TablixMembers") != null) {
                        allLeafStatic = false;
                        break;
                    }
                }

                if (allLeafStatic) {
                    levels.Add (new AxisLevel { Statics = members });
                    return levels;
                }

                if (members.Count != 1) {
                    // A static member alongside a grouped one is a subtotal; grouped siblings are
                    // adjacent pivots. Either way there is no uniform-level Matrix equivalent, and
                    // converting without them would silently drop rows or columns.
                    rl?.LogError (8, $"Tablix '{tablixName}' mixes grouped and static members on its {axis} " +
                        "hierarchy (adjacent groups or subtotals); this pivot layout is not supported yet " +
                        "and the region was ignored.");
                    return null;
                }

                var only = members[0];
                if (FindChild (only, "Group") == null) {
                    rl?.LogError (8, $"Tablix '{tablixName}' has stacked static members on its {axis} " +
                        "hierarchy; this pivot layout is not supported yet and the region was ignored.");
                    return null;
                }

                levels.Add (new AxisLevel { Dynamic = only });

                var nested = FindChild (only, "TablixMembers");
                members = nested == null ? new List<XmlElement> () : ChildrenNamed (nested, "TablixMember");
            }

            return levels;
        }

        /// <summary>
        /// A single headerless static leaf under a group is just the 2008 way of writing "one body
        /// cell"; the level carries nothing a Matrix needs to model.
        /// </summary>
        private static void TrimBareLeafLevel (List<AxisLevel> levels)
        {
            if (levels.Count < 2)
                return;

            var last = levels[levels.Count - 1];
            if (last.Statics != null && last.Statics.Count == 1 && FindChild (last.Statics[0], "TablixHeader") == null)
                levels.RemoveAt (levels.Count - 1);
        }

        private static int LeafCount (List<AxisLevel> levels)
        {
            if (levels.Count == 0)
                return 1;

            var last = levels[levels.Count - 1];
            return last.Statics?.Count ?? 1;
        }

        /// <summary>Builds ColumnGroupings or RowGroupings from the levels of one axis.</summary>
        private static XmlElement BuildGroupings (XmlDocument doc, string ns, List<AxisLevel> levels, bool isColumnAxis)
        {
            var groupings = doc.CreateElement (isColumnAxis ? "ColumnGroupings" : "RowGroupings", ns);

            // Matrix requires at least one grouping per axis; a Tablix axis with no members at all
            // (or nothing left after trimming) means a single unlabelled band.
            if (levels.Count == 0)
                levels = new List<AxisLevel> { new AxisLevel { Statics = new List<XmlElement> () } };

            foreach (var level in levels) {
                var grouping = doc.CreateElement (isColumnAxis ? "ColumnGrouping" : "RowGrouping", ns);
                var sizeName = isColumnAxis ? "Height" : "Width";

                if (level.Dynamic != null) {
                    grouping.AppendChild (BuildSize (doc, ns, sizeName, HeaderSize (level.Dynamic)));

                    var dynamic = doc.CreateElement (isColumnAxis ? "DynamicColumns" : "DynamicRows", ns);
                    dynamic.AppendChild (BuildGrouping (doc, ns, FindChild (level.Dynamic, "Group")));

                    var sortExpressions = FindChild (level.Dynamic, "SortExpressions");
                    if (sortExpressions != null)
                        dynamic.AppendChild (BuildSorting (doc, ns, sortExpressions));

                    var visibility = FindChild (level.Dynamic, "Visibility");
                    if (visibility != null)
                        dynamic.AppendChild (visibility.CloneNode (true));

                    dynamic.AppendChild (BuildSingleItem (doc, ns, HeaderContents (level.Dynamic)));
                    grouping.AppendChild (dynamic);
                } else {
                    var size = "0in";
                    foreach (var member in level.Statics) {
                        var s = HeaderSize (member);
                        if (s != null) {
                            size = s;
                            break;
                        }
                    }
                    grouping.AppendChild (BuildSize (doc, ns, sizeName, size));

                    var statics = doc.CreateElement (isColumnAxis ? "StaticColumns" : "StaticRows", ns);
                    if (level.Statics.Count == 0) {
                        statics.AppendChild (BuildStaticMember (doc, ns, isColumnAxis, null));
                    } else {
                        foreach (var member in level.Statics)
                            statics.AppendChild (BuildStaticMember (doc, ns, isColumnAxis, HeaderContents (member)));
                    }
                    grouping.AppendChild (statics);
                }

                groupings.AppendChild (grouping);
            }

            return groupings;
        }

        private static XmlElement BuildStaticMember (XmlDocument doc, string ns, bool isColumnAxis, XmlElement contents)
        {
            var member = doc.CreateElement (isColumnAxis ? "StaticColumn" : "StaticRow", ns);
            member.AppendChild (BuildSingleItem (doc, ns, contents));
            return member;
        }

        private static XmlElement BuildSize (XmlDocument doc, string ns, string name, string value)
        {
            var size = doc.CreateElement (name, ns);
            size.InnerText = value ?? "0in";
            return size;
        }

        private static string HeaderSize (XmlElement member)
            => FindChild (FindChild (member, "TablixHeader"), "Size")?.InnerText;

        private static XmlElement HeaderContents (XmlElement member)
            => FindChild (FindChild (member, "TablixHeader"), "CellContents");

        /// <summary>Tablix's Group carries its name as an attribute, exactly like 2005's Grouping.</summary>
        private static XmlElement BuildGrouping (XmlDocument doc, string ns, XmlElement group)
        {
            var grouping = doc.CreateElement ("Grouping", ns);

            var name = group.GetAttribute ("Name");
            if (!string.IsNullOrEmpty (name))
                grouping.SetAttribute ("Name", name);

            foreach (var child in Children (group))
                grouping.AppendChild (child.CloneNode (true));

            return grouping;
        }

        /// <summary>2008 SortExpressions/SortExpression{Value,Direction} to 2005 Sorting/SortBy.</summary>
        private static XmlElement BuildSorting (XmlDocument doc, string ns, XmlElement sortExpressions)
        {
            var sorting = doc.CreateElement ("Sorting", ns);

            foreach (var sortExpression in ChildrenNamed (sortExpressions, "SortExpression")) {
                var sortBy = doc.CreateElement ("SortBy", ns);

                var expression = doc.CreateElement ("SortExpression", ns);
                expression.InnerText = FindChild (sortExpression, "Value")?.InnerText ?? string.Empty;
                sortBy.AppendChild (expression);

                var direction = FindChild (sortExpression, "Direction");
                if (direction != null)
                    sortBy.AppendChild (direction.CloneNode (true));

                sorting.AppendChild (sortBy);
            }

            return sorting;
        }

        /// <summary>
        /// ReportItems holding exactly one item, from CellContents that may hold none or several.
        /// The Matrix positions (DynamicColumns, StaticRow, MatrixCell, Corner) all require it.
        /// </summary>
        private static XmlElement BuildSingleItem (XmlDocument doc, string ns, XmlElement contents)
        {
            var items = doc.CreateElement ("ReportItems", ns);

            if (contents != null) {
                foreach (var child in Children (contents)) {
                    if (child.LocalName == "ColSpan" || child.LocalName == "RowSpan")
                        continue;

                    items.AppendChild (child.CloneNode (true));
                }
            }

            if (items.ChildNodes.Count == 0)
                items.AppendChild (BuildEmptyTextbox (doc, ns));
            else if (items.ChildNodes.Count > 1)
                items = WrapInRectangle (doc, ns, items);

            return items;
        }

        /// <summary>
        /// The Tablix corner is a grid of cells over the row-header columns; 2005's Corner holds a
        /// single item. One cell maps directly; several are laid out inside a Rectangle, at offsets
        /// taken from the row-level widths and column-level heights when the units allow it.
        /// </summary>
        private static XmlElement BuildCorner (XmlDocument doc, string ns, XmlElement tablixCorner,
            List<AxisLevel> rowLevels, List<AxisLevel> columnLevels, ReportLog rl, string tablixName)
        {
            if (tablixCorner == null)
                return null;

            var items = new List<XmlElement> ();
            var cornerRows = ChildrenNamed (FindChild (tablixCorner, "TablixCornerRows"), "TablixCornerRow");

            for (var rowIndex = 0; rowIndex < cornerRows.Count; rowIndex++) {
                var cells = ChildrenNamed (cornerRows[rowIndex], "TablixCornerCell");
                for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++) {
                    var contents = FindChild (cells[cellIndex], "CellContents");
                    if (contents == null)
                        continue;   // spanned-over placeholder

                    foreach (var child in Children (contents)) {
                        if (child.LocalName == "ColSpan" || child.LocalName == "RowSpan")
                            continue;

                        var item = (XmlElement)child.CloneNode (true);
                        PositionCornerItem (doc, ns, item, cellIndex, rowIndex, rowLevels, columnLevels);
                        items.Add (item);
                    }
                }
            }

            if (items.Count == 0)
                return null;

            var corner = doc.CreateElement ("Corner", ns);
            if (items.Count == 1) {
                var single = doc.CreateElement ("ReportItems", ns);
                single.AppendChild (items[0]);
                corner.AppendChild (single);
            } else {
                var grouped = doc.CreateElement ("ReportItems", ns);
                foreach (var item in items)
                    grouped.AppendChild (item);
                corner.AppendChild (WrapInRectangle (doc, ns, grouped));
            }

            return corner;
        }

        /// <summary>
        /// Sets Top/Left/Width/Height on a corner item from the sizes of the levels before it.
        /// Sizes in mixed units are left unset; the layout degrades but nothing is lost.
        /// </summary>
        private static void PositionCornerItem (XmlDocument doc, string ns, XmlElement item,
            int cellIndex, int rowIndex, List<AxisLevel> rowLevels, List<AxisLevel> columnLevels)
        {
            string LevelSize (List<AxisLevel> levels, int index)
            {
                if (index >= levels.Count)
                    return null;

                var level = levels[index];
                if (level.Dynamic != null)
                    return HeaderSize (level.Dynamic) ?? "0in";

                foreach (var member in level.Statics) {
                    var s = HeaderSize (member);
                    if (s != null)
                        return s;
                }
                return "0in";
            }

            void Set (string elementName, string value)
            {
                if (value == null || FindChild (item, elementName) != null)
                    return;

                var element = doc.CreateElement (elementName, ns);
                element.InnerText = value;
                item.AppendChild (element);
            }

            string Sum (Func<int, string> sizeAt, int count)
            {
                double total = 0;
                string unit = null;
                for (var i = 0; i < count; i++) {
                    var size = sizeAt (i);
                    if (size == null)
                        return null;

                    var parsed = ParseSize (size);
                    if (parsed == null || (unit != null && unit != parsed.Value.Unit))
                        return null;

                    unit = parsed.Value.Unit;
                    total += parsed.Value.Value;
                }
                return unit == null ? "0in" : FormatSize (total, unit);
            }

            Set ("Left", cellIndex == 0 ? "0in" : Sum (i => LevelSize (rowLevels, i), cellIndex));
            Set ("Top", rowIndex == 0 ? "0in" : Sum (i => LevelSize (columnLevels, i), rowIndex));
            Set ("Width", LevelSize (rowLevels, cellIndex));
            Set ("Height", LevelSize (columnLevels, rowIndex));
        }

        private static (double Value, string Unit)? ParseSize (string size)
        {
            var match = System.Text.RegularExpressions.Regex.Match (size.Trim (),
                @"^([\d.]+)\s*([a-zA-Z]+)$");
            if (!match.Success)
                return null;

            if (!double.TryParse (match.Groups[1].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                return null;

            return (value, match.Groups[2].Value.ToLowerInvariant ());
        }

        private static string FormatSize (double value, string unit)
            => value.ToString ("0.#####", System.Globalization.CultureInfo.InvariantCulture) + unit;

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

        private const string ReportDesignerNamespace =
            "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";

        private static void RemoveVersionOnlyElements (XmlElement report)
        {
            foreach (var name in VersionOnlyElements) {
                foreach (var element in FindDescendants (report, name))
                    element.ParentNode?.RemoveChild (element);
            }

            // Designer-namespace (rd:) elements are authoring metadata with no runtime meaning.
            // Inside most containers they are merely noisy; inside ReportItems the parser treats
            // them as dropped report items, which downstream consumers rightly refuse to render.
            var designerElements = new List<XmlElement> ();
            CollectDesignerElements (report, designerElements);
            foreach (var element in designerElements)
                element.ParentNode?.RemoveChild (element);
        }

        private static void CollectDesignerElements (XmlElement parent, List<XmlElement> found)
        {
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is not XmlElement element)
                    continue;
                if (string.Equals (element.NamespaceURI, ReportDesignerNamespace, StringComparison.OrdinalIgnoreCase))
                    found.Add (element);
                else
                    CollectDesignerElements (element, found);
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
