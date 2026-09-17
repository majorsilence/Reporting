using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace Majorsilence.Reporting.RdlDesign
{
    internal partial class DialogDataSources : System.Windows.Forms.Form
	{
		#region Windows Form Designer generated code
		DesignXmlDraw _Draw;
private System.Windows.Forms.TextBox tbFilename;
private System.Windows.Forms.Button bGetFilename;
private System.Windows.Forms.ComboBox cbDataProvider;
private System.Windows.Forms.TextBox tbConnection;
private System.Windows.Forms.CheckBox ckbIntSecurity;
private System.Windows.Forms.TextBox tbPrompt;
private System.Windows.Forms.Button bOK;
private System.Windows.Forms.Button bCancel;
private System.Windows.Forms.Button bTestConnection;
private System.Windows.Forms.ListBox lbDataSources;
private System.Windows.Forms.Button bRemove;
private System.Windows.Forms.Button bAdd;
private System.Windows.Forms.CheckBox chkSharedDataSource;
private System.Windows.Forms.Label label1;
private System.Windows.Forms.Label lDataProvider;
private System.Windows.Forms.Label lConnectionString;
private System.Windows.Forms.Label lPrompt;
private System.Windows.Forms.TextBox tbDSName;
private System.Windows.Forms.Button bExprConnect;
private System.ComponentModel.Container components = null;

		private void InitializeComponent()
		{
			System.Windows.Forms.ComponentResourceManager resources = new System.Windows.Forms.ComponentResourceManager(typeof(DialogDataSources));
			this.tbFilename = new System.Windows.Forms.TextBox();
			this.bGetFilename = new System.Windows.Forms.Button();
			this.lDataProvider = new System.Windows.Forms.Label();
			this.cbDataProvider = new System.Windows.Forms.ComboBox();
			this.lConnectionString = new System.Windows.Forms.Label();
			this.tbConnection = new System.Windows.Forms.TextBox();
			this.ckbIntSecurity = new System.Windows.Forms.CheckBox();
			this.lPrompt = new System.Windows.Forms.Label();
			this.tbPrompt = new System.Windows.Forms.TextBox();
			this.bOK = new System.Windows.Forms.Button();
			this.bCancel = new System.Windows.Forms.Button();
			this.bTestConnection = new System.Windows.Forms.Button();
			this.lbDataSources = new System.Windows.Forms.ListBox();
			this.bRemove = new System.Windows.Forms.Button();
			this.bAdd = new System.Windows.Forms.Button();
			this.chkSharedDataSource = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.tbDSName = new System.Windows.Forms.TextBox();
			this.bExprConnect = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// tbFilename
			// 
			resources.ApplyResources(this.tbFilename, "tbFilename");
			this.tbFilename.Name = "tbFilename";
			this.tbFilename.TextChanged += this.tbFilename_TextChanged;
			// 
			// bGetFilename
			// 
			resources.ApplyResources(this.bGetFilename, "bGetFilename");
			this.bGetFilename.Name = "bGetFilename";
			this.bGetFilename.Click += this.bGetFilename_Click;
			// 
			// lDataProvider
			// 
			resources.ApplyResources(this.lDataProvider, "lDataProvider");
			this.lDataProvider.Name = "lDataProvider";
			// 
			// cbDataProvider
			// 
			resources.ApplyResources(this.cbDataProvider, "cbDataProvider");
			this.cbDataProvider.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
			this.cbDataProvider.Items.AddRange(new object[] {
            resources.GetString("cbDataProvider.Items"),
            resources.GetString("cbDataProvider.Items1"),
            resources.GetString("cbDataProvider.Items2")});
			this.cbDataProvider.Name = "cbDataProvider";
			this.cbDataProvider.SelectedIndexChanged += this.cbDataProvider_SelectedIndexChanged;
			// 
			// lConnectionString
			// 
			resources.ApplyResources(this.lConnectionString, "lConnectionString");
			this.lConnectionString.Name = "lConnectionString";
			// 
			// tbConnection
			// 
			resources.ApplyResources(this.tbConnection, "tbConnection");
			this.tbConnection.Name = "tbConnection";
			this.tbConnection.TextChanged += this.tbConnection_TextChanged;
			// 
			// ckbIntSecurity
			// 
			resources.ApplyResources(this.ckbIntSecurity, "ckbIntSecurity");
			this.ckbIntSecurity.Name = "ckbIntSecurity";
			this.ckbIntSecurity.CheckedChanged += this.ckbIntSecurity_CheckedChanged;
			// 
			// lPrompt
			// 
			resources.ApplyResources(this.lPrompt, "lPrompt");
			this.lPrompt.Name = "lPrompt";
			// 
			// tbPrompt
			// 
			resources.ApplyResources(this.tbPrompt, "tbPrompt");
			this.tbPrompt.Name = "tbPrompt";
			this.tbPrompt.TextChanged += this.tbPrompt_TextChanged;
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
			// bTestConnection
			// 
			resources.ApplyResources(this.bTestConnection, "bTestConnection");
			this.bTestConnection.Name = "bTestConnection";
			this.bTestConnection.Click += this.bTestConnection_Click;
			// 
			// lbDataSources
			// 
			resources.ApplyResources(this.lbDataSources, "lbDataSources");
			this.lbDataSources.Name = "lbDataSources";
			this.lbDataSources.SelectedIndexChanged += this.lbDataSources_SelectedIndexChanged;
			// 
			// bRemove
			// 
			resources.ApplyResources(this.bRemove, "bRemove");
			this.bRemove.Name = "bRemove";
			this.bRemove.Click += this.bRemove_Click;
			// 
			// bAdd
			// 
			resources.ApplyResources(this.bAdd, "bAdd");
			this.bAdd.Name = "bAdd";
			this.bAdd.Click += this.bAdd_Click;
			// 
			// chkSharedDataSource
			// 
			resources.ApplyResources(this.chkSharedDataSource, "chkSharedDataSource");
			this.chkSharedDataSource.Name = "chkSharedDataSource";
			this.chkSharedDataSource.CheckedChanged += this.chkSharedDataSource_CheckedChanged;
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbDSName
			// 
			resources.ApplyResources(this.tbDSName, "tbDSName");
			this.tbDSName.Name = "tbDSName";
			this.tbDSName.TextChanged += this.tbDSName_TextChanged;
			this.tbDSName.Validating += this.tbDSName_Validating;
			// 
			// bExprConnect
			// 
			resources.ApplyResources(this.bExprConnect, "bExprConnect");
			this.bExprConnect.Name = "bExprConnect";
			this.bExprConnect.Tag = "pright";
			this.bExprConnect.Click += this.bExprConnect_Click;
			// 
			// DialogDataSources
			// 
			this.AcceptButton = this.bOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.bCancel;
			this.Controls.Add(this.bExprConnect);
			this.Controls.Add(this.tbDSName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.chkSharedDataSource);
			this.Controls.Add(this.bRemove);
			this.Controls.Add(this.bAdd);
			this.Controls.Add(this.lbDataSources);
			this.Controls.Add(this.bTestConnection);
			this.Controls.Add(this.bCancel);
			this.Controls.Add(this.bOK);
			this.Controls.Add(this.tbPrompt);
			this.Controls.Add(this.lPrompt);
			this.Controls.Add(this.ckbIntSecurity);
			this.Controls.Add(this.tbConnection);
			this.Controls.Add(this.lConnectionString);
			this.Controls.Add(this.cbDataProvider);
			this.Controls.Add(this.lDataProvider);
			this.Controls.Add(this.bGetFilename);
			this.Controls.Add(this.tbFilename);
			this.FormBorderStyle = Majorsilence.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogDataSources";
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
