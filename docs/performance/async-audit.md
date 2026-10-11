# Async audit: removing sync-over-async, and what .NET 11 changes

Blocking on async work (`Task.Run(...).GetAwaiter().GetResult()`, `.Result`, `.Wait()`) costs a thread-pool hop plus a blocked pool thread per call, can deadlock under a synchronization context, and starves the pool when many reports render at once (a web server). Before this work the engine and data providers had **22 such sites**; the heaviest were inside sort comparers, so a sort of n rows made O(n log n) hops.

## Done (all with identical output; new tests pin the behaviour and also pass on the previous engine)
| Where | Was | Now |
|---|---|---|
| `Rows` (table/list/Top N filter sort) | `Compare` blocked per comparison, twice per sort key | `SortAsync`: each row's keys evaluated once, awaited, then the same `Array.Sort` and row-number tie-break |
| `GroupEntryCompare` (table groups) | blocked per comparison | `GroupEntrySort.SortAsync`, same approach (sets each entry as its group's current group while evaluating) |
| `RecursiveCompare` (recursive groups) | `List.BinarySearch` + blocking comparer | async binary search with `List.BinarySearch`'s exact probing |
| `MatrixEntryComparer` | blocked per comparison | `MatrixEntrySort.SortAsync`; `Matrix.RunBuildSet` awaits `PrepareSortedDataAsync` once after building, so the many synchronous matrix methods only read sorted lists |
| `ChartBase` constructor | blocked on the tooltip expressions | `InitAsync()` awaited from `Chart.RunChartBuild` |
| `DynamicExpression` constructor | blocked on parsing | `CreateAsync` factory |
| `Report.ClientLanguage` | getter ran the Language expression through `Task.Run(...).GetResult()` | `GetClientLanguageAsync()` / `SetClientLanguage()`; the embedded Code's synchronous `User.Language` reads a value resolved at the start of each run |
| `ReportItem.DataElementOutput` | getter blocked on `IsConstant()` | `Textbox.FinalPass` awaits it once |
| `MhtWebFile.ToString()` | started a download and blocked on it | no longer downloads (every caller awaits `Download()` first) |
| Designer `SelectedStyle`, save and exit menus | blocked | async (`GetSelectedStyleAsync`, `FileSaveAsync`, `OkToCloseAsync`); `bSuppressChange` no longer left set by an early return |
| `PdfSigner` timestamp request | blocked on `PostAsync` | `HttpClient.Send` (net5+) |
| `JsonDataReader`, `TxtDataReader`, `MultipleStreamReader` | wrapped async stream opening / `ReadLineAsync` in `Task.Run(...).GetResult()` | synchronous I/O where the contract is synchronous (`IDataReader`, `TextReader.ReadLine`): files were already sync streams, HTTP uses `HttpClient.Send` / `ReadAsStream` |

Measured effect: WorldFacts 50 -> 44 ms and 9.5 -> 8.0 MB per render; thread-pool work items per WorldFacts render fell from ~3,700 to ~15 (0 for the tiny and matrix reports); `-- profile` prints the count.

## Public API: `UserReportParameter`
`DefaultValue`, `DisplayValues`, `DataValues`, `Value` and `Values` blocked the calling thread. They are marked `[Obsolete]` (they still work) and the replacements are `GetDefaultValueAsync()`, `GetDisplayValuesAsync()`, `GetDataValuesAsync()`, `GetValueAsync()`, `GetValuesAsync()`, `SetValueAsync()` and `SetValuesAsync()`. Every caller in this repository (Subreport, ASP.NET Core, MCP, the WinForms-style, Avalonia and Gtk3 viewers, RdlDesktop) uses the async methods; `UserReportParameterAsyncTests` covers them.

## `ConfigureAwait(false)`
All awaits in `RdlEngine`, `DataProviders` and `RdlCreator` (~2,200) now use `ConfigureAwait(false)`, applied with the CA2007 code fix, and CA2007 is a warning for those projects in `.editorconfig` so new code is held to it. UI, ASP.NET Core and command-line projects are deliberately not covered: a UI project needs to resume on its UI thread, and ASP.NET Core has no synchronization context. `SynchronizationContextTests` renders reports under a counting context and requires zero posts back to it (WorldFacts posted 6 times before the pass). The one event the engine raises, `SubreportDataRetrieval`, used to arrive on the UI thread by accident of the captured context; the WinForms-style and Avalonia viewers now marshal it back to the UI thread explicitly.

## Remaining blocking calls (what is left, and why)
| Where | Why it stays |
|---|---|
| `UserReportParameter` `DefaultValue` / `DisplayValues` / `DataValues` / `Value` / `Values`, `Report.ClientLanguage`, `DesignCtl` / `RdlEditPreview` / `MDIChild` `SelectedStyle`, `MDIChild.FileSave` / `OkToClose` | Public compatibility members, all `[Obsolete]` with an async replacement; nothing in this repository calls them any more (the tests that pin their behaviour use a pragma) |
| *(none)* | The report designer and map designer closing handlers are async now (cancel, ask, close again; see below) |
| `RdlNative` C entry points (`rdl_report_render_file`, `..._buffer`) | The C ABI is synchronous by definition: the Python / PHP / Ruby / Rust caller is waiting for the return value, on a thread with no `SynchronizationContext` |
| `RdlReader.Main` silent print | Process entry point, no UI loop or context yet, and the thread must stay the STA thread that runs the application |
| `PdfSigner` on netstandard2.0 | `HttpClient.Send` needs .NET 5+; net6.0 and later use it |
| Benchmark harness (`Program.cs`, `ConcurrencyBenchmarks`) | Deliberate: the concurrency benchmark measures render CPU scaling on blocked threads |

## Behaviour changes to be aware of
Things that are observably different, in the order they matter to an application:
1. **Engine continuations no longer resume on the caller's context** (`ConfigureAwait(false)`). Code the engine runs after an await may be on a thread-pool thread: expressions, the report's embedded Code, custom report items, and the engine's one event, `SubreportDataRetrieval`. The WinForms-style and Avalonia viewers marshal that event back to the UI thread; an application that subscribes to `Report.SubreportDataRetrieval` directly now receives it on whatever thread is running the report, and must marshal to its UI itself if it touches controls.
2. **Sort expressions are evaluated once per row, not once per comparison.** Table, list, group, matrix and Top N/Bottom N sorts evaluate each key once, then sort. The order produced is the same for any deterministic expression. An expression with side effects or a changing value (`Rnd()`, `Now()`, a custom function with state) used to be re-evaluated on every comparison, which could not give a consistent order anyway.
3. **`MhtWebFile.ToString()` no longer downloads.** An undownloaded file reads as an empty string. Nothing in the engine reaches this (it awaits `Download()` first); the class is internal.
4. **Obsolete members still block** (`UserReportParameter` value members, `Report.ClientLanguage`, the designer's `SelectedStyle`, `MDIChild.FileSave()` / `OkToClose()`). They behave as before; new code should use the async methods.
5. **Report designer: closing.** `FormClosing` stays synchronous, so a modified report that is closed cancels that close, asks, and closes itself again once answered. The main window, when closed with modified reports open, asks about all of them once and then closes everything (previously each report asked as it closed). A report answered "No" (discard) is no longer asked a second time on exit. The map designer works the same way for its one document.
6. **`Report.ClientLanguage` is resolved at the start of `RunGetData` / `RunRender`** for the report's embedded Code (`User.Language`); a change to the report's Language expression's inputs between those calls and a later code read is not seen until the next run.
7. **ASP.NET Core:** `ReportHelper.GetParameterHtml` and the page builder are async; the HTML produced is the same.
8. **PDF** (earlier round, already merged): zlib level 6 instead of 9, so the compressed bytes differ while inflating to the same content.

## Findings along the way (existing behaviour, not changed)
- Matrix rows sorted on an aggregate such as `Sum(...)` keep first-met order: the sort expression is evaluated against the entry's first row. Plain sort expressions work. Pinned in `AsyncSortTests`.
- `MultipleStreamReader`'s path-list parser mishandles quoted paths (the separator after a closing quote starts a new entry, and the last quoted file is queued twice).
- `Textbox.HTMLEXPR` is greedy (`<expr>.+</expr>`), so two `<expr>` blocks in one html text are treated as one expression.

## .NET 11: what to know before leaning on it
.NET 11 adds **runtime async**: the JIT, not the C# compiler, turns `async` methods into suspendable code, so the per-method state-machine class and intermediate `Task` objects are no longer needed for eligible methods; locals can stay on the stack until a real suspension. Sources: [Performance Improvements in .NET 11](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-11/), [What's new in .NET 11 runtime](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/runtime). It is still a preview feature behind the compiler switch `runtime-async=on` (the docs say a `net11.0` project no longer needs `EnablePreviewFeatures`; check the current page before relying on either statement), continuations can skip `ExecutionContext` capture when there is nothing to restore, and suspension points are merged to cut code size. Gains are workload dependent; the claim is parity with .NET 10 at worst and fewer allocations for chains of small async methods.

What that means for this engine:
- Our hottest shape is exactly that: `IExpr.Evaluate*` is a deep chain of tiny `async Task<object>` methods (631 declarations), and expression evaluation was 5-9% of allocation per render (`Task<object>`, `Task<string>`, `UnwrapPromise`). Runtime async should remove most of that **without code changes**, once the library has a `net11.0` target and builds with the feature on. Plan: add `net11.0` to `RdlEngine`/`DataProviders` when it ships (November 2026), turn the switch on for that target, and re-run `-- gate` and the benchmarks; the allocation gate will show the effect directly.
- Do not convert the `IExpr` surface to `ValueTask` by hand now: it is ~630 signatures and the runtime does the equivalent. (A hand-written `ValueTask` fast path is still the right tool for the few *non-async-method* cases that complete synchronously and return a cached value.)
- It does nothing for blocking. A `.GetResult()` still blocks a thread, so the work above stands.
- Things that make runtime async less effective, so avoid adding them: storing or passing `Task` objects around (`Task.WhenAll`, caching tasks), `Task.Run` wrappers, and `async void`. Straight-line `await` chains are the case it optimises.
- `ConfigureAwait(false)` matters for UI hosts (WinForms/Majorsilence.Forms/Avalonia viewers resume every await on the UI thread) and not for ASP.NET Core (no synchronization context). Library code that is host-agnostic should use it, enforced with analyzer CA2007; that is a mechanical change worth doing together with the `net11.0` target, and it is independent of this audit.

## Rules going forward
1. No `Task.Run(...).GetResult()` / `.Result` / `.Wait()` in engine or provider code. Where a contract is synchronous (`IDataReader`, `TextReader`, comparers), use a genuinely synchronous implementation (sync I/O, `HttpClient.Send`) or restructure so the async work happens before the synchronous consumer runs (decorate-then-sort, prepare-then-read).
2. Constructors and property getters must not await: use `InitAsync` / `CreateAsync` / `GetXAsync()`.
3. Comparers that need async data are replaced by "evaluate keys once, then sort on the keys".
4. A thread-pool work-item count in the concurrency benchmark (`Completed Work Items`) is the quick regression signal for sync-over-async creeping back.
