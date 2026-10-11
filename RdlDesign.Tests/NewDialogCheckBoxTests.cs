// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using System.Xml;
using Majorsilence.Forms;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    [TestFixture]
    public class NewDialogCheckBoxTests
    {
        private static DesignXmlDraw Draw()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<Report><Body><ReportItems/></Body></Report>");
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        private static bool CheckOnClick(object dialog, string field)
        {
            var f = dialog.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null, field);
            return ((CheckedListBox)f.GetValue(dialog)).CheckOnClick;
        }

        [Test]
        public void NewMatrix_ListsToggleOnFirstClick()
        {
            var d = Draw();
            using var dlg = new DialogNewMatrix(d, d.ReportDocument.DocumentElement);
            Assert.That(CheckOnClick(dlg, "lbMatrixColumns"), Is.True);
            Assert.That(CheckOnClick(dlg, "lbMatrixRows"), Is.True);
        }

        [Test]
        public void NewTable_ListToggleOnFirstClick()
        {
            var d = Draw();
            using var dlg = new DialogNewTable(d, d.ReportDocument.DocumentElement);
            Assert.That(CheckOnClick(dlg, "lbTableColumns"), Is.True);
        }
    }
}
