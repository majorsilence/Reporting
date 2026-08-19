using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Reflection;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;
using System.Data.Common;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Query representation against a data source.  Holds the data at runtime.
    ///</summary>
    [Serializable]
    internal class Query : ReportLink
    {
        string _DataSourceName;     // Name of the data source to execute the query against
        DataSourceDefn _DataSourceDefn; //  the data source object the DataSourceName references.
        QueryCommandTypeEnum _QueryCommandType; // Indicates what type of query is contained in the CommandText
        Expression _CommandText;    //	(string) The query to execute to obtain the data for the report
        QueryParameters _QueryParameters;   // A list of parameters that are passed to the data
                                            // source as part of the query.		
        int _Timeout;               // Number of seconds to allow the query to run before
                                    // timing out.   Must be >= 0; If omitted or zero; no timeout
        int _RowLimit;              // Number of rows to retrieve before stopping retrieval; 0 means no limit

        IDictionary _Columns;       // QueryColumn (when SQL)

        internal Query(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            _DataSourceName = null;
            _QueryCommandType = QueryCommandTypeEnum.Text;
            _CommandText = null;
            _QueryParameters = null;
            _Timeout = 0;
            _RowLimit = 0;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name.ToLowerInvariant())
                {
                    case "datasourcename":
                        _DataSourceName = xNodeLoop.InnerText;
                        break;
                    case "commandtype":
                        _QueryCommandType = Majorsilence.Reporting.Rdl.QueryCommandType.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "commandtext":
                        _CommandText = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "queryparameters":
                        _QueryParameters = new QueryParameters(r, this, xNodeLoop);
                        break;
                    case "timeout":
                        _Timeout = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    case "rowlimit":                // Extension of RDL specification
                        _RowLimit = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Query element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }   // end of switch
            }   // end of foreach

            // Resolve the data source name to the object
            if (_DataSourceName == null)
            {
                r.rl.LogError(8, "DataSourceName element not specified for Query.");
                return;
            }
        }

        // Handle parsing of function in final pass
        internal override async Task FinalPass()
        {
            if (_CommandText != null)
                await _CommandText.FinalPass();
            if (_QueryParameters != null)
                await _QueryParameters.FinalPass();

            // verify the data source
            DataSourceDefn ds = null;
            if (OwnerReport.DataSourcesDefn != null &&
                OwnerReport.DataSourcesDefn.Items != null)
            {
                ds = OwnerReport.DataSourcesDefn[_DataSourceName];
            }
            if (ds == null)
            {
                OwnerReport.rl.LogError(8, "Query references unknown data source '" + _DataSourceName + "'");
                return;
            }
            _DataSourceDefn = ds;

            IDbConnection cnSQL = ds.SqlConnect(null);
            if (cnSQL == null || _CommandText == null)
                return;

            // Treat this as a SQL statement
            String sql = await _CommandText.EvaluateString(null, null);

            try
            {
                using var cmSQL = cnSQL.CreateCommand();
                // Bind by name where the provider supports it, so that a bind
                // variable may be repeated and QueryParameter order does not
                // silently change which value lands where. See TrySetBindByName.
                bool bindByName = TrySetBindByName(cmSQL);
                cmSQL.CommandText = await AddParametersAsLiterals(null, cnSQL, sql, false);
                if (this._QueryCommandType == QueryCommandTypeEnum.StoredProcedure)
                    cmSQL.CommandType = CommandType.StoredProcedure;

                await AddParameters(null, cnSQL, cmSQL, false, bindByName);
                // Schema pass: metadata only, deliberately no rows. Do not
                // change to Default - that would execute every report's query
                // a second time at compile time.
                using var dr = await CreateDataReader(cmSQL, CommandBehavior.SchemaOnly).ConfigureAwait(false);

                if (dr.FieldCount < 10)
                    _Columns = new ListDictionary(); // Hashtable is overkill for small lists
                else
                    _Columns = new Hashtable(dr.FieldCount);

                for (int i = 0; i < dr.FieldCount; i++)
                {
                    QueryColumn qc = new QueryColumn(i, dr.GetName(i), Type.GetTypeCode(dr.GetFieldType(i)));

                    try { _Columns.Add(qc.colName, qc); }
                    catch // name has already been added to list: 
                    { // According to the RDL spec SQL names are matched by Name not by relative
                      //   position: this seems wrong to me and causes this problem; but 
                      //   user can fix by using "as" keyword to name columns in Select 
                      //    e.g.  Select col as "col1", col as "col2" from tableA
                        OwnerReport.rl.LogError(8, String.Format("Column '{0}' is not uniquely defined within the SQL Select columns.", qc.colName));
                    }
                }
            }
            catch (Exception e)
            {
                // Issue #35 - Kept the logging
                OwnerReport.rl.LogError(4, "SQL Exception during report compilation: " + e.Message + "\r\nSQL: " + sql);
                throw;
            }
        }

        /// <summary>
        /// Executes the command and returns a reader.
        /// </summary>
        /// <remarks>
        /// The behaviour must be supplied by the caller, because the two
        /// callers need opposite things and previously shared one hardcoded
        /// value.
        /// </para>
        /// <para>
        /// <see cref="FinalPass"/> is the schema pass. It runs at compile time
        /// purely to learn column names and types, and must NOT actually
        /// execute the query, so it passes <c>CommandBehavior.SchemaOnly</c>.
        /// <see cref="GetData"/> is the data pass and must pass
        /// <c>CommandBehavior.Default</c> to get rows back.
        /// </para>
        /// <para>
        /// Both call sites previously used a helper that hardcoded
        /// <c>SchemaOnly</c>. Per the ADO.NET contract, <c>SchemaOnly</c>
        /// returns column metadata and no rows, so the data pass opened a
        /// reader whose first <c>Read()</c> immediately returned false. The
        /// effect was that EVERY SQL-backed dataset returned zero rows at run
        /// time. There was no error at any severity: the connection opened
        /// cleanly, the SQL was correct and unmodified, the reader was valid,
        /// and the report simply rendered its NoRows message. Datasets fed by
        /// static &lt;Rows&gt; or by SetData() were unaffected, since they never
        /// reach this method, which is why the failure could look specific to
        /// one report or one data source.
        /// </para>
        /// <para>
        /// This is provider-independent. <c>CommandBehavior</c> is part of the
        /// ADO.NET contract, not a provider extension, so Oracle, SQL Server,
        /// MySQL, PostgreSQL, SQLite and ODBC were all affected identically.
        /// </para>
        /// </remarks>
        private static async Task<IDataReader> CreateDataReader(IDbCommand cmSQL, CommandBehavior behavior)
        {
            IDataReader dr;
            if (cmSQL is DbCommand dbCommand)
            {
                dr = await dbCommand.ExecuteReaderAsync(behavior).ConfigureAwait(false);
            }
            else
            {
                dr = cmSQL.ExecuteReader(behavior);
            }

            return dr;
        }

        internal async Task<bool> GetData(Report rpt, Fields flds, Filters f)
        {
            Rows uData = this.GetMyUserData(rpt);
            if (uData != null)
            {
                this.SetMyData(rpt, uData);
                return uData.Data == null || uData.Data.Count == 0 ? false : true;
            }

            // Treat this as a SQL statement
            DataSourceDefn ds = _DataSourceDefn;
            if (ds == null || _CommandText == null)
            {
                this.SetMyData(rpt, null);
                return false;
            }

            IDbConnection cnSQL = ds.SqlConnect(rpt);
            if (cnSQL == null)
            {
                this.SetMyData(rpt, null);
                return false;
            }

            Rows _Data = new Rows(rpt, null, null, null);       // no sorting and grouping at base data
            String sql = await _CommandText.EvaluateString(rpt, null);

            try
            {
                using var cmSQL = cnSQL.CreateCommand();
                // Must match GetSchema above: the schema pass and the data pass
                // have to bind identically, or a report can compile and then
                // fail (or worse, succeed with the wrong values) at run time.
                bool bindByName = TrySetBindByName(cmSQL);
                cmSQL.CommandText = await AddParametersAsLiterals(rpt, cnSQL, sql, true);
                if (this._QueryCommandType == QueryCommandTypeEnum.StoredProcedure)
                    cmSQL.CommandType = CommandType.StoredProcedure;
                if (this._Timeout > 0)
                    cmSQL.CommandTimeout = this._Timeout;

                await AddParameters(rpt, cnSQL, cmSQL, true, bindByName);
                // Data pass: MUST be Default. SchemaOnly here returns column
                // metadata with zero rows and raises no error, silently
                // emptying every SQL-backed dataset in the report.
                using var dr = await CreateDataReader(cmSQL, CommandBehavior.Default);

                List<Row> ar = new List<Row>();
                _Data.Data = ar;
                int rowCount = 0;
                int maxRows = _RowLimit > 0 ? _RowLimit : int.MaxValue;
                int fieldCount = flds.Items.Count;

                // Determine the query column number for each field
                int[] qcn = new int[flds.Items.Count];
                foreach (Field fld in flds)
                {
                    qcn[fld.ColumnNumber] = -1;
                    if (fld.Value != null)
                        continue;
                    try
                    {
                        qcn[fld.ColumnNumber] = dr.GetOrdinal(fld.DataField);
                    }
                    catch
                    {
                        qcn[fld.ColumnNumber] = -1;
                    }
                }

                while (dr.Read())
                {
                    Row or = new Row(_Data, fieldCount);

                    foreach (Field fld in flds)
                    {
                        if (qcn[fld.ColumnNumber] != -1)
                        {
                            or.Data[fld.ColumnNumber] = dr.GetValue(qcn[fld.ColumnNumber]);
                        }
                    }

                    // Apply the filters
                    if (f == null || await f.Apply(rpt, or))
                    {
                        or.RowNumber = rowCount;    // 
                        rowCount++;
                        ar.Add(or);
                    }
                    if (--maxRows <= 0)             // don't retrieve more than max
                        break;
                }
                ar.TrimExcess();        // free up any extraneous space; can be sizeable for large # rows
                if (f != null)
                    await f.ApplyFinalFilters(rpt, _Data, false);
                //#if DEBUG
                //				rpt.rl.LogError(4, "Rows Read:" + ar.Count.ToString() + " SQL:" + sql );
                //#endif
            }
            catch (Exception e)
            {
                // Issue #35 - Kept the logging
                rpt.rl.LogError(8, "SQL Exception" + e.Message + "\r\n" + e.StackTrace);
                throw;
            }

            this.SetMyData(rpt, _Data);
            return _Data == null || _Data.Data == null || _Data.Data.Count == 0 ? false : true;
        }

        // Obtain the data from the XML
        internal async Task<bool> GetData(Report rpt, string xmlData, Fields flds, Filters f)
        {
            Rows uData = this.GetMyUserData(rpt);
            if (uData != null)
            {
                this.SetMyData(rpt, uData);
                return uData.Data == null || uData.Data.Count == 0 ? false : true;
            }

            int fieldCount = flds.Items.Count;

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            doc.LoadXml(xmlData);

            XmlNode xNode;
            xNode = doc.LastChild;
            if (xNode == null || !(xNode.Name == "Rows" || xNode.Name == "fyi:Rows"))
            {
                throw new Exception(Strings.Query_Error_XMLMustContainTopLevelRows);
            }

            Rows _Data = new Rows(rpt, null, null, null);
            List<Row> ar = new List<Row>();
            _Data.Data = ar;

            int rowCount = 0;
            foreach (XmlNode xNodeRow in xNode.ChildNodes)
            {
                if (xNodeRow.NodeType != XmlNodeType.Element)
                {
                    continue;
                }
                if (xNodeRow.Name != "Row")
                {
                    continue;
                }
                Row or = new Row(_Data, fieldCount);
                foreach (XmlNode xNodeColumn in xNodeRow.ChildNodes)
                {
                    Field fld = (Field)(flds.Items[xNodeColumn.Name]);	// Find the column
                    if (fld == null)
                    {
                        continue;			// Extraneous data is ignored
                    }
                    TypeCode tc = fld.qColumn != null ? fld.qColumn.colType : fld.Type;

                    if (xNodeColumn.InnerText == null || xNodeColumn.InnerText.Length == 0)
                    {
                        or.Data[fld.ColumnNumber] = null;
                    }
                    else if (tc == TypeCode.String)
                    {
                        or.Data[fld.ColumnNumber] = xNodeColumn.InnerText;
                    }
                    else if (tc == TypeCode.DateTime)
                    {
                        try
                        {
                            or.Data[fld.ColumnNumber] =
                                Convert.ToDateTime(xNodeColumn.InnerText,
                                System.Globalization.DateTimeFormatInfo.InvariantInfo);
                        }
                        catch	// all conversion errors result in a null value
                        {
                            or.Data[fld.ColumnNumber] = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            or.Data[fld.ColumnNumber] =
                                Convert.ChangeType(xNodeColumn.InnerText, tc, NumberFormatInfo.InvariantInfo);
                        }
                        catch	// all conversion errors result in a null value
                        {
                            or.Data[fld.ColumnNumber] = null;
                        }
                    }
                }
                // Apply the filters 
                if (f == null || await f.Apply(rpt, or))
                {
                    or.RowNumber = rowCount;    // 
                    rowCount++;
                    ar.Add(or);
                }
            }

            ar.TrimExcess();		// free up any extraneous space; can be sizeable for large # rows
            if (f != null)
            {
                await f.ApplyFinalFilters(rpt, _Data, false);
            }

            SetMyData(rpt, _Data);
            return _Data == null || _Data.Data == null || _Data.Data.Count == 0 ? false : true;

        }

        [RequiresUnreferencedCode("Reflects over user-provided object types to map fields; members may be trimmed")]
        internal async Task SetData(Report rpt, IEnumerable ie, Fields flds, Filters f, bool collection = false)
        {
            if (ie == null)         // Does user want to remove user data?
            {
                SetMyUserData(rpt, null);
                return;
            }

            Rows rows = new Rows(rpt, null, null, null);		// no sorting and grouping at base data

            List<Row> ar = new List<Row>();
            rows.Data = ar;
            int rowCount = 0;
            int maxRows = _RowLimit > 0 ? _RowLimit : int.MaxValue;
            int fieldCount = flds.Items.Count;
            Field[] orderedFields = null;
            foreach (object dt in ie)
            {
                // Get the type.
                Type myType = dt.GetType();

                // Build the row
                Row or = new Row(rows, fieldCount);

                if (collection)
                {
                    if (dt is IDictionary)
                    {
                        IDictionary dic = (IDictionary)dt;
                        foreach (Field fld in flds)
                        {
                            if (dic.Contains(fld.Name.Nm))
                            {
                                or.Data[fld.ColumnNumber] = dic[fld.Name.Nm];
                            }
                        }
                    }
                    else if (dt is IEnumerable)
                    {
                        if (orderedFields == null)
                        {
                            orderedFields = new Field[fieldCount];
                            foreach (Field fld in flds)
                            {
                                orderedFields[fld.ColumnNumber] = fld;
                            }
                        }
                        IEnumerator inum = ((IEnumerable)dt).GetEnumerator();
                        foreach (Field fld in orderedFields)
                        {
                            if (!inum.MoveNext())
                                break;
                            or.Data[fld.ColumnNumber] = inum.Current;
                        }
                    }
                }
                else
                {
                    // Go thru each field and try to obtain a value
                    foreach (Field fld in flds)
                    {
                        // Get the type and fields of FieldInfoClass.
                        FieldInfo fi = myType.GetField(fld.Name.Nm, BindingFlags.Instance | BindingFlags.Public);
                        if (fi != null)
                        {
                            or.Data[fld.ColumnNumber] = fi.GetValue(dt);
                        }
                        else
                        {
                            // Try getting it as a property as well
                            PropertyInfo pi = myType.GetProperty(fld.Name.Nm, BindingFlags.Instance | BindingFlags.Public);
                            if (pi != null)
                            {
                                or.Data[fld.ColumnNumber] = pi.GetValue(dt, null);
                            }
                        }
                    }
                }

                // Apply the filters 
                if (f == null || await f.Apply(rpt, or))
                {
                    or.RowNumber = rowCount;    // 
                    rowCount++;
                    ar.Add(or);
                }
                if (--maxRows <= 0)             // don't retrieve more than max
                    break;
            }
            ar.TrimExcess();        // free up any extraneous space; can be sizeable for large # rows
            if (f != null)
                await f.ApplyFinalFilters(rpt, rows, false);

            SetMyUserData(rpt, rows);
        }

        internal async Task SetData(Report rpt, IDataReader dr, Fields flds, Filters f)
        {
            if (dr == null)         // Does user want to remove user data?
            {
                SetMyUserData(rpt, null);
                return;
            }

            Rows rows = new Rows(rpt, null, null, null);		// no sorting and grouping at base data

            List<Row> ar = new List<Row>();
            rows.Data = ar;
            int rowCount = 0;
            int maxRows = _RowLimit > 0 ? _RowLimit : int.MaxValue;

            while (dr.Read())
            {
                Row or = new Row(rows, dr.FieldCount);
                dr.GetValues(or.Data);
                // Apply the filters 
                if (f == null || await f.Apply(rpt, or))
                {
                    or.RowNumber = rowCount;    // 
                    rowCount++;
                    ar.Add(or);
                }
                if (--maxRows <= 0)             // don't retrieve more than max
                    break;
            }
            ar.TrimExcess();        // free up any extraneous space; can be sizeable for large # rows
            if (f != null)
                await f.ApplyFinalFilters(rpt, rows, false);

            SetMyUserData(rpt, rows);
        }

        internal async Task SetData(Report rpt, DataTable dt, Fields flds, Filters f)
        {
            if (dt == null)         // Does user want to remove user data?
            {
                SetMyUserData(rpt, null);
                return;
            }

            Rows rows = new Rows(rpt, null, null, null);		// no sorting and grouping at base data

            List<Row> ar = new List<Row>();
            rows.Data = ar;
            int rowCount = 0;
            int maxRows = _RowLimit > 0 ? _RowLimit : int.MaxValue;

            int fieldCount = flds.Items.Count;
            foreach (DataRow dr in dt.Rows)
            {
                Row or = new Row(rows, fieldCount);
                // Loop thru the columns obtaining the data values by name
                foreach (Field fld in flds.Items.Values)
                {
                    or.Data[fld.ColumnNumber] = dr[fld.DataField];
                }
                // Apply the filters 
                if (f == null || await f.Apply(rpt, or))
                {
                    or.RowNumber = rowCount;    // 
                    rowCount++;
                    ar.Add(or);
                }
                if (--maxRows <= 0)             // don't retrieve more than max
                    break;
            }
            ar.TrimExcess();        // free up any extraneous space; can be sizeable for large # rows
            if (f != null)
                await f.ApplyFinalFilters(rpt, rows, false);

            SetMyUserData(rpt, rows);
        }

        internal async Task SetData(Report rpt, XmlDocument xmlDoc, Fields flds, Filters f)
        {
            if (xmlDoc == null)         // Does user want to remove user data?
            {
                SetMyUserData(rpt, null);
                return;
            }

            Rows rows = new Rows(rpt, null, null, null);        // no sorting and grouping at base data

            XmlNode xNode;
            xNode = xmlDoc.LastChild;
            if (xNode == null || !(xNode.Name == "Rows" || xNode.Name == "fyi:Rows"))
            {
                throw new Exception(Strings.Query_Error_XMLMustContainTopLevelRows);
            }

            List<Row> ar = new List<Row>();
            rows.Data = ar;

            int rowCount = 0;
            int fieldCount = flds.Items.Count;
            foreach (XmlNode xNodeRow in xNode.ChildNodes)
            {
                if (xNodeRow.NodeType != XmlNodeType.Element)
                    continue;
                if (xNodeRow.Name != "Row")
                    continue;
                Row or = new Row(rows, fieldCount);
                foreach (XmlNode xNodeColumn in xNodeRow.ChildNodes)
                {
                    Field fld = (Field)(flds.Items[xNodeColumn.Name]);  // Find the column
                    if (fld == null)
                        continue;           // Extraneous data is ignored
                    if (xNodeColumn.InnerText == null || xNodeColumn.InnerText.Length == 0)
                        or.Data[fld.ColumnNumber] = null;
                    else if (fld.Type == TypeCode.String)
                        or.Data[fld.ColumnNumber] = xNodeColumn.InnerText;
                    else
                    {
                        try
                        {
                            or.Data[fld.ColumnNumber] =
                                Convert.ChangeType(xNodeColumn.InnerText, fld.Type, NumberFormatInfo.InvariantInfo);
                        }
                        catch   // all conversion errors result in a null value
                        {
                            or.Data[fld.ColumnNumber] = null;
                        }
                    }
                }
                // Apply the filters 
                if (f == null || await f.Apply(rpt, or))
                {
                    or.RowNumber = rowCount;    // 
                    rowCount++;
                    ar.Add(or);
                }
            }

            ar.TrimExcess();        // free up any extraneous space; can be sizeable for large # rows
            if (f != null)
                await f.ApplyFinalFilters(rpt, rows, false);

            SetMyUserData(rpt, rows);
        }


        /// <summary>
        /// Enables named parameter binding on providers that support it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="IDbCommand"/> has no concept of named versus positional
        /// binding, so a provider's own default applies. Oracle's ODP.NET
        /// defaults <c>BindByName</c> to <c>false</c>, meaning parameters bind
        /// by the order they were added rather than by name — even though the
        /// SQL uses named placeholders such as <c>:Facility</c>, and even
        /// though <see cref="AddParameters"/> gives each parameter a name.
        /// </para>
        /// <para>
        /// Two problems follow. First, a bind variable can only be used once
        /// per statement: if <c>:Facility</c> appears five times, Oracle sees
        /// five placeholders while the engine supplies one parameter, and the
        /// statement fails with ORA-01008 (not all variables bound). Second,
        /// and worse, if the order of &lt;QueryParameter&gt; elements does not
        /// match the order the placeholders appear in the SQL, every value
        /// binds to the wrong placeholder. That produces incorrect results
        /// with no error raised.
        /// </para>
        /// <para>
        /// Setting <c>BindByName</c> to <c>true</c> resolves both: placeholders
        /// match on name, repeats work, and element order stops mattering.
        /// </para>
        /// <para>
        /// The property is set by reflection so that RdlEngine keeps no
        /// compile-time dependency on ODP.NET (or any other provider assembly).
        /// Providers that do not expose <c>BindByName</c> — SQL Server, MySQL,
        /// PostgreSQL, SQLite and the rest — are unaffected: the property is
        /// simply absent and the call is a no-op. Providers already binding by
        /// name see no change in behaviour.
        /// </para>
        /// </remarks>
        /// <param name="cmd">The command created from the provider connection.</param>
        /// <returns>
        /// True if the command now binds by name. Callers MUST pass this to
        /// <see cref="AddParameters"/>, because it changes how parameters have
        /// to be named - see the remarks there.
        /// </returns>
        private static bool TrySetBindByName(IDbCommand cmd)
        {
            try
            {
                var pi = cmd.GetType().GetProperty("BindByName");
                if (pi != null && pi.CanWrite && pi.PropertyType == typeof(bool))
                {
                    pi.SetValue(cmd, true, null);
                    return true;
                }
            }
            catch
            {
                // Deliberately non-fatal. If the property cannot be set the
                // command still works, falling back to the provider default
                // (positional binding on ODP.NET). Better to run with the
                // previous behaviour than to fail the report outright.
            }
            return false;
        }

        /// <remarks>
        /// The <paramref name="bindByName"/> flag must reflect what
        /// <see cref="TrySetBindByName"/> did to this command, because the two
        /// binding modes need different parameter names.
        /// <para>
        /// Historically every parameter was named with a leading '@'. That is
        /// SQL Server syntax, and it was harmless only because binding was
        /// positional: with BindByName off the provider ignores names entirely
        /// and matches on the order parameters were added.
        /// </para>
        /// <para>
        /// Once BindByName is on, the name is what the provider matches
        /// against the placeholder in the SQL. Oracle placeholders look like
        /// ':FacNbr', so a parameter named '@FacNbr' matches nothing and the
        /// command fails with ORA-50028 (invalid parameter binding). When
        /// binding by name the bare name is used; ODP.NET accepts it with or
        /// without the ':' prefix.
        /// </para>
        /// </remarks>
        private async Task AddParameters(Report rpt, IDbConnection cn, IDbCommand cmSQL, bool bValue, bool bindByName)
        {
            // any parameters to substitute
            if (this._QueryParameters == null ||
                this._QueryParameters.Items == null ||
                this._QueryParameters.Items.Count == 0 ||
                this._QueryParameters.ContainsArray)            // arrays get handled by AddParametersAsLiterals
                return;

            // AddParametersAsLiterals handles it when there is replacement
            if (RdlEngineConfig.DoParameterReplacement(Provider, cn))
                return;

            foreach (QueryParameter qp in this._QueryParameters.Items)
            {
                string paramName;

                if (bindByName)
                {   // name must match the placeholder in the SQL; no '@'
                    paramName = qp.Name.Nm[0] == '@' ? qp.Name.Nm.Substring(1) : qp.Name.Nm;
                }
                else if (qp.Name.Nm[0] == '@')
                {   // positional binding; keep the historical '@' naming
                    paramName = qp.Name.Nm;
                }
                else
                {
                    paramName = "@" + qp.Name.Nm;
                }
                object pvalue = bValue ? await qp.Value.Evaluate(rpt, null) : null;
                IDbDataParameter dp = cmSQL.CreateParameter();

                dp.ParameterName = paramName;
                if (pvalue is ArrayList)    // Probably a MultiValue Report parameter result
                {
                    ArrayList ar = (ArrayList)pvalue;
                    dp.Value = ar.ToArray(ar[0].GetType());
                }
                else
                    dp.Value = pvalue;
                cmSQL.Parameters.Add(dp);
            }
        }

        private async Task<string> AddParametersAsLiterals(Report rpt, IDbConnection cn, string sql, bool bValue)
        {
            // No parameters means nothing to do
            if (this._QueryParameters == null ||
                this._QueryParameters.Items == null ||
                this._QueryParameters.Items.Count == 0)
                return sql;

            // Only do this for ODBC datasources - AddParameters handles it in other cases
            if (!RdlEngineConfig.DoParameterReplacement(Provider, cn))
            {
                if (!_QueryParameters.ContainsArray)    // when array we do substitution
                    return sql;
            }

            StringBuilder sb = new StringBuilder(sql);
            List<QueryParameter> qlist;
            if (_QueryParameters.Items.Count <= 1)
                qlist = _QueryParameters.Items;
            else
            {   // need to sort the list so that longer items are first in the list
                // otherwise substitution could be done incorrectly
                qlist = new List<QueryParameter>(_QueryParameters.Items);
                qlist.Sort();
            }

            foreach (QueryParameter qp in qlist)
            {
                string paramName;

                // force the name to start with @
                if (qp.Name.Nm[0] == '@')
                    paramName = qp.Name.Nm;
                else
                    paramName = "@" + qp.Name.Nm;

                // build the replacement value
                string svalue;
                if (bValue)
                {	// use the value provided
                    svalue = await this.ParameterValue(rpt, qp);
                }
                else
                {   // just need a place holder value that will pass parsing
                    switch (qp.Value.Expr.GetTypeCode())
                    {
                        case TypeCode.Char:
                            svalue = "' '";
                            break;
                        case TypeCode.DateTime:
                            svalue = "'1900-01-01 00:00:00'";
                            break;
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            svalue = "0";
                            break;
                        case TypeCode.Boolean:
                            svalue = "'false'";
                            break;
                        case TypeCode.String:
                        default:
                            svalue = "' '";
                            break;
                    }
                }
                sb.Replace(paramName, svalue);
            }
            return sb.ToString();
        }

        private async Task<string> ParameterValue(Report rpt, QueryParameter qp)
        {
            if (!qp.IsArray)
            {
                // handle non-array
                string svalue = await qp.Value.EvaluateString(rpt, null);
                if (svalue == null)
                    svalue = "null";
                else switch (qp.Value.Expr.GetTypeCode())
                    {
                        case TypeCode.Char:
                        case TypeCode.DateTime:
                        case TypeCode.String:
                            // need to double up on "'" and then surround by '
                            svalue = svalue.Replace("'", "''");
                            svalue = "'" + svalue + "'";
                            break;
                    }
                return svalue;
            }

            StringBuilder sb = new StringBuilder();
            ArrayList ar = await qp.Value.Evaluate(rpt, null) as ArrayList;

            if (ar == null)
                return null;

            bool bFirst = true;
            foreach (object v in ar)
            {
                if (!bFirst)
                    sb.Append(", ");
                if (v == null)
                {
                    sb.Append("null");
                }
                else
                {
                    string sv = v.ToString();
                    if (v is string || v is char || v is DateTime)
                    {
                        // need to double up on "'" and then surround by '
                        sv = sv.Replace("'", "''");
                        sb.Append("'");
                        sb.Append(sv);
                        sb.Append("'");
                    }
                    else
                        sb.Append(sv);
                }
                bFirst = false;
            }

            if (sb.Length == 0)
            {
                sb.Append("null");
            }

            return sb.ToString();
        }

        private string Provider
        {
            get
            {
                if (this.DataSourceDefn == null ||
                    this.DataSourceDefn.ConnectionProperties == null)
                    return "";
                return this.DataSourceDefn.ConnectionProperties.DataProvider;
            }
        }

        internal string DataSourceName
        {
            get { return _DataSourceName; }
        }

        internal DataSourceDefn DataSourceDefn
        {
            get { return _DataSourceDefn; }
        }

        internal QueryCommandTypeEnum QueryCommandType
        {
            get { return _QueryCommandType; }
            set { _QueryCommandType = value; }
        }

        internal Expression CommandText
        {
            get { return _CommandText; }
            set { _CommandText = value; }
        }

        internal QueryParameters QueryParameters
        {
            get { return _QueryParameters; }
            set { _QueryParameters = value; }
        }

        internal int Timeout
        {
            get { return _Timeout; }
            set { _Timeout = value; }
        }

        internal IDictionary Columns
        {
            get { return _Columns; }
        }

        // Runtime data
        internal Rows GetMyData(Report rpt)
        {
            return rpt.Cache.Get(this, "data") as Rows;
        }

        private void SetMyData(Report rpt, Rows data)
        {
            if (data == null)
            {
                rpt.Cache.Remove(this, "data");
            }
            else
            {
                rpt.Cache.AddReplace(this, "data", data);
            }
        }

        private Rows GetMyUserData(Report rpt)
        {
            return rpt.Cache.Get(this, "userdata") as Rows;
        }

        private void SetMyUserData(Report rpt, Rows data)
        {
            if (data == null)
            {
                rpt.Cache.Remove(this, "userdata");
            }
            else
            {
                rpt.Cache.AddReplace(this, "userdata", data);
            }
        }

    }
}
