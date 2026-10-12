using System.Globalization;
using System.Text;

namespace Majorsilence.Reporting.Benchmarks
{
    /// <summary>The documents the data provider benchmarks and the peak memory runs read.</summary>
    public static class ReadBenchmarkData
    {
        // 10 columns: int, long, double, bool, 6 strings (one of them a date)
        public static List<Dictionary<string, object>> Create(string jsonFile, string xmlFile, int rows)
        {
            var json = new StringBuilder("[");
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?><rows>");
            var list = new List<Dictionary<string, object>>(rows);
            for (int i = 0; i < rows; i++)
            {
                var d = new Dictionary<string, object>
                {
                    ["Id"] = (long)i,
                    ["Count"] = (long)(i * 7 % 1000),
                    ["Price"] = i * 1.25 + 0.5,
                    ["Active"] = i % 2 == 0,
                    ["Name"] = "Customer " + i,
                    ["City"] = "City " + (i % 50),
                    ["Email"] = "user" + i + "@example.com",
                    ["Created"] = "2024-01-" + (1 + i % 28).ToString("00") + "T10:00:00",
                    ["Notes"] = "Some free text for row " + i + " with a few words in it",
                    ["Code"] = "C" + (i % 9999).ToString("0000"),
                };
                list.Add(d);

                if (i > 0) json.Append(',');
                json.Append('{');
                xml.Append("<row>");
                bool first = true;
                foreach (var (k, v) in d)
                {
                    if (!first) json.Append(',');
                    first = false;
                    string text = Convert.ToString(v, CultureInfo.InvariantCulture)!;
                    json.Append('"').Append(k).Append("\":");
                    if (v is string) json.Append('"').Append(text).Append('"');
                    else json.Append(v is bool b ? (b ? "true" : "false") : text);
                    xml.Append('<').Append(k).Append('>').Append(text).Append("</").Append(k).Append('>');
                }
                json.Append('}');
                xml.Append("</row>");
            }
            json.Append(']');
            xml.Append("</rows>");
            File.WriteAllText(jsonFile, json.ToString());
            File.WriteAllText(xmlFile, xml.ToString());
            return list;
        }
    }
}
