using System;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Object-tolerant mirrors of System.String's static methods, under the same names and
	/// -- the part that matters -- the same argument order and the same meaning.
	/// <para>
	/// The parser binds a method by the argument types it could infer at parse time, and an
	/// argument it could not type is Object. String.Join(", ", Parameters!M.Value) is the
	/// common case: a multi-value parameter is an ArrayList, the parser calls it Object, and
	/// none of String.Join's overloads takes one, so a expression that works in SSRS did not
	/// resolve here at all.
	/// </para>
	/// <para>
	/// These live in their own class rather than in VBFunctions on purpose. VBFunctions is
	/// VB's runtime library, where Join means Join(values, delimiter) -- the same name as
	/// String.Join with the arguments the other way round. Falling back there would bind the
	/// separator as the list and render the separator on its own.
	/// </para>
	/// </summary>
	sealed internal class StringFunctions
	{
		/// <summary>
		/// String.Join's shape: the separator first, then the values. The values may be an
		/// ArrayList (a multi-value parameter), any other IEnumerable, or a single value.
		/// <para>
		/// A null separator joins with nothing, as String.Join does. Null values give an
		/// empty string rather than throwing, because a report should not die over an
		/// unsupplied parameter.
		/// </para>
		/// </summary>
		static public string Join(object separator, object values)
		{
			string sep = separator == null || separator is DBNull
				? string.Empty
				: Convert.ToString(separator);

			return VBFunctions.Join(values, sep);
		}
	}
}
