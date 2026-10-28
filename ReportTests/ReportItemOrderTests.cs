#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Report items are taken top down and left to right, and items at the same top and left in
    /// the order the report gives them: that order decides which is drawn over the other, and
    /// which one the items below are placed from. The sort was not stable, so once a container
    /// held more than 16 items those at the same place came out in an arbitrary order.
    /// </summary>
    [TestFixture]
    public class ReportItemOrderTests
    {
        private static string Rdl(int count)
        {
            var items = new StringBuilder();
            for (int i = 0; i < count; i++)
                items.Append($@"<Textbox Name=""T{i:00}""><Top>0pt</Top><Left>0pt</Left><Width>100pt</Width><Height>10pt</Height>
                    <Value>{i:00}</Value></Textbox>");
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>300pt</Width>
  <Body><Height>300pt</Height><ReportItems>{items}</ReportItems></Body>
  <PageHeight>600pt</PageHeight><PageWidth>400pt</PageWidth>
</Report>";
        }

        [TestCase(5)]
        [TestCase(40)]
        public async Task ItemsAtTheSamePlace_PrintInTheReportsOrder(int count)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(Rdl(count)) { SkipDatabaseSchemaValidation = true }.Parse();
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            var printed = pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Select(t => t.Text).ToList();

            Assert.That(printed, Is.EqualTo(Enumerable.Range(0, count).Select(i => i.ToString("00")).ToList()));
        }
    }
}
#endif
