using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class FindTab : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code

		private System.Windows.Forms.Label label1;
private System.Windows.Forms.TextBox txtFind;
private System.Windows.Forms.RadioButton radioUp;
private System.Windows.Forms.RadioButton radioDown;
private System.Windows.Forms.GroupBox groupBox1;
private System.Windows.Forms.CheckBox chkCase;
public System.Windows.Forms.TabPage tabGoTo;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.TextBox txtLine;
private System.Windows.Forms.Button btnNext;
private RdlEditPreview rdlEdit;
private System.Windows.Forms.Button btnGoto;
private System.Windows.Forms.Button btnCancel;
public System.Windows.Forms.TabPage tabReplace;
private System.Windows.Forms.Button btnFindNext;
private System.Windows.Forms.CheckBox chkMatchCase;
private System.Windows.Forms.Button btnReplaceAll;
private System.Windows.Forms.Button btnReplace;
private System.Windows.Forms.TextBox txtFindR;
private System.Windows.Forms.Label label3;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.TextBox txtReplace;
private System.Windows.Forms.Button bCloseReplace;
private System.Windows.Forms.Button bCloseGoto;
public System.Windows.Forms.TabControl tcFRG;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(FindTab));
			this.tcFRG = new System.Windows.Forms.TabControl();
			this.tabFind = new System.Windows.Forms.TabPage();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnNext = new System.Windows.Forms.Button();
			this.chkCase = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.radioUp = new System.Windows.Forms.RadioButton();
			this.radioDown = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.txtFind = new System.Windows.Forms.TextBox();
			this.tabReplace = new System.Windows.Forms.TabPage();
			this.bCloseReplace = new System.Windows.Forms.Button();
			this.btnFindNext = new System.Windows.Forms.Button();
			this.chkMatchCase = new System.Windows.Forms.CheckBox();
			this.btnReplaceAll = new System.Windows.Forms.Button();
			this.btnReplace = new System.Windows.Forms.Button();
			this.txtFindR = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.txtReplace = new System.Windows.Forms.TextBox();
			this.tabGoTo = new System.Windows.Forms.TabPage();
			this.bCloseGoto = new System.Windows.Forms.Button();
			this.txtLine = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.btnGoto = new System.Windows.Forms.Button();
			this.tcFRG.SuspendLayout();
			this.tabFind.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.tabReplace.SuspendLayout();
			this.tabGoTo.SuspendLayout();
			this.SuspendLayout();
			// 
			// tcFRG
			// 
			this.tcFRG.Controls.Add(this.tabFind);
			this.tcFRG.Controls.Add(this.tabReplace);
			this.tcFRG.Controls.Add(this.tabGoTo);
			resources.ApplyResources(this.tcFRG, "tcFRG");
			this.tcFRG.Name = "tcFRG";
			this.tcFRG.SelectedIndex = 0;
			this.tcFRG.SelectedIndexChanged += this.tcFRG_SelectedIndexChanged;
			this.tcFRG.Enter += this.tcFRG_Enter;
			// 
			// tabFind
			// 
			this.tabFind.Controls.Add(this.btnCancel);
			this.tabFind.Controls.Add(this.btnNext);
			this.tabFind.Controls.Add(this.chkCase);
			this.tabFind.Controls.Add(this.groupBox1);
			this.tabFind.Controls.Add(this.label1);
			this.tabFind.Controls.Add(this.txtFind);
			resources.ApplyResources(this.tabFind, "tabFind");
			this.tabFind.Name = "tabFind";
			this.tabFind.Tag = "find";
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += this.btnCancel_Click;
			// 
			// btnNext
			// 
			resources.ApplyResources(this.btnNext, "btnNext");
			this.btnNext.Name = "btnNext";
			this.btnNext.Click += this.btnNext_Click;
			// 
			// chkCase
			// 
			resources.ApplyResources(this.chkCase, "chkCase");
			this.chkCase.Name = "chkCase";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.radioUp);
			this.groupBox1.Controls.Add(this.radioDown);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// radioUp
			// 
			resources.ApplyResources(this.radioUp, "radioUp");
			this.radioUp.Name = "radioUp";
			// 
			// radioDown
			// 
			this.radioDown.Checked = true;
			resources.ApplyResources(this.radioDown, "radioDown");
			this.radioDown.Name = "radioDown";
			this.radioDown.TabStop = true;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// txtFind
			// 
			resources.ApplyResources(this.txtFind, "txtFind");
			this.txtFind.Name = "txtFind";
			this.txtFind.TextChanged += this.txtFind_TextChanged;
			// 
			// tabReplace
			// 
			this.tabReplace.Controls.Add(this.bCloseReplace);
			this.tabReplace.Controls.Add(this.btnFindNext);
			this.tabReplace.Controls.Add(this.chkMatchCase);
			this.tabReplace.Controls.Add(this.btnReplaceAll);
			this.tabReplace.Controls.Add(this.btnReplace);
			this.tabReplace.Controls.Add(this.txtFindR);
			this.tabReplace.Controls.Add(this.label3);
			this.tabReplace.Controls.Add(this.label2);
			this.tabReplace.Controls.Add(this.txtReplace);
			resources.ApplyResources(this.tabReplace, "tabReplace");
			this.tabReplace.Name = "tabReplace";
			this.tabReplace.Tag = "replace";
			// 
			// bCloseReplace
			// 
			resources.ApplyResources(this.bCloseReplace, "bCloseReplace");
			this.bCloseReplace.Name = "bCloseReplace";
			this.bCloseReplace.Click += this.btnCancel_Click;
			// 
			// btnFindNext
			// 
			resources.ApplyResources(this.btnFindNext, "btnFindNext");
			this.btnFindNext.Name = "btnFindNext";
			this.btnFindNext.Click += this.btnFindNext_Click;
			// 
			// chkMatchCase
			// 
			resources.ApplyResources(this.chkMatchCase, "chkMatchCase");
			this.chkMatchCase.Name = "chkMatchCase";
			// 
			// btnReplaceAll
			// 
			resources.ApplyResources(this.btnReplaceAll, "btnReplaceAll");
			this.btnReplaceAll.Name = "btnReplaceAll";
			this.btnReplaceAll.Click += this.btnReplaceAll_Click;
			// 
			// btnReplace
			// 
			resources.ApplyResources(this.btnReplace, "btnReplace");
			this.btnReplace.Name = "btnReplace";
			this.btnReplace.Click += this.btnReplace_Click;
			// 
			// txtFindR
			// 
			resources.ApplyResources(this.txtFindR, "txtFindR");
			this.txtFindR.Name = "txtFindR";
			this.txtFindR.TextChanged += this.txtFindR_TextChanged;
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
			// txtReplace
			// 
			resources.ApplyResources(this.txtReplace, "txtReplace");
			this.txtReplace.Name = "txtReplace";
			// 
			// tabGoTo
			// 
			this.tabGoTo.Controls.Add(this.bCloseGoto);
			this.tabGoTo.Controls.Add(this.txtLine);
			this.tabGoTo.Controls.Add(this.label4);
			this.tabGoTo.Controls.Add(this.btnGoto);
			resources.ApplyResources(this.tabGoTo, "tabGoTo");
			this.tabGoTo.Name = "tabGoTo";
			this.tabGoTo.Tag = "goto";
			// 
			// bCloseGoto
			// 
			resources.ApplyResources(this.bCloseGoto, "bCloseGoto");
			this.bCloseGoto.Name = "bCloseGoto";
			this.bCloseGoto.Click += this.btnCancel_Click;
			// 
			// txtLine
			// 
			resources.ApplyResources(this.txtLine, "txtLine");
			this.txtLine.Name = "txtLine";
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// btnGoto
			// 
			resources.ApplyResources(this.btnGoto, "btnGoto");
			this.btnGoto.Name = "btnGoto";
			this.btnGoto.Click += this.btnGoto_Click;
			// 
			// FindTab
			// 
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.tcFRG);
			this.Name = "FindTab";
			this.TopMost = true;
			this.FormClosed += this.FindTab_FormClosed;
			this.tcFRG.ResumeLayout(false);
			this.tabFind.ResumeLayout(false);
			this.tabFind.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.tabReplace.ResumeLayout(false);
			this.tabReplace.PerformLayout();
			this.tabGoTo.ResumeLayout(false);
			this.tabGoTo.PerformLayout();
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

		public TabPage tabFind;
	}
}
