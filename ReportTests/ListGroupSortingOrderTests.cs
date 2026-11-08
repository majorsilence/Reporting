using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// Issue #579: a grouped &lt;List&gt;'s &lt;Sorting&gt; only broke ties between rows that already
    /// shared the same group key, because <c>Rows(Report, TableGroups, Grouping, Sorting)</c>
    /// (<c>RdlEngine/Definition/Rows.cs</c>) added the group's own key expression(s) to the sort
    /// keys before the group's &lt;Sorting&gt; expressions, which fixed the order of the groups
    /// before Sorting ever got to compare two different groups. Groups came out in group-expression
    /// (i.e. alphabetical, for string keys) order no matter what Sorting said.
    /// </summary>
    [TestFixture]
    public class ListGroupSortingOrderTests
    {
        [SetUp]
        public void SetUp() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Rdl = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>6in</Width>
              <DataSources><DataSource Name="Src"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets>
                <DataSet Name="Rows">
                  <Query><DataSourceName>Src</DataSourceName><CommandText /></Query>
                  <Fields>
                    <Field Name="Grp"><DataField>Grp</DataField></Field>
                    <Field Name="Ord"><DataField>Ord</DataField></Field>
                  </Fields>
                </DataSet>
              </DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <List Name="Groups">
                    <DataSetName>Rows</DataSetName>
                    <Width>6in</Width><Height>.2in</Height>
                    <Grouping Name="ByGrp"><GroupExpressions><GroupExpression>=Fields!Grp.Value</GroupExpression></GroupExpressions></Grouping>
                    <Sorting><SortBy><SortExpression>=Fields!Ord.Value</SortExpression><Direction>Descending</Direction></SortBy></Sorting>
                    <ReportItems>
                      <Textbox Name="GrpName"><Top>0in</Top><Width>6in</Width><Height>.2in</Height><Value>=Fields!Grp.Value</Value></Textbox>
                    </ReportItems>
                  </List>
                </ReportItems>
              </Body>
            </Report>
            """;

        // Grp B has two rows both with Ord=1 (the group should still appear only once), Grp A has
        // Ord=2, Grp C has Ord=3. Sorting descending on Ord should order the groups C, A, B --
        // not the alphabetical B, A, C that sorting by the group key (Grp) alone would give.
        [Test]
        public async Task Groups_are_ordered_by_Sorting_not_by_the_group_key()
        {
            var document = new XmlDocument();
            document.LoadXml(Rdl);
            var parser = new RDLParser(document) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));

            var table = new DataTable("Rows");
            table.Columns.Add("Grp", typeof(string));
            table.Columns.Add("Ord", typeof(int));
            table.Rows.Add("B", 1);
            table.Rows.Add("B", 1);
            table.Rows.Add("A", 2);
            table.Rows.Add("C", 3);

            await report.DataSets["Rows"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var groupNames = pages.Cast<Page>()
                .SelectMany(p => p.Cast<PageItem>())
                .OfType<PageText>()
                .Select(t => t.Text)
                .ToList();

            Assert.That(groupNames, Is.EqualTo(new List<string> { "C", "A", "B" }));
        }
    }
}
