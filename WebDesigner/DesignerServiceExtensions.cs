using Microsoft.Extensions.DependencyInjection;

namespace Majorsilence.Reporting.WebDesigner;

public static class DesignerServiceExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="RdlDesignerOptions"/> so it is available to the
    /// endpoint handlers via DI.  Call before <see cref="DesignerEndpoints.MapRdlDesigner"/>.
    /// </summary>
    public static IServiceCollection AddRdlDesigner(
        this IServiceCollection services,
        Action<RdlDesignerOptions>? configure = null)
    {
        var opts = new RdlDesignerOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        return services;
    }

    /// <summary>
    /// Registers a singleton <see cref="RdlViewerOptions"/> so it is available to the
    /// endpoint handlers via DI.  Call before <see cref="ViewerEndpoints.MapRdlViewer"/>.
    /// </summary>
    public static IServiceCollection AddRdlViewer(
        this IServiceCollection services,
        Action<RdlViewerOptions>? configure = null)
    {
        var opts = new RdlViewerOptions();
        configure?.Invoke(opts);
        services.AddSingleton(opts);
        return services;
    }
}
