

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text;
using Majorsilence.Reporting.RdlEngine.Resources;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	///<summary>
	/// The definition of a Subreport (report name, parameters, ...).
	///</summary>
	[Serializable]
	internal class Subreport : ReportItem
	{
		string _ReportName;		// The full path (e.g. �/salesreports/orderdetails�) or
								// relative path (e.g. �orderdetails�) to a subreport.
								// Relative paths start in the same folder as the current
								// 
								// Cannot be an empty string (ignoring whitespace)
		SubReportParameters _Parameters;	//Parameters to the Subreport
								// If the subreport is executed without parameters
								// (and contains no Toggle elements), it will only be
								// executed once (even if it appears inside of a list,
								// table or matrix)
		Expression _NoRows;		// (string)	Message to display in the subreport (instead of the
								// region layout) when no rows of data are available
								// in any data set in the subreport
								// Note: Style information on the subreport applies to
								// this text.
		bool _MergeTransactions;	// Indicates that transactions in the subreport should
								//be merged with transactions in the parent report
								//(into a single transaction for the entire report) if the
								//data sources use the same connection.	
	
		ReportDefn _ReportDefn;	// loaded report definition

        // EBN 30/03/2014
        // Store the cross object
        CrossDelegate _SubReportGetContent = new CrossDelegate();

		internal Subreport(ReportDefn r, ReportLink p, XmlNode xNode) :base(r, p, xNode)
		{
			_ReportName=null;
			_Parameters=null;
			_NoRows=null;
			_MergeTransactions=true;
            _SubReportGetContent = r.SubReportGetContent;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name.ToLowerInvariant())
				{
					case "reportname":
						_ReportName = xNodeLoop.InnerText;
						break;
					case "parameters":
						_Parameters = new SubReportParameters(r, this, xNodeLoop);
						break;
					case "norows":
						_NoRows = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					case "mergetransactions":
						_MergeTransactions = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:	
						if (ReportItemElement(xNodeLoop))	// try at ReportItem level
							break;
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Image element " + xNodeLoop.Name + " ignored.");
						break;
				}
			}
		
			if (_ReportName == null)
				OwnerReport.rl.LogError(8, "Subreport requires the ReportName element.");
			
			OwnerReport.ContainsSubreport = true;	// owner report contains a subreport
		}

		// Handle parsing of function in final pass
		override async internal Task FinalPass()
		{
			await base.FinalPass();

			// Subreports aren't allowed in PageHeader or PageFooter; 
			if (this.InPageHeaderOrFooter())
				OwnerReport.rl.LogError(8, String.Format("The Subreport '{0}' is not allowed in a PageHeader or PageFooter", this.Name == null? "unknown": Name.Nm) );

			if (_Parameters != null)
                await _Parameters.FinalPass();
			if (_NoRows != null)
                await _NoRows.FinalPass();

			_ReportDefn = await GetReport(OwnerReport.ParseFolder);
            if (_ReportDefn != null)    // only null in error case (e.g. subreport not found)
			    _ReportDefn.Subreport = this;
			return;
		}

		async override internal Task Run(IPresent ip, Row row)
		{
			Report r = ip.Report();
            await base.Run(ip, row);

			// need to save the owner report and nest in this defintion
			ReportDefn saveReport = r.ReportDefinition;
#if !AOT
            NeedPassword np = r.GetDataSourceReferencePassword;   // get current password
#endif
            r.SetReportDefinition(_ReportDefn);
			r.Folder = _ReportDefn.ParseFolder;		// folder needs to get set since the id of the report is used by the cache
#if !AOT
            r.GetDataSourceReferencePassword = np;
#endif
		
            DataSourcesDefn saveDS = r.ParentConnections;
			if (this.MergeTransactions)
				r.ParentConnections = saveReport.DataSourcesDefn;
			else
				r.ParentConnections = null;

            r.SubreportDataRetrievalTriggerEvent();

			if (_Parameters == null)
			{	// When no parameters we only retrieve data once
				if (r.Cache.Get(this, "report") == null)
				{
                    await r.RunGetData(null);
                    if (!r.IsSubreportDataRetrievalDefined)       // if use has defined subreportdataretrieval they might
                        r.Cache.Add(this, "report", this);      //    set the data; so we don't cache
				}
			}
			else
			{
                await SetSubreportParameters(r, row);
                await r.RunGetData(null);
			}

            await ip.Subreport(this, row);

			r.SetReportDefinition(saveReport);			// restore the current report
			r.ParentConnections = saveDS;				// restore the data connnections
		}

        async override internal Task RunPage(Pages pgs, Row row)
		{
			Report r = pgs.Report;
			if (await IsHidden(r, row))
				return;

            await base.RunPage(pgs, row);

			// need to save the owner report and nest in this defintion
			ReportDefn saveReport = r.ReportDefinition;
#if !AOT
            NeedPassword np = r.GetDataSourceReferencePassword;   // get current password
#endif

			r.SetReportDefinition(_ReportDefn);
			r.Folder = _ReportDefn.ParseFolder;		// folder needs to get set since the id of the report is used by the cache
#if !AOT
            r.GetDataSourceReferencePassword = np;
#endif

			DataSourcesDefn saveDS = r.ParentConnections;
			if (this.MergeTransactions)
				r.ParentConnections = saveReport.DataSourcesDefn;
			else
			    r.ParentConnections = null;

            r.SubreportDataRetrievalTriggerEvent();

            bool bRows = true;
			if (_Parameters == null)
			{	// When no parameters we only retrieve data once
                SubreportWorkClass wc = r.Cache.Get(this, "report") as SubreportWorkClass;

				if (wc == null)
				{   // run report first time; 
					bRows = await r.RunGetData(null);
                    if (!r.IsSubreportDataRetrievalDefined)       // if use has defined subreportdataretrieval they might set data
                        r.Cache.Add(this, "report", new SubreportWorkClass(bRows));	    // so we can't cache
				}
                else
                    bRows = wc.bRows;
			}
			else
			{
                await SetSubreportParameters(r, row);		// apply the parameters
				bRows = await r.RunGetData(null);
			}

			SetPageLeft(r);				// Set the Left attribute since this will be the margin for this report

			SetPagePositionBegin(pgs);

            float yOffset;

            if (bRows)  // Only run subreport if have a row in some Dataset
            {
                //
                // Run the subreport -- this is the major effort in creating the display objects in the page
                //
                await r.ReportDefinition.Body.RunPage(pgs);		// create a the subreport items
                yOffset = pgs.CurrentPage.YOffset;
            }
            else
            {   // Handle NoRows message 
                string msg;
                if (this.NoRows != null)
                    msg = await this.NoRows.EvaluateString(pgs.Report, null);
                else
                    msg = null;

                if (msg != null)
                {
                    PageText pt = new PageText(msg);
                    await SetPagePositionAndStyle(pgs.Report, pt, null);

                    if (pt.SI.BackgroundImage != null)
                        pt.SI.BackgroundImage.H = pt.H;		//   and in the background image

                    pgs.CurrentPage.AddObject(pt);

                    yOffset = pt.Y + pt.H;
                }
                else
                    yOffset = pgs.CurrentPage.YOffset;
            }

			r.SetReportDefinition(saveReport);			// restore the current report
			r.ParentConnections = saveDS;				// restore the data connnections

			SetPagePositionEnd(pgs, yOffset);
		}

        internal override void RemoveWC(Report rpt)
        {
            base.RemoveWC(rpt);

            if (_ReportDefn == null)
                return;

            _ReportDefn.Body.RemoveWC(rpt);
        }

		private async Task<ReportDefn> GetReport(string folder)
		{
			string prog;
			string name;

			if (_ReportName[0] == Path.DirectorySeparatorChar ||
				_ReportName[0] == Path.AltDirectorySeparatorChar)
				name = _ReportName;
			else 
				name = Path.Combine (folder, _ReportName);

			if(!Path.HasExtension (name))
				name = Path.ChangeExtension (name, ".rdl");

			// Load and Compile the report
			RDLParser rdlp;
			Report r;
			ReportDefn rdefn=null;
			try
			{
				prog = GetRdlSource(name);
				rdlp =  new RDLParser(prog);
				rdlp.Folder = folder;
				if(OwnerReport.OverwriteInSubreport)
				{
					rdlp.OverwriteConnectionString = OwnerReport.OverwriteConnectionString;
					rdlp.OverwriteInSubreport = OwnerReport.OverwriteInSubreport;
				}

				r = await rdlp.Parse(OwnerReport.GetObjectNumber());
				OwnerReport.SetObjectNumber(r.ReportDefinition.GetObjectNumber());
				if (r.ErrorMaxSeverity > 0) 
				{
					string err;
					if (r.ErrorMaxSeverity > 4)
						err = string.Format("Subreport {0} failed to compile with the following errors.", this._ReportName);
					else
						err = string.Format("Subreport {0} compiled with the following warnings.", this._ReportName);
					OwnerReport.rl.LogError(r.ErrorMaxSeverity, err);
					OwnerReport.rl.LogError(r.rl);	// log all these errors
					OwnerReport.rl.LogError(0, "End of Subreport errors");
				}
				// If we've loaded the report; we should tell it where it got loaded from
				if (r.ErrorMaxSeverity <= 4) 
				{	
					rdefn = r.ReportDefinition;
				}
			}
			catch (Exception ex)
			{
				OwnerReport.rl.LogError(8, string.Format("Subreport {0} failed with exception. {1}", this._ReportName, ex.Message));
			}
			return rdefn;
		}

		private string GetRdlSource(string name)
		{
			// TODO: at some point might want to provide interface so that read can be controlled
			//         by server:  would allow for caching etc.
            //if (this.OwnerReport)

            // EBN 30/03/2014
            // If a cross object has been defined, use it to get the content of the sub report
            if (_SubReportGetContent.SubReportGetContent != null)
            {
                try
                {
                    return _SubReportGetContent.SubReportGetContent(name);
                }
                catch
                {
                    return null;
                }
            }
   
            StreamReader fs=null;
			string prog=null;
			try
			{
				fs = new StreamReader(name);		
				prog = fs.ReadToEnd();
			}
			finally
			{
				if (fs != null)
					fs.Close();
			}

			return prog;
		}

		private async Task SetSubreportParameters(Report rpt, Row row)
		{
			UserReportParameter userp;
			foreach (SubreportParameter srp in _Parameters.Items)
			{
				userp=null;						
				foreach (UserReportParameter urp in rpt.UserReportParameters)
				{
					if (urp.Name == srp.Name.Nm)
					{
						userp = urp;
						break;
					}
				}
				if (userp == null)
				{	// parameter name not found
					throw new Exception(
						string.Format(Strings.Subreport_Error_SubreportNotParameter, _ReportName, srp.Name.Nm));
				}
				object v = await srp.Value.Evaluate(rpt, row);
				userp.Value = v;
			}
		}

		internal string ReportName
		{
			get { return  _ReportName; }
			set {  _ReportName = value; }
		}

		internal ReportDefn ReportDefn
		{
			get { return _ReportDefn; }
		}

		internal SubReportParameters Parameters
		{
			get { return  _Parameters; }
			set {  _Parameters = value; }
		}

		internal Expression NoRows
		{
			get { return  _NoRows; }
			set {  _NoRows = value; }
		}

		internal bool MergeTransactions
		{
			get { return  _MergeTransactions; }
			set {  _MergeTransactions = value; }
		}
	}

    class SubreportWorkClass
    {
        internal bool bRows;
        internal SubreportWorkClass(bool rows)
        {
            bRows = rows;
        }
    }
}
