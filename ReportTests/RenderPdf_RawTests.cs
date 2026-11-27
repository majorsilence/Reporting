using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests.Utils
{
    [TestFixture]
    public class RenderPdf_RawTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;
        private const string Ext = ".pdf";

        [SetUp]
        public void Prepare2Tests()
        {
            _outputFolder ??= GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static readonly object[] TestCases =
        {
            new object[] { "LineObjects.rdl",   "en-US", null },
            new object[] { "WorldFacts.rdl",    "pl-PL", null },
            new object[] { "ChartTypes.rdl",    "en-US", null },
            new object[] { "MatrixExample.rdl", "en-US", null },
            new object[] { "ListReport.rdl",    "en-US", null },
        };

        [Test, TestCaseSource(nameof(TestCases))]
        public async Task RenderPdf_RawDraw(string file2test, string cultureName,
            Func<Dictionary<string, IEnumerable>> fillDatasets)
        {
            GeneralUtils.ChangeCurrentCultrue(cultureName);

            Uri fileRdlUri = new Uri(_reportFolder, file2test);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            rap.Folder = _reportFolder.LocalPath;

            if (fillDatasets != null)
            {
                foreach (var ds in fillDatasets())
                    await rap.DataSets[ds.Key].SetData(ds.Value);
            }
            await rap.RunGetData();

            string outFile = System.IO.Path.Combine(_outputFolder.LocalPath,
                $"{file2test}_{cultureName}_RenderPdf_Raw{Ext}");

            using var sg = new OneFileStreamGen(outFile, true);
            await rap.RunRender(sg, OutputPresentationType.RenderPdf_Majorsilence);

            Assert.That(File.Exists(outFile), "PDF output file was not created");
            var bytes = File.ReadAllBytes(outFile);
            Assert.That(bytes.Length, Is.GreaterThan(100), "PDF output is suspiciously small");
            Assert.That(bytes[0], Is.EqualTo((byte)'%'), "PDF must start with '%'");
            Assert.That(bytes[1], Is.EqualTo((byte)'P'), "PDF must start with '%P'");
            Assert.That(bytes[2], Is.EqualTo((byte)'D'), "PDF must start with '%PD'");
            Assert.That(bytes[3], Is.EqualTo((byte)'F'), "PDF must start with '%PDF'");
        }

        [Test, TestCaseSource(nameof(TestCases))]
        public async Task RenderPdf_Raw_WithPdfA2b_WritesTheConformanceClaim(string file2test, string cultureName,
            Func<Dictionary<string, IEnumerable>> fillDatasets)
        {
            GeneralUtils.ChangeCurrentCultrue(cultureName);

            Uri fileRdlUri = new Uri(_reportFolder, file2test);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            rap.Folder = _reportFolder.LocalPath;
            rap.PdfConformance = Majorsilence.Pdf.PdfConformance.PdfA2b;
            await rap.RunGetData();

            string outFile = System.IO.Path.Combine(_outputFolder.LocalPath,
                $"{file2test}_{cultureName}_RenderPdf_Raw_PdfA2b{Ext}");

            using (var sg = new OneFileStreamGen(outFile, true))
                await rap.RunRender(sg, OutputPresentationType.PDF);

            string pdfText = System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(outFile));
            Assert.That(pdfText, Does.StartWith("%PDF-1.7"), "PDF/A-2b is based on PDF 1.7");
            Assert.That(pdfText, Does.Contain("<pdfaid:part>2</pdfaid:part>"), "the XMP must claim PDF/A-2");
            Assert.That(pdfText, Does.Contain("/OutputIntents"), "PDF/A requires an output intent");
            Assert.That(pdfText, Does.Contain("/CIDToGIDMap /Identity"), "PDF/A requires CIDToGIDMap on TrueType CIDFonts");
        }

        [Test]
        public async Task RenderPdf_Raw_WithPdfA2b_EmbedsEveryFont()
        {
            Assume.That(Directory.Exists(Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory()),
                "the bundled fonts stand in for the standard fonts under PDF/A");

            // Faces that would otherwise fall back to the standard Helvetica font, which is not embedded.
            Report rap = await ReportWithText("No Such Font Family", "Tahoma-Not-Installed");
            rap.PdfConformance = Majorsilence.Pdf.PdfConformance.PdfA2b;
            string pdfText = await RenderToText(rap);

            Assert.That(pdfText, Does.Contain("<pdfaid:part>2</pdfaid:part>"));
            Assert.That(pdfText, Does.Not.Contain("/Subtype /Type1"), "PDF/A requires every font to be embedded");
            Assert.That(pdfText, Does.Contain("/FontFile2"), "the text is drawn in an embedded TrueType font");
        }

        [TestCase("Symbol")]
        [TestCase("ZapfDingbats")]
        public async Task RenderPdf_Raw_WithPdfA2b_SymbolFonts_AreEmbeddedOrRefused(string family)
        {
            // Without PDF/A these are always drawn in the standard Symbol / ZapfDingbats fonts.
            Report rap = await ReportWithText(family);
            rap.PdfConformance = Majorsilence.Pdf.PdfConformance.PdfA2b;

            string pdfText;
            try
            {
                pdfText = await RenderToText(rap);
            }
            catch (InvalidOperationException ex)
            {
                // No installed font of that name: refused rather than written as a false PDF/A claim.
                Assert.That(ex.Message, Does.Contain(family).And.Contain("embedded"));
                return;
            }
            Assert.That(pdfText, Does.Not.Contain("/Subtype /Type1"), "the installed TrueType font is embedded instead");
            Assert.That(pdfText, Does.Contain("/FontFile2"));
        }

        [Test]
        public async Task RenderPdf_Raw_WithoutConformance_StillUsesTheStandardFallback()
        {
            Report rap = await ReportWithText("No Such Font Family");
            string pdfText = await RenderToText(rap);

            // Liberation Sans is the default stand-in when bundled; the Helvetica fallback only
            // shows where it is not. Either way no conformance level means no PDF/A claim.
            Assert.That(pdfText, Does.Not.Contain("pdfaid"));
        }

        [Test]
        public async Task RenderPdf_Raw_PdfAWithSecurity_ThrowsBeforeRendering()
        {
            Report rap = await ReportWithText("Arial");
            rap.PdfConformance = Majorsilence.Pdf.PdfConformance.PdfA2b;

            string outFile = Path.Combine(_outputFolder.LocalPath, "PdfAWithSecurity.pdf");
            using var sg = new OneFileStreamGen(outFile, true);
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => rap.RunRender(sg,
                OutputPresentationType.PDF, Majorsilence.Pdf.Security.PdfSecurity.Protect("owner", "user")));
            Assert.That(ex!.Message, Does.Contain("Report.PdfConformance"));
        }

        private static async Task<Report> ReportWithText(params string[] fontFamilies)
        {
            var boxes = new System.Text.StringBuilder();
            float top = 0.1f;
            foreach (string family in fontFamilies)
            {
                boxes.Append($@"
        <Textbox Name=""T{boxes.Length}"">
          <Top>{top.ToString(System.Globalization.CultureInfo.InvariantCulture)}in</Top><Left>0.1in</Left>
          <Width>3in</Width><Height>0.3in</Height>
          <Value>Text in {family}</Value>
          <Style><FontFamily>{family}</FontFamily></Style>
        </Textbox>");
                top += 0.4f;
            }
            string rdl = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8.5in</Width>
  <Body>
    <Height>3in</Height>
    <ReportItems>{boxes}
    </ReportItems>
  </Body>
</Report>";
            var rap = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(rap.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), "the test report must parse");
            await rap.RunGetData(null);
            return rap;
        }

        private async Task<string> RenderToText(Report rap)
        {
            string outFile = Path.Combine(_outputFolder.LocalPath, $"PdfA_{Guid.NewGuid():N}{Ext}");
            using (var sg = new OneFileStreamGen(outFile, true))
                await rap.RunRender(sg, OutputPresentationType.PDF);
            return System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(outFile));
        }
    }
}
