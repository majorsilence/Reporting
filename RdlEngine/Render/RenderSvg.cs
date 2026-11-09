/*
 * Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SkiaSharp;
using Draw2 = Majorsilence.Forms.Drawing;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Renders a report as SVG, one vector image per page. This is a page oriented formatting renderer.
    ///</summary>
    /// <remarks>
    /// <para>An SVG holds one page, so page 1 is written to the main stream and every later page to
    /// a stream of its own from <see cref="IStreamGen.GetIOStream"/>, in page order. Written through
    /// <see cref="OneFileStreamGen"/> (RdlCmd, the viewers' export) that is report.svg, report2.svg,
    /// report3.svg, ...; through <see cref="MemoryStreamGen"/> it is MemoryList[0], [1], [2], ...
    /// A caller that only reads the main stream gets the first page.</para>
    /// <para>The page is drawn by the same page drawing as TIF (<see cref="RenderTif.DrawPage"/>),
    /// onto SkiaSharp's SVG canvas instead of a bitmap, so it matches the TIF and the screen.
    /// Text, lines, rectangles, ellipses, pies, polygons, curves and gradient backgrounds come out
    /// as SVG elements, text as &lt;text&gt;; images (pictures, and the charts, barcodes and other
    /// custom report items that the engine renders as images) are embedded PNG/JPEG data.</para>
    /// <para>Coordinates are in CSS pixels at 96 per inch, the SVG user unit, so the page has its
    /// real size when it is placed in a document; the root element carries a viewBox so it scales.
    /// FinishSvg says what else is put right in SkiaSharp's output.</para>
    /// </remarks>
    internal class RenderSvg : IPresent
    {
        // CSS pixels per inch: an SVG user unit with no unit given is 1/96 inch.
        private const float SvgUnitsPerInch = 96f;

        Report r;               // report
        IStreamGen _sg;         // page 1 goes to its main stream, later pages to new streams

        public RenderSvg(Report rep, IStreamGen sg)
        {
            r = rep;
            _sg = sg;
        }

        public void Dispose() { }

        public Report Report()
        {
            return r;
        }

        public bool IsPagingNeeded()
        {
            return true;
        }

        public void Start()
        {
        }

        public Task End()
        {
            return Task.CompletedTask;
        }

        public async Task RunPages(Pages pgs)   // this does all the work
        {
            // Page coordinates are points; RenderTif converts them at 72 dpi, i.e. leaves them in
            // points, and the Graphics below takes them as points (PageUnit.Point) and scales them
            // to 96 per inch itself. Fonts, pen widths and padding are all in points as well, so
            // they scale with the rest of the page.
            var drawer = new RenderTif(r, 72f);
            float widthPx = r.ReportDefinition.PageWidth.Size / 2540F * SvgUnitsPerInch;
            float heightPx = r.ReportDefinition.PageHeight.Size / 2540F * SvgUnitsPerInch;

            int pageNo = 1;
            foreach (Page p in pgs)
            {
                byte[] svg = await RenderPage(drawer, p, widthPx, heightPx).ConfigureAwait(false);
                if (pageNo == 1)
                {
                    _sg.GetStream().Write(svg, 0, svg.Length);
                }
                else
                {
                    Stream io = _sg.GetIOStream(out string relativeName, "svg");
                    try
                    {
                        io.Write(svg, 0, svg.Length);
                    }
                    finally
                    {
                        io.Flush();
                        // Closing a MemoryStream still leaves MemoryStreamGen's caller its ToArray.
                        io.Close();
                    }
                }
                pageNo++;
            }
        }

        private async Task<byte[]> RenderPage(RenderTif drawer, Page p, float widthPx, float heightPx)
        {
            using var ms = new MemoryStream();
            // The SVG is written out when the canvas is disposed, so it is finished before ms is read.
            using (var canvas = SKSvgCanvas.Create(SKRect.Create(widthPx, heightPx), ms))
            {
                Draw2.Graphics? g = CreateGraphics(canvas);
                if (g != null)
                {
                    using (g)
                    {
                        g.PageUnit = Draw2.GraphicsUnit.Point;
                        g.FillRectangle(Draw2.Brushes.White, 0F, 0F, widthPx * 72f / SvgUnitsPerInch, heightPx * 72f / SvgUnitsPerInch);
                        await drawer.DrawPage(g, p).ConfigureAwait(false);
                    }
                }
                else
                {
                    await DrawRasterFallback(p, canvas, widthPx, heightPx).ConfigureAwait(false);
                }
            }
            return FinishSvg(ms.ToArray(), widthPx, heightPx);
        }

        // Majorsilence.Forms.Drawing.Graphics has no public way to draw onto a caller's SKCanvas -
        // only Graphics.FromImage, onto a bitmap it owns - but it does have an internal constructor
        // taking an SKCanvas (the one its own controls paint through). That is the only way to get
        // the GDI-style page drawing onto the SVG canvas, so it is found by reflection; the
        // DynamicDependency keeps it through trimming and Native AOT. Null when a later
        // Majorsilence.Forms no longer has it, and the page is then drawn as a picture instead.
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(Draw2.Graphics))]
        private static Draw2.Graphics? CreateGraphics(SKCanvas canvas)
        {
            foreach (ConstructorInfo ci in typeof(Draw2.Graphics).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                ParameterInfo[] ps = ci.GetParameters();
                if (ps.Length == 0 || ps[0].ParameterType != typeof(SKCanvas))
                    continue;
                bool restOptional = true;
                for (int i = 1; i < ps.Length; i++)
                    restOptional &= ps[i].IsOptional;
                if (!restOptional)
                    continue;

                object?[] args = new object?[ps.Length];
                args[0] = canvas;
                for (int i = 1; i < ps.Length; i++)
                    args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
                try
                {
                    return (Draw2.Graphics?)ci.Invoke(args);
                }
                catch (TargetInvocationException)
                {
                    return null;
                }
            }
            return null;
        }

        // The page drawn as TIF draws it, onto a bitmap, and the bitmap placed on the SVG page.
        // Used only when CreateGraphics finds no way to draw onto the SVG canvas directly.
        private const float FallbackDpi = 192f;

        private async Task DrawRasterFallback(Page p, SKCanvas canvas, float widthPx, float heightPx)
        {
            var drawer = new RenderTif(r, FallbackDpi);
            float scale = FallbackDpi / SvgUnitsPerInch;
            using var skBitmap = new SKBitmap(Math.Max(1, (int)Math.Ceiling(widthPx * scale)), Math.Max(1, (int)Math.Ceiling(heightPx * scale)));
            Draw2.Image bm = skBitmap!;
            using (Draw2.Graphics g = Draw2.Graphics.FromImage(bm))
            {
                g.PageUnit = Draw2.GraphicsUnit.Pixel;
                g.FillRectangle(Draw2.Brushes.White, 0F, 0F, (float)bm.Width, (float)bm.Height);
                await drawer.DrawPage(g, p).ConfigureAwait(false);
            }
            using var image = SKImage.FromBitmap(skBitmap);
            canvas.DrawImage(image, SKRect.Create(widthPx, heightPx));
        }

        // SkiaSharp's SVG needs three things put right before it is handed out:
        //  - the root has its width and height rounded up to whole pixels and no viewBox, so the
        //    page is a fraction too big and does not scale when it is given another size (a design
        //    tool, an <img> with a width); it gets the exact size and a viewBox.
        //  - a <text>'s glyph positions, x="0, 10.8, 21, " and y="12.9, ", end in a separator;
        //    strict readers (librsvg, so GNOME and LibreOffice) reject the whole list and pile the
        //    letters up at the origin. The trailing separator is dropped.
        //  - a <text>'s characters sit between a newline and tabs of indentation. With SVG's
        //    default whitespace handling that only works while the text has no run of spaces, which
        //    is collapsed and moves every later letter onto the wrong position. The indentation is
        //    taken off and the text marked xml:space="preserve", so each character keeps its x.
        private static byte[] FinishSvg(byte[] svg, float widthPx, float heightPx)
        {
            XDocument doc;
            using (var input = new MemoryStream(svg))
                doc = XDocument.Load(input, LoadOptions.PreserveWhitespace);
            XElement? root = doc.Root;
            if (root == null)
                return svg;

            string w = widthPx.ToString("0.###", CultureInfo.InvariantCulture);
            string h = heightPx.ToString("0.###", CultureInfo.InvariantCulture);
            root.SetAttributeValue("width", w);
            root.SetAttributeValue("height", h);
            root.SetAttributeValue("viewBox", "0 0 " + w + " " + h);

            foreach (XElement text in root.Descendants(root.Name.Namespace + "text"))
            {
                foreach (string name in new[] { "x", "y" })
                {
                    XAttribute? a = text.Attribute(name);
                    if (a != null)
                        a.Value = a.Value.TrimEnd(' ', ',');
                }
                if (text.HasElements)
                    continue;
                text.Value = TrimIndent(text.Value);
                text.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            }

            using var output = new MemoryStream();
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false) };
            using (var xw = XmlWriter.Create(output, settings))
                doc.Save(xw);
            return output.ToArray();
        }

        // Skia's indentation around a text's characters: a newline then tabs, before and after.
        private static string TrimIndent(string s)
        {
            int start = 0, end = s.Length;
            if (start < end && s[start] == '\n')
            {
                start++;
                while (start < end && s[start] == '\t')
                    start++;
            }
            int e = end;
            while (e > start && s[e - 1] == '\t')
                e--;
            if (e > start && s[e - 1] == '\n')
                end = e - 1;
            return s.Substring(start, end - start);
        }

        // Body: main container for the report
        public void BodyStart(Body b)
        {
        }

        public void BodyEnd(Body b)
        {
        }

        public void PageHeaderStart(PageHeader ph)
        {
        }

        public void PageHeaderEnd(PageHeader ph)
        {
        }

        public void PageFooterStart(PageFooter pf)
        {
        }

        public void PageFooterEnd(PageFooter pf)
        {
        }

        public Task Textbox(Textbox tb, string t, Row row)
        {
            return Task.CompletedTask;
        }

        public Task DataRegionNoRows(DataRegion d, string noRowsMsg)
        {
            return Task.CompletedTask;
        }

        // Lists
        public Task<bool> ListStart(List l, Row r)
        {
            return Task.FromResult(true);
        }

        public Task ListEnd(List l, Row r)
        {
            return Task.CompletedTask;
        }

        public Task ListEntryBegin(List l, Row r)
        {
            return Task.CompletedTask;
        }

        public void ListEntryEnd(List l, Row r)
        {
        }

        // Tables               // Report item table
        public Task<bool> TableStart(Table t, Row row)
        {
            return Task.FromResult(true);
        }

        public Task TableEnd(Table t, Row row)
        {
            return Task.CompletedTask;
        }

        public void TableBodyStart(Table t, Row row)
        {
        }

        public void TableBodyEnd(Table t, Row row)
        {
        }

        public void TableFooterStart(Footer f, Row row)
        {
        }

        public void TableFooterEnd(Footer f, Row row)
        {
        }

        public void TableHeaderStart(Header h, Row row)
        {
        }

        public void TableHeaderEnd(Header h, Row row)
        {
        }

        public Task TableRowStart(TableRow tr, Row row)
        {
            return Task.CompletedTask;
        }

        public void TableRowEnd(TableRow tr, Row row)
        {
        }

        public Task TableCellStart(TableCell t, Row row)
        {
            return Task.CompletedTask;
        }

        public void TableCellEnd(TableCell t, Row row)
        {
            return;
        }

        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)            // called first
        {
            return Task.FromResult(true);
        }

        public void MatrixColumns(Matrix m, MatrixColumns mc)   // called just after MatrixStart
        {
        }

        public Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
        {
            return Task.CompletedTask;
        }

        public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
        {
            return Task.CompletedTask;
        }

        public void MatrixRowStart(Matrix m, int row, Row r)
        {
        }

        public void MatrixRowEnd(Matrix m, int row, Row r)
        {
        }

        public Task MatrixEnd(Matrix m, Row r)            // called last
        {
            return Task.CompletedTask;
        }

        public Task Chart(Chart c, Row r, ChartBase cb)
        {
            return Task.CompletedTask;
        }

        public Task Image(Rdl.Image i, Row r, string mimeType, Stream ior)
        {
            return Task.CompletedTask;
        }

        public Task Line(Line l, Row r)
        {
            return Task.CompletedTask;
        }

        public Task<bool> RectangleStart(Rdl.Rectangle rect, Row r)
        {
            return Task.FromResult(true);
        }

        public Task RectangleEnd(Rdl.Rectangle rect, Row r)
        {
            return Task.CompletedTask;
        }

        public Task Subreport(Subreport s, Row r)
        {
            return Task.CompletedTask;
        }

        public void GroupingStart(Grouping g)         // called at start of grouping
        {
        }
        public void GroupingInstanceStart(Grouping g)   // called at start for each grouping instance
        {
        }
        public void GroupingInstanceEnd(Grouping g)   // called at start for each grouping instance
        {
        }
        public void GroupingEnd(Grouping g)         // called at end of grouping
        {
        }
    }
}
