using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics.Operations.ClippingPaths;
using UglyToad.PdfPig.Graphics.Operations.PathConstruction;
using UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;

namespace ReportTests
{
    /// <summary>
    /// A textbox that may not grow and carries &lt;Custom&gt;&lt;NoWrap&gt;true&lt;/NoWrap&gt;&lt;/Custom&gt;
    /// prints its value on one line, unwrapped, cut at the box's sides. Without it the value is
    /// wrapped to the box's width and the lines below the box's height are dropped, so a long
    /// value loses its last words rather than showing as much of its one line as fits.
    /// </summary>
    [TestFixture]
    public class TextboxNoWrapTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        // The box's left edge on the page: a half-inch margin and the box 1in in.
        private const double BoxLeft = 36 + 72;
        private const double BoxWidth = 60;

        private const string LongValue = "Alpha Bravo Charlie Delta Echo";

        private static async Task<string> Render(string name, string value, bool noWrap = true, bool canGrow = false,
            string align = "Left", bool inTable = false, OutputPresentationType type = OutputPresentationType.PDF)
        {
            string custom = noWrap ? "<Custom><NoWrap>true</NoWrap></Custom>" : "";
            string textbox = $@"<Textbox Name=""T"">
      {(inTable ? "" : $"<Top>0in</Top><Left>1in</Left><Width>{BoxWidth}pt</Width><Height>12pt</Height>")}
      <CanGrow>{(canGrow ? "true" : "false")}</CanGrow>
      <Value>{value}</Value>
      {custom}
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><TextAlign>{align}</TextAlign>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight>
        <PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom></Style>
    </Textbox>";
            string item = !inTable ? textbox : $@"<Table Name=""Tbl"">
      <Top>0in</Top><Left>1in</Left>
      <TableColumns><TableColumn><Width>{BoxWidth}pt</Width></TableColumn></TableColumns>
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

            string output = Path.Combine(outputFolder.LocalPath, name + (type == OutputPresentationType.PDF ? ".pdf" : ".html"));
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, type);
            }
            return output;
        }

        // The page's glyphs grouped into drawn lines by baseline, topmost first.
        private static List<List<Letter>> Lines(string pdfPath)
        {
            using var pdf = PdfDocument.Open(pdfPath);
            return pdf.GetPages().Single().Letters
                .GroupBy(l => System.Math.Round(l.StartBaseLine.Y, 1))
                .OrderByDescending(g => g.Key)
                .Select(g => g.OrderBy(l => l.StartBaseLine.X).ToList())
                .ToList();
        }

        private static string Text(IEnumerable<Letter> letters) =>
            new string(string.Concat(letters.Select(l => l.Value)).Where(char.IsLetter).ToArray());

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

        private static void AssertClippedToBox(string pdf)
        {
            var shows = ClipsAtText(pdf);
            Assert.That(shows, Is.Not.Empty, "the value is drawn");
            foreach (var clips in shows)
            {
                Assert.That(clips.Any(c => System.Math.Abs(c.Left - BoxLeft) < 0.05
                                        && System.Math.Abs(c.Right - (BoxLeft + BoxWidth)) < 0.05), Is.True,
                    "drawn inside a clip to the box's left and right edges; clips in force were: "
                    + string.Join(", ", clips.Select(c => $"{c.Left:F2}..{c.Right:F2}")));
            }
        }

        // The whole value is drawn on the one line, from the box's left edge, and cut at its right.
        [Test]
        public async Task NoWrap_DrawsTheWholeValueOnOneLine()
        {
            string pdf = await Render("NoWrap_left", LongValue);

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.EqualTo(1), "one line; lines drawn were: "
                + string.Join(" | ", lines.Select(Text)));
            Assert.That(Text(lines[0]), Is.EqualTo(LongValue.Replace(" ", "")));
            Assert.That(lines[0].First().StartBaseLine.X, Is.EqualTo(BoxLeft).Within(0.5));
            AssertClippedToBox(pdf);
        }

        // Right aligned, the line ends at the box's right edge and is cut at its left.
        [Test]
        public async Task NoWrap_RightAligned_EndsAtTheRightEdge()
        {
            string pdf = await Render("NoWrap_right", LongValue, align: "Right");

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(Text(lines[0]), Is.EqualTo(LongValue.Replace(" ", "")));
            Assert.That(lines[0].Last().EndBaseLine.X, Is.EqualTo(BoxLeft + BoxWidth).Within(0.5));
            Assert.That(lines[0].First().StartBaseLine.X, Is.LessThan(BoxLeft), "the start runs out past the left edge, to be cut");
            AssertClippedToBox(pdf);
        }

        // The value still breaks at its own line breaks; a box one line high shows the first line.
        [Test]
        public async Task NoWrap_ValueWithALineBreak_ShowsTheFirstLineInAOneLineBox()
        {
            string pdf = await Render("NoWrap_break", "Alpha Bravo Charlie&#10;Delta Echo");

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(Text(lines[0]), Is.EqualTo("AlphaBravoCharlie"));
        }

        // A table cell asked not to wrap is cut at its sides too.
        [Test]
        public async Task NoWrap_TableCell_IsCutAtItsSides()
        {
            string pdf = await Render("NoWrap_cell", LongValue, inTable: true);

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(Text(lines[0]), Is.EqualTo(LongValue.Replace(" ", "")));
            AssertClippedToBox(pdf);
        }

        // Without the opt-in the value wraps and the box keeps its first line, as before.
        [Test]
        public async Task WithoutNoWrap_TheValueWraps()
        {
            string pdf = await Render("NoWrap_off", LongValue, noWrap: false);

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.EqualTo(1));
            Assert.That(Text(lines[0]), Does.Not.Contain("Echo"));
        }

        // A box that may grow wraps whatever it asks.
        [Test]
        public async Task NoWrap_ABoxThatMayGrow_StillWraps()
        {
            string pdf = await Render("NoWrap_cangrow", LongValue, canGrow: true);

            var lines = Lines(pdf);
            Assert.That(lines.Count, Is.GreaterThan(1));
            Assert.That(string.Concat(lines.Select(Text)), Is.EqualTo(LongValue.Replace(" ", "")));
        }

        // Renderers that do not act on it render the box as they always have.
        [Test]
        public async Task NoWrap_OtherRenderers_IgnoreIt()
        {
            string html = await Render("NoWrap_html", LongValue, type: OutputPresentationType.HTML);

            Assert.That(File.ReadAllText(html), Does.Contain(LongValue));
        }
    }
}
