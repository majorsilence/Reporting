// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #295: dragging a selected table's corner/edge handles resizes it.
    [TestFixture]
    public class TableResizeTests
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
        <Top>72pt</Top><Left>72pt</Left>
        <TableColumns>
          <TableColumn><Width>100pt</Width></TableColumn>
          <TableColumn><Width>50pt</Width></TableColumn>
        </TableColumns>
        <Header>
          <TableRows>
            <TableRow>
              <Height>20pt</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""H1""><Value>a</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""H2""><Value>b</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Header>
        <Details>
          <TableRows>
            <TableRow>
              <Height>10pt</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""D1""><Value>a</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""D2""><Value>b</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
  <Height>4in</Height>
</Report>";

        private static DesignXmlDraw Load(out XmlNode table)
        {
            var doc = new XmlDocument();
            doc.LoadXml(Rdl);
            table = doc.GetElementsByTagName("Table")[0];
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        private static float[] Sizes(XmlNode table, string container, string item) =>
            table.SelectNodes($".//*[local-name()='{item}']/*[local-name()='{container}']").Cast<XmlNode>()
                .Select(n => float.Parse(n.InnerText.Replace("pt", ""), CultureInfo.InvariantCulture)).ToArray();

        private static float Child(XmlNode table, string name) =>
            float.Parse(table.SelectSingleNode($"*[local-name()='{name}']").InnerText.Replace("pt", ""), CultureInfo.InvariantCulture);

        [Test]
        public void BottomRight_ScalesColumnsAndRowsProportionally()
        {
            var d = Load(out var table);
            Assert.That(d.ResizeTable(table, 75, 15, HitLocationEnum.BottomRight), Is.True);
            Assert.That(Sizes(table, "Width", "TableColumn"), Is.EqualTo(new[] { 150f, 75f }).Within(0.01f));
            Assert.That(Sizes(table, "Height", "TableRow"), Is.EqualTo(new[] { 30f, 15f }).Within(0.01f));
            Assert.That(Child(table, "Left"), Is.EqualTo(72f));
            Assert.That(Child(table, "Top"), Is.EqualTo(72f));
        }

        [Test]
        public void RightMiddle_ChangesOnlyWidth()
        {
            var d = Load(out var table);
            d.ResizeTable(table, 30, 99, HitLocationEnum.RightMiddle);
            Assert.That(Sizes(table, "Width", "TableColumn"), Is.EqualTo(new[] { 120f, 60f }).Within(0.01f));
            Assert.That(Sizes(table, "Height", "TableRow"), Is.EqualTo(new[] { 20f, 10f }));
        }

        [Test]
        public void BottomMiddle_ChangesOnlyHeight()
        {
            var d = Load(out var table);
            d.ResizeTable(table, 99, 30, HitLocationEnum.BottomMiddle);
            Assert.That(Sizes(table, "Width", "TableColumn"), Is.EqualTo(new[] { 100f, 50f }));
            Assert.That(Sizes(table, "Height", "TableRow"), Is.EqualTo(new[] { 40f, 20f }).Within(0.01f));
        }

        [Test]
        public void TopLeft_ShrinksAndMovesTheTable()
        {
            var d = Load(out var table);
            d.ResizeTable(table, 15, 6, HitLocationEnum.TopLeft);
            Assert.That(Sizes(table, "Width", "TableColumn"), Is.EqualTo(new[] { 90f, 45f }).Within(0.01f));
            Assert.That(Sizes(table, "Height", "TableRow"), Is.EqualTo(new[] { 16f, 8f }).Within(0.01f));
            Assert.That(Child(table, "Left"), Is.EqualTo(87f).Within(0.01f));
            Assert.That(Child(table, "Top"), Is.EqualTo(78f).Within(0.01f));
        }

        [Test]
        public void TopLeft_CannotMoveOffThePage()
        {
            var d = Load(out var table);
            d.ResizeTable(table, -500, -500, HitLocationEnum.TopLeft);
            Assert.That(Child(table, "Left"), Is.EqualTo(0f));
            Assert.That(Child(table, "Top"), Is.EqualTo(0f));
            Assert.That(Sizes(table, "Width", "TableColumn").Sum(), Is.EqualTo(150f + 72f).Within(0.05f));
        }

        [Test]
        public void Shrinking_StopsAtAMinimumSize()
        {
            var d = Load(out var table);
            d.ResizeTable(table, -10000, -10000, HitLocationEnum.BottomRight);
            Assert.That(Sizes(table, "Width", "TableColumn").All(w => w >= 0.1f), Is.True);
            Assert.That(Sizes(table, "Height", "TableRow").All(h => h >= 0.1f), Is.True);
        }

        [Test]
        public void NoChange_ReturnsFalse()
        {
            var d = Load(out var table);
            Assert.That(d.ResizeTable(table, 0, 0, HitLocationEnum.BottomRight), Is.False);
            Assert.That(d.ResizeTable(table, 10, 10, HitLocationEnum.Move), Is.False);
            Assert.That(d.ResizeTable(table.ParentNode, 10, 10, HitLocationEnum.BottomRight), Is.False);
        }

        [Test]
        public void ResizeReportItem_RoutesTablesToResizeTable()
        {
            var d = Load(out var table);
            // 20 pixels is 15 points at 96 dpi
            Assert.That(d.ResizeReportItem(table, 20, 0, HitLocationEnum.RightMiddle), Is.True);
            Assert.That(Sizes(table, "Width", "TableColumn").Sum(), Is.EqualTo(165f).Within(0.2f));
        }

        [Test]
        public void Handles_AreAtCornersAndEdgeMiddles()
        {
            var h = DesignXmlDraw.TableResizeHandles(new RectangleF(10, 20, 100, 40))
                .ToDictionary(x => x.Key, x => x.Value);
            Assert.That(h, Has.Count.EqualTo(8));
            Assert.That(h[HitLocationEnum.TopLeft], Is.EqualTo(new PointF(10, 20)));
            Assert.That(h[HitLocationEnum.BottomRight], Is.EqualTo(new PointF(110, 60)));
            Assert.That(h[HitLocationEnum.RightMiddle], Is.EqualTo(new PointF(110, 40)));
            Assert.That(h[HitLocationEnum.TopMiddle], Is.EqualTo(new PointF(60, 20)));
        }

        [Test]
        public void HitTableResizeHandle_FindsHandlesStraddlingTheEdge()
        {
            var r = new RectangleF(10, 20, 100, 40);
            Assert.That(DesignXmlDraw.HitTableResizeHandle(r, new PointF(111, 61)), Is.EqualTo(HitLocationEnum.BottomRight));
            Assert.That(DesignXmlDraw.HitTableResizeHandle(r, new PointF(109, 40)), Is.EqualTo(HitLocationEnum.RightMiddle));
            Assert.That(DesignXmlDraw.HitTableResizeHandle(r, new PointF(60, 40)), Is.EqualTo(HitLocationEnum.Inside));
        }

        [Test]
        public void HitNode_OnlyReportsHandlesWhenTheTableIsSelected()
        {
            var d = Load(out var table);
            const float PxPerPt = 96f / 72f;
            var tr = d.GetRectangle(table);
            // bottom right corner of the table: 150pt x 30pt
            var p = new Point((int)((tr.Left + 150 + 1) * PxPerPt), (int)((tr.Top + 30 + 1) * PxPerPt));
            var before = d.HitNode(p, 0, 0);
            Assert.That(before == null || before.HitSpot != HitLocationEnum.BottomRight, Is.True);

            d.SetSelection(table, false);
            var hl = d.HitNode(p, 0, 0);
            Assert.That(hl, Is.Not.Null);
            Assert.That(hl.HitNode, Is.SameAs(table));
            Assert.That(hl.HitSpot, Is.EqualTo(HitLocationEnum.BottomRight));
        }
    }
}
