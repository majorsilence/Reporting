using System;
using Microsoft.Extensions.DependencyInjection;

namespace Majorsilence.Reporting.RdlAsp
{
    public static class RdlAspServiceExtensions
    {
        /// <summary>
        /// Adds what the report controllers need: their <see cref="Settings"/>, a memory cache and MVC controllers.
        /// Reports are then shown at <c>/msr/RdlReport/ShowFile/{report}/{html|pdf|xml|csv|xlsx|rtf}</c>
        /// and listed at <c>/msr/RdlList</c>.
        /// </summary>
        public static IMvcBuilder AddRdlAsp(this IServiceCollection services, Action<Settings> configure = null)
        {
            var settings = new Settings();
            configure?.Invoke(settings);
            services.AddSingleton(settings);
            services.AddMemoryCache();
            return services.AddControllersWithViews().AddApplicationPart(typeof(RdlReport).Assembly);
        }
    }
}
