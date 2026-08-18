using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class MajorsilenceDarawingColorTranslatorTests
    {
        private static readonly (string ColorString, System.Drawing.Color ExpectedColor)[] TestColors =
        {
            ("Bisque", System.Drawing.Color.FromArgb(255, 228, 196)),
            ("Red", System.Drawing.Color.FromArgb(255, 0, 0)),
            ("#F00", System.Drawing.Color.FromArgb(255, 0, 0)),
            ("#FF0000", System.Drawing.Color.FromArgb(255, 0, 0)),
            ("#80FF0000", System.Drawing.Color.FromArgb(128, 255, 0)),
            ("#FF5733", System.Drawing.Color.FromArgb(255, 87, 51)),
            ("#80FF5733", System.Drawing.Color.FromArgb(128, 255, 87, 51)),
            ("#000000", System.Drawing.Color.FromArgb(0, 0, 0)),
            ("#FFFFFFFF", System.Drawing.Color.FromArgb(255, 255, 255)),
            ("#123456", System.Drawing.Color.FromArgb(18, 52, 86)),
            ("#7F123456", System.Drawing.Color.FromArgb(127, 18, 52))
        };
        
        [Test, TestCaseSource(nameof(TestColors))]
        public void FromHtml_ValidHexWithoutHash_ReturnsCorrectColor((string ColorString, System.Drawing.Color ExpectedColor) testCase)
        {
            var color = Majorsilence.Forms.Drawing.ColorTranslator.FromHtml(testCase.ColorString);
            Assert.That(color.R, Is.EqualTo(testCase.ExpectedColor.R));
            Assert.That(color.G, Is.EqualTo(testCase.ExpectedColor.G));
            Assert.That(color.B, Is.EqualTo(testCase.ExpectedColor.B));
        }
    }
}