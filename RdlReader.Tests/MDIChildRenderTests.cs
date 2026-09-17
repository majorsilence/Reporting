// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlReader.Tests
{
    // Verifies MDIChild (the per-report window RdlReader.Forms's MDI shell creates for each
    // opened file) wraps a working RdlViewer.Forms control end to end, on the Headless backend.
    // Doesn't exercise the RdlReader shell Form itself (its constructor touches real roaming-
    // folder state via GetStartupState) -- MDIChild is the part D3 actually changed.
    [TestFixture]
    public class MDIChildRenderTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        [Test]
        public async Task MDIChild_LoadsFixtureReport_AndReportsPositivePageCount()
        {
            string templateDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates", "invoice");
            string rdlPath = Path.Combine(templateDir, "invoice.rdl");
            string rdlXml = await File.ReadAllTextAsync(rdlPath);

            string sampleDataPath = Path.Combine(templateDir, "sample-data.json");
            rdlXml = rdlXml.Replace("file=sample-data.json", $"file={sampleDataPath}");

            string tempRdlPath = Path.Combine(Path.GetTempPath(), $"invoice-{System.Guid.NewGuid():N}.rdl");
            await File.WriteAllTextAsync(tempRdlPath, rdlXml);
            try
            {
                using var mc = new Majorsilence.Reporting.RdlReader.MDIChild(800, 600);

                await mc.SetSourceFile(new System.Uri(tempRdlPath));

                Assert.That(mc.Viewer.PageCount, Is.GreaterThan(0), "expected at least one rendered page");
                Assert.That(mc.SourceFile, Is.EqualTo(new System.Uri(tempRdlPath)));
            }
            finally
            {
                File.Delete(tempRdlPath);
            }
        }
    }
}
