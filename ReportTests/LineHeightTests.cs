using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;

namespace ReportTests
{
    [TestFixture]
    public class LineHeightTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<double[]> LineBaselines(string name, string lineHeightXml, string boxHeight = "1.5in")
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            string rdlPath = Path.Combine(outputFolder.LocalPath, name + ".rdl");
            File.WriteAllText(rdlPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
  <Width>7.5in</Width>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name=""T"">
      <Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>{boxHeight}</Height>
      <CanGrow>false</CanGrow>
      <Value>First{{0}}Second{{0}}Third</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>{lineHeightXml}
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>
  </ReportItems></Body>
</Report>".Replace("{0}", "&#10;"));

            Report report = await RdlUtils.GetReport(new System.Uri(rdlPath));
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(outputFolder.LocalPath, name + ".pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            return pdf.GetPages().Single().Letters
                .Where(l => l.Value.Trim().Length > 0)
                .Select(l => System.Math.Round(l.StartBaseLine.Y, 1))
                .Distinct().OrderByDescending(y => y).ToArray();
        }

        [Test]
        public async Task LineHeight_SetsThePdfBaselineSpacing()
        {
            var baselines = await LineBaselines("LineHeight_24pt", "<LineHeight>24pt</LineHeight>");
            Assert.That(baselines.Length, Is.EqualTo(3));
            Assert.That(baselines[0] - baselines[1], Is.EqualTo(24.0).Within(0.2));
            Assert.That(baselines[1] - baselines[2], Is.EqualTo(24.0).Within(0.2));
        }

        // A box that may not grow keeps every line whose letters fit in it: the last needs an
        // em, not a whole pitch. Three lines of 14.25pt in 42pt fit, the third's letters
        // ending at 38.5pt, and the third was dropped for want of a full 42.75pt.
        [Test]
        public async Task LineHeight_KeepsALastLineWhoseLettersFit()
        {
            var baselines = await LineBaselines("LineHeight_lastline", "<LineHeight>14.25pt</LineHeight>", "42pt");
            Assert.That(baselines.Length, Is.EqualTo(3));
        }

        [Test]
        public async Task LineHeight_StillClipsALineThatDoesNotFit()
        {
            var baselines = await LineBaselines("LineHeight_clipped", "<LineHeight>14.25pt</LineHeight>", "36pt");
            Assert.That(baselines.Length, Is.EqualTo(2));
        }

        // With no explicit LineHeight, the pitch comes from Arial's own hhea metrics
        // (ascent + descent + lineGap scaled to 10pt), not the bare 10pt font size -- that bare
        // font size produced text visibly more compressed than native GDI rendering. See issue #491.
        [Test]
        public async Task NoLineHeight_UsesTheFontsOwnMetrics()
        {
            var baselines = await LineBaselines("LineHeight_none", "");
            Assert.That(baselines.Length, Is.EqualTo(3));
            Assert.That(baselines[0] - baselines[1], Is.EqualTo(11.5).Within(0.2));
        }
    }
}
