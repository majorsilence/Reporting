# Direct Export Example

This example demonstrates how to use the Report class directly to export reports to various formats (PDF, Excel, CSV, etc.) without using a viewer.

## Overview

The Report class now provides convenient `Export` and `ExportToMemory` methods that allow you to:
- Export reports directly to files in various formats
- Export reports to memory (byte arrays) for in-memory processing
- Pass parameters to reports
- Control the entire report generation process programmatically

## Key Methods

### Export Method
```csharp
public async Task Export(
    OutputPresentationType outputType, 
    string filePath, 
    IDictionary parameters = null)
```

Exports the report to a file with the specified format.

**Parameters:**
- `outputType`: The output format (PDF, Excel2007, CSV, etc.)
- `filePath`: Full path where the file should be saved
- `parameters`: Optional report parameters

### ExportToMemory Method
```csharp
public async Task<byte[]> ExportToMemory(
    OutputPresentationType outputType, 
    IDictionary parameters = null)
```

Exports the report to memory as a byte array.

**Parameters:**
- `outputType`: The output format (PDF, Excel2007, CSV, etc.)
- `parameters`: Optional report parameters

**Returns:** Byte array containing the rendered report

## Supported Output Formats

- `OutputPresentationType.PDF` - PDF format
- `OutputPresentationType.Excel2007` - Excel 2007+ (.xlsx)
- `OutputPresentationType.CSV` - Comma-separated values
- `OutputPresentationType.XML` - XML format
- `OutputPresentationType.HTML` - HTML format
- `OutputPresentationType.RTF` - Rich Text Format
- `OutputPresentationType.ExcelTableOnly` - Excel with table data only

## Usage Examples

### Example 1: Simple PDF Export
```csharp
// Load the report
string rdlContent = await File.ReadAllTextAsync("report.rdl");
RDLParser parser = new RDLParser(rdlContent);
parser.Folder = Path.GetDirectoryName("report.rdl");

Report report = await parser.Parse();
report.Folder = Path.GetDirectoryName("report.rdl");

// Export to PDF
await report.Export(OutputPresentationType.PDF, "output.pdf");
```

### Example 2: Export with Parameters
```csharp
// Create parameters
ListDictionary parameters = new ListDictionary();
parameters.Add("EmployeeID", "5");
parameters.Add("StartDate", "2024-01-01");

// Export with parameters
await report.Export(OutputPresentationType.PDF, "output.pdf", parameters);
```

### Example 3: Export to Memory
```csharp
// Export to byte array
byte[] pdfBytes = await report.ExportToMemory(OutputPresentationType.PDF);

// Use the byte array (e.g., send via HTTP response)
await File.WriteAllBytesAsync("output.pdf", pdfBytes);
```

### Example 4: Export to Multiple Formats
```csharp
// Export to PDF
await report.Export(OutputPresentationType.PDF, "report.pdf");

// Reload report for next export
report = await parser.Parse();
report.Folder = reportFolder;

// Export to Excel
await report.Export(OutputPresentationType.Excel2007, "report.xlsx");

// Reload report for next export
report = await parser.Parse();
report.Folder = reportFolder;

// Export to CSV
await report.Export(OutputPresentationType.CSV, "report.csv");
```

## Running the Example

### Prerequisites
- .NET 8.0 SDK or later
- My-FyiReporting library

### Build and Run
```bash
cd Examples/DirectExportExample/DirectExportExample
dotnet build
dotnet run
```

The example will:
1. Load a sample report
2. Export it to PDF, Excel, and CSV formats
3. Demonstrate parameter passing
4. Show how to export to memory
5. Save all output files to the `Output` directory

## Error Handling

Always check for errors after parsing the report:

```csharp
Report report = await parser.Parse();

if (report.ErrorMaxSeverity > 4)
{
    Console.WriteLine("Report has severe errors:");
    foreach (string error in report.ErrorItems)
    {
        Console.WriteLine(error);
    }
    return;
}
```

## Benefits of Direct Export

1. **No UI Required**: Perfect for server-side report generation
2. **Batch Processing**: Generate multiple reports in a loop
3. **Flexible Output**: Export to file or memory as needed
4. **Web Integration**: Easy integration with web applications
5. **Automated Reporting**: Schedule report generation tasks

## Integration with Web Applications

### ASP.NET Core Example
```csharp
[HttpGet("report/pdf")]
public async Task<IActionResult> GetPdfReport(string reportName)
{
    // Load report
    string rdlPath = Path.Combine(_reportsPath, $"{reportName}.rdl");
    string rdlContent = await System.IO.File.ReadAllTextAsync(rdlPath);
    
    RDLParser parser = new RDLParser(rdlContent);
    parser.Folder = _reportsPath;
    
    Report report = await parser.Parse();
    report.Folder = _reportsPath;
    
    // Export to memory
    byte[] pdfBytes = await report.ExportToMemory(OutputPresentationType.PDF);
    
    // Return as file
    return File(pdfBytes, "application/pdf", $"{reportName}.pdf");
}
```

## Additional Notes

- The `Export` method automatically calls `RunGetData` if parameters are provided
- For multiple exports of the same report, reload the report between exports
- Memory exports are ideal for scenarios where you need to process or transmit the report data
- The report folder must be set correctly for the report to find data sources and subreports

## See Also

- [RdlCmd](../../RdlCmd) - Command-line report generation tool
- [Sample Report Viewer](../Sample-Report-Viewer) - Interactive report viewer example
- [Report Parameters Guide](https://github.com/majorsilence/My-FyiReporting/wiki) - Guide on using report parameters
