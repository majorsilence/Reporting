
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
	internal enum SingleCtlTypeEnum
	{
		InteractivityCtl, VisibilityCtl, BorderCtl, FontCtl, BackgroundCtl, BackgroundImage,
		ReportParameterCtl, ReportCodeCtl, ReportModulesClassesCtl, ImageCtl, SubreportCtl,
		FiltersCtl, SortingCtl, GroupingCtl
	}

    /// <summary>
    /// Summary description for PropertyDialog.
    /// </summary>
    internal partial class SingleCtlDialog
    {

        // design draw 
        private List<XmlNode> _Nodes;			// selected nodes
        private SingleCtlTypeEnum _Type;
        IProperty _Ctl;

        internal SingleCtlDialog(DesignCtl dc, DesignXmlDraw dxDraw, List<XmlNode> sNodes,
            SingleCtlTypeEnum type, string[] names)
        {
            this._Type = type;
            this._DesignCtl = dc;
            this._Draw = dxDraw;
            this._Nodes = sNodes;

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //   Add the control for the selected ReportItems
            //     We could have forced the user to create this (and maybe should have) 
            //     instead of using an enum.
            UserControl uc = null;
            string title = null;
            switch (type)
            {
                case SingleCtlTypeEnum.InteractivityCtl:
                    title = Strings.Tabs_Interactivity;
                    uc = new InteractivityCtl(dxDraw, sNodes);
                    break;
                case SingleCtlTypeEnum.VisibilityCtl:
                    title = Strings.Tabs_Visibility;
                    uc = new VisibilityCtl(dxDraw, sNodes);
                    break;
                case SingleCtlTypeEnum.BorderCtl:
                    title = Strings.Tabs_Borders;
                    uc = new StyleBorderCtl(dxDraw, names, sNodes);
                    break;
                case SingleCtlTypeEnum.FontCtl:
                    title = Strings.Tabs_Font;
                    uc = new FontCtl(dxDraw, names, sNodes);
                    break;
                case SingleCtlTypeEnum.BackgroundCtl:
                    title = Strings.Tabs_Background;
                    uc = new BackgroundCtl(dxDraw, names, sNodes);
                    break;
                case SingleCtlTypeEnum.ImageCtl:
                    title = Strings.Tabs_Image;
                    uc = new ImageCtl(dxDraw, sNodes);
                    break;
                case SingleCtlTypeEnum.SubreportCtl:
                    title = Strings.Tabs_Subreport;
                    uc = new SubreportCtl(dxDraw, sNodes[0]);
                    break;
                case SingleCtlTypeEnum.FiltersCtl:
                    title = Strings.Tabs_Filter;
                    uc = new FiltersCtl(dxDraw, sNodes[0]);
                    break;
                case SingleCtlTypeEnum.SortingCtl:
                    title = Strings.Tabs_Sorting;
                    uc = new SortingCtl(dxDraw, sNodes[0]);
                    break;
                case SingleCtlTypeEnum.GroupingCtl:
                    title = Strings.Tabs_Grouping;
                    uc = new GroupingCtl(dxDraw, sNodes[0]);
                    break;
                case SingleCtlTypeEnum.ReportParameterCtl:
                    title = Strings.Tabs_ReportParameters;
                    uc = new ReportParameterCtl(dxDraw);
                    break;
                case SingleCtlTypeEnum.ReportCodeCtl:
                    title = Strings.Tabs_Code;
                    uc = new CodeCtl(dxDraw);
                    break;
                case SingleCtlTypeEnum.ReportModulesClassesCtl:
                    title = Strings.Tabs_ModulesAndClasses;
                    uc = new ModulesClassesCtl(dxDraw);
                    break;
            }
            _Ctl = uc as IProperty;
            if (title != null)
                Text = Text + " - " + title;

            if (uc == null)
                return;
            int h = uc.Height;
            int w = uc.Width;
            uc.Top = 0;
            uc.Left = 0;
            uc.Dock = DockStyle.Fill;
            uc.Parent = this.pMain;
            this.Height = h + (this.Height - pMain.Height);
            this.Width = w + (this.Width - pMain.Width);
            this.ResumeLayout(true);
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            string c = "";
            switch (_Type)
            {
                case SingleCtlTypeEnum.InteractivityCtl:
                    c = Strings.SingleCtlDialog_Undo_InteractivityChange;
                    break;
                case SingleCtlTypeEnum.VisibilityCtl:
                    c = Strings.SingleCtlDialog_Undo_VisibilityChange;
                    break;
                case SingleCtlTypeEnum.BorderCtl:
                    c = Strings.SingleCtlDialog_Undo_BorderChange;
                    break;
                case SingleCtlTypeEnum.FontCtl:
                    c = Strings.SingleCtlDialog_Undo_AppearanceChange;
                    break;
                case SingleCtlTypeEnum.BackgroundCtl:
                case SingleCtlTypeEnum.BackgroundImage:
                    c = Strings.SingleCtlDialog_Undo_BackgroundChange;
                    break;
                case SingleCtlTypeEnum.FiltersCtl:
                    c = Strings.SingleCtlDialog_Undo_FiltersChange;
                    break;
                case SingleCtlTypeEnum.SortingCtl:
                    c = Strings.SingleCtlDialog_Undo_SortChange;
                    break;
                case SingleCtlTypeEnum.GroupingCtl:
                    c = Strings.SingleCtlDialog_Undo_GroupingChange;
                    break;
                case SingleCtlTypeEnum.ReportCodeCtl:
                    c = Strings.SingleCtlDialog_Undo_ReportCodeChange;
                    break;
                case SingleCtlTypeEnum.ImageCtl:
                    c = Strings.SingleCtlDialog_Undo_ImageChange;
                    break;
                case SingleCtlTypeEnum.SubreportCtl:
                    c = Strings.SingleCtlDialog_Undo_SubreportChange;
                    break;
                case SingleCtlTypeEnum.ReportModulesClassesCtl:
                    c = Strings.SingleCtlDialog_Undo_ReportModules_ClassesChange;
                    break;
            }
            this._DesignCtl.StartUndoGroup(c);

            this._Ctl.Apply();

            this._DesignCtl.EndUndoGroup(true);
            _DesignCtl.SignalReportChanged();
            _Draw.Invalidate();

            this.DialogResult = DialogResult.OK;
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }


    }
}
