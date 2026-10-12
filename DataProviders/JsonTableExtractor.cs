using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Majorsilence.Reporting.Data
{
    public class JsonTableExtractor
    {
        private readonly Dictionary<string, Table> _tables = new();

        /// <summary>
        /// One table of rows. A row is an array indexed by column. The columns are those of the table's
        /// first row; names that only appear in later rows are dropped, as they always have been.
        /// </summary>
        private sealed class Table
        {
            public readonly List<string> Names = new();
            public readonly Dictionary<string, int> Index = new(StringComparer.Ordinal);
            public readonly List<object[]> Rows = new();
            public bool Defined;            // the first row is complete, so the columns are fixed
        }

        private sealed class RowBuilder
        {
            private readonly Table _table;
            private readonly bool _first;
            private readonly List<object> _firstValues;
            private readonly object[] _values;

            public RowBuilder(Table table)
            {
                _table = table;
                _first = !table.Defined;
                if (_first)
                    _firstValues = new List<object>();
                else
                    _values = new object[table.Names.Count];
            }

            public void Set(string name, object value)
            {
                if (_table.Index.TryGetValue(name, out int i))
                {
                    if (_first) _firstValues[i] = value;
                    else _values[i] = value;
                }
                else if (_first)
                {
                    _table.Index[name] = _table.Names.Count;
                    _table.Names.Add(name);
                    _firstValues.Add(value);
                }
            }

            public void Commit()
            {
                if (_first)
                {
                    _table.Rows.Add(_firstValues.ToArray());
                    _table.Defined = true;
                }
                else
                    _table.Rows.Add(_values);
            }
        }

        private Table GetTable(string name)
        {
            if (!_tables.TryGetValue(name, out var table))
                _tables[name] = table = new Table();
            return table;
        }

        public Dictionary<string, IDataReader> Extract(string json) => Extract(Encoding.UTF8.GetBytes(json));

        /// <summary>
        /// Reads a document straight from its bytes. Nothing is made into a string first, and for a
        /// top-level array only one element is parsed at a time.
        /// </summary>
        public Dictionary<string, IDataReader> Extract(byte[] json)
        {
            ReadOnlySpan<byte> utf8 = ToUtf8(json);
            var reader = new Utf8JsonReader(utf8);

            if (!reader.Read())
                throw new JsonException("The JSON document is empty.");

            var root = GetTable("root");
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    using var item = JsonDocument.ParseValue(ref reader);
                    var row = new RowBuilder(root);
                    var guid = Guid.NewGuid().ToString();
                    row.Set("__guid", guid);

                    Flatten(item.RootElement, row, "root", guid);
                    row.Commit();
                }

                if (reader.Read())
                    throw new JsonException("Unexpected content after the end of the top-level array.");
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var row = new RowBuilder(root);
                var guid = Guid.NewGuid().ToString();
                row.Set("__guid", guid);

                Flatten(doc.RootElement, row, "root", guid);
                row.Commit();

                if (reader.Read())
                    throw new JsonException("Unexpected content after the end of the top-level object.");
            }
            else
            {
                throw new InvalidOperationException("Root element must be an array or object.");
            }

            return _tables.ToDictionary(kv => kv.Key,
                kv => (IDataReader)new DictionaryDataReader(kv.Value.Names.ToArray(), kv.Value.Rows));
        }

        // Utf8JsonReader rejects a byte order mark, and a UTF-16 or UTF-32 document has to be decoded first
        // (a StreamReader did both of these before).
        private static ReadOnlySpan<byte> ToUtf8(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return bytes.AsSpan(3);

            bool utf16 = bytes.Length >= 2 &&
                         ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE));
            bool utf32 = bytes.Length >= 4 &&
                         ((bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0xFE && bytes[3] == 0xFF) ||
                          (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0));
            if (utf16 || utf32)
            {
                using var sr = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return Encoding.UTF8.GetBytes(sr.ReadToEnd());
            }

            return bytes;
        }

        private void Flatten(JsonElement element, RowBuilder currentRow, string currentTable,
            string parentGuid)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var name = prop.Name;
                var val = prop.Value;

                switch (val.ValueKind)
                {
                    case JsonValueKind.Array:
                        // Handle arrays as before - creates child tables
                        var tableName = $"{currentTable}_{name}";
                        var childTable = GetTable(tableName);

                        foreach (var item in val.EnumerateArray())
                        {
                            var childRow = new RowBuilder(childTable);
                            childRow.Set("__parent_guid", parentGuid);

                            if (item.ValueKind == JsonValueKind.Object)
                                Flatten(item, childRow, tableName, parentGuid);
                            else
                                childRow.Set(name, GetPrimitiveValue(item));

                            childRow.Commit();
                        }

                        break;

                    case JsonValueKind.Object:
                        // Two approaches for objects:
                        // 1. Create a nested dictionary and store the entire object
                        var nestedDict = new Dictionary<string, object>();
                        foreach (var nested in val.EnumerateObject())
                        {
                            if (nested.Value.ValueKind == JsonValueKind.Object ||
                                nested.Value.ValueKind == JsonValueKind.Array)
                            {
                                // For deeply nested objects, recursively process them
                                var childGuid = Guid.NewGuid().ToString();
                                var nestedTable = $"{currentTable}_{name}_{nested.Name}";

                                if (nested.Value.ValueKind == JsonValueKind.Object)
                                {
                                    // Handle nested object
                                    var nestedTableData = GetTable(nestedTable);

                                    var childRow = new RowBuilder(nestedTableData);
                                    childRow.Set("__parent_guid", parentGuid);
                                    Flatten(nested.Value, childRow, nestedTable, childGuid);
                                    childRow.Commit();
                                }

                                nestedDict[nested.Name] = GetPrimitiveValue(nested.Value);
                            }
                            else
                            {
                                nestedDict[nested.Name] = GetPrimitiveValue(nested.Value);
                            }
                        }

                        currentRow.Set(name, nestedDict);

                        // 2. Also flatten the object properties with prefixed names (for backward compatibility)
                        foreach (var nested in val.EnumerateObject())
                        {
                            if (nested.Value.ValueKind != JsonValueKind.Object &&
                                nested.Value.ValueKind != JsonValueKind.Array)
                            {
                                currentRow.Set($"{name}_{nested.Name}", GetPrimitiveValue(nested.Value));
                            }
                        }

                        break;

                    default:
                        currentRow.Set(name, GetPrimitiveValue(val));
                        break;
                }
            }
        }

        private object? GetPrimitiveValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString() // Fallback to raw JSON for unhandled types
            };
        }
    }
}