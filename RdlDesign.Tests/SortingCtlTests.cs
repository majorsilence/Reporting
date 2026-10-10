// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using Majorsilence.Forms;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    [TestFixture]
    public class SortingCtlTests
    {
        [Test]
        public void CheckBoxCell_CommitsImmediately()
        {
            Assert.That(SortingCtl.ShouldCommitOnDirty(new DataGridViewCheckBoxCell()), Is.True);
        }

        [Test]
        public void TextCell_DoesNotCommitOnDirty()
        {
            Assert.That(SortingCtl.ShouldCommitOnDirty(new DataGridViewTextBoxCell()), Is.False);
        }

        [Test]
        public void NullCell_DoesNotCommitOnDirty()
        {
            Assert.That(SortingCtl.ShouldCommitOnDirty(null), Is.False);
        }
    }
}
