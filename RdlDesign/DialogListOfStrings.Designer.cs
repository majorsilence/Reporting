using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogListOfStrings : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.TextBox tbStrings;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogListOfStrings));
			this.bOK = new Majorsilence.Forms.Button();
			this.tbStrings = new Majorsilence.Forms.TextBox();
			this.bCancel = new Majorsilence.Forms.Button();
			this.label1 = new Majorsilence.Forms.Label();
			this.SuspendLayout();
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.DialogResult = Majorsilence.Forms.DialogResult.OK;
			this.bOK.Name = "bOK";
			// 
			// tbStrings
			// 
			resources.ApplyResources(this.tbStrings, "tbStrings");
			this.tbStrings.Name = "tbStrings";
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
			// DialogListOfStrings
			// 
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.tbStrings);
			this.Controls.Add(this.bOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogListOfStrings";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
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
