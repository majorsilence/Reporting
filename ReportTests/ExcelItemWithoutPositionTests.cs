using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A report item without a Top or Left is at 0, as RDL has it, in Excel output too. Designers leave
    /// them out for an item at the top or left of its container. The Excel renderer left a textbox
    /// without either out of the sheet, and failed on an image or a line without one.
    /// </summary>
    [TestFixture]
    public class ExcelItemWithoutPositionTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            _folder = Path.Combine(Path.GetTempPath(), $"rdl-excel-without-position-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        // A 1x1 PNG.
        private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        private static string Rdl(string items) => $"""
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
              <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
              <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
              <Width>7.5in</Width>
              <EmbeddedImages>
                <EmbeddedImage Name="Dot"><MIMEType>image/png</MIMEType><ImageData>{Png}</ImageData></EmbeddedImage>
              </EmbeddedImages>
              <Body>
                <Height>3in</Height>
                <ReportItems>
                  {items}
                </ReportItems>
              </Body>
            </Report>
            """;

        private async Task<ZipArchive> Excel(string rdl)
        {
            using var report = await new RDLParser(rdl) { Folder = _folder }.Parse();
            report.Folder = _folder;
            await report.RunGetData(null);
            string path = Path.Combine(_folder, "out.xlsx");
            using (var sg = new OneFileStreamGen(path, true))
                await report.RunRender(sg, OutputPresentationType.Excel2007);

            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4),
                report.ErrorItems == null ? "" : string.Join("; ", report.ErrorItems.Cast<object>()));
            return ZipFile.OpenRead(path);
        }

        private static string Entry(ZipArchive zip, string name)
        {
            var entry = zip.GetEntry(name);
            Assert.That(entry, Is.Not.Null, name + " is missing: " + string.Join(",", zip.Entries.Select(e => e.FullName)));
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }

        [Test]
        public async Task TextboxWithoutTopOrLeft_IsInTheSheet()
        {
            using var zip = await Excel(Rdl("""
                <Textbox Name="NoTopNoLeft"><Width>1in</Width><Height>0.25in</Height><Value>NO-TOP-NO-LEFT</Value></Textbox>
                <Textbox Name="NoTop"><Left>2in</Left><Width>1in</Width><Height>0.25in</Height><Value>NO-TOP</Value></Textbox>
                <Textbox Name="NoLeft"><Top>1in</Top><Width>1in</Width><Height>0.25in</Height><Value>NO-LEFT</Value></Textbox>
                <Textbox Name="Both"><Top>1in</Top><Left>2in</Left><Width>1in</Width><Height>0.25in</Height><Value>BOTH</Value></Textbox>
                """));
            string sheet = Entry(zip, "xl/worksheets/sheet1.xml");

            Assert.That(sheet, Does.Contain(">BOTH<"));
            Assert.That(sheet, Does.Contain(">NO-TOP-NO-LEFT<"), "A textbox without Top or Left was left out");
            Assert.That(sheet, Does.Contain(">NO-TOP<"), "A textbox without Top was left out");
            Assert.That(sheet, Does.Contain(">NO-LEFT<"), "A textbox without Left was left out");
        }

        [Test]
        public async Task TextboxWithoutTopOrLeft_IsInTheFirstCell()
        {
            using var zip = await Excel(Rdl("""
                <Textbox Name="NoTopNoLeft"><Width>1in</Width><Height>0.25in</Height><Value>FIRST</Value></Textbox>
                """));
            string sheet = Entry(zip, "xl/worksheets/sheet1.xml");

            Assert.That(sheet, Does.Match("<c r=\"A1\"[^>]*><is><t>FIRST</t>"));
        }

        [Test]
        public async Task ImageAndLineWithoutTopOrLeft_AreDrawn()
        {
            using var zip = await Excel(Rdl("""
                <Image Name="DotImage"><Source>Embedded</Source><Value>Dot</Value><Width>0.5in</Width><Height>0.5in</Height></Image>
                <Line Name="Rule"><Width>3in</Width><Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Line>
                """));
            string drawing = Entry(zip, "xl/drawings/drawing1.xml");

            Assert.That(drawing, Does.Contain("<xdr:pic>"), "The image without Top or Left is missing");
            Assert.That(drawing, Does.Contain("name=\"Line 1\""), "The line without Top or Left is missing");
            Assert.That(drawing, Does.Contain("<xdr:pos x=\"0\" y=\"0\"/>"), "The items should be at 0, 0");
        }
    }
}
