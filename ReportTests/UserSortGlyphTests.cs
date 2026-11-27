#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The geometry the interactive viewers share for a sort header's glyph (issue #636): where a
    /// click sorts, and the triangles drawn for each direction.
    /// </summary>
    [TestFixture]
    public class UserSortGlyphTests
    {
        // A 100 x 20 pt sort header at (50, 30) with 2pt padding and a 10pt font.
        private static PageText Header(string toggleKey = null, string hyperlink = null)
        {
            var si = new StyleInfo { PaddingLeft = 2, PaddingRight = 2, PaddingTop = 2, PaddingBottom = 2, FontSize = 10 };
            return new PageText("Name")
            {
                X = 50, Y = 30, W = 100, H = 20, SI = si,
                UserSortKey = "SortName", ToggleKey = toggleKey, HyperLink = hyperlink,
            };
        }

        [Test]
        public void The_glyph_sits_in_the_right_padding_level_with_the_first_line()
        {
            var pt = Header();
            pt.GetUserSortGlyphBounds(out float x, out float y, out float size);

            Assert.That(size, Is.EqualTo(PageItem.UserSortGlyphSize));
            Assert.That(x, Is.EqualTo(50 + 100 - 2 - PageItem.UserSortGlyphSpace + 1.5f));
            Assert.That(y, Is.EqualTo(30 + 2 + (12 - 7) / 2f));
        }

        [Test]
        public void The_glyph_hit_test_is_its_square_widened_to_the_glyph_space()
        {
            var pt = Header();
            pt.GetUserSortGlyphBounds(out float x, out float y, out float size);

            Assert.That(pt.UserSortGlyphContains(x + size / 2, y + size / 2), Is.True, "the middle");
            Assert.That(pt.UserSortGlyphContains(x - 1, y - 1), Is.True, "just outside the square, inside the space");
            Assert.That(pt.UserSortGlyphContains(x + size + 1.5f, y + size + 1.5f), Is.True, "the far corner of the space");
            Assert.That(pt.UserSortGlyphContains(x - 2, y + size / 2), Is.False, "left of the space: the text");
            Assert.That(pt.UserSortGlyphContains(x + size / 2, y + size + 2), Is.False, "below the first line");
            Assert.That(pt.UserSortGlyphContains(60, 35), Is.False, "the start of the text");

            pt.UserSortKey = null;
            Assert.That(pt.UserSortGlyphContains(x + size / 2, y + size / 2), Is.False, "not a sort header");
        }

        [Test]
        public void A_click_anywhere_on_a_plain_sort_header_sorts()
        {
            var pt = Header();
            Assert.That(pt.IsUserSortClick(60, 35), Is.True);
            pt.GetUserSortGlyphBounds(out float x, out float y, out float size);
            Assert.That(pt.IsUserSortClick(x + size / 2, y + size / 2), Is.True);

            pt.UserSortKey = null;
            Assert.That(pt.IsUserSortClick(60, 35), Is.False, "not a sort header");
        }

        [TestCase("toggle", null)]
        [TestCase(null, "https://example.com")]
        public void On_a_header_that_also_toggles_or_links_only_the_glyph_sorts(string toggleKey, string hyperlink)
        {
            var pt = Header(toggleKey, hyperlink);
            pt.GetUserSortGlyphBounds(out float x, out float y, out float size);

            Assert.That(pt.IsUserSortClick(x + size / 2, y + size / 2), Is.True, "the glyph sorts");
            Assert.That(pt.IsUserSortClick(60, 35), Is.False, "the rest toggles or follows the link");
        }

        [Test]
        public void Ascending_is_one_triangle_pointing_up()
        {
            var t = PageItem.GetUserSortGlyphTriangles(UserSortDirection.Ascending, 10, 20, 7);
            Assert.That(t, Has.Length.EqualTo(1));
            Assert.That(t[0], Is.EqualTo(new float[] { 10, 27, 13.5f, 20, 17, 27 }), "base at the bottom, apex at the top middle");
        }

        [Test]
        public void Descending_is_one_triangle_pointing_down()
        {
            var t = PageItem.GetUserSortGlyphTriangles(UserSortDirection.Descending, 10, 20, 7);
            Assert.That(t, Has.Length.EqualTo(1));
            Assert.That(t[0], Is.EqualTo(new float[] { 10, 20, 17, 20, 13.5f, 27 }), "base at the top, apex at the bottom middle");
        }

        [Test]
        public void Unsorted_is_a_small_up_triangle_over_a_small_down_one()
        {
            var t = PageItem.GetUserSortGlyphTriangles(UserSortDirection.None, 10, 20, 7);
            Assert.That(t, Has.Length.EqualTo(2));
            // up: apex at the top, base at y = 23 (half of 7 less a 1pt gap)
            Assert.That(t[0], Is.EqualTo(new float[] { 10, 23, 13.5f, 20, 17, 23 }));
            // down: base at y = 24, apex at the bottom
            Assert.That(t[1], Is.EqualTo(new float[] { 10, 24, 17, 24, 13.5f, 27 }));
            Assert.That(t[0][1], Is.LessThan(t[1][1]), "the up one is above the down one");
        }
    }
}
#endif
