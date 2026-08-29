# Majorsilence.Reporting.Mcp

An [MCP](https://modelcontextprotocol.io) server that lets an AI assistant work with
[Majorsilence Reporting](https://github.com/majorsilence/Reporting) RDL/RDLC reports: **render**
them, **inspect** their structure, **lint** them for engine compatibility, and **scaffold** new
ones — all in-process against the reporting engine, no GUI required.

It is a `dotnet` global tool that speaks MCP over stdin/stdout.

```
assistant  ──MCP/stdio──▶  majorsilence-report-mcp  ──▶  Majorsilence.Reporting engine
```

## Install

```
dotnet tool install -g Majorsilence.Reporting.Mcp
```

## Point a client at it

Claude Code:

```
claude mcp add majorsilence-report -- majorsilence-report-mcp
```

Any client that launches MCP servers itself (Claude Desktop, editors, agent frameworks) takes the
same command in its own configuration format:

```json
{
  "mcpServers": {
    "majorsilence-report": { "command": "majorsilence-report-mcp" }
  }
}
```

The server takes no options. `--help` prints a summary.

## Tools

| Tool | Arguments | Does |
|---|---|---|
| `report_formats` | — | Lists the output formats `report_render` accepts. |
| `report_inspect` | `path` \| `xml`, `dataSourceReferencePassword?` | Parses a report and returns its structure as JSON — name, page size, margins, parameters (type, prompt, nullable, multi-value, defaults), datasets and fields, data sources and providers, a body-item summary, and any parse errors/warnings. |
| `report_lint` | `path` \| `xml` | Runs the same compatibility checks as the [`rdl-doctor`](../RdlDoctor) CLI (Tablix, Gauge/Indicator/Sparkline/Map, `Lookup*`, unknown elements, unresolved subreports/images, …) and returns the findings. |
| `report_render` | `format`, `path` \| `xml`, `parameters?`, `connectionStringOverride?`, `outputPath?`, `dataSourceReferencePassword?` | Renders to `pdf`, `html`, `mhtml`, `xml`, `csv`, `rtf`, `xlsx`, `xlsx_table`, `tif` or `tifbw`. Writes to `outputPath` and returns a summary, or returns the result inline (text formats as text, binary as base64, up to ~8 MB). |
| `report_scaffold` | `spec` (JSON), `outputPath?` | Builds a minimal, valid RDL from a small spec — title, optional data source + dataset + query, an optional table over the dataset's fields, optional free-standing textboxes — and validates it by parsing before returning. |

A report is given either as a file `path` (subreports, images and shared data-source references
resolve relative to its directory) or as inline `xml`.

### `report_scaffold` spec

```json
{
  "name": "Sales by region",
  "description": "optional",
  "author": "optional",
  "pageWidth": "8.5in",
  "pageHeight": "11in",
  "dataSource": { "name": "DS1", "provider": "Microsoft.Data.Sqlite", "connectionString": "Data Source=sales.db;" },
  "dataSet": {
    "name": "Data",
    "query": "SELECT Region, Total FROM Sales",
    "fields": [
      { "name": "Region", "type": "System.String" },
      { "name": "Total",  "type": "System.Decimal" }
    ]
  },
  "table": { "columns": [ { "field": "Region", "header": "Region" }, { "field": "Total", "header": "Total", "width": "1in" } ] },
  "textboxes": [ { "text": "Sales by region", "top": "0in", "left": "0in", "width": "6in", "height": "0.3in", "bold": true, "fontSize": "16pt" } ]
}
```

Every part except `name` is optional. With no `dataSet`/`table` you get a static report of just the
textboxes.

## Driving the visual designer

Designer-GUI automation is a **separate** concern and this server does not do it. `ReportDesigner`
is a [Majorsilence.Forms](https://github.com/majorsilence/Majorsilence.Forms) app, so drive its UI
with [`Majorsilence.Forms.Mcp`](https://www.nuget.org/packages/Majorsilence.Forms.Mcp) pointed at
the designer's `WebDriverServer` endpoint, and register both servers with your client:

```
claude mcp add majorsilence-report   -- majorsilence-report-mcp
claude mcp add majorsilence-designer  -- majorsilence-mcp --port 4444
```

## Notes

- No font installation is required. When a report asks for Arial / Times New Roman / Courier New /
  Calibri / Cambria and those aren't on the machine, the renderer substitutes the
  metric-compatible fonts bundled with `Majorsilence.Forms.Drawing.Common` (Liberation, Carlito,
  Caladea, plus Noto for wide Unicode coverage), so PDF/image output looks right on a bare Linux
  container. Installing the real fonts (`ttf-mscorefonts-installer`) only matters if you need those
  exact typefaces rather than close equivalents.
- `report_render` connects to the report's data source. Pass `connectionStringOverride` to point it
  somewhere else, or scaffold/inspect without ever connecting.

Apache-2.0. Part of [Majorsilence Reporting](https://github.com/majorsilence/Reporting).
