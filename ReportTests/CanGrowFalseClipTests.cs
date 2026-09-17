using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ReportTests
{
    [TestFixture]
    public class CanGrowFalseClipTests
    {
        private Uri _reportFolder;
        private Uri _outputFolder;

        [SetUp]
        public void Prepare()
        {
            _outputFolder = GeneralUtils.OutputTestsFolder();
            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        /// <summary>
        /// A textbox that may not grow may not draw outside its own rectangle.
        ///
        /// The PDF renderer places each wrapped line itself, one font size below the last,
        /// and drew every line it had been given no matter how tall the box was. A value
        /// too wide for its column therefore wrapped onto a second line that was painted
        /// below the box, on top of whatever was there - in a table, the next detail row,
        /// so one over-long value cost two rows instead of one. CanGrow=false says the box
        /// keeps its height, which means the overflow is clipped, not relocated.
        ///
        /// The fixture stacks two 0.153in boxes - the height of a fixed table row - the
        /// upper holding a value three lines wide at its column's width. Correct output is
        /// one line in each box.
        /// </summary>
        [Test]
        public async Task CanGrowFalse_TextboxDoesNotPaintBelowItsOwnBox()
        {
            IReadOnlyList<Letter> letters = await RenderFixture();

            // Every line of text the page actually drew, topmost first. The first two are
            // the stacked pair: the box that may not grow, and the box below it.
            var lines = LinesByBaseline(letters);

            var clipped = lines[0];
            var rowBelow = lines[1];

            Assert.Multiple(() =>
            {
                Assert.That(clipped.text, Does.StartWith("Our"),
                    "the value's first line still renders - clipping is not blanking");
                Assert.That(clipped.text, Does.Not.Contain("Everywhere"),
                    "the overflow line is clipped away, not painted below the box");

                Assert.That(rowBelow.text, Is.EqualTo("Piccolo"),
                    "the box below holds its own value and nothing else; the wrapped " +
                    "second line used to be drawn on top of it");

                // Said as geometry as well as as text: nothing from the top box is drawn
                // at or below the next box's own line.
                Assert.That(clipped.baseline - rowBelow.baseline, Is.GreaterThan(1.0),
                    "the two boxes' text sits on two distinct baselines");
            });

            // The whole page: exactly one line in each of the three no-grow boxes, plus the
            // three the grown box is entitled to. Any extra line is overflow somewhere.
            Assert.That(lines.Count, Is.EqualTo(6),
                "6 lines: 1 clipped + 1 row below + 3 grown + 1 short. Lines drawn were: "
                + string.Join(" | ", lines.Select(l => $"{l.baseline:F1}:{l.text}")));
        }

        /// <summary>
        /// The opposite direction: CanGrow=true still grows. The same value in the same
        /// 1in-wide, 0.153in-tall box wraps to three lines and all three are drawn, because
        /// a box that may grow has no height to overflow.
        ///
        /// This one is a guard, not a reproduction: it holds both with the clipping and
        /// without it, and its job is to fail if the clipping ever reaches a box that is
        /// allowed to grow.
        /// </summary>
        [Test]
        public async Task CanGrowTrue_TextboxStillDrawsEveryWrappedLine()
        {
            IReadOnlyList<Letter> letters = await RenderFixture();
            var lines = LinesByBaseline(letters);

            // The grown box is the three lines immediately above the short value at the
            // bottom of the page - anchored that way rather than by index, so that how many
            // lines the boxes above it drew cannot move the ones being asserted here.
            int shortLine = lines.FindIndex(l => l.text.Trim() == "Short");
            Assert.That(shortLine, Is.GreaterThanOrEqualTo(3), "the short value renders last");

            // Letters only: PdfPig reports the inter-word space as a glyph of its own and
            // what this test is about is which lines were drawn, not how they are spaced.
            string grown = new string(
                string.Concat(lines.Skip(shortLine - 3).Take(3).Select(l => l.text))
                .Where(char.IsLetter).ToArray());

            Assert.That(grown, Is.EqualTo("OurWheelsFollowUsEverywhere"),
                "every wrapped line of a CanGrow=true box is drawn. Lines drawn were: "
                + string.Join(" | ", lines.Select(l => $"{l.baseline:F1}:{l.text}")));
        }

        private async Task<IReadOnlyList<Letter>> RenderFixture()
        {
            var rdl = new Uri(_reportFolder, "CanGrowFalseClipTest.rdl");
            Report report = await RdlUtils.GetReport(rdl);
            report.Folder = _reportFolder.LocalPath;
            await report.RunGetData(null);

            string output = Path.Combine(_outputFolder.LocalPath, "CanGrowFalseClipTest.pdf");
            using (var sg = new OneFileStreamGen(output, true))
            {
                await report.RunRender(sg, OutputPresentationType.PDF);
            }

            using var pdf = PdfDocument.Open(output);
            return pdf.GetPages().Single().Letters.ToList();
        }

        /// <summary>
        /// Groups the page's glyphs into drawn lines by their baseline, topmost first.
        /// PDF y grows upward, so a larger baseline is higher on the page.
        /// </summary>
        private static List<(double baseline, string text)> LinesByBaseline(
            IReadOnlyList<Letter> letters)
        {
            return letters
                .GroupBy(l => Math.Round(l.StartBaseLine.Y, 1))
                .OrderByDescending(g => g.Key)
                .Select(g => (
                    baseline: g.Key,
                    text: string.Concat(g.OrderBy(l => l.StartBaseLine.X).Select(l => l.Value))))
                .ToList();
        }
    }
}
