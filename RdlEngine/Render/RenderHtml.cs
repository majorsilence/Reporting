

using System;
using Majorsilence.Reporting.Rdl;
using System.IO;
using System.Collections;
using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives
using Majorsilence.Forms.Drawing.Imaging;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Threading.Tasks;

namespace Majorsilence.Reporting.Rdl
{
	
	///<summary>
	/// Renders a report to HTML.   This handles some page formating but does not do true page formatting.
	///</summary>
	internal class RenderHtml: IPresent
	{
		Report r;					// report
		StringWriter tw;			// temporary location where the output is going
		IStreamGen _sg;				// stream generater
		Hashtable _styles;			// hash table of styles we've generated
		int cssId=1;				// ID for css when names are usable or available
		bool bScriptToggle=false;	// need to generate toggle javascript in header
		bool bScriptTableSort=false; // need to generate table sort javascript in header
		Bitmap _bm=null;			// bm and
		Graphics _g=null;			//		  g are needed when calculating string heights
		bool _Asp=false;			// denotes ASP.NET compatible HTML; e.g. no <html>, <body>
									//    separate JavaScript and CSS
		string _Prefix="";			// prefix to generating all HTML names (e.g. css, ...
		string _CSS;				// when ASP we put the CSS into a string
		string _JavaScript;			//     as well as any required javascript
		int _SkipMatrixCols=0;		// # of matrix columns to skip

		public RenderHtml(Report rep, IStreamGen sg)
		{
			r = rep;
			_sg = sg;					// We need this in future

			tw = new StringWriter();	// will hold the bulk of the HTML until we generate
			// final file
			_styles = new Hashtable();
		}
        public void Dispose() 
		{
			// These should already be cleaned up; but in case of an unexpected error 
			//   these still need to be disposed of
			if (_bm != null)
				_bm.Dispose();
			if (_g != null)
				_g.Dispose();
		}

		public Report Report()
		{
			return r;
		}

		public bool Asp
		{
			get {return _Asp;}
			set {_Asp = value;}
		}

		public string JavaScript
		{
			get {return _JavaScript;}
		}

		public string CSS
		{
			get {return _CSS;}
		}

		public string Prefix
		{
			get {return _Prefix;}
			set 
			{
				_Prefix = value==null? "": value;
				if (_Prefix.Length > 0 &&
					_Prefix[0] == '_')
					_Prefix = "a" + _Prefix;	// not perfect but underscores as first letter don't work
			}
		}

		public bool IsPagingNeeded()
		{
			return false;
		}

		public void Start()		
		{
			return;
		}

		string FixupRelativeName(string relativeName)
		{
			if (_sg is OneFileStreamGen)
			{
				if (relativeName[0] == Path.DirectorySeparatorChar || relativeName[0] == Path.AltDirectorySeparatorChar)
					relativeName = relativeName.Substring(1);
			}
            else if (!(relativeName[0] == Path.DirectorySeparatorChar || relativeName[0] == Path.AltDirectorySeparatorChar))
            {
                if (_Asp)
                    relativeName = "/" + relativeName;      // Hack for Firefox
                else
                    relativeName = Path.DirectorySeparatorChar + relativeName;
            }
			return relativeName;
		}
		// puts the JavaScript into the header
		private void ScriptGenerate(TextWriter ftw)
		{
			if (bScriptToggle || bScriptTableSort)
			{
				ftw.WriteLine("<script language=\"javascript\">");
			}

			if (bScriptToggle)
			{
				ftw.WriteLine("var dname='';");
				ftw.WriteLine(@"function hideShow(node, hideCount, showID) {
if (navigator.appName.toLowerCase().indexOf('netscape') > -1) 
	dname = 'table-row';
else
	dname = 'block';

 var tNode;
 for (var ci=0;ci<node.childNodes.length;ci++) {
     if (node.childNodes[ci].tagName && node.childNodes[ci].tagName.toLowerCase() == 'img') tNode = node.childNodes[ci];
 }
	var rows = findObject(showID);
	if (rows[0].style.display == dname) {hideRows(rows, hideCount); tNode.src='plus.gif';}
	else {
	   tNode.src='minus.gif';
	   for (var i = 0; i < rows.length; i++) {
		  rows[i].style.display = dname;
	   }
	}
}
function hideRows(rows, count) {
    var row;
	if (navigator.appName.toLowerCase().indexOf('netscape') > -1) 
	{
		for (var r=0; r < rows.length; r++) {
			row = rows[r];
			row.style.display = 'none';
			var imgs = row.getElementsByTagName('img');
			for (var ci=0;ci<imgs.length;ci++) {
				if (imgs[ci].className == 'toggle') {
					imgs[ci].src='plus.gif';
				}
			}
		}
	return;
	}
 if (rows.tagName == 'TR')
    row = rows;
 else
    row = rows[0];   	
 while (count > 0) {
   row.style.display = 'none';
   var imgs = row.getElementsByTagName('img');
   for (var ci=0;ci<imgs.length;ci++) {
      if (imgs[ci].className == 'toggle') {
		     imgs[ci].src='plus.gif';
      }
   }
   row = row.nextSibling;
   count--;
 }
}
function findObject(id) {
	if (navigator.appName.toLowerCase().indexOf('netscape') > -1) 
	{
	   var a = new Array();
	   var count=0;
	   for (var i=0; i < document.all.length; i++)
	   {
	      if (document.all[i].id == id)
			a[count++] = document.all[i];
	   }
		return a;
	} 
	else 
	{
	    var o = document.all[id];
		if (o.tagName == 'TR')
		{
		   var a = new Array();
		   a[0] = o;
		   return a;
		}
		return o;
	} 
}
");
 
			}

			if (bScriptTableSort)
			{
				ftw.WriteLine("var SORT_INDEX;");
				ftw.WriteLine("var SORT_DIR;");

				ftw.WriteLine("function sort_getInnerText(element) {");
				ftw.WriteLine("	if (typeof element == 'string') return element;");
				ftw.WriteLine("	if (typeof element == 'undefined') return element;");
				ftw.WriteLine("	if (element.innerText) return element.innerText;");
				ftw.WriteLine("	var s = '';");
	
				ftw.WriteLine("	var cn = element.childNodes;");
				ftw.WriteLine("	for (var i = 0; i < cn.length; i++) {");
				ftw.WriteLine("		switch (cn[i].nodeType) {");
				ftw.WriteLine("			case 1:");		// element node
				ftw.WriteLine("				s += sort_getInnerText(cn[i]);");
				ftw.WriteLine("				break;");
				ftw.WriteLine("			case 3:");		// text node
				ftw.WriteLine("				s += cn[i].nodeValue;");
				ftw.WriteLine("				break;");
				ftw.WriteLine("		}");
				ftw.WriteLine("	}");
				ftw.WriteLine("	return s;");
				ftw.WriteLine("}");

				ftw.WriteLine("function sort_table(node, sortfn, header_rows, footer_rows) {");
				ftw.WriteLine("    var arrowNode;");	// arrow node
				ftw.WriteLine("    for (var ci=0;ci<node.childNodes.length;ci++) {");
				ftw.WriteLine("        if (node.childNodes[ci].tagName && node.childNodes[ci].tagName.toLowerCase() == 'span') arrowNode = node.childNodes[ci];");
				ftw.WriteLine("    }");

				ftw.WriteLine("    var td = node.parentNode;");
				ftw.WriteLine("    SORT_INDEX = td.cellIndex;");	// need to remember SORT_INDEX in compare function
				ftw.WriteLine("    var table = sort_getTable(td);");

				ftw.WriteLine("    var sortnext;");
				ftw.WriteLine("    if (arrowNode.getAttribute('sortdir') == 'down') {");
				ftw.WriteLine("        arrow = '&nbsp;&nbsp;&uarr;';");
				ftw.WriteLine("        SORT_DIR = -1;");			// descending SORT_DIR in compare function
				ftw.WriteLine("        sortnext = 'up';");
				ftw.WriteLine("    } else {");
				ftw.WriteLine("        arrow = '&nbsp;&nbsp;&darr;';");
				ftw.WriteLine("        SORT_DIR = 1;");				// ascending SORT_DIR in compare function
				ftw.WriteLine("        sortnext = 'down';");
				ftw.WriteLine("    }");
    
				ftw.WriteLine("    var newRows = new Array();");
				ftw.WriteLine("    for (j=header_rows;j<table.rows.length-footer_rows;j++) { newRows[j-header_rows] = table.rows[j]; }");

				ftw.WriteLine("    newRows.sort(sortfn);");

				// We appendChild rows that already exist to the tbody, so it moves them rather than creating new ones
				ftw.WriteLine("    for (i=0;i<newRows.length;i++) {table.tBodies[0].appendChild(newRows[i]);}");
    
				// Reset all arrows and directions for next time
				ftw.WriteLine("    var spans = document.getElementsByTagName('span');");
				ftw.WriteLine("    for (var ci=0;ci<spans.length;ci++) {");
				ftw.WriteLine("        if (spans[ci].className == 'sortarrow') {");
				// in the same table as us?
				ftw.WriteLine("            if (sort_getTable(spans[ci]) == sort_getTable(node)) {");
				ftw.WriteLine("                spans[ci].innerHTML = '&nbsp;&nbsp;&nbsp;';");
				ftw.WriteLine("                spans[ci].setAttribute('sortdir','up');");
				ftw.WriteLine("            }");
				ftw.WriteLine("        }");
				ftw.WriteLine("    }");
        
				ftw.WriteLine("    arrowNode.innerHTML = arrow;");
				ftw.WriteLine("    arrowNode.setAttribute('sortdir',sortnext);");
				ftw.WriteLine("}");

				ftw.WriteLine("function sort_getTable(el) {");
				ftw.WriteLine("	if (el == null) return null;");
				ftw.WriteLine("	else if (el.nodeType == 1 && el.tagName.toLowerCase() == 'table')");
				ftw.WriteLine("		return el;");
				ftw.WriteLine("	else");
				ftw.WriteLine("		return sort_getTable(el.parentNode);");
				ftw.WriteLine("}");

				ftw.WriteLine("function sort_cmp_date(c1,c2) {");
				ftw.WriteLine("    t1 = sort_getInnerText(c1.cells[SORT_INDEX]);");
				ftw.WriteLine("    t2 = sort_getInnerText(c2.cells[SORT_INDEX]);");
				ftw.WriteLine("    dt1 = new Date(t1);");
				ftw.WriteLine("    dt2 = new Date(t2);");
				ftw.WriteLine("    if (dt1==dt2) return 0;");
				ftw.WriteLine("    if (dt1<dt2) return -SORT_DIR;");
				ftw.WriteLine("    return SORT_DIR;");
				ftw.WriteLine("}");

				// numeric - removes any extraneous formating characters before parsing
				ftw.WriteLine("function sort_cmp_number(c1,c2) {"); 
				ftw.WriteLine("    t1 = sort_getInnerText(c1.cells[SORT_INDEX]).replace(/[^0-9.]/g,'');");
				ftw.WriteLine("    t2 = sort_getInnerText(c2.cells[SORT_INDEX]).replace(/[^0-9.]/g,'');");
				ftw.WriteLine("    n1 = parseFloat(t1);");
				ftw.WriteLine("    n2 = parseFloat(t2);");
				ftw.WriteLine("    if (isNaN(n1)) n1 = Number.MAX_VALUE");
				ftw.WriteLine("    if (isNaN(n2)) n2 = Number.MAX_VALUE");
				ftw.WriteLine("    return (n1 - n2)*SORT_DIR;");
				ftw.WriteLine("}");

				// For string we first do a case insensitive comparison;
				//   when equal we then do a case sensitive comparison
				ftw.WriteLine("function sort_cmp_string(c1,c2) {");
				ftw.WriteLine("    t1 = sort_getInnerText(c1.cells[SORT_INDEX]).toLowerCase();");
				ftw.WriteLine("    t2 = sort_getInnerText(c2.cells[SORT_INDEX]).toLowerCase();");
				ftw.WriteLine("    if (t1==t2) return sort_cmp_casesensitive(c1,c2);");
				ftw.WriteLine("    if (t1<t2) return -SORT_DIR;");
				ftw.WriteLine("    return SORT_DIR;");
				ftw.WriteLine("}");

				ftw.WriteLine("function sort_cmp_casesensitive(c1,c2) {");
				ftw.WriteLine("    t1 = sort_getInnerText(c1.cells[SORT_INDEX]);");
				ftw.WriteLine("    t2 = sort_getInnerText(c2.cells[SORT_INDEX]);");
				ftw.WriteLine("    if (t1==t2) return 0;");
				ftw.WriteLine("    if (t2<t2) return -SORT_DIR;");
				ftw.WriteLine("    return SORT_DIR;");
				ftw.WriteLine("}");
			}

			if (bScriptToggle || bScriptTableSort)
			{
				ftw.WriteLine("</script>");
			}

			return;
		}

		// handle the Action tag
		private async Task<string> Action(Action a, Row r, string t, string tooltip)
		{
			if (a == null)
				return t;
			
			string result = t;
			if (a.Hyperlink != null)
			{	// Handle a hyperlink
				string url = await a.HyperLinkValue(this.r, r);
				if (tooltip == null)
					result = String.Format("<a target=\"_top\" href=\"{0}\">{1}</a>", url, t);
				else
					result = String.Format("<a target=\"_top\" href=\"{0}\" title=\"{1}\">{2}</a>", url, tooltip, t);
			}
			else if (a.Drill != null)
			{	// Handle a drill through
				StringBuilder args= new StringBuilder("<a target=\"_top\" href=\"");
				if (_Asp)	// for ASP we go thru the default page and pass it as an argument
					args.Append("Default.aspx?rs:url=");
				args.Append(a.Drill.ReportName);
				args.Append(".rdl");
				if (a.Drill.DrillthroughParameters != null)
				{
					bool bFirst = !_Asp;		// ASP already have an argument
					foreach (DrillthroughParameter dtp in a.Drill.DrillthroughParameters.Items)
					{
						if (!await dtp.OmitValue(this.r, r))
						{
							if (bFirst)
							{	// First parameter - prefixed by '?'
								args.Append('?');
								bFirst = false;
							}
							else
							{	// Subsequant parameters - prefixed by '&'
								args.Append('&');
							}
							args.Append(dtp.Name.Nm);
							args.Append('=');
							args.Append(dtp.ValueValue(this.r, r));
						}
					}
				}
				args.Append('"');
				if (tooltip != null)
					args.Append(String.Format(" title=\"{0}\"", tooltip));
				args.Append(">");
				args.Append(t);
				args.Append("</a>");
				result = args.ToString();
			}
			else if (a.BookmarkLink != null)
			{	// Handle a bookmark
				string bm = await a.BookmarkLinkValue(this.r, r);
				if (tooltip == null)
					result = String.Format("<a href=\"#{0}\">{1}</a>", bm, t);
				else
					result = String.Format("<a href=\"#{0}\" title=\"{1}\">{2}</a>", bm, tooltip, t);
			}

			return result;
		}

		private string Bookmark(string bm, string t)
		{
			if (bm == null)
				return t;

			return String.Format("<div id=\"{0}\">{1}</div>", bm, t);
		}

		// Generate the CSS styles and put them in the header
		private void CssGenerate(TextWriter ftw)
		{
			if (_styles.Count <= 0)
				return;

			if (!_Asp)
				ftw.WriteLine("<style type='text/css'>");

			// Browsers default <th> to center-aligned text, but the RDL's "General" alignment
			// (used whenever a cell has no explicit TextAlign, header or not) means left for
			// text - which is how the PDF renderer draws it and how <td> already behaves with
			// no CSS at all. Cells with an explicit TextAlign still win: an id-qualified rule
			// like "th#cssN {text-align:Center;}" is more specific than this bare-element rule.
			ftw.WriteLine("th {text-align: left;}");

			// Browsers also default <body> to a non-zero margin, which offsets every item
			// positioned via "position: absolute; left: 0; top: 0;" away from the page edge.
			// The PDF renderer has no such default, so a report item designed to bleed to the
			// edge of the page (e.g. Top/Left 0 with Width equal to PageWidth) renders inset
			// from the edge in HTML unless that default margin is reset.
			ftw.WriteLine("body {margin: 0; padding: 0;}");

			// The entire report body is wrapped in a plain, unstyled <table> purely so its
			// single cell can host the "position: relative;" div that every report item is
			// absolutely positioned against. Browsers default <table> to a 2px border-spacing
			// and <td>/<th> to a small default padding, both of which shift that div (and so
			// every item positioned relative to it) away from the page edge just like the
			// <body> margin did. Explicit per-cell padding from the RDL (always emitted, even
			// as "0pt" - see Style.GetCSS) still overrides this default.
			ftw.WriteLine("table {border-spacing: 0;} td, th {padding: 0; margin: 0;}");

			foreach (CssCacheEntry cce in _styles.Values)
			{
				int i = cce.Css.IndexOf('{');
				if (cce.Name.IndexOf('#') >= 0)
					ftw.WriteLine("{0} {1}", cce.Name, cce.Css.Substring(i));
				else
					ftw.WriteLine(".{0} {1}", cce.Name, cce.Css.Substring(i));
			}

			if (!_Asp)
				ftw.WriteLine("</style>");
		}

		private async Task<string> CssAdd(Style s, ReportLink rl, Row row)
		{
			return await CssAdd(s, rl, row, false, float.MinValue, float.MinValue);
		}
		
		private async Task<string> CssAdd(Style s, ReportLink rl, Row row, bool bForceRelative)
		{
			return await CssAdd(s, rl, row, bForceRelative, float.MinValue, float.MinValue);
		}
		private async Task<string> CssAdd(Style s, ReportLink rl, Row row, bool bForceRelative, float h, float w)
		{
			string css;
			string prefix = CssPrefix(s, rl);
			if (_Asp && prefix == "table#")
				bForceRelative = true;

			string position = await CssPosition(rl, row, bForceRelative, h, w);
			if (s != null)
				css = prefix + "{" + position + await s.GetCSS(this.r, row, true) + "}";
			else if (rl is Table || rl is Matrix)
				css = prefix + "{" + position + "border-collapse:collapse;}";
			else
				css = prefix + "{" + position + "}";

			CssCacheEntry cce = (CssCacheEntry) _styles[css];
			if (cce == null)
			{
				string name = prefix + this.Prefix + "css" + cssId++.ToString();
				cce = new CssCacheEntry(css, name);
				_styles.Add(cce.Css, cce);
			}

			int i = cce.Name.IndexOf('#');
			if (i > 0)
				return cce.Name.Substring(i+1);
			else
				return cce.Name;
		}

		private async Task<string> CssPosition(ReportLink rl,Row row, bool bForceRelative, float h, float w)
		{
			if (!(rl is ReportItem))		// if not a report item then no position
				return "";

			// no positioning within a table
			for (ReportLink p=rl.Parent; p != null; p=p.Parent)
			{
				if (p is TableCell)
					return "";
				if (p is RowGrouping ||
					p is MatrixCell ||
					p is ColumnGrouping ||
					p is Corner)
				{
					StringBuilder sb2 = new StringBuilder();
					if (h != float.MinValue)
						sb2.AppendFormat(NumberFormatInfo.InvariantInfo, "height: {0}pt; ", h);
					if (w != float.MinValue)
						sb2.AppendFormat(NumberFormatInfo.InvariantInfo, "width: {0}pt; ", w);
					return sb2.ToString();
				}
			}

			// TODO: optimize by putting this into ReportItem and caching result???
			ReportItem ri = (ReportItem) rl;

			StringBuilder sb = new StringBuilder();

			if (ri.Left != null && ri.Left.Size > 0)
			{
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "left: {0}; ", ri.Left.CSS);
			}
            if (ri is Matrix || ri is Image || ri is Chart)
            { }
            else 
            {
				if (ri.Width != null)
					sb.AppendFormat(NumberFormatInfo.InvariantInfo, "width: {0}; ", ri.Width.CSS);
			}
            if (ri.Top != null && ri.Top.Size > 0)
			{
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "top: {0}pt; ", ri.Gap(this.r));
			}
			if (ri is List)
			{
				List l = ri as List;
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "height: {0}pt; ", await l.HeightOfList(this.r, GetGraphics,row));
			}
			else if (ri is Matrix || ri is Table || ri is Image || ri is Chart)
			{}
			else if (ri.Height != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "height: {0}; ", ri.Height.CSS);

			if (sb.Length > 0)
			{
				if (bForceRelative)
					sb.Insert(0, "position: relative; ");
				else
					sb.Insert(0, "position: absolute; ");
			}

			return sb.ToString();
		}

		private Graphics GetGraphics
		{
			get 
			{
				if (_g == null)
				{
					_bm = new Bitmap(10, 10);
					_g = Graphics.FromImage(_bm);
				}
				return _g;
			}
		}

		private string CssPrefix(Style s, ReportLink rl)
		{
			string cssPrefix=null;
			ReportLink p;

			if (rl is Table || rl is Matrix || rl is Rectangle)
			{
				cssPrefix = "table#";
			}
			else if (rl is Body)
			{
				cssPrefix = "body#";
			}
			else if (rl is Line)
			{
				cssPrefix = "table#";
			}
			else if (rl is List)
			{
				cssPrefix = "";
			}
			else if (rl is Subreport)
			{
				cssPrefix = "";
			}
			else if (rl is Chart)
			{	
				cssPrefix = "";
			}
			if (cssPrefix != null)
				return cssPrefix;

			// now find what the style applies to
			for (p=rl.Parent; p != null; p=p.Parent)
			{
				if (p is TableCell)
				{
					bool bHead = false;
					ReportLink p2;
					for (p2=p.Parent; p2 != null; p2=p2.Parent)
					{
						Type t2 = p2.GetType();
						if (t2 == typeof(Header))
						{
							if (p2.Parent is Table)
								bHead=true;
							break;
						}
					}
					if (bHead)
						cssPrefix = "th#";
					else
						cssPrefix = "td#";
					break;
				}
				else if (p is RowGrouping ||
					p is MatrixCell ||
					p is ColumnGrouping ||
					p is Corner)
				{
					cssPrefix = "td#";
					break;
				}
			}

			return cssPrefix == null? "": cssPrefix;
		}

		public async Task End()
		{
			string bodyCssId;
			if (r.ReportDefinition.Body != null)
				bodyCssId = await CssAdd(r.ReportDefinition.Body.Style, r.ReportDefinition.Body, null);		// add the style for the body
			else
				bodyCssId = null;

			TextWriter ftw = _sg.GetTextWriter();	// the final text writer location

			if (_Asp)
			{
				// do any required JavaScript
				StringWriter sw = new StringWriter();
				ScriptGenerate(sw);
				_JavaScript = sw.ToString();
				sw.Close();
				// do any required CSS
				sw = new StringWriter();
				CssGenerate(sw);
				_CSS = sw.ToString();
				sw.Close();
			}
			else
            {
                ftw.WriteLine("<!DOCTYPE html>");
				ftw.WriteLine("<html>");

				// handle the <head>: description, javascript and CSS goes here
				ftw.WriteLine("<head>");
                ftw.WriteLine("<meta charset=\"utf-8\">");

				ScriptGenerate(ftw);
				CssGenerate(ftw);

				if (r.Description != null)	// Use description as title if provided
					ftw.WriteLine(string.Format(@"<title>{0}</title>", XmlUtil.XmlAnsi(r.Description)));

				ftw.WriteLine(@"</head>");
			}

			// Always want an HTML body - even if report doesn't have a body stmt
			if (this._Asp)
			{
				ftw.WriteLine("<table style=\"position: relative;\">");
			}
			else if (bodyCssId != null)
				ftw.WriteLine(@"<body id='{0}'><table>", bodyCssId);
			else
				ftw.WriteLine("<body><table>");

			ftw.Write(tw.ToString());

			if (this._Asp)
				ftw.WriteLine(@"</table>");
			else
				ftw.WriteLine(@"</table></body></html>");

			if (_g != null)
			{
				_g.Dispose();
				_g = null;
			}
			if (_bm != null)
			{
				_bm.Dispose();
				_bm = null;
			}
			return;
		}

		// Body: main container for the report
		public void BodyStart(Body b)
		{
			if (b.ReportItems != null && b.ReportItems.Items.Count > 0)
				tw.WriteLine("<tr><td><div style=\"POSITION: relative; \">");
		}

		public void BodyEnd(Body b)
		{
			if (b.ReportItems != null && b.ReportItems.Items.Count > 0)
				tw.WriteLine("</div></td></tr>");
		}
		
		public void PageHeaderStart(PageHeader ph)
		{
			if (ph.ReportItems != null && ph.ReportItems.Items.Count > 0)
				tw.WriteLine("<tr><td><div style=\"overflow: clip; POSITION: relative; HEIGHT: {0};\">", ph.Height.CSS);
		}

		public void PageHeaderEnd(PageHeader ph)
		{
			if (ph.ReportItems != null && ph.ReportItems.Items.Count > 0)
				tw.WriteLine("</div></td></tr>");
		}
		
		public void PageFooterStart(PageFooter pf)
		{
			if (pf.ReportItems != null && pf.ReportItems.Items.Count > 0)
				tw.WriteLine("<tr><td><div style=\"overflow: clip; POSITION: relative; HEIGHT: {0};\">", pf.Height.CSS);
		}

		public void PageFooterEnd(PageFooter pf)
		{
			if (pf.ReportItems != null && pf.ReportItems.Items.Count > 0)
				tw.WriteLine("</div></td></tr>");
		}

		public async Task Textbox(Textbox tb, string t, Row row)
		{
            if (await tb.IsHtml(this.r, row))		// we leave the text as is (except to handle unicode) when request is to treat as html
            {                               //   this can screw up the generated HTML if not properly formed HTML
                t = XmlUtil.HtmlAnsi(t);
            }
            else
            {									
				// make all the characters browser readable
				t = XmlUtil.XmlAnsi(t);
				t = t.Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>");

				// handle any specified bookmark
				t = Bookmark(await tb.BookmarkValue(this.r, row), t);

				// handle any specified actions
				t = await Action(tb.Action, row, t, await tb.ToolTipValue(this.r, row));
			}
			// determine if we're in a tablecell
			Type tp = tb.Parent.Parent.GetType();
			bool bCell;
			if (tp == typeof(TableCell) ||
				tp == typeof(Corner) ||
				tp == typeof(DynamicColumns) ||
				tp == typeof(DynamicRows) ||
				tp == typeof(StaticRow) ||
				tp == typeof(StaticColumn) ||
				tp == typeof(Subtotal) ||
				tp == typeof(MatrixCell))
				bCell = true;
			else
				bCell = false;

			if (tp == typeof(Rectangle))
				tw.Write("<td>");

			if (bCell)
			{	// The cell has the formatting for this text
				if (t == "")
					tw.Write("<br />");		// must have something in cell for formating
				else
					tw.Write(t);
			}
			else
			{	// Formatting must be specified
				string cssName = await CssAdd(tb.Style, tb, row);	// get the style name for this item

				tw.Write("<div class='{0}'>{1}</div>", cssName, t);
			}

			if (tp == typeof(Rectangle))
				tw.Write("</td>");
		}

		public async Task DataRegionNoRows(DataRegion d, string noRowsMsg)			// no rows in table
		{
			if (noRowsMsg == null)
				noRowsMsg = "";

			bool bTableCell = d.Parent.Parent.GetType() == typeof(TableCell);

			if (bTableCell)
			{
				if (noRowsMsg == "")
					tw.Write("<br />");
				else
					tw.Write(noRowsMsg);
			}
			else
			{
				string cssName = await CssAdd(d.Style, d, null);	// get the style name for this item
				tw.Write("<div class='{0}'>{1}</div>", cssName, noRowsMsg);
			}
			return;
		}

		// Lists
		public async Task<bool> ListStart(List l, Row r)
		{
			// identifiy reportitem it if necessary
			string bookmark = await l.BookmarkValue(this.r, r);
			if (bookmark != null)	// 
				tw.WriteLine("<div id=\"{0}\">", bookmark);		// can't use the table id since we're using for css style
			return true;
		}

		public async Task ListEnd(List l, Row r)
		{
			string bookmark = await l.BookmarkValue(this.r, r);
			if (bookmark != null)
				tw.WriteLine("</div>"); 
		}

		public async Task ListEntryBegin(List l, Row r)
		{
			string cssName = await CssAdd(l.Style, l, r, true);	// get the style name for this item; force to be relative
			tw.WriteLine();
			tw.WriteLine("<div class={0}>", cssName);
		}

		public void ListEntryEnd(List l, Row r)
		{
			tw.WriteLine();
			tw.WriteLine("</div>");
		}

		// Tables					// Report item table
		public async Task<bool> TableStart(Table t, Row row)
		{
			string cssName = await CssAdd(t.Style, t, row);	// get the style name for this item

			// Determine if report custom defn want this table to be sortable
			if (IsTableSortable(t))
			{
				this.bScriptTableSort = true;
			}

			string bookmark = await t.BookmarkValue(this.r, row);
			if (bookmark != null)
				tw.WriteLine("<div id=\"{0}\">", bookmark);		// can't use the table id since we're using for css style

			// Calculate the width of all the columns
			int width = await t.WidthInPixels(this.r, row);
			if (width <= 0)
				tw.WriteLine("<table id='{0}'>", cssName);
			else
				tw.WriteLine("<table id='{0}' width={1}>", cssName, width);

			return true;
		}

		public bool IsTableSortable(Table t)
		{
			if (t.TableGroups != null || t.Details == null || 
				t.Details.TableRows == null || t.Details.TableRows.Items.Count != 1)		
				return false;	// can't have tableGroups; must have 1 detail row

			// Determine if report custom defn want this table to be sortable
			bool bReturn = false;
			if (t.Custom != null)
			{
				// Loop thru all the child nodes
				foreach(XmlNode xNodeLoop in t.Custom.CustomXmlNode.ChildNodes)
				{
					if (xNodeLoop.Name == "HTML")
					{
						if (xNodeLoop.LastChild.InnerText.ToLower() == "true")
						{
							bReturn = true;
						}
						break;
					}
				}
			}
			return bReturn;
		}

		public async Task TableEnd(Table t, Row row)
		{
			string bookmark = await t.BookmarkValue(this.r, row);
			if (bookmark != null)
				tw.WriteLine("</div>"); 
			tw.WriteLine("</table>");
			return;
		}
 
		public void TableBodyStart(Table t, Row row)
		{
			tw.WriteLine("<tbody>");
		}

		public void TableBodyEnd(Table t, Row row)
		{
			tw.WriteLine("</tbody>");
		}

		public void TableFooterStart(Footer f, Row row)
		{
			tw.WriteLine("<tfoot>");
		}

		public void TableFooterEnd(Footer f, Row row)
		{
			tw.WriteLine("</tfoot>");
		}

		public void TableHeaderStart(Header h, Row row)
		{
			tw.WriteLine("<thead>");
		}

		public void TableHeaderEnd(Header h, Row row)
		{
			tw.WriteLine("</thead>");
		}

		public async Task TableRowStart(TableRow tr, Row row)
		{
			tw.Write("\t<tr");
			ReportLink rl = tr.Parent.Parent;
			Visibility v=null;
			Textbox togText=null;		// holds the toggle text box if any
			if (rl is Details)
			{
				Details d = (Details) rl;
				v = d.Visibility;
				togText = d.ToggleTextbox;
			}
			else if (rl.Parent is TableGroup)
			{
				TableGroup tg = (TableGroup) rl.Parent;
				v = tg.Visibility;
				togText = tg.ToggleTextbox;
			}

			StringBuilder rowStyle = new StringBuilder();
			if (tr.Height != null)
				rowStyle.AppendFormat(NumberFormatInfo.InvariantInfo, "height: {0};", tr.Height.CSS);

			if (v != null &&
				v.Hidden != null)
			{
				bool bHide = await v.Hidden.EvaluateBoolean(this.r, row);
				if (bHide)
					rowStyle.Append("display:none;");
			}

			if (rowStyle.Length > 0)
				tw.Write(" style=\"{0}\"", rowStyle.ToString());

			if (togText != null && togText.Name != null)
			{
				string name = togText.Name.Nm + "_" + togText.RunCount(this.r).ToString();
				tw.Write(" id='{0}'", name);
			}

			tw.Write(">");
		}

		public void TableRowEnd(TableRow tr, Row row)
		{
			tw.WriteLine("</tr>");
		}

		public async Task TableCellStart(TableCell t, Row row)
		{
			string cellType = t.InTableHeader? "th": "td";

			ReportItem r = t.ReportItems.Items[0];

			string cssName = await CssAdd(r.Style, r, row);	// get the style name for this item

			tw.Write("<{0} id='{1}'", cellType, cssName);

			// calculate width of column
			if (t.InTableHeader && t.OwnerTable.TableColumns != null)
			{
				// Calculate the width across all the spanned columns
				int width = 0;
				for (int ci=t.ColIndex; ci < t.ColIndex + t.ColSpan; ci++)
				{
					TableColumn tc = t.OwnerTable.TableColumns.Items[ci] as TableColumn;
					if (tc != null && tc.Width != null)
						width += tc.Width.PixelsX;
				}
				if (width > 0)
					tw.Write(" width={0}", width);
			}

			if (t.ColSpan > 1)
				tw.Write(" colspan={0}", t.ColSpan);

			Textbox tb = r as Textbox;
			if (tb != null &&				// have textbox
				tb.IsToggle &&				//   and its a toggle
				tb.Name != null)			//   and need name as well
			{
				int groupNestCount = t.OwnerTable.GetGroupNestCount(this.r);
				if (groupNestCount > 0) // anything to toggle?
				{
					string name = tb.Name.Nm + "_" + (tb.RunCount(this.r)+1).ToString();
					bScriptToggle = true;

					// need both hand and pointer because IE and Firefox use different names
					tw.Write(" onClick=\"hideShow(this, {0}, '{1}')\" onMouseOver=\"style.cursor ='hand';style.cursor ='pointer'\">", groupNestCount, name);
                    tw.Write("<img class='toggle' src=\"plus.gif\" align=\"top\"/>");
				}
				else
                    tw.Write("<img src=\"empty.gif\" align=\"top\"/>");
			}
			else
				tw.Write(">");

			if (t.InTableHeader)
			{	
				// put the second half of the sort tags for the column; if needed
				// first half ---- <a href="#" onclick="sort_table(this,sort_cmp_string,1,0);return false;">
				// next half follows text  ---- <span class="sortarrow">&nbsp;&nbsp;&nbsp;</span></a></th>

				string sortcmp = SortType(t, tb);	// obtain the sort type
				if (sortcmp != null)				// null if sort not needed
				{
					int headerRows, footerRows;
					headerRows = t.OwnerTable.Header.TableRows.Items.Count;	// since we're in header we know we have some rows
					if (t.OwnerTable.Footer != null &&
						t.OwnerTable.Footer.TableRows != null)
						footerRows = t.OwnerTable.Footer.TableRows.Items.Count;
					else
						footerRows = 0;
					tw.Write("<a href=\"#\" title='Sort' onclick=\"sort_table(this,{0},{1},{2});return false;\">",sortcmp, headerRows, footerRows);
				}
			}

			return;
		}

		private string SortType(TableCell tc, Textbox tb)
		{
			// return of null means don't sort
			if (tb == null || !IsTableSortable(tc.OwnerTable))
				return null;

			// default is true if table is sortable;
			//   but user may place override on Textbox custom tag
			if (tb.Custom != null)
			{
				// Loop thru all the child nodes
				foreach(XmlNode xNodeLoop in tb.Custom.CustomXmlNode.ChildNodes)
				{
					if (xNodeLoop.Name == "HTML")
					{
						if (xNodeLoop.LastChild.InnerText.ToLower() == "false")
						{
							return null;
						}
						break;
					}
				}
			}

			// Must find out the type of the detail column
			Details d = tc.OwnerTable.Details;
			if (d == null)
				return null;
			TableRow tr = d.TableRows.Items[0] as TableRow;
			if (tr == null)
				return null;
			TableCell dtc = tr.TableCells.Items[tc.ColIndex] as TableCell;
			if (dtc == null)
				return null;
			Textbox dtb = dtc.ReportItems.Items[0] as Textbox;
			if (dtb == null)
				return null;

			string sortcmp;
			switch (dtb.Value.Type)
			{
				case TypeCode.DateTime:
					sortcmp = "sort_cmp_date";
					break;
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Decimal:
				case TypeCode.Single:
				case TypeCode.Double:
					sortcmp = "sort_cmp_number";
					break;
				case TypeCode.String:
					sortcmp = "sort_cmp_string";
					break;
				case TypeCode.Empty:	// Not a type we know how to sort
				default:		
					sortcmp = null;
					break;
			}

			return sortcmp;
		}

		public void TableCellEnd(TableCell t, Row row)
		{
			string cellType = t.InTableHeader? "th": "td";
			Textbox tb = t.ReportItems.Items[0] as Textbox;
			if (cellType == "th" && SortType(t, tb) != null)
			{	// put the second half of the sort tags for the column
				// first half ---- <a href="#" onclick="sort_table(this,sort_cmp_string,1,0);return false;">
				// next half follows text  ---- <span class="sortarrow">&nbsp;&nbsp;&nbsp;</span></a></th>
				tw.Write("<span class=\"sortarrow\">&nbsp;&nbsp;&nbsp;</span></a>");
			}

			tw.Write("</{0}>", cellType);
			return;
		}

        public async Task<bool> MatrixStart(Matrix m, MatrixCellEntry[,] matrix, Row r, int headerRows, int maxRows, int maxCols)				// called first
		{
			string bookmark = await m.BookmarkValue(this.r, r);
			if (bookmark != null)
				tw.WriteLine("<div id=\"{0}\">", bookmark);		// can't use the table id since we're using for css style

			// output some of the table styles
			string cssName = await CssAdd(m.Style, m, r);	// get the style name for this item

			tw.WriteLine("<table id='{0}'>", cssName);
			return true;
		}

		public void MatrixColumns(Matrix m, MatrixColumns mc)	// called just after MatrixStart
		{
		}

		public async Task MatrixCellStart(Matrix m, ReportItem ri, int row, int column, Row r, float h, float w, int colSpan)
		{
			if (ri == null)			// Empty cell?
			{
				if (_SkipMatrixCols == 0)
					tw.Write("<td>");
				return;
			}

			string cssName = await CssAdd(ri.Style, ri, r, false, h, w);	// get the style name for this item

			await tw.WriteAsync($"<td id='{cssName}'");
			if (colSpan != 1)
			{
				tw.Write(" colspan={0}", colSpan);
				_SkipMatrixCols=-(colSpan-1);	// start it as negative as indicator that we need this </td>
			}
			else
				_SkipMatrixCols=0;
			if (ri is Textbox)
			{
				Textbox tb = (Textbox) ri;
				if (tb.IsToggle && tb.Name != null)		// name is required for this
				{
					string name = tb.Name.Nm + "_" + (tb.RunCount(this.r)+1).ToString();

					bScriptToggle = true;	// we need to generate JavaScript in header
					// TODO -- need to calculate the hide count correctly
					tw.Write(" onClick=\"hideShow(this, {0}, '{1}')\" onMouseOver=\"style.cursor ='hand'\"", 0, name);
				}
			}

			tw.Write(">");
		}

		public Task MatrixCellEnd(Matrix m, ReportItem ri, int row, int column, Row r)
		{
			if (_SkipMatrixCols == 0)
				tw.Write("</td>");
			else if (_SkipMatrixCols < 0)
			{
				tw.Write("</td>");
				_SkipMatrixCols = -_SkipMatrixCols;
			}
			else
				_SkipMatrixCols--;
			return Task.CompletedTask;
		}

		public void MatrixRowStart(Matrix m, int row, Row r)
		{
			tw.Write("\t<tr");

			tw.Write(">");
		}

		public void MatrixRowEnd(Matrix m, int row, Row r)
		{
			tw.WriteLine("</tr>");
		}

		public async Task MatrixEnd(Matrix m, Row r)				// called last
		{
			tw.Write("</table>");

			string bookmark = await m.BookmarkValue(this.r, r);
			if (bookmark != null)
				tw.WriteLine("</div>");		
			return;
		}

		public async Task Chart(Chart c, Row r, ChartBase cb)
		{
			string relativeName;

			Stream io = _sg.GetIOStream(out relativeName, "png");
			try
			{
				cb.Save(this.r, io, ImageFormat.Png);
			}
			finally
			{
				io.Flush();
				io.Close();
			}
			
			relativeName = FixupRelativeName(relativeName);

			// Create syntax in a string buffer
			StringWriter sw = new StringWriter();

			string bookmark = await c.BookmarkValue(this.r, r);
			if (bookmark != null)
				sw.WriteLine("<div id=\"{0}\">", bookmark);		// can't use the table id since we're using for css style

			string cssName = await CssAdd(c.Style, c, null);	// get the style name for this item

			sw.Write("<img src=\"{0}\" class='{1}'", relativeName, cssName);
			string tooltip = await c.ToolTipValue(this.r, r);
			if (tooltip != null)
				sw.Write(" alt=\"{0}\"", tooltip);
			if (c.Height != null)
				sw.Write(" height=\"{0}\"", c.Height.PixelsY.ToString());
			if (c.Width != null)
				sw.Write(" width=\"{0}\"", c.Width.PixelsX.ToString());
			sw.Write(">");
			if (bookmark != null)
				sw.Write("</div>");

			tw.Write(await Action(c.Action, r, sw.ToString(), tooltip));
			
			return;
		}

		public async Task Image(Image i, Row r, string mimeType, Stream ioin)
        {
            string relativeName;
            string suffix;

            switch (mimeType)
            {
                case "image/bmp":
                    suffix = "bmp";
                    break;
                case "image/jpeg":
                    suffix = "jpeg";
                    break;
                case "image/gif":
                    suffix = "gif";
                    break;
                case "image/png":
                case "image/x-png":
                    suffix = "png";
                    break;
                default:
                    suffix = "unk";
                    break;
            }


            if (i.ImageSource == ImageSourceEnum.Embedded) // embedded image in html
            { 
                if (!string.IsNullOrWhiteSpace(i.EmbeddedImageData))
                {
                    relativeName = "data:" + mimeType + ";base64," + i.EmbeddedImageData;
                }
                else
                {
                    relativeName = "data:" + mimeType + ";base64," + await WriteImageToBase64String(ioin);
                }
              
            }
            else
            {
                relativeName = await WriteImageToFile(ioin, suffix);
                relativeName = FixupRelativeName(relativeName);
            }

            // Create syntax in a string buffer
            StringWriter sw = new StringWriter();

            string bookmark = await i.BookmarkValue(this.r, r);
            if (bookmark != null)
                sw.WriteLine("<div id=\"{0}\">", bookmark);     // we're using for css style

            string cssName = await CssAdd(i.Style, i, null);    // get the style name for this item

            sw.Write("<img src=\"{0}\" class='{1}'", relativeName, cssName);

            string tooltip = await i.ToolTipValue(this.r, r);
            if (tooltip != null)
                sw.Write(" alt=\"{0}\"", tooltip);
            int h = i.Height == null ? -1 : i.Height.PixelsY;
            int w = i.Width == null ? -1 : i.Width.PixelsX;
            switch (i.Sizing)
            {
                case ImageSizingEnum.AutoSize:
                    break;          // this is right
                case ImageSizingEnum.Clip:
                    break;          // not sure how to clip it    
                case ImageSizingEnum.Fit:
                    if (h > 0)
                        sw.Write(" height=\"{0}\"", h.ToString());
                    if (w > 0)
                        sw.Write(" width=\"{0}\"", w.ToString());
                    break;
                case ImageSizingEnum.FitProportional:
                    break;          // would have to create an image to handle this
            }

            sw.Write("/>");

            if (bookmark != null)
                sw.Write("</div>");

            tw.Write(await Action(i.Action, r, sw.ToString(), tooltip));
        }

        private async Task<string> WriteImageToBase64String(Stream ioin)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ioin.Position = 0;
                await ioin.CopyToAsync(ms);
                byte[] imageBytes = ms.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }

        private async Task<string> WriteImageToFile(Stream ioin, string suffix)
        {
            string relativeName;
            Stream io = _sg.GetIOStream(out relativeName, suffix);
            try
            {
                if (ioin.CanSeek)       // ioin.Length requires Seek support
                {
                    byte[] ba = new byte[ioin.Length];
                    ioin.ReadExactly(ba, 0, ba.Length);
                    io.Write(ba, 0, ba.Length);
                }
                else
                {
                    byte[] ba = new byte[1000];     // read a 1000 bytes at a time
                    while (true)
                    {
                        int length = ioin.Read(ba, 0, ba.Length);
                        if (length <= 0)
                            break;
                        io.Write(ba, 0, length);
                    }
                }
            }
            finally
            {
                await io.FlushAsync();
                io.Close();
            }

            return relativeName;
        }

        public async Task Line(Line l, Row r)
		{
			bool bVertical;
			string t;
			if (l.Height == null || l.Height.PixelsY > 0)	// only handle horizontal rule
			{
				if (l.Width == null || l.Width.PixelsX > 0)	//    and vertical rules
					return;
				bVertical = true;
				t = "<TABLE style=\"border-collapse:collapse;BORDER-STYLE: none;WIDTH: {0}; POSITION: absolute; LEFT: {1}; TOP: {2}; HEIGHT: {3}; BACKGROUND-COLOR:{4};\"><TBODY><TR style=\"WIDTH:{0}\"><TD style=\"WIDTH:{0}\"></TD></TR></TBODY></TABLE>";
			}
			else
			{
				bVertical = false;			
				t = "<TABLE style=\"border-collapse:collapse;BORDER-STYLE: none;WIDTH: {0}; POSITION: absolute; LEFT: {1}; TOP: {2}; HEIGHT: {3}; BACKGROUND-COLOR:{4};\"><TBODY><TR style=\"HEIGHT:{3}\"><TD style=\"HEIGHT:{3}\"></TD></TR></TBODY></TABLE>";
			}

			string width, left, top, height, color;
			Style s = l.Style;

			left = l.Left == null? "0px": l.Left.CSS;
			top = l.Top == null? "0px": l.Top.CSS;

			if (bVertical)
			{
				height = l.Height == null? "0px": l.Height.CSS;
				// width comes from the BorderWidth
				if (s != null && s.BorderWidth != null && s.BorderWidth.Default != null)
					width = await s.BorderWidth.Default.EvaluateString(this.r, r);
				else
					width = "1px";
			}
			else
			{
				width = l.Width == null? "0px": l.Width.CSS;
				// height comes from the BorderWidth
				if (s != null && s.BorderWidth != null && s.BorderWidth.Default != null)
					height = await s.BorderWidth.Default.EvaluateString(this.r, r);
				else
					height = "1px";
			}

			if (s != null && s.BorderColor != null && s.BorderColor.Default != null)
				color = await s.BorderColor.Default.EvaluateString(this.r, r);
			else
				color = "black";
			
			tw.WriteLine(t, width, left, top, height, color);
			return;
		}

		public async Task<bool> RectangleStart(Rdl.Rectangle rect, Row r)
		{
			string cssName = await CssAdd(rect.Style, rect, r);	// get the style name for this item

			string bookmark = await rect.BookmarkValue(this.r, r);
			if (bookmark != null)
				tw.WriteLine("<div id=\"{0}\">", bookmark);		// can't use the table id since we're using for css style

			// Calculate the width of all the columns
			int width = rect.Width == null? -1: rect.Width.PixelsX;
			if (width < 0)
				tw.WriteLine("<table id='{0}'><tr>", cssName);
			else
				tw.WriteLine("<table id='{0}' width={1}><tr>", cssName, width);

			return true;
		}

		public async Task RectangleEnd(Rdl.Rectangle rect, Row r)
		{
			tw.WriteLine("</tr></table>");
			string bookmark = await rect.BookmarkValue(this.r, r);
			if (bookmark != null)
				tw.WriteLine("</div>"); 
			return;
		}

		// Subreport:  
		public async Task Subreport(Subreport s, Row r)
		{
			string cssName = await CssAdd(s.Style, s, r);	// get the style name for this item

			tw.WriteLine("<div class='{0}'>", cssName);

            await s.ReportDefn.Run(this);
			
			tw.WriteLine("</div>");
            return;
        }
		public void GroupingStart(Grouping g)			// called at start of grouping
		{
		}
		public void GroupingInstanceStart(Grouping g)	// called at start for each grouping instance
		{
		}
		public void GroupingInstanceEnd(Grouping g)	// called at start for each grouping instance
		{
		}
		public void GroupingEnd(Grouping g)			// called at end of grouping
		{
		}
		public Task RunPages(Pages pgs)	// we don't have paging turned on for html
		{
            return Task.CompletedTask;
        }
	}
	

	class CssCacheEntry
	{
		string _Css;					// css 
		string _Name;					// name of entry

		public CssCacheEntry(string css, string name)
		{
			_Css = css;
			_Name = name;				
		}

		public string Css
		{
			get { return  _Css; }
			set { _Css = value; }
		}

		public string Name
		{
			get { return  _Name; }
			set { _Name = value; }
		}
	}
}
