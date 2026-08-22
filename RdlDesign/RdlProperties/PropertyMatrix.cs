
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyMatrix - The Table Properties
    /// </summary>
    [TypeConverter(typeof(PropertyMatrixConverter))]
    internal class PropertyMatrix : PropertyDataRegion
    {
        public PropertyMatrix(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris)
            : base(d, dc, ris)
        {
        }
    }
    internal class PropertyMatrixConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyMatrix)
            {
                PropertyMatrix pe = value as PropertyMatrix;
                return pe.Name;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

}
