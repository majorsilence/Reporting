#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A null number is Nothing: the aggregates skip it and IsNothing is True for it. A negative
    /// value that is zero at the format's precision prints as the format's zero, with no minus sign.
    /// </summary>
    [TestFixture]
    public class NullNumberAndNegativeZeroTests
    {
        private static string Rdl(string[] header, string[] detail, string format = null) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Language>en-US</Language>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""D""><DataField>D</DataField><TypeName>System.Double</TypeName></Field>
      <Field Name=""M""><DataField>M</DataField><TypeName>System.Decimal</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>600pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns>{string.Concat(Enumerable.Repeat("<TableColumn><Width>80pt</Width></TableColumn>", Math.Max(header.Length, detail.Length)))}</TableColumns>
      {(header.Length == 0 ? "" : $"<Header><TableRows>{Rows(header, "H", format)}</TableRows></Header>")}
      {(detail.Length == 0 ? "" : $"<Details><TableRows>{Rows(detail, "L", format)}</TableRows></Details>")}
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static string Rows(string[] values, string prefix, string format)
        {
            var style = format == null ? "" : $"<Style><Format>{format}</Format></Style>";
            var cells = values.Select((v, i) =>
                $"<TableCell><ReportItems><Textbox Name=\"{prefix}{i}\"><Value>{v}</Value>{style}</Textbox></ReportItems></TableCell>");
            return $"<TableRow><Height>14pt</Height><TableCells>{string.Concat(cells)}</TableCells></TableRow>";
        }

        private static async Task<string[][]> Lines(string[] header, string[] detail, (double? d, decimal? m)[] rows, string format = null)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(header, detail, format)) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("D", typeof(double));
            table.Columns.Add("M", typeof(decimal));
            foreach (var (d, m) in rows)
                table.Rows.Add(d.HasValue ? d.Value : DBNull.Value, m.HasValue ? m.Value : DBNull.Value);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .GroupBy(t => t.Y).OrderBy(g => g.Key)
                .Select(g => g.OrderBy(t => t.X).Select(t => t.Text).ToArray()).ToArray();
        }

        private static readonly (double?, decimal?)[] WithNulls = { (1, 1m), (null, null), (3, 3m), (3, 3m) };

        [Test]
        public async Task Aggregates_SkipANullNumber()
        {
            var lines = await Lines(new[]
            {
                "=Count(Fields!D.Value)", "=CountDistinct(Fields!D.Value)", "=Sum(Fields!D.Value)", "=Avg(Fields!D.Value)",
                "=Count(Fields!M.Value)", "=CountDistinct(Fields!M.Value)", "=Sum(Fields!M.Value)", "=Avg(Fields!M.Value)",
            }, new string[0], WithNulls);
            Assert.That(lines[0], Is.EqualTo(new[] { "3", "2", "7", "2.3333333333333335", "3", "2", "7", "2.3333333333333333333333333333" }));
        }

        [Test]
        public async Task RunningValues_SkipANullNumber()
        {
            var lines = await Lines(new string[0], new[]
            {
                "=RunningValue(Fields!D.Value, Count, Nothing)", "=RunningValue(Fields!D.Value, CountDistinct, Nothing)",
                "=RunningValue(Fields!D.Value, Sum, Nothing)", "=RunningValue(Fields!D.Value, Avg, Nothing)",
                "=RunningValue(Fields!D.Value, Min, Nothing)", "=RunningValue(Fields!M.Value, Sum, Nothing)",
                "=RunningValue(Fields!M.Value, Avg, Nothing)",
            }, new (double?, decimal?)[] { (null, null), (4, 4m), (null, null), (2, 2m) });
            Assert.That(lines.Select(l => string.Join(",", l)), Is.EqualTo(new[]
            {
                "0,0,0,,,0,",
                "1,1,4,4,4,4,4",
                "1,1,4,4,4,4,4",
                "2,2,6,3,2,6,3",
            }));
        }

        [Test]
        public async Task IsNothing_IsTrueForANullNumber()
        {
            var lines = await Lines(new string[0], new[]
            {
                "=IIf(IsNothing(Fields!D.Value), \"null\", \"set\")", "=IIf(IsNothing(Fields!M.Value), \"null\", \"set\")",
            }, new (double?, decimal?)[] { (1, 1m), (null, null) });
            Assert.That(lines[0], Is.EqualTo(new[] { "set", "set" }));
            Assert.That(lines[1], Is.EqualTo(new[] { "null", "null" }));
        }

        [TestCase("C", "$0.00", "-$1.50")]
        [TestCase("#,##0.00", "0.00", "-1.50")]
        [TestCase("#,##0.00;(#,##0.00)", "0.00", "(1.50)")]
        [TestCase("#,##0.00;(#,##0.00);\"nil\"", "nil", "(1.50)")]
        [TestCase("P1", "0.0%", "-150.0%")]
        public async Task ANegativeZero_PrintsTheFormatsZero(string format, string zero, string negative)
        {
            var lines = await Lines(new string[0], new[] { "=Fields!D.Value" },
                new (double?, decimal?)[] { (-0.0, 0m), (-1e-13, 0m), (-0.0004, 0m), (-1.5, 0m), (0.0, 0m) },
                System.Security.SecurityElement.Escape(format));
            Assert.That(lines.Select(l => l[0]), Is.EqualTo(new[] { zero, zero, zero, negative, zero }));
        }
    }
}
#endif
