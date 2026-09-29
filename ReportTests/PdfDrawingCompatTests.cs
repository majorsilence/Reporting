using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using UglyToad.PdfPig;

namespace ReportTests
{
    /// <summary>
    /// Two ways the SkiaSharp build of the PDF renderer lost content the System.Drawing build
    /// kept: an embedded BMP never reached the page, and a line the graphics library measured
    /// a fraction too wide was split, its second half then clipped by the box.
    /// </summary>
    [TestFixture]
    public class PdfDrawingCompatTests
    {
        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
        }

        // A 4x2 24-bit BMP: a 54-byte header, then two rows of four BGR pixels (12 bytes, no padding).
        private static byte[] SmallBmp()
        {
            const int w = 4, h = 2, rowBytes = w * 3, pixelBytes = rowBytes * h;
            var bmp = new byte[54 + pixelBytes];
            bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
            BitConverter.GetBytes(bmp.Length).CopyTo(bmp, 2);
            BitConverter.GetBytes(54).CopyTo(bmp, 10);
            BitConverter.GetBytes(40).CopyTo(bmp, 14);
            BitConverter.GetBytes(w).CopyTo(bmp, 18);
            BitConverter.GetBytes(h).CopyTo(bmp, 22);
            BitConverter.GetBytes((short)1).CopyTo(bmp, 26);
            BitConverter.GetBytes((short)24).CopyTo(bmp, 28);
            BitConverter.GetBytes(pixelBytes).CopyTo(bmp, 34);
            for (int i = 54; i < bmp.Length; i += 3) { bmp[i] = 0; bmp[i + 1] = 0; bmp[i + 2] = 255; }   // red
            return bmp;
        }

        /// <summary>
        /// A BMP is re-encoded as JPEG before it is drawn, through EncoderParameters(1). The
        /// Majorsilence.Forms.Drawing that the SkiaSharp build used gave that constructor an empty
        /// Param array, so setting Param[0] threw, the image was treated as unloadable, and the
        /// PDF went out without it.
        /// </summary>
        [Test]
        public async Task EmbeddedBmp_IsDrawnIntoThePdf()
        {
            string rdl = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><Width>7.5in</Width>
  <EmbeddedImages>
    <EmbeddedImage Name=""Logo""><MIMEType>image/bmp</MIMEType><ImageData>{Convert.ToBase64String(SmallBmp())}</ImageData></EmbeddedImage>
  </EmbeddedImages>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Image Name=""Img""><Source>Embedded</Source><Value>Logo</Value><Sizing>Fit</Sizing>
        <Top>0in</Top><Left>0in</Left><Width>2in</Width><Height>1in</Height></Image>
    </ReportItems>
  </Body>
</Report>";
            var report = await new RDLParser(rdl).Parse();
            await report.RunGetData(null);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.PDF);

            byte[] pdfBytes = ((MemoryStream)ms.GetStream()).ToArray();
            using var pdf = PdfDocument.Open(pdfBytes);
            Assert.That(pdf.GetPages().SelectMany(p => p.GetImages()).Count(), Is.EqualTo(1),
                "the embedded BMP must reach the page");
        }

        // RenderBase keeps the space a soft wrap broke after, and trims a paragraph's last line.
        [TestCase(new[] { "Customer ", "ID" }, new[] { "Customer ID" })]
        [TestCase(new[] { "one ", "two ", "three" }, new[] { "one two three" })]
        [TestCase(new[] { "first line", "second line" }, new[] { "first line", "second line" })]
        [TestCase(new[] { "wrapped ", "paragraph", "next paragraph" }, new[] { "wrapped paragraph", "next paragraph" })]
        [TestCase(new[] { "", "after a blank line" }, new[] { "", "after a blank line" })]
        public void RejoinSoftWraps_JoinsOnlyTheLinesSplitAfterASpace(string[] lines, string[] expected)
        {
            Assert.That(RenderPdf_Raw.RejoinSoftWraps(lines), Is.EqualTo(expected));
        }
    }
}
