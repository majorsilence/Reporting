// Majorsilence.Pdf — example program
// Demonstrates the key features of the library using the FontRegistry with
// bundled Liberation / Caladea / Carlito / NotoSans TrueType fonts.

using System;
using System.Collections.Generic;
using System.IO;
using Majorsilence.Pdf;

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

Console.WriteLine($"\nAll PDFs written to: {outputDir}");

// ── runner ────────────────────────────────────────────────────────────────────

void RunExample(string name, Action<string, FontRegistry> example)
{
    Console.Write($"  {name} ... ");
    try   { example(name, registry); Console.WriteLine("OK"); }
    catch (Exception ex) { Console.WriteLine($"SKIPPED ({ex.Message})"); }
}

// ── example 01: hello world ──────────────────────────────────────────────────

static void HelloWorld(string name, FontRegistry fonts)
{
    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 02: text styles ──────────────────────────────────────────────────

static void TextStyles(string name, FontRegistry fonts)
{
    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 03: shapes ───────────────────────────────────────────────────────

static void Shapes(string name, FontRegistry fonts)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);

    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 04: lines and strokes ────────────────────────────────────────────

static void LinesAndStrokes(string name, FontRegistry fonts)
{
    var label = TextStyle.Default.WithFamily("LiberationSans").WithSize(10);

    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 05: multi-page document ─────────────────────────────────────────

static void MultiPage(string name, FontRegistry fonts)
{
    var doc = PdfDocument.Create()
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

    doc.Save(Out(name));
}

// ── example 06: custom font file (WithFontFile) ──────────────────────────────
// Demonstrates embedding a user-supplied TTF directly via TextStyle.WithFontFile().
// Falls back to a system font for portability.

static void CustomFont(string name, FontRegistry fonts)
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
        .Save(Out(name));
}

// ── example 07: image ────────────────────────────────────────────────────────

static void ImageExample(string name, FontRegistry fonts)
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
        .Save(Out(name));
}

// ── example 08: annotations ──────────────────────────────────────────────────

static void Annotations(string name, FontRegistry fonts)
{
    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 09: invoice ──────────────────────────────────────────────────────

static void InvoiceExample(string name, FontRegistry fonts)
{
    float pw = PageSizes.Letter.Width;
    float ph = PageSizes.Letter.Height;
    const float margin = 54f;
    var accent = PdfColor.FromHex("#1A56A0");

    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 10: dashboard / data visualization ───────────────────────────────

static void DashboardExample(string name, FontRegistry fonts)
{
    PdfDocument.Create()
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
        .Save(Out(name));
}

// ── example 11: font registry showcase ───────────────────────────────────────

static void FontRegistryExample(string name, FontRegistry fonts)
{
    var body    = TextStyle.Default.WithFamily("LiberationSans").WithSize(12);
    var heading = TextStyle.Default.WithFamily("LiberationSans").WithSize(20).WithBold();

    PdfDocument.Create()
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
        .Save(Out(name));
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

// ── utility ───────────────────────────────────────────────────────────────────

static string Out(string name) =>
    Path.Combine(AppContext.BaseDirectory, "output", $"{name}.pdf");

static string? FindFont(params string[] paths)
{
    foreach (var p in paths) if (File.Exists(p)) return p;
    return null;
}
