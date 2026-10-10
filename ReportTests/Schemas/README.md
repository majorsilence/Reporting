# Microsoft RDL schemas (not checked in)

`Rdl2008ExporterTests.ExportedRegions_ValidateAgainstTheMicrosoft2008Schema` checks that what the
designer writes for an RDLC report (List, Table and Matrix saved as Tablix) is valid against
Microsoft's own schema. The test is skipped unless it can find the XSD, because Microsoft publishes
the schemas "as is" and we do not redistribute them.

## Where to download them

Each version has a landing page that links to `ReportDefinition.xsd`:

| Version | Landing page | XSD |
|---|---|---|
| RDL 2005 | https://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition/ | `.../2005/01/reportdefinition/ReportDefinition.xsd` |
| RDL 2008 | https://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition/ | `.../2008/01/reportdefinition/ReportDefinition.xsd` |
| RDL 2010 | https://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition/ | `.../2010/01/reportdefinition/ReportDefinition.xsd` |
| RDL 2016 | https://learn.microsoft.com/en-us/openspecs/sql_server_protocols/ms-rdl/52ce3983-2bfc-4e72-9359-42aaf5fe4509 | not served by schemas.microsoft.com (404); extract it from the spec page, see below |

The schemas also ship with SQL Server (the Extras folder of the install media) and are served by
any running Reporting Services instance at `https://<server>/reportserver/reportdefinition.xsd`.

The 2005, 2008 and 2010 XSD URLs were confirmed to download; 2003/10 was not checked.

## RDL 2016

Microsoft removed `ReportDefinition.xsd` for 2016 from schemas.microsoft.com, but the MS-RDL
specification page embeds the whole schema. `extract-rdl2016-xsd.py` pulls it out (standard library
only) and removes one stray `xmlns` declaration in the spec text that otherwise makes the schema
unloadable:

```bash
python3 -I ReportTests/Schemas/extract-rdl2016-xsd.py ReportTests/Schemas/ReportDefinition-2016.xsd
```

## Running the schema test

```bash
curl -L -o /tmp/ReportDefinition-2008.xsd \
  https://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition/ReportDefinition.xsd
RDL_XSD_2008=/tmp/ReportDefinition-2008.xsd dotnet test ReportTests/ReportTests.csproj --filter "FullyQualifiedName~ValidateAgainst"
```

`RDL_XSD_2010` and `RDL_XSD_2016` enable the other cases. Cases without their variable set are skipped.

The test reads the path from `RDL_XSD_2008` only. `*.xsd` files saved in this folder are git-ignored, so it is a convenient place to keep them.

Note that the 2010 and 2016 schemas place `Body` inside `ReportSections/ReportSection`, unlike 2008, so a
hand-built 2010 test document needs that wrapper to validate.

## CI

`.github/workflows/linux.yml` downloads the 2008 and 2010 XSDs and extracts the 2016 one before the test step, so
the schema tests run on every build. The download is best effort: if it fails the tests are skipped.
