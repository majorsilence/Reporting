// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #469: a host should be able to intercept a handful of named designer commands
    // (data sources, dataset properties) and skip the native dialog by setting Handled.
    [TestFixture]
    public class DesignerCommandEventArgsTests
    {
        [Test]
        public void DesignerCommandEventArgs_DefaultsToNotHandled()
        {
            var args = new DesignerCommandEventArgs();
            Assert.That(args.Handled, Is.False);

            args.Handled = true;
            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void DesignerDataSetCommandEventArgs_CarriesTheDatasetNameAndIsHandleable()
        {
            var args = new DesignerDataSetCommandEventArgs("Orders");
            Assert.That(args.DataSetName, Is.EqualTo("Orders"));
            Assert.That(args.Handled, Is.False);

            args.Handled = true;
            Assert.That(args.Handled, Is.True);
        }
    }
}
