#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #367: a font size was converted from points to pixels by the engine and again by
    /// Majorsilence.Forms.Drawing.Font, so CanGrow textboxes measured their text about a third too
    /// large and grew to two lines when the renderer draws one.
    /// </summary>
    [TestFixture]
    public class CanGrowFontScaleTests
    {
        private static string Rdl(string text, int lines) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>200pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Grow"">
      <Top>0pt</Top><Left>0pt</Left><Width>200pt</Width><Height>2pt</Height>
      <Value>{text}</Value><CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>300pt</PageWidth>
</Report>";

        private static async Task<float> GrownHeight(string text)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(text, 1)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().H;
        }

        [Test]
        public async Task TextAt80PercentOfTheBox_StaysOnOneLine()
        {
            // About 160pt of Arial 10pt in a 200pt box; one line is about 12pt, two would be about 24pt+.
            float h = await GrownHeight("The quick brown fox jumps over the");
            Assert.That(h, Is.LessThan(16), $"one line of 10pt text grew to {h}pt");
        }

        [Test]
        public async Task TextWiderThanTheBox_GrowsToTwoLines()
        {
            float h = await GrownHeight("The quick brown fox jumps over the lazy dog while five wizards box");
            Assert.That(h, Is.GreaterThan(16).And.LessThan(30), $"two lines of 10pt text measured {h}pt");
        }
    }
}
#endif
