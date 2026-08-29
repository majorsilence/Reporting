using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDoctor;
using ModelContextProtocol.Server;

namespace Majorsilence.Reporting.Mcp;

/// <summary>
/// The MCP tool surface over Majorsilence Reporting's engine and authoring APIs.
///
/// Everything here is in-process against the RDL engine — no GUI. Reports are taken as a file path
/// or as inline XML; when a path is given, subreports, images and shared data-source references
/// resolve relative to that file's directory, the same as every other engine entry point.
///
/// Failures a caller can act on — a report that won't parse, an unknown format, a data source that
/// won't connect — come back as text in the result rather than as exceptions.
/// </summary>
[McpServerToolType]
public sealed class ReportTools
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // format token -> (engine type, file extension, whether the output is text rather than binary)
    private static readonly Dictionary<string, (OutputPresentationType Type, string Ext, bool IsText)> Formats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["pdf"] = (OutputPresentationType.PDF, "pdf", false),
            ["html"] = (OutputPresentationType.HTML, "html", true),
            ["htm"] = (OutputPresentationType.HTML, "html", true),
            ["mhtml"] = (OutputPresentationType.MHTML, "mht", true),
            ["mht"] = (OutputPresentationType.MHTML, "mht", true),
            ["xml"] = (OutputPresentationType.XML, "xml", true),
            ["csv"] = (OutputPresentationType.CSV, "csv", true),
            ["rtf"] = (OutputPresentationType.RTF, "rtf", true),
            ["xlsx"] = (OutputPresentationType.Excel2007, "xlsx", false),
            ["xlsx_table"] = (OutputPresentationType.ExcelTableOnly, "xlsx", false),
            ["tif"] = (OutputPresentationType.TIF, "tif", false),
            ["tiff"] = (OutputPresentationType.TIF, "tif", false),
            ["tifbw"] = (OutputPresentationType.TIFBW, "tif", false),
        };

    /// <summary>Lists the output formats the engine can render to.</summary>
    [McpServerTool(Name = "report_formats", ReadOnly = true, Idempotent = true)]
    [Description("List the output formats report_render accepts, with the file extension each produces " +
                 "and whether it is a text or a binary format.")]
    public string Formats_List() =>
        JsonSerializer.Serialize(
            Formats.Where(kv => kv.Key is not ("htm" or "mht" or "tiff"))   // drop the aliases
                   .Select(kv => new { format = kv.Key, extension = kv.Value.Ext, kind = kv.Value.IsText ? "text" : "binary" }),
            Json);

    /// <summary>Parses a report and reports its structure.</summary>
    [McpServerTool(Name = "report_inspect", ReadOnly = true, Idempotent = true)]
    [Description("Parse an .rdl/.rdlc report and report its structure as JSON: name, description, " +
                 "author, page size, margins, report parameters (type, prompt, nullable, multi-value, " +
                 "defaults), datasets and their fields, data sources and providers, a summary of the " +
                 "body report items, and any parse errors or warnings the engine raised.")]
    public async Task<string> Inspect_Report(
        [Description("Path to an .rdl/.rdlc file. Give this OR 'xml', not both.")]
        string? path = null,
        [Description("Inline RDL XML. Give this OR 'path', not both.")]
        string? xml = null,
        [Description("Pass phrase for a report that uses a shared (encrypted) data-source reference.")]
        string? dataSourceReferencePassword = null,
        CancellationToken cancellationToken = default)
    {
        return await Guard(async () =>
        {
            if (!TryResolveSource(path, xml, out var source, out var folder, out var sourceError))
                return sourceError;

            var structure = ReadStructureFromXml(source);

            // Compile pass: relay the engine's own errors/warnings and the resolved parameters.
            var (report, messages) = await Compile(source, folder, connectionStringOverride: null,
                dataSourceReferencePassword, cancellationToken).ConfigureAwait(false);

            var result = new
            {
                structure.name,
                structure.description,
                structure.author,
                structure.pageWidth,
                structure.pageHeight,
                structure.margins,
                parameters = report is not null ? ReadParameters(report) : structure.parameters,
                structure.dataSources,
                structure.dataSets,
                structure.bodyItems,
                parsed = report is not null,
                messages,
            };

            return JsonSerializer.Serialize(result, Json);
        });
    }

    /// <summary>Runs the rdl-doctor compatibility checks against a report.</summary>
    [McpServerTool(Name = "report_lint", ReadOnly = true, Idempotent = true)]
    [Description("Check an .rdl/.rdlc report for constructs Majorsilence Reporting's engine does not " +
                 "support (Tablix, Gauge/Indicator/Sparkline/Map data regions, Lookup* functions, " +
                 "unknown elements, unresolved subreports/images, and more) — the same checks as the " +
                 "rdl-doctor CLI. Returns a JSON list of findings with an id, severity and message.")]
    public async Task<string> Lint_Report(
        [Description("Path to an .rdl/.rdlc file. Give this OR 'xml', not both.")]
        string? path = null,
        [Description("Inline RDL XML. Give this OR 'path', not both.")]
        string? xml = null,
        CancellationToken cancellationToken = default)
    {
        return await Guard(async () =>
        {
            if (!TryResolveSource(path, xml, out _, out _, out var sourceError))
                return sourceError;

            // CompatibilityChecker works off a file path (it resolves external resources relative to
            // it), so inline XML is spilled to a temp file first.
            var (filePath, temp) = await MaterializeAsync(path, xml, cancellationToken).ConfigureAwait(false);
            try
            {
                IReadOnlyList<Finding> findings = await CompatibilityChecker.CheckAsync(filePath).ConfigureAwait(false);
                var payload = new
                {
                    findingCount = findings.Count,
                    worst = findings.Count == 0 ? "none" : findings.Max(f => f.Severity).ToString(),
                    findings = findings.Select(f => new { id = f.Id, severity = f.Severity.ToString(), message = f.Message }),
                };
                return JsonSerializer.Serialize(payload, Json);
            }
            finally
            {
                if (temp) TryDelete(filePath);
            }
        });
    }

    /// <summary>Renders a report to a chosen format.</summary>
    [McpServerTool(Name = "report_render", Destructive = false)]
    [Description("Render an .rdl/.rdlc report to a format (see report_formats). Optionally pass report " +
                 "parameters and a connection-string override. If 'outputPath' is given the bytes are " +
                 "written there and a summary is returned; otherwise a text format is returned inline " +
                 "and a binary format is returned base64-encoded (up to ~8 MB).")]
    public async Task<string> Render_Report(
        [Description("Output format token, e.g. 'pdf', 'html', 'xlsx', 'csv'. See report_formats.")]
        string format,
        [Description("Path to an .rdl/.rdlc file. Give this OR 'xml', not both.")]
        string? path = null,
        [Description("Inline RDL XML. Give this OR 'path', not both.")]
        string? xml = null,
        [Description("Report parameters as a JSON object of name -> value, e.g. {\"Year\":\"2026\"}.")]
        string? parameters = null,
        [Description("Connection string to use instead of the one in the report definition.")]
        string? connectionStringOverride = null,
        [Description("Where to write the rendered file. If omitted, the result is returned inline.")]
        string? outputPath = null,
        [Description("Pass phrase for a report that uses a shared (encrypted) data-source reference.")]
        string? dataSourceReferencePassword = null,
        CancellationToken cancellationToken = default)
    {
        return await Guard(async () =>
        {
            if (!Formats.TryGetValue(format, out var fmt))
                return $"Unknown format '{format}'. Call report_formats for the list.";

            if (!TryResolveSource(path, xml, out var source, out var folder, out var sourceError))
                return sourceError;

            IDictionary? parms;
            try
            {
                parms = ParseParameters(parameters);
            }
            catch (JsonException ex)
            {
                return $"'parameters' is not a valid JSON object: {ex.Message}";
            }

            var (report, messages) = await Compile(source, folder, connectionStringOverride,
                dataSourceReferencePassword, cancellationToken).ConfigureAwait(false);

            if (report is null)
                return JsonSerializer.Serialize(new { rendered = false, messages }, Json);

            await report.RunGetData(parms!).ConfigureAwait(false);   // the engine handles a null parms

            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, fmt.Type).ConfigureAwait(false);

            CollectRuntimeMessages(report, messages);

            var stream = sg.GetStream();
            stream.Position = 0;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            var bytes = ms.ToArray();

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var full = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllBytesAsync(full, bytes, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(new
                {
                    rendered = true, format, path = full, bytes = bytes.Length, messages,
                }, Json);
            }

            const int InlineLimit = 8 * 1024 * 1024;
            if (bytes.Length > InlineLimit)
                return JsonSerializer.Serialize(new
                {
                    rendered = true, format, bytes = bytes.Length, messages,
                    note = $"Output is {bytes.Length:N0} bytes — too large to return inline. Re-run with 'outputPath'.",
                }, Json);

            if (fmt.IsText)
                return JsonSerializer.Serialize(new
                {
                    rendered = true, format, bytes = bytes.Length, messages,
                    text = Encoding.UTF8.GetString(bytes),
                }, Json);

            return JsonSerializer.Serialize(new
            {
                rendered = true, format, bytes = bytes.Length, messages,
                base64 = Convert.ToBase64String(bytes),
            }, Json);
        });
    }

    /// <summary>Builds a minimal RDL from a small JSON spec.</summary>
    [McpServerTool(Name = "report_scaffold")]
    [Description("Build a minimal, valid RDL document from a small JSON spec and return the XML " +
                 "(and write it to 'outputPath' if given). The spec is: " +
                 "{ \"name\": \"...\", \"description\"?: \"...\", \"author\"?: \"...\", " +
                 "\"pageWidth\"?: \"8.5in\", \"pageHeight\"?: \"11in\", " +
                 "\"dataSource\"?: { \"name\": \"DS1\", \"provider\": \"Microsoft.Data.Sqlite\", \"connectionString\": \"...\" }, " +
                 "\"dataSet\"?: { \"name\": \"Data\", \"query\": \"SELECT ...\", \"fields\": [ { \"name\": \"Id\", \"type\": \"System.Int64\" } ] }, " +
                 "\"table\"?: { \"columns\": [ { \"field\": \"Id\", \"header\": \"ID\", \"width\"?: \"1in\" } ] }, " +
                 "\"textboxes\"?: [ { \"text\": \"Title\", \"top\": \"0in\", \"left\": \"0in\", \"width\": \"6in\", \"height\": \".3in\", \"bold\"?: true, \"fontSize\"?: \"14pt\" } ] }. " +
                 "The report is parsed to validate it before returning.")]
    public async Task<string> Scaffold_Report(
        [Description("The JSON spec described above.")]
        string spec,
        [Description("Where to write the generated .rdl. If omitted, the XML is only returned.")]
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        return await Guard(async () =>
        {
            ScaffoldSpec model;
            try
            {
                model = JsonSerializer.Deserialize<ScaffoldSpec>(spec,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new JsonException("spec was null");
            }
            catch (JsonException ex)
            {
                return $"'spec' is not valid JSON for the scaffold format: {ex.Message}";
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                return "'spec.name' is required.";

            var xml = BuildRdl(model);

            // Validate: a scaffold that does not parse is worse than an error message.
            var parser = new RDLParser(xml);
            var report = await parser.Parse().ConfigureAwait(false);
            var messages = DrainMessages(report);
            if (report is null || report.ErrorMaxSeverity > 4)
                return JsonSerializer.Serialize(new { ok = false, messages, xml }, Json);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                var full = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllTextAsync(full, xml, cancellationToken).ConfigureAwait(false);
                return JsonSerializer.Serialize(new { ok = true, path = full, messages, xml }, Json);
            }

            return JsonSerializer.Serialize(new { ok = true, messages, xml }, Json);
        });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task<string> Guard(Func<Task<string>> body)
    {
        try
        {
            return await body().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return $"The operation failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static bool TryResolveSource(string? path, string? xml, out string source, out string? folder, out string error)
    {
        source = string.Empty;
        folder = null;
        error = string.Empty;

        var hasPath = !string.IsNullOrWhiteSpace(path);
        var hasXml = !string.IsNullOrWhiteSpace(xml);

        if (hasPath == hasXml)
        {
            error = "Give exactly one of 'path' or 'xml'.";
            return false;
        }

        if (hasPath)
        {
            var full = Path.GetFullPath(path!);
            if (!File.Exists(full))
            {
                error = $"No file at '{full}'.";
                return false;
            }
            source = File.ReadAllText(full);
            folder = Path.GetDirectoryName(full);
            return true;
        }

        source = xml!;
        folder = null;
        return true;
    }

    private static async Task<(string Path, bool Temp)> MaterializeAsync(string? path, string? xml, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(path))
            return (Path.GetFullPath(path), false);

        var temp = Path.Combine(Path.GetTempPath(), $"mcp-report-{Guid.NewGuid():N}.rdl");
        await File.WriteAllTextAsync(temp, xml!, ct).ConfigureAwait(false);
        return (temp, true);
    }

    private static async Task<(Report? Report, List<string> Messages)> Compile(
        string source, string? folder, string? connectionStringOverride, string? password, CancellationToken ct)
    {
        var messages = new List<string>();
        try
        {
            var parser = new RDLParser(source)
            {
                Folder = folder ?? Environment.CurrentDirectory,
            };
            if (!string.IsNullOrWhiteSpace(connectionStringOverride))
                parser.OverwriteConnectionString = connectionStringOverride;
            if (!string.IsNullOrWhiteSpace(password))
                parser.DataSourceReferencePassword = new NeedPassword(() => password!);

            var report = await parser.Parse().ConfigureAwait(false);
            if (report is null)
            {
                messages.Add("The report could not be parsed.");
                return (null, messages);
            }

            if (!string.IsNullOrEmpty(folder))
            {
                report.Folder = folder;
            }
            if (!string.IsNullOrWhiteSpace(password))
                report.GetDataSourceReferencePassword = new NeedPassword(() => password!);

            var severity = report.ErrorMaxSeverity;
            messages.AddRange(DrainMessages(report));

            return severity > 4 ? (null, messages) : (report, messages);
        }
        catch (Exception ex)
        {
            messages.Add($"{ex.GetType().Name}: {ex.Message}");
            return (null, messages);
        }
    }

    private static List<string> DrainMessages(Report? report)
    {
        var list = new List<string>();
        if (report is null || report.ErrorMaxSeverity <= 0)
            return list;
        foreach (var m in report.ErrorItems)
            list.Add(m?.ToString() ?? string.Empty);
        report.ErrorReset();
        return list;
    }

    private static void CollectRuntimeMessages(Report report, List<string> into)
    {
        if (report.ErrorMaxSeverity <= 0)
            return;
        foreach (var m in report.ErrorItems)
            into.Add(m?.ToString() ?? string.Empty);
        report.ErrorReset();
    }

    private static IDictionary? ParseParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var dict = new System.Collections.Specialized.ListDictionary();
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var value = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => prop.Value.GetRawText(),
            };
            dict.Add(prop.Name, value);
        }
        return dict;
    }

    private static IEnumerable<object> ReadParameters(Report report)
    {
        foreach (UserReportParameter p in report.UserReportParameters)
        {
            object[]? defaults = null;
            try { defaults = p.DefaultValue; } catch { /* expression default that needs data */ }

            yield return new
            {
                name = p.Name,
                type = p.dt.ToString(),
                nullable = p.Nullable,
                allowBlank = p.AllowBlank,
                multiValue = p.MultiValue,
                prompt = p.Prompt,
                defaultValues = defaults?.Select(d => d?.ToString()).ToArray(),
            };
        }
    }

    // Namespace-agnostic structural read straight off the XML — works even when the report will not
    // fully compile, and does not need the engine's internal definition classes.
    private static (string? name, string? description, string? author, string? pageWidth, string? pageHeight,
        object margins, object[] parameters, object[] dataSources, object[] dataSets, object[] bodyItems)
        ReadStructureFromXml(string xml)
    {
        XElement root;
        try
        {
            root = XDocument.Parse(xml).Root ?? throw new InvalidOperationException("empty document");
        }
        catch
        {
            return (null, null, null, null, null,
                new { }, Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>(), Array.Empty<object>());
        }

        string? El(XElement? e, string local) => e?.Elements().FirstOrDefault(x => x.Name.LocalName == local)?.Value;
        IEnumerable<XElement> Els(XElement? e, string local) =>
            e?.Descendants().Where(x => x.Name.LocalName == local) ?? Enumerable.Empty<XElement>();
        string? Attr(XElement e, string name) => e.Attributes().FirstOrDefault(a => a.Name.LocalName == name)?.Value;

        var margins = new
        {
            top = El(root, "TopMargin"),
            left = El(root, "LeftMargin"),
            right = El(root, "RightMargin"),
            bottom = El(root, "BottomMargin"),
        };

        var parameters = Els(root, "ReportParameter").Select(p => (object)new
        {
            name = Attr(p, "Name"),
            type = El(p, "DataType"),
            nullable = El(p, "Nullable"),
            multiValue = El(p, "MultiValue"),
            prompt = El(p, "Prompt"),
        }).ToArray();

        var dataSources = Els(root, "DataSource").Select(ds =>
        {
            var conn = ds.Elements().FirstOrDefault(x => x.Name.LocalName == "ConnectionProperties");
            return (object)new
            {
                name = Attr(ds, "Name"),
                provider = El(conn, "DataProvider"),
                integratedSecurity = El(conn, "IntegratedSecurity"),
            };
        }).ToArray();

        var dataSets = Els(root, "DataSet").Select(dset =>
        {
            var query = dset.Elements().FirstOrDefault(x => x.Name.LocalName == "Query");
            var fields = dset.Elements().FirstOrDefault(x => x.Name.LocalName == "Fields");
            return (object)new
            {
                name = Attr(dset, "Name"),
                dataSourceName = El(query, "DataSourceName"),
                commandText = El(query, "CommandText"),
                fields = (fields?.Elements().Where(x => x.Name.LocalName == "Field") ?? Enumerable.Empty<XElement>())
                    .Select(f => new
                    {
                        name = Attr(f, "Name"),
                        dataField = El(f, "DataField"),
                        type = f.Elements().FirstOrDefault(x => x.Name.LocalName == "TypeName")?.Value,
                        value = El(f, "Value"),
                    }).ToArray(),
            };
        }).ToArray();

        var body = root.Elements().FirstOrDefault(x => x.Name.LocalName == "Body");
        var reportItems = body?.Elements().FirstOrDefault(x => x.Name.LocalName == "ReportItems");
        var containerKinds = new HashSet<string> { "Textbox", "Table", "Matrix", "List", "Rectangle", "Image", "Subreport", "Chart", "Line", "CustomReportItem" };
        var bodyItems = (reportItems?.Elements() ?? Enumerable.Empty<XElement>())
            .Where(x => containerKinds.Contains(x.Name.LocalName))
            .Select(x => (object)new
            {
                kind = x.Name.LocalName,
                name = Attr(x, "Name"),
                dataSetName = El(x, "DataSetName"),
            }).ToArray();

        return (
            El(root, "Name") ?? Attr(root, "Name"),
            El(root, "Description"),
            El(root, "Author"),
            El(root, "PageWidth"),
            El(root, "PageHeight"),
            margins, parameters, dataSources, dataSets, bodyItems);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    // ── scaffold ─────────────────────────────────────────────────────────────

    private sealed class ScaffoldSpec
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? PageWidth { get; set; }
        public string? PageHeight { get; set; }
        public DataSourceSpec? DataSource { get; set; }
        public DataSetSpec? DataSet { get; set; }
        public TableSpec? Table { get; set; }
        public List<TextboxSpec>? Textboxes { get; set; }
    }

    private sealed class DataSourceSpec { public string? Name { get; set; } public string? Provider { get; set; } public string? ConnectionString { get; set; } }
    private sealed class DataSetSpec { public string? Name { get; set; } public string? Query { get; set; } public List<FieldSpec>? Fields { get; set; } }
    private sealed class FieldSpec { public string? Name { get; set; } public string? Type { get; set; } public string? DataField { get; set; } }
    private sealed class TableSpec { public List<TableColumnSpec>? Columns { get; set; } }
    private sealed class TableColumnSpec { public string? Field { get; set; } public string? Header { get; set; } public string? Width { get; set; } }
    private sealed class TextboxSpec { public string? Text { get; set; } public string? Top { get; set; } public string? Left { get; set; } public string? Width { get; set; } public string? Height { get; set; } public bool Bold { get; set; } public string? FontSize { get; set; } }

    private static readonly XNamespace Rdl = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

    private static XElement E(string name, params object?[] content) =>
        new(Rdl + name, content.Where(c => c is not null).ToArray());

    private static string BuildRdl(ScaffoldSpec m)
    {
        var report = E("Report");

        if (!string.IsNullOrEmpty(m.Description)) report.Add(E("Description", m.Description));
        if (!string.IsNullOrEmpty(m.Author)) report.Add(E("Author", m.Author));
        report.Add(E("PageWidth", m.PageWidth ?? "8.5in"));
        report.Add(E("PageHeight", m.PageHeight ?? "11in"));
        report.Add(E("Width", "7.5in"));
        foreach (var margin in new[] { "TopMargin", "LeftMargin", "RightMargin", "BottomMargin" })
            report.Add(E(margin, "0.5in"));

        if (m.DataSource is { } ds)
        {
            report.Add(E("DataSources",
                E("DataSource",
                    new XAttribute("Name", ds.Name ?? "DS1"),
                    E("ConnectionProperties",
                        E("DataProvider", ds.Provider ?? "Microsoft.Data.Sqlite"),
                        E("ConnectString", ds.ConnectionString ?? string.Empty)))));
        }

        if (m.DataSet is { } dset)
        {
            var fields = E("Fields");
            foreach (var f in dset.Fields ?? new())
                fields.Add(E("Field",
                    new XAttribute("Name", f.Name ?? "Field"),
                    E("DataField", f.DataField ?? f.Name ?? "Field"),
                    E("TypeName", f.Type ?? "System.String")));

            report.Add(E("DataSets",
                E("DataSet",
                    new XAttribute("Name", dset.Name ?? "Data"),
                    E("Query",
                        E("DataSourceName", m.DataSource?.Name ?? "DS1"),
                        E("CommandText", dset.Query ?? string.Empty)),
                    fields)));
        }

        var items = E("ReportItems");
        var n = 0;

        foreach (var tb in m.Textboxes ?? new())
        {
            var style = E("Style");
            if (!string.IsNullOrEmpty(tb.FontSize)) style.Add(E("FontSize", tb.FontSize));
            if (tb.Bold) style.Add(E("FontWeight", "Bold"));

            var textbox = E("Textbox",
                new XAttribute("Name", $"Textbox{++n}"),
                E("Value", tb.Text ?? string.Empty),
                E("Top", tb.Top ?? "0in"),
                E("Left", tb.Left ?? "0in"),
                E("Width", tb.Width ?? "6in"),
                E("Height", tb.Height ?? "0.25in"));
            if (style.HasElements) textbox.Add(style);
            items.Add(textbox);
        }

        if (m.Table is { Columns.Count: > 0 } table && m.DataSet is { } tdset)
        {
            XElement Cell(string value, bool bold)
            {
                var tbx = E("Textbox", new XAttribute("Name", $"Textbox{++n}"), E("Value", value));
                if (bold) tbx.Add(E("Style", E("FontWeight", "Bold")));
                return E("TableCell", E("ReportItems", tbx));
            }

            var columns = E("TableColumns");
            var headerCells = E("TableCells");
            var detailCells = E("TableCells");
            foreach (var c in table.Columns)
            {
                columns.Add(E("TableColumn", E("Width", c.Width ?? "1.5in")));
                headerCells.Add(Cell(c.Header ?? c.Field ?? string.Empty, bold: true));
                detailCells.Add(Cell($"=Fields!{c.Field}.Value", bold: false));
            }

            items.Add(E("Table",
                new XAttribute("Name", "Table1"),
                E("DataSetName", tdset.Name ?? "Data"),
                E("Top", "0.5in"),
                E("Left", "0in"),
                columns,
                E("Header",
                    E("TableRows", E("TableRow", E("Height", "0.25in"), headerCells)),
                    E("RepeatOnNewPage", "true")),
                E("Details",
                    E("TableRows", E("TableRow", E("Height", "0.25in"), detailCells)))));
        }

        var body = E("Body", E("Height", "2in"));
        if (items.HasElements)
            body.Add(items);
        report.Add(body);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), report);
        using var sw = new Utf8StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
