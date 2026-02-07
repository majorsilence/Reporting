/*
 *
 Copyright (C) 2004-2008  fyiReporting Software, LLC
 Copyright (C) 2011  Peter Gill <peter@majorsilence.com>
 Copyright (c) 2010 devFU Pty Ltd, Josh Wilson and Others (http://reportfu.org)



 This file has been modified with suggestiong from forum users.
 *Obtained from Forum, User: sinnovasoft http://www.fyireporting.com/forum/viewtopic.php?t=1049

  Refactored by Daniel Romanowski http://dotlink.pl

  This file is part of the fyiReporting RDL project.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using System.Collections.Generic;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
using Imaging = Majorsilence.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using Imaging = System.Drawing.Imaging;
#endif
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Majorsilence.Reporting.Rdl.Utility;
using System.Security;
using System.Linq;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Renders a report to PDF.   This is a page oriented formatting renderer.
    ///</summary>
    [SecuritySafeCritical]
    internal class RenderPdf_iTextSharp : RenderBase
    {
        #region private

        Document _pdfDocument;
        PdfContentByte _contentByte;
        MemoryStream _ms;

        int _osPlatform = (int)Environment.OSVersion.Platform;
        int _osVersion = (int)Environment.OSVersion.Version.Major;

        bool _dejavuFonts = false;

        /// <summary>
        /// List itextSharp Basefont added
        /// </summary>
        private List<BaseFont> _baseFonts = new List<BaseFont>();

        /// <summary>
        /// List font name
        /// </summary>
        private List<string> _baseFontsName = new List<string>();

        #endregion

        static RenderPdf_iTextSharp()
        {
            iTextSharp.text.FontFactory.RegisterDirectories();
        }

        #region properties

        private bool IsOSX =>
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX);

        /// <summary> 
        /// Default I get embedded fonts in Fonts folder in current 
        /// folder RdlEngine.dll in, can set font folder here 
        /// </summary> 
        private string FontFolder
        {
            get
            {
                //Kind of MacOSX
                if (IsOSX)
                {
                    return "/System/Library/Fonts/Supplemental";
                }

                if (_osPlatform == (int)PlatformID.Unix)
                {
                    if (System.IO.Directory.Exists("/usr/share/fonts/truetype/msttcorefonts"))
                    {
                        return "/usr/share/fonts/truetype/msttcorefonts";
                    }
                    else if (System.IO.Directory.Exists("/usr/share/fonts/truetype/dejavu"))
                    {
                        _dejavuFonts = true;
                        return "/usr/share/fonts/truetype/dejavu";
                    }
                    else
                    {
                        _dejavuFonts = true;
                        return Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                    }
                }

                // get parent of System folder to have Windows folder
                DirectoryInfo dirWindowsFolder =
                    Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.System));
                // Concatenate Fonts folder onto Windows folder.
                return Path.Combine(dirWindowsFolder.FullName, "Fonts");
                // Results in full path e.g. "C:\Windows\Fonts" 
            }
        }

        #endregion

        #region ctor

        public RenderPdf_iTextSharp(Report report, IStreamGen sg) : base(report, sg)
        {
            _pdfDocument = new Document();
            _ms = new MemoryStream();
        }

        #endregion

        #region implementations

        protected internal override void CreateDocument()
        {
            Report r = base.Report();
            PdfWriter writer = PdfWriter.GetInstance(_pdfDocument, _ms);
            _pdfDocument.Open();
            _contentByte = writer.DirectContent;
            _pdfDocument.AddAuthor(r.Author);
            _pdfDocument.AddCreationDate();
            _pdfDocument.AddCreator("Majorsilence Reporting - RenderPdf_iTextSharp");
            _pdfDocument.AddSubject(r.Description);
            _pdfDocument.AddTitle(r.Name);
        }

        protected internal override void EndDocument(Stream sg)
        {
            _pdfDocument.Close();
            //write out ItextSharp pdf stream to RDL stream
            byte[] contentbyte = _ms.ToArray();
            sg.Write(contentbyte, 0, contentbyte.Length);
            _ms.Dispose();
            _baseFonts.Clear();
            _baseFontsName.Clear();
        }

        protected internal override void CreatePage()
        {
            _pdfDocument.SetPageSize(new iTextSharp.text.Rectangle(PageSize.xWidth, PageSize.yHeight));
            _pdfDocument.NewPage();
        }

        protected internal override void AfterProcessPage()
        {
        }

        protected internal override void AddBookmark(PageText pt)
        {
        }

        protected internal override void AddLine(float x, float y, float x2, float y2, float width, Draw2.Color c,
            BorderStyleEnum ls)
        {
            // Get the line color			
            _contentByte.SetRgbColorStroke(c.R, c.G, c.B);
            _contentByte.SetLineWidth(width);
            // Get the line style Dotted - Dashed - Solid

            switch (ls)
            {
                case BorderStyleEnum.Dashed:
                    _contentByte.SetLineDash(new float[] { width * 3, width }, 0);
                    break;
                case BorderStyleEnum.Dotted:
                    _contentByte.SetLineDash(new float[] { width }, 0);
                    break;
                case BorderStyleEnum.Solid:
                default:
                    _contentByte.SetLineDash(new float[] { }, 0);
                    break;
            }

            _contentByte.MoveTo(x, PageSize.yHeight - y);
            _contentByte.LineTo(x2, PageSize.yHeight - y2);
            _contentByte.Stroke();
        }

        protected internal override void AddImage(string name, StyleInfo si, Imaging.ImageFormat imf, float x, float y,
            float width, float height, Draw2.RectangleF clipRect, byte[] im, int samplesW, int samplesH, string url,
            string tooltip)
        {
            iTextSharp.text.Image pdfImg = iTextSharp.text.Image.GetInstance(im);
            pdfImg.ScaleAbsolute(width, height); //zoom		  
            pdfImg.SetAbsolutePosition(x, PageSize.yHeight - y - height); //Set position
            _pdfDocument.Add(pdfImg);
            //add url
            if (url != null)
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y - PageSize.topMargin, width + x, height, url));
            //add tooltip
            if (!string.IsNullOrEmpty(tooltip))
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y - PageSize.topMargin, width + x, height,
                    tooltip));
            iAddBorder(si, x - si.PaddingLeft, y - si.PaddingTop,
                height + si.PaddingTop + si.PaddingBottom,
                width + si.PaddingLeft + si.PaddingRight); // add any required border
        }

        protected internal override void AddPolygon(Draw2.PointF[] pts, StyleInfo si, string url)
        {
            if (si.BackgroundColor.IsEmpty)
                return; // nothing to do

            // Get the fill color - could be a gradient or pattern etc...
            Draw2.Color c = si.BackgroundColor;
            iAddPoints(pts);
            _contentByte.SetRgbColorFill(c.R, c.G, c.B);
            _contentByte.ClosePathFillStroke();
        }

        protected internal override void AddRectangle(float x, float y, float height, float width, StyleInfo si,
            string url, string tooltip)
        {
            // Draw background rectangle if needed
            if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0)
            {
                // background color, height and width are specified
                iAddFillRect(x, y, width, height, si);
            }

            iAddBorder(si, x, y, height, width); // add any required border

            if (url != null)
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y, width + x, height, url));
            if (!string.IsNullOrEmpty(tooltip))
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y, width + x, height, tooltip));

            return;
        }


        protected internal override void AddPie(float x, float y, float height, float width, StyleInfo si, string url,
            string tooltip)
        {
            // Draw background rectangle if needed
            if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0)
            {
                // background color, height and width are specified
                iAddFillRect(x, y, width, height, si);
            }

            iAddBorder(si, x, y, height, width); // add any required border

            //add url
            if (url != null)
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y, width + x, height, url));
            //add tooltip
            if (!string.IsNullOrEmpty(tooltip))
                _pdfDocument.Add(new Annotation(x, PageSize.yHeight - y, width + x, height, tooltip));
            return;
        }

        protected internal override void AddCurve(Draw2.PointF[] pts, StyleInfo si)
        {
            if (pts.Length > 2)
            {
                // do a spline curve
                Draw2.PointF[] tangents = iGetCurveTangents(pts);
                iDoCurve(pts, tangents, si);
            }
            else
            {
                // we only have two points; just do a line segment
                AddLine(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y, si);
            }
        }

        protected internal override void AddEllipse(float x, float y, float height, float width, StyleInfo si,
            string url)
        {
            if (si.BStyleTop != BorderStyleEnum.None)
            {
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        _contentByte.SetLineDash(new float[] { '3', '2' }, 0);
                        break;
                    case BorderStyleEnum.Dotted:
                        _contentByte.SetLineDash(new float[] { '2' }, 0);
                        break;
                    case BorderStyleEnum.Solid:
                    default:
                        _contentByte.SetLineDash(new float[] { }, 0);
                        break;
                }

                _contentByte.SetRgbColorStroke(si.BColorTop.R, si.BColorTop.G, si.BColorTop.B);
            }

            float RadiusX = (width / 2.0f);
            float RadiusY = (height / 2.0f);
            _contentByte.Ellipse(x, PageSize.yHeight - y, x + RadiusX, y + RadiusY);
            if (!si.BackgroundColor.IsEmpty)
            {
                _contentByte.SetRgbColorStrokeF(si.BackgroundColor.R, si.BackgroundColor.G, si.BackgroundColor.B);
            }

            if (si.BackgroundColor.IsEmpty)
                _contentByte.ClosePathStroke();
            else
                _contentByte.ClosePathFillStroke();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Font name , for my application almost fonts  will be unicode and embedded
        /// </summary>
        /// <returns></returns>
        private string iFontNameNormalize(string face)
        {
            string faceName = face;
            switch (face.ToLower())
            {
                case "times":
                case "times-roman":
                case "times roman":
                case "timesnewroman":
                case "times new roman":
                case "timesnewromanps":
                case "timesnewromanpsmt":
                case "serif":
                    faceName = "Times-Roman";
                    break;
                case "helvetica":
                case "arial":
                case "arialmt":
                case "sans-serif":
                case "sans serif":
                default:
                    //faceName = "Arial";
                    break;
                case "courier":
                case "couriernew":
                case "courier new":
                case "couriernewpsmt":
                case "monospace":
                    faceName = "Courier New";
                    break;
                case "symbol":
                    faceName = "Symbol";
                    break;
                case "zapfdingbats":
                case "wingdings":
                case "wingding":
                    faceName = "ZapfDingbats";
                    break;
            }

            return faceName;
        }

        public bool IsAsian(string[] text)
        {
            bool asian = false;
            for (var i = 0; i < text.Length; ++i)
            {
                asian |= text[i].Any(c => (c >= 0x3040 && c <= 0x309f) || //Hiragana
                                          (c >= 0x30a0 && c <= 0x30ff) || //Katanka
                                          (c >= 0xE00 && c <= 0xE7F) || //Thai
                                          c >= 0x4e00);
                if (asian)
                {
                    break;
                }
            }

            return asian;
        }

        protected internal override void AddText(float x, float y, float height, float width, string[] sa, StyleInfo si,
            float[] tw, bool bWrap, string url, bool bNoClip, string tooltip)
        {
            BaseFont bf = null;
            string face = iFontNameNormalize(si.FontFamily);
            string fontname = "";
            bool fonttype1 = true;
            var folder = FontFolder; //Call to determine folder and set value of _dejavuFonts;
            if (face == "Times-Roman")
            {
                if (si.IsFontBold() && si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "TimesNewRomanPS-BoldItalicMT";
                        fontname = "Times New Roman Bold Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Serif Condensed Bold Italic" : "Times-BoldItalic";
                        fontname = (_dejavuFonts ? "DejaVuSerifCondensed-BoldItalic.ttf" : "timesbi.ttf");
                    }
                }
                else if (si.IsFontBold())
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "TimesNewRomanPS-BoldMT";
                        fontname = "Times New Roman Bold.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Serif Condensed Bold" : "Times-Bold";
                        fontname = (_dejavuFonts ? "DejaVuSerifCondensed-Bold.ttf" : "timesbd.ttf");
                    }
                }
                else if (si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "TimesNewRomanPS-ItalicMT";
                        fontname = "Times New Roman Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Serif Condensed Italic" : "Times-Italic";
                        fontname = (_dejavuFonts ? "DejaVuSerifCondensed-Italic.ttf" : "timesi.ttf");
                    }
                }
                else
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "TimesNewRomanPSMT";
                        fontname = "Times New Roman.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Serif Condensed" : face;
                        fontname = (_dejavuFonts ? "DejaVuSerifCondensed.ttf" : "times.ttf");
                    }
                }

                fonttype1 = false;
            }
            else if (face == "Arial")
            {
                if (IsAsian(sa))
                {
                    face = "Arial Unicode MS";
                    fontname = "arialuni.ttf";
                }
                else if (si.IsFontBold() && si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "Arial BoldItalicMT";
                        fontname = "Arial Bold Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Condensed Bold Oblique" : "Arial-BoldItalic";
                        fontname = (_dejavuFonts ? "DejaVuSansCondensed-BoldOblique.ttf" : "arialbi.ttf");
                    }
                }
                else if (si.IsFontBold())
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "Arial-BoldMT";
                        fontname = "Arial Bold.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Condensed Bold" : "Arial-Bold";
                        fontname = (_dejavuFonts ? "DejaVuSansCondensed-Bold.ttf" : "arialbd.ttf");
                    }
                }
                else if (si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "Arial-ItalicMT";
                        fontname = "Arial Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Condensed Oblique" : "Arial-Italic";
                        fontname = (_dejavuFonts ? "DejaVuSansCondensed-Oblique.ttf" : "ariali.ttf");
                    }
                }
                else
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "ArialMT";
                        fontname = "Arial.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Condensed" : face;
                        fontname = (_dejavuFonts ? "DejaVuSansCondensed.ttf" : "arial.ttf");
                    }
                }

                fonttype1 = false;
            }
            else if (face == "Courier New")
            {
                if (si.IsFontBold() && si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "CourierNewPS-BoldItalicMT";
                        fontname = "Courier New Bold Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Mono Bold Oblique" : "Courier New-BoldItalic";
                        fontname = (_dejavuFonts ? "DejaVuSansMono-BoldOblique.ttf" : "courbi.ttf");
                    }
                }
                else if (si.IsFontBold())
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "CourierNewPS-BoldMT";
                        fontname = "Courier New Bold.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Mono Bold" : "Courier New-Bold";
                        fontname = (_dejavuFonts ? "DejaVuSansMono-Oblique.ttf" : "courbd.ttf");
                    }
                }
                else if (si.FontStyle == FontStyleEnum.Italic)
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "CourierNewPS-ItalicMT";
                        fontname = "Courier New Italic.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Mono Oblique" : "Courier New-Italic";
                        fontname = (_dejavuFonts ? "DejaVuSansMono-Oblique.ttf" : "couri.ttf");
                    }
                }
                else
                {
                    //OSX
                    if (IsOSX)
                    {
                        face = "CourierNewPSMT";
                        fontname = "Courier New.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Mono" : face;
                        fontname = (_dejavuFonts ? "DejaVuSansMono.ttf" : "cour.ttf");
                    }
                }

                fonttype1 = false;
            }
            else
            {
                int style = si.IsFontBold() ? iTextSharp.text.Font.BOLD : 0;
                style += si.FontStyle == FontStyleEnum.Italic ? iTextSharp.text.Font.ITALIC : 0;
                iTextSharp.text.Font ff = FontFactory.GetFont(face, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, 10f, style);
                bf = ff.BaseFont;
                if (bf == null)
                {
                    if (IsOSX)
                    {
                        face = "ArialMT";
                        fontname = "Arial.ttf";
                    }
                    else
                    {
                        face = _dejavuFonts ? "DejaVu Sans Condensed" : "Arial";
                        fontname = (_dejavuFonts ? "DejaVuSansCondensed.ttf" : "arial.ttf");
                    }
                }

                /*                if (si.IsFontBold() &&
                            si.FontStyle == FontStyleEnum.Italic)   // bold and italic?
                                    face = face + "-BoldOblique";
                                else if (si.IsFontBold())           // just bold?
                                    face = face + "-Bold";
                                else if (si.FontStyle == FontStyleEnum.Italic)
                                    face = face + "-Oblique";*/
                fonttype1 = false;
            }

            if (bf == null)
            {
                //Get index of fontname in List font name
                int indexbf = _baseFontsName.FindIndex(delegate(string _fontname) { return _fontname == face; });
                //If not found then add new BaseFont
                if (indexbf == -1)
                {
                    _baseFontsName.Add(face);
                    if (fonttype1)
                    {
                        bf = BaseFont.CreateFont(face, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                    }
                    else
                    {
                        string path = System.IO.Path.Combine(folder, fontname);
                        bf = BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    }

                    _baseFonts.Add(bf);
                }
                else
                    //Get from List
                {
                    bf = _baseFonts[indexbf];
                }
            }

            // Loop thru the lines of text
            for (int i = 0; i < sa.Length; i++)
            {
                string text = sa[i];
                float textwidth = bf.GetWidthPoint(text, si.FontSize);
                // Calculate the x positino
                float startX = x + si.PaddingLeft; // TODO: handle tb_rl
                float startY = y + si.PaddingTop + (i * si.FontSize); // TODO: handle tb_rl
                int align = 0;
                if (si.WritingMode == WritingModeEnum.lr_tb)
                {
                    // TODO: not sure what alignment means with tb_lr so I'll leave it out for now
                    switch (si.TextAlign)
                    {
                        case TextAlignEnum.Center:
                            if (width > 0)
                            {
                                startX = x + si.PaddingLeft + ((width - si.PaddingLeft - si.PaddingRight) / 2) -
                                         (textwidth / 2);
                                align = Element.ALIGN_CENTER;
                            }

                            break;
                        case TextAlignEnum.Right:
                            if (width > 0)
                            {
                                startX = x + width - textwidth - si.PaddingRight;
                                align = Element.ALIGN_RIGHT;
                            }

                            break;
                        case TextAlignEnum.Left:
                        default:
                            align = Element.ALIGN_LEFT;
                            break;
                    }

                    // Calculate the y position
                    switch (si.VerticalAlign)
                    {
                        case VerticalAlignEnum.Middle:
                            if (height <= 0)
                                break;

                            // calculate the middle of the region
                            startY = y + si.PaddingTop + ((height - si.PaddingTop - si.PaddingBottom) / 2) -
                                     (si.FontSize / 2);
                            // now go up or down depending on which line
                            if (sa.Length == 1)
                                break;
                            if (sa.Length % 2 == 0) // even number
                            {
                                startY = startY - (((sa.Length / 2) - i) * si.FontSize) + (si.FontSize / 2);
                            }
                            else
                            {
                                startY = startY - (((sa.Length / 2) - i) * si.FontSize);
                            }

                            break;
                        case VerticalAlignEnum.Bottom:
                            if (height <= 0)
                                break;

                            startY = y + height - si.PaddingBottom - (si.FontSize * (sa.Length - i));
                            break;
                        case VerticalAlignEnum.Top:
                        default:
                            break;
                    }
                }
                else
                {
                    //25072008 GJL - Move x in a little - it draws to close to the edge of the rectangle (25% of the font size seems to work!) and Center or right align vertical text
                    startX += si.FontSize / 4;

                    switch (si.TextAlign)
                    {
                        case TextAlignEnum.Center:
                            if (height > 0)
                                startY = y + si.PaddingLeft + ((height - si.PaddingLeft - si.PaddingRight) / 2) -
                                         (textwidth / 2);
                            break;
                        case TextAlignEnum.Right:
                            if (width > 0)
                                startY = y + height - textwidth - si.PaddingRight;
                            break;
                        case TextAlignEnum.Left:
                        default:
                            break;
                    }
                }

                // Draw background rectangle if needed (only put out on the first line, since we do whole rectangle)
                if (!si.BackgroundColor.IsEmpty && height > 0 && width > 0 && i == 0)
                {
                    // background color, height and width are specified
                    iAddFillRect(x, y, width, height, si.BackgroundColor);
                }

                // Set the clipping path, (Itext have no clip)
                if (height > 0 && width > 0)
                {
                    _contentByte.SetRgbColorFill(si.Color.R, si.Color.G, si.Color.B);

                    if (si.WritingMode == WritingModeEnum.lr_tb)
                    {
                        //If textline after measure with word break can fit just simple show Text
                        if (width >= textwidth)
                        {
                            _contentByte.BeginText();
                            _contentByte.SetFontAndSize(bf, si.FontSize);
                            _contentByte.SetTextMatrix(startX, (PageSize.yHeight - startY - si.FontSize));
                            _contentByte.ShowText(text);
                            _contentByte.EndText();
                        }
                        else
                        {
                            //else use Column text to wrap or clip (wrap: for example a text like an URL so word break is not working here, itextsharp ColumnText do the work for us)
                            ColumnText ct = new ColumnText(_contentByte);
                            Phrase myPhrase = new Phrase(text, new iTextSharp.text.Font(bf, si.FontSize));
                            ct.SetSimpleColumn(myPhrase, x + si.PaddingLeft, PageSize.yHeight - startY,
                                x + width - si.PaddingRight, PageSize.yHeight - y - si.PaddingBottom - height, 10f,
                                align);
                            ct.Go();
                        }
                    }
                    else
                    {
                        // Handle rotated text
                        double angleRadians;
                        switch (si.WritingMode)
                        {
                            case WritingModeEnum.tb_rl:
                                angleRadians = -Math.PI / 2; // -90 degrees
                                break;
                            case WritingModeEnum.rl_bt:
                                angleRadians = Math.PI; // 180 degrees
                                break;
                            case WritingModeEnum.tb_lr:
                                angleRadians = Math.PI / 2; // 90 degrees
                                break;
                            default:
                                angleRadians = 0;
                                break;
                        }
                        double radsCos = Math.Cos(angleRadians);
                        double radsSin = Math.Sin(angleRadians);
                        _contentByte.BeginText();
                        _contentByte.SetFontAndSize(bf, si.FontSize);
                        _contentByte.SetTextMatrix((float)radsCos, (float)radsSin, (float)-radsSin, (float)radsCos,
                            startX, PageSize.yHeight - startY);
                        _contentByte.ShowText(text);
                        _contentByte.EndText();
                    }

                    //add URL
                    if (url != null)
                        _pdfDocument.Add(new Annotation(x, PageSize.yHeight - (y + height), width + x,
                            PageSize.yHeight - y, url));
                    //add tooltip
                    if (tooltip != null)
                        _pdfDocument.Add(new Annotation(x, PageSize.yHeight - (y + height), width + x,
                            PageSize.yHeight - y, tooltip));
                }

                // Handle underlining etc.
                float maxX;
                switch (si.TextDecoration)
                {
                    case TextDecorationEnum.Underline:
                        maxX = width > 0 ? Math.Min(x + width, startX + textwidth) : startX + textwidth;
                        AddLine(startX, startY + si.FontSize + 1, maxX, startY + si.FontSize + 1, 1, si.Color,
                            BorderStyleEnum.Solid);
                        break;
                    case TextDecorationEnum.LineThrough:
                        maxX = width > 0 ? Math.Min(x + width, startX + textwidth) : startX + textwidth;
                        AddLine(startX, startY + (si.FontSize / 2) + 1, maxX, startY + (si.FontSize / 2) + 1, 1,
                            si.Color, BorderStyleEnum.Solid);
                        break;
                    case TextDecorationEnum.Overline:
                        maxX = width > 0 ? Math.Min(x + width, startX + textwidth) : startX + textwidth;
                        AddLine(startX, startY + 1, maxX, startY + 1, 1, si.Color, BorderStyleEnum.Solid);
                        break;
                    case TextDecorationEnum.None:
                    default:
                        break;
                }
            }

            iAddBorder(si, x, y, height, width); // add any required border

            return;
        }

        /// <summary>
        /// Add a filled rectangle
        /// </summary>
        /// <returns></returns>
        private void iAddFillRect(float x, float y, float width, float height, Draw2.Color c)
        {
            // Get the fill color
            _contentByte.SetRgbColorFill(c.R, c.G, c.B);
            _contentByte.Rectangle(x, PageSize.yHeight - y - height, width, height);
            _contentByte.Fill();
        }

        /// <summary>
        /// Add border
        /// </summary>
        private void iAddBorder(StyleInfo si, float x, float y, float height, float width)
        {
            // Handle any border required   TODO: optimize border by drawing a rect when possible
            if (height <= 0 || width <= 0) // no bounding box to use
                return;

            float ybottom = (y + height);
            float xright = x + width;
            if (si.BStyleTop != BorderStyleEnum.None && si.BWidthTop > 0)
                AddLine(x, y, xright, y, si.BWidthTop, si.BColorTop, si.BStyleTop);

            if (si.BStyleRight != BorderStyleEnum.None && si.BWidthRight > 0)
                AddLine(xright, y, xright, ybottom, si.BWidthRight, si.BColorRight, si.BStyleRight);

            if (si.BStyleLeft != BorderStyleEnum.None && si.BWidthLeft > 0)
                AddLine(x, y, x, ybottom, si.BWidthLeft, si.BColorLeft, si.BStyleLeft);

            if (si.BStyleBottom != BorderStyleEnum.None && si.BWidthBottom > 0)
                AddLine(x, ybottom, xright, ybottom, si.BWidthBottom, si.BColorBottom, si.BStyleBottom);

            return;
        }

        private void iAddPoints(Draw2.PointF[] pts)
        {
            if (pts.Length > 0)
            {
                _contentByte.MoveTo(pts[0].X, PageSize.yHeight - pts[0].Y);
                for (int pi = 1; pi < pts.Length; pi++)
                {
                    _contentByte.LineTo(pts[pi].X, PageSize.yHeight - pts[pi].Y);
                }
            }

            return;
        }

        private void iAddFillRect(float x, float y, float width, float height, StyleInfo si)
        {
            Draw2.Color c;
            // Get the fill color - could be a gradient or pattern etc...
            c = si.BackgroundColor;
            _contentByte.SetRgbColorFill(c.R, c.G, c.B);
            _contentByte.Rectangle(x, PageSize.yHeight - y - height, width, height);
            //_contentByte.ClosePathFillStroke();
            _contentByte.Fill();
        }

        //25072008 GJL Draw a bezier curve
        private void iAddCurve(float X1, float Y1, float X2, float Y2, float X3, float Y3, float X4, float Y4,
            StyleInfo si, string url)
        {
            if (si.BStyleTop != BorderStyleEnum.None)
            {
                switch (si.BStyleTop)
                {
                    case BorderStyleEnum.Dashed:
                        _contentByte.SetLineDash(new float[] { '3', '2' }, 0);
                        break;
                    case BorderStyleEnum.Dotted:
                        _contentByte.SetLineDash(new float[] { '2' }, 0);
                        break;
                    case BorderStyleEnum.Solid:
                    default:
                        _contentByte.SetLineDash(new float[] { }, 0);
                        break;
                }

                _contentByte.SetRgbColorStroke(si.BColorTop.R, si.BColorTop.G, si.BColorTop.B);
            }

            if (!si.BackgroundColor.IsEmpty)
            {
                _contentByte.SetRgbColorStrokeF(si.BackgroundColor.R, si.BackgroundColor.G, si.BackgroundColor.B);
            }

            _contentByte.CurveTo(X1, PageSize.yHeight - Y1, X2, PageSize.yHeight - Y1, X3, PageSize.yHeight - Y3);
            if (si.BackgroundColor.IsEmpty)
                _contentByte.ClosePathStroke();
            else
                _contentByte.ClosePathFillStroke();
        }

        private void iDoCurve(Draw2.PointF[] points, Draw2.PointF[] tangents, StyleInfo si)
        {
            int i;

            for (i = 0; i < points.Length - 1; i++)
            {
                int j = i + 1;

                float x0 = points[i].X;
                float y0 = points[i].Y;

                float x1 = points[i].X + tangents[i].X;
                float y1 = points[i].Y + tangents[i].Y;

                float x2 = points[j].X - tangents[j].X;
                float y2 = points[j].Y - tangents[j].Y;

                float x3 = points[j].X;
                float y3 = points[j].Y;
                iAddCurve(x0, y0, x1, y1, x2, y2, x3, y3, si, null);
            }
        }

        private Draw2.PointF[] iGetCurveTangents(Draw2.PointF[] points)
        {
            float tension = .5f; // This  is the tension used on the DrawCurve GDI call.
            float coefficient = tension / 3.0f;
            int i;

            Draw2.PointF[] tangents = new Draw2.PointF[points.Length];

            // initialize everything to zero to begin with
            for (i = 0; i < tangents.Length; i++)
            {
                tangents[i].X = 0;
                tangents[i].Y = 0;
            }

            if (tangents.Length <= 2)
                return tangents;
            int count = tangents.Length;
            for (i = 0; i < count; i++)
            {
                int r = i + 1;
                int s = i - 1;

                if (r >= points.Length)
                    r = points.Length - 1;
                if (s < 0)
                    s = 0;

                tangents[i].X += (coefficient * (points[r].X - points[s].X));
                tangents[i].Y += (coefficient * (points[r].Y - points[s].Y));
            }

            return tangents;
        }

        #endregion
    }
}