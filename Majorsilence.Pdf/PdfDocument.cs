// Copyright (C) 2025 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using Majorsilence.Pdf.Internal;

namespace Majorsilence.Pdf
{
    /// <summary>
    /// Root of a PDF document.  Create one with <see cref="Create"/>, add pages, then save.
    /// </summary>
    /// <example>
    /// <code>
    /// // Callback style (recommended for simple documents)
    /// PdfDocument.Create()
    ///     .WithTitle("My Report")
    ///     .AddPage(PageSizes.A4, canvas =>
    ///     {
    ///         canvas.DrawText("Hello, World!", 72, 72, TextStyle.Default.WithSize(24));
    ///     })
    ///     .Save("report.pdf");
    ///
    /// // Incremental style (useful when building content imperatively)
    /// var doc = PdfDocument.Create();
    /// var canvas = doc.AddPage(PageSizes.Letter);
    /// canvas.DrawRectangle(20, 20, 200, 100, ShapeStyle.Filled(PdfColor.LightGray));
    /// canvas.DrawText("Page 1", 30, 60);
    /// doc.Save("output.pdf");
    /// </code>
    /// </example>
    public sealed class PdfDocument
    {
        private readonly PdfSerializer _ser = new PdfSerializer();
        private readonly FontCache _fontCache = new FontCache();
        private FontRegistry? _fontRegistry;

        private PdfVersion _version = PdfVersion.Pdf14;

        private PdfDocument() { }

        // ── factory ──────────────────────────────────────────────────────────

        /// <summary>Create a new, empty PDF document.</summary>
        public static PdfDocument Create() => new PdfDocument();

        /// <summary>
        /// Set the PDF specification version written to the output file.
        /// Defaults to <see cref="PdfVersion.Pdf14"/> for broadest compatibility.
        /// </summary>
        public PdfDocument WithVersion(PdfVersion version) { _version = version; return this; }

        // ── font registry ─────────────────────────────────────────────────────

        /// <summary>
        /// Attach a <see cref="FontRegistry"/> that maps logical family names to TrueType fonts
        /// and defines the fallback chain used when a glyph is missing from the primary font.
        /// </summary>
        public PdfDocument WithFontRegistry(FontRegistry registry)
        {
            _fontRegistry = registry ?? throw new ArgumentNullException(nameof(registry));
            return this;
        }

        // ── metadata ─────────────────────────────────────────────────────────

        private string _author  = "";
        private string _title   = "";
        private string _subject = "";
        private string _creator = "Majorsilence.Pdf";

        public PdfDocument WithAuthor(string author)   { _author  = author  ?? ""; return this; }
        public PdfDocument WithTitle(string title)     { _title   = title   ?? ""; return this; }
        public PdfDocument WithSubject(string subject) { _subject = subject ?? ""; return this; }
        public PdfDocument WithCreator(string creator) { _creator = creator ?? "Majorsilence.Pdf"; return this; }

        // ── page management ──────────────────────────────────────────────────

        /// <summary>
        /// Add a page and return its <see cref="PdfCanvas"/> for incremental drawing.
        /// </summary>
        public PdfCanvas AddPage(float width, float height)
        {
            var page = _ser.CreatePage(width, height);
            return new PdfCanvas(page, _ser, _fontCache, _fontRegistry);
        }

        /// <inheritdoc cref="AddPage(float,float)"/>
        public PdfCanvas AddPage(PdfPageSize size) => AddPage(size.Width, size.Height);

        /// <summary>
        /// Add a page, pass its <see cref="PdfCanvas"/> to <paramref name="draw"/>, then
        /// return this document for further chaining.
        /// </summary>
        public PdfDocument AddPage(float width, float height, Action<PdfCanvas> draw)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            var canvas = AddPage(width, height);
            draw(canvas);
            return this;
        }

        /// <inheritdoc cref="AddPage(float,float,Action{PdfCanvas})"/>
        public PdfDocument AddPage(PdfPageSize size, Action<PdfCanvas> draw) =>
            AddPage(size.Width, size.Height, draw);

        // ── output ───────────────────────────────────────────────────────────

        /// <summary>Write the PDF to <paramref name="stream"/>.</summary>
        public void Save(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _ser.SetMetadata(_author, _title, _subject, _creator);
            _ser.SetVersion(_version);
            _ser.Write(stream);
        }

        /// <summary>Write the PDF to a file at <paramref name="path"/>.</summary>
        public void Save(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                Save(fs);
        }

        /// <summary>Return the document as a byte array.</summary>
        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                Save(ms);
                return ms.ToArray();
            }
        }
    }
}
