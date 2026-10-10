#!/usr/bin/env python3
"""Extract the RDL 2016/01 XSD from Microsoft's MS-RDL specification page.

Microsoft no longer serves ReportDefinition.xsd for 2016 (the schemas.microsoft.com URL is 404), but
the spec page embeds the full schema in a <pre> block.

    python3 -I extract-rdl2016-xsd.py ReportDefinition-2016.xsd
    RDL_XSD_2016=$PWD/ReportDefinition-2016.xsd dotnet test ../ReportTests.csproj --filter ValidateAgainst

The output is Microsoft's schema with one local fix, described below. Do not commit it.
"""
import html
import re
import sys
import urllib.request
import xml.dom.minidom

URL = ("https://learn.microsoft.com/en-us/openspecs/sql_server_protocols/ms-rdl/"
       "52ce3983-2bfc-4e72-9359-42aaf5fe4509")
METADATA_NS = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition/authoringmetadata"

out = sys.argv[1] if len(sys.argv) > 1 else "ReportDefinition-2016.xsd"
page = urllib.request.urlopen(URL, timeout=60).read().decode("utf-8", "replace")
block = re.search(r"<pre>(.*?)</pre>", page, re.S).group(1)
xsd = html.unescape(re.sub(r"<[^>]+>", "", block)).replace("\xa0", " ").strip() + "\n"

# The spec's AuthoringMetadata element re-declares the default namespace as the authoringmetadata
# one, so its type name resolves to a namespace with no schema, although the type is defined in the
# main one a few lines below. Removing the stray declaration makes the schema loadable.
xsd = xsd.replace(f' xmlns="{METADATA_NS}"', "", 1)

xml.dom.minidom.parseString(xsd.encode("utf-8"))  # fail early if the page layout changed
with open(out, "w", encoding="utf-8") as f:
    f.write(xsd)
print(f"wrote {out} ({len(xsd)} characters)")
