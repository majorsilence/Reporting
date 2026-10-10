using NUnit.Framework;
using System.Collections.Generic;
using Majorsilence.Reporting.Cri;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Forms.Drawing;

namespace ReportTests
{
    /// <summary>
    /// Issue #204: barcodes were always centered in their box. The optional "Align" property
    /// moves the symbol to the left or right edge.
    /// </summary>
    [TestFixture]
    public class BarCodeAlignTest
    {
        private const int Width = 600;
        private const int Height = 100;

        private static Bitmap Render(string align)
        {
            ICustomReportItem barcode = new BarCode128();
            var props = new Dictionary<string, object> { { "Code", "ALIGN-204" } };
            if (align != null)
                props["Align"] = align;
            barcode.SetProperties(props);

            var bm = new Bitmap(Width, Height);
            barcode.DrawImage(ref bm);
            return bm;
        }

        private static (int first, int last) DarkColumns(Bitmap bm)
        {
            int first = -1, last = -1;
            for (int x = 0; x < bm.Width; x++)
            {
                for (int y = 0; y < bm.Height; y++)
                {
                    var c = bm.GetPixel(x, y);
                    if (c.A > 0 && c.R < 128 && c.G < 128 && c.B < 128)
                    {
                        if (first < 0) first = x;
                        last = x;
                        break;
                    }
                }
            }
            return (first, last);
        }

        [Test]
        public void Left_PlacesBarcodeAtLeftEdge()
        {
            using var bm = Render("Left");
            var (first, last) = DarkColumns(bm);

            Assert.That(first, Is.EqualTo(0));
            Assert.That(last, Is.LessThan(bm.Width - 1), "Barcode should not span the whole box");
            Assert.That(bm.Width - 1 - last, Is.GreaterThan(first), "Free space should be on the right");
        }

        [Test]
        public void Right_PlacesBarcodeAtRightEdge()
        {
            using var bm = Render("Right");
            var (first, last) = DarkColumns(bm);

            Assert.That(last, Is.EqualTo(bm.Width - 1));
            Assert.That(first, Is.GreaterThan(0), "Barcode should not span the whole box");
        }

        [TestCase(null)]
        [TestCase("Center")]
        public void CenterAndDefault_LeaveBarcodeCentered(string align)
        {
            using var bm = Render(align);
            var (first, last) = DarkColumns(bm);

            int leftGap = first;
            int rightGap = bm.Width - 1 - last;
            Assert.That(leftGap, Is.GreaterThan(0));
            Assert.That(rightGap, Is.GreaterThan(0));
            Assert.That(System.Math.Abs(leftGap - rightGap), Is.LessThanOrEqualTo(bm.Width / 20));
        }

        [Test]
        public void Left_AndRight_HaveSameBarcodeWidth()
        {
            using var left = Render("Left");
            using var right = Render("Right");
            var (lf, ll) = DarkColumns(left);
            var (rf, rl) = DarkColumns(right);

            Assert.That(rl - rf, Is.EqualTo(ll - lf));
        }
    }
}
