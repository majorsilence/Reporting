using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// SVG export (issue #634): each page is a vector image of its own. Page 1 goes to the main
    /// stream and every later page to a further stream (report.svg, report2.svg, ... on disk).
    /// </summary>
    [TestFixture]
    public class SvgRenderTests
    {
        private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        // A 400pt x 300pt page; a table grouped on G with a page break after each group, so
        // groups A, B and C make three pages. Each page has a heading, a filled rectangle and a line.
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>400pt</PageWidth><PageHeight>300pt</PageHeight>
  <LeftMargin>10pt</LeftMargin><RightMargin>10pt</RightMargin>
  <TopMargin>10pt</TopMargin><BottomMargin>10pt</BottomMargin>
  <Width>380pt</Width>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString></ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS</DataSourceName><CommandText>select</CommandText></Query>
    <Fields><Field Name=""G""><DataField>G</DataField><TypeName>System.String</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>120pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>380pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""ByG""><GroupExpressions><GroupExpression>=Fields!G.Value</GroupExpression></GroupExpressions><PageBreakAtEnd>true</PageBreakAtEnd></Grouping>
        <Header><TableRows><TableRow><Height>100pt</Height>
          <TableCells><TableCell><ReportItems>
            <Rectangle Name=""Band""><ReportItems>
              <Textbox Name=""Heading""><Top>0pt</Top><Left>0pt</Left><Width>300pt</Width><Height>20pt</Height>
                <Style><FontSize>14pt</FontSize></Style>
                <Value>=""GROUP "" &amp; Fields!G.Value</Value></Textbox>
              <Rectangle Name=""Box""><Top>30pt</Top><Left>0pt</Left><Width>100pt</Width><Height>40pt</Height>
                <Style><BackgroundColor>#3366CC</BackgroundColor></Style></Rectangle>
              <Line Name=""Rule""><Top>80pt</Top><Left>0pt</Left><Width>300pt</Width><Height>0pt</Height>
                <Style><BorderStyle><Default>Solid</Default></BorderStyle><BorderColor><Default>Red</Default></BorderColor><BorderWidth><Default>2pt</Default></BorderWidth></Style></Line>
            </ReportItems></Rectangle>
          </ReportItems></TableCell></TableCells>
        </TableRow></TableRows></Header>
      </TableGroup></TableGroups>
      <Details><TableRows><TableRow><Height>1pt</Height><Visibility><Hidden>true</Hidden></Visibility>
        <TableCells><TableCell><ReportItems><Textbox Name=""Detail""><Value>=Fields!G.Value</Value></Textbox></ReportItems></TableCell></TableCells>
      </TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
</Report>";

        private static readonly string[] Groups = { "A", "B", "C" };

        // 400pt and 300pt in CSS pixels (96 per inch), the SVG user unit.
        private const double ExpectedWidth = 400.0 / 72.0 * 96.0;
        private const double ExpectedHeight = 300.0 / 72.0 * 96.0;

        private static async Task<Report> BuildReport()
        {
            var report = await new RDLParser(Rdl).Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), "test report has fatal parse errors");
            var data = new DataTable();
            data.Columns.Add("G", typeof(string));
            foreach (var g in Groups)
                data.Rows.Add(g);
            await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);
            return report;
        }

        private static XDocument ParseSvg(byte[] bytes, string what)
        {
            Assert.That(bytes, Is.Not.Empty, $"{what} is empty");
            XDocument doc;
            using (var ms = new MemoryStream(bytes))
                doc = XDocument.Load(ms);
            Assert.That(doc.Root, Is.Not.Null, $"{what} has no root element");
            Assert.That(doc.Root!.Name, Is.EqualTo(Svg + "svg"), $"{what} root is not <svg>");
            return doc;
        }

        private static double Number(XElement e, string attribute)
        {
            string? v = (string?)e.Attribute(attribute);
            Assert.That(v, Is.Not.Null, $"<svg> has no {attribute}");
            return double.Parse(v!, CultureInfo.InvariantCulture);
        }

        private static string AllText(XDocument doc) =>
            string.Concat(doc.Descendants(Svg + "text").Select(t => t.Value));

        private static void AssertPage(XDocument doc, int pageNo)
        {
            string what = $"page {pageNo}";
            XElement root = doc.Root!;
            Assert.That(Number(root, "width"), Is.EqualTo(ExpectedWidth).Within(0.5), $"{what} width");
            Assert.That(Number(root, "height"), Is.EqualTo(ExpectedHeight).Within(0.5), $"{what} height");

            string[] viewBox = ((string?)root.Attribute("viewBox") ?? "").Split(' ');
            Assert.That(viewBox, Has.Length.EqualTo(4), $"{what} viewBox");
            Assert.That(double.Parse(viewBox[2], CultureInfo.InvariantCulture), Is.EqualTo(ExpectedWidth).Within(0.5), $"{what} viewBox width");
            Assert.That(double.Parse(viewBox[3], CultureInfo.InvariantCulture), Is.EqualTo(ExpectedHeight).Within(0.5), $"{what} viewBox height");

            // Text stays text, and only this page's group is on it.
            string text = AllText(doc);
            string expected = "GROUP " + Groups[pageNo - 1];
            Assert.That(text.Replace(" ", ""), Does.Contain(expected.Replace(" ", "")), $"{what} text: '{text}'");
            foreach (var other in Groups.Where((g, i) => i != pageNo - 1))
                Assert.That(text, Does.Not.Contain("GROUP " + other), $"{what} shows another page's group");

            // Each character keeps its glyph position: a list strict readers accept, one entry per
            // character, and no whitespace collapsing to shift them.
            foreach (XElement t in doc.Descendants(Svg + "text"))
            {
                string x = (string?)t.Attribute("x") ?? "";
                Assert.That(x.TrimEnd(), Does.Not.EndWith(","), $"{what} glyph positions end in a separator: '{x}'");
                Assert.That((string?)t.Attribute(XNamespace.Xml + "space"), Is.EqualTo("preserve"), $"{what} text whitespace");
                if (x.Length > 0)
                    Assert.That(x.Split(',').Length, Is.EqualTo(t.Value.Length), $"{what} one position per character of '{t.Value}'");
            }

            // Drawn as vectors, not as a picture of the page.
            Assert.That(doc.Descendants(Svg + "image"), Is.Empty, $"{what} contains a raster image");
            Assert.That(doc.Descendants().Count(e => e.Name == Svg + "rect" || e.Name == Svg + "path" || e.Name == Svg + "line"),
                Is.GreaterThanOrEqualTo(2), $"{what} has no shapes for the rectangle and the line");
        }

        [Test]
        public async Task MemoryStreamGen_EachPageIsItsOwnSvg()
        {
            var report = await BuildReport();

            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.SVG);

            // Page 1 is the main stream; pages 2.. are the further streams, in order.
            Assert.That(sg.MemoryList.Count, Is.EqualTo(Groups.Length), "one stream per page");
            Assert.That(sg.MemoryList[0], Is.SameAs(sg.GetStream()), "page 1 is the main stream");
            for (int i = 0; i < sg.MemoryList.Count; i++)
            {
                byte[] bytes = ((MemoryStream)sg.MemoryList[i]!).ToArray();
                AssertPage(ParseSvg(bytes, $"page {i + 1}"), i + 1);
            }
        }

        [Test]
        public async Task OneFileStreamGen_WritesReportSvgThenNumberedPages()
        {
            var report = await BuildReport();
            string dir = Path.Combine(Path.GetTempPath(), "SvgRenderTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var sg = new OneFileStreamGen(Path.Combine(dir, "report.svg"), true);
                try
                {
                    await report.RunRender(sg, OutputPresentationType.SVG);
                }
                finally
                {
                    sg.CloseMainStream();
                }

                var expected = new[] { "report.svg", "report2.svg", "report3.svg" };
                Assert.That(Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal),
                    Is.EqualTo(expected));
                for (int i = 0; i < expected.Length; i++)
                    AssertPage(ParseSvg(File.ReadAllBytes(Path.Combine(dir, expected[i])), expected[i]), i + 1);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public async Task RenderFormatName_IsImage()
        {
            var report = await new RDLParser(Rdl.Replace(@"=""GROUP "" &amp; Fields!G.Value", "=Globals!RenderFormat.Name")).Parse();
            var data = new DataTable();
            data.Columns.Add("G", typeof(string));
            data.Rows.Add("A");
            await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);

            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, OutputPresentationType.SVG);

            var doc = ParseSvg(((MemoryStream)sg.GetStream()).ToArray(), "page 1");
            Assert.That(AllText(doc), Does.Contain("IMAGE"));
        }
    }
}
