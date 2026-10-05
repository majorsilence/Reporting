using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A list instance grows with a subreport inside it. The list only measured its CanGrow textboxes,
    /// so a department block closed off at its designed height and the next department was drawn over
    /// the employees its subreport had listed.
    /// </summary>
    [TestFixture]
    public class ListSubreportGrowthTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-list-subreport-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);

            // Five people in A, one in B: A's subreport is far taller than the list's designed row.
            File.WriteAllText(Path.Combine(_folder, "people.json"), """
                [{"Name":"A1","Dept":"A"},{"Name":"A2","Dept":"A"},{"Name":"A3","Dept":"A"},
                 {"Name":"A4","Dept":"A"},{"Name":"A5","Dept":"A"},{"Name":"B1","Dept":"B"}]
                """);
            File.WriteAllText(Path.Combine(_folder, "People.rdl"), WithData(SubreportRdl));
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        // An absolute path, so this does not depend on how a relative file= is resolved.
        private string WithData(string rdl) => rdl.Replace("file=people.json", "file=" + Path.Combine(_folder, "people.json"));

        private const string DataSource = """
              <DataSources>
                <DataSource Name="Json">
                  <ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file=people.json</ConnectString></ConnectionProperties>
                </DataSource>
              </DataSources>
            """;

        private const string MasterRdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
            """ + DataSource + """
              <DataSets>
                <DataSet Name="Data">
                  <Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name,Dept</CommandText></Query>
                  <Fields><Field Name="Name"><DataField>Name</DataField></Field><Field Name="Dept"><DataField>Dept</DataField></Field></Fields>
                </DataSet>
              </DataSets>
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
              <ReportParameters><ReportParameter Name="Dept"><DataType>String</DataType><Prompt>Dept</Prompt></ReportParameter></ReportParameters>
            """ + DataSource + """
              <DataSets>
                <DataSet Name="Data">
                  <Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name,Dept</CommandText></Query>
                  <Fields><Field Name="Name"><DataField>Name</DataField></Field><Field Name="Dept"><DataField>Dept</DataField></Field></Fields>
                  <Filters><Filter><FilterExpression>=Fields!Dept.Value</FilterExpression><Operator>Equal</Operator>
                    <FilterValues><FilterValue>=Parameters!Dept.Value</FilterValue></FilterValues></Filter></Filters>
                </DataSet>
              </DataSets>
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

        [Test]
        public async Task The_next_list_instance_starts_below_a_grown_subreport()
        {
            var parser = new RDLParser(WithData(MasterRdl)) { Folder = _folder };
            var report = await parser.Parse();
            report.Folder = _folder;

            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var texts = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().ToList();
            var lastOfA = texts.SingleOrDefault(t => t.Text == "A5");
            var headingB = texts.SingleOrDefault(t => t.Text == "Dept B");

            Assert.That(lastOfA, Is.Not.Null, "A's subreport rows were not rendered: " + string.Join(", ", texts.Select(t => t.Text)));
            Assert.That(headingB, Is.Not.Null, "B's heading was not rendered");
            Assert.That(headingB.Y, Is.GreaterThanOrEqualTo(lastOfA.Y + lastOfA.H),
                $"Dept B starts at {headingB.Y:0.0}pt, inside A's subreport, which ends at {lastOfA.Y + lastOfA.H:0.0}pt");
        }
    }
}
