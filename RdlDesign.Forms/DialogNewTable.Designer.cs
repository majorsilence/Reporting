using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogNewTable : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.ComboBox cbDataSets;
private Majorsilence.Forms.Label label2;
private Majorsilence.Forms.Label label3;
private Majorsilence.Forms.ListBox lbFields;
private Majorsilence.Forms.CheckedListBox lbTableColumns;
private Majorsilence.Forms.Button bUp;
private Majorsilence.Forms.Button bDown;
private Majorsilence.Forms.Button bRight;
private Majorsilence.Forms.Button bAllRight;
private Majorsilence.Forms.Button bLeft;
private Majorsilence.Forms.Button bAllLeft;
private Majorsilence.Forms.Label label4;
private Majorsilence.Forms.ComboBox cbGroupColumn;
private Majorsilence.Forms.CheckBox chkGrandTotals;
private Majorsilence.Forms.GroupBox groupBox1;
private Majorsilence.Forms.RadioButton rbHorz;
private Majorsilence.Forms.RadioButton rbVert;
private Majorsilence.Forms.RadioButton rbVertComp;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogNewTable));
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.cbDataSets = new Majorsilence.Forms.ComboBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.label3 = new Majorsilence.Forms.Label();
			this.lbFields = new Majorsilence.Forms.ListBox();
			this.lbTableColumns = new Majorsilence.Forms.CheckedListBox();
			this.bUp = new Majorsilence.Forms.Button();
			this.bDown = new Majorsilence.Forms.Button();
			this.bRight = new Majorsilence.Forms.Button();
			this.bAllRight = new Majorsilence.Forms.Button();
			this.bLeft = new Majorsilence.Forms.Button();
			this.bAllLeft = new Majorsilence.Forms.Button();
			this.label4 = new Majorsilence.Forms.Label();
			this.cbGroupColumn = new Majorsilence.Forms.ComboBox();
			this.chkGrandTotals = new Majorsilence.Forms.CheckBox();
			this.groupBox1 = new Majorsilence.Forms.GroupBox();
			this.rbVertComp = new Majorsilence.Forms.RadioButton();
			this.rbVert = new Majorsilence.Forms.RadioButton();
			this.rbHorz = new Majorsilence.Forms.RadioButton();
			this.groupBox1.SuspendLayout();
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
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// cbDataSets
			// 
			resources.ApplyResources(this.cbDataSets, "cbDataSets");
			this.cbDataSets.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDataSets.Name = "cbDataSets";
			this.cbDataSets.SelectedIndexChanged += this.cbDataSets_SelectedIndexChanged;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// lbFields
			// 
			resources.ApplyResources(this.lbFields, "lbFields");
			this.lbFields.Name = "lbFields";
			this.lbFields.SelectionMode = Majorsilence.Forms.SelectionMode.MultiExtended;
			// 
			// lbTableColumns
			// 
			resources.ApplyResources(this.lbTableColumns, "lbTableColumns");
			this.lbTableColumns.Name = "lbTableColumns";
			// 
			// bUp
			// 
			resources.ApplyResources(this.bUp, "bUp");
			this.bUp.Name = "bUp";
			this.bUp.Click += this.bUp_Click;
			// 
			// bDown
			// 
			resources.ApplyResources(this.bDown, "bDown");
			this.bDown.Name = "bDown";
			this.bDown.Click += this.bDown_Click;
			// 
			// bRight
			// 
			resources.ApplyResources(this.bRight, "bRight");
			this.bRight.Name = "bRight";
			this.bRight.Click += this.bRight_Click;
			// 
			// bAllRight
			// 
			resources.ApplyResources(this.bAllRight, "bAllRight");
			this.bAllRight.Name = "bAllRight";
			this.bAllRight.Click += this.bAllRight_Click;
			// 
			// bLeft
			// 
			resources.ApplyResources(this.bLeft, "bLeft");
			this.bLeft.Name = "bLeft";
			this.bLeft.Click += this.bLeft_Click;
			// 
			// bAllLeft
			// 
			resources.ApplyResources(this.bAllLeft, "bAllLeft");
			this.bAllLeft.Name = "bAllLeft";
			this.bAllLeft.Click += this.bAllLeft_Click;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// cbGroupColumn
			// 
			resources.ApplyResources(this.cbGroupColumn, "cbGroupColumn");
			this.cbGroupColumn.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbGroupColumn.Name = "cbGroupColumn";
			this.cbGroupColumn.Enter += this.cbGroupColumn_Enter;
			// 
			// chkGrandTotals
			// 
			resources.ApplyResources(this.chkGrandTotals, "chkGrandTotals");
			this.chkGrandTotals.Name = "chkGrandTotals";
			// 
			// groupBox1
			// 
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Controls.Add(this.rbVertComp);
			this.groupBox1.Controls.Add(this.rbVert);
			this.groupBox1.Controls.Add(this.rbHorz);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// rbVertComp
			// 
			resources.ApplyResources(this.rbVertComp, "rbVertComp");
			this.rbVertComp.Name = "rbVertComp";
			// 
			// rbVert
			// 
			resources.ApplyResources(this.rbVert, "rbVert");
			this.rbVert.Name = "rbVert";
			// 
			// rbHorz
			// 
			resources.ApplyResources(this.rbHorz, "rbHorz");
			this.rbHorz.Name = "rbHorz";
			this.rbHorz.CheckedChanged += this.rbHorz_CheckedChanged;
			// 
			// DialogNewTable
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.chkGrandTotals);
			this.Controls.Add(this.cbGroupColumn);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.bAllLeft);
			this.Controls.Add(this.bLeft);
			this.Controls.Add(this.bAllRight);
			this.Controls.Add(this.bRight);
			this.Controls.Add(this.bDown);
			this.Controls.Add(this.bUp);
			this.Controls.Add(this.lbTableColumns);
			this.Controls.Add(this.lbFields);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.cbDataSets);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogNewTable";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			this.groupBox1.ResumeLayout(false);
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
	}
}
