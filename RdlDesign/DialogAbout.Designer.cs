using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogAbout : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private System.Windows.Forms.Button bOK;
private System.Windows.Forms.TextBox tbLicense;
private System.Windows.Forms.LinkLabel linkLabel3;
private System.Windows.Forms.LinkLabel linkLabel4;
private System.Windows.Forms.Label label5;
private System.Windows.Forms.Label label6;
private System.Windows.Forms.Label label8;
private System.Windows.Forms.Label lVersion;
private System.Windows.Forms.Label lVMVersion;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DialogAbout));
            this.bOK = new System.Windows.Forms.Button();
            this.tbLicense = new System.Windows.Forms.TextBox();
            this.linkLabel3 = new System.Windows.Forms.LinkLabel();
            this.linkLabel4 = new System.Windows.Forms.LinkLabel();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lVersion = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lVMVersion = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bOK
            // 
            resources.ApplyResources(this.bOK, "bOK");
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
            this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnk_LinkClicked);
            // 
            // linkLabel4
            // 
            resources.ApplyResources(this.linkLabel4, "linkLabel4");
            this.linkLabel4.Name = "linkLabel4";
            this.linkLabel4.TabStop = true;
            this.linkLabel4.Tag = "https://github.com/majorsilence/Reporting";
            this.linkLabel4.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnk_LinkClicked);
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
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogAbout";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
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
