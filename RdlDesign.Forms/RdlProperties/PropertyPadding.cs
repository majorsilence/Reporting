
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyExpr - 
    /// </summary>
    [TypeConverter(typeof(PropertyPaddingConverter))]
    internal class PropertyPadding : IReportItem
    {
        PropertyReportItem _pri;
        string[] _names;
        string[] _subitems;

        public PropertyPadding(PropertyReportItem pri)
        {
            _pri = pri;
            _names = null;
            _subitems = new string[] { "Style", "" };
        }

        public PropertyPadding(PropertyReportItem ri, params string[] names)
        {
            _pri = ri;
            _names = names;

            // now build the array used to get/set values
            _subitems = new string[names.Length + 2];
            int i = 0;
            foreach (string s in names)
                _subitems[i++] = s;

            _subitems[i++] = "Style";
        }
        
        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Padding_Left")]
        public PropertyExpr Left
        {
            get
            {
                return new PropertyExpr(GetStyleValue("PaddingLeft", "0pt"));
            }
            set
            {
                SetPadding("PaddingLeft", value, true);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Padding_Right")]
		public PropertyExpr Right
        {
            get
            {
                return new PropertyExpr(GetStyleValue("PaddingRight", "0pt"));
            }
            set
            {
                SetPadding("PaddingRight", value, false);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Padding_Top")]
		public PropertyExpr Top
        {
            get
            {
                return new PropertyExpr(GetStyleValue("PaddingTop", "0pt"));
            }
            set
            {
                SetPadding("PaddingTop", value, true); 
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Padding_Bottom")]
		public PropertyExpr Bottom
        {
            get
            {
                return new PropertyExpr(GetStyleValue("PaddingBottom", "0pt"));
            }
            set
            {
                SetPadding("PaddingBottom", value, false); 
            }
        }

        void SetPadding(string l, PropertyExpr pe, bool bMinus) //Josh: Eddited to allow negative padding on top and left 
        {
            if (!_pri.IsExpression(pe.Expression))
            {
                DesignerUtility.ValidateSize(pe.Expression, true, /*false*/bMinus);
            }
            SetStyleValue(l, pe.Expression);
        }

        private string GetStyleValue(string l1, string def)
        {
            _subitems[_subitems.Length - 1] = l1;
            return _pri.GetWithList(def, _subitems);
        }

        private void SetStyleValue(string l1, string val)
        {
            _subitems[_subitems.Length - 1] = l1;
            _pri.SetWithList(val, _subitems);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", Left, Top, Right, Bottom);
        }

        #region IReportItem Members
        public PropertyReportItem GetPRI()
        {
            return _pri;
        }

        #endregion
    }

    internal class PropertyPaddingConverter : ExpandableObjectConverter
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
            if (destinationType == typeof(string) && value is PropertyPadding)
            {
                PropertyPadding pe = value as PropertyPadding;
                return pe.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}