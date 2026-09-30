# Data Binding and Looping Through Data in Reports

This guide explains how to bind data sources to reports and loop through data using textboxes and tables.

## Table of Contents
- [Understanding Data Binding](#understanding-data-binding)
- [Binding Data Sources](#binding-data-sources)
- [Looping Through Data with Tables](#looping-through-data-with-tables)
- [Using Textboxes to Display Data](#using-textboxes-to-display-data)
- [Code Examples](#code-examples)

## Understanding Data Binding

Majorsilence Reporting uses RDL (Report Definition Language) files to define reports. Data binding connects your data source to report elements so they can display dynamic data.

### Key Components:
- **DataSource**: Defines the connection to your database or data provider
- **DataSet**: Contains a query and defines the fields available for binding
- **Fields**: Individual data columns that can be bound to report elements
- **Report Items**: Visual elements (Textboxes, Tables, etc.) that display the data

## Binding Data Sources

### Method 1: Database Connection (RDL Definition)

In your RDL file, define a DataSource and DataSet:

```xml
<DataSources>
  <DataSource Name="DS1">
    <ConnectionProperties>
      <DataProvider>Microsoft.Data.Sqlite</DataProvider>
      <ConnectString>Data Source=/path/to/database.db</ConnectString>
    </ConnectionProperties>
  </DataSource>
</DataSources>

<DataSets>
  <DataSet Name="Data">
    <Query>
      <DataSourceName>DS1</DataSourceName>
      <CommandText>SELECT CategoryID, CategoryName, Description FROM Categories</CommandText>
    </Query>
    <Fields>
      <Field Name="CategoryID">
        <DataField>CategoryID</DataField>
        <rd:TypeName>System.Int64</rd:TypeName>
      </Field>
      <Field Name="CategoryName">
        <DataField>CategoryName</DataField>
        <rd:TypeName>System.String</rd:TypeName>
      </Field>
      <Field Name="Description">
        <DataField>Description</DataField>
        <rd:TypeName>System.String</rd:TypeName>
      </Field>
    </Fields>
  </DataSet>
</DataSets>
```

### Method 2: Programmatic Data Binding (C#)

You can also set data programmatically in your application:

```csharp
using System.Data;
using Majorsilence.Reporting.RdlViewer;

// Load the report
string filepath = "path/to/report.rdl";
await rdlViewer1.SetSourceFile(new Uri(filepath));

// Create or retrieve your data
DataTable dt = new DataTable();
dt.Columns.Add("CategoryID", typeof(int));
dt.Columns.Add("CategoryName", typeof(string));
dt.Columns.Add("Description", typeof(string));

// Add sample data
dt.Rows.Add(1, "Beverages", "Soft drinks, coffees, teas");
dt.Rows.Add(2, "Condiments", "Sweet and savory sauces");

// Bind data to the dataset
await (await rdlViewer1.Report()).DataSets["Data"].SetData(dt);
await rdlViewer1.Rebuild();
```

### Method 3: Using RdlCreator to Generate Reports

```csharp
using Majorsilence.Reporting.RdlCreator;

// Initialize configuration (once per app instance)
RdlEngineConfig.RdlEngineConfigInit();

string dataProvider = "Microsoft.Data.Sqlite";
string connectionString = "Data Source=/path/to/database.db";
string query = "SELECT CategoryID, CategoryName, Description FROM Categories";

var create = new Majorsilence.Reporting.RdlCreator.Create();
var report = await create.GenerateRdl(
    dataProvider,
    connectionString,
    query,
    pageHeaderText: "My Report Title"
);

// Generate PDF
string filepath = "output.pdf";
var ofs = new Majorsilence.Reporting.Rdl.OneFileStreamGen(filepath, true);
await report.RunGetData(null);
await report.RunRender(ofs, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
```

## Looping Through Data with Tables

To loop through data and display multiple rows, you use a **Table** element with a **Details** section. Each textbox in the Details section will be repeated for every row in your dataset.

### Basic Table Structure

```xml
<Table Name="Table1">
  <DataSetName>Data</DataSetName>
  <NoRows>Query returned no rows!</NoRows>
  <TableColumns>
    <TableColumn>
      <Width>1.5in</Width>
    </TableColumn>
    <TableColumn>
      <Width>2in</Width>
    </TableColumn>
  </TableColumns>
  
  <!-- Header Row (appears once at top) -->
  <Header>
    <TableRows>
      <TableRow>
        <Height>12pt</Height>
        <TableCells>
          <TableCell>
            <ReportItems>
              <Textbox Name="HeaderTextbox1">
                <Value>Category ID</Value>
                <Style>
                  <FontWeight>Bold</FontWeight>
                </Style>
              </Textbox>
            </ReportItems>
          </TableCell>
          <TableCell>
            <ReportItems>
              <Textbox Name="HeaderTextbox2">
                <Value>Category Name</Value>
                <Style>
                  <FontWeight>Bold</FontWeight>
                </Style>
              </Textbox>
            </ReportItems>
          </TableCell>
        </TableCells>
      </TableRow>
    </TableRows>
  </Header>
  
  <!-- Details Section (repeats for each row) -->
  <Details>
    <TableRows>
      <TableRow>
        <Height>12pt</Height>
        <TableCells>
          <TableCell>
            <ReportItems>
              <Textbox Name="CategoryID">
                <Value>=Fields!CategoryID.Value</Value>
              </Textbox>
            </ReportItems>
          </TableCell>
          <TableCell>
            <ReportItems>
              <Textbox Name="CategoryName">
                <Value>=Fields!CategoryName.Value</Value>
              </Textbox>
            </ReportItems>
          </TableCell>
        </TableCells>
      </TableRow>
    </TableRows>
  </Details>
</Table>
```

### How It Works:
1. The `<DataSetName>Data</DataSetName>` tells the table which dataset to use
2. The `<Header>` section defines column headers (displayed once)
3. The `<Details>` section defines the row template that repeats for each data row
4. Each textbox in the Details section uses `=Fields!FieldName.Value` to bind to data

## Using Textboxes to Display Data

### Simple Textbox (Non-Repeating)

For displaying a single value (like in a page header or footer):

```xml
<Textbox Name="PageTitle">
  <Top>.1in</Top>
  <Left>.1in</Left>
  <Width>6in</Width>
  <Height>.25in</Height>
  <Value>My Report Title</Value>
  <Style>
    <FontSize>15pt</FontSize>
    <FontWeight>Bold</FontWeight>
  </Style>
</Textbox>
```

### Data-Bound Textbox (Inside Table)

For displaying values that repeat with data:

```xml
<Textbox Name="CategoryName">
  <Value>=Fields!CategoryName.Value</Value>
  <CanGrow>true</CanGrow>
  <Style>
    <BorderStyle>
      <Default>Solid</Default>
    </BorderStyle>
    <PaddingLeft>2pt</PaddingLeft>
  </Style>
</Textbox>
```

### Using Expressions

Textboxes support expressions for formatting and calculations:

```xml
<!-- Concatenate fields -->
<Value>=Fields!FirstName.Value &amp; " " &amp; Fields!LastName.Value</Value>

<!-- Format numbers -->
<Value>=Format(Fields!Price.Value, "C2")</Value>

<!-- Format dates -->
<Value>=Format(Fields!OrderDate.Value, "yyyy-MM-dd")</Value>

<!-- Conditional formatting -->
<Value>=IIF(Fields!Quantity.Value > 100, "High", "Low")</Value>

<!-- Math operations -->
<Value>=Fields!Price.Value * Fields!Quantity.Value</Value>
```

## Code Examples

### Complete Example: Database to PDF

```csharp
using System;
using System.Threading.Tasks;
using Majorsilence.Reporting.RdlCreator;

namespace ReportExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize once per application
            RdlEngineConfig.RdlEngineConfigInit();
            
            // Database connection
            string connectionString = "Data Source=northwind.db";
            string query = @"
                SELECT 
                    CategoryID, 
                    CategoryName, 
                    Description 
                FROM Categories";
            
            // Create report
            var creator = new Majorsilence.Reporting.RdlCreator.Create();
            var report = await creator.GenerateRdl(
                "Microsoft.Data.Sqlite",
                connectionString,
                query,
                pageHeaderText: "Categories Report"
            );
            
            // Generate PDF
            string outputPath = "categories.pdf";
            var fileStream = new Majorsilence.Reporting.Rdl.OneFileStreamGen(
                outputPath, 
                true
            );
            
            await report.RunGetData(null);
            await report.RunRender(
                fileStream, 
                Majorsilence.Reporting.Rdl.OutputPresentationType.PDF
            );
            
            Console.WriteLine($"Report generated: {outputPath}");
        }
    }
}
```

### Example: In-Memory Data

```csharp
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using Majorsilence.Reporting.RdlViewer;

public partial class ReportForm : Form
{
    private RdlViewer rdlViewer1;
    
    public ReportForm()
    {
        InitializeComponent();
        rdlViewer1 = new RdlViewer();
        rdlViewer1.Dock = DockStyle.Fill;
        this.Controls.Add(rdlViewer1);
    }
    
    private async void LoadReport()
    {
        // Create in-memory data
        DataTable categories = new DataTable("Categories");
        categories.Columns.Add("CategoryID", typeof(int));
        categories.Columns.Add("CategoryName", typeof(string));
        categories.Columns.Add("Description", typeof(string));
        
        categories.Rows.Add(1, "Beverages", "Soft drinks, coffees, teas, beers");
        categories.Rows.Add(2, "Condiments", "Sweet and savory sauces, relishes");
        categories.Rows.Add(3, "Confections", "Desserts, candies, and sweet breads");
        categories.Rows.Add(4, "Dairy Products", "Cheeses");
        
        // Load report definition
        string reportPath = "path/to/report.rdl";
        await rdlViewer1.SetSourceFile(new Uri(reportPath));
        
        // Bind data to dataset
        var report = await rdlViewer1.Report();
        await report.DataSets["Data"].SetData(categories);
        
        // Refresh display
        await rdlViewer1.Rebuild();
    }
}
```

### Example: Using Multiple Datasets

```xml
<!-- In RDL file -->
<DataSets>
  <DataSet Name="Categories">
    <Query>
      <DataSourceName>DS1</DataSourceName>
      <CommandText>SELECT * FROM Categories</CommandText>
    </Query>
  </DataSet>
  
  <DataSet Name="Products">
    <Query>
      <DataSourceName>DS1</DataSourceName>
      <CommandText>SELECT * FROM Products</CommandText>
    </Query>
  </DataSet>
</DataSets>

<Body>
  <ReportItems>
    <!-- First table using Categories dataset -->
    <Table Name="CategoriesTable">
      <DataSetName>Categories</DataSetName>
      <!-- ... table definition ... -->
    </Table>
    
    <!-- Second table using Products dataset -->
    <Table Name="ProductsTable">
      <DataSetName>Products</DataSetName>
      <!-- ... table definition ... -->
    </Table>
  </ReportItems>
</Body>
```

```csharp
// Bind multiple datasets programmatically
await rdlViewer1.SetSourceFile(new Uri(reportPath));
var report = await rdlViewer1.Report();

DataTable categories = GetCategoriesData();
DataTable products = GetProductsData();

await report.DataSets["Categories"].SetData(categories);
await report.DataSets["Products"].SetData(products);

await rdlViewer1.Rebuild();
```

## Common Scenarios

### Scenario 1: Master-Detail Report

Use nested tables or filters to show related data:

```xml
<!-- Parent table -->
<Table Name="Categories">
  <DataSetName>Categories</DataSetName>
  <Details>
    <TableRows>
      <TableRow>
        <TableCells>
          <TableCell>
            <ReportItems>
              <!-- Nested table for products -->
              <Table Name="Products">
                <DataSetName>Products</DataSetName>
                <Filters>
                  <Filter>
                    <FilterExpression>=Fields!CategoryID.Value</FilterExpression>
                    <Operator>Equal</Operator>
                    <FilterValues>
                      <FilterValue>=Fields!CategoryID.Value</FilterValue>
                    </FilterValues>
                  </Filter>
                </Filters>
                <!-- ... product table definition ... -->
              </Table>
            </ReportItems>
          </TableCell>
        </TableCells>
      </TableRow>
    </TableRows>
  </Details>
</Table>
```

### Scenario 2: Grouped Data

```xml
<Table Name="Table1">
  <DataSetName>Data</DataSetName>
  <TableGroups>
    <TableGroup>
      <Grouping Name="CategoryGroup">
        <GroupExpressions>
          <GroupExpression>=Fields!CategoryName.Value</GroupExpression>
        </GroupExpressions>
      </Grouping>
      <Header>
        <TableRows>
          <TableRow>
            <TableCells>
              <TableCell>
                <ReportItems>
                  <Textbox Name="GroupHeader">
                    <Value>=Fields!CategoryName.Value</Value>
                    <Style>
                      <FontWeight>Bold</FontWeight>
                      <BackgroundColor>LightGray</BackgroundColor>
                    </Style>
                  </Textbox>
                </ReportItems>
              </TableCell>
            </TableCells>
          </TableRow>
        </TableRows>
      </Header>
    </TableGroup>
  </TableGroups>
  <!-- ... details section ... -->
</Table>
```

### Scenario 3: Conditional Formatting

```xml
<Textbox Name="Status">
  <Value>=Fields!Status.Value</Value>
  <Style>
    <BackgroundColor>=IIF(Fields!Status.Value = "Active", "Green", "Red")</BackgroundColor>
    <Color>White</Color>
  </Style>
</Textbox>
```

## Best Practices

1. **Use Meaningful Names**: Give your DataSets, Tables, and Textboxes descriptive names
2. **Set CanGrow**: For text fields that might be long, set `<CanGrow>true</CanGrow>` to allow expansion
3. **Handle Null Values**: Use `IsNothing()` function to check for null values
4. **Optimize Queries**: Only select the fields you need in your SQL queries
5. **Use Parameters**: For dynamic reports, use report parameters to filter data
6. **Test with Real Data**: Always test reports with actual data to ensure proper formatting

## Supported Data Providers

Majorsilence Reporting supports various data providers:
- Microsoft.Data.Sqlite
- Microsoft.Data.SqlClient
- MySQL.NET
- PostgreSQL
- Firebird.NET 2.0
- JSON (via JSON data provider)
- In-memory DataTables

## Additional Resources

- [Project Wiki](https://github.com/majorsilence/My-FyiReporting/wiki)
- [Examples Directory](../Examples/) - Contains working examples
- [Database Providers Guide](https://github.com/majorsilence/My-FyiReporting/wiki/Database-Providers-Howto)
- [Main README](../Readme.md)

## Troubleshooting

### Issue: "Query returned no rows!"
- Verify your connection string is correct
- Check that the database file exists at the specified path
- Ensure your query syntax is valid for your database
- Test the query directly in a database tool

### Issue: Fields not displaying data
- Verify field names match between DataSet definition and textbox binding
- Check that the DataSetName in your Table matches the DataSet Name
- Ensure data types in Field definitions match actual data

### Issue: Report not updating with new data
- Make sure to call `await rdlViewer1.Rebuild()` after setting data
- Verify the dataset name matches between RDL and code

## Summary

To loop through data in reports:
1. Define a **DataSet** with your query and fields
2. Create a **Table** element and set its **DataSetName**
3. Add **Textboxes** in the **Details** section using `=Fields!FieldName.Value`
4. Bind data either through:
   - Database connection in RDL
   - Programmatically using `DataSets["Name"].SetData(dataTable)`
   - Using RdlCreator to generate reports from queries

The Table's Details section automatically loops through all rows in your dataset, creating a textbox for each row with the bound data.
