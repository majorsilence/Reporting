# Inking templates

RDLs used by `ReportTests/InkingRegressionTests.cs` for pixel-diff ("inking") regression
testing: each one is rendered to a raster image and compared against a committed baseline
PNG with a tolerance threshold, so a layout or drawing regression shows up as a failing test
instead of only a numeric assertion.

`invoice1-a4.rdl`, `receipt1-a4.rdl`, `purchase-order1-a4.rdl` and `training-certificate1-a4.rdl`
are adapted from the [Majorsilence.RaaS](https://github.com/majorsilence/Majorsilence.RaaS)
template library (same author). Each `*-data.json` is that template's sample data; the RDL's
`ConnectString` was renamed from the shared `data.json`/`receipt1.json` filename to a
per-template `<name>-data.json` so all four can live side by side.
