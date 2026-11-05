using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// A pie with one slice draws that slice as the whole circle. Its arc, swept a full turn,
    /// ended where it began, and the pie was drawn as a single line from its centre.
    /// </summary>
    [TestFixture]
    public class PieWholeSliceTests
    {
        [SetUp]
        public void Prepare() => RdlEngineConfig.RdlEngineConfigInit();

        private const string Rdl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <PageWidth>8.5in</PageWidth><PageHeight>11in</PageHeight>
  <Width>7.5in</Width>
  <DataSources><DataSource Name=""DS""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString></ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS</DataSourceName><CommandText>select</CommandText></Query>
    <Fields>
      <Field Name=""G""><DataField>G</DataField><TypeName>System.String</TypeName></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>System.Double</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>4in</Height><ReportItems>
    <Chart Name=""Pie""><Top>0in</Top><Left>0in</Left><Width>3in</Width><Height>3in</Height>
      <DataSetName>Data</DataSetName><Type>Pie</Type>
      <SeriesGroupings><SeriesGrouping><DynamicSeries>
        <Grouping Name=""ByG""><GroupExpressions><GroupExpression>=Fields!G.Value</GroupExpression></GroupExpressions></Grouping>
        <Label>=Fields!G.Value</Label>
      </DynamicSeries></SeriesGrouping></SeriesGroupings>
      <ChartData><ChartSeries><DataPoints><DataPoint><DataValues><DataValue><Value>=Sum(Fields!V.Value)</Value></DataValue></DataValues></DataPoint></DataPoints></ChartSeries></ChartData>
    </Chart>
  </ReportItems></Body>
</Report>";

        [Test]
        public async Task OneSlice_FillsTheWholePie()
        {
            var report = await new RDLParser(Rdl).Parse();
            var data = new System.Data.DataTable();
            data.Columns.Add("G", typeof(string));
            data.Columns.Add("V", typeof(double));
            data.Rows.Add("A", 12.0);
            await report.DataSets["Data"].SetData(data);
            await report.RunGetData(null);
            var pages = await report.BuildPages();

            var image = pages[0].OfType<PageImage>().Single();
            using var bitmap = SkiaSharp.SKBitmap.Decode(image.GetImageData());
            int inked = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    if (c.Red < 200 || c.Green < 200 || c.Blue < 200)
                        inked++;
                }
            double share = (double)inked / (bitmap.Width * bitmap.Height);
            Assert.That(share, Is.GreaterThan(0.3), $"the pie covers {share:P0} of the chart");
        }
    }
}
