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
    public class SheetProtectionTests
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

        [Test]
        public void DefaultProtection_BlocksEverythingButSelecting()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "locked");
            writer.ProtectSheet();

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var p = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetProtection>().Single();
            Assert.That(p.Sheet!.Value, Is.True);
            Assert.That(p.Objects!.Value, Is.True);
            Assert.That(p.FormatCells, Is.Null, "absent means blocked");
            Assert.That(p.SelectLockedCells, Is.Null, "absent means selecting is allowed");
            Assert.That(p.HashValue, Is.Null);
        }

        [Test]
        public void AllowedActions_AreWrittenAsUnblocked()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetAutoFilter(0, 0, 3, 1);
            writer.ProtectSheet(new XlsxSheetProtection
            {
                AllowFormatCells = true,
                AllowSort = true,
                AllowAutoFilter = true,
                AllowInsertRows = true,
                AllowSelectLockedCells = false,
            });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var p = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetProtection>().Single();
            Assert.That(p.FormatCells!.Value, Is.False);
            Assert.That(p.Sort!.Value, Is.False);
            Assert.That(p.AutoFilter!.Value, Is.False);
            Assert.That(p.InsertRows!.Value, Is.False);
            Assert.That(p.SelectLockedCells!.Value, Is.True);
        }

        [Test]
        public void Password_IsStoredAsSaltedSha512Hash()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.ProtectSheet(new XlsxSheetProtection { Password = "secret" });

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var p = doc.WorkbookPart!.WorksheetParts.Single().Worksheet.Elements<SheetProtection>().Single();
            Assert.That(p.AlgorithmName!.Value, Is.EqualTo("SHA-512"));
            Assert.That(p.SpinCount!.Value, Is.EqualTo(100000u));

            byte[] salt = Convert.FromBase64String(p.SaltValue!.Value!);
            Assert.That(salt, Has.Length.EqualTo(16));
            string expected = Convert.ToBase64String(XlsxWriterProtectionHash("secret", salt));
            Assert.That(p.HashValue!.Value, Is.EqualTo(expected));
        }

        [Test]
        public void UnlockedAndHiddenCellFormats_WriteProtectionOnTheXf()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(0, 0, "editable", new XlsxCellFormat { Locked = false });
            writer.SetFormula(1, 0, "1+1", 2, new XlsxCellFormat { FormulaHidden = true });
            writer.SetCell(2, 0, "plain", new XlsxCellFormat());
            writer.ProtectSheet();

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var xfs = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet.CellFormats!.Elements<CellFormat>().ToList();
            var cells = doc.WorkbookPart.WorksheetParts.Single().Worksheet.Descendants<Cell>().ToList();
            CellFormat XfOf(int i) => xfs[(int)(cells[i].StyleIndex?.Value ?? 0)];

            Assert.That(XfOf(0).Protection!.Locked!.Value, Is.False);
            Assert.That(XfOf(1).Protection!.Hidden!.Value, Is.True);
            Assert.That(XfOf(2).Protection, Is.Null, "a default (locked) format needs no protection element");
        }

        // Independent re-implementation of ISO/IEC 29500 sheet protection hashing.
        private static byte[] XlsxWriterProtectionHash(string password, byte[] salt)
        {
            using var sha = System.Security.Cryptography.SHA512.Create();
            byte[] hash = sha.ComputeHash(salt.Concat(System.Text.Encoding.Unicode.GetBytes(password)).ToArray());
            for (int i = 0; i < 100000; i++)
                hash = sha.ComputeHash(hash.Concat(BitConverter.GetBytes(i)).ToArray());
            return hash;
        }
    }
}
