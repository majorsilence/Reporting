# Json, Xml and Dictionary data provider reads

Reads every cell of every row, as the report engine does before rendering
(`Majorsilence.Reporting.Benchmarks/DataProviderReadBenchmark.cs`). 10 columns per row.
Intel Core i5-8350U, .NET 10.0.112, BenchmarkDotNet ShortRun. "Allocated" is everything allocated during
one read, not what was alive at once; peak memory is below.

    dotnet run -c Release -f net10.0 -- --filter '*DataProviderRead*'

## Time and allocation, 100,000 rows

| Method              | Before          | After           |
|---------------------|----------------:|----------------:|
| Json                | 736 ms, 281 MB  | 547 ms, 137 MB  |
| JsonAsync           | 716 ms, 281 MB  | 540 ms, 137 MB  |
| JsonThreeColumns    | 683 ms, 281 MB  | 534 ms, 137 MB  |
| Xml                 | 947 ms, 1038 MB | 222 ms, 51 MB   |
| XmlWithColumnXPaths | 611 ms, 375 MB  | 158 ms, 12 MB   |
| Dictionary          | 21.1 ms         | 21.5 ms (same)  |

At 1,000 rows Json allocates 2.8 MB before and 1.3 MB after, Xml 10.6 MB before and 0.5 MB after.

## Peak working set, 250,000 rows (60 MB json, 74 MB xml)

`-- peak gen <dir> <rows>` then `-- peak read json|xml|jsonasync|xmlasync <dir>`, one process per read, keeping every
row the way `Query.GetData` does.

| Read | Before                     | After                     |
|------|---------------------------:|--------------------------:|
| Json | 2660 ms, 719 MB peak       | 2134 ms, 340 MB peak      |
| Xml  | 3252 ms, 297 MB peak       | 1200 ms, 206 MB peak      |

The engine keeps every row it reads, so most of the Xml peak after the change is the rows themselves.

## What changed

- **Json**: the document is parsed from its UTF-8 bytes, one top-level array element at a time, instead of
  being turned into a string and a whole `JsonDocument`. Rows are arrays indexed by column, not a dictionary each.
- **Xml**: a row path of plain element names (`/rows/row`) with plain child/attribute columns is read with an
  `XmlReader`, one row at a time. Everything else (other XPath, namespace prefixes, http urls, no rows)
  uses the `XPathDocument` code as before, which now compiles each column expression once instead of parsing it for
  every cell.
- **Dictionary**: unchanged in speed. `GetFieldType` is cached per column; it used to rescan the rows on every call,
  which only costs anything when a column starts with many nulls.
