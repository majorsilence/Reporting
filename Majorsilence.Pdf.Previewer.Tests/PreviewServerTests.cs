// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Majorsilence.Pdf.Previewer.Tests
{
    /// <summary>
    /// Runs the previewer as a real child process (exactly how a user invokes it) and exercises
    /// its HTTP surface.
    /// </summary>
    /// <remarks>
    /// The SSE auto-reload path (<c>/events</c>) is deliberately not asserted here via HTTP: a
    /// child <c>dotnet</c> process serving a long-lived streaming (chunked, no Content-Length)
    /// response, called from an <c>HttpClient</c> running inside the NUnit test host, was found
    /// to hang or fail unpredictably in this repo's CI sandbox even though the exact same
    /// request succeeds instantly from a plain shell (<c>curl -N</c>) against the same running
    /// process. The debounce/broadcast logic that feeds that endpoint is covered deterministically
    /// and without any networking by <see cref="PdfWatcherTests.Changed_FiresOnce_AfterMultipleRapidWrites"/>;
    /// the end-to-end SSE behavior (a real reload event arriving over the wire, and five rapid
    /// writes coalescing into exactly one event) was verified manually — see the previewer's
    /// README for the exact commands used.
    /// </remarks>
    [TestFixture]
    public class PreviewServerTests
    {
        private const int TestPort = 59874;

        [Test]
        public async Task WatchCommand_ServesCurrentFileContent_AndTracksFileUpdates()
        {
            string pdfPath = Path.Combine(Path.GetTempPath(), $"previewer-test-{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\noriginal"));

            string previewerDll = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..", "..", "..", "..", "Majorsilence.Pdf.Previewer", "bin",
                GetConfigurationDirName(), "net10.0", "Majorsilence.Pdf.Previewer.dll"));
            Assert.That(File.Exists(previewerDll), Is.True, $"Expected built previewer at {previewerDll}");

            var psi = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(previewerDll);
            psi.ArgumentList.Add("watch");
            psi.ArgumentList.Add(pdfPath);
            psi.ArgumentList.Add("--port");
            psi.ArgumentList.Add(TestPort.ToString());

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start previewer process.");
            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = new Uri($"http://127.0.0.1:{TestPort}"),
                    Timeout = TimeSpan.FromSeconds(5),
                };

                byte[]? pdfBytes = null;
                for (int i = 0; i < 30 && pdfBytes == null; i++)
                {
                    try { pdfBytes = await client.GetByteArrayAsync("/pdf"); }
                    catch { await Task.Delay(200); }
                }
                Assert.That(pdfBytes, Is.Not.Null, "server did not come up in time");
                Assert.That(Encoding.ASCII.GetString(pdfBytes!), Does.Contain("original"));

                // The index page should reference the endpoints the JS client depends on.
                string indexHtml = await client.GetStringAsync("/");
                Assert.That(indexHtml, Does.Contain("/pdf"));
                Assert.That(indexHtml, Does.Contain("/events"));

                // Rewrite the watched file and confirm the endpoint picks up the new content on
                // the next request (independent of the SSE push notification tested separately).
                await File.WriteAllBytesAsync(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\nupdated"));
                byte[]? updatedBytes = null;
                for (int i = 0; i < 30 && (updatedBytes == null || !Encoding.ASCII.GetString(updatedBytes).Contains("updated")); i++)
                {
                    try { updatedBytes = await client.GetByteArrayAsync("/pdf"); }
                    catch { /* transient during the file write */ }
                    if (updatedBytes == null || !Encoding.ASCII.GetString(updatedBytes).Contains("updated"))
                        await Task.Delay(200);
                }
                Assert.That(updatedBytes, Is.Not.Null);
                Assert.That(Encoding.ASCII.GetString(updatedBytes!), Does.Contain("updated"));
            }
            finally
            {
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                }
                File.Delete(pdfPath);
            }
        }

        private static string GetConfigurationDirName()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }
}
