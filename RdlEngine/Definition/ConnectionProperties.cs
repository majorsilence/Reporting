

using System;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// Information about how to connect to the data source.
	///</summary>
	[Serializable]
	internal class ConnectionProperties : ReportLink
	{
		string _DataProvider;	// The type of the data source. This will determine
								// the syntax of the Connectstring and
								// CommandText. Supported types are SQL, OLEDB, ODBC, Oracle
		Expression _ConnectString;	// The connection string for the data source
		bool _IntegratedSecurity;	// Indicates that this data source should connected
									// to using integrated security
		string _Prompt;			// The prompt displayed to the user when
								// prompting for database credentials for this data source.
	
		internal ConnectionProperties(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_DataProvider=null;
			_ConnectString=null;
			_IntegratedSecurity=false;
			_Prompt=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "DataProvider":
						_DataProvider = xNodeLoop.InnerText;
						break;
					case "ConnectString":
					_ConnectString = String.IsNullOrWhiteSpace (r.OverwriteConnectionString) 
						? new Expression(r, this, xNodeLoop, ExpressionType.String)
						: new Expression(r, this, r.OverwriteConnectionString, ExpressionType.String);
						break;
					case "IntegratedSecurity":
						_IntegratedSecurity = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					case "Prompt":
						_Prompt = xNodeLoop.InnerText;
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ConnectionProperties element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (_DataProvider == null)
				OwnerReport.rl.LogError(8, "ConnectionProperties DataProvider is required.");
			if (_ConnectString == null)
				OwnerReport.rl.LogError(8, "ConnectionProperties ConnectString is required.");
		}
		
		async override internal Task FinalPass()
		{
			if (_ConnectString != null)
                await _ConnectString.FinalPass().ConfigureAwait(false);
			return;
		}

		internal string DataProvider
		{
			get { return  _DataProvider; }
			set {  _DataProvider = value; }
		}

		internal async Task<string> Connectstring(Report rpt)
		{
			return await _ConnectString.EvaluateString(rpt, null).ConfigureAwait(false);
		}

		internal string ConnectstringValue
		{
			get {return _ConnectString==null?null:_ConnectString.Source; }
		}

		internal bool IntegratedSecurity
		{
			get { return  _IntegratedSecurity; }
			set {  _IntegratedSecurity = value; }
		}

		internal string Prompt
		{
			get { return  _Prompt; }
			set {  _Prompt = value; }
		}
	}
}
