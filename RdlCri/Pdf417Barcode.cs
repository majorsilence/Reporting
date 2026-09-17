using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Draw2 = Majorsilence.Forms.Drawing;
using System.ComponentModel;
using System.Xml;
using ZXing;

namespace Majorsilence.Reporting.Cri
{
    public class Pdf417Barcode : ZxingBarcodes
    {
        public Pdf417Barcode() : base(35.91f, 65.91f) // Optimal width at mag 1
        {
            format = ZXing.BarcodeFormat.PDF_417;
        }
    }
}