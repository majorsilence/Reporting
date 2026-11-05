using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using SkiaSharp;

namespace ReportTests
{
    /// <summary>
    /// The PDF renderer decoded every non-JPEG image to RGB and dropped the alpha channel, so the
    /// see-through parts of a transparent PNG were drawn black. A transparent image must now carry
    /// a soft mask (SMask); a fully opaque one must not.
    /// </summary>
    [TestFixture]
    public class PdfTransparentImageTests
    {
        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // An 8x8 PNG: a 4x4 opaque blue centre, fully transparent around it unless opaque is set.
        private static byte[] Png(bool opaque)
        {
            using var bmp = new SKBitmap(new SKImageInfo(8, 8, SKColorType.Rgba8888, SKAlphaType.Unpremul));
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    bool centre = x >= 2 && x < 6 && y >= 2 && y < 6;
                    bmp.SetPixel(x, y, centre || opaque ? new SKColor(30, 111, 217, 255) : new SKColor(0, 0, 0, 0));
                }
            using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static async Task<string> RenderPdf(byte[] png)
        {
            string rdl = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <EmbeddedImages>
    <EmbeddedImage Name=""Pic""><MIMEType>image/png</MIMEType><ImageData>{Convert.ToBase64String(png)}</ImageData></EmbeddedImage>
  </EmbeddedImages>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Image Name=""Img""><Source>Embedded</Source><Value>Pic</Value><Sizing>Fit</Sizing>
        <Top>0in</Top><Left>0in</Left><Width>1in</Width><Height>1in</Height></Image>
    </ReportItems>
  </Body>
</Report>";
            var report = await new RDLParser(rdl).Parse();
            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.PDF);
            return Encoding.Latin1.GetString(((MemoryStream)ms.GetStream()).ToArray());
        }

        [Test]
        public async Task TransparentPng_IsDrawnWithASoftMask()
        {
            string pdf = await RenderPdf(Png(opaque: false));
            Assert.That(pdf, Does.Contain("/SMask"), "the see-through parts would be drawn black without a soft mask");
        }

        [Test]
        public async Task OpaquePng_IsDrawnWithoutASoftMask()
        {
            string pdf = await RenderPdf(Png(opaque: true));
            Assert.That(pdf, Does.Not.Contain("/SMask"));
        }
    }
}
