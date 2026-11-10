using Majorsilence.Pdf;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using Letter = UglyToad.PdfPig.Content.Letter;

namespace ReportTests
{
    /// <summary>
    /// Right-to-left and complex-script text in PDF output (#638): the Unicode Bidirectional
    /// Algorithm orders each line, HarfBuzz shapes it (Arabic joining forms and ligatures,
    /// Devanagari conjuncts), Direction is the paragraph direction and General alignment starts at
    /// the right edge of a right-to-left box. The text is read back from the PDF with PdfPig:
    /// each glyph's position, and its text from the ToUnicode CMap.
    /// </summary>
    [TestFixture]
    [NonParallelizable]     // PdfFonts registrations are process-wide
    public class ComplexScriptPdfTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            PdfFonts.Reset();
            _folder = Path.Combine(Path.GetTempPath(), "rdl-complex-script-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            PdfFonts.Reset();
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }

        private static string Rdl(string value, string style, string font) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin>
              <TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin>
              <Width>6.5in</Width>
              <Body>
                <Height>2in</Height>
                <ReportItems>
                  <Textbox Name="T">
                    <Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>0.5in</Height>
                    <Value>{value}</Value><CanGrow>true</CanGrow>
                    <Style><FontFamily>{font}</FontFamily><FontSize>16pt</FontSize>{style}
                      <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight></Style>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        private async Task<IReadOnlyList<Letter>> RenderLetters(string value, string style = "", string font = "Arial")
        {
            using var report = await new RDLParser(Rdl(value, style, font)) { Folder = _folder }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            await report.RunGetData(null);
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.PDF);
            byte[] pdf = ((MemoryStream)sg.GetStream()).ToArray();
            await File.WriteAllBytesAsync(Path.Combine(_folder, "out.pdf"), pdf);

            using var doc = PdfPigDocument.Open(pdf);
            return doc.GetPage(1).Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
        }

        // The page's text read right to left: the order a right-to-left reader takes the glyphs in.
        private static string RightToLeft(IEnumerable<Letter> letters) =>
            string.Concat(letters.OrderByDescending(l => l.StartBaseLine.X).Select(l => l.Value));

        private const float BoxLeft = 72f, BoxRight = 72f + 4 * 72f;

        [Test]
        public async Task Arabic_IsShaped_AndReadsRightToLeft()
        {
            // seen, lam, alef, meem: lam and alef make one ligature glyph, and every letter joins.
            var letters = await RenderLetters("سلام", "<Direction>RTL</Direction>");

            Assert.That(letters, Has.Count.EqualTo(3), "lam-alef is one glyph: the text was shaped");
            Assert.That(RightToLeft(letters), Is.EqualTo("سلام"), "the first letter is on the right; the ligature reads as lam and alef");
        }

        [Test]
        public async Task Hebrew_ReadsRightToLeft_AndGeneralAlignmentIsTheRightEdge()
        {
            var letters = await RenderLetters("שלום", "<Direction>RTL</Direction>");

            Assert.That(RightToLeft(letters), Is.EqualTo("שלום"));
            float right = letters.Max(l => (float)l.EndBaseLine.X);
            Assert.That(right, Is.EqualTo(BoxRight).Within(1.5f), "a right-to-left box starts at its right edge");
        }

        [Test]
        public async Task LeftToRightBox_KeepsGeneralAlignmentLeft_AndStillOrdersHebrew()
        {
            var letters = await RenderLetters("שלום");

            Assert.That(RightToLeft(letters), Is.EqualTo("שלום"), "Hebrew is right to left in a left-to-right box too");
            Assert.That(letters.Min(l => (float)l.StartBaseLine.X), Is.EqualTo(BoxLeft).Within(1.5f));
        }

        [Test]
        public async Task ArabicWithNumbers_KeepsTheNumberLeftToRight()
        {
            // "the number 123" - the digits read left to right inside the right-to-left line.
            var letters = await RenderLetters("العدد 123", "<Direction>RTL</Direction>");

            var digits = letters.Where(l => l.Value.Length == 1 && char.IsDigit(l.Value[0])).ToList();
            Assert.That(digits.Select(d => d.Value), Is.EqualTo(new[] { "1", "2", "3" }), "digits in the order drawn");
            Assert.That(digits[0].StartBaseLine.X, Is.LessThan(digits[1].StartBaseLine.X));
            Assert.That(digits[1].StartBaseLine.X, Is.LessThan(digits[2].StartBaseLine.X));

            var arabic = letters.Where(l => !char.IsDigit(l.Value[0])).ToList();
            Assert.That(arabic.Min(l => l.StartBaseLine.X), Is.GreaterThan(digits.Max(d => d.StartBaseLine.X)),
                "the word that comes first is at the right, the number after it to its left");
        }

        [Test]
        public async Task HebrewWithEnglish_KeepsTheEnglishWordsInOrder()
        {
            var letters = await RenderLetters("שלום Hello World", "<Direction>RTL</Direction>");

            var latin = letters.Where(l => l.Value[0] < 0x0590).OrderBy(l => l.StartBaseLine.X).ToList();
            Assert.That(string.Concat(latin.Select(l => l.Value)), Is.EqualTo("HelloWorld"),
                "the English phrase reads left to right, its words in order");
            var hebrew = letters.Where(l => l.Value[0] >= 0x0590).ToList();
            Assert.That(hebrew.Min(l => l.StartBaseLine.X), Is.GreaterThan(latin.Max(l => l.StartBaseLine.X)),
                "the Hebrew word that starts the line is at its right");
        }

        [Test]
        public async Task JustifiedRightToLeft_SpreadsLinesAndEndsAtTheRightEdge()
        {
            // Long enough to wrap in the 4in box: every line but the last is spread to both edges,
            // and the last starts at the right edge, where right-to-left text starts.
            const string text = "هذا نص عربي طويل يلتف على عدة أسطر ويجب أن يضبط من اليمين إلى اليسار بشكل صحيح تماما";
            var letters = await RenderLetters(text, "<Direction>RTL</Direction><TextAlign>Justified</TextAlign>");

            var lines = letters.GroupBy(l => Math.Round(l.StartBaseLine.Y)).OrderByDescending(g => g.Key).ToList();
            Assert.That(lines.Count, Is.GreaterThan(1), "the text wraps");
            foreach (var line in lines.Take(lines.Count - 1))
            {
                Assert.That(line.Min(l => (float)l.StartBaseLine.X), Is.EqualTo(BoxLeft).Within(2f), "a spread line reaches the left edge");
                Assert.That(line.Max(l => (float)l.EndBaseLine.X), Is.EqualTo(BoxRight).Within(2f), "and the right");
            }
            var last = lines.Last();
            Assert.That(last.Max(l => (float)l.EndBaseLine.X), Is.EqualTo(BoxRight).Within(2f), "the last line starts at the right edge");
            Assert.That(last.Min(l => (float)l.StartBaseLine.X), Is.GreaterThan(BoxLeft + 10f), "and is not spread");
        }

        [Test]
        public async Task BidiOverride_DrawsEveryCharacterInTheBaseDirection()
        {
            var letters = await RenderLetters("abc", "<Direction>RTL</Direction><UnicodeBiDi>BiDi-Override</UnicodeBiDi>");

            Assert.That(string.Concat(letters.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value)), Is.EqualTo("cba"));
        }

        [Test]
        public async Task Devanagari_FormsConjuncts()
        {
            string font = Path.Combine(AppContext.BaseDirectory, "Fonts", "NotoSansDevanagari-Regular.ttf");
            Assert.That(File.Exists(font), Is.True, font);
            PdfFonts.RegisterFont("Noto Sans Devanagari", font);

            // ksha (ka, virama, ssa) is one conjunct glyph; the vowel sign i, typed after the ka it
            // belongs to, is drawn before it.
            var letters = await RenderLetters("क्ष कि", font: "Noto Sans Devanagari");

            Assert.That(letters, Has.Count.EqualTo(3), "ka-virama-ssa is one glyph: the text was shaped");
            var ksha = letters.OrderBy(l => l.StartBaseLine.X).First();
            Assert.That(ksha.Value, Is.EqualTo("क्ष"), "the conjunct reads as its three characters");
            var ki = letters.OrderBy(l => l.StartBaseLine.X).Skip(1).ToList();
            Assert.That(ki[0].Value, Is.Not.EqualTo("क"), "the vowel sign is drawn first, left of the ka");
            Assert.That(ki[1].Value, Is.EqualTo("क"));
        }
    }
}
