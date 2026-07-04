using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogValidateRdl : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private RdlDesigner _RdlDesigner;
private Majorsilence.Forms.Button bClose;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.Button bValidate;
private Majorsilence.Forms.ListBox lbSchemaErrors;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogValidateRdl));
			this.bClose = new Majorsilence.Forms.Button();
			this.lbSchemaErrors = new Majorsilence.Forms.ListBox();
			this.label1 = new Majorsilence.Forms.Label();
			this.bValidate = new Majorsilence.Forms.Button();
			this.SuspendLayout();
			// 
			// bClose
			// 
			resources.ApplyResources(this.bClose, "bClose");
			this.bClose.CausesValidation = false;
			this.bClose.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bClose.Name = "bClose";
			this.bClose.Click += this.bClose_Click;
			// 
			// lbSchemaErrors
			// 
			resources.ApplyResources(this.lbSchemaErrors, "lbSchemaErrors");
			this.lbSchemaErrors.Name = "lbSchemaErrors";
			this.lbSchemaErrors.DoubleClick += this.lbSchemaErrors_DoubleClick;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// bValidate
			// 
			resources.ApplyResources(this.bValidate, "bValidate");
			this.bValidate.Name = "bValidate";
			this.bValidate.Click += this.bValidate_Click;
			// 
			// DialogValidateRdl
			// 
			this.AcceptButton = this.bValidate;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bClose;
			this.Controls.Add(this.bValidate);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.lbSchemaErrors);
			this.Controls.Add(this.bClose);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogValidateRdl";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Show;
			this.FormClosing += this.DialogValidateRdl_Closing;
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
