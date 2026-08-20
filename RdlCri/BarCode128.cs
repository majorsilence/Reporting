using System;
using System.Collections.Generic;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
#else
using Draw2 = System.Drawing;
#endif
using Majorsilence.Reporting.Rdl;
using System.Text;
using System.Xml;
using System.ComponentModel;

namespace Majorsilence.Reporting.Cri
{
    public class BarCode128 : ZxingBarcodes
    {
        public BarCode128() : base(35.91f, 65.91f) // Optimal width at mag 1
        {
            format = ZXing.BarcodeFormat.CODE_128;
        }
    }
}