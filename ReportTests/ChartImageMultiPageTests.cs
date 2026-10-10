#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Issue #60: charts and images that fall beyond the first page went missing from HTML and Excel output.
    /// </summary>
    [TestFixture]
    public class ChartImageMultiPageTests
    {
        private const string ReportFile = "ChartImageMultiPage.rdl";

        private async Task<Report> LoadAsync()
        {
            var folder = GeneralUtils.ReportsFolder();
            Directory.SetCurrentDirectory(folder.LocalPath);
            RdlEngineConfig.RdlEngineConfigInit();
            var rap = await RdlUtils.GetReport(new Uri(folder, ReportFile));
            rap.Folder = folder.LocalPath;
            await rap.RunGetData();
            return rap;
        }

        [Test]
        public async Task Html_ChartAndImageOnLaterPage_AreRendered()
        {
            var rap = await LoadAsync();
            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();
            int imgs = System.Text.RegularExpressions.Regex.Matches(html, "<img ").Count;
            // 3 charts + 1 external image in the report
            Assert.That(imgs, Is.GreaterThanOrEqualTo(4), html);
            // in-memory output has no sibling files, so charts must be inlined
            Assert.That(html, Does.Contain("src=\"data:image/png;base64,"));
        }

        [Test]
        public async Task Html_InMemory_AllImagesAreInlined()
        {
            var rap = await LoadAsync();
            using var ms = new MemoryStreamGen();
            await rap.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();
            var srcs = System.Text.RegularExpressions.Regex.Matches(html, "<img [^>]*src=\"([^\"]*)\"")
                .Select(m => m.Groups[1].Value).ToList();
            Assert.That(srcs, Is.Not.Empty);
            Assert.That(srcs, Has.All.StartsWith("data:"), string.Join("\n", srcs.Select(x => x.Length > 80 ? x.Substring(0, 80) : x)));
            // chart data is real PNG content, not an empty payload
            Assert.That(srcs.Where(x => x.StartsWith("data:image/png;base64,")).All(x => x.Length > "data:image/png;base64,".Length + 100));
        }

        [Test]
        public async Task Html_ToFile_KeepsLinkedImageFiles()
        {
            var rap = await LoadAsync();
            string dir = Path.Combine(GeneralUtils.OutputTestsFolder().LocalPath, "ChartImageMultiPageHtml");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "ChartImageMultiPage.html");
            var sg = new OneFileStreamGen(path, true);
            await rap.RunRender(sg, OutputPresentationType.HTML);
            string html = File.ReadAllText(path);
            Assert.That(html, Does.Not.Contain("src=\"data:image/png;base64,"));
            Assert.That(html, Does.Contain("<img "));
        }

        [Test]
        public async Task Excel_ChartAndImageOnLaterPage_AreEmbedded()
        {
            var rap = await LoadAsync();
            string path = Path.Combine(GeneralUtils.OutputTestsFolder().LocalPath, "ChartImageMultiPage.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sg = new OneFileStreamGen(path, true);
            await rap.RunRender(sg, OutputPresentationType.Excel2007);
            using var zip = ZipFile.OpenRead(path);
            var media = zip.Entries.Where(e => e.FullName.StartsWith("xl/media/")).ToList();
            // 3 charts + 1 image
            Assert.That(media.Count, Is.GreaterThanOrEqualTo(4), string.Join(",", zip.Entries.Select(e => e.FullName)));
        }
    }
}
#endif
