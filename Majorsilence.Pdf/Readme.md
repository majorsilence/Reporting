# Majorsilence.Pdf

Majorsilence.Pdf is tri licensed under Apache-2.0, MIT, or BSD-3-Clause. Pick your choice.

- SPDX-License-Identifier: MIT OR Apache-2.0 OR BSD-3-Clause
- Copyright (C) 2026 Peter Gill <peter@majorsilence.com>

---

A pure-managed PDF generation library for .NET. No native dependencies. Produces PDF 1.4, PDF 1.7, and PDF 2.0 output with optional PDF/A conformance.

## Quick Start

```csharp
using Majorsilence.Pdf;

PdfDocument.Create()
    .WithTitle("My Report")
    .AddPage(PageSizes.A4, canvas =>
    {
        canvas.DrawText("Hello, World!", 72, 72, TextStyle.Default.WithSize(24));
        canvas.DrawRectangle(72, 120, 200, 60,
            ShapeStyle.Filled(PdfColor.LightGray).WithBorder(PdfColor.Black, 1f));
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
    .WithColor(PdfColor.DarkBlue)
    .WithAlignment(TextAlignment.Center)
```

**`ShapeStyle`** — fill, border, and opacity:
```csharp
ShapeStyle.Filled(PdfColor.LightBlue)
    .WithBorder(PdfColor.Navy, 1.5f)
    .WithFillOpacity(0.5f)   // 50% transparent fill
    .WithStrokeOpacity(0.8f)
```

**`StrokeStyle`** — line style and opacity:
```csharp
StrokeStyle.Solid(PdfColor.Red, 2f)
    .WithDash(4, 2)
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
    .WithHeaderBackground(PdfColor.SteelBlue)
    .WithAlternateRowBackground(new PdfColor(240, 240, 240))
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

Register TrueType fonts and fall back to the built-in font set:

```csharp
var registry = FontRegistry.CreateDefault()
    .WithFont("NotoSans", "/path/to/NotoSans-Regular.ttf")
    .WithFont("NotoSans", "/path/to/NotoSans-Bold.ttf", bold: true);

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
