using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlViewer
{
    public partial class DialogMessages : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private System.Windows.Forms.Button bOK;
private System.Windows.Forms.TextBox tbMessages;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DialogMessages));
			this.bOK = new System.Windows.Forms.Button();
			this.tbMessages = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bOK.Name = "bOK";
			// 
			// tbMessages
			// 
			resources.ApplyResources(this.tbMessages, "tbMessages");
			this.tbMessages.Name = "tbMessages";
			this.tbMessages.ReadOnly = true;
			// 
			// DialogMessages
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bOK;
			this.Controls.Add(this.tbMessages);
			this.Controls.Add(this.bOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogMessages";
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
