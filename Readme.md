# Majorsilence Reporting (formerly My-FyiReporting)

If you have any question about Majorsilence Reporting or do you want to contribute a discussion group for Majorsilence Reporting is available here:

https://groups.google.com/d/forum/myfyireporting


|         |Linux |Mac | Win | Win(AppeyVeyor) |
|---------|:------:|:------:|:------:|:------:|
|**Master**| [![linux](https://github.com/majorsilence/My-FyiReporting/actions/workflows/linux.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/linux.yml) | [![mac](https://github.com/majorsilence/My-FyiReporting/actions/workflows/mac.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/mac.yml) | [![.github/workflows/windows.yml](https://github.com/majorsilence/My-FyiReporting/actions/workflows/windows.yml/badge.svg?branch=master)](https://github.com/majorsilence/My-FyiReporting/actions/workflows/windows.yml) | [![Build status appveyor](https://ci.appveyor.com/api/projects/status/a44n015bli95rmpw?svg=true)](https://ci.appveyor.com/project/majorsilence/my-fyireporting) | 



# Documentation
See the [projects wiki](https://github.com/majorsilence/My-FyiReporting/wiki).

# Download

See the [downloads page](https://github.com/majorsilence/My-FyiReporting/wiki/Downloads).

Alternatively if you want keep up with the latest version you can always use Git

    git clone https://github.com/majorsilence/My-FyiReporting.git

# Introduction
Majorsilence Reporting is a powerful, open-source .NET reporting framework designed for developers who need to create, design, and deliver rich, reports. Supporting modern .NET versions (8.0), it provides a flexible and extensible platform for building reports from a variety of data sources. With a drag-and-drop designer, multiple viewer options, and cross-platform support, Majorsilence Reporting is ideal for both desktop and web applications. Whether you need to generate reports programmatically or empower users with a visual designer (windows only), this project offers the tools and documentation to get you started quickly.

**The core of Majorsilence Reporting supports Linux and macOS for server-side application report generation. Only the WinForms-based designer and viewer are Windows-only.**

## Internationalization

Majorsilence Reporting supports multiple languages including English, Russian, French, and Spanish. See [INTERNATIONALIZATION.md](INTERNATIONALIZATION.md) for details on supported languages and how to configure or contribute translations.

# Quick start

Add these nuget packages to your project.

```bash
dotnet add package Majorsilence.Reporting.RdlCreator.SkiaSharp
dotnet add package Majorsilence.Reporting.RdlEngine.SkiaSharp
dotnet add package Majorsilence.Reporting.RdlCri.SkiaSharp
```

If running on linux install the [required fonts](https://github.com/majorsilence/My-FyiReporting/wiki/Linux---PDF-export-and-Fonts).

```bash
sudo apt install ttf-mscorefonts-installer
```

You are now ready to create and generate reports.

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


