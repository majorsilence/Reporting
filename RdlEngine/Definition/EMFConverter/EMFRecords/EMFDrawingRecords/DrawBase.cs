
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
#else
using Draw2 = System.Drawing;
#endif

namespace Majorsilence.Reporting.Rdl
{
    class DrawBase
    {
        protected const float SCALEFACTOR = 72f / 96f;
        protected System.Collections.Hashtable ObjectTable;
        protected Single X;
        protected Single Y;
        protected Single Width;
        protected Single Height;
        protected List<PageItem> items;

        protected static BorderStyleEnum getLineStyle(Draw2.Pen p)
        {
            BorderStyleEnum ls = BorderStyleEnum.Solid;
            switch (p.DashStyle)
            {               
                case Draw2.Drawing2D.DashStyle.Dash:
                    ls = BorderStyleEnum.Dashed;
                    break;
                case Draw2.Drawing2D.DashStyle.DashDot:
                    ls = BorderStyleEnum.Dashed;
                    break;
                case Draw2.Drawing2D.DashStyle.DashDotDot:
                    ls = BorderStyleEnum.Dashed;
                    break;
                case Draw2.Drawing2D.DashStyle.Dot: 
                    ls = BorderStyleEnum.Dotted;
                    break;
                case Draw2.Drawing2D.DashStyle.Solid:
                    ls = BorderStyleEnum.Solid;
                    break;
                case Draw2.Drawing2D.DashStyle.Custom:
                    ls = BorderStyleEnum.Solid;
                    break;
                default:                   
                    break;
            }  
            return ls;
        }

       
    }

    
}
