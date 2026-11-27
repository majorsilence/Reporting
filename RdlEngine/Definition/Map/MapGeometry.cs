using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>A coordinate of a map: longitude and latitude, or planar x and y.</summary>
    internal readonly struct MapPoint
    {
        internal readonly double X;
        internal readonly double Y;

        internal MapPoint (double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// The shapes of one spatial element (a map polygon, line or point), flattened: polygon rings
    /// (outer rings and holes together, drawn with the even-odd rule), lines, and points.
    /// </summary>
    /// <remarks>
    /// Read from the forms a report carries spatial data in: the base64 VectorData of an embedded
    /// element, which Report Builder writes in SQL Server's own serialisation of geometry and
    /// geography values (MS-SSCLRT), and the values of a dataset's spatial field, which arrive as
    /// that serialisation, as well-known binary, or as well-known text (what SqlGeometry and
    /// SqlGeography give from ToString). Circular arcs (SQL Server 2012's version 2 format) are
    /// read as straight lines between their points.
    /// </remarks>
    internal sealed class MapGeometry
    {
        internal readonly List<MapPoint[]> Rings = new List<MapPoint[]> ();
        internal readonly List<MapPoint[]> Lines = new List<MapPoint[]> ();
        internal readonly List<MapPoint> Points = new List<MapPoint> ();

        internal bool IsEmpty => Rings.Count == 0 && Lines.Count == 0 && Points.Count == 0;

        /// <summary>
        /// Reads a spatial value. <paramref name="geographic"/> says the map is in geographic
        /// coordinates, where a SQL Server geography value stores latitude before longitude.
        /// Returns null when the value is not spatial data in a form it knows.
        /// </summary>
        internal static MapGeometry Read (object value, bool geographic)
        {
            try {
                switch (value) {
                    case null:
                    case DBNull _:
                        return null;
                    case byte[] bytes:
                        return ReadBinary (bytes, geographic);
                    case string text:
                        return ReadText (text, geographic);
                    default:
                        // SqlGeometry and SqlGeography give their well-known text from ToString.
                        return ReadText (Convert.ToString (value, CultureInfo.InvariantCulture), geographic);
                }
            } catch (Exception e) when (e is FormatException || e is EndOfStreamException || e is ArgumentException || e is OverflowException || e is IndexOutOfRangeException) {
                return null;
            }
        }

        private static MapGeometry ReadText (string text, bool geographic)
        {
            if (string.IsNullOrWhiteSpace (text))
                return null;
            text = text.Trim ();
            if (char.IsLetter (text[0]))
                return Wkt.Read (text);
            return ReadBinary (Convert.FromBase64String (text), geographic);
        }

        private static MapGeometry ReadBinary (byte[] bytes, bool geographic)
        {
            if (bytes == null || bytes.Length < 5)
                return null;
            // A big-endian well-known binary point and a SQL Server value with SRID 0 start alike:
            // well-known binary only when it reads to the last byte.
            if (Wkb.Looks (bytes)) {
                try {
                    var wkb = Wkb.Read (bytes);
                    if (wkb != null)
                        return wkb;
                } catch (Exception e) when (e is FormatException || e is ArgumentException || e is IndexOutOfRangeException) {
                }
            }
            return SqlSerialization.Read (bytes, geographic);
        }

        internal void AddFigure (MapPoint[] points, FigureKind kind)
        {
            if (points == null || points.Length == 0)
                return;
            switch (kind) {
                case FigureKind.Ring:
                    if (points.Length >= 3)
                        Rings.Add (points);
                    break;
                case FigureKind.Line:
                    if (points.Length >= 2)
                        Lines.Add (points);
                    break;
                default:
                    Points.AddRange (points);
                    break;
            }
        }

        internal enum FigureKind { Point, Line, Ring }

        #region SQL Server serialisation

        /// <summary>The CLR serialisation of geometry and geography values (MS-SSCLRT), versions 1 and 2.</summary>
        private static class SqlSerialization
        {
            internal static MapGeometry Read (byte[] bytes, bool geographic)
            {
                using var reader = new BinaryReader (new MemoryStream (bytes));
                var srid = reader.ReadInt32 ();
                var version = reader.ReadByte ();
                if (version != 1 && version != 2)
                    return null;
                var properties = reader.ReadByte ();
                bool hasZ = (properties & 0x01) != 0, hasM = (properties & 0x02) != 0;
                bool singlePoint = (properties & 0x08) != 0, singleSegment = (properties & 0x10) != 0;

                // Geography stores latitude first. A geography value always has a geodetic SRID; a
                // geometry one usually 0 or a projected system, and is read in x, y order.
                var latitudeFirst = geographic && ((srid >= 4120 && srid <= 4999) || srid == 104001);
                MapPoint Point (double a, double b) => latitudeFirst ? new MapPoint (b, a) : new MapPoint (a, b);

                var geometry = new MapGeometry ();
                if (singlePoint || singleSegment) {
                    var count = singlePoint ? 1 : 2;
                    var points = new MapPoint[count];
                    for (var i = 0; i < count; i++)
                        points[i] = Point (reader.ReadDouble (), reader.ReadDouble ());
                    geometry.AddFigure (points, singlePoint ? FigureKind.Point : FigureKind.Line);
                    return geometry;
                }

                var pointCount = reader.ReadInt32 ();
                if (pointCount < 0 || pointCount > bytes.Length / 16)
                    return null;
                var all = new MapPoint[pointCount];
                for (var i = 0; i < pointCount; i++)
                    all[i] = Point (reader.ReadDouble (), reader.ReadDouble ());
                if (hasZ)
                    reader.BaseStream.Seek (8L * pointCount, SeekOrigin.Current);
                if (hasM)
                    reader.BaseStream.Seek (8L * pointCount, SeekOrigin.Current);

                var figureCount = reader.ReadInt32 ();
                if (figureCount < 0 || figureCount > bytes.Length / 5)
                    return null;
                var figureOffsets = new int[figureCount];
                for (var i = 0; i < figureCount; i++) {
                    reader.ReadByte ();     // attribute: interior/exterior ring, line, arc; the shape says what it is
                    figureOffsets[i] = reader.ReadInt32 ();
                }

                var shapeCount = reader.ReadInt32 ();
                if (shapeCount < 0 || shapeCount > bytes.Length / 9)
                    return null;
                var shapes = new (int Figure, byte Type)[shapeCount];
                for (var i = 0; i < shapeCount; i++) {
                    reader.ReadInt32 ();    // parent shape
                    shapes[i] = (reader.ReadInt32 (), reader.ReadByte ());
                }

                // Shapes are in pre-order, and a collection's figure offset is its first child's, so
                // the figures of a simple shape run up to the next shape that has any.
                for (var s = 0; s < shapeCount; s++) {
                    var kind = KindOf (shapes[s].Type);
                    if (kind == null || shapes[s].Figure < 0)
                        continue;
                    var endFigure = figureCount;
                    for (var n = s + 1; n < shapeCount; n++) {
                        if (shapes[n].Figure >= 0) {
                            endFigure = shapes[n].Figure;
                            break;
                        }
                    }
                    for (var f = shapes[s].Figure; f < endFigure && f < figureCount; f++) {
                        var start = figureOffsets[f];
                        var end = f + 1 < figureCount ? figureOffsets[f + 1] : pointCount;
                        if (start < 0 || end > pointCount || end <= start)
                            continue;
                        var points = new MapPoint[end - start];
                        Array.Copy (all, start, points, 0, points.Length);
                        geometry.AddFigure (points, kind.Value);
                    }
                }
                return geometry;
            }

            private static FigureKind? KindOf (byte openGisType)
            {
                switch (openGisType) {
                    case 1: return FigureKind.Point;
                    case 2:                         // LineString
                    case 8:                         // CircularString
                    case 9: return FigureKind.Line; // CompoundCurve
                    case 3:                         // Polygon
                    case 10: return FigureKind.Ring; // CurvePolygon
                    default: return null;           // collections hold the simple shapes; FullGlobe has no outline
                }
            }
        }

        #endregion

        #region Well-known binary

        private static class Wkb
        {
            /// <summary>A byte-order mark followed by a known type number, which SQL Server's SRID never gives.</summary>
            internal static bool Looks (byte[] bytes)
            {
                if (bytes[0] > 1)
                    return false;
                var type = bytes[0] == 1
                    ? (uint)(bytes[1] | bytes[2] << 8 | bytes[3] << 16 | bytes[4] << 24)
                    : (uint)(bytes[4] | bytes[3] << 8 | bytes[2] << 16 | bytes[1] << 24);
                var basic = (type & 0x0FFFFFFF) % 1000;
                return basic >= 1 && basic <= 7 && ((type & 0x0FFFFFFF) / 1000) <= 3;
            }

            internal static MapGeometry Read (byte[] bytes)
            {
                var geometry = new MapGeometry ();
                var position = 0;
                ReadOne (bytes, ref position, geometry, 0);
                return position == bytes.Length ? geometry : null;
            }

            private static void ReadOne (byte[] bytes, ref int position, MapGeometry geometry, int depth)
            {
                if (depth > 32)
                    throw new FormatException ("Nested too deeply.");
                var little = bytes[position++] == 1;
                var type = ReadUInt (bytes, ref position, little);
                var hasZ = (type & 0x80000000) != 0;
                var hasM = (type & 0x40000000) != 0;
                if ((type & 0x20000000) != 0)
                    ReadUInt (bytes, ref position, little);     // EWKB SRID
                var code = type & 0x0FFFFFFF;
                hasZ |= code / 1000 == 1 || code / 1000 == 3;
                hasM |= code / 1000 == 2 || code / 1000 == 3;
                var dimensions = 2 + (hasZ ? 1 : 0) + (hasM ? 1 : 0);

                switch (code % 1000) {
                    case 1:
                        geometry.AddFigure (new[] { ReadPoint (bytes, ref position, little, dimensions) }, FigureKind.Point);
                        break;
                    case 2:
                        geometry.AddFigure (ReadPoints (bytes, ref position, little, dimensions), FigureKind.Line);
                        break;
                    case 3:
                        var rings = ReadUInt (bytes, ref position, little);
                        for (var i = 0; i < rings; i++)
                            geometry.AddFigure (ReadPoints (bytes, ref position, little, dimensions), FigureKind.Ring);
                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        var parts = ReadUInt (bytes, ref position, little);
                        for (var i = 0; i < parts; i++)
                            ReadOne (bytes, ref position, geometry, depth + 1);
                        break;
                    default:
                        throw new FormatException ("Unknown geometry type " + code + ".");
                }
            }

            private static MapPoint[] ReadPoints (byte[] bytes, ref int position, bool little, int dimensions)
            {
                var count = ReadUInt (bytes, ref position, little);
                if (count > (uint)(bytes.Length / (8 * dimensions)))
                    throw new FormatException ("Too many points.");
                var points = new MapPoint[count];
                for (var i = 0; i < count; i++)
                    points[i] = ReadPoint (bytes, ref position, little, dimensions);
                return points;
            }

            private static MapPoint ReadPoint (byte[] bytes, ref int position, bool little, int dimensions)
            {
                var x = ReadDouble (bytes, ref position, little);
                var y = ReadDouble (bytes, ref position, little);
                position += 8 * (dimensions - 2);
                return new MapPoint (x, y);
            }

            private static uint ReadUInt (byte[] bytes, ref int position, bool little)
            {
                var span = new ReadOnlySpan<byte> (bytes, position, 4);
                position += 4;
                return little ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian (span)
                              : System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian (span);
            }

            private static double ReadDouble (byte[] bytes, ref int position, bool little)
            {
                var span = new ReadOnlySpan<byte> (bytes, position, 8);
                position += 8;
                var bits = little ? System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian (span)
                                  : System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian (span);
                return BitConverter.Int64BitsToDouble (bits);
            }
        }

        #endregion

        #region Well-known text

        /// <summary>
        /// POINT, LINESTRING, POLYGON, their MULTI forms and GEOMETRYCOLLECTION, with or without Z
        /// and M, and the EWKT "SRID=n;" prefix. CIRCULARSTRING and the other curves are read as lines through their points.
        /// </summary>
        private static class Wkt
        {
            private static readonly string[] Tags = {
                "POINT", "LINESTRING", "CIRCULARSTRING", "POLYGON", "MULTIPOINT", "MULTILINESTRING", "MULTIPOLYGON",
                "GEOMETRYCOLLECTION", "COMPOUNDCURVE", "CURVEPOLYGON",
            };

            internal static MapGeometry Read (string text)
            {
                if (text.StartsWith ("SRID=", StringComparison.OrdinalIgnoreCase)) {
                    var semicolon = text.IndexOf (';');
                    text = semicolon < 0 ? string.Empty : text.Substring (semicolon + 1);
                }
                var geometry = new MapGeometry ();
                var position = 0;
                ReadTagged (text, ref position, geometry, 0);
                return geometry;
            }

            private static void ReadTagged (string text, ref int position, MapGeometry geometry, int depth)
            {
                if (depth > 32)
                    throw new FormatException ("Nested too deeply.");
                var tag = Word (text, ref position).ToUpperInvariant ();
                if (Array.IndexOf (Tags, tag) < 0)
                    throw new FormatException ("Unknown geometry " + tag + ".");
                var next = Peek (text, position);
                while (next == 'Z' || next == 'M' || next == 'z' || next == 'm') {    // dimension markers: Z, M, ZM
                    Word (text, ref position);
                    next = Peek (text, position);
                }
                if (Peek (text, position) != '(') {
                    Word (text, ref position);  // EMPTY
                    return;
                }

                switch (tag) {
                    case "POINT":
                        geometry.AddFigure (List (text, ref position), FigureKind.Point);
                        break;
                    case "LINESTRING":
                    case "CIRCULARSTRING":
                        geometry.AddFigure (List (text, ref position), FigureKind.Line);
                        break;
                    case "POLYGON":
                        Lists (text, ref position, geometry, FigureKind.Ring);
                        break;
                    case "MULTIPOINT":
                        // both MULTIPOINT ((1 2), (3 4)) and MULTIPOINT (1 2, 3 4) occur
                        Expect (text, ref position, '(');
                        do {
                            if (Peek (text, position) == '(')
                                geometry.AddFigure (List (text, ref position), FigureKind.Point);
                            else
                                geometry.AddFigure (new[] { Coordinate (text, ref position) }, FigureKind.Point);
                        } while (Comma (text, ref position));
                        Expect (text, ref position, ')');
                        break;
                    case "MULTILINESTRING":
                        Lists (text, ref position, geometry, FigureKind.Line);
                        break;
                    case "MULTIPOLYGON":
                        Expect (text, ref position, '(');
                        do
                            Lists (text, ref position, geometry, FigureKind.Ring);
                        while (Comma (text, ref position));
                        Expect (text, ref position, ')');
                        break;
                    case "GEOMETRYCOLLECTION":
                    case "COMPOUNDCURVE":
                    case "CURVEPOLYGON":
                        // their members are tagged (or, in a curve, plain lists of a line)
                        Expect (text, ref position, '(');
                        do {
                            if (Peek (text, position) == '(')
                                geometry.AddFigure (List (text, ref position), tag == "CURVEPOLYGON" ? FigureKind.Ring : FigureKind.Line);
                            else
                                ReadTagged (text, ref position, geometry, depth + 1);
                        } while (Comma (text, ref position));
                        Expect (text, ref position, ')');
                        break;
                    default:
                        throw new FormatException ("Unknown geometry " + tag + ".");
                }
            }

            /// <summary>( (list), (list), ... ) with each list a figure of the kind.</summary>
            private static void Lists (string text, ref int position, MapGeometry geometry, FigureKind kind)
            {
                Expect (text, ref position, '(');
                do
                    geometry.AddFigure (List (text, ref position), kind);
                while (Comma (text, ref position));
                Expect (text, ref position, ')');
            }

            private static MapPoint[] List (string text, ref int position)
            {
                var points = new List<MapPoint> ();
                Expect (text, ref position, '(');
                do
                    points.Add (Coordinate (text, ref position));
                while (Comma (text, ref position));
                Expect (text, ref position, ')');
                return points.ToArray ();
            }

            private static MapPoint Coordinate (string text, ref int position)
            {
                var x = Number (text, ref position);
                var y = Number (text, ref position);
                while (Peek (text, position) != ',' && Peek (text, position) != ')' && Peek (text, position) != '\0')
                    Number (text, ref position);    // Z and M
                return new MapPoint (x, y);
            }

            private static double Number (string text, ref int position)
            {
                SkipSpace (text, ref position);
                var start = position;
                while (position < text.Length && (char.IsDigit (text[position]) || "+-.eE".IndexOf (text[position]) >= 0))
                    position++;
                return double.Parse (text.AsSpan (start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private static string Word (string text, ref int position)
            {
                SkipSpace (text, ref position);
                var start = position;
                while (position < text.Length && char.IsLetter (text[position]))
                    position++;
                return text.Substring (start, position - start);
            }

            private static bool Comma (string text, ref int position)
            {
                if (Peek (text, position) != ',')
                    return false;
                SkipSpace (text, ref position);
                position++;
                return true;
            }

            private static void Expect (string text, ref int position, char c)
            {
                if (Peek (text, position) != c)
                    throw new FormatException ($"Expected '{c}' at {position}.");
                SkipSpace (text, ref position);
                position++;
            }

            private static char Peek (string text, int position)
            {
                while (position < text.Length && char.IsWhiteSpace (text[position]))
                    position++;
                return position < text.Length ? text[position] : '\0';
            }

            private static void SkipSpace (string text, ref int position)
            {
                while (position < text.Length && char.IsWhiteSpace (text[position]))
                    position++;
            }
        }

        #endregion
    }
}
