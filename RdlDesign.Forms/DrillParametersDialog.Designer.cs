using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DrillParametersDialog : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.Button bFile;
private Majorsilence.Forms.TextBox tbReportFile;
private Majorsilence.Forms.DataGridView dgParms;
private Majorsilence.Forms.Button bRefreshParms;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DrillParametersDialog));
			this.dgParms = new Majorsilence.Forms.DataGridView();
			this.label1 = new Majorsilence.Forms.Label();
			this.tbReportFile = new Majorsilence.Forms.TextBox();
			this.bFile = new Majorsilence.Forms.Button();
			this.bRefreshParms = new Majorsilence.Forms.Button();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dgParms)).BeginInit();
			this.SuspendLayout();
			// 
			// dgParms
			// 
			resources.ApplyResources(this.dgParms, "dgParms");
			this.dgParms.DataMember = "";
			this.dgParms.Name = "dgParms";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbReportFile
			// 
			resources.ApplyResources(this.tbReportFile, "tbReportFile");
			this.tbReportFile.Name = "tbReportFile";
			// 
			// bFile
			// 
			resources.ApplyResources(this.bFile, "bFile");
			this.bFile.Name = "bFile";
			this.bFile.Click += this.bFile_Click;
			// 
			// bRefreshParms
			// 
			resources.ApplyResources(this.bRefreshParms, "bRefreshParms");
			this.bRefreshParms.Name = "bRefreshParms";
			this.bRefreshParms.Click += this.bRefreshParms_Click;
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.DialogResult = Majorsilence.Forms.DialogResult.OK;
			this.bOK.Name = "bOK";
			this.bOK.Click += this.bOK_Click;
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.CausesValidation = false;
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// DrillParametersDialog
			// 
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.CausesValidation = false;
			this.ControlBox = false;
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bRefreshParms);
			this.Controls.Add(this.bFile);
			this.Controls.Add(this.tbReportFile);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.dgParms);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DrillParametersDialog";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.dgParms)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

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
