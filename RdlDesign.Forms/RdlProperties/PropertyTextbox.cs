using Majorsilence.Reporting.Rdl;

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyTextbox - The Textbox Properties
    /// </summary>
    [DefaultProperty("Value")]
    internal class PropertyTextbox : PropertyReportItem
    {
        public PropertyTextbox(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris) : base(d, dc, ris)
        {
        }

        [LocalizedCategory("Textbox")]
		[LocalizedDisplayName("Textbox_Value")]
		[LocalizedDescription("Textbox_Value")]
        public PropertyExpr Value
        {
            get { return new PropertyExpr(this.GetValue("Value", "")); }
            set
            {
                this.SetValue("Value", value.Expression);
            }
        }

        [LocalizedCategory("Style")]
		[LocalizedDisplayName("Textbox_Appearance")]
		[LocalizedDescription("Textbox_Appearance")]
        public PropertyAppearance Appearance
        {
            get { return new PropertyAppearance(this); }
        }

        [LocalizedCategory("Textbox")]
		[LocalizedDisplayName("Textbox_CanGrow")]
		[LocalizedDescription("Textbox_CanGrow")]
        public bool CanGrow
        {
            get { return this.GetValue("CanGrow", "false").ToLower() == "true"; }
            set
            {
                this.SetValue("CanGrow", value? "true": "false");
            }
        }

        [LocalizedCategory("Textbox")]
		[LocalizedDisplayName("Textbox_CanShrink")]
		[LocalizedDescription("Textbox_CanShrink")]
        public bool CanShrink
        {
            get { return this.GetValue("CanShrink", "false").ToLower() == "true"; }
            set
            {
                this.SetValue("CanShrink", value ? "true" : "false");
            }
        }

        [LocalizedCategory("Textbox")]
		[LocalizedDisplayName("Textbox_HideDuplicates")]
		[LocalizedDescription("Textbox_HideDuplicates")]
		[TypeConverter(typeof(HideDuplicatesConverter))]
        public string HideDuplicates
        {
            get { return this.GetValue("HideDuplicates", ""); }
            set
            {
                if (value == "")
                    this.RemoveValue("HideDuplicates");
                else
                    this.SetValue("HideDuplicates", value);
            }
        }

        [LocalizedCategory("XML")]
		[LocalizedDisplayName("Textbox_DataElementStyle")]
		[LocalizedDescription("Textbox_DataElementStyle")]
        public DataElementStyleEnum DataElementStyle
        {
            get
            {
                string v = GetValue("DataElementStyle", "Auto");
                return Majorsilence.Reporting.Rdl.DataElementStyle.GetStyle(v);
            }
            set
            {
                SetValue("DataElementStyle", value.ToString());
            }
        }

        internal class HideDuplicatesConverter : StringConverter
        {

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            {
                // returning false here means the property will
                // have a drop down and a value that can be manually
                // entered.      
                return false;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                if (context == null)
                    return base.GetStandardValues(context);

                PropertyTextbox pt = context.Instance as PropertyTextbox;
                if (pt == null)
                    return base.GetStandardValues(context);

                // Populate with the list of datasets and group names
                ArrayList ar = new ArrayList();
                ar.Add("");         // add an empty string to the collection
                object[] dsn = pt.Draw.DataSetNames;
                if (dsn != null)
                    ar.AddRange(dsn);
                object[] grps = pt.Draw.GroupingNames;
                if (grps != null)
                    ar.AddRange(grps);

                StandardValuesCollection svc = new StandardValuesCollection(ar);
                
                return svc;
            }
        }

    }
}
