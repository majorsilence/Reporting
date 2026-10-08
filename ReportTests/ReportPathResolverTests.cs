using System.IO;
using Majorsilence.Reporting.RdlAsp;
using NUnit.Framework;

namespace ReportTests
{
    [TestFixture]
    public class ReportPathResolverTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "rdl-resolver-" + Path.GetRandomFileName(), "Reports");
            Directory.CreateDirectory(Path.Combine(_root, "sub"));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(Path.GetDirectoryName(_root), true);

        [TestCase("a.rdl")]
        [TestCase("sub/a.rdl")]
        [TestCase("./a.rdl")]
        [TestCase("sub/../a.rdl")]
        public void InsideRoot_IsResolved(string file)
        {
            var result = ReportPathResolver.ResolveWithin(_root, file);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith(Path.GetFullPath(_root)));
        }

        [TestCase("../secret.rdl")]
        [TestCase("sub/../../secret.rdl")]
        [TestCase("..\\..\\secret.rdl")]
        [TestCase("/etc/passwd")]
        [TestCase("*.rdl")]
        [TestCase("a?.rdl")]
        [TestCase("")]
        [TestCase(null)]
        public void OutsideRootOrInvalid_IsRejected(string file)
        {
            // "..\\" is only a separator on Windows, elsewhere it is a plain file name inside the root.
            if (file == "..\\..\\secret.rdl" && Path.DirectorySeparatorChar == '/')
            {
                Assert.Pass();
            }
            Assert.That(ReportPathResolver.ResolveWithin(_root, file), Is.Null);
        }

        [Test]
        public void SiblingFolderWithSamePrefix_IsRejected()
        {
            var sibling = _root + "-other";
            Directory.CreateDirectory(sibling);
            Assert.That(ReportPathResolver.ResolveWithin(_root, "../Reports-other/a.rdl"), Is.Null);
        }

        [TestCase("a.rdl", true)]
        [TestCase("sub/a.rdl", false)]
        [TestCase("..", false)]
        [TestCase("*.rdl", false)]
        public void BareFileName(string file, bool expected)
            => Assert.That(ReportPathResolver.IsBareFileName(file), Is.EqualTo(expected));
    }
}
