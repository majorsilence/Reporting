#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #523: a Textbox with no TextAlign gets the RDL default, General, which the spec
    /// resolves to Left for strings and Right for numbers and dates. The resolution in
    /// Textbox.RunPage used to key off the field's declared/static type, which a DataTable
    /// pushed with no &lt;rd:TypeName&gt; never carries (it defaults to String), so numeric and
    /// date values stayed left-aligned. These pin the fix: resolution uses the actual runtime
    /// value instead.
    /// </summary>
    [TestFixture]
    public class GeneralTextAlignTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>6in</Width><PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>xxx</DataProvider><ConnectString>none</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText></CommandText></Query>
      <Fields>
        <Field Name=""Name""><DataField>Name</DataField></Field>
        <Field Name=""Amount""><DataField>Amount</DataField></Field>
        <Field Name=""OrderDate""><DataField>OrderDate</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <List Name=""Rows"">
        <DataSetName>Data</DataSetName>
        <Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>0.8in</Height>
        <ReportItems>
          <Textbox Name=""NameBox""><Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>0.2in</Height><Value>=Fields!Name.Value</Value></Textbox>
          <Textbox Name=""AmountBox""><Top>0.2in</Top><Left>0in</Left><Width>2in</Width><Height>0.2in</Height><Value>=Fields!Amount.Value</Value></Textbox>
          <Textbox Name=""DateBox""><Top>0.4in</Top><Left>0in</Left><Width>2in</Width><Height>0.2in</Height><Value>=Fields!OrderDate.Value</Value></Textbox>
          <Textbox Name=""FixedLeftAmountBox""><Top>0.6in</Top><Left>0in</Left><Width>2in</Width><Height>0.2in</Height><Value>=Fields!Amount.Value</Value><Style><TextAlign>Left</TextAlign></Style></Textbox>
        </ReportItems>
      </List>
    </ReportItems>
  </Body>
</Report>";

        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        // Boxes are stacked top to bottom in the RDL (NameBox, AmountBox, DateBox,
        // FixedLeftAmountBox); Y position, not text, tells them apart since the General and
        // FixedLeft boxes render the same text ("1234567").
        private static async Task<List<PageText>> RunAsync()
        {
            using var report = await new RDLParser(Rdl).Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                report.ErrorItems == null ? "(none)" : string.Join(" | ", report.ErrorItems.OfType<object>()));

            var dt = new DataTable();
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Amount", typeof(int));
            dt.Columns.Add("OrderDate", typeof(DateTime));
            dt.Rows.Add("hello", 1234567, new DateTime(2026, 1, 2));
            await report.DataSets["Data"].SetData(dt);

            await report.RunGetData();
            Pages pages = await report.BuildPages();

            var items = new List<PageText>();
            foreach (Page page in pages)
                foreach (PageItem pi in page)
                    if (pi is PageText pt)
                        items.Add(pt);
            return items.OrderBy(pt => pt.Y).ToList();
        }

        [Test]
        public async Task NumericField_WithNoDeclaredType_IsRightAlignedWhenGeneral()
        {
            var items = await RunAsync();
            Assert.That(items[1].Text, Is.EqualTo("1234567"));
            Assert.That(items[1].SI.TextAlign, Is.EqualTo(TextAlignEnum.Right));
        }

        [Test]
        public async Task DateField_WithNoDeclaredType_IsRightAlignedWhenGeneral()
        {
            var items = await RunAsync();
            var dateItem = items.Single(pt => pt.Text.Contains("2026"));
            Assert.That(dateItem.SI.TextAlign, Is.EqualTo(TextAlignEnum.Right));
        }

        [Test]
        public async Task StringField_StaysLeftAlignedWhenGeneral()
        {
            var items = await RunAsync();
            var nameBox = items.Single(pt => pt.Text == "hello");
            Assert.That(nameBox.SI.TextAlign, Is.EqualTo(TextAlignEnum.General));
        }

        [Test]
        public async Task ExplicitTextAlign_OnNumericField_IsNotTouched()
        {
            var items = await RunAsync();
            var amountItems = items.Where(pt => pt.Text == "1234567").OrderBy(pt => pt.Y).ToList();
            Assert.That(amountItems, Has.Count.EqualTo(2));
            Assert.That(amountItems[0].SI.TextAlign, Is.EqualTo(TextAlignEnum.Right), "the General box is resolved to Right");
            Assert.That(amountItems[1].SI.TextAlign, Is.EqualTo(TextAlignEnum.Left),
                "a textbox with an explicit TextAlign must keep it, even on a numeric field");
        }
    }
}
#endif
