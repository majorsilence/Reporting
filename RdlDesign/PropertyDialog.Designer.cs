using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class PropertyDialog : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private System.Windows.Forms.Panel panel1;
private System.Windows.Forms.Button bCancel;
private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bApply;
private System.Windows.Forms.TabControl tcProps;
private System.Windows.Forms.Button bDelete;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertyDialog));
            this.panel1 = new System.Windows.Forms.Panel();
            this.bDelete = new System.Windows.Forms.Button();
            this.bApply = new System.Windows.Forms.Button();
            this.bOK = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.tcProps = new System.Windows.Forms.TabControl();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.CausesValidation = false;
            this.panel1.Controls.Add(this.bDelete);
            this.panel1.Controls.Add(this.bApply);
            this.panel1.Controls.Add(this.bOK);
            this.panel1.Controls.Add(this.bCancel);
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // bDelete
            // 
            resources.ApplyResources(this.bDelete, "bDelete");
            this.bDelete.Name = "bDelete";
            this.bDelete.Click += new System.EventHandler(this.bDelete_Click);
            // 
            // bApply
            // 
            resources.ApplyResources(this.bApply, "bApply");
            this.bApply.Name = "bApply";
            this.bApply.Click += new System.EventHandler(this.bApply_Click);
            // 
            // bOK
            // 
            resources.ApplyResources(this.bOK, "bOK");
            this.bOK.Name = "bOK";
            this.bOK.Click += new System.EventHandler(this.bOK_Click);
            // 
            // bCancel
            // 
            resources.ApplyResources(this.bCancel, "bCancel");
            this.bCancel.CausesValidation = false;
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Name = "bCancel";
            // 
            // tcProps
            // 
            resources.ApplyResources(this.tcProps, "tcProps");
            this.tcProps.Multiline = true;
            this.tcProps.Name = "tcProps";
            this.tcProps.SelectedIndex = 0;
            // 
            // PropertyDialog
            // 
            this.AcceptButton = this.bOK;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.bCancel;
            this.Controls.Add(this.tcProps);
            this.Controls.Add(this.panel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PropertyDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PropertyDialog_Closing);
            this.panel1.ResumeLayout(false);
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
