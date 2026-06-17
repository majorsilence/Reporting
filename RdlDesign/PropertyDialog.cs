
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using Majorsilence.Reporting.RdlDesign.Resources;

namespace Majorsilence.Reporting.RdlDesign
{
    internal enum PropertyTypeEnum
    {
        Report,
        DataSets,
        ReportItems,
        Grouping,
        ChartLegend,
        CategoryAxis,
        ValueAxis,
        ChartTitle,
        CategoryAxisTitle,
        ValueAxisTitle,
        TableGroup,
        ValueAxis2Title// 20022008 AJM GJL

    }

    /// <summary>
    /// Summary description for PropertyDialog.
    /// </summary>
    internal partial class PropertyDialog 
    {
        // design draw 
        private List<XmlNode> _Nodes;			// selected nodes
        private PropertyTypeEnum _Type;
        private bool _Changed = false;
        private bool _Delete = false;
        private XmlNode _TableColumn = null;	// when table this is the current table column
        private XmlNode _TableRow = null;		// when table this is the current table row
        private List<UserControl> _TabPanels = new List<UserControl>();		// list of IProperty controls

        internal PropertyDialog(DesignXmlDraw dxDraw, List<XmlNode> sNodes, PropertyTypeEnum type)
            : this(dxDraw, sNodes, type, null, null) { }

        internal PropertyDialog(DesignXmlDraw dxDraw, List<XmlNode> sNodes, PropertyTypeEnum type, XmlNode tcNode, XmlNode trNode)
        {
            _Draw = dxDraw;
            _Nodes = sNodes;
            _Type = type;
            _TableColumn = tcNode;
            _TableRow = trNode;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //   Add the controls for the selected ReportItems
            switch (_Type)
            {
                case PropertyTypeEnum.Report:
                    BuildReportTabs();
                    break;
                case PropertyTypeEnum.DataSets:
                    BuildDataSetsTabs();
                    break;
                case PropertyTypeEnum.Grouping:
                    BuildGroupingTabs();
                    break;
                case PropertyTypeEnum.ChartLegend:
                    BuildChartLegendTabs();
                    break;
                case PropertyTypeEnum.CategoryAxis:
                case PropertyTypeEnum.ValueAxis:
                    BuildChartAxisTabs(type);
                    break;
                case PropertyTypeEnum.ChartTitle:
                case PropertyTypeEnum.CategoryAxisTitle:
                case PropertyTypeEnum.ValueAxisTitle:
                case PropertyTypeEnum.ValueAxis2Title:// 20022008 AJM GJL
                    BuildTitle(type);
                    break;
                default:
                    BuildReportItemTabs();
                    break;
            }
        }

        internal bool Changed
        {
            get { return _Changed; }
        }

        internal bool Delete
        {
            get { return _Delete; }
        }

        private void BuildReportTabs()
        {
            Text = Strings.PropertyDialog_BuildReportTabs_ReportProperties;

            var rc = new ReportCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_Report, rc);

            var pc = new ReportParameterCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_Parameters, pc);

            var xc = new ReportXmlCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_XMLRendering, xc);

            var bc = new BodyCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_Body, bc);

            var cc = new CodeCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_Code, cc);

            var mc = new ModulesClassesCtl(_Draw);
            AddTab(Strings.PropertyDialog_BuildReportTabs_Modules_Classes, mc);
        }

        private void BuildDataSetsTabs()
        {
            bDelete.Visible = true;

            Text = Strings.PropertyDialog_BuildDataSetsTabs_DataSet_Header;

            XmlNode aNode;
            if (_Nodes != null && _Nodes.Count > 0)
                aNode = _Nodes[0];
            else
                aNode = null;

            var dsc = new DataSetsCtl(_Draw, aNode);
            AddTab(Strings.PropertyDialog_BuildDataSetsTabs_DataSet, dsc);

            var qp = new QueryParametersCtl(_Draw, dsc.DSV);
            AddTab(Strings.PropertyDialog_BuildDataSetsTabs_QueryParameters, qp);

            var fc = new FiltersCtl(_Draw, aNode);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fc);

            var dsrc = new DataSetRowsCtl(_Draw, aNode, dsc.DSV);
            AddTab(Strings.PropertyDialog_BuildDataSetsTabs_Data, dsrc);
        }

        private void BuildGroupingTabs()
        {
            var aNode = _Nodes[0];

            if (aNode.Name == "DynamicSeries")
            {
                Text = Strings.PropertyDialog_BuildGroupingTabs_SeriesGrouping;
            }
            else if (aNode.Name == "DynamicCategories")
            {
                Text = Strings.PropertyDialog_BuildGroupingTabs_CategoryGrouping;
            }
            else
            {
                Text = Strings.PropertyDialog_BuildGroupingTabs_GroupingAndSorting;
            }

            var gc = new GroupingCtl(_Draw, aNode);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Grouping, gc);

            var sc = new SortingCtl(_Draw, aNode);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Sorting, sc);

            // We have to create a grouping here but will need to kill it if no definition follows it
            var gNode = _Draw.GetCreateNamedChildNode(aNode, "Grouping");

            var fc = new FiltersCtl(_Draw, gNode);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fc);
        }

        private void BuildReportItemTabs()
        {
            XmlNode aNode = _Nodes[0];

            // Determine if all nodes are the same type
            string type = aNode.Name;
            if (type == "CustomReportItem")
            {
                // For customReportItems we use the type that is a parameter
                string t = _Draw.GetElementValue(aNode, "Type", "");
                if (t.Length > 0)
                    type = t;
            }

            foreach (XmlNode pNode in this._Nodes)
            {
                // For customReportItems we use the type that is a parameter
                string t = pNode.Name;
                if (t == "CustomReportItem")
                {
                    t = _Draw.GetElementValue(aNode, "Type", "");
                    if (t.Length == 0)       // Shouldn't happen
                        t = pNode.Name;
                }
                if (t != type)
                    type = "";			// Not all nodes have the same type
            }

            EnsureStyle();	// Make sure we have Style nodes for all the report items

            if (_Nodes.Count > 1)
                Text = Strings.PropertyDialog_BuildReportItemTabs_GroupSelectionProperties;
            else
            {
                var name = _Draw.GetElementAttribute(aNode, "Name", "");
                Text = string.Format("{0} {1} " + Strings.PropertyDialog_BuildReportItemTabs_Properties, type.Replace("fyi:", ""), name);
            }

            // Create all the tabs
	        switch (type)
	        {
		        case "Textbox":
			        var stc = new StyleTextCtl(_Draw, _Nodes, this);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Text, stc);
			        break;

		        case "List":
			        var lc = new ListCtl(_Draw, _Nodes);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_List, lc);

			        if (_Nodes.Count == 1)
			        {
				        var l = _Nodes[0];
				        var fc = new FiltersCtl(_Draw, l);
				        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fc);
				        var srtc = new SortingCtl(_Draw, l);
				        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Sorting, srtc);
			        }
			        break;

		        case "Chart":
			        var cc = new ChartCtl(_Draw, _Nodes);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Chart, cc);

			        // 05122007 AJM & GJL Create a new StaticSeriesCtl tab
			        var ssc = new StaticSeriesCtl(_Draw, _Nodes);

			        if (ssc.ShowMe)
			        {
				        //If the chart has static series, then show the StaticSeriesCtl GJL
				        AddTab(Strings.PropertyDialog_BuildReportItemTabs_StaticSeries, ssc);
			        }

			        if (_Nodes.Count == 1)
			        {
				        var fc = new FiltersCtl(_Draw, _Nodes[0]);
						AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fc);
			        }
			        break;

		        case "Image":
			        var imgc = new ImageCtl(_Draw, _Nodes);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Image, imgc);
			        break;

				case "Table":
					var table = _Nodes[0];
					var tc = new TableCtl(_Draw, _Nodes);
					AddTab(Strings.PropertyDialog_BuildReportItemTabs_Table, tc);
				    var fct = new FiltersCtl(_Draw, table);
					AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fct);
			        var details = _Draw.GetNamedChildNode(table, "Details");

					if (details != null)
					{
						// if no details then we don't need details sorting
						var grpc = new GroupingCtl(_Draw, details);
						AddTab(Strings.PropertyDialog_BuildReportItemTabs_Grouping, grpc);
						var srtc = new SortingCtl(_Draw, details);
						AddTab(Strings.PropertyDialog_BuildReportItemTabs_Sorting, srtc);
					}

					if (_TableColumn != null)
					{
						var tcc = new TableColumnCtl(_Draw, _TableColumn);
						AddTab(Strings.PropertyDialog_BuildReportItemTabs_TableColumn, tcc);
					}

					if (_TableRow != null)
					{
						var trc = new TableRowCtl(_Draw, _TableRow);
						AddTab(Strings.PropertyDialog_BuildReportItemTabs_TableRow, trc);
					}
					break;

		        case "fyi:Grid":
			        var gc = new GridCtl(_Draw, this._Nodes);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Grid, gc);
			        break;

		        case "Matrix":
			        var matrix = _Nodes[0];
			        var mc = new MatrixCtl(_Draw, this._Nodes);
			        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Matrix, mc);
			        var fcm = new FiltersCtl(_Draw, matrix);
					AddTab(Strings.PropertyDialog_BuildReportItemTabs_Filters, fcm);
			        break;

		        case "Subreport":
			        if (_Nodes.Count == 1)
			        {
				        var subreport = _Nodes[0];
				        var src = new SubreportCtl(_Draw, subreport);
				        AddTab(Strings.PropertyDialog_BuildReportItemTabs_Subreport, src);
			        }
			        break;

				default:
			        if (aNode.Name == "CustomReportItem")
			        {
				        var cric = new CustomReportItemCtl(_Draw, _Nodes);
				        AddTab(type, cric);
			        }
			        break;
	        }

	        // Position tab
            var pc = new PositionCtl(_Draw, this._Nodes);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Name_Position, pc);

            // Border tab
            var bc = new StyleBorderCtl(_Draw, null, this._Nodes);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Border, bc);

            if (!(type == "Line" || type == "Subreport"))
            {
                // Style tab
                var sc = new StyleCtl(_Draw, this._Nodes);
                AddTab(Strings.PropertyDialog_BuildReportItemTabs_Style, sc);

                // Interactivity tab
                var ic = new InteractivityCtl(_Draw, this._Nodes);
                AddTab(Strings.PropertyDialog_BuildReportItemTabs_Interactivity, ic);
            }
        }

        private void BuildChartAxisTabs(PropertyTypeEnum type)
        {
            string propName;
            if (type == PropertyTypeEnum.CategoryAxis)
            {
                Text = Strings.PropertyDialog_BuildChartAxisTabs_ChartCategoryAxis;
                propName = "CategoryAxis";
            }
            else
            {
                Text = Strings.PropertyDialog_BuildChartAxisTabs_ChartValueAxis;
                propName = "ValueAxis";
            }

            XmlNode cNode = _Nodes[0];
            XmlNode aNode = _Draw.GetCreateNamedChildNode(cNode, propName);
            XmlNode axNode = _Draw.GetCreateNamedChildNode(aNode, "Axis");

            // Now we replace the node array with a new one containing only the legend
            _Nodes = new List<XmlNode>();
            _Nodes.Add(axNode);

            EnsureStyle();	// Make sure we have Style nodes

            // Chart Axis
            var cac = new ChartAxisCtl(_Draw, _Nodes);
            AddTab(Strings.PropertyDialog_BuildChartAxisTabs_Axis, cac);

            // Style Text
            var stc = new StyleTextCtl(_Draw, _Nodes, this);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Text, stc);

            // Border tab
            var bc = new StyleBorderCtl(_Draw, null, _Nodes);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Border, bc);

            // Style tab
            var sc = new StyleCtl(_Draw, _Nodes);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Style, sc);
        }

        private void BuildChartLegendTabs()
        {
            Text = Strings.PropertyDialog_BuildChartLegendTabs_ChartLegendProperties;

            XmlNode cNode = _Nodes[0];
            XmlNode lNode = _Draw.GetCreateNamedChildNode(cNode, "Legend");

            // Now we replace the node array with a new one containing only the legend
            _Nodes = new List<XmlNode>();
            _Nodes.Add(lNode);

            EnsureStyle();	// Make sure we have Style nodes

            // Chart Legend
            var clc = new ChartLegendCtl(_Draw, _Nodes);
            AddTab(Strings.PropertyDialog_BuildChartLegendTabs_Legend, clc);

            // Style Text
            var stc = new StyleTextCtl(_Draw, _Nodes, this);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Text, stc);

            // Border tab
            var bc = new StyleBorderCtl(_Draw, null, _Nodes);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Border, bc);

            // Style tab
            var sc = new StyleCtl(_Draw, _Nodes);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Style, sc);
        }

        private void BuildTitle(PropertyTypeEnum type)
        {
            XmlNode cNode = _Nodes[0];
            _Nodes = new List<XmlNode>();		// replace with a new one
            if (type == PropertyTypeEnum.ChartTitle)
            {
                Text = Strings.PropertyDialog_BuildTitle_ChartTitle;

                XmlNode lNode = _Draw.GetCreateNamedChildNode(cNode, "Title");
                _Nodes.Add(lNode);		// Working on the title		
            }
            else if (type == PropertyTypeEnum.CategoryAxisTitle)
            {
                Text = Strings.PropertyDialog_BuildTitle_CategoryAxisTitle;
                XmlNode caNode = _Draw.GetCreateNamedChildNode(cNode, "CategoryAxis");
                XmlNode aNode = _Draw.GetCreateNamedChildNode(caNode, "Axis");
                XmlNode tNode = _Draw.GetCreateNamedChildNode(aNode, "Title");
                _Nodes.Add(tNode);		// Working on the title		
            }
            // 20022008 AJM GJL
            else if (type == PropertyTypeEnum.ValueAxis2Title)
            {
                Text = Strings.PropertyDialog_BuildTitle_ValueAxisRightTitle;
                XmlNode caNode = _Draw.GetCreateNamedChildNode(cNode, "ValueAxis");
                XmlNode aNode = _Draw.GetCreateNamedChildNode(caNode, "Axis");
                XmlNode tNode = _Draw.GetCreateNamedChildNode(aNode, "fyi:Title2");
                _Nodes.Add(tNode);		// Working on the title		   
            }
            else
            {
                Text = Strings.PropertyDialog_BuildTitle_ValueAxisTitle;
                XmlNode caNode = _Draw.GetCreateNamedChildNode(cNode, "ValueAxis");
                XmlNode aNode = _Draw.GetCreateNamedChildNode(caNode, "Axis");
                XmlNode tNode = _Draw.GetCreateNamedChildNode(aNode, "Title");
                _Nodes.Add(tNode);		// Working on the title		
            }

            EnsureStyle();	// Make sure we have Style nodes

            // Style Text
            var stc = new StyleTextCtl(_Draw, _Nodes, this);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Text, stc);

            // Border tab
            var bc = new StyleBorderCtl(_Draw, null, _Nodes);
			AddTab(Strings.PropertyDialog_BuildReportItemTabs_Border, bc);

            // Style tab
            var sc = new StyleCtl(_Draw, _Nodes);
            AddTab(Strings.PropertyDialog_BuildReportItemTabs_Style, sc);
        }

        private void EnsureStyle()
        {
            // Make sure we have Style nodes for all the nodes
            foreach (XmlNode pNode in this._Nodes)
            {
                XmlNode stNode = _Draw.GetCreateNamedChildNode(pNode, "Style");
            }
            return;
        }

        private void AddTab(string name, UserControl uc)
        {
            // Style tab
            TabPage tp = new TabPage();
            tp.Location = new System.Drawing.Point(4, 22);
            tp.Name = name + "1";
            tp.Size = new System.Drawing.Size(552, 284);
            tp.TabIndex = 1;
            tp.Text = name;

            _TabPanels.Add(uc);
            tp.Controls.Add(uc);

            uc.Dock = System.Windows.Forms.DockStyle.Fill;
            uc.Location = new System.Drawing.Point(0, 0);
            uc.Name = name + "1";
            uc.Size = new System.Drawing.Size(552, 284);
            uc.TabIndex = 0;

            tcProps.Controls.Add(tp);
        }

        private void bApply_Click(object sender, System.EventArgs e)
        {
            if (!IsValid())
                return;

            this._Changed = true;
            foreach (IProperty ip in _TabPanels)
            {
                ip.Apply();
            }
            this._Draw.Invalidate();		// Force screen to redraw
        }

        private void bOK_Click(object sender, System.EventArgs e)
        {
            if (!IsValid())
                return;

            bApply_Click(sender, e);	// Apply does all the work
            this.DialogResult = DialogResult.OK;
        }

        private bool IsValid()
        {
            int index = 0;
            foreach (IProperty ip in _TabPanels)
            {
                if (!ip.IsValid())
                {
                    tcProps.SelectedIndex = index;
                    return false;
                }
                index++;
            }
            return true;
        }

        private void PropertyDialog_Closing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (_Type == PropertyTypeEnum.Grouping)
            {	// Need to check if grouping value is still required
                XmlNode aNode = _Nodes[0];

                // We have to create a grouping here but will need to kill it if no definition follows it
                XmlNode gNode = _Draw.GetNamedChildNode(aNode, "Grouping");
                if (gNode != null &&
                    _Draw.GetNamedChildNode(gNode, "GroupExpressions") == null)
                {	// Not a valid group if no GroupExpressions
                    aNode.RemoveChild(gNode);
                }
            }

        }

        private void bDelete_Click(object sender, System.EventArgs e)
        {
            if (MessageBox.Show(this,
                    Strings.PropertyDialog_ShowF_WantDeleteDataset,
                    Strings.PropertyDialog_ShowF_DataSet,
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _Delete = true;
                DialogResult = DialogResult.OK;
            }
        }
    }

    internal interface IProperty
    {
        void Apply();
        bool IsValid();
    }

}
