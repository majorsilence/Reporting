// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Drawing;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #292: borderless controls get a hover outline, and the canvas has a visible dot grid.
    [TestFixture]
    public class BorderlessOutlineTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  <Body>
    <Height>4in</Height>
    <ReportItems>
      <Rectangle Name=""Rect1"">
        <Top>0.25in</Top><Left>1in</Left><Width>2in</Width><Height>1in</Height>
      </Rectangle>
    </ReportItems>
  </Body>
  <Height>4in</Height>
</Report>";

        private static DesignXmlDraw Load(out XmlDocument doc)
        {
            doc = new XmlDocument();
            doc.LoadXml(Rdl);
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        [Test]
        public void HoverNode_AcceptsReportItem()
        {
            var d = Load(out var doc);
            var rect = doc.GetElementsByTagName("Rectangle")[0];
            d.HoverNode = rect;
            Assert.That(d.HoverNode, Is.SameAs(rect));
        }

        [Test]
        public void HoverNode_IgnoresNonReportItems()
        {
            var d = Load(out var doc);
            d.HoverNode = doc.GetElementsByTagName("Body")[0];
            Assert.That(d.HoverNode, Is.Null);
        }

        [Test]
        public void HoverNode_CanBeCleared()
        {
            var d = Load(out var doc);
            d.HoverNode = doc.GetElementsByTagName("Rectangle")[0];
            d.HoverNode = null;
            Assert.That(d.HoverNode, Is.Null);
        }

        [Test]
        public void GridDots_AreSpacedByStep()
        {
            var dots = DesignXmlDraw.GridDots(new RectangleF(10, 20, 8, 8), new Size(4, 4)).ToList();
            Assert.That(dots, Is.EqualTo(new[]
            {
                new PointF(10, 20), new PointF(14, 20),
                new PointF(10, 24), new PointF(14, 24),
            }));
        }

        [Test]
        public void GridDots_EmptyForInvalidStepOrArea()
        {
            Assert.That(DesignXmlDraw.GridDots(new RectangleF(0, 0, 10, 10), new Size(0, 4)), Is.Empty);
            Assert.That(DesignXmlDraw.GridDots(new RectangleF(0, 0, 0, 10), new Size(4, 4)), Is.Empty);
        }

        [Test]
        public void GridDotColor_IsVisibleOnWhitePage()
        {
            Assert.That(DesignXmlDraw.GridDotColor.GetBrightness(), Is.LessThan(0.8f));
        }
    }
}
