#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Rich (html) text that ends in a closing tag finishes its last line as any other: the
    /// line is aligned, its runs are set on one baseline, and it counts in the text's height.
    /// The runs before the tag were placed on the line at the tag and the line was then left as
    /// it stood: at the left whatever the alignment, each run at the top of the line, and the
    /// text a line short.
    /// </summary>
    [TestFixture]
    public class HtmlLastLineTests
    {
        private static string Rdl(string value, string align) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>
      <Value>{SecurityElement.Escape(value)}</Value>
      <CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <TextAlign>{align}</TextAlign><LineHeight>12pt</LineHeight>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<(PageTextHtml html, PageText[] runs)> Laid(string value, string align = "Left")
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(value, align)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var html = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageTextHtml>().Single();
            await html.Build(pages.G);
            return (html, html.Cast<PageItem>().OfType<PageText>().Skip(1).Where(t => t.Text.Trim().Length > 0).ToArray());
        }

        [Test]
        public async Task RightAligned_ALineEndingInAClosingTagEndsAtTheRight()
        {
            var (_, runs) = await Laid("Total <b>12.00</b>", "Right");

            Assert.That(runs[1].X + runs[1].W, Is.EqualTo(400f).Within(0.5f));
            Assert.That(runs[0].X + runs[0].W, Is.EqualTo(runs[1].X).Within(0.5f), "the runs of the line stay together");
        }

        [Test]
        public async Task ASmallerLastRun_SitsOnTheLinesBaseline()
        {
            var (_, runs) = await Laid("Subtotal <span style=\"font-size:7pt\">(before discounts)</span>");

            Assert.That(runs[1].Y + runs[1].H - runs[1].Descent,
                Is.EqualTo(runs[0].Y + runs[0].H - runs[0].Descent).Within(0.01f));
        }

        [Test]
        public async Task TextEndingInAClosingTag_CountsItsLastLine()
        {
            var (ending, _) = await Laid("First<br>Second <b>line</b>");
            var (plain, _) = await Laid("First<br>Second line");

            Assert.That(ending.TotalHeight, Is.EqualTo(plain.TotalHeight).Within(0.01f));
            Assert.That(ending.TotalHeight, Is.GreaterThan(23f), "two lines of 12pt");
        }
    }
}
#endif
