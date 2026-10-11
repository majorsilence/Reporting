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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Majorsilence.Reporting.RdlEngine.Resources;

namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// represents an external file referenced in our parent HTML at the target URL
	/// </summary>
	class MhtWebFile
	{
		#region Fields
		MhtBuilder _Builder;
		string _ContentLocation;
		string _ContentType;
		byte[] _DownloadedBytes;
		Exception _DownloadException = null;
		string _DownloadExtension = "";
		string _DownloadFilename = "";
		string _DownloadFolder = "";
		NameValueCollection _ExternalFileCollection;
		bool _IsBinary;
		Encoding _TextEncoding;
		string _Url;
		string _UrlFolder;
		string _UrlRoot;
		string _UrlUnmodified;
		bool _UseHtmlFilename = false;
		bool _WasDownloaded = false;

		public bool WasAppended;
		#endregion Fields

		#region Constructor
		public MhtWebFile(MhtBuilder parent)
		{
			_Builder = parent;
		}

		public MhtWebFile(MhtBuilder parent, string url)
		{
			_Builder = parent;
			if (url != "")
				this.Url = url;
		}
		#endregion Constructor

		#region Properties
		/// <summary>
		/// The Content-Type of this file as returned by the server
		/// </summary>
		public string ContentType
		{
			get
			{
				return _ContentType;
			}
		}

		/// <summary>
		/// The raw bytes returned from the server for this file
		/// </summary>
		public byte[] DownloadedBytes
		{
			get
			{
				return _DownloadedBytes;
			}
		}

		/// <summary>
		/// If not .WasDownloaded, the exception that prevented download is stored here
		/// </summary>
		public Exception DownloadException
		{
			get
			{
				return _DownloadException;
			}
		}

		/// <summary>
		/// file type extension to use on downloaded file
		/// this property is only used if the DownloadFilename property does not
		/// already contain a file extension
		/// </summary>
		public string DownloadExtension
		{
			get
			{
				if (_DownloadExtension == "" && this.WasDownloaded)
					_DownloadExtension = this.ExtensionFromContentType();
				
				return _DownloadExtension;
			}
			set
			{
				_DownloadExtension = value;
			}
		}

		/// <summary>
		/// filename to download this file as
		/// if no filename is provided, a filename will be auto-generated based on
		/// the URL; if the UseHtmlTitleAsFilename property is true, then the
		/// title tag will be used to generate the filename
		/// </summary>
		public string DownloadFilename
		{
			get
			{
				if (_DownloadFilename == "")
				{
					if (this._UseHtmlFilename && this.WasDownloaded && this.IsHtml)
					{
						string htmlTitle = this.HtmlTitle;
						if (htmlTitle != "")
							_DownloadFilename = MakeValidFilename(htmlTitle, false) + ".htm";
					}
					else
						_DownloadFilename = this.FilenameFromUrl();
				}
				return _DownloadFilename;
			}
			set
			{
				_DownloadFilename = value;
			}
		}

		/// <summary>
		/// folder to download this file to
		/// if no folder is provided, the current application folder will be used
		/// </summary>
		public string DownloadFolder
		{
			get
			{
				if (_DownloadFolder == "")
					_DownloadFolder = AppDomain.CurrentDomain.BaseDirectory;
				
				return _DownloadFolder;
			}
			set
			{
				this._DownloadFolder = value;
			}
		}

		/// <summary>
		/// the folder name used in the DownloadFolder
		/// </summary>
		public string DownloadFolderName
		{
			get
			{
				return Regex.Match(this.DownloadFolder, @"(?<Folder>[^\\]+)\\*$").Groups["Folder"].Value;
			}
		}

		/// <summary>
		/// fully qualified path and filename to download this file to
		/// </summary>
		public string DownloadPath
		{
			get
			{
				if (Path.GetExtension(this.DownloadFilename) == "")
					return Path.Combine(this.DownloadFolder, this.DownloadFilename + this.DownloadExtension);
				
				return Path.Combine(this.DownloadFolder, this.DownloadFilename);
			}
			set
			{
				this._DownloadFilename = Path.GetFileName(value);
				if (_DownloadFilename == "")
					_DownloadFolder = value;
				else
					_DownloadFolder = value.Replace(_DownloadFilename, "");
			}
		}

		/// <summary>
		/// If this file has external dependencies, the folder they will be stored on disk
		/// </summary>
		public string ExternalFilesFolder
		{
			get
			{
				return (Path.Combine(this.DownloadFolder, Path.GetFileNameWithoutExtension(this.DownloadFilename)) + "_files");
			}
		}

		/// <summary>
		/// If this file is HTML, retrieve the &lt;TITLE&gt; tag from the HTML
		/// (maximum of 50 characters)
		/// </summary>
		public string HtmlTitle
		{
			get
			{
//				if (!this.IsHtml)
//					throw new Exception("This file isn't HTML, so it has no HTML <TITLE> tag.");
				
				string remp = this.ToString();
				string s = Regex.Match(this.ToString(), "<title[^>]*?>(?<text>[^<]+)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["text"].Value;
				if (s.Length > 50)
					return s.Substring(0, 50);
				
				return s;
			}
		}

		/// <summary>
		/// Does this file contain binary data? If not, it must be text data.
		/// </summary>
		public bool IsBinary
		{
			get
			{
				return _IsBinary;
			}
		}

		/// <summary>
		/// Is this file CSS content?
		/// </summary>
		public bool IsCss
		{
			get
			{
				return Regex.IsMatch(_ContentType, "text/css", RegexOptions.IgnoreCase);
			}
		}

		/// <summary>
		/// Is this file HTML content?
		/// </summary>
		public bool IsHtml
		{
			get
			{
				return Regex.IsMatch(_ContentType, "text/html", RegexOptions.IgnoreCase);
			}
		}

		/// <summary>
		/// If this file is text (eg, it isn't binary), the type of text encoding used
		/// </summary>
		public Encoding TextEncoding
		{
			get
			{
				return _TextEncoding;
			}
		}

		/// <summary>
		/// The URL target for this file
		/// </summary>
		public string Url
		{
			get
			{
				return this._Url;
			}
			set
			{
				_UrlUnmodified = value;
				SetUrl(value, true);
				_DownloadedBytes = new byte[1];
				_ExternalFileCollection = null;
				_DownloadException = null;
				_TextEncoding = null;
				_ContentType = "";
				_ContentLocation = "";
				_IsBinary = false;
				_WasDownloaded = false;
			}
		}

		/// <summary>
		/// The Content-Location of this URL as provided by the server,
		/// only if the URL was not fully qualified;
		/// eg, http://mywebsite.com/ actually maps to http://mywebsite.com/default.htm 
		/// </summary>
		public string UrlContentLocation
		{
			get
			{
				return _ContentLocation;
			}
		}

		/// <summary>
		/// The root and folder of the URL, eg, http://mywebsite.com/myfolder
		/// </summary>
		public string UrlFolder
		{
			get
			{
				return this._UrlFolder;
			}
		}

		/// <summary>
		/// The root of the URL, eg, http://mywebsite.com/
		/// </summary>
		public string UrlRoot
		{
			get
			{
				return this._UrlRoot;
			}
		}

		/// <summary>
		/// The unmodified "raw" URL as originally provided
		/// </summary>
		public string UrlUnmodified
		{
			get
			{
				return _UrlUnmodified;
			}
		}

		/// <summary>
		/// If enabled, will use the first 50 characters of the TITLE tag 
		/// to form the filename when saved to disk
		/// </summary>
		public bool UseHtmlTitleAsFilename
		{
			get
			{
				return this._UseHtmlFilename;
			}
			set
			{
				this._UseHtmlFilename = value;
			}
		}

		/// <summary>
		/// Was this file successfully downloaded via HTTP?
		/// </summary>
		public bool WasDownloaded
		{
			get
			{
				return _WasDownloaded;
			}
		}

		#endregion Properties

		#region Public methods
		/// <summary>
		/// converts all external Html files (gif, jpg, css, etc) to local refs
		/// external ref:
		///    &lt;img src="http://mywebsite/myfolder/myimage.gif"&gt;
		/// into local refs:
		///    &lt;img src="mypage_files/myimage.gif"&gt;
		/// </summary>
		public void ConvertReferencesToLocal()
		{
			if (!IsHtml && !IsCss)
				throw new Exception(string.Format(Strings.MhtWebFile_Error_ConvertOnlyHTMLOrCSS, ContentType));
			
			// get a list of all external references
			string html = this.ToString();
			NameValueCollection fileCollection = this.ExternalHtmlFiles();
			
			// no external refs? nothing to do
			if (fileCollection.Count == 0)
				return;
			
			string[] keys = fileCollection.AllKeys;
			for (int idx = 0; idx < keys.Length; idx++)
			{
				string delimitedUrl = keys[idx];
				string fileUrl = fileCollection[delimitedUrl];
				if (_Builder.WebFiles.Contains(fileUrl))
				{
					MhtWebFile wf = (MhtWebFile) _Builder.WebFiles[fileUrl];
					string newPath = this.ExternalFilesFolder + "/" + wf.DownloadFilename;
					string delimitedReplacement = Regex.Replace(delimitedUrl, 
						@"^(?<StartDelim>""|'|\()*(?<Value>[^'"")]*)(?<EndDelim>""|'|\))*$",
						"${StartDelim}" + newPath + "${EndDelim}");

					// correct original Url references in Html so they point to our local files
					html = html.Replace(delimitedUrl, delimitedReplacement);
				}
			}

			_DownloadedBytes = _TextEncoding.GetBytes(html);
		}

		/// <summary>
		/// Download this file from the target URL
		/// </summary>
		public async Task Download()
		{
			Debug.Write("Downloading " + this._Url + "  ..");
			await DownloadBytes().ConfigureAwait(false);
			
			if (_DownloadException == null)
				Debug.WriteLine("OK");
			else
			{
				Debug.WriteLine("failed: ", "Error");
				Debug.WriteLine("    " + _DownloadException.Message, "Error");
				return;
			}
			
			if (this.IsHtml)
				_DownloadedBytes = _TextEncoding.GetBytes(ProcessHtml(this.ToString()));
			
			if (this.IsCss)
				_DownloadedBytes = _TextEncoding.GetBytes(ProcessHtml(this.ToString()));
		}

		/// <summary>
		/// download ALL externally referenced files in this file's html, not recursively,
		/// to the default download path for this page
		/// </summary>
		public async Task DownloadExternalFiles()
		{
			await DownloadExternalFiles(ExternalFilesFolder, false).ConfigureAwait(false);
		}

		/// <summary>
		/// download ALL externally referenced files in this file's html, potentially recursively,
		/// to the default download path for this page
		/// </summary>
		public async Task DownloadExternalFiles(bool recursive)
		{
			await DownloadExternalFiles(ExternalFilesFolder, recursive).ConfigureAwait(false);
		}

		/// <summary>
		/// Saves this file to disk as a plain text file
		/// </summary>
		public void SaveAsTextFile()
		{
			this.SaveToFile(Path.ChangeExtension(this.DownloadPath, ".txt"), true);
		}

		/// <summary>
		/// Saves this file to disk as a plain text file, to an arbitrary path
		/// </summary>
		public void SaveAsTextFile(string filePath)
		{
			this.SaveToFile(filePath, true);
		}

		/// <summary>
		/// writes contents of file to DownloadPath, using appropriate encoding as necessary
		/// </summary>
		public void SaveToFile()
		{
			this.SaveToFile(this.DownloadPath, false);
		}

		/// <summary>
		/// writes contents of file to DownloadPath, using appropriate encoding as necessary
		/// </summary>
		public void SaveToFile(string filePath)
		{
			this.SaveToFile(filePath, false);
		}

		/// <summary>
		/// Returns a string representation of the data downloaded for this file
		/// </summary>
		public override string ToString()
		{
			// Not downloaded yet means empty. ToString used to start the download itself (blocking on it),
			// but a download is async and every caller in MhtBuilder awaits Download() first.
			if (!_WasDownloaded || _DownloadedBytes.Length <= 0)
				return "";
		
			if (_IsBinary)
				return ("[" + _DownloadedBytes.Length.ToString() + " bytes of binary data]");
				
			return this.TextEncoding.GetString(_DownloadedBytes);
		}

		/// <summary>
		/// Returns the plain text representation of the data in this file, 
		/// stripping out any HTML tags and codes
		/// </summary>
		public string ToTextString(bool removeWhitespace /* = false */)
		{
			string html = this.ToString();

			// get rid of <script> .. </script>
			html = this.StripHtmlTag("script", html);
			
			// get rid of <style> .. </style>
			html = this.StripHtmlTag("style", html);
			
			// get rid of all HTML tags
			html = Regex.Replace(html, 
				@"<\w+(\s+[A-Za-z0-9_\-]+\s*=\s*(""([^""]*)""|'([^']*)'))*\s*(/)*>|<[^>]+>", 
				" ");

			// convert escaped HTML to plaintext
			html = HtmlDecode(html);

			if (removeWhitespace)
			{
				// clean up whitespace (optional, depends what you want..)
				html = Regex.Replace(html, @"[\n\r\f\t]", " ", RegexOptions.Multiline);
				html = Regex.Replace(html, " {2,}", " ", RegexOptions.Multiline);
			}
			return html;
		}

		#endregion Public methods

		#region Private methods
		/// <summary>
		/// appends key=value named matches in a regular expression
		/// to a target NameValueCollection
		/// </summary>
		void AddMatchesToCollection(string s, Regex r, ref NameValueCollection nvc)
		{
			bool headerDisplayed = false;
//			Regex urlRegex = new Regex(@"^https*://\w+", RegexOptions.IgnoreCase);
			Regex urlRegex = new Regex(@"^files*:///\w+", RegexOptions.IgnoreCase);

			foreach (Match match in r.Matches(s))
			{
				if (!headerDisplayed)
				{
					Debug.WriteLine("Matches added from regex:");
					Debug.WriteLine("'" + match.ToString() + "'");
					headerDisplayed = true;
				}
			
				string key = match.Groups["Key"].ToString();
				string val = match.Groups["Value"].ToString();
				if (nvc[key] == null)
				{
					Debug.WriteLine(" Match: " + match.ToString());
					Debug.WriteLine("   Key: " + key);
					Debug.WriteLine(" Value: " + val);
					if (urlRegex.IsMatch(val))
						nvc.Add(key, val);
					else
						Debug.WriteLine("Match discarded; does not appear to be valid fully qualified file:// Url", "Error");
//						Debug.WriteLine("Match discarded; does not appear to be valid fully qualified http:// Url", "Error");
				}
			}
		}

		/// <summary>
		/// converts all relative url references
		///    href="myfolder/mypage.htm"
		/// into absolute url references
		///    href="http://mywebsite/myfolder/mypage.htm"
		/// </summary>
		string ConvertRelativeToAbsoluteRefs(string html)
		{
			string urlPattern = 
				@"(?<attrib>\shref|\ssrc|\sbackground)\s*?=\s*?" +
				@"(?<delim1>[""'\\]{0,2})(?!\s*\+|#|http:|ftp:|mailto:|javascript:)" +
				@"/(?<url>[^""'>\\]+)(?<delim2>[""'\\]{0,2})";

			string cssPattern = 
				@"(?<attrib>@import\s|\S+-image:|background:)\s*?(url)*['""(]{1,2}" +
				@"(?!http)\s*/(?<url>[^""')]+)['"")]{1,2}";

			// href="/anything" to href="http://www.web.com/anything"
			Regex r = new Regex(urlPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			html = r.Replace(html, "${attrib}=${delim1}" + this._UrlRoot + "/${url}${delim2}");
			
			// href="anything" to href="http://www.web.com/folder/anything"
			r = new Regex(urlPattern.Replace("/", ""), RegexOptions.Multiline | RegexOptions.IgnoreCase);
			html = r.Replace(html, "${attrib}=${delim1}" + this._UrlFolder + "/${url}${delim2}");
			
			// @import(/anything) to @import url(http://www.web.com/anything)
			r = new Regex(cssPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			html = r.Replace(html, "${attrib} url(" + this._UrlRoot + "/${url})");
			
			// @import(anything) to @import url(http://www.web.com/folder/anything)
			r = new Regex(cssPattern.Replace("/", ""), RegexOptions.Multiline | RegexOptions.IgnoreCase);
			html = r.Replace(html, "${attrib} url(" + this._UrlFolder + "/${url})");
	
			return html;
		}

		/// <summary>
		/// if the user passed in a directory, form the filename automatically using the Html title tag
		/// if the user passed in a filename, make sure the extension matches our desired extension
		/// </summary>
		string DeriveFilename(string FilePath, string html, string fileExtension)
		{
			if (IsDirectory(FilePath))
			{
				string htmlTitle = this.HtmlTitle;
				if (htmlTitle == "")
					throw new Exception(Strings.MhtWebFile_Error_NoFilename);
				
				return Path.Combine(Path.GetDirectoryName(FilePath), MakeValidFilename(htmlTitle, false) + fileExtension);
			}

			if (Path.GetExtension(FilePath) != fileExtension)
				return Path.ChangeExtension(FilePath, fileExtension);
			
			return FilePath;
		}

		/// <summary>
		/// download this file from the target URL;
		/// place the bytes downloaded in _DownloadedBytes
		/// if an exception occurs, capture it in _DownloadException
		/// </summary>
		async Task DownloadBytes()
		{
			if (this.WasDownloaded)
				return;
			
			// always download to memory first
			try
			{
				_DownloadedBytes = await _Builder.WebClient.DownloadBytes(_Url).ConfigureAwait(false);
				_WasDownloaded = true;
			}
			catch (WebException ex)
			{
				_DownloadException = ex;
				_Builder.WebClient.ClearDownload();
			}

			// necessary if the original client URL was imprecise;
			// server location is always authoritatitve
			if (_Builder.WebClient.ContentLocation != "")
			{
				_ContentLocation = _Builder.WebClient.ContentLocation;
				SetUrl(_ContentLocation, false);
			}

			_IsBinary = _Builder.WebClient.ResponseIsBinary;
			_ContentType = _Builder.WebClient.ResponseContentType;
			_TextEncoding = _Builder.WebClient.DetectedEncoding;
			_Builder.WebClient.ClearDownload();
		}

		/// <summary>
		/// Download a single externally referenced file (if we haven't already downloaded it)
		/// </summary>
		async Task DownloadExternalFile(string url, string targetFolder, bool recursive)
		{
			bool isNew;
			MhtWebFile wf;
			
			// have we already downloaded (or attempted to) this file?
			if (_Builder.WebFiles.Contains(url) || _Builder.Url == url)
			{
				wf = (MhtWebFile) _Builder.WebFiles[url];
				isNew = false;
			}
			else
			{
				wf = new MhtWebFile(_Builder, url);
				isNew = true;
			}

			await wf.Download().ConfigureAwait(false);
			
			if (isNew)
			{
				// add this (possibly) downloaded file to our shared collection
				_Builder.WebFiles.Add(wf.UrlUnmodified, wf);
				
				// if this is an HTML file, it has dependencies of its own;
				// download them into a subfolder
				if ((wf.IsHtml || wf.IsCss) && recursive)
					await wf.DownloadExternalFiles(recursive).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// download ALL externally referenced files in this html, potentially recursively
		/// to a specific download path
		/// </summary>
		async Task DownloadExternalFiles(string targetFolder, bool recursive)
		{
			NameValueCollection fileCollection = ExternalHtmlFiles();
			if (!fileCollection.HasKeys())
				return;
			
			Debug.WriteLine("Downloading all external files collected from URL:");
			Debug.WriteLine("    " + this.Url);
			foreach (string key in fileCollection.Keys)
				await DownloadExternalFile(fileCollection[key], targetFolder, recursive).ConfigureAwait(false);
		}

		/// <summary>
		/// if we weren't given a filename extension, infer it from the download
		/// Content-Type header
		/// </summary>
		/// <remarks>
		/// http://www.utoronto.ca/webdocs/HTMLdocs/Book/Book-3ed/appb/mimetype.html
		/// </remarks>
		string ExtensionFromContentType()
		{
			switch (Regex.Match(this.ContentType, "^[^ ;]+").Value.ToLower())
			{
				case "text/html":
					return ".htm";
				case "image/gif":
					return ".gif";
				case "image/jpeg":
					return ".jpg";
				case "text/javascript":
				case "application/x-javascript":
					return ".js";
				case "image/x-png":
					return ".png";
				case "text/css":
					return ".css";
				case "text/plain":
					return ".txt";
				default:
					Debug.WriteLine("Unknown content-type '" + this.ContentType + "'", "Error");
					return ".htm";
			}
		}

		/// <summary>
		/// returns a name/value collection of all external files referenced in HTML:
		/// 
		///     "/myfolder/blah.png"
		///     'http://mywebsite/blah.gif'
		///     src=blah.jpg  
		/// 
		/// note that the Key includes the delimiting quotes or parens (if present), but the Value does not
		/// this is important because the delimiters are used for matching and replacement to make the
		/// match more specific!
		/// </summary>
		NameValueCollection ExternalHtmlFiles()
		{
			// avoid doing this work twice, however, be careful that the HTML hasn't
			// changed since the last time we called this function
			if (_ExternalFileCollection != null)
				return _ExternalFileCollection;

			_ExternalFileCollection = new NameValueCollection();
			string html = this.ToString();

			Debug.WriteLine("Resolving all external HTML references from URL:");
			Debug.WriteLine("    " + this.Url);

			// src='filename.ext' ; background="filename.ext"
			// note that we have to test 3 times to catch all quote styles: '', "", and none
			Regex r = new Regex(
				@"(\ssrc|\sbackground)\s*=\s*((?<Key>'(?<Value>[^']+)')|(?<Key>""(?<Value>[^""]+)"")|(?<Key>(?<Value>[^ \n\r\f]+)))",
				RegexOptions.Multiline | RegexOptions.IgnoreCase);
			AddMatchesToCollection(html, r, ref _ExternalFileCollection);
			
			// @import "style.css" or @import url(style.css)
			r = new Regex(
				@"(@import\s|\S+-image:|background:)\s*?(url)*\s*?(?<Key>[""'(]{1,2}(?<Value>[^""')]+)[""')]{1,2})", 
				RegexOptions.Multiline | RegexOptions.IgnoreCase);
			AddMatchesToCollection(html, r, ref _ExternalFileCollection);

			// <link rel=stylesheet href="style.css">
			r = new Regex(
				@"<link[^>]+?href\s*=\s*(?<Key>('|"")*(?<Value>[^'"">]+)('|"")*)",
				RegexOptions.Multiline | RegexOptions.IgnoreCase);
			AddMatchesToCollection(html, r, ref _ExternalFileCollection);

			// <iframe src="mypage.htm"> or <frame src="mypage.aspx">
			r = new Regex(
				@"<i*frame[^>]+?src\s*=\s*(?<Key>['""]{0,1}(?<Value>[^'""\\>]+)['""]{0,1})", 
				RegexOptions.Multiline | RegexOptions.IgnoreCase);
			AddMatchesToCollection(html, r, ref _ExternalFileCollection);
			
			return _ExternalFileCollection;
		}

		/// <summary>
		/// attempt to get a coherent filename out of the Url
		/// </summary>
		string FilenameFromUrl()
		{
			// first, try to get a filename out of the URL itself;
			// this means anything past the final slash that doesn't include another slash
			// or a question mark, eg http://mywebsite/myfolder/crazy?param=1&param=2
			string filename = Regex.Match(this._Url, "/(?<Filename>[^/?]+)[^/]*$").Groups["Filename"].Value;
			if (filename != "")
			{
				// that worked, but we need to make sure the filename is unique
				// if query params were passed to the URL file
				Uri u = new Uri(this._Url);
				if (u.Query != "")
					filename = Path.GetFileNameWithoutExtension(filename) + "_" + u.Query.GetHashCode().ToString() + this.DownloadExtension;
			}

			// ok, that didn't work; if this file is HTML try to get the TITLE tag
			if (filename == "" && this.IsHtml)
			{
				filename = this.HtmlTitle;
				if (filename != "")
					filename = filename + ".htm";
			}

			// now we're really desperate. Hash the URL and make that the filename.
			if (filename == "")
				filename = _Url.GetHashCode().ToString() + this.DownloadExtension;

			return this.MakeValidFilename(filename, false);
		}

		/// <summary>
		/// returns true if this path refers to a directory (vs. a filename)
		/// </summary>
		bool IsDirectory(string FilePath)
		{
			return FilePath.EndsWith(@"\");
		}

		/// <summary>
		/// removes all unsafe filesystem characters to form a valid filesystem filename
		/// </summary>
		string MakeValidFilename(string s, bool enforceLength /* = false */)
		{
            //if (enforceLength)
            //{
            //}

            //// replace any invalid filesystem chars, plus leading/trailing/doublespaces
            //return Regex.Replace(
            //    Regex.Replace(
            //    s, 
            //    @"[\/\\\:\*\?\""""\<\>\|]|^\s+|\s+$", 
            //    ""),
            //    @"\s{2,}", 
            //    " ");

            // Replaces any invalid filesystem chars, plus leading/trailing/doublespaces. 
            string name = Regex.Replace(
            Regex.Replace(
            s,
            @"[\/\\\:\*\?\""""\<\>\|]|^\s+|\s+$",
            ""),
            @"\s{2,}",
            " ");

            // Enforces the maximum length to 25 characters. 
            if (name.Length > 25)
            {
                string ext = Path.GetExtension(name);
                name = name.Substring(0, 25 - ext.Length) + ext;
            }

            return name; 
		}

		/// <summary>
		/// Pre-process the CSS using global preference settings
		/// </summary>
		string ProcessCss(string css)
		{
			return this.ConvertRelativeToAbsoluteRefs(css);
		}

		/// <summary>
		/// Pre-process the HTML using global preference settings
		/// </summary>
		string ProcessHtml(string html)
		{
			Debug.WriteLine("Downloaded content was HTML/CSS -- processing: resolving URLs, getting <base>, etc");
			if (_Builder.AddWebMark)
			{
				// add "mark of the web":
				// http://www.microsoft.com/technet/prodtechnol/winxppro/maintain/sp2brows.mspx#XSLTsection133121120120
				html = "<!-- saved from url=(" + string.Format("{0:0000}", this._Url.Length) +
					")" + this._Url +  " -->" + Environment.NewLine + html;
			}

			// see if we need to strip elements from the HTML
			if (_Builder.StripScripts)
				html = this.StripHtmlTag("script", html);
			if (_Builder.StripIframes)
				html = this.StripHtmlTag("iframe", html);

			// if we have a <base>, we must use it as the _UrlFolder, 
			// not what was parsed from the original _Url
			string baseUrlFolder = Regex.Match(
				html, 
				"<base[^>]+?href=['\"]{0,1}(?<BaseUrl>[^'\">]+)['\"]{0,1}", 
				RegexOptions.IgnoreCase).Groups["BaseUrl"].Value;
			if (baseUrlFolder != "")
			{
				if (baseUrlFolder.EndsWith("/"))
					_UrlFolder = baseUrlFolder.Substring(0, baseUrlFolder.Length - 1);
				else
					_UrlFolder = baseUrlFolder;
			}
			
			// remove the <base href=''> tag if present; causes problems when viewing locally.
			html = Regex.Replace(html, "<base[^>]*?>", "");

			// relative URLs are a PITA for the processing we're about to do, 
			// so convert them all to absolute up front
			return this.ConvertRelativeToAbsoluteRefs(html);
		}

		/// <summary>
		/// fully resolves any relative pathing inside the URL, and other URL oddities
		/// </summary>
		string ResolveUrl(string url)
		{
			// resolve any relative pathing
			try
			{
				url = new Uri(url).AbsoluteUri;
			}
			catch (UriFormatException ex)
			{
				throw new ArgumentException("'" + url + "' does not appear to be a valid URL.", ex);
			}

			// remove any anchor tags from the end of URLs
			if (url.IndexOf("#") > -1)
			{
				string jump = Regex.Match(url, "/[^/]*?(?<jump>#[^/?.]+$)").Groups["jump"].Value;
				if (jump != "")
					url = url.Replace(jump, "");
			}

			return url;
		}

		/// <summary>
		/// sets the DownloadPath and writes contents of file, using appropriate encoding as necessary
		/// </summary>
		void SaveToFile(string filePath, bool asText)
		{
			Debug.WriteLine("Saving to file " + filePath);
			using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
			{
				using (BinaryWriter writer = new BinaryWriter(fs))
				{
					if (this.IsBinary)
						writer.Write(_DownloadedBytes);
					else if (asText)
						writer.Write(this.ToTextString(false));
					else
						writer.Write(_DownloadedBytes);
				}
			}
		}

		void SetUrl(string url, bool validate)
		{
			if (validate)
				this._Url = this.ResolveUrl(url);
			else
				this._Url = url;
			
			// http://mywebsite
			this._UrlRoot = Regex.Match(url, "http://[^/'\"]+", RegexOptions.IgnoreCase).ToString();
			// http://mywebsite/myfolder
			if (this._Url.LastIndexOf("/") > 7)
				this._UrlFolder = this._Url.Substring(0, this._Url.LastIndexOf("/"));
			else
				this._UrlFolder = this._UrlRoot;
		}

		/// <summary>
		/// perform the regex replacement of all &lt;tagName&gt; .. &lt;/tagName&gt; blocks
		/// </summary>
		string StripHtmlTag(string tagName, string html)
		{
			Regex r = new Regex(
				string.Format(@"<{0}[^>]*?>[\w|\t|\r|\W]*?</{0}>", tagName), RegexOptions.Multiline | RegexOptions.IgnoreCase);
			return r.Replace(html, "");
		}

		#region Html decoding
		string HtmlDecode(string s)
		{
			if (s == null)
				return null;
			if (s.IndexOf('&') < 0)
				return s;
			
			StringBuilder builder = new StringBuilder();
			StringWriter writer = new StringWriter(builder);

			for (int i = 0; i < s.Length; i++)
			{
				char currentChar = s[i];
				if (currentChar != '&')
				{
					writer.Write(currentChar);
					continue;
				}

				int pos = s.IndexOf(';', i + 1);
				if (pos <= 0)
				{
					writer.Write(currentChar);
					continue;
				}

				string subText = s.Substring(i + 1, (pos - i) - 1);
				if (subText[0] == '#' && subText.Length > 1)
				{
					try
					{
						if ((subText[1] == 'x') || (subText[1] == 'X'))
							writer.Write((char) ((ushort) int.Parse(subText.Substring(2), 
								System.Globalization.NumberStyles.AllowHexSpecifier)));
						else
							writer.Write((char) ((ushort) int.Parse(subText.Substring(1))));
						i = pos;
					}
					catch (FormatException)
					{
						i++;
					}
					catch (ArgumentException)
					{
						i++;
					}
				}
				else
				{
					i = pos;
					currentChar = HtmlLookup(subText);
					if (currentChar != '\0')
					{
						writer.Write(currentChar);
					}
					else
					{
						writer.Write('&');
						writer.Write(subText);
						writer.Write(';');
					}
				}
			}

			return builder.ToString();
		}

		static volatile Hashtable htmlEntitiesTable = null;

		char HtmlLookup(string entity)
		{
			if (htmlEntitiesTable == null)
			{
				lock (typeof(MhtWebFile))
				{
					if (htmlEntitiesTable == null)
					{
						// Filled in a local and published afterwards: another thread seeing the field
						// non-null skips the lock, so it must never see a half-filled table.
						var table = new Hashtable();
						string[] htmlEntities = new string[] 
						{ 
							"\"-quot", "&-amp", "<-lt", ">-gt", "\x00a0-nbsp", "\x00a1-iexcl", "\x00a2-cent", "\x00a3-pound", "\x00a4-curren", "\x00a5-yen", "\x00a6-brvbar", "\x00a7-sect", "\x00a8-uml", "\x00a9-copy", "\x00aa-ordf", "\x00ab-laquo", 
							"\x00ac-not", "\x00ad-shy", "\x00ae-reg", "\x00af-macr", "\x00b0-deg", "\x00b1-plusmn", "\x00b2-sup2", "\x00b3-sup3", "\x00b4-acute", "\x00b5-micro", "\x00b6-para", "\x00b7-middot", "\x00b8-cedil", "\x00b9-sup1", "\x00ba-ordm", "\x00bb-raquo", 
							"\x00bc-frac14", "\x00bd-frac12", "\x00be-frac34", "\x00bf-iquest", "\x00c0-Agrave", "\x00c1-Aacute", "\x00c2-Acirc", "\x00c3-Atilde", "\x00c4-Auml", "\x00c5-Aring", "\x00c6-AElig", "\x00c7-Ccedil", "\x00c8-Egrave", "\x00c9-Eacute", "\x00ca-Ecirc", "\x00cb-Euml", 
							"\x00cc-Igrave", "\x00cd-Iacute", "\x00ce-Icirc", "\x00cf-Iuml", "\x00d0-ETH", "\x00d1-Ntilde", "\x00d2-Ograve", "\x00d3-Oacute", "\x00d4-Ocirc", "\x00d5-Otilde", "\x00d6-Ouml", "\x00d7-times", "\x00d8-Oslash", "\x00d9-Ugrave", "\x00da-Uacute", "\x00db-Ucirc", 
							"\x00dc-Uuml", "\x00dd-Yacute", "\x00de-THORN", "\x00df-szlig", "\x00e0-agrave", "\x00e1-aacute", "\x00e2-acirc", "\x00e3-atilde", "\x00e4-auml", "\x00e5-aring", "\x00e6-aelig", "\x00e7-ccedil", "\x00e8-egrave", "\x00e9-eacute", "\x00ea-ecirc", "\x00eb-euml", 
							"\x00ec-igrave", "\x00ed-iacute", "\x00ee-icirc", "\x00ef-iuml", "\x00f0-eth", "\x00f1-ntilde", "\x00f2-ograve", "\x00f3-oacute", "\x00f4-ocirc", "\x00f5-otilde", "\x00f6-ouml", "\x00f7-divide", "\x00f8-oslash", "\x00f9-ugrave", "\x00fa-uacute", "\x00fb-ucirc", 
							"\x00fc-uuml", "\x00fd-yacute", "\x00fe-thorn", "\x00ff-yuml", "\u0152-OElig", "\u0153-oelig", "\u0160-Scaron", "\u0161-scaron", "\u0178-Yuml", "\u0192-fnof", "\u02c6-circ", "\u02dc-tilde", "\u0391-Alpha", "\u0392-Beta", "\u0393-Gamma", "\u0394-Delta", 
							"\u0395-Epsilon", "\u0396-Zeta", "\u0397-Eta", "\u0398-Theta", "\u0399-Iota", "\u039a-Kappa", "\u039b-Lambda", "\u039c-Mu", "\u039d-Nu", "\u039e-Xi", "\u039f-Omicron", "\u03a0-Pi", "\u03a1-Rho", "\u03a3-Sigma", "\u03a4-Tau", "\u03a5-Upsilon", 
							"\u03a6-Phi", "\u03a7-Chi", "\u03a8-Psi", "\u03a9-Omega", "\u03b1-alpha", "\u03b2-beta", "\u03b3-gamma", "\u03b4-delta", "\u03b5-epsilon", "\u03b6-zeta", "\u03b7-eta", "\u03b8-theta", "\u03b9-iota", "\u03ba-kappa", "\u03bb-lambda", "\u03bc-mu", 
							"\u03bd-nu", "\u03be-xi", "\u03bf-omicron", "\u03c0-pi", "\u03c1-rho", "\u03c2-sigmaf", "\u03c3-sigma", "\u03c4-tau", "\u03c5-upsilon", "\u03c6-phi", "\u03c7-chi", "\u03c8-psi", "\u03c9-omega", "\u03d1-thetasym", "\u03d2-upsih", "\u03d6-piv", 
							"\u2002-ensp", "\u2003-emsp", "\u2009-thinsp", "\u200c-zwnj", "\u200d-zwj", "\u200e-lrm", "\u200f-rlm", "\u2013-ndash", "\u2014-mdash", "\u2018-lsquo", "\u2019-rsquo", "\u201a-sbquo", "\u201c-ldquo", "\u201d-rdquo", "\u201e-bdquo", "\u2020-dagger", 
							"\u2021-Dagger", "\u2022-bull", "\u2026-hellip", "\u2030-permil", "\u2032-prime", "\u2033-Prime", "\u2039-lsaquo", "\u203a-rsaquo", "\u203e-oline", "\u2044-frasl", "\u20ac-euro", "\u2111-image", "\u2118-weierp", "\u211c-real", "\u2122-trade", "\u2135-alefsym", 
							"\u2190-larr", "\u2191-uarr", "\u2192-rarr", "\u2193-darr", "\u2194-harr", "\u21b5-crarr", "\u21d0-lArr", "\u21d1-uArr", "\u21d2-rArr", "\u21d3-dArr", "\u21d4-hArr", "\u2200-forall", "\u2202-part", "\u2203-exist", "\u2205-empty", "\u2207-nabla", 
							"\u2208-isin", "\u2209-notin", "\u220b-ni", "\u220f-prod", "\u2211-sum", "\u2212-minus", "\u2217-lowast", "\u221a-radic", "\u221d-prop", "\u221e-infin", "\u2220-ang", "\u2227-and", "\u2228-or", "\u2229-cap", "\u222a-cup", "\u222b-int", 
							"\u2234-there4", "\u223c-sim", "\u2245-cong", "\u2248-asymp", "\u2260-ne", "\u2261-equiv", "\u2264-le", "\u2265-ge", "\u2282-sub", "\u2283-sup", "\u2284-nsub", "\u2286-sube", "\u2287-supe", "\u2295-oplus", "\u2297-otimes", "\u22a5-perp", 
							"\u22c5-sdot", "\u2308-lceil", "\u2309-rceil", "\u230a-lfloor", "\u230b-rfloor", "\u2329-lang", "\u232a-rang", "\u25ca-loz", "\u2660-spades", "\u2663-clubs", "\u2665-hearts", "\u2666-diams"
						};

						for (int i = 0; i < htmlEntities.Length; i++)
						{
							string current = htmlEntities[i];
							table[current.Substring(2)] = current[0];
						}
						htmlEntitiesTable = table;
					}
				}
			}

			object oChar = htmlEntitiesTable[entity];
			if (oChar != null)
				return (char) oChar;
			return '\0';
		}

		#endregion Html decoding

		#endregion Private methods
	}
}