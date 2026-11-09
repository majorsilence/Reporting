// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;
using NUnit.Framework;

namespace Majorsilence.Reporting.RdlViewer.Tests
{
    // Drill-down toggles (Visibility/ToggleItem) in the viewer (issue #635): the toggle textbox's
    // page item carries its key, and ToggleItem -- what a click on it calls -- lays the report out
    // again from the data already read with the toggled rows shown, then hidden again.
    [TestFixture]
    public class ViewerToggleTests
    {
        [OneTimeSetUp]
        public void Init() => RdlEngineConfig.RdlEngineConfigInit();

        private static string Rdl(string dataPath) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>400pt</Width>
  <DataSources><DataSource Name=""DS1""><ConnectionProperties><DataProvider>Json</DataProvider><ConnectString>file={dataPath}</ConnectString></ConnectionProperties></DataSource></DataSources>
  <DataSets><DataSet Name=""Data"">
    <Query><DataSourceName>DS1</DataSourceName><CommandText>columns=K,V;table=root_Rows</CommandText></Query>
    <Fields>
      <Field Name=""K""><DataField>K</DataField></Field>
      <Field Name=""V""><DataField>V</DataField></Field>
    </Fields>
  </DataSet></DataSets>
  <Body><Height>300pt</Height><ReportItems>
    <Table Name=""T"">
      <DataSetName>Data</DataSetName>
      <TableColumns><TableColumn><Width>300pt</Width></TableColumn></TableColumns>
      <TableGroups><TableGroup>
        <Grouping Name=""G""><GroupExpressions><GroupExpression>=CStr(Fields!K.Value)</GroupExpression></GroupExpressions></Grouping>
        <Header><TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Tog""><Value>=""Group "" &amp; CStr(Fields!K.Value)</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows></Header>
      </TableGroup></TableGroups>
      <Details>
        <TableRows><TableRow><Height>14pt</Height><TableCells><TableCell><ReportItems>
          <Textbox Name=""Line""><Value>=""Detail "" &amp; CStr(Fields!V.Value)</Value></Textbox>
        </ReportItems></TableCell></TableCells></TableRow></TableRows>
        <Visibility><Hidden>true</Hidden><ToggleItem>Tog</ToggleItem></Visibility>
      </Details>
    </Table>
  </ReportItems></Body>
  <PageHeight>800pt</PageHeight><PageWidth>500pt</PageWidth>
</Report>";

        [Test]
        public async Task ClickingAToggle_ShowsItsRows_AndClickingAgainHidesThem()
        {
            string dir = Path.Combine(Path.GetTempPath(), "toggle-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string dataPath = Path.Combine(dir, "data.json");
                await File.WriteAllTextAsync(dataPath,
                    @"{ ""Rows"": [ { ""K"": ""alpha"", ""V"": ""one"" }, { ""K"": ""alpha"", ""V"": ""two"" }, { ""K"": ""beta"", ""V"": ""three"" } ] }");
                string rdlPath = Path.Combine(dir, "toggle.rdl");
                await File.WriteAllTextAsync(rdlPath, Rdl(dataPath));

                using var viewer = new RdlViewer { ShowWaitDialog = false };
                await viewer.SetSourceFile(new System.Uri(rdlPath));
                await viewer.EnsureRendered();

                Assert.That(await viewer.Find("Detail one"), Is.Null, "the detail rows start hidden");
                PageItem alpha = await viewer.Find("Group alpha");
                Assert.That(alpha, Is.Not.Null);
                Assert.That(alpha.ToggleKey, Is.Not.Null, "the group header is a toggle");
                Assert.That(alpha.ToggleExpanded, Is.False);

                await viewer.ToggleItem(alpha.ToggleKey);
                Assert.That(await viewer.Find("Detail one"), Is.Not.Null);
                Assert.That(await viewer.Find("Detail two"), Is.Not.Null);
                Assert.That(await viewer.Find("Detail three"), Is.Null, "only the clicked group expands");
                Assert.That((await viewer.Find("Group alpha")).ToggleExpanded, Is.True);

                await viewer.ToggleItem(alpha.ToggleKey);
                Assert.That(await viewer.Find("Detail one"), Is.Null);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
