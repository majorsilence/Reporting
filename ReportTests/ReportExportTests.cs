using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    [TestFixture]
    public class ReportExportTests
    {
        private Uri _reportFolder = null;
        private Uri _outputFolder = null;

        [SetUp]
        public void Setup()
        {
            if (_outputFolder == null)
            {
                _outputFolder = GeneralUtils.OutputTestsFolder();
            }

            _reportFolder = GeneralUtils.ReportsFolder();
            Directory.CreateDirectory(_outputFolder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
        }

        [Test]
        public async Task Export_ToPdf_CreatesFile()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_PDF.pdf");
            
            // Clean up any existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // Act
            await report.Export(OutputPresentationType.PDF, outputPath);

            // Assert
            Assert.That(File.Exists(outputPath), Is.True, "PDF file should be created");
            FileInfo fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "PDF file should have content");
        }

        [Test]
        public async Task Export_ToExcel_CreatesFile()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_Excel.xlsx");
            
            // Clean up any existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // Act
            await report.Export(OutputPresentationType.Excel2007, outputPath);

            // Assert
            Assert.That(File.Exists(outputPath), Is.True, "Excel file should be created");
            FileInfo fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "Excel file should have content");
        }

        [Test]
        public async Task Export_ToCsv_CreatesFile()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_CSV.csv");
            
            // Clean up any existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // Act
            await report.Export(OutputPresentationType.CSV, outputPath);

            // Assert
            Assert.That(File.Exists(outputPath), Is.True, "CSV file should be created");
            FileInfo fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "CSV file should have content");
        }

        [Test]
        public async Task Export_WithParameters_UsesParameters()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            string outputPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_WithParams.pdf");
            
            // Clean up any existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            // Create parameters
            ListDictionary parameters = new ListDictionary();
            // Note: WorldFacts.rdl doesn't have parameters, but this tests the parameter passing mechanism

            // Act
            await report.Export(OutputPresentationType.PDF, outputPath, parameters);

            // Assert
            Assert.That(File.Exists(outputPath), Is.True, "PDF file should be created with parameters");
            FileInfo fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), "PDF file should have content");
        }

        [Test]
        public async Task Export_NullFilePath_ThrowsArgumentException()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await report.Export(OutputPresentationType.PDF, null);
            });
        }

        [Test]
        public async Task Export_EmptyFilePath_ThrowsArgumentException()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await report.Export(OutputPresentationType.PDF, string.Empty);
            });
        }

        [Test]
        public async Task ExportToMemory_ToPdf_ReturnsBytes()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;

            // Act
            byte[] pdfBytes = await report.ExportToMemory(OutputPresentationType.PDF);

            // Assert
            Assert.That(pdfBytes, Is.Not.Null, "PDF bytes should not be null");
            Assert.That(pdfBytes.Length, Is.GreaterThan(0), "PDF bytes should have content");
            
            // Verify PDF header (PDF files start with %PDF)
            string header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, Math.Min(4, pdfBytes.Length));
            Assert.That(header, Is.EqualTo("%PDF"), "Should be a valid PDF file");
        }

        [Test]
        public async Task ExportToMemory_ToExcel_ReturnsBytes()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;

            // Act
            byte[] excelBytes = await report.ExportToMemory(OutputPresentationType.Excel2007);

            // Assert
            Assert.That(excelBytes, Is.Not.Null, "Excel bytes should not be null");
            Assert.That(excelBytes.Length, Is.GreaterThan(0), "Excel bytes should have content");
            
            // Verify ZIP header (Excel 2007 files are ZIP archives starting with PK)
            string header = System.Text.Encoding.ASCII.GetString(excelBytes, 0, Math.Min(2, excelBytes.Length));
            Assert.That(header, Is.EqualTo("PK"), "Should be a valid Excel 2007 file (ZIP format)");
        }

        [Test]
        public async Task ExportToMemory_ToCsv_ReturnsBytes()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;

            // Act
            byte[] csvBytes = await report.ExportToMemory(OutputPresentationType.CSV);

            // Assert
            Assert.That(csvBytes, Is.Not.Null, "CSV bytes should not be null");
            Assert.That(csvBytes.Length, Is.GreaterThan(0), "CSV bytes should have content");
            
            // Verify it's text content
            string csvContent = System.Text.Encoding.UTF8.GetString(csvBytes);
            Assert.That(csvContent, Is.Not.Empty, "CSV content should not be empty");
        }

        [Test]
        public async Task ExportToMemory_WithParameters_UsesParameters()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            // Create parameters
            ListDictionary parameters = new ListDictionary();
            // Note: WorldFacts.rdl doesn't have parameters, but this tests the parameter passing mechanism

            // Act
            byte[] pdfBytes = await report.ExportToMemory(OutputPresentationType.PDF, parameters);

            // Assert
            Assert.That(pdfBytes, Is.Not.Null, "PDF bytes should not be null");
            Assert.That(pdfBytes.Length, Is.GreaterThan(0), "PDF bytes should have content");
        }

        [Test]
        public async Task Export_MultipleFormats_AllSucceed()
        {
            // Arrange
            Uri fileRdlUri = new Uri(_reportFolder, "WorldFacts.rdl");
            Report report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            
            string pdfPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_Multi.pdf");
            string excelPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_Multi.xlsx");
            string csvPath = Path.Combine(_outputFolder.LocalPath, "ExportTest_Multi.csv");
            
            // Clean up any existing files
            if (File.Exists(pdfPath)) File.Delete(pdfPath);
            if (File.Exists(excelPath)) File.Delete(excelPath);
            if (File.Exists(csvPath)) File.Delete(csvPath);

            // Act
            await report.Export(OutputPresentationType.PDF, pdfPath);
            
            // Need to get data again for subsequent exports
            report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            await report.Export(OutputPresentationType.Excel2007, excelPath);
            
            report = await RdlUtils.GetReport(fileRdlUri);
            report.Folder = _reportFolder.LocalPath;
            await report.Export(OutputPresentationType.CSV, csvPath);

            // Assert
            Assert.That(File.Exists(pdfPath), Is.True, "PDF file should be created");
            Assert.That(File.Exists(excelPath), Is.True, "Excel file should be created");
            Assert.That(File.Exists(csvPath), Is.True, "CSV file should be created");
            
            Assert.That(new FileInfo(pdfPath).Length, Is.GreaterThan(0), "PDF should have content");
            Assert.That(new FileInfo(excelPath).Length, Is.GreaterThan(0), "Excel should have content");
            Assert.That(new FileInfo(csvPath).Length, Is.GreaterThan(0), "CSV should have content");
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up test files if needed
            // Files are left in the output folder for manual inspection
        }
    }
}
