# Release Notes — v26.0

Version **26.0** is a major update focused on modernization.

## Highlights

- Fluent `PdfLayout` for rows, columns, and auto-pagination
- Markdown rendering via the new `Majorsilence.Pdf.Markdown` package
- Native AOT verified in CI, not just checked for warnings
- A live-reload preview tool: `mspdf-preview`

## Upgrade steps

1. Update your package references to `26.0.0` or later
2. Review the [migration notes](https://github.com/majorsilence/Reporting/wiki/Migration-to-v5) if upgrading from an older major version
3. Run your test suite

## Example

Rendering this exact file is what produced the PDF you're looking at:

```csharp
using Majorsilence.Pdf;
using Majorsilence.Pdf.Layout;
using Majorsilence.Pdf.Markdown;

var doc = PdfDocument.Create().WithTitle("Release Notes");
var layout = PdfLayout.Begin(doc, PageSizes.A4).WithMargins(36);
layout.Markdown(File.ReadAllText("sample.md"));
layout.End().Save("release-notes.pdf");
```

| Package | New in v26.0 |
|---|---|
| Majorsilence.Pdf | PdfLayout |
| Majorsilence.Pdf.Markdown | yes (new package) |
| Majorsilence.Pdf.Previewer | yes (new package) |

---

Thanks for reading — see the [full changelog](https://github.com/majorsilence/Reporting/releases) for every change.
