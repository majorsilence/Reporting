// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Net.Http.Json;
using System.Text.Json;
using Majorsilence.Reporting.WebDesigner;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace WebDesigner.Tests;

[TestFixture]
public class ViewerEndpointsTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task StartServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddRdlViewer(o =>
        {
            o.ReportsFolder = Path.Combine(AppContext.BaseDirectory, "Reports");
        });

        _app = builder.Build();
        _app.MapRdlViewer();
        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;
        var baseAddress = addresses.First();

        _client = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    [OneTimeTearDown]
    public async Task StopServer()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task Parameters_ReturnsDeclaredReportParameterWithDefault()
    {
        var response = await _client.GetAsync("/rdl-viewer/parameters/SimpleSalesReport");
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var parameters = json.GetProperty("parameters");
        Assert.That(parameters.GetArrayLength(), Is.EqualTo(1));

        var p = parameters[0];
        Assert.That(p.GetProperty("name").GetString(), Is.EqualTo("MinAmount"));
        Assert.That(p.GetProperty("typeName").GetString(), Is.EqualTo("Int32"));
    }

    [Test]
    public async Task Render_Html_WithDefaultParameters_ReturnsAllRows()
    {
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "SimpleSalesReport",
            format = "html",
        });
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Northwind Traders"));
        Assert.That(html, Does.Contain("Contoso Ltd"));
        Assert.That(html, Does.Contain("Fabrikam Inc"));
    }

    [Test]
    public async Task Render_Html_WithNumericParameter_FiltersRows()
    {
        // Regression test: numeric parameter values arrive from System.Text.Json as boxed
        // JsonElement, not a native CLR int -- passing one straight into RunGetData threw
        // "Unable to convert '1000' to Int32" until ViewerEndpoints started unwrapping it.
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "SimpleSalesReport",
            format = "html",
            parameters = new { MinAmount = 1000 },
        });
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Northwind Traders")); // 2532, kept
        Assert.That(html, Does.Contain("Fabrikam Inc"));      // 1875, kept
        Assert.That(html, Does.Not.Contain("Contoso Ltd"));   // 480, filtered out
        Assert.That(html, Does.Not.Contain("Tailwind Traders")); // 120, filtered out
        Assert.That(html, Does.Not.Contain("Adventure Works"));  // 975, filtered out
    }

    [Test]
    public async Task Render_Pdf_ReturnsNonEmptyPdfBytes()
    {
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "SimpleSalesReport",
            format = "pdf",
        });
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.That(bytes.Length, Is.GreaterThan(0));
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("%PDF"));
    }

    [Test]
    public async Task Render_Csv_ReturnsCsvContent()
    {
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "SimpleSalesReport",
            format = "csv",
        });
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.That(csv, Does.Contain("Northwind Traders"));
    }

    [Test]
    public async Task Render_ConvenienceGetRoute_HonorsQueryStringParameters()
    {
        var response = await _client.GetAsync("/rdl-viewer/render/SimpleSalesReport.pdf?MinAmount=1000");
        Assert.That(response.IsSuccessStatusCode, Is.True);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.That(bytes.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task Render_UnknownReport_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "NoSuchReport",
            format = "html",
        });
        Assert.That((int)response.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task Render_UnsupportedFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/rdl-viewer/render", new
        {
            name = "SimpleSalesReport",
            format = "docx",
        });
        Assert.That((int)response.StatusCode, Is.EqualTo(400));
    }
}
