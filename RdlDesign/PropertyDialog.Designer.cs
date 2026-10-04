using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class PropertyDialog : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private DesignXmlDraw _Draw;
private Majorsilence.Forms.Panel panel1;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bApply;
private Majorsilence.Forms.TabControl tcProps;
private Majorsilence.Forms.Button bDelete;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
            Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(PropertyDialog));
            this.panel1 = new Majorsilence.Forms.Panel();
            this.bDelete = new Majorsilence.Forms.Button();
            this.bApply = new Majorsilence.Forms.Button();
            this.bOK = new Majorsilence.Forms.Button();
            this.bCancel = new Majorsilence.Forms.Button();
            this.tcProps = new Majorsilence.Forms.TabControl();
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
            this.bDelete.Click += this.bDelete_Click;
            // 
            // bApply
            // 
            resources.ApplyResources(this.bApply, "bApply");
            this.bApply.Name = "bApply";
            this.bApply.Click += this.bApply_Click;
            // 
            // bOK
            // 
            resources.ApplyResources(this.bOK, "bOK");
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
            // tcProps
            // 
            resources.ApplyResources(this.tcProps, "tcProps");
            this.tcProps.Multiline = true;
            this.tcProps.Name = "tcProps";
            // SelectedIndex = 0 removed: tcProps has 0 tabs at this point (property tabs are
            // added dynamically at runtime via tcProps.Controls.Add), same gap as
            // RdlDesigner.Designer.cs's mainTC -- see that file's comment.
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
            this.FormClosing += this.PropertyDialog_Closing;
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
