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
    /// Justified text in a PDF: every line of a paragraph but its last is spread to the full
    /// width of the box, as the screen renderer already draws it. It printed flush left.
    /// </summary>
    [TestFixture]
    public class JustifiedTextTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Paragraph =
            "The quick brown fox jumps over the lazy dog and keeps on running far across the wide field until evening";

        // Each printed line's left and right edges, top line first.
        private static async Task<(double Left, double Right)[]> LineEdges(string name, string align)
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
      <Top>0in</Top><Left>0in</Left><Width>2.5in</Width><Height>1.5in</Height>
      <CanGrow>true</CanGrow>
      <Value>{Paragraph}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><TextAlign>{align}</TextAlign>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>
  </ReportItems></Body>
</Report>");

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
                .GroupBy(l => System.Math.Round(l.StartBaseLine.Y, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => (g.Min(l => l.StartBaseLine.X), g.Max(l => l.EndBaseLine.X)))
                .ToArray();
        }

        [Test]
        public async Task JustifiedLines_ReachTheRightEdge_ButTheLastDoesNot()
        {
            var lines = await LineEdges("Justified_paragraph", "Justified");
            double left = 36, right = 36 + 180;     // half-inch margin, a box 2.5in wide

            Assert.That(lines.Length, Is.GreaterThan(2), "the paragraph wraps");
            Assert.Multiple(() =>
            {
                foreach (var (l, r) in lines.Take(lines.Length - 1))
                {
                    Assert.That(l, Is.EqualTo(left).Within(0.5));
                    Assert.That(r, Is.EqualTo(right).Within(1.0), "spread to the right edge");
                }
                Assert.That(lines.Last().Right, Is.LessThan(right - 5), "the last line stays ragged");
            });
        }

        [Test]
        public async Task LeftAlignedLines_StayRagged()
        {
            var lines = await LineEdges("Left_paragraph", "Left");

            Assert.That(lines.Take(lines.Length - 1).Any(e => e.Right < 36 + 180 - 0.5), Is.True);
        }
    }
}
