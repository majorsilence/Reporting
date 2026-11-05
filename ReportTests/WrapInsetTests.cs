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
    /// A textbox's WrapInset breaks its lines that much short of the right edge they are drawn
    /// against, as Crystal Reports breaks lines measured in a slightly wider font than it draws:
    /// a justified paragraph's lines break short and are still spread to the full width. Padding
    /// on the right broke them in the same places, and stopped the justified lines short too.
    /// </summary>
    [TestFixture]
    public class WrapInsetTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Paragraph =
            "The quick brown fox jumps over the lazy dog and keeps on running far across the wide field until evening";

        // Each printed line's text, left and right edges, top line first, for a 2.5in box at the
        // page's half-inch margin.
        private static async Task<(string Text, double Left, double Right)[]> Lines(string name, string align, string custom)
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
      <CanGrow>true</CanGrow>{custom}
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
                .Select(g => (string.Concat(g.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value)),
                    g.Min(l => l.StartBaseLine.X), g.Max(l => l.EndBaseLine.X)))
                .ToArray();
        }

        private const string Inset = "<Custom><WrapInset>40pt</WrapInset></Custom>";

        [Test]
        public async Task JustifiedLines_BreakShortByTheInset_AndStillReachTheRightEdge()
        {
            var plain = await Lines("WrapInset_justified_none", "Justified", "");
            var inset = await Lines("WrapInset_justified_40pt", "Justified", Inset);
            double left = 36, right = 36 + 180;     // half-inch margin, a box 2.5in wide

            Assert.That(inset.Length, Is.GreaterThan(plain.Length), "the lines break 40pt shorter, so there are more of them");
            Assert.Multiple(() =>
            {
                foreach (var (_, l, r) in inset.Take(inset.Length - 1))
                {
                    Assert.That(l, Is.EqualTo(left).Within(0.5));
                    Assert.That(r, Is.EqualTo(right).Within(1.0), "spread to the box's full width");
                }
            });
        }

        [Test]
        public async Task LeftAlignedLines_StopShortOfTheInset()
        {
            var inset = await Lines("WrapInset_left_40pt", "Left", Inset);
            double left = 36, breakAt = 36 + 180 - 40;

            Assert.That(inset.Length, Is.GreaterThan(2), "the paragraph wraps");
            Assert.Multiple(() =>
            {
                foreach (var (_, l, r) in inset)
                {
                    Assert.That(l, Is.EqualTo(left).Within(0.5));
                    Assert.That(r, Is.LessThanOrEqualTo(breakAt + 0.5));
                }
            });
        }

        // A growing box grows by the lines it draws, broken short by the inset: the item below
        // it starts below its last line. Measured without the inset it grew by fewer lines and
        // the item below printed over its last.
        [Test]
        public async Task AGrowingBox_GrowsByTheLinesItBreaksIntoWithTheInset()
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);
            string rdlPath = Path.Combine(outputFolder.LocalPath, "WrapInset_grows.rdl");
            File.WriteAllText(rdlPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
  <Width>7.5in</Width>
  <Body><Height>2in</Height><ReportItems>
    <Textbox Name=""T"">
      <Top>0pt</Top><Left>0in</Left><Width>2.5in</Width><Height>12pt</Height>
      <CanGrow>true</CanGrow>{Inset}
      <Value>{Paragraph}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><TextAlign>Justified</TextAlign>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>
    <Textbox Name=""Below"">
      <Top>14pt</Top><Left>0in</Left><Width>2.5in</Width><Height>12pt</Height>
      <Value>BELOW</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>
  </ReportItems></Body>
</Report>");

            Report report = await RdlUtils.GetReport(new System.Uri(rdlPath));
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);
            string output = Path.Combine(outputFolder.LocalPath, "WrapInset_grows.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            var baselines = pdf.GetPages().Single().Letters
                .Where(l => l.Value.Trim().Length > 0)
                .GroupBy(l => System.Math.Round(l.StartBaseLine.Y, 0))
                .OrderByDescending(g => g.Key)
                .Select(g => (Y: g.Key, Text: string.Concat(g.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value))))
                .ToList();
            int below = baselines.FindIndex(b => b.Text == "BELOW");
            string seen = string.Join(" | ", baselines.Select(b => $"{b.Y}:{b.Text}"));

            // BELOW sits 2pt under the box, so a line of the paragraph (about 11.5pt) that the
            // box did not grow by puts it over that line rather than a line further down.
            Assert.Multiple(() =>
            {
                Assert.That(below, Is.EqualTo(baselines.Count - 1), seen);
                Assert.That(baselines[below - 1].Y - baselines[below].Y, Is.GreaterThan(10), seen);
            });
        }

        // A size that does not parse, or is not positive, is no inset at all.
        [TestCase("<Custom><WrapInset>0pt</WrapInset></Custom>")]
        [TestCase("<Custom><WrapInset>-20pt</WrapInset></Custom>")]
        [TestCase("<Custom><WrapInset>wide</WrapInset></Custom>")]
        public async Task AnInsetThatIsNotAPositiveSize_ChangesNothing(string custom)
        {
            var plain = await Lines("WrapInset_left_none", "Left", "");
            var other = await Lines("WrapInset_left_other", "Left", custom);

            Assert.That(other.Select(l => l.Text), Is.EqualTo(plain.Select(l => l.Text)));
        }
    }
}
