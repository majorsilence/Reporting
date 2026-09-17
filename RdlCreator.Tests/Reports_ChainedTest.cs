using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;
using Majorsilence.Reporting.RdlCreator;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using System.Linq;

using Majorsilence.Forms.Drawing;
using System.Drawing;  // value types (Color, Point, Size, Rectangle, ...) come from System.Drawing.Primitives


namespace Majorsilence.Reporting.RdlCreator.Tests
{
    [TestFixture]
    public class Reports_ChainedTest
    {
        string connectionString = "Data Source=sqlitetestdb2.db;";
        string dataProvider = "Microsoft.Data.Sqlite";

        [Test]
        public async Task PdfStreamExport()
        {
            var create = new RdlCreator.Create();
            var report = GenerateTestData();
            report.Author = "Test Author";
            report.Name = "Test Name";
            report.Description = "Test Description";

            var fyiReport = await create.GenerateRdl(report);
            using var ms = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
            await fyiReport.RunGetData();
            await fyiReport.RunRender(ms, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
            var pdfStream = ms.GetStream();

            pdfStream.Position = 0;
            using var pdfDocument = PdfDocument.Open(pdfStream);
            var text = string.Join(" ",
                pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

            Assert.Multiple(() =>
            {
                Assert.That(pdfDocument.Information.Author, Is.EqualTo("Test Author"));
                Assert.That(pdfDocument.Information.Title, Is.EqualTo("Test Name"));
                Assert.That(pdfDocument.Information.Subject, Is.EqualTo("Test Description"));
                Assert.That(pdfDocument.Information.Creator,
                    Is.EqualTo("Majorsilence Reporting - RenderPdf_Raw"));
                Assert.That(text, Is.Not.Null);
                Assert.That(text,
                    Is.EqualTo(
                        "Test Data Set Report CategoryID CategoryName Description Beverages Soft drinks, coffees, teas, beers, and ales Condiments Sweet and savory sauces, relishes, spreads, and seasonings Confections Desserts, candies, and sweet breads Dairy Products Cheeses Grains/Cereals Breads, crackers, pasta, and cereal Meat/Poultry Prepared meats Produce Dried fruit and bean curd Seafood Seaweed and fish 1 of 1"));
            });
        }

        [Test]
        public async Task PdfDiskExport()
        {
            var create = new RdlCreator.Create();
            var report = GenerateTestData();
            var fyiReport = await create.GenerateRdl(report);
            using var ms = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
            await fyiReport.RunGetData();
            await fyiReport.RunRender(ms, Majorsilence.Reporting.Rdl.OutputPresentationType.PDF);
            var pdfStream = ms.GetStream();
            pdfStream.Position = 0;

            using var fileStream = new FileStream("PdfChainedDiskExport.pdf", FileMode.Create, FileAccess.Write);
            pdfStream.CopyTo(fileStream);
            await fileStream.DisposeAsync();

            using var pdfDocument = PdfDocument.Open("PdfChainedDiskExport.pdf");
            var text = string.Join(" ",
                pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

        Assert.That(text, Is.Not.Null);
            Assert.That(text,
                Is.EqualTo(
                    "Test Data Set Report CategoryID CategoryName Description Beverages Soft drinks, coffees, teas, beers, and ales Condiments Sweet and savory sauces, relishes, spreads, and seasonings Confections Desserts, candies, and sweet breads Dairy Products Cheeses Grains/Cereals Breads, crackers, pasta, and cereal Meat/Poultry Prepared meats Produce Dried fruit and bean curd Seafood Seaweed and fish 1 of 1"));
        }

        [Test]
        public async Task HtmlDiskExport()
        {
            var create = new RdlCreator.Create();
            var report = GenerateTestData();
            var fyiReport = await create.GenerateRdl(report);
            using var ms = new Majorsilence.Reporting.Rdl.MemoryStreamGen();
            await fyiReport.RunGetData();
            await fyiReport.RunRender(ms, Majorsilence.Reporting.Rdl.OutputPresentationType.HTML);
            var pdfStream = ms.GetStream();
            pdfStream.Position = 0;

            using var fileStream = new FileStream("HtmlChainedDiskExport.html", FileMode.Create, FileAccess.Write);
            pdfStream.CopyTo(fileStream);
            await fileStream.DisposeAsync();

            var text = await System.IO.File.ReadAllTextAsync("HtmlChainedDiskExport.html");
            Assert.That(text.Contains("seasonings"));
            Assert.That(text.Contains("bean curd"));
        }

        private RdlCreator.Report GenerateTestData()
        {
            var report = new Report
                {
                    Description = "Sample report",
                    Author = "John Doe",
                    PageHeight = "11in",
                    PageWidth = "8.5in",
                    Width = "7.5in",
                    TopMargin = ".25in",
                    LeftMargin = ".25in",
                    RightMargin = ".25in",
                    BottomMargin = ".25in"
                }
                .WithDataSources(
                    new DataSources
                    {
                        DataSource = new DataSource
                        {
                            Name = "DS1",
                            ConnectionProperties = new ConnectionProperties
                            {
                                DataProvider = dataProvider, ConnectString = connectionString
                            }
                        }
                    }
                )
                .WithDataSets(
                    new DataSets
                    {
                        DataSet = new DataSet
                        {
                            Name = "Data",
                            Query = new Query
                            {
                                DataSourceName = "DS1",
                                CommandText = "SELECT CategoryID, CategoryName, Description FROM Categories"
                            },
                            Fields = new Fields
                            {
                                Field = new List<Field>
                                {
                                    new Field
                                    {
                                        Name = "CategoryID",
                                        DataField = "CategoryID",
                                        TypeName = "System.Int64"
                                    },
                                    new Field
                                    {
                                        Name = "CategoryName",
                                        DataField = "CategoryName",
                                        TypeName = "System.String"
                                    },
                                    new Field
                                    {
                                        Name = "Description",
                                        DataField = "Description",
                                        TypeName = "System.String"
                                    }
                                }
                            }
                        }
                    }
                )
                .WithPageHeader(
                    new PageHeader
                    {
                        Height = ".5in",
                        ReportItems = new ReportItemsHeader
                        {
                            Textbox = new Text
                            {
                                Name = "Textbox1",
                                Top = ReportItemSize.FromInches(0.1f),
                                Left = ReportItemSize.FromInches(0.1f),
                                Width = ReportItemSize.FromInches(6.0f),
                                Height = ReportItemSize.FromInches(0.25f),
                                Value = new Value { Text = "Test Data Set Report" },
                                Style = new Style { FontSize = ReportItemSize.FromPoints(15), FontWeight = "Bold" }
                            }
                        },
                        PrintOnFirstPage = "true",
                        PrintOnLastPage = "true"
                    })
                .WithPageFooter(new PageFooter
                {
                    Height = "14pt",
                    ReportItems = new ReportItemsFooter
                    {
                        Textbox = new Text
                        {
                            Name = "Textbox5",
                            Top = "1pt",
                            Left = "10pt",
                            Height = "12pt",
                            Width = "3in",
                            Value = new Value { Text = "=Globals!PageNumber + ' of ' + Globals!TotalPages" },
                            Style = new Style { FontSize = "10pt", FontWeight = "Normal" }
                        }
                    },
                    PrintOnFirstPage = "true",
                    PrintOnLastPage = "true"
                });

            // Add a body to the report

            report
                .WithBody("36pt")
                .WithTable()
                .WithTableName("Table1")
                .WithTableDataSetName("Data")
                .WithTableNoRows("Query returned no rows!")
                .WithTableHeader(
                    new TableRow
                    {
                        Height = "12pt",
                        TableCells = new TableCells()
                        {
                            TableCell = new List<TableCell>
                            {
                                new TableCell
                                {
                                    ReportItems = new TableCellReportItems()
                                    {
                                        ReportItem = new Text
                                        {
                                            Name = "Textbox2",
                                            Value = new Value { Text = "CategoryID" },
                                            Style =
                                                new Style { TextAlign = "Center", FontWeight = "Bold" }
                                        }
                                    }
                                },
                                new TableCell
                                {
                                    ReportItems = new TableCellReportItems()
                                    {
                                        ReportItem = new Text
                                        {
                                            Name = "Textbox3",
                                            Value = new Value { Text = "CategoryName" },
                                            Style =
                                                new Style { TextAlign = "Center", FontWeight = "Bold" }
                                        }
                                    }
                                },
                                new TableCell
                                {
                                    ReportItems = new TableCellReportItems()
                                    {
                                        ReportItem = new Text
                                        {
                                            Name = "Textbox4",
                                            Value = new Value { Text = "Description" },
                                            Style = new Style { TextAlign = "Center", FontWeight = "Bold" }
                                        }
                                    }
                                }
                            }
                        }
                    }, repeatOnNewPage: "true")
                .WithTableColumns(new TableColumns
                {
                    TableColumn = new List<TableColumn>
                    {
                        new TableColumn { Width = "1.25in" },
                        new TableColumn { Width = "1.5in" },
                        new TableColumn { Width = "1.375in" }
                    }
                })
                .WithTableDetails(new TableRow
                {
                    Height = "12pt",
                    TableCells = new TableCells()
                    {
                        TableCell = new List<TableCell>
                        {
                            new TableCell
                            {
                                ReportItems = new TableCellReportItems()
                                {
                                    ReportItem = new CustomReportItems()
                                    {
                                        Name = "QrCode",
                                        Type = "QR Code",
                                        Width = "35.91mm",
                                        Height = "35.91mm",
                                        CustomProperties =
                                            new CustomProperties
                                            {
                                                CustomProperty = new CustomProperty()
                                                {
                                                    Name = "QrCode", Value = "=Fields!CategoryID.Value"
                                                }
                                            },
                                        CanGrow = "false",
                                        Style =
                                            new Style
                                            {
                                                BorderStyle =
                                                    new BorderStyle
                                                    {
                                                        Default = BorderStyleType.None,
                                                        Bottom = BorderStyleType.Solid
                                                    },
                                                BorderColor =
                                                    new BorderColor { Bottom = Color.Green.Name },
                                                BorderWidth =
                                                    new BorderWidth { Bottom = "1pt" }
                                            },
                                        Source = "Embedded"
                                    }
                                }
                            },
                            new TableCell
                            {
                                ReportItems = new TableCellReportItems()
                                {
                                    ReportItem = new Text
                                    {
                                        Name = "CategoryName",
                                        Value =
                                            new Value { Text = "=Fields!CategoryName.Value" },
                                        CanGrow = true,
                                        Style = new Style
                                        {
                                            BorderStyle =
                                                new BorderStyle
                                                {
                                                    Default = BorderStyleType.None, Bottom = BorderStyleType.Solid
                                                },
                                            BorderColor =
                                                new BorderColor { Bottom = Color.Green.Name },
                                            BorderWidth = new BorderWidth { Bottom = "1pt" }
                                        }
                                    }
                                }
                            },
                            new TableCell
                            {
                                ReportItems = new TableCellReportItems()
                                {
                                    ReportItem = new Text
                                    {
                                        Name = "Description",
                                        Value =
                                            new Value { Text = "=Fields!Description.Value" },
                                        CanGrow = true,
                                        Style = new Style
                                        {
                                            BorderStyle =
                                                new BorderStyle
                                                {
                                                    Default = BorderStyleType.None, Bottom = BorderStyleType.Solid
                                                },
                                            BorderColor = new BorderColor { Bottom = Color.Green.Name },
                                            BorderWidth = new BorderWidth { Bottom = "1pt" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

            return report;
        }
    }
}