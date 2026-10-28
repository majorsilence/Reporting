#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table group with KeepTogether that does not fit in what is left of the page starts on
    /// the next one; one longer than a page starts on the next page and runs across the break.
    /// </summary>
    [TestFixture]
    public class TableGroupKeepTogetherTests
    {
        private static string Row(string name, string value, string extra = "") =>
            $@"<TableRow><Height>20pt</Height>{extra}<TableCells><TableCell><ReportItems>
                 <Textbox Name=""{name}""><Value>{value}</Value><Style><FontSize>10pt</FontSize></Style></Textbox>
               </ReportItems></TableCell></TableCells></TableRow>";

        private static string Rdl(string groupElements, string innerGroup = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
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
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
        {groupElements}
        <Header><TableRows>{Row("Head", "=\"HEAD \" &amp; Fields!K.Value")}</TableRows></Header>
        <Footer><TableRows>{Row("Foot", "=\"FOOT \" &amp; Fields!K.Value")}</TableRows></Footer>
      </TableGroup>{innerGroup}</TableGroups>
      <Details><TableRows>{Row("Line", "=Fields!V.Value")}</TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>300pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        // Group "a" takes 140pt of the 300pt page and group "b" the given number of details
        // plus its header and footer, 20pt a row: with seven, 180pt, more than is left.
        private static async Task<(int Page, PageText Text)[]> Printed(string rdl, int bDetails = 7)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(string));
            for (int i = 1; i <= 5; i++)
                table.Rows.Add("a", "a" + i);
            for (int i = 1; i <= bDetails; i++)
                table.Rows.Add("b", "b" + i);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return Enumerable.Range(0, pages.Count)
                .SelectMany(i => pages[i].Cast<PageItem>().OfType<PageText>().Select(t => (i + 1, t)))
                .ToArray();
        }

        private static (int Page, PageText Text) Find((int Page, PageText Text)[] printed, string text) =>
            printed.Single(p => p.Text.Text == text);

        private const string KeepTogether = "<KeepTogether>true</KeepTogether>";

        [Test]
        public async Task AGroupKeptTogetherThatDoesNotFit_StartsTheNextPage()
        {
            var printed = await Printed(Rdl(KeepTogether));

            Assert.That(Find(printed, "FOOT a").Page, Is.EqualTo(1));
            var head = Find(printed, "HEAD b");
            Assert.That(head.Page, Is.EqualTo(2));
            Assert.That(head.Text.Y, Is.LessThan(10f), "at the top of the page");
            Assert.That(Find(printed, "b1").Page, Is.EqualTo(2), "printed once, on the new page");
            Assert.That(Find(printed, "FOOT b").Page, Is.EqualTo(2));
        }

        // A report that does not ask for it runs the group on from where it starts.
        [Test]
        public async Task AGroupNotKeptTogether_StartsWhereItIs()
        {
            var printed = await Printed(Rdl(""));

            Assert.That(Find(printed, "HEAD b").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "FOOT b").Page, Is.EqualTo(2));
        }

        [Test]
        public async Task AGroupKeptTogetherThatFits_StaysOnItsPage()
        {
            var printed = await Printed(Rdl(KeepTogether), bDetails: 5);

            Assert.That(Find(printed, "HEAD b").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "FOOT b").Page, Is.EqualTo(1));
        }

        // Twenty details, 440pt with the header and footer: more than a page of its own.
        [Test]
        public async Task AGroupKeptTogetherLongerThanAPage_StartsTheNextPageAndRunsAcross()
        {
            var printed = await Printed(Rdl(KeepTogether), bDetails: 20);

            Assert.That(Find(printed, "HEAD b").Page, Is.EqualTo(2));
            Assert.That(Find(printed, "b1").Page, Is.EqualTo(2));
            Assert.That(Find(printed, "FOOT b").Page, Is.EqualTo(3));
        }

        // A page broken on purpose inside the group is not a group that ran out of room: here a
        // group within it starts a page before "b3".
        [Test]
        public async Task AGroupKeptTogetherThatBreaksPagesItself_StartsWhereItIs()
        {
            string inner = @"<TableGroup><Grouping Name=""Row""><GroupExpressions><GroupExpression>=Fields!V.Value</GroupExpression></GroupExpressions>
              <PageBreakAtStart>true</PageBreakAtStart><PageBreakCondition>=Fields!V.Value = ""b3""</PageBreakCondition></Grouping></TableGroup>";
            var printed = await Printed(Rdl(KeepTogether, inner));

            Assert.That(Find(printed, "HEAD b").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "b2").Page, Is.EqualTo(1));
            Assert.That(Find(printed, "b3").Page, Is.EqualTo(2));
        }
    }
}
#endif
