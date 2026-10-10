#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A repeating group header must be output at the top of every body column, not only
    /// at the top of every page (issue 173). Header.RunPage used to skip output when the row
    /// and page matched the previous output, which is also true when moving to the next column.
    /// </summary>
    [TestFixture]
    public class GroupHeaderColumnsTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task GroupHeader_RepeatsOnEachBodyColumn()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "GroupHeaderColumnsTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            var parser = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath))
            {
                Folder = _reportFolder.LocalPath
            };
            Report report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8), "parse errors: " + (report.ErrorItems == null ? "(none)" : string.Join(" | ", System.Linq.Enumerable.OfType<object>(report.ErrorItems))));
            report.Folder = _reportFolder.LocalPath;

            var dt = new DataTable();
            dt.Columns.Add("Grp", typeof(string));
            dt.Columns.Add("Item", typeof(string));
            // Column holds 10 rows: group A = header + 8 items fills 9, group B's header takes the
            // last slot and its first item overflows to the next column, re-running the header with
            // the very same row on the very same page.
            for (int i = 0; i < 8; i++)
                dt.Rows.Add("A", "a" + i);
            for (int i = 0; i < 4; i++)
                dt.Rows.Add("B", "b" + i);

            await report.DataSets["Data"].SetData(dt);
            await report.RunGetData();
            Pages pgs = await report.BuildPages();

            Assert.That(pgs.PageCount, Is.GreaterThanOrEqualTo(1));
            Page first = pgs[0];
            var headerXs = new System.Collections.Generic.HashSet<float>();
            foreach (PageItem pi in first)
            {
                if (pi is PageText pt && pt.Text == "GROUPHDR" && pt.X > 100)
                    headerXs.Add(pt.X);
            }
            Assert.That(headerXs.Count, Is.EqualTo(1),
                "group B header must be repeated at the top of the second body column");
        }
    }
}
#endif
