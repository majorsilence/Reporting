// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #448: problems found embedding RdlUserControl in a host application.
    [TestFixture]
    public class EmbeddedDesignerTests
    {
        private static string Report(string text) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>4in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  <Body>
    <Height>1in</Height>
    <ReportItems>
      <Textbox Name=""Greeting""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.25in</Height><Value>{text}</Value></Textbox>
    </ReportItems>
  </Body>
</Report>";

        private static RdlEditPreview Editor(RdlUserControl designer) =>
            (RdlEditPreview)typeof(RdlUserControl)
                .GetField("rdlEditPreview1", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(designer)!;

        [Test]
        public void LoadingAReport_IsNotAModification()
        {
            using var designer = new RdlUserControl();
            int changes = 0;
            designer.RdlChanged += (s, e) => changes++;

            designer.SourceRdl = Report("First");
            Assert.That(designer.Modified, Is.False, "first load");

            designer.SourceRdl = Report("Second");
            Assert.That(designer.Modified, Is.False, "second load");
            Assert.That(changes, Is.EqualTo(0), "loading does not raise RdlChanged");
        }

        [Test]
        public void EditingTheReport_IsAModification()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report("First");
            int changes = 0;
            designer.RdlChanged += (s, e) => changes++;

            DesignCtl design = Editor(designer).DesignCtl;
            var ns = new XmlNamespaceManager(design.ReportDocument.NameTable);
            ns.AddNamespace("r", "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition");
            design.ReportDocument.SelectSingleNode("//r:Textbox/r:Value", ns)!.InnerText = "Edited";
            design.SignalReportChanged();

            Assert.That(designer.Modified, Is.True);
            Assert.That(changes, Is.GreaterThan(0));
            Assert.That(designer.GetRdlText(), Does.Contain("Edited"));
        }

        [Test]
        public async Task Export_UsesTheLoadedReport_WithoutVisitingThePreviewTab()
        {
            string file = Path.Combine(Path.GetTempPath(), "embedded-" + Guid.NewGuid().ToString("N") + ".html");
            try
            {
                using (var designer = new RdlUserControl())
                {
                    designer.SourceRdl = Report("First");
                    designer.SourceRdl = Report("Second");

                    await designer.SaveAsAsync(file, OutputPresentationType.HTML);
                }

                string html = await File.ReadAllTextAsync(file);
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
