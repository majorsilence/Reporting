using System;

namespace Majorsilence.Reporting.Rdl
{
	public class PageItem : ICloneable, IComparable
	{
		Page parent;            // parent page
		float x;                // x coordinate
		float y;                // y coordinate
		float h;                // height  --- line redefines as Y2
		float w;                // width   --- line redefines as X2
		string hyperlink;       //  a hyperlink the object should link to
		string bookmarklink;	//  a hyperlink within the report object should link to
		string bookmark;        //  bookmark text for this pageItem
		string tooltip;			//  a message to display when user hovers with mouse
		int zindex;             // zindex; items will be sorted by this
		int itemNumber;         //  original number of item
		StyleInfo si;			// all the style information evaluated
		bool allowselect = true;  // allow selection of this item
		string toggleKey;		//  the toggle instance this item is, when it is a visibility toggle textbox
		bool toggleExpanded;	//  whether the items it toggles are shown

		/// <summary>
		/// How far, in points, an interactive viewer moves a toggle textbox's text right to draw
		/// its +/- glyph before it. The page item is laid out without it.
		/// </summary>
		public const float ToggleGlyphSpace = 10f;
		/// <summary>The side of the +/- glyph, in points.</summary>
		public const float ToggleGlyphSize = 7f;

		public Page Page
		{
			get { return parent; }
			set { parent = value; }
		}

		public bool AllowSelect
		{
			get { return allowselect; }
			set { allowselect = value; }
		}
		public float X
		{
			get { return x; }
			set { x = value; }
		}

		public float Y
		{
			get { return y; }
			set { y = value; }
		}

		public int ZIndex
		{
			get { return zindex; }
			set { zindex = value; }
		}

		public int ItemNumber
		{
			get { return itemNumber; }
			set { itemNumber = value; }
		}

		public float H
		{
			get { return h; }
			set { h = value; }
		}

		public float W
		{
			get { return w; }
			set { w = value; }
		}

		public string HyperLink
		{
			get { return hyperlink; }
			set { hyperlink = value; }
		}

		public string BookmarkLink
		{
			get { return bookmarklink; }
			set { bookmarklink = value; }
		}

		public string Bookmark
		{
			get { return bookmark; }
			set { bookmark = value; }
		}

		public string Tooltip
		{
			get { return tooltip; }
			set { tooltip = value; }
		}

		public StyleInfo SI
		{
			get { return si; }
			set { si = value; }
		}

		/// <summary>
		/// Non-null when this item is an instance of a textbox named as a Visibility/ToggleItem:
		/// pass it to <see cref="Report.Toggle"/> to expand or collapse what it toggles, then build
		/// the pages again.
		/// </summary>
		public string ToggleKey
		{
			get { return toggleKey; }
			set { toggleKey = value; }
		}

		/// <summary>
		/// For a toggle (<see cref="ToggleKey"/> not null): true when the items it toggles are
		/// shown (draw a minus), false when they are hidden (draw a plus).
		/// </summary>
		public bool ToggleExpanded
		{
			get { return toggleExpanded; }
			set { toggleExpanded = value; }
		}

		/// <summary>
		/// Where an interactive viewer draws the +/- glyph of a toggle, in points on the page: a
		/// square of <see cref="ToggleGlyphSize"/> inside the left padding, in the first
		/// <see cref="ToggleGlyphSpace"/> of the text's room (the viewer draws the text that much
		/// further right), level with the first line.
		/// </summary>
		public void GetToggleGlyphBounds(out float x, out float y, out float size)
		{
			size = ToggleGlyphSize;
			float padLeft = si != null ? si.PaddingLeft : 0;
			float padTop = si != null ? si.PaddingTop : 0;
			float padBottom = si != null ? si.PaddingBottom : 0;
			float line = si != null ? si.FontSize * 1.2f : 12f;
			float room = Math.Min(h - padTop - padBottom, line);
			x = this.x + padLeft + (ToggleGlyphSpace - size) / 2;
			y = this.y + padTop + Math.Max(0, (room - size) / 2);
		}
		#region ICloneable Members

		public object Clone()
		{
			return this.MemberwiseClone();
		}

		#endregion
		#region IComparable Members

		// Sort items based on zindex, then on order items were added to array
		public int CompareTo(object obj)
		{
			PageItem pi = obj as PageItem;

			int rc = this.zindex - pi.zindex;
			if (rc == 0)
				rc = this.itemNumber - pi.itemNumber;
			return rc;
		}

		internal void Dispose()
		{
			si = null;
			parent = null;
		}

		#endregion
	}
}