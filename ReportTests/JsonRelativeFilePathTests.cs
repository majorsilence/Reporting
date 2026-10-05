using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A JSON data source's relative <c>file=</c> path is relative to the report, as
    /// <c>&lt;Rows File="..."/&gt;</c> is. The provider used to open it against the process's working
    /// directory, so Examples/JsonToPdf/Employees.rdl ("file=employees.json", next to the report)
    /// reported "Could not find file" when opened in the designer.
    /// </summary>
    [TestFixture]
    public class JsonRelativeFilePathTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            // Deliberately not the working directory, so a path resolved against it cannot pass.
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-json-relative-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "people.json"), """
                [{"Name":"Alice"},{"Name":"Bob"}]
                """);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private const string Rdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>4in</Width>
              <DataSources>
                <DataSource Name="Json">
                  <ConnectionProperties>
                    <DataProvider>Json</DataProvider>
                    <ConnectString>file=people.json</ConnectString>
                  </ConnectionProperties>
                </DataSource>
              </DataSources>
              <DataSets>
                <DataSet Name="Data">
                  <Query><DataSourceName>Json</DataSourceName><CommandText>columns=Name</CommandText></Query>
                  <Fields><Field Name="Name"><DataField>Name</DataField></Field></Fields>
                </DataSet>
              </DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <List Name="People">
                    <DataSetName>Data</DataSetName>
                    <Height>.25in</Height><Width>4in</Width>
                    <ReportItems>
                      <Textbox Name="PersonName"><Height>.25in</Height><Width>4in</Width><Value>=Fields!Name.Value</Value></Textbox>
                    </ReportItems>
                  </List>
                </ReportItems>
              </Body>
            </Report>
            """;

        [Test]
        public async Task A_relative_file_path_is_read_from_the_reports_folder()
        {
            var parser = new RDLParser(Rdl) { Folder = _folder };
            var report = await parser.Parse();
            report.Folder = _folder;

            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var texts = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Select(t => t.Text).ToList();

            Assert.That(texts, Does.Contain("Alice").And.Contain("Bob"),
                "report errors: " + string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Array.Empty<object>()));
        }
    }
}
