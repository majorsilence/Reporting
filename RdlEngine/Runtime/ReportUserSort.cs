using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	// Interactive sorting (a textbox's UserSort, #636) for an interactive viewer or HTML.
	//
	// A sortable textbox's page item carries a UserSortKey and its current direction. A click is
	// UserSort.Toggle(key) (or ToggleUserSort); BuildPages run again lays the report out sorted.
	// Each UserSort, when the report is parsed, puts a sort expression on the levels it sorts --
	// a group, or a data region's detail rows -- and they go ahead of the level's own sort
	// expressions while the state names them. With no state the report sorts as it always did.
	public partial class Report
	{
		/// <summary>
		/// The interactive sorts to apply the next time the pages are built; null (the default) or
		/// empty for the report's own order. Supplied by the caller, as it is not part of the report.
		/// </summary>
		public UserSortState UserSort { get; set; }

		/// <summary>
		/// A click on a sortable textbox (<see cref="PageItem.UserSortKey"/>): see
		/// <see cref="UserSortState.Toggle"/>. Makes <see cref="UserSort"/> when there is none.
		/// Takes effect the next time the pages are built.
		/// </summary>
		public UserSortDirection ToggleUserSort(string key, bool additive = false)
		{
			UserSortState state = UserSort;
			if (state == null)
				UserSort = state = new UserSortState();
			return state.Toggle(key, additive);
		}

		/// <summary>The direction the sortable textbox <paramref name="key"/> is sorted in now.</summary>
		internal UserSortDirection UserSortDirectionOf(string key)
			=> UserSort?.GetDirection(key) ?? UserSortDirection.None;

		/// <summary>
		/// The sort expressions of a level for this run: the user's sorts on it (<paramref name="level"/>
		/// and <paramref name="also"/> name it: a Grouping, or a data region for its detail rows), in
		/// click order, then its own <paramref name="defined"/> ones. The defined list itself when
		/// no user sort applies; null when there are none at all.
		/// </summary>
		internal List<SortBy> EffectiveSort(Sorting defined, ReportLink level, ReportLink also = null)
		{
			List<SortBy> items = defined?.Items;
			UserSortState state = UserSort;
			if (state == null || state.IsEmpty)
				return items;
			List<UserSortBinding> bindings = null;
			Add(ref bindings, level);
			Add(ref bindings, also);
			if (bindings == null)
				return items;

			List<SortBy> result = null;
			foreach (var entry in state.Entries)
			{
				foreach (UserSortBinding b in bindings)
				{
					if (!string.Equals(b.Owner.Key, entry.Key, StringComparison.Ordinal))
						continue;
					result ??= new List<SortBy>();
					result.Add(entry.Value == UserSortDirection.Descending ? b.Descending : b.Ascending);
				}
			}
			if (result == null)
				return items;
			if (items != null)
				result.AddRange(items);
			return result;
		}

		static void Add(ref List<UserSortBinding> bindings, ReportLink level)
		{
			var all = level?.OwnerReport.UserSortBindings;
			if (all == null || !all.TryGetValue(level, out List<UserSortBinding> found))
				return;
			if (bindings == null)
				bindings = found;
			else
			{
				bindings = new List<UserSortBinding>(bindings);
				bindings.AddRange(found);
			}
		}
	}
}
