#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Reporting.Rdl.PptxConverter
{
    /// <summary>
    /// Writes a PowerPoint (.pptx) package: PresentationML in an Open Packaging Conventions zip, with no
    /// Office dependency. It knows nothing of reports; RenderPptx hands it slides of finished shape XML.
    /// Every slide uses one blank layout of one master, so the slides carry everything they show.
    /// </summary>
    internal sealed class PptxPackage
    {
        internal const long EmuPerPoint = 12700;

        // PowerPoint refuses a slide side outside 1in..56in.
        internal const long MinSlideEmu = 914400;
        internal const long MaxSlideEmu = 51206400;

        private readonly List<PptxSlide> _slides = new List<PptxSlide>();
        private readonly Dictionary<string, string> _mediaByHash = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<(string Path, byte[] Data)> _media = new List<(string, byte[])>();
        private readonly HashSet<string> _mediaExtensions = new HashSet<string>(StringComparer.Ordinal);

        internal long SlideWidthEmu { get; set; } = 9144000;
        internal long SlideHeightEmu { get; set; } = 6858000;
        internal string Title { get; set; }
        internal string Author { get; set; }
        internal string Description { get; set; }
        internal DateTime Created { get; set; } = DateTime.Now;

        internal int SlideCount => _slides.Count;

        internal PptxSlide AddSlide()
        {
            var slide = new PptxSlide();
            _slides.Add(slide);
            return slide;
        }

        /// <summary>
        /// Puts an image in the package once however many slides show it (a logo on every page),
        /// and returns its part name relative to the slides folder.
        /// </summary>
        internal string AddMedia(byte[] data, string extension)
        {
            string hash = Convert.ToHexString(SHA256.HashData(data)) + "." + extension;
            if (_mediaByHash.TryGetValue(hash, out string existing))
                return existing;

            string name = "image" + (_media.Count + 1).ToString(CultureInfo.InvariantCulture) + "." + extension;
            _media.Add(("ppt/media/" + name, data));
            _mediaExtensions.Add(extension);
            string target = "../media/" + name;
            _mediaByHash[hash] = target;
            return target;
        }

        internal void Write(Stream output)
        {
            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

            WriteText(zip, "[Content_Types].xml", BuildContentTypes());
            WriteText(zip, "_rels/.rels", RootRels);
            WriteText(zip, "docProps/core.xml", BuildCoreProperties());
            WriteText(zip, "docProps/app.xml", BuildAppProperties());
            WriteText(zip, "ppt/presentation.xml", BuildPresentation());
            WriteText(zip, "ppt/_rels/presentation.xml.rels", BuildPresentationRels());
            WriteText(zip, "ppt/presProps.xml", PresProps);
            WriteText(zip, "ppt/tableStyles.xml", TableStyles);
            WriteText(zip, "ppt/theme/theme1.xml", Theme);
            WriteText(zip, "ppt/slideMasters/slideMaster1.xml", SlideMaster);
            WriteText(zip, "ppt/slideMasters/_rels/slideMaster1.xml.rels", SlideMasterRels);
            WriteText(zip, "ppt/slideLayouts/slideLayout1.xml", SlideLayout);
            WriteText(zip, "ppt/slideLayouts/_rels/slideLayout1.xml.rels", SlideLayoutRels);

            for (int i = 0; i < _slides.Count; i++)
            {
                string n = (i + 1).ToString(CultureInfo.InvariantCulture);
                WriteText(zip, "ppt/slides/slide" + n + ".xml", _slides[i].BuildXml());
                WriteText(zip, "ppt/slides/_rels/slide" + n + ".xml.rels", _slides[i].BuildRels());
            }

            foreach (var (path, data) in _media)
                WriteBytes(zip, path, data);
        }

        #region parts

        private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n";
        internal const string NsA = "http://schemas.openxmlformats.org/drawingml/2006/main";
        internal const string NsR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        internal const string NsP = "http://schemas.openxmlformats.org/presentationml/2006/main";
        private const string NsRels = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string RelBase = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
        private const string PmlNamespaces = " xmlns:a=\"" + NsA + "\" xmlns:r=\"" + NsR + "\" xmlns:p=\"" + NsP + "\"";

        internal const string RelSlideLayout = RelBase + "slideLayout";
        internal const string RelImage = RelBase + "image";
        internal const string RelHyperlink = RelBase + "hyperlink";

        private string BuildContentTypes()
        {
            var sb = new StringBuilder(Declaration);
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            foreach (string ext in new[] { "png", "jpeg", "gif" })
            {
                if (_mediaExtensions.Contains(ext))
                    sb.Append("<Default Extension=\"").Append(ext).Append("\" ContentType=\"image/").Append(ext).Append("\"/>");
            }
            const string pml = "application/vnd.openxmlformats-officedocument.presentationml.";
            Override(sb, "/ppt/presentation.xml", pml + "presentation.main+xml");
            Override(sb, "/ppt/presProps.xml", pml + "presProps+xml");
            Override(sb, "/ppt/tableStyles.xml", pml + "tableStyles+xml");
            Override(sb, "/ppt/theme/theme1.xml", "application/vnd.openxmlformats-officedocument.theme+xml");
            Override(sb, "/ppt/slideMasters/slideMaster1.xml", pml + "slideMaster+xml");
            Override(sb, "/ppt/slideLayouts/slideLayout1.xml", pml + "slideLayout+xml");
            for (int i = 1; i <= _slides.Count; i++)
                Override(sb, "/ppt/slides/slide" + i.ToString(CultureInfo.InvariantCulture) + ".xml", pml + "slide+xml");
            Override(sb, "/docProps/core.xml", "application/vnd.openxmlformats-package.core-properties+xml");
            Override(sb, "/docProps/app.xml", "application/vnd.openxmlformats-officedocument.extended-properties+xml");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static void Override(StringBuilder sb, string part, string type) =>
            sb.Append("<Override PartName=\"").Append(part).Append("\" ContentType=\"").Append(type).Append("\"/>");

        private const string RootRels = Declaration +
            "<Relationships xmlns=\"" + NsRels + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + RelBase + "officeDocument\" Target=\"ppt/presentation.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"" + RelBase + "extended-properties\" Target=\"docProps/app.xml\"/>" +
            "</Relationships>";

        private string BuildCoreProperties()
        {
            var sb = new StringBuilder(Declaration);
            sb.Append("<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\"")
              .Append(" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\"")
              .Append(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            if (!string.IsNullOrEmpty(Title))
                sb.Append("<dc:title>").Append(Xml.Escape(Title)).Append("</dc:title>");
            if (!string.IsNullOrEmpty(Author))
                sb.Append("<dc:creator>").Append(Xml.Escape(Author)).Append("</dc:creator>");
            if (!string.IsNullOrEmpty(Description))
                sb.Append("<dc:description>").Append(Xml.Escape(Description)).Append("</dc:description>");
            string when = Created.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            sb.Append("<dcterms:created xsi:type=\"dcterms:W3CDTF\">").Append(when).Append("</dcterms:created>");
            sb.Append("<dcterms:modified xsi:type=\"dcterms:W3CDTF\">").Append(when).Append("</dcterms:modified>");
            sb.Append("</cp:coreProperties>");
            return sb.ToString();
        }

        private string BuildAppProperties() =>
            Declaration +
            "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\">" +
            "<Application>Majorsilence Reporting</Application>" +
            "<Slides>" + _slides.Count.ToString(CultureInfo.InvariantCulture) + "</Slides>" +
            "</Properties>";

        // rId1 master, rId2 theme, rId3 presProps, rId4 tableStyles, rId5.. the slides.
        private string BuildPresentation()
        {
            var sb = new StringBuilder(Declaration);
            sb.Append("<p:presentation").Append(PmlNamespaces).Append(" saveSubsetFonts=\"1\">");
            sb.Append("<p:sldMasterIdLst><p:sldMasterId id=\"2147483648\" r:id=\"rId1\"/></p:sldMasterIdLst>");
            if (_slides.Count > 0)
            {
                sb.Append("<p:sldIdLst>");
                for (int i = 0; i < _slides.Count; i++)
                {
                    sb.Append("<p:sldId id=\"").Append((256 + i).ToString(CultureInfo.InvariantCulture))
                      .Append("\" r:id=\"rId").Append((5 + i).ToString(CultureInfo.InvariantCulture)).Append("\"/>");
                }
                sb.Append("</p:sldIdLst>");
            }
            sb.Append("<p:sldSz cx=\"").Append(SlideWidthEmu.ToString(CultureInfo.InvariantCulture))
              .Append("\" cy=\"").Append(SlideHeightEmu.ToString(CultureInfo.InvariantCulture)).Append("\"/>");
            sb.Append("<p:notesSz cx=\"6858000\" cy=\"9144000\"/>");
            sb.Append("</p:presentation>");
            return sb.ToString();
        }

        private string BuildPresentationRels()
        {
            var sb = new StringBuilder(Declaration);
            sb.Append("<Relationships xmlns=\"").Append(NsRels).Append("\">");
            Rel(sb, "rId1", RelBase + "slideMaster", "slideMasters/slideMaster1.xml");
            Rel(sb, "rId2", RelBase + "theme", "theme/theme1.xml");
            Rel(sb, "rId3", RelBase + "presProps", "presProps.xml");
            Rel(sb, "rId4", RelBase + "tableStyles", "tableStyles.xml");
            for (int i = 0; i < _slides.Count; i++)
                Rel(sb, "rId" + (5 + i).ToString(CultureInfo.InvariantCulture), RelBase + "slide",
                    "slides/slide" + (i + 1).ToString(CultureInfo.InvariantCulture) + ".xml");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        internal static void Rel(StringBuilder sb, string id, string type, string target, bool external = false)
        {
            sb.Append("<Relationship Id=\"").Append(id).Append("\" Type=\"").Append(type)
              .Append("\" Target=\"").Append(Xml.Escape(target)).Append('"');
            if (external)
                sb.Append(" TargetMode=\"External\"");
            sb.Append("/>");
        }

        private const string PresProps = Declaration + "<p:presentationPr" + PmlNamespaces + "/>";

        private const string TableStyles = Declaration +
            "<a:tblStyleLst xmlns:a=\"" + NsA + "\" def=\"{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}\"/>";

        internal const string EmptyGroup =
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>";

        private const string SlideMaster = Declaration +
            "<p:sldMaster" + PmlNamespaces + ">" +
            "<p:cSld><p:bg><p:bgPr><a:solidFill><a:srgbClr val=\"FFFFFF\"/></a:solidFill><a:effectLst/></p:bgPr></p:bg>" +
            "<p:spTree>" + EmptyGroup + "</p:spTree></p:cSld>" +
            "<p:clrMap bg1=\"lt1\" tx1=\"dk1\" bg2=\"lt2\" tx2=\"dk2\" accent1=\"accent1\" accent2=\"accent2\" accent3=\"accent3\"" +
            " accent4=\"accent4\" accent5=\"accent5\" accent6=\"accent6\" hlink=\"hlink\" folHlink=\"folHlink\"/>" +
            "<p:sldLayoutIdLst><p:sldLayoutId id=\"2147483649\" r:id=\"rId1\"/></p:sldLayoutIdLst>" +
            "<p:txStyles><p:titleStyle/><p:bodyStyle/><p:otherStyle/></p:txStyles>" +
            "</p:sldMaster>";

        private const string SlideMasterRels = Declaration +
            "<Relationships xmlns=\"" + NsRels + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + RelBase + "slideLayout\" Target=\"../slideLayouts/slideLayout1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"" + RelBase + "theme\" Target=\"../theme/theme1.xml\"/>" +
            "</Relationships>";

        private const string SlideLayout = Declaration +
            "<p:sldLayout" + PmlNamespaces + " type=\"blank\" preserve=\"1\">" +
            "<p:cSld name=\"Blank\"><p:spTree>" + EmptyGroup + "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sldLayout>";

        private const string SlideLayoutRels = Declaration +
            "<Relationships xmlns=\"" + NsRels + "\">" +
            "<Relationship Id=\"rId1\" Type=\"" + RelBase + "slideMaster\" Target=\"../slideMasters/slideMaster1.xml\"/>" +
            "</Relationships>";

        private const string PhFill = "<a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill>";
        private const string PhLine = "<a:ln w=\"6350\"><a:solidFill><a:schemeClr val=\"phClr\"/></a:solidFill></a:ln>";
        private const string NoEffect = "<a:effectStyle><a:effectLst/></a:effectStyle>";

        private const string Theme = Declaration +
            "<a:theme xmlns:a=\"" + NsA + "\" name=\"Office Theme\"><a:themeElements>" +
            "<a:clrScheme name=\"Office\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\"/></a:dk1>" +
            "<a:lt1><a:sysClr val=\"window\" lastClr=\"FFFFFF\"/></a:lt1>" +
            "<a:dk2><a:srgbClr val=\"44546A\"/></a:dk2><a:lt2><a:srgbClr val=\"E7E6E6\"/></a:lt2>" +
            "<a:accent1><a:srgbClr val=\"4472C4\"/></a:accent1><a:accent2><a:srgbClr val=\"ED7D31\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"A5A5A5\"/></a:accent3><a:accent4><a:srgbClr val=\"FFC000\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"5B9BD5\"/></a:accent5><a:accent6><a:srgbClr val=\"70AD47\"/></a:accent6>" +
            "<a:hlink><a:srgbClr val=\"0563C1\"/></a:hlink><a:folHlink><a:srgbClr val=\"954F72\"/></a:folHlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"Office\">" +
            "<a:majorFont><a:latin typeface=\"Arial\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:majorFont>" +
            "<a:minorFont><a:latin typeface=\"Arial\"/><a:ea typeface=\"\"/><a:cs typeface=\"\"/></a:minorFont>" +
            "</a:fontScheme>" +
            "<a:fmtScheme name=\"Office\">" +
            "<a:fillStyleLst>" + PhFill + PhFill + PhFill + "</a:fillStyleLst>" +
            "<a:lnStyleLst>" + PhLine + PhLine + PhLine + "</a:lnStyleLst>" +
            "<a:effectStyleLst>" + NoEffect + NoEffect + NoEffect + "</a:effectStyleLst>" +
            "<a:bgFillStyleLst>" + PhFill + PhFill + PhFill + "</a:bgFillStyleLst>" +
            "</a:fmtScheme>" +
            "</a:themeElements><a:objectDefaults/><a:extraClrSchemeLst/></a:theme>";

        #endregion

        // A fixed time on every entry, so the same report gives the same bytes.
        private static readonly DateTimeOffset EntryTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static void WriteText(ZipArchive zip, string path, string content) =>
            WriteBytes(zip, path, new UTF8Encoding(false).GetBytes(content));

        private static void WriteBytes(ZipArchive zip, string path, byte[] data)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
            entry.LastWriteTime = EntryTime;
            using var stream = entry.Open();
            stream.Write(data, 0, data.Length);   // one write: the compressed bytes depend only on the content
        }
    }

    /// <summary>One slide: its shape XML and its relationships (rId1 is always the layout).</summary>
    internal sealed class PptxSlide
    {
        private readonly StringBuilder _shapes = new StringBuilder();
        private readonly StringBuilder _rels = new StringBuilder();
        private readonly Dictionary<string, string> _relByTarget = new Dictionary<string, string>(StringComparer.Ordinal);
        private int _nextRel = 2;
        private int _nextShapeId = 2;  // 1 is the shape tree itself

        internal StringBuilder Shapes => _shapes;

        internal int NextShapeId() => _nextShapeId++;

        internal string AddRelationship(string type, string target, bool external = false)
        {
            string key = type + "|" + target;
            if (_relByTarget.TryGetValue(key, out string id))
                return id;
            id = "rId" + (_nextRel++).ToString(CultureInfo.InvariantCulture);
            PptxPackage.Rel(_rels, id, type, target, external);
            _relByTarget[key] = id;
            return id;
        }

        internal string BuildXml() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
            "<p:sld xmlns:a=\"" + PptxPackage.NsA + "\" xmlns:r=\"" + PptxPackage.NsR + "\" xmlns:p=\"" + PptxPackage.NsP + "\">" +
            "<p:cSld><p:spTree>" + PptxPackage.EmptyGroup + _shapes + "</p:spTree></p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";

        internal string BuildRels()
        {
            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            PptxPackage.Rel(sb, "rId1", PptxPackage.RelSlideLayout, "../slideLayouts/slideLayout1.xml");
            sb.Append(_rels);
            sb.Append("</Relationships>");
            return sb.ToString();
        }
    }

    internal static class Xml
    {
        /// <summary>Escapes text for an XML attribute or element, dropping characters XML 1.0 cannot hold.</summary>
        internal static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            StringBuilder sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                string rep = null;
                bool drop = false;
                switch (c)
                {
                    case '&': rep = "&amp;"; break;
                    case '<': rep = "&lt;"; break;
                    case '>': rep = "&gt;"; break;
                    case '"': rep = "&quot;"; break;
                    case '\'': rep = "&apos;"; break;
                    default:
                        if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                        {
                            sb?.Append(c).Append(s[i + 1]);
                            i++;
                            continue;
                        }
                        drop = (c < 0x20 && c != '\t' && c != '\n' && c != '\r') || char.IsSurrogate(c) || c == '￾' || c == '￿';
                        break;
                }
                if (rep == null && !drop)
                {
                    sb?.Append(c);
                    continue;
                }
                if (sb == null)
                    sb = new StringBuilder(s.Length + 16).Append(s, 0, i);
                if (rep != null)
                    sb.Append(rep);
            }
            return sb == null ? s : sb.ToString();
        }
    }
}
