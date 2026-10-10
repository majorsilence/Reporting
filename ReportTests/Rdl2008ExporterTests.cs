using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace ReportTests
{
    /// <summary>
    /// The designer edits RDL 2005, whose List is not part of the RDLC (2008+) schema. The exporter
    /// rewrites Lists as one-cell Tablixes on save, and the normalizer reads that shape back as a
    /// List, so a report survives the round trip. Covers issues #136 (export) and #87 (Tablix).
    /// </summary>
    [TestFixture]
    public class Rdl2008ExporterTests
    {
        private const string Rdl2005 = "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition";
        private const string Rdl2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";

        private static XmlDocument Load (string xml)
        {
            var doc = new XmlDocument ();
            doc.LoadXml (xml);
            return doc;
        }

        private static string ReportWith (string ns, string bodyItems) => $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""{ns}"">
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties>
        <DataProvider>SQLite</DataProvider>
        <ConnectString>Data Source=none.db</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Data"">
      <Query><DataSourceName>DS1</DataSourceName><CommandText>/* Local Query */</CommandText></Query>
      <Fields><Field Name=""Name""><DataField>Name</DataField></Field></Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>3in</Height>
    <ReportItems>{bodyItems}</ReportItems>
  </Body>
  <Width>6in</Width>
</Report>";

        private const string SimpleList = @"
<List Name=""List1"">
  <Top>0.1in</Top><Left>0.2in</Left><Height>1.5in</Height><Width>3in</Width>
  <DataSetName>Data</DataSetName>
  <ReportItems><Textbox Name=""T1""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>1in</Width><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
</List>";

        private static XmlElement First (XmlDocument doc, string localName)
            => doc.GetElementsByTagName (localName).Cast<XmlElement> ().FirstOrDefault ();

        [SetUp]
        public void SetUp () => RdlEngineConfig.RdlEngineConfigInit ();

        [Test]
        public void List_InRdlcNamespace_IsWrittenAsTablix ()
        {
            var doc = Load (ReportWith (Rdl2010, SimpleList));

            var converted = Rdl2008Exporter.ConvertListsToTablix (doc);

            Assert.That (converted, Is.EqualTo (1));
            Assert.That (doc.GetElementsByTagName ("List"), Is.Empty, "<List> is not in the RDLC schema");
            var tablix = First (doc, "Tablix");
            Assert.That (tablix, Is.Not.Null);
            Assert.That (tablix.GetAttribute ("Name"), Is.EqualTo ("List1"));
            Assert.That (tablix.NamespaceURI, Is.EqualTo (Rdl2010), "must stay in the report's namespace");
            Assert.That (tablix["DataSetName"].InnerText, Is.EqualTo ("Data"));
            Assert.That (tablix["Top"].InnerText, Is.EqualTo ("0.1in"));
            Assert.That (tablix["Height"].InnerText, Is.EqualTo ("1.5in"));
            Assert.That (First (doc, "TablixColumn")["Width"].InnerText, Is.EqualTo ("3in"));
            Assert.That (First (doc, "TablixRow")["Height"].InnerText, Is.EqualTo ("1.5in"));
            Assert.That (First (doc, "Group"), Is.Not.Null, "a List over a dataset repeats per record");
            Assert.That (First (doc, "Rectangle").SelectSingleNode (".//*[local-name()='Textbox']"), Is.Not.Null,
                "the list's items move into the cell");
        }

        [Test]
        public void List_InRdl2005_IsLeftAlone ()
        {
            var doc = Load (ReportWith (Rdl2005, SimpleList));

            Assert.That (Rdl2008Exporter.NeedsListConversion (doc), Is.False);
            Assert.That (Rdl2008Exporter.ConvertListsToTablix (doc), Is.EqualTo (0));
            Assert.That (doc.GetElementsByTagName ("List"), Has.Count.EqualTo (1), "List is valid in 2005");
        }

        [Test]
        public void StaticList_HasNoRowGroup ()
        {
            var doc = Load (ReportWith (Rdl2010,
                @"<List Name=""L""><Height>1in</Height><Width>1in</Width><ReportItems /></List>"));

            Rdl2008Exporter.ConvertListsToTablix (doc);

            Assert.That (First (doc, "Group"), Is.Null);
        }

        [Test]
        public void Grouping_SortingAndPageBreak_AreTranslated ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<List Name=""L""><Height>1in</Height><Width>1in</Width><DataSetName>Data</DataSetName>
  <Grouping Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions><PageBreakAtStart>true</PageBreakAtStart></Grouping>
  <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression><Direction>Descending</Direction></SortBy></Sorting>
  <ReportItems />
</List>"));

            Rdl2008Exporter.ConvertListsToTablix (doc);

            var group = First (doc, "Group");
            Assert.That (group.GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (group["GroupExpressions"]["GroupExpression"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (group["PageBreak"]["BreakLocation"].InnerText, Is.EqualTo ("Start"));
            Assert.That (group["PageBreakAtStart"], Is.Null);
            var sort = First (doc, "SortExpression");
            Assert.That (sort["Value"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (sort["Direction"].InnerText, Is.EqualTo ("Descending"));
            Assert.That (doc.GetElementsByTagName ("Sorting"), Is.Empty);
        }

        [Test]
        public void NestedLists_AreAllConverted ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<List Name=""Outer""><Height>2in</Height><Width>3in</Width><DataSetName>Data</DataSetName>
  <ReportItems>
    <List Name=""Inner""><Height>1in</Height><Width>2in</Width><DataSetName>Data</DataSetName><ReportItems /></List>
  </ReportItems>
</List>"));

            var converted = Rdl2008Exporter.ConvertListsToTablix (doc);

            Assert.That (converted, Is.EqualTo (2));
            Assert.That (doc.GetElementsByTagName ("List"), Is.Empty);
            Assert.That (doc.GetElementsByTagName ("Tablix"), Has.Count.EqualTo (2));
        }

        [Test]
        public void RoundTrip_ListSurvivesExportAndImport ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<List Name=""Orders""><Top>0.1in</Top><Left>0.2in</Left><Height>1.5in</Height><Width>3in</Width><DataSetName>Data</DataSetName>
  <Grouping Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
  <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression></SortBy></Sorting>
  <ReportItems><Textbox Name=""T1""><Height>0.25in</Height><Width>1in</Width><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
</List>"));
            Rdl2008Exporter.ConvertListsToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            Assert.That (doc.GetElementsByTagName ("Tablix"), Is.Empty);
            var list = First (doc, "List");
            Assert.That (list, Is.Not.Null, "a one-cell Tablix with a row group reads back as a List");
            Assert.That (list.GetAttribute ("Name"), Is.EqualTo ("Orders"));
            Assert.That (list["DataSetName"].InnerText, Is.EqualTo ("Data"));
            Assert.That (list["Height"].InnerText, Is.EqualTo ("1.5in"));
            Assert.That (list["Width"].InnerText, Is.EqualTo ("3in"));
            Assert.That (list["Grouping"].GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (list["Sorting"]["SortBy"]["SortExpression"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (list["ReportItems"].ChildNodes, Has.Count.EqualTo (1), "the export's Rectangle wrapper is unwrapped");
            Assert.That (list["ReportItems"].FirstChild.LocalName, Is.EqualTo ("Textbox"));
        }

        [Test]
        public void Import_OneCellTablixWithDetailsGroup_BecomesList ()
        {
            // Hand-written, as Report Builder emits a List: no wrapper Rectangle, bare Details group.
            var doc = Load (ReportWith (Rdl2010, @"
<Tablix Name=""Card""><Height>1in</Height><Width>2in</Width><DataSetName>Data</DataSetName>
  <TablixBody>
    <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
    <TablixRows><TablixRow><Height>1in</Height><TablixCells><TablixCell><CellContents>
      <Textbox Name=""T1""><Value>=Fields!Name.Value</Value></Textbox>
    </CellContents></TablixCell></TablixCells></TablixRow></TablixRows>
  </TablixBody>
  <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
  <TablixRowHierarchy><TablixMembers><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers></TablixRowHierarchy>
</Tablix>"));

            Rdl2008Normalizer.Normalize (doc, null);

            var list = First (doc, "List");
            Assert.That (list, Is.Not.Null);
            Assert.That (list["Grouping"], Is.Null, "Details has no group expressions");
            Assert.That (list["ReportItems"].FirstChild.LocalName, Is.EqualTo ("Textbox"));
        }

        [Test]
        public void Import_ThreeRowTablix_StillBecomesTable ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<Tablix Name=""Grid""><DataSetName>Data</DataSetName>
  <TablixBody>
    <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
    <TablixRows>
      <TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents><Textbox Name=""H""><Value>Name</Value></Textbox></CellContents></TablixCell></TablixCells></TablixRow>
      <TablixRow><Height>0.25in</Height><TablixCells><TablixCell><CellContents><Textbox Name=""D""><Value>=Fields!Name.Value</Value></Textbox></CellContents></TablixCell></TablixCells></TablixRow>
    </TablixRows>
  </TablixBody>
  <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
  <TablixRowHierarchy><TablixMembers><TablixMember /><TablixMember><Group Name=""Details"" /></TablixMember></TablixMembers></TablixRowHierarchy>
</Tablix>"));

            Rdl2008Normalizer.Normalize (doc, null);

            Assert.That (First (doc, "Table"), Is.Not.Null);
            Assert.That (First (doc, "List"), Is.Null);
        }

        [Test]
        public async Task ExportedReport_ParsesInTheEngine ()
        {
            var doc = Load (ReportWith (Rdl2010, SimpleList));
            Rdl2008Exporter.ConvertListsToTablix (doc);

            var parser = new RDLParser (doc.OuterXml) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report, Is.Not.Null);
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }
    }
}
