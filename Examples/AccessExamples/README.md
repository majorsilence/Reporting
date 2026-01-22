# MS Access Database Examples

This directory contains examples demonstrating how to use Majorsilence Reporting with Microsoft Access databases.

## Prerequisites

- Windows operating system
- Microsoft Access Database Engine 2016 Redistributable installed
  - [Download here](https://www.microsoft.com/en-us/download/details.aspx?id=54920)
- .NET 8.0 or later

## Examples

### Basic Access Report (BasicAccessReport.cs)

Demonstrates creating a simple report from an MS Access database (.accdb or .mdb file).

**Key concepts:**
- Using `DataProviders.OleDb` for Access databases
- Building connection strings for .accdb and .mdb files
- Generating PDF reports from Access data

### Dynamic Database Report (DynamicAccessReport.cs)

Shows how to change the database location dynamically at runtime.

**Key concepts:**
- Building connection strings programmatically
- Switching between multiple databases
- Handling different Access database formats

### On-the-Fly Database Report (OnTheFlyAccessReport.cs)

Illustrates creating and populating an Access database programmatically, then generating a report from it.

**Key concepts:**
- Creating Access databases in code
- Populating databases with data
- Generating reports from temporary databases

## Running the Examples

1. Ensure you have the Microsoft Access Database Engine installed
2. Update the database paths in the example files to point to your Access databases
3. Build and run the examples:

```bash
dotnet run --project BasicAccessReport.cs
```

## Connection String Examples

### For .accdb files (Access 2007+):
```csharp
string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\path\to\database.accdb;";
```

### For .mdb files (Access 2003 and earlier):
```csharp
string connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\path\to\database.mdb;";
```

### With password protection:
```csharp
string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\path\to\database.accdb;Jet OLEDB:Database Password=YourPassword;";
```

## Complete Documentation

For comprehensive documentation on MS Access support, see:
- [MS Access Database Support Guide](../../docs/MS-Access-Database-Support.md)

## Notes

- MS Access support is Windows-only due to OleDb dependency
- The `DataProviders.OleDb` enum value is only available when building for Windows
- For cross-platform scenarios, consider using SQLite or another database system
