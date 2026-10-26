#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A table row holding a Rectangle is as tall as what the Rectangle grows to hold. Only a
    /// textbox directly in a cell counted towards the row's height, so a growing textbox in a
    /// Rectangle in a cell was drawn past the row and the next row printed over it.
    /// </summary>
    [TestFixture]
    public class TableRowContainerHeightTests
    {
        private static string Rdl(string firstCell) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <Width>200pt</Width>
      <TableColumns><TableColumn><Width>200pt</Width></TableColumn></TableColumns>
      <Header>
        <TableRows>
          <TableRow><Height>20pt</Height><TableCells><TableCell><ReportItems>{firstCell}</ReportItems></TableCell></TableCells></TableRow>
          <TableRow><Height>20pt</Height><TableCells><TableCell><ReportItems>
            <Textbox Name=""Next""><Value>NEXT</Value><Style><FontSize>10pt</FontSize></Style></Textbox>
          </ReportItems></TableCell></TableCells></TableRow>
        </TableRows>
      </Header>
    </Table>
  </ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";

        private const string GrowingRectangle = @"<Rectangle Name=""R""><ReportItems>
            <Textbox Name=""Long""><Top>2pt</Top><Left>0pt</Left><Width>200pt</Width><Height>12pt</Height>
              <Value>=""one"" &amp; Chr(10) &amp; ""two"" &amp; Chr(10) &amp; ""three"" &amp; Chr(10) &amp; ""four"" &amp; Chr(10) &amp; ""five""</Value>
              <CanGrow>true</CanGrow><Style><FontSize>10pt</FontSize></Style></Textbox>
          </ReportItems></Rectangle>";

        private static async Task<(PageText Long, PageText Next)> Printed(string firstCell)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(firstCell)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var texts = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().ToList();
            return (texts.FirstOrDefault(t => t.Text.StartsWith("one")), texts.Single(t => t.Text == "NEXT"));
        }

        [Test]
        public async Task ARowWhoseRectangleGrows_PushesTheNextRowDown()
        {
            var (grown, next) = await Printed(GrowingRectangle);

            Assert.That(grown.H, Is.GreaterThan(40f), "five lines of 10pt text");
            Assert.That(next.Y, Is.GreaterThanOrEqualTo(grown.Y + grown.H - 0.01f));
        }

        // A Rectangle whose contents fit keeps the row its own height.
        [Test]
        public async Task ARowWhoseRectangleFits_KeepsItsHeight()
        {
            var (_, next) = await Printed(@"<Rectangle Name=""R""><ReportItems>
                <Textbox Name=""Short""><Top>2pt</Top><Left>0pt</Left><Width>200pt</Width><Height>12pt</Height>
                  <Value>one</Value><CanGrow>true</CanGrow><Style><FontSize>10pt</FontSize></Style></Textbox>
              </ReportItems></Rectangle>");

            Assert.That(next.Y, Is.EqualTo(20f).Within(0.05f));
        }

        // A box or a line drawn past the row's bottom is drawn over the rows after it, as
        // designed; only what the Rectangle holds that grows moves them.
        [Test]
        public async Task ARowWhoseBoxRunsPastIt_KeepsItsHeight()
        {
            var (_, next) = await Printed(@"<Rectangle Name=""R""><ReportItems>
                <Rectangle Name=""Box""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>60pt</Height>
                  <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Rectangle>
                <Line Name=""Rule""><Top>0pt</Top><Left>150pt</Left><Width>0pt</Width><Height>60pt</Height>
                  <Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Line>
              </ReportItems></Rectangle>");

            Assert.That(next.Y, Is.EqualTo(20f).Within(0.05f));
        }
    }
}
#endif
