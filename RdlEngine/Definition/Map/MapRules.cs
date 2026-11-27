using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Color = System.Drawing.Color;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// One class of values an appearance rule gives a look to: a numeric range (From to To), or a
    /// single value (Category) when the rule's values are not numbers. A colour rule fills in
    /// Color; the legend and colour scale draw from these.
    /// </summary>
    internal sealed class MapBucket
    {
        internal double From;
        internal double To;
        internal string Category;
        internal Color Color = Color.Empty;

        internal bool IsCategory => Category != null;

        internal string Label => IsCategory
            ? Category
            : From.ToString ("G6", CultureInfo.InvariantCulture) + " - " + To.ToString ("G6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What a rule made of a layer's values: its buckets in order, and which bucket each value fell
    /// in (-1 for none: no value, or outside the rule's start and end). Kept on the scene, so a
    /// legend or colour scale can show the same classes the shapes were drawn with.
    /// </summary>
    internal sealed class MapRuleResult
    {
        internal MapAppearanceRule Rule;
        internal string LayerName;
        internal List<MapBucket> Buckets = new List<MapBucket> ();
        internal int[] BucketOfValue = Array.Empty<int> ();

        internal MapBucket BucketFor (int valueIndex)
        {
            var b = valueIndex >= 0 && valueIndex < BucketOfValue.Length ? BucketOfValue[valueIndex] : -1;
            return b >= 0 && b < Buckets.Count ? Buckets[b] : null;
        }
    }

    /// <summary>
    /// What the colour, size and marker rules of a map layer share (MapAppearanceRule): the value
    /// they classify and how the values are split into buckets.
    /// </summary>
    /// <remarks>
    /// DistributionType is Optimal (natural breaks, the default), EqualInterval, EqualDistribution
    /// (about as many values in each bucket) or Custom (the rule's MapBuckets). Values that are not
    /// numbers are classified one bucket per distinct value, in the order they first occur. A
    /// DataValue written "#NAME" is the spatial element's own field NAME, not an expression.
    /// </remarks>
    internal abstract class MapAppearanceRule : ReportLink
    {
        internal Expression DataValue;
        internal string DistributionType = "Optimal";
        internal int BucketCount = -1;             // -1: not given; the rule chooses
        internal Expression StartValue;
        internal Expression EndValue;
        internal List<(Expression Start, Expression End)> CustomBuckets = new List<(Expression, Expression)> ();
        internal string LegendName;
        internal Expression LegendText;

        protected MapAppearanceRule (ReportDefn r, ReportLink p) : base (r, p)
        {
        }

        protected void Parse (XmlNode xNode)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                if (!RuleElement (node) && !SpecificElement (node))
                    OwnerReport.rl.LogError (4, "Unknown " + xNode.Name + " element '" + node.Name + "' ignored.");
            }
        }

        private bool RuleElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "DataValue":
                    DataValue = new Expression (OwnerReport, this, node, ExpressionType.Variant);
                    return true;
                case "DistributionType":
                    DistributionType = node.InnerText.Trim ();
                    return true;
                case "BucketCount":
                    if (int.TryParse (node.InnerText.Trim (), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0)
                        BucketCount = Math.Min (count, 256);
                    return true;
                case "StartValue":
                    StartValue = new Expression (OwnerReport, this, node, ExpressionType.Variant);
                    return true;
                case "EndValue":
                    EndValue = new Expression (OwnerReport, this, node, ExpressionType.Variant);
                    return true;
                case "MapBuckets":
                    foreach (XmlNode bucket in node.ChildNodes) {
                        if (bucket.NodeType != XmlNodeType.Element || bucket.LocalName != "MapBucket")
                            continue;
                        Expression start = null, end = null;
                        foreach (XmlNode part in bucket.ChildNodes) {
                            if (part.LocalName == "StartValue")
                                start = new Expression (OwnerReport, this, part, ExpressionType.Variant);
                            else if (part.LocalName == "EndValue")
                                end = new Expression (OwnerReport, this, part, ExpressionType.Variant);
                        }
                        if (start != null && end != null)
                            CustomBuckets.Add ((start, end));
                    }
                    return true;
                case "LegendName":
                    LegendName = node.InnerText.Trim ();
                    return true;
                case "LegendText":
                    LegendText = new Expression (OwnerReport, this, node, ExpressionType.String);
                    return true;
                case "DataElementName":
                case "DataElementOutput":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Elements of the specific rule; false for one it does not know.</summary>
        protected abstract bool SpecificElement (XmlNode node);

        internal override async Task FinalPass ()
        {
            if (DataValue != null)
                await DataValue.FinalPass ().ConfigureAwait (false);
            if (StartValue != null)
                await StartValue.FinalPass ().ConfigureAwait (false);
            if (EndValue != null)
                await EndValue.FinalPass ().ConfigureAwait (false);
            if (LegendText != null)
                await LegendText.FinalPass ().ConfigureAwait (false);
            foreach (var (start, end) in CustomBuckets) {
                await start.FinalPass ().ConfigureAwait (false);
                await end.FinalPass ().ConfigureAwait (false);
            }
        }

        /// <summary>The field of the spatial element a "#NAME" DataValue names, or null for an expression.</summary>
        internal string SpatialFieldName
        {
            get {
                var source = DataValue?.Source?.Trim ();
                return source != null && source.Length > 1 && source[0] == '#' ? source.Substring (1) : null;
            }
        }

        /// <summary>
        /// The rule's value for one spatial element: its own field for "#NAME", otherwise the
        /// DataValue evaluated on the row of the group bound to it (aggregates take that group's
        /// rows). Null for an element no data is bound to.
        /// </summary>
        internal async Task<object> ValueFor (Report rpt, Row row, MapSpatialElement element, bool layerHasData)
        {
            if (DataValue == null)
                return null;
            var field = SpatialFieldName;
            if (field != null)
                return element.Field (field);
            if (row == null && layerHasData && DataValue.Source?.TrimStart ().StartsWith ("=", StringComparison.Ordinal) == true)
                return null;
            var value = await DataValue.Evaluate (rpt, row).ConfigureAwait (false);
            return value is DBNull ? null : value;
        }

        /// <summary>Splits the values into buckets. <paramref name="defaultCount"/> is used when the rule gives no BucketCount.</summary>
        internal async Task<MapRuleResult> Classify (Report rpt, IList<object> values, int defaultCount)
        {
            var result = new MapRuleResult { Rule = this, BucketOfValue = new int[values.Count] };
            for (var i = 0; i < values.Count; i++)
                result.BucketOfValue[i] = -1;

            var numbers = new double?[values.Count];
            var numeric = true;
            var any = false;
            for (var i = 0; i < values.Count; i++) {
                if (values[i] == null)
                    continue;
                any = true;
                if (TryNumber (values[i], out var d))
                    numbers[i] = d;
                else
                    numeric = false;
            }
            if (!any)
                return result;

            if (!numeric) {
                var index = new Dictionary<string, int> (StringComparer.Ordinal);
                for (var i = 0; i < values.Count; i++) {
                    if (values[i] == null)
                        continue;
                    var key = Convert.ToString (values[i], CultureInfo.InvariantCulture);
                    if (!index.TryGetValue (key, out var b)) {
                        b = result.Buckets.Count;
                        index[key] = b;
                        result.Buckets.Add (new MapBucket { Category = key });
                    }
                    result.BucketOfValue[i] = b;
                }
                return result;
            }

            var present = numbers.Where (n => n.HasValue).Select (n => n.Value).OrderBy (n => n).ToArray ();
            var low = await Bound (rpt, StartValue, present[0]).ConfigureAwait (false);
            var high = await Bound (rpt, EndValue, present[present.Length - 1]).ConfigureAwait (false);
            if (high < low)
                (low, high) = (high, low);
            var inRange = present.Where (v => v >= low && v <= high).ToArray ();
            var count = BucketCount > 0 ? BucketCount : defaultCount;
            count = Math.Max (1, count);

            List<(double From, double To)> ranges;
            if (string.Equals (DistributionType, "Custom", StringComparison.OrdinalIgnoreCase) && CustomBuckets.Count > 0) {
                ranges = new List<(double, double)> ();
                foreach (var (start, end) in CustomBuckets) {
                    var from = await Bound (rpt, start, double.NaN).ConfigureAwait (false);
                    var to = await Bound (rpt, end, double.NaN).ConfigureAwait (false);
                    if (!double.IsNaN (from) && !double.IsNaN (to))
                        ranges.Add ((Math.Min (from, to), Math.Max (from, to)));
                }
            } else if (inRange.Length == 0 || high <= low) {
                ranges = new List<(double, double)> { (low, high) };
            } else if (string.Equals (DistributionType, "EqualInterval", StringComparison.OrdinalIgnoreCase)) {
                ranges = EqualInterval (low, high, count);
            } else if (string.Equals (DistributionType, "EqualDistribution", StringComparison.OrdinalIgnoreCase)) {
                ranges = Quantiles (inRange, low, high, count);
            } else {
                ranges = NaturalBreaks (inRange, low, high, count);
            }

            foreach (var (from, to) in ranges)
                result.Buckets.Add (new MapBucket { From = from, To = to });

            for (var i = 0; i < numbers.Length; i++) {
                if (!numbers[i].HasValue)
                    continue;
                var v = numbers[i].Value;
                for (var b = 0; b < ranges.Count; b++) {
                    var last = b == ranges.Count - 1;
                    if (v >= ranges[b].From && (v < ranges[b].To || (v <= ranges[b].To && (last || ranges[b].To <= ranges[b].From)))) {
                        result.BucketOfValue[i] = b;
                        break;
                    }
                }
            }
            return result;
        }

        private static async Task<double> Bound (Report rpt, Expression e, double fallback)
        {
            if (e == null || string.IsNullOrWhiteSpace (e.Source))
                return fallback;
            var value = await e.Evaluate (rpt, null).ConfigureAwait (false);
            return value != null && TryNumber (value, out var d) ? d : fallback;
        }

        internal static bool TryNumber (object value, out double number)
        {
            switch (value) {
                case double d:
                    number = d;
                    return !double.IsNaN (d) && !double.IsInfinity (d);
                case float f:
                    number = f;
                    return !float.IsNaN (f) && !float.IsInfinity (f);
                case decimal m:
                    number = (double)m;
                    return true;
                case int _: case long _: case short _: case byte _: case sbyte _: case uint _: case ulong _: case ushort _:
                    number = Convert.ToDouble (value, CultureInfo.InvariantCulture);
                    return true;
                case string s:
                    return double.TryParse (s.Trim (), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                        && !double.IsNaN (number) && !double.IsInfinity (number);
                default:
                    number = 0;
                    return false;
            }
        }

        private static List<(double, double)> EqualInterval (double low, double high, int count)
        {
            var ranges = new List<(double, double)> (count);
            var step = (high - low) / count;
            for (var i = 0; i < count; i++)
                ranges.Add ((low + step * i, i == count - 1 ? high : low + step * (i + 1)));
            return ranges;
        }

        private static List<(double, double)> Quantiles (double[] sorted, double low, double high, int count)
        {
            count = Math.Min (count, sorted.Length);
            var ranges = new List<(double, double)> (count);
            var from = low;
            for (var i = 0; i < count; i++) {
                var to = i == count - 1 ? high : sorted[(int)((long)(i + 1) * sorted.Length / count)];
                if (to > from || i == count - 1)
                    ranges.Add ((from, to));
                from = Math.Max (from, to);
            }
            return ranges;
        }

        /// <summary>Jenks natural breaks: buckets that keep each one's values as close together as possible.</summary>
        private static List<(double, double)> NaturalBreaks (double[] sorted, double low, double high, int count)
        {
            var distinct = sorted.Distinct ().Count ();
            count = Math.Min (count, distinct);
            if (count <= 1)
                return new List<(double, double)> { (low, high) };
            if (sorted.Length > 2000)       // the method is quadratic; that many shapes split by count instead
                return Quantiles (sorted, low, high, count);

            var n = sorted.Length;
            var lower = new int[n + 1, count + 1];
            var variance = new double[n + 1, count + 1];
            for (var j = 1; j <= count; j++) {
                lower[1, j] = 1;
                for (var i = 2; i <= n; i++)
                    variance[i, j] = double.MaxValue;
            }
            for (var l = 2; l <= n; l++) {
                double sum = 0, squares = 0, w = 0, v = 0;
                for (var m = 1; m <= l; m++) {
                    var lowerIndex = l - m + 1;
                    var value = sorted[lowerIndex - 1];
                    w++;
                    sum += value;
                    squares += value * value;
                    v = squares - sum * sum / w;
                    var previous = lowerIndex - 1;
                    if (previous == 0)
                        continue;
                    for (var j = 2; j <= count; j++) {
                        if (variance[l, j] >= v + variance[previous, j - 1]) {
                            lower[l, j] = lowerIndex;
                            variance[l, j] = v + variance[previous, j - 1];
                        }
                    }
                }
                lower[l, 1] = 1;
                variance[l, 1] = v;
            }

            var starts = new double[count];
            var k = n;
            for (var j = count; j >= 1; j--) {
                var id = lower[k, j] - 1;
                starts[j - 1] = sorted[id];
                k = lower[k, j] - 1;
                if (k < 1)
                    k = 1;
            }
            var ranges = new List<(double, double)> (count);
            for (var j = 0; j < count; j++) {
                var from = j == 0 ? low : starts[j];
                var to = j == count - 1 ? high : starts[j + 1];
                if (to > from || j == count - 1)
                    ranges.Add ((from, to));
            }
            return ranges;
        }
    }

    /// <summary>
    /// A rule that colours shapes (MapColorRule): a colour range, a palette, or custom colours.
    /// </summary>
    internal abstract class MapColorRule : MapAppearanceRule
    {
        internal bool ShowInColorScale;

        protected MapColorRule (ReportDefn r, ReportLink p) : base (r, p)
        {
        }

        /// <summary>The colour rule an element of MapPolygonRules (and the other layers' rules) describes, or null.</summary>
        internal static MapColorRule Create (ReportDefn r, ReportLink p, XmlNode node)
        {
            switch (node.LocalName) {
                case "MapColorRangeRule":
                    return new MapColorRangeRule (r, p, node);
                case "MapColorPaletteRule":
                    return new MapColorPaletteRule (r, p, node);
                case "MapCustomColorRule":
                    return new MapCustomColorRule (r, p, node);
                default:
                    return null;
            }
        }

        protected override bool SpecificElement (XmlNode node)
        {
            if (node.LocalName == "ShowInColorScale") {
                ShowInColorScale = string.Equals (node.InnerText.Trim (), "true", StringComparison.OrdinalIgnoreCase);
                return true;
            }
            return ColorElement (node);
        }

        protected abstract bool ColorElement (XmlNode node);

        /// <summary>How many buckets the rule makes when it does not say.</summary>
        protected virtual int DefaultBucketCount => 5;

        /// <summary>The colour of each of <paramref name="count"/> buckets, first to last.</summary>
        protected abstract Color[] Colors (Report rpt, int count);

        /// <summary>Classifies the values and colours the buckets.</summary>
        internal async Task<MapRuleResult> Assign (Report rpt, IList<object> values)
        {
            var result = await Classify (rpt, values, DefaultBucketCount).ConfigureAwait (false);
            var colors = Colors (rpt, result.Buckets.Count);
            for (var i = 0; i < result.Buckets.Count && i < colors.Length; i++)
                result.Buckets[i].Color = colors[i];
            return result;
        }

        protected Color ColorOf (Report rpt, string text, Color fallback)
        {
            if (string.IsNullOrWhiteSpace (text) || text.TrimStart ().StartsWith ("=", StringComparison.Ordinal))
                return fallback;
            return XmlUtil.ColorFromHtml (text.Trim (), fallback, rpt);
        }

        /// <summary>Evenly spread colours along the stops, as a colour range draws them.</summary>
        internal static Color[] Blend (IList<Color> stops, int count)
        {
            var colors = new Color[count];
            if (stops.Count == 0)
                return colors;
            for (var i = 0; i < count; i++) {
                var t = count == 1 ? 0 : (double)i / (count - 1) * (stops.Count - 1);
                var at = Math.Min ((int)Math.Floor (t), stops.Count - 1);
                var next = Math.Min (at + 1, stops.Count - 1);
                var f = t - at;
                Color a = stops[at], b = stops[next];
                colors[i] = Color.FromArgb (
                    (int)Math.Round (a.A + (b.A - a.A) * f), (int)Math.Round (a.R + (b.R - a.R) * f),
                    (int)Math.Round (a.G + (b.G - a.G) * f), (int)Math.Round (a.B + (b.B - a.B) * f));
            }
            return colors;
        }
    }

    /// <summary>Colours running from StartColor through MiddleColor to EndColor (green, yellow, red unless the rule says).</summary>
    internal sealed class MapColorRangeRule : MapColorRule
    {
        private string _start, _middle, _end;

        internal MapColorRangeRule (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Parse (xNode);
        }

        protected override bool ColorElement (XmlNode node)
        {
            switch (node.LocalName) {
                case "StartColor": _start = node.InnerText; return true;
                case "MiddleColor": _middle = node.InnerText; return true;
                case "EndColor": _end = node.InnerText; return true;
                default: return false;
            }
        }

        protected override Color[] Colors (Report rpt, int count)
        {
            var stops = new List<Color> { ColorOf (rpt, _start, Color.Green) };
            var middle = _middle == null ? Color.Yellow : ColorOf (rpt, _middle, Color.Empty);
            if (!middle.IsEmpty)
                stops.Add (middle);
            stops.Add (ColorOf (rpt, _end, Color.Red));
            return Blend (stops, count);
        }
    }

    /// <summary>The colours of a named palette, one per bucket and round again.</summary>
    internal sealed class MapColorPaletteRule : MapColorRule
    {
        private string _palette = "Random";

        internal MapColorPaletteRule (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Parse (xNode);
        }

        protected override bool ColorElement (XmlNode node)
        {
            if (node.LocalName != "Palette")
                return false;
            _palette = node.InnerText.Trim ();
            return true;
        }

        protected override Color[] Colors (Report rpt, int count)
        {
            var palette = MapPalettes.Get (_palette);
            var colors = new Color[count];
            for (var i = 0; i < count; i++)
                colors[i] = palette[i % palette.Length];
            return colors;
        }
    }

    /// <summary>The report's own colours (MapCustomColors), one per bucket; more buckets than colours blend between them.</summary>
    internal sealed class MapCustomColorRule : MapColorRule
    {
        private readonly List<string> _colors = new List<string> ();

        internal MapCustomColorRule (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            Parse (xNode);
        }

        protected override bool ColorElement (XmlNode node)
        {
            if (node.LocalName != "MapCustomColors")
                return false;
            foreach (XmlNode color in node.ChildNodes) {
                if (color.NodeType == XmlNodeType.Element && color.LocalName == "MapCustomColor")
                    _colors.Add (color.InnerText);
            }
            return true;
        }

        protected override int DefaultBucketCount => Math.Max (1, _colors.Count);

        protected override Color[] Colors (Report rpt, int count)
        {
            var stops = _colors.Select (c => ColorOf (rpt, c, Color.Gray)).ToList ();
            if (stops.Count == 0)
                stops.Add (Color.Gray);
            if (count <= stops.Count)
                return stops.Take (count).ToArray ();
            return Blend (stops, count);
        }
    }

    /// <summary>
    /// The named map palettes. Report Builder does not publish their colours; these follow the chart
    /// palettes of the same names. Random is fixed, so a report draws the same way each time.
    /// </summary>
    internal static class MapPalettes
    {
        private static Color[] Hex (params uint[] argb) => argb.Select (c => Color.FromArgb ((int)c)).ToArray ();

        private static readonly Color[] BrightPastel = Hex (
            0xFF418CF0, 0xFFFCB441, 0xFFE0400A, 0xFF056492, 0xFFBFBFBF, 0xFF1A3B69, 0xFFFFE382, 0xFF129CDD,
            0xFFCA6B4B, 0xFF005CDB, 0xFFF3D288, 0xFF506381, 0xFFF1B9A8, 0xFFE0830A, 0xFF7893BE);

        private static readonly Color[] Pacific = Hex (
            0xFF2A80B9, 0xFF1ABC9C, 0xFF5DADE2, 0xFF16A085, 0xFF85C1E9, 0xFF48C9B0, 0xFF2471A3, 0xFF76D7C4,
            0xFF1B4F72, 0xFF0E6655);

        private static readonly Color[] Light = Hex (
            0xFFE6E6FA, 0xFFFFF0F5, 0xFFFFDAB9, 0xFFFFFACD, 0xFFFFE4E1, 0xFFF0FFF0, 0xFFF0F8FF, 0xFFF5F5F5,
            0xFFFAEBD7, 0xFFE0FFFF);

        private static readonly Color[] SemiTransparent = Hex (
            0x96FF0000, 0x9600FF00, 0x960000FF, 0x96FFFF00, 0x9600FFFF, 0x96FF00FF, 0x96AA7814, 0x96FF8C00,
            0x96008000, 0x96000080);

        private static readonly Color[] Random = Hex (
            0xFF6FA8DC, 0xFFF6B26B, 0xFF93C47D, 0xFFE06666, 0xFF8E7CC3, 0xFFFFD966, 0xFF76A5AF, 0xFFC27BA0,
            0xFFB6D7A8, 0xFFF9CB9C, 0xFFA4C2F4, 0xFFD5A6BD);

        internal static Color[] Get (string name)
        {
            switch ((name ?? string.Empty).Trim ().ToLowerInvariant ()) {
                case "brightpastel": return BrightPastel;
                case "pacific": return Pacific;
                case "light": return Light;
                case "semitransparent": return SemiTransparent;
                default: return Random;
            }
        }
    }
}
