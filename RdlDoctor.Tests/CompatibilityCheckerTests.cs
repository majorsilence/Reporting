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
    public class CompatibilityCheckerTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Fixture(string name) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

        [Test]
        public async Task CleanReport_HasNoErrorsOrWarnings_OnlyOptionalInfo()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("clean-2005.rdl"));

            Assert.That(findings.Any(f => f.Severity == FindingSeverity.Error), Is.False);
            Assert.That(findings.Any(f => f.Severity == FindingSeverity.Warning), Is.False);
        }

        [Test]
        public async Task Tablix_IsFlaggedAsUnsupported()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC001" && f.Severity == FindingSeverity.Error), Is.True);
        }

        [Test]
        public async Task Non2005Namespace_ProducesInfoOnly_NotErrorOrWarning()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            var namespaceFindings = findings.Where(f => f.Id == "DOC002").ToList();

            Assert.That(namespaceFindings, Is.Not.Empty);
            Assert.That(namespaceFindings.All(f => f.Severity == FindingSeverity.Info), Is.True,
                "the engine is namespace-agnostic, so a non-2005 namespace must never be reported as an error or warning");
        }

        [Test]
        public async Task Gauge_IsFlaggedAsUnsupported()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC003" && f.Message.Contains("Gauge")), Is.True);
        }

        [Test]
        public async Task UnknownElements_AreRelayedFromTheRealEngineParse_Deduplicated()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            var unknownElementFindings = findings.Where(f => f.Id == "DOC004" && f.Message.Contains("Unknown element")).ToList();

            // Tablix, Gauge, and FunkyWidget each appear exactly once in the fixture -- each
            // should produce exactly one DOC004 finding, not duplicated.
            Assert.That(unknownElementFindings.Count(f => f.Message.Contains("<Tablix>")), Is.EqualTo(1));
            Assert.That(unknownElementFindings.Count(f => f.Message.Contains("<Gauge>")), Is.EqualTo(1));
            Assert.That(unknownElementFindings.Count(f => f.Message.Contains("<FunkyWidget>")), Is.EqualTo(1));
        }

        [Test]
        public async Task LookupFunction_IsFlaggedAsUnsupported()
        {
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC005" && f.Message.Contains("Lookup")), Is.True);
        }

        [Test]
        public async Task SevereParseErrors_AreSurfacedAsErrorFinding()
        {
            // The fixture's Lookup() call references a nonexistent second dataset, which the
            // engine treats as a severity-8 ("can't run report") error -- confirm that surfaces
            // as an Error-level DOC004 finding, not silently dropped.
            var findings = await CompatibilityChecker.CheckAsync(Fixture("unsupported-2016.rdl"));
            Assert.That(findings.Any(f => f.Id == "DOC004" && f.Severity == FindingSeverity.Error), Is.True);
        }

        [Test]
        public void MalformedXml_ThrowsRatherThanReturningAFinding()
        {
            // Program.cs relies on this throwing so it can distinguish "not even XML" (exit 2)
            // from ordinary compatibility findings (exit 1).
            Assert.ThrowsAsync<System.Xml.XmlException>(() => CompatibilityChecker.CheckAsync(Fixture("malformed.rdl")));
        }
    }
}
