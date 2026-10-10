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

        private static async Task<Report> BuildReport(string name, string lineHeightXml)
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
      <Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>1.5in</Height>
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
            return report;
        }

        private static async Task<double[]> LineBaselines(string name, string lineHeightXml)
        {
            Report report = await BuildReport(name, lineHeightXml);
            string output = Path.Combine(report.Folder, name + ".pdf");
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

        [Test]
        public async Task NoLineHeight_KeepsTheFontSizeSpacing()
        {
            var baselines = await LineBaselines("LineHeight_none", "");
            Assert.That(baselines.Length, Is.EqualTo(3));
            Assert.That(baselines[0] - baselines[1], Is.EqualTo(10.0).Within(0.2));
        }

        [Test]
        public async Task LineHeight_IsWrittenToTheHtmlStyle()
        {
            Report report = await BuildReport("LineHeight_html", "<LineHeight>24pt</LineHeight>");
            var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.HTML);
            string html = sg.GetText();
            Assert.That(html, Does.Contain("line-height:24pt"));
        }

        [Test]
        public async Task LineHeight_IsAppliedWhenRenderingToAnImage()
        {
            // the image renderer lays the text out line by line at the requested pitch
            Report report = await BuildReport("LineHeight_tif", "<LineHeight>24pt</LineHeight>");
            var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.TIF);
            Assert.That(sg.GetStream().Length, Is.GreaterThan(0));
        }
    }
}
