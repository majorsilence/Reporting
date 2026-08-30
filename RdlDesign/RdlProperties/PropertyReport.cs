using Majorsilence.Reporting.RdlDesign.Resources;

using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;
using System.Text;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyReportItem - The ReportItem Properties
    /// </summary>
    [DefaultProperty("Author")]
    internal class PropertyReport
    {
   		private DesignXmlDraw _Draw;
        private DesignCtl _DesignCtl;

        public PropertyReport(DesignXmlDraw d, DesignCtl dc)
        {
            _Draw = d;
            _DesignCtl = dc;
        }

        internal DesignXmlDraw Draw
        {
            get { return _Draw; }
        }

        internal DesignCtl DesignCtl
        {
            get { return _DesignCtl; }
        }

        XmlNode ReportNode
        {
            get { return _Draw.GetReportNode(); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Author")]
		[LocalizedDescription("Report_Author")]
        public string Author
        {
            get {return GetReportValue("Author"); }
            set{SetReportValue("Author", value); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Description")]
		[LocalizedDescription("Report_Description")]
        public string Description
        {
            get { return GetReportValue("Description"); }
            set { SetReportValue("Description", value); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Width")]
		[LocalizedDescription("Report_Width")]
        public string Width
        {
            get { return GetReportValue("Width"); }
            set
            {
                DesignerUtility.ValidateSize(value, false, false);
                SetReportValue("Width", value);
                DesignCtl.SetScrollControls();          // this will force ruler and scroll bars to be updated
            }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Parameters")]
		[LocalizedDescription("Report_Parameters")]
        public PropertyReportParameters Parameters
        {
            get { return new PropertyReportParameters(this); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Code")]
		[LocalizedDescription("Report_Code")]
        public PropertyReportCode Code
        {
            get { return new PropertyReportCode(this); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_ModulesClasses")]
		[LocalizedDescription("Report_ModulesClasses")]
        public PropertyReportModulesClasses ModulesClasses
        {
            get { return new PropertyReportModulesClasses(this); }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_PageWidth")]
		[LocalizedDescription("Report_PageWidth")]
        public string PageWidth
        {
            get { return GetReportValue("PageWidth"); }
            set
            {
                DesignerUtility.ValidateSize(value, false, false);
                SetReportValue("PageWidth", value);

                DesignCtl.SetScrollControls();          // this will force ruler and scroll bars to be updated
            }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_PageHeight")]
		[LocalizedDescription("Report_PageHeight")]
        public string PageHeight
        {
            get { return GetReportValue("PageHeight"); }
            set
            {
                DesignerUtility.ValidateSize(value, false, false);
                SetReportValue("PageHeight", value);
            }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_Margins")]
		[LocalizedDescription("Report_Margins")]
        public PropertyMargin Margins
        {
            get
            {
                return new PropertyMargin(this);
            }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_PageHeader")]
		[LocalizedDescription("Report_PageHeader")]
        public PropertyPrintFirstLast PageHeader
        {
            get
            {
                XmlNode phNode = _Draw.GetCreateNamedChildNode(ReportNode, "PageHeader");
                return new PropertyPrintFirstLast(this, phNode);
            }
        }

        [LocalizedCategory("Report")]
		[LocalizedDisplayName("Report_PageFooter")]
		[LocalizedDescription("Report_PageFooter")]
        public PropertyPrintFirstLast PageFooter
        {
            get
            {
                XmlNode phNode = _Draw.GetCreateNamedChildNode(ReportNode, "PageFooter");
                return new PropertyPrintFirstLast(this, phNode);
            }
        }

        [LocalizedCategory("Body")]
		[LocalizedDisplayName("Report_BodyHeight")]
		[LocalizedDescription("Report_BodyHeight")]
        public string BodyHeight
        {
            get
            {
                return GetBodyValue("Height", "");
            }
            set
            {
                DesignerUtility.ValidateSize(value, true, false);
                SetBodyValue("Height", value);
                DesignCtl.SetScrollControls();          // this will force ruler and scroll bars to be updated
            }
        }

        [LocalizedCategory("Body")]
		[LocalizedDisplayName("Report_BodyColumns")]
		[LocalizedDescription("Report_BodyColumns")]
        public int BodyColumns
        {
            get
            {
                string c = GetBodyValue("Columns", "1");
                try
                {
                    return Convert.ToInt32(c);
                }
                catch
                {
                    return 1;
                }
            }
            set
            {
                if (value < 1)
                    throw new ArgumentException("The number of columns in the body must be greater than 0.");
                SetBodyValue("Columns", value.ToString());
            }
        }

        [LocalizedCategory("Body")]
		[LocalizedDisplayName("Report_BodyColumnSpacing")]
		[LocalizedDescription("Report_BodyColumnSpacing")]
        public string BodyColumnSpacing
        {
            get
            {
                return GetBodyValue("ColumnSpacing", "");
            }
            set
            {
                if (value.Length == 0)
                {
                    RemoveBodyValue("ColumnSpacing");
                }
                else
                {
                    DesignerUtility.ValidateSize(value, true, false);
                    SetBodyValue("ColumnSpacing", value);
                }
            }
        }

        [LocalizedCategory("XML")]
        [Editor(typeof(FileUIEditor), typeof(UITypeEditor))]
		[LocalizedDisplayName("Report_DataTransform")]
		[LocalizedDescription("Report_DataTransform")]
		public string DataTransform
        {
            get { return GetReportValue("DataTransform"); }
            set { SetReportValue("DataTransform", value); }
        }

        [LocalizedCategory("XML")]
		[LocalizedDisplayName("Report_DataSchema")]
		[LocalizedDescription("Report_DataSchema")]
        public string DataSchema
        {
            get { return GetReportValue("DataSchema"); }
            set { SetReportValue("DataSchema", value); }
        }

        [LocalizedCategory("XML")]
		[LocalizedDisplayName("Report_DataElementName")]
		[LocalizedDescription("Report_DataElementName")]
        public string DataElementName
        {
            get { return GetReportValue("DataElementName"); }
            set { SetReportValue("DataElementName", value); }
        }

        [LocalizedCategory("XML")]
		[TypeConverter(typeof(ElementStyleConverter))]
		[LocalizedDisplayName("Report_DataElementStyle")]
		[LocalizedDescription("Report_DataElementStyle")]
        public string DataElementStyle
        {
            get { return GetReportValue("DataElementStyle", "AttributeNormal"); }
            set { SetReportValue("DataElementStyle", value); }
        }


        string GetBodyValue(string l, string def)
        {
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode bNode = _Draw.GetNamedChildNode(rNode, "Body");
            return _Draw.GetElementValue(bNode, l, def);
        }

        void SetBodyValue(string l, string v)
        {
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode bNode = _Draw.GetNamedChildNode(rNode, "Body");
            _DesignCtl.StartUndoGroup(Strings.PropertyReport_Undo_Body + " " + l + " " + Strings.PropertyReport_Undo_change);
            _Draw.SetElement(bNode, l, v);
            _DesignCtl.EndUndoGroup(true);
            _DesignCtl.SignalReportChanged();
            _Draw.Invalidate();
        }

        void RemoveBodyValue(string l)
        {
            XmlNode rNode = _Draw.GetReportNode();
            XmlNode bNode = _Draw.GetNamedChildNode(rNode, "Body");
			_DesignCtl.StartUndoGroup(Strings.PropertyReport_Undo_Body + " " + l + " " + Strings.PropertyReport_Undo_change);
            _Draw.RemoveElement(bNode, l);
            _DesignCtl.EndUndoGroup(true);
            _DesignCtl.SignalReportChanged();
            _Draw.Invalidate();
        }

        internal string GetReportValue(string l)
        {
            return GetReportValue(l, "");
        }

        internal string GetReportValue(string l, string def)
        {
            XmlNode rNode = _Draw.GetReportNode();
            return _Draw.GetElementValue(rNode, l, def);
        }

        internal void SetReportValue(string l, string v)
        {
            XmlNode rNode = _Draw.GetReportNode();
            
            _DesignCtl.StartUndoGroup(l + " " + Strings.PropertyReport_Undo_change);
            _Draw.SetElement(rNode, l, v);
            _DesignCtl.EndUndoGroup(true);

            _DesignCtl.SignalReportChanged();
            _Draw.Invalidate();
        }
    }

	#region Parameters
    [TypeConverter(typeof(PropertyReportParameterConverter)),
     Editor(typeof(PropertyReportParameterUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyReportParameters
    {
        PropertyReport pr;

        public PropertyReportParameters(PropertyReport r)
        {
            pr = r;
        }
        
        public override string ToString()
        {
            XmlNode rNode = pr.Draw.GetReportNode();
            XmlNode rpsNode = pr.Draw.GetNamedChildNode(rNode, "ReportParameters");

            string s;
            if (rpsNode == null)
                s = "No parameters defined";
            else
            {
                // show the list of parameters
                StringBuilder sb = new StringBuilder();
                foreach (XmlNode repNode in rpsNode)
                {
                    XmlAttribute nAttr = repNode.Attributes["Name"];
                    if (nAttr == null)	// shouldn't really happen
                        continue;
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(nAttr.Value);
                }
                sb.Append(" defined");
                s = sb.ToString();
            }
            return s;
        }
    }
    internal class PropertyReportParameterConverter  : StringConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyReportParameters))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyReportParameters)
            {
                PropertyReportParameters prp = value as PropertyReportParameters;
                return prp.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyReportParameterUIEditor : UITypeEditor
    {
        // Adding a parameterless constructor to resolve the MissingMethodException
        public PropertyReportParameterUIEditor()
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
            PropertyReport pr = context.Instance as PropertyReport;

            if (pr == null)
                return base.EditValue(context, provider, value);

            using (SingleCtlDialog scd = new SingleCtlDialog(pr.DesignCtl, pr.Draw, null, SingleCtlTypeEnum.ReportParameterCtl, null))
            {
                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyReportParameters(pr);
                }

                return base.EditValue(context, provider, value);
            }
        }
    }

    #endregion

    #region Code
    [TypeConverter(typeof(PropertyReportCodeConverter)),
     Editor(typeof(PropertyReportCodeUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyReportCode
    {
        PropertyReport pr;

        public PropertyReportCode(PropertyReport r)
        {
            pr = r;
        }

        public override string ToString()
        {
            XmlNode rNode = pr.Draw.GetReportNode();
            XmlNode cNode = pr.Draw.GetNamedChildNode(rNode, "Code");
            return cNode == null ? "None defined" : "Defined";
        }
    }
    internal class PropertyReportCodeConverter : StringConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyReportCode))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyReportCode)
            {
                PropertyReportCode prc = value as PropertyReportCode;
                return prc.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyReportCodeUIEditor : UITypeEditor
    {
        public PropertyReportCodeUIEditor() : base()
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
            PropertyReport pr = context.Instance as PropertyReport;

            if (pr == null)
                return base.EditValue(context, provider, value);

            using (SingleCtlDialog scd = new SingleCtlDialog(pr.DesignCtl, pr.Draw, null, SingleCtlTypeEnum.ReportCodeCtl, null))
            {
                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyReportCode(pr);
                }

                return base.EditValue(context, provider, value);
            }
        }
    }
#endregion

    #region ModulesClasses
    [TypeConverter(typeof(PropertyReportModulesClassesConverter)),
    Editor(typeof(PropertyReportModulesClassesUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyReportModulesClasses
    {
        PropertyReport pr;

        public PropertyReportModulesClasses(PropertyReport r)
        {
            pr = r;
        }

        public override string ToString()
        {
            XmlNode rNode = pr.Draw.GetReportNode();
            StringBuilder sb = new StringBuilder();

            XmlNode cmsNode = pr.Draw.GetNamedChildNode(rNode, "CodeModules");
            sb.Append(cmsNode == null? "No Modules, ": "Modules, ");
 
   			XmlNode clsNode = pr.Draw.GetNamedChildNode(rNode, "Classes");
            sb.Append(clsNode == null? "No Classes": "Classes");

            sb.Append(" defined");
            return sb.ToString();
        }
    }
    internal class PropertyReportModulesClassesConverter : StringConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyReportModulesClasses))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyReportModulesClasses)
            {
                PropertyReportModulesClasses prm = value as PropertyReportModulesClasses;
                return prm.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyReportModulesClassesUIEditor : UITypeEditor
    {
        public PropertyReportModulesClassesUIEditor() : base()
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
            PropertyReport pr = context.Instance as PropertyReport;

            if (pr == null)
                return base.EditValue(context, provider, value);

            using (SingleCtlDialog scd = new SingleCtlDialog(pr.DesignCtl, pr.Draw, null, SingleCtlTypeEnum.ReportModulesClassesCtl, null))
            {
                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyReportModulesClasses(pr);
                }

                return base.EditValue(context, provider, value);
            }
        }
    }
#endregion

    #region XSLFile
    internal class FileUIEditor : UITypeEditor
    {
        public FileUIEditor()
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
            PropertyReport pr = context.Instance as PropertyReport;

            if (pr == null)
                return base.EditValue(context, provider, value);

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = Strings.FileUIEditor_EditValue_XSLFilesFilter;
                ofd.FilterIndex = 0;
                ofd.CheckFileExists = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return ofd.FileName;
                }

                return base.EditValue(context, provider, value);
            }
        }
    }
    #endregion

    #region ElementStyle
    internal class ElementStyleConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;   
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new string[] {
                                    "AttributeNormal", "ElementNormal"});
        }
    }
    #endregion
}
