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
    [Apartment(ApartmentState.STA)]
    public class EmbeddedDesignerTests
    {
        // Creating a WinForms control installs a WindowsFormsSynchronizationContext, so an awaited
        // continuation is posted to this thread's message queue. A test thread has no message loop,
        // so an async test would wait forever; pump messages until the work finishes instead.
        private static void RunPumped(Func<Task> work)
        {
            Task task = work();
            var clock = Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (clock.Elapsed > TimeSpan.FromSeconds(60))
                    Assert.Fail("timed out");
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(1);
            }
            task.GetAwaiter().GetResult();
        }

        private static string Report(string text) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\">" +
            "<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>" +
            "<Body><Height>1in</Height><ReportItems>" +
            "<Textbox Name=\"Greeting\"><Value>" + text + "</Value>" +
            "<Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height></Textbox>" +
            "</ReportItems></Body></Report>";

        [Test]
        public void Viewer_PageCount_DoesNotRenderTheReport_EnsureRenderedDoes()
        {
            using (var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { ShowWaitDialog = false })
            {
                RunPumped(() => viewer.SetSourceRdl(Report("Hello")));

                // Reading PageCount used to render the report on a pool thread and block the caller.
                Assert.That(viewer.PageCount, Is.EqualTo(0));

                RunPumped(() => viewer.EnsureRendered());
                Assert.That(viewer.PageCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void LoadingAReport_IsNotAModification()
        {
            using (var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl())
            {
                int changes = 0;
                designer.RdlChanged += (s, e) => changes++;

                designer.SourceRdl = Report("First");
                Assert.That(designer.Modified, Is.False, "first load");

                designer.SourceRdl = Report("Second");
                Assert.That(designer.Modified, Is.False, "second load");
                Assert.That(changes, Is.EqualTo(0), "loading does not raise RdlChanged");
            }
        }

        [Test]
        public void Export_UsesTheLoadedReport_WithoutVisitingThePreviewTab()
        {
            string file = Path.Combine(Path.GetTempPath(), "embedded-" + Guid.NewGuid().ToString("N") + ".html");
            try
            {
                using (var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl())
                {
                    designer.SourceRdl = Report("First");
                    designer.SourceRdl = Report("Second");

                    RunPumped(() => designer.SaveAsAsync(file, OutputPresentationType.HTML));
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
        }
    }
}
