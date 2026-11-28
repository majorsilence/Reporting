using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A point layer (MapPointLayer): a marker at each point of the layer's spatial elements,
    /// coloured, sized and shaped by the layer's rules (MapPointRules: a colour rule, MapSizeRule
    /// and MapMarkerRule), or by the point template where a rule gives nothing.
    /// </summary>
    /// <remarks>
    /// The template is a MapMarkerTemplate (MapPointTemplate in some writers): its MapMarker's
    /// MapMarkerStyle, its Size (5.25pt when not given), and its Style, whose BackgroundColor fills
    /// the marker and whose Border outlines it (dim grey, half a point, unless it says otherwise).
    /// A point's own template (UseCustomPointTemplate) replaces the layer's. Elements that are not
    /// points (lines, polygons) are not drawn by a point layer. Labels, offsets, marker images and
    /// actions are not drawn.
    /// </remarks>
    internal sealed class MapPointLayer : MapVectorLayer
    {
        internal static readonly Color DefaultFill = Color.FromArgb (0xFF, 0x41, 0x8C, 0xF0);
        internal const float DefaultSizePoints = 5.25f;
        internal const float DefaultStartSize = 5f, DefaultEndSize = 20f;

        private MapMarkerTemplate _template;
        internal readonly MapLayerRules Rules = new MapLayerRules ();

        internal MapPointLayer (ReportDefn r, ReportLink p, XmlElement xNode) : base (r, p, xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (VectorLayerElement (node) || EmbeddedElements (node))
                    continue;
                switch (node.LocalName) {
                    case "MapMarkerTemplate":
                    case "MapPointTemplate":
                        _template = new MapMarkerTemplate (r, this, node);
                        break;
                    case "MapPointRules":
                        Rules.Read (r, this, node, markers: true);
                        break;
                    default:
                        OwnerReport.rl.LogError (4, "Unknown MapPointLayer element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        protected override bool EmbeddedElements (XmlNode node)
        {
            if (node.LocalName != "MapPoints")
                return false;
            foreach (XmlNode point in node.ChildNodes) {
                if (point.NodeType == XmlNodeType.Element && point.LocalName == "MapPoint")
                    Embedded.Add (new MapEmbeddedPoint (OwnerReport, this, point));
            }
            return true;
        }

        internal override async Task FinalPass ()
        {
            await base.FinalPass ().ConfigureAwait (false);     // finds the data region the template and rule expressions run in
            if (_template != null)
                await _template.FinalPass ().ConfigureAwait (false);
            await Rules.FinalPass ().ConfigureAwait (false);
        }

        internal override async Task Build (Report rpt, Row row, MapScene scene)
        {
            var points = new List<(MapSpatialElement Element, MapMarkerTemplate.Look Look)> ();
            var run = new MapLayerRules.Run (Rules);
            var hasData = DataRegion != null;

            await Bind (rpt, row, scene.Viewport.Geographic, async element => {
                if (element.Geometry.Points.Count == 0)
                    return;
                var template = (element.Embedded as MapEmbeddedPoint)?.CustomTemplate ?? _template;
                var look = template == null ? null : await template.Evaluate (rpt, element.Row).ConfigureAwait (false);
                if (look != null && look.Hidden)
                    return;
                points.Add ((element, look));
                await run.Add (rpt, element, hasData).ConfigureAwait (false);
            }).ConfigureAwait (false);

            await run.Apply (rpt, Name, scene, DefaultStartSize, DefaultEndSize).ConfigureAwait (false);

            for (var i = 0; i < points.Count; i++) {
                var (element, look) = points[i];
                var fill = run.ColorResult?.BucketFor (i)?.Color ?? Color.Empty;
                if (fill.IsEmpty)
                    fill = look?.Fill.IsEmpty == false ? look.Fill : DefaultFill;
                var size = run.SizeResult?.BucketFor (i)?.SizePoints ?? float.NaN;
                if (float.IsNaN (size))
                    size = look?.SizePoints ?? DefaultSizePoints;
                var marker = run.MarkerResult?.BucketFor (i)?.Marker ?? look?.Marker ?? MapMarkerStyle.Circle;

                foreach (var point in element.Geometry.Points) {
                    scene.Drawables.Add (new MapMarkerShape (scene.Viewport.Project (point)) {
                        Marker = marker,
                        SizePoints = size,
                        Fill = Fade (fill),
                        Border = Fade (look?.Border ?? MapPolygonLayer.DefaultBorder),
                        BorderPoints = look?.BorderPoints ?? MapPolygonLayer.DefaultBorderPoints,
                    });
                }
            }
        }
    }

    /// <summary>
    /// A point template (MapMarkerTemplate, or the MapPointTemplate or MapCenterPointTemplate
    /// that holds one): the marker, its size, its style, and whether it is hidden.
    /// </summary>
    internal sealed class MapMarkerTemplate : ReportLink
    {
        private Style _style;
        private Expression _size;
        private Expression _hidden;
        private MapMarkerStyle? _marker;

        internal MapMarkerTemplate (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Read (xNode);
        }

        private void Read (XmlNode xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                switch (node.LocalName) {
                    case "MapMarkerTemplate":
                        Read (node);    // a MapPointTemplate wrapping the marker template
                        break;
                    case "MapMarker":
                        _marker = MapMarkers.OfMapMarker (node) ?? _marker;
                        break;
                    case "Size":
                        _size = new Expression (OwnerReport, this, node, ExpressionType.String);
                        break;
                    case "Style":
                        _style = new Style (OwnerReport, this, node);
                        break;
                    case "Hidden":
                        _hidden = new Expression (OwnerReport, this, node, ExpressionType.Boolean);
                        break;
                    // Read but not drawn: see MapSupport for what is reported.
                    case "Label":
                    case "LabelPlacement":
                    case "OffsetX":
                    case "OffsetY":
                    case "ToolTip":
                    case "ActionInfo":
                    case "DataElementLabel":
                    case "DataElementName":
                    case "DataElementOutput":
                        break;
                    default:
                        OwnerReport.rl.LogError (4, "Unknown " + xNode.Name + " element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        internal override async Task FinalPass ()
        {
            if (_style != null)
                await _style.FinalPass ().ConfigureAwait (false);
            if (_size != null)
                await _size.FinalPass ().ConfigureAwait (false);
            if (_hidden != null)
                await _hidden.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>What the template gives one element; null for anything it does not say.</summary>
        internal sealed class Look
        {
            internal bool Hidden;
            internal MapMarkerStyle? Marker;
            internal float? SizePoints;
            internal Color Fill = Color.Empty;
            internal Color? Border;
            internal float? BorderPoints;
        }

        internal async Task<Look> Evaluate (Report rpt, Row row)
        {
            var look = new Look {
                Marker = _marker,
                SizePoints = await MapMarkers.Points (_size, rpt, row).ConfigureAwait (false),
                Hidden = _hidden != null && await _hidden.EvaluateBoolean (rpt, row).ConfigureAwait (false),
            };
            if (_style != null) {
                var style = await _style.GetStyleInfo (rpt, row).ConfigureAwait (false);
                look.Fill = style.BackgroundColor;
                if (_style.BorderColor != null)
                    look.Border = style.BColorTop;
                if (_style.BorderWidth != null)
                    look.BorderPoints = style.BWidthTop;
                if (_style.BorderStyle != null && style.BStyleTop == BorderStyleEnum.None)
                    look.BorderPoints = 0;
            }
            return look;
        }
    }

    /// <summary>An embedded MapPoint, which can carry its own template.</summary>
    internal sealed class MapEmbeddedPoint : MapEmbeddedElement
    {
        private bool _useCustomTemplate;
        private MapMarkerTemplate _customTemplate;

        internal MapEmbeddedPoint (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
        }

        /// <summary>The point's own template when it says to use it; null for the layer's.</summary>
        internal MapMarkerTemplate CustomTemplate => _useCustomTemplate ? _customTemplate : null;

        protected override bool ElementOfKind (XmlNode node)
        {
            switch (node.LocalName) {
                case "UseCustomPointTemplate":
                    _useCustomTemplate = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "MapMarkerTemplate":
                case "MapPointTemplate":
                    _customTemplate = new MapMarkerTemplate (OwnerReport, this, node);
                    return true;
                default:
                    return false;
            }
        }

        internal override async Task FinalPass ()
        {
            if (_customTemplate != null)
                await _customTemplate.FinalPass ().ConfigureAwait (false);
        }
    }
}
