
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Drawing.Design;
using System.Globalization;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyAction - 
    /// </summary>
    [TypeConverter(typeof(PropertyAppearanceConverter))]
    [Editor(typeof(PropertyAppearanceUIEditor), typeof(UITypeEditor))]
    internal class PropertyAppearance : IReportItem
    {
        PropertyReportItem pri;
        string[] _subitems;
        string[] _names;

        public PropertyAppearance(PropertyReportItem ri)
        {
            pri = ri;
            _names = null;
            _subitems = new string[] { "Style", "" };
        }

        public PropertyAppearance(PropertyReportItem ri, params string[] names)
        {
            pri = ri;
            _names = names;

            // now build the array used to get/set values
            if (names != null)
            {
                _subitems = new string[names.Length + 2];
                int i = 0;
                foreach (string s in names)
                    _subitems[i++] = s;

                _subitems[i++] = "Style";
            }
            else
                _subitems = new string[] { "Style", "" };
        }

        internal string[] Names
        {
            get { return _names; }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Appearance_FontFamily")]
		[LocalizedDescription("Appearance_FontFamily")]
        public PropertyExpr FontFamily
        {
            get 
            {
                return new PropertyExpr(GetStyleValue("FontFamily", "Arial")); 
            }
            set
            {
                SetStyleValue("FontFamily", value.Expression);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Appearance_FontSize")]
		[LocalizedDescription("Appearance_FontSize")]
        public PropertyExpr FontSize
        {
            get
            {
                return new PropertyExpr(GetStyleValue("FontSize", "10pt"));
            }
            set
            {
                if (!pri.IsExpression(value.Expression))
                    DesignerUtility.ValidateSize(value.Expression, true, false);
                SetStyleValue("FontSize", value.Expression);
            }
        }

        [TypeConverter(typeof(FontStyleConverter))]
		[LocalizedDisplayName("Appearance_FontStyle")]
		[LocalizedDescription("Appearance_FontStyle")]
        public string FontStyle
        {
            get
            {
                return GetStyleValue("FontStyle", "Normal");
            }
            set
            {
                SetStyleValue("FontStyle", value);
            }
        }

        [TypeConverter(typeof(FontWeightConverter))]
		[LocalizedDisplayName("Appearance_FontWeight")]
		[LocalizedDescription("Appearance_FontWeight")]
        public string FontWeight
        {
            get
            {
                return GetStyleValue("FontWeight", "Normal");
            }
            set
            {
                SetStyleValue("FontWeight", value);
            }
        }

        [TypeConverter(typeof(ColorConverter))]
		[LocalizedDisplayName("Appearance_Color")]
		[LocalizedDescription("Appearance_Color")]
        public string Color
        {
            get
            {
                return GetStyleValue("Color", "black");
            }
            set
            {
                SetStyleValue("Color", value);
            }
        }

        [TypeConverter(typeof(TextDecorationConverter))]
		[LocalizedDisplayName("Appearance_TextDecoration")]
		[LocalizedDescription("Appearance_TextDecoration")]
        public string TextDecoration
        {
            get
            {
                return GetStyleValue("TextDecoration", "None");
            }
            set
            {
                SetStyleValue("TextDecoration", value);
            }
        }

        [TypeConverter(typeof(TextAlignConverter))]
		[LocalizedDisplayName("Appearance_TextAlign")]
		[LocalizedDescription("Appearance_TextAlign")]
        public string TextAlign
        {
            get
            {
                return GetStyleValue("TextAlign", "General");
            }
            set
            {
                SetStyleValue("TextAlign", value);
            }
        }

        [TypeConverter(typeof(VerticalAlignConverter))]
		[LocalizedDisplayName("Appearance_VerticalAlign")]
		[LocalizedDescription("Appearance_VerticalAlign")]
        public string VerticalAlign
        {
            get
            {
                return GetStyleValue("VerticalAlign", "Top");
            }
            set
            {
                SetStyleValue("VerticalAlign", value);
            }
        }

        [TypeConverter(typeof(DirectionConverter))]
		[LocalizedDisplayName("Appearance_Direction")]
		[LocalizedDescription("Appearance_Direction")]
        public string Direction
        {
            get
            {
                return GetStyleValue("Direction", "LTR");
            }
            set
            {
                SetStyleValue("Direction", value);
            }
        }

        [TypeConverter(typeof(WritingModeConverter))]
		[LocalizedDisplayName("Appearance_WritingMode")]
		[LocalizedDescription("Appearance_WritingMode")]
        public string WritingMode
        {
            get
            {
                return GetStyleValue("WritingMode", "lr-tb");
            }
            set
            {
                SetStyleValue("WritingMode", value);
            }
        }

        [TypeConverter(typeof(FormatConverter))]
		[LocalizedDisplayName("Appearance_Format")]
		[LocalizedDescription("Appearance_Format")]
        public string Format
        {
            get
            {
                return GetStyleValue("Format", "");
            }
            set
            {
                SetStyleValue("Format", value);
            }
        }

        public override string ToString()
        {
            string f = GetStyleValue("FontFamily", "Arial");
            string s = GetStyleValue("FontSize", "10pt");
            string c = GetStyleValue("Color", "Black");

            return string.Format("{0}, {1}, {2}", f,s,c);
        }
        
        private string GetStyleValue(string l1, string def)
        {
            _subitems[_subitems.Length - 1] = l1;
            return pri.GetWithList(def, _subitems);
        }

        private void SetStyleValue(string l1, string val)
        {
            _subitems[_subitems.Length - 1] = l1;
            pri.SetWithList(val, _subitems);
        }

        #region IReportItem Members
        public PropertyReportItem GetPRI()
        {
            return pri;
        }
        #endregion
    }

    internal class PropertyAppearanceConverter :  ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyAppearance))
                return true;
            
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyAppearance)
            {
                PropertyAppearance pf = value as PropertyAppearance;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyAppearanceUIEditor : UITypeEditor
    {
        public PropertyAppearanceUIEditor()
        {
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context,
                                        IServiceProvider provider,
                                        object value)
        {

            if ((context == null) || (provider == null))
                return base.EditValue(context, provider, value);

            // Access the Property Browser's UI display service
            IWindowsFormsEditorService editorService =
                (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

            if (editorService == null)
                return base.EditValue(context, provider, value);

            // Create an instance of the UI editor form
            IReportItem iri = context.Instance as IReportItem;
            if (iri == null)
                return base.EditValue(context, provider, value);
            PropertyReportItem pre = iri.GetPRI();

            PropertyAppearance pf = value as PropertyAppearance;
            if (pf == null)
                return base.EditValue(context, provider, value);

            using (SingleCtlDialog scd = new SingleCtlDialog(pre.DesignCtl, pre.Draw, pre.Nodes, SingleCtlTypeEnum.FontCtl, pf.Names))
            {
                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyAppearance(pre, pf.Names);
                }
            }
            return base.EditValue(context, provider, value);
        }
    }
    #region FontStyle
    internal class FontStyleConverter : StringConverter
    {
        static readonly string[] StyleList = new string[] {"Normal","Italic"};
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit the color directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(StyleList);
        }
    }
    #endregion
    #region FontWeight
    internal class FontWeightConverter : StringConverter
    {
        static readonly string[] WeightList = new string[] { "Lighter", "Normal","Bold", "Bolder",
             "100", "200", "300", "400", "500", "600", "700", "800", "900"};

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit the color directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(WeightList);
        }
    }
    #endregion
    #region TextDecoration
    internal class TextDecorationConverter : StringConverter
    {
        static readonly string[] TDList = new string[] { "Underline", "Overline", "LineThrough", "None"};

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(TDList);
        }
    }
    #endregion
    #region TextAlign
    internal class TextAlignConverter : StringConverter
    {
        static readonly string[] TAList = new string[] { "Left", "Center", "Right", "General" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(TAList);
        }
    }
    #endregion
    #region VerticalAlign
    internal class VerticalAlignConverter : StringConverter
    {
        static readonly string[] VAList = new string[] { "Top", "Middle", "Bottom" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(VAList);
        }
    }
    #endregion
    #region Direction
    internal class DirectionConverter : StringConverter
    {
        static readonly string[] DirList = new string[] { "LTR", "RTL" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(DirList);
        }
    }
    #endregion
    #region WritingMode
    internal class WritingModeConverter : StringConverter
    {
        static readonly string[] WMList = new string[] { "lr-tb", "tb-rl", "tb-lr" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(WMList);
        }
    }
    #endregion
    #region Format
    internal class FormatConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;   // allow user to also edit directly
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(StaticLists.FormatList);
        }
    }
    #endregion
}