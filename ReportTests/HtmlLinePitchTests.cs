#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Each line of rich (html) text is set at its own pitch: the LineHeight of the runs on it,
    /// which a span's line-height sets, and a line of no text at the pitch of the style its
    /// break is in. The height of a line was carried to the lines after a line break, so the
    /// lines under a heading in a larger size were all as far apart as the heading's.
    /// </summary>
    [TestFixture]
    public class HtmlLinePitchTests
    {
        private static string Rdl(string value) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Rich"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>
      <Value>{SecurityElement.Escape(value)}</Value>
      <CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><Format>html</Format>
        <LineHeight>12pt</LineHeight>
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
        public async Task LinesUnderALargerHeading_AreSetAtTheirOwnPitch()
        {
            var runs = await Runs("<span style=\"font-size:20pt\">Heading</span><br>One<br>Two<br>Three");

            Assert.That(runs.Select(r => r.Text.Trim()), Is.EqualTo(new[] { "Heading", "One", "Two", "Three" }));
            Assert.That(runs[2].Y - runs[1].Y, Is.EqualTo(12f).Within(0.01f));
            Assert.That(runs[3].Y - runs[2].Y, Is.EqualTo(12f).Within(0.01f));
        }

        [Test]
        public async Task ASpansLineHeight_SetsItsLinesPitch()
        {
            var runs = await Runs("<span style=\"line-height:20pt\">One</span><br>Two<br>Three");

            Assert.That(runs[1].Y - runs[0].Y, Is.EqualTo(20f).Within(0.01f), "the line in the span");
            Assert.That(runs[2].Y - runs[1].Y, Is.EqualTo(12f).Within(0.01f), "the line after it, at the box's");
        }

        [Test]
        public async Task ALineInTwoPitches_IsSetAtTheGreater()
        {
            var runs = await Runs("One <span style=\"line-height:20pt\">two</span><br>Three");

            Assert.That(runs[2].Y - runs[0].Y, Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public async Task AnEmptyLine_IsAsTallAsTheStyleItsBreakIsIn()
        {
            var runs = await Runs("One<br><span style=\"line-height:20pt\"><br></span>Two");

            Assert.That(runs[1].Y - runs[0].Y, Is.EqualTo(12f + 20f).Within(0.01f));
        }
    }
}
#endif
