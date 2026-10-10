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

            var converted = Rdl2008Exporter.ConvertToTablix (doc);

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

            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.False);
            Assert.That (Rdl2008Exporter.ConvertToTablix (doc), Is.EqualTo (0));
            Assert.That (doc.GetElementsByTagName ("List"), Has.Count.EqualTo (1), "List is valid in 2005");
        }

        [Test]
        public void StaticList_HasNoRowGroup ()
        {
            var doc = Load (ReportWith (Rdl2010,
                @"<List Name=""L""><Height>1in</Height><Width>1in</Width><ReportItems /></List>"));

            Rdl2008Exporter.ConvertToTablix (doc);

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

            Rdl2008Exporter.ConvertToTablix (doc);

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

            var converted = Rdl2008Exporter.ConvertToTablix (doc);

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
            Rdl2008Exporter.ConvertToTablix (doc);

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

        private static string TablixCellXml (string text, string name)
            => $@"<TablixCell><CellContents><Textbox Name=""{name}""><Value>{text}</Value></Textbox></CellContents></TablixCell>";

        private static string TablixRowXml (string name, string text)
            => $"<TablixRow><Height>0.25in</Height><TablixCells>{TablixCellXml (text, name)}</TablixCells></TablixRow>";

        [Test]
        public void Import_GroupedTablix_BecomesTableWithTableGroups ()
        {
            // Header, group header, detail, group footer, footer: the shape of a grouped table.
            var doc = Load (ReportWith (Rdl2010, $@"
<Tablix Name=""Grouped""><DataSetName>Data</DataSetName>
  <TablixBody>
    <TablixColumns><TablixColumn><Width>2in</Width></TablixColumn></TablixColumns>
    <TablixRows>{TablixRowXml ("H", "Title")}{TablixRowXml ("GH", "=Fields!Name.Value")}{TablixRowXml ("D", "detail")}{TablixRowXml ("GF", "subtotal")}{TablixRowXml ("F", "total")}</TablixRows>
  </TablixBody>
  <TablixColumnHierarchy><TablixMembers><TablixMember /></TablixMembers></TablixColumnHierarchy>
  <TablixRowHierarchy><TablixMembers>
    <TablixMember />
    <TablixMember>
      <Group Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>
      <SortExpressions><SortExpression><Value>=Fields!Name.Value</Value></SortExpression></SortExpressions>
      <TablixMembers>
        <TablixMember />
        <TablixMember><Group Name=""Details"" /></TablixMember>
        <TablixMember />
      </TablixMembers>
    </TablixMember>
    <TablixMember />
  </TablixMembers></TablixRowHierarchy>
</Tablix>"));

            Rdl2008Normalizer.Normalize (doc, null);

            var table = First (doc, "Table");
            Assert.That (table, Is.Not.Null);
            string Text (XmlNode section) => string.Concat (section.SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ().Select (n => n.InnerText));
            Assert.That (Text (table["Header"]), Is.EqualTo ("Title"));
            Assert.That (Text (table["Details"]), Is.EqualTo ("detail"));
            Assert.That (Text (table["Footer"]), Is.EqualTo ("total"));
            var group = table["TableGroups"]["TableGroup"];
            Assert.That (group["Grouping"].GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (group["Grouping"]["GroupExpressions"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (group["Sorting"]["SortBy"]["SortExpression"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (Text (group["Header"]), Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (Text (group["Footer"]), Is.EqualTo ("subtotal"));
        }

        private const string GroupedTable = @"
<Table Name=""Sales""><DataSetName>Data</DataSetName><Height>1.25in</Height><Width>2in</Width>
  <TableColumns><TableColumn><Width>2in</Width></TableColumn></TableColumns>
  <Header><RepeatOnNewPage>true</RepeatOnNewPage><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""H""><Value>Title</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
  <TableGroups><TableGroup>
    <Grouping Name=""ByName""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
    <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression></SortBy></Sorting>
    <Header><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""GH""><Value>group head</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
    <Footer><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""GF""><Value>subtotal</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Footer>
  </TableGroup></TableGroups>
  <Details><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""D""><Value>detail</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Details>
  <Footer><TableRows><TableRow><Height>0.25in</Height><TableCells><TableCell><ReportItems><Textbox Name=""F""><Value>total</Value></Textbox></ReportItems></TableCell></TableCells></TableRow></TableRows></Footer>
</Table>";

        [Test]
        public void Table_InRdlcNamespace_IsWrittenAsTablixWithRowHierarchy ()
        {
            var doc = Load (ReportWith (Rdl2010, GroupedTable));

            Assert.That (Rdl2008Exporter.ConvertToTablix (doc), Is.EqualTo (1));

            Assert.That (doc.GetElementsByTagName ("Table"), Is.Empty, "<Table> is not in the RDLC schema");
            var tablix = First (doc, "Tablix");
            Assert.That (tablix.GetAttribute ("Name"), Is.EqualTo ("Sales"));
            var texts = tablix["TablixBody"]["TablixRows"].SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ()
                .Select (n => n.InnerText).ToArray ();
            Assert.That (texts, Is.EqualTo (new[] { "Title", "group head", "detail", "subtotal", "total" }),
                "body rows must follow the order the hierarchy visits them");
            Assert.That (doc.GetElementsByTagName ("Group"), Has.Count.EqualTo (2), "ByName plus the details group");
            Assert.That (First (doc, "RepeatOnNewPage").InnerText, Is.EqualTo ("true"));
        }

        [Test]
        public void Table_ColSpan_GetsPlaceholderCells ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<Table Name=""T""><TableColumns><TableColumn><Width>1in</Width></TableColumn><TableColumn><Width>1in</Width></TableColumn></TableColumns>
  <Details><TableRows><TableRow><Height>0.25in</Height><TableCells>
    <TableCell><ColSpan>2</ColSpan><ReportItems><Textbox Name=""X""><Value>wide</Value></Textbox></ReportItems></TableCell>
  </TableCells></TableRow></TableRows></Details>
</Table>"));

            Rdl2008Exporter.ConvertToTablix (doc);

            Assert.That (doc.GetElementsByTagName ("TablixCell"), Has.Count.EqualTo (2));
            Assert.That (First (doc, "CellContents")["ColSpan"].InnerText, Is.EqualTo ("2"));
        }

        [Test]
        public void RoundTrip_GroupedTableSurvivesExportAndImport ()
        {
            var doc = Load (ReportWith (Rdl2010, GroupedTable));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var table = First (doc, "Table");
            Assert.That (table, Is.Not.Null);
            string Text (XmlNode n) => string.Concat (n.SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ().Select (v => v.InnerText));
            Assert.That (Text (table["Header"]), Is.EqualTo ("Title"));
            Assert.That (table["Header"]["RepeatOnNewPage"].InnerText, Is.EqualTo ("true"));
            Assert.That (Text (table["Details"]), Is.EqualTo ("detail"));
            Assert.That (Text (table["Footer"]), Is.EqualTo ("total"));
            var group = table["TableGroups"]["TableGroup"];
            Assert.That (group["Grouping"].GetAttribute ("Name"), Is.EqualTo ("ByName"));
            Assert.That (Text (group["Header"]), Is.EqualTo ("group head"));
            Assert.That (Text (group["Footer"]), Is.EqualTo ("subtotal"));
            Assert.That (group["Sorting"]["SortBy"]["SortExpression"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
        }

        private const string SalesMatrix = @"
<Matrix Name=""Pivot""><DataSetName>Data</DataSetName><Height>0.5in</Height><Width>2in</Width>
  <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
  <ColumnGroupings><ColumnGrouping><Height>0.25in</Height><DynamicColumns>
    <Grouping Name=""ByYear""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
    <Sorting><SortBy><SortExpression>=Fields!Name.Value</SortExpression></SortBy></Sorting>
    <ReportItems><Textbox Name=""YearHead""><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
  </DynamicColumns></ColumnGrouping></ColumnGroupings>
  <RowGroupings><RowGrouping><Width>1in</Width><DynamicRows>
    <Grouping Name=""ByRegion""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping>
    <ReportItems><Textbox Name=""RegionHead""><Value>=Fields!Name.Value</Value></Textbox></ReportItems>
  </DynamicRows></RowGrouping></RowGroupings>
  <MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells><MatrixCell><ReportItems><Textbox Name=""Cell""><Value>=Sum(Fields!Name.Value)</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
  <MatrixColumns><MatrixColumn><Width>1in</Width></MatrixColumn></MatrixColumns>
</Matrix>";

        [Test]
        public void Matrix_InRdlcNamespace_IsWrittenAsTablix ()
        {
            var doc = Load (ReportWith (Rdl2010, SalesMatrix));

            Assert.That (Rdl2008Exporter.ConvertToTablix (doc), Is.EqualTo (1));

            Assert.That (doc.GetElementsByTagName ("Matrix"), Is.Empty, "<Matrix> is not in the RDLC schema");
            var tablix = First (doc, "Tablix");
            Assert.That (tablix.GetAttribute ("Name"), Is.EqualTo ("Pivot"));
            var columnMember = tablix["TablixColumnHierarchy"]["TablixMembers"]["TablixMember"];
            Assert.That (columnMember["Group"].GetAttribute ("Name"), Is.EqualTo ("ByYear"));
            Assert.That (columnMember["TablixHeader"]["Size"].InnerText, Is.EqualTo ("0.25in"));
            var rowMember = tablix["TablixRowHierarchy"]["TablixMembers"]["TablixMember"];
            Assert.That (rowMember["Group"].GetAttribute ("Name"), Is.EqualTo ("ByRegion"));
            Assert.That (rowMember["TablixHeader"]["Size"].InnerText, Is.EqualTo ("1in"));
            Assert.That (tablix["TablixCorner"], Is.Not.Null);
        }

        [Test]
        public void RoundTrip_MatrixSurvivesExportAndImport ()
        {
            var doc = Load (ReportWith (Rdl2010, SalesMatrix));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            Assert.That (doc.GetElementsByTagName ("Tablix"), Is.Empty);
            var matrix = First (doc, "Matrix");
            Assert.That (matrix, Is.Not.Null, "an exported pivot reads back as a Matrix");
            string Text (XmlNode n) => string.Concat (n.SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ().Select (v => v.InnerText));
            var column = matrix["ColumnGroupings"]["ColumnGrouping"];
            Assert.That (column["Height"].InnerText, Is.EqualTo ("0.25in"));
            Assert.That (column["DynamicColumns"]["Grouping"].GetAttribute ("Name"), Is.EqualTo ("ByYear"));
            Assert.That (column["DynamicColumns"]["Sorting"]["SortBy"]["SortExpression"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            var row = matrix["RowGroupings"]["RowGrouping"];
            Assert.That (row["Width"].InnerText, Is.EqualTo ("1in"));
            Assert.That (row["DynamicRows"]["Grouping"].GetAttribute ("Name"), Is.EqualTo ("ByRegion"));
            Assert.That (Text (matrix["MatrixRows"]), Is.EqualTo ("=Sum(Fields!Name.Value)"));
            Assert.That (Text (matrix["Corner"]), Is.EqualTo ("Corner"));
        }

        private const string TwoLevelMatrix = @"
<Matrix Name=""Pivot2"">
  <Corner><ReportItems><Textbox Name=""Corner""><Value>Corner</Value></Textbox></ReportItems></Corner>
  <ColumnGroupings>
    <ColumnGrouping><Height>0.25in</Height><DynamicColumns><Grouping Name=""Year""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping><ReportItems><Textbox Name=""Y""><Value>y</Value></Textbox></ReportItems></DynamicColumns></ColumnGrouping>
    <ColumnGrouping><Height>0.3in</Height><DynamicColumns><Grouping Name=""Quarter""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping><ReportItems><Textbox Name=""Q""><Value>q</Value></Textbox></ReportItems></DynamicColumns></ColumnGrouping>
  </ColumnGroupings>
  <RowGroupings>
    <RowGrouping><Width>1in</Width><DynamicRows><Grouping Name=""Region""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping><ReportItems><Textbox Name=""R""><Value>r</Value></Textbox></ReportItems></DynamicRows></RowGrouping>
    <RowGrouping><Width>0.8in</Width><DynamicRows><Grouping Name=""Store""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Grouping><ReportItems><Textbox Name=""S""><Value>s</Value></Textbox></ReportItems></DynamicRows></RowGrouping>
  </RowGroupings>
  <MatrixRows><MatrixRow><Height>0.25in</Height><MatrixCells><MatrixCell><ReportItems><Textbox Name=""Cell""><Value>v</Value></Textbox></ReportItems></MatrixCell></MatrixCells></MatrixRow></MatrixRows>
  <MatrixColumns><MatrixColumn><Width>1in</Width></MatrixColumn></MatrixColumns>
</Matrix>";

        [Test]
        public void MultiLevelMatrix_CornerSpansTheHeaderBlock ()
        {
            var doc = Load (ReportWith (Rdl2010, TwoLevelMatrix));

            Rdl2008Exporter.ConvertToTablix (doc);

            var corner = First (doc, "TablixCorner");
            var rows = corner["TablixCornerRows"].ChildNodes;
            Assert.That (rows, Has.Count.EqualTo (2), "one corner row per column level");
            Assert.That (rows[0].ChildNodes, Has.Count.EqualTo (2), "one corner cell per row level");
            var contents = rows[0].FirstChild["CellContents"];
            Assert.That (contents["ColSpan"].InnerText, Is.EqualTo ("2"));
            Assert.That (contents["RowSpan"].InnerText, Is.EqualTo ("2"));
        }

        [Test]
        public void RoundTrip_MultiLevelMatrixKeepsBothLevelsAndTheCorner ()
        {
            var doc = Load (ReportWith (Rdl2010, TwoLevelMatrix));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var matrix = First (doc, "Matrix");
            Assert.That (matrix, Is.Not.Null);
            var columns = matrix["ColumnGroupings"].ChildNodes;
            Assert.That (columns, Has.Count.EqualTo (2));
            Assert.That (columns[0]["DynamicColumns"]["Grouping"].GetAttribute ("Name"), Is.EqualTo ("Year"));
            Assert.That (columns[1]["DynamicColumns"]["Grouping"].GetAttribute ("Name"), Is.EqualTo ("Quarter"));
            Assert.That (columns[1]["Height"].InnerText, Is.EqualTo ("0.3in"));
            var rows = matrix["RowGroupings"].ChildNodes;
            Assert.That (rows[1]["DynamicRows"]["Grouping"].GetAttribute ("Name"), Is.EqualTo ("Store"));
            Assert.That (rows[1]["Width"].InnerText, Is.EqualTo ("0.8in"));
            var cornerItem = matrix["Corner"]["ReportItems"].FirstChild;
            Assert.That (cornerItem.LocalName, Is.EqualTo ("Textbox"));
            Assert.That (cornerItem["Width"], Is.Null, "an item covering the whole corner is left unpositioned");
        }

        [Test]
        public void Import_CornerItemWithSpan_IsSizedAcrossTheSpannedLevels ()
        {
            // An item in the first corner cell spanning two row levels but one column level.
            var doc = Load (ReportWith (Rdl2010, TwoLevelMatrix));
            Rdl2008Exporter.ConvertToTablix (doc);
            var corner = First (doc, "TablixCorner");
            var contents = corner["TablixCornerRows"].FirstChild.FirstChild["CellContents"];
            contents.RemoveChild (contents["RowSpan"]);

            Rdl2008Normalizer.Normalize (doc, null);

            var item = First (doc, "Matrix")["Corner"]["ReportItems"].FirstChild;
            Assert.That (item["Width"].InnerText, Is.EqualTo ("1.8in"), "1in + 0.8in");
            Assert.That (item["Height"].InnerText, Is.EqualTo ("0.25in"));
        }

        private static string PivotCell (string name, string text)
            => $@"<TablixCell><CellContents><Textbox Name=""{name}""><Value>{text}</Value></Textbox></CellContents></TablixCell>";

        private static string Header (string name, string text, string size = "1in")
            => $@"<TablixHeader><Size>{size}</Size><CellContents><Textbox Name=""{name}""><Value>{text}</Value></Textbox></CellContents></TablixHeader>";

        /// <summary>
        /// A pivot with a Total column after the Year group and a Grand total row after the Region
        /// group: body is 2 rows x 2 columns, of which only the first of each is detail.
        /// </summary>
        private static readonly string PivotWithSubtotals = $@"
<Tablix Name=""Sales""><DataSetName>Data</DataSetName>
  <TablixBody>
    <TablixColumns><TablixColumn><Width>1in</Width></TablixColumn><TablixColumn><Width>1.2in</Width></TablixColumn></TablixColumns>
    <TablixRows>
      <TablixRow><Height>0.25in</Height><TablixCells>{PivotCell ("Amount", "=Sum(Fields!Name.Value)")}{PivotCell ("RowTotal", "=Sum(Fields!Name.Value)")}</TablixCells></TablixRow>
      <TablixRow><Height>0.25in</Height><TablixCells>{PivotCell ("ColTotal", "=Sum(Fields!Name.Value)")}{PivotCell ("GrandTotal", "=Sum(Fields!Name.Value)")}</TablixCells></TablixRow>
    </TablixRows>
  </TablixBody>
  <TablixColumnHierarchy><TablixMembers>
    <TablixMember><Group Name=""Year""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>{Header ("YearHead", "=Fields!Name.Value", "0.25in")}</TablixMember>
    <TablixMember>{Header ("YearTotal", "Total", "0.25in")}</TablixMember>
  </TablixMembers></TablixColumnHierarchy>
  <TablixRowHierarchy><TablixMembers>
    <TablixMember><Group Name=""Region""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>{Header ("RegionHead", "=Fields!Name.Value")}</TablixMember>
    <TablixMember>{Header ("RegionTotal", "Grand total")}</TablixMember>
  </TablixMembers></TablixRowHierarchy>
</Tablix>";

        [Test]
        public void Import_PivotWithSubtotals_BecomesMatrixWithSubtotalElements ()
        {
            var doc = Load (ReportWith (Rdl2010, PivotWithSubtotals));

            Rdl2008Normalizer.Normalize (doc, null);

            var matrix = First (doc, "Matrix");
            Assert.That (matrix, Is.Not.Null, "a pivot with totals is still a pivot");
            string Text (XmlNode n) => string.Concat (n.SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ().Select (v => v.InnerText));
            var columns = matrix["ColumnGroupings"]["ColumnGrouping"]["DynamicColumns"];
            Assert.That (Text (columns["Subtotal"]), Is.EqualTo ("Total"));
            var rows = matrix["RowGroupings"]["RowGrouping"]["DynamicRows"];
            Assert.That (Text (rows["Subtotal"]), Is.EqualTo ("Grand total"));
            Assert.That (matrix["MatrixRows"].ChildNodes, Has.Count.EqualTo (1), "only the detail row is kept");
            Assert.That (matrix["MatrixRows"].FirstChild["MatrixCells"].ChildNodes, Has.Count.EqualTo (1), "only the detail column is kept");
            Assert.That (matrix["MatrixColumns"].ChildNodes, Has.Count.EqualTo (1));
            Assert.That (matrix["MatrixColumns"].FirstChild["Width"].InnerText, Is.EqualTo ("1in"), "the detail column's width, not the total's");
            Assert.That (Text (matrix["MatrixRows"]), Is.EqualTo ("=Sum(Fields!Name.Value)"));
        }

        [Test]
        public void Import_SubtotalBeforeTheGroup_KeepsTheDetailColumn ()
        {
            // Total listed first: the detail column is then the second body column.
            var xml = PivotWithSubtotals
                .Replace ("<TablixColumn><Width>1in</Width></TablixColumn><TablixColumn><Width>1.2in</Width></TablixColumn>",
                          "<TablixColumn><Width>1.2in</Width></TablixColumn><TablixColumn><Width>1in</Width></TablixColumn>");
            var doc = Load (ReportWith (Rdl2010, xml));
            var members = doc.GetElementsByTagName ("TablixColumnHierarchy")[0]["TablixMembers"];
            members.InsertBefore (members.LastChild, members.FirstChild);   // static total first, then the group

            Rdl2008Normalizer.Normalize (doc, null);

            var matrix = First (doc, "Matrix");
            Assert.That (matrix, Is.Not.Null);
            Assert.That (matrix["MatrixColumns"].FirstChild["Width"].InnerText, Is.EqualTo ("1in"));
            Assert.That (matrix["MatrixRows"].FirstChild["MatrixCells"].FirstChild.InnerText, Does.Contain ("Sum"));
        }

        [Test]
        public void Import_AdjacentGroups_AreStillRefused ()
        {
            var xml = PivotWithSubtotals.Replace ($"<TablixMember>{Header ("YearTotal", "Total", "0.25in")}</TablixMember>",
                $@"<TablixMember><Group Name=""Other""><GroupExpressions><GroupExpression>=Fields!Name.Value</GroupExpression></GroupExpressions></Group>{Header ("Other", "x", "0.25in")}</TablixMember>");
            var doc = Load (ReportWith (Rdl2010, xml));
            var log = new ReportLog ();

            Rdl2008Normalizer.Normalize (doc, log);

            Assert.That (First (doc, "Matrix"), Is.Null, "two sibling groups have no Matrix equivalent");
            Assert.That (First (doc, "Tablix"), Is.Not.Null, "and are left in place rather than rendered wrongly");
            Assert.That (log.MaxSeverity, Is.GreaterThanOrEqualTo (8));
        }

        [Test]
        public void RoundTrip_PivotSubtotalsSurviveExportAndImport ()
        {
            var doc = Load (ReportWith (Rdl2010, PivotWithSubtotals));
            Rdl2008Normalizer.Normalize (doc, null);   // as the designer opens it

            Rdl2008Exporter.ConvertToTablix (doc);     // as the designer saves it

            var tablix = First (doc, "Tablix");
            Assert.That (tablix["TablixBody"]["TablixColumns"].ChildNodes, Has.Count.EqualTo (2), "detail plus total column");
            Assert.That (tablix["TablixBody"]["TablixRows"].ChildNodes, Has.Count.EqualTo (2), "detail plus total row");
            var texts = string.Concat (tablix["TablixColumnHierarchy"].SelectNodes (".//*[local-name()='Value']").Cast<XmlNode> ().Select (v => v.InnerText + "|"));
            Assert.That (texts, Does.Contain ("Total"));

            Rdl2008Normalizer.Normalize (doc, null);

            var matrix = First (doc, "Matrix");
            Assert.That (matrix["ColumnGroupings"]["ColumnGrouping"]["DynamicColumns"]["Subtotal"], Is.Not.Null);
            Assert.That (matrix["RowGroupings"]["RowGrouping"]["DynamicRows"]["Subtotal"], Is.Not.Null);
        }

        [Test]
        public async Task ExportedMatrix_ParsesInTheEngine ()
        {
            var doc = Load (ReportWith (Rdl2010, SalesMatrix));
            Rdl2008Exporter.ConvertToTablix (doc);

            var parser = new RDLParser (doc.OuterXml) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }

        [Test]
        public void Textbox_IsWrittenWithParagraphsAndTextRuns ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<Textbox Name=""Title""><Top>0in</Top><Height>0.25in</Height><Width>2in</Width><Value>Hello</Value>
  <Style><FontFamily>Arial</FontFamily><FontSize>12pt</FontSize><TextAlign>Center</TextAlign><BackgroundColor>Yellow</BackgroundColor></Style>
</Textbox>"));

            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.True);
            Rdl2008Exporter.ConvertToTablix (doc);

            var textbox = First (doc, "Textbox");
            Assert.That (textbox["Value"], Is.Null, "a 2008 Textbox has no direct Value");
            var run = textbox["Paragraphs"]["Paragraph"]["TextRuns"]["TextRun"];
            Assert.That (run["Value"].InnerText, Is.EqualTo ("Hello"));
            Assert.That (run["Style"]["FontFamily"].InnerText, Is.EqualTo ("Arial"));
            Assert.That (run["Style"]["FontSize"].InnerText, Is.EqualTo ("12pt"));
            Assert.That (textbox["Paragraphs"]["Paragraph"]["Style"]["TextAlign"].InnerText, Is.EqualTo ("Center"));
            Assert.That (textbox["Style"]["BackgroundColor"].InnerText, Is.EqualTo ("Yellow"), "box styling stays on the Textbox");
            Assert.That (textbox["Style"]["FontFamily"], Is.Null);
        }

        [Test]
        public void Textbox_RoundTripKeepsValueAndStyle ()
        {
            var doc = Load (ReportWith (Rdl2010, @"
<Textbox Name=""Title""><Height>0.25in</Height><Width>2in</Width><Value>=Fields!Name.Value</Value>
  <Style><FontWeight>Bold</FontWeight><TextAlign>Right</TextAlign></Style>
</Textbox>"));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var textbox = First (doc, "Textbox");
            Assert.That (textbox["Value"].InnerText, Is.EqualTo ("=Fields!Name.Value"));
            Assert.That (textbox["Style"]["FontWeight"].InnerText, Is.EqualTo ("Bold"));
            Assert.That (textbox["Style"]["TextAlign"].InnerText, Is.EqualTo ("Right"));
        }

        [Test]
        public void Textbox_AlreadyInTheRichTextForm_IsLeftAlone ()
        {
            var doc = Load (ReportWith ("http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition", @"
<Textbox Name=""T""><Paragraphs><Paragraph><TextRuns><TextRun><Value>x</Value><Style /></TextRun></TextRuns><Style /></Paragraph></Paragraphs></Textbox>"));

            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.False);
        }

        /// <summary>
        /// Validates exported output against Microsoft's own schemas. The XSDs are not
        /// redistributed here; see ReportTests/Schemas/README.md for where to get them, then set
        /// RDL_XSD_2008 / RDL_XSD_2010 / RDL_XSD_2016 to their paths to run the matching case.
        /// </summary>
        [TestCase ("2008")]
        [TestCase ("2010")]
        [TestCase ("2016")]
        public void ExportedRegions_ValidateAgainstTheMicrosoftSchema (string version)
        {
            var xsdPath = System.Environment.GetEnvironmentVariable ("RDL_XSD_" + version);
            if (string.IsNullOrEmpty (xsdPath) || !System.IO.File.Exists (xsdPath))
                Assert.Ignore ($"Set RDL_XSD_{version} to a local copy of the Microsoft {version} ReportDefinition.xsd to run.");

            var ns = $"http://schemas.microsoft.com/sqlserver/reporting/{version}/01/reportdefinition";
            var schemas = new System.Xml.Schema.XmlSchemaSet ();
            schemas.Add (ns, xsdPath);

            var matrixWithTotals = SalesMatrix.Replace ("</DynamicColumns>",
                @"<Subtotal><ReportItems><Textbox Name=""TT""><Value>Total</Value></Textbox></ReportItems></Subtotal></DynamicColumns>");
            foreach (var (label, region) in new[] { ("List", SimpleList), ("Table", GroupedTable), ("Matrix", SalesMatrix), ("Matrix with subtotal", matrixWithTotals) }) {
                var doc = Load (ReportWith (ns, region));
                // What the designer holds besides the region: page setup, a page header and a
                // bordered textbox, all in the 2005 spelling.
                doc.DocumentElement.InnerXml += PageSetup2005;
                Rdl2008Exporter.ConvertToTablix (doc);

                var errors = new System.Collections.Generic.List<string> ();
                var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = schemas };
                settings.ValidationEventHandler += (_, e) => errors.Add ($"line {e.Exception.LineNumber}: {e.Message}");
                using (var reader = XmlReader.Create (new System.IO.StringReader (doc.OuterXml), settings)) {
                    while (reader.Read ()) { }
                }

                Assert.That (errors, Is.Empty, $"{label}: " + string.Join (" | ", errors));
            }
        }

        private const string PageSetup2005 = @"
<PageHeader><Height>0.5in</Height><PrintOnFirstPage>true</PrintOnFirstPage><PrintOnLastPage>true</PrintOnLastPage>
  <ReportItems><Textbox Name=""HeaderText""><Top>0in</Top><Left>0in</Left><Height>0.25in</Height><Width>2in</Width><Value>Page header</Value>
    <Style><FontWeight>Bold</FontWeight><BorderStyle><Default>Solid</Default><Bottom>Dashed</Bottom></BorderStyle><BorderColor><Default>Black</Default></BorderColor><BorderWidth><Default>1pt</Default></BorderWidth></Style>
  </Textbox></ReportItems>
</PageHeader>
<PageHeight>11in</PageHeight><PageWidth>8.5in</PageWidth><LeftMargin>1in</LeftMargin><RightMargin>1in</RightMargin><TopMargin>1in</TopMargin><BottomMargin>1in</BottomMargin>";

        [Test]
        public void Borders_AreWrittenPerEdge ()
        {
            var doc = Load (ReportWith (Rdl2010,
                @"<Textbox Name=""B""><Value>x</Value><Style><BorderStyle><Default>Solid</Default><Bottom>Dashed</Bottom></BorderStyle><BorderColor><Default>Red</Default></BorderColor><BorderWidth><Default>2pt</Default></BorderWidth></Style></Textbox>"));

            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.True);
            Rdl2008Exporter.ConvertToTablix (doc);

            var style = First (doc, "Textbox")["Style"];
            Assert.That (style["BorderStyle"], Is.Null, "2005 border groups are not in the 2008 schema");
            Assert.That (style["Border"]["Style"].InnerText, Is.EqualTo ("Solid"));
            Assert.That (style["Border"]["Color"].InnerText, Is.EqualTo ("Red"));
            Assert.That (style["Border"]["Width"].InnerText, Is.EqualTo ("2pt"));
            Assert.That (style["BottomBorder"]["Style"].InnerText, Is.EqualTo ("Dashed"));
        }

        [Test]
        public void Borders_RoundTrip ()
        {
            var doc = Load (ReportWith (Rdl2010,
                @"<Textbox Name=""B""><Value>x</Value><Style><BorderStyle><Default>Solid</Default><Bottom>Dashed</Bottom></BorderStyle><BorderWidth><Default>2pt</Default></BorderWidth></Style></Textbox>"));
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var style = First (doc, "Textbox")["Style"];
            Assert.That (style["BorderStyle"]["Default"].InnerText, Is.EqualTo ("Solid"));
            Assert.That (style["BorderStyle"]["Bottom"].InnerText, Is.EqualTo ("Dashed"));
            Assert.That (style["BorderWidth"]["Default"].InnerText, Is.EqualTo ("2pt"));
        }

        [TestCase (Rdl2010, true)]
        [TestCase ("http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition", true)]
        [TestCase ("http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition", false)]
        public void BodyAndPageSetup_AreGroupedForEachVersion (string ns, bool expectSections)
        {
            var doc = Load (ReportWith (ns, "<Textbox Name=\"T\"><Value>x</Value></Textbox>"));
            doc.DocumentElement.InnerXml += PageSetup2005;

            Rdl2008Exporter.ConvertToTablix (doc);

            var root = doc.DocumentElement;
            if (!expectSections) {
                Assert.That (root["ReportSections"], Is.Null, "2008 has no sections");
                Assert.That (root["Body"], Is.Not.Null);
                Assert.That (root["Page"]["PageHeader"], Is.Not.Null, "but its page setup lives in Page");
                Assert.That (root["Page"]["PageHeight"].InnerText, Is.EqualTo ("11in"));
                Assert.That (root["PageHeight"], Is.Null);
                return;
            }
            Assert.That (root["Body"], Is.Null, "Body moves into the section");
            var section = root["ReportSections"]["ReportSection"];
            Assert.That (section["Body"], Is.Not.Null);
            Assert.That (section["Width"].InnerText, Is.EqualTo ("6in"));
            Assert.That (section["Page"]["PageHeader"], Is.Not.Null);
            Assert.That (section["Page"]["PageHeight"].InnerText, Is.EqualTo ("11in"));
            Assert.That (root["PageHeight"], Is.Null);
            Assert.That (Rdl2008Exporter.NeedsConversion (doc), Is.False, "converting twice must not wrap twice");
        }

        [Test]
        public void ReportSections_RoundTripBackToTheFlatLayout ()
        {
            var doc = Load (ReportWith (Rdl2010, "<Textbox Name=\"T\"><Value>x</Value></Textbox>"));
            doc.DocumentElement.InnerXml += PageSetup2005;
            Rdl2008Exporter.ConvertToTablix (doc);

            Rdl2008Normalizer.Normalize (doc, null);

            var root = doc.DocumentElement;
            Assert.That (root["ReportSections"], Is.Null);
            Assert.That (root["Body"], Is.Not.Null);
            Assert.That (root["PageHeader"], Is.Not.Null);
            Assert.That (root["PageHeight"].InnerText, Is.EqualTo ("11in"));
        }

        [Test]
        public async Task ExportedReport_ParsesInTheEngine ()
        {
            var doc = Load (ReportWith (Rdl2010, SimpleList));
            Rdl2008Exporter.ConvertToTablix (doc);

            var parser = new RDLParser (doc.OuterXml) { SkipDatabaseSchemaValidation = true };
            using var report = await parser.Parse ();

            Assert.That (report, Is.Not.Null);
            Assert.That (report.ErrorMaxSeverity, Is.LessThanOrEqualTo (4),
                string.Join (" | ", report.ErrorItems ?? new System.Collections.ArrayList ()));
        }
    }
}
