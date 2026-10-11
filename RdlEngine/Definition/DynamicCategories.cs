

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// In Matrix, the dynamic categories.
	///</summary>
	[Serializable]
	internal class DynamicCategories : ReportLink
	{
		Grouping _Grouping;	// The expressions to group the data by. Page
							// breaks in the grouping are not allowed.
		Sorting _Sorting;	// The expressions to sort the data by
		Expression _Label;	//(Variant) The label displayed on the axis.		
	
		internal DynamicCategories(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Grouping=null;
			_Sorting=null;
			_Label=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "grouping":
						_Grouping = new Grouping(r, this, xNodeLoop);
						break;
					case "sorting":
						_Sorting = new Sorting(r, this, xNodeLoop);
						break;
					case "label":
						_Label = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					default:
						break;
				}
			}
			if (_Grouping == null)
				OwnerReport.rl.LogError(8, "DynamicCategories requires the Grouping element.");
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Grouping != null)
                await _Grouping.FinalPass().ConfigureAwait(false);
			if (_Sorting != null)
                await _Sorting.FinalPass().ConfigureAwait(false);
			if (_Label != null)
                await _Label.FinalPass().ConfigureAwait(false);
			return;
		}

		internal Grouping Grouping
		{
			get { return  _Grouping; }
			set {  _Grouping = value; }
		}

		internal Sorting Sorting
		{
			get { return  _Sorting; }
			set {  _Sorting = value; }
		}

		internal Expression Label
		{
			get { return  _Label; }
			set {  _Label = value; }
		}
	}
}
