using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// VB date expressions SSRS reports use (#426): Now and Today without parentheses, spelt
    /// DateTime.Now / Date.Today too, the members called on them (Today.AddDays(-7), Now.Year),
    /// Weekday with a FirstDayOfWeek, and the FirstDayOfWeek / FirstWeekOfYear constants.
    /// "Today" used to print as the word, and the rest failed to parse.
    /// </summary>
    [TestFixture]
    public class VbDateExpressionTests
    {
        private static async Task<string> Text(string expr)
        {
            string rdl = $@"<?xml version=""1.0""?><Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""><Body><Height>1in</Height><ReportItems>
<Textbox Name=""T""><Value>{expr}</Value><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>4in</Width></Textbox></ReportItems></Body><Width>5in</Width></Report>";
            RdlEngineConfig.RdlEngineConfigInit();
            using var report = await new RDLParser(rdl).Parse();
            Assert.That(report.ErrorMaxSeverity, Is.LessThanOrEqualTo(4), string.Join(" | ", report.ErrorItems?.Cast<object>() ?? Enumerable.Empty<object>()));
            await report.RunGetData(null);
            var pages = await report.BuildPages();
            return pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>().Single().Text;
        }

        [TestCase("=Format(Today, \"yyyy-MM-dd\")")]
        [TestCase("=Format(DateTime.Today, \"yyyy-MM-dd\")")]
        [TestCase("=Format(Date.Today, \"yyyy-MM-dd\")")]
        [TestCase("=Format(Now, \"yyyy-MM-dd\")")]
        [TestCase("=Format(DateTime.Now, \"yyyy-MM-dd\")")]
        [TestCase("=Format(System.DateTime.Now, \"yyyy-MM-dd\")")]
        public async Task NowAndToday_WithoutParentheses_AreTheDate(string expr)
        {
            var before = DateTime.Today;
            string text = await Text(expr);
            Assert.That(text, Is.EqualTo(before.ToString("yyyy-MM-dd")).Or.EqualTo(DateTime.Today.ToString("yyyy-MM-dd")));
        }

        [Test]
        public async Task MembersOfTodayAndNow()
        {
            var today = DateTime.Today;
            Assert.That(await Text("=Format(Today.AddDays(-7), \"yyyy-MM-dd\")"), Is.EqualTo(today.AddDays(-7).ToString("yyyy-MM-dd")));
            Assert.That(await Text("=Format(DateTime.Today.AddMonths(1), \"yyyy-MM\")"), Is.EqualTo(today.AddMonths(1).ToString("yyyy-MM")));
            Assert.That(await Text("=Today.Year"), Is.EqualTo(today.Year.ToString()));
            Assert.That(await Text("=Today.DayOfYear"), Is.EqualTo(today.DayOfYear.ToString()));
            Assert.That(await Text("=Now.ToString(\"yyyy\")"), Is.EqualTo(DateTime.Now.ToString("yyyy")));
        }

        [TestCase("FirstDayOfWeek.Sunday", 4)]      // 2026-11-11 is a Wednesday
        [TestCase("FirstDayOfWeek.Monday", 3)]
        [TestCase("FirstDayOfWeek.Wednesday", 1)]
        [TestCase("FirstDayOfWeek.Thursday", 7)]
        public async Task Weekday_CountsFromTheFirstDayOfWeek(string firstDay, int expected)
        {
            Assert.That(await Text($"=Weekday(DateSerial(2026, 11, 11), {firstDay})"), Is.EqualTo(expected.ToString()));
        }

        [Test]
        public async Task TheStartOfTheWeek_AsTheHeatmapCalendarReportWorksItOut()
        {
            // From ReportTests/Reports/RealWorld/.../Heatmap Calendar.rdl.
            var today = DateTime.Today;
            var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
            Assert.That(await Text("=Format(Today.AddDays(1-WeekDay(Today,FirstDayOfWeek.Monday)), \"yyyy-MM-dd\")"), Is.EqualTo(monday.ToString("yyyy-MM-dd")));
        }

        [Test]
        public async Task FirstWeekOfYear_Constants()
        {
            Assert.That(await Text("=FirstWeekOfYear.FirstFourDays"), Is.EqualTo("2"));
        }
    }
}
