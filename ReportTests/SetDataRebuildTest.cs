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
    /// Issues #162 ("RdlViewer Rebuild method removes the DataSet") and #183
    /// ("Rebuild very slow after a datatable is assigned"). Data supplied through
    /// DataSet.SetData() must survive a rebuild - i.e. a second RunGetData() must
    /// reuse the caller-supplied rows instead of dropping them or re-running the
    /// query. This verifies the current engine behaviour so it cannot regress.
    /// </summary>
    [TestFixture]
    public class SetDataRebuildTest
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static DataTable SentinelTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("CustomerID", typeof(string));
            dt.Rows.Add("ZZZSENTINEL1");
            dt.Rows.Add("ZZZSENTINEL2");
            return dt;
        }

        private static async Task<string> Render(Report report)
        {
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task SetData_SurvivesRebuild()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "SetDataRebuildTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report report = await RdlUtils.GetReport(fileRdlUri);
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            report.Folder = _reportFolder.LocalPath;

            // Supply our own rows, then run the report the first time.
            await report.DataSets["Data"].SetData(SentinelTable());
            await report.RunGetData();
            string firstPass = await Render(report);

            Assert.That(firstPass, Does.Contain("ZZZSENTINEL1"),
                "SetData() rows should appear on the first render.");
            Assert.That(firstPass, Does.Not.Contain("ALFKI"),
                "The query should not run when data was supplied via SetData().");

            // Simulate RdlViewer.Rebuild(): run the data + layout pipeline again
            // WITHOUT calling SetData() a second time.
            await report.RunGetData();
            string rebuilt = await Render(report);

            Assert.That(rebuilt, Does.Contain("ZZZSENTINEL1"),
                "SetData() rows must survive a rebuild (#162).");
            Assert.That(rebuilt, Does.Contain("ZZZSENTINEL2"),
                "All SetData() rows must survive a rebuild.");
            Assert.That(rebuilt, Does.Not.Contain("ALFKI"),
                "A rebuild must not re-run the query and replace SetData() rows (#183).");
        }
    }
}
#endif
