using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// RDL 2008 replaced Table/Matrix/List with Tablix and gave Textbox a rich-text model, neither
    /// of which the 2005-era definition classes understand. Rdl2008Normalizer rewrites the document
    /// into the 2005 shape before parsing; these tests drive it through the public RDLParser, so
    /// they assert the behaviour callers actually get rather than the intermediate XML.
    /// </summary>
    [TestFixture]
    public class Rdl2008NormalizerTests
    {
        private const string Rdl2008Namespace =
            "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        private static string TablixReport (string rowHierarchy, string rows, string columnHierarchy = null, string ns = Rdl2008Namespace)
        {
            columnHierarchy ??= @"<TablixMembers><TablixMember /><TablixMember /></TablixMembers>";

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{ns}"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties>
        <DataProvider>SQLite</DataProvider>
        <ConnectString>Data Source=this-file-does-not-exist.db</ConnectString>
      </ConnectionProperties>
      <DataSourceID>{{00000000-0000-0000-0000-000000000001}}</DataSourceID>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields>
        <Field Name=""Name""><DataField>Name</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""Amount""><DataField>Amount</DataField><rd:TypeName>System.String</rd:TypeName></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <ReportItems>
      <Tablix Name=""Tablix1"">
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>2in</Width></TablixColumn>
            <TablixColumn><Width>2in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>{rows}</TablixRows>
        </TablixBody>
        <TablixColumnHierarchy>{columnHierarchy}</TablixColumnHierarchy>
        <TablixRowHierarchy>{rowHierarchy}</TablixRowHierarchy>
        <DataSetName>Data</DataSetName>
        <Top>0in</Top>
        <Height>0.5in</Height>
        <Width>4in</Width>
      </Tablix>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>4in</Width>
  <Page>
    <LeftMargin>0.25in</LeftMargin>
    <RightMargin>0.25in</RightMargin>
    <TopMargin>0.25in</TopMargin>
    <BottomMargin>0.25in</BottomMargin>
  </Page>
  <ReportUnitType>Inch</ReportUnitType>
  <ReportID>{{00000000-0000-0000-0000-000000000002}}</ReportID>
</Report>";
        }

        /// <summary>A 2008 Textbox: text lives in Paragraphs/TextRuns, not a direct Value.</summary>
        private static string Textbox (string name, string value, string style = "")
            => $@"<Textbox Name=""{name}"">
                    <CanGrow>true</CanGrow>
                    <KeepTogether>true</KeepTogether>
                    <Paragraphs><Paragraph><TextRuns><TextRun>
                      <Value>{value}</Value><Style>{style}</Style>
                    </TextRun></TextRuns><Style /></Paragraph></Paragraphs>
                    <Style><Border><Color>LightGrey</Color><Style>Solid</Style></Border></Style>
                  </Textbox>";

        private static string Row (string height, params string[] cells)
        {
            var body = string.Empty;
            foreach (var cell in cells)
                body += cell;

            return $"<TablixRow><Height>{height}</Height><TablixCells>{body}</TablixCells></TablixRow>";
        }

        private static string Cell (string contents) => $"<TablixCell><CellContents>{contents}</CellContents></TablixCell>";

        /// <summary>The placeholder 2008 emits for a position covered by a preceding ColSpan.</summary>
        private static string CoveredCell () => "<TablixCell />";

        private const string HeaderThenDetail = @"
            <TablixMembers>
              <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
              <TablixMember><Group Name=""Details"" /></TablixMember>
            </TablixMembers>";

        private static DataTable SampleData ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Amount", typeof (string));
            table.Rows.Add ("Widget", "10.00");
            table.Rows.Add ("Gadget", "20.00");
            return table;
        }

        private static async Task<Report> ParseAsync (string rdl)
        {
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            return await parser.Parse ();
        }

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        [Test]
        public async Task Tablix_ParsesWithoutErrors ()
        {
            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (Textbox ("H2", "Amount"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            using var report = await ParseAsync (rdl);

            Assert.That (report, Is.Not.Null, "a 2008 Tablix report should parse");
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "no fatal errors expected: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }

        [Test]
        public async Task Tablix_RendersHeaderAndDetailRows ()
        {
            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Product")), Cell (Textbox ("H2", "Price"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            var html = await RenderHtml (rdl);

            // The static header comes from the header row, the values from the two detail rows --
            // so this covers both the row classification and the Paragraphs/TextRuns collapse.
            Assert.That (html, Does.Contain ("Product"), "header row text missing");
            Assert.That (html, Does.Contain ("Widget"), "first detail row missing");
            Assert.That (html, Does.Contain ("Gadget"), "second detail row missing");
        }

        [Test]
        public async Task ColSpan_CoveredCellIsDropped_SoColumnCountMatches ()
        {
            // Row 1 is a single cell spanning both columns; 2008 follows it with an empty
            // TablixCell placeholder that 2005 must not receive, or the row overruns the table.
            var spanning = "<TablixCell><CellContents><ColSpan>2</ColSpan>" +
                Textbox ("Title", "Spanning title") + "</CellContents></TablixCell>";

            var rdl = TablixReport (HeaderThenDetail,
                $"<TablixRow><Height>0.25in</Height><TablixCells>{spanning}{CoveredCell ()}</TablixCells></TablixRow>" +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "column count mismatch: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }

        [Test]
        public async Task EmptyCellContents_StillProducesACell ()
        {
            // Present-but-empty CellContents is a genuinely blank cell (not a span placeholder);
            // 2005 requires exactly one report item in every cell.
            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (string.Empty)) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "blank cell rejected: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }

        [Test]
        public async Task StaticRowsInsideDetailGroup_AllRepeatPerRecord ()
        {
            // A "card" layout: the Details group nests several static rows, each of which should
            // repeat once per record rather than being treated as a page header.
            const string nested = @"
                <TablixMembers>
                  <TablixMember>
                    <Group Name=""Details"" />
                    <TablixMembers><TablixMember /><TablixMember /></TablixMembers>
                  </TablixMember>
                </TablixMembers>";

            var rdl = TablixReport (nested,
                Row ("0.25in", Cell (Textbox ("L1", "Name:")), Cell (Textbox ("V1", "=Fields!Name.Value"))) +
                Row ("0.25in", Cell (Textbox ("L2", "Amount:")), Cell (Textbox ("V2", "=Fields!Amount.Value"))));

            var html = await RenderHtml (rdl);

            Assert.That (html, Does.Contain ("Widget"));
            Assert.That (html, Does.Contain ("Gadget"), "the nested static rows should repeat for every record");
        }

        [Test]
        public async Task MultipleTextRuns_AreCombinedIntoOneExpression ()
        {
            var mixedRuns = @"<Textbox Name=""Mixed"">
                <Paragraphs><Paragraph><TextRuns>
                  <TextRun><Value>Item: </Value><Style /></TextRun>
                  <TextRun><Value>=Fields!Name.Value</Value><Style /></TextRun>
                </TextRuns></Paragraph></Paragraphs>
                <Style /></Textbox>";

            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (Textbox ("H2", "Amount"))) +
                Row ("0.25in", Cell (mixedRuns), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            var html = await RenderHtml (rdl);

            // The literal run and the field run must both survive, joined rather than one dropped.
            Assert.That (html, Does.Contain ("Item:"), "literal run lost");
            Assert.That (html, Does.Contain ("Widget"), "expression run lost");
        }

        [Test]
        public async Task Rdl2016Namespace_IsAlsoNormalized ()
        {
            const string ns2016 = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";

            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Product")), Cell (Textbox ("H2", "Price"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))),
                ns: ns2016);

            var html = await RenderHtml (rdl);

            Assert.That (html, Does.Contain ("Widget"), "2016 reports should normalize like 2008 ones");
        }

        [Test]
        public async Task Rdl2005Report_IsLeftAlone ()
        {
            // The normalizer must be inert on the format the engine already handles.
            const string rdl2005 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <Width>4in</Width>
  <Body>
    <ReportItems>
      <Textbox Name=""Only""><Value>Plain 2005 report</Value><Top>0in</Top><Left>0in</Left>
        <Height>0.25in</Height><Width>3in</Width></Textbox>
    </ReportItems>
    <Height>1in</Height>
  </Body>
</Report>";

            var parser = new RDLParser (rdl2005) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report, Is.Not.Null);
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4));
        }

        [Test]
        public async Task DynamicColumnHierarchy_IsReportedRatherThanMisrendered ()
        {
            // A dynamic column hierarchy is a pivot, which belongs on Matrix; converting it to a
            // Table would silently produce the wrong shape, so it must be refused loudly.
            const string dynamicColumns = @"
                <TablixMembers>
                  <TablixMember><Group Name=""ColGroup""><GroupExpressions>
                    <GroupExpression>=Fields!Name.Value</GroupExpression>
                  </GroupExpressions></Group></TablixMember>
                </TablixMembers>";

            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (Textbox ("H2", "Amount"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))),
                dynamicColumns);

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo (8),
                "an unsupported pivot layout should raise an error rather than render wrongly");
        }

        private static async Task<string> RenderHtml (string rdl)
        {
            using var report = await ParseAsync (rdl);

            Assert.That (report, Is.Not.Null, "report failed to parse");
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "parse errors: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));

            await report.DataSets["Data"].SetData (SampleData ());
            await report.RunGetData (null);

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);

            return memory.GetText ();
        }
    }
}
