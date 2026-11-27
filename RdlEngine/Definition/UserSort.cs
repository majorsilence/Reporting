using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// A textbox's UserSort: the user can sort by clicking it (an interactive sort, #636). The same
	/// in RDL 2005 and 2008+: SortExpression, and optionally SortExpressionScope and SortTarget.
	///</summary>
	/// <remarks>
	/// The final pass works out what it sorts and gives each level a sort expression of its own,
	/// parsed where the level's own sort expressions are (so an aggregate in it is over the group):
	/// the group SortExpressionScope names, or with none the detail rows, in the data region the
	/// textbox is in and in the one SortTarget names. While <see cref="Report.UserSort"/> names the
	/// textbox, those go ahead of the level's own sort expressions (Report.EffectiveSort).
	/// </remarks>
	[Serializable]
	internal class UserSort : ReportLink
	{
		string _SortExpression;			// the expression to sort by (source)
		string _SortExpressionScope;	// the group (or data region) to evaluate it in, and sort; detail rows when null
		string _SortTarget;				// another data region (or group) the sort applies to as well
		readonly Textbox _Textbox;

		internal UserSort(ReportDefn r, Textbox p, XmlNode xNode) : base(r, p)
		{
			_Textbox = p;
			foreach (XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "sortexpression":
						_SortExpression = xNodeLoop.InnerText;
						break;
					case "sortexpressionscope":
						_SortExpressionScope = NullIfBlank(xNodeLoop.InnerText);
						break;
					case "sorttarget":
						_SortTarget = NullIfBlank(xNodeLoop.InnerText);
						break;
					default:
						OwnerReport.rl.LogError(4, "Unknown UserSort element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (string.IsNullOrWhiteSpace(_SortExpression))
			{
				_SortExpression = null;
				OwnerReport.rl.LogError(8, $"UserSort on textbox '{TextboxName}' requires the SortExpression element.");
			}
		}

		static string NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

		string TextboxName => _Textbox.Name?.Nm ?? "unnamed";

		/// <summary>
		/// The name a viewer or <see cref="UserSortState"/> knows this sort by: the textbox's name,
		/// after the names of the subreports it is in.
		/// </summary>
		internal string Key
		{
			get
			{
				string key = _Textbox.Name?.Nm ?? "";
				for (Subreport s = OwnerReport.Subreport; s != null; s = s.OwnerReport.Subreport)
					key = (s.Name?.Nm ?? "") + "/" + key;
				return key;
			}
		}

		internal string SortExpression => _SortExpression;
		internal string SortExpressionScope => _SortExpressionScope;
		internal string SortTarget => _SortTarget;

		async override internal Task FinalPass()
		{
			if (_SortExpression == null)
				return;
			if (_Textbox.Name == null)
			{
				OwnerReport.rl.LogError(4, "UserSort on a textbox without a name is ignored: a click could not name it.");
				return;
			}

			// What SortExpressionScope names: a group, a data region (its detail rows), or nothing.
			Grouping scopeGroup = null;
			DataRegion scopeRegion = null;
			if (_SortExpressionScope != null)
			{
				object o = OwnerReport.LUAggrScope[_SortExpressionScope];
				if (o is Grouping g)
					scopeGroup = g;
				else if (o is DataRegion d)
					scopeRegion = d;
				else
					OwnerReport.rl.LogError(4, $"UserSort on textbox '{TextboxName}': SortExpressionScope '{_SortExpressionScope}' is not a group or data region; the detail rows are sorted.");
			}

			DataRegion target = null;
			if (_SortTarget != null)
			{
				object o = OwnerReport.LUAggrScope[_SortTarget];
				if (o is DataRegion d)
					target = d;
				else if (o is Grouping g)
					target = RegionOf(g);
				else
					OwnerReport.rl.LogError(4, $"UserSort on textbox '{TextboxName}': SortTarget '{_SortTarget}' is not a data region or group; it is ignored.");
			}

			// The regions it sorts: the one it is in, the target, and the one its scope is in.
			var regions = new List<DataRegion>();
			void AddRegion(DataRegion r)
			{
				if (r != null && !regions.Contains(r))
					regions.Add(r);
			}
			AddRegion(RegionOf(_Textbox));
			AddRegion(target);
			AddRegion(scopeGroup != null ? RegionOf(scopeGroup) : scopeRegion);
			if (regions.Count == 0)
			{
				OwnerReport.rl.LogError(4, $"UserSort on textbox '{TextboxName}' is not in a data region and has no SortTarget; it sorts nothing.");
				return;
			}

			// In the region its scope group is in, that group's instances are sorted; in any other,
			// the detail rows (within each instance of the groups around them).
			foreach (DataRegion region in regions)
			{
				Grouping g = scopeGroup != null && RegionOf(scopeGroup) == region ? scopeGroup : null;
				await Bind(region, g).ConfigureAwait(false);
			}
		}

		async Task Bind(DataRegion region, Grouping g)
		{
			ReportLink context;		// where the region's own sort expressions for that level are
			if (!(region is Table || region is List || region is Matrix))
			{
				OwnerReport.rl.LogError(4, $"UserSort on textbox '{TextboxName}' cannot sort '{region.Name?.Nm}': only a table, list, matrix or tablix can be sorted.");
				return;
			}
			if (g != null)
				context = g.Parent;		// TableGroup, Details, List, DynamicRows/Columns, or a tablix member
			else if (region is Table t)
				context = t.Details;
			else if (region is List)
				context = region;
			else if (region is TablixPivot)
				context = region;
			else
				context = null;			// a 2005 Matrix has no detail rows to sort
			if (context == null)
			{
				OwnerReport.rl.LogError(4, $"UserSort on textbox '{TextboxName}' cannot sort '{region.Name?.Nm}': it has no detail rows; name a group in SortExpressionScope.");
				return;
			}

			var ascending = new SortBy(OwnerReport, context, _SortExpression, SortDirectionEnum.Ascending);
			await ascending.FinalPass().ConfigureAwait(false);
			var binding = new UserSortBinding(this, ascending, new SortBy(ascending, SortDirectionEnum.Descending));

			ReportLink level = (ReportLink) g ?? region;
			OwnerReport.UserSortBindings ??= new Dictionary<ReportLink, List<UserSortBinding>>();
			if (!OwnerReport.UserSortBindings.TryGetValue(level, out List<UserSortBinding> list))
				OwnerReport.UserSortBindings[level] = list = new List<UserSortBinding>();
			list.Add(binding);
		}

		/// <summary>The nearest data region around l; null when it is in none.</summary>
		internal static DataRegion RegionOf(ReportLink l)
		{
			for (ReportLink p = l?.Parent; p != null; p = p.Parent)
			{
				if (p is DataRegion d)
					return d;
			}
			return null;
		}
	}

	/// <summary>A UserSort's sort expression on one level it sorts, each way.</summary>
	[Serializable]
	internal sealed class UserSortBinding
	{
		internal UserSortBinding(UserSort owner, SortBy ascending, SortBy descending)
		{
			Owner = owner;
			Ascending = ascending;
			Descending = descending;
		}

		internal UserSort Owner { get; }
		internal SortBy Ascending { get; }
		internal SortBy Descending { get; }
	}
}
