/*
 * Dynamic MS Access Report Example
 * 
 * This example demonstrates how to dynamically change the database location at runtime.
 * Shows how to generate reports from multiple different Access databases.
 * 
 * Prerequisites:
 * - Windows OS
 * - Microsoft Access Database Engine 2016 Redistributable
 * - One or more Access database files
 * 
 * Usage:
 * 1. Update the database paths in the example
 * 2. Run the program to see reports generated from different databases
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlCreator;
using Majorsilence.Reporting.Rdl;

namespace AccessReportingExamples
{
    class DynamicAccessReport
    {
        static async Task Main(string[] args)
        {
            // Initialize the reporting engine
            RdlEngineConfig.RdlEngineConfigInit();

            Console.WriteLine("=== Dynamic MS Access Report Example ===\n");
            Console.WriteLine("This example shows how to change database locations dynamically.\n");

            // Example 1: Generate reports from multiple databases
            await GenerateReportsFromMultipleDatabases();

            // Example 2: User-specified database path
            await GenerateReportFromUserPath();

            // Example 3: Auto-detect database format
            await GenerateReportWithAutoDetection();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Generate reports from multiple different Access databases
        /// </summary>
        static async Task GenerateReportsFromMultipleDatabases()
        {
            Console.WriteLine("--- Example 1: Multiple Databases ---");

            // Define multiple database paths
            var databases = new[]
            {
                new { 
                    Path = @"C:\Data\Customers.accdb", 
                    Query = "SELECT TOP 5 * FROM Customers",
                    Title = "Customer Report",
                    OutputFile = "CustomersReport.pdf"
                },
                new { 
                    Path = @"C:\Data\Sales.accdb", 
                    Query = "SELECT TOP 5 * FROM Sales",
                    Title = "Sales Report",
                    OutputFile = "SalesReport.pdf"
                },
                new { 
                    Path = @"C:\Data\Inventory.accdb", 
                    Query = "SELECT TOP 5 * FROM Products",
                    Title = "Inventory Report",
                    OutputFile = "InventoryReport.pdf"
                }
            };

            foreach (var db in databases)
            {
                try
                {
                    // Check if database exists
                    if (!File.Exists(db.Path))
                    {
                        Console.WriteLine($"  SKIPPED: {db.Title} - Database not found at {db.Path}");
                        continue;
                    }

                    Console.WriteLine($"  Generating {db.Title}...");

                    // Build connection string dynamically for each database
                    string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={db.Path};";

                    // Generate report
                    var report = await GenerateReportFromAccessDb(
                        connectionString, 
                        db.Query, 
                        db.Title);

                    // Save to PDF
                    string outputPath = Path.Combine(Environment.CurrentDirectory, db.OutputFile);
                    var fileStream = new OneFileStreamGen(outputPath, true);
                    await report.RunGetData(null);
                    await report.RunRender(fileStream, OutputPresentationType.PDF);

                    Console.WriteLine($"  SUCCESS: {db.OutputFile} created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR generating {db.Title}: {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Generate report from user-specified database path
        /// </summary>
        static async Task GenerateReportFromUserPath()
        {
            Console.WriteLine("--- Example 2: User-Specified Path ---");

            try
            {
                // In a real application, you might get this from:
                // - Command line arguments
                // - User input dialog
                // - Configuration file
                // - Environment variable
                
                Console.Write("  Enter database path (or press Enter to skip): ");
                string userPath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userPath))
                {
                    Console.WriteLine("  SKIPPED: No path provided\n");
                    return;
                }

                if (!File.Exists(userPath))
                {
                    Console.WriteLine($"  ERROR: File not found at {userPath}\n");
                    return;
                }

                Console.Write("  Enter table name: ");
                string tableName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(tableName))
                    tableName = "YourTable";

                // Build connection string from user path
                string connectionString = BuildConnectionString(userPath);
                
                Console.WriteLine("  Generating report...");
                
                var report = await GenerateReportFromAccessDb(
                    connectionString,
                    $"SELECT * FROM {tableName}",
                    "User-Specified Database Report");

                string outputPath = "UserDatabaseReport.pdf";
                var fileStream = new OneFileStreamGen(outputPath, true);
                await report.RunGetData(null);
                await report.RunRender(fileStream, OutputPresentationType.PDF);

                Console.WriteLine($"  SUCCESS: Report created as {outputPath}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Generate report with automatic format detection
        /// </summary>
        static async Task GenerateReportWithAutoDetection()
        {
            Console.WriteLine("--- Example 3: Auto-Detect Format ---");

            string[] testPaths = {
                @"C:\Data\OldFormat.mdb",    // Access 2003
                @"C:\Data\NewFormat.accdb"   // Access 2007+
            };

            foreach (var dbPath in testPaths)
            {
                try
                {
                    if (!File.Exists(dbPath))
                    {
                        Console.WriteLine($"  SKIPPED: {Path.GetFileName(dbPath)} - Not found");
                        continue;
                    }

                    Console.WriteLine($"  Processing {Path.GetFileName(dbPath)}...");

                    // Auto-detect format and build appropriate connection string
                    string connectionString = BuildConnectionString(dbPath);
                    Console.WriteLine($"    Detected format: {(dbPath.EndsWith(".mdb") ? "Access 2003" : "Access 2007+")}");

                    var report = await GenerateReportFromAccessDb(
                        connectionString,
                        "SELECT TOP 5 * FROM TableName",  // Update as needed
                        $"Report from {Path.GetFileNameWithoutExtension(dbPath)}");

                    string outputPath = $"{Path.GetFileNameWithoutExtension(dbPath)}_Report.pdf";
                    var fileStream = new OneFileStreamGen(outputPath, true);
                    await report.RunGetData(null);
                    await report.RunRender(fileStream, OutputPresentationType.PDF);

                    Console.WriteLine($"    SUCCESS: {outputPath} created");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ERROR: {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Helper method to generate report from Access database
        /// </summary>
        static async Task<Report> GenerateReportFromAccessDb(
            string connectionString, 
            string query, 
            string title)
        {
            var create = new Create();
            
            var report = await create.GenerateRdl(
#if WINDOWS
                DataProviders.OleDb,
#else
                "OLEDB",
#endif
                connectionString,
                query,
                pageHeaderText: title);

            return report;
        }

        /// <summary>
        /// Helper method to build connection string based on file extension
        /// </summary>
        static string BuildConnectionString(string databasePath)
        {
            string extension = Path.GetExtension(databasePath).ToLower();
            string provider;

            switch (extension)
            {
                case ".mdb":
                    // Access 2003 and earlier
                    provider = "Microsoft.Jet.OLEDB.4.0";
                    break;
                case ".accdb":
                    // Access 2007 and later
                    provider = "Microsoft.ACE.OLEDB.12.0";
                    break;
                default:
                    // Default to ACE provider
                    provider = "Microsoft.ACE.OLEDB.12.0";
                    break;
            }

            return $@"Provider={provider};Data Source={databasePath};";
        }

        /// <summary>
        /// Helper method to build connection string with password
        /// </summary>
        static string BuildConnectionStringWithPassword(string databasePath, string password)
        {
            string baseConnectionString = BuildConnectionString(databasePath);
            return $"{baseConnectionString}Jet OLEDB:Database Password={password};";
        }
    }
}
