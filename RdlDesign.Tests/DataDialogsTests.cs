// Copyright (C) 2026 Peter Gill <peter@majorsilence.com>
// Licensed under the Apache License, Version 2.0.

using NUnit.Framework;

namespace Majorsilence.Reporting.RdlDesign.Tests
{
    // Issue #469/#470: RdlUserControl had no way to open the Data Sources / Dataset dialogs at all,
    // unlike the stand-alone RdlDesigner MDI shell. These tests cover the new ShowDataSourcesDialog /
    // GetDataSetNames / ShowDataSetDialog methods without driving a real modal ShowDialog(), following
    // this test suite's existing convention (see EmbeddedDesignerTests).
    [TestFixture]
    public class DataDialogsTests
    {
        private static string Report(string dataSetsXml) => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition"">
  <Width>4in</Width>
  <PageWidth>8.5in</PageWidth>
  <PageHeight>11in</PageHeight>
  {dataSetsXml}
  <Body>
    <Height>1in</Height>
  </Body>
</Report>";

        private const string TwoDataSets = @"<DataSets>
    <DataSet Name=""Orders"">
      <Fields><Field Name=""OrderID""><DataField>OrderID</DataField></Field></Fields>
    </DataSet>
    <DataSet Name=""Customers"">
      <Fields><Field Name=""CustomerID""><DataField>CustomerID</DataField></Field></Fields>
    </DataSet>
  </DataSets>";

        [Test]
        public void GetDataSetNames_ReturnsNamesInDocumentOrder()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report(TwoDataSets);

            Assert.That(designer.GetDataSetNames(), Is.EqualTo(new[] { "Orders", "Customers" }));
        }

        [Test]
        public void GetDataSetNames_NoDataSets_ReturnsEmpty()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report("");

            Assert.That(designer.GetDataSetNames(), Is.Empty);
        }

        [Test]
        public void ShowDataSourcesDialog_Handled_SkipsNativeDialogAndDoesNotModify()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report("");

            bool raised = false;
            designer.DataSourcesPropertiesRequested += (s, e) =>
            {
                raised = true;
                e.Handled = true;
            };

            designer.ShowDataSourcesDialog();

            Assert.That(raised, Is.True);
            Assert.That(designer.Modified, Is.False, "the native dialog never ran, so nothing changed");
        }

        [Test]
        public void ShowDataSetDialog_ForExistingDataSet_PassesItsNameToTheEvent()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report(TwoDataSets);

            string seenName = null;
            designer.DataSetPropertiesRequested += (s, e) =>
            {
                seenName = e.DataSetName;
                e.Handled = true;
            };

            designer.ShowDataSetDialog("Customers");

            Assert.That(seenName, Is.EqualTo("Customers"));
            Assert.That(designer.Modified, Is.False);
        }

        [Test]
        public void ShowDataSetDialog_NoNameGiven_RaisesEventForANewDataSet()
        {
            using var designer = new RdlUserControl();
            designer.SourceRdl = Report("");

            string seenName = null;
            designer.DataSetPropertiesRequested += (s, e) =>
            {
                seenName = e.DataSetName;
                e.Handled = true;
            };

            designer.ShowDataSetDialog();

            // Matches RdlDesigner's own "Data > Datasets > New..." menu item's label, so a host that
            // compares DataSetName against its own list of existing datasets sees the same "not one of
            // these" signal the built-in handler does.
            Assert.That(seenName, Is.EqualTo(Resources.Strings.RdlDesigner_menuData_Popup_New));
            Assert.That(designer.Modified, Is.False);
        }
    }
}
