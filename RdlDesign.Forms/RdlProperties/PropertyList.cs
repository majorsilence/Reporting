using Majorsilence.Reporting.Rdl;

using System;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyList - The List Properties
    /// </summary>
    [TypeConverter(typeof(PropertyListConverter))]
    internal class PropertyList : PropertyDataRegion
    {
        public PropertyList(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris)
            : base(d, dc, ris)
        {
        }

        [LocalizedCategory("List")]
		[LocalizedDisplayName("List_Grouping")]
		[LocalizedDescription("List_Grouping")]
        public PropertyGrouping Grouping
        {
            get
            {
                return new PropertyGrouping(this);
            }
        }

		[LocalizedCategory("List")]
		[LocalizedDisplayName("List_Sorting")]
		[LocalizedDescription("List_Sorting")]
        public PropertySorting Sorting
        {
            get
            {
                return new PropertySorting(this);
            }
        }

        #region XML
        [LocalizedCategory("XML")]
		[LocalizedDisplayName("List_DataInstanceName")]
		[LocalizedDescription("List_DataInstanceName")]
        public string DataInstanceName
        {
            get
            {
                return GetValue("DataInstanceName", "");
            }
            set
            {
                SetValue("DataInstanceName", value);
            }
        }

        [LocalizedCategory("XML")]
		[LocalizedDisplayName("List_DataInstanceElementOutput")]
		[LocalizedDescription("List_DataInstanceElementOutput")]
        public DataInstanceElementOutputEnum DataInstanceElementOutput
        {
            get
            {
                string v = GetValue("DataInstanceElementOutput", "Output");
                return Majorsilence.Reporting.Rdl.DataInstanceElementOutput.GetStyle(v);
            }
            set
            {
                SetValue("DataInstanceElementOutput", value.ToString());
            }
        }
        #endregion
    }

    internal class PropertyListConverter : ExpandableObjectConverter
    {
        public PropertyListConverter(){}
        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyList)
            {
                PropertyList pe = value as PropertyList;
                return pe.Name;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

}
