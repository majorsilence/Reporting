using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// One node of a report's document map as a tree: an entry of <see cref="Pages.GetDocumentMap"/>
	/// and the entries nested beneath it. Viewers build their document-map panel from these.
	/// </summary>
	public sealed class DocumentMapNode
	{
		readonly List<DocumentMapNode> _children = new List<DocumentMapNode>();

		DocumentMapNode(DocumentMapEntry entry)
		{
			Entry = entry;
		}

		/// <summary>The entry: its label, and the page and place it points at.</summary>
		public DocumentMapEntry Entry { get; }

		/// <summary>The entries nested beneath this one, in report order.</summary>
		public IReadOnlyList<DocumentMapNode> Children => _children;

		/// <summary>
		/// Nests the entries, in the order given, by their levels: each goes beneath the nearest
		/// entry before it of a lower level. An entry more than one level below the one before it
		/// goes just one below, and one with no entry before it at a lower level is at the top.
		/// Returns the top-level nodes.
		/// </summary>
		public static IReadOnlyList<DocumentMapNode> Build(IEnumerable<DocumentMapEntry> entries)
		{
			if (entries == null)
				throw new ArgumentNullException(nameof(entries));

			var roots = new List<DocumentMapNode>();
			var open = new List<DocumentMapNode>();		// the last node at each depth
			foreach (DocumentMapEntry e in entries)
			{
				var node = new DocumentMapNode(e);
				int depth = Math.Max(0, Math.Min(e.Level, open.Count));
				open.RemoveRange(depth, open.Count - depth);
				if (depth == 0)
					roots.Add(node);
				else
					open[depth - 1]._children.Add(node);
				open.Add(node);
			}
			return roots;
		}
	}
}
