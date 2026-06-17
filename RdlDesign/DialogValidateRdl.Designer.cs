using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogValidateRdl : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private RdlDesigner _RdlDesigner;
private System.Windows.Forms.Button bClose;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Button bValidate;
private System.Windows.Forms.ListBox lbSchemaErrors;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DialogValidateRdl));
			this.bClose = new System.Windows.Forms.Button();
			this.lbSchemaErrors = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.bValidate = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// bClose
			// 
			resources.ApplyResources(this.bClose, "bClose");
			this.bClose.CausesValidation = false;
			this.bClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.bClose.Name = "bClose";
			this.bClose.Click += new System.EventHandler(this.bClose_Click);
			// 
			// lbSchemaErrors
			// 
			resources.ApplyResources(this.lbSchemaErrors, "lbSchemaErrors");
			this.lbSchemaErrors.Name = "lbSchemaErrors";
			this.lbSchemaErrors.DoubleClick += new System.EventHandler(this.lbSchemaErrors_DoubleClick);
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
			this.bValidate.Click += new System.EventHandler(this.bValidate_Click);
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
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DialogValidateRdl_Closing);
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
