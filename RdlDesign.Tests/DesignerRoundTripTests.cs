// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // D6: drives the designer's core "open -> edit -> save -> preview" loop end to end on the
    // Headless backend. Doesn't simulate real mouse-drag placement (that needs Majorsilence.Forms'
    // drag-drop machinery driven interactively, better verified manually per the plan's own "on
    // Linux desktop, create/save/preview a report end-to-end" note) -- instead edits the design
    // surface's underlying RDL XML directly, which is exactly what a completed drag-drop operation
    // itself produces, and verifies the design surface (MDIChild.SourceRdl) picks up and round-trips
    // that edit correctly, and that the result renders.
    [TestFixture]
    public class DesignerRoundTripTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        [Test]
        public async Task OpenEditSaveRender_RoundTripsCorrectly()
        {
            string templateDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Templates", "invoice");
            string rdlPath = Path.Combine(templateDir, "invoice.rdl");
            string rdlXml = await File.ReadAllTextAsync(rdlPath);

            string sampleDataPath = Path.Combine(templateDir, "sample-data.json");
            rdlXml = rdlXml.Replace("file=sample-data.json", $"file={sampleDataPath}");

            string openedRdlPath = Path.Combine(Path.GetTempPath(), $"d6-open-{Guid.NewGuid():N}.rdl");
            await File.WriteAllTextAsync(openedRdlPath, rdlXml);
            try
            {
                using var mc = new Majorsilence.Reporting.RdlDesign.MDIChild(800, 600);

                // 1. Open: the design surface loads and reflects the report.
                await mc.SetSourceFileAsync(new Uri(openedRdlPath));
                Assert.That(mc.SourceRdl, Does.Contain("<Textbox Name=\"D_Desc\">"),
                    "expected the design surface to reflect the opened report's content");

                // 2. Edit: insert a new report item, standing in for what a completed drag-drop
                // placement produces (a new element under the Body's <ReportItems>). The Body's
                // <ReportItems> is the first one to appear in the serialized document (Body comes
                // before PageHeader/PageFooter's own ReportItems), and doesn't directly follow
                // <Body> (there's a <Height> element in between), so target the first
                // <ReportItems> occurrence directly rather than assuming adjacency.
                const string newTextboxXml = "<Textbox Name=\"D6_NewField\"><Value>D6 round-trip test</Value></Textbox>";
                int insertAt = mc.SourceRdl.IndexOf("<ReportItems>", StringComparison.Ordinal);
                Assert.That(insertAt, Is.GreaterThanOrEqualTo(0), "test setup: expected to find a <ReportItems> insertion point");
                insertAt += "<ReportItems>".Length;
                string edited = mc.SourceRdl.Insert(insertAt, newTextboxXml);
                mc.SourceRdl = edited;

                // 3. The design surface picked up and retains the edit.
                Assert.That(mc.SourceRdl, Does.Contain("D6_NewField"),
                    "expected the design surface to retain the inserted report item after re-parsing");

                // 4. Save.
                string savedRdlPath = Path.Combine(Path.GetTempPath(), $"d6-saved-{Guid.NewGuid():N}.rdl");
                await File.WriteAllTextAsync(savedRdlPath, mc.SourceRdl);

                try
                {
                    // 5. Preview: the saved report parses and renders through RdlEngine itself
                    // (independent of the designer), proving the edit produced valid RDL.
                    string savedXml = await File.ReadAllTextAsync(savedRdlPath);
                    var parser = new RDLParser(savedXml) { Folder = templateDir };
                    using var report = await parser.Parse();

                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"parse errors: {string.Join("; ", report.ErrorItems)}");

                    await report.RunGetData();
                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"data-binding errors: {string.Join("; ", report.ErrorItems)}");

                    using var sg = new MemoryStreamGen();
                    await report.RunRender(sg, OutputPresentationType.PDF);

                    Assert.That(sg.GetStream().Length, Is.GreaterThan(0), "expected non-empty rendered PDF output");
                    Assert.That(report.ErrorMaxSeverity, Is.LessThan(8),
                        $"render errors: {string.Join("; ", report.ErrorItems)}");
                }
                finally
                {
                    File.Delete(savedRdlPath);
                }
            }
            finally
            {
                File.Delete(openedRdlPath);
            }
        }

        // #136: a List inserted into an RDLC (2008+ namespace) report must be saved as a Tablix,
        // because <List> is not part of that schema. The in-memory document keeps the List.
        [Test]
        public void SaveRdlcWithList_WritesTablixNotList()
        {
            const string rdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition"">
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <List Name=""List1""><Top>0in</Top><Left>0in</Left><Height>1in</Height><Width>2in</Width><ReportItems /></List>
    </ReportItems>
  </Body>
  <Width>4in</Width>
</Report>";

            using var mc = new Majorsilence.Reporting.RdlDesign.MDIChild(800, 600);
            mc.SourceRdl = rdlc;

            Assert.That(mc.SourceRdl, Does.Not.Contain("<List"), "RDLC schema has no List element");
            Assert.That(mc.SourceRdl, Does.Contain("<Tablix Name=\"List1\""));
        }

        // A Tablix file opens as the Table/Matrix/List the designer edits, and saves as a Tablix.
        [Test]
        public void OpenRdlcWithTablix_EditsAsListAndSavesAsTablix()
        {
            const string rdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"">
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Tablix Name=""Card"">
        <Top>0in</Top><Left>0in</Left><Height>1in</Height><Width>2in</Width>
        <TablixBody>
          <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
          <TablixRows><TablixRow><Height>1in</Height><TablixCells><TablixCell><CellContents>
            <Textbox Name=""T1""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Hello</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
          </CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
        </TablixBody>
        <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
        <TablixRowHierarchy><TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers></TablixRowHierarchy>
      </Tablix>
    </ReportItems>
  </Body>
  <Width>4in</Width>
</Report>";

            var design = new DesignCtl();
            design.ReportSource = rdlc;

            var doc = design.ReportDocument;
            Assert.That(doc.GetElementsByTagName("List"), Has.Count.EqualTo(1), "the designer edits the List it understands");
            Assert.That(doc.GetElementsByTagName("Tablix"), Is.Empty);

            var saved = design.ReportSource;
            Assert.That(saved, Does.Contain("<Tablix Name=\"Card\""));
            Assert.That(saved, Does.Not.Contain("<List"));
            Assert.That(saved, Does.Contain("<TextRun>"));
            Assert.That(saved, Does.Contain("Hello"));
        }

        // Items the engine cannot render (Sparkline, ...) must still survive an open and save.
        [Test]
        public void OpenRdlcWithSparkline_KeepsItOnSave()
        {
            const string rdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"">
  <Body><Height>2in</Height><ReportItems>
    <Sparkline Name=""Trend""><Top>0in</Top><Left>0in</Left><Height>0.5in</Height><Width>2in</Width><Marker>x</Marker></Sparkline>
  </ReportItems></Body>
  <Width>4in</Width>
</Report>";

            var design = new DesignCtl();
            design.ReportSource = rdlc;

            var saved = design.ReportSource;
            Assert.That(saved, Does.Contain("<Sparkline Name=\"Trend\""));
            Assert.That(saved, Does.Contain("<Marker>x</Marker>"), "its settings are written back unchanged");
        }

        // Visual Studio keeps identifiers in these elements; opening and saving must not drop them.
        [Test]
        public void OpenRdlc_KeepsVersionOnlyElementsOnSave()
        {
            const string rdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"" xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <AutoRefresh>0</AutoRefresh>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString>x</ConnectString></ConnectionProperties><rd:DataSourceID>11111111-1111-1111-1111-111111111111</rd:DataSourceID></DataSource></DataSources>
  <Body><Height>1in</Height><ReportItems /></Body>
  <Width>4in</Width>
  <rd:ReportUnitType>Inch</rd:ReportUnitType>
  <rd:ReportID>22222222-2222-2222-2222-222222222222</rd:ReportID>
</Report>";

            var design = new DesignCtl();
            design.ReportSource = rdlc;

            var saved = design.ReportSource;
            Assert.That(saved, Does.Contain("11111111-1111-1111-1111-111111111111"));
            Assert.That(saved, Does.Contain("22222222-2222-2222-2222-222222222222"));
            Assert.That(saved, Does.Contain("<AutoRefresh>0</AutoRefresh>"));
            Assert.That(saved, Does.Contain("Inch"));
        }

        // A 2008+ chart opens as the chart model the designer edits and saves back in the 2008+ model.
        [Test]
        public void OpenRdlcWithChart_EditsAs2005ChartAndSavesAs2008()
        {
            const string rdlc = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"">
  <Body><Height>3in</Height><ReportItems>
    <Chart Name=""C1"">
      <ChartCategoryHierarchy><ChartMembers><ChartMember><Group Name=""G""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group><Label>=Fields!Name.Value</Label></ChartMember></ChartMembers></ChartCategoryHierarchy>
      <ChartData><ChartSeriesCollection><ChartSeries Name=""S""><ChartDataPoints><ChartDataPoint><ChartDataPointValues><Y>=Sum(Fields!Amount.Value)</Y></ChartDataPointValues></ChartDataPoint></ChartDataPoints><Type>Line</Type><Subtype>Plain</Subtype></ChartSeries></ChartSeriesCollection></ChartData>
      <ChartTitles><ChartTitle Name=""Default""><Caption>Trend</Caption></ChartTitle></ChartTitles>
      <Top>0in</Top><Left>0in</Left><Height>3in</Height><Width>5in</Width>
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

            var design = new DesignCtl();
            design.ReportSource = rdlc;

            var chart = (System.Xml.XmlElement)design.ReportDocument.GetElementsByTagName("Chart")[0];
            Assert.That(chart["Type", chart.NamespaceURI].InnerText, Is.EqualTo("Line"));
            Assert.That(chart["Title", chart.NamespaceURI]["Caption", chart.NamespaceURI].InnerText, Is.EqualTo("Trend"));

            var saved = design.ReportSource;
            Assert.That(saved, Does.Contain("<ChartSeriesCollection>"));
            Assert.That(saved, Does.Contain("<ChartTitle Name=\"Default\">"));
            Assert.That(saved, Does.Not.Contain("<CategoryGroupings>"));
        }
    }
}
