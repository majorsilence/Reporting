// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Xml;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // https://github.com/majorsilence/Reporting/issues/673
    [TestFixture]
    public class EmbeddedImagePickerTests
    {
        // 1x1 transparent PNG
        private const string Png =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

        private static DesignXmlDraw Load(string embedded = "")
        {
            var doc = new XmlDocument();
            doc.LoadXml("<Report>" + embedded + "<Body><ReportItems/></Body></Report>");
            return new DesignXmlDraw { Width = 1200, Height = 800, ReportDocument = doc };
        }

        private static EmbeddedImageValues Image(string name) =>
            new EmbeddedImageValues(name) { MIMEType = "image/png", ImageData = Png };

        private const string OneImage =
            "<EmbeddedImages><EmbeddedImage Name=\"logo\"><MIMEType>image/png</MIMEType><ImageData>" + Png +
            "</ImageData></EmbeddedImage></EmbeddedImages>";

        [Test]
        public void Import_IntoReportWithNoImages_EnablesOkAndWritesTheImage()
        {
            var d = Load("<EmbeddedImages/>");
            using var dlg = new DialogEmbeddedImages(d);
            Assert.That(dlg.CanAccept, Is.False);

            dlg.AddImage(Image("photo"));
            Assert.That(dlg.CanAccept, Is.True);

            dlg.Accept();
            Assert.That(d.ReportNames.EmbeddedImageNames, Is.EqualTo(new[] { "photo" }));
        }

        [Test]
        public void ImportedImage_IsChosenForTheReportItem()
        {
            using var dlg = new DialogEmbeddedImages(Load(OneImage));
            dlg.AddImage(Image("photo"));

            Assert.That(dlg.ChooseImageName(""), Is.EqualTo("photo"));
            Assert.That(dlg.ChooseImageName("logo"), Is.EqualTo("photo"));
        }

        [Test]
        public void NoImport_KeepsAValidCurrentValue()
        {
            using var dlg = new DialogEmbeddedImages(Load(OneImage));

            Assert.That(dlg.ChooseImageName("logo"), Is.EqualTo("logo"));
            Assert.That(dlg.ChooseImageName("=Fields!Img.Value"), Is.EqualTo("=Fields!Img.Value"));
        }

        [Test]
        public void NoImport_BlankOrStaleValue_PicksTheSelectedImage()
        {
            using var dlg = new DialogEmbeddedImages(Load(OneImage));

            Assert.That(dlg.ChooseImageName(""), Is.EqualTo("logo"));
            Assert.That(dlg.ChooseImageName("removed"), Is.EqualTo("logo"));
        }
    }
}
