#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// An aggregate over a scope with no rows, such as one scoped to a group in the header or
    /// footer of a table with no rows, is Nothing, as SSRS gives it, and a count of no rows is 0.
    /// A sum scoped to a group printed decimal.MinValue, the engine's own marker for "no scope",
    /// and other aggregates printed NaN or int.MinValue.
    /// </summary>
    [TestFixture]
    public class AggregateOfNoRowsTests
    {
        private static string Rdl(string header, string footer) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>Decimal</TypeName></Field>
      <Field Name=""W""><DataField>W</DataField><TypeName>Double</TypeName></Field>
      <Field Name=""GT""><Value>=Sum(Fields!V.Value, ""G"")</Value></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Head""><Value>{header}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Line""><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
      <Footer><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
        <Textbox Name=""Foot""><Value>{footer}</Value></Textbox>
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Footer>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        // Each aggregate, scoped to the group, printed between bars.
        private const string EachAggregate = "=\"[\" &amp; CStr(Sum(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Sum(Fields!W.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Avg(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Avg(Fields!W.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Min(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Max(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(First(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Last(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(StDev(Fields!W.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Var(Fields!W.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, Sum, \"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!W.Value, Sum, \"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!W.Value, Avg, \"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!W.Value, StDev, \"G\"))"
            + " &amp; \"|\" &amp; CStr(Count(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(CountDistinct(Fields!V.Value, \"G\"))"
            + " &amp; \"|\" &amp; CStr(CountRows(\"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, Count, \"G\"))"
            + " &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, CountDistinct, \"G\"))"
            + " &amp; \"]\"";

        private const string NoneOfThem = "[||||||||||||||0|0|0|0|0]";

        private static async Task<string[]> Texts(string rdl, bool withRows)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(decimal));
            table.Columns.Add("W", typeof(double));
            if (withRows)
            {
                table.Rows.Add("a", 1m, 1d);
                table.Rows.Add("a", 3m, 3d);
            }
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            using (report)
            {
                var pages = await report.BuildPages();
                return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>().OfType<PageText>())
                    .OrderBy(t => t.Y).Select(t => t.Text).ToArray();
            }
        }

        [Test]
        public async Task InAnEmptyTablesHeaderAndFooter_AnAggregateScopedToAGroupIsNothing()
        {
            var texts = await Texts(Rdl(EachAggregate, EachAggregate), withRows: false);

            Assert.That(texts, Is.EqualTo(new[] { NoneOfThem, NoneOfThem }));
        }

        [Test]
        public async Task InAnEmptyTable_IsNothingIsTrueAndAsANumberItIsZero()
        {
            var texts = await Texts(Rdl(
                "=CStr(IsNothing(Sum(Fields!V.Value, \"G\"))) &amp; \" \" &amp; CStr(IsNothing(Sum(Fields!W.Value, \"G\"))) &amp; \" \" &amp; CStr(IsNothing(Avg(Fields!W.Value, \"G\")))",
                "=CStr(Sum(Fields!V.Value, \"G\") + 1) &amp; \" \" &amp; CStr(Sum(Fields!W.Value, \"G\") + 1)"), withRows: false);

            Assert.That(texts, Is.EqualTo(new[] { "True True True", "1 1" }));
        }

        [Test]
        public async Task InAnEmptyTable_ACalculatedFieldOfAGroupsSumIsNothing()
        {
            var texts = await Texts(Rdl("=\"[\" &amp; CStr(Fields!GT.Value) &amp; \"]\"", "=\"[\" &amp; CStr(Fields!GT.Value) &amp; \"]\""), withRows: false);

            Assert.That(texts, Is.EqualTo(new[] { "[]", "[]" }));
        }

        // A sum over the table's own data, with no rows, is Nothing too, and a running value over
        // it is Nothing or 0 rather than an error.
        [Test]
        public async Task InAnEmptyTable_ASumOverTheDataSetIsNothing()
        {
            var texts = await Texts(Rdl(
                "=\"[\" &amp; CStr(Sum(Fields!V.Value)) &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, Sum, \"Data\")) &amp; \"|\" &amp; CStr(RunningValue(Fields!W.Value, Avg, \"Data\")) &amp; \"]\"",
                "=\"[\" &amp; CStr(Count(Fields!V.Value)) &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, Count, \"Data\")) &amp; \"|\" &amp; CStr(RunningValue(Fields!V.Value, CountDistinct, \"Data\")) &amp; \"]\""), withRows: false);

            Assert.That(texts, Is.EqualTo(new[] { "[||]", "[0|0|0]" }));
        }

        // With rows, each has its value; a deviation of one value is Nothing, not NaN.
        [Test]
        public async Task WithRows_EachAggregateHasItsValue()
        {
            var texts = await Texts(Rdl(EachAggregate, "=CStr(Sum(Fields!V.Value, \"G\") + 1) &amp; \" \" &amp; CStr(Fields!GT.Value)"), withRows: true);

            // The header has the first group, at its first row, for a running value.
            Assert.That(texts.First(), Is.EqualTo("[4|4|2|2|1|3|1|3|1.4142135623730951|2|1|1|1||2|2|2|1|1]"));
            Assert.That(texts.Last(), Is.EqualTo("5 4"));
        }
    }
}
#endif
