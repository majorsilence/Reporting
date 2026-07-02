#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Regression tests for HTML output drifting from the PDF rendering of the same report.
    /// RenderHtml positions every report item with absolute CSS coordinates taken from the
    /// RDL design (top/left/height), the same way the PDF renderer lays things out. Anything
    /// RenderHtml measures or sizes differently than the PDF renderer throws off every item
    /// positioned below it, since those still use their fixed absolute "top" from the design.
    /// </summary>
    [TestFixture]
    public class HtmlRenderRegressionTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<string> RenderHtml(string reportFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, $"Report '{reportFile}' failed to parse (fatal errors)");

            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task MeetingMinutesReport_TableRowsCarryTheirRdlDefinedHeight()
        {
            // Bug: TableRowStart() never wrote the RDL <TableRow><Height> onto the <tr>, so
            // the browser sized rows purely from font metrics instead. Everything the RDL
            // positions below a table (section headings, rule bars, the next table) still
            // sits at its fixed absolute "top" from the design, so any drift between the
            // browser's row height and the design height opened a gap (or overlap) between
            // sections that isn't there in the PDF rendering of the same report.
            string html = await RenderHtml("meeting-minutes1-a4.rdl");

            // meeting-minutes1-a4.rdl/.json define 3 header rows (0.2763in), 8 attendee rows
            // (0.2551in), 4 agenda rows (0.5845in) and 5 action rows (0.3188in), plus the
            // single outer body-positioning <tr> that RenderHtml always emits with no style.
            AssertRowCountWithHeight(html, "0.2763in", 3);
            AssertRowCountWithHeight(html, "0.2551in", 8);
            AssertRowCountWithHeight(html, "0.5845in", 4);
            AssertRowCountWithHeight(html, "0.3188in", 5);

            int unstyledRows = Regex.Matches(html, "<tr>").Count;
            Assert.That(unstyledRows, Is.EqualTo(1),
                "Only RenderHtml's own body-positioning row should be missing a height; every RDL TableRow should carry one");
        }

        private static void AssertRowCountWithHeight(string html, string height, int expectedCount)
        {
            string pattern = $"<tr style=\"height:\\s*{Regex.Escape(height)};\"";
            int actualCount = Regex.Matches(html, pattern).Count;
            Assert.That(actualCount, Is.EqualTo(expectedCount),
                $"Expected {expectedCount} table row(s) with height {height}, found {actualCount}");
        }

        [Test]
        public async Task MeetingMinutesReport_HeaderCellsDefaultToLeftAlignExceptWhereCentered()
        {
            // Bug: browsers default <th> to center-aligned text (unlike <td>, which defaults
            // to left), but the RDL's "General" alignment - used by any header textbox with no
            // explicit <TextAlign>, e.g. Name/Title, Topic/Discussion/Owner, Action/Due Date -
            // means left, same as the PDF renderer and the <td> data rows underneath. Only
            // cells with an explicit <TextAlign>Center</TextAlign> in the RDL (#, Present,
            // Status, Priority) should end up centered.
            string html = await RenderHtml("meeting-minutes1-a4.rdl");

            Assert.That(html, Does.Match(@"(?i)th\s*\{[^}]*text-align:\s*left"),
                "No default left text-align rule found for <th> cells");

            AssertHeaderTextAlign(html, "Name", expectCenter: false);
            AssertHeaderTextAlign(html, "Title", expectCenter: false);
            AssertHeaderTextAlign(html, "Present", expectCenter: true);
            AssertHeaderTextAlign(html, "Topic", expectCenter: false);
            AssertHeaderTextAlign(html, "Owner", expectCenter: false);
            AssertHeaderTextAlign(html, "Status", expectCenter: true);
            AssertHeaderTextAlign(html, "Action", expectCenter: false);
            AssertHeaderTextAlign(html, "Priority", expectCenter: true);
        }

        private static void AssertHeaderTextAlign(string html, string headerText, bool expectCenter)
        {
            var cellMatch = Regex.Match(html, $@"<th id='([^']+)'[^>]*>{Regex.Escape(headerText)}</th>");
            Assert.That(cellMatch.Success, Is.True, $"Could not find header cell '{headerText}'");
            string cssId = cellMatch.Groups[1].Value;

            var ruleMatch = Regex.Match(html, $@"th#{Regex.Escape(cssId)}\s*\{{([^}}]*)\}}");
            Assert.That(ruleMatch.Success, Is.True, $"Could not find CSS rule for header cell '{headerText}' (id {cssId})");
            bool hasExplicitCenter = ruleMatch.Groups[1].Value.IndexOf("text-align:Center", StringComparison.OrdinalIgnoreCase) >= 0;

            // A per-id rule that doesn't declare text-align at all still lets the bare "th"
            // default rule apply for that property, so the absence of an explicit center here
            // means the cell resolves to the sheet-wide left default - this mirrors real CSS
            // cascade behavior rather than any specific browser quirk.
            Assert.That(hasExplicitCenter, Is.EqualTo(expectCenter),
                $"Header cell '{headerText}' {(expectCenter ? "should" : "should not")} have an explicit centered text-align rule");
        }

        [Test]
        public async Task MeetingMinutesReport_BodyHasNoDefaultMargin()
        {
            // Bug: browsers apply a non-zero default margin to <body>, but every report item
            // is positioned with "position: absolute; left: ...; top: ...;" relative to it.
            // The header band's textbox is designed to bleed to the page edge (Top/Left 0in,
            // Width equal to the full 8.27in PageWidth), which the PDF renderer draws flush
            // against the page edge since it has no such default margin. Without resetting it,
            // HTML rendered the same header inset from the left/right/top by the browser's
            // default body margin instead of flush against the edge.
            string html = await RenderHtml("meeting-minutes1-a4.rdl");

            Assert.That(html, Does.Match(@"(?i)body\s*\{[^}]*margin:\s*0"),
                "No default zero-margin rule found for <body>");
        }

        [Test]
        public async Task MeetingMinutesReport_PositioningTableHasNoDefaultSpacingOrPadding()
        {
            // Bug: resetting <body>'s default margin wasn't enough - the report body is
            // wrapped in a plain <table><td> purely to host the "position: relative;" div that
            // every item is absolutely positioned against, and browsers default <table> to a
            // 2px border-spacing and <td>/<th> to a small default padding. Either one still
            // shifts that div (and everything positioned relative to it) away from the page
            // edge, the same way the <body> margin did, leaving a visible gap versus the PDF.
            string html = await RenderHtml("meeting-minutes1-a4.rdl");

            Assert.That(html, Does.Match(@"(?i)table\s*\{[^}]*border-spacing:\s*0"),
                "No default zero border-spacing rule found for <table>");
            Assert.That(html, Does.Match(@"(?i)td\s*,\s*th\s*\{[^}]*padding:\s*0"),
                "No default zero padding rule found for <td>/<th>");
        }

        [Test]
        public async Task ListReport_ListHeightIsAnAwaitedNumericValue()
        {
            // Bug: CssPosition() interpolated `l.HeightOfList(...)` directly into a format
            // string without awaiting the returned Task<float>, so a <List> report item's
            // CSS came out as "height: System.Threading.Tasks.Task`1[System.Single]pt;"
            // instead of a real measurement, breaking its layout (and everything positioned
            // after it) in HTML while the PDF rendering of the same report was unaffected.
            string html = await RenderHtml("ListReport.rdl");

            Assert.That(html, Does.Not.Contain("System.Threading.Tasks"),
                "An un-awaited Task leaked into the rendered CSS instead of its value");
            Assert.That(html, Does.Match(@"height:\s*\d+(\.\d+)?pt;"),
                "List item height did not render as a numeric point value");
        }
    }
}
#endif
