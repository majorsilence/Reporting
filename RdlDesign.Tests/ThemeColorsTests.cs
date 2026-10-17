// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlViewer;
using NUnit.Framework;
using Bitmap = Majorsilence.Forms.Drawing.Bitmap;
using Graphics = Majorsilence.Forms.Drawing.Graphics;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #448: the design surface's chrome follows the Majorsilence.Forms theme; the paper does not.
    [TestFixture]
    [NonParallelizable]     // Theme is process-wide
    public class ThemeColorsTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  <Body>
    <Height>4in</Height>
  </Body>
  <Height>4in</Height>
</Report>";

        [TearDown]
        public void ResetTheme() => Theme.SetBuiltInTheme(BuiltInTheme.Default);

        [Test]
        public void DefaultTheme_KeepsClassicColors()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Default);

            Assert.Multiple(() =>
            {
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
                Assert.That(DesignerColors.Band.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
                Assert.That(DesignerColors.RulerText.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
                Assert.That(DesignerColors.Margin.ToArgb(), Is.EqualTo(Color.Azure.ToArgb()));
                Assert.That(ChromeColors.PageOutline.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
            });
        }

        [Test]
        public void DarkTheme_ChromeFollowsTokens_PageColorsStay()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);

            Assert.Multiple(() =>
            {
                Assert.That(ChromeColors.IsDarkTheme, Is.True);
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(ChromeColors.FromToken(Theme.ControlMidColor).ToArgb()));
                Assert.That(DesignerColors.RulerText.ToArgb(), Is.EqualTo(ChromeColors.FromToken(Theme.ForegroundColor).ToArgb()));
                // drawn on the white paper, so unchanged
                Assert.That(DesignerColors.Margin.ToArgb(), Is.EqualTo(Color.Azure.ToArgb()));
                Assert.That(DesignerColors.Handle.ToArgb(), Is.EqualTo(Color.Black.ToArgb()));
            });
        }

        [Test]
        public void CssVariable_OverridesColor()
        {
            Theme.LoadFromCss(@"@theme ""RdlTest"" extends Light;
:root {
  --rdl-area-background: #123456;
  --rdl-handle: #ff0000;
}");

            Assert.Multiple(() =>
            {
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.FromArgb(0x12, 0x34, 0x56).ToArgb()));
                Assert.That(DesignerColors.Handle.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
                Assert.That(DesignerColors.Band.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()), "variables it does not set keep their default");
            });
        }

        [Test]
        public async Task DarkTheme_RendersAreaBehindPageDark_PaperWhite()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);
            using var bmp = await Render();

            Color behind = bmp.GetPixel(1150, 100);     // right of the 8.5in page
            Color paper = bmp.GetPixel(300, 100);        // inside the body's work area

            Assert.Multiple(() =>
            {
                Assert.That(behind.ToArgb(), Is.EqualTo(ChromeColors.FromToken(Theme.ControlMidColor).ToArgb()));
                Assert.That(paper.ToArgb(), Is.EqualTo(Color.White.ToArgb()));
            });
        }

        [Test]
        public async Task DefaultTheme_RendersClassicAreaBehindPage()
        {
            using var bmp = await Render();

            Assert.That(bmp.GetPixel(1150, 100).ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
        }

        [Test]
        public void EveryColor_UsesAKnownVariable()
        {
            // ChromeColors.Resolve asserts (Debug) that its variable is in ChromeColors.Variables, which
            // is what keeps a theme's --rdl-* declarations from being reported as unused
            foreach (var type in new[] { typeof(ChromeColors), typeof(DesignerColors) })
                foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    if (prop.PropertyType == typeof(Color))
                        Assert.DoesNotThrow(() => prop.GetValue(null), $"{type.Name}.{prop.Name}");
        }

        [Test]
        public void Apply_BuiltIn()
        {
            var result = DesignerTheme.Apply("dark", null);

            Assert.That(result.FellBack, Is.False);
            Assert.That(result.Messages, Is.Empty);
            Assert.That(ChromeColors.IsDarkTheme, Is.True);
        }

        [Test]
        public void Apply_MissingCssFile_FallsBackToDefault()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);

            var result = DesignerTheme.Apply(DesignerTheme.Custom, Path.Combine(Path.GetTempPath(), "no-such-theme-448.css"));

            Assert.That(result.FellBack, Is.True);
            Assert.That(ChromeColors.IsDarkTheme, Is.False);
        }

        [Test]
        public void ApplyCss_WithErrors_FallsBackAndReportsThem()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);

            var result = DesignerTheme.ApplyCss(":root { --rdl-area-background: #123456; } Button { colour: red; }");

            Assert.That(result.FellBack, Is.True);
            Assert.That(result.Messages, Has.Some.Contains("colour"));
            Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
        }

        [Test]
        public void ApplyCss_KnownRdlVariable_IsNotReportedUnused_ButATypoIs()
        {
            var result = DesignerTheme.ApplyCss(":root { --rdl-area-background: #123456; --rdl-area-bakground: #654321; }");

            Assert.That(result.FellBack, Is.False);
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.That(result.Messages[0], Does.Contain("--rdl-area-bakground"));
        }

        [Test]
        public void ShippedThemes_ParseCleanly()
        {
            string dir = FindShippedThemes();
            var files = Directory.GetFiles(dir, "*.css");
            Assert.That(files, Is.Not.Empty);

            foreach (string file in files)
            {
                var result = DesignerTheme.Apply(DesignerTheme.Custom, file);
                Assert.That(result.FellBack, Is.False, file);
                Assert.That(result.Messages, Is.Empty, file);
            }
        }

        private static string FindShippedThemes()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                string candidate = Path.Combine(d.FullName, "ReportDesigner", "Themes");
                if (Directory.Exists(candidate))
                    return candidate;
            }
            Assert.Fail("ReportDesigner/Themes not found above the test directory");
            return null;
        }

        private static async Task<Bitmap> Render()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Rdl);
            var draw = new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
            var bmp = new Bitmap(1200, 800);
            using (var g = Graphics.FromImage(bmp))
                await draw.Draw(g, 0, 0, new Rectangle(0, 0, 1200, 800));
            return bmp;
        }
    }
}
