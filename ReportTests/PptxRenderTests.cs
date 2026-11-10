#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// OutputPresentationType.PPTX writes a PowerPoint presentation, one slide per page, the slides
    /// the size of the page. Each test opens the package, reads its slides as XML and checks the
    /// whole package with the Open XML SDK validator.
    /// </summary>
    [TestFixture]
    public class PptxRenderTests
    {
        const string A = "http://schemas.openxmlformats.org/drawingml/2006/main";
        const string P = "http://schemas.openxmlformats.org/presentationml/2006/main";
        const string R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const long EmuPerInch = 914400;

        private Uri _reportFolder;
        private Uri _outputFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            _outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task TableReport_HasOneSlidePerPage()
        {
            Report rap = await LoadReport("WorldFacts.rdl");
            byte[] pptx = await Render(rap, "WorldFacts");

            using var zip = Open(pptx);
            int slides = zip.Entries.Count(e => e.FullName.StartsWith("ppt/slides/slide", StringComparison.Ordinal));
            Assert.That(slides, Is.EqualTo(rap.TotalPages), "one slide per page");
            Assert.That(slides, Is.GreaterThan(1), "WorldFacts runs to more than one page");

            XmlDocument presentation = LoadXml(zip, "ppt/presentation.xml");
            var ns = Namespaces(presentation);
            Assert.That(presentation.SelectNodes("//p:sldIdLst/p:sldId", ns).Count, Is.EqualTo(slides));

            string first = LoadXml(zip, "ppt/slides/slide1.xml").DocumentElement.InnerText;
            Assert.That(first, Does.Contain("Continent"), "the table header is text on the slide");
            Assert.That(first, Does.Contain("The World's Countries"), "and so is the page header");

            AssertValid(pptx);
        }

        [Test]
        public async Task SlideIsThePageSize()
        {
            byte[] pptx = await RenderRdl(Rdl("11in", "8.5in", """
                <Textbox Name="T"><Top>1in</Top><Left>1in</Left><Width>2in</Width><Height>0.5in</Height><Value>Landscape</Value></Textbox>
                """), "PageSize");

            using var zip = Open(pptx);
            XmlDocument presentation = LoadXml(zip, "ppt/presentation.xml");
            XmlElement size = (XmlElement)presentation.SelectSingleNode("//p:sldSz", Namespaces(presentation));
            Assert.That(long.Parse(size.GetAttribute("cx")), Is.EqualTo(11 * EmuPerInch));
            Assert.That(long.Parse(size.GetAttribute("cy")), Is.EqualTo(17 * EmuPerInch / 2));
            AssertValid(pptx);
        }

        [Test]
        public async Task Textbox_IsAnEditableTextShapeWithItsStyle()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Textbox Name="Styled">
                  <Top>1in</Top><Left>2in</Left><Width>3in</Width><Height>1in</Height>
                  <Value>Hello &amp; welcome</Value>
                  <Style>
                    <FontFamily>Courier New</FontFamily><FontSize>14pt</FontSize><FontWeight>Bold</FontWeight>
                    <FontStyle>Italic</FontStyle><TextDecoration>Underline</TextDecoration>
                    <Color>#FF0000</Color><BackgroundColor>#00FF00</BackgroundColor>
                    <TextAlign>Center</TextAlign><VerticalAlign>Middle</VerticalAlign>
                    <BorderStyle><Default>Solid</Default></BorderStyle><BorderColor><Default>#0000FF</Default></BorderColor>
                    <BorderWidth><Default>2pt</Default></BorderWidth>
                    <PaddingLeft>4pt</PaddingLeft>
                  </Style>
                </Textbox>
                """), "Textbox");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            XmlElement sp = Shape(slide, ns, "Hello & welcome");

            XmlElement off = (XmlElement)sp.SelectSingleNode("p:spPr/a:xfrm/a:off", ns);
            XmlElement ext = (XmlElement)sp.SelectSingleNode("p:spPr/a:xfrm/a:ext", ns);
            Assert.That(sp.SelectSingleNode("p:nvSpPr/p:cNvSpPr", ns).Attributes["txBox"]?.Value, Is.EqualTo("1"));
            Assert.That(long.Parse(off.GetAttribute("x")), Is.EqualTo(2 * EmuPerInch + Margin));
            Assert.That(long.Parse(off.GetAttribute("y")), Is.EqualTo(1 * EmuPerInch + Margin));
            Assert.That(long.Parse(ext.GetAttribute("cx")), Is.EqualTo(3 * EmuPerInch));
            Assert.That(long.Parse(ext.GetAttribute("cy")), Is.EqualTo(1 * EmuPerInch));

            XmlElement rPr = (XmlElement)sp.SelectSingleNode(".//a:r/a:rPr", ns);
            Assert.Multiple(() =>
            {
                Assert.That(rPr.GetAttribute("sz"), Is.EqualTo("1400"));
                Assert.That(rPr.GetAttribute("b"), Is.EqualTo("1"));
                Assert.That(rPr.GetAttribute("i"), Is.EqualTo("1"));
                Assert.That(rPr.GetAttribute("u"), Is.EqualTo("sng"));
                Assert.That(((XmlElement)rPr.SelectSingleNode("a:solidFill/a:srgbClr", ns)).GetAttribute("val"), Is.EqualTo("FF0000"));
                Assert.That(((XmlElement)rPr.SelectSingleNode("a:latin", ns)).GetAttribute("typeface"), Is.EqualTo("Courier New"));
                Assert.That(((XmlElement)sp.SelectSingleNode(".//a:pPr", ns)).GetAttribute("algn"), Is.EqualTo("ctr"));

                XmlElement bodyPr = (XmlElement)sp.SelectSingleNode("p:txBody/a:bodyPr", ns);
                Assert.That(bodyPr.GetAttribute("anchor"), Is.EqualTo("ctr"));
                Assert.That(bodyPr.GetAttribute("wrap"), Is.EqualTo("square"), "PowerPoint wraps the text");
                Assert.That(long.Parse(bodyPr.GetAttribute("lIns")), Is.EqualTo(4 * 12700).Within(1).Percent, "padding is the inset");

                Assert.That(((XmlElement)sp.SelectSingleNode("p:spPr/a:solidFill/a:srgbClr", ns)).GetAttribute("val"), Is.EqualTo("00FF00"));
                XmlElement ln = (XmlElement)sp.SelectSingleNode("p:spPr/a:ln", ns);
                Assert.That(long.Parse(ln.GetAttribute("w")), Is.EqualTo(2 * 12700).Within(1).Percent, "a border alike on all sides is the outline");
                Assert.That(((XmlElement)ln.SelectSingleNode("a:solidFill/a:srgbClr", ns)).GetAttribute("val"), Is.EqualTo("0000FF"));
            });

            AssertValid(pptx);
        }

        [Test]
        public async Task TextWithLineBreaks_IsOneParagraphPerLine()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Textbox Name="Lines"><Top>1in</Top><Left>1in</Left><Width>3in</Width><Height>1in</Height>
                  <Value>="first" &amp; Chr(13) &amp; Chr(10) &amp; "second" &amp; Chr(10) &amp; "third"</Value></Textbox>
                """), "LineBreaks");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            XmlElement sp = Shape(slide, ns, "first");
            var paragraphs = sp.SelectNodes("p:txBody/a:p", ns).Cast<XmlElement>().Select(p => p.InnerText).ToArray();
            Assert.That(paragraphs, Is.EqualTo(new[] { "first", "second", "third" }));
            AssertValid(pptx);
        }

        [Test]
        public async Task BorderOnOneSide_IsALine()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Textbox Name="Underlined"><Top>1in</Top><Left>1in</Left><Width>3in</Width><Height>0.5in</Height><Value>Total</Value>
                  <Style><BorderStyle><Bottom>Solid</Bottom></BorderStyle><BorderColor><Bottom>#123456</Bottom></BorderColor></Style></Textbox>
                """), "BottomBorder");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            XmlElement sp = Shape(slide, ns, "Total");
            Assert.That(sp.SelectSingleNode("p:spPr/a:ln/a:noFill", ns), Is.Not.Null, "the shape has no outline of its own");

            var lines = slide.SelectNodes("//p:cxnSp", ns).Cast<XmlElement>().ToList();
            Assert.That(lines, Has.Count.EqualTo(1), "only the bottom side is drawn");
            XmlElement off = (XmlElement)lines[0].SelectSingleNode("p:spPr/a:xfrm/a:off", ns);
            XmlElement ext = (XmlElement)lines[0].SelectSingleNode("p:spPr/a:xfrm/a:ext", ns);
            Assert.That(long.Parse(off.GetAttribute("y")), Is.EqualTo(Margin + 3 * EmuPerInch / 2), "along the bottom of the box");
            Assert.That(long.Parse(ext.GetAttribute("cx")), Is.EqualTo(3 * EmuPerInch));
            Assert.That(long.Parse(ext.GetAttribute("cy")), Is.EqualTo(0));
            Assert.That(((XmlElement)lines[0].SelectSingleNode(".//a:ln/a:solidFill/a:srgbClr", ns)).GetAttribute("val"), Is.EqualTo("123456"));
            AssertValid(pptx);
        }

        [Test]
        public async Task Hyperlink_IsAClickableLink()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Textbox Name="Link"><Top>1in</Top><Left>1in</Left><Width>3in</Width><Height>0.5in</Height><Value>Site</Value>
                  <Action><Hyperlink>https://example.com/a?b=1&amp;c=2</Hyperlink></Action></Textbox>
                """), "Hyperlink");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            XmlElement link = (XmlElement)Shape(slide, ns, "Site").SelectSingleNode("p:nvSpPr/p:cNvPr/a:hlinkClick", ns);
            Assert.That(link, Is.Not.Null);

            XmlDocument rels = LoadXml(zip, "ppt/slides/_rels/slide1.xml.rels");
            XmlElement rel = rels.DocumentElement.ChildNodes.Cast<XmlElement>().Single(e => e.GetAttribute("Id") == link.GetAttribute("id", R));
            Assert.That(rel.GetAttribute("Target"), Is.EqualTo("https://example.com/a?b=1&c=2"));
            Assert.That(rel.GetAttribute("TargetMode"), Is.EqualTo("External"));
            AssertValid(pptx);
        }

        [Test]
        public async Task LinesAndRectangles_AreShapes()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Rectangle Name="Box"><Top>1in</Top><Left>1in</Left><Width>2in</Width><Height>1in</Height>
                  <Style><BackgroundColor>Yellow</BackgroundColor><BorderStyle><Default>Dashed</Default></BorderStyle></Style></Rectangle>
                <Line Name="Up"><Top>4in</Top><Left>1in</Left><Width>2in</Width><Height>-1in</Height>
                  <Style><BorderStyle><Default>Solid</Default></BorderStyle><BorderWidth><Default>3pt</Default></BorderWidth></Style></Line>
                """), "Shapes");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);

            XmlElement rect = slide.SelectNodes("//p:sp", ns).Cast<XmlElement>()
                .Single(s => s.SelectSingleNode("p:spPr/a:prstGeom", ns) is XmlElement g && g.GetAttribute("prst") == "rect"
                             && s.SelectSingleNode("p:txBody", ns) == null);
            Assert.That(((XmlElement)rect.SelectSingleNode("p:spPr/a:solidFill/a:srgbClr", ns)).GetAttribute("val"), Is.EqualTo("FFFF00"));
            Assert.That(((XmlElement)rect.SelectSingleNode("p:spPr/a:ln/a:prstDash", ns)).GetAttribute("val"), Is.EqualTo("dash"));

            XmlElement line = (XmlElement)slide.SelectSingleNode("//p:cxnSp", ns);
            XmlElement xfrm = (XmlElement)line.SelectSingleNode("p:spPr/a:xfrm", ns);
            Assert.That(xfrm.GetAttribute("flipV"), Is.EqualTo("1"), "a line going up is flipped");
            Assert.That(long.Parse(((XmlElement)xfrm.SelectSingleNode("a:off", ns)).GetAttribute("y")), Is.EqualTo(Margin + 3 * EmuPerInch));
            Assert.That(long.Parse(((XmlElement)line.SelectSingleNode("p:spPr/a:ln", ns)).GetAttribute("w")), Is.EqualTo(3 * 12700).Within(1).Percent);
            AssertValid(pptx);
        }

        [Test]
        public async Task Image_IsAPicture_StoredOnce()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Image Name="A"><Top>1in</Top><Left>1in</Left><Width>1in</Width><Height>1in</Height><Source>Embedded</Source><Value>Dot</Value><Sizing>Fit</Sizing></Image>
                <Image Name="B"><Top>3in</Top><Left>1in</Left><Width>2in</Width><Height>1in</Height><Source>Embedded</Source><Value>Dot</Value><Sizing>Fit</Sizing></Image>
                """), "Images");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            var pics = slide.SelectNodes("//p:pic", ns).Cast<XmlElement>().ToList();
            Assert.That(pics, Has.Count.EqualTo(2));
            Assert.That(zip.Entries.Count(e => e.FullName.StartsWith("ppt/media/", StringComparison.Ordinal)), Is.EqualTo(1),
                "the same image is in the package once");
            string embed = ((XmlElement)pics[0].SelectSingleNode("p:blipFill/a:blip", ns)).GetAttribute("embed", R);
            Assert.That(((XmlElement)pics[1].SelectSingleNode("p:blipFill/a:blip", ns)).GetAttribute("embed", R), Is.EqualTo(embed));
            Assert.That(long.Parse(((XmlElement)pics[1].SelectSingleNode("p:spPr/a:xfrm/a:ext", ns)).GetAttribute("cx")), Is.EqualTo(2 * EmuPerInch),
                "Fit stretches the picture to its box");
            AssertValid(pptx);
        }

        [Test]
        public async Task Chart_IsAPicture()
        {
            Report rap = await LoadReport("ChartTypes.rdl");
            byte[] pptx = await Render(rap, "ChartTypes");

            using var zip = Open(pptx);
            XmlDocument slide = LoadXml(zip, "ppt/slides/slide1.xml");
            var ns = Namespaces(slide);
            Assert.That(slide.SelectNodes("//p:pic", ns).Count, Is.GreaterThan(0), "each chart is a picture");
            Assert.That(zip.Entries.Any(e => e.FullName.StartsWith("ppt/media/", StringComparison.Ordinal)), Is.True, "with the chart images in the package");
            AssertValid(pptx);
        }

        [TestCase("LineObjects.rdl")]
        [TestCase("ImageSizing.rdl")]
        [TestCase("HtmlTextboxTest.rdl")]
        [TestCase("WritingModeRotate180Test.rdl")]
        [TestCase("MatrixExample.rdl")]
        public async Task Report_IsAValidPresentation(string reportFile)
        {
            Report rap = await LoadReport(reportFile);
            byte[] pptx = await Render(rap, Path.GetFileNameWithoutExtension(reportFile));
            AssertValid(pptx);
        }

        [Test]
        public async Task SameReport_GivesTheSameBytes()
        {
            Report rap = await LoadReport("WorldFacts.rdl");
            byte[] first = await Render(rap, null);
            byte[] second = await Render(rap, null);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public async Task RenderFormatName_IsPptx()
        {
            byte[] pptx = await RenderRdl(Rdl("8.5in", "11in", """
                <Textbox Name="Fmt"><Top>1in</Top><Left>1in</Left><Width>3in</Width><Height>0.5in</Height><Value>=Globals!RenderFormat.Name</Value></Textbox>
                """), "RenderFormat");

            using var zip = Open(pptx);
            Assert.That(LoadXml(zip, "ppt/slides/slide1.xml").DocumentElement.InnerText, Does.Contain("PPTX"));
        }

        #region helpers

        // The test reports' margins, which the page items are offset by.
        private const long Margin = EmuPerInch / 2;

        // A 1x1 PNG.
        private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        private static string Rdl(string pageWidth, string pageHeight, string items) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>{pageWidth}</PageWidth><PageHeight>{pageHeight}</PageHeight>
              <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
              <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
              <Width>7in</Width>
              <EmbeddedImages>
                <EmbeddedImage Name="Dot"><MIMEType>image/png</MIMEType><ImageData>{Png}</ImageData></EmbeddedImage>
              </EmbeddedImages>
              <Body>
                <Height>6in</Height>
                <ReportItems>
                  {items}
                </ReportItems>
              </Body>
            </Report>
            """;

        private static XmlElement Shape(XmlDocument slide, XmlNamespaceManager ns, string text)
        {
            var sp = slide.SelectNodes("//p:sp", ns).Cast<XmlElement>()
                .FirstOrDefault(s => s.SelectSingleNode("p:txBody", ns)?.InnerText.Contains(text) == true);
            Assert.That(sp, Is.Not.Null, $"no text shape with '{text}'");
            return sp;
        }

        private async Task<Report> LoadReport(string reportFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(rap, Is.Not.Null, $"Report '{reportFile}' failed to parse");
            Assert.That(rap.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), $"Report '{reportFile}' has fatal parse errors");
            rap.Folder = _reportFolder.LocalPath;
            await rap.RunGetData();
            return rap;
        }

        private async Task<byte[]> RenderRdl(string rdl, string name)
        {
            using Report rap = await new RDLParser(rdl) { Folder = _reportFolder.LocalPath }.Parse();
            Assert.That(rap.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                rap.ErrorItems == null ? "" : string.Join("; ", rap.ErrorItems.Cast<object>()));
            rap.Folder = _reportFolder.LocalPath;
            await rap.RunGetData(null);
            return await Render(rap, name);
        }

        private async Task<byte[]> Render(Report rap, string name)
        {
            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.PPTX);
            byte[] pptx = ((MemoryStream)ms.GetStream()).ToArray();
            Assert.That(pptx.Length, Is.GreaterThan(0), "pptx output is empty");
            Assert.That(rap.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                rap.ErrorItems == null ? "" : string.Join("; ", rap.ErrorItems.Cast<object>()));

            // Also write to disk for manual inspection during development
            if (name != null)
                await File.WriteAllBytesAsync(Path.Combine(_outputFolder.LocalPath, name + "_PowerPoint.pptx"), pptx);
            return pptx;
        }

        private static ZipArchive Open(byte[] pptx) => new ZipArchive(new MemoryStream(pptx), ZipArchiveMode.Read);

        private static XmlDocument LoadXml(ZipArchive zip, string path)
        {
            ZipArchiveEntry entry = zip.GetEntry(path);
            Assert.That(entry, Is.Not.Null, path + " is missing");
            var doc = new XmlDocument();
            using (Stream s = entry.Open())
                doc.Load(s);        // throws when the part isn't well formed XML
            return doc;
        }

        private static XmlNamespaceManager Namespaces(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("a", A);
            ns.AddNamespace("p", P);
            ns.AddNamespace("r", R);
            return ns;
        }

        private static void AssertValid(byte[] pptx)
        {
            using var ms = new MemoryStream(pptx);
            Assert.That(OpenXmlUtils.ValidatePresentationDocument(ms), Is.True);
        }

        #endregion
    }
}
#endif
