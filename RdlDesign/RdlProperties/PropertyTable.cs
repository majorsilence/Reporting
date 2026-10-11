
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
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyTable - The Table Properties
    /// </summary>
    [TypeConverter(typeof(PropertyTableConverter)),
       Editor(typeof(PropertyTableUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
    internal class PropertyTable : PropertyDataRegion
    {
        public PropertyTable(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris)
            : base(d, dc, ris)
        {
        }

        [Category("Table Rows")]
        [DisplayName("Header Repeat On New Page")]
        [Description("Repeat the table header rows at the top of each page. Requires header rows; add them from the table's popup menu.")]
        public bool HeaderRepeatOnNewPage
        {
            get { return GetRepeat("Header"); }
            set { SetRepeat("Header", value); }
        }

        [Category("Table Rows")]
        [DisplayName("Footer Repeat On New Page")]
        [Description("Repeat the table footer rows at the bottom of each page. Requires footer rows; add them from the table's popup menu.")]
        public bool FooterRepeatOnNewPage
        {
            get { return GetRepeat("Footer"); }
            set { SetRepeat("Footer", value); }
        }

        private bool GetRepeat(string section)
        {
            return GetValue(section, "RepeatOnNewPage", "false").ToLower().Trim() == "true";
        }

        private void SetRepeat(string section, bool value)
        {
            // Header/Footer rows are structural; only toggle the flag when they exist.
            if (Draw.GetNamedChildNode(Nodes[0], section) == null)
                return;
            SetValue(section, "RepeatOnNewPage", value ? "true" : "false");
        }
    }
    internal class PropertyTableConverter : ExpandableObjectConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyTable)
            {
                PropertyTable pe = value as PropertyTable;
                return pe.Name;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
    internal class PropertyTableUIEditor : UITypeEditor
    {
        public PropertyTableUIEditor()
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
            PropertyReportItem pri = iri.GetPRI();

            PropertyTable pt = value as PropertyTable;
            if (pt == null)
                return base.EditValue(context, provider, value);

            //SingleCtlDialog scd = new SingleCtlDialog(pri.DesignCtl, pri.Draw, pri.Nodes, SingleCtlTypeEnum.BorderCtl, pb.Names);
            DesignCtl dc = pri.DesignCtl;
            DesignXmlDraw dp = dc.DrawCtl;
            if (dp.SelectedCount != 1)
                return base.EditValue(context, provider, value); 
            XmlNode riNode = dp.SelectedList[0];
            XmlNode table = dp.GetTableFromReportItem(riNode);
            if (table == null)
                return base.EditValue(context, provider, value);
            XmlNode tc = dp.GetTableColumn(riNode);
            XmlNode tr = dp.GetTableRow(riNode);

            List<XmlNode> ar = new List<XmlNode>();		// need to put this is a list for dialog to handle
            ar.Add(table);
            dc.UndoObject.StartUndoGroup(Strings.PropertyTableUIEditor_EditValue_TableDialog);
            using (PropertyDialog pd = new PropertyDialog(dp, ar, PropertyTypeEnum.ReportItems, tc, tr))
            {
                // Display the UI editor dialog
                DialogResult dr = editorService.ShowDialog(pd);
                dc.UndoObject.EndUndoGroup(pd.Changed || dr == DialogResult.OK);
                if (pd.Changed || dr == DialogResult.OK)
                {
                    dp.Invalidate();
                    return new PropertyTable(dp, dc, pt.Nodes);
                }

                return base.EditValue(context, provider, value);
            }
        }

    }

}
