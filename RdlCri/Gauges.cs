using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using SkiaSharp;
using Draw2 = Majorsilence.Forms.Drawing;

namespace Majorsilence.Reporting.Cri
{
    /// <summary>
    /// Draws an RDL gauge as a bitmap: the shared properties and parsing for the radial and linear
    /// gauges. The report supplies each value as a custom property, so any of them can be an
    /// expression.
    /// </summary>
    /// <remarks>
    /// Properties (all text, so they can be expressions): Value, Min, Max, Interval (distance between
    /// labelled ticks), Ranges (from:to:color pairs joined by ';'), PointerColor, FaceColor, and for the
    /// radial gauge StartAngle and SweepAngle (degrees clockwise from the bottom), for the linear gauge
    /// Orientation (Horizontal or Vertical).
    /// </remarks>
    public abstract class GaugeBase : ICustomReportItem
    {
        protected double Value = 50;
        protected double Min;
        protected double Max = 100;
        protected double Interval;
        protected List<(double From, double To, SKColor Color)> Ranges = new List<(double, double, SKColor)>();
        protected SKColor PointerColor = new SKColor(0xC0, 0x39, 0x2B);
        protected SKColor FaceColor = new SKColor(0xF4, 0xF4, 0xF4);
        protected SKColor InkColor = new SKColor(0x33, 0x33, 0x33);
        protected IDictionary<string, object> Properties = new Dictionary<string, object>();

        protected abstract float OptimalWidthMm { get; }
        protected abstract float OptimalHeightMm { get; }
        protected abstract string TypeName { get; }
        protected abstract void Draw(SKCanvas canvas, int width, int height);

        #region ICustomReportItem

        bool ICustomReportItem.IsDataRegion()
        {
            return false;
        }

        void ICustomReportItem.DrawImage(ref Draw2.Bitmap bm)
        {
            Render(ref bm);
        }

        void ICustomReportItem.DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            Value = 62; Min = 0; Max = 100; Interval = 0;
            Ranges = new List<(double, double, SKColor)> {
                (0, 60, new SKColor(0x2E, 0xA0, 0x43)), (60, 85, new SKColor(0xF2, 0xB1, 0x34)), (85, 100, new SKColor(0xC0, 0x39, 0x2B)),
            };
            Render(ref bm);
        }

        void ICustomReportItem.SetProperties(IDictionary<string, object> props)
        {
            Properties = props ?? new Dictionary<string, object>();
            Value = Number("Value", 50);
            Min = Number("Min", 0);
            Max = Number("Max", 100);
            if (Max <= Min)
                Max = Min + 1;
            Interval = Number("Interval", 0);
            PointerColor = Colour("PointerColor", PointerColor);
            FaceColor = Colour("FaceColor", FaceColor);
            Ranges = ParseRanges(Text("Ranges"));
            ReadSpecific();
        }

        protected virtual void ReadSpecific()
        {
        }

        object ICustomReportItem.GetPropertiesInstance(XmlNode node)
        {
            return null;    // no designer property grid; the properties are edited as custom properties
        }

        void ICustomReportItem.SetPropertiesInstance(XmlNode node, object inst)
        {
        }

        string ICustomReportItem.GetCustomReportItemXml()
        {
            return "<CustomReportItem><Type>{0}</Type>" +
                   string.Format(CultureInfo.InvariantCulture, "<Height>{0}mm</Height><Width>{1}mm</Width>", OptimalHeightMm, OptimalWidthMm) +
                   "<CustomProperties>" +
                   "<CustomProperty><Name>Value</Name><Value>=50</Value></CustomProperty>" +
                   "<CustomProperty><Name>Min</Name><Value>0</Value></CustomProperty>" +
                   "<CustomProperty><Name>Max</Name><Value>100</Value></CustomProperty>" +
                   "</CustomProperties></CustomReportItem>";
        }

        void IDisposable.Dispose()
        {
        }

        #endregion

        #region Drawing

        private void Render(ref Draw2.Bitmap bm)
        {
            int width = Math.Max(1, bm.Width);
            int height = Math.Max(1, bm.Height);
            var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                Draw(canvas, width, height);
            }
            // The conversion is declared on Image; what it hands back is a Bitmap.
            bm = (Draw2.Bitmap)(Draw2.Image)bitmap;
        }

        /// <summary>How far along the scale a value is, 0 at Min and 1 at Max, clamped.</summary>
        protected double Fraction(double value)
        {
            return Math.Max(0, Math.Min(1, (value - Min) / (Max - Min)));
        }

        /// <summary>
        /// The values to put a tick and label at, from Min to Max. With no Interval set the step is chosen
        /// so the labels fit along <paramref name="length"/> pixels without running into each other.
        /// </summary>
        protected IEnumerable<double> TickValues(float length, float labelSize)
        {
            double range = Max - Min;
            double step = Interval > 0 ? Interval : NiceStep(range);
            if (Interval <= 0)
            {
                int chars = Math.Max(Format(Min).Length, Format(Max).Length);
                double need = labelSize * 0.62 * chars + 6;     // pixels a label occupies along the scale
                while (range / step * need > length && step < range)
                    step = NextNice(step);
            }
            if (step <= 0)
                yield break;
            // count rather than add, so rounding error cannot drop the last tick
            int count = (int)Math.Round(range / step);
            for (int i = 0; i <= count && i <= 200; i++)
                yield return Min + i * step;
        }

        private static double NiceStep(double range)
        {
            // about 5 to 10 intervals, on a 1, 2 or 5 step
            double rough = range / 8.0;
            double power = Math.Pow(10, Math.Floor(Math.Log10(rough)));
            double fraction = rough / power;
            double nice = fraction < 1.5 ? 1 : fraction < 3.5 ? 2 : fraction < 7.5 ? 5 : 10;
            return nice * power;
        }

        /// <summary>The next step up the 1, 2, 5, 10 sequence.</summary>
        private static double NextNice(double step)
        {
            double power = Math.Pow(10, Math.Floor(Math.Log10(step)));
            double fraction = step / power;
            double next = fraction < 1.5 ? 2 : fraction < 3.5 ? 5 : 10;
            return next * power;
        }

        protected static string Format(double value)
        {
            return value.ToString("G5", CultureInfo.InvariantCulture);
        }

        protected SKPaint Stroke(SKColor color, float width, bool round = false)
        {
            return new SKPaint { Color = color, IsStroke = true, StrokeWidth = width, IsAntialias = true, StrokeCap = round ? SKStrokeCap.Round : SKStrokeCap.Butt };
        }

        protected SKPaint Fill(SKColor color)
        {
            return new SKPaint { Color = color, IsStroke = false, IsAntialias = true };
        }

        protected void Label(SKCanvas canvas, string text, float x, float y, float size, SKColor color)
        {
            using var font = new SKFont { Size = size };
            using var paint = Fill(color);
            canvas.DrawText(text, x, y + size * 0.35f, SKTextAlign.Center, font, paint);
        }

        #endregion

        #region Property parsing

        protected string Text(string name)
        {
            return Properties.TryGetValue(name, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : null;
        }

        protected double Number(string name, double fallback)
        {
            if (!Properties.TryGetValue(name, out var v) || v == null)
                return fallback;
            try
            {
                var d = Convert.ToDouble(v, CultureInfo.InvariantCulture);
                return double.IsNaN(d) || double.IsInfinity(d) ? fallback : d;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                return fallback;
            }
        }

        protected SKColor Colour(string name, SKColor fallback)
        {
            return ParseColour(Text(name), fallback);
        }

        /// <summary>A colour name (Green) or #RRGGBB / #AARRGGBB; anything else keeps the fallback.</summary>
        protected static SKColor ParseColour(string text, SKColor fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            text = text.Trim();
            if (SKColor.TryParse(text, out var parsed))
                return parsed;
            switch (text.ToLowerInvariant())
            {
                case "red": return SKColors.Red;
                case "green": return new SKColor(0x2E, 0xA0, 0x43);
                case "yellow": return new SKColor(0xF2, 0xB1, 0x34);
                case "orange": return SKColors.Orange;
                case "blue": return SKColors.RoyalBlue;
                case "black": return SKColors.Black;
                case "white": return SKColors.White;
                case "gray": case "grey": return SKColors.Gray;
                case "lightgray": case "lightgrey": return SKColors.LightGray;
                case "darkred": return SKColors.DarkRed;
                case "darkgreen": return SKColors.DarkGreen;
                case "darkblue": return SKColors.DarkBlue;
                case "silver": return SKColors.Silver;
                default: return fallback;
            }
        }

        /// <summary>"0:60:Green;60:85:Yellow" into ranges; malformed pieces are skipped.</summary>
        protected static List<(double From, double To, SKColor Color)> ParseRanges(string text)
        {
            var ranges = new List<(double, double, SKColor)>();
            if (string.IsNullOrWhiteSpace(text))
                return ranges;
            foreach (var piece in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = piece.Split(':');
                if (parts.Length < 3)
                    continue;
                if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var from) ||
                    !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var to))
                    continue;
                ranges.Add((Math.Min(from, to), Math.Max(from, to), ParseColour(parts[2], SKColors.Gray)));
            }
            return ranges;
        }

        #endregion
    }

    /// <summary>A dial with a coloured scale, tick marks and labels, and a needle.</summary>
    public class GaugeRadial : GaugeBase
    {
        private double _startAngle = 25;    // degrees clockwise from the bottom, as Report Builder measures
        private double _sweepAngle = 310;

        protected override float OptimalWidthMm => 50f;
        protected override float OptimalHeightMm => 50f;
        protected override string TypeName => "Gauge Radial";

        protected override void ReadSpecific()
        {
            _startAngle = Number("StartAngle", 25);
            _sweepAngle = Number("SweepAngle", 310);
            if (_sweepAngle <= 0 || _sweepAngle > 360)
                _sweepAngle = 310;
        }

        // 0 degrees is the bottom and angles run clockwise; Skia measures from 3 o'clock, also clockwise.
        private static float ToSkia(double gaugeAngle)
        {
            return (float)(90 + gaugeAngle);
        }

        private static SKPoint OnCircle(SKPoint center, float radius, double gaugeAngle)
        {
            double radians = ToSkia(gaugeAngle) * Math.PI / 180.0;
            return new SKPoint(center.X + radius * (float)Math.Cos(radians), center.Y + radius * (float)Math.Sin(radians));
        }

        protected override void Draw(SKCanvas canvas, int width, int height)
        {
            float margin = 2;
            float radius = Math.Min(width, height) / 2f - margin;
            if (radius < 8)
                return;
            var center = new SKPoint(width / 2f, height / 2f);

            // face and rim
            using (var face = Fill(FaceColor))
                canvas.DrawCircle(center, radius, face);
            using (var rim = Stroke(InkColor, Math.Max(1.5f, radius * 0.04f)))
                canvas.DrawCircle(center, radius - rim.StrokeWidth / 2, rim);

            float scaleRadius = radius * 0.78f;
            float band = Math.Max(3f, radius * 0.09f);
            var arc = new SKRect(center.X - scaleRadius, center.Y - scaleRadius, center.X + scaleRadius, center.Y + scaleRadius);

            // the scale track, then the coloured ranges over it
            using (var track = Stroke(new SKColor(0xD0, 0xD0, 0xD0), band))
                canvas.DrawArc(arc, ToSkia(_startAngle), (float)_sweepAngle, false, track);
            foreach (var (from, to, color) in Ranges)
            {
                double a = _startAngle + _sweepAngle * Fraction(from);
                double b = _startAngle + _sweepAngle * Fraction(to);
                if (b <= a)
                    continue;
                using var paint = Stroke(color, band);
                canvas.DrawArc(arc, ToSkia(a), (float)(b - a), false, paint);
            }

            // ticks and labels inside the scale
            float labelSize = Math.Max(7f, Math.Min(15f, radius * 0.13f));
            using (var tick = Stroke(InkColor, Math.Max(1f, radius * 0.02f)))
            {
                // the labels sit on an arc of about 60% of the scale radius
                float arcLength = (float)(_sweepAngle * Math.PI / 180.0) * scaleRadius * 0.6f;
                foreach (var value in TickValues(arcLength, labelSize))
                {
                    double angle = _startAngle + _sweepAngle * Fraction(value);
                    var outer = OnCircle(center, scaleRadius - band * 0.7f, angle);
                    var inner = OnCircle(center, scaleRadius - band * 0.7f - radius * 0.09f, angle);
                    canvas.DrawLine(outer, inner, tick);
                    var at = OnCircle(center, scaleRadius - band * 0.7f - radius * 0.09f - labelSize * 0.9f, angle);
                    Label(canvas, Format(value), at.X, at.Y, labelSize, InkColor);
                }
            }

            // needle and cap
            double needleAngle = _startAngle + _sweepAngle * Fraction(Value);
            var tip = OnCircle(center, scaleRadius * 0.92f, needleAngle);
            using (var needle = Stroke(PointerColor, Math.Max(2f, radius * 0.045f), round: true))
                canvas.DrawLine(center, tip, needle);
            using (var cap = Fill(PointerColor))
                canvas.DrawCircle(center, Math.Max(3f, radius * 0.07f), cap);
            using (var hub = Fill(FaceColor))
                canvas.DrawCircle(center, Math.Max(1.2f, radius * 0.025f), hub);
        }
    }

    /// <summary>A horizontal or vertical scale with coloured ranges, tick marks and labels, and a bar showing the value.</summary>
    public class GaugeLinear : GaugeBase
    {
        private bool _vertical;

        protected override float OptimalWidthMm => 60f;
        protected override float OptimalHeightMm => 22f;
        protected override string TypeName => "Gauge Linear";

        protected override void ReadSpecific()
        {
            _vertical = string.Equals(Text("Orientation"), "Vertical", StringComparison.OrdinalIgnoreCase);
        }

        protected override void Draw(SKCanvas canvas, int width, int height)
        {
            float margin = 3;
            float labelSize = Math.Max(7f, Math.Min(13f, Math.Min(width, height) * 0.16f));
            // L is the length of the scale; thickness is the track. P(t, offset) maps a position along the
            // scale (0 at Min, 1 at Max) and an offset across it to a point, for either orientation.
            float length = (_vertical ? height : width) - 2 * margin - (_vertical ? labelSize : labelSize * 2);
            float across = _vertical ? width : height;
            if (length < 10 || across < 8)
                return;
            float thickness = Math.Max(4f, across * 0.22f);
            float start = margin + (_vertical ? labelSize / 2 : labelSize);
            float cx = width / 2f, cy = height / 2f;
            // leave room on the label side
            float trackCentre = _vertical ? cx - across * 0.12f : cy - across * 0.1f;

            SKPoint P(double t, float offset)
            {
                return _vertical
                    ? new SKPoint(trackCentre + offset, start + (float)(1 - t) * length)
                    : new SKPoint(start + (float)t * length, trackCentre + offset);
            }

            SKPath Band(double t1, double t2, float o1, float o2)
            {
                var path = new SKPath();
                path.MoveTo(P(t1, o1));
                path.LineTo(P(t2, o1));
                path.LineTo(P(t2, o2));
                path.LineTo(P(t1, o2));
                path.Close();
                return path;
            }

            float half = thickness / 2;

            // the track and the ranges
            using (var track = Fill(new SKColor(0xD8, 0xD8, 0xD8)))
            using (var path = Band(0, 1, -half, half))
                canvas.DrawPath(path, track);
            foreach (var (from, to, color) in Ranges)
            {
                double a = Fraction(from), b = Fraction(to);
                if (b <= a)
                    continue;
                using var paint = Fill(color);
                using var path = Band(a, b, -half, half);
                canvas.DrawPath(path, paint);
            }

            // the value as a bar along the middle of the track
            using (var bar = Fill(PointerColor))
            using (var path = Band(0, Fraction(Value), -half * 0.38f, half * 0.38f))
                canvas.DrawPath(path, bar);

            // a marker at the value, on the side away from the labels
            using (var marker = Fill(InkColor))
            {
                var tip = P(Fraction(Value), -half - 1);
                var a = _vertical ? P(Fraction(Value), -half - 1 - thickness * 0.5f) : P(Fraction(Value), -half - 1 - thickness * 0.5f);
                using var path = new SKPath();
                path.MoveTo(tip);
                path.LineTo(_vertical ? new SKPoint(a.X, a.Y - thickness * 0.3f) : new SKPoint(a.X - thickness * 0.3f, a.Y));
                path.LineTo(_vertical ? new SKPoint(a.X, a.Y + thickness * 0.3f) : new SKPoint(a.X + thickness * 0.3f, a.Y));
                path.Close();
                canvas.DrawPath(path, marker);
            }

            // ticks and labels on the other side
            using (var tick = Stroke(InkColor, 1f))
            {
                foreach (var value in TickValues(length, labelSize))
                {
                    double t = Fraction(value);
                    canvas.DrawLine(P(t, half + 1), P(t, half + 1 + thickness * 0.4f), tick);
                    var at = P(t, half + 1 + thickness * 0.4f + labelSize * (_vertical ? 1.0f : 0.8f));
                    Label(canvas, Format(value), at.X, at.Y, labelSize, InkColor);
                }
            }
        }
    }
}
