using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A polygon layer (MapPolygonLayer): shapes filled by the layer's colour rule (a
    /// choropleth), or by the polygon template's own style where the rule gives no colour.
    /// </summary>
    /// <remarks>
    /// The fill is the colour rule's bucket colour for the shape's value; a shape with no value,
    /// or one outside the rule's range, takes the template's BackgroundColor (light grey when it
    /// has none). The outline is the template's border: dim grey, half a point, unless the template
    /// says otherwise. A polygon's own template (UseCustomPolygonTemplate) replaces the layer's.
    /// Labels, centre points and actions are not drawn.
    /// </remarks>
    internal sealed class MapPolygonLayer : MapVectorLayer
    {
        internal static readonly Color DefaultFill = Color.FromArgb (0xE0, 0xE0, 0xE0);
        internal static readonly Color DefaultBorder = Color.DimGray;
        internal const float DefaultBorderPoints = 0.5f;

        private Style _template;
        internal MapColorRule ColorRule { get; private set; }

        internal MapPolygonLayer (ReportDefn r, ReportLink p, XmlElement xNode) : base (r, p, xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (VectorLayerElement (node) || EmbeddedElements (node))
                    continue;
                switch (node.LocalName) {
                    case "MapPolygonTemplate":
                        _template = TemplateStyle (r, this, node);
                        break;
                    case "MapPolygonRules":
                        foreach (XmlNode rule in node.ChildNodes) {
                            if (rule.NodeType != XmlNodeType.Element)
                                continue;
                            var colorRule = MapColorRule.Create (r, this, rule);
                            if (colorRule != null)
                                ColorRule ??= colorRule;
                        }
                        break;
                    case "MapCenterPointRules":
                    case "MapCenterPointTemplate":
                        break;      // centre points are not drawn; MapSupport reports the rules
                    default:
                        OwnerReport.rl.LogError (4, "Unknown MapPolygonLayer element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        /// <summary>The Style of a MapPolygonTemplate; its other settings (labels, actions, ...) are not used.</summary>
        internal static Style TemplateStyle (ReportDefn r, ReportLink p, XmlNode template)
        {
            var style = MapEmbeddedElement.Child (template, "Style");
            return style == null ? null : new Style (r, p, style);
        }

        protected override bool EmbeddedElements (XmlNode node)
        {
            if (node.LocalName != "MapPolygons")
                return false;
            foreach (XmlNode polygon in node.ChildNodes) {
                if (polygon.NodeType == XmlNodeType.Element && polygon.LocalName == "MapPolygon")
                    Embedded.Add (new MapEmbeddedPolygon (OwnerReport, this, polygon));
            }
            return true;
        }

        internal override async Task FinalPass ()
        {
            await base.FinalPass ().ConfigureAwait (false);     // finds the data region the template and rule expressions run in
            if (_template != null)
                await _template.FinalPass ().ConfigureAwait (false);
            if (ColorRule != null)
                await ColorRule.FinalPass ().ConfigureAwait (false);
        }

        internal override async Task Build (Report rpt, Row row, MapScene scene)
        {
            var shapes = new List<(MapSpatialElement Element, StyleInfo Style)> ();
            var values = new List<object> ();
            var hasData = DataRegion != null;

            await Bind (rpt, row, scene.Viewport.Geographic, async element => {
                if (element.Geometry.Rings.Count == 0)
                    return;
                var template = (element.Embedded as MapEmbeddedPolygon)?.CustomTemplate ?? _template;
                var style = template == null ? null : await template.GetStyleInfo (rpt, element.Row).ConfigureAwait (false);
                object value = null;
                if (ColorRule != null) {
                    value = ColorRule.DataValue == null
                        ? shapes.Count.ToString (CultureInfo.InvariantCulture)    // a palette with no value: a colour per shape
                        : await ColorRule.ValueFor (rpt, element.Row, element, hasData).ConfigureAwait (false);
                }
                shapes.Add ((element, style));
                values.Add (value);
            }).ConfigureAwait (false);

            MapRuleResult result = null;
            if (ColorRule != null) {
                result = await ColorRule.Assign (rpt, values).ConfigureAwait (false);
                result.LayerName = Name;
                scene.Rules.Add (result);
            }

            for (var i = 0; i < shapes.Count; i++) {
                var (element, style) = shapes[i];
                var template = (element.Embedded as MapEmbeddedPolygon)?.CustomTemplate ?? _template;
                var fill = result?.BucketFor (i)?.Color ?? Color.Empty;
                if (fill.IsEmpty)
                    fill = style != null && !style.BackgroundColor.IsEmpty ? style.BackgroundColor : DefaultFill;

                var border = DefaultBorder;
                var borderPoints = DefaultBorderPoints;
                if (style != null && template != null) {
                    if (template.BorderColor != null)
                        border = style.BColorTop;
                    if (template.BorderWidth != null)
                        borderPoints = style.BWidthTop;
                    if (template.BorderStyle != null && style.BStyleTop == BorderStyleEnum.None)
                        borderPoints = 0;
                }

                scene.Drawables.Add (new MapPolygonShape (Project (scene.Viewport, element.Geometry.Rings)) {
                    Fill = Fade (fill),
                    Border = Fade (border),
                    BorderPoints = borderPoints,
                });
            }
        }
    }

    /// <summary>An embedded MapPolygon, which can carry its own template.</summary>
    internal sealed class MapEmbeddedPolygon : MapEmbeddedElement
    {
        private bool _useCustomTemplate;
        private Style _customTemplate;

        internal MapEmbeddedPolygon (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
        }

        /// <summary>The polygon's own template style when it says to use it; null for the layer's.</summary>
        internal Style CustomTemplate => _useCustomTemplate ? _customTemplate : null;

        protected override bool ElementOfKind (XmlNode node)
        {
            switch (node.LocalName) {
                case "UseCustomPolygonTemplate":
                    _useCustomTemplate = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "MapPolygonTemplate":
                    _customTemplate = MapPolygonLayer.TemplateStyle (OwnerReport, this, node);
                    return true;
                case "UseCustomCenterPointTemplate":
                case "MapCenterPointTemplate":
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
