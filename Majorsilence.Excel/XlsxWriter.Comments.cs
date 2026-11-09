using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace Majorsilence.Excel
{
    public partial class XlsxWriter
    {
        /// <summary>Attaches a note to a cell, shown when the cell is hovered. A cell has at most one; a later call replaces it.</summary>
        public void AddComment(int rowIndex, int colIndex, string text, string? author = null, int sheetIndex = 0)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var comments = _sheets[sheetIndex].Comments;
            comments.RemoveAll(c => c.Row == rowIndex && c.Col == colIndex);
            comments.Add(new CommentEntry(rowIndex, colIndex, text, author ?? ""));
        }

        // The comments relationships come right after the drawing (rId1) and external hyperlinks.
        private static int CommentsFirstRelId(Sheet sheet, bool hasDrawings) =>
            (hasDrawings ? 1 : 0) + sheet.ExternalHyperlinks.Count + 1;

        private static int AppendCommentRels(StringBuilder sb, int sheetNum, Sheet sheet, int rid)
        {
            if (sheet.Comments.Count == 0) return rid;
            sb.Append($"<Relationship Id=\"rId{rid++}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/vmlDrawing\" Target=\"../drawings/vmlDrawing{sheetNum}.vml\"/>");
            sb.Append($"<Relationship Id=\"rId{rid++}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments\" Target=\"../comments{sheetNum}.xml\"/>");
            return rid;
        }

        private static void AppendLegacyDrawing(StringBuilder sb, Sheet sheet, bool hasDrawings)
        {
            if (sheet.Comments.Count > 0)
                sb.Append($"<legacyDrawing r:id=\"rId{CommentsFirstRelId(sheet, hasDrawings)}\"/>");
        }

        private void AppendCommentContentTypes(StringBuilder sb)
        {
            bool any = false;
            for (int s = 0; s < _sheets.Count; s++)
            {
                if (_sheets[s].Comments.Count == 0) continue;
                any = true;
                sb.Append($"<Override PartName=\"/xl/comments{s + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml\"/>");
            }
            if (any)
                sb.Append("<Default Extension=\"vml\" ContentType=\"application/vnd.openxmlformats-officedocument.vmlDrawing\"/>");
        }

        private static void WriteCommentParts(ZipArchive zip, int sheetNum, Sheet sheet)
        {
            if (sheet.Comments.Count == 0) return;
            var comments = new List<CommentEntry>(sheet.Comments);
            comments.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Col.CompareTo(b.Col));
            WriteText(zip, $"xl/comments{sheetNum}.xml", BuildComments(comments));
            WriteText(zip, $"xl/drawings/vmlDrawing{sheetNum}.vml", BuildCommentVml(sheetNum, comments));
        }

        private static string BuildComments(List<CommentEntry> comments)
        {
            var authors = new List<string>();
            foreach (var c in comments)
                if (!authors.Contains(c.Author)) authors.Add(c.Author);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<comments xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><authors>");
            foreach (var a in authors)
                sb.Append($"<author>{XmlEscape(a)}</author>");
            sb.Append("</authors><commentList>");
            foreach (var c in comments)
            {
                sb.Append($"<comment ref=\"{ColRef(c.Col)}{c.Row + 1}\" authorId=\"{authors.IndexOf(c.Author)}\"><text>");
                if (c.Author.Length > 0)
                    sb.Append($"<r><rPr><rFont val=\"Tahoma\"/><b/><sz val=\"9\"/></rPr><t xml:space=\"preserve\">{XmlEscape(c.Author)}:\n</t></r>");
                sb.Append($"<r><rPr><rFont val=\"Tahoma\"/><sz val=\"9\"/></rPr><t xml:space=\"preserve\">{XmlEscape(c.Text)}</t></r>");
                sb.Append("</text></comment>");
            }
            sb.Append("</commentList></comments>");
            return sb.ToString();
        }

        // Excel draws the note indicator and popup from this legacy VML part, not from comments.xml.
        private static string BuildCommentVml(int sheetNum, List<CommentEntry> comments)
        {
            var sb = new StringBuilder();
            sb.Append("<xml xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\">");
            sb.Append($"<o:shapelayout v:ext=\"edit\"><o:idmap v:ext=\"edit\" data=\"{sheetNum}\"/></o:shapelayout>");
            sb.Append("<v:shapetype id=\"_x0000_t202\" coordsize=\"21600,21600\" o:spt=\"202\" path=\"m,l,21600r21600,l21600,xe\">");
            sb.Append("<v:stroke joinstyle=\"miter\"/><v:path gradientshapeok=\"t\" o:connecttype=\"rect\"/></v:shapetype>");
            for (int i = 0; i < comments.Count; i++)
            {
                var c = comments[i];
                int shapeId = sheetNum * 1024 + i + 1;
                sb.Append($"<v:shape id=\"_x0000_s{shapeId}\" type=\"#_x0000_t202\" ");
                sb.Append("style=\"position:absolute;margin-left:59.25pt;margin-top:1.5pt;width:108pt;height:59.25pt;z-index:")
                  .Append(i + 1).Append(";visibility:hidden\" fillcolor=\"#ffffe1\" o:insetmode=\"auto\">");
                sb.Append("<v:fill color2=\"#ffffe1\"/><v:shadow on=\"t\" color=\"black\" obscured=\"t\"/><v:path o:connecttype=\"none\"/>");
                sb.Append("<v:textbox style=\"mso-direction-alt:auto\"><div style=\"text-align:left\"></div></v:textbox>");
                sb.Append("<x:ClientData ObjectType=\"Note\"><x:MoveWithCells/><x:SizeWithCells/>");
                sb.Append($"<x:Anchor>{c.Col + 1}, 15, {Math.Max(0, c.Row - 1)}, 10, {c.Col + 3}, 15, {c.Row + 3}, 4</x:Anchor>");
                sb.Append($"<x:AutoFill>False</x:AutoFill><x:Row>{c.Row}</x:Row><x:Column>{c.Col}</x:Column></x:ClientData>");
                sb.Append("</v:shape>");
            }
            sb.Append("</xml>");
            return sb.ToString();
        }

        private class CommentEntry
        {
            public readonly int Row, Col;
            public readonly string Text, Author;
            public CommentEntry(int row, int col, string text, string author) { Row = row; Col = col; Text = text; Author = author; }
        }

        private partial class Sheet
        {
            public readonly List<CommentEntry> Comments = new List<CommentEntry>();
        }
    }
}
