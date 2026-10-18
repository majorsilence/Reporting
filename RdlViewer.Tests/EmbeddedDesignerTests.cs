using System;
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
        private static string Report(string text) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition\">" +
            "<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth>" +
            "<Body><Height>1in</Height><ReportItems>" +
            "<Textbox Name=\"Greeting\"><Value>" + text + "</Value>" +
            "<Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height></Textbox>" +
            "</ReportItems></Body></Report>";

        [Test]
        public async Task Viewer_PageCount_DoesNotRenderTheReport_EnsureRenderedDoes()
        {
            using (var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer { ShowWaitDialog = false })
            {
                await viewer.SetSourceRdl(Report("Hello"));

                // Reading PageCount used to render the report on a pool thread and block the caller.
                Assert.That(viewer.PageCount, Is.EqualTo(0));

                await viewer.EnsureRendered();
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
        public async Task Export_UsesTheLoadedReport_WithoutVisitingThePreviewTab()
        {
            string file = Path.Combine(Path.GetTempPath(), "embedded-" + Guid.NewGuid().ToString("N") + ".html");
            try
            {
                using (var designer = new Majorsilence.Reporting.RdlDesign.RdlUserControl())
                {
                    designer.SourceRdl = Report("First");
                    designer.SourceRdl = Report("Second");

                    await designer.SaveAsAsync(file, OutputPresentationType.HTML);
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
