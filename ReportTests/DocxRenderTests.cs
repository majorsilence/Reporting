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
    /// OutputPresentationType.Word writes a real Word document (.docx, WordprocessingML), not RTF.
    /// Each test opens the package, reads document.xml as XML and checks it with the Open XML
    /// SDK validator.
    /// </summary>
    [TestFixture]
    public class DocxRenderTests
    {
        const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

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
        public async Task Word_TableReport_IsAValidDocxWithATable()
        {
            byte[] docx = await RenderDocx("WorldFacts.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            Assert.Multiple(() =>
            {
                Assert.That(zip.GetEntry("[Content_Types].xml"), Is.Not.Null, "content types");
                Assert.That(zip.GetEntry("_rels/.rels"), Is.Not.Null, "package relationships");
                Assert.That(zip.GetEntry("word/document.xml"), Is.Not.Null, "main document");
                Assert.That(zip.GetEntry("word/_rels/document.xml.rels"), Is.Not.Null, "document relationships");
            });

            XmlDocument doc = LoadXml(zip, "word/document.xml");
            XmlNamespaceManager ns = Namespaces(doc);
            Assert.That(doc.SelectNodes("//w:tbl", ns).Count, Is.GreaterThan(0), "the table becomes a Word table");
            Assert.That(doc.SelectNodes("//w:tbl/w:tr", ns).Count, Is.GreaterThan(1), "with its rows");
            Assert.That(doc.SelectSingleNode("/w:document/w:body/w:sectPr/w:pgSz", ns), Is.Not.Null, "page size");

            Assert.That(doc.DocumentElement.InnerText, Does.Contain("Continent"), "table header text");
            Assert.That(doc.SelectSingleNode("//w:tblHeader", ns), Is.Not.Null, "the table header repeats on each page");

            // the page header is its own part, used by the section
            Assert.That(doc.SelectSingleNode("//w:sectPr/w:headerReference", ns), Is.Not.Null, "the section uses the header");
            XmlDocument header = LoadXml(zip, "word/header1.xml");
            Assert.That(header.DocumentElement.InnerText, Does.Contain("The World's Countries"), "page header text");

            AssertValid(docx);
        }

        [Test]
        public async Task Word_IsNotRtf_RtfStillIs()
        {
            byte[] docx = await RenderDocx("WorldFacts.rdl");
            Assert.That(docx[0] == (byte)'P' && docx[1] == (byte)'K', "a .docx is a zip package");

            Report rap = await LoadReport("WorldFacts.rdl");
            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.RTF);
            Assert.That(ms.GetText(), Does.StartWith("{\\rtf"), "RTF output is unchanged");
        }

        [Test]
        public async Task Word_MatrixReport_ProducesATable()
        {
            byte[] docx = await RenderDocx("MatrixExample.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            XmlDocument doc = LoadXml(zip, "word/document.xml");
            XmlNamespaceManager ns = Namespaces(doc);
            Assert.That(doc.SelectNodes("//w:tbl", ns).Count, Is.GreaterThan(0), "the matrix becomes a Word table");
            Assert.That(doc.DocumentElement.InnerText, Does.Contain("Grand Total"));

            AssertValid(docx);
        }

        [Test]
        public async Task Word_PageFooter_PageNumbersBecomeFields_AndHtmlBecomesText()
        {
            byte[] docx = await RenderDocx("ListPagingTest.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            Assert.That(zip.GetEntry("word/footer1.xml"), Is.Not.Null, "the page footer has its own part");

            XmlDocument footer = LoadXml(zip, "word/footer1.xml");
            XmlNamespaceManager fns = Namespaces(footer);
            var fields = footer.SelectNodes("//w:fldSimple", fns).Cast<XmlElement>()
                .Select(e => e.GetAttribute("instr", W).Trim()).ToList();
            Assert.That(fields, Is.EquivalentTo(new[] { "PAGE", "NUMPAGES" }), "page number and total pages are Word fields");
            Assert.That(footer.DocumentElement.InnerText, Does.Contain("Page ").And.Contain(" of "));

            XmlDocument doc = LoadXml(zip, "word/document.xml");
            XmlNamespaceManager ns = Namespaces(doc);
            Assert.That(doc.SelectSingleNode("//w:sectPr/w:footerReference", ns), Is.Not.Null, "the section uses the footer");
            string text = doc.DocumentElement.InnerText;
            Assert.That(text, Does.Contain("ROW1 WORD01"), "list rows are written");
            Assert.That(text, Does.Contain("HPARA01"), "html text is kept");
            Assert.That(text, Does.Not.Contain("<p>").And.Not.Contain("<b>"), "but not its tags");

            AssertValid(docx);
        }

        [Test]
        public async Task Word_Image_IsEmbeddedAtItsReportSize()
        {
            byte[] docx = await RenderDocx("ImageSizing.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            Assert.That(zip.Entries.Count(e => e.FullName.StartsWith("word/media/")), Is.EqualTo(1), "one picture");

            XmlDocument doc = LoadXml(zip, "word/document.xml");
            XmlNamespaceManager ns = Namespaces(doc);
            var extent = (XmlElement)doc.SelectSingleNode("//wp:inline/wp:extent", ns);
            Assert.That(extent, Is.Not.Null, "the picture is inline in the document");
            // 2in x 1in; 914400 EMUs to the inch
            Assert.Multiple(() =>
            {
                Assert.That(long.Parse(extent.GetAttribute("cx")), Is.EqualTo(1828800L).Within(12700L), "width");
                Assert.That(long.Parse(extent.GetAttribute("cy")), Is.EqualTo(914400L).Within(12700L), "height");
            });

            string rels = new StreamReader(zip.GetEntry("word/_rels/document.xml.rels").Open()).ReadToEnd();
            Assert.That(rels, Does.Contain("relationships/image").And.Contain("media/"), "the picture is related from the document");

            AssertValid(docx);
        }

        [Test]
        public async Task Word_Chart_IsEmbeddedAsAPicture()
        {
            byte[] docx = await RenderDocx("ChartTypes.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            Assert.That(zip.Entries.Count(e => e.FullName.StartsWith("word/media/") && e.FullName.EndsWith(".png")),
                Is.GreaterThan(0), "charts are pictures");

            AssertValid(docx);
        }

        [Test]
        public async Task Word_GroupPageBreaks_SplitTheTable_AndCellsKeepTheirFormatting()
        {
            byte[] docx = await RenderDocx("DocxPageBreakTest.rdl");

            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            XmlDocument doc = LoadXml(zip, "word/document.xml");
            XmlNamespaceManager ns = Namespaces(doc);

            // three regions with a break after each but the last: the table is split twice,
            //   each piece starts on a new page and repeats the table header
            var tables = doc.SelectNodes("/w:document/w:body/w:tbl", ns);
            Assert.That(tables.Count, Is.EqualTo(3), "one table per region");
            Assert.That(doc.SelectNodes("/w:document/w:body/w:p[w:pPr/w:pageBreakBefore]", ns).Count, Is.EqualTo(2), "two page breaks");
            foreach (XmlNode t in tables)
            {
                XmlNode first = t.SelectSingleNode("w:tr[1]", ns);
                Assert.That(first.SelectSingleNode("w:trPr/w:tblHeader", ns), Is.Not.Null, "the header row repeats");
                Assert.That(first.InnerText, Is.EqualTo("MERGED HEADING"));
            }

            // the merged header cell
            XmlNode merged = doc.SelectSingleNode("//w:tc[w:tcPr/w:gridSpan/@w:val='2']", ns);
            Assert.That(merged, Is.Not.Null, "ColSpan becomes gridSpan");
            Assert.Multiple(() =>
            {
                Assert.That(((XmlElement)merged.SelectSingleNode("w:tcPr/w:tcBorders/w:top", ns)).GetAttribute("val", W), Is.EqualTo("single"));
                Assert.That(((XmlElement)merged.SelectSingleNode("w:tcPr/w:tcBorders/w:top", ns)).GetAttribute("color", W), Is.EqualTo("1F4E79"));
                Assert.That(((XmlElement)merged.SelectSingleNode("w:tcPr/w:shd", ns)).GetAttribute("fill", W), Is.EqualTo("DDEBF7"));
                Assert.That(((XmlElement)merged.SelectSingleNode("w:p/w:pPr/w:jc", ns)).GetAttribute("val", W), Is.EqualTo("center"));
            });

            // the title's font, size, italic, underline and colour; and its text escaped
            XmlNode title = doc.SelectSingleNode("/w:document/w:body/w:p[1]", ns);
            Assert.Multiple(() =>
            {
                Assert.That(title.InnerText, Is.EqualTo("DOCX TITLE & <more>"));
                Assert.That(((XmlElement)title.SelectSingleNode(".//w:rPr/w:rFonts", ns)).GetAttribute("ascii", W), Is.EqualTo("Times New Roman"));
                Assert.That(((XmlElement)title.SelectSingleNode(".//w:rPr/w:sz", ns)).GetAttribute("val", W), Is.EqualTo("32"));
                Assert.That(title.SelectSingleNode(".//w:rPr/w:i", ns), Is.Not.Null, "italic");
                Assert.That(((XmlElement)title.SelectSingleNode(".//w:rPr/w:u", ns)).GetAttribute("val", W), Is.EqualTo("single"));
                Assert.That(((XmlElement)title.SelectSingleNode(".//w:rPr/w:color", ns)).GetAttribute("val", W), Is.EqualTo("C00000"));
            });

            // the page header is left off the first page
            XmlNode sect = doc.SelectSingleNode("/w:document/w:body/w:sectPr", ns);
            Assert.That(sect.SelectSingleNode("w:titlePg", ns), Is.Not.Null, "first page differs");
            Assert.That(zip.GetEntry("word/header2.xml"), Is.Not.Null, "an empty first page header");
            Assert.That(LoadXml(zip, "word/header1.xml").DocumentElement.InnerText, Is.EqualTo("Regional amounts"));
            // margins: 0.75in left (1080 twips); the body starts below the 0.3in header (720 + 432)
            var pgMar = (XmlElement)sect.SelectSingleNode("w:pgMar", ns);
            Assert.Multiple(() =>
            {
                Assert.That(pgMar.GetAttribute("left", W), Is.EqualTo("1080"));
                Assert.That(pgMar.GetAttribute("header", W), Is.EqualTo("720"));
                Assert.That(pgMar.GetAttribute("top", W), Is.EqualTo("1152"));
            });

            AssertValid(docx);
        }

        [Test]
        public async Task Word_ListReport_IsValid()
        {
            byte[] docx = await RenderDocx("ListReport.rdl");
            using var zip = new ZipArchive(new MemoryStream(docx), ZipArchiveMode.Read);
            LoadXml(zip, "word/document.xml");
            AssertValid(docx);
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

        private async Task<byte[]> RenderDocx(string reportFile)
        {
            Report rap = await LoadReport(reportFile);
            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.Word);
            byte[] docx = ((MemoryStream)ms.GetStream()).ToArray();
            Assert.That(docx.Length, Is.GreaterThan(0), "docx output is empty");

            // Also write to disk for manual inspection during development
            await File.WriteAllBytesAsync(Path.Combine(_outputFolder.LocalPath,
                Path.GetFileNameWithoutExtension(reportFile) + "_Word.docx"), docx);
            return docx;
        }

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
            ns.AddNamespace("w", W);
            ns.AddNamespace("wp", "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing");
            return ns;
        }

        private static void AssertValid(byte[] docx)
        {
            using var ms = new MemoryStream(docx);
            Assert.That(OpenXmlUtils.ValidateWordprocessingDocument(ms), Is.True);
        }
    }
}
#endif
