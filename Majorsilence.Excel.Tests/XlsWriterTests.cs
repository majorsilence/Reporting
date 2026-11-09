using System;
using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NUnit.Framework;

namespace Majorsilence.Excel.Tests
{
    /// <summary>The .xls output is read back with NPOI's HSSF reader, an independent BIFF8 implementation.</summary>
    [TestFixture]
    public class XlsWriterTests
    {
        private static HSSFWorkbook Read(XlsWriter writer)
        {
            var ms = new MemoryStream();
            writer.Write(ms);
            ms.Position = 0;
            return new HSSFWorkbook(ms);
        }

        [Test]
        public void TypedCells_RoundTrip()
        {
            var writer = new XlsWriter("Data");
            writer.SetCell(0, 0, "Äpple ünïcode ✓");
            writer.SetCell(0, 1, 1234.5);
            writer.SetCell(0, 2, true);
            writer.SetCell(0, 3, new DateTime(2024, 3, 15, 13, 30, 0));
            writer.SetCell(5, 255, "last column");

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            Assert.That(wb.GetSheetName(0), Is.EqualTo("Data"));
            var row = sheet.GetRow(0);
            Assert.That(row.GetCell(0).StringCellValue, Is.EqualTo("Äpple ünïcode ✓"));
            Assert.That(row.GetCell(1).NumericCellValue, Is.EqualTo(1234.5));
            Assert.That(row.GetCell(2).BooleanCellValue, Is.True);

            var date = row.GetCell(3);
            Assert.That(DateUtil.IsCellDateFormatted(date), Is.True);
            Assert.That(DateUtil.GetJavaDate(date.NumericCellValue), Is.EqualTo(new DateTime(2024, 3, 15, 13, 30, 0)));
            Assert.That(date.CellStyle.GetDataFormatString(), Is.EqualTo(XlsxNumberFormats.ShortDateTime));
            Assert.That(sheet.GetRow(5).GetCell(255).StringCellValue, Is.EqualTo("last column"));
        }

        [Test]
        public void CellFormats_AreWrittenAsFontsFillsBordersAndNumberFormats()
        {
            var header = new XlsxCellFormat
            {
                Bold = true, Italic = true, FontName = "Arial", FontSizePt = 14, FontColor = (192, 0, 0),
                HasBackground = true, BackgroundColor = (221, 235, 247),
                BorderBottom = new XlsxBorderSide { Style = "medium", Color = (0, 0, 255) },
                HorizontalAlign = "center", VerticalAlign = "bottom", WrapText = false,
            };
            var money = new XlsxCellFormat { NumberFormatCode = "#,##0.00 \"USD\"" };
            var builtIn = new XlsxCellFormat { NumberFormatCode = "0.00%" };
            var writer = new XlsWriter("Sheet1");
            writer.SetCell(0, 0, "Header", header);
            writer.SetCell(1, 0, 5.5, money);
            writer.SetCell(2, 0, 0.25, builtIn);
            writer.SetCell(3, 0, "editable", new XlsxCellFormat { Locked = false });

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            var style = (HSSFCellStyle)sheet.GetRow(0).GetCell(0).CellStyle;
            var font = (HSSFFont)style.GetFont(wb);
            Assert.That(font.IsBold, Is.True);
            Assert.That(font.IsItalic, Is.True);
            Assert.That(font.FontName, Is.EqualTo("Arial"));
            Assert.That(font.FontHeightInPoints, Is.EqualTo(14));
            Assert.That(Rgb(wb, font.Color), Is.EqualTo(new byte[] { 192, 0, 0 }));
            Assert.That(style.FillPattern, Is.EqualTo(FillPattern.SolidForeground));
            Assert.That(Rgb(wb, style.FillForegroundColor), Is.EqualTo(new byte[] { 221, 235, 247 }));
            Assert.That(style.BorderBottom, Is.EqualTo(BorderStyle.Medium));
            Assert.That(Rgb(wb, style.BottomBorderColor), Is.EqualTo(new byte[] { 0, 0, 255 }));
            Assert.That(style.BorderTop, Is.EqualTo(BorderStyle.None));
            Assert.That(style.Alignment, Is.EqualTo(HorizontalAlignment.Center));
            Assert.That(style.VerticalAlignment, Is.EqualTo(VerticalAlignment.Bottom));

            Assert.That(sheet.GetRow(1).GetCell(0).CellStyle.GetDataFormatString(), Is.EqualTo("#,##0.00 \"USD\""));
            Assert.That(sheet.GetRow(2).GetCell(0).CellStyle.DataFormat, Is.EqualTo(10), "a built-in format uses its built-in id");
            Assert.That(sheet.GetRow(3).GetCell(0).CellStyle.IsLocked, Is.False);
            Assert.That(sheet.GetRow(1).GetCell(0).CellStyle.IsLocked, Is.True);
        }

        [Test]
        public void EqualFormats_ShareOneXf()
        {
            var writer = new XlsWriter("Sheet1");
            writer.SetCell(0, 0, "a", new XlsxCellFormat { Bold = true });
            writer.SetCell(1, 0, "b", new XlsxCellFormat { Bold = true });
            writer.SetCell(2, 0, "c", new XlsxCellFormat { Italic = true });

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            short xf(int r) => sheet.GetRow(r).GetCell(0).CellStyle.Index;
            Assert.That(xf(0), Is.EqualTo(xf(1)));
            Assert.That(xf(2), Is.Not.EqualTo(xf(0)));
        }

        [Test]
        public void Layout_MergesWidthsHeightsHiddenOutlinePanesAndPrintSetup()
        {
            var writer = new XlsWriter("Sheet1");
            writer.SetCell(0, 0, "Title");
            writer.AddMerge(0, 0, 1, 3);
            writer.SetColumnWidth(0, 25);
            writer.SetRowHeight(2, 30);
            writer.SetColumnHidden(5, true);
            writer.SetRowHidden(4, true);
            writer.SetRowOutlineLevel(6, 2);
            writer.SetCell(6, 0, 1.0);
            writer.FreezePanes(1, 0);
            writer.SetPrintSetup(0.5, 0.6, 0.7, 0.8, landscape: true);

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            var merge = sheet.GetMergedRegion(0);
            Assert.That((merge.FirstRow, merge.FirstColumn, merge.LastRow, merge.LastColumn), Is.EqualTo((0, 0, 1, 3)));
            Assert.That(sheet.GetColumnWidth(0), Is.EqualTo(25 * 256));
            Assert.That(sheet.GetRow(2).HeightInPoints, Is.EqualTo(30));
            Assert.That(sheet.IsColumnHidden(5), Is.True);
            Assert.That(sheet.GetRow(4).ZeroHeight, Is.True);
            Assert.That(sheet.GetRow(6).OutlineLevel, Is.EqualTo(2));
            Assert.That(sheet.PaneInformation.IsFreezePane(), Is.True);
            Assert.That(sheet.PaneInformation.HorizontalSplitTopRow, Is.EqualTo(1));
            Assert.That(sheet.PrintSetup.Landscape, Is.True);
            Assert.That(sheet.GetMargin(MarginType.LeftMargin), Is.EqualTo(0.5));
            Assert.That(sheet.GetMargin(MarginType.BottomMargin), Is.EqualTo(0.8));
        }

        [Test]
        public void MultipleSheets_GetSanitisedUniqueNames()
        {
            var writer = new XlsWriter("Report: Q1/Q2 [final] with a very long name indeed");
            int second = writer.AddSheet("Report: Q1/Q2 [final] with a very long name indeed");
            writer.SetCell(0, 0, "first");
            writer.SetCell(0, 0, "second", sheetIndex: second);

            using var wb = Read(writer);
            Assert.That(wb.NumberOfSheets, Is.EqualTo(2));
            string a = wb.GetSheetName(0), b = wb.GetSheetName(1);
            Assert.That(a, Is.EqualTo("Report_ Q1_Q2 _final_ with a ve"));
            Assert.That(b, Has.Length.LessThanOrEqualTo(31).And.EndsWith("(2)"));
            Assert.That(wb.GetSheetAt(1).GetRow(0).GetCell(0).StringCellValue, Is.EqualTo("second"));
        }

        [Test]
        public void ManyAndLongStrings_SplitAcrossContinueRecords()
        {
            var writer = new XlsWriter("Sheet1");
            string longText = string.Concat(Enumerable.Range(0, 2000).Select(i => "abcde"));   // 10,000 chars, > one record
            writer.SetCell(0, 0, longText);
            for (int r = 1; r <= 3000; r++)
                writer.SetCell(r, 0, "string number " + r + " " + new string('x', r % 50));
            writer.SetCell(3001, 0, "string number 7 " + new string('x', 7)); // a repeat shares the SST entry

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            Assert.That(sheet.GetRow(0).GetCell(0).StringCellValue, Is.EqualTo(longText));
            for (int r = 1; r <= 3000; r += 37)
                Assert.That(sheet.GetRow(r).GetCell(0).StringCellValue, Is.EqualTo("string number " + r + " " + new string('x', r % 50)));
            Assert.That(sheet.GetRow(3001).GetCell(0).StringCellValue, Is.EqualTo(sheet.GetRow(7).GetCell(0).StringCellValue));
        }

        [Test]
        public void MoreColoursThanThePalette_MapToTheNearest()
        {
            var writer = new XlsWriter("Sheet1");
            for (int i = 0; i < 80; i++)
                writer.SetCell(i, 0, "c", new XlsxCellFormat { HasBackground = true, BackgroundColor = ((byte)(i * 3), (byte)(255 - i * 3), 17) });

            using var wb = Read(writer);
            var sheet = wb.GetSheetAt(0);
            var first = Rgb(wb, sheet.GetRow(0).GetCell(0).CellStyle.FillForegroundColor);
            Assert.That(first, Is.EqualTo(new byte[] { 0, 255, 17 }));
            var last = Rgb(wb, sheet.GetRow(79).GetCell(0).CellStyle.FillForegroundColor);
            Assert.That(last, Is.EqualTo(new byte[] { 162, 93, 17 }),
                "black text keeps one slot, the other 55 take the first 55 fills; (237,18,17) is nearest the 55th");
        }

        [Test]
        public void RowsAndColumnsBeyondBiff8Limits_Throw()
        {
            var writer = new XlsWriter("Sheet1");
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.SetCell(XlsWriter.MaxRows, 0, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.SetCell(0, XlsWriter.MaxColumns, "x"));
            Assert.DoesNotThrow(() => writer.SetCell(XlsWriter.MaxRows - 1, XlsWriter.MaxColumns - 1, "x"));
        }

        [Test]
        public void EmptyWorkbook_IsReadable()
        {
            using var wb = Read(new XlsWriter("Empty"));
            Assert.That(wb.GetSheetAt(0).PhysicalNumberOfRows, Is.EqualTo(0));
        }

        [TestCase(10)]
        [TestCase(4096)]
        [TestCase(100_000)]
        [TestCase(8 * 1024 * 1024)]   // over 109 FAT sectors, so DIFAT sectors are needed
        public void CompoundFile_StreamReadsBackExactly(int length)
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);
            var ms = new MemoryStream();
            Majorsilence.Excel.Xls.CompoundFileWriter.Write(ms, "Workbook", data);
            Assert.That(ms.Length % 512, Is.EqualTo(0));

            ms.Position = 0;
            var fs = new POIFSFileSystem(ms);
            using var stream = fs.CreateDocumentInputStream("Workbook");
            var read = new byte[length];
            int total = 0, n;
            while (total < length && (n = stream.Read(read, total, length - total)) > 0) total += n;
            Assert.That(total, Is.EqualTo(length));
            Assert.That(read, Is.EqualTo(data));
        }

        private static byte[] Rgb(HSSFWorkbook wb, short index) => wb.GetCustomPalette().GetColor(index).RGB;
    }
}
