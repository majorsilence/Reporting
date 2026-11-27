using System;
using System.Collections.Generic;
using System.Text;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>The direction an interactive sort (a textbox's UserSort) is in.</summary>
	public enum UserSortDirection
	{
		/// <summary>Not sorted by this textbox: the report's own order.</summary>
		None,
		Ascending,
		Descending
	}

	/// <summary>
	/// The interactive sorts (RDL UserSort) a user has clicked, for one report run. Each entry is a
	/// sortable textbox's <see cref="PageItem.UserSortKey"/> and its direction, kept in the order
	/// the textboxes were first clicked: the first is the primary sort, the next breaks its ties,
	/// and so on, ahead of the report's own sort expressions (as SSRS does with shift-click).
	/// </summary>
	/// <remarks>
	/// It is not part of the report definition: set it on <see cref="Report.UserSort"/>, change it,
	/// and build the pages again (<see cref="Report.BuildPages"/>) to lay the report out in the new
	/// order. A parsed or compiled report shared between runs is not changed by it.
	/// </remarks>
	public sealed class UserSortState
	{
		readonly List<KeyValuePair<string, UserSortDirection>> _Entries = new List<KeyValuePair<string, UserSortDirection>>();
		readonly object _Lock = new object();

		/// <summary>
		/// A click on the sortable textbox <paramref name="key"/>: ascending the first time, then
		/// descending, ascending... Unless <paramref name="additive"/> (a shift-click), it becomes
		/// the only sort; otherwise it is added after the sorts already clicked, or a sort already
		/// there keeps its place and flips. Returns the textbox's new direction.
		/// </summary>
		public UserSortDirection Toggle(string key, bool additive = false)
		{
			if (key == null)
				return UserSortDirection.None;
			lock (_Lock)
			{
				int i = IndexOf(key);
				UserSortDirection next = i >= 0 && _Entries[i].Value == UserSortDirection.Ascending
					? UserSortDirection.Descending : UserSortDirection.Ascending;
				if (!additive)
				{
					_Entries.Clear();
					i = -1;
				}
				if (i >= 0)
					_Entries[i] = new KeyValuePair<string, UserSortDirection>(key, next);
				else
					_Entries.Add(new KeyValuePair<string, UserSortDirection>(key, next));
				return next;
			}
		}

		/// <summary>
		/// Sorts by the textbox <paramref name="key"/> in <paramref name="direction"/>, keeping its
		/// place if it is already sorted, else after the others; <see cref="UserSortDirection.None"/>
		/// removes it. For a caller restoring a sort it was told about, e.g. from an HTML link.
		/// </summary>
		public void Set(string key, UserSortDirection direction)
		{
			if (key == null)
				return;
			lock (_Lock)
			{
				int i = IndexOf(key);
				if (direction == UserSortDirection.None)
				{
					if (i >= 0)
						_Entries.RemoveAt(i);
				}
				else if (i >= 0)
					_Entries[i] = new KeyValuePair<string, UserSortDirection>(key, direction);
				else
					_Entries.Add(new KeyValuePair<string, UserSortDirection>(key, direction));
			}
		}

		/// <summary>The direction the textbox <paramref name="key"/> sorts in; None when it does not.</summary>
		public UserSortDirection GetDirection(string key)
		{
			if (key == null)
				return UserSortDirection.None;
			lock (_Lock)
			{
				int i = IndexOf(key);
				return i >= 0 ? _Entries[i].Value : UserSortDirection.None;
			}
		}

		/// <summary>
		/// The direction a click on <paramref name="key"/> would give: descending when it is
		/// ascending now, else ascending. For a link that carries the direction to sort in.
		/// </summary>
		public UserSortDirection NextDirection(string key)
			=> GetDirection(key) == UserSortDirection.Ascending ? UserSortDirection.Descending : UserSortDirection.Ascending;

		/// <summary>Removes every sort: the report is in its own order again.</summary>
		public void Clear()
		{
			lock (_Lock)
				_Entries.Clear();
		}

		/// <summary>True when nothing is sorted.</summary>
		public bool IsEmpty
		{
			get
			{
				lock (_Lock)
					return _Entries.Count == 0;
			}
		}

		/// <summary>The sorts, primary first.</summary>
		public IReadOnlyList<KeyValuePair<string, UserSortDirection>> Entries
		{
			get
			{
				lock (_Lock)
					return _Entries.ToArray();
			}
		}

		/// <summary>
		/// The state as text, to carry it between requests (an HTML link, a query string):
		/// "A" or "D" and the URI-escaped key, comma separated, primary first. <see cref="Parse"/>
		/// reads it back.
		/// </summary>
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var e in Entries)
			{
				if (sb.Length > 0)
					sb.Append(',');
				sb.Append(e.Value == UserSortDirection.Descending ? 'D' : 'A');
				sb.Append(Uri.EscapeDataString(e.Key));
			}
			return sb.ToString();
		}

		/// <summary>Reads the text <see cref="ToString"/> writes; anything it does not understand is skipped.</summary>
		public static UserSortState Parse(string text)
		{
			var state = new UserSortState();
			if (string.IsNullOrEmpty(text))
				return state;
			foreach (string part in text.Split(','))
			{
				if (part.Length < 2 || (part[0] != 'A' && part[0] != 'D'))
					continue;
				string key;
				try
				{
					key = Uri.UnescapeDataString(part.Substring(1));
				}
				catch (UriFormatException)
				{
					continue;
				}
				state.Set(key, part[0] == 'D' ? UserSortDirection.Descending : UserSortDirection.Ascending);
			}
			return state;
		}

		int IndexOf(string key)
		{
			for (int i = 0; i < _Entries.Count; i++)
				if (string.Equals(_Entries[i].Key, key, StringComparison.Ordinal))
					return i;
			return -1;
		}
	}
}
