// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // #298: the designer exposes a report's embedded code and referenced assemblies
    // (CodeModules) / class instances through the Code and Modules and Classes controls.
    [TestFixture]
    public class CodeAndAssembliesTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Code>Public Function Twice(ByVal x As Integer) As Integer
  Return x * 2
End Function</Code>
  <CodeModules><CodeModule>System.Drawing, Version=4.0.0.0</CodeModule></CodeModules>
  <Classes><Class><ClassName>My.Helper</ClassName><InstanceName>helper</InstanceName></Class></Classes>
  <Body><Height>1in</Height><ReportItems /></Body>
  <Width>4in</Width>
</Report>";

        private static XmlDocument Parse(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc;
        }

        private static string Text(XmlDocument doc, string xpath)
        {
            var nsm = new XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("r", doc.DocumentElement.NamespaceURI);
            return doc.SelectSingleNode(xpath, nsm)?.InnerText;
        }

        [Test]
        public void ModulesClassesCtl_LoadsAndReapplies_ExistingModulesAndClasses()
        {
            using var mc = new MDIChild(800, 600) { SourceRdl = Rdl };
            using var ctl = new ModulesClassesCtl(mc.DrawCtl);

            Assert.That(ctl.IsValid(), Is.True);
            ctl.Apply();

            var doc = Parse(mc.SourceRdl);
            Assert.That(Text(doc, "//r:CodeModules/r:CodeModule"), Is.EqualTo("System.Drawing, Version=4.0.0.0"));
            Assert.That(Text(doc, "//r:Classes/r:Class/r:ClassName"), Is.EqualTo("My.Helper"));
            Assert.That(Text(doc, "//r:Classes/r:Class/r:InstanceName"), Is.EqualTo("helper"));
        }

        [Test]
        public void ModulesClassesCtl_ApplyOnReportWithoutThem_AddsNothing()
        {
            const string bare = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Body><Height>1in</Height><ReportItems /></Body>
  <Width>4in</Width>
</Report>";
            using var mc = new MDIChild(800, 600) { SourceRdl = bare };
            using var ctl = new ModulesClassesCtl(mc.DrawCtl);
            ctl.Apply();

            var doc = Parse(mc.SourceRdl);
            Assert.That(Text(doc, "//r:CodeModules"), Is.Null);
            Assert.That(Text(doc, "//r:Classes"), Is.Null);
        }

        [Test]
        public void CodeCtl_LoadsAndReapplies_EmbeddedCode()
        {
            using var mc = new MDIChild(800, 600) { SourceRdl = Rdl };
            using var ctl = new CodeCtl(mc.DrawCtl);
            ctl.Apply();

            var doc = Parse(mc.SourceRdl);
            Assert.That(Text(doc, "//r:Code"), Does.Contain("Function Twice"));
        }
    }
}
