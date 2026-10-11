using System.Drawing;
using Draw2 = Majorsilence.Forms.Drawing;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// <c>Graphics.MeasureString</c> in Majorsilence.Forms lays the text out through
    /// <c>TextMeasurer.CreateTextBlock</c>, which keeps its results in one static, plain
    /// <c>Dictionary</c> that every Graphics shares. That is fine on a UI thread but corrupts the
    /// dictionary (InvalidOperationException, or a lost entry) when reports are rendered on several
    /// threads at once, which a web server does. Until the cache itself is thread-safe, measuring
    /// goes through one lock. Only the MeasureString overloads reach that cache; MeasureCharacterRanges
    /// does not, so it needs no lock.
    /// </summary>
    internal static class GraphicsMeasureExtensions
    {
        private static readonly object Sync = new object();

        internal static SizeF MeasureStringLocked(this Draw2.Graphics g, string text, Draw2.Font font)
        {
            lock (Sync) return g.MeasureString(text, font);
        }

        internal static SizeF MeasureStringLocked(this Draw2.Graphics g, string text, Draw2.Font font, int width, Draw2.StringFormat format)
        {
            lock (Sync) return g.MeasureString(text, font, width, format);
        }

        internal static SizeF MeasureStringLocked(this Draw2.Graphics g, string text, Draw2.Font font, SizeF layoutArea, Draw2.StringFormat format)
        {
            lock (Sync) return g.MeasureString(text, font, layoutArea, format);
        }
    }
}
