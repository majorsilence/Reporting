using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// SSRS's PDF renderer drops ToolTips (they are an interactive-HTML feature), while a
    /// hyperlink Action becomes a link annotation. Emitting tooltips as PDF Text annotations
    /// litters the page with sticky-note icons that SSRS output never had, so the PDF render
    /// path must skip them.
    /// </summary>
    [TestFixture]
    public class PdfAnnotationTests
    {
        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        private static string ReportWith (string textboxExtras)
            => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>6in</Width>
  <Body>
    <ReportItems>
      <Textbox Name=""T1"">
        <Value>Some text</Value>
        {textboxExtras}
        <Top>0.1in</Top><Left>0.1in</Left><Height>0.3in</Height><Width>4in</Width>
      </Textbox>
    </ReportItems>
    <Height>1in</Height>
  </Body>
  <Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
</Report>";

        private static async Task<string> RenderPdfLatin1 (string rdl)
        {
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                "parse errors: " + string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));

            await report.RunGetData (null);

            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.PDF);

            var bytes = ((MemoryStream)memory.GetStream ()).ToArray ();
            return Encoding.GetEncoding ("ISO-8859-1").GetString (bytes);
        }

        [Test]
        public async Task Tooltip_DoesNotBecomeAStickyNoteAnnotation ()
        {
            var pdf = await RenderPdfLatin1 (ReportWith ("<ToolTip>Report Title</ToolTip>"));

            Assert.That (pdf, Does.Not.Contain ("/Subtype /Text"),
                "a ToolTip must not surface as a sticky-note annotation; SSRS drops tooltips in PDF output");
        }

        [Test]
        public async Task Hyperlink_StillBecomesALinkAnnotation ()
        {
            var pdf = await RenderPdfLatin1 (ReportWith (
                "<Action><Hyperlink>https://example.test/target</Hyperlink></Action>"));

            Assert.That (pdf, Does.Contain ("/Subtype /Link"),
                "hyperlink actions must still render as link annotations");
        }
    }
}
