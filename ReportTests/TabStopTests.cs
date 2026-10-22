#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A tab in a textbox's text goes to the next tab stop, every half inch from where the text
    /// starts, as Word and Crystal Reports place it. RDL has no tab stops, and the tab was drawn
    /// as whatever the font makes of the character, usually nothing, so a report header such as
    /// "Printed Date: 1/2/2026&lt;tab&gt;&lt;tab&gt;Last modified: ..." printed its halves run together.
    /// The tab becomes the spaces that reach the stop in the textbox's own font.
    /// </summary>
    [TestFixture]
    public class TabStopTests
    {
        private static string Rdl(string text) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""Tabbed"">
      <Top>0pt</Top><Left>0pt</Left><Width>400pt</Width><Height>12pt</Height>
      <Value>{text}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize>
        <PaddingLeft>0pt</PaddingLeft><PaddingRight>0pt</PaddingRight><PaddingTop>0pt</PaddingTop><PaddingBottom>0pt</PaddingBottom>
      </Style>
    </Textbox>
  </ReportItems></Body>
  <PageHeight>400pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<string> Printed(string text)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(text)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text;
        }

        private static int SpacesAfter(string printed, string prefix)
        {
            Assert.That(printed, Does.StartWith(prefix));
            return printed.Substring(prefix.Length).TakeWhile(c => c == ' ').Count();
        }

        [Test]
        public async Task ATab_IsSpacesToTheNextHalfInch()
        {
            string printed = await Printed("A&#9;B");

            Assert.That(printed, Does.Not.Contain("\t"));
            Assert.That(printed, Does.EndWith("B"));
            // "A" in 10pt Arial is about 6.7pt and a space about 2.8pt: about ten spaces to 36pt.
            Assert.That(SpacesAfter(printed, "A"), Is.InRange(8, 13));
        }

        [Test]
        public async Task TwoTabs_GoToTheSecondStop()
        {
            int one = SpacesAfter(await Printed("A&#9;B"), "A");
            int two = SpacesAfter(await Printed("A&#9;&#9;B"), "A");

            // Another 36pt is about thirteen more spaces.
            Assert.That(two - one, Is.InRange(10, 16));
        }

        [Test]
        public async Task LongerTextBeforeATab_GetsFewerSpaces()
        {
            int shortPrefix = SpacesAfter(await Printed("A&#9;B"), "A");
            int longPrefix = SpacesAfter(await Printed("AAAA&#9;B"), "AAAA");

            Assert.That(longPrefix, Is.LessThan(shortPrefix));
            Assert.That(longPrefix, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task TextWithoutTabs_IsUnchanged()
        {
            Assert.That(await Printed("No tabs here"), Is.EqualTo("No tabs here"));
        }
    }
}
#endif
