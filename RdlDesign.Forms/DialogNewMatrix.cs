using System;
using Majorsilence.Forms.Drawing;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using System.Xml;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogDataSourceRef.
    /// </summary>
    internal partial class DialogNewMatrix 
    {
        internal DialogNewMatrix(DesignXmlDraw dxDraw, XmlNode container)
        {
            _Draw = dxDraw;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            InitValues(container);
        }

        private void InitValues(XmlNode container)
        {
            this.bOK.Enabled = false;
            //
            // Obtain the existing DataSets info
            //
            object[] datasets = _Draw.DataSetNames;
            if (datasets == null)
                return;		// not much to do if no DataSets

            if (_Draw.IsDataRegion(container))
            {
                string s = _Draw.GetDataSetNameValue(container);
                if (s == null)
                    return;
                this.cbDataSets.Items.Add(s);
                this.cbDataSets.Enabled = false;
            }
            else
                this.cbDataSets.Items.AddRange(datasets);
            if (cbDataSets.Items.Count > 0)
                cbDataSets.SelectedIndex = 0;
        }

        internal string MatrixXml
        {
            get
            {
                StringBuilder matrix = new StringBuilder("<Matrix>");
                matrix.AppendFormat("<DataSetName>{0}</DataSetName>", this.cbDataSets.Text);
                matrix.Append("<NoRows>Query returned no rows!</NoRows><Style>" +
                    "<BorderStyle><Default>Solid</Default></BorderStyle></Style>");

                matrix.Append("<Corner><ReportItems><Textbox Name=\"Corner\"><Value>Corner</Value>" +
              "<Style><BorderStyle><Default>Solid</Default></BorderStyle><BorderWidth>" +
                  "<Left>1pt</Left><Right>1pt</Right><Top>1pt</Top><Bottom>1pt</Bottom>" +
                "</BorderWidth><FontWeight>bold</FontWeight></Style>" +
                "</Textbox></ReportItems></Corner>");
                // do the column groupings
                matrix.Append("<ColumnGroupings>");
                foreach (string cname in this.lbMatrixColumns.Items)
                {
                    matrix.Append("<ColumnGrouping><Height>12pt</Height>");
                    matrix.Append("<DynamicColumns>");
                    matrix.AppendFormat("<Grouping><GroupExpressions>" +
                        "<GroupExpression>=Fields!{0}.Value</GroupExpression>" +
                        "</GroupExpressions></Grouping>", cname);
                    matrix.AppendFormat("<ReportItems><Textbox>" +
                        "<Value>=Fields!{0}.Value</Value>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>" +
                        "</Textbox></ReportItems>", cname);
                    int iChecked = this.lbMatrixColumns.CheckedItems.IndexOf(cname);
                    if (iChecked >= 0)
                    {
                        matrix.AppendFormat("<Subtotal><ReportItems><Textbox>" +
                            "<Value>{0} Subtotal</Value>" +
                            "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>" +
                            "</Textbox></ReportItems></Subtotal>", cname);
                    }

                    matrix.Append("</DynamicColumns>");
                    matrix.Append("</ColumnGrouping>");
                }
                matrix.Append("</ColumnGroupings>");
                // do the row groupings
                matrix.Append("<RowGroupings>");
                foreach (string rname in this.lbMatrixRows.Items)
                {
                    matrix.Append("<RowGrouping><Width>1in</Width>");
                    matrix.Append("<DynamicRows>");
                    matrix.AppendFormat("<Grouping><GroupExpressions>" +
                        "<GroupExpression>=Fields!{0}.Value</GroupExpression>" +
                        "</GroupExpressions></Grouping>", rname);
                    matrix.AppendFormat("<ReportItems><Textbox>" +
                        "<Value>=Fields!{0}.Value</Value>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>" +
                        "</Textbox></ReportItems>", rname);
                    int iChecked = this.lbMatrixRows.CheckedItems.IndexOf(rname);
                    if (iChecked >= 0)
                    {
                        matrix.AppendFormat("<Subtotal><ReportItems><Textbox>" +
                            "<Value>{0} Subtotal</Value>" +
                            "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style>" +
                            "</Textbox></ReportItems></Subtotal>", rname);
                    }

                    matrix.Append("</DynamicRows>");
                    matrix.Append("</RowGrouping>");
                }
                matrix.Append("</RowGroupings>");
                // Matrix Columns
                matrix.Append("<MatrixColumns><MatrixColumn><Width>1in</Width></MatrixColumn></MatrixColumns>");
                // Matrix Rows
                matrix.AppendFormat("<MatrixRows><MatrixRow><Height>12pt</Height>" +
                    "<MatrixCells><MatrixCell><ReportItems>" +
                    "<Textbox><Value>{0}</Value>" +
                    "<Style><BorderStyle><Default>Solid</Default></BorderStyle></Style></Textbox>" +
                    "</ReportItems></MatrixCell></MatrixCells>" +
                    "</MatrixRow></MatrixRows>", this.cbMatrixCell.Text);
                // end of matrix defintion
                matrix.Append("</Matrix>");

                return matrix.ToString();
            }
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            // apply the result
            DialogResult = DialogResult.OK;
        }

        private void cbDataSets_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.lbMatrixColumns.Items.Clear();
            this.lbMatrixRows.Items.Clear();
            bOK.Enabled = false;
            this.lbFields.Items.Clear();
            string[] fields = _Draw.GetFields(cbDataSets.Text, false);
            if (fields != null)
                lbFields.Items.AddRange(fields);
        }

        private void bColumn_Click(object sender, System.EventArgs e)
        {
            ICollection sic = lbFields.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbFields.Items[i];
                if (this.lbMatrixColumns.Items.IndexOf(fname) < 0)
                    lbMatrixColumns.Items.Add(fname);
            }
            OkEnable();
        }

        private void bRow_Click(object sender, System.EventArgs e)
        {
            ICollection sic = lbFields.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbFields.Items[i];
                if (this.lbMatrixRows.Items.IndexOf(fname) < 0)
                    lbMatrixRows.Items.Add(fname);
            }
            OkEnable();
        }

        private void bColumnUp_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixColumns.SelectedIndex;
            if (index <= 0)
                return;

            string prename = (string)lbMatrixColumns.Items[index - 1];
            lbMatrixColumns.Items.RemoveAt(index - 1);
            lbMatrixColumns.Items.Insert(index, prename);
        }

        private void bColumnDown_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixColumns.SelectedIndex;
            if (index < 0 || index + 1 == lbMatrixColumns.Items.Count)
                return;

            string postname = (string)lbMatrixColumns.Items[index + 1];
            lbMatrixColumns.Items.RemoveAt(index + 1);
            lbMatrixColumns.Items.Insert(index, postname);
        }

        private void bColumnDelete_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixColumns.SelectedIndex;
            if (index < 0)
                return;

            lbMatrixColumns.Items.RemoveAt(index);
            OkEnable();
        }

        private void bRowUp_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixRows.SelectedIndex;
            if (index <= 0)
                return;

            string prename = (string)lbMatrixRows.Items[index - 1];
            lbMatrixRows.Items.RemoveAt(index - 1);
            lbMatrixRows.Items.Insert(index, prename);
        }

        private void bRowDown_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixRows.SelectedIndex;
            if (index < 0 || index + 1 == lbMatrixRows.Items.Count)
                return;

            string postname = (string)lbMatrixRows.Items[index + 1];
            lbMatrixRows.Items.RemoveAt(index + 1);
            lbMatrixRows.Items.Insert(index, postname);
        }

        private void bRowDelete_Click(object sender, System.EventArgs e)
        {
            int index = lbMatrixRows.SelectedIndex;
            if (index < 0)
                return;

            lbMatrixRows.Items.RemoveAt(index);
            OkEnable();
        }

        private void OkEnable()
        {
            // We need values in datasets, rows, columns, and matrix cells for OK to work correctly
            bOK.Enabled = this.lbMatrixColumns.Items.Count > 0 &&
                          this.lbMatrixRows.Items.Count > 0 &&
                        this.cbMatrixCell.Text != null &&
                        this.cbMatrixCell.Text.Length > 0 &&
                        this.cbDataSets.Text != null &&
                        this.cbDataSets.Text.Length > 0;
        }

        private void cbMatrixCell_Enter(object sender, System.EventArgs e)
        {
            cbMatrixCell.Items.Clear();
            foreach (string field in this.lbFields.Items)
            {
                if (this.lbMatrixColumns.Items.IndexOf(field) >= 0 ||
                    this.lbMatrixRows.Items.IndexOf(field) >= 0)
                    continue;
                // Field selected in columns and rows
                this.cbMatrixCell.Items.Add(string.Format("=Sum(Fields!{0}.Value)", field));
            }
        }

        private void cbMatrixCell_TextChanged(object sender, System.EventArgs e)
        {
            OkEnable();
        }
    }
}
