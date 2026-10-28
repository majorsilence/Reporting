

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// TableGroup definition and processing.
	///</summary>
	[Serializable]
	internal class TableGroup : ReportLink
	{
		Grouping _Grouping;		// The expressions to group the data by.
		Sorting _Sorting;		// The expressions to sort the data by.
		Header _Header;			// A group header row.
		Footer _Footer;			// A group footer row.
		Visibility _Visibility;	// Indicates if the group (and all groups embedded
								// within it) should be hidden.		
		Textbox _ToggleTextbox;	//  resolved TextBox for toggling visibility
		bool _KeepTogether;		// Indicates the group should be kept on one page if it can be.
	
		internal TableGroup(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Grouping=null;
			_Sorting=null;
			_Header=null;
			_Footer=null;
			_Visibility=null;
			_ToggleTextbox=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Grouping":
						_Grouping = new Grouping(r, this, xNodeLoop);
						break;
					case "Sorting":
						_Sorting = new Sorting(r, this, xNodeLoop);
						break;
					case "Header":
						_Header = new Header(r, this, xNodeLoop);
						break;
					case "Footer":
						_Footer = new Footer(r, this, xNodeLoop);
						break;
					case "Visibility":
						_Visibility = new Visibility(r, this, xNodeLoop);
						break;
					case "KeepTogether":
						_KeepTogether = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableGroup element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_Grouping == null)
				OwnerReport.rl.LogError(8, "TableGroup requires the Grouping element.");
		}
		
		async override internal Task FinalPass()
		{
			if (_Grouping != null)
                await _Grouping.FinalPass().ConfigureAwait(false);
			if (_Sorting != null)
                await _Sorting.FinalPass().ConfigureAwait(false);
			if (_Header != null)
                await _Header.FinalPass().ConfigureAwait(false);
			if (_Footer != null)
                await _Footer.FinalPass().ConfigureAwait(false);
			if (_Visibility != null)
			{
                await _Visibility.FinalPass().ConfigureAwait(false);
				if (_Visibility.ToggleItem != null)
				{
					_ToggleTextbox = (Textbox) (OwnerReport.LUReportItems[_Visibility.ToggleItem]);
					if (_ToggleTextbox != null)
						_ToggleTextbox.IsToggle = true;
				}
			}
			return;
		}

		internal float DefnHeight()
		{
			float height=0;
			if (_Header != null)
				height += _Header.TableRows.DefnHeight();

			if (_Footer != null)
				height += _Footer.TableRows.DefnHeight();

			return height;
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

		internal Header Header
		{
			get { return  _Header; }
			set {  _Header = value; }
		}

		internal int HeaderCount
		{
			get 
			{
				if (_Header == null)
					return 0;
				else
					return _Header.TableRows.Items.Count;
			}
		}

		internal Footer Footer
		{
			get { return  _Footer; }
			set {  _Footer = value; }
		}

		internal int FooterCount
		{
			get 
			{
				if (_Footer == null)
					return 0;
				else
					return _Footer.TableRows.Items.Count;
			}
		}

		internal Visibility Visibility
		{
			get { return  _Visibility; }
			set {  _Visibility = value; }
		}

		internal Textbox ToggleTextbox
		{
			get { return  _ToggleTextbox; }
		}

		// See Table.RunGroupKeptTogether.
		internal bool KeepTogether
		{
			get { return  _KeepTogether; }
			set {  _KeepTogether = value; }
		}
	}
}
