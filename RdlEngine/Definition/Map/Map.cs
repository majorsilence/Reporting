using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms.Drawing.Imaging;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// The RDL 2010+ Map report item, for the maps the engine draws (see <see cref="MapSupport"/>
    /// for which; the normaliser swaps any other for a placeholder before the report is parsed).
    /// </summary>
    /// <remarks>
    /// Each instance is evaluated into a <see cref="MapScene"/> (layers bound to their data,
    /// rules applied, shapes projected) and drawn as a PNG, the way a custom report item such as a
    /// gauge is: on a page as a <see cref="PageImage"/> that is drawn again at the resolution the
    /// output wants (PDF and the image formats), and through <see cref="IPresent.Image"/> for HTML
    /// and the other formats that run the report item by item. The map's Style is the item's, as
    /// for any image; the viewport's background is drawn inside the bitmap.
    ///
    /// Parts of a map that are read but not drawn (legends, titles, scales, ...) are named in the
    /// normaliser's warning, so they are passed over quietly here.
    /// </remarks>
    internal sealed class Map : ReportItem
    {
        private MapViewport _viewport;
        private readonly List<MapDataRegion> _dataRegions = new List<MapDataRegion> ();
        private MapLayers _layers;
        private readonly Image _presentAs;    // describes the drawn map to the renderers' Image method

        private static readonly HashSet<string> NotDrawn = new HashSet<string> (StringComparer.Ordinal)
        {
            "MapLegends", "MapTitles", "MapDistanceScale", "MapColorScale", "MapBorderSkin", "PageBreak",
            "AntiAliasing", "TextAntiAliasingQuality", "ShadowIntensity", "MaximumSpatialElementCount",
            "MaximumTotalPointCount", "TileLanguage",
        };

        internal Map (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                switch (node.LocalName) {
                    case "MapViewport":
                        _viewport = new MapViewport (r, this, node);
                        break;
                    case "MapDataRegions":
                        foreach (XmlNode region in node.ChildNodes) {
                            if (region.NodeType == XmlNodeType.Element && region.LocalName == "MapDataRegion")
                                _dataRegions.Add (new MapDataRegion (r, this, region));
                        }
                        break;
                    case "MapLayers":
                        _layers = new MapLayers (r, this, node);
                        break;
                    default:
                        if (NotDrawn.Contains (node.LocalName) || ReportItemElement (node))
                            break;
                        OwnerReport.rl.LogError (4, "Unknown Map element '" + node.Name + "' ignored.");
                        break;
                }
            }
            _viewport ??= new MapViewport (r, this, null);
            _presentAs = new Image (r, this, ImageDescription (xNode));
        }

        /// <summary>An Image element with the map's name, position, size and style, for <see cref="IPresent.Image"/>.</summary>
        private static XmlElement ImageDescription (XmlNode map)
        {
            var doc = new XmlDocument ();
            var image = doc.CreateElement ("Image", map.NamespaceURI);
            if (map is XmlElement e && e.HasAttribute ("Name"))
                image.SetAttribute ("Name", e.GetAttribute ("Name"));
            foreach (var name in new[] { "Top", "Left", "Height", "Width", "Style", "ToolTip" }) {
                var source = MapEmbeddedElement.Child (map, name);
                if (source != null)
                    image.AppendChild (doc.ImportNode (source, true));
            }
            void Add (string name, string text)
            {
                var child = doc.CreateElement (name, map.NamespaceURI);
                child.InnerText = text;
                image.AppendChild (child);
            }
            Add ("Source", "External");
            Add ("Value", "map.png");
            Add ("Sizing", "Clip");
            return image;
        }

        internal MapDataRegion FindDataRegion (string name)
        {
            foreach (var region in _dataRegions) {
                if (region.Name != null && string.Equals (region.Name.Nm, name, StringComparison.OrdinalIgnoreCase))
                    return region;
            }
            return null;
        }

        internal MapViewport Viewport => _viewport;

        internal IReadOnlyList<MapLayer> Layers => _layers?.Items ?? (IReadOnlyList<MapLayer>)Array.Empty<MapLayer> ();

        internal override async Task FinalPass ()
        {
            await base.FinalPass ().ConfigureAwait (false);
            await _viewport.FinalPass ().ConfigureAwait (false);
            // Data regions before layers: a layer's expressions are scoped by the region it binds to.
            foreach (var region in _dataRegions)
                await region.FinalPass ().ConfigureAwait (false);
            if (_layers != null)
                await _layers.FinalPass ().ConfigureAwait (false);
            await _presentAs.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>Evaluates the map for one instance: every shown layer, in order, into one scene.</summary>
        internal async Task<MapScene> BuildScene (Report rpt, Row row)
        {
            var scene = new MapScene (_viewport, WidthOrOwnerWidth (rpt), HeightOrOwnerHeight);
            if (_viewport.Style != null) {
                var style = await _viewport.Style.GetStyleInfo (rpt, row).ConfigureAwait (false);
                scene.ViewportBackground = style.BackgroundColor;
            }
            foreach (var layer in Layers) {
                if (!layer.Hidden)
                    await layer.Build (rpt, row, scene).ConfigureAwait (false);
            }
            return scene;
        }

        internal override async Task Run (IPresent ip, Row row)
        {
            var rpt = ip.Report ();
            if (await IsHidden (rpt, row).ConfigureAwait (false))
                return;
            try {
                var scene = await BuildScene (rpt, row).ConfigureAwait (false);
                var png = scene.ToPng (Math.Max (1, WidthCalc (rpt, null)), Math.Max (1, RSize.PixelsFromPoints (HeightOrOwnerHeight)));
                using var stream = new MemoryStream (png);
                await ip.Image (_presentAs, row, "image/png", stream).ConfigureAwait (false);
            } catch (Exception ex) {
                rpt.rl.LogError (8, string.Format ("Exception in Map handling.\n{0}\n{1}", ex.Message, ex.StackTrace));
            }
        }

        internal override async Task RunPage (Pages pgs, Row row)
        {
            var rpt = pgs.Report;
            if (await IsHidden (rpt, row).ConfigureAwait (false))
                return;

            SetPagePositionBegin (pgs);
            try {
                var scene = await BuildScene (rpt, row).ConfigureAwait (false);
                // Drawn again at the size the output asks for, as a custom report item is.
                var pi = new PageImage (ImageFormat.Png, (format, width, height) => scene.ToPng (width, height), ImageSizingEnum.Clip);
                await SetPagePositionAndStyle (rpt, pi, row).ConfigureAwait (false);

                if (IsInBody && pgs.CurrentPage.YOffset + pi.Y + pi.H >= pgs.BottomOfPage && !pgs.CurrentPage.IsEmpty ()) {
                    pgs.NextOrNew ();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                    if (YParents != null)
                        pi.Y = 0;
                }

                pgs.CurrentPage.AddObject (pi);
                SetPagePositionEnd (pgs, pi.Y + pi.H);
            } catch (Exception ex) {
                rpt.rl.LogError (8, string.Format ("Exception in Map handling: {0}", ex.Message));
            }
        }
    }
}
