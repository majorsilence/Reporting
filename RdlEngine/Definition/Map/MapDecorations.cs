using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// The map's decorations: MapLegends, MapTitles, MapColorScale and MapDistanceScale. Read with
    /// the map; for each instance they are evaluated, laid out around or over the viewport, and
    /// added to the scene as overlays.
    /// </summary>
    /// <remarks>
    /// Layout, kept simple:
    /// <list type="bullet">
    /// <item>A decoration with a MapLocation is drawn there (its MapSize, or the size it needs), over everything.</item>
    /// <item>One docked outside the viewport (DockOutsideViewport) takes a band off the viewport's
    /// edge named by its Position (TopCenter: the top, LeftBottom: the left, ...), aligned along
    /// that edge by the rest of the name; the viewport shrinks and the map is fitted to what is left.
    /// Titles are placed first (outermost), then legends, the colour scale and the distance scale.</item>
    /// <item>One docked inside (the default) is drawn over the viewport at its Position; several at
    /// the same Position are stacked inwards from the edge.</item>
    /// </list>
    /// Element names and defaults follow MS-RDL 2010 (MapDockableSubItem: Position TopCenter,
    /// DockOutsideViewport false, Hidden false) as known; no schema file is in the repository.
    /// </remarks>
    internal sealed class MapDecorations
    {
        private readonly List<MapLegendDefn> _legends = new List<MapLegendDefn> ();
        private readonly List<MapTitleDefn> _titles = new List<MapTitleDefn> ();
        private MapColorScaleDefn _colorScale;
        private MapDistanceScaleDefn _distanceScale;

        internal IReadOnlyList<MapLegendDefn> Legends => _legends;

        /// <summary>Reads a decoration element of the map; false for any other element.</summary>
        internal bool Read (ReportDefn r, ReportLink p, XmlNode node)
        {
            switch (node.LocalName) {
                case "MapLegends":
                    foreach (XmlNode legend in node.ChildNodes) {
                        if (legend.NodeType == XmlNodeType.Element && legend.LocalName == "MapLegend")
                            _legends.Add (new MapLegendDefn (r, p, legend));
                    }
                    return true;
                case "MapTitles":
                    foreach (XmlNode title in node.ChildNodes) {
                        if (title.NodeType == XmlNodeType.Element && title.LocalName == "MapTitle")
                            _titles.Add (new MapTitleDefn (r, p, title));
                    }
                    return true;
                case "MapColorScale":
                    _colorScale = new MapColorScaleDefn (r, p, node);
                    return true;
                case "MapDistanceScale":
                    _distanceScale = new MapDistanceScaleDefn (r, p, node);
                    return true;
                default:
                    return false;
            }
        }

        internal async Task FinalPass ()
        {
            foreach (var legend in _legends)
                await legend.FinalPass ().ConfigureAwait (false);
            foreach (var title in _titles)
                await title.FinalPass ().ConfigureAwait (false);
            if (_colorScale != null)
                await _colorScale.FinalPass ().ConfigureAwait (false);
            if (_distanceScale != null)
                await _distanceScale.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>
        /// The legend a rule's buckets go to: the one its LegendName names, or the first legend when
        /// it names none. Null when it names a legend the map does not have.
        /// </summary>
        internal MapLegendDefn LegendFor (MapAppearanceRule rule)
        {
            if (_legends.Count == 0)
                return null;
            if (string.IsNullOrEmpty (rule?.LegendName))
                return _legends[0];
            return _legends.FirstOrDefault (l => string.Equals (l.Name, rule.LegendName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Evaluates the shown decorations for one instance, lays them out and adds them to the scene.</summary>
        internal async Task Build (Report rpt, Row row, MapScene scene)
        {
            var placed = new List<(MapDecorationOverlay Overlay, MapPlacement Placement)> ();

            foreach (var title in _titles) {
                if (await title.IsHidden (rpt, row).ConfigureAwait (false))
                    continue;
                var overlay = await title.Evaluate (rpt, row).ConfigureAwait (false);
                if (overlay != null)
                    placed.Add ((overlay, title.Placement (scene)));
            }

            foreach (var legend in _legends) {
                if (await legend.IsHidden (rpt, row).ConfigureAwait (false))
                    continue;
                var rules = scene.Rules.Where (r => r.Buckets.Count > 0 && LegendFor (r.Rule) == legend).ToList ();
                var overlay = await legend.Evaluate (rpt, row, rules).ConfigureAwait (false);
                if (overlay != null)
                    placed.Add ((overlay, legend.Placement (scene)));
            }

            if (_colorScale != null && !await _colorScale.IsHidden (rpt, row).ConfigureAwait (false)) {
                var rule = scene.Rules.FirstOrDefault (r => r.Buckets.Count > 0 && r.Rule is MapColorRule c && c.ShowInColorScale);
                if (rule != null)
                    placed.Add ((await _colorScale.Evaluate (rpt, row, rule).ConfigureAwait (false), _colorScale.Placement (scene)));
            }

            // A distance needs a known unit: geographic coordinates. Planar ones are drawn without it
            // (MapSupport says so).
            if (_distanceScale != null && scene.Viewport.Geographic && !await _distanceScale.IsHidden (rpt, row).ConfigureAwait (false))
                placed.Add ((await _distanceScale.Evaluate (rpt, row, scene.Viewport).ConfigureAwait (false), _distanceScale.Placement (scene)));

            MapDecorationLayout.Arrange (scene, placed);
            foreach (var (overlay, _) in placed)
                scene.Overlays.Add (overlay);
        }
    }

    /// <summary>Where a decoration goes: a docking Position (edge and alignment), inside or outside the viewport, or a fixed location; sizes in points.</summary>
    internal sealed class MapPlacement
    {
        internal string Edge = "Top";          // Top, Bottom, Left, Right
        internal string Align = "Center";      // along the edge: Left, Center, Right (Top/Bottom edges) or Top, Center, Bottom
        internal bool Outside;
        internal SKPoint? Location;
        internal SKSize? Size;

        internal string Slot => Edge + Align;

        /// <summary>Splits an MS-RDL Position (TopCenter, LeftBottom, ...); anything else is TopCenter.</summary>
        internal static (string Edge, string Align) ParsePosition (string position)
        {
            position = position?.Trim () ?? string.Empty;
            foreach (var edge in new[] { "Top", "Bottom", "Left", "Right" }) {
                if (!position.StartsWith (edge, StringComparison.OrdinalIgnoreCase))
                    continue;
                var rest = position.Substring (edge.Length);
                var aligns = edge == "Top" || edge == "Bottom" ? new[] { "Left", "Center", "Right" } : new[] { "Top", "Center", "Bottom" };
                foreach (var a in aligns) {
                    if (string.Equals (rest, a, StringComparison.OrdinalIgnoreCase))
                        return (edge, a);
                }
                return (edge, "Center");
            }
            return ("Top", "Center");
        }
    }

    /// <summary>Places the decorations (see <see cref="MapDecorations"/> for the rules) and shrinks the scene's viewport for the docked-outside ones.</summary>
    internal static class MapDecorationLayout
    {
        internal const float Gap = 3f;              // points between a decoration and what is next to it
        private const float MinimumViewport = 0.2f;  // the viewport keeps at least this share of each side

        internal static void Arrange (MapScene scene, List<(MapDecorationOverlay Overlay, MapPlacement Placement)> items)
        {
            float w = scene.WidthPoints, h = scene.HeightPoints;
            if (w <= 0 || h <= 0)
                return;
            var view = SKRect.Create ((float)(w * scene.ViewLeft / 100), (float)(h * scene.ViewTop / 100),
                (float)(w * scene.ViewWidth / 100), (float)(h * scene.ViewHeight / 100));
            float minWidth = view.Width * MinimumViewport, minHeight = view.Height * MinimumViewport;

            var inside = new List<(MapDecorationOverlay, MapPlacement)> ();
            foreach (var (overlay, p) in items) {
                overlay.ItemWidthPoints = w;
                overlay.ItemHeightPoints = h;
                if (p.Location != null) {
                    var at = p.Location.Value;
                    var size = SizeOf (overlay, p, Math.Max (1, w - at.X), Math.Max (1, h - at.Y));
                    overlay.Bounds = SKRect.Create (at, size);
                    continue;
                }
                if (!p.Outside) {
                    inside.Add ((overlay, p));
                    continue;
                }
                var horizontal = p.Edge == "Top" || p.Edge == "Bottom";
                var maxW = horizontal ? view.Width : Math.Max (1, view.Width / 2);
                var maxH = horizontal ? Math.Max (1, view.Height / 2) : view.Height;
                var s = Clamp (SizeOf (overlay, p, maxW, maxH), maxW, maxH);
                // Not enough room left outside: draw it over the viewport instead.
                if ((horizontal && view.Height - s.Height - Gap < minHeight) || (!horizontal && view.Width - s.Width - Gap < minWidth)) {
                    inside.Add ((overlay, p));
                    continue;
                }
                switch (p.Edge) {
                    case "Top":
                        overlay.Bounds = SKRect.Create (AlignX (view, s.Width, p.Align, 0), view.Top, s.Width, s.Height);
                        view.Top += s.Height + Gap;
                        break;
                    case "Bottom":
                        overlay.Bounds = SKRect.Create (AlignX (view, s.Width, p.Align, 0), view.Bottom - s.Height, s.Width, s.Height);
                        view.Bottom -= s.Height + Gap;
                        break;
                    case "Left":
                        overlay.Bounds = SKRect.Create (view.Left, AlignY (view, s.Height, p.Align, 0), s.Width, s.Height);
                        view.Left += s.Width + Gap;
                        break;
                    default:
                        overlay.Bounds = SKRect.Create (view.Right - s.Width, AlignY (view, s.Height, p.Align, 0), s.Width, s.Height);
                        view.Right -= s.Width + Gap;
                        break;
                }
            }

            scene.ViewLeft = view.Left / w * 100;
            scene.ViewTop = view.Top / h * 100;
            scene.ViewWidth = view.Width / w * 100;
            scene.ViewHeight = view.Height / h * 100;

            // Inside: over the viewport, stacked inwards from the edge at each position.
            var offsets = new Dictionary<string, float> (StringComparer.Ordinal);
            foreach (var (overlay, p) in inside) {
                offsets.TryGetValue (p.Slot, out var offset);
                var horizontal = p.Edge == "Top" || p.Edge == "Bottom";
                var maxW = Math.Max (1, view.Width - 2 * Gap - (horizontal ? 0 : offset));
                var maxH = Math.Max (1, view.Height - 2 * Gap - (horizontal ? offset : 0));
                if (horizontal)
                    maxH = Math.Max (1, Math.Min (maxH, view.Height / 2));
                else
                    maxW = Math.Max (1, Math.Min (maxW, view.Width / 2));
                var s = Clamp (SizeOf (overlay, p, maxW, maxH), maxW, maxH);
                switch (p.Edge) {
                    case "Top":
                        overlay.Bounds = SKRect.Create (AlignX (view, s.Width, p.Align, Gap), view.Top + Gap + offset, s.Width, s.Height);
                        offsets[p.Slot] = offset + s.Height + Gap;
                        break;
                    case "Bottom":
                        overlay.Bounds = SKRect.Create (AlignX (view, s.Width, p.Align, Gap), view.Bottom - Gap - offset - s.Height, s.Width, s.Height);
                        offsets[p.Slot] = offset + s.Height + Gap;
                        break;
                    case "Left":
                        overlay.Bounds = SKRect.Create (view.Left + Gap + offset, AlignY (view, s.Height, p.Align, Gap), s.Width, s.Height);
                        offsets[p.Slot] = offset + s.Width + Gap;
                        break;
                    default:
                        overlay.Bounds = SKRect.Create (view.Right - Gap - offset - s.Width, AlignY (view, s.Height, p.Align, Gap), s.Width, s.Height);
                        offsets[p.Slot] = offset + s.Width + Gap;
                        break;
                }
            }
        }

        /// <summary>The decoration's MapSize, or the size it wants; either way it lays out its content in that room.</summary>
        private static SKSize SizeOf (MapDecorationOverlay overlay, MapPlacement p, float maxW, float maxH)
        {
            if (p.Size is SKSize given) {
                overlay.Measure (given.Width, given.Height);
                return given;
            }
            return overlay.Measure (maxW, maxH);
        }

        private static SKSize Clamp (SKSize s, float maxW, float maxH)
            => new SKSize (Math.Max (1, Math.Min (s.Width, maxW)), Math.Max (1, Math.Min (s.Height, maxH)));

        private static float AlignX (SKRect r, float width, string align, float gap)
            => align == "Left" ? r.Left + gap : align == "Right" ? r.Right - gap - width : r.MidX - width / 2;

        private static float AlignY (SKRect r, float height, string align, float gap)
            => align == "Top" ? r.Top + gap : align == "Bottom" ? r.Bottom - gap - height : r.MidY - height / 2;
    }

    /// <summary>
    /// What every map decoration has (MapSubItem and MapDockableSubItem): a Style, where it goes
    /// (Position, DockOutsideViewport, or MapLocation and MapSize), and whether it is Hidden.
    /// Margins, ZIndex and actions are read and not used.
    /// </summary>
    internal abstract class MapSubItem : ReportLink
    {
        internal readonly string Name;
        internal Style Style;
        internal Expression Hidden;
        internal string Position = "TopCenter";
        internal bool DockOutsideViewport;
        private (double X, double Y, string Unit)? _location;
        private (double W, double H, string Unit)? _size;

        protected MapSubItem (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Name = (xNode as XmlElement)?.GetAttribute ("Name") ?? string.Empty;
        }

        protected void Parse (XmlNode xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (!SubItemElement (node) && !OwnElement (node))
                    OwnerReport.rl.LogError (4, "Unknown " + xNode.Name + " element '" + node.Name + "' ignored.");
            }
        }

        private bool SubItemElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "Style":
                    Style = new Style (OwnerReport, this, node);
                    return true;
                case "Hidden":
                    Hidden = new Expression (OwnerReport, this, node, ExpressionType.Boolean);
                    return true;
                case "Position":
                    Position = node.InnerText.Trim ();
                    return true;
                case "DockOutsideViewport":
                    DockOutsideViewport = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "MapLocation": {
                    var x = Number (MapEmbeddedElement.Child (node, "Left")?.InnerText);
                    var y = Number (MapEmbeddedElement.Child (node, "Top")?.InnerText);
                    if (x != null && y != null)
                        _location = (x.Value, y.Value, MapEmbeddedElement.Child (node, "Unit")?.InnerText?.Trim ());
                    return true;
                }
                case "MapSize": {
                    var w = Number (MapEmbeddedElement.Child (node, "Width")?.InnerText);
                    var h = Number (MapEmbeddedElement.Child (node, "Height")?.InnerText);
                    if (w > 0 && h > 0)
                        _size = (w.Value, h.Value, MapEmbeddedElement.Child (node, "Unit")?.InnerText?.Trim ());
                    return true;
                }
                case "LeftMargin":
                case "RightMargin":
                case "TopMargin":
                case "BottomMargin":
                case "ZIndex":
                case "ActionInfo":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Elements of the specific decoration; false for one it does not know.</summary>
        protected abstract bool OwnElement (XmlNode node);

        internal override async Task FinalPass ()
        {
            if (Style != null)
                await Style.FinalPass ().ConfigureAwait (false);
            if (Hidden != null)
                await Hidden.FinalPass ().ConfigureAwait (false);
            await OwnFinalPass ().ConfigureAwait (false);
        }

        protected virtual Task OwnFinalPass () => Task.CompletedTask;

        internal async Task<bool> IsHidden (Report rpt, Row row)
            => Hidden != null && await Hidden.EvaluateBoolean (rpt, row).ConfigureAwait (false);

        internal MapPlacement Placement (MapScene scene)
        {
            var (edge, align) = MapPlacement.ParsePosition (Position);
            var p = new MapPlacement { Edge = edge, Align = align, Outside = DockOutsideViewport };
            if (_location is var (x, y, lu))
                p.Location = new SKPoint (ToPoints (x, lu, scene.WidthPoints), ToPoints (y, lu, scene.HeightPoints));
            if (_size is var (w, h, su))
                p.Size = new SKSize (ToPoints (w, su, scene.WidthPoints), ToPoints (h, su, scene.HeightPoints));
            return p;
        }

        /// <summary>A MapLocation or MapSize value in points: Percent (the default) of the map item, or Inch, Point, Centimeter, Millimeter, Pica.</summary>
        internal static float ToPoints (double value, string unit, float itemPoints)
        {
            switch ((unit ?? "Percent").ToLowerInvariant ()) {
                case "inch": return (float)(value * 72);
                case "point": return (float)value;
                case "centimeter": return (float)(value * 72 / 2.54);
                case "millimeter": return (float)(value * 72 / 25.4);
                case "pica": return (float)(value * 12);
                default: return (float)(value * itemPoints / 100);
            }
        }

        protected static double? Number (string text)
            => double.TryParse (text?.Trim (), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (double?)null;

        /// <summary>A colour written in the report (not an expression), or the fallback.</summary>
        protected static Color ColorOf (Report rpt, string text, Color fallback)
        {
            if (string.IsNullOrWhiteSpace (text) || text.TrimStart ().StartsWith ("=", StringComparison.Ordinal))
                return fallback;
            return XmlUtil.ColorFromHtml (text.Trim (), fallback, rpt);
        }

        protected async Task<StyleInfo> StyleInfoOf (Report rpt, Row row)
            => Style == null ? new StyleInfo () : await Style.GetStyleInfo (rpt, row).ConfigureAwait (false);

        /// <summary>The text style of a Style: its font, or the given size and weight where it sets none.</summary>
        internal static MapTextStyle TextStyle (Style style, StyleInfo si, float defaultSize, bool defaultBold)
            => new MapTextStyle {
                Family = string.IsNullOrWhiteSpace (si.FontFamily) ? "Arial" : si.FontFamily,
                Size = style?.FontSize != null && si.FontSize > 0 ? si.FontSize : defaultSize,
                Bold = style?.FontWeight != null ? si.IsFontBold () : defaultBold,
                Italic = si.FontStyle == FontStyleEnum.Italic,
                Color = si.Color.IsEmpty ? Color.Black : si.Color,
            };

        /// <summary>Background, border and padding of the decoration's box from its style (3pt padding unless it says).</summary>
        protected void ApplyBox (MapDecorationOverlay overlay, StyleInfo si)
        {
            overlay.Background = si.BackgroundColor;
            if (si.BStyleTop != BorderStyleEnum.None && si.BWidthTop > 0) {
                overlay.Border = si.BColorTop;
                overlay.BorderPoints = si.BWidthTop;
            }
            if (Style?.PaddingLeft != null)
                overlay.Padding = si.PaddingLeft;
        }

        /// <summary>A string written in the report, or the expression's value.</summary>
        protected static async Task<string> Text (Report rpt, Row row, Expression e)
        {
            if (e == null)
                return null;
            var source = e.Source;
            if (source == null || !source.TrimStart ().StartsWith ("=", StringComparison.Ordinal))
                return source;
            return await e.EvaluateString (rpt, row).ConfigureAwait (false);
        }
    }

    /// <summary>
    /// A MapLegend: the buckets of the rules that name it (or of the rules that name no legend,
    /// for the first legend), one entry each, with a swatch and the rule's LegendText.
    /// </summary>
    /// <remarks>
    /// Layout Column (and TallTable) lists the entries top to bottom, wrapping into more columns
    /// when the room runs out; Row (and WideTable) left to right, wrapping into more rows;
    /// AutoTable (the default) is a row at TopCenter or BottomCenter and a column anywhere else.
    /// MapLegendTitle's Caption is drawn above the entries, with a line under it unless its
    /// TitleSeparator is None (the default). The legend's Style sets the entries' font, the box's
    /// background and border. AutoFitTextDisabled, MinFontSize, InterlacedRows,
    /// EquallySpacedItems and TextWrapThreshold are read and not used.
    /// </remarks>
    internal sealed class MapLegendDefn : MapSubItem
    {
        internal string Layout = "AutoTable";
        private Expression _caption;
        private Style _titleStyle;
        private string _separator = "None";
        private string _separatorColor;

        internal MapLegendDefn (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            Position = "TopRight";      // the legend's usual place; MS-RDL leaves Position to MapDockableSubItem
            Parse (xNode);
        }

        protected override bool OwnElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "Layout":
                    Layout = node.InnerText.Trim ();
                    return true;
                case "MapLegendTitle":
                    foreach (XmlNode part in node.ChildNodes) {
                        if (part.NodeType != XmlNodeType.Element)
                            continue;
                        switch (part.LocalName) {
                            case "Caption": _caption = new Expression (OwnerReport, this, part, ExpressionType.String); break;
                            case "Style": _titleStyle = new Style (OwnerReport, this, part); break;
                            case "TitleSeparator": _separator = part.InnerText.Trim (); break;
                            case "TitleSeparatorColor": _separatorColor = part.InnerText; break;
                        }
                    }
                    return true;
                case "AutoFitTextDisabled":
                case "MinFontSize":
                case "InterlacedRows":
                case "InterlacedRowsColor":
                case "EquallySpacedItems":
                case "TextWrapThreshold":
                    return true;
                default:
                    return false;
            }
        }

        protected override async Task OwnFinalPass ()
        {
            if (_caption != null)
                await _caption.FinalPass ().ConfigureAwait (false);
            if (_titleStyle != null)
                await _titleStyle.FinalPass ().ConfigureAwait (false);
        }

        /// <summary>True when the entries run left to right.</summary>
        internal bool InRows (string edge, string align)
        {
            switch ((Layout ?? string.Empty).ToLowerInvariant ()) {
                case "row":
                case "widetable":
                    return true;
                case "column":
                case "talltable":
                    return false;
                default:
                    return (edge == "Top" || edge == "Bottom") && align == "Center";
            }
        }

        /// <summary>The legend for one instance, or null when no rule's buckets go to it.</summary>
        internal async Task<MapLegendOverlay> Evaluate (Report rpt, Row row, List<MapRuleResult> rules)
        {
            var entries = new List<MapLegendEntry> ();
            foreach (var result in rules) {
                foreach (var bucket in result.Buckets) {
                    entries.Add (new MapLegendEntry {
                        Swatch = result.Rule?.LegendSwatch (bucket) ?? new MapColorSwatch (bucket.Color),
                        Text = await MapLegendText.For (rpt, result.Rule, bucket).ConfigureAwait (false),
                    });
                }
            }
            if (entries.Count == 0)
                return null;

            var si = await StyleInfoOf (rpt, row).ConfigureAwait (false);
            var (edge, align) = MapPlacement.ParsePosition (Position);
            var overlay = new MapLegendOverlay {
                Name = Name,
                Rows = InRows (edge, align),
                TextStyle = TextStyle (Style, si, 8, false),
                Title = await Text (rpt, row, _caption).ConfigureAwait (false),
                TitleSeparator = !string.IsNullOrEmpty (_separator) && !string.Equals (_separator, "None", StringComparison.OrdinalIgnoreCase),
                SeparatorColor = ColorOf (rpt, _separatorColor, Color.Gray),
            };
            overlay.Entries.AddRange (entries);
            var titleInfo = _titleStyle == null ? new StyleInfo () : await _titleStyle.GetStyleInfo (rpt, row).ConfigureAwait (false);
            overlay.TitleStyle = TextStyle (_titleStyle, titleInfo, 9, true);
            ApplyBox (overlay, si);
            return overlay;
        }
    }

    /// <summary>A MapTitle: its Text, in its Style's font, centred in its box. Angle and TextShadowOffset are read and not used.</summary>
    internal sealed class MapTitleDefn : MapSubItem
    {
        private Expression _text;

        internal MapTitleDefn (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            Parse (xNode);
        }

        protected override bool OwnElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "Text":
                    _text = new Expression (OwnerReport, this, node, ExpressionType.String);
                    return true;
                case "Angle":
                case "TextShadowOffset":
                    return true;
                default:
                    return false;
            }
        }

        protected override async Task OwnFinalPass ()
        {
            if (_text != null)
                await _text.FinalPass ().ConfigureAwait (false);
        }

        internal async Task<MapTitleOverlay> Evaluate (Report rpt, Row row)
        {
            var text = await Text (rpt, row, _text).ConfigureAwait (false);
            if (string.IsNullOrWhiteSpace (text))
                return null;
            var si = await StyleInfoOf (rpt, row).ConfigureAwait (false);
            var overlay = new MapTitleOverlay { Text = text, TextStyle = TextStyle (Style, si, 12, true) };
            ApplyBox (overlay, si);
            return overlay;
        }
    }

    /// <summary>
    /// The MapColorScale: a bar of the first rule's bucket colours that says ShowInColorScale, with
    /// tick marks and labels at the bucket boundaries (or a label per category), under its
    /// MapColorScaleTitle Caption.
    /// </summary>
    /// <remarks>
    /// TickMarkLength (2.25pt), ColorBarBorderColor (black), LabelInterval, LabelFormat,
    /// LabelPlacement (Alternate, the default: below and above in turn; Inside: above; Outside:
    /// below) and HideEndLabels are used; LabelBehavior, RangeGapColor and NoDataText are read and
    /// not used. Without a rule to show, nothing is drawn. Labels that would overlap the one before
    /// are left out.
    /// </remarks>
    internal sealed class MapColorScaleDefn : MapSubItem
    {
        private Expression _caption;
        private Style _titleStyle;
        private float _tickLength = 2.25f;
        private string _barBorderColor;
        private int _labelInterval = 1;
        private string _labelFormat;
        private string _labelPlacement = "Alternate";
        private bool _hideEndLabels;

        internal MapColorScaleDefn (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            Parse (xNode);
        }

        protected override bool OwnElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "MapColorScaleTitle":
                    foreach (XmlNode part in node.ChildNodes) {
                        if (part.NodeType != XmlNodeType.Element)
                            continue;
                        if (part.LocalName == "Caption")
                            _caption = new Expression (OwnerReport, this, part, ExpressionType.String);
                        else if (part.LocalName == "Style")
                            _titleStyle = new Style (OwnerReport, this, part);
                    }
                    return true;
                case "TickMarkLength":
                    var size = new RSize (OwnerReport, node.InnerText.Trim ());
                    if (size.Points > 0)
                        _tickLength = size.Points;
                    return true;
                case "ColorBarBorderColor":
                    _barBorderColor = node.InnerText;
                    return true;
                case "LabelInterval":
                    if (int.TryParse (node.InnerText.Trim (), NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval) && interval > 0)
                        _labelInterval = interval;
                    return true;
                case "LabelFormat":
                    _labelFormat = node.InnerText.Trim ();
                    return true;
                case "LabelPlacement":
                    _labelPlacement = node.InnerText.Trim ();
                    return true;
                case "HideEndLabels":
                    _hideEndLabels = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                    return true;
                case "LabelBehavior":
                case "RangeGapColor":
                case "NoDataText":
                    return true;
                default:
                    return false;
            }
        }

        protected override async Task OwnFinalPass ()
        {
            if (_caption != null)
                await _caption.FinalPass ().ConfigureAwait (false);
            if (_titleStyle != null)
                await _titleStyle.FinalPass ().ConfigureAwait (false);
        }

        internal async Task<MapColorScaleOverlay> Evaluate (Report rpt, Row row, MapRuleResult rule)
        {
            var si = await StyleInfoOf (rpt, row).ConfigureAwait (false);
            var titleInfo = _titleStyle == null ? new StyleInfo () : await _titleStyle.GetStyleInfo (rpt, row).ConfigureAwait (false);
            var categories = rule.Buckets.All (b => b.IsCategory);
            var overlay = new MapColorScaleOverlay {
                Title = await Text (rpt, row, _caption).ConfigureAwait (false),
                TitleStyle = TextStyle (_titleStyle, titleInfo, 8, true),
                LabelStyle = TextStyle (Style, si, 7, false),
                TickLength = _tickLength,
                BarBorder = ColorOf (rpt, _barBorderColor, Color.Black),
                Above = !string.Equals (_labelPlacement, "Outside", StringComparison.OrdinalIgnoreCase),
                Below = !string.Equals (_labelPlacement, "Inside", StringComparison.OrdinalIgnoreCase),
            };
            overlay.Colors.AddRange (rule.Buckets.Select (b => b.Color));
            if (categories) {
                overlay.Categories = true;
                for (var i = 0; i < rule.Buckets.Count; i++)
                    overlay.Labels.Add (i % _labelInterval == 0 ? rule.Buckets[i].Category : null);
            } else {
                var bounds = rule.Buckets.Select (b => b.From).Append (rule.Buckets[rule.Buckets.Count - 1].To).ToList ();
                for (var i = 0; i < bounds.Count; i++) {
                    var end = i == 0 || i == bounds.Count - 1;
                    overlay.Labels.Add ((_hideEndLabels && end) || i % _labelInterval != 0 ? null : FormatNumber (bounds[i], _labelFormat));
                }
            }
            ApplyBox (overlay, si);
            return overlay;
        }

        internal static string FormatNumber (double value, string format)
        {
            if (!string.IsNullOrWhiteSpace (format)) {
                try {
                    return value.ToString (format, CultureInfo.InvariantCulture);
                } catch (FormatException) {
                    // fall through to the default
                }
            }
            return value.ToString ("#,##0.##", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The MapDistanceScale: bars of a round distance in kilometres and in miles, measured along
    /// the parallel through the centre of the map. Drawn for Geographic coordinates only: Planar
    /// coordinates have no known unit, so for those it is left out and the normaliser says so.
    /// ScaleColor (white) fills the bars, ScaleBorderColor (dark grey) outlines them.
    /// </summary>
    internal sealed class MapDistanceScaleDefn : MapSubItem
    {
        private string _scaleColor;
        private string _scaleBorderColor;

        internal MapDistanceScaleDefn (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p, xNode)
        {
            Position = "BottomLeft";    // where SSRS puts it by default; MS-RDL leaves Position to MapDockableSubItem
            Parse (xNode);
        }

        protected override bool OwnElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "ScaleColor":
                    _scaleColor = node.InnerText;
                    return true;
                case "ScaleBorderColor":
                    _scaleBorderColor = node.InnerText;
                    return true;
                default:
                    return false;
            }
        }

        internal async Task<MapDistanceScaleOverlay> Evaluate (Report rpt, Row row, MapViewport viewport)
        {
            var si = await StyleInfoOf (rpt, row).ConfigureAwait (false);
            var overlay = new MapDistanceScaleOverlay {
                Mercator = viewport.IsMercator,
                TextStyle = TextStyle (Style, si, 7, false),
                ScaleColor = ColorOf (rpt, _scaleColor, Color.White),
                ScaleBorder = ColorOf (rpt, _scaleBorderColor, Color.DarkGray),
            };
            ApplyBox (overlay, si);
            return overlay;
        }
    }

    /// <summary>
    /// A legend entry's text: the rule's LegendText with its keywords replaced, or the bucket's own
    /// label when there is none. #FROMVALUE and #TOVALUE are a range's ends, #VALUE a category (or
    /// a range's start); each can carry a .NET format in braces, as in "#FROMVALUE{N0} - #TOVALUE{N0}".
    /// Numbers are formatted with the invariant culture.
    /// </summary>
    internal static class MapLegendText
    {
        private static readonly Regex Keyword = new Regex (@"#(FROMVALUE|TOVALUE|VALUE)(\{([^}]*)\})?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static async Task<string> For (Report rpt, MapAppearanceRule rule, MapBucket bucket)
        {
            var template = rule?.LegendText?.Source;
            if (template != null && template.TrimStart ().StartsWith ("=", StringComparison.Ordinal))
                template = await rule.LegendText.EvaluateString (rpt, null).ConfigureAwait (false);
            return string.IsNullOrWhiteSpace (template) ? bucket.Label : Format (template, bucket);
        }

        internal static string Format (string template, MapBucket bucket)
            => Keyword.Replace (template, m => {
                var format = m.Groups[3].Success ? m.Groups[3].Value : null;
                if (bucket.IsCategory)
                    return bucket.Category;
                var value = m.Groups[1].Value.Equals ("TOVALUE", StringComparison.OrdinalIgnoreCase) ? bucket.To : bucket.From;
                if (!string.IsNullOrEmpty (format)) {
                    try {
                        return value.ToString (format, CultureInfo.InvariantCulture);
                    } catch (FormatException) {
                        // the plain number below
                    }
                }
                return value.ToString ("G6", CultureInfo.InvariantCulture);
            });
    }
}
