#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Each line of rich (html) text is aligned by the paragraph it is in: text-align or align on
    /// a &lt;p&gt; or &lt;div&gt;, and the box's TextAlign outside them. Every line used to take
    /// the box's one alignment, so a centred title over a body at the left printed all at the
    /// left. A division starts and ends a line with no space around it; lines break WrapInset
    /// short of the right and are still aligned across the box's full width.
    /// </summary>
    [TestFixture]
    public class HtmlParagraphAlignmentTests
    {
        private static string Rdl(string value, string custom = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>200pt</Height>
      <Value>{System.Security.SecurityElement.Escape(value)}</Value>
      <CanGrow>false</CanGrow>{custom}
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <TextAlign>Left</TextAlign><LineHeight>20pt</LineHeight>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        // The runs printed, in order, after the box's own background item.
        private static async Task<PageText[]> Runs(string value, string custom = "")
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(value, custom)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            return html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray();
        }

        private static float Right(PageText t) => t.X + t.W;

        [Test]
        public async Task EachDivision_IsAlignedOnItsOwn()
        {
            var runs = await Runs("<div style=\"text-align:center\">Title</div><div>Body</div><div align=\"right\">Signed</div>");

            Assert.Multiple(() =>
            {
                Assert.That(runs.Select(r => r.Text.Trim()), Is.EqualTo(new[] { "Title", "Body", "Signed" }));
                Assert.That(runs[0].X + Right(runs[0]), Is.EqualTo(400f).Within(0.5f), "the title is centred");
                Assert.That(runs[1].X, Is.EqualTo(0f).Within(0.01f), "the body takes the box's alignment");
                Assert.That(Right(runs[2]), Is.EqualTo(400f).Within(0.5f), "the signature is at the right");
            });
        }

        // A division is a line of its own, set at the pitch of the lines and no further apart.
        [Test]
        public async Task Divisions_FollowEachOtherAtTheLinePitch()
        {
            var runs = await Runs("<div style=\"text-align:center\">Title</div><div style=\"text-align:left\">Body</div><div>&nbsp;</div><div>After</div>");

            Assert.Multiple(() =>
            {
                Assert.That(runs[1].Y - runs[0].Y, Is.EqualTo(20f).Within(0.1f));
                Assert.That(runs[2].Y - runs[1].Y, Is.EqualTo(40f).Within(0.1f), "an empty division is an empty line");
            });
        }

        // A paragraph's alignment ends with it.
        [Test]
        public async Task AParagraphsAlignment_EndsWithIt()
        {
            var runs = await Runs("<p align=\"center\">Title</p><p>Body</p>");

            Assert.Multiple(() =>
            {
                Assert.That(runs[0].X + Right(runs[0]), Is.EqualTo(400f).Within(0.5f), "the title is centred");
                Assert.That(runs[1].X, Is.EqualTo(0f).Within(0.01f), "the body is at the left");
            });
        }

        // Lines break WrapInset short of the right edge, and a right-aligned or centred one is
        // still aligned against the full width.
        [Test]
        public async Task LinesBreakShortOfTheWrapInset_AndAlignAcrossTheFullWidth()
        {
            string words = string.Join(" ", Enumerable.Repeat("words", 40));
            var runs = await Runs($"<div align=\"right\">Signed</div><div>{words}</div>",
                "<Custom><WrapInset>100pt</WrapInset></Custom>");

            Assert.Multiple(() =>
            {
                Assert.That(Right(runs[0]), Is.EqualTo(400f).Within(0.5f), "aligned against the full width");
                Assert.That(runs.Skip(1).Max(Right), Is.LessThanOrEqualTo(300.5f), "broken short of the inset");
                Assert.That(runs.Length, Is.GreaterThan(3), "the words take several lines");
            });
        }
    }
}
#endif
