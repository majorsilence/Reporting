#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ReportTests
{
    /// <summary>
    /// A table cell merged down over the rows below it (#648): Tablix's RowSpan, as Report
    /// Builder and the RDLC designer save it, and the same RowSpan on a 2005 TableCell. The
    /// rows below have no cell where it reaches; their cells take the columns it leaves free.
    /// </summary>
    [TestFixture]
    public class TableRowSpanTests
    {
        // The report from the issue: [Dept (2 rows)] [Count (2 columns)] [Total (2 rows)]
        // over [A] [B], then a detail row of four cells. 0.25in rows are 18pt.
        private const string Tablix = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"">
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets>
    <DataSet Name=""DS"">
      <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
      <Fields>
        <Field Name=""Dept""><DataField>Dept</DataField></Field>
        <Field Name=""A""><DataField>A</DataField></Field>
        <Field Name=""B""><DataField>B</DataField></Field>
        <Field Name=""Total""><DataField>Total</DataField></Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Tablix Name=""T1"">
        <TablixBody>
          <TablixColumns>
            <TablixColumn><Width>1.2in</Width></TablixColumn>
            <TablixColumn><Width>1in</Width></TablixColumn>
            <TablixColumn><Width>1in</Width></TablixColumn>
            <TablixColumn><Width>1in</Width></TablixColumn>
          </TablixColumns>
          <TablixRows>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""HDept""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Dept</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox><RowSpan>2</RowSpan></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HCount""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Count</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox><ColSpan>2</ColSpan></CellContents></TablixCell>
                <TablixCell />
                <TablixCell><CellContents><Textbox Name=""HTotal""><Paragraphs><Paragraph><TextRuns><TextRun><Value>Total</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox><RowSpan>2</RowSpan></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell />
                <TablixCell><CellContents><Textbox Name=""HA""><Paragraphs><Paragraph><TextRuns><TextRun><Value>A</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""HB""><Paragraphs><Paragraph><TextRuns><TextRun><Value>B</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
                <TablixCell />
              </TablixCells>
            </TablixRow>
            <TablixRow>
              <Height>0.25in</Height>
              <TablixCells>
                <TablixCell><CellContents><Textbox Name=""DDept""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=Fields!Dept.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""DA""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=Fields!A.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""DB""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=Fields!B.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
                <TablixCell><CellContents><Textbox Name=""DTotal""><Paragraphs><Paragraph><TextRuns><TextRun><Value>=Fields!Total.Value</Value></TextRun></TextRuns></Paragraph></Paragraphs></Textbox></CellContents></TablixCell>
              </TablixCells>
            </TablixRow>
          </TablixRows>
        </TablixBody>
        <TablixColumnHierarchy>
          <TablixMembers><TablixMember /><TablixMember /><TablixMember /><TablixMember /></TablixMembers>
        </TablixColumnHierarchy>
        <TablixRowHierarchy>
          <TablixMembers>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><KeepWithGroup>After</KeepWithGroup></TablixMember>
            <TablixMember><Group Name=""Details"" /></TablixMember>
          </TablixMembers>
        </TablixRowHierarchy>
        <DataSetName>DS</DataSetName>
        <Top>0in</Top><Left>0in</Left><Height>0.75in</Height><Width>4.2in</Width>
      </Tablix>
    </ReportItems>
  </Body>
  <Width>6in</Width>
  <Page><PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth></Page>
</Report>";

        // A 2005 table whose first header cell spans two rows, holding {spanned} text.
        private static string Table2005(string spanned, string spannedStyle = "") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""DS"">
    <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>DS</DataSetName>
      <TableColumns><TableColumn><Width>60pt</Width></TableColumn><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
      <Header><TableRows>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><RowSpan>2</RowSpan><ReportItems>
            <Textbox Name=""Title""><Value>{spanned}</Value><CanGrow>true</CanGrow><Style><FontSize>10pt</FontSize>{spannedStyle}</Style></Textbox>
          </ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""R1""><Value>first</Value><Style><FontSize>10pt</FontSize></Style></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""R2""><Value>second</Value><Style><FontSize>10pt</FontSize></Style></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ColSpan>2</ColSpan><ReportItems><Textbox Name=""After""><Value>AFTER</Value><Style><FontSize>10pt</FontSize></Style></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
      </TableRows></Header>
    </Table>
  </ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static async Task<Report> Parse(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            string errors = string.Join(" | ", report.ErrorItems?.Cast<string>() ?? Enumerable.Empty<string>());
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), errors);
            Assert.That(errors, Does.Not.Contain("RowSpan"));
            if (report.DataSets["DS"] != null)
            {
                var table = new DataTable();
                foreach (var c in new[] { "Dept", "A", "B", "Total" })
                    table.Columns.Add(c, typeof(string));
                table.Rows.Add("North", "10", "2", "12");
                table.Rows.Add("South", "20", "4", "24");
                await report.DataSets["DS"].SetData(table);
            }
            await report.RunGetData(null);
            return report;
        }

        private static async Task<PageText[]> Printed(string rdl)
        {
            using var report = await Parse(rdl);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().ToArray();
        }

        private static PageText Text(PageText[] texts, string text) => texts.Single(t => t.Text == text);

        [Test]
        public async Task ATablixWithRowSpans_LaysEachCellOutOverTheRowsItSpans()
        {
            var texts = await Printed(Tablix);

            PageText dept = Text(texts, "Dept"), count = Text(texts, "Count"), total = Text(texts, "Total");
            PageText a = Text(texts, "A"), b = Text(texts, "B");
            float top = dept.Y;

            Assert.That(dept.H, Is.EqualTo(36f).Within(0.05f), "two 18pt rows");
            Assert.That(total.H, Is.EqualTo(36f).Within(0.05f), "two 18pt rows");
            Assert.That(total.Y, Is.EqualTo(top).Within(0.05f));
            Assert.That(total.X - dept.X, Is.EqualTo(3.2f * 72).Within(0.05f), "Total stays in the fourth column");
            Assert.That(count.H, Is.EqualTo(18f).Within(0.05f));
            Assert.That(count.W, Is.EqualTo(144f).Within(0.05f), "Count spans two columns");

            // The second row's cells move past the column Dept covers.
            Assert.That(a.Y, Is.EqualTo(top + 18f).Within(0.05f));
            Assert.That(a.X - dept.X, Is.EqualTo(1.2f * 72).Within(0.05f));
            Assert.That(b.X - dept.X, Is.EqualTo(2.2f * 72).Within(0.05f));

            // The detail rows follow the two header rows.
            PageText north = Text(texts, "North"), south = Text(texts, "South");
            Assert.That(north.Y, Is.EqualTo(top + 36f).Within(0.05f));
            Assert.That(south.Y, Is.EqualTo(top + 54f).Within(0.05f));
            Assert.That(Text(texts, "24").X - dept.X, Is.EqualTo(3.2f * 72).Within(0.05f));
        }

        [Test]
        public async Task ASpanningCellThatFits_LeavesTheRowsTheirHeight()
        {
            var texts = await Printed(Table2005("short"));

            PageText title = Text(texts, "short"), second = Text(texts, "second"), after = Text(texts, "AFTER");
            Assert.That(title.H, Is.EqualTo(40f).Within(0.05f));
            Assert.That(second.Y - title.Y, Is.EqualTo(20f).Within(0.05f));
            Assert.That(second.X - title.X, Is.EqualTo(60f).Within(0.05f));
            Assert.That(after.Y - title.Y, Is.EqualTo(40f).Within(0.05f));
        }

        // Text taller than the rows it spans grows the last of them, and the rows after move down.
        [Test]
        public async Task AGrowingSpanningCell_GrowsTheLastRowItSpans()
        {
            const string lines = "=\"one\" &amp; Chr(10) &amp; \"two\" &amp; Chr(10) &amp; \"three\" &amp; Chr(10) &amp; \"four\" &amp; Chr(10) &amp; \"five\"";
            var texts = await Printed(Table2005(lines));

            PageText title = texts.Single(t => t.Text.StartsWith("one")), first = Text(texts, "first"),
                second = Text(texts, "second"), after = Text(texts, "AFTER");
            Assert.That(title.H, Is.GreaterThan(55f), "five lines of 10pt text");
            Assert.That(first.H, Is.EqualTo(20f).Within(0.05f), "the first row keeps its height");
            Assert.That(second.Y + second.H, Is.EqualTo(title.Y + title.H).Within(0.05f), "the second row takes what is left");
            Assert.That(after.Y, Is.EqualTo(title.Y + title.H).Within(0.05f));
        }

        // Each record is two rows with the name spanning both; at the foot of a page the
        // record moves to the next whole, and the name with it.
        [Test]
        public async Task ARecordWhoseCellSpansItsRows_IsNotSplitAcrossPages()
        {
            const string rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""DS"">
    <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>100pt</Height><ReportItems>
    <Table Name=""T""><DataSetName>DS</DataSetName>
      <TableColumns><TableColumn><Width>100pt</Width></TableColumn><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
      <Details><TableRows>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><RowSpan>2</RowSpan><ReportItems><Textbox Name=""Name""><Value>=Fields!Dept.Value</Value></Textbox></ReportItems></TableCell>
          <TableCell><ReportItems><Textbox Name=""Up""><Value>=""up "" &amp; Fields!Dept.Value</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
        <TableRow><Height>20pt</Height><TableCells>
          <TableCell><ReportItems><Textbox Name=""Down""><Value>=""down "" &amp; Fields!Dept.Value</Value></Textbox></ReportItems></TableCell>
        </TableCells></TableRow>
      </TableRows></Details>
    </Table>
  </ReportItems></Body>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <PageHeight>100pt</PageHeight><PageWidth>300pt</PageWidth>
</Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            var table = new DataTable();
            table.Columns.Add("Dept", typeof(string));
            for (int i = 1; i <= 5; i++)
                table.Rows.Add("d" + i);
            await report.DataSets["DS"].SetData(table);
            await report.RunGetData(null);
            var pages = (await report.BuildPages()).Cast<Page>().ToList();

            Assert.That(pages, Has.Count.GreaterThan(1));
            for (int i = 1; i <= 5; i++)
            {
                Page Of(string text) => pages.Single(p => p.Cast<PageItem>().OfType<PageText>().Any(t => t.Text == text));
                Page name = Of("d" + i);
                Assert.That(Of("up d" + i), Is.SameAs(name), "d" + i);
                Assert.That(Of("down d" + i), Is.SameAs(name), "d" + i);
                var cell = name.Cast<PageItem>().OfType<PageText>().Single(t => t.Text == "d" + i);
                var down = name.Cast<PageItem>().OfType<PageText>().Single(t => t.Text == "down d" + i);
                Assert.That(cell.Y + cell.H, Is.EqualTo(down.Y + down.H).Within(0.05f));
            }
        }

        // A record taller than a page is paged row by row; the rows a cell spans still go
        // to the next page together. Rows 1-4 fill 80pt of a 110pt page; row 5 would fit below
        // them, but not with row 6, and the cell spans both.
        [Test]
        public async Task InARecordTallerThanAPage_TheRowsACellSpansMoveToTheNextPageTogether()
        {
            string Row(string name, string cells) => $@"<TableRow><Height>20pt</Height><TableCells>{cells}</TableCells></TableRow>";
            string Cell(string name, string extra = "") => $@"<TableCell>{extra}<ReportItems><Textbox Name=""{name}""><Value>{name}</Value></Textbox></ReportItems></TableCell>";
            string rows = string.Concat(Enumerable.Range(1, 4).Select(i => Row("r" + i, Cell("a" + i) + Cell("b" + i))))
                + Row("r5", Cell("span", "<RowSpan>2</RowSpan>") + Cell("b5"))
                + Row("r6", Cell("b6"));
            string rdl = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <DataSources><DataSource Name=""Src""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""DS"">
    <Query><DataSourceName>Src</DataSourceName><CommandText>none</CommandText></Query>
    <Fields><Field Name=""Dept""><DataField>Dept</DataField></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>110pt</Height><ReportItems>
    <Table Name=""T""><DataSetName>DS</DataSetName>
      <TableColumns><TableColumn><Width>100pt</Width></TableColumn><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
      <Details><TableRows>{rows}</TableRows></Details>
    </Table>
  </ReportItems></Body>
  <TopMargin>0pt</TopMargin><BottomMargin>0pt</BottomMargin>
  <PageHeight>110pt</PageHeight><PageWidth>300pt</PageWidth>
</Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4));
            var table = new DataTable();
            table.Columns.Add("Dept", typeof(string));
            table.Rows.Add("d1");
            await report.DataSets["DS"].SetData(table);
            await report.RunGetData(null);
            var pages = (await report.BuildPages()).Cast<Page>().ToList();

            int PageOf(string text) => pages.FindIndex(p => p.Cast<PageItem>().OfType<PageText>().Any(t => t.Text == text));
            Assert.That(PageOf("a4"), Is.EqualTo(0));
            Assert.That(PageOf("span"), Is.EqualTo(1));
            Assert.That(PageOf("b5"), Is.EqualTo(1));
            Assert.That(PageOf("b6"), Is.EqualTo(1));
            var span = pages[1].Cast<PageItem>().OfType<PageText>().Single(t => t.Text == "span");
            var b6 = pages[1].Cast<PageItem>().OfType<PageText>().Single(t => t.Text == "b6");
            Assert.That(span.Y + span.H, Is.EqualTo(b6.Y + b6.H).Within(0.05f));
        }

        [Test]
        public async Task Html_WritesTheRowSpan()
        {
            using var report = await Parse(Tablix);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.HTML);
            string html = ms.GetText();

            Assert.That(Regex.Matches(html, "rowspan=2").Count, Is.EqualTo(2), html);
            Assert.That(Regex.Matches(html, "colspan=2").Count, Is.EqualTo(1), html);
        }

        [Test]
        public async Task Excel_MergesTheCellDownOverTheRowsItSpans()
        {
            string folder = Path.Combine(Path.GetTempPath(), "rowspan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                using var report = await Parse(Tablix);
                string path = Path.Combine(folder, "out.xlsx");
                using (var sg = new OneFileStreamGen(path, true))
                    await report.RunRender(sg, OutputPresentationType.Excel2007);

                string xml;
                using (var zip = ZipFile.OpenRead(path))
                using (var reader = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()))
                    xml = reader.ReadToEnd();
                var merges = Regex.Matches(xml, "<mergeCell ref=\"([A-Z]+)(\\d+):([A-Z]+)(\\d+)\"").Cast<Match>()
                    .Select(m => (C1: m.Groups[1].Value, R1: int.Parse(m.Groups[2].Value), C2: m.Groups[3].Value, R2: int.Parse(m.Groups[4].Value)))
                    .ToList();

                Assert.That(merges.Count(m => m.C1 == m.C2 && m.R2 == m.R1 + 1), Is.EqualTo(2), xml);   // Dept and Total
                Assert.That(merges.Count(m => m.C1 != m.C2 && m.R1 == m.R2), Is.EqualTo(1), xml);       // Count
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public async Task Word_MergesTheCellDownOverTheRowsItSpans()
        {
            using var report = await Parse(Tablix);
            using var ms = new MemoryStreamGen();
            await report.RunRender(ms, OutputPresentationType.Word);
            byte[] docx = ((MemoryStream)ms.GetStream()).ToArray();

            var doc = new XmlDocument();
            using (var zip = new ZipArchive(new MemoryStream(docx)))
            using (Stream s = zip.GetEntry("word/document.xml")!.Open())
                doc.Load(s);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            var rows = doc.SelectNodes("//w:tbl/w:tr", ns)!.Cast<XmlNode>().ToList();

            Assert.That(rows, Has.Count.EqualTo(4));
            string Merge(XmlNode tc) => tc.SelectSingleNode("w:tcPr/w:vMerge", ns) is XmlElement v
                ? (v.GetAttribute("val", ns.LookupNamespace("w")) is { Length: > 0 } val ? val : "continue") : "";
            var first = rows[0].SelectNodes("w:tc", ns)!.Cast<XmlNode>().ToList();
            var second = rows[1].SelectNodes("w:tc", ns)!.Cast<XmlNode>().ToList();
            Assert.That(first.Select(Merge), Is.EqualTo(new[] { "restart", "", "restart" }));
            Assert.That(second.Select(Merge), Is.EqualTo(new[] { "continue", "", "", "continue" }), "each row has a cell for all four columns");
            Assert.That(second[1].InnerText + second[2].InnerText, Is.EqualTo("AB"));
        }

        [Test]
        public void Export_KeepsThePlaceholdersTheRowSpanCovers_AndReadsBackTheSame()
        {
            const string ns = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";
            var doc = new XmlDocument();
            doc.LoadXml(Table2005("title").Replace("http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition", ns));

            Rdl2008Exporter.ConvertToTablix(doc);

            var tablixRows = doc.GetElementsByTagName("TablixRow").Cast<XmlElement>().ToList();
            Assert.That(tablixRows.Select(r => r["TablixCells"].ChildNodes.Count), Is.EqualTo(new[] { 2, 2, 2 }),
                "2008 has a cell for every position");
            Assert.That(tablixRows[0].GetElementsByTagName("RowSpan")[0].InnerText, Is.EqualTo("2"));
            Assert.That(tablixRows[1]["TablixCells"].FirstChild["CellContents"], Is.Null, "the position the span covers");

            Rdl2008Normalizer.Normalize(doc, null);

            var tableRows = doc.GetElementsByTagName("TableRow").Cast<XmlElement>().ToList();
            Assert.That(tableRows.Select(r => r["TableCells"].ChildNodes.Count), Is.EqualTo(new[] { 2, 1, 1 }));
            Assert.That(tableRows[0]["TableCells"].FirstChild["RowSpan"].InnerText, Is.EqualTo("2"));
        }
    }
}
#endif
