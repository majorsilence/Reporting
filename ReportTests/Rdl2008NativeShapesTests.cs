#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The RDL 2008+ spellings that the engine reads itself rather than the normaliser rewriting
    /// them (#427 phase 1): Page, edge borders, ActionInfo and NoRowsMessage.
    /// </summary>
    [TestFixture]
    public class Rdl2008NativeShapesTests
    {
        private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string Tb(string name, string value, string extra)
            => $@"<Textbox Name=""{name}"">{extra}<Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        private const string Position = "<Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>";

        private static string Report(string bodyItems, string page) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Ns}"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data""><Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""N""><DataField>N</DataField></Field></Fields></DataSet></DataSets>
  <Body><Height>2in</Height><ReportItems>{bodyItems}</ReportItems></Body>
  <Width>3in</Width>
  <Page>{page}</Page>
</Report>";

        private static async Task<(Report Report, List<PageText> Texts, string Errors)> Render(string rdl, DataTable data = null)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), errors);
            if (data != null)
                await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return (report, pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().ToList(), errors);
        }

        [Test]
        public async Task Page_SetsThePageSizeMarginsHeaderAndColumns()
        {
            var (report, texts, errors) = await Render(Report(
                Tb("B", "body", Position),
                @"<PageHeader><Height>0.5in</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage>
                    <ReportItems>" + Tb("H", "header", Position) + @"</ReportItems></PageHeader>
                  <PageHeight>5in</PageHeight><PageWidth>4in</PageWidth><TopMargin>0.25in</TopMargin>
                  <Columns>2</Columns><ColumnSpacing>0.25in</ColumnSpacing>
                  <InteractiveHeight>8in</InteractiveHeight>
                  <Style><Border><Style>Solid</Style></Border></Style>"));
            using (report)
            {
                Assert.That(errors, Does.Not.Contain("Unknown"));
                Assert.That(report.PageHeightPoints, Is.EqualTo(360f).Within(0.1f));
                Assert.That(texts.Select(t => t.Text), Does.Contain("header"));
                Assert.That(report.ReportDefinition.Body.Columns, Is.EqualTo(2));
                Assert.That(report.ReportDefinition.Body.ColumnSpacing.Points, Is.EqualTo(18f).Within(0.1f));
            }
        }

        [Test]
        public async Task EdgeBorders_AreReadPerEdge()
        {
            var (report, texts, errors) = await Render(Report(
                Tb("B", "boxed", Position + @"<Style>
                    <Border><Color>Red</Color><Style>Solid</Style><Width>2pt</Width></Border>
                    <BottomBorder><Style>Dashed</Style></BottomBorder></Style>"),
                "<PageHeight>5in</PageHeight><PageWidth>4in</PageWidth>"));
            using (report)
            {
                Assert.That(errors, Does.Not.Contain("Unknown"));
                var si = texts.Single(t => t.Text == "boxed").SI;
                Assert.That(si.BStyleTop, Is.EqualTo(BorderStyleEnum.Solid));
                Assert.That(si.BStyleBottom, Is.EqualTo(BorderStyleEnum.Dashed));
                Assert.That(si.BColorLeft.Name, Is.EqualTo("Red").IgnoreCase.Or.EqualTo("ffff0000"));
                Assert.That(si.BWidthRight, Is.EqualTo(2f).Within(0.05f));
            }
        }

        [Test]
        public async Task ActionInfo_TheFirstActionIsFollowed()
        {
            var (report, texts, errors) = await Render(Report(
                Tb("Link", "go", Position + @"<ActionInfo><Actions>
                    <Action><Hyperlink>http://example.com/first</Hyperlink></Action>
                    <Action><Hyperlink>http://example.com/second</Hyperlink></Action>
                  </Actions></ActionInfo>"),
                "<PageHeight>5in</PageHeight><PageWidth>4in</PageWidth>"));
            using (report)
            {
                Assert.That(errors, Does.Not.Contain("Unknown"));
                Assert.That(texts.Single(t => t.Text == "go").HyperLink, Is.EqualTo("http://example.com/first"));
            }
        }

        [Test]
        public async Task NoRowsMessage_IsShownForAnEmptyRegion()
        {
            string tablix = $@"<Tablix Name=""T"">
                <TablixBody><TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
                  <TablixRows><TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents>{Tb("C", "=Fields!N.Value", "")}</CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
                </TablixBody>
                <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
                <TablixRowHierarchy><TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers></TablixRowHierarchy>
                <NoRowsMessage>Nothing to show</NoRowsMessage>
                <DataSetName>Data</DataSetName>{Position}
              </Tablix>";
            var empty = new DataTable();
            empty.Columns.Add("N", typeof(string));
            var (report, texts, errors) = await Render(Report(tablix, "<PageHeight>5in</PageHeight><PageWidth>4in</PageWidth>"), empty);
            using (report)
            {
                Assert.That(errors, Does.Not.Contain("Unknown"));
                Assert.That(texts.Select(t => t.Text), Does.Contain("Nothing to show"));
            }
        }
    }
}
#endif
