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
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using Majorsilence.Forms;
using System.Xml;

namespace fyiReporting.RdlDesign
{
	/// <summary>
	/// Summary description for ChartCtl.
	/// </summary>
	internal class ChartCtl : Majorsilence.Forms.UserControl, IProperty
	{
        private List<XmlNode> _ReportItems;
		private DesignXmlDraw _Draw;
		//AJM GJL 14082008 Adding more flags
		bool fChartType, fVector, fSubtype, fPalette, fRenderElement, fPercentWidth;
		bool fNoRows, fDataSet, fPageBreakStart, fPageBreakEnd;
		bool fChartData;
		private Majorsilence.Forms.Label label1;
		private Majorsilence.Forms.Label label2;
		private Majorsilence.Forms.Label label3;
		private Majorsilence.Forms.Label label4;
		private Majorsilence.Forms.ComboBox cbChartType;
		private Majorsilence.Forms.ComboBox cbSubType;
		private Majorsilence.Forms.ComboBox cbPalette;
		private Majorsilence.Forms.ComboBox cbRenderElement;
		private Majorsilence.Forms.Label label5;
		private Majorsilence.Forms.NumericUpDown tbPercentWidth;
		private Majorsilence.Forms.Label label6;
		private Majorsilence.Forms.TextBox tbNoRows;
		private Majorsilence.Forms.Label label7;
		private Majorsilence.Forms.ComboBox cbDataSet;
		private Majorsilence.Forms.CheckBox chkPageBreakStart;
		private Majorsilence.Forms.CheckBox chkPageBreakEnd;
        private Majorsilence.Forms.ComboBox cbChartData;
        private ComboBox cbDataLabel;
        private CheckBox chkDataLabel;
        private Button bDataLabelExpr;
		private Majorsilence.Forms.Label lData1;
        private ComboBox cbChartData2;
        private Label lData2;
        private ComboBox cbChartData3;
        private Label lData3;
        private Button bDataExpr;
        private Button bDataExpr3;
        private Button bDataExpr2;
        private ComboBox cbVector;
        private Button btnVectorExp;
        private Label label8;
        private Button button1;
        private Button button2;
        private Button button3;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal ChartCtl(DesignXmlDraw dxDraw, List<XmlNode> ris)
		{
			_ReportItems = ris;
			_Draw = dxDraw;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Initialize form using the style node values
			InitValues();			
		}

		private void InitValues()
		{
			XmlNode node = _ReportItems[0];

            string type = _Draw.GetElementValue(node, "Type", "Column");
            this.cbChartType.Text = type;
            type = type.ToLowerInvariant();

            lData2.Enabled = cbChartData2.Enabled = bDataExpr2.Enabled = (type == "scatter" || type == "bubble");
            lData3.Enabled = cbChartData3.Enabled = bDataExpr3.Enabled = (type == "bubble");
			//AJM GJL 14082008
            this.cbVector.Text = _Draw.GetElementValue(node, "fyi:RenderAsVector", "False");
			this.cbSubType.Text = _Draw.GetElementValue(node, "Subtype", "Plain");
			this.cbPalette.Text = _Draw.GetElementValue(node, "Palette", "Default");
			this.cbRenderElement.Text = _Draw.GetElementValue(node, "ChartElementOutput", "Output");
			this.tbPercentWidth.Text = _Draw.GetElementValue(node, "PointWidth", "0");
			this.tbNoRows.Text = _Draw.GetElementValue(node, "NoRows", "");
			// Handle the dataset for this dataregion
			object[] dsNames = _Draw.DataSetNames;
			string defName="";
			if (dsNames != null && dsNames.Length > 0)
			{
				this.cbDataSet.Items.AddRange(_Draw.DataSetNames);
				defName = (string) dsNames[0];
			}
			cbDataSet.Text = _Draw.GetDataSetNameValue(node);
			if (_Draw.GetReportItemDataRegionContainer(node) != null)
				cbDataSet.Enabled = false;
			// page breaks
			this.chkPageBreakStart.Checked = _Draw.GetElementValue(node, "PageBreakAtStart", "false").ToLower() == "true"? true: false;
			this.chkPageBreakEnd.Checked = _Draw.GetElementValue(node, "PageBreakAtEnd", "false").ToLower() == "true"? true: false;

			// Chart data-- this is a simplification of what is possible (TODO) 
			string cdata=string.Empty;
            string cdata2 = string.Empty;
            string cdata3 = string.Empty;
//        <ChartData>
//          <ChartSeries>
//            <DataPoints>
//              <DataPoint>
            //<DataValues>
            //      <DataValue>
            //        <Value>=Sum(Fields!Sales.Value)</Value>
            //      </DataValue>
            //      <DataValue>
            //        <Value>=Fields!Year.Value</Value>         ----- only scatter and bubble
            //      </DataValue>
            //      <DataValue>
            //        <Value>=Sum(Fields!Sales.Value)</Value>   ----- only bubble
            //      </DataValue>
            //    </DataValues>
//                <DataLabel>
//                  <Style>
//                    <Format>c</Format>
//                  </Style>
//                </DataLabel>
//                <Marker />
//              </DataPoint>
//            </DataPoints>
//          </ChartSeries>
//        </ChartData>

          
            //Determine if we have a static series or not... We are not allowing this to be changed here. That decision is taken when creating the chart. 05122007GJL
            XmlNode ss = DesignXmlDraw.FindNextInHierarchy(node, "SeriesGroupings", "SeriesGrouping", "StaticSeries");
            bool StaticSeries = ss != null;    
                                  
            XmlNode dvs = DesignXmlDraw.FindNextInHierarchy(node,
                "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataValues");
            int iter = 0;
            XmlNode cnode;
            foreach (XmlNode dv in dvs.ChildNodes)
            {
                if (dv.Name != "DataValue")
                    continue;
                iter++;
                cnode = DesignXmlDraw.FindNextInHierarchy(dv, "Value");
                if (cnode == null)
                    continue;
                switch (iter)
                {
                    case 1:
                        cdata = cnode.InnerText;
                        break;
                    case 2:
                        cdata2 = cnode.InnerText;
                        break;
                    case 3:
                        cdata3 = cnode.InnerText;
                        break;
                    default:
                        break;
                }
            }
			this.cbChartData.Text = cdata;
            this.cbChartData2.Text = cdata2;
            this.cbChartData3.Text = cdata3;
 
            //If the chart doesn't have a static series then dont show the datalabel values. 05122007GJL
            if (!StaticSeries) 
            {     
                //GJL 131107 Added data labels
                XmlNode labelExprNode = DesignXmlDraw.FindNextInHierarchy(node,
                    "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataLabel", "Value");
                if (labelExprNode != null)
                    this.cbDataLabel.Text = labelExprNode.InnerText;
                XmlNode labelVisNode = DesignXmlDraw.FindNextInHierarchy(node,
                    "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataLabel", "Visible");
                if (labelVisNode != null)
                    this.chkDataLabel.Checked = labelVisNode.InnerText.ToUpper().Equals("TRUE");
            }

            chkDataLabel.Enabled = bDataLabelExpr.Enabled = cbDataLabel.Enabled = 
                bDataExpr.Enabled = cbChartData.Enabled = !StaticSeries; 
            // Don't allow the datalabel OR datavalues to be changed here if we have a static series GJL


			fChartType = fSubtype = fPalette = fRenderElement = fPercentWidth =
				fNoRows = fDataSet = fPageBreakStart = fPageBreakEnd = fChartData = false;           
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label1 = new Majorsilence.Forms.Label();
            this.label2 = new Majorsilence.Forms.Label();
            this.label3 = new Majorsilence.Forms.Label();
            this.label4 = new Majorsilence.Forms.Label();
            this.cbChartType = new Majorsilence.Forms.ComboBox();
            this.cbSubType = new Majorsilence.Forms.ComboBox();
            this.cbPalette = new Majorsilence.Forms.ComboBox();
            this.cbRenderElement = new Majorsilence.Forms.ComboBox();
            this.label5 = new Majorsilence.Forms.Label();
            this.tbPercentWidth = new Majorsilence.Forms.NumericUpDown();
            this.label6 = new Majorsilence.Forms.Label();
            this.tbNoRows = new Majorsilence.Forms.TextBox();
            this.label7 = new Majorsilence.Forms.Label();
            this.cbDataSet = new Majorsilence.Forms.ComboBox();
            this.chkPageBreakStart = new Majorsilence.Forms.CheckBox();
            this.chkPageBreakEnd = new Majorsilence.Forms.CheckBox();
            this.cbChartData = new Majorsilence.Forms.ComboBox();
            this.cbDataLabel = new Majorsilence.Forms.ComboBox();
            this.chkDataLabel = new Majorsilence.Forms.CheckBox();
            this.bDataLabelExpr = new Majorsilence.Forms.Button();
            this.lData1 = new Majorsilence.Forms.Label();
            this.cbChartData2 = new Majorsilence.Forms.ComboBox();
            this.lData2 = new Majorsilence.Forms.Label();
            this.cbChartData3 = new Majorsilence.Forms.ComboBox();
            this.lData3 = new Majorsilence.Forms.Label();
            this.bDataExpr = new Majorsilence.Forms.Button();
            this.bDataExpr3 = new Majorsilence.Forms.Button();
            this.bDataExpr2 = new Majorsilence.Forms.Button();
            this.cbVector = new Majorsilence.Forms.ComboBox();
            this.btnVectorExp = new Majorsilence.Forms.Button();
            this.label8 = new Majorsilence.Forms.Label();
            this.button1 = new Majorsilence.Forms.Button();
            this.button2 = new Majorsilence.Forms.Button();
            this.button3 = new Majorsilence.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.tbPercentWidth)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(16, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Chart Type";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(16, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 16);
            this.label2.TabIndex = 1;
            this.label2.Text = "Palette";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(16, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(128, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "Render XML Element";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(16, 84);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(340, 19);
            this.label4.TabIndex = 3;
            this.label4.Text = "Percent width for Bars/Columns (>100% will cause overlap of columns)";
            // 
            // cbChartType
            // 
            this.cbChartType.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cbChartType.Items.AddRange(new object[] {
            "Area",
            "Bar",
            "Column",
            "Doughnut",
            "Line",
            "Map",
            "Pie",
            "Bubble",
            "Scatter"});
            this.cbChartType.Location = new System.Drawing.Point(126, 9);
            this.cbChartType.Name = "cbChartType";
            this.cbChartType.Size = new System.Drawing.Size(121, 21);
            this.cbChartType.TabIndex = 0;
            this.cbChartType.SelectedIndexChanged += this.cbChartType_SelectedIndexChanged;
            // 
            // cbSubType
            // 
            this.cbSubType.Location = new System.Drawing.Point(331, 9);
            this.cbSubType.Name = "cbSubType";
            this.cbSubType.Size = new System.Drawing.Size(80, 21);
            this.cbSubType.TabIndex = 1;
            this.cbSubType.SelectedIndexChanged += this.cbSubType_SelectedIndexChanged;
            this.cbSubType.TextUpdate += this.cbSubType_SelectedIndexChanged;
            this.cbSubType.TextChanged += this.cbSubType_SelectedIndexChanged;
            // 
            // cbPalette
            // 
            this.cbPalette.Items.AddRange(new object[] {
            "Default",
            "EarthTones",
            "Excel",
            "GrayScale",
            "Light",
            "Pastel",
            "SemiTransparent",
            "Patterned",
            "PatternedBlack"});
            this.cbPalette.Location = new System.Drawing.Point(126, 34);
            this.cbPalette.Name = "cbPalette";
            this.cbPalette.Size = new System.Drawing.Size(121, 21);
            this.cbPalette.TabIndex = 2;
            this.cbPalette.SelectedIndexChanged += this.cbPalette_SelectedIndexChanged;
            // 
            // cbRenderElement
            // 
            this.cbRenderElement.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cbRenderElement.Items.AddRange(new object[] {
            "Output",
            "NoOutput"});
            this.cbRenderElement.Location = new System.Drawing.Point(126, 59);
            this.cbRenderElement.Name = "cbRenderElement";
            this.cbRenderElement.Size = new System.Drawing.Size(121, 21);
            this.cbRenderElement.TabIndex = 3;
            this.cbRenderElement.SelectedIndexChanged += this.cbRenderElement_SelectedIndexChanged;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(275, 12);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 23);
            this.label5.TabIndex = 8;
            this.label5.Text = "Sub-type";
            // 
            // tbPercentWidth
            // 
            this.tbPercentWidth.Location = new System.Drawing.Point(363, 80);
            this.tbPercentWidth.Name = "tbPercentWidth";
            this.tbPercentWidth.Size = new System.Drawing.Size(48, 20);
            this.tbPercentWidth.TabIndex = 4;
            this.tbPercentWidth.ValueChanged += this.tbPercentWidth_ValueChanged;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(16, 106);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 23);
            this.label6.TabIndex = 10;
            this.label6.Text = "No Rows Message";
            // 
            // tbNoRows
            // 
            this.tbNoRows.Location = new System.Drawing.Point(156, 106);
            this.tbNoRows.Name = "tbNoRows";
            this.tbNoRows.Size = new System.Drawing.Size(255, 20);
            this.tbNoRows.TabIndex = 5;
            this.tbNoRows.TextChanged += this.tbNoRows_TextChanged;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(16, 135);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 23);
            this.label7.TabIndex = 12;
            this.label7.Text = "Data Set Name";
            // 
            // cbDataSet
            // 
            this.cbDataSet.DropDownStyle = Majorsilence.Forms.ComboBoxStyle.DropDownList;
            this.cbDataSet.Location = new System.Drawing.Point(156, 133);
            this.cbDataSet.Name = "cbDataSet";
            this.cbDataSet.Size = new System.Drawing.Size(255, 21);
            this.cbDataSet.TabIndex = 6;
            this.cbDataSet.SelectedIndexChanged += this.cbDataSet_SelectedIndexChanged;
            // 
            // chkPageBreakStart
            // 
            this.chkPageBreakStart.Location = new System.Drawing.Point(20, 260);
            this.chkPageBreakStart.Name = "chkPageBreakStart";
            this.chkPageBreakStart.Size = new System.Drawing.Size(136, 24);
            this.chkPageBreakStart.TabIndex = 13;
            this.chkPageBreakStart.Text = "Page Break at Start";
            this.chkPageBreakStart.CheckedChanged += this.chkPageBreakStart_CheckedChanged;
            // 
            // chkPageBreakEnd
            // 
            this.chkPageBreakEnd.Location = new System.Drawing.Point(280, 260);
            this.chkPageBreakEnd.Name = "chkPageBreakEnd";
            this.chkPageBreakEnd.Size = new System.Drawing.Size(136, 24);
            this.chkPageBreakEnd.TabIndex = 14;
            this.chkPageBreakEnd.Text = "Page Break at End";
            this.chkPageBreakEnd.CheckedChanged += this.chkPageBreakEnd_CheckedChanged;
            // 
            // cbChartData
            // 
            this.cbChartData.Location = new System.Drawing.Point(156, 157);
            this.cbChartData.Name = "cbChartData";
            this.cbChartData.Size = new System.Drawing.Size(255, 21);
            this.cbChartData.TabIndex = 7;
            this.cbChartData.TextChanged += this.cbChartData_Changed;
            // 
            // cbDataLabel
            // 
            this.cbDataLabel.Enabled = false;
            this.cbDataLabel.Location = new System.Drawing.Point(156, 237);
            this.cbDataLabel.Name = "cbDataLabel";
            this.cbDataLabel.Size = new System.Drawing.Size(254, 21);
            this.cbDataLabel.TabIndex = 17;
            this.cbDataLabel.TextChanged += this.cbChartData_Changed;
            // 
            // chkDataLabel
            // 
            this.chkDataLabel.AutoSize = true;
            this.chkDataLabel.Location = new System.Drawing.Point(20, 240);
            this.chkDataLabel.Name = "chkDataLabel";
            this.chkDataLabel.Size = new System.Drawing.Size(75, 17);
            this.chkDataLabel.TabIndex = 19;
            this.chkDataLabel.Text = "DataLabel";
            this.chkDataLabel.UseVisualStyleBackColor = true;
            this.chkDataLabel.CheckedChanged += this.chkDataLabel_CheckedChanged;
            // 
            // bDataLabelExpr
            // 
            this.bDataLabelExpr.Enabled = false;
            this.bDataLabelExpr.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))));
            this.bDataLabelExpr.Location = new System.Drawing.Point(415, 237);
            this.bDataLabelExpr.Name = "bDataLabelExpr";
            this.bDataLabelExpr.Size = new System.Drawing.Size(22, 21);
            this.bDataLabelExpr.TabIndex = 20;
            this.bDataLabelExpr.Text = "fx";
            this.bDataLabelExpr.UseVisualStyleBackColor = true;
            this.bDataLabelExpr.Click += this.bDataLabelExpr_Click;
            // 
            // lData1
            // 
            this.lData1.Location = new System.Drawing.Point(16, 157);
            this.lData1.Name = "lData1";
            this.lData1.Size = new System.Drawing.Size(137, 23);
            this.lData1.TabIndex = 16;
            this.lData1.Text = "Chart Data (X Coordinate)";
            // 
            // cbChartData2
            // 
            this.cbChartData2.Location = new System.Drawing.Point(156, 182);
            this.cbChartData2.Name = "cbChartData2";
            this.cbChartData2.Size = new System.Drawing.Size(255, 21);
            this.cbChartData2.TabIndex = 9;
            this.cbChartData2.TextChanged += this.cbChartData_Changed;
            // 
            // lData2
            // 
            this.lData2.Location = new System.Drawing.Point(16, 182);
            this.lData2.Name = "lData2";
            this.lData2.Size = new System.Drawing.Size(100, 23);
            this.lData2.TabIndex = 18;
            this.lData2.Text = "Y Coordinate";
            // 
            // cbChartData3
            // 
            this.cbChartData3.Location = new System.Drawing.Point(156, 207);
            this.cbChartData3.Name = "cbChartData3";
            this.cbChartData3.Size = new System.Drawing.Size(255, 21);
            this.cbChartData3.TabIndex = 11;
            this.cbChartData3.TextChanged += this.cbChartData_Changed;
            // 
            // lData3
            // 
            this.lData3.Location = new System.Drawing.Point(16, 207);
            this.lData3.Name = "lData3";
            this.lData3.Size = new System.Drawing.Size(100, 23);
            this.lData3.TabIndex = 20;
            this.lData3.Text = "Bubble Size";
            // 
            // bDataExpr
            // 
            this.bDataExpr.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bDataExpr.Location = new System.Drawing.Point(415, 157);
            this.bDataExpr.Name = "bDataExpr";
            this.bDataExpr.Size = new System.Drawing.Size(22, 21);
            this.bDataExpr.TabIndex = 8;
            this.bDataExpr.Tag = "d1";
            this.bDataExpr.Text = "fx";
            this.bDataExpr.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.bDataExpr.Click += this.bDataExpr_Click;
            // 
            // bDataExpr3
            // 
            this.bDataExpr3.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bDataExpr3.Location = new System.Drawing.Point(415, 207);
            this.bDataExpr3.Name = "bDataExpr3";
            this.bDataExpr3.Size = new System.Drawing.Size(22, 21);
            this.bDataExpr3.TabIndex = 12;
            this.bDataExpr3.Tag = "d3";
            this.bDataExpr3.Text = "fx";
            this.bDataExpr3.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.bDataExpr3.Click += this.bDataExpr_Click;
            // 
            // bDataExpr2
            // 
            this.bDataExpr2.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bDataExpr2.Location = new System.Drawing.Point(415, 182);
            this.bDataExpr2.Name = "bDataExpr2";
            this.bDataExpr2.Size = new System.Drawing.Size(22, 21);
            this.bDataExpr2.TabIndex = 10;
            this.bDataExpr2.Tag = "d2";
            this.bDataExpr2.Text = "fx";
            this.bDataExpr2.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.bDataExpr2.Click += this.bDataExpr_Click;
            // 
            // cbVector
            // 
            this.cbVector.Items.AddRange(new object[] {
            "False",
            "True"});
            this.cbVector.Location = new System.Drawing.Point(330, 36);
            this.cbVector.Name = "cbVector";
            this.cbVector.Size = new System.Drawing.Size(80, 21);
            this.cbVector.TabIndex = 21;
            this.cbVector.SelectedIndexChanged += this.cbVector_SelectedIndexChanged;
            // 
            // btnVectorExp
            // 
            this.btnVectorExp.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnVectorExp.Location = new System.Drawing.Point(415, 36);
            this.btnVectorExp.Name = "btnVectorExp";
            this.btnVectorExp.Size = new System.Drawing.Size(22, 21);
            this.btnVectorExp.TabIndex = 22;
            this.btnVectorExp.Tag = "d4";
            this.btnVectorExp.Text = "fx";
            this.btnVectorExp.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.btnVectorExp.Click += this.bDataExpr_Click;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(275, 32);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 27);
            this.label8.TabIndex = 23;
            this.label8.Text = "Render As Vector";
            // 
            // button1
            // 
            this.button1.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(415, 9);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(22, 21);
            this.button1.TabIndex = 24;
            this.button1.Tag = "d7";
            this.button1.Text = "fx";
            this.button1.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.button1.Click += this.bDataExpr_Click;
            // 
            // button2
            // 
            this.button2.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(251, 9);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(22, 21);
            this.button2.TabIndex = 25;
            this.button2.Tag = "d5";
            this.button2.Text = "fx";
            this.button2.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.button2.Visible = false;
            this.button2.Click += this.bDataExpr_Click;
            // 
            // button3
            // 
            this.button3.Font = new Majorsilence.Forms.Drawing.Font("Arial", 8.25F, ((Majorsilence.Forms.Drawing.FontStyle)((Majorsilence.Forms.Drawing.FontStyle.Bold | Majorsilence.Forms.Drawing.FontStyle.Italic))), Majorsilence.Forms.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.Location = new System.Drawing.Point(251, 32);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(22, 21);
            this.button3.TabIndex = 26;
            this.button3.Tag = "d6";
            this.button3.Text = "fx";
            this.button3.TextAlign = Majorsilence.Forms.ContentAlignment.MiddleLeft;
            this.button3.Click += this.bDataExpr_Click;
            // 
            // ChartCtl
            // 
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.btnVectorExp);
            this.Controls.Add(this.cbVector);
            this.Controls.Add(this.bDataExpr2);
            this.Controls.Add(this.bDataExpr3);
            this.Controls.Add(this.bDataExpr);
            this.Controls.Add(this.cbChartData3);
            this.Controls.Add(this.lData3);
            this.Controls.Add(this.cbChartData2);
            this.Controls.Add(this.lData2);
            this.Controls.Add(this.bDataLabelExpr);
            this.Controls.Add(this.chkDataLabel);
            this.Controls.Add(this.cbDataLabel);
            this.Controls.Add(this.cbChartData);
            this.Controls.Add(this.lData1);
            this.Controls.Add(this.chkPageBreakEnd);
            this.Controls.Add(this.chkPageBreakStart);
            this.Controls.Add(this.cbDataSet);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tbNoRows);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tbPercentWidth);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cbRenderElement);
            this.Controls.Add(this.cbPalette);
            this.Controls.Add(this.cbSubType);
            this.Controls.Add(this.cbChartType);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ChartCtl";
            this.Size = new System.Drawing.Size(440, 288);
            ((System.ComponentModel.ISupportInitialize)(this.tbPercentWidth)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

		public bool IsValid()
		{
			return true;
		}

		public void Apply()
		{
			// take information in control and apply to all the style nodes
			//  Only change information that has been marked as modified;
			//   this way when group is selected it is possible to change just
			//   the items you want and keep the rest the same.
				
			foreach (XmlNode riNode in this._ReportItems)
				ApplyChanges(riNode);

			// No more changes
			//AJM GJL 14082008
			fChartType = fVector = fSubtype = fPalette = fRenderElement = fPercentWidth =
				fNoRows = fDataSet = fPageBreakStart = fPageBreakEnd = fChartData = false;
		}

		public void ApplyChanges(XmlNode node)
		{
			if (fChartType)
			{
				_Draw.SetElement(node, "Type", this.cbChartType.Text);
			}
            if (fVector) //AJM GJL 14082008
            {
                _Draw.SetElement(node, "fyi:RenderAsVector", this.cbVector.Text);
            }
			if (fSubtype)
			{
				_Draw.SetElement(node, "Subtype", this.cbSubType.Text);
			}
			if (fPalette)
			{
				_Draw.SetElement(node, "Palette", this.cbPalette.Text);
			}
			if (fRenderElement)
			{
				_Draw.SetElement(node, "ChartElementOutput", this.cbRenderElement.Text);
			}
			if (fPercentWidth)
			{
				_Draw.SetElement(node, "PointWidth", this.tbPercentWidth.Text);
			}
			if (fNoRows)
			{
				_Draw.SetElement(node, "NoRows", this.tbNoRows.Text);
			}
			if (fDataSet)
			{
				_Draw.SetElement(node, "DataSetName", this.cbDataSet.Text);
			}
			if (fPageBreakStart)
			{
				_Draw.SetElement(node, "PageBreakAtStart", this.chkPageBreakStart.Checked? "true": "false");
			}
			if (fPageBreakEnd)
			{
				_Draw.SetElement(node, "PageBreakAtEnd", this.chkPageBreakEnd.Checked? "true": "false");
			}
			if (fChartData)
			{
				//        <ChartData>
				//          <ChartSeries>
				//            <DataPoints>
				//              <DataPoint>
				//                <DataValues>
				//                  <DataValue>
				//                    <Value>=Sum(Fields!Sales.Value)</Value>
				//                  </DataValue>   --- you can have up to 3 DataValue elements
				//                </DataValues>
				//                <DataLabel>
				//                  <Style>
				//                    <Format>c</Format>
				//                  </Style>
				//                </DataLabel>
				//                <Marker />
				//              </DataPoint>
				//            </DataPoints>
				//          </ChartSeries>
				//        </ChartData>
				XmlNode chartdata = _Draw.SetElement(node, "ChartData", null);
				XmlNode chartseries = _Draw.SetElement(chartdata, "ChartSeries", null);
				XmlNode datapoints = _Draw.SetElement(chartseries, "DataPoints", null);
				XmlNode datapoint = _Draw.SetElement(datapoints, "DataPoint", null);
				XmlNode datavalues = _Draw.SetElement(datapoint, "DataValues", null);
                _Draw.RemoveElementAll(datavalues, "DataValue");
                XmlNode datalabel = _Draw.SetElement(datapoint, "DataLabel", null);
				XmlNode datavalue = _Draw.SetElement(datavalues, "DataValue", null);
				_Draw.SetElement(datavalue, "Value", this.cbChartData.Text);

                string type = cbChartType.Text.ToLowerInvariant();
                if (type == "scatter" || type == "bubble")
                {
                    datavalue = _Draw.CreateElement(datavalues, "DataValue", null);
                    _Draw.SetElement(datavalue, "Value", this.cbChartData2.Text);
                    if (type == "bubble")
                    {
                        datavalue = _Draw.CreateElement(datavalues, "DataValue", null);
                        _Draw.SetElement(datavalue, "Value", this.cbChartData3.Text);
                    }
                }
                _Draw.SetElement(datalabel, "Value", this.cbDataLabel.Text);
                _Draw.SetElement(datalabel, "Visible", this.chkDataLabel.Checked.ToString());
			}
		}

		private void cbChartType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fChartType = true;
			// Change the potential sub-types
			string savesub = cbSubType.Text;
			string[] subItems = new string [] {"Plain"};
            bool bEnableY = false;
            bool bEnableBubble = false;
			switch (cbChartType.Text)
			{
				case "Column":
					subItems = new string [] {"Plain", "Stacked", "PercentStacked"};
					break;
				case "Bar":
					subItems = new string [] {"Plain", "Stacked", "PercentStacked"};
					break;
				case "Line":
					subItems = new string [] {"Plain", "Smooth"};
					break;
				case "Pie":
					subItems = new string [] {"Plain", "Exploded"};
					break;
				case "Area":
					subItems = new string [] {"Plain", "Stacked"};
					break;
				case "Doughnut":
					break;
                case "Map":
                    subItems = RdlDesigner.MapSubtypes;
                    break;
				case "Scatter":
					subItems = new string [] {"Plain", "Line", "SmoothLine"};
                    bEnableY = true;
                    break;
				case "Stock":
                    break;
				case "Bubble":
                    bEnableY = bEnableBubble = true;
                    break;
				default:
					break;
			}
			cbSubType.Items.Clear();
			cbSubType.Items.AddRange(subItems);

            lData2.Enabled = cbChartData2.Enabled = bDataExpr2.Enabled = bEnableY;
            lData3.Enabled = cbChartData3.Enabled = bDataExpr3.Enabled = bEnableBubble;
            
            int i=0;
			foreach (string s in subItems)
			{
				if (s == savesub)
				{
					cbSubType.SelectedIndex = i;
					return;
				}
				i++;
			}
			// Didn't match old style
			cbSubType.SelectedIndex = 0;
		}

		//AJM GJL 14082008
        private void cbVector_SelectedIndexChanged(object sender, EventArgs e)
        {
            fVector = true;
        }

		private void cbSubType_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fSubtype = true;
		}

		private void cbPalette_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fPalette = true;
		}

		private void cbRenderElement_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fRenderElement = true;
		}

		private void tbPercentWidth_ValueChanged(object sender, System.EventArgs e)
		{
			fPercentWidth = true;
		}

		private void tbNoRows_TextChanged(object sender, System.EventArgs e)
		{
			fNoRows = true;
		}

		private void cbDataSet_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			fDataSet = true;
		}

		private void chkPageBreakStart_CheckedChanged(object sender, System.EventArgs e)
		{
			fPageBreakStart = true;
		}

		private void chkPageBreakEnd_CheckedChanged(object sender, System.EventArgs e)
		{
			fPageBreakEnd = true;
		}

		private void cbChartData_Changed(object sender, System.EventArgs e)
		{
			fChartData = true;		
		}
        private void bDataExpr_Click(object sender, System.EventArgs e)
        {
            Button bs = sender as Button;
            if (bs == null)
                return;
            Control ctl = null; 
            switch (bs.Tag as string)
            {
                case "d1":
                    ctl = cbChartData;
                    break;
                case "d2":
                    ctl = cbChartData2;
                    break;
                case "d3":
                    ctl = cbChartData3;
                    break;
				//AJM GJL 14082008
                case "d4":
                    ctl = cbVector;
                    fVector = true;
                    break;
                case "d5":
                    ctl = cbChartType;
                    fChartType = true;
                    break;
                case "d6":
                    ctl = cbPalette;
                    fPalette = true;
                    break;
                case "d7":
                    ctl = cbSubType;
                    fSubtype = true;
                    break;
                default:
                    return;
            }
            DialogExprEditor ee = new DialogExprEditor(_Draw, ctl.Text, _ReportItems[0], false);
            try
            {
                DialogResult dlgr = ee.ShowDialog();
                if (dlgr == DialogResult.OK)
                {
                    ctl.Text = ee.Expression;
                    fChartData = true;
                }
            }
            finally
            {
                ee.Dispose();
            }
        }

        private void chkDataLabel_CheckedChanged(object sender, EventArgs e)
        {
            cbDataLabel.Enabled = bDataLabelExpr.Enabled = chkDataLabel.Checked; 
        }

        private void bDataLabelExpr_Click(object sender, EventArgs e)
        {
            DialogExprEditor ee = new DialogExprEditor(_Draw, cbDataLabel.Text,_ReportItems[0] , false);
            try
            {
                if (ee.ShowDialog() == DialogResult.OK)
                {                 
                    cbDataLabel.Text = ee.Expression;
                }

            }
            finally
            {
                ee.Dispose();
            }
            return;
        }

	}
}
