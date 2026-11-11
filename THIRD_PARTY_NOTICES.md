# Third-Party Notices

This product includes third-party components. Their licenses and copyright
notices are listed below.

---

## Bundled Fonts

The following fonts are embedded in the `Majorsilence.Forms.Drawing.Common`
assembly, which this project consumes as a NuGet package, as cross-platform
fallback fonts. They are used automatically when a font requested by a report is
not installed on the host system.

Full copyright notices and the complete OFL 1.1 license text ship with that
package, in `src/Majorsilence.Forms.Drawing.Common/Fonts/` of
<https://github.com/majorsilence/Majorsilence.Forms>.

All bundled fonts are distributed under the **SIL Open Font License, Version 1.1**.

| Font | Copyright | Replaces |
|------|-----------|---------|
| Liberation Sans | Copyright (c) 2010 Google Corporation; Copyright (c) 2012 Red Hat, Inc. | Arial |
| Liberation Serif | Copyright (c) 2010 Google Corporation; Copyright (c) 2012 Red Hat, Inc. | Times New Roman |
| Liberation Mono | Copyright (c) 2010 Google Corporation; Copyright (c) 2012 Red Hat, Inc. | Courier New |
| Carlito | Copyright (c) 2010-2013 tyPoland Lukasz Dziedzic | Calibri |
| Caladea | Copyright (c) 2012 Carolina Giovagnoli, Andres Torresi, Huerta Tipografia | Cambria |
| Noto Sans | Copyright (c) 2010-2020 Google Inc. / Google LLC | Unicode fallback |

### Test fonts

`ReportTests/Fonts/NotoSansDevanagari-Regular.ttf` (Noto Sans Devanagari, Copyright 2015
Google Inc.) is used only by the tests, to check Devanagari shaping in PDF output; it is not
shipped in any package. It is under the SIL Open Font License 1.1, whose full text is beside it
in `ReportTests/Fonts/OFL-1.1.txt`.

### SIL Open Font License 1.1 — Summary

The OFL allows the fonts to be freely used, studied, modified, and redistributed,
including bundled with or embedded in software, provided that:

- The fonts (original or modified) are not sold by themselves.
- Each copy includes the copyright notice and this license.
- Modified versions do not use the Reserved Font Names listed above.

The full license text is at
`src/Majorsilence.Forms.Drawing.Common/Fonts/OFL-1.1.txt` in
<https://github.com/majorsilence/Majorsilence.Forms>.

---

## Test Reports

`ReportTests/Reports/RealWorld/` holds real-world RDL reports used only by the tests, to check
how RDL 2008+ reports made with Microsoft's tools render; they are not shipped in any package.
They are unmodified copies, and each source's full licence text is beside its reports.
`ReportTests/Reports/RealWorld/README.md` lists every file with its upstream path and commit.

| Folder | Source | Copyright | Licence |
|--------|--------|-----------|---------|
| `MicrosoftSqlServerSamples/` | <https://github.com/microsoft/sql-server-samples> | Copyright (c) Microsoft Corporation | MIT (`MicrosoftSqlServerSamples/LICENSE.txt`) |
| `ReportingServicesExamples/` | <https://github.com/sql-server-projects/reporting-services-examples> | Copyright (c) 2019 Anthony Duguid | MIT (`ReportingServicesExamples/LICENSE.txt`) |
