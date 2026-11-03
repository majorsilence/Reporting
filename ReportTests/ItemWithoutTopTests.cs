using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A report item without a Top is at 0, as RDL has it, and pushes down the items below it when it
    /// grows (#544). Designers leave Top out for an item at the top of its container, and the engine
    /// left such an item out when it worked out which items sit above another, so the items below a
    /// growing subreport or textbox at the top stayed at their designed Top and were drawn over it.
    /// </summary>
    [TestFixture]
    public class ItemWithoutTopTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-item-without-top-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "Header.rdl"), HeaderRdl);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        // A header subreport: a table of six 0.25in rows, placed in a 0.25in item.
        private const string HeaderRdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets><DataSet Name="Lines"><Query><DataSourceName>Pushed</DataSourceName><CommandText>none</CommandText></Query>
                <Fields><Field Name="Line"><DataField>Line</DataField></Field></Fields></DataSet></DataSets>
              <Body>
                <Height>0.25in</Height>
                <ReportItems>
                  <Table Name="HeaderLines">
                    <DataSetName>Lines</DataSetName>
                    <Top>0in</Top><Left>0in</Left>
                    <TableColumns><TableColumn><Width>6in</Width></TableColumn></TableColumns>
                    <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
                      <TableCell><ReportItems><Textbox Name="HeaderLine"><Value>=Fields!Line.Value</Value></Textbox></ReportItems></TableCell>
                    </TableCells></TableRow></TableRows></Details>
                  </Table>
                </ReportItems>
              </Body>
            </Report>
            """;

        private static DataTable HeaderLines()
        {
            var table = new DataTable();
            table.Columns.Add("Line", typeof(string));
            for (int i = 1; i <= 6; i++)
                table.Rows.Add("HEADER-" + i);
            return table;
        }

        // The item below comes first, as the issue's report has it, so only its Top puts it below.
        private static string Rdl(string grower) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
              <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
              <Width>7.5in</Width>
              <Body>
                <Height>2in</Height>
                <ReportItems>
                  <Textbox Name="Below">
                    <Top>0.4in</Top><Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
                    <Value>BELOW</Value>
                  </Textbox>
                  {grower}
                </ReportItems>
              </Body>
            </Report>
            """;

        private static string Subreport(string top) => $"""
            <Subreport Name="Header">
              <ReportName>Header</ReportName>
              {top}<Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
            </Subreport>
            """;

        private static string GrowingTextbox(string top) => $"""
            <Textbox Name="Grower">
              {top}<Left>0in</Left><Width>6in</Width><Height>0.25in</Height>
              <CanGrow>true</CanGrow>
              <Value>GROW-1&#10;GROW-2&#10;GROW-3&#10;GROW-4&#10;GROW-5&#10;GROW-6</Value>
              <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize></Style>
            </Textbox>
            """;

        private async Task<List<PageText>> Texts(string rdl)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            report.SubreportDataRetrieval += (_, e) => e.Report.DataSets["Lines"].SetData(HeaderLines()).GetAwaiter().GetResult();
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            return pages.Cast<Page>().First().Cast<PageItem>().OfType<PageText>().ToList();
        }

        private static PageText Text(List<PageText> texts, string startsWith)
            => texts.Single(t => t.Text != null && t.Text.StartsWith(startsWith));

        [Test]
        public async Task SubreportWithoutTop_PushesDownTheItemBelowIt()
        {
            var texts = await Texts(Rdl(Subreport(top: "")));
            var last = Text(texts, "HEADER-6");
            var below = Text(texts, "BELOW");

            Assert.That(below.Y, Is.GreaterThanOrEqualTo(last.Y + last.H - 0.01f),
                "The item below was drawn over the subreport");

            var withTop = await Texts(Rdl(Subreport(top: "<Top>0in</Top>")));
            Assert.That(below.Y, Is.EqualTo(Text(withTop, "BELOW").Y).Within(0.01f),
                "A missing Top should place the items below as Top 0 does");
        }

        [Test]
        public async Task GrowingTextboxWithoutTop_PushesDownTheItemBelowIt()
        {
            var texts = await Texts(Rdl(GrowingTextbox(top: "")));
            var grower = Text(texts, "GROW-1");
            var below = Text(texts, "BELOW");

            Assert.That(grower.H, Is.GreaterThan(0.25f * 72 * 2), "The textbox should have grown");
            Assert.That(below.Y, Is.GreaterThanOrEqualTo(grower.Y + grower.H - 0.01f),
                "The item below was drawn over the textbox");

            var withTop = await Texts(Rdl(GrowingTextbox(top: "<Top>0in</Top>")));
            Assert.That(below.Y, Is.EqualTo(Text(withTop, "BELOW").Y).Within(0.01f),
                "A missing Top should place the items below as Top 0 does");
        }
    }
}
