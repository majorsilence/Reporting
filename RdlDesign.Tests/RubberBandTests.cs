// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using Majorsilence.Forms;
using Majorsilence.Forms.Drawing;
using NUnit.Framework;
using Point = System.Drawing.Point;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // The drag rectangle used to be drawn with an XOR screen frame, which Majorsilence.Forms cannot do,
    // so DrawPanelRubberBand was an empty method and nothing showed while dragging. It is now state that
    // the surface's paint draws over the rendered report.
    [TestFixture]
    public class RubberBandTests
    {
        private static T Call<T>(DesignCtl d, string name, params object[] args) =>
            (T)typeof(DesignCtl).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(d, args);

        private static void Call(DesignCtl d, string name, params object[] args) =>
            typeof(DesignCtl).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(d, args);

        private static bool HasBand(DesignCtl d) =>
            typeof(DesignCtl).GetField("_rubberBand", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(d) != null;

        private static SkiaSharp.SKColor PixelAfterOverlay(DesignCtl d, int x, int y)
        {
            using var bmp = new Bitmap(400, 300);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, 400, 300);
                Call(d, "DrawRubberBandOverlay", g);
            }
            using var sk = bmp.ToSKBitmap();
            return sk.GetPixel(x, y);
        }

        [Test]
        public void RubberBand_IsPaintedOverTheSurface_ThenCleared()
        {
            var design = new DesignCtl { Width = 400, Height = 300 };

            var before = PixelAfterOverlay(design, 150, 140);
            Assert.That(before.Red, Is.EqualTo(255), "nothing drawn without a band");

            Call(design, "DrawPanelRubberBand", new Point(100, 100), new Point(200, 180));
            Assert.That(HasBand(design), Is.True);
            var inside = PixelAfterOverlay(design, 150, 140);
            Assert.That(inside.Red, Is.LessThan(255), "the band's fill is drawn inside the rectangle");
            var outside = PixelAfterOverlay(design, 300, 250);
            Assert.That(outside.Red, Is.EqualTo(255), "and nothing outside it");

            Call(design, "ClearRubberBand");
            Assert.That(HasBand(design), Is.False);
            Assert.That(PixelAfterOverlay(design, 150, 140).Red, Is.EqualTo(255));
        }

        [Test]
        public void RubberBand_DraggedBackwards_StillCoversTheRectangle()
        {
            var design = new DesignCtl { Width = 400, Height = 300 };
            Call(design, "DrawPanelRubberBand", new Point(200, 180), new Point(100, 100));
            Assert.That(PixelAfterOverlay(design, 150, 140).Red, Is.LessThan(255));
        }
    }
}
