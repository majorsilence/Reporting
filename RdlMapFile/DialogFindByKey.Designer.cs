using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlMapFile
{
    public partial class DialogFindByKey : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private Label label1;
private Button bOK;
private Button bCancel;
private ListBox lbKeyList;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogFindByKey));
			this.label1 = new Majorsilence.Forms.Label();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.lbKeyList = new Majorsilence.Forms.ListBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.Name = "bOK";
			this.bOK.UseVisualStyleBackColor = true;
			this.bOK.Click += this.bOK_Click;
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			this.bCancel.UseVisualStyleBackColor = true;
			this.bCancel.Click += this.bCancel_Click;
			// 
			// lbKeyList
			// 
			resources.ApplyResources(this.lbKeyList, "lbKeyList");
			this.lbKeyList.FormattingEnabled = true;
			this.lbKeyList.Name = "lbKeyList";
			this.lbKeyList.SelectionMode = Majorsilence.Forms.SelectionMode.MultiExtended;
			// 
			// DialogFindByKey
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.lbKeyList);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogFindByKey";
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
