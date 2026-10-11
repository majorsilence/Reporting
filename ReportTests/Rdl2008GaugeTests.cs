using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The engine has no gauge drawing, so a GaugePanel is approximated with items it can render:
    /// indicators as coloured boxes and numbers, gauges as a bar on their scale (issue #407).
    /// </summary>
    [TestFixture]
    public class Rdl2008GaugeTests
    {
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

        private const string Panel = @"
<GaugePanel Name=""Kpi""><DataSetName>Data</DataSetName><Top>0.5in</Top><Left>0in</Left><Height>2in</Height><Width>4in</Width>
  <StateIndicators><StateIndicator Name=""Status"">
    <Top>0</Top><Left>0</Left><Height>20</Height><Width>25</Width>
    <GaugeInputValue><Value>=Sum(Fields!Amount.Value)</Value></GaugeInputValue>
    <IndicatorStates>
      <IndicatorState><Color>Red</Color><StartValue>0</StartValue><EndValue>10</EndValue></IndicatorState>
      <IndicatorState><Color>Green</Color><StartValue>10</StartValue><EndValue>100</EndValue></IndicatorState>
    </IndicatorStates>
  </StateIndicator></StateIndicators>
  <NumericIndicators><NumericIndicator Name=""Total"">
    <Top>0</Top><Left>30</Left><Height>20</Height><Width>30</Width><DecimalDigits>1</DecimalDigits>
    <GaugeInputValue><Value>=Sum(Fields!Amount.Value)</Value></GaugeInputValue>
  </NumericIndicator></NumericIndicators>
  <RadialGauges><RadialGauge Name=""Dial"">
    <Top>25</Top><Left>0</Left><Height>75</Height><Width>50</Width>
    <GaugeScales><RadialScale Name=""Scale1"">
      <MinimumValue><Value>0</Value></MinimumValue><MaximumValue><Value>50</Value></MaximumValue><Interval>10</Interval><StartAngle>30</StartAngle><SweepAngle>300</SweepAngle>
      <ScaleRanges>
        <ScaleRange><StartValue><Value>0</Value></StartValue><EndValue><Value>30</Value></EndValue><Style><BackgroundColor>Green</BackgroundColor></Style></ScaleRange>
        <ScaleRange><StartValue><Value>30</Value></StartValue><EndValue><Value>=50</Value></EndValue><Style><BackgroundColor>Red</BackgroundColor></Style></ScaleRange>
      </ScaleRanges>
      <GaugePointers><RadialPointer Name=""Needle""><Style><BackgroundColor>Blue</BackgroundColor></Style><GaugeInputValue><Value>=Sum(Fields!Amount.Value)</Value></GaugeInputValue></RadialPointer></GaugePointers>
    </RadialScale></GaugeScales>
  </RadialGauge></RadialGauges>
  <LinearGauges><LinearGauge Name=""Bar"">
    <Top>25</Top><Left>55</Left><Height>75</Height><Width>45</Width>
    <GaugeScales><LinearScale Name=""Scale2"">
      <MinimumValue><Value>0</Value></MinimumValue><MaximumValue><Value>100</Value></MaximumValue>
      <GaugePointers><LinearPointer Name=""Marker""><GaugeInputValue><Value>=Sum(Fields!Amount.Value)</Value></GaugeInputValue></LinearPointer></GaugePointers>
    </LinearScale></GaugeScales>
  </LinearGauge></LinearGauges>
</GaugePanel>";

        private static string ReportWith (string item) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{Rdl2010}"">
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>SQLite</DataProvider><ConnectString>Data Source=none.db</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
    <Fields><Field Name=""Amount""><DataField>Amount</DataField></Field></Fields>
  </DataSet></DataSets>
  <ReportSections><ReportSection>
    <Body><Height>3in</Height><ReportItems>
      <Textbox Name=""Before""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Paragraphs><Paragraph><TextRuns><TextRun><Value>Before gauges</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>
      {item}
    </ReportItems></Body>
    <Width>5in</Width>
  </ReportSection></ReportSections>
</Report>";

        private static XmlDocument Load (string xml)
        {
            var doc = new XmlDocument ();
            doc.LoadXml (xml);
            return doc;
        }

        private static XmlElement Find (XmlNode scope, string name)
            => scope.SelectNodes (".//*[local-name()='" + name + "']").Cast<XmlElement> ().FirstOrDefault ();

        private static DataTable Data ()
        {
            var table = new DataTable ();
            table.Columns.Add ("Amount", typeof (double));
            table.Rows.Add (12.0);
            table.Rows.Add (10.0);
            return table;
        }

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        [Test]
        public void GaugePanel_BecomesARectangleOfTheSameSize ()
        {
            var doc = Load (ReportWith (Panel));

            Rdl2008Normalizer.Normalize (doc, null);

            Assert.That (doc.GetElementsByTagName ("GaugePanel"), Is.Empty);
            var rectangle = doc.GetElementsByTagName ("Rectangle").Cast<XmlElement> ().First (r => r.GetAttribute ("Name") == "Kpi");
            Assert.That (rectangle["Top", Rdl2010].InnerText, Is.EqualTo ("0.5in"));
            Assert.That (rectangle["Width", Rdl2010].InnerText, Is.EqualTo ("4in"));
            // the two indicators and the two drawn gauges share a one-instance list for the dataset
            Assert.That (rectangle["ReportItems", Rdl2010].ChildNodes, Has.Count.EqualTo (1));
            Assert.That (rectangle["ReportItems", Rdl2010]["List", Rdl2010]["ReportItems", Rdl2010].ChildNodes, Has.Count.EqualTo (4));
        }

        [Test]
        public void StateIndicator_IsAColourExpressionOverItsStates ()
        {
            var doc = Load (ReportWith (Panel));
            Rdl2008Normalizer.Normalize (doc, null);

            var box = doc.GetElementsByTagName ("Textbox").Cast<XmlElement> ().First (r => r.GetAttribute ("Name") == "Kpi_State1");
            Assert.That (box["Width", Rdl2010].InnerText, Is.EqualTo ("1in"), "25% of the 4in panel");
            Assert.That (box["Height", Rdl2010].InnerText, Is.EqualTo ("0.4in"), "20% of the 2in panel");
            var color = box["Style", Rdl2010]["BackgroundColor", Rdl2010].InnerText;
            Assert.That (color, Does.StartWith ("=Iif("));
            Assert.That (color, Does.Contain ("\"Red\"").And.Contain ("\"Green\""));
            Assert.That (color, Does.Contain ("Sum(Fields!Amount.Value)"));
        }

        [Test]
        public void PercentageIndicator_ScalesTheValueToTheRange ()
        {
            // Match across the line break whatever its form: the source file is checked out with CRLF on Windows.
            var xml = Regex.Replace (Panel, @"</GaugeInputValue>\s*<IndicatorStates>",
                "</GaugeInputValue><TransformationType>Percentage</TransformationType><MinimumValue><Value>0</Value></MinimumValue><MaximumValue><Value>200</Value></MaximumValue><IndicatorStates>");
            Assert.That (xml, Does.Contain ("Percentage"), "the fixture must actually be changed");
            var doc = Load (ReportWith (xml));
            Rdl2008Normalizer.Normalize (doc, null);

            var color = doc.GetElementsByTagName ("Textbox").Cast<XmlElement> ().First (r => r.GetAttribute ("Name") == "Kpi_State1")["Style", Rdl2010]["BackgroundColor", Rdl2010].InnerText;
            Assert.That (color, Does.Contain ("/ ((200) - (0)) * 100"));
        }

        [Test]
        public void NumericIndicator_IsAFormattedTextbox ()
        {
            var doc = Load (ReportWith (Panel));
            Rdl2008Normalizer.Normalize (doc, null);

            var box = doc.GetElementsByTagName ("Textbox").Cast<XmlElement> ().First (t => t.GetAttribute ("Name") == "Kpi_Number2");
            Assert.That (box["Value", Rdl2010].InnerText, Is.EqualTo ("=Sum(Fields!Amount.Value)"));
            Assert.That (box["Style", Rdl2010]["Format", Rdl2010].InnerText, Is.EqualTo ("N1"));
        }

        private static XmlElement Drawn (XmlDocument doc, string name)
            => doc.GetElementsByTagName ("CustomReportItem").Cast<XmlElement> ().First (c => c.GetAttribute ("Name") == name);

        private static string Property (XmlElement item, string name)
            => item.SelectNodes (".//*[local-name()='CustomProperty']").Cast<XmlElement> ()
                .FirstOrDefault (p => p["Name", Rdl2010].InnerText == name)?["Value", Rdl2010].InnerText;

        [Test]
        public void Gauges_BecomeDrawnItemsWithTheirScale ()
        {
            var doc = Load (ReportWith (Panel));
            Rdl2008Normalizer.Normalize (doc, null);

            Assert.That (doc.GetElementsByTagName ("Chart"), Is.Empty, "the drawing module is present, so no fallback chart");
            var radial = Drawn (doc, "Kpi_Radial3");
            Assert.That (radial["Type", Rdl2010].InnerText, Is.EqualTo ("Gauge Radial"));
            Assert.That (radial["Width", Rdl2010].InnerText, Is.EqualTo ("2in"), "50% of the 4in panel");
            Assert.That (Property (radial, "Value"), Is.EqualTo ("=Sum(Fields!Amount.Value)"));
            Assert.That (Property (radial, "Min"), Is.EqualTo ("=0"));
            Assert.That (Property (radial, "Max"), Is.EqualTo ("=50"));
            Assert.That (Property (radial, "Interval"), Is.EqualTo ("10"));
            Assert.That (Property (radial, "StartAngle"), Is.EqualTo ("30"));
            Assert.That (Property (radial, "SweepAngle"), Is.EqualTo ("300"));
            Assert.That (Property (radial, "PointerColor"), Is.EqualTo ("Blue"));
            var ranges = Property (radial, "Ranges");
            Assert.That (ranges, Does.StartWith ("=CStr((0)) & \":\" & CStr((30)) & \":Green\""));
            Assert.That (ranges, Does.Contain ("CStr((50)) & \":Red\""), "an expression end value is kept as an expression");
            var linear = Drawn (doc, "Kpi_Linear4");
            Assert.That (linear["Type", Rdl2010].InnerText, Is.EqualTo ("Gauge Linear"));
            Assert.That (Property (linear, "Max"), Is.EqualTo ("=100"));
        }

        [Test]
        public void Gauges_FallBackToABarWhenTheDrawingModuleIsMissing ()
        {
            Rdl2008GaugeConverter.DrawnGaugesEnabled = false;
            try {
                var doc = Load (ReportWith (Panel));
                Rdl2008Normalizer.Normalize (doc, null);

                var charts = doc.GetElementsByTagName ("Chart").Cast<XmlElement> ().ToList ();
                Assert.That (charts, Has.Count.EqualTo (2));
                Assert.That (doc.GetElementsByTagName ("CustomReportItem"), Is.Empty);
                var radial = charts.First (c => c.GetAttribute ("Name") == "Kpi_Radial3");
                Assert.That (radial["Type", Rdl2010].InnerText, Is.EqualTo ("Bar"));
                Assert.That (radial["ValueAxis", Rdl2010]["Axis", Rdl2010]["Max", Rdl2010].InnerText, Is.EqualTo ("=50"));
            } finally {
                Rdl2008GaugeConverter.DrawnGaugesEnabled = true;
            }
        }

        [Test]
        public void EmptyGaugePanel_StillGetsThePlaceholder ()
        {
            var doc = Load (ReportWith (@"<GaugePanel Name=""Empty""><Top>0.5in</Top><Left>0in</Left><Height>1in</Height><Width>1in</Width></GaugePanel>"));
            var log = new ReportLog ();

            Rdl2008Normalizer.Normalize (doc, log);

            var placeholder = doc.GetElementsByTagName ("Rectangle").Cast<XmlElement> ().Single (r => r.GetAttribute ("Name") == "Empty");
            Assert.That (placeholder.ChildNodes.Cast<XmlNode> ().Any (n => n.LocalName == "ReportItems"), Is.False);
            Assert.That (log.ErrorItems.Cast<string> ().Single (), Does.Contain ("not supported"));
        }

        [Test]
        public void DesignerKeepsTheOriginalPanel ()
        {
            var doc = Load (ReportWith (Panel));

            Rdl2008Exporter.NormalizeForEditing (doc);

            Assert.That (doc.GetElementsByTagName ("GaugePanel"), Has.Count.EqualTo (1), "saving must write the gauge panel back");
        }

        [Test]
        public async Task GaugePanel_RendersItsPartsWithData ()
        {
            var parser = new RDLParser (ReportWith (Panel)) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            var messages = report.ErrorItems == null ? new string[0] : report.ErrorItems.Cast<string> ().ToArray ();
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4), string.Join (" | ", messages));
            Assert.That (messages.Count (m => m.Contains ("GaugePanel")), Is.EqualTo (1));
            Assert.That (messages.Single (m => m.Contains ("GaugePanel")), Does.Contain ("approximated"));

            await report.DataSets["Data"].SetData (Data ());
            await report.RunGetData (null);
            using var memory = new MemoryStreamGen ();
            await report.RunRender (memory, OutputPresentationType.HTML);
            var html = memory.GetText ();

            Assert.That (html, Does.Contain ("Before gauges"));
            Assert.That (Regex.Matches (html, "<img").Count, Is.EqualTo (2), "one bar per gauge");
            Assert.That (html, Does.Contain ("22.0"), "the numeric indicator shows the sum with one decimal");
            // Match the style, not the word: the chart images are inlined as base64, which can contain "red".
            Assert.That (html, Does.Contain ("background-color:Green"), "a total of 22 is in the Green state");
            Assert.That (html, Does.Not.Contain ("background-color:Red"));
        }
    }
}
