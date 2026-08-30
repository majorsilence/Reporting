using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogFilterOperator : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.Label lOp;
private Majorsilence.Forms.ComboBox cbOperator;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogFilterOperator));
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.lOp = new Majorsilence.Forms.Label();
			this.cbOperator = new Majorsilence.Forms.ComboBox();
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
