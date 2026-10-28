#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Draw = Majorsilence.Forms.Drawing;

namespace ReportTests
{
    /// <summary>
    /// A growing box grows by the lines its text is drawn in. It was measured with a "w" added to
    /// the text and wrapped by the graphics library, so text that fits its box with less than a "w"
    /// to spare grew by a line that was never drawn.
    /// </summary>
    [TestFixture]
    public class GrowLineCountTests
    {
        private const string Text = "Priority one of three";

        private static float TextWidthPoints()
        {
            using var bitmap = new Draw.Bitmap(10, 10);
            using var g = Draw.Graphics.FromImage(bitmap);
            using var font = new Draw.Font("Arial", 10);
            return g.MeasureRunWidth(Text, font) * 72f / g.DpiX;
        }

        private static async Task<PageText> Printed(float boxWidth)
        {
            string width = boxWidth.ToString("0.###", CultureInfo.InvariantCulture);
            string rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>100pt</Height><ReportItems>
    <Textbox Name=""Box""><Top>0pt</Top><Left>0pt</Left><Width>{width}pt</Width><Height>12pt</Height>
      <Value>{Text}</Value><CanGrow>true</CanGrow>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize><LineHeight>12pt</LineHeight></Style></Textbox>
  </ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single(t => t.Text == Text);
        }

        [Test]
        public async Task TextThatFitsWithLittleToSpare_DoesNotGrow()
        {
            var box = await Printed(TextWidthPoints() + 1f);

            Assert.That(box.H, Is.EqualTo(12f).Within(0.05f));
        }

        [Test]
        public async Task TextThatWrapsOnce_GrowsByOneLine()
        {
            var box = await Printed(TextWidthPoints() * 0.6f);

            Assert.That(box.H, Is.EqualTo(24f).Within(0.05f));
        }    }
}
#endif
