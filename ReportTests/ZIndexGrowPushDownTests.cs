using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using ReportTests.Utils;

namespace ReportTests
{
    // Issue #520: a report item below a CanGrow item was pushed down only when the
    // grower's ZIndex was lower than or equal to the follower's. ZIndex is drawing
    // (paint) order only and must not affect vertical layout, so both orderings below
    // must push the follower down by the same amount.
    [TestFixture]
    public class ZIndexGrowPushDownTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private static async Task<double> FollowerTop(string name, int growerZIndex, int followerZIndex)
        {
            var outputFolder = GeneralUtils.OutputTestsFolder();
            Directory.CreateDirectory(outputFolder.LocalPath);

            string rdlPath = Path.Combine(outputFolder.LocalPath, name + ".rdl");
            File.WriteAllText(rdlPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <LeftMargin>0.5in</LeftMargin><RightMargin>0.5in</RightMargin>
  <TopMargin>0.5in</TopMargin><BottomMargin>0.5in</BottomMargin>
  <Width>7.5in</Width>
  <Body><Height>3in</Height><ReportItems>
    <Textbox Name=""Grower"">
      <Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>0.3in</Height>
      <CanGrow>true</CanGrow>
      <ZIndex>{growerZIndex}</ZIndex>
      <Value>Line1&#10;Line2&#10;Line3&#10;Line4&#10;Line5&#10;Line6</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize></Style>
    </Textbox>
    <Textbox Name=""Follower"">
      <Top>0.5in</Top><Left>0in</Left><Width>3in</Width><Height>0.3in</Height>
      <ZIndex>{followerZIndex}</ZIndex>
      <Value>Follower</Value>
      <Style><FontFamily>Arial</FontFamily><FontSize>10pt</FontSize></Style>
    </Textbox>
  </ReportItems></Body>
</Report>");

            Report report = await RdlUtils.GetReport(new System.Uri(rdlPath));
            report.Folder = outputFolder.LocalPath;
            await report.RunGetData(null);
            Pages pgs = await report.BuildPages();

            PageItem grower = null, follower = null;
            foreach (Page p in pgs)
            {
                foreach (PageItem pi in p)
                {
                    if (pi is PageText pt && pt.Text != null)
                    {
                        if (pt.Text.StartsWith("Line1")) grower = pi;
                        if (pt.Text.StartsWith("Follower")) follower = pi;
                    }
                }
            }

            Assert.That(grower, Is.Not.Null, "Grower textbox not found in output.");
            Assert.That(follower, Is.Not.Null, "Follower textbox not found in output.");

            // The grower must actually have grown past its own design-time bottom (0.3in ==
            // 21.6pt) for this test to mean anything.
            Assert.That(grower.Y + grower.H, Is.GreaterThan(21.6 * 1.5),
                $"Grower did not grow as expected (bottom={grower.Y + grower.H}).");

            return follower.Y;
        }

        [Test]
        public async Task GrowerZIndexLowerThanFollower_PushesFollowerDown()
        {
            double followerTop = await FollowerTop("ZIndexGrow_lower", growerZIndex: 0, followerZIndex: 1);
            // Follower's design Top is 0.5in == 36pt; it must have moved past the grower's bottom.
            Assert.That(followerTop, Is.GreaterThan(36.0));
        }

        [Test]
        public async Task GrowerZIndexHigherThanFollower_StillPushesFollowerDown()
        {
            double lowerCaseTop = await FollowerTop("ZIndexGrow_lower2", growerZIndex: 0, followerZIndex: 1);
            double higherCaseTop = await FollowerTop("ZIndexGrow_higher", growerZIndex: 1, followerZIndex: 0);

            // Same geometry, only the ZIndex values differ: the push-down must not depend on it.
            Assert.That(higherCaseTop, Is.EqualTo(lowerCaseTop).Within(0.1));
        }
    }
}
