// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDoctor;
using NUnit.Framework;

namespace RdlDoctor.Tests
{
    [TestFixture]
    public class Checks6To10AndFixTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Fixture(string name) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

        [Test]
        public async Task SharedDataSetReference_IsFlaggedAsUnsupported()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC006" && f.Severity == FindingSeverity.Error), Is.True);
        }

        [Test]
        public async Task UnregisteredDataProvider_IsFlaggedWithSuggestionList()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            var finding = findings.FirstOrDefault(f => f.Id == "DOC007");

            Assert.That(finding, Is.Not.Null);
            Assert.That(finding!.Message, Does.Contain("MyMadeUpProvider"));
            Assert.That(finding.Message, Does.Contain("SQLite")); // a real registered provider should be listed
        }

        [Test]
        public async Task CodeModule_ProducesInformationalNote()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC008" && f.Message.Contains("MyCustomAssembly.dll")), Is.True);
        }

        [Test]
        public async Task ClassesWithoutCodeModules_ProducesWarningNote()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("classes-only.rdlc"));
            Assert.That(findings.Any(f => f.Id == "DOC008" && f.Message.Contains("without <CodeModules>")), Is.True);
        }

        [Test]
        public async Task ClassesWithCodeModulesPresent_DoesNotWarnAboutMissingCodeModules()
        {
            // checks6-10.rdl has both <Classes> and <CodeModules> -- the "Classes without
            // CodeModules" warning must not fire here.
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC008" && f.Message.Contains("without <CodeModules>")), Is.False);
        }

        [Test]
        public async Task MissingExternalImageAndSubreport_AreFlagged()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            var doc009 = findings.Where(f => f.Id == "DOC009").ToList();

            Assert.That(doc009.Any(f => f.Message.Contains("does-not-exist.png")), Is.True);
            Assert.That(doc009.Any(f => f.Message.Contains("NoSuchSubreport")), Is.True);
        }

        [Test]
        public async Task RdlcDataSetWithNoQuery_ProducesInformationalNote()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("classes-only.rdlc"));
            Assert.That(findings.Any(f => f.Id == "DOC010"), Is.True);
        }

        [Test]
        public async Task RdlFileExtension_NeverTriggersRdlcQuirkCheck()
        {
            // checks6-10.rdl has no <Query> in its shared-reference DataSet either, but DOC010
            // is scoped to .rdlc files only.
            var findings = await CompatibilityChecker.CheckAsync(Fixture("checks6-10.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC010"), Is.False);
        }

        [Test]
        public void Fix_NormalizesNamespaceAndStripsDesignerAttributes_WhenNoStructuralIncompatibility()
        {
            string xml = File.ReadAllText(Fixture("fixable-2016ns.rdl"));
            var result = RdlFixer.Fix(xml, stripUnknown: false);

            Assert.That(result.ChangesMade, Has.Count.EqualTo(2));
            Assert.That(result.Xml, Does.Contain("2005/01/reportdefinition"));
            Assert.That(result.Xml, Does.Not.Contain("2016/01/reportdefinition"));
            Assert.That(result.Xml, Does.Not.Contain("rd:ReportUnitType"));
        }

        [Test]
        public async Task Fix_RoundTrip_FixedFileParsesWithFewerOrEqualFindingsThanOriginal()
        {
            string originalPath = Fixture("fixable-2016ns.rdl");
            string xml = await File.ReadAllTextAsync(originalPath);
            var result = RdlFixer.Fix(xml, stripUnknown: false);

            string fixedPath = Path.Combine(Path.GetTempPath(), $"rdldoctor-fixed-{System.Guid.NewGuid():N}.rdl");
            await File.WriteAllTextAsync(fixedPath, result.Xml);
            try
            {
                var beforeFindings = await CompatibilityChecker.CheckAsync(originalPath);
                var afterFindings = await CompatibilityChecker.CheckAsync(fixedPath);

                Assert.That(afterFindings.Count, Is.LessThan(beforeFindings.Count));
                Assert.That(afterFindings.Any(f => f.Id == "DOC002"), Is.False,
                    "the namespace note should be gone once normalized to 2005");
            }
            finally
            {
                File.Delete(fixedPath);
            }
        }

        [Test]
        public void Fix_NeverRemovesTablixEvenWithStripUnknown()
        {
            string xml = File.ReadAllText(Fixture("unsupported-2016.rdl"));
            var result = RdlFixer.Fix(xml, stripUnknown: true,
                new System.Collections.Generic.HashSet<string> { "Tablix", "Gauge", "FunkyWidget" });

            Assert.That(result.Xml, Does.Contain("<Tablix"));
            Assert.That(result.Xml, Does.Not.Contain("<Gauge"));
            Assert.That(result.Xml, Does.Not.Contain("<FunkyWidget"));
        }
    }
}
