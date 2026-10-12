
using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// MultipleStreamReader provides a logical single stream over multiple streams.  Only support ReadLine.
    /// </summary>
    public class MultipleStreamReader : TextReader
    {
        Queue _files;
        TextReader _sr = null;
        Dictionary<string, string> _prefetched;     // http(s) files downloaded by CreateAsync, by url
        // The user agent is set once here: DefaultRequestHeaders is not thread-safe, so adding it on every
        // fetch could corrupt the headers when readers run on several threads.
        static readonly HttpClient _httpClient = CreateHttpClient();

        static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.AddMajorsilenceReportingUserAgent();
            return client;
        }

        /// <summary>
        /// Constructor taking a path list string.  Files are separated with blanks.  
        /// If a file contains a blank then it should be enclosed with quotes (").
        /// </summary>
        /// <param name="pathlist"></param>
        public MultipleStreamReader(string pathlist)
        {
            GetFileList(pathlist);          // split the string into multiple files

            GetStream();             // get the first file
        }

        MultipleStreamReader(string pathlist, bool open)
        {
            GetFileList(pathlist);
        }

        /// <summary>
        /// Like the constructor, but downloads the http(s) entries up front with SendAsync so that
        /// reading lines never needs a synchronous HTTP request.
        /// </summary>
        internal static async Task<MultipleStreamReader> CreateAsync(string pathlist, CancellationToken cancellationToken)
        {
            var reader = new MultipleStreamReader(pathlist, false);
            foreach (string file in reader._files)
            {
                if (!RemoteContent.IsHttp(file))
                    continue;
                reader._prefetched ??= new Dictionary<string, string>();
                if (!reader._prefetched.ContainsKey(file))
                    reader._prefetched[file] = await RemoteContent.TryReadTextAsync(_httpClient, file, null, cancellationToken).ConfigureAwait(false);
            }

            reader.GetStream();             // get the first file
            return reader;
        }

        void GetFileList(string pathlist)
        {
            _files = new Queue();

            StringBuilder sb = null;

            int STARTFILE = 0;
            int INFILE = 1;
            int state = STARTFILE;
            bool bQuote = false;

            foreach (char c in pathlist)
            {
                if (state == STARTFILE)
                {
                    sb = new StringBuilder();
                    bQuote = (c == '"');

                    if (!bQuote)
                        sb.Append(c);
                    state = INFILE;
                }
                else if (state == INFILE)
                {
                    if ((bQuote && c == '"') || // quoted file and on last quote
(!bQuote && c == ' '))      // not quoted file and blank
                    {
                        AddFileToQueue(sb.ToString(), bQuote);

                        state = STARTFILE;
                    }
                    else
                        sb.Append(c);
                }
            }

            if (sb != null && sb.Length > 0)
                AddFileToQueue(sb.ToString(), bQuote);

            return;
        }

        void AddFileToQueue(string f, bool asis)
        {
            if (!asis)
                f = f.Trim();           // get rid of extraneous blanks

            if (f.Length <= 0)
                return;

            if (Path.GetFileNameWithoutExtension(f) == "*")
            {
                int i = f.LastIndexOf('*');
                string path = f.Substring(0, i);
                string[] fl = Directory.GetFiles(path, "*" + Path.GetExtension(f));
                foreach (string file in fl)
                    _files.Enqueue(file);
            }
            else
                _files.Enqueue(f);
        }

        // This class is a TextReader, whose ReadLine is synchronous, so the files are read synchronously
        // too (HttpClient.Send is a real synchronous call): no thread-pool hop or blocked pool thread.
        // Urls downloaded up front by CreateAsync are served from _prefetched instead.
        TextReader GetStream()
        {
            if (_sr != null)        // close out the previous file before getting another
            {
                _sr.Close();
                _sr = null;
            }

            if (_files == null || _files.Count == 0)
                return null;

            string fname = _files.Dequeue() as string;
            if (_prefetched != null && _prefetched.TryGetValue(fname, out string text))
            {
                _sr = new StringReader(text);
                return _sr;
            }

            Stream strm = null;

            if (fname.StartsWith("http:") ||
                fname.StartsWith("file:") ||
                fname.StartsWith("https:"))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, fname);
                HttpResponseMessage response = _httpClient.SendSync(request);
                response.EnsureSuccessStatusCode();
                strm = response.Content.ReadAsStream();
            }
            else
                strm = new FileStream(fname, System.IO.FileMode.Open, FileAccess.Read);

            _sr = new StreamReader(strm);

            return _sr;
        }

        public override void Close()
        {
            if (_sr != null)
            {
                _sr.Close();
                _sr = null;
            }

            _files.Clear();
        }

        public override string ReadLine()
        {
            // At the end of a file, carry on with the next one (an empty file is skipped).
            while (_sr != null)
            {
                string rs = _sr.ReadLine();
                if (rs != null)
                    return rs;

                GetStream();
            }

            return null;
        }

    }
}
