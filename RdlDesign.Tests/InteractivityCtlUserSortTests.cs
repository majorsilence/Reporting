// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    /// <summary>
    /// The Interactivity tab's interactive sort fields (a textbox's UserSort, #636): they read the
    /// UserSort element, write it back, and remove it when the sort is turned off.
    /// </summary>
    [TestFixture]
    public class InteractivityCtlUserSortTests
    {
        private const string Rdl = @"<Report><Body><ReportItems>
<Table Name=""Table1""><DataSetName>Data</DataSetName>
  <TableGroups><TableGroup><Grouping Name=""RegionGroup""><GroupExpressions><GroupExpression>=Fields!Region.Value</GroupExpression></GroupExpressions></Grouping></TableGroup></TableGroups>
  <Header><TableRows><TableRow><TableCells>
    <TableCell><ReportItems><Textbox Name=""SortName""><Value>Name</Value>{0}</Textbox></ReportItems></TableCell>
  </TableCells></TableRow></TableRows></Header>
</Table>
<List Name=""List1""><DataSetName>Data</DataSetName><ReportItems><Textbox Name=""Other""><Value>x</Value></Textbox></ReportItems></List>
</ReportItems></Body></Report>";

        private static (DesignXmlDraw Draw, XmlNode Textbox) Load(string userSort = "")
        {
            var doc = new XmlDocument();
            doc.LoadXml(string.Format(Rdl, userSort));
            var draw = new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
            return (draw, doc.SelectSingleNode("//Textbox[@Name='SortName']"));
        }

        private static T Field<T>(object o, string name)
        {
            var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null, name);
            return (T)f.GetValue(o);
        }

        [Test]
        public void Reads_the_user_sort_and_lists_the_groups_and_regions()
        {
            var (draw, tb) = Load("<UserSort><SortExpression>=Fields!Name.Value</SortExpression><SortExpressionScope>RegionGroup</SortExpressionScope><SortTarget>List1</SortTarget></UserSort>");
            using var ctl = new InteractivityCtl(draw, new List<XmlNode> { tb });

            Assert.That(Field<GroupBox>(ctl, "grpUserSort").Visible, Is.True);
            Assert.That(Field<CheckBox>(ctl, "chkUserSort").Checked, Is.True);
            Assert.That(Field<TextBox>(ctl, "tbSortExpression").Text, Is.EqualTo("=Fields!Name.Value"));
            Assert.That(Field<ComboBox>(ctl, "cbSortScope").Text, Is.EqualTo("RegionGroup"));
            Assert.That(Field<ComboBox>(ctl, "cbSortTarget").Text, Is.EqualTo("List1"));
            var names = Field<ComboBox>(ctl, "cbSortScope").Items.Cast<object>().Select(o => o.ToString()).ToArray();
            Assert.That(names, Is.SupersetOf(new[] { "", "List1", "RegionGroup", "Table1" }));
        }

        [Test]
        public void Writes_a_new_user_sort_without_the_blank_optional_elements()
        {
            var (draw, tb) = Load();
            using var ctl = new InteractivityCtl(draw, new List<XmlNode> { tb });
            Assert.That(Field<CheckBox>(ctl, "chkUserSort").Checked, Is.False);
            Assert.That(Field<TextBox>(ctl, "tbSortExpression").Enabled, Is.False);

            Field<CheckBox>(ctl, "chkUserSort").Checked = true;
            Field<TextBox>(ctl, "tbSortExpression").Text = "=Fields!Name.Value";
            Assert.That(ctl.IsValid(), Is.True);
            ctl.Apply();

            XmlNode us = tb.SelectSingleNode("UserSort");
            Assert.That(us, Is.Not.Null);
            Assert.That(us.SelectSingleNode("SortExpression")?.InnerText, Is.EqualTo("=Fields!Name.Value"));
            Assert.That(us.SelectSingleNode("SortExpressionScope"), Is.Null);
            Assert.That(us.SelectSingleNode("SortTarget"), Is.Null);
        }

        [Test]
        public void Changes_and_removes_the_user_sort()
        {
            var (draw, tb) = Load("<UserSort><SortExpression>=Fields!Name.Value</SortExpression><SortTarget>List1</SortTarget></UserSort>");
            using (var ctl = new InteractivityCtl(draw, new List<XmlNode> { tb }))
            {
                Field<ComboBox>(ctl, "cbSortScope").Text = "RegionGroup";
                Field<ComboBox>(ctl, "cbSortTarget").Text = "";
                ctl.Apply();
            }
            Assert.That(tb.SelectSingleNode("UserSort/SortExpressionScope")?.InnerText, Is.EqualTo("RegionGroup"));
            Assert.That(tb.SelectSingleNode("UserSort/SortTarget"), Is.Null);
            Assert.That(tb.SelectSingleNode("UserSort/SortExpression")?.InnerText, Is.EqualTo("=Fields!Name.Value"));

            using (var ctl = new InteractivityCtl(draw, new List<XmlNode> { tb }))
            {
                Field<CheckBox>(ctl, "chkUserSort").Checked = false;
                ctl.Apply();
            }
            Assert.That(tb.SelectSingleNode("UserSort"), Is.Null);
        }

        [Test]
        public void An_unchanged_tab_leaves_the_user_sort_alone()
        {
            var (draw, tb) = Load("<UserSort><SortExpression>=Fields!Name.Value</SortExpression></UserSort>");
            string before = tb.OuterXml;
            using var ctl = new InteractivityCtl(draw, new List<XmlNode> { tb });
            ctl.Apply();
            Assert.That(tb.OuterXml, Is.EqualTo(before));
        }

        [Test]
        public void The_sort_fields_are_hidden_for_other_report_items()
        {
            var (draw, _) = Load();
            XmlNode list = draw.ReportDocument.SelectSingleNode("//List");
            using var ctl = new InteractivityCtl(draw, new List<XmlNode> { list });
            Assert.That(Field<GroupBox>(ctl, "grpUserSort").Visible, Is.False);
            ctl.Apply();
            Assert.That(list.SelectSingleNode("UserSort"), Is.Null);
        }
    }
}
