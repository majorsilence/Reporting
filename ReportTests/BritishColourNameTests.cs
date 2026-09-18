using System.Drawing;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// Reports written in British English name colours in British English. The CLR knows only
    /// LightGray, and ColorTranslator.FromHtml does not object to LightGrey — it returns an
    /// unnamed, fully transparent colour, so the parser's catch never fires and the renderers
    /// paint the result black.
    /// <para>
    /// The symptom was a black band across a report where a light grey row was intended, with
    /// the row's own text lost inside it, and nothing logged to explain it. A single
    /// real-world report was found to say Grey 180 times.
    /// </para>
    /// </summary>
    [TestFixture]
    public class BritishColourNameTests
    {
        private static readonly Color Fallback = Color.Magenta;   // never a real answer here

        [Test]
        public void LightGrey_ResolvesTheSameAsLightGray ()
        {
            var grey = XmlUtil.ColorFromHtml ("LightGrey", Fallback);
            var gray = XmlUtil.ColorFromHtml ("LightGray", Fallback);

            Assert.That (grey.ToArgb (), Is.EqualTo (gray.ToArgb ()));
            Assert.That (grey.ToArgb (), Is.Not.EqualTo (Fallback.ToArgb ()), "should not have fallen back");
        }

        [TestCase ("Grey", "Gray")]
        [TestCase ("DarkGrey", "DarkGray")]
        [TestCase ("DimGrey", "DimGray")]
        [TestCase ("SlateGrey", "SlateGray")]
        [TestCase ("LightSlateGrey", "LightSlateGray")]
        [TestCase ("DarkSlateGrey", "DarkSlateGray")]
        [TestCase ("lightgrey", "lightgray")]
        public void EveryGreySpelling_ResolvesToItsGrayEquivalent (string grey, string gray)
        {
            Assert.That (XmlUtil.ColorFromHtml (grey, Fallback).ToArgb (),
                Is.EqualTo (XmlUtil.ColorFromHtml (gray, Fallback).ToArgb ()));
        }

        /// <summary>
        /// The thing that actually went wrong: an unresolved name is transparent, and painting
        /// transparent gives black. Nothing may come back that way.
        /// </summary>
        [Test]
        public void AGreyColour_IsNeverTransparent ()
        {
            var c = XmlUtil.ColorFromHtml ("LightGrey", Fallback);

            Assert.That (c.A, Is.EqualTo (255), "a transparent background is painted as black");
        }

        /// <summary>
        /// The repair must not swallow a name that is simply wrong: that still belongs on the
        /// caller's default, which is what the original catch was for.
        /// </summary>
        [Test]
        public void AnInventedColourName_StillFallsBack ()
        {
            Assert.That (XmlUtil.ColorFromHtml ("Grue", Fallback).ToArgb (),
                Is.EqualTo (Fallback.ToArgb ()));
        }

        /// <summary>
        /// A deliberately transparent hex colour has the same alpha-zero signature as a failed
        /// name, and must be taken at its word rather than replaced.
        /// </summary>
        [TestCase ("#00FFFFFF", 0, 255, 255, 255)]
        [TestCase ("#333333", 255, 51, 51, 51)]
        public void HexLiterals_AreTakenAtTheirWord (string hex, int a, int r, int g, int b)
        {
            var c = XmlUtil.ColorFromHtml (hex, Fallback);

            Assert.Multiple (() => {
                Assert.That (c.A, Is.EqualTo (a));
                Assert.That (c.R, Is.EqualTo (r));
                Assert.That (c.G, Is.EqualTo (g));
                Assert.That (c.B, Is.EqualTo (b));
            });
        }

        /// <summary>Named colours that were already right must be untouched.</summary>
        [TestCase ("SteelBlue")]
        [TestCase ("Silver")]
        [TestCase ("White")]
        [TestCase ("Transparent")]
        public void ColoursTheClrAlreadyKnows_AreUnchanged (string name)
        {
            var c = XmlUtil.ColorFromHtml (name, Fallback);

            Assert.That (c.ToArgb (), Is.EqualTo (Color.FromName (name).ToArgb ()));
        }
    }
}
