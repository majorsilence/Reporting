// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #296: a selected table exposes its data-region properties (NoRows, ...) in the property grid.
    [TestFixture]
    public class TablePropertyGridTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>8in</Width>
  <Body>
    <Height>4in</Height>
    <ReportItems>
      <Table Name=""Table1"">
        <Top>72pt</Top><Left>72pt</Left>
        <TableColumns><TableColumn><Width>100pt</Width></TableColumn></TableColumns>
        <Header>
          <TableRows><TableRow><Height>20pt</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""H1""><Value>a</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows>
        </Header>
        <Details>
          <TableRows><TableRow><Height>10pt</Height><TableCells>
            <TableCell><ReportItems><Textbox Name=""D1""><Value>a</Value></Textbox></ReportItems></TableCell>
          </TableCells></TableRow></TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

        private static PropertyTable Load(out XmlNode table)
        {
            var design = new DesignCtl();
            design.ReportSource = Rdl;
            design.ReportChanged += (s, e) => { };
            table = design.ReportDocument.GetElementsByTagName("Table")[0];
            return new PropertyTable(design.DrawCtl, design, new List<XmlNode> { table });
        }

        [Test]
        public void TableObject_ExposesDataRegionAndTableProperties()
        {
            var props = Load(out _);
            var names = TypeDescriptor.GetProperties(props).Cast<PropertyDescriptor>().Select(p => p.Name).ToList();
            Assert.That(names, Does.Contain("NoRows"));
            Assert.That(names, Does.Contain("DataSetName"));
            Assert.That(names, Does.Contain("KeepTogether"));
            Assert.That(names, Does.Contain("HeaderRepeatOnNewPage"));
            Assert.That(names, Does.Contain("FooterRepeatOnNewPage"));
        }

        [Test]
        public void NoRows_WritesAndClearsElement()
        {
            var props = Load(out var table);
            props.NoRows = new PropertyExpr("Nothing to show");
            Assert.That(table.SelectSingleNode("*[local-name()='NoRows']").InnerText, Is.EqualTo("Nothing to show"));
            props.NoRows = new PropertyExpr("");
            Assert.That(table.SelectSingleNode("*[local-name()='NoRows']"), Is.Null);
        }

        [Test]
        public void HeaderRepeat_WritesToHeaderElement()
        {
            var props = Load(out var table);
            Assert.That(props.HeaderRepeatOnNewPage, Is.False);
            props.HeaderRepeatOnNewPage = true;
            Assert.That(props.HeaderRepeatOnNewPage, Is.True);
            Assert.That(table.SelectSingleNode("*[local-name()='Header']/*[local-name()='RepeatOnNewPage']").InnerText, Is.EqualTo("true"));
        }

        [Test]
        public void FooterRepeat_IsIgnoredWhenTableHasNoFooter()
        {
            var props = Load(out var table);
            props.FooterRepeatOnNewPage = true;
            Assert.That(props.FooterRepeatOnNewPage, Is.False);
            Assert.That(table.SelectSingleNode("*[local-name()='Footer']"), Is.Null);
        }
    }
}
