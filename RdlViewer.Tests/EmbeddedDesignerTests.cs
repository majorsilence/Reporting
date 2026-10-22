using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace RdlViewer.Tests
{
    // Issue #448: problems found embedding RdlUserControl in a host application.
    [TestFixture]
    public class EmbeddedDesignerTests
    {
        private static string Report(string text) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\">" +
            "<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>" +
            "<Body><Height>1in</Height><ReportItems>" +
            "<Textbox Name=\"Greeting\"><Value>" + text + "</Value>" +
            "<Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height></Textbox>" +
            "</ReportItems></Body></Report>";

        private static volatile string _step;

        private static void Step(string step) => _step = step;

        // Runs a test body on its own STA thread. If it does not finish (a modal dialog, a deadlock)
        // the test fails naming the step it reached, rather than hanging the whole test run.
        private static void RunOnSta(Action body)
        {
            Exception failure = null;
            _step = "start";
            var thread = new Thread(() =>
            {
                try { body(); }
                catch (Exception e) { failure = e; }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromMinutes(2)))
                Assert.Fail("did not finish; stuck at: " + _step + Environment.NewLine + StackOf(thread));
            if (failure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static string StackOf(Thread thread)
        {
#if NETFRAMEWORK
#pragma warning disable 618     // Suspend/Resume and StackTrace(Thread) are obsolete, but the only way to see where it hangs
            try
            {
                thread.Suspend();
                try { return new StackTrace(thread, true).ToString(); }
                finally { thread.Resume(); }
            }
            catch (Exception e)
            {
                return "(no stack: " + e.Message + ")";
            }
#pragma warning restore 618
#else
            return "(stack only reported on net48)";
#endif
        }

        // Creating a WinForms control installs a WindowsFormsSynchronizationContext, so an awaited
        // continuation is posted to this thread's message queue. A test thread has no message loop,
        // so pump messages until the work finishes.
        private static void Pumped(Func<Task> work)
        {
            Task task = work();
            var clock = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(60))
                    throw new TimeoutException("timed out at: " + _step);
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1);
            }
            task.GetAwaiter().GetResult();
        }

        [Test]
        public void Viewer_PageCount_DoesNotRenderTheReport_EnsureRenderedDoes() => RunOnSta(() =>
        {
            Step("new RdlViewer");
            using (var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { ShowWaitDialog = false })
            {
                // SetSourceRdl renders straight away when the control counts as Visible, which an
                // unparented control does; SourceRdlNoLoad never renders.
                Step("SourceRdlNoLoad");
                viewer.SourceRdlNoLoad = Report("Hello");

                // Reading PageCount used to render the report on a pool thread and block the caller.
                Step("PageCount before render");
                Assert.That(viewer.PageCount, Is.EqualTo(0));

                Step("EnsureRendered");
                Pumped(() => viewer.EnsureRendered());
                Assert.That(viewer.PageCount, Is.EqualTo(1));
                Step("dispose");
            }
        });

        [Test]
        public void LoadingAReport_IsNotAModification() => RunOnSta(() =>
        {
            Step("new RdlUserControl");
            using (var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl())
            {
                int changes = 0;
                designer.RdlChanged += (s, e) => changes++;

                Step("first load");
                designer.SourceRdl = Report("First");
                Assert.That(designer.Modified, Is.False, "first load");

                Step("second load");
                designer.SourceRdl = Report("Second");
                Assert.That(designer.Modified, Is.False, "second load");
                Assert.That(changes, Is.EqualTo(0), "loading does not raise RdlChanged");

                // In an embedded designer this showed a modal "unable to create RDL" box and returned "".
                Step("GetRdlText");
                Assert.That(designer.GetRdlText(), Does.Contain("Second"));
                Step("dispose");
            }
        });

        // Regression for the #448 Preview-rendering report: EnsureRendered could return before
        // the report it's waiting on had actually finished loading. The designer's Preview tab
        // starts a load on tab-select; a host that then awaits EnsureRendered() right after could
        // see the call complete in 0ms with PageCount still 0, because the old code tracked "a
        // load is running" with a bool and let a later caller bail out instead of joining it.
        [Test]
        public void EnsureRendered_JoinsAnAlreadyInFlightLoad_InsteadOfReturningEarly() => RunOnSta(() =>
        {
            Step("new RdlViewer");
            using (var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { ShowWaitDialog = false })
            {
                Step("SourceRdlNoLoad");
                viewer.SourceRdlNoLoad = Report("Hello");

                Step("first EnsureRendered (not awaited)");
                Task first = viewer.EnsureRendered();     // starts the load; suspended at its first await
                Step("second EnsureRendered (not awaited)");
                Task second = viewer.EnsureRendered();    // must join the same in-flight load, not skip it

                // Nothing has pumped the STA thread's message queue since the two calls above, so
                // this is the real race: the bug made "second" a synchronously-completed task with
                // PageCount still 0, exactly as the reporter observed (0ms, zero pages).
                Step("check for the early return");
                Assert.That(second.IsCompleted && viewer.PageCount == 0, Is.False,
                    "second EnsureRendered call returned synchronously while the first call's load was still in flight");

                Step("pump to completion");
                Pumped(() => Task.WhenAll(first, second));
                Assert.That(viewer.PageCount, Is.EqualTo(1));
                Step("dispose");
            }
        });

        // Regression for the same report's A -> B -> A sequence: the source can change while a
        // load from the previous source is still running. The stale load must not win the race
        // and overwrite the pages of the report that is now selected.
        [Test]
        public void EnsureRendered_AfterSourceChangesMidLoad_EndsOnTheNewSource() => RunOnSta(() =>
        {
            Step("new RdlViewer");
            using (var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { ShowWaitDialog = false })
            {
                Step("load First");
                viewer.SourceRdlNoLoad = Report("First");
                Task staleLoad = viewer.EnsureRendered();     // "First" load starts, suspended

                Step("switch to Second mid-load");
                viewer.SourceRdlNoLoad = Report("Second");     // source changes before staleLoad finishes
                Task freshLoad = viewer.EnsureRendered();      // "Second" load starts

                Step("pump to completion");
                Pumped(() => Task.WhenAll(staleLoad, freshLoad));

                Assert.That(viewer.PageCount, Is.EqualTo(1),
                    "the stale First load must not leave the viewer without pages for Second");

                string file = Path.Combine(Path.GetTempPath(), "embedded-ensure-rendered-" + Guid.NewGuid().ToString("N") + ".html");
                try
                {
                    Step("SaveAs HTML");
                    Pumped(() => viewer.SaveAs(file, OutputPresentationType.HTML));
                    string html = File.ReadAllText(file);
                    Assert.That(html, Does.Contain("Second"));
                    Assert.That(html, Does.Not.Contain("First"),
                        "the stale First load overwrote the Second report that is now selected");
                }
                finally
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                Step("dispose");
            }
        });

        [Test]
        public void Export_UsesTheLoadedReport_WithoutVisitingThePreviewTab() => RunOnSta(() =>
        {
            string file = Path.Combine(Path.GetTempPath(), "embedded-" + Guid.NewGuid().ToString("N") + ".html");
            try
            {
                Step("new RdlUserControl");
                using (var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl())
                {
                    Step("load");
                    designer.SourceRdl = Report("First");
                    designer.SourceRdl = Report("Second");

                    Assert.That(designer.Viewer.SourceRdl, Does.Contain("Second"), "loading primes the preview");

                    Step("SaveAsAsync");
                    Pumped(() => designer.SaveAsAsync(file, OutputPresentationType.HTML));
                    Step("dispose");
                }

                string html = File.ReadAllText(file);
                Assert.That(html, Does.Contain("Second"));
                Assert.That(html, Does.Not.Contain("First"));
            }
            finally
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        });
    }
}
