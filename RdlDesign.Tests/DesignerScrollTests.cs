using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // The design surface renders only the report items that intersect the visible region. That region
    // was built from the scroll position twice over (DesignCtl passed a clip already offset by the
    // scroll, and DesignXmlDraw.Draw added the scroll again), so after scrolling down by d the region
    // started at about 2d and the items between d and 2d -- the top of what is on screen -- were not
    // drawn. The horizontal scroll bar's page size was the vertical bar's width instead of the visible
    // width, so it stayed enabled when everything fitted.
    [TestFixture]
    public class DesignerScrollTests
    {
        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>4in</Width>
  <PageWidth>4in</PageWidth>
  <PageHeight>30in</PageHeight>
  <LeftMargin>0in</LeftMargin><RightMargin>0in</RightMargin><TopMargin>0in</TopMargin><BottomMargin>0in</BottomMargin>
  <Body>
    <Height>20in</Height>
    <ReportItems>
      <Textbox Name=""Marker"">
        <Top>10in</Top><Left>1in</Left><Width>2in</Width><Height>1in</Height>
        <Value></Value>
        <Style><BackgroundColor>Red</BackgroundColor></Style>
      </Textbox>
    </ReportItems>
  </Body>
</Report>";

        private static DesignCtl Load(int width, int height)
        {
            var design = new DesignCtl { Width = width, Height = height };
            design.ReportSource = Rdl;
            design.SetScrollControls();
            return design;
        }

        private static async Task<SkiaSharp.SKColor> RenderPixel(DesignCtl design, int x, int y)
        {
            var render = typeof(DesignCtl).GetMethod("Internal_DrawPanelPaintAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            await (Task)render.Invoke(design, null);
            var buffer = (Majorsilence.Forms.Drawing.Bitmap)typeof(DesignCtl)
                .GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(design);
            using var sk = buffer.ToSKBitmap();
            return sk.GetPixel(x, y);
        }

        [Test]
        public async Task ScrolledDown_DrawsItemAtTopOfView()
        {
            var design = Load(600, 400);
            // the marker's top is at 10in; scroll so that it shows near the top of the view
            int markerTop = design.PixelsY(10 * 72.27f);
            int scroll = markerTop - 50;
            Assert.That(design._vScroll.Maximum, Is.GreaterThan(scroll), "the body is tall enough to scroll there");
            design._vScroll.Value = scroll;

            // a point inside the marker: 20px into it, 1.5in from the left
            var c = await RenderPixel(design, design.PixelsX(1.5f * 72.27f), 50 + 20);
            Assert.That(c.Red, Is.GreaterThan(200), "red marker drawn");
            Assert.That(c.Green, Is.LessThan(80), "red marker drawn");
        }

        // The bar's own thumb and track stop at Maximum - LargeChange + 1 (the last page of content
        // starts there). The wheel and PageDown went on to Maximum, past the end of the report into
        // blank space, with the thumb pinned at the bottom while the view kept moving.
        private static int LastPage(ScrollBar sb) => sb.Maximum - sb.LargeChange + 1;

        [Test]
        public void WheelDown_StopsAtLastPage()
        {
            var design = Load(600, 400);
            var wheel = typeof(DesignCtl).GetMethod("DrawPanelMouseWheel", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 500; i++)
                wheel.Invoke(design, new object[] { design._DrawPanel, new MouseEventArgs(MouseButtons.None, 0, 10, 10, -120) });
            Assert.That(design._vScroll.Value, Is.EqualTo(LastPage(design._vScroll)));
        }

        [Test]
        public void PageDown_StopsAtLastPage()
        {
            var design = Load(600, 400);
            var key = typeof(DesignCtl).GetMethod("DrawPanelKeyDown", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 50; i++)
                key.Invoke(design, new object[] { design._DrawPanel, new KeyEventArgs(Keys.PageDown) });
            Assert.That(design._vScroll.Value, Is.EqualTo(LastPage(design._vScroll)));
        }

        [Test]
        public void ContentNarrowerThanView_NoHorizontalScroll()
        {
            var design = Load(800, 400);   // the page is 4in (about 384px at 96 dpi) wide
            Assert.That(design._hScroll.Enabled, Is.False);
        }
    }
}
