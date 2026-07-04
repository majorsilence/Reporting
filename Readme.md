# Majorsilence Reporting (formerly My-FyiReporting)

**Cross-platform RDL/RDLC reporting for .NET 8 and .NET 10 — no SSRS server, no Crystal runtime, Native-AOT-ready.**

Run SQL Server Reporting Services report definitions as a library inside your own app: render to PDF, HTML, CSV, Excel, and images on Linux, macOS, and Windows. Design reports in the browser, in a WinForms designer, or code-first in C#.

[![NuGet](https://img.shields.io/nuget/v/Majorsilence.Reporting.RdlEngine.SkiaSharp?label=RdlEngine.SkiaSharp)](https://www.nuget.org/packages/Majorsilence.Reporting.RdlEngine.SkiaSharp)
[![NuGet](https://img.shields.io/nuget/dt/Majorsilence.Reporting.RdlEngine.SkiaSharp?label=downloads)](https://www.nuget.org/packages/Majorsilence.Reporting.RdlEngine.SkiaSharp)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE.txt)

|         |Linux |Mac | Win | Win(AppeyVeyor) |
|---------|:------:|:------:|:------:|:------:|
|**Master**| [![linux](https://github.com/majorsilence/My-FyiReporting/actions/workflows/linux.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/linux.yml) | [![mac](https://github.com/majorsilence/My-FyiReporting/actions/workflows/mac.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/mac.yml) | [![.github/workflows/windows.yml](https://github.com/majorsilence/My-FyiReporting/actions/workflows/windows.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/windows.yml) | [![Build status appveyor](https://ci.appveyor.com/api/projects/status/a44n015bli95rmpw?svg=true)](https://ci.appveyor.com/project/majorsilence/my-fyireporting) |

# 30-second quick start

```bash
dotnet add package Majorsilence.Reporting.RdlCreator.SkiaSharp
dotnet add package Majorsilence.Reporting.RdlEngine.SkiaSharp
dotnet add package Majorsilence.Reporting.RdlCri.SkiaSharp
```

```cs
using Majorsilence.Reporting.RdlCreator;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

var create = new Majorsilence.Reporting.RdlCreator.Create();
using var report = await create.GenerateRdl("Microsoft.Data.Sqlite",
    connectionString,
    "SELECT CategoryID, CategoryName, Description FROM Categories",
    pageHeaderText: "Categories");

using var ofs = new Majorsilence.Reporting.Rdl.OneFileStreamGen("categories.pdf", true);
await report.RunGetData(null);
await report.RunRender(ofs, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
```

If running on Linux install the [required fonts](https://github.com/majorsilence/My-FyiReporting/wiki/Linux---PDF-export-and-Fonts):

```bash
sudo apt install ttf-mscorefonts-installer
```

# Packages

Package IDs ending in `.SkiaSharp` render with a SkiaSharp-based drawing layer and run on Linux, macOS, Windows, and containers. The unsuffixed variants render with System.Drawing and are Windows-only. Pick one family — don't mix.

| Package | Purpose | Platforms | Status |
|---------|---------|-----------|--------|
| `Majorsilence.Reporting.RdlEngine.SkiaSharp` | RDL/RDLC rendering engine | Linux, macOS, Windows | Active |
| `Majorsilence.Reporting.RdlEngine` | Rendering engine (System.Drawing) | Windows | Active |
| `Majorsilence.Reporting.RdlCreator.SkiaSharp` / `…RdlCreator` | Code-first fluent report + PDF builder | per variant | Active |
| `Majorsilence.Reporting.RdlCri.SkiaSharp` / `…RdlCri` | Barcodes and QR codes as report items | per variant | Active |
| `Majorsilence.Reporting.DataProviders.SkiaSharp` / `…DataProviders` | SQL Server, SQLite, MySQL, PostgreSQL, ODBC, JSON, XML providers | per variant | Active |
| `Majorsilence.Reporting.WebDesigner` | Browser-based report designer (Web Component + Blazor/React/Angular wrappers) | any (ASP.NET Core) | Active |
| `Majorsilence.Reporting.RdlAsp.Mvc` | ASP.NET Core report display helpers | any | Active |
| `Majorsilence.Pdf` | Zero-dependency PDF writer (usable standalone) | any | Active |
| `Majorsilence.Pdf.Security` | PDF encryption + digital signatures | any | Active |
| `Majorsilence.Drawing.Common` | SkiaSharp System.Drawing-compatible drawing layer | any | Active |
| `Majorsilence.Reporting.RdlViewer` | WinForms viewer control | Windows | Active¹ |
| `Majorsilence.Reporting.ReportDesigner` | WinForms drag-and-drop designer | Windows | Active¹ |
| `Majorsilence.Reporting.EncryptionProvider` | Connection-string encryption for viewers/designers | any | Active |
| `Majorsilence.Reporting.LibRdlWpfViewer` | WPF viewer wrapper | Windows | **Maintenance²** |
| `Majorsilence.Reporting.RdlGtk3` | GTK3 viewer library | Linux | **Maintenance²** |

¹ A cross-platform successor built on [Majorsilence.Forms](https://github.com/majorsilence/Modern.Forms) is planned.
² Maintenance mode: bug fixes only. For new work use the Avalonia viewer (`Majorsilence.Reporting.UI.RdlAvalonia`, in this repo) or render server-side and display in the browser.

## Viewer choices

- **Server-side / web** — render to PDF or HTML with `RdlEngine` and stream it from ASP.NET Core; the `WebDesigner` package includes preview endpoints.
- **Cross-platform desktop** — the Avalonia viewer control in `Majorsilence.Reporting.UI.RdlAvalonia` (sample app in `Majorsilence.Reporting.UI`).
- **Windows desktop** — the WinForms `RdlViewer` control.
- **Command line** — `RdlCmd` renders reports from scripts and cron jobs; Native AOT builds available on the [releases page](https://github.com/majorsilence/My-FyiReporting/releases).

# Documentation

See the [projects wiki](https://github.com/majorsilence/My-FyiReporting/wiki). If you have questions, the [discussion group](https://groups.google.com/d/forum/myfyireporting) is available.

# Download

See the [downloads page](https://github.com/majorsilence/My-FyiReporting/wiki/Downloads).

Alternatively if you want keep up with the latest version you can always use Git

    git clone https://github.com/majorsilence/My-FyiReporting.git

# More examples

## c# example, create report, connected to an sql database

See [Database Providers](https://github.com/majorsilence/My-FyiReporting/wiki/Database-Providers-Howto).

```cs
using Majorsilence.Reporting.RdlCreator;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

string dataProvider = "[PLACEHOLDER/Json/Microsoft.Data.SqlClient/MySQL.NET/Firebird.NET 2.0/Microsoft.Data.Sqlite/PostgreSQL";
var create = new Majorsilence.Reporting.RdlCreator.Create();

using var report = await create.GenerateRdl(dataProvider,
    connectionString,
    "SELECT CategoryID, CategoryName, Description FROM Categories",
    pageHeaderText: "DataProviderTest TestMethod1");

string filepath = System.IO.Path.Combine(Environment.CurrentDirectory, "PLACEHOLDER.pdf");
using var ofs = new Majorsilence.Reporting.Rdl.OneFileStreamGen(filepath, true);
await report.RunGetData(null);
await report.RunRender(ofs, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
```

## c# example, create a pdf document

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

## c# load an existing rdl report via RdlEngine

note: Notice the namespace and type difference of Majorsilence.Reporting.RdlCreator.Report for creating new rdl reports versus Majorsilence.Reporting.Rdl.Report for running a report.

```cs
using Majorsilence.Reporting.Rdl;

// One time per app instance
RdlEngineConfig.RdlEngineConfigInit();

var rdlp = new RDLParser(System.IO.File.ReadAllText(@"C:\path\to\report\file.rdl"));
using var report = await rdlp.Parse();

string filepath = System.IO.Path.Combine(Environment.CurrentDirectory, "PLACEHOLDER.pdf");
using var ofs = new Majorsilence.Reporting.Rdl.OneFileStreamGen(filepath, true);
await report.RunGetData(null);
await report.RunRender(ofs, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
```


# Development
Majorsilence Reporting is developed with the following workflow:

* Nothing happens for weeks or months
* Someone needs it to do something it doesn't already do
* That person implements that something and submits a pull request
* Repeat

If it doesn't have a feature that you want it to have, add it.  If it has a bug you need fixed, fix it.

See [Contribute](https://github.com/majorsilence/My-FyiReporting/wiki/Contribute).

# Benchmarks

## one

BenchmarkDotNet v0.15.2, macOS 26.0.1 (25A362) [Darwin 25.0.0]
Apple M2, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.200
  [Host]     : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD
  Job-AKATUD : .NET 8.0.13 (8.0.1325.6609), Arm64 RyuJIT AdvSIMD

BuildConfiguration=Release-DrawingCompat  RunStrategy=Throughput  

| Method     | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|----------:|------------:|
| NestedJson | 5.401 ms | 0.0560 ms | 0.0935 ms |  0.99 |    0.03 | 390.6250 | 125.0000 |   3.12 MB |        1.00 |


|Threads|Duration|Source|Calls/Sec|Total Calls|Errors|
|-------|--------|------|---------:|----------:|-----:|
|1|30|JsonDataProviderBenchmark|162|4,873|0|
|20|30|JsonDataProviderBenchmark|362|10,865|0|
|20|30|JsonDataProviderBenchmark|363|10,894|0|
|30|30|JsonDataProviderBenchmark|334|10,043|0|
|40|30|JsonDataProviderBenchmark|316|9,497|0|
|50|30|JsonDataProviderBenchmark|296|8,905|0|


## two

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Intel Core i5-8350U CPU 1.70GHz (Max: 1.90GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.414
  [Host]     : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2
  Job-AKATUD : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2

BuildConfiguration=Release-DrawingCompat  RunStrategy=Throughput  

| Method     | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|------:|--------:|---------:|---------:|----------:|------------:|
| NestedJson | 16.67 ms | 0.796 ms | 2.140 ms | 15.84 ms |  1.01 |    0.17 | 727.2727 | 272.7273 |   3.24 MB |        1.00 |


|Threads|Duration|Source|Calls/Sec|Total Calls|Errors|
|-------|--------|------|---------:|----------:|-----:|
|1|30|JsonDataProviderBenchmark|34|1,009|0|
|20|30|JsonDataProviderBenchmark|64|1,920|0|
|20|30|JsonDataProviderBenchmark|63|1,893|0|
|30|30|JsonDataProviderBenchmark|58|1,739|0|
|40|30|JsonDataProviderBenchmark|49|1,497|0|
|50|30|JsonDataProviderBenchmark|51|1,546|0|

## three

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 5 3600 4.21GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 8.0.120
[Host]     : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2
Job-AKATUD : .NET 8.0.20 (8.0.2025.41914), X64 RyuJIT AVX2

BuildConfiguration=Release-DrawingCompat  RunStrategy=Throughput

| Method     | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|----------:|------------:|
| NestedJson | 5.542 ms | 0.1100 ms | 0.2011 ms |  1.00 |    0.05 | 359.3750 | 125.0000 |   2.94 MB |        1.00 |


|Threads|Duration|Source|Calls/Sec|Total Calls|Errors|
|-------|--------|------|---------:|----------:|-----:|
|1|30|JsonDataProviderBenchmark|152|4,571|0|
|20|30|JsonDataProviderBenchmark|403|12,115|0|
|20|30|JsonDataProviderBenchmark|406|12,196|0|
|30|30|JsonDataProviderBenchmark|358|10,757|0|
|40|30|JsonDataProviderBenchmark|315|9,460|0|
|50|30|JsonDataProviderBenchmark|290|8,720|0|


# License

The entire project is Apache 2.0 licensed with select sub projects tri-licensed.  Original code is apache 2.0 only.  New projects are tri-licensed as of 26.0.0.


## Apache 2.0 Licensed
- DataProviders
- EncryptionProver
- LibRdlWpfViewer
- Majorsilence.WinformUtils
- RdlAsp.Mvc
- RdlCmd
- RdlCri
- RdlDesign
- RdlDesktop
- RdlEngine
- RdlGtk3
- RdlGtk3Viewer
- RdlMapFile
- RdlReader
- RdlViewer
- ReportDesigner

## Tri Licensed
- MIT License               — see LICENSE-MIT in sub folders
- Apache License 2.0        — see LICENSE-APACHE in sub folders
- BSD 2-Clause License      — see LICENSE-BSD in sub folders

tri-licensed projects

- Majorsilence.Drawing.Common
- Majorsilence.Pdf
- Majorsilence.Pdf.Security
- Majorsilence.Reporting.UI
- Majorsilence.Reporting.UI.RdlAvalonia
- PdfNative
- RdlCreator
- RdlNative
- WebDesigner
