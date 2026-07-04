using System;
using System.Collections;
using System.ComponentModel;
using Majorsilence.Forms;
using System.Text;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;


namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Summary description for DialogDataSourceRef.
    /// </summary>
    internal partial class DialogNewChart 
    {

        internal DialogNewChart(DesignXmlDraw dxDraw, XmlNode container)
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

            this.cbChartType.SelectedIndex = 2;
        }

        internal string ChartXml
        {
            get
            {
                StringBuilder chart = new StringBuilder("<Chart><Height>2in</Height><Width>4in</Width>");
                chart.AppendFormat("<DataSetName>{0}</DataSetName>", this.cbDataSets.Text);
                chart.Append("<NoRows>Query returned no rows!</NoRows><Style>" +
                    "<BorderStyle><Default>Solid</Default></BorderStyle>" +
                    "<BackgroundColor>White</BackgroundColor>" +
                    "<BackgroundGradientType>LeftRight</BackgroundGradientType>" +
                    "<BackgroundGradientEndColor>Azure</BackgroundGradientEndColor>" +
                    "</Style>");
                chart.AppendFormat("<Type>{0}</Type><Subtype>{1}</Subtype>",
                    this.cbChartType.Text, this.cbSubType.Text);
                // do the categories
                string tcat = "";
                if (this.lbChartCategories.Items.Count > 0)
                {
                    chart.Append("<CategoryGroupings>");
                    foreach (string cname in this.lbChartCategories.Items)
                    {
                        if (tcat == "")
                            tcat = cname;
                        chart.Append("<CategoryGrouping>");
                        chart.Append("<DynamicCategories>");
                        chart.AppendFormat("<Grouping><GroupExpressions>" +
                            "<GroupExpression>=Fields!{0}.Value</GroupExpression>" +
                            "</GroupExpressions></Grouping>", cname);

                        chart.Append("</DynamicCategories>");
                        chart.Append("</CategoryGrouping>");
                    }
                    chart.Append("</CategoryGroupings>");
                    // Do the category axis
                    chart.AppendFormat("<CategoryAxis><Axis><Visible>true</Visible>" +
                        "<MajorTickMarks>Inside</MajorTickMarks>" +
                        "<MajorGridLines><ShowGridLines>true</ShowGridLines>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                        "</Style></MajorGridLines>" +
                        "<MinorGridLines><ShowGridLines>true</ShowGridLines>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                        "</Style></MinorGridLines>" +
                        "<Title><Caption>{0}</Caption>" +
                        "</Title></Axis></CategoryAxis>", tcat);

                }
                // do the series
                string tser = "";
                if (this.lbChartSeries.Items.Count > 0)
                {
                    chart.Append("<SeriesGroupings>");
                    //If we have chartData Set then we want dynamic series GJL
                    if (this.cbChartData.Text.Length > 0)
                    {
                        foreach (string sname in this.lbChartSeries.Items)
                        {
                            if (tser == "")
                                tser = sname;
                            chart.Append("<SeriesGrouping>");
                            chart.Append("<DynamicSeries>");
                            chart.AppendFormat("<Grouping><GroupExpressions>" +
                                "<GroupExpression>=Fields!{0}.Value</GroupExpression>" +
                                "</GroupExpressions></Grouping>", sname);
                            chart.AppendFormat("<Label>=Fields!{0}.Value</Label>", sname);
                            chart.Append("</DynamicSeries>");
                            chart.Append("</SeriesGrouping>");
                        }
                    }
                    //If we don't have chart data set we want static series GJL
                    else
                    {
                        chart.Append("<SeriesGrouping>");
                        chart.Append("<StaticSeries>");
                        foreach (string sname in this.lbChartSeries.Items)
                        {
                            chart.Append("<StaticMember>");
                            chart.AppendFormat("<Label>{0}</Label>", sname);
                            chart.AppendFormat("<Value>=Fields!{0}.Value</Value>", sname);
                            chart.Append("</StaticMember>");

                        }

                        chart.Append("</StaticSeries>");
                        chart.Append("</SeriesGrouping>");

                    }
                    chart.Append("</SeriesGroupings>");
                }
                // Chart Data
                string vtitle;
                if (this.cbChartData.Text.Length > 0)
                {
                    chart.Append("<ChartData><ChartSeries><DataPoints><DataPoint>" +
                        "<DataValues>");
                    chart.AppendFormat("<DataValue><Value>{0}</Value></DataValue>",
                        this.cbChartData.Text);
                    string ctype = this.cbChartType.Text.ToLowerInvariant();

                    if (ctype == "scatter" || ctype == "bubble")
                    {
                        chart.AppendFormat("<DataValue><Value>{0}</Value></DataValue>",
                            this.cbChartData2.Text);
                        if (ctype == "bubble")
                        {
                            chart.AppendFormat("<DataValue><Value>{0}</Value></DataValue>",
                                this.cbChartData3.Text);
                        }
                    }
                    chart.Append("</DataValues>" +
                        "</DataPoint></DataPoints></ChartSeries></ChartData>");

                    // Do the value axis

                    int start = this.cbChartData.Text.LastIndexOf("!");
                    if (start > 0)
                    {
                        int end = this.cbChartData.Text.LastIndexOf(".Value");
                        if (end < 0 || end <= start + 1)
                            vtitle = this.cbChartData.Text.Substring(start + 1);
                        else
                            vtitle = this.cbChartData.Text.Substring(start + 1, end - start - 1);
                    }
                    else
                        vtitle = "Values";
                }
                else
                {
                    //If we don't have chartData then use the items in the series box
                    //to create Static Series
                    chart.Append("<ChartData>");
                    foreach (string sname in this.lbChartSeries.Items)
                    {
                        chart.Append("<ChartSeries>");
                        chart.Append("<DataPoints>");
                        chart.Append("<DataPoint>");
                        chart.Append("<DataValues>");

                        if (cbChartType.SelectedItem.Equals("Scatter"))
                        {
                            //we need a y datavalue as well...
                            string xname = (string)lbChartCategories.Items[0];
                            chart.Append("<DataValue>");
                            chart.AppendFormat("<Value>=Fields!{0}.Value</Value>", xname);
                            chart.Append("</DataValue>");
                            chart.Append("<DataValue>");
                            chart.AppendFormat("<Value>=Fields!{0}.Value</Value>", sname);
                            chart.Append("</DataValue>");
                        }
                        else
                        {
                            chart.Append("<DataValue>");
                            chart.AppendFormat("<Value>=Fields!{0}.Value</Value>", sname);
                            chart.Append("</DataValue>");
                        }
                        chart.Append("</DataValues>");
                        chart.Append("</DataPoint>");
                        chart.Append("</DataPoints>");
                        chart.Append("</ChartSeries>");
                    }
                    chart.Append("</ChartData>");
                    vtitle = "Values";

                }

                chart.AppendFormat("<ValueAxis><Axis><Visible>true</Visible>" +
                        "<MajorTickMarks>Inside</MajorTickMarks>" +
                        "<MajorGridLines><ShowGridLines>true</ShowGridLines>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                        "<FontSize>8pt</FontSize>" +
                        "</Style></MajorGridLines>" +
                        "<MinorGridLines><ShowGridLines>true</ShowGridLines>" +
                        "<Style><BorderStyle><Default>Solid</Default></BorderStyle>" +
                        "</Style></MinorGridLines>" +
                        "<Title><Caption>{0}</Caption>" +
                        "<Style><WritingMode>tb-rl</WritingMode></Style>" +
                        "</Title></Axis></ValueAxis>", vtitle);

                // Legend
                chart.Append("<Legend><Style><BorderStyle><Default>Solid</Default>" +
                    "</BorderStyle><PaddingLeft>5pt</PaddingLeft>" +
                    "<FontSize>8pt</FontSize></Style><Visible>true</Visible>" +
                    "<Position>RightCenter</Position></Legend>");

                // Title
                chart.AppendFormat("<Title><Style><FontWeight>Bold</FontWeight>" +
                    "<FontSize>14pt</FontSize><TextAlign>Center</TextAlign>" +
                    "</Style><Caption>{0} {1} Chart</Caption></Title>", tcat, tser);

                // end of Chart defintion
                chart.Append("</Chart>");

                return chart.ToString();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>


        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>

        #endregion

        private void bOK_Click(object sender, System.EventArgs e)
        {
            bool bFail = false;
            string ctype = cbChartType.Text.ToLowerInvariant();
            if (cbChartData.Text.Length == 0 && lbChartSeries.Items.Count == 0) //Added second condition 05122007GJL
            {
                MessageBox.Show(Strings.DialogNewChart_ShowC_FillExpression);
                bFail = true;
            }
            else if (ctype == "scatter" && cbChartData2.Text.Length == 0 && lbChartSeries.Items.Count == 0)
            {
                MessageBox.Show(Strings.DialogNewChart_ShowC_FillYExpression);
                bFail = true;

            }
            else if (ctype == "bubble" && (cbChartData2.Text.Length == 0 || cbChartData3.Text.Length == 0))
            {
                MessageBox.Show(Strings.DialogNewChart_ShowC_FillYAndBubbleExpressions);
                bFail = true;
            }
            if (bFail)
                return;
            // apply the result
            DialogResult = DialogResult.OK;
        }

        private void cbDataSets_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.lbChartCategories.Items.Clear();
            this.lbChartSeries.Items.Clear();
            bOK.Enabled = false;
            this.lbFields.Items.Clear();
            string[] fields = _Draw.GetFields(cbDataSets.Text, false);
            if (fields != null)
                lbFields.Items.AddRange(fields);
        }

        private void bCategory_Click(object sender, System.EventArgs e)
        {
            ICollection sic = lbFields.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbFields.Items[i];
                if (this.lbChartCategories.Items.IndexOf(fname) < 0)
                    lbChartCategories.Items.Add(fname);
            }
            OkEnable();
        }

        private void bSeries_Click(object sender, System.EventArgs e)
        {
            ICollection sic = lbFields.SelectedIndices;
            int count = sic.Count;
            foreach (int i in sic)
            {
                string fname = (string)lbFields.Items[i];
                if (this.lbChartSeries.Items.IndexOf(fname) < 0 || cbChartType.SelectedItem.Equals("Scatter"))
                    lbChartSeries.Items.Add(fname);
            }
            OkEnable();
        }

        private void bCategoryUp_Click(object sender, System.EventArgs e)
        {
            int index = lbChartCategories.SelectedIndex;
            if (index <= 0)
                return;

            string prename = (string)lbChartCategories.Items[index - 1];
            lbChartCategories.Items.RemoveAt(index - 1);
            lbChartCategories.Items.Insert(index, prename);
        }

        private void bCategoryDown_Click(object sender, System.EventArgs e)
        {
            int index = lbChartCategories.SelectedIndex;
            if (index < 0 || index + 1 == lbChartCategories.Items.Count)
                return;

            string postname = (string)lbChartCategories.Items[index + 1];
            lbChartCategories.Items.RemoveAt(index + 1);
            lbChartCategories.Items.Insert(index, postname);
        }

        private void bCategoryDelete_Click(object sender, System.EventArgs e)
        {
            int index = lbChartCategories.SelectedIndex;
            if (index < 0)
                return;

            lbChartCategories.Items.RemoveAt(index);
            OkEnable();
        }

        private void bSeriesUp_Click(object sender, System.EventArgs e)
        {
            int index = lbChartSeries.SelectedIndex;
            if (index <= 0)
                return;

            string prename = (string)lbChartSeries.Items[index - 1];
            lbChartSeries.Items.RemoveAt(index - 1);
            lbChartSeries.Items.Insert(index, prename);
        }

        private void bSeriesDown_Click(object sender, System.EventArgs e)
        {
            int index = lbChartSeries.SelectedIndex;
            if (index < 0 || index + 1 == lbChartSeries.Items.Count)
                return;

            string postname = (string)lbChartSeries.Items[index + 1];
            lbChartSeries.Items.RemoveAt(index + 1);
            lbChartSeries.Items.Insert(index, postname);
        }

        private void bSeriesDelete_Click(object sender, System.EventArgs e)
        {
            int index = lbChartSeries.SelectedIndex;
            if (index < 0)
                return;

            lbChartSeries.Items.RemoveAt(index);
            OkEnable();
        }

        private void OkEnable()
        {
            // We need values in datasets and Categories or Series for OK to work correctly
            bool bEnable = (this.lbChartCategories.Items.Count > 0 ||
                          this.lbChartSeries.Items.Count > 0) &&
                        this.cbDataSets.Text != null &&
                        this.cbDataSets.Text.Length > 0;
            // && this.cbChartData.Text.Length > 0; Not needed with static series 05122007GJL
            string ctype = cbChartType.Text.ToLowerInvariant();
            if (ctype == "scatter")
                bEnable = bEnable && (this.cbChartData2.Text.Length > 0 || lbChartSeries.Items.Count > 0);
            else if (ctype == "bubble")
                bEnable = bEnable && (this.cbChartData2.Text.Length > 0 && this.cbChartData3.Text.Length > 0);

            bOK.Enabled = bEnable;
        }

        private void cbChartData_Enter(object sender, System.EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null)
                return;
            cb.Items.Clear();
            foreach (string field in this.lbFields.Items)
            {
                if (this.lbChartCategories.Items.IndexOf(field) >= 0 ||
                    this.lbChartSeries.Items.IndexOf(field) >= 0)
                    continue;
                // Field selected in columns and rows
                cb.Items.Add(string.Format("=Sum(Fields!{0}.Value)", field));
            }
        }

        private void cbChartData_TextChanged(object sender, System.EventArgs e)
        {
            OkEnable();
        }

        private void cbChartType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            // Change the potential sub-types
            string savesub = cbSubType.Text;
            string[] subItems;
            bool bEnableData2 = false;
            bool bEnableData3 = false;
            switch (cbChartType.Text)
            {
                case "Column":
                    subItems = new string[] { "Plain", "Stacked", "PercentStacked" };
                    break;
                case "Bar":
                    subItems = new string[] { "Plain", "Stacked", "PercentStacked" };
                    break;
                case "Line":
                    subItems = new string[] { "Plain", "Smooth" };
                    break;
                case "Pie":
                    subItems = new string[] { "Plain", "Exploded" };
                    break;
                case "Area":
                    subItems = new string[] { "Plain", "Stacked" };
                    break;
                case "Doughnut":
                    subItems = new string[] { "Plain" };
                    break;
                case "Scatter":
                    subItems = new string[] { "Plain", "Line", "SmoothLine" };
                    bEnableData2 = true;
                    break;
                case "Bubble":
                    subItems = new string[] { "Plain" };
                    bEnableData2 = bEnableData3 = true;
                    break;
                case "Stock":
                default:
                    subItems = new string[] { "Plain" };
                    break;
            }
            lChartData2.Enabled = cbChartData2.Enabled = bEnableData2;
            lChartData3.Enabled = cbChartData3.Enabled = bEnableData3;

            // handle the subtype
            cbSubType.Items.Clear();
            cbSubType.Items.AddRange(subItems);
            int i = 0;
            foreach (string s in subItems)
            {
                if (s == savesub)
                {
                    cbSubType.SelectedIndex = i;
                    break;
                }
                i++;
            }
            // Didn't match old style
            if (i >= subItems.Length)
                i = 0;
            cbSubType.SelectedIndex = i;
        }


    }
}
