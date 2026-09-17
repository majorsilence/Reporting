// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    [TestFixture]
    public class DesignerUtilityTests
    {
        [TestCase("=Parameters!Test.Value", "Test")]
        [TestCase("={?Test}", "Test")]
        public void ExtractParameterNameFromExpression(string expression, string expectedName)
        {
            var result = DesignerUtility.ExtractParameterNameFromParameterExpression(expression);
            Assert.That(result, Is.EqualTo(expectedName));
        }
    }
}
