/* ====================================================================
   Copyright (C) 2004-2008  fyiReporting Software, LLC
   Copyright (C) 2011  Peter Gill <peter@majorsilence.com>

   This file is part of the fyiReporting RDL project.
	
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


   For additional information, email info@fyireporting.com or visit
   the website www.fyiReporting.com.
*/
// written by GJL & AJM
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Majorsilence.Forms;
using System.Xml;

//This NEW class allows the modifying of static series on a chart. GJL & AJM


namespace fyiReporting.RdlDesign
{
    public partial class StaticSeriesCtl : UserControl, IProperty
     {
        private DesignXmlDraw _Draw;
        private List<XmlNode> _ReportItems;
        private SeriesItem si;
        public bool ShowMe;

        internal StaticSeriesCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
        {
            _Draw = dxDraw;
            _ReportItems = ris;
            InitializeComponent();
            InitValues();
        }                
        
        public StaticSeriesCtl()
        {
            InitializeComponent();
        }

        public bool IsValid()
        {
            return true;
        }

        public void Apply()
        {
                XmlNode node = _ReportItems[0];
                XmlNode ncds = DesignXmlDraw.FindNextInHierarchy(node, "SeriesGroupings", "SeriesGrouping","StaticSeries");
                XmlNode ncdc = DesignXmlDraw.FindNextInHierarchy(node, "ChartData");
                XmlNode nTyp = DesignXmlDraw.FindNextInHierarchy(node, "Type");              
            
                ncds.InnerText = "";
                ncdc.InnerText = "";
           
            foreach (SeriesItem si in lbDataSeries.Items)
            {              
                 //Write the staticMember fields 
                ncds.InnerXml += "<StaticMember><Label>" + si.Name + "</Label><Value>" + si.Data.Replace("<","&lt;").Replace(">","&gt;") + "</Value></StaticMember>";   
        
                 //Write the chartSeries fields 
                //if we have a scatter plot we need to do two datavalues!
                if (nTyp.InnerXml.Equals("Scatter"))
                {
                    ncdc.InnerXml += "<ChartSeries><PlotType>" + si.PlotType + "</PlotType><fyi:NoMarker xmlns:fyi=\"http://www.fyireporting.com/schemas\">" + si.NoMarker + "</fyi:NoMarker><fyi:LineSize xmlns:fyi=\"http://www.fyireporting.com/schemas\">" + si.LineSize + "</fyi:LineSize><YAxis>" + si.YAxis +
                        "</YAxis><DataPoints><DataPoint><DataValues><DataValue><Value>"
                        + si.Xplot.Replace("<", "&lt;").Replace(">", "&gt;") + "</Value></DataValue><DataValue><Value>" // 20022008 AJM GJL
                        + si.Data.Replace("<", "&lt;").Replace(">", "&gt;") + "</Value></DataValue></DataValues><DataLabel><Value>" + si.Label.Replace("<", "&lt;").Replace(">", "&gt;") + "</Value><Visible>" + si.ShowLabel.ToString()
                        + "</Visible></DataLabel></DataPoint></DataPoints></ChartSeries>";
                }
                else
                {
                    ncdc.InnerXml += "<ChartSeries><PlotType>" + si.PlotType + "</PlotType><fyi:NoMarker xmlns:fyi=\"http://www.fyireporting.com/schemas\">" + si.NoMarker + "</fyi:NoMarker><fyi:LineSize xmlns:fyi=\"http://www.fyireporting.com/schemas\">" + si.LineSize + "</fyi:LineSize><YAxis>" + si.YAxis + "</YAxis><DataPoints><DataPoint><DataValues><DataValue><Value>" // 20022008 AJM GJL
                        + si.Data.Replace("<", "&lt;").Replace(">", "&gt;") + "</Value></DataValue></DataValues><DataLabel><Value>" + si.Label.Replace("<", "&lt;").Replace(">", "&gt;") + "</Value><Visible>" + si.ShowLabel.ToString()
                        + "</Visible></DataLabel></DataPoint></DataPoints></ChartSeries>";
                }
            }
        }

        private void FunctionButtonClick(Object sender, EventArgs e)     
        {
            Button myButton = (Button)sender;
            switch (myButton.Name)
            {
                case ("btnSeriesName"):
                    functionBox(txtSeriesName);
                    break;
                case ("btnDataValue"):
                    functionBox(txtDataValue);
                    break;
                case ("btnLabelValue"):
                    functionBox(txtLabelValue);
                    break;
                case ("btnX"):
                    functionBox(txtX);
                    break;
            }
        }
        
           private void functionBox(TextBox txt)
           {
                DialogExprEditor ee = new DialogExprEditor(_Draw, txt.Text,_ReportItems[0] , false);
                try
                {
                    if (ee.ShowDialog() == DialogResult.OK)
                    {                 
                        txt.Text = ee.Expression;
                    }

                }
            finally
            {
                ee.Dispose();
            }
            return;
           }

        private void chkShowLabels_CheckedChanged(object sender, EventArgs e)
        {
            txtLabelValue.Enabled = btnLabelValue.Enabled = chkShowLabels.Checked;
            if (lbDataSeries.SelectedIndex > -1)
            {              
                si.ShowLabel = chkShowLabels.Checked;
            }
        }
		//GJL
        private void chkMarker_CheckedChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {
                si.NoMarker = !chkMarker.Checked;
            }
        }

        private void cbLine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbLine.SelectedIndex > -1)
            {
                si.LineSize = (String)cbLine.Items[cbLine.SelectedIndex];
            }
        }

        

       

        private void InitValues()
        {
            XmlNode node = _ReportItems[0];
            XmlNode nTyp = DesignXmlDraw.FindNextInHierarchy(node, "Type");
            XmlNode ncds = DesignXmlDraw.FindNextInHierarchy(node, "SeriesGroupings","SeriesGrouping","StaticSeries");
            ShowMe = ncds != null;

            txtX.Enabled = btnX.Enabled = nTyp.InnerXml == "Scatter";

            if (ShowMe)
            {                
                XmlNode cd;

                XmlNode cdo = DesignXmlDraw.FindNextInHierarchy(node, "ChartData");
                int i = 0;

                foreach (XmlNode ncd in ncds.ChildNodes)
                {
                    cd = cdo.ChildNodes[i];
                    XmlNode ndv = DesignXmlDraw.FindNextInHierarchy(ncd,"Label");
                    String nameValue = ndv == null? "": ndv.InnerText;

                    XmlNode ndv2 = DesignXmlDraw.FindNextInHierarchy(ncd,"Value");
                    String nameStaticValue = ndv2 == null ? "" : ndv2.InnerText;

                    XmlNode pt = DesignXmlDraw.FindNextInHierarchy(cd, "PlotType");
                    String PlotValue;
                    if (pt == null) 
                    {
                        PlotValue = "Auto";
                    }
                    else
                    {
                        PlotValue = pt.InnerText;
                    }
					//GJL 14082008
                    XmlNode Nm = DesignXmlDraw.FindNextInHierarchy(cd, "fyi:NoMarker");
                    bool NoMarker;
                    if (Nm == null)
                    {
                        NoMarker = false;
                    }
                    else
                    {
                        NoMarker = Boolean.Parse(Nm.InnerText);
                    }

                    XmlNode Ls = DesignXmlDraw.FindNextInHierarchy(cd, "fyi:LineSize");
                    String LineSize;
                    if (Ls== null)
                    {
                        LineSize = "Regular";
                    }
                    else
                    {
                        LineSize = Ls.InnerText;
                    }

                    // 20022008 AJM GJL
                    XmlNode ya = DesignXmlDraw.FindNextInHierarchy(cd, "YAxis");
                    String Yaxis;
                    if (ya == null)
                    { 
                        Yaxis = "Left"; 
                    }
                    else
                    { 
                        Yaxis = ya.InnerText;
                    }

                    XmlNode dv = DesignXmlDraw.FindNextInHierarchy(cd, "DataPoints","DataPoint","DataValues","DataValue","Value");
                    String dataValue = dv.InnerText;

                    XmlNode lv = DesignXmlDraw.FindNextInHierarchy(cd, "DataPoints","DataPoint","DataLabel","Visible");
                    bool showLabel = false;
                    if (lv != null)
                    {
                        showLabel = lv.InnerText.ToUpper().Equals("TRUE");
                    }              

                    XmlNode lva = DesignXmlDraw.FindNextInHierarchy(cd, "DataPoints","DataPoint","DataLabel","Value");
                    String labelValue = "";
                    if (lva != null)
                    {
                        labelValue = lva.InnerText;
                    }               

                    SeriesItem si = new SeriesItem();
                    si.Name = nameValue;
                    si.Label = labelValue;
                    si.ShowLabel = showLabel;
                    si.Data = nameStaticValue;
                    si.PlotType = PlotValue;
                    si.YAxis = Yaxis;// 20022008 AJM GJL
                    si.Xplot = dataValue; //Only for XY plots
                    si.NoMarker = NoMarker;//0206208 GJL
                    si.LineSize = LineSize;


                    lbDataSeries.Items.Add(si);
                    
                    i++;
                }
            }  
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex >= 0)
            {
                lbDataSeries.Items.RemoveAt(lbDataSeries.SelectedIndex);
                txtDataValue.Text = "";
                txtLabelValue.Text = "";
                txtSeriesName.Text = "";
                chkMarker.Checked = true;
                chkShowLabels.Checked = false;
              
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            SeriesItem si = new SeriesItem();
            si.Data = "";
            si.Name = "<New Series>";
            si.ShowLabel = false;
            si.Label = "";
            si.PlotType = "Auto";
            si.NoMarker = false;
            si.LineSize = "Regular";
            si.YAxis = "Left";// 20022008 AJM GJL
            lbDataSeries.Items.Add(si);
            lbDataSeries.SelectedItem = si;
        }

        private class SeriesItem
        {
            public String Data;
            public String Name;
            public bool ShowLabel;
            public String Label;
            public String PlotType;
            public String YAxis;// 20022008 AJM GJL
            public String Xplot; //030308 GJL
            public bool NoMarker;//020608 GJL
            public String LineSize = "Regular"; //260608 GJL
            
            public override String ToString()
            {
                return Name;
            }
        }

        private void txtSeriesName_TextChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            { 
                
                si.Name = txtSeriesName.Text;
                int i = lbDataSeries.Items.IndexOf(si);
                lbDataSeries.Items.Remove(si);
                lbDataSeries.Items.Insert(i,si);
                lbDataSeries.SelectedItem = si;
            }
           
        }

        private void txtDataValue_TextChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {              
                si.Data = txtDataValue.Text;
            }
        }

        private void txtLabelValue_TextChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {              
                si.Label = txtLabelValue.Text;
            }
        }

        private void lbDataSeries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {
                si = (SeriesItem)lbDataSeries.SelectedItem;
                txtDataValue.Text = si.Data;
                txtLabelValue.Text = si.Label;
                txtSeriesName.Text = si.Name;
                chkShowLabels.Checked = si.ShowLabel;
                cbPlotType.Text = si.PlotType;
                chkMarker.Checked = !si.NoMarker;
                cbLine.Text = si.LineSize;
                if (txtX.Enabled)
                {
                    txtX.Text = si.Xplot;
                }
                if (si.YAxis == "Right")
                { 
                    chkRight.Checked = true; 
                }
                else
                { 
                    chkLeft.Checked = true; 
                }
            }                 
        }

        private void cbPlotType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {
                si.PlotType = cbPlotType.Text;
            }
        }
		// 20022008 AJM GJL
        private void chkLeft_CheckedChanged(object sender, EventArgs e)
        {
            chkRight.Checked = !chkLeft.Checked;
            if (lbDataSeries.SelectedIndex > -1)
            {              
                if (chkRight.Checked)
                { si.YAxis = "Right"; }
                else
                { si.YAxis = "Left"; }
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int index = lbDataSeries.SelectedIndex;
            if (index < 0 || index + 1 == lbDataSeries.Items.Count)
                return;

            object postname = lbDataSeries.Items[index + 1];
            lbDataSeries.Items.RemoveAt(index + 1);
            lbDataSeries.Items.Insert(index, postname);
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            int index = lbDataSeries.SelectedIndex;
            if (index <= 0)
                return;

            object prename = lbDataSeries.Items[index - 1];
            lbDataSeries.Items.RemoveAt(index - 1);
            lbDataSeries.Items.Insert(index, prename);
        }

        private void txtX_TextChanged(object sender, EventArgs e)
        {
            if (lbDataSeries.SelectedIndex > -1)
            {
                si.Xplot = txtX.Text;
            }
        }

      

      

     

       

       

       
    }
}
