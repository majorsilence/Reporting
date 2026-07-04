using Majorsilence.Reporting.Rdl;

using System;
using System.Collections.Generic;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyChart - The Chart Properties
    /// </summary>
    internal class PropertyChart : PropertyDataRegion
    {
        public PropertyChart(DesignXmlDraw d, DesignCtl dc, List<XmlNode> ris) : base(d, dc, ris)
        {
        }

        [LocalizedCategory("Chart")]
        [TypeConverter(typeof(ChartTypeConverter))]
		[LocalizedDisplayName("Chart_Type")]
		[LocalizedDescription("Chart_Type")]
        public string Type
        {
            get { return this.GetValue("Type", "Column"); }
            set
            {
                this.SetValue("Type", value);
            }
        }

        [LocalizedCategory("Chart")]
        [TypeConverter(typeof(PaletteTypeConverter))]
		[LocalizedDisplayName("Chart_Palette")]
		[LocalizedDescription("Chart_Palette")]
        public string Palette
        {
            get { return this.GetValue("Palette", "Default"); }
            set
            {
                this.SetValue("Palette", value);
            }
        }

        [LocalizedCategory("Chart")]
		[LocalizedDisplayName("Chart_PointWidth")]
		[LocalizedDescription("Chart_PointWidth")]
        public int PointWidth
        {
            get 
            {
                int pw = 100;
                try
                {
                    string spw = this.GetValue("PointWidth", "100");
                    pw = Convert.ToInt32(spw);
                }
                catch { }
                return pw;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("PointWidth must be greater than 0.");
                this.SetValue("PointWidth", value.ToString());
            }
        }
 
        [LocalizedCategory("Chart")]
		[LocalizedDisplayName("Chart_ChartData")]
		[LocalizedDescription("Chart_ChartData")]
        public PropertyChartData ChartData
        {
            get { return new PropertyChartData(this);}
        }

        [LocalizedCategory("Chart")]
		[TypeConverter(typeof(ChartTitleTypeConverter))]
		[LocalizedDisplayName("Chart_Title")]
		[LocalizedDescription("Chart_Title")]
        public PropertyChartTitle Title
        {
            get { return new PropertyChartTitle(this); }
        }

        [LocalizedCategory("Chart")]
		[TypeConverter(typeof(ChartLegendTypeConverter))]
		[LocalizedDisplayName("Chart_Legend")]
		[LocalizedDescription("Chart_Legend")]
        public PropertyChartLegend Legend
        {
            get { return new PropertyChartLegend(this); }
        }

        [LocalizedCategory("Chart")]
		[TypeConverter(typeof(ChartAxisTypeConverter))]
		[LocalizedDisplayName("Chart_CategoryAxis")]
		[LocalizedDescription("Chart_CategoryAxis")]
        public PropertyChartAxis CategoryAxis
        {
            get { return new PropertyChartAxis(this, "CategoryAxis"); }
        }

		[LocalizedCategory("Chart")]
		[TypeConverter(typeof(ChartAxisTypeConverter))]
		[LocalizedDisplayName("Chart_ValueAxis")]
		[LocalizedDescription("Chart_ValueAxis")]
		public PropertyChartAxis ValueAxis
        {
            get { return new PropertyChartAxis(this, "ValueAxis"); }
        }
    }

    #region ChartAxis
    [TypeConverter(typeof(ChartAxisTypeConverter))]
	[LocalizedDescription("ChartAxis")]
    internal class PropertyChartAxis : IReportItem
    {
        PropertyChart _pt;
        string _Axis;
        internal PropertyChartAxis(PropertyChart pt, string axis)
        {
            _pt = pt;
            _Axis = axis;
        }

		[LocalizedDisplayName("ChartAxis_Visible")]
		[LocalizedDescription("ChartAxis_Visible")]
        public bool Visible
        {
            get
            {
                string v = _pt.GetWithList("true", _Axis, "Axis", "Visible");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "Visible");
            }
        }

		[LocalizedDisplayName("ChartAxis_Margin")]
		[LocalizedDescription("ChartAxis_Margin")]
        public bool Margin
        {
            get
            {
                string v = _pt.GetWithList("false", _Axis, "Axis", "Margin");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "Margin");
            }
        }

		[LocalizedDisplayName("ChartAxis_MajorTickMarks")]
		[LocalizedDescription("ChartAxis_MajorTickMarks")]
        public AxisTickMarksEnum MajorTickMarks
        {
            get
            {
                string v = _pt.GetWithList("None", _Axis, "Axis", "MajorTickMarks");
                return AxisTickMarks.GetStyle(v);
            }
            set
            {
                _pt.SetWithList(value.ToString(), _Axis, "Axis", "MajorTickMarks");
            }
        }

		[LocalizedDisplayName("ChartAxis_MinorTickMarks")]
		[LocalizedDescription("ChartAxis_MinorTickMarks")]
        public AxisTickMarksEnum MinorTickMarks
        {
            get
            {
                string v = _pt.GetWithList("None", _Axis, "Axis", "MinorTickMarks");
                return AxisTickMarks.GetStyle(v);
            }
            set
            {
                _pt.SetWithList(value.ToString(), _Axis, "Axis", "MinorTickMarks");
            }
        }

		[LocalizedDisplayName("ChartAxis_MajorInterval")]
		[LocalizedDescription("ChartAxis_MajorInterval")]
		public PropertyExpr MajorInterval
        {
            get
            {
                string v = _pt.GetWithList("", _Axis, "Axis", "MajorInterval");
                return new PropertyExpr(v);
            }
            set
            {
                _pt.SetWithList(value.Expression, _Axis, "Axis", "MajorInterval");
            }
        }

		[LocalizedDisplayName("ChartAxis_MinorInterval")]
		[LocalizedDescription("ChartAxis_MinorInterval")]
        public PropertyExpr MinorInterval
        {
            get
            {
                string v = _pt.GetWithList("", _Axis, "Axis", "MinorInterval");
                return new PropertyExpr(v);
            }
            set
            {
                _pt.SetWithList(value.Expression, _Axis, "Axis", "MinorInterval");
            }
        }

		[LocalizedDisplayName("ChartAxis_CrossAt")]
		[LocalizedDescription("ChartAxis_CrossAt")]
        public PropertyExpr CrossAt
        {
            get
            {
                string v = _pt.GetWithList("", _Axis, "Axis", "CrossAt");
                return new PropertyExpr(v);
            }
            set
            {
                _pt.SetWithList(value.Expression, _Axis, "Axis", "CrossAt");
            }
        }

		[LocalizedDisplayName("ChartAxis_Min")]
		[LocalizedDescription("ChartAxis_Min")]
        public PropertyExpr Min
        {
            get
            {
                string v = _pt.GetWithList("", _Axis, "Axis", "Min");
                return new PropertyExpr(v);
            }
            set
            {
                _pt.SetWithList(value.Expression, _Axis, "Axis", "Min");
            }
        }

		[LocalizedDisplayName("ChartAxis_Max")]
		[LocalizedDescription("ChartAxis_Max")]
        public PropertyExpr Max
        {
            get
            {
                string v = _pt.GetWithList("", _Axis, "Axis", "Max");
                return new PropertyExpr(v);
            }
            set
            {
                _pt.SetWithList(value.Expression, _Axis, "Axis", "Max");
            }
        }

		[LocalizedDisplayName("ChartAxis_Reverse")]
		[LocalizedDescription("ChartAxis_Reverse")]
        public bool Reverse
        {
            get
            {
                string v = _pt.GetWithList("false", _Axis, "Axis", "Reverse");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "Reverse");
            }
        }

		[LocalizedDisplayName("ChartAxis_Interlaced")]
		[LocalizedDescription("ChartAxis_Interlaced")]
        public bool Interlaced
        {
            get
            {
                string v = _pt.GetWithList("false", _Axis, "Axis", "Interlaced");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "Interlaced");
            }
        }

		[LocalizedDisplayName("ChartAxis_Scalar")]
		[LocalizedDescription("ChartAxis_Scalar")]
        public bool Scalar
        {
            get
            {
                string v = _pt.GetWithList("false", _Axis, "Axis", "Scalar");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "Scalar");
            }
        }

		[LocalizedDisplayName("ChartAxis_LogScale")]
		[LocalizedDescription("ChartAxis_LogScale")]
        public bool LogScale
        {
            get
            {
                string v = _pt.GetWithList("false", _Axis, "Axis", "LogScale");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", _Axis, "Axis", "LogScale");
            }
        }

		[LocalizedDisplayName("ChartAxis_Title")]
		[LocalizedDescription("ChartAxis_Title")]
        public PropertyChartTitle Title
        {
            get { return new PropertyChartTitle(_pt, _Axis, "Axis"); }
        }

		[LocalizedDisplayName("ChartAxis_Appearance")]
		[LocalizedDescription("ChartAxis_Appearance")]
        public PropertyAppearance Appearance
        {
            get { return new PropertyAppearance(_pt, _Axis, "Axis"); }
        }

		[LocalizedDisplayName("ChartAxis_Background")]
		[LocalizedDescription("ChartAxis_Background")]
        public PropertyBackground Background
        {
            get { return new PropertyBackground(_pt, _Axis, "Axis"); }
        }

		[LocalizedDisplayName("ChartAxis_Border")]
		[LocalizedDescription("ChartAxis_Border")]
        public PropertyBorder Border
        {
            get { return new PropertyBorder(_pt, _Axis, "Axis"); }
        }

		[LocalizedDisplayName("ChartAxis_Padding")]
		[LocalizedDescription("ChartAxis_Padding")]
        public PropertyPadding Padding
        {
            get { return new PropertyPadding(_pt, _Axis, "Axis"); }
        }

		[LocalizedDisplayName("ChartAxis_MajorGridLines")]
		[LocalizedDescription("ChartAxis_MajorGridLines")]
        public PropertyChartGridLines MajorGridLines
        {
            get { return new PropertyChartGridLines(_pt, _Axis, "Axis", "MajorGridLines"); }
        }

		[LocalizedDisplayName("ChartAxis_MinorGridLines")]
		[LocalizedDescription("ChartAxis_MinorGridLines")]
        public PropertyChartGridLines MinorGridLines
        {
            get { return new PropertyChartGridLines(_pt, _Axis, "Axis", "MinorGridLines"); }
        }

        public override string ToString()
        {
            if (!this.Visible)
                return "hidden";
            else
                return this.Title.Caption.Expression;
        }

        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pt;
        }

        #endregion
    }
    internal class ChartAxisTypeConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyChartAxis))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyChartAxis)
            {
                PropertyChartAxis pa = value as PropertyChartAxis;
                return pa.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }


    #endregion

    #region ChartData
    [LocalizedCategory("ChartData")]
	[TypeConverter(typeof(ChartDataTypeConverter))]
	[LocalizedDescription("ChartData")]
    [ReadOnly(true)]//Doesn't work with static rows so we have disabled it... 05122007 AJM & GJL
    internal class PropertyChartData : IReportItem
    {
        PropertyChart _pt;
        internal PropertyChartData(PropertyChart pt)
        {
            _pt = pt;
        }

		[LocalizedDisplayName("ChartData_LabelAppearance")]
		[LocalizedDescription("ChartData_LabelAppearance")]
        public PropertyAppearance LabelAppearance
        {
            get
            {
                return new PropertyAppearance(_pt,
                                "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataLabel");
            }
        }

		[LocalizedDisplayName("ChartData_LabelVisible")]
		[LocalizedDescription("ChartData_LabelVisible")]
        public bool LabelVisible
        {
            get
            {
                string cdata = _pt.GetWithList("false",
                    "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataLabel", "Visible");

                return cdata.ToLower() == "true";
            }
            set
            {
                _pt.SetWithList(value? "true": "false", "ChartData", "ChartSeries", 
                    "DataPoints", "DataPoint", "DataLabel", "Visible");
            }
        }

		[LocalizedDisplayName("ChartData_DataValue")]
		[LocalizedDescription("ChartData_DataValue")]
        public PropertyExpr DataValue
        {
            get
            {
                // Chart data-- this is a simplification of what is possible (TODO) 
                //        <ChartData>
                //          <ChartSeries>
                //            <DataPoints>
                //              <DataPoint>
                //                <DataValues>
                //                  <DataValue>
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
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

                string cdata = _pt.GetWithList("",
                    "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataValues", "DataValue", "Value");
                return new PropertyExpr(cdata);
            }
            set
            {
                _pt.SetWithList(value.Expression, "ChartData", "ChartSeries", "DataPoints", "DataPoint",
                    "DataValues", "DataValue", "Value");
            }
        }

		[LocalizedDisplayName("ChartData_DataValue2")]
		[LocalizedDescription("ChartData_DataValue2")]
        public PropertyExpr DataValue2
        {
            get
            {
                // Chart data-- this is a simplification of what is possible (TODO) 
                //        <ChartData>
                //          <ChartSeries>
                //            <DataPoints>
                //              <DataPoint>
                //                <DataValues>
                //                  <DataValue>
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
                //                  <DataValue>   ---- this is the second data value
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
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
                string type = _pt.Type.ToLowerInvariant();
                if (!(type == "scatter" || type == "bubble"))
                    return new PropertyExpr("");

                return new PropertyExpr(GetChartDataValue(2));
            }
            set
            {
                string type = _pt.Type.ToLowerInvariant();
                if (!(type == "scatter" || type == "bubble"))
                    throw new ArgumentException("Chart type must be 'Scatter' or 'Bubble' to set DataValue2."); ;
                SetChartDataValue(2, value.Expression);
            }
        }

		[LocalizedDisplayName("ChartData_DataValue3")]
		[LocalizedDescription("ChartData_DataValue3")]
        public PropertyExpr DataValue3
        {
            get
            {
                // Chart data-- this is a simplification of what is possible 
                //        <ChartData>
                //          <ChartSeries>
                //            <DataPoints>
                //              <DataPoint>
                //                <DataValues>
                //                  <DataValue>
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
                //                  <DataValue>
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
                //                  <DataValue>   ---- this is the third data value-- bubble size
                //                    <Value>=Sum(Fields!Sales.Value)</Value>
                //                  </DataValue>
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
                string type = _pt.Type.ToLowerInvariant();
                if (type != "bubble")
                    return new PropertyExpr("");

                return new PropertyExpr(GetChartDataValue(3));
            }
            set
            {
                string type = _pt.Type.ToLowerInvariant();
                if (type != "bubble")
                    throw new ArgumentException("Chart type must be 'Bubble' to set DataValue3."); ;
                SetChartDataValue(3, value.Expression);
            }
        }

		private string GetChartDataValue(int i)
        {
            XmlNode dvs = DesignXmlDraw.FindNextInHierarchy(_pt.Node,
    "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataValues");
            XmlNode cnode;
            foreach (XmlNode dv in dvs.ChildNodes)
            {
                if (dv.Name != "DataValue")
                    continue;
                i--;
                cnode = DesignXmlDraw.FindNextInHierarchy(dv, "Value");
                if (cnode == null)
                    continue;
                if (i <= 0)
                    return cnode.InnerText;
            }
            return "";
        }
 
        private void SetChartDataValue(int i, string expr)
        {
            DesignXmlDraw dr = _pt.Draw;
            string expr1 = i == 1 ? expr : _pt.ChartData.DataValue.Expression;
            string expr2 = i == 2 ? expr : _pt.ChartData.DataValue2.Expression;
            string expr3 = i == 3 ? expr : _pt.ChartData.DataValue3.Expression;

            foreach (XmlNode node in _pt.Nodes)
            {
                XmlNode chartdata = dr.SetElement(node, "ChartData", null);
                XmlNode chartseries = dr.SetElement(chartdata, "ChartSeries", null);
                XmlNode datapoints = dr.SetElement(chartseries, "DataPoints", null);
                XmlNode datapoint = dr.SetElement(datapoints, "DataPoint", null);
                XmlNode datavalues = dr.SetElement(datapoint, "DataValues", null);
                dr.RemoveElementAll(datavalues, "DataValue");
                XmlNode datavalue = dr.SetElement(datavalues, "DataValue", null);
                dr.SetElement(datavalue, "Value", expr1);

                string type = _pt.Type.ToLowerInvariant();
                if (type == "scatter" || type == "bubble")
                {
                    datavalue = dr.CreateElement(datavalues, "DataValue", null);
                    dr.SetElement(datavalue, "Value", expr2);
                    if (type == "bubble")
                    {
                        datavalue = dr.CreateElement(datavalues, "DataValue", null);
                        dr.SetElement(datavalue, "Value", expr3);
                    }
                }
            }
        }
        
        public override string ToString()
        {
            return _pt.GetWithList("",
                "ChartData", "ChartSeries", "DataPoints", "DataPoint", "DataValues", "DataValue", "Value");
        }

#region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pt;
        }

        #endregion
    }

    internal class ChartDataTypeConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyChartData))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyChartData)
            {
                PropertyChartData pc = value as PropertyChartData;
                return pc.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }


#endregion

    #region ChartGridLines
    [TypeConverter(typeof(ChartGridLinesTypeConverter))]
    internal class PropertyChartGridLines : IReportItem
    {
        PropertyChart _pt;
        string[] _names;

        internal PropertyChartGridLines(PropertyChart pt, params string[] names)
        {
            _pt = pt;
            _names = names;
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("ChartGridLines_ShowGridLines")]
		[LocalizedDescription("ChartGridLines_ShowGridLines")]
        public bool ShowGridLines
        {
            get
            {
                List<string> l = new List<string>(_names);
                l.Add("ShowGridLines");
                string s = _pt.GetWithList("false", l.ToArray());
                return s == "true";
            }
            set
            {
                List<string> l = new List<string>(_names);
                l.Add("ShowGridLines");
                _pt.SetWithList(value?"true":"false", l.ToArray());
            }
        }

        [TypeConverter(typeof(ColorConverter))]
		[LocalizedDisplayName("ChartGridLines_Color")]
		[LocalizedDescription("ChartGridLines_Color")]
        public string Color
        {
            get
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderColor");
                l.Add("Default");
                return _pt.GetWithList("Black", l.ToArray());
            }
            set
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderColor");
                l.Add("Default");
                _pt.SetWithList(value, l.ToArray());
            }
        }

		[LocalizedDisplayName("ChartGridLines_LineStyle")]
		[LocalizedDescription("ChartGridLines_LineStyle")]
        public LineStyleEnum LineStyle
        {
            get
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderStyle");
                l.Add("Default");
                string s = _pt.GetWithList("Solid", l.ToArray());
                switch (s)
                {
                    case "Solid": return LineStyleEnum.Solid;
                    case "Dotted": return LineStyleEnum.Dotted;
                    case "Dashed": return LineStyleEnum.Dashed;
                    default: return LineStyleEnum.Solid;
                }
            }
            set
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderStyle");
                l.Add("Default");
                _pt.SetWithList(value.ToString(), l.ToArray());
            }
        }

		[LocalizedDisplayName("ChartGridLines_Width")]
		[LocalizedDescription("ChartGridLines_Width")]
        public PropertyExpr Width
        {
            get
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderWidth");
                l.Add("Default");
                return new PropertyExpr(_pt.GetWithList("1pt", l.ToArray()));
            }
            set
            {
                List<string> l = new List<string>(_names);
                l.Add("Style");
                l.Add("BorderWidth");
                l.Add("Default");
                _pt.SetWithList(value.Expression, l.ToArray());
            }
        }
        
        public override string ToString()
        {
            return this.ShowGridLines?"visible":"hidden";
        }

        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pt;
        }

        #endregion
    }
    internal class ChartGridLinesTypeConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyChartGridLines))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyChartGridLines)
            {
                PropertyChartGridLines pf = value as PropertyChartGridLines;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    #endregion

    #region ChartLegend
    [LocalizedCategory("ChartLegend")]
    [TypeConverter(typeof(ChartLegendTypeConverter))]
	[LocalizedDescription("ChartLegend")]
    internal class PropertyChartLegend : IReportItem
    {
        PropertyChart _pt;
        internal PropertyChartLegend(PropertyChart pt)
        {
            _pt = pt;
        }

		[LocalizedDisplayName("ChartLegend_Visible")]
		[LocalizedDescription("ChartLegend_Visible")]
        public bool Visible
        {
            get
            {
                string v = _pt.GetWithList("true", "Legend", "Visible");
                return v == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", "Legend", "Visible");
            }
        }

		[LocalizedDisplayName("ChartLegend_Position")]
		[LocalizedDescription("ChartLegend_Position")]
        public LegendPositionEnum Position
        {
            get
            {
                string v = _pt.GetWithList("RightTop", "Legend", "Position");
                return LegendPosition.GetStyle(v);
            }
            set
            {
                _pt.SetWithList(value.ToString(), "Legend", "Position");
            }
        }

		[LocalizedDisplayName("ChartLegend_Layout")]
		[LocalizedDescription("ChartLegend_Layout")]
        public LegendLayoutEnum Layout
        {
            get
            {
                string v = _pt.GetWithList("Column", "Legend", "Layout");
                return LegendLayout.GetStyle(v);
            }
            set
            {
                _pt.SetWithList(value.ToString(), "Legend", "Layout");
            }
        }

		[LocalizedDisplayName("ChartLegend_InsidePlotArea")]
		[LocalizedDescription("ChartLegend_InsidePlotArea")]
        public bool InsidePlotArea
        {
            get
            {
                string v = _pt.GetWithList("true", "Legend", "InsidePlotArea");
                return v.ToLower() == "true";
            }
            set
            {
                _pt.SetWithList(value ? "true" : "false", "Legend", "InsidePlotArea");
            }
        }

		[LocalizedDisplayName("ChartLegend_Appearance")]
		[LocalizedDescription("ChartLegend_Appearance")]
        public PropertyAppearance Appearance
        {
            get { return new PropertyAppearance(_pt, "Legend"); }
        }

		[LocalizedDisplayName("ChartLegend_Background")]
		[LocalizedDescription("ChartLegend_Background")]
        public PropertyBackground Background
        {
            get { return new PropertyBackground(_pt, "Legend"); }
        }

		[LocalizedDisplayName("ChartLegend_Border")]
		[LocalizedDescription("ChartLegend_Border")]
        public PropertyBorder Border
        {
            get { return new PropertyBorder(_pt, "Legend"); }
        }

		[LocalizedDisplayName("ChartLegend_Padding")]
		[LocalizedDescription("ChartLegend_Padding")]
        public PropertyPadding Padding
        {
            get { return new PropertyPadding(_pt, "Legend"); }
        }

        public override string ToString()
        {
            //return _pt.GetWithList("", "Title", "Caption");
            if (!this.Visible)
                return "hidden";
            else
                return this.Position.ToString();
        }

        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pt;
        }

        #endregion
    }
    internal class ChartLegendTypeConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyChartLegend))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyChartLegend)
            {
                PropertyChartLegend pf = value as PropertyChartLegend;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }


#endregion

    #region ChartTitle
    [LocalizedCategory("ChartTitle")]
    [TypeConverter(typeof(ChartTitleTypeConverter))]
	[LocalizedDescription("ChartTitle")]
    internal class PropertyChartTitle : IReportItem
    {
        PropertyChart _pt;
        string[] _subitems;
        string[] _names;

        internal PropertyChartTitle(PropertyChart pt)
        {
            _pt = pt;
            _names = null;
            _subitems = new string[] { "Title"};
        }

        internal PropertyChartTitle(PropertyChart pt, params string[] names)
        {
            _pt = pt;
            _names = names;

            // now build the array used to get/set values
            _subitems = new string[names.Length + 1];
            int i = 0;
            foreach (string s in names)
                _subitems[i++] = s;

            _subitems[i++] = "Title";
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("ChartTitle_Caption")]
		[LocalizedDescription("ChartTitle_Caption")]
        public PropertyExpr Caption
        {
            get
            {
                List<string> l = new List<string>(_subitems);
                l.Add("Caption");
                return new PropertyExpr(_pt.GetWithList("", l.ToArray()));
            }
            set
            {
                List<string> l = new List<string>(_subitems);
                l.Add("Caption");
                _pt.SetWithList(value.Expression, l.ToArray());
            }
        }

		[LocalizedDisplayName("ChartTitle_Appearance")]
		[LocalizedDescription("ChartTitle_Appearance")]
        public PropertyAppearance Appearance
        {
            get { return new PropertyAppearance(_pt, _subitems); }
        }

		[LocalizedDisplayName("ChartTitle_Background")]
		[LocalizedDescription("ChartTitle_Background")]
        public PropertyBackground Background
        {
            get { return new PropertyBackground(_pt, _subitems); }
        }

		[LocalizedDisplayName("ChartTitle_Border")]
		[LocalizedDescription("ChartTitle_Border")]
        public PropertyBorder Border
        {
            get { return new PropertyBorder(_pt, _subitems); }
        }

		[LocalizedDisplayName("ChartTitle_Padding")]
		[LocalizedDescription("ChartTitle_Padding")]
        public PropertyPadding Padding
        {
            get { return new PropertyPadding(_pt, _subitems); }
        }

        public override string ToString()
        {
            return this.Caption.Expression;
        }

        #region IReportItem Members

        public PropertyReportItem GetPRI()
        {
            return this._pt;
        }

        #endregion
    }
    internal class ChartTitleTypeConverter : ExpandableObjectConverter
    {
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context,
                                          System.Type destinationType)
        {
            if (destinationType == typeof(PropertyChartTitle))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyChartTitle)
            {
                PropertyChartTitle pf = value as PropertyChartTitle;
                return pf.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    #endregion

    #region ChartType
    internal class ChartTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new string[] {
                                    "Column", "Bar", "Line", "Map", "Pie", "Area", "Doughnut", "Bubble", "Scatter"});
        }
    }
    #endregion

    #region PaletteType
    internal class PaletteTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new string[] {
               "Default", "EarthTones", "Excel", "GrayScale", "Light", "Pastel", "SemiTransparent","Patterned","PatternedBlack","Custom"});
        }
    }
    #endregion

    internal enum LineStyleEnum {Solid, Dotted, Dashed };
}
