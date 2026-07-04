// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Majorsilence.Pdf.Previewer.Tests
{
    /// <summary>
    /// Deterministic, networking-free tests for the file-watching and debounce logic that backs
    /// the previewer's auto-reload behavior.
    /// </summary>
    [TestFixture]
    public class PdfWatcherTests
    {
        [Test]
        public async Task Changed_FiresOnce_AfterMultipleRapidWrites()
        {
            string path = Path.Combine(Path.GetTempPath(), $"watcher-test-{Guid.NewGuid():N}.pdf");
            await File.WriteAllTextAsync(path, "original");

            try
            {
                using var watcher = new PdfWatcher(path);
                int changedCount = 0;
                using var allDone = new SemaphoreSlim(0);
                watcher.Changed += () => { Interlocked.Increment(ref changedCount); allDone.Release(); };

                // Five rapid writes in quick succession, simulating a build tool that saves a
                // file more than once per rebuild.
                for (int i = 0; i < 5; i++)
                {
                    await File.WriteAllTextAsync(path, $"revision {i}");
                    await Task.Delay(20);
                }

                // Give the 250ms debounce window time to fire exactly once.
                await allDone.WaitAsync(TimeSpan.FromSeconds(2));
                await Task.Delay(500); // ensure no further (incorrect) events trickle in afterwards

                Assert.That(changedCount, Is.EqualTo(1),
                    "five rapid writes within the debounce window should coalesce into a single Changed event");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task TryReadAsync_ReturnsCurrentFileContent()
        {
            string path = Path.Combine(Path.GetTempPath(), $"watcher-test-{Guid.NewGuid():N}.pdf");
            await File.WriteAllTextAsync(path, "hello world");

            try
            {
                using var watcher = new PdfWatcher(path);
                byte[]? bytes = await watcher.TryReadAsync();

                Assert.That(bytes, Is.Not.Null);
                Assert.That(Encoding.UTF8.GetString(bytes!), Is.EqualTo("hello world"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public async Task TryReadAsync_ReturnsNull_WhenFileDoesNotExist()
        {
            string path = Path.Combine(Path.GetTempPath(), $"watcher-test-missing-{Guid.NewGuid():N}.pdf");
            using var watcher = new PdfWatcher(path);
            byte[]? bytes = await watcher.TryReadAsync(maxAttempts: 2, delayMs: 10);
            Assert.That(bytes, Is.Null);
        }
    }
}
