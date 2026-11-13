#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A division of rich (html) text can have margins and a first-line indent: margin-left,
    /// margin-right and text-indent in its style, in points. Its lines start at the left margin,
    /// the first at the indent from it, and break short of the right margin. A tab on a first
    /// line that hangs left of the margin stops at the margin, as a bullet's text does.
    /// </summary>
    [TestFixture]
    public class HtmlBlockIndentTests
    {
        private const string Long = "one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen";

        private static string Rdl(string value) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>0pt</Left><Width>200pt</Width><Height>12pt</Height>
      <Value>{SecurityElement.Escape(value)}</Value>
      <CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        // The runs printed, in order, after the box's own background item.
        private static async Task<PageText[]> Runs(string value)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(value)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            return html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray();
        }

        [Test]
        public async Task ALeftMargin_StartsEveryLineOfTheDivisionAtIt()
        {
            var runs = await Runs($"<div style=\"margin-left:30pt\">{Long}</div>After");

            Assert.That(runs.Length, Is.GreaterThan(3), "the division wraps");
            Assert.That(runs.Take(runs.Length - 1).Select(r => r.X), Is.All.EqualTo(30f).Within(0.01f));
            Assert.That(runs[^1].X, Is.EqualTo(0f).Within(0.01f), "the text after the division");
        }

        [Test]
        public async Task ARightMargin_BreaksTheLinesShortOfIt()
        {
            var runs = await Runs($"<div style=\"margin-right:80pt\">{Long}</div>");
            var whole = await Runs($"<div>{Long}</div>");

            Assert.That(runs.Select(r => r.X + r.W), Is.All.LessThanOrEqualTo(120f));
            Assert.That(runs.Length, Is.GreaterThan(whole.Length));
        }

        [Test]
        public async Task AHangingFirstLine_StartsLeftOfTheMarginAndItsTabStopsAtIt()
        {
            var runs = await Runs($"<div style=\"margin-left:30pt;text-indent:-30pt\">*<tab>{Long}</div>");

            Assert.That(runs[0].Text.Trim(), Is.EqualTo("*"));
            Assert.That(runs[0].X, Is.EqualTo(0f).Within(0.01f), "the bullet");
            Assert.That(runs.Skip(1).Select(r => r.X), Is.All.EqualTo(30f).Within(0.01f), "its text, and the lines under it");
        }

        [Test]
        public async Task AFirstLineIndent_MovesTheFirstLineOnly()
        {
            var runs = await Runs($"<div style=\"text-indent:20pt\">{Long}</div>");

            Assert.That(runs[0].X, Is.EqualTo(20f).Within(0.01f));
            Assert.That(runs[1].X, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public async Task RightAligned_ALineEndsAtTheRightMargin()
        {
            var runs = await Runs("<div style=\"text-align:right;margin-right:50pt\">Total due</div>Plain");

            Assert.That(runs[0].X + runs[0].W, Is.EqualTo(150f).Within(0.5f));
        }
    }
}
#endif
