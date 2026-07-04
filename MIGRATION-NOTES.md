# WinForms → Majorsilence.Forms migration notes

Playbook and gotchas from **D1** (`Majorsilence.WinformUtils` → `Majorsilence.WinformUtils.Forms`),
the toolchain-proving exercise for the rest of Track D (D2: RdlViewer, D3: RdlReader, D4-D6:
RdlDesign/ReportDesigner/RdlMapFile). Read this before starting each of those.

## Toolchain

- `Majorsilence.Forms` **is published to nuget.org**, currently at `26.0.0`
  (`Majorsilence.Forms`, `.Avalonia`, `.Headless`, `.Uno`, `.Drawing.Common`, `.Telerik` all
  confirmed live). Add package versions to the root `Directory.Packages.props` under a
  `<!-- Majorsilence.Forms migration track (Track D) -->` comment as you need new backend
  packages (only `Majorsilence.Forms`, `Majorsilence.Forms.Avalonia`, and
  `Majorsilence.Forms.Headless` are there so far).
- The migrator lives in the sibling repo: `../Modern.Forms/tools/Majorsilence.Forms.Migrator`.
  Build it once (`dotnet build -c Release`) and invoke the DLL directly:
  ```
  dotnet ../Modern.Forms/tools/Majorsilence.Forms.Migrator/bin/Release/net10.0/majorsilence-migrate.dll \
    <ProjectDir>/<Project>.csproj \
    -o <ProjectDir>.Forms \
    --backend avalonia --package-version 26.0.0 \
    --report <ProjectDir>.Forms/migration-report.md
  ```
  **Always pass `--package-version 26.0.0` explicitly.** Both of the tool's own defaults (`1.0.4`
  in code, `0.3.0` in `--help` text) are stale and don't match what's actually published.
- **Dry-run first** (`--dry-run --diff`) to read the exact diff before committing to a real run.
- **The migrator only writes files it changes.** If a source file needs zero *textual*
  substitutions (see "implicit usings" below for why that undersells it), it's left out of the
  output tree entirely — copy every other file (`.resx`, unrelated `.cs`, etc.) over by hand.
- **When outputting to a separate directory** (`-o`, the parallel-tree strategy this whole track
  uses), the migrator can't safely edit the shared root `Directory.Packages.props` — that file is
  outside the output tree by design. It emits a warning telling you to add the `PackageVersion`
  entries yourself. Do that as part of every migration, not just D1.
- The migrated `.csproj`'s `TargetFrameworks` needs a manual pass after the tool runs: it
  preserves whatever TFMs the original had (just dropping the `-windows` suffix), including
  `net48` if present — but **`Majorsilence.Forms` itself only targets `net8.0;net10.0`**, so any
  `net48` entry left in a migrated project's TFM list will fail to restore. Drop it explicitly.

## The single biggest real gap: implicit global usings

`UseWindowsForms=true` makes the SDK auto-inject a `GlobalUsings.g.cs` with
`global using System.Drawing;` and `global using System.Windows.Forms;` (confirmed by inspecting
`obj/**/GlobalUsings.g.cs` for the original project) **on top of** the standard
`ImplicitUsings=enable` set (`System`, `System.Collections.Generic`, `System.IO`, `System.Linq`,
`System.Threading.Tasks` — those survive migration fine since `ImplicitUsings=enable` isn't
removed).

The migrator's text engine only rewrites **explicitly-qualified** references
(`System.Windows.Forms.Foo` → `Majorsilence.Forms.Foo`). A file that uses `Form`, `Application`,
`Color`, `Point`, etc. **unqualified**, relying entirely on the implicit global using, is
therefore invisible to the tool: no textual match, no rewrite, and — worse — the file won't even
show up as "changed" in the migration report, so it's easy to assume it needed no work at all.
Once `UseWindowsForms` is stripped from the `.csproj`, that implicit using disappears and the file
stops compiling.

**Action for every future migration:** after running the tool, grep the *original* project for
files with no `using System.Windows.Forms;`/`using System.Drawing;` at the top that still use
unqualified WinForms/Drawing types (`Form`, `Control`, `Application`, `Color`, `Point`, `Size`,
etc.). Add `using System.Drawing;` (real BCL primitives — `Color`/`Point`/`Size`/`Rectangle` stay
as-is, they're never rewritten to `Majorsilence.Forms.Drawing`) and `using Majorsilence.Forms;`
(for `Form`, `Application`, and all the control types) to those files by hand. For D1 this was 2
of the project's 4 source files (`WaitForm.cs`, `FormExtensions.cs`) — don't assume "0 warnings
from the tool" means "nothing to check."

Consider trying `--engine roslyn` (symbol-accurate, needs a loadable project) for D4/D5/D6 given
their size — it may not have this blind spot since it resolves symbols rather than matching text.
Not verified in D1; worth a quick trial before starting D4.

## The other big one: `Form` is not a `Control`

This is an actual API design difference in Majorsilence.Forms, not a migrator limitation — no
tool would have caught it, and no future migration on a project that uses `Form` for anything
beyond the most basic "show a window" will avoid running into it.

In classic `System.Windows.Forms`: `Form : ContainerControl : ScrollableControl : Control`. A
`Form` *is* a `Control` — you can pass one anywhere a `Control` or `ContainerControl` is expected,
call `Width`/`Height`/`PointToScreen`/`Refresh()` on it, subscribe to `SizeChanged`/`Move`, etc.

In Majorsilence.Forms: `Form : WindowBase : Component`. `Control`/`ContainerControl`/`Panel`/
`UserControl` are a **separate** hierarchy, also rooted at `Component`. A `Form` and a
`ContainerControl` share no inheritance relationship — code like
`if (control is Form) { ... }` where `control` is statically typed `ContainerControl` won't just
misbehave, it **won't compile** (`CS8121: An expression of type 'ContainerControl' cannot be
handled by a pattern of type 'Form'`).

Concrete consequences hit in D1, and what to reach for instead:

| Classic WinForms (on `Form`) | Majorsilence.Forms equivalent | Notes |
|---|---|---|
| `Width` / `Height` (int) | `Size.Width` / `Size.Height` | `Size` is `get`+`set` on `Form` (a `new` override hiding `WindowBase`'s get-only `Size`) |
| `Location` (settable) | `Location` | Also a settable `new` override on `Form`; screen-relative already, no `PointToScreen` needed |
| `Bounds` (settable) | `Bounds` | Settable on `Form`; the cleanest way to reposition+resize in one call |
| `SizeChanged` / `Move` events | *(none)* | No equivalent events exist. If you need to track a moving/resizing owner, poll `Bounds` on an existing timer tick rather than trying to subscribe to something that isn't there. |
| `HandleCreated` event | `Shown` event, or `IsHandleCreated` property | `Shown` fires once and was the closest semantic match for "safe to start doing work now" in D1; `IsHandleCreated` exists if you need a poll-able check instead of an event |
| `DoubleBuffered` | *(none, and not needed)* | Avalonia-backed rendering doesn't need this WinForms-specific GDI+ hint; just delete the assignment |
| `Refresh()` | `Invalidate()` | `Invalidate()` marks dirty for the next paint; there's no immediate/synchronous repaint call |
| `parent.PointToScreen(...)` where `parent` might be a `Form` | Branch on type first | `PointToScreen` is a `Control` member `Form` doesn't have. If code needs to compute a `Form`-or-`ContainerControl`'s screen bounds generically, use a `Func<Rectangle>` delegate supplied by the caller (who knows which concrete type they have) rather than trying to accept a common base type — there isn't one below `Component`. |
| `IsDisposed` | *(none on `Form`/`WindowBase`)* | No disposed-state property to pre-check; rely on `try { ... } catch (ObjectDisposedException) { }` around whatever call would actually fail, the same as D1's timer callback does. |

**Action for D2+:** grep the target project for `is Form` / `as Form` patterns and for any method
that accepts `ContainerControl` (or `Control`) but is also invoked with a `Form` argument
somewhere. Both are guaranteed compile breaks, not warnings. RdlViewer (D2) is the "crown jewel"
per the plan specifically because of its GDI+ paint/print/zoom code — expect this table to grow
during that item, and expect it to be a bigger lift than D1's few call sites.

## Testing pattern

`Majorsilence.Forms.Headless` (also on nuget.org at `26.0.0`) is a real dependency-free backend —
no windowing toolkit, no UI-thread affinity — built exactly for this. Select it once per test
assembly via a `[ModuleInitializer]`, mirroring `Majorsilence.Forms`'s own test suite:

```csharp
using System.Runtime.CompilerServices;
using Majorsilence.Forms.Backends;
using Majorsilence.Forms.Headless;

internal static class TestBackend
{
    [ModuleInitializer]
    internal static void Initialize() => Platform.Backend = new HeadlessPlatformBackend();
}
```

Then instantiate real `Form`/`Control`-derived types in ordinary NUnit tests, no mocking needed.
D1's `Majorsilence.WinformUtils.Forms.Tests` (5 tests) exercises the actual `ShowWaiter`/
`HideWaiter` extension methods this way, including `Application.OpenForms` count assertions —
genuine behavior verification, not just "it constructs without throwing."

## D2: RdlViewer — the GDI+ paint/print gauntlet

RdlViewer.Forms compiles clean (0 errors, 0 warnings, both TFMs) and passes a real headless
render test (`RdlViewer.Forms.Tests`, 3 fixture templates including one with a chart) rendering
through the actual `PageDrawing.Draw` code path — not just "the control constructs." This section
is the concrete gap/fix catalogue for D3 (RdlReader also uses `PageDrawing`-adjacent code) and any
future GDI+-heavy migration.

### The Color-type bridge: RdlEngine's DRAWINGCOMPAT vs Majorsilence.Forms

The single largest error category (dozens of call sites). Root cause: `Directory.Build.props`
activates `DRAWINGCOMPAT` for **any** project built on a non-Windows OS — including `RdlEngine`
itself, a dependency, when it's pulled into a project (like `RdlViewer.Forms`) that now builds on
Linux. Under DRAWINGCOMPAT, RdlEngine's own `Style`/`StyleInfo` properties (`si.BColorLeft`,
`si.BackgroundColor`, `si.Color`, `si.GetFontFamily()`, `PageCurve.Points`, etc.) are typed against
`Majorsilence.Drawing.*` (the SkiaSharp-backed compat layer in `Majorsilence.Drawing.Common`), not
`System.Drawing.*`. This never surfaced before because RdlViewer (original) only ever targeted
`net48;net8.0-windows;net10.0-windows` — RdlEngine was always built non-DRAWINGCOMPAT for it, so
these properties were always real `System.Drawing.Color`/`PointF`/`FontFamily` with zero friction.

Majorsilence.Forms's own drawing types (`Pen`, `Brush`, `Font`, `Graphics.DrawLine`, etc.) are
built against **real `System.Drawing.Color`/`PointF`** (confirmed: `Majorsilence.Forms/Drawing/
Pen.cs` has `using System.Drawing;`), so there's a genuine 2-way bridge needed:
`Majorsilence.Drawing.Color` (RdlEngine, DRAWINGCOMPAT) → `System.Drawing.Color` (Majorsilence.Forms).

`Majorsilence.Drawing.Common/Color.cs` **does** define `public static implicit operator
System.Drawing.Color(Color p)` — but it's wrapped in `#if !DRAWINGCOMPAT`, i.e. it only exists in
builds where DRAWINGCOMPAT is *off*. Since it's always on here, the operator is compiled out and
every call site needs an explicit bridge. Fix: one helper,
```csharp
private static System.Drawing.Color ToSysColor(Majorsilence.Drawing.Color c) =>
    System.Drawing.Color.FromArgb(c.ToArgb());
```
(`ToArgb()` exists unconditionally, outside the `#if`), applied at each `si.BColorXxx` /
`si.BackgroundColor` / `si.Color` call site. Same pattern for `PointF` (`PageCurve.Points` is
`Majorsilence.Drawing.PointF[]` — convert element-by-element via `.X`/`.Y`) and `FontFamily`
(`si.GetFontFamily()` is `Majorsilence.Drawing.FontFamily` — use its `.Name` string against
Majorsilence.Forms's `Font(string, float, FontStyle)` overload instead of the `Font(FontFamily,
float, FontStyle)` one, since that overload wants `Majorsilence.Forms.Drawing.FontFamily`, a third,
unrelated type).

**Action for D3+:** grep the target file's Style/StyleInfo/PageItem property reads for anything
color/point/font-typed coming from RdlEngine, and wrap with the same bridge before passing into a
Majorsilence.Forms drawing call. This is mechanical once you know the pattern.

### Graphics-method signature differences (not gaps — just different overloads)

Several GDI+ calls exist in Majorsilence.Forms but with a different signature shape than
`System.Drawing.Graphics`. All fixed by adjusting the call site, no functionality lost:

| GDI+ call | Majorsilence.Forms reality | Fix |
|---|---|---|
| `DrawLine(Pen, float, float, float, float)` | Only `(Pen, Point, Point)` and `(Pen, PointF, PointF)` exist | Wrap coordinates in `new PointF(x, y)` |
| `DrawPie(Pen, RectangleF, float, float)` | Only takes `Rectangle` (int-based) | `Rectangle.Round(r)` |
| `new LinearGradientBrush(rect, c1, c2, LinearGradientMode)` | 4th param is a raw `float angleDegrees`, no `LinearGradientMode` enum at all | Map enum → angle: Horizontal=0, Vertical=90, ForwardDiagonal=45, BackwardDiagonal=135 |
| `new HatchBrush(style, Color, Color)` | Same shape, just needs the Color bridge above | `ToSysColor(...)` on both color args |
| `Region.GetRegionData()` (used to clone a clip region before mutating) | Doesn't exist | `Region.Clone()` does the same job directly |
| `using (Pen p = ...)` / `p.Dispose()` | `Pen` doesn't implement `IDisposable` at all (nothing to release — thin SkiaSharp wrapper) | Delete the `using`/`Dispose()`, keep a plain block |
| `DrawCurve(Pen, PointF[], int offset, int numSegments, float tension)` | Only `(Pen, PointF[])` / `(Pen, Point[])` — no offset/segment-count/tension params | Call the 2-arg overload; curves with non-default `Offset`/`Tension` lose that shaping (documented fidelity loss, curves are a rare RDL construct) |
| `MouseEventArgs.Delta` (was `int`) | `Point` (tracks horizontal+vertical wheel deltas) | Use `.Y` for the classic vertical-wheel comparison |
| event `+=` with named delegates (`PaintEventHandler`, `MouseEventHandler`, `ScrollEventHandler`, `KeyEventHandler`) | Majorsilence.Forms events are `EventHandler<TEventArgs>` generically; those named delegate *types* don't exist (there's no `LayoutEventHandler`/`PrintPageEventHandler` at all) | Drop the `new XxxEventHandler(...)` wrapper, assign the method group directly (`Scroll += this.OnHScroll;`) — compiler infers the right delegate either way |
| `new ToolStripButton(text, null, handler)` | Two constructor overloads (`Image?` and `SKBitmap?`) both accept `null`, ambiguous | Cast: `(Image)null` |
| `TextBox.PreferredHeight` | Doesn't exist (no auto-size convenience prop) | Hardcode a sensible single-line height (e.g. `22`) |

### Real gaps: features with no equivalent at all

These needed an actual design decision, not just a signature fix:

- **`ControlPaint.DrawReversibleFrame`/`FrameStyle`** (rubber-band drag-select visual feedback):
  no equivalent — it's classic XOR-mode direct-to-screen GDI+, incompatible with a
  SkiaSharp/compositing renderer (most modern UI frameworks can't do XOR drawing at all).
  **Fix:** made `RubberBand(...)` a no-op. The selection *logic* (`CreateSelectionList`,
  hit-testing) is separate and still works correctly on mouse-up — only the live rectangle while
  dragging is gone. Real fix would track the drag rect as state and paint it through the normal
  buffered Paint cycle; not done here.
- **`StringFormat.SetMeasurableCharacterRanges` / `Graphics.MeasureCharacterRanges` / `Graphics.
  FillRegion`** (search-term highlighting): no equivalent — this was RdlViewer's mechanism for
  computing exact per-substring highlight regions across wrapped multi-line text. **Fix:**
  approximate with `MeasureString` — measure the width of the text before each match and the
  match itself, draw a highlight rectangle at that X offset. Accurate for single-line text (the
  common case); a highlight that straddles a line-wrap in a multi-line textbox won't split
  correctly (documented fidelity loss).
- **Text justification** (`GraphicsExtended.DrawStringJustified`): RdlEngine's own helper for this
  is typed against `Majorsilence.Drawing`/`System.Drawing.Graphics` (RdlEngine's own DRAWINGCOMPAT
  types) — a third Graphics type unrelated to `Majorsilence.Forms.Graphics`, so it can't be reused
  at all. **Fix:** wrote a real replacement (`DrawStringJustified` in `PageDrawing.cs`) that word-
  wraps manually via `MeasureString` and stretches inter-word gaps per line — genuinely justifies
  text, doesn't fake it, using only confirmed-available APIs.
- **`PageTextHtml.Build(Draw.Graphics g)`** (HTML textbox layout): also wants RdlEngine's own
  `Majorsilence.Drawing.Graphics`, not `Majorsilence.Forms.Graphics` — but only for *measurement*
  during layout, not final drawing. **Fix:** create a throwaway `Majorsilence.Drawing.Bitmap(1,1)`
  + `Majorsilence.Drawing.Graphics.FromImage(...)` just for that call (mirrors the pattern
  RdlEngine's own `RenderTif.cs` uses internally), then draw for real afterward via the normal `g`.
- **`EncryptionProvider.Prompt.ShowDialog`** (passkey-entry dialog for encrypted RDL files): that
  class is compiled only under `#if WINDOWS || NET48` (a raw `System.Windows.Forms.Form`, never
  migrated) — invisible to a plain `net8.0`/`net10.0` build. **Fix:** wrote a small local
  replacement using `Majorsilence.Forms.Form`/`Label`/`TextBox`/`Button` directly in
  `RdlViewer.cs`, using the default `CenterScreen` `StartPosition` instead of `Prompt`'s manual
  `Screen.FromControl`/`WorkingArea` centering math (that API surface wasn't worth chasing down).

### Printing: full redesign, not a port

This was the expected big one. `Majorsilence.Forms.Printing.PrintPageEventArgs.Graphics` is
`SkiaGraphics` — a completely separate, much narrower type from `Majorsilence.Forms.Graphics`
(no inheritance relationship, missing `PageUnit`/`Transform`/most overloads `PageDrawing.Draw`
needs). `PageSettings.PrintableArea` doesn't exist either. Porting the screen-paint code to drive
printing through this API would need a large parallel rendering path.

Bigger picture: Majorsilence.Forms's own `PrintDocument` **doesn't talk to an OS print spooler at
all** — it always renders straight to a PDF file via `SKDocument.CreatePdf`, and `PrintDialog`/
`PrintPreviewDialog`/`PageSetupDialog` are no-op stubs with no real UI (confirmed:
`PrintDialog.AllowSelection` doesn't even exist as a property).

**Decision:** removed `Print(PrintDocument)`/`_Print`/`PrintPage` entirely. "Printing" now means
`SaveAs(path, OutputPresentationType.PDF)` — the exact same mature `RunRenderPdf` pipeline every
other part of Majorsilence Reporting already uses (full RDL fidelity, not a re-derivation of
PageDrawing's screen-paint code) — handing the file to the OS's own PDF viewer to actually print.
`ViewerToolstrip.PrintClicked` now goes straight to a `SaveFileDialog` instead of pretending to
show a print dialog that wouldn't render anything anyway. Real OS print-spooler integration
(printer selection, native page-range dialog, duplex) is explicitly not implemented — consistent
with the plan's own "printing may no-op initially" allowance, just more useful than a true no-op.

### Migrator loose ends

- The migrator doesn't copy binary embedded resources (`.png` icons etc.) that aren't part of the
  "changed source files" set — same blind spot as the zero-textual-change `.resx`/`.cs` files
  D1 already flagged. Copy `Resources/*.png` by hand same as everything else.
- `.sln` registration: `RdlViewer.Forms`/`RdlViewer.Forms.Tests` weren't added to
  `MajorsilenceReporting.sln` as part of the migrator run — `dotnet sln add` by hand, same as D1.

## Package/versioning conventions used (carry forward)

- Package ID gets a `.Forms` suffix: `Majorsilence.WinformUtils.Forms` (folder matches: new
  parallel directory `Majorsilence.WinformUtils.Forms/`, original `Majorsilence.WinformUtils/`
  untouched).
- `<VersionSuffix>preview</VersionSuffix>` until D8's parity sign-off flips it.
- `Description` explicitly says what it's a preview successor to and why (cross-platform vs.
  Windows-only), so anyone browsing NuGet understands the relationship without reading this file.
