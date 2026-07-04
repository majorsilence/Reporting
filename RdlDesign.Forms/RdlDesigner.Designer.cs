using System;
using Majorsilence.Forms;
using System.Drawing;
using Majorsilence.Forms.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
	public partial class RdlDesigner : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
        private MDIChild printChild=null;
        private DialogValidateRdl _ValidateRdl=null;
        private DockStyle _PropertiesLocation = DockStyle.Right;   

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(RdlDesigner));
            this.DoubleBuffered = true;
			this.menuStrip1 = new Majorsilence.Forms.MenuStrip();
			this.fileToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.newReportToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.closeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new Majorsilence.Forms.ToolStripSeparator();
			this.saveToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.printToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.exportToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.pDFToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.pDFOldStyleToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.tIFFToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.cSVToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.excelToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.Excel2007ToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.dOCToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.rTFDOCToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.xMLToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.webPageHTMLToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.webArchiveSingleFileMHTToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new Majorsilence.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new Majorsilence.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new Majorsilence.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new Majorsilence.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new Majorsilence.Forms.ToolStripSeparator();
			this.findToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.findNextToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.replaceToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.goToToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator7 = new Majorsilence.Forms.ToolStripSeparator();
			this.formatXMLToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.designerToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.rDLTextToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.previewToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.showReportInBrowserToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.propertiesWindowsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.insertToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.chartToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.gridToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.imageToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.lineToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.listToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.matrixToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.rectangleToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.subReportToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.tableToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.textboxToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.dataToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.dataSetsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.dataSourcesToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator8 = new Majorsilence.Forms.ToolStripSeparator();
			this.embeddedImagesToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new Majorsilence.Forms.ToolStripSeparator();
			this.createSharedDataSourceToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.formatToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.alignToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.leftsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.centersToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.rightsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator10 = new Majorsilence.Forms.ToolStripSeparator();
			this.topsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.middlesToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.bottomsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.sizeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.widthToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.heightToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.bothToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.horizontalSpacingToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.makeEqualToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.verticalSpacingToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.makeEqualToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator11 = new Majorsilence.Forms.ToolStripSeparator();
			this.paddingLeftToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem2 = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem2 = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem2 = new Majorsilence.Forms.ToolStripMenuItem();
			this.paddingRightToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem3 = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem3 = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem3 = new Majorsilence.Forms.ToolStripMenuItem();
			this.paddintTopToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem4 = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem4 = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem4 = new Majorsilence.Forms.ToolStripMenuItem();
			this.paddingBottomToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.increaseToolStripMenuItem5 = new Majorsilence.Forms.ToolStripMenuItem();
			this.decreaseToolStripMenuItem5 = new Majorsilence.Forms.ToolStripMenuItem();
			this.zeroToolStripMenuItem5 = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new Majorsilence.Forms.ToolStripSeparator();
			this.centerInContainerToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.centerHorizontallyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.centerVerticallyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.centerBothToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.validateRDLToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator12 = new Majorsilence.Forms.ToolStripSeparator();
			this.startDesktopServerToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.toolStripSeparator13 = new Majorsilence.Forms.ToolStripSeparator();
			this.optionsToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.windowToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.cascadeToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.tileToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.horizontalToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.verticallyToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.closeAllToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem1 = new Majorsilence.Forms.ToolStripMenuItem();
			this.supportToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new Majorsilence.Forms.ToolStripMenuItem();
			this.mainTB = new Majorsilence.Forms.ToolStrip();
			this.newToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.openToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.saveToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.cutToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.copyToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.pasteToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.undoToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.textboxToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.chartToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.tableToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.listToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.imageToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.matrixToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.subreportToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.rectangleToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.lineToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.fxToolStripLabel1 = new Majorsilence.Forms.ToolStripLabel();
			this.ctlEditTextbox = new Majorsilence.Forms.ToolStripTextBox();
			this.zoomControl = new Majorsilence.Reporting.RdlDesign.ToolStripUserZoomControl();
			this.toolStrip1 = new Majorsilence.Forms.ToolStrip();
			this.boldToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.italiacToolStripButton1 = new Majorsilence.Forms.ToolStripButton();
			this.underlineToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.leftAlignToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.centerAlignToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.rightAlignToolStripButton3 = new Majorsilence.Forms.ToolStripButton();
			this.fontToolStripComboBox1 = new Majorsilence.Forms.ToolStripComboBox();
			this.fontSizeToolStripComboBox1 = new Majorsilence.Forms.ToolStripComboBox();
			this.printToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.zoomToolStripComboBox1 = new Majorsilence.Forms.ToolStripComboBox();
			this.selectToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.pdfToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.htmlToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.excelToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.XmlToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.MhtToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.CsvToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.RtfToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.TifToolStripButton2 = new Majorsilence.Forms.ToolStripButton();
			this.toolStripSeparator14 = new Majorsilence.Forms.ToolStripSeparator();
			this.AlignmentGridEnable = new Majorsilence.Forms.ToolStripButton();
			this.mainTC = new Majorsilence.Forms.TabControl();
			this.label1 = new Majorsilence.Forms.Label();
			this.label2 = new Majorsilence.Forms.Label();
			this.panel1 = new Majorsilence.Forms.Panel();
			this.foreColorPicker1 = new Majorsilence.Reporting.RdlDesign.ColorPicker();
			this.backColorPicker1 = new Majorsilence.Reporting.RdlDesign.ColorPicker();
			this.statusStrip1 = new Majorsilence.Forms.StatusStrip();
			this.statusSelected = new Majorsilence.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new Majorsilence.Forms.ToolStripStatusLabel();
			this.statusPosition = new Majorsilence.Forms.ToolStripStatusLabel();
			this.mainSP = new Majorsilence.Forms.Splitter();
			this.ContextMenuTB = new Majorsilence.Forms.ContextMenuStrip(this.components);
			this.MenuTBClose = new Majorsilence.Forms.ToolStripMenuItem();
			this.MenuTBSave = new Majorsilence.Forms.ToolStripMenuItem();
			this.MenuTBCloseAllButThis = new Majorsilence.Forms.ToolStripMenuItem();
			this.mainProperties = new Majorsilence.Reporting.RdlDesign.PropertyCtl();
			this.menuStrip1.SuspendLayout();
			this.mainTB.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.ContextMenuTB.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.menuStrip1.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.insertToolStripMenuItem,
            this.dataToolStripMenuItem,
            this.formatToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem});
			resources.ApplyResources(this.menuStrip1, "menuStrip1");
			this.menuStrip1.Name = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.newReportToolStripMenuItem,
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.printToolStripMenuItem,
            this.exportToolStripMenuItem,
            this.toolStripSeparator2,
            this.recentFilesToolStripMenuItem,
            this.toolStripSeparator3,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
			this.fileToolStripMenuItem.DropDownOpening += this.menuFile_Popup;
			// 
			// newReportToolStripMenuItem
			// 
			this.newReportToolStripMenuItem.Name = "newReportToolStripMenuItem";
			resources.ApplyResources(this.newReportToolStripMenuItem, "newReportToolStripMenuItem");
			this.newReportToolStripMenuItem.Click += this.menuFileNewReport_Click;
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			resources.ApplyResources(this.openToolStripMenuItem, "openToolStripMenuItem");
			this.openToolStripMenuItem.Click += this.menuFileOpen_Click;
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			resources.ApplyResources(this.closeToolStripMenuItem, "closeToolStripMenuItem");
			this.closeToolStripMenuItem.Click += this.menuFileClose_Click;
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			// 
			// saveToolStripMenuItem
			// 
			resources.ApplyResources(this.saveToolStripMenuItem, "saveToolStripMenuItem");
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Click += this.menuFileSave_Click;
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
			// exportToolStripMenuItem
			// 
			this.exportToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.pDFToolStripMenuItem,
            this.pDFOldStyleToolStripMenuItem,
            this.tIFFToolStripMenuItem,
            this.cSVToolStripMenuItem,
            this.excelToolStripMenuItem,
            this.Excel2007ToolStripMenuItem,
            this.dOCToolStripMenuItem,
            this.rTFDOCToolStripMenuItem,
            this.xMLToolStripMenuItem,
            this.webPageHTMLToolStripMenuItem,
            this.webArchiveSingleFileMHTToolStripMenuItem});
			resources.ApplyResources(this.exportToolStripMenuItem, "exportToolStripMenuItem");
			this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
			// 
			// pDFToolStripMenuItem
			// 
			this.pDFToolStripMenuItem.Name = "pDFToolStripMenuItem";
			resources.ApplyResources(this.pDFToolStripMenuItem, "pDFToolStripMenuItem");
			this.pDFToolStripMenuItem.Click += this.pDFToolStripMenuItem_Click;
			// 
			// pDFOldStyleToolStripMenuItem
			// 
			this.pDFOldStyleToolStripMenuItem.Name = "pDFOldStyleToolStripMenuItem";
			resources.ApplyResources(this.pDFOldStyleToolStripMenuItem, "pDFOldStyleToolStripMenuItem");
			this.pDFOldStyleToolStripMenuItem.Click += this.pDFOldStyleToolStripMenuItem_Click;
			// 
			// tIFFToolStripMenuItem
			// 
			this.tIFFToolStripMenuItem.Name = "tIFFToolStripMenuItem";
			resources.ApplyResources(this.tIFFToolStripMenuItem, "tIFFToolStripMenuItem");
			this.tIFFToolStripMenuItem.Click += this.tIFFToolStripMenuItem_Click;
			// 
			// cSVToolStripMenuItem
			// 
			this.cSVToolStripMenuItem.Name = "cSVToolStripMenuItem";
			resources.ApplyResources(this.cSVToolStripMenuItem, "cSVToolStripMenuItem");
			this.cSVToolStripMenuItem.Click += this.cSVToolStripMenuItem_Click;
			// 
			// excelToolStripMenuItem
			// 
			this.excelToolStripMenuItem.Name = "excelToolStripMenuItem";
			resources.ApplyResources(this.excelToolStripMenuItem, "excelToolStripMenuItem");
			this.excelToolStripMenuItem.Click += this.excelToolStripMenuItem_Click;
			// 
			// Excel2007ToolStripMenuItem
			// 
			this.Excel2007ToolStripMenuItem.Name = "Excel2007ToolStripMenuItem";
			resources.ApplyResources(this.Excel2007ToolStripMenuItem, "Excel2007ToolStripMenuItem");
			this.Excel2007ToolStripMenuItem.Tag = "Excel2007";
			this.Excel2007ToolStripMenuItem.Click += this.Excel2007ToolStripMenuItem_Click;
			// 
			// dOCToolStripMenuItem
			// 
			this.dOCToolStripMenuItem.Name = "dOCToolStripMenuItem";
			resources.ApplyResources(this.dOCToolStripMenuItem, "dOCToolStripMenuItem");
			this.dOCToolStripMenuItem.Click += this.dOCToolStripMenuItem_Click;
			// 
			// rTFDOCToolStripMenuItem
			// 
			this.rTFDOCToolStripMenuItem.Name = "rTFDOCToolStripMenuItem";
			resources.ApplyResources(this.rTFDOCToolStripMenuItem, "rTFDOCToolStripMenuItem");
			this.rTFDOCToolStripMenuItem.Click += this.rTFDOCToolStripMenuItem_Click;
			// 
			// xMLToolStripMenuItem
			// 
			this.xMLToolStripMenuItem.Name = "xMLToolStripMenuItem";
			resources.ApplyResources(this.xMLToolStripMenuItem, "xMLToolStripMenuItem");
			this.xMLToolStripMenuItem.Click += this.xMLToolStripMenuItem_Click;
			// 
			// webPageHTMLToolStripMenuItem
			// 
			this.webPageHTMLToolStripMenuItem.Name = "webPageHTMLToolStripMenuItem";
			resources.ApplyResources(this.webPageHTMLToolStripMenuItem, "webPageHTMLToolStripMenuItem");
			this.webPageHTMLToolStripMenuItem.Click += this.webPageHTMLToolStripMenuItem_Click;
			// 
			// webArchiveSingleFileMHTToolStripMenuItem
			// 
			this.webArchiveSingleFileMHTToolStripMenuItem.Name = "webArchiveSingleFileMHTToolStripMenuItem";
			resources.ApplyResources(this.webArchiveSingleFileMHTToolStripMenuItem, "webArchiveSingleFileMHTToolStripMenuItem");
			this.webArchiveSingleFileMHTToolStripMenuItem.Click += this.webArchiveSingleFileMHTToolStripMenuItem_Click;
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			// 
			// recentFilesToolStripMenuItem
			// 
			resources.ApplyResources(this.recentFilesToolStripMenuItem, "recentFilesToolStripMenuItem");
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			resources.ApplyResources(this.exitToolStripMenuItem, "exitToolStripMenuItem");
			this.exitToolStripMenuItem.Click += this.ExitToolStripMenuItem_Click;
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripSeparator4,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripSeparator5,
            this.selectAllToolStripMenuItem,
            this.toolStripSeparator6,
            this.findToolStripMenuItem,
            this.findNextToolStripMenuItem,
            this.replaceToolStripMenuItem,
            this.goToToolStripMenuItem,
            this.toolStripSeparator7,
            this.formatXMLToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
			this.editToolStripMenuItem.DropDownOpening += this.menuEdit_Popup;
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			resources.ApplyResources(this.undoToolStripMenuItem, "undoToolStripMenuItem");
			this.undoToolStripMenuItem.Click += this.menuEditUndo_Click;
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			resources.ApplyResources(this.redoToolStripMenuItem, "redoToolStripMenuItem");
			this.redoToolStripMenuItem.Click += this.menuEditRedo_Click;
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			resources.ApplyResources(this.cutToolStripMenuItem, "cutToolStripMenuItem");
			this.cutToolStripMenuItem.Click += this.menuEditCut_Click;
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
			this.copyToolStripMenuItem.Click += this.menuEditCopy_Click;
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			resources.ApplyResources(this.pasteToolStripMenuItem, "pasteToolStripMenuItem");
			this.pasteToolStripMenuItem.Click += this.menuEditPaste_Click;
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			resources.ApplyResources(this.deleteToolStripMenuItem, "deleteToolStripMenuItem");
			this.deleteToolStripMenuItem.Click += this.menuEditDelete_Click;
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			resources.ApplyResources(this.selectAllToolStripMenuItem, "selectAllToolStripMenuItem");
			this.selectAllToolStripMenuItem.Click += this.menuEditSelectAll_Click;
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
			// 
			// findToolStripMenuItem
			// 
			this.findToolStripMenuItem.Name = "findToolStripMenuItem";
			resources.ApplyResources(this.findToolStripMenuItem, "findToolStripMenuItem");
			this.findToolStripMenuItem.Click += this.menuEditFind_Click;
			// 
			// findNextToolStripMenuItem
			// 
			this.findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
			resources.ApplyResources(this.findNextToolStripMenuItem, "findNextToolStripMenuItem");
			this.findNextToolStripMenuItem.Click += this.menuEditFindNext_Click;
			// 
			// replaceToolStripMenuItem
			// 
			this.replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
			resources.ApplyResources(this.replaceToolStripMenuItem, "replaceToolStripMenuItem");
			this.replaceToolStripMenuItem.Click += this.menuEditReplace_Click;
			// 
			// goToToolStripMenuItem
			// 
			this.goToToolStripMenuItem.Name = "goToToolStripMenuItem";
			resources.ApplyResources(this.goToToolStripMenuItem, "goToToolStripMenuItem");
			this.goToToolStripMenuItem.Click += this.menuEditGoto_Click;
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
			// 
			// formatXMLToolStripMenuItem
			// 
			this.formatXMLToolStripMenuItem.Name = "formatXMLToolStripMenuItem";
			resources.ApplyResources(this.formatXMLToolStripMenuItem, "formatXMLToolStripMenuItem");
			this.formatXMLToolStripMenuItem.Click += this.menuEdit_FormatXml;
			// 
			// viewToolStripMenuItem
			// 
			this.viewToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.designerToolStripMenuItem,
            this.rDLTextToolStripMenuItem,
            this.previewToolStripMenuItem,
            this.showReportInBrowserToolStripMenuItem,
            this.propertiesWindowsToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			resources.ApplyResources(this.viewToolStripMenuItem, "viewToolStripMenuItem");
			this.viewToolStripMenuItem.DropDownOpening += this.menuView_Popup;
			// 
			// designerToolStripMenuItem
			// 
			this.designerToolStripMenuItem.Name = "designerToolStripMenuItem";
			resources.ApplyResources(this.designerToolStripMenuItem, "designerToolStripMenuItem");
			this.designerToolStripMenuItem.Click += this.menuViewDesigner_Click;
			// 
			// rDLTextToolStripMenuItem
			// 
			this.rDLTextToolStripMenuItem.Name = "rDLTextToolStripMenuItem";
			resources.ApplyResources(this.rDLTextToolStripMenuItem, "rDLTextToolStripMenuItem");
			this.rDLTextToolStripMenuItem.Click += this.menuViewRDL_Click;
			// 
			// previewToolStripMenuItem
			// 
			this.previewToolStripMenuItem.Name = "previewToolStripMenuItem";
			resources.ApplyResources(this.previewToolStripMenuItem, "previewToolStripMenuItem");
			this.previewToolStripMenuItem.Click += this.menuViewPreview_Click;
			// 
			// showReportInBrowserToolStripMenuItem
			// 
			this.showReportInBrowserToolStripMenuItem.Name = "showReportInBrowserToolStripMenuItem";
			resources.ApplyResources(this.showReportInBrowserToolStripMenuItem, "showReportInBrowserToolStripMenuItem");
			this.showReportInBrowserToolStripMenuItem.Click += this.menuViewBrowser_Click;
			// 
			// propertiesWindowsToolStripMenuItem
			// 
			this.propertiesWindowsToolStripMenuItem.Name = "propertiesWindowsToolStripMenuItem";
			resources.ApplyResources(this.propertiesWindowsToolStripMenuItem, "propertiesWindowsToolStripMenuItem");
			this.propertiesWindowsToolStripMenuItem.Click += this.menuEditProperties_Click;
			// 
			// insertToolStripMenuItem
			// 
			this.insertToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.chartToolStripMenuItem,
            this.gridToolStripMenuItem,
            this.imageToolStripMenuItem,
            this.lineToolStripMenuItem,
            this.listToolStripMenuItem,
            this.matrixToolStripMenuItem,
            this.rectangleToolStripMenuItem,
            this.subReportToolStripMenuItem,
            this.tableToolStripMenuItem,
            this.textboxToolStripMenuItem});
			this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
			resources.ApplyResources(this.insertToolStripMenuItem, "insertToolStripMenuItem");
			// 
			// chartToolStripMenuItem
			// 
			this.chartToolStripMenuItem.Name = "chartToolStripMenuItem";
			resources.ApplyResources(this.chartToolStripMenuItem, "chartToolStripMenuItem");
			this.chartToolStripMenuItem.Tag = "Chart";
			this.chartToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// gridToolStripMenuItem
			// 
			this.gridToolStripMenuItem.Name = "gridToolStripMenuItem";
			resources.ApplyResources(this.gridToolStripMenuItem, "gridToolStripMenuItem");
			this.gridToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// imageToolStripMenuItem
			// 
			this.imageToolStripMenuItem.Name = "imageToolStripMenuItem";
			resources.ApplyResources(this.imageToolStripMenuItem, "imageToolStripMenuItem");
			this.imageToolStripMenuItem.Tag = "Image";
			this.imageToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// lineToolStripMenuItem
			// 
			this.lineToolStripMenuItem.Name = "lineToolStripMenuItem";
			resources.ApplyResources(this.lineToolStripMenuItem, "lineToolStripMenuItem");
			this.lineToolStripMenuItem.Tag = "Line";
			this.lineToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// listToolStripMenuItem
			// 
			this.listToolStripMenuItem.Name = "listToolStripMenuItem";
			resources.ApplyResources(this.listToolStripMenuItem, "listToolStripMenuItem");
			this.listToolStripMenuItem.Tag = "List";
			this.listToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// matrixToolStripMenuItem
			// 
			this.matrixToolStripMenuItem.Name = "matrixToolStripMenuItem";
			resources.ApplyResources(this.matrixToolStripMenuItem, "matrixToolStripMenuItem");
			this.matrixToolStripMenuItem.Tag = "Matrix";
			this.matrixToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// rectangleToolStripMenuItem
			// 
			this.rectangleToolStripMenuItem.Name = "rectangleToolStripMenuItem";
			resources.ApplyResources(this.rectangleToolStripMenuItem, "rectangleToolStripMenuItem");
			this.rectangleToolStripMenuItem.Tag = "Rectangle";
			this.rectangleToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// subReportToolStripMenuItem
			// 
			this.subReportToolStripMenuItem.Name = "subReportToolStripMenuItem";
			resources.ApplyResources(this.subReportToolStripMenuItem, "subReportToolStripMenuItem");
			this.subReportToolStripMenuItem.Tag = "Subreport";
			this.subReportToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// tableToolStripMenuItem
			// 
			this.tableToolStripMenuItem.Name = "tableToolStripMenuItem";
			resources.ApplyResources(this.tableToolStripMenuItem, "tableToolStripMenuItem");
			this.tableToolStripMenuItem.Tag = "Table";
			this.tableToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// textboxToolStripMenuItem
			// 
			this.textboxToolStripMenuItem.Name = "textboxToolStripMenuItem";
			resources.ApplyResources(this.textboxToolStripMenuItem, "textboxToolStripMenuItem");
			this.textboxToolStripMenuItem.Tag = "Textbox";
			this.textboxToolStripMenuItem.Click += this.InsertToolStripMenuItem_Click;
			// 
			// dataToolStripMenuItem
			// 
			this.dataToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.dataSetsToolStripMenuItem,
            this.dataSourcesToolStripMenuItem1,
            this.toolStripSeparator8,
            this.embeddedImagesToolStripMenuItem,
            this.toolStripSeparator9,
            this.createSharedDataSourceToolStripMenuItem});
			this.dataToolStripMenuItem.Name = "dataToolStripMenuItem";
			resources.ApplyResources(this.dataToolStripMenuItem, "dataToolStripMenuItem");
			this.dataToolStripMenuItem.DropDownOpening += this.menuData_Popup;
			// 
			// dataSetsToolStripMenuItem
			// 
			this.dataSetsToolStripMenuItem.Name = "dataSetsToolStripMenuItem";
			resources.ApplyResources(this.dataSetsToolStripMenuItem, "dataSetsToolStripMenuItem");
			// 
			// dataSourcesToolStripMenuItem1
			// 
			this.dataSourcesToolStripMenuItem1.Name = "dataSourcesToolStripMenuItem1";
			resources.ApplyResources(this.dataSourcesToolStripMenuItem1, "dataSourcesToolStripMenuItem1");
			this.dataSourcesToolStripMenuItem1.Click += this.dataSourcesToolStripMenuItem1_Click;
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
			// 
			// embeddedImagesToolStripMenuItem
			// 
			this.embeddedImagesToolStripMenuItem.Name = "embeddedImagesToolStripMenuItem";
			resources.ApplyResources(this.embeddedImagesToolStripMenuItem, "embeddedImagesToolStripMenuItem");
			this.embeddedImagesToolStripMenuItem.Click += this.embeddedImagesToolStripMenuItem_Click;
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
			// 
			// createSharedDataSourceToolStripMenuItem
			// 
			this.createSharedDataSourceToolStripMenuItem.Name = "createSharedDataSourceToolStripMenuItem";
			resources.ApplyResources(this.createSharedDataSourceToolStripMenuItem, "createSharedDataSourceToolStripMenuItem");
			this.createSharedDataSourceToolStripMenuItem.Click += this.menuFileNewDataSourceRef_Click;
			// 
			// formatToolStripMenuItem
			// 
			this.formatToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.alignToolStripMenuItem,
            this.sizeToolStripMenuItem,
            this.horizontalSpacingToolStripMenuItem,
            this.verticalSpacingToolStripMenuItem,
            this.toolStripSeparator11,
            this.paddingLeftToolStripMenuItem,
            this.paddingRightToolStripMenuItem,
            this.paddintTopToolStripMenuItem,
            this.paddingBottomToolStripMenuItem,
            this.toolStripMenuItem1,
            this.centerInContainerToolStripMenuItem});
			this.formatToolStripMenuItem.Name = "formatToolStripMenuItem";
			resources.ApplyResources(this.formatToolStripMenuItem, "formatToolStripMenuItem");
			this.formatToolStripMenuItem.DropDownOpening += this.menuFormat_Popup;
			// 
			// alignToolStripMenuItem
			// 
			this.alignToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.leftsToolStripMenuItem,
            this.centersToolStripMenuItem,
            this.rightsToolStripMenuItem,
            this.toolStripSeparator10,
            this.topsToolStripMenuItem,
            this.middlesToolStripMenuItem,
            this.bottomsToolStripMenuItem});
			this.alignToolStripMenuItem.Name = "alignToolStripMenuItem";
			resources.ApplyResources(this.alignToolStripMenuItem, "alignToolStripMenuItem");
			// 
			// leftsToolStripMenuItem
			// 
			this.leftsToolStripMenuItem.Name = "leftsToolStripMenuItem";
			resources.ApplyResources(this.leftsToolStripMenuItem, "leftsToolStripMenuItem");
			this.leftsToolStripMenuItem.Click += this.menuFormatAlignL_Click;
			// 
			// centersToolStripMenuItem
			// 
			this.centersToolStripMenuItem.Name = "centersToolStripMenuItem";
			resources.ApplyResources(this.centersToolStripMenuItem, "centersToolStripMenuItem");
			this.centersToolStripMenuItem.Click += this.menuFormatAlignC_Click;
			// 
			// rightsToolStripMenuItem
			// 
			this.rightsToolStripMenuItem.Name = "rightsToolStripMenuItem";
			resources.ApplyResources(this.rightsToolStripMenuItem, "rightsToolStripMenuItem");
			this.rightsToolStripMenuItem.Click += this.menuFormatAlignR_Click;
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
			// 
			// topsToolStripMenuItem
			// 
			this.topsToolStripMenuItem.Name = "topsToolStripMenuItem";
			resources.ApplyResources(this.topsToolStripMenuItem, "topsToolStripMenuItem");
			this.topsToolStripMenuItem.Click += this.menuFormatAlignT_Click;
			// 
			// middlesToolStripMenuItem
			// 
			this.middlesToolStripMenuItem.Name = "middlesToolStripMenuItem";
			resources.ApplyResources(this.middlesToolStripMenuItem, "middlesToolStripMenuItem");
			this.middlesToolStripMenuItem.Click += this.menuFormatAlignM_Click;
			// 
			// bottomsToolStripMenuItem
			// 
			this.bottomsToolStripMenuItem.Name = "bottomsToolStripMenuItem";
			resources.ApplyResources(this.bottomsToolStripMenuItem, "bottomsToolStripMenuItem");
			this.bottomsToolStripMenuItem.Click += this.menuFormatAlignB_Click;
			// 
			// sizeToolStripMenuItem
			// 
			this.sizeToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.widthToolStripMenuItem,
            this.heightToolStripMenuItem,
            this.bothToolStripMenuItem});
			this.sizeToolStripMenuItem.Name = "sizeToolStripMenuItem";
			resources.ApplyResources(this.sizeToolStripMenuItem, "sizeToolStripMenuItem");
			// 
			// widthToolStripMenuItem
			// 
			this.widthToolStripMenuItem.Name = "widthToolStripMenuItem";
			resources.ApplyResources(this.widthToolStripMenuItem, "widthToolStripMenuItem");
			this.widthToolStripMenuItem.Click += this.menuFormatSizeW_Click;
			// 
			// heightToolStripMenuItem
			// 
			this.heightToolStripMenuItem.Name = "heightToolStripMenuItem";
			resources.ApplyResources(this.heightToolStripMenuItem, "heightToolStripMenuItem");
			this.heightToolStripMenuItem.Click += this.menuFormatSizeH_Click;
			// 
			// bothToolStripMenuItem
			// 
			this.bothToolStripMenuItem.Name = "bothToolStripMenuItem";
			resources.ApplyResources(this.bothToolStripMenuItem, "bothToolStripMenuItem");
			this.bothToolStripMenuItem.Click += this.menuFormatSizeB_Click;
			// 
			// horizontalSpacingToolStripMenuItem
			// 
			this.horizontalSpacingToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.makeEqualToolStripMenuItem,
            this.increaseToolStripMenuItem,
            this.decreaseToolStripMenuItem,
            this.zeroToolStripMenuItem});
			this.horizontalSpacingToolStripMenuItem.Name = "horizontalSpacingToolStripMenuItem";
			resources.ApplyResources(this.horizontalSpacingToolStripMenuItem, "horizontalSpacingToolStripMenuItem");
			// 
			// makeEqualToolStripMenuItem
			// 
			this.makeEqualToolStripMenuItem.Name = "makeEqualToolStripMenuItem";
			resources.ApplyResources(this.makeEqualToolStripMenuItem, "makeEqualToolStripMenuItem");
			this.makeEqualToolStripMenuItem.Click += this.menuFormatHorzE_Click;
			// 
			// increaseToolStripMenuItem
			// 
			this.increaseToolStripMenuItem.Name = "increaseToolStripMenuItem";
			resources.ApplyResources(this.increaseToolStripMenuItem, "increaseToolStripMenuItem");
			this.increaseToolStripMenuItem.Click += this.menuFormatHorzI_Click;
			// 
			// decreaseToolStripMenuItem
			// 
			this.decreaseToolStripMenuItem.Name = "decreaseToolStripMenuItem";
			resources.ApplyResources(this.decreaseToolStripMenuItem, "decreaseToolStripMenuItem");
			this.decreaseToolStripMenuItem.Click += this.menuFormatHorzD_Click;
			// 
			// zeroToolStripMenuItem
			// 
			this.zeroToolStripMenuItem.Name = "zeroToolStripMenuItem";
			resources.ApplyResources(this.zeroToolStripMenuItem, "zeroToolStripMenuItem");
			this.zeroToolStripMenuItem.Click += this.menuFormatHorzZ_Click;
			// 
			// verticalSpacingToolStripMenuItem
			// 
			this.verticalSpacingToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.makeEqualToolStripMenuItem1,
            this.increaseToolStripMenuItem1,
            this.decreaseToolStripMenuItem1,
            this.zeroToolStripMenuItem1});
			this.verticalSpacingToolStripMenuItem.Name = "verticalSpacingToolStripMenuItem";
			resources.ApplyResources(this.verticalSpacingToolStripMenuItem, "verticalSpacingToolStripMenuItem");
			// 
			// makeEqualToolStripMenuItem1
			// 
			this.makeEqualToolStripMenuItem1.Name = "makeEqualToolStripMenuItem1";
			resources.ApplyResources(this.makeEqualToolStripMenuItem1, "makeEqualToolStripMenuItem1");
			this.makeEqualToolStripMenuItem1.Click += this.menuFormatVertE_Click;
			// 
			// increaseToolStripMenuItem1
			// 
			this.increaseToolStripMenuItem1.Name = "increaseToolStripMenuItem1";
			resources.ApplyResources(this.increaseToolStripMenuItem1, "increaseToolStripMenuItem1");
			this.increaseToolStripMenuItem1.Click += this.menuFormatVertI_Click;
			// 
			// decreaseToolStripMenuItem1
			// 
			this.decreaseToolStripMenuItem1.Name = "decreaseToolStripMenuItem1";
			resources.ApplyResources(this.decreaseToolStripMenuItem1, "decreaseToolStripMenuItem1");
			this.decreaseToolStripMenuItem1.Click += this.menuFormatVertD_Click;
			// 
			// zeroToolStripMenuItem1
			// 
			this.zeroToolStripMenuItem1.Name = "zeroToolStripMenuItem1";
			resources.ApplyResources(this.zeroToolStripMenuItem1, "zeroToolStripMenuItem1");
			this.zeroToolStripMenuItem1.Click += this.menuFormatVertZ_Click;
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			resources.ApplyResources(this.toolStripSeparator11, "toolStripSeparator11");
			// 
			// paddingLeftToolStripMenuItem
			// 
			this.paddingLeftToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.increaseToolStripMenuItem2,
            this.decreaseToolStripMenuItem2,
            this.zeroToolStripMenuItem2});
			this.paddingLeftToolStripMenuItem.Name = "paddingLeftToolStripMenuItem";
			resources.ApplyResources(this.paddingLeftToolStripMenuItem, "paddingLeftToolStripMenuItem");
			// 
			// increaseToolStripMenuItem2
			// 
			this.increaseToolStripMenuItem2.Name = "increaseToolStripMenuItem2";
			resources.ApplyResources(this.increaseToolStripMenuItem2, "increaseToolStripMenuItem2");
			this.increaseToolStripMenuItem2.Click += this.menuFormatPadding_Click;
			// 
			// decreaseToolStripMenuItem2
			// 
			this.decreaseToolStripMenuItem2.Name = "decreaseToolStripMenuItem2";
			resources.ApplyResources(this.decreaseToolStripMenuItem2, "decreaseToolStripMenuItem2");
			this.decreaseToolStripMenuItem2.Click += this.menuFormatPadding_Click;
			// 
			// zeroToolStripMenuItem2
			// 
			this.zeroToolStripMenuItem2.Name = "zeroToolStripMenuItem2";
			resources.ApplyResources(this.zeroToolStripMenuItem2, "zeroToolStripMenuItem2");
			this.zeroToolStripMenuItem2.Click += this.menuFormatPadding_Click;
			// 
			// paddingRightToolStripMenuItem
			// 
			this.paddingRightToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.increaseToolStripMenuItem3,
            this.decreaseToolStripMenuItem3,
            this.zeroToolStripMenuItem3});
			this.paddingRightToolStripMenuItem.Name = "paddingRightToolStripMenuItem";
			resources.ApplyResources(this.paddingRightToolStripMenuItem, "paddingRightToolStripMenuItem");
			// 
			// increaseToolStripMenuItem3
			// 
			this.increaseToolStripMenuItem3.Name = "increaseToolStripMenuItem3";
			resources.ApplyResources(this.increaseToolStripMenuItem3, "increaseToolStripMenuItem3");
			this.increaseToolStripMenuItem3.Click += this.menuFormatPadding_Click;
			// 
			// decreaseToolStripMenuItem3
			// 
			this.decreaseToolStripMenuItem3.Name = "decreaseToolStripMenuItem3";
			resources.ApplyResources(this.decreaseToolStripMenuItem3, "decreaseToolStripMenuItem3");
			this.decreaseToolStripMenuItem3.Click += this.menuFormatPadding_Click;
			// 
			// zeroToolStripMenuItem3
			// 
			this.zeroToolStripMenuItem3.Name = "zeroToolStripMenuItem3";
			resources.ApplyResources(this.zeroToolStripMenuItem3, "zeroToolStripMenuItem3");
			this.zeroToolStripMenuItem3.Click += this.menuFormatPadding_Click;
			// 
			// paddintTopToolStripMenuItem
			// 
			this.paddintTopToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.increaseToolStripMenuItem4,
            this.decreaseToolStripMenuItem4,
            this.zeroToolStripMenuItem4});
			this.paddintTopToolStripMenuItem.Name = "paddintTopToolStripMenuItem";
			resources.ApplyResources(this.paddintTopToolStripMenuItem, "paddintTopToolStripMenuItem");
			// 
			// increaseToolStripMenuItem4
			// 
			this.increaseToolStripMenuItem4.Name = "increaseToolStripMenuItem4";
			resources.ApplyResources(this.increaseToolStripMenuItem4, "increaseToolStripMenuItem4");
			this.increaseToolStripMenuItem4.Click += this.menuFormatPadding_Click;
			// 
			// decreaseToolStripMenuItem4
			// 
			this.decreaseToolStripMenuItem4.Name = "decreaseToolStripMenuItem4";
			resources.ApplyResources(this.decreaseToolStripMenuItem4, "decreaseToolStripMenuItem4");
			this.decreaseToolStripMenuItem4.Click += this.menuFormatPadding_Click;
			// 
			// zeroToolStripMenuItem4
			// 
			this.zeroToolStripMenuItem4.Name = "zeroToolStripMenuItem4";
			resources.ApplyResources(this.zeroToolStripMenuItem4, "zeroToolStripMenuItem4");
			this.zeroToolStripMenuItem4.Click += this.menuFormatPadding_Click;
			// 
			// paddingBottomToolStripMenuItem
			// 
			this.paddingBottomToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.increaseToolStripMenuItem5,
            this.decreaseToolStripMenuItem5,
            this.zeroToolStripMenuItem5});
			this.paddingBottomToolStripMenuItem.Name = "paddingBottomToolStripMenuItem";
			resources.ApplyResources(this.paddingBottomToolStripMenuItem, "paddingBottomToolStripMenuItem");
			// 
			// increaseToolStripMenuItem5
			// 
			this.increaseToolStripMenuItem5.Name = "increaseToolStripMenuItem5";
			resources.ApplyResources(this.increaseToolStripMenuItem5, "increaseToolStripMenuItem5");
			this.increaseToolStripMenuItem5.Click += this.menuFormatPadding_Click;
			// 
			// decreaseToolStripMenuItem5
			// 
			this.decreaseToolStripMenuItem5.Name = "decreaseToolStripMenuItem5";
			resources.ApplyResources(this.decreaseToolStripMenuItem5, "decreaseToolStripMenuItem5");
			this.decreaseToolStripMenuItem5.Click += this.menuFormatPadding_Click;
			// 
			// zeroToolStripMenuItem5
			// 
			this.zeroToolStripMenuItem5.Name = "zeroToolStripMenuItem5";
			resources.ApplyResources(this.zeroToolStripMenuItem5, "zeroToolStripMenuItem5");
			this.zeroToolStripMenuItem5.Click += this.menuFormatPadding_Click;
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
			// 
			// centerInContainerToolStripMenuItem
			// 
			this.centerInContainerToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.centerHorizontallyToolStripMenuItem,
            this.centerVerticallyToolStripMenuItem,
            this.centerBothToolStripMenuItem});
			this.centerInContainerToolStripMenuItem.Name = "centerInContainerToolStripMenuItem";
			resources.ApplyResources(this.centerInContainerToolStripMenuItem, "centerInContainerToolStripMenuItem");
			// 
			// centerHorizontallyToolStripMenuItem
			// 
			this.centerHorizontallyToolStripMenuItem.Name = "centerHorizontallyToolStripMenuItem";
			resources.ApplyResources(this.centerHorizontallyToolStripMenuItem, "centerHorizontallyToolStripMenuItem");
			this.centerHorizontallyToolStripMenuItem.Click += this.CenterHorizontallyToolStripMenuItem_Click;
			// 
			// centerVerticallyToolStripMenuItem
			// 
			this.centerVerticallyToolStripMenuItem.Name = "centerVerticallyToolStripMenuItem";
			resources.ApplyResources(this.centerVerticallyToolStripMenuItem, "centerVerticallyToolStripMenuItem");
			this.centerVerticallyToolStripMenuItem.Click += this.CenterVerticallyToolStripMenuItem_Click;
			// 
			// centerBothToolStripMenuItem
			// 
			this.centerBothToolStripMenuItem.Name = "centerBothToolStripMenuItem";
			resources.ApplyResources(this.centerBothToolStripMenuItem, "centerBothToolStripMenuItem");
			this.centerBothToolStripMenuItem.Click += this.CenterBothToolStripMenu_Click;
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.validateRDLToolStripMenuItem,
            this.toolStripSeparator12,
            this.startDesktopServerToolStripMenuItem,
            this.toolStripSeparator13,
            this.optionsToolStripMenuItem});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			resources.ApplyResources(this.toolsToolStripMenuItem, "toolsToolStripMenuItem");
			this.toolsToolStripMenuItem.DropDownOpening += this.menuTools_Popup;
			// 
			// validateRDLToolStripMenuItem
			// 
			this.validateRDLToolStripMenuItem.Name = "validateRDLToolStripMenuItem";
			resources.ApplyResources(this.validateRDLToolStripMenuItem, "validateRDLToolStripMenuItem");
			this.validateRDLToolStripMenuItem.Click += this.menuToolsValidateSchema_Click;
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			resources.ApplyResources(this.toolStripSeparator12, "toolStripSeparator12");
			// 
			// startDesktopServerToolStripMenuItem
			// 
			this.startDesktopServerToolStripMenuItem.Name = "startDesktopServerToolStripMenuItem";
			resources.ApplyResources(this.startDesktopServerToolStripMenuItem, "startDesktopServerToolStripMenuItem");
			this.startDesktopServerToolStripMenuItem.Click += this.menuToolsProcess_Click;
			// 
			// toolStripSeparator13
			// 
			this.toolStripSeparator13.Name = "toolStripSeparator13";
			resources.ApplyResources(this.toolStripSeparator13, "toolStripSeparator13");
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			resources.ApplyResources(this.optionsToolStripMenuItem, "optionsToolStripMenuItem");
			this.optionsToolStripMenuItem.Click += this.menuToolsOptions_Click;
			// 
			// windowToolStripMenuItem
			// 
			this.windowToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.cascadeToolStripMenuItem,
            this.tileToolStripMenuItem,
            this.closeAllToolStripMenuItem});
			this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
			resources.ApplyResources(this.windowToolStripMenuItem, "windowToolStripMenuItem");
			this.windowToolStripMenuItem.DropDownOpening += this.menuWnd_Popup;
			// 
			// cascadeToolStripMenuItem
			// 
			this.cascadeToolStripMenuItem.Name = "cascadeToolStripMenuItem";
			resources.ApplyResources(this.cascadeToolStripMenuItem, "cascadeToolStripMenuItem");
			this.cascadeToolStripMenuItem.Click += this.menuWndCascade_Click;
			// 
			// tileToolStripMenuItem
			// 
			this.tileToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.horizontalToolStripMenuItem,
            this.verticallyToolStripMenuItem});
			this.tileToolStripMenuItem.Name = "tileToolStripMenuItem";
			resources.ApplyResources(this.tileToolStripMenuItem, "tileToolStripMenuItem");
			// 
			// horizontalToolStripMenuItem
			// 
			this.horizontalToolStripMenuItem.Name = "horizontalToolStripMenuItem";
			resources.ApplyResources(this.horizontalToolStripMenuItem, "horizontalToolStripMenuItem");
			this.horizontalToolStripMenuItem.Click += this.menuWndTileH_Click;
			// 
			// verticallyToolStripMenuItem
			// 
			this.verticallyToolStripMenuItem.Name = "verticallyToolStripMenuItem";
			resources.ApplyResources(this.verticallyToolStripMenuItem, "verticallyToolStripMenuItem");
			this.verticallyToolStripMenuItem.Click += this.menuWndTileV_Click;
			// 
			// closeAllToolStripMenuItem
			// 
			this.closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
			resources.ApplyResources(this.closeAllToolStripMenuItem, "closeAllToolStripMenuItem");
			this.closeAllToolStripMenuItem.Click += this.menuWndCloseAll_Click;
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem1,
            this.supportToolStripMenuItem,
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			// 
			// helpToolStripMenuItem1
			// 
			this.helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
			resources.ApplyResources(this.helpToolStripMenuItem1, "helpToolStripMenuItem1");
			this.helpToolStripMenuItem1.Click += this.menuHelpHelp_Click;
			// 
			// supportToolStripMenuItem
			// 
			this.supportToolStripMenuItem.Name = "supportToolStripMenuItem";
			resources.ApplyResources(this.supportToolStripMenuItem, "supportToolStripMenuItem");
			this.supportToolStripMenuItem.Click += this.menuHelpSupport_Click;
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			resources.ApplyResources(this.aboutToolStripMenuItem, "aboutToolStripMenuItem");
			this.aboutToolStripMenuItem.Click += this.menuHelpAbout_Click;
			// 
			// mainTB
			// 
			this.mainTB.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.mainTB.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.newToolStripButton1,
            this.openToolStripButton1,
            this.saveToolStripButton1,
            this.cutToolStripButton1,
            this.copyToolStripButton1,
            this.pasteToolStripButton1,
            this.undoToolStripButton1,
            this.textboxToolStripButton1,
            this.chartToolStripButton1,
            this.tableToolStripButton1,
            this.listToolStripButton1,
            this.imageToolStripButton1,
            this.matrixToolStripButton1,
            this.subreportToolStripButton1,
            this.rectangleToolStripButton1,
            this.lineToolStripButton1,
            this.fxToolStripLabel1,
            this.ctlEditTextbox,
            this.zoomControl});
			resources.ApplyResources(this.mainTB, "mainTB");
			this.mainTB.Name = "mainTB";
			// 
			// newToolStripButton1
			// 
			this.newToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.newToolStripButton1, "newToolStripButton1");
			this.newToolStripButton1.Name = "newToolStripButton1";
			this.newToolStripButton1.Tag = "New";
			this.newToolStripButton1.Click += this.menuFileNewReport_Click;
			// 
			// openToolStripButton1
			// 
			this.openToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.openToolStripButton1, "openToolStripButton1");
			this.openToolStripButton1.Name = "openToolStripButton1";
			this.openToolStripButton1.Tag = "Open";
			this.openToolStripButton1.Click += this.menuFileOpen_Click;
			// 
			// saveToolStripButton1
			// 
			this.saveToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.saveToolStripButton1, "saveToolStripButton1");
			this.saveToolStripButton1.Name = "saveToolStripButton1";
			this.saveToolStripButton1.Tag = "Save";
			this.saveToolStripButton1.Click += this.menuFileSave_Click;
			// 
			// cutToolStripButton1
			// 
			this.cutToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.cutToolStripButton1, "cutToolStripButton1");
			this.cutToolStripButton1.Name = "cutToolStripButton1";
			this.cutToolStripButton1.Tag = "Cut";
			this.cutToolStripButton1.Click += this.menuEditCut_Click;
			// 
			// copyToolStripButton1
			// 
			this.copyToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.copyToolStripButton1, "copyToolStripButton1");
			this.copyToolStripButton1.Name = "copyToolStripButton1";
			this.copyToolStripButton1.Tag = "Copy";
			this.copyToolStripButton1.Click += this.menuEditCopy_Click;
			// 
			// pasteToolStripButton1
			// 
			this.pasteToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.pasteToolStripButton1, "pasteToolStripButton1");
			this.pasteToolStripButton1.Name = "pasteToolStripButton1";
			this.pasteToolStripButton1.Tag = "Paste";
			this.pasteToolStripButton1.Click += this.menuEditPaste_Click;
			// 
			// undoToolStripButton1
			// 
			this.undoToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.undoToolStripButton1, "undoToolStripButton1");
			this.undoToolStripButton1.Name = "undoToolStripButton1";
			this.undoToolStripButton1.Tag = "Undo";
			this.undoToolStripButton1.Click += this.menuEditUndo_Click;
			// 
			// textboxToolStripButton1
			// 
			this.textboxToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.textboxToolStripButton1, "textboxToolStripButton1");
			this.textboxToolStripButton1.Name = "textboxToolStripButton1";
			this.textboxToolStripButton1.Tag = "Textbox";
			this.textboxToolStripButton1.Click += this.Insert_Click;
			// 
			// chartToolStripButton1
			// 
			this.chartToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.chartToolStripButton1, "chartToolStripButton1");
			this.chartToolStripButton1.Name = "chartToolStripButton1";
			this.chartToolStripButton1.Tag = "Chart";
			this.chartToolStripButton1.Click += this.Insert_Click;
			// 
			// tableToolStripButton1
			// 
			this.tableToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.tableToolStripButton1, "tableToolStripButton1");
			this.tableToolStripButton1.Name = "tableToolStripButton1";
			this.tableToolStripButton1.Tag = "Table";
			this.tableToolStripButton1.Click += this.Insert_Click;
			// 
			// listToolStripButton1
			// 
			this.listToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.listToolStripButton1, "listToolStripButton1");
			this.listToolStripButton1.Name = "listToolStripButton1";
			this.listToolStripButton1.Tag = "List";
			this.listToolStripButton1.Click += this.Insert_Click;
			// 
			// imageToolStripButton1
			// 
			this.imageToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.imageToolStripButton1, "imageToolStripButton1");
			this.imageToolStripButton1.Name = "imageToolStripButton1";
			this.imageToolStripButton1.Tag = "Image";
			this.imageToolStripButton1.Click += this.Insert_Click;
			// 
			// matrixToolStripButton1
			// 
			this.matrixToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.matrixToolStripButton1, "matrixToolStripButton1");
			this.matrixToolStripButton1.Name = "matrixToolStripButton1";
			this.matrixToolStripButton1.Tag = "Matrix";
			this.matrixToolStripButton1.Click += this.Insert_Click;
			// 
			// subreportToolStripButton1
			// 
			this.subreportToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.subreportToolStripButton1, "subreportToolStripButton1");
			this.subreportToolStripButton1.Name = "subreportToolStripButton1";
			this.subreportToolStripButton1.Tag = "Subreport";
			this.subreportToolStripButton1.Click += this.Insert_Click;
			// 
			// rectangleToolStripButton1
			// 
			this.rectangleToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.rectangleToolStripButton1, "rectangleToolStripButton1");
			this.rectangleToolStripButton1.Name = "rectangleToolStripButton1";
			this.rectangleToolStripButton1.Tag = "Rectangle";
			this.rectangleToolStripButton1.Click += this.Insert_Click;
			// 
			// lineToolStripButton1
			// 
			this.lineToolStripButton1.CheckOnClick = true;
			resources.ApplyResources(this.lineToolStripButton1, "lineToolStripButton1");
			this.lineToolStripButton1.Name = "lineToolStripButton1";
			this.lineToolStripButton1.Tag = "Line";
			this.lineToolStripButton1.Click += this.Insert_Click;
			// 
			// fxToolStripLabel1
			// 
			resources.ApplyResources(this.fxToolStripLabel1, "fxToolStripLabel1");
			this.fxToolStripLabel1.Name = "fxToolStripLabel1";
			this.fxToolStripLabel1.Tag = "fx";
			this.fxToolStripLabel1.Click += this.fxExpr_Click;
			this.fxToolStripLabel1.MouseEnter += this.fxExpr_MouseEnter;
			this.fxToolStripLabel1.MouseLeave += this.fxExpr_MouseLeave;
			// 
			// ctlEditTextbox
			// 
			this.ctlEditTextbox.Name = "ctlEditTextbox";
			resources.ApplyResources(this.ctlEditTextbox, "ctlEditTextbox");
			this.ctlEditTextbox.KeyDown += this.EditTextBox_KeyDown;
			this.ctlEditTextbox.Validated += this.EditTextbox_Validated;
			// 
			// zoomControl
			// 
			this.zoomControl.BackColor = System.Drawing.Color.White;
			resources.ApplyResources(this.zoomControl, "zoomControl");
			this.zoomControl.Name = "zoomControl";
			this.zoomControl.ZoomChanged += new System.EventHandler<Majorsilence.Reporting.RdlDesign.UserZoomControl.CambiaValori>(this.ZoomControl1_ValueChanged);
			// 
			// toolStrip1
			// 
			this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolStrip1.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.boldToolStripButton1,
            this.italiacToolStripButton1,
            this.underlineToolStripButton2,
            this.leftAlignToolStripButton2,
            this.centerAlignToolStripButton2,
            this.rightAlignToolStripButton3,
            this.fontToolStripComboBox1,
            this.fontSizeToolStripComboBox1,
            this.printToolStripButton2,
            this.zoomToolStripComboBox1,
            this.selectToolStripButton2,
            this.pdfToolStripButton2,
            this.htmlToolStripButton2,
            this.excelToolStripButton2,
            this.XmlToolStripButton2,
            this.MhtToolStripButton2,
            this.CsvToolStripButton2,
            this.RtfToolStripButton2,
            this.TifToolStripButton2,
            this.toolStripSeparator14,
            this.AlignmentGridEnable});
			resources.ApplyResources(this.toolStrip1, "toolStrip1");
			this.toolStrip1.Name = "toolStrip1";
			// 
			// boldToolStripButton1
			// 
			this.boldToolStripButton1.CheckOnClick = true;
			this.boldToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.boldToolStripButton1, "boldToolStripButton1");
			this.boldToolStripButton1.Name = "boldToolStripButton1";
			this.boldToolStripButton1.Tag = "bold";
			// 
			// italiacToolStripButton1
			// 
			this.italiacToolStripButton1.CheckOnClick = true;
			this.italiacToolStripButton1.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.italiacToolStripButton1, "italiacToolStripButton1");
			this.italiacToolStripButton1.Name = "italiacToolStripButton1";
			this.italiacToolStripButton1.Tag = "italic";
			this.italiacToolStripButton1.Click += this.ctlItalic_Click;
			// 
			// underlineToolStripButton2
			// 
			this.underlineToolStripButton2.CheckOnClick = true;
			this.underlineToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.underlineToolStripButton2, "underlineToolStripButton2");
			this.underlineToolStripButton2.Name = "underlineToolStripButton2";
			this.underlineToolStripButton2.Tag = "underline";
			this.underlineToolStripButton2.Click += this.ctlUnderline_Click;
			// 
			// leftAlignToolStripButton2
			// 
			this.leftAlignToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.leftAlignToolStripButton2, "leftAlignToolStripButton2");
			this.leftAlignToolStripButton2.Name = "leftAlignToolStripButton2";
			this.leftAlignToolStripButton2.Tag = "Left Align";
			this.leftAlignToolStripButton2.Click += this.bottomsToolStripMenuItemutton_Click;
			// 
			// centerAlignToolStripButton2
			// 
			this.centerAlignToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.centerAlignToolStripButton2, "centerAlignToolStripButton2");
			this.centerAlignToolStripButton2.Name = "centerAlignToolStripButton2";
			this.centerAlignToolStripButton2.Tag = "Center Align";
			this.centerAlignToolStripButton2.Click += this.bottomsToolStripMenuItemutton_Click;
			// 
			// rightAlignToolStripButton3
			// 
			this.rightAlignToolStripButton3.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.rightAlignToolStripButton3, "rightAlignToolStripButton3");
			this.rightAlignToolStripButton3.Name = "rightAlignToolStripButton3";
			this.rightAlignToolStripButton3.Tag = "Right Align";
			this.rightAlignToolStripButton3.Click += this.bottomsToolStripMenuItemutton_Click;
			// 
			// fontToolStripComboBox1
			// 
			this.fontToolStripComboBox1.Name = "fontToolStripComboBox1";
			resources.ApplyResources(this.fontToolStripComboBox1, "fontToolStripComboBox1");
			this.fontToolStripComboBox1.Tag = "Font";
			this.fontToolStripComboBox1.SelectedIndexChanged += this.ctlFont_Change;
			this.fontToolStripComboBox1.Validated += this.ctlFont_Change;
			// 
			// fontSizeToolStripComboBox1
			// 
			this.fontSizeToolStripComboBox1.Name = "fontSizeToolStripComboBox1";
			resources.ApplyResources(this.fontSizeToolStripComboBox1, "fontSizeToolStripComboBox1");
			this.fontSizeToolStripComboBox1.Tag = "Font Size";
			this.fontSizeToolStripComboBox1.SelectedIndexChanged += this.ctlFontSize_Change;
			this.fontSizeToolStripComboBox1.Validated += this.ctlFontSize_Change;
			// 
			// printToolStripButton2
			// 
			this.printToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.printToolStripButton2, "printToolStripButton2");
			this.printToolStripButton2.Name = "printToolStripButton2";
			this.printToolStripButton2.Tag = "Print";
			this.printToolStripButton2.Click += this.menuFilePrint_Click;
			// 
			// zoomToolStripComboBox1
			// 
			this.zoomToolStripComboBox1.Name = "zoomToolStripComboBox1";
			resources.ApplyResources(this.zoomToolStripComboBox1, "zoomToolStripComboBox1");
			this.zoomToolStripComboBox1.Tag = "Zoom";
			this.zoomToolStripComboBox1.SelectedIndexChanged += this.ctlZoom_Change;
			this.zoomToolStripComboBox1.Validated += this.ctlZoom_Change;
			// 
			// selectToolStripButton2
			// 
			this.selectToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.selectToolStripButton2, "selectToolStripButton2");
			this.selectToolStripButton2.Name = "selectToolStripButton2";
			this.selectToolStripButton2.Tag = "Select Tool";
			this.selectToolStripButton2.Click += this.ctlSelectTool_Click;
			// 
			// pdfToolStripButton2
			// 
			this.pdfToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.pdfToolStripButton2, "pdfToolStripButton2");
			this.pdfToolStripButton2.Name = "pdfToolStripButton2";
			this.pdfToolStripButton2.Tag = "PDF";
			this.pdfToolStripButton2.Click += this.exportToolStripMenuItemPdf_Click;
			// 
			// htmlToolStripButton2
			// 
			this.htmlToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.htmlToolStripButton2, "htmlToolStripButton2");
			this.htmlToolStripButton2.Name = "htmlToolStripButton2";
			this.htmlToolStripButton2.Tag = "HTML";
			this.htmlToolStripButton2.Click += this.exportToolStripMenuItemHtml_Click;
			// 
			// excelToolStripButton2
			// 
			this.excelToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.excelToolStripButton2, "excelToolStripButton2");
			this.excelToolStripButton2.Name = "excelToolStripButton2";
			this.excelToolStripButton2.Tag = "Excel";
			this.excelToolStripButton2.Click += this.exportToolStripMenuItemExcel_Click;
			// 
			// XmlToolStripButton2
			// 
			this.XmlToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.XmlToolStripButton2, "XmlToolStripButton2");
			this.XmlToolStripButton2.Name = "XmlToolStripButton2";
			this.XmlToolStripButton2.Tag = "XML";
			this.XmlToolStripButton2.Click += this.exportToolStripMenuItemXml_Click;
			// 
			// MhtToolStripButton2
			// 
			this.MhtToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.MhtToolStripButton2, "MhtToolStripButton2");
			this.MhtToolStripButton2.Name = "MhtToolStripButton2";
			this.MhtToolStripButton2.Tag = "MHT";
			this.MhtToolStripButton2.Click += this.exportToolStripMenuItemMHtml_Click;
			// 
			// CsvToolStripButton2
			// 
			this.CsvToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.CsvToolStripButton2, "CsvToolStripButton2");
			this.CsvToolStripButton2.Name = "CsvToolStripButton2";
			this.CsvToolStripButton2.Tag = "CSV";
			this.CsvToolStripButton2.Click += this.exportToolStripMenuItemCsv_Click;
			// 
			// RtfToolStripButton2
			// 
			this.RtfToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.RtfToolStripButton2, "RtfToolStripButton2");
			this.RtfToolStripButton2.Name = "RtfToolStripButton2";
			this.RtfToolStripButton2.Tag = "RTF";
			this.RtfToolStripButton2.Click += this.exportToolStripMenuItemRtf_Click;
			// 
			// TifToolStripButton2
			// 
			this.TifToolStripButton2.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.TifToolStripButton2, "TifToolStripButton2");
			this.TifToolStripButton2.Name = "TifToolStripButton2";
			this.TifToolStripButton2.Tag = "TIF";
			this.TifToolStripButton2.Click += this.exportToolStripMenuItemTif_Click;
			// 
			// toolStripSeparator14
			// 
			this.toolStripSeparator14.Name = "toolStripSeparator14";
			resources.ApplyResources(this.toolStripSeparator14, "toolStripSeparator14");
			// 
			// AlignmentGridEnable
			// 
			this.AlignmentGridEnable.CheckOnClick = true;
			this.AlignmentGridEnable.DisplayStyle = Majorsilence.Forms.ToolStripItemDisplayStyle.Text;
			resources.ApplyResources(this.AlignmentGridEnable, "AlignmentGridEnable");
			this.AlignmentGridEnable.Name = "AlignmentGridEnable";
			this.AlignmentGridEnable.CheckStateChanged += this.AlignmentGridEnable_CheckStateChanged;
			// 
			// mainTC
			// 
			resources.ApplyResources(this.mainTC, "mainTC");
			this.mainTC.Name = "mainTC";
			this.mainTC.SelectedIndex = 0;
			this.mainTC.SelectedIndexChanged += this.mainTC_SelectedIndexChanged;
			this.mainTC.MouseClick += this.mainTC_MouseClick;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.label2);
			this.panel1.Controls.Add(this.foreColorPicker1);
			this.panel1.Controls.Add(this.backColorPicker1);
			this.panel1.Controls.Add(this.mainTC);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			// 
			// foreColorPicker1
			// 
			this.foreColorPicker1.DrawMode = Majorsilence.Forms.DrawMode.OwnerDrawFixed;
			this.foreColorPicker1.DropDownHeight = 1;
			this.foreColorPicker1.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.foreColorPicker1, "foreColorPicker1");
			this.foreColorPicker1.FormattingEnabled = true;
			this.foreColorPicker1.Items.AddRange(new object[] {
            resources.GetString("foreColorPicker1.Items"),
            resources.GetString("foreColorPicker1.Items1"),
            resources.GetString("foreColorPicker1.Items2"),
            resources.GetString("foreColorPicker1.Items3"),
            resources.GetString("foreColorPicker1.Items4"),
            resources.GetString("foreColorPicker1.Items5"),
            resources.GetString("foreColorPicker1.Items6"),
            resources.GetString("foreColorPicker1.Items7"),
            resources.GetString("foreColorPicker1.Items8"),
            resources.GetString("foreColorPicker1.Items9"),
            resources.GetString("foreColorPicker1.Items10"),
            resources.GetString("foreColorPicker1.Items11"),
            resources.GetString("foreColorPicker1.Items12"),
            resources.GetString("foreColorPicker1.Items13"),
            resources.GetString("foreColorPicker1.Items14"),
            resources.GetString("foreColorPicker1.Items15"),
            resources.GetString("foreColorPicker1.Items16"),
            resources.GetString("foreColorPicker1.Items17"),
            resources.GetString("foreColorPicker1.Items18"),
            resources.GetString("foreColorPicker1.Items19"),
            resources.GetString("foreColorPicker1.Items20"),
            resources.GetString("foreColorPicker1.Items21"),
            resources.GetString("foreColorPicker1.Items22"),
            resources.GetString("foreColorPicker1.Items23"),
            resources.GetString("foreColorPicker1.Items24"),
            resources.GetString("foreColorPicker1.Items25"),
            resources.GetString("foreColorPicker1.Items26"),
            resources.GetString("foreColorPicker1.Items27"),
            resources.GetString("foreColorPicker1.Items28"),
            resources.GetString("foreColorPicker1.Items29"),
            resources.GetString("foreColorPicker1.Items30"),
            resources.GetString("foreColorPicker1.Items31"),
            resources.GetString("foreColorPicker1.Items32"),
            resources.GetString("foreColorPicker1.Items33"),
            resources.GetString("foreColorPicker1.Items34"),
            resources.GetString("foreColorPicker1.Items35"),
            resources.GetString("foreColorPicker1.Items36"),
            resources.GetString("foreColorPicker1.Items37"),
            resources.GetString("foreColorPicker1.Items38"),
            resources.GetString("foreColorPicker1.Items39"),
            resources.GetString("foreColorPicker1.Items40"),
            resources.GetString("foreColorPicker1.Items41"),
            resources.GetString("foreColorPicker1.Items42"),
            resources.GetString("foreColorPicker1.Items43"),
            resources.GetString("foreColorPicker1.Items44"),
            resources.GetString("foreColorPicker1.Items45"),
            resources.GetString("foreColorPicker1.Items46"),
            resources.GetString("foreColorPicker1.Items47"),
            resources.GetString("foreColorPicker1.Items48"),
            resources.GetString("foreColorPicker1.Items49"),
            resources.GetString("foreColorPicker1.Items50"),
            resources.GetString("foreColorPicker1.Items51"),
            resources.GetString("foreColorPicker1.Items52"),
            resources.GetString("foreColorPicker1.Items53"),
            resources.GetString("foreColorPicker1.Items54"),
            resources.GetString("foreColorPicker1.Items55"),
            resources.GetString("foreColorPicker1.Items56"),
            resources.GetString("foreColorPicker1.Items57"),
            resources.GetString("foreColorPicker1.Items58"),
            resources.GetString("foreColorPicker1.Items59"),
            resources.GetString("foreColorPicker1.Items60"),
            resources.GetString("foreColorPicker1.Items61"),
            resources.GetString("foreColorPicker1.Items62"),
            resources.GetString("foreColorPicker1.Items63"),
            resources.GetString("foreColorPicker1.Items64"),
            resources.GetString("foreColorPicker1.Items65"),
            resources.GetString("foreColorPicker1.Items66"),
            resources.GetString("foreColorPicker1.Items67"),
            resources.GetString("foreColorPicker1.Items68"),
            resources.GetString("foreColorPicker1.Items69"),
            resources.GetString("foreColorPicker1.Items70"),
            resources.GetString("foreColorPicker1.Items71"),
            resources.GetString("foreColorPicker1.Items72"),
            resources.GetString("foreColorPicker1.Items73"),
            resources.GetString("foreColorPicker1.Items74"),
            resources.GetString("foreColorPicker1.Items75"),
            resources.GetString("foreColorPicker1.Items76"),
            resources.GetString("foreColorPicker1.Items77"),
            resources.GetString("foreColorPicker1.Items78"),
            resources.GetString("foreColorPicker1.Items79"),
            resources.GetString("foreColorPicker1.Items80"),
            resources.GetString("foreColorPicker1.Items81"),
            resources.GetString("foreColorPicker1.Items82"),
            resources.GetString("foreColorPicker1.Items83"),
            resources.GetString("foreColorPicker1.Items84"),
            resources.GetString("foreColorPicker1.Items85"),
            resources.GetString("foreColorPicker1.Items86"),
            resources.GetString("foreColorPicker1.Items87"),
            resources.GetString("foreColorPicker1.Items88"),
            resources.GetString("foreColorPicker1.Items89"),
            resources.GetString("foreColorPicker1.Items90"),
            resources.GetString("foreColorPicker1.Items91"),
            resources.GetString("foreColorPicker1.Items92"),
            resources.GetString("foreColorPicker1.Items93"),
            resources.GetString("foreColorPicker1.Items94"),
            resources.GetString("foreColorPicker1.Items95"),
            resources.GetString("foreColorPicker1.Items96"),
            resources.GetString("foreColorPicker1.Items97"),
            resources.GetString("foreColorPicker1.Items98"),
            resources.GetString("foreColorPicker1.Items99"),
            resources.GetString("foreColorPicker1.Items100"),
            resources.GetString("foreColorPicker1.Items101"),
            resources.GetString("foreColorPicker1.Items102"),
            resources.GetString("foreColorPicker1.Items103"),
            resources.GetString("foreColorPicker1.Items104"),
            resources.GetString("foreColorPicker1.Items105"),
            resources.GetString("foreColorPicker1.Items106"),
            resources.GetString("foreColorPicker1.Items107"),
            resources.GetString("foreColorPicker1.Items108"),
            resources.GetString("foreColorPicker1.Items109"),
            resources.GetString("foreColorPicker1.Items110"),
            resources.GetString("foreColorPicker1.Items111"),
            resources.GetString("foreColorPicker1.Items112"),
            resources.GetString("foreColorPicker1.Items113"),
            resources.GetString("foreColorPicker1.Items114"),
            resources.GetString("foreColorPicker1.Items115"),
            resources.GetString("foreColorPicker1.Items116"),
            resources.GetString("foreColorPicker1.Items117"),
            resources.GetString("foreColorPicker1.Items118"),
            resources.GetString("foreColorPicker1.Items119"),
            resources.GetString("foreColorPicker1.Items120"),
            resources.GetString("foreColorPicker1.Items121"),
            resources.GetString("foreColorPicker1.Items122"),
            resources.GetString("foreColorPicker1.Items123"),
            resources.GetString("foreColorPicker1.Items124"),
            resources.GetString("foreColorPicker1.Items125"),
            resources.GetString("foreColorPicker1.Items126"),
            resources.GetString("foreColorPicker1.Items127"),
            resources.GetString("foreColorPicker1.Items128"),
            resources.GetString("foreColorPicker1.Items129"),
            resources.GetString("foreColorPicker1.Items130"),
            resources.GetString("foreColorPicker1.Items131"),
            resources.GetString("foreColorPicker1.Items132"),
            resources.GetString("foreColorPicker1.Items133"),
            resources.GetString("foreColorPicker1.Items134"),
            resources.GetString("foreColorPicker1.Items135"),
            resources.GetString("foreColorPicker1.Items136"),
            resources.GetString("foreColorPicker1.Items137"),
            resources.GetString("foreColorPicker1.Items138"),
            resources.GetString("foreColorPicker1.Items139"),
            resources.GetString("foreColorPicker1.Items140"),
            resources.GetString("foreColorPicker1.Items141"),
            resources.GetString("foreColorPicker1.Items142"),
            resources.GetString("foreColorPicker1.Items143"),
            resources.GetString("foreColorPicker1.Items144"),
            resources.GetString("foreColorPicker1.Items145"),
            resources.GetString("foreColorPicker1.Items146"),
            resources.GetString("foreColorPicker1.Items147"),
            resources.GetString("foreColorPicker1.Items148"),
            resources.GetString("foreColorPicker1.Items149"),
            resources.GetString("foreColorPicker1.Items150"),
            resources.GetString("foreColorPicker1.Items151"),
            resources.GetString("foreColorPicker1.Items152"),
            resources.GetString("foreColorPicker1.Items153"),
            resources.GetString("foreColorPicker1.Items154"),
            resources.GetString("foreColorPicker1.Items155"),
            resources.GetString("foreColorPicker1.Items156"),
            resources.GetString("foreColorPicker1.Items157"),
            resources.GetString("foreColorPicker1.Items158"),
            resources.GetString("foreColorPicker1.Items159"),
            resources.GetString("foreColorPicker1.Items160"),
            resources.GetString("foreColorPicker1.Items161"),
            resources.GetString("foreColorPicker1.Items162"),
            resources.GetString("foreColorPicker1.Items163"),
            resources.GetString("foreColorPicker1.Items164"),
            resources.GetString("foreColorPicker1.Items165"),
            resources.GetString("foreColorPicker1.Items166"),
            resources.GetString("foreColorPicker1.Items167"),
            resources.GetString("foreColorPicker1.Items168"),
            resources.GetString("foreColorPicker1.Items169"),
            resources.GetString("foreColorPicker1.Items170"),
            resources.GetString("foreColorPicker1.Items171"),
            resources.GetString("foreColorPicker1.Items172"),
            resources.GetString("foreColorPicker1.Items173"),
            resources.GetString("foreColorPicker1.Items174"),
            resources.GetString("foreColorPicker1.Items175"),
            resources.GetString("foreColorPicker1.Items176"),
            resources.GetString("foreColorPicker1.Items177"),
            resources.GetString("foreColorPicker1.Items178"),
            resources.GetString("foreColorPicker1.Items179"),
            resources.GetString("foreColorPicker1.Items180"),
            resources.GetString("foreColorPicker1.Items181"),
            resources.GetString("foreColorPicker1.Items182"),
            resources.GetString("foreColorPicker1.Items183"),
            resources.GetString("foreColorPicker1.Items184"),
            resources.GetString("foreColorPicker1.Items185"),
            resources.GetString("foreColorPicker1.Items186"),
            resources.GetString("foreColorPicker1.Items187"),
            resources.GetString("foreColorPicker1.Items188"),
            resources.GetString("foreColorPicker1.Items189"),
            resources.GetString("foreColorPicker1.Items190"),
            resources.GetString("foreColorPicker1.Items191"),
            resources.GetString("foreColorPicker1.Items192"),
            resources.GetString("foreColorPicker1.Items193"),
            resources.GetString("foreColorPicker1.Items194"),
            resources.GetString("foreColorPicker1.Items195"),
            resources.GetString("foreColorPicker1.Items196"),
            resources.GetString("foreColorPicker1.Items197"),
            resources.GetString("foreColorPicker1.Items198"),
            resources.GetString("foreColorPicker1.Items199"),
            resources.GetString("foreColorPicker1.Items200"),
            resources.GetString("foreColorPicker1.Items201"),
            resources.GetString("foreColorPicker1.Items202"),
            resources.GetString("foreColorPicker1.Items203"),
            resources.GetString("foreColorPicker1.Items204"),
            resources.GetString("foreColorPicker1.Items205"),
            resources.GetString("foreColorPicker1.Items206"),
            resources.GetString("foreColorPicker1.Items207"),
            resources.GetString("foreColorPicker1.Items208"),
            resources.GetString("foreColorPicker1.Items209"),
            resources.GetString("foreColorPicker1.Items210"),
            resources.GetString("foreColorPicker1.Items211"),
            resources.GetString("foreColorPicker1.Items212"),
            resources.GetString("foreColorPicker1.Items213"),
            resources.GetString("foreColorPicker1.Items214"),
            resources.GetString("foreColorPicker1.Items215"),
            resources.GetString("foreColorPicker1.Items216"),
            resources.GetString("foreColorPicker1.Items217"),
            resources.GetString("foreColorPicker1.Items218"),
            resources.GetString("foreColorPicker1.Items219"),
            resources.GetString("foreColorPicker1.Items220"),
            resources.GetString("foreColorPicker1.Items221"),
            resources.GetString("foreColorPicker1.Items222"),
            resources.GetString("foreColorPicker1.Items223"),
            resources.GetString("foreColorPicker1.Items224"),
            resources.GetString("foreColorPicker1.Items225"),
            resources.GetString("foreColorPicker1.Items226"),
            resources.GetString("foreColorPicker1.Items227"),
            resources.GetString("foreColorPicker1.Items228"),
            resources.GetString("foreColorPicker1.Items229"),
            resources.GetString("foreColorPicker1.Items230"),
            resources.GetString("foreColorPicker1.Items231"),
            resources.GetString("foreColorPicker1.Items232"),
            resources.GetString("foreColorPicker1.Items233"),
            resources.GetString("foreColorPicker1.Items234"),
            resources.GetString("foreColorPicker1.Items235"),
            resources.GetString("foreColorPicker1.Items236"),
            resources.GetString("foreColorPicker1.Items237"),
            resources.GetString("foreColorPicker1.Items238"),
            resources.GetString("foreColorPicker1.Items239"),
            resources.GetString("foreColorPicker1.Items240"),
            resources.GetString("foreColorPicker1.Items241"),
            resources.GetString("foreColorPicker1.Items242"),
            resources.GetString("foreColorPicker1.Items243"),
            resources.GetString("foreColorPicker1.Items244"),
            resources.GetString("foreColorPicker1.Items245"),
            resources.GetString("foreColorPicker1.Items246"),
            resources.GetString("foreColorPicker1.Items247"),
            resources.GetString("foreColorPicker1.Items248"),
            resources.GetString("foreColorPicker1.Items249"),
            resources.GetString("foreColorPicker1.Items250"),
            resources.GetString("foreColorPicker1.Items251"),
            resources.GetString("foreColorPicker1.Items252"),
            resources.GetString("foreColorPicker1.Items253"),
            resources.GetString("foreColorPicker1.Items254"),
            resources.GetString("foreColorPicker1.Items255"),
            resources.GetString("foreColorPicker1.Items256"),
            resources.GetString("foreColorPicker1.Items257"),
            resources.GetString("foreColorPicker1.Items258"),
            resources.GetString("foreColorPicker1.Items259"),
            resources.GetString("foreColorPicker1.Items260"),
            resources.GetString("foreColorPicker1.Items261"),
            resources.GetString("foreColorPicker1.Items262"),
            resources.GetString("foreColorPicker1.Items263"),
            resources.GetString("foreColorPicker1.Items264"),
            resources.GetString("foreColorPicker1.Items265"),
            resources.GetString("foreColorPicker1.Items266"),
            resources.GetString("foreColorPicker1.Items267"),
            resources.GetString("foreColorPicker1.Items268"),
            resources.GetString("foreColorPicker1.Items269"),
            resources.GetString("foreColorPicker1.Items270"),
            resources.GetString("foreColorPicker1.Items271"),
            resources.GetString("foreColorPicker1.Items272"),
            resources.GetString("foreColorPicker1.Items273"),
            resources.GetString("foreColorPicker1.Items274"),
            resources.GetString("foreColorPicker1.Items275"),
            resources.GetString("foreColorPicker1.Items276"),
            resources.GetString("foreColorPicker1.Items277"),
            resources.GetString("foreColorPicker1.Items278"),
            resources.GetString("foreColorPicker1.Items279"),
            resources.GetString("foreColorPicker1.Items280"),
            resources.GetString("foreColorPicker1.Items281"),
            resources.GetString("foreColorPicker1.Items282"),
            resources.GetString("foreColorPicker1.Items283"),
            resources.GetString("foreColorPicker1.Items284"),
            resources.GetString("foreColorPicker1.Items285"),
            resources.GetString("foreColorPicker1.Items286"),
            resources.GetString("foreColorPicker1.Items287"),
            resources.GetString("foreColorPicker1.Items288"),
            resources.GetString("foreColorPicker1.Items289"),
            resources.GetString("foreColorPicker1.Items290"),
            resources.GetString("foreColorPicker1.Items291"),
            resources.GetString("foreColorPicker1.Items292"),
            resources.GetString("foreColorPicker1.Items293"),
            resources.GetString("foreColorPicker1.Items294"),
            resources.GetString("foreColorPicker1.Items295"),
            resources.GetString("foreColorPicker1.Items296"),
            resources.GetString("foreColorPicker1.Items297"),
            resources.GetString("foreColorPicker1.Items298"),
            resources.GetString("foreColorPicker1.Items299"),
            resources.GetString("foreColorPicker1.Items300"),
            resources.GetString("foreColorPicker1.Items301"),
            resources.GetString("foreColorPicker1.Items302"),
            resources.GetString("foreColorPicker1.Items303"),
            resources.GetString("foreColorPicker1.Items304"),
            resources.GetString("foreColorPicker1.Items305"),
            resources.GetString("foreColorPicker1.Items306"),
            resources.GetString("foreColorPicker1.Items307"),
            resources.GetString("foreColorPicker1.Items308"),
            resources.GetString("foreColorPicker1.Items309"),
            resources.GetString("foreColorPicker1.Items310"),
            resources.GetString("foreColorPicker1.Items311"),
            resources.GetString("foreColorPicker1.Items312"),
            resources.GetString("foreColorPicker1.Items313"),
            resources.GetString("foreColorPicker1.Items314"),
            resources.GetString("foreColorPicker1.Items315"),
            resources.GetString("foreColorPicker1.Items316"),
            resources.GetString("foreColorPicker1.Items317"),
            resources.GetString("foreColorPicker1.Items318"),
            resources.GetString("foreColorPicker1.Items319"),
            resources.GetString("foreColorPicker1.Items320"),
            resources.GetString("foreColorPicker1.Items321"),
            resources.GetString("foreColorPicker1.Items322"),
            resources.GetString("foreColorPicker1.Items323"),
            resources.GetString("foreColorPicker1.Items324"),
            resources.GetString("foreColorPicker1.Items325"),
            resources.GetString("foreColorPicker1.Items326"),
            resources.GetString("foreColorPicker1.Items327"),
            resources.GetString("foreColorPicker1.Items328"),
            resources.GetString("foreColorPicker1.Items329"),
            resources.GetString("foreColorPicker1.Items330"),
            resources.GetString("foreColorPicker1.Items331"),
            resources.GetString("foreColorPicker1.Items332"),
            resources.GetString("foreColorPicker1.Items333"),
            resources.GetString("foreColorPicker1.Items334"),
            resources.GetString("foreColorPicker1.Items335"),
            resources.GetString("foreColorPicker1.Items336"),
            resources.GetString("foreColorPicker1.Items337"),
            resources.GetString("foreColorPicker1.Items338"),
            resources.GetString("foreColorPicker1.Items339"),
            resources.GetString("foreColorPicker1.Items340"),
            resources.GetString("foreColorPicker1.Items341"),
            resources.GetString("foreColorPicker1.Items342"),
            resources.GetString("foreColorPicker1.Items343"),
            resources.GetString("foreColorPicker1.Items344"),
            resources.GetString("foreColorPicker1.Items345"),
            resources.GetString("foreColorPicker1.Items346"),
            resources.GetString("foreColorPicker1.Items347"),
            resources.GetString("foreColorPicker1.Items348"),
            resources.GetString("foreColorPicker1.Items349"),
            resources.GetString("foreColorPicker1.Items350"),
            resources.GetString("foreColorPicker1.Items351"),
            resources.GetString("foreColorPicker1.Items352"),
            resources.GetString("foreColorPicker1.Items353"),
            resources.GetString("foreColorPicker1.Items354"),
            resources.GetString("foreColorPicker1.Items355"),
            resources.GetString("foreColorPicker1.Items356"),
            resources.GetString("foreColorPicker1.Items357"),
            resources.GetString("foreColorPicker1.Items358"),
            resources.GetString("foreColorPicker1.Items359"),
            resources.GetString("foreColorPicker1.Items360"),
            resources.GetString("foreColorPicker1.Items361"),
            resources.GetString("foreColorPicker1.Items362"),
            resources.GetString("foreColorPicker1.Items363"),
            resources.GetString("foreColorPicker1.Items364"),
            resources.GetString("foreColorPicker1.Items365"),
            resources.GetString("foreColorPicker1.Items366"),
            resources.GetString("foreColorPicker1.Items367"),
            resources.GetString("foreColorPicker1.Items368"),
            resources.GetString("foreColorPicker1.Items369"),
            resources.GetString("foreColorPicker1.Items370"),
            resources.GetString("foreColorPicker1.Items371"),
            resources.GetString("foreColorPicker1.Items372"),
            resources.GetString("foreColorPicker1.Items373"),
            resources.GetString("foreColorPicker1.Items374"),
            resources.GetString("foreColorPicker1.Items375"),
            resources.GetString("foreColorPicker1.Items376"),
            resources.GetString("foreColorPicker1.Items377"),
            resources.GetString("foreColorPicker1.Items378"),
            resources.GetString("foreColorPicker1.Items379"),
            resources.GetString("foreColorPicker1.Items380"),
            resources.GetString("foreColorPicker1.Items381"),
            resources.GetString("foreColorPicker1.Items382"),
            resources.GetString("foreColorPicker1.Items383"),
            resources.GetString("foreColorPicker1.Items384"),
            resources.GetString("foreColorPicker1.Items385"),
            resources.GetString("foreColorPicker1.Items386"),
            resources.GetString("foreColorPicker1.Items387"),
            resources.GetString("foreColorPicker1.Items388"),
            resources.GetString("foreColorPicker1.Items389"),
            resources.GetString("foreColorPicker1.Items390"),
            resources.GetString("foreColorPicker1.Items391"),
            resources.GetString("foreColorPicker1.Items392"),
            resources.GetString("foreColorPicker1.Items393"),
            resources.GetString("foreColorPicker1.Items394"),
            resources.GetString("foreColorPicker1.Items395"),
            resources.GetString("foreColorPicker1.Items396"),
            resources.GetString("foreColorPicker1.Items397"),
            resources.GetString("foreColorPicker1.Items398"),
            resources.GetString("foreColorPicker1.Items399"),
            resources.GetString("foreColorPicker1.Items400"),
            resources.GetString("foreColorPicker1.Items401"),
            resources.GetString("foreColorPicker1.Items402"),
            resources.GetString("foreColorPicker1.Items403"),
            resources.GetString("foreColorPicker1.Items404"),
            resources.GetString("foreColorPicker1.Items405"),
            resources.GetString("foreColorPicker1.Items406"),
            resources.GetString("foreColorPicker1.Items407"),
            resources.GetString("foreColorPicker1.Items408"),
            resources.GetString("foreColorPicker1.Items409"),
            resources.GetString("foreColorPicker1.Items410"),
            resources.GetString("foreColorPicker1.Items411"),
            resources.GetString("foreColorPicker1.Items412"),
            resources.GetString("foreColorPicker1.Items413"),
            resources.GetString("foreColorPicker1.Items414"),
            resources.GetString("foreColorPicker1.Items415"),
            resources.GetString("foreColorPicker1.Items416"),
            resources.GetString("foreColorPicker1.Items417"),
            resources.GetString("foreColorPicker1.Items418"),
            resources.GetString("foreColorPicker1.Items419"),
            resources.GetString("foreColorPicker1.Items420"),
            resources.GetString("foreColorPicker1.Items421"),
            resources.GetString("foreColorPicker1.Items422"),
            resources.GetString("foreColorPicker1.Items423"),
            resources.GetString("foreColorPicker1.Items424"),
            resources.GetString("foreColorPicker1.Items425"),
            resources.GetString("foreColorPicker1.Items426"),
            resources.GetString("foreColorPicker1.Items427"),
            resources.GetString("foreColorPicker1.Items428"),
            resources.GetString("foreColorPicker1.Items429"),
            resources.GetString("foreColorPicker1.Items430"),
            resources.GetString("foreColorPicker1.Items431"),
            resources.GetString("foreColorPicker1.Items432"),
            resources.GetString("foreColorPicker1.Items433"),
            resources.GetString("foreColorPicker1.Items434"),
            resources.GetString("foreColorPicker1.Items435"),
            resources.GetString("foreColorPicker1.Items436"),
            resources.GetString("foreColorPicker1.Items437"),
            resources.GetString("foreColorPicker1.Items438"),
            resources.GetString("foreColorPicker1.Items439"),
            resources.GetString("foreColorPicker1.Items440"),
            resources.GetString("foreColorPicker1.Items441"),
            resources.GetString("foreColorPicker1.Items442"),
            resources.GetString("foreColorPicker1.Items443"),
            resources.GetString("foreColorPicker1.Items444"),
            resources.GetString("foreColorPicker1.Items445"),
            resources.GetString("foreColorPicker1.Items446"),
            resources.GetString("foreColorPicker1.Items447"),
            resources.GetString("foreColorPicker1.Items448"),
            resources.GetString("foreColorPicker1.Items449"),
            resources.GetString("foreColorPicker1.Items450"),
            resources.GetString("foreColorPicker1.Items451"),
            resources.GetString("foreColorPicker1.Items452"),
            resources.GetString("foreColorPicker1.Items453"),
            resources.GetString("foreColorPicker1.Items454"),
            resources.GetString("foreColorPicker1.Items455"),
            resources.GetString("foreColorPicker1.Items456"),
            resources.GetString("foreColorPicker1.Items457"),
            resources.GetString("foreColorPicker1.Items458"),
            resources.GetString("foreColorPicker1.Items459"),
            resources.GetString("foreColorPicker1.Items460"),
            resources.GetString("foreColorPicker1.Items461"),
            resources.GetString("foreColorPicker1.Items462"),
            resources.GetString("foreColorPicker1.Items463"),
            resources.GetString("foreColorPicker1.Items464"),
            resources.GetString("foreColorPicker1.Items465"),
            resources.GetString("foreColorPicker1.Items466"),
            resources.GetString("foreColorPicker1.Items467"),
            resources.GetString("foreColorPicker1.Items468"),
            resources.GetString("foreColorPicker1.Items469"),
            resources.GetString("foreColorPicker1.Items470"),
            resources.GetString("foreColorPicker1.Items471"),
            resources.GetString("foreColorPicker1.Items472"),
            resources.GetString("foreColorPicker1.Items473"),
            resources.GetString("foreColorPicker1.Items474"),
            resources.GetString("foreColorPicker1.Items475"),
            resources.GetString("foreColorPicker1.Items476"),
            resources.GetString("foreColorPicker1.Items477"),
            resources.GetString("foreColorPicker1.Items478"),
            resources.GetString("foreColorPicker1.Items479"),
            resources.GetString("foreColorPicker1.Items480"),
            resources.GetString("foreColorPicker1.Items481"),
            resources.GetString("foreColorPicker1.Items482"),
            resources.GetString("foreColorPicker1.Items483"),
            resources.GetString("foreColorPicker1.Items484"),
            resources.GetString("foreColorPicker1.Items485"),
            resources.GetString("foreColorPicker1.Items486"),
            resources.GetString("foreColorPicker1.Items487"),
            resources.GetString("foreColorPicker1.Items488"),
            resources.GetString("foreColorPicker1.Items489"),
            resources.GetString("foreColorPicker1.Items490"),
            resources.GetString("foreColorPicker1.Items491"),
            resources.GetString("foreColorPicker1.Items492"),
            resources.GetString("foreColorPicker1.Items493"),
            resources.GetString("foreColorPicker1.Items494"),
            resources.GetString("foreColorPicker1.Items495"),
            resources.GetString("foreColorPicker1.Items496"),
            resources.GetString("foreColorPicker1.Items497"),
            resources.GetString("foreColorPicker1.Items498"),
            resources.GetString("foreColorPicker1.Items499"),
            resources.GetString("foreColorPicker1.Items500"),
            resources.GetString("foreColorPicker1.Items501"),
            resources.GetString("foreColorPicker1.Items502"),
            resources.GetString("foreColorPicker1.Items503"),
            resources.GetString("foreColorPicker1.Items504"),
            resources.GetString("foreColorPicker1.Items505"),
            resources.GetString("foreColorPicker1.Items506"),
            resources.GetString("foreColorPicker1.Items507"),
            resources.GetString("foreColorPicker1.Items508"),
            resources.GetString("foreColorPicker1.Items509"),
            resources.GetString("foreColorPicker1.Items510"),
            resources.GetString("foreColorPicker1.Items511"),
            resources.GetString("foreColorPicker1.Items512"),
            resources.GetString("foreColorPicker1.Items513"),
            resources.GetString("foreColorPicker1.Items514"),
            resources.GetString("foreColorPicker1.Items515"),
            resources.GetString("foreColorPicker1.Items516"),
            resources.GetString("foreColorPicker1.Items517"),
            resources.GetString("foreColorPicker1.Items518"),
            resources.GetString("foreColorPicker1.Items519"),
            resources.GetString("foreColorPicker1.Items520"),
            resources.GetString("foreColorPicker1.Items521"),
            resources.GetString("foreColorPicker1.Items522"),
            resources.GetString("foreColorPicker1.Items523"),
            resources.GetString("foreColorPicker1.Items524"),
            resources.GetString("foreColorPicker1.Items525"),
            resources.GetString("foreColorPicker1.Items526"),
            resources.GetString("foreColorPicker1.Items527"),
            resources.GetString("foreColorPicker1.Items528"),
            resources.GetString("foreColorPicker1.Items529"),
            resources.GetString("foreColorPicker1.Items530"),
            resources.GetString("foreColorPicker1.Items531"),
            resources.GetString("foreColorPicker1.Items532"),
            resources.GetString("foreColorPicker1.Items533"),
            resources.GetString("foreColorPicker1.Items534"),
            resources.GetString("foreColorPicker1.Items535"),
            resources.GetString("foreColorPicker1.Items536"),
            resources.GetString("foreColorPicker1.Items537"),
            resources.GetString("foreColorPicker1.Items538"),
            resources.GetString("foreColorPicker1.Items539"),
            resources.GetString("foreColorPicker1.Items540"),
            resources.GetString("foreColorPicker1.Items541"),
            resources.GetString("foreColorPicker1.Items542"),
            resources.GetString("foreColorPicker1.Items543"),
            resources.GetString("foreColorPicker1.Items544"),
            resources.GetString("foreColorPicker1.Items545"),
            resources.GetString("foreColorPicker1.Items546"),
            resources.GetString("foreColorPicker1.Items547"),
            resources.GetString("foreColorPicker1.Items548"),
            resources.GetString("foreColorPicker1.Items549"),
            resources.GetString("foreColorPicker1.Items550"),
            resources.GetString("foreColorPicker1.Items551"),
            resources.GetString("foreColorPicker1.Items552"),
            resources.GetString("foreColorPicker1.Items553"),
            resources.GetString("foreColorPicker1.Items554"),
            resources.GetString("foreColorPicker1.Items555"),
            resources.GetString("foreColorPicker1.Items556"),
            resources.GetString("foreColorPicker1.Items557"),
            resources.GetString("foreColorPicker1.Items558"),
            resources.GetString("foreColorPicker1.Items559"),
            resources.GetString("foreColorPicker1.Items560"),
            resources.GetString("foreColorPicker1.Items561"),
            resources.GetString("foreColorPicker1.Items562"),
            resources.GetString("foreColorPicker1.Items563"),
            resources.GetString("foreColorPicker1.Items564"),
            resources.GetString("foreColorPicker1.Items565"),
            resources.GetString("foreColorPicker1.Items566"),
            resources.GetString("foreColorPicker1.Items567"),
            resources.GetString("foreColorPicker1.Items568"),
            resources.GetString("foreColorPicker1.Items569"),
            resources.GetString("foreColorPicker1.Items570"),
            resources.GetString("foreColorPicker1.Items571"),
            resources.GetString("foreColorPicker1.Items572"),
            resources.GetString("foreColorPicker1.Items573"),
            resources.GetString("foreColorPicker1.Items574"),
            resources.GetString("foreColorPicker1.Items575"),
            resources.GetString("foreColorPicker1.Items576"),
            resources.GetString("foreColorPicker1.Items577"),
            resources.GetString("foreColorPicker1.Items578"),
            resources.GetString("foreColorPicker1.Items579"),
            resources.GetString("foreColorPicker1.Items580"),
            resources.GetString("foreColorPicker1.Items581"),
            resources.GetString("foreColorPicker1.Items582"),
            resources.GetString("foreColorPicker1.Items583"),
            resources.GetString("foreColorPicker1.Items584"),
            resources.GetString("foreColorPicker1.Items585"),
            resources.GetString("foreColorPicker1.Items586"),
            resources.GetString("foreColorPicker1.Items587"),
            resources.GetString("foreColorPicker1.Items588"),
            resources.GetString("foreColorPicker1.Items589"),
            resources.GetString("foreColorPicker1.Items590"),
            resources.GetString("foreColorPicker1.Items591"),
            resources.GetString("foreColorPicker1.Items592"),
            resources.GetString("foreColorPicker1.Items593"),
            resources.GetString("foreColorPicker1.Items594"),
            resources.GetString("foreColorPicker1.Items595"),
            resources.GetString("foreColorPicker1.Items596"),
            resources.GetString("foreColorPicker1.Items597"),
            resources.GetString("foreColorPicker1.Items598"),
            resources.GetString("foreColorPicker1.Items599"),
            resources.GetString("foreColorPicker1.Items600"),
            resources.GetString("foreColorPicker1.Items601"),
            resources.GetString("foreColorPicker1.Items602"),
            resources.GetString("foreColorPicker1.Items603"),
            resources.GetString("foreColorPicker1.Items604"),
            resources.GetString("foreColorPicker1.Items605"),
            resources.GetString("foreColorPicker1.Items606"),
            resources.GetString("foreColorPicker1.Items607"),
            resources.GetString("foreColorPicker1.Items608"),
            resources.GetString("foreColorPicker1.Items609"),
            resources.GetString("foreColorPicker1.Items610"),
            resources.GetString("foreColorPicker1.Items611"),
            resources.GetString("foreColorPicker1.Items612"),
            resources.GetString("foreColorPicker1.Items613"),
            resources.GetString("foreColorPicker1.Items614"),
            resources.GetString("foreColorPicker1.Items615"),
            resources.GetString("foreColorPicker1.Items616"),
            resources.GetString("foreColorPicker1.Items617"),
            resources.GetString("foreColorPicker1.Items618"),
            resources.GetString("foreColorPicker1.Items619"),
            resources.GetString("foreColorPicker1.Items620"),
            resources.GetString("foreColorPicker1.Items621"),
            resources.GetString("foreColorPicker1.Items622"),
            resources.GetString("foreColorPicker1.Items623"),
            resources.GetString("foreColorPicker1.Items624"),
            resources.GetString("foreColorPicker1.Items625"),
            resources.GetString("foreColorPicker1.Items626"),
            resources.GetString("foreColorPicker1.Items627"),
            resources.GetString("foreColorPicker1.Items628"),
            resources.GetString("foreColorPicker1.Items629"),
            resources.GetString("foreColorPicker1.Items630"),
            resources.GetString("foreColorPicker1.Items631"),
            resources.GetString("foreColorPicker1.Items632"),
            resources.GetString("foreColorPicker1.Items633"),
            resources.GetString("foreColorPicker1.Items634"),
            resources.GetString("foreColorPicker1.Items635"),
            resources.GetString("foreColorPicker1.Items636"),
            resources.GetString("foreColorPicker1.Items637"),
            resources.GetString("foreColorPicker1.Items638"),
            resources.GetString("foreColorPicker1.Items639"),
            resources.GetString("foreColorPicker1.Items640"),
            resources.GetString("foreColorPicker1.Items641"),
            resources.GetString("foreColorPicker1.Items642"),
            resources.GetString("foreColorPicker1.Items643"),
            resources.GetString("foreColorPicker1.Items644"),
            resources.GetString("foreColorPicker1.Items645"),
            resources.GetString("foreColorPicker1.Items646"),
            resources.GetString("foreColorPicker1.Items647"),
            resources.GetString("foreColorPicker1.Items648"),
            resources.GetString("foreColorPicker1.Items649"),
            resources.GetString("foreColorPicker1.Items650"),
            resources.GetString("foreColorPicker1.Items651"),
            resources.GetString("foreColorPicker1.Items652"),
            resources.GetString("foreColorPicker1.Items653"),
            resources.GetString("foreColorPicker1.Items654"),
            resources.GetString("foreColorPicker1.Items655"),
            resources.GetString("foreColorPicker1.Items656"),
            resources.GetString("foreColorPicker1.Items657"),
            resources.GetString("foreColorPicker1.Items658"),
            resources.GetString("foreColorPicker1.Items659"),
            resources.GetString("foreColorPicker1.Items660"),
            resources.GetString("foreColorPicker1.Items661"),
            resources.GetString("foreColorPicker1.Items662"),
            resources.GetString("foreColorPicker1.Items663"),
            resources.GetString("foreColorPicker1.Items664"),
            resources.GetString("foreColorPicker1.Items665"),
            resources.GetString("foreColorPicker1.Items666"),
            resources.GetString("foreColorPicker1.Items667"),
            resources.GetString("foreColorPicker1.Items668"),
            resources.GetString("foreColorPicker1.Items669"),
            resources.GetString("foreColorPicker1.Items670"),
            resources.GetString("foreColorPicker1.Items671"),
            resources.GetString("foreColorPicker1.Items672"),
            resources.GetString("foreColorPicker1.Items673"),
            resources.GetString("foreColorPicker1.Items674"),
            resources.GetString("foreColorPicker1.Items675"),
            resources.GetString("foreColorPicker1.Items676"),
            resources.GetString("foreColorPicker1.Items677"),
            resources.GetString("foreColorPicker1.Items678"),
            resources.GetString("foreColorPicker1.Items679"),
            resources.GetString("foreColorPicker1.Items680"),
            resources.GetString("foreColorPicker1.Items681"),
            resources.GetString("foreColorPicker1.Items682"),
            resources.GetString("foreColorPicker1.Items683"),
            resources.GetString("foreColorPicker1.Items684"),
            resources.GetString("foreColorPicker1.Items685"),
            resources.GetString("foreColorPicker1.Items686"),
            resources.GetString("foreColorPicker1.Items687"),
            resources.GetString("foreColorPicker1.Items688"),
            resources.GetString("foreColorPicker1.Items689"),
            resources.GetString("foreColorPicker1.Items690"),
            resources.GetString("foreColorPicker1.Items691"),
            resources.GetString("foreColorPicker1.Items692"),
            resources.GetString("foreColorPicker1.Items693"),
            resources.GetString("foreColorPicker1.Items694"),
            resources.GetString("foreColorPicker1.Items695"),
            resources.GetString("foreColorPicker1.Items696"),
            resources.GetString("foreColorPicker1.Items697"),
            resources.GetString("foreColorPicker1.Items698"),
            resources.GetString("foreColorPicker1.Items699"),
            resources.GetString("foreColorPicker1.Items700"),
            resources.GetString("foreColorPicker1.Items701"),
            resources.GetString("foreColorPicker1.Items702"),
            resources.GetString("foreColorPicker1.Items703"),
            resources.GetString("foreColorPicker1.Items704"),
            resources.GetString("foreColorPicker1.Items705"),
            resources.GetString("foreColorPicker1.Items706"),
            resources.GetString("foreColorPicker1.Items707"),
            resources.GetString("foreColorPicker1.Items708"),
            resources.GetString("foreColorPicker1.Items709"),
            resources.GetString("foreColorPicker1.Items710"),
            resources.GetString("foreColorPicker1.Items711"),
            resources.GetString("foreColorPicker1.Items712"),
            resources.GetString("foreColorPicker1.Items713"),
            resources.GetString("foreColorPicker1.Items714"),
            resources.GetString("foreColorPicker1.Items715"),
            resources.GetString("foreColorPicker1.Items716"),
            resources.GetString("foreColorPicker1.Items717"),
            resources.GetString("foreColorPicker1.Items718"),
            resources.GetString("foreColorPicker1.Items719"),
            resources.GetString("foreColorPicker1.Items720"),
            resources.GetString("foreColorPicker1.Items721"),
            resources.GetString("foreColorPicker1.Items722"),
            resources.GetString("foreColorPicker1.Items723"),
            resources.GetString("foreColorPicker1.Items724"),
            resources.GetString("foreColorPicker1.Items725"),
            resources.GetString("foreColorPicker1.Items726"),
            resources.GetString("foreColorPicker1.Items727"),
            resources.GetString("foreColorPicker1.Items728"),
            resources.GetString("foreColorPicker1.Items729"),
            resources.GetString("foreColorPicker1.Items730"),
            resources.GetString("foreColorPicker1.Items731"),
            resources.GetString("foreColorPicker1.Items732"),
            resources.GetString("foreColorPicker1.Items733"),
            resources.GetString("foreColorPicker1.Items734"),
            resources.GetString("foreColorPicker1.Items735"),
            resources.GetString("foreColorPicker1.Items736"),
            resources.GetString("foreColorPicker1.Items737"),
            resources.GetString("foreColorPicker1.Items738"),
            resources.GetString("foreColorPicker1.Items739"),
            resources.GetString("foreColorPicker1.Items740"),
            resources.GetString("foreColorPicker1.Items741"),
            resources.GetString("foreColorPicker1.Items742"),
            resources.GetString("foreColorPicker1.Items743"),
            resources.GetString("foreColorPicker1.Items744"),
            resources.GetString("foreColorPicker1.Items745"),
            resources.GetString("foreColorPicker1.Items746"),
            resources.GetString("foreColorPicker1.Items747"),
            resources.GetString("foreColorPicker1.Items748"),
            resources.GetString("foreColorPicker1.Items749"),
            resources.GetString("foreColorPicker1.Items750"),
            resources.GetString("foreColorPicker1.Items751"),
            resources.GetString("foreColorPicker1.Items752"),
            resources.GetString("foreColorPicker1.Items753"),
            resources.GetString("foreColorPicker1.Items754"),
            resources.GetString("foreColorPicker1.Items755"),
            resources.GetString("foreColorPicker1.Items756"),
            resources.GetString("foreColorPicker1.Items757"),
            resources.GetString("foreColorPicker1.Items758"),
            resources.GetString("foreColorPicker1.Items759"),
            resources.GetString("foreColorPicker1.Items760"),
            resources.GetString("foreColorPicker1.Items761"),
            resources.GetString("foreColorPicker1.Items762"),
            resources.GetString("foreColorPicker1.Items763"),
            resources.GetString("foreColorPicker1.Items764"),
            resources.GetString("foreColorPicker1.Items765"),
            resources.GetString("foreColorPicker1.Items766"),
            resources.GetString("foreColorPicker1.Items767"),
            resources.GetString("foreColorPicker1.Items768"),
            resources.GetString("foreColorPicker1.Items769"),
            resources.GetString("foreColorPicker1.Items770"),
            resources.GetString("foreColorPicker1.Items771"),
            resources.GetString("foreColorPicker1.Items772"),
            resources.GetString("foreColorPicker1.Items773"),
            resources.GetString("foreColorPicker1.Items774"),
            resources.GetString("foreColorPicker1.Items775"),
            resources.GetString("foreColorPicker1.Items776"),
            resources.GetString("foreColorPicker1.Items777"),
            resources.GetString("foreColorPicker1.Items778"),
            resources.GetString("foreColorPicker1.Items779"),
            resources.GetString("foreColorPicker1.Items780"),
            resources.GetString("foreColorPicker1.Items781"),
            resources.GetString("foreColorPicker1.Items782"),
            resources.GetString("foreColorPicker1.Items783"),
            resources.GetString("foreColorPicker1.Items784"),
            resources.GetString("foreColorPicker1.Items785"),
            resources.GetString("foreColorPicker1.Items786"),
            resources.GetString("foreColorPicker1.Items787"),
            resources.GetString("foreColorPicker1.Items788"),
            resources.GetString("foreColorPicker1.Items789"),
            resources.GetString("foreColorPicker1.Items790"),
            resources.GetString("foreColorPicker1.Items791"),
            resources.GetString("foreColorPicker1.Items792"),
            resources.GetString("foreColorPicker1.Items793"),
            resources.GetString("foreColorPicker1.Items794"),
            resources.GetString("foreColorPicker1.Items795"),
            resources.GetString("foreColorPicker1.Items796"),
            resources.GetString("foreColorPicker1.Items797"),
            resources.GetString("foreColorPicker1.Items798"),
            resources.GetString("foreColorPicker1.Items799"),
            resources.GetString("foreColorPicker1.Items800"),
            resources.GetString("foreColorPicker1.Items801"),
            resources.GetString("foreColorPicker1.Items802"),
            resources.GetString("foreColorPicker1.Items803"),
            resources.GetString("foreColorPicker1.Items804"),
            resources.GetString("foreColorPicker1.Items805"),
            resources.GetString("foreColorPicker1.Items806"),
            resources.GetString("foreColorPicker1.Items807"),
            resources.GetString("foreColorPicker1.Items808"),
            resources.GetString("foreColorPicker1.Items809"),
            resources.GetString("foreColorPicker1.Items810"),
            resources.GetString("foreColorPicker1.Items811"),
            resources.GetString("foreColorPicker1.Items812"),
            resources.GetString("foreColorPicker1.Items813"),
            resources.GetString("foreColorPicker1.Items814"),
            resources.GetString("foreColorPicker1.Items815"),
            resources.GetString("foreColorPicker1.Items816"),
            resources.GetString("foreColorPicker1.Items817"),
            resources.GetString("foreColorPicker1.Items818"),
            resources.GetString("foreColorPicker1.Items819"),
            resources.GetString("foreColorPicker1.Items820"),
            resources.GetString("foreColorPicker1.Items821"),
            resources.GetString("foreColorPicker1.Items822"),
            resources.GetString("foreColorPicker1.Items823"),
            resources.GetString("foreColorPicker1.Items824"),
            resources.GetString("foreColorPicker1.Items825"),
            resources.GetString("foreColorPicker1.Items826"),
            resources.GetString("foreColorPicker1.Items827"),
            resources.GetString("foreColorPicker1.Items828"),
            resources.GetString("foreColorPicker1.Items829"),
            resources.GetString("foreColorPicker1.Items830"),
            resources.GetString("foreColorPicker1.Items831"),
            resources.GetString("foreColorPicker1.Items832"),
            resources.GetString("foreColorPicker1.Items833"),
            resources.GetString("foreColorPicker1.Items834"),
            resources.GetString("foreColorPicker1.Items835"),
            resources.GetString("foreColorPicker1.Items836"),
            resources.GetString("foreColorPicker1.Items837"),
            resources.GetString("foreColorPicker1.Items838"),
            resources.GetString("foreColorPicker1.Items839"),
            resources.GetString("foreColorPicker1.Items840"),
            resources.GetString("foreColorPicker1.Items841"),
            resources.GetString("foreColorPicker1.Items842"),
            resources.GetString("foreColorPicker1.Items843"),
            resources.GetString("foreColorPicker1.Items844"),
            resources.GetString("foreColorPicker1.Items845"),
            resources.GetString("foreColorPicker1.Items846"),
            resources.GetString("foreColorPicker1.Items847"),
            resources.GetString("foreColorPicker1.Items848"),
            resources.GetString("foreColorPicker1.Items849"),
            resources.GetString("foreColorPicker1.Items850"),
            resources.GetString("foreColorPicker1.Items851"),
            resources.GetString("foreColorPicker1.Items852"),
            resources.GetString("foreColorPicker1.Items853"),
            resources.GetString("foreColorPicker1.Items854"),
            resources.GetString("foreColorPicker1.Items855"),
            resources.GetString("foreColorPicker1.Items856"),
            resources.GetString("foreColorPicker1.Items857"),
            resources.GetString("foreColorPicker1.Items858"),
            resources.GetString("foreColorPicker1.Items859"),
            resources.GetString("foreColorPicker1.Items860"),
            resources.GetString("foreColorPicker1.Items861"),
            resources.GetString("foreColorPicker1.Items862"),
            resources.GetString("foreColorPicker1.Items863"),
            resources.GetString("foreColorPicker1.Items864"),
            resources.GetString("foreColorPicker1.Items865"),
            resources.GetString("foreColorPicker1.Items866"),
            resources.GetString("foreColorPicker1.Items867"),
            resources.GetString("foreColorPicker1.Items868"),
            resources.GetString("foreColorPicker1.Items869"),
            resources.GetString("foreColorPicker1.Items870"),
            resources.GetString("foreColorPicker1.Items871"),
            resources.GetString("foreColorPicker1.Items872"),
            resources.GetString("foreColorPicker1.Items873"),
            resources.GetString("foreColorPicker1.Items874"),
            resources.GetString("foreColorPicker1.Items875"),
            resources.GetString("foreColorPicker1.Items876"),
            resources.GetString("foreColorPicker1.Items877"),
            resources.GetString("foreColorPicker1.Items878"),
            resources.GetString("foreColorPicker1.Items879"),
            resources.GetString("foreColorPicker1.Items880"),
            resources.GetString("foreColorPicker1.Items881"),
            resources.GetString("foreColorPicker1.Items882"),
            resources.GetString("foreColorPicker1.Items883"),
            resources.GetString("foreColorPicker1.Items884"),
            resources.GetString("foreColorPicker1.Items885"),
            resources.GetString("foreColorPicker1.Items886"),
            resources.GetString("foreColorPicker1.Items887"),
            resources.GetString("foreColorPicker1.Items888"),
            resources.GetString("foreColorPicker1.Items889"),
            resources.GetString("foreColorPicker1.Items890"),
            resources.GetString("foreColorPicker1.Items891"),
            resources.GetString("foreColorPicker1.Items892"),
            resources.GetString("foreColorPicker1.Items893"),
            resources.GetString("foreColorPicker1.Items894"),
            resources.GetString("foreColorPicker1.Items895"),
            resources.GetString("foreColorPicker1.Items896"),
            resources.GetString("foreColorPicker1.Items897"),
            resources.GetString("foreColorPicker1.Items898"),
            resources.GetString("foreColorPicker1.Items899"),
            resources.GetString("foreColorPicker1.Items900"),
            resources.GetString("foreColorPicker1.Items901"),
            resources.GetString("foreColorPicker1.Items902"),
            resources.GetString("foreColorPicker1.Items903"),
            resources.GetString("foreColorPicker1.Items904"),
            resources.GetString("foreColorPicker1.Items905"),
            resources.GetString("foreColorPicker1.Items906"),
            resources.GetString("foreColorPicker1.Items907"),
            resources.GetString("foreColorPicker1.Items908"),
            resources.GetString("foreColorPicker1.Items909"),
            resources.GetString("foreColorPicker1.Items910"),
            resources.GetString("foreColorPicker1.Items911"),
            resources.GetString("foreColorPicker1.Items912"),
            resources.GetString("foreColorPicker1.Items913"),
            resources.GetString("foreColorPicker1.Items914"),
            resources.GetString("foreColorPicker1.Items915"),
            resources.GetString("foreColorPicker1.Items916"),
            resources.GetString("foreColorPicker1.Items917"),
            resources.GetString("foreColorPicker1.Items918"),
            resources.GetString("foreColorPicker1.Items919"),
            resources.GetString("foreColorPicker1.Items920"),
            resources.GetString("foreColorPicker1.Items921"),
            resources.GetString("foreColorPicker1.Items922"),
            resources.GetString("foreColorPicker1.Items923"),
            resources.GetString("foreColorPicker1.Items924"),
            resources.GetString("foreColorPicker1.Items925"),
            resources.GetString("foreColorPicker1.Items926"),
            resources.GetString("foreColorPicker1.Items927"),
            resources.GetString("foreColorPicker1.Items928"),
            resources.GetString("foreColorPicker1.Items929"),
            resources.GetString("foreColorPicker1.Items930"),
            resources.GetString("foreColorPicker1.Items931"),
            resources.GetString("foreColorPicker1.Items932"),
            resources.GetString("foreColorPicker1.Items933"),
            resources.GetString("foreColorPicker1.Items934"),
            resources.GetString("foreColorPicker1.Items935"),
            resources.GetString("foreColorPicker1.Items936"),
            resources.GetString("foreColorPicker1.Items937"),
            resources.GetString("foreColorPicker1.Items938"),
            resources.GetString("foreColorPicker1.Items939"),
            resources.GetString("foreColorPicker1.Items940"),
            resources.GetString("foreColorPicker1.Items941"),
            resources.GetString("foreColorPicker1.Items942"),
            resources.GetString("foreColorPicker1.Items943"),
            resources.GetString("foreColorPicker1.Items944"),
            resources.GetString("foreColorPicker1.Items945"),
            resources.GetString("foreColorPicker1.Items946"),
            resources.GetString("foreColorPicker1.Items947"),
            resources.GetString("foreColorPicker1.Items948"),
            resources.GetString("foreColorPicker1.Items949"),
            resources.GetString("foreColorPicker1.Items950"),
            resources.GetString("foreColorPicker1.Items951"),
            resources.GetString("foreColorPicker1.Items952"),
            resources.GetString("foreColorPicker1.Items953"),
            resources.GetString("foreColorPicker1.Items954"),
            resources.GetString("foreColorPicker1.Items955"),
            resources.GetString("foreColorPicker1.Items956"),
            resources.GetString("foreColorPicker1.Items957"),
            resources.GetString("foreColorPicker1.Items958"),
            resources.GetString("foreColorPicker1.Items959"),
            resources.GetString("foreColorPicker1.Items960"),
            resources.GetString("foreColorPicker1.Items961"),
            resources.GetString("foreColorPicker1.Items962"),
            resources.GetString("foreColorPicker1.Items963"),
            resources.GetString("foreColorPicker1.Items964"),
            resources.GetString("foreColorPicker1.Items965"),
            resources.GetString("foreColorPicker1.Items966"),
            resources.GetString("foreColorPicker1.Items967"),
            resources.GetString("foreColorPicker1.Items968"),
            resources.GetString("foreColorPicker1.Items969"),
            resources.GetString("foreColorPicker1.Items970"),
            resources.GetString("foreColorPicker1.Items971"),
            resources.GetString("foreColorPicker1.Items972"),
            resources.GetString("foreColorPicker1.Items973"),
            resources.GetString("foreColorPicker1.Items974"),
            resources.GetString("foreColorPicker1.Items975"),
            resources.GetString("foreColorPicker1.Items976"),
            resources.GetString("foreColorPicker1.Items977"),
            resources.GetString("foreColorPicker1.Items978"),
            resources.GetString("foreColorPicker1.Items979"),
            resources.GetString("foreColorPicker1.Items980"),
            resources.GetString("foreColorPicker1.Items981"),
            resources.GetString("foreColorPicker1.Items982"),
            resources.GetString("foreColorPicker1.Items983"),
            resources.GetString("foreColorPicker1.Items984"),
            resources.GetString("foreColorPicker1.Items985"),
            resources.GetString("foreColorPicker1.Items986"),
            resources.GetString("foreColorPicker1.Items987"),
            resources.GetString("foreColorPicker1.Items988"),
            resources.GetString("foreColorPicker1.Items989"),
            resources.GetString("foreColorPicker1.Items990"),
            resources.GetString("foreColorPicker1.Items991"),
            resources.GetString("foreColorPicker1.Items992"),
            resources.GetString("foreColorPicker1.Items993"),
            resources.GetString("foreColorPicker1.Items994"),
            resources.GetString("foreColorPicker1.Items995"),
            resources.GetString("foreColorPicker1.Items996"),
            resources.GetString("foreColorPicker1.Items997"),
            resources.GetString("foreColorPicker1.Items998"),
            resources.GetString("foreColorPicker1.Items999"),
            resources.GetString("foreColorPicker1.Items1000"),
            resources.GetString("foreColorPicker1.Items1001"),
            resources.GetString("foreColorPicker1.Items1002"),
            resources.GetString("foreColorPicker1.Items1003"),
            resources.GetString("foreColorPicker1.Items1004"),
            resources.GetString("foreColorPicker1.Items1005"),
            resources.GetString("foreColorPicker1.Items1006"),
            resources.GetString("foreColorPicker1.Items1007"),
            resources.GetString("foreColorPicker1.Items1008"),
            resources.GetString("foreColorPicker1.Items1009"),
            resources.GetString("foreColorPicker1.Items1010"),
            resources.GetString("foreColorPicker1.Items1011"),
            resources.GetString("foreColorPicker1.Items1012"),
            resources.GetString("foreColorPicker1.Items1013"),
            resources.GetString("foreColorPicker1.Items1014"),
            resources.GetString("foreColorPicker1.Items1015"),
            resources.GetString("foreColorPicker1.Items1016"),
            resources.GetString("foreColorPicker1.Items1017"),
            resources.GetString("foreColorPicker1.Items1018"),
            resources.GetString("foreColorPicker1.Items1019"),
            resources.GetString("foreColorPicker1.Items1020"),
            resources.GetString("foreColorPicker1.Items1021"),
            resources.GetString("foreColorPicker1.Items1022"),
            resources.GetString("foreColorPicker1.Items1023"),
            resources.GetString("foreColorPicker1.Items1024"),
            resources.GetString("foreColorPicker1.Items1025"),
            resources.GetString("foreColorPicker1.Items1026"),
            resources.GetString("foreColorPicker1.Items1027"),
            resources.GetString("foreColorPicker1.Items1028"),
            resources.GetString("foreColorPicker1.Items1029"),
            resources.GetString("foreColorPicker1.Items1030"),
            resources.GetString("foreColorPicker1.Items1031"),
            resources.GetString("foreColorPicker1.Items1032"),
            resources.GetString("foreColorPicker1.Items1033"),
            resources.GetString("foreColorPicker1.Items1034"),
            resources.GetString("foreColorPicker1.Items1035"),
            resources.GetString("foreColorPicker1.Items1036"),
            resources.GetString("foreColorPicker1.Items1037"),
            resources.GetString("foreColorPicker1.Items1038"),
            resources.GetString("foreColorPicker1.Items1039"),
            resources.GetString("foreColorPicker1.Items1040"),
            resources.GetString("foreColorPicker1.Items1041"),
            resources.GetString("foreColorPicker1.Items1042"),
            resources.GetString("foreColorPicker1.Items1043"),
            resources.GetString("foreColorPicker1.Items1044"),
            resources.GetString("foreColorPicker1.Items1045"),
            resources.GetString("foreColorPicker1.Items1046"),
            resources.GetString("foreColorPicker1.Items1047"),
            resources.GetString("foreColorPicker1.Items1048"),
            resources.GetString("foreColorPicker1.Items1049"),
            resources.GetString("foreColorPicker1.Items1050"),
            resources.GetString("foreColorPicker1.Items1051"),
            resources.GetString("foreColorPicker1.Items1052"),
            resources.GetString("foreColorPicker1.Items1053"),
            resources.GetString("foreColorPicker1.Items1054"),
            resources.GetString("foreColorPicker1.Items1055"),
            resources.GetString("foreColorPicker1.Items1056"),
            resources.GetString("foreColorPicker1.Items1057"),
            resources.GetString("foreColorPicker1.Items1058"),
            resources.GetString("foreColorPicker1.Items1059"),
            resources.GetString("foreColorPicker1.Items1060"),
            resources.GetString("foreColorPicker1.Items1061"),
            resources.GetString("foreColorPicker1.Items1062"),
            resources.GetString("foreColorPicker1.Items1063"),
            resources.GetString("foreColorPicker1.Items1064"),
            resources.GetString("foreColorPicker1.Items1065"),
            resources.GetString("foreColorPicker1.Items1066"),
            resources.GetString("foreColorPicker1.Items1067"),
            resources.GetString("foreColorPicker1.Items1068"),
            resources.GetString("foreColorPicker1.Items1069"),
            resources.GetString("foreColorPicker1.Items1070"),
            resources.GetString("foreColorPicker1.Items1071"),
            resources.GetString("foreColorPicker1.Items1072"),
            resources.GetString("foreColorPicker1.Items1073"),
            resources.GetString("foreColorPicker1.Items1074"),
            resources.GetString("foreColorPicker1.Items1075"),
            resources.GetString("foreColorPicker1.Items1076"),
            resources.GetString("foreColorPicker1.Items1077"),
            resources.GetString("foreColorPicker1.Items1078"),
            resources.GetString("foreColorPicker1.Items1079"),
            resources.GetString("foreColorPicker1.Items1080"),
            resources.GetString("foreColorPicker1.Items1081"),
            resources.GetString("foreColorPicker1.Items1082"),
            resources.GetString("foreColorPicker1.Items1083"),
            resources.GetString("foreColorPicker1.Items1084"),
            resources.GetString("foreColorPicker1.Items1085"),
            resources.GetString("foreColorPicker1.Items1086"),
            resources.GetString("foreColorPicker1.Items1087"),
            resources.GetString("foreColorPicker1.Items1088"),
            resources.GetString("foreColorPicker1.Items1089"),
            resources.GetString("foreColorPicker1.Items1090"),
            resources.GetString("foreColorPicker1.Items1091"),
            resources.GetString("foreColorPicker1.Items1092"),
            resources.GetString("foreColorPicker1.Items1093"),
            resources.GetString("foreColorPicker1.Items1094"),
            resources.GetString("foreColorPicker1.Items1095"),
            resources.GetString("foreColorPicker1.Items1096"),
            resources.GetString("foreColorPicker1.Items1097"),
            resources.GetString("foreColorPicker1.Items1098"),
            resources.GetString("foreColorPicker1.Items1099"),
            resources.GetString("foreColorPicker1.Items1100"),
            resources.GetString("foreColorPicker1.Items1101"),
            resources.GetString("foreColorPicker1.Items1102"),
            resources.GetString("foreColorPicker1.Items1103"),
            resources.GetString("foreColorPicker1.Items1104"),
            resources.GetString("foreColorPicker1.Items1105"),
            resources.GetString("foreColorPicker1.Items1106"),
            resources.GetString("foreColorPicker1.Items1107"),
            resources.GetString("foreColorPicker1.Items1108"),
            resources.GetString("foreColorPicker1.Items1109"),
            resources.GetString("foreColorPicker1.Items1110"),
            resources.GetString("foreColorPicker1.Items1111"),
            resources.GetString("foreColorPicker1.Items1112"),
            resources.GetString("foreColorPicker1.Items1113"),
            resources.GetString("foreColorPicker1.Items1114"),
            resources.GetString("foreColorPicker1.Items1115"),
            resources.GetString("foreColorPicker1.Items1116"),
            resources.GetString("foreColorPicker1.Items1117"),
            resources.GetString("foreColorPicker1.Items1118"),
            resources.GetString("foreColorPicker1.Items1119"),
            resources.GetString("foreColorPicker1.Items1120"),
            resources.GetString("foreColorPicker1.Items1121"),
            resources.GetString("foreColorPicker1.Items1122"),
            resources.GetString("foreColorPicker1.Items1123"),
            resources.GetString("foreColorPicker1.Items1124"),
            resources.GetString("foreColorPicker1.Items1125"),
            resources.GetString("foreColorPicker1.Items1126"),
            resources.GetString("foreColorPicker1.Items1127"),
            resources.GetString("foreColorPicker1.Items1128"),
            resources.GetString("foreColorPicker1.Items1129"),
            resources.GetString("foreColorPicker1.Items1130"),
            resources.GetString("foreColorPicker1.Items1131"),
            resources.GetString("foreColorPicker1.Items1132"),
            resources.GetString("foreColorPicker1.Items1133"),
            resources.GetString("foreColorPicker1.Items1134"),
            resources.GetString("foreColorPicker1.Items1135"),
            resources.GetString("foreColorPicker1.Items1136"),
            resources.GetString("foreColorPicker1.Items1137"),
            resources.GetString("foreColorPicker1.Items1138"),
            resources.GetString("foreColorPicker1.Items1139"),
            resources.GetString("foreColorPicker1.Items1140"),
            resources.GetString("foreColorPicker1.Items1141"),
            resources.GetString("foreColorPicker1.Items1142"),
            resources.GetString("foreColorPicker1.Items1143"),
            resources.GetString("foreColorPicker1.Items1144"),
            resources.GetString("foreColorPicker1.Items1145"),
            resources.GetString("foreColorPicker1.Items1146"),
            resources.GetString("foreColorPicker1.Items1147"),
            resources.GetString("foreColorPicker1.Items1148"),
            resources.GetString("foreColorPicker1.Items1149"),
            resources.GetString("foreColorPicker1.Items1150"),
            resources.GetString("foreColorPicker1.Items1151"),
            resources.GetString("foreColorPicker1.Items1152"),
            resources.GetString("foreColorPicker1.Items1153"),
            resources.GetString("foreColorPicker1.Items1154"),
            resources.GetString("foreColorPicker1.Items1155"),
            resources.GetString("foreColorPicker1.Items1156"),
            resources.GetString("foreColorPicker1.Items1157"),
            resources.GetString("foreColorPicker1.Items1158"),
            resources.GetString("foreColorPicker1.Items1159"),
            resources.GetString("foreColorPicker1.Items1160"),
            resources.GetString("foreColorPicker1.Items1161"),
            resources.GetString("foreColorPicker1.Items1162"),
            resources.GetString("foreColorPicker1.Items1163"),
            resources.GetString("foreColorPicker1.Items1164"),
            resources.GetString("foreColorPicker1.Items1165"),
            resources.GetString("foreColorPicker1.Items1166"),
            resources.GetString("foreColorPicker1.Items1167"),
            resources.GetString("foreColorPicker1.Items1168"),
            resources.GetString("foreColorPicker1.Items1169"),
            resources.GetString("foreColorPicker1.Items1170"),
            resources.GetString("foreColorPicker1.Items1171"),
            resources.GetString("foreColorPicker1.Items1172"),
            resources.GetString("foreColorPicker1.Items1173"),
            resources.GetString("foreColorPicker1.Items1174"),
            resources.GetString("foreColorPicker1.Items1175"),
            resources.GetString("foreColorPicker1.Items1176"),
            resources.GetString("foreColorPicker1.Items1177"),
            resources.GetString("foreColorPicker1.Items1178"),
            resources.GetString("foreColorPicker1.Items1179"),
            resources.GetString("foreColorPicker1.Items1180"),
            resources.GetString("foreColorPicker1.Items1181"),
            resources.GetString("foreColorPicker1.Items1182"),
            resources.GetString("foreColorPicker1.Items1183"),
            resources.GetString("foreColorPicker1.Items1184"),
            resources.GetString("foreColorPicker1.Items1185"),
            resources.GetString("foreColorPicker1.Items1186"),
            resources.GetString("foreColorPicker1.Items1187"),
            resources.GetString("foreColorPicker1.Items1188"),
            resources.GetString("foreColorPicker1.Items1189"),
            resources.GetString("foreColorPicker1.Items1190"),
            resources.GetString("foreColorPicker1.Items1191"),
            resources.GetString("foreColorPicker1.Items1192"),
            resources.GetString("foreColorPicker1.Items1193"),
            resources.GetString("foreColorPicker1.Items1194"),
            resources.GetString("foreColorPicker1.Items1195"),
            resources.GetString("foreColorPicker1.Items1196"),
            resources.GetString("foreColorPicker1.Items1197"),
            resources.GetString("foreColorPicker1.Items1198"),
            resources.GetString("foreColorPicker1.Items1199"),
            resources.GetString("foreColorPicker1.Items1200"),
            resources.GetString("foreColorPicker1.Items1201"),
            resources.GetString("foreColorPicker1.Items1202"),
            resources.GetString("foreColorPicker1.Items1203"),
            resources.GetString("foreColorPicker1.Items1204"),
            resources.GetString("foreColorPicker1.Items1205"),
            resources.GetString("foreColorPicker1.Items1206"),
            resources.GetString("foreColorPicker1.Items1207"),
            resources.GetString("foreColorPicker1.Items1208"),
            resources.GetString("foreColorPicker1.Items1209"),
            resources.GetString("foreColorPicker1.Items1210"),
            resources.GetString("foreColorPicker1.Items1211"),
            resources.GetString("foreColorPicker1.Items1212"),
            resources.GetString("foreColorPicker1.Items1213"),
            resources.GetString("foreColorPicker1.Items1214"),
            resources.GetString("foreColorPicker1.Items1215"),
            resources.GetString("foreColorPicker1.Items1216"),
            resources.GetString("foreColorPicker1.Items1217"),
            resources.GetString("foreColorPicker1.Items1218"),
            resources.GetString("foreColorPicker1.Items1219"),
            resources.GetString("foreColorPicker1.Items1220"),
            resources.GetString("foreColorPicker1.Items1221"),
            resources.GetString("foreColorPicker1.Items1222"),
            resources.GetString("foreColorPicker1.Items1223"),
            resources.GetString("foreColorPicker1.Items1224"),
            resources.GetString("foreColorPicker1.Items1225"),
            resources.GetString("foreColorPicker1.Items1226"),
            resources.GetString("foreColorPicker1.Items1227"),
            resources.GetString("foreColorPicker1.Items1228"),
            resources.GetString("foreColorPicker1.Items1229"),
            resources.GetString("foreColorPicker1.Items1230"),
            resources.GetString("foreColorPicker1.Items1231"),
            resources.GetString("foreColorPicker1.Items1232"),
            resources.GetString("foreColorPicker1.Items1233"),
            resources.GetString("foreColorPicker1.Items1234"),
            resources.GetString("foreColorPicker1.Items1235"),
            resources.GetString("foreColorPicker1.Items1236"),
            resources.GetString("foreColorPicker1.Items1237"),
            resources.GetString("foreColorPicker1.Items1238"),
            resources.GetString("foreColorPicker1.Items1239"),
            resources.GetString("foreColorPicker1.Items1240"),
            resources.GetString("foreColorPicker1.Items1241"),
            resources.GetString("foreColorPicker1.Items1242"),
            resources.GetString("foreColorPicker1.Items1243"),
            resources.GetString("foreColorPicker1.Items1244"),
            resources.GetString("foreColorPicker1.Items1245"),
            resources.GetString("foreColorPicker1.Items1246"),
            resources.GetString("foreColorPicker1.Items1247"),
            resources.GetString("foreColorPicker1.Items1248"),
            resources.GetString("foreColorPicker1.Items1249"),
            resources.GetString("foreColorPicker1.Items1250"),
            resources.GetString("foreColorPicker1.Items1251"),
            resources.GetString("foreColorPicker1.Items1252"),
            resources.GetString("foreColorPicker1.Items1253"),
            resources.GetString("foreColorPicker1.Items1254"),
            resources.GetString("foreColorPicker1.Items1255"),
            resources.GetString("foreColorPicker1.Items1256"),
            resources.GetString("foreColorPicker1.Items1257"),
            resources.GetString("foreColorPicker1.Items1258"),
            resources.GetString("foreColorPicker1.Items1259"),
            resources.GetString("foreColorPicker1.Items1260"),
            resources.GetString("foreColorPicker1.Items1261"),
            resources.GetString("foreColorPicker1.Items1262"),
            resources.GetString("foreColorPicker1.Items1263"),
            resources.GetString("foreColorPicker1.Items1264"),
            resources.GetString("foreColorPicker1.Items1265"),
            resources.GetString("foreColorPicker1.Items1266"),
            resources.GetString("foreColorPicker1.Items1267"),
            resources.GetString("foreColorPicker1.Items1268"),
            resources.GetString("foreColorPicker1.Items1269"),
            resources.GetString("foreColorPicker1.Items1270"),
            resources.GetString("foreColorPicker1.Items1271"),
            resources.GetString("foreColorPicker1.Items1272"),
            resources.GetString("foreColorPicker1.Items1273"),
            resources.GetString("foreColorPicker1.Items1274"),
            resources.GetString("foreColorPicker1.Items1275"),
            resources.GetString("foreColorPicker1.Items1276"),
            resources.GetString("foreColorPicker1.Items1277"),
            resources.GetString("foreColorPicker1.Items1278"),
            resources.GetString("foreColorPicker1.Items1279"),
            resources.GetString("foreColorPicker1.Items1280"),
            resources.GetString("foreColorPicker1.Items1281"),
            resources.GetString("foreColorPicker1.Items1282"),
            resources.GetString("foreColorPicker1.Items1283"),
            resources.GetString("foreColorPicker1.Items1284"),
            resources.GetString("foreColorPicker1.Items1285"),
            resources.GetString("foreColorPicker1.Items1286"),
            resources.GetString("foreColorPicker1.Items1287"),
            resources.GetString("foreColorPicker1.Items1288"),
            resources.GetString("foreColorPicker1.Items1289"),
            resources.GetString("foreColorPicker1.Items1290"),
            resources.GetString("foreColorPicker1.Items1291"),
            resources.GetString("foreColorPicker1.Items1292"),
            resources.GetString("foreColorPicker1.Items1293"),
            resources.GetString("foreColorPicker1.Items1294"),
            resources.GetString("foreColorPicker1.Items1295"),
            resources.GetString("foreColorPicker1.Items1296"),
            resources.GetString("foreColorPicker1.Items1297"),
            resources.GetString("foreColorPicker1.Items1298"),
            resources.GetString("foreColorPicker1.Items1299"),
            resources.GetString("foreColorPicker1.Items1300"),
            resources.GetString("foreColorPicker1.Items1301"),
            resources.GetString("foreColorPicker1.Items1302"),
            resources.GetString("foreColorPicker1.Items1303"),
            resources.GetString("foreColorPicker1.Items1304"),
            resources.GetString("foreColorPicker1.Items1305"),
            resources.GetString("foreColorPicker1.Items1306"),
            resources.GetString("foreColorPicker1.Items1307"),
            resources.GetString("foreColorPicker1.Items1308"),
            resources.GetString("foreColorPicker1.Items1309"),
            resources.GetString("foreColorPicker1.Items1310"),
            resources.GetString("foreColorPicker1.Items1311"),
            resources.GetString("foreColorPicker1.Items1312"),
            resources.GetString("foreColorPicker1.Items1313"),
            resources.GetString("foreColorPicker1.Items1314"),
            resources.GetString("foreColorPicker1.Items1315"),
            resources.GetString("foreColorPicker1.Items1316"),
            resources.GetString("foreColorPicker1.Items1317"),
            resources.GetString("foreColorPicker1.Items1318"),
            resources.GetString("foreColorPicker1.Items1319"),
            resources.GetString("foreColorPicker1.Items1320"),
            resources.GetString("foreColorPicker1.Items1321"),
            resources.GetString("foreColorPicker1.Items1322"),
            resources.GetString("foreColorPicker1.Items1323"),
            resources.GetString("foreColorPicker1.Items1324"),
            resources.GetString("foreColorPicker1.Items1325"),
            resources.GetString("foreColorPicker1.Items1326"),
            resources.GetString("foreColorPicker1.Items1327"),
            resources.GetString("foreColorPicker1.Items1328"),
            resources.GetString("foreColorPicker1.Items1329"),
            resources.GetString("foreColorPicker1.Items1330"),
            resources.GetString("foreColorPicker1.Items1331"),
            resources.GetString("foreColorPicker1.Items1332"),
            resources.GetString("foreColorPicker1.Items1333"),
            resources.GetString("foreColorPicker1.Items1334"),
            resources.GetString("foreColorPicker1.Items1335"),
            resources.GetString("foreColorPicker1.Items1336"),
            resources.GetString("foreColorPicker1.Items1337"),
            resources.GetString("foreColorPicker1.Items1338"),
            resources.GetString("foreColorPicker1.Items1339"),
            resources.GetString("foreColorPicker1.Items1340"),
            resources.GetString("foreColorPicker1.Items1341"),
            resources.GetString("foreColorPicker1.Items1342"),
            resources.GetString("foreColorPicker1.Items1343"),
            resources.GetString("foreColorPicker1.Items1344"),
            resources.GetString("foreColorPicker1.Items1345"),
            resources.GetString("foreColorPicker1.Items1346"),
            resources.GetString("foreColorPicker1.Items1347"),
            resources.GetString("foreColorPicker1.Items1348"),
            resources.GetString("foreColorPicker1.Items1349"),
            resources.GetString("foreColorPicker1.Items1350"),
            resources.GetString("foreColorPicker1.Items1351"),
            resources.GetString("foreColorPicker1.Items1352"),
            resources.GetString("foreColorPicker1.Items1353"),
            resources.GetString("foreColorPicker1.Items1354"),
            resources.GetString("foreColorPicker1.Items1355"),
            resources.GetString("foreColorPicker1.Items1356"),
            resources.GetString("foreColorPicker1.Items1357"),
            resources.GetString("foreColorPicker1.Items1358"),
            resources.GetString("foreColorPicker1.Items1359"),
            resources.GetString("foreColorPicker1.Items1360"),
            resources.GetString("foreColorPicker1.Items1361"),
            resources.GetString("foreColorPicker1.Items1362"),
            resources.GetString("foreColorPicker1.Items1363"),
            resources.GetString("foreColorPicker1.Items1364"),
            resources.GetString("foreColorPicker1.Items1365"),
            resources.GetString("foreColorPicker1.Items1366"),
            resources.GetString("foreColorPicker1.Items1367"),
            resources.GetString("foreColorPicker1.Items1368"),
            resources.GetString("foreColorPicker1.Items1369"),
            resources.GetString("foreColorPicker1.Items1370"),
            resources.GetString("foreColorPicker1.Items1371"),
            resources.GetString("foreColorPicker1.Items1372"),
            resources.GetString("foreColorPicker1.Items1373"),
            resources.GetString("foreColorPicker1.Items1374"),
            resources.GetString("foreColorPicker1.Items1375"),
            resources.GetString("foreColorPicker1.Items1376"),
            resources.GetString("foreColorPicker1.Items1377"),
            resources.GetString("foreColorPicker1.Items1378"),
            resources.GetString("foreColorPicker1.Items1379"),
            resources.GetString("foreColorPicker1.Items1380"),
            resources.GetString("foreColorPicker1.Items1381"),
            resources.GetString("foreColorPicker1.Items1382"),
            resources.GetString("foreColorPicker1.Items1383"),
            resources.GetString("foreColorPicker1.Items1384"),
            resources.GetString("foreColorPicker1.Items1385"),
            resources.GetString("foreColorPicker1.Items1386"),
            resources.GetString("foreColorPicker1.Items1387"),
            resources.GetString("foreColorPicker1.Items1388"),
            resources.GetString("foreColorPicker1.Items1389"),
            resources.GetString("foreColorPicker1.Items1390"),
            resources.GetString("foreColorPicker1.Items1391"),
            resources.GetString("foreColorPicker1.Items1392"),
            resources.GetString("foreColorPicker1.Items1393"),
            resources.GetString("foreColorPicker1.Items1394"),
            resources.GetString("foreColorPicker1.Items1395"),
            resources.GetString("foreColorPicker1.Items1396"),
            resources.GetString("foreColorPicker1.Items1397"),
            resources.GetString("foreColorPicker1.Items1398"),
            resources.GetString("foreColorPicker1.Items1399"),
            resources.GetString("foreColorPicker1.Items1400"),
            resources.GetString("foreColorPicker1.Items1401"),
            resources.GetString("foreColorPicker1.Items1402"),
            resources.GetString("foreColorPicker1.Items1403"),
            resources.GetString("foreColorPicker1.Items1404"),
            resources.GetString("foreColorPicker1.Items1405"),
            resources.GetString("foreColorPicker1.Items1406"),
            resources.GetString("foreColorPicker1.Items1407"),
            resources.GetString("foreColorPicker1.Items1408"),
            resources.GetString("foreColorPicker1.Items1409"),
            resources.GetString("foreColorPicker1.Items1410"),
            resources.GetString("foreColorPicker1.Items1411"),
            resources.GetString("foreColorPicker1.Items1412"),
            resources.GetString("foreColorPicker1.Items1413"),
            resources.GetString("foreColorPicker1.Items1414"),
            resources.GetString("foreColorPicker1.Items1415"),
            resources.GetString("foreColorPicker1.Items1416"),
            resources.GetString("foreColorPicker1.Items1417"),
            resources.GetString("foreColorPicker1.Items1418"),
            resources.GetString("foreColorPicker1.Items1419"),
            resources.GetString("foreColorPicker1.Items1420"),
            resources.GetString("foreColorPicker1.Items1421"),
            resources.GetString("foreColorPicker1.Items1422"),
            resources.GetString("foreColorPicker1.Items1423"),
            resources.GetString("foreColorPicker1.Items1424"),
            resources.GetString("foreColorPicker1.Items1425"),
            resources.GetString("foreColorPicker1.Items1426"),
            resources.GetString("foreColorPicker1.Items1427"),
            resources.GetString("foreColorPicker1.Items1428"),
            resources.GetString("foreColorPicker1.Items1429"),
            resources.GetString("foreColorPicker1.Items1430"),
            resources.GetString("foreColorPicker1.Items1431"),
            resources.GetString("foreColorPicker1.Items1432"),
            resources.GetString("foreColorPicker1.Items1433"),
            resources.GetString("foreColorPicker1.Items1434"),
            resources.GetString("foreColorPicker1.Items1435"),
            resources.GetString("foreColorPicker1.Items1436"),
            resources.GetString("foreColorPicker1.Items1437"),
            resources.GetString("foreColorPicker1.Items1438"),
            resources.GetString("foreColorPicker1.Items1439"),
            resources.GetString("foreColorPicker1.Items1440"),
            resources.GetString("foreColorPicker1.Items1441"),
            resources.GetString("foreColorPicker1.Items1442"),
            resources.GetString("foreColorPicker1.Items1443"),
            resources.GetString("foreColorPicker1.Items1444"),
            resources.GetString("foreColorPicker1.Items1445"),
            resources.GetString("foreColorPicker1.Items1446"),
            resources.GetString("foreColorPicker1.Items1447"),
            resources.GetString("foreColorPicker1.Items1448"),
            resources.GetString("foreColorPicker1.Items1449"),
            resources.GetString("foreColorPicker1.Items1450"),
            resources.GetString("foreColorPicker1.Items1451"),
            resources.GetString("foreColorPicker1.Items1452"),
            resources.GetString("foreColorPicker1.Items1453"),
            resources.GetString("foreColorPicker1.Items1454"),
            resources.GetString("foreColorPicker1.Items1455"),
            resources.GetString("foreColorPicker1.Items1456"),
            resources.GetString("foreColorPicker1.Items1457"),
            resources.GetString("foreColorPicker1.Items1458"),
            resources.GetString("foreColorPicker1.Items1459"),
            resources.GetString("foreColorPicker1.Items1460"),
            resources.GetString("foreColorPicker1.Items1461"),
            resources.GetString("foreColorPicker1.Items1462"),
            resources.GetString("foreColorPicker1.Items1463"),
            resources.GetString("foreColorPicker1.Items1464"),
            resources.GetString("foreColorPicker1.Items1465"),
            resources.GetString("foreColorPicker1.Items1466"),
            resources.GetString("foreColorPicker1.Items1467"),
            resources.GetString("foreColorPicker1.Items1468"),
            resources.GetString("foreColorPicker1.Items1469"),
            resources.GetString("foreColorPicker1.Items1470"),
            resources.GetString("foreColorPicker1.Items1471"),
            resources.GetString("foreColorPicker1.Items1472"),
            resources.GetString("foreColorPicker1.Items1473"),
            resources.GetString("foreColorPicker1.Items1474"),
            resources.GetString("foreColorPicker1.Items1475"),
            resources.GetString("foreColorPicker1.Items1476"),
            resources.GetString("foreColorPicker1.Items1477"),
            resources.GetString("foreColorPicker1.Items1478"),
            resources.GetString("foreColorPicker1.Items1479"),
            resources.GetString("foreColorPicker1.Items1480"),
            resources.GetString("foreColorPicker1.Items1481"),
            resources.GetString("foreColorPicker1.Items1482"),
            resources.GetString("foreColorPicker1.Items1483"),
            resources.GetString("foreColorPicker1.Items1484"),
            resources.GetString("foreColorPicker1.Items1485"),
            resources.GetString("foreColorPicker1.Items1486"),
            resources.GetString("foreColorPicker1.Items1487"),
            resources.GetString("foreColorPicker1.Items1488"),
            resources.GetString("foreColorPicker1.Items1489"),
            resources.GetString("foreColorPicker1.Items1490"),
            resources.GetString("foreColorPicker1.Items1491"),
            resources.GetString("foreColorPicker1.Items1492"),
            resources.GetString("foreColorPicker1.Items1493"),
            resources.GetString("foreColorPicker1.Items1494"),
            resources.GetString("foreColorPicker1.Items1495"),
            resources.GetString("foreColorPicker1.Items1496"),
            resources.GetString("foreColorPicker1.Items1497"),
            resources.GetString("foreColorPicker1.Items1498"),
            resources.GetString("foreColorPicker1.Items1499"),
            resources.GetString("foreColorPicker1.Items1500"),
            resources.GetString("foreColorPicker1.Items1501"),
            resources.GetString("foreColorPicker1.Items1502"),
            resources.GetString("foreColorPicker1.Items1503"),
            resources.GetString("foreColorPicker1.Items1504"),
            resources.GetString("foreColorPicker1.Items1505"),
            resources.GetString("foreColorPicker1.Items1506"),
            resources.GetString("foreColorPicker1.Items1507"),
            resources.GetString("foreColorPicker1.Items1508"),
            resources.GetString("foreColorPicker1.Items1509"),
            resources.GetString("foreColorPicker1.Items1510"),
            resources.GetString("foreColorPicker1.Items1511"),
            resources.GetString("foreColorPicker1.Items1512"),
            resources.GetString("foreColorPicker1.Items1513"),
            resources.GetString("foreColorPicker1.Items1514"),
            resources.GetString("foreColorPicker1.Items1515"),
            resources.GetString("foreColorPicker1.Items1516"),
            resources.GetString("foreColorPicker1.Items1517"),
            resources.GetString("foreColorPicker1.Items1518"),
            resources.GetString("foreColorPicker1.Items1519"),
            resources.GetString("foreColorPicker1.Items1520"),
            resources.GetString("foreColorPicker1.Items1521"),
            resources.GetString("foreColorPicker1.Items1522"),
            resources.GetString("foreColorPicker1.Items1523"),
            resources.GetString("foreColorPicker1.Items1524"),
            resources.GetString("foreColorPicker1.Items1525"),
            resources.GetString("foreColorPicker1.Items1526"),
            resources.GetString("foreColorPicker1.Items1527"),
            resources.GetString("foreColorPicker1.Items1528"),
            resources.GetString("foreColorPicker1.Items1529"),
            resources.GetString("foreColorPicker1.Items1530"),
            resources.GetString("foreColorPicker1.Items1531"),
            resources.GetString("foreColorPicker1.Items1532"),
            resources.GetString("foreColorPicker1.Items1533"),
            resources.GetString("foreColorPicker1.Items1534"),
            resources.GetString("foreColorPicker1.Items1535"),
            resources.GetString("foreColorPicker1.Items1536"),
            resources.GetString("foreColorPicker1.Items1537"),
            resources.GetString("foreColorPicker1.Items1538"),
            resources.GetString("foreColorPicker1.Items1539"),
            resources.GetString("foreColorPicker1.Items1540"),
            resources.GetString("foreColorPicker1.Items1541"),
            resources.GetString("foreColorPicker1.Items1542"),
            resources.GetString("foreColorPicker1.Items1543"),
            resources.GetString("foreColorPicker1.Items1544"),
            resources.GetString("foreColorPicker1.Items1545"),
            resources.GetString("foreColorPicker1.Items1546"),
            resources.GetString("foreColorPicker1.Items1547"),
            resources.GetString("foreColorPicker1.Items1548"),
            resources.GetString("foreColorPicker1.Items1549"),
            resources.GetString("foreColorPicker1.Items1550"),
            resources.GetString("foreColorPicker1.Items1551"),
            resources.GetString("foreColorPicker1.Items1552"),
            resources.GetString("foreColorPicker1.Items1553"),
            resources.GetString("foreColorPicker1.Items1554"),
            resources.GetString("foreColorPicker1.Items1555"),
            resources.GetString("foreColorPicker1.Items1556"),
            resources.GetString("foreColorPicker1.Items1557"),
            resources.GetString("foreColorPicker1.Items1558"),
            resources.GetString("foreColorPicker1.Items1559"),
            resources.GetString("foreColorPicker1.Items1560"),
            resources.GetString("foreColorPicker1.Items1561"),
            resources.GetString("foreColorPicker1.Items1562"),
            resources.GetString("foreColorPicker1.Items1563"),
            resources.GetString("foreColorPicker1.Items1564"),
            resources.GetString("foreColorPicker1.Items1565"),
            resources.GetString("foreColorPicker1.Items1566"),
            resources.GetString("foreColorPicker1.Items1567"),
            resources.GetString("foreColorPicker1.Items1568"),
            resources.GetString("foreColorPicker1.Items1569"),
            resources.GetString("foreColorPicker1.Items1570"),
            resources.GetString("foreColorPicker1.Items1571"),
            resources.GetString("foreColorPicker1.Items1572"),
            resources.GetString("foreColorPicker1.Items1573"),
            resources.GetString("foreColorPicker1.Items1574"),
            resources.GetString("foreColorPicker1.Items1575"),
            resources.GetString("foreColorPicker1.Items1576"),
            resources.GetString("foreColorPicker1.Items1577"),
            resources.GetString("foreColorPicker1.Items1578"),
            resources.GetString("foreColorPicker1.Items1579"),
            resources.GetString("foreColorPicker1.Items1580"),
            resources.GetString("foreColorPicker1.Items1581"),
            resources.GetString("foreColorPicker1.Items1582"),
            resources.GetString("foreColorPicker1.Items1583"),
            resources.GetString("foreColorPicker1.Items1584"),
            resources.GetString("foreColorPicker1.Items1585"),
            resources.GetString("foreColorPicker1.Items1586"),
            resources.GetString("foreColorPicker1.Items1587"),
            resources.GetString("foreColorPicker1.Items1588"),
            resources.GetString("foreColorPicker1.Items1589"),
            resources.GetString("foreColorPicker1.Items1590"),
            resources.GetString("foreColorPicker1.Items1591"),
            resources.GetString("foreColorPicker1.Items1592"),
            resources.GetString("foreColorPicker1.Items1593"),
            resources.GetString("foreColorPicker1.Items1594"),
            resources.GetString("foreColorPicker1.Items1595"),
            resources.GetString("foreColorPicker1.Items1596"),
            resources.GetString("foreColorPicker1.Items1597"),
            resources.GetString("foreColorPicker1.Items1598"),
            resources.GetString("foreColorPicker1.Items1599"),
            resources.GetString("foreColorPicker1.Items1600"),
            resources.GetString("foreColorPicker1.Items1601"),
            resources.GetString("foreColorPicker1.Items1602"),
            resources.GetString("foreColorPicker1.Items1603"),
            resources.GetString("foreColorPicker1.Items1604"),
            resources.GetString("foreColorPicker1.Items1605"),
            resources.GetString("foreColorPicker1.Items1606"),
            resources.GetString("foreColorPicker1.Items1607"),
            resources.GetString("foreColorPicker1.Items1608"),
            resources.GetString("foreColorPicker1.Items1609"),
            resources.GetString("foreColorPicker1.Items1610"),
            resources.GetString("foreColorPicker1.Items1611"),
            resources.GetString("foreColorPicker1.Items1612"),
            resources.GetString("foreColorPicker1.Items1613"),
            resources.GetString("foreColorPicker1.Items1614"),
            resources.GetString("foreColorPicker1.Items1615"),
            resources.GetString("foreColorPicker1.Items1616"),
            resources.GetString("foreColorPicker1.Items1617"),
            resources.GetString("foreColorPicker1.Items1618"),
            resources.GetString("foreColorPicker1.Items1619"),
            resources.GetString("foreColorPicker1.Items1620"),
            resources.GetString("foreColorPicker1.Items1621"),
            resources.GetString("foreColorPicker1.Items1622"),
            resources.GetString("foreColorPicker1.Items1623"),
            resources.GetString("foreColorPicker1.Items1624"),
            resources.GetString("foreColorPicker1.Items1625"),
            resources.GetString("foreColorPicker1.Items1626"),
            resources.GetString("foreColorPicker1.Items1627"),
            resources.GetString("foreColorPicker1.Items1628"),
            resources.GetString("foreColorPicker1.Items1629"),
            resources.GetString("foreColorPicker1.Items1630"),
            resources.GetString("foreColorPicker1.Items1631"),
            resources.GetString("foreColorPicker1.Items1632"),
            resources.GetString("foreColorPicker1.Items1633"),
            resources.GetString("foreColorPicker1.Items1634"),
            resources.GetString("foreColorPicker1.Items1635"),
            resources.GetString("foreColorPicker1.Items1636"),
            resources.GetString("foreColorPicker1.Items1637"),
            resources.GetString("foreColorPicker1.Items1638"),
            resources.GetString("foreColorPicker1.Items1639"),
            resources.GetString("foreColorPicker1.Items1640"),
            resources.GetString("foreColorPicker1.Items1641"),
            resources.GetString("foreColorPicker1.Items1642"),
            resources.GetString("foreColorPicker1.Items1643"),
            resources.GetString("foreColorPicker1.Items1644"),
            resources.GetString("foreColorPicker1.Items1645"),
            resources.GetString("foreColorPicker1.Items1646"),
            resources.GetString("foreColorPicker1.Items1647"),
            resources.GetString("foreColorPicker1.Items1648"),
            resources.GetString("foreColorPicker1.Items1649"),
            resources.GetString("foreColorPicker1.Items1650"),
            resources.GetString("foreColorPicker1.Items1651"),
            resources.GetString("foreColorPicker1.Items1652"),
            resources.GetString("foreColorPicker1.Items1653"),
            resources.GetString("foreColorPicker1.Items1654"),
            resources.GetString("foreColorPicker1.Items1655"),
            resources.GetString("foreColorPicker1.Items1656"),
            resources.GetString("foreColorPicker1.Items1657"),
            resources.GetString("foreColorPicker1.Items1658"),
            resources.GetString("foreColorPicker1.Items1659"),
            resources.GetString("foreColorPicker1.Items1660"),
            resources.GetString("foreColorPicker1.Items1661"),
            resources.GetString("foreColorPicker1.Items1662"),
            resources.GetString("foreColorPicker1.Items1663"),
            resources.GetString("foreColorPicker1.Items1664"),
            resources.GetString("foreColorPicker1.Items1665"),
            resources.GetString("foreColorPicker1.Items1666"),
            resources.GetString("foreColorPicker1.Items1667"),
            resources.GetString("foreColorPicker1.Items1668"),
            resources.GetString("foreColorPicker1.Items1669"),
            resources.GetString("foreColorPicker1.Items1670"),
            resources.GetString("foreColorPicker1.Items1671"),
            resources.GetString("foreColorPicker1.Items1672"),
            resources.GetString("foreColorPicker1.Items1673"),
            resources.GetString("foreColorPicker1.Items1674"),
            resources.GetString("foreColorPicker1.Items1675"),
            resources.GetString("foreColorPicker1.Items1676"),
            resources.GetString("foreColorPicker1.Items1677"),
            resources.GetString("foreColorPicker1.Items1678"),
            resources.GetString("foreColorPicker1.Items1679"),
            resources.GetString("foreColorPicker1.Items1680"),
            resources.GetString("foreColorPicker1.Items1681"),
            resources.GetString("foreColorPicker1.Items1682"),
            resources.GetString("foreColorPicker1.Items1683"),
            resources.GetString("foreColorPicker1.Items1684"),
            resources.GetString("foreColorPicker1.Items1685"),
            resources.GetString("foreColorPicker1.Items1686"),
            resources.GetString("foreColorPicker1.Items1687"),
            resources.GetString("foreColorPicker1.Items1688"),
            resources.GetString("foreColorPicker1.Items1689"),
            resources.GetString("foreColorPicker1.Items1690"),
            resources.GetString("foreColorPicker1.Items1691"),
            resources.GetString("foreColorPicker1.Items1692"),
            resources.GetString("foreColorPicker1.Items1693"),
            resources.GetString("foreColorPicker1.Items1694"),
            resources.GetString("foreColorPicker1.Items1695"),
            resources.GetString("foreColorPicker1.Items1696"),
            resources.GetString("foreColorPicker1.Items1697"),
            resources.GetString("foreColorPicker1.Items1698"),
            resources.GetString("foreColorPicker1.Items1699"),
            resources.GetString("foreColorPicker1.Items1700"),
            resources.GetString("foreColorPicker1.Items1701"),
            resources.GetString("foreColorPicker1.Items1702"),
            resources.GetString("foreColorPicker1.Items1703"),
            resources.GetString("foreColorPicker1.Items1704"),
            resources.GetString("foreColorPicker1.Items1705"),
            resources.GetString("foreColorPicker1.Items1706"),
            resources.GetString("foreColorPicker1.Items1707"),
            resources.GetString("foreColorPicker1.Items1708"),
            resources.GetString("foreColorPicker1.Items1709"),
            resources.GetString("foreColorPicker1.Items1710"),
            resources.GetString("foreColorPicker1.Items1711"),
            resources.GetString("foreColorPicker1.Items1712"),
            resources.GetString("foreColorPicker1.Items1713"),
            resources.GetString("foreColorPicker1.Items1714"),
            resources.GetString("foreColorPicker1.Items1715"),
            resources.GetString("foreColorPicker1.Items1716"),
            resources.GetString("foreColorPicker1.Items1717"),
            resources.GetString("foreColorPicker1.Items1718"),
            resources.GetString("foreColorPicker1.Items1719"),
            resources.GetString("foreColorPicker1.Items1720"),
            resources.GetString("foreColorPicker1.Items1721"),
            resources.GetString("foreColorPicker1.Items1722"),
            resources.GetString("foreColorPicker1.Items1723"),
            resources.GetString("foreColorPicker1.Items1724"),
            resources.GetString("foreColorPicker1.Items1725"),
            resources.GetString("foreColorPicker1.Items1726"),
            resources.GetString("foreColorPicker1.Items1727"),
            resources.GetString("foreColorPicker1.Items1728"),
            resources.GetString("foreColorPicker1.Items1729"),
            resources.GetString("foreColorPicker1.Items1730"),
            resources.GetString("foreColorPicker1.Items1731"),
            resources.GetString("foreColorPicker1.Items1732"),
            resources.GetString("foreColorPicker1.Items1733"),
            resources.GetString("foreColorPicker1.Items1734"),
            resources.GetString("foreColorPicker1.Items1735"),
            resources.GetString("foreColorPicker1.Items1736"),
            resources.GetString("foreColorPicker1.Items1737"),
            resources.GetString("foreColorPicker1.Items1738"),
            resources.GetString("foreColorPicker1.Items1739"),
            resources.GetString("foreColorPicker1.Items1740"),
            resources.GetString("foreColorPicker1.Items1741"),
            resources.GetString("foreColorPicker1.Items1742"),
            resources.GetString("foreColorPicker1.Items1743"),
            resources.GetString("foreColorPicker1.Items1744"),
            resources.GetString("foreColorPicker1.Items1745"),
            resources.GetString("foreColorPicker1.Items1746"),
            resources.GetString("foreColorPicker1.Items1747"),
            resources.GetString("foreColorPicker1.Items1748"),
            resources.GetString("foreColorPicker1.Items1749"),
            resources.GetString("foreColorPicker1.Items1750"),
            resources.GetString("foreColorPicker1.Items1751"),
            resources.GetString("foreColorPicker1.Items1752"),
            resources.GetString("foreColorPicker1.Items1753"),
            resources.GetString("foreColorPicker1.Items1754"),
            resources.GetString("foreColorPicker1.Items1755"),
            resources.GetString("foreColorPicker1.Items1756"),
            resources.GetString("foreColorPicker1.Items1757"),
            resources.GetString("foreColorPicker1.Items1758"),
            resources.GetString("foreColorPicker1.Items1759"),
            resources.GetString("foreColorPicker1.Items1760"),
            resources.GetString("foreColorPicker1.Items1761"),
            resources.GetString("foreColorPicker1.Items1762"),
            resources.GetString("foreColorPicker1.Items1763"),
            resources.GetString("foreColorPicker1.Items1764"),
            resources.GetString("foreColorPicker1.Items1765"),
            resources.GetString("foreColorPicker1.Items1766"),
            resources.GetString("foreColorPicker1.Items1767"),
            resources.GetString("foreColorPicker1.Items1768"),
            resources.GetString("foreColorPicker1.Items1769"),
            resources.GetString("foreColorPicker1.Items1770"),
            resources.GetString("foreColorPicker1.Items1771"),
            resources.GetString("foreColorPicker1.Items1772"),
            resources.GetString("foreColorPicker1.Items1773"),
            resources.GetString("foreColorPicker1.Items1774"),
            resources.GetString("foreColorPicker1.Items1775"),
            resources.GetString("foreColorPicker1.Items1776"),
            resources.GetString("foreColorPicker1.Items1777"),
            resources.GetString("foreColorPicker1.Items1778"),
            resources.GetString("foreColorPicker1.Items1779"),
            resources.GetString("foreColorPicker1.Items1780"),
            resources.GetString("foreColorPicker1.Items1781"),
            resources.GetString("foreColorPicker1.Items1782"),
            resources.GetString("foreColorPicker1.Items1783"),
            resources.GetString("foreColorPicker1.Items1784"),
            resources.GetString("foreColorPicker1.Items1785"),
            resources.GetString("foreColorPicker1.Items1786"),
            resources.GetString("foreColorPicker1.Items1787"),
            resources.GetString("foreColorPicker1.Items1788"),
            resources.GetString("foreColorPicker1.Items1789"),
            resources.GetString("foreColorPicker1.Items1790"),
            resources.GetString("foreColorPicker1.Items1791"),
            resources.GetString("foreColorPicker1.Items1792"),
            resources.GetString("foreColorPicker1.Items1793"),
            resources.GetString("foreColorPicker1.Items1794"),
            resources.GetString("foreColorPicker1.Items1795"),
            resources.GetString("foreColorPicker1.Items1796"),
            resources.GetString("foreColorPicker1.Items1797"),
            resources.GetString("foreColorPicker1.Items1798"),
            resources.GetString("foreColorPicker1.Items1799"),
            resources.GetString("foreColorPicker1.Items1800"),
            resources.GetString("foreColorPicker1.Items1801"),
            resources.GetString("foreColorPicker1.Items1802"),
            resources.GetString("foreColorPicker1.Items1803"),
            resources.GetString("foreColorPicker1.Items1804"),
            resources.GetString("foreColorPicker1.Items1805"),
            resources.GetString("foreColorPicker1.Items1806"),
            resources.GetString("foreColorPicker1.Items1807"),
            resources.GetString("foreColorPicker1.Items1808"),
            resources.GetString("foreColorPicker1.Items1809"),
            resources.GetString("foreColorPicker1.Items1810"),
            resources.GetString("foreColorPicker1.Items1811"),
            resources.GetString("foreColorPicker1.Items1812"),
            resources.GetString("foreColorPicker1.Items1813"),
            resources.GetString("foreColorPicker1.Items1814"),
            resources.GetString("foreColorPicker1.Items1815"),
            resources.GetString("foreColorPicker1.Items1816"),
            resources.GetString("foreColorPicker1.Items1817"),
            resources.GetString("foreColorPicker1.Items1818"),
            resources.GetString("foreColorPicker1.Items1819"),
            resources.GetString("foreColorPicker1.Items1820"),
            resources.GetString("foreColorPicker1.Items1821"),
            resources.GetString("foreColorPicker1.Items1822"),
            resources.GetString("foreColorPicker1.Items1823"),
            resources.GetString("foreColorPicker1.Items1824"),
            resources.GetString("foreColorPicker1.Items1825"),
            resources.GetString("foreColorPicker1.Items1826"),
            resources.GetString("foreColorPicker1.Items1827"),
            resources.GetString("foreColorPicker1.Items1828"),
            resources.GetString("foreColorPicker1.Items1829"),
            resources.GetString("foreColorPicker1.Items1830"),
            resources.GetString("foreColorPicker1.Items1831"),
            resources.GetString("foreColorPicker1.Items1832"),
            resources.GetString("foreColorPicker1.Items1833"),
            resources.GetString("foreColorPicker1.Items1834"),
            resources.GetString("foreColorPicker1.Items1835"),
            resources.GetString("foreColorPicker1.Items1836"),
            resources.GetString("foreColorPicker1.Items1837"),
            resources.GetString("foreColorPicker1.Items1838"),
            resources.GetString("foreColorPicker1.Items1839"),
            resources.GetString("foreColorPicker1.Items1840"),
            resources.GetString("foreColorPicker1.Items1841"),
            resources.GetString("foreColorPicker1.Items1842"),
            resources.GetString("foreColorPicker1.Items1843"),
            resources.GetString("foreColorPicker1.Items1844"),
            resources.GetString("foreColorPicker1.Items1845"),
            resources.GetString("foreColorPicker1.Items1846"),
            resources.GetString("foreColorPicker1.Items1847"),
            resources.GetString("foreColorPicker1.Items1848"),
            resources.GetString("foreColorPicker1.Items1849"),
            resources.GetString("foreColorPicker1.Items1850"),
            resources.GetString("foreColorPicker1.Items1851"),
            resources.GetString("foreColorPicker1.Items1852"),
            resources.GetString("foreColorPicker1.Items1853"),
            resources.GetString("foreColorPicker1.Items1854"),
            resources.GetString("foreColorPicker1.Items1855"),
            resources.GetString("foreColorPicker1.Items1856"),
            resources.GetString("foreColorPicker1.Items1857"),
            resources.GetString("foreColorPicker1.Items1858"),
            resources.GetString("foreColorPicker1.Items1859"),
            resources.GetString("foreColorPicker1.Items1860"),
            resources.GetString("foreColorPicker1.Items1861"),
            resources.GetString("foreColorPicker1.Items1862"),
            resources.GetString("foreColorPicker1.Items1863"),
            resources.GetString("foreColorPicker1.Items1864"),
            resources.GetString("foreColorPicker1.Items1865"),
            resources.GetString("foreColorPicker1.Items1866"),
            resources.GetString("foreColorPicker1.Items1867"),
            resources.GetString("foreColorPicker1.Items1868"),
            resources.GetString("foreColorPicker1.Items1869"),
            resources.GetString("foreColorPicker1.Items1870"),
            resources.GetString("foreColorPicker1.Items1871"),
            resources.GetString("foreColorPicker1.Items1872"),
            resources.GetString("foreColorPicker1.Items1873"),
            resources.GetString("foreColorPicker1.Items1874"),
            resources.GetString("foreColorPicker1.Items1875"),
            resources.GetString("foreColorPicker1.Items1876"),
            resources.GetString("foreColorPicker1.Items1877"),
            resources.GetString("foreColorPicker1.Items1878"),
            resources.GetString("foreColorPicker1.Items1879"),
            resources.GetString("foreColorPicker1.Items1880"),
            resources.GetString("foreColorPicker1.Items1881"),
            resources.GetString("foreColorPicker1.Items1882"),
            resources.GetString("foreColorPicker1.Items1883"),
            resources.GetString("foreColorPicker1.Items1884"),
            resources.GetString("foreColorPicker1.Items1885"),
            resources.GetString("foreColorPicker1.Items1886"),
            resources.GetString("foreColorPicker1.Items1887"),
            resources.GetString("foreColorPicker1.Items1888"),
            resources.GetString("foreColorPicker1.Items1889"),
            resources.GetString("foreColorPicker1.Items1890"),
            resources.GetString("foreColorPicker1.Items1891"),
            resources.GetString("foreColorPicker1.Items1892"),
            resources.GetString("foreColorPicker1.Items1893"),
            resources.GetString("foreColorPicker1.Items1894"),
            resources.GetString("foreColorPicker1.Items1895"),
            resources.GetString("foreColorPicker1.Items1896"),
            resources.GetString("foreColorPicker1.Items1897"),
            resources.GetString("foreColorPicker1.Items1898"),
            resources.GetString("foreColorPicker1.Items1899"),
            resources.GetString("foreColorPicker1.Items1900"),
            resources.GetString("foreColorPicker1.Items1901"),
            resources.GetString("foreColorPicker1.Items1902"),
            resources.GetString("foreColorPicker1.Items1903"),
            resources.GetString("foreColorPicker1.Items1904"),
            resources.GetString("foreColorPicker1.Items1905"),
            resources.GetString("foreColorPicker1.Items1906"),
            resources.GetString("foreColorPicker1.Items1907"),
            resources.GetString("foreColorPicker1.Items1908"),
            resources.GetString("foreColorPicker1.Items1909"),
            resources.GetString("foreColorPicker1.Items1910"),
            resources.GetString("foreColorPicker1.Items1911"),
            resources.GetString("foreColorPicker1.Items1912"),
            resources.GetString("foreColorPicker1.Items1913"),
            resources.GetString("foreColorPicker1.Items1914"),
            resources.GetString("foreColorPicker1.Items1915"),
            resources.GetString("foreColorPicker1.Items1916"),
            resources.GetString("foreColorPicker1.Items1917"),
            resources.GetString("foreColorPicker1.Items1918"),
            resources.GetString("foreColorPicker1.Items1919"),
            resources.GetString("foreColorPicker1.Items1920"),
            resources.GetString("foreColorPicker1.Items1921"),
            resources.GetString("foreColorPicker1.Items1922"),
            resources.GetString("foreColorPicker1.Items1923"),
            resources.GetString("foreColorPicker1.Items1924"),
            resources.GetString("foreColorPicker1.Items1925"),
            resources.GetString("foreColorPicker1.Items1926"),
            resources.GetString("foreColorPicker1.Items1927"),
            resources.GetString("foreColorPicker1.Items1928"),
            resources.GetString("foreColorPicker1.Items1929"),
            resources.GetString("foreColorPicker1.Items1930"),
            resources.GetString("foreColorPicker1.Items1931"),
            resources.GetString("foreColorPicker1.Items1932"),
            resources.GetString("foreColorPicker1.Items1933"),
            resources.GetString("foreColorPicker1.Items1934"),
            resources.GetString("foreColorPicker1.Items1935"),
            resources.GetString("foreColorPicker1.Items1936"),
            resources.GetString("foreColorPicker1.Items1937"),
            resources.GetString("foreColorPicker1.Items1938"),
            resources.GetString("foreColorPicker1.Items1939"),
            resources.GetString("foreColorPicker1.Items1940"),
            resources.GetString("foreColorPicker1.Items1941"),
            resources.GetString("foreColorPicker1.Items1942"),
            resources.GetString("foreColorPicker1.Items1943"),
            resources.GetString("foreColorPicker1.Items1944"),
            resources.GetString("foreColorPicker1.Items1945"),
            resources.GetString("foreColorPicker1.Items1946"),
            resources.GetString("foreColorPicker1.Items1947"),
            resources.GetString("foreColorPicker1.Items1948"),
            resources.GetString("foreColorPicker1.Items1949"),
            resources.GetString("foreColorPicker1.Items1950"),
            resources.GetString("foreColorPicker1.Items1951"),
            resources.GetString("foreColorPicker1.Items1952"),
            resources.GetString("foreColorPicker1.Items1953"),
            resources.GetString("foreColorPicker1.Items1954"),
            resources.GetString("foreColorPicker1.Items1955"),
            resources.GetString("foreColorPicker1.Items1956"),
            resources.GetString("foreColorPicker1.Items1957"),
            resources.GetString("foreColorPicker1.Items1958"),
            resources.GetString("foreColorPicker1.Items1959"),
            resources.GetString("foreColorPicker1.Items1960"),
            resources.GetString("foreColorPicker1.Items1961"),
            resources.GetString("foreColorPicker1.Items1962"),
            resources.GetString("foreColorPicker1.Items1963"),
            resources.GetString("foreColorPicker1.Items1964"),
            resources.GetString("foreColorPicker1.Items1965"),
            resources.GetString("foreColorPicker1.Items1966"),
            resources.GetString("foreColorPicker1.Items1967"),
            resources.GetString("foreColorPicker1.Items1968"),
            resources.GetString("foreColorPicker1.Items1969"),
            resources.GetString("foreColorPicker1.Items1970"),
            resources.GetString("foreColorPicker1.Items1971"),
            resources.GetString("foreColorPicker1.Items1972"),
            resources.GetString("foreColorPicker1.Items1973"),
            resources.GetString("foreColorPicker1.Items1974"),
            resources.GetString("foreColorPicker1.Items1975"),
            resources.GetString("foreColorPicker1.Items1976"),
            resources.GetString("foreColorPicker1.Items1977"),
            resources.GetString("foreColorPicker1.Items1978"),
            resources.GetString("foreColorPicker1.Items1979"),
            resources.GetString("foreColorPicker1.Items1980"),
            resources.GetString("foreColorPicker1.Items1981"),
            resources.GetString("foreColorPicker1.Items1982"),
            resources.GetString("foreColorPicker1.Items1983"),
            resources.GetString("foreColorPicker1.Items1984"),
            resources.GetString("foreColorPicker1.Items1985"),
            resources.GetString("foreColorPicker1.Items1986"),
            resources.GetString("foreColorPicker1.Items1987"),
            resources.GetString("foreColorPicker1.Items1988"),
            resources.GetString("foreColorPicker1.Items1989"),
            resources.GetString("foreColorPicker1.Items1990"),
            resources.GetString("foreColorPicker1.Items1991"),
            resources.GetString("foreColorPicker1.Items1992"),
            resources.GetString("foreColorPicker1.Items1993"),
            resources.GetString("foreColorPicker1.Items1994"),
            resources.GetString("foreColorPicker1.Items1995"),
            resources.GetString("foreColorPicker1.Items1996"),
            resources.GetString("foreColorPicker1.Items1997"),
            resources.GetString("foreColorPicker1.Items1998"),
            resources.GetString("foreColorPicker1.Items1999"),
            resources.GetString("foreColorPicker1.Items2000"),
            resources.GetString("foreColorPicker1.Items2001"),
            resources.GetString("foreColorPicker1.Items2002"),
            resources.GetString("foreColorPicker1.Items2003"),
            resources.GetString("foreColorPicker1.Items2004"),
            resources.GetString("foreColorPicker1.Items2005"),
            resources.GetString("foreColorPicker1.Items2006"),
            resources.GetString("foreColorPicker1.Items2007"),
            resources.GetString("foreColorPicker1.Items2008"),
            resources.GetString("foreColorPicker1.Items2009"),
            resources.GetString("foreColorPicker1.Items2010"),
            resources.GetString("foreColorPicker1.Items2011"),
            resources.GetString("foreColorPicker1.Items2012"),
            resources.GetString("foreColorPicker1.Items2013"),
            resources.GetString("foreColorPicker1.Items2014"),
            resources.GetString("foreColorPicker1.Items2015"),
            resources.GetString("foreColorPicker1.Items2016"),
            resources.GetString("foreColorPicker1.Items2017"),
            resources.GetString("foreColorPicker1.Items2018"),
            resources.GetString("foreColorPicker1.Items2019"),
            resources.GetString("foreColorPicker1.Items2020"),
            resources.GetString("foreColorPicker1.Items2021"),
            resources.GetString("foreColorPicker1.Items2022"),
            resources.GetString("foreColorPicker1.Items2023"),
            resources.GetString("foreColorPicker1.Items2024"),
            resources.GetString("foreColorPicker1.Items2025"),
            resources.GetString("foreColorPicker1.Items2026"),
            resources.GetString("foreColorPicker1.Items2027"),
            resources.GetString("foreColorPicker1.Items2028"),
            resources.GetString("foreColorPicker1.Items2029"),
            resources.GetString("foreColorPicker1.Items2030"),
            resources.GetString("foreColorPicker1.Items2031"),
            resources.GetString("foreColorPicker1.Items2032"),
            resources.GetString("foreColorPicker1.Items2033"),
            resources.GetString("foreColorPicker1.Items2034"),
            resources.GetString("foreColorPicker1.Items2035"),
            resources.GetString("foreColorPicker1.Items2036"),
            resources.GetString("foreColorPicker1.Items2037"),
            resources.GetString("foreColorPicker1.Items2038"),
            resources.GetString("foreColorPicker1.Items2039"),
            resources.GetString("foreColorPicker1.Items2040"),
            resources.GetString("foreColorPicker1.Items2041"),
            resources.GetString("foreColorPicker1.Items2042"),
            resources.GetString("foreColorPicker1.Items2043"),
            resources.GetString("foreColorPicker1.Items2044"),
            resources.GetString("foreColorPicker1.Items2045"),
            resources.GetString("foreColorPicker1.Items2046"),
            resources.GetString("foreColorPicker1.Items2047"),
            resources.GetString("foreColorPicker1.Items2048"),
            resources.GetString("foreColorPicker1.Items2049"),
            resources.GetString("foreColorPicker1.Items2050"),
            resources.GetString("foreColorPicker1.Items2051"),
            resources.GetString("foreColorPicker1.Items2052"),
            resources.GetString("foreColorPicker1.Items2053"),
            resources.GetString("foreColorPicker1.Items2054"),
            resources.GetString("foreColorPicker1.Items2055"),
            resources.GetString("foreColorPicker1.Items2056"),
            resources.GetString("foreColorPicker1.Items2057"),
            resources.GetString("foreColorPicker1.Items2058"),
            resources.GetString("foreColorPicker1.Items2059"),
            resources.GetString("foreColorPicker1.Items2060"),
            resources.GetString("foreColorPicker1.Items2061"),
            resources.GetString("foreColorPicker1.Items2062"),
            resources.GetString("foreColorPicker1.Items2063"),
            resources.GetString("foreColorPicker1.Items2064"),
            resources.GetString("foreColorPicker1.Items2065"),
            resources.GetString("foreColorPicker1.Items2066"),
            resources.GetString("foreColorPicker1.Items2067"),
            resources.GetString("foreColorPicker1.Items2068"),
            resources.GetString("foreColorPicker1.Items2069"),
            resources.GetString("foreColorPicker1.Items2070"),
            resources.GetString("foreColorPicker1.Items2071"),
            resources.GetString("foreColorPicker1.Items2072"),
            resources.GetString("foreColorPicker1.Items2073"),
            resources.GetString("foreColorPicker1.Items2074"),
            resources.GetString("foreColorPicker1.Items2075"),
            resources.GetString("foreColorPicker1.Items2076"),
            resources.GetString("foreColorPicker1.Items2077"),
            resources.GetString("foreColorPicker1.Items2078"),
            resources.GetString("foreColorPicker1.Items2079"),
            resources.GetString("foreColorPicker1.Items2080"),
            resources.GetString("foreColorPicker1.Items2081"),
            resources.GetString("foreColorPicker1.Items2082"),
            resources.GetString("foreColorPicker1.Items2083"),
            resources.GetString("foreColorPicker1.Items2084"),
            resources.GetString("foreColorPicker1.Items2085"),
            resources.GetString("foreColorPicker1.Items2086"),
            resources.GetString("foreColorPicker1.Items2087"),
            resources.GetString("foreColorPicker1.Items2088"),
            resources.GetString("foreColorPicker1.Items2089"),
            resources.GetString("foreColorPicker1.Items2090"),
            resources.GetString("foreColorPicker1.Items2091"),
            resources.GetString("foreColorPicker1.Items2092"),
            resources.GetString("foreColorPicker1.Items2093"),
            resources.GetString("foreColorPicker1.Items2094"),
            resources.GetString("foreColorPicker1.Items2095"),
            resources.GetString("foreColorPicker1.Items2096"),
            resources.GetString("foreColorPicker1.Items2097"),
            resources.GetString("foreColorPicker1.Items2098"),
            resources.GetString("foreColorPicker1.Items2099"),
            resources.GetString("foreColorPicker1.Items2100"),
            resources.GetString("foreColorPicker1.Items2101"),
            resources.GetString("foreColorPicker1.Items2102"),
            resources.GetString("foreColorPicker1.Items2103"),
            resources.GetString("foreColorPicker1.Items2104"),
            resources.GetString("foreColorPicker1.Items2105"),
            resources.GetString("foreColorPicker1.Items2106"),
            resources.GetString("foreColorPicker1.Items2107"),
            resources.GetString("foreColorPicker1.Items2108"),
            resources.GetString("foreColorPicker1.Items2109"),
            resources.GetString("foreColorPicker1.Items2110"),
            resources.GetString("foreColorPicker1.Items2111"),
            resources.GetString("foreColorPicker1.Items2112"),
            resources.GetString("foreColorPicker1.Items2113"),
            resources.GetString("foreColorPicker1.Items2114"),
            resources.GetString("foreColorPicker1.Items2115"),
            resources.GetString("foreColorPicker1.Items2116"),
            resources.GetString("foreColorPicker1.Items2117"),
            resources.GetString("foreColorPicker1.Items2118"),
            resources.GetString("foreColorPicker1.Items2119"),
            resources.GetString("foreColorPicker1.Items2120"),
            resources.GetString("foreColorPicker1.Items2121"),
            resources.GetString("foreColorPicker1.Items2122"),
            resources.GetString("foreColorPicker1.Items2123"),
            resources.GetString("foreColorPicker1.Items2124"),
            resources.GetString("foreColorPicker1.Items2125"),
            resources.GetString("foreColorPicker1.Items2126"),
            resources.GetString("foreColorPicker1.Items2127"),
            resources.GetString("foreColorPicker1.Items2128"),
            resources.GetString("foreColorPicker1.Items2129"),
            resources.GetString("foreColorPicker1.Items2130"),
            resources.GetString("foreColorPicker1.Items2131"),
            resources.GetString("foreColorPicker1.Items2132"),
            resources.GetString("foreColorPicker1.Items2133"),
            resources.GetString("foreColorPicker1.Items2134"),
            resources.GetString("foreColorPicker1.Items2135"),
            resources.GetString("foreColorPicker1.Items2136"),
            resources.GetString("foreColorPicker1.Items2137"),
            resources.GetString("foreColorPicker1.Items2138"),
            resources.GetString("foreColorPicker1.Items2139"),
            resources.GetString("foreColorPicker1.Items2140"),
            resources.GetString("foreColorPicker1.Items2141"),
            resources.GetString("foreColorPicker1.Items2142"),
            resources.GetString("foreColorPicker1.Items2143"),
            resources.GetString("foreColorPicker1.Items2144"),
            resources.GetString("foreColorPicker1.Items2145"),
            resources.GetString("foreColorPicker1.Items2146"),
            resources.GetString("foreColorPicker1.Items2147"),
            resources.GetString("foreColorPicker1.Items2148"),
            resources.GetString("foreColorPicker1.Items2149"),
            resources.GetString("foreColorPicker1.Items2150"),
            resources.GetString("foreColorPicker1.Items2151"),
            resources.GetString("foreColorPicker1.Items2152"),
            resources.GetString("foreColorPicker1.Items2153"),
            resources.GetString("foreColorPicker1.Items2154"),
            resources.GetString("foreColorPicker1.Items2155"),
            resources.GetString("foreColorPicker1.Items2156"),
            resources.GetString("foreColorPicker1.Items2157"),
            resources.GetString("foreColorPicker1.Items2158"),
            resources.GetString("foreColorPicker1.Items2159"),
            resources.GetString("foreColorPicker1.Items2160"),
            resources.GetString("foreColorPicker1.Items2161"),
            resources.GetString("foreColorPicker1.Items2162"),
            resources.GetString("foreColorPicker1.Items2163"),
            resources.GetString("foreColorPicker1.Items2164"),
            resources.GetString("foreColorPicker1.Items2165"),
            resources.GetString("foreColorPicker1.Items2166"),
            resources.GetString("foreColorPicker1.Items2167"),
            resources.GetString("foreColorPicker1.Items2168"),
            resources.GetString("foreColorPicker1.Items2169"),
            resources.GetString("foreColorPicker1.Items2170"),
            resources.GetString("foreColorPicker1.Items2171"),
            resources.GetString("foreColorPicker1.Items2172"),
            resources.GetString("foreColorPicker1.Items2173"),
            resources.GetString("foreColorPicker1.Items2174"),
            resources.GetString("foreColorPicker1.Items2175"),
            resources.GetString("foreColorPicker1.Items2176"),
            resources.GetString("foreColorPicker1.Items2177"),
            resources.GetString("foreColorPicker1.Items2178"),
            resources.GetString("foreColorPicker1.Items2179"),
            resources.GetString("foreColorPicker1.Items2180"),
            resources.GetString("foreColorPicker1.Items2181"),
            resources.GetString("foreColorPicker1.Items2182"),
            resources.GetString("foreColorPicker1.Items2183"),
            resources.GetString("foreColorPicker1.Items2184"),
            resources.GetString("foreColorPicker1.Items2185"),
            resources.GetString("foreColorPicker1.Items2186"),
            resources.GetString("foreColorPicker1.Items2187"),
            resources.GetString("foreColorPicker1.Items2188"),
            resources.GetString("foreColorPicker1.Items2189"),
            resources.GetString("foreColorPicker1.Items2190"),
            resources.GetString("foreColorPicker1.Items2191"),
            resources.GetString("foreColorPicker1.Items2192"),
            resources.GetString("foreColorPicker1.Items2193"),
            resources.GetString("foreColorPicker1.Items2194"),
            resources.GetString("foreColorPicker1.Items2195"),
            resources.GetString("foreColorPicker1.Items2196"),
            resources.GetString("foreColorPicker1.Items2197"),
            resources.GetString("foreColorPicker1.Items2198"),
            resources.GetString("foreColorPicker1.Items2199"),
            resources.GetString("foreColorPicker1.Items2200"),
            resources.GetString("foreColorPicker1.Items2201"),
            resources.GetString("foreColorPicker1.Items2202"),
            resources.GetString("foreColorPicker1.Items2203"),
            resources.GetString("foreColorPicker1.Items2204"),
            resources.GetString("foreColorPicker1.Items2205"),
            resources.GetString("foreColorPicker1.Items2206"),
            resources.GetString("foreColorPicker1.Items2207"),
            resources.GetString("foreColorPicker1.Items2208"),
            resources.GetString("foreColorPicker1.Items2209"),
            resources.GetString("foreColorPicker1.Items2210"),
            resources.GetString("foreColorPicker1.Items2211"),
            resources.GetString("foreColorPicker1.Items2212"),
            resources.GetString("foreColorPicker1.Items2213"),
            resources.GetString("foreColorPicker1.Items2214"),
            resources.GetString("foreColorPicker1.Items2215"),
            resources.GetString("foreColorPicker1.Items2216"),
            resources.GetString("foreColorPicker1.Items2217"),
            resources.GetString("foreColorPicker1.Items2218"),
            resources.GetString("foreColorPicker1.Items2219"),
            resources.GetString("foreColorPicker1.Items2220"),
            resources.GetString("foreColorPicker1.Items2221"),
            resources.GetString("foreColorPicker1.Items2222"),
            resources.GetString("foreColorPicker1.Items2223"),
            resources.GetString("foreColorPicker1.Items2224"),
            resources.GetString("foreColorPicker1.Items2225"),
            resources.GetString("foreColorPicker1.Items2226"),
            resources.GetString("foreColorPicker1.Items2227"),
            resources.GetString("foreColorPicker1.Items2228"),
            resources.GetString("foreColorPicker1.Items2229"),
            resources.GetString("foreColorPicker1.Items2230"),
            resources.GetString("foreColorPicker1.Items2231"),
            resources.GetString("foreColorPicker1.Items2232"),
            resources.GetString("foreColorPicker1.Items2233"),
            resources.GetString("foreColorPicker1.Items2234"),
            resources.GetString("foreColorPicker1.Items2235"),
            resources.GetString("foreColorPicker1.Items2236"),
            resources.GetString("foreColorPicker1.Items2237"),
            resources.GetString("foreColorPicker1.Items2238"),
            resources.GetString("foreColorPicker1.Items2239"),
            resources.GetString("foreColorPicker1.Items2240"),
            resources.GetString("foreColorPicker1.Items2241"),
            resources.GetString("foreColorPicker1.Items2242"),
            resources.GetString("foreColorPicker1.Items2243"),
            resources.GetString("foreColorPicker1.Items2244"),
            resources.GetString("foreColorPicker1.Items2245"),
            resources.GetString("foreColorPicker1.Items2246"),
            resources.GetString("foreColorPicker1.Items2247"),
            resources.GetString("foreColorPicker1.Items2248"),
            resources.GetString("foreColorPicker1.Items2249"),
            resources.GetString("foreColorPicker1.Items2250"),
            resources.GetString("foreColorPicker1.Items2251"),
            resources.GetString("foreColorPicker1.Items2252"),
            resources.GetString("foreColorPicker1.Items2253"),
            resources.GetString("foreColorPicker1.Items2254"),
            resources.GetString("foreColorPicker1.Items2255"),
            resources.GetString("foreColorPicker1.Items2256"),
            resources.GetString("foreColorPicker1.Items2257"),
            resources.GetString("foreColorPicker1.Items2258"),
            resources.GetString("foreColorPicker1.Items2259"),
            resources.GetString("foreColorPicker1.Items2260"),
            resources.GetString("foreColorPicker1.Items2261"),
            resources.GetString("foreColorPicker1.Items2262"),
            resources.GetString("foreColorPicker1.Items2263"),
            resources.GetString("foreColorPicker1.Items2264"),
            resources.GetString("foreColorPicker1.Items2265"),
            resources.GetString("foreColorPicker1.Items2266"),
            resources.GetString("foreColorPicker1.Items2267"),
            resources.GetString("foreColorPicker1.Items2268"),
            resources.GetString("foreColorPicker1.Items2269"),
            resources.GetString("foreColorPicker1.Items2270"),
            resources.GetString("foreColorPicker1.Items2271"),
            resources.GetString("foreColorPicker1.Items2272"),
            resources.GetString("foreColorPicker1.Items2273"),
            resources.GetString("foreColorPicker1.Items2274"),
            resources.GetString("foreColorPicker1.Items2275"),
            resources.GetString("foreColorPicker1.Items2276"),
            resources.GetString("foreColorPicker1.Items2277"),
            resources.GetString("foreColorPicker1.Items2278"),
            resources.GetString("foreColorPicker1.Items2279"),
            resources.GetString("foreColorPicker1.Items2280"),
            resources.GetString("foreColorPicker1.Items2281"),
            resources.GetString("foreColorPicker1.Items2282"),
            resources.GetString("foreColorPicker1.Items2283"),
            resources.GetString("foreColorPicker1.Items2284"),
            resources.GetString("foreColorPicker1.Items2285"),
            resources.GetString("foreColorPicker1.Items2286"),
            resources.GetString("foreColorPicker1.Items2287"),
            resources.GetString("foreColorPicker1.Items2288"),
            resources.GetString("foreColorPicker1.Items2289"),
            resources.GetString("foreColorPicker1.Items2290"),
            resources.GetString("foreColorPicker1.Items2291"),
            resources.GetString("foreColorPicker1.Items2292"),
            resources.GetString("foreColorPicker1.Items2293"),
            resources.GetString("foreColorPicker1.Items2294"),
            resources.GetString("foreColorPicker1.Items2295"),
            resources.GetString("foreColorPicker1.Items2296"),
            resources.GetString("foreColorPicker1.Items2297"),
            resources.GetString("foreColorPicker1.Items2298"),
            resources.GetString("foreColorPicker1.Items2299"),
            resources.GetString("foreColorPicker1.Items2300"),
            resources.GetString("foreColorPicker1.Items2301"),
            resources.GetString("foreColorPicker1.Items2302"),
            resources.GetString("foreColorPicker1.Items2303"),
            resources.GetString("foreColorPicker1.Items2304"),
            resources.GetString("foreColorPicker1.Items2305"),
            resources.GetString("foreColorPicker1.Items2306"),
            resources.GetString("foreColorPicker1.Items2307"),
            resources.GetString("foreColorPicker1.Items2308"),
            resources.GetString("foreColorPicker1.Items2309"),
            resources.GetString("foreColorPicker1.Items2310"),
            resources.GetString("foreColorPicker1.Items2311"),
            resources.GetString("foreColorPicker1.Items2312"),
            resources.GetString("foreColorPicker1.Items2313"),
            resources.GetString("foreColorPicker1.Items2314"),
            resources.GetString("foreColorPicker1.Items2315"),
            resources.GetString("foreColorPicker1.Items2316"),
            resources.GetString("foreColorPicker1.Items2317"),
            resources.GetString("foreColorPicker1.Items2318"),
            resources.GetString("foreColorPicker1.Items2319"),
            resources.GetString("foreColorPicker1.Items2320"),
            resources.GetString("foreColorPicker1.Items2321"),
            resources.GetString("foreColorPicker1.Items2322"),
            resources.GetString("foreColorPicker1.Items2323"),
            resources.GetString("foreColorPicker1.Items2324"),
            resources.GetString("foreColorPicker1.Items2325"),
            resources.GetString("foreColorPicker1.Items2326"),
            resources.GetString("foreColorPicker1.Items2327"),
            resources.GetString("foreColorPicker1.Items2328"),
            resources.GetString("foreColorPicker1.Items2329"),
            resources.GetString("foreColorPicker1.Items2330"),
            resources.GetString("foreColorPicker1.Items2331"),
            resources.GetString("foreColorPicker1.Items2332"),
            resources.GetString("foreColorPicker1.Items2333"),
            resources.GetString("foreColorPicker1.Items2334"),
            resources.GetString("foreColorPicker1.Items2335"),
            resources.GetString("foreColorPicker1.Items2336"),
            resources.GetString("foreColorPicker1.Items2337"),
            resources.GetString("foreColorPicker1.Items2338"),
            resources.GetString("foreColorPicker1.Items2339"),
            resources.GetString("foreColorPicker1.Items2340"),
            resources.GetString("foreColorPicker1.Items2341"),
            resources.GetString("foreColorPicker1.Items2342"),
            resources.GetString("foreColorPicker1.Items2343"),
            resources.GetString("foreColorPicker1.Items2344"),
            resources.GetString("foreColorPicker1.Items2345"),
            resources.GetString("foreColorPicker1.Items2346"),
            resources.GetString("foreColorPicker1.Items2347"),
            resources.GetString("foreColorPicker1.Items2348"),
            resources.GetString("foreColorPicker1.Items2349"),
            resources.GetString("foreColorPicker1.Items2350"),
            resources.GetString("foreColorPicker1.Items2351"),
            resources.GetString("foreColorPicker1.Items2352"),
            resources.GetString("foreColorPicker1.Items2353"),
            resources.GetString("foreColorPicker1.Items2354"),
            resources.GetString("foreColorPicker1.Items2355"),
            resources.GetString("foreColorPicker1.Items2356"),
            resources.GetString("foreColorPicker1.Items2357"),
            resources.GetString("foreColorPicker1.Items2358"),
            resources.GetString("foreColorPicker1.Items2359"),
            resources.GetString("foreColorPicker1.Items2360"),
            resources.GetString("foreColorPicker1.Items2361"),
            resources.GetString("foreColorPicker1.Items2362"),
            resources.GetString("foreColorPicker1.Items2363"),
            resources.GetString("foreColorPicker1.Items2364"),
            resources.GetString("foreColorPicker1.Items2365"),
            resources.GetString("foreColorPicker1.Items2366"),
            resources.GetString("foreColorPicker1.Items2367"),
            resources.GetString("foreColorPicker1.Items2368"),
            resources.GetString("foreColorPicker1.Items2369"),
            resources.GetString("foreColorPicker1.Items2370"),
            resources.GetString("foreColorPicker1.Items2371"),
            resources.GetString("foreColorPicker1.Items2372"),
            resources.GetString("foreColorPicker1.Items2373"),
            resources.GetString("foreColorPicker1.Items2374"),
            resources.GetString("foreColorPicker1.Items2375"),
            resources.GetString("foreColorPicker1.Items2376"),
            resources.GetString("foreColorPicker1.Items2377"),
            resources.GetString("foreColorPicker1.Items2378"),
            resources.GetString("foreColorPicker1.Items2379"),
            resources.GetString("foreColorPicker1.Items2380"),
            resources.GetString("foreColorPicker1.Items2381"),
            resources.GetString("foreColorPicker1.Items2382"),
            resources.GetString("foreColorPicker1.Items2383"),
            resources.GetString("foreColorPicker1.Items2384"),
            resources.GetString("foreColorPicker1.Items2385"),
            resources.GetString("foreColorPicker1.Items2386"),
            resources.GetString("foreColorPicker1.Items2387"),
            resources.GetString("foreColorPicker1.Items2388"),
            resources.GetString("foreColorPicker1.Items2389"),
            resources.GetString("foreColorPicker1.Items2390"),
            resources.GetString("foreColorPicker1.Items2391"),
            resources.GetString("foreColorPicker1.Items2392"),
            resources.GetString("foreColorPicker1.Items2393"),
            resources.GetString("foreColorPicker1.Items2394"),
            resources.GetString("foreColorPicker1.Items2395"),
            resources.GetString("foreColorPicker1.Items2396"),
            resources.GetString("foreColorPicker1.Items2397"),
            resources.GetString("foreColorPicker1.Items2398"),
            resources.GetString("foreColorPicker1.Items2399"),
            resources.GetString("foreColorPicker1.Items2400"),
            resources.GetString("foreColorPicker1.Items2401"),
            resources.GetString("foreColorPicker1.Items2402"),
            resources.GetString("foreColorPicker1.Items2403"),
            resources.GetString("foreColorPicker1.Items2404"),
            resources.GetString("foreColorPicker1.Items2405"),
            resources.GetString("foreColorPicker1.Items2406"),
            resources.GetString("foreColorPicker1.Items2407"),
            resources.GetString("foreColorPicker1.Items2408"),
            resources.GetString("foreColorPicker1.Items2409"),
            resources.GetString("foreColorPicker1.Items2410"),
            resources.GetString("foreColorPicker1.Items2411"),
            resources.GetString("foreColorPicker1.Items2412"),
            resources.GetString("foreColorPicker1.Items2413"),
            resources.GetString("foreColorPicker1.Items2414"),
            resources.GetString("foreColorPicker1.Items2415"),
            resources.GetString("foreColorPicker1.Items2416"),
            resources.GetString("foreColorPicker1.Items2417"),
            resources.GetString("foreColorPicker1.Items2418"),
            resources.GetString("foreColorPicker1.Items2419"),
            resources.GetString("foreColorPicker1.Items2420"),
            resources.GetString("foreColorPicker1.Items2421"),
            resources.GetString("foreColorPicker1.Items2422"),
            resources.GetString("foreColorPicker1.Items2423"),
            resources.GetString("foreColorPicker1.Items2424"),
            resources.GetString("foreColorPicker1.Items2425"),
            resources.GetString("foreColorPicker1.Items2426"),
            resources.GetString("foreColorPicker1.Items2427"),
            resources.GetString("foreColorPicker1.Items2428"),
            resources.GetString("foreColorPicker1.Items2429"),
            resources.GetString("foreColorPicker1.Items2430"),
            resources.GetString("foreColorPicker1.Items2431"),
            resources.GetString("foreColorPicker1.Items2432"),
            resources.GetString("foreColorPicker1.Items2433"),
            resources.GetString("foreColorPicker1.Items2434"),
            resources.GetString("foreColorPicker1.Items2435"),
            resources.GetString("foreColorPicker1.Items2436"),
            resources.GetString("foreColorPicker1.Items2437"),
            resources.GetString("foreColorPicker1.Items2438"),
            resources.GetString("foreColorPicker1.Items2439"),
            resources.GetString("foreColorPicker1.Items2440"),
            resources.GetString("foreColorPicker1.Items2441"),
            resources.GetString("foreColorPicker1.Items2442"),
            resources.GetString("foreColorPicker1.Items2443"),
            resources.GetString("foreColorPicker1.Items2444"),
            resources.GetString("foreColorPicker1.Items2445"),
            resources.GetString("foreColorPicker1.Items2446"),
            resources.GetString("foreColorPicker1.Items2447"),
            resources.GetString("foreColorPicker1.Items2448"),
            resources.GetString("foreColorPicker1.Items2449"),
            resources.GetString("foreColorPicker1.Items2450"),
            resources.GetString("foreColorPicker1.Items2451"),
            resources.GetString("foreColorPicker1.Items2452"),
            resources.GetString("foreColorPicker1.Items2453"),
            resources.GetString("foreColorPicker1.Items2454"),
            resources.GetString("foreColorPicker1.Items2455"),
            resources.GetString("foreColorPicker1.Items2456"),
            resources.GetString("foreColorPicker1.Items2457"),
            resources.GetString("foreColorPicker1.Items2458"),
            resources.GetString("foreColorPicker1.Items2459"),
            resources.GetString("foreColorPicker1.Items2460"),
            resources.GetString("foreColorPicker1.Items2461"),
            resources.GetString("foreColorPicker1.Items2462"),
            resources.GetString("foreColorPicker1.Items2463"),
            resources.GetString("foreColorPicker1.Items2464"),
            resources.GetString("foreColorPicker1.Items2465"),
            resources.GetString("foreColorPicker1.Items2466"),
            resources.GetString("foreColorPicker1.Items2467"),
            resources.GetString("foreColorPicker1.Items2468"),
            resources.GetString("foreColorPicker1.Items2469"),
            resources.GetString("foreColorPicker1.Items2470"),
            resources.GetString("foreColorPicker1.Items2471"),
            resources.GetString("foreColorPicker1.Items2472"),
            resources.GetString("foreColorPicker1.Items2473"),
            resources.GetString("foreColorPicker1.Items2474"),
            resources.GetString("foreColorPicker1.Items2475"),
            resources.GetString("foreColorPicker1.Items2476"),
            resources.GetString("foreColorPicker1.Items2477"),
            resources.GetString("foreColorPicker1.Items2478"),
            resources.GetString("foreColorPicker1.Items2479"),
            resources.GetString("foreColorPicker1.Items2480"),
            resources.GetString("foreColorPicker1.Items2481"),
            resources.GetString("foreColorPicker1.Items2482"),
            resources.GetString("foreColorPicker1.Items2483"),
            resources.GetString("foreColorPicker1.Items2484"),
            resources.GetString("foreColorPicker1.Items2485"),
            resources.GetString("foreColorPicker1.Items2486"),
            resources.GetString("foreColorPicker1.Items2487"),
            resources.GetString("foreColorPicker1.Items2488"),
            resources.GetString("foreColorPicker1.Items2489"),
            resources.GetString("foreColorPicker1.Items2490"),
            resources.GetString("foreColorPicker1.Items2491"),
            resources.GetString("foreColorPicker1.Items2492"),
            resources.GetString("foreColorPicker1.Items2493"),
            resources.GetString("foreColorPicker1.Items2494"),
            resources.GetString("foreColorPicker1.Items2495"),
            resources.GetString("foreColorPicker1.Items2496"),
            resources.GetString("foreColorPicker1.Items2497"),
            resources.GetString("foreColorPicker1.Items2498"),
            resources.GetString("foreColorPicker1.Items2499"),
            resources.GetString("foreColorPicker1.Items2500"),
            resources.GetString("foreColorPicker1.Items2501"),
            resources.GetString("foreColorPicker1.Items2502"),
            resources.GetString("foreColorPicker1.Items2503"),
            resources.GetString("foreColorPicker1.Items2504"),
            resources.GetString("foreColorPicker1.Items2505"),
            resources.GetString("foreColorPicker1.Items2506"),
            resources.GetString("foreColorPicker1.Items2507"),
            resources.GetString("foreColorPicker1.Items2508"),
            resources.GetString("foreColorPicker1.Items2509"),
            resources.GetString("foreColorPicker1.Items2510"),
            resources.GetString("foreColorPicker1.Items2511"),
            resources.GetString("foreColorPicker1.Items2512"),
            resources.GetString("foreColorPicker1.Items2513"),
            resources.GetString("foreColorPicker1.Items2514"),
            resources.GetString("foreColorPicker1.Items2515"),
            resources.GetString("foreColorPicker1.Items2516"),
            resources.GetString("foreColorPicker1.Items2517"),
            resources.GetString("foreColorPicker1.Items2518"),
            resources.GetString("foreColorPicker1.Items2519")});
			this.foreColorPicker1.Name = "foreColorPicker1";
			this.foreColorPicker1.Tag = "Fore Color";
			this.foreColorPicker1.SelectedValueChanged += this.ctlForeColor_Change;
			this.foreColorPicker1.Validated += this.ctlForeColor_Change;
			// 
			// backColorPicker1
			// 
			this.backColorPicker1.DrawMode = Majorsilence.Forms.DrawMode.OwnerDrawFixed;
			this.backColorPicker1.DropDownHeight = 1;
			this.backColorPicker1.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.backColorPicker1, "backColorPicker1");
			this.backColorPicker1.FormattingEnabled = true;
			this.backColorPicker1.Items.AddRange(new object[] {
            resources.GetString("backColorPicker1.Items"),
            resources.GetString("backColorPicker1.Items1"),
            resources.GetString("backColorPicker1.Items2"),
            resources.GetString("backColorPicker1.Items3"),
            resources.GetString("backColorPicker1.Items4"),
            resources.GetString("backColorPicker1.Items5"),
            resources.GetString("backColorPicker1.Items6"),
            resources.GetString("backColorPicker1.Items7"),
            resources.GetString("backColorPicker1.Items8"),
            resources.GetString("backColorPicker1.Items9"),
            resources.GetString("backColorPicker1.Items10"),
            resources.GetString("backColorPicker1.Items11"),
            resources.GetString("backColorPicker1.Items12"),
            resources.GetString("backColorPicker1.Items13"),
            resources.GetString("backColorPicker1.Items14"),
            resources.GetString("backColorPicker1.Items15"),
            resources.GetString("backColorPicker1.Items16"),
            resources.GetString("backColorPicker1.Items17"),
            resources.GetString("backColorPicker1.Items18"),
            resources.GetString("backColorPicker1.Items19"),
            resources.GetString("backColorPicker1.Items20"),
            resources.GetString("backColorPicker1.Items21"),
            resources.GetString("backColorPicker1.Items22"),
            resources.GetString("backColorPicker1.Items23"),
            resources.GetString("backColorPicker1.Items24"),
            resources.GetString("backColorPicker1.Items25"),
            resources.GetString("backColorPicker1.Items26"),
            resources.GetString("backColorPicker1.Items27"),
            resources.GetString("backColorPicker1.Items28"),
            resources.GetString("backColorPicker1.Items29"),
            resources.GetString("backColorPicker1.Items30"),
            resources.GetString("backColorPicker1.Items31"),
            resources.GetString("backColorPicker1.Items32"),
            resources.GetString("backColorPicker1.Items33"),
            resources.GetString("backColorPicker1.Items34"),
            resources.GetString("backColorPicker1.Items35"),
            resources.GetString("backColorPicker1.Items36"),
            resources.GetString("backColorPicker1.Items37"),
            resources.GetString("backColorPicker1.Items38"),
            resources.GetString("backColorPicker1.Items39"),
            resources.GetString("backColorPicker1.Items40"),
            resources.GetString("backColorPicker1.Items41"),
            resources.GetString("backColorPicker1.Items42"),
            resources.GetString("backColorPicker1.Items43"),
            resources.GetString("backColorPicker1.Items44"),
            resources.GetString("backColorPicker1.Items45"),
            resources.GetString("backColorPicker1.Items46"),
            resources.GetString("backColorPicker1.Items47"),
            resources.GetString("backColorPicker1.Items48"),
            resources.GetString("backColorPicker1.Items49"),
            resources.GetString("backColorPicker1.Items50"),
            resources.GetString("backColorPicker1.Items51"),
            resources.GetString("backColorPicker1.Items52"),
            resources.GetString("backColorPicker1.Items53"),
            resources.GetString("backColorPicker1.Items54"),
            resources.GetString("backColorPicker1.Items55"),
            resources.GetString("backColorPicker1.Items56"),
            resources.GetString("backColorPicker1.Items57"),
            resources.GetString("backColorPicker1.Items58"),
            resources.GetString("backColorPicker1.Items59"),
            resources.GetString("backColorPicker1.Items60"),
            resources.GetString("backColorPicker1.Items61"),
            resources.GetString("backColorPicker1.Items62"),
            resources.GetString("backColorPicker1.Items63"),
            resources.GetString("backColorPicker1.Items64"),
            resources.GetString("backColorPicker1.Items65"),
            resources.GetString("backColorPicker1.Items66"),
            resources.GetString("backColorPicker1.Items67"),
            resources.GetString("backColorPicker1.Items68"),
            resources.GetString("backColorPicker1.Items69"),
            resources.GetString("backColorPicker1.Items70"),
            resources.GetString("backColorPicker1.Items71"),
            resources.GetString("backColorPicker1.Items72"),
            resources.GetString("backColorPicker1.Items73"),
            resources.GetString("backColorPicker1.Items74"),
            resources.GetString("backColorPicker1.Items75"),
            resources.GetString("backColorPicker1.Items76"),
            resources.GetString("backColorPicker1.Items77"),
            resources.GetString("backColorPicker1.Items78"),
            resources.GetString("backColorPicker1.Items79"),
            resources.GetString("backColorPicker1.Items80"),
            resources.GetString("backColorPicker1.Items81"),
            resources.GetString("backColorPicker1.Items82"),
            resources.GetString("backColorPicker1.Items83"),
            resources.GetString("backColorPicker1.Items84"),
            resources.GetString("backColorPicker1.Items85"),
            resources.GetString("backColorPicker1.Items86"),
            resources.GetString("backColorPicker1.Items87"),
            resources.GetString("backColorPicker1.Items88"),
            resources.GetString("backColorPicker1.Items89"),
            resources.GetString("backColorPicker1.Items90"),
            resources.GetString("backColorPicker1.Items91"),
            resources.GetString("backColorPicker1.Items92"),
            resources.GetString("backColorPicker1.Items93"),
            resources.GetString("backColorPicker1.Items94"),
            resources.GetString("backColorPicker1.Items95"),
            resources.GetString("backColorPicker1.Items96"),
            resources.GetString("backColorPicker1.Items97"),
            resources.GetString("backColorPicker1.Items98"),
            resources.GetString("backColorPicker1.Items99"),
            resources.GetString("backColorPicker1.Items100"),
            resources.GetString("backColorPicker1.Items101"),
            resources.GetString("backColorPicker1.Items102"),
            resources.GetString("backColorPicker1.Items103"),
            resources.GetString("backColorPicker1.Items104"),
            resources.GetString("backColorPicker1.Items105"),
            resources.GetString("backColorPicker1.Items106"),
            resources.GetString("backColorPicker1.Items107"),
            resources.GetString("backColorPicker1.Items108"),
            resources.GetString("backColorPicker1.Items109"),
            resources.GetString("backColorPicker1.Items110"),
            resources.GetString("backColorPicker1.Items111"),
            resources.GetString("backColorPicker1.Items112"),
            resources.GetString("backColorPicker1.Items113"),
            resources.GetString("backColorPicker1.Items114"),
            resources.GetString("backColorPicker1.Items115"),
            resources.GetString("backColorPicker1.Items116"),
            resources.GetString("backColorPicker1.Items117"),
            resources.GetString("backColorPicker1.Items118"),
            resources.GetString("backColorPicker1.Items119"),
            resources.GetString("backColorPicker1.Items120"),
            resources.GetString("backColorPicker1.Items121"),
            resources.GetString("backColorPicker1.Items122"),
            resources.GetString("backColorPicker1.Items123"),
            resources.GetString("backColorPicker1.Items124"),
            resources.GetString("backColorPicker1.Items125"),
            resources.GetString("backColorPicker1.Items126"),
            resources.GetString("backColorPicker1.Items127"),
            resources.GetString("backColorPicker1.Items128"),
            resources.GetString("backColorPicker1.Items129"),
            resources.GetString("backColorPicker1.Items130"),
            resources.GetString("backColorPicker1.Items131"),
            resources.GetString("backColorPicker1.Items132"),
            resources.GetString("backColorPicker1.Items133"),
            resources.GetString("backColorPicker1.Items134"),
            resources.GetString("backColorPicker1.Items135"),
            resources.GetString("backColorPicker1.Items136"),
            resources.GetString("backColorPicker1.Items137"),
            resources.GetString("backColorPicker1.Items138"),
            resources.GetString("backColorPicker1.Items139"),
            resources.GetString("backColorPicker1.Items140"),
            resources.GetString("backColorPicker1.Items141"),
            resources.GetString("backColorPicker1.Items142"),
            resources.GetString("backColorPicker1.Items143"),
            resources.GetString("backColorPicker1.Items144"),
            resources.GetString("backColorPicker1.Items145"),
            resources.GetString("backColorPicker1.Items146"),
            resources.GetString("backColorPicker1.Items147"),
            resources.GetString("backColorPicker1.Items148"),
            resources.GetString("backColorPicker1.Items149"),
            resources.GetString("backColorPicker1.Items150"),
            resources.GetString("backColorPicker1.Items151"),
            resources.GetString("backColorPicker1.Items152"),
            resources.GetString("backColorPicker1.Items153"),
            resources.GetString("backColorPicker1.Items154"),
            resources.GetString("backColorPicker1.Items155"),
            resources.GetString("backColorPicker1.Items156"),
            resources.GetString("backColorPicker1.Items157"),
            resources.GetString("backColorPicker1.Items158"),
            resources.GetString("backColorPicker1.Items159"),
            resources.GetString("backColorPicker1.Items160"),
            resources.GetString("backColorPicker1.Items161"),
            resources.GetString("backColorPicker1.Items162"),
            resources.GetString("backColorPicker1.Items163"),
            resources.GetString("backColorPicker1.Items164"),
            resources.GetString("backColorPicker1.Items165"),
            resources.GetString("backColorPicker1.Items166"),
            resources.GetString("backColorPicker1.Items167"),
            resources.GetString("backColorPicker1.Items168"),
            resources.GetString("backColorPicker1.Items169"),
            resources.GetString("backColorPicker1.Items170"),
            resources.GetString("backColorPicker1.Items171"),
            resources.GetString("backColorPicker1.Items172"),
            resources.GetString("backColorPicker1.Items173"),
            resources.GetString("backColorPicker1.Items174"),
            resources.GetString("backColorPicker1.Items175"),
            resources.GetString("backColorPicker1.Items176"),
            resources.GetString("backColorPicker1.Items177"),
            resources.GetString("backColorPicker1.Items178"),
            resources.GetString("backColorPicker1.Items179"),
            resources.GetString("backColorPicker1.Items180"),
            resources.GetString("backColorPicker1.Items181"),
            resources.GetString("backColorPicker1.Items182"),
            resources.GetString("backColorPicker1.Items183"),
            resources.GetString("backColorPicker1.Items184"),
            resources.GetString("backColorPicker1.Items185"),
            resources.GetString("backColorPicker1.Items186"),
            resources.GetString("backColorPicker1.Items187"),
            resources.GetString("backColorPicker1.Items188"),
            resources.GetString("backColorPicker1.Items189"),
            resources.GetString("backColorPicker1.Items190"),
            resources.GetString("backColorPicker1.Items191"),
            resources.GetString("backColorPicker1.Items192"),
            resources.GetString("backColorPicker1.Items193"),
            resources.GetString("backColorPicker1.Items194"),
            resources.GetString("backColorPicker1.Items195"),
            resources.GetString("backColorPicker1.Items196"),
            resources.GetString("backColorPicker1.Items197"),
            resources.GetString("backColorPicker1.Items198"),
            resources.GetString("backColorPicker1.Items199"),
            resources.GetString("backColorPicker1.Items200"),
            resources.GetString("backColorPicker1.Items201"),
            resources.GetString("backColorPicker1.Items202"),
            resources.GetString("backColorPicker1.Items203"),
            resources.GetString("backColorPicker1.Items204"),
            resources.GetString("backColorPicker1.Items205"),
            resources.GetString("backColorPicker1.Items206"),
            resources.GetString("backColorPicker1.Items207"),
            resources.GetString("backColorPicker1.Items208"),
            resources.GetString("backColorPicker1.Items209"),
            resources.GetString("backColorPicker1.Items210"),
            resources.GetString("backColorPicker1.Items211"),
            resources.GetString("backColorPicker1.Items212"),
            resources.GetString("backColorPicker1.Items213"),
            resources.GetString("backColorPicker1.Items214"),
            resources.GetString("backColorPicker1.Items215"),
            resources.GetString("backColorPicker1.Items216"),
            resources.GetString("backColorPicker1.Items217"),
            resources.GetString("backColorPicker1.Items218"),
            resources.GetString("backColorPicker1.Items219"),
            resources.GetString("backColorPicker1.Items220"),
            resources.GetString("backColorPicker1.Items221"),
            resources.GetString("backColorPicker1.Items222"),
            resources.GetString("backColorPicker1.Items223"),
            resources.GetString("backColorPicker1.Items224"),
            resources.GetString("backColorPicker1.Items225"),
            resources.GetString("backColorPicker1.Items226"),
            resources.GetString("backColorPicker1.Items227"),
            resources.GetString("backColorPicker1.Items228"),
            resources.GetString("backColorPicker1.Items229"),
            resources.GetString("backColorPicker1.Items230"),
            resources.GetString("backColorPicker1.Items231"),
            resources.GetString("backColorPicker1.Items232"),
            resources.GetString("backColorPicker1.Items233"),
            resources.GetString("backColorPicker1.Items234"),
            resources.GetString("backColorPicker1.Items235"),
            resources.GetString("backColorPicker1.Items236"),
            resources.GetString("backColorPicker1.Items237"),
            resources.GetString("backColorPicker1.Items238"),
            resources.GetString("backColorPicker1.Items239"),
            resources.GetString("backColorPicker1.Items240"),
            resources.GetString("backColorPicker1.Items241"),
            resources.GetString("backColorPicker1.Items242"),
            resources.GetString("backColorPicker1.Items243"),
            resources.GetString("backColorPicker1.Items244"),
            resources.GetString("backColorPicker1.Items245"),
            resources.GetString("backColorPicker1.Items246"),
            resources.GetString("backColorPicker1.Items247"),
            resources.GetString("backColorPicker1.Items248"),
            resources.GetString("backColorPicker1.Items249"),
            resources.GetString("backColorPicker1.Items250"),
            resources.GetString("backColorPicker1.Items251"),
            resources.GetString("backColorPicker1.Items252"),
            resources.GetString("backColorPicker1.Items253"),
            resources.GetString("backColorPicker1.Items254"),
            resources.GetString("backColorPicker1.Items255"),
            resources.GetString("backColorPicker1.Items256"),
            resources.GetString("backColorPicker1.Items257"),
            resources.GetString("backColorPicker1.Items258"),
            resources.GetString("backColorPicker1.Items259"),
            resources.GetString("backColorPicker1.Items260"),
            resources.GetString("backColorPicker1.Items261"),
            resources.GetString("backColorPicker1.Items262"),
            resources.GetString("backColorPicker1.Items263"),
            resources.GetString("backColorPicker1.Items264"),
            resources.GetString("backColorPicker1.Items265"),
            resources.GetString("backColorPicker1.Items266"),
            resources.GetString("backColorPicker1.Items267"),
            resources.GetString("backColorPicker1.Items268"),
            resources.GetString("backColorPicker1.Items269"),
            resources.GetString("backColorPicker1.Items270"),
            resources.GetString("backColorPicker1.Items271"),
            resources.GetString("backColorPicker1.Items272"),
            resources.GetString("backColorPicker1.Items273"),
            resources.GetString("backColorPicker1.Items274"),
            resources.GetString("backColorPicker1.Items275"),
            resources.GetString("backColorPicker1.Items276"),
            resources.GetString("backColorPicker1.Items277"),
            resources.GetString("backColorPicker1.Items278"),
            resources.GetString("backColorPicker1.Items279"),
            resources.GetString("backColorPicker1.Items280"),
            resources.GetString("backColorPicker1.Items281"),
            resources.GetString("backColorPicker1.Items282"),
            resources.GetString("backColorPicker1.Items283"),
            resources.GetString("backColorPicker1.Items284"),
            resources.GetString("backColorPicker1.Items285"),
            resources.GetString("backColorPicker1.Items286"),
            resources.GetString("backColorPicker1.Items287"),
            resources.GetString("backColorPicker1.Items288"),
            resources.GetString("backColorPicker1.Items289"),
            resources.GetString("backColorPicker1.Items290"),
            resources.GetString("backColorPicker1.Items291"),
            resources.GetString("backColorPicker1.Items292"),
            resources.GetString("backColorPicker1.Items293"),
            resources.GetString("backColorPicker1.Items294"),
            resources.GetString("backColorPicker1.Items295"),
            resources.GetString("backColorPicker1.Items296"),
            resources.GetString("backColorPicker1.Items297"),
            resources.GetString("backColorPicker1.Items298"),
            resources.GetString("backColorPicker1.Items299"),
            resources.GetString("backColorPicker1.Items300"),
            resources.GetString("backColorPicker1.Items301"),
            resources.GetString("backColorPicker1.Items302"),
            resources.GetString("backColorPicker1.Items303"),
            resources.GetString("backColorPicker1.Items304"),
            resources.GetString("backColorPicker1.Items305"),
            resources.GetString("backColorPicker1.Items306"),
            resources.GetString("backColorPicker1.Items307"),
            resources.GetString("backColorPicker1.Items308"),
            resources.GetString("backColorPicker1.Items309"),
            resources.GetString("backColorPicker1.Items310"),
            resources.GetString("backColorPicker1.Items311"),
            resources.GetString("backColorPicker1.Items312"),
            resources.GetString("backColorPicker1.Items313"),
            resources.GetString("backColorPicker1.Items314"),
            resources.GetString("backColorPicker1.Items315"),
            resources.GetString("backColorPicker1.Items316"),
            resources.GetString("backColorPicker1.Items317"),
            resources.GetString("backColorPicker1.Items318"),
            resources.GetString("backColorPicker1.Items319"),
            resources.GetString("backColorPicker1.Items320"),
            resources.GetString("backColorPicker1.Items321"),
            resources.GetString("backColorPicker1.Items322"),
            resources.GetString("backColorPicker1.Items323"),
            resources.GetString("backColorPicker1.Items324"),
            resources.GetString("backColorPicker1.Items325"),
            resources.GetString("backColorPicker1.Items326"),
            resources.GetString("backColorPicker1.Items327"),
            resources.GetString("backColorPicker1.Items328"),
            resources.GetString("backColorPicker1.Items329"),
            resources.GetString("backColorPicker1.Items330"),
            resources.GetString("backColorPicker1.Items331"),
            resources.GetString("backColorPicker1.Items332"),
            resources.GetString("backColorPicker1.Items333"),
            resources.GetString("backColorPicker1.Items334"),
            resources.GetString("backColorPicker1.Items335"),
            resources.GetString("backColorPicker1.Items336"),
            resources.GetString("backColorPicker1.Items337"),
            resources.GetString("backColorPicker1.Items338"),
            resources.GetString("backColorPicker1.Items339"),
            resources.GetString("backColorPicker1.Items340"),
            resources.GetString("backColorPicker1.Items341"),
            resources.GetString("backColorPicker1.Items342"),
            resources.GetString("backColorPicker1.Items343"),
            resources.GetString("backColorPicker1.Items344"),
            resources.GetString("backColorPicker1.Items345"),
            resources.GetString("backColorPicker1.Items346"),
            resources.GetString("backColorPicker1.Items347"),
            resources.GetString("backColorPicker1.Items348"),
            resources.GetString("backColorPicker1.Items349"),
            resources.GetString("backColorPicker1.Items350"),
            resources.GetString("backColorPicker1.Items351"),
            resources.GetString("backColorPicker1.Items352"),
            resources.GetString("backColorPicker1.Items353"),
            resources.GetString("backColorPicker1.Items354"),
            resources.GetString("backColorPicker1.Items355"),
            resources.GetString("backColorPicker1.Items356"),
            resources.GetString("backColorPicker1.Items357"),
            resources.GetString("backColorPicker1.Items358"),
            resources.GetString("backColorPicker1.Items359"),
            resources.GetString("backColorPicker1.Items360"),
            resources.GetString("backColorPicker1.Items361"),
            resources.GetString("backColorPicker1.Items362"),
            resources.GetString("backColorPicker1.Items363"),
            resources.GetString("backColorPicker1.Items364"),
            resources.GetString("backColorPicker1.Items365"),
            resources.GetString("backColorPicker1.Items366"),
            resources.GetString("backColorPicker1.Items367"),
            resources.GetString("backColorPicker1.Items368"),
            resources.GetString("backColorPicker1.Items369"),
            resources.GetString("backColorPicker1.Items370"),
            resources.GetString("backColorPicker1.Items371"),
            resources.GetString("backColorPicker1.Items372"),
            resources.GetString("backColorPicker1.Items373"),
            resources.GetString("backColorPicker1.Items374"),
            resources.GetString("backColorPicker1.Items375"),
            resources.GetString("backColorPicker1.Items376"),
            resources.GetString("backColorPicker1.Items377"),
            resources.GetString("backColorPicker1.Items378"),
            resources.GetString("backColorPicker1.Items379"),
            resources.GetString("backColorPicker1.Items380"),
            resources.GetString("backColorPicker1.Items381"),
            resources.GetString("backColorPicker1.Items382"),
            resources.GetString("backColorPicker1.Items383"),
            resources.GetString("backColorPicker1.Items384"),
            resources.GetString("backColorPicker1.Items385"),
            resources.GetString("backColorPicker1.Items386"),
            resources.GetString("backColorPicker1.Items387"),
            resources.GetString("backColorPicker1.Items388"),
            resources.GetString("backColorPicker1.Items389"),
            resources.GetString("backColorPicker1.Items390"),
            resources.GetString("backColorPicker1.Items391"),
            resources.GetString("backColorPicker1.Items392"),
            resources.GetString("backColorPicker1.Items393"),
            resources.GetString("backColorPicker1.Items394"),
            resources.GetString("backColorPicker1.Items395"),
            resources.GetString("backColorPicker1.Items396"),
            resources.GetString("backColorPicker1.Items397"),
            resources.GetString("backColorPicker1.Items398"),
            resources.GetString("backColorPicker1.Items399"),
            resources.GetString("backColorPicker1.Items400"),
            resources.GetString("backColorPicker1.Items401"),
            resources.GetString("backColorPicker1.Items402"),
            resources.GetString("backColorPicker1.Items403"),
            resources.GetString("backColorPicker1.Items404"),
            resources.GetString("backColorPicker1.Items405"),
            resources.GetString("backColorPicker1.Items406"),
            resources.GetString("backColorPicker1.Items407"),
            resources.GetString("backColorPicker1.Items408"),
            resources.GetString("backColorPicker1.Items409"),
            resources.GetString("backColorPicker1.Items410"),
            resources.GetString("backColorPicker1.Items411"),
            resources.GetString("backColorPicker1.Items412"),
            resources.GetString("backColorPicker1.Items413"),
            resources.GetString("backColorPicker1.Items414"),
            resources.GetString("backColorPicker1.Items415"),
            resources.GetString("backColorPicker1.Items416"),
            resources.GetString("backColorPicker1.Items417"),
            resources.GetString("backColorPicker1.Items418"),
            resources.GetString("backColorPicker1.Items419"),
            resources.GetString("backColorPicker1.Items420"),
            resources.GetString("backColorPicker1.Items421"),
            resources.GetString("backColorPicker1.Items422"),
            resources.GetString("backColorPicker1.Items423"),
            resources.GetString("backColorPicker1.Items424"),
            resources.GetString("backColorPicker1.Items425"),
            resources.GetString("backColorPicker1.Items426"),
            resources.GetString("backColorPicker1.Items427"),
            resources.GetString("backColorPicker1.Items428"),
            resources.GetString("backColorPicker1.Items429"),
            resources.GetString("backColorPicker1.Items430"),
            resources.GetString("backColorPicker1.Items431"),
            resources.GetString("backColorPicker1.Items432"),
            resources.GetString("backColorPicker1.Items433"),
            resources.GetString("backColorPicker1.Items434"),
            resources.GetString("backColorPicker1.Items435"),
            resources.GetString("backColorPicker1.Items436"),
            resources.GetString("backColorPicker1.Items437"),
            resources.GetString("backColorPicker1.Items438"),
            resources.GetString("backColorPicker1.Items439"),
            resources.GetString("backColorPicker1.Items440"),
            resources.GetString("backColorPicker1.Items441"),
            resources.GetString("backColorPicker1.Items442"),
            resources.GetString("backColorPicker1.Items443"),
            resources.GetString("backColorPicker1.Items444"),
            resources.GetString("backColorPicker1.Items445"),
            resources.GetString("backColorPicker1.Items446"),
            resources.GetString("backColorPicker1.Items447"),
            resources.GetString("backColorPicker1.Items448"),
            resources.GetString("backColorPicker1.Items449"),
            resources.GetString("backColorPicker1.Items450"),
            resources.GetString("backColorPicker1.Items451"),
            resources.GetString("backColorPicker1.Items452"),
            resources.GetString("backColorPicker1.Items453"),
            resources.GetString("backColorPicker1.Items454"),
            resources.GetString("backColorPicker1.Items455"),
            resources.GetString("backColorPicker1.Items456"),
            resources.GetString("backColorPicker1.Items457"),
            resources.GetString("backColorPicker1.Items458"),
            resources.GetString("backColorPicker1.Items459"),
            resources.GetString("backColorPicker1.Items460"),
            resources.GetString("backColorPicker1.Items461"),
            resources.GetString("backColorPicker1.Items462"),
            resources.GetString("backColorPicker1.Items463"),
            resources.GetString("backColorPicker1.Items464"),
            resources.GetString("backColorPicker1.Items465"),
            resources.GetString("backColorPicker1.Items466"),
            resources.GetString("backColorPicker1.Items467"),
            resources.GetString("backColorPicker1.Items468"),
            resources.GetString("backColorPicker1.Items469"),
            resources.GetString("backColorPicker1.Items470"),
            resources.GetString("backColorPicker1.Items471"),
            resources.GetString("backColorPicker1.Items472"),
            resources.GetString("backColorPicker1.Items473"),
            resources.GetString("backColorPicker1.Items474"),
            resources.GetString("backColorPicker1.Items475"),
            resources.GetString("backColorPicker1.Items476"),
            resources.GetString("backColorPicker1.Items477"),
            resources.GetString("backColorPicker1.Items478"),
            resources.GetString("backColorPicker1.Items479"),
            resources.GetString("backColorPicker1.Items480"),
            resources.GetString("backColorPicker1.Items481"),
            resources.GetString("backColorPicker1.Items482"),
            resources.GetString("backColorPicker1.Items483"),
            resources.GetString("backColorPicker1.Items484"),
            resources.GetString("backColorPicker1.Items485"),
            resources.GetString("backColorPicker1.Items486"),
            resources.GetString("backColorPicker1.Items487"),
            resources.GetString("backColorPicker1.Items488"),
            resources.GetString("backColorPicker1.Items489"),
            resources.GetString("backColorPicker1.Items490"),
            resources.GetString("backColorPicker1.Items491"),
            resources.GetString("backColorPicker1.Items492"),
            resources.GetString("backColorPicker1.Items493"),
            resources.GetString("backColorPicker1.Items494"),
            resources.GetString("backColorPicker1.Items495"),
            resources.GetString("backColorPicker1.Items496"),
            resources.GetString("backColorPicker1.Items497"),
            resources.GetString("backColorPicker1.Items498"),
            resources.GetString("backColorPicker1.Items499"),
            resources.GetString("backColorPicker1.Items500"),
            resources.GetString("backColorPicker1.Items501"),
            resources.GetString("backColorPicker1.Items502"),
            resources.GetString("backColorPicker1.Items503"),
            resources.GetString("backColorPicker1.Items504"),
            resources.GetString("backColorPicker1.Items505"),
            resources.GetString("backColorPicker1.Items506"),
            resources.GetString("backColorPicker1.Items507"),
            resources.GetString("backColorPicker1.Items508"),
            resources.GetString("backColorPicker1.Items509"),
            resources.GetString("backColorPicker1.Items510"),
            resources.GetString("backColorPicker1.Items511"),
            resources.GetString("backColorPicker1.Items512"),
            resources.GetString("backColorPicker1.Items513"),
            resources.GetString("backColorPicker1.Items514"),
            resources.GetString("backColorPicker1.Items515"),
            resources.GetString("backColorPicker1.Items516"),
            resources.GetString("backColorPicker1.Items517"),
            resources.GetString("backColorPicker1.Items518"),
            resources.GetString("backColorPicker1.Items519"),
            resources.GetString("backColorPicker1.Items520"),
            resources.GetString("backColorPicker1.Items521"),
            resources.GetString("backColorPicker1.Items522"),
            resources.GetString("backColorPicker1.Items523"),
            resources.GetString("backColorPicker1.Items524"),
            resources.GetString("backColorPicker1.Items525"),
            resources.GetString("backColorPicker1.Items526"),
            resources.GetString("backColorPicker1.Items527"),
            resources.GetString("backColorPicker1.Items528"),
            resources.GetString("backColorPicker1.Items529"),
            resources.GetString("backColorPicker1.Items530"),
            resources.GetString("backColorPicker1.Items531"),
            resources.GetString("backColorPicker1.Items532"),
            resources.GetString("backColorPicker1.Items533"),
            resources.GetString("backColorPicker1.Items534"),
            resources.GetString("backColorPicker1.Items535"),
            resources.GetString("backColorPicker1.Items536"),
            resources.GetString("backColorPicker1.Items537"),
            resources.GetString("backColorPicker1.Items538"),
            resources.GetString("backColorPicker1.Items539"),
            resources.GetString("backColorPicker1.Items540"),
            resources.GetString("backColorPicker1.Items541"),
            resources.GetString("backColorPicker1.Items542"),
            resources.GetString("backColorPicker1.Items543"),
            resources.GetString("backColorPicker1.Items544"),
            resources.GetString("backColorPicker1.Items545"),
            resources.GetString("backColorPicker1.Items546"),
            resources.GetString("backColorPicker1.Items547"),
            resources.GetString("backColorPicker1.Items548"),
            resources.GetString("backColorPicker1.Items549"),
            resources.GetString("backColorPicker1.Items550"),
            resources.GetString("backColorPicker1.Items551"),
            resources.GetString("backColorPicker1.Items552"),
            resources.GetString("backColorPicker1.Items553"),
            resources.GetString("backColorPicker1.Items554"),
            resources.GetString("backColorPicker1.Items555"),
            resources.GetString("backColorPicker1.Items556"),
            resources.GetString("backColorPicker1.Items557"),
            resources.GetString("backColorPicker1.Items558"),
            resources.GetString("backColorPicker1.Items559"),
            resources.GetString("backColorPicker1.Items560"),
            resources.GetString("backColorPicker1.Items561"),
            resources.GetString("backColorPicker1.Items562"),
            resources.GetString("backColorPicker1.Items563"),
            resources.GetString("backColorPicker1.Items564"),
            resources.GetString("backColorPicker1.Items565"),
            resources.GetString("backColorPicker1.Items566"),
            resources.GetString("backColorPicker1.Items567"),
            resources.GetString("backColorPicker1.Items568"),
            resources.GetString("backColorPicker1.Items569"),
            resources.GetString("backColorPicker1.Items570"),
            resources.GetString("backColorPicker1.Items571"),
            resources.GetString("backColorPicker1.Items572"),
            resources.GetString("backColorPicker1.Items573"),
            resources.GetString("backColorPicker1.Items574"),
            resources.GetString("backColorPicker1.Items575"),
            resources.GetString("backColorPicker1.Items576"),
            resources.GetString("backColorPicker1.Items577"),
            resources.GetString("backColorPicker1.Items578"),
            resources.GetString("backColorPicker1.Items579"),
            resources.GetString("backColorPicker1.Items580"),
            resources.GetString("backColorPicker1.Items581"),
            resources.GetString("backColorPicker1.Items582"),
            resources.GetString("backColorPicker1.Items583"),
            resources.GetString("backColorPicker1.Items584"),
            resources.GetString("backColorPicker1.Items585"),
            resources.GetString("backColorPicker1.Items586"),
            resources.GetString("backColorPicker1.Items587"),
            resources.GetString("backColorPicker1.Items588"),
            resources.GetString("backColorPicker1.Items589"),
            resources.GetString("backColorPicker1.Items590"),
            resources.GetString("backColorPicker1.Items591"),
            resources.GetString("backColorPicker1.Items592"),
            resources.GetString("backColorPicker1.Items593"),
            resources.GetString("backColorPicker1.Items594"),
            resources.GetString("backColorPicker1.Items595"),
            resources.GetString("backColorPicker1.Items596"),
            resources.GetString("backColorPicker1.Items597"),
            resources.GetString("backColorPicker1.Items598"),
            resources.GetString("backColorPicker1.Items599"),
            resources.GetString("backColorPicker1.Items600"),
            resources.GetString("backColorPicker1.Items601"),
            resources.GetString("backColorPicker1.Items602"),
            resources.GetString("backColorPicker1.Items603"),
            resources.GetString("backColorPicker1.Items604"),
            resources.GetString("backColorPicker1.Items605"),
            resources.GetString("backColorPicker1.Items606"),
            resources.GetString("backColorPicker1.Items607"),
            resources.GetString("backColorPicker1.Items608"),
            resources.GetString("backColorPicker1.Items609"),
            resources.GetString("backColorPicker1.Items610"),
            resources.GetString("backColorPicker1.Items611"),
            resources.GetString("backColorPicker1.Items612"),
            resources.GetString("backColorPicker1.Items613"),
            resources.GetString("backColorPicker1.Items614"),
            resources.GetString("backColorPicker1.Items615"),
            resources.GetString("backColorPicker1.Items616"),
            resources.GetString("backColorPicker1.Items617"),
            resources.GetString("backColorPicker1.Items618"),
            resources.GetString("backColorPicker1.Items619"),
            resources.GetString("backColorPicker1.Items620"),
            resources.GetString("backColorPicker1.Items621"),
            resources.GetString("backColorPicker1.Items622"),
            resources.GetString("backColorPicker1.Items623"),
            resources.GetString("backColorPicker1.Items624"),
            resources.GetString("backColorPicker1.Items625"),
            resources.GetString("backColorPicker1.Items626"),
            resources.GetString("backColorPicker1.Items627"),
            resources.GetString("backColorPicker1.Items628"),
            resources.GetString("backColorPicker1.Items629"),
            resources.GetString("backColorPicker1.Items630"),
            resources.GetString("backColorPicker1.Items631"),
            resources.GetString("backColorPicker1.Items632"),
            resources.GetString("backColorPicker1.Items633"),
            resources.GetString("backColorPicker1.Items634"),
            resources.GetString("backColorPicker1.Items635"),
            resources.GetString("backColorPicker1.Items636"),
            resources.GetString("backColorPicker1.Items637"),
            resources.GetString("backColorPicker1.Items638"),
            resources.GetString("backColorPicker1.Items639"),
            resources.GetString("backColorPicker1.Items640"),
            resources.GetString("backColorPicker1.Items641"),
            resources.GetString("backColorPicker1.Items642"),
            resources.GetString("backColorPicker1.Items643"),
            resources.GetString("backColorPicker1.Items644"),
            resources.GetString("backColorPicker1.Items645"),
            resources.GetString("backColorPicker1.Items646"),
            resources.GetString("backColorPicker1.Items647"),
            resources.GetString("backColorPicker1.Items648"),
            resources.GetString("backColorPicker1.Items649"),
            resources.GetString("backColorPicker1.Items650"),
            resources.GetString("backColorPicker1.Items651"),
            resources.GetString("backColorPicker1.Items652"),
            resources.GetString("backColorPicker1.Items653"),
            resources.GetString("backColorPicker1.Items654"),
            resources.GetString("backColorPicker1.Items655"),
            resources.GetString("backColorPicker1.Items656"),
            resources.GetString("backColorPicker1.Items657"),
            resources.GetString("backColorPicker1.Items658"),
            resources.GetString("backColorPicker1.Items659"),
            resources.GetString("backColorPicker1.Items660"),
            resources.GetString("backColorPicker1.Items661"),
            resources.GetString("backColorPicker1.Items662"),
            resources.GetString("backColorPicker1.Items663"),
            resources.GetString("backColorPicker1.Items664"),
            resources.GetString("backColorPicker1.Items665"),
            resources.GetString("backColorPicker1.Items666"),
            resources.GetString("backColorPicker1.Items667"),
            resources.GetString("backColorPicker1.Items668"),
            resources.GetString("backColorPicker1.Items669"),
            resources.GetString("backColorPicker1.Items670"),
            resources.GetString("backColorPicker1.Items671"),
            resources.GetString("backColorPicker1.Items672"),
            resources.GetString("backColorPicker1.Items673"),
            resources.GetString("backColorPicker1.Items674"),
            resources.GetString("backColorPicker1.Items675"),
            resources.GetString("backColorPicker1.Items676"),
            resources.GetString("backColorPicker1.Items677"),
            resources.GetString("backColorPicker1.Items678"),
            resources.GetString("backColorPicker1.Items679"),
            resources.GetString("backColorPicker1.Items680"),
            resources.GetString("backColorPicker1.Items681"),
            resources.GetString("backColorPicker1.Items682"),
            resources.GetString("backColorPicker1.Items683"),
            resources.GetString("backColorPicker1.Items684"),
            resources.GetString("backColorPicker1.Items685"),
            resources.GetString("backColorPicker1.Items686"),
            resources.GetString("backColorPicker1.Items687"),
            resources.GetString("backColorPicker1.Items688"),
            resources.GetString("backColorPicker1.Items689"),
            resources.GetString("backColorPicker1.Items690"),
            resources.GetString("backColorPicker1.Items691"),
            resources.GetString("backColorPicker1.Items692"),
            resources.GetString("backColorPicker1.Items693"),
            resources.GetString("backColorPicker1.Items694"),
            resources.GetString("backColorPicker1.Items695"),
            resources.GetString("backColorPicker1.Items696"),
            resources.GetString("backColorPicker1.Items697"),
            resources.GetString("backColorPicker1.Items698"),
            resources.GetString("backColorPicker1.Items699"),
            resources.GetString("backColorPicker1.Items700"),
            resources.GetString("backColorPicker1.Items701"),
            resources.GetString("backColorPicker1.Items702"),
            resources.GetString("backColorPicker1.Items703"),
            resources.GetString("backColorPicker1.Items704"),
            resources.GetString("backColorPicker1.Items705"),
            resources.GetString("backColorPicker1.Items706"),
            resources.GetString("backColorPicker1.Items707"),
            resources.GetString("backColorPicker1.Items708"),
            resources.GetString("backColorPicker1.Items709"),
            resources.GetString("backColorPicker1.Items710"),
            resources.GetString("backColorPicker1.Items711"),
            resources.GetString("backColorPicker1.Items712"),
            resources.GetString("backColorPicker1.Items713"),
            resources.GetString("backColorPicker1.Items714"),
            resources.GetString("backColorPicker1.Items715"),
            resources.GetString("backColorPicker1.Items716"),
            resources.GetString("backColorPicker1.Items717"),
            resources.GetString("backColorPicker1.Items718"),
            resources.GetString("backColorPicker1.Items719"),
            resources.GetString("backColorPicker1.Items720"),
            resources.GetString("backColorPicker1.Items721"),
            resources.GetString("backColorPicker1.Items722"),
            resources.GetString("backColorPicker1.Items723"),
            resources.GetString("backColorPicker1.Items724"),
            resources.GetString("backColorPicker1.Items725"),
            resources.GetString("backColorPicker1.Items726"),
            resources.GetString("backColorPicker1.Items727"),
            resources.GetString("backColorPicker1.Items728"),
            resources.GetString("backColorPicker1.Items729"),
            resources.GetString("backColorPicker1.Items730"),
            resources.GetString("backColorPicker1.Items731"),
            resources.GetString("backColorPicker1.Items732"),
            resources.GetString("backColorPicker1.Items733"),
            resources.GetString("backColorPicker1.Items734"),
            resources.GetString("backColorPicker1.Items735"),
            resources.GetString("backColorPicker1.Items736"),
            resources.GetString("backColorPicker1.Items737"),
            resources.GetString("backColorPicker1.Items738"),
            resources.GetString("backColorPicker1.Items739"),
            resources.GetString("backColorPicker1.Items740"),
            resources.GetString("backColorPicker1.Items741"),
            resources.GetString("backColorPicker1.Items742"),
            resources.GetString("backColorPicker1.Items743"),
            resources.GetString("backColorPicker1.Items744"),
            resources.GetString("backColorPicker1.Items745"),
            resources.GetString("backColorPicker1.Items746"),
            resources.GetString("backColorPicker1.Items747"),
            resources.GetString("backColorPicker1.Items748"),
            resources.GetString("backColorPicker1.Items749"),
            resources.GetString("backColorPicker1.Items750"),
            resources.GetString("backColorPicker1.Items751"),
            resources.GetString("backColorPicker1.Items752"),
            resources.GetString("backColorPicker1.Items753"),
            resources.GetString("backColorPicker1.Items754"),
            resources.GetString("backColorPicker1.Items755"),
            resources.GetString("backColorPicker1.Items756"),
            resources.GetString("backColorPicker1.Items757"),
            resources.GetString("backColorPicker1.Items758"),
            resources.GetString("backColorPicker1.Items759"),
            resources.GetString("backColorPicker1.Items760"),
            resources.GetString("backColorPicker1.Items761"),
            resources.GetString("backColorPicker1.Items762"),
            resources.GetString("backColorPicker1.Items763"),
            resources.GetString("backColorPicker1.Items764"),
            resources.GetString("backColorPicker1.Items765"),
            resources.GetString("backColorPicker1.Items766"),
            resources.GetString("backColorPicker1.Items767"),
            resources.GetString("backColorPicker1.Items768"),
            resources.GetString("backColorPicker1.Items769"),
            resources.GetString("backColorPicker1.Items770"),
            resources.GetString("backColorPicker1.Items771"),
            resources.GetString("backColorPicker1.Items772"),
            resources.GetString("backColorPicker1.Items773"),
            resources.GetString("backColorPicker1.Items774"),
            resources.GetString("backColorPicker1.Items775"),
            resources.GetString("backColorPicker1.Items776"),
            resources.GetString("backColorPicker1.Items777"),
            resources.GetString("backColorPicker1.Items778"),
            resources.GetString("backColorPicker1.Items779"),
            resources.GetString("backColorPicker1.Items780"),
            resources.GetString("backColorPicker1.Items781"),
            resources.GetString("backColorPicker1.Items782"),
            resources.GetString("backColorPicker1.Items783"),
            resources.GetString("backColorPicker1.Items784"),
            resources.GetString("backColorPicker1.Items785"),
            resources.GetString("backColorPicker1.Items786"),
            resources.GetString("backColorPicker1.Items787"),
            resources.GetString("backColorPicker1.Items788"),
            resources.GetString("backColorPicker1.Items789"),
            resources.GetString("backColorPicker1.Items790"),
            resources.GetString("backColorPicker1.Items791"),
            resources.GetString("backColorPicker1.Items792"),
            resources.GetString("backColorPicker1.Items793"),
            resources.GetString("backColorPicker1.Items794"),
            resources.GetString("backColorPicker1.Items795"),
            resources.GetString("backColorPicker1.Items796"),
            resources.GetString("backColorPicker1.Items797"),
            resources.GetString("backColorPicker1.Items798"),
            resources.GetString("backColorPicker1.Items799"),
            resources.GetString("backColorPicker1.Items800"),
            resources.GetString("backColorPicker1.Items801"),
            resources.GetString("backColorPicker1.Items802"),
            resources.GetString("backColorPicker1.Items803"),
            resources.GetString("backColorPicker1.Items804"),
            resources.GetString("backColorPicker1.Items805"),
            resources.GetString("backColorPicker1.Items806"),
            resources.GetString("backColorPicker1.Items807"),
            resources.GetString("backColorPicker1.Items808"),
            resources.GetString("backColorPicker1.Items809"),
            resources.GetString("backColorPicker1.Items810"),
            resources.GetString("backColorPicker1.Items811"),
            resources.GetString("backColorPicker1.Items812"),
            resources.GetString("backColorPicker1.Items813"),
            resources.GetString("backColorPicker1.Items814"),
            resources.GetString("backColorPicker1.Items815"),
            resources.GetString("backColorPicker1.Items816"),
            resources.GetString("backColorPicker1.Items817"),
            resources.GetString("backColorPicker1.Items818"),
            resources.GetString("backColorPicker1.Items819"),
            resources.GetString("backColorPicker1.Items820"),
            resources.GetString("backColorPicker1.Items821"),
            resources.GetString("backColorPicker1.Items822"),
            resources.GetString("backColorPicker1.Items823"),
            resources.GetString("backColorPicker1.Items824"),
            resources.GetString("backColorPicker1.Items825"),
            resources.GetString("backColorPicker1.Items826"),
            resources.GetString("backColorPicker1.Items827"),
            resources.GetString("backColorPicker1.Items828"),
            resources.GetString("backColorPicker1.Items829"),
            resources.GetString("backColorPicker1.Items830"),
            resources.GetString("backColorPicker1.Items831"),
            resources.GetString("backColorPicker1.Items832"),
            resources.GetString("backColorPicker1.Items833"),
            resources.GetString("backColorPicker1.Items834"),
            resources.GetString("backColorPicker1.Items835"),
            resources.GetString("backColorPicker1.Items836"),
            resources.GetString("backColorPicker1.Items837"),
            resources.GetString("backColorPicker1.Items838"),
            resources.GetString("backColorPicker1.Items839"),
            resources.GetString("backColorPicker1.Items840"),
            resources.GetString("backColorPicker1.Items841"),
            resources.GetString("backColorPicker1.Items842"),
            resources.GetString("backColorPicker1.Items843"),
            resources.GetString("backColorPicker1.Items844"),
            resources.GetString("backColorPicker1.Items845"),
            resources.GetString("backColorPicker1.Items846"),
            resources.GetString("backColorPicker1.Items847"),
            resources.GetString("backColorPicker1.Items848"),
            resources.GetString("backColorPicker1.Items849"),
            resources.GetString("backColorPicker1.Items850"),
            resources.GetString("backColorPicker1.Items851"),
            resources.GetString("backColorPicker1.Items852"),
            resources.GetString("backColorPicker1.Items853"),
            resources.GetString("backColorPicker1.Items854"),
            resources.GetString("backColorPicker1.Items855"),
            resources.GetString("backColorPicker1.Items856"),
            resources.GetString("backColorPicker1.Items857"),
            resources.GetString("backColorPicker1.Items858"),
            resources.GetString("backColorPicker1.Items859"),
            resources.GetString("backColorPicker1.Items860"),
            resources.GetString("backColorPicker1.Items861"),
            resources.GetString("backColorPicker1.Items862"),
            resources.GetString("backColorPicker1.Items863"),
            resources.GetString("backColorPicker1.Items864"),
            resources.GetString("backColorPicker1.Items865"),
            resources.GetString("backColorPicker1.Items866"),
            resources.GetString("backColorPicker1.Items867"),
            resources.GetString("backColorPicker1.Items868"),
            resources.GetString("backColorPicker1.Items869"),
            resources.GetString("backColorPicker1.Items870"),
            resources.GetString("backColorPicker1.Items871"),
            resources.GetString("backColorPicker1.Items872"),
            resources.GetString("backColorPicker1.Items873"),
            resources.GetString("backColorPicker1.Items874"),
            resources.GetString("backColorPicker1.Items875"),
            resources.GetString("backColorPicker1.Items876"),
            resources.GetString("backColorPicker1.Items877"),
            resources.GetString("backColorPicker1.Items878"),
            resources.GetString("backColorPicker1.Items879"),
            resources.GetString("backColorPicker1.Items880"),
            resources.GetString("backColorPicker1.Items881"),
            resources.GetString("backColorPicker1.Items882"),
            resources.GetString("backColorPicker1.Items883"),
            resources.GetString("backColorPicker1.Items884"),
            resources.GetString("backColorPicker1.Items885"),
            resources.GetString("backColorPicker1.Items886"),
            resources.GetString("backColorPicker1.Items887"),
            resources.GetString("backColorPicker1.Items888"),
            resources.GetString("backColorPicker1.Items889"),
            resources.GetString("backColorPicker1.Items890"),
            resources.GetString("backColorPicker1.Items891"),
            resources.GetString("backColorPicker1.Items892"),
            resources.GetString("backColorPicker1.Items893"),
            resources.GetString("backColorPicker1.Items894"),
            resources.GetString("backColorPicker1.Items895"),
            resources.GetString("backColorPicker1.Items896"),
            resources.GetString("backColorPicker1.Items897"),
            resources.GetString("backColorPicker1.Items898"),
            resources.GetString("backColorPicker1.Items899"),
            resources.GetString("backColorPicker1.Items900"),
            resources.GetString("backColorPicker1.Items901"),
            resources.GetString("backColorPicker1.Items902"),
            resources.GetString("backColorPicker1.Items903"),
            resources.GetString("backColorPicker1.Items904"),
            resources.GetString("backColorPicker1.Items905"),
            resources.GetString("backColorPicker1.Items906"),
            resources.GetString("backColorPicker1.Items907"),
            resources.GetString("backColorPicker1.Items908"),
            resources.GetString("backColorPicker1.Items909"),
            resources.GetString("backColorPicker1.Items910"),
            resources.GetString("backColorPicker1.Items911"),
            resources.GetString("backColorPicker1.Items912"),
            resources.GetString("backColorPicker1.Items913"),
            resources.GetString("backColorPicker1.Items914"),
            resources.GetString("backColorPicker1.Items915"),
            resources.GetString("backColorPicker1.Items916"),
            resources.GetString("backColorPicker1.Items917"),
            resources.GetString("backColorPicker1.Items918"),
            resources.GetString("backColorPicker1.Items919"),
            resources.GetString("backColorPicker1.Items920"),
            resources.GetString("backColorPicker1.Items921"),
            resources.GetString("backColorPicker1.Items922"),
            resources.GetString("backColorPicker1.Items923"),
            resources.GetString("backColorPicker1.Items924"),
            resources.GetString("backColorPicker1.Items925"),
            resources.GetString("backColorPicker1.Items926"),
            resources.GetString("backColorPicker1.Items927"),
            resources.GetString("backColorPicker1.Items928"),
            resources.GetString("backColorPicker1.Items929"),
            resources.GetString("backColorPicker1.Items930"),
            resources.GetString("backColorPicker1.Items931"),
            resources.GetString("backColorPicker1.Items932"),
            resources.GetString("backColorPicker1.Items933"),
            resources.GetString("backColorPicker1.Items934"),
            resources.GetString("backColorPicker1.Items935"),
            resources.GetString("backColorPicker1.Items936"),
            resources.GetString("backColorPicker1.Items937"),
            resources.GetString("backColorPicker1.Items938"),
            resources.GetString("backColorPicker1.Items939"),
            resources.GetString("backColorPicker1.Items940"),
            resources.GetString("backColorPicker1.Items941"),
            resources.GetString("backColorPicker1.Items942"),
            resources.GetString("backColorPicker1.Items943"),
            resources.GetString("backColorPicker1.Items944"),
            resources.GetString("backColorPicker1.Items945"),
            resources.GetString("backColorPicker1.Items946"),
            resources.GetString("backColorPicker1.Items947"),
            resources.GetString("backColorPicker1.Items948"),
            resources.GetString("backColorPicker1.Items949"),
            resources.GetString("backColorPicker1.Items950"),
            resources.GetString("backColorPicker1.Items951"),
            resources.GetString("backColorPicker1.Items952"),
            resources.GetString("backColorPicker1.Items953"),
            resources.GetString("backColorPicker1.Items954"),
            resources.GetString("backColorPicker1.Items955"),
            resources.GetString("backColorPicker1.Items956"),
            resources.GetString("backColorPicker1.Items957"),
            resources.GetString("backColorPicker1.Items958"),
            resources.GetString("backColorPicker1.Items959"),
            resources.GetString("backColorPicker1.Items960"),
            resources.GetString("backColorPicker1.Items961"),
            resources.GetString("backColorPicker1.Items962"),
            resources.GetString("backColorPicker1.Items963"),
            resources.GetString("backColorPicker1.Items964"),
            resources.GetString("backColorPicker1.Items965"),
            resources.GetString("backColorPicker1.Items966"),
            resources.GetString("backColorPicker1.Items967"),
            resources.GetString("backColorPicker1.Items968"),
            resources.GetString("backColorPicker1.Items969"),
            resources.GetString("backColorPicker1.Items970"),
            resources.GetString("backColorPicker1.Items971"),
            resources.GetString("backColorPicker1.Items972"),
            resources.GetString("backColorPicker1.Items973"),
            resources.GetString("backColorPicker1.Items974"),
            resources.GetString("backColorPicker1.Items975"),
            resources.GetString("backColorPicker1.Items976"),
            resources.GetString("backColorPicker1.Items977"),
            resources.GetString("backColorPicker1.Items978"),
            resources.GetString("backColorPicker1.Items979"),
            resources.GetString("backColorPicker1.Items980"),
            resources.GetString("backColorPicker1.Items981"),
            resources.GetString("backColorPicker1.Items982"),
            resources.GetString("backColorPicker1.Items983"),
            resources.GetString("backColorPicker1.Items984"),
            resources.GetString("backColorPicker1.Items985"),
            resources.GetString("backColorPicker1.Items986"),
            resources.GetString("backColorPicker1.Items987"),
            resources.GetString("backColorPicker1.Items988"),
            resources.GetString("backColorPicker1.Items989"),
            resources.GetString("backColorPicker1.Items990"),
            resources.GetString("backColorPicker1.Items991"),
            resources.GetString("backColorPicker1.Items992"),
            resources.GetString("backColorPicker1.Items993"),
            resources.GetString("backColorPicker1.Items994"),
            resources.GetString("backColorPicker1.Items995"),
            resources.GetString("backColorPicker1.Items996"),
            resources.GetString("backColorPicker1.Items997"),
            resources.GetString("backColorPicker1.Items998"),
            resources.GetString("backColorPicker1.Items999"),
            resources.GetString("backColorPicker1.Items1000"),
            resources.GetString("backColorPicker1.Items1001"),
            resources.GetString("backColorPicker1.Items1002"),
            resources.GetString("backColorPicker1.Items1003"),
            resources.GetString("backColorPicker1.Items1004"),
            resources.GetString("backColorPicker1.Items1005"),
            resources.GetString("backColorPicker1.Items1006"),
            resources.GetString("backColorPicker1.Items1007"),
            resources.GetString("backColorPicker1.Items1008"),
            resources.GetString("backColorPicker1.Items1009"),
            resources.GetString("backColorPicker1.Items1010"),
            resources.GetString("backColorPicker1.Items1011"),
            resources.GetString("backColorPicker1.Items1012"),
            resources.GetString("backColorPicker1.Items1013"),
            resources.GetString("backColorPicker1.Items1014"),
            resources.GetString("backColorPicker1.Items1015"),
            resources.GetString("backColorPicker1.Items1016"),
            resources.GetString("backColorPicker1.Items1017"),
            resources.GetString("backColorPicker1.Items1018"),
            resources.GetString("backColorPicker1.Items1019"),
            resources.GetString("backColorPicker1.Items1020"),
            resources.GetString("backColorPicker1.Items1021"),
            resources.GetString("backColorPicker1.Items1022"),
            resources.GetString("backColorPicker1.Items1023"),
            resources.GetString("backColorPicker1.Items1024"),
            resources.GetString("backColorPicker1.Items1025"),
            resources.GetString("backColorPicker1.Items1026"),
            resources.GetString("backColorPicker1.Items1027"),
            resources.GetString("backColorPicker1.Items1028"),
            resources.GetString("backColorPicker1.Items1029"),
            resources.GetString("backColorPicker1.Items1030"),
            resources.GetString("backColorPicker1.Items1031"),
            resources.GetString("backColorPicker1.Items1032"),
            resources.GetString("backColorPicker1.Items1033"),
            resources.GetString("backColorPicker1.Items1034"),
            resources.GetString("backColorPicker1.Items1035"),
            resources.GetString("backColorPicker1.Items1036"),
            resources.GetString("backColorPicker1.Items1037"),
            resources.GetString("backColorPicker1.Items1038"),
            resources.GetString("backColorPicker1.Items1039"),
            resources.GetString("backColorPicker1.Items1040"),
            resources.GetString("backColorPicker1.Items1041"),
            resources.GetString("backColorPicker1.Items1042"),
            resources.GetString("backColorPicker1.Items1043"),
            resources.GetString("backColorPicker1.Items1044"),
            resources.GetString("backColorPicker1.Items1045"),
            resources.GetString("backColorPicker1.Items1046"),
            resources.GetString("backColorPicker1.Items1047"),
            resources.GetString("backColorPicker1.Items1048"),
            resources.GetString("backColorPicker1.Items1049"),
            resources.GetString("backColorPicker1.Items1050"),
            resources.GetString("backColorPicker1.Items1051"),
            resources.GetString("backColorPicker1.Items1052"),
            resources.GetString("backColorPicker1.Items1053"),
            resources.GetString("backColorPicker1.Items1054"),
            resources.GetString("backColorPicker1.Items1055"),
            resources.GetString("backColorPicker1.Items1056"),
            resources.GetString("backColorPicker1.Items1057"),
            resources.GetString("backColorPicker1.Items1058"),
            resources.GetString("backColorPicker1.Items1059"),
            resources.GetString("backColorPicker1.Items1060"),
            resources.GetString("backColorPicker1.Items1061"),
            resources.GetString("backColorPicker1.Items1062"),
            resources.GetString("backColorPicker1.Items1063"),
            resources.GetString("backColorPicker1.Items1064"),
            resources.GetString("backColorPicker1.Items1065"),
            resources.GetString("backColorPicker1.Items1066"),
            resources.GetString("backColorPicker1.Items1067"),
            resources.GetString("backColorPicker1.Items1068"),
            resources.GetString("backColorPicker1.Items1069"),
            resources.GetString("backColorPicker1.Items1070"),
            resources.GetString("backColorPicker1.Items1071"),
            resources.GetString("backColorPicker1.Items1072"),
            resources.GetString("backColorPicker1.Items1073"),
            resources.GetString("backColorPicker1.Items1074"),
            resources.GetString("backColorPicker1.Items1075"),
            resources.GetString("backColorPicker1.Items1076"),
            resources.GetString("backColorPicker1.Items1077"),
            resources.GetString("backColorPicker1.Items1078"),
            resources.GetString("backColorPicker1.Items1079"),
            resources.GetString("backColorPicker1.Items1080"),
            resources.GetString("backColorPicker1.Items1081"),
            resources.GetString("backColorPicker1.Items1082"),
            resources.GetString("backColorPicker1.Items1083"),
            resources.GetString("backColorPicker1.Items1084"),
            resources.GetString("backColorPicker1.Items1085"),
            resources.GetString("backColorPicker1.Items1086"),
            resources.GetString("backColorPicker1.Items1087"),
            resources.GetString("backColorPicker1.Items1088"),
            resources.GetString("backColorPicker1.Items1089"),
            resources.GetString("backColorPicker1.Items1090"),
            resources.GetString("backColorPicker1.Items1091"),
            resources.GetString("backColorPicker1.Items1092"),
            resources.GetString("backColorPicker1.Items1093"),
            resources.GetString("backColorPicker1.Items1094"),
            resources.GetString("backColorPicker1.Items1095"),
            resources.GetString("backColorPicker1.Items1096"),
            resources.GetString("backColorPicker1.Items1097"),
            resources.GetString("backColorPicker1.Items1098"),
            resources.GetString("backColorPicker1.Items1099"),
            resources.GetString("backColorPicker1.Items1100"),
            resources.GetString("backColorPicker1.Items1101"),
            resources.GetString("backColorPicker1.Items1102"),
            resources.GetString("backColorPicker1.Items1103"),
            resources.GetString("backColorPicker1.Items1104"),
            resources.GetString("backColorPicker1.Items1105"),
            resources.GetString("backColorPicker1.Items1106"),
            resources.GetString("backColorPicker1.Items1107"),
            resources.GetString("backColorPicker1.Items1108"),
            resources.GetString("backColorPicker1.Items1109"),
            resources.GetString("backColorPicker1.Items1110"),
            resources.GetString("backColorPicker1.Items1111"),
            resources.GetString("backColorPicker1.Items1112"),
            resources.GetString("backColorPicker1.Items1113"),
            resources.GetString("backColorPicker1.Items1114"),
            resources.GetString("backColorPicker1.Items1115"),
            resources.GetString("backColorPicker1.Items1116"),
            resources.GetString("backColorPicker1.Items1117"),
            resources.GetString("backColorPicker1.Items1118"),
            resources.GetString("backColorPicker1.Items1119"),
            resources.GetString("backColorPicker1.Items1120"),
            resources.GetString("backColorPicker1.Items1121"),
            resources.GetString("backColorPicker1.Items1122"),
            resources.GetString("backColorPicker1.Items1123"),
            resources.GetString("backColorPicker1.Items1124"),
            resources.GetString("backColorPicker1.Items1125"),
            resources.GetString("backColorPicker1.Items1126"),
            resources.GetString("backColorPicker1.Items1127"),
            resources.GetString("backColorPicker1.Items1128"),
            resources.GetString("backColorPicker1.Items1129"),
            resources.GetString("backColorPicker1.Items1130"),
            resources.GetString("backColorPicker1.Items1131"),
            resources.GetString("backColorPicker1.Items1132"),
            resources.GetString("backColorPicker1.Items1133"),
            resources.GetString("backColorPicker1.Items1134"),
            resources.GetString("backColorPicker1.Items1135"),
            resources.GetString("backColorPicker1.Items1136"),
            resources.GetString("backColorPicker1.Items1137"),
            resources.GetString("backColorPicker1.Items1138"),
            resources.GetString("backColorPicker1.Items1139"),
            resources.GetString("backColorPicker1.Items1140"),
            resources.GetString("backColorPicker1.Items1141"),
            resources.GetString("backColorPicker1.Items1142"),
            resources.GetString("backColorPicker1.Items1143"),
            resources.GetString("backColorPicker1.Items1144"),
            resources.GetString("backColorPicker1.Items1145"),
            resources.GetString("backColorPicker1.Items1146"),
            resources.GetString("backColorPicker1.Items1147"),
            resources.GetString("backColorPicker1.Items1148"),
            resources.GetString("backColorPicker1.Items1149"),
            resources.GetString("backColorPicker1.Items1150"),
            resources.GetString("backColorPicker1.Items1151"),
            resources.GetString("backColorPicker1.Items1152"),
            resources.GetString("backColorPicker1.Items1153"),
            resources.GetString("backColorPicker1.Items1154"),
            resources.GetString("backColorPicker1.Items1155"),
            resources.GetString("backColorPicker1.Items1156"),
            resources.GetString("backColorPicker1.Items1157"),
            resources.GetString("backColorPicker1.Items1158"),
            resources.GetString("backColorPicker1.Items1159"),
            resources.GetString("backColorPicker1.Items1160"),
            resources.GetString("backColorPicker1.Items1161"),
            resources.GetString("backColorPicker1.Items1162"),
            resources.GetString("backColorPicker1.Items1163"),
            resources.GetString("backColorPicker1.Items1164"),
            resources.GetString("backColorPicker1.Items1165"),
            resources.GetString("backColorPicker1.Items1166"),
            resources.GetString("backColorPicker1.Items1167"),
            resources.GetString("backColorPicker1.Items1168"),
            resources.GetString("backColorPicker1.Items1169"),
            resources.GetString("backColorPicker1.Items1170"),
            resources.GetString("backColorPicker1.Items1171"),
            resources.GetString("backColorPicker1.Items1172"),
            resources.GetString("backColorPicker1.Items1173"),
            resources.GetString("backColorPicker1.Items1174"),
            resources.GetString("backColorPicker1.Items1175"),
            resources.GetString("backColorPicker1.Items1176"),
            resources.GetString("backColorPicker1.Items1177"),
            resources.GetString("backColorPicker1.Items1178"),
            resources.GetString("backColorPicker1.Items1179"),
            resources.GetString("backColorPicker1.Items1180"),
            resources.GetString("backColorPicker1.Items1181"),
            resources.GetString("backColorPicker1.Items1182"),
            resources.GetString("backColorPicker1.Items1183"),
            resources.GetString("backColorPicker1.Items1184"),
            resources.GetString("backColorPicker1.Items1185"),
            resources.GetString("backColorPicker1.Items1186"),
            resources.GetString("backColorPicker1.Items1187"),
            resources.GetString("backColorPicker1.Items1188"),
            resources.GetString("backColorPicker1.Items1189"),
            resources.GetString("backColorPicker1.Items1190"),
            resources.GetString("backColorPicker1.Items1191"),
            resources.GetString("backColorPicker1.Items1192"),
            resources.GetString("backColorPicker1.Items1193"),
            resources.GetString("backColorPicker1.Items1194"),
            resources.GetString("backColorPicker1.Items1195"),
            resources.GetString("backColorPicker1.Items1196"),
            resources.GetString("backColorPicker1.Items1197"),
            resources.GetString("backColorPicker1.Items1198"),
            resources.GetString("backColorPicker1.Items1199"),
            resources.GetString("backColorPicker1.Items1200"),
            resources.GetString("backColorPicker1.Items1201"),
            resources.GetString("backColorPicker1.Items1202"),
            resources.GetString("backColorPicker1.Items1203"),
            resources.GetString("backColorPicker1.Items1204"),
            resources.GetString("backColorPicker1.Items1205"),
            resources.GetString("backColorPicker1.Items1206"),
            resources.GetString("backColorPicker1.Items1207"),
            resources.GetString("backColorPicker1.Items1208"),
            resources.GetString("backColorPicker1.Items1209"),
            resources.GetString("backColorPicker1.Items1210"),
            resources.GetString("backColorPicker1.Items1211"),
            resources.GetString("backColorPicker1.Items1212"),
            resources.GetString("backColorPicker1.Items1213"),
            resources.GetString("backColorPicker1.Items1214"),
            resources.GetString("backColorPicker1.Items1215"),
            resources.GetString("backColorPicker1.Items1216"),
            resources.GetString("backColorPicker1.Items1217"),
            resources.GetString("backColorPicker1.Items1218"),
            resources.GetString("backColorPicker1.Items1219"),
            resources.GetString("backColorPicker1.Items1220"),
            resources.GetString("backColorPicker1.Items1221"),
            resources.GetString("backColorPicker1.Items1222"),
            resources.GetString("backColorPicker1.Items1223"),
            resources.GetString("backColorPicker1.Items1224"),
            resources.GetString("backColorPicker1.Items1225"),
            resources.GetString("backColorPicker1.Items1226"),
            resources.GetString("backColorPicker1.Items1227"),
            resources.GetString("backColorPicker1.Items1228"),
            resources.GetString("backColorPicker1.Items1229"),
            resources.GetString("backColorPicker1.Items1230"),
            resources.GetString("backColorPicker1.Items1231"),
            resources.GetString("backColorPicker1.Items1232"),
            resources.GetString("backColorPicker1.Items1233"),
            resources.GetString("backColorPicker1.Items1234"),
            resources.GetString("backColorPicker1.Items1235"),
            resources.GetString("backColorPicker1.Items1236"),
            resources.GetString("backColorPicker1.Items1237"),
            resources.GetString("backColorPicker1.Items1238"),
            resources.GetString("backColorPicker1.Items1239"),
            resources.GetString("backColorPicker1.Items1240"),
            resources.GetString("backColorPicker1.Items1241"),
            resources.GetString("backColorPicker1.Items1242"),
            resources.GetString("backColorPicker1.Items1243"),
            resources.GetString("backColorPicker1.Items1244"),
            resources.GetString("backColorPicker1.Items1245"),
            resources.GetString("backColorPicker1.Items1246"),
            resources.GetString("backColorPicker1.Items1247"),
            resources.GetString("backColorPicker1.Items1248"),
            resources.GetString("backColorPicker1.Items1249"),
            resources.GetString("backColorPicker1.Items1250"),
            resources.GetString("backColorPicker1.Items1251"),
            resources.GetString("backColorPicker1.Items1252"),
            resources.GetString("backColorPicker1.Items1253"),
            resources.GetString("backColorPicker1.Items1254"),
            resources.GetString("backColorPicker1.Items1255"),
            resources.GetString("backColorPicker1.Items1256"),
            resources.GetString("backColorPicker1.Items1257"),
            resources.GetString("backColorPicker1.Items1258"),
            resources.GetString("backColorPicker1.Items1259"),
            resources.GetString("backColorPicker1.Items1260"),
            resources.GetString("backColorPicker1.Items1261"),
            resources.GetString("backColorPicker1.Items1262"),
            resources.GetString("backColorPicker1.Items1263"),
            resources.GetString("backColorPicker1.Items1264"),
            resources.GetString("backColorPicker1.Items1265"),
            resources.GetString("backColorPicker1.Items1266"),
            resources.GetString("backColorPicker1.Items1267"),
            resources.GetString("backColorPicker1.Items1268"),
            resources.GetString("backColorPicker1.Items1269"),
            resources.GetString("backColorPicker1.Items1270"),
            resources.GetString("backColorPicker1.Items1271"),
            resources.GetString("backColorPicker1.Items1272"),
            resources.GetString("backColorPicker1.Items1273"),
            resources.GetString("backColorPicker1.Items1274"),
            resources.GetString("backColorPicker1.Items1275"),
            resources.GetString("backColorPicker1.Items1276"),
            resources.GetString("backColorPicker1.Items1277"),
            resources.GetString("backColorPicker1.Items1278"),
            resources.GetString("backColorPicker1.Items1279"),
            resources.GetString("backColorPicker1.Items1280"),
            resources.GetString("backColorPicker1.Items1281"),
            resources.GetString("backColorPicker1.Items1282"),
            resources.GetString("backColorPicker1.Items1283"),
            resources.GetString("backColorPicker1.Items1284"),
            resources.GetString("backColorPicker1.Items1285"),
            resources.GetString("backColorPicker1.Items1286"),
            resources.GetString("backColorPicker1.Items1287"),
            resources.GetString("backColorPicker1.Items1288"),
            resources.GetString("backColorPicker1.Items1289"),
            resources.GetString("backColorPicker1.Items1290"),
            resources.GetString("backColorPicker1.Items1291"),
            resources.GetString("backColorPicker1.Items1292"),
            resources.GetString("backColorPicker1.Items1293"),
            resources.GetString("backColorPicker1.Items1294"),
            resources.GetString("backColorPicker1.Items1295"),
            resources.GetString("backColorPicker1.Items1296"),
            resources.GetString("backColorPicker1.Items1297"),
            resources.GetString("backColorPicker1.Items1298"),
            resources.GetString("backColorPicker1.Items1299"),
            resources.GetString("backColorPicker1.Items1300"),
            resources.GetString("backColorPicker1.Items1301"),
            resources.GetString("backColorPicker1.Items1302"),
            resources.GetString("backColorPicker1.Items1303"),
            resources.GetString("backColorPicker1.Items1304"),
            resources.GetString("backColorPicker1.Items1305"),
            resources.GetString("backColorPicker1.Items1306"),
            resources.GetString("backColorPicker1.Items1307"),
            resources.GetString("backColorPicker1.Items1308"),
            resources.GetString("backColorPicker1.Items1309"),
            resources.GetString("backColorPicker1.Items1310"),
            resources.GetString("backColorPicker1.Items1311"),
            resources.GetString("backColorPicker1.Items1312"),
            resources.GetString("backColorPicker1.Items1313"),
            resources.GetString("backColorPicker1.Items1314"),
            resources.GetString("backColorPicker1.Items1315"),
            resources.GetString("backColorPicker1.Items1316"),
            resources.GetString("backColorPicker1.Items1317"),
            resources.GetString("backColorPicker1.Items1318"),
            resources.GetString("backColorPicker1.Items1319"),
            resources.GetString("backColorPicker1.Items1320"),
            resources.GetString("backColorPicker1.Items1321"),
            resources.GetString("backColorPicker1.Items1322"),
            resources.GetString("backColorPicker1.Items1323"),
            resources.GetString("backColorPicker1.Items1324"),
            resources.GetString("backColorPicker1.Items1325"),
            resources.GetString("backColorPicker1.Items1326"),
            resources.GetString("backColorPicker1.Items1327"),
            resources.GetString("backColorPicker1.Items1328"),
            resources.GetString("backColorPicker1.Items1329"),
            resources.GetString("backColorPicker1.Items1330"),
            resources.GetString("backColorPicker1.Items1331"),
            resources.GetString("backColorPicker1.Items1332"),
            resources.GetString("backColorPicker1.Items1333"),
            resources.GetString("backColorPicker1.Items1334"),
            resources.GetString("backColorPicker1.Items1335"),
            resources.GetString("backColorPicker1.Items1336"),
            resources.GetString("backColorPicker1.Items1337"),
            resources.GetString("backColorPicker1.Items1338"),
            resources.GetString("backColorPicker1.Items1339"),
            resources.GetString("backColorPicker1.Items1340"),
            resources.GetString("backColorPicker1.Items1341"),
            resources.GetString("backColorPicker1.Items1342"),
            resources.GetString("backColorPicker1.Items1343"),
            resources.GetString("backColorPicker1.Items1344"),
            resources.GetString("backColorPicker1.Items1345"),
            resources.GetString("backColorPicker1.Items1346"),
            resources.GetString("backColorPicker1.Items1347"),
            resources.GetString("backColorPicker1.Items1348"),
            resources.GetString("backColorPicker1.Items1349"),
            resources.GetString("backColorPicker1.Items1350"),
            resources.GetString("backColorPicker1.Items1351"),
            resources.GetString("backColorPicker1.Items1352"),
            resources.GetString("backColorPicker1.Items1353"),
            resources.GetString("backColorPicker1.Items1354"),
            resources.GetString("backColorPicker1.Items1355"),
            resources.GetString("backColorPicker1.Items1356"),
            resources.GetString("backColorPicker1.Items1357"),
            resources.GetString("backColorPicker1.Items1358"),
            resources.GetString("backColorPicker1.Items1359"),
            resources.GetString("backColorPicker1.Items1360"),
            resources.GetString("backColorPicker1.Items1361"),
            resources.GetString("backColorPicker1.Items1362"),
            resources.GetString("backColorPicker1.Items1363"),
            resources.GetString("backColorPicker1.Items1364"),
            resources.GetString("backColorPicker1.Items1365"),
            resources.GetString("backColorPicker1.Items1366"),
            resources.GetString("backColorPicker1.Items1367"),
            resources.GetString("backColorPicker1.Items1368"),
            resources.GetString("backColorPicker1.Items1369"),
            resources.GetString("backColorPicker1.Items1370"),
            resources.GetString("backColorPicker1.Items1371"),
            resources.GetString("backColorPicker1.Items1372"),
            resources.GetString("backColorPicker1.Items1373"),
            resources.GetString("backColorPicker1.Items1374"),
            resources.GetString("backColorPicker1.Items1375"),
            resources.GetString("backColorPicker1.Items1376"),
            resources.GetString("backColorPicker1.Items1377"),
            resources.GetString("backColorPicker1.Items1378"),
            resources.GetString("backColorPicker1.Items1379"),
            resources.GetString("backColorPicker1.Items1380"),
            resources.GetString("backColorPicker1.Items1381"),
            resources.GetString("backColorPicker1.Items1382"),
            resources.GetString("backColorPicker1.Items1383"),
            resources.GetString("backColorPicker1.Items1384"),
            resources.GetString("backColorPicker1.Items1385"),
            resources.GetString("backColorPicker1.Items1386"),
            resources.GetString("backColorPicker1.Items1387"),
            resources.GetString("backColorPicker1.Items1388"),
            resources.GetString("backColorPicker1.Items1389"),
            resources.GetString("backColorPicker1.Items1390"),
            resources.GetString("backColorPicker1.Items1391"),
            resources.GetString("backColorPicker1.Items1392"),
            resources.GetString("backColorPicker1.Items1393"),
            resources.GetString("backColorPicker1.Items1394"),
            resources.GetString("backColorPicker1.Items1395"),
            resources.GetString("backColorPicker1.Items1396"),
            resources.GetString("backColorPicker1.Items1397"),
            resources.GetString("backColorPicker1.Items1398"),
            resources.GetString("backColorPicker1.Items1399"),
            resources.GetString("backColorPicker1.Items1400"),
            resources.GetString("backColorPicker1.Items1401"),
            resources.GetString("backColorPicker1.Items1402"),
            resources.GetString("backColorPicker1.Items1403"),
            resources.GetString("backColorPicker1.Items1404"),
            resources.GetString("backColorPicker1.Items1405"),
            resources.GetString("backColorPicker1.Items1406"),
            resources.GetString("backColorPicker1.Items1407"),
            resources.GetString("backColorPicker1.Items1408"),
            resources.GetString("backColorPicker1.Items1409"),
            resources.GetString("backColorPicker1.Items1410"),
            resources.GetString("backColorPicker1.Items1411"),
            resources.GetString("backColorPicker1.Items1412"),
            resources.GetString("backColorPicker1.Items1413"),
            resources.GetString("backColorPicker1.Items1414"),
            resources.GetString("backColorPicker1.Items1415"),
            resources.GetString("backColorPicker1.Items1416"),
            resources.GetString("backColorPicker1.Items1417"),
            resources.GetString("backColorPicker1.Items1418"),
            resources.GetString("backColorPicker1.Items1419"),
            resources.GetString("backColorPicker1.Items1420"),
            resources.GetString("backColorPicker1.Items1421"),
            resources.GetString("backColorPicker1.Items1422"),
            resources.GetString("backColorPicker1.Items1423"),
            resources.GetString("backColorPicker1.Items1424"),
            resources.GetString("backColorPicker1.Items1425"),
            resources.GetString("backColorPicker1.Items1426"),
            resources.GetString("backColorPicker1.Items1427"),
            resources.GetString("backColorPicker1.Items1428"),
            resources.GetString("backColorPicker1.Items1429"),
            resources.GetString("backColorPicker1.Items1430"),
            resources.GetString("backColorPicker1.Items1431"),
            resources.GetString("backColorPicker1.Items1432"),
            resources.GetString("backColorPicker1.Items1433"),
            resources.GetString("backColorPicker1.Items1434"),
            resources.GetString("backColorPicker1.Items1435"),
            resources.GetString("backColorPicker1.Items1436"),
            resources.GetString("backColorPicker1.Items1437"),
            resources.GetString("backColorPicker1.Items1438"),
            resources.GetString("backColorPicker1.Items1439"),
            resources.GetString("backColorPicker1.Items1440"),
            resources.GetString("backColorPicker1.Items1441"),
            resources.GetString("backColorPicker1.Items1442"),
            resources.GetString("backColorPicker1.Items1443"),
            resources.GetString("backColorPicker1.Items1444"),
            resources.GetString("backColorPicker1.Items1445"),
            resources.GetString("backColorPicker1.Items1446"),
            resources.GetString("backColorPicker1.Items1447"),
            resources.GetString("backColorPicker1.Items1448"),
            resources.GetString("backColorPicker1.Items1449"),
            resources.GetString("backColorPicker1.Items1450"),
            resources.GetString("backColorPicker1.Items1451"),
            resources.GetString("backColorPicker1.Items1452"),
            resources.GetString("backColorPicker1.Items1453"),
            resources.GetString("backColorPicker1.Items1454"),
            resources.GetString("backColorPicker1.Items1455"),
            resources.GetString("backColorPicker1.Items1456"),
            resources.GetString("backColorPicker1.Items1457"),
            resources.GetString("backColorPicker1.Items1458"),
            resources.GetString("backColorPicker1.Items1459"),
            resources.GetString("backColorPicker1.Items1460"),
            resources.GetString("backColorPicker1.Items1461"),
            resources.GetString("backColorPicker1.Items1462"),
            resources.GetString("backColorPicker1.Items1463"),
            resources.GetString("backColorPicker1.Items1464"),
            resources.GetString("backColorPicker1.Items1465"),
            resources.GetString("backColorPicker1.Items1466"),
            resources.GetString("backColorPicker1.Items1467"),
            resources.GetString("backColorPicker1.Items1468"),
            resources.GetString("backColorPicker1.Items1469"),
            resources.GetString("backColorPicker1.Items1470"),
            resources.GetString("backColorPicker1.Items1471"),
            resources.GetString("backColorPicker1.Items1472"),
            resources.GetString("backColorPicker1.Items1473"),
            resources.GetString("backColorPicker1.Items1474"),
            resources.GetString("backColorPicker1.Items1475"),
            resources.GetString("backColorPicker1.Items1476"),
            resources.GetString("backColorPicker1.Items1477"),
            resources.GetString("backColorPicker1.Items1478"),
            resources.GetString("backColorPicker1.Items1479"),
            resources.GetString("backColorPicker1.Items1480"),
            resources.GetString("backColorPicker1.Items1481"),
            resources.GetString("backColorPicker1.Items1482"),
            resources.GetString("backColorPicker1.Items1483"),
            resources.GetString("backColorPicker1.Items1484"),
            resources.GetString("backColorPicker1.Items1485"),
            resources.GetString("backColorPicker1.Items1486"),
            resources.GetString("backColorPicker1.Items1487"),
            resources.GetString("backColorPicker1.Items1488"),
            resources.GetString("backColorPicker1.Items1489"),
            resources.GetString("backColorPicker1.Items1490"),
            resources.GetString("backColorPicker1.Items1491"),
            resources.GetString("backColorPicker1.Items1492"),
            resources.GetString("backColorPicker1.Items1493"),
            resources.GetString("backColorPicker1.Items1494"),
            resources.GetString("backColorPicker1.Items1495"),
            resources.GetString("backColorPicker1.Items1496"),
            resources.GetString("backColorPicker1.Items1497"),
            resources.GetString("backColorPicker1.Items1498"),
            resources.GetString("backColorPicker1.Items1499"),
            resources.GetString("backColorPicker1.Items1500"),
            resources.GetString("backColorPicker1.Items1501"),
            resources.GetString("backColorPicker1.Items1502"),
            resources.GetString("backColorPicker1.Items1503"),
            resources.GetString("backColorPicker1.Items1504"),
            resources.GetString("backColorPicker1.Items1505"),
            resources.GetString("backColorPicker1.Items1506"),
            resources.GetString("backColorPicker1.Items1507"),
            resources.GetString("backColorPicker1.Items1508"),
            resources.GetString("backColorPicker1.Items1509"),
            resources.GetString("backColorPicker1.Items1510"),
            resources.GetString("backColorPicker1.Items1511"),
            resources.GetString("backColorPicker1.Items1512"),
            resources.GetString("backColorPicker1.Items1513"),
            resources.GetString("backColorPicker1.Items1514"),
            resources.GetString("backColorPicker1.Items1515"),
            resources.GetString("backColorPicker1.Items1516"),
            resources.GetString("backColorPicker1.Items1517"),
            resources.GetString("backColorPicker1.Items1518"),
            resources.GetString("backColorPicker1.Items1519"),
            resources.GetString("backColorPicker1.Items1520"),
            resources.GetString("backColorPicker1.Items1521"),
            resources.GetString("backColorPicker1.Items1522"),
            resources.GetString("backColorPicker1.Items1523"),
            resources.GetString("backColorPicker1.Items1524"),
            resources.GetString("backColorPicker1.Items1525"),
            resources.GetString("backColorPicker1.Items1526"),
            resources.GetString("backColorPicker1.Items1527"),
            resources.GetString("backColorPicker1.Items1528"),
            resources.GetString("backColorPicker1.Items1529"),
            resources.GetString("backColorPicker1.Items1530"),
            resources.GetString("backColorPicker1.Items1531"),
            resources.GetString("backColorPicker1.Items1532"),
            resources.GetString("backColorPicker1.Items1533"),
            resources.GetString("backColorPicker1.Items1534"),
            resources.GetString("backColorPicker1.Items1535"),
            resources.GetString("backColorPicker1.Items1536"),
            resources.GetString("backColorPicker1.Items1537"),
            resources.GetString("backColorPicker1.Items1538"),
            resources.GetString("backColorPicker1.Items1539"),
            resources.GetString("backColorPicker1.Items1540"),
            resources.GetString("backColorPicker1.Items1541"),
            resources.GetString("backColorPicker1.Items1542"),
            resources.GetString("backColorPicker1.Items1543"),
            resources.GetString("backColorPicker1.Items1544"),
            resources.GetString("backColorPicker1.Items1545"),
            resources.GetString("backColorPicker1.Items1546"),
            resources.GetString("backColorPicker1.Items1547"),
            resources.GetString("backColorPicker1.Items1548"),
            resources.GetString("backColorPicker1.Items1549"),
            resources.GetString("backColorPicker1.Items1550"),
            resources.GetString("backColorPicker1.Items1551"),
            resources.GetString("backColorPicker1.Items1552"),
            resources.GetString("backColorPicker1.Items1553"),
            resources.GetString("backColorPicker1.Items1554"),
            resources.GetString("backColorPicker1.Items1555"),
            resources.GetString("backColorPicker1.Items1556"),
            resources.GetString("backColorPicker1.Items1557"),
            resources.GetString("backColorPicker1.Items1558"),
            resources.GetString("backColorPicker1.Items1559"),
            resources.GetString("backColorPicker1.Items1560"),
            resources.GetString("backColorPicker1.Items1561"),
            resources.GetString("backColorPicker1.Items1562"),
            resources.GetString("backColorPicker1.Items1563"),
            resources.GetString("backColorPicker1.Items1564"),
            resources.GetString("backColorPicker1.Items1565"),
            resources.GetString("backColorPicker1.Items1566"),
            resources.GetString("backColorPicker1.Items1567"),
            resources.GetString("backColorPicker1.Items1568"),
            resources.GetString("backColorPicker1.Items1569"),
            resources.GetString("backColorPicker1.Items1570"),
            resources.GetString("backColorPicker1.Items1571"),
            resources.GetString("backColorPicker1.Items1572"),
            resources.GetString("backColorPicker1.Items1573"),
            resources.GetString("backColorPicker1.Items1574"),
            resources.GetString("backColorPicker1.Items1575"),
            resources.GetString("backColorPicker1.Items1576"),
            resources.GetString("backColorPicker1.Items1577"),
            resources.GetString("backColorPicker1.Items1578"),
            resources.GetString("backColorPicker1.Items1579"),
            resources.GetString("backColorPicker1.Items1580"),
            resources.GetString("backColorPicker1.Items1581"),
            resources.GetString("backColorPicker1.Items1582"),
            resources.GetString("backColorPicker1.Items1583"),
            resources.GetString("backColorPicker1.Items1584"),
            resources.GetString("backColorPicker1.Items1585"),
            resources.GetString("backColorPicker1.Items1586"),
            resources.GetString("backColorPicker1.Items1587"),
            resources.GetString("backColorPicker1.Items1588"),
            resources.GetString("backColorPicker1.Items1589"),
            resources.GetString("backColorPicker1.Items1590"),
            resources.GetString("backColorPicker1.Items1591"),
            resources.GetString("backColorPicker1.Items1592"),
            resources.GetString("backColorPicker1.Items1593"),
            resources.GetString("backColorPicker1.Items1594"),
            resources.GetString("backColorPicker1.Items1595"),
            resources.GetString("backColorPicker1.Items1596"),
            resources.GetString("backColorPicker1.Items1597"),
            resources.GetString("backColorPicker1.Items1598"),
            resources.GetString("backColorPicker1.Items1599"),
            resources.GetString("backColorPicker1.Items1600"),
            resources.GetString("backColorPicker1.Items1601"),
            resources.GetString("backColorPicker1.Items1602"),
            resources.GetString("backColorPicker1.Items1603"),
            resources.GetString("backColorPicker1.Items1604"),
            resources.GetString("backColorPicker1.Items1605"),
            resources.GetString("backColorPicker1.Items1606"),
            resources.GetString("backColorPicker1.Items1607"),
            resources.GetString("backColorPicker1.Items1608"),
            resources.GetString("backColorPicker1.Items1609"),
            resources.GetString("backColorPicker1.Items1610"),
            resources.GetString("backColorPicker1.Items1611"),
            resources.GetString("backColorPicker1.Items1612"),
            resources.GetString("backColorPicker1.Items1613"),
            resources.GetString("backColorPicker1.Items1614"),
            resources.GetString("backColorPicker1.Items1615"),
            resources.GetString("backColorPicker1.Items1616"),
            resources.GetString("backColorPicker1.Items1617"),
            resources.GetString("backColorPicker1.Items1618"),
            resources.GetString("backColorPicker1.Items1619"),
            resources.GetString("backColorPicker1.Items1620"),
            resources.GetString("backColorPicker1.Items1621"),
            resources.GetString("backColorPicker1.Items1622"),
            resources.GetString("backColorPicker1.Items1623"),
            resources.GetString("backColorPicker1.Items1624"),
            resources.GetString("backColorPicker1.Items1625"),
            resources.GetString("backColorPicker1.Items1626"),
            resources.GetString("backColorPicker1.Items1627"),
            resources.GetString("backColorPicker1.Items1628"),
            resources.GetString("backColorPicker1.Items1629"),
            resources.GetString("backColorPicker1.Items1630"),
            resources.GetString("backColorPicker1.Items1631"),
            resources.GetString("backColorPicker1.Items1632"),
            resources.GetString("backColorPicker1.Items1633"),
            resources.GetString("backColorPicker1.Items1634"),
            resources.GetString("backColorPicker1.Items1635"),
            resources.GetString("backColorPicker1.Items1636"),
            resources.GetString("backColorPicker1.Items1637"),
            resources.GetString("backColorPicker1.Items1638"),
            resources.GetString("backColorPicker1.Items1639"),
            resources.GetString("backColorPicker1.Items1640"),
            resources.GetString("backColorPicker1.Items1641"),
            resources.GetString("backColorPicker1.Items1642"),
            resources.GetString("backColorPicker1.Items1643"),
            resources.GetString("backColorPicker1.Items1644"),
            resources.GetString("backColorPicker1.Items1645"),
            resources.GetString("backColorPicker1.Items1646"),
            resources.GetString("backColorPicker1.Items1647"),
            resources.GetString("backColorPicker1.Items1648"),
            resources.GetString("backColorPicker1.Items1649"),
            resources.GetString("backColorPicker1.Items1650"),
            resources.GetString("backColorPicker1.Items1651"),
            resources.GetString("backColorPicker1.Items1652"),
            resources.GetString("backColorPicker1.Items1653"),
            resources.GetString("backColorPicker1.Items1654"),
            resources.GetString("backColorPicker1.Items1655"),
            resources.GetString("backColorPicker1.Items1656"),
            resources.GetString("backColorPicker1.Items1657"),
            resources.GetString("backColorPicker1.Items1658"),
            resources.GetString("backColorPicker1.Items1659"),
            resources.GetString("backColorPicker1.Items1660"),
            resources.GetString("backColorPicker1.Items1661"),
            resources.GetString("backColorPicker1.Items1662"),
            resources.GetString("backColorPicker1.Items1663"),
            resources.GetString("backColorPicker1.Items1664"),
            resources.GetString("backColorPicker1.Items1665"),
            resources.GetString("backColorPicker1.Items1666"),
            resources.GetString("backColorPicker1.Items1667"),
            resources.GetString("backColorPicker1.Items1668"),
            resources.GetString("backColorPicker1.Items1669"),
            resources.GetString("backColorPicker1.Items1670"),
            resources.GetString("backColorPicker1.Items1671"),
            resources.GetString("backColorPicker1.Items1672"),
            resources.GetString("backColorPicker1.Items1673"),
            resources.GetString("backColorPicker1.Items1674"),
            resources.GetString("backColorPicker1.Items1675"),
            resources.GetString("backColorPicker1.Items1676"),
            resources.GetString("backColorPicker1.Items1677"),
            resources.GetString("backColorPicker1.Items1678"),
            resources.GetString("backColorPicker1.Items1679"),
            resources.GetString("backColorPicker1.Items1680"),
            resources.GetString("backColorPicker1.Items1681"),
            resources.GetString("backColorPicker1.Items1682"),
            resources.GetString("backColorPicker1.Items1683"),
            resources.GetString("backColorPicker1.Items1684"),
            resources.GetString("backColorPicker1.Items1685"),
            resources.GetString("backColorPicker1.Items1686"),
            resources.GetString("backColorPicker1.Items1687"),
            resources.GetString("backColorPicker1.Items1688"),
            resources.GetString("backColorPicker1.Items1689"),
            resources.GetString("backColorPicker1.Items1690"),
            resources.GetString("backColorPicker1.Items1691"),
            resources.GetString("backColorPicker1.Items1692"),
            resources.GetString("backColorPicker1.Items1693"),
            resources.GetString("backColorPicker1.Items1694"),
            resources.GetString("backColorPicker1.Items1695"),
            resources.GetString("backColorPicker1.Items1696"),
            resources.GetString("backColorPicker1.Items1697"),
            resources.GetString("backColorPicker1.Items1698"),
            resources.GetString("backColorPicker1.Items1699"),
            resources.GetString("backColorPicker1.Items1700"),
            resources.GetString("backColorPicker1.Items1701"),
            resources.GetString("backColorPicker1.Items1702"),
            resources.GetString("backColorPicker1.Items1703"),
            resources.GetString("backColorPicker1.Items1704"),
            resources.GetString("backColorPicker1.Items1705"),
            resources.GetString("backColorPicker1.Items1706"),
            resources.GetString("backColorPicker1.Items1707"),
            resources.GetString("backColorPicker1.Items1708"),
            resources.GetString("backColorPicker1.Items1709"),
            resources.GetString("backColorPicker1.Items1710"),
            resources.GetString("backColorPicker1.Items1711"),
            resources.GetString("backColorPicker1.Items1712"),
            resources.GetString("backColorPicker1.Items1713"),
            resources.GetString("backColorPicker1.Items1714"),
            resources.GetString("backColorPicker1.Items1715"),
            resources.GetString("backColorPicker1.Items1716"),
            resources.GetString("backColorPicker1.Items1717"),
            resources.GetString("backColorPicker1.Items1718"),
            resources.GetString("backColorPicker1.Items1719"),
            resources.GetString("backColorPicker1.Items1720"),
            resources.GetString("backColorPicker1.Items1721"),
            resources.GetString("backColorPicker1.Items1722"),
            resources.GetString("backColorPicker1.Items1723"),
            resources.GetString("backColorPicker1.Items1724"),
            resources.GetString("backColorPicker1.Items1725"),
            resources.GetString("backColorPicker1.Items1726"),
            resources.GetString("backColorPicker1.Items1727"),
            resources.GetString("backColorPicker1.Items1728"),
            resources.GetString("backColorPicker1.Items1729"),
            resources.GetString("backColorPicker1.Items1730"),
            resources.GetString("backColorPicker1.Items1731"),
            resources.GetString("backColorPicker1.Items1732"),
            resources.GetString("backColorPicker1.Items1733"),
            resources.GetString("backColorPicker1.Items1734"),
            resources.GetString("backColorPicker1.Items1735"),
            resources.GetString("backColorPicker1.Items1736"),
            resources.GetString("backColorPicker1.Items1737"),
            resources.GetString("backColorPicker1.Items1738"),
            resources.GetString("backColorPicker1.Items1739"),
            resources.GetString("backColorPicker1.Items1740"),
            resources.GetString("backColorPicker1.Items1741"),
            resources.GetString("backColorPicker1.Items1742"),
            resources.GetString("backColorPicker1.Items1743"),
            resources.GetString("backColorPicker1.Items1744"),
            resources.GetString("backColorPicker1.Items1745"),
            resources.GetString("backColorPicker1.Items1746"),
            resources.GetString("backColorPicker1.Items1747"),
            resources.GetString("backColorPicker1.Items1748"),
            resources.GetString("backColorPicker1.Items1749"),
            resources.GetString("backColorPicker1.Items1750"),
            resources.GetString("backColorPicker1.Items1751"),
            resources.GetString("backColorPicker1.Items1752"),
            resources.GetString("backColorPicker1.Items1753"),
            resources.GetString("backColorPicker1.Items1754"),
            resources.GetString("backColorPicker1.Items1755"),
            resources.GetString("backColorPicker1.Items1756"),
            resources.GetString("backColorPicker1.Items1757"),
            resources.GetString("backColorPicker1.Items1758"),
            resources.GetString("backColorPicker1.Items1759"),
            resources.GetString("backColorPicker1.Items1760"),
            resources.GetString("backColorPicker1.Items1761"),
            resources.GetString("backColorPicker1.Items1762"),
            resources.GetString("backColorPicker1.Items1763"),
            resources.GetString("backColorPicker1.Items1764"),
            resources.GetString("backColorPicker1.Items1765"),
            resources.GetString("backColorPicker1.Items1766"),
            resources.GetString("backColorPicker1.Items1767"),
            resources.GetString("backColorPicker1.Items1768"),
            resources.GetString("backColorPicker1.Items1769"),
            resources.GetString("backColorPicker1.Items1770"),
            resources.GetString("backColorPicker1.Items1771"),
            resources.GetString("backColorPicker1.Items1772"),
            resources.GetString("backColorPicker1.Items1773"),
            resources.GetString("backColorPicker1.Items1774"),
            resources.GetString("backColorPicker1.Items1775"),
            resources.GetString("backColorPicker1.Items1776"),
            resources.GetString("backColorPicker1.Items1777"),
            resources.GetString("backColorPicker1.Items1778"),
            resources.GetString("backColorPicker1.Items1779"),
            resources.GetString("backColorPicker1.Items1780"),
            resources.GetString("backColorPicker1.Items1781"),
            resources.GetString("backColorPicker1.Items1782"),
            resources.GetString("backColorPicker1.Items1783"),
            resources.GetString("backColorPicker1.Items1784"),
            resources.GetString("backColorPicker1.Items1785"),
            resources.GetString("backColorPicker1.Items1786"),
            resources.GetString("backColorPicker1.Items1787"),
            resources.GetString("backColorPicker1.Items1788"),
            resources.GetString("backColorPicker1.Items1789"),
            resources.GetString("backColorPicker1.Items1790"),
            resources.GetString("backColorPicker1.Items1791"),
            resources.GetString("backColorPicker1.Items1792"),
            resources.GetString("backColorPicker1.Items1793"),
            resources.GetString("backColorPicker1.Items1794"),
            resources.GetString("backColorPicker1.Items1795"),
            resources.GetString("backColorPicker1.Items1796"),
            resources.GetString("backColorPicker1.Items1797"),
            resources.GetString("backColorPicker1.Items1798"),
            resources.GetString("backColorPicker1.Items1799"),
            resources.GetString("backColorPicker1.Items1800"),
            resources.GetString("backColorPicker1.Items1801"),
            resources.GetString("backColorPicker1.Items1802"),
            resources.GetString("backColorPicker1.Items1803"),
            resources.GetString("backColorPicker1.Items1804"),
            resources.GetString("backColorPicker1.Items1805"),
            resources.GetString("backColorPicker1.Items1806"),
            resources.GetString("backColorPicker1.Items1807"),
            resources.GetString("backColorPicker1.Items1808"),
            resources.GetString("backColorPicker1.Items1809"),
            resources.GetString("backColorPicker1.Items1810"),
            resources.GetString("backColorPicker1.Items1811"),
            resources.GetString("backColorPicker1.Items1812"),
            resources.GetString("backColorPicker1.Items1813"),
            resources.GetString("backColorPicker1.Items1814"),
            resources.GetString("backColorPicker1.Items1815"),
            resources.GetString("backColorPicker1.Items1816"),
            resources.GetString("backColorPicker1.Items1817"),
            resources.GetString("backColorPicker1.Items1818"),
            resources.GetString("backColorPicker1.Items1819"),
            resources.GetString("backColorPicker1.Items1820"),
            resources.GetString("backColorPicker1.Items1821"),
            resources.GetString("backColorPicker1.Items1822"),
            resources.GetString("backColorPicker1.Items1823"),
            resources.GetString("backColorPicker1.Items1824"),
            resources.GetString("backColorPicker1.Items1825"),
            resources.GetString("backColorPicker1.Items1826"),
            resources.GetString("backColorPicker1.Items1827"),
            resources.GetString("backColorPicker1.Items1828"),
            resources.GetString("backColorPicker1.Items1829"),
            resources.GetString("backColorPicker1.Items1830"),
            resources.GetString("backColorPicker1.Items1831"),
            resources.GetString("backColorPicker1.Items1832"),
            resources.GetString("backColorPicker1.Items1833"),
            resources.GetString("backColorPicker1.Items1834"),
            resources.GetString("backColorPicker1.Items1835"),
            resources.GetString("backColorPicker1.Items1836"),
            resources.GetString("backColorPicker1.Items1837"),
            resources.GetString("backColorPicker1.Items1838"),
            resources.GetString("backColorPicker1.Items1839"),
            resources.GetString("backColorPicker1.Items1840"),
            resources.GetString("backColorPicker1.Items1841"),
            resources.GetString("backColorPicker1.Items1842"),
            resources.GetString("backColorPicker1.Items1843"),
            resources.GetString("backColorPicker1.Items1844"),
            resources.GetString("backColorPicker1.Items1845"),
            resources.GetString("backColorPicker1.Items1846"),
            resources.GetString("backColorPicker1.Items1847"),
            resources.GetString("backColorPicker1.Items1848"),
            resources.GetString("backColorPicker1.Items1849"),
            resources.GetString("backColorPicker1.Items1850"),
            resources.GetString("backColorPicker1.Items1851"),
            resources.GetString("backColorPicker1.Items1852"),
            resources.GetString("backColorPicker1.Items1853"),
            resources.GetString("backColorPicker1.Items1854"),
            resources.GetString("backColorPicker1.Items1855"),
            resources.GetString("backColorPicker1.Items1856"),
            resources.GetString("backColorPicker1.Items1857"),
            resources.GetString("backColorPicker1.Items1858"),
            resources.GetString("backColorPicker1.Items1859"),
            resources.GetString("backColorPicker1.Items1860"),
            resources.GetString("backColorPicker1.Items1861"),
            resources.GetString("backColorPicker1.Items1862"),
            resources.GetString("backColorPicker1.Items1863"),
            resources.GetString("backColorPicker1.Items1864"),
            resources.GetString("backColorPicker1.Items1865"),
            resources.GetString("backColorPicker1.Items1866"),
            resources.GetString("backColorPicker1.Items1867"),
            resources.GetString("backColorPicker1.Items1868"),
            resources.GetString("backColorPicker1.Items1869"),
            resources.GetString("backColorPicker1.Items1870"),
            resources.GetString("backColorPicker1.Items1871"),
            resources.GetString("backColorPicker1.Items1872"),
            resources.GetString("backColorPicker1.Items1873"),
            resources.GetString("backColorPicker1.Items1874"),
            resources.GetString("backColorPicker1.Items1875"),
            resources.GetString("backColorPicker1.Items1876"),
            resources.GetString("backColorPicker1.Items1877"),
            resources.GetString("backColorPicker1.Items1878"),
            resources.GetString("backColorPicker1.Items1879"),
            resources.GetString("backColorPicker1.Items1880"),
            resources.GetString("backColorPicker1.Items1881"),
            resources.GetString("backColorPicker1.Items1882"),
            resources.GetString("backColorPicker1.Items1883"),
            resources.GetString("backColorPicker1.Items1884"),
            resources.GetString("backColorPicker1.Items1885"),
            resources.GetString("backColorPicker1.Items1886"),
            resources.GetString("backColorPicker1.Items1887"),
            resources.GetString("backColorPicker1.Items1888"),
            resources.GetString("backColorPicker1.Items1889"),
            resources.GetString("backColorPicker1.Items1890"),
            resources.GetString("backColorPicker1.Items1891"),
            resources.GetString("backColorPicker1.Items1892"),
            resources.GetString("backColorPicker1.Items1893"),
            resources.GetString("backColorPicker1.Items1894"),
            resources.GetString("backColorPicker1.Items1895"),
            resources.GetString("backColorPicker1.Items1896"),
            resources.GetString("backColorPicker1.Items1897"),
            resources.GetString("backColorPicker1.Items1898"),
            resources.GetString("backColorPicker1.Items1899"),
            resources.GetString("backColorPicker1.Items1900"),
            resources.GetString("backColorPicker1.Items1901"),
            resources.GetString("backColorPicker1.Items1902"),
            resources.GetString("backColorPicker1.Items1903"),
            resources.GetString("backColorPicker1.Items1904"),
            resources.GetString("backColorPicker1.Items1905"),
            resources.GetString("backColorPicker1.Items1906"),
            resources.GetString("backColorPicker1.Items1907"),
            resources.GetString("backColorPicker1.Items1908"),
            resources.GetString("backColorPicker1.Items1909"),
            resources.GetString("backColorPicker1.Items1910"),
            resources.GetString("backColorPicker1.Items1911"),
            resources.GetString("backColorPicker1.Items1912"),
            resources.GetString("backColorPicker1.Items1913"),
            resources.GetString("backColorPicker1.Items1914"),
            resources.GetString("backColorPicker1.Items1915"),
            resources.GetString("backColorPicker1.Items1916"),
            resources.GetString("backColorPicker1.Items1917"),
            resources.GetString("backColorPicker1.Items1918"),
            resources.GetString("backColorPicker1.Items1919"),
            resources.GetString("backColorPicker1.Items1920"),
            resources.GetString("backColorPicker1.Items1921"),
            resources.GetString("backColorPicker1.Items1922"),
            resources.GetString("backColorPicker1.Items1923"),
            resources.GetString("backColorPicker1.Items1924"),
            resources.GetString("backColorPicker1.Items1925"),
            resources.GetString("backColorPicker1.Items1926"),
            resources.GetString("backColorPicker1.Items1927"),
            resources.GetString("backColorPicker1.Items1928"),
            resources.GetString("backColorPicker1.Items1929"),
            resources.GetString("backColorPicker1.Items1930"),
            resources.GetString("backColorPicker1.Items1931"),
            resources.GetString("backColorPicker1.Items1932"),
            resources.GetString("backColorPicker1.Items1933"),
            resources.GetString("backColorPicker1.Items1934"),
            resources.GetString("backColorPicker1.Items1935"),
            resources.GetString("backColorPicker1.Items1936"),
            resources.GetString("backColorPicker1.Items1937"),
            resources.GetString("backColorPicker1.Items1938"),
            resources.GetString("backColorPicker1.Items1939"),
            resources.GetString("backColorPicker1.Items1940"),
            resources.GetString("backColorPicker1.Items1941"),
            resources.GetString("backColorPicker1.Items1942"),
            resources.GetString("backColorPicker1.Items1943"),
            resources.GetString("backColorPicker1.Items1944"),
            resources.GetString("backColorPicker1.Items1945"),
            resources.GetString("backColorPicker1.Items1946"),
            resources.GetString("backColorPicker1.Items1947"),
            resources.GetString("backColorPicker1.Items1948"),
            resources.GetString("backColorPicker1.Items1949"),
            resources.GetString("backColorPicker1.Items1950"),
            resources.GetString("backColorPicker1.Items1951"),
            resources.GetString("backColorPicker1.Items1952"),
            resources.GetString("backColorPicker1.Items1953"),
            resources.GetString("backColorPicker1.Items1954"),
            resources.GetString("backColorPicker1.Items1955"),
            resources.GetString("backColorPicker1.Items1956"),
            resources.GetString("backColorPicker1.Items1957"),
            resources.GetString("backColorPicker1.Items1958"),
            resources.GetString("backColorPicker1.Items1959"),
            resources.GetString("backColorPicker1.Items1960"),
            resources.GetString("backColorPicker1.Items1961"),
            resources.GetString("backColorPicker1.Items1962"),
            resources.GetString("backColorPicker1.Items1963"),
            resources.GetString("backColorPicker1.Items1964"),
            resources.GetString("backColorPicker1.Items1965"),
            resources.GetString("backColorPicker1.Items1966"),
            resources.GetString("backColorPicker1.Items1967"),
            resources.GetString("backColorPicker1.Items1968"),
            resources.GetString("backColorPicker1.Items1969"),
            resources.GetString("backColorPicker1.Items1970"),
            resources.GetString("backColorPicker1.Items1971"),
            resources.GetString("backColorPicker1.Items1972"),
            resources.GetString("backColorPicker1.Items1973"),
            resources.GetString("backColorPicker1.Items1974"),
            resources.GetString("backColorPicker1.Items1975"),
            resources.GetString("backColorPicker1.Items1976"),
            resources.GetString("backColorPicker1.Items1977"),
            resources.GetString("backColorPicker1.Items1978"),
            resources.GetString("backColorPicker1.Items1979"),
            resources.GetString("backColorPicker1.Items1980"),
            resources.GetString("backColorPicker1.Items1981"),
            resources.GetString("backColorPicker1.Items1982"),
            resources.GetString("backColorPicker1.Items1983"),
            resources.GetString("backColorPicker1.Items1984"),
            resources.GetString("backColorPicker1.Items1985"),
            resources.GetString("backColorPicker1.Items1986"),
            resources.GetString("backColorPicker1.Items1987"),
            resources.GetString("backColorPicker1.Items1988"),
            resources.GetString("backColorPicker1.Items1989"),
            resources.GetString("backColorPicker1.Items1990"),
            resources.GetString("backColorPicker1.Items1991"),
            resources.GetString("backColorPicker1.Items1992"),
            resources.GetString("backColorPicker1.Items1993"),
            resources.GetString("backColorPicker1.Items1994"),
            resources.GetString("backColorPicker1.Items1995"),
            resources.GetString("backColorPicker1.Items1996"),
            resources.GetString("backColorPicker1.Items1997"),
            resources.GetString("backColorPicker1.Items1998"),
            resources.GetString("backColorPicker1.Items1999"),
            resources.GetString("backColorPicker1.Items2000"),
            resources.GetString("backColorPicker1.Items2001"),
            resources.GetString("backColorPicker1.Items2002"),
            resources.GetString("backColorPicker1.Items2003"),
            resources.GetString("backColorPicker1.Items2004"),
            resources.GetString("backColorPicker1.Items2005"),
            resources.GetString("backColorPicker1.Items2006"),
            resources.GetString("backColorPicker1.Items2007"),
            resources.GetString("backColorPicker1.Items2008"),
            resources.GetString("backColorPicker1.Items2009"),
            resources.GetString("backColorPicker1.Items2010"),
            resources.GetString("backColorPicker1.Items2011"),
            resources.GetString("backColorPicker1.Items2012"),
            resources.GetString("backColorPicker1.Items2013"),
            resources.GetString("backColorPicker1.Items2014"),
            resources.GetString("backColorPicker1.Items2015"),
            resources.GetString("backColorPicker1.Items2016"),
            resources.GetString("backColorPicker1.Items2017"),
            resources.GetString("backColorPicker1.Items2018"),
            resources.GetString("backColorPicker1.Items2019"),
            resources.GetString("backColorPicker1.Items2020"),
            resources.GetString("backColorPicker1.Items2021"),
            resources.GetString("backColorPicker1.Items2022"),
            resources.GetString("backColorPicker1.Items2023"),
            resources.GetString("backColorPicker1.Items2024"),
            resources.GetString("backColorPicker1.Items2025"),
            resources.GetString("backColorPicker1.Items2026"),
            resources.GetString("backColorPicker1.Items2027"),
            resources.GetString("backColorPicker1.Items2028"),
            resources.GetString("backColorPicker1.Items2029"),
            resources.GetString("backColorPicker1.Items2030"),
            resources.GetString("backColorPicker1.Items2031"),
            resources.GetString("backColorPicker1.Items2032"),
            resources.GetString("backColorPicker1.Items2033"),
            resources.GetString("backColorPicker1.Items2034"),
            resources.GetString("backColorPicker1.Items2035"),
            resources.GetString("backColorPicker1.Items2036"),
            resources.GetString("backColorPicker1.Items2037"),
            resources.GetString("backColorPicker1.Items2038"),
            resources.GetString("backColorPicker1.Items2039"),
            resources.GetString("backColorPicker1.Items2040"),
            resources.GetString("backColorPicker1.Items2041"),
            resources.GetString("backColorPicker1.Items2042"),
            resources.GetString("backColorPicker1.Items2043"),
            resources.GetString("backColorPicker1.Items2044"),
            resources.GetString("backColorPicker1.Items2045"),
            resources.GetString("backColorPicker1.Items2046"),
            resources.GetString("backColorPicker1.Items2047"),
            resources.GetString("backColorPicker1.Items2048"),
            resources.GetString("backColorPicker1.Items2049"),
            resources.GetString("backColorPicker1.Items2050"),
            resources.GetString("backColorPicker1.Items2051"),
            resources.GetString("backColorPicker1.Items2052"),
            resources.GetString("backColorPicker1.Items2053"),
            resources.GetString("backColorPicker1.Items2054"),
            resources.GetString("backColorPicker1.Items2055"),
            resources.GetString("backColorPicker1.Items2056"),
            resources.GetString("backColorPicker1.Items2057"),
            resources.GetString("backColorPicker1.Items2058"),
            resources.GetString("backColorPicker1.Items2059"),
            resources.GetString("backColorPicker1.Items2060"),
            resources.GetString("backColorPicker1.Items2061"),
            resources.GetString("backColorPicker1.Items2062"),
            resources.GetString("backColorPicker1.Items2063"),
            resources.GetString("backColorPicker1.Items2064"),
            resources.GetString("backColorPicker1.Items2065"),
            resources.GetString("backColorPicker1.Items2066"),
            resources.GetString("backColorPicker1.Items2067"),
            resources.GetString("backColorPicker1.Items2068"),
            resources.GetString("backColorPicker1.Items2069"),
            resources.GetString("backColorPicker1.Items2070"),
            resources.GetString("backColorPicker1.Items2071"),
            resources.GetString("backColorPicker1.Items2072"),
            resources.GetString("backColorPicker1.Items2073"),
            resources.GetString("backColorPicker1.Items2074"),
            resources.GetString("backColorPicker1.Items2075"),
            resources.GetString("backColorPicker1.Items2076"),
            resources.GetString("backColorPicker1.Items2077"),
            resources.GetString("backColorPicker1.Items2078"),
            resources.GetString("backColorPicker1.Items2079"),
            resources.GetString("backColorPicker1.Items2080"),
            resources.GetString("backColorPicker1.Items2081"),
            resources.GetString("backColorPicker1.Items2082"),
            resources.GetString("backColorPicker1.Items2083"),
            resources.GetString("backColorPicker1.Items2084"),
            resources.GetString("backColorPicker1.Items2085"),
            resources.GetString("backColorPicker1.Items2086"),
            resources.GetString("backColorPicker1.Items2087"),
            resources.GetString("backColorPicker1.Items2088"),
            resources.GetString("backColorPicker1.Items2089"),
            resources.GetString("backColorPicker1.Items2090"),
            resources.GetString("backColorPicker1.Items2091"),
            resources.GetString("backColorPicker1.Items2092"),
            resources.GetString("backColorPicker1.Items2093"),
            resources.GetString("backColorPicker1.Items2094"),
            resources.GetString("backColorPicker1.Items2095"),
            resources.GetString("backColorPicker1.Items2096"),
            resources.GetString("backColorPicker1.Items2097"),
            resources.GetString("backColorPicker1.Items2098"),
            resources.GetString("backColorPicker1.Items2099"),
            resources.GetString("backColorPicker1.Items2100"),
            resources.GetString("backColorPicker1.Items2101"),
            resources.GetString("backColorPicker1.Items2102"),
            resources.GetString("backColorPicker1.Items2103"),
            resources.GetString("backColorPicker1.Items2104"),
            resources.GetString("backColorPicker1.Items2105"),
            resources.GetString("backColorPicker1.Items2106"),
            resources.GetString("backColorPicker1.Items2107"),
            resources.GetString("backColorPicker1.Items2108"),
            resources.GetString("backColorPicker1.Items2109"),
            resources.GetString("backColorPicker1.Items2110"),
            resources.GetString("backColorPicker1.Items2111"),
            resources.GetString("backColorPicker1.Items2112"),
            resources.GetString("backColorPicker1.Items2113"),
            resources.GetString("backColorPicker1.Items2114"),
            resources.GetString("backColorPicker1.Items2115"),
            resources.GetString("backColorPicker1.Items2116"),
            resources.GetString("backColorPicker1.Items2117"),
            resources.GetString("backColorPicker1.Items2118"),
            resources.GetString("backColorPicker1.Items2119"),
            resources.GetString("backColorPicker1.Items2120"),
            resources.GetString("backColorPicker1.Items2121"),
            resources.GetString("backColorPicker1.Items2122"),
            resources.GetString("backColorPicker1.Items2123"),
            resources.GetString("backColorPicker1.Items2124"),
            resources.GetString("backColorPicker1.Items2125"),
            resources.GetString("backColorPicker1.Items2126"),
            resources.GetString("backColorPicker1.Items2127"),
            resources.GetString("backColorPicker1.Items2128"),
            resources.GetString("backColorPicker1.Items2129"),
            resources.GetString("backColorPicker1.Items2130"),
            resources.GetString("backColorPicker1.Items2131"),
            resources.GetString("backColorPicker1.Items2132"),
            resources.GetString("backColorPicker1.Items2133"),
            resources.GetString("backColorPicker1.Items2134"),
            resources.GetString("backColorPicker1.Items2135"),
            resources.GetString("backColorPicker1.Items2136"),
            resources.GetString("backColorPicker1.Items2137"),
            resources.GetString("backColorPicker1.Items2138"),
            resources.GetString("backColorPicker1.Items2139"),
            resources.GetString("backColorPicker1.Items2140"),
            resources.GetString("backColorPicker1.Items2141"),
            resources.GetString("backColorPicker1.Items2142"),
            resources.GetString("backColorPicker1.Items2143"),
            resources.GetString("backColorPicker1.Items2144"),
            resources.GetString("backColorPicker1.Items2145"),
            resources.GetString("backColorPicker1.Items2146"),
            resources.GetString("backColorPicker1.Items2147"),
            resources.GetString("backColorPicker1.Items2148"),
            resources.GetString("backColorPicker1.Items2149"),
            resources.GetString("backColorPicker1.Items2150"),
            resources.GetString("backColorPicker1.Items2151"),
            resources.GetString("backColorPicker1.Items2152"),
            resources.GetString("backColorPicker1.Items2153"),
            resources.GetString("backColorPicker1.Items2154"),
            resources.GetString("backColorPicker1.Items2155"),
            resources.GetString("backColorPicker1.Items2156"),
            resources.GetString("backColorPicker1.Items2157"),
            resources.GetString("backColorPicker1.Items2158"),
            resources.GetString("backColorPicker1.Items2159"),
            resources.GetString("backColorPicker1.Items2160"),
            resources.GetString("backColorPicker1.Items2161"),
            resources.GetString("backColorPicker1.Items2162"),
            resources.GetString("backColorPicker1.Items2163"),
            resources.GetString("backColorPicker1.Items2164"),
            resources.GetString("backColorPicker1.Items2165"),
            resources.GetString("backColorPicker1.Items2166"),
            resources.GetString("backColorPicker1.Items2167"),
            resources.GetString("backColorPicker1.Items2168"),
            resources.GetString("backColorPicker1.Items2169"),
            resources.GetString("backColorPicker1.Items2170"),
            resources.GetString("backColorPicker1.Items2171"),
            resources.GetString("backColorPicker1.Items2172"),
            resources.GetString("backColorPicker1.Items2173"),
            resources.GetString("backColorPicker1.Items2174"),
            resources.GetString("backColorPicker1.Items2175"),
            resources.GetString("backColorPicker1.Items2176"),
            resources.GetString("backColorPicker1.Items2177"),
            resources.GetString("backColorPicker1.Items2178"),
            resources.GetString("backColorPicker1.Items2179"),
            resources.GetString("backColorPicker1.Items2180"),
            resources.GetString("backColorPicker1.Items2181"),
            resources.GetString("backColorPicker1.Items2182"),
            resources.GetString("backColorPicker1.Items2183"),
            resources.GetString("backColorPicker1.Items2184"),
            resources.GetString("backColorPicker1.Items2185"),
            resources.GetString("backColorPicker1.Items2186"),
            resources.GetString("backColorPicker1.Items2187"),
            resources.GetString("backColorPicker1.Items2188"),
            resources.GetString("backColorPicker1.Items2189"),
            resources.GetString("backColorPicker1.Items2190"),
            resources.GetString("backColorPicker1.Items2191"),
            resources.GetString("backColorPicker1.Items2192"),
            resources.GetString("backColorPicker1.Items2193"),
            resources.GetString("backColorPicker1.Items2194"),
            resources.GetString("backColorPicker1.Items2195"),
            resources.GetString("backColorPicker1.Items2196"),
            resources.GetString("backColorPicker1.Items2197"),
            resources.GetString("backColorPicker1.Items2198"),
            resources.GetString("backColorPicker1.Items2199"),
            resources.GetString("backColorPicker1.Items2200"),
            resources.GetString("backColorPicker1.Items2201"),
            resources.GetString("backColorPicker1.Items2202"),
            resources.GetString("backColorPicker1.Items2203"),
            resources.GetString("backColorPicker1.Items2204"),
            resources.GetString("backColorPicker1.Items2205"),
            resources.GetString("backColorPicker1.Items2206"),
            resources.GetString("backColorPicker1.Items2207"),
            resources.GetString("backColorPicker1.Items2208"),
            resources.GetString("backColorPicker1.Items2209"),
            resources.GetString("backColorPicker1.Items2210"),
            resources.GetString("backColorPicker1.Items2211"),
            resources.GetString("backColorPicker1.Items2212"),
            resources.GetString("backColorPicker1.Items2213"),
            resources.GetString("backColorPicker1.Items2214"),
            resources.GetString("backColorPicker1.Items2215"),
            resources.GetString("backColorPicker1.Items2216"),
            resources.GetString("backColorPicker1.Items2217"),
            resources.GetString("backColorPicker1.Items2218"),
            resources.GetString("backColorPicker1.Items2219"),
            resources.GetString("backColorPicker1.Items2220"),
            resources.GetString("backColorPicker1.Items2221"),
            resources.GetString("backColorPicker1.Items2222"),
            resources.GetString("backColorPicker1.Items2223"),
            resources.GetString("backColorPicker1.Items2224"),
            resources.GetString("backColorPicker1.Items2225"),
            resources.GetString("backColorPicker1.Items2226"),
            resources.GetString("backColorPicker1.Items2227"),
            resources.GetString("backColorPicker1.Items2228"),
            resources.GetString("backColorPicker1.Items2229"),
            resources.GetString("backColorPicker1.Items2230"),
            resources.GetString("backColorPicker1.Items2231"),
            resources.GetString("backColorPicker1.Items2232"),
            resources.GetString("backColorPicker1.Items2233"),
            resources.GetString("backColorPicker1.Items2234"),
            resources.GetString("backColorPicker1.Items2235"),
            resources.GetString("backColorPicker1.Items2236"),
            resources.GetString("backColorPicker1.Items2237"),
            resources.GetString("backColorPicker1.Items2238"),
            resources.GetString("backColorPicker1.Items2239"),
            resources.GetString("backColorPicker1.Items2240"),
            resources.GetString("backColorPicker1.Items2241"),
            resources.GetString("backColorPicker1.Items2242"),
            resources.GetString("backColorPicker1.Items2243"),
            resources.GetString("backColorPicker1.Items2244"),
            resources.GetString("backColorPicker1.Items2245"),
            resources.GetString("backColorPicker1.Items2246"),
            resources.GetString("backColorPicker1.Items2247"),
            resources.GetString("backColorPicker1.Items2248"),
            resources.GetString("backColorPicker1.Items2249"),
            resources.GetString("backColorPicker1.Items2250"),
            resources.GetString("backColorPicker1.Items2251"),
            resources.GetString("backColorPicker1.Items2252"),
            resources.GetString("backColorPicker1.Items2253"),
            resources.GetString("backColorPicker1.Items2254"),
            resources.GetString("backColorPicker1.Items2255"),
            resources.GetString("backColorPicker1.Items2256"),
            resources.GetString("backColorPicker1.Items2257"),
            resources.GetString("backColorPicker1.Items2258"),
            resources.GetString("backColorPicker1.Items2259"),
            resources.GetString("backColorPicker1.Items2260"),
            resources.GetString("backColorPicker1.Items2261"),
            resources.GetString("backColorPicker1.Items2262"),
            resources.GetString("backColorPicker1.Items2263"),
            resources.GetString("backColorPicker1.Items2264"),
            resources.GetString("backColorPicker1.Items2265"),
            resources.GetString("backColorPicker1.Items2266"),
            resources.GetString("backColorPicker1.Items2267"),
            resources.GetString("backColorPicker1.Items2268"),
            resources.GetString("backColorPicker1.Items2269"),
            resources.GetString("backColorPicker1.Items2270"),
            resources.GetString("backColorPicker1.Items2271"),
            resources.GetString("backColorPicker1.Items2272"),
            resources.GetString("backColorPicker1.Items2273"),
            resources.GetString("backColorPicker1.Items2274"),
            resources.GetString("backColorPicker1.Items2275"),
            resources.GetString("backColorPicker1.Items2276"),
            resources.GetString("backColorPicker1.Items2277"),
            resources.GetString("backColorPicker1.Items2278"),
            resources.GetString("backColorPicker1.Items2279"),
            resources.GetString("backColorPicker1.Items2280"),
            resources.GetString("backColorPicker1.Items2281"),
            resources.GetString("backColorPicker1.Items2282"),
            resources.GetString("backColorPicker1.Items2283"),
            resources.GetString("backColorPicker1.Items2284"),
            resources.GetString("backColorPicker1.Items2285"),
            resources.GetString("backColorPicker1.Items2286"),
            resources.GetString("backColorPicker1.Items2287"),
            resources.GetString("backColorPicker1.Items2288"),
            resources.GetString("backColorPicker1.Items2289"),
            resources.GetString("backColorPicker1.Items2290"),
            resources.GetString("backColorPicker1.Items2291"),
            resources.GetString("backColorPicker1.Items2292"),
            resources.GetString("backColorPicker1.Items2293"),
            resources.GetString("backColorPicker1.Items2294"),
            resources.GetString("backColorPicker1.Items2295"),
            resources.GetString("backColorPicker1.Items2296"),
            resources.GetString("backColorPicker1.Items2297"),
            resources.GetString("backColorPicker1.Items2298"),
            resources.GetString("backColorPicker1.Items2299"),
            resources.GetString("backColorPicker1.Items2300"),
            resources.GetString("backColorPicker1.Items2301"),
            resources.GetString("backColorPicker1.Items2302"),
            resources.GetString("backColorPicker1.Items2303"),
            resources.GetString("backColorPicker1.Items2304"),
            resources.GetString("backColorPicker1.Items2305"),
            resources.GetString("backColorPicker1.Items2306"),
            resources.GetString("backColorPicker1.Items2307"),
            resources.GetString("backColorPicker1.Items2308"),
            resources.GetString("backColorPicker1.Items2309"),
            resources.GetString("backColorPicker1.Items2310"),
            resources.GetString("backColorPicker1.Items2311"),
            resources.GetString("backColorPicker1.Items2312"),
            resources.GetString("backColorPicker1.Items2313"),
            resources.GetString("backColorPicker1.Items2314"),
            resources.GetString("backColorPicker1.Items2315"),
            resources.GetString("backColorPicker1.Items2316"),
            resources.GetString("backColorPicker1.Items2317"),
            resources.GetString("backColorPicker1.Items2318"),
            resources.GetString("backColorPicker1.Items2319"),
            resources.GetString("backColorPicker1.Items2320"),
            resources.GetString("backColorPicker1.Items2321"),
            resources.GetString("backColorPicker1.Items2322"),
            resources.GetString("backColorPicker1.Items2323"),
            resources.GetString("backColorPicker1.Items2324"),
            resources.GetString("backColorPicker1.Items2325"),
            resources.GetString("backColorPicker1.Items2326"),
            resources.GetString("backColorPicker1.Items2327"),
            resources.GetString("backColorPicker1.Items2328"),
            resources.GetString("backColorPicker1.Items2329"),
            resources.GetString("backColorPicker1.Items2330"),
            resources.GetString("backColorPicker1.Items2331"),
            resources.GetString("backColorPicker1.Items2332"),
            resources.GetString("backColorPicker1.Items2333"),
            resources.GetString("backColorPicker1.Items2334"),
            resources.GetString("backColorPicker1.Items2335"),
            resources.GetString("backColorPicker1.Items2336"),
            resources.GetString("backColorPicker1.Items2337"),
            resources.GetString("backColorPicker1.Items2338"),
            resources.GetString("backColorPicker1.Items2339"),
            resources.GetString("backColorPicker1.Items2340"),
            resources.GetString("backColorPicker1.Items2341"),
            resources.GetString("backColorPicker1.Items2342"),
            resources.GetString("backColorPicker1.Items2343"),
            resources.GetString("backColorPicker1.Items2344"),
            resources.GetString("backColorPicker1.Items2345"),
            resources.GetString("backColorPicker1.Items2346"),
            resources.GetString("backColorPicker1.Items2347"),
            resources.GetString("backColorPicker1.Items2348"),
            resources.GetString("backColorPicker1.Items2349"),
            resources.GetString("backColorPicker1.Items2350"),
            resources.GetString("backColorPicker1.Items2351"),
            resources.GetString("backColorPicker1.Items2352"),
            resources.GetString("backColorPicker1.Items2353"),
            resources.GetString("backColorPicker1.Items2354"),
            resources.GetString("backColorPicker1.Items2355"),
            resources.GetString("backColorPicker1.Items2356"),
            resources.GetString("backColorPicker1.Items2357"),
            resources.GetString("backColorPicker1.Items2358"),
            resources.GetString("backColorPicker1.Items2359"),
            resources.GetString("backColorPicker1.Items2360"),
            resources.GetString("backColorPicker1.Items2361"),
            resources.GetString("backColorPicker1.Items2362"),
            resources.GetString("backColorPicker1.Items2363"),
            resources.GetString("backColorPicker1.Items2364"),
            resources.GetString("backColorPicker1.Items2365"),
            resources.GetString("backColorPicker1.Items2366"),
            resources.GetString("backColorPicker1.Items2367"),
            resources.GetString("backColorPicker1.Items2368"),
            resources.GetString("backColorPicker1.Items2369"),
            resources.GetString("backColorPicker1.Items2370"),
            resources.GetString("backColorPicker1.Items2371"),
            resources.GetString("backColorPicker1.Items2372"),
            resources.GetString("backColorPicker1.Items2373"),
            resources.GetString("backColorPicker1.Items2374"),
            resources.GetString("backColorPicker1.Items2375"),
            resources.GetString("backColorPicker1.Items2376"),
            resources.GetString("backColorPicker1.Items2377"),
            resources.GetString("backColorPicker1.Items2378"),
            resources.GetString("backColorPicker1.Items2379"),
            resources.GetString("backColorPicker1.Items2380"),
            resources.GetString("backColorPicker1.Items2381"),
            resources.GetString("backColorPicker1.Items2382"),
            resources.GetString("backColorPicker1.Items2383"),
            resources.GetString("backColorPicker1.Items2384"),
            resources.GetString("backColorPicker1.Items2385"),
            resources.GetString("backColorPicker1.Items2386"),
            resources.GetString("backColorPicker1.Items2387"),
            resources.GetString("backColorPicker1.Items2388"),
            resources.GetString("backColorPicker1.Items2389"),
            resources.GetString("backColorPicker1.Items2390"),
            resources.GetString("backColorPicker1.Items2391"),
            resources.GetString("backColorPicker1.Items2392"),
            resources.GetString("backColorPicker1.Items2393"),
            resources.GetString("backColorPicker1.Items2394"),
            resources.GetString("backColorPicker1.Items2395"),
            resources.GetString("backColorPicker1.Items2396"),
            resources.GetString("backColorPicker1.Items2397"),
            resources.GetString("backColorPicker1.Items2398"),
            resources.GetString("backColorPicker1.Items2399"),
            resources.GetString("backColorPicker1.Items2400"),
            resources.GetString("backColorPicker1.Items2401"),
            resources.GetString("backColorPicker1.Items2402"),
            resources.GetString("backColorPicker1.Items2403"),
            resources.GetString("backColorPicker1.Items2404"),
            resources.GetString("backColorPicker1.Items2405"),
            resources.GetString("backColorPicker1.Items2406"),
            resources.GetString("backColorPicker1.Items2407"),
            resources.GetString("backColorPicker1.Items2408"),
            resources.GetString("backColorPicker1.Items2409"),
            resources.GetString("backColorPicker1.Items2410"),
            resources.GetString("backColorPicker1.Items2411"),
            resources.GetString("backColorPicker1.Items2412"),
            resources.GetString("backColorPicker1.Items2413"),
            resources.GetString("backColorPicker1.Items2414"),
            resources.GetString("backColorPicker1.Items2415"),
            resources.GetString("backColorPicker1.Items2416"),
            resources.GetString("backColorPicker1.Items2417"),
            resources.GetString("backColorPicker1.Items2418"),
            resources.GetString("backColorPicker1.Items2419"),
            resources.GetString("backColorPicker1.Items2420"),
            resources.GetString("backColorPicker1.Items2421"),
            resources.GetString("backColorPicker1.Items2422"),
            resources.GetString("backColorPicker1.Items2423"),
            resources.GetString("backColorPicker1.Items2424"),
            resources.GetString("backColorPicker1.Items2425"),
            resources.GetString("backColorPicker1.Items2426"),
            resources.GetString("backColorPicker1.Items2427"),
            resources.GetString("backColorPicker1.Items2428"),
            resources.GetString("backColorPicker1.Items2429"),
            resources.GetString("backColorPicker1.Items2430"),
            resources.GetString("backColorPicker1.Items2431"),
            resources.GetString("backColorPicker1.Items2432"),
            resources.GetString("backColorPicker1.Items2433"),
            resources.GetString("backColorPicker1.Items2434"),
            resources.GetString("backColorPicker1.Items2435"),
            resources.GetString("backColorPicker1.Items2436"),
            resources.GetString("backColorPicker1.Items2437"),
            resources.GetString("backColorPicker1.Items2438"),
            resources.GetString("backColorPicker1.Items2439"),
            resources.GetString("backColorPicker1.Items2440"),
            resources.GetString("backColorPicker1.Items2441"),
            resources.GetString("backColorPicker1.Items2442"),
            resources.GetString("backColorPicker1.Items2443"),
            resources.GetString("backColorPicker1.Items2444"),
            resources.GetString("backColorPicker1.Items2445"),
            resources.GetString("backColorPicker1.Items2446"),
            resources.GetString("backColorPicker1.Items2447"),
            resources.GetString("backColorPicker1.Items2448"),
            resources.GetString("backColorPicker1.Items2449"),
            resources.GetString("backColorPicker1.Items2450"),
            resources.GetString("backColorPicker1.Items2451"),
            resources.GetString("backColorPicker1.Items2452"),
            resources.GetString("backColorPicker1.Items2453"),
            resources.GetString("backColorPicker1.Items2454"),
            resources.GetString("backColorPicker1.Items2455"),
            resources.GetString("backColorPicker1.Items2456"),
            resources.GetString("backColorPicker1.Items2457"),
            resources.GetString("backColorPicker1.Items2458"),
            resources.GetString("backColorPicker1.Items2459"),
            resources.GetString("backColorPicker1.Items2460"),
            resources.GetString("backColorPicker1.Items2461"),
            resources.GetString("backColorPicker1.Items2462"),
            resources.GetString("backColorPicker1.Items2463"),
            resources.GetString("backColorPicker1.Items2464"),
            resources.GetString("backColorPicker1.Items2465"),
            resources.GetString("backColorPicker1.Items2466"),
            resources.GetString("backColorPicker1.Items2467"),
            resources.GetString("backColorPicker1.Items2468"),
            resources.GetString("backColorPicker1.Items2469"),
            resources.GetString("backColorPicker1.Items2470"),
            resources.GetString("backColorPicker1.Items2471"),
            resources.GetString("backColorPicker1.Items2472"),
            resources.GetString("backColorPicker1.Items2473"),
            resources.GetString("backColorPicker1.Items2474"),
            resources.GetString("backColorPicker1.Items2475"),
            resources.GetString("backColorPicker1.Items2476"),
            resources.GetString("backColorPicker1.Items2477"),
            resources.GetString("backColorPicker1.Items2478"),
            resources.GetString("backColorPicker1.Items2479"),
            resources.GetString("backColorPicker1.Items2480"),
            resources.GetString("backColorPicker1.Items2481"),
            resources.GetString("backColorPicker1.Items2482"),
            resources.GetString("backColorPicker1.Items2483"),
            resources.GetString("backColorPicker1.Items2484"),
            resources.GetString("backColorPicker1.Items2485"),
            resources.GetString("backColorPicker1.Items2486"),
            resources.GetString("backColorPicker1.Items2487"),
            resources.GetString("backColorPicker1.Items2488"),
            resources.GetString("backColorPicker1.Items2489"),
            resources.GetString("backColorPicker1.Items2490"),
            resources.GetString("backColorPicker1.Items2491"),
            resources.GetString("backColorPicker1.Items2492"),
            resources.GetString("backColorPicker1.Items2493"),
            resources.GetString("backColorPicker1.Items2494"),
            resources.GetString("backColorPicker1.Items2495"),
            resources.GetString("backColorPicker1.Items2496"),
            resources.GetString("backColorPicker1.Items2497"),
            resources.GetString("backColorPicker1.Items2498"),
            resources.GetString("backColorPicker1.Items2499"),
            resources.GetString("backColorPicker1.Items2500"),
            resources.GetString("backColorPicker1.Items2501"),
            resources.GetString("backColorPicker1.Items2502"),
            resources.GetString("backColorPicker1.Items2503"),
            resources.GetString("backColorPicker1.Items2504"),
            resources.GetString("backColorPicker1.Items2505"),
            resources.GetString("backColorPicker1.Items2506"),
            resources.GetString("backColorPicker1.Items2507"),
            resources.GetString("backColorPicker1.Items2508"),
            resources.GetString("backColorPicker1.Items2509"),
            resources.GetString("backColorPicker1.Items2510"),
            resources.GetString("backColorPicker1.Items2511"),
            resources.GetString("backColorPicker1.Items2512"),
            resources.GetString("backColorPicker1.Items2513"),
            resources.GetString("backColorPicker1.Items2514"),
            resources.GetString("backColorPicker1.Items2515"),
            resources.GetString("backColorPicker1.Items2516"),
            resources.GetString("backColorPicker1.Items2517"),
            resources.GetString("backColorPicker1.Items2518"),
            resources.GetString("backColorPicker1.Items2519")});
			this.backColorPicker1.Name = "backColorPicker1";
			this.backColorPicker1.Tag = "Back Color";
			this.backColorPicker1.SelectedValueChanged += this.ctlBackColor_Change;
			this.backColorPicker1.Validated += this.ctlBackColor_Change;
			// 
			// statusStrip1
			// 
			this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.statusStrip1.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.statusSelected,
            this.toolStripStatusLabel2,
            this.statusPosition});
			resources.ApplyResources(this.statusStrip1, "statusStrip1");
			this.statusStrip1.Name = "statusStrip1";
			// 
			// statusSelected
			// 
			this.statusSelected.Name = "statusSelected";
			resources.ApplyResources(this.statusSelected, "statusSelected");
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.ForeColor = System.Drawing.Color.Gray;
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			resources.ApplyResources(this.toolStripStatusLabel2, "toolStripStatusLabel2");
			// 
			// statusPosition
			// 
			this.statusPosition.Name = "statusPosition";
			resources.ApplyResources(this.statusPosition, "statusPosition");
			// 
			// mainSP
			// 
			resources.ApplyResources(this.mainSP, "mainSP");
			this.mainSP.Name = "mainSP";
			this.mainSP.TabStop = false;
			// 
			// ContextMenuTB
			// 
			this.ContextMenuTB.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.ContextMenuTB.Items.AddRange(new Majorsilence.Forms.ToolStripItem[] {
            this.MenuTBClose,
            this.MenuTBSave,
            this.MenuTBCloseAllButThis});
			this.ContextMenuTB.Name = "ContextMenuTB";
			resources.ApplyResources(this.ContextMenuTB, "ContextMenuTB");
			// 
			// MenuTBClose
			// 
			this.MenuTBClose.Name = "MenuTBClose";
			resources.ApplyResources(this.MenuTBClose, "MenuTBClose");
			this.MenuTBClose.Click += this.menuFileClose_Click;
			// 
			// MenuTBSave
			// 
			this.MenuTBSave.Name = "MenuTBSave";
			resources.ApplyResources(this.MenuTBSave, "MenuTBSave");
			this.MenuTBSave.Click += this.menuFileSave_Click;
			// 
			// MenuTBCloseAllButThis
			// 
			this.MenuTBCloseAllButThis.Name = "MenuTBCloseAllButThis";
			resources.ApplyResources(this.MenuTBCloseAllButThis, "MenuTBCloseAllButThis");
			this.MenuTBCloseAllButThis.Click += this.menuWndCloseAllButCurrent_Click;
			// 
			// mainProperties
			// 
			resources.ApplyResources(this.mainProperties, "mainProperties");
			this.mainProperties.Name = "mainProperties";
			// 
			// RdlDesigner
			// 
			this.AllowDrop = true;
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.mainSP);
			this.Controls.Add(this.mainProperties);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.mainTB);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "RdlDesigner";
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			this.WindowState = Majorsilence.Forms.FormWindowState.Maximized;
			this.Load += this.RdlDesigner_Load;
			this.DragDrop += this.RdlDesigner_DragDrop;
			this.DragEnter += this.RdlDesigner_DragEnter;
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.mainTB.ResumeLayout(false);
			this.mainTB.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ContextMenuTB.ResumeLayout(false);
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
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem dataToolStripMenuItem;
        private ToolStripMenuItem formatToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem newReportToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem closeToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem printToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem pDFToolStripMenuItem;
        private ToolStripMenuItem tIFFToolStripMenuItem;
        private ToolStripMenuItem cSVToolStripMenuItem;
        private ToolStripMenuItem excelToolStripMenuItem;
        private ToolStripMenuItem rTFDOCToolStripMenuItem;
        private ToolStripMenuItem xMLToolStripMenuItem;
        private ToolStripMenuItem webPageHTMLToolStripMenuItem;
        private ToolStripMenuItem webArchiveSingleFileMHTToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem recentFilesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem selectAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem findNextToolStripMenuItem;
        private ToolStripMenuItem replaceToolStripMenuItem;
        private ToolStripMenuItem goToToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripMenuItem formatXMLToolStripMenuItem;
        private ToolStripMenuItem designerToolStripMenuItem;
        private ToolStripMenuItem rDLTextToolStripMenuItem;
        private ToolStripMenuItem previewToolStripMenuItem;
        private ToolStripMenuItem showReportInBrowserToolStripMenuItem;
        private ToolStripMenuItem propertiesWindowsToolStripMenuItem;
        private ToolStripMenuItem dataSetsToolStripMenuItem;
        private ToolStripMenuItem embeddedImagesToolStripMenuItem;
        private ToolStripMenuItem createSharedDataSourceToolStripMenuItem;
        private ToolStripMenuItem dataSourcesToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator8;
        private ToolStripSeparator toolStripSeparator9;
        private ToolStripMenuItem alignToolStripMenuItem;
        private ToolStripMenuItem leftsToolStripMenuItem;
        private ToolStripMenuItem centersToolStripMenuItem;
        private ToolStripMenuItem rightsToolStripMenuItem;
        private ToolStripMenuItem topsToolStripMenuItem;
        private ToolStripMenuItem middlesToolStripMenuItem;
        private ToolStripMenuItem bottomsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator10;
        private ToolStripMenuItem sizeToolStripMenuItem;
        private ToolStripMenuItem widthToolStripMenuItem;
        private ToolStripMenuItem heightToolStripMenuItem;
        private ToolStripMenuItem bothToolStripMenuItem;
        private ToolStripMenuItem horizontalSpacingToolStripMenuItem;
        private ToolStripMenuItem makeEqualToolStripMenuItem;
        private ToolStripMenuItem increaseToolStripMenuItem;
        private ToolStripMenuItem decreaseToolStripMenuItem;
        private ToolStripMenuItem zeroToolStripMenuItem;
        private ToolStripMenuItem verticalSpacingToolStripMenuItem;
        private ToolStripMenuItem makeEqualToolStripMenuItem1;
        private ToolStripMenuItem increaseToolStripMenuItem1;
        private ToolStripMenuItem decreaseToolStripMenuItem1;
        private ToolStripMenuItem zeroToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator11;
        private ToolStripMenuItem paddingLeftToolStripMenuItem;
        private ToolStripMenuItem increaseToolStripMenuItem2;
        private ToolStripMenuItem decreaseToolStripMenuItem2;
        private ToolStripMenuItem zeroToolStripMenuItem2;
        private ToolStripMenuItem paddingRightToolStripMenuItem;
        private ToolStripMenuItem increaseToolStripMenuItem3;
        private ToolStripMenuItem decreaseToolStripMenuItem3;
        private ToolStripMenuItem zeroToolStripMenuItem3;
        private ToolStripMenuItem paddintTopToolStripMenuItem;
        private ToolStripMenuItem increaseToolStripMenuItem4;
        private ToolStripMenuItem decreaseToolStripMenuItem4;
        private ToolStripMenuItem zeroToolStripMenuItem4;
        private ToolStripMenuItem paddingBottomToolStripMenuItem;
        private ToolStripMenuItem increaseToolStripMenuItem5;
        private ToolStripMenuItem decreaseToolStripMenuItem5;
        private ToolStripMenuItem zeroToolStripMenuItem5;
        private ToolStripMenuItem validateRDLToolStripMenuItem;
        private ToolStripMenuItem startDesktopServerToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator12;
        private ToolStripSeparator toolStripSeparator13;
        private ToolStripMenuItem cascadeToolStripMenuItem;
        private ToolStripMenuItem tileToolStripMenuItem;
        private ToolStripMenuItem horizontalToolStripMenuItem;
        private ToolStripMenuItem verticallyToolStripMenuItem;
        private ToolStripMenuItem closeAllToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripMenuItem supportToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStrip mainTB;
        private ToolStripButton newToolStripButton1;
        private ToolStrip toolStrip1;
        private ToolStripButton openToolStripButton1;
        private ToolStripButton saveToolStripButton1;
        private ToolStripButton cutToolStripButton1;
        private ToolStripButton copyToolStripButton1;
        private ToolStripButton pasteToolStripButton1;
        private ToolStripButton undoToolStripButton1;
        private ToolStripButton textboxToolStripButton1;
        private ToolStripButton chartToolStripButton1;
        private ToolStripButton tableToolStripButton1;
        private ToolStripButton listToolStripButton1;
        private ToolStripButton imageToolStripButton1;
        private ToolStripButton matrixToolStripButton1;
        private ToolStripButton subreportToolStripButton1;
        private ToolStripButton rectangleToolStripButton1;
        private ToolStripButton lineToolStripButton1;
        private ToolStripLabel fxToolStripLabel1;
        private ToolStripTextBox ctlEditTextbox;
        private ToolStripButton boldToolStripButton1;
        private ToolStripButton italiacToolStripButton1;
        private ToolStripButton underlineToolStripButton2;
        private ToolStripButton leftAlignToolStripButton2;
        private ToolStripButton centerAlignToolStripButton2;
        private ToolStripButton rightAlignToolStripButton3;
        private ToolStripComboBox fontToolStripComboBox1;
        private ToolStripComboBox fontSizeToolStripComboBox1;
        private ColorPicker foreColorPicker1;
        private ColorPicker backColorPicker1;
        private ToolStripButton printToolStripButton2;
        private ToolStripComboBox zoomToolStripComboBox1;
        private ToolStripButton selectToolStripButton2;
        private TabControl mainTC;
        private ToolStripButton pdfToolStripButton2;
        private ToolStripButton htmlToolStripButton2;
        private ToolStripButton excelToolStripButton2;
        private ToolStripButton XmlToolStripButton2;
        private ToolStripButton MhtToolStripButton2;
        private ToolStripButton CsvToolStripButton2;
        private ToolStripButton RtfToolStripButton2;
        private ToolStripButton TifToolStripButton2;
        private Label label1;
        private Label label2;
        private Panel panel1;
        private StatusStrip statusStrip1;
        private Splitter mainSP;
        private PropertyCtl mainProperties;
        private ToolStripStatusLabel statusSelected;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel statusPosition;
        private ToolStripMenuItem insertToolStripMenuItem;
        private ToolStripMenuItem chartToolStripMenuItem;
        private ToolStripMenuItem gridToolStripMenuItem;
        private ToolStripMenuItem imageToolStripMenuItem;
        private ToolStripMenuItem lineToolStripMenuItem;
        private ToolStripMenuItem listToolStripMenuItem;
        private ToolStripMenuItem matrixToolStripMenuItem;
        private ToolStripMenuItem rectangleToolStripMenuItem;
        private ToolStripMenuItem subReportToolStripMenuItem;
        private ToolStripMenuItem tableToolStripMenuItem;
		private ToolStripMenuItem textboxToolStripMenuItem;
        private ToolStripMenuItem pDFOldStyleToolStripMenuItem;
        private ToolStripMenuItem dOCToolStripMenuItem;
		private ContextMenuStrip ContextMenuTB;
		private IContainer components;
		private ToolStripMenuItem MenuTBClose;
		private ToolStripMenuItem MenuTBSave;
		private ToolStripMenuItem MenuTBCloseAllButThis;
        private ToolStripMenuItem Excel2007ToolStripMenuItem;
        private ToolStripButton AlignmentGridEnable;
        private ToolStripSeparator toolStripSeparator14;
        private ToolStripUserZoomControl zoomControl;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem centerInContainerToolStripMenuItem;
        private ToolStripMenuItem centerHorizontallyToolStripMenuItem;
        private ToolStripMenuItem centerVerticallyToolStripMenuItem;
		private ToolStripMenuItem centerBothToolStripMenuItem;
	}
}
