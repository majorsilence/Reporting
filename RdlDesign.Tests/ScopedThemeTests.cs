// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;
using Majorsilence.Forms.Headless;
using Majorsilence.Reporting.RdlViewer;
using NUnit.Framework;
using SkiaSharp;
using Bitmap = Majorsilence.Forms.Drawing.Bitmap;
using Graphics = Majorsilence.Forms.Drawing.Graphics;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #448: a designer with its own ThemeScope is themed by it while the application keeps its theme.
    [TestFixture]
    [NonParallelizable]     // Theme and HeadlessRenderer are process-wide
    public class ScopedThemeTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>4in</Width>
  <PageWidth>4in</PageWidth>
  <PageHeight>6in</PageHeight>
  <Body>
    <Height>3in</Height>
  </Body>
</Report>";

        [SetUp]
        [TearDown]
        public void ResetTheme() => Theme.SetBuiltInTheme(BuiltInTheme.Default);

        [Test]
        public void ScopedColors_FollowTheScope_ApplicationKeepsItsOwn()
        {
            var scope = new ThemeScope();
            Assert.That(DesignerTheme.ApplyTo(scope, "Dark", null).FellBack, Is.False);

            using (scope.Enter())
            {
                Assert.That(ChromeColors.IsDarkTheme, Is.True);
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(ChromeColors.FromToken(scope.ControlMidColor).ToArgb()));
            }
            Assert.That(ChromeColors.IsDarkTheme, Is.False);
            Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
        }

        [Test]
        public void ScopedCssVariables_ApplyOnlyInTheScope()
        {
            var scope = new ThemeScope();
            var result = DesignerTheme.ApplyCssTo(scope, ":root { --rdl-area-background: #123456; }");
            Assert.That(result.FellBack, Is.False);
            Assert.That(result.Messages, Is.Empty);

            using (scope.Enter())
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.FromArgb(0x12, 0x34, 0x56).ToArgb()));
            Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
        }

        [Test]
        public void ApplicationCssVariables_ReachAScopeThatOnlyTweaksTokens_NotOneWithItsOwnTheme()
        {
            DesignerTheme.ApplyCss(":root { --rdl-area-background: #123456; }");
            var tweak = new ThemeScope { AccentColor = SKColors.Orange };
            var dark = new ThemeScope();
            dark.SetBuiltInTheme(BuiltInTheme.Dark);

            using (tweak.Enter())
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(Color.FromArgb(0x12, 0x34, 0x56).ToArgb()));
            using (dark.Enter())
                Assert.That(DesignerColors.AreaBackground.ToArgb(), Is.EqualTo(ChromeColors.FromToken(dark.ControlMidColor).ToArgb()));
        }

        [Test]
        public async Task AsyncRender_UsesTheRenderScope_AfterAwaiting()
        {
            var scope = new ThemeScope();
            scope.SetBuiltInTheme(BuiltInTheme.Dark);

            Color scoped = await RenderBehindPage(scope);
            Color plain = await RenderBehindPage(null);

            Assert.That(scoped.ToArgb(), Is.EqualTo(ChromeColors.FromToken(scope.ControlMidColor).ToArgb()));
            Assert.That(plain.ToArgb(), Is.EqualTo(Color.LightGray.ToArgb()));
            Assert.That(ThemeScope.Current, Is.Null, "the render scope does not leak to the caller");
        }

        private static async Task<Color> RenderBehindPage(ThemeScope scope)
        {
            ChromeColors.SetRenderScope(scope);
            await Task.Yield();                 // ThemeScope.Current is long gone by now
            var doc = new XmlDocument();
            doc.LoadXml(Rdl);
            var draw = new DesignXmlDraw { Width = 800, Height = 400, ReportDocument = doc };
            using var bmp = new Bitmap(800, 400);
            using (var g = Graphics.FromImage(bmp))
                await draw.Draw(g, 0, 0, new Rectangle(0, 0, 800, 400));
            return bmp.GetPixel(750, 100);      // right of the 4in page
        }

        [Test]
        public void ScopedDesigner_InAnUnscopedWindow_PaintsWithItsScope()
        {
            HeadlessRenderer.Use();
            var scope = new ThemeScope();
            scope.SetBuiltInTheme(BuiltInTheme.Dark);
            var dark = new SKColor(scope.ControlMidColor.Red, scope.ControlMidColor.Green, scope.ControlMidColor.Blue);

            using var form = new Form { Size = new Size(900, 500) };
            var design = new DesignCtl { Dock = DockStyle.Fill, ThemeScope = scope };
            form.Controls.Add(design);
            design.ReportSource = Rdl;
            form.Show();

            int darkPixels = 0;
            for (int attempt = 0; attempt < 20 && darkPixels == 0; attempt++)
            {
                Application.DoEvents();         // let the asynchronous render finish and repaint
                using var png = SKBitmap.Decode(HeadlessRenderer.CapturePng(form));
                darkPixels = CountColumn(png, png.Width - 60, dark);
            }

            Assert.That(darkPixels, Is.GreaterThan(50), "the area behind the page takes the designer's scope");
            Assert.That(Theme.BackgroundColor, Is.Not.EqualTo(scope.BackgroundColor), "the application theme is untouched");
        }

        [Test]
        public void UseTheme_GivesTheDesignerItsOwnScope()
        {
            using var designer = new RdlUserControl();

            var result = designer.UseTheme("Dark");

            Assert.That(result.FellBack, Is.False);
            Assert.That(designer.ThemeScope, Is.Not.Null);
            using (designer.ThemeScope.Enter())
                Assert.That(ChromeColors.IsDarkTheme, Is.True);
            Assert.That(ChromeColors.IsDarkTheme, Is.False);
        }

        [Test]
        public void RdlUserControl_CanBeCreated_WithItsToolbarImages()
        {
            // Properties.Resources cast the System.Drawing.Bitmap a resx file reference produces to
            // Majorsilence.Forms.Drawing.Bitmap, so the embeddable designer threw on construction.
            Assert.That(Properties.Resources.document_new, Is.Not.Null);
            Assert.DoesNotThrow(() => new RdlUserControl().Dispose());
        }

        private static int CountColumn(SKBitmap bmp, int x, SKColor color) =>
            Enumerable.Range(0, bmp.Height).Count(y =>
            {
                var c = bmp.GetPixel(x, y);
                return c.Red == color.Red && c.Green == color.Green && c.Blue == color.Blue;
            });
    }
}
