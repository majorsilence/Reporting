using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Draw2 = Majorsilence.Forms.Drawing;
using Majorsilence.Forms.Drawing.Imaging;

using System.Text.RegularExpressions;
using System.Globalization;
using Majorsilence.Reporting.Rdl;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.RdlCmd
{
	/// <summary>
	/// RdlCmd is a batch report generation program.  It takes a report definition
    ///   and renders it into the requested formats.
	/// </summary>
	public class RdlCmd
	{
		/// <summary>
		/// RdlCmd takes a report definition and renders it in the specified formats.
		/// </summary>
		///
		private int returnCode=0;				// return code
		private string? _DataSourcePassword;
        private bool _ShowStats = false;        // show statistics
		private string? _StampInfo;	// PDF stamping information
        private string? _user; // Allow the user to be set via a command line param GJL AJM 12062008

		[STAThread]
		static public async Task<int> Main(string[] args)
		{
			// Handle the arguments
			if (args == null || args.Length==0)
			{
				WriteLicense();
				return 8;
			}

			RdlCmd rc = new RdlCmd();

			char[] breakChars = new char[] {'+'};
			string[]? files=null;
			string[]? types=null;
			string? dir=null;
			int returnCode=0;
            string connectionStringOverwrite = string.Empty;
			foreach(string s in args)
			{
				string t = s.Substring(0,2);
				switch (t)
				{
					case "/l":
					case "-l":
						WriteLicense();
						return 0;
					case "/f":
					case "-f":
						files = s.Substring(2).Split(breakChars);
						break;
					case "/o":
					case "-o":
						dir = s.Substring(2);
						break;
					case "/p":
					case "-p":
						rc._DataSourcePassword = s.Substring(2);
						break;
					case "/t":
					case "-t":
						types = s.Substring(2).Split(breakChars);
						break;
                    case "--help":
                    case "-help":
                    case "-h":
                    case "--h":
					case "/?":
					case "-?":
						WriteHelp();
						return 0;
                    case "/s":
                    case "-s":
                        rc._ShowStats = true;
                        break;
					case "/i":
					case "-i":
						rc._StampInfo = s.Substring(2);
						break;
                    case "/u":
                    case "-u":
                        rc._user = s.Substring(2); // Allow the user to be set via a command line param (u) GJL AJM 12062008
                        break;
                    case "/c":
                    case "-c":
                        connectionStringOverwrite = s.Substring(2);
                        break;
                    default:
						Console.WriteLine("Unknown command '{0}' ignored.", s);
						returnCode = 4;
						break;
				}
			}
			if (files == null)
			{
				Console.WriteLine("/f parameter is required.");
				return 8;
			}

			if (types == null)
			{
				Console.WriteLine("/t parameter is required.");
				return 8;
			}

			if (dir == null)
			{
				dir = Environment.CurrentDirectory;
			}

			if (dir[dir.Length-1] != Path.DirectorySeparatorChar)
				dir += Path.DirectorySeparatorChar;

			rc.returnCode = returnCode;

			RdlEngineConfig.RdlEngineConfigInit();
            await rc.DoRender(dir, files, types, connectionStringOverwrite);

			return rc.returnCode;
		}

		private string GetPassword() => _DataSourcePassword ?? string.Empty;

		// Render the report files with the requested types
		private async Task DoRender(string dir, string[] files, string[] types, string connectionStringOverwrite)
		{
			string? source;
			Report? report;
			int index;
			ListDictionary? ld;
			string file;

            DateTime start = DateTime.Now;
			foreach (string filename in files)
			{
                DateTime startF = DateTime.Now;

				// Any parameters?  e.g.  file1.rdl?ordid=5
				index = filename.LastIndexOf('?');
				if (index >= 0)
				{
					ld = this.GetParameters(filename.Substring(index+1));
					file = filename.Substring(0, index);
				}
				else
				{
					ld = null;
					file = filename;
				}

				// Obtain the source
				source = this.GetSource(file);
                if (this._ShowStats)
                {
                    DateTime temp = DateTime.Now;
                    Console.WriteLine("load file {0}: {1}", file, temp - startF);
                    startF = DateTime.Now;
                }

				if (source == null)
					continue;					// error: process the rest of the files

				// Compile the report
				report = await this.GetReport(source, file, connectionStringOverwrite);
                report!.UserID = _user!; //Set the user of the report based on the parameter passed in GJL AJM 12062008
                if (this._ShowStats)
                {
                    DateTime temp = DateTime.Now;
                    Console.WriteLine("Compile: {0}", temp - startF);
                    startF = DateTime.Now;
                }

                if (report == null)
                    continue;					// error: process the rest of the files

				// Obtain the data
				string? fileNoExt=null;
				if (ld != null)
				{
					fileNoExt = ld["rc:ofile"] as string;
					if (fileNoExt != null)
						ld.Remove("rc:ofile");	// don't pass this as an argument to the report
				}

                await report.RunGetData(ld!);
                if (this._ShowStats)
                {
                    DateTime temp = DateTime.Now;
                    Console.WriteLine("Get Data: {0}", temp - startF);
                    startF = DateTime.Now;
                }

				// Render the report in each of the requested types
				if (fileNoExt != null)
					fileNoExt = dir + fileNoExt;
				else
					fileNoExt = dir + Path.GetFileNameWithoutExtension(file);

				foreach (string stype in types)
				{
                    await SaveAs(report, fileNoExt+"."+stype, stype);
                    if (this._ShowStats)
                    {
                        DateTime temp = DateTime.Now;
                        Console.WriteLine("Render {0}: {1}", stype, temp - startF);
                        startF = DateTime.Now;
                    }
                }
			}	// end foreach files
            if (this._ShowStats)
            {
                DateTime end = DateTime.Now;
                Console.WriteLine("Total time: {0}", end - start);
            }

        }

		private ListDictionary GetParameters(string parms)
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
                    string val = p.Substring(iEq + 1);
                    ld.Add(name, val);
                }
            }
			return ld;
		}

		private string? GetSource(string file)
		{
			StreamReader? fs=null;
			string? prog=null;
			try
			{
				fs = new StreamReader(file);
				prog = fs.ReadToEnd();
			}
			catch(Exception e)
			{
				prog = null;
				Console.WriteLine(e.Message);
				returnCode = 8;
			}
			finally
			{
				if (fs != null)
					fs.Close();
			}
			return prog;
		}

		private async Task<Report?> GetReport(string prog, string file, string connectionStringOverwrite)
		{
			// Now parse the file
			RDLParser rdlp;
			Report? r;
			try
			{
				rdlp =  new RDLParser(prog);
				string folder = Path.GetDirectoryName(file) ?? Environment.CurrentDirectory;
				if (folder == "")
					folder = Environment.CurrentDirectory;
				rdlp.Folder = folder;
				rdlp.DataSourceReferencePassword = new NeedPassword(this.GetPassword);
                if (!string.IsNullOrWhiteSpace(connectionStringOverwrite))
                {
                    rdlp.OverwriteConnectionString = connectionStringOverwrite;
                }


				r = await rdlp.Parse();
				if (r.ErrorMaxSeverity > 0)
				{
					// have errors fill out the msgs
					Console.WriteLine("{0} has the following errors:", file);
					foreach (string emsg in r.ErrorItems)
					{
						Console.WriteLine(emsg);		// output message to console
					}
					int severity = r.ErrorMaxSeverity;
					r.ErrorReset();
					if (severity > 4)
					{
						r = null;			// don't return when severe errors
						returnCode = 8;
					}
				}
				// If we've loaded the report; we should tell it where it got loaded from
				if (r != null)
				{
					r.Folder = folder;
					r.Name = Path.GetFileNameWithoutExtension(file);
					r.GetDataSourceReferencePassword = new Rdl.NeedPassword(GetPassword);
				}
			}
			catch(Exception e)
			{
				r = null;
				Console.WriteLine(e.Message);
				returnCode = 8;
			}
			return r;
		}

		/// <summary>
		/// Save the file.  The extension determines the type of file to save.
		/// </summary>
		/// <param name="FileName">Name of the file to be saved to.</param>
		/// <param name="ext">Type of file to save.  Should be "pdf", "xml", "html", mht.</param>
		private async Task SaveAs(Report report, string FileName, string type)
		{
			string ext = type.ToLower();
			OneFileStreamGen? sg=null;
			try
			{
				bool isOldPdf = false;
				if (System.Environment.OSVersion.Platform == PlatformID.Unix && type=="pdf" ) {

                    if (System.IO.Directory.Exists("/usr/share/fonts/truetype/msttcorefonts")==false)
                    {
						isOldPdf = true;
                    }

                }



                if (ext == "tifb")
                    FileName = FileName.Substring(0, FileName.Length - 1);      // get rid of the 'b'
				sg = new OneFileStreamGen(FileName, true);	// overwrite with this name
				switch(ext)
				{
					case "pdf":
						if (this._StampInfo == null)
						{
							if (isOldPdf)
							{
                                await report.RunRender(sg, OutputPresentationType.PDFOldStyle);
							}
							else
							{
                                await report.RunRender(sg, OutputPresentationType.PDF);
							}

						}
						else
                            await SaveAsPdf(report, sg);
						break;
					case "xml":
                        await report.RunRender(sg, OutputPresentationType.XML);
						break;
					case "mht":
                        await report.RunRender(sg, OutputPresentationType.MHTML);
						break;
					case "html": case "htm":
                        await report.RunRender(sg, OutputPresentationType.HTML);
						break;
                    case "csv":
                        await report.RunRender(sg, OutputPresentationType.CSV);
                        break;
                    case "xlsx_table":
                        await report.RunRender(sg, OutputPresentationType.ExcelTableOnly);
                        break;
                    case "xlsx":
                        await report.RunRender(sg, OutputPresentationType.Excel2007);
                        break;
                    case "rtf":
                        await report.RunRender(sg, OutputPresentationType.RTF);
                        break;
                    case "tif": case "tiff":
                        await report.RunRender(sg, OutputPresentationType.TIF);
                        break;
                    case "tifb":
                        await report.RunRender(sg, OutputPresentationType.TIFBW);
                        break;
					default:
						Console.WriteLine("Unsupported file extension '{0}'.  Must be 'pdf', 'xml', 'mht', 'csv', 'xslx', 'xlsx_table', 'rtf', 'tif', 'tifb' or 'html'", type);
						returnCode = 8;
						break;
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				returnCode = 8;
			}
			finally
			{
				if (sg != null)
				{
					sg.CloseMainStream();
				}
			}

			if (report.ErrorMaxSeverity > 0)
			{
				// have errors fill out the msgs
				Console.WriteLine("{0} has the following runtime errors:", FileName);
				foreach (string emsg in report.ErrorItems)
				{
					Console.WriteLine(emsg);		// output message to console
				}
				report.ErrorReset();
			}

			return;
		}
		private async Task SaveAsPdf(Report report, OneFileStreamGen sg)
		{
			Pages pgs = await report.BuildPages();
			FileStream? strm=null;
			Draw2.Image? im=null;

			// Handle any parameters
			float x = 0;		// x position of image
			float y = 0;		// y position of image
			float h = 0;		// height of image
			float w = 0;		// width position of image
			string? fname=null;
			int index = _StampInfo!.LastIndexOf('?');
			bool bClip=false;	// we force clip if either height or width not specified

			if (index >= 0)
			{
				// Get all the arguments for sizing the image
				ListDictionary ld = this.GetParameters(_StampInfo.Substring(index+1));
				fname = _StampInfo.Substring(0, index);
				string? ws = ld["x"] as string;
				x = Size(ws);
				ws = ld["y"] as string;
				y = Size(ws);
				ws = ld["h"] as string;
				if (ws == null)
				{
					bClip = true;
					ws = "12in";	// just give it a big value
				}
				h = Size(ws);
				ws = ld["w"] as string;
				if (ws == null)
				{
					bClip = true;
					ws = "12in";	// just give it a big value
				}
				w = Size(ws);
			}
			else
			{
				fname = _StampInfo;
				// force size
				bClip = true;
				h = Size("12in");
				w = Size("12in");
			}

			// Stamp the first page
			foreach (Page p in pgs)		// we loop then break after obtaining one
			{
				try
				{
					strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);
					im = Draw2.Image.FromStream(strm);
					int height = im.Height;
					int width = im.Width;
					MemoryStream ostrm = new MemoryStream();

                    /* Replaced with high quality JPEG encoder
                      * 06122007AJM */
 					ImageFormat imf = ImageFormat.Jpeg;
 					//im.Save(ostrm, imf);
                    Draw2.Imaging.ImageCodecInfo[] info;
                    info = ImageCodecInfo.GetImageEncoders();
                    EncoderParameters encoderParameters;
                    encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                    Draw2.Imaging.ImageCodecInfo? codec = null;
                    for (int i = 0; i < info.Length; i++)
                    {
                        if (info[i].FormatDescription == "JPEG")
                        {
                            codec = info[i];
                            break;
                        }
                    }
                    im.Save(ostrm, codec!, encoderParameters);
                    // end change
                    byte[] ba = ostrm.ToArray();
					ostrm.Close();
					PageImage pi = new PageImage(imf, ba, width, height);
					pi.SI = new StyleInfo();			// defaults are ok; don't want border, etc
					// Set location, height and width
					pi.X = x;
					pi.Y = y;
					pi.H = h;
					pi.W = w;
					pi.Sizing = bClip? ImageSizingEnum.Clip: ImageSizingEnum.FitProportional;

					p.InsertObject(pi);
				}
				catch (Exception e)
				{
					// image failed to load, continue processing
					Console.WriteLine("Stamping image failed.  {0}", e.Message);
				}
				finally
				{
					if (strm != null)
						strm.Close();
					if (im != null)
						im.Dispose();
				}
				break;			// only stamp the first page
			}

			// Now create the PDF
			await report.RunRenderPdf(sg, pgs);
		}

		private float Size(string? t)
		{
			if (t == null)
				return 0;
			// Size is specified in CSS Length Units
			// format is <decimal number nnn.nnn><optional space><unit>
			// in -> inches (1 inch = 2.54 cm)
			// cm -> centimeters (.01 meters)
			// mm -> millimeters (.001 meters)
			// pt -> points (1 point = 1/72.27 inches)
			// pc -> Picas (1 pica = 12 points)
			int size=0;
			t = t.Trim();
			int space = t.LastIndexOf(' ');
			string n;						// number string
			string u;						// unit string
			decimal d;						// initial number
			try		// Convert.ToDecimal can be very picky
			{
				if (space != -1)	// any spaces
				{
					n = t.Substring(0,space).Trim();	// number string
					u = t.Substring(space).Trim();	// unit string
				}
				else if (t.Length >= 3)
				{
					n = t.Substring(0, t.Length-2).Trim();
					u = t.Substring(t.Length-2).Trim();
				}
				else
				{
					// Illegal unit
					Console.WriteLine(string.Format("Illegal size '{0}' specified, assuming 0 length.", t));
					return 0;
				}
				if (!Regex.IsMatch(n, @"\A[ ]*[-]?[0-9]*[.]?[0-9]*[ ]*\Z"))
				{
					Console.WriteLine(string.Format("Unknown characters in '{0}' specified.  Number must be of form '###.##'.  Local conversion will be attempted.", t));
					d = Convert.ToDecimal(n, NumberFormatInfo.InvariantInfo);		// initial number
				}
				else
					d = Convert.ToDecimal(n, NumberFormatInfo.InvariantInfo);		// initial number
			}
			catch (Exception ex)
			{
				// Illegal unit
				Console.WriteLine("Illegal size '" + t + "' specified, assuming 0 length.\r\n"+ex.Message);
				return 0;
			}

			switch(u)			// convert to millimeters
			{
				case "in":
					size = (int) (d * 2540m);
					break;
				case "cm":
					size = (int) (d * 1000m);
					break;
				case "mm":
					size = (int) (d * 100m);
					break;
				case "pt":
					size = (int) (d * (2540m / 72.27m));
					break;
				case "pc":
					size = (int) (d * (2540m / 72.27m * 12m));
					break;
				default:
					// Illegal unit
					Console.WriteLine("Unknown sizing unit '" + u + "' specified, assuming inches.");
					size = (int) (d * 2540m);
					break;
			}

			return (float) ((double) size / 2540.0 * 72.27f);
		}

		static private void WriteHelp()
		{
			Console.WriteLine("");
			Console.WriteLine("Runs a RDL report file and creates a file for each type specified.");
			Console.WriteLine("RdlCmd /ffile.rdl /tpdf [/ooutputdir]");
			Console.WriteLine("/f is followed by a file or file list.  e.g. /ffile1.rdl+file2.rdl");
			Console.WriteLine("  Report arguments can optionally be passed by using '?' after the file.");
			Console.WriteLine("  Multiple report arguments are separated by '&'.");
			Console.WriteLine("  For example, /ffile1.rdl?parm1=XYZ Inc.&parm2=1000");
			Console.WriteLine("  One special argument is 'rc:ofile' which names the output file.");
			Console.WriteLine("  For example, /ffile1.rdl?parm1=XYZ Inc.&parm2=1000&rc:ofile=xyzfile");
			Console.WriteLine("/t is followed by the type of output file: pdf, html, mht, xml, csv,");
            Console.WriteLine("  xslx, xlsx_table, rtf, tif, tifb   e.g /tpdf+xml");
			Console.WriteLine("/o is followed by the output directory.   The file name is the same as the");
			Console.WriteLine("  input (or the rc:ofile parameter) except with the type as the extension.");
            Console.WriteLine("/c is followed by the connection string");
            Console.WriteLine("  This will overwrite the connection string in the report definition.");
            Console.WriteLine("/p is followed by the pass phrase needed by reports using a shared data source.");
			Console.WriteLine("/s displays elapsed time statistics");
			Console.WriteLine("/i is followed by an image filename to be stamped onto first page of the PDF.");
			Console.WriteLine("  Location arguments x, y, h, w can optionally be passed using '?'");
			Console.WriteLine("  Arguments are separated by '&'.  Only pdf files support this option.");
			Console.WriteLine("  For example, /i\"copyright.gif?x=4in&y=3in&w=7cm&h=16pt\"");
            Console.WriteLine("/u is followed by the report user ID.");
            Console.WriteLine("/l outputs the license and warranty");
            Console.WriteLine("/? outputs this text");
		}

		static private void WriteLicense()
		{
			Console.WriteLine(string.Format("RdlCmd Version {0}, Copyright (C) 2004-2008 fyiReporting Software, LLC",
							Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"));
			Console.WriteLine("");
			Console.WriteLine("RdlCmd comes with ABSOLUTELY NO WARRANTY.  This is free software,");
			Console.WriteLine("and you are welcome to redistribute it under certain conditions.");
			Console.WriteLine("");
			Console.WriteLine("For help, type RdlCmd /?");

			return;
		}
	}
}
