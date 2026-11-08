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
    /// Issue #601: an aggregate with no scope in a table's header or footer, such as
    /// <c>=Sum(Fields!Amt.Value)</c>, summed every row of the table's dataset. Its scope was the
    /// dataset, since a header or footer is in no group, so a table nested in a grouped List gave the
    /// grand total in every group, and a table with Filters counted the rows they removed. As in SSRS,
    /// the scope is now the table: the rows it shows.
    /// </summary>
    [TestFixture]
    public class TableAggregateDefaultScopeTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Table(string name, string filters) => $"""
            <Table Name="{name}">
              <DataSetName>Rows</DataSetName>
              <Top>0in</Top><Width>6in</Width>
              {filters}
              <TableColumns><TableColumn><Width>3in</Width></TableColumn><TableColumn><Width>3in</Width></TableColumn></TableColumns>
              <Header>
                <TableRows><TableRow><Height>.2in</Height><TableCells>
                  <TableCell><ReportItems><Textbox Name="{name}Hdr"><Height>.2in</Height><Width>3in</Width><Value>="Rows " &amp; Count(Fields!Item.Value)</Value></Textbox></ReportItems></TableCell>
                  <TableCell><ReportItems><Textbox Name="{name}HdrMax"><Height>.2in</Height><Width>3in</Width><Value>="Max " &amp; Max(Fields!Amt.Value)</Value></Textbox></ReportItems></TableCell>
                </TableCells></TableRow></TableRows>
              </Header>
              <Details>
                <TableRows><TableRow><Height>.2in</Height><TableCells>
                  <TableCell><ReportItems><Textbox Name="{name}Item"><Height>.2in</Height><Width>3in</Width><Value>=Fields!Item.Value</Value></Textbox></ReportItems></TableCell>
                  <TableCell><ReportItems><Textbox Name="{name}Amt"><Height>.2in</Height><Width>3in</Width><Value>=Fields!Amt.Value</Value></Textbox></ReportItems></TableCell>
                </TableCells></TableRow></TableRows>
              </Details>
              <Footer>
                <TableRows><TableRow><Height>.2in</Height><TableCells>
                  <TableCell><ReportItems><Textbox Name="{name}FtrLabel"><Height>.2in</Height><Width>3in</Width><Value>="Total " &amp; Fields!Grp.Value</Value></Textbox></ReportItems></TableCell>
                  <TableCell><ReportItems><Textbox Name="{name}FtrSum"><Height>.2in</Height><Width>3in</Width><Value>="Sum " &amp; Sum(Fields!Amt.Value)</Value></Textbox></ReportItems></TableCell>
                </TableCells></TableRow></TableRows>
              </Footer>
            </Table>
            """;

        private static string Report(string items) => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <DataSources><DataSource Name="Src"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets>
                <DataSet Name="Rows">
                  <Query><DataSourceName>Src</DataSourceName><CommandText /></Query>
                  <Fields>
                    <Field Name="Grp"><DataField>Grp</DataField></Field>
                    <Field Name="Item"><DataField>Item</DataField></Field>
                    <Field Name="Amt"><DataField>Amt</DataField></Field>
                  </Fields>
                </DataSet>
              </DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>{items}</ReportItems>
              </Body>
            </Report>
            """;

        private static string InList(string table) => $"""
            <List Name="Groups">
              <DataSetName>Rows</DataSetName>
              <Width>6in</Width><Height>.8in</Height>
              <Grouping Name="ByGrp"><GroupExpressions><GroupExpression>=Fields!Grp.Value</GroupExpression></GroupExpressions></Grouping>
              <Sorting><SortBy><SortExpression>=Fields!Grp.Value</SortExpression><Direction>Ascending</Direction></SortBy></Sorting>
              <ReportItems>{table}</ReportItems>
            </List>
            """;

        private const string OnlyGroupB = """
            <Filters><Filter><FilterExpression>=Fields!Grp.Value</FilterExpression><Operator>Equal</Operator><FilterValues><FilterValue>B</FilterValue></FilterValues></Filter></Filters>
            """;

        private const string NoRows = """
            <Filters><Filter><FilterExpression>=Fields!Grp.Value</FilterExpression><Operator>Equal</Operator><FilterValues><FilterValue>Z</FilterValue></FilterValues></Filter></Filters>
            """;

        private static async Task<List<string>> Render(string rdl)
        {
            var document = new XmlDocument();
            document.LoadXml(rdl);
            var report = await new RDLParser(document) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));

            var table = new DataTable("Rows");
            table.Columns.Add("Grp", typeof(string));
            table.Columns.Add("Item", typeof(string));
            table.Columns.Add("Amt", typeof(int));
            table.Rows.Add("B", "b1", 1);
            table.Rows.Add("B", "b2", 2);
            table.Rows.Add("A", "a1", 10);
            table.Rows.Add("C", "c1", 100);

            await report.DataSets["Rows"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            return pages.Cast<Page>()
                .SelectMany(p => p.Cast<PageItem>())
                .OfType<PageText>()
                .OrderBy(t => t.Y).ThenBy(t => t.X)
                .Select(t => t.Text)
                .ToList();
        }

        [Test]
        public async Task TableInAListGroup_UnscopedAggregates_CoverTheGroupsRows()
        {
            var texts = await Render(Report(InList(Table("T", ""))));

            Assert.That(texts, Is.EqualTo(new[]
            {
                "Rows 1", "Max 10", "a1", "10", "Total A", "Sum 10",
                "Rows 2", "Max 2", "b1", "1", "b2", "2", "Total B", "Sum 3",
                "Rows 1", "Max 100", "c1", "100", "Total C", "Sum 100",
            }));
        }

        [Test]
        public async Task TableWithFilters_UnscopedAggregates_CoverTheRowsItShows()
        {
            var texts = await Render(Report(Table("T", OnlyGroupB)));

            Assert.That(texts, Is.EqualTo(new[] { "Rows 2", "Max 2", "b1", "1", "b2", "2", "Total B", "Sum 3" }));
        }

        [Test]
        public async Task TableWithoutFilters_UnscopedAggregates_CoverTheDataset()
        {
            var texts = await Render(Report(Table("T", "")));

            Assert.That(texts.First(), Is.EqualTo("Rows 4"));
            Assert.That(texts.Last(), Is.EqualTo("Sum 113"));
        }

        [Test]
        public async Task TableWhoseFiltersRemoveEveryRow_CountsNone()
        {
            var texts = await Render(Report(Table("T", NoRows)));

            Assert.That(texts, Does.Contain("Rows 0"));
            Assert.That(texts, Has.None.Contains("113"));
        }
    }
}
