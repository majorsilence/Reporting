using Majorsilence.Reporting.Rdl;
using System;
using System.Collections.Generic;
using Draw2 = Majorsilence.Forms.Drawing;
using System.Xml;
using System.ComponentModel;
using ZXing;

namespace Majorsilence.Reporting.Cri
{
    public class BarCodeITF14 : ICustomReportItem
    {
        static public readonly float OptimalWidth = 78f;
        static public readonly float OptimalHeight = 39f;
        private string _Itf14Data;

        public void Dispose() { }

        public void DrawDesignerImage(ref Draw2.Bitmap bm)
        {
            InternalDraw(ref bm, "12345678901231");
        }

        public void DrawImage(ref Draw2.Bitmap bm)
        {
            InternalDraw(ref bm, _Itf14Data);
        }

        private void InternalDraw(ref Draw2.Bitmap bm, string value)
        {
            var writer = new ZXing.SkiaSharp.BarcodeWriter();
            writer.Format = BarcodeFormat.ITF;
            writer.Options.Width = Math.Max(1, bm.Width);
            writer.Options.Height = Math.Max(1, bm.Height);
            writer.Options.Hints[EncodeHintType.CHARACTER_SET] = "UTF-8";

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch (InvalidOperationException) { }

            // Majorsilence.Forms.Drawing declares the SKBitmap conversion on Image; the object it
            // hands back is a Bitmap, so the ref parameter's type needs the downcast.
            bm = (Draw2.Bitmap)(Draw2.Image)writer.Write(value);
        }

        public string GetCustomReportItemXml()
        {
            return "<CustomReportItem><Type>{0}</Type>" +
               string.Format("<Height>{0}mm</Height><Width>{1}mm</Width>", OptimalHeight, OptimalWidth) +
               "<CustomProperties>" +
               "<CustomProperty>" +
               "<Name>ITF14</Name>" +
               "<Value>Enter Your Value</Value>" +
               "</CustomProperty>" +
               "</CustomProperties>" +
               "</CustomReportItem>";
        }

        public object GetPropertiesInstance(XmlNode iNode)
        {
            BarCodePropertiesItf14 bcp = new BarCodePropertiesItf14(this, iNode);

            foreach (XmlNode n in iNode.ChildNodes)
            {
                if (n.Name != "CustomProperty")
                    continue;
                string pname = XmlHelpers.GetNamedElementValue(n, "Name", "");
                switch (pname)
                {
                    case "ITF14":
                        bcp.SetITF14(XmlHelpers.GetNamedElementValue(n, "Value", ""));
                        break;
                    default:
                        break;
                }
            }

            return bcp;
        }

        public bool IsDataRegion()
        {
            return false;
        }

        public void SetProperties(IDictionary<string, object> props)
        {
            try
            {
                _Itf14Data = props["ITF14"].ToString();
                if (_Itf14Data.Length < 13 || _Itf14Data.Length > 14)
                    throw new Exception("ITF 14 data must be of length 13 or 14");
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("ITF14 property must be specified");
            }
        }

        public void SetPropertiesInstance(XmlNode node, object inst)
        {
            node.RemoveAll();

            var itfCode = inst as BarCodePropertiesItf14;
            if (itfCode == null)
                return;

            XmlHelpers.CreateChild(node, "ITF14", itfCode.Itf14);
        }

        public class BarCodePropertiesItf14
        {
            string _itf14Data;
            BarCodeITF14 _itf14;
            XmlNode _node;

            internal BarCodePropertiesItf14(BarCodeITF14 bc, XmlNode node)
            {
                _itf14 = bc;
                _node = node;
            }

            internal void SetITF14(string ns)
            {
                _itf14Data = ns;
            }

            [Category("ITF14"), Description("The text string to be encoded as a ITF14 barcode.")]
            public string Itf14
            {
                get { return _itf14Data; }
                set { _itf14Data = value; _itf14.SetPropertiesInstance(_node, this); }
            }
        }
    }
}
