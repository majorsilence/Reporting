

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// A collection of rows.
	///</summary>
    internal class Rows : IDisposable
	{
		List<Row> _Data;	// array of Row object;
        List<RowsSortExpression> _SortBy;	// array of expressions used to sort the data
		GroupEntry[] _CurrentGroups;	// group
		Report _Rpt;

		internal Rows(Report rpt)
		{
			_Rpt = rpt;
			_SortBy = null;
			_CurrentGroups = null;
		}

		// Constructor that takes existing Rows; a start, end and bitArray with the rows wanted
		internal Rows(Report rpt, Rows r, int start, int end, BitArray ba)
		{
			_Rpt = rpt;
			_SortBy = null;
			_CurrentGroups = null;
			if (end - start < 0)			// null set?
			{
				_Data = new List<Row>(1);
                _Data.TrimExcess();
				return;
			}
			_Data = new List<Row>(end - start + 1);

			for (int iRow = start; iRow <= end; iRow++)
			{
				if (ba == null || ba.Get(iRow))
				{
					Row or = r.Data[iRow]; 
					Row nr = new Row(this, or);
					nr.RowNumber = or.RowNumber;
					_Data.Add(nr);
				}
			}

            _Data.TrimExcess();
		}

		// Constructor that takes existing Rows
		internal Rows(Report rpt, Rows r)
		{
			_Rpt = rpt;
			_SortBy = null;
			_CurrentGroups = null;
			if (r.Data == null || r.Data.Count <= 0)			// null set?
			{
				_Data = new List<Row>(1);
                _Data.TrimExcess();
				return;
			}
			_Data = new List<Row>(r.Data.Count);

			for (int iRow = 0; iRow < r.Data.Count; iRow++)
			{
				Row or = r.Data[iRow]; 
				Row nr = new Row(this, or);
				nr.RowNumber = or.RowNumber;
				_Data.Add(nr);
			}

            _Data.TrimExcess();
		}

        // Constructor that creates exactly one row and one column
        static internal Rows CreateOneRow(Report rpt)
        {
            Rows or = new Rows(rpt);

            or._Data = new List<Row>(1);
            Row nr = new Row(or, 1);
            nr.RowNumber = 0;
            or._Data.Add(nr);
            or._Data.TrimExcess();
            return or;
        }

		internal Rows(Report rpt, TableGroups tg, Grouping g, Sorting s)
		{
			_Rpt = rpt;
            _SortBy = new List<RowsSortExpression>();
			// Pull all the sort expression together
			if (tg != null)
			{
				foreach(TableGroup t in tg.Items)
				{
					foreach(GroupExpression ge in t.Grouping.GroupExpressions.Items)
					{
						_SortBy.Add(new RowsSortExpression(ge.Expression));
					}
					// TODO what to do with the sort expressions!!!!
				}
			}
			if (g != null)
			{
				if (g.ParentGroup != null)
					_SortBy.Add(new RowsSortExpression(g.ParentGroup));
				else if (g.GroupExpressions != null)
				{
					foreach (GroupExpression ge in g.GroupExpressions.Items)
					{
						_SortBy.Add(new RowsSortExpression(ge.Expression));
					}
				}
			}
			if (s != null)
			{
				foreach (SortBy sb in s.Items)
				{
					_SortBy.Add(new RowsSortExpression(sb.SortExpression, sb.Direction == SortDirectionEnum.Ascending));
				}
			}
            if (_SortBy.Count > 0)
            {
                _SortBy.TrimExcess();
            }
            else
            {
                _SortBy = null;
            }
		}

		internal Report Report
		{
			get {return this._Rpt;}
		}

		/// <summary>
		/// Sorts the rows by their sort expressions, evaluating each row's keys once, awaited, in order.
		/// (A comparer has to be synchronous, so sorting with one meant running every evaluation through
		/// Task.Run(...).GetResult(): a thread-pool hop per evaluation, O(n log n) of them, each blocking a
		/// pool thread - slow, and a way to starve the pool when several reports render at once.)
		/// </summary>
		internal async Task SortAsync()
		{
			if (_SortBy == null || _Data.Count < 2)
				return;     // nothing to sort by (or nothing to sort)

			int keyCount = _SortBy.Count;
			var types = new TypeCode[keyCount];
			for (int k = 0; k < keyCount; k++)
				types[k] = _SortBy[k].expr.GetTypeCode();

			var entries = new SortEntry[_Data.Count];
			for (int i = 0; i < entries.Length; i++)
			{
				Row row = _Data[i];
				var keys = new object[keyCount];
				bool failed = false;
				int k = 0;
				try
				{
					for (; k < keyCount; k++)
						keys[k] = await _SortBy[k].expr.Evaluate(_Rpt, row);
				}
				catch (Exception e)		// this really shouldn't happen
				{
					failed = true;
					_Rpt.rl.LogError(8,
						string.Format("Sort rows exception\r\nSort key: {0} of row {1}\r\n{2}\r\n{3}",
						k, row.RowNumber, e.Message, e.StackTrace));
				}
				entries[i] = new SortEntry { Row = row, Keys = keys, Failed = failed };
			}

			Array.Sort(entries, (a, b) =>
			{
				if (a.Row == b.Row)
					return 0;
				if (!a.Failed && !b.Failed)
				{
					try
					{
						for (int k = 0; k < keyCount; k++)
						{
							int rc = Filter.ApplyCompare(types[k], a.Keys[k], b.Keys[k]);
							if (rc != 0)
								return _SortBy[k].bAscending ? rc : -rc;
						}
					}
					catch (Exception e)		// this really shouldn't happen
					{
						_Rpt.rl.LogError(8,
							string.Format("Sort rows exception\r\nRows: {0} {1}\r\n{2}\r\n{3}",
							a.Row.RowNumber, b.Row.RowNumber, e.Message, e.StackTrace));
					}
				}
				return a.Row.RowNumber - b.Row.RowNumber;	// in case of tie use original row number
			});

			for (int i = 0; i < entries.Length; i++)
				_Data[i] = entries[i].Row;
		}

		private struct SortEntry
		{
			internal Row Row;
			internal object[] Keys;
			internal bool Failed;
		}

		internal List<Row> Data
		{
			get { return  _Data; }
			set 
			{ 
				_Data = value;			// Assign the new value
				foreach(Row r in _Data)	// Updata all rows
				{
					r.R = this;
				}
		
			}
		}

        internal List<RowsSortExpression> SortBy
		{
			get { return  _SortBy; }
			set { _SortBy = value; }
		}

		internal GroupEntry[] CurrentGroups
		{
			get { return  _CurrentGroups; }
			set { _CurrentGroups = value; }
		}


		public void Dispose()
		{
			_Data.Clear();
		}
	}

	class RowsSortExpression
	{
		internal Expression expr;
		internal bool bAscending;

		internal RowsSortExpression(Expression e, bool asc)
		{
			expr = e;
			bAscending = asc;
		}

		internal RowsSortExpression(Expression e)
		{
			expr = e;
			bAscending = true;
		}
	}
}
