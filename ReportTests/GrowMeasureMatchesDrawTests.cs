#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;

namespace ReportTests
{
    /// <summary>
    /// Issue #511: a growing cell grows by the lines the PDF draws its text in, and no others. Its
    /// text was measured with a "w" added and wrapped by the graphics library, which breaks inside a
    /// word (after a hyphen, or between two letters) where the PDF breaks only at spaces, so a word
    /// that fits its cell with less than a "w" to spare made the row a line or two taller than the
    /// text drawn in it.
    /// </summary>
    [TestFixture]
    public class GrowMeasureMatchesDrawTests
    {
        private static string Pt(float points) => points.ToString("0.###", CultureInfo.InvariantCulture) + "pt";

        // A one-column Grid, a table with no data behind it: the growing cell in a 1pt row, and, when
        // asked, a row after it.
        private static string Rdl(string value, string family, float size, float column, float padLeft, bool withNext) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>200pt</Height><ReportItems>
    <Grid Name=""T"">
      <Width>{Pt(column)}</Width>
      <TableColumns><TableColumn><Width>{Pt(column)}</Width></TableColumn></TableColumns>
      <Header><TableRows>
        <TableRow><Height>1pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Grow""><Value>{SecurityElement.Escape(value)}</Value><CanGrow>true</CanGrow>
            <Style><FontFamily>{family}</FontFamily><FontSize>{Pt(size)}</FontSize>
              <PaddingLeft>{Pt(padLeft)}</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style></Textbox>
        </ReportItems></TableCell></TableCells></TableRow>
        {(withNext ? @"<TableRow><Height>12pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Next""><Value>NEXT</Value><Style><FontSize>8pt</FontSize></Style></Textbox>
        </ReportItems></TableCell></TableCells></TableRow>" : "")}
      </TableRows></Header>
    </Grid>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private static async Task<Report> Run(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            return report;
        }

        // The height the growing row is given: where the row after it starts.
        private static async Task<float> RowHeight(string value, string family, float size, float column, float padLeft)
        {
            using var report = await Run(Rdl(value, family, size, column, padLeft, withNext: true));
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single(t => t.Text == "NEXT").Y;
        }

        // The number of lines the PDF draws the value in.
        private static async Task<int> LinesDrawn(string value, string family, float size, float column, float padLeft)
        {
            using var report = await Run(Rdl(value, family, size, column, padLeft, withNext: false));
            string output = Path.Combine(GeneralUtils.OutputTestsFolder().LocalPath, "GrowMeasureMatchesDraw.pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var sg = new OneFileStreamGen(output, true))
                await report.RunRender(sg, OutputPresentationType.PDF);

            using var pdf = PdfPigDocument.Open(output);
            return pdf.GetPages().Single().Letters
                .Where(l => !string.IsNullOrWhiteSpace(l.Value))
                .Select(l => Math.Round(l.StartBaseLine.Y, 1)).Distinct().Count();
        }

        // The row is as tall as a row holding that many short lines in the same font, in a column
        // too wide to wrap them: the lines the PDF draws and no more.
        private static async Task AssertGrowsByTheLinesDrawn(string value, string family, float size, float column, float padLeft, int lines)
        {
            Assert.That(await LinesDrawn(value, family, size, column, padLeft), Is.EqualTo(lines), "lines the PDF draws");

            string shortLines = "=" + string.Join(" & Chr(10) & ", Enumerable.Repeat("\"1\"", lines));
            float expected = await RowHeight(shortLines, family, size, column: 300, padLeft: 0);
            Assert.That(await RowHeight(value, family, size, column, padLeft), Is.EqualTo(expected).Within(0.05f));
        }

        // The issue's repro: 45pt (60px) for the text, which fits it with less than a "w" to spare.
        [Test]
        public Task ADateThatFitsWithLittleToSpare_GrowsByOneLine()
            => AssertGrowsByTheLinesDrawn("01-01-1990", "Arial", 8, column: 53, padLeft: 8, lines: 1);

        [Test]
        public Task TextEndingInSuchADate_GrowsByOneLine()
            => AssertGrowsByTheLinesDrawn("Paid on 01-01-1990", "Arial", 8, column: 80, padLeft: 8, lines: 1);

        // "Total", Arial 10pt, in 30px.
        [Test]
        public Task AWordThatFitsWithLittleToSpare_GrowsByOneLine()
            => AssertGrowsByTheLinesDrawn("Total", "Arial", 10, column: 22.5f, padLeft: 0, lines: 1);

        // A word with no break in it was broken between its letters, one line for each. It is a
        // little wider than the column, and the PDF draws it on one line all the same.
        [Test]
        public Task TwoLettersInANarrowColumn_GrowByOneLine()
            => AssertGrowsByTheLinesDrawn("Nº", "Arial", 10, column: 38, padLeft: 28.79f, lines: 1);

        // A family the machine does not have is drawn in the PDF's substitute for it, as wide as
        // Arial, in which these fit with about 3pt to spare; in the system's fallback (Segoe UI,
        // DejaVu Sans) they are wider than the box.
        [Test]
        public Task AFamilyTheMachineLacks_GrowsByTheLinesItsSubstituteIsDrawnIn()
            => AssertGrowsByTheLinesDrawn("mmmmm mmmmm", "No Such Family 511", 20, column: 175, padLeft: 0, lines: 1);
    }
}
#endif
