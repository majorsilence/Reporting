using System.Reflection;
using Majorsilence.Pdf;
using Majorsilence.Pdf.Security;

// Checks that only make sense in the browser build. The browser has no system fonts, so a font
// has to arrive as bytes (fetched over HTTP, or here embedded as a resource).
static class WasmChecks
{
    internal static void Run(Action<string, bool> check, Func<byte[], bool> isValidPdf)
    {
        // AES-256 normalizes the password (NFKC); the browser runtime may not support that.
        byte[] unicodePdf = PdfDocument.Create()
            .WithSecurity(PdfSecurity.Protect("pässwörd-ｆｕｌｌ"))
            .AddPage(PageSizes.A4, c => c.DrawText("Unicode password", 72, 72))
            .ToBytes();
        check("AES-256 with a non-ASCII password", isValidPdf(unicodePdf));

        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestFont.ttf");
        if (s == null)
        {
            Console.WriteLine("SKIP: embedded TrueType font (build with -p:WasmTestFont=/path/to/font.ttf)");
            return;
        }
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        byte[] ttf = ms.ToArray();

        var registry = new FontRegistry().AddFamily("Embedded", regular: ttf);
        byte[] pdf = PdfDocument.Create()
            .WithFontRegistry(registry)
            .AddPage(PageSizes.A4, c => c.DrawText("Hello from WebAssembly", 72, 72,
                TextStyle.Default.WithFamily("Embedded").WithSize(18)))
            .ToBytes();

        check("font from bytes embeds into a PDF", isValidPdf(pdf));
        check("embedded font data is in the PDF", System.Text.Encoding.Latin1.GetString(pdf).Contains("FontFile2"));
    }
}
