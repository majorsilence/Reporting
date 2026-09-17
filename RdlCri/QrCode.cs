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
    public class QrCode : ZxingBarcodes
    {
        public QrCode() : base(35.91f, 35.91f) // Optimal width at mag 1
        {
            format = ZXing.BarcodeFormat.QR_CODE;
        }
    }
}