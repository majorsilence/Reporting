// This file was contributed to the RDL Project under the MIT License.  It was
// modified as part of merging into the RDL Project.

/*
The MIT License
Copyright (c) 2006 Christian Cunlif and Lionel Cuir of Aulofee

Permission is hereby granted, free of charge, to any person obtaining a copy of 
this software and associated documentation files (the "Software"), to deal in 
the Software without restriction, including without limitation the rights to use, 
copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, subject 
to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlEngine.Resources;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// This class builds the following from a URL:
	///
	///   .mht file (Web Archive, single file)
	///   .htm file with dereferenced (absolute) references (Web Page, HTML Only)
	///   .htm file plus all referenced files in a local subfolder (Web Page, complete) 
	///   .txt file (non-HTML contents of Web Page)
	///
	/// The .mht format is based on RFC2557 
	///    "compliant Multipart MIME Message (mhtml web archive)"
	///    http://www.ietf.org/rfc/rfc2557.txt
	/// </summary>
	/// <remarks>
	///   Jeff Atwood
	///   http://www.codinghorror.com/
	/// </remarks>
	public class MhtBuilder
	{
		#region Fields and enum
		StringBuilder _MhtBuilder;
		bool _StripScriptFromHtml = false;
		bool _StripIframeFromHtml = false;
		bool _AllowRecursion = true;
		bool _AddWebMark = true;
		Encoding _ForcedEncoding = null;
		
		MhtWebFile _HtmlFile;
		
		internal MhtWebClientLocal WebClient = new MhtWebClientLocal();
		internal SortedList WebFiles = new SortedList();

		const string _MimeFinalBoundaryTag = "----=_NextPart_000_00";
		
		// note that chunk size is equal to maximum line width (expanded = 75 chars)
		const int _ChunkSize = 57;

		public enum FileStorage
		{
			Memory,
			DiskPermanent,
			DiskTemporary
		}
		#endregion Fields and enum

		#region Constructor
		public MhtBuilder()
		{
			_HtmlFile = new MhtWebFile(this);
		}
		#endregion Constructor

		#region Properties
		/// <summary>
		/// Add the "Mark of the web" to retrieved HTML content so it can run 
		/// locally on Windows XP SP2
		/// </summary>
		/// <remarks>
		///   http://www.microsoft.com/technet/prodtechnol/winxppro/maintain/sp2brows.mspx#XSLTsection133121120120
		/// </remarks>
		public bool AddWebMark
		{
			get
			{
				return this._AddWebMark;
			}
			set
			{
				this._AddWebMark = value;
			}
		}

		/// <summary>
		/// allow recursive retrieval of any embedded HTML (typically IFRAME or FRAME)
		/// turn off to prevent infinite recursion in the case of pages that reference themselves..
		/// </summary>
		public bool AllowRecursiveFileRetrieval
		{
			get
			{
				return _AllowRecursion;
			}
			set
			{
				_AllowRecursion = value;
			}
		}

		/// <summary>
		/// returns the Mime content-type string designation of a mht file
		/// </summary>
		public string MhtContentType
		{
			get
			{
				return "message/rfc822";
			}
		}

		/// <summary>
		/// Strip all &lt;IFRAME&gt; blocks from any retrieved HTML
		/// </summary>
		public bool StripIframes
		{
			get
			{
				return this._StripIframeFromHtml;
			}
			set
			{
				this._StripIframeFromHtml = value;
			}
		}

		/// <summary>
		/// Strip all &lt;SCRIPT&gt; blocks from any retrieved HTML
		/// </summary>
		public bool StripScripts
		{
			get
			{
				return this._StripScriptFromHtml;
			}
			set
			{
				this._StripScriptFromHtml = value;
			}
		}

		/// <summary>
		/// *only* set this if you want to FORCE a specific text encoding for all the HTML pages you're downloading;
		/// otherwise the text encoding is autodetected, which is generally what you want
		/// </summary>
		public Encoding TextEncoding
		{
			get
			{
				return this._ForcedEncoding;
			}
			set
			{
				this._ForcedEncoding = value;
			}
		}

		/// <summary>
		/// Specifies the target Url we want to save
		/// </summary>
		public string Url
		{
			get
			{
				return _HtmlFile.Url;
			}
			set
			{
				WebFiles.Clear();
				_HtmlFile.Url = value;
			}
		}

		#endregion Properties

		#region Private methods
		/// <summary>
		/// Appends a downloaded external binary file to our MhtBuilder using Base64 encoding
		/// </summary>
		void AppendMhtBinaryFile(MhtWebFile ef)
		{
			AppendMhtBoundary();
			AppendMhtLine("Content-Type: " + ef.ContentType);
			AppendMhtLine("Content-Transfer-Encoding: base64");
			AppendMhtLine("Content-Location: " + ef.Url);
			AppendMhtLine();
            
			// note that chunk size is equal to maximum line width (expanded = 75 chars)
			int len = ef.DownloadedBytes.Length;
			if (len <= _ChunkSize)
				AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, 0, len));
			else
			{
				int i = 0;
				while ((i + _ChunkSize) < len)
				{
					AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, i, _ChunkSize));
					i += _ChunkSize;
				}
				if (i != len)
					AppendMhtLine(Convert.ToBase64String(ef.DownloadedBytes, i, len - i));
			}
		}

		/// <summary>
		/// appends a boundary marker to our MhtBuilder
		/// </summary>
		void AppendMhtBoundary()
		{
			AppendMhtLine();
			AppendMhtLine("--"+ _MimeFinalBoundaryTag);
		}

		/// <summary>
		/// appends a boundary marker to our MhtBuilder
		/// </summary>
		void AppendMhtFinalBoundary()
		{
			AppendMhtLine();
			AppendMhtLine("--" + _MimeFinalBoundaryTag + "--");
		}

		/// <summary>
		/// Appends a downloaded external file to our MhtBuilder
		/// </summary>
		void AppendMhtFile(MhtWebFile ef)
		{
			if (ef.WasDownloaded && !ef.WasAppended)
			{
				if (ef.IsBinary)
					AppendMhtBinaryFile(ef);
				else
					AppendMhtTextFile(ef);
			}
			ef.WasAppended = true;
		}

		/// <summary>
		/// appends all downloaded files (from _ExternalFiles) to our MhtBuilder
		/// </summary>
		/// <param name="st">type of storage to use when downloading external files</param>
		/// <param name="storagePath">path to use for downloaded external files</param>
		void AppendMhtFiles()
		{
			foreach (MhtWebFile ef in WebFiles.Values)
				AppendMhtFile(ef);
			AppendMhtFinalBoundary();
		}

		/// <summary>
		/// appends the Mht header, which includes the root HTML
		/// </summary>
		void AppendMhtHeader(MhtWebFile ef)
		{
			// clear the stringbuilder contents
			_MhtBuilder = new StringBuilder();

            //AppendMhtLine("From: <Saved by " + Environment.UserName + " on " + Environment.MachineName + ">");
            //AppendMhtLine("Subject: " + ef.HtmlTitle);
            // For the title, reduces its size if too long and removes line breaks if any. 
            string title = ef.HtmlTitle;
            if (title != null)
            {
                if (title.Length > 260)
                    title = title.Substring(0, 260);
                if (title.IndexOf('\n') != -1)
                    title = title.Replace('\n', ' ');
                if (title.IndexOf(Environment.NewLine) != -1)
                    title = title.Replace(Environment.NewLine, " ");
            }

            AppendMhtLine("From: <Saved by " + Environment.UserName + " on " + Environment.MachineName + ">");
            AppendMhtLine("Subject: " + title); 

			AppendMhtLine("Date: " + DateTime.Now.ToString("ddd, dd MMM yyyy HH:mm:ss zzz"));
			AppendMhtLine("MIME-Version: 1.0");
			AppendMhtLine("Content-Type: multipart/related;");
			AppendMhtLine(Convert.ToChar(9).ToString() + "type=\"text/html\";");
			AppendMhtLine(Convert.ToChar(9).ToString() + "boundary=\"----=_NextPart_000_00\"");
			AppendMhtLine("X-MimeOLE: Produced by " + this.GetType().ToString() + " " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
			AppendMhtLine("");
			AppendMhtLine("This is a multi-part message in MIME format.");
   
			AppendMhtFile(ef);
		}

		/// <summary>
		/// append a single line, with trailing CRLF, to our MhtBuilder
		/// </summary>
		void AppendMhtLine()
		{
			_MhtBuilder.Append(Environment.NewLine);
		}

		/// <summary>
		/// append a single line, with trailing CRLF, to our MhtBuilder
		/// </summary>
		void AppendMhtLine(string s)
		{
			if (s != null)
				_MhtBuilder.Append(s);
			_MhtBuilder.Append(Environment.NewLine);
		}

		/// <summary>
		/// Appends a downloaded external text file to our MhtBuilder using Quoted-Printable encoding
		/// </summary>
		void AppendMhtTextFile(MhtWebFile ef)
		{
			AppendMhtBoundary();
			AppendMhtLine("Content-Type: " + ef.ContentType + ";");
			AppendMhtLine(Convert.ToChar(9).ToString() + @"charset="" + ef.TextEncoding.WebName + @""");
			AppendMhtLine("Content-Transfer-Encoding: quoted-printable");
			AppendMhtLine("Content-Location: " + ef.Url);
			AppendMhtLine();
			AppendMhtLine(QuotedPrintableEncode(ef.ToString(), ef.TextEncoding));
		}

		/// <summary>
		/// returns the root HTML we'll use to generate everything else;
		/// this is tracked in the _HtmlFile object, which is always FileStorage.Memory
		/// </summary>
		async Task DownloadHtmlFile(string url)
		{
			if (url != "")
				Url = url;
            
			_HtmlFile.WasAppended = false;
			await _HtmlFile.Download().ConfigureAwait(false);
			if (!_HtmlFile.WasDownloaded)
				throw new Exception(string.Format(Strings.MhtBuilder_Error_UnableDownload, Url, _HtmlFile.DownloadException.Message), _HtmlFile.DownloadException);
		}

		/// <summary>
		/// dumps our MhtBuilder as a string and clears it
		/// </summary>
		string FinalizeMht()
		{
			string s = _MhtBuilder.ToString();
			_MhtBuilder = null;
			return s;
		}

		/// <summary>
		/// dumps our MhtBuilder to disk and clears it
		/// </summary>
		void FinalizeMht(string outputFilePath)
		{
			StreamWriter writer = new StreamWriter(outputFilePath, false, _HtmlFile.TextEncoding);
			writer.Write(_MhtBuilder.ToString());
			writer.Close();
			_MhtBuilder = null;
		}

		/// <summary>
		/// returns true if this path refers to a directory (vs. a filename)
		/// </summary>
		bool IsDirectory(string FilePath)
		{
			return FilePath.EndsWith(@"\");
		}

		/// <summary>
		/// ensures that the path, if it contains a filename, matches one of the semicolon delimited 
		/// filetypes provided in fileExtension
		/// </summary>
		void ValidateFilename(string FilePath, string fileExtensions)
		{
			if (IsDirectory(FilePath))
				return;
			
			string ext = Path.GetExtension(FilePath);
			if (ext == "")
			{
				throw new Exception(string.Format(Strings.MhtBuilder_Error_FilenameNoExtension, Path.GetFileName(FilePath), fileExtensions));
			}
			if (!Regex.IsMatch(fileExtensions, ext + "(;|$)", RegexOptions.IgnoreCase))
			{
				throw new Exception(string.Format(Strings.MhtBuilder_Error_Extension, Path.GetFileName(FilePath), fileExtensions));
			}
		}

		#endregion Private methods

		#region Public methods
		/// <summary>
		/// Generates a string representation of the current URL as a Mht archive file
		/// using exclusively in-memory storage
		/// </summary>
		/// <returns>string representation of MHT file</returns>
		public async Task<string> GetPageArchive()
		{
			return await GetPageArchive(string.Empty).ConfigureAwait(false);
		}

		/// <summary>
		/// Generates a string representation of the URL as a Mht archive file
		/// using exclusively in-memory storage
		/// </summary>
		/// <param name="url">fully qualified URL you wish to render to Mht</param>
		/// <returns>string representation of MHT file</returns>
		public async Task<string> GetPageArchive(string url)
		{
            await DownloadHtmlFile(url).ConfigureAwait(false);

            // download all references
            await _HtmlFile.DownloadExternalFiles(_AllowRecursion).ConfigureAwait(false);
			
			// build the Mht 
			AppendMhtHeader(_HtmlFile);
			AppendMhtFiles();

			return this.FinalizeMht();
		}

		/// <summary>
		/// Saves the current URL to disk as a single file Mht archive
		/// if a folder is provided instead of a filename, the TITLE tag is used to name the file
		/// </summary>
		/// <param name="outputFilePath">path to generate to, or filename to generate</param>
		/// <param name="st">type of storage to use when generating the Mht archive</param>
		/// <returns>the complete path of the Mht archive file that was generated</returns>
		public async Task<string> SavePageArchive(string outputFilePath)
		{
			return await SavePageArchive(outputFilePath, string.Empty).ConfigureAwait(false);
		}

		/// <summary>
		/// Saves URL to disk as a single file Mht archive
		/// if a folder is provided instead of a filename, the TITLE tag is used to name the file
		/// </summary>
		/// <param name="outputFilePath">path to generate to, or filename to generate</param>
		/// <param name="st">type of storage to use when generating the Mht archive</param>
		/// <param name="url">fully qualified URL you wish to save as Mht</param>
		/// <returns>the complete path of the Mht archive file that was generated</returns>
		public async Task<string> SavePageArchive(string outputFilePath, string url)
		{
			ValidateFilename(outputFilePath, ".mht");
            await DownloadHtmlFile(url).ConfigureAwait(false);
        
			_HtmlFile.DownloadPath = outputFilePath;
			_HtmlFile.UseHtmlTitleAsFilename = true;

            // download all references
            await _HtmlFile.DownloadExternalFiles(_AllowRecursion).ConfigureAwait(false);
            
			// build the Mht 
			AppendMhtHeader(_HtmlFile);
			AppendMhtFiles();
			
			FinalizeMht(Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht"));
            
			WebFiles.Clear();

			return Path.ChangeExtension(_HtmlFile.DownloadPath, ".mht");
		}

		#endregion Public methods

		#region Quoted-Printable encoding
		/// <summary>
		/// converts a string into Quoted-Printable encoding
		///   http://www.freesoft.org/CIE/RFC/1521/6.htm
		/// </summary>
		string QuotedPrintableEncode(string s, Encoding e)
		{
			int lastSpace = 0;
			int lineLength = 0;
			int lineBreaks = 0;
			StringBuilder sb = new StringBuilder();

			if (s == null || s.Length == 0)
				return "";
			
			foreach (char c in s)
			{
				int ascii = Convert.ToInt32(c);
				if (ascii == 61 || ascii > 126)
				{
					if (ascii <= 255)
					{
						sb.Append("=");
						sb.Append(Convert.ToString(ascii, 16).ToUpper());
						lineLength += 3;
					}
					else
					{
						// double-byte land..
						foreach (byte b in e.GetBytes(c.ToString()))
						{
							sb.Append("=");
							sb.Append(Convert.ToString(b, 16).ToUpper());
							lineLength += 3;
						}
					}
				}
				else
				{
					sb.Append(c);
					lineLength++;
					if (ascii == 32)
						lastSpace = sb.Length;
				}

				if (lineLength >= 73)
				{
					if (lastSpace == 0)
					{
						sb.Insert(sb.Length, "=" + Environment.NewLine);
						lineLength = 0;
					}
					else
					{
						sb.Insert(lastSpace, "=" + Environment.NewLine);
						lineLength = sb.Length - lastSpace - 1;
					}
					lineBreaks++;
					lastSpace = 0;
				}
			}

			// if we split the line, have to indicate trailing spaces
			if (lineBreaks > 0 && sb[sb.Length - 1] == ' ')
			{
				sb.Remove(sb.Length - 1, 1);
				sb.Append("=20");
			}

			return sb.ToString();
		}

		#endregion Quoted-Printable encoding
	}
}