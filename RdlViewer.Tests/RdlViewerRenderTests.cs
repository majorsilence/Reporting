// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlViewer.Tests
{
    // Verifies RdlViewer.Forms's core loop -- SetSourceFile -> LoadPageIfNeeded (internally) ->
    // PageDrawing.Draw for every page -- against real fixture reports, on the Headless backend.
    // This exercises the actual screen-paint code fixed during the WinForms -> Majorsilence.Forms
    // migration (Color bridging, DrawLine/DrawCurve/gradient overload differences, the justified
    // text fallback, etc.), not just "does the control construct."
    [TestFixture]
    public class RdlViewerRenderTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string TemplatesRoot => Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates");

        private static IEnumerable<string> DiscoverTemplateDirs() =>
            Directory.GetDirectories(TemplatesRoot).OrderBy(d => d, System.StringComparer.Ordinal);

        [TestCaseSource(nameof(DiscoverTemplateDirs))]
        public async Task Viewer_LoadsFixtureReport_AndReportsPositivePageCount(string templateDir)
        {
            string name = Path.GetFileName(templateDir);
            string rdlPath = Directory.GetFiles(templateDir, "*.rdl").Single();
            string rdlXml = await File.ReadAllTextAsync(rdlPath);

            // Templates using the Json data provider reference sample-data.json by a placeholder
            // relative name inside the RDL's ConnectString -- substitute the real absolute path,
            // same as Templates.Tests does, then write the substituted RDL to a temp file since
            // RdlViewer's public API (SetSourceFile) loads from a file Uri, not raw XML text.
            string sampleDataPath = Path.Combine(templateDir, "sample-data.json");
            if (File.Exists(sampleDataPath))
                rdlXml = rdlXml.Replace("file=sample-data.json", $"file={sampleDataPath}");

            string tempRdlPath = Path.Combine(Path.GetTempPath(), $"{name}-{System.Guid.NewGuid():N}.rdl");
            await File.WriteAllTextAsync(tempRdlPath, rdlXml);
            try
            {
                using var viewer = new Majorsilence.Reporting.RdlViewer.RdlViewer
                {
                    ShowWaitDialog = false,
                };

                await viewer.SetSourceFile(new System.Uri(tempRdlPath));

                // SetSourceFile only renders when the viewer is Visible (it is not, here -- never
                // shown); EnsureRendered forces the load. PageCount no longer blocks on a hidden
                // Task.Run to do it as a side effect of being read.
                await viewer.EnsureRendered();

                Assert.That(viewer.PageCount, Is.GreaterThan(0), $"{name}: expected at least one rendered page");
            }
            finally
            {
                File.Delete(tempRdlPath);
            }
        }
    }
}
