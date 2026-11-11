#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReportTests
{
    /// <summary>
    /// Real reports made with Microsoft's tools (Reports/RealWorld, see its README for where each
    /// came from and its licence), rendered through the RDL 2008+ normaliser and natively.
    /// </summary>
    [TestFixture]
    public class RealWorldReportTests
    {
        // Several reports carry a VB <Code> block, which needs a compiler registered, as an
        // application running them would (see RoslynCodeTests).
        [SetUp]
        public void SetUp() => Majorsilence.Reporting.RoslynCode.RoslynCodeCompiler.Register();

        [TearDown]
        public void TearDown() => RdlEngineConfig.RegisterCodeCompiler(null);

        public static IEnumerable<string> Reports()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "Reports", "RealWorld");
            return Directory.GetFiles(folder, "*.rdl", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal);
        }

        /// <summary>Rows for each of the report's datasets, from its fields' declared types.</summary>
        internal static Dictionary<string, DataTable> SampleData(XDocument doc, int rows = 12)
        {
            var data = new Dictionary<string, DataTable>();
            foreach (var ds in doc.Descendants().Where(e => e.Name.LocalName == "DataSet"))
            {
                var table = new DataTable();
                var fields = ds.Elements().Where(e => e.Name.LocalName == "Fields").Elements().Where(e => e.Name.LocalName == "Field")
                    .Select(f => (Column: f.Elements().FirstOrDefault(e => e.Name.LocalName == "DataField")?.Value,
                                  Type: f.Elements().FirstOrDefault(e => e.Name.LocalName == "TypeName")?.Value))
                    .Where(f => !string.IsNullOrEmpty(f.Column)).ToList();
                foreach (var (column, type) in fields)
                    if (!table.Columns.Contains(column))
                        table.Columns.Add(column, ClrType(type));
                var start = new DateTime(2024, 1, 1, 8, 0, 0);
                for (int i = 0; i < rows; i++)
                {
                    var row = table.NewRow();
                    foreach (DataColumn c in table.Columns)
                        row[c] = Value(c.DataType, c.ColumnName, i, start);
                    table.Rows.Add(row);
                }
                data[(string)ds.Attribute("Name")] = table;
            }
            return data;
        }

        private static Type ClrType(string typeName)
        {
            switch (typeName)
            {
                case "System.Int16": case "System.Int32": case "System.Byte": case "System.SByte": return typeof(int);
                case "System.Int64": return typeof(long);
                case "System.Decimal": return typeof(decimal);
                case "System.Double": case "System.Single": return typeof(double);
                case "System.DateTime": case "System.DateTimeOffset": return typeof(DateTime);
                case "System.Boolean": return typeof(bool);
                case "System.Guid": return typeof(Guid);
                default: return typeof(string);
            }
        }

        // A few distinct values per column, so groups have several instances of several rows.
        private static object Value(Type t, string column, int i, DateTime start)
        {
            if (t == typeof(int)) return i % 5 + 1;
            if (t == typeof(long)) return (long)(i * 37 % 101);
            if (t == typeof(decimal)) return (decimal)(i * 13 % 29) + 0.25m;
            if (t == typeof(double)) return (i * 13 % 29) + 0.5;
            if (t == typeof(DateTime)) return start.AddDays(i % 4).AddHours(i);
            if (t == typeof(bool)) return i % 2 == 0;
            if (t == typeof(Guid)) return new Guid(i + 1, 0, 0, new byte[8]);
            return column + (i % 3);
        }

        /// <summary>A value for each parameter that has no default of its own.</summary>
        internal static Dictionary<string, string> SampleParameters(XDocument doc)
        {
            var values = new Dictionary<string, string>();
            foreach (var p in doc.Descendants().Where(e => e.Name.LocalName == "ReportParameter"))
            {
                if (p.Elements().Any(e => e.Name.LocalName == "DefaultValue"))
                    continue;
                string type = p.Elements().FirstOrDefault(e => e.Name.LocalName == "DataType")?.Value;
                values[(string)p.Attribute("Name")] = type switch
                {
                    "Integer" => "1",
                    "Float" => "1",
                    "Boolean" => "false",
                    "DateTime" => "2024-01-01",
                    _ => "x",
                };
            }
            return values;
        }

        internal sealed class Outcome
        {
            internal int Severity;
            internal string Errors;
            internal string[] Items;
            internal string RenderError;
            internal string Kinds;		// what each top-level region became
        }

        internal static async Task<Outcome> Render(string relativePath, bool native)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Reports", "RealWorld", relativePath);
            string rdl = File.ReadAllText(path);
            var doc = XDocument.Parse(rdl);
            RdlEngineConfig.RdlEngineConfigInit();
            var outcome = new Outcome();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true, NativeTablix = native, Folder = Path.GetDirectoryName(path) }.Parse();
            using (report)
            {
                outcome.Severity = report.ErrorMaxSeverity;
                outcome.Errors = string.Join(" | ", (report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()).Select(o => o.ToString()).Distinct());
                outcome.Kinds = string.Join(",", report.ReportDefinition?.Body?.ReportItems?.Items.Select(i => i.GetType().Name) ?? Enumerable.Empty<string>());
                if (report.ErrorMaxSeverity > 4)
                    return outcome;
                try
                {
                    foreach (var kv in SampleData(doc))
                        if (report.DataSets[kv.Key] != null)
                            await report.DataSets[kv.Key].SetData(kv.Value);
                    await report.RunGetData(SampleParameters(doc));
                    var pages = await report.BuildPages();
                    var items = new List<string>();
                    foreach (Page page in pages)
                        foreach (PageItem pi in page)
                            items.Add(string.Format(CultureInfo.InvariantCulture, "p{0} {1} {2:0.#},{3:0.#} {4:0.#}x{5:0.#} {6}",
                                page.PageNumber, pi.GetType().Name, pi.X, pi.Y, pi.W, pi.H, (pi as PageText)?.Text));
                    outcome.Items = items.ToArray();
                }
                catch (Exception e)
                {
                    outcome.RenderError = e.GetType().Name + ": " + e.Message;
                }
            }
            return outcome;
        }

        /// <summary>
        /// Reports whose native rendering differs from the normaliser's because the normaliser's is
        /// wrong, each checked by hand: they must still render natively, but are not compared.
        /// </summary>
        private static readonly Dictionary<string, string> KnownDifferences = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ReportingServicesExamples/ServerReports/Heatmap Calendar.rdl"] =
                "the Matrix the normaliser makes draws each month's header one week wide and 3.4pt tall, where the " +
                "report sizes it 0.42cm; the native pivot spans it over the month's weeks.",
        };

        /// <summary>
        /// The report's Tablixes the normaliser maps to a Table that loses their row groups (they
        /// are not one chain), which the native path runs as a pivot instead; null when none.
        /// </summary>
        private static string TablixesTheNormaliserFlattens(string report)
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load(Path.Combine(AppContext.BaseDirectory, "Reports", "RealWorld", report));
            var names = doc.GetElementsByTagName("Tablix").Cast<System.Xml.XmlElement>().ToList()	// a live list
                .Where(t => Rdl2008Normalizer.NativeKind(t) == "Table" && Rdl2008Normalizer.RunsNatively(t) == "TablixPivot")
                .Select(t => t.GetAttribute("Name")).ToList();
            return names.Count == 0 ? null : string.Join(", ", names);
        }

        [TestCaseSource(nameof(Reports))]
        public void IsUnmodifiedFromUpstream(string report)
        {
            // README.md says these are byte-for-byte copies; SHA256SUMS holds each one's checksum.
            string folder = Path.Combine(AppContext.BaseDirectory, "Reports", "RealWorld");
            var sums = File.ReadAllLines(Path.Combine(folder, "SHA256SUMS"))
                .Where(l => l.Length > 66)
                .ToDictionary(l => l.Substring(66).TrimStart('.', '/'), l => l.Substring(0, 64), StringComparer.Ordinal);
            Assert.That(sums.ContainsKey(report), Is.True, "every vendored report is listed in SHA256SUMS");
            using var sha = System.Security.Cryptography.SHA256.Create();
            string actual = Convert.ToHexString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(folder, report)))).ToLowerInvariant();
            Assert.That(actual, Is.EqualTo(sums[report]));
        }

        [TestCaseSource(nameof(Reports))]
        public async Task RendersTheSameNativelyAsNormalised(string report)
        {
            var normalised = await Render(report, native: false);
            var native = await Render(report, native: true);

            // The native path never fails where the normaliser does not. It may run regions the
            // normaliser refused or dropped, so reach more expressions; one using what the engine
            // does not support at all (DateTime.Now, Globals!ReportServerUrl, ...) fails either way.
            var Errors = (Outcome o) => o.Errors.Split(" | ").Where(e => e.StartsWith("Severity: 8") && !e.Contains("Tablix")).ToArray();
            static bool Unsupported(string e) => e.Contains("is an unknown identifer") || e.Contains("is not known.")
                || System.Text.RegularExpressions.Regex.IsMatch(e, "Globals '[^']*'  not found");
            Assert.That(Errors(native).Except(Errors(normalised)).Where(e => !Unsupported(e)), Is.Empty, "the native path adds no errors");

            if (normalised.Items == null)
            {
                string reason = normalised.RenderError ?? Errors(normalised).FirstOrDefault() ?? normalised.Errors;
                Assert.Ignore("The engine does not run this report yet: " + reason);
            }
            Assert.That(native.RenderError, Is.Null);
            Assert.That(native.Items, Is.Not.Empty);
            if (KnownDifferences.TryGetValue(report, out string why))
                Assert.Pass("Not compared: " + why);
            string flattened = TablixesTheNormaliserFlattens(report);
            if (flattened != null)
                Assert.Pass($"Not compared: the normaliser flattens the row groups of {flattened} into one row per " +
                    "record, totalling over the whole table; the native path lays the groups out.");
            // The two renders run a moment apart: a time of day printed (Globals!ExecutionTime, Now) may differ.
            static string[] Untimed(string[] items) => items.Select(i => System.Text.RegularExpressions.Regex.Replace(i, @"\d{1,2}:\d{2}(:\d{2})?", "hh:mm")).ToArray();
            normalised.Items = Untimed(normalised.Items);
            native.Items = Untimed(native.Items);
            Assert.That(native.Items, Is.EqualTo(normalised.Items), () => "first differences (normalised => native):\n" + string.Join("\n",
                Enumerable.Range(0, Math.Max(native.Items.Length, normalised.Items.Length))
                    .Where(i => i >= native.Items.Length || i >= normalised.Items.Length || native.Items[i] != normalised.Items[i]).Take(8)
                    .Select(i => (i < normalised.Items.Length ? normalised.Items[i] : "-") + "  =>  " + (i < native.Items.Length ? native.Items[i] : "-"))));
        }
    }
}
#endif
