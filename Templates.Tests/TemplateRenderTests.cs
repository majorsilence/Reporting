// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Templates.Tests
{
    [TestFixture]
    public class TemplateRenderTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string TemplatesRoot => Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates");

        // Every subfolder of Templates/ containing exactly one .rdl file is a template.
        private static IEnumerable<string> DiscoverTemplateDirs() =>
            Directory.GetDirectories(TemplatesRoot).OrderBy(d => d, System.StringComparer.Ordinal);

        [TestCaseSource(nameof(DiscoverTemplateDirs))]
        public async Task Template_RendersToNonEmptyPdf_WithNoFatalErrors(string templateDir)
        {
            string name = Path.GetFileName(templateDir);
            string rdlPath = Directory.GetFiles(templateDir, "*.rdl").Single();
            string rdlXml = await File.ReadAllTextAsync(rdlPath);

            // Templates using the Json data provider reference a sample-data.json file by a
            // placeholder relative name inside the RDL's ConnectString -- substitute the real
            // absolute path here the same way every JSON-provider example in this repo does.
            string sampleDataPath = Path.Combine(templateDir, "sample-data.json");
            if (File.Exists(sampleDataPath))
                rdlXml = rdlXml.Replace("file=sample-data.json", $"file={sampleDataPath}");

            var parser = new RDLParser(rdlXml) { Folder = templateDir };
            using var report = await parser.Parse();

            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                $"{name}: parse errors: {string.Join("; ", report.ErrorItems)}");

            await report.RunGetData();
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                $"{name}: data-binding errors: {string.Join("; ", report.ErrorItems)}");

            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.PDF);

            Assert.That(sg.GetStream().Length, Is.GreaterThan(0), $"{name}: rendered PDF was empty");
            Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                $"{name}: render errors: {string.Join("; ", report.ErrorItems)}");
        }

        [TestCaseSource(nameof(DiscoverTemplateDirs))]
        public void Template_HasRequiredGalleryFiles(string templateDir)
        {
            string name = Path.GetFileName(templateDir);
            Assert.That(File.Exists(Path.Combine(templateDir, "template.json")), Is.True, $"{name}: missing template.json");
            Assert.That(File.Exists(Path.Combine(templateDir, "README.md")), Is.True, $"{name}: missing README.md");
            Assert.That(Directory.GetFiles(templateDir, "*.rdl"), Has.Length.EqualTo(1), $"{name}: expected exactly one .rdl file");
        }
    }
}
