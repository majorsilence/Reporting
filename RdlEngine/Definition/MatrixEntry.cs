

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Runtime data structure representing the group hierarchy
    ///</summary>
    internal class MatrixEntry
    {
        Row _r;                 // needed for sort
        Dictionary<string, MatrixEntry> _HashData;  // Hash table of data values
        List<MatrixEntry> _SortedData;	//  Sorted List version of the data 
        string _hash;           // item that is used for hash
        BitArray _Rows;         // rows 
        MatrixEntry _Parent;    // parent
        ColumnGrouping _ColumnGroup;    //   Column grouping
        RowGrouping _RowGroup;  // Row grouping
        int _FirstRow;          // First row in _Rows marked true
        int _LastRow;           // Last row in _Rows marked true
        int _rowCount;          //   we save the rowCount so we can delay creating bitArray
        int _StaticColumn = 0;  // this is the index to which column to use (always 0 when dynamic)
        int _StaticRow = 0;     // this is the index to which row to use (always 0 when dynamic)
        Rows _Data;             // set dynamically when needed

        internal MatrixEntry(Row r, string hash, MatrixEntry p, int rowCount)
        {
            _r = r;
            _hash = hash;
            _HashData = new Dictionary<string, MatrixEntry>();
            _ColumnGroup = null;
            _RowGroup = null;
            _SortedData = null;
            _Data = null;
            _rowCount = rowCount;
            _Rows = null;
            _Parent = p;
            _FirstRow = -1;
            _LastRow = -1;
        }

        internal Dictionary<string, MatrixEntry> HashData
        {
            get { return _HashData; }
        }

        internal Rows Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        internal string HashItem
        {
            get { return _hash; }
        }
        internal List<MatrixEntry> GetSortedData(Report rpt)
        {
            if (_SortedData == null && _HashData != null && _HashData.Count > 0)
            {
                // Normally already done by PrepareSortedDataAsync; this is the synchronous fallback.
                _SortedData = new List<MatrixEntry>(_HashData.Values);
                _SortedData.Sort(new MatrixEntryComparer(rpt, GetSortBy()));
                _HashData = null;       // we only keep one
            }

            return _SortedData;
        }

        /// <summary>
        /// Sorts this entry's children, and theirs, awaiting the sort expressions. The matrix reads the
        /// sorted lists from many synchronous methods through <see cref="GetSortedData"/>, so they are
        /// sorted here once, after the entries are built, rather than lazily with a blocking comparer.
        /// </summary>
        internal async Task PrepareSortedDataAsync(Report rpt)
        {
            if (_SortedData == null && _HashData != null && _HashData.Count > 0)
            {
                var list = new List<MatrixEntry>(_HashData.Values);
                await MatrixEntryComparer.SortAsync(rpt, list, GetSortBy());
                _SortedData = list;
                _HashData = null;       // we only keep one
            }

            if (_SortedData != null)
            {
                foreach (MatrixEntry child in _SortedData)
                    await child.PrepareSortedDataAsync(rpt);
            }
        }

        internal MatrixEntry Parent
        {
            get { return _Parent; }
        }

        internal ColumnGrouping ColumnGroup
        {
            get { return _ColumnGroup; }
            set { _ColumnGroup = value; }
        }

        internal int StaticRow
        {
            get { return _StaticRow; }
            set { _StaticRow = value; }
        }

        internal int StaticColumn
        {
            get { return _StaticColumn; }
            set { _StaticColumn = value; }
        }

        internal RowGrouping RowGroup
        {
            get { return _RowGroup; }
            set { _RowGroup = value; }
        }

        internal int FirstRow
        {
            get { return _FirstRow; }
            set
            {
                if (_FirstRow == -1)
                    _FirstRow = value;
            }
        }

        internal int LastRow
        {
            get { return _LastRow; }
            set
            {
                if (value >= _LastRow)
                    _LastRow = value;
            }
        }

        internal BitArray Rows
        {
            get
            {
                if (_Rows == null)
                    _Rows = new BitArray(_rowCount);

                return _Rows;
            }
            set { _Rows = value; }
        }

        internal Row R
        {
            get { return _r; }
        }

        Sorting GetSortBy()
        {
            MatrixEntry me = null;
            foreach (MatrixEntry m in this.HashData.Values)
            {   // just get the first one
                me = m;
                break;
            }
            if (me == null)
                return null;

            Sorting sb = null;
            if (me.RowGroup != null)
            {
                if (me.RowGroup.DynamicRows != null)
                    sb = me.RowGroup.DynamicRows.Sorting;
            }
            else if (me.ColumnGroup != null)
            {
                if (me.ColumnGroup.DynamicColumns != null)
                    sb = me.ColumnGroup.DynamicColumns.Sorting;
            }
            return sb;
        }

    }

    class MatrixEntryComparer : System.Collections.Generic.IComparer<MatrixEntry>
    {
        Report _rpt;
        Sorting _Sorting;

        public MatrixEntryComparer(Report rpt, Sorting s)
        {
            _rpt = rpt;
            _Sorting = s;
        }

        /// <summary>
        /// Sorts like <c>list.Sort(new MatrixEntryComparer(rpt, sorting))</c>, with each entry's sort
        /// expressions evaluated once, awaited, instead of two Task.Run(...).GetResult() hops per comparison.
        /// </summary>
        internal static async Task SortAsync(Report rpt, List<MatrixEntry> list, Sorting sorting)
        {
            if (sorting == null || list.Count < 2)
            {
                list.Sort(new MatrixEntryComparer(rpt, sorting));
                return;
            }

            var sortItems = sorting.Items;
            var exprs = new IExpr[sortItems.Count];
            var types = new TypeCode[sortItems.Count];
            for (int k = 0; k < exprs.Length; k++)
            {
                exprs[k] = ((SortBy) sortItems[k]).SortExpression.Expr;
                types[k] = exprs[k].GetTypeCode();
            }

            var items = new Keyed[list.Count];
            for (int i = 0; i < items.Length; i++)
            {
                MatrixEntry me = list[i];
                var keys = new object[exprs.Length];
                bool failed = false;
                int k = 0;
                try
                {
                    for (; k < exprs.Length; k++)
                        keys[k] = await exprs[k].Evaluate(rpt, me.R);
                }
                catch (Exception e)		// this really shouldn't happen
                {
                    failed = true;
                    rpt.rl.LogError(8,
                        string.Format("Matrix Sort rows exception\r\nSort key: {0} of entry {1}\r\n{2}\r\n{3}",
                        k, me.HashItem, e.Message, e.StackTrace));
                }
                items[i] = new Keyed { Entry = me, Keys = keys, Failed = failed };
            }

            Array.Sort(items, (a, b) =>
            {
                if (a.Failed || b.Failed)
                    return a.Entry.HashItem.CompareTo(b.Entry.HashItem);
                try
                {
                    for (int k = 0; k < exprs.Length; k++)
                    {
                        int rc = Filter.ApplyCompare(types[k], a.Keys[k], b.Keys[k]);
                        if (rc != 0)
                            return ((SortBy) sortItems[k]).Direction == SortDirectionEnum.Ascending ? rc : -rc;
                    }
                }
                catch (Exception e)		// this really shouldn't happen
                {
                    rpt.rl.LogError(8,
                        string.Format("Matrix Sort rows exception\r\nEntries: {0} {1}\r\n{2}\r\n{3}",
                        a.Entry.HashItem, b.Entry.HashItem, e.Message, e.StackTrace));
                    return a.Entry.HashItem.CompareTo(b.Entry.HashItem);
                }
                return 0;		    // treat as the same
            });

            for (int i = 0; i < items.Length; i++)
                list[i] = items[i].Entry;
        }

        private struct Keyed
        {
            internal MatrixEntry Entry;
            internal object[] Keys;
            internal bool Failed;
        }

        #region IComparer Members

        public int Compare(MatrixEntry m1, MatrixEntry m2)
        {
            if (_Sorting == null)
                return m1.HashItem.CompareTo(m2.HashItem);

            object o1 = null, o2 = null;
            TypeCode tc = TypeCode.Object;
            int rc;
            try
            {
                foreach (SortBy sb in _Sorting.Items)
                {
                    IExpr e = sb.SortExpression.Expr;
                    // HACK: async
                    o1 = Task.Run(async () => await e.Evaluate(_rpt, m1.R)).GetAwaiter().GetResult();
                    o2 = Task.Run(async () => await e.Evaluate(_rpt, m2.R)).GetAwaiter().GetResult();
                    tc = e.GetTypeCode();
                    rc = Filter.ApplyCompare(tc, o1, o2);
                    if (rc != 0)
                        return sb.Direction == SortDirectionEnum.Ascending ? rc : -rc;
                }
            }
            catch (Exception e)		// this really shouldn't happen
            {
                _rpt.rl.LogError(8,
                    string.Format("Matrix Sort rows exception\r\nArguments: {0} {1}\r\nTypecode: {2}\r\n{3}\r\n{4}",
                    o1, o2, tc.ToString(), e.Message, e.StackTrace));
                return m1.HashItem.CompareTo(m2.HashItem);
            }
            return 0;		    // treat as the same

        }

        #endregion
    }
}
