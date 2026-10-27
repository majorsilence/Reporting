using System;
using System.Collections;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	public class Page : IEnumerable, IDisposable
	{
		// note: all sizes are in points
		int _pageno;
		List<PageItem> _items;              // array of items on the page
		float _yOffset;                 // current y offset; top margin, page header, other details, ... 
		float _xOffset;                 // current x offset; margin, body taken into account?
		int _emptyItems;				// # of items which constitute empty
		bool _needSort;                 // need sort
		int _lastZIndex;                // last ZIndex
		System.Collections.Generic.Dictionary<string, Rows> _PageExprReferences;    // needed to save page header/footer expressions

		public Page(int page)
		{
			_pageno = page;
			_items = new List<PageItem>();
			_emptyItems = 0;
			_needSort = false;
		}

		public PageItem this[int index]
		{
			get { return _items[index]; }
		}

		public int Count
		{
			get { return _items.Count; }
		}

		public void InsertObject(PageItem pi)
		{
			AddObjectInternal(pi);
			_items.Insert(0, pi);
		}

		public void AddObject(PageItem pi)
		{
			AddObjectInternal(pi);
			_items.Add(pi);
		}

		private void AddObjectInternal(PageItem pi)
		{
			pi.Page = this;
			pi.ItemNumber = _items.Count;
			if (_items.Count == 0)
				_lastZIndex = pi.ZIndex;
			else if (_lastZIndex != pi.ZIndex)
				_needSort = true;

			// adjust the page item locations
			pi.X += _xOffset;
			pi.Y += _yOffset;
			if (pi is PageLine)
			{
				PageLine pl = pi as PageLine;
				pl.X2 += _xOffset;
				pl.Y2 += _yOffset;
			}
			else if (pi is PagePolygon)
			{
				PagePolygon pp = pi as PagePolygon;
				for (int i = 0; i < pp.Points.Length; i++)
				{
					pp.Points[i].X += _xOffset;
					pp.Points[i].Y += _yOffset;
				}
			}
			else if (pi is PageCurve)
			{
				PageCurve pc = pi as PageCurve;
				for (int i = 0; i < pc.Points.Length; i++)
				{
					pc.Points[i].X += _xOffset;
					pc.Points[i].Y += _yOffset;
				}
			}
		}

		public bool IsEmpty()
		{
			return _items.Count > _emptyItems ? false : true;
		}

		public void SortPageItems()
		{
			if (!_needSort)
				return;
			_items.Sort();
		}

		public void ResetEmpty()
		{
			_emptyItems = 0;
		}

		public void SetEmpty()
		{
			_emptyItems = _items.Count;
		}

		public int PageNumber
		{
			get { return _pageno; }
		}

		public float XOffset
		{
			get { return _xOffset; }
			set { _xOffset = value; }
		}

		public float YOffset
		{
			get { return _yOffset; }
			set { _yOffset = value; }
		}

		internal void AddPageExpressionRow(Report rpt, string exprname, Row r)
		{
			if (exprname == null || r == null)
				return;

			if (_PageExprReferences == null)
				_PageExprReferences = new Dictionary<string, Rows>();

			Rows rows = null;
			_PageExprReferences.TryGetValue(exprname, out rows);
			if (rows == null)
			{
				rows = new Rows(rpt);
				rows.Data = new List<Row>();
				_PageExprReferences.Add(exprname, rows);
			}
			Row row = new Row(rows, r); // have to make a new copy
			row.RowNumber = rows.Data.Count;
			rows.Data.Add(row);         // add row to rows
			return;
		}

		internal Rows GetPageExpressionRows(string exprname)
		{
			if (_PageExprReferences == null)
				return null;

			Rows rows = null;
			_PageExprReferences.TryGetValue(exprname, out rows);
			return rows;
		}

		internal void ResetPageExpressions()
		{
			_PageExprReferences = null;     // clear it out; not needed once page header/footer are processed
		}

		// What the page holds at a point, so that what is added after it can be taken off again
		// and run on another page: its items, and the rows they recorded for page header and
		// footer expressions.
		internal sealed class Contents
		{
			internal int Items;
			internal Dictionary<string, int> ExpressionRows;
		}

		internal Contents MarkContents()
		{
			Contents c = new Contents { Items = _items.Count };
			if (_PageExprReferences != null)
			{
				c.ExpressionRows = new Dictionary<string, int>();
				foreach (KeyValuePair<string, Rows> kv in _PageExprReferences)
					c.ExpressionRows.Add(kv.Key, kv.Value.Data.Count);
			}
			return c;
		}

		internal void RemoveContentsAfter(Contents c)
		{
			_items.RemoveRange(c.Items, _items.Count - c.Items);
			if (_PageExprReferences == null)
				return;
			List<string> added = new List<string>();
			foreach (KeyValuePair<string, Rows> kv in _PageExprReferences)
			{
				if (c.ExpressionRows != null && c.ExpressionRows.TryGetValue(kv.Key, out int count))
					kv.Value.Data.RemoveRange(count, kv.Value.Data.Count - count);
				else
					added.Add(kv.Key);
			}
			foreach (string name in added)
				_PageExprReferences.Remove(name);
		}

		#region IEnumerable Members

		public IEnumerator GetEnumerator()      // just loop thru the pages
		{
			return _items.GetEnumerator();
		}

		public void Dispose()
		{
			foreach (var pageItems in _items)
			{
				pageItems.Dispose();
			}
			_items.Clear();
			_PageExprReferences?.Clear();
		}

		#endregion
	}
}