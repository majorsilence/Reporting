

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// DataPoint definition and processing.
	///</summary>
	[Serializable]
	internal class DataPoint : ReportLink
	{
		DataValues _DataValues;	//Data value set for the Y axis.
		DataLabel _DataLabel;	// Indicates the values should be marked with data labels.
		Action _Action;			// Action to execute.
		Style _Style;			// Defines border and background style
								// properties for the data point.
		Marker _Marker;			// Defines marker properties. Markers do
								//	not apply to data points of pie, doughnut
								//	and any stacked chart types.
		string _DataElementName;	// The name to use for the data element for
									//	this data point.
									//	Default: Name of corresponding static
									//	series or category. If there is no static
									//	series or categories, �Value�
		DataElementOutputEnum _DataElementOutput;	// Indicates whether the data point should
									// appear in a data rendering.
	
		internal DataPoint(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_DataValues=null;
			_DataLabel=null;
			_Action=null;
			_Style=null;
			_Marker=null;
			_DataElementName=null;
			_DataElementOutput=DataElementOutputEnum.Output;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "datavalues":
						_DataValues = new DataValues(r, this, xNodeLoop);
						break;
					case "datalabel":
						_DataLabel = new DataLabel(r, this, xNodeLoop);
						break;
					case "action":
						_Action = new Action(r, this, xNodeLoop);
						break;
					case "style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "marker":
						_Marker = new Marker(r, this, xNodeLoop);
						break;
					case "dataelementname":
						_DataElementName = xNodeLoop.InnerText;
						break;
					case "dataelementoutput":
						_DataElementOutput = Majorsilence.Reporting.Rdl.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown DataPoint element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_DataValues == null)
				OwnerReport.rl.LogError(8, "DataPoint requires the DataValues element.");
		}
		
		async override internal Task FinalPass()
		{
			if (_DataValues != null)
                await _DataValues.FinalPass().ConfigureAwait(false);
			if (_DataLabel != null)
                await _DataLabel.FinalPass().ConfigureAwait(false);
			if (_Action != null)
                await _Action.FinalPass().ConfigureAwait(false);
			if (_Style != null)
                await _Style.FinalPass().ConfigureAwait(false);
			if (_Marker != null)
                await _Marker.FinalPass().ConfigureAwait(false);
			return;
		}


		internal DataValues DataValues
		{
			get { return  _DataValues; }
			set {  _DataValues = value; }
		}

		internal DataLabel DataLabel
		{
			get { return  _DataLabel; }
			set {  _DataLabel = value; }
		}

		internal Action Action
		{
			get { return  _Action; }
			set {  _Action = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal Marker Marker
		{
			get { return  _Marker; }
			set {  _Marker = value; }
		}

		internal string DataElementName
		{
			get { return  _DataElementName; }
			set {  _DataElementName = value; }
		}

		internal DataElementOutputEnum DataElementOutput
		{
			get { return  _DataElementOutput; }
			set {  _DataElementOutput = value; }
		}
	}
	
}
