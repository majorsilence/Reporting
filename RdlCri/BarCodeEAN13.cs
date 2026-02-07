using System;
using System.Collections.Generic;
using System.Text;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Drawing;
#else
using Draw2 = System.Drawing;
#endif
using System.ComponentModel; // need this for the properties metadata
using System.Xml;
using System.Text.RegularExpressions;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Cri
{
    /// <summary>
    /// BarCode: create EAN-13 compatible BarCode images.
    ///     See http://www.barcodeisland.com/ean13.phtml for description of how to
    ///     construct a EAN-13 compatible barcode
    ///     See http://194.203.97.138/eanucc/ for the specification.
    /// </summary>
    public class BarCodeEAN13 : ICustomReportItem
    {
        // Encoding arrays: digit encoding depends on whether it is on the 
        // right hand (product code side) or the left hand (manufacturer side).
        // When on the left hand the first number of the system digit (country)
        // determines the even/odd parity order.

        // index RightHandEncoding when doing the product code side
        static readonly string[] RightHandEncoding =
        {
            "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000",
            "1110100"
        };

        // index LeftHandEncodingOdd when the ParityOrdering char is odd '1'
        static readonly string[] LeftHandEncodingOdd =
        {
            "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111",
            "0001011"
        };

        // index LeftHandEncodingEven when the ParityOrdering char is even '0'
        static readonly string[] LeftHandEncodingEven =
        {
            "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001",
            "0010111"
        };

        // index ParityOrdering using the first number of the system digit
        static readonly string[] ParityOrdering = // Convention: 1 is considered Odd, 0 is considered even
        {
            "111111", "110100", "110010", "110001", "101100", "100110", "100011", "101010", "101001", "100101"
        };

        string _NumberSystem; // Number system
        string _ManufacturerCode; // Manufacturer code (assigned by number system authority)
        string _ProductCode; // Product code (assigned by company)

//  The EAN-13 Bar Code Symbol shall be made up as follows, reading from left to right: 
//�  A left Quiet Zone
//�  A normal Guard Bar Pattern
//�  Six symbol characters from number sets A and B (each 7 modules long)
//�  A centre Guard Bar Pattern
//�  Six symbol characters from number set C (each 7 modules long)
//�  A normal Guard Bar Pattern
//�  A right Quiet Zone

        static public readonly float OptimalHeight = 25.91f; // Optimal height at magnification 1    
        static public readonly float OptimalWidth = 37.29f; // Optimal width at mag 1
        static readonly float ModuleWidth = 0.33f; // module width in mm at mag factor 1
        static readonly float FontHeight = 8; // Font height at mag factor 1
        static readonly int LeftQuietZoneModules = 11; // # of modules in left quiet zone  
        static readonly int RightQuietZoneModules = 7; // # of modules in left quiet zone  
        static readonly int GuardModules = 3; // # of modules in left and right guard
        static readonly int ManufacturingModules = 7 * 6; // # of modules in manufacturing
        static readonly int CenterBarModules = 5; // # of modules in center bar
        static readonly int ProductModules = 7 * 6; // # of modules in product + checksum

        static readonly int ModulesToManufacturingStart =
            LeftQuietZoneModules + GuardModules;

        static readonly int ModulesToManufacturingEnd =
            ModulesToManufacturingStart + ManufacturingModules;

        static readonly int ModulesToProductStart =
            ModulesToManufacturingEnd + CenterBarModules;

        static readonly int ModulesToProductEnd =
            ModulesToProductStart + ProductModules;

        static readonly int TotalModules = ModulesToProductEnd + GuardModules + RightQuietZoneModules;

        public BarCodeEAN13() // Need to be able to create an instance
        {
        }

        #region ICustomReportItem Members

        /// <summary>
        /// Runtime: Draw the BarCode
        /// </summary>
        /// <param name="bm">Bitmap to draw the barcode in.</param>
        public void DrawImage(ref Draw2.Bitmap bm)
        {
            string upcode = _NumberSystem + _ManufacturerCode + _ProductCode;

            DrawImage(bm, upcode);
        }

        /// <summary>
        /// Design time: Draw a hard coded BarCode for design time;  Parameters can't be
        /// relied on since they aren't available.
        /// </summary>
        /// <param name="bm"></param>
        public void DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            DrawImage(bm, "00" + "12345" + "12345");
        }

        /// <summary>
        /// DrawImage given a Bitmap and a upcode does all the drawing work.
        /// </summary>
        /// <param name="bm"></param>
        /// <param name="upcode"></param>
        internal void DrawImage(Draw2.Bitmap bm, string upcode)
        {
            string barPattern = this.GetEncoding(upcode);
            using Draw2.Graphics g = Draw2.Graphics.FromImage(bm);
            
            // Set graphics quality for crisp bars
            g.InterpolationMode = Draw2.Drawing2D.InterpolationMode.NearestNeighbor;
            g.SmoothingMode = Draw2.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = Draw2.Drawing2D.PixelOffsetMode.Half;
            
            float mag = PixelConversions.GetMagnification(g, bm.Width, bm.Height, OptimalHeight, OptimalWidth);

            // Convert to pixel units for crisp rendering
            g.PageUnit = Draw2.GraphicsUnit.Pixel;
            
            // Calculate dimensions in pixels (not millimeters) for sharp rendering
            float barWidthMM = ModuleWidth * mag;
            float barHeightMM = OptimalHeight * mag;
            float fontHeightMM = FontHeight * mag;
            
            // Convert MM to pixels for precise rendering
            float barWidthPixels = PixelConversions.PixelXFromMm(g, barWidthMM);
            float barHeightPixels = PixelConversions.PixelYFromMm(g, barHeightMM);
            float fontHeightPixels = PixelConversions.PixelYFromMm(g, fontHeightMM);
            
            // Ensure minimum readable font size (at least 8 pixels)
            fontHeightPixels = Math.Max(fontHeightPixels, 8.0f);
            
            // Fill in the background with white
            g.FillRectangle(Draw2.Brushes.White, 0, 0, bm.Width, bm.Height);

            // Draw the bars - round positions to whole pixels for crisp edges
            int barCount = LeftQuietZoneModules;
            foreach (char bar in barPattern)
            {
                if (bar == '1')
                {
                    float barHeightForDigits = barHeightPixels - fontHeightPixels;
                    float bh = ((barCount > ModulesToManufacturingStart && barCount < ModulesToManufacturingEnd) ||
                                (barCount > ModulesToProductStart && barCount < ModulesToProductEnd))
                        ? barHeightForDigits
                        : barHeightPixels;

                    // Round to whole pixels for crisp bars
                    float x = (float)Math.Round(barWidthPixels * barCount);
                    float width = (float)Math.Round(barWidthPixels);
                    
                    g.FillRectangle(Draw2.Brushes.Black, x, 0, width, bh);
                }

                barCount++;
            }

            // Draw the human readable portion of the barcode with anti-aliasing for text
            g.TextRenderingHint = Draw2.Text.TextRenderingHint.AntiAlias;
            using var f = new Draw2.Font("Arial", fontHeightPixels, Draw2.GraphicsUnit.Pixel);

            // Draw the left guard text (i.e. 1st digit of the NumberSystem)
            string wc = upcode.Substring(0, 1);
            float textY = barHeightPixels - fontHeightPixels;
            g.DrawString(wc, f, Draw2.Brushes.Black,
                new Draw2.PointF((float)Math.Round(barWidthPixels * LeftQuietZoneModules - g.MeasureString(wc, f).Width),
                    textY));

            // Draw the manufacturing digits
            wc = upcode.Substring(1, 6);
            g.DrawString(wc, f, Draw2.Brushes.Black,
                new Draw2.PointF((float)Math.Round(barWidthPixels * ModulesToManufacturingEnd - g.MeasureString(wc, f).Width),
                    textY));

            // Draw the product code + the checksum digit
            wc = upcode.Substring(7, 5) + CheckSum(upcode).ToString();
            g.DrawString(wc, f, Draw2.Brushes.Black,
                new Draw2.PointF((float)Math.Round(barWidthPixels * ModulesToProductEnd - g.MeasureString(wc, f).Width),
                    textY));
        }

        /// <summary>
        /// BarCode isn't a DataRegion
        /// </summary>
        /// <returns></returns>
        public bool IsDataRegion()
        {
            return false;
        }

        /// <summary>
        /// Set the properties;  No validation is done at this time.
        /// </summary>
        /// <param name="props"></param>
        public void SetProperties(IDictionary<string, object> props)
        {
            object pv;
            try
            {
                pv = props["NumberSystem"];
                if (pv is int || pv is long || pv is float || pv is double)
                    _NumberSystem = string.Format("{0:00}", pv);
                else
                    _NumberSystem = pv.ToString();
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("NumberSystem property must be specified");
            }

            if (!Regex.IsMatch(_NumberSystem, "^[0-9][0-9]$"))
                throw new Exception("NumberSystem must be a 2 digit string.");

            try
            {
                pv = props["ManufacturerCode"];
                if (pv is int || pv is long || pv is float || pv is double)
                    _ManufacturerCode = string.Format("{0:00000}", pv);
                else
                    _ManufacturerCode = pv.ToString();
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("ManufacturerCode property must be specified");
            }

            if (!Regex.IsMatch(_ManufacturerCode, "^[0-9][0-9][0-9][0-9][0-9]$"))
                throw new Exception("ManufacturerCode must be a 5 digit string.");

            try
            {
                pv = props["ProductCode"];
                if (pv is int || pv is long || pv is float || pv is double)
                    _ProductCode = string.Format("{0:00000}", pv);
                else
                    _ProductCode = pv.ToString();
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("ProductCode property must be specified.");
            }

            if (!Regex.IsMatch(_ProductCode, "^[0-9][0-9][0-9][0-9][0-9]$"))
                throw new Exception("ProductCode must be a 5 digit string.");

            return;
        }

        /// <summary>
        /// Design time call: return string with <CustomReportItem> ... </CustomReportItem> syntax for 
        /// the insert.  The string contains a variable {0} which will be substituted with the
        /// configuration name.  This allows the name to be completely controlled by
        /// the configuration file.
        /// </summary>
        /// <returns></returns>
        public string GetCustomReportItemXml()
        {
            return "<CustomReportItem><Type>{0}</Type>" +
                   string.Format("<Height>{0}mm</Height><Width>{1}mm</Width>", OptimalHeight, OptimalWidth) +
                   "<CustomProperties>" +
                   "<CustomProperty>" +
                   "<Name>NumberSystem</Name>" +
                   "<Value>00</Value>" +
                   "</CustomProperty>" +
                   "<CustomProperty>" +
                   "<Name>ManufacturerCode</Name>" +
                   "<Value>12345</Value>" +
                   "</CustomProperty>" +
                   "<CustomProperty>" +
                   "<Name>ProductCode</Name>" +
                   "<Value>12345</Value>" +
                   "</CustomProperty>" +
                   "</CustomProperties>" +
                   "</CustomReportItem>";
        }

        /// <summary>
        /// Return an instance of the class representing the properties.
        /// This method is called at design time;
        /// </summary>
        /// <returns></returns>
        public object GetPropertiesInstance(XmlNode iNode)
        {
            BarCodeProperties bcp = new BarCodeProperties(this, iNode);
            foreach (XmlNode n in iNode.ChildNodes)
            {
                if (n.Name != "CustomProperty")
                    continue;
                string pname = this.GetNamedElementValue(n, "Name", "");
                switch (pname)
                {
                    case "ProductCode":
                        bcp.SetProductCode(GetNamedElementValue(n, "Value", "00000"));
                        break;
                    case "ManufacturerCode":
                        bcp.SetManufacturerCode(GetNamedElementValue(n, "Value", "00000"));
                        break;
                    case "NumberSystem":
                        bcp.SetNumberSystem(GetNamedElementValue(n, "Value", "00"));
                        break;
                    default:
                        break;
                }
            }

            return bcp;
        }

        /// <summary>
        /// Set the custom properties given the properties object
        /// </summary>
        /// <param name="node"></param>
        /// <param name="inst"></param>
        public void SetPropertiesInstance(XmlNode node, object inst)
        {
            node.RemoveAll(); // Get rid of all properties

            BarCodeProperties bcp = inst as BarCodeProperties;
            if (bcp == null)
                return;

            // NumberSystem
            CreateChild(node, "NumberSystem", bcp.NumberSystem);

            // ManufacturerCode
            CreateChild(node, "ManufacturerCode", bcp.ManufacturerCode);

            // ProductCode
            CreateChild(node, "ProductCode", bcp.ProductCode);
        }

        void CreateChild(XmlNode node, string nm, string val)
        {
            XmlDocument xd = node.OwnerDocument;
            XmlNode cp = xd.CreateElement("CustomProperty");
            node.AppendChild(cp);

            XmlNode name = xd.CreateElement("Name");
            name.InnerText = nm;
            cp.AppendChild(name);

            XmlNode v = xd.CreateElement("Value");
            v.InnerText = val;
            cp.AppendChild(v);
        }

        public void Dispose()
        {
            return;
        }

        #endregion


        /// <summary>
        /// GetEncoding returns a string representing the on/off bars.  It should be passed
        /// a string of 12 characters: Country code 2 chars + Manufacturers code 5 chars +
        /// Product code 5 chars.   
        /// </summary>
        /// <param name="upccode"></param>
        /// <returns></returns>
        string GetEncoding(string upccode)
        {
            if (upccode == null)
                throw new ArgumentNullException("upccode");
            else if (upccode.Length != 12)
                throw new ArgumentException(
                    "UPC code must be 12 characters: country code 2 chars, mfg code 5 chars, product code 5 chars");

            StringBuilder sb = new StringBuilder();

            // Left guard bars
            sb.Append("101");

            int cc1digit = (int)Char.GetNumericValue(upccode[0]); // country code first digit
            int digit;
            string encode;

            // 2nd Country code & Manufacturing code:
            //    loop thru second character of country code and 5 digits of manufacturers code
            string parity = BarCodeEAN13.ParityOrdering[cc1digit];
            for (int i = 1; i < 7; i++)
            {
                digit = (int)Char.GetNumericValue(upccode[i]); // get the current digit
                if (parity[i - 1] == '1')
                    encode = BarCodeEAN13.LeftHandEncodingOdd[digit];
                else
                    encode = BarCodeEAN13.LeftHandEncodingEven[digit];
                sb.Append(encode);
            }

            // Centerbars
            sb.Append("01010");

            // Product code encoding: loop thru the 5 digits of the product code
            for (int i = 7; i < 12; i++)
            {
                digit = (int)Char.GetNumericValue(upccode[i]); // get the current digit
                encode = BarCodeEAN13.RightHandEncoding[digit];
                sb.Append(encode);
            }

            // Checksum of the bar code
            digit = CheckSum(upccode);
            encode = BarCodeEAN13.RightHandEncoding[digit];
            sb.Append(encode);

            // Right guard bars
            sb.Append("101");

            return sb.ToString();
        }


        /// <summary>
        /// Calculate the checksum: (sum odd digits * 3 + sum even digits ) 
        ///   Checksum is the number that must be added to sum to make it 
        ///   evenly divisible by 10
        /// </summary>
        /// <param name="upccode"></param>
        /// <returns></returns>
        int CheckSum(string upccode)
        {
            int sum = 0;
            bool bOdd = false;
            foreach (char c in upccode)
            {
                int digit = (int)Char.GetNumericValue(c);
                sum += (bOdd ? digit * 3 : digit);
                bOdd = !bOdd; // switch every other character
            }

            int cs = 10 - (sum % 10);

            return cs == 10 ? 0 : cs;
        }

        /// <summary>
        /// Get the child element with the specified name.  Return the InnerText
        /// value if found otherwise return the passed default.
        /// </summary>
        /// <param name="xNode">Parent node</param>
        /// <param name="name">Name of child node to look for</param>
        /// <param name="def">Default value to use if not found</param>
        /// <returns>Value the named child node</returns>
        string GetNamedElementValue(XmlNode xNode, string name, string def)
        {
            if (xNode == null)
                return def;

            foreach (XmlNode cNode in xNode.ChildNodes)
            {
                if (cNode.NodeType == XmlNodeType.Element &&
                    cNode.Name == name)
                    return cNode.InnerText;
            }

            return def;
        }

        /// <summary>
        /// BarCodeProperties- All properties are type string to allow for definition of
        /// a runtime expression.
        /// </summary>
        public class BarCodeProperties
        {
            string _NumberSystem;
            string _ManufacturerCode;
            string _ProductCode;
            BarCodeEAN13 _bc;
            XmlNode _node;

            internal BarCodeProperties(BarCodeEAN13 bc, XmlNode node)
            {
                _bc = bc;
                _node = node;
            }

            internal void SetNumberSystem(string ns)
            {
                _NumberSystem = ns;
            }

            [Category("BarCode"),
             Description(
                 "The Number System consists of two (sometimes three) digits which identifies the country or region numbering authority which assigned the manufacturer code.")]
            public string NumberSystem
            {
                get { return _NumberSystem; }
                set
                {
                    _NumberSystem = value;
                    _bc.SetPropertiesInstance(_node, this);
                }
            }

            internal void SetManufacturerCode(string mc)
            {
                _ManufacturerCode = mc;
            }

            [Category("BarCode"),
             Description(
                 "Manufacturer Code is a unique 5 digit code assiged by numbering authority indicated by the number system code.")]
            public string ManufacturerCode
            {
                get { return _ManufacturerCode; }
                set
                {
                    _ManufacturerCode = value;
                    _bc.SetPropertiesInstance(_node, this);
                }
            }

            internal void SetProductCode(string pc)
            {
                _ProductCode = pc;
            }

            [Category("BarCode"),
             Description("Product Code is a unique 5 digit code assigned by the manufacturer.")]
            public string ProductCode
            {
                get { return _ProductCode; }
                set
                {
                    _ProductCode = value;
                    _bc.SetPropertiesInstance(_node, this);
                }
            }
        }
    }
}