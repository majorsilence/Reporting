using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Majorsilence.Reporting.Data
{
    /// <summary>
    /// Reads the rows of an XML document one at a time with an <see cref="XmlReader"/>, so the whole
    /// document is never held in memory (an <c>XPathDocument</c> costs several times the size of the file).
    /// </summary>
    /// <remarks>
    /// This only handles what can be answered without XPath: a row path made of plain element names
    /// (<c>/rows/row</c>) and columns that are plain child element or attribute names. It follows XPath's
    /// rules for those: a name without a prefix matches only elements and attributes that are in no namespace,
    /// the value of an element is the text of all its descendants, and whitespace between elements is ignored.
    /// <see cref="XmlDataReader"/> uses the XPath implementation for everything else.
    /// </remarks>
    internal sealed class XmlRowStream : IDisposable
    {
        // a child element or attribute of a row
        internal readonly struct Node
        {
            public readonly string Name, Namespace, Prefix, Value;
            public Node(string name, string ns, string prefix, string value)
            {
                Name = name; Namespace = ns; Prefix = prefix; Value = value;
            }
        }

        static readonly Regex NamePath = new(@"^/?[A-Za-z_][A-Za-z0-9_.\-]*(/[A-Za-z_][A-Za-z0-9_.\-]*)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static readonly Regex ColumnName = new(@"^@?[A-Za-z_][A-Za-z0-9_.\-]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsSimpleRowPath(string xpath) => xpath != null && NamePath.IsMatch(xpath);
        public static bool IsSimpleColumn(string xpath) => xpath != null && ColumnName.IsMatch(xpath);

        readonly XmlReader _reader;
        readonly string[] _path;
        readonly bool[] _matched;
        bool _finished;

        // what to keep from each row; null means everything (used for the first row, which defines the columns)
        HashSet<string> _wantedElements;
        HashSet<string> _wantedAttributes;

        public List<Node> Elements { get; } = new();
        public List<Node> Attributes { get; } = new();

        XmlRowStream(XmlReader reader, string rowsXPath)
        {
            _reader = reader;
            _path = rowsXPath.TrimStart('/').Split('/');
            _matched = new bool[_path.Length];
        }

        /// <summary>Opens a local file or text for streaming; null when it cannot be streamed.</summary>
        public static XmlRowStream TryOpen(string url, string text, string rowsXPath)
        {
            if (!IsSimpleRowPath(rowsXPath))
                return null;

            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true,        // what XPathDocument does: only significant whitespace is kept
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = null,
                CloseInput = true,
            };

            if (text != null)
                return new XmlRowStream(XmlReader.Create(new StringReader(text), settings), rowsXPath);

            string path = LocalPath(url);
            if (path == null)
                return null;

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
            try
            {
                return new XmlRowStream(XmlReader.Create(stream, settings), rowsXPath);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        static string LocalPath(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsFile)
                    return null;
                url = uri.LocalPath;
            }
            else if (url.Contains("://") || url.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
                return null;

            return File.Exists(url) ? url : null;
        }

        /// <summary>After the first row, only the named children and attributes are kept.</summary>
        public void Restrict(IEnumerable<string> elements, IEnumerable<string> attributes)
        {
            _wantedElements = new HashSet<string>(elements, StringComparer.Ordinal);
            _wantedAttributes = new HashSet<string>(attributes, StringComparer.Ordinal);
        }

        /// <summary>Moves to the next row; the row's children and attributes are in <see cref="Elements"/> and <see cref="Attributes"/>.</summary>
        public bool MoveNext()
        {
            if (_finished)
                return false;

            if (_reader.ReadState == ReadState.Initial)
                _reader.Read();

            while (!_reader.EOF)
            {
                if (_reader.NodeType == XmlNodeType.Element)
                {
                    int d = _reader.Depth;
                    bool match = d < _path.Length &&
                                 (d == 0 || _matched[d - 1]) &&
                                 _reader.NamespaceURI.Length == 0 &&
                                 _reader.LocalName == _path[d];
                    if (d < _path.Length)
                        _matched[d] = match;

                    if (!match)
                    {
                        _reader.Skip();         // nothing below can be a row
                        continue;
                    }

                    if (d == _path.Length - 1)
                    {
                        ReadRow();
                        return true;
                    }
                }

                _reader.Read();
            }

            Dispose();
            return false;
        }

        // reads the row element the reader is on, and leaves the reader on the node after it
        void ReadRow()
        {
            Elements.Clear();
            Attributes.Clear();

            if (_reader.HasAttributes)
            {
                while (_reader.MoveToNextAttribute())
                {
                    // namespace declarations are not attributes as far as XPath is concerned
                    if (_reader.Prefix == "xmlns" || (_reader.Prefix.Length == 0 && _reader.LocalName == "xmlns"))
                        continue;
                    if (_wantedAttributes == null || _wantedAttributes.Contains(_reader.LocalName))
                        Attributes.Add(new Node(_reader.LocalName, _reader.NamespaceURI, _reader.Prefix, _reader.Value));
                }
                _reader.MoveToElement();
            }

            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return;
            }

            int depth = _reader.Depth;
            _reader.Read();
            while (!(_reader.NodeType == XmlNodeType.EndElement && _reader.Depth == depth) && !_reader.EOF)
            {
                if (_reader.NodeType == XmlNodeType.Element)
                {
                    if (_wantedElements == null || (_reader.NamespaceURI.Length == 0 && _wantedElements.Contains(_reader.LocalName)))
                    {
                        string name = _reader.LocalName, ns = _reader.NamespaceURI, prefix = _reader.Prefix;
                        Elements.Add(new Node(name, ns, prefix, ReadStringValue()));
                    }
                    else
                        _reader.Skip();
                }
                else
                    _reader.Read();
            }
            _reader.Read();         // past the row's end tag
        }

        // the XPath string value of the element the reader is on (the text of all its descendants); leaves the reader after it
        string ReadStringValue()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return "";
            }

            int depth = _reader.Depth;
            string single = null;
            StringBuilder sb = null;
            _reader.Read();
            while (!(_reader.NodeType == XmlNodeType.EndElement && _reader.Depth == depth) && !_reader.EOF)
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if (single == null) single = _reader.Value;
                        else (sb ??= new StringBuilder(single)).Append(_reader.Value);
                        break;
                }
                _reader.Read();
            }
            _reader.Read();         // past the end tag
            return sb != null ? sb.ToString() : single ?? "";
        }

        public static string Find(List<Node> nodes, string name)
        {
            foreach (var n in nodes)
            {
                if (n.Namespace.Length == 0 && n.Name == name)
                    return n.Value;
            }
            return null;
        }

        public void Dispose()
        {
            _finished = true;
            _reader.Dispose();
        }
    }
}
