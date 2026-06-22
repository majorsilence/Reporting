using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;

namespace Majorsilence.Reporting.RdlNative;

internal sealed class ReportContext
{
    internal string RdlPath { get; }
    internal string? ConnectionString { get; }
    internal Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal ReportContext(string rdlPath, string? connectionString)
    {
        RdlPath = rdlPath;
        ConnectionString = connectionString;
    }
}

/// <summary>
/// Exported C API for the native AOT shared library (see rdlnative.h for the C declarations).
/// All entry points are callable from Python/ctypes, PHP/FFI, Ruby/Fiddle, or any C-compatible host.
/// </summary>
public static class RdlNativeApi
{
    private static readonly nint s_errorBuf;
    private static readonly object s_errorLock = new();
    private static int s_initDone;

    static unsafe RdlNativeApi()
    {
        s_errorBuf = (nint)NativeMemory.AllocZeroed(2048);
    }

    private static unsafe void SetError(Exception ex)
    {
        // Walk the inner exception chain for the most useful message.
        var sb = new System.Text.StringBuilder();
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (sb.Length > 0) sb.Append(" -> ");
            sb.Append(e.GetType().Name).Append(": ").Append(e.Message);
        }
        SetError(sb.ToString());
    }

    private static unsafe void SetError(string msg)
    {
        byte[] encoded = Encoding.UTF8.GetBytes(msg.Length > 2000 ? msg[..2000] : msg);
        lock (s_errorLock)
        {
            var span = new Span<byte>((byte*)s_errorBuf, 2048);
            span.Clear();
            encoded.AsSpan().CopyTo(span);
        }
    }

    private static ReportContext? GetCtx(nint handle)
    {
        if (handle == 0) return null;
        var gh = GCHandle.FromIntPtr(handle);
        return gh.IsAllocated ? gh.Target as ReportContext : null;
    }

    // Lazy one-time engine init so callers don't strictly have to call rdl_init() first.
    private static void EnsureInit()
    {
        if (Interlocked.CompareExchange(ref s_initDone, 1, 0) == 0)
            RdlEngineConfig.RdlEngineConfigInit();
    }

    // ─── P/Invoke library resolver ───────────────────────────────────────────
    // When rdlnative.so is loaded by a foreign host (Python/PHP/Ruby), .NET's
    // P/Invoke resolver searches for dependencies (e.g. libSkiaSharp.so) relative
    // to the host process rather than rdlnative.so's own directory. The caller
    // sets RDLNATIVE_LIB_DIR to the directory containing rdlnative.so before
    // calling rdl_init(), and we register a resolver that looks there first.

    private static void SetupLibraryResolver(string libDir)
    {
        DllImportResolver resolver = (name, _, _) => ResolveFromDir(name, libDir);
        RegisterSafe(resolver, typeof(RdlNativeApi).Assembly);
        RegisterSafe(resolver, typeof(RdlEngineConfig).Assembly);
#if DRAWINGCOMPAT
        RegisterSafe(resolver, typeof(SkiaSharp.SKBitmap).Assembly);
#endif
        RegisterSafe(resolver, typeof(SQLitePCL.raw).Assembly);
        RegisterSafe(resolver, typeof(MySql.Data.MySqlClient.MySqlConnection).Assembly);
        RegisterSafe(resolver, typeof(Npgsql.NpgsqlConnection).Assembly);
    }

    private static void RegisterSafe(DllImportResolver resolver, Assembly asm)
    {
        try { NativeLibrary.SetDllImportResolver(asm, resolver); }
        catch { }
    }

    private static nint ResolveFromDir(string name, string dir)
    {
        string[] candidates = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? new[] { $"lib{name}.dylib", $"{name}.dylib", name }
            : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { $"{name}.dll", name }
            : new[] { $"lib{name}.so", $"{name}.so", name };

        foreach (string c in candidates)
        {
            if (NativeLibrary.TryLoad(Path.Combine(dir, c), out nint h))
                return h;
        }
        NativeLibrary.TryLoad(name, out nint fallback);
        return fallback;
    }

    // ─── Exported C API ─────────────────────────────────────────────────────

    /// <summary>Initialize the reporting engine. Call once at startup before any other function.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_init")]
    public static int Init()
    {
        try
        {
            // If the caller set RDLNATIVE_LIB_DIR, register a P/Invoke resolver so
            // that sibling libraries (libSkiaSharp.so etc.) are found in the same
            // directory as rdlnative.so rather than the host process directory.
            string? libDir = Environment.GetEnvironmentVariable("RDLNATIVE_LIB_DIR");
            if (libDir is not null)
                SetupLibraryResolver(libDir);
            EnsureInit();
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Open a report from an RDL file. Returns opaque handle, or 0 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_report_open")]
    public static unsafe nint ReportOpen(byte* rdlPathUtf8, byte* connectionStringUtf8)
    {
        try
        {
            EnsureInit();
            string path = Marshal.PtrToStringUTF8((nint)rdlPathUtf8) ?? "";
            string? cs  = connectionStringUtf8 != null
                ? Marshal.PtrToStringUTF8((nint)connectionStringUtf8)
                : null;
            return GCHandle.ToIntPtr(GCHandle.Alloc(new ReportContext(path, cs)));
        }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Set a named parameter on the report. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_report_set_param")]
    public static unsafe int ReportSetParam(nint handle, byte* nameUtf8, byte* valueUtf8)
    {
        try
        {
            var ctx = GetCtx(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Parameters[Marshal.PtrToStringUTF8((nint)nameUtf8) ?? ""] =
                Marshal.PtrToStringUTF8((nint)valueUtf8) ?? "";
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Render the report to a file. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_report_render_file")]
    public static unsafe int ReportRenderFile(nint handle, byte* outputPathUtf8, byte* formatUtf8)
    {
        try
        {
            var ctx = GetCtx(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            string outPath = Marshal.PtrToStringUTF8((nint)outputPathUtf8) ?? "";
            string fmt     = Marshal.PtrToStringUTF8((nint)formatUtf8)     ?? "pdf";
            RenderToFile(ctx, outPath, fmt).GetAwaiter().GetResult();
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Render the report to a caller-owned native buffer.
    /// On success, *outData points to memory that must be freed with rdl_free().
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_report_render_buffer")]
    public static unsafe int ReportRenderBuffer(
        nint handle, byte* formatUtf8, byte** outData, int* outSize)
    {
        *outData = null;
        *outSize = 0;
        try
        {
            var ctx = GetCtx(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            string fmt = Marshal.PtrToStringUTF8((nint)formatUtf8) ?? "pdf";
            byte[] data = RenderToBytes(ctx, fmt).GetAwaiter().GetResult();
            byte* ptr = (byte*)NativeMemory.Alloc((nuint)data.Length);
            data.AsSpan().CopyTo(new Span<byte>(ptr, data.Length));
            *outData = ptr;
            *outSize = data.Length;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Free a buffer returned by rdl_report_render_buffer.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_free")]
    public static unsafe void Free(void* ptr) { if (ptr != null) NativeMemory.Free(ptr); }

    /// <summary>Close a report handle and release associated resources.</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_report_close")]
    public static void ReportClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    /// <summary>Return a UTF-8 string describing the last error (valid until the next error).</summary>
    [UnmanagedCallersOnly(EntryPoint = "rdl_last_error")]
    public static unsafe byte* LastError() => (byte*)s_errorBuf;

    // ─── Private helpers ─────────────────────────────────────────────────────

    private static OutputPresentationType ParseFormat(string fmt) =>
        fmt.ToLowerInvariant() switch
        {
            "html" or "htm"   => OutputPresentationType.HTML,
            "xml"             => OutputPresentationType.XML,
            "csv"             => OutputPresentationType.CSV,
            "xlsx"            => OutputPresentationType.Excel2007,
            "xlsx_table"      => OutputPresentationType.ExcelTableOnly,
            "rtf"             => OutputPresentationType.RTF,
            "tif" or "tiff"   => OutputPresentationType.TIF,
            "tifb"            => OutputPresentationType.TIFBW,
            "mht" or "mhtml"  => OutputPresentationType.MHTML,
            _                 => OutputPresentationType.PDF,
        };

    private static async Task<Report> BuildReport(ReportContext ctx)
    {
        string xml    = await File.ReadAllTextAsync(ctx.RdlPath);
        string folder = Path.GetDirectoryName(ctx.RdlPath) ?? Directory.GetCurrentDirectory();
        var parser = new RDLParser(xml) { Folder = folder };
        if (ctx.ConnectionString is not null)
            parser.OverwriteConnectionString = ctx.ConnectionString;
        var report = await parser.Parse();
        if (report is null)
            throw new InvalidOperationException(
                $"Failed to parse report '{ctx.RdlPath}'. Check the RDL for errors.");
        return report;
    }

    private static IDictionary? BuildParams(ReportContext ctx)
    {
        if (ctx.Parameters.Count == 0) return null;
        var ld = new ListDictionary();
        foreach (var kv in ctx.Parameters)
            ld[kv.Key] = kv.Value;
        return ld;
    }

    private static async Task<byte[]> RenderToBytes(ReportContext ctx, string fmt)
    {
        using var report = await BuildReport(ctx);
        await report.RunGetData(BuildParams(ctx));
        using var sg = new MemoryStreamGen();
        await report.RunRender(sg, ParseFormat(fmt));
        sg.CloseMainStream();
        return ((MemoryStream)sg.GetStream()).ToArray();
    }

    private static async Task RenderToFile(ReportContext ctx, string outPath, string fmt)
    {
        using var report = await BuildReport(ctx);
        await report.RunGetData(BuildParams(ctx));
        using var sg = new OneFileStreamGen(outPath, true);
        await report.RunRender(sg, ParseFormat(fmt));
        sg.CloseMainStream();
    }
}
