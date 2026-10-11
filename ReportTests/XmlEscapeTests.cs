#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Text;

namespace ReportTests
{
    /// <summary>
    /// XmlAnsi / HtmlAnsi return their input untouched when nothing needs escaping. They must still give exactly
    /// what the character-by-character originals gave, which are kept here as the reference.
    /// </summary>
    [TestFixture]
    public class XmlEscapeTests
    {
        private static string ReferenceXml(string s)
        {
            var rs = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c == '<') rs.Append("&lt;");
                else if (c == '&') rs.Append("&amp;");
                else if ((int)c <= 127) rs.Append(c);
                else rs.Append("&#" + ((int)c).ToString() + ";");
            }
            return rs.ToString();
        }

        private static string ReferenceHtml(string s)
        {
            var rs = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if ((int)c <= 127) rs.Append(c);
                else rs.Append("&#" + ((int)c).ToString() + ";");
            }
            return rs.ToString();
        }

        private static readonly string[] Samples =
        {
            "", "a", "plain text", "Total: 1,234.50", "a < b", "a & b", "<", "&", "x<y&z", "café", "é", "abécd",
            "snowman ☃ and <tag> & more", "😀 emoji", "tab\tnew\nline\r", "end<", "&end", "123456789012345678901234567890",
            "\u0000nul", "\u007f", "\u0080",
        };

        [Test]
        public void XmlAnsi_MatchesTheOriginal()
        {
            foreach (string s in Samples)
                Assert.That(XmlUtil.XmlAnsi(s), Is.EqualTo(ReferenceXml(s)), "input: " + s);
        }

        [Test]
        public void HtmlAnsi_MatchesTheOriginal()
        {
            foreach (string s in Samples)
                Assert.That(XmlUtil.HtmlAnsi(s), Is.EqualTo(ReferenceHtml(s)), "input: " + s);
        }

        [Test]
        public void PlainText_IsReturnedAsTheSameString()
        {
            string s = new string('x', 40);
            Assert.That(ReferenceEquals(XmlUtil.XmlAnsi(s), s), Is.True);
            Assert.That(ReferenceEquals(XmlUtil.HtmlAnsi(s), s), Is.True);
        }
    }
}
#endif
