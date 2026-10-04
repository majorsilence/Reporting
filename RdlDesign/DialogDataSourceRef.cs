using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;


namespace Majorsilence.Reporting.RdlDesign
{
	/// <summary>
	/// Summary description for DialogDataSourceRef.
	/// </summary>
	public partial class DialogDataSourceRef 
	{
			public DialogDataSourceRef()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			string[] items = RdlEngineConfig.GetProviders();

			cbDataProvider.Items.AddRange(items);

			this.cbDataProvider.SelectedIndex = 0;
			this.bOK.Enabled = false;		
		}

		private void bGetFilename_Click(object sender, System.EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = Strings.DialogDataSourceRef_bGetFilename_Click_DSRFilter;
			sfd.FilterIndex = 1;
			if (tbFilename.Text.Length > 0)
				sfd.FileName = tbFilename.Text;
			else
				sfd.FileName = "*.dsr";

			sfd.Title = Strings.DialogDataSourceRef_bGetFilename_Click_DSRTitle;
			sfd.OverwritePrompt = true;
			sfd.DefaultExt = "dsr";
			sfd.AddExtension = true;
            try
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                    tbFilename.Text = sfd.FileName;
            }
            finally
            {
                sfd.Dispose();
            }
		}

		private void validate_TextChanged(object sender, System.EventArgs e)
		{
			if (this.tbFilename.Text.Length > 0 &&
				this.tbPassword.Text.Length > 0 &&
				this.tbPassword.Text == this.tbPassword2.Text &&
				this.tbConnection.Text.Length > 0)
				bOK.Enabled = true;
			else
				bOK.Enabled = false;
			return;
		}

		private void bOK_Click(object sender, System.EventArgs e)
		{
			// Build the ConnectionProperties XML
			StringBuilder sb = new StringBuilder();
			sb.Append("<ConnectionProperties>");
			sb.AppendFormat("<DataProvider>{0}</DataProvider>", 
				this.cbDataProvider.Text);
			sb.AppendFormat("<ConnectString>{0}</ConnectString>", 
				this.tbConnection.Text.Replace("<", "&lt;"));
			sb.AppendFormat("<IntegratedSecurity>{0}</IntegratedSecurity>", 
				this.ckbIntSecurity.Checked? "true": "false");
			if (this.tbPrompt.Text.Length > 0)
				sb.AppendFormat("<Prompt>{0}</Prompt>",
					this.tbPrompt.Text.Replace("<", "&lt;"));
			sb.Append("</ConnectionProperties>");
			try
			{
				Rdl.DataSourceReference.Create(tbFilename.Text, sb.ToString(), tbPassword.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Strings.DialogDataSourceRef_bOK_Click_UnableCreateDSR);
				return;
			}

			DialogResult = DialogResult.OK;
		}

		private void bTestConnection_Click(object sender, System.EventArgs e)
		{
			if (DesignerUtility.TestConnection(this.cbDataProvider.Text, tbConnection.Text))
				MessageBox.Show(Strings.DialogDatabase_Show_ConnectionSuccessful, Strings.DesignerUtility_Show_TestConnection);

		}

		private void cbDataProvider_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (cbDataProvider.Text == "ODBC")
			{
				lODBC.Visible = cbOdbcNames.Visible = true;
				DesignerUtility.FillOdbcNames(cbOdbcNames);
			}
			else
			{
				lODBC.Visible = cbOdbcNames.Visible = false;
			}

		}

		private void cbOdbcNames_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			tbConnection.Text = "dsn=" + cbOdbcNames.Text + ";";
		}
	}
}
