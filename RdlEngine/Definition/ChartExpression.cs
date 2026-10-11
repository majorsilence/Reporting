using System;
using System.Xml;
using System.IO;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// ChartExpression definition and processing.
	///</summary>
	[Serializable]
	internal class ChartExpression : ReportItem
	{
        //Expression _Value;	// (Variant) An expression, the value of which is
        //                    // displayed in the chart
        DataValues _Values;     // all the data values
        DataPoint _DataPoint;	// The data point that generated this
        Expression _ChartLabel;    // Chart Label
        Expression _PlotType; // 05122007 AJM & GJL Added for PlotType Support
        Expression _YAxis; //140208 GJL Added for left/Right YAxis Support
        Expression _NoMarker; //30052008 GJL Added to allow lines with no markers
        Expression _LineSize;
        Expression _Colour;
		internal ChartExpression(ReportDefn r, ReportLink p, XmlNode xNode):base(r,p,xNode)
		{
			_Values=null;
		
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
                    //case "Value":
                    //    _Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                    //    break;

                    case "datavalues":
                        _Values = new DataValues(r, p, xNodeLoop);
                        break;
                    case "datapoint":
						_DataPoint = (DataPoint) this.OwnerReport.LUDynamicNames[xNodeLoop.InnerText];
						break;
                    case "chartlabel":
                        _ChartLabel = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    // 05122007AJM & GJL Added to store PlotType
                    case "plottype":
                        _PlotType = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Variant);
                        break;    
                    //140208 GJL Added for left/Right YAxis Support
                    case "yaxis":
                        _YAxis = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "nomarker":
                    case "fyi:nomarker":
                        _NoMarker = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "linesize":
                    case "fyi:linesize":
                        _LineSize = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "fyi:color":
                    case "color":
                    case "colour":
                        _Colour = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
					default:
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Chart element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
            await base.FinalPass().ConfigureAwait(false);
			if (_Values != null)
                await _Values.FinalPass().ConfigureAwait(false);
            if (_DataPoint != null)
                await _DataPoint.FinalPass().ConfigureAwait(false);
            if (_ChartLabel != null)
                await _ChartLabel.FinalPass().ConfigureAwait(false);
            if (_PlotType != null)
                await _PlotType.FinalPass().ConfigureAwait(false);
            if (_YAxis != null)
                await _YAxis.FinalPass().ConfigureAwait(false);
            if (_NoMarker != null)
                await _NoMarker.FinalPass().ConfigureAwait(false);
            if (_LineSize != null)
                await _LineSize.FinalPass().ConfigureAwait(false);
            if (_Colour != null)
                await _Colour.FinalPass().ConfigureAwait(false);
            return;
		}

		override internal Task Run(IPresent ip, Row row)
		{
			return Task.CompletedTask;
		}

		internal Expression Value
		{
            get { return _Values != null && _Values.Items.Count > 0? _Values.Items[0].Value: null; }
		}

        internal Expression Value2
        {
            get { return _Values != null && _Values.Items.Count > 1 ? _Values.Items[1].Value : null; }
        }

        internal Expression Value3
        {
            get { return _Values != null && _Values.Items.Count > 2 ? _Values.Items[2].Value : null; }
        }
 
		internal DataPoint DP
		{
			get { return  _DataPoint; }
		}

        internal Expression ChartLabel
        {
            get {return _ChartLabel;}
        }
        // 05122007AJM & GJL Added for PlotType support
        internal Expression PlotType
        {
            get { return _PlotType; }
        }
		// 20022008 AJM GJL - Added for Second Y axis support
        internal Expression YAxis
        {
            get { return _YAxis; }
        }
        //30052008 GJL - Added to allow lines with no markers
        internal Expression NoMarker
        {
            get { return _NoMarker; }
        }

        internal Expression LineSize
        {
            get { return _LineSize; }
        }

        internal Expression Colour
        {
            get { return _Colour; }
        }

	}
}
