// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Net;
using System.Text.RegularExpressions;
using Majorsilence.Reporting.RdlAsp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace RdlAsp.Mvc.Tests;

/// <summary>
/// The ASP.NET Core report controllers, run in a real server. Issue #121: a long report shown in
/// the browser must keep all of its content, and exports must not lose any either.
/// </summary>
[TestFixture]
public class RdlReportTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private string _reports = null!;

    [OneTimeSetUp]
    public async Task StartServer()
    {
        // a copy of the reports so a test can edit one
        _reports = Path.Combine(Path.GetTempPath(), "rdlasp-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_reports);
        foreach (var f in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Reports")))
            File.Copy(f, Path.Combine(_reports, Path.GetFileName(f)));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddRdlAsp(o => o.ReportsFolder = _reports);

        _app = builder.Build();
        _app.MapControllers();
        await _app.StartAsync();

        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [OneTimeTearDown]
    public async Task StopServer()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        Directory.Delete(_reports, true);
    }

    [Test]
    public async Task Html_ListReport_HasEveryRowAndTheStyles()
    {
        var response = await _client.GetAsync("/msr/RdlReport/ShowFile/ListPagingTest/html");
        string html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), html);
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/html"));
        for (int i = 1; i <= 5; i++)
        {
            Assert.That(html, Does.Contain($"ROWEND{i}"), $"Row {i} is missing.");
            Assert.That(html, Does.Contain($"HEND{i}"), $"Html row {i} is missing.");
        }
        Assert.That(html, Does.Contain("<style"), "The report's styles must be sent with it.");
    }

    [Test]
    public async Task Pdf_ListReport_HasEveryPage()
    {
        var response = await _client.GetAsync("/msr/RdlReport/ShowFile/ListPagingTest/pdf");
        byte[] pdf = await response.Content.ReadAsByteArrayAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/pdf"));
        Assert.That(System.Text.Encoding.ASCII.GetString(pdf, 0, 5), Is.EqualTo("%PDF-"));
        // every one of the 5 rows needs more than a page
        int pages = Regex.Matches(System.Text.Encoding.Latin1.GetString(pdf), @"/Type\s*/Page[^s]").Count;
        Assert.That(pages, Is.GreaterThan(5));
    }

    [TestCase("csv", "text/csv")]
    [TestCase("xml", "text/xml")]
    [TestCase("rtf", "application/rtf")]
    public async Task TextExports_AreSent(string type, string contentType)
    {
        var response = await _client.GetAsync($"/msr/RdlReport/ShowFile/Parameters/{type}?Who=somebody");
        string text = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), text);
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo(contentType));
        Assert.That(text, Does.Contain("somebody"));
    }

    [Test]
    public async Task Xlsx_IsSentAsAWorkbook()
    {
        var response = await _client.GetAsync("/msr/RdlReport/ShowFile/Parameters/xlsx");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType, Does.Contain("spreadsheetml"));
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 2), Is.EqualTo("PK"), "An xlsx file is a zip.");
    }

    [Test]
    public async Task QueryString_ValuesReachTheReport()
    {
        string html = await _client.GetStringAsync("/msr/RdlReport/ShowFile/Parameters/html?Min=42&Who=Ann");

        Assert.That(html, Does.Contain("MIN=42 WHO=Ann"));
    }

    [Test]
    public async Task ParameterForm_ShowsPromptsAndTheChosenValues_AndEncodesThem()
    {
        string html = await _client.GetStringAsync(
            "/msr/RdlReport/ShowFile/Parameters/html?Who=" + Uri.EscapeDataString("\"><script>alert(1)</script>"));

        Assert.That(html, Does.Contain("Minimum"));
        Assert.That(html, Does.Contain("name=\"Who\""));
        Assert.That(html, Does.Not.Contain("<script>alert(1)"), "A parameter value must not become markup.");
        Assert.That(html, Does.Contain("/msr/RdlReport/ShowFile/Parameters/pdf?Who="), "Export links keep the chosen values.");
    }

    [Test]
    public async Task Html_Images_AreInTheHtml()
    {
        string html = await _client.GetStringAsync("/msr/RdlReport/ShowFile/Parameters/html");

        Assert.That(html, Does.Contain("data:image/png;base64,"));
    }

    [TestCase("/msr/RdlReport/ShowFile/NoSuchReport/html", HttpStatusCode.NotFound)]
    [TestCase("/msr/RdlReport/ShowFile/..%2Fsecret/html", HttpStatusCode.NotFound)]
    [TestCase("/msr/RdlReport/ShowFile/Parameters/doc", HttpStatusCode.BadRequest)]
    public async Task BadRequests_GetAnErrorStatus(string url, HttpStatusCode expected)
    {
        var response = await _client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public async Task ReportThatFailsToRun_IsAServerError_WithTheMessageEncoded()
    {
        var response = await _client.GetAsync("/msr/RdlReport/ShowFile/Parameters/html?Min=" + Uri.EscapeDataString("<b>x</b>"));
        string html = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError), html);
        Assert.That(html, Does.Not.Contain("<b>x</b>"));
    }

    [Test]
    public async Task EditedReport_IsReadAgain()
    {
        string path = Path.Combine(_reports, "Edited.rdl");
        string source = File.ReadAllText(Path.Combine(_reports, "Parameters.rdl"));
        File.WriteAllText(path, source.Replace("MIN=", "FIRST="));
        Assert.That(await _client.GetStringAsync("/msr/RdlReport/ShowFile/Edited/html"), Does.Contain("FIRST=1"));

        File.WriteAllText(path, source.Replace("MIN=", "SECOND="));
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

        Assert.That(await _client.GetStringAsync("/msr/RdlReport/ShowFile/Edited/html"), Does.Contain("SECOND=1"));
    }

    [Test]
    public async Task Statistics_CountCacheHits()
    {
        await _client.GetStringAsync("/msr/RdlReport/ShowFile/Parameters/html");
        await _client.GetStringAsync("/msr/RdlReport/ShowFile/Parameters/html");
        string stats = await _client.GetStringAsync("/msr/RdlReport/ShowFile/statistics");

        Assert.That(stats, Does.Match(@"[1-9]\d* cache hits"));
    }

    [Test]
    public async Task ReportList_LinksEveryReport()
    {
        string html = await _client.GetStringAsync("/msr/RdlList");

        Assert.That(html, Does.Contain("/msr/RdlReport/ShowFile/Parameters/html"));
        Assert.That(html, Does.Contain("ListPagingTest"));
    }
}
