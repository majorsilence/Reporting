using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A Rectangle marked <c>&lt;Custom&gt;&lt;DrawBeneath&gt;true&lt;/DrawBeneath&gt;&lt;/Custom&gt;</c>
    /// is drawn beneath what follows it and takes no room, as a watermark does. A table row whose
    /// Rectangle is so marked takes only its own height, which may be none, and the rows after it
    /// print over it; the group header, detail record or footer it is in still ends below the
    /// Rectangle's Height, so the group's footer and the next group print below it. A row grew to
    /// hold everything drawn in it, so a backdrop pushed the rows it was meant to be under down
    /// the page, past it. A negative ZIndex alone is draw order only, and changes no layout.
    /// </summary>
    [TestFixture]
    public class DrawnBeneathRowTests
    {
        private static string Row(string height, string items) => $"""
            <TableRow><Height>{height}</Height><TableCells><TableCell><ReportItems>
            {items}
            </ReportItems></TableCell></TableCells></TableRow>
            """;

        private static string Text(string name, string value, string top = "0in") => $"""
            <Textbox Name="{name}"><Top>{top}</Top><Left>0in</Left><Width>2in</Width><Height>.25in</Height><Value>{value}</Value></Textbox>
            """;

        private const string Beneath = "<Custom><DrawBeneath>true</DrawBeneath></Custom>";

        // A Rectangle drawn beneath, 3in tall, in a row of no height: a backdrop with a line of
        // text at its top and one at its foot. Unmarked, it is an ordinary Rectangle at a
        // negative ZIndex.
        private static string Backdrop(string prefix, bool marked = true) => Row("0in", $"""
            <Rectangle Name="{prefix}Backdrop"><ZIndex>-1</ZIndex><Height>3in</Height>{(marked ? Beneath : "")}<ReportItems>
            {Text(prefix + "Top", "=\"" + prefix + "-top \" &amp; Fields!Bill.Value")}
            {Text(prefix + "Foot", "=\"" + prefix + "-foot \" &amp; Fields!Bill.Value", "2.75in")}
            </ReportItems></Rectangle>
            """);

        private static string Report(string header, string details, string footer) => $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>
              <TopMargin>.5in</TopMargin><BottomMargin>.5in</BottomMargin>
              <Width>6in</Width>
              <DataSources><DataSource Name="Pushed"><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
              <DataSets><DataSet Name="Data"><Query><DataSourceName>Pushed</DataSourceName><CommandText>none</CommandText></Query>
                <Fields><Field Name="Bill"><DataField>Bill</DataField></Field><Field Name="Line"><DataField>Line</DataField></Field></Fields>
              </DataSet></DataSets>
              <Body>
                <Height>1in</Height>
                <ReportItems>
                  <Table Name="Bills">
                    <DataSetName>Data</DataSetName>
                    <TableColumns><TableColumn><Width>6in</Width></TableColumn></TableColumns>
                    <TableGroups><TableGroup>
                      <Grouping Name="ByBill"><GroupExpressions><GroupExpression>=Fields!Bill.Value</GroupExpression></GroupExpressions></Grouping>
                      <Header><TableRows>{header}</TableRows></Header>
                      <Footer><TableRows>{footer}</TableRows></Footer>
                    </TableGroup></TableGroups>
                    <Details><TableRows>{details}</TableRows></Details>
                  </Table>
                </ReportItems>
              </Body>
            </Report>
            """;

        private static readonly string LineRow = Row(".25in", "<Textbox Name=\"Line\"><Value>=Fields!Line.Value</Value></Textbox>");
        private static readonly string TotalRow = Row(".25in", "<Textbox Name=\"Total\"><Value>=\"total \" &amp; Fields!Bill.Value</Value></Textbox>");

        private static async Task<List<Page>> Build(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl).Parse();
            var table = new DataTable();
            table.Columns.Add("Bill", typeof(string));
            table.Columns.Add("Line", typeof(string));
            table.Rows.Add("B1", "L1");
            table.Rows.Add("B1", "L2");
            table.Rows.Add("B2", "L3");
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().ToList();
        }

        private static float Top(Page page, string text) =>
            page.Cast<PageItem>().OfType<PageText>().Single(t => t.Text == text).Y;

        [Test]
        public async Task AGroupHeaderDrawnBeneath_HasItsDetailsPrintedOverIt_AndItsFooterBelowIt()
        {
            var pages = await Build(Report(Backdrop("H"), LineRow, TotalRow));
            var page = pages.Single();

            Assert.Multiple(() =>
            {
                // The page's top margin is 36pt; the backdrop is 3in, 216pt, tall.
                Assert.That(Top(page, "H-top B1"), Is.EqualTo(36f).Within(0.01f));
                Assert.That(Top(page, "H-foot B1"), Is.EqualTo(234f).Within(0.01f));
                // The details print from the group's top, over the backdrop.
                Assert.That(Top(page, "L1"), Is.EqualTo(36f).Within(0.01f));
                Assert.That(Top(page, "L2"), Is.EqualTo(54f).Within(0.01f));
                // The footer, and the next group, print below it.
                Assert.That(Top(page, "total B1"), Is.EqualTo(252f).Within(0.01f));
                Assert.That(Top(page, "H-top B2"), Is.EqualTo(270f).Within(0.01f));
                Assert.That(Top(page, "L3"), Is.EqualTo(270f).Within(0.01f));
                Assert.That(Top(page, "total B2"), Is.EqualTo(486f).Within(0.01f));
            });
        }

        [Test]
        public async Task WhatARectangleDrawnBeneathHolds_IsDrawnBeneathTheRest()
        {
            var pages = await Build(Report(Backdrop("H"), LineRow, TotalRow));
            var texts = pages.Single().Cast<PageItem>().OfType<PageText>().ToList();
            pages.Single().SortPageItems();
            var drawn = pages.Single().Cast<PageItem>().OfType<PageText>().Select(t => t.Text).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(texts.Where(t => t.Text.StartsWith("H-")).Select(t => t.ZIndex), Is.All.EqualTo(-1));
                Assert.That(texts.Where(t => !t.Text.StartsWith("H-")).Select(t => t.ZIndex), Is.All.EqualTo(0));
                // Drawn first, so everything else is drawn over them.
                Assert.That(drawn.Take(4), Is.All.StartsWith("H-"));
            });
        }

        [Test]
        public async Task ARecordsRowDrawnBeneath_HasTheRecordsOtherRowsOverIt_AndTheNextRecordBelowIt()
        {
            var pages = await Build(Report(Row(".25in", Text("Head", "=Fields!Bill.Value")),
                Row("0in", """
                    <Rectangle Name="Card"><ZIndex>-1</ZIndex><Height>1in</Height><Custom><DrawBeneath>true</DrawBeneath></Custom><ReportItems>
                    <Textbox Name="CardText"><Top>.5in</Top><Left>0in</Left><Width>2in</Width><Height>.25in</Height><Value>="card " &amp; Fields!Line.Value</Value></Textbox>
                    </ReportItems></Rectangle>
                    """) + LineRow,
                TotalRow));
            var page = pages.Single();

            Assert.Multiple(() =>
            {
                Assert.That(Top(page, "L1"), Is.EqualTo(54f).Within(0.01f));
                Assert.That(Top(page, "card L1"), Is.EqualTo(90f).Within(0.01f));
                // The next record starts below the first's 1in card, not below its line.
                Assert.That(Top(page, "L2"), Is.EqualTo(126f).Within(0.01f));
                Assert.That(Top(page, "total B1"), Is.EqualTo(198f).Within(0.01f));
            });
        }

        [Test]
        public async Task ARectangleDrawnBeneath_InAnOrdinaryRow_DoesNotMakeItTaller()
        {
            // A row's Rectangle holding a heading and, 2in down, a mark drawn beneath: the row
            // stays .25in tall.
            var pages = await Build(Report(Row(".25in", $"""
                    <Rectangle Name="Band"><ReportItems>
                    <Textbox Name="Head"><Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>.25in</Height><Value>=Fields!Bill.Value</Value></Textbox>
                    <Rectangle Name="MarkBox"><ZIndex>-1</ZIndex><Top>2in</Top><Left>3in</Left><Width>2in</Width><Height>.25in</Height>{Beneath}<ReportItems>
                    <Textbox Name="Mark"><Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>.25in</Height><Value>="mark " &amp; Fields!Bill.Value</Value></Textbox>
                    </ReportItems></Rectangle>
                    </ReportItems></Rectangle>
                    """), LineRow, TotalRow));
            var page = pages.Single();

            Assert.Multiple(() =>
            {
                Assert.That(Top(page, "mark B1"), Is.EqualTo(180f).Within(0.01f));
                Assert.That(Top(page, "L1"), Is.EqualTo(54f).Within(0.01f));
                Assert.That(Top(page, "total B1"), Is.EqualTo(90f).Within(0.01f));
            });
        }

        [Test]
        public async Task ANegativeZIndexAlone_ChangesNoLayout()
        {
            // The same backdrop, unmarked: its row grows to hold what it draws, 3in, and the
            // details print below it, as before.
            var pages = await Build(Report(Backdrop("H", marked: false), LineRow, TotalRow));
            var page = pages.Single();

            Assert.Multiple(() =>
            {
                Assert.That(Top(page, "H-foot B1"), Is.EqualTo(234f).Within(0.01f));
                Assert.That(Top(page, "L1"), Is.EqualTo(252f).Within(0.01f));
                Assert.That(Top(page, "total B1"), Is.EqualTo(288f).Within(0.01f));
                Assert.That(page.Cast<PageItem>().OfType<PageText>().Where(t => t.Text.StartsWith("H-")).Select(t => t.ZIndex),
                    Is.All.EqualTo(0), "its contents keep their own ZIndex");
            });
        }
    }
}
