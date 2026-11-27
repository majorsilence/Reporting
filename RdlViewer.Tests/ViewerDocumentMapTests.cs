// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlViewer.Tests
{
    // The document-map panel (issue #637): the viewer builds it from the report's Label entries,
    // has none for a report without them, and selecting an entry scrolls to its page.
    [TestFixture]
    public class ViewerDocumentMapTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Rdl(string labelFirst, string labelSecond) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <Body><Height>300pt</Height><ReportItems>
    <Textbox Name=""First""><Top>10pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
      <Value>first</Value>{labelFirst}</Textbox>
    <Rectangle Name=""R""><Top>40pt</Top><Left>0pt</Left><Width>300pt</Width><Height>50pt</Height>
      <PageBreakAtStart>true</PageBreakAtStart>
      <ReportItems>
        <Textbox Name=""Second""><Top>20pt</Top><Left>0pt</Left><Width>200pt</Width><Height>14pt</Height>
          <Value>second</Value>{labelSecond}</Textbox>
      </ReportItems>
    </Rectangle>
  </ReportItems></Body>
  <PageHeight>300pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<RdlViewer> View(string rdl)
        {
            var viewer = new RdlViewer { ShowWaitDialog = false, Width = 800, Height = 400 };
            await viewer.SetSourceRdl(rdl);
            await viewer.EnsureRendered();
            return viewer;
        }

        [Test]
        public async Task AReportWithLabels_HasADocumentMap_AndAnEntryScrollsToItsPage()
        {
            using var viewer = await View(Rdl("<Label>First</Label>", "<Label>Second</Label>"));

            Assert.That(viewer.HasDocumentMap, Is.True);
            Assert.That(viewer.ShowDocumentMap, Is.True, "shown by default");

            var map = viewer.DocumentMap;
            Assert.That(map.Select(e => (e.Label, e.PageIndex)), Is.EqualTo(new[] { ("First", 0), ("Second", 1) }));

            await viewer.ScrollToDocumentMapEntry(map[1]);
            Assert.That(viewer.PageCurrent, Is.EqualTo(2));

            await viewer.ScrollToDocumentMapEntry(map[0]);
            Assert.That(viewer.PageCurrent, Is.EqualTo(1));
        }

        [Test]
        public async Task AReportWithoutLabels_HasNoDocumentMap()
        {
            using var viewer = await View(Rdl("", ""));

            Assert.That(viewer.HasDocumentMap, Is.False);
            Assert.That(viewer.DocumentMap, Is.Empty);
        }
    }
}
