namespace HostingBrowser;

internal static class SampleReport
{
    public const string Json = """
{"Items":[
 {"Product":"Chai","Region":"North","Units":120},{"Product":"Chai","Region":"South","Units":80},
 {"Product":"Ikura","Region":"North","Units":45},{"Product":"Ikura","Region":"South","Units":60},
 {"Product":"Tofu","Region":"North","Units":200},{"Product":"Tofu","Region":"South","Units":150}
]}
""";

    public static string Rdl(string jsonPath) => $$"""
<?xml version="1.0" encoding="utf-8"?>
<Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition" xmlns:rd="http://schemas.microsoft.com/SQLServer/reporting/reportdesigner">
  <Width>6.5in</Width>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name="Title"><Top>0in</Top><Left>0in</Left><Width>6in</Width><Height>0.4in</Height>
      <Value>Units sold (Majorsilence.Forms viewer in WebAssembly)</Value>
      <Style><FontSize>14pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
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
  <DataSources><DataSource Name="DS1"><ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file={{jsonPath}}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name="Data"><Query><DataSourceName>DS1</DataSourceName><CommandText>columns=Product,Region,Units;table=root_Items</CommandText></Query><Fields>
    <Field Name="Product"><DataField>Product</DataField><rd:TypeName>System.String</rd:TypeName></Field>
    <Field Name="Region"><DataField>Region</DataField><rd:TypeName>System.String</rd:TypeName></Field>
    <Field Name="Units"><DataField>Units</DataField><rd:TypeName>System.Int32</rd:TypeName></Field>
  </Fields></DataSet></DataSets>
</Report>
""";
}
