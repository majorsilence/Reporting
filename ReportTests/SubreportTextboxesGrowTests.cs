using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A subreport in the body whose content is free-standing textboxes, with no table or list, pushes
    /// down the items below it when its content reaches below its designed bottom, and keeps its
    /// designed height when its content fits it. The subreport's end was the page's running offset
    /// after its body ran, which only a table, list or matrix moves on, so it ended where it started:
    /// the item below it printed over the subreport's content, or moved up into its designed space.
    /// </summary>
    [TestFixture]
    public class SubreportTextboxesGrowTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-subreport-textboxes-grow-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "Tall.rdl"), SubRdl("2in", """
                <Textbox Name="First"><Top>0.5in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height><Value>SUB-FIRST</Value></Textbox>
                <Textbox Name="Last"><Top>1.75in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height><Value>SUB-LAST</Value></Textbox>
                """));
            File.WriteAllText(Path.Combine(_folder, "Short.rdl"), SubRdl("0.5in", """
                <Textbox Name="Only"><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height><Value>SUB-ONLY</Value></Textbox>
                """));
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private static string SubRdl(string height, string items) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <Body>
                <Height>{height}</Height>
                <ReportItems>
                  {items}
                </ReportItems>
              </Body>
            </Report>
            """;

        // A subreport placed in a 0.5in or 2in item at the top of the body, with an item below it.
        private static string Rdl(string subreport, string subreportHeight, string belowTop) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
              <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
              <Width>7.5in</Width>
              <Body>
                <Height>4in</Height>
                <ReportItems>
                  <Subreport Name="Content">
                    <ReportName>{subreport}</ReportName>
                    <Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>{subreportHeight}</Height>
                  </Subreport>
                  <Textbox Name="Below">
                    <Top>{belowTop}</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>BELOW</Value>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        private async Task<List<PageText>> Texts(string rdl)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            return pages.Cast<Page>().First().Cast<PageItem>().OfType<PageText>().ToList();
        }

        private static PageText Text(List<PageText> texts, string text)
            => texts.Single(t => t.Text == text);

        [Test]
        public async Task SubreportOfTextboxes_PushesDownTheItemBelowIt()
        {
            // The subreport is designed 0.5in tall, and its content reaches 2in down; the item below
            // is designed 0.4in under the subreport, and stays that far under its content.
            var texts = await Texts(Rdl("Tall", "0.5in", "0.9in"));
            var last = Text(texts, "SUB-LAST");
            var below = Text(texts, "BELOW");

            Assert.That(last.Y + last.H, Is.EqualTo(36f + 2 * 72f).Within(0.01f));
            Assert.That(below.Y, Is.EqualTo(last.Y + last.H + 0.4f * 72).Within(0.01f),
                "The item below was drawn over the subreport");
        }

        [Test]
        public async Task SubreportOfTextboxesThatFits_KeepsItsDesignedHeight()
        {
            // The content ends 0.25in down a 2in subreport: the item below stays at its designed Top.
            var texts = await Texts(Rdl("Short", "2in", "2.5in"));
            var below = Text(texts, "BELOW");

            Assert.That(Text(texts, "SUB-ONLY").Y, Is.EqualTo(36f).Within(0.01f));
            Assert.That(below.Y, Is.EqualTo(36f + 2.5f * 72).Within(0.01f));
        }
    }
}
