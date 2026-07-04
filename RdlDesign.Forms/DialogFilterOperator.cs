
using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Xml;
using System.Text;
using System.Reflection;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// DialogFilterOperator: puts up a dialog that lets a user pick a Filter Operator
    /// </summary>
    public partial class DialogFilterOperator 
    {

        internal DialogFilterOperator(string op)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            if (op != null && op.Length > 0)
                cbOperator.Text = op;

            return;
        }

        public string Operator
        {
            get { return this.cbOperator.Text; }
        }

        private void DialogFilterOperator_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (string op in cbOperator.Items)
            {
                if (op == cbOperator.Text)
                    return;
            }
            MessageBox.Show(string.Format(Strings.DialogFilterOperator_Show_OperatorInList, cbOperator.Text), Strings.DialogFilterOperator_Show_PickFilterOperator);
            e.Cancel = true;
        }

    }

}
