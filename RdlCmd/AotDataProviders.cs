using System.Runtime.CompilerServices;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.Rdl
{
    /// <summary>
    /// Registers the database drivers this app references directly, so reports that name them in
    /// &lt;DataProvider&gt; work when the app is published with Native AOT. The config file finds these
    /// drivers by loading their assemblies from disk, which AOT cannot do.
    /// Compiled into RdlCmd, RdlNative and the AOT smoke test (a linked file); copy the same few lines
    /// into your own app for the drivers it uses.
    /// </summary>
    internal static class AotDataProviders
    {
        /// <summary>Registers the drivers when running under AOT. Without AOT the config file entries are used unchanged.</summary>
        internal static void RegisterIfAot()
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                Register();
        }

        internal static void Register()
        {
            RdlEngineConfig.RegisterDataProvider("Microsoft.Data.Sqlite",
                cs => new Microsoft.Data.Sqlite.SqliteConnection(cs),
                tableSelect: "SELECT name FROM sqlite_master WHERE type = 'table'");
            RdlEngineConfig.RegisterDataProvider("Microsoft.Data.SqlClient",
                cs => new Microsoft.Data.SqlClient.SqlConnection(cs));
            RdlEngineConfig.RegisterDataProvider("PostgreSQL",
                cs => new Npgsql.NpgsqlConnection(cs),
                tableSelect: "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME NOT LIKE 'pg_%' ORDER BY 1");
            RdlEngineConfig.RegisterDataProvider("MySQL.NET",
                cs => new MySql.Data.MySqlClient.MySqlConnection(cs),
                tableSelect: "show tables", replaceParameters: true);
        }
    }
}
