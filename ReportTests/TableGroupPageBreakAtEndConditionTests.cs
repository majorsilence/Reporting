#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// PageBreakCondition gates a table group's PageBreakAtEnd as it gates PageBreakAtStart: the
    /// break after an instance comes only where the condition holds on the instance's last row.
    /// A table group and a Details grouping alike.
    /// </summary>
    [TestFixture]
    public class TableGroupPageBreakAtEndConditionTests
    {
        private static string Row(string name, string value) =>
            $@"<TableRow><Height>20pt</Height><TableCells><TableCell><ReportItems>
                 <Textbox Name=""{name}""><Value>{value}</Value><Style><FontSize>10pt</FontSize></Style></Textbox>
               </ReportItems></TableCell></TableCells></TableRow>";

        private static string Rdl(string tableGroups, string detailGrouping) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      {tableGroups}
      <Details>{detailGrouping}<TableRows>{Row("Line", "=Fields!V.Value")}</TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private static string Group(string breakElements) =>
            $@"<TableGroups><TableGroup>
                 <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions>{breakElements}</Grouping>
                 <Header><TableRows>{Row("Head", "=\"HEAD \" &amp; Fields!K.Value")}</TableRows></Header>
                 <Footer><TableRows>{Row("Foot", "=\"FOOT \" &amp; Fields!K.Value")}</TableRows></Footer>
               </TableGroup></TableGroups>";

        private static string DetailGrouping(string breakElements) =>
            $@"<Grouping Name=""Record""><GroupExpressions><GroupExpression>=Fields!V.Value</GroupExpression></GroupExpressions>{breakElements}</Grouping>";

        // Three groups of two rows, a, b and c, all of which fit on one page.
        private static async Task<(int Page, string Text)[]> Printed(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            foreach (string k in new[] { "a", "b", "c" })
                for (int i = 1; i <= 2; i++)
                    table.Rows.Add(k, k + i);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return Enumerable.Range(0, pages.Count)
                .SelectMany(i => pages[i].Cast<PageItem>().OfType<PageText>().Select(t => (i + 1, t.Text)))
                .ToArray();
        }

        private static int PageOf((int Page, string Text)[] printed, string text) =>
            printed.Single(p => p.Text == text).Page;

        [Test]
        public async Task AGroupBreaksAtItsEndOnlyWhereTheConditionHolds()
        {
            var printed = await Printed(Rdl(Group(
                "<PageBreakAtEnd>true</PageBreakAtEnd><PageBreakCondition>=Fields!K.Value = \"b\"</PageBreakCondition>"), ""));

            Assert.That(PageOf(printed, "FOOT a"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "HEAD b"), Is.EqualTo(1), "no break after a");
            Assert.That(PageOf(printed, "FOOT b"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "HEAD c"), Is.EqualTo(2), "a break after b");
        }

        // The break comes after the instance, so the condition is tested on its last row.
        [Test]
        public async Task AGroupsConditionAtItsEnd_IsTestedOnItsLastRow()
        {
            var printed = await Printed(Rdl(Group(
                "<PageBreakAtEnd>true</PageBreakAtEnd><PageBreakCondition>=Fields!V.Value = \"b2\"</PageBreakCondition>"), ""));

            Assert.That(PageOf(printed, "HEAD b"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "HEAD c"), Is.EqualTo(2));
        }

        [Test]
        public async Task AGroupWithNoConditionBreaksAtTheEndOfEachInstance()
        {
            var printed = await Printed(Rdl(Group("<PageBreakAtEnd>true</PageBreakAtEnd>"), ""));

            Assert.That(PageOf(printed, "HEAD a"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "HEAD b"), Is.EqualTo(2));
            Assert.That(PageOf(printed, "HEAD c"), Is.EqualTo(3));
        }

        // A break after one record, as a report that starts a new page after a record whose
        // value says so: the footer of the group it closes moves on with the break.
        [Test]
        public async Task ADetailGroupingBreaksAfterTheRecordsWhereTheConditionHolds()
        {
            var printed = await Printed(Rdl(Group(""), DetailGrouping(
                "<PageBreakAtEnd>true</PageBreakAtEnd><PageBreakCondition>=Fields!V.Value = \"a2\" Or Fields!V.Value = \"b1\"</PageBreakCondition>")));

            Assert.That(PageOf(printed, "a1"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "a2"), Is.EqualTo(1));
            Assert.That(PageOf(printed, "FOOT a"), Is.EqualTo(2));
            Assert.That(PageOf(printed, "b1"), Is.EqualTo(2));
            Assert.That(PageOf(printed, "b2"), Is.EqualTo(3));
            Assert.That(PageOf(printed, "c2"), Is.EqualTo(3));
        }

        [Test]
        public async Task ADetailGroupingWhoseConditionNeverHolds_DoesNotBreak()
        {
            var printed = await Printed(Rdl("", DetailGrouping(
                "<PageBreakAtEnd>true</PageBreakAtEnd><PageBreakCondition>=false</PageBreakCondition>")));

            Assert.That(printed.Select(p => p.Page).Distinct(), Is.EqualTo(new[] { 1 }));
            Assert.That(printed.Count(p => p.Text.Length == 2), Is.EqualTo(6));
        }
    }
}
#endif
