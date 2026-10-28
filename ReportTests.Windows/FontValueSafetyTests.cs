// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using Majorsilence.Reporting.RdlDesign;
using NUnit.Framework;

namespace ReportTests.Windows
{
    [TestFixture]
    public class FontValueSafetyTests
    {
        private static DesignXmlDraw Draw()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<Report><Body><ReportItems/></Body></Report>");
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        private static XmlNode TextboxWithStyle(DesignXmlDraw d, string styleInner)
        {
            var doc = d.ReportDocument;
            XmlNode tb = doc.CreateElement("Textbox");
            XmlNode style = doc.CreateElement("Style");
            style.InnerXml = styleInner;
            tb.AppendChild(style);
            return tb;
        }

        // Issue #492: an empty <FontFamily/> used to index InnerText[0] on an empty string and
        // throw an IndexOutOfRangeException when the designer inspected the textbox's style.
        [Test]
        public async Task EmptyFontFamily_DoesNotThrow_AndDefaultsToArial()
        {
            var d = Draw();
            XmlNode tb = TextboxWithStyle(d, "<FontFamily></FontFamily>");

            StyleInfo si = null;
            Assert.DoesNotThrowAsync(async () => si = await d.GetStyleInfo(tb));
            Assert.That(si.FontFamily, Is.EqualTo("Arial"));
        }

        // Issue #492: an empty <FontSize/> hit the same InnerText[0] index-out-of-range, and a
        // unit-only value such as "pt" resolved to 0pt instead of a safe default.
        [Test]
        public async Task EmptyFontSize_DoesNotThrow_AndDefaultsToTenPoint()
        {
            var d = Draw();
            XmlNode tb = TextboxWithStyle(d, "<FontSize></FontSize>");

            StyleInfo si = null;
            Assert.DoesNotThrowAsync(async () => si = await d.GetStyleInfo(tb));
            Assert.That(si.FontSize, Is.EqualTo(10f));
        }

        [Test]
        public async Task UnitOnlyFontSize_DefaultsToTenPointInsteadOfZero()
        {
            var d = Draw();
            XmlNode tb = TextboxWithStyle(d, "<FontSize>pt</FontSize>");

            StyleInfo si = await d.GetStyleInfo(tb);
            Assert.That(si.FontSize, Is.EqualTo(10f));
        }

        [Test]
        public async Task ExpressionFontFamilyAndSize_FallBackToSafeDefaults()
        {
            var d = Draw();
            XmlNode tb = TextboxWithStyle(d, "<FontFamily>=Fields!Font.Value</FontFamily><FontSize>=Fields!Size.Value</FontSize>");

            StyleInfo si = await d.GetStyleInfo(tb);
            Assert.That(si.FontFamily, Is.EqualTo("Arial"));
            Assert.That(si.FontSize, Is.EqualTo(10f));
        }
    }
}
