

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;


namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The collection of embedded images in the Report.
	///</summary>
	[Serializable]
	internal class EmbeddedImages : ReportLink
	{
        List<EmbeddedImage> _Items;			// list of EmbeddedImage

		internal EmbeddedImages(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
            _Items = new List<EmbeddedImage>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "EmbeddedImage")
				{
					EmbeddedImage ei = new EmbeddedImage(r, this, xNodeLoop);
					_Items.Add(ei);
				}
				else
					this.OwnerReport.rl.LogError(4, "Unknown Report element '" + xNodeLoop.Name + "' ignored.");
			}
			if (_Items.Count == 0)
				OwnerReport.rl.LogError(8, "For EmbeddedImages at least one EmbeddedImage is required.");
			else
                _Items.TrimExcess();
		}
		
		async override internal Task FinalPass()
		{
			foreach (EmbeddedImage ei in _Items)
			{
                await ei.FinalPass().ConfigureAwait(false);
			}
			return;
		}

        internal List<EmbeddedImage> Items
		{
			get { return  _Items; }
		}
	}
}
