#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// RDL 2008+ report and group Variables (Variables!Name.Value), and DocumentMapLabel, the
    /// 2008 name for Label (#427 phase 1).
    /// </summary>
    [TestFixture]
    public class Rdl2008VariablesTests
    {
        private const string Ns = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string Tb(string name, string value, string extra = "")
            => $@"<Textbox Name=""{name}"">{extra}<Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>";

        // A Tablix grouped by Name, a detail row of [Name] [share], and whatever the report adds.
        private static string Report(string share, string reportExtras = "", string groupExtras = "", string bodyExtras = "")
            => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""{Ns}"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <ReportParameters><ReportParameter Name=""Who""><DataType>String</DataType><DefaultValue><Values><Value>World</Value></Values></DefaultValue><Prompt>Who</Prompt></ReportParameter></ReportParameters>
  {reportExtras}
  <Body><Height>3in</Height><ReportItems>
    <Tablix Name=""T1"">
      <TablixBody>
        <TablixColumns><TablixColumn><Width>1in</Width></TablixColumn><TablixColumn><Width>1in</Width></TablixColumn></TablixColumns>
        <TablixRows><TablixRow><Height>0.25in</Height><TablixCells>
          <TablixCell><CellContents>{Tb("DName", "=Fields!Name.Value")}</CellContents></TablixCell>
          <TablixCell><CellContents>{Tb("DShare", share)}</CellContents></TablixCell>
        </TablixCells></TablixRow></TablixRows>
      </TablixBody>
      <TablixColumnHierarchy><TablixMembers><TablixMember /><TablixMember /></TablixMembers></TablixColumnHierarchy>
      <TablixRowHierarchy><TablixMembers>
        <TablixMember>
          <Group Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions>{groupExtras}</Group>
          <TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers>
        </TablixMember>
      </TablixMembers></TablixRowHierarchy>
      <DataSetName>Data</DataSetName>
      <Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>
    </Tablix>
    {bodyExtras}
  </ReportItems></Body>
  <Width>6in</Width>
  <Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
</Report>";

        private static async Task<Report> Parse(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            return await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
        }

        private static string Errors(Report report)
            => string.Join(" | ", report.ErrorItems?.Cast<object>().Select(o => o.ToString()) ?? Enumerable.Empty<string>());

        private static async Task<string[]> Texts(string rdl)
        {
            using var report = await Parse(rdl);
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), Errors(report));
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Amount", typeof(double));
            table.Rows.Add("A", 10.0);
            table.Rows.Add("A", 30.0);
            table.Rows.Add("B", 60.0);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Select(t => t.Text).ToArray();
        }

        [Test]
        public async Task AGroupVariable_HoldsTheGroupInstancesAggregate()
        {
            var texts = await Texts(Report(
                share: @"=Format(Fields!Amount.Value / Variables!GroupTotal.Value, ""0.00"")",
                groupExtras: @"<Variables><Variable Name=""GroupTotal""><Value>=Sum(Fields!Amount.Value)</Value></Variable></Variables>"));

            // A: 10/40, 30/40; B: 60/60.
            Assert.That(texts, Is.SupersetOf(new[] { "0.25", "0.75", "1.00" }));
        }

        [Test]
        public async Task AReportVariable_IsReadAnywhere_AndCanAggregateADataSet()
        {
            var texts = await Texts(Report(
                share: @"=Format(Fields!Amount.Value / Variables!GrandTotal.Value, ""0.00"")",
                reportExtras: @"<Variables>
                    <Variable Name=""GrandTotal""><Value>=Sum(Fields!Amount.Value, ""Data"")</Value></Variable>
                    <Variable Name=""Greeting""><Value>=""Hello "" &amp; Parameters!Who.Value</Value></Variable>
                  </Variables>",
                bodyExtras: $@"<Textbox Name=""Hi""><Top>2in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>
                    <Paragraphs><Paragraph><TextRuns><TextRun><Value>=Variables!Greeting.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>"));

            // 10, 30 and 60 of 100.
            Assert.That(texts, Is.SupersetOf(new[] { "0.10", "0.30", "0.60", "Hello World" }));
        }

        [Test]
        public async Task AGroupVariable_HidesAReportVariableOfTheSameName()
        {
            var texts = await Texts(Report(
                share: @"=Variables!Which.Value",
                reportExtras: @"<Variables><Variable Name=""Which""><Value>report</Value></Variable></Variables>",
                groupExtras: @"<Variables><Variable Name=""Which""><Value>group</Value></Variable></Variables>",
                bodyExtras: @"<Textbox Name=""Outside""><Top>2in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>
                    <Paragraphs><Paragraph><TextRuns><TextRun><Value>=Variables!Which.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>"));

            Assert.That(texts.Count(t => t == "group"), Is.EqualTo(3), "each detail row is in the group");
            Assert.That(texts.Count(t => t == "report"), Is.EqualTo(1), "the textbox outside the table sees the report's");
        }

        [Test]
        public async Task AnUnknownVariable_IsAnError()
        {
            using var report = await Parse(Report(share: "=Variables!Missing.Value"));
            Assert.That(report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo(8));
            Assert.That(Errors(report), Does.Contain("Variable 'Missing' not found"));
        }

        [Test]
        public async Task DocumentMapLabel_IsReadAsTheLabel_OnItemsAndGroups()
        {
            using var report = await Parse(Report(
                share: "=Fields!Amount.Value",
                groupExtras: "<DocumentMapLabel>=Fields!Name.Value</DocumentMapLabel>",
                bodyExtras: @"<Textbox Name=""Mapped""><DocumentMapLabel>Summary</DocumentMapLabel><Top>2in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width>
                    <Paragraphs><Paragraph><TextRuns><TextRun><Value>x</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox>"));

            Assert.That(Errors(report), Does.Not.Contain("DocumentMapLabel"));
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), Errors(report));
        }

        [Test]
        public void TheDesignerRoundTrip_KeepsVariablesAndDocumentMapLabels()
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(Report(
                share: "=Variables!GroupTotal.Value",
                reportExtras: @"<Variables><Variable Name=""GrandTotal""><Value>=Sum(Fields!Amount.Value, ""Data"")</Value></Variable></Variables>",
                groupExtras: @"<DocumentMapLabel>=Fields!Name.Value</DocumentMapLabel><Variables><Variable Name=""GroupTotal""><Value>=Sum(Fields!Amount.Value)</Value></Variable></Variables>"));

            Rdl2008Exporter.NormalizeForEditing(doc);
            Rdl2008Exporter.ConvertToTablix(doc);

            var group = doc.GetElementsByTagName("Group").Cast<System.Xml.XmlElement>().Single(g => g.GetAttribute("Name") == "ByName");
            Assert.That(group["DocumentMapLabel"]?.InnerText, Is.EqualTo("=Fields!Name.Value"));
            Assert.That(group["Variables"]?["Variable"]?.GetAttribute("Name"), Is.EqualTo("GroupTotal"));
            Assert.That(doc.DocumentElement["Variables"]?["Variable"]?.GetAttribute("Name"), Is.EqualTo("GrandTotal"));
        }
    }
}
#endif
