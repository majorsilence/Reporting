using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReportTests
{
    [TestFixture]
    public class WritingModeRotate180Tests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        [Test]
        public void GetWritingMode_ParsesRlBt()
        {
            Assert.That(StyleInfo.GetWritingMode("rl-bt", WritingModeEnum.lr_tb), Is.EqualTo(WritingModeEnum.rl_bt));
            Assert.That(StyleInfo.GetWritingMode("lr-tb", WritingModeEnum.rl_bt), Is.EqualTo(WritingModeEnum.lr_tb));
        }

        /// <summary>
        /// rl-bt turns the horizontal layout half a revolution about the middle of the box:
        /// the glyphs come out rotated 180 degrees, and a left-aligned, top-aligned run
        /// ends up against the box's right edge, in its lower half.
        /// </summary>
        [Test]
        public async Task RlBt_PdfTextIsUpsideDownInTheOppositeCorner()
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            var reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            Report report = await RdlUtils.GetReport(new System.Uri(reportFolder, "WritingModeRotate180Test.rdl"));
            report.Folder = reportFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(outputFolder.LocalPath, "WritingModeRotate180Test.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var letters = pdf.GetPages().Single().Letters.Where(l => l.Value.Trim().Length > 0).ToList();

            Assert.That(letters, Is.Not.Empty);
            Assert.That(letters.All(l => l.TextOrientation == TextOrientation.Rotate180), Is.True);

            // box: x 36+72=108 .. 108+216=324 pt; y (from page top) 36+36=72 .. 108 pt
            double left = letters.Min(l => System.Math.Min(l.StartBaseLine.X, l.EndBaseLine.X));
            double right = letters.Max(l => System.Math.Max(l.StartBaseLine.X, l.EndBaseLine.X));
            Assert.That(right, Is.InRange(318.0, 324.5), "the run starts at the box's right edge");
            Assert.That(left, Is.GreaterThan(324.0 - 60), "and runs back only as far as the text is long");

            double pageHeight = pdf.GetPage(1).Height;
            double baselineFromTop = pageHeight - letters[0].StartBaseLine.Y;
            Assert.That(baselineFromTop, Is.GreaterThan(90).And.LessThanOrEqualTo(108.5),
                "top-aligned text lands against the bottom of the box once turned over");
        }

        /// <summary>
        /// The vertical modes read the same way in PDF as in the other renderers, and stay
        /// inside their box: tb-rl reads downward, tops of the letters to the right, against
        /// the box's right edge; tb-lr reads upward, tops to the left, from the bottom-left.
        /// </summary>
        [TestCase("tb-rl", TextOrientation.Rotate90)]
        [TestCase("tb-lr", TextOrientation.Rotate270)]
        public async Task VerticalModes_ArePlacedInsideTheirBox(string mode, TextOrientation expected)
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            var reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            string rdlPath = Path.Combine(outputFolder.LocalPath, $"WritingMode_{mode}.rdl");
            File.WriteAllText(rdlPath,
                File.ReadAllText(new System.Uri(reportFolder, "WritingModeRotate180Test.rdl").LocalPath)
                    .Replace("rl-bt", mode)
                    .Replace("<Width>3in</Width>", "<Width>1in</Width>")
                    .Replace("<Height>0.5in</Height>", "<Height>2in</Height>"));

            Report report = await RdlUtils.GetReport(new System.Uri(rdlPath));
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(outputFolder.LocalPath, $"WritingMode_{mode}.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var page = pdf.GetPages().Single();
            var letters = page.Letters.Where(l => l.Value.Trim().Length > 0).ToList();
            Assert.That(letters, Is.Not.Empty);
            Assert.That(letters.All(l => l.TextOrientation == expected), Is.True, mode);

            // box: x 108..180 pt; from the page top y 72..216 pt
            double top = page.Height - letters.Max(l => System.Math.Max(l.StartBaseLine.Y, l.EndBaseLine.Y));
            double bottom = page.Height - letters.Min(l => System.Math.Min(l.StartBaseLine.Y, l.EndBaseLine.Y));
            double bx = letters.Average(l => l.StartBaseLine.X);
            Assert.That(top, Is.GreaterThanOrEqualTo(71.5), "the run stays below the top of its box");
            Assert.That(bottom, Is.LessThanOrEqualTo(216.5), "and above its bottom");
            Assert.That(bx, Is.InRange(108.0, 180.0), "and between its sides");
            if (mode == "tb-rl")
                Assert.That(top, Is.LessThan(80), "tb-rl starts at the top of the box");
            else
                Assert.That(bottom, Is.GreaterThan(208), "tb-lr starts at the bottom of the box");
        }
    }
}
