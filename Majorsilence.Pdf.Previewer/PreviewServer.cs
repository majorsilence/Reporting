// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Majorsilence.Pdf.Previewer;

/// <summary>
/// Serves a browser page that displays a watched PDF file and auto-reloads it via
/// server-sent events whenever the file changes on disk.
/// </summary>
internal static class PreviewServer
{
    public static async Task<int> RunAsync(string pdfPath, int port, CancellationToken cancellationToken)
    {
        using var watcher = new PdfWatcher(pdfPath);
        var clients = new ConcurrentDictionary<Guid, Channel<string>>();

        watcher.Changed += () =>
        {
            foreach (var channel in clients.Values)
                channel.Writer.TryWrite("reload");
        };

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        var app = builder.Build();

        app.MapGet("/", () => Results.Content(IndexHtml, "text/html"));

        app.MapGet("/pdf", async (HttpContext ctx) =>
        {
            var bytes = await watcher.TryReadAsync();
            if (bytes == null)
            {
                ctx.Response.StatusCode = 404;
                return;
            }
            ctx.Response.ContentType = "application/pdf";
            ctx.Response.Headers.CacheControl = "no-store";
            await ctx.Response.Body.WriteAsync(bytes);
        });

        app.MapGet("/events", async (HttpContext ctx) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers["X-Accel-Buffering"] = "no";

            var id = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<string>();
            clients[id] = channel;
            try
            {
                await foreach (var message in channel.Reader.ReadAllAsync(ctx.RequestAborted))
                {
                    await ctx.Response.WriteAsync($"data: {message}\n\n", ctx.RequestAborted);
                    await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected; fall through to cleanup.
            }
            finally
            {
                clients.TryRemove(id, out _);
            }
        });

        Console.WriteLine($"Previewing {watcher.FilePath}");
        Console.WriteLine($"Open http://localhost:{port} in your browser. Press Ctrl+C to stop.");

        // Bind explicitly to 127.0.0.1 rather than the "localhost" hostname: "localhost"
        // resolves to both 127.0.0.1 and ::1, and a client that tries the IPv6 address first
        // when only the IPv4 side is actually listening can stall for several seconds per
        // request before falling back.
        app.Urls.Add($"http://127.0.0.1:{port}");
        // Pass the token through so Ctrl+C (Program.cs) and the test harness can both stop
        // Kestrel deterministically -- app.RunAsync(url) alone ignores cancellationToken.
        await app.RunAsync(cancellationToken);
        return 0;
    }

    private const string IndexHtml = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <title>Majorsilence.Pdf Previewer</title>
        <style>
          html, body { margin: 0; height: 100%; background: #525659; }
          embed { width: 100%; height: 100%; border: none; }
        </style>
        </head>
        <body>
        <embed id="pdf" src="/pdf?v=0" type="application/pdf">
        <script>
          let v = 0;
          const es = new EventSource('/events');
          es.onmessage = () => {
            v++;
            document.getElementById('pdf').src = '/pdf?v=' + v;
          };
        </script>
        </body>
        </html>
        """;
}
