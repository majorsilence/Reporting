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
            // MySql.Data (Oracle's connector, the config file's "MySQL.NET") cannot open a connection under
            // Native AOT: its static initializer uses System.Configuration, which is not AOT-compatible.
            // MySqlConnector is; reports that name either provider get it when running as an AOT binary.
            RdlEngineConfig.RegisterDataProvider("Oracle.ManagedDataAccess",
                cs => new Oracle.ManagedDataAccess.Client.OracleConnection(cs),
                tableSelect: "select OWNER || '.' || TABLE_NAME from ALL_TABLES WHERE TABLESPACE_NAME NOT IN ('SYSTEM', 'SYSAUX')");
            RdlEngineConfig.RegisterDataProvider("MySqlConnector",
                cs => new MySqlConnector.MySqlConnection(cs),
                tableSelect: "show tables", replaceParameters: true);
            RdlEngineConfig.RegisterDataProvider("MySQL.NET",
                cs => new MySqlConnector.MySqlConnection(cs),
                tableSelect: "show tables", replaceParameters: true);
        }
    }
}
