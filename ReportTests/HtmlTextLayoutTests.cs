#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Rich (html) text is laid out the way plain text is: each line aligned by the box's
    /// TextAlign, the lines set LineHeight apart where the style gives one, and the text
    /// inside the box's padding. Every line used to start at the left edge of the box itself,
    /// at the font's measured height apart.
    /// </summary>
    [TestFixture]
    public class HtmlTextLayoutTests
    {
        private static string Rdl(string align, string extraStyle) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>
      <Value>&lt;b&gt;Bold&lt;/b&gt; plain&lt;br/&gt;Second</Value>
      <CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <TextAlign>{align}</TextAlign>{extraStyle}
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private const string NoPadding =
            "<PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>";

        // The runs printed, in order, after the box's own background item.
        private static async Task<PageText[]> Runs(string align, string extraStyle = NoPadding)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(align, extraStyle)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            return html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray();
        }

        private static float Right(PageText t) => t.X + t.W;

        [Test]
        public async Task LeftAligned_EachLineStartsAtTheLeft()
        {
            var runs = await Runs("Left");

            Assert.That(runs.Select(r => r.Text.Trim()), Is.EqualTo(new[] { "Bold", "plain", "Second" }));
            Assert.That(runs[0].X, Is.EqualTo(0f).Within(0.01f));
            Assert.That(runs[2].X, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public async Task RightAligned_EachLineEndsAtTheRight()
        {
            var runs = await Runs("Right");

            Assert.That(Right(runs[1]), Is.EqualTo(400f).Within(0.5f), "the first line, bold and plain together");
            Assert.That(Right(runs[2]), Is.EqualTo(400f).Within(0.5f), "the second line");
            Assert.That(runs[1].X, Is.EqualTo(Right(runs[0])).Within(0.5f), "the runs of a line stay together");
        }

        [Test]
        public async Task Centred_EachLineIsCentred()
        {
            var runs = await Runs("Center");

            Assert.That(runs[0].X + Right(runs[1]), Is.EqualTo(400f).Within(0.5f), "the first line's margins are equal");
            Assert.That(runs[2].X + Right(runs[2]), Is.EqualTo(400f).Within(0.5f), "the second line's margins are equal");
        }

        [Test]
        public async Task ALineHeight_SetsTheLinesApart()
        {
            var measured = await Runs("Left");
            var pitched = await Runs("Left", NoPadding + "<LineHeight>30pt</LineHeight>");

            Assert.That(measured[2].Y - measured[0].Y, Is.LessThan(20f), "10pt text's own height");
            Assert.That(pitched[2].Y - pitched[0].Y, Is.EqualTo(30f).Within(0.01f));
        }

        // A span can underline a run, and set one upright in an italic box.
        [Test]
        public async Task ASpan_UnderlinesAndStraightens()
        {
            const string value = "&lt;span style='text-decoration:underline'&gt;Under&lt;/span&gt; &lt;span style='font-style:normal'&gt;Upright&lt;/span&gt;";
            RdlEngineConfig.RdlEngineConfigInit();
            string rdl = Rdl("Left", NoPadding + "<FontStyle>Italic</FontStyle>")
                .Replace("&lt;b&gt;Bold&lt;/b&gt; plain&lt;br/&gt;Second", value);
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            var runs = html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(runs[0].SI.TextDecoration, Is.EqualTo(TextDecorationEnum.Underline));
                Assert.That(runs[1].SI.FontStyle, Is.EqualTo(FontStyleEnum.Normal));
            });
        }

        // The space between two runs stays with the first: in "Not &lt;u&gt;paid&lt;/u&gt;"
        // the underline starts at the word.
        [Test]
        public async Task ASpaceBeforeAStyleChange_StaysWithTheTextBeforeIt()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            string rdl = Rdl("Left", NoPadding).Replace("&lt;b&gt;Bold&lt;/b&gt; plain&lt;br/&gt;Second",
                "Not &lt;span style='text-decoration:underline'&gt;paid&lt;/span&gt;");
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            var runs = html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Length > 0).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(runs.Select(r => r.Text), Is.EqualTo(new[] { "Not ", "paid" }));
                Assert.That(runs[1].X, Is.GreaterThan(Right(runs[0]) - 0.5f), "the words do not run together");
            });
        }

        // A tab goes to the next half-inch stop, as it does in plain text.
        [Test]
        public async Task ATab_GoesToTheNextStop()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            string rdl = Rdl("Left", NoPadding).Replace("&lt;b&gt;Bold&lt;/b&gt; plain&lt;br/&gt;Second",
                "&lt;b&gt;Date:&lt;/b&gt;&lt;tab/&gt;Today&lt;tab/&gt;&lt;tab/&gt;End");
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            var runs = html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Length > 0).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(runs.Select(r => r.Text), Is.EqualTo(new[] { "Date:", "Today", "End" }));
                Assert.That(runs[1].X, Is.EqualTo(36f).Within(0.01f));
                Assert.That(runs[2].X, Is.EqualTo(108f).Within(0.01f), "two stops on");
            });
        }

        [Test]
        public async Task TheTextSitsInsideThePadding()
        {
            var runs = await Runs("Left",
                "<PaddingLeft>6pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>4pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>");

            Assert.That(runs[0].X, Is.EqualTo(6f).Within(0.01f));
            Assert.That(runs[0].Y, Is.EqualTo(4f).Within(0.01f));
        }
    }
}
#endif
