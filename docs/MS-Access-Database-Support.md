# Microsoft Access Database Support

This document explains how to use Majorsilence Reporting with Microsoft Access databases.

## Overview

Majorsilence Reporting supports Microsoft Access databases through the OleDb data provider. This allows you to create reports from both `.mdb` (Access 2003 and earlier) and `.accdb` (Access 2007 and later) database files.

## Platform Requirements

**Important**: MS Access support via OleDb is only available on Windows platforms. This is because:
- The OleDb provider in the codebase is conditionally compiled for Windows only (`#if WINDOWS`)
- Microsoft Access Database Engine (ACE) or Jet drivers are Windows-only

On Linux and macOS, you would need to use alternative approaches such as:
- Converting your Access database to another format (SQLite, PostgreSQL, etc.)
- Using ODBC with third-party Access ODBC drivers
- Hosting the data through a web service or API

## Prerequisites

To connect to MS Access databases, you need one of the following installed on your Windows machine:

### For .mdb files (Access 2003 and earlier):
- Microsoft Jet Database Engine (usually included with Windows)

### For .accdb files (Access 2007 and later):
- Microsoft Access Database Engine 2016 Redistributable (recommended)
  - [Download 64-bit](https://www.microsoft.com/en-us/download/details.aspx?id=54920)
  - [Download 32-bit](https://www.microsoft.com/en-us/download/details.aspx?id=54920)

**Note**: Install the version (32-bit or 64-bit) that matches your application's architecture, not your operating system.

## Connection Strings

### For .mdb files (Access 2003 and earlier)

```csharp
string connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\path\to\your\database.mdb;";
```

### For .accdb files (Access 2007 and later)

```csharp
string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\path\to\your\database.accdb;";
```

### With Password Protection

```csharp
// For .mdb with password
string connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\path\to\your\database.mdb;Jet OLEDB:Database Password=YourPassword;";

// For .accdb with password
string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\path\to\your\database.accdb;Jet OLEDB:Database Password=YourPassword;";
```

## Basic Usage Example

Here's a complete example showing how to create a report from an MS Access database:

```csharp
using Majorsilence.Reporting.RdlCreator;
using Majorsilence.Reporting.Rdl;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

// Connection string for your Access database
string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\path\to\your\database.accdb;";

// Create report generator
var create = new Create();

// Generate report from Access database
var report = await create.GenerateRdl(
    DataProviders.OleDb,  // Use OleDb provider for Access
    connectionString,
    "SELECT CustomerID, CompanyName, ContactName, City FROM Customers",
    pageHeaderText: "Customer Report");

// Export to PDF
string filepath = Path.Combine(Environment.CurrentDirectory, "AccessReport.pdf");
var fileStream = new OneFileStreamGen(filepath, true);
await report.RunGetData(null);
await report.RunRender(fileStream, OutputPresentationType.PDF);
```

## Changing Database Location Dynamically

One of the key questions from the issue is whether you can change the database location dynamically. **Yes, you can!** Here are several approaches:

### Approach 1: Build Connection String Dynamically

```csharp
using Majorsilence.Reporting.RdlCreator;

// Method to generate reports from different Access databases
public async Task<Report> GenerateReportFromAccessDb(string databasePath, string query)
{
    // Build connection string with the specified path
    string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};";
    
    var create = new Create();
    var report = await create.GenerateRdl(
        DataProviders.OleDb,
        connectionString,
        query,
        pageHeaderText: "Dynamic Report");
    
    return report;
}

// Usage example - create reports from different databases on the fly
var report1 = await GenerateReportFromAccessDb(@"C:\data\customers.accdb", "SELECT * FROM Customers");
var report2 = await GenerateReportFromAccessDb(@"C:\data\sales.accdb", "SELECT * FROM Sales");
var report3 = await GenerateReportFromAccessDb(@"C:\data\inventory.accdb", "SELECT * FROM Products");
```

### Approach 2: Using Configuration or User Input

```csharp
using System.IO;
using Majorsilence.Reporting.RdlCreator;

// Get database path from configuration, user input, or environment
string databasePath = GetDatabasePathFromConfig(); // Your method to get path

// Validate the file exists
if (!File.Exists(databasePath))
{
    throw new FileNotFoundException($"Access database not found at: {databasePath}");
}

// Determine provider based on file extension
string provider = Path.GetExtension(databasePath).ToLower() == ".mdb" 
    ? "Microsoft.Jet.OLEDB.4.0" 
    : "Microsoft.ACE.OLEDB.12.0";

string connectionString = $@"Provider={provider};Data Source={databasePath};";

var create = new Create();
var report = await create.GenerateRdl(
    DataProviders.OleDb,
    connectionString,
    "SELECT * FROM YourTable",
    pageHeaderText: "Report from Dynamic Database");
```

### Approach 3: Using String Provider Directly

Instead of the enum, you can also use the string provider name directly:

```csharp
using Majorsilence.Reporting.RdlCreator;

string databasePath = @"C:\databases\mydatabase.accdb";
string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={databasePath};";

var create = new Create();

// Use string provider name "OLEDB" instead of enum
var report = await create.GenerateRdl(
    "OLEDB",  // String provider name
    connectionString,
    "SELECT * FROM Employees",
    pageHeaderText: "Employee Report");
```

## Working with Temporary or On-the-Fly Databases

If you're creating Access databases on the fly, here's a complete workflow:

```csharp
using System.Data.OleDb;
using System.IO;
using Majorsilence.Reporting.RdlCreator;

// 1. Create a new Access database programmatically
string newDbPath = Path.Combine(Path.GetTempPath(), $"TempDB_{Guid.NewGuid()}.accdb");
CreateNewAccessDatabase(newDbPath);

// 2. Populate it with data
PopulateDatabase(newDbPath);

// 3. Generate report from it
string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={newDbPath};";
var create = new Create();
var report = await create.GenerateRdl(
    DataProviders.OleDb,
    connectionString,
    "SELECT * FROM DataTable",
    pageHeaderText: "Report from Temporary Database");

// 4. Generate PDF
await report.RunGetData(null);
var ms = new MemoryStreamGen();
await report.RunRender(ms, OutputPresentationType.PDF);

// 5. Clean up (optional)
// File.Delete(newDbPath);

// Helper method to create new Access database
void CreateNewAccessDatabase(string dbPath)
{
    var catalog = new ADOX.Catalog();
    catalog.Create($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};");
    System.Runtime.InteropServices.Marshal.ReleaseComObject(catalog);
}

// Helper method to populate database
void PopulateDatabase(string dbPath)
{
    string connStr = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";
    using (var conn = new OleDbConnection(connStr))
    {
        conn.Open();
        
        // Create table
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE DataTable (ID INT, Name TEXT(50), Value DOUBLE)";
            cmd.ExecuteNonQuery();
        }
        
        // Insert data
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO DataTable (ID, Name, Value) VALUES (?, ?, ?)";
            cmd.Parameters.AddWithValue("@ID", 1);
            cmd.Parameters.AddWithValue("@Name", "Sample");
            cmd.Parameters.AddWithValue("@Value", 123.45);
            cmd.ExecuteNonQuery();
        }
    }
}
```

## Using Relative Paths

For applications that need to work with databases in different locations:

```csharp
using System.IO;
using Majorsilence.Reporting.RdlCreator;

// Get the application's base directory
string appPath = AppDomain.CurrentDomain.BaseDirectory;

// Build relative path
string relativePath = Path.Combine(appPath, "Data", "mydb.accdb");

// Or use relative to current directory
string currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), "mydb.accdb");

// Create connection string with absolute path
string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.GetFullPath(relativePath)};";

var create = new Create();
var report = await create.GenerateRdl(
    DataProviders.OleDb,
    connectionString,
    "SELECT * FROM Products",
    pageHeaderText: "Product Report");
```

## Modifying Existing Report Connection Strings

If you have an existing RDL report definition and want to change its connection string:

```csharp
using Majorsilence.Reporting.Rdl;

// Load existing report
var rdlParser = new RDLParser("path/to/existing/report.rdl");
var report = await rdlParser.Parse();

// Override connection string at runtime
report.OverwriteConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\new\path\database.accdb;";

// Run report with new connection
await report.RunGetData(null);
var ms = new MemoryStreamGen();
await report.RunRender(ms, OutputPresentationType.PDF);
```

## Common Issues and Solutions

### Issue: "The 'Microsoft.ACE.OLEDB.12.0' provider is not registered"

**Solution**: Install the Microsoft Access Database Engine Redistributable that matches your application's platform (32-bit or 64-bit).

### Issue: "Could not find installable ISAM"

**Solution**: Check your connection string syntax. Ensure the Provider string exactly matches one of:
- `Microsoft.Jet.OLEDB.4.0` for .mdb files
- `Microsoft.ACE.OLEDB.12.0` for .accdb files

### Issue: Platform incompatibility on Linux/Mac

**Solution**: OleDb provider is Windows-only. Consider these alternatives:
- Convert Access database to SQLite: Use tools like [mdb-tools](https://github.com/mdbtools/mdbtools) or commercial converters
- Use ODBC with appropriate drivers
- Export data to CSV and use the Text provider
- Use a web service layer that runs on Windows

### Issue: "Unrecognized database format"

**Solution**: Ensure you have the correct Access Database Engine version installed:
- For .accdb files created in Access 2007-2019, use ACE.OLEDB.12.0
- For older .mdb files, Jet.OLEDB.4.0 usually works
- For very new .accdb files, you may need the latest ACE driver

## Performance Considerations

1. **Connection Pooling**: OleDb connections are pooled automatically. No special configuration needed.

2. **Database Locking**: Access databases lock when in use. If your report runs slowly, ensure:
   - The database is not open exclusively in MS Access
   - Network databases have proper permissions
   - Consider using `Mode=Share Deny None` in connection string for read-only access

3. **Large Databases**: For databases larger than 2GB (Access limit), consider:
   - Splitting data across multiple Access files
   - Migrating to SQL Server or another enterprise database
   - Using Access as a frontend with linked tables to another backend

## Complete Working Example

Here's a full, working example that demonstrates all concepts:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlCreator;
using Majorsilence.Reporting.Rdl;

namespace AccessReportingExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the reporting engine once
            RdlEngineConfig.RdlEngineConfigInit();

            // Example 1: Basic report from Access database
            await BasicAccessReport();

            // Example 2: Dynamic database selection
            await DynamicDatabaseReport();

            // Example 3: Multiple databases
            await MultiDatabaseReport();
        }

        static async Task BasicAccessReport()
        {
            Console.WriteLine("Generating basic Access report...");

            string dbPath = @"C:\Data\Northwind.accdb";
            string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

            var create = new Create();
            var report = await create.GenerateRdl(
                DataProviders.OleDb,
                connectionString,
                "SELECT TOP 10 CustomerID, CompanyName, City, Country FROM Customers",
                pageHeaderText: "Top 10 Customers");

            string outputPath = "BasicReport.pdf";
            var fileStream = new OneFileStreamGen(outputPath, true);
            await report.RunGetData(null);
            await report.RunRender(fileStream, OutputPresentationType.PDF);

            Console.WriteLine($"Report saved to: {outputPath}");
        }

        static async Task DynamicDatabaseReport()
        {
            Console.WriteLine("Generating report from dynamic database...");

            // Get database path from user input or configuration
            string[] databasePaths = {
                @"C:\Data\Sales2023.accdb",
                @"C:\Data\Sales2024.accdb"
            };

            foreach (var dbPath in databasePaths)
            {
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine($"Database not found: {dbPath}");
                    continue;
                }

                string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";
                string year = Path.GetFileNameWithoutExtension(dbPath).Replace("Sales", "");

                var create = new Create();
                var report = await create.GenerateRdl(
                    DataProviders.OleDb,
                    connectionString,
                    "SELECT * FROM SalesData",
                    pageHeaderText: $"Sales Report {year}");

                string outputPath = $"SalesReport_{year}.pdf";
                var fileStream = new OneFileStreamGen(outputPath, true);
                await report.RunGetData(null);
                await report.RunRender(fileStream, OutputPresentationType.PDF);

                Console.WriteLine($"Report saved to: {outputPath}");
            }
        }

        static async Task MultiDatabaseReport()
        {
            Console.WriteLine("Generating reports from multiple databases...");

            var databases = new[]
            {
                new { Path = @"C:\Data\Customers.accdb", Table = "Customers", Title = "Customer List" },
                new { Path = @"C:\Data\Orders.accdb", Table = "Orders", Title = "Order History" },
                new { Path = @"C:\Data\Products.accdb", Table = "Products", Title = "Product Catalog" }
            };

            foreach (var db in databases)
            {
                if (!File.Exists(db.Path))
                {
                    Console.WriteLine($"Skipping {db.Title} - file not found");
                    continue;
                }

                string connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={db.Path};";

                var create = new Create();
                var report = await create.GenerateRdl(
                    DataProviders.OleDb,
                    connectionString,
                    $"SELECT * FROM {db.Table}",
                    pageHeaderText: db.Title);

                string outputPath = $"{db.Title.Replace(" ", "")}.pdf";
                var fileStream = new OneFileStreamGen(outputPath, true);
                await report.RunGetData(null);
                await report.RunRender(fileStream, OutputPresentationType.PDF);

                Console.WriteLine($"Report saved to: {outputPath}");
            }
        }
    }
}
```

## Additional Resources

- [Microsoft Access Database Engine 2016 Redistributable](https://www.microsoft.com/en-us/download/details.aspx?id=54920)
- [OLE DB Connection Strings](https://www.connectionstrings.com/access/)
- [Majorsilence Reporting Wiki](https://github.com/majorsilence/My-FyiReporting/wiki)
- [Database Providers How-to](https://github.com/majorsilence/My-FyiReporting/wiki/Database-Providers-Howto)

## Summary

Yes, Majorsilence Reporting **can** create reports based on MS Access data, and yes, you **can** change the database location in code. The key points are:

1. Use `DataProviders.OleDb` or `"OLEDB"` as the provider
2. Build your connection string with the correct provider and database path
3. Change the database path by modifying the connection string dynamically
4. This only works on Windows platforms
5. You can create reports from different databases by simply changing the connection string for each report

The flexibility of the connection string system means you can:
- Generate reports from databases created on the fly
- Switch between different databases at runtime
- Use relative or absolute paths
- Work with password-protected databases
- Handle both .mdb and .accdb formats
