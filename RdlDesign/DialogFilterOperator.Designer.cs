using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogFilterOperator : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bCancel;
private System.Windows.Forms.Label lOp;
private System.Windows.Forms.ComboBox cbOperator;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DialogFilterOperator));
			this.bOK = new System.Windows.Forms.Button();
			this.bCancel = new System.Windows.Forms.Button();
			this.lOp = new System.Windows.Forms.Label();
			this.cbOperator = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
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
			this.bCancel.CausesValidation = false;
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// lOp
			// 
			resources.ApplyResources(this.lOp, "lOp");
			this.lOp.Name = "lOp";
			// 
			// cbOperator
			// 
			resources.ApplyResources(this.cbOperator, "cbOperator");
			this.cbOperator.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.Simple;
			this.cbOperator.Items.AddRange(new object[] {
            resources.GetString("cbOperator.Items"),
            resources.GetString("cbOperator.Items1"),
            resources.GetString("cbOperator.Items2"),
            resources.GetString("cbOperator.Items3"),
            resources.GetString("cbOperator.Items4"),
            resources.GetString("cbOperator.Items5"),
            resources.GetString("cbOperator.Items6"),
            resources.GetString("cbOperator.Items7"),
            resources.GetString("cbOperator.Items8"),
            resources.GetString("cbOperator.Items9"),
            resources.GetString("cbOperator.Items10"),
            resources.GetString("cbOperator.Items11"),
            resources.GetString("cbOperator.Items12")});
			this.cbOperator.Name = "cbOperator";
			this.cbOperator.Validating += this.DialogFilterOperator_Validating;
			// 
			// DialogFilterOperator
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.cbOperator);
			this.Controls.Add(this.lOp);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogFilterOperator";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = Majorsilence.Forms.SizeGripStyle.Hide;
			this.Validating += this.DialogFilterOperator_Validating;
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
