#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// The trim / Native AOT safe ways of pushing data into a DataSet: the generic SetData&lt;T&gt;
    /// (members are looked up on the annotated T) and SetCollectionData (dictionaries and
    /// enumerables, no reflection at all). Neither may depend on each item's runtime type.
    /// </summary>
    [TestFixture]
    public class AotSafeSetDataTests
    {
        private Uri _reportFolder;

        private sealed class Customer
        {
            public string CustomerID { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private async Task<Report> ParseAsync()
        {
            Uri fileRdlUri = new Uri(_reportFolder, "PushedDataCalculatedFieldTest.rdl");
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
            var parser = new RDLParser(File.ReadAllText(fileRdlUri.LocalPath)) { Folder = _reportFolder.LocalPath };
            Report report = await parser.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8));
            report.Folder = _reportFolder.LocalPath;
            return report;
        }

        private static async Task<string> RenderHtmlAsync(Report report)
        {
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            return ms.GetText();
        }

        [Test]
        public async Task GenericSetData_MapsPropertiesOfT()
        {
            Report report = await ParseAsync();
            await report.DataSets["Data"].SetData(new List<Customer>
            {
                new Customer { CustomerID = "ALFKI" },
                new Customer { CustomerID = "BERGS" },
            });

            string html = await RenderHtmlAsync(report);
            Assert.That(html, Does.Contain("ALFKI"));
            Assert.That(html, Does.Contain("BERGS"));
        }

        [Test]
        public async Task SetCollectionData_MapsDictionariesByKey()
        {
            Report report = await ParseAsync();
            await report.DataSets["Data"].SetCollectionData(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["CustomerID"] = "ALFKI" },
                new Dictionary<string, object> { ["CustomerID"] = "BERGS" },
            });

            string html = await RenderHtmlAsync(report);
            Assert.That(html, Does.Contain("ALFKI"));
            Assert.That(html, Does.Contain("BERGS"));
        }
    }
}
#endif
