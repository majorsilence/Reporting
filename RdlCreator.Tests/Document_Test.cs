using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;
using Majorsilence.Reporting.RdlCreator;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using System.Linq;

namespace Majorsilence.Reporting.RdlCreator.Tests
{
    [TestFixture]
    public class Document_Test
    {
        [Test]
        public async Task SinglePagePdfDiskExport()
        {
            var document = GenerateData(1);
            using var fileStream = new FileStream("SinglePagePdf.pdf", FileMode.Create, FileAccess.Write);
            await document.Create(fileStream);
            await fileStream.DisposeAsync();

            using var pdfDocument = PdfDocument.Open("SinglePagePdf.pdf");
            var text = string.Join(" ", pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

            Assert.That(text, Is.Not.Null);
            Assert.That(pdfDocument.NumberOfPages, Is.EqualTo(1));
            Assert.That(text, Is.EqualTo("Test Header Text Area 1 Lorem ipsum. 0"));
        }

        [Test]
        public async Task MultiPagePdfTest()
        {
            var document = GenerateData(2);
            using var fileStream = new FileStream("MultiPagePdf.pdf", FileMode.Create, FileAccess.Write);
            await document.Create(fileStream);
            await fileStream.DisposeAsync();

            using var pdfDocument = PdfDocument.Open("MultiPagePdf.pdf");
            var text = string.Join(" ", pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

            Assert.That(text, Is.Not.Null);
            Assert.That(pdfDocument.NumberOfPages, Is.EqualTo(2));
            Assert.That(text, Is.EqualTo("Test Header Text Area 1 Lorem ipsum. 0 Test Header Text Area 1 Lorem ipsum. 1"));
        }

        [Test]
        [Ignore("This test is slow.  Remove the ignore when you want to run it.")]
        public async Task LoadTest()
        {
            var document = GenerateData(10000);
            using var fileStream = new FileStream("LoadTestDocument.pdf", FileMode.Create, FileAccess.Write);
            await document.Create(fileStream);
            await fileStream.DisposeAsync();

            using var pdfDocument = PdfDocument.Open("LoadTestDocument.pdf");
            Assert.That(pdfDocument.NumberOfPages, Is.EqualTo(10000));
        }

        [Test]
        public async Task ImageTestPdfDiskExport()
        {
            var document = GenerateImageDocument();
            using var fileStream = new FileStream("ImageTestPdfDiskExport.pdf", FileMode.Create, FileAccess.Write);
            await document.Create(fileStream);
            await fileStream.DisposeAsync();

            using var pdfDocument = PdfDocument.Open("ImageTestPdfDiskExport.pdf");
            var text = string.Join(" ", pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

            Assert.That(text, Is.Not.Null);
            Assert.That(pdfDocument.NumberOfPages, Is.EqualTo(1));
            Assert.That(text, Is.EqualTo("Test Header Text Area 1 Lorem ipsum. Hello World"));
        }

        [Test]
        public async Task PdfInfoAddedTest()
        {
            var document = GenerateData(2);
            document.Author = "Test Author";
            document.Name = "Test Name";
            document.Description = "Test Description";

            using var fileStream = new FileStream("PdfInfoAddedTest.pdf", FileMode.Create, FileAccess.Write);
            await document.Create(fileStream);
            await fileStream.DisposeAsync();
            // Majorsilence Reporting - RenderPdf_iTextSharp

            using var pdfDocument = PdfDocument.Open("PdfInfoAddedTest.pdf");
            var text = string.Join(" ", pdfDocument.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));

            Assert.Multiple(() =>
            {
                Assert.That(pdfDocument.Information.Author, Is.EqualTo("Test Author"));
                Assert.That(pdfDocument.Information.Title, Is.EqualTo("Test Name"));
                Assert.That(pdfDocument.Information.Subject, Is.EqualTo("Test Description"));
                Assert.That(pdfDocument.Information.Creator, Is.EqualTo("Majorsilence Reporting - RdlCreator"));
                Assert.That(text, Is.Not.Null);
                Assert.That(pdfDocument.NumberOfPages, Is.EqualTo(2));
            });
        }

        private RdlCreator.Document GenerateData(int pageCount = 1)
        {
            var document = new Document
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
            };

            for (int i = 0; i < pageCount; i++)
            {
                document.WithPage((option) =>
                {
                    option.WithHeight("10in")
                    .WithWidth("7.5in")
                    .WithText(new Text
                    {
                        Name = "Textbox1",
                        Top = ".1in",
                        Left = ".1in",
                        Width = "6in",
                        Height = ".25in",
                        Value = new Value { Text = "Text Area 1" },
                        Style = new Style { FontSize = "12pt", FontWeight = "Bold" }
                    })
                    .WithText(new Text
                    {
                        Name = "Textbox2",
                        Top = "1in",
                        Left = "1in",
                        Width = "6in",
                        Height = "4in",
                        Value = new Value { Text = "Lorem ipsum." },
                        Style = new Style
                        {
                            FontSize = "12pt",
                            BackgroundColor = "gray"
                        }
                    });

                    option.WithPageFooter(new PageFooter
                    {
                        Height = "14pt",
                        ReportItems = new ReportItemsFooter
                        {
                            Textbox = new Text
                            {
                                Name = "Footer",
                                Top = "1pt",
                                Left = "10pt",
                                Height = "12pt",
                                Width = "3in",
                                Value = new Value { Text = $"{i}" },
                                Style = new Style { FontSize = "10pt", FontWeight = "Normal" }
                            }
                        },
                        PrintOnFirstPage = "true",
                        PrintOnLastPage = "true"
                    });

                    option.WithPageHeader(
                        new PageHeader
                        {
                            Height = ".5in",
                            ReportItems = new ReportItemsHeader
                            {
                                Textbox = new Text
                                {
                                    Name = "Header",
                                    Top = ".1in",
                                    Left = ".1in",
                                    Width = "6in",
                                    Height = ".25in",
                                    Value = new Value { Text = "Test Header" },
                                    Style = new Style { FontSize = "15pt", FontWeight = "Bold" }
                                }
                            },
                            PrintOnFirstPage = "true",
                            PrintOnLastPage = "true"
                        });
                });
            }

            return document;
        }

        private RdlCreator.Document GenerateImageDocument()
        {
            var document = new Document
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
            };


            document.WithPage((option) =>
            {
                option.WithHeight("10in")
                .WithWidth("7.5in")
                .WithText(new Text
                {
                    Name = "Textbox1",
                    Top = ".1in",
                    Left = ".1in",
                    Width = "6in",
                    Height = ".25in",
                    Value = new Value { Text = "Text Area 1" },
                    Style = new Style { FontSize = "12pt", FontWeight = "Bold" }
                })
                .WithText(new Text
                {
                    Name = "Textbox2",
                    Top = "1in",
                    Left = "1in",
                    Width = "6in",
                    Height = "1in",
                    Value = new Value { Text = "Lorem ipsum." },
                    Style = new Style
                    {
                        FontSize = "12pt",
                        BackgroundColor = "gray"
                    }
                });

                option.WithImage(new ReportItemImage
                {
                    Name = "Image1",
                    Top = "1in",
                    Left = "1in",
                    Width = "6in",
                    Height = "6in",
                    Value = "test-image.jpg",
                    Source = "External",
                    Sizing = "Fit"
                });

                option.WithPageFooter(new PageFooter
                {
                    Height = "14pt",
                    ReportItems = new ReportItemsFooter
                    {
                        Textbox = new Text
                        {
                            Name = "Footer",
                            Top = "1pt",
                            Left = "10pt",
                            Height = "12pt",
                            Width = "3in",
                            Value = new Value { Text = $"Hello World" },
                            Style = new Style { FontSize = "10pt", FontWeight = "Normal" }
                        }
                    },
                    PrintOnFirstPage = "true",
                    PrintOnLastPage = "true"
                });

                option.WithPageHeader(
                    new PageHeader
                    {
                        Height = ".5in",
                        ReportItems = new ReportItemsHeader
                        {
                            Textbox = new Text
                            {
                                Name = "Header",
                                Top = ".1in",
                                Left = ".1in",
                                Width = "6in",
                                Height = ".25in",
                                Value = new Value { Text = "Test Header" },
                                Style = new Style { FontSize = "15pt", FontWeight = "Bold" }
                            }
                        },
                        PrintOnFirstPage = "true",
                        PrintOnLastPage = "true"
                    });
            });


            return document;
        }
    }
}
