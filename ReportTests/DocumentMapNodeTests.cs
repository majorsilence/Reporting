#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportTests
{
    /// <summary>
    /// DocumentMapNode.Build, which nests the flat document map for the viewers' panels.
    /// </summary>
    [TestFixture]
    public class DocumentMapNodeTests
    {
        private static DocumentMapEntry E(string label, int level, int page = 0) =>
            new DocumentMapEntry(label, level, page, 0, 0);

        // The tree written out an entry a line, two spaces a level.
        private static string[] Outline(IEnumerable<DocumentMapNode> nodes, int depth = 0) =>
            nodes.SelectMany(n => new[] { new string(' ', depth * 2) + n.Entry.Label }
                .Concat(Outline(n.Children, depth + 1))).ToArray();

        [Test]
        public void NoEntries_NoNodes()
        {
            Assert.That(DocumentMapNode.Build(Array.Empty<DocumentMapEntry>()), Is.Empty);
        }

        [Test]
        public void Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DocumentMapNode.Build(null));
        }

        [Test]
        public void EntriesNest_ByLevel_InOrder()
        {
            var roots = DocumentMapNode.Build(new[]
            {
                E("Table", 0), E("Group a", 1), E("Row 1", 2), E("Row 2", 2), E("Group b", 1), E("Row 3", 2), E("After", 0),
            });

            Assert.That(Outline(roots), Is.EqualTo(new[]
            {
                "Table", "  Group a", "    Row 1", "    Row 2", "  Group b", "    Row 3", "After",
            }));
        }

        [Test]
        public void AnEntryMoreThanOneLevelDeeper_GoesJustOneDeeper()
        {
            var roots = DocumentMapNode.Build(new[] { E("A", 0), E("Deep", 3), E("Deeper", 4), E("B", 1) });

            Assert.That(Outline(roots), Is.EqualTo(new[] { "A", "  Deep", "    Deeper", "  B" }));
        }

        [Test]
        public void AFirstEntryBelowTheTop_IsAtTheTop()
        {
            var roots = DocumentMapNode.Build(new[] { E("Starts deep", 2), E("Child", 3) });

            Assert.That(Outline(roots), Is.EqualTo(new[] { "Starts deep", "  Child" }));
        }

        [Test]
        public void NodesKeepTheirEntry()
        {
            var entry = new DocumentMapEntry("X", 0, 3, 12.5f, 40f);
            var root = DocumentMapNode.Build(new[] { entry }).Single();

            Assert.That(root.Entry, Is.SameAs(entry));
        }
    }
}
#endif
