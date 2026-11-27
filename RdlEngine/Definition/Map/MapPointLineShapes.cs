using System;
using System.Collections.Generic;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>A line of a line layer: its parts, stroked at a width in points, solid, dashed or dotted.</summary>
    internal sealed class MapLineShape : MapDrawable
    {
        internal readonly List<MapPoint[]> Parts;
        internal readonly bool Closed;     // polygon rings drawn as lines
        internal Color Color;
        internal float WidthPoints;
        internal BorderStyleEnum Dash = BorderStyleEnum.Solid;

        internal MapLineShape (List<MapPoint[]> projectedParts, bool closed)
        {
            Parts = projectedParts;
            Closed = closed;
        }

        internal override MapBounds? Bounds ()
        {
            MapBounds? bounds = null;
            foreach (var part in Parts)
                bounds = MapBounds.Union (bounds, MapBounds.Of (part));
            return bounds;
        }

        internal override float OverhangPoints => WidthPoints / 2;

        internal override void Draw (SKCanvas canvas, MapFrame frame)
        {
            if (Color.IsEmpty || Color.A == 0 || WidthPoints <= 0)
                return;
            using var path = new SKPath ();
            foreach (var part in Parts) {
                path.MoveTo (frame.ToPixel (part[0]));
                for (var i = 1; i < part.Length; i++)
                    path.LineTo (frame.ToPixel (part[i]));
                if (Closed)
                    path.Close ();
            }
            var width = Math.Max (0.5f, WidthPoints * frame.PixelsPerPoint);
            using var paint = new SKPaint {
                Color = MapScene.ToSk (Color), IsStroke = true, IsAntialias = true, StrokeWidth = width,
                StrokeJoin = SKStrokeJoin.Round, StrokeCap = Dash == BorderStyleEnum.Dotted ? SKStrokeCap.Round : SKStrokeCap.Butt,
            };
            if (Dash == BorderStyleEnum.Dashed)
                paint.PathEffect = SKPathEffect.CreateDash (new[] { width * 3, width * 2 }, 0);
            else if (Dash == BorderStyleEnum.Dotted)
                paint.PathEffect = SKPathEffect.CreateDash (new[] { 0.01f, width * 2 }, 0);     // round caps make the dots
            canvas.DrawPath (path, paint);
        }
    }

    /// <summary>A marker of a point layer, centred on its point (a push pin stands on it), sized in points.</summary>
    internal sealed class MapMarkerShape : MapDrawable
    {
        internal readonly MapPoint Center;
        internal MapMarkerStyle Marker = MapMarkerStyle.Circle;
        internal float SizePoints;
        internal Color Fill;
        internal Color Border;
        internal float BorderPoints;

        internal MapMarkerShape (MapPoint projected)
        {
            Center = projected;
        }

        internal override MapBounds? Bounds () => MapBounds.Of (new[] { Center });

        internal override float OverhangPoints
            => (Marker == MapMarkerStyle.PushPin ? SizePoints * 1.5f : SizePoints / 2) + BorderPoints;

        internal override void Draw (SKCanvas canvas, MapFrame frame)
        {
            if (Marker == MapMarkerStyle.None || SizePoints <= 0)
                return;
            var at = frame.ToPixel (Center);
            var size = Math.Max (1f, SizePoints * frame.PixelsPerPoint);
            using var path = Outline (Marker, at.X, at.Y, size);
            if (!Fill.IsEmpty && Fill.A > 0) {
                using var fill = new SKPaint { Color = MapScene.ToSk (Fill), IsStroke = false, IsAntialias = true };
                canvas.DrawPath (path, fill);
            }
            if (!Border.IsEmpty && Border.A > 0 && BorderPoints > 0) {
                using var stroke = new SKPaint {
                    Color = MapScene.ToSk (Border), IsStroke = true, IsAntialias = true,
                    StrokeWidth = Math.Max (0.5f, BorderPoints * frame.PixelsPerPoint), StrokeJoin = SKStrokeJoin.Round,
                };
                canvas.DrawPath (path, stroke);
            }
        }

        /// <summary>The outline of a marker <paramref name="size"/> pixels across at (x, y).</summary>
        internal static SKPath Outline (MapMarkerStyle marker, float x, float y, float size)
        {
            var h = size / 2;
            var path = new SKPath ();
            switch (marker) {
                case MapMarkerStyle.Rectangle:
                    path.AddRect (new SKRect (x - h, y - h, x + h, y + h));
                    break;
                case MapMarkerStyle.Diamond:
                    Polygon (path, (x, y - h), (x + h, y), (x, y + h), (x - h, y));
                    break;
                case MapMarkerStyle.Triangle:
                    Polygon (path, (x, y - h), (x + h, y + h), (x - h, y + h));
                    break;
                case MapMarkerStyle.Trapezoid:
                    Polygon (path, (x - h / 2, y - h), (x + h / 2, y - h), (x + h, y + h), (x - h, y + h));
                    break;
                case MapMarkerStyle.Wedge:
                    // a downward wedge, its point on the location's row
                    Polygon (path, (x - h, y - h), (x + h, y - h), (x, y + h));
                    break;
                case MapMarkerStyle.Pentagon:
                    Regular (path, x, y, h, 5, 1);
                    break;
                case MapMarkerStyle.Star:
                    Regular (path, x, y, h, 5, 0.4f);
                    break;
                case MapMarkerStyle.PushPin:
                    // a round head a marker across, on a point that stands on the location
                    var head = y - size;
                    path.MoveTo (x, y);
                    path.LineTo (x - h * 0.7071f, head + h * 0.7071f);
                    path.ArcTo (new SKRect (x - h, head - h, x + h, head + h), 135, 270, false);
                    path.Close ();
                    break;
                default:
                    path.AddCircle (x, y, h);
                    break;
            }
            return path;
        }

        private static void Polygon (SKPath path, params (float X, float Y)[] points)
        {
            path.MoveTo (points[0].X, points[0].Y);
            for (var i = 1; i < points.Length; i++)
                path.LineTo (points[i].X, points[i].Y);
            path.Close ();
        }

        /// <summary>A regular polygon of <paramref name="corners"/> points up, or a star when the inner radius is less than 1.</summary>
        private static void Regular (SKPath path, float x, float y, float radius, int corners, float inner)
        {
            var star = inner < 1;
            var steps = star ? corners * 2 : corners;
            for (var i = 0; i < steps; i++) {
                var r = star && i % 2 == 1 ? radius * inner : radius;
                var angle = -Math.PI / 2 + i * Math.PI * 2 / steps;
                var px = x + (float)(r * Math.Cos (angle));
                var py = y + (float)(r * Math.Sin (angle));
                if (i == 0)
                    path.MoveTo (px, py);
                else
                    path.LineTo (px, py);
            }
            path.Close ();
        }
    }
}
