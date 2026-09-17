// MarkdownToPdf — render a Markdown file to a paginated PDF with Majorsilence.Pdf.Markdown.
//
// Run:  dotnet run
// Output: release-notes.pdf in the output directory
//
// Key patterns shown:
//   - layout.Markdown(...) renders block-by-block through PdfLayout, so headings, paragraphs,
//     lists, code blocks, and tables all auto-paginate exactly like Row/Table already do
//   - MarkdownStyle customizes heading/body/code styles and the link color
//   - Unsupported constructs (images, blockquotes) are reported via an optional warnings list
//     instead of throwing

using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using Majorsilence.Pdf.Markdown;

string markdown = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sample.md"));

var style = MarkdownStyle.Default
    .WithHeading(1, TextStyle.Default.WithSize(24).WithBold().WithColor(PdfColor.FromRgb(30, 60, 120)))
    .WithLinkColor(PdfColor.Blue);

var doc = PdfDocument.Create().WithTitle("Release Notes");
var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);

var warnings = new List<string>();
layout.Markdown(markdown, style, warnings);

string outPath = Path.Combine(AppContext.BaseDirectory, "release-notes.pdf");
layout.End().Save(outPath);

Console.WriteLine($"Written: {outPath}");
foreach (var warning in warnings)
    Console.WriteLine($"warning: {warning}");
