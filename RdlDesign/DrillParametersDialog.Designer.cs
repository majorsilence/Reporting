using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DrillParametersDialog : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Button bFile;
private System.Windows.Forms.TextBox tbReportFile;
private System.Windows.Forms.DataGridView dgParms;
private System.Windows.Forms.Button bRefreshParms;
private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bCancel;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DrillParametersDialog));
			this.dgParms = new System.Windows.Forms.DataGridView();
			this.label1 = new System.Windows.Forms.Label();
			this.tbReportFile = new System.Windows.Forms.TextBox();
			this.bFile = new System.Windows.Forms.Button();
			this.bRefreshParms = new System.Windows.Forms.Button();
			this.bOK = new System.Windows.Forms.Button();
			this.bCancel = new System.Windows.Forms.Button();
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
