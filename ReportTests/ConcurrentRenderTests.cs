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
    /// A web server renders many reports at once, each thread with its own Report. The engine shares
    /// some state between them (the config, the font registry and parsed fonts, static tables), and
    /// performance work adds more caches, so rendering from many threads has to give exactly what
    /// rendering from one does.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class ConcurrentRenderTests
    {
        private Uri _reportFolder;

        private static readonly string[] Reports =
        {
            "testdata.rdl",         // inline rows
            "WorldFacts.rdl",       // xml file, sorting, grouping
            "MatrixExample.rdl",    // sqlite
            "ListPagingTest.rdl",   // html text split across pages
            "CanGrowSplitTest.rdl",
            "GroupSortTest.rdl",    // groups sorted by an aggregate
            "RecursiveGroupTest.rdl",
        };

        private static readonly OutputPresentationType[] Formats =
        {
            OutputPresentationType.PDF,
            OutputPresentationType.CSV,
            OutputPresentationType.XML,
        };

        [SetUp]
        public void SetUp()
        {
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(_reportFolder.LocalPath);
        }

        private async Task<string> RenderHash(string reportFile, OutputPresentationType format)
        {
            Report report = await RdlUtils.GetReport(new Uri(_reportFolder, reportFile));
            Assert.That(report, Is.Not.Null, reportFile + " failed to parse");
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData();
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, format);

            var stream = ms.GetStream();
            stream.Position = 0;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            byte[] bytes = copy.ToArray();

            if (format == OutputPresentationType.PDF)
            {
                // creation time and file id differ on every run
                string text = Encoding.Latin1.GetString(bytes);
                text = Regex.Replace(text, @"/(CreationDate|ModDate)\s*\([^)]*\)", "/$1()");
                text = Regex.Replace(text, @"/ID\s*\[[^\]]*\]", "/ID[]");
                bytes = Encoding.Latin1.GetBytes(text);
            }

            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        [Test]
        public async Task SameOutputFromManyThreadsAsFromOne()
        {
            // Expected output, one render at a time. The first PDF render also fills the shared font caches.
            var expected = new Dictionary<(string, OutputPresentationType), string>();
            foreach (string r in Reports)
                foreach (var f in Formats)
                    expected[(r, f)] = await RenderHash(r, f);

            const int threads = 8;
            const int rounds = 4;
            var failures = new ConcurrentBag<string>();
            using var start = new ManualResetEventSlim(false);

            var workers = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
            {
                start.Wait();   // let them all go at once
                for (int round = 0; round < rounds; round++)
                {
                    // a different starting point per thread so the threads are not in step
                    for (int i = 0; i < Reports.Length * Formats.Length; i++)
                    {
                        int k = (i + t * 3) % (Reports.Length * Formats.Length);
                        string reportFile = Reports[k / Formats.Length];
                        var format = Formats[k % Formats.Length];
                        try
                        {
                            string hash = await RenderHash(reportFile, format);
                            if (hash != expected[(reportFile, format)])
                                failures.Add($"{reportFile} {format}: output differs from the single threaded render");
                        }
                        catch (Exception ex)
                        {
                            failures.Add($"{reportFile} {format}: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            })).ToArray();

            start.Set();
            await Task.WhenAll(workers);

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Distinct().Take(10)));
        }

        [Test]
        public async Task FirstRenderFromManyThreadsAtOnce()
        {
            // Nothing has rendered yet in a fresh web process, so every thread races to build the config,
            // the font registry and the parsed fonts. The caches are static, so this only really races in
            // the first test of a run; the threads still all start together.
            const int threads = 8;
            var failures = new ConcurrentBag<string>();
            using var start = new ManualResetEventSlim(false);

            var workers = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
            {
                start.Wait();
                try
                {
                    RdlEngineConfig.RdlEngineConfigInit();
                    string hash = await RenderHash("testdata.rdl", OutputPresentationType.PDF);
                    if (string.IsNullOrEmpty(hash)) failures.Add("empty");
                }
                catch (Exception ex)
                {
                    failures.Add($"{ex.GetType().Name}: {ex.Message}");
                }
            })).ToArray();

            start.Set();
            await Task.WhenAll(workers);

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures.Distinct().Take(10)));
        }
    }
}
#endif
