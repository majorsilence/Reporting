
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign.Resources;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Majorsilence.Forms;
using System.Xml;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// Static utility classes used in the Rdl Designer
    /// </summary>
    internal static class DesignerUtility
	{
		static internal Color ColorFromHtml(string sc, Color dc)
		{
			Color c = dc;
			try 
			{
                if (!sc.StartsWith("="))            // don't even try when color is an expression
				    c = ColorTranslator.FromHtml(sc);
			}
			catch 
			{	// Probably should report this error
			}
			return c;
		}
        
		/// <summary>
		/// Read the registry to find out the ODBC names
		/// </summary>
		static internal void FillOdbcNames(ComboBox cbOdbcNames)
		{
			if (cbOdbcNames.Items.Count > 0)
				return;

			// System names						   
			RegistryKey rk = (Registry.LocalMachine).OpenSubKey("Software");
			if (rk == null)
				return;
			rk = rk.OpenSubKey("ODBC");
			if (rk == null)
				return;
			rk = rk.OpenSubKey("ODBC.INI");
			
			string[] nms = rk.GetSubKeyNames();
			if (nms != null)
			{
				foreach (string name in nms)
				{
					if (name == "ODBC Data Sources" ||
						name == "ODBC File DSN" || name == "ODBC")
						continue;
					cbOdbcNames.Items.Add(name);
				}
			}
			// User names
			rk = (Registry.CurrentUser).OpenSubKey("Software");
			if (rk == null)
				return;
			rk = rk.OpenSubKey("ODBC");
			if (rk == null)
				return;
			rk = rk.OpenSubKey("ODBC.INI");
			nms = rk.GetSubKeyNames();
			if (nms != null)
			{
				foreach (string name in nms)
				{
					if (name == "ODBC Data Sources" ||
						name == "ODBC File DSN" || name == "ODBC")
						continue;
					cbOdbcNames.Items.Add(name);
				}
			}

			return;

		}

		static internal string FormatXml(string sDoc)
		{
			XmlDocument xDoc = new XmlDocument();
			xDoc.PreserveWhitespace = false;
			xDoc.LoadXml(sDoc);						// this will throw an exception if invalid XML
			StringWriter sw = new StringWriter();
			XmlTextWriter xtw = new XmlTextWriter(sw);
			xtw.IndentChar = ' ';
			xtw.Indentation = 2;
			xtw.Formatting = Formatting.Indented;
			
			xDoc.WriteContentTo(xtw);
			xtw.Close();
			sw.Close();
			return sw.ToString();
		}

        static internal bool GetSharedConnectionInfo(RdlUserControl dsr, string filename, out string dataProvider, out string connectInfo)
        {
            dataProvider = null;
            connectInfo = null;

            string pswd = null;
            string xml = "";
            try
            {
                pswd = dsr.GetPassword();
                if (pswd == null)
                    return false;
                if (!filename.EndsWith(".dsr", StringComparison.InvariantCultureIgnoreCase))
                    filename += ".dsr";

                xml = Rdl.DataSourceReference.Retrieve(filename, pswd);
            }
            catch
            {
                MessageBox.Show(Strings.DesignerUtility_Show_SharedConnectionError, Strings.DesignerUtility_Show_TestConnection);
                dsr.ResetPassword();			// make sure to prompt again for the password
                return false;
            }
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xml);
            XmlNode xNodeLoop = xDoc.FirstChild;
            foreach (XmlNode node in xNodeLoop.ChildNodes)
            {
                switch (node.Name)
                {
                    case "DataProvider":
                        dataProvider = node.InnerText;
                        break;
                    case "ConnectString":
                        connectInfo = node.InnerText;
                        break;
                    default:
                        break;
                }
            }
            return true;
        }


        static internal bool GetSharedConnectionInfo(RdlDesigner dsr, string filename, out string dataProvider, out string connectInfo)
        {
            dataProvider = null;
            connectInfo = null;

            string pswd = null;
            string xml = "";
            try
            {
                pswd = dsr.GetPassword();
                if (pswd == null)
                    return false;
                if (!filename.EndsWith(".dsr", StringComparison.InvariantCultureIgnoreCase))
                    filename += ".dsr";

                xml = Rdl.DataSourceReference.Retrieve(filename, pswd);
            }
            catch
            {
                MessageBox.Show(Strings.DesignerUtility_Show_SharedConnectionError, Strings.DesignerUtility_Show_TestConnection);
                dsr.ResetPassword();			// make sure to prompt again for the password
                return false;
            }
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xml);
            XmlNode xNodeLoop = xDoc.FirstChild;
            foreach (XmlNode node in xNodeLoop.ChildNodes)
            {
                switch (node.Name)
                {
                    case "DataProvider":
                        dataProvider = node.InnerText;
                        break;
                    case "ConnectString":
                        connectInfo = node.InnerText;
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
		static internal void GetSqlData(string dataProvider, string connection, string sql, IList parameters, DataTable dt)
		{
			IDbConnection cnSQL=null;
			IDbCommand cmSQL=null;
			IDataReader dr=null;	   
			Cursor saveCursor=Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				// Open up a connection
				cnSQL = RdlEngineConfig.GetConnection(dataProvider, connection);
				if (cnSQL == null)
					return;

				cnSQL.Open();
				cmSQL = cnSQL.CreateCommand();
				cmSQL.CommandText = sql;
				AddParameters(cmSQL, parameters);
				dr = cmSQL.ExecuteReader(CommandBehavior.SingleResult);

				object[] rowValues = new object[dt.Columns.Count];

				while (dr.Read())
				{
					int ci=0;
					foreach (DataColumn dc in dt.Columns)
					{
                        object v = dr[dc.ColumnName];
//                        string val = Convert.ToString(dr[dc.ColumnName], System.Globalization.NumberFormatInfo.InvariantInfo);
//                        rowValues[ci++] = val;
                        rowValues[ci++] = v;
					}
					dt.Rows.Add(rowValues);
				}
			}
			finally
			{
				if (cnSQL != null)
				{
					cnSQL.Close();
					cnSQL.Dispose();
					if (cmSQL != null)
					{
						cmSQL.Dispose();
						if (dr != null)
							dr.Close();
					}
				}
				Cursor.Current=saveCursor;
			}
			return;
		}

        static internal bool GetConnnectionInfo(DesignXmlDraw d, string ds, out string dataProvider, out string connection)
        {
            XmlNode dsNode = d.DataSourceName(ds);
            dataProvider = null;
            connection = null;
            if (dsNode == null)
                return false;

            string dataSourceReference = d.GetElementValue(dsNode, "DataSourceReference", null);
            if (dataSourceReference != null)
            {
                //  This is not very pretty code since it is assuming the structure of the windows parenting.
                //    But there isn't any other way to get this information from here.
                Control p = d;
                MDIChild mc = null;
                while (p != null && !(p is RdlDesigner))
                {
                    if (p is MDIChild)
                        mc = (MDIChild)p;

                    p = p.Parent;
                }
                if (p == null || mc == null || mc.SourceFile == null)
                {
                    MessageBox.Show(Strings.DataSetRowsCtl_ShowC_UnableLocateDSR);
                    return false;
                }
                Uri filename = new Uri(Path.GetDirectoryName(mc.SourceFile.LocalPath) + Path.DirectorySeparatorChar + dataSourceReference);
                if (!DesignerUtility.GetSharedConnectionInfo((RdlDesigner)p, filename.LocalPath, out dataProvider, out connection))
                {
                    return false;
                }
            }
            else
            {
                XmlNode dp = DesignXmlDraw.FindNextInHierarchy(dsNode, "ConnectionProperties", "DataProvider");
                if (dp == null)
                    return false;
                dataProvider = dp.InnerText;
                dp = DesignXmlDraw.FindNextInHierarchy(dsNode, "ConnectionProperties", "ConnectString");
                if (dp == null)
                    return false;
                connection = dp.InnerText;

                if (connection.StartsWith("="))
                {
                    connection = d.GetReportParameterDefaultValue(connection);
                }
            }
            return true;
        }

        public static string ExtractParameterNameFromParameterExpression(string parameterExpression)
        {
            return parameterExpression
                // syntax 1
                .Replace("=Parameters!", "")
                .Replace(".Value", "")
                // syntax 2
                .Replace("={?", "")
                .Replace("}", "")
                ;
        }

        static internal List<SqlColumn> GetSqlColumns(DesignXmlDraw d, string ds, string sql)
		{
            string dataProvider;
            string connection;

            if (!GetConnnectionInfo(d, ds, out dataProvider, out connection))
                return null;

            IList parameters=null;

			return GetSqlColumns(dataProvider, connection, sql, parameters);
		}

		static internal List<SqlColumn> GetSqlColumns(DesignXmlDraw d, string ds, string sql, DataTable parameters)
		{
			string dataProvider;
			string connection;

			if (!GetConnnectionInfo(d, ds, out dataProvider, out connection))
				return null;

            List<SqlColumn> cols = new List<SqlColumn>();

			try
			{
				// Open up a connection
				using (IDbConnection cnSQL = RdlEngineConfig.GetConnection(dataProvider, connection))
				{
					if (cnSQL == null)
						return cols;

					cnSQL.Open();
					IDbCommand cmSQL = cnSQL.CreateCommand();
					cmSQL.CommandText = sql;
					AddParameters(cmSQL, parameters);
					using (IDataReader dr = cmSQL.ExecuteReader(CommandBehavior.SchemaOnly))
					{
						for (int i = 0; i < dr.FieldCount; i++)
						{
							SqlColumn sc = new SqlColumn();
							sc.Name = dr.GetName(i).TrimEnd('\0');
							sc.DataType = dr.GetFieldType(i);
							cols.Add(sc);
						}
					}
				}
			}
			catch (SqlException sqle)
			{
				MessageBox.Show(sqle.Message, Strings.DesignerUtility_Show_SQLError);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.InnerException == null? e.Message:e.InnerException.Message, Strings.DesignerUtility_Show_Error);
			}

			return cols;
		}

        static internal List<SqlColumn> GetSqlColumns(string dataProvider, string connection, string sql, IList parameters)
		{
            List<SqlColumn> cols = new List<SqlColumn>();
			IDbConnection cnSQL=null;
			IDbCommand cmSQL=null;
			IDataReader dr=null;	   
			Cursor saveCursor=Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				// Open up a connection
				cnSQL = RdlEngineConfig.GetConnection(dataProvider, connection);
				if (cnSQL == null)
					return cols;

				cnSQL.Open();
				cmSQL = cnSQL.CreateCommand();
				cmSQL.CommandText = sql;
				AddParameters(cmSQL, parameters);
				dr = cmSQL.ExecuteReader(CommandBehavior.SchemaOnly);
				for (int i=0; i < dr.FieldCount; i++)
				{
					SqlColumn sc = new SqlColumn();
                    sc.Name = dr.GetName(i).TrimEnd('\0');
					sc.DataType = dr.GetFieldType(i);
					cols.Add(sc);
				}
			}
			catch (SqlException sqle)
			{
				MessageBox.Show(sqle.Message, Strings.DesignerUtility_Show_SQLError);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.InnerException == null? e.Message:e.InnerException.Message, Strings.DesignerUtility_Show_Error);
			}
			finally
			{
				if (cnSQL != null)
				{
					if (cmSQL != null)
					{
						cmSQL.Dispose();
						if (dr != null)
							dr.Close();
					}
					cnSQL.Close();
					cnSQL.Dispose();
				}
				Cursor.Current=saveCursor;
			}
			return cols;
		}

        static internal List<SqlSchemaInfo> GetSchemaInfo(DesignXmlDraw d, string ds)
		{
            string dataProvider;
            string connection;

            if (!GetConnnectionInfo(d, ds, out dataProvider, out connection))
                return null;
            
            return GetSchemaInfo(dataProvider, connection);
		}

        static internal List<SqlSchemaInfo> GetSchemaInfo(string dataProvider, string connection)
		{
            List<SqlSchemaInfo> schemaList = new List<SqlSchemaInfo>();
			IDbConnection cnSQL = null;
			IDbCommand cmSQL = null;
			IDataReader dr = null;
			Cursor saveCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;

			// Get the schema information
			try
			{
				int ID_TABLE = 0;
				int ID_TYPE = 1;

				// Open up a connection
				cnSQL = RdlEngineConfig.GetConnection(dataProvider, connection);
				if (cnSQL == null)
				{
					MessageBox.Show(string.Format(Strings.DesignerUtility_Show_ConnectDataProviderError,dataProvider), Strings.DesignerUtility_Show_SQLError);
					return schemaList;
				}
				cnSQL.Open();

                // Take advantage of .Net metadata if available
                if (cnSQL is System.Data.SqlClient.SqlConnection)
                    return GetSchemaInfo((System.Data.SqlClient.SqlConnection) cnSQL, schemaList);
                if (cnSQL is System.Data.Odbc.OdbcConnection)
                    return GetSchemaInfo((System.Data.Odbc.OdbcConnection)cnSQL, schemaList);
                if (cnSQL is System.Data.OleDb.OleDbConnection)
                    return GetSchemaInfo((System.Data.OleDb.OleDbConnection)cnSQL, schemaList);

				// Obtain the query needed to get table/view list
				string sql = RdlEngineConfig.GetTableSelect(dataProvider, cnSQL);
				if (sql == null || sql.Length == 0)		// when no query string; no meta information available
					return schemaList;

				// Obtain the query needed to get table/view list
				cmSQL = cnSQL.CreateCommand();
				cmSQL.CommandText = sql;

				dr = cmSQL.ExecuteReader();
				string type = "TABLE";
				while (dr.Read())
				{
					SqlSchemaInfo ssi = new SqlSchemaInfo();

					if (ID_TYPE >= 0 && 
						dr.FieldCount < ID_TYPE &&
						(string) dr[ID_TYPE] == "VIEW")
						type = "VIEW";

					ssi.Type = type;
					ssi.Name = (string) dr[ID_TABLE];
					schemaList.Add(ssi);
				}
			}
			catch (SqlException sqle)
			{
				MessageBox.Show(sqle.Message, Strings.DesignerUtility_Show_SQLError);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.InnerException == null? e.Message: e.InnerException.Message, Strings.DesignerUtility_Show_Error);
			}
			finally
			{
				if (cnSQL != null)
				{
					cnSQL.Close();
					if (cmSQL != null)
					{
						cmSQL.Dispose();
					}
                    if ((dr != null) && (dataProvider != "SQLite"))
                    {
                        dr.Close();
                    }
				}
				Cursor.Current=saveCursor;
			}
			return schemaList;
		}

        static internal List<SqlSchemaInfo> GetSchemaInfo(System.Data.SqlClient.SqlConnection con, List<SqlSchemaInfo> schemaList)
        {
            try
            { 
                DataTable tbl = con.GetSchema(System.Data.SqlClient.SqlClientMetaDataCollectionNames.Tables); 
                foreach (DataRow row in tbl.Rows) 
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "TABLE";
                    string schema = row["table_schema"] as string;
                    if (schema != null && schema != "dbo")
                        ssi.Name = string.Format("{0}.{1}", schema, (string)row["table_name"]);
                    else
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);
                }
                tbl = con.GetSchema(System.Data.SqlClient.SqlClientMetaDataCollectionNames.Views);
                foreach (DataRow row in tbl.Rows)
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "VIEW";
                    string schema = row["table_schema"] as string;
                    if (schema != null && schema != "dbo")
                        ssi.Name = string.Format("{0}.{1}", schema, (string)row["table_name"]);
                    else
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);

                }
            }
            catch
            { 
            }
            schemaList.Sort();
            return schemaList;
        }

        static internal List<SqlSchemaInfo> GetSchemaInfo(System.Data.OleDb.OleDbConnection con, List<SqlSchemaInfo> schemaList)
        {
            try
            {
                DataTable tbl = con.GetSchema(System.Data.OleDb.OleDbMetaDataCollectionNames.Tables);
                foreach (DataRow row in tbl.Rows)
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "TABLE";
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);
                }
                tbl = con.GetSchema(System.Data.OleDb.OleDbMetaDataCollectionNames.Views);
                foreach (DataRow row in tbl.Rows)
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "VIEW";
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);
                }
            }
            catch
            {
            }
            schemaList.Sort();
            return schemaList;
        }

        static internal List<SqlSchemaInfo> GetSchemaInfo(System.Data.Odbc.OdbcConnection con, List<SqlSchemaInfo> schemaList)
        {
            try
            {
                DataTable tbl = con.GetSchema(System.Data.Odbc.OdbcMetaDataCollectionNames.Tables);
                foreach (DataRow row in tbl.Rows)
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "TABLE";
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);
                }
                tbl = con.GetSchema(System.Data.Odbc.OdbcMetaDataCollectionNames.Views);
                foreach (DataRow row in tbl.Rows)
                {
                    SqlSchemaInfo ssi = new SqlSchemaInfo();

                    ssi.Type = "VIEW";
                    ssi.Name = (string)row["table_name"];
                    schemaList.Add(ssi);
                }
            }
            catch
            {
            }
            schemaList.Sort();
            return schemaList;
        }

        static internal string NormalizeSqlName(string name)
        {	// Routine ensures valid sql name
            if (name == null || name.Length == 0)
                return "";

            // split out the owner name (schema)
            string schema = null;
            int si = name.IndexOf('.');
            if (si >= 0)
            {
                schema = name.Substring(0, si);
                name = name.Substring(si+1);
            }

            bool bLetterOrDigit = (Char.IsLetter(name, 0) || name[0] == '_');   // special rules for 1st character
            for (int i = 0; i < name.Length && bLetterOrDigit; i++)
            {
                if (name[i] == '.')
                { }						// allow names to have a "." for owner qualified tables
                else if (!(Char.IsLetterOrDigit(name, i) || name[i] == '#' || name[i] == '_'))
                    bLetterOrDigit = false;
            }
            if (!bLetterOrDigit)
                name = "\"" + name + "\"";

            if (schema == null)
                return name;
            return schema + "." + name;
        }

		static internal bool TestConnection(string dataProvider, string connection)
		{
			IDbConnection cnSQL=null;
			bool bResult = false;
			try
			{
				cnSQL = RdlEngineConfig.GetConnection(dataProvider, connection);
				cnSQL.Open();
				bResult = true;			// we opened the connection
			}
			catch (Exception e)
			{
				MessageBox.Show(e.InnerException == null? e.Message: e.InnerException.Message, Strings.DesignerUtility_Show_OpenConnectionError);
			}
			finally
			{
				if (cnSQL != null)
				{
					cnSQL.Close();
					cnSQL.Dispose();
				}
			}
			return bResult;
		}

		static internal bool IsNumeric(Type t)
		{
			string st = t.ToString();	
			switch (st)
			{
				case "System.Int16":
				case "System.Int32":
				case "System.Int64":
				case "System.Single":
				case "System.Double":
				case "System.Decimal":
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Validates a size parameter
		/// </summary>
		/// <param name="t"></param>
		/// <param name="bZero">true if 0 is valid size</param>
		/// <param name="bMinus">true if minus is allowed</param>
		/// <returns>Throws exception with the invalid message</returns>
		internal static void ValidateSize(string t, bool bZero, bool bMinus)
		{
			t = t.Trim();
			if (t.Length == 0) // not specified is ok?
				return;

			// Ensure we have valid units
			if (t.IndexOf("in") < 0 &&
			    t.IndexOf("cm") < 0 &&
			    t.IndexOf("mm") < 0 &&
			    t.IndexOf("pt") < 0 &&
			    t.IndexOf("pc") < 0)
			{
				throw new Exception(Strings.DesignerUtility_Error_SizeUnitInvalid);
			}

			int space = t.LastIndexOf(' ');

			string n = ""; // number string
			string u; // unit string

			if (space != -1) // any spaces
			{
				n = t.Substring(0, space).Trim(); // number string
				u = t.Substring(space).Trim(); // unit string
			}
			else if (t.Length >= 3)
			{
				n = t.Substring(0, t.Length - 2).Trim();
				u = t.Substring(t.Length - 2).Trim();
			}


			if (n.Length == 0 || !Regex.IsMatch(n, @"\A[ ]*[-]?[0-9]*[.]?[0-9]*[ ]*\Z"))
			{
				throw new Exception(Strings.DesignerUtility_Error_NumberFormatinvalid);
			}

			float v = DesignXmlDraw.GetSize(t);
			if (!bZero)
			{
				if (v < .1)
					throw new Exception(Strings.DesignerUtility_Error_SizeZero);
			}
			else if (v < 0 && !bMinus)
				throw new Exception(Strings.DesignerUtility_Error_SizeLessZero);
		}

		static internal string MakeValidSize(string t, bool bZero)
		{
			return MakeValidSize(t, bZero, false);
		}

		/// <summary>
		/// Ensures that a user provided string results in a valid size
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		static internal string MakeValidSize(string t, bool bZero, bool bNegative)
		{
			// Ensure we have valid units
			if (t.IndexOf("in") < 0 &&
				t.IndexOf("cm") < 0 &&
				t.IndexOf("mm") < 0 &&
				t.IndexOf("pt") < 0 &&
				t.IndexOf("pc") < 0)
			{
				t += "in";
			}

			float v = DesignXmlDraw.GetSize(t);
			if (!bZero)
			{
				if (v < .1)
					t = ".1pt";
			}
			if (!bNegative)
			{
				if (v < 0)
					t = "0in";
			}

			return t;
		}

		static private void AddParameters(IDbCommand cmSQL, DataTable parameters)
		{
			if (parameters == null || parameters.Rows.Count <= 0)
				return;

			foreach (DataRow param in parameters.Rows)
			{
				string paramName;

				// force the name to start with @
				if (((string)param[0])[0] == '@')
					paramName = (string)param[0];
				else
					paramName = "@" + (string)param[0];

				IDbDataParameter dp = cmSQL.CreateParameter();
				dp.ParameterName = paramName;
				//Perhaps for some databases need fill values, for MySQL works wihtout it
				cmSQL.Parameters.Add(dp);
			}
		}

		static private void AddParameters(IDbCommand cmSQL, IList parameters)
		{
			if (parameters == null || parameters.Count <= 0)
				return;

			foreach(ReportParm rp in parameters)
			{
				string paramName;

				// force the name to start with @
				if (rp.Name[0] == '@')
					paramName = rp.Name;
				else
					paramName = "@" + rp.Name;

				IDbDataParameter dp = cmSQL.CreateParameter();
				dp.ParameterName = paramName;
				if (rp.DefaultValue == null || rp.DefaultValue.Count == 0)
				{
					object pvalue=null;
					// put some dummy values in it;  some drivers (e.g. mysql odbc) don't like null values
					switch (rp.DataType.ToLower())
					{
						case "datetime":
                            pvalue = new DateTime(1900, 1, 1);
							break;
						case "double":
							pvalue = new double();
							break;
						case "boolean":
							pvalue = new Boolean();
							break;
						case "string":
						default:
							pvalue = (object) "";
							break;
					}
					dp.Value = pvalue;
				}
				else
				{
					string val = (string) rp.DefaultValue[0];
					dp.Value = val;
				}

				cmSQL.Parameters.Add(dp);
			}
		}

        // From Paul Welter's site: http://weblogs.asp.net/pwelter34/archive/2006/02/08/437677.aspx

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromDirectory">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string RelativePathTo(string fromDirectory, string toPath)
        {
            if (fromDirectory == null)
                throw new ArgumentNullException("fromDirectory");
            if (toPath == null)
                throw new ArgumentNullException("fromDirectory");
            if (System.IO.Path.IsPathRooted(fromDirectory) && System.IO.Path.IsPathRooted(toPath))
            {
                if (string.Compare(System.IO.Path.GetPathRoot(fromDirectory),
                System.IO.Path.GetPathRoot(toPath), true) != 0)
                {
                    throw new ArgumentException(
                    string.Format("The paths '{0} and '{1}' have different path roots.",
                    fromDirectory, toPath));
                }
            }
            StringCollection relativePath = new StringCollection();
            string[] fromDirectories = fromDirectory.Split(System.IO.Path.DirectorySeparatorChar);
            string[] toDirectories = toPath.Split(System.IO.Path.DirectorySeparatorChar);
            int length = Math.Min(fromDirectories.Length, toDirectories.Length);
            int lastCommonRoot = -1;
            // find common root
            for (int x = 0; x < length; x++)
            {
                if (string.Compare(fromDirectories[x], toDirectories[x], true) != 0)
                    break;
                lastCommonRoot = x;
            }
            if (lastCommonRoot == -1)
            {
                throw new ArgumentException(
                string.Format("The paths '{0} and '{1}' do not have a common prefix path.",
                fromDirectory, toPath));
            }
            // add relative folders in from path
            for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
                if (fromDirectories[x].Length > 0)
                    relativePath.Add("..");
            // add to folders to path
            for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
                relativePath.Add(toDirectories[x]);
            // create relative path
            string[] relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);
            string newPath = string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), relativeParts);
            return newPath;
        }

	}

	internal class SqlColumn
	{
		string _Name;
		Type _DataType;

		override public string ToString()
		{
			return _Name;
		}

		internal string Name
		{
			get {return _Name;}
			set {_Name = value;}
		}

		internal Type DataType
		{
			get {return _DataType;}
			set {_DataType = value;}
		}
	}

	internal class SqlSchemaInfo : IComparable<SqlSchemaInfo>
	{
		string _Name;
		string _Type;

		internal string Name
		{
			get {return _Name;}
			set {_Name = value;}
		}

		internal string Type
		{
			get {return _Type;}
			set {_Type = value;}
		}

        #region IComparable<SqlSchemaInfo> Members

        int IComparable<SqlSchemaInfo>.CompareTo(SqlSchemaInfo other)
        {
            return (this._Type == other._Type)?
                string.Compare(this.Name, other.Name):
                string.Compare(this.Type, other.Type);
        }

        #endregion
	}

	internal class ReportParm
	{
		string _Name;
		string _Prompt;
		string _DataType;
		
		bool   _bDefault=true;				// use default value if true otherwise DataSetName
        List<string> _DefaultValue;			// list of strings
		string _DefaultDSRDataSetName;		// DefaultValues DataSetReference DataSetName
		string _DefaultDSRValueField;		// DefaultValues DataSetReference ValueField

		bool   _bValid=true;				// use valid value if true otherwise DataSetName
        List<ParameterValueItem> _ValidValues;	// list of ParameterValueItem
		string _ValidValuesDSRDataSetName;		// ValidValues DataSetReference DataSetName
		string _ValidValuesDSRValueField;		// ValidValues DataSetReference ValueField
		string _ValidValuesDSRLabelField;		// ValidValues DataSetReference LabelField
		bool _AllowNull;
		bool _AllowBlank;
        bool _MultiValue;

		internal ReportParm(string name)
		{
			_Name = name;
			_DataType = "String";
		}

		internal string Name
		{
			get {return _Name;}
			set {_Name = value;}
		}

		internal string Prompt
		{
			get {return _Prompt;}
			set {_Prompt = value;}
		}

		internal string DataType
		{
			get {return _DataType;}
			set {_DataType = value;}
		}

		internal bool Valid
		{
			get {return _bValid;}
			set {_bValid = value;}
		}

        internal List<ParameterValueItem> ValidValues
		{
			get {return _ValidValues;}
			set {_ValidValues = value;}
		}

		internal string ValidValuesDisplay
		{
			get 
			{
				if (_ValidValues == null || _ValidValues.Count == 0)
					return "";
				StringBuilder sb = new StringBuilder();
				bool bFirst = true;
				foreach (ParameterValueItem pvi in _ValidValues)
				{
					if (bFirst)
						bFirst = false;
					else
						sb.Append(", ");
					if (pvi.Label != null)
						sb.AppendFormat("{0}={1}", pvi.Value, pvi.Label);
					else
						sb.Append(pvi.Value);
				}
				return sb.ToString();
			}
		}

		internal bool Default
		{
			get {return _bDefault;}
			set {_bDefault = value;}
		}

		internal List<string> DefaultValue
		{
			get {return _DefaultValue;}
			set {_DefaultValue = value;}
		}

		internal string DefaultValueDisplay
		{
			get 
			{
				if (_DefaultValue == null || _DefaultValue.Count == 0)
					return "";
				StringBuilder sb = new StringBuilder();
				bool bFirst = true;
				foreach (string dv in _DefaultValue)
				{
					if (bFirst)
						bFirst = false;
					else
						sb.Append(", ");
					sb.Append(dv);
				}
				return sb.ToString();
			}
		}
		internal bool AllowNull
		{
			get {return _AllowNull;}
			set {_AllowNull = value;}
		}

		internal bool AllowBlank
		{
			get {return _AllowBlank;}
			set {_AllowBlank = value;}
		}

        internal bool MultiValue
        {
            get { return _MultiValue; }
            set { _MultiValue = value; }
        }
        internal string DefaultDSRDataSetName
		{
			get {return _DefaultDSRDataSetName;}
			set {_DefaultDSRDataSetName=value;}
		}
		internal string DefaultDSRValueField
		{
			get {return _DefaultDSRValueField;}
			set {_DefaultDSRValueField=value;}
		}

		internal string ValidValuesDSRDataSetName
		{
			get {return _ValidValuesDSRDataSetName;}
			set {_ValidValuesDSRDataSetName=value;}
		}
		internal string  ValidValuesDSRValueField
		{
			get {return _ValidValuesDSRValueField;}
			set {_ValidValuesDSRValueField=value;}
		}
		internal string ValidValuesDSRLabelField
		{
			get {return _ValidValuesDSRLabelField;}
			set {_ValidValuesDSRLabelField=value;}
		}

		override public string ToString()
		{
			return _Name;
		}
	}

	internal class ParameterValueItem
	{
		internal string Value;
		internal string Label;
	}

	internal static class StaticLists
	{
		/// <summary>
		/// Names of colors to put into lists
		/// </summary>
        static public readonly string[] ColorListColorSort = new string[] {
            "Black", 
            "White", 
            "DimGray", 
            "Gray", 
            "DarkGray", 
            "Silver", 
            "LightGray", 
            "Gainsboro",
            "WhiteSmoke", 
            "Maroon", 
            "DarkRed",
            "Red",
            "Brown",
            "Firebrick",
            "IndianRed",
            "Snow",
            "LightCoral",
            "RoseyBrown",
            "MistyRose",
            "Salmon",
            "Tomato",
            "DarkSalmon",
            "Coral",
            "OrangeRed",
            "LightSalmon",
            "Sienna",
            "SeaShell",
            "Chocalate",
            "SaddleBrown",
            "SandyBrown",
            "PeachPuff",
            "Peru",
            "Linen",
            "Bisque",
            "DarkOrange",
            "BurlyWood",
            "Tan",
            "AntiqueWhite",
            "NavajoWhite",
            "BlanchedAlmond",
            "PapayaWhip",
            "Moccasin",
            "Orange",
            "Wheat",
            "OldLace",
            "DarkGoldenrod",
            "Goldenrod",
            "Cornsilk",
            "Gold",
            "Khaki",
            "LemonChiffon",
            "PaleGoldenrod",
            "DarkKhaki",
            "Beige",
            "LightGoldenrodYellow",
            "Olive",
            "Yellow",
            "LightYellow",
            "Ivory",
            "OliveDrab",
            "YellowGreen",
            "DarkOliveGreen",
            "GreenYellow",
            "Chartreuse",
            "LawnGreen",
            "DarkSeaGreen",
            "LightGreen",
            "ForestGreen",
            "LimeGreen",
            "PaleGreen",
            "DarkGreen",
            "Green",
            "Lime",
            "HoneyDew",
            "SeaGreen",
            "MediumSeaGreen",
            "SpringGreen",
            "MintCream",
            "MediumSpringGreen",
            "MediumAquamarine",
            "Aquamarine",
            "Turquoise",
            "LightSeaGreen",
            "MediumTurquoise",
            "DarkSlateGray",
            "PaleTurQuoise",
            "Teal",
            "DarkCyan","Cyan",
            "Aqua",
            "LightCyan",
            "Azure",
            "DarkTurquoise",
            "CadetBlue",
            "PowderBlue",
            "LightBlue",
            "DeepSkyBlue",
            "SkyBlue",
            "LightSkyBlue",
            "SteelBlue",
            "AliceBlue",
            "DodgerBlue",
            "SlateGray",
            "LightSlateGray",
            "LightSteelBlue",
            "CornflowerBlue",
            "RoyalBlue",
            "MidnightBlue",
            "Lavender",
            "Navy",
            "DarkBlue",
            "MediumBlue",
            "Blue",
            "GhostWhite",
            "SlateBlue",
            "DarkSlateBlue",
            "MediumSlateBlue",
            "MediumPurple",
            "BlueViolet",
            "Indigo",
            "DarkOrchid",
            "DarkViolet",
            "MediumOrchid",
            "Thistle",
            "Plum",
            "Violet",
            "Purple",
            "DarkMagenta",
            "Fuchsia",
            "Magenta",
            "Orchid",
            "MediumVioletRed",
            "DeepPink",
            "HotPink",
            "LavenderBlush",
            "PaleVioletRed",
            "Crimson",
            "Pink",
            "LightPink",
			"Floralwhite"
        };
		static public readonly string[] ColorList = new string[] {
										"Aliceblue",
										"Antiquewhite",
										"Aqua",
										"Aquamarine",
										"Azure",
										"Beige",
										"Bisque",
										"Black",
										"Blanchedalmond",
										"Blue",
										"Blueviolet",
										"Brown",
										"Burlywood",
										"Cadetblue",
										"Chartreuse",
										"Chocolate",
										"Coral",
										"Cornflowerblue",
										"Cornsilk",
										"Crimson",
										"Cyan",
										"Darkblue",
										"Darkcyan",
										"Darkgoldenrod",
										"Darkgray",
										"Darkgreen",
										"Darkkhaki",
										"Darkmagenta",
										"Darkolivegreen",
										"Darkorange",
										"Darkorchid",
										"Darkred",
										"Darksalmon",
										"Darkseagreen",
										"Darkslateblue",
										"Darkslategray",
										"Darkturquoise",
										"Darkviolet",
										"Deeppink",
										"Deepskyblue",
										"Dimgray",
										"Dodgerblue",
										"Firebrick",
										"Floralwhite",
										"Forestgreen",
										"Fuchsia",
										"Gainsboro",
										"Ghostwhite",
										"Gold",
										"Goldenrod",
										"Gray",
										"Green",
										"Greenyellow",
										"Honeydew",
										"Hotpink",
										"Indianred", 
                                        "Indigo",
										"Ivory",
										"Khaki",
										"Lavender",
										"Lavenderblush",
										"Lawngreen",
										"Lemonchiffon",
										"Lightblue",
										"Lightcoral",
										"Lightcyan",
										"Lightgoldenrodyellow",
										"Lightgreen",
										"Lightgrey",
										"Lightpink",
										"Lightsalmon",
										"Lightseagreen",
										"Lightskyblue",
										"Lightslategrey",
										"Lightsteelblue",
										"Lightyellow",
										"Lime",
										"Limegreen",
										"Linen",
										"Magenta",
										"Maroon",
										"Mediumaquamarine",
										"Mediumblue",
										"Mediumorchid",
										"Mediumpurple",
										"Mediumseagreen",
										"Mediumslateblue",
										"Mediumspringgreen",
										"Mediumturquoise",
										"Mediumvioletred",
										"Midnightblue",
										"Mintcream",
										"Mistyrose",
										"Moccasin",
										"Navajowhite",
										"Navy",
										"Oldlace",
										"Olive",
										"Olivedrab",
										"Orange",
										"Orangered",
										"Orchid",
										"Palegoldenrod",
										"Palegreen",
										"Paleturquoise",
										"Palevioletred",
										"Papayawhip",
										"Peachpuff",
										"Peru",
										"Pink",
										"Plum",
										"Powderblue",
										"Purple",
										"Red",
										"Rosybrown",
										"Royalblue",
										"Saddlebrown",
										"Salmon",
										"Sandybrown",
										"Seagreen",
										"Seashell",
										"Sienna",
										"Silver",
										"Skyblue",
										"Slateblue",
										"Slategray",
										"Snow",
										"Springgreen",
										"Steelblue",
										"Tan",
										"Teal",
										"Thistle",
										"Tomato",
										"Turquoise",
										"Violet",
										"Wheat",
										"White",
										"Whitesmoke",
										"Yellow",
										"Yellowgreen"};
		/// <summary>
		/// Names of globals to put into expressions
		/// </summary>
		static public readonly string[] GlobalList = new string[] {
																	  "=Globals!PageNumber",
																	  "=Globals!TotalPages",
																	  "=Globals!ExecutionTime",
																	  "=Globals!ReportFolder",
		                                                              "=Globals!ReportName"};
        /// <summary>
        /// Names of user info to put into expressions GJL AJM 12082008
        /// </summary>
        static public readonly string[] UserList = new string[] {
																	  "=User!UserID",
																	  "=User!Language"};


        static public readonly string[] OperatorList = new string[] { 
                                " & ", " + "," - "," * "," / "," mod ", 
								" and ", " or ", " !", 
								" = ", " != ", " > ", " >= ", " < ", " <= "};

		/// <summary>
		/// Names of functions with pseudo arguments
		/// </summary>
		static public readonly string[] FunctionList = new string[] {	"Iif(boolean, trueExpr, falseExpr)",
																		"Choose(number, choice1 [,choice2]...)",
																		"Switch(boolean1, value1[, boolean2, value2]...[elseValue])",
																		"Format(expr, formatExpr)"};

		static public readonly string[] AggrFunctionList = new string[] {"Sum(number [, scope])",
																		"Aggregate(number [, scope])",
																		"Avg(number [, scope])",
																		"Min(expr [, scope])",
																		"Max(expr [, scope])",
																		"First(expr [, scope])",
																		"Last(expr [, scope])",
																		"Next(expr [, scope])",
																		"Previous(expr [, scope])",
																		"Level([scope])",
																		"Count(expr [, scope])",
																		"Countrows(expr [, scope])",
																		"Countdistinct(expr [, scope])",
																		"RowNumber()",
																		"Runningvalue(expr, sum [, scope])",
																		"Runningvalue(expr, avg [, scope])",
																		"Runningvalue(expr, count [, scope])",
																		"Runningvalue(expr, max [, scope])",
																		"Runningvalue(expr, min [, scope])",
																		"Runningvalue(expr, stdev [, scope])",
																		"Runningvalue(expr, stdevp [, scope])",
																		"Runningvalue(expr, var [, scope])",
																		"Runningvalue(expr, varp [, scope])",
																		"Stdev(expr [, scope])",
																		"Stdevp(expr [, scope])",
																		"Var(expr [, scope])",
																		"Varp(expr [, scope])"};
		/// <summary>
		/// Zoom values
		/// </summary>
		static public readonly string[] ZoomList = new string[] {
									"Actual Size",
									"Fit Page",
									"Fit Width",
									"800%",
									"400%",
									"200%",
									"150%",
									"125%",
									"100%",
									"75%",
									"50%",
									"25%"};
        /// <summary>
        /// List of format values
        /// </summary>
        static public readonly string[] FormatList = new string[] { "",
            "#,##0", "#,##0.00", "0", "0.00", "", "MM/dd/yyyy",
            "dddd, MMMM dd, yyyy", "dddd, MMMM dd, yyyy HH:mm",
            "dddd, MMMM dd, yyyy HH:mm:ss", "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy HH:mm:ss", "MMMM dd",
            "Ddd, dd MMM yyyy HH\':\'mm\'\"ss \'GMT\'",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss GMT",
            "HH:mm", "HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss", "html"};

        /// <summary>
        /// list of gradient types
        /// </summary>
        static public readonly string[] GradientList = new string[] {
        "None", "LeftRight", "TopBottom", "Center", "DiagonalLeft",
        "DiagonalRight", "HorizontalCenter", "VerticalCenter"};

		//Josh: 6:22:10 Added to place the start and end shortcut "caps" on the fields.
		public static List<string> ArrayToFormattedList(IEnumerable<string> array, string frontCap, string endCap)
		{
            return array.Select(item => item.StartsWith("=") ?
                string.Format("{0}{1}{2}",
                frontCap,
                item.Split('!')[1].Replace(".Value", string.Empty),
                endCap)
                : item).ToList();
		}

	}
}
