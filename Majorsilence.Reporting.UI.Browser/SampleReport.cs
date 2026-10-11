namespace Majorsilence.Reporting.UI.Browser;

internal sealed record SampleRow(string Product, string Region, int Units);

internal static class SampleReport
{
    public static readonly List<SampleRow> Rows = new()
    {
        new("Chai", "North", 120), new("Chai", "South", 80), new("Ikura", "North", 45),
        new("Ikura", "South", 60), new("Tofu", "North", 200), new("Tofu", "South", 150),
    };

    public const string Rdl = """
<?xml version="1.0" encoding="utf-8"?>
<Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition" xmlns:rd="http://schemas.microsoft.com/SQLServer/reporting/reportdesigner">
  <Width>6.5in</Width>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name="Title"><Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.4in</Height>
      <Value>Units sold (running in WebAssembly)</Value>
      <Style><FontSize>16pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
    <Table Name="T1"><Top>0.5in</Top><DataSetName>Data</DataSetName><Width>6in</Width>
      <TableColumns>
        <TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>2in</Width></TableColumn>
      </TableColumns>
      <Header><TableRows><TableRow><Height>0.3in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name="H1"><Value>Product</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name="H2"><Value>Region</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name="H3"><Value>Units</Value><Style><FontWeight>Bold</FontWeight><BackgroundColor>LightGray</BackgroundColor></Style></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
        <TableCell><ReportItems><Textbox Name="C1"><Value>=Fields!Product.Value</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name="C2"><Value>=Fields!Region.Value</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name="C3"><Value>=Fields!Units.Value</Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <DataSources><DataSource Name="DS1"><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>unused</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name="Data"><Query><DataSourceName>DS1</DataSourceName><CommandText>SELECT 1</CommandText></Query><Fields>
    <Field Name="Product"><DataField>Product</DataField><rd:TypeName>System.String</rd:TypeName></Field>
    <Field Name="Region"><DataField>Region</DataField><rd:TypeName>System.String</rd:TypeName></Field>
    <Field Name="Units"><DataField>Units</DataField><rd:TypeName>System.Int32</rd:TypeName></Field>
  </Fields></DataSet></DataSets>
</Report>
""";
}
