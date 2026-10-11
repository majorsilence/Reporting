#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// A desktop host (WinForms, Majorsilence.Forms, Avalonia) has a SynchronizationContext, and every await
    /// that does not say ConfigureAwait(false) resumes on it. Engine code does not need the UI thread, so each
    /// of those is a pointless hop (and a way to deadlock if the caller blocks). The engine library must not
    /// post back to the context its caller started on.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class SynchronizationContextTests
    {
        private sealed class CountingContext : SynchronizationContext
        {
            public int Posts;

            public override void Post(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref Posts);
                ThreadPool.QueueUserWorkItem(_ => d(state));
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref Posts);
                d(state);
            }
        }

        [TestCase("WorldFacts.rdl")]
        [TestCase("MatrixExample.rdl")]
        [TestCase("GroupSortTest.rdl")]
        [TestCase("RecursiveGroupTest.rdl")]
        [TestCase("ParameterApiTest.rdl")]
        public async Task Engine_DoesNotPostBackToTheCallersContext(string reportFile)
        {
            var folder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(folder.LocalPath);
            Report report = await RdlUtils.GetReport(new Uri(folder, reportFile));
            report.Folder = folder.LocalPath;

            var previous = SynchronizationContext.Current;
            var ctx = new CountingContext();
            SynchronizationContext.SetSynchronizationContext(ctx);
            try
            {
                // the test's own awaits must not count, so they do not capture the context
                await report.RunGetData().ConfigureAwait(false);
                using var ms = new MemoryStreamGen();
                await report.RunRender(ms, OutputPresentationType.PDF).ConfigureAwait(false);
                await report.RunGetData().ConfigureAwait(false);
                await report.RunRender(ms, OutputPresentationType.HTML).ConfigureAwait(false);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }

            Assert.That(ctx.Posts, Is.Zero, "engine continuations resumed on the caller's SynchronizationContext");
        }
    }
}
#endif
