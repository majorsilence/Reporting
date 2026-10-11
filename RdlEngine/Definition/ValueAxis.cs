

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Chart value axis definition.
	///</summary>
	[Serializable]
	internal class ValueAxis : ReportLink
	{
		Axis _Axis;		// Display properties for the value axis.		
	
		internal ValueAxis(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Axis=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "axis":
						_Axis = new Axis(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ValueAxis element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		async override internal Task FinalPass()
		{
			if (_Axis != null)
                await _Axis.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Axis Axis
		{
			get { return  _Axis; }
			set {  _Axis = value; }
		}
	}
}
