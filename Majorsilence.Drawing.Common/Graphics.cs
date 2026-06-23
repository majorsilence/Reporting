using SkiaSharp;


namespace Majorsilence.Drawing
{
    public class Graphics : IDisposable
    {
        private SKCanvas _canvas;
        private bool _ownsCanvas;

        public Text.TextRenderingHint TextRenderingHint { get; set; }
        public Drawing2D.InterpolationMode InterpolationMode { get; set; }
        public Drawing2D.SmoothingMode SmoothingMode { get; set; }
        public Drawing2D.PixelOffsetMode PixelOffsetMode { get; set; }
        public Drawing2D.CompositingQuality CompositingQuality { get; set; }
        private Drawing.GraphicsUnit _pageUnit = Drawing.GraphicsUnit.Display;

        public Drawing.GraphicsUnit PageUnit
        {
            get => _pageUnit;
            set
            {
                _pageUnit = value;
                // Reset any prior unit scale and re-apply for the new unit so that
                // drawing code using PageUnit=Millimeter or PageUnit=Point gets
                // automatic coordinate mapping to pixels on the SkiaSharp canvas.
                // (BarCodeEAN13 draws bar geometry in mm and relies on this.)
                _canvas.ResetMatrix();
                switch (value)
                {
                    case Drawing.GraphicsUnit.Millimeter:
                        _canvas.Scale(DpiX / 25.4f, DpiY / 25.4f);
                        break;
                    case Drawing.GraphicsUnit.Point:
                        _canvas.Scale(DpiX / 72f, DpiY / 72f);
                        break;
                    // Pixel, Display, World: pixel-identity — no scale needed.
                }
            }
        }
        public float DpiX { get; set; } = 96;
        public float DpiY { get; set; } = 96;

        public Drawing2D.Matrix Transform
        {
            get => new Drawing2D.Matrix(_canvas.TotalMatrix);
            set { if (value != null) _canvas.SetMatrix(value.ToSKMatrix()); }
        }

        public Graphics(SKCanvas canvas, bool ownsCanvas = false)
        {
            _canvas = canvas;
            _ownsCanvas = ownsCanvas;
        }

        // ── Canvas access ───────────────────────────────────────────────────

        public SKCanvas GetSkCanvas() => _canvas;

        // ── Clear ────────────────────────────────────────────────────────────

        public void Clear(Color color)
        {
            _canvas.Clear(color.ToSkColor());
        }

        // ── Transform ────────────────────────────────────────────────────────

        public void ResetTransform()
        {
            _canvas.ResetMatrix();
        }

        public void TranslateTransform(float dx, float dy,
            Drawing2D.MatrixOrder order = Drawing2D.MatrixOrder.Prepend)
        {
            _canvas.Translate(dx, dy);
        }

        public void RotateTransform(float angle,
            Drawing2D.MatrixOrder order = Drawing2D.MatrixOrder.Prepend)
        {
            _canvas.RotateDegrees(angle);
        }

        public void ScaleTransform(float sx, float sy,
            Drawing2D.MatrixOrder order = Drawing2D.MatrixOrder.Prepend)
        {
            _canvas.Scale(sx, sy);
        }

        public void MultiplyTransform(Drawing2D.Matrix matrix,
            Drawing2D.MatrixOrder order = Drawing2D.MatrixOrder.Prepend)
        {
            var current = _canvas.TotalMatrix;
            var result = order == Drawing2D.MatrixOrder.Append
                ? SKMatrix.Concat(current, matrix.ToSKMatrix())
                : SKMatrix.Concat(matrix.ToSKMatrix(), current);
            _canvas.SetMatrix(result);
        }

        public void AddMetafileComment(byte[] data)
        {
            // No equivalent in SkiaSharp; ignore.
        }

        // ── Clipping ─────────────────────────────────────────────────────────

        public void SetClip(Rectangle rect)
        {
            _canvas.ClipRect(new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height));
        }

        public void SetClip(RectangleF rect)
        {
            _canvas.ClipRect(new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height));
        }

        public void SetClip(Drawing2D.GraphicsPath path)
        {
            _canvas.ClipPath(path.ToSKPath());
        }

        public void ResetClip()
        {
            // SkiaSharp has no single-call clip reset. Clip is undone by restoring canvas state.
            // Callers should pair SetClip with Save/Restore for correct behaviour.
        }

        public RectangleF ClipBounds
        {
            get
            {
                var b = _canvas.LocalClipBounds;
                return new RectangleF(b.Left, b.Top, b.Width, b.Height);
            }
        }

        // ── State save/restore ───────────────────────────────────────────────

        public Drawing2D.GraphicsState Save()
        {
            var state = new Drawing2D.GraphicsState(_canvas);
            _canvas.Save();
            return state;
        }

        public void Restore(Drawing2D.GraphicsState state)
        {
            _canvas.RestoreToCount(state.SaveCount);
        }

        // ── Draw/Fill rectangles ─────────────────────────────────────────────

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            _canvas.DrawRect(ToSkRect(rect), pen.ToSkPaint());
        }

        public void DrawRectangle(Pen pen, RectangleF rect)
        {
            _canvas.DrawRect(ToSkRect(rect), pen.ToSkPaint());
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            _canvas.DrawRect(new SKRect(x, y, x + width, y + height), pen.ToSkPaint());
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            _canvas.DrawRect(new SKRect(x, y, x + width, y + height), pen.ToSkPaint());
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            _canvas.DrawRect(ToSkRect(rect), brush.ToSkPaint());
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            _canvas.DrawRect(ToSkRect(rect), brush.ToSkPaint());
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            _canvas.DrawRect(new SKRect(x, y, x + width, y + height), brush.ToSkPaint());
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            _canvas.DrawRect(new SKRect(x, y, x + width, y + height), brush.ToSkPaint());
        }

        public void FillRegion(Brush brush, Region region)
        {
            _canvas.DrawRect(new SKRect(region.X, region.Y,
                region.X + region.Width, region.Y + region.Height), brush.ToSkPaint());
        }

        // ── Draw/Fill ellipses ───────────────────────────────────────────────

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            _canvas.DrawOval(ToSkRect(rect), pen.ToSkPaint());
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            _canvas.DrawOval(ToSkRect(rect), pen.ToSkPaint());
        }

        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            _canvas.DrawOval(new SKRect(x, y, x + width, y + height), pen.ToSkPaint());
        }

        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            _canvas.DrawOval(new SKRect(x, y, x + width, y + height), pen.ToSkPaint());
        }

        public void FillEllipse(Brush brush, Rectangle rect)
        {
            _canvas.DrawOval(ToSkRect(rect), brush.ToSkPaint());
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            _canvas.DrawOval(ToSkRect(rect), brush.ToSkPaint());
        }

        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            _canvas.DrawOval(new SKRect(x, y, x + width, y + height), brush.ToSkPaint());
        }

        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            _canvas.DrawOval(new SKRect(x, y, x + width, y + height), brush.ToSkPaint());
        }

        // ── Draw/Fill arcs and pies ──────────────────────────────────────────

        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            using var path = new SKPath();
            path.AddArc(ToSkRect(rect), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            using var path = new SKPath();
            path.AddArc(ToSkRect(rect), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawArc(Pen pen, float x, float y, float width, float height,
            float startAngle, float sweepAngle)
        {
            using var path = new SKPath();
            path.AddArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            using var path = BuildPiePath(ToSkRect(rect), startAngle, sweepAngle);
            _canvas.DrawPath(path, brush.ToSkPaint());
        }

        public void FillPie(Brush brush, float x, float y, float width, float height,
            float startAngle, float sweepAngle)
        {
            using var path = BuildPiePath(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle);
            _canvas.DrawPath(path, brush.ToSkPaint());
        }

        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            using var path = BuildPiePath(ToSkRect(rect), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawPie(Pen pen, float x, float y, float width, float height,
            float startAngle, float sweepAngle)
        {
            using var path = BuildPiePath(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            using var path = BuildPiePath(new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom), startAngle, sweepAngle);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        private static SKPath BuildPiePath(SKRect rect, float startAngle, float sweepAngle)
        {
            var path = new SKPath();
            path.MoveTo(rect.MidX, rect.MidY);
            path.ArcTo(rect, startAngle, sweepAngle, false);
            path.Close();
            return path;
        }

        // ── Lines ────────────────────────────────────────────────────────────

        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            _canvas.DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.ToSkPaint());
        }

        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            _canvas.DrawLine(pt1.X, pt1.Y, pt2.X, pt2.Y, pen.ToSkPaint());
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            _canvas.DrawLine(x1, y1, x2, y2, pen.ToSkPaint());
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            _canvas.DrawLine(x1, y1, x2, y2, pen.ToSkPaint());
        }

        public void DrawLines(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            using var path = new SKPath();
            path.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i].X, points[i].Y);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            using var path = new SKPath();
            path.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++)
                path.LineTo(points[i].X, points[i].Y);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        // ── Curves ───────────────────────────────────────────────────────────

        public void DrawCurve(Pen pen, Point[] points, float tension = 0.5f)
        {
            if (points == null || points.Length < 2) return;
            var pts = Array.ConvertAll(points, p => new PointF(p.X, p.Y));
            DrawCurve(pen, pts, tension);
        }

        public void DrawCurve(Pen pen, PointF[] points, float tension = 0.5f)
        {
            if (points == null || points.Length < 2) return;
            using var path = new SKPath();
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            path.MoveTo(skPts[0]);
            for (int i = 1; i < skPts.Length; i++)
            {
                var p0 = i > 1 ? skPts[i - 2] : skPts[0];
                var p1 = skPts[i - 1];
                var p2 = skPts[i];
                var p3 = i < skPts.Length - 1 ? skPts[i + 1] : skPts[i];
                var cp1 = new SKPoint(p1.X + (p2.X - p0.X) * tension / 3f,
                                      p1.Y + (p2.Y - p0.Y) * tension / 3f);
                var cp2 = new SKPoint(p2.X - (p3.X - p1.X) * tension / 3f,
                                      p2.Y - (p3.Y - p1.Y) * tension / 3f);
                path.CubicTo(cp1, cp2, p2);
            }
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (points == null || numberOfSegments < 1) return;
            int end = Math.Min(offset + numberOfSegments + 1, points.Length);
            var segment = new PointF[end - offset];
            Array.Copy(points, offset, segment, 0, segment.Length);
            DrawCurve(pen, segment, tension);
        }

        public void DrawClosedCurve(Pen pen, PointF[] points, float tension = 0.5f)
        {
            if (points == null || points.Length < 3) return;
            // Close the curve by appending first points
            var closed = new PointF[points.Length + 3];
            Array.Copy(points, closed, points.Length);
            closed[points.Length] = points[0];
            closed[points.Length + 1] = points[1];
            closed[points.Length + 2] = points[2];
            DrawCurve(pen, closed, tension);
        }

        public void FillClosedCurve(Brush brush, PointF[] points, float tension = 0.5f)
        {
            if (points == null || points.Length < 3) return;
            using var path = new SKPath();
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            path.MoveTo(skPts[0]);
            for (int i = 1; i < skPts.Length; i++)
            {
                var p0 = i > 1 ? skPts[i - 2] : skPts[0];
                var p1 = skPts[i - 1];
                var p2 = skPts[i];
                var p3 = i < skPts.Length - 1 ? skPts[i + 1] : skPts[i];
                var cp1 = new SKPoint(p1.X + (p2.X - p0.X) * tension / 3f,
                                      p1.Y + (p2.Y - p0.Y) * tension / 3f);
                var cp2 = new SKPoint(p2.X - (p3.X - p1.X) * tension / 3f,
                                      p2.Y - (p3.Y - p1.Y) * tension / 3f);
                path.CubicTo(cp1, cp2, p2);
            }
            path.Close();
            _canvas.DrawPath(path, brush.ToSkPaint());
        }

        // ── Beziers ──────────────────────────────────────────────────────────

        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            using var path = new SKPath();
            path.MoveTo(pt1.X, pt1.Y);
            path.CubicTo(pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawBezier(Pen pen, float x1, float y1, float cx1, float cy1,
            float cx2, float cy2, float x2, float y2)
        {
            using var path = new SKPath();
            path.MoveTo(x1, y1);
            path.CubicTo(cx1, cy1, cx2, cy2, x2, y2);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawBeziers(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 4) return;
            using var path = new SKPath();
            path.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i + 2 < points.Length; i += 3)
                path.CubicTo(points[i].X, points[i].Y,
                              points[i + 1].X, points[i + 1].Y,
                              points[i + 2].X, points[i + 2].Y);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        // ── Polygons ─────────────────────────────────────────────────────────

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (points == null || points.Length < 2) return;
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            using var path = new SKPath();
            path.AddPoly(skPts, true);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void DrawPolygon(Pen pen, Point[] points)
        {
            if (points == null || points.Length < 2) return;
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            using var path = new SKPath();
            path.AddPoly(skPts, true);
            _canvas.DrawPath(path, pen.ToSkPaint());
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (points == null || points.Length < 3) return;
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            using var path = new SKPath();
            path.AddPoly(skPts, true);
            _canvas.DrawPath(path, brush.ToSkPaint());
        }

        public void FillPolygon(Brush brush, Point[] points)
        {
            if (points == null || points.Length < 3) return;
            var skPts = Array.ConvertAll(points, p => new SKPoint(p.X, p.Y));
            using var path = new SKPath();
            path.AddPoly(skPts, true);
            _canvas.DrawPath(path, brush.ToSkPaint());
        }

        // ── Paths ────────────────────────────────────────────────────────────

        public void DrawPath(Pen pen, Drawing2D.GraphicsPath path)
        {
            _canvas.DrawPath(path.ToSKPath(), pen.ToSkPaint());
        }

        public void FillPath(Brush brush, Drawing2D.GraphicsPath path)
        {
            _canvas.DrawPath(path.ToSKPath(), brush.ToSkPaint());
        }

        // ── Images ───────────────────────────────────────────────────────────

        public void DrawImage(Image image, Rectangle destRect)
        {
            if (image?.SkiaBitmap == null) return;
            var src = new SKRect(0, 0, image.SkiaBitmap.Width, image.SkiaBitmap.Height);
            var dst = ToSkRect(destRect);
            _canvas.DrawBitmap(image.SkiaBitmap, src, dst);
        }

        public void DrawImage(Image image, RectangleF destRect)
        {
            if (image?.SkiaBitmap == null) return;
            var src = new SKRect(0, 0, image.SkiaBitmap.Width, image.SkiaBitmap.Height);
            _canvas.DrawBitmap(image.SkiaBitmap, src, ToSkRect(destRect));
        }

        public void DrawImage(Image image, float x, float y)
        {
            if (image?.SkiaBitmap == null) return;
            _canvas.DrawBitmap(image.SkiaBitmap, x, y);
        }

        public void DrawImage(Image image, int x, int y)
        {
            if (image?.SkiaBitmap == null) return;
            _canvas.DrawBitmap(image.SkiaBitmap, x, y);
        }

        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image?.SkiaBitmap == null) return;
            _canvas.DrawBitmap(image.SkiaBitmap, ToSkRect(srcRect), ToSkRect(destRect));
        }

        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image?.SkiaBitmap == null) return;
            _canvas.DrawBitmap(image.SkiaBitmap, ToSkRect(srcRect), ToSkRect(destRect));
        }

        public void DrawImageUnscaled(Image image, Point pt)
        {
            DrawImage(image, pt.X, pt.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            DrawImage(image, x, y);
        }

        // ── Text ─────────────────────────────────────────────────────────────

        public void DrawString(string s, Font font, Brush brush, PointF point)
        {
            if (string.IsNullOrEmpty(s)) return;
            var skPaint = brush.ToSkPaint();
            var skFont = font.ToSkFont();
            _canvas.DrawText(s, point.X, point.Y - skFont.Metrics.Ascent, skFont, skPaint);
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new PointF(x, y));
        }

        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
        {
            if (string.IsNullOrEmpty(s)) return;
            var skFont = font.ToSkFont();
            var skPaint = brush.ToSkPaint();
            var bounds = new SKRect();
            skFont.MeasureText(s, out bounds);

            float x = point.X;
            float y = point.Y - skFont.Metrics.Ascent;

            if (format != null)
            {
                if (format.Alignment == StringAlignment.Center)
                    x -= bounds.Width / 2f;
                else if (format.Alignment == StringAlignment.Far)
                    x -= bounds.Width;
            }

            _canvas.DrawText(s, x, y, skFont, skPaint);
        }

        public void DrawString(string s, Font font, Brush brush, Rectangle layoutRect, StringFormat format)
        {
            DrawString(s, font, brush,
                new RectangleF(layoutRect.X, layoutRect.Y, layoutRect.Width, layoutRect.Height), format);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRect, StringFormat format)
        {
            if (string.IsNullOrEmpty(s)) return;

            var skFont = font.ToSkFont();
            var skPaint = brush.ToSkPaint();
            var metrics = skFont.Metrics;
            float lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
            if (lineHeight <= 0) lineHeight = font.Size * 1.2f;

            float maxWidth = layoutRect.Width;
            float x = layoutRect.X;
            float y = layoutRect.Y - metrics.Ascent;

            bool noWrap = format != null &&
                (format.FormatFlags & StringFormatFlags.NoWrap) != 0;

            // Clip to the layout rectangle so text never overflows the cell boundary.
            _canvas.Save();
            _canvas.ClipRect(new SKRect(layoutRect.X, layoutRect.Y,
                                        layoutRect.X + layoutRect.Width,
                                        layoutRect.Y + layoutRect.Height));

            var paragraphs = s.Replace("\r\n", "\n").Split('\n');
            foreach (var para in paragraphs)
            {
                if (noWrap || maxWidth <= 0)
                {
                    float drawX = ComputeAlignX(x, maxWidth, para, skFont, format);
                    _canvas.DrawText(para, drawX, y, skFont, skPaint);
                    y += lineHeight;
                    continue;
                }

                // Word-wrap
                var words = para.Length > 0 ? para.Split(' ') : new[] { "" };
                string currentLine = "";
                foreach (var word in words)
                {
                    string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                    skFont.MeasureText(testLine, out var testBounds);

                    if (testBounds.Width > maxWidth && currentLine.Length > 0)
                    {
                        float drawX = ComputeAlignX(x, maxWidth, currentLine, skFont, format);
                        _canvas.DrawText(currentLine, drawX, y, skFont, skPaint);
                        y += lineHeight;
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }

                    // If currentLine is now a single word wider than the cell,
                    // break it at character boundaries and flush all but the last chunk.
                    skFont.MeasureText(currentLine, out var lineBounds);
                    if (lineBounds.Width > maxWidth)
                    {
                        var chunks = BreakWordToFit(currentLine, maxWidth, skFont);
                        for (int ci = 0; ci < chunks.Count - 1; ci++)
                        {
                            float drawX = ComputeAlignX(x, maxWidth, chunks[ci], skFont, format);
                            _canvas.DrawText(chunks[ci], drawX, y, skFont, skPaint);
                            y += lineHeight;
                        }
                        currentLine = chunks.Count > 0 ? chunks[chunks.Count - 1] : "";
                    }
                }

                if (currentLine.Length > 0)
                {
                    float drawX = ComputeAlignX(x, maxWidth, currentLine, skFont, format);
                    _canvas.DrawText(currentLine, drawX, y, skFont, skPaint);
                    y += lineHeight;
                }
            }

            _canvas.Restore();
        }

        private static System.Collections.Generic.List<string> BreakWordToFit(string word, float maxWidth, SKFont font)
        {
            var chunks = new System.Collections.Generic.List<string>();
            var current = "";
            foreach (var ch in word)
            {
                var test = current + ch;
                font.MeasureText(test, out var b);
                if (b.Width > maxWidth && current.Length > 0)
                {
                    chunks.Add(current);
                    current = ch.ToString();
                }
                else
                {
                    current = test;
                }
            }
            if (current.Length > 0)
                chunks.Add(current);
            return chunks;
        }

        private static float ComputeAlignX(float baseX, float maxWidth, string text,
            SKFont font, StringFormat format)
        {
            if (format == null || format.Alignment == StringAlignment.Near || maxWidth <= 0)
                return baseX;

            font.MeasureText(text, out var b);
            if (format.Alignment == StringAlignment.Center)
                return baseX + (maxWidth - b.Width) / 2f;
            if (format.Alignment == StringAlignment.Far)
                return baseX + maxWidth - b.Width;
            return baseX;
        }

        // ── Measure ──────────────────────────────────────────────────────────

        public SizeF MeasureString(string text, Font font)
        {
            return MeasureString(text, font, 0, StringFormat.GenericTypographic);
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            return MeasureString(text, font, width, StringFormat.GenericTypographic);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
        {
            return MeasureString(text, font, (int)layoutArea.Width, stringFormat);
        }

        public SizeF MeasureString(string text, Font font, int maxWidth, StringFormat stringFormat)
        {
            var skFont = font.ToSkFont();
            var metrics = skFont.Metrics;
            // Font.Size is in points; SkiaSharp treats it as pixels. Scale to real pixels.
            float scale = DpiX / 72f;
            float lineHeight = (metrics.Descent - metrics.Ascent + metrics.Leading) * scale;
            if (lineHeight <= 0) lineHeight = font.Size * scale * 1.2f;

            if (string.IsNullOrEmpty(text))
                return new SizeF(0, lineHeight);

            bool noWrap = stringFormat != null &&
                (stringFormat.FormatFlags & StringFormatFlags.NoWrap) != 0;

            float maxLineWidth = 0f;
            float totalHeight = 0f;

            var paragraphs = text.Replace("\r\n", "\n").Split('\n');
            foreach (var para in paragraphs)
            {
                if (noWrap || maxWidth <= 0)
                {
                    skFont.MeasureText(para, out var b);
                    maxLineWidth = Math.Max(maxLineWidth, b.Width * scale);
                    totalHeight += lineHeight;
                    continue;
                }

                var words = para.Length > 0 ? para.Split(' ') : new[] { "" };
                string currentLine = "";
                foreach (var word in words)
                {
                    string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                    skFont.MeasureText(testLine, out var testBounds);
                    float testWidth = testBounds.Width * scale;

                    if (testWidth > maxWidth && currentLine.Length > 0)
                    {
                        skFont.MeasureText(currentLine, out var lb);
                        maxLineWidth = Math.Max(maxLineWidth, lb.Width * scale);
                        totalHeight += lineHeight;
                        currentLine = word;
                    }
                    else if (testWidth > maxWidth)
                    {
                        // Single word wider than max width
                        maxLineWidth = Math.Max(maxLineWidth, testWidth);
                        totalHeight += lineHeight;
                        currentLine = "";
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }

                if (currentLine.Length > 0)
                {
                    skFont.MeasureText(currentLine, out var lb);
                    maxLineWidth = Math.Max(maxLineWidth, lb.Width * scale);
                    totalHeight += lineHeight;
                }
            }

            if (totalHeight < lineHeight)
                totalHeight = lineHeight;

            return new SizeF(maxLineWidth, totalHeight);
        }

        public Region[] MeasureCharacterRanges(string text, Font font,
            RectangleF layoutRect, StringFormat stringFormat)
        {
            var skFont = font.ToSkFont();
            var regions = new List<Region>();

            if (stringFormat?.MeasurableCharacterRanges == null)
                return regions.ToArray();

            const float padding = 1.2f;
            foreach (var range in stringFormat.MeasurableCharacterRanges)
            {
                if (range.First < 0 || range.First + range.Length > text.Length)
                {
                    regions.Add(new Region(0, 0, 0, 0));
                    continue;
                }

                // Measure each range in isolation — the RenderBase word-wrap engine
                // computes line widths as (end[i] - start[startWord] + bearing), where
                // all starts must be approximately 0 for the arithmetic to work correctly.
                var substring = text.AsSpan(range.First, range.Length);
                skFont.MeasureText(substring, out var bounds);

                regions.Add(new Region(
                    (int)(layoutRect.X + bounds.Left),
                    (int)(layoutRect.Y + bounds.Top),
                    (int)(bounds.Width + 2 * padding),
                    (int)(bounds.Height + 2 * padding)
                ));
            }

            return regions.ToArray();
        }

        // ── Factory ──────────────────────────────────────────────────────────

        public static Graphics FromImage(Bitmap bm)
        {
            return bm.GetGraphics();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static SKRect ToSkRect(Rectangle r) =>
            new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);

        private static SKRect ToSkRect(RectangleF r) =>
            new SKRect(r.X, r.Y, r.X + r.Width, r.Y + r.Height);

        // ── Dispose ──────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_ownsCanvas)
                _canvas?.Dispose();
        }
    }
}
