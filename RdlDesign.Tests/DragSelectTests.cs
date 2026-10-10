// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #294: rubber-band selection must pick top-level report items (table, rectangle)
    // rather than reaching into their children.
    [TestFixture]
    public class DragSelectTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  <Body>
    <Height>4in</Height>
    <ReportItems>
      <Table Name=""Table1"">
        <Top>0.25in</Top><Left>0.25in</Left>
        <TableColumns>
          <TableColumn><Width>1in</Width></TableColumn>
          <TableColumn><Width>1in</Width></TableColumn>
        </TableColumns>
        <Details>
          <TableRows>
            <TableRow>
              <Height>0.25in</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""Cell1""><Value>a</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""Cell2""><Value>b</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
      <Rectangle Name=""Rect1"">
        <Top>0.25in</Top><Left>3in</Left><Width>2in</Width><Height>2in</Height>
        <ReportItems>
          <Textbox Name=""Inner1""><Top>0.5in</Top><Left>0.5in</Left><Width>1in</Width><Height>0.25in</Height><Value>x</Value></Textbox>
        </ReportItems>
      </Rectangle>
    </ReportItems>
  </Body>
  <Height>4in</Height>
</Report>";

        private static DesignXmlDraw Load()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Rdl);
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        private static string[] Selected(DesignXmlDraw d) =>
            d.SelectedList.Select(n => n.Attributes["Name"]?.Value).OrderBy(x => x).ToArray();

        [Test]
        public void RubberBandOverTable_SelectsTableNotCells()
        {
            var d = Load();
            d.SelectInRectangle(new Rectangle(0, 0, 250, 100), 0, 0);
            Assert.That(Selected(d), Is.EqualTo(new[] { "Table1" }));
        }

        [Test]
        public void RubberBandOverTableAndRectangle_SelectsBoth()
        {
            var d = Load();
            d.SelectInRectangle(new Rectangle(0, 0, 900, 400), 0, 0);
            Assert.That(Selected(d), Is.EqualTo(new[] { "Rect1", "Table1" }));
        }

        [Test]
        public void RubberBandInsideRectangle_SelectsInnerItem()
        {
            var d = Load();
            d.SelectInRectangle(new Rectangle(360, 97, 40, 5), 0, 0);
            Assert.That(Selected(d), Is.EqualTo(new[] { "Inner1" }));
        }

        [Test]
        public void RubberBandInsideRectangleOverEmptyArea_SelectsRectangle()
        {
            var d = Load();
            d.SelectInRectangle(new Rectangle(300, 60, 20, 10), 0, 0);
            Assert.That(Selected(d), Is.EqualTo(new[] { "Rect1" }));
        }

        [Test]
        public void IsMovableReportItem_RecognizesItemsOnly()
        {
            var doc = new XmlDocument();
            doc.LoadXml(Rdl);
            var rect = doc.GetElementsByTagName("Rectangle")[0];
            var body = doc.GetElementsByTagName("Body")[0];
            Assert.That(DesignXmlDraw.IsMovableReportItem(rect), Is.True);
            Assert.That(DesignXmlDraw.IsMovableReportItem(body), Is.False);
            Assert.That(DesignXmlDraw.IsMovableReportItem(null), Is.False);
        }
    }
}
