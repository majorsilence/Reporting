using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;

namespace ReportTests
{
    [TestFixture]
    public class PageTextHtmlLexerTests
    {
        private static string[] Lex(string html) =>
            new PageTextHtmlLexer(html).Lex().Select(t => t.Replace(PageTextHtmlLexer.HTMLCMD, '^')).ToArray();

        // A '<' within the last three characters used to rewind the reader past the previous token:
        // text ending in "><" lexed forever and "<b" at the end threw.
        [TestCase("a/><", new[] { "a/", ">", "^<" })]
        [TestCase("x><", new[] { "x", ">", "^<" })]
        [TestCase("<b", new[] { "^<b" })]
        [TestCase("<", new[] { "^<" })]
        public void TagAtEndOfText(string html, string[] expected)
        {
            Assert.That(Lex(html), Is.EqualTo(expected.Append(PageTextHtmlLexer.EOF)));
        }

        [Test]
        public void WordsTagsEntitiesAndComments()
        {
            Assert.That(Lex("<b>a&amp;b</b> <!-- note --><q>c</q>&lt;"),
                Is.EqualTo(new[] { "^<b>", "a&b", "^</b>", PageTextHtmlLexer.WHITESPACE, "^<cmnt>", "\"", "c", "\"", "<", PageTextHtmlLexer.EOF }));
        }
    }
}
