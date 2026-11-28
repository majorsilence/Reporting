using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// One shape of a vector layer at run time: its geometry, its spatial fields (what the layer's
    /// MapBindingFieldPairs match data on), and, once bound, the row of the data group it shows.
    /// </summary>
    internal sealed class MapSpatialElement
    {
        internal MapGeometry Geometry;
        internal readonly Dictionary<string, object> Fields = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);

        /// <summary>The embedded element it came from, for its own template; null from a dataset.</summary>
        internal MapEmbeddedElement Embedded;

        /// <summary>The first row of the data group bound to the element, or null when none is.</summary>
        internal Row Row;

        internal object Field (string name) => name != null && Fields.TryGetValue (name, out var v) ? v : null;
    }

    /// <summary>
    /// A spatial element written into the report (MapPolygon, MapPoint or MapLine): its VectorData
    /// and its MapFields. The geometry is read once, when the report is parsed.
    /// </summary>
    internal class MapEmbeddedElement : ReportLink
    {
        internal readonly string VectorData;
        internal readonly Dictionary<string, string> Fields = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
        private MapGeometry _geometry;
        private bool _read;

        internal MapEmbeddedElement (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                switch (node.LocalName) {
                    case "VectorData":
                        VectorData = node.InnerText;
                        break;
                    case "MapFields":
                        foreach (XmlNode field in node.ChildNodes) {
                            if (field is XmlElement e && e.LocalName == "MapField") {
                                var name = e.GetAttribute ("Name");
                                if (!string.IsNullOrEmpty (name))
                                    Fields[name] = Child (e, "Value")?.InnerText ?? e.InnerText;
                            }
                        }
                        break;
                    default:
                        if (!ElementOfKind (node))
                            OwnerReport.rl.LogError (4, "Unknown " + xNode.Name + " element '" + node.Name + "' ignored.");
                        break;
                }
            }
        }

        /// <summary>
        /// Reads an element only this kind has (a polygon's own template); false when it is
        /// unknown. Called from the constructor, so it may only set fields.
        /// </summary>
        protected virtual bool ElementOfKind (XmlNode node) => false;

        internal MapGeometry Geometry (bool geographic)
        {
            if (!_read) {
                _geometry = MapGeometry.Read (VectorData, geographic);
                _read = true;
                if (_geometry == null && !string.IsNullOrWhiteSpace (VectorData))
                    OwnerReport.rl.LogError (4, "A map element's VectorData is not spatial data the engine can read; it is not drawn.");
            }
            return _geometry;
        }

        internal MapSpatialElement ToElement (bool geographic)
        {
            var element = new MapSpatialElement { Geometry = Geometry (geographic), Embedded = this };
            foreach (var pair in Fields)
                element.Fields[pair.Key] = pair.Value;
            return element;
        }

        internal static XmlElement Child (XmlNode parent, string localName)
        {
            foreach (XmlNode node in parent.ChildNodes) {
                if (node is XmlElement e && e.LocalName == localName)
                    return e;
            }
            return null;
        }
    }

    /// <summary>
    /// Shapes read from a dataset's spatial field (MapSpatialDataSet): one element per row, with
    /// the fields named in MapFieldNames for binding.
    /// </summary>
    internal sealed class MapSpatialDataSet : ReportLink
    {
        private readonly string _dataSetName;
        private readonly string _spatialField;
        private readonly List<string> _fieldNames = new List<string> ();
        private DataSetDefn _dataSet;

        internal MapSpatialDataSet (ReportDefn r, ReportLink p, XmlNode xNode) : base (r, p)
        {
            foreach (XmlNode node in xNode.ChildNodes) {
                switch (node.LocalName) {
                    case "DataSetName":
                        _dataSetName = node.InnerText.Trim ();
                        break;
                    case "SpatialField":
                        _spatialField = node.InnerText.Trim ();
                        break;
                    case "MapFieldNames":
                        foreach (XmlNode name in node.ChildNodes) {
                            if (name.NodeType == XmlNodeType.Element && name.LocalName == "MapFieldName")
                                _fieldNames.Add (name.InnerText.Trim ());
                        }
                        break;
                }
            }
        }

        internal override Task FinalPass ()
        {
            if (_dataSetName != null && OwnerReport.DataSetsDefn != null)
                _dataSet = OwnerReport.DataSetsDefn.Items[_dataSetName] as DataSetDefn;
            if (_dataSet == null)
                OwnerReport.rl.LogError (4, $"The map's spatial dataset '{_dataSetName}' is not in the report's DataSets; its shapes are not drawn.");
            else if (FieldOf (_spatialField) == null)
                OwnerReport.rl.LogError (4, $"The map's spatial field '{_spatialField}' is not a field of dataset '{_dataSetName}'; its shapes are not drawn.");
            return Task.CompletedTask;
        }

        private Field FieldOf (string name)
            => name == null || _dataSet?.Fields == null ? null : _dataSet.Fields.Items[name] as Field;

        internal List<MapSpatialElement> Elements (Report rpt, bool geographic)
        {
            var elements = new List<MapSpatialElement> ();
            var spatial = FieldOf (_spatialField);
            var rows = spatial == null ? null : _dataSet.Query?.GetMyData (rpt);
            if (rows?.Data == null)
                return elements;

            var fields = new List<(string Name, Field Field)> ();
            foreach (var name in _fieldNames) {
                var f = FieldOf (name);
                if (f != null)
                    fields.Add ((name, f));
            }

            foreach (Row row in rows.Data) {
                var geometry = Value (row, spatial);
                var element = new MapSpatialElement { Geometry = MapGeometry.Read (geometry, geographic) };
                if (element.Geometry == null)
                    continue;
                foreach (var (name, field) in fields)
                    element.Fields[name] = Value (row, field);
                elements.Add (element);
            }
            return elements;
        }

        private static object Value (Row row, Field field)
        {
            var at = field.ColumnNumber;
            return at >= 0 && at < row.Data.Length ? row.Data[at] : null;
        }
    }
}
