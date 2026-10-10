using System;
using System.Collections.Generic;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Converts a chart between the RDL 2008+ model (ChartCategoryHierarchy, ChartSeriesHierarchy,
    /// ChartData/ChartSeriesCollection, ChartAreas, ChartLegends, ChartTitles) and the 2005 model
    /// the engine renders (Type/Subtype, CategoryGroupings, SeriesGroupings, ChartData, one set of
    /// axes, one Legend, one Title).
    /// </summary>
    /// <remarks>
    /// The engine charts one type per chart and one area, so what 2008 can say beyond that -- mixed
    /// series types, several chart areas, secondary axes, series types with no 2005 counterpart
    /// (Range, Polar, Funnel, ...) -- is reduced to the first of each and reported once per chart.
    /// </remarks>
    internal static class Rdl2008ChartConverter
    {
        private static readonly string[] Elements2008Only =
        {
            "ChartCategoryHierarchy", "ChartSeriesHierarchy", "ChartData", "ChartAreas", "ChartLegends",
            "ChartTitles", "DynamicHeight", "DynamicWidth", "ChartCustomPaletteColors", "PaletteHatchBehavior",
            "ChartBorderSkin", "DataElementName", "DataElementOutput", "DataElementStyle", "PageName", "ChartConsumedByLegend",
        };

        #region Detection

        /// <summary>True for a chart written in the 2008+ vocabulary.</summary>
        internal static bool Is2008Chart (XmlElement chart)
            => Child (chart, "ChartData") != null && Child (Child (chart, "ChartData"), "ChartSeriesCollection") != null
               || Child (chart, "ChartCategoryHierarchy") != null
               || Child (chart, "ChartAreas") != null;

        /// <summary>True for a chart written in the 2005 vocabulary.</summary>
        internal static bool Is2005Chart (XmlElement chart)
            => Child (chart, "Type") != null || Child (chart, "CategoryGroupings") != null
               || Child (Child (chart, "ChartData"), "ChartSeries") != null;

        #endregion

        #region 2008+ to the engine

        internal static XmlElement ToEngine (XmlElement chart, ReportLog rl)
        {
            var doc = chart.OwnerDocument;
            var ns = chart.NamespaceURI;
            var name = chart.GetAttribute ("Name");
            var unsupported = new List<string> ();

            var result = El (doc, ns, "Chart");
            if (name.Length > 0)
                result.SetAttribute ("Name", name);

            foreach (XmlNode node in chart.ChildNodes) {
                if (node is not XmlElement e || Array.IndexOf (Elements2008Only, e.LocalName) >= 0)
                    continue;
                // 2008 added palettes (BrightPastel, Pacific, ...) the engine does not have.
                if (e.LocalName == "Palette" && !EnginePalettes.Contains (e.InnerText.Trim ())) {
                    unsupported.Add ($"the {e.InnerText.Trim ()} palette");
                    continue;
                }
                result.AppendChild (e.CloneNode (true));
            }

            var series = Kids (Child (Child (chart, "ChartData"), "ChartSeriesCollection"), "ChartSeries");

            var (type, subtype) = ("Column", "Plain");
            if (series.Count > 0) {
                var first = series[0];
                (type, subtype) = MapTypeToEngine (Child (first, "Type")?.InnerText, Child (first, "Subtype")?.InnerText, unsupported);
                foreach (var other in series) {
                    if (Child (other, "Type")?.InnerText != Child (first, "Type")?.InnerText) {
                        unsupported.Add ("series of different types");
                        break;
                    }
                }
            }
            result.AppendChild (El (doc, ns, "Type", type));
            result.AppendChild (El (doc, ns, "Subtype", subtype));

            var categories = Child (chart, "ChartCategoryHierarchy");
            var categoryGroupings = El (doc, ns, "CategoryGroupings");
            AppendGroupings (doc, ns, categoryGroupings, Child (categories, "ChartMembers"),
                "CategoryGrouping", "DynamicCategories", "StaticCategories", unsupported);
            if (categoryGroupings.HasChildNodes)
                result.AppendChild (categoryGroupings);

            var seriesHierarchy = Child (chart, "ChartSeriesHierarchy");
            var seriesGroupings = El (doc, ns, "SeriesGroupings");
            AppendGroupings (doc, ns, seriesGroupings, Child (seriesHierarchy, "ChartMembers"),
                "SeriesGrouping", "DynamicSeries", "StaticSeries", unsupported);
            var dynamicSeries = Child (seriesGroupings, "SeriesGrouping") != null
                && Child (Child (seriesGroupings, "SeriesGrouping"), "DynamicSeries") != null;

            // A dynamic series grouping repeats one template series; otherwise every series stands alone.
            var usedSeries = dynamicSeries ? series.GetRange (0, Math.Min (1, series.Count)) : series;
            if (dynamicSeries && series.Count > 1)
                unsupported.Add ("several series under a series group");

            if (!dynamicSeries && usedSeries.Count > 0) {
                var staticSeries = El (doc, ns, "StaticSeries");
                foreach (var s in usedSeries) {
                    var member = El (doc, ns, "StaticMember");
                    member.AppendChild (El (doc, ns, "Label", Child (s, "LegendText")?.InnerText ?? s.GetAttribute ("Name")));
                    staticSeries.AppendChild (member);
                }
                var grouping = El (doc, ns, "SeriesGrouping");
                grouping.AppendChild (staticSeries);
                seriesGroupings.AppendChild (grouping);
            }
            if (seriesGroupings.HasChildNodes)
                result.AppendChild (seriesGroupings);

            var data = El (doc, ns, "ChartData");
            foreach (var s in usedSeries)
                data.AppendChild (SeriesToEngine (doc, ns, s, type, subtype));
            result.AppendChild (data);

            var areas = Kids (Child (chart, "ChartAreas"), "ChartArea");
            if (areas.Count > 1)
                unsupported.Add ("several chart areas");
            if (areas.Count > 0) {
                var area = areas[0];
                var categoryAxes = Kids (Child (area, "ChartCategoryAxes"), "ChartAxis");
                var valueAxes = Kids (Child (area, "ChartValueAxes"), "ChartAxis");
                // Report Builder always declares a Secondary axis; only a series using it matters.
                if (series.Exists (x => IsSecondary (Child (x, "ValueAxisName")) || IsSecondary (Child (x, "CategoryAxisName"))))
                    unsupported.Add ("secondary axes");
                if (categoryAxes.Count > 0)
                    result.AppendChild (AxisToEngine (doc, ns, "CategoryAxis", categoryAxes[0]));
                if (valueAxes.Count > 0)
                    result.AppendChild (AxisToEngine (doc, ns, "ValueAxis", valueAxes[0]));
                var areaStyle = Child (area, "Style");
                if (areaStyle != null) {
                    var plot = El (doc, ns, "PlotArea");
                    plot.AppendChild (areaStyle.CloneNode (true));
                    result.AppendChild (plot);
                }
            }

            var legend = Child (Child (chart, "ChartLegends"), "ChartLegend");
            if (legend != null) {
                var l = El (doc, ns, "Legend");
                l.AppendChild (El (doc, ns, "Visible", IsTrue (Child (legend, "Hidden")?.InnerText) ? "false" : "true"));
                CopyIfPresent (legend, l, "Position", "Layout", "Style");
                result.AppendChild (l);
            }

            var title = Child (Child (chart, "ChartTitles"), "ChartTitle");
            if (title != null) {
                var t = El (doc, ns, "Title");
                CopyIfPresent (title, t, "Caption", "Style");
                result.AppendChild (t);
            }

            if (unsupported.Count > 0) {
                rl?.LogError (4, $"Chart '{name}' uses {string.Join (", ", unsupported)}, which the engine does not " +
                    "support; the first of each is rendered.");
            }

            return result;
        }

        private static bool IsSecondary (XmlElement axisName)
            => axisName != null && !string.Equals (axisName.InnerText.Trim (), "Primary", StringComparison.OrdinalIgnoreCase);

        private static readonly HashSet<string> EnginePalettes = new HashSet<string> (StringComparer.OrdinalIgnoreCase) {
            "Default", "EarthTones", "Excel", "GrayScale", "Light", "Pastel", "SemiTransparent", "Patterned", "PatternedBlack", "Custom",
        };

        private static (string Type, string Subtype) MapTypeToEngine (string type, string subtype, List<string> unsupported)
        {
            type = type?.Trim () ?? "Column";
            subtype = subtype?.Trim () ?? "Plain";
            switch (type) {
                case "Column":
                case "Bar":
                case "Area":
                    return (type, subtype is "Stacked" or "PercentStacked" or "Smooth" ? subtype : "Plain");
                case "Line":
                    return (type, subtype == "Smooth" ? "Smooth" : "Plain");
                case "Shape":
                    switch (subtype) {
                        case "Pie": return ("Pie", "Plain");
                        case "ExplodedPie": return ("Pie", "Exploded");
                        case "Doughnut": return ("Doughnut", "Plain");
                        case "ExplodedDoughnut": return ("Doughnut", "Exploded");
                        default:
                            unsupported.Add ($"the {subtype} shape chart");
                            return ("Pie", "Plain");
                    }
                case "Scatter":
                    return (type, subtype is "Line" or "SmoothLine" ? subtype : "Plain");
                case "Bubble":
                    return (type, "Plain");
                case "Stock":
                    return (type, subtype is "HighLowClose" or "OpenHighLowClose" or "Candlestick" ? subtype : "HighLowClose");
                default:
                    unsupported.Add ($"the {type} chart type");
                    return ("Column", "Plain");
            }
        }

        private static void AppendGroupings (XmlDocument doc, string ns, XmlElement parent, XmlElement members,
            string groupingName, string dynamicName, string staticName, List<string> unsupported)
        {
            if (members == null)
                return;

            var level = Kids (members, "ChartMember");
            var staticRun = (XmlElement) null;
            XmlElement nested = null;
            var groupCount = 0;

            foreach (var member in level) {
                var group = Child (member, "Group");
                if (group == null) {
                    if (staticRun == null) {
                        staticRun = El (doc, ns, staticName);
                        var wrapper = El (doc, ns, groupingName);
                        wrapper.AppendChild (staticRun);
                        parent.AppendChild (wrapper);
                    }
                    var staticMember = El (doc, ns, "StaticMember");
                    staticMember.AppendChild (El (doc, ns, "Label", Child (member, "Label")?.InnerText ?? string.Empty));
                    staticRun.AppendChild (staticMember);
                    continue;
                }

                if (++groupCount > 1) {
                    unsupported.Add ("adjacent groups");
                    continue;
                }

                var dynamic = El (doc, ns, dynamicName);
                var grouping = El (doc, ns, "Grouping");
                if (group.GetAttribute ("Name").Length > 0)
                    grouping.SetAttribute ("Name", group.GetAttribute ("Name"));
                foreach (XmlNode n in group.ChildNodes) {
                    if (n is XmlElement e)
                        grouping.AppendChild (e.CloneNode (true));
                }
                dynamic.AppendChild (grouping);

                var sort = Child (member, "SortExpressions");
                if (sort != null) {
                    var sorting = El (doc, ns, "Sorting");
                    foreach (var sortExpression in Kids (sort, "SortExpression")) {
                        var sortBy = El (doc, ns, "SortBy");
                        sortBy.AppendChild (El (doc, ns, "SortExpression", Child (sortExpression, "Value")?.InnerText ?? string.Empty));
                        var direction = Child (sortExpression, "Direction");
                        if (direction != null)
                            sortBy.AppendChild (direction.CloneNode (true));
                        sorting.AppendChild (sortBy);
                    }
                    dynamic.AppendChild (sorting);
                }

                var label = Child (member, "Label");
                if (label != null)
                    dynamic.AppendChild (El (doc, ns, "Label", label.InnerText));

                var wrapperGroup = El (doc, ns, groupingName);
                wrapperGroup.AppendChild (dynamic);
                parent.AppendChild (wrapperGroup);
                nested = Child (member, "ChartMembers");
            }

            // The levels inside a group become the next grouping.
            if (nested != null)
                AppendGroupings (doc, ns, parent, nested, groupingName, dynamicName, staticName, unsupported);
        }

        private static XmlElement SeriesToEngine (XmlDocument doc, string ns, XmlElement series, string type, string subtype)
        {
            var result = El (doc, ns, "ChartSeries");
            var points = El (doc, ns, "DataPoints");
            foreach (var point in Kids (Child (series, "ChartDataPoints"), "ChartDataPoint")) {
                var dataPoint = El (doc, ns, "DataPoint");
                var values = El (doc, ns, "DataValues");
                var source = Child (point, "ChartDataPointValues");
                foreach (var valueName in ValueNamesFor (type, subtype)) {
                    var value = Child (source, valueName);
                    if (value != null)
                        values.AppendChild (DataValue (doc, ns, value.InnerText));
                }
                if (!values.HasChildNodes)
                    values.AppendChild (DataValue (doc, ns, Child (source, "Y")?.InnerText ?? string.Empty));
                dataPoint.AppendChild (values);

                var label = Child (point, "ChartDataLabel");
                if (label != null) {
                    var dataLabel = El (doc, ns, "DataLabel");
                    var visible = Child (label, "Visible")?.InnerText;
                    dataLabel.AppendChild (El (doc, ns, "Visible", visible == null || IsTrue (visible) || visible == "Auto" ? "true" : "false"));
                    var text = Child (label, "Label");
                    if (text != null)
                        dataLabel.AppendChild (El (doc, ns, "Value", text.InnerText));
                    CopyIfPresent (label, dataLabel, "Style");
                    dataPoint.AppendChild (dataLabel);
                }

                var marker = Child (point, "ChartMarker");
                if (marker != null) {
                    var m = El (doc, ns, "Marker");
                    CopyIfPresent (marker, m, "Type", "Size", "Style");
                    dataPoint.AppendChild (m);
                }

                CopyIfPresent (point, dataPoint, "Style");
                points.AppendChild (dataPoint);
            }
            result.AppendChild (points);
            return result;
        }

        private static XmlElement DataValue (XmlDocument doc, string ns, string expression)
        {
            var value = El (doc, ns, "DataValue");
            value.AppendChild (El (doc, ns, "Value", expression));
            return value;
        }

        /// <summary>The data point values a chart type reads, in the order the 2005 DataValues expects.</summary>
        private static string[] ValueNamesFor (string type, string subtype)
        {
            switch (type) {
                case "Scatter": return new[] { "X", "Y" };
                case "Bubble": return new[] { "X", "Y", "Size" };
                case "Stock":
                    return subtype == "OpenHighLowClose" || subtype == "Candlestick"
                        ? new[] { "High", "Low", "Open", "Close" }
                        : new[] { "High", "Low", "Close" };
                default: return new[] { "Y" };
            }
        }

        private static XmlElement AxisToEngine (XmlDocument doc, string ns, string wrapperName, XmlElement source)
        {
            var wrapper = El (doc, ns, wrapperName);
            var axis = El (doc, ns, "Axis");

            var visible = Child (source, "Visible")?.InnerText;
            axis.AppendChild (El (doc, ns, "Visible", visible == null || visible == "Auto" || IsTrue (visible) ? "true" : "false"));

            var title = Child (source, "ChartAxisTitle");
            if (title != null) {
                var t = El (doc, ns, "Title");
                CopyIfPresent (title, t, "Caption", "Style");
                axis.AppendChild (t);
            }

            foreach (var (from, to) in new[] { ("ChartMajorGridLines", "MajorGridLines"), ("ChartMinorGridLines", "MinorGridLines") }) {
                var lines = Child (source, from);
                if (lines == null)
                    continue;
                var g = El (doc, ns, to);
                g.AppendChild (El (doc, ns, "ShowGridLines", Child (lines, "Enabled")?.InnerText is "False" or "false" ? "false" : "true"));
                CopyIfPresent (lines, g, "Style");
                axis.AppendChild (g);
            }

            CopyIfPresent (source, axis, "Style", "Min", "Max", "LogScale", "Reverse");
            var interval = Child (source, "Interval");
            if (interval != null)
                axis.AppendChild (El (doc, ns, "MajorInterval", interval.InnerText));

            wrapper.AppendChild (axis);
            return wrapper;
        }

        #endregion

        #region The engine to 2008+

        /// <returns>The 2008+ chart, or null when the chart cannot be expressed (the caller keeps it unchanged).</returns>
        internal static XmlElement FromEngine (XmlElement chart)
        {
            var doc = chart.OwnerDocument;
            var ns = chart.NamespaceURI;
            var type = Child (chart, "Type")?.InnerText?.Trim () ?? "Column";
            var subtype = Child (chart, "Subtype")?.InnerText?.Trim () ?? "Plain";
            if (type == "Map")
                return null;

            var (type2008, subtype2008) = MapTypeFromEngine (type, subtype);
            var name = chart.GetAttribute ("Name");

            var result = El (doc, ns, "Chart");
            if (name.Length > 0)
                result.SetAttribute ("Name", name);

            var seriesGroupings = Kids (Child (chart, "SeriesGroupings"), "SeriesGrouping");
            var categoryGroupings = Kids (Child (chart, "CategoryGroupings"), "CategoryGrouping");

            result.AppendChild (BuildHierarchy (doc, ns, "ChartCategoryHierarchy", categoryGroupings,
                "DynamicCategories", "StaticCategories", name + "_CategoryGroup"));
            var dynamicSeries = seriesGroupings.Exists (g => Child (g, "DynamicSeries") != null);
            if (dynamicSeries) {
                result.AppendChild (BuildHierarchy (doc, ns, "ChartSeriesHierarchy", seriesGroupings,
                    "DynamicSeries", "StaticSeries", name + "_SeriesGroup"));
            }

            var staticLabels = new List<string> ();
            foreach (var g in seriesGroupings) {
                foreach (var member in Kids (Child (g, "StaticSeries"), "StaticMember"))
                    staticLabels.Add (Child (member, "Label")?.InnerText ?? string.Empty);
            }

            var collection = El (doc, ns, "ChartSeriesCollection");
            var index = 0;
            foreach (var series in Kids (Child (chart, "ChartData"), "ChartSeries")) {
                index++;
                var s = El (doc, ns, "ChartSeries");
                s.SetAttribute ("Name", "Series" + index);

                var points = El (doc, ns, "ChartDataPoints");
                foreach (var point in Kids (Child (series, "DataPoints"), "DataPoint"))
                    points.AppendChild (DataPointFromEngine (doc, ns, point, type, subtype));
                s.AppendChild (points);

                s.AppendChild (El (doc, ns, "Type", type2008));
                s.AppendChild (El (doc, ns, "Subtype", subtype2008));
                if (!dynamicSeries && index - 1 < staticLabels.Count && staticLabels[index - 1].Length > 0)
                    s.AppendChild (El (doc, ns, "LegendText", staticLabels[index - 1]));
                s.AppendChild (El (doc, ns, "CategoryAxisName", "Primary"));
                s.AppendChild (El (doc, ns, "ValueAxisName", "Primary"));
                collection.AppendChild (s);
            }
            var data = El (doc, ns, "ChartData");
            data.AppendChild (collection);
            result.AppendChild (data);

            var area = El (doc, ns, "ChartArea");
            area.SetAttribute ("Name", "Default");
            var categoryAxes = El (doc, ns, "ChartCategoryAxes");
            categoryAxes.AppendChild (AxisFromEngine (doc, ns, Child (Child (chart, "CategoryAxis"), "Axis")));
            var valueAxes = El (doc, ns, "ChartValueAxes");
            valueAxes.AppendChild (AxisFromEngine (doc, ns, Child (Child (chart, "ValueAxis"), "Axis")));
            area.AppendChild (categoryAxes);
            area.AppendChild (valueAxes);
            CopyIfPresent (Child (chart, "PlotArea"), area, "Style");
            var areas = El (doc, ns, "ChartAreas");
            areas.AppendChild (area);
            result.AppendChild (areas);

            var legend = Child (chart, "Legend");
            if (legend != null) {
                var l = El (doc, ns, "ChartLegend");
                l.SetAttribute ("Name", "Default");
                var visible = Child (legend, "Visible")?.InnerText;
                if (visible != null && !IsTrue (visible))
                    l.AppendChild (El (doc, ns, "Hidden", "true"));
                CopyIfPresent (legend, l, "Position", "Layout", "Style");
                var legends = El (doc, ns, "ChartLegends");
                legends.AppendChild (l);
                result.AppendChild (legends);
            }

            var title = Child (chart, "Title");
            if (title != null) {
                var t = El (doc, ns, "ChartTitle");
                t.SetAttribute ("Name", "Default");
                CopyIfPresent (title, t, "Caption", "Style");
                var titles = El (doc, ns, "ChartTitles");
                titles.AppendChild (t);
                result.AppendChild (titles);
            }

            // Everything else (position, size, dataset, style, filters, palette, ...) is spelled the same.
            string[] engineOnly = { "Type", "Subtype", "CategoryGroupings", "SeriesGroupings", "ChartData", "CategoryAxis",
                "ValueAxis", "Legend", "Title", "PlotArea", "PointWidth", "ThreeDProperties", "ChartElementOutput",
                "RenderAsVector", "HyNewOnderfulVector" };
            foreach (XmlNode node in chart.ChildNodes) {
                if (node is XmlElement e && Array.IndexOf (engineOnly, e.LocalName) < 0)
                    result.AppendChild (e.CloneNode (true));
            }

            return result;
        }

        private static (string Type, string Subtype) MapTypeFromEngine (string type, string subtype)
        {
            switch (type) {
                case "Pie": return ("Shape", subtype == "Exploded" ? "ExplodedPie" : "Pie");
                case "Doughnut": return ("Shape", subtype == "Exploded" ? "ExplodedDoughnut" : "Doughnut");
                case "Bubble": return ("Bubble", "Plain");
                default: return (type, string.IsNullOrEmpty (subtype) ? "Plain" : subtype);
            }
        }

        private static XmlElement BuildHierarchy (XmlDocument doc, string ns, string hierarchyName,
            List<XmlElement> groupings, string dynamicName, string staticName, string defaultGroupName)
        {
            var hierarchy = El (doc, ns, hierarchyName);
            var members = El (doc, ns, "ChartMembers");
            hierarchy.AppendChild (members);

            var container = members;
            var counter = 0;
            foreach (var grouping in groupings) {
                var dynamic = Child (grouping, dynamicName);
                if (dynamic != null) {
                    var member = El (doc, ns, "ChartMember");
                    var source = Child (dynamic, "Grouping");
                    var group = El (doc, ns, "Group");
                    var groupName = source?.GetAttribute ("Name");
                    group.SetAttribute ("Name", string.IsNullOrEmpty (groupName) ? defaultGroupName + (++counter) : groupName);
                    if (source != null) {
                        foreach (XmlNode n in source.ChildNodes) {
                            if (n is XmlElement e)
                                group.AppendChild (e.CloneNode (true));
                        }
                    }
                    member.AppendChild (group);

                    var sorting = Child (dynamic, "Sorting");
                    if (sorting != null)
                        member.AppendChild (SortExpressionsFromEngine (doc, ns, sorting));
                    var label = Child (dynamic, "Label");
                    if (label != null)
                        member.AppendChild (El (doc, ns, "Label", label.InnerText));

                    var nested = El (doc, ns, "ChartMembers");
                    member.AppendChild (nested);
                    container.AppendChild (member);
                    container = nested;
                    continue;
                }

                foreach (var staticMember in Kids (Child (grouping, staticName), "StaticMember")) {
                    var member = El (doc, ns, "ChartMember");
                    member.AppendChild (El (doc, ns, "Label", Child (staticMember, "Label")?.InnerText ?? string.Empty));
                    container.AppendChild (member);
                }
            }

            // An innermost group with nothing inside it carries no ChartMembers.
            RemoveEmptyMembers (members);
            if (!members.HasChildNodes)
                members.AppendChild (El (doc, ns, "ChartMember"));
            return hierarchy;
        }

        private static void RemoveEmptyMembers (XmlElement members)
        {
            foreach (var member in Kids (members, "ChartMember")) {
                var nested = Child (member, "ChartMembers");
                if (nested == null)
                    continue;
                RemoveEmptyMembers (nested);
                if (!nested.HasChildNodes)
                    member.RemoveChild (nested);
            }
        }

        private static XmlElement SortExpressionsFromEngine (XmlDocument doc, string ns, XmlElement sorting)
        {
            var result = El (doc, ns, "SortExpressions");
            foreach (var sortBy in Kids (sorting, "SortBy")) {
                var sortExpression = El (doc, ns, "SortExpression");
                sortExpression.AppendChild (El (doc, ns, "Value", Child (sortBy, "SortExpression")?.InnerText ?? string.Empty));
                CopyIfPresent (sortBy, sortExpression, "Direction");
                result.AppendChild (sortExpression);
            }
            return result;
        }

        private static XmlElement DataPointFromEngine (XmlDocument doc, string ns, XmlElement point, string type, string subtype)
        {
            var result = El (doc, ns, "ChartDataPoint");
            var values = El (doc, ns, "ChartDataPointValues");
            var names = ValueNamesFor (type, subtype);
            var index = 0;
            foreach (var value in Kids (Child (point, "DataValues"), "DataValue")) {
                if (index >= names.Length)
                    break;
                values.AppendChild (El (doc, ns, names[index++], Child (value, "Value")?.InnerText ?? string.Empty));
            }
            result.AppendChild (values);

            var label = Child (point, "DataLabel");
            if (label != null) {
                var l = El (doc, ns, "ChartDataLabel");
                var visible = Child (label, "Visible")?.InnerText;
                if (visible != null)
                    l.AppendChild (El (doc, ns, "Visible", IsTrue (visible) ? "true" : "false"));
                var value = Child (label, "Value");
                if (value != null)
                    l.AppendChild (El (doc, ns, "Label", value.InnerText));
                CopyIfPresent (label, l, "Style");
                result.AppendChild (l);
            }

            var marker = Child (point, "Marker");
            if (marker != null) {
                var m = El (doc, ns, "ChartMarker");
                CopyIfPresent (marker, m, "Type", "Size", "Style");
                result.AppendChild (m);
            }

            CopyIfPresent (point, result, "Style");
            return result;
        }

        private static XmlElement AxisFromEngine (XmlDocument doc, string ns, XmlElement source)
        {
            var axis = El (doc, ns, "ChartAxis");
            axis.SetAttribute ("Name", "Primary");
            if (source == null)
                return axis;

            var visible = Child (source, "Visible")?.InnerText;
            axis.AppendChild (El (doc, ns, "Visible", visible != null && IsTrue (visible) ? "true" : "false"));

            var title = Child (source, "Title");
            if (title != null) {
                var t = El (doc, ns, "ChartAxisTitle");
                CopyIfPresent (title, t, "Caption", "Style");
                axis.AppendChild (t);
            }

            foreach (var (from, to) in new[] { ("MajorGridLines", "ChartMajorGridLines"), ("MinorGridLines", "ChartMinorGridLines") }) {
                var lines = Child (source, from);
                if (lines == null)
                    continue;
                var g = El (doc, ns, to);
                g.AppendChild (El (doc, ns, "Enabled", Child (lines, "ShowGridLines")?.InnerText is "False" or "false" ? "false" : "true"));
                CopyIfPresent (lines, g, "Style");
                axis.AppendChild (g);
            }

            CopyIfPresent (source, axis, "Style", "Min", "Max", "LogScale", "Reverse");
            var interval = Child (source, "MajorInterval");
            if (interval != null)
                axis.AppendChild (El (doc, ns, "Interval", interval.InnerText));
            return axis;
        }

        #endregion

        #region Helpers

        private static bool IsTrue (string text)
            => string.Equals (text?.Trim (), "true", StringComparison.OrdinalIgnoreCase);

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

        private static void CopyIfPresent (XmlElement from, XmlElement to, params string[] names)
        {
            if (from == null)
                return;
            foreach (var name in names) {
                var source = Child (from, name);
                if (source != null)
                    to.AppendChild (source.CloneNode (true));
            }
        }

        #endregion
    }
}
