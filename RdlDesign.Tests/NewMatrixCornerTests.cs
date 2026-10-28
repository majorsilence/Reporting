// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    [TestFixture]
    public class NewMatrixCornerTests
    {
        private static DesignXmlDraw Draw()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<Report><Body><ReportItems/></Body></Report>");
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        // Issue #493: the Matrix wizard used to generate <Value>Corner</Value> as the
        // top-left corner textbox's content, so a brand-new Matrix showed the literal word
        // "Corner" as report output instead of a blank, editable cell.
        [Test]
        public void NewMatrix_CornerValueIsBlank()
        {
            var d = Draw();
            using var dlg = new DialogNewMatrix(d, d.ReportDocument.DocumentElement);
            string xml = dlg.MatrixXml;

            Assert.That(xml, Does.Contain("<Textbox Name=\"Corner\"><Value></Value>"));
            Assert.That(xml, Does.Not.Contain(">Corner<"));
        }

        [Test]
        public void NewMatrix_CornerKeepsItsBorderStyling()
        {
            var d = Draw();
            using var dlg = new DialogNewMatrix(d, d.ReportDocument.DocumentElement);
            string xml = dlg.MatrixXml;

            Assert.That(xml, Does.Contain("<BorderStyle><Default>Solid</Default></BorderStyle>"));
            Assert.That(xml, Does.Contain("<FontWeight>bold</FontWeight>"));
        }
    }
}
