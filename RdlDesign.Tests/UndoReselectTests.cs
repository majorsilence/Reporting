// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #297: undo must re-select the report item(s) it reverted.
    [TestFixture]
    public class UndoReselectTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  <Body>
    <Height>4in</Height>
    <ReportItems>
      <Textbox Name=""A""><Top>0.25in</Top><Left>0.25in</Left><Width>1in</Width><Height>0.25in</Height><Value>a</Value></Textbox>
      <Textbox Name=""B""><Top>1in</Top><Left>0.25in</Left><Width>1in</Width><Height>0.25in</Height><Value>b</Value></Textbox>
    </ReportItems>
  </Body>
  <Height>4in</Height>
</Report>";

        private XmlDocument _doc;
        private DesignXmlDraw _draw;
        private Undo _undo;

        [SetUp]
        public void SetUp()
        {
            _doc = new XmlDocument();
            _doc.LoadXml(Rdl);
            _draw = new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = _doc };
            _undo = new Undo(_doc, 300) { GroupsOnly = true };
        }

        private XmlNode Item(string name) =>
            _doc.SelectSingleNode($"//*[@Name='{name}']");

        private string[] Selected() =>
            _draw.SelectedList.Select(n => n.Attributes["Name"]?.Value).OrderBy(x => x).ToArray();

        private void UndoAndReselect()
        {
            var affected = new List<XmlNode>();
            _undo.undo(affected);
            _draw.SelectAffectedByUndo(affected);
        }

        [Test]
        public void UndoMove_ReselectsMovedItem()
        {
            _undo.StartUndoGroup("move");
            Item("A").SelectSingleNode("*[local-name()='Left']").InnerText = "3in";
            _undo.EndUndoGroup();
            _draw.SetSelection(Item("B"), false);

            UndoAndReselect();

            Assert.That(Selected(), Is.EqualTo(new[] { "A" }));
            Assert.That(Item("A").SelectSingleNode("*[local-name()='Left']").InnerText, Is.EqualTo("0.25in"));
        }

        [Test]
        public void UndoGroupOverTwoItems_ReselectsBoth()
        {
            _undo.StartUndoGroup("move both");
            Item("A").SelectSingleNode("*[local-name()='Left']").InnerText = "3in";
            Item("B").SelectSingleNode("*[local-name()='Left']").InnerText = "3in";
            _undo.EndUndoGroup();
            _draw.SelectedList.Clear();

            UndoAndReselect();

            Assert.That(Selected(), Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void UndoDelete_ReselectsRestoredItem()
        {
            var a = Item("A");
            _undo.StartUndoGroup("delete");
            a.ParentNode.RemoveChild(a);
            _undo.EndUndoGroup();
            _draw.SelectedList.Clear();

            UndoAndReselect();

            Assert.That(Selected(), Is.EqualTo(new[] { "A" }));
        }

        [Test]
        public void UndoInsert_DoesNotSelectRemovedItem()
        {
            var items = Item("A").ParentNode;
            _undo.StartUndoGroup("insert");
            var tb = _doc.CreateElement("Textbox", items.NamespaceURI);
            var name = _doc.CreateAttribute("Name");
            name.Value = "C";
            tb.Attributes.Append(name);
            items.AppendChild(tb);
            _undo.EndUndoGroup();
            _draw.SetSelection(tb, false);

            UndoAndReselect();

            Assert.That(_draw.SelectedList.Contains(tb), Is.False);
        }
    }
}
