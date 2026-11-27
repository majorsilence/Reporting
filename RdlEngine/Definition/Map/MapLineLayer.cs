using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// A line layer (MapLineLayer): the lines of the layer's spatial elements, coloured and sized
    /// by the layer's rules (MapLineRules: a colour rule and MapSizeRule, which sets the width), or
    /// by the line template where a rule gives nothing.
    /// </summary>
    /// <remarks>
    /// The template (MapLineTemplate) gives the Width (3.75pt when not given) and, in its Style,
    /// the colour: BackgroundColor, or the Border colour when there is no background; and the
    /// dash: a Dashed or Dotted border style draws the line so. A line's own template
    /// (UseCustomLineTemplate) replaces the layer's. Polygon rings in a line layer are drawn as
    /// closed lines; points are not drawn. Labels and actions are not drawn.
    /// </remarks>
    internal sealed class MapLineLayer : MapVectorLayer
    {
        internal static readonly Color DefaultColor = Color.FromArgb (0xFF, 0x41, 0x69, 0xE1);
        internal const float DefaultWidthPoints = 3.75f;
        internal const float DefaultStartWidth = 1f, DefaultEndWidth = 8f;

        private MapLineTemplate _template;
        internal readonly MapLayerRules Rules = new MapLayerRules ();

        internal MapLineLayer (ReportDefn r, ReportLink p, XmlElement xNode) : base (r, p, xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (VectorLayerElement (node) || EmbeddedElements (node))
                    continue;
                switch (node.LocalName) {
                    case "MapLineTemplate":
                        _template = new MapLineTemplate (r, this, node);
                        break;
                    case "MapLineRules":
                        Rules.Read (r, this, node, markers: false);
                        break;
                    default:
                        OwnerReport.rl.LogError (4, "Unknown MapLineLayer element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        protected override bool EmbeddedElements (XmlNode node)
        {
            if (node.LocalName != "MapLines")
                return false;
            foreach (XmlNode line in node.ChildNodes) {
                if (line.NodeType == XmlNodeType.Element && line.LocalName == "MapLine")
                    Embedded.Add (new MapEmbeddedLine (OwnerReport, this, line));
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
            var lines = new List<(MapSpatialElement Element, MapLineTemplate.Look Look)> ();
            var run = new MapLayerRules.Run (Rules);
            var hasData = DataRegion != null;

            await Bind (rpt, row, scene.Viewport.Geographic, async element => {
                if (element.Geometry.Lines.Count == 0 && element.Geometry.Rings.Count == 0)
                    return;
                var template = (element.Embedded as MapEmbeddedLine)?.CustomTemplate ?? _template;
                var look = template == null ? null : await template.Evaluate (rpt, element.Row).ConfigureAwait (false);
                if (look != null && look.Hidden)
                    return;
                lines.Add ((element, look));
                await run.Add (rpt, element, hasData).ConfigureAwait (false);
            }).ConfigureAwait (false);

            await run.Apply (rpt, Name, scene, DefaultStartWidth, DefaultEndWidth).ConfigureAwait (false);

            for (var i = 0; i < lines.Count; i++) {
                var (element, look) = lines[i];
                var color = run.ColorResult?.BucketFor (i)?.Color ?? Color.Empty;
                if (color.IsEmpty)
                    color = look?.Color.IsEmpty == false ? look.Color : DefaultColor;
                var width = run.SizeResult?.BucketFor (i)?.SizePoints ?? float.NaN;
                if (float.IsNaN (width))
                    width = look?.WidthPoints ?? DefaultWidthPoints;
                var dash = look?.Dash ?? BorderStyleEnum.Solid;

                if (element.Geometry.Lines.Count > 0)
                    scene.Drawables.Add (new MapLineShape (Project (scene.Viewport, element.Geometry.Lines), false) { Color = Fade (color), WidthPoints = width, Dash = dash });
                if (element.Geometry.Rings.Count > 0)
                    scene.Drawables.Add (new MapLineShape (Project (scene.Viewport, element.Geometry.Rings), true) { Color = Fade (color), WidthPoints = width, Dash = dash });
            }
        }
    }

    /// <summary>A line template (MapLineTemplate): the width, the style's colour and dash, and whether it is hidden.</summary>
    internal sealed class MapLineTemplate : ReportLink
    {
        private Style _style;
        private Expression _width;
        private Expression _hidden;

        internal MapLineTemplate (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                switch (node.LocalName) {
                    case "Width":
                        _width = new Expression (OwnerReport, this, node, ExpressionType.String);
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
                        OwnerReport.rl.LogError (4, "Unknown MapLineTemplate element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        internal override async Task FinalPass ()
        {
            if (_style != null)
                await _style.FinalPass ().ConfigureAwait (false);
            if (_width != null)
                await _width.FinalPass ().ConfigureAwait (false);
            if (_hidden != null)
                await _hidden.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>What the template gives one element; null or empty for anything it does not say.</summary>
        internal sealed class Look
        {
            internal bool Hidden;
            internal float? WidthPoints;
            internal Color Color = Color.Empty;
            internal BorderStyleEnum Dash = BorderStyleEnum.Solid;
        }

        internal async Task<Look> Evaluate (Report rpt, Row row)
        {
            var look = new Look {
                WidthPoints = await MapMarkers.Points (_width, rpt, row).ConfigureAwait (false),
                Hidden = _hidden != null && await _hidden.EvaluateBoolean (rpt, row).ConfigureAwait (false),
            };
            if (_style != null) {
                var style = await _style.GetStyleInfo (rpt, row).ConfigureAwait (false);
                look.Color = style.BackgroundColor;
                if (look.Color.IsEmpty && _style.BorderColor != null)
                    look.Color = style.BColorTop;
                if (_style.BorderStyle != null && (style.BStyleTop == BorderStyleEnum.Dashed || style.BStyleTop == BorderStyleEnum.Dotted))
                    look.Dash = style.BStyleTop;
            }
            return look;
        }
    }

    /// <summary>An embedded MapLine, which can carry its own template.</summary>
    internal sealed class MapEmbeddedLine : MapEmbeddedElement
    {
        private bool _useCustomTemplate;
        private MapLineTemplate _customTemplate;

        internal MapEmbeddedLine (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
        }

        /// <summary>The line's own template when it says to use it; null for the layer's.</summary>
        internal MapLineTemplate CustomTemplate => _useCustomTemplate ? _customTemplate : null;

        protected override bool ElementOfKind (XmlNode node)
        {
            switch (node.LocalName) {
                case "UseCustomLineTemplate":
                    _useCustomTemplate = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "MapLineTemplate":
                    _customTemplate = new MapLineTemplate (OwnerReport, this, node);
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
