# Majorsilence.Spreadsheet

A zero-dependency library for writing and reading `.xlsx` (Office Open XML SpreadsheetML) files. It needs no Excel installation, Office interop or third-party package. It is Native AOT compatible and targets .NET Standard 2.0, .NET 6, .NET 8 and .NET 10.

```
dotnet add package Majorsilence.Spreadsheet
```

Row and column indexes are **0-based** throughout (row 0, column 0 is `A1`).

## Writing a workbook

```csharp
using Majorsilence.Spreadsheet;

var writer = new XlsxWriter("Sales");

var header = new XlsxCellFormat { Bold = true, HasBackground = true, BackgroundColor = (221, 235, 247) };
writer.SetCell(0, 0, "Product", header);
writer.SetCell(0, 1, "Amount", header);
writer.SetCell(0, 2, "Date", header);

var money = new XlsxCellFormat { NumberFormatCode = XlsxNumberFormats.Currency };
writer.SetCell(1, 0, "Apples");
writer.SetCell(1, 1, 1234.5, money);                    // a real number, so Excel can sum it
writer.SetCell(1, 2, new DateTime(2024, 3, 15));        // gets a date format automatically
writer.SetFormula(2, 1, "SUM(B2:B2)", cachedValue: 1234.5, format: money);

writer.SetColumnWidth(0, 20);
writer.FreezePanes(1, 0);                               // keep the header row visible
writer.SetAutoFilter(0, 0, 1, 2);

using var file = File.Create("sales.xlsx");
writer.Write(file);
```

### Cells and formats

| Method | Writes |
|---|---|
| `SetCell(row, col, string)` | text (inline, or shared strings with `UseSharedStrings = true`) |
| `SetCell(row, col, double)` | a number |
| `SetCell(row, col, bool)` | a boolean |
| `SetCell(row, col, DateTime)` | a date serial; without a format it gets a short date (or date and time) format |
| `SetFormula(row, col, formula, cachedValue)` | a formula; the cached value is shown until Excel recalculates |
| `SetRichText(row, col, runs)` | text whose runs have their own font, size, colour, bold, italic, and so on |

Every `Set*` method takes an optional `XlsxCellFormat` for:
- the font (name, size, bold, italic, underline, strike, colour)
- the background colour
- each border side
- alignment, wrapping and rotation
- `NumberFormatCode` (any Excel format code, or a preset in `XlsxNumberFormats`)
- `Locked`/`FormulaHidden`, which apply on protected sheets

Equal formats are written once.

```csharp
writer.SetRichText(3, 0, new[]
{
    new XlsxTextRun("Total ") { Bold = true },
    new XlsxTextRun("(before tax)") { Italic = true, Color = (128, 128, 128) },
});
```

### Layout

```csharp
writer.AddMerge(0, 0, 0, 3);                            // A1:D1
writer.SetRowHeight(0, 24);
writer.SetRowOutlineLevel(5, 1);                        // group rows
writer.SetColumnHidden(4, true);
writer.SetPrintSetup(0.5, 0.5, 0.5, 0.5, landscape: true);
writer.AddImage(File.ReadAllBytes("logo.jpg"), 0, 0, 120 * XlsxUnits.EmuPerPoint, 40 * XlsxUnits.EmuPerPoint);   // JPEG
writer.AddLine(0, 0, 200 * XlsxUnits.EmuPerPoint, 0, lineWidthPt: 1);
```

### Sheets, names and links

```csharp
int lookup = writer.AddSheet("Lookup");
writer.SetCell(0, 0, "Yes", sheetIndex: lookup);
writer.SetCell(1, 0, "No", sheetIndex: lookup);

writer.AddDefinedName("Answers", lookup, 0, 0, 1, 0);   // 'Lookup'!$A$1:$A$2
writer.AddListValidation(1, 3, 100, 3, "Answers");      // dropdown from the named range
writer.AddListValidation(1, 4, 100, 4, new[] { "Low", "Medium", "High" });

writer.AddHyperlink(1, 0, "https://example.com");
writer.AddInternalHyperlink(2, 0, "'Lookup'!A1");
```

Every sheet-level method takes an optional `sheetIndex` (default 0, the first sheet).

### Conditional formatting, comments, tables and protection

```csharp
writer.AddConditionalFormat(1, 1, 100, 1, XlsxConditionOperator.GreaterThan, 1000,
    new XlsxDifferentialFormat { Bold = true, FontColor = (0, 97, 0), BackgroundColor = (198, 239, 206) });
writer.AddConditionalFormatExpression(1, 0, 100, 2, "$B2<0",
    new XlsxDifferentialFormat { FontColor = (192, 0, 0) });

writer.AddComment(1, 1, "Includes the March adjustment", author: "Finance");

writer.AddTable(0, 0, 1, 2, name: "Sales", styleName: "TableStyleMedium2");

var editable = new XlsxCellFormat { Locked = false };
writer.SetCell(5, 1, 0.0, editable);
writer.ProtectSheet(new XlsxSheetProtection { Password = "secret", AllowSort = true, AllowAutoFilter = true });
```

A table's column names come from its header row. A password is stored as a salted SHA-512 hash, as current Excel does. Sheet protection only discourages edits; it is not encryption.

### Fitting columns to their contents

```csharp
writer.AutoFitColumns(minWidth: 8, maxWidth: 60);
```

This measures what each cell shows (numbers and dates through their number format), scaled for font size and bold. Call it after writing the cells.

## Large exports: `XlsxStreamWriter`

`XlsxWriter` keeps the workbook in memory until `Write`. For very large exports, `XlsxStreamWriter` writes each row to the output as it is added:

```csharp
using var file = File.Create("big.xlsx");
using (var w = new XlsxStreamWriter(file, "Data"))
{
    w.SetColumnWidth(0, 30);        // column widths and frozen panes come before the first row
    w.FreezePanes(1, 0);

    w.StartRow(0);
    w.WriteCell(0, "Name");
    w.WriteCell(1, "Value");

    for (int r = 1; r <= 1_000_000; r++)
    {
        w.StartRow(r);              // rows, and cells within a row, in increasing order
        w.WriteCell(0, "Item " + r);
        w.WriteCell(1, r * 0.5);
    }
}                                   // Dispose (or Finish) completes the file; the stream stays open
```

It supports text, numbers, booleans, dates, formulas, cell formats, multiple sheets, merges, AutoFilter and print setup. Use `XlsxWriter` for the other features.

## Legacy Excel 97-2003 files: `XlsWriter`

For software that can't open `.xlsx`, `XlsWriter` writes the binary `.xls` format (BIFF8 in an OLE2 compound file). Its API is shaped like `XlsxWriter`'s, and it takes the same `XlsxCellFormat`:

```csharp
var xls = new XlsWriter("Sales");
xls.SetCell(0, 0, "Product", new XlsxCellFormat { Bold = true });
xls.SetCell(1, 0, "Apples");
xls.SetCell(1, 1, 1234.5, new XlsxCellFormat { NumberFormatCode = "#,##0.00" });
xls.SetCell(1, 2, new DateTime(2024, 3, 15));
xls.AddMerge(3, 0, 3, 2);
xls.SetColumnWidth(0, 20);
xls.FreezePanes(1, 0);

using var legacy = File.Create("sales.xls");
xls.Write(legacy);
```

It supports:
- text, numbers, booleans and dates
- fonts, fills, borders, alignment, number formats and the `Locked`/`FormulaHidden` flags
- merges, column widths and row heights
- outlining and hidden rows/columns
- frozen panes, print setup and multiple sheets

The format's own limits apply:
- 65,536 rows and 256 columns (`XlsWriter.MaxRows`/`MaxColumns`); writing outside them throws
- about 4,000 distinct cell formats
- 56 colours: a colour beyond the first 56 maps to the nearest one in use
- sheet names of up to 31 characters: longer names are cut, and characters Excel forbids are replaced

Formulas, rich text, images, comments, tables, validation, conditional formatting and hyperlinks are `.xlsx` only.

## Reading a workbook

```csharp
using var reader = XlsxReader.Open("sales.xlsx");
foreach (string name in reader.SheetNames)
    Console.WriteLine(name);

XlsxReadSheet sheet = reader.ReadSheet("Sales");
foreach (XlsxReadCell cell in sheet.Cells)
    Console.WriteLine($"{cell.Row},{cell.Column}: {cell.Type} {cell.DisplayText}");

XlsxReadCell? amount = sheet.GetCell(1, 1);
double? value = amount?.Number;               // 1234.5
string? shown = amount?.DisplayText;          // "$1,234.50"
```

Each cell has the following properties:
- `Type`: Blank, String, Number, Boolean, Error or Date
- `Text`
- `Number`, `Boolean` and `DateValue`
- `Formula`
- `NumberFormatCode`
- `DisplayText`: the value as Excel shows it, using en-US conventions

Strings are resolved from shared strings, inline strings and rich text. Dates are recognised from their number format, including in workbooks that use the 1904 date system. `sheet.Merges` lists the merged ranges.

`XlsxNumberFormatter.Format(value, formatCode)` applies an Excel format code to any number on its own:

```csharp
XlsxNumberFormatter.Format(0.256, "0.0%");                  // "25.6%"
XlsxNumberFormatter.Format(45366.5625, "dddd h:mm AM/PM");  // "Friday 1:30 PM"
```

## License

MIT OR Apache-2.0 OR BSD-3-Clause, at your option.
