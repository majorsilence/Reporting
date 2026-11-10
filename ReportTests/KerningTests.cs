using Majorsilence.Pdf;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using Letter = UglyToad.PdfPig.Content.Letter;

namespace ReportTests
{
    /// <summary>
    /// Pair kerning from the font's kern table: TrueTypeFont reads the pairs, a TextStyle with
    /// Kerning measures and draws text with them, and a report with
    /// &lt;Custom&gt;&lt;Kerning&gt;true&lt;/Kerning&gt;&lt;/Custom&gt; has its PDF text measured,
    /// wrapped, aligned and grown by the kerned widths. Without it nothing is kerned.
    /// </summary>
    [TestFixture]
    [NonParallelizable]     // PdfFonts registrations are process-wide
    public class KerningTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            PdfFonts.Reset();
            _folder = Path.Combine(Path.GetTempPath(), "rdl-kerning-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            PdfFonts.Reset();
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }

        private static string Bundled(string name)
        {
            string path = Path.Combine(Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory(), name);
            Assume.That(File.Exists(path), Is.True, name + " is not bundled");
            return path;
        }

        private static short Kern(TrueTypeFont font, char left, char right) =>
            font.GetKerning(font.GetGlyphId(left), font.GetGlyphId(right));

        // ── the kern table ────────────────────────────────────────────────────

        [Test]
        public void KernTable_GivesThePairsOfTheFont()
        {
            var font = new TrueTypeFont(Bundled("LiberationSans-Regular.ttf"));

            Assert.Multiple(() =>
            {
                Assert.That(font.UnitsPerEm, Is.EqualTo(2048));
                Assert.That(font.HasKerning, Is.True);
                Assert.That(Kern(font, 'T', 'a'), Is.EqualTo(-227));
                Assert.That(Kern(font, 'T', 'o'), Is.EqualTo(-227));
                Assert.That(Kern(font, 'A', 'V'), Is.EqualTo(-152));
                Assert.That(Kern(font, ' ', 'A'), Is.EqualTo(-113));
                Assert.That(Kern(font, 'x', 'e'), Is.EqualTo(0), "a pair the font does not list");
            });
        }

        [Test]
        public void AFontWithoutAKernTable_HasNoPairs()
        {
            var font = new TrueTypeFont(Bundled("LiberationMono-Regular.ttf"));

            Assert.That(font.HasKerning, Is.False);
            Assert.That(Kern(font, 'T', 'a'), Is.EqualTo(0));
            Assert.That(font.GetWidthPoint("Taxes", 10f, true), Is.EqualTo(font.GetWidthPoint("Taxes", 10f)));
        }

        [Test]
        public void KernedWidth_IsTheAdvancesPlusThePairs()
        {
            var font = new TrueTypeFont(Bundled("LiberationSans-Regular.ttf"));
            float plain = font.GetWidthPoint("Taxes", 10f);

            Assert.That(font.GetWidthPoint("Taxes", 10f, true), Is.EqualTo(plain - 227f / 2048f * 10f).Within(0.0001f));
            Assert.That(font.GetWidthPoint("Taxes", 10f, false), Is.EqualTo(plain));
        }

        // ── the canvas ────────────────────────────────────────────────────────

        [Test]
        public void Canvas_MeasuresKernedTextNarrower_OnlyWhenAsked()
        {
            string path = Bundled("LiberationSans-Regular.ttf");
            var font = new TrueTypeFont(path);
            var style = TextStyle.Default.WithFontFile(path).WithSize(16f);
            const string text = "Taxes Total";
            float plain = 0, kerned = 0;
            PdfDocument.Create().AddPage(PageSizes.A4, c =>
            {
                plain = c.MeasureTextWidth(text, style);
                kerned = c.MeasureTextWidth(text, style.WithKerning());
            });
            int pairs = 0;      // 'Ta', 'To' and, in this font, ' T'
            for (int i = 1; i < text.Length; i++)
                pairs += Kern(font, text[i - 1], text[i]);

            Assert.That(pairs, Is.LessThanOrEqualTo(-2 * 227));
            Assert.That(kerned - plain, Is.EqualTo(pairs / 2048f * 16f).Within(0.001f));
        }

        private static IReadOnlyList<Letter> Letters(byte[] pdf)
        {
            using var doc = PdfPigDocument.Open(pdf);
            return doc.GetPage(1).Letters.Where(l => !string.IsNullOrWhiteSpace(l.Value)).ToList();
        }

        [Test]
        public void Canvas_DrawsEachKernedPairCloser()
        {
            string path = Bundled("LiberationSans-Regular.ttf");
            var font = new TrueTypeFont(path);
            var style = TextStyle.Default.WithFontFile(path).WithSize(20f);
            byte[] Draw(TextStyle s) => PdfDocument.Create().AddPage(PageSizes.A4, c => c.DrawText("Taxes", 100, 100, s)).ToBytes();

            var plain = Letters(Draw(style));
            var kerned = Letters(Draw(style.WithKerning()));
            float advanceT = font.GetWidthPoint("T", 20f);

            Assert.That(plain[1].StartBaseLine.X - plain[0].StartBaseLine.X, Is.EqualTo(advanceT).Within(0.05));
            Assert.That(kerned[1].StartBaseLine.X - kerned[0].StartBaseLine.X,
                Is.EqualTo(advanceT - 227f / 2048f * 20f).Within(0.05), "'a' is set the pair's kerning closer to 'T'");
            Assert.That(kerned[4].EndBaseLine.X - kerned[0].StartBaseLine.X,
                Is.EqualTo(font.GetWidthPoint("Taxes", 20f, true)).Within(0.05), "drawn as wide as measured");
        }

        [Test]
        public void Canvas_RightAlignsKernedTextByItsKernedWidth()
        {
            string path = Bundled("LiberationSans-Regular.ttf");
            var style = TextStyle.Default.WithFontFile(path).WithSize(20f).WithKerning().WithAlignment(TextAlignment.Right);
            var letters = Letters(PdfDocument.Create().AddPage(PageSizes.A4, c => c.DrawText("Taxes", 300, 100, style)).ToBytes());

            Assert.That(letters.Last().EndBaseLine.X, Is.EqualTo(300).Within(0.05));
        }

        // ── reports ───────────────────────────────────────────────────────────

        private const float BoxLeft = 72f;

        private static string Rdl(string value, string width, string style, bool kerning) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin>
              <TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin>
              <Width>6.5in</Width>
              {(kerning ? "<Custom><Kerning>true</Kerning></Custom>" : "")}
              <Body>
                <Height>2in</Height>
                <ReportItems>
                  <Textbox Name="T">
                    <Top>0in</Top><Left>0in</Left><Width>{width}</Width><Height>12pt</Height>
                    <Value>{value}</Value><CanGrow>true</CanGrow>
                    <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>{style}
                      <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
                      <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
                  </Textbox>
                </ReportItems>
              </Body>
            </Report>
            """;

        private async Task<IReadOnlyList<Letter>> RenderLetters(string rdl)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            await report.RunGetData(null);
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.PDF);
            return Letters(((MemoryStream)sg.GetStream()).ToArray());
        }

        // The font the PDF draws the reports' Arial in: Arial, or Liberation Sans where Arial is not installed.
        private static TrueTypeFont DrawnArial()
        {
            var font = RenderPdf_Raw.DrawnFont("Arial", false, false);
            Assume.That(font != null && font.HasKerning, "Arial is drawn in a font with a kern table");
            return font;
        }

        private static string Points(float pt) => pt.ToString("0.###", CultureInfo.InvariantCulture) + "pt";

        [Test]
        public async Task Report_WithKerning_DrawsAKernedWordNarrower()
        {
            var font = DrawnArial();
            var plain = await RenderLetters(Rdl("Taxes", "4in", "", kerning: false));
            var kerned = await RenderLetters(Rdl("Taxes", "4in", "", kerning: true));

            float Span(IReadOnlyList<Letter> l) => (float)(l.Last().EndBaseLine.X - l[0].StartBaseLine.X);
            float pair = -Kern(font, 'T', 'a') / (float)font.UnitsPerEm * 10f;
            Assert.That(pair, Is.GreaterThan(0.5f));
            Assert.That(Span(plain) - Span(kerned), Is.EqualTo(pair).Within(0.05), "'Ta' is kerned");
            Assert.That(kerned[0].StartBaseLine.X, Is.EqualTo(BoxLeft).Within(0.05));
        }

        [Test]
        public async Task Report_WithKerning_RightAlignsByTheKernedWidth()
        {
            var font = DrawnArial();
            var letters = await RenderLetters(Rdl("Taxes Total", "4in", "<TextAlign>Right</TextAlign>", kerning: true));

            float kernedWidth = font.GetWidthPoint("Taxes Total", 10f, true);
            Assert.That(letters[0].StartBaseLine.X, Is.EqualTo(BoxLeft + 4 * 72f - kernedWidth).Within(0.05),
                "the line starts its kerned width short of the right edge");
            Assert.That(letters.Last().EndBaseLine.X, Is.EqualTo(BoxLeft + 4 * 72f).Within(0.05));
        }

        // A box between the line's kerned and unkerned widths: kerned, the line fits it, is drawn on
        // one line, and the growing box is measured one line tall.
        private const string PairedLine = "Ta To Ta To Ta To Ta To";

        private static string BetweenWidths(TrueTypeFont font) =>
            Points((font.GetWidthPoint(PairedLine, 10f, true) + font.GetWidthPoint(PairedLine, 10f)) / 2f);

        [Test]
        public async Task Report_WithKerning_WrapsByTheKernedWidth()
        {
            var font = DrawnArial();
            string width = BetweenWidths(font);
            var plain = await RenderLetters(Rdl(PairedLine, width, "", kerning: false));
            var kerned = await RenderLetters(Rdl(PairedLine, width, "", kerning: true));

            int Lines(IReadOnlyList<Letter> l) => l.Select(x => Math.Round(x.StartBaseLine.Y, 1)).Distinct().Count();
            Assert.That(Lines(plain), Is.EqualTo(2), "unkerned, the line is wider than the box");
            Assert.That(Lines(kerned), Is.EqualTo(1), "kerned, it fits");
        }

        [Test]
        public async Task Report_WithKerning_GrowsTheBoxByTheKernedLines()
        {
            var font = DrawnArial();
            string width = BetweenWidths(font);

            async Task<float> Height(bool kerning)
            {
                using var report = await new RDLParser(Rdl(PairedLine, width, "", kerning)) { Folder = _folder }.Parse();
                await report.RunGetData(null);
                var pages = await report.BuildPages();
                return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                    .Single(t => t.Text == PairedLine).H;
            }

            float plain = await Height(false);
            float kerned = await Height(true);
            Assert.That(kerned, Is.LessThan(plain), "the box grows by one line kerned, two unkerned");
        }
    }
}
