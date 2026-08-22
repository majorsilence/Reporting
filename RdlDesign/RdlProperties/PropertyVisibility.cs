
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;            // need this for the properties metadata
using System.Xml;
using System.Globalization;
using Majorsilence.Forms;
using Majorsilence.Forms.Design;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyAction - 
    /// </summary>
    [TypeConverter(typeof(PropertyVisibilityConverter)),
       Editor(typeof(PropertyVisibilityUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyVisibility:IReportItem
    {
        PropertyReportItem pri;

        public PropertyVisibility(PropertyReportItem ri)
        {
            pri = ri;
        }
        
        public override string ToString()
        {
            string result = "";
            DesignXmlDraw dr = pri.Draw;

            XmlNode visNode = dr.GetNamedChildNode(pri.Node, "Visibility");
            if (visNode != null)
            {
                XmlNode hNode = dr.GetNamedChildNode(pri.Node, "Visibility");
                XmlNode vNode = dr.GetNamedChildNode(hNode, "Hidden");
                if (vNode != null)
                    result = string.Format("Hidden: {0}", vNode.InnerText);
            }
            return result;
        }

        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this.pri;
        }

        #endregion
    }

    internal class PropertyVisibilityConverter : StringConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyVisibility))
                return true;
            
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyVisibility)
            {
                PropertyVisibility pv = value as PropertyVisibility;
                return pv.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    internal class PropertyVisibilityUIEditor : UITypeEditor
    {
        public PropertyVisibilityUIEditor()
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

            PropertyVisibility pv = value as PropertyVisibility;
            if (pv == null)
                return base.EditValue(context, provider, value);

            using (SingleCtlDialog scd = new SingleCtlDialog(pre.DesignCtl, pre.Draw, pre.Nodes, SingleCtlTypeEnum.VisibilityCtl, null))
            {
                // Display the UI editor dialog
                if (editorService.ShowDialog(scd) == DialogResult.OK)
                {
                    // Return the new property value from the UI editor form
                    return new PropertyAction(pre);
                }

                return base.EditValue(context, provider, value);
            }
        }
    }
}