#if NET8_0_OR_GREATER
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// Rich (html) text sits inside its box's padding once, where plain text in the same box
    /// sits. Each run was placed inside the padding and then kept the box's padding as its own,
    /// which every renderer added again: the text printed one padding too low and too far right.
    /// </summary>
    [TestFixture]
    public class HtmlTextPaddingTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Padding =
            "<PaddingLeft>12pt</PaddingLeft><PaddingRight>2pt</PaddingRight><PaddingTop>10pt</PaddingTop><PaddingBottom>2pt</PaddingBottom>";

        // Two boxes alike but for the format: html at the left, plain text 200pt to its right.
        private static string Rdl() => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>500pt</PageWidth><PageHeight>400pt</PageHeight>
  <LeftMargin>0pt</LeftMargin><RightMargin>0pt</RightMargin>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <Width>500pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>20pt</Top><Left>20pt</Left><Width>150pt</Width><Height>40pt</Height>
      <Value>&lt;b&gt;Html&lt;/b&gt; text</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>{Padding}</Style>
    </Textbox>
    <Textbox Name=""Plain"">
      <Top>20pt</Top><Left>220pt</Left><Width>150pt</Width><Height>40pt</Height>
      <Value>Plain text</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>{Padding}</Style>
    </Textbox>
  </ReportItems></Body>
</Report>";

        [Test]
        public async Task HtmlText_PrintsWherePlainTextInTheSameBoxPrints()
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);
            string output = Path.Combine(outputFolder.LocalPath, "HtmlTextPadding.pdf");

            using (var report = await new RDLParser(Rdl()) { SkipDatabaseSchemaValidation = true }.Parse())
            {
                report.Folder = outputFolder.LocalPath;
                await report.RunGetData(null);
                using var sg = new OneFileStreamGen(output, true);
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var letters = pdf.GetPages().Single().Letters;
            var html = letters.First(l => l.Value == "H");
            var plain = letters.First(l => l.Value == "P");

            Assert.Multiple(() =>
            {
                Assert.That(html.StartBaseLine.X, Is.EqualTo(plain.StartBaseLine.X - 200).Within(0.5), "the same distance from the box's left");
                Assert.That(html.StartBaseLine.Y, Is.EqualTo(plain.StartBaseLine.Y).Within(0.5), "on the same baseline");
            });
        }

        // The runs are placed inside the padding, and carry none of their own for a renderer to add.
        [Test]
        public async Task TheRuns_CarryNoPaddingOfTheirOwn()
        {
            using var report = await new RDLParser(Rdl()) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var box = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await box.Build(pages.G);
            var runs = box.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray();

            Assert.That(runs, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(runs[0].X, Is.EqualTo(box.X + 12f).Within(0.01f));
                Assert.That(runs[0].Y, Is.EqualTo(box.Y + 10f).Within(0.01f));
                foreach (var run in runs)
                    Assert.That(new[] { run.SI.PaddingLeft, run.SI.PaddingTop, run.SI.PaddingRight, run.SI.PaddingBottom },
                        Is.All.EqualTo(0f), run.Text);
            });
        }
    }
}
#endif
