# Examples Overview

This directory contains various examples demonstrating different features of Majorsilence Reporting.

## Quick Start Examples

### Data Binding and Looping
For detailed information on how to bind data sources and loop through data in reports, see:
**[Data Binding and Looping Documentation](../docs/DataBinding-and-Looping.md)**

**[SimpleDataBindingExample.rdl](SimpleDataBindingExample.rdl)** - A minimal, well-commented example that demonstrates:
- How to define a DataSource and DataSet
- How to create a Table that loops through data rows
- How to bind Textboxes to data fields using `=Fields!FieldName.Value`
- Basic formatting and styling

### Example Projects

#### SampleApp2-SetData
Demonstrates how to programmatically set data to a report using DataTables.
- Shows database connection with SQLite
- Example of binding in-memory data to report datasets
- See `Form1.cs` for the implementation

**Key Concepts:**
```csharp
// Load report
await rdlViewer1.SetSourceFile(new Uri(filepath));

// Bind data
await (await rdlViewer1.Report()).DataSets["Data"].SetData(dataTable);

// Refresh view
await rdlViewer1.Rebuild();
```

#### SqliteExamples
Contains various RDL files demonstrating:
- SimpleTest1.rdl - Basic table with data binding
- SimpleTest2.rdl - More complex report layouts
- SimpleTest3WithParameters.rdl - Reports with parameters
- chart.rdl - Chart examples
- barcode.rdl - Barcode generation

#### JsonExamples
Examples of using JSON as a data provider

#### SampleApp
Basic sample application showing report viewing

#### Sample-Report-Viewer
Sample WinForms application for viewing reports

#### SampleDesignerControl
Shows how to integrate the report designer control into your application

## Common Patterns

### 1. Looping Through Data with Tables

In your RDL file, use a Table element:
```xml
<Table Name="Table1">
  <DataSetName>Data</DataSetName>
  <Details>
    <TableRows>
      <TableRow>
        <TableCells>
          <TableCell>
            <ReportItems>
              <Textbox Name="FieldName">
                <Value>=Fields!FieldName.Value</Value>
              </Textbox>
            </ReportItems>
          </TableCell>
        </TableCells>
      </TableRow>
    </TableRows>
  </Details>
</Table>
```

The Details section automatically loops through each row in your dataset.

### 2. Binding Data Sources

**From Database (in RDL):**
```xml
<DataSources>
  <DataSource Name="DS1">
    <ConnectionProperties>
      <DataProvider>Microsoft.Data.Sqlite</DataProvider>
      <ConnectString>Data Source=path/to/db.db</ConnectString>
    </ConnectionProperties>
  </DataSource>
</DataSources>
```

**Programmatically (in C#):**
```csharp
DataTable dt = GetYourData();
await (await rdlViewer.Report()).DataSets["DataSetName"].SetData(dt);
await rdlViewer.Rebuild();
```

### 3. Using Textboxes

**Static Textbox:**
```xml
<Textbox Name="Title">
  <Value>Report Title</Value>
</Textbox>
```

**Data-Bound Textbox (inside Table):**
```xml
<Textbox Name="ProductName">
  <Value>=Fields!ProductName.Value</Value>
</Textbox>
```

## Database File

The `northwindEF.db` file in this directory is a sample SQLite database used by several examples. It contains standard Northwind sample data including:
- Categories
- Products
- Employees
- Orders
- And more...

## Running the Examples

1. Open the solution file in Visual Studio (Windows)
2. Build the solution
3. Run the desired example project
4. Most examples use the northwindEF.db database included in this directory

## Additional Resources

- [Full Data Binding Documentation](../docs/DataBinding-and-Looping.md)
- [Project Wiki](https://github.com/majorsilence/My-FyiReporting/wiki)
- [Database Providers Guide](https://github.com/majorsilence/My-FyiReporting/wiki/Database-Providers-Howto)
- [Main README](../Readme.md)
