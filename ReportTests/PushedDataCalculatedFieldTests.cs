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
    /// Pushing a DataTable into a DataSet that declares anything other than plain data
    /// columns. A calculated field has a Value expression and no DataField, and a caller's
    /// own table need not carry every column the report declares; the pushed-data path
    /// looked up both by name regardless and threw from inside DataColumnCollection, so a
    /// report with even one calculated field could not be given data at all.
    ///
    /// The query path has always skipped calculated fields and tolerated a missing column.
    /// This pins the pushed path to the same behaviour.
    /// </summary>
    [TestFixture]
    public class PushedDataCalculatedFieldTests
    {
        private Uri _reportFolder;

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static DataTable SuppliedRows()
        {
            // Deliberately carries only one of the three declared fields.
            var dt = new DataTable();
            dt.Columns.Add("CustomerID", typeof(string));
            dt.Rows.Add("ALFKI");
            dt.Rows.Add("BERGS");
            return dt;
        }

        [Test]
        public async Task PushedData_WithCalculatedAndMissingFields_Renders()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "PushedDataCalculatedFieldTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            var parser = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath))
            {
                Folder = _reportFolder.LocalPath
            };
            Report report = await parser.Parse();
            Assert.That(report, Is.Not.Null, "Report failed to parse");
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                "parse errors: " + (report.ErrorItems == null ? "(none)"
                    : string.Join(" | ", System.Linq.Enumerable.OfType<object>(report.ErrorItems))));
            report.Folder = _reportFolder.LocalPath;

            await report.DataSets["Data"].SetData(SuppliedRows());
            await report.RunGetData();

            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();

            Assert.That(html, Does.Contain("ALFKI"), "the supplied column must render");
            Assert.That(html, Does.Contain("BERGS"), "every supplied row must render");
            Assert.That(html, Does.Contain("ALFKI!"),
                "the calculated field must still evaluate from the supplied column");
        }
    }
}
#endif
