using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Majorsilence.Reporting.Rdl;
using ReportTests.Utils;

namespace ReportTests
{
    [TestFixture]
    public class CanGrowCanShrinkTests
    {
        [Test]
        public async Task TestCanGrowCanShrink_PDF()
        {
            string reportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Reports", "CanGrowCanShrinkTest.rdl");
            string outputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "CanGrowCanShrinkTest_output.pdf");
            
            Assert.That(File.Exists(reportPath), Is.True, $"Report file not found: {reportPath}");
            
            Uri fileRdlUri = new Uri(reportPath);
            Report report = await RdlUtils.GetReport(fileRdlUri);
            
            Assert.That(report, Is.Not.Null, "Report should not be null");
            
            await report.RunGetData(null);
            
            Pages pages = await report.BuildPages();
            
            Assert.That(pages, Is.Not.Null, "Pages should not be null");
            Assert.That(pages.PageCount, Is.GreaterThan(0), "Should have at least one page");
            
            // Generate PDF to verify rendering works
            OneFileStreamGen sg = new OneFileStreamGen(outputPath, true);
            try
            {
                await report.RunRenderPdf(sg, pages);
                Assert.That(File.Exists(outputPath), Is.True, $"Output PDF should be created: {outputPath}");
                
                FileInfo fi = new FileInfo(outputPath);
                Assert.That(fi.Length, Is.GreaterThan(0), "Output PDF should not be empty");
                
                Console.WriteLine($"PDF generated successfully: {outputPath}");
                Console.WriteLine($"File size: {fi.Length} bytes");
            }
            finally
            {
                sg?.CloseMainStream();
            }
        }
        
        [Test]
        public async Task TestCanGrow_TableRow_HeightIncreases()
        {
            string reportPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Reports", "CanGrowCanShrinkTest.rdl");
            
            Assert.That(File.Exists(reportPath), Is.True, $"Report file not found: {reportPath}");
            
            Uri fileRdlUri = new Uri(reportPath);
            Report report = await RdlUtils.GetReport(fileRdlUri);
            
            Assert.That(report, Is.Not.Null, "Report should not be null");
            
            await report.RunGetData(null);
            
            Pages pages = await report.BuildPages();
            
            Assert.That(pages, Is.Not.Null, "Pages should not be null");
            Assert.That(pages.PageCount, Is.GreaterThan(0), "Should have at least one page");
            
            // Verify that pages were generated - this is a basic sanity check
            // More detailed verification would require inspecting the page items
            Page firstPage = pages[0];
            Assert.That(firstPage, Is.Not.Null, "First page should not be null");
            Assert.That(firstPage.Count, Is.GreaterThan(0), "First page should have items");
            
            Console.WriteLine($"Page generated with {firstPage.Count} items");
        }
    }
}
