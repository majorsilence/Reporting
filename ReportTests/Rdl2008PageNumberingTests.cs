#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// RDL 2008+ PageBreak (Disabled, ResetPageNumber, Between), PageName and InitialPageName, and
    /// Globals!OverallPageNumber, OverallTotalPages and PageName (#427 phase 1).
    /// </summary>
    [TestFixture]
    public class Rdl2008PageNumberingTests
    {
        private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string Tb(string name, string value, string position = "")
            => $@"<Textbox Name=""{name}"">{position}<Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        // A Tablix of one 0.25in detail row per record, grouped by Name, on 4in pages whose 0.5in
        // footer prints "{footer}": 14 rows to a page.
        private static string Report(string groupExtras, string footer, string reportExtras = "")
            => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Ns}"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""N""><DataField>N</DataField></Field></Fields>
  </DataSet></DataSets>
  {reportExtras}
  <Body><Height>0.25in</Height><ReportItems>
    <Tablix Name=""T1"">
      <TablixBody>
        <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
        <TablixRows><TablixRow><Height>0.25in</Height><TablixCells>
          <TablixCell><CellContents>{Tb("Row", "=Fields!N.Value")}</CellContents></TablixCell>
        </TablixCells></TablixRow></TablixRows>
      </TablixBody>
      <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
      <TablixRowHierarchy><TablixMembers>
        <TablixMember>
          <Group Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions>{groupExtras}</Group>
          <TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers>
        </TablixMember>
      </TablixMembers></TablixRowHierarchy>
      <DataSetName>Data</DataSetName>
      <Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>
    </Tablix>
  </ReportItems></Body>
  <Width>3in</Width>
  <Page>
    <PageFooter><Height>0.5in</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage>
      <ReportItems>{Tb("Foot", footer, "<Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>3in</Width>")}</ReportItems>
    </PageFooter>
    <PageHeight>4in</PageHeight><PageWidth>3in</PageWidth>
    <TopMargin>0in</TopMargin><BottomMargin>0in</BottomMargin><LeftMargin>0in</LeftMargin><RightMargin>0in</RightMargin>
  </Page>
</Report>";

        private const string Numbers = @"=Globals!PageNumber &amp; ""/"" &amp; Globals!TotalPages &amp; "" "" &amp; Globals!OverallPageNumber &amp; ""/"" &amp; Globals!OverallTotalPages";

        // 20 records named A (two pages), then 2 named B.
        private static DataTable Data()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("N", typeof(string));
            for (int i = 1; i <= 20; i++)
                table.Rows.Add("A", "a" + i);
            table.Rows.Add("B", "b1");
            table.Rows.Add("B", "b2");
            return table;
        }

        private static async Task<List<Page>> Render(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), errors);
            Assert.That(errors, Does.Not.Contain("PageBreak").And.Not.Contain("PageName"));
            await report.DataSets["Data"].SetData(Data());
            await report.RunGetData(null);
            return (await report.BuildPages()).Cast<Page>().ToList();
        }

        private static string[] Footers(List<Page> pages)
            => pages.Select(p => p.Cast<PageItem>().OfType<PageText>().Last().Text).ToArray();

        [Test]
        public async Task ResetPageNumber_RestartsPageNumberAndTotalPages_NotTheOverallOnes()
        {
            var pages = await Render(Report(
                @"<PageBreak><BreakLocation>Between</BreakLocation><ResetPageNumber>true</ResetPageNumber></PageBreak>",
                Numbers));

            Assert.That(Footers(pages), Is.EqualTo(new[] { "1/2 1/3", "2/2 2/3", "1/1 3/3" }));
        }

        [Test]
        public async Task WithoutResetPageNumber_PageNumbersRunOn()
        {
            var pages = await Render(Report(@"<PageBreak><BreakLocation>Between</BreakLocation></PageBreak>", Numbers));

            Assert.That(Footers(pages), Is.EqualTo(new[] { "1/3 1/3", "2/3 2/3", "3/3 3/3" }));
        }

        [Test]
        public async Task Between_DoesNotBreakAfterTheLastInstance()
        {
            var pages = await Render(Report(@"<PageBreak><BreakLocation>Between</BreakLocation></PageBreak>", "x"));

            Assert.That(pages, Has.Count.EqualTo(3));
        }

        [Test]
        public async Task Disabled_TurnsTheBreakOff()
        {
            var pages = await Render(Report(
                @"<PageBreak><BreakLocation>Between</BreakLocation><Disabled>true</Disabled></PageBreak>", Numbers));

            // 22 rows run on: 14 on the first page, 8 on the second.
            Assert.That(Footers(pages), Is.EqualTo(new[] { "1/2 1/2", "2/2 2/2" }));
        }

        [Test]
        public async Task GroupPageName_NamesThePagesEachInstanceStarts()
        {
            var pages = await Render(Report(
                @"<PageBreak><BreakLocation>Between</BreakLocation></PageBreak><PageName>=""Dept "" &amp; Fields!Name.Value</PageName>",
                "=Globals!PageName"));

            Assert.That(Footers(pages), Is.EqualTo(new[] { "Dept A", "Dept A", "Dept B" }));
        }

        [Test]
        public async Task InitialPageName_NamesThePagesUntilAnItemNamesOne()
        {
            var pages = await Render(Report(
                @"<PageBreak><BreakLocation>Between</BreakLocation></PageBreak>",
                "=Globals!PageName",
                reportExtras: "<InitialPageName>Start</InitialPageName>"));

            Assert.That(Footers(pages), Is.EqualTo(new[] { "Start", "Start", "Start" }));
        }

        // A rectangle's 2008 PageBreak was dropped as an unknown element.
        [Test]
        public async Task ARectanglesPageBreak_BreaksThePage()
        {
            string rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Ns}"">
  <Body><Height>2in</Height><ReportItems>
    {Tb("Before", "before", "<Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>")}
    <Rectangle Name=""R"">
      <ReportItems>{Tb("Inside", "inside", "<Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>")}</ReportItems>
      <PageBreak><BreakLocation>Start</BreakLocation><ResetPageNumber>true</ResetPageNumber></PageBreak>
      <Top>0.5in</Top><Left>0in</Left><Height>0.5in</Height><Width>2in</Width>
    </Rectangle>
  </ReportItems></Body>
  <Width>3in</Width>
  <Page><PageHeight>4in</PageHeight><PageWidth>3in</PageWidth></Page>
</Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            await report.RunGetData(null);
            var pages = (await report.BuildPages()).Cast<Page>().ToList();

            Assert.That(pages, Has.Count.EqualTo(2));
            Assert.That(pages[1].Cast<PageItem>().OfType<PageText>().Select(t => t.Text), Does.Contain("inside"));
        }

        // A region's PageBreakAtStart was never acted on (DataRegion.PageBreakCondition was
        // always false), so a table asking to start on a new page did not.
        [Test]
        public async Task ATablesPageBreakAtStart_BreaksThePage()
        {
            string rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>3in</Width>
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""N""><DataField>N</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name=""Before""><Value>before</Value><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width></Textbox>
    <Table Name=""T""><DataSetName>Data</DataSetName><PageBreakAtStart>true</PageBreakAtStart><Top>0.5in</Top>
      <TableColumns><TableColumn><Width>2in</Width></TableColumn></TableColumns>
      <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""Cell""><Value>=Fields!N.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>4in</PageHeight><PageWidth>3in</PageWidth>
</Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            var table = Data();
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = (await report.BuildPages()).Cast<Page>().ToList();

            Assert.That(pages[0].Cast<PageItem>().OfType<PageText>().Select(t => t.Text), Does.Contain("before").And.Not.Contain("a1"));
            Assert.That(pages[1].Cast<PageItem>().OfType<PageText>().Select(t => t.Text), Does.Contain("a1"));
        }

        [Test]
        public void TheDesignerRoundTrip_KeepsDisabledAndResetPageNumber()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Report(
                @"<PageBreak><BreakLocation>Between</BreakLocation><Disabled>=False</Disabled><ResetPageNumber>true</ResetPageNumber></PageBreak>",
                "x"));

            Rdl2008Exporter.NormalizeForEditing(doc);
            Rdl2008Exporter.ConvertToTablix(doc);

            var group = doc.GetElementsByTagName("Group").Cast<XmlElement>().Single(g => g.GetAttribute("Name") == "ByName");
            var breaks = group.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "PageBreak").ToList();
            Assert.That(breaks, Has.Count.EqualTo(1), group.OuterXml);
            Assert.That(breaks[0]["BreakLocation"]?.InnerText, Is.EqualTo("Between"));
            Assert.That(breaks[0]["Disabled"]?.InnerText, Is.EqualTo("=False"));
            Assert.That(breaks[0]["ResetPageNumber"]?.InnerText, Is.EqualTo("true"));
            Assert.That(group["PageBreakAtEnd"], Is.Null);
        }
    }
}
#endif
