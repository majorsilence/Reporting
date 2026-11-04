#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// A tab in a textbox's text goes to the next tab stop, every half inch from where the text
    /// starts, as Word places it, or as far apart as the box's TabStopInterval says (Crystal
    /// Reports' are a quarter inch). RDL has no tab stops, and the tab was drawn as whatever the
    /// font makes of the character, usually nothing, so a report header such as
    /// "Printed Date: 1/2/2026&lt;tab&gt;&lt;tab&gt;Last modified: ..." printed its halves run together.
    /// The tab becomes the spaces that reach the stop in the textbox's own font.
    /// </summary>
    [TestFixture]
    public class TabStopTests
    {
        private static string Rdl(string text, string custom = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Tabbed"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>{custom}
      <Value>{text}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<string> Printed(string text, string custom = "")
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(text, custom)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text;
        }

        private static int SpacesAfter(string printed, string prefix)
        {
            Assert.That(printed, Does.StartWith(prefix));
            return printed.Substring(prefix.Length).TakeWhile(c => c == ' ').Count();
        }

        [Test]
        public async Task ATab_IsSpacesToTheNextHalfInch()
        {
            string printed = await Printed("A&#9;B");

            Assert.That(printed, Does.Not.Contain("\t"));
            Assert.That(printed, Does.EndWith("B"));
            // "A" in 10pt Arial is about 6.7pt and a space about 2.8pt: about ten spaces to 36pt.
            Assert.That(SpacesAfter(printed, "A"), Is.InRange(8, 13));
        }

        [Test]
        public async Task TwoTabs_GoToTheSecondStop()
        {
            int one = SpacesAfter(await Printed("A&#9;B"), "A");
            int two = SpacesAfter(await Printed("A&#9;&#9;B"), "A");

            // Another 36pt is about thirteen more spaces.
            Assert.That(two - one, Is.InRange(10, 16));
        }

        [Test]
        public async Task LongerTextBeforeATab_GetsFewerSpaces()
        {
            int shortPrefix = SpacesAfter(await Printed("A&#9;B"), "A");
            int longPrefix = SpacesAfter(await Printed("AAAA&#9;B"), "AAAA");

            Assert.That(longPrefix, Is.LessThan(shortPrefix));
            Assert.That(longPrefix, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task TextWithoutTabs_IsUnchanged()
        {
            Assert.That(await Printed("No tabs here"), Is.EqualTo("No tabs here"));
        }

        private static string Interval(string size) =>
            $"<Custom><TabStopInterval>{size}</TabStopInterval></Custom>";

        // A quarter inch, as Crystal Reports sets its tab stops: "A" and about four spaces to 18pt.
        [Test]
        public async Task ATabStopInterval_SetsHowFarApartTheStopsAre()
        {
            int halfInch = SpacesAfter(await Printed("A&#9;B"), "A");
            int quarterInch = SpacesAfter(await Printed("A&#9;B", Interval("18pt")), "A");
            int twoQuarters = SpacesAfter(await Printed("A&#9;&#9;B", Interval("0.25in")), "A");

            Assert.Multiple(() =>
            {
                Assert.That(quarterInch, Is.InRange(3, 5));
                Assert.That(quarterInch, Is.LessThan(halfInch));
                // The second stop is another 18pt on, about six more spaces.
                Assert.That(twoQuarters - quarterInch, Is.InRange(5, 8));
            });
        }

        [TestCase("")]
        [TestCase("0pt")]
        [TestCase("wide")]
        public async Task AnIntervalThatIsNotAPositiveSize_LeavesTheHalfInch(string size)
        {
            int halfInch = SpacesAfter(await Printed("A&#9;B"), "A");

            Assert.That(SpacesAfter(await Printed("A&#9;B", Interval(size)), "A"), Is.EqualTo(halfInch));
        }

        // A paragraph set in by tabs keeps its indent on its first line when it wraps. The spaces
        // the tabs became were taken as empty words when the paragraph was wrapped to the box,
        // and dropped, so the first line printed at the margin.
        [Test]
        public async Task LeadingTabs_KeepTheirIndent_WhenTheParagraphWraps()
        {
            var lines = await PdfLineStarts("Tabs_indent_wrapped",
                "&#9;&#9;Indented words that run on for long enough to wrap onto a second line of the box");
            double left = 36;   // half-inch margin

            Assert.That(lines.Length, Is.GreaterThan(1), "the paragraph wraps");
            Assert.Multiple(() =>
            {
                Assert.That(lines[0], Is.EqualTo(left + 72).Within(1.5), "two half-inch stops in");
                Assert.That(lines[1], Is.EqualTo(left).Within(0.5), "the next line starts at the margin");
            });
        }

        // Where each printed line starts, top line first, in a 3in box at the page's half-inch margin.
        private static async Task<double[]> PdfLineStarts(string name, string text)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            string rdlPath = Path.Combine(outputFolder.LocalPath, name + ".rdl");
            File.WriteAllText(rdlPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
  <Width>7.5in</Width>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name=""T"">
      <Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>1.5in</Height>
      <CanGrow>true</CanGrow>
      <Value>{text}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>
  </ReportItems></Body>
</Report>");

            Report report = await RdlUtils.GetReport(new System.Uri(rdlPath));
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(outputFolder.LocalPath, name + ".pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            return pdf.GetPages().Single().Letters
                .Where(l => l.Value.Trim().Length > 0)
                .GroupBy(l => System.Math.Round(l.StartBaseLine.Y, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => g.Min(l => l.StartBaseLine.X))
                .ToArray();
        }
    }
}
#endif
