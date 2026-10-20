using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A category's Label is what its axis prints. The axis printed the grouping's value
    /// whatever the Label said, so a chart grouped on a date and labelled with its month
    /// printed the whole date-time under every column.
    /// </summary>
    [TestFixture]
    public class ChartCategoryLabelTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string type, string label) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Year""><DataField>Year</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Chart Name=""Chart1""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>4in</Width><Height>2.5in</Height>
      <Type>{type}</Type>
      <CategoryGroupings><CategoryGrouping><DynamicCategories>
        <Grouping Name=""Years""><GroupExpressions><GroupExpression>=Fields!Year.Value</GroupExpression></GroupExpressions></Grouping>
        {label}
      </DynamicCategories></CategoryGrouping></CategoryGroupings>
      <CategoryAxis><Axis><Visible>true</Visible></Axis></CategoryAxis>
      <ValueAxis><Axis><Visible>true</Visible></Axis></ValueAxis>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        // Labels nothing like the values: "WWWWWWWW" in place of "Y1", "Y2".
        private const string LongLabel = "<Label>=\"WWWWWWWW\" &amp; Fields!Year.Value</Label>";

        private static async Task<string> ChartImage (string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (rdl) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems?.Cast<string> () ?? Enumerable.Empty<string> ()));
            var table = new DataTable ();
            table.Columns.Add ("Year", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add ("Y1", 10.0);
            table.Rows.Add ("Y2", 20.0);
            await report.DataSets["Data"].SetData (table);
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            var src = Regex.Match (memory.GetText (), "src=\"data:image/png;base64,([^\"]+)\"");
            Assert.That (src.Success, Is.True, "the chart is inlined as a PNG");
            return src.Groups[1].Value;
        }

        [TestCase ("Column")]
        [TestCase ("Bar")]
        public async Task TheCategoryAxis_PrintsTheLabel (string type)
        {
            string byValue = await ChartImage (Report (type, ""));
            string byLabel = await ChartImage (Report (type, LongLabel));

            Assert.That (byLabel, Is.Not.EqualTo (byValue), "a Label unlike the value changes what the axis prints");
        }

        [Test]
        public async Task ALabelThatIsTheValue_PrintsTheSame ()
        {
            string byValue = await ChartImage (Report ("Column", ""));
            string sameLabel = await ChartImage (Report ("Column", "<Label>=Fields!Year.Value</Label>"));

            Assert.That (sameLabel, Is.EqualTo (byValue));
        }
    }
}
