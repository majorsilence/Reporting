
using System;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyAction - 
    /// </summary>
    [TypeConverter(typeof(PropertyBackgroundConverter))]
    [Editor(typeof(PropertyBackgroundUIEditor), typeof(UITypeEditor))]
    internal class PropertyBackground : IReportItem
    {
        PropertyReportItem pri;
        string[] _names;
        string[] _subitems;

        public PropertyBackground(PropertyReportItem ri)
        {
            pri = ri;
            _names = null;
            _subitems = new string[] { "Style", "" };
        }

        public PropertyBackground(PropertyReportItem ri, params string[] names)
        {
            pri = ri;
            _names = names;

            // now build the array used to get/set values
            _subitems = new string[names.Length + 2];
            int i = 0;
            foreach (string s in names)
                _subitems[i++] = s;

            _subitems[i++] = "Style";
        }

        internal PropertyReportItem RI
        {
            get { return pri; }
        }

        internal string[] Names
        {
            get { return _names; }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ColorConverter))]
		[LocalizedDisplayName("Background_Color")]
		[LocalizedDescription("Background_Color")]
        public string Color
        {
            get 
            {
                return GetStyleValue("BackgroundColor", ""); 
            }
            set
            {
                SetStyleValue("BackgroundColor", value);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ColorConverter))]
		[LocalizedDisplayName("Background_EndColor")]
		[LocalizedDescription("Background_EndColor")]
        public string EndColor
        {
            get
            {
                return GetStyleValue("BackgroundGradientEndColor", "");
            }
            set
            {
                SetStyleValue("BackgroundGradientEndColor", value);
            }
        }


        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(GradientTypeConverter))]
		[LocalizedDisplayName("Background_GradientType")]
		[LocalizedDescription("Background_GradientType")]
        public string GradientType
        {
            get
            {
                return GetStyleValue("BackgroundGradientType", "None");
            }
            set
            {
                SetStyleValue("BackgroundGradientType", value);
            }
        }

		[LocalizedDisplayName("Background_Image")]
		[LocalizedDescription("Background_Image")]
        public PropertyBackgroundImage Image
        {
            get
            {
                return new PropertyBackgroundImage(pri, _names);
            }
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

        public override string ToString()
        {
            return GetStyleValue("BackgroundColor", ""); 
        }

        #region IReportItem Members
        public PropertyReportItem GetPRI()
        {
            return pri;
        }
        #endregion
    }

    internal class PropertyBackgroundConverter :  ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyBackground))
                return true;
            
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyBackground)
            {
                PropertyBackground pf = value as PropertyBackground;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyBackgroundUIEditor : UITypeEditor
    {
        public PropertyBackgroundUIEditor()
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

            string[] names;
            PropertyBackground pb = value as PropertyBackground;
            if (pb != null)
                names = pb.Names;
            else
            {
                PropertyBackgroundImage pbi = value as PropertyBackgroundImage;
                if (pbi == null)
                    return base.EditValue(context, provider, value);
                names = pbi.Names;
            }

            using (SingleCtlDialog scd = new SingleCtlDialog(pre.DesignCtl, pre.Draw, pre.Nodes,
                SingleCtlTypeEnum.BackgroundCtl, names))
            {

                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyBackground(pre);
                }

                return base.EditValue(context, provider, value);
            }
        }
    }

    #region GradientType
    internal class GradientTypeConverter : StringConverter
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
            return new StandardValuesCollection(StaticLists.GradientList);
        }
    }
    #endregion

    [TypeConverter(typeof(PropertyBackgroundImageConverter))]
    [Editor(typeof(PropertyBackgroundUIEditor), typeof(UITypeEditor))]
    internal class PropertyBackgroundImage : IReportItem
    {
        PropertyReportItem pri;
        string[] _names;
        string[] _subitems;

        public PropertyBackgroundImage(PropertyReportItem ri, string[] names)
        {
            pri = ri;
            _names = names;
            if (names == null)
            {
                _subitems = new string[] { "Style", "BackgroundImage", "" };
            }
            else
            {
                // now build the array used to get/set values
                _subitems = new string[names.Length + 3];
                int i = 0;
                foreach (string s in names)
                    _subitems[i++] = s;

                _subitems[i++] = "Style";
                _subitems[i++] = "BackgroundImage";
            }
        }

        internal string[] Names
        {
            get { return _names; }
        }

        public override string ToString()
        {
            string s = this.Source;
            string v = "";
            if (s.ToLower().Trim() != "none")
                v = this.Value.Expression;

            return string.Format("{0} {1}", s, v);
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ImageSourceConverter))]
		[LocalizedDisplayName("BackgroundImage_Source")]
		[LocalizedDescription("BackgroundImage_Source")]
        public string Source
        {
            get
            {
                _subitems[_subitems.Length-1] = "Source";
                return pri.GetWithList("None", _subitems);
            }
            set
            {
                if (value.ToLower().Trim() == "none")
                {
                    List<string> l = new List<string>(_subitems);
                    l.RemoveAt(l.Count - 1);
                    pri.RemoveWithList(l.ToArray());
                }
                else
                {
                    _subitems[_subitems.Length - 1] = "Source";
                    pri.SetWithList(value, _subitems);
                }
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("BackgroundImage_Value")]
		[LocalizedDescription("BackgroundImage_Value")]
        public PropertyExpr Value
        {
            get
            {
                _subitems[_subitems.Length - 1] = "Value";
                return new PropertyExpr(pri.GetWithList("", _subitems));
            }
            set
            {
                if (this.Source.ToLower().Trim() == "none")
                    throw new ArgumentException("Value isn't relevent when Source=None.");
                _subitems[_subitems.Length - 1] = "Value";
                pri.SetWithList(value.Expression, _subitems);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ImageMIMETypeConverter))]
		[LocalizedDisplayName("BackgroundImage_MIMEType")]
		[LocalizedDescription("BackgroundImage_MIMEType")]
        public string MIMEType
        {
            get
            {
                _subitems[_subitems.Length - 1] = "MIMEType";
                return pri.GetWithList("", _subitems);
            }
            set
            {
                if (this.Source.ToLower().Trim() != "database")
                    throw new ArgumentException("MIMEType isn't relevent when Source isn't Database.");
                _subitems[_subitems.Length - 1] = "MIMEType";
                pri.SetWithList(value, _subitems);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ImageRepeatConverter))]
		[LocalizedDisplayName("BackgroundImage_Repeat")]
		[LocalizedDescription("BackgroundImage_Repeat")]
        public string Repeat
        {
            get
            {
                _subitems[_subitems.Length - 1] = "BackgroundRepeat";
                return pri.GetWithList("Repeat", _subitems);
            }
            set
            {
                if (this.Source.ToLower().Trim() == "none")
                    throw new ArgumentException("Repeat isn't relevent when Source=None.");
                _subitems[_subitems.Length - 1] = "BackgroundRepeat";
                pri.SetWithList(value, _subitems);
            }
        }


        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return pri;
        }

        #endregion
    }

    internal class PropertyBackgroundImageConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyBackgroundImage))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyBackgroundImage)
            {
                PropertyBackgroundImage pf = value as PropertyBackgroundImage;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }


	#region ImageSource
    internal class ImageSourceConverter : StringConverter
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
            if (context.Instance is PropertyImageI)
                return new StandardValuesCollection(new string[] {
                                    "External", "Embedded", "Database"});
            else
                return new StandardValuesCollection(new string[] {
                                    "None", "External", "Embedded", "Database"});
        }
    }
    #endregion

    #region ImageMIMEType
    internal class ImageMIMETypeConverter : StringConverter
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
            return new StandardValuesCollection(new string[] {
                                    "image/bmp", "image/jpeg", "image/gif", "image/png","image/x-png"});
        }
    }
#endregion

    #region ImageRepeat
    internal class ImageRepeatConverter : StringConverter
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
            return new StandardValuesCollection(new string[] {
                                    "Repeat", "NoRepeat", "RepeatX", "RepeatY"});
        }
    }
#endregion
}