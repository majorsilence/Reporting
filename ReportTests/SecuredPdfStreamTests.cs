#if NET8_0_OR_GREATER
using Majorsilence.Pdf.Security;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Rendering an encrypted or signed PDF to a file must finish the file the way the plain PDF path
    /// does. The secured path used to return without closing the stream generator, so the output
    /// stayed open and unflushed until the caller disposed it.
    /// </summary>
    [TestFixture]
    public class SecuredPdfStreamTests
    {
        private sealed record Row(string CustomerID);

        [Test]
        public async Task EncryptedPdf_IsClosedAndCompleteAfterRunRender()
        {
            var folder = GeneralUtils.ReportsFolder();
            RdlEngineConfig.RdlEngineConfigInit();
            Directory.SetCurrentDirectory(folder.LocalPath);

            var parser = new RDLParser(File.ReadAllText(new Uri(folder, "PushedDataCalculatedFieldTest.rdl").LocalPath))
            {
                Folder = folder.LocalPath,
                SkipDatabaseSchemaValidation = true,
            };
            using Report report = await parser.Parse();
            report.Folder = folder.LocalPath;
            await report.DataSets["Data"].SetData(new List<Row> { new("ALFKI") });
            await report.RunGetData();

            string path = Path.Combine(Path.GetTempPath(), "secured-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                // Deliberately no using/Dispose on the stream generator: RunRender must finish the file itself.
                await report.RunRender(new OneFileStreamGen(path, true), OutputPresentationType.PDF,
                    PdfSecurity.Protect("user", "owner"));

                // An exclusive open fails if the generator's stream is still open.
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }

                string text = Encoding.Latin1.GetString(File.ReadAllBytes(path));
                Assert.That(text, Does.StartWith("%PDF-"));
                Assert.That(text, Does.Contain("/Encrypt"));
                Assert.That(text.TrimEnd(), Does.EndWith("%%EOF"), "the file must be complete, not cut off by an unflushed writer");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
#endif
