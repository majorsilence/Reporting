// SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

namespace Majorsilence.Pdf.Previewer;

/// <summary>
/// Watches a single PDF file for changes and raises <see cref="Changed"/> once per burst of
/// writes (debounced), so a single `dotnet build` that touches the file several times in quick
/// succession only triggers one browser reload.
/// </summary>
internal sealed class PdfWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly System.Threading.Timer _debounceTimer;
    private readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(250);

    public string FilePath { get; }

    public event Action? Changed;

    public PdfWatcher(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(FilePath) ?? ".";
        var fileName = Path.GetFileName(FilePath);

        _debounceTimer = new System.Threading.Timer(_ => Changed?.Invoke(), null, Timeout.Infinite, Timeout.Infinite);

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        };
        _watcher.Changed += (_, _) => _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        _watcher.Created += (_, _) => _debounceTimer.Change(_debounce, Timeout.InfiniteTimeSpan);
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Reads the current file bytes, retrying briefly if the file is transiently locked by a
    /// build process still writing it (common right after a FileSystemWatcher event fires).
    /// </summary>
    public async Task<byte[]?> TryReadAsync(int maxAttempts = 10, int delayMs = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch (IOException)
            {
                await Task.Delay(delayMs);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}
