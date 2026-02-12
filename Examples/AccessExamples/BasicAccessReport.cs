/*
 * Basic MS Access Report Example
 * 
 * This example demonstrates how to create a simple report from a Microsoft Access database.
 * 
 * Prerequisites:
 * - Windows OS
 * - Microsoft Access Database Engine 2016 Redistributable
 * - An Access database file (.accdb or .mdb)
 * 
 * Usage:
 * 1. Update the connectionString variable with your database path
 * 2. Update the SQL query to match your database schema
 * 3. Run the program
 */

using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlCreator;
using Majorsilence.Reporting.Rdl;

namespace AccessReportingExamples
{
    class BasicAccessReport
    {
        static async Task Main(string[] args)
        {
            // Initialize the reporting engine once per application
            RdlEngineConfig.RdlEngineConfigInit();

            Console.WriteLine("=== Basic MS Access Report Example ===\n");

            // Example 1: Report from .accdb file (Access 2007+)
            await GenerateReportFromAccdb();

            // Example 2: Report from .mdb file (Access 2003 and earlier)
            // Uncomment to use:
            // await GenerateReportFromMdb();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Generate a report from an Access 2007+ (.accdb) database
        /// </summary>
        static async Task GenerateReportFromAccdb()
        {
            try
            {
                Console.WriteLine("Generating report from .accdb database...");

                // Connection string for .accdb file
                // IMPORTANT: Update this path to point to your Access database
                string dbPath = @"C:\Data\YourDatabase.accdb";
                string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

                // Validate database file exists
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine($"ERROR: Database file not found at: {dbPath}");
                    Console.WriteLine("Please update the dbPath variable to point to your Access database.");
                    return;
                }

                // Create report generator
                var create = new Create();

                // Generate report from Access database
                // UPDATE the SQL query to match your database schema
                var report = await create.GenerateRdl(
#if WINDOWS
                    DataProviders.OleDb,  // Use OleDb provider for Access (Windows only)
#else
                    "OLEDB",  // Fallback string provider (though this won't work on non-Windows)
#endif
                    connectionString,
                    "SELECT TOP 10 * FROM YourTableName",  // UPDATE THIS QUERY
                    pageHeaderText: "MS Access Report - Basic Example");

                // Export to PDF
                string outputPath = Path.Combine(Environment.CurrentDirectory, "AccessReport.pdf");
                var fileStream = new OneFileStreamGen(outputPath, true);
                
                Console.WriteLine("Running query and generating PDF...");
                await report.RunGetData(null);
                await report.RunRender(fileStream, OutputPresentationType.PDF);

                Console.WriteLine($"SUCCESS: Report generated successfully!");
                Console.WriteLine($"Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"\nFull details:\n{ex}");
            }
        }

        /// <summary>
        /// Generate a report from an Access 2003 (.mdb) database
        /// </summary>
        static async Task GenerateReportFromMdb()
        {
            try
            {
                Console.WriteLine("Generating report from .mdb database...");

                // Connection string for .mdb file
                // IMPORTANT: Update this path to point to your Access database
                string dbPath = @"C:\Data\YourDatabase.mdb";
                string connectionString = $@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={dbPath};";

                // Validate database file exists
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine($"ERROR: Database file not found at: {dbPath}");
                    Console.WriteLine("Please update the dbPath variable to point to your Access database.");
                    return;
                }

                // Create report generator
                var create = new Create();

                // Generate report from Access database
                var report = await create.GenerateRdl(
#if WINDOWS
                    DataProviders.OleDb,
#else
                    "OLEDB",
#endif
                    connectionString,
                    "SELECT TOP 10 * FROM YourTableName",  // UPDATE THIS QUERY
                    pageHeaderText: "MS Access Report - Legacy Format");

                // Export to PDF
                string outputPath = Path.Combine(Environment.CurrentDirectory, "AccessReportLegacy.pdf");
                var fileStream = new OneFileStreamGen(outputPath, true);
                
                Console.WriteLine("Running query and generating PDF...");
                await report.RunGetData(null);
                await report.RunRender(fileStream, OutputPresentationType.PDF);

                Console.WriteLine($"SUCCESS: Report generated successfully!");
                Console.WriteLine($"Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"\nFull details:\n{ex}");
            }
        }
    }
}
