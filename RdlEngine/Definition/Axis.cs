using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Axis definition and processing.
	///</summary>
	[Serializable]
	internal class Axis : ReportLink
	{
		bool _Visible;	// Whether the axis labels are displayed. Defaults to false.
		Style _Style;	// Defines text style properties for the axis labels
		// and line style properties for the axis line.
		Title _Title;	// Defines a title for the First Y axis
        Title _Title2;  // 20022008 AJM GJL Defines a title for the Second Y axis
		bool _Margin;	// Indicates whether an axis margin will be
		//	created. The size of the margin is automatically
		//	generated based on the Scale and the number of
		//	data points. Defaults to false.
		AxisTickMarksEnum _MajorTickMarks; // None (Default)
		AxisTickMarksEnum _MinorTickMarks; // None (Default)
		ChartGridLines _MajorGridLines;	// Indicates major gridlines should be displayed for this axis.
		ChartGridLines _MinorGridLines;	// Indicates minor gridlines should be displayed for this axis.
		Expression _MajorInterval;		// Unit for major gridlines/tickmarks
		// If omitted, the axis is autodivided
		Expression _MinorInterval;		// Unit for minor gridlines/tickmarks
		// If omitted, the axis is autodivided
		bool _Reverse;	// If false (Default) the axis is plotted normally, if
		//  true its direction is reversed.
		int _CrossAt;	//  Value at which to cross the other axis
		// If omitted, uses the default behavior for the chart type.
		bool _Interlaced;	// If this property is true then strip lines are drawn
		//every other grid line interval for the axis. If grid
		//lines are not used for the axis then the axis� tick
		//marks or labels are used to determine the
		//interlaced strip lines interval.
		//Defaults to False.
		bool _Scalar;	// Indicates the values along this axis are scalar
		//values (i.e. numeric or date) which should be
		//displayed on the chart in a continuous axis.
		//Scalar cannot be true if the axis has more than
		//one grouping, if that grouping is static or has
		//more than one group expression or if the axis
		//values have a label.
		Expression _Min;		// Minimum value for the axis
		// If omitted, the axis autoscales
		Expression _Max;		// Maximum value for the axis
		// If omitted, the axis autoscales
		bool _LogScale;	// Whether the axis is logarithmic. Default is false.		
	    // If checked the axis shows gridlines at change of each month WP 12 May 2008.  Default is false
        bool _Month;

        
        bool _CanOmit=false;	// When display values don't fit, is it OK to drop some from display

		internal Axis(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Visible =false;
			_Style = null;
			_Title = null;
            _Title2 = null;// 20022008 AJM GJL
			_Margin = false;
			_MajorTickMarks = AxisTickMarksEnum.None;
			_MinorTickMarks = AxisTickMarksEnum.None;
			_MajorGridLines = null;
			_MinorGridLines = null;
			_MajorInterval = null;
			_MinorInterval =null;
			_Reverse = false;
			_CrossAt = 0;
			_Interlaced = false;
			_Scalar=false;
			_Min=null;
			_Max=null;
			_LogScale=false;
            _Month = false; //12052008 WP

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "visible":
						_Visible = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "title":
						_Title = new Title(r, this, xNodeLoop);
						break;
                    // 20022008 AJM GJL - Second Y axis
                    case "title2":
                    case "fyi:title2":
                        _Title2 = new Title(r, this, xNodeLoop);
                       break;
					case "margin":
						_Margin = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "majortickmarks":
						_MajorTickMarks = AxisTickMarks.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "minortickmarks":
						_MinorTickMarks = AxisTickMarks.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "majorgridlines":
						_MajorGridLines = new ChartGridLines(r, this, xNodeLoop);
						break;
					case "minorgridlines":
						_MinorGridLines = new ChartGridLines(r, this, xNodeLoop);
						break;
					case "majorinterval":
						_MajorInterval = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Integer);
						OwnerReport.rl.LogError(4, "Axis element MajorInterval is currently ignored.");
						break;
					case "minorinterval":
						_MinorInterval = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Integer);
						OwnerReport.rl.LogError(4, "Axis element MinorInterval is currently ignored.");
						break;
					case "reverse":
						_Reverse = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "crossat":
						_CrossAt = XmlUtil.Integer(xNodeLoop.InnerText);
						break;
					case "interlaced":
						_Interlaced = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "scalar":
						_Scalar = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "min":
						_Min = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Integer);
						break;
					case "max":
						_Max = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Integer);
						break;
					case "logscale":
						_LogScale = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
                    case "month":
                    case "fyi:month":
                        _Month = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "fyi:canomit":
                        _CanOmit = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Axis element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		async override internal Task FinalPass()
		{
			if (_MajorInterval != null)
                await _MajorInterval.FinalPass().ConfigureAwait(false);
			if (_MinorInterval != null)
                await _MinorInterval.FinalPass().ConfigureAwait(false);
			if (_Max != null)
                await _Max.FinalPass().ConfigureAwait(false);
			if (_Min != null)
                await _Min.FinalPass().ConfigureAwait(false);
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			if (_Title != null)
                await _Title.FinalPass().ConfigureAwait(false);
			// 20022008 AJM GJL - Second Y axis
            if (_Title2 != null)
                await _Title2.FinalPass().ConfigureAwait(false);
			if (_MajorGridLines != null)
                await _MajorGridLines.FinalPass().ConfigureAwait(false);
			if (_MinorGridLines != null)
                await _MinorGridLines.FinalPass().ConfigureAwait(false);
			return;
		}

		internal bool Visible
		{
			get { return  _Visible; }
			set {  _Visible = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal Title Title
		{
			get { return  _Title; }
			set {  _Title = value; }
		}
		// 20022008 AJM GJL - Second Y axis
        internal Title Title2
        {
            get { return _Title2; }
            set { _Title2 = value; }
        } 

		internal bool Margin
		{
			get { return  _Margin; }
			set {  _Margin = value; }
		}

		internal AxisTickMarksEnum MajorTickMarks
		{
			get { return  _MajorTickMarks; }
			set {  _MajorTickMarks = value; }
		}

		internal AxisTickMarksEnum MinorTickMarks
		{
			get { return  _MinorTickMarks; }
			set {  _MinorTickMarks = value; }
		}

		internal ChartGridLines MajorGridLines
		{
			get { return  _MajorGridLines; }
			set {  _MajorGridLines = value; }
		}

		internal ChartGridLines MinorGridLines
		{
			get { return  _MinorGridLines; }
			set {  _MinorGridLines = value; }
		}

		internal Expression MajorInterval
		{
			get { return  _MajorInterval; }
			set {  _MajorInterval = value; }
		}

		internal Expression MinorInterval
		{
			get { return  _MinorInterval; }
			set {  _MinorInterval = value; }
		}

		internal bool Reverse
		{
			get { return  _Reverse; }
			set {  _Reverse = value; }
		}

		internal int CrossAt
		{
			get { return  _CrossAt; }
			set {  _CrossAt = value; }
		}

		internal bool Interlaced
		{
			get { return  _Interlaced; }
			set {  _Interlaced = value; }
		}

		internal bool Scalar
		{
			get { return  _Scalar; }
			set {  _Scalar = value; }
		}

		internal Expression Min
		{
			get { return  _Min; }
			set {  _Min = value; }
		}

		internal Expression Max
		{
			get { return  _Max; }
			set {  _Max = value; }
		}
        //GJL 010308 --- Changed from int... We may want to start / stop a chart on a decimal
		internal async Task<double> MaxEval(Report r, Row row)
		{
			if (_Max == null)
				return (double)int.MinValue;
			return await _Max.EvaluateDouble(r, row).ConfigureAwait(false);
		}

		internal async Task<double> MinEval(Report r, Row row)
		{
			if (_Min == null)
                return (double)int.MinValue;
			return await _Min.EvaluateDouble(r, row).ConfigureAwait(false);
		}
        //WP 12 may 2008 -- set up month gridlines for report
        internal bool Month
        {
            get { return _Month; }
            set { _Month = value; }
		}

		internal bool LogScale
		{
			get { return  _LogScale; }
			set {  _LogScale = value; }
		}

        internal bool CanOmit
        {
            get { return _CanOmit; }
            set { _CanOmit = value; }
        }
    }	
}
