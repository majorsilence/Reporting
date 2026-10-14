#if NET8_0_OR_GREATER
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
    /// The height of a growing textbox with a constant style is measured with a font, formats and paddings made
    /// once per run (Style.GetMeasureSetup); any other style is measured with them evaluated per call. Both have
    /// to give the same height.
    /// </summary>
    [TestFixture]
    public class GrowHeightTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Box(string name, string top, string value, string direction, string lineHeight) => $@"
    <Textbox Name=""{name}"">
      <Top>{top}</Top><Left>0in</Left><Width>1.5in</Width><Height>0.2in</Height>
      <CanGrow>true</CanGrow>
      <Value>{value}</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>9pt</FontSize><FontWeight>Bold</FontWeight>{lineHeight}
        <Direction>{direction}</Direction>
        <PaddingLeft>3pt</PaddingLeft><PaddingRight>5pt</PaddingRight>
        <PaddingTop>2pt</PaddingTop><PaddingBottom>4pt</PaddingBottom></Style>
    </Textbox>";

        [TestCase("")]
        [TestCase("<LineHeight>14pt</LineHeight>")]
        public async Task ConstantStyle_MeasuresTheSameAsPerCallEvaluation(string lineHeight)
        {
            string text = "A long line of text that has to wrap over several lines in a narrow textbox to grow it";
            string rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <Width>7.5in</Width>
  <Body><Height>3in</Height><ReportItems>
    {Box("Constant", "0in", text, "LTR", lineHeight)}
    {Box("PerCall", "1in", text, "=IIF(Year(Now()) &gt; 1900, \"LTR\", \"RTL\")", lineHeight)}
  </ReportItems></Body>
</Report>";

            var folder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(folder.LocalPath);
            string path = Path.Combine(folder.LocalPath, "GrowHeight" + lineHeight.Length + ".rdl");
            File.WriteAllText(path, rdl);
            Report report = await RdlUtils.GetReport(new System.Uri(path));
            await report.RunGetData(null);

            var boxes = report.ReportDefinition.Body.ReportItems.Items.OfType<Textbox>().ToDictionary(t => t.Name.Nm);
            using var bitmap = new Draw.Bitmap(10, 10);
            using var g = Draw.Graphics.FromImage(bitmap);

            float constant = await boxes["Constant"].RunTextCalcHeight(report, g, null);
            float again = await boxes["Constant"].RunTextCalcHeight(report, g, null);     // from the kept setup
            float perCall = await boxes["PerCall"].RunTextCalcHeight(report, g, null);

            Assert.That(boxes["Constant"].Style.TryGetMeasureSetup(report), Is.Not.Null, "constant style keeps a setup");
            Assert.That(boxes["PerCall"].Style.TryGetMeasureSetup(report), Is.Null, "an expression Direction is measured per call");
            Assert.That(constant, Is.GreaterThan(0.4f * 72), "the text wraps, so the box grows");
            Assert.That(again, Is.EqualTo(constant));
            Assert.That(perCall, Is.EqualTo(constant));
        }
    }
}
#endif
