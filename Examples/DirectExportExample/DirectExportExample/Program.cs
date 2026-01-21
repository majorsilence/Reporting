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
            // In a real application, you would typically get this from configuration
            string reportPath = "/home/runner/work/My-FyiReporting/My-FyiReporting/Examples/SqliteExamples/SimpleTest1.rdl";
            
            // Or use a relative path from the project root
            // string reportPath = Path.Combine(
            //     Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            //     "..", "..", "..", "..", "..", "..",
            //     "Examples", "SqliteExamples", "SimpleTest1.rdl");
            // reportPath = Path.GetFullPath(reportPath);

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
                // Example 1: Export to PDF (may not work on Linux without System.Drawing support)
                try
                {
                    await ExportToPdf(reportPath, outputDir);
                }
                catch (Exception ex) when (ex.Message.Contains("Gdip") || ex.Message.Contains("System.Drawing"))
                {
                    Console.WriteLine("1. Exporting to PDF...");
                    Console.WriteLine($"   ⚠ PDF export skipped: System.Drawing not supported on this platform");
                    Console.WriteLine($"   (This is expected on Linux without libgdiplus)\n");
                }

                // Example 2: Export to Excel (may not work on Linux without System.Drawing support)
                try
                {
                    await ExportToExcel(reportPath, outputDir);
                }
                catch (Exception ex) when (ex.Message.Contains("Gdip") || ex.Message.Contains("System.Drawing"))
                {
                    Console.WriteLine("2. Exporting to Excel...");
                    Console.WriteLine($"   ⚠ Excel export skipped: System.Drawing not supported on this platform\n");
                }

                // Example 3: Export to CSV (works on all platforms)
                await ExportToCsv(reportPath, outputDir);

                // Example 4: Export to XML (works on all platforms)
                await ExportToXml(reportPath, outputDir);

                // Example 5: Export with parameters
                await ExportWithParameters(reportPath, outputDir);

                // Example 6: Export to memory (byte array) - CSV version
                await ExportToMemoryCsv(reportPath);

                Console.WriteLine("\n=== Exports completed! ===");
                Console.WriteLine($"Output files saved to: {outputDir}");
                Console.WriteLine("\nNote: PDF and Excel exports require System.Drawing support.");
                Console.WriteLine("On Linux, install libgdiplus: sudo apt-get install libgdiplus");
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
        /// Example 5: Export report with parameters
        /// </summary>
        static async Task ExportWithParameters(string reportPath, string outputDir)
        {
            Console.WriteLine("5. Exporting with parameters...");

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

            // Export to CSV with parameters (CSV works on all platforms)
            string outputPath = Path.Combine(outputDir, "report_with_params.csv");
            await report.Export(OutputPresentationType.CSV, outputPath, parameters);

            Console.WriteLine($"   ✓ CSV with parameters exported to: {outputPath}\n");
        }

        /// <summary>
        /// Example 5: Export to memory (byte array) for in-memory processing - CSV version
        /// </summary>
        static async Task ExportToMemoryCsv(string reportPath)
        {
            Console.WriteLine("6. Exporting to memory (byte array)...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to memory as byte array (CSV works on all platforms)
            byte[] csvBytes = await report.ExportToMemory(OutputPresentationType.CSV);

            Console.WriteLine($"   ✓ CSV exported to memory: {csvBytes.Length} bytes");
            Console.WriteLine($"   This byte array can be sent via HTTP, saved to database, etc.\n");
        }

        /// <summary>
        /// Example 7: Export to XML format (works on all platforms)
        /// </summary>
        static async Task ExportToXml(string reportPath, string outputDir)
        {
            Console.WriteLine("4. Exporting to XML...");

            // Load the report
            string rdlContent = await File.ReadAllTextAsync(reportPath);
            RDLParser parser = new RDLParser(rdlContent);
            parser.Folder = Path.GetDirectoryName(reportPath);

            Report report = await parser.Parse();
            report.Folder = Path.GetDirectoryName(reportPath);

            // Export to XML
            string outputPath = Path.Combine(outputDir, "report_output.xml");
            await report.Export(OutputPresentationType.XML, outputPath);

            Console.WriteLine($"   ✓ XML exported to: {outputPath}\n");
        }
    }
}
