#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Expression-language gaps found by rendering real-world reports: a string literal
    /// ending in a backslash, the Like operator, and the Picture and RoundUp functions.
    /// Each of these failed at parse time, which is fatal for the whole report rather
    /// than for the one expression, so they are asserted through a real render.
    /// </summary>
    [TestFixture]
    public class ExpressionGapsTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<(Report Report, string Html)> Render(string reportFile)
        {
            Uri fileRdlUri = new Uri(_reportFolder, reportFile);
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            // Parsed here rather than through RdlUtils because a Subreport is resolved
            // during the parse's final pass, so the folder has to be known before it.
            var rdlp = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath))
            {
                Folder = _reportFolder.LocalPath
            };
            Report report = await rdlp.Parse();
            Assert.That(report, Is.Not.Null, $"Report '{reportFile}' failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return (report, ms.GetText());
        }

        [Test]
        public async Task ExpressionGaps_ParseAndEvaluate()
        {
            var (report, html) = await Render("ExpressionGapsTest.rdl");

            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                "One of these expressions failed to parse, which fails the whole report.");

            // A backslash is an ordinary character in a VB string literal. Lexing it as an
            // escape swallowed the closing quote and reported an unterminated string.
            Assert.That(html, Does.Contain(@"PATH:C:\Temp\"),
                "A string literal ending in a backslash should keep the backslash.");

            // Like: "*" spans characters, and the whole string has to match.
            Assert.That(html, Does.Contain("LIKEMATCH/LIKEMISS"),
                "Like should match on the prefix pattern and reject the non-matching one.");
            Assert.That(html, Does.Contain("CLASSMATCH"),
                "Like should support the character-class and digit forms of the pattern.");

            // Picture fills each "x" from the text and copies everything else through.
            Assert.That(html, Does.Contain("PIC:20-25-08-24"),
                "Picture should substitute the text into the template's placeholders.");

            // RoundUp(102.3) is 103; the two-argument form rounds up at a decimal place
            // rather than to a multiple, so RoundUp(12.34, 1) lands in (12.35, 12.45).
            Assert.That(html, Does.Contain("UP:103/DEC"),
                "RoundUp should round up to a whole number and to a decimal place.");
        }

        [Test]
        public async Task NullableSubreportParameter_RendersInsteadOfFailing()
        {
            // The parent relays its own unsupplied parameter, so the subreport's parameter
            // is set to null. Before Nullable was honoured this threw converting null to
            // the declared numeric type, and the throw happened inside the error path
            // itself, so the whole render died with a bare NullReferenceException.
            var (report, html) = await Render("NullableParameterTest.rdl");

            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8));
            Assert.That(html, Does.Contain("PARENTRENDERED"));
            Assert.That(html, Does.Contain("SUBRENDERED"),
                "The subreport should still render when its nullable parameter arrives empty.");
        }
    }
}
#endif
