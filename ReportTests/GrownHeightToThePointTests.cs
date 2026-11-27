#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using Draw = Majorsilence.Forms.Drawing;

namespace ReportTests
{
    /// <summary>
    /// A growing textbox with a LineHeight is as tall as its lines at that height, to the point. Taken up to a
    /// whole pixel, four lines of 9.25pt came out 37.5pt, and each row of a list of such boxes sat half a point
    /// lower than the one before. Without a LineHeight the height is in whole pixels, as it was.
    /// </summary>
    [TestFixture]
    public class GrownHeightToThePointTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<float> FourLines(string lineHeight)
        {
            string rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <Width>7.5in</Width>
  <Body><Height>3in</Height><ReportItems>
    <Textbox Name=""Lines"">
      <Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>9.25pt</Height>
      <CanGrow>true</CanGrow>
      <Value>=""ONE"" &amp; Chr(10) &amp; ""TWO"" &amp; Chr(10) &amp; ""THREE"" &amp; Chr(10) &amp; ""FOUR""</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>7pt</FontSize>{lineHeight}</Style>
    </Textbox>
  </ReportItems></Body>
</Report>";

            var folder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(folder.LocalPath);
            string path = Path.Combine(folder.LocalPath, "GrownHeightToThePoint" + lineHeight.Length + ".rdl");
            File.WriteAllText(path, rdl);
            Report report = await RdlUtils.GetReport(new Uri(path));
            await report.RunGetData(null);

            var box = report.ReportDefinition.Body.ReportItems.Items.OfType<Textbox>().Single();
            using var bitmap = new Draw.Bitmap(10, 10);
            using var g = Draw.Graphics.FromImage(bitmap);
            return await box.RunTextCalcHeight(report, g, null);
        }

        [Test]
        public async Task WithALineHeight_TheBoxIsItsLinesAtThatHeight()
        {
            // a size is read in hundredths of a millimetre, so 9.25pt is a little under or over
            Assert.That(await FourLines("<LineHeight>9.25pt</LineHeight>"), Is.EqualTo(37f).Within(0.1f));
        }

        [Test]
        public async Task WithoutOne_TheHeightIsInWholePixels()
        {
            float pixels = await FourLines("") * 96f / 72f;

            Assert.That(pixels, Is.EqualTo(Math.Round(pixels)).Within(0.01));
        }
    }
}
#endif
