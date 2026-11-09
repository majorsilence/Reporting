using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A legend position's first word is the side the legend sits on (#629). A RightTop legend
    /// (the default) is on the right, aligned to the top: it takes its width from the plot area,
    /// not its height too. A short chart with a tall side legend lost all its plot height and
    /// drew only the legend ("Chart drawing stopped early: Plot area height is less than or equal to 0").
    /// </summary>
    [TestFixture]
    public class ChartLegendMarginTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";

        private static string Report (string type, string position) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2005}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Name""><DataField>Name</DataField></Field><Field Name=""Amount""><DataField>Amount</DataField><TypeName>System.Double</TypeName></Field></Fields>
  </DataSet></DataSets>
  <Body><Height>3in</Height><ReportItems>
    <Chart Name=""Chart1""><DataSetName>Data</DataSetName><Top>0in</Top><Left>0in</Left><Width>8.27cm</Width><Height>5cm</Height>
      <Type>{type}</Type>
      <Legend><Visible>true</Visible><Position>{position}</Position><Style><FontSize>8pt</FontSize></Style></Legend>
      <CategoryGroupings><CategoryGrouping><StaticCategories>
        <StaticMember><Label>Total</Label></StaticMember>
      </StaticCategories></CategoryGrouping></CategoryGroupings>
      <SeriesGroupings><SeriesGrouping><DynamicSeries>
        <Grouping Name=""Names""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
        <Label>=Fields!Name.Value</Label>
      </DynamicSeries></SeriesGrouping></SeriesGroupings>
      <CategoryAxis><Axis><Visible>true</Visible></Axis></CategoryAxis>
      <ValueAxis><Axis><Visible>true</Visible></Axis></ValueAxis>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!Amount.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
    </Chart>
  </ReportItems></Body>
  <Width>6in</Width>
</Report>";

        private static async Task<string[]> RenderErrors (string type, string position)
        {
            RdlEngineConfig.RdlEngineConfigInit ();
            var parser = new RDLParser (Report (type, position)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems?.Cast<string> () ?? Enumerable.Empty<string> ()));
            var table = new DataTable ();
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Amount", typeof (double));
            for (int i = 1; i <= 12; i++)
                table.Rows.Add ("Series " + i, i * 10.0);
            await report.DataSets["Data"].SetData (table);
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            return report.ErrorItems?.Cast<string> ().ToArray () ?? new string[0];
        }

        [TestCase ("Pie", "RightTop")]
        [TestCase ("Column", "RightTop")]
        [TestCase ("Column", "LeftTop")]
        [TestCase ("Column", "RightBottom")]
        public async Task ATallSideLegend_LeavesThePlotArea (string type, string position)
        {
            string[] errors = await RenderErrors (type, position);

            Assert.That (errors.Where (e => e.Contains ("Chart drawing stopped early")), Is.Empty);
        }
    }
}
