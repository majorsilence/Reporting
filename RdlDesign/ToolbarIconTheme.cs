// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlViewer;
using Bitmap = Majorsilence.Forms.Drawing.Bitmap;
using Color = System.Drawing.Color;
using Image = Majorsilence.Forms.Drawing.Image;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Dark-theme variants of the designer's older toolbar bitmaps (#526): the report item buttons (Text Box,
    /// Table, List, Matrix, Subreport, Rectangle, Line), the select and properties buttons, and the export
    /// buttons (PDF, HTML, XLS, ...). Each was drawn on an opaque white tile, which shows as a white box on a
    /// dark toolbar, and in black or pure blue, which is hard to read there. On a dark theme each such
    /// bitmap is shown with its white taken out, its greys and black in the theme's text colour, and its dark
    /// colours lightened; on a light theme it is the original. The Tango icons (Properties.Resources) have
    /// transparent backgrounds and read on both, so they are left alone.
    /// </summary>
    internal static class ToolbarIconTheme
    {
        private sealed class Entry
        {
            public Image Original;
            public Image Dark;
            public Color DarkFor;       // the text colour Dark was made with
        }

        private static readonly ConditionalWeakTable<ToolStripItem, Entry> s_entries = new();

        /// <summary>
        /// Records the white-tile bitmaps of <paramref name="strips"/>' items; call it right after
        /// InitializeComponent, before a host's <see cref="IDesignerIconProvider"/> can replace any.
        /// </summary>
        internal static void Register(params ToolStrip[] strips)
        {
            foreach (var item in Items(strips))
            {
                if (item.Image is Bitmap bmp && HasWhiteTile(bmp) && !s_entries.TryGetValue(item, out _))
                    s_entries.Add(item, new Entry { Original = bmp });
            }
        }

        /// <summary>
        /// Shows each registered item's dark variant or its original, as <paramref name="owner"/>'s theme
        /// is dark or light. An item whose image was since replaced (by an icon provider) is left as it is.
        /// </summary>
        internal static void Apply(Control owner, params ToolStrip[] strips) => Apply(ScopeOf(owner), strips);

        // The scope a control paints under: its own, else the nearest ancestor's, else its window's.
        private static ThemeScope ScopeOf(Control control)
        {
            for (var c = control; c != null; c = c.Parent)
                if (c.ThemeScope != null)
                    return c.ThemeScope;
            return control?.FindForm()?.ThemeScope;
        }

        /// <summary>As above, for a window: its own scope, if it has one.</summary>
        internal static void Apply(Form owner, params ToolStrip[] strips) => Apply(owner?.ThemeScope, strips);

        // scope: the theme the toolbars are painted under; null for the application's.
        private static void Apply(ThemeScope scope, ToolStrip[] strips)
        {
            bool dark;
            Color text;
            using (scope?.Enter())
            {
                dark = ChromeColors.IsDarkTheme;
                text = ChromeColors.Text;
            }

            foreach (var item in Items(strips))
            {
                if (!s_entries.TryGetValue(item, out var entry))
                    continue;
                if (!ReferenceEquals(item.Image, entry.Original) && !ReferenceEquals(item.Image, entry.Dark))
                {
                    s_entries.Remove(item);     // replaced by the host; not ours to change
                    continue;
                }
                if (dark && (entry.Dark == null || entry.DarkFor != text))
                {
                    entry.Dark = DarkVariant((Bitmap)entry.Original, text);
                    entry.DarkFor = text;
                }
                item.Image = dark ? entry.Dark : entry.Original;
            }
        }

        private static IEnumerable<ToolStripItem> Items(ToolStrip[] strips)
        {
            var stack = new Stack<ToolStripItem>();
            foreach (var strip in strips)
            {
                if (strip == null) continue;
                foreach (ToolStripItem item in strip.Items)
                    stack.Push(item);
            }
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                yield return item;
                if (item is ToolStripDropDownItem dd && dd.HasDropDownItems)
                    foreach (ToolStripItem child in dd.DropDownItems)
                        stack.Push(child);
            }
        }

        // An opaque white corner on every side: the bitmap was drawn on a white tile.
        private static bool HasWhiteTile(Bitmap bmp)
        {
            int w = bmp.Width - 1, h = bmp.Height - 1;
            foreach (var (x, y) in new[] { (0, 0), (w, 0), (0, h), (w, h) })
            {
                var c = bmp.GetPixel(x, y);
                if (c.A < 250 || c.R < 250 || c.G < 250 || c.B < 250)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <paramref name="source"/> taken off its white tile: each pixel is read as a colour laid over
        /// white with some opacity, which keeps anti-aliased edges smooth. What is left that is grey or black
        /// becomes <paramref name="text"/>; a dark colour is lightened to read on a dark background.
        /// </summary>
        internal static Bitmap DarkVariant(Bitmap source, Color text)
        {
            var result = new Bitmap(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var p = source.GetPixel(x, y);
                    result.SetPixel(x, y, DarkPixel(p, text));
                }
            }
            return result;
        }

        internal static Color DarkPixel(Color p, Color text)
        {
            // The least opacity over white that gives p: the smallest channel's distance from white.
            int min = Math.Min(p.R, Math.Min(p.G, p.B));
            double alpha = (255 - min) / 255.0;
            if (alpha <= 0.02 || p.A == 0)
                return Color.Transparent;
            int Unmix(int c) => Math.Clamp((int)Math.Round((c - 255 * (1 - alpha)) / alpha), 0, 255);
            int r = Unmix(p.R), g = Unmix(p.G), b = Unmix(p.B);
            int a = (int)Math.Round(p.A * alpha);

            int max = Math.Max(r, Math.Max(g, b)), lo = Math.Min(r, Math.Min(g, b));
            if (max - lo < 48)
                return Color.FromArgb(a, text.R, text.G, text.B);      // black and grey lines

            // A colour: lighten one too dark to read toward white, keeping its hue.
            double lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
            const double wanted = 170;
            if (lum < wanted)
            {
                double k = (wanted - lum) / (255 - lum);
                r += (int)Math.Round((255 - r) * k);
                g += (int)Math.Round((255 - g) * k);
                b += (int)Math.Round((255 - b) * k);
            }
            return Color.FromArgb(a, r, g, b);
        }
    }
}
