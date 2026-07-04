
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyExpr - 
    /// </summary>
    [TypeConverter(typeof(PropertyLocationConverter))]
    internal class PropertyLocation
    {
        PropertyReportItem _pri;
        string _left;
        string _top;

        public PropertyLocation(PropertyReportItem pri, string x, string y)
        {
            _pri = pri;
            _left = x;
            _top = y;
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Location_Left")]
        public string Left
        {
            get { return _left; }
            set 
            {
                DesignerUtility.ValidateSize(value, true, false);
                _left = value;
                _pri.SetValue("Left", value);
            }
        }
        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Location_Top")]
		public string Top
        {
            get { return _top; }
            set 
            {
                DesignerUtility.ValidateSize(value, true, false);
                _top = value;
                _pri.SetValue("Top", value);
            }
        }
    }

    internal class PropertyLocationConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyLocation)
            {
                PropertyLocation pe = value as PropertyLocation;
                return string.Format("({0}, {1})", pe.Left, pe.Top);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}