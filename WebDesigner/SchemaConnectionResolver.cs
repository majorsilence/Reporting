#nullable enable
using System;
using System.Collections.Generic;

namespace Majorsilence.Reporting.WebDesigner;

/// <summary>A data connection the server owner has approved for schema discovery.</summary>
public sealed class RdlSchemaConnection
{
    public string DataProvider { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Chooses the provider and connection string used by the schema endpoint. The request body is
/// untrusted, so by default it only names a connection that the server configured.
/// </summary>
internal static class SchemaConnectionResolver
{
    /// <summary>
    /// Providers that read server files or make server-side requests. A client may never choose
    /// these directly; they are only usable through a connection configured on the server.
    /// </summary>
    private static readonly HashSet<string> ClientBlockedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "filedirectory", "xml", "webservice", "weblog", "text", "json", "itunes",
    };

    /// <summary>Returns the connection to use, or null with <paramref name="error"/> set.</summary>
    public static RdlSchemaConnection? Resolve(
        IReadOnlyDictionary<string, RdlSchemaConnection> configured,
        bool allowClientConnectionStrings,
        string? connectionName,
        string? clientProvider,
        string? clientConnectionString,
        out string? error)
    {
        error = null;

        if (!string.IsNullOrWhiteSpace(connectionName)
            && configured.TryGetValue(connectionName.Trim(), out var named))
        {
            return named;
        }

        if (!allowClientConnectionStrings)
        {
            error = "Schema discovery only accepts connections configured on the server "
                  + "(RdlDesignerOptions.SchemaConnections).";
            return null;
        }

        if (string.IsNullOrWhiteSpace(clientProvider) || string.IsNullOrWhiteSpace(clientConnectionString))
        {
            error = "dataProvider and connectionString are required.";
            return null;
        }

        if (ClientBlockedProviders.Contains(clientProvider.Trim()))
        {
            error = $"Data provider '{clientProvider.Trim()}' must be configured on the server "
                  + "(RdlDesignerOptions.SchemaConnections).";
            return null;
        }

        return new RdlSchemaConnection
        {
            DataProvider = clientProvider.Trim(),
            ConnectionString = clientConnectionString.Trim(),
        };
    }
}
