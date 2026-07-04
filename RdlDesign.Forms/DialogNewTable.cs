using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
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
    internal partial class DialogNewTable 
    {
        public DialogNewTable(DesignXmlDraw dxDraw, XmlNode container)
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
            rbHorz.Checked = true;
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

        internal string TableXml
        {
            get
            {
                return rbHorz.Checked ? TableXmlHorz : TableXmlVert;
            }
        }

        private string TableXmlHorz
        {
            get
            {
                StringBuilder table = new StringBuilder("<Table>");
                table.AppendFormat("<DataSetName>{0}</DataSetName>", this.cbDataSets.Text);
                table.Append("<NoRows>Query returned no rows!</NoRows><Style>" +
                    "<BorderStyle><Default>Solid</Default></BorderStyle></Style>");

                StringBuilder tablecolumns = new StringBuilder("<TableColumns>");

                StringBuilder headercolumns =
                    new StringBuilder("<Header><TableRows><TableRow><Height>12 pt</Height><TableCells>");

                StringBuilder detailcolumns =
                    new StringBuilder("<Details><TableRows><TableRow><Height>12 pt</Height><TableCells>");

                StringBuilder tablegroups = null;
                StringBuilder footergroup = null;
                string gname = this.cbGroupColumn.Text;
                if (gname != null && gname.Trim() != "")
                {
                    gname = gname.Trim();
                    tablegroups =
                        new StringBuilder("<TableGroups><TableGroup><Grouping><GroupExpressions><GroupExpression>");
                    tablegroups.AppendFormat("=Fields!{0}.Value</GroupExpression></GroupExpressions></Grouping>", gname);
                    tablegroups.Append("<Header><TableRows><TableRow><Height>12 pt</Height><TableCells>");
                    footergroup =
                        new StringBuilder("<Footer><TableRows><TableRow><Height>12 pt</Height><TableCells>");
                }
                else
                    gname = null;

                StringBuilder footercolumns = null;
                if (this.chkGrandTotals.Checked)
                    footercolumns =
                        new StringBuilder("<Footer><TableRows><TableRow><Height>12 pt</Height><TableCells>");

                bool bHaveFooter = false;		// indicates one or more columns have been checked for subtotaling
                foreach (string colname in this.lbTableColumns.Items)
                {
                    tablecolumns.Append("<TableColumn><Width>1in</Width></TableColumn>");
                    headercolumns.AppendFormat("<TableCell><ReportItems><Textbox><Value>{0}</Value>" +
                      "<Style><TextAlign>Center</TextAlign><BorderStyle><Default>Solid</Default></BorderStyle>" +
                      "<FontWeight>Bold</FontWeight></Style>" +
                        "</Textbox></ReportItems></TableCell>", colname);
                    string dcol;
                    string gcol;
                    if (gname == colname)
                    {
                        dcol = "";
                        gcol = string.Format("=Fields!{0}.Value", colname);
                    }
                    else
                    {
                        gcol = "";
                        dcol = string.Format("=Fields!{0}.Value", colname);
                    }
                    int iChecked = this.lbTableColumns.CheckedItems.IndexOf(colname);
                    string fcol = "";
                    if (iChecked >= 0)
                    {
                        bHaveFooter = true;
                        fcol = string.Format("=Sum(Fields!{0}.Value)", colname);
                    }
                    if (tablegroups != null)
                    {
                        tablegroups.AppendFormat("<TableCell><ReportItems><Textbox>" +
                            "<Value>{0}</Value><CanGrow>true</CanGrow>" +
                            "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                            "</Style></Textbox></ReportItems></TableCell>", gcol);
                        footergroup.AppendFormat("<TableCell><ReportItems><Textbox>" +
                            "<Value>{0}</Value><CanGrow>true</CanGrow>" +
                            "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                            "</Style></Textbox></ReportItems></TableCell>", fcol);
                    }
                    detailcolumns.AppendFormat("<TableCell><ReportItems><Textbox>" +
                      "<Value>{0}</Value><CanGrow>true</CanGrow>" +
                      "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                      "</Style></Textbox></ReportItems></TableCell>", dcol);
                    if (footercolumns != null)
                        footercolumns.AppendFormat("<TableCell><ReportItems><Textbox>" +
                            "<Value>{0}</Value><CanGrow>true</CanGrow>" +
                            "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                            "</Style></Textbox></ReportItems></TableCell>", fcol);
                }
                tablecolumns.Append("</TableColumns>");
                table.Append(tablecolumns.ToString());
                headercolumns.Append("</TableCells></TableRow></TableRows>" +
                    "<RepeatOnNewPage>true</RepeatOnNewPage></Header>");
                table.Append(headercolumns.ToString());
                detailcolumns.Append("</TableCells></TableRow></TableRows>" +
                    "</Details>");
                table.Append(detailcolumns.ToString());
                if (footercolumns != null)
                {
                    footercolumns.Append("</TableCells></TableRow></TableRows>" +
                        "</Footer>");
                    table.Append(footercolumns.ToString());
                }
                if (tablegroups != null)
                {
                    tablegroups.Append("</TableCells></TableRow></TableRows>" +
                        "</Header>");
                    if (bHaveFooter)
                    {
                        footergroup.Append("</TableCells></TableRow></TableRows>" +
                            "</Footer>");
                        tablegroups.Append(footergroup.ToString());
                    }
                    tablegroups.Append("</TableGroup></TableGroups>");
                    table.Append(tablegroups);
                }
                table.Append("</Table>");

                return table.ToString();
            }
        }

        private string TableXmlVert
        {
            get
            {
                StringBuilder table = new StringBuilder("<Table>");
                table.AppendFormat("<DataSetName>{0}</DataSetName>", this.cbDataSets.Text);
                table.Append("<NoRows>Query returned no rows!</NoRows><Style>" +
                    "<BorderStyle><Default>Solid</Default></BorderStyle></Style>");

                table.Append("<TableColumns><TableColumn><Width>5in</Width></TableColumn></TableColumns>");

                table.Append("<Details><TableRows>" + Environment.NewLine);

                foreach (string colname in this.lbTableColumns.Items)
                {
                    string dcol = string.Format("Fields!{0}.Value", colname);

                    if (this.rbVertComp.Checked)
                    {
                        string val = String.Format("<Value>=\"&lt;span style='color:Crimson;'&gt;{0}:&amp;nbsp;&amp;nbsp;&lt;/span&gt;\" &amp; {1}</Value>", colname, dcol);
                        table.AppendFormat(
                            "<TableRow><Height>12 pt</Height>" +
                            "<Visibility><Hidden>=Iif({1} = Nothing, true, false)</Hidden></Visibility>" +
                            "<TableCells><TableCell><ReportItems><Textbox>" +
                            "{0}" +
                            "<CanGrow>true</CanGrow>" +
                            "<Style><BorderStyle><Default>None</Default></BorderStyle>" +
                            "<Format>html</Format>" +
                            "</Style></Textbox></ReportItems></TableCell>" +
                            "</TableCells></TableRow>" +
                            Environment.NewLine, val, dcol);
                    }
                    else
                    {
                        table.AppendFormat(
                            "<TableRow><Height>12 pt</Height><TableCells>" +
                            "<TableCell><ReportItems><Textbox>" +
                            "<Value>{0}</Value>" +
                            "<Style><BorderStyle><Default>None</Default></BorderStyle>" +
                            "<FontWeight>Bold</FontWeight>" +
                            "<Color>Crimson</Color>" +
                            "</Style></Textbox></ReportItems></TableCell>" +
                            "</TableCells></TableRow>", colname);

                        table.AppendFormat(
                            "<TableRow><Height>12 pt</Height><TableCells>" +
                            "<TableCell><ReportItems><Textbox>" +
                            "<Value>={0}</Value><CanGrow>true</CanGrow>" +
                            "<Style><BorderStyle><Default>None</Default></BorderStyle>" +
                            "</Style></Textbox></ReportItems></TableCell>" +
                            "</TableCells></TableRow>", dcol);
                    }
                }
                table.Append("</TableRows></Details></Table>");

                return table.ToString();
            }
        }

        public void Apply()
        {
            //
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            // apply the result
            Apply();
            DialogResult = DialogResult.OK;
        }

        private void cbDataSets_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.lbTableColumns.Items.Clear();
            bOK.Enabled = false;
            this.lbFields.Items.Clear();
            string[] fields = _Draw.GetFields(cbDataSets.Text, false);
            if (fields != null)
                lbFields.Items.AddRange(fields);
        }

        private void bRight_Click(object sender, System.EventArgs e)
        {
            ListBox.SelectedIndexCollection sic = lbFields.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbFields.Items[i];
                lbTableColumns.Items.Add(fname);
            }
            // Need to remove backwards
            ArrayList ar = new ArrayList(sic);
            ar.Reverse();
            foreach (int i in ar)
            {
                lbFields.Items.RemoveAt(i);
            }
            bOK.Enabled = lbTableColumns.Items.Count > 0;
            if (count > 0 && lbFields.Items.Count > 0)
                lbFields.SelectedIndex = 0;
        }

        private void bLeft_Click(object sender, System.EventArgs e)
        {
            ICollection sic = lbTableColumns.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbTableColumns.Items[i];
                lbFields.Items.Add(fname);
                if (fname == this.cbGroupColumn.Text)
                    this.cbGroupColumn.Text = "";
            }
            // Need to remove backwards
            ArrayList ar = new ArrayList(sic);
            ar.Reverse();
            foreach (int i in ar)
            {
                lbTableColumns.Items.RemoveAt(i);
            }
            bOK.Enabled = lbTableColumns.Items.Count > 0;
            if (count > 0 && lbTableColumns.Items.Count > 0)
                lbTableColumns.SelectedIndex = 0;
        }

        private void bAllRight_Click(object sender, System.EventArgs e)
        {
            foreach (object fname in lbFields.Items)
            {
                lbTableColumns.Items.Add(fname);
            }
            lbFields.Items.Clear();
            bOK.Enabled = lbTableColumns.Items.Count > 0;
        }

        private void bAllLeft_Click(object sender, System.EventArgs e)
        {
            foreach (object fname in lbTableColumns.Items)
            {
                lbFields.Items.Add(fname);
            }
            lbTableColumns.Items.Clear();
            this.cbGroupColumn.Text = "";
            bOK.Enabled = false;
        }

        private void bUp_Click(object sender, System.EventArgs e)
        {
            int index = lbTableColumns.SelectedIndex;
            if (index <= 0)
                return;

            string prename = (string)lbTableColumns.Items[index - 1];
            lbTableColumns.Items.RemoveAt(index - 1);
            lbTableColumns.Items.Insert(index, prename);
        }

        private void bDown_Click(object sender, System.EventArgs e)
        {
            int index = lbTableColumns.SelectedIndex;
            if (index < 0 || index + 1 == lbTableColumns.Items.Count)
                return;

            string postname = (string)lbTableColumns.Items[index + 1];
            lbTableColumns.Items.RemoveAt(index + 1);
            lbTableColumns.Items.Insert(index, postname);
        }

        private void cbGroupColumn_Enter(object sender, System.EventArgs e)
        {
            cbGroupColumn.Items.Clear();
            cbGroupColumn.Items.Add("");
            if (lbTableColumns.Items.Count > 0)
            {
                object[] names = new object[lbTableColumns.Items.Count];
                lbTableColumns.Items.CopyTo(names, 0);
                cbGroupColumn.Items.AddRange(names);
            }
        }

        private void rbHorz_CheckedChanged(object sender, System.EventArgs e)
        {
            // only standard column report supports grouping and totals
            this.cbGroupColumn.Enabled = this.chkGrandTotals.Enabled = rbHorz.Checked;
        }
    }
}
