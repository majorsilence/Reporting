
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Globalization;
using System.Net;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{

	/// <summary>
	/// Control for providing a designer image of RDL.   Works directly off the RDL XML.
	/// </summary>
    internal class DesignRuler : UserControl, System.ComponentModel.ISupportInitialize
    {
        DesignCtl _Design = null;
        bool _Vertical = false;
        int _Offset = 0;            // offset to start with (in pixels)
        bool _IsMetric;
        int _Intervals;

        // Background colors for gradient
        static readonly Color BEGINCOLOR = Color.White;
        static readonly Color ENDCOLOR = Color.FromArgb(30, Color.LightSkyBlue);
        static readonly Color GAPCOLOR = Color.FromArgb(30, Color.LightSkyBlue);

        internal DesignRuler()
            : base()
        {
            // force to double buffering for smoother drawing
            this.DoubleBuffered = true;

            //editor = e;
            //editor.TextChanged += new System.EventHandler(editor_TextChanged);
            //editor.Resize += new System.EventHandler(editor_Resize);
            //editor.VScroll += new System.EventHandler(editor_VScroll);

            //RegionInfo rinfo = new RegionInfo(CultureInfo.CurrentCulture.Name);
            //_IsMetric = rinfo.IsMetric;
            //_Intervals = _IsMetric ? 4 : 8;

            this.Paint += new PaintEventHandler(DesignRulerPaint);
        }

         [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal DesignCtl Design
        {
            get { return _Design; }
            set 
            { 
                _Design = value;
                if (_Design == null)
                    return;
                if (_Vertical)
                {
                    _Design.VerticalScrollChanged += new System.EventHandler(ScrollChanged);
                    // need to know when the various heights change as well
                    _Design.HeightChanged += new DesignCtl.HeightEventHandler(HeightChanged);
                }
                else
                    _Design.HorizontalScrollChanged += new System.EventHandler(ScrollChanged);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Vertical
        {
            get { return _Vertical; }
            set { _Vertical = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }

        private int ScrollPosition
        {
            get 
            {
                if (_Design == null)
                    return 0;
                return _Vertical ? _Design.VerticalScrollPosition : _Design.HorizontalScrollPosition; 
            }
        }
        
		private void DesignRulerPaint(object sender, System.Windows.Forms.PaintEventArgs e)
        {

            _IsMetric = (RdlDesign.RdlDesigner.MeasureUnits == "cm" || RdlDesign.RdlDesigner.MeasureUnits == "mm") ? true : false;
            _Intervals = _IsMetric ? 4 : 8;
            if (_Vertical)
                Ruler_DrawVert(e.Graphics);
            else
                Ruler_DrawHorz(e.Graphics);
        }

        private void HeightChanged(object sender, HeightEventArgs e)
        {
            if (e.Node != null)
            {
                // Only need to invalidate when the Body, PageHeader or PageFooter change height
                XmlNode p = e.Node.ParentNode;
                if (p != null &&
                    (p.Name == "Body" || p.Name == "PageHeader" || p.Name == "PageFooter"))
                {
                    this.Invalidate();
                }
            }
        }

        private void ScrollChanged(object sender, System.EventArgs e)
        {
            this.Invalidate();
            _Design.Invalidate();
        }
        
        private void Ruler_DrawHorz(Graphics g)
        {
            float xoff, yoff, xinc;

            StringFormat drawFormat=null;
            Font f = null;
            LinearGradientBrush lgb=null;
            try
            {
                drawFormat = new StringFormat();
                drawFormat.FormatFlags |= StringFormatFlags.NoWrap;

                drawFormat.Alignment = StringAlignment.Near;
                float mod;
                yoff = this.Height/2 -2;
                mod = g.DpiX;
                if (RdlDesign.RdlDesigner.MeasureUnits == "mm")
                    mod = mod / 25.4f;
                else if (_IsMetric)
                    mod = mod / 2.54f;
                mod *= _Design.SCALEX;
                xinc = mod / (_Intervals * _Design.SCALEX);
                xinc *= _Design.SCALEX;

                float scroll = ScrollPosition;
                if (scroll == 0)
                    xoff = 0;
                else
                    xoff = scroll + (xinc - scroll % xinc);
                
                RectangleF rf;
                if (Offset > 0) // Fill in the left gap; if any
                {
                    rf = new RectangleF(0, 0, Offset, this.Height);
                    if (rf.IntersectsWith(g.ClipBounds))
                    {
                        lgb = new LinearGradientBrush(rf, BEGINCOLOR, ENDCOLOR, LinearGradientMode.ForwardDiagonal);
                        g.FillRectangle(lgb, rf);
                        lgb.Dispose();
                        lgb = null;
//                        g.FillRectangle(new SolidBrush(GAPCOLOR), rf);
                    }
                }

                // Fill in the background for the entire ruler
                rf = new RectangleF(this.Offset, 0, this.Width, this.Height);
                if (rf.IntersectsWith(g.ClipBounds))
                {
                    lgb = new LinearGradientBrush(rf, BEGINCOLOR, ENDCOLOR, LinearGradientMode.Vertical);
                    g.FillRectangle(lgb, rf);
                    lgb.Dispose();
                    lgb = null;
                }
                else      // nothing to draw
                    return;

                f = new Font("Arial", 8, FontStyle.Regular);

                // Loop thru and draw the ruler
                while (xoff - scroll < this.Width-Offset)
                {
            //        if (xoff % mod < .001f )
                    if (xoff % mod < .1f || Math.Abs((xoff % mod) - mod) < .1f)
                    {
                        if (xoff != 0)  // Don't draw the zero
                        {
                            string l = string.Format("{0:0}", xoff / mod);
                            SizeF sz = g.MeasureString(l, f);

                            g.DrawString(l, f, Brushes.Black,
                                Offset + xoff - (sz.Width / 2) - scroll, yoff - (sz.Height / 2), drawFormat);
                        }
                        g.DrawLine(Pens.Black, Offset + xoff - scroll, this.Height, Offset + xoff - scroll, this.Height - 2);
                    }
                    else
                    {
                        g.DrawLine(Pens.Black, Offset + xoff - scroll, yoff, Offset + xoff - scroll, yoff + 2);
                    }
                    xoff += xinc;
                }
            }
            finally
            {
                if (lgb != null)
                    lgb.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
                if (f != null)
                    f.Dispose();
            }
        }

        private void Ruler_DrawVert(Graphics g)
        {
            StringFormat strFormat = null;
            Font fontOutput = null;
            SolidBrush sb = null;           // brush for non-ruler portions of ruler
            SolidBrush bb = null;           // brush for drawing the areas next to band separator
            float SectionHeigth = 0f;

            g.ScaleTransform(1f, 1f);
            g.PageUnit = GraphicsUnit.Point;

            try
            {
                // create some drawing resources
                strFormat = new StringFormat();
                strFormat.FormatFlags |= StringFormatFlags.NoWrap;
                strFormat.Alignment = StringAlignment.Near;
                fontOutput = new Font("Arial", 8, FontStyle.Regular);
                sb = new SolidBrush(GAPCOLOR);
                bb = new SolidBrush(Design.SepColor);
                // Go thru the regions

                float sp = Design.PointsY(this.ScrollPosition) * _Design.SCALEY;


                // 1) Offset
                RectangleF rf;
                float off = 0;
                float offset = Design.PointsY(this.Offset);
                float width = Design.PointsX(this.Width);
                if (this.Offset > 0)
                {
                    rf = new RectangleF(0, 0, width, offset); // scrolling doesn't affect offset
                    if (rf.IntersectsWith(g.ClipBounds))
                    {
                        LinearGradientBrush lgb = new LinearGradientBrush(rf, BEGINCOLOR, ENDCOLOR, LinearGradientMode.ForwardDiagonal);
                        g.FillRectangle(lgb, rf);
                        lgb.Dispose();
                        lgb = null;
                    }
                    off = offset;
                }


                // 2) PageHeader
                if (Design.PageHeaderHeight > 0)
                {
                    SectionHeigth = Design.PageHeaderHeight * _Design.SCALEY;
                    Ruler_DrawVertPart(g, fontOutput, strFormat, off, SectionHeigth);
                    off += SectionHeigth;
                }

                // 3) PageHeader separator
                SectionHeigth = Design.SepHeight * _Design.SCALEY;
                rf = new RectangleF(0, off - sp, width, SectionHeigth);
                if (rf.IntersectsWith(g.ClipBounds))
                    g.FillRectangle(bb, rf);
                off += SectionHeigth;

                // 4) Body
                if (Design.BodyHeight > 0)
                {
                    SectionHeigth = Design.BodyHeight * _Design.SCALEY;
                    Ruler_DrawVertPart(g, fontOutput, strFormat, off, SectionHeigth);
                    off += SectionHeigth;
                }

                // 5) Body separator
                SectionHeigth = Design.SepHeight * _Design.SCALEY;
                rf = new RectangleF(0, off - sp, width, SectionHeigth);
                if (rf.IntersectsWith(g.ClipBounds))
                    g.FillRectangle(bb, rf);
                off += SectionHeigth;


                // 6) PageFooter
                if (Design.PageFooterHeight > 0)
                {
                    SectionHeigth = Design.PageFooterHeight * _Design.SCALEY;
                    Ruler_DrawVertPart(g, fontOutput, strFormat, off, SectionHeigth);
                    off += SectionHeigth;
                }

                // 7) PageFooter separator
                SectionHeigth = Design.SepHeight * _Design.SCALEY;
                rf = new RectangleF(0, off - sp, width, SectionHeigth);
                if (rf.IntersectsWith(g.ClipBounds))
                    g.FillRectangle(bb, rf);
                off += SectionHeigth;

                // 8) The rest to end
                rf = new RectangleF(0, off - sp, width, Design.PointsY(this.Height) - (off - sp));
                if (rf.IntersectsWith(g.ClipBounds))
                    g.FillRectangle(sb, rf);
            }
            finally
            {
                if (strFormat != null)
                    strFormat.Dispose();
                if (fontOutput != null)
                    fontOutput.Dispose();
                if (sb != null)
                    sb.Dispose();
                if (bb != null)
                    bb.Dispose();
            }
        }
        private void Ruler_DrawVertPart(Graphics g, Font f, StringFormat df, float offset, float height)
        {
            float xoff, yoff, yinc, sinc;
            float mod;

            xoff = Design.PointsX(this.Width / 2 - 2);
            mod = Design.PointsY(g.DpiY);
            if (RdlDesign.RdlDesigner.MeasureUnits == "mm")
                mod = Design.PointsY(g.DpiY / 25.4f);
            else if (_IsMetric)
                mod = Design.PointsY(g.DpiY / 2.54f);
            mod *= _Design.SCALEY;

            yinc = mod / (_Intervals * _Design.SCALEY);
            yinc *= _Design.SCALEY;
            
            float scroll = Design.PointsY(ScrollPosition)*_Design.SCALEY;
            sinc = yoff = 0;
            if (scroll > offset)
                sinc += (yinc - scroll % yinc);

            // Fill in the background for the entire ruler
            RectangleF rf = new RectangleF(0, yoff + offset - scroll, this.Width, height);
        //    RectangleF rf = new RectangleF(0, offset - scroll, Design.PointsX(this.Width), height);
            if (rf.IntersectsWith(g.ClipBounds))
            {
                LinearGradientBrush lgb = new LinearGradientBrush(rf, BEGINCOLOR, ENDCOLOR, LinearGradientMode.Horizontal);
                g.FillRectangle(lgb, rf);
                lgb.Dispose();
            }
            else
                return;         // nothing to draw

            // Loop thru and draw the ruler
            float width = Design.PointsX(this.Width);
            while (sinc + offset + yoff - scroll < (offset + height) - scroll &&
                sinc + offset + yoff - scroll < g.ClipBounds.Bottom)
            {
                if (sinc + offset + yoff - scroll < g.ClipBounds.Top - 20)
                {   // we don't need to do anything here 
                }
                else if (yoff % mod < .1f || Math.Abs((yoff % mod) - mod) < .1f)
                {
                    if (yoff != 0)      // Don't draw the 0
                    {
                        string l = string.Format("{0:0}", yoff / mod);
                        SizeF sz = g.MeasureString(l, f);

                        g.DrawString(l, f, Brushes.Black,
                            xoff - (sz.Width / 2), sinc+offset + yoff - (sz.Height / 2) - scroll, df);
                    }
                    g.DrawLine(Pens.Black, width, sinc + offset + yoff - scroll, width - 2, sinc+offset + yoff - scroll);
                }
                else
                {
                    g.DrawLine(Pens.Black, xoff, sinc + offset + yoff - scroll, xoff + 2, sinc+offset + yoff - scroll);
                }
                yoff += yinc;
            }
        }

        #region ISupportInitialize Members

        void System.ComponentModel.ISupportInitialize.BeginInit()
        {
            return;
        }

        void System.ComponentModel.ISupportInitialize.EndInit()
        {
            return;
        }

        #endregion
    }
}
