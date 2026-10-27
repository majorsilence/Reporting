using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics.Operations.ClippingPaths;
using UglyToad.PdfPig.Graphics.Operations.PathConstruction;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;

namespace ReportTests
{
    /// <summary>
    /// A line of text wider than a textbox that may not grow is cut at the box's sides in a PDF.
    /// The renderer drew it whole, running out of the box over its neighbour: a right-aligned
    /// amount too wide for its column ran leftwards into the column before it.
    /// </summary>
    [TestFixture]
    public class TextClippedToBoxTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        // The box's left edge on the page: a half-inch margin and the box 1in in.
        private const double BoxLeft = 36 + 72;

        private static async Task<string> Render(string name, string value, string width, bool canGrow,
            string align = "Right", string padLeft = "0pt", string padRight = "0pt", bool inTable = false)
        {
            string textbox = $@"<Textbox Name=""T"">
      {(inTable ? "" : $"<Top>0in</Top><Left>1in</Left><Width>{width}</Width><Height>12pt</Height>")}
      <CanGrow>{(canGrow ? "true" : "false")}</CanGrow>
      <Value>{value}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><TextAlign>{align}</TextAlign>
        <PaddingLeft>{padLeft}</PaddingLeft><PaddingRight>{padRight}</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>";
            string item = !inTable ? textbox : $@"<Table Name=""Tbl"">
      <Top>0in</Top><Left>1in</Left>
      <TableColumns><TableColumn><Width>{width}</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>12pt</Height><TableCells><TableCell><ReportItems>
        {textbox}
      </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
    </Table>";
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            string rdlPath = Path.Combine(outputFolder.LocalPath, name + ".rdl");
            File.WriteAllText(rdlPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
  <Width>7.5in</Width>
  <Body><Height>1in</Height><ReportItems>
    {item}
  </ReportItems></Body>
</Report>");

            Report report = await new RDLParser(File.ReadAllText(rdlPath)) { SkipDatabaseSchemaValidation = true }.Parse();
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(outputFolder.LocalPath, name + ".pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }
            return output;
        }

        // The horizontal extent of every clip rectangle in force where the page shows text,
        // one list per text-showing operator.
        private static List<List<(double Left, double Right)>> ClipsAtText(string pdfPath)
        {
            using var pdf = PdfDocument.Open(pdfPath);
            var result = new List<List<(double, double)>>();
            var clips = new List<(double, double)>();
            var saved = new Stack<List<(double, double)>>();
            AppendRectangle pending = null;
            foreach (IGraphicsStateOperation op in pdf.GetPages().Single().Operations)
            {
                switch (op)
                {
                    case Push: saved.Push(new List<(double, double)>(clips)); break;
                    case Pop: clips = saved.Pop(); break;
                    case AppendRectangle re: pending = re; break;
                    case ModifyClippingByNonZeroWindingIntersect:
                        if (pending != null)
                            clips.Add((pending.LowerLeftX, pending.LowerLeftX + pending.Width));
                        break;
                    case ShowText:
                    case ShowTextsWithPositioning:
                        result.Add(new List<(double, double)>(clips));
                        break;
                }
            }
            return result;
        }

        [Test]
        public async Task RightAlignedAmountWiderThanItsBox_IsCutAtTheBoxSides()
        {
            string pdf = await Render("Clip_right_amount", "$2,939.85", "30pt", canGrow: false);

            var shows = ClipsAtText(pdf);
            Assert.That(shows, Is.Not.Empty, "the amount is drawn");
            foreach (var clips in shows)
            {
                Assert.That(clips.Any(c => System.Math.Abs(c.Left - BoxLeft) < 0.05
                                        && System.Math.Abs(c.Right - (BoxLeft + 30)) < 0.05), Is.True,
                    "drawn inside a clip to the box's left and right edges; clips in force were: "
                    + string.Join(", ", clips.Select(c => $"{c.Left:F2}..{c.Right:F2}")));
            }
        }

        // The text is laid out inside the padding, and is cut there.
        [Test]
        public async Task OverflowingText_IsCutAtThePaddedEdges()
        {
            string pdf = await Render("Clip_padded", "Unbreakable_long_reference", "40pt", canGrow: false,
                align: "Left", padLeft: "2pt", padRight: "4pt");

            var shows = ClipsAtText(pdf);
            Assert.That(shows, Is.Not.Empty);
            foreach (var clips in shows)
            {
                Assert.That(clips.Any(c => System.Math.Abs(c.Left - (BoxLeft + 2)) < 0.05
                                        && System.Math.Abs(c.Right - (BoxLeft + 40 - 4)) < 0.05), Is.True,
                    "clips in force were: " + string.Join(", ", clips.Select(c => $"{c.Left:F2}..{c.Right:F2}")));
            }
        }

        // Text that fits is drawn as it always was, with no clip around it.
        [Test]
        public async Task TextThatFits_IsNotClipped()
        {
            string pdf = await Render("Clip_fits", "$9.85", "60pt", canGrow: false);

            Assert.That(ClipsAtText(pdf).All(c => c.Count == 0), Is.True);
        }

        // A box that may grow is the report's to wrap; what it cannot wrap still shows whole.
        [Test]
        public async Task ABoxThatMayGrow_IsNotClipped()
        {
            string pdf = await Render("Clip_cangrow", "$2,939.85", "30pt", canGrow: true);

            Assert.That(ClipsAtText(pdf).All(c => c.Count == 0), Is.True);
        }

        // A table cell is as wide as its column, not as its text, and its text runs on over the
        // cells beside it as before: cut to a narrow column, it would print nothing.
        [Test]
        public async Task ATableCell_IsNotClipped()
        {
            string pdf = await Render("Clip_tablecell", "$2,939.85", "30pt", canGrow: false, inTable: true);

            var shows = ClipsAtText(pdf);
            Assert.That(shows, Is.Not.Empty, "the amount is drawn");
            Assert.That(shows.All(c => c.Count == 0), Is.True);
        }
    }
}
