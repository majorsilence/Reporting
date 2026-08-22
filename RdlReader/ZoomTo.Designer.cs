using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlReader
{
    public partial class ZoomTo : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.ComboBox cbMagnify;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private RdlViewer.RdlViewer _Viewer;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(ZoomTo));
			this.label1 = new Majorsilence.Forms.Label();
			this.cbMagnify = new Majorsilence.Forms.ComboBox();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// cbMagnify
			// 
			resources.ApplyResources(this.cbMagnify, "cbMagnify");
			this.cbMagnify.Items.AddRange(new object[] {
            resources.GetString("cbMagnify.Items"),
            resources.GetString("cbMagnify.Items1"),
            resources.GetString("cbMagnify.Items2"),
            resources.GetString("cbMagnify.Items3"),
            resources.GetString("cbMagnify.Items4"),
            resources.GetString("cbMagnify.Items5"),
            resources.GetString("cbMagnify.Items6"),
            resources.GetString("cbMagnify.Items7"),
            resources.GetString("cbMagnify.Items8"),
            resources.GetString("cbMagnify.Items9"),
            resources.GetString("cbMagnify.Items10")});
			this.cbMagnify.Name = "cbMagnify";
			// 
			// bOK
			// 
			resources.ApplyResources(this.bOK, "bOK");
			this.bOK.DialogResult = Majorsilence.Forms.DialogResult.OK;
			this.bOK.Name = "bOK";
			this.bOK.Click += new System.EventHandler(this.bOK_Click);
			// 
			// bCancel
			// 
			resources.ApplyResources(this.bCancel, "bCancel");
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
			// 
			// ZoomTo
			// 
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.cbMagnify);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ZoomTo";
			this.ShowInTaskbar = false;
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
