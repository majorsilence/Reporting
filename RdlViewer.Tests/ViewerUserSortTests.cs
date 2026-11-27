// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlViewer.Tests
{
    // Interactive sorting (a textbox's UserSort, issue #636) in the viewer: a click on a sort
    // header -- PageItem.IsUserSortClick picks the click, RdlViewer.SortItem is what the mouse
    // handler calls with it -- lays the report out again from the data already read, sorted.
    [TestFixture]
    public class ViewerUserSortTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Rdl(string dataPath) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file={dataPath}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>columns=Name,Amount;table=root_Rows</CommandText></Query>
    <Fields>
      <Field Name=""Name""><DataField>Name</DataField></Field>
      <Field Name=""Amount""><DataField>Amount</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>150pt</Width></TableColumn><TableColumn><Width>150pt</Width></TableColumn></TableColumns>
      <Header><TableRows><TableRow><Height>14pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""SortName""><Value>Name</Value>
          <UserSort><SortExpression>=Fields!Name.Value</SortExpression></UserSort></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""SortAmount""><Value>Amount</Value>
          <UserSort><SortExpression>=CInt(Fields!Amount.Value)</SortExpression></UserSort></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Header>
      <Details><TableRows><TableRow><Height>14pt</Height><TableCells>
        <TableCell><ReportItems><Textbox Name=""Line""><Value>=""D "" &amp; Fields!Name.Value &amp; CStr(Fields!Amount.Value)</Value></Textbox></ReportItems></TableCell>
        <TableCell><ReportItems><Textbox Name=""Pad""><Value></Value></Textbox></ReportItems></TableCell>
      </TableCells></TableRow></TableRows></Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        private static readonly string[] Lines = { "D b2", "D a2", "D c1", "D a1" };     // data order

        private string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "usersort-" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        private async Task<RdlViewer> Open()
        {
            Directory.CreateDirectory(_dir);
            string dataPath = Path.Combine(_dir, "data.json");
            await File.WriteAllTextAsync(dataPath,
                @"{ ""Rows"": [ { ""Name"": ""b"", ""Amount"": ""2"" }, { ""Name"": ""a"", ""Amount"": ""2"" },
                               { ""Name"": ""c"", ""Amount"": ""1"" }, { ""Name"": ""a"", ""Amount"": ""1"" } ] }");
            string rdlPath = Path.Combine(_dir, "usersort.rdl");
            await File.WriteAllTextAsync(rdlPath, Rdl(dataPath));

            var viewer = new RdlViewer { ShowWaitDialog = false };
            await viewer.SetSourceFile(new System.Uri(rdlPath));
            await viewer.EnsureRendered();
            return viewer;
        }

        // The detail lines as the viewer shows them, top to bottom.
        private static async Task<string[]> Order(RdlViewer viewer)
        {
            var found = new System.Collections.Generic.List<(float Y, string Text)>();
            foreach (string line in Lines)
            {
                PageItem pi = await viewer.Find(line);
                Assert.That(pi, Is.Not.Null, line);
                found.Add((pi.Y, line));
            }
            return found.OrderBy(f => f.Y).Select(f => f.Text).ToArray();
        }

        // What a left click on the middle of a header's sort glyph does (PageDrawing.OnMouseDown).
        private static async Task ClickGlyph(RdlViewer viewer, string header, bool shift = false)
        {
            PageItem pi = await viewer.Find(header);
            Assert.That(pi, Is.Not.Null, header);
            pi.GetUserSortGlyphBounds(out float gx, out float gy, out float size);
            float px = gx + size / 2, py = gy + size / 2;
            Assert.That(pi.IsUserSortClick(px, py), Is.True, header + " sorts on a click on its glyph");
            await viewer.SortItem(pi.UserSortKey, shift);
        }

        private static async Task<UserSortDirection> Direction(RdlViewer viewer, string header) =>
            (await viewer.Find(header)).UserSortDirection;

        [Test]
        public async Task Clicking_a_sort_header_sorts_ascending_then_descending()
        {
            using var viewer = await Open();
            Assert.That(await Order(viewer), Is.EqualTo(Lines), "unsorted: data order");
            Assert.That(await Direction(viewer, "Name"), Is.EqualTo(UserSortDirection.None));

            await ClickGlyph(viewer, "Name");
            Assert.That(await Order(viewer), Is.EqualTo(new[] { "D a2", "D a1", "D b2", "D c1" }));
            Assert.That(await Direction(viewer, "Name"), Is.EqualTo(UserSortDirection.Ascending));

            await ClickGlyph(viewer, "Name");
            Assert.That(await Order(viewer), Is.EqualTo(new[] { "D c1", "D b2", "D a2", "D a1" }));
            Assert.That(await Direction(viewer, "Name"), Is.EqualTo(UserSortDirection.Descending));
        }

        [Test]
        public async Task Shift_click_adds_a_secondary_sort_and_a_plain_click_replaces_it()
        {
            using var viewer = await Open();

            await ClickGlyph(viewer, "Amount");
            Assert.That(await Order(viewer), Is.EqualTo(new[] { "D c1", "D a1", "D b2", "D a2" }),
                "by amount alone: ties keep data order");

            await ClickGlyph(viewer, "Name", shift: true);
            Assert.That(await Order(viewer), Is.EqualTo(new[] { "D a1", "D c1", "D a2", "D b2" }),
                "then by name within each amount");
            Assert.That(await Direction(viewer, "Amount"), Is.EqualTo(UserSortDirection.Ascending));
            Assert.That(await Direction(viewer, "Name"), Is.EqualTo(UserSortDirection.Ascending));

            await ClickGlyph(viewer, "Name");
            Assert.That(await Order(viewer), Is.EqualTo(new[] { "D c1", "D b2", "D a2", "D a1" }),
                "a plain click sorts by name alone, flipped");
            Assert.That(await Direction(viewer, "Name"), Is.EqualTo(UserSortDirection.Descending));
            Assert.That(await Direction(viewer, "Amount"), Is.EqualTo(UserSortDirection.None));
        }
    }
}
