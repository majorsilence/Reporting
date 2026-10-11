#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// UserReportParameter's DefaultValue / DisplayValues / DataValues / Value / Values blocked the calling
    /// thread. They are obsolete now; the async methods give the same answers without blocking.
    /// </summary>
    [TestFixture]
    public class UserReportParameterAsyncTests
    {
        private Report _report;

        [SetUp]
        public async Task SetUp()
        {
            var folder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(folder.LocalPath);
            _report = await RdlUtils.GetReport(new Uri(folder, "ParameterApiTest.rdl"));
            Assert.That(_report, Is.Not.Null);
            _report.Folder = folder.LocalPath;
        }

        private UserReportParameter Param(string name) =>
            _report.UserReportParameters.Cast<UserReportParameter>().Single(p => p.Name == name);

        [Test]
        public async Task AsyncGetters_ReturnTheLabelsValuesAndDefault()
        {
            var region = Param("Region");

            Assert.That(await region.GetDisplayValuesAsync(), Is.EqualTo(new[] { "North", "South" }));
            Assert.That(await region.GetDataValuesAsync(), Is.EqualTo(new object[] { "N", "S" }));
            Assert.That(await region.GetDefaultValueAsync(), Is.EqualTo(new object[] { "S" }));

            var free = Param("Free");
            Assert.That(await free.GetDisplayValuesAsync(), Is.Null);
            Assert.That(await free.GetDataValuesAsync(), Is.Null);
            Assert.That(await free.GetDefaultValueAsync(), Is.Null);
        }

        [Test]
        public async Task SetValueAsync_TranslatesADisplayValueToItsDataValue()
        {
            var region = Param("Region");

            await region.SetValueAsync("North");      // the label the user sees
            Assert.That(await region.GetValueAsync(), Is.EqualTo("N"));

            await region.SetValueAsync("S");          // a data value passes through
            Assert.That(await region.GetValueAsync(), Is.EqualTo("S"));
        }

        [Test]
        public async Task SetValueAsync_FeedsTheReport()
        {
            await Param("Region").SetValueAsync("North");
            await Param("Free").SetValueAsync("abc");
            await _report.RunGetData();
            using var ms = new MemoryStreamGen();
            await _report.RunRender(ms, OutputPresentationType.HTML);

            Assert.That(ms.GetText(), Does.Contain("N/abc"));
        }

        [Test]
        public async Task ObsoleteProperties_StillAgreeWithTheAsyncMethods()
        {
#pragma warning disable CS0618 // the point of this test
            var region = Param("Region");
            Assert.That(region.DisplayValues, Is.EqualTo(await region.GetDisplayValuesAsync()));
            Assert.That(region.DataValues, Is.EqualTo(await region.GetDataValuesAsync()));
            Assert.That(region.DefaultValue, Is.EqualTo(await region.GetDefaultValueAsync()));

            region.Value = "North";
            Assert.That(region.Value, Is.EqualTo("N"));
            Assert.That(await region.GetValueAsync(), Is.EqualTo("N"));
#pragma warning restore CS0618
        }

        [Test]
        public async Task ParametersCanBeSetFromManyThreads()
        {
            // each report is its own; this checks nothing static is shared between them
            var folder = GeneralUtils.ReportsFolder();
            var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
            {
                var r = await RdlUtils.GetReport(new Uri(folder, "ParameterApiTest.rdl"));
                var p = r.UserReportParameters.Cast<UserReportParameter>().Single(x => x.Name == "Region");
                await p.SetValueAsync(i % 2 == 0 ? "North" : "South");
                return await p.GetValueAsync();
            })).ToArray();

            var values = await Task.WhenAll(tasks);
            Assert.That(values, Is.EqualTo(Enumerable.Range(0, 8).Select(i => i % 2 == 0 ? "N" : "S")));
        }
    }
}
#endif
