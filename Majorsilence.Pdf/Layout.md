# PdfLayout guide

`PdfLayout` is a flow layout on top of `PdfDocument` and `PdfCanvas`. Your calls record an **element tree**; `End()` measures and paginates all of it, then draws every page. Two consequences:

- Content that is too tall for the space left **splits** and continues on the next page.
- Nothing is drawn until the whole document is known, so headers and footers can show the total page count.

```csharp
var doc = PdfDocument.Create();
var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(54);
layout.Text("Hello").Table(table).Spans(...);
layout.End().Save("out.pdf");
```

Every builder method exists on `PdfLayout` and on `LayoutColumn` (the object passed to the callbacks of `Row`, `Box`, `Decoration`…), so anything can nest in anything. Callbacks run **once**, when you make the call; they only record elements, so side effects inside them are safe.

## Content

| Call | What it adds | Splits across pages? |
|---|---|---|
| `Text(text, style)` | word-wrapped text | between lines |
| `Spans(s => …)` | mixed-style text: `Span`, `Link`, `GoTo`, `LineBreak`, `PageNumber`, `TotalPages` | between lines |
| `Table(table)` | a `PdfTable` | between rows; header row(s) repeat; never cuts a row span |
| `Image(img, width, height, fit, alignment)` | a `PdfImage` | no |
| `Row(r => r.Column(0.5f, c => …))` | side-by-side columns (fraction or `LayoutColumn.Fixed(points)`) | no (atomic) |
| `Box(style, c => …)` | padding / border / background / sizing around content | with its content |
| `KeepTogether(c => …)` | content that moves to the next page whole | no |
| `Decoration(header, content, footer)` | content with a header and footer repeated on every page it spans | with its content |
| `Layers(l => l.Behind(…).Primary(…).InFront(…))` | stacked layers; the primary layer sets the size | with the primary layer |
| `Inlined(i => i.Item(…))` | items flowing left to right and wrapping | between lines of items |
| `Bullet(marker, indent, c => …)` | indented content with a hanging marker | with its content |
| `When(() => cond, c => …)` | content included only if the condition is true at pagination | with its content |
| `Component(new MyComponent())` | an `ILayoutComponent` reused anywhere | with its content |
| `Spacer(h)`, `Line()`, `PageBreak()`, `EnsureSpace(h)` | space, a rule, a forced break, "break unless *h* points remain" | — |
| `Canvas((canvas, rect) => …, height)` | a region you draw yourself | no |
| `Bookmark(title, level)`, `Destination(name)` | outline entry / named jump target at this position | — |
| `Markdown(md)` | (in `Majorsilence.Pdf.Markdown`) markdown, split like any other content | between blocks and lines |

**Orphan and widow control.** A paragraph never leaves a single line alone at the bottom of a page or the top of the next. If a page is too short to honour that, the rule is dropped for that paragraph rather than failing.

**Atomic elements** (rows, images, canvas regions, keep-together boxes) that are taller than a whole page's content area throw `InvalidOperationException` at the call that added them.

## Boxes and sizing

```csharp
var card = LayoutBoxStyle.Default
    .WithPadding(8)
    .WithBorder(PdfColor.Gray, 0.75f)
    .WithBackground(PdfColor.FromRgb(245, 245, 245))
    .WithMaxWidth(300)                      // or WithWidth / WithMinWidth / WithShrink()
    .WithAlignment(TextAlignment.Center);   // where a narrower box sits
layout.Box(card, c => c.Text("Centered card"));
```

`WithMinHeight`, `WithMaxHeight` and `WithHeight` size vertically (content beyond a maximum continues on the next page). `WithShrink()` makes the box as wide as its content. `WithKeepTogether()` stops it splitting.

## Headers, footers and page layers

```csharp
PdfLayout.Begin(doc, PageSizes.A4)
    .WithHeaderContent(c => c.Spans(s => s.Span("ACME — Confidential")), height: 28)
    .WithFooterText("Page {page} of {pages}")                       // quick form
    .WithFooterContent(c => c.Spans(s => s.Span("p. ").PageNumber().Span(" / ").TotalPages()))
    .WithFooter((canvas, page, total) => canvas.DrawText($"{page}/{total}", 500, 810))   // drawing callback
    .WithBackground((canvas, page, total) => DrawWatermark(canvas))
    .WithForeground((canvas, page, total) => DrawStamp(canvas));
```

Configure these before the first content call. The page-number placeholders inside `Spans` are measured two digits wide.

## Repeating headers inside a split element

```csharp
layout.Decoration(
    header: h => h.ShowOnce(t => t.Text("Statement", big)).Text("ACME — account 1234", small),
    content: c => c.Table(transactions),
    footer: f => f.SkipOnce(n => n.Text("continued…")));
```

`ShowOnce` content appears on the first page only; `SkipOnce` content appears on every page but the first.

## Tables

```csharp
var table = new PdfTable(120, 120, 120).WithHeaderBackground(PdfColor.LightGray);
table.AddRow(new PdfTableRow(new PdfTableCell("Region").WithRowSpan(2), new PdfTableCell("2026").WithColumnSpan(2)));
table.AddRow(new PdfTableRow(new PdfTableCell("H1"), new PdfTableCell("H2")));   // column 0 is covered by the row span
```

Cells are placed left to right in the next free column, as in HTML, so a row below a row-spanning cell lists only the cells that start in that row.

## Choosing between the callback API and the element tree

`Row`/`Column` callbacks are the same element tree as everything else, not a separate engine, so they are **not deprecated**: they remain the way to put things side by side. Use `Box`, `Decoration`, `Layers` and `Inlined` where you need containers; there is no need to migrate existing `Row` code.

## Limits

- Text uses the core renderer's shaping: good for Latin, Cyrillic, Greek and CJK, weaker for complex scripts.
- Images: JPEG (gray or YCbCr) and non-interlaced 8-bit PNG. Convert anything else to raw pixels and use `PdfImage.FromRgb24` / `FromRgba`.
- A `Row` is atomic: put long content in a `Box` or directly in the flow instead.
