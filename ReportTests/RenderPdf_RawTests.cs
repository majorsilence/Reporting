using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests.Utils
{
    [TestFixture]
    public class RenderPdf_RawTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;
        private const string Ext = ".pdf";

        [SetUp]
        public void Prepare2Tests()
        {
            _outputFolder ??= GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        private static readonly object[] TestCases =
        {
            new object[] { "LineObjects.rdl",   "en-US", null },
            new object[] { "WorldFacts.rdl",    "pl-PL", null },
            new object[] { "ChartTypes.rdl",    "en-US", null },
            new object[] { "MatrixExample.rdl", "en-US", null },
            new object[] { "ListReport.rdl",    "en-US", null },
        };

        [Test, TestCaseSource(nameof(TestCases))]
        public async Task RenderPdf_RawDraw(string file2test, string cultureName,
            Func<Dictionary<string, IEnumerable>> fillDatasets)
        {
            GeneralUtils.ChangeCurrentCultrue(cultureName);

            Uri fileRdlUri = new Uri(_reportFolder, file2test);
            System.IO.Directory.SetCurrentDirectory(_reportFolder.LocalPath);

            Report rap = await RdlUtils.GetReport(fileRdlUri);
            rap.Folder = _reportFolder.LocalPath;

            if (fillDatasets != null)
            {
                foreach (var ds in fillDatasets())
                    await rap.DataSets[ds.Key].SetData(ds.Value);
            }
            await rap.RunGetData();

            string outFile = System.IO.Path.Combine(_outputFolder.LocalPath,
                $"{file2test}_{cultureName}_RenderPdf_Raw{Ext}");

            using var sg = new OneFileStreamGen(outFile, true);
            await rap.RunRender(sg, OutputPresentationType.RenderPdf_Raw);

            Assert.That(File.Exists(outFile), "PDF output file was not created");
            var bytes = File.ReadAllBytes(outFile);
            Assert.That(bytes.Length, Is.GreaterThan(100), "PDF output is suspiciously small");
            Assert.That(bytes[0], Is.EqualTo((byte)'%'), "PDF must start with '%'");
            Assert.That(bytes[1], Is.EqualTo((byte)'P'), "PDF must start with '%P'");
            Assert.That(bytes[2], Is.EqualTo((byte)'D'), "PDF must start with '%PD'");
            Assert.That(bytes[3], Is.EqualTo((byte)'F'), "PDF must start with '%PDF'");
        }
    }
}
