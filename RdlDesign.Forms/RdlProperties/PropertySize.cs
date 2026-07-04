
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyExpr - 
    /// </summary>
    [TypeConverter(typeof(PropertySizeConverter))]
    internal class PropertySize
    {
        PropertyReportItem _pri;
        string _h;
        string _w;

        public PropertySize(PropertyReportItem pri, string h, string w)
        {
            _pri = pri;
            _h = h;
            _w = w;
        }
        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Size_Height")]
        public string Height
        {
            get { return _h; }
            set 
            {
                DesignerUtility.ValidateSize(value, true, false);
                _h = value;
                _pri.SetValue("Height", value);
            }
        }

		[RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Size_Width")]
		public string Width
        {
            get { return _w; }
            set 
            {
                DesignerUtility.ValidateSize(value, true, false);
                _w = value;
                _pri.SetValue("Width", value);
            }
        }
    }

    internal class PropertySizeConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertySize)
            {
                PropertySize pe = value as PropertySize;
                return string.Format("({0}, {1})", pe.Height, pe.Width);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}