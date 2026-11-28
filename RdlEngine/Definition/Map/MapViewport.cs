using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// The map's MapViewport: the coordinate system and projection, where in the map item the
    /// shapes are drawn, and how far they are zoomed.
    /// </summary>
    /// <remarks>
    /// MapCoordinateSystem is Planar (the schema default: coordinates are drawn as they are) or
    /// Geographic (longitude and latitude, projected). Of the projections, Equirectangular (the
    /// default) and Mercator are drawn; the others are drawn as Equirectangular, and the
    /// normaliser says so. The view is always fitted to the data and centred on it; a
    /// MapCustomView's Zoom (percent, 100 fits the data) is applied, its centre and MapLimits
    /// are not. MapLocation and MapSize place the viewport in the item when given in percent.
    /// </remarks>
    internal sealed class MapViewport : ReportLink
    {
        internal bool Geographic;
        internal string Projection = "Equirectangular";
        internal double Zoom = 100;
        internal double Left, Top, Width = 100, Height = 100;     // percent of the map item
        internal Style Style;

        internal MapViewport (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            if (xNode == null)
                return;
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                switch (node.LocalName) {
                    case "MapCoordinateSystem":
                        Geographic = string.Equals (node.InnerText.Trim (), "Geographic", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "MapProjection":
                        Projection = node.InnerText.Trim ();
                        break;
                    case "MapCustomView":
                    case "MapDataBoundView":
                    case "MapElementView":
                        var zoom = Number (MapEmbeddedElement.Child (node, "Zoom")?.InnerText);
                        if (zoom > 0)
                            Zoom = zoom.Value;
                        break;
                    case "MapLocation":
                        if (InPercent (node)) {
                            Left = Number (MapEmbeddedElement.Child (node, "Left")?.InnerText) ?? Left;
                            Top = Number (MapEmbeddedElement.Child (node, "Top")?.InnerText) ?? Top;
                        }
                        break;
                    case "MapSize":
                        if (InPercent (node)) {
                            Width = Number (MapEmbeddedElement.Child (node, "Width")?.InnerText) ?? Width;
                            Height = Number (MapEmbeddedElement.Child (node, "Height")?.InnerText) ?? Height;
                        }
                        break;
                    case "Style":
                        Style = new Style (r, this, node);
                        break;
                    // Read but not acted on: see the remarks, and MapSupport for what is reported.
                    case "ProjectionCenterX":
                    case "ProjectionCenterY":
                    case "MapLimits":
                    case "MaximumZoom":
                    case "MinimumZoom":
                    case "SimplificationResolution":
                    case "MapMeridians":
                    case "MapParallels":
                    case "GridUnderContent":
                    case "ContentMargin":
                    case "LeftMargin":
                    case "RightMargin":
                    case "TopMargin":
                    case "BottomMargin":
                    case "ZIndex":
                        break;
                    default:
                        OwnerReport.rl.LogError (4, "Unknown MapViewport element '" + node.Name + "' ignored.");
                        break;
                }
            }
            if (Width <= 0 || Height <= 0) {
                Width = 100;
                Height = 100;
            }
        }

        private static bool InPercent (XmlNode node)
        {
            var unit = MapEmbeddedElement.Child (node, "Unit")?.InnerText?.Trim ();
            return string.IsNullOrEmpty (unit) || string.Equals (unit, "Percent", StringComparison.OrdinalIgnoreCase);
        }

        private static double? Number (string text)
            => double.TryParse (text?.Trim (), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (double?)null;

        internal override async Task FinalPass ()
        {
            if (Style != null)
                await Style.FinalPass ().ConfigureAwait (false);
        }

        internal bool IsMercator => Geographic && string.Equals (Projection, "Mercator", StringComparison.OrdinalIgnoreCase);

        /// <summary>The projections that are drawn as written; any other is drawn as Equirectangular.</summary>
        internal static bool IsDrawnProjection (string projection)
            => string.IsNullOrEmpty (projection)
               || string.Equals (projection, "Equirectangular", StringComparison.OrdinalIgnoreCase)
               || string.Equals (projection, "Mercator", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// A coordinate on the projected plane, x to the east and y to the north. Planar
        /// coordinates are kept; geographic ones are projected (Mercator latitude stretched,
        /// clamped short of the poles, in degree-like units so it mixes with the equirectangular scale).
        /// </summary>
        internal MapPoint Project (MapPoint p)
        {
            if (!IsMercator)
                return p;
            var latitude = Math.Max (-85.0, Math.Min (85.0, p.Y)) * Math.PI / 180.0;
            var y = Math.Log (Math.Tan (Math.PI / 4 + latitude / 2)) * 180.0 / Math.PI;
            return new MapPoint (p.X, y);
        }
    }
}
