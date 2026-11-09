#if NET8_0_OR_GREATER
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTests
{
    /// <summary>
    /// Visibility toggles (Visibility/ToggleItem, the drill-down +/-) in paged output: a toggle
    /// textbox's page items say which instance they are, and toggling one in the report flips
    /// what it hides the next time the pages are built (issue #635).
    /// </summary>
    [TestFixture]
    public class VisibilityToggleTests
    {
        // A table grouped on K. Its group header shows K in the toggle textbox "Tog"; the detail
        // rows, or (groupToggle) a nested group on V, start hidden and are toggled by it.
        private static string Rdl(bool toggleItem = true, bool groupToggle = false)
        {
            string visibility = $@"<Visibility><Hidden>true</Hidden>{(toggleItem ? "<ToggleItem>Tog</ToggleItem>" : "")}</Visibility>";
            string detailRow = @"<TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Line""><Value>=""D "" &amp; CStr(Fields!V.Value)</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows>";
            string innerGroup = groupToggle ? $@"<TableGroup>
        <Grouping Name=""GV""><GroupExpressions><GroupExpression>=Fields!V.Value</GroupExpression></GroupExpressions></Grouping>
        <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Inner""><Value>=""V "" &amp; CStr(Fields!V.Value)</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
        {visibility}
      </TableGroup>" : "";
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""D""><ConnectionProperties><DataProvider>SQL</DataProvider><ConnectString /></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>D</DataSourceName><CommandText>none</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField><TypeName>Int32</TypeName></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=Fields!K.Value</GroupExpression></GroupExpressions></Grouping>
        <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Tog""><Value>=Fields!K.Value</Value><Style><PaddingLeft>2pt</PaddingLeft></Style></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      </TableGroup>{innerGroup}</TableGroups>
      <Details>{detailRow}{(groupToggle ? "" : visibility)}</Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";
        }

        private static async Task<Report> Load(string rdl)
        {
            RdlEngineConfig.RdlEngineConfigInit();
            var report = await new RDLParser(rdl) { SkipDatabaseSchemaValidation = true }.Parse();
            var table = new DataTable();
            table.Columns.Add("K", typeof(string));
            table.Columns.Add("V", typeof(int));
            foreach (var (k, v) in new[] { ("a", 1), ("a", 2), ("b", 10), ("b", 20), ("b", 30) })
                table.Rows.Add(k, v);
            await report.DataSets["Data"].SetData(table);
            await report.RunGetData(null);
            return report;
        }

        private static PageText[] Texts(Pages pages) =>
            pages.Cast<Page>().SelectMany(p => p.Cast<PageItem>()).OfType<PageText>()
                .OrderBy(t => t.Y).ToArray();

        private static string[] Lines(Pages pages) => Texts(pages).Select(t => t.Text).ToArray();

        private static PageText Toggle(Pages pages, string text) =>
            Texts(pages).Single(t => t.Text == text && t.ToggleKey != null);

        [Test]
        public async Task WithNothingToggled_TheHiddenDetailRowsAreHidden()
        {
            using var report = await Load(Rdl());
            var pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public async Task TheToggleTextbox_SaysWhichInstanceItIsAndThatItIsCollapsed()
        {
            using var report = await Load(Rdl());
            var pages = await report.BuildPages();
            var a = Toggle(pages, "a");
            var b = Toggle(pages, "b");
            Assert.That(a.ToggleKey, Is.Not.EqualTo(b.ToggleKey));
            Assert.That(a.ToggleExpanded, Is.False);
            Assert.That(b.ToggleExpanded, Is.False);
            // An item that toggles nothing is not a toggle.
            Assert.That(Texts(pages).Where(t => t.Text != "a" && t.Text != "b"), Is.Empty);
        }

        [Test]
        public async Task ToggledOnce_TheInstancesRowsShow_AndOnlyThatInstances()
        {
            using var report = await Load(Rdl());
            var pages = await report.BuildPages();
            report.Toggle(Toggle(pages, "a").ToggleKey);

            pages = await report.BuildPages();     // from the data already read
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "D 1", "D 2", "b" }));
            Assert.That(Toggle(pages, "a").ToggleExpanded, Is.True);
            Assert.That(Toggle(pages, "b").ToggleExpanded, Is.False);
            Assert.That(Texts(pages).Single(t => t.Text == "D 1").ToggleKey, Is.Null);
        }

        [Test]
        public async Task ToggledTwice_TheRowsHideAgain()
        {
            using var report = await Load(Rdl());
            var pages = await report.BuildPages();
            string key = Toggle(pages, "b").ToggleKey;
            report.Toggle(key);
            pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b", "D 10", "D 20", "D 30" }));

            report.Toggle(key);
            pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(report.IsToggled(key), Is.False);
        }

        [Test]
        public async Task ClearToggles_PutsEverythingBack()
        {
            using var report = await Load(Rdl());
            var pages = await report.BuildPages();
            report.Toggle(Toggle(pages, "a").ToggleKey);
            report.Toggle(Toggle(pages, "b").ToggleKey);
            report.ClearToggles();
            pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public async Task TheGlyph_IsInsideTheToggle_InTheRoomTheViewerMovesItsTextOver()
        {
            using var report = await Load(Rdl());
            var tog = Toggle(await report.BuildPages(), "a");
            Assert.That(tog.SI.PaddingLeft, Is.EqualTo(2).Within(0.05), "the page item keeps its own layout");

            tog.GetToggleGlyphBounds(out float x, out float y, out float size);
            Assert.That(size, Is.EqualTo(PageItem.ToggleGlyphSize));
            Assert.That(x, Is.GreaterThanOrEqualTo(tog.X + tog.SI.PaddingLeft));
            Assert.That(x + size, Is.LessThanOrEqualTo(tog.X + tog.SI.PaddingLeft + PageItem.ToggleGlyphSpace));
            Assert.That(y, Is.GreaterThanOrEqualTo(tog.Y));
            Assert.That(y + size, Is.LessThanOrEqualTo(tog.Y + tog.H));
        }

        [Test]
        public async Task InAnInteractiveViewer_TheLayoutIsTheSameAsStatic_WhenNothingIsToggledInAGroup()
        {
            using var interactive = await Load(Rdl());
            interactive.InteractiveToggles = true;
            using var plain = await Load(Rdl());
            Assert.That(Texts(await interactive.BuildPages()).Select(Layout),
                Is.EqualTo(Texts(await plain.BuildPages()).Select(Layout)));
        }

        [Test]
        public async Task InAnInteractiveViewer_AGroupToggledByAnOuterGroup_CollapsesAndExpands()
        {
            using var report = await Load(Rdl(groupToggle: true));
            report.InteractiveToggles = true;
            var pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b" }));

            report.Toggle(Toggle(pages, "b").ToggleKey);
            pages = await report.BuildPages();
            Assert.That(Lines(pages), Is.EqualTo(new[] { "a", "b", "V 10", "D 10", "V 20", "D 20", "V 30", "D 30" }));
        }

        // Static output is unchanged: paged output (what PDF, TIF and the image renderers draw)
        // with a ToggleItem is laid out exactly as without one, and a toggled table group is
        // laid out as it always was, every instance shown.
        [Test]
        public async Task StaticOutput_IsLaidOutAsWithoutAToggle()
        {
            using var withToggle = await Load(Rdl(toggleItem: true));
            using var without = await Load(Rdl(toggleItem: false));
            var a = Texts(await withToggle.BuildPages());
            var b = Texts(await without.BuildPages());
            Assert.That(a.Select(Layout), Is.EqualTo(b.Select(Layout)));

            using var groupWithToggle = await Load(Rdl(toggleItem: true, groupToggle: true));
            using var groupWithout = await Load(Rdl(toggleItem: false, groupToggle: true));
            var c = Texts(await groupWithToggle.BuildPages());
            var d = Texts(await groupWithout.BuildPages());
            Assert.That(c.Select(Layout), Is.EqualTo(d.Select(Layout)));
            Assert.That(c.Select(t => t.Text), Does.Contain("V 10"));
        }

        // The renders that go through the report items rather than the pages print what they
        // printed before toggles could be clicked: a toggled detail row or group is written out
        // (the renderer has no way to expand it).
        [TestCase(false, new[] { "a", "D 1", "D 2", "b", "D 10", "D 20", "D 30" })]
        [TestCase(true, new[] { "a", "V 1", "D 1", "V 2", "D 2", "b", "V 10", "D 10", "V 20", "D 20", "V 30", "D 30" })]
        public async Task StaticRenders_PrintWhatTheyAlwaysHave(bool groupToggle, string[] expected)
        {
            using var csvReport = await Load(Rdl(groupToggle: groupToggle));
            string csv = await Render(csvReport, OutputPresentationType.CSV);
            Assert.That(csv.Split('\n', System.StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r').Trim('"')),
                Is.EqualTo(expected));

            using var rtfReport = await Load(Rdl(groupToggle: groupToggle));
            string rtf = await Render(rtfReport, OutputPresentationType.RTF);
            foreach (string line in expected)
                Assert.That(rtf, Does.Contain(line + "}\\cell"), line);
        }

        private static async Task<string> Render(Report report, OutputPresentationType type)
        {
            using var sg = new MemoryStreamGen();
            await report.RunRender(sg, type);
            return sg.GetText();
        }

        private static string Layout(PageText t) =>
            $"{t.Text}|{t.X}|{t.Y}|{t.W}|{t.H}|{t.SI.PaddingLeft}";
    }
}
#endif
