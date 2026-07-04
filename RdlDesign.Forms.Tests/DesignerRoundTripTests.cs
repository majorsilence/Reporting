// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // D6: drives the designer's core "open -> edit -> save -> preview" loop end to end on the
    // Headless backend. Doesn't simulate real mouse-drag placement (that needs Majorsilence.Forms'
    // drag-drop machinery driven interactively, better verified manually per the plan's own "on
    // Linux desktop, create/save/preview a report end-to-end" note) -- instead edits the design
    // surface's underlying RDL XML directly, which is exactly what a completed drag-drop operation
    // itself produces, and verifies the design surface (MDIChild.SourceRdl) picks up and round-trips
    // that edit correctly, and that the result renders.
    [TestFixture]
    public class DesignerRoundTripTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        [Test]
        public async Task OpenEditSaveRender_RoundTripsCorrectly()
        {
            string templateDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates", "invoice");
            string rdlPath = Path.Combine(templateDir, "invoice.rdl");
            string rdlXml = await File.ReadAllTextAsync(rdlPath);

            string sampleDataPath = Path.Combine(templateDir, "sample-data.json");
            rdlXml = rdlXml.Replace("file=sample-data.json", $"file={sampleDataPath}");

            string openedRdlPath = Path.Combine(Path.GetTempPath(), $"d6-open-{Guid.NewGuid():N}.rdl");
            await File.WriteAllTextAsync(openedRdlPath, rdlXml);
            try
            {
                using var mc = new Majorsilence.Reporting.RdlDesign.MDIChild(800, 600);

                // 1. Open: the design surface loads and reflects the report.
                await mc.SetSourceFileAsync(new Uri(openedRdlPath));
                Assert.That(mc.SourceRdl, Does.Contain("<Textbox Name=\"D_Desc\">"),
                    "expected the design surface to reflect the opened report's content");

                // 2. Edit: insert a new report item, standing in for what a completed drag-drop
                // placement produces (a new element under the Body's <ReportItems>). The Body's
                // <ReportItems> is the first one to appear in the serialized document (Body comes
                // before PageHeader/PageFooter's own ReportItems), and doesn't directly follow
                // <Body> (there's a <Height> element in between), so target the first
                // <ReportItems> occurrence directly rather than assuming adjacency.
                const string newTextboxXml = "<Textbox Name=\"D6_NewField\"><Value>D6 round-trip test</Value></Textbox>";
                int insertAt = mc.SourceRdl.IndexOf("<ReportItems>", StringComparison.Ordinal);
                Assert.That(insertAt, Is.GreaterThanOrEqualTo(0), "test setup: expected to find a <ReportItems> insertion point");
                insertAt += "<ReportItems>".Length;
                string edited = mc.SourceRdl.Insert(insertAt, newTextboxXml);
                mc.SourceRdl = edited;

                // 3. The design surface picked up and retains the edit.
                Assert.That(mc.SourceRdl, Does.Contain("D6_NewField"),
                    "expected the design surface to retain the inserted report item after re-parsing");

                // 4. Save.
                string savedRdlPath = Path.Combine(Path.GetTempPath(), $"d6-saved-{Guid.NewGuid():N}.rdl");
                await File.WriteAllTextAsync(savedRdlPath, mc.SourceRdl);

                try
                {
                    // 5. Preview: the saved report parses and renders through RdlEngine itself
                    // (independent of the designer), proving the edit produced valid RDL.
                    string savedXml = await File.ReadAllTextAsync(savedRdlPath);
                    var parser = new RDLParser(savedXml) { Folder = templateDir };
                    using var report = await parser.Parse();

                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"parse errors: {string.Join("; ", report.ErrorItems)}");

                    await report.RunGetData();
                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"data-binding errors: {string.Join("; ", report.ErrorItems)}");

                    using var sg = new MemoryStreamGen();
                    await report.RunRender(sg, OutputPresentationType.PDF);

                    Assert.That(sg.GetStream().Length, Is.GreaterThan(0), "expected non-empty rendered PDF output");
                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"render errors: {string.Join("; ", report.ErrorItems)}");
                }
                finally
                {
                    File.Delete(savedRdlPath);
                }
            }
            finally
            {
                File.Delete(openedRdlPath);
            }
        }
    }
}
