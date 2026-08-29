using Majorsilence.Reporting.Mcp;
using Majorsilence.Reporting.Rdl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return 0;
}

if (args.Length != 0)
{
    // stderr, not stdout: stdout carries the JSON-RPC stream for the stdio transport.
    Console.Error.WriteLine($"majorsilence-report-mcp: unexpected argument '{args[0]}'.");
    Console.Error.WriteLine("This server takes no options; it is launched by an MCP client over stdio. Run with --help.");
    return 2;
}

// The engine's format/provider registry has to be initialised once before any report is parsed
// or rendered -- RdlCmd and rdl-doctor both do this at startup.
RdlEngineConfig.RdlEngineConfigInit();

// The host builder gets no args: its command-line configuration provider rejects switch-style
// flags and there is nothing here to configure from them anyway.
var builder = Host.CreateApplicationBuilder();

// One stray write to stdout corrupts the protocol and the client drops the connection, so every
// log line goes to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ReportTools>();

await builder.Build().RunAsync();
return 0;

static void PrintUsage()
{
    Console.WriteLine(
        """
        majorsilence-report-mcp — an MCP server for working with RDL/RDLC reports through
        Majorsilence Reporting's engine.

        It speaks MCP over stdin/stdout, so it is launched by an MCP client rather than run
        directly. With Claude Code:

            claude mcp add majorsilence-report -- majorsilence-report-mcp

        Any client that launches MCP servers itself takes the same command in its own config:

            {
              "mcpServers": {
                "majorsilence-report": { "command": "majorsilence-report-mcp" }
              }
            }

        Tools:
          report_formats    List the output formats the engine can render to.
          report_inspect    Parse an .rdl/.rdlc (path or inline XML) and report its structure:
                            name, page size, parameters, datasets and fields, data sources,
                            body item summary, and any parse errors/warnings.
          report_lint       Run the rdl-doctor compatibility checks against a report.
          report_render     Render a report to a chosen format, with parameters and an optional
                            connection-string override; write to a file or return the bytes.
          report_scaffold   Build a minimal, valid RDL from a small JSON spec (title, optional
                            dataset + query, optional table, optional textboxes).

        Designer GUI automation is a separate concern: ReportDesigner is a Majorsilence.Forms app,
        so drive it with `Majorsilence.Forms.Mcp` pointed at its WebDriver endpoint and register
        both servers with your client.

        Options:
          -h, --help   Show this help.
        """);
}
