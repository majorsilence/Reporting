

using System;	
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;


namespace Majorsilence.Reporting.RdlDesktop
{
	class ConnectionThread
	{
		private string _WorkingDir;	// subdirectory name under server root to place generated reports
		string serverRoot;			// directory we start with
		public TcpListener myListener;
		private RdlDesktop _server;
		
		// Some statics used for statistics
		private static int connections = 0;
		private static int peakConnections = 0;
		private static int requests = 0;
		private static double totalts = 0;

		public ConnectionThread(RdlDesktop rs, TcpListener l, string sr, string wd)
		{
			_server = rs;
			serverRoot = sr;
			_WorkingDir = wd;
			myListener = l;
		}

		private void AccumTimeSpan(TimeSpan ts)
		{
			totalts += ts.TotalSeconds;
			return;
		}

		static public void ClearStatistics()
		{
			// Clear out the statistics;  not connections, though!!!
			peakConnections=0;
			requests=0;
			totalts=0;
			return;
		}

		static public double GetTotalRequestTime()
		{
			return totalts;
		}

		static public int GetConnectionCount()
		{
			return connections;
		}

		static public int GetPeakConnectionCount()
		{
			return peakConnections;
		}

		static public int GetRequestCount()
		{
			return requests;
		}

		public string WorkingDir
		{
			get { return _WorkingDir; }
		}

		// Build an HTML page for directories
		public async Task<string> ProcessDirectory(string directory)
		{
			StringWriter sw = new StringWriter();
			DirectoryInfo di;
			FileSystemInfo[] afsi;

			// Check to see of there is an index.html or index.rdl file 
			StreamReader sr=null;
			string result=null;
			try
			{
				string filename = serverRoot + directory + "index.html";
				FileInfo fi = new FileInfo(filename);

				if (fi.Exists)
				{
					sr = new StreamReader(filename);
					result = sr.ReadToEnd();
					sr.Close();
					sr = null;
				}
				else
				{
					filename = serverRoot + directory + "index.rdl";
					fi = new FileInfo(filename); 
					if (fi.Exists)
					{
			
						(byte[] ba, string mimeType) = await ProcessReportFile(directory + "index.rdl", filename, null, fi.LastWriteTime);
						result = Encoding.ASCII.GetString(ba);
					}
				}
			}
			catch
			{	// clean up a little
				if (sr != null)
					sr.Close();
			}	
			if (result != null)
				return result;

			try 
			{
				di = new DirectoryInfo(serverRoot + directory);
				afsi = di.GetFileSystemInfos();
			}
			catch
			{
				sw.WriteLine("<html><Body>Illegal File or directory.</body></html>");
				return sw.ToString();
			} 

			sw.WriteLine("<html>" +
						"<style type='text/css'>" +
						"a:link img {border-style:none;}" +
						"</style>" +
						"<title>Directory of " + directory + 
						"</title><body><h3>Reports in " + directory + "</h3><table>");
			foreach (FileSystemInfo fsi in afsi)
			{
				if (fsi.Name == WorkingDir)		// never show the working directory
					continue;
				if ((fsi.Attributes & FileAttributes.Directory) == 0 &&
					 fsi.Extension.ToLower() != ".rdl")	// only show report files
					continue;
				string name = directory.Replace(" ", "%20");
				if (directory[directory.Length-1] != '/')
					name += "/";
				name += fsi.Name.Replace(" ", "%20");

				if ((fsi.Attributes & FileAttributes.Directory) == 0)	// handle files
				{
					sw.WriteLine("<tr><td>{0}</td>"+
							"<td><a href=\"{1}?rs:Format=HTML\">HTML</a></td>"+
							"<td><a href=\"{1}?rs:Format=PDF\">PDF</a></td>"+
							"<td><a href=\"{1}?rs:Format=XML\">XML</a></td></tr>",
						Path.GetFileNameWithoutExtension(fsi.Name), name);
				}
				else		// handle directories
					sw.WriteLine("<tr><td><a href=\"{1}\">{0}<a></td><td></td><td></td></tr>",
						fsi.Name, name);
			}
			sw.WriteLine("</table></body></html>");

			return sw.ToString();
		}

		// Handle the processing of a report
		private async Task<(byte[] Report, string MimeType)> ProcessReportFile(string url, string file, string parms, DateTime lastUpdateTime)
		{
			byte[] result=null;
			string source;

			string mimeType = "text/html";			// set the default

			// try finding report in the server cache!
			IList il = _server.Cache.Find(url + (parms==null?"":parms), lastUpdateTime);
			if (il != null)
			{
				try
				{
					string cfile = (string) il[il.Count-1];
					result = _server.ReadCache.Read(cfile);
					mimeType = GetMimeType(cfile);		// set mimetype based on file's extension
				}
				catch (Exception ex)
				{
					if (_server.TraceLevel >= 0) 
						Console.WriteLine("Cache read exception in ProcessReportFile: {0} {1}", il[0], ex.Message);
				}
				return (result,mimeType);
			}

			// Obtain the source
			if (!ProcessReportGetSource(file, out source))
			{
				return (Encoding.ASCII.GetBytes(source), mimeType);		// we got an error opening file; source contains html error text
			}

			// Compile the report
			(Report report, string msgs) = await ProcessReportCompile(file, source);
			if (report == null)
			{
				mimeType = "text/html";			// force to html
				return (Encoding.ASCII.GetBytes(String.Format("<H2>Report '{0}' has the following syntax errors.</H2><p>{1}", url, msgs)), mimeType);
			}

			// Obtain the result HTML from running the report
			string dbgFilename="";
			try 
			{
				ListDictionary ld = ProcessReportGetParms(parms);	// split parms into dictionary

				OutputPresentationType type;
				string stype = (string) ld["rs:Format"];
				if (stype == null)
					stype = "html";
				else
					stype = stype.ToLower();
				switch (stype)
				{
					case "pdf":
						type = OutputPresentationType.PDF;
						break;
					case "xml":
						type = OutputPresentationType.XML;
						string ext = (string) ld["rs:FileExtension"];
						if (ext != null)
							stype = ext;
						break;
					case "html":
						type = OutputPresentationType.HTML;
						break;
					default:
						type = OutputPresentationType.HTML;
						stype = "html";
						break;
				}

				StreamGen sg = new StreamGen(serverRoot, WorkingDir, stype);
				
				ProcessReport pr = new ProcessReport(report, sg);

                await pr.Run(ld, type);

				// handle any error messages
				if (report.ErrorMaxSeverity > 0)
				{
					string errs=null;
					if (report.ErrorMaxSeverity > 4)
					{
						mimeType = "text/html";			// force to html
						errs = "<H2>Severe errors encountered when running report.</H2>";
					}
					foreach (String emsg in report.ErrorItems)
					{
						if (report.ErrorMaxSeverity > 4)
							errs += ("<p>" + emsg + "</p>");
						else
						{
							if (_server.TraceLevel > 0) 
								Console.WriteLine(emsg);		// output message to console
						}
					}
					if (errs != null)
						result = Encoding.ASCII.GetBytes(errs);
					report.ErrorReset();
				}

				// Only read the result if significant errors didn't occur
				if (result == null)
				{						
					ReportRender rr = new ReportRender(report);
					rr.ActionUrl = "/" + url;
					
					string p = await rr.ParameterHtmlAsync(ld);
					// put this into a file.
					string parmUrl;
					StreamWriter sw=null;
					try 
					{
						sw = new StreamWriter(sg.GetIOStream(out parmUrl, "html"));
						sw.Write(p);
					}
					finally
					{
						if (sw != null)
							sw.Close();
					}

					// Add it to the file list
					IList newlist = _server.Cache.Add(url + (parms==null?"":parms), sg.FileList);
					dbgFilename = (string) newlist[0];		// this is the first fully qualified name

					FileInfo fi = new FileInfo(dbgFilename);
					string mHtml = rr.MainHtml(report, parmUrl, sg.RelativeDirectory+fi.Name);
					string mUrl;
					sw=null;
					try 
					{
						sw = new StreamWriter(sg.GetIOStream(out mUrl, "html"));
						sw.Write(mHtml);
					}
					finally
					{
						if (sw != null)
							sw.Close();
					}

					result = Encoding.ASCII.GetBytes(mHtml);
					
				}
			}
			catch (Exception ex)
			{
				if (_server.TraceLevel >= 0) 
					Console.WriteLine("Exception in ProcessReportFile: {0} {1}", file, ex.Message);
				result = Encoding.ASCII.GetBytes(string.Format("<H2>{0}</H2><p>{1}</p>", ex.Message, ex.StackTrace));
				mimeType = "text/html";			// force to html
			}
			
			return (result, mimeType);
		}

		private ListDictionary ProcessReportGetParms(string parms)
		{
			ListDictionary ld= new ListDictionary();
			if (parms == null)
				return ld;				// dictionary will be empty in this case

			// parms are separated by &
			char[] breakChars = new char[] {'&'};
			string[] ps = parms.Split(breakChars);
			foreach (string p in ps)
			{
				int iEq = p.IndexOf("=");
				if (iEq > 0)
				{
					string name = p.Substring(0, iEq);
					string val = p.Substring(iEq+1);
					ld.Add(WebUtility.UrlDecode(name), WebUtility.UrlDecode(val));	
				}
			}
			return ld;
		}

		private bool ProcessReportGetSource(string file, out string source)
		{
			StreamReader fs=null;
			string prog=null;
			try
			{
				fs = new StreamReader(file);
				prog = fs.ReadToEnd();
			}
			catch (Exception ae)
			{
				source = string.Format("<H2>{0}</H2>", ae.Message);
				return false;
			}
			finally
			{
				if (fs != null)
					fs.Close();
			}
			source = prog;
			return true;
		}

		private async Task<(Report Report, string Msgs)> ProcessReportCompile(string file, string prog)
		{
			// Now parse the file
			RDLParser rdlp;
			Report r;
			string msgs = "";
			try
			{
				rdlp =  new RDLParser(prog);
				rdlp.Folder = Path.GetDirectoryName(file);
				rdlp.DataSourceReferencePassword = new NeedPassword(this.GetPassword);

				r = await rdlp.Parse();
				if (r.ErrorMaxSeverity > 0) 
				{
					// have errors fill out the msgs 
					foreach (String emsg in r.ErrorItems)
					{
						if (_server.TraceLevel > 0)
							Console.WriteLine(emsg);		// output message to console
						msgs += (emsg + "<p>");
					}
					int severity = r.ErrorMaxSeverity;
					r.ErrorReset();
					if (severity > 4)
						r = null;			// don't return when severe errors
				}
				// If we've loaded the report; we should tell it where it got loaded from
				if (r != null)
				{
					r.Folder = Path.GetDirectoryName(file);
					r.Name = Path.GetFileNameWithoutExtension(file);
					r.GetDataSourceReferencePassword = new Rdl.NeedPassword(GetPassword);
				}
			}
			catch (Exception ex)
			{
				msgs = string.Format("<H2>{0}</H2>", ex.Message);
				r = null;
			}


			return (r, msgs);
		}
  
		private string GetPassword()
		{
			return _server.DataSourceReferencePassword;
		}

		/// <summary>
		/// This function takes a File Name as Input and returns the mime type.
		/// </summary>
		/// <param name="sRequestedFile">To indentify the Mime Type</param>
		/// <returns>Mime Type</returns>
		private string GetMimeType(string file)
		{
			String mimeType;
			String fileExt;
			
			int startPos = file.IndexOf(".") + 1;

			fileExt = file.Substring(startPos).ToLower();
			mimeType = (string) ( this._server.Mimes[fileExt]);

			return mimeType; 
		}

		/// <summary>
		/// This function send the Header Information to the client (Browser)
		/// </summary>
		/// <param name="sHttpVersion">HTTP Version</param>
		/// <param name="sMIMEHeader">Mime Type</param>
		/// <param name="iTotBytes">Total Bytes to be sent in the body</param>
		/// <param name="mySocket">Socket reference</param>
		/// <returns></returns>
		public void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, string modifiedTime, ref Socket mySocket)
		{

			StringBuilder buffer = new StringBuilder();
			
			// if Mime type is not provided set default to text/html
			if (sMIMEHeader == null || sMIMEHeader.Length == 0 )
			{
				sMIMEHeader = "text/html";  // Default Mime Type is text/html
			}

			buffer.Append(sHttpVersion);
			buffer.Append(sStatusCode);
			buffer.Append("\r\n");
			buffer.Append("Server: RdlDesktop\r\n");
			buffer.Append("Cache-Control: must-revalidate\r\n");
			buffer.AppendFormat("Content-Type: {0}\r\n", sMIMEHeader);
			if (modifiedTime != null)
				buffer.AppendFormat("Last-Modifed: {0}\r\n", modifiedTime);
			buffer.Append("Accept-Ranges: bytes\r\n");
			buffer.AppendFormat("Content-Length: {0}\r\n\r\n", iTotBytes);
			
			Byte[] data = Encoding.ASCII.GetBytes(buffer.ToString()); 

			SendToBrowser( data, ref mySocket);
		}

		/// <summary>
		/// Overloaded Function, takes string, convert to bytes and calls 
		/// overloaded sendToBrowserFunction.
		/// </summary>
		/// <param name="sData">The data to be sent to the browser(client)</param>
		/// <param name="mySocket">Socket reference</param>
		public void SendToBrowser(String sData, ref Socket mySocket)
		{
			SendToBrowser (Encoding.ASCII.GetBytes(sData), ref mySocket);
		}

		/// <summary>
		/// Sends data to the browser (client)
		/// </summary>
		/// <param name="bSendData">Byte Array</param>
		/// <param name="mySocket">Socket reference</param>
		public void SendToBrowser(Byte[] bSendData, ref Socket mySocket)
		{
			int numBytes = 0;
			
			try
			{
				if (mySocket.Connected)
				{
					if (( numBytes = mySocket.Send(bSendData, bSendData.Length,0)) == -1)
					{
						if (_server.TraceLevel >= 0) 
							Console.WriteLine("Socket Error cannot Send Packet");
					}
				}
				else
				{
					if (_server.TraceLevel >= 4) 
						Console.WriteLine("Connection Dropped....");
				}
			}
			catch (Exception  e)
			{
				if (_server.TraceLevel >= 0) 
					Console.WriteLine("Error Occurred : {0} ", e );
			}
		}

		//This method Accepts new connection 
		public async Task HandleConnection()
		{
			DateTime sDateTime;			// start date time
			int iStartPos = 0;
			string sRequest;
			string sDirName;
			string sRequestedFile;
			string sErrorMessage;
			string sPhysicalFilePath = "";
			string sParameters=null;
			
			//Accept a new connection
			Socket mySocket = myListener.AcceptSocket() ;

			if(!mySocket.Connected)
				return;

			sDateTime = DateTime.Now;
			connections++;			// current number of connections
			requests++;				// total requests made
			if (connections > peakConnections)
				peakConnections = connections;	// peak connections

			if (_server.TraceLevel >= 4) 
			{
				Console.WriteLine("\nClient connected IP={0} -- {1} active connection{2}", 
					mySocket.RemoteEndPoint, connections, connections > 1? "s": "") ;
			}

			//make a byte array and receive data from the client 
			Byte[] data = new Byte[1024] ;
			try
			{
				int i = mySocket.Receive(data,data.Length,0) ;
			}
			catch (Exception e)
			{
				connections--;
				mySocket.Close();
				if (_server.TraceLevel >= 0) 
					Console.WriteLine("Error Occurred : {0} ", e );
				this.AccumTimeSpan(DateTime.Now - sDateTime);
				return;
			}
			
			//Convert Byte to String
			string sBuffer = Encoding.ASCII.GetString(data);

			//At present we will only deal with GET type
			if (sBuffer.Substring(0,3) != "GET" )
			{
				string req = sBuffer.Substring(0,30);
				if (_server.TraceLevel >= 0) 
					Console.WriteLine("{0} not supported.  Only Get Method is supported.", req);
				mySocket.Close();
				connections--;
				this.AccumTimeSpan(DateTime.Now - sDateTime);
				return;
			}
		
			// Look for HTTP request
			iStartPos = sBuffer.IndexOf("HTTP",1);

			// Get the HTTP text and version e.g. it will return "HTTP/1.1"
			string sHttpVersion = sBuffer.Substring(iStartPos,8);
					        			
			// Extract the Requested Type and Requested file/directory
			sRequest = sBuffer.Substring(0,iStartPos - 1);

			if (_server.TraceLevel >= 4) 
				Console.WriteLine("Request=" + UnescapeRequest(sRequest));

			int iStartParm = sRequest.IndexOf("?");
			if (iStartParm >= 0)
			{
				sParameters = sRequest.Substring(iStartParm+1);
				sRequest = sRequest.Substring(0, iStartParm);
			}

			sRequest = UnescapeRequest(sRequest);	// can't do this to parameters until they've been separated

			//If file name is not supplied add forward slash to indicate 
			//that it is a directory and then we will look for the 
			//default file name..
			if ((sRequest.IndexOf(".") <1) && (!sRequest.EndsWith("/")))
			{
				sRequest = sRequest + "/"; 
			}

			//Extract the requested file name
			iStartPos = sRequest.LastIndexOf("/") + 1;
			sRequestedFile = sRequest.Substring(iStartPos);
			
			//Extract The directory Name
			sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/")-3);
			
			if ( sDirName == "")
				sDirName = "/";

			// When directory output the index
			if (sRequestedFile.Length == 0 )
			{
				string directoryHTML;
				directoryHTML = await ProcessDirectory(sDirName);
				SendHeader(sHttpVersion, "text/html", directoryHTML.Length, " 200 OK", null, ref mySocket);

				SendToBrowser(directoryHTML, ref mySocket);
				mySocket.Close();
				connections--;
				this.AccumTimeSpan(DateTime.Now - sDateTime);
				return;
			}

			// Handle files
			String mimeType = GetMimeType(sRequestedFile);	// get mime type

			//Build the physical path
			sPhysicalFilePath = serverRoot + sDirName + sRequestedFile;
			bool bFail= mimeType == null? true: false;	// mime type can be null if extension isn't in config.xml
			FileInfo fi=null;
			if (!bFail)
			{
				try 
				{
					fi = new FileInfo(sPhysicalFilePath);
					if (fi == null || fi.Exists == false)
						bFail = true;
				}
				catch
				{
					bFail = true;
				}
			}
			if (bFail)
			{
				sErrorMessage = "<H2>404 File Not Found</H2>";
				SendHeader(sHttpVersion, "text/html", sErrorMessage.Length, " 404 Not Found", null, ref mySocket);
				SendToBrowser( sErrorMessage, ref mySocket);

				mySocket.Close();						
				connections--;
				this.AccumTimeSpan(DateTime.Now - sDateTime);
				return;
			}

			string fileTime = string.Format("{0:r}", fi.LastWriteTime);
			// Handle the primary file type of this server
			if (mimeType == "application/rdl")
			{
				// Get the url of the request
				int uStart = sRequest.IndexOf("/");	// "Get /" precedes the url name
				string url;
				if (uStart >= 0)
					url = sRequest.Substring(uStart+1);
				else
					url = sRequest;
				string mtype;
				(byte[] ba, mtype) = await ProcessReportFile(url, sPhysicalFilePath, sParameters, fi.LastWriteTime);
				SendHeader(sHttpVersion, mtype, ba.Length, " 200 OK", fileTime, ref mySocket);
				SendToBrowser(ba, ref mySocket);
				mySocket.Close();
				connections--;
				this.AccumTimeSpan(DateTime.Now - sDateTime);
				return;
			}

			// Handle any other kind of file
			try
			{
				byte[] bytes = _server.ReadCache.Read(sPhysicalFilePath);
				SendHeader(sHttpVersion,  mimeType, bytes.Length, " 200 OK", fileTime, ref mySocket);
				SendToBrowser(bytes, ref mySocket);
			}
			catch (Exception ex)
			{	
				sErrorMessage = string.Format("<H2>{0}</H2><p>{1}</p>", ex.Message, ex.StackTrace);
				SendHeader(sHttpVersion, "text/html", sErrorMessage.Length, " 200 OK", null, ref mySocket);
				SendToBrowser( sErrorMessage, ref mySocket);
			}

			mySocket.Close();						
			connections--;
			this.AccumTimeSpan(DateTime.Now - sDateTime);
			return;
		}
		
		string UnescapeRequest(string req)
		{
			req = req.Replace("\\","/"); //Replace backslash with Forward Slash, if Any
			return WebUtility.UrlDecode(req);
		}
	}
}
