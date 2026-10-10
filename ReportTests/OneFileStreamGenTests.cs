#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace ReportTests
{
    /// <summary>
    /// The output file names OneFileStreamGen creates. A path without an extension (what PHP's
    /// tempnam() hands the native library) used to throw ArgumentOutOfRangeException, and a bare
    /// file name used to be rooted at the file system root.
    /// </summary>
    [TestFixture]
    public class OneFileStreamGenTests
    {
        [Test]
        public void PathWithoutExtension_CreatesExactlyThatFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "ofsg-noext-" + Guid.NewGuid().ToString("N"));
            try
            {
                var sg = new OneFileStreamGen(path, true);
                sg.GetStream().WriteByte(1);
                sg.CloseMainStream();

                Assert.That(File.Exists(path), Is.True, "the file must be created at the requested path, not at 'path.'");
                Assert.That(Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + "*").Select(Path.GetFileName),
                    Is.EqualTo(new[] { Path.GetFileName(path) }), "no file with a trailing dot or other suffix");
                Assert.That(new FileInfo(path).Length, Is.EqualTo(1));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void PathWithExtension_IsUnchanged()
        {
            string path = Path.Combine(Path.GetTempPath(), "ofsg-ext-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var sg = new OneFileStreamGen(path, true);
                sg.CloseMainStream();
                Assert.That(File.Exists(path), Is.True);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void BareFileName_IsRelativeToTheCurrentDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ofsg-rel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string previous = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(dir);
                var sg = new OneFileStreamGen("out.csv", true);
                sg.CloseMainStream();
                Assert.That(File.Exists(Path.Combine(dir, "out.csv")), Is.True);
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                Directory.Delete(dir, true);
            }
        }
    }
}
#endif
