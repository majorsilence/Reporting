// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

using System.Diagnostics;
using Majorsilence.Pdf.Previewer;

const int DefaultPort = 5990;

void PrintUsage()
{
    Console.WriteLine("""
        mspdf-preview — live PDF preview while you code.

        Usage:
          mspdf-preview watch <file.pdf> [--port <port>]
              Watch an existing PDF file and reload the browser whenever it changes on disk.

          mspdf-preview run <project-dir> --pdf <relative-output-path> [--port <port>] [-- <extra dotnet watch run args>]
              Run `dotnet watch run` in <project-dir> (so your program rebuilds and re-renders
              on every source change) while watching --pdf for changes and reloading the browser.

        Examples:
          mspdf-preview watch ./bin/Debug/net10.0/invoice.pdf
          mspdf-preview run . --pdf bin/Debug/net10.0/invoice.pdf
        """);
}

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

int ParsePort(string[] a, int defaultPort)
{
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == "--port" && int.TryParse(a[i + 1], out var p)) return p;
    return defaultPort;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

switch (args[0])
{
    case "watch":
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }
        int port = ParsePort(args, DefaultPort);
        return await PreviewServer.RunAsync(args[1], port, cts.Token);
    }

    case "run":
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return 1;
        }
        string projectDir = args[1];
        string? pdfRelativePath = null;
        var extraArgs = new List<string>();
        int port = DefaultPort;

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--pdf" && i + 1 < args.Length) { pdfRelativePath = args[++i]; }
            else if (args[i] == "--port" && i + 1 < args.Length) { port = int.Parse(args[++i]); }
            else if (args[i] == "--") { extraArgs.AddRange(args[(i + 1)..]); break; }
        }

        if (pdfRelativePath == null)
        {
            Console.Error.WriteLine("error: --pdf <relative-output-path> is required with 'run'.");
            PrintUsage();
            return 1;
        }

        string pdfFullPath = Path.IsPathRooted(pdfRelativePath)
            ? pdfRelativePath
            : Path.Combine(Path.GetFullPath(projectDir), pdfRelativePath);

        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetFullPath(projectDir),
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("watch");
        psi.ArgumentList.Add("run");
        foreach (var extra in extraArgs) psi.ArgumentList.Add(extra);

        Console.WriteLine($"Starting: dotnet watch run (in {psi.WorkingDirectory})");
        using var child = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start 'dotnet watch run'.");

        try
        {
            return await PreviewServer.RunAsync(pdfFullPath, port, cts.Token);
        }
        finally
        {
            if (!child.HasExited)
            {
                try { child.Kill(entireProcessTree: true); } catch { /* best effort */ }
            }
        }
    }

    default:
        PrintUsage();
        return 1;
}
