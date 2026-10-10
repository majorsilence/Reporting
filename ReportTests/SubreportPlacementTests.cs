#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A subreport is placed by its own Top and by its container's offset plus its Left (#377).
    /// </summary>
    [TestFixture]
    public class SubreportPlacementTests
    {
        private PageText _above;
        private PageText _subFirst;

        [SetUp]
        public async Task SetUp()
        {
            RdlEngineConfig.RdlEngineConfigInit();
            Uri folder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(folder.LocalPath);

            var parser = new RDLParser(File.ReadAllText(new Uri(folder, "SubreportPlacement.rdl").LocalPath))
            {
                Folder = folder.LocalPath
            };
            Report report = await parser.Parse();
            report.Folder = folder.LocalPath;
            await report.RunGetData();

            using Pages pages = await report.BuildPages();
            var items = pages.Cast<Page>().First().Cast<PageItem>().OfType<PageText>().ToList();
            _above = items.Single(t => t.Text == "ABOVE");
            _subFirst = items.Single(t => t.Text == "SUB-FIRST");
        }

        [Test]
        public void ASubreport_PrintsAtItsTopBelowAnotherItem()
        {
            // Textbox at 12pt, Subreport at 34pt: the subreport's first item is 22pt below the textbox's top
            Assert.That(_subFirst.Y - _above.Y, Is.EqualTo(22f).Within(0.5f));
        }

        [Test]
        public void ASubreport_PrintsAtItsContainersOffsetPlusItsLeft()
        {
            Assert.That(_subFirst.X, Is.EqualTo(_above.X).Within(0.5f));
        }
    }
}
#endif
