using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>The shape of a map marker (MapMarkerStyle). Square, which some writers use, is read as Rectangle.</summary>
    internal enum MapMarkerStyle
    {
        None,
        Rectangle,
        Circle,
        Diamond,
        Triangle,
        Trapezoid,
        Star,
        Wedge,
        Pentagon,
        PushPin,
    }

    internal static class MapMarkers
    {
        /// <summary>The marker a MapMarkerStyle value names, or null for an expression or an unknown name.</summary>
        internal static MapMarkerStyle? Parse (string text)
        {
            text = text?.Trim ();
            if (string.IsNullOrEmpty (text) || text.StartsWith ("=", StringComparison.Ordinal))
                return null;
            if (string.Equals (text, "Square", StringComparison.OrdinalIgnoreCase))
                return MapMarkerStyle.Rectangle;
            return Enum.TryParse<MapMarkerStyle> (text, true, out var style) && Enum.IsDefined (typeof (MapMarkerStyle), style)
                ? style : (MapMarkerStyle?)null;
        }

        /// <summary>The marker of a MapMarker element (its MapMarkerStyle; a MapMarkerImage is not drawn).</summary>
        internal static MapMarkerStyle? OfMapMarker (XmlNode mapMarker)
            => Parse (MapEmbeddedElement.Child (mapMarker, "MapMarkerStyle")?.InnerText);

        /// <summary>A size written in the report ("6pt", "0.1in"), in points; null for none or an expression.</summary>
        internal static float? Points (string text)
        {
            text = text?.Trim ();
            if (string.IsNullOrEmpty (text) || text.StartsWith ("=", StringComparison.Ordinal))
                return null;
            var points = new RSize (null, text).Points;
            return points >= 0 ? points : (float?)null;
        }

        internal static async Task<float?> Points (Expression e, Report rpt, Row row)
        {
            if (e == null)
                return null;
            var value = await e.Evaluate (rpt, row).ConfigureAwait (false);
            if (value == null || value is DBNull)
                return null;
            if (MapAppearanceRule.TryNumber (value, out var number))
                return number >= 0 ? (float)number : (float?)null;     // a bare number is points
            return Points (Convert.ToString (value, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A rule that sizes points or lines (MapSizeRule): StartSize for the first bucket to EndSize
    /// for the last, evenly between.
    /// </summary>
    internal sealed class MapSizeRule : MapAppearanceRule
    {
        private float? _start, _end;

        internal MapSizeRule (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Parse (xNode);
        }

        protected override bool SpecificElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "StartSize":
                    _start = MapMarkers.Points (node.InnerText);
                    return true;
                case "EndSize":
                    _end = MapMarkers.Points (node.InnerText);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Classifies the values and gives each bucket its size; the defaults are the layer kind's.</summary>
        internal async Task<MapRuleResult> Assign (Report rpt, IList<object> values, float defaultStart, float defaultEnd)
        {
            var result = await Classify (rpt, values, 5).ConfigureAwait (false);
            var start = _start ?? defaultStart;
            var end = _end ?? defaultEnd;
            var n = result.Buckets.Count;
            for (var i = 0; i < n; i++)
                result.Buckets[i].SizePoints = n == 1 ? start : start + (end - start) * i / (n - 1);
            return result;
        }
    }

    /// <summary>A rule that gives points a marker (MapMarkerRule): the rule's MapMarkers, one per bucket and round again.</summary>
    internal sealed class MapMarkerRule : MapAppearanceRule
    {
        // Used when the rule lists no markers.
        private static readonly MapMarkerStyle[] DefaultMarkers = {
            MapMarkerStyle.Circle, MapMarkerStyle.Rectangle, MapMarkerStyle.Triangle, MapMarkerStyle.Diamond,
            MapMarkerStyle.Star, MapMarkerStyle.Wedge, MapMarkerStyle.Pentagon, MapMarkerStyle.Trapezoid,
        };

        private readonly List<MapMarkerStyle> _markers = new List<MapMarkerStyle> ();

        internal MapMarkerRule (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Parse (xNode);
        }

        protected override bool SpecificElement (XmlNode node)
        {
            if (node.LocalName != "MapMarkers")
                return false;
            foreach (XmlNode marker in node.ChildNodes) {
                if (marker.NodeType == XmlNodeType.Element && marker.LocalName == "MapMarker")
                    _markers.Add (MapMarkers.OfMapMarker (marker) ?? MapMarkerStyle.Circle);
            }
            return true;
        }

        internal async Task<MapRuleResult> Assign (Report rpt, IList<object> values)
        {
            var markers = _markers.Count > 0 ? (IList<MapMarkerStyle>)_markers : DefaultMarkers;
            var result = await Classify (rpt, values, markers.Count).ConfigureAwait (false);
            for (var i = 0; i < result.Buckets.Count; i++)
                result.Buckets[i].Marker = markers[i % markers.Count];
            return result;
        }
    }

    /// <summary>
    /// The rules of a point or line layer (MapPointRules, MapLineRules): at most one colour rule,
    /// one size rule and, for points, one marker rule. Each is classified on its own DataValue.
    /// </summary>
    internal sealed class MapLayerRules
    {
        internal MapColorRule Color;
        internal MapSizeRule Size;
        internal MapMarkerRule Marker;

        internal void Read (ReportDefn r, ReportLink p, XmlNode rules, bool markers)
        {
            foreach (XmlNode rule in rules.ChildNodes) {
                if (rule.NodeType != XmlNodeType.Element)
                    continue;
                var color = MapColorRule.Create (r, p, rule);
                if (color != null) {
                    Color ??= color;
                    continue;
                }
                if (rule.LocalName == "MapSizeRule")
                    Size ??= new MapSizeRule (r, p, rule);
                else if (markers && rule.LocalName == "MapMarkerRule")
                    Marker ??= new MapMarkerRule (r, p, rule);
                else
                    r.rl.LogError (4, "Unknown " + rules.Name + " element '" + rule.Name + "' ignored.");
            }
        }

        internal async Task FinalPass ()
        {
            if (Color != null)
                await Color.FinalPass ().ConfigureAwait (false);
            if (Size != null)
                await Size.FinalPass ().ConfigureAwait (false);
            if (Marker != null)
                await Marker.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>The values of one run of a layer, element by element, and what the rules made of them.</summary>
        internal sealed class Run
        {
            private readonly MapLayerRules _rules;
            private readonly List<object> _color = new List<object> (), _size = new List<object> (), _marker = new List<object> ();
            internal MapRuleResult ColorResult, SizeResult, MarkerResult;

            internal Run (MapLayerRules rules)
            {
                _rules = rules;
            }

            internal int Count => _color.Count;

            /// <summary>Adds the rules' values for the next element.</summary>
            internal async Task Add (Report rpt, MapSpatialElement element, bool hasData)
            {
                var index = _color.Count;
                _color.Add (_rules.Color == null ? null
                    : _rules.Color.DataValue == null ? index.ToString (CultureInfo.InvariantCulture)   // a palette with no value: a colour per element
                    : await _rules.Color.ValueFor (rpt, element.Row, element, hasData).ConfigureAwait (false));
                _size.Add (_rules.Size == null ? null : await _rules.Size.ValueFor (rpt, element.Row, element, hasData).ConfigureAwait (false));
                _marker.Add (_rules.Marker == null ? null : await _rules.Marker.ValueFor (rpt, element.Row, element, hasData).ConfigureAwait (false));
            }

            /// <summary>Applies each rule to its values and keeps its result on the scene, as the polygon layer's colour rule does.</summary>
            internal async Task Apply (Report rpt, string layerName, MapScene scene, float defaultStartSize, float defaultEndSize)
            {
                if (_rules.Color != null)
                    ColorResult = Keep (await _rules.Color.Assign (rpt, _color).ConfigureAwait (false), layerName, scene);
                if (_rules.Size != null)
                    SizeResult = Keep (await _rules.Size.Assign (rpt, _size, defaultStartSize, defaultEndSize).ConfigureAwait (false), layerName, scene);
                if (_rules.Marker != null)
                    MarkerResult = Keep (await _rules.Marker.Assign (rpt, _marker).ConfigureAwait (false), layerName, scene);
            }

            private static MapRuleResult Keep (MapRuleResult result, string layerName, MapScene scene)
            {
                result.LayerName = layerName;
                scene.Rules.Add (result);
                return result;
            }
        }
    }
}
