using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogAbout : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.TextBox tbLicense;
private Majorsilence.Forms.LinkLabel linkLabel3;
private Majorsilence.Forms.LinkLabel linkLabel4;
private Majorsilence.Forms.Label label5;
private Majorsilence.Forms.Label label6;
private Majorsilence.Forms.Label label8;
private Majorsilence.Forms.Label lVersion;
private Majorsilence.Forms.Label lVMVersion;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogAbout));
            this.bOK = new Majorsilence.Forms.Button();
            this.tbLicense = new Majorsilence.Forms.TextBox();
            this.linkLabel3 = new Majorsilence.Forms.LinkLabel();
            this.linkLabel4 = new Majorsilence.Forms.LinkLabel();
            this.label5 = new Majorsilence.Forms.Label();
            this.label6 = new Majorsilence.Forms.Label();
            this.lVersion = new Majorsilence.Forms.Label();
            this.label8 = new Majorsilence.Forms.Label();
            this.lVMVersion = new Majorsilence.Forms.Label();
            this.SuspendLayout();
            // 
            // bOK
            // 
            resources.ApplyResources(this.bOK, "bOK");
            this.bOK.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
            this.bOK.Name = "bOK";
            // 
            // tbLicense
            // 
            resources.ApplyResources(this.tbLicense, "tbLicense");
            this.tbLicense.Name = "tbLicense";
            this.tbLicense.ReadOnly = true;
            // 
            // linkLabel3
            // 
            resources.ApplyResources(this.linkLabel3, "linkLabel3");
            this.linkLabel3.Name = "linkLabel3";
            this.linkLabel3.TabStop = true;
            this.linkLabel3.Tag = "https://github.com/majorsilence/Reporting/discussions";
            this.linkLabel3.LinkClicked += this.lnk_LinkClicked;
            // 
            // linkLabel4
            // 
            resources.ApplyResources(this.linkLabel4, "linkLabel4");
            this.linkLabel4.Name = "linkLabel4";
            this.linkLabel4.TabStop = true;
            this.linkLabel4.Tag = "https://github.com/majorsilence/Reporting";
            this.linkLabel4.LinkClicked += this.lnk_LinkClicked;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // lVersion
            // 
            resources.ApplyResources(this.lVersion, "lVersion");
            this.lVersion.Name = "lVersion";
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // lVMVersion
            // 
            resources.ApplyResources(this.lVMVersion, "lVMVersion");
            this.lVMVersion.Name = "lVMVersion";
            // 
            // DialogAbout
            // 
            this.AcceptButton = this.bOK;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.bOK;
            this.Controls.Add(this.lVMVersion);
            this.Controls.Add(this.linkLabel3);
            this.Controls.Add(this.linkLabel4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lVersion);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tbLicense);
            this.Controls.Add(this.bOK);
            this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogAbout";
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
