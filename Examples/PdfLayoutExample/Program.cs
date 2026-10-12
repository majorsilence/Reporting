// PdfLayoutExample — fluent Row/Column cursor layout for Majorsilence.Pdf.
//
// Run:  dotnet run
// Output: invoice.pdf in the output directory
//
// Key patterns shown:
//   - PdfLayout records an element tree; End() paginates the whole document, then draws it
//   - Because the page count is known up front, the footer can say "Page 2 of 3"
//   - Text, Spans and Table split across pages (a long table repeats its header row)
//   - Box adds padding / border / background; Spans mixes bold, colour and links in a paragraph
//   - Bookmark adds an entry to the PDF outline; Image fits a picture into the flow
//   - Row treats its columns as atomic: the taller column decides the row's height
//   - layout.End() hands back the underlying PdfDocument for Save()

using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;

var lineItems = new (string Description, int Qty, decimal UnitPrice)[]
{
    ("Consulting services", 8, 150.00m),
    ("Software license (annual)", 1, 899.00m),
    ("Onboarding support", 2, 200.00m),
};

decimal subtotal = 0m;
foreach (var item in lineItems) subtotal += item.Qty * item.UnitPrice;
decimal tax = subtotal * 0.08m;
decimal total = subtotal + tax;

var titleStyle  = TextStyle.Default.WithSize(22).WithBold();
var labelStyle  = TextStyle.Default.WithSize(9).WithColor(PdfColor.Gray);
var bodyStyle   = TextStyle.Default.WithSize(11);
var boldStyle   = bodyStyle.WithBold();
var footerStyle = TextStyle.Default.WithSize(9).WithColor(PdfColor.Gray);

var doc = PdfDocument.Create()
    .WithTitle("Invoice INV-2026-0042")
    .WithAuthor("ACME Corp");

var layout = PdfLayout.Begin(doc, PageSizes.A4)
    .WithMargins(72)
    .WithFooter((canvas, page, pages) =>
    {
        canvas.DrawText($"Page {page} of {pages}", 486, 780, footerStyle.WithAlignment(TextAlignment.Right));
        canvas.DrawText("ACME Corp — Thank you for your business.", 72, 780, footerStyle);
    }, height: 30f);

// A small generated logo; in real code use PdfImage.FromBytes(File.ReadAllBytes("logo.png")).
var logoPixels = new byte[64 * 64 * 3];
for (int y = 0; y < 64; y++)
    for (int x = 0; x < 64; x++)
    {
        int i = (y * 64 + x) * 3;
        logoPixels[i] = (byte)(x * 4); logoPixels[i + 1] = (byte)(y * 4); logoPixels[i + 2] = 160;
    }
var logo = PdfImage.FromRgb24(logoPixels, 64, 64);

layout
    .Bookmark("Invoice")
    .Image(logo, width: 48)
    .Spacer(6)
    .Text("ACME Corp", titleStyle)
    .Spacer(4)
    .Text("123 Business Ave, Toronto, ON", bodyStyle)
    .Spacer(16)
    .Row(row =>
    {
        row.Column(0.6f, col => col
            .Text("BILL TO", labelStyle)
            .Text("Contoso Ltd.", bodyStyle)
            .Text("456 Client Street, Vancouver, BC", bodyStyle));
        row.Column(0.4f, col => col
            .Text("INVOICE #", labelStyle)
            .Text("INV-2026-0042", bodyStyle)
            .Text("Due: July 18, 2026", bodyStyle));
    })
    .Spacer(12)
    .Line();

var table = new PdfTable(new float[] { 260, 60, 90, 90 })
    .WithHeaderBackground(PdfColor.FromRgb(230, 230, 230))
    .WithBorder(PdfColor.Gray, 0.5f)
    .WithCellPadding(6f);

table.AddRow("Description", "Qty", "Unit Price", "Amount");
foreach (var item in lineItems)
{
    table.AddRow(item.Description, item.Qty.ToString(),
        item.UnitPrice.ToString("C"), (item.Qty * item.UnitPrice).ToString("C"));
}

layout
    .Table(table)
    .Spacer(12)
    .Row(row =>
    {
        row.Column(0.7f, col => col.Text(""));
        row.Column(0.3f, col => col
            .Text($"Subtotal: {subtotal:C}", bodyStyle.WithAlignment(TextAlignment.Right))
            .Text($"Tax (8%): {tax:C}", bodyStyle.WithAlignment(TextAlignment.Right))
            .Text($"Total: {total:C}", boldStyle.WithAlignment(TextAlignment.Right)));
    })
    .Spacer(24)
    .Bookmark("Terms and Conditions")
    .Text("Terms and Conditions", titleStyle.WithSize(14))
    .Spacer(8)
    .Box(LayoutBoxStyle.Default.WithPadding(8).WithBackground(PdfColor.FromRgb(245, 245, 245)).WithBorder(PdfColor.LightGray, 0.5f), box => box
        .Spans(s => s
            .Span("Questions about this invoice? ")
            .Span("Email billing@acme.example", boldStyle)
            .Span(" or visit ")
            .Link("our support page", "https://example.com/support", bodyStyle.WithColor(PdfColor.Blue)))
    )
    .Spacer(8)
    .Text(string.Join(" ", Enumerable.Repeat(
        "Payment is due within 14 days of the invoice date. Late payments are subject to a " +
        "1.5% monthly service charge. Goods and services described above are provided subject " +
        "to our standard terms of service, available on request. Any disputes regarding this " +
        "invoice must be raised in writing within 30 days of receipt.", 20)), bodyStyle);

string outPath = Path.Combine(AppContext.BaseDirectory, "invoice.pdf");
layout.End().Save(outPath);

Console.WriteLine($"Written: {outPath}");
