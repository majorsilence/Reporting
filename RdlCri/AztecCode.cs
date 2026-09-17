using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Draw2 = Majorsilence.Forms.Drawing;
using System.ComponentModel;
using System.Xml;

namespace Majorsilence.Reporting.Cri
{
    public class AztecCode : ZxingBarcodes
    {
        public AztecCode() : base(35.91f, 35.91f) // Optimal width at mag 1
        {
            format = ZXing.BarcodeFormat.AZTEC;
        }
    }
}