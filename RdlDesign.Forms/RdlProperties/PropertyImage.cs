using Majorsilence.Reporting.Rdl;

using System;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Drawing.Design;
using System.Globalization;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyImage - The Image specific Properties
    /// </summary>
    internal class PropertyImage : PropertyReportItem
    {
        internal PropertyImage(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris) : base(d, dc, ris)
        {

        }

        [LocalizedCategory("Image")]
		[LocalizedDisplayName("Image_Image")]
		[LocalizedDescription("Image_Image")]
        public PropertyImageI Image
        {
            get { return new PropertyImageI(this); }

        }
    }

    [TypeConverter(typeof(PropertyImageConverter))]
	[Editor(typeof(PropertyImageUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyImageI : IReportItem
    {
        PropertyImage _pi;

        public PropertyImageI(PropertyImage pi)
        {
            _pi = pi;
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ImageSourceConverter))]
		[LocalizedDisplayName("ImageI_Source")]
		[LocalizedDescription("ImageI_Source")]
        public string Source
        {
            get
            {
                return _pi.GetValue("Source", "External");
            }
            set
            {
                _pi.SetValue("Source", value);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("ImageI_Value")]
		[LocalizedDescription("ImageI_Value")]
        public PropertyExpr Value
        {
            get
            {
                return new PropertyExpr(_pi.GetValue("Value", ""));
            }
            set
            {
                _pi.SetValue("Value", value.Expression);
            }
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[TypeConverter(typeof(ImageMIMETypeConverter))]
		[LocalizedDisplayName("ImageI_MIMEType")]
		[LocalizedDescription("ImageI_MIMEType")]
        public string MIMEType
        {
            get
            {
                return _pi.GetValue("MIMEType", "");
            }
            set
            {
                if (string.Compare(this.Source.Trim(), "database", true) == 0)
                    throw new ArgumentException("MIMEType isn't relevent when Source isn't Database.");
                _pi.SetValue("MIMEType", value);
            }
        }

		[LocalizedDisplayName("ImageI_Sizing")]
		[LocalizedDescription("ImageI_Sizing")]
        public ImageSizingEnum Sizing
        {
            get
            {
                string s = _pi.GetValue("Sizing", "AutoSize");
                return ImageSizing.GetStyle(s);
            }
            set
            {
                _pi.SetValue("Sizing", value.ToString());
            }
        }

        public override string ToString()
        {
            string s = this.Source;
            string v = "";
            if (s.ToLower().Trim() != "none")
                v = this.Value.Expression;

            return string.Format("{0} {1}", s, v);
        }


        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pi;
        }

        #endregion
    }

    #region ImageConverter
    internal class PropertyImageConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyImageI))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyImage)
            {
                PropertyImageI pi = value as PropertyImageI;
                return pi.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
#endregion

    #region UIEditor  
    internal class PropertyImageUIEditor : UITypeEditor
    {
        public PropertyImageUIEditor()
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
            PropertyImage pre = iri.GetPRI() as PropertyImage;

            PropertyImageI pbi = value as PropertyImageI;
            if (pbi == null)
                 return base.EditValue(context, provider, value);

             using (SingleCtlDialog scd = new SingleCtlDialog(pre.DesignCtl, pre.Draw, pre.Nodes,
                 SingleCtlTypeEnum.ImageCtl, null))
             {
                 ///////
                 // Display the UI editor dialog
                 if (editorService.ShowDialog(scd) == DialogResult.OK)
                 {
                     // Return the new property value from the UI editor form
                     return new PropertyImageI(pre);
                 }

                 return base.EditValue(context, provider, value);
             }
        }
    }
    #endregion
}
