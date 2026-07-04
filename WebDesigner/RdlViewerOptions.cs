namespace Majorsilence.Reporting.WebDesigner;

public sealed class RdlViewerOptions
{
    /// <summary>URL prefix for the viewer API endpoints (default: "rdl-viewer").</summary>
    public string RoutePrefix { get; set; } = "rdl-viewer";

    /// <summary>Folder on disk where named reports are read from (via the <c>name</c> parameter).</summary>
    public string ReportsFolder { get; set; } = "Reports";
}
