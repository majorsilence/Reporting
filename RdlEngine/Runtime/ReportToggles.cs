using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	// Visibility toggles (Visibility/ToggleItem, the drill-down +/-) for an interactive viewer.
	//
	// A toggle textbox's page item carries a ToggleKey naming the instance clicked: the textbox
	// and the group instance it is in. Toggle(key) flips the hidden state of every item that
	// textbox toggles within that instance; BuildPages run again lays them out that way. With
	// nothing toggled every item starts as its Hidden expression says, so static output (PDF,
	// TIF, RTF, Excel...) is what it always was.
	public partial class Report
	{
		readonly HashSet<string> _Toggled = new HashSet<string>(StringComparer.Ordinal);
		readonly object _ToggledLock = new object();
		volatile bool _HasToggles;

		/// <summary>
		/// Set by an interactive viewer: a table group toggled by a textbox is laid out hidden or
		/// shown as its toggle says. Without it every table group is laid out, as paged output
		/// always has. Detail rows, table rows and report items follow their toggles either way
		/// (nothing is toggled unless <see cref="Toggle"/> is called).
		/// </summary>
		public bool InteractiveToggles { get; set; }

		/// <summary>
		/// Flips the hidden state of the items toggled by the toggle instance <paramref name="toggleKey"/>
		/// (a <see cref="PageItem.ToggleKey"/>). Takes effect the next time the pages are built.
		/// </summary>
		public void Toggle(string toggleKey)
		{
			if (toggleKey == null)
				return;
			lock (_ToggledLock)
			{
				if (!_Toggled.Remove(toggleKey))
					_Toggled.Add(toggleKey);
				_HasToggles = _Toggled.Count > 0;
			}
		}

		/// <summary>
		/// Whether the toggle instance <paramref name="toggleKey"/> has been clicked an odd
		/// number of times: its items show the opposite of their initial state.
		/// </summary>
		public bool IsToggled(string toggleKey)
		{
			if (toggleKey == null || !_HasToggles)
				return false;
			lock (_ToggledLock)
				return _Toggled.Contains(toggleKey);
		}

		/// <summary>
		/// Puts every toggled item back in its initial state.
		/// </summary>
		public void ClearToggles()
		{
			lock (_ToggledLock)
			{
				_Toggled.Clear();
				_HasToggles = false;
			}
		}

		internal bool HasToggles
		{
			get { return _HasToggles; }
		}
	}
}
