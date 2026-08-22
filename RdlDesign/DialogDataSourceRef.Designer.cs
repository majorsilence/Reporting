using System;
using Majorsilence.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    public partial class DialogDataSourceRef : Majorsilence.Forms.Form
	{
		#region Windows Form Designer generated code
		private Majorsilence.Forms.Label label1;
private Majorsilence.Forms.TextBox tbPassword;
private Majorsilence.Forms.Label label2;
private Majorsilence.Forms.TextBox tbFilename;
private Majorsilence.Forms.Button bGetFilename;
private Majorsilence.Forms.Label label3;
private Majorsilence.Forms.ComboBox cbDataProvider;
private Majorsilence.Forms.Label label4;
private Majorsilence.Forms.TextBox tbConnection;
private Majorsilence.Forms.CheckBox ckbIntSecurity;
private Majorsilence.Forms.Label label5;
private Majorsilence.Forms.TextBox tbPrompt;
private Majorsilence.Forms.Button bOK;
private Majorsilence.Forms.Button bCancel;
private Majorsilence.Forms.TextBox tbPassword2;
private Majorsilence.Forms.Label label6;
private Majorsilence.Forms.Button bTestConnection;
private Majorsilence.Forms.ComboBox cbOdbcNames;
private Majorsilence.Forms.Label lODBC;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			Majorsilence.Forms.ComponentResourceManager resources = new Majorsilence.Forms.ComponentResourceManager(typeof(DialogDataSourceRef));
			this.label1 = new Majorsilence.Forms.Label();
			this.tbPassword = new Majorsilence.Forms.TextBox();
			this.label2 = new Majorsilence.Forms.Label();
			this.tbFilename = new Majorsilence.Forms.TextBox();
			this.bGetFilename = new Majorsilence.Forms.Button();
			this.label3 = new Majorsilence.Forms.Label();
			this.cbDataProvider = new Majorsilence.Forms.ComboBox();
			this.label4 = new Majorsilence.Forms.Label();
			this.tbConnection = new Majorsilence.Forms.TextBox();
			this.ckbIntSecurity = new Majorsilence.Forms.CheckBox();
			this.label5 = new Majorsilence.Forms.Label();
			this.tbPrompt = new Majorsilence.Forms.TextBox();
			this.bOK = new Majorsilence.Forms.Button();
			this.bCancel = new Majorsilence.Forms.Button();
			this.tbPassword2 = new Majorsilence.Forms.TextBox();
			this.label6 = new Majorsilence.Forms.Label();
			this.bTestConnection = new Majorsilence.Forms.Button();
			this.cbOdbcNames = new Majorsilence.Forms.ComboBox();
			this.lODBC = new Majorsilence.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbPassword
			// 
			resources.ApplyResources(this.tbPassword, "tbPassword");
			this.tbPassword.Name = "tbPassword";
			this.tbPassword.TextChanged += this.validate_TextChanged;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// tbFilename
			// 
			resources.ApplyResources(this.tbFilename, "tbFilename");
			this.tbFilename.Name = "tbFilename";
			this.tbFilename.TextChanged += this.validate_TextChanged;
			// 
			// bGetFilename
			// 
			resources.ApplyResources(this.bGetFilename, "bGetFilename");
			this.bGetFilename.Name = "bGetFilename";
			this.bGetFilename.Click += this.bGetFilename_Click;
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// cbDataProvider
			// 
			resources.ApplyResources(this.cbDataProvider, "cbDataProvider");
			this.cbDataProvider.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDataProvider.Name = "cbDataProvider";
			this.cbDataProvider.SelectedIndexChanged += this.cbDataProvider_SelectedIndexChanged;
			// 
			// label4
			// 
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			// 
			// tbConnection
			// 
			resources.ApplyResources(this.tbConnection, "tbConnection");
			this.tbConnection.Name = "tbConnection";
			this.tbConnection.TextChanged += this.validate_TextChanged;
			// 
			// ckbIntSecurity
			// 
			resources.ApplyResources(this.ckbIntSecurity, "ckbIntSecurity");
			this.ckbIntSecurity.Name = "ckbIntSecurity";
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// tbPrompt
			// 
			resources.ApplyResources(this.tbPrompt, "tbPrompt");
			this.tbPrompt.Name = "tbPrompt";
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
			this.bCancel.DialogResult = Majorsilence.Forms.DialogResult.Cancel;
			this.bCancel.Name = "bCancel";
			// 
			// tbPassword2
			// 
			resources.ApplyResources(this.tbPassword2, "tbPassword2");
			this.tbPassword2.Name = "tbPassword2";
			this.tbPassword2.TextChanged += this.validate_TextChanged;
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// bTestConnection
			// 
			resources.ApplyResources(this.bTestConnection, "bTestConnection");
			this.bTestConnection.Name = "bTestConnection";
			this.bTestConnection.Click += this.bTestConnection_Click;
			// 
			// cbOdbcNames
			// 
			resources.ApplyResources(this.cbOdbcNames, "cbOdbcNames");
			this.cbOdbcNames.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbOdbcNames.Name = "cbOdbcNames";
			this.cbOdbcNames.Sorted = true;
			this.cbOdbcNames.SelectedIndexChanged += this.cbOdbcNames_SelectedIndexChanged;
			// 
			// lODBC
			// 
			resources.ApplyResources(this.lODBC, "lODBC");
			this.lODBC.Name = "lODBC";
			// 
			// DialogDataSourceRef
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.cbOdbcNames);
			this.Controls.Add(this.lODBC);
			this.Controls.Add(this.bTestConnection);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.tbPassword2);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.tbPrompt);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.ckbIntSecurity);
			this.Controls.Add(this.tbConnection);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.cbDataProvider);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.bGetFilename);
			this.Controls.Add(this.tbFilename);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.tbPassword);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogDataSourceRef";
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
