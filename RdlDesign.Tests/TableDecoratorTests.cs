// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #293: handles beside a table select a whole row, column or the table.
    [TestFixture]
    public class TableDecoratorTests
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
        <Top>0.5in</Top><Left>0.5in</Left>
        <TableColumns>
          <TableColumn><Width>1in</Width></TableColumn>
          <TableColumn><Width>1in</Width></TableColumn>
          <TableColumn><Width>1in</Width></TableColumn>
        </TableColumns>
        <Header>
          <TableRows>
            <TableRow>
              <Height>0.25in</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""H1""><Value>a</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""H2""><Value>b</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""H3""><Value>c</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Header>
        <Details>
          <TableRows>
            <TableRow>
              <Height>0.25in</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""D1""><Value>a</Value></Textbox></ReportItems></TableCell>
                <TableCell><ColSpan>2</ColSpan><ReportItems><Textbox Name=""D23""><Value>b</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
            <TableRow>
              <Height>0.25in</Height>
              <TableCells>
                <TableCell><ReportItems/></TableCell>
                <TableCell><ReportItems><Textbox Name=""E2""><Value>b</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems/></TableCell>
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

        private static string[] Names(System.Collections.Generic.IEnumerable<XmlNode> nodes) =>
            nodes.Select(n => n.Attributes["Name"]?.Value).OrderBy(x => x).ToArray();

        [Test]
        public void TableOf_FindsTableFromCellItem()
        {
            var d = Load(out var table);
            var h1 = table.SelectNodes(".//*[@Name='H1']").Cast<XmlNode>().First();
            Assert.That(DesignXmlDraw.TableOf(h1), Is.SameAs(table));
            Assert.That(DesignXmlDraw.TableOf(table), Is.SameAs(table));
            Assert.That(DesignXmlDraw.TableOf(table.ParentNode), Is.Null);
            Assert.That(DesignXmlDraw.TableOf(null), Is.Null);
        }

        [Test]
        public void SelectRow_SelectsEveryItemInTheRow()
        {
            var d = Load(out var table);
            Assert.That(d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Row, Table = table, Index = 0 }, false), Is.True);
            Assert.That(Names(d.SelectedList), Is.EqualTo(new[] { "H1", "H2", "H3" }));
        }

        [Test]
        public void SelectColumn_SelectsEveryItemInTheColumn_HonoringColSpan()
        {
            var d = Load(out var table);
            d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Column, Table = table, Index = 1 }, false);
            // column 1 is covered by the spanning cell D23 in the first details row
            Assert.That(Names(d.SelectedList), Is.EqualTo(new[] { "D23", "E2", "H2" }));
        }

        [Test]
        public void SelectTable_SelectsTheTableItself()
        {
            var d = Load(out var table);
            d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Table, Table = table }, false);
            Assert.That(d.SelectedList, Is.EqualTo(new[] { table }));
        }

        [Test]
        public void Select_EmptyRowOrColumn_KeepsSelection()
        {
            var d = Load(out var table);
            d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Table, Table = table }, false);
            // details row 2 / column 0 holds only empty cells in the second row, but D1 and H1 exist;
            // an out of range row has nothing
            Assert.That(d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Row, Table = table, Index = 99 }, false), Is.False);
            Assert.That(d.SelectedList, Is.EqualTo(new[] { table }));
        }

        [Test]
        public void Select_AddKeepsExistingSelection()
        {
            var d = Load(out var table);
            d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Row, Table = table, Index = 0 }, false);
            d.SelectTableDecorator(new DesignXmlDraw.TableDecoratorHit
                { Kind = DesignXmlDraw.TableDecoratorKind.Column, Table = table, Index = 0 }, true);
            Assert.That(Names(d.SelectedList), Is.EqualTo(new[] { "D1", "H1", "H2", "H3" }));
        }

        [Test]
        public void Layout_PlacesHandlesAlongTopAndLeftEdges()
        {
            var l = DesignXmlDraw.LayoutDecorator(new RectangleF(100, 50, 60, 30), new float[] { 20, 40 }, new float[] { 10, 20 });
            Assert.That(l.Columns, Has.Count.EqualTo(2));
            Assert.That(l.Rows, Has.Count.EqualTo(2));
            Assert.That(l.Columns[1], Is.EqualTo(new RectangleF(120, 50 - DesignXmlDraw.DecoratorSize, 40, DesignXmlDraw.DecoratorSize)));
            Assert.That(l.Rows[1], Is.EqualTo(new RectangleF(100 - DesignXmlDraw.DecoratorSize, 60, DesignXmlDraw.DecoratorSize, 20)));
            Assert.That(l.Corner.Right, Is.EqualTo(100));
            Assert.That(l.Corner.Bottom, Is.EqualTo(50));
        }

        [Test]
        public void HitDecorator_ReportsTheHandleUnderThePoint()
        {
            var l = DesignXmlDraw.LayoutDecorator(new RectangleF(100, 50, 60, 30), new float[] { 20, 40 }, new float[] { 10, 20 });
            var col = DesignXmlDraw.HitDecorator(l, new PointF(130, 46));
            Assert.That((col.Kind, col.Index), Is.EqualTo((DesignXmlDraw.TableDecoratorKind.Column, 1)));
            var row = DesignXmlDraw.HitDecorator(l, new PointF(96, 55));
            Assert.That((row.Kind, row.Index), Is.EqualTo((DesignXmlDraw.TableDecoratorKind.Row, 0)));
            Assert.That(DesignXmlDraw.HitDecorator(l, new PointF(96, 46)).Kind, Is.EqualTo(DesignXmlDraw.TableDecoratorKind.Table));
            Assert.That(DesignXmlDraw.HitDecorator(l, new PointF(130, 60)).Kind, Is.EqualTo(DesignXmlDraw.TableDecoratorKind.None));
        }

        [Test]
        public void DecoratedTable_FollowsSelectionThenHover()
        {
            var d = Load(out var table);
            Assert.That(d.DecoratedTable, Is.Null);
            d.HoverTable = table;
            Assert.That(d.DecoratedTable, Is.SameAs(table));
            d.HoverTable = null;
            d.SetSelection(table.SelectNodes(".//*[@Name='H1']").Cast<XmlNode>().First(), false);
            Assert.That(d.DecoratedTable, Is.SameAs(table));
        }

        [Test]
        public void HitTableDecorator_FindsHandleInRealLayout()
        {
            var d = Load(out var table);
            d.HoverTable = table;
            var tr = d.GetRectangle(table);
            Assert.That(tr.IsEmpty, Is.False);
            // probe just above the middle of the second column (points -> pixels at 96 dpi)
            const float PxPerPt = 96f / 72f;
            int px = (int)((tr.Left + 1.5f * 72) * PxPerPt), py = (int)((tr.Top - 3) * PxPerPt);
            var hit = d.HitTableDecorator(new Point(px, py), 0, 0);
            Assert.That(hit.Kind, Is.EqualTo(DesignXmlDraw.TableDecoratorKind.Column));
            Assert.That(hit.Index, Is.EqualTo(1));
        }
    }
}
