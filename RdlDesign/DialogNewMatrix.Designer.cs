using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogNewMatrix : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bCancel;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.ComboBox cbDataSets;
private System.Windows.Forms.Label label2;
private System.Windows.Forms.Label label3;
private System.Windows.Forms.ListBox lbFields;
private System.Windows.Forms.CheckedListBox lbMatrixColumns;
private System.Windows.Forms.Button bColumnUp;
private System.Windows.Forms.Button bColumnDown;
private System.Windows.Forms.Button bColumn;
private System.Windows.Forms.Button bRowSelect;
private System.Windows.Forms.CheckedListBox lbMatrixRows;
private System.Windows.Forms.Button bColumnDelete;
private System.Windows.Forms.Button bRowDelete;
private System.Windows.Forms.Button bRowDown;
private System.Windows.Forms.Button bRowUp;
private System.Windows.Forms.Label label4;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.ComboBox cbMatrixCell;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DialogNewMatrix));
			this.bOK = new System.Windows.Forms.Button();
			this.bCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.cbDataSets = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.lbFields = new System.Windows.Forms.ListBox();
			this.lbMatrixColumns = new System.Windows.Forms.CheckedListBox();
			this.bColumnUp = new System.Windows.Forms.Button();
			this.bColumnDown = new System.Windows.Forms.Button();
			this.bColumn = new System.Windows.Forms.Button();
			this.bRowSelect = new System.Windows.Forms.Button();
			this.lbMatrixRows = new System.Windows.Forms.CheckedListBox();
			this.bColumnDelete = new System.Windows.Forms.Button();
			this.bRowDelete = new System.Windows.Forms.Button();
			this.bRowDown = new System.Windows.Forms.Button();
			this.bRowUp = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.cbMatrixCell = new System.Windows.Forms.ComboBox();
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
			// lbMatrixColumns
			// 
			resources.ApplyResources(this.lbMatrixColumns, "lbMatrixColumns");
			this.lbMatrixColumns.Name = "lbMatrixColumns";
			// 
			// bColumnUp
			// 
			resources.ApplyResources(this.bColumnUp, "bColumnUp");
			this.bColumnUp.Name = "bColumnUp";
			this.bColumnUp.Click += this.bColumnUp_Click;
			// 
			// bColumnDown
			// 
			resources.ApplyResources(this.bColumnDown, "bColumnDown");
			this.bColumnDown.Name = "bColumnDown";
			this.bColumnDown.Click += this.bColumnDown_Click;
			// 
			// bColumn
			// 
			resources.ApplyResources(this.bColumn, "bColumn");
			this.bColumn.Name = "bColumn";
			this.bColumn.Click += this.bColumn_Click;
			// 
			// bRowSelect
			// 
			resources.ApplyResources(this.bRowSelect, "bRowSelect");
			this.bRowSelect.Name = "bRowSelect";
			this.bRowSelect.Click += this.bRow_Click;
			// 
			// lbMatrixRows
			// 
			resources.ApplyResources(this.lbMatrixRows, "lbMatrixRows");
			this.lbMatrixRows.Name = "lbMatrixRows";
			// 
			// bColumnDelete
			// 
			resources.ApplyResources(this.bColumnDelete, "bColumnDelete");
			this.bColumnDelete.Name = "bColumnDelete";
			this.bColumnDelete.Click += this.bColumnDelete_Click;
			// 
			// bRowDelete
			// 
			resources.ApplyResources(this.bRowDelete, "bRowDelete");
			this.bRowDelete.Name = "bRowDelete";
			this.bRowDelete.Click += this.bRowDelete_Click;
			// 
			// bRowDown
			// 
			resources.ApplyResources(this.bRowDown, "bRowDown");
			this.bRowDown.Name = "bRowDown";
			this.bRowDown.Click += this.bRowDown_Click;
			// 
			// bRowUp
			// 
			resources.ApplyResources(this.bRowUp, "bRowUp");
			this.bRowUp.Name = "bRowUp";
			this.bRowUp.Click += this.bRowUp_Click;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// cbMatrixCell
			// 
			resources.ApplyResources(this.cbMatrixCell, "cbMatrixCell");
			this.cbMatrixCell.Name = "cbMatrixCell";
			this.cbMatrixCell.TextChanged += this.cbMatrixCell_TextChanged;
			this.cbMatrixCell.Enter += this.cbMatrixCell_Enter;
			// 
			// DialogNewMatrix
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.cbMatrixCell);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.bRowDelete);
			this.Controls.Add(this.bRowDown);
			this.Controls.Add(this.bRowUp);
			this.Controls.Add(this.bColumnDelete);
			this.Controls.Add(this.lbMatrixRows);
			this.Controls.Add(this.bRowSelect);
			this.Controls.Add(this.bColumn);
			this.Controls.Add(this.bColumnDown);
			this.Controls.Add(this.bColumnUp);
			this.Controls.Add(this.lbMatrixColumns);
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
			this.Name = "DialogNewMatrix";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
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
