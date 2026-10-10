using System.Text;
using Majorsilence.Reporting.Rdl;

namespace RdlAotSmokeTest
{
    /// <summary>Builds small RDL documents for the scenarios.</summary>
    static class ReportBuilder
    {
        const string Header = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <TopMargin>.25in</TopMargin><LeftMargin>.25in</LeftMargin><RightMargin>.25in</RightMargin><BottomMargin>.25in</BottomMargin>";

        static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // "Name" or "Name:Type" (System.Type short name); the type is declared because there is no
        // database schema to infer it from.
        static string DataSet(string[] fields)
        {
            var sb = new StringBuilder();
            sb.Append(@"<DataSources><DataSource Name=""DS1""><ConnectionProperties>
  <DataProvider>SQLite</DataProvider><ConnectString>Data Source=this-file-does-not-exist.db</ConnectString>
</ConnectionProperties></DataSource></DataSources>
<DataSets><DataSet Name=""Data""><Query><DataSourceName>DS1</DataSourceName><CommandText>SELECT 1</CommandText></Query><Fields>");
            foreach (var f in fields)
            {
                var parts = f.Split(':');
                sb.Append($@"<Field Name=""{parts[0]}""><DataField>{parts[0]}</DataField>");
                if (parts.Length > 1) sb.Append($"<rd:TypeName>System.{parts[1]}</rd:TypeName>");
                sb.Append("</Field>");
            }
            sb.Append("</Fields></DataSet></DataSets>");
            return sb.ToString();
        }

        static string Cell(string name, string expr) =>
            $@"<TableCell><ReportItems><Textbox Name=""{name}""><Value>{Esc(expr)}</Value></Textbox></ReportItems></TableCell>";

        static string Cols(int n) =>
            "<TableColumns>" + string.Concat(Enumerable.Repeat("<TableColumn><Width>0.7in</Width></TableColumn>", n)) + "</TableColumns>";

        public static string Table(string[] fields, string[] cells, string? classes = null)
        {
            var cellXml = string.Concat(cells.Select((c, i) => Cell("C" + i, c)));
            return Header + (classes ?? "") + DataSet(fields) + $@"
<Body><Height>1in</Height><ReportItems>
  <Table Name=""T1""><DataSetName>Data</DataSetName><Width>{cells.Length * 0.7}in</Width>{Cols(cells.Length)}
    <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>{cellXml}</TableCells></TableRow></TableRows></Details>
  </Table>
</ReportItems></Body></Report>";
        }

        public static string GroupedTable() => Header + DataSet(new[] { "Region:String", "Amount:Int32" }) + $@"
<Body><Height>1in</Height><ReportItems>
  <Table Name=""T1""><DataSetName>Data</DataSetName><Width>2.1in</Width>{Cols(3)}
    <TableGroups><TableGroup>
      <Grouping Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Grouping>
      <Header><TableRows><TableRow><Height>0.25in</Height><TableCells>
        {Cell("G0", "=Fields!Region.Value")}{Cell("G1", "=Sum(Fields!Amount.Value)")}{Cell("G2", "=Count(Fields!Amount.Value)")}
      </TableCells></TableRow></TableRows></Header>
    </TableGroup></TableGroups>
    <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
      {Cell("D0", "=Fields!Region.Value")}{Cell("D1", "=Fields!Amount.Value")}{Cell("D2", "=Fields!Amount.Value * 2")}
    </TableCells></TableRow></TableRows></Details>
    <Footer><TableRows><TableRow><Height>0.25in</Height><TableCells>
      {Cell("F0", "Total")}{Cell("F1", "=Sum(Fields!Amount.Value)")}{Cell("F2", "=Count(Fields!Amount.Value)")}
    </TableCells></TableRow></TableRows></Footer>
  </Table>
</ReportItems></Body></Report>";

        public static string StaticReport(string text) => Header + $@"
<Body><Height>0.5in</Height><ReportItems>
  <Textbox Name=""T""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height><Value>{Esc(text)}</Value></Textbox>
</ReportItems></Body></Report>";

        public static string WithSubreport(string subreportName) => Header + DataSet(new[] { "N:Int32" }) + $@"
<Body><Height>2in</Height><ReportItems>
  <Table Name=""T1""><DataSetName>Data</DataSetName><Width>0.7in</Width>{Cols(1)}
    <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>{Cell("C0", "=Fields!N.Value")}</TableCells></TableRow></TableRows></Details>
  </Table>
  <Subreport Name=""S1""><Top>1in</Top><Left>0in</Left><Height>0.5in</Height><Width>3in</Width><ReportName>{subreportName}</ReportName></Subreport>
</ReportItems></Body></Report>";

        public static string WithQrCode() => Header + DataSet(new[] { "N:Int32" }) + $@"
<Body><Height>2in</Height><ReportItems>
  <Table Name=""T1""><DataSetName>Data</DataSetName><Width>0.7in</Width>{Cols(1)}
    <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>{Cell("C0", "=Fields!N.Value")}</TableCells></TableRow></TableRows></Details>
  </Table>
  <CustomReportItem Name=""QR""><Type>QRCode</Type><Top>1in</Top><Left>0in</Left><Width>1in</Width><Height>1in</Height>
    <CustomProperties><CustomProperty><Name>Code</Name><Value>https://github.com/majorsilence/Reporting</Value></CustomProperty></CustomProperties>
    <Source>Embedded</Source>
  </CustomReportItem>
</ReportItems></Body></Report>";
    }

    static class Render
    {
        public static async Task<byte[]> Bytes(string folder, string rdl, OutputPresentationType type, Func<Report, Task> pushData)
        {
            var parser = new RDLParser(rdl) { Folder = folder, SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse();
            if (report.ErrorMaxSeverity > 4)
                throw new InvalidOperationException("parse errors: " + string.Join(" | ", report.ErrorItems.Cast<object>()));
            report.Folder = folder;

            await pushData(report);
            await report.RunGetData();
            if (report.ErrorMaxSeverity > 4)
                throw new InvalidOperationException("data errors: " + string.Join(" | ", report.ErrorItems.Cast<object>()));

            var path = Path.Combine(folder, Guid.NewGuid().ToString("N") + "." + type);
            await report.RunRender(new OneFileStreamGen(path, true), type);
            if (report.ErrorMaxSeverity > 4)
                throw new InvalidOperationException("render errors: " + string.Join(" | ", report.ErrorItems.Cast<object>()));
            return await File.ReadAllBytesAsync(path);
        }

        public static async Task<string> Text(string folder, string rdl, OutputPresentationType type, Func<Report, Task> pushData) =>
            Encoding.UTF8.GetString(await Bytes(folder, rdl, type, pushData));

        public static Task<string> Csv(string folder, string rdl, Func<Report, Task> pushData) =>
            Text(folder, rdl, OutputPresentationType.CSV, pushData);
    }

    static class Expect
    {
        public static void Contains(string text, params string[] expected)
        {
            var missing = expected.Where(e => !text.Contains(e, StringComparison.Ordinal)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException($"output is missing [{string.Join(", ", missing)}]; output was: {Trim(text)}");
        }

        public static void ValidPdf(byte[] bytes)
        {
            var text = Encoding.Latin1.GetString(bytes);
            if (!text.StartsWith("%PDF-", StringComparison.Ordinal) || !text.Contains("%%EOF") || bytes.Length < 500)
                throw new InvalidOperationException($"not a structurally valid PDF ({bytes.Length} bytes)");
        }

        public static void ZipHeader(byte[] bytes)
        {
            if (bytes.Length < 4 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
                throw new InvalidOperationException($"not a zip container ({bytes.Length} bytes)");
        }

        static string Trim(string s) => s.Length <= 300 ? s.Replace("\n", "\\n") : s.Substring(0, 300).Replace("\n", "\\n") + "...";
    }
}
