using System.Collections.Generic;
using System.Text;

namespace ReportTests.Containers.Utils
{
    /// <summary>
    /// Builds the minimal RDL needed to prove a backend round-trips through the engine: one
    /// data source, one SQL-backed dataset, one table.
    ///
    /// Modelled on the inline RDL in ReportTests/SqlDataSetRegressionTests.cs. The
    /// &lt;NoRows&gt; sentinel is the important part - it is what makes "the query returned
    /// nothing" assertable, since a dataset that silently yields zero rows still renders a
    /// perfectly valid report (issue #308).
    /// </summary>
    internal static class BackendRdl
    {
        internal const string NoRowsSentinel = "NO_ROWS_MESSAGE";

        /// <param name="providerName">The &lt;DataProvider&gt; name, as registered in RdlEngineConfig.</param>
        /// <param name="connectString">Provider-specific connection string.</param>
        /// <param name="commandText">The SELECT to run. Placeholder syntax is the backend's own.</param>
        /// <param name="queryParameters">QueryParameter name to value expression, in declaration order.</param>
        internal static string Build(
            string providerName,
            string connectString,
            string commandText,
            IEnumerable<KeyValuePair<string, string>> queryParameters = null)
        {
            var qp = new StringBuilder();
            if (queryParameters != null)
            {
                var items = new StringBuilder();
                foreach (var p in queryParameters)
                {
                    items.Append($@"
          <QueryParameter Name=""{p.Key}""><Value>{p.Value}</Value></QueryParameter>");
                }

                if (items.Length > 0)
                    qp.Append($@"
        <QueryParameters>{items}
        </QueryParameters>");
            }

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition""
        xmlns:rd=""http://schemas.microsoft.com/SQLServer/reporting/reportdesigner"">
  <PageHeight>11in</PageHeight>
  <PageWidth>8.5in</PageWidth>
  <Width>7.5in</Width>
  <TopMargin>.25in</TopMargin>
  <LeftMargin>.25in</LeftMargin>
  <RightMargin>.25in</RightMargin>
  <BottomMargin>.25in</BottomMargin>
  <DataSources>
    <DataSource Name=""DS1"">
      <ConnectionProperties>
        <DataProvider>{providerName}</DataProvider>
        <ConnectString>{Escape(connectString)}</ConnectString>
        <IntegratedSecurity>false</IntegratedSecurity>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name=""Sales"">
      <Query>
        <DataSourceName>DS1</DataSourceName>
        <CommandText>{Escape(commandText)}</CommandText>{qp}
      </Query>
      <Fields>
        <Field Name=""Product"">
          <DataField>Product</DataField>
          <rd:TypeName>System.String</rd:TypeName>
        </Field>
        <Field Name=""Amount"">
          <DataField>Amount</DataField>
          <rd:TypeName>System.Int32</rd:TypeName>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <Height>2in</Height>
    <ReportItems>
      <Table Name=""SalesTable"">
        <DataSetName>Sales</DataSetName>
        <NoRows>{NoRowsSentinel}</NoRows>
        <Width>7in</Width>
        <TableColumns>
          <TableColumn><Width>4in</Width></TableColumn>
          <TableColumn><Width>3in</Width></TableColumn>
        </TableColumns>
        <Details>
          <TableRows>
            <TableRow>
              <Height>12pt</Height>
              <TableCells>
                <TableCell><ReportItems><Textbox Name=""Product""><Value>=Fields!Product.Value</Value></Textbox></ReportItems></TableCell>
                <TableCell><ReportItems><Textbox Name=""Amount""><Value>=Fields!Amount.Value</Value></Textbox></ReportItems></TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";
        }

        private static string Escape(string value)
            => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
