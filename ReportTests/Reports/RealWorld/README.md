# Real-world RDL reports (test fixtures)

Reports authored with Microsoft's own tools (SSRS Report Designer and Report Builder), in the
RDL 2008, 2010 and 2016 schemas. The tests render each one through the RDL 2008+ normaliser and
through the engine's native Tablix support and compare the pages (see `RealWorldReportTests.cs`).

They are **test data only**: they are not compiled into, or shipped with, any package.

All of them are vendored **unmodified**, byte for byte as they are upstream. `SHA256SUMS`
lists each file's checksum, and `.gitattributes` keeps git from changing their line endings.
They query SQL Server system tables; the tests do not connect to a database but supply rows
for each dataset's fields themselves, and register the Roslyn compiler for their VB `<Code>`.
A report using something the engine does not support yet (for example `DataSets("x").CommandText`,
`Globals!ReportServerUrl`, `DateTime.Now` or a shared dataset) is reported as ignored, with the
reason, rather than failing; it starts being compared as soon as the engine runs it.

## Licences

Both sources are under the MIT License, which permits copying and redistribution provided the
copyright notice and the permission notice are included in all copies or substantial portions.
Each source's licence is kept, unchanged, beside its reports, and both are listed in the
repository's `THIRD_PARTY_NOTICES.md`.

| Folder | Source | Commit | Copyright | Licence |
|---|---|---|---|---|
| `MicrosoftSqlServerSamples/` | <https://github.com/microsoft/sql-server-samples> | `8fe3fd0c9a825fef3ae6d601e97537b612d7b214` | Copyright (c) Microsoft Corporation | MIT, `MicrosoftSqlServerSamples/LICENSE.txt` (upstream `license.txt`) |
| `ReportingServicesExamples/` | <https://github.com/sql-server-projects/reporting-services-examples> | `550b279064482d21b9b71ffd7120e89124ea9e94` | Copyright (c) 2019 Anthony Duguid | MIT, `ReportingServicesExamples/LICENSE.txt` (upstream `LICENSE`) |

## Files

### `MicrosoftSqlServerSamples/` (from `samples/features/` of microsoft/sql-server-samples)

| File here | Upstream path |
|---|---|
| `epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyDashboard.rdl` | `samples/features/epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyDashboard.rdl` |
| `epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyDetails.rdl` | `samples/features/epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyDetails.rdl` |
| `epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationDetails.rdl` | `samples/features/epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationDetails.rdl` |
| `epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationErrorDetails.rdl` | `samples/features/epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationErrorDetails.rdl` |
| `epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationErrors.rdl` | `samples/features/epm-framework/3.0/2Reporting/PolicyReports/PolicyReports/PolicyEvaluationErrors.rdl` |
| `epm-framework/4.0/2Reporting/PolicyReports/PolicyDashboard.rdl` | `samples/features/epm-framework/4.0/2Reporting/PolicyReports/PolicyDashboard.rdl` |
| `epm-framework/5.0/2Reporting/PolicyReports/PolicyDashboard.rdl` | `samples/features/epm-framework/5.0/2Reporting/PolicyReports/PolicyDashboard.rdl` |
| `epm-framework/5.0/2Reporting/PolicyReports/PolicyDashboardFiltered.rdl` | `samples/features/epm-framework/5.0/2Reporting/PolicyReports/PolicyDashboardFiltered.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Active Sessions.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Active Sessions.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Configuration.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Configuration.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Configure Instance.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Configure Instance.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Execution Statistics.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Execution Statistics.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Extended Events.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Extended Events.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Packages.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Packages.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Resource Usage.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Resource Usage.rdl` |
| `machine-learning-services/ssms-custom-reports/ML Services - Script.rdl` | `samples/features/machine-learning-services/ssms-custom-reports/ML Services - Script.rdl` |
| `r-services/ssms-custom-reports/R Services - Active Sessions.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Active Sessions.rdl` |
| `r-services/ssms-custom-reports/R Services - Configuration.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Configuration.rdl` |
| `r-services/ssms-custom-reports/R Services - Configure Instance.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Configure Instance.rdl` |
| `r-services/ssms-custom-reports/R Services - Execution Statistics.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Execution Statistics.rdl` |
| `r-services/ssms-custom-reports/R Services - Extended Events.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Extended Events.rdl` |
| `r-services/ssms-custom-reports/R Services - Packages.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Packages.rdl` |
| `r-services/ssms-custom-reports/R Services - Resource Usage.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Resource Usage.rdl` |
| `r-services/ssms-custom-reports/R Services - Script.rdl` | `samples/features/r-services/ssms-custom-reports/R Services - Script.rdl` |
| `r-services/telco-customer-churn-v1/ReportBuilder/telcoChurn-reportBuilder.rdl` | `samples/features/r-services/telco-customer-churn-v1/ReportBuilder/telcoChurn-reportBuilder.rdl` |

Not included: the `PolicyDetails`, `PolicyEvaluationDetails`, `PolicyEvaluationErrorDetails`
and `PolicyEvaluationErrors` reports of `epm-framework/4.0` and `epm-framework/5.0`. Those
eight files are truncated upstream (cut off mid-element and padded with zero bytes), so they are
not well-formed XML. The `epm-framework/3.0` versions of the same reports are included.

### `ReportingServicesExamples/` (all of sql-server-projects/reporting-services-examples's reports)

| File here | Upstream path |
|---|---|
| `ServerReports/Activity Monitor.rdl` | `ServerReports/Activity Monitor.rdl` |
| `ServerReports/Data Sources.rdl` | `ServerReports/Data Sources.rdl` |
| `ServerReports/Database Dictionary.rdl` | `ServerReports/Database Dictionary.rdl` |
| `ServerReports/Execution Log.rdl` | `ServerReports/Execution Log.rdl` |
| `ServerReports/Heatmap Calendar.rdl` | `ServerReports/Heatmap Calendar.rdl` |
| `ServerReports/Integration Detail.rdl` | `ServerReports/Integration Detail.rdl` |
| `ServerReports/Integrations.rdl` | `ServerReports/Integrations.rdl` |
| `ServerReports/Job Search.rdl` | `ServerReports/Job Search.rdl` |
| `ServerReports/Permissions.rdl` | `ServerReports/Permissions.rdl` |
| `ServerReports/Report Favorites.rdl` | `ServerReports/Report Favorites.rdl` |
| `ServerReports/Report List.rdl` | `ServerReports/Report List.rdl` |
| `ServerReports/Scheduled Jobs.rdl` | `ServerReports/Scheduled Jobs.rdl` |
| `ServerReports/Subscriptions.rdl` | `ServerReports/Subscriptions.rdl` |

## Updating

Fetch the files from the commit given above (or a newer one, updating the table), keep them
unmodified, regenerate `SHA256SUMS` with `sha256sum` from this folder, and keep each source's
licence file beside its reports.
