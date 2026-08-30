// 
// CairoExtensions.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Cairo;
using Gdk;
using Color = Cairo.Color;
using Rectangle = Cairo.Rectangle;
using Draw2 = Majorsilence.Forms.Drawing;


namespace Majorsilence.Reporting.RdlGtk3
{
    public static class CairoExtensions
    {
        public static bool ContainsPoint(this Rectangle r, double x, double y)
        {
            if (x < r.X || x >= r.X + r.Width)
            {
                return false;
            }

            if (y < r.Y || y >= r.Y + r.Height)
            {
                return false;
            }

            return true;
        }

        public static ImageSurface Clone(this ImageSurface surf)
        {
            ImageSurface newsurf = new(surf.Format, surf.Width, surf.Height);

            using (Context g = new(newsurf))
            {
                g.SetSource(surf);
                g.Paint();
            }

            return newsurf;
        }

        public static Color ToCairoColor(this System.Drawing.Color color)
        {
            return new Color(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }
        // Most of these functions return an affected area
        // This can be ignored if you don't need it

        #region context

        public static Rectangle DrawRectangle(this Context g, Rectangle r, Color color, int lineWidth)
        {
            // Put it on a pixel line
            if (lineWidth == 1)
            {
                r = new Rectangle(r.X - 0.5, r.Y - 0.5, r.Width, r.Height);
            }

            g.Save();

            g.MoveTo(r.X, r.Y);
            g.LineTo(r.X + r.Width, r.Y);
            g.LineTo(r.X + r.Width, r.Y + r.Height);
            g.LineTo(r.X, r.Y + r.Height);
            g.LineTo(r.X, r.Y);

            g.SetSourceColor(color);
            g.LineWidth = lineWidth;
            g.LineCap = LineCap.Square;

            Rectangle dirty = g.StrokeExtents();
            g.Stroke();

            g.Restore();

            return dirty;
        }


        public static Rectangle FillRectangle(this Context g, Rectangle r, Color color)
        {
            g.Save();

            g.MoveTo(r.X, r.Y);
            g.LineTo(r.X + r.Width, r.Y);
            g.LineTo(r.X + r.Width, r.Y + r.Height);
            g.LineTo(r.X, r.Y + r.Height);
            g.LineTo(r.X, r.Y);

            g.SetSourceColor(color);

            Rectangle dirty = g.StrokeExtents();

            g.Fill();
            g.Restore();

            return dirty;
        }

        public static Rectangle FillRectangle(this Context g, Rectangle r, Pattern pattern)
        {
            g.Save();

            g.MoveTo(r.X, r.Y);
            g.LineTo(r.X + r.Width, r.Y);
            g.LineTo(r.X + r.Width, r.Y + r.Height);
            g.LineTo(r.X, r.Y + r.Height);
            g.LineTo(r.X, r.Y);

#pragma warning disable CS0618 // Type or member is obsolete
            g.Pattern = pattern;
#pragma warning restore CS0618 // Type or member is obsolete

            Rectangle dirty = g.StrokeExtents();
            g.Fill();

            g.Restore();

            return dirty;
        }

        public static void DrawPixbufRect(this Context g, Pixbuf pixbuf, Rectangle r, float scale)
        {
            double wScale = r.Width / scale / pixbuf.Width;
            double hScale = r.Height / scale / pixbuf.Height;
            g.Save();
            g.Scale(scale * wScale, scale * hScale);
            CairoHelper.SetSourcePixbuf(g, pixbuf, (int)(r.X / scale / wScale), (int)(r.Y / scale / hScale));
            g.Paint();
            g.Restore();
        }

        #endregion
    }
}