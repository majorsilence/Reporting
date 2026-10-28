#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A hidden item keeps its place, and the items below it are placed from the bottom it would
    /// have had on the page. A hidden Textbox, Image, Line or Rectangle recorded that bottom
    /// without the page's offset - the top margin, or the top of the rectangle holding it - so
    /// everything placed below it printed that much too high. A line recorded its start, so what
    /// was below a vertical one printed the line's height too high.
    /// </summary>
    [TestFixture]
    public class HiddenItemPositionTests
    {
        private const string Hidden = "<Visibility><Hidden>=1 = 1</Hidden></Visibility>";

        private static readonly string[] HiddenItems =
        {
            $@"<Textbox Name=""H""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
                 <Value>hidden</Value>{Hidden}</Textbox>",
            $@"<Image Name=""H""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
                 <Source>External</Source><Value>none.png</Value>{Hidden}</Image>",
            $@"<Line Name=""H""><Top>10pt</Top><Left>0pt</Left><Width>100pt</Width><Height>0pt</Height>{Hidden}</Line>",
            $@"<Rectangle Name=""H""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>{Hidden}</Rectangle>",
        };

        private const string Below = @"<Textbox Name=""Below""><Top>20pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
            <Value>BELOW</Value></Textbox>";

        private static string Rdl(string bodyItems) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <TopMargin>36pt</TopMargin><LeftMargin>36pt</LeftMargin>
  <Body><Height>300pt</Height><ReportItems>{bodyItems}</ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private static async Task<PageText> PrintedBelow(string bodyItems)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(bodyItems)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single(t => t.Text == "BELOW");
        }

        [TestCaseSource(nameof(HiddenItems))]
        public async Task AnItemBelowAHiddenOneInTheBody_KeepsTheTopMargin(string hidden)
        {
            var below = await PrintedBelow(hidden + Below);

            Assert.That(below.Y, Is.EqualTo(36f + 20f).Within(0.05f));
        }

        [TestCaseSource(nameof(HiddenItems))]
        public async Task AnItemBelowAHiddenOneInARectangle_KeepsTheRectanglesTop(string hidden)
        {
            var below = await PrintedBelow($@"<Rectangle Name=""R""><Top>100pt</Top><Left>0pt</Left><Width>200pt</Width><Height>40pt</Height>
                <ReportItems>{hidden}{Below}</ReportItems></Rectangle>");

            Assert.That(below.Y, Is.EqualTo(36f + 100f + 20f).Within(0.05f));
        }

        // A line's items below are placed from its end: a vertical line recorded its start, so what
        // was below it printed the line's height too high.
        [TestCase("")]
        [TestCase(Hidden)]
        public async Task AnItemBelowAVerticalLine_IsPlacedBelowItsEnd(string visibility)
        {
            var below = await PrintedBelow($@"<Line Name=""V""><Top>0pt</Top><Left>0pt</Left><Width>0pt</Width><Height>100pt</Height>
                <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>{visibility}</Line>
              <Textbox Name=""Below""><Top>120pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
                <Value>BELOW</Value></Textbox>");

            Assert.That(below.Y, Is.EqualTo(36f + 120f).Within(0.05f));
        }

        // As where the item above is shown.
        [Test]
        public async Task AnItemBelowAShownOneInARectangle_KeepsTheRectanglesTop()
        {
            var below = await PrintedBelow($@"<Rectangle Name=""R""><Top>100pt</Top><Left>0pt</Left><Width>200pt</Width><Height>40pt</Height>
                <ReportItems><Textbox Name=""Shown""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
                  <Value>shown</Value></Textbox>{Below}</ReportItems></Rectangle>");

            Assert.That(below.Y, Is.EqualTo(36f + 100f + 20f).Within(0.05f));
        }
    }
}
#endif
