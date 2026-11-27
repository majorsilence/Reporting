

using System;
using System.Collections;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Runtime data structure representing the group hierarchy
	///</summary>
	internal class GroupEntry
	{
		Grouping _Group;		// Group this represents
		List<SortBy> _Sort;		// Sort cooresponding to this group (with any interactive sort; see Report.EffectiveSort)
		int _StartRow;			// Starting row of the group (inclusive)
		int _EndRow;			// Endding row of the group (inclusive)
		List<GroupEntry> _NestedGroup;	// group one hierarchy below
	
		internal GroupEntry(Grouping g, List<SortBy> s, int start)
		{
			_Group = g;
			_Sort = s;
			_StartRow = start;
			_EndRow = -1;
            _NestedGroup = new List<GroupEntry>();

			// Check to see if grouping and sorting are the same
			if (g == null || s == null)
				return;			// nothing to check if either is null

			if (s.Count == 0)
			{
				_Sort = null;
				return;
			}

			if (s.Count != g.GroupExpressions.Items.Count)
				return;

			for (int i = 0; i < s.Count; i++)
			{
				SortBy sb = s[i];

				if (sb.Direction == SortDirectionEnum.Descending)
					return;			// TODO we could optimize this 
				
				FunctionField ff = sb.SortExpression.Expr as FunctionField;
				if (ff == null || ff.GetTypeCode() != TypeCode.String)
					return;

				GroupExpression ge = g.GroupExpressions.Items[i] as GroupExpression;
				FunctionField ff2 = ge.Expression.Expr as FunctionField;
				if (ff2 == null || ff.Fld != ff2.Fld)
					return;
			}
			_Sort = null;		// we won't need to sort since the groupby will handle it correctly
		}

		internal int StartRow
		{
			get { return  _StartRow; }
			set {  _StartRow = value; }
		}

		internal int EndRow
		{
			get { return  _EndRow; }
			set {  _EndRow = value; }
		}

		internal List<GroupEntry> NestedGroup
		{
			get { return  _NestedGroup; }
			set {  _NestedGroup = value; }
		}

		internal Grouping Group
		{
			get { return  _Group; }
		}

		internal List<SortBy> Sort
		{
			get { return  _Sort; }
		}
	}

}
