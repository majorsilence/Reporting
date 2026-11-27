#if NET8_0_OR_GREATER
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// RdlCmd's /d switch starts html and mht output with the table of contents built from the
    /// document map (Report.HtmlDocumentMap, issue #637); without it the output has none.
    /// </summary>
    [TestFixture]
    [NonParallelizable]     // RdlCmd writes to the console
    public class RdlCmdDocumentMapTests
    {
        private string _folder;

        private const string Rdl = """
            <?xml version="1.0" encoding="utf-8"?>
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition">
              <Width>400pt</Width>
              <Body><Height>100pt</Height><ReportItems>
                <Textbox Name="A"><Top>0pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
                  <Value>Chapter text</Value><Label>Chapter One</Label></Textbox>
              </ReportItems></Body>
              <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
            </Report>
            """;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), $"rdlcmd-docmap-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "Labelled.rdl"), Rdl);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_folder, recursive: true); }
            catch (IOException) { }
        }

        private async Task<string> Run(string type, params string[] extra)
        {
            string[] args = new[] { "/f" + Path.Combine(_folder, "Labelled.rdl"), "/t" + type, "/o" + _folder };
            int rc = await Majorsilence.Reporting.RdlCmd.RdlCmd.Main([.. args, .. extra]);
            Assert.That(rc, Is.EqualTo(0), "RdlCmd return code");
            string output = File.ReadAllText(Path.Combine(_folder, "Labelled." + type));
            return type == "mht" ? QuotedPrintable(output) : output;
        }

        // The html part of an mht file is quoted-printable: lines end in a soft break ('=') and
        // some characters are written as =XX.
        private static string QuotedPrintable(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s.Replace("=\r\n", "").Replace("=\n", ""),
                "=([0-9A-F]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());

        [TestCase("html")]
        [TestCase("mht")]
        public async Task WithD_TheOutputHasTheTableOfContents(string type)
        {
            string output = await Run(type, "/d");
            Assert.That(output, Does.Contain("documentmap"));
            Assert.That(output, Does.Contain("Chapter One"));
        }

        [TestCase("html")]
        [TestCase("mht")]
        public async Task WithoutD_TheOutputHasNone(string type)
        {
            string output = await Run(type);
            Assert.That(output, Does.Not.Contain("documentmap"));
        }

        [Test]
        public async Task DashD_IsTheSameSwitch()
        {
            string output = await Run("html", "-d");
            Assert.That(output, Does.Contain("documentmap"));
        }
    }
}
#endif
