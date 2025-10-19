using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;

namespace DirectExportExample
{
    /// <summary>
    /// This example demonstrates how to use the Report class directly to export reports
    /// to various formats (PDF, Excel, CSV, etc.) without using a viewer.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== My-FyiReporting Direct Export Example ===\n");

            // Initialize the RDL Engine configuration
            RdlEngineConfig.RdlEngineConfigInit();

            // Define the report file path
            string reportPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "..",
                "SqliteExamples", "SimpleTest1.rdl");

            if (!File.Exists(reportPath))
            {
                Console.WriteLine($"Report file not found: {reportPath}");
                Console.WriteLine("Please ensure the report file exists.");
                return;
            }

            Console.WriteLine($"Loading report: {reportPath}\n");

            // Create output directory
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
            Directory.CreateDirectory(outputDir);

            try
            {
                // Example 1: Export to PDF
                await ExportToPdf(reportPath, outputDir);

                // Example 2: Export to Excel
                await ExportToExcel(reportPath, outputDir);

                // Example 3: Export to CSV
                await ExportToCsv(reportPath, outputDir);

                // Example 4: Export with parameters
                await ExportWithParameters(reportPath, outputDir);

                // Example 5: Export to memory (byte array)
                await ExportToMemory(reportPath);

                Console.WriteLine("\n=== All exports completed successfully! ===");
                Console.WriteLine($"Output files saved to: {outputDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Example 1: Export report directly to PDF
        /// </summary>
        static async Task ExportToPdf(string reportPath, string outputDir)
        {
            Console.WriteLine("1. Exporting to PDF...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();

            if (report.ErrorMaxSeverity > 4)
            {
                Console.WriteLine("   ERROR: Report has severe errors and cannot be rendered.");
                return;
            }

            // Set report properties
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to PDF
            string outputPath = Path.Combine(outputDir, "report_output.pdf");
            await report.Export(OutputPresentationType.PDF, outputPath);

            Console.WriteLine($"   ✓ PDF exported to: {outputPath}\n");
        }

        /// <summary>
        /// Example 2: Export report to Excel 2007 format
        /// </summary>
        static async Task ExportToExcel(string reportPath, string outputDir)
        {
            Console.WriteLine("2. Exporting to Excel...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to Excel
            string outputPath = Path.Combine(outputDir, "report_output.xlsx");
            await report.Export(OutputPresentationType.Excel2007, outputPath);

            Console.WriteLine($"   ✓ Excel exported to: {outputPath}\n");
        }

        /// <summary>
        /// Example 3: Export report to CSV format
        /// </summary>
        static async Task ExportToCsv(string reportPath, string outputDir)
        {
            Console.WriteLine("3. Exporting to CSV...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to CSV
            string outputPath = Path.Combine(outputDir, "report_output.csv");
            await report.Export(OutputPresentationType.CSV, outputPath);

            Console.WriteLine($"   ✓ CSV exported to: {outputPath}\n");
        }

        /// <summary>
        /// Example 4: Export report with parameters
        /// </summary>
        static async Task ExportWithParameters(string reportPath, string outputDir)
        {
            Console.WriteLine("4. Exporting with parameters...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Create parameters dictionary
            // Note: The actual parameters depend on your report definition
            ListDictionary parameters = new ListDictionary();
            // Example: parameters.Add("EmployeeID", "5");
            // Example: parameters.Add("StartDate", "2024-01-01");

            // Export to PDF with parameters
            string outputPath = Path.Combine(outputDir, "report_with_params.pdf");
            await report.Export(OutputPresentationType.PDF, outputPath, parameters);

            Console.WriteLine($"   ✓ PDF with parameters exported to: {outputPath}\n");
        }

        /// <summary>
        /// Example 5: Export report to memory (byte array) for in-memory processing
        /// </summary>
        static async Task ExportToMemory(string reportPath)
        {
            Console.WriteLine("5. Exporting to memory (byte array)...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to memory as byte array
            byte[] pdfBytes = await report.ExportToMemory(OutputPresentationType.PDF);

            Console.WriteLine($"   ✓ PDF exported to memory: {pdfBytes.Length} bytes");
            Console.WriteLine($"   This byte array can be sent via HTTP, saved to database, etc.\n");
        }
    }
}
