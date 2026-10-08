#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// Which installed font a PDF draws a family in (#348). On Linux and macOS only a few
    /// folders were registered, by file name, so a font installed anywhere else or named any
    /// other way was never found: Microsoft's core fonts install as
    /// /usr/share/fonts/truetype/msttcorefonts/Arial.ttf, and a report in Arial got the
    /// fallback even where Arial was installed. And Times New Roman and Courier New were drawn
    /// in Liberation Serif and Mono whenever those were present, even with the real font
    /// installed. Each test assumes the font it is about is installed.
    /// </summary>
    [TestFixture]
    public class InstalledFontTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;

        [SetUp]
        public void SetUp()
        {
            _outputFolder = GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // An installed font file whose name contains one of the given stems, on Windows in its
        // font folder and elsewhere anywhere below the usual font roots.
        private static bool Installed(params string[] stems)
        {
            var roots = OperatingSystem.IsWindows()
                ? new[] { Environment.GetFolderPath(Environment.SpecialFolder.Fonts) }
                : new[] { "/usr/share/fonts", "/usr/local/share/fonts", "/System/Library/Fonts", "/Library/Fonts" };
            foreach (var root in roots.Where(Directory.Exists))
            {
                var files = Directory.EnumerateFiles(root, "*.ttf", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true });
                if (files.Any(f => stems.Any(s => Path.GetFileNameWithoutExtension(f).Equals(s, StringComparison.OrdinalIgnoreCase))))
                    return true;
            }
            return false;
        }

        // The font the PDF draws the given text in.
        private async Task<string> FontOf(string text)
        {
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, "InstalledFontTest.rdl"));
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(null);
            string output = Path.Combine(_outputFolder.LocalPath, "InstalledFontTest.pdf");
            using (var sg = new OneFileStreamGen(output, true))
                await report.RunRender(sg, OutputPresentationType.PDF);

            using var pdf = PdfDocument.Open(output);
            var letters = pdf.GetPages().Single().Letters.ToList();
            string drawn = string.Concat(letters.Select(l => l.Value));
            int at = drawn.IndexOf(text, StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), $"'{text}' is drawn; the page drew '{drawn}'");
            return letters[at].FontName;
        }

        [Test]
        public async Task ATextboxInArial_IsDrawnInTheInstalledArial()
        {
            Assume.That(Installed("arial", "Arial"), Is.True, "Arial is not installed here");

            Assert.That(await FontOf("ARIALTEXT"), Does.Contain("Arial"));
        }

        [Test]
        public async Task ATextboxInTimesNewRoman_IsDrawnInTheInstalledTimes_NotLiberationSerif()
        {
            Assume.That(Installed("times", "Times_New_Roman", "Times New Roman"), Is.True, "Times New Roman is not installed here");

            Assert.That(await FontOf("TIMESTEXT"), Does.Contain("TimesNewRoman"));
        }
    }
}
#endif
