using System;

namespace Majorsilence.Reporting.Rdl
{
	public class PageText : PageItem, ICloneable
	{
		string text;            // the text
		float descent;          // in some cases the Font descent will be recorded; 0 otherwise
		bool bGrow;
		bool _NoClip = false;		// on drawing disallow clipping
		bool _ClipSides = false;	// on drawing cut a line too wide for the box at its sides
		PageTextHtml _HtmlParent = null;

		public PageText(string t)
		{
			text = t;
			descent = 0;
			bGrow = false;
		}

		public PageTextHtml HtmlParent
		{
			get { return _HtmlParent; }
			set { _HtmlParent = value; }
		}

		public string Text
		{
			get { return text; }
			set { text = value; }
		}

		public float Descent
		{
			get { return descent; }
			set { descent = value; }
		}
		public bool NoClip
		{
			get { return _NoClip; }
			set { _NoClip = value; }
		}

		internal bool ClipSides
		{
			get { return _ClipSides; }
			set { _ClipSides = value; }
		}

		public bool CanGrow
		{
			get { return bGrow; }
			set { bGrow = value; }
		}
		#region ICloneable Members

		new public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion
	}
}