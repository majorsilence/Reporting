using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Approximates an RDL 2008+ GaugePanel with report items the engine already renders.
    /// </summary>
    /// <remarks>
    /// The engine has no gauge drawing. A state indicator becomes an empty Textbox filled by an
    /// expression over its ranges, a numeric indicator a formatted Textbox, a gauge, radial or
    /// linear, a single bar on a fixed axis spanning its scale. Child positions in a gauge panel are percentages of the panel. Styling that has no
    /// 2005 counterpart (frames, needles, tick marks, labels, images) is not reproduced; the
    /// value is what carries over.
    /// </remarks>
    internal static class Rdl2008GaugeConverter
    {
        /// <returns>A Rectangle holding the converted children, or null when there is nothing to convert.</returns>
        internal static XmlElement ToRectangle (XmlElement panel)
        {
            var doc = panel.OwnerDocument;
            var ns = panel.NamespaceURI;
            var name = panel.GetAttribute ("Name");
            var dataSet = Child (panel, "DataSetName")?.InnerText;
            var width = Child (panel, "Width")?.InnerText;
            var height = Child (panel, "Height")?.InnerText;

            var items = El (doc, ns, "ReportItems");
            var indicators = El (doc, ns, "ReportItems");
            var index = 0;

            foreach (var indicator in Kids (Child (panel, "StateIndicators"), "StateIndicator")) {
                var item = StateIndicator (doc, ns, indicator, name + "_State" + (++index), width, height);
                if (item != null)
                    indicators.AppendChild (item);
            }
            foreach (var indicator in Kids (Child (panel, "NumericIndicators"), "NumericIndicator")) {
                var item = NumericIndicator (doc, ns, indicator, name + "_Number" + (++index), width, height);
                if (item != null)
                    indicators.AppendChild (item);
            }

            foreach (var gauge in Kids (Child (panel, "RadialGauges"), "RadialGauge")) {
                var itemName = name + "_Radial" + (++index);
                var drawn = DrawnGauge (doc, ns, gauge, itemName, width, height, radial: true);
                if (drawn != null) {
                    indicators.AppendChild (drawn);     // like the indicators, it needs the dataset's scope
                    continue;
                }
                var item = Gauge (doc, ns, gauge, itemName, width, height, dataSet, radial: true);
                if (item != null)
                    items.AppendChild (item);
            }
            foreach (var gauge in Kids (Child (panel, "LinearGauges"), "LinearGauge")) {
                var itemName = name + "_Linear" + (++index);
                var drawn = DrawnGauge (doc, ns, gauge, itemName, width, height, radial: false);
                if (drawn != null) {
                    indicators.AppendChild (drawn);
                    continue;
                }
                var item = Gauge (doc, ns, gauge, itemName, width, height, dataSet, radial: false);
                if (item != null)
                    items.AppendChild (item);
            }

            // Indicator values are aggregates over the panel's dataset. A Textbox or Rectangle outside
            // a data region has no dataset scope, so they sit in a List with one instance (grouped on
            // a constant) bound to it. Charts carry their own DataSetName and stay outside.
            if (indicators.HasChildNodes) {
                if (string.IsNullOrEmpty (dataSet)) {
                    foreach (XmlNode n in new List<XmlNode> (indicators.ChildNodes.Cast<XmlNode> ()))
                        items.AppendChild (n);
                } else {
                    var list = El (doc, ns, "List");
                    list.SetAttribute ("Name", name + "_Scope");
                    list.AppendChild (El (doc, ns, "Top", "0in"));
                    list.AppendChild (El (doc, ns, "Left", "0in"));
                    if (height != null)
                        list.AppendChild (El (doc, ns, "Height", height));
                    if (width != null)
                        list.AppendChild (El (doc, ns, "Width", width));
                    list.AppendChild (El (doc, ns, "DataSetName", dataSet));
                    var grouping = El (doc, ns, "Grouping");
                    grouping.SetAttribute ("Name", name + "_Scope_Group");
                    var expressions = El (doc, ns, "GroupExpressions");
                    expressions.AppendChild (El (doc, ns, "GroupExpression", "=1"));
                    grouping.AppendChild (expressions);
                    list.AppendChild (grouping);
                    list.AppendChild (indicators);
                    items.AppendChild (list);
                }
            }
            if (!items.HasChildNodes)
                return null;

            var rectangle = El (doc, ns, "Rectangle");
            rectangle.SetAttribute ("Name", string.IsNullOrEmpty (name) ? "RdlGaugePanel" + System.Threading.Interlocked.Increment(ref _counter) : name);
            foreach (var keep in new[] { "Top", "Left", "Height", "Width", "ZIndex", "Visibility" }) {
                var source = Child (panel, keep);
                if (source != null)
                    rectangle.AppendChild (source.CloneNode (true));
            }
            rectangle.AppendChild (items);
            return rectangle;
        }

        private static int _counter;

        #region Indicators

        private static XmlElement StateIndicator (XmlDocument doc, string ns, XmlElement indicator, string name,
            string panelWidth, string panelHeight)
        {
            var value = ValueOf (Child (indicator, "GaugeInputValue"));
            var states = Kids (Child (indicator, "IndicatorStates"), "IndicatorState");
            if (value == null || states.Count == 0)
                return null;

            // With a percentage transformation the state ranges are percentages of the min..max span.
            var scaled = value;
            var transformation = Child (indicator, "TransformationType")?.InnerText;
            if (string.Equals (transformation, "Percentage", StringComparison.OrdinalIgnoreCase)) {
                var min = ValueOf (Child (indicator, "MinimumValue")) ?? "0";
                var max = ValueOf (Child (indicator, "MaximumValue")) ?? "100";
                scaled = $"(({value}) - ({min})) / (({max}) - ({min})) * 100";
            }

            // Nested Iif from the last state outwards; a value in no state keeps the first state's colour.
            var expression = Quote (Child (states[0], "Color")?.InnerText ?? "Transparent");
            for (var i = states.Count - 1; i >= 0; i--) {
                var start = ValueOf (Child (states[i], "StartValue")) ?? "0";
                var end = ValueOf (Child (states[i], "EndValue")) ?? "0";
                var color = Quote (Child (states[i], "Color")?.InnerText ?? "Transparent");
                expression = $"Iif(({scaled}) >= ({start}) And ({scaled}) <= ({end}), {color}, {expression})";
            }

            // An empty Textbox: the engine does not draw the fill of a Rectangle that holds nothing.
            var box = El (doc, ns, "Textbox");
            box.SetAttribute ("Name", name);
            Place (doc, ns, box, indicator, panelWidth, panelHeight);
            box.AppendChild (El (doc, ns, "Value", ""));
            var style = El (doc, ns, "Style");
            style.AppendChild (El (doc, ns, "BackgroundColor", "=" + expression));
            box.AppendChild (style);
            return box;
        }

        private static XmlElement NumericIndicator (XmlDocument doc, string ns, XmlElement indicator, string name,
            string panelWidth, string panelHeight)
        {
            var value = ValueOf (Child (indicator, "GaugeInputValue"));
            if (value == null)
                return null;

            var textbox = El (doc, ns, "Textbox");
            textbox.SetAttribute ("Name", name);
            Place (doc, ns, textbox, indicator, panelWidth, panelHeight);
            textbox.AppendChild (El (doc, ns, "Value", "=" + value));
            var style = El (doc, ns, "Style");
            var digits = Child (indicator, "DecimalDigits")?.InnerText;
            style.AppendChild (El (doc, ns, "Format", int.TryParse (digits, out var d) && d > 0 ? "N" + d : "N0"));
            style.AppendChild (El (doc, ns, "TextAlign", "Center"));
            style.AppendChild (El (doc, ns, "VerticalAlign", "Middle"));
            textbox.AppendChild (style);
            return textbox;
        }

        #endregion

        #region Gauges

        // The gauge drawing lives in the RdlCri module (GaugeRadial / GaugeLinear custom report items).
        // When that module is not available the gauges fall back to a bar chart, below.
        private const string RadialType = "Gauge Radial";
        private const string LinearType = "Gauge Linear";

        /// <summary>Tests set this to false to exercise the fallback.</summary>
        internal static bool DrawnGaugesEnabled = true;

        private static bool IsAvailable (string type)
        {
            if (!DrawnGaugesEnabled)
                return false;
            try {
                RdlEngineConfig.GetCustomReportTypes ();     // makes sure the configuration is loaded
                using (RdlEngineConfig.CreateCustomReportItem (type))
                    return true;
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>A custom report item that draws the gauge, or null when the drawing module is missing.</summary>
        private static XmlElement DrawnGauge (XmlDocument doc, string ns, XmlElement gauge, string name,
            string panelWidth, string panelHeight, bool radial)
        {
            var type = radial ? RadialType : LinearType;
            var scale = radial
                ? Child (Child (gauge, "GaugeScales"), "RadialScale")
                : Child (Child (gauge, "GaugeScales"), "LinearScale");
            var pointer = radial
                ? Child (Child (scale, "GaugePointers"), "RadialPointer")
                : Child (Child (scale, "GaugePointers"), "LinearPointer");
            var value = ValueOf (Child (pointer, "GaugeInputValue"));
            if (value == null || !IsAvailable (type))
                return null;

            var item = El (doc, ns, "CustomReportItem");
            item.SetAttribute ("Name", name);
            item.AppendChild (El (doc, ns, "Type", type));
            Place (doc, ns, item, gauge, panelWidth, panelHeight);
            var properties = El (doc, ns, "CustomProperties");
            item.AppendChild (properties);

            void Property (string propertyName, string text)
            {
                if (string.IsNullOrEmpty (text))
                    return;
                var property = El (doc, ns, "CustomProperty");
                property.AppendChild (El (doc, ns, "Name", propertyName));
                property.AppendChild (El (doc, ns, "Value", text));
                properties.AppendChild (property);
            }

            Property ("Value", "=" + value);
            Property ("Min", "=" + (ValueOf (Child (scale, "MinimumValue")) ?? "0"));
            Property ("Max", "=" + (ValueOf (Child (scale, "MaximumValue")) ?? "100"));
            Property ("Interval", Child (scale, "Interval")?.InnerText?.Trim ());

            // Ranges travel as one "from:to:color;..." string, built by an expression because the ends can be expressions.
            var pieces = new List<string> ();
            foreach (var range in Kids (Child (scale, "ScaleRanges"), "ScaleRange")) {
                var from = ValueOf (Child (range, "StartValue"));
                var to = ValueOf (Child (range, "EndValue"));
                var color = Child (Child (range, "Style"), "BackgroundColor")?.InnerText?.Trim ();
                if (from == null || to == null || string.IsNullOrEmpty (color) || color.StartsWith ("=", StringComparison.Ordinal))
                    continue;
                pieces.Add ($"CStr(({from})) & \":\" & CStr(({to})) & \":{color.Replace ("\"", string.Empty)}\"");
            }
            if (pieces.Count > 0)
                Property ("Ranges", "=" + string.Join (" & \";\" & ", pieces));

            var pointerColor = Child (Child (pointer, "Style"), "BackgroundColor")?.InnerText?.Trim ();
            if (!string.IsNullOrEmpty (pointerColor) && !pointerColor.StartsWith ("=", StringComparison.Ordinal))
                Property ("PointerColor", pointerColor);

            if (radial) {
                Property ("StartAngle", Child (scale, "StartAngle")?.InnerText?.Trim ());
                Property ("SweepAngle", Child (scale, "SweepAngle")?.InnerText?.Trim ());
            } else {
                Property ("Orientation", Child (gauge, "Orientation")?.InnerText?.Trim ());
            }
            return item;
        }

        private static XmlElement Gauge (XmlDocument doc, string ns, XmlElement gauge, string name,
            string panelWidth, string panelHeight, string dataSet, bool radial)
        {
            var scale = radial
                ? Child (Child (gauge, "GaugeScales"), "RadialScale")
                : Child (Child (gauge, "GaugeScales"), "LinearScale");
            var pointer = radial
                ? Child (Child (scale, "GaugePointers"), "RadialPointer")
                : Child (Child (scale, "GaugePointers"), "LinearPointer");
            var value = ValueOf (Child (pointer, "GaugeInputValue"));
            if (value == null)
                return null;

            var min = ValueOf (Child (scale, "MinimumValue")) ?? "0";
            var max = ValueOf (Child (scale, "MaximumValue")) ?? "100";
            var clamped = $"Iif(({value}) < ({min}), ({min}), Iif(({value}) > ({max}), ({max}), ({value})))";

            var chart = El (doc, ns, "Chart");
            chart.SetAttribute ("Name", name);
            Place (doc, ns, chart, gauge, panelWidth, panelHeight);
            if (!string.IsNullOrEmpty (dataSet))
                chart.AppendChild (El (doc, ns, "DataSetName", dataSet));

            chart.AppendChild (El (doc, ns, "Type", "Bar"));
            chart.AppendChild (El (doc, ns, "Subtype", "Plain"));

            // One category (grouped on a constant, because the engine's chart does not implement
            // static categories) and one static series, so there is a single bar.
            var grouping = El (doc, ns, "Grouping");
            grouping.SetAttribute ("Name", name + "_Group");
            var expressions = El (doc, ns, "GroupExpressions");
            expressions.AppendChild (El (doc, ns, "GroupExpression", "=1"));
            grouping.AppendChild (expressions);
            var dynamic = El (doc, ns, "DynamicCategories");
            dynamic.AppendChild (grouping);
            var category = El (doc, ns, "CategoryGrouping");
            category.AppendChild (dynamic);
            var groupings = El (doc, ns, "CategoryGroupings");
            groupings.AppendChild (category);
            chart.AppendChild (groupings);

            var staticSeries = El (doc, ns, "StaticSeries");
            var seriesMember = El (doc, ns, "StaticMember");
            seriesMember.AppendChild (El (doc, ns, "Label", "Value"));
            staticSeries.AppendChild (seriesMember);
            var seriesGrouping = El (doc, ns, "SeriesGrouping");
            seriesGrouping.AppendChild (staticSeries);
            var seriesGroupings = El (doc, ns, "SeriesGroupings");
            seriesGroupings.AppendChild (seriesGrouping);
            chart.AppendChild (seriesGroupings);

            var points = El (doc, ns, "DataPoints");
            points.AppendChild (DataPoint (doc, ns, "=" + clamped));
            var series = El (doc, ns, "ChartSeries");
            series.AppendChild (points);
            var data = El (doc, ns, "ChartData");
            data.AppendChild (series);
            chart.AppendChild (data);

            // Fix the value axis to the gauge's scale so the bar shows how full it is.
            var axis = El (doc, ns, "Axis");
            axis.AppendChild (El (doc, ns, "Visible", "true"));
            axis.AppendChild (El (doc, ns, "Min", "=" + min));
            axis.AppendChild (El (doc, ns, "Max", "=" + max));
            var valueAxis = El (doc, ns, "ValueAxis");
            valueAxis.AppendChild (axis);
            chart.AppendChild (valueAxis);
            var categoryAxis = El (doc, ns, "CategoryAxis");
            var hidden = El (doc, ns, "Axis");
            hidden.AppendChild (El (doc, ns, "Visible", "false"));
            categoryAxis.AppendChild (hidden);
            chart.AppendChild (categoryAxis);

            return chart;
        }

        private static XmlElement DataPoint (XmlDocument doc, string ns, string expression)
        {
            var value = El (doc, ns, "DataValue");
            value.AppendChild (El (doc, ns, "Value", expression));
            var values = El (doc, ns, "DataValues");
            values.AppendChild (value);
            var point = El (doc, ns, "DataPoint");
            point.AppendChild (values);
            return point;
        }

        #endregion

        #region Helpers

        /// <summary>Gauge children are positioned and sized as percentages of the panel.</summary>
        private static void Place (XmlDocument doc, string ns, XmlElement target, XmlElement source, string panelWidth, string panelHeight)
        {
            void Set (string element, string percent, string panelSize)
            {
                var size = Rdl2008Normalizer.ScalePercent (panelSize, percent);
                if (size != null)
                    target.AppendChild (El (doc, ns, element, size));
            }

            Set ("Top", Child (source, "Top")?.InnerText ?? "0", panelHeight);
            Set ("Left", Child (source, "Left")?.InnerText ?? "0", panelWidth);
            Set ("Height", Child (source, "Height")?.InnerText ?? "100", panelHeight);
            Set ("Width", Child (source, "Width")?.InnerText ?? "100", panelWidth);
        }

        /// <summary>The expression of a GaugeInputValue without its leading '=', or null when it has none.</summary>
        private static string ValueOf (XmlElement input)
        {
            if (input == null)
                return null;
            // Most inputs hold a Value child; a few writers put the text straight in the element.
            var text = (Child (input, "Value")?.InnerText ?? (input.HasChildNodes && Child (input, "Formula") == null && !HasElementChildren (input) ? input.InnerText : null))?.Trim ();
            if (string.IsNullOrEmpty (text))
                return null;
            return text.StartsWith ("=", StringComparison.Ordinal) ? text.Substring (1) : text;
        }

        private static bool HasElementChildren (XmlElement e)
        {
            foreach (XmlNode n in e.ChildNodes) {
                if (n is XmlElement)
                    return true;
            }
            return false;
        }

        private static string Quote (string text)
            => "\"" + text.Replace ("\"", "\"\"") + "\"";

        private static XmlElement El (XmlDocument doc, string ns, string name, string text = null)
        {
            var e = doc.CreateElement (name, ns);
            if (text != null)
                e.InnerText = text;
            return e;
        }

        private static XmlElement Child (XmlElement parent, string localName)
        {
            if (parent == null)
                return null;
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement e && e.LocalName == localName)
                    return e;
            }
            return null;
        }

        private static List<XmlElement> Kids (XmlElement parent, string localName)
        {
            var result = new List<XmlElement> ();
            if (parent == null)
                return result;
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement e && e.LocalName == localName)
                    result.Add (e);
            }
            return result;
        }

        #endregion
    }
}
