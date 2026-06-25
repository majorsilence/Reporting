using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;

namespace Majorsilence.Pdf.Native;

// ─── Context objects held behind opaque C handles ─────────────────────────────

internal sealed class DocContext
{
    internal PdfDocument Doc { get; } = PdfDocument.Create();
}

internal sealed class CanvasContext
{
    internal PdfCanvas Canvas { get; }
    internal CanvasContext(PdfCanvas canvas) { Canvas = canvas; }
}

internal sealed class StyleContext
{
    internal string  FontFamily   { get; set; } = "Helvetica";
    internal string? FontFilePath { get; set; }
    internal float   FontSize     { get; set; } = 12f;
    internal PdfColor Color       { get; set; } = PdfColor.Black;
    internal bool    IsBold       { get; set; }
    internal bool    IsItalic     { get; set; }
    internal TextAlignment  Alignment  { get; set; } = TextAlignment.Left;
    internal TextDecoration Decoration { get; set; } = TextDecoration.None;

    internal TextStyle ToTextStyle()
    {
        var s = TextStyle.Default
            .WithSize(FontSize)
            .WithColor(Color)
            .WithBold(IsBold)
            .WithItalic(IsItalic)
            .WithAlignment(Alignment);
        s = FontFilePath != null ? s.WithFontFile(FontFilePath) : s.WithFamily(FontFamily);
        return Decoration switch
        {
            TextDecoration.Underline     => s.WithUnderline(),
            TextDecoration.Strikethrough => s.WithStrikethrough(),
            TextDecoration.Overline      => s.WithOverline(),
            _                            => s,
        };
    }
}

internal sealed class TableContext
{
    internal PdfTable    Table       { get; set; }
    internal List<string> StagedCells { get; } = new();
    internal TableContext(PdfTable table) { Table = table; }
}

internal sealed class MergeContext
{
    internal PdfMerger Merger { get; } = new PdfMerger();
}

internal sealed class ShapeStyleContext
{
    internal PdfColor? FillColor    { get; set; }
    internal float     FillOpacity  { get; set; } = 1f;
    internal PdfColor? StrokeColor  { get; set; }
    internal float     StrokeWidth  { get; set; } = 1f;
    internal float     StrokeOpacity { get; set; } = 1f;

    internal ShapeStyle ToShapeStyle()
    {
        ShapeStyle s = ShapeStyle.Empty;
        if (FillColor.HasValue)
            s = s.WithFill(FillColor.Value).WithFillOpacity(FillOpacity);
        if (StrokeColor.HasValue)
            s = s.WithStroke(StrokeColor.Value, StrokeWidth).WithStrokeOpacity(StrokeOpacity);
        return s;
    }
}

internal sealed class PubKeySecurityContext
{
    internal List<X509Certificate2> Recipients { get; } = new();
    internal PdfPermissions Permissions { get; set; } = PdfPermissions.All;
}

/// <summary>
/// Exported C API for the native AOT shared library (see pdfnative.h for the C declarations).
/// All entry points are callable from Python/ctypes, PHP/FFI, Ruby/Fiddle, or any C-compatible host.
/// </summary>
public static class PdfNativeApi
{
    private static readonly nint s_errorBuf;
    private static readonly object s_errorLock = new();

    static unsafe PdfNativeApi()
    {
        s_errorBuf = (nint)NativeMemory.AllocZeroed(2048);
    }

    private static unsafe void SetError(Exception ex)
    {
        var sb = new StringBuilder();
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

    private static T? GetCtx<T>(nint handle) where T : class
    {
        if (handle == 0) return null;
        var gh = GCHandle.FromIntPtr(handle);
        return gh.IsAllocated ? gh.Target as T : null;
    }

    // ─── P/Invoke library resolver ────────────────────────────────────────────
    // When pdfnative.so is loaded by a foreign host (Python/PHP/Ruby), .NET's
    // P/Invoke resolver may search for dependencies relative to the host process.
    // Set PDFNATIVE_LIB_DIR to the directory containing pdfnative.so before
    // calling pdf_init() to have sibling libraries resolved from there first.

    private static void SetupLibraryResolver(string libDir)
    {
        RegisterSafe(ResolverFor(libDir), typeof(PdfNativeApi).Assembly);
    }

    private static DllImportResolver ResolverFor(string dir) =>
        (name, _, _) => ResolveFromDir(name, dir);

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

    // ─── Exported C API ───────────────────────────────────────────────────────

    /// <summary>Initialize the PDF library. Optional — call once at startup. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_init")]
    public static int Init()
    {
        try
        {
            string? libDir = Environment.GetEnvironmentVariable("PDFNATIVE_LIB_DIR");
            if (libDir is not null)
                SetupLibraryResolver(libDir);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    // ── Document ──────────────────────────────────────────────────────────────

    /// <summary>Create a new PDF document. Returns opaque handle, or 0 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_create")]
    public static nint DocCreate()
    {
        try { return GCHandle.ToIntPtr(GCHandle.Alloc(new DocContext())); }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Set the document title metadata. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_title")]
    public static unsafe int DocSetTitle(nint handle, byte* titleUtf8)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Doc.WithTitle(Marshal.PtrToStringUTF8((nint)titleUtf8) ?? "");
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the document author metadata. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_author")]
    public static unsafe int DocSetAuthor(nint handle, byte* authorUtf8)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Doc.WithAuthor(Marshal.PtrToStringUTF8((nint)authorUtf8) ?? "");
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the document subject metadata. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_subject")]
    public static unsafe int DocSetSubject(nint handle, byte* subjectUtf8)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Doc.WithSubject(Marshal.PtrToStringUTF8((nint)subjectUtf8) ?? "");
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the document creator metadata. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_creator")]
    public static unsafe int DocSetCreator(nint handle, byte* creatorUtf8)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Doc.WithCreator(Marshal.PtrToStringUTF8((nint)creatorUtf8) ?? "Majorsilence.Pdf");
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Apply password-based encryption to the document.
    /// <paramref name="encVersion"/>: 0 = AES-256 (PDF 2.0, default), 1 = AES-128 (PDF 1.5+).
    /// <paramref name="permissionsFlags"/>: bitmask of allowed operations — use -1 for all permissions.
    ///   Print=4, ModifyContent=8, CopyText=16, AddAnnotations=32, FillForms=256,
    ///   ExtractText=512, Assemble=1024, PrintHighQuality=2048.
    /// <paramref name="ownerPasswordUtf8"/>: pass NULL to use the same password as the user password.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_security")]
    public static unsafe int DocSetSecurity(nint handle,
        byte* userPasswordUtf8, byte* ownerPasswordUtf8,
        int permissionsFlags, int encVersion)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }

            string userPw  = Marshal.PtrToStringUTF8((nint)userPasswordUtf8) ?? "";
            string? ownerPw = ownerPasswordUtf8 != null
                ? Marshal.PtrToStringUTF8((nint)ownerPasswordUtf8)
                : null;

            var perms = permissionsFlags < 0
                ? PdfPermissions.All
                : (PdfPermissions)permissionsFlags;

            var ver = encVersion == 1
                ? PdfEncryptionVersion.AES128
                : PdfEncryptionVersion.AES256;

            var security = PdfSecurity.Protect(userPw, ownerPw)
                .WithPermissions(perms)
                .WithEncryptionVersion(ver);

            ctx.Doc.WithSecurity(security);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Set PDF/A conformance level.
    /// level: 0 = None (default), 1 = PDF/A-1b, 2 = PDF/A-2b, 3 = PDF/A-3b.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_conformance")]
    public static int DocSetConformance(nint handle, int level)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            ctx.Doc.WithConformance(level switch
            {
                1 => PdfConformance.PdfA1b,
                2 => PdfConformance.PdfA2b,
                3 => PdfConformance.PdfA3b,
                _ => PdfConformance.None,
            });
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Embed an invisible (appearW == 0) or visible (appearW > 0) PKCS#7 digital signature.
    /// p12Path: path to a PKCS#12 (.p12/.pfx) file containing the signing certificate and private key.
    /// p12Password: password for the PKCS#12 file (pass NULL or empty string if not password-protected).
    /// reason/location/tsaUrl: optional — pass NULL to omit each.
    /// appearX/Y/W/H: visible appearance rectangle in top-left coordinates (points).
    ///   Pass appearW == 0 for an invisible signature (example 14).
    ///   Pass appearW > 0 for a visible signature appearance (example 22).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_signature")]
    public static unsafe int DocSetSignature(nint handle,
        byte* p12PathUtf8, byte* p12PasswordUtf8,
        byte* reasonUtf8, byte* locationUtf8, byte* tsaUrlUtf8,
        float appearX, float appearY, float appearW, float appearH)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }

            string p12Path  = Marshal.PtrToStringUTF8((nint)p12PathUtf8) ?? "";
            string p12Pass  = Marshal.PtrToStringUTF8((nint)p12PasswordUtf8) ?? "";
            string? reason   = reasonUtf8   != null ? Marshal.PtrToStringUTF8((nint)reasonUtf8)   : null;
            string? location = locationUtf8 != null ? Marshal.PtrToStringUTF8((nint)locationUtf8) : null;
            string? tsaUrl   = tsaUrlUtf8   != null ? Marshal.PtrToStringUTF8((nint)tsaUrlUtf8)   : null;

#if NET9_0_OR_GREATER
            var cert = X509CertificateLoader.LoadPkcs12FromFile(p12Path, p12Pass,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#else
            var cert = new X509Certificate2(p12Path, p12Pass,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#endif

            var opts = new PdfSignatureOptions(cert);
            if (reason   is not null) opts = opts.WithReason(reason);
            if (location is not null) opts = opts.WithLocation(location);
            if (tsaUrl   is not null) opts = opts.WithTimestampAuthority(tsaUrl);
            if (appearW  > 0f)       opts = opts.WithAppearance(appearX, appearY, appearW, appearH);

            ctx.Doc.WithSignature(opts);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Encrypt the document so that only holders of the specified X.509 certificates can open it.
    /// ps: a handle created with pdf_pubkey_security_create() with recipients added via
    ///     pdf_pubkey_security_add_recipient(). Pass 0 to remove previously configured pubkey encryption.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_set_pubkey_security")]
    public static int DocSetPubKeySecurity(nint handle, nint ps)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }

            if (ps == 0)
            {
                ctx.Doc.WithPublicKeySecurity(null);
                return 0;
            }

            var psctx = GetCtx<PubKeySecurityContext>(ps);
            if (psctx is null) { SetError("Invalid pubkey security handle."); return -1; }
            if (psctx.Recipients.Count == 0) { SetError("No recipient certificates added."); return -1; }

            var security = PdfPublicKeySecurity.ForRecipients(psctx.Recipients.ToArray())
                .WithPermissions(psctx.Permissions);
            ctx.Doc.WithPublicKeySecurity(security);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    // ── PubKey Security ───────────────────────────────────────────────────────

    /// <summary>
    /// Create a public-key security handle. Add recipient certificates with
    /// pdf_pubkey_security_add_recipient(), then pass the handle to pdf_doc_set_pubkey_security().
    /// Returns handle on success, or 0 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_pubkey_security_create")]
    public static nint PubKeySecurityCreate()
    {
        try { return GCHandle.ToIntPtr(GCHandle.Alloc(new PubKeySecurityContext())); }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>
    /// Add a recipient certificate from a DER or PEM file.
    /// certPath: path to an X.509 certificate file (.cer/.der/.pem) — public key only, no password needed.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_pubkey_security_add_recipient")]
    public static unsafe int PubKeySecurityAddRecipient(nint ps, byte* certPathUtf8)
    {
        try
        {
            var ctx = GetCtx<PubKeySecurityContext>(ps);
            if (ctx is null) { SetError("Invalid pubkey security handle."); return -1; }
            string path = Marshal.PtrToStringUTF8((nint)certPathUtf8) ?? "";
#if NET9_0_OR_GREATER
            ctx.Recipients.Add(X509CertificateLoader.LoadCertificateFromFile(path));
#else
            ctx.Recipients.Add(new X509Certificate2(path));
#endif
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Set the permissions bitmask for public-key encrypted documents (default: all permissions).
    /// Uses the same flag values as pdf_doc_set_security (Print=4, ModifyContent=8, etc.).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_pubkey_security_set_permissions")]
    public static int PubKeySecuritySetPermissions(nint ps, int permissionsFlags)
    {
        try
        {
            var ctx = GetCtx<PubKeySecurityContext>(ps);
            if (ctx is null) { SetError("Invalid pubkey security handle."); return -1; }
            ctx.Permissions = permissionsFlags < 0 ? PdfPermissions.All : (PdfPermissions)permissionsFlags;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a public-key security handle.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_pubkey_security_close")]
    public static void PubKeySecurityClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    /// <summary>
    /// Add a page and return an opaque canvas handle for drawing.
    /// Use well-known sizes: A4 = 595.28 × 841.89, Letter = 612 × 792 (points, 1 pt = 1/72 in).
    /// Returns a canvas handle on success, or 0 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_add_page")]
    public static nint DocAddPage(nint handle, float width, float height)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return 0; }
            var canvas = ctx.Doc.AddPage(width, height);
            return GCHandle.ToIntPtr(GCHandle.Alloc(new CanvasContext(canvas)));
        }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Write the document to a file. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_save_file")]
    public static unsafe int DocSaveFile(nint handle, byte* pathUtf8)
    {
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            string path = Marshal.PtrToStringUTF8((nint)pathUtf8) ?? "";
            ctx.Doc.Save(path);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Write the document to a heap-allocated buffer.
    /// On success, *outData points to memory that must be freed with pdf_free().
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_save_buffer")]
    public static unsafe int DocSaveBuffer(nint handle, byte** outData, int* outSize)
    {
        *outData = null;
        *outSize = 0;
        try
        {
            var ctx = GetCtx<DocContext>(handle);
            if (ctx is null) { SetError("Invalid handle."); return -1; }
            byte[] data = ctx.Doc.ToBytes();
            byte* ptr = (byte*)NativeMemory.Alloc((nuint)data.Length);
            data.AsSpan().CopyTo(new Span<byte>(ptr, data.Length));
            *outData = ptr;
            *outSize = data.Length;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Close a document handle and release associated resources.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_doc_close")]
    public static void DocClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draw text at (x, y) with the given style. y is the text baseline.
    /// style may be 0 to use the default style (Helvetica 12 pt black).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_text")]
    public static unsafe int CanvasDrawText(nint canvas, byte* textUtf8, float x, float y, nint style)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            string text = Marshal.PtrToStringUTF8((nint)textUtf8) ?? "";
            var sctx = style != 0 ? GetCtx<StyleContext>(style) : null;
            cctx.Canvas.DrawText(text, x, y, sctx?.ToTextStyle());
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw word-wrapped text inside a box. Returns the character offset of the first
    /// character that did not fit (equals text length when all text was rendered),
    /// or -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_textbox")]
    public static unsafe int CanvasDrawTextBox(nint canvas, byte* textUtf8,
        float x, float y, float width, float height, nint style, float lineSpacing)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            string text = Marshal.PtrToStringUTF8((nint)textUtf8) ?? "";
            var sctx = style != 0 ? GetCtx<StyleContext>(style) : null;
            float ls = lineSpacing <= 0f ? 1.2f : lineSpacing;
            return cctx.Canvas.DrawTextBox(text, x, y, width, height, sctx?.ToTextStyle(), ls);
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw a straight line from (x1, y1) to (x2, y2).
    /// r, g, b are byte values 0-255. Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_line")]
    public static int CanvasDrawLine(nint canvas,
        float x1, float y1, float x2, float y2,
        byte r, byte g, byte b, float width)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            var stroke = StrokeStyle.Default
                .WithColor(new PdfColor(r, g, b))
                .WithWidth(width);
            cctx.Canvas.DrawLine(x1, y1, x2, y2, stroke);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw a rectangle with top-left at (x, y) with given width and height.
    /// hasFill / hasStroke: 0 = false, non-zero = true.
    /// Fill and stroke colours use byte values 0-255.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_rect")]
    public static int CanvasDrawRect(nint canvas,
        float x, float y, float width, float height,
        byte fillR, byte fillG, byte fillB, int hasFill,
        byte strokeR, byte strokeG, byte strokeB, float strokeWidth, int hasStroke)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            ShapeStyle style = ShapeStyle.Empty;
            if (hasFill != 0)
                style = style.WithFill(new PdfColor(fillR, fillG, fillB));
            if (hasStroke != 0)
                style = style.WithStroke(new PdfColor(strokeR, strokeG, strokeB), strokeWidth);
            cctx.Canvas.DrawRectangle(x, y, width, height, style);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw an ellipse bounded by the rectangle at (x, y) with given width and height.
    /// hasFill / hasStroke: 0 = false, non-zero = true.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_ellipse")]
    public static int CanvasDrawEllipse(nint canvas,
        float x, float y, float width, float height,
        byte fillR, byte fillG, byte fillB, int hasFill,
        byte strokeR, byte strokeG, byte strokeB, float strokeWidth, int hasStroke)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            ShapeStyle style = ShapeStyle.Empty;
            if (hasFill != 0)
                style = style.WithFill(new PdfColor(fillR, fillG, fillB));
            if (hasStroke != 0)
                style = style.WithStroke(new PdfColor(strokeR, strokeG, strokeB), strokeWidth);
            cctx.Canvas.DrawEllipse(x, y, width, height, style);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw an image with top-left at (x, y) scaled to (w × h) points.
    /// isJpeg: non-zero if data is JPEG bytes; 0 if data is raw RGB24 bytes (width*height*3).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_image")]
    public static unsafe int CanvasDrawImage(nint canvas,
        byte* data, int dataLen, int pixelWidth, int pixelHeight, int isJpeg,
        float x, float y, float w, float h)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            byte[] imageData = new byte[dataLen];
            new Span<byte>(data, dataLen).CopyTo(imageData);
            cctx.Canvas.DrawImage(imageData, pixelWidth, pixelHeight, isJpeg != 0, x, y, w, h);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw a table (created with pdf_table_create) with its top-left at (x, y).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_table")]
    public static int CanvasDrawTable(nint canvas, nint table, float x, float y)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            var tctx = GetCtx<TableContext>(table);
            if (tctx is null) { SetError("Invalid table handle."); return -1; }
            cctx.Canvas.DrawTable(tctx.Table, x, y);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Add a clickable hyperlink over the given area. Returns 0 on success, -1 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_add_link")]
    public static unsafe int CanvasAddLink(nint canvas, float x, float y, float width, float height, byte* uriUtf8)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            string uri = Marshal.PtrToStringUTF8((nint)uriUtf8) ?? "";
            cctx.Canvas.AddLink(x, y, width, height, uri);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a canvas handle. The drawn content is retained in the document.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_close")]
    public static void CanvasClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    // ── Style ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a text style handle. Initial values: Helvetica 12 pt black, left-aligned.
    /// Returns handle on success, or 0 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_create")]
    public static nint StyleCreate()
    {
        try { return GCHandle.ToIntPtr(GCHandle.Alloc(new StyleContext())); }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Set the font family name (e.g. "Helvetica", "Times-Roman", "Courier"). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_font_family")]
    public static unsafe int StyleSetFontFamily(nint style, byte* familyUtf8)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.FontFamily   = Marshal.PtrToStringUTF8((nint)familyUtf8) ?? "Helvetica";
            ctx.FontFilePath = null;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Embed the TrueType/OpenType font at the given path. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_font_file")]
    public static unsafe int StyleSetFontFile(nint style, byte* pathUtf8)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.FontFilePath = Marshal.PtrToStringUTF8((nint)pathUtf8);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set font size in points. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_size")]
    public static int StyleSetSize(nint style, float points)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.FontSize = points;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set text colour using byte values 0-255. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_color")]
    public static int StyleSetColor(nint style, byte r, byte g, byte b)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.Color = new PdfColor(r, g, b);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set bold. isBold: 0 = false, non-zero = true. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_bold")]
    public static int StyleSetBold(nint style, int isBold)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.IsBold = isBold != 0;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set italic. isItalic: 0 = false, non-zero = true. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_italic")]
    public static int StyleSetItalic(nint style, int isItalic)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.IsItalic = isItalic != 0;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set text alignment. alignment: 0 = left, 1 = center, 2 = right. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_alignment")]
    public static int StyleSetAlignment(nint style, int alignment)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.Alignment = alignment switch { 1 => TextAlignment.Center, 2 => TextAlignment.Right, _ => TextAlignment.Left };
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Set text decoration.
    /// decoration: 0 = none, 1 = underline, 2 = strikethrough, 3 = overline.
    /// Returns 0 on success.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_set_decoration")]
    public static int StyleSetDecoration(nint style, int decoration)
    {
        try
        {
            var ctx = GetCtx<StyleContext>(style);
            if (ctx is null) { SetError("Invalid style handle."); return -1; }
            ctx.Decoration = decoration switch
            {
                1 => TextDecoration.Underline,
                2 => TextDecoration.Strikethrough,
                3 => TextDecoration.Overline,
                _ => TextDecoration.None,
            };
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a style handle.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_style_close")]
    public static void StyleClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    // ── Shape Style ───────────────────────────────────────────────────────────

    /// <summary>
    /// Create a shape style handle for use with pdf_canvas_draw_rect_styled and
    /// pdf_canvas_draw_ellipse_styled. Defaults: no fill, no stroke, full opacity.
    /// Returns handle on success, or 0 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_create")]
    public static nint ShapeStyleCreate()
    {
        try { return GCHandle.ToIntPtr(GCHandle.Alloc(new ShapeStyleContext())); }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Set the fill colour (byte values 0-255). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_set_fill")]
    public static int ShapeStyleSetFill(nint style, byte r, byte g, byte b)
    {
        try
        {
            var ctx = GetCtx<ShapeStyleContext>(style);
            if (ctx is null) { SetError("Invalid shape style handle."); return -1; }
            ctx.FillColor = new PdfColor(r, g, b);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the fill opacity (0.0 = transparent, 1.0 = opaque). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_set_fill_opacity")]
    public static int ShapeStyleSetFillOpacity(nint style, float opacity)
    {
        try
        {
            var ctx = GetCtx<ShapeStyleContext>(style);
            if (ctx is null) { SetError("Invalid shape style handle."); return -1; }
            ctx.FillOpacity = opacity < 0f ? 0f : opacity > 1f ? 1f : opacity;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the stroke colour and width (byte values 0-255). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_set_stroke")]
    public static int ShapeStyleSetStroke(nint style, byte r, byte g, byte b, float width)
    {
        try
        {
            var ctx = GetCtx<ShapeStyleContext>(style);
            if (ctx is null) { SetError("Invalid shape style handle."); return -1; }
            ctx.StrokeColor = new PdfColor(r, g, b);
            ctx.StrokeWidth = width;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the stroke opacity (0.0 = transparent, 1.0 = opaque). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_set_stroke_opacity")]
    public static int ShapeStyleSetStrokeOpacity(nint style, float opacity)
    {
        try
        {
            var ctx = GetCtx<ShapeStyleContext>(style);
            if (ctx is null) { SetError("Invalid shape style handle."); return -1; }
            ctx.StrokeOpacity = opacity < 0f ? 0f : opacity > 1f ? 1f : opacity;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a shape style handle.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_shape_style_close")]
    public static void ShapeStyleClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    /// <summary>
    /// Draw a rectangle using a shape style handle (supports opacity).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_rect_styled")]
    public static int CanvasDrawRectStyled(nint canvas, float x, float y, float width, float height, nint style)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            var sctx = GetCtx<ShapeStyleContext>(style);
            if (sctx is null) { SetError("Invalid shape style handle."); return -1; }
            cctx.Canvas.DrawRectangle(x, y, width, height, sctx.ToShapeStyle());
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Draw an ellipse using a shape style handle (supports opacity).
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_canvas_draw_ellipse_styled")]
    public static int CanvasDrawEllipseStyled(nint canvas, float x, float y, float width, float height, nint style)
    {
        try
        {
            var cctx = GetCtx<CanvasContext>(canvas);
            if (cctx is null) { SetError("Invalid canvas handle."); return -1; }
            var sctx = GetCtx<ShapeStyleContext>(style);
            if (sctx is null) { SetError("Invalid shape style handle."); return -1; }
            cctx.Canvas.DrawEllipse(x, y, width, height, sctx.ToShapeStyle());
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Create a table handle with the given column widths in points.
    /// Returns handle on success, or 0 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_create")]
    public static unsafe nint TableCreate(float* colWidths, int nCols)
    {
        try
        {
            if (colWidths == null || nCols <= 0) { SetError("colWidths must not be null and nCols must be > 0."); return 0; }
            var widths = new float[nCols];
            new Span<float>(colWidths, nCols).CopyTo(widths);
            return GCHandle.ToIntPtr(GCHandle.Alloc(new TableContext(new PdfTable(widths))));
        }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Set the header background colour (byte values 0-255). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_set_header_bg")]
    public static int TableSetHeaderBg(nint handle, byte r, byte g, byte b)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.Table = ctx.Table.WithHeaderBackground(new PdfColor(r, g, b));
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the alternate row background colour (byte values 0-255). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_set_alternate_bg")]
    public static int TableSetAlternateBg(nint handle, byte r, byte g, byte b)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.Table = ctx.Table.WithAlternateRowBackground(new PdfColor(r, g, b));
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the border colour and width. Pass width = 0 to suppress borders. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_set_border")]
    public static int TableSetBorder(nint handle, byte r, byte g, byte b, float width)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.Table = width <= 0f
                ? ctx.Table.WithNoBorder()
                : ctx.Table.WithBorder(new PdfColor(r, g, b), width);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Set the cell padding in points (default 4). Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_set_cell_padding")]
    public static int TableSetCellPadding(nint handle, float padding)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.Table = ctx.Table.WithCellPadding(padding);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Stage one cell for the current row. Call once per column, then call
    /// pdf_table_commit_row() to append the row to the table.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_stage_cell")]
    public static unsafe int TableStageCell(nint handle, byte* textUtf8)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.StagedCells.Add(Marshal.PtrToStringUTF8((nint)textUtf8) ?? "");
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Commit the staged cells as a new table row, then clear the staged cells.
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_commit_row")]
    public static int TableCommitRow(nint handle)
    {
        try
        {
            var ctx = GetCtx<TableContext>(handle);
            if (ctx is null) { SetError("Invalid table handle."); return -1; }
            ctx.Table.AddRow(ctx.StagedCells.ToArray());
            ctx.StagedCells.Clear();
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a table handle.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_table_close")]
    public static void TableClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    // ── Merge ─────────────────────────────────────────────────────────────────

    /// <summary>Create a PDF merger handle. Returns handle on success, or 0 on error.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_merge_create")]
    public static nint MergeCreate()
    {
        try { return GCHandle.ToIntPtr(GCHandle.Alloc(new MergeContext())); }
        catch (Exception ex) { SetError(ex); return 0; }
    }

    /// <summary>Add a PDF document (as raw bytes) to the merge queue. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_merge_add_bytes")]
    public static unsafe int MergeAddBytes(nint handle, byte* data, int dataLen)
    {
        try
        {
            var ctx = GetCtx<MergeContext>(handle);
            if (ctx is null) { SetError("Invalid merge handle."); return -1; }
            byte[] bytes = new byte[dataLen];
            new Span<byte>(data, dataLen).CopyTo(bytes);
            ctx.Merger.Add(bytes);
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Merge all added PDFs and write the result to a file. Returns 0 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_merge_save_file")]
    public static unsafe int MergeSaveFile(nint handle, byte* pathUtf8)
    {
        try
        {
            var ctx = GetCtx<MergeContext>(handle);
            if (ctx is null) { SetError("Invalid merge handle."); return -1; }
            string path = Marshal.PtrToStringUTF8((nint)pathUtf8) ?? "";
            File.WriteAllBytes(path, ctx.Merger.Merge());
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>
    /// Merge all added PDFs and write the result to a heap-allocated buffer.
    /// On success, *outData points to memory that must be freed with pdf_free().
    /// Returns 0 on success, -1 on error.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_merge_save_buffer")]
    public static unsafe int MergeSaveBuffer(nint handle, byte** outData, int* outSize)
    {
        *outData = null;
        *outSize = 0;
        try
        {
            var ctx = GetCtx<MergeContext>(handle);
            if (ctx is null) { SetError("Invalid merge handle."); return -1; }
            byte[] data = ctx.Merger.Merge();
            byte* ptr = (byte*)NativeMemory.Alloc((nuint)data.Length);
            data.AsSpan().CopyTo(new Span<byte>(ptr, data.Length));
            *outData = ptr;
            *outSize = data.Length;
            return 0;
        }
        catch (Exception ex) { SetError(ex); return -1; }
    }

    /// <summary>Release a merger handle.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_merge_close")]
    public static void MergeClose(nint handle)
    {
        if (handle == 0) return;
        try { GCHandle.FromIntPtr(handle).Free(); } catch { }
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    /// <summary>Free a buffer returned by pdf_doc_save_buffer or pdf_merge_save_buffer.</summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_free")]
    public static unsafe void Free(void* ptr) { if (ptr != null) NativeMemory.Free(ptr); }

    /// <summary>
    /// Return a UTF-8 string describing the most recent error.
    /// The returned pointer is valid until the next error occurs. Never returns NULL.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "pdf_last_error")]
    public static unsafe byte* LastError() => (byte*)s_errorBuf;
}
