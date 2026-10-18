// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using Majorsilence.Forms;
using Majorsilence.Reporting.Rdl.Utility;
using Majorsilence.Reporting.RdlDesign.Resources;
using Majorsilence.Reporting.RdlViewer;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #448: white-labelling -- the designer and viewer under an application's own name.
    [TestFixture]
    [NonParallelizable]     // ReportingBranding.Current and Paths are process-wide
    public class BrandingTests
    {
        [TearDown]
        public void Reset()
        {
            ReportingBranding.Current = new ReportingBranding();
            Paths.ApplicationFolderName = "MajorsilenceReporting";
        }

        [Test]
        public void SettingsFolderName_MovesTheSettingsFolder()
        {
            string name = "BrandingTest" + Guid.NewGuid().ToString("N");
            ReportingBranding.Current.SettingsFolderName = name;
            try
            {
                Assert.That(Path.GetFileName(Paths.MajorsilenceRoamingFolder()), Is.EqualTo(name));
                Assert.That(Path.GetFileName(Paths.MajorsilenceLocalFolder()), Is.EqualTo(name));
            }
            finally
            {
                foreach (var folder in new[] { Environment.SpecialFolder.ApplicationData, Environment.SpecialFolder.LocalApplicationData })
                {
                    string dir = Path.Combine(Environment.GetFolderPath(folder), name);
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
            }
        }

        [TestCase("")]
        [TestCase("a/b")]
        [TestCase("..\\x")]
        public void SettingsFolderName_MustBeAFolderName(string name)
        {
            Assert.Throws<ArgumentException>(() => Paths.ApplicationFolderName = name);
        }

        [Test]
        public void Unbranded_KeepsTheBuiltInText()
        {
            using var about = new DialogAbout();

            Assert.That(Find<Label>(about, "label8").Text, Is.EqualTo("Majorsilence Reporting Designer"));
            Assert.That(Find<TextBox>(about, "tbLicense").Text, Is.EqualTo(Strings.DialogAbout_DialogAbout_About));
            Assert.That(ReportingBranding.Current.EffectiveHelpUrl, Does.Contain("github.com/majorsilence/Reporting"));
        }

        [Test]
        public void AboutBox_ShowsTheBranding_AndKeepsTheLicence()
        {
            ReportingBranding.Current = new ReportingBranding
            {
                DesignerName = "Acme Report Studio",
                AboutText = "Copyright Acme Ltd.",
                WebsiteUrl = "https://acme.example/",
                SupportUrl = "https://acme.example/support",
            };

            using var about = new DialogAbout();

            Assert.That(Find<Label>(about, "label8").Text, Is.EqualTo("Acme Report Studio"));
            Assert.That(Find<LinkLabel>(about, "linkLabel4").Tag, Is.EqualTo("https://acme.example/"));
            Assert.That(Find<LinkLabel>(about, "linkLabel3").Tag, Is.EqualTo("https://acme.example/support"));
            string licence = Find<TextBox>(about, "tbLicense").Text;
            Assert.That(licence, Does.StartWith("Copyright Acme Ltd."));
            Assert.That(licence, Does.EndWith(Strings.DialogAbout_DialogAbout_About), "the fyiReporting licence notice is always shown");
        }

        [Test]
        public void DesignerWindow_TakesTheBranding()
        {
            ReportingBranding.Current = new ReportingBranding { DesignerName = "Acme Report Studio", ShowAboutBox = false };

            using var designer = new RdlDesigner("BrandingTest", false);

            Assert.That(designer.Text, Is.EqualTo("Acme Report Studio"));
            var about = (ToolStripMenuItem)typeof(RdlDesigner)
                .GetField("aboutToolStripMenuItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(designer)!;
            Assert.That(about.Visible, Is.False);
        }

        private static T Find<T>(Form form, string name) where T : Control =>
            (T)form.Controls.Find(name, true).Single();
    }
}
