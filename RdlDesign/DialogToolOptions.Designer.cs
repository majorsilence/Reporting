using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogToolOptions : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		RdlDesigner _RdlDesigner;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.TabControl tabControl1;
private Majorsilence.Forms.TabPage tpGeneral;
private Majorsilence.Forms.TabPage tpToolbar;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.TextBox tbRecentFilesMax;
private Majorsilence.Forms.Label label2;
private Majorsilence.Forms.Label label3;
private Majorsilence.Forms.TextBox tbHelpUrl;
private Majorsilence.Forms.ListBox lbOperation;
private Majorsilence.Forms.ListBox lbToolbar;
private Majorsilence.Forms.Label label4;
private Majorsilence.Forms.Label label5;
private Majorsilence.Forms.Button bCopyItem;
private Majorsilence.Forms.Button bUp;
private Majorsilence.Forms.Button bDown;
private Majorsilence.Forms.Button bReset;
private Majorsilence.Forms.Button bRemove;
private Majorsilence.Forms.Button bApply;
private Majorsilence.Forms.TabPage tpDesktop;
private Majorsilence.Forms.Label label6;
private Majorsilence.Forms.TextBox tbPort;
private Majorsilence.Forms.Label label7;
private Majorsilence.Forms.Label label8;
private Majorsilence.Forms.Label label9;
private Majorsilence.Forms.TextBox tbDirectory;
private Majorsilence.Forms.CheckBox ckLocal;
private Majorsilence.Forms.Button bBrowse;
private CheckBox cbEditLines;
private CheckBox cbOutline;
private CheckBox cbTabInterface;
private GroupBox gbPropLoc;
private RadioButton rbPBBottom;
private RadioButton rbPBTop;
private RadioButton rbPBLeft;
private RadioButton rbPBRight;
private CheckBox chkPBAutoHide;
private TabPage tabPage1;
private Button bRemoveMap;
private Button bAddMap;
private ListBox lbMaps;
private Label label10;
private CheckBox cbShowReportWaitDialog;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogToolOptions));
            this.bOK = new Majorsilence.Forms.Button();
            this.bCancel = new Majorsilence.Forms.Button();
            this.tabControl1 = new Majorsilence.Forms.TabControl();
            this.tpGeneral = new Majorsilence.Forms.TabPage();
            this.groupBox1 = new Majorsilence.Forms.GroupBox();
            this.radioButtonCm = new Majorsilence.Forms.RadioButton();
            this.radioButtonInches = new Majorsilence.Forms.RadioButton();
            this.label12 = new Majorsilence.Forms.Label();
            this.comboXmlEndingLine = new Majorsilence.Forms.ComboBox();
            this.label11 = new Majorsilence.Forms.Label();
            this.tbLanguage = new Majorsilence.Forms.ComboBox();
            this.cbShowReportWaitDialog = new Majorsilence.Forms.CheckBox();
            this.gbPropLoc = new Majorsilence.Forms.GroupBox();
            this.chkPBAutoHide = new Majorsilence.Forms.CheckBox();
            this.rbPBBottom = new Majorsilence.Forms.RadioButton();
            this.rbPBTop = new Majorsilence.Forms.RadioButton();
            this.rbPBLeft = new Majorsilence.Forms.RadioButton();
            this.rbPBRight = new Majorsilence.Forms.RadioButton();
            this.cbTabInterface = new Majorsilence.Forms.CheckBox();
            this.cbOutline = new Majorsilence.Forms.CheckBox();
            this.cbEditLines = new Majorsilence.Forms.CheckBox();
            this.tbHelpUrl = new Majorsilence.Forms.TextBox();
            this.label3 = new Majorsilence.Forms.Label();
            this.label2 = new Majorsilence.Forms.Label();
            this.tbRecentFilesMax = new Majorsilence.Forms.TextBox();
            this.label1 = new Majorsilence.Forms.Label();
            this.tpToolbar = new Majorsilence.Forms.TabPage();
            this.bRemove = new Majorsilence.Forms.Button();
            this.bReset = new Majorsilence.Forms.Button();
            this.bDown = new Majorsilence.Forms.Button();
            this.bUp = new Majorsilence.Forms.Button();
            this.bCopyItem = new Majorsilence.Forms.Button();
            this.label5 = new Majorsilence.Forms.Label();
            this.label4 = new Majorsilence.Forms.Label();
            this.lbToolbar = new Majorsilence.Forms.ListBox();
            this.lbOperation = new Majorsilence.Forms.ListBox();
            this.tpDesktop = new Majorsilence.Forms.TabPage();
            this.bBrowse = new Majorsilence.Forms.Button();
            this.tbDirectory = new Majorsilence.Forms.TextBox();
            this.label9 = new Majorsilence.Forms.Label();
            this.label8 = new Majorsilence.Forms.Label();
            this.label7 = new Majorsilence.Forms.Label();
            this.ckLocal = new Majorsilence.Forms.CheckBox();
            this.tbPort = new Majorsilence.Forms.TextBox();
            this.label6 = new Majorsilence.Forms.Label();
            this.tabPage1 = new Majorsilence.Forms.TabPage();
            this.label10 = new Majorsilence.Forms.Label();
            this.bRemoveMap = new Majorsilence.Forms.Button();
            this.bAddMap = new Majorsilence.Forms.Button();
            this.lbMaps = new Majorsilence.Forms.ListBox();
            this.bApply = new Majorsilence.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tpGeneral.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.gbPropLoc.SuspendLayout();
            this.tpToolbar.SuspendLayout();
            this.tpDesktop.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bOK
            // 
            resources.ApplyResources(this.bOK, "bOK");
            this.bOK.Name = "bOK";
            this.bOK.Click += this.bOK_Click;
            // 
            // bCancel
            // 
            this.bCancel.CausesValidation = false;
            this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.bCancel, "bCancel");
            this.bCancel.Name = "bCancel";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tpGeneral);
            this.tabControl1.Controls.Add(this.tpToolbar);
            this.tabControl1.Controls.Add(this.tpDesktop);
            this.tabControl1.Controls.Add(this.tabPage1);
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            // 
            // tpGeneral
            // 
            this.tpGeneral.Controls.Add(this.groupBox1);
            this.tpGeneral.Controls.Add(this.label12);
            this.tpGeneral.Controls.Add(this.comboXmlEndingLine);
            this.tpGeneral.Controls.Add(this.label11);
            this.tpGeneral.Controls.Add(this.tbLanguage);
            this.tpGeneral.Controls.Add(this.cbShowReportWaitDialog);
            this.tpGeneral.Controls.Add(this.gbPropLoc);
            this.tpGeneral.Controls.Add(this.cbTabInterface);
            this.tpGeneral.Controls.Add(this.cbOutline);
            this.tpGeneral.Controls.Add(this.cbEditLines);
            this.tpGeneral.Controls.Add(this.tbHelpUrl);
            this.tpGeneral.Controls.Add(this.label3);
            this.tpGeneral.Controls.Add(this.label2);
            this.tpGeneral.Controls.Add(this.tbRecentFilesMax);
            this.tpGeneral.Controls.Add(this.label1);
            resources.ApplyResources(this.tpGeneral, "tpGeneral");
            this.tpGeneral.Name = "tpGeneral";
            this.tpGeneral.Tag = "general";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButtonCm);
            this.groupBox1.Controls.Add(this.radioButtonInches);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // radioButtonCm
            // 
            resources.ApplyResources(this.radioButtonCm, "radioButtonCm");
            this.radioButtonCm.Name = "radioButtonCm";
            this.radioButtonCm.UseVisualStyleBackColor = true;
            this.radioButtonCm.CheckedChanged += this.RadioButtonCm_CheckedChanged;
            // 
            // radioButtonInches
            // 
            resources.ApplyResources(this.radioButtonInches, "radioButtonInches");
            this.radioButtonInches.Checked = true;
            this.radioButtonInches.Name = "radioButtonInches";
            this.radioButtonInches.TabStop = true;
            this.radioButtonInches.UseVisualStyleBackColor = true;
            this.radioButtonInches.CheckedChanged += this.RadioButtonInches_CheckedChanged;
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            // 
            // comboXmlEndingLine
            // 
            this.comboXmlEndingLine.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.comboXmlEndingLine.FormattingEnabled = true;
            resources.ApplyResources(this.comboXmlEndingLine, "comboXmlEndingLine");
            this.comboXmlEndingLine.Name = "comboXmlEndingLine";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            // 
            // tbLanguage
            // 
            this.tbLanguage.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.tbLanguage.FormattingEnabled = true;
            resources.ApplyResources(this.tbLanguage, "tbLanguage");
            this.tbLanguage.Name = "tbLanguage";
            this.tbLanguage.SelectedIndexChanged += this.Desktop_Changed;
            // 
            // cbShowReportWaitDialog
            // 
            resources.ApplyResources(this.cbShowReportWaitDialog, "cbShowReportWaitDialog");
            this.cbShowReportWaitDialog.Name = "cbShowReportWaitDialog";
            this.cbShowReportWaitDialog.UseVisualStyleBackColor = true;
            // 
            // gbPropLoc
            // 
            this.gbPropLoc.Controls.Add(this.chkPBAutoHide);
            this.gbPropLoc.Controls.Add(this.rbPBBottom);
            this.gbPropLoc.Controls.Add(this.rbPBTop);
            this.gbPropLoc.Controls.Add(this.rbPBLeft);
            this.gbPropLoc.Controls.Add(this.rbPBRight);
            resources.ApplyResources(this.gbPropLoc, "gbPropLoc");
            this.gbPropLoc.Name = "gbPropLoc";
            this.gbPropLoc.TabStop = false;
            // 
            // chkPBAutoHide
            // 
            resources.ApplyResources(this.chkPBAutoHide, "chkPBAutoHide");
            this.chkPBAutoHide.Name = "chkPBAutoHide";
            this.chkPBAutoHide.UseVisualStyleBackColor = true;
            // 
            // rbPBBottom
            // 
            resources.ApplyResources(this.rbPBBottom, "rbPBBottom");
            this.rbPBBottom.Name = "rbPBBottom";
            this.rbPBBottom.TabStop = true;
            this.rbPBBottom.UseVisualStyleBackColor = true;
            // 
            // rbPBTop
            // 
            resources.ApplyResources(this.rbPBTop, "rbPBTop");
            this.rbPBTop.Name = "rbPBTop";
            this.rbPBTop.TabStop = true;
            this.rbPBTop.UseVisualStyleBackColor = true;
            // 
            // rbPBLeft
            // 
            resources.ApplyResources(this.rbPBLeft, "rbPBLeft");
            this.rbPBLeft.Name = "rbPBLeft";
            this.rbPBLeft.TabStop = true;
            this.rbPBLeft.UseVisualStyleBackColor = true;
            // 
            // rbPBRight
            // 
            resources.ApplyResources(this.rbPBRight, "rbPBRight");
            this.rbPBRight.Name = "rbPBRight";
            this.rbPBRight.TabStop = true;
            this.rbPBRight.UseVisualStyleBackColor = true;
            // 
            // cbTabInterface
            // 
            resources.ApplyResources(this.cbTabInterface, "cbTabInterface");
            this.cbTabInterface.Name = "cbTabInterface";
            this.cbTabInterface.UseVisualStyleBackColor = true;
            this.cbTabInterface.CheckedChanged += this.cbTabInterface_CheckedChanged;
            // 
            // cbOutline
            // 
            resources.ApplyResources(this.cbOutline, "cbOutline");
            this.cbOutline.Name = "cbOutline";
            this.cbOutline.UseVisualStyleBackColor = true;
            // 
            // cbEditLines
            // 
            resources.ApplyResources(this.cbEditLines, "cbEditLines");
            this.cbEditLines.Name = "cbEditLines";
            this.cbEditLines.UseVisualStyleBackColor = true;
            // 
            // tbHelpUrl
            // 
            resources.ApplyResources(this.tbHelpUrl, "tbHelpUrl");
            this.tbHelpUrl.Name = "tbHelpUrl";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // tbRecentFilesMax
            // 
            resources.ApplyResources(this.tbRecentFilesMax, "tbRecentFilesMax");
            this.tbRecentFilesMax.Name = "tbRecentFilesMax";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tpToolbar
            // 
            this.tpToolbar.Controls.Add(this.bRemove);
            this.tpToolbar.Controls.Add(this.bReset);
            this.tpToolbar.Controls.Add(this.bDown);
            this.tpToolbar.Controls.Add(this.bUp);
            this.tpToolbar.Controls.Add(this.bCopyItem);
            this.tpToolbar.Controls.Add(this.label5);
            this.tpToolbar.Controls.Add(this.label4);
            this.tpToolbar.Controls.Add(this.lbToolbar);
            this.tpToolbar.Controls.Add(this.lbOperation);
            resources.ApplyResources(this.tpToolbar, "tpToolbar");
            this.tpToolbar.Name = "tpToolbar";
            this.tpToolbar.Tag = "toolbar";
            // 
            // bRemove
            // 
            resources.ApplyResources(this.bRemove, "bRemove");
            this.bRemove.Name = "bRemove";
            this.bRemove.Click += this.bRemove_Click;
            // 
            // bReset
            // 
            resources.ApplyResources(this.bReset, "bReset");
            this.bReset.Name = "bReset";
            this.bReset.Click += this.bReset_Click;
            // 
            // bDown
            // 
            resources.ApplyResources(this.bDown, "bDown");
            this.bDown.Name = "bDown";
            this.bDown.Click += this.bDown_Click;
            // 
            // bUp
            // 
            resources.ApplyResources(this.bUp, "bUp");
            this.bUp.Name = "bUp";
            this.bUp.Click += this.bUp_Click;
            // 
            // bCopyItem
            // 
            resources.ApplyResources(this.bCopyItem, "bCopyItem");
            this.bCopyItem.Name = "bCopyItem";
            this.bCopyItem.Click += this.bCopyItem_Click;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // lbToolbar
            // 
            resources.ApplyResources(this.lbToolbar, "lbToolbar");
            this.lbToolbar.Name = "lbToolbar";
            // 
            // lbOperation
            // 
            resources.ApplyResources(this.lbOperation, "lbOperation");
            this.lbOperation.Name = "lbOperation";
            // 
            // tpDesktop
            // 
            this.tpDesktop.Controls.Add(this.bBrowse);
            this.tpDesktop.Controls.Add(this.tbDirectory);
            this.tpDesktop.Controls.Add(this.label9);
            this.tpDesktop.Controls.Add(this.label8);
            this.tpDesktop.Controls.Add(this.label7);
            this.tpDesktop.Controls.Add(this.ckLocal);
            this.tpDesktop.Controls.Add(this.tbPort);
            this.tpDesktop.Controls.Add(this.label6);
            resources.ApplyResources(this.tpDesktop, "tpDesktop");
            this.tpDesktop.Name = "tpDesktop";
            this.tpDesktop.Tag = "desktop";
            // 
            // bBrowse
            // 
            resources.ApplyResources(this.bBrowse, "bBrowse");
            this.bBrowse.Name = "bBrowse";
            this.bBrowse.Click += this.bBrowse_Click;
            // 
            // tbDirectory
            // 
            resources.ApplyResources(this.tbDirectory, "tbDirectory");
            this.tbDirectory.Name = "tbDirectory";
            this.tbDirectory.TextChanged += this.Desktop_Changed;
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // ckLocal
            // 
            resources.ApplyResources(this.ckLocal, "ckLocal");
            this.ckLocal.Name = "ckLocal";
            this.ckLocal.CheckedChanged += this.Desktop_Changed;
            // 
            // tbPort
            // 
            resources.ApplyResources(this.tbPort, "tbPort");
            this.tbPort.Name = "tbPort";
            this.tbPort.TextChanged += this.Desktop_Changed;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.bRemoveMap);
            this.tabPage1.Controls.Add(this.bAddMap);
            this.tabPage1.Controls.Add(this.lbMaps);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // bRemoveMap
            // 
            resources.ApplyResources(this.bRemoveMap, "bRemoveMap");
            this.bRemoveMap.Name = "bRemoveMap";
            this.bRemoveMap.UseVisualStyleBackColor = true;
            this.bRemoveMap.Click += this.bRemoveMap_Click;
            // 
            // bAddMap
            // 
            resources.ApplyResources(this.bAddMap, "bAddMap");
            this.bAddMap.Name = "bAddMap";
            this.bAddMap.UseVisualStyleBackColor = true;
            this.bAddMap.Click += this.bAddMap_Click;
            // 
            // lbMaps
            // 
            this.lbMaps.FormattingEnabled = true;
            resources.ApplyResources(this.lbMaps, "lbMaps");
            this.lbMaps.Name = "lbMaps";
            // 
            // bApply
            // 
            resources.ApplyResources(this.bApply, "bApply");
            this.bApply.Name = "bApply";
            this.bApply.Click += this.bApply_Click;
            // 
            // DialogToolOptions
            // 
            this.AcceptButton = this.bOK;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.bCancel;
            this.Controls.Add(this.bApply);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOK);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogToolOptions";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
            this.tabControl1.ResumeLayout(false);
            this.tpGeneral.ResumeLayout(false);
            this.tpGeneral.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbPropLoc.ResumeLayout(false);
            this.gbPropLoc.PerformLayout();
            this.tpToolbar.ResumeLayout(false);
            this.tpDesktop.ResumeLayout(false);
            this.tpDesktop.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private Label label11;
		private ComboBox tbLanguage;
        private Label label12;
        private ComboBox comboXmlEndingLine;
        private GroupBox groupBox1;
        private RadioButton radioButtonCm;
        private RadioButton radioButtonInches;
    }
}
