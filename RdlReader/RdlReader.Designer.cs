using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlReader
{
	public partial class RdlReader : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private System.ComponentModel.Container components = null;
private MDIChild printChild=null;


		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(RdlReader));
			this.menuStrip1 = new Majorsilence.Forms.MenuStrip();
			this.fileToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.closeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new Majorsilence.Forms.ToolStripSeparator();
			this.saveAsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.printToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new Majorsilence.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new Majorsilence.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.selectionToolToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new Majorsilence.Forms.ToolStripSeparator();
			this.copyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new Majorsilence.Forms.ToolStripSeparator();
			this.findToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.zoomToToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.actualSizeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.fitPageToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.fitWidthToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new Majorsilence.Forms.ToolStripSeparator();
			this.pageLayoutToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.singlePageToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.continuousToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.facingToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.continuousFacingToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.windowToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.cascadeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.tileToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.horizontallyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.verticallyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.closeAllToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStrip1 = new Majorsilence.Forms.ToolStrip();
			this.toolStripButtonOpen = new Majorsilence.Forms.ToolStripButton();
			this.toolStripButtonSave = new Majorsilence.Forms.ToolStripButton();
			this.toolStripButtonPrint = new Majorsilence.Forms.ToolStripButton();
			this.menuStrip1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			resources.ApplyResources(this.menuStrip1, "menuStrip1");
			this.menuStrip1.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip1.MdiWindowListItem = this.windowToolStripMenuItem;
			this.menuStrip1.Name = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
			this.fileToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveAsToolStripMenuItem,
            this.printToolStripMenuItem,
            this.toolStripSeparator2,
            this.recentFilesToolStripMenuItem,
            this.toolStripSeparator3,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			// 
			// openToolStripMenuItem
			// 
			resources.ApplyResources(this.openToolStripMenuItem, "openToolStripMenuItem");
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Click += this.menuFileOpen_Click;
			// 
			// closeToolStripMenuItem
			// 
			resources.ApplyResources(this.closeToolStripMenuItem, "closeToolStripMenuItem");
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.Click += this.menuFileClose_Click;
			// 
			// toolStripSeparator1
			// 
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			// 
			// saveAsToolStripMenuItem
			// 
			resources.ApplyResources(this.saveAsToolStripMenuItem, "saveAsToolStripMenuItem");
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Click += this.menuFileSaveAs_Click;
			// 
			// printToolStripMenuItem
			// 
			resources.ApplyResources(this.printToolStripMenuItem, "printToolStripMenuItem");
			this.printToolStripMenuItem.Name = "printToolStripMenuItem";
			this.printToolStripMenuItem.Click += this.menuFilePrint_Click;
			// 
			// toolStripSeparator2
			// 
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			// 
			// recentFilesToolStripMenuItem
			// 
			resources.ApplyResources(this.recentFilesToolStripMenuItem, "recentFilesToolStripMenuItem");
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			// 
			// toolStripSeparator3
			// 
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			// 
			// exitToolStripMenuItem
			// 
			resources.ApplyResources(this.exitToolStripMenuItem, "exitToolStripMenuItem");
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Click += this.menuFileExit_Click;
			// 
			// editToolStripMenuItem
			// 
			resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
			this.editToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.selectionToolToolStripMenuItem,
            this.toolStripSeparator4,
            this.copyToolStripMenuItem,
            this.toolStripSeparator5,
            this.findToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			// 
			// selectionToolToolStripMenuItem
			// 
			resources.ApplyResources(this.selectionToolToolStripMenuItem, "selectionToolToolStripMenuItem");
			this.selectionToolToolStripMenuItem.Name = "selectionToolToolStripMenuItem";
			this.selectionToolToolStripMenuItem.Click += this.menuSelection_Click;
			// 
			// toolStripSeparator4
			// 
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			// 
			// copyToolStripMenuItem
			// 
			resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Click += this.menuCopy_Click;
			// 
			// toolStripSeparator5
			// 
			resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			// 
			// findToolStripMenuItem
			// 
			resources.ApplyResources(this.findToolStripMenuItem, "findToolStripMenuItem");
			this.findToolStripMenuItem.Name = "findToolStripMenuItem";
			this.findToolStripMenuItem.Click += this.menuFind_Click;
			// 
			// viewToolStripMenuItem
			// 
			resources.ApplyResources(this.viewToolStripMenuItem, "viewToolStripMenuItem");
			this.viewToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.zoomToToolStripMenuItem,
            this.actualSizeToolStripMenuItem,
            this.fitPageToolStripMenuItem,
            this.fitWidthToolStripMenuItem,
            this.toolStripSeparator6,
            this.pageLayoutToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			// 
			// zoomToToolStripMenuItem
			// 
			resources.ApplyResources(this.zoomToToolStripMenuItem, "zoomToToolStripMenuItem");
			this.zoomToToolStripMenuItem.Name = "zoomToToolStripMenuItem";
			this.zoomToToolStripMenuItem.Click += this.menuPLZoomTo_Click;
			// 
			// actualSizeToolStripMenuItem
			// 
			resources.ApplyResources(this.actualSizeToolStripMenuItem, "actualSizeToolStripMenuItem");
			this.actualSizeToolStripMenuItem.Name = "actualSizeToolStripMenuItem";
			this.actualSizeToolStripMenuItem.Click += this.menuPLActualSize_Click;
			// 
			// fitPageToolStripMenuItem
			// 
			resources.ApplyResources(this.fitPageToolStripMenuItem, "fitPageToolStripMenuItem");
			this.fitPageToolStripMenuItem.Name = "fitPageToolStripMenuItem";
			this.fitPageToolStripMenuItem.Click += this.menuPLFitPage_Click;
			// 
			// fitWidthToolStripMenuItem
			// 
			resources.ApplyResources(this.fitWidthToolStripMenuItem, "fitWidthToolStripMenuItem");
			this.fitWidthToolStripMenuItem.Name = "fitWidthToolStripMenuItem";
			this.fitWidthToolStripMenuItem.Click += this.menuPLFitWidth_Click;
			// 
			// toolStripSeparator6
			// 
			resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			// 
			// pageLayoutToolStripMenuItem
			// 
			resources.ApplyResources(this.pageLayoutToolStripMenuItem, "pageLayoutToolStripMenuItem");
			this.pageLayoutToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.singlePageToolStripMenuItem,
            this.continuousToolStripMenuItem,
            this.facingToolStripMenuItem,
            this.continuousFacingToolStripMenuItem});
			this.pageLayoutToolStripMenuItem.Name = "pageLayoutToolStripMenuItem";
			// 
			// singlePageToolStripMenuItem
			// 
			resources.ApplyResources(this.singlePageToolStripMenuItem, "singlePageToolStripMenuItem");
			this.singlePageToolStripMenuItem.Name = "singlePageToolStripMenuItem";
			this.singlePageToolStripMenuItem.Click += this.menuPLSinglePage_Click;
			// 
			// continuousToolStripMenuItem
			// 
			resources.ApplyResources(this.continuousToolStripMenuItem, "continuousToolStripMenuItem");
			this.continuousToolStripMenuItem.Name = "continuousToolStripMenuItem";
			this.continuousToolStripMenuItem.Click += this.menuPLContinuous_Click;
			// 
			// facingToolStripMenuItem
			// 
			resources.ApplyResources(this.facingToolStripMenuItem, "facingToolStripMenuItem");
			this.facingToolStripMenuItem.Name = "facingToolStripMenuItem";
			this.facingToolStripMenuItem.Click += this.menuPLFacing_Click;
			// 
			// continuousFacingToolStripMenuItem
			// 
			resources.ApplyResources(this.continuousFacingToolStripMenuItem, "continuousFacingToolStripMenuItem");
			this.continuousFacingToolStripMenuItem.Name = "continuousFacingToolStripMenuItem";
			this.continuousFacingToolStripMenuItem.Click += this.menuPLContinuousFacing_Click;
			// 
			// windowToolStripMenuItem
			// 
			resources.ApplyResources(this.windowToolStripMenuItem, "windowToolStripMenuItem");
			this.windowToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.cascadeToolStripMenuItem,
            this.tileToolStripMenuItem,
            this.closeAllToolStripMenuItem});
			this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
			// 
			// cascadeToolStripMenuItem
			// 
			resources.ApplyResources(this.cascadeToolStripMenuItem, "cascadeToolStripMenuItem");
			this.cascadeToolStripMenuItem.Name = "cascadeToolStripMenuItem";
			this.cascadeToolStripMenuItem.Click += this.menuWndCascade_Click;
			// 
			// tileToolStripMenuItem
			// 
			resources.ApplyResources(this.tileToolStripMenuItem, "tileToolStripMenuItem");
			this.tileToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.horizontallyToolStripMenuItem,
            this.verticallyToolStripMenuItem});
			this.tileToolStripMenuItem.Name = "tileToolStripMenuItem";
			// 
			// horizontallyToolStripMenuItem
			// 
			resources.ApplyResources(this.horizontallyToolStripMenuItem, "horizontallyToolStripMenuItem");
			this.horizontallyToolStripMenuItem.Name = "horizontallyToolStripMenuItem";
			this.horizontallyToolStripMenuItem.Click += this.menuWndTileH_Click;
			// 
			// verticallyToolStripMenuItem
			// 
			resources.ApplyResources(this.verticallyToolStripMenuItem, "verticallyToolStripMenuItem");
			this.verticallyToolStripMenuItem.Name = "verticallyToolStripMenuItem";
			this.verticallyToolStripMenuItem.Click += this.menuWndTileV_Click;
			// 
			// closeAllToolStripMenuItem
			// 
			resources.ApplyResources(this.closeAllToolStripMenuItem, "closeAllToolStripMenuItem");
			this.closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
			this.closeAllToolStripMenuItem.Click += this.menuWndCloseAll_Click;
			// 
			// helpToolStripMenuItem
			// 
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			this.helpToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			// 
			// aboutToolStripMenuItem
			// 
			resources.ApplyResources(this.aboutToolStripMenuItem, "aboutToolStripMenuItem");
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Click += this.menuHelpAbout_Click;
			// 
			// toolStrip1
			// 
			resources.ApplyResources(this.toolStrip1, "toolStrip1");
			this.toolStrip1.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.toolStripButtonOpen,
            this.toolStripButtonSave,
            this.toolStripButtonPrint});
			this.toolStrip1.Name = "toolStrip1";
			// 
			// toolStripButtonOpen
			// 
			resources.ApplyResources(this.toolStripButtonOpen, "toolStripButtonOpen");
			this.toolStripButtonOpen.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOpen.Image = global::RdlReader.Properties.Resources.document_open;
			this.toolStripButtonOpen.Name = "toolStripButtonOpen";
			this.toolStripButtonOpen.Click += this.menuFileOpen_Click;
			// 
			// toolStripButtonSave
			// 
			resources.ApplyResources(this.toolStripButtonSave, "toolStripButtonSave");
			this.toolStripButtonSave.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonSave.Image = global::RdlReader.Properties.Resources.document_save;
			this.toolStripButtonSave.Name = "toolStripButtonSave";
			this.toolStripButtonSave.Click += this.menuFileSaveAs_Click;
			// 
			// toolStripButtonPrint
			// 
			resources.ApplyResources(this.toolStripButtonPrint, "toolStripButtonPrint");
			this.toolStripButtonPrint.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonPrint.Image = global::RdlReader.Properties.Resources.document_print;
			this.toolStripButtonPrint.Name = "toolStripButtonPrint";
			this.toolStripButtonPrint.Click += this.menuFilePrint_Click;
			// 
			// RdlReader
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "RdlReader";
			this.WindowState = Majorsilence.Forms.FormWindowState.Maximized;
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem printToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem recentFilesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem selectionToolToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem zoomToToolStripMenuItem;
        private ToolStripMenuItem actualSizeToolStripMenuItem;
        private ToolStripMenuItem fitPageToolStripMenuItem;
        private ToolStripMenuItem fitWidthToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripMenuItem singlePageToolStripMenuItem;
        private ToolStripMenuItem continuousToolStripMenuItem;
        private ToolStripMenuItem facingToolStripMenuItem;
        private ToolStripMenuItem pageLayoutToolStripMenuItem;
        private ToolStripMenuItem continuousFacingToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem cascadeToolStripMenuItem;
        private ToolStripMenuItem tileToolStripMenuItem;
        private ToolStripMenuItem horizontallyToolStripMenuItem;
        private ToolStripMenuItem verticallyToolStripMenuItem;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButtonPrint;
        private ToolStripButton toolStripButtonOpen;
        private ToolStripButton toolStripButtonSave;
	}
}
