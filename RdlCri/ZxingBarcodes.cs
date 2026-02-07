using System;
using System.Collections.Generic;
using System.Text;
using Majorsilence.Reporting.Rdl;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
#else
using Draw2 = System.Drawing;
#endif
using System.ComponentModel;
using System.Xml;
using ZXing;

namespace Majorsilence.Reporting.Cri
{
    public class ZxingBarcodes : ICustomReportItem
    {
        private readonly float OptimalHeight;  
        private readonly float OptimalWidth;
        protected ZXing.BarcodeFormat format;
        
        // special chars for datamatrix, gs1 128
        protected string FrontMatter = "";
        protected string EndMatter = "";

        #region ICustomReportItem Members

        // optimal height and width are set to 35.91mm, which is the default for most barcodes.
        public ZxingBarcodes() : this(35.91f, 65.91f) 
        {
        }

        public ZxingBarcodes(float optimalHeight, float optimalWidth)
        {
            OptimalHeight = optimalHeight;
            OptimalWidth = optimalWidth;
        }
        
        bool ICustomReportItem.IsDataRegion()
        {
            return false;
        }

        void ICustomReportItem.DrawImage(ref Draw2.Bitmap bm)
        {
            DrawImage(ref bm, _code);
        }

        /// <summary>
        /// Does the actual drawing of the image.
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="qrcode"></param>
        internal void DrawImage(ref Draw2.Bitmap bm, string qrcode)
        {
#if DRAWINGCOMPAT
            var writer = new ZXing.SkiaSharp.BarcodeWriter();
#elif NETSTANDARD2_0 || NET5_0_OR_GREATER
            var writer = new ZXing.Windows.Compatibility.BarcodeWriter();
#else
            var writer = new ZXing.BarcodeWriter();
#endif
            writer.Format = format;
            writer.Options.Hints[EncodeHintType.CHARACTER_SET] = "UTF-8";

            using Draw2.Graphics g = Draw2.Graphics.FromImage(bm);
            float mag = PixelConversions.GetMagnification(g, bm.Width, bm.Height, OptimalHeight, OptimalWidth);

            int barHeight = PixelConversions.PixelXFromMm(g, OptimalHeight * mag);
            int barWidth = PixelConversions.PixelYFromMm(g, OptimalWidth * mag);

            writer.Options.Height = barHeight;
            writer.Options.Width = barWidth;

            try
            {
                // TODO: move to program startup
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch (InvalidOperationException)
            {
                // The provider has already been registered.
            }

            var barcodeBitmap = writer.Write(qrcode);
            
            // Fill background with white
            g.FillRectangle(Draw2.Brushes.White, 0, 0, bm.Width, bm.Height);
            
            // Draw the barcode aligned to the left (top-left corner at 0, 0)
            g.DrawImage(barcodeBitmap, 0, 0);
        }

        /// <summary>
        /// Design time: Draw a hard coded BarCode for design time;  Parameters can't be
        /// relied on since they aren't available.
        /// </summary>
        /// <param name="bm"></param>
        void ICustomReportItem.DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            DrawImage(ref bm, "https://github.com/majorsilence/My-FyiReporting");
        }

        private string _code = "";

        void ICustomReportItem.SetProperties(IDictionary<string, object> props)
        {
            try
            {
                if (props.TryGetValue("AztecCode", out object codeValueA))
                {
                    // Backwards Compatibility: if the property is present, use it
                    _code = codeValueA.ToString();
                }
                else  if (props.TryGetValue("QrCode", out object codeValueQ))
                {
                    // Backwards Compatibility: if the property is present, use it
                    _code = codeValueQ.ToString();
                }
                else {
                    // fallback to standard "Code" property
                    _code = props["Code"].ToString();
                }
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Code property must be specified");
            }
        }

        object ICustomReportItem.GetPropertiesInstance(System.Xml.XmlNode iNode)
        {
            ZxingBarCodeProperties bcp = new ZxingBarCodeProperties(this, iNode);
            foreach (XmlNode n in iNode.ChildNodes)
            {
                if (n.Name != "CustomProperty")
                    continue;
                string pname = XmlHelpers.GetNamedElementValue(n, "Name", "");
                switch (pname)
                {
                    case "Code":
                        bcp.SetCode(XmlHelpers.GetNamedElementValue(n, "Value", ""));
                        break;
                    default:
                        break;
                }
            }

            return bcp;
        }

        public void SetPropertiesInstance(System.Xml.XmlNode node, object inst)
        {
            node.RemoveAll(); // Get rid of all properties

            ZxingBarCodeProperties bcp = inst as ZxingBarCodeProperties;
            if (bcp == null)
                return;


            XmlHelpers.CreateChild(node, "Code", bcp.Code);
        }


        /// <summary>
        /// Design time call: return string with <CustomReportItem> ... </CustomReportItem> syntax for 
        /// the insert.  The string contains a variable {0} which will be substituted with the
        /// configuration name.  This allows the name to be completely controlled by
        /// the configuration file.
        /// </summary>
        /// <returns></returns>
        string ICustomReportItem.GetCustomReportItemXml()
        {
            return "<CustomReportItem><Type>{0}</Type>" +
                   string.Format("<Height>{0}mm</Height><Width>{1}mm</Width>", OptimalHeight, OptimalWidth) +
                   "<CustomProperties>" +
                   "<CustomProperty>" +
                   "<Name>Code</Name>" +
                   "<Value>Enter Your Value</Value>" +
                   "</CustomProperty>" +
                   "</CustomProperties>" +
                   "</CustomReportItem>";
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            return;
        }

        #endregion

        /// <summary>
        /// BarCodeProperties- All properties are type string to allow for definition of
        /// a runtime expression.
        /// </summary>
        public class ZxingBarCodeProperties
        {
            string _Code;
            ZxingBarcodes _bc;
            XmlNode _node;

            internal ZxingBarCodeProperties(ZxingBarcodes bc, XmlNode node)
            {
                _bc = bc;
                _node = node;
            }

            internal void SetCode(string ns)
            {
                _Code = ns;
            }

            [Category("Code"),
             Description("The text string to be encoded as a PDF417 barcode.")]
            public string Code
            {
                get { return _Code; }
                set
                {
                    _Code = value;
                    _bc.SetPropertiesInstance(_node, this);
                }
            }
        }
    }
}