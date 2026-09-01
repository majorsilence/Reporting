using fyiReporting.RDL;
using fyiReporting.RdlPrint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace fyiReporting.RDLPrint
{
    /// <summary>
    /// Prints report unattended or SaveAs saves report
    /// </summary>
    public partial class RdlPrint
    {
        public event EventHandler<SubreportDataRetrievalEventArgs> SubreportDataRetrieval;

        private PageDrawing _DrawPanel = new PageDrawing();

        public NeedPassword GetDataSourceReferencePassword = null;
        private Uri _SourceFileName;  	// file name to use
        private string _SourceRdl;			// source Rdl; if provided overrides filename
        private string _Parameters;			// parameters to run the report
        private Report _Report;				// the report
        private string _Folder;				// folder for DataSourceReference (if file name not provided)
        private Pages _pgs;					// the pages of the report to view
        private bool _loadFailed;			// last load of report failed
        private float _PageWidth;			// width of page
        private float _PageHeight;			// height of page
        private string _ReportDescription;
        private string _ReportAuthor;
        private string _ReportName;
        private IList _errorMsgs;

        private bool _UseTrueMargins = true;    // compensate for non-printable region
        private int printEndPage;			// end page
        private int printCurrentPage;		// current page to print

        public RdlPrint()
        {
            _SourceFileName = null;
            _SourceRdl = null;
            _Parameters = null;				// parameters to run the report
            _pgs = null;						// the pages of the report to view
            _loadFailed = false;
            _PageWidth = 0;
            _PageHeight = 0;
            _ReportDescription = null;
            _ReportAuthor = null;
            _ReportName = null;
        }

        /// <summary>
        /// When true printing will compensate for non-printable area of paper
        /// </summary>
        public bool UseTrueMargins
        {
            get { return _UseTrueMargins; }
            set { _UseTrueMargins = value; }
        }
        public int PageCount
        {
            get
            {
                LoadPageIfNeeded();
                if (_pgs == null)
                    return 0;
                else
                    return _pgs.PageCount;
            }
        }

        /// <summary>
        /// Gets the report definition.
        /// </summary>
        public Report Report
        {
            get
            {
                LoadPageIfNeeded();
                return _Report;
            }
        }

        /// <summary>
        /// Holds the XML source of the report in RDL (Report Specification Language).
        /// SourceRdl is mutually exclusive with SourceFile.  Setting SourceRdl will nullify SourceFile.
        /// </summary>
        public string SourceRdl
        {
            get { return _SourceRdl; }
            set
            {
                _SourceRdl = value;
                if (value != null)
                    _SourceFileName = null;
                //_pgs = null;				// reset pages
                //_DrawPanel.Pgs = null;
                //_loadFailed = false;			// attempt to load the report	
                //_vScroll.Value = _hScroll.Value = 0;
                //if (this.Visible)
                //{
                //    LoadPageIfNeeded();			// force load of report
                //    this._DrawPanel.Invalidate();
                //}
            }
        }

        /// <summary>
        /// Holds the folder to data source reference files when SourceFileName not available.
        /// </summary>
        public string Folder
        {
            get { return _Folder; }
            set { _Folder = value; }
        }

        /// <summary>
        /// Parameters passed to report when run.  Parameters are separated by '&'.  For example,
        /// OrderID=10023&OrderDate=10/14/2002
        /// Note: these parameters will override the user specified ones.
        /// </summary>
        public string Parameters
        {
            get { return _Parameters; }
            set { _Parameters = value; }
        }

        /// <summary>
        /// The height of the report page (in points) as defined within the report.
        /// </summary>
        public float PageHeight
        {
            get
            {
                LoadPageIfNeeded();
                return _PageHeight;
            }
        }

        /// <summary>
        /// The width of the report page (in points) as defined within the report.
        /// </summary>
        public float PageWidth
        {
            get
            {
                LoadPageIfNeeded();
                return _PageWidth;
            }
        }

        /// <summary>
        /// Description of the report.
        /// </summary>
        public string ReportDescription
        {
            get
            {
                LoadPageIfNeeded();
                return _ReportDescription;
            }
        }

        /// <summary>
        /// Author of the report.
        /// </summary>
        public string ReportAuthor
        {
            get
            {
                LoadPageIfNeeded();
                return _ReportAuthor;
            }
        }

        /// <summary>
        /// Name of the report.
        /// </summary>
        public string ReportName
        {
            get
            {
                return _ReportName;
            }
            set { _ReportName = value; }
        }

        /// <summary>
        /// Print the report.
        /// </summary>
        public void Print(PrintDocument pd)
        {
            LoadPageIfNeeded();

            pd.PrintPage += new PrintPageEventHandler(PrintPage);

            // This is a work around where many printers do not support printing
            // more then one copy.  This will cause a prompt for each copy if using XPS
            //
            // http://msdn.microsoft.com/en-us/library/system.drawing.printing.printersettings.copies.aspx
            // "Not all printers support printing multiple copes. You can use the MaximumCopies property 
            // to determine the maximum number of copies the printer supports. If the number of 
            // copies is set higher than the maximum copies supported by the printer, only the 
            // maximum number of copies will be printed, and no exception will occur."
            // 
            if (pd.PrinterSettings.MaximumCopies == 1 && pd.PrinterSettings.Copies > 1)
            {
                for (int i = 0; i < pd.PrinterSettings.Copies; i++)
                {
                    _Print(pd);
                }
            }
            else
            {
                _Print(pd);
            }
        }
        private void _Print(PrintDocument pd)
        {
            printCurrentPage = -1;
            switch (pd.PrinterSettings.PrintRange)
            {
                case PrintRange.AllPages:
                    printCurrentPage = 0;
                    printEndPage = _pgs.PageCount - 1;
                    break;
                case PrintRange.Selection:
                    printCurrentPage = pd.PrinterSettings.FromPage - 1;
                    printEndPage = pd.PrinterSettings.FromPage - 1;
                    break;
                case PrintRange.SomePages:
                    printCurrentPage = pd.PrinterSettings.FromPage - 1;
                    if (printCurrentPage < 0)
                        printCurrentPage = 0;
                    printEndPage = pd.PrinterSettings.ToPage - 1;
                    if (printEndPage >= _pgs.PageCount)
                        printEndPage = _pgs.PageCount - 1;
                    break;
            }
            pd.Print();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            System.Drawing.Rectangle r = new System.Drawing.Rectangle(0, 0, int.MaxValue, int.MaxValue);
            // account for the non-printable area of the paper
            PointF pageOffset;
            if (this.UseTrueMargins && this._Report != null)
            {
                // The page offset is set in pixels as the Draw method changes the graphics object to use pixels
                // (the origin transform does not get changed by the change in units.  PrintableArea returns
                // numbers in the hundredths of an inch.

                float x = ((e.PageSettings.PrintableArea.X * e.Graphics.DpiX) / 100.0F) - e.Graphics.Transform.OffsetX;
                float y = ((e.PageSettings.PrintableArea.Y * e.Graphics.DpiY) / 100.0F) - e.Graphics.Transform.OffsetY;

                // Get the margins in printer pixels (don't use the function!)
                // Points to pixels conversion ((double)x * DpiX / POINTSIZEF)
                float lm = (float)((double)_Report.LeftMarginPoints * e.Graphics.DpiX / POINTSIZEF);
                float tm = (float)((double)_Report.TopMarginPoints * e.Graphics.DpiY / POINTSIZEF);
                // Correct based on the report margin
                if (x > lm)      // left margin is less than the minimum left margin
                    x = 0;
                if (y > tm)      // top margin is less than the minimum top margin
                    y = 0;
                pageOffset = new PointF(-x, -y);
            }
            else
            {
                pageOffset = PointF.Empty;
            }

            _DrawPanel.Draw(e.Graphics, printCurrentPage, r, false, pageOffset);

            printCurrentPage++;
            if (printCurrentPage > printEndPage)
                e.HasMorePages = false;
            else
                e.HasMorePages = true;
        }


        /// <summary>
        /// Save the file.  The extension determines the type of file to save.
        /// </summary>
        /// <param name="FileName">Name of the file to be saved to.</param>
        /// <param name="type">Type of file to save.  Should be "pdf", "xml", "html", "mhtml", "csv", "rtf", "excel", "tif".</param>
        //public void SaveAs(string FileName, fyiReporting.RDL.OutputPresentationType type)
        //{
        //    LoadPageIfNeeded();


        //    OneFileStreamGen sg = new OneFileStreamGen(FileName, true);	// overwrite with this name
        //    if (!(type == OutputPresentationType.PDF || type == OutputPresentationType.PDFOldStyle ||
        //        type == OutputPresentationType.TIF || type == OutputPresentationType.TIFBW))
        //    {
        //        ListDictionary ld = GetParameters();		// split parms into dictionary
        //        _Report.RunGetData(ld);                     // obtain the data (again)
        //    }
        //    try
        //    {
        //        switch (type)
        //        {
        //            case OutputPresentationType.PDF:
        //                _Report.ItextPDF = true;
        //                _Report.RunRenderPdf(sg, _pgs);
        //                break;
        //            case OutputPresentationType.PDFOldStyle:
        //                _Report.ItextPDF = false;
        //                _Report.RunRenderPdf(sg, _pgs);
        //                break;
        //            case OutputPresentationType.TIF:
        //                _Report.RunRenderTif(sg, _pgs, true);
        //                break;
        //            case OutputPresentationType.TIFBW:
        //                _Report.RunRenderTif(sg, _pgs, false);
        //                break;
        //            case OutputPresentationType.CSV:
        //                _Report.RunRender(sg, OutputPresentationType.CSV);
        //                break;
        //            case OutputPresentationType.Word:
        //            case OutputPresentationType.RTF:
        //                _Report.RunRender(sg, OutputPresentationType.RTF);
        //                break;
        //            case OutputPresentationType.Excel:
        //                _Report.RunRender(sg, OutputPresentationType.Excel);
        //                break;
        //            case OutputPresentationType.XML:
        //                _Report.RunRender(sg, OutputPresentationType.XML);
        //                break;
        //            case OutputPresentationType.HTML:
        //                _Report.RunRender(sg, OutputPresentationType.HTML);
        //                break;
        //            case OutputPresentationType.MHTML:
        //                _Report.RunRender(sg, OutputPresentationType.MHTML);
        //                break;
        //            default:
        //                throw new Exception("Unsupported file extension for SaveAs");
        //        }
        //    }
        //    finally
        //    {
        //        if (sg != null)
        //        {
        //            sg.CloseMainStream();
        //        }


        //    }
        //    return;
        //}

        private float POINTSIZEF = 72f;   // see RSize.POINTSIZED - 1pt = 1/72in

        private Report GetReport()
        {
            string prog;

            // Obtain the source
            if (_loadFailed)
                prog = GetReportErrorMsg();
            else if (_SourceRdl != null)
                prog = _SourceRdl;
            else if (_SourceFileName != null)
                prog = GetRdlSource();
            else
                prog = GetReportEmptyMsg();

            // Compile the report
            // Now parse the file
            RDLParser rdlp;
            Report r;
            //try
            //{
            _errorMsgs = null;
            rdlp = new RDLParser(prog);
            rdlp.DataSourceReferencePassword = GetDataSourceReferencePassword;
            if (_SourceFileName != null)
                rdlp.Folder = Path.GetDirectoryName(_SourceFileName.LocalPath);
            else
                rdlp.Folder = this.Folder;

            r = rdlp.Parse();
            if (r.ErrorMaxSeverity > 0)
            {
                _errorMsgs = r.ErrorItems;		// keep a copy of the errors

                int severity = r.ErrorMaxSeverity;
                r.ErrorReset();
                if (severity > 4)
                {
                    r = null;			// don't return when severe errors
                    _loadFailed = true;
                }
            }
            // If we've loaded the report; we should tell it where it got loaded from
            if (r != null && !_loadFailed)
            {	// Don't care much if this fails; and don't want to null out report if it does
                try
                {
                    if (_SourceFileName != null)
                    {
                        r.Name = Path.GetFileNameWithoutExtension(_SourceFileName.LocalPath);
                        r.Folder = Path.GetDirectoryName(_SourceFileName.LocalPath);
                    }
                    else
                    {
                        r.Folder = this.Folder;
                        r.Name = this.ReportName;
                    }
                }
                catch { }
            }
            //}
            //catch (Exception ex)
            //{
            //    _loadFailed = true;
            //    _errorMsgs = new List<string>();		// create new error list
            //    _errorMsgs.Add(ex.Message);			// put the message in it
            //    _errorMsgs.Add(ex.StackTrace);		//   and the stack trace
            //    r = null;
            //}

            if (r != null)
            {
                _PageWidth = r.PageWidthPoints;
                _PageHeight = r.PageHeightPoints;
                _ReportDescription = r.Description;
                _ReportAuthor = r.Author;
                r.SubreportDataRetrieval += new EventHandler<SubreportDataRetrievalEventArgs>(r_SubreportDataRetrieval);
                //ParametersBuild(r);
            }
            else
            {
                _PageWidth = 0;
                _PageHeight = 0;
                _ReportDescription = null;
                _ReportAuthor = null;
                _ReportName = null;
            }
            return r;
        }

        void r_SubreportDataRetrieval(object sender, SubreportDataRetrievalEventArgs e)
        {
            if (this.SubreportDataRetrieval != null)
                SubreportDataRetrieval(this, e);
        }

        private string GetReportEmptyMsg()
        {
            string prog = "<Report><Width>8.5in</Width><Body><Height>1in</Height><ReportItems><Textbox><Value></Value><Style><FontWeight>Bold</FontWeight></Style><Height>.3in</Height><Width>5 in</Width></Textbox></ReportItems></Body></Report>";
            return prog;
        }

        private string GetReportErrorMsg()
        {
            string data1 = @"<?xml version='1.0' encoding='UTF-8'?>
<Report> 
	<LeftMargin>.4in</LeftMargin><Width>8.5in</Width>
	<Author></Author>
	<DataSources>
		<DataSource Name='DS1'>
			<ConnectionProperties> 
				<DataProvider>xxx</DataProvider>
				<ConnectString></ConnectString>
			</ConnectionProperties>
		</DataSource>
	</DataSources>
	<DataSets>
		<DataSet Name='Data'>
			<Query>
				<DataSourceName>DS1</DataSourceName>
			</Query>
			<Fields>
				<Field Name='Error'> 
					<DataField>Error</DataField>
					<TypeName>String</TypeName>
				</Field>
			</Fields>";

            string data2 = @"
		</DataSet>
	</DataSets>
	<PageHeader>
		<Height>1 in</Height>
		<ReportItems>
			<Textbox><Top>.1in</Top><Value>fyiReporting Software, LLC</Value><Style><FontSize>18pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
			<Textbox><Top>.1in</Top><Left>4.25in</Left><Value>=Globals!ExecutionTime</Value><Style><Format>dddd, MMMM dd, yyyy hh:mm:ss tt</Format><FontSize>12pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
			<Textbox><Top>.5in</Top><Value>Errors processing report</Value><Style><FontSize>12pt</FontSize><FontWeight>Bold</FontWeight></Style></Textbox>
		</ReportItems>
	</PageHeader>
	<Body><Height>3 in</Height>
		<ReportItems>
			<Table>
				<Style><BorderStyle>Solid</BorderStyle></Style>
				<TableColumns>
					<TableColumn><Width>7 in</Width></TableColumn>
				</TableColumns>
				<Header>
					<TableRows>
						<TableRow>
							<Height>15 pt</Height>
							<TableCells>
								<TableCell>
									<ReportItems><Textbox><Value>Messages</Value><Style><FontWeight>Bold</FontWeight></Style></Textbox></ReportItems>
								</TableCell>
							</TableCells>
						</TableRow>
					</TableRows>
					<RepeatOnNewPage>true</RepeatOnNewPage>
				</Header>
				<Details>
					<TableRows>
						<TableRow>
							<Height>12 pt</Height>
							<TableCells>
								<TableCell>
									<ReportItems><Textbox Name='ErrorMsg'><Value>=Fields!Error.Value</Value><CanGrow>true</CanGrow></Textbox></ReportItems>
								</TableCell>
							</TableCells>
						</TableRow>
					</TableRows>
				</Details>
			</Table>
		</ReportItems>
	</Body>
</Report>";

            StringBuilder sb = new StringBuilder(data1, data1.Length + data2.Length + 1000);
            // Build out the error messages
            sb.Append("<Rows>");
            foreach (string msg in _errorMsgs)
            {
                sb.Append("<Row><Error>");
                string newmsg = msg.Replace("&", @"&amp;");
                newmsg = newmsg.Replace("<", @"&lt;");
                sb.Append(newmsg);
                sb.Append("</Error></Row>");
            }
            sb.Append("</Rows>");
            sb.Append(data2);
            return sb.ToString();
        }

        private Pages GetPages()
        {
            this._Report = GetReport();
            if (_loadFailed)			// retry on failure; this will get error report
                this._Report = GetReport();

            return GetPages(this._Report);
        }

        private Pages GetPages(Report report)
        {
            Pages pgs = null;

            ListDictionary ld = GetParameters();		// split parms into dictionary

            try
            {
                report.RunGetData(ld);

                pgs = report.BuildPages();

                if (report.ErrorMaxSeverity > 0)
                {
                    if (_errorMsgs == null)
                    {
                        _errorMsgs = report.ErrorItems;		// keep a copy of the errors
                    }
                    else
                    {
                        foreach (string err in report.ErrorItems)
                        {
                            _errorMsgs.Add(err);
                        }
                    }

                    report.ErrorReset();
                }

            }
            catch (Exception e)
            {
                string msg = e.Message;
            }

            return pgs;
        }

        private ListDictionary GetParameters()
        {
            ListDictionary ld = new ListDictionary();
            if (_Parameters == null)
            {
                return ld;				// dictionary will be empty in this case
            }

            // parms are separated by &
            char[] breakChars = new char[] { '&' };
            string parm = _Parameters.Replace("&amp;", '\ufffe'.ToString());    // handle &amp; as user wanted '&'
            string[] ps = parm.Split(breakChars);
            foreach (string p in ps)
            {
                int iEq = p.IndexOf("=");
                if (iEq > 0)
                {
                    string name = p.Substring(0, iEq);
                    string val = p.Substring(iEq + 1);
                    ld.Add(name, val.Replace('\ufffe', '&'));
                }
            }
            return ld;
        }

        private string GetRdlSource()
        {
            StreamReader fs = null;
            string prog = null;
            try
            {
                fs = new StreamReader(_SourceFileName.LocalPath);
                prog = fs.ReadToEnd();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }

            return prog;
        }

        /// <summary>
        /// Call LoadPageIfNeeded when a routine requires the report to be loaded in order
        /// to fulfill the request.
        /// </summary>
        private void LoadPageIfNeeded()
        {
            if (_pgs == null)
            {
                _pgs = GetPages();
                _DrawPanel.Pgs = _pgs;
            }
        }
    }
}
