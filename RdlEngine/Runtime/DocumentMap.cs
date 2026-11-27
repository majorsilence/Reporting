using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// A document-map label carried by a page item: the evaluated <c>Label</c> (RDL 2005) or
	/// <c>DocumentMapLabel</c> (RDL 2008+) of a report item or group instance, and how deep it
	/// nests in the document map. Level 0 is the top.
	/// </summary>
	public sealed class DocumentMapLabel
	{
		public DocumentMapLabel(string label, int level)
		{
			Label = label ?? throw new ArgumentNullException(nameof(label));
			Level = Math.Max(0, level);
		}

		public string Label { get; }

		public int Level { get; }
	}

	/// <summary>
	/// One entry of a rendered report's document map: a label, its nesting level, and where the
	/// item it labels is drawn. See <see cref="Pages.GetDocumentMap"/>.
	/// </summary>
	public sealed class DocumentMapEntry
	{
		public DocumentMapEntry(string label, int level, int pageIndex, float x, float y)
		{
			Label = label;
			Level = level;
			PageIndex = pageIndex;
			X = x;
			Y = y;
		}

		/// <summary>The text shown in the document map.</summary>
		public string Label { get; }

		/// <summary>The nesting level, 0 at the top. An entry is at most one level below the one before it.</summary>
		public int Level { get; }

		/// <summary>The zero-based index of the page in <see cref="Pages"/>.</summary>
		public int PageIndex { get; }

		/// <summary>The left of the labelled item on its page, in points.</summary>
		public float X { get; }

		/// <summary>The top of the labelled item on its page, in points from the top of the page.</summary>
		public float Y { get; }
	}

	public partial class Report
	{
		// The document map is built as the pages are: an item with a label, or the instance of a
		// group with one, puts it on the first page item it draws, nested one level below the
		// labelled containers and groups it is drawn in. The labels travel with the page items, so
		// a group or row taken off a page and run again (keep together) takes its labels with it.

		int _documentMapDepth;					// how many labelled containers and groups the items being laid out are in
		List<DocumentMapLabel> _pendingDocumentMap;	// labels of containers and groups waiting for the first item they draw

		internal int DocumentMapDepth => _documentMapDepth;

		internal void ResetDocumentMap()
		{
			_documentMapDepth = 0;
			_pendingDocumentMap = null;
		}

		// A container or group instance with a label starts: its label goes on the next page item
		// drawn, and what is inside it nests one level deeper. Returns the token EndDocumentMapScope
		// takes, or -1 when there is no label (nothing to end).
		internal int BeginDocumentMapScope(string label)
		{
			if (string.IsNullOrEmpty(label))
				return -1;
			_pendingDocumentMap ??= new List<DocumentMapLabel>();
			int token = _pendingDocumentMap.Count;
			_pendingDocumentMap.Add(new DocumentMapLabel(label, _documentMapDepth));
			_documentMapDepth++;
			return token;
		}

		// The container or group instance ends. When it drew nothing (hidden, or no rows) its label
		// is still waiting, and is dropped rather than given to whatever is drawn next.
		internal void EndDocumentMapScope(int token)
		{
			if (token < 0)
				return;
			_documentMapDepth = Math.Max(0, _documentMapDepth - 1);
			if (_pendingDocumentMap != null && _pendingDocumentMap.Count > token)
				_pendingDocumentMap.RemoveRange(token, _pendingDocumentMap.Count - token);
		}

		// A label for the next page item drawn at a given level, outside any scope: a matrix row
		// group instance, whose rows are laid out as a grid rather than group by group, so nothing
		// nests inside it by scope.
		internal void QueueDocumentMapLabel(string label, int level)
		{
			if (string.IsNullOrEmpty(label))
				return;
			_pendingDocumentMap ??= new List<DocumentMapLabel>();
			_pendingDocumentMap.Add(new DocumentMapLabel(label, level));
		}

		// Called as a page item is put on a page: it takes the labels waiting for an item, ahead of
		// any label of its own.
		internal void TakePendingDocumentMap(PageItem pi)
		{
			if (_pendingDocumentMap == null || _pendingDocumentMap.Count == 0)
				return;
			pi.InsertDocumentMapLabels(_pendingDocumentMap);
			_pendingDocumentMap.Clear();
		}

		// Labels still waiting once the body is laid out had nothing drawn for them; the page
		// header and footer, laid out after, take no part in the document map.
		internal void EndDocumentMapBody()
		{
			_pendingDocumentMap = null;
			_documentMapDepth = 0;
		}
	}
}
