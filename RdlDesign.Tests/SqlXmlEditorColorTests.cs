// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Majorsilence.Forms;
using Majorsilence.Reporting.RdlDesign.Syntax;
using Majorsilence.Reporting.RdlViewer;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #527: the SQL editor (DataSetsCtl) and the RDL XML editor (RdlEditPreview) are coloured
    // by the shim's stand-ins for Scintilla's SQL and XML lexers, in colours that follow the theme.
    [TestFixture]
    [NonParallelizable]     // Theme is process-wide
    public class SqlXmlEditorColorTests
    {
        [TearDown]
        public void ResetTheme() => Theme.SetBuiltInTheme(BuiltInTheme.Default);

        private static Scintilla SqlEditor(string text)
        {
            var scintilla = new Scintilla();
            new ScintillaSqlStyle(scintilla);
            scintilla.Text = text;
            return scintilla;
        }

        private static Scintilla XmlEditor(string text)
        {
            var scintilla = new Scintilla();
            ScintillaXMLStyle.ConfigureScintillaStyle(scintilla);
            scintilla.Text = text;
            return scintilla;
        }

        private static List<TextSpanStyle> Spans(Scintilla scintilla) => scintilla.Colorizer(scintilla.Text).ToList();

        // The colour of the span covering exactly the first `token` at or after `from`; null when no
        // span covers exactly it.
        private static Color? ColorOf(Scintilla scintilla, string token, int from = 0)
        {
            int at = scintilla.Text.IndexOf(token, from, System.StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), token);
            var span = Spans(scintilla).Where(s => s.Start == at && s.Length == token.Length).Select(s => (TextSpanStyle?)s).FirstOrDefault();
            if (span == null) return null;
            var c = span.Value.Color;
            return Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
        }

        private static void AssertColor(Scintilla scintilla, string token, Color expected, int from = 0)
        {
            var actual = ColorOf(scintilla, token, from);
            Assert.That(actual, Is.Not.Null, $"no span covers '{token}'");
            Assert.That(actual.Value.ToArgb(), Is.EqualTo(expected.ToArgb()), $"colour of '{token}'");
        }

        [Test]
        public void Sql_DefaultTheme_KeepsTheClassicColors()
        {
            var sql = SqlEditor("SELECT Name, COUNT(*) FROM Sales -- note\nWHERE Code = 'a''b' AND Qty > 10 /* x */");

            Assert.Multiple(() =>
            {
                AssertColor(sql, "SELECT", Color.Blue);
                AssertColor(sql, "FROM", Color.Blue);
                AssertColor(sql, "COUNT", Color.Fuchsia);
                AssertColor(sql, "AND", Color.Gray);
                AssertColor(sql, "'a''b'", Color.Red);
                AssertColor(sql, "-- note", Color.Green);
                AssertColor(sql, "/* x */", Color.Green);
                AssertColor(sql, "10", Color.Maroon);
                AssertColor(sql, "=", Color.Black);
                Assert.That(ColorOf(sql, "Name"), Is.Null, "a column name keeps the editor's colour");
            });
        }

        [Test]
        public void Sql_NamesThatLookLikeKeywords_AreNotColored()
        {
            var sql = SqlEditor("SELECT [select], @count, dbo.sum FROM #table");

            Assert.Multiple(() =>
            {
                Assert.That(ColorOf(sql, "[select]"), Is.Null);
                Assert.That(ColorOf(sql, "@count"), Is.Null);
                Assert.That(ColorOf(sql, "sum"), Is.Null);
                Assert.That(ColorOf(sql, "#table"), Is.Null);
            });
        }

        [Test]
        public void Xml_DefaultTheme_KeepsTheClassicColors()
        {
            var xml = XmlEditor("<?xml version=\"1.0\"?>\n<Report a='x'><!-- c --><Value>a &amp; b</Value></Report>");

            Assert.Multiple(() =>
            {
                AssertColor(xml, "<?xml", Color.Blue);
                AssertColor(xml, "version", Color.Red);
                AssertColor(xml, "\"1.0\"", Color.DeepPink);
                AssertColor(xml, "<Report", Color.Blue);
                AssertColor(xml, "'x'", Color.DeepPink);
                AssertColor(xml, "<!-- c -->", Color.Green);
                AssertColor(xml, "&amp;", Color.Red);
                AssertColor(xml, "</Value", Color.Blue);
                Assert.That(ColorOf(xml, "a &amp;"), Is.Null, "text between tags keeps the editor's colour");
            });
        }

        [Test]
        public void DarkTheme_ColorsAreLighter()
        {
            Theme.SetBuiltInTheme(BuiltInTheme.Dark);
            var sql = SqlEditor("SELECT 'x' = 1");
            var xml = XmlEditor("<Report Name=\"x\"/>");

            Assert.Multiple(() =>
            {
                AssertColor(sql, "SELECT", Color.FromArgb(0x56, 0x9C, 0xD6));
                AssertColor(sql, "'x'", Color.FromArgb(0xCE, 0x91, 0x78));
                AssertColor(sql, "=", ChromeColors.Text);       // was black, unreadable on dark
                AssertColor(xml, "<Report", Color.FromArgb(0x56, 0x9C, 0xD6));
                AssertColor(xml, "Name", Color.FromArgb(0x9C, 0xDC, 0xFE));
            });
        }

        [Test]
        public void ThemeChange_RecolorsAnEditorAlreadyConfigured()
        {
            var sql = SqlEditor("SELECT 1");
            AssertColor(sql, "SELECT", Color.Blue);

            Theme.SetBuiltInTheme(BuiltInTheme.Dark);

            AssertColor(sql, "SELECT", Color.FromArgb(0x56, 0x9C, 0xD6));
        }

        [Test]
        public void ThemeCssVariable_SetsTheColor()
        {
            var result = DesignerTheme.ApplyCss(":root { --rdl-syntax-keyword: #123456; --rdl-syntax-tag: #654321; }");
            Assert.That(result.Messages, Is.Empty);

            AssertColor(SqlEditor("SELECT 1"), "SELECT", Color.FromArgb(0x12, 0x34, 0x56));
            AssertColor(XmlEditor("<Report/>"), "<Report", Color.FromArgb(0x65, 0x43, 0x21));
        }

        [Test]
        public void UnterminatedTokens_RunToTheirEnd_WithoutThrowing()
        {
            Assert.Multiple(() =>
            {
                Assert.DoesNotThrow(() => Spans(SqlEditor("SELECT 'abc")));
                Assert.DoesNotThrow(() => Spans(SqlEditor("/* open")));
                Assert.DoesNotThrow(() => Spans(SqlEditor("SELECT [open")));
                Assert.DoesNotThrow(() => Spans(XmlEditor("<a b=\"open")));
                Assert.DoesNotThrow(() => Spans(XmlEditor("<!-- open")));
                Assert.DoesNotThrow(() => Spans(XmlEditor("<a <b>")));
                Assert.DoesNotThrow(() => Spans(XmlEditor("")));
            });
            AssertColor(SqlEditor("SELECT 'abc"), "'abc", Color.Red);
        }
    }
}
