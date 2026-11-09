

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Visibility definition and processing.
	///</summary>
	[Serializable]
	internal class Visibility : ReportLink
	{
		Expression _Hidden;		// (Boolean) Indicates if the item should be initially hidden.
		string _ToggleItem;		// The name of the textbox used to
					// hide/unhide this report item. Clicking on
					//an instance of the ToggleItem will toggle
					//the hidden state of every corresponding
					//instance of this item. If the Toggle item
					//becomes hidden, this item should become
					//hidden.
					//Must be a textbox in the same grouping
					//scope as this item or in any containing (ancestor) grouping scope
					//If omitted, no item will toggle the hidden
					//state of this item.
					//Not allowed on and cannot refer to report
					//items contained in a page header or
					//footer.
					//Cannot refer to a report item contained
					//within the current report item unless
					//current grouping scope has a Parent.		
		Textbox _ToggleTextbox;	// the textbox named by ToggleItem; null when none or not a textbox
	
		internal Visibility(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Hidden=null;
			_ToggleItem=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Hidden":
						_Hidden = new Expression(r, this, xNodeLoop, ExpressionType.Boolean);
						break;
					case "ToggleItem":
						_ToggleItem = xNodeLoop.InnerText;
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Visibility element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
			if (_Hidden != null)
                await _Hidden.FinalPass().ConfigureAwait(false);
			if (_ToggleItem != null)
			{
				_ToggleTextbox = OwnerReport.LUReportItems[_ToggleItem] as Textbox;
				_ToggleTextbox?.AddToggleTarget(this);
			}
			return;
		}

		internal Expression Hidden
		{
			get { return  _Hidden; }
			set {  _Hidden = value; }
		}

		internal Task<bool> IsHidden(Report rpt, Row r)
		{
			// An instance of the toggle item clicked in a viewer flips the state it starts in. With
			// nothing toggled (every static render) this is the Hidden expression alone.
			if (_ToggleTextbox == null || rpt == null || !rpt.HasToggles)
				return IsInitiallyHidden(rpt, r);
			return IsHiddenToggled(rpt, r);
		}

		private async Task<bool> IsHiddenToggled(Report rpt, Row r)
		{
			bool hidden = await IsInitiallyHidden(rpt, r).ConfigureAwait(false);
			if (rpt.IsToggled(await _ToggleTextbox.ToggleKey(rpt, r).ConfigureAwait(false)))
				hidden = !hidden;
			return hidden;
		}

		// The Hidden expression alone: the state before any toggle is clicked.
		internal Task<bool> IsInitiallyHidden(Report rpt, Row r)
		{
			if (_Hidden == null)
				return Task.FromResult(false);

			return _Hidden.EvaluateBoolean(rpt, r);
		}

		// The textbox that toggles this item; null when there is none.
		internal Textbox ToggleTextbox
		{
			get { return _ToggleTextbox; }
		}

		internal string ToggleItem
		{
			get { return  _ToggleItem; }
			set {  _ToggleItem = value; }
		}
	}
}
