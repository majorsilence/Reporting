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
    /// Group and hierarchy ordering. These sorts evaluate report expressions, which are async, and used to
    /// block a thread-pool thread per comparison; they are awaited now, and must order exactly as before.
    /// </summary>
    [TestFixture]
    public class AsyncSortTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
        }

        private async Task<string[]> RenderCsvLines(string reportFile)
        {
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, reportFile));
            Assert.That(report, Is.Not.Null);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.CSV);
            return ms.GetText().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
        }

        [Test]
        public async Task TableGroups_SortedDescendingByAggregate()
        {
            string[] lines = await RenderCsvLines("GroupSortTest.rdl");

            // group header rows are the ones with a quoted region name
            string[] groups = lines.Where(l => l.StartsWith("\"")).ToArray();
            Assert.That(groups, Is.EqualTo(new[]
            {
                "\"Central\",100", "\"East\",70", "\"South\",51", "\"West\",45", "\"North\",15",
            }));

            // and each group's own rows follow it, in the order they came in
            Assert.That(lines[0], Is.EqualTo("\"Central\",100"));
            Assert.That(lines.Skip(1).Take(2), Is.EqualTo(new[] { "9,99", "10,1" }));
        }

        [Test]
        public async Task RecursiveGroup_ListsChildrenUnderTheirParent()
        {
            string[] lines = await RenderCsvLines("RecursiveGroupTest.rdl");

            Assert.That(lines, Is.EqualTo(new[]
            {
                "\"CEO\",0",
                "\"VP Eng\",1",
                "\"Dev Lead\",2",
                "\"Dev One\",3",
                "\"Dev Two\",3",
                "\"QA Lead\",2",
                "\"VP Sales\",1",
                "\"Account Mgr\",2",
                "\"Rep One\",3",
            }));
        }

        [Test]
        public async Task Matrix_DynamicColumnsSortedDescending_RowsKeepEncounterOrder()
        {
            string[] lines = await RenderCsvLines("MatrixSortTest.rdl");

            // The columns have a plain sort expression and are reordered. The rows sort on Sum(), which the
            // matrix evaluates against the entry's first row, so they stay in the order first met. That is
            // how the engine has always behaved; this pins it so the sort rewrite cannot change it.
            Assert.That(lines, Is.EqualTo(new[]
            {
                "\"Q4\",\"Q3\",\"Q2\",\"Q1\"",
                "\"North\",0,0,5,10",
                "\"South\",0,1,0,50",
                "\"East\",0,0,30,40",
                "\"West\",20,0,0,25",
                "\"Central\",0,99,1,0",
            }));
        }
    }
}
#endif
