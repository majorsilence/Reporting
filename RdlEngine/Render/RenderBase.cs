
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
using Drawing2D = Majorsilence.Forms.Drawing.Imaging;
using Majorsilence.Forms.Drawing.Imaging;
#else
using Draw2 = System.Drawing;
using Drawing2D = System.Drawing.Imaging;
#endif
using System.Text;
using Majorsilence.Reporting.Rdl.Utility;
using System.Security;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{


    ///<summary>
    /// Renders a report to PDF.   This is a page oriented formatting renderer.
    ///</summary>
    internal abstract class RenderBase : IPresent
    {

        #region private
        Stream _streamGen;                  // where the output is going
        Report _report;                 // report
        static readonly char[] LINEBREAK = new char[] { '\n' };
        static readonly char[] WORDBREAK = new char[] { ' ' };
        //		static readonly int MEASUREMAX = int.MaxValue;  //  .Net 2 doesn't seem to have a limit; 1.1 limit was 32
        static readonly int MEASUREMAX = 32;  //  guess I'm wrong -- .Net 2 doesn't seem to have a limit; 1.1 limit was 32

        #endregion


        #region properties
        private PdfPageSize _pageSize;

        internal protected PdfPageSize PageSize
        {
            get { return _pageSize; }
            private set { _pageSize = value; }
        }
        #endregion


        #region abstract methods
        internal protected void AddLine(float x, float y, float x2, float y2, StyleInfo si)
        {
            AddLine(x, y, x2, y2, si.BWidthTop, si.BColorTop, si.BStyleTop);
        }
        /// <summary>
        /// Page line element at the X Y to X2 Y2 position
        /// </summary>
        /// <returns></returns>
        internal abstract protected void CreateDocument();
        internal abstract protected void EndDocument(Stream sg);
        internal abstract protected void CreatePage();
        internal abstract protected void AfterProcessPage();
        internal abstract protected void AddBookmark(PageText pt);

        internal abstract protected void AddLine(float x, float y, float x2, float y2, float width, System.Drawing.Color c, BorderStyleEnum ls);


        /// <summary>
        /// Add image to the page.
        /// </summary>
        /// <returns>string Image name</returns>
        internal abstract protected void AddImage(string name, StyleInfo si,
            Drawing2D.ImageFormat imf, float x, float y, float width, float height, System.Drawing.RectangleF clipRect,
            byte[] im, int samplesW, int samplesH, string url, string tooltip);

        /// <summary>
        /// Page Polygon
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="si"></param>
        /// <param name="url"></param>
        /// <param name="patterns"></param>
        internal abstract protected void AddPolygon(System.Drawing.PointF[] pts, StyleInfo si, string url);


        /// <summary>
        /// Page Rectangle element at the X Y position
        /// </summary>
        /// <returns></returns>
        internal abstract protected void AddRectangle(float x, float y, float height, float width, StyleInfo si, string url, string tooltip);
        /// <summary>
        /// Draw a pie
        /// </summary>
        /// <returns></returns>
        internal abstract protected void AddPie(float x, float y, float height, float width, StyleInfo si, string url, string tooltip);

        /// <summary>
        /// Draw a curve
        /// </summary>
        /// <returns></returns>
        internal abstract protected void AddCurve(System.Drawing.PointF[] pts, StyleInfo si);



        //25072008 GJL Draw 4 bezier curves to approximate a circle
        internal abstract protected void AddEllipse(float x, float y, float height, float width, StyleInfo si, string url);



        /// <summary>
        /// Page Text element at the X Y position; multiple lines handled
        /// </summary>
        /// <returns></returns>
        internal abstract protected void AddText(float x, float y, float height, float width, string[] sa,
            StyleInfo si, float[] tw, bool bWrap, string url, bool bNoClip, string tooltip);

        #endregion

        public void Dispose() { }


        public RenderBase(Report rep, IStreamGen sg)
        {
            _streamGen = sg.GetStream();
            _report = rep;
        }

        public Report Report()
        {
            return _report;
        }

        public bool IsPagingNeeded()
        {
            return true;
        }

        public void Start()
        {
            CreateDocument();
        }

        public Task End()
        {
            EndDocument(_streamGen);
            return Task.CompletedTask;
        }

        public async Task RunPages(Pages pgs)	// this does all the work
        {
            foreach (Page p in pgs)
            {
                PageSize = new PdfPageSize((int)_report.ReportDefinition.PageWidth.ToPoints(),
                                       (int)_report.ReportDefinition.PageHeight.ToPoints());

                //Create a Page 
                CreatePage();
                await ProcessPage(pgs, p);
                // after a page
                AfterProcessPage();
            }
            return;
        }
        // render all the objects in a page in PDF
        private async Task ProcessPage(Pages pgs, IEnumerable items)
        {
            foreach (PageItem pi in items)
            {
                if (pi.SI.BackgroundImage != null)
                {	// put out any background image
                    PageImage bgImg = pi.SI.BackgroundImage;
                    //					elements.AddImage(images, i.Name, content.objectNum, i.SI, i.ImgFormat, 
                    //						pi.X, pi.Y, pi.W, pi.H, i.ImageData,i.SamplesW, i.SamplesH, null);				   
                    //Duc Phan modified 10 Dec, 2007 to support on background image 
                    float imW = Measurement.PointsFromPixels(bgImg.SamplesW, pgs.G.DpiX);
                    float imH = Measurement.PointsFromPixels(bgImg.SamplesH, pgs.G.DpiY);
                    int repeatX = 0;
                    int repeatY = 0;
                    float itemW = pi.W - (pi.SI.PaddingLeft + pi.SI.PaddingRight);
                    float itemH = pi.H - (pi.SI.PaddingTop + pi.SI.PaddingBottom);
                    switch (bgImg.Repeat)
                    {
                        case ImageRepeat.Repeat:
                            repeatX = (int)Math.Floor(itemW / imW);
                            repeatY = (int)Math.Floor(itemH / imH);
                            break;
                        case ImageRepeat.RepeatX:
                            repeatX = (int)Math.Floor(itemW / imW);
                            repeatY = 1;
                            break;
                        case ImageRepeat.RepeatY:
                            repeatY = (int)Math.Floor(itemH / imH);
                            repeatX = 1;
                            break;
                        case ImageRepeat.NoRepeat:
                        default:
                            repeatX = repeatY = 1;
                            break;
                    }

                    //make sure the image is drawn at least 1 times 
                    repeatX = Math.Max(repeatX, 1);
                    repeatY = Math.Max(repeatY, 1);

                    float currX = pi.X + pi.SI.PaddingLeft;
                    float currY = pi.Y + pi.SI.PaddingTop;
                    float startX = currX;
                    float startY = currY;
                    for (int i = 0; i < repeatX; i++)
                    {
                        for (int j = 0; j < repeatY; j++)
                        {
                            currX = startX + (i * imW);
                            currY = startY + (j * imH);



                            AddImage(bgImg.Name, bgImg.SI, bgImg.ImgFormat,
                                            currX, currY, imW, imH, System.Drawing.RectangleF.Empty, bgImg.GetImageData(), bgImg.SamplesW, bgImg.SamplesH, null, pi.Tooltip);

                        }
                    }
                }

                if (pi is PageTextHtml)
                {
                    PageTextHtml pth = pi as PageTextHtml;
                    await pth.Build(pgs.G);
                    await ProcessPage(pgs, pth);
                    continue;
                }

                if (pi is PageText)
                {
                    PageText pt = pi as PageText;
                    float[] textwidth;
                    string[] sa = MeasureString(pt, pgs.G, out textwidth);


                    AddText(pt.X, pt.Y, pt.H, pt.W, sa, pt.SI, textwidth, pt.CanGrow, pt.HyperLink, pt.NoClip, pt.Tooltip);

                    if (pt.Bookmark != null)
                    {
                        AddBookmark(pt);
                    }
                    continue;
                }

                if (pi is PageLine)
                {
                    PageLine pl = pi as PageLine;
                    AddLine(pl.X, pl.Y, pl.X2, pl.Y2, pl.SI);
                    continue;
                }

                if (pi is PageEllipse)
                {
                    PageEllipse pe = pi as PageEllipse;
                    AddEllipse(pe.X, pe.Y, pe.H, pe.W, pe.SI, pe.HyperLink);
                    continue;
                }



                if (pi is PageImage)
                {
                    PageImage i = pi as PageImage;

                    // Points, like every other coordinate handed to AddImage. The width
                    // and height used to be converted to pixels while the x and y beside
                    // them stayed in points, so an image came out DpiX/72 too large - a
                    // third over at the usual 96 dpi - anchored at the correct top-left
                    // corner. The padding subtracted from them is in points too, which is
                    // the other half of the same mismatch.
                    System.Drawing.RectangleF r2 = new System.Drawing.RectangleF(i.X + i.SI.PaddingLeft,
                        i.Y + i.SI.PaddingTop,
                        i.W - i.SI.PaddingLeft - i.SI.PaddingRight,
                        i.H - i.SI.PaddingTop - i.SI.PaddingBottom);

                    System.Drawing.RectangleF adjustedRect;   // work rectangle 
                    System.Drawing.RectangleF clipRect = System.Drawing.RectangleF.Empty;
                    switch (i.Sizing)
                    {
                        case ImageSizingEnum.AutoSize:
                            adjustedRect = new System.Drawing.RectangleF(r2.Left, r2.Top,
                                            r2.Width, r2.Height);
                            break;
                        case ImageSizingEnum.Clip:
                            // Pixels, not points: this argument is a sample count, asking a
                            // generated image - a barcode, say - for a bitmap that many
                            // samples across. Ask for three quarters as many and a dense
                            // symbology stops being readable at all.
                            var im = Draw2.Image.FromStream(new MemoryStream(
                                i.GetImageData((int)PixelsX(r2.Width, pgs), (int)PixelsY(r2.Height, pgs))));
                          
                            float originalWidth = Measurement.PointsFromPixels(im.Width, pgs.G.DpiX);
                            float originalHeight = Measurement.PointsFromPixels(im.Height, pgs.G.DpiY);
                            adjustedRect = new System.Drawing.RectangleF(r2.Left, r2.Top, originalWidth, originalHeight);
                            clipRect = new System.Drawing.RectangleF(r2.Left, r2.Top, originalWidth,originalHeight);
                            im.Dispose();
                            break;
                        case ImageSizingEnum.FitProportional:
                            float height;
                            float width;
                            float ratioIm = (float)i.SamplesH / i.SamplesW;
                            float ratioR = r2.Height / r2.Width;
                            height = r2.Height;
                            width = r2.Width;
                            if (ratioIm > ratioR)
                            {   // this means the rectangle width must be corrected 
                                width = height * (1 / ratioIm);
                            }
                            else if (ratioIm < ratioR)
                            {   // this means the rectangle height must be corrected 
                                height = width * ratioIm;
                            }
                            adjustedRect = new System.Drawing.RectangleF(r2.X, r2.Y, width, height);
                            break;
                        case ImageSizingEnum.Fit:
                        default:
                            adjustedRect = r2;
                            break;
                    }
#if !DRAWINGCOMPAT
                    if (i.ImgFormat == Draw2.Imaging.ImageFormat.Wmf || i.ImgFormat == Draw2.Imaging.ImageFormat.Emf)
                    {
                        //We dont want to add it - its already been broken down into page items;
                    }
                    else
                    {
#endif

                        AddImage(i.Name, i.SI, i.ImgFormat,
                            adjustedRect.X, adjustedRect.Y, adjustedRect.Width,
                            adjustedRect.Height, clipRect, 
                            i.GetImageData((int)clipRect.Width, (int)clipRect.Height),
                            i.SamplesW, i.SamplesH, i.HyperLink, i.Tooltip);
#if !DRAWINGCOMPAT
                    }
#endif
                    continue;
                }

                if (pi is PageRectangle)
                {
                    PageRectangle pr = pi as PageRectangle;
                    AddRectangle(pr.X, pr.Y, pr.H, pr.W, pi.SI, pi.HyperLink, pi.Tooltip);
                    continue;
                }
                if (pi is PagePie)
                {   // TODO
                    PagePie pp = pi as PagePie;
                    // 
                    AddPie(pp.X, pp.Y, pp.H, pp.W, pi.SI, pi.HyperLink, pi.Tooltip);
                    continue;
                }
                if (pi is PagePolygon)
                {
                    PagePolygon ppo = pi as PagePolygon;
                    AddPolygon(ppo.Points, pi.SI, pi.HyperLink);
                    continue;
                }
                if (pi is PageCurve)
                {
                    PageCurve pc = pi as PageCurve;
                    AddCurve(pc.Points, pi.SI);
                    continue;
                }

            }

        }

        internal float PixelsX(float x, Pages pgs)
        {
            return (float)(x * pgs.G.DpiX / 72.0f);
        }

        internal float PixelsY(float y, Pages pgs)
        {
            return (float)(y * pgs.G.DpiY / 72.0f);
        }

        private string[] MeasureString(PageText pt, Draw2.Graphics g, out float[] width)
        {
            StyleInfo si = pt.SI;
            string s = pt.Text;

            Draw2.Font drawFont = null;
            Draw2.StringFormat drawFormat = null;
            System.Drawing.SizeF ms;
            string[] sa = null;
            width = null;
            try
            {
                // STYLE
                Draw2.FontStyle fs = 0;
                if (si.FontStyle == FontStyleEnum.Italic)
                    fs |= Draw2.FontStyle.Italic;

                // WEIGHT
                switch (si.FontWeight)
                {
                    case FontWeightEnum.Bold:
                    case FontWeightEnum.Bolder:
                    case FontWeightEnum.W500:
                    case FontWeightEnum.W600:
                    case FontWeightEnum.W700:
                    case FontWeightEnum.W800:
                    case FontWeightEnum.W900:
                        fs |= Draw2.FontStyle.Bold;
                        break;
                    default:
                        break;
                }

                drawFont = new Draw2.Font(StyleInfo.GetFontFamily(si.FontFamilyFull), Style.CompatFontSize(si.FontSize), fs);
                drawFormat = new Draw2.StringFormat();
                drawFormat.Alignment = Draw2.StringAlignment.Near;

                // Measure string   
                //  pt.NoClip indicates that this was generated by PageTextHtml Build.  It has already word wrapped.
                if (pt.NoClip || pt.SI.WritingMode == WritingModeEnum.tb_rl)	// TODO: support multiple lines for vertical text
                {
                    ms = MeasureString(s, g, drawFont, drawFormat);
                    width = new float[1];
                    width[0] = Measurement.PointsFromPixels(ms.Width, g.DpiX);	// convert to points from pixels
                    sa = new string[1];
                    sa[0] = s;
                    return sa;
                }

                // handle multiple lines;
                //  1) split the string into the forced line breaks (ie "\n and \r")
                //  2) foreach of the forced line breaks; break these into words and recombine 
                s = s.Replace("\r\n", "\n");	// don't want this to result in double lines
                string[] flines = s.Split(LINEBREAK);
                List<string> lines = new List<string>();
                List<float> lineWidths = new List<float>();
                // remove the size reserved for left and right padding
                float ptWidth = pt.W - pt.SI.PaddingLeft - pt.SI.PaddingRight;
                if (ptWidth <= 0)
                    ptWidth = 1;
                foreach (string tfl in flines)
                {
                    string fl;
                    if (tfl.Length > 0 && tfl[tfl.Length - 1] == ' ')
                        fl = tfl.TrimEnd(' ');
                    else
                        fl = tfl;

                    // Check if entire string fits into a line
                    ms = MeasureString(fl, g, drawFont, drawFormat);
                    float tw = Measurement.PointsFromPixels(ms.Width, g.DpiX);
                    if (tw <= ptWidth)
                    {					   // line fits don't need to break it down further
                        lines.Add(fl);
                        lineWidths.Add(tw);
                        continue;
                    }

                    // Line too long; need to break into multiple lines
                    // 1) break line into parts; then build up again keeping track of word positions
                    string[] parts = fl.Split(WORDBREAK);	// this is the maximum split of lines
                    StringBuilder sb = new StringBuilder(fl.Length);
                    Draw2.CharacterRange[] cra = new Draw2.CharacterRange[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        int sc = sb.Length;	 // starting character
                        sb.Append(parts[i]);	// endding character
                        if (i != parts.Length - 1)  // last item doesn't need blank
                            sb.Append(" ");
                        int ec = sb.Length;
                        Draw2.CharacterRange cr = new Draw2.CharacterRange(sc, ec - sc);
                        cra[i] = cr;			// add to character array
                    }

                    // 2) Measure the word locations within the line
                    string wfl = sb.ToString();
                    WordStartFinish[] wordLocations = MeasureString(wfl, g, drawFont, drawFormat, cra);
                    if (wordLocations == null)
                        continue;

                    // 3) Loop thru creating new lines as needed
                    int startLoc = 0;
                    Draw2.CharacterRange crs = cra[startLoc];
                    Draw2.CharacterRange cre = cra[startLoc];
                    float cwidth = wordLocations[0].end;	// length of the first
                    float bwidth = wordLocations[0].start;  // characters need a little extra on start
                    string ts;
                    bool bLine = true;
                    for (int i = 1; i < cra.Length; i++)
                    {
                        cwidth = wordLocations[i].end - wordLocations[startLoc].start + bwidth;
                        if (cwidth > ptWidth)
                        {	// time for a new line
                            cre = cra[i - 1];
                            ts = wfl.Substring(crs.First, cre.First + cre.Length - crs.First);
                            lines.Add(ts);
                            lineWidths.Add(wordLocations[i - 1].end - wordLocations[startLoc].start + bwidth);

                            // Find the first non-blank character of the next line
                            while (i < cra.Length &&
                                    cra[i].Length == 1 &&
                                    fl[cra[i].First] == ' ')
                            {
                                i++;
                            }
                            if (i < cra.Length)   // any lines left?
                            {  // yes, continue on
                                startLoc = i;
                                crs = cre = cra[startLoc];
                                cwidth = wordLocations[i].end - wordLocations[startLoc].start + bwidth;
                            }
                            else  // no, we can stop
                                bLine = false;
                            //  bwidth = wordLocations[startLoc].start - wordLocations[startLoc - 1].end;
                        }
                        else
                            cre = cra[i];
                    }
                    if (bLine)
                    {
                        ts = fl.Substring(crs.First, cre.First + cre.Length - crs.First);
                        lines.Add(ts);
                        lineWidths.Add(cwidth);
                    }
                }
                // create the final array from the Lists
                string[] la = lines.ToArray();
                width = lineWidths.ToArray();
                return la;
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawFormat != null)
                    drawFont.Dispose();
            }
        }

        /// <summary>
        /// Measures the location of an arbritrary # of words within a string
        /// </summary>
        private WordStartFinish[] MeasureString(string s, Draw2.Graphics g, Draw2.Font drawFont, Draw2.StringFormat drawFormat, Draw2.CharacterRange[] cra)
        {
            if (cra.Length <= MEASUREMAX)		// handle the simple case of < MEASUREMAX words
                return MeasureString32(s, g, drawFont, drawFormat, cra);

            // Need to compensate for SetMeasurableCharacterRanges limitation of 32 (MEASUREMAX)
            int mcra = (cra.Length / MEASUREMAX);	// # of full 32 arrays we need
            int ip = cra.Length % MEASUREMAX;		// # of partial entries needed for last array (if any)
            WordStartFinish[] sz = new WordStartFinish[cra.Length];	// this is the final result;
            float startPos = 0;
            Draw2.CharacterRange[] cra32 = new Draw2.CharacterRange[MEASUREMAX];	// fill out			
            int icra = 0;						// index thru the cra 
            for (int i = 0; i < mcra; i++)
            {
                // fill out the new array
                int ticra = icra;
                for (int j = 0; j < cra32.Length; j++)
                {
                    // Build a fresh range rather than copy-then-adjust: CharacterRange is a struct in
                    // System.Drawing but a class in the compat layer, so assigning and then mutating
                    // .First would rewrite the caller's cra entry and corrupt every range past the
                    // first 32-word chunk.
                    cra32[j] = new Draw2.CharacterRange(cra[ticra].First - cra[icra].First, cra[ticra].Length);
                    ticra++;
                }

                // measure the word locations (in the new string)
                // ???? should I put a blank in front of it?? 
                string ts = s.Substring(cra[icra].First,
                    cra[icra + cra32.Length - 1].First + cra[icra + cra32.Length - 1].Length - cra[icra].First);
                WordStartFinish[] pos = MeasureString32(ts, g, drawFont, drawFormat, cra32);

                // copy the values adding in the new starting positions
                for (int j = 0; j < pos.Length; j++)
                {
                    sz[icra].start = pos[j].start + startPos;
                    sz[icra++].end = pos[j].end + startPos;
                }
                startPos = sz[icra - 1].end;	// reset the start position for the next line
            }
            // handle the remaining character
            if (ip > 0)
            {
                // resize the range array
                cra32 = new Draw2.CharacterRange[ip];
                // fill out the new array
                int ticra = icra;
                for (int j = 0; j < cra32.Length; j++)
                {
                    // Build a fresh range rather than copy-then-adjust: CharacterRange is a struct in
                    // System.Drawing but a class in the compat layer, so assigning and then mutating
                    // .First would rewrite the caller's cra entry and corrupt every range past the
                    // first 32-word chunk.
                    cra32[j] = new Draw2.CharacterRange(cra[ticra].First - cra[icra].First, cra[ticra].Length);
                    ticra++;
                }
                // measure the word locations (in the new string)
                // ???? should I put a blank in front of it?? 
                string ts = s.Substring(cra[icra].First,
                    cra[icra + cra32.Length - 1].First + cra[icra + cra32.Length - 1].Length - cra[icra].First);
                WordStartFinish[] pos = MeasureString32(ts, g, drawFont, drawFormat, cra32);

                // copy the values adding in the new starting positions
                for (int j = 0; j < pos.Length; j++)
                {
                    sz[icra].start = pos[j].start + startPos;
                    sz[icra++].end = pos[j].end + startPos;
                }
            }
            return sz;
        }

        /// <summary>
        /// Measures the location of words within a string;  limited by .Net 1.1 to 32 words
        ///	 MEASUREMAX is a constant that defines that limit
        /// </summary>
        /// <param name="s"></param>
        /// <param name="g"></param>
        /// <param name="drawFont"></param>
        /// <param name="drawFormat"></param>
        /// <param name="cra"></param>
        /// <returns></returns>
        private WordStartFinish[] MeasureString32(string s, Draw2.Graphics g, Draw2.Font drawFont, Draw2.StringFormat drawFormat, Draw2.CharacterRange[] cra)
        {
            if (s == null || s.Length == 0)
                return null;

            drawFormat.SetMeasurableCharacterRanges(cra);
            Draw2.Region[] rs = new Draw2.Region[cra.Length];
            rs = g.MeasureCharacterRanges(s, drawFont, new System.Drawing.RectangleF(0, 0, float.MaxValue, float.MaxValue),
                drawFormat);
            WordStartFinish[] sz = new WordStartFinish[cra.Length];
            int isz = 0;
            foreach (Draw2.Region r in rs)
            {
                System.Drawing.RectangleF mr = r.GetBounds(g);
                sz[isz].start = Measurement.PointsFromPixels(mr.Left, g.DpiX);
                sz[isz].end = Measurement.PointsFromPixels(mr.Right, g.DpiX);
                isz++;
            }
            return sz;
        }

        struct WordStartFinish
        {
            internal float start;
            internal float end;
        }

        private System.Drawing.SizeF MeasureString(string s, Draw2.Graphics g, Draw2.Font drawFont, Draw2.StringFormat drawFormat)
        {
            if (s == null || s.Length == 0)
                return System.Drawing.SizeF.Empty;

            Draw2.CharacterRange[] cr = { new Draw2.CharacterRange(0, s.Length) };
            drawFormat.SetMeasurableCharacterRanges(cr);
            Draw2.Region[] rs = new Draw2.Region[1];
            rs = g.MeasureCharacterRanges(s, drawFont, new System.Drawing.RectangleF(0, 0, float.MaxValue, float.MaxValue),
                drawFormat);
            System.Drawing.RectangleF mr = rs[0].GetBounds(g);

            return new System.Drawing.SizeF(mr.Width, mr.Height);
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

        // Tables					// Report item table
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

        public Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)				// called first
        {
            return Task.FromResult(true);
        }

        public void MatrixColumns(Matrix m, MatrixColumns mc)	// called just after MatrixStart
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

        public Task MatrixEnd(Matrix m, Row r)				// called last
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

        public void GroupingStart(Grouping g)			// called at start of grouping
        {
        }
        public void GroupingInstanceStart(Grouping g)	// called at start for each grouping instance
        {
        }
        public void GroupingInstanceEnd(Grouping g)	// called at start for each grouping instance
        {
        }
        public void GroupingEnd(Grouping g)			// called at end of grouping
        {
        }
    }
}
