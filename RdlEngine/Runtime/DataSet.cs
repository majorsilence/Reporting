

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Collections;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Runtime Information about a set of data; public interface to the definition
	///</summary>
	[Serializable]
	public class DataSet
	{
		Report _rpt;		//	the runtime report
		DataSetDefn _dsd;	//  the true definition of the DataSet
	
		internal DataSet(Report rpt, DataSetDefn dsd)
		{
			_rpt = rpt;
			_dsd = dsd;
		}

		public async Task SetData(IDataReader dr)
		{
            await _dsd.Query.SetData(_rpt, dr, _dsd.Fields, _dsd.Filters);		// get the data (and apply the filters
		}

		public async Task SetData(DataTable dt)
		{
            await _dsd.Query.SetData(_rpt, dt, _dsd.Fields, _dsd.Filters);
		}

		public async Task SetData(XmlDocument xmlDoc)
		{
            await _dsd.Query.SetData(_rpt, xmlDoc, _dsd.Fields, _dsd.Filters);
		}

        /// <summary>
        /// Sets the data in the dataset from an IEnumerable. The content of the IEnumerable
        /// depends on the flag collection. If collection is false it will contain classes whose fields
        /// or properties will be matched to the dataset field names. If collection is true it may 
        /// contain IDictionary(s) that will be matched by key with the field name or IEnumerable(s) 
        /// that will be matched by column number. It is possible to have a mix of IDictionary and 
        /// IEnumerable when collection is true.
        /// </summary>
        /// <param name="ie"></param>
        /// <param name="collection"></param>
		[RequiresUnreferencedCode("When collection is false, items are mapped by reflection over their runtime type, whose members may be trimmed. Use SetData<T> or SetCollectionData for Native AOT / trimmed apps.")]
		public async Task SetData(IEnumerable ie, bool collection = false)
		{
            await _dsd.Query.SetData(_rpt, ie, _dsd.Fields, _dsd.Filters, collection);
		}

        /// <summary>
        /// Trim and Native AOT safe form of <see cref="SetData(IEnumerable, bool)"/> for objects.
        /// The public fields and properties declared on <typeparamref name="T"/> are matched to the
        /// dataset field names; members that exist only on a derived runtime type are not seen.
        /// </summary>
        public async Task SetData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] T>(IEnumerable<T> items)
        {
            await _dsd.Query.SetDataCore(_rpt, items, _dsd.Fields, _dsd.Filters, false, typeof(T));
        }

        /// <summary>
        /// Trim and Native AOT safe form of <c>SetData(ie, collection: true)</c>: each item is an
        /// IDictionary matched by key, or an IEnumerable matched by column number.
        /// </summary>
        public async Task SetCollectionData(IEnumerable ie)
        {
            await _dsd.Query.SetDataCore(_rpt, ie, _dsd.Fields, _dsd.Filters, true, null);
        }

        public async Task SetSource(string sql)
        {
            await _dsd.Query.CommandText.SetSource(sql);
        }


	}
}
