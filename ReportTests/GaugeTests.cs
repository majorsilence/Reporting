using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms.Drawing;
using Majorsilence.Reporting.Cri;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The radial and linear gauge custom report items (issue #407). They draw a bitmap from the
    /// properties the report supplies, so the tests look at where colours land in it.
    /// </summary>
    [TestFixture]
    public class GaugeTests
    {
        private static Bitmap Draw(ICustomReportItem gauge, int width, int height, params (string Name, object Value)[] props)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var (name, value) in props)
                dictionary[name] = value;
            gauge.SetProperties(dictionary);
            var bm = new Bitmap(width, height);
            gauge.DrawImage(ref bm);
            return bm;
        }

        /// <summary>Pixels close to a colour, as (x, y) points.</summary>
        private static List<(int X, int Y)> Pixels(Bitmap bm, byte r, byte g, byte b, int tolerance = 40)
        {
            var found = new List<(int, int)>();
            for (int y = 0; y < bm.Height; y++)
            {
                for (int x = 0; x < bm.Width; x++)
                {
                    var c = bm.GetPixel(x, y);
                    if (c.A > 200 && Math.Abs(c.R - r) <= tolerance && Math.Abs(c.G - g) <= tolerance && Math.Abs(c.B - b) <= tolerance)
                        found.Add((x, y));
                }
            }
            return found;
        }

        // the default pointer colour and the colours Green, Yellow and Red resolve to
        private static readonly byte[] Pointer = { 0xC0, 0x39, 0x2B };
        private static readonly byte[] Green = { 0x2E, 0xA0, 0x43 };
        private static readonly byte[] Yellow = { 0xF2, 0xB1, 0x34 };

        private static (double X, double Y) Centroid(List<(int X, int Y)> points)
            => (points.Average(p => (double)p.X), points.Average(p => (double)p.Y));

        // --- radial ------------------------------------------------------------------------------------------

        [Test]
        public void Radial_DrawsAtTheRequestedSizeAndIsNotBlank()
        {
            using var bm = Draw(new GaugeRadial(), 240, 240, ("Value", 50));

            Assert.That(bm.Width, Is.EqualTo(240));
            Assert.That(bm.Height, Is.EqualTo(240));
            Assert.That(Pixels(bm, Pointer[0], Pointer[1], Pointer[2]), Is.Not.Empty, "the needle");
        }

        [Test]
        public void Radial_TheNeedleTurnsWithTheValue()
        {
            // The dial starts at the lower left and sweeps clockwise to the lower right, so a low value
            // leaves the needle on the left and a high one on the right.
            using var low = Draw(new GaugeRadial(), 240, 240, ("Value", 5));
            using var high = Draw(new GaugeRadial(), 240, 240, ("Value", 95));
            using var middle = Draw(new GaugeRadial(), 240, 240, ("Value", 50));

            var lowTip = Centroid(Pixels(low, Pointer[0], Pointer[1], Pointer[2]));
            var highTip = Centroid(Pixels(high, Pointer[0], Pointer[1], Pointer[2]));
            var middleTip = Centroid(Pixels(middle, Pointer[0], Pointer[1], Pointer[2]));

            Assert.That(lowTip.X, Is.LessThan(120 - 10), "a low value points left");
            Assert.That(highTip.X, Is.GreaterThan(120 + 10), "a high value points right");
            Assert.That(Math.Abs(middleTip.X - 120), Is.LessThan(10), "the middle of the scale points straight up");
            Assert.That(middleTip.Y, Is.LessThan(120), "...above the centre");
        }

        [Test]
        public void Radial_RangesAreColouredAlongTheScale()
        {
            using var bm = Draw(new GaugeRadial(), 240, 240, ("Value", 50), ("Ranges", "0:50:Green;50:100:Yellow"));

            var green = Pixels(bm, Green[0], Green[1], Green[2]);
            var yellow = Pixels(bm, Yellow[0], Yellow[1], Yellow[2]);
            Assert.That(green, Is.Not.Empty);
            Assert.That(yellow, Is.Not.Empty);
            Assert.That(Centroid(green).X, Is.LessThan(120), "the first half of the scale is on the left");
            Assert.That(Centroid(yellow).X, Is.GreaterThan(120), "the second half is on the right");
        }

        [Test]
        public void Radial_StartAndSweepAngleLimitTheDial()
        {
            // a quarter-turn dial from the bottom: the scale is only on the left-hand side
            using var bm = Draw(new GaugeRadial(), 240, 240, ("Value", 50), ("StartAngle", 0), ("SweepAngle", 90),
                ("Ranges", "0:100:Green"));

            var green = Pixels(bm, Green[0], Green[1], Green[2]);
            Assert.That(green, Is.Not.Empty);
            Assert.That(green.Max(p => p.X), Is.LessThan(120 + 6), "no scale to the right of the vertical centre line");
            Assert.That(green.Min(p => p.Y), Is.GreaterThan(120 - 6), "and none above the horizontal one");
        }

        [Test]
        public void Radial_OutOfRangeValuesAreClampedNotFatal()
        {
            using var below = Draw(new GaugeRadial(), 200, 200, ("Value", -500), ("Min", 0), ("Max", 100));
            using var above = Draw(new GaugeRadial(), 200, 200, ("Value", 5000), ("Min", 0), ("Max", 100));

            Assert.That(Pixels(below, Pointer[0], Pointer[1], Pointer[2]), Is.Not.Empty);
            Assert.That(Pixels(above, Pointer[0], Pointer[1], Pointer[2]), Is.Not.Empty);
        }

        [Test]
        public void MissingOrBadProperties_FallBackToDefaultsInsteadOfThrowing()
        {
            Assert.DoesNotThrow(() => Draw(new GaugeRadial(), 120, 120).Dispose());
            Assert.DoesNotThrow(() => Draw(new GaugeLinear(), 120, 60).Dispose());
            Assert.DoesNotThrow(() => Draw(new GaugeRadial(), 120, 120, ("Value", "not a number"), ("Max", 0), ("Min", 0),
                ("Ranges", "junk;1:2;a:b:Red;3:4:NotAColour"), ("SweepAngle", 9999)).Dispose());
        }

        [Test]
        public void ATinyBitmapIsDrawnOrSkippedButNeverThrows()
        {
            Assert.DoesNotThrow(() => Draw(new GaugeRadial(), 4, 4, ("Value", 50)).Dispose());
            Assert.DoesNotThrow(() => Draw(new GaugeLinear(), 4, 4, ("Value", 50)).Dispose());
            Assert.DoesNotThrow(() => Draw(new GaugeRadial(), 1, 1, ("Value", 50)).Dispose());
        }

        // --- linear ------------------------------------------------------------------------------------------

        [Test]
        public void Linear_TheBarGrowsWithTheValue()
        {
            using var low = Draw(new GaugeLinear(), 300, 100, ("Value", 20));
            using var high = Draw(new GaugeLinear(), 300, 100, ("Value", 80));

            var lowBar = Pixels(low, Pointer[0], Pointer[1], Pointer[2]);
            var highBar = Pixels(high, Pointer[0], Pointer[1], Pointer[2]);
            Assert.That(lowBar, Is.Not.Empty);
            Assert.That(highBar.Max(p => p.X) - highBar.Min(p => p.X), Is.GreaterThan((lowBar.Max(p => p.X) - lowBar.Min(p => p.X)) * 2.5),
                "80 is four times as far along as 20");
            Assert.That(lowBar.Min(p => p.X), Is.EqualTo(highBar.Min(p => p.X)).Within(2), "both bars start at the same end");
        }

        [Test]
        public void Linear_VerticalRunsUpTheBitmap()
        {
            using var bm = Draw(new GaugeLinear(), 100, 300, ("Value", 75), ("Orientation", "Vertical"));

            var bar = Pixels(bm, Pointer[0], Pointer[1], Pointer[2]);
            Assert.That(bar, Is.Not.Empty);
            int height = bar.Max(p => p.Y) - bar.Min(p => p.Y);
            int width = bar.Max(p => p.X) - bar.Min(p => p.X);
            Assert.That(height, Is.GreaterThan(width * 5), "a tall thin bar");
            Assert.That(bar.Max(p => p.Y), Is.GreaterThan(200), "starting low in the bitmap (the minimum is at the bottom)");
        }

        [Test]
        public void Linear_RangesFillTheTrackInOrder()
        {
            using var bm = Draw(new GaugeLinear(), 300, 100, ("Value", 50), ("Ranges", "0:50:Green;50:100:Yellow"));

            Assert.That(Centroid(Pixels(bm, Green[0], Green[1], Green[2])).X, Is.LessThan(Centroid(Pixels(bm, Yellow[0], Yellow[1], Yellow[2])).X));
        }

        [Test]
        public void TheDesignerImageIsDrawn()
        {
            var radial = (ICustomReportItem)new GaugeRadial();
            var bm = new Bitmap(100, 100);
            radial.DrawDesignerImage(ref bm);
            Assert.That(Pixels(bm, Green[0], Green[1], Green[2]), Is.Not.Empty, "the sample dial has coloured ranges");
            bm.Dispose();
        }
    }
}
