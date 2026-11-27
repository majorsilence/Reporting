using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// The document map of HTML output (Report.HtmlDocumentMap): the Label / DocumentMapLabel of the
	/// report items and group instances written, as a table of contents of links to anchors in the
	/// page. It follows the rules of the paged document map (Report.BeginDocumentMapScope): a label
	/// points at the first thing written after it, a labelled container or group nests what is in
	/// it one level deeper, a label with nothing written for it is dropped, an empty label is no
	/// entry and no level, and the page header and footer are left out. A hidden item and all that
	/// is in a hidden container are left out too.
	/// </summary>
	internal sealed class HtmlDocumentMap
	{
		private readonly string _idPrefix;
		private readonly List<(string Label, int Level, string Id)> _entries = new List<(string, int, string)>();
		private readonly List<(string Label, int Level)> _pending = new List<(string, int)>();
		// One per open container or group instance: where its label waits in _pending (-1 when it
		// has none), and whether it is hidden. A group's label is evaluated on the first row
		// written in it (TableRowStart), so it is null until then.
		private readonly List<Scope> _scopes = new List<Scope>();
		private int _depth;
		private int _hidden;
		private int _nextId;

		private sealed class Scope
		{
			internal int Token = -1;
			internal bool Hidden;
			internal Grouping Group;	// a group instance whose label is still to be evaluated
		}

		internal HtmlDocumentMap(string idPrefix)
		{
			_idPrefix = idPrefix ?? "";
		}

		internal int Count => _entries.Count;

		// A labelled container (table, list, matrix, rectangle) starts.
		internal async Task BeginItem(Report rpt, ReportItem ri, Row row)
		{
			var scope = new Scope();
			_scopes.Add(scope);
			if (ri.InPageHeaderOrFooter())
				return;
			if (await ri.IsHidden(rpt, row).ConfigureAwait(false))
			{
				scope.Hidden = true;
				_hidden++;
				return;
			}
			if (ri.Label != null)
				Begin(scope, await ri.Label.EvaluateString(rpt, row).ConfigureAwait(false));
		}

		// A group instance starts; its label waits for the instance's first row (ResolveGroups).
		internal void BeginGroup(Grouping g)
		{
			_scopes.Add(new Scope { Group = g?.Label != null ? g : null });
		}

		// A group instance whose row is known at its start (a list entry).
		internal async Task BeginGroup(Report rpt, Grouping g, Row row)
		{
			var scope = new Scope();
			_scopes.Add(scope);
			if (g?.Label != null && row != null)
				Begin(scope, await g.Label.EvaluateString(rpt, row).ConfigureAwait(false));
		}

		// The group instances begun since their last row: their labels are evaluated on this row,
		// the first written in each.
		internal async Task ResolveGroups(Report rpt, Row row)
		{
			if (row == null)
				return;
			foreach (Scope scope in _scopes)
			{
				if (scope.Group == null)
					continue;
				Grouping g = scope.Group;
				scope.Group = null;
				Begin(scope, await g.Label.EvaluateString(rpt, row).ConfigureAwait(false));
			}
		}

		private void Begin(Scope scope, string label)
		{
			if (_hidden > 0 || string.IsNullOrEmpty(label))
				return;
			scope.Token = _pending.Count;
			_pending.Add((label, _depth));
			_depth++;
		}

		// The container or group instance last begun ends.
		internal void End()
		{
			if (_scopes.Count == 0)
				return;
			Scope scope = _scopes[_scopes.Count - 1];
			_scopes.RemoveAt(_scopes.Count - 1);
			if (scope.Hidden)
				_hidden--;
			if (scope.Token < 0)
				return;
			_depth = Math.Max(0, _depth - 1);
			if (_pending.Count > scope.Token)		// nothing was written for it
				_pending.RemoveRange(scope.Token, _pending.Count - scope.Token);
		}

		// Something is written for ri (its own label counted when it is not a container): returns
		// the id to give its anchor, or null when no label points at it.
		internal async Task<string> Anchor(Report rpt, ReportItem ri, Row row, bool ownLabel)
		{
			if (ri != null && ri.InPageHeaderOrFooter())
				return null;
			await ResolveGroups(rpt, row).ConfigureAwait(false);
			if (_hidden > 0)
				return null;
			string own = null;
			if (ownLabel && ri?.Label != null && !await ri.IsHidden(rpt, row).ConfigureAwait(false))
				own = await ri.Label.EvaluateString(rpt, row).ConfigureAwait(false);
			if (_pending.Count == 0 && string.IsNullOrEmpty(own))
				return null;

			string id = _idPrefix + "dm" + (_nextId++).ToString(System.Globalization.CultureInfo.InvariantCulture);
			foreach (var (label, level) in _pending)
				_entries.Add((label, level, id));
			_pending.Clear();
			if (!string.IsNullOrEmpty(own))
				_entries.Add((own, _depth, id));
			return id;
		}

		// The table of contents: a nested list of links, each entry at most one level below the one
		// before it.
		internal void Write(TextWriter tw)
		{
			tw.WriteLine("<nav class='{0}documentmap' aria-label='Document map'>", _idPrefix);
			int open = 0;		// lists open
			bool itemOpen = false;
			foreach (var (label, level, id) in _entries)
			{
				int target = Math.Min(level, open == 0 ? 0 : open);	// at most one deeper than the last
				if (open == 0)
				{
					tw.Write("<ul>");
					open = 1;
				}
				else if (target + 1 > open)
				{
					tw.Write("<ul>");
					open++;
					itemOpen = false;
				}
				else
				{
					if (itemOpen)
						tw.Write("</li>");
					while (open > target + 1)
					{
						tw.Write("</ul></li>");
						open--;
					}
				}
				tw.Write("<li><a href='#{0}'>{1}</a>", id, XmlUtil.XmlAnsi(label));
				itemOpen = true;
			}
			if (itemOpen)
				tw.Write("</li>");
			while (open > 0)
			{
				tw.Write("</ul>");
				if (--open > 0)
					tw.Write("</li>");
			}
			tw.WriteLine();
			tw.WriteLine("</nav>");
		}
	}
}
