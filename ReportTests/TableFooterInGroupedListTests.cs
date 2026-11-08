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
    /// Issue #580: a &lt;Table&gt; nested inside a grouped &lt;List&gt; rendered its &lt;Header&gt; and
    /// &lt;Details&gt; rows for every group instance, but never its &lt;Footer&gt; rows. The same table at
    /// the top level of the body rendered its footer correctly. <c>Table.RunPage</c> only ran the footer
    /// when its <c>row</c> parameter was null, but that parameter is just the outer binding context
    /// (null at the top of the Body, the current group row when nested in a List), not a "last page"
    /// flag -- so the footer was skipped for every table nested in a List.
    ///
    /// The footer's sum uses an explicit scope (the List's Grouping name) rather than a bare
    /// <c>=Sum(Fields!Amt.Value)</c>: an unscoped aggregate here defaults to the Table's whole bound
    /// dataset rather than the current List group (<c>Expression.FinalPass</c>'s ancestor walk stops
    /// at the Table, a DataRegion, before it would reach the List's Grouping) -- a separate scoping
    /// gap, not part of this fix, filed on its own.
    /// </summary>
    [TestFixture]
    public class TableFooterInGroupedListTests
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
                    <Field Name="Item"><DataField>Item</DataField></Field>
                    <Field Name="Amt"><DataField>Amt</DataField></Field>
                  </Fields>
                </DataSet>
              </DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <List Name="Groups">
                    <DataSetName>Rows</DataSetName>
                    <Width>6in</Width><Height>.6in</Height>
                    <Grouping Name="ByGrp"><GroupExpressions><GroupExpression>=Fields!Grp.Value</GroupExpression></GroupExpressions></Grouping>
                    <ReportItems>
                      <Table Name="T1">
                        <DataSetName>Rows</DataSetName>
                        <Top>0in</Top><Width>6in</Width>
                        <TableColumns><TableColumn><Width>3in</Width></TableColumn><TableColumn><Width>3in</Width></TableColumn></TableColumns>
                        <Header>
                          <TableRows><TableRow><Height>.2in</Height><TableCells>
                            <TableCell><ReportItems><Textbox Name="HdrItem"><Height>.2in</Height><Width>3in</Width><Value>Item</Value></Textbox></ReportItems></TableCell>
                            <TableCell><ReportItems><Textbox Name="HdrAmt"><Height>.2in</Height><Width>3in</Width><Value>Amt</Value></Textbox></ReportItems></TableCell>
                          </TableCells></TableRow></TableRows>
                        </Header>
                        <Details>
                          <DataSetName>Rows</DataSetName>
                          <TableRows><TableRow><Height>.2in</Height><TableCells>
                            <TableCell><ReportItems><Textbox Name="Item"><Height>.2in</Height><Width>3in</Width><Value>=Fields!Item.Value</Value></Textbox></ReportItems></TableCell>
                            <TableCell><ReportItems><Textbox Name="Amt"><Height>.2in</Height><Width>3in</Width><Value>=Fields!Amt.Value</Value></Textbox></ReportItems></TableCell>
                          </TableCells></TableRow></TableRows>
                        </Details>
                        <Footer>
                          <TableRows><TableRow><Height>.2in</Height><TableCells>
                            <TableCell><ReportItems><Textbox Name="FtrLabel"><Height>.2in</Height><Width>3in</Width><Value>="Total " &amp; Fields!Grp.Value</Value></Textbox></ReportItems></TableCell>
                            <TableCell><ReportItems><Textbox Name="FtrSum"><Height>.2in</Height><Width>3in</Width><Value>=Sum(Fields!Amt.Value, "ByGrp")</Value></Textbox></ReportItems></TableCell>
                          </TableCells></TableRow></TableRows>
                        </Footer>
                      </Table>
                    </ReportItems>
                  </List>
                </ReportItems>
              </Body>
            </Report>
            """;

        [Test]
        public async Task Table_footer_renders_once_per_list_group_instance()
        {
            var document = new XmlDocument();
            document.LoadXml(Rdl);
            var parser = new RDLParser(document) { SkipDatabaseSchemaValidation = true };
            var report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>()));

            var table = new DataTable("Rows");
            table.Columns.Add("Grp", typeof(string));
            table.Columns.Add("Item", typeof(string));
            table.Columns.Add("Amt", typeof(int));
            table.Rows.Add("A", "a1", 10);
            table.Rows.Add("A", "a2", 20);
            table.Rows.Add("B", "b1", 100);

            await report.DataSets["Rows"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var texts = pages.Cast<Page>()
                .SelectMany(p => p.Cast<PageItem>())
                .OfType<PageText>()
                .Select(t => t.Text)
                .ToList();

            Assert.That(texts, Does.Contain("Total A"));
            Assert.That(texts, Does.Contain("30"));
            Assert.That(texts, Does.Contain("Total B"));
            Assert.That(texts, Does.Contain("100"));
        }
    }
}
