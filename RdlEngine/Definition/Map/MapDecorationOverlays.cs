using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A decoration drawn over the map item: a box in points of the item (placed by
    /// <see cref="MapDecorationLayout"/>), with the style's background and border, and content.
    /// The canvas is scaled to points before the content is drawn, so the content is drawn in
    /// points whatever the bitmap's resolution.
    /// </summary>
    internal abstract class MapDecorationOverlay : MapOverlay
    {
        internal SKRect Bounds;
        internal float ItemWidthPoints;
        internal float ItemHeightPoints;
        internal Color Background = Color.Empty;
        internal Color Border = Color.Empty;
        internal float BorderPoints;
        internal float Padding = 3;

        /// <summary>The size the decoration wants, in points, within the room given.</summary>
        internal abstract SKSize Measure (float maxWidth, float maxHeight);

        internal sealed override void Draw (SKCanvas canvas, MapFrame frame)
        {
            if (ItemWidthPoints <= 0 || ItemHeightPoints <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            canvas.Save ();
            canvas.Scale (frame.Width / ItemWidthPoints, frame.Height / ItemHeightPoints);
            if (!Background.IsEmpty && Background.A > 0) {
                using var fill = new SKPaint { Color = MapScene.ToSk (Background), IsStroke = false };
                canvas.DrawRect (Bounds, fill);
            }
            if (!Border.IsEmpty && Border.A > 0 && BorderPoints > 0) {
                using var stroke = new SKPaint { Color = MapScene.ToSk (Border), IsStroke = true, StrokeWidth = BorderPoints, IsAntialias = true };
                var inset = BorderPoints / 2;
                canvas.DrawRect (SKRect.Create (Bounds.Left + inset, Bounds.Top + inset, Bounds.Width - BorderPoints, Bounds.Height - BorderPoints), stroke);
            }
            var content = SKRect.Create (Bounds.Left + Padding, Bounds.Top + Padding,
                Math.Max (0, Bounds.Width - 2 * Padding), Math.Max (0, Bounds.Height - 2 * Padding));
            canvas.Save ();
            canvas.ClipRect (Bounds);
            DrawContent (canvas, frame, content);
            canvas.Restore ();
            canvas.Restore ();
        }

        /// <summary>Draws inside <paramref name="content"/>, in points of the item.</summary>
        protected abstract void DrawContent (SKCanvas canvas, MapFrame frame, SKRect content);
    }

    /// <summary>A font and colour for a decoration's text, sizes in points.</summary>
    internal sealed class MapTextStyle
    {
        internal string Family = "Arial";
        internal float Size = 8;
        internal bool Bold;
        internal bool Italic;
        internal Color Color = Color.Black;

        private static readonly ConcurrentDictionary<(string, bool, bool), SKTypeface> Typefaces = new ConcurrentDictionary<(string, bool, bool), SKTypeface> ();

        internal SKFont Font ()
        {
            var typeface = Typefaces.GetOrAdd ((Family, Bold, Italic), key =>
                SKTypeface.FromFamilyName (key.Item1,
                    key.Item2 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                    key.Item3 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright) ?? SKTypeface.Default);
            return new SKFont (typeface, Size) { Subpixel = true };
        }

        internal float Width (string text)
        {
            if (string.IsNullOrEmpty (text))
                return 0;
            using var font = Font ();
            return font.MeasureText (text);
        }

        internal float LineHeight
        {
            get {
                using var font = Font ();
                return font.Spacing;
            }
        }

        /// <summary>Draws one line with its top at <paramref name="top"/>.</summary>
        internal void Draw (SKCanvas canvas, string text, float x, float top, SKTextAlign align = SKTextAlign.Left)
        {
            if (string.IsNullOrEmpty (text))
                return;
            using var font = Font ();
            using var paint = new SKPaint { Color = MapScene.ToSk (Color), IsAntialias = true };
            canvas.DrawText (text, x, top - font.Metrics.Ascent, align, font, paint);
        }
    }

    /// <summary>
    /// The mark a legend entry shows for its bucket. A rule chooses its own with
    /// <see cref="MapAppearanceRule.LegendSwatch"/>: a colour rule a filled box (<see cref="MapColorSwatch"/>);
    /// a size rule would return a marker of the bucket's size, a marker rule the bucket's marker shape.
    /// </summary>
    internal abstract class MapLegendSwatch
    {
        /// <summary>The swatch's size in points next to text of <paramref name="textPoints"/>.</summary>
        internal abstract SKSize Size (float textPoints);

        /// <summary>Draws the swatch in <paramref name="box"/> (points).</summary>
        internal abstract void Draw (SKCanvas canvas, SKRect box);
    }

    /// <summary>A box filled with the bucket's colour, outlined in grey.</summary>
    internal sealed class MapColorSwatch : MapLegendSwatch
    {
        internal readonly Color Color;

        internal MapColorSwatch (Color color)
        {
            Color = color;
        }

        internal override SKSize Size (float textPoints) => new SKSize (textPoints * 1.8f, textPoints * 1.1f);

        internal override void Draw (SKCanvas canvas, SKRect box)
        {
            if (!Color.IsEmpty && Color.A > 0) {
                using var fill = new SKPaint { Color = MapScene.ToSk (Color), IsStroke = false };
                canvas.DrawRect (box, fill);
            }
            using var stroke = new SKPaint { Color = SKColors.DimGray, IsStroke = true, StrokeWidth = 0.5f, IsAntialias = true };
            canvas.DrawRect (box, stroke);
        }
    }

    internal sealed class MapLegendEntry
    {
        internal MapLegendSwatch Swatch;
        internal string Text;
        internal SKRect At;     // relative to the top left of the entries, set by Measure
    }

    /// <summary>A legend: an optional title, then its entries in columns or rows (see <see cref="MapLegendDefn"/>).</summary>
    internal sealed class MapLegendOverlay : MapDecorationOverlay
    {
        private const float SwatchGap = 4;
        private const float EntryGap = 2;
        private const float ColumnGap = 10;

        internal string Name;
        internal string Title;
        internal MapTextStyle TitleStyle = new MapTextStyle { Bold = true, Size = 9 };
        internal MapTextStyle TextStyle = new MapTextStyle ();
        internal bool TitleSeparator;
        internal Color SeparatorColor = Color.Gray;
        internal bool Rows;
        internal readonly List<MapLegendEntry> Entries = new List<MapLegendEntry> ();

        private float TitleHeight => string.IsNullOrEmpty (Title) ? 0 : TitleStyle.LineHeight + (TitleSeparator ? 3 : 0) + EntryGap;

        internal override SKSize Measure (float maxWidth, float maxHeight)
        {
            var roomW = Math.Max (1, maxWidth - 2 * Padding);
            var roomH = Math.Max (1, maxHeight - 2 * Padding - TitleHeight);
            var line = TextStyle.LineHeight;
            float x = 0, y = 0, extentW = 0, extentH = 0, band = 0;
            var inBand = 0;
            foreach (var e in Entries) {
                var swatch = e.Swatch.Size (TextStyle.Size);
                var w = swatch.Width + SwatchGap + TextStyle.Width (e.Text);
                var h = Math.Max (line, swatch.Height);
                if (Rows) {
                    if (inBand > 0 && x + w > roomW) {
                        x = 0;
                        y += band + EntryGap;
                        band = 0;
                        inBand = 0;
                    }
                    e.At = SKRect.Create (x, y, w, h);
                    x += w + ColumnGap;
                    band = Math.Max (band, h);
                } else {
                    if (inBand > 0 && y + h > roomH) {
                        y = 0;
                        x += band + ColumnGap;
                        band = 0;
                        inBand = 0;
                    }
                    e.At = SKRect.Create (x, y, w, h);
                    y += h + EntryGap;
                    band = Math.Max (band, w);
                }
                inBand++;
                extentW = Math.Max (extentW, e.At.Right);
                extentH = Math.Max (extentH, e.At.Bottom);
            }
            var titleW = TitleStyle.Width (Title);
            return new SKSize (Math.Max (extentW, titleW) + 2 * Padding, extentH + TitleHeight + 2 * Padding);
        }

        protected override void DrawContent (SKCanvas canvas, MapFrame frame, SKRect content)
        {
            var top = content.Top;
            if (!string.IsNullOrEmpty (Title)) {
                TitleStyle.Draw (canvas, Title, content.MidX, top, SKTextAlign.Center);
                top += TitleStyle.LineHeight;
                if (TitleSeparator) {
                    using var rule = new SKPaint { Color = MapScene.ToSk (SeparatorColor), IsStroke = true, StrokeWidth = 0.75f, IsAntialias = true };
                    canvas.DrawLine (content.Left, top + 1.5f, content.Right, top + 1.5f, rule);
                    top += 3;
                }
                top += EntryGap;
            }
            var line = TextStyle.LineHeight;
            foreach (var e in Entries) {
                var at = e.At;
                at.Offset (content.Left, top);
                var size = e.Swatch.Size (TextStyle.Size);
                e.Swatch.Draw (canvas, SKRect.Create (at.Left, at.MidY - size.Height / 2, size.Width, size.Height));
                TextStyle.Draw (canvas, e.Text, at.Left + size.Width + SwatchGap, at.MidY - line / 2);
            }
        }
    }

    /// <summary>A map title: lines of text centred in its box.</summary>
    internal sealed class MapTitleOverlay : MapDecorationOverlay
    {
        internal string Text;
        internal MapTextStyle TextStyle = new MapTextStyle { Size = 12, Bold = true };

        private string[] Lines => (Text ?? string.Empty).Replace ("\r\n", "\n").Split ('\n');

        internal override SKSize Measure (float maxWidth, float maxHeight)
        {
            float width = 0;
            foreach (var line in Lines)
                width = Math.Max (width, TextStyle.Width (line));
            return new SKSize (width + 2 * Padding, Lines.Length * TextStyle.LineHeight + 2 * Padding);
        }

        protected override void DrawContent (SKCanvas canvas, MapFrame frame, SKRect content)
        {
            var lines = Lines;
            var height = lines.Length * TextStyle.LineHeight;
            var top = content.MidY - height / 2;
            foreach (var line in lines) {
                TextStyle.Draw (canvas, line, content.MidX, top, SKTextAlign.Center);
                top += TextStyle.LineHeight;
            }
        }
    }

    /// <summary>
    /// The colour scale: equal cells of the bucket colours, tick marks at the boundaries between
    /// ranges with their values (or a label under each category), and a title above.
    /// </summary>
    internal sealed class MapColorScaleOverlay : MapDecorationOverlay
    {
        private const float BarHeight = 8;
        private const float PreferredWidth = 160;

        internal string Title;
        internal MapTextStyle TitleStyle = new MapTextStyle { Bold = true };
        internal MapTextStyle LabelStyle = new MapTextStyle { Size = 7 };
        internal float TickLength = 2.25f;
        internal Color BarBorder = Color.Black;
        internal bool Above = true;
        internal bool Below = true;
        internal bool Categories;
        internal readonly List<Color> Colors = new List<Color> ();

        /// <summary>A label per boundary (ranges: one more than the colours) or per category; null where none is drawn.</summary>
        internal readonly List<string> Labels = new List<string> ();

        private float TitleHeight => string.IsNullOrEmpty (Title) ? 0 : TitleStyle.LineHeight + 1;
        private bool LabelsAbove => Above && (!Below || Labels.Count > 1);  // alternate needs two labels to alternate

        internal override SKSize Measure (float maxWidth, float maxHeight)
        {
            float widest = 0;
            foreach (var label in Labels)
                widest = Math.Max (widest, LabelStyle.Width (label));
            var want = Math.Max (PreferredWidth, Math.Max (TitleStyle.Width (Title), widest * Labels.Count * (Above && Below ? 0.6f : 1.1f)));
            var width = Math.Min (maxWidth, want + 2 * Padding);
            var labelRow = TickLength + LabelStyle.LineHeight;
            var height = TitleHeight + (LabelsAbove ? labelRow : 0) + BarHeight + (Below ? labelRow : 0) + 2 * Padding;
            return new SKSize (width, height);
        }

        protected override void DrawContent (SKCanvas canvas, MapFrame frame, SKRect content)
        {
            if (Colors.Count == 0)
                return;
            var top = content.Top;
            if (!string.IsNullOrEmpty (Title)) {
                TitleStyle.Draw (canvas, Title, content.MidX, top, SKTextAlign.Center);
                top += TitleHeight;
            }
            var labelRow = TickLength + LabelStyle.LineHeight;
            var above = LabelsAbove;
            var barTop = top + (above ? labelRow : 0);

            // Room at the ends for half a label, so the end labels are not cut off.
            var endRoom = Math.Min (content.Width / 4, Math.Max (LabelStyle.Width (Labels.Count > 0 ? Labels[0] : null),
                LabelStyle.Width (Labels.Count > 0 ? Labels[Labels.Count - 1] : null)) / 2);
            var bar = SKRect.Create (content.Left + (Categories ? 0 : endRoom), barTop, content.Width - (Categories ? 0 : 2 * endRoom), BarHeight);
            var cell = bar.Width / Colors.Count;
            for (var i = 0; i < Colors.Count; i++) {
                if (Colors[i].IsEmpty || Colors[i].A == 0)
                    continue;
                using var fill = new SKPaint { Color = MapScene.ToSk (Colors[i]), IsStroke = false };
                canvas.DrawRect (SKRect.Create (bar.Left + i * cell, bar.Top, cell, bar.Height), fill);
            }
            using var stroke = new SKPaint { Color = MapScene.ToSk (BarBorder), IsStroke = true, StrokeWidth = 0.5f, IsAntialias = true };
            canvas.DrawRect (bar, stroke);

            float lastAbove = float.MinValue, lastBelow = float.MinValue;
            var labelNumber = 0;
            for (var i = 0; i < Labels.Count; i++) {
                var x = Categories ? bar.Left + (i + 0.5f) * cell : bar.Left + i * cell;
                if (!Categories)
                    canvas.DrawLine (x, bar.Bottom, x, bar.Bottom + TickLength, stroke);
                var label = Labels[i];
                if (string.IsNullOrEmpty (label))
                    continue;
                var onTop = above && (!Below || labelNumber % 2 == 1);
                labelNumber++;
                var half = LabelStyle.Width (label) / 2;
                ref var last = ref onTop ? ref lastAbove : ref lastBelow;
                if (x - half < last + 2)
                    continue;   // would overlap the label before it
                last = x + half;
                if (onTop) {
                    if (!Categories)
                        canvas.DrawLine (x, bar.Top, x, bar.Top - TickLength, stroke);
                    LabelStyle.Draw (canvas, label, x, bar.Top - TickLength - LabelStyle.LineHeight, SKTextAlign.Center);
                } else {
                    LabelStyle.Draw (canvas, label, x, bar.Bottom + TickLength, SKTextAlign.Center);
                }
            }
        }
    }

    /// <summary>
    /// The distance scale: a bar of a round number of kilometres and one of miles, measured at the
    /// centre of the map, worked out when drawn (it depends on how the shapes are fitted).
    /// </summary>
    internal sealed class MapDistanceScaleOverlay : MapDecorationOverlay
    {
        private const float BarHeight = 4;
        private const float PreferredWidth = 120;

        internal bool Mercator;
        internal MapTextStyle TextStyle = new MapTextStyle { Size = 7 };
        internal Color ScaleColor = Color.White;
        internal Color ScaleBorder = Color.DarkGray;

        /// <summary>The bars of the last draw, in points: for tests.</summary>
        internal MapDistanceBar Metric, Imperial;

        internal override SKSize Measure (float maxWidth, float maxHeight)
            => new SKSize (Math.Min (maxWidth, PreferredWidth), 2 * TextStyle.LineHeight + 2 * BarHeight + 2 * Padding);

        protected override void DrawContent (SKCanvas canvas, MapFrame frame, SKRect content)
        {
            if (!frame.HasData || frame.Width <= 0)
                return;
            // Projected x is degrees of longitude in both projections; the frame scales it to pixels.
            var pointsPerDegree = frame.Scale * ItemWidthPoints / frame.Width;
            var latitude = MapDistance.Latitude (frame.CenterY, Mercator);
            var maxBar = content.Width - 1;
            Metric = MapDistance.Bar (MapDistance.KilometresPerDegree (latitude) * 1000, pointsPerDegree, maxBar, true);
            Imperial = MapDistance.Bar (MapDistance.KilometresPerDegree (latitude) * 1000, pointsPerDegree, maxBar, false);
            if (Metric.Length <= 0 || Imperial.Length <= 0)
                return;

            var line = TextStyle.LineHeight;
            var barTop = content.Top + line;
            using var fill = new SKPaint { Color = MapScene.ToSk (ScaleColor), IsStroke = false };
            using var stroke = new SKPaint { Color = MapScene.ToSk (ScaleBorder), IsStroke = true, StrokeWidth = 0.75f, IsAntialias = true };
            var metric = SKRect.Create (content.Left, barTop, Metric.Length, BarHeight);
            var imperial = SKRect.Create (content.Left, barTop + BarHeight, Imperial.Length, BarHeight);
            canvas.DrawRect (metric, fill);
            canvas.DrawRect (imperial, fill);
            canvas.DrawRect (metric, stroke);
            canvas.DrawRect (imperial, stroke);

            TextStyle.Draw (canvas, Metric.Label, Math.Max (content.Left + TextStyle.Width (Metric.Label), metric.Right), content.Top, SKTextAlign.Right);
            TextStyle.Draw (canvas, Imperial.Label, Math.Max (content.Left + TextStyle.Width (Imperial.Label), imperial.Right), imperial.Bottom, SKTextAlign.Right);
        }
    }

    /// <summary>One bar of a distance scale: a round distance, its label, and its length in points.</summary>
    internal readonly struct MapDistanceBar
    {
        internal readonly double Distance;
        internal readonly string Unit;
        internal readonly float Length;

        internal MapDistanceBar (double distance, string unit, float length)
        {
            Distance = distance;
            Unit = unit;
            Length = length;
        }

        internal string Label => Distance.ToString ("#,##0.###", CultureInfo.InvariantCulture) + " " + Unit;
    }

    /// <summary>The distance scale's arithmetic, on a sphere of the Earth's mean radius.</summary>
    internal static class MapDistance
    {
        internal const double EarthRadiusKm = 6371.0088;
        internal const double MetresPerMile = 1609.344;
        internal const double MetresPerFoot = 0.3048;

        /// <summary>The length of one degree of longitude at the latitude, in kilometres.</summary>
        internal static double KilometresPerDegree (double latitude)
            => Math.Max (0, EarthRadiusKm * Math.PI / 180 * Math.Cos (latitude * Math.PI / 180));

        /// <summary>The latitude of a projected y: itself for Equirectangular, the inverse of the viewport's Mercator.</summary>
        internal static double Latitude (double projectedY, bool mercator)
            => mercator ? (2 * Math.Atan (Math.Exp (projectedY * Math.PI / 180)) - Math.PI / 2) * 180 / Math.PI : projectedY;

        /// <summary>The largest 1, 2 or 5 times a power of ten that is not more than <paramref name="max"/>; 0 for none.</summary>
        internal static double Nice (double max)
        {
            if (!(max > 0) || double.IsInfinity (max))
                return 0;
            var power = Math.Pow (10, Math.Floor (Math.Log10 (max)));
            foreach (var m in new[] { 5.0, 2.0, 1.0 }) {
                if (m * power <= max * (1 + 1e-9))
                    return m * power;
            }
            return power;
        }

        /// <summary>
        /// A bar of a round distance no longer than <paramref name="maxPoints"/>: kilometres (metres
        /// under one), or miles (feet under one).
        /// </summary>
        internal static MapDistanceBar Bar (double metresPerDegree, double pointsPerDegree, double maxPoints, bool metric)
        {
            if (!(metresPerDegree > 0) || !(pointsPerDegree > 0) || !(maxPoints > 0))
                return default;
            var metresPerPoint = metresPerDegree / pointsPerDegree;
            var maxMetres = maxPoints * metresPerPoint;
            var (big, bigName, small, smallName) = metric ? (1000.0, "km", 1.0, "m") : (MetresPerMile, "mi", MetresPerFoot, "ft");
            var unit = maxMetres >= big ? big : small;
            var distance = Nice (maxMetres / unit);
            return new MapDistanceBar (distance, unit == big ? bigName : smallName, (float)(distance * unit / metresPerPoint));
        }
    }
}
