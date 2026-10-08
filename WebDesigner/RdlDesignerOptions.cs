namespace Majorsilence.Reporting.WebDesigner;

public sealed class RdlDesignerOptions
{
    /// <summary>URL prefix for the designer API endpoints (default: "rdl-designer").</summary>
    public string RoutePrefix { get; set; } = "rdl-designer";

    /// <summary>Folder on disk where RDL files are read and written by the load/save endpoints.</summary>
    public string ReportsFolder { get; set; } = "Reports";

    /// <summary>When false the save endpoint returns 403.</summary>
    public bool AllowSave { get; set; } = true;

    /// <summary>When false the load endpoint returns 403.</summary>
    public bool AllowLoad { get; set; } = true;

    /// <summary>When false the schema-discovery endpoint returns 403.</summary>
    public bool AllowSchema { get; set; } = true;

    /// <summary>
    /// Connections the schema endpoint may use, by name. The designer sends the data source's
    /// name and the server uses the provider and connection string configured here, so the
    /// request never supplies a connection string.
    /// </summary>
    public Dictionary<string, RdlSchemaConnection> SchemaConnections { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// When true the schema endpoint also accepts a provider and connection string from the
    /// request, except for providers that read files or make web requests. Leave this off unless
    /// the endpoint is only reachable by trusted users: the client picks which database the server connects to.
    /// </summary>
    public bool AllowClientConnectionStrings { get; set; } = false;
}
