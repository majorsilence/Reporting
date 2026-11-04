#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The HTML renderer lays the body (and each rectangle) out as a CSS grid built from the items'
    /// design edges, with rows that grow with their content. Before this, every body item was
    /// absolutely positioned at its design Top, so the table below a table that grew was drawn
    /// across the grown rows (List Invoices: the clinic totals over the invoice lines).
    /// </summary>
    [TestFixture]
    public class HtmlGridLayoutTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<string> Html()
        {
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, "ZIndexPushDownTest.rdl"));
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        private static string RuleFor(string html, string id)
        {
            var m = Regex.Match(html, @"table#" + id + @"\s*\{([^}]*)\}");
            Assert.That(m.Success, Is.True, $"no css rule for table#{id}");
            return m.Groups[1].Value;
        }

        [Test]
        public async Task BodyItems_ArePlacedOnAGrid_NotAbsolutelyPositioned()
        {
            string html = await Html();

            Assert.That(html, Does.Match(@"<div style=""POSITION: relative; display: grid;[^""]*grid-template-rows:[^""]*minmax\("), "the body is a grid whose rows can grow");

            // The two tables are the only table# rules with a placement (cells have none).
            var ids = Regex.Matches(html, @"<table id='(css\d+)'");
            Assert.That(ids.Count, Is.EqualTo(2), "an upper and a lower table");
            string upper = RuleFor(html, ids[0].Groups[1].Value);
            string lower = RuleFor(html, ids[1].Groups[1].Value);
            Assert.That(upper, Does.Not.Contain("position: absolute"));
            Assert.That(lower, Does.Not.Contain("position: absolute"));

            int Row(string rule) => int.Parse(Regex.Match(rule, @"grid-row: (\d+) /").Groups[1].Value);
            int RowEnd(string rule) => int.Parse(Regex.Match(rule, @"grid-row: \d+ / (\d+)").Groups[1].Value);
            Assert.That(Row(lower), Is.GreaterThanOrEqualTo(RowEnd(upper)), "the lower table starts on a grid row after the upper table's rows, so it moves down as they grow");
        }
    }
}
#endif
