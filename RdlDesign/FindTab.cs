
using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for FindTab.
    /// </summary>
    internal partial class FindTab 
    {
        public FindTab()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            this.AcceptButton = btnNext;
            this.CancelButton = btnCancel;
            txtFind.Focus();
        }

        internal FindTab(RdlEditPreview pad)
        {
            rdlEdit = pad;
			rdlEdit.FindTab = this;
            InitializeComponent();

            this.AcceptButton = btnNext;
            this.CancelButton = btnCancel;
            txtFind.Focus();
		}

        private void btnNext_Click(object sender, System.EventArgs e)
        {
            rdlEdit.FindNext(this, txtFind.Text, chkCase.Checked, radioUp.Checked);
        }

        private void txtFind_TextChanged(object sender, System.EventArgs e)
        {
            if (txtFind.Text == "")
                btnNext.Enabled = false;
            else
                btnNext.Enabled = true;
        }

        private void btnFindNext_Click(object sender, System.EventArgs e)
        {
            rdlEdit.FindNext(this, txtFindR.Text, chkMatchCase.Checked, false);
            txtFind.Focus();
        }

        private void btnReplace_Click(object sender, System.EventArgs e)
        {
            rdlEdit.ReplaceNext(this, txtFindR.Text, txtReplace.Text, chkCase.Checked);
            txtFindR.Focus();
        }

        private void txtFindR_TextChanged(object sender, System.EventArgs e)
        {
            bool bEnable = (txtFindR.Text == "") ? false : true;
            btnFindNext.Enabled = bEnable;
            btnReplace.Enabled = bEnable;
            btnReplaceAll.Enabled = bEnable;

        }

        private void btnReplaceAll_Click(object sender, System.EventArgs e)
        {

            rdlEdit.ReplaceAll(this, txtFindR.Text, txtReplace.Text, chkMatchCase.Checked);
            txtFindR.Focus();
        }

        private void btnGoto_Click(object sender, System.EventArgs e)
        {
            try
            {
                try
                {
                    int nLine = Int32.Parse(txtLine.Text);
                    rdlEdit.Goto(this, nLine);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, Strings.FindTab_ShowE_InvalidLN);
                }

                txtLine.Focus();

            }
            catch (Exception er)
            {
                er.ToString();
            }
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void tcFRG_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            TabControl tc = (TabControl)sender;
            string tag = (string)tc.TabPages[tc.SelectedIndex].Tag;
            switch (tag)
            {
                case "find":
                    this.AcceptButton = btnNext;
                    this.CancelButton = btnCancel;
                    txtFind.Focus();
                    break;
                case "replace":
                    this.AcceptButton = this.btnFindNext;
                    this.CancelButton = this.bCloseReplace;
                    txtFindR.Focus();
                    break;
                case "goto":
                    this.AcceptButton = btnGoto;
                    this.CancelButton = this.bCloseGoto;
                    txtLine.Focus();
                    break;
            }
        }

        private void tcFRG_Enter(object sender, System.EventArgs e)
        {
            tcFRG_SelectedIndexChanged(this.tcFRG, new EventArgs());
        }

		private void FindTab_FormClosed(object sender, FormClosedEventArgs e)
		{
			rdlEdit.ClearSearchHighlight();
		}

		public void FindNextClick()
		{
			if (!String.IsNullOrEmpty(txtFind.Text))
				btnNext.PerformClick();
		}
    }
}
