using System.Text.Json;
using Majorsilence.Reporting.Mcp;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.Mcp.Tests;

[TestFixture]
public class ReportToolsTests
{
    private readonly ReportTools _tools = new();

    [OneTimeSetUp]
    public void Init() => RdlEngineConfig.RdlEngineConfigInit();

    [Test]
    public void Formats_lists_pdf_and_csv()
    {
        using var doc = JsonDocument.Parse(_tools.Formats_List());
        var formats = doc.RootElement.EnumerateArray().Select(e => e.GetProperty("format").GetString()).ToList();

        Assert.That(formats, Does.Contain("pdf"));
        Assert.That(formats, Does.Contain("csv"));
        Assert.That(formats, Does.Contain("xlsx"));
    }

    [Test]
    public async Task Scaffold_produces_a_report_that_parses()
    {
        var spec = JsonSerializer.Serialize(new
        {
            name = "Test",
            author = "unit",
            textboxes = new[] { new { text = "Hello", top = "0in", left = "0in", width = "6in", height = "0.3in", bold = true, fontSize = "14pt" } },
        });

        using var doc = JsonDocument.Parse(await _tools.Scaffold_Report(spec));

        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
        Assert.That(doc.RootElement.GetProperty("xml").GetString(), Does.Contain("Hello"));
    }

    [Test]
    public async Task Scaffold_with_a_table_renders_from_the_returned_xml()
    {
        var spec = JsonSerializer.Serialize(new
        {
            name = "Tabled",
            textboxes = new[] { new { text = "Title", top = "0in", left = "0in", width = "6in", height = "0.3in" } },
        });

        using var scaffold = JsonDocument.Parse(await _tools.Scaffold_Report(spec));
        var xml = scaffold.RootElement.GetProperty("xml").GetString()!;

        using var rendered = JsonDocument.Parse(
            await _tools.Render_Report(format: "pdf", path: null, xml: xml, parameters: null,
                connectionStringOverride: null, outputPath: null, dataSourceReferencePassword: null));

        Assert.That(rendered.RootElement.GetProperty("rendered").GetBoolean(), Is.True);
        Assert.That(rendered.RootElement.GetProperty("base64").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Inspect_reports_a_missing_report()
    {
        var result = await _tools.Inspect_Report(path: "does-not-exist.rdl");
        Assert.That(result, Does.Contain("No file"));
    }

    [Test]
    public async Task Inspect_and_lint_require_exactly_one_source()
    {
        Assert.That(await _tools.Inspect_Report(), Does.Contain("exactly one"));
        Assert.That(await _tools.Lint_Report(path: "a.rdl", xml: "<Report/>"), Does.Contain("exactly one"));
    }

    [Test]
    public async Task Lint_flags_a_tablix()
    {
        const string tablix = """
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
              <Body><ReportItems><Tablix Name="T1"></Tablix></ReportItems><Height>2in</Height></Body>
              <Width>7in</Width>
            </Report>
            """;

        using var doc = JsonDocument.Parse(await _tools.Lint_Report(xml: tablix));

        Assert.That(doc.RootElement.GetProperty("findingCount").GetInt32(), Is.GreaterThan(0));
        var ids = doc.RootElement.GetProperty("findings").EnumerateArray()
            .Select(f => f.GetProperty("id").GetString()).ToList();
        Assert.That(ids, Does.Contain("DOC001"));
    }
}
