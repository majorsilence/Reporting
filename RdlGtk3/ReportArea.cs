// 
//  ReportArea.cs
//  
//  Author:
//       Krzysztof Marecki 
// 
//  Copyright (c) 2010 Krzysztof Marecki
// 
// This file is part of the NReports project
// This file is part of the Majorsilence Reporting project 
//	
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using Cairo;
using Gdk;
using Gtk;
using Majorsilence.Reporting.Rdl;
using Pango;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Alignment = Pango.Alignment;
using CairoHelper = Pango.CairoHelper;
using Color = Cairo.Color;
using Context = Cairo.Context;
using Layout = Pango.Layout;
using Rectangle = Cairo.Rectangle;
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives

namespace Majorsilence.Reporting.RdlGtk3
{
    [ToolboxItem(true)]
    public class ReportArea : DrawingArea
    {
        private readonly object drawLock = new();
        internal float DpiX = 96;

        internal float DpiY = 96;

        private readonly List<HitListEntry> hitList = new();
        private Page pages;
        private readonly int rep_padding = 10;
        private Report report;
        private float scale = 1.0f;

        private PageItem selectedItem;
        private readonly int shadow_padding = 16;
        private ImageSurface cachedSurface = null;
        private float cachedScale = -1f;

        public ReportArea()
        {
            // Insert initialization code here.
            AddEvents((int)EventMask.ButtonPressMask);
        }

        public float Scale
        {
            get => scale;
            set
            {
                if (value != scale && value != 0)
                {
                    scale = value;

                    QueueResize();
                    //Window.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), true);
                }
            }
        }

        protected override void OnRealized()
        {
            base.OnRealized();
        }

        public void SetReport(Report report, Page pages)
        {
            lock (drawLock)
            {
                this.pages = pages;
                this.report = report;
                // Invalidate cached rendering for this page
                if (cachedSurface != null)
                {
                    try { cachedSurface.Dispose(); } catch { }
                    cachedSurface = null;
                    cachedScale = -1f;
                }
            }

            // Request a resize/draw on the main loop
            Application.Invoke((s, a) => QueueResize());
            //Window.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), true);
        }

        private Rectangle GetSelectedItemRectangle()
        {
            lock (drawLock)
            {
                if (selectedItem == null)
                    return new Rectangle(0, 0, 0, 0);

                return new Rectangle(selectedItem.X * scale, selectedItem.Y * scale, selectedItem.W * scale,
                    selectedItem.H * scale);
            }
        }

        protected override bool OnButtonPressEvent(EventButton ev)
        {
            if (ev.Button == 3)
            {
                HitListEntry hitAreaItem;
                lock (drawLock)
                {
                    hitAreaItem = hitList.FirstOrDefault(x => x.Contains(new PointD(ev.X, ev.Y)));
                }
                if (hitAreaItem == null)
                {
                    return false;
                }

                string text;
                if (hitAreaItem.pi is PageText)
                {
                    text = (hitAreaItem.pi as PageText).Text;
                }
                else if (hitAreaItem.pi is PageTextHtml)
                {
                    text = (hitAreaItem.pi as PageTextHtml).Text;
                }
                else
                {
                    return false;
                }

                lock (drawLock)
                {
                    selectedItem = hitAreaItem.pi;
                }
                QueueDraw();
                //Window.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), true);
                Menu popupMenu = new();
                MenuItem menuItem = new("Копировать");
                menuItem.Activated += (sender, e) =>
                {
                    Clipboard clipboard = Clipboard.Get(Atom.Intern("CLIPBOARD", false));
                    clipboard.Text = text;
                    lock (drawLock)
                    {
                        selectedItem = null;
                    }
                    QueueDraw();
                    //Window.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), true);
                };
                popupMenu.Add(menuItem);
                popupMenu.ShowAll();
                popupMenu.Popup();
                popupMenu.Hidden += (sender, e) =>
                {
                    lock (drawLock)
                    {
                        selectedItem = null;
                    }
                    QueueDraw();
                    //Window.InvalidateRect(new Gdk.Rectangle(0, 0, Allocation.Width, Allocation.Height), true);
                };
            }

            // Insert button press handling code here.
            return base.OnButtonPressEvent(ev);
        }

        internal float PixelsX(float x)
        {
            return x * DpiX / 96.0f;
        }

        internal float PixelsY(float y)
        {
            return y * DpiY / 96.0f;
        }

        private void SetItemsHitArea()
        {
            lock (drawLock)
            {
                float XAdditional = GetLeftAreaPosition();
                float YAdditional = rep_padding;
                hitList.Clear();
                if (pages == null)
                    return;

                foreach (PageItem pi in pages)
                {
                    if (pi is PageTextHtml)
                    {
                        // PageTextHtml is actually a composite object (just like a page)
                        Rectangle hr = new(PixelsX(pi.X * scale) + XAdditional, PixelsY(pi.Y * scale) + YAdditional,
                            PixelsX(pi.W * scale), PixelsY(pi.H * scale));
                        hitList.Add(new HitListEntry(hr, pi));
                        continue;
                    }

                    Rectangle rect = new(PixelsX(pi.X * scale) + XAdditional, PixelsY(pi.Y * scale) + YAdditional,
                        PixelsX(pi.W * scale), PixelsY(pi.H * scale));

                    if (pi is PageText || pi is PageImage)
                    {
                        hitList.Add(new HitListEntry(rect, pi));
                    }
                    // Only care about items with links and tips
                    else if (pi.HyperLink != null || pi.BookmarkLink != null || pi.Tooltip != null)
                    {
                        HitListEntry hle;
                        if (pi is PagePolygon)
                        {
                            hle = new HitListEntry(pi as PagePolygon, XAdditional, YAdditional, this);
                        }
                        else
                        {
                            hle = new HitListEntry(rect, pi);
                        }

                        hitList.Add(hle);
                    }
                }
            }
        }

        private int GetLeftAreaPosition()
        {
            lock (drawLock)
            {
                if (report == null)
                    return 0;

                int width = (int)(report.PageWidthPoints * Scale);
                int widgetWidth = Allocation.Width;
                int widgetHeight = Allocation.Height;

                int position = (widgetWidth - width) / 2;
                return position;
            }
        }

        protected override bool OnDrawn(Context g)
        {
            base.OnDrawn(g);

            lock (drawLock)
            {
                if (pages == null || report == null)
                {
                    return false;
                }

                int width = (int)(report.PageWidthPoints * Scale);
                int height = (int)(report.PageHeightPoints * Scale);
                // Defensive: avoid creating zero-sized surfaces which can crash Cairo/GDK
                if (width <= 0 || height <= 0)
                {
                    return false;
                }
                Rectangle rep_r = new(1, 1, width - 1, height - 1);

                int widgetWidth = Allocation.Width;
                int widgetHeight = Allocation.Height;

                g.Translate(((widgetWidth - width) / 2) - rep_padding, 0);

                // Wrap the drawing in a try/catch to prevent native crashes from bubbling up
                try
                {
                    // If cachedSurface exists at the current scale, paint it directly. Otherwise render and cache it.
                    if (cachedSurface == null || cachedScale != Scale)
                    {
                        // Dispose previous cached surface if any
                        if (cachedSurface != null)
                        {
                            try { cachedSurface.Dispose(); } catch { }
                            cachedSurface = null;
                        }

                        using (SolidPattern shadowGPattern = new(new Color(0.6, 0.6, 0.6)))
                        using (SolidPattern repGPattern = new(new Color(1, 1, 1)))
                        using (ImageSurface repS = new(Format.Argb32, width, height))
                        using (Context repG = new(repS))
                        using (ImageSurface shadowS = new ImageSurface(Format.Argb32, width, height))
                        using (Context shadowG = new(shadowS))
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            shadowG.Pattern = shadowGPattern;
#pragma warning restore CS0618 // Type or member is obsolete
                            shadowG.Paint();
                            g.SetSourceSurface(shadowS, shadow_padding, shadow_padding);
                            g.Paint();

#pragma warning disable CS0618 // Type or member is obsolete
                            repG.Pattern = repGPattern;
#pragma warning restore CS0618 // Type or member is obsolete
                            repG.Paint();
                            repG.DrawRectangle(rep_r, new Color(0.1, 0.1, 0.1), 1);

                            SetItemsHitArea();

                            Pattern currentRepGPattern = null;
                            if (selectedItem != null)
                            {
                                currentRepGPattern = new SolidPattern(new Color(0.4, 0.4, 1));
#pragma warning disable CS0618 // Type or member is obsolete
                                repG.Pattern = currentRepGPattern;
#pragma warning restore CS0618 // Type or member is obsolete
                                repG.Rectangle(GetSelectedItemRectangle());
                                repG.Fill();
                            }

                            using (RenderCairo render = new(repG, Scale))
                            {
                                render.RunPage(pages);
                            }

                            // Move the freshly rendered repS into cache by creating a clone surface
                            cachedSurface = repS.Clone();
                            cachedScale = Scale;

                            currentRepGPattern?.Dispose();
                        }
                    }

                    // Paint cached surface to widget
                    g.SetSourceSurface(cachedSurface, rep_padding, rep_padding);
                    g.Paint();
                }
                catch (System.Exception ex)
                {
                    // Log and bail out; avoid crashing the process for native drawing errors
                    System.Console.WriteLine("Render error in ReportArea.OnDrawn: " + ex);
                    return false;
                }

                return true;
            }
        }


        private void DrawString(string s, Context g, Rectangle r)
        {
            StyleInfo si = new();
            g.Save();

            Layout layout;

            layout = CairoHelper.CreateLayout(g);

            float fontsize = si.FontSize * 72 / 96;
            FontDescription font = FontDescription.FromString(string.Format("{0} {1}", si.GetFontFamily().Name,
                fontsize * PixelsX(1)));
            if (si.FontStyle == FontStyleEnum.Italic)
            {
                font.Style = Pango.Style.Italic;
            }

            switch (si.FontWeight)
            {
                case FontWeightEnum.Bold:
                case FontWeightEnum.Bolder:
                case FontWeightEnum.W500:
                case FontWeightEnum.W600:
                case FontWeightEnum.W700:
                case FontWeightEnum.W800:
                case FontWeightEnum.W900:
                    font.Weight = Weight.Bold;
                    break;
            }

            FontDescription oldfont = layout.FontDescription;
            layout.FontDescription = font;

            switch (si.TextAlign)
            {
                case TextAlignEnum.Right:
                    layout.Alignment = Alignment.Right;
                    break;
                case TextAlignEnum.Center:
                    layout.Alignment = Alignment.Center;
                    break;
                case TextAlignEnum.Left:
                default:
                    layout.Alignment = Alignment.Left;
                    break;
            }

            layout.Width = Units.FromPixels((int)(r.Width - si.PaddingLeft - si.PaddingRight - 2));
            //				layout.Width = 	(int)Pango.Units.FromPixels((int)r.Width);

            layout.SetText(s);

            Pango.Rectangle logical;
            Pango.Rectangle ink;
            layout.GetExtents(out ink, out logical);
            double height = logical.Height / Pango.Scale.PangoScale;
            double y = 0;
            switch (si.VerticalAlign)
            {
                case VerticalAlignEnum.Top:
                    y = r.Y + si.PaddingTop;
                    break;
                case VerticalAlignEnum.Middle:
                    y = r.Y + ((r.Height - height) / 2);
                    break;
                case VerticalAlignEnum.Bottom:
                    y = r.Y + (r.Height - height) - si.PaddingBottom;
                    break;
            }

            Rectangle box = new(
                r.X + si.PaddingLeft + 1,
                y,
                r.Width,
                r.Height);

            g.SetSourceColor(si.Color.ToCairoColor());

            g.MoveTo(box.X, box.Y);

            CairoHelper.ShowLayout(g, layout);

            layout.FontDescription = oldfont;
            g.Restore();
        }


        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
            // Insert layout code here.
        }

        public override void Destroy()
        {
            lock (drawLock)
            {
                try { cachedSurface?.Dispose(); } catch { }
                cachedSurface = null;
            }

            base.Destroy();
        }

        protected override void OnGetPreferredWidth(out int minimumWidth, out int naturalWidth)
        {
            if (report != null)
            {
                minimumWidth = (int)(report.PageWidthPoints * scale) + (rep_padding * 2);
                naturalWidth = minimumWidth;
            }
            else
            {
                minimumWidth = 0;
                naturalWidth = 0;
            }
        }

        protected override void OnGetPreferredHeight(out int minimumHeight, out int naturalHeight)
        {
            if (report != null)
            {
                minimumHeight = (int)(report.PageHeightPoints * scale) + (rep_padding * 2);
                naturalHeight = minimumHeight;
            }
            else
            {
                minimumHeight = 0;
                naturalHeight = 0;
            }
        }

        private class HitListEntry
        {
            internal readonly PageItem pi;
            internal readonly PointF[] poly;
            internal readonly Rectangle rect;

            internal HitListEntry(Rectangle r, PageItem pitem)
            {
                rect = r;
                pi = pitem;
                poly = null;
            }

            internal HitListEntry(PagePolygon pp, float x, float y, ReportArea ra)
            {
                pi = pp;
                poly = new PointF[pp.Points.Length];
                for (int i = 0; i < pp.Points.Length; i++)
                {
                    poly[i].X = ra.PixelsX(pp.Points[i].X + x);
                    poly[i].Y = ra.PixelsY(pp.Points[i].Y + y);
                }

                rect = new Rectangle(0, 0, 0, 0);
            }

            /// <summary>
            ///     Contains- determine whether point in the pageitem
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            internal bool Contains(PointD p)
            {
                return pi is PagePolygon ? PointInPolygon(p) : rect.ContainsPoint(p.X, p.Y);
            }

            /// <summary>
            ///     PointInPolygon: uses ray casting algorithm ( http://en.wikipedia.org/wiki/Point_in_polygon )
            /// </summary>
            /// <param name="p"></param>
            /// <returns></returns>
            private bool PointInPolygon(PointD p)
            {
                PointF p1, p2;
                bool bIn = false;
                if (poly.Length < 3)
                {
                    return false;
                }

                PointF op = new(poly[poly.Length - 1].X, poly[poly.Length - 1].Y);
                for (int i = 0; i < poly.Length; i++)
                {
                    PointF np = new(poly[i].X, poly[i].Y);
                    if (np.X > op.X)
                    {
                        p1 = op;
                        p2 = np;
                    }
                    else
                    {
                        p1 = np;
                        p2 = op;
                    }

                    if ((np.X < p.X) == (p.X <= op.X)
                        && (p.Y - p1.Y) * (p2.X - p1.X) < (p2.Y - p1.Y) * (p.X - p1.X))
                    {
                        bIn = !bIn;
                    }

                    op = np;
                }

                return bIn;
            }
        }
    }
}

