
using System;
using System.Collections.Generic;
using System.Text;
#if DRAWINGCOMPAT
using Draw2 = Majorsilence.Forms.Drawing;
#else
using Draw2 = System.Drawing;
#endif
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;
using System.Text.RegularExpressions;

using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Cri
{
    /// <summary>
    /// BarCodeBookland: create Bookland compatible BarCode images.
    ///     See http://www.barcodeisland.com/bookland.phtml for description of how to
    ///     construct a Bookland compatible barcode.  In essence, Bookland is simply 
    ///     EAN-13 barcode with a number system of 978.
    /// </summary>
    public class BarCodeBookland: ICustomReportItem
    {
        string _ISBN;               // ISBN number
        BarCodeEAN13 _Ean13;        // the EAN-13 barcode object

        public BarCodeBookland()        // Need to be able to create an instance
        {
            _Ean13 = new BarCodeEAN13();
        }

        #region ICustomReportItem Members
        /// <summary>
        /// Runtime: Draw the BarCode
        /// </summary>
        /// <param name="bm">Bitmap to draw the barcode in.</param>
        public void DrawImage(ref Draw2.Bitmap bm)
        {
            _Ean13.DrawImage(ref bm);
        }

        /// <summary>
        /// Design time: Draw a hard coded BarCode for design time;  Parameters can't be
        /// relied on since they aren't available.
        /// </summary>
        /// <param name="bm"></param>
        public void DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            _Ean13.DrawImage(bm, "978015602732");    // Yann Martel-Life of Pi
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
            try
            {
                string p;
                if (props.TryGetValue("ISBN", out object codeValue))
                {
                    // Backwards Compatibility: if the property is present, use it
                    p = codeValue.ToString();
                }
                else {
                    // fallback to standard "Code" property
                    p = props["Code"].ToString();
                }
                if (p == null)
                    throw new Exception("ISBN property must be a string.");

                // remove any dashes
                p = p.Replace("-", "");
                if (p.Length > 9)           // get rid of the ISBN checksum digit
                    p = p.Substring(0, 9);

                if (!Regex.IsMatch(p, "^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]$"))
                    throw new Exception("ISBN must have at least nine digits.");

                _ISBN = p;

                // Now set the properties of the EAN-13
                IDictionary<string, object> ean13_props = new Dictionary<string, object>();
                ean13_props.Add("NumberSystem", "97");
                ean13_props.Add("ManufacturerCode", "8" + _ISBN.Substring(0, 4));
                ean13_props.Add("ProductCode", _ISBN.Substring(4,5));
                _Ean13.SetProperties(ean13_props);
            }
            catch (KeyNotFoundException )
            { 
                throw new Exception("ISBN property must be specified"); 
            }

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
                string.Format("<Height>{0}mm</Height><Width>{1}mm</Width>", BarCodeEAN13.OptimalHeight, BarCodeEAN13.OptimalWidth) + 
                "<CustomProperties>" +
                "<CustomProperty>" +
                "<Name>ISBN</Name>" +
                "<Value>0-15-602732-1</Value>" +    // Yann Martel- Life of Pi
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
            BarCodeBooklandProperties bcp = new BarCodeBooklandProperties(this, iNode);
            foreach (XmlNode n in iNode.ChildNodes)
            {
                if (n.Name != "CustomProperty") 
                    continue;
                string pname = this.GetNamedElementValue(n, "Name", "");
                switch (pname)
                {
                    case "ISBN":
                        bcp.SetISBN(GetNamedElementValue(n, "Value", "0-15-602732-1"));
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
            node.RemoveAll();       // Get rid of all properties

            BarCodeBooklandProperties bcp = inst as BarCodeBooklandProperties;
            if (bcp == null)
                return;

            // ISBN
            CreateChild(node, "ISBN", bcp.ISBN);
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
            _Ean13.Dispose();
            return;
        }

        #endregion

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
        public class BarCodeBooklandProperties
        {
            BarCodeBookland _bcbl;
            XmlNode _node;
            string _ISBN;

            internal BarCodeBooklandProperties(BarCodeBookland bcbl, XmlNode node)
            {
                _bcbl = bcbl;
                _node = node;
            }


            internal void SetISBN(string isbn)
            {
                _ISBN = isbn;
            }

            [Category("BarCode"),
               Description("ISBN is the book's ISBN number.")]
            public string ISBN
            {
                get { return _ISBN; }
                set 
                { 
                    _ISBN = value;
                    _bcbl.SetPropertiesInstance(_node, this);
                }
            }

        }

    }
}
