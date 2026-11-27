using System;
using System.Collections.Generic;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Which parts of an RDL 2010+ Map report item the engine draws, decided on the XML before the
    /// report is parsed, so that the normaliser can keep a map it can draw, replace one it cannot
    /// with a placeholder, and name in one warning what is left out.
    /// </summary>
    /// <remarks>
    /// Drawn: polygon layers whose shapes are embedded in the report (MapPolygons with VectorData),
    /// come from a dataset's spatial field (MapSpatialDataSet), or from the bound data region's
    /// spatial field (MapSpatialDataRegion), coloured by a colour range, palette or custom colour
    /// rule; Planar or Geographic coordinates in the Equirectangular or Mercator projection.
    /// Not drawn: tile layers (online map images, which need network access and a licence),
    /// shapefiles, point and line layers, legends, colour scales, titles, distance scales,
    /// meridians and parallels, border skins, polygon centre points and labels.
    /// </remarks>
    internal static class MapSupport
    {
        /// <summary>
        /// True when the map has a layer the engine draws. Adds to <paramref name="notDrawn"/> a
        /// description of each part it has that is not drawn.
        /// </summary>
        internal static bool Inspect (XmlElement map, ICollection<string> notDrawn)
        {
            var drawn = false;
            var layers = Child (map, "MapLayers");
            if (layers != null) {
                foreach (XmlNode node in layers.ChildNodes) {
                    if (node is not XmlElement layer)
                        continue;
                    var missing = LayerNotDrawn (layer);
                    if (missing == null) {
                        if (!IsHidden (layer))
                            drawn = true;
                        foreach (var part in PartsNotDrawn (layer))
                            notDrawn.Add (part);
                    } else if (missing.Length > 0) {
                        notDrawn.Add (missing);
                    }
                }
            }

            if (HasVisible (Child (map, "MapLegends"), "MapLegend"))
                notDrawn.Add ("a legend");
            if (HasVisible (Child (map, "MapTitles"), "MapTitle"))
                notDrawn.Add ("a title");
            if (Shown (Child (map, "MapColorScale")))
                notDrawn.Add ("a colour scale");
            if (Shown (Child (map, "MapDistanceScale")))
                notDrawn.Add ("a distance scale");
            var skin = Child (Child (map, "MapBorderSkin"), "MapBorderSkinType")?.InnerText?.Trim ();
            if (!string.IsNullOrEmpty (skin) && !string.Equals (skin, "None", StringComparison.OrdinalIgnoreCase))
                notDrawn.Add ("a border skin");

            var viewport = Child (map, "MapViewport");
            if (Shown (Child (viewport, "MapMeridians")) || Shown (Child (viewport, "MapParallels")))
                notDrawn.Add ("meridians and parallels");
            var projection = Child (viewport, "MapProjection")?.InnerText?.Trim ();
            var geographic = string.Equals (Child (viewport, "MapCoordinateSystem")?.InnerText?.Trim (), "Geographic", StringComparison.OrdinalIgnoreCase);
            if (geographic && !MapViewport.IsDrawnProjection (projection))
                notDrawn.Add ($"the {projection} projection (drawn as Equirectangular)");

            return drawn;
        }

        /// <summary>
        /// Null when the engine draws the layer (<see cref="MapLayers.Create"/> makes its
        /// definition), otherwise what the layer is, for the warning ("" for a layer that is hidden
        /// anyway, which is not worth a warning).
        /// </summary>
        internal static string LayerNotDrawn (XmlElement layer)
        {
            switch (layer.LocalName) {
                case "MapPolygonLayer":
                    if (Child (layer, "MapShapefile") != null || Child (Child (layer, "MapSpatialData"), "MapShapefile") != null)
                        return IsHidden (layer) ? string.Empty : "a polygon layer bound to a shapefile";
                    if (HasSpatialData (layer, "MapPolygons"))
                        return null;
                    return IsHidden (layer) ? string.Empty : "a polygon layer without spatial data";
                case "MapTileLayer":
                    return IsHidden (layer) ? string.Empty : "a tile layer of online map images";
                case "MapPointLayer":
                    return IsHidden (layer) ? string.Empty : "a point layer";
                case "MapLineLayer":
                    return IsHidden (layer) ? string.Empty : "a line layer";
                default:
                    return "a " + layer.LocalName;
            }
        }

        /// <summary>What a drawn layer has that is not drawn.</summary>
        private static IEnumerable<string> PartsNotDrawn (XmlElement layer)
        {
            if (layer.LocalName == "MapPolygonLayer") {
                if (Child (layer, "MapCenterPointRules") != null)
                    yield return "centre points on polygons";
                var label = Child (Child (layer, "MapPolygonTemplate"), "Label")?.InnerText?.Trim ();
                if (!string.IsNullOrEmpty (label))
                    yield return "polygon labels";
            }
        }

        /// <summary>Embedded elements of the kind, a spatial dataset, or a data region's spatial field.</summary>
        private static bool HasSpatialData (XmlElement layer, string embedded)
        {
            foreach (var holder in new[] { layer, Child (layer, "MapSpatialData") }) {
                if (holder == null)
                    continue;
                var elements = Child (holder, embedded);
                if (elements != null && elements.HasChildNodes)
                    return true;
                if (Child (holder, "MapSpatialDataSet") != null || Child (Child (holder, "MapSpatialDataRegion"), "VectorData") != null)
                    return true;
            }
            return false;
        }

        private static bool IsHidden (XmlElement layer)
            => string.Equals (Child (layer, "VisibilityMode")?.InnerText?.Trim (), "Hidden", StringComparison.OrdinalIgnoreCase);

        /// <summary>The element is there and not Hidden.</summary>
        private static bool Shown (XmlElement element)
            => element != null && !string.Equals (Child (element, "Hidden")?.InnerText?.Trim (), "true", StringComparison.OrdinalIgnoreCase);

        private static bool HasVisible (XmlElement collection, string itemName)
        {
            if (collection == null)
                return false;
            foreach (XmlNode node in collection.ChildNodes) {
                if (node is XmlElement e && e.LocalName == itemName && Shown (e))
                    return true;
            }
            return false;
        }

        private static XmlElement Child (XmlElement parent, string localName)
            => parent == null ? null : MapEmbeddedElement.Child (parent, localName);
    }
}
