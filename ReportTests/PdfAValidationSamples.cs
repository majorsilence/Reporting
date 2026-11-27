using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    // Writes PDF/A files for an independent validator (veraPDF, in CI: see .github/scripts/validate_pdfa.py).
    // Our own tests can only look for the entries we know PDF/A needs; veraPDF checks the whole standard.
    //
    // Runs only when MAJORSILENCE_PDFA_VALIDATION_DIR names the output folder; otherwise it is skipped.
    [TestFixture]
    [Category("PdfAValidationSamples")]
    public class PdfAValidationSamples
    {
        private const string OutputVariable = "MAJORSILENCE_PDFA_VALIDATION_DIR";

        private string _dir = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            var dir = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(dir))
                Assert.Ignore($"Set {OutputVariable} to write PDF/A validation samples.");
            _dir = Directory.CreateDirectory(dir!).FullName;
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static readonly object[] Reports =
        {
            new object[] { "LineObjects.rdl",   PdfConformance.PdfA2b },
            new object[] { "WorldFacts.rdl",    PdfConformance.PdfA2b },
            new object[] { "ChartTypes.rdl",    PdfConformance.PdfA2b },
            new object[] { "MatrixExample.rdl", PdfConformance.PdfA2b },
            new object[] { "ListReport.rdl",    PdfConformance.PdfA2b },
            new object[] { "ImageSizing.rdl",   PdfConformance.PdfA2b },
            new object[] { "LineObjects.rdl",   PdfConformance.PdfA1b },
            new object[] { "ChartTypes.rdl",    PdfConformance.PdfA1b },
            new object[] { "ListReport.rdl",    PdfConformance.PdfA1b },
            new object[] { "ImageSizing.rdl",   PdfConformance.PdfA1b },
        };

        [TestCaseSource(nameof(Reports))]
        public async Task Report(string file, PdfConformance conformance)
        {
            GeneralUtils.ChangeCurrentCultrue("en-US");
            Uri reportFolder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(new Uri(reportFolder, file));
            rap.Folder = reportFolder.LocalPath;
            rap.PdfConformance = conformance;
            await rap.RunGetData();

            string outFile = Path.Combine(_dir, $"report-{Path.GetFileNameWithoutExtension(file)}-{conformance}.pdf");
            using (var sg = new OneFileStreamGen(outFile, true))
                await rap.RunRender(sg, OutputPresentationType.PDF);
            Assert.That(File.Exists(outFile));
        }

        private static FontRegistry BundledFonts()
        {
            string fontsDir = Majorsilence.Forms.Drawing.FontResourceLoader.GetFontDirectory();
            Assume.That(Directory.Exists(fontsDir));
            return new FontRegistry().AddDirectory(fontsDir);
        }

        // Text in an embedded subset font, a translucent fill and stroke, an image with alpha, a link and a tooltip.
        private static void DrawEverything(PdfCanvas c)
        {
            var rgba = new byte[16 * 16 * 4];
            for (int i = 0; i < 16 * 16; i++)
            {
                rgba[i * 4] = 200; rgba[i * 4 + 1] = 30; rgba[i * 4 + 2] = 30;
                rgba[i * 4 + 3] = (byte)(i * 255 / (16 * 16 - 1));
            }
            var text = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);
            c.DrawText("PDF/A validation sample: été, ß, €", 72, 72, text)
             .DrawText("Bold", 72, 96, text.WithBold(true))
             .DrawRectangle(72, 120, 100, 50, ShapeStyle.Filled(PdfColor.FromRgb(0, 0, 255)).WithFillOpacity(0.3f))
             .DrawLine(72, 190, 300, 190, StrokeStyle.Default.WithOpacity(0.5f))
             .DrawImage(PdfImage.FromRgba(rgba, 16, 16), 72, 210, 48, 48)
             .AddLink(72, 280, 100, 20, "https://example.com")
             .AddTooltip(200, 280, 100, 20, "A viewer pop-up");
        }

        [TestCase(PdfConformance.PdfA1b)]
        [TestCase(PdfConformance.PdfA2b)]
        [TestCase(PdfConformance.PdfA3b)]
        public void Library(PdfConformance conformance)
        {
            var bytes = PdfDocument.Create()
                .WithConformance(conformance)
                .WithTitle("PDF/A sample")
                .WithFontRegistry(BundledFonts())
                .AddPage(PageSizes.A4, DrawEverything)
                .ToBytes();
            File.WriteAllBytes(Path.Combine(_dir, $"library-{conformance}.pdf"), bytes);
        }

        [TestCase(PdfConformance.PdfA1b)]
        [TestCase(PdfConformance.PdfA2b)]
        public void LibraryWithVisibleSignature(PdfConformance conformance)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=PDF/A Sample", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
#pragma warning disable SYSLIB0057
            using var signer = new X509Certificate2(cert.Export(X509ContentType.Pfx), (string?)null, X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057

            var bytes = PdfDocument.Create()
                .WithConformance(conformance)
                .WithFontRegistry(BundledFonts())
                .WithSignature(new PdfSignatureOptions(signer).WithSignerName("Alice").WithAppearance(72, 600, 180, 40))
                .AddPage(PageSizes.A4, c => c.DrawText("Signed PDF/A sample", 72, 72,
                    TextStyle.Default.WithFamily("LiberationSans").WithSize(12)))
                .ToBytes();
            File.WriteAllBytes(Path.Combine(_dir, $"library-signed-{conformance}.pdf"), bytes);
        }
    }
}
