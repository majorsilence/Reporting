#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A CompiledReport is parsed once and shared by every run, on every thread. These tests render each report from
    /// one shared definition on many threads and compare with a render from a freshly parsed report.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class CompiledReportTests
    {
        private string _folder;

        private static readonly string[] Reports =
        {
            "testdata.rdl",
            "WorldFacts.rdl",
            "MatrixExample.rdl",
            "ListPagingTest.rdl",
            "CanGrowSplitTest.rdl",
            "GroupSortTest.rdl",
            "RecursiveGroupTest.rdl",
            "SubreportPlacement.rdl",
            "SubreportNoRowsTest.rdl",
        };

        private static readonly OutputPresentationType[] Formats =
        {
            OutputPresentationType.PDF,
            OutputPresentationType.CSV,
            OutputPresentationType.XML,
            OutputPresentationType.HTML,
        };

        [SetUp]
        public void SetUp()
        {
            _folder = GeneralUtils.ReportsFolder().LocalPath;
            Directory.SetCurrentDirectory(_folder);
        }

        private static async Task<string> HashOf(Report report, string folder, OutputPresentationType format)
        {
            report.Folder = folder;
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, format);

            var stream = ms.GetStream();
            stream.Position = 0;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            string text = Encoding.Latin1.GetString(copy.ToArray());
            text = Regex.Replace(text, @"/(CreationDate|ModDate)\s*\([^)]*\)", "/$1()");
            text = Regex.Replace(text, @"/ID\s*\[[^\]]*\]", "/ID[]");
            // HTML names its temporary image files and style sheets per run
            text = Regex.Replace(text, @"[A-Za-z0-9_\-]*\.tmp", "x.tmp");
            return Convert.ToHexString(SHA256.HashData(Encoding.Latin1.GetBytes(text)));
        }

        private async Task<string> FreshHash(string file, OutputPresentationType format)
        {
            var parser = new RDLParser(File.ReadAllText(Path.Combine(_folder, file))) { Folder = _folder };
            Report report = await parser.Parse();
            return await HashOf(report, _folder, format);
        }

        private async Task<CompiledReport> Compile(string file) =>
            await CompiledReport.CompileAsync(File.ReadAllText(Path.Combine(_folder, file)),
                new CompiledReportOptions { Folder = _folder });

        [Test]
        public async Task ManyThreadsRenderingOneCompiledDefinitionGiveTheSameOutputAsAFreshParse()
        {
            var compiled = new Dictionary<string, CompiledReport>();
            var expected = new Dictionary<(string, OutputPresentationType), string>();
            foreach (string r in Reports)
            {
                compiled[r] = await Compile(r);
                foreach (var f in Formats)
                    expected[(r, f)] = await FreshHash(r, f);
            }

            try
            {
                const int threads = 8;
                const int rounds = 3;
                var failures = new ConcurrentBag<string>();
                using var start = new ManualResetEventSlim(false);

                var workers = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
                {
                    start.Wait();
                    for (int round = 0; round < rounds; round++)
                    {
                        int total = Reports.Length * Formats.Length;
                        for (int i = 0; i < total; i++)
                        {
                            int k = (i + t * 5) % total;
                            string file = Reports[k / Formats.Length];
                            var format = Formats[k % Formats.Length];
                            try
                            {
                                using Report report = compiled[file].CreateReport();
                                string hash = await HashOf(report, _folder, format);
                                if (hash != expected[(file, format)])
                                    failures.Add($"{file} {format}: output differs from a freshly parsed report");
                            }
                            catch (Exception ex)
                            {
                                failures.Add($"{file} {format}: {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                    }
                })).ToArray();

                start.Set();
                await Task.WhenAll(workers);

                Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Distinct().Take(10)));
            }
            finally
            {
                foreach (var c in compiled.Values) c.Dispose();
            }
        }

        [Test]
        public async Task DisposingAReportLeavesTheCompiledDefinitionUsable()
        {
            using var compiled = await Compile("testdata.rdl");
            string expected = await FreshHash("testdata.rdl", OutputPresentationType.CSV);

            for (int i = 0; i < 3; i++)
            {
                using Report report = compiled.CreateReport();
                Assert.That(await HashOf(report, _folder, OutputPresentationType.CSV), Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task ASharedReportCannotChangeTheDefinitionsPassword()
        {
            using var compiled = await Compile("testdata.rdl");
            using Report report = compiled.CreateReport();
            Assert.Throws<InvalidOperationException>(() => report.GetDataSourceReferencePassword = () => "x");
        }

        [Test]
        public async Task CreateReportAfterDisposeThrows()
        {
            var compiled = await Compile("testdata.rdl");
            compiled.Dispose();
            Assert.Throws<ObjectDisposedException>(() => compiled.CreateReport());
        }
    }
}
#endif
