# Majorsilence.Pdf

**Zero-dependency PDF generation for .NET.** Pure managed code — no native binaries, no libgdiplus, no Chromium. Produces PDF 1.4, PDF 1.7, and PDF 2.0 output with optional PDF/A conformance. Native-AOT-compatible, which makes it a good fit for AWS Lambda, Azure Functions, and other serverless/container workloads where cold-start size and native dependencies matter.

Majorsilence.Pdf is tri licensed under Apache-2.0, MIT, or BSD-3-Clause. Pick your choice.

- SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
- Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

---

## At a glance

| Capability | Package |
|---|---|
| Text, shapes, images, tables, links | `Majorsilence.Pdf` |
| Fluent flow layout — rows, columns, auto-pagination (`PdfLayout`) | `Majorsilence.Pdf` |
| PDF/A-1b, PDF/A-2b, PDF/A-3b conformance | `Majorsilence.Pdf` |
| Multi-document merge | `Majorsilence.Pdf` |
| AES-128 (R=4) / AES-256 (R=6) password protection | `Majorsilence.Pdf.Security` |
| Permission flags (print, copy, edit) | `Majorsilence.Pdf.Security` |
| PKCS#7 digital signatures | `Majorsilence.Pdf.Security` |
| Markdown rendering (headings, lists, code, tables, links) | `Majorsilence.Pdf.Markdown` |
| Live browser preview while you code (`mspdf-preview`) | `Majorsilence.Pdf.Previewer` (dotnet tool) |

Targets .NET Standard 2.0, .NET 6, .NET 8, and .NET 10 — usable from .NET Framework 4.6.1+ up through the latest runtime.

## Quick Start

```csharp
using Majorsilence.Pdf;

PdfDocument.Create()
    .WithTitle("My Report")
    .AddPage(PageSizes.A4, canvas =>
    {
        canvas.DrawText("Hello, World!", 72, 72, TextStyle.Default.WithSize(24));
        canvas.DrawRectangle(72, 120, 200, 60,
            ShapeStyle.Filled(PdfColor.LightGray).WithStroke(PdfColor.Black, 1f));
    })
    .Save("report.pdf");
```

## Features

### Drawing primitives

| Method | Description |
|---|---|
| `DrawText` | Single-line text at a point |
| `DrawTextBox` | Multi-line word-wrapped text in a bounding box; returns overflow index for pagination |
| `DrawLine` | Straight line segment |
| `DrawRectangle` | Rectangle with optional fill and border |
| `DrawEllipse` | Ellipse / circle |
| `DrawPolygon` | Arbitrary closed polygon |
| `DrawCurve` | Cubic Bézier curve |
| `DrawImage` | Embedded bitmap image |
| `DrawTable` | Table with header row, alternating-row backgrounds, borders, and cell padding |
| `MeasureTextWidth` | Measure the pixel width of a string |
| `MeasureTextBoxHeight` | Measure the height of wrapped text in a given width |

### Styles

**`TextStyle`** — fluent immutable builder:
```csharp
TextStyle.Default
    .WithSize(14)
    .WithBold(true)
    .WithColor(PdfColor.FromHex("#003366"))
    .WithAlignment(TextAlignment.Center)
```

**`ShapeStyle`** — fill, border, and opacity:
```csharp
ShapeStyle.Filled(PdfColor.FromRgb(200, 220, 255))
    .WithStroke(PdfColor.Blue, 1.5f)
    .WithFillOpacity(0.5f)   // 50% transparent fill
    .WithStrokeOpacity(0.8f)
```

**`StrokeStyle`** — line style and opacity:
```csharp
StrokeStyle.Default
    .WithColor(PdfColor.Red)
    .WithWidth(2f)
    .Dashed()
    .WithOpacity(0.6f)
```

### Multi-line text (`DrawTextBox`)

```csharp
int overflowAt = canvas.DrawTextBox(
    text:        longText,
    x:           72,
    y:           100,
    width:       400,
    height:      600,
    style:       TextStyle.Default.WithSize(11),
    lineSpacing: 1.4f);

// If overflowAt < text.Length, paginate by passing text.Substring(overflowAt) to the next page.
```

### Tables

```csharp
var table = new PdfTable(new float[] { 120, 200, 80 })
    .WithHeaderBackground(PdfColor.FromRgb(70, 130, 180))
    .WithAlternateRowBackground(PdfColor.FromRgb(240, 240, 240))
    .WithBorder(PdfColor.Gray, 0.5f)
    .WithCellPadding(6f);

table.AddRow("Name", "Description", "Value");      // header
table.AddRow("Item A", "First item in the list", "1.00");
table.AddRow("Item B", "Second item", "2.50");

canvas.DrawTable(table, x: 72, y: 120, out float tableBottom);
```

### PDF/A conformance

```csharp
PdfDocument.Create()
    .WithConformance(PdfConformance.PdfA2b)  // or PdfA1b, PdfA3b
    .WithFontRegistry(registry)              // embedded TrueType fonts required
    .AddPage(PageSizes.A4, canvas => { ... })
    .Save("archive.pdf");
```

PDF/A automatically sets the correct PDF version, embeds an sRGB ICC output intent, and writes the required XMP conformance claim. PDF/A and encryption cannot be combined.

### Fonts

Register TrueType fonts and set up a fallback chain for glyphs the primary font lacks:

```csharp
var registry = new FontRegistry()
    .AddFamily("NotoSans",
        regular: "/path/to/NotoSans-Regular.ttf",
        bold:    "/path/to/NotoSans-Bold.ttf")
    .AddFallback("NotoSans");   // used when the default font has no glyph for a character

PdfDocument.Create()
    .WithFontRegistry(registry)
    .AddPage(PageSizes.A4, canvas =>
    {
        canvas.DrawText("Hello", 72, 72,
            TextStyle.Default.WithFamily("NotoSans").WithSize(14));
    })
    .Save("output.pdf");
```

Unicode shaping with multi-script support (Latin, Cyrillic, Greek, Arabic, CJK) is available when the appropriate NotoSans fonts are registered.

### Images

```csharp
byte[] imageBytes = File.ReadAllBytes("photo.jpg");
canvas.DrawImage(imageBytes, x: 72, y: 200, width: 200, height: 150);
```

### Links and tooltips

```csharp
canvas.AddLink(x: 72, y: 300, width: 150, height: 20, uri: "https://example.com");
canvas.AddTooltip(x: 72, y: 330, width: 150, height: 20, tooltip: "Hover text");
```

### Merging documents

```csharp
byte[] combined = new PdfMerger()
    .Add(File.ReadAllBytes("cover.pdf"))
    .Add(File.ReadAllBytes("body.pdf"))
    .WithTitle("Combined Report")
    .Merge();

File.WriteAllBytes("combined.pdf", combined);
```

### Page sizes

Pre-defined sizes in `PageSizes`: `A4`, `A3`, `Letter`, `Legal`, `Tabloid`. Custom sizes via `AddPage(width, height)`.

### Document metadata

```csharp
PdfDocument.Create()
    .WithTitle("Annual Report")
    .WithAuthor("Acme Corp")
    .WithSubject("Financial Summary")
    .Save("report.pdf");
```

### PDF versions

| Version | Use case |
|---|---|
| `PdfVersion.Pdf14` | Maximum compatibility (default) |
| `PdfVersion.Pdf17` | PDF/A-2b and PDF/A-3b |
| `PdfVersion.Pdf20` | Latest spec; includes XMP metadata stream |

---

## Security companion

Password encryption and digital signatures are provided by the companion package **Majorsilence.Pdf.Security**. See that package's Readme for details.

## Native AOT — verified, not just compatible

Majorsilence.Pdf targets `net6.0` and above with `IsAotCompatible` enabled (which turns on the trim, AOT, and single-file analyzers) and has zero reflection in the drawing/rendering path. Every CI run publishes [`Examples/PdfAotSmokeTest`](https://github.com/majorsilence/Reporting/tree/main/Examples/PdfAotSmokeTest) as a self-contained Native AOT binary and **actually executes it** — text, tables, `PdfLayout` flow layout, AES password protection, PKCS#7 signing, and PDF merge all have to produce a structurally valid PDF or the build fails. This isn't just "it compiled without warnings"; it's "the binary ran and did the work."

Measured on Linux x64 (.NET 10, `dotnet publish -r linux-x64 -p:PublishAot=true --self-contained true`):

| Metric | Value |
|---|---|
| Published binary size | ~6.7 MiB |
| Cold start, full smoke-test workload (3 documents + AES encryption + PKCS#7 signing + merge) | ~70–150 ms |
| Native dependencies | none |
| Runtime required on target machine | none — the binary is self-contained |

This makes Majorsilence.Pdf a good fit for AWS Lambda, Azure Functions, and other serverless or container workloads where cold-start latency and image size matter — no native library to bundle, no JIT warm-up.

`Majorsilence.Pdf.Markdown` (the Markdig-based extension package) publishes cleanly as Native AOT too, with the same zero-warning trim/AOT analyzer results.
