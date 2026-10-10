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
            var writer = new ZXing.SkiaSharp.BarcodeWriter();
            writer.Format = format;
            writer.Options.Hints[EncodeHintType.CHARACTER_SET] = "UTF-8";

            // Generate the barcode at the exact pixel dimensions of the destination
            // bitmap so that every renderer (Avalonia, PDF, WinForms) receives an
            // image that already matches its cell size and needs no further scaling.
            writer.Options.Height = Math.Max(1, bm.Height);
            writer.Options.Width = Math.Max(1, bm.Width);

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch (InvalidOperationException)
            {
                // The provider has already been registered.
            }

            // Majorsilence.Forms.Drawing declares the SKBitmap conversion on Image; the object it
            // hands back is a Bitmap, so the ref parameter's type needs the downcast.
            var rendered = writer.Write(qrcode);
            if (!string.Equals(_align, "Center", StringComparison.OrdinalIgnoreCase))
            {
                rendered = AlignHorizontally(rendered, _align);
            }
            bm = (Draw2.Bitmap)(Draw2.Image)rendered;
        }

        /// <summary>
        /// ZXing centers the symbol (plus quiet zone) in the requested width. Trim the blank
        /// columns either side and move the symbol to the left or right edge of the bitmap.
        /// </summary>
        private static SkiaSharp.SKBitmap AlignHorizontally(SkiaSharp.SKBitmap src, string align)
        {
            bool right = string.Equals(align, "Right", StringComparison.OrdinalIgnoreCase);
            if (!right && !string.Equals(align, "Left", StringComparison.OrdinalIgnoreCase))
                return src;

            int first = -1, last = -1;
            for (int x = 0; x < src.Width && first < 0; x++)
                for (int y = 0; y < src.Height; y++)
                    if (IsDark(src.GetPixel(x, y))) { first = x; break; }
            if (first < 0)
                return src;
            for (int x = src.Width - 1; x >= first && last < 0; x--)
                for (int y = 0; y < src.Height; y++)
                    if (IsDark(src.GetPixel(x, y))) { last = x; break; }

            int contentWidth = last - first + 1;
            var result = new SkiaSharp.SKBitmap(src.Width, src.Height);
            using (var canvas = new SkiaSharp.SKCanvas(result))
            {
                canvas.Clear(SkiaSharp.SKColors.White);
                int dest = right ? src.Width - contentWidth : 0;
                canvas.DrawBitmap(src,
                    new SkiaSharp.SKRect(first, 0, first + contentWidth, src.Height),
                    new SkiaSharp.SKRect(dest, 0, dest + contentWidth, src.Height));
            }
            src.Dispose();
            return result;
        }

        private static bool IsDark(SkiaSharp.SKColor c)
        {
            return c.Alpha > 0 && (c.Red + c.Green + c.Blue) / 3 < 128;
        }

        private string _align = "Center";

        /// <summary>
        /// Design time: Draw a hard coded BarCode for design time;  Parameters can't be
        /// relied on since they aren't available.
        /// </summary>
        /// <param name="bm"></param>
        void ICustomReportItem.DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            DrawImage(ref bm, "https://github.com/majorsilence/Reporting");
        }

        private string _code = "";

        void ICustomReportItem.SetProperties(IDictionary<string, object> props)
        {
            _align = props.TryGetValue("Align", out object alignValue) && alignValue != null
                ? alignValue.ToString()
                : "Center";

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
                    case "Align":
                        bcp.SetAlign(XmlHelpers.GetNamedElementValue(n, "Value", "Center"));
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
            if (!string.IsNullOrEmpty(bcp.Align) && bcp.Align != "Center")
                XmlHelpers.CreateChild(node, "Align", bcp.Align);
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

            string _Align = "Center";

            internal void SetAlign(string align)
            {
                _Align = align;
            }

            [Category("Code"),
             Description("Horizontal position of the barcode within its box: Left, Center or Right.")]
            [TypeConverter(typeof(StringConverter))]
            public string Align
            {
                get { return _Align; }
                set
                {
                    _Align = value;
                    _bc.SetPropertiesInstance(_node, this);
                }
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