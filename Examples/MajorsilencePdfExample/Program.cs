// Majorsilence.Pdf — example program
// Demonstrates the key features of the library using the FontRegistry with
// bundled Liberation / Caladea / Carlito / NotoSans TrueType fonts.
// Each example is written twice: once as PDF 1.4 and once as PDF 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;

string outputDir = Path.Combine(AppContext.BaseDirectory, "output");
Directory.CreateDirectory(outputDir);

// ── shared font registry ──────────────────────────────────────────────────────
// Scan the bundled fonts that ship with Majorsilence.Drawing.Common.
// NotoSans is the fallback for any glyph the primary font is missing.
string fontsDir = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "Majorsilence.Drawing.Common", "Fonts"));

FontRegistry registry;
if (Directory.Exists(fontsDir))
{
    registry = new FontRegistry()
        .AddDirectory(fontsDir)
        .AddFallback("NotoSans");
    Console.WriteLine($"Fonts loaded from: {fontsDir}");
}
else
{
    registry = new FontRegistry(); // empty — examples will SKIPPED gracefully
    Console.WriteLine($"WARNING: bundled fonts not found at {fontsDir}");
}

// ── run examples ──────────────────────────────────────────────────────────────

RunExample("01_hello_world",        HelloWorld);
RunExample("02_text_styles",        TextStyles);
RunExample("03_shapes",             Shapes);
RunExample("04_lines_and_strokes",  LinesAndStrokes);
RunExample("05_multipage",          MultiPage);
RunExample("06_custom_font",        CustomFont);
RunExample("07_image",              ImageExample);
RunExample("08_annotations",        Annotations);
RunExample("09_invoice",            InvoiceExample);
RunExample("10_dashboard",          DashboardExample);
RunExample("11_font_registry",      FontRegistryExample);
RunExample("12_unicode",            UnicodeExample);
RunExample("13_password_protected", PasswordProtectedExample);
RunExample("14_signed",            SignedExample);
RunExample("15_rtl_text",          RightToLeftExample);
RunExample("16_pdf_merge",         PdfMergeExample);
RunExample("17_images_from_disk",  ImagesFromDiskExample);
RunExample("18_opacity",           OpacityExample);
RunExample("19_text_wrapping",     TextWrappingExample);
RunExample("20_table",             TableExample);
RunExample("21_pdfa",              PdfAExample);
RunExample("22_visible_signature", VisibleSignatureExample);
RunExample("23_pubkey_encryption", PublicKeyEncryptionExample);

Console.WriteLine($"\nAll PDFs written to: {outputDir}");

// ── runner ────────────────────────────────────────────────────────────────────

// Runs each example once for PDF 1.4 and once for PDF 2.0.
void RunExample(string name, Action<string, FontRegistry, PdfVersion> example)
{
    foreach (var version in new[] { PdfVersion.Pdf14, PdfVersion.Pdf20 })
    {
        string label = version == PdfVersion.Pdf20 ? "v2.0" : "v1.4";
        Console.Write($"  {name}_{label} ... ");
        try   { example(name, registry, version); Console.WriteLine("OK"); }
        catch (Exception ex) { Console.WriteLine($"SKIPPED ({ex.Message})"); }
    }
}

// ── example 01: hello world ──────────────────────────────────────────────────

static void HelloWorld(string name, FontRegistry fonts, PdfVersion version)
{
    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Hello World")
        .WithAuthor("Majorsilence.Pdf")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(36).WithBold();
            var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);

            canvas.DrawText("Hello, World!", 72, 100, heading);
            canvas.DrawText(
                "Generated with Majorsilence.Pdf — a zero-dependency PDF library.",
                72, 155, body);
        })
        .Save(Out(name, version));
}

// ── example 02: text styles ──────────────────────────────────────────────────

static void TextStyles(string name, FontRegistry fonts, PdfVersion version)
{
    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Text Style Showcase")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;
            const float step = 28;

            void Row(string label, TextStyle style)
            {
                canvas.DrawText(label, 72, y, style);
                y += step;
            }

            Row("LiberationSans 14 pt (default)",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14));
            Row("LiberationSans Bold",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithBold());
            Row("LiberationSans Italic",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithItalic());
            Row("LiberationSans Bold Italic",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithBold().WithItalic());
            Row("LiberationSerif 14 pt",
                TextStyle.Default.WithFamily("LiberationSerif").WithSize(14));
            Row("LiberationMono 12 pt",
                TextStyle.Default.WithFamily("LiberationMono").WithSize(12));
            Row("Caladea 14 pt",
                TextStyle.Default.WithFamily("Caladea").WithSize(14));
            Row("Carlito 14 pt",
                TextStyle.Default.WithFamily("Carlito").WithSize(14));
            Row("NotoSans 14 pt",
                TextStyle.Default.WithFamily("NotoSans").WithSize(14));
            Row("Red 16 pt",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(16).WithColor(PdfColor.Red));
            Row("Blue 14 pt",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithColor(PdfColor.Blue));
            Row("Underlined text",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithUnderline());
            Row("Strikethrough text",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithStrikethrough());
            Row("Overline text",
                TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithOverline());

            canvas.DrawText("Display — 48 pt Bold", 72, y + 20,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(48).WithBold()
                    .WithColor(PdfColor.DarkGray));
        })
        .Save(Out(name, version));
}

// ── example 03: shapes ───────────────────────────────────────────────────────

static void Shapes(string name, FontRegistry fonts, PdfVersion version)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Shape Showcase")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawRectangle(50, 50, 150, 80, ShapeStyle.Filled(PdfColor.LightGray));
            canvas.DrawText("Filled rect", 60, 100, label);

            canvas.DrawRectangle(230, 50, 150, 80, ShapeStyle.Stroked(PdfColor.Black, 2f));
            canvas.DrawText("Stroked rect", 240, 100, label);

            canvas.DrawRectangle(410, 50, 150, 80,
                ShapeStyle.Filled(PdfColor.Yellow).WithStroke(PdfColor.Orange, 2f));
            canvas.DrawText("Fill + stroke", 420, 100, label);

            canvas.DrawRectangle(50, 175, 150, 80,
                ShapeStyle.Stroked(PdfColor.DarkGray, 1.5f).Dashed());
            canvas.DrawText("Dashed border", 60, 225, label);

            canvas.DrawRectangle(230, 175, 150, 80,
                ShapeStyle.Stroked(PdfColor.DarkGray, 1.5f).Dotted());
            canvas.DrawText("Dotted border", 240, 225, label);

            canvas.DrawEllipse(50, 300, 150, 80, ShapeStyle.Filled(PdfColor.Blue));
            canvas.DrawText("Filled ellipse", 60, 350, label);

            canvas.DrawEllipse(230, 300, 150, 80, ShapeStyle.Stroked(PdfColor.Red, 2f));
            canvas.DrawText("Stroked ellipse", 240, 350, label);

            canvas.DrawEllipse(410, 300, 80, 80,
                ShapeStyle.Filled(PdfColor.Green).WithStroke(PdfColor.DarkGray, 1f));
            canvas.DrawText("Circle", 435, 365, label);

            canvas.DrawPolygon(
                new List<(float, float)> { (50, 450), (200, 450), (125, 370) },
                ShapeStyle.Filled(PdfColor.Orange).WithStroke(PdfColor.DarkGray));
            canvas.DrawText("Triangle", 90, 470, label);

            canvas.DrawPolygon(Pentagon(310, 410, 60),
                ShapeStyle.Filled(PdfColor.Red).WithStroke(PdfColor.DarkGray));
            canvas.DrawText("Pentagon", 285, 490, label);

            canvas.DrawPolygon(Hexagon(480, 415, 55),
                ShapeStyle.Filled(PdfColor.Blue).WithStroke(PdfColor.DarkGray));
            canvas.DrawText("Hexagon", 455, 490, label);

            canvas.DrawCurve(
                new List<(float, float)>
                    { (50,550),(120,510),(200,580),(280,520),(360,570),(440,530),(520,560) },
                StrokeStyle.Default.WithWidth(2).WithColor(PdfColor.Blue));
            canvas.DrawText("Smooth curve (Catmull-Rom spline)", 50, 590, label);

            canvas.DrawCurve(
                new List<(float, float)>
                    { (50,640),(150,610),(250,650),(350,615),(450,645) },
                StrokeStyle.Default.WithWidth(1.5f).WithColor(PdfColor.Red).Dashed());
            canvas.DrawText("Dashed curve", 50, 665, label);
        })
        .Save(Out(name, version));
}

// ── example 04: lines and strokes ────────────────────────────────────────────

static void LinesAndStrokes(string name, FontRegistry fonts, PdfVersion version)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Lines and Strokes")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;
            const float step = 36;
            const float x1 = 100, x2 = 480;

            void Line(string lbl, StrokeStyle style)
            {
                canvas.DrawText(lbl, 50, y - 4, label);
                canvas.DrawLine(x1, y, x2, y, style);
                y += step;
            }

            Line("0.5 pt solid",    StrokeStyle.Default.WithWidth(0.5f));
            Line("1 pt solid",      StrokeStyle.Default.WithWidth(1f));
            Line("2 pt solid",      StrokeStyle.Default.WithWidth(2f));
            Line("4 pt solid",      StrokeStyle.Default.WithWidth(4f));
            Line("8 pt solid",      StrokeStyle.Default.WithWidth(8f));
            Line("1 pt dashed",     StrokeStyle.Default.WithWidth(1f).Dashed());
            Line("2 pt dashed",     StrokeStyle.Default.WithWidth(2f).Dashed());
            Line("1 pt dotted",     StrokeStyle.Default.WithWidth(1f).Dotted());
            Line("2 pt dotted",     StrokeStyle.Default.WithWidth(2f).Dotted());
            Line("Red 2 pt",        StrokeStyle.Default.WithWidth(2f).WithColor(PdfColor.Red));
            Line("Blue 2 pt",       StrokeStyle.Default.WithWidth(2f).WithColor(PdfColor.Blue));
            Line("Green dashed",    StrokeStyle.Default.WithWidth(2f).WithColor(PdfColor.Green).Dashed());
            Line("Orange dotted",   StrokeStyle.Default.WithWidth(2f).WithColor(PdfColor.Orange).Dotted());

            canvas.DrawText("Diagonal lines:", 50, y, label);
            y += 20;
            for (int i = 0; i < 8; i++)
            {
                float startX = 100 + i * 50;
                canvas.DrawLine(startX, y, startX + 40, y + 60,
                    StrokeStyle.Default.WithWidth(1 + i * 0.5f)
                               .WithColor(PdfColor.FromHex($"#{(i * 30):X2}6080")));
            }
        })
        .Save(Out(name, version));
}

// ── example 05: multi-page document ─────────────────────────────────────────

static void MultiPage(string name, FontRegistry fonts, PdfVersion version)
{
    var doc = PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Multi-Page Document")
        .WithAuthor("Example Author")
        .WithFontRegistry(fonts);

    string[] chapters = { "Introduction", "Methods", "Results", "Discussion", "Conclusion" };

    for (int i = 0; i < chapters.Length; i++)
    {
        int pageNum = i + 1;
        string chapter = chapters[i];

        doc.AddPage(PageSizes.A4, canvas =>
        {
            var hdrText  = TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithColor(PdfColor.White);
            var titleTxt = TextStyle.Default.WithFamily("LiberationSans").WithSize(24).WithBold();
            var bodyTxt  = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
            var footerTxt= TextStyle.Default.WithFamily("LiberationSans").WithSize(9).WithColor(PdfColor.Gray);

            canvas.DrawRectangle(0, 0, PageSizes.A4.Width, 36, ShapeStyle.Filled(PdfColor.DarkGray));
            canvas.DrawText("Multi-Page Example Document", 20, 24, hdrText);

            canvas.DrawText($"Chapter {pageNum}: {chapter}", 72, 80, titleTxt);
            canvas.DrawLine(72, 105, PageSizes.A4.Width - 72, 105,
                StrokeStyle.Default.WithWidth(0.5f).WithColor(PdfColor.Gray));

            float y = 125;
            for (int line = 0; line < 12; line++)
            {
                canvas.DrawText(
                    $"Line {line + 1} of body text for {chapter}. " +
                     "The quick brown fox jumps over the lazy dog.",
                    72, y, bodyTxt);
                y += 18;
            }

            canvas.DrawLine(72, PageSizes.A4.Height - 40,
                PageSizes.A4.Width - 72, PageSizes.A4.Height - 40,
                StrokeStyle.Default.WithWidth(0.5f).WithColor(PdfColor.LightGray));
            canvas.DrawText($"Page {pageNum} of {chapters.Length}",
                PageSizes.A4.Width / 2 - 30, PageSizes.A4.Height - 25, footerTxt);
        });
    }

    doc.Save(Out(name, version));
}

// ── example 06: custom font file (WithFontFile) ──────────────────────────────
// Demonstrates embedding a user-supplied TTF directly via TextStyle.WithFontFile().
// Falls back to a system font for portability.

static void CustomFont(string name, FontRegistry fonts, PdfVersion version)
{
    // Look for a font the user might have; system fonts are fine for demonstration.
    string? customPath =
        FindFont(
            @"C:\Windows\Fonts\georgia.ttf",
            @"C:\Windows\Fonts\trebuc.ttf",
            @"C:\Windows\Fonts\verdana.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf",
            "/Library/Fonts/Georgia.ttf");

    if (customPath == null)
        throw new FileNotFoundException("No custom font file found for this example");

    // WithFontFile bypasses the registry entirely — the file is embedded directly.
    var custom = TextStyle.Default.WithFontFile(customPath).WithSize(14);
    var reg    = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Custom Font File")
        .WithFontRegistry(fonts)   // registry still used for reg/bold/italic body styles
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Custom font via WithFontFile()", 72, 60,
                custom.WithSize(20).WithBold());
            canvas.DrawText($"File: {Path.GetFileName(customPath)}", 72, 90,
                custom.WithSize(11).WithItalic());
            canvas.DrawLine(72, 106, 520, 106, StrokeStyle.Default.WithWidth(0.5f));

            float y = 126;
            canvas.DrawText("Regular — The quick brown fox jumps over the lazy dog.", 72, y, custom);
            y += 24;
            canvas.DrawText("Bold    — The quick brown fox jumps over the lazy dog.", 72, y,
                custom.WithBold());
            y += 24;
            canvas.DrawText("Italic  — The quick brown fox jumps over the lazy dog.", 72, y,
                custom.WithItalic());
            y += 40;

            canvas.DrawText("Size ramp:", 72, y, reg);
            y += 20;
            foreach (int sz in new[] { 8, 10, 12, 14, 18, 24, 36 })
            {
                canvas.DrawText($"{sz}pt — The quick brown fox.", 72, y,
                    custom.WithSize(sz));
                y += sz + 6;
            }
        })
        .Save(Out(name, version));
}

// ── example 07: image ────────────────────────────────────────────────────────

static void ImageExample(string name, FontRegistry fonts, PdfVersion version)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);

    const int W = 200, H = 150;
    var rgb = new byte[W * H * 3];
    for (int row = 0; row < H; row++)
        for (int col = 0; col < W; col++)
        {
            int idx = (row * W + col) * 3;
            rgb[idx]     = (byte)(col * 255 / W);
            rgb[idx + 1] = (byte)(row * 255 / H);
            rgb[idx + 2] = 128;
        }

    const int CW = 100, CH = 100;
    var checker = new byte[CW * CH * 3];
    for (int row = 0; row < CH; row++)
        for (int col = 0; col < CW; col++)
        {
            int idx = (row * CW + col) * 3;
            byte v = ((row / 10) + (col / 10)) % 2 == 0 ? (byte)30 : (byte)220;
            checker[idx] = checker[idx + 1] = checker[idx + 2] = v;
        }

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Image Example")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Images embedded in PDF", 72, 50,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold());

            canvas.DrawText("Gradient (200x150 raw RGB, FlateDecode compressed):", 72, 80, label);
            canvas.DrawImage(rgb, W, H, isJpeg: false, x: 72, y: 95, width: 200, height: 150);

            canvas.DrawText("Checkerboard (100x100 raw RGB):", 300, 80, label);
            canvas.DrawImage(checker, CW, CH, isJpeg: false, x: 300, y: 95, width: 150, height: 150);

            canvas.DrawText("Same gradient at different scales:", 72, 265, label);
            canvas.DrawImage(rgb, W, H, isJpeg: false, x: 72,  y: 285, width: 60,  height: 45);
            canvas.DrawImage(rgb, W, H, isJpeg: false, x: 145, y: 285, width: 120, height: 90);
            canvas.DrawImage(rgb, W, H, isJpeg: false, x: 280, y: 285, width: 240, height: 180);
        })
        .Save(Out(name, version));
}

// ── example 08: annotations ──────────────────────────────────────────────────

static void Annotations(string name, FontRegistry fonts, PdfVersion version)
{
    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Annotations Example")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(20).WithBold();
            var link    = TextStyle.Default.WithFamily("LiberationSans").WithSize(14)
                              .WithColor(PdfColor.Blue).WithUnderline();
            var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);
            var small   = TextStyle.Default.WithFamily("LiberationSans").WithSize(10)
                              .WithColor(PdfColor.Gray).WithItalic();

            canvas.DrawText("Annotations", 72, 60, heading);

            canvas.DrawText("» Click here to visit majorsilence.com", 72, 110, link);
            canvas.AddLink(72, 96, 330, 20, "https://majorsilence.com");

            canvas.DrawText("» GitHub repository", 72, 145, link);
            canvas.AddLink(72, 131, 200, 20, "https://github.com/majorsilence");

            canvas.DrawRectangle(72, 190, 200, 40,
                ShapeStyle.Filled(PdfColor.LightGray).WithStroke(PdfColor.Gray));
            canvas.DrawText("Hover for tooltip", 82, 215, body);
            canvas.AddTooltip(72, 190, 200, 40,
                "This is a tooltip annotation. It appears when you hover over the box.");

            canvas.DrawRectangle(72, 260, 300, 50,
                ShapeStyle.Filled(PdfColor.FromHex("#E8F4FF")).WithStroke(PdfColor.Blue));
            canvas.DrawText("[PDF] Download specification", 82, 290,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(13).WithColor(PdfColor.Blue));
            canvas.AddLink(72, 260, 300, 50, "https://example.com/spec.pdf");

            canvas.DrawText("Note: open this PDF in a viewer that supports annotations", 72, 350, small);
        })
        .Save(Out(name, version));
}

// ── example 09: invoice ──────────────────────────────────────────────────────

static void InvoiceExample(string name, FontRegistry fonts, PdfVersion version)
{
    float pw = PageSizes.Letter.Width;
    float ph = PageSizes.Letter.Height;
    const float margin = 54f;
    var accent = PdfColor.FromHex("#1A56A0");

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Invoice #INV-2025-0042")
        .WithAuthor("Majorsilence Corp")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.Letter, canvas =>
        {
            var sans   = "LiberationSans";
            var bold   = TextStyle.Default.WithFamily(sans).WithBold().WithSize(11);
            var normal = TextStyle.Default.WithFamily(sans).WithSize(11);
            var small  = TextStyle.Default.WithFamily(sans).WithSize(9).WithColor(PdfColor.Gray);

            // Header bar
            canvas.DrawRectangle(0, 0, pw, 80, ShapeStyle.Filled(accent));
            canvas.DrawText("INVOICE", margin, 52,
                TextStyle.Default.WithFamily(sans).WithSize(32).WithBold().WithColor(PdfColor.White));
            canvas.DrawText("Majorsilence Corp  ·  123 Main Street  ·  Anytown, ST 00000",
                pw - margin - 280, 30,
                TextStyle.Default.WithFamily(sans).WithSize(9).WithColor(PdfColor.White));
            canvas.DrawText("hello@majorsilence.com  ·  +1 555-000-1234",
                pw - margin - 250, 48,
                TextStyle.Default.WithFamily(sans).WithSize(9).WithColor(PdfColor.White));

            // Invoice details
            float y = 105;
            canvas.DrawText("Invoice #:", margin, y, bold);
            canvas.DrawText("INV-2025-0042", margin + 80, y, normal);
            canvas.DrawText("Date:", pw / 2, y, bold);
            canvas.DrawText("June 17, 2025", pw / 2 + 50, y, normal);
            y += 18;
            canvas.DrawText("Due Date:", margin, y, bold);
            canvas.DrawText("July 17, 2025", margin + 80, y, normal);
            canvas.DrawText("Status:", pw / 2, y, bold);
            canvas.DrawText("Unpaid", pw / 2 + 50, y, normal.WithColor(PdfColor.Red).WithBold());

            // Bill to
            y += 36;
            canvas.DrawText("Bill To:", margin, y, bold.WithColor(accent));
            y += 16;
            canvas.DrawText("Acme Industries Ltd.", margin, y, bold);
            y += 15; canvas.DrawText("456 Corporate Blvd", margin, y, normal);
            y += 15; canvas.DrawText("Business City, BC  V1A 2B3", margin, y, normal);
            y += 15; canvas.DrawText("accounts@acme.example.com", margin, y,
                normal.WithColor(accent).WithUnderline());
            canvas.AddLink(margin, y - 4, 200, 14, "mailto:accounts@acme.example.com");

            // Table header
            y += 36;
            canvas.DrawRectangle(margin, y, pw - margin * 2, 22, ShapeStyle.Filled(accent));
            float[] cols = { margin + 4, margin + 200, margin + 310, margin + 380, pw - margin - 4 };
            var hdr = TextStyle.Default.WithFamily(sans).WithSize(10).WithBold().WithColor(PdfColor.White);
            canvas.DrawText("Description", cols[0], y + 15, hdr);
            canvas.DrawText("Qty",        cols[1], y + 15, hdr.WithAlignment(TextAlignment.Right));
            canvas.DrawText("Unit Price", cols[2], y + 15, hdr.WithAlignment(TextAlignment.Right));
            canvas.DrawText("Tax",        cols[3], y + 15, hdr.WithAlignment(TextAlignment.Right));
            canvas.DrawText("Amount",     cols[4], y + 15, hdr.WithAlignment(TextAlignment.Right));
            y += 22;

            // Line items
            var items = new[]
            {
                ("PDF Library License (1 year)",  1, 1200m, 0.10m),
                ("Custom Report Templates (x5)",   5,  250m, 0.10m),
                ("Integration Support (hours)",    8,  150m, 0.00m),
                ("Documentation Pack",             1,  350m, 0.10m),
            };

            decimal subtotal = 0, taxTotal = 0;
            bool alt = false;
            foreach (var (desc, qty, price, taxRate) in items)
            {
                decimal lineTotal = qty * price;
                decimal lineTax   = lineTotal * taxRate;
                subtotal  += lineTotal;
                taxTotal  += lineTax;

                if (alt)
                    canvas.DrawRectangle(margin, y, pw - margin * 2, 18,
                        ShapeStyle.Filled(PdfColor.FromHex("#F3F6FA")));
                alt = !alt;

                var row = TextStyle.Default.WithFamily(sans).WithSize(10);
                canvas.DrawText(desc, cols[0], y + 13, row);
                canvas.DrawText(qty.ToString(), cols[1], y + 13, row.WithAlignment(TextAlignment.Right));
                canvas.DrawText(price.ToString("C"), cols[2], y + 13, row.WithAlignment(TextAlignment.Right));
                canvas.DrawText(taxRate == 0 ? "—" : (taxRate * 100).ToString("0") + "%",
                    cols[3], y + 13, row.WithAlignment(TextAlignment.Right));
                canvas.DrawText((lineTotal + lineTax).ToString("C"),
                    cols[4], y + 13, row.WithAlignment(TextAlignment.Right));
                y += 18;
            }

            // Totals
            canvas.DrawLine(margin, y + 4, pw - margin, y + 4,
                StrokeStyle.Default.WithWidth(0.5f).WithColor(PdfColor.LightGray));
            y += 14;

            void TotalRow(string lbl, string value, bool isBold = false)
            {
                var s = isBold ? bold : normal;
                canvas.DrawText(lbl, cols[3] - 80, y, s);
                canvas.DrawText(value, cols[4], y, s.WithAlignment(TextAlignment.Right));
                y += 16;
            }

            TotalRow("Subtotal:", subtotal.ToString("C"));
            TotalRow("Tax:", taxTotal.ToString("C"));
            canvas.DrawLine(cols[3] - 80, y, pw - margin, y,
                StrokeStyle.Default.WithWidth(1).WithColor(accent));
            y += 14;
            TotalRow("Total Due:", (subtotal + taxTotal).ToString("C"), isBold: true);

            // Payment info
            y += 20;
            canvas.DrawRectangle(margin, y, pw - margin * 2, 70,
                ShapeStyle.Filled(PdfColor.FromHex("#F0F4F8")).WithStroke(PdfColor.LightGray));
            canvas.DrawText("Payment Instructions", margin + 8, y + 16, bold.WithColor(accent));
            canvas.DrawText(
                "Bank Transfer: Anybank · Routing 021000021 · Account 1234567890",
                margin + 8, y + 33, TextStyle.Default.WithFamily(sans).WithSize(10));
            canvas.DrawText("Please reference invoice #INV-2025-0042 in your transfer.",
                margin + 8, y + 49, TextStyle.Default.WithFamily(sans).WithSize(10));
            canvas.DrawText("Thank you for your business!", margin + 8, y + 62,
                TextStyle.Default.WithFamily(sans).WithSize(10).WithItalic().WithColor(PdfColor.Gray));

            // Footer
            float fy = ph - 30;
            canvas.DrawLine(margin, fy - 10, pw - margin, fy - 10,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            canvas.DrawText(
                "Majorsilence Corp  ·  majorsilence.com  ·  Generated by Majorsilence.Pdf",
                pw / 2 - 160, fy, small);
        })
        .Save(Out(name, version));
}

// ── example 10: dashboard / data visualization ───────────────────────────────

static void DashboardExample(string name, FontRegistry fonts, PdfVersion version)
{
    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Sales Dashboard")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.Letter.Landscape(), canvas =>
        {
            float pw = PageSizes.Letter.Height;   // landscape swap
            float ph = PageSizes.Letter.Width;
            const float m = 36f;
            var sans   = "LiberationSans";
            var accent = PdfColor.FromHex("#2563EB");

            // Title bar
            canvas.DrawRectangle(0, 0, pw, 50, ShapeStyle.Filled(accent));
            canvas.DrawText("Sales Dashboard — Q2 2025", m, 33,
                TextStyle.Default.WithFamily(sans).WithSize(18).WithBold().WithColor(PdfColor.White));
            canvas.DrawText("Majorsilence.Pdf example", pw - 200, 33,
                TextStyle.Default.WithFamily(sans).WithSize(10).WithColor(PdfColor.White));

            // KPI cards
            var kpis = new[] {
                ("Total Revenue",  "$1.24M", "+12%",  true),
                ("New Customers",  "847",    "+8%",   true),
                ("Avg Order",      "$1,463", "+3%",   true),
                ("Churn Rate",     "2.1%",   "-0.4%", false),
            };

            float cardW = (pw - m * 2 - 24) / 4;
            float cx = m;
            foreach (var (lbl, value, change, positive) in kpis)
            {
                canvas.DrawRectangle(cx, 62, cardW, 68,
                    ShapeStyle.Filled(PdfColor.White).WithStroke(PdfColor.LightGray));
                canvas.DrawText(lbl, cx + 8, 82,
                    TextStyle.Default.WithFamily(sans).WithSize(9).WithColor(PdfColor.Gray));
                canvas.DrawText(value, cx + 8, 107,
                    TextStyle.Default.WithFamily(sans).WithSize(20).WithBold().WithColor(PdfColor.DarkGray));
                canvas.DrawText(change, cx + 8, 122,
                    TextStyle.Default.WithFamily(sans).WithSize(9)
                        .WithColor(positive ? PdfColor.Green : PdfColor.Red));
                cx += cardW + 8;
            }

            // Bar chart
            float chartX = m, chartY = 148, chartW = pw * 0.55f - m - 10;
            float chartH = ph - chartY - m - 20;
            canvas.DrawRectangle(chartX, chartY, chartW, chartH + 20,
                ShapeStyle.Filled(PdfColor.White).WithStroke(PdfColor.LightGray));
            canvas.DrawText("Monthly Revenue ($K)", chartX + 8, chartY + 16,
                TextStyle.Default.WithFamily(sans).WithSize(11).WithBold());

            decimal[] monthly = { 180, 210, 195, 230, 270, 245 };
            string[] months   = { "Jan","Feb","Mar","Apr","May","Jun" };
            decimal maxVal    = 300;
            float barAreaX = chartX + 40, barAreaY = chartY + 30;
            float barAreaW = chartW - 50, barAreaH = chartH - 20;
            float barW = barAreaW / monthly.Length * 0.6f;
            float gap  = barAreaW / monthly.Length;

            for (int g = 0; g <= 3; g++)
            {
                float gy = barAreaY + barAreaH - barAreaH * g / 3;
                canvas.DrawLine(barAreaX, gy, barAreaX + barAreaW, gy,
                    StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
                canvas.DrawText(((int)(maxVal * g / 3)).ToString(),
                    chartX + 4, gy + 4,
                    TextStyle.Default.WithFamily(sans).WithSize(7).WithColor(PdfColor.Gray));
            }

            for (int i = 0; i < monthly.Length; i++)
            {
                float bx = barAreaX + i * gap + (gap - barW) / 2;
                float bh = barAreaH * (float)monthly[i] / (float)maxVal;
                float by = barAreaY + barAreaH - bh;
                canvas.DrawRectangle(bx, by, barW, bh, ShapeStyle.Filled(accent));
                canvas.DrawText(months[i], bx + barW / 2 - 6, barAreaY + barAreaH + 12,
                    TextStyle.Default.WithFamily(sans).WithSize(8));
                canvas.DrawText(monthly[i].ToString(), bx + barW / 2 - 8, by - 8,
                    TextStyle.Default.WithFamily(sans).WithSize(7));
            }

            // Pie chart
            float pieX = pw * 0.55f + 10, pieY = chartY;
            float pieW = pw - pieX - m, pieH = chartH + 20;
            canvas.DrawRectangle(pieX, pieY, pieW, pieH,
                ShapeStyle.Filled(PdfColor.White).WithStroke(PdfColor.LightGray));
            canvas.DrawText("Revenue by Region", pieX + 8, pieY + 16,
                TextStyle.Default.WithFamily(sans).WithSize(11).WithBold());

            var regions = new[] {
                ("North America", 0.42f, PdfColor.Blue),
                ("Europe",        0.28f, PdfColor.FromHex("#10B981")),
                ("Asia Pacific",  0.18f, PdfColor.Orange),
                ("Other",         0.12f, PdfColor.FromHex("#8B5CF6")),
            };

            float pcx = pieX + pieW / 2, pcy = pieY + pieH * 0.5f + 10, pr = 70;
            float angle = -90f;
            foreach (var (_, share, color) in regions)
            {
                DrawPieSlice(canvas, pcx, pcy, pr, angle, share * 360f, color);
                angle += share * 360f;
            }

            float ly = pieY + 30;
            foreach (var (rLabel, share, color) in regions)
            {
                canvas.DrawRectangle(pieX + 8, ly, 10, 10, ShapeStyle.Filled(color));
                canvas.DrawText($"{rLabel}: {share * 100:F0}%", pieX + 22, ly + 10,
                    TextStyle.Default.WithFamily(sans).WithSize(9));
                ly += 16;
            }

            // Data table
            float tableY = ph - m - 90;
            canvas.DrawRectangle(m, tableY - 2, pw - m * 2, 94,
                ShapeStyle.Filled(PdfColor.White).WithStroke(PdfColor.LightGray));
            canvas.DrawText("Top Products", m + 8, tableY + 12,
                TextStyle.Default.WithFamily(sans).WithSize(11).WithBold());

            float[] tcols = { m + 8, m + 160, m + 280, m + 370, m + 460 };
            var thdr = TextStyle.Default.WithFamily(sans).WithSize(9).WithBold().WithColor(PdfColor.Gray);
            canvas.DrawText("Product",    tcols[0], tableY + 28, thdr);
            canvas.DrawText("Category",   tcols[1], tableY + 28, thdr);
            canvas.DrawText("Units Sold", tcols[2], tableY + 28, thdr);
            canvas.DrawText("Revenue",    tcols[3], tableY + 28, thdr);
            canvas.DrawText("Growth",     tcols[4], tableY + 28, thdr);
            canvas.DrawLine(m + 8, tableY + 32, pw - m - 8, tableY + 32,
                StrokeStyle.Default.WithWidth(0.5f).WithColor(PdfColor.LightGray));

            var products = new[] {
                ("PDF Library Pro",   "Software", "2,341", "$1.12M", "+18%"),
                ("Report Designer",   "Software", "1,876", "$0.89M", "+12%"),
                ("Integration Pack",  "Services", "934",   "$0.56M", "+7%"),
                ("Support Contracts", "Services", "1,203", "$0.48M", "+5%"),
            };
            float ty = tableY + 48;
            foreach (var (prod, cat, units, rev, growth) in products)
            {
                var tr = TextStyle.Default.WithFamily(sans).WithSize(9);
                canvas.DrawText(prod,   tcols[0], ty, tr);
                canvas.DrawText(cat,    tcols[1], ty, tr);
                canvas.DrawText(units,  tcols[2], ty, tr);
                canvas.DrawText(rev,    tcols[3], ty, tr);
                canvas.DrawText(growth, tcols[4], ty, tr.WithColor(PdfColor.Green));
                ty += 14;
            }
        })
        .Save(Out(name, version));
}

// ── example 11: font registry showcase ───────────────────────────────────────

static void FontRegistryExample(string name, FontRegistry fonts, PdfVersion version)
{
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);
    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(20).WithBold();

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Font Registry Showcase")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;
            const float step = 22;

            canvas.DrawText("Font Registry", 72, y, heading);
            y += 34;

            // One row per registered family
            var families = new[]
            {
                ("LiberationSans",  "sans-serif, metric-compatible with Arial"),
                ("LiberationSerif", "serif, metric-compatible with Times New Roman"),
                ("LiberationMono",  "monospaced, metric-compatible with Courier New"),
                ("Caladea",         "serif, metric-compatible with Cambria"),
                ("Carlito",         "sans-serif, metric-compatible with Calibri"),
                ("NotoSans",        "sans-serif, broad Unicode coverage"),
            };

            foreach (var (family, desc) in families)
            {
                canvas.DrawText($"{family}  —  {desc}", 72, y,
                    TextStyle.Default.WithFamily(family).WithSize(12));
                y += step;
                canvas.DrawText(
                    "    Regular  |  ",
                    72, y, TextStyle.Default.WithFamily(family).WithSize(11));
                float x2 = 72 + canvas.MeasureTextWidth("    Regular  |  ",
                    TextStyle.Default.WithFamily(family).WithSize(11));
                canvas.DrawText("Bold  |  ", x2, y,
                    TextStyle.Default.WithFamily(family).WithSize(11).WithBold());
                float x3 = x2 + canvas.MeasureTextWidth("Bold  |  ",
                    TextStyle.Default.WithFamily(family).WithSize(11).WithBold());
                canvas.DrawText("Italic", x3, y,
                    TextStyle.Default.WithFamily(family).WithSize(11).WithItalic());
                y += step * 1.6f;
            }

            // Fallback demo
            canvas.DrawLine(72, y, PageSizes.A4.Width - 72, y,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            y += 14;
            canvas.DrawText("Fallback demo (LiberationSans primary, NotoSans fallback):", 72, y, body);
            y += step;
            canvas.DrawText("  Latin + accented: café résumé naïve", 72, y, body);
            y += step;
            canvas.DrawText("  Typographic: — – ‘ ’ “ ” • …", 72, y, body);
            y += step;

            // Measurement
            string sample = "Hello, World!";
            float w = canvas.MeasureTextWidth(sample, body);
            canvas.DrawText($"MeasureTextWidth(\"{sample}\", LiberationSans 12pt) = {w:F1} pt",
                72, y, body.WithColor(PdfColor.DarkGray));
        })
        .Save(Out(name, version));
}

// ── example 12: unicode / utf-8 text ─────────────────────────────────────────
// Demonstrates rendering of Latin Extended, Greek, Cyrillic, Emoji, and CJK text.
// Characters not supported by a font render as .notdef boxes — no exception is thrown.

static void UnicodeExample(string name, FontRegistry fonts, PdfVersion version)
{
    // Optional system fonts for emoji and CJK
    string? emojiFont = FindFont(
        @"C:\Windows\Fonts\seguiemj.ttf",
        "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf",
        "/usr/share/fonts/truetype/noto/NotoEmoji-Regular.ttf");

    string? cjkFont = FindFont(
        @"C:\Windows\Fonts\malgun.ttf",
        @"C:\Windows\Fonts\Deng.ttf",
        "/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf");

    // Extend the shared registry with optional system fonts
    FontRegistry reg = fonts;
    if (emojiFont != null)
        reg = new FontRegistry().AddDirectory(
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "Majorsilence.Drawing.Common", "Fonts")))
            .AddFamily("Emoji", regular: emojiFont)
            .AddFallback("NotoSans")
            .AddFallback("Emoji");

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Unicode / UTF-8 Text Showcase")
        .WithFontRegistry(reg)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 50;
            const float gap = 20f;

            var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(16).WithBold();
            var body    = TextStyle.Default.WithFamily("NotoSans").WithSize(12);
            var small   = TextStyle.Default.WithFamily("NotoSans").WithSize(10)
                              .WithColor(PdfColor.Gray).WithItalic();
            var mono    = TextStyle.Default.WithFamily("LiberationMono").WithSize(11);

            void Section(string title)
            {
                y += 8;
                canvas.DrawLine(72, y, PageSizes.A4.Width - 72, y,
                    StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
                y += 14;
                canvas.DrawText(title, 72, y, heading);
                y += gap + 2;
            }

            // ── Latin Extended ───────────────────────────────────────────────
            Section("Latin Extended (LiberationSans)");
            canvas.DrawText("café résumé naïve Ångström fiancée Üniversität", 72, y,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(12)); y += gap;
            canvas.DrawText("— em-dash  – en-dash  ' ' “ ”  …  •  © ® ™", 72, y,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(12)); y += gap;

            // ── Greek ────────────────────────────────────────────────────────
            Section("Greek (NotoSans)");
            canvas.DrawText("αβγδεζηθι  ΑΒΓΔΕΖΗΘΙ  Ω π φ ψ", 72, y, body); y += gap;
            canvas.DrawText("Ελληνικά  αλφάβητο  φιλοσοφία", 72, y, body); y += gap;

            // ── Cyrillic ─────────────────────────────────────────────────────
            Section("Cyrillic / Russian (NotoSans)");
            canvas.DrawText("АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", 72, y, body); y += gap;
            canvas.DrawText("Привет мир  Москва  Санкт-Петербург", 72, y, body); y += gap;

            // ── Mixed scripts ────────────────────────────────────────────────
            Section("Mixed scripts — fallback segmentation (LiberationSans + NotoSans)");
            canvas.DrawText("Hello κόσμε мир — Latin + Greek + Cyrillic in one call", 72, y,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(12)); y += gap;

            // ── Emoji ────────────────────────────────────────────────────────
            Section("Emoji (surrogate pairs — U+1F600 range)");
            if (emojiFont != null)
            {
                canvas.DrawText("😀 🎉 👍 ❤️ 🌍 🎵", 72, y,
                    TextStyle.Default.WithFamily("Emoji").WithSize(18)); y += gap + 4;
            }
            else
            {
                canvas.DrawText("😀 🎉 👍 ❤️  (renders as .notdef — no emoji font found)", 72, y, body); y += gap;
                canvas.DrawText("Install a system emoji font to see glyphs.", 72, y, small); y += gap;
            }

            // ── CJK ──────────────────────────────────────────────────────────
            Section("CJK — Chinese / Japanese / Korean");
            if (cjkFont != null)
            {
                // CJK font was added to reg above; use it directly via WithFontFile
                canvas.DrawText("你好世界  こんにちは  안녕하세요", 72, y,
                    TextStyle.Default.WithFontFile(cjkFont).WithSize(18)); y += gap + 4;
            }
            else
            {
                canvas.DrawText("你好世界 こんにちは 안녕하세요", 72, y, body); y += gap;
                canvas.DrawText("(renders as .notdef — no CJK font found on this machine)", 72, y, small); y += gap;
            }

            // ── Encoding note ────────────────────────────────────────────────
            y += 6;
            canvas.DrawLine(72, y, PageSizes.A4.Width - 72, y,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            y += 16;
            canvas.DrawText("Note: characters without a glyph in any registered font render as",
                72, y, small); y += 16;
            canvas.DrawText("empty boxes (.notdef). See example 15 for right-to-left (RTL) text.",
                72, y, small);
        })
        .Save(Out(name, version));
}

// ── example 13: password-protected ───────────────────────────────────────────
// Encryption: Standard Security Handler Rev 4, AES-128-CBC (V=4, R=4).
// This is the algorithm defined in PDF 1.5–1.7 and is supported by every major
// viewer. PDF 2.0 formally specifies Rev 6 / AES-256, but that algorithm
// requires significantly more complex implementation (O_E / U_E / Perms entries,
// hash rounds). AES-128 Rev 4 is widely accepted and remains secure for most
// document-protection use cases.

static void PasswordProtectedExample(string name, FontRegistry fonts, PdfVersion version)
{
    var security = PdfSecurity.Protect(userPassword: "open123", ownerPassword: "owner456")
        .WithPermissions(PdfPermissions.Print | PdfPermissions.CopyText);

    var heading = TextStyle.Default.WithSize(18).WithBold(true);
    var body    = TextStyle.Default.WithSize(12);
    var small   = TextStyle.Default.WithSize(9).WithItalic(true);
    var mono    = TextStyle.Default.WithFamily("LiberationMono").WithSize(10);

    PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .WithTitle("Password-Protected Example")
        .WithSecurity(security)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 72;
            const float gap = 20;

            canvas.DrawText("Password-Protected PDF", 72, y, heading); y += gap + 6;
            canvas.DrawText("This document is encrypted with AES-128 (Standard Security Handler Rev 4).", 72, y, body); y += gap;
            canvas.DrawText("Open password: open123     Owner password: owner456", 72, y, body); y += gap;
            canvas.DrawText("Permissions granted: Print, CopyText", 72, y, body); y += gap * 2;

            canvas.DrawText("Encryption details:", 72, y, TextStyle.Default.WithSize(12).WithBold(true)); y += gap;
            canvas.DrawText("/Filter /Standard  /V 4  /R 4  /Length 128  /StmF /StdCF  /StrF /StdCF", 72, y, mono); y += gap;
            canvas.DrawText("/CFM /AESV2  (AES-128-CBC, per-object key derived with MD5 + sAlT suffix)", 72, y, mono); y += gap * 2;

            canvas.DrawText("Note: PDF 2.0 specifies AES-256 (Rev 6) but AES-128 (Rev 4) is", 72, y, small); y += 14;
            canvas.DrawText("universally supported and remains appropriate for most use cases.", 72, y, small);
        })
        .Save(Out(name, version));
}

// ── example 14: digitally signed ─────────────────────────────────────────────
// Creates a self-signed RSA-2048 certificate on the fly and embeds a
// PKCS#7 detached signature (adbe.pkcs7.detached) using SHA-256.
// In production supply a real certificate issued by a trusted CA.

static void SignedExample(string name, FontRegistry fonts, PdfVersion version)
{
    using var cert = CreateSelfSignedCert();
    var sigOpts = new PdfSignatureOptions(cert)
        .WithReason("Example approval")
        .WithSignerName("Majorsilence Example")
        .WithLocation("Toronto, ON");

    var heading = TextStyle.Default.WithSize(18).WithBold(true);
    var body    = TextStyle.Default.WithSize(12);
    var small   = TextStyle.Default.WithSize(9).WithItalic(true);

    PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .WithTitle("Digitally-Signed Example")
        .WithSignature(sigOpts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 72;
            const float gap = 20;

            canvas.DrawText("Digitally-Signed PDF", 72, y, heading); y += gap + 6;
            canvas.DrawText("This document contains an embedded PKCS#7 detached digital signature.", 72, y, body); y += gap;
            canvas.DrawText("SubFilter: adbe.pkcs7.detached    Digest: SHA-256", 72, y, body); y += gap;
            canvas.DrawText("Signer: Majorsilence Example    Location: Toronto, ON", 72, y, body); y += gap;
            canvas.DrawText("Reason: Example approval", 72, y, body); y += gap * 2;

            canvas.DrawText("The certificate used here is self-signed (generated at runtime).", 72, y, small); y += 14;
            canvas.DrawText("Open in Adobe Acrobat or similar to verify the signature panel.", 72, y, small);
        })
        .Save(Out(name, version));
}

static X509Certificate2 CreateSelfSignedCert()
{
    using var rsa = RSA.Create(2048);
    var req = new CertificateRequest(
        "CN=Majorsilence Example Signer",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
    req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
    var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
    var notAfter  = notBefore.AddYears(1);
    var cert = req.CreateSelfSigned(notBefore, notAfter);
    // Export and re-import so the private key is exportable on all platforms
    var pfxBytes = cert.Export(X509ContentType.Pfx);
    return X509CertificateLoader.LoadPkcs12(pfxBytes, (string?)null,
        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
}

// ── example 15: right-to-left text ───────────────────────────────────────────
// Uses the bundled NotoSansHebrew / NotoSansArabic fonts (SIL OFL 1.1 license).
// IsRightToLeft reverses code-point order for visual RTL rendering.
// Hebrew works correctly without a shaping engine (letters have no contextual forms).
// Arabic requires an external shaper for proper ligature forms; this example
// shows visual reversal which is readable for many Arabic words.
// Combine WithRightToLeft() with TextAlignment.Right so x is the right anchor.

static void RightToLeftExample(string name, FontRegistry fonts, PdfVersion version)
{
    float pageWidth   = PageSizes.A4.Width;
    float rightMargin = pageWidth - 72;

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var label   = TextStyle.Default.WithFamily("LiberationSans").WithSize(10).WithColor(PdfColor.Gray);
    var note    = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    // Build a local registry that extends the shared one with the script-specific
    // Noto families that ship in the bundled fonts directory.
    string fontsDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "Majorsilence.Drawing.Common", "Fonts"));

    // Build a local registry from the bundled fonts dir.
    // NotoSansHebrew / NotoSansArabic / NotoSansSymbols are all picked up by
    // AddDirectory and then listed as fallbacks so glyphs missed by any primary
    // font (including arrows in NotoSansHebrew and Arabic in LiberationSans labels)
    // are covered automatically.
    var reg = new FontRegistry()
        .AddDirectory(fontsDir)
        .AddFallback("NotoSans")
        .AddFallback("NotoSansHebrew")
        .AddFallback("NotoSansArabic")
        .AddFallback("NotoSansSymbols");

    bool hasHebrew = reg.Contains("NotoSansHebrew");
    bool hasArabic = reg.Contains("NotoSansArabic");
    string hebrewFamily = hasHebrew ? "NotoSansHebrew" : "NotoSans";
    string arabicFamily = hasArabic ? "NotoSansArabic" : "NotoSans";

    var rtlHebrew = TextStyle.Default.WithFamily(hebrewFamily).WithSize(16)
                        .WithRightToLeft().WithAlignment(TextAlignment.Right);
    var rtlArabic = TextStyle.Default.WithFamily(arabicFamily).WithSize(16)
                        .WithRightToLeft().WithAlignment(TextAlignment.Right);

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Right-to-Left Text")
        .WithFontRegistry(reg)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;
            const float sectionGap = 12f;

            canvas.DrawText("Right-to-Left (RTL) Text", 72, y, heading); y += 36;

            // ── Hebrew ────────────────────────────────────────────────────────
            canvas.DrawText("Hebrew (עברית) — NotoSansHebrew + IsRightToLeft + TextAlignment.Right:", 72, y, label); y += 20;

            canvas.DrawText("שלום עולם", rightMargin, y, rtlHebrew.WithSize(24).WithBold()); y += 32;
            canvas.DrawText("ספר, ישראל, שלום, אהבה", rightMargin, y, rtlHebrew); y += 26;
            canvas.DrawText("האות הראשונה של האלפבית היא אלף", rightMargin, y, rtlHebrew.WithSize(13)); y += 28;

            canvas.DrawLine(72, y, rightMargin, y,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            y += sectionGap + 4;

            // ── Arabic (visual reversal only) ─────────────────────────────────
            canvas.DrawText("Arabic (العربية) — NotoSansArabic + visual reversal (no ligature shaping):", 72, y, label); y += 20;

            canvas.DrawText("مرحبا بالعالم", rightMargin, y, rtlArabic.WithSize(24).WithBold()); y += 32;
            canvas.DrawText("كتاب, قلم, ورقة", rightMargin, y, rtlArabic); y += 26;

            canvas.DrawLine(72, y, rightMargin, y,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            y += sectionGap + 4;

            // ── Mixed LTR + RTL side-by-side ─────────────────────────────────
            canvas.DrawText("Mixed — LTR on left, RTL on right:", 72, y, label); y += 20;

            var ltr = TextStyle.Default.WithFamily("LiberationSans").WithSize(14);
            canvas.DrawText("Hello, World!", 72, y, ltr);
            canvas.DrawText("!שלום, עולם", rightMargin, y,
                rtlHebrew.WithSize(14).WithAlignment(TextAlignment.Right));
            y += 28;

            canvas.DrawText("Left-to-right →", 72, y, ltr.WithSize(11).WithColor(PdfColor.Blue));
            canvas.DrawText("← ימינה לשמאל", rightMargin, y,
                rtlHebrew.WithSize(11).WithColor(PdfColor.Red).WithAlignment(TextAlignment.Right));
            y += 36;

            // ── Note ─────────────────────────────────────────────────────────
            canvas.DrawLine(72, y, rightMargin, y,
                StrokeStyle.Default.WithWidth(0.3f).WithColor(PdfColor.LightGray));
            y += 12;
            canvas.DrawText("Fonts: NotoSansHebrew / NotoSansArabic (SIL Open Font License 1.1).", 72, y, note); y += 13;
            canvas.DrawText("Hebrew renders correctly without a shaper. Arabic requires HarfBuzz or similar", 72, y, note); y += 13;
            canvas.DrawText("for correct contextual ligature forms (initial / medial / final / isolated).", 72, y, note);
        })
        .Save(Out(name, version));
}

// ── geometry helpers ─────────────────────────────────────────────────────────

static List<(float, float)> Pentagon(float cx, float cy, float r)
{
    var pts = new List<(float, float)>();
    for (int i = 0; i < 5; i++)
    {
        double a = Math.PI * (i * 72 - 90) / 180.0;
        pts.Add(((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a))));
    }
    return pts;
}

static List<(float, float)> Hexagon(float cx, float cy, float r)
{
    var pts = new List<(float, float)>();
    for (int i = 0; i < 6; i++)
    {
        double a = Math.PI * (i * 60 - 90) / 180.0;
        pts.Add(((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a))));
    }
    return pts;
}

static void DrawPieSlice(PdfCanvas canvas, float cx, float cy, float r,
    float startDeg, float sweepDeg, PdfColor color)
{
    int segments = Math.Max(3, (int)(sweepDeg / 5));
    var pts = new List<(float, float)> { (cx, cy) };
    for (int i = 0; i <= segments; i++)
    {
        double a = (startDeg + sweepDeg * i / segments) * Math.PI / 180.0;
        pts.Add(((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a))));
    }
    canvas.DrawPolygon(pts, ShapeStyle.Filled(color).WithStroke(PdfColor.White, 0.5f));
}

// ── example 16: PDF merge ────────────────────────────────────────────────────

static void PdfMergeExample(string name, FontRegistry fonts, PdfVersion version)
{
    var title  = TextStyle.Default.WithFamily("LiberationSans").WithSize(28).WithBold();
    var sub    = TextStyle.Default.WithFamily("LiberationSans").WithSize(14);
    var body   = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
    var pageNr = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                     .WithColor(new PdfColor(120, 120, 120))
                     .WithAlignment(TextAlignment.Right);

    // ── source A: cover + intro (2 pages) ────────────────────────────────────
    byte[] sourceA = PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawRectangle(0, 0, 595, 842,
                ShapeStyle.Filled(new PdfColor(30, 80, 160)));
            canvas.DrawText("Quarterly Report",        72, 280, title.WithColor(PdfColor.White).WithSize(36));
            canvas.DrawText("Q2 2026  —  Example Corp", 72, 330, sub.WithColor(new PdfColor(200, 220, 255)));
        })
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Introduction", 72, 72, title.WithSize(22));
            canvas.DrawLine(72, 98, 523, 98, StrokeStyle.Default.WithColor(new PdfColor(30, 80, 160)).WithWidth(1.5f));
            canvas.DrawText("This document was assembled by PdfMerger from two independently", 72, 120, body);
            canvas.DrawText("generated source PDFs, each produced with PdfDocument.", 72, 138, body);
            canvas.DrawText("Source A · Page 2", 523, 800, pageNr);
        })
        .ToBytes();

    // ── source B: two content pages ──────────────────────────────────────────
    byte[] sourceB = PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Financial Summary", 72, 72, title.WithSize(22));
            canvas.DrawLine(72, 98, 523, 98, StrokeStyle.Default.WithColor(new PdfColor(30, 80, 160)).WithWidth(1.5f));

            float y = 130;
            void Row(string label, string value, bool highlight = false)
            {
                var c = highlight ? new PdfColor(30, 80, 160) : PdfColor.Black;
                canvas.DrawText(label, 72,  y, body.WithColor(c));
                canvas.DrawText(value, 450, y, body.WithAlignment(TextAlignment.Right).WithColor(c));
                y += 22;
            }

            Row("Revenue",             "$4 200 000");
            Row("Cost of Goods Sold",  "$1 800 000");
            Row("Gross Profit",        "$2 400 000", highlight: true);
            Row("Operating Expenses",  "$  900 000");
            Row("Net Income",          "$1 500 000", highlight: true);
            canvas.DrawText("Source B · Page 1", 523, 800, pageNr);
        })
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Notes", 72, 72, title.WithSize(22));
            canvas.DrawLine(72, 98, 523, 98, StrokeStyle.Default.WithColor(new PdfColor(30, 80, 160)).WithWidth(1.5f));
            canvas.DrawText("• Figures are illustrative only.", 72, 130, body);
            canvas.DrawText("• Assembled with Majorsilence.Pdf PdfMerger.", 72, 154, body);
            canvas.DrawText("Source B · Page 2", 523, 800, pageNr);
        })
        .ToBytes();

    // ── merge A + B into a single 4-page document ─────────────────────────────
    byte[] merged = new PdfMerger()
        .Add(sourceA)
        .Add(sourceB)
        .WithTitle("Quarterly Report Q2 2026")
        .WithAuthor("Example Corp")
        .WithSubject("Financial Summary")
        .WithCreator("Majorsilence.Pdf PdfMerger")
        .Merge();

    // PdfMerger output is always PDF 1.4; name the file by source version.
    File.WriteAllBytes(Out(name, version), merged);
}

// ── example 17: images from disk (JPEG + PNG) ────────────────────────────────
// sample_photo.jpg and sample_logo.png sit alongside Program.cs in the source
// tree and are copied to the output directory at build time via the .csproj.

static void ImagesFromDiskExample(string name, FontRegistry fonts, PdfVersion version)
{
    string baseDir  = AppContext.BaseDirectory;
    string jpegPath = Path.Combine(baseDir, "sample_photo.jpg");
    string pngPath  = Path.Combine(baseDir, "sample_logo.png");

    if (!File.Exists(jpegPath) || !File.Exists(pngPath))
        throw new FileNotFoundException(
            $"Sample images not found. Expected:\n  {jpegPath}\n  {pngPath}");

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var label   = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);
    var caption = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(new PdfColor(80, 80, 80));

    // Load JPEG — pass raw bytes directly; PDF readers decompress natively.
    var (jpegBytes, jpegW, jpegH) = LoadJpeg(jpegPath);

    // Load PNG — decode to raw RGB24 bytes; PdfCanvas FlateDecode-compresses them.
    var (pngRgb, pngW, pngH) = LoadPngAsRgb(pngPath);

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Images from Disk")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Embedding images loaded from disk", 72, 50, heading);

            // ── JPEG ─────────────────────────────────────────────────────────
            canvas.DrawText("JPEG — raw bytes embedded, no re-encoding:", 72, 90, label);
            float jpegDrawW = 200f;
            float jpegDrawH = jpegDrawW * jpegH / jpegW;
            canvas.DrawImage(jpegBytes, jpegW, jpegH, isJpeg: true,
                x: 72, y: 105, width: jpegDrawW, height: jpegDrawH);
            canvas.DrawText(Path.GetFileName(jpegPath), 72, 105 + jpegDrawH + 8, caption);

            // ── PNG ──────────────────────────────────────────────────────────
            float pngTop = 105 + jpegDrawH + 30;
            canvas.DrawText("PNG — decoded to raw RGB24, FlateDecode compressed:", 72, pngTop, label);
            float pngDrawW = 200f;
            float pngDrawH = pngDrawW * pngH / pngW;
            canvas.DrawImage(pngRgb, pngW, pngH, isJpeg: false,
                x: 72, y: pngTop + 15, width: pngDrawW, height: pngDrawH);
            canvas.DrawText(Path.GetFileName(pngPath), 72, pngTop + 15 + pngDrawH + 8, caption);

            // ── side-by-side at fixed size ────────────────────────────────────
            float sxTop = pngTop + 15 + pngDrawH + 30;
            canvas.DrawText("Both images scaled to 120 × 80 pt, side-by-side:", 72, sxTop, label);
            canvas.DrawImage(jpegBytes, jpegW, jpegH, isJpeg: true,
                x: 72,  y: sxTop + 15, width: 120, height: 80);
            canvas.DrawImage(pngRgb,   pngW,  pngH,  isJpeg: false,
                x: 210, y: sxTop + 15, width: 120, height: 80);
        })
        .Save(Out(name, version));
}

// Load a JPEG file — raw bytes go straight into the PDF; scan SOF marker for dimensions.
static (byte[] data, int width, int height) LoadJpeg(string path)
{
    byte[] data = File.ReadAllBytes(path);
    for (int i = 0; i < data.Length - 8; i++)
    {
        if (data[i] == 0xFF && (data[i + 1] == 0xC0 || data[i + 1] == 0xC2))
        {
            int h = (data[i + 5] << 8) | data[i + 6];
            int w = (data[i + 7] << 8) | data[i + 8];
            return (data, w, h);
        }
    }
    throw new InvalidDataException($"JPEG SOF marker not found in {path}");
}

// Decode a PNG file to raw RGB24 bytes using the built-in .NET PNG decoder.
// PdfCanvas.DrawImage(isJpeg: false) accepts raw RGB24 and compresses with FlateDecode.
static (byte[] rgb, int width, int height) LoadPngAsRgb(string path)
{
    // System.Drawing is available in net6+ via System.Drawing.Common on Windows,
    // or via Majorsilence.Drawing.Common (SkiaSharp-backed) on Linux/macOS.
    // Here we parse the PNG ourselves to stay zero-dependency in this example.
    byte[] png = File.ReadAllBytes(path);

    // Read IHDR: width at byte 16, height at byte 20
    if (png.Length < 33 || png[1] != 'P' || png[2] != 'N' || png[3] != 'G')
        throw new InvalidDataException("Not a valid PNG file");
    int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
    int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
    int bitDepth  = png[24];
    int colorType = png[25]; // 2=RGB, 6=RGBA
    if (bitDepth != 8 || (colorType != 2 && colorType != 6))
        throw new NotSupportedException($"Only 8-bit RGB/RGBA PNG supported (got bitDepth={bitDepth} colorType={colorType})");

    // Collect IDAT chunks
    var idat = new System.Collections.Generic.List<byte>();
    int pos = 8;
    while (pos + 12 <= png.Length)
    {
        int len  = (png[pos] << 24) | (png[pos+1] << 16) | (png[pos+2] << 8) | png[pos+3];
        string ct = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
        if (ct == "IDAT") idat.AddRange(new ArraySegment<byte>(png, pos + 8, len));
        if (ct == "IEND") break;
        pos += 12 + len;
    }

    // Decompress and unfilter
    byte[] compressed = idat.ToArray();
    byte[] raw = DecompressZlib(compressed);
    int stride = colorType == 6 ? w * 4 : w * 3;
    var rgb = new byte[w * h * 3];
    var prev = new byte[stride];
    int outIdx = 0;
    for (int row = 0; row < h; row++)
    {
        int rowStart = row * (stride + 1);
        byte filter = raw[rowStart];
        var cur = new byte[stride];
        Array.Copy(raw, rowStart + 1, cur, 0, stride);
        ApplyPngFilter(filter, cur, prev, colorType == 6 ? 4 : 3);
        int channels = colorType == 6 ? 4 : 3;
        for (int x = 0; x < w; x++)
        {
            rgb[outIdx++] = cur[x * channels];
            rgb[outIdx++] = cur[x * channels + 1];
            rgb[outIdx++] = cur[x * channels + 2];
        }
        prev = cur;
    }
    return (rgb, w, h);
}

static byte[] DecompressZlib(byte[] data)
{
    // skip 2-byte zlib header
    using var ms  = new MemoryStream(data, 2, data.Length - 2);
    using var ds  = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
    using var out_ = new MemoryStream();
    ds.CopyTo(out_);
    return out_.ToArray();
}

static void ApplyPngFilter(byte filter, byte[] cur, byte[] prev, int bpp)
{
    switch (filter)
    {
        case 1: // Sub
            for (int i = bpp; i < cur.Length; i++)
                cur[i] = (byte)(cur[i] + cur[i - bpp]);
            break;
        case 2: // Up
            for (int i = 0; i < cur.Length; i++)
                cur[i] = (byte)(cur[i] + prev[i]);
            break;
        case 3: // Average
            for (int i = 0; i < cur.Length; i++)
            {
                byte a = i >= bpp ? cur[i - bpp] : (byte)0;
                cur[i] = (byte)(cur[i] + (a + prev[i]) / 2);
            }
            break;
        case 4: // Paeth
            for (int i = 0; i < cur.Length; i++)
            {
                byte a = i >= bpp ? cur[i - bpp] : (byte)0;
                byte b = prev[i];
                byte c = i >= bpp ? prev[i - bpp] : (byte)0;
                cur[i] = (byte)(cur[i] + PaethPredictor(a, b, c));
            }
            break;
        // case 0: None — no change
    }
}

static byte PaethPredictor(byte a, byte b, byte c)
{
    int p = a + b - c;
    int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
    return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
}

// ── example 18: opacity / transparency ──────────────────────────────────────

static void OpacityExample(string name, FontRegistry fonts, PdfVersion version)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);
    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Opacity / Transparency")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Opacity & Transparency", 72, 50, heading);

            // Fill opacity ramp
            canvas.DrawText("Fill opacity: 1.0  →  0.1", 72, 90, label);
            for (int i = 0; i < 10; i++)
            {
                float opacity = 1f - i * 0.09f;
                canvas.DrawRectangle(72 + i * 46, 105, 40, 40,
                    ShapeStyle.Filled(PdfColor.Blue).WithFillOpacity(opacity));
            }

            // Stroke opacity ramp
            canvas.DrawText("Stroke opacity: 1.0  →  0.1", 72, 165, label);
            for (int i = 0; i < 10; i++)
            {
                float opacity = 1f - i * 0.09f;
                canvas.DrawRectangle(72 + i * 46, 180, 40, 40,
                    ShapeStyle.Stroked(PdfColor.Red, 3f).WithStrokeOpacity(opacity));
            }

            // Overlapping shapes — fill transparency lets background show through
            canvas.DrawText("Overlapping shapes with transparency:", 72, 245, label);
            canvas.DrawRectangle(80, 260, 150, 100, ShapeStyle.Filled(PdfColor.Red));
            canvas.DrawRectangle(155, 285, 150, 100,
                ShapeStyle.Filled(PdfColor.Blue).WithFillOpacity(0.5f));
            canvas.DrawEllipse(115, 320, 150, 80,
                ShapeStyle.Filled(PdfColor.Green).WithFillOpacity(0.4f));

            // Semi-transparent stroke over a filled background
            canvas.DrawText("Semi-transparent stroke (50%) over solid fill:", 72, 395, label);
            canvas.DrawRectangle(72, 410, 200, 60, ShapeStyle.Filled(PdfColor.Yellow));
            canvas.DrawRectangle(72, 410, 200, 60,
                ShapeStyle.Stroked(PdfColor.DarkGray, 6f).WithStrokeOpacity(0.5f));

            // StrokeStyle opacity on lines
            canvas.DrawText("Line opacity: StrokeStyle.WithOpacity()", 72, 500, label);
            for (int i = 0; i < 10; i++)
            {
                float opacity = 1f - i * 0.09f;
                canvas.DrawLine(72, 518 + i * 16, 450, 518 + i * 16,
                    StrokeStyle.Default.WithWidth(4).WithColor(PdfColor.DarkGray)
                                .WithOpacity(opacity));
            }
        })
        .Save(Out(name, version));
}

// ── example 19: multi-line text wrapping ────────────────────────────────────

static void TextWrappingExample(string name, FontRegistry fonts, PdfVersion version)
{
    const string loremIpsum =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor " +
        "incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud " +
        "exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure " +
        "dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt " +
        "mollit anim id est laborum. " +
        "Curabitur pretium tincidunt lacus. Nulla gravida orci a odio. Nullam varius, turpis " +
        "molestie pretium placerat, arcu ante aliquet magna, sed tempus erat felis nec urna.\n\n" +
        "Second paragraph: hard newlines (\\n) force a line break at any point, while words " +
        "are always kept intact across soft wraps. The overflow index returned by DrawTextBox " +
        "lets you continue the text on the next page.";

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(14).WithBold();
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
    var caption = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Multi-Line Text Wrapping")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 50;

            // Left-aligned box
            canvas.DrawText("Left-aligned (400 pt wide, 160 pt tall):", 72, y, heading);
            y += 20;
            canvas.DrawRectangle(72, y, 400, 160,
                ShapeStyle.Stroked(PdfColor.LightGray, 0.5f));
            int overflow = canvas.DrawTextBox(loremIpsum, 72, y, 400, 160, body);
            y += 168;

            if (overflow < loremIpsum.Length)
            {
                canvas.DrawText($"(text overflowed at index {overflow} — continues below)",
                    72, y, caption);
                y += 16;

                // Continue the overflow in a second box
                canvas.DrawText("Continued text (overflow):", 72, y, heading);
                y += 20;
                canvas.DrawRectangle(72, y, 400, 120,
                    ShapeStyle.Stroked(PdfColor.LightGray, 0.5f));
                canvas.DrawTextBox(loremIpsum.Substring(overflow), 72, y, 400, 120, body);
                y += 128;
            }

            // Centre-aligned
            y += 10;
            canvas.DrawText("Centre-aligned (300 pt wide):", 72, y, heading);
            y += 18;
            canvas.DrawRectangle(72, y, 300, 80,
                ShapeStyle.Stroked(PdfColor.LightGray, 0.5f));
            canvas.DrawTextBox("Centre-aligned text box.\nSecond line is also centred.",
                72, y, 300, 80,
                body.WithAlignment(TextAlignment.Center));
            y += 88;

            // Right-aligned
            canvas.DrawText("Right-aligned (300 pt wide):", 72, y, heading);
            y += 18;
            canvas.DrawRectangle(72, y, 300, 80,
                ShapeStyle.Stroked(PdfColor.LightGray, 0.5f));
            canvas.DrawTextBox("Right-aligned text box.\nAll lines snap to right edge.",
                72, y, 300, 80,
                body.WithAlignment(TextAlignment.Right));
        })
        .Save(Out(name, version));
}

// ── example 20: tables ───────────────────────────────────────────────────────

static void TableExample(string name, FontRegistry fonts, PdfVersion version)
{
    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var caption = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    PdfDocument.Create()
        .WithVersion(version)
        .WithTitle("Table Example")
        .WithFontRegistry(fonts)
        .AddPage(PageSizes.A4, canvas =>
        {
            canvas.DrawText("Table Layout", 72, 40, heading);

            // ── basic table ───────────────────────────────────────────────────
            canvas.DrawText("Basic table with header and alternating rows:", 72, 76,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(10));

            var productTable = new PdfTable(new float[] { 180, 80, 90, 90 })
                .WithHeaderBackground(new PdfColor(26, 86, 160))
                .WithAlternateRowBackground(new PdfColor(240, 245, 252))
                .WithBorder(new PdfColor(200, 200, 200), 0.5f)
                .WithCellPadding(5f)
                .WithCellTextStyle(TextStyle.Default.WithFamily("LiberationSans").WithSize(10))
                .WithHeaderTextStyle(
                    TextStyle.Default.WithFamily("LiberationSans").WithSize(10)
                                     .WithBold().WithColor(PdfColor.White));

            productTable.AddRow("Product",          "Qty",    "Unit Price", "Total");
            productTable.AddRow("PDF Library Pro",   "3",     "$400.00",   "$1 200.00");
            productTable.AddRow("Report Designer",   "1",     "$250.00",   "$250.00");
            productTable.AddRow("Integration Pack",  "2",     "$180.00",   "$360.00");
            productTable.AddRow("Support (12 mo.)",  "1",     "$500.00",   "$500.00");
            productTable.AddRow("",                   "",     "Total:",    "$2 310.00");

            canvas.DrawTable(productTable, 72, 92, out float tableBottom);

            // ── no-border table ───────────────────────────────────────────────
            float y2 = 92 + tableBottom + 28;
            canvas.DrawText("Table without borders (report style):", 72, y2,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(10));
            y2 += 16;

            var reportTable = new PdfTable(new float[] { 200, 100, 100 })
                .WithNoBorder()
                .WithHeaderBackground(new PdfColor(245, 245, 245))
                .WithCellPadding(4f)
                .WithCellTextStyle(TextStyle.Default.WithFamily("LiberationSans").WithSize(10))
                .WithHeaderTextStyle(
                    TextStyle.Default.WithFamily("LiberationSans").WithSize(10).WithBold());

            reportTable.AddRow("Category",         "Revenue",  "Growth");
            reportTable.AddRow("North America",     "$1.24M",   "+12%");
            reportTable.AddRow("Europe",            "$0.89M",   "+8%");
            reportTable.AddRow("Asia Pacific",      "$0.45M",   "+18%");
            reportTable.AddRow("Other",             "$0.12M",   "+3%");

            canvas.DrawTable(reportTable, 72, y2, out float table2Bottom);

            // ── wrapped-cell table ────────────────────────────────────────────
            float y3 = y2 + table2Bottom + 28;
            canvas.DrawText("Table with wrapping cell text:", 72, y3,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(10));
            y3 += 16;

            var wrapTable = new PdfTable(new float[] { 120, 250, 70 })
                .WithHeaderBackground(new PdfColor(80, 120, 180))
                .WithBorder(new PdfColor(180, 180, 180), 0.5f)
                .WithCellPadding(5f)
                .WithCellTextStyle(TextStyle.Default.WithFamily("LiberationSans").WithSize(9))
                .WithHeaderTextStyle(
                    TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                                     .WithBold().WithColor(PdfColor.White));

            wrapTable.AddRow("Feature",      "Description",                                   "Status");
            wrapTable.AddRow("DrawTextBox",  "Word-wrapped multi-line text with overflow index for pagination.", "Done");
            wrapTable.AddRow("DrawTable",    "Table layout with header, alternating rows, and per-cell styles.", "Done");
            wrapTable.AddRow("Opacity",      "ShapeStyle and StrokeStyle opacity via ExtGState /ca and /CA.",   "Done");
            wrapTable.AddRow("PDF/A",        "XMP conformance claim + embedded ICC sRGB output intent.",         "Done");

            canvas.DrawTable(wrapTable, 72, y3);
        })
        .Save(Out(name, version));
}

// ── example 21: PDF/A conformance ───────────────────────────────────────────
// PDF/A documents cannot use standard Type1 fonts (Helvetica, Times, Courier).
// All text must use embedded TrueType fonts from the FontRegistry.

static void PdfAExample(string name, FontRegistry fonts, PdfVersion version)
{
    // PDF/A-2b requires embedded fonts — skip if registry has none.
    if (!fonts.Contains("LiberationSans"))
        throw new InvalidOperationException("LiberationSans not found — PDF/A requires embedded fonts");

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
    var mono    = TextStyle.Default.WithFamily("LiberationMono").WithSize(10);
    var small   = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    // PDF/A-2b: based on PDF 1.7, Level B visual reproducibility.
    // WithConformance() automatically sets version to PDF 1.7.
    PdfDocument.Create()
        .WithConformance(PdfConformance.PdfA2b)
        .WithFontRegistry(fonts)
        .WithTitle("PDF/A-2b Conformance Example")
        .WithAuthor("Majorsilence.Pdf")
        .WithSubject("PDF/A archival conformance demonstration")
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;

            canvas.DrawText("PDF/A-2b Conformance", 72, y, heading); y += 32;
            canvas.DrawRectangle(72, y, PageSizes.A4.Width - 144, 2,
                ShapeStyle.Filled(new PdfColor(26, 86, 160))); y += 16;

            canvas.DrawText("This document claims PDF/A-2b (ISO 19005-2, Level B) conformance.", 72, y, body); y += 20;
            canvas.DrawText("The output includes:", 72, y, body); y += 20;

            void Bullet(string text)
            {
                canvas.DrawText("•  " + text, 88, y, body);
                y += 18;
            }

            Bullet("XMP metadata stream with pdfaid:part=2 / pdfaid:conformance=B");
            Bullet("Embedded sRGB ICC output intent (/GTS_PDFA1 subtype)");
            Bullet("PDF 1.7 header (%PDF-1.7) — automatically set by WithConformance()");
            Bullet("Embedded TrueType fonts — standard Type1 fonts are not permitted");
            Bullet("No encryption — PDF/A and encryption are mutually exclusive");
            y += 10;

            canvas.DrawText("Conformance level variants:", 72, y, body.WithBold()); y += 20;

            var levels = new[]
            {
                ("PdfA1b", "Based on PDF 1.4",  "Max compat; no transparency; widespread archival use"),
                ("PdfA2b", "Based on PDF 1.7",  "Allows transparency; recommended for new archives"),
                ("PdfA3b", "Based on PDF 1.7",  "Same as PDF/A-2b + supports embedded file attachments"),
            };

            foreach (var (level, basis, desc) in levels)
            {
                canvas.DrawText(level, 88, y, mono.WithBold());
                canvas.DrawText(basis, 185, y, mono);
                canvas.DrawText(desc,  290, y, TextStyle.Default.WithFamily("LiberationSans").WithSize(9));
                y += 16;
            }

            y += 12;
            canvas.DrawText("Usage:", 72, y, body.WithBold()); y += 18;
            canvas.DrawText("PdfDocument.Create()", 88, y, mono); y += 14;
            canvas.DrawText("    .WithConformance(PdfConformance.PdfA2b)", 88, y, mono); y += 14;
            canvas.DrawText("    .WithFontRegistry(registry)  // embedded fonts required", 88, y, mono); y += 14;
            canvas.DrawText("    .AddPage(...)", 88, y, mono); y += 14;
            canvas.DrawText("    .Save(\"archive.pdf\");", 88, y, mono); y += 22;

            canvas.DrawText("Note: use a PDF/A validator (e.g. veraPDF) to confirm full conformance.", 72, y, small);
        })
        .Save(Out(name, version));
}

// ── example 22: visible signature appearance ─────────────────────────────────

static void VisibleSignatureExample(string name, FontRegistry fonts, PdfVersion version)
{
    using var cert = CreateSelfSignedCert();

    var sigOpts = new PdfSignatureOptions(cert)
        .WithReason("Document approved")
        .WithSignerName("Jane Smith")
        .WithLocation("Toronto, ON")
        .WithAppearance(x: 72, y: 690, width: 220, height: 60);

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
    var small   = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .WithTitle("Visible Signature Appearance")
        .WithSignature(sigOpts)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;

            canvas.DrawText("Visible Digital Signature", 72, y, heading); y += 32;
            canvas.DrawText("This document contains a PKCS#7 digital signature with a visible", 72, y, body); y += 18;
            canvas.DrawText("appearance box rendered at the bottom of the page.", 72, y, body); y += 18;
            canvas.DrawText("The signature box shows a border, 'Digitally Signed', and the signer name.", 72, y, body); y += 30;

            canvas.DrawText("Signature properties:", 72, y, body.WithBold()); y += 18;
            canvas.DrawText("Signer:    Jane Smith",         88, y, body); y += 16;
            canvas.DrawText("Reason:    Document approved",  88, y, body); y += 16;
            canvas.DrawText("Location:  Toronto, ON",        88, y, body); y += 16;
            canvas.DrawText("Algorithm: PKCS#7 / SHA-256 / adbe.pkcs7.detached", 88, y, body); y += 30;

            canvas.DrawText("WithAppearance() usage:", 72, y, body.WithBold()); y += 18;
            var mono = TextStyle.Default.WithFamily("LiberationMono").WithSize(10);
            canvas.DrawText("new PdfSignatureOptions(cert)", 88, y, mono); y += 14;
            canvas.DrawText("    .WithSignerName(\"Jane Smith\")", 88, y, mono); y += 14;
            canvas.DrawText("    .WithReason(\"Document approved\")", 88, y, mono); y += 14;
            canvas.DrawText("    .WithAppearance(x: 72, y: 690, width: 220, height: 60)", 88, y, mono); y += 28;

            canvas.DrawText("The visible appearance box is placed below this text.", 72, y, body); y += 16;
            canvas.DrawText("Open in a PDF viewer to see the signature panel.", 72, y, body);

            // Draw a label arrow pointing to where the sig box will appear
            canvas.DrawText("↓  Signature box appears here  ↓", 72, 670,
                TextStyle.Default.WithFamily("LiberationSans").WithSize(10)
                                 .WithColor(new PdfColor(26, 86, 160)));
            canvas.DrawLine(72, 680, 292, 680,
                StrokeStyle.Default.WithWidth(0.5f).WithColor(new PdfColor(26, 86, 160)).Dashed());
        })
        .Save(Out(name, version));
}

// ── example 23: certificate-based (public-key) encryption ───────────────────
// Encrypts the PDF so only holders of the recipient X.509 certificate private
// key can open it.  /Filter /Adobe.PubSec, V=4, AES-128.

static void PublicKeyEncryptionExample(string name, FontRegistry fonts, PdfVersion version)
{
    // Use the same self-signed cert as both the signer and the sole recipient
    // so the example can be run without an external key store.
    // In production, supply your recipients' real public-key certificates.
    using var recipientCert = CreateSelfSignedCertForEncryption();

    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(18).WithBold();
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(11);
    var mono    = TextStyle.Default.WithFamily("LiberationMono").WithSize(10);
    var small   = TextStyle.Default.WithFamily("LiberationSans").WithSize(9)
                      .WithColor(PdfColor.Gray).WithItalic();

    var security = PdfPublicKeySecurity.ForRecipients(recipientCert)
        .WithPermissions(PdfPermissions.Print | PdfPermissions.CopyText);

    PdfDocument.Create()
        .WithVersion(version)
        .WithFontRegistry(fonts)
        .WithTitle("Public-Key Encrypted PDF")
        .WithPublicKeySecurity(security)
        .AddPage(PageSizes.A4, canvas =>
        {
            float y = 60;

            canvas.DrawText("Certificate-Based Encryption", 72, y, heading); y += 32;
            canvas.DrawText("This document is encrypted with /Filter /Adobe.PubSec (V=4, AES-128).", 72, y, body); y += 20;
            canvas.DrawText("Only holders of the designated recipient certificate's private key can open it.", 72, y, body); y += 30;

            canvas.DrawText("How it works:", 72, y, body.WithBold()); y += 18;
            void Bullet(string text) { canvas.DrawText("•  " + text, 88, y, body); y += 18; }

            Bullet("A 20-byte random seed is generated per document");
            Bullet("The seed is encrypted to each recipient via CMS EnvelopedData (RSA)");
            Bullet("The file encryption key (FEK) = first 16 bytes of SHA-1(seed ∥ all blobs)");
            Bullet("All streams and strings are AES-128-CBC encrypted with per-object keys");
            y += 10;

            canvas.DrawText("Usage:", 72, y, body.WithBold()); y += 18;
            canvas.DrawText("var cert = new X509Certificate2(\"recipient.cer\");", 88, y, mono); y += 14;
            canvas.DrawText("PdfDocument.Create()", 88, y, mono); y += 14;
            canvas.DrawText("    .WithPublicKeySecurity(", 88, y, mono); y += 14;
            canvas.DrawText("        PdfPublicKeySecurity.ForRecipients(cert))", 88, y, mono); y += 14;
            canvas.DrawText("    .AddPage(...)", 88, y, mono); y += 14;
            canvas.DrawText("    .Save(\"encrypted.pdf\");", 88, y, mono); y += 22;

            canvas.DrawText("Multiple recipients are supported:", 72, y, body); y += 18;
            canvas.DrawText("PdfPublicKeySecurity.ForRecipients(cert1, cert2, cert3)", 88, y, mono); y += 22;

            canvas.DrawText("Note: this example uses a self-signed certificate generated at runtime.", 72, y, small); y += 14;
            canvas.DrawText("In production supply real certificates from a trusted CA.", 72, y, small);
        })
        .Save(Out(name, version));
}

static X509Certificate2 CreateSelfSignedCertForEncryption()
{
    using var rsa = RSA.Create(2048);
    var req = new CertificateRequest(
        "CN=Majorsilence Example Recipient",
        rsa,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
    req.CertificateExtensions.Add(new X509KeyUsageExtension(
        X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, false));
    var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
        DateTimeOffset.UtcNow.AddYears(1));
    var pfx = cert.Export(X509ContentType.Pfx);
    return X509CertificateLoader.LoadPkcs12(pfx, (string?)null,
        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
}

// ── utility ───────────────────────────────────────────────────────────────────

static string Out(string name, PdfVersion version)
{
    string vLabel = version == PdfVersion.Pdf20 ? "v2.0" : "v1.4";
    return Path.Combine(AppContext.BaseDirectory, "output", $"{name}_{vLabel}.pdf");
}

static string? FindFont(params string[] paths)
{
    foreach (var p in paths) if (File.Exists(p)) return p;
    return null;
}
