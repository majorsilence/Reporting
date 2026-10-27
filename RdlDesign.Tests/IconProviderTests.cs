// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using Majorsilence.Forms;
using Majorsilence.Forms.Drawing;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #468: a host should be able to override individual designer toolbar/menu icons
    // (keyed by a stable name) while keeping the built-in defaults for icons it doesn't override.
    [TestFixture]
    public class IconProviderTests
    {
        private class FixedIconProvider : IDesignerIconProvider
        {
            private readonly string _name;
            private readonly Image _icon;

            public FixedIconProvider(string name, Image icon)
            {
                _name = name;
                _icon = icon;
            }

            public Image GetIcon(string name) => name == _name ? _icon : null;
        }

        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(target)!;

        [Test]
        public void NoProvider_LeavesBuiltInIconsUnchanged()
        {
            using var designer = new RdlUserControl();
            var newButton = Field<ToolStripButton>(designer, "newToolStripButton1");
            Image original = newButton.Image;

            Assert.That(designer.IconProvider, Is.Null);
            Assert.That(newButton.Image, Is.SameAs(original));
        }

        [Test]
        public void SettingProvider_OverridesOnlyTheNamedIcon()
        {
            using var designer = new RdlUserControl();
            var newButton = Field<ToolStripButton>(designer, "newToolStripButton1");
            var cutButton = Field<ToolStripButton>(designer, "cutToolStripButton1");
            Image originalCutIcon = cutButton.Image;

            var replacement = new Bitmap(16, 16);
            designer.IconProvider = new FixedIconProvider("document_new", replacement);

            Assert.That(newButton.Image, Is.SameAs(replacement), "overridden icon is swapped in");
            Assert.That(cutButton.Image, Is.SameAs(originalCutIcon), "icons the provider returns null for keep their default");
        }
    }
}
