#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;

namespace ReportTests
{
    /// <summary>
    /// A word wider than its textbox's line is broken after the last letter that fits, and what is
    /// left of it goes on the next line, followed by the words after it, as Crystal Reports and SSRS
    /// print it. It was never broken: it ran out of the box, or was cut off at its side. A box one
    /// line tall that may not grow still sets it whole and cuts it at its edge, as Crystal does.
    /// </summary>
    [TestFixture]
    [NonParallelizable]     // PdfFonts registrations are process-wide
    public class LongWordBreakTests
    {
        // A monospaced font, registered with the PDF so that every machine draws in it: every
        // letter is 6pt at 10pt, so a 46pt line holds seven.
        private const string Mono = "Long Word Break Test Mono";

        [SetUp]
        public void Prepare()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            string dir = Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();
            PdfFonts.RegisterFont(Mono, regular: Path.Combine(dir, "LiberationMono-Regular.ttf"));
        }

        [TearDown]
        public void TearDown() => PdfFonts.Reset();

        private const string BoxStyle = $@"<Style><FontFamily>{Mono}</FontFamily><FontSize>10pt</FontSize>
              <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>";

        // A one-column Grid, a table with no data behind it: the growing cell in a 1pt row, and, when
        // asked, a row after it.
        private static string GridRdl(string value, float column, bool withNext) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>200pt</Height><ReportItems>
    <Grid Name=""T"">
      <Width>{column}pt</Width>
      <TableColumns><TableColumn><Width>{column}pt</Width></TableColumn></TableColumns>
      <Header><TableRows>
        <TableRow><Height>1pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Grow""><Value>{SecurityElement.Escape(value)}</Value><CanGrow>true</CanGrow>{BoxStyle}</Textbox>
        </ReportItems></TableCell></TableCells></TableRow>
        {(withNext ? @"<TableRow><Height>12pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Next""><Value>NEXT</Value><Style><FontSize>8pt</FontSize></Style></Textbox>
        </ReportItems></TableCell></TableCells></TableRow>" : "")}
      </TableRows></Header>
    </Grid>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        // A box of the given height, on its own in the body, that may not grow.
        private static string FixedBoxRdl(string value, float height) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>200pt</Height><ReportItems>
    <Textbox Name=""Fixed""><Top>0pt</Top><Left>0pt</Left><Width>46pt</Width><Height>{height}pt</Height>
      <Value>{SecurityElement.Escape(value)}</Value><CanGrow>false</CanGrow>{BoxStyle}</Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private static async Task<Report> Run(string rdl)
        {
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            return report;
        }

        // The letters of each line the PDF draws, topmost first; spaces are left out.
        private static async Task<string[]> LinesDrawn(string rdl, string name)
        {
            using var report = await Run(rdl);
            string output = Path.Combine(GeneralUtils.OutputTestsFolder().LocalPath, name + ".pdf");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var sg = new OneFileStreamGen(output, true))
                await report.RunRender(sg, OutputPresentationType.PDF);

            using var pdf = PdfPigDocument.Open(output);
            return pdf.GetPages().Single().Letters
                .Where(l => !string.IsNullOrWhiteSpace(l.Value))
                .GroupBy(l => Math.Round(l.StartBaseLine.Y, 1))
                .OrderByDescending(g => g.Key)
                .Select(g => string.Concat(g.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value)))
                .ToArray();
        }

        // The height the growing row is given: where the row after it starts.
        private static async Task<float> RowHeight(string value, float column)
        {
            using var report = await Run(GridRdl(value, column, withNext: true));
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single(t => t.Text == "NEXT").Y;
        }

        [Test]
        public async Task AWordWiderThanTheBox_IsBrokenAfterTheLastLetterThatFits()
        {
            string[] lines = await LinesDrawn(GridRdl("ABCDEFGHIJKLMNOPQRSTU", 46, withNext: false), "LongWordBreak_word");

            Assert.That(lines, Is.EqualTo(new[] { "ABCDEFG", "HIJKLMN", "OPQRSTU" }));
        }

        // "HIJ KL" is six letters wide: the rest of the broken word and the word after it share a line.
        [Test]
        public async Task TheRestOfABrokenWord_IsFollowedByTheWordsAfterIt()
        {
            string[] lines = await LinesDrawn(GridRdl("ABCDEFGHIJ KL MNOPQ", 46, withNext: false), "LongWordBreak_rest");

            Assert.That(lines, Is.EqualTo(new[] { "ABCDEFG", "HIJKL", "MNOPQ" }));
        }

        // The row is as tall as one holding three short lines in the same font: the lines drawn.
        [Test]
        public async Task ABoxThatMayGrow_GrowsByTheLinesTheWordIsBrokenInto()
        {
            float threeLines = await RowHeight("=\"1\" & Chr(10) & \"1\" & Chr(10) & \"1\"", 300);

            Assert.That(await RowHeight("ABCDEFGHIJKLMNOPQRSTU", 46), Is.EqualTo(threeLines).Within(0.05f));
        }

        // As before, a box that may not grow shows the lines that fit in it: here the first two.
        [Test]
        public async Task ABoxTwoLinesTallThatMayNotGrow_ShowsTheFirstTwoLinesOfTheBrokenWord()
        {
            string[] lines = await LinesDrawn(FixedBoxRdl("ABCDEFGHIJKLMNOPQRSTU", 24), "LongWordBreak_fixed2");

            Assert.That(lines, Is.EqualTo(new[] { "ABCDEFG", "HIJKLMN" }));
        }
    }
}
#endif