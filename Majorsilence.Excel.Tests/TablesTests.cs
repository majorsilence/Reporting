using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using NUnit.Framework;

namespace Majorsilence.Excel.Tests
{
    [TestFixture]
    public class TablesTests
    {
        private static SpreadsheetDocument WriteAndOpen(XlsxWriter writer, MemoryStream ms)
        {
            writer.Write(ms);
            ms.Position = 0;
            using (var check = SpreadsheetDocument.Open(ms, false))
            {
                var errors = new OpenXmlValidator().Validate(check).ToList();
                Assert.That(errors, Is.Empty,
                    string.Join(Environment.NewLine, errors.Select(e => $"{e.Description} ({e.Path?.XPath})")));
            }
            ms.Position = 0;
            return SpreadsheetDocument.Open(ms, false);
        }

        private static XlsxWriter WithData()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "Name");
            writer.SetCell(0, 1, "Amount");
            writer.SetCell(1, 0, "Apple");
            writer.SetCell(1, 1, 3.0);
            writer.SetCell(2, 0, "Pear");
            writer.SetCell(2, 1, 5.0);
            return writer;
        }

        [Test]
        public void Table_WritesPartWithColumnsFromHeaderAndStyle()
        {
            var writer = WithData();
            string name = writer.AddTable(0, 0, 2, 1, "Fruit", "TableStyleLight9");

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var sheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            var table = sheetPart.TableDefinitionParts.Single().Table;

            Assert.That(name, Is.EqualTo("Fruit"));
            Assert.That(table.Name!.Value, Is.EqualTo("Fruit"));
            Assert.That(table.Reference!.Value, Is.EqualTo("A1:B3"));
            Assert.That(table.TableColumns!.Elements<TableColumn>().Select(c => c.Name!.Value), Is.EqualTo(new[] { "Name", "Amount" }));
            Assert.That(table.TableStyleInfo!.Name!.Value, Is.EqualTo("TableStyleLight9"));
            Assert.That(table.TableStyleInfo.ShowRowStripes!.Value, Is.True);
            Assert.That(sheetPart.Worksheet.Elements<TableParts>().Single().Count!.Value, Is.EqualTo(1u));
        }

        [Test]
        public void MissingNumericAndDuplicateHeaders_BecomeUniqueTextHeaders()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "Qty");
            writer.SetCell(0, 1, 2024.0);
            writer.SetCell(0, 3, "Qty");
            writer.SetCell(1, 0, 1.0);

            string name = writer.AddTable(0, 0, 1, 3);

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var sheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            var columns = sheetPart.TableDefinitionParts.Single().Table.TableColumns!.Elements<TableColumn>().Select(c => c.Name!.Value).ToList();
            Assert.That(name, Is.EqualTo("Table1"));
            Assert.That(columns, Is.EqualTo(new[] { "Qty", "2024", "Column3", "Qty2" }));

            var headerCells = sheetPart.Worksheet.Descendants<Row>().First().Elements<Cell>().ToList();
            Assert.That(headerCells.Select(c => c.InnerText), Is.EqualTo(columns), "header cells hold the column names as text");
            Assert.That(headerCells.All(c => c.DataType!.Value == CellValues.InlineString), Is.True);
        }

        [Test]
        public void SharedStringHeaders_AreKept()
        {
            var writer = WithData();
            writer.UseSharedStrings = true;
            writer.SetCell(0, 0, "Name");
            writer.AddTable(0, 0, 2, 1);

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var columns = doc.WorkbookPart!.WorksheetParts.Single().TableDefinitionParts.Single().Table.TableColumns!
                .Elements<TableColumn>().Select(c => c.Name!.Value);
            Assert.That(columns, Is.EqualTo(new[] { "Name", "Amount" }));
        }

        [Test]
        public void TablesOnTwoSheets_WithCommentsHyperlinksAndImages_AreAllValid()
        {
            var writer = WithData();
            int second = writer.AddSheet("Second");
            writer.SetCell(0, 0, "H", sheetIndex: second);
            writer.SetCell(1, 0, "v", sheetIndex: second);
            writer.AddImage(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }, 0, 0, 100, 100);
            writer.AddHyperlink(1, 0, "https://example.com");
            writer.AddComment(2, 0, "note");
            writer.AddTable(0, 0, 2, 1);
            writer.AddTable(0, 0, 1, 0, sheetIndex: second);

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var ids = doc.WorkbookPart!.WorksheetParts.SelectMany(p => p.TableDefinitionParts).Select(t => t.Table.Id!.Value).OrderBy(i => i);
            Assert.That(ids, Is.EqualTo(new[] { 1u, 2u }));
        }

        [TestCase("A1")]
        [TestCase("R1C1")]
        [TestCase("has space")]
        [TestCase("1st")]
        public void InvalidNames_Throw(string name)
        {
            Assert.Throws<ArgumentException>(() => WithData().AddTable(0, 0, 2, 1, name));
        }

        [Test]
        public void DuplicateNamesAndOverlaps_Throw()
        {
            var writer = WithData();
            writer.AddTable(0, 0, 2, 1, "Data");
            Assert.Throws<ArgumentException>(() => writer.AddTable(5, 0, 6, 1, "data"));
            Assert.Throws<ArgumentException>(() => writer.AddTable(1, 1, 4, 2));
        }
    }
}
