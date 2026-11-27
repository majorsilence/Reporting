using System;
using System.Collections.Generic;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Everything one map instance draws, evaluated and projected: the layers' shapes in drawing
    /// order, the rules' buckets, and the overlays drawn over the item. Built once per instance by
    /// <see cref="Map"/>; drawing it needs no report or data, so the page image can draw it again at
    /// whatever resolution the output asks for (PDF, HTML and the image formats all go through the
    /// same PNG).
    /// </summary>
    internal sealed class MapScene
    {
        internal readonly MapViewport Viewport;
        internal readonly float WidthPoints;
        internal readonly float HeightPoints;
        internal Color ViewportBackground = Color.Empty;

        /// <summary>Shapes in projected coordinates, drawn in order (first layer at the bottom).</summary>
        internal readonly List<MapDrawable> Drawables = new List<MapDrawable> ();

        /// <summary>
        /// What each layer's rules made of its values, for a legend or colour scale to show. A layer
        /// adds one result per rule it applied.
        /// </summary>
        internal readonly List<MapRuleResult> Rules = new List<MapRuleResult> ();

        /// <summary>Items drawn over the whole map item after the viewport (legends, titles, scales).</summary>
        internal readonly List<MapOverlay> Overlays = new List<MapOverlay> ();

        internal MapScene (MapViewport viewport, float widthPoints, float heightPoints)
        {
            Viewport = viewport;
            WidthPoints = widthPoints;
            HeightPoints = heightPoints;
        }

        /// <summary>The projected extent of every shape, or null when there is nothing to draw.</summary>
        internal MapBounds? Bounds ()
        {
            MapBounds? bounds = null;
            foreach (var d in Drawables)
                bounds = MapBounds.Union (bounds, d.Bounds ());
            return bounds;
        }

        internal byte[] ToPng (int width, int height)
        {
            width = Math.Max (1, width);
            height = Math.Max (1, height);
            using var bitmap = new SKBitmap (width, height);
            using (var canvas = new SKCanvas (bitmap)) {
                canvas.Clear (SKColors.Transparent);
                Draw (canvas, width, height);
            }
            using var image = SKImage.FromBitmap (bitmap);
            using var data = image.Encode (SKEncodedImageFormat.Png, 100);
            return data.ToArray ();
        }

        internal void Draw (SKCanvas canvas, int width, int height)
        {
            var frame = Frame (width, height);
            if (!ViewportBackground.IsEmpty && ViewportBackground.A > 0) {
                using var background = new SKPaint { Color = ToSk (ViewportBackground), IsStroke = false };
                canvas.DrawRect (frame.Viewport, background);
            }
            if (frame.HasData) {
                canvas.Save ();
                canvas.ClipRect (frame.Viewport);
                foreach (var d in Drawables)
                    d.Draw (canvas, frame);
                canvas.Restore ();
            }
            foreach (var overlay in Overlays)
                overlay.Draw (canvas, frame);
        }

        /// <summary>Fits the shapes into the viewport: one scale for both axes, centred, a small margin, and the view's zoom.</summary>
        internal MapFrame Frame (int width, int height)
        {
            var frame = new MapFrame {
                Width = width,
                Height = height,
                PixelsPerPoint = WidthPoints > 0 ? width / WidthPoints : 1,
            };
            var v = Viewport;
            frame.Viewport = SKRect.Create (
                (float)(width * v.Left / 100), (float)(height * v.Top / 100),
                (float)(width * v.Width / 100), (float)(height * v.Height / 100));

            var bounds = Bounds ();
            if (bounds == null)
                return frame;
            var b = bounds.Value;
            var margin = Math.Min (frame.Viewport.Width, frame.Viewport.Height) * 0.03f;
            // Markers and wide lines reach past their coordinates: keep them inside, up to a third of the viewport.
            var overhang = 0f;
            foreach (var d in Drawables)
                overhang = Math.Max (overhang, d.OverhangPoints * frame.PixelsPerPoint);
            margin = Math.Max (margin, Math.Min (overhang, Math.Min (frame.Viewport.Width, frame.Viewport.Height) / 3));
            var dx = Math.Max (b.MaxX - b.MinX, 1e-9);
            var dy = Math.Max (b.MaxY - b.MinY, 1e-9);
            var scale = Math.Min ((frame.Viewport.Width - 2 * margin) / dx, (frame.Viewport.Height - 2 * margin) / dy);
            if (scale <= 0 || double.IsNaN (scale) || double.IsInfinity (scale))
                return frame;
            frame.Scale = scale * (v.Zoom > 0 ? v.Zoom / 100.0 : 1);
            frame.CenterX = (b.MinX + b.MaxX) / 2;
            frame.CenterY = (b.MinY + b.MaxY) / 2;
            frame.HasData = true;
            return frame;
        }

        internal static SKColor ToSk (Color c) => new SKColor (c.R, c.G, c.B, c.A);
    }

    /// <summary>An extent on the projected plane.</summary>
    internal readonly struct MapBounds
    {
        internal readonly double MinX, MinY, MaxX, MaxY;

        internal MapBounds (double minX, double minY, double maxX, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        internal static MapBounds? Of (IEnumerable<MapPoint> points)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            var any = false;
            foreach (var p in points) {
                if (double.IsNaN (p.X) || double.IsNaN (p.Y) || double.IsInfinity (p.X) || double.IsInfinity (p.Y))
                    continue;
                any = true;
                minX = Math.Min (minX, p.X);
                minY = Math.Min (minY, p.Y);
                maxX = Math.Max (maxX, p.X);
                maxY = Math.Max (maxY, p.Y);
            }
            return any ? new MapBounds (minX, minY, maxX, maxY) : (MapBounds?)null;
        }

        internal static MapBounds? Union (MapBounds? a, MapBounds? b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;
            return new MapBounds (Math.Min (a.Value.MinX, b.Value.MinX), Math.Min (a.Value.MinY, b.Value.MinY),
                Math.Max (a.Value.MaxX, b.Value.MaxX), Math.Max (a.Value.MaxY, b.Value.MaxY));
        }
    }

    /// <summary>Where things go on the bitmap being drawn: its size, the viewport, and the projected-to-pixel transform.</summary>
    internal sealed class MapFrame
    {
        internal float Width;
        internal float Height;
        internal float PixelsPerPoint;
        internal SKRect Viewport;
        internal bool HasData;
        internal double Scale;
        internal double CenterX;
        internal double CenterY;

        internal SKPoint ToPixel (MapPoint projected)
            => new SKPoint (
                (float)(Viewport.MidX + (projected.X - CenterX) * Scale),
                (float)(Viewport.MidY - (projected.Y - CenterY) * Scale));
    }

    /// <summary>Something drawn inside the viewport, in projected coordinates. Point and line layers add their own kinds.</summary>
    internal abstract class MapDrawable
    {
        internal abstract MapBounds? Bounds ();
        internal abstract void Draw (SKCanvas canvas, MapFrame frame);

        /// <summary>How far, in points, the drawing reaches past its bounds (half a marker, half a line's width).</summary>
        internal virtual float OverhangPoints => 0;
    }

    /// <summary>Something drawn over the whole map item, in pixels of the bitmap: a legend, a title, a scale.</summary>
    internal abstract class MapOverlay
    {
        internal abstract void Draw (SKCanvas canvas, MapFrame frame);
    }

    /// <summary>A filled polygon (its outer rings and holes, even-odd) with an outline.</summary>
    internal sealed class MapPolygonShape : MapDrawable
    {
        internal readonly List<MapPoint[]> Rings;
        internal Color Fill;
        internal Color Border;
        internal float BorderPoints;

        internal MapPolygonShape (List<MapPoint[]> projectedRings)
        {
            Rings = projectedRings;
        }

        internal override MapBounds? Bounds ()
        {
            MapBounds? bounds = null;
            foreach (var ring in Rings)
                bounds = MapBounds.Union (bounds, MapBounds.Of (ring));
            return bounds;
        }

        internal override void Draw (SKCanvas canvas, MapFrame frame)
        {
            using var path = new SKPath { FillType = SKPathFillType.EvenOdd };
            foreach (var ring in Rings) {
                path.MoveTo (frame.ToPixel (ring[0]));
                for (var i = 1; i < ring.Length; i++)
                    path.LineTo (frame.ToPixel (ring[i]));
                path.Close ();
            }
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
    }
}
