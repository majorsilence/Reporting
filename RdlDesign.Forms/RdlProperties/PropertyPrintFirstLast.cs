using Majorsilence.Reporting.RdlDesign.Resources;

using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyExpr - 
    /// </summary>
    [TypeConverter(typeof(PropertyPrintFirstLastConverter))]
    internal class PropertyPrintFirstLast
    {
        PropertyReport _pr;
        XmlNode _parent;

        public PropertyPrintFirstLast(PropertyReport pr, XmlNode phNode)
        {
            _pr = pr;
            _parent = phNode;
        }

		[LocalizedDisplayName("PrintFirstLast_Height")]
        public string Height
        {
            get
            {
                return _pr.Draw.GetElementValue(_parent, "Height", "0pt");
            }
            set
            {
                string v = value;
                if (v.Length == 0)
                    v = "0pt";
                else
                    DesignerUtility.ValidateSize(v, true, false);

                SetProp("Height", v);
                _pr.DesignCtl.SetScrollControls();          // this will force ruler and scroll bars to be updated

            }
        }

		[LocalizedDisplayName("PrintFirstLast_PrintOnFirstPage")]
		public bool PrintOnFirstPage
        {
            get
            {
                return _pr.Draw.GetElementValue(_parent, "PrintOnFirstPage", "false").ToLower() == "true" ? true : false;
            }
            set
            {
                SetPrint("PrintOnFirstPage", value);
            }
        }

		[LocalizedDisplayName("PrintFirstLast_PrintOnLastPage")]
		public bool PrintOnLastPage
        {
            get
            {
                return _pr.Draw.GetElementValue(_parent, "PrintOnLastPage", "false").ToLower() == "true" ? true : false;
            }
            set
            {
                SetPrint("PrintOnLastPage", value);
            }
        }

        void SetPrint(string l, bool b)
        {
            SetProp(l, b ? "true" : "false");
        }

        void SetProp(string l, string v)
        {
            _pr.DesignCtl.StartUndoGroup(l + " " + Strings.PropertyPrintFirstLast_SetProp_change);
            _pr.Draw.SetElement(_parent, l, v);
            _pr.DesignCtl.EndUndoGroup(true);
            _pr.DesignCtl.SignalReportChanged();
            _pr.Draw.Invalidate();
        }

    }

    internal class PropertyPrintFirstLastConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context,
                                         System.Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyPrintFirstLast)
            {
                return "";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}