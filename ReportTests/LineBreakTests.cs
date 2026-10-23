#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A carriage return on its own ends a line, as it does in Crystal Reports and in Windows
    /// text controls; a report converted from Crystal builds lines with Chr(13) alone. The engine
    /// broke lines only at a line feed, and ran such lines together on one line.
    /// </summary>
    [TestFixture]
    public class LineBreakTests
    {
        private static string Rdl(string text) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Lines"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>
      <Value>{text}</Value>
      <CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<PageText> Printed(string text)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(text)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single();
        }

        [Test]
        public async Task ACarriageReturnAlone_EndsALine()
        {
            Assert.That((await Printed("A&#13;B")).Text, Is.EqualTo("A\nB"));
        }

        [Test]
        public async Task ACarriageReturnAndLineFeed_AreOneBreak()
        {
            Assert.That((await Printed("A&#13;&#10;B")).Text, Is.EqualTo("A\nB"));
        }

        [Test]
        public async Task LinesEndedByCarriageReturns_GrowTheTextboxAsLineFeedsDo()
        {
            var returns = await Printed("A&#13;B&#13;C");
            var feeds = await Printed("A&#10;B&#10;C");

            Assert.That(feeds.H, Is.GreaterThan(30f), "three lines of 10pt text");
            Assert.That(returns.H, Is.EqualTo(feeds.H));
        }

        [Test]
        public async Task TextWithoutBreaks_IsUnchanged()
        {
            Assert.That((await Printed("One line")).Text, Is.EqualTo("One line"));
        }
    }
}
#endif
