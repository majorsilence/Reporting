#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A constant style is evaluated once and its StyleInfo is shared by every item that uses it. Anything that
    /// has to change a StyleInfo for one item (the right alignment of a number, a chart's background) copies it first.
    /// </summary>
    [TestFixture]
    public class SharedStyleTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields>
        <Field Name=""Name""><DataField>Name</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""Amount""><DataField>Amount</DataField><rd:TypeName>System.Int32</rd:TypeName></Field>
      </Fields>
      <fyi:Rows xmlns:fyi=""http://www.fyireporting.com/schemas""><Row><Name>a</Name><Amount>1</Amount></Row><Row><Name>b</Name><Amount>2</Amount></Row><Row><Name>c</Name><Amount>3</Amount></Row></fyi:Rows>
    </DataSet>
  </DataSets>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Table Name=""Table1"">
        <DataSetName>Data</DataSetName>
        <Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>0.2in</Height>
        <TableColumns><TableColumn><Width>2in</Width></TableColumn><TableColumn><Width>2in</Width></TableColumn></TableColumns>
        <Details><TableRows><TableRow><Height>0.2in</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""NameCell""><Value>=Fields!Name.Value</Value><Style><FontSize>9pt</FontSize></Style></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""AmountCell""><Value>=Fields!Amount.Value</Value><Style><FontSize>9pt</FontSize></Style></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows></Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<List<PageText>> TextItems()
        {
            using var report = await new RDLParser(Rdl).Parse();
            await report.RunGetData();
            Pages pages = await report.BuildPages();
            var items = new List<PageText>();
            foreach (Page page in pages)
                foreach (PageItem pi in page)
                    if (pi is PageText pt)
                        items.Add(pt);
            return items;
        }

        [Test]
        public async Task ConstantStyle_IsSharedBetweenRows_AndNumbersGetTheirOwnCopyWithRightAlignment()
        {
            var items = await TextItems();
            var names = items.Where(i => i.Text is "a" or "b" or "c").ToList();
            var amounts = items.Where(i => i.Text is "1" or "2" or "3").ToList();
            Assert.That(names, Has.Count.EqualTo(3));
            Assert.That(amounts, Has.Count.EqualTo(3));

            // text cells: General alignment, one StyleInfo for all rows
            Assert.That(names.Select(n => n.SI.TextAlign), Is.All.EqualTo(TextAlignEnum.General));
            Assert.That(names.Select(n => n.SI).Distinct().Count(), Is.EqualTo(1), "a constant style is shared between rows");

            // number cells: right aligned, and changing one did not touch the text cells' shared style
            Assert.That(amounts.Select(a => a.SI.TextAlign), Is.All.EqualTo(TextAlignEnum.Right));
            Assert.That(amounts.Select(a => a.SI).Intersect(names.Select(n => n.SI)), Is.Empty);
            Assert.That(names[0].SI.FontSize, Is.EqualTo(9f).Within(0.1f));
            Assert.That(amounts[0].SI.FontSize, Is.EqualTo(9f).Within(0.1f));
        }

        [Test]
        public async Task EachRender_HasItsOwnStyles()
        {
            var first = await TextItems();
            var second = await TextItems();
            var a = first.First(i => i.Text == "a").SI;
            var b = second.First(i => i.Text == "a").SI;

            // shared within a render, never between reports (the cache is per Report)
            Assert.That(ReferenceEquals(a, b), Is.False);
            a.TextAlign = TextAlignEnum.Center;       // mutating one render's style...
            Assert.That(b.TextAlign, Is.EqualTo(TextAlignEnum.General));   // ...cannot reach another's
        }
    }
}
#endif
