using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Draw2 = Majorsilence.Forms.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Majorsilence.Reporting.Cri
{
    public class BarCode39 : ZxingBarcodes
    {
        public BarCode39() : base(35.91f, 65.91f) // Optimal width at mag 1
        {
            format = ZXing.BarcodeFormat.CODE_39;
        }
    }
}