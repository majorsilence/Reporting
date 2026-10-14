// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Majorsilence.Pdf.Internal;
using NUnit.Framework;

namespace Majorsilence.Pdf.Tests
{
    [TestFixture]
    public class ContentCompressionTests
    {
        // Page content builders are reused between pages and documents, so the same content arrives in
        // differently sized chunks. The compressed bytes must not depend on that.
        [Test]
        public void SameContent_InDifferentlyChunkedBuilders_CompressesToTheSameBytes()
        {
            var line = "BT /F1 9 Tf 1 0 0 1 72.5 700.25 Tm (Row value 12345) Tj ET 0.5 w 72 690 m 540 690 l S\n";

            var growing = new StringBuilder();                  // many small chunks
            for (int i = 0; i < 3000; i++) growing.Append(line).Append(i).Append('\n');
            string text = growing.ToString();

            var single = new StringBuilder(text.Length + 10);   // one chunk
            single.Append(text);

            var reused = new StringBuilder();                   // grown, cleared, filled again
            for (int i = 0; i < 5000; i++) reused.Append(line);
            reused.Clear();
            foreach (char c in text) reused.Append(c);

            byte[] expected = PdfSerializer.Compress(growing);
            Assert.That(PdfSerializer.Compress(single), Is.EqualTo(expected));
            Assert.That(PdfSerializer.Compress(reused), Is.EqualTo(expected));
            Assert.That(PdfSerializer.Decompress(expected), Is.EqualTo(text));
        }
    }
}
