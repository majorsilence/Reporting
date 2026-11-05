using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A custom report item (barcode, QR code) is drawn to a PNG in a MemoryStream and handed to the
    /// renderer. The stream was handed over still positioned at its end, so the HTML renderer threw
    /// "Unable to read beyond the end of the stream" (the barcode was dropped and an empty .unk file
    /// left behind), Excel embedded a 0-byte picture and RTF a 1x1 one.
    /// </summary>
    [TestFixture]
    public class CustomReportItemStreamOutputTests
    {
        private string _dir;

        private const string Rdl = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <Body>
    <Height>3in</Height>
    <ReportItems>
      <CustomReportItem Name=""Qr"">
        <Type>QrCode</Type>
        <Top>0in</Top><Left>0in</Left><Width>1.2in</Width><Height>1.2in</Height>
        <CustomProperties><CustomProperty><Name>Code</Name><Value>HELLO-QR</Value></CustomProperty></CustomProperties>
      </CustomReportItem>
      <CustomReportItem Name=""Bar"">
        <Type>BarCode128</Type>
        <Top>1.5in</Top><Left>0in</Left><Width>3in</Width><Height>0.8in</Height>
        <CustomProperties><CustomProperty><Name>Code</Name><Value>HELLO-128</Value></CustomProperty></CustomProperties>
      </CustomReportItem>
    </ReportItems>
  </Body>
</Report>";

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _dir = Path.Combine(Path.GetTempPath(), "cri-stream-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private async Task<int> Render(OutputPresentationType type, string fileName)
        {
            var report = await new RDLParser(Rdl).Parse();
            await report.RunGetData(null);
            report.ErrorReset();
            using (var gen = new OneFileStreamGen(Path.Combine(_dir, fileName), true))
                await report.RunRender(gen, type);
            return report.ErrorMaxSeverity;
        }

        [Test]
        public async Task Html_WritesEachCustomReportItemAsAPng()
        {
            int severity = await Render(OutputPresentationType.HTML, "out.html");

            var images = Directory.GetFiles(_dir).Where(f => !f.EndsWith(".html")).ToList();
            Assert.That(severity, Is.LessThan(8), "rendering a custom report item must not fail");
            Assert.That(images, Has.Count.EqualTo(2));
            Assert.That(images.Select(f => new FileInfo(f).Length), Has.All.GreaterThan(100));
            Assert.That(images.Select(Path.GetExtension), Has.All.EqualTo(".png"));
        }

        [Test]
        public async Task Excel_EmbedsNonEmptyCustomReportItemImages()
        {
            int severity = await Render(OutputPresentationType.Excel2007, "out.xlsx");

            using var zip = ZipFile.OpenRead(Path.Combine(_dir, "out.xlsx"));
            var media = zip.Entries.Where(e => e.FullName.StartsWith("xl/media/")).ToList();
            Assert.That(severity, Is.LessThan(8));
            Assert.That(media, Has.Count.EqualTo(2));
            Assert.That(media.Select(e => e.Length), Has.All.GreaterThan(100), "the pictures must not be empty");
        }

        [Test]
        public async Task Rtf_EmbedsFullSizeCustomReportItemImages()
        {
            int severity = await Render(OutputPresentationType.RTF, "out.rtf");

            string rtf = Encoding.Latin1.GetString(File.ReadAllBytes(Path.Combine(_dir, "out.rtf")));
            var widths = Regex.Matches(rtf, @"\\pngblip\S* ([0-9a-fA-F\s]+)\}")
                .Select(m => Convert.FromHexString(Regex.Replace(m.Groups[1].Value, @"\s", "")))
                .Select(png => (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19])   // PNG IHDR width
                .ToList();
            Assert.That(severity, Is.LessThan(8));
            Assert.That(widths, Has.Count.EqualTo(2));
            Assert.That(widths, Has.All.GreaterThan(1), "a 1x1 picture means the barcode was lost");
        }
    }
}
