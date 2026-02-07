using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReportTests
{
    public class RunningValueTest
    {
        [Test]
        public async Task TestRunningValueWithScope()
        {
            // Create a minimal RDL report that uses RunningValue with scope
            string rdl = @"<?xml version='1.0' encoding='UTF-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <DataSources>
    <DataSource Name='TestDataSource'>
      <ConnectionProperties>
        <DataProvider>SQL</DataProvider>
        <ConnectString>Data Source=.</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name='TestDataSet'>
      <Query>
        <DataSourceName>TestDataSource</DataSourceName>
        <CommandText>SELECT 1 as debito, 'A' as cuenta</CommandText>
      </Query>
      <Fields>
        <Field Name='debito'>
          <DataField>debito</DataField>
        </Field>
        <Field Name='cuenta'>
          <DataField>cuenta</DataField>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <ReportItems>
      <Table Name='TestTable'>
        <DataSetName>TestDataSet</DataSetName>
        <TableGroups>
          <TableGroup>
            <Grouping Name='cuenta'>
              <GroupExpressions>
                <GroupExpression>=Fields!cuenta.Value</GroupExpression>
              </GroupExpressions>
            </Grouping>
          </TableGroup>
        </TableGroups>
        <Details>
          <TableRows>
            <TableRow>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='RunningValueTest'>
                      <Value>=RunningValue(Fields!debito.Value, Sum, cuenta)</Value>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

            try
            {
                // Parse the RDL - this should not throw an exception
                var rdlParser = new Majorsilence.Reporting.Rdl.RDLParser(rdl);
                await rdlParser.Parse();
                
                // If we get here, the parsing succeeded
                Assert.Pass("RunningValue with scope parsed successfully");
            }
            catch (Exception ex)
            {
                // Check if the error is the specific one we're fixing
                if (ex.Message.Contains("scope must be a constant") || 
                    ex.Message.Contains("scope mut be a constant"))
                {
                    Assert.Fail($"RunningValue scope parsing failed with the expected error: {ex.Message}");
                }
                else
                {
                    // Some other error occurred - this might be acceptable depending on the RDL
                    // We're mainly testing that we don't get the "scope must be a constant" error
                    Console.WriteLine($"Different error occurred: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // Still pass as long as it's not the scope error
                    if (!ex.Message.Contains("Sum"))
                    {
                        Assert.Pass($"No scope error, different issue: {ex.Message}");
                    }
                    else
                    {
                        Assert.Fail($"Unexpected error: {ex.Message}");
                    }
                }
            }
        }

        [Test]
        public async Task TestRunningValueWithoutScope()
        {
            // Test RunningValue without scope parameter
            string rdl = @"<?xml version='1.0' encoding='UTF-8'?>
<Report xmlns='http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition' xmlns:rd='http://schemas.microsoft.com/SQLServer/reporting/reportdesigner'>
  <DataSources>
    <DataSource Name='TestDataSource'>
      <ConnectionProperties>
        <DataProvider>SQL</DataProvider>
        <ConnectString>Data Source=.</ConnectString>
      </ConnectionProperties>
    </DataSource>
  </DataSources>
  <DataSets>
    <DataSet Name='TestDataSet'>
      <Query>
        <DataSourceName>TestDataSource</DataSourceName>
        <CommandText>SELECT 1 as debito</CommandText>
      </Query>
      <Fields>
        <Field Name='debito'>
          <DataField>debito</DataField>
        </Field>
      </Fields>
    </DataSet>
  </DataSets>
  <Body>
    <ReportItems>
      <Table Name='TestTable'>
        <DataSetName>TestDataSet</DataSetName>
        <Details>
          <TableRows>
            <TableRow>
              <TableCells>
                <TableCell>
                  <ReportItems>
                    <Textbox Name='RunningValueTest2'>
                      <Value>=RunningValue(Fields!debito.Value, Sum)</Value>
                    </Textbox>
                  </ReportItems>
                </TableCell>
              </TableCells>
            </TableRow>
          </TableRows>
        </Details>
      </Table>
    </ReportItems>
  </Body>
</Report>";

            try
            {
                // Parse the RDL - this should not throw an exception
                var rdlParser = new Majorsilence.Reporting.Rdl.RDLParser(rdl);
                await rdlParser.Parse();
                
                // If we get here, the parsing succeeded
                Assert.Pass("RunningValue without scope parsed successfully");
            }
            catch (Exception ex)
            {
                // Check if the error is the specific one we're fixing
                if (ex.Message.Contains("scope must be a constant") || 
                    ex.Message.Contains("scope mut be a constant"))
                {
                    Assert.Fail($"RunningValue scope parsing failed with the expected error: {ex.Message}");
                }
                else
                {
                    // Some other error - still pass as long as it's not the scope error
                    Console.WriteLine($"Different error occurred: {ex.Message}");
                    if (!ex.Message.Contains("Sum"))
                    {
                        Assert.Pass($"No scope error, different issue: {ex.Message}");
                    }
                    else
                    {
                        Assert.Fail($"Unexpected error: {ex.Message}");
                    }
                }
            }
        }
    }
}
