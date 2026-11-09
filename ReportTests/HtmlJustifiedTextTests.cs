#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A justified paragraph of rich (html) text has each line but its last spread to the full
    /// width, as plain justified text has, and still breaks its lines WrapInset short of the
    /// right. Its lines used to be set at the left, ragged.
    /// </summary>
    [TestFixture]
    public class HtmlJustifiedTextTests
    {
        private static string Rdl(string value, string custom) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>10pt</Left><Width>300pt</Width><Height>200pt</Height>
      <Value>{System.Security.SecurityElement.Escape(value)}</Value>
      <CanGrow>false</CanGrow>{custom}
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <TextAlign>Justified</TextAlign><LineHeight>12pt</LineHeight>
        <PaddingLeft>5pt</PaddingLeft><PaddingRight>5pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        // The pieces of text printed, after the box's own background item, grouped by line.
        private static async Task<PageText[][]> Lines(string value, string custom = "")
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(value, custom)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            return html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0)
                .GroupBy(t => (int) System.Math.Round(t.Y)).OrderBy(g => g.Key)
                .Select(g => g.OrderBy(t => t.X).ToArray()).ToArray();
        }

        private static float Right(PageText t) => t.X + t.W;

        private static readonly string Words = string.Join(" ", Enumerable.Repeat("justified words", 30));

        [Test]
        public async Task EachLineButAParagraphsLast_ReachesTheRightEdge()
        {
            string first = string.Join(" ", Enumerable.Repeat("alpha words", 27));
            var lines = await Lines($"{first}<br>{Words}");

            Assert.That(lines.Length, Is.GreaterThan(4));
            int lastOfFirst = System.Array.FindLastIndex(lines, l => l.Any(t => t.Text.Contains("alpha")));
            Assert.Multiple(() =>
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    Assert.That(lines[i][0].X, Is.EqualTo(15f).Within(0.01f), $"line {i} starts at the left");
                    if (i == lastOfFirst || i == lines.Length - 1)
                        Assert.That(Right(lines[i].Last()), Is.LessThan(290f), $"line {i} ends its paragraph and is not spread");
                    else
                        Assert.That(Right(lines[i].Last()), Is.EqualTo(305f).Within(1f), $"line {i} is spread to the right edge");
                }
                Assert.That(lastOfFirst, Is.GreaterThan(0), "the first paragraph's last line is found");
            });
        }

        // The words of a spread line are set evenly apart, the runs of other styles in it too.
        // A word's piece holds the spaces beside it, so each gap is the room shared out.
        [Test]
        public async Task TheWordsOfASpreadLine_AreEvenlyApart()
        {
            var lines = await Lines("<b>Bold</b> words and <i>more words</i> " + Words);
            var first = lines[0];
            var gaps = first.Zip(first.Skip(1), (a, b) => b.X - Right(a)).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(first[0].Text.Trim(), Is.EqualTo("Bold"));
                Assert.That(first[0].SI.FontWeight, Is.EqualTo(FontWeightEnum.Bold));
                Assert.That(first.First(t => t.Text.Trim() == "more").SI.FontStyle, Is.EqualTo(FontStyleEnum.Italic));
                Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(1f), "the gaps are the same");
                Assert.That(gaps.Min(), Is.GreaterThan(1f), "and the line is spread");
            });
        }

        // The lines break WrapInset short of the right, and are spread to the full width.
        [Test]
        public async Task LinesBreakShortOfTheWrapInset_AndAreSpreadToTheFullWidth()
        {
            var plain = await Lines(Words);
            var inset = await Lines(Words, "<Custom><WrapInset>60pt</WrapInset></Custom>");

            Assert.Multiple(() =>
            {
                Assert.That(inset.Length, Is.GreaterThan(plain.Length), "broken shorter");
                Assert.That(Right(inset[0].Last()), Is.EqualTo(305f).Within(1f), "spread to the full width");
            });
        }
    }
}
#endif
