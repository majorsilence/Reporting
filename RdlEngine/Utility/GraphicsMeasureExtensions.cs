using System.Drawing;
using System.Runtime.CompilerServices;
using Draw2 = Majorsilence.Forms.Drawing;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Measures text with <c>Graphics.MeasureRunWidth</c> / <c>MeasureRunSpans</c>, which make the same
    /// <c>SKFont.MeasureText</c> calls <c>MeasureCharacterRanges</c> does, without a Region, StringFormat and layout
    /// list per range. The widths are the same floats. The height of one line is the font's line height, so it is
    /// found once per font and surface (through <c>MeasureCharacterRanges</c>, once) and kept.
    /// </summary>
    internal static class GraphicsMeasureExtensions
    {
        private sealed class LineHeights
        {
            private readonly System.Collections.Generic.Dictionary<Draw2.Font, float> _heights =
                new System.Collections.Generic.Dictionary<Draw2.Font, float>();

            internal float Get(Draw2.Graphics g, Draw2.Font font)
            {
                lock (this)
                {
                    if (!_heights.TryGetValue(font, out float h))
                        _heights[font] = h = ViaRegions(g, "Xg", font).Height;
                    return h;
                }
            }
        }

        private static readonly ConditionalWeakTable<Draw2.Graphics, LineHeights> Heights =
            new ConditionalWeakTable<Draw2.Graphics, LineHeights>();

        private static bool HasLineBreak(string s) => s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;

        /// <summary>The size of a run of text as one unwrapped line, as <c>MeasureCharacterRanges</c> reports it.</summary>
        internal static SizeF MeasureRun(this Draw2.Graphics g, string text, Draw2.Font font)
        {
            if (HasLineBreak(text))
                return ViaRegions(g, text, font);       // more than one line: its height is the layout's

            return new SizeF(g.MeasureRunWidth(text, font), Heights.GetValue(g, _ => new LineHeights()).Get(g, font));
        }

        /// <summary>Left and right edge of each range of a single line, in pixels.</summary>
        internal static (float Left, float Right)[] MeasureRunSpans(this Draw2.Graphics g, string text, Draw2.Font font, Draw2.CharacterRange[] ranges)
        {
            var result = new (float Left, float Right)[ranges.Length];
            if (HasLineBreak(text))
            {
                var format = new Draw2.StringFormat();
                try
                {
                    format.SetMeasurableCharacterRanges(ranges);
                    var regions = g.MeasureCharacterRanges(text, font, new RectangleF(0, 0, float.MaxValue, float.MaxValue), format);
                    for (int i = 0; i < regions.Length; i++)
                    {
                        RectangleF b = regions[i].GetBounds(g);
                        regions[i].Dispose();
                        result[i] = (b.Left, b.Right);
                    }
                }
                finally { format.Dispose(); }
                return result;
            }

            g.MeasureRunSpans(text, font, ranges, result);
            return result;
        }

        private static SizeF ViaRegions(Draw2.Graphics g, string text, Draw2.Font font)
        {
            var format = new Draw2.StringFormat();
            try
            {
                format.SetMeasurableCharacterRanges(new[] { new Draw2.CharacterRange(0, text.Length) });
                var regions = g.MeasureCharacterRanges(text, font, new RectangleF(0, 0, float.MaxValue, float.MaxValue), format);
                RectangleF b = regions[0].GetBounds(g);
                regions[0].Dispose();
                return new SizeF(b.Width, b.Height);
            }
            finally { format.Dispose(); }
        }
    }
}
