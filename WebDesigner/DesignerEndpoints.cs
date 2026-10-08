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

public static class DesignerEndpoints
{
    /// <summary>
    /// Maps the three RDL designer API endpoints:
    /// <list type="bullet">
    ///   <item><c>POST /{prefix}/preview</c> — body: RDL XML → returns a full HTML preview page</item>
    ///   <item><c>GET  /{prefix}/load?name=x</c> — returns the named RDL file as XML</item>
    ///   <item><c>POST /{prefix}/save</c> — body: <c>{"name":"x","rdl":"..."}</c> → saves file</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapRdlDesigner(
        this IEndpointRouteBuilder app,
        RdlDesignerOptions? options = null)
    {
        options ??= app.ServiceProvider?.GetService<RdlDesignerOptions>()
                   ?? new RdlDesignerOptions();

        EnsureEngineConfig();

        var prefix = options.RoutePrefix.Trim('/');

        // ── Preview ───────────────────────────────────────────────────────────
        app.MapPost($"/{prefix}/preview", async (HttpRequest req) =>
        {
            string rdlXml;
            using (var sr = new StreamReader(req.Body, Encoding.UTF8))
                rdlXml = await sr.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rdlXml))
                return Results.BadRequest("Empty RDL body.");

            try
            {
                var rdlp = new RDLParser(rdlXml) { Folder = options.ReportsFolder };
                var report = await rdlp.Parse();

                if (report.ErrorMaxSeverity >= 8)
                {
                    var errs = string.Join("<br>", report.ErrorItems.Cast<string>().Select(System.Net.WebUtility.HtmlEncode));
                    return Results.Content(ErrorPage("Parse errors", errs), "text/html");
                }

                await report.RunGetData(new ListDictionary());
                var sg = new MemoryStreamGen(string.Empty, null, "html");
                await report.RunRender(sg, OutputPresentationType.HTML);

                var html  = sg.GetText() ?? string.Empty;
                var css   = report.CSS   ?? string.Empty;
                var js    = report.JavaScript ?? string.Empty;

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
            catch (Exception ex)
            {
                return Results.Content(ErrorPage("Preview error", System.Net.WebUtility.HtmlEncode(ex.Message)), "text/html");
            }
        });

        // ── Load ──────────────────────────────────────────────────────────────
        app.MapGet($"/{prefix}/load", async (string? name) =>
        {
            if (!options.AllowLoad)
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("name parameter is required.");

            var safeName = Path.GetFileName(name);
            if (!safeName.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
                safeName += ".rdl";

            var path = Path.Combine(options.ReportsFolder, safeName);
            if (!File.Exists(path))
                return Results.NotFound($"Report '{safeName}' not found.");

            var xml = await File.ReadAllTextAsync(path);
            return Results.Content(xml, "application/xml");
        });

        // ── Save ──────────────────────────────────────────────────────────────
        app.MapPost($"/{prefix}/save", async (HttpRequest req) =>
        {
            if (!options.AllowSave)
                return Results.Forbid();

            RdlSaveRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<RdlSaveRequest>(
                    req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return Results.BadRequest("Expected JSON body: {\"name\":\"...\",\"rdl\":\"...\"}");
            }

            if (body is null || string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Rdl))
                return Results.BadRequest("name and rdl are required.");

            var safeName = Path.GetFileName(body.Name);
            if (!safeName.EndsWith(".rdl", StringComparison.OrdinalIgnoreCase))
                safeName += ".rdl";

            Directory.CreateDirectory(options.ReportsFolder);
            var path = Path.Combine(options.ReportsFolder, safeName);
            await File.WriteAllTextAsync(path, body.Rdl, Encoding.UTF8);

            return Results.Ok(new { saved = safeName });
        });

        // ── Schema ────────────────────────────────────────────────────────────
        app.MapPost($"/{prefix}/schema", async (HttpRequest req) =>
        {
            if (!options.AllowSchema)
                return Results.Forbid();

            RdlSchemaRequest? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<RdlSchemaRequest>(
                    req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return Results.BadRequest(
                    "Expected JSON body: {\"connectionName\":\"...\",\"commandText\":\"...\"}");
            }

            if (body is null)
                return Results.BadRequest("A JSON body is required.");

            var target = SchemaConnectionResolver.Resolve(
                options.SchemaConnections,
                options.AllowClientConnectionStrings,
                body.ConnectionName,
                body.DataProvider,
                body.ConnectionString,
                out var resolveError);
            if (target is null)
                return Results.BadRequest(resolveError);

            try
            {
                var conn = RdlEngineConfig.GetConnection(target.DataProvider, target.ConnectionString);

                if (conn is null)
                    return Results.BadRequest($"Unknown data provider '{target.DataProvider}'.");

                conn.Open();
                using (conn as IDisposable) // dispose if provider implements it
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = body.CommandText ?? string.Empty;

                    using var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SchemaOnly);

                    // Advance to first row so GetFieldType returns actual CLR types.
                    bool hasRows = reader.Read();

                    var fields = new List<object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        if (name.StartsWith("__")) continue; // skip internal tracking columns
                        var clrType = hasRows ? reader.GetFieldType(i) : null;
                        fields.Add(new { name, typeName = MapClrType(clrType) });
                    }

                    return Results.Ok(new { fields });
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 400);
            }
        });

        return app;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string MapClrType(Type? t) => t?.FullName switch
    {
        "System.Int32"    => "System.Int32",
        "System.Int64"    => "System.Int64",
        "System.Double"   => "System.Double",
        "System.Decimal"  => "System.Decimal",
        "System.Boolean"  => "System.Boolean",
        "System.DateTime" => "System.DateTime",
        _                 => "System.String",
    };

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

    private static string ErrorPage(string title, string body) => $$"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8">
        <style>body{font-family:system-ui,sans-serif;padding:16px;color:#c00}</style>
        </head><body><h3>{{title}}</h3><p>{{body}}</p></body></html>
        """;
}

internal sealed class RdlSaveRequest
{
    public string Name { get; set; } = string.Empty;
    public string Rdl  { get; set; } = string.Empty;
}

internal sealed class RdlSchemaRequest
{
    /// <summary>Name of a connection in RdlDesignerOptions.SchemaConnections (the data source name).</summary>
    public string? ConnectionName   { get; set; }
    public string? DataProvider     { get; set; }
    public string? ConnectionString { get; set; }
    public string? CommandText      { get; set; }
}
