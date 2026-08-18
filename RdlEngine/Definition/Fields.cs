
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Collection of fields for a DataSet.
	///</summary>
	[Serializable]
	internal class Fields : ReportLink, ICollection
	{
		IDictionary _Items;			// dictionary of items

		internal Fields(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Field f;
			// Case-insensitive lookups: expressions frequently reach this collection with
			// a different casing than the <Field Name> declaration (reports converted from
			// other products carry e.g. a "TotalFeeLC" reference against a "TotalFeeLc"
			// column), and a case-mismatch here is a hard "Field not found" parse error.
			// Nothing legitimately declares two fields differing only by case — that
			// already logs "has duplicates" below.
			if (xNode.ChildNodes.Count < 10)
				_Items = new ListDictionary(StringComparer.OrdinalIgnoreCase);
			else
				_Items = new Hashtable(xNode.ChildNodes.Count, StringComparer.OrdinalIgnoreCase);

			// Loop thru all the child nodes
			int iCol=0;
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "field":
						f = new Field(r, this, xNodeLoop);
						f.ColumnNumber = iCol++;			// Assign the column number
						break;
					default:	
						f=null;	
						r.rl.LogError(4, "Unknown element '" + xNodeLoop.Name + "' in fields list."); 
						break;
				}
				if (f != null)
				{
					if (_Items.Contains(f.Name.Nm))
					{
						r.rl.LogError(4, "Field " + f.Name + " has duplicates."); 
					}
					else	
						_Items.Add(f.Name.Nm, f);
				}
			}
		}

		internal Field this[string s]
		{
			get 
			{
				return _Items[s] as Field;
			}
		}
		
		async override internal Task FinalPass()
		{
			// Parse order matters for calculated (<Value>) fields that reference other
			// calculated fields: a referencing expression's type check (e.g. AND/OR's
			// boolean requirement) calls FunctionField.GetTypeCode -> Field.Type ->
			// _Value.Expr.GetTypeCode(), which is only meaningful once the referenced
			// field's own Value has been through FinalPass — before that Expr is null and
			// the type comes back Object, failing checks that would pass a moment later.
			// _Items is a Hashtable and .NET randomizes string hashing per process, so the
			// old unordered walk made those failures a per-run coin flip. Order the walk:
			// DataField-bound fields first (no dependencies), then Value fields
			// topologically by scanning their source for Fields!Name references. Cycles
			// (already guarded elsewhere) and unknown names just fall back to the
			// leftover order.
			var done = new System.Collections.Generic.HashSet<Field>();
			var byName = new System.Collections.Generic.Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);
			foreach (Field f in _Items.Values)
				if (f.Name?.Nm != null && !byName.ContainsKey(f.Name.Nm))
					byName[f.Name.Nm] = f;

			async Task Visit(Field f, System.Collections.Generic.HashSet<Field> path)
			{
				if (done.Contains(f) || path.Contains(f))
					return;
				path.Add(f);
				string src = f.Value?.Source;
				if (src != null)
					foreach (System.Text.RegularExpressions.Match m in
						System.Text.RegularExpressions.Regex.Matches(src, @"Fields!(\w+)"))
						if (byName.TryGetValue(m.Groups[1].Value, out Field dep) && dep != f)
							await Visit(dep, path);
				path.Remove(f);
				if (done.Add(f))
					await f.FinalPass();
			}

			foreach (Field f in _Items.Values)
				if (f.Value == null && done.Add(f))
					await f.FinalPass();
			foreach (Field f in _Items.Values)
				await Visit(f, new System.Collections.Generic.HashSet<Field>());
			return;
		}

		internal IDictionary Items
		{
			get { return  _Items; }
		}
		#region ICollection Members

		public bool IsSynchronized
		{
			get
			{
				return _Items.Values.IsSynchronized;
			}
		}

		public int Count
		{
			get
			{
				return _Items.Values.Count;
			}
		}

		public void CopyTo(Array array, int index)
		{
			_Items.Values.CopyTo(array, index);
		}

		public object SyncRoot
		{
			get
			{
				return _Items.Values.SyncRoot;
			}
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _Items.Values.GetEnumerator();
		}

		#endregion

	}
}
