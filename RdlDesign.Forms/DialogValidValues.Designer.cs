using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogValidValues : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
private Majorsilence.Forms.DataGridView dgParms;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Button bDelete;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogValidValues));
			this.dgParms = new Majorsilence.Forms.DataGridView();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.bDelete = new Majorsilence.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dgParms)).BeginInit();
			this.SuspendLayout();
			// 
			// dgParms
			// 
			resources.ApplyResources(this.dgParms, "dgParms");
			this.dgParms.DataMember = "";
			this.dgParms.Name = "dgParms";
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.DialogResult = Majorsilence.Forms.DialogResult.OK;
			this.bOK.Name = "bOK";
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// bDelete
			// 
			resources.ApplyResources(this.bDelete, "bDelete");
			this.bDelete.Name = "bDelete";
			this.bDelete.Click += this.bDelete_Click;
			// 
			// DialogValidValues
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.ControlBox = false;
			this.Controls.Add(this.bDelete);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.dgParms);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogValidValues";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.dgParms)).EndInit();
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
