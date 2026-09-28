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

        /// <param name="tablixExtras">
        /// Further children of the Tablix element: the 2008 vocabulary — SortExpressions,
        /// RepeatRowHeaders, PageBreak — that has to be translated or deliberately dropped.
        /// </param>
        /// <param name="nameFieldType">rd:TypeName for the Name field, to exercise the type map.</param>
        private static string TablixReport (string rowHierarchy, string rows, string columnHierarchy = null,
            string ns = Rdl2008Namespace, string tablixExtras = "", string nameFieldType = "System.String")
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
        <Field Name=""Name""><DataField>Name</DataField><rd:TypeName>{nameFieldType}</rd:TypeName></Field>
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
        {tablixExtras}
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
            // A pivot whose shape the Matrix converter cannot express (here: one leaf column in
            // the hierarchy but two body columns, and a header row mixed in with the row group)
            // must still be refused loudly rather than rendered wrongly.
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


        [Test]
        public async Task NestedTablix_InsideACell_IsAlsoConverted ()
        {
            // Converting the outer Tablix clones its cells into the new Table, so an inner
            // Tablix arrives as a fresh unconverted clone and needs a second pass.
            var inner = @"<Tablix Name=""Inner"">
                <TablixBody>
                  <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
                  <TablixRows><TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents>" +
                    Textbox ("InnerCell", "=Fields!Name.Value") + @"
                  </CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
                </TablixBody>
                <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
                <TablixRowHierarchy><TablixMembers><TablixMember><Group Name=""InnerDetails"" /></TablixMember></TablixMembers></TablixRowHierarchy>
                <DataSetName>Data</DataSetName>
              </Tablix>";

            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (Textbox ("H2", "Amount"))) +
                Row ("0.25in", Cell (inner), Cell (Textbox ("D2", "=Fields!Amount.Value"))));

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "nested tablix rejected: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }

        [Test]
        public async Task DesignerNamespaceElements_AreStripped ()
        {
            // rd:* elements are authoring metadata; inside CellContents the parser would treat
            // one as a dropped report item.
            var cellWithDesignerNoise = "<TablixCell><CellContents><rd:Selected>true</rd:Selected>" +
                Textbox ("D1", "=Fields!Name.Value") + "</CellContents></TablixCell>";

            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Name")), Cell (Textbox ("H2", "Amount"))) +
                $"<TablixRow><Height>0.25in</Height><TablixCells>{cellWithDesignerNoise}" +
                Cell (Textbox ("D2", "=Fields!Amount.Value")) + "</TablixCells></TablixRow>");

            using var report = await ParseAsync (rdl);

            var errors = string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ());
            Assert.That (errors, Does.Not.Contain ("rd:Selected"), "designer element leaked: " + errors);
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), errors);
        }

        [Test]
        public async Task MultiValueParameter_BareCountProperty_Parses ()
        {
            // Report Builder emits Parameters!X.Count (not Parameters!X.Value.Count).
            var rdl = TablixReport (HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "=Parameters!Clinics.Count")), Cell (Textbox ("H2", "Amount"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")), Cell (Textbox ("D2", "=Fields!Amount.Value"))))
                .Replace ("<DataSets>",
                    @"<ReportParameters><ReportParameter Name=""Clinics""><DataType>String</DataType><MultiValue>true</MultiValue></ReportParameter></ReportParameters><DataSets>");

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "Count expression rejected: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }
        #region Matrix (dynamic column hierarchy)

        /// <summary>
        /// A Tablix whose column hierarchy pivots on data. Body dimensions default to one cell,
        /// which is the leaf shape of a plain matrix; the groupings multiply it at run time.
        /// </summary>
        private static string MatrixTablixReport (string columnHierarchy, string rowHierarchy, string bodyRows,
            int bodyColumns = 1, string corner = "")
        {
            var columns = string.Empty;
            for (var i = 0; i < bodyColumns; i++)
                columns += "<TablixColumn><Width>1in</Width></TablixColumn>";

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2008Namespace}"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties>
        <DataProvider>SQLite</DataProvider>
        <ConnectString>Data Source=this-file-does-not-exist.db</ConnectString>
      </ConnectionProperties>
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
      <Tablix Name=""Pivot1"">
        <TablixBody>
          <TablixColumns>{columns}</TablixColumns>
          <TablixRows>{bodyRows}</TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers>{columnHierarchy}</TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy><TablixMembers>{rowHierarchy}</TablixMembers></TablixRowHierarchy>
        {corner}
        <RepeatColumnHeaders>true</RepeatColumnHeaders>
        <DataSetName>Data</DataSetName>
        <Top>0in</Top>
        <Height>0.5in</Height>
        <Width>4in</Width>
      </Tablix>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6in</Width>
</Report>";
        }

        private static string DynamicMember (string groupName, string groupExpression,
            string headerTextbox = null, string sortExpression = null, string nested = null)
            => $@"<TablixMember>
                    <Group Name=""{groupName}"">
                      <GroupExpressions><GroupExpression>{groupExpression}</GroupExpression></GroupExpressions>
                    </Group>
                    {(sortExpression == null ? string.Empty
                        : $"<SortExpressions><SortExpression><Value>{sortExpression}</Value></SortExpression></SortExpressions>")}
                    {(headerTextbox == null ? string.Empty
                        : $"<TablixHeader><Size>0.3in</Size><CellContents>{headerTextbox}</CellContents></TablixHeader>")}
                    {(nested == null ? string.Empty : $"<TablixMembers>{nested}</TablixMembers>")}
                  </TablixMember>";

        private static string StaticMember (string headerTextbox = null)
            => headerTextbox == null
                ? "<TablixMember />"
                : $@"<TablixMember><TablixHeader><Size>1in</Size><CellContents>{headerTextbox}</CellContents></TablixHeader></TablixMember>";

        [Test]
        public async Task DynamicColumns_ConvertToMatrix_AndPivotTheData ()
        {
            // One column per distinct Name, one static row: the defining matrix behaviour.
            var rdl = MatrixTablixReport (
                DynamicMember ("ColName", "=Fields!Name.Value", Textbox ("ColHead", "=Fields!Name.Value")),
                StaticMember (Textbox ("RowHead", "AmountsByName")),
                Row ("0.25in", Cell (Textbox ("Data", "=First(Fields!Amount.Value)"))));

            var html = await RenderHtml (rdl);

            Assert.That (html, Does.Contain ("Widget"), "first group instance header missing");
            Assert.That (html, Does.Contain ("Gadget"), "second group instance header missing");
            Assert.That (html, Does.Contain ("10.00"), "first pivoted cell missing");
            Assert.That (html, Does.Contain ("20.00"), "second pivoted cell missing");
            Assert.That (html, Does.Contain ("AmountsByName"), "row header missing");
        }

        [Test]
        public async Task DynamicRowsAndColumns_BothPivot ()
        {
            var rdl = MatrixTablixReport (
                DynamicMember ("ColAmount", "=Fields!Amount.Value", Textbox ("ColHead", "=Fields!Amount.Value")),
                DynamicMember ("RowName", "=Fields!Name.Value", Textbox ("RowHead", "=Fields!Name.Value")),
                Row ("0.25in", Cell (Textbox ("Data", "=Count(Fields!Name.Value)"))));

            var html = await RenderHtml (rdl);

            Assert.That (html, Does.Contain ("Widget").And.Contain ("Gadget"), "row group headers missing");
            Assert.That (html, Does.Contain ("10.00").And.Contain ("20.00"), "column group headers missing");
        }

        [Test]
        public async Task ColumnGroupSort_IsCarriedAcross ()
        {
            // Data order is Widget then Gadget; an ascending sort must flip the column order,
            // proving SortExpressions became 2005 Sorting rather than being dropped.
            var rdl = MatrixTablixReport (
                DynamicMember ("ColName", "=Fields!Name.Value", Textbox ("ColHead", "=Fields!Name.Value"),
                    sortExpression: "=Fields!Name.Value"),
                StaticMember (Textbox ("RowHead", "AmountsByName")),
                Row ("0.25in", Cell (Textbox ("Data", "=First(Fields!Amount.Value)"))));

            var html = await RenderHtml (rdl);

            Assert.That (html.IndexOf ("Gadget", StringComparison.Ordinal),
                Is.GreaterThanOrEqualTo (0).And.LessThan (html.IndexOf ("Widget", StringComparison.Ordinal)),
                "ascending sort should put Gadget before Widget");
        }

        [Test]
        public async Task CornerContent_IsPreserved ()
        {
            var corner = @"<TablixCorner><TablixCornerRows><TablixCornerRow>
                             <TablixCornerCell><CellContents>" + Textbox ("CornerBox", "CornerLabel") + @"</CellContents></TablixCornerCell>
                           </TablixCornerRow></TablixCornerRows></TablixCorner>";

            var rdl = MatrixTablixReport (
                DynamicMember ("ColName", "=Fields!Name.Value", Textbox ("ColHead", "=Fields!Name.Value")),
                StaticMember (Textbox ("RowHead", "AmountsByName")),
                Row ("0.25in", Cell (Textbox ("Data", "=First(Fields!Amount.Value)"))),
                corner: corner);

            var html = await RenderHtml (rdl);

            Assert.That (html, Does.Contain ("CornerLabel"));
        }

        [Test]
        public async Task StaticSiblingOfDynamicMember_IsRefused ()
        {
            // A static member alongside a dynamic one is a subtotal column. Converting without it
            // would silently drop a totals column from the output, so the region must refuse.
            var rdl = MatrixTablixReport (
                DynamicMember ("ColName", "=Fields!Name.Value", Textbox ("ColHead", "=Fields!Name.Value")) +
                StaticMember (Textbox ("TotalHead", "Total")),
                StaticMember (Textbox ("RowHead", "AmountsByName")),
                Row ("0.25in",
                    Cell (Textbox ("Data", "=First(Fields!Amount.Value)")) +
                    Cell (Textbox ("DataTotal", "=Sum(Fields!Amount.Value)"))),
                bodyColumns: 2);

            using var report = await ParseAsync (rdl);

            Assert.That (report.ErrorMaxSeverity, Is.GreaterThanOrEqualTo (8),
                "a subtotal layout the converter cannot express must refuse rather than drop the column");
        }

        #endregion

        /// <summary>
        /// A row-only Tablix becomes a Table, and 2008 puts its sort on the data region while
        /// 2005 puts it on the detail rows. Untranslated, the rows come out in whatever order
        /// the query returned — which is not what the report asked for, and says so nowhere.
        /// </summary>
        [Test]
        public async Task DetailSort_IsCarriedAcross ()
        {
            var rdl = TablixReport (
                HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Product")), Cell (Textbox ("H2", "Price"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")),
                                Cell (Textbox ("D2", "=Fields!Amount.Value"))),
                tablixExtras: @"<SortExpressions><SortExpression>
                                  <Value>=Fields!Name.Value</Value><Direction>Ascending</Direction>
                                </SortExpression></SortExpressions>");

            var html = await RenderHtml (rdl);

            // Data order is Widget then Gadget; ascending must flip them.
            Assert.That (html.IndexOf ("Gadget", StringComparison.Ordinal),
                Is.GreaterThanOrEqualTo (0).And.LessThan (html.IndexOf ("Widget", StringComparison.Ordinal)),
                "detail rows should be sorted ascending by Name");
        }

        /// <summary>
        /// The rest of the 2008 data-region vocabulary. RepeatRowHeaders has a 2005 equivalent
        /// and is translated; the frozen-pane hints describe a scrolling viewport that
        /// paginated output does not have, and are dropped. Either way the report must not
        /// accumulate an "unknown element" warning per occurrence on every single render.
        /// </summary>
        [Test]
        public async Task PaginationAndFrozenPaneHints_AreHandledWithoutWarnings ()
        {
            var rdl = TablixReport (
                HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Product")), Cell (Textbox ("H2", "Price"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")),
                                Cell (Textbox ("D2", "=Fields!Amount.Value"))),
                tablixExtras: @"<RepeatRowHeaders>true</RepeatRowHeaders>
                                <RepeatColumnHeaders>true</RepeatColumnHeaders>
                                <FixedRowHeaders>true</FixedRowHeaders>
                                <FixedColumnHeaders>true</FixedColumnHeaders>
                                <PageBreak><BreakLocation>End</BreakLocation></PageBreak>");

            using var report = await ParseAsync (rdl);

            Assert.That (Warnings (report), Has.None.Contains ("Unknown Table element"),
                "2008 data-region elements should be translated or dropped, not reported");
        }

        /// <summary>
        /// TypeCode has no member for Guid or DateTimeOffset, so Object is the right answer;
        /// formatting and comparison reach both through IFormattable and IComparable. What was
        /// wrong was reporting a correct mapping as an unrecognised type, once per field, on
        /// every render — eleven and eighteen times in one real report.
        /// </summary>
        [TestCase ("System.Guid")]
        [TestCase ("System.DateTimeOffset")]
        public async Task ClrTypesWithoutATypeCode_AreRecognised (string typeName)
        {
            var rdl = TablixReport (
                HeaderThenDetail,
                Row ("0.25in", Cell (Textbox ("H1", "Product")), Cell (Textbox ("H2", "Price"))) +
                Row ("0.25in", Cell (Textbox ("D1", "=Fields!Name.Value")),
                                Cell (Textbox ("D2", "=Fields!Amount.Value"))),
                nameFieldType: typeName);

            using var report = await ParseAsync (rdl);

            Assert.That (Warnings (report), Has.None.Contains ("is not a recognized type"),
                typeName + " should be a recognised type name");
        }

        private static string[] Warnings (Report report)
        {
            var items = report?.ErrorItems;
            if (items == null)
                return new string[0];

            var messages = new string[items.Count];
            for (var i = 0; i < items.Count; i++)
                messages[i] = items[i]?.ToString () ?? string.Empty;
            return messages;
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

