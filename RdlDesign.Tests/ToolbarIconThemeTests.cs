// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;
using Majorsilence.Forms.Headless;
using Majorsilence.Reporting.RdlViewer;
using NUnit.Framework;
using Bitmap = Majorsilence.Forms.Drawing.Bitmap;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #526: the designer's older toolbar bitmaps were drawn on opaque white tiles, which showed as
    // white boxes on a dark toolbar. On a dark theme they are shown off their tiles, in readable colours.
    [TestFixture]
    [NonParallelizable]     // Theme is process-wide
    public class ToolbarIconThemeTests
    {
        [TearDown]
        public void ResetTheme() => Theme.SetBuiltInTheme(BuiltInTheme.Default);

        private static readonly Color Text = Color.FromArgb(0xE4, 0xE4, 0xE8);

        private static double Luminance(Color c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;

        [Test]
        public void DarkPixel_WhiteGoes_BlackTakesTheTextColour()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ToolbarIconTheme.DarkPixel(Color.White, Text).A, Is.EqualTo(0));
                Assert.That(ToolbarIconTheme.DarkPixel(Color.Black, Text).ToArgb(), Is.EqualTo(Text.ToArgb()));

                // an anti-aliased grey edge: the text colour, half opaque
                var grey = ToolbarIconTheme.DarkPixel(Color.FromArgb(128, 128, 128), Text);
                Assert.That(Color.FromArgb(grey.R, grey.G, grey.B).ToArgb(), Is.EqualTo(Text.ToArgb()));
                Assert.That(grey.A, Is.InRange(120, 135));
            });
        }

        [Test]
        public void DarkPixel_DarkColours_AreLightened_KeepingTheirHue()
        {
            var blue = ToolbarIconTheme.DarkPixel(Color.Blue, Text);
            var red = ToolbarIconTheme.DarkPixel(Color.Red, Text);

            Assert.Multiple(() =>
            {
                Assert.That(blue.A, Is.EqualTo(255));
                Assert.That(Luminance(blue), Is.GreaterThanOrEqualTo(165));
                Assert.That(blue.B, Is.GreaterThan(blue.R).And.GreaterThan(blue.G), "still blue");
                Assert.That(Luminance(red), Is.GreaterThanOrEqualTo(165));
                Assert.That(red.R, Is.GreaterThan(red.G).And.GreaterThan(red.B), "still red");

                // a light colour is already readable and is left as it is
                var yellow = ToolbarIconTheme.DarkPixel(Color.FromArgb(255, 220, 0), Text);
                Assert.That(yellow.ToArgb(), Is.EqualTo(Color.FromArgb(255, 220, 0).ToArgb()));
            });
        }

        private static IEnumerable<ToolStripItem> Items(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is ToolStrip strip)
                    foreach (ToolStripItem item in strip.Items)
                        yield return item;
                foreach (var item in Items(c))
                    yield return item;
            }
        }

        private static ToolStripItem Button(RdlUserControl designer, string tag)
            => Items(designer).Single(i => (i.Tag as string) == tag);

        private static Color Corner(ToolStripItem item) => ((Bitmap)item.Image).GetPixel(0, 0);

        [Test]
        public void DefaultTheme_KeepsTheOriginalBitmaps()
        {
            using var designer = new RdlUserControl();

            Assert.That(Corner(Button(designer, "Textbox")).A, Is.EqualTo(255), "drawn on its white tile, as always");
            Assert.That(Corner(Button(designer, "PDF")).A, Is.EqualTo(255));
        }

        [Test]
        public void DarkTheme_TakesTheBitmapsOffTheirTiles_AndALightThemeRestoresThem()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);
            using var designer = new RdlUserControl();
            var textbox = Button(designer, "Textbox");
            var pdf = Button(designer, "PDF");

            Assert.That(Corner(textbox).A, Is.EqualTo(0));
            Assert.That(Corner(pdf).A, Is.EqualTo(0));

            // the Tango icons already have transparent backgrounds and are not touched
            var cut = Items(designer).Single(i => ReferenceEquals(i.Image, Properties.Resources.edit_cut));
            Assert.That(cut, Is.Not.Null);

            Theme.SetBuiltInTheme(BuiltInTheme.Default);
            ToolbarIconTheme.Apply(designer, Items(designer).OfType<ToolStripItem>().Select(i => i.Owner).Distinct().ToArray());

            Assert.That(Corner(textbox).A, Is.EqualTo(255));
            Assert.That(Corner(pdf).A, Is.EqualTo(255));
        }

        [Test]
        public void ThemeChange_SwapsTheBitmaps_OfAShownDesigner()
        {
            HeadlessRenderer.Use();
            using var form = new Form { Size = new Size(900, 300) };
            var designer = new RdlUserControl { Dock = DockStyle.Fill };
            form.Controls.Add(designer);
            form.Show();
            var textbox = Button(designer, "Textbox");
            Assert.That(Corner(textbox).A, Is.EqualTo(255));

            Theme.SetBuiltInTheme(BuiltInTheme.Dark);
            Application.DoEvents();

            Assert.That(Corner(textbox).A, Is.EqualTo(0));
        }

        [Test]
        public void DesignerWithADarkScope_InALightApplication_GetsTheDarkVariants()
        {
            using var designer = new RdlUserControl();
            designer.UseTheme("Dark");
            ToolbarIconTheme.Apply(designer, Items(designer).Select(i => i.Owner).Distinct().ToArray());

            Assert.That(ChromeColors.IsDarkTheme, Is.False, "the application stays light");
            Assert.That(Corner(Button(designer, "Textbox")).A, Is.EqualTo(0));
        }

        [Test]
        public void AnImageTheHostReplaced_IsLeftAlone()
        {
            using var designer = new RdlUserControl();
            var textbox = Button(designer, "Textbox");
            var custom = new Bitmap(16, 16);
            textbox.Image = custom;

            Theme.SetBuiltInTheme(BuiltInTheme.Dark);
            ToolbarIconTheme.Apply(designer, Items(designer).Select(i => i.Owner).Distinct().ToArray());

            Assert.That(textbox.Image, Is.SameAs(custom));
        }
    }
}
