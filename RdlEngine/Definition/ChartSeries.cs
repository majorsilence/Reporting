
				// 20022008 AJM GJL - Added Second Y axis support
using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Chart series definition and processing.
	///</summary>
	[Serializable]
	internal class ChartSeries : ReportLink
	{
        String _Colour;
        DataPoints _Datapoints;	// Data points within a series
		PlotTypeEnum _PlotType;		// Indicates whether the series should be plotted
								// as a line in a Column chart. If set to auto,
								// should be plotted per the primary chart type.
								// Auto (Default) | Line	
        String _YAxis;          //Indicates if the series uses the left or right axis. GJL 140208
        bool _NoMarker;          //Indicates if the series should not show its plot markers. GJL 300508
        String _LineSize;
	
		internal ChartSeries(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Datapoints=null;
			_PlotType=PlotTypeEnum.Auto;
            _YAxis = "Left";
            _NoMarker = false;
            _LineSize = "Regular";

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "datapoints":
						_Datapoints = new DataPoints(r, this, xNodeLoop);
						break;
					case "plottype":
						_PlotType = Majorsilence.Reporting.Rdl.PlotType.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
                    case "yaxis":
                    case "fyi:yaxis":
                        _YAxis = xNodeLoop.InnerText;
                        break;
                    case "nomarker":
                    case "fyi:nomarker":
                        _NoMarker = Boolean.Parse(xNodeLoop.InnerText);
                        break;
                    case "linesize":
                    case "fyi:linesize":
                        _LineSize = xNodeLoop.InnerText;
                        break;
                    case "fyi:color":
                    case "color":
                    case "colour":
                        _Colour = xNodeLoop.InnerText;
                        break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ChartSeries element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Datapoints == null)
				OwnerReport.rl.LogError(8, "ChartSeries requires the DataPoints element.");
		}
		
		async override internal Task FinalPass()
		{
			if (_Datapoints != null)
                await _Datapoints.FinalPass().ConfigureAwait(false);
			return;
		}

		internal DataPoints Datapoints
		{
			get { return  _Datapoints; }
			set {  _Datapoints = value; }
		}

		internal PlotTypeEnum PlotType
		{
			get { return  _PlotType; }
			set {  _PlotType = value; }
		}

        internal String Colour
        {
            get { return _Colour; }
            set { _Colour = value; }
        }

        internal String YAxis
        {
            get { return _YAxis; }
            set { _YAxis = value; }
        }

        internal bool NoMarker
        {
            get { return _NoMarker; }
            set { _NoMarker = value; }
        }

        internal string LineSize
        {
            get { return _LineSize; }
            set { _LineSize = value; }
        }
	}
}
