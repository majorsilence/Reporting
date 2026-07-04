using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Majorsilence.Reporting.Rdl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Majorsilence.Reporting.WebDesigner;

public static class ViewerEndpoints
{
    /// <summary>
    /// Maps the RDL viewer API endpoints:
    /// <list type="bullet">
    ///   <item><c>POST /{prefix}/render</c> — body: <c>{"name"|"rdl","parameters":{},"format":"html"|"pdf"|"csv"|"xlsx"}</c></item>
    ///   <item><c>GET  /{prefix}/render/{name}.{ext}?param=value...</c> — convenience route; query string becomes report parameters</item>
    ///   <item><c>GET  /{prefix}/parameters/{name}</c> — report-parameter metadata (name, type, default, prompt, ...) as JSON</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapRdlViewer(
        this IEndpointRouteBuilder app,
        RdlViewerOptions? options = null)
    {
        options ??= app.ServiceProvider?.GetService<RdlViewerOptions>()
                   ?? new RdlViewerOptions();

        EnsureEngineConfig();

        var prefix = options.RoutePrefix.Trim('/');

        // ── Render (JSON body) ───────────────────────────────────────────────────
        app.MapPost($"/{prefix}/render", async (HttpRequest req) =>
        {
            RdlRenderRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<RdlRenderRequest>(
                    req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return Results.BadRequest("Expected JSON body: {\"name\"|\"rdl\", \"parameters\":{}, \"format\":\"html\"}");
            }

            if (body is null || (string.IsNullOrWhiteSpace(body.Name) && string.IsNullOrWhiteSpace(body.Rdl)))
                return Results.BadRequest("Either name or rdl is required.");

            return await RenderAsync(options, body.Name, body.Rdl, body.Parameters, body.Format ?? "html");
        });

        // ── Render (convenience GET route) ──────────────────────────────────────
        app.MapGet($"/{prefix}/render/{{name}}.{{ext}}", async (string name, string ext, HttpRequest req) =>
        {
            var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in req.Query)
                parameters[kv.Key] = kv.Value.ToString();

            return await RenderAsync(options, name, null, parameters, ext);
        });

        // ── Parameter metadata ───────────────────────────────────────────────────
        app.MapGet($"/{prefix}/parameters/{{name}}", async (string name) =>
        {
            var path = ResolveReportPath(options.ReportsFolder, name);
            if (!File.Exists(path))
                return Results.NotFound($"Report '{name}' not found.");

            try
            {
                var rdlXml = await File.ReadAllTextAsync(path);
                var rdlp = new RDLParser(rdlXml) { Folder = options.ReportsFolder };
                var report = await rdlp.Parse();

                if (report.ErrorMaxSeverity >= 8)
                    return Results.Problem(detail: "Report has parse errors.", statusCode: 400);

                var list = new List<object>();
                foreach (UserReportParameter urp in report.UserReportParameters)
                {
                    object? defaultValue = null;
                    try
                    {
                        var dv = await urp.GetValueAsync();
                        defaultValue = dv;
                    }
                    catch { /* expression-driven defaults that need data context are left null here */ }

                    list.Add(new
                    {
                        name = urp.Name,
                        typeName = urp.dt.ToString(),
                        nullable = urp.Nullable,
                        allowBlank = urp.AllowBlank,
                        multiValue = urp.MultiValue,
                        prompt = urp.Prompt,
                        defaultValue,
                    });
                }

                return Results.Ok(new { parameters = list });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 400);
            }
        });

        return app;
    }

    private static async Task<IResult> RenderAsync(
        RdlViewerOptions options,
        string? name,
        string? rdl,
        IDictionary? parameters,
        string format)
    {
        string rdlXml;
        string folder;

        if (!string.IsNullOrWhiteSpace(rdl))
        {
            rdlXml = rdl;
            folder = options.ReportsFolder;
        }
        else
        {
            var path = ResolveReportPath(options.ReportsFolder, name!);
            if (!File.Exists(path))
                return Results.NotFound($"Report '{name}' not found.");
            rdlXml = await File.ReadAllTextAsync(path);
            folder = options.ReportsFolder;
        }

        try
        {
            var rdlp = new RDLParser(rdlXml) { Folder = folder };
            var report = await rdlp.Parse();

            if (report.ErrorMaxSeverity >= 8)
            {
                var errs = string.Join("; ", report.ErrorItems?.Cast<object>().Select(o => o?.ToString()) ?? Array.Empty<string>());
                return Results.Problem(detail: $"Report has parse errors: {errs}", statusCode: 400);
            }

            var runtimeParams = await BuildRuntimeParameters(report, parameters);
            await report.RunGetData(runtimeParams);

            switch (format.ToLowerInvariant())
            {
                case "html":
                {
                    var sg = new MemoryStreamGen(string.Empty, null, "html");
                    await report.RunRender(sg, OutputPresentationType.HTML);
                    var html = sg.GetText() ?? string.Empty;
                    var css = report.CSS ?? string.Empty;
                    var js = report.JavaScript ?? string.Empty;
                    var full = $$"""
                        <!DOCTYPE html>
                        <html><head><meta charset="utf-8">
                        <style>body{margin:0;padding:16px}{{css}}</style>
                        </head><body>
                        {{html}}
                        <script>{{js}}</script>
                        </body></html>
                        """;
                    return Results.Content(full, "text/html");
                }
                case "pdf":
                    return await RenderBinary(report, OutputPresentationType.PDF, "application/pdf", (name ?? "report") + ".pdf");
                case "csv":
                    return await RenderBinary(report, OutputPresentationType.CSV, "text/csv", (name ?? "report") + ".csv");
                case "xlsx":
                    return await RenderBinary(report, OutputPresentationType.Excel2007, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", (name ?? "report") + ".xlsx");
                default:
                    return Results.BadRequest($"Unsupported format '{format}'. Supported: html, pdf, csv, xlsx.");
            }
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> RenderBinary(
        Report report, OutputPresentationType type, string contentType, string fileName)
    {
        using var sg = new MemoryStreamGen();
        await report.RunRender(sg, type);
        var stream = sg.GetStream();
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);
        return Results.File(bytes, contentType, fileName);
    }

    /// <summary>
    /// Builds the parameter dictionary <see cref="Report.RunGetData(System.Collections.IDictionary)"/>
    /// expects: one entry per <see cref="UserReportParameter"/>, using the caller-supplied value when
    /// present (case-insensitively) and falling back to the RDL's own default otherwise -- the same
    /// pattern <c>RdlViewer.RdlViewer.GetParameters()</c> uses.
    /// </summary>
    private static async Task<IDictionary> BuildRuntimeParameters(Report report, IDictionary? supplied)
    {
        var runtimeParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var suppliedCi = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (supplied != null)
            foreach (DictionaryEntry e in supplied)
                if (e.Key is string k)
                    suppliedCi[k] = UnwrapJsonElement(e.Value);

        foreach (UserReportParameter urp in report.UserReportParameters)
        {
            if (suppliedCi.TryGetValue(urp.Name, out var v))
                runtimeParams[urp.Name] = v;
            else
                runtimeParams[urp.Name] = await urp.GetValueAsync();
        }

        // Pass through any supplied values that aren't declared report parameters (e.g. dataset
        // query parameters not surfaced as prompted ReportParameters).
        foreach (var kv in suppliedCi)
            if (!runtimeParams.ContainsKey(kv.Key))
                runtimeParams[kv.Key] = kv.Value;

        return runtimeParams;
    }

    /// <summary>
    /// <see cref="JsonSerializer"/> deserializes <c>object?</c>-typed dictionary values as boxed
    /// <see cref="JsonElement"/> instances rather than native CLR primitives -- passing one of
    /// those straight into <see cref="Report.RunGetData(IDictionary)"/> makes the engine's own
    /// <c>Convert.ToInt32</c>/<c>ToDouble</c>/etc. parameter-type coercion throw, since
    /// <see cref="JsonElement"/> doesn't implement <see cref="IConvertible"/>. Unwrap to the
    /// natural CLR type first.
    /// </summary>
    private static object? UnwrapJsonElement(object? value)
    {
        if (value is not JsonElement je) return value;
        return je.ValueKind switch
        {
            JsonValueKind.String => je.GetString(),
            JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => je.ToString(),
        };
    }

    private static string ResolveReportPath(string reportsFolder, string name)
    {
        var safeName = Path.GetFileName(name);
        if (!safeName.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
            safeName += ".rdl";
        return Path.Combine(reportsFolder, safeName);
    }

    private static void EnsureEngineConfig()
    {
        try
        {
            RdlEngineConfig.RdlEngineConfigInit(new[]
            {
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "."
            });
        }
        catch { /* already initialised */ }
    }
}

internal sealed class RdlRenderRequest
{
    public string? Name { get; set; }
    public string? Rdl { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public string? Format { get; set; }
}
