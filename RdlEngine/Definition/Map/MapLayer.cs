using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// The map's MapLayers. Only the layer kinds the engine draws are created (see
    /// <see cref="Create"/>); the normaliser has already warned about the others
    /// (<see cref="MapSupport"/>), so they are passed over quietly here.
    /// </summary>
    internal sealed class MapLayers : ReportLink
    {
        internal readonly List<MapLayer> Items = new List<MapLayer> ();

        internal MapLayers (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node is not XmlElement element)
                    continue;
                if (MapSupport.LayerNotDrawn (element) != null)
                    continue;
                var layer = Create (r, this, element);
                if (layer != null)
                    Items.Add (layer);
            }
        }

        /// <summary>
        /// The definition of a layer the engine draws. A new layer kind is added here and, so the
        /// normaliser keeps maps that have it, in <see cref="MapSupport.LayerNotDrawn"/>.
        /// </summary>
        internal static MapLayer Create (ReportDefn r, ReportLink p, XmlElement element)
        {
            switch (element.LocalName) {
                case "MapPolygonLayer":
                    return new MapPolygonLayer (r, p, element);
                case "MapLineLayer":
                    return new MapLineLayer (r, p, element);
                case "MapPointLayer":
                    return new MapPointLayer (r, p, element);
                default:
                    return null;
            }
        }

        internal override async Task FinalPass ()
        {
            foreach (var layer in Items)
                await layer.FinalPass ().ConfigureAwait (false);
        }
    }

    /// <summary>What every map layer has (MapLayer): a name, whether it is shown, and its transparency.</summary>
    internal abstract class MapLayer : ReportLink
    {
        internal readonly string Name;
        internal bool Hidden;
        internal double Transparency;      // percent

        protected MapLayer (ReportDefn r, ReportLink p, XmlElement xNode) : base (r, p)
        {
            Name = xNode.GetAttribute ("Name");
        }

        /// <summary>Reads the elements every layer has; false for any other.</summary>
        protected bool LayerElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "VisibilityMode":
                    // ZoomBased layers are drawn: the report is drawn at one zoom.
                    Hidden = string.Equals (node.InnerText.Trim (), "Hidden", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "Transparency":
                    if (double.TryParse (node.InnerText.Trim (), NumberStyles.Float, CultureInfo.InvariantCulture, out var t))
                        Transparency = Math.Max (0, Math.Min (100, t));
                    return true;
                case "MinimumZoom":
                case "MaximumZoom":
                case "DataElementName":
                case "DataElementOutput":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Applies the layer's transparency to a colour.</summary>
        internal System.Drawing.Color Fade (System.Drawing.Color c)
        {
            if (c.IsEmpty || Transparency <= 0)
                return c;
            return System.Drawing.Color.FromArgb ((int)Math.Round (c.A * (1 - Transparency / 100)), c.R, c.G, c.B);
        }

        /// <summary>
        /// Evaluates the layer for one instance of the map and adds what it draws to the scene: its
        /// shapes, projected with the scene's viewport, and what its rules made of its values.
        /// </summary>
        internal abstract Task Build (Report rpt, Row row, MapScene scene);
    }

    /// <summary>
    /// What the polygon, line and point layers share (MapVectorLayer): where their shapes come
    /// from and how analytical data is bound to them.
    /// </summary>
    /// <remarks>
    /// Shapes come from the layer's embedded elements (MapPolygons, MapLines, MapPoints, read by
    /// the layer kind), from a dataset's spatial field (MapSpatialDataSet), or from the spatial
    /// field of the bound data region itself (MapSpatialDataRegion, one shape per group). A
    /// MapShapefile is not read. Data is bound by MapBindingFieldPairs: a group of the data region
    /// whose binding expressions equal a shape's spatial fields is that shape's data. Numbers
    /// compare as numbers, anything else as text ignoring case and surrounding spaces.
    /// </remarks>
    internal abstract class MapVectorLayer : MapLayer
    {
        private string _dataRegionName;
        private readonly List<(string FieldName, Expression Binding)> _bindings = new List<(string, Expression)> ();
        private MapSpatialDataSet _spatialDataSet;
        private Expression _spatialDataRegionVector;
        protected readonly List<MapEmbeddedElement> Embedded = new List<MapEmbeddedElement> ();

        /// <summary>The data region the layer is bound to, once FinalPass has found it; its expressions run in its groups.</summary>
        internal MapDataRegion DataRegion { get; private set; }

        protected MapVectorLayer (ReportDefn r, ReportLink p, XmlElement xNode) : base (r, p, xNode)
        {
        }

        /// <summary>Reads the elements every vector layer has; false for any other.</summary>
        protected bool VectorLayerElement (XmlNode node)
        {
            if (LayerElement (node))
                return true;
            switch (node.LocalName) {
                case "MapDataRegionName":
                    _dataRegionName = node.InnerText.Trim ();
                    return true;
                case "MapBindingFieldPairs":
                    foreach (XmlNode pair in node.ChildNodes) {
                        if (pair.NodeType != XmlNodeType.Element || pair.LocalName != "MapBindingFieldPair")
                            continue;
                        var field = MapEmbeddedElement.Child (pair, "FieldName")?.InnerText?.Trim ();
                        var binding = MapEmbeddedElement.Child (pair, "BindingExpression");
                        if (!string.IsNullOrEmpty (field) && binding != null)
                            _bindings.Add ((field, new Expression (OwnerReport, this, binding, ExpressionType.Variant)));
                    }
                    return true;
                case "MapFieldDefinitions":
                    return true;    // the types of the spatial fields; values are compared by what they hold
                case "MapSpatialDataSet":
                    _spatialDataSet = new MapSpatialDataSet (OwnerReport, this, node);
                    return true;
                case "MapSpatialDataRegion":
                    var vector = MapEmbeddedElement.Child (node, "VectorData");
                    if (vector != null)
                        _spatialDataRegionVector = new Expression (OwnerReport, this, vector, ExpressionType.Variant);
                    return true;
                case "MapShapefile":
                    return true;    // not read; MapSupport keeps such a layer out
                case "MapSpatialData":
                    // a wrapper some writers put the source in
                    foreach (XmlNode inner in node.ChildNodes) {
                        if (inner.NodeType == XmlNodeType.Element && !VectorLayerElement (inner) && !EmbeddedElements (inner))
                            OwnerReport.rl.LogError (4, "Unknown MapSpatialData element '" + inner.Name + "' ignored.");
                    }
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Reads the layer kind's own embedded elements (MapPolygons, ...); false for anything else.</summary>
        protected abstract bool EmbeddedElements (XmlNode node);

        internal override async Task FinalPass ()
        {
            // The data region first: Expression.FinalPass reads it to scope this layer's expressions.
            if (!string.IsNullOrEmpty (_dataRegionName)) {
                for (var link = Parent; link != null; link = link.Parent) {
                    if (link is Map map) {
                        DataRegion = map.FindDataRegion (_dataRegionName);
                        break;
                    }
                }
                if (DataRegion == null)
                    OwnerReport.rl.LogError (4, $"Map layer '{Name}' names data region '{_dataRegionName}', which the map does not have; its shapes are drawn without data.");
            }
            foreach (var (_, binding) in _bindings)
                await binding.FinalPass ().ConfigureAwait (false);
            if (_spatialDataSet != null)
                await _spatialDataSet.FinalPass ().ConfigureAwait (false);
            if (_spatialDataRegionVector != null)
                await _spatialDataRegionVector.FinalPass ().ConfigureAwait (false);
            foreach (var element in Embedded)
                await element.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>
        /// Gets the layer's shapes and binds the data region's groups to them. <paramref name="evaluate"/>
        /// runs once for each shape: for a bound one while its group's rows are current, with
        /// <see cref="MapSpatialElement.Row"/> set, so the layer's expressions and aggregates read
        /// that group; for the others afterwards, with no row.
        /// </summary>
        protected async Task<List<MapSpatialElement>> Bind (Report rpt, Row row, bool geographic, Func<MapSpatialElement, Task> evaluate)
        {
            var elements = new List<MapSpatialElement> ();

            if (_spatialDataRegionVector != null) {
                if (DataRegion != null) {
                    await DataRegion.ForEachGroup (rpt, row, async groupRow => {
                        var vector = await _spatialDataRegionVector.Evaluate (rpt, groupRow).ConfigureAwait (false);
                        var geometry = MapGeometry.Read (vector, geographic);
                        if (geometry == null)
                            return;
                        var element = new MapSpatialElement { Geometry = geometry, Row = groupRow };
                        elements.Add (element);
                        await evaluate (element).ConfigureAwait (false);
                    }).ConfigureAwait (false);
                }
                return elements;
            }

            foreach (var embedded in Embedded) {
                var element = embedded.ToElement (geographic);
                if (element.Geometry != null)
                    elements.Add (element);
            }
            if (_spatialDataSet != null)
                elements.AddRange (_spatialDataSet.Elements (rpt, geographic));

            if (DataRegion != null && _bindings.Count > 0) {
                var index = new Dictionary<string, List<MapSpatialElement>> (StringComparer.Ordinal);
                foreach (var element in elements) {
                    var key = Key (_bindings.ConvertAll (b => element.Field (b.FieldName)));
                    if (!index.TryGetValue (key, out var matches))
                        index[key] = matches = new List<MapSpatialElement> ();
                    matches.Add (element);
                }
                await DataRegion.ForEachGroup (rpt, row, async groupRow => {
                    var values = new List<object> (_bindings.Count);
                    foreach (var (_, binding) in _bindings)
                        values.Add (await binding.Evaluate (rpt, groupRow).ConfigureAwait (false));
                    if (!index.TryGetValue (Key (values), out var matches))
                        return;
                    foreach (var element in matches) {
                        if (element.Row != null)
                            continue;   // the first group bound to a shape is its data
                        element.Row = groupRow;
                        await evaluate (element).ConfigureAwait (false);
                    }
                }).ConfigureAwait (false);
            }

            foreach (var element in elements) {
                if (element.Row == null)
                    await evaluate (element).ConfigureAwait (false);
            }
            return elements;
        }

        /// <summary>The binding key of the values: numbers by value, anything else as trimmed, upper-cased text.</summary>
        internal static string Key (IEnumerable<object> values)
        {
            var key = new System.Text.StringBuilder ();
            foreach (var value in values) {
                if (value == null || value is DBNull)
                    key.Append ('\u0000');
                else if (MapAppearanceRule.TryNumber (value, out var number))
                    key.Append ('#').Append (number.ToString ("R", CultureInfo.InvariantCulture));
                else
                    key.Append ('$').Append (Convert.ToString (value, CultureInfo.InvariantCulture).Trim ().ToUpperInvariant ());
                key.Append ('\u001F');
            }
            return key.ToString ();
        }

        /// <summary>Projects each ring of the geometry with the viewport.</summary>
        protected static List<MapPoint[]> Project (MapViewport viewport, List<MapPoint[]> rings)
        {
            var projected = new List<MapPoint[]> (rings.Count);
            foreach (var ring in rings) {
                var points = new MapPoint[ring.Length];
                for (var i = 0; i < ring.Length; i++)
                    points[i] = viewport.Project (ring[i]);
                projected.Add (points);
            }
            return projected;
        }
    }
}
