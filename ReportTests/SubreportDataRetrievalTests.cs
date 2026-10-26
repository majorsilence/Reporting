using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// Data set on a subreport's dataset in SubreportDataRetrieval is filtered with that instance's
    /// parameters. The event was raised before the parameters were set, and SetData applies the
    /// dataset's filters at once, so each instance was filtered with the previous one's values: the
    /// first with none at all.
    /// </summary>
    [TestFixture]
    public class SubreportDataRetrievalTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-subreport-data-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "People.rdl"), SubreportRdl);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private const string Fields = """
            <Fields><Field Name="Name"><DataField>Name</DataField></Field><Field Name="Dept"><DataField>Dept</DataField></Field></Fields>
            """;

        private const string MasterRdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets><DataSet Name="Data"><Query><DataSourceName>Pushed</DataSourceName><CommandText>none</CommandText></Query>
            """ + Fields + """
              </DataSet></DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <List Name="Depts">
                    <DataSetName>Data</DataSetName>
                    <Width>6in</Width><Height>.6in</Height>
                    <Grouping Name="ByDept"><GroupExpressions><GroupExpression>=Fields!Dept.Value</GroupExpression></GroupExpressions></Grouping>
                    <ReportItems>
                      <Textbox Name="DeptName"><Top>0in</Top><Width>6in</Width><Height>.2in</Height><Value>="Dept " &amp; Fields!Dept.Value</Value></Textbox>
                      <Subreport Name="People">
                        <Top>.25in</Top><Width>6in</Width><Height>.25in</Height>
                        <ReportName>People</ReportName>
                        <Parameters><Parameter Name="Dept"><Value>=Fields!Dept.Value</Value></Parameter></Parameters>
                      </Subreport>
                    </ReportItems>
                  </List>
                </ReportItems>
              </Body>
            </Report>
            """;

        private const string SubreportRdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <ReportParameters><ReportParameter Name="Dept"><DataType>String</DataType><Nullable>true</Nullable><Prompt>Dept</Prompt></ReportParameter></ReportParameters>
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets><DataSet Name="Data"><Query><DataSourceName>Pushed</DataSourceName><CommandText>none</CommandText></Query>
            """ + Fields + """
                  <Filters><Filter><FilterExpression>=Fields!Dept.Value</FilterExpression><Operator>Equal</Operator>
                    <FilterValues><FilterValue>=Parameters!Dept.Value</FilterValue></FilterValues></Filter></Filters>
              </DataSet></DataSets>
              <Body>
                <Height>.25in</Height>
                <ReportItems>
                  <Table Name="Rows">
                    <DataSetName>Data</DataSetName>
                    <TableColumns><TableColumn><Width>3in</Width></TableColumn></TableColumns>
                    <Details><TableRows><TableRow><Height>.25in</Height><TableCells>
                      <TableCell><ReportItems><Textbox Name="PersonName"><Value>=Fields!Name.Value</Value></Textbox></ReportItems></TableCell>
                    </TableCells></TableRow></TableRows></Details>
                  </Table>
                </ReportItems>
              </Body>
            </Report>
            """;

        private static DataTable People()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Dept", typeof(string));
            foreach (var (name, dept) in new[] { ("A1", "A"), ("A2", "A"), ("B1", "B"), ("C1", "C"), ("C2", "C") })
                table.Rows.Add(name, dept);
            return table;
        }

        [Test]
        public async Task EachInstance_IsFilteredWithItsOwnParameters()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(MasterRdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.DataSets["Data"].SetData(People());
            report.SubreportDataRetrieval += (_, e) => e.Report.DataSets["Data"].SetData(People()).GetAwaiter().GetResult();

            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var texts = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).Select(t => t.Text).ToList();
            Assert.That(texts, Is.EqualTo(new[] { "Dept A", "A1", "A2", "Dept B", "B1", "Dept C", "C1", "C2" }));
        }
    }
}
