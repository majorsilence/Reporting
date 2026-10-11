

using System;
using System.Xml;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Definition of the filter on a DataSet.  If boolean expression evaluates to false
	/// then row is not added to DataSet.
	///</summary>
	[Serializable]
	internal class Filter : ReportLink
	{
		Expression _FilterExpression;	//(Variant)
						//An expression that is evaluated for each
						//instance within the group or each row of the
						//data set or data region and compared (via the
						//Operator) to the FilterValues. Failed
						//comparisons result in the row/instance being
						//filtered out of the data set, data region or
						//grouping. See Filter Expression Restrictions
						//below.
		FilterOperatorEnum _FilterOperator; 
						//Notes: Top and Bottom operators include ties
						//in the resulting data. string comparisons are
						//locale-dependent. Null equals Null.
		FilterValues _FilterValues;	// The values to compare to the FilterExpression.
						//For Equal, Like, NotEqual, GreaterThan,
						//GreaterThanOrEqual, LessThan, LessThanOrEqual, TopN, BottomN,
						//TopPercent and BottomPercent, there must be
						//exactly one FilterValue
		
						//For TopN and BottomN, the FilterValue
						//expression must evaluate to an integer.
		
						//For TopPercent and BottomPercent, the
						//FilterValue expression must evaluate to an
						//integer or float.1
						
						//For Between, there must be exactly two FilterValue elements.
						
						//For In, the FilterValues are treated as a set (if
						//the FilterExpression value appears anywhere in
						//the set of FilterValues, the instance is not
						//filtered out.)
						
						//Like uses the same special characters as the
						//Visual Basic LIKE operator (e.g. �?� to
						//represent a single character and �*� to
						//represent any series of characers). See
						//http://msdn.microsoft.com/library/enus/vblr7/html/vaoprlike.asp.	
			bool _FilterOperatorSingleRow;	// false for Top/Bottom N and Percent; otherwise true
		internal Filter(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_FilterExpression=null;
			_FilterOperator=FilterOperatorEnum.Unknown;
			_FilterValues=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "FilterExpression":
						_FilterExpression = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					case "Operator":
						_FilterOperator = Rdl.FilterOperator.GetStyle(xNodeLoop.InnerText);
						if (_FilterOperator == FilterOperatorEnum.Unknown)
							OwnerReport.rl.LogError(8, "Unknown Filter operator '" + xNodeLoop.InnerText + "'.");
						break;
					case "FilterValues":
						_FilterValues = new FilterValues(r, this, xNodeLoop);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Filter element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_FilterExpression == null)
				OwnerReport.rl.LogError(8, "Filter requires the FilterExpression element.");
			if (_FilterValues == null)
			{
				OwnerReport.rl.LogError(8, "Filter requires the FilterValues element.");
				return;		// some of the filter operator checks require values
			}
			_FilterOperatorSingleRow = true;
			switch (_FilterOperator)
			{
				case FilterOperatorEnum.Like:
				case FilterOperatorEnum.Equal:
				case FilterOperatorEnum.NotEqual:
				case FilterOperatorEnum.GreaterThan:
				case FilterOperatorEnum.GreaterThanOrEqual:
				case FilterOperatorEnum.LessThan:
				case FilterOperatorEnum.LessThanOrEqual:
					if (_FilterValues.Items.Count != 1)
						OwnerReport.rl.LogError(8, "Filter Operator requires exactly 1 FilterValue.");
					break;
				case FilterOperatorEnum.TopN:
				case FilterOperatorEnum.BottomN:
				case FilterOperatorEnum.TopPercent:
				case FilterOperatorEnum.BottomPercent:
					_FilterOperatorSingleRow = false;
					if (_FilterValues.Items.Count != 1)
						OwnerReport.rl.LogError(8, "Filter Operator requires exactly 1 FilterValue.");
					break;
				case FilterOperatorEnum.In:
					break;
				case FilterOperatorEnum.Between:
					if (_FilterValues.Items.Count != 2)
						OwnerReport.rl.LogError(8, "Filter Operator Between requires exactly 2 FilterValues.");
					break;
				default:		
					OwnerReport.rl.LogError(8, "Valid Filter operator must be specified.");
					break;
			}
		}

		// Handle parsing of function in final pass
		async override internal Task FinalPass()
		{
            await _FilterExpression.FinalPass().ConfigureAwait(false);
            await _FilterValues.FinalPass().ConfigureAwait(false);
			return;
		}

		// Apply the filters to a row to determine if row is valid
		internal async Task<bool> Apply(Report rpt, Row datarow)
		{
			object left = await _FilterExpression.Evaluate(rpt, datarow).ConfigureAwait(false);
			TypeCode tc = _FilterExpression.GetTypeCode();
			object right = await ((FilterValue)(_FilterValues.Items[0])).Expression.Evaluate(rpt, datarow).ConfigureAwait(false);
			switch (_FilterOperator)
			{
				case FilterOperatorEnum.Equal:
					return ApplyCompare(tc, left, right) == 0? true: false;
				case FilterOperatorEnum.Like:	// TODO - this is really regex (not like)
					if (left == null || right == null)
						return false;
					string s1 = Convert.ToString(left);
					string s2 = Convert.ToString(right);
					return Regex.IsMatch(s1, s2);
				case FilterOperatorEnum.NotEqual:
					return ApplyCompare(tc, left, right) == 0? false: true;
				case FilterOperatorEnum.GreaterThan:
					return ApplyCompare(tc, left, right) > 0? true: false;
				case FilterOperatorEnum.GreaterThanOrEqual:
					return ApplyCompare(tc, left, right) >= 0? true: false;
				case FilterOperatorEnum.LessThan:
					return ApplyCompare(tc, left, right) < 0? true: false;
				case FilterOperatorEnum.LessThanOrEqual:
					return ApplyCompare(tc, left, right) <= 0? true: false;
				case FilterOperatorEnum.TopN:
				case FilterOperatorEnum.BottomN:
				case FilterOperatorEnum.TopPercent:
				case FilterOperatorEnum.BottomPercent:
					return true;		// This is handled elsewhere
				case FilterOperatorEnum.In:
					foreach (FilterValue fv in _FilterValues.Items)
					{
						right = await fv.Expression.Evaluate(rpt, datarow).ConfigureAwait(false);
                        if (right is ArrayList)         // this can only happen with MultiValue parameters
                        {   // check each object in the array
                            foreach (object v in right as ArrayList)
                            {
                                if (ApplyCompare(tc, left, v) == 0)
                                    return true;
                            }
                        }
						else if (ApplyCompare(tc, left, right) == 0)
							return true;
					}
					return false;
				case FilterOperatorEnum.Between:
					if (ApplyCompare(tc, left, right) < 0)
						return false;
					right = await ((FilterValue)(_FilterValues.Items[1])).Expression.Evaluate(rpt, datarow).ConfigureAwait(false);
					return ApplyCompare(tc, left, right) <= 0? true: false;
				default:
					return true;
			}
		}

		internal async Task Apply(Report rpt, Rows data)
		{
			if (this._FilterOperatorSingleRow)
                await ApplySingleRowFilter(rpt, data).ConfigureAwait(false);
			else
                await ApplyTopBottomFilter(rpt, data).ConfigureAwait(false);
		}

		private async Task ApplySingleRowFilter(Report rpt, Rows data)
		{
			List<Row> ar = data.Data;
			// handle a single row operator; by looping thru the rows and applying
			//   the filter
			int iRow = 0;
			while (iRow < ar.Count)
			{
				Row datarow = ar[iRow];
				if (await Apply(rpt, datarow).ConfigureAwait(false))
					iRow++;
				else
					ar.RemoveAt(iRow);
			}
			return;
		}

		private async Task ApplyTopBottomFilter(Report rpt, Rows data)
		{
			if (data.Data.Count <= 0)		// No data; nothing to do
				return;

			// Get the filter value and validate it 
			FilterValue fv = this._FilterValues.Items[0];
			double val = await fv.Expression.EvaluateDouble(rpt, data.Data[0]).ConfigureAwait(false);
			if (val <= 0)			// if less than equal 0; then request results in no data
			{
				data.Data.Clear();
				return;
			}

			// Calculate the row number of the affected item and do additional validation
			int ival;
			if (_FilterOperator == FilterOperatorEnum.TopN ||
				_FilterOperator == FilterOperatorEnum.BottomN)
			{
				ival = (int) val;
				if (ival != val)
					throw new Exception(string.Format(Strings.Filter_Error_TopNAndBottomNRequireInt, val));
				if (ival >= data.Data.Count)		// includes all the data?
					return;
				ival--;					// make zero based
			}
			else
			{
				if (val >= 100)			// greater than 100% means all the data
					return;
				ival = (int) (data.Data.Count * (val/100));
				if (ival <= 0)			// if less than equal 0; then request results in no data
				{
					data.Data.Clear();
					return;
				}
				if (ival >= data.Data.Count)	// make sure rounding hasn't forced us past 100%
					return;
				ival--;					// make zero based
			}

			// Sort the data by the FilterExpression
            List<RowsSortExpression> sl = new List<RowsSortExpression>();
			sl.Add(new RowsSortExpression(this._FilterExpression));
			data.SortBy = sl;					// update the sort by
			await data.SortAsync().ConfigureAwait(false);			// sort the data
			
			// reverse the order of the data for top so that data is in the beginning
			if (_FilterOperator == FilterOperatorEnum.TopN ||
				_FilterOperator == FilterOperatorEnum.TopPercent)
				data.Data.Reverse();

			List<Row> ar = data.Data;
			TypeCode tc = _FilterExpression.GetTypeCode();
			object o = await _FilterExpression.Evaluate(rpt, data.Data[ival]).ConfigureAwait(false);

			// adjust the ival based on duplicate values
			ival++;
			while (ival < ar.Count)
			{
				object n = await _FilterExpression.Evaluate(rpt, data.Data[ival]).ConfigureAwait(false);
				if (ApplyCompare(tc, o, n) != 0)
					break;
				ival++;
			}
			if (ival < ar.Count)	// if less than we need to remove the rest of the rows
			{
				ar.RemoveRange(ival, ar.Count - ival);
			}			
			return;
		}

		static internal int ApplyCompare(TypeCode tc, object left, object right)
		{
			//Because double.NaN = DBNull for numbers
			if(left == null || (left is double && double.IsNaN((double)left)))
			{
				return (right == null || (right is double && double.IsNaN((double)right))) ? 0: -1;
			}
			if (right == null || (right is double && double.IsNaN((double)right)))
				return 1;

			try
			{
				switch (tc)
				{
					case TypeCode.DateTime:
						return ((DateTime) left).CompareTo(Convert.ToDateTime(right));
                    case TypeCode.Int16:
						return ((short) left).CompareTo(Convert.ToInt16(right));
					case TypeCode.UInt16:
						return ((ushort) left).CompareTo(Convert.ToUInt16(right));
					case TypeCode.Int32:
						return ((int) left).CompareTo(Convert.ToInt32(right));
					case TypeCode.UInt32:
						return ((uint) left).CompareTo(Convert.ToUInt32(right));
					case TypeCode.Int64:
						return ((long) left).CompareTo(Convert.ToInt64(right));
					case TypeCode.UInt64:
						return ((ulong) left).CompareTo(Convert.ToUInt64(right));
					case TypeCode.String:
						return ((string) left).CompareTo(Convert.ToString(right));
					case TypeCode.Decimal:
						return ((Decimal) left).CompareTo(Convert.ToDecimal(right, NumberFormatInfo.InvariantInfo));
					case TypeCode.Single:
						return ((float) left).CompareTo(Convert.ToSingle(right, NumberFormatInfo.InvariantInfo));
					case TypeCode.Double:
						return ((double) left).CompareTo(Convert.ToDouble(right, NumberFormatInfo.InvariantInfo));
					case TypeCode.Boolean:
						return ((bool) left).CompareTo(Convert.ToBoolean(right));
					case TypeCode.Char:
						return ((char) left).CompareTo(Convert.ToChar(right));
					case TypeCode.SByte:
						return ((sbyte) left).CompareTo(Convert.ToSByte(right));
					case TypeCode.Byte:
						return ((byte) left).CompareTo(Convert.ToByte(right));
					case TypeCode.Empty:
					case TypeCode.DBNull:
						if (right == null)
							return 0;
						else
							return -1;
					default:	// ok we do this based on the actual type of the arguments
						return ApplyCompare(left, right);
				}
			}
			catch
			{	// do based on actual type of arguments
				return ApplyCompare(left, right);
			}
		}

		static internal int ApplyCompare(object left, object right) 
		{
			if (left is string)
				return ((string) left).CompareTo(Convert.ToString(right));
			if (left is decimal)
				return ((Decimal) left).CompareTo(Convert.ToDecimal(right, NumberFormatInfo.InvariantInfo));
			if (left is Single)
				return ((float) left).CompareTo(Convert.ToSingle(right, NumberFormatInfo.InvariantInfo));
			if (left is double)
				return ((double) left).CompareTo(Convert.ToDouble(right, NumberFormatInfo.InvariantInfo));
			if (left is DateTime)
				return ((DateTime) left).CompareTo(Convert.ToDateTime(right));
			if (left is short)
				return ((short) left).CompareTo(Convert.ToInt16(right));
			if (left is ushort)
				return ((ushort) left).CompareTo(Convert.ToUInt16(right));
			if (left is int)
				return ((int) left).CompareTo(Convert.ToInt32(right));
			if (left is uint)
				return ((uint) left).CompareTo(Convert.ToUInt32(right));
			if (left is long)
				return ((long) left).CompareTo(Convert.ToInt64(right));
			if (left is ulong)
				return ((ulong) left).CompareTo(Convert.ToUInt64(right));
			if (left is bool)
				return ((bool) left).CompareTo(Convert.ToBoolean(right));
			if (left is char)
				return ((char) left).CompareTo(Convert.ToChar(right));
			if (left is sbyte)
				return ((sbyte) left).CompareTo(Convert.ToSByte(right));
			if (left is byte)
				return ((byte) left).CompareTo(Convert.ToByte(right));
			if (left is DBNull)
			{
				if (right == null)
					return 0;
				else
					return -1;
			}
			return 0;
		}

		internal Expression FilterExpression
		{
			get { return  _FilterExpression; }
			set {  _FilterExpression = value; }
		}

		internal FilterOperatorEnum FilterOperator
		{
			get { return  _FilterOperator; }
			set {  _FilterOperator = value; }
		}

		internal FilterValues FilterValues
		{
			get { return  _FilterValues; }
			set {  _FilterValues = value; }
		}

		internal bool FilterOperatorSingleRow
		{
			get { return  _FilterOperatorSingleRow; }
		}
	}
}
