using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A Hidden expression on a Tablix column member is the column's visibility, decided once
    /// for the table against its first row. The column positions were worked out against no
    /// row, so a field-based expression such as IIf(IsNothing(Fields!C2.Value), True, False)
    /// read the field as Nothing and hid the column there, while each row's cells, which read
    /// the field from their own row, still printed it, at the left edge of the page.
    /// </summary>
    [TestFixture]
    public class TablixColumnMemberVisibilityTests
    {
        private const float Inch = 72f;

        /// <param name="hidden">Each column's Hidden expression, or null for a member without Visibility.</param>
        private static string Rdl (string[] hidden, bool headerRow)
        {
            var columns = new StringBuilder ();
            var members = new StringBuilder ();
            var header = new StringBuilder ();
            var detail = new StringBuilder ();
            for (var c = 1; c <= hidden.Length; c++) {
                columns.Append ("<TablixColumn><Width>1in</Width></TablixColumn>");
                members.Append (hidden[c - 1] == null
                    ? "<TablixMember />"
                    : $"<TablixMember><Visibility><Hidden>{hidden[c - 1]}</Hidden></Visibility></TablixMember>");
                header.Append (Cell ($"H{c}", $"Head{c}"));
                detail.Append (Cell ($"D{c}", $"=Fields!C{c}.Value"));
            }

            var rows = (headerRow ? $"<TablixRow><Height>0.25in</Height><TablixCells>{header}</TablixCells></TablixRow>" : "")
                + $"<TablixRow><Height>0.25in</Height><TablixCells>{detail}</TablixCells></TablixRow>";
            var rowMembers = (headerRow ? "<TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>" : "")
                + @"<TablixMember><Group Name=""Details"" /></TablixMember>";

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields>
        <Field Name=""C1""><DataField>C1</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""C2""><DataField>C2</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""C3""><DataField>C3</DataField><rd:TypeName>System.String</rd:TypeName></Field>
        <Field Name=""Flag""><DataField>Flag</DataField><rd:TypeName>System.Boolean</rd:TypeName></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <ReportItems>
      <Tablix Name=""Tablix1"">
        <TablixBody>
          <TablixColumns>{columns}</TablixColumns>
          <TablixRows>{rows}</TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers>{members}</TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy><TablixMembers>{rowMembers}</TablixMembers></TablixRowHierarchy>
        <DataSetName>Data</DataSetName>
        <Top>0in</Top><Left>1in</Left>
        <Height>0.5in</Height>
        <Width>{hidden.Length}in</Width>
      </Tablix>
    </ReportItems>
    <Height>2in</Height>
  </Body>
  <Width>6in</Width>
  <Page>
    <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>
    <LeftMargin>0in</LeftMargin><RightMargin>0in</RightMargin>
    <TopMargin>0in</TopMargin><BottomMargin>0in</BottomMargin>
  </Page>
</Report>";
        }

        private static string Cell (string name, string value)
            => $@"<TablixCell><CellContents><Textbox Name=""{name}""><CanGrow>true</CanGrow><KeepTogether>true</KeepTogether>
                    <Paragraphs><Paragraph><TextRuns><TextRun><Value>{value}</Value><Style><FontSize>9pt</FontSize></Style></TextRun></TextRuns><Style /></Paragraph></Paragraphs>
                    <Style /></Textbox></CellContents></TablixCell>";

        /// <summary>Renders two rows, R1 and R2, whose C2 is <paramref name="c2"/> and whose Flag is <paramref name="flags"/>.</summary>
        private static async Task<List<PageText>> Printed (string[] hidden, bool headerRow,
            string c2 = "C2", params bool[] flags)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            using var report = await new RDLParser (Rdl (hidden, headerRow)) { SkipDatabaseSchemaValidation = true }.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));

            if (flags.Length == 0)
                flags = new[] { false, false };
            var data = new DataTable ();
            foreach (var name in new[] { "C1", "C2", "C3" })
                data.Columns.Add (name, typeof (string));
            data.Columns.Add ("Flag", typeof (bool));
            for (var r = 1; r <= flags.Length; r++)
                data.Rows.Add ($"R{r}C1", c2 == null ? null : $"R{r}{c2}", $"R{r}C3", flags[r - 1]);
            await report.DataSets["Data"].SetData (data);
            await report.RunGetData (null);

            var pages = await report.BuildPages ();
            return pages.Cast<Page> ().SelectMany (p => p.Cast<PageItem> ()).OfType<PageText> ().ToList ();
        }

        private static float X (List<PageText> texts, string text) => texts.Single (t => t.Text == text).X;

        private static void AssertColumnsAt (List<PageText> texts, bool headerRow, params float[] xs)
        {
            for (var c = 1; c <= xs.Length; c++) {
                if (headerRow)
                    Assert.That (X (texts, $"Head{c}"), Is.EqualTo (xs[c - 1]).Within (0.01f), $"Head{c}");
                Assert.That (X (texts, $"R1C{c}"), Is.EqualTo (xs[c - 1]).Within (0.01f), $"R1C{c}");
                Assert.That (X (texts, $"R2C{c}"), Is.EqualTo (xs[c - 1]).Within (0.01f), $"R2C{c}");
            }
        }

        // The form the RDL designers write: hide the column when its field is empty. Every row
        // has the field, so every column shows where its width puts it.
        [TestCase (false)]
        [TestCase (true)]
        public async Task AFieldExpressionThatIsFalse_KeepsEveryColumnInPlace (bool headerRow)
        {
            var hide = new[] { null,
                "=IIf(IsNothing(Fields!C2.Value), True, False)",
                "=IIf(IsNothing(Fields!C3.Value), True, False)" };

            var texts = await Printed (hide, headerRow);

            AssertColumnsAt (texts, headerRow, Inch, 2 * Inch, 3 * Inch);
        }

        [TestCase (false)]
        [TestCase (true)]
        public async Task AConstantFalse_KeepsEveryColumnInPlace (bool headerRow)
        {
            var texts = await Printed (new[] { "=False", "=False", "=False" }, headerRow);

            AssertColumnsAt (texts, headerRow, Inch, 2 * Inch, 3 * Inch);
        }

        // A hidden column prints nothing, and the columns after it move left into its place.
        [TestCase (false)]
        [TestCase (true)]
        public async Task AFieldExpressionThatIsTrue_HidesTheColumn (bool headerRow)
        {
            var texts = await Printed (new[] { null, "=Fields!Flag.Value", null }, headerRow, "C2", true, true);

            Assert.That (texts.Where (t => t.Text.EndsWith ("C2") || t.Text == "Head2"), Is.Empty);
            Assert.That (X (texts, "R1C3"), Is.EqualTo (2 * Inch).Within (0.01f));
            Assert.That (X (texts, "R2C3"), Is.EqualTo (2 * Inch).Within (0.01f));
            if (headerRow)
                Assert.That (X (texts, "Head3"), Is.EqualTo (2 * Inch).Within (0.01f));
        }

        [Test]
        public async Task AnEmptyFieldHidesItsColumn ()
        {
            var hide = new[] { null, "=IIf(IsNothing(Fields!C2.Value), True, False)", null };

            var texts = await Printed (hide, true, c2: null);

            Assert.That (texts.Where (t => t.Text == "Head2"), Is.Empty);
            Assert.That (X (texts, "Head3"), Is.EqualTo (2 * Inch).Within (0.01f));
            Assert.That (X (texts, "R2C3"), Is.EqualTo (2 * Inch).Within (0.01f));
        }

        // A column is hidden or shown for the whole table, as its first row decides; a later row
        // that would decide otherwise neither prints into the gap nor leaves a hole.
        [Test]
        public async Task TheFirstRowDecidesForEveryRow ()
        {
            var hide = new[] { null, "=Fields!Flag.Value", null };

            var hidden = await Printed (hide, false, "C2", true, false);
            Assert.That (hidden.Where (t => t.Text.EndsWith ("C2")), Is.Empty);
            Assert.That (X (hidden, "R2C3"), Is.EqualTo (2 * Inch).Within (0.01f));

            var shown = await Printed (hide, false, "C2", false, true);
            AssertColumnsAt (shown, false, Inch, 2 * Inch, 3 * Inch);
        }
    }
}
