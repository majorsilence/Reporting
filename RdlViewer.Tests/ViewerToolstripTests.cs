// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Reflection;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlViewer.Tests
{
    [TestFixture]
    public class ViewerToolstripTests
    {
        // The toolbar looked its images up under the old fyiReporting.RdlViewer resource names, found no
        // stream, and Majorsilence.Forms made a 1x1 placeholder of each: Open, Save As and Print had no icon.
        [TestCase("document-open.png")]
        [TestCase("document-save.png")]
        [TestCase("document-print.png")]
        public void ToolbarImages_AreFound(string file)
        {
            var toolbar = new ViewerToolstrip();
            var getImage = typeof(ViewerToolstrip).GetMethod("GetImage", BindingFlags.NonPublic | BindingFlags.Instance);

            var image = (Majorsilence.Forms.Drawing.Bitmap)getImage.Invoke(toolbar, new object[] { file });

            Assert.That(image.Width, Is.EqualTo(16));
            Assert.That(image.Height, Is.EqualTo(16));
        }
    }
}
