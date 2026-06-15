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


using Majorsilence.Reporting.Rdl.Utility;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	class MhtWebClientLocal
	{
		#region Fields and enum
		const string _AcceptedEncodings = "gzip,deflate";
		const string _DefaultHttpUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1)";

		string _ContentLocation;
		Encoding _DefaultEncoding;
		string _DetectedContentType;
		Encoding _DetectedEncoding;
		Encoding _ForcedEncoding;
		byte[] _ResponseBytes;

		enum HttpContentEncoding
		{
			None,
			Gzip,
			Compress,
			Deflate,
			Unknown
		}
		#endregion Fields and enum

		#region Properties
		/// <summary>
		/// Returns the actual location of the downloaded content, which can 
		/// be different than the requested URL, eg, http://web.com/IsThisAfolderOrNot
		/// </summary>
		public string ContentLocation
		{
			get
			{
				return _ContentLocation;
			}
		}

		/// <summary>
		/// if the correct string encoding type cannot be detected, or detection is disabled
		/// this is the default string encoding that will be used.
		/// </summary>
		public Encoding DefaultEncoding
		{
			get
			{
				return _DefaultEncoding;
			}
			set
			{
				_DefaultEncoding = value;
			}
		}

		/// <summary>
		/// this is the string encoding that was autodetected from the HTML content
		/// </summary>
		public Encoding DetectedEncoding
		{
			get
			{
				return _DetectedEncoding;
			}
		}

		/// <summary>
		/// Bypass detection of content encoding and force a specific encoding
		/// </summary>
		public Encoding ForcedEncoding
		{
			get
			{
				return _ForcedEncoding;
			}
			set
			{
				_ForcedEncoding = value;
			}
		}

		/// <summary>
		/// Returns the raw bytestream representing the last HTTP response data
		/// </summary>
		public byte[] ResponseBytes
		{
			get
			{
				return _ResponseBytes;
			}
		}

		/// <summary>
		/// this is the string encoding that was autodetected from the HTML content
		/// </summary>
		public string ResponseContentType
		{
			get
			{
				return _DetectedContentType;
			}
		}

		/// <summary>
		/// Returns true if the last HTTP response was in a non-text format
		/// </summary>
		public bool ResponseIsBinary
		{
			get
			{
				// if we truly have no content-type, we're kinda hosed, but 
				// let's assume the response is text data to be on the safe side
				if (_DetectedContentType == "")
					return false;
				
				return (_DetectedContentType.IndexOf("text") == -1);
			}
		}

		/// <summary>
		/// Returns a string representation, with encoding, of the last HTTP response data
		/// </summary>
		public string ResponseString
		{
			get
			{
				if (this.ResponseIsBinary)
					return "(" + _ResponseBytes.Length.ToString() + " bytes of binary data)";
				
				if (_ForcedEncoding == null)
					return _DetectedEncoding.GetString(_ResponseBytes);
				
				return _ForcedEncoding.GetString(_ResponseBytes);
			}
		}

		#endregion Properties

		#region Constructor
		public MhtWebClientLocal()
		{
			// sets default values
			this.Clear();
		}

		#endregion Constructor

		#region Public methods
		/// <summary>
		/// clears any downloaded content and resets all properties to default values
		/// </summary>
		public void Clear()
		{
			ClearDownload();
			_DefaultEncoding = Encoding.GetEncoding("Windows-1252");
			_ForcedEncoding = null;
		}

		/// <summary>
		/// clears any downloaded content
		/// </summary>
		public void ClearDownload()
		{
			_ResponseBytes = null;
			_DetectedEncoding = null;
			_DetectedContentType = "";
			_ContentLocation = "";
		}

		/// <summary>
		/// download URL contents to an array of bytes, using HTTP compression if possible
		/// </summary>
		public async Task<byte[]> DownloadBytes(string url)
		{
			await GetUrlData(url);
			return _ResponseBytes;
		}

        // ...

#if NET48
        public Task GetUrlData(string url)
#else
        public async Task GetUrlData(string url)
#endif
        {
            Uri uri = new Uri(url);
            if (!uri.IsFile)
                throw new UriFormatException("url is not a local file");

            string localPath = uri.LocalPath;
            if (!File.Exists(localPath))
            {
                this.Clear();
#if NET48
                return Task.CompletedTask;
#else
                return;
#endif
            }

            // HttpClient does not support file:// URIs on non-Windows platforms.
            // Read local files directly to avoid the platform limitation.
#if NET48
            _ResponseBytes =  File.ReadAllBytes(localPath);
#else
            _ResponseBytes = await File.ReadAllBytesAsync(localPath);
#endif
            _ContentLocation = "";

                // if we have string content, determine encoding type
                // (must cast to prevent null)
                // HACK We determine the content type based on the uri extension, 
                // as the header returned by the FileWebResponse is always "application/octet-stream" (hard coded in .NET!!)
                // text/html
                string ext = Path.GetExtension(uri.LocalPath).TrimStart(new char[] { '.' });
                switch (ext)
                {
                    // What's important here is to identify TEXT mime types. Because, the default will resort to binary file.
                    case "htm":
                    case "html": _DetectedContentType = "text/html"; break;
                    case "css": _DetectedContentType = "text/css"; break;
                    case "csv": _DetectedContentType = "text/csv"; break;
                    case "rtf": _DetectedContentType = "text/rtf"; break;
                    case "aspx":
                    case "xsl":
                    case "xml": _DetectedContentType = "text/xml"; break;

                    case "bmp": _DetectedContentType = "image/bmp"; break;
                    case "gif": _DetectedContentType = "image/gif"; break;
                    case "ico": _DetectedContentType = "image/x-icon"; break;
                    case "jpg":
                    case "jpeg": _DetectedContentType = "image/jpeg"; break;
                    case "png": _DetectedContentType = "image/png"; break;
                    case "tif":
                    case "tiff": _DetectedContentType = "image/tiff"; break;

                    case "js": _DetectedContentType = "application/x-javascript"; break;
                    default:
                        _DetectedContentType = "application/octet-stream";
                        break;
                }
                if (_DetectedContentType == null)
                    _DetectedContentType = "";
                if (ResponseIsBinary)
                    _DetectedEncoding = null;
                else if (_ForcedEncoding == null)
                    _DetectedEncoding = DetectEncoding(_DetectedContentType, _ResponseBytes);
                
#if NET48
                return Task.CompletedTask;
#endif
        }

#endregion Public methods
		
        #region Private methods
		/// <summary>
		/// attempt to convert this charset string into a named .NET text encoding
		/// </summary>
		private Encoding CharsetToEncoding(string charset)
		{
			if (charset == "")
				return null;
			
			try
			{
				return Encoding.GetEncoding(charset);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		/// <summary>
		/// try to determine string encoding using Content-Type HTTP header and
		/// raw HTTP content bytes
		/// "Content-Type: text/html; charset=us-ascii"
		/// &lt;meta http-equiv="Content-Type" content="text/html; charset=utf-8"/&gt;
		/// </summary>
		private Encoding DetectEncoding(string contentTypeHeader, byte[] responseBytes)
		{
			// first try the header
			string s = Regex.Match(contentTypeHeader, 
				@"charset=([^;""'/>]+)", 
				RegexOptions.IgnoreCase).Groups[1].ToString().ToLower();

			// if we can't get it from header, try the body bytes forced to ASCII
			Encoding encoding = CharsetToEncoding(s);
			if (encoding == null)
			{
				s = Regex.Match(Encoding.ASCII.GetString(responseBytes), 
					@"<meta[^>]+content-type[^>]+charset=([^;""'/>]+)", 
					RegexOptions.IgnoreCase).Groups[1].ToString().ToLower();
				encoding = CharsetToEncoding(s);
				
				if (encoding == null)
					return _DefaultEncoding;
			}

			return encoding;
		}

		private void SaveResponseToFile(string filePath)
		{
			using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate))
			{
				BinaryWriter bw = new BinaryWriter(fs);
				bw.Write(_ResponseBytes);
				bw.Close();
			}
		}

        #endregion Private methods
	
        #region Nested class : ExtendedBinaryReader
		/// <summary>
		///   Extends the <see cref="System.IO.BinaryReader"/> class by a <see cref="ReadToEnd"/>
		///   method that can be used to read a whole file.
		/// </summary>
		/// <remarks>
		/// See http://dotnet.mvps.org/dotnet/faqs/?id=readfile&amp;lang=en
		/// </remarks>
		public class ExtendedBinaryReader : BinaryReader
		{
			/// <summary>
			///   Creates a new instance of the <c>ExtendedBinaryReader</c> class.
			/// </summary>
			/// <param name="Input">A stream.</param>
			public ExtendedBinaryReader(Stream Input) : base(Input)
			{
			}

			/// <summary>
			///   Creates a new instance of the <c>ExtendedBinaryReader</c> class.
			/// </summary>
			/// <param name="Input">The provided stream.</param>
			/// <param name="Encoding">The character encoding.</param>
			public ExtendedBinaryReader(Stream Input, Encoding Encoding) : base(Input, Encoding)
			{
			}

			/// <summary>
			///   Reads the whole data in the base stream and returns it in an
			///   array of bytes.
			/// </summary>
			/// <returns>The streams whole binary data.</returns>
			/// <remarks>This method use a buffer of 32k.</remarks>
			public byte[] ReadToEnd()
			{
				return this.ReadToEnd(short.MaxValue);
			}

			/// <summary>
			///   Reads the whole data in the base stream and returns it in an
			///   array of bytes.
			/// </summary>
			/// <param name="initialLength">The initial buffer length.</param>
			/// <returns>The stream's whole binary data.</returns>
			/// <remarks>
			/// Based on an implementation by Jon Skeet [MVP].
			/// See <see href="http://www.yoda.arachsys.com/csharp/readbinary.html"/>
			/// </remarks>
			public byte[] ReadToEnd(int initialLength)
			{
				// If an unhelpful initial length was passed, just use 32K.
				if (initialLength < 1)
					initialLength = short.MaxValue;
				
				byte[] buffer = new byte[initialLength];
				int read = 0;
				int chunk = BaseStream.Read(buffer, 0, buffer.Length);

				//				for (int chunk = this.BaseStream.Read(buffer, read, buffer.Length - read); chunk > 0; chunk = this.BaseStream.Read(buffer, read, buffer.Length - read))
				while (chunk > 0)
				{
					read += chunk;

					// If the end of the buffer is reached, check to see if there is
					// any more data.
					if (read == buffer.Length)
					{
						int nextByte = BaseStream.ReadByte();
						
						// If the end of the stream is reached, we are done.
						if (nextByte == -1)
							return buffer;
						
						// Nope.  Resize the buffer, put in the byte we have just
						// read, and continue.
						byte[] newBuffer = new byte[buffer.Length * 2];
						Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
						//						Array.Copy(buffer, newBuffer, buffer.Length);
						newBuffer[read] = (byte) nextByte;
						buffer = newBuffer;
						read++;
					}

					chunk = BaseStream.Read(buffer, read, buffer.Length - read);
				}

				// The buffer is now too big. Shrink it.
				byte[] returnBuffer = new byte[read];
				Buffer.BlockCopy(buffer, 0, returnBuffer, 0, read);
				//				Array.Copy(buffer, returnBuffer, read);
				return returnBuffer;
			}

		}
        #endregion Nested class : ExtendedBinaryReader
	}
}