---
layout: post
title: First Post
date: 2025-12-28
last_modified: 2025-12-28
comments: true
enable_syntax_highlighting: true
---

Majorsilence Reporting is a powerful, open-source .NET reporting framework designed for developers who need to create, design, and deliver rich, reports. Supporting modern .NET versions (8.0, 10.0), it provides a flexible and extensible platform for building reports from a variety of data sources. With a drag-and-drop designer (windows only), multiple viewer options, and cross-platform support, Majorsilence Reporting is ideal for both desktop and web applications. Whether you need to generate reports programmatically or empower users with a visual designer, this project offers the tools and documentation to get you started quickly.

**The core of Majorsilence Reporting supports Linux and macOS for server-side application report generation.

## Quick Start

### Create a new report

```cs
using Majorsilence.Reporting.RdlCreator;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

string dataProvider = "[PLACEHOLDER/Json/Microsoft.Data.SqlClient/MySQL.NET/Firebird.NET 2.0/Microsoft.Data.Sqlite/PostgreSQL";
var create = new Majorsilence.Reporting.RdlCreator.Create();

var report = await create.GenerateRdl(dataProvider,
    connectionString,
    "SELECT CategoryID, CategoryName, Description FROM Categories",
    pageHeaderText: "DataProviderTest TestMethod1");

string filepath = System.IO.Path.Combine(Environment.CurrentDirectory, "PLACEHOLDER.pdf");
var ofs = new Majorsilence.Reporting.Rdl.OneFileStreamGen(filepath, true);
await report.RunGetData(null);
await report.RunRender(ofs, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
```

### Create an ad hoc PDF document

```cs
using Majorsilence.Reporting.RdlCreator;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

var document = new Majorsilence.Reporting.RdlCreator.Document()
{
    Description = "Sample report",
    Author = "John Doe",
    PageHeight = "11in",
    PageWidth = "8.5in",
    //Width = "7.5in",
    TopMargin = ".25in",
    LeftMargin = ".25in",
    RightMargin = ".25in",
    BottomMargin = ".25in"
}
.WithPage((page) =>
{
    page.WithHeight("10in")
    .WithWidth("7.5in")
    .WithText(new Text
    {
        Name = "TheSimplePageText",
        Top = ".1in",
        Left = ".1in",
        Width = "6in",
        Height = ".25in",
        Value = new Value { Text = "Text Area 1" },
        Style = new Style { FontSize = "12pt", FontWeight = "Bold" }
    });
});

using var fileStream = new FileStream("PLACEHOLDER.pdf", FileMode.Create, FileAccess.Write);
await document.Create(fileStream);
```

## Reference

[Majorsilence Reporting Wiki contents page](https://github.com/majorsilence/My-FyiReporting/wiki/_pages) - external link.
