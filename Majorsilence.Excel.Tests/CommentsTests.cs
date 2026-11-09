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
    public class CommentsTests
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
        public void Comment_WritesCommentsPartVmlAndLegacyDrawing()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.SetCell(1, 2, 42.0);
            writer.AddComment(1, 2, "Checked by finance", "Ann");

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var sheetPart = doc.WorkbookPart!.WorksheetParts.Single();

            var comment = sheetPart.WorksheetCommentsPart!.Comments.CommentList!.Elements<Comment>().Single();
            Assert.That(comment.Reference!.Value, Is.EqualTo("C2"));
            Assert.That(comment.CommentText!.InnerText, Does.Contain("Checked by finance"));
            Assert.That(sheetPart.WorksheetCommentsPart.Comments.Authors!.Elements<Author>().Single().Text, Is.EqualTo("Ann"));

            Assert.That(sheetPart.VmlDrawingParts.Count(), Is.EqualTo(1));
            var legacy = sheetPart.Worksheet.Elements<LegacyDrawing>().Single();
            Assert.That(sheetPart.GetPartById(legacy.Id!.Value!), Is.InstanceOf<VmlDrawingPart>());

            using var vml = new StreamReader(sheetPart.VmlDrawingParts.Single().GetStream());
            string vmlText = vml.ReadToEnd();
            Assert.That(vmlText, Does.Contain("<x:Row>1</x:Row><x:Column>2</x:Column>"));
        }

        [Test]
        public void CommentsAlongsideDrawingsAndHyperlinks_GetDistinctRelationshipIds()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.AddImage(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }, 0, 0, 100, 100);
            writer.SetCell(0, 0, "link");
            writer.AddHyperlink(0, 0, "https://example.com");
            writer.AddComment(0, 0, "one");
            writer.AddComment(3, 1, "two", "Bob");

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var sheetPart = doc.WorkbookPart!.WorksheetParts.Single();
            Assert.That(sheetPart.DrawingsPart, Is.Not.Null);
            Assert.That(sheetPart.HyperlinkRelationships.Single().Uri.ToString(), Is.EqualTo("https://example.com/"));
            Assert.That(sheetPart.WorksheetCommentsPart!.Comments.CommentList!.Count(), Is.EqualTo(2));
            var authors = sheetPart.WorksheetCommentsPart.Comments.Authors!.Elements<Author>().Select(a => a.Text).ToList();
            Assert.That(authors, Is.EqualTo(new[] { "", "Bob" }));
        }

        [Test]
        public void CommentsOnTwoSheets_EachGetTheirOwnParts()
        {
            var writer = new XlsxWriter("A");
            int b = writer.AddSheet("B");
            writer.AddComment(0, 0, "on A");
            writer.AddComment(0, 0, "on B", sheetIndex: b);

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var texts = doc.WorkbookPart!.WorksheetParts
                .Select(p => p.WorksheetCommentsPart!.Comments.CommentList!.Elements<Comment>().Single().CommentText!.InnerText)
                .OrderBy(t => t).ToList();
            Assert.That(texts, Is.EqualTo(new[] { "on A", "on B" }));
        }

        [Test]
        public void SecondCommentOnACell_ReplacesTheFirst()
        {
            var writer = new XlsxWriter("Sheet1");
            writer.AddComment(0, 0, "old");
            writer.AddComment(0, 0, "new");

            using var ms = new MemoryStream();
            using var doc = WriteAndOpen(writer, ms);
            var comment = doc.WorkbookPart!.WorksheetParts.Single().WorksheetCommentsPart!.Comments.CommentList!.Elements<Comment>().Single();
            Assert.That(comment.CommentText!.InnerText, Is.EqualTo("new"));
        }
    }
}
