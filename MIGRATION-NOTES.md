# WinForms → Majorsilence.Forms migration notes

Playbook and gotchas from **D1** (`Majorsilence.WinformUtils` → `Majorsilence.WinformUtils.Forms`),
the toolchain-proving exercise for the rest of Track D (D2: RdlViewer, D3: RdlReader, D4-D6:
RdlDesign/ReportDesigner/RdlMapFile). Read this before starting each of those.

## Modern.Forms source vs. published package (important as of D3)

The published `Majorsilence.Forms` `26.0.0` on nuget.org lags the checked-out `../Modern.Forms`
source. D2 hit and worked around several gaps (`Pen` not `IDisposable`, `Form` missing `Width`/
`Height`, `TextBox` missing `PreferredHeight`, `LinearGradientBrush` taking only a raw angle, no
tension-aware `DrawCurve`) that turned out to be easy, safe additions to make directly in
`../Modern.Forms/src/Majorsilence.Forms` (NOT `Majorsilence.Forms.Drawing.Common` — that's a
separate, unreferenced sibling package that happens to reuse the same `Majorsilence.Forms.Drawing`
namespace and already has several of these fixed, which is a red herring: `Majorsilence.Forms`
itself has its own independent `Drawing/*.cs`/`Graphics.cs`, with no `ProjectReference` between
the two projects, and that's what the published NuGet package is actually built from).

**Fixed directly in `../Modern.Forms/src/Majorsilence.Forms`** (all confirmed against the real
2507-test `Majorsilence.Forms.Tests` suite, still 100% green after the changes):
- `Drawing/Pen.cs`: `Pen` now implements `IDisposable` (no-op `Dispose()` — `CreatePaint()` already
  handed the caller a fresh, caller-owned `SKPaint` each time, so there was nothing to release; this
  is purely for `using (var p = new Pen(...))` source compatibility with ported WinForms code).
- `Form.cs`: added `int Width`/`Height` properties (thin wrappers over the existing `Size`) — this
  was D1's own "biggest gotcha" table entry, so worth fixing at the root instead of re-deriving
  `Func<Rectangle>` workarounds in every future migration.
- `TextBox.cs`: added `PreferredHeight` (single-line-text-plus-padding height via `TextMeasurer`).
- `Drawing/Brush.cs`: added a `LinearGradientBrush(RectangleF, Color, Color,
  Drawing2D.LinearGradientMode)` overload (the enum already existed in `Drawing2D.cs`; only the
  brush constructor accepting it was missing) mapping Horizontal/Vertical/ForwardDiagonal/
  BackwardDiagonal to 0/90/45/135 degrees internally.
- `Graphics.cs`: added `DrawLine(Pen, float, float, float, float)`, and a real tension-aware
  `DrawCurve(Pen, PointF[], int offset, int numberOfSegments, float tension)` (Catmull-Rom spline
  via Hermite basis functions, 24 steps/segment) — the existing `DrawCurve` overloads just draw
  straight lines between points, not an actual curve.

**Version bumped to `26.0.1`** in `Modern.Forms/Directory.Build.props`, packed to
`Reporting/.local-nuget-feed/` via `dotnet pack -c Release -o .local-nuget-feed` for each of
`Majorsilence.Forms`/`.Avalonia`/`.Headless`, and wired in via `Reporting/nuget.config` (adds the
local feed as a second package source alongside nuget.org) plus a version bump in
`Directory.Packages.props`. **When you change `../Modern.Forms` source, re-pack all three and
re-restore** — the local feed doesn't auto-detect source changes, it's a snapshot from the last
`dotnet pack`. D2's original PageDrawing.cs workarounds for `LinearGradientMode`/`DrawCurve`
tension were reverted once 26.0.1 was in place — grep for `ToSysColor`/`Pen` usage patterns in
`PageDrawing.cs` if you're unsure whether a given call site still needs a manual workaround or can
use the real API now.

**Action for D4+:** if you hit an API gap that looks like a small, safe, no-architectural-decision
addition (a missing property, a missing overload with an obvious mapping to what exists), consider
fixing it in `../Modern.Forms` directly rather than working around it in Reporting code — verify
against `Majorsilence.Forms.Tests` (`dotnet test ../Modern.Forms/tests/Majorsilence.Forms.Tests`),
bump the patch version, re-pack, re-restore. Reserve in-Reporting workarounds for things that are
genuinely architectural (the printing redesign, XOR-mode rubber-band selection) rather than simple
missing API surface.

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

## D4: RdlDesign — the big one (283 files, 566 → 0 unique compile errors)

By far the largest and most varied of the Track D migrations. Went from 566 unique `error CS*`
lines after the mechanical migration to a clean `dotnet build` (net8.0/net10.0) across roughly
two dozen build/fix/rebuild/commit cycles, each one bumping a local `Majorsilence.Forms` patch
version (26.0.1 → 26.0.12) after verifying the full 2507-test `Majorsilence.Forms.Tests` suite
stayed green. Same triage method as D1-D3: fix the highest-count `CS*` category first, not
file-by-file.

### ScintillaNET → `ScintillaCompat` shim (see `reporting.map.json`)

Mapped the whole `ScintillaNET` namespace to `Majorsilence.Reporting.RdlDesign.Syntax` via the
migrator's `--map`, then defined a shim class literally named `Scintilla` (not `ScintillaCompat`)
in that namespace so the migrator's prefix-rewrite resolves — `RdlDesign.Forms/Syntax/
ScintillaCompat.cs`. It's a `RichTextBox` subclass with real undo/redo/selection/search-in-target;
styling/lexer members are no-ops. Made *real* (actual syntax coloring) is D5's job, not D4's.

### `Form` is not a `Control` — the recurring pattern, one level deeper

D1 already documented that `Form : WindowBase : Component` (not `Control`), so `is Form`/`as Form`
casts on a `Control`-typed value are compile breaks. D4 surfaced the *second-order* version of the
same problem: code that walks a `Control.Parent` chain looking for an ancestor `MDIChild`/
`RdlDesigner` (both Forms) can never find one — the chain can't reach a Form at all, regardless of
cast syntax, because Forms don't participate in the Control.Parent hierarchy.

**Fix, applied in `DesignerUtility.GetConnnectionInfo`, `DataSetRowsCtl.cs`, `PropertyCtl.cs`:**
replace the manual `while (p != null && !(p is RdlDesigner)) { ...; p = p.Parent; }` walk with
`someControl.FindForm()` (closest enclosing Form) and, when a *specific* Form further up an MDI
hierarchy is needed, `.MdiParent` from there. `Control.FindForm()` already existed in
Majorsilence.Forms; nothing needed adding upstream for this one.

### Bitmap/ImageFormat: three unrelated types share a name

This project juggles **three** different `Bitmap`/`Image`/`ImageFormat` types that all resolve
from a bare identifier depending which `using` wins, and none convert to each other publicly:
1. `Majorsilence.Forms.Drawing.*` — the UI framework's SkiaSharp-backed types, used for on-screen
   drawing (`Graphics.DrawImage`, `DrawImageSized`, etc.).
2. `Majorsilence.Drawing.*` (`Majorsilence.Drawing.Common`) — RdlEngine's own SkiaSharp-backed
   DRAWINGCOMPAT types, used by RdlEngine's own APIs like `ICustomReportItem.DrawDesignerImage
   (ref Majorsilence.Drawing.Bitmap)` and `PageImage`'s constructor.
3. `System.Drawing.*` — still referenced directly in a few spots (via `System.Drawing.Common`).

Neither (1) nor (2) exposes a public `SKBitmap` accessor usable from the other's assembly (both
have one, but `internal`). **Fix pattern:** bridge via a PNG round-trip through a `MemoryStream`
(`engineBm.Save(ms, Majorsilence.Drawing.Imaging.ImageFormat.Png)` then
`new Majorsilence.Forms.Drawing.Bitmap(ms)`) rather than trying to expose the internals publicly.
For bare `ImageFormat.Jpeg`-style references that silently resolved to the wrong one of the three,
just fully-qualify at the call site — cheaper than chasing a using-directive fix per file.

### Upstream additions to `../Modern.Forms` this pass required (26.0.1 → 26.0.12)

All verified against the full `Majorsilence.Forms.Tests` suite (2507 tests) before packing. Full
per-patch detail lives in `../Modern.Forms`'s git log (one commit per version bump) — not
duplicated here or in `Directory.Packages.props`'s comment (which itself got trimmed partway
through D4 once the running list got too long to stay readable). Categories, roughly:
- **Form parity additions**: `Width`/`Height`, `AllowDrop`/`DragEnter`/`DragDrop`,
  `MouseDown`/`MouseMove`/`Leave` (real events, forwarded from the internal adapter Control),
  `CausesValidation`, `Focus()`, `Validating` (stub).
- **Control-collection gaps**: `ListBoxItemCollection.Add(object)` (int-returning, matching
  `ObjectCollection.Add`; the inherited `ObservableCollection<T>.Add` is void), a real (not lazy)
  `CheckedListBox.CheckedItems`, a real (not no-op) `CheckedObjectCollection.CopyTo`,
  `DataGridViewComboBoxColumn.Items` as `List<object>` (was `IList`, no `AddRange`),
  `MenuItemCollection`'s string (name-based) indexer, `GridItem` (`PropertyGrid.SelectedGridItem`
  now returns it instead of `object`, still always `null` — D6's problem to make real).
- **Drawing/Graphics overloads**: `DrawLine`/`DrawCurve`/`DrawEllipse`/`DrawImage` float and
  `RectangleF` overloads matching existing sibling overloads' pattern; `LinearGradientBrush(...,
  LinearGradientMode)`; `FontFamily.Families` (real, SkiaSharp-backed); a real `ControlPaint.
  DrawGrid`; `ControlStyle`'s implicit conversion from `DataGridViewCellStyle` (needed because
  `DataGridViewRenderer` already depended on `ControlStyle`'s shape for real header painting, so
  the property couldn't just switch to `DataGridViewCellStyle` outright); `Image.Save
  (ImageCodecInfo, EncoderParameters)`.
- **Printing**: `PrinterSettings.PaperSizes` (fixed list of common ISO/ANSI sizes — no real
  printer driver to query), `PrintDialog.AllowSelection`.
- **Misc**: `Control : IWin32Window`, `IDataObject`'s `DataFormat`-typed overloads (`GetData`/
  `GetDataPresent`/`SetData`, including the 2-arg `autoConvert` forms), `TextBox.VScroll`/
  `HScroll` (real events, `new`-shadowing `Control`'s unrelated bool properties of the same name),
  `TreeViewCancelEventArgs.Node` retyped `TreeNode` (was the base `TreeViewItem`).

### Things intentionally dropped, not fixed

- **`ImageAttributes`/`SetColorKey`** (transparent-color-key image drawing in `SimpleButton.cs`/
  `SimpleToggle.cs`): no Majorsilence.Forms equivalent; would need per-pixel SkiaSharp color
  filtering to replicate. Both controls are already flagged as migration candidates (same
  "very crazy control, need replace it" territory as `ColorPicker.cs`'s existing comment) — drew
  the image directly instead, dropping the color-keying effect as a documented cosmetic loss.
- **`ControlPaint.DrawReversibleFrame`** (rubber-band selection rectangle in `DesignCtl.cs`): same
  GDI+ XOR-mode direct-to-screen gap as D2's `RubberBand` — made the method a documented no-op.
  Selection logic itself is unaffected, only the live-drag visual feedback is gone.
- **`GridItem`-based expression editing** (`CustomReportItemCtl.bExpr_Click`): compiles now (see
  `GridItem` above) but is functionally dead — `PropertyGrid.SelectedGridItem` always returns
  `null`, so this code path would `NullReferenceException` at runtime regardless. Not a D4
  regression; the whole feature depends on PropertyGrid selection tracking Majorsilence.Forms
  doesn't have yet (D6).

## D5: ScintillaCompat made real (expression coloring)

Decision gate from the plan: is per-span text coloring paintable in a day? **Yes** — confirmed by
reading `Majorsilence.Forms.TextMeasurer.CreateTextBlock`'s existing mnemonic-underline code,
which already builds a RichTextKit `TextBlock` from *multiple* styled `AddText` runs (splitting
text into normal/underlined/normal around an access-key character). Multi-run styling was already
proven inside the framework; it just wasn't exposed as a public hook.

**Added `Majorsilence.Forms.TextBox.Colorizer`** (26.0.13): a
`Func<string, IEnumerable<TextSpanStyle>>` property. `TextBoxDocument.GetTextBlock()` builds a
multi-run `TextBlock` from the spans it returns instead of the single-style cached path, filling
gaps between spans with the normal foreground color. `TextSpanStyle` is `(int Start, int Length,
SKColor Color, bool Bold, bool Underline)`. Inherited by `RichTextBox` and therefore `ScintillaCompat`'s
`Scintilla` shim. Malformed output (out-of-range/overlapping spans) is skipped rather than thrown.

**The RdlDesign-side integration needed zero changes to `RdlScriptLexer.cs` or
`ScintillaExprStyle.cs`** — both already drive Scintilla entirely through `StartStyling`/
`SetStyling`/`GetEndStyled`/the `StyleNeeded` event and a `Styles[styleIndex]` color table (set up
once by `ScintillaExprStyle.ConfigureScintillaStyle()`). D4's shim had stubbed those three methods
to no-ops; D5 made them real instead of touching the lexer:
- `StartStyling(pos)`/`SetStyling(len, style)` now record `(start, length, styleIndex)` runs.
- `Scintilla`'s constructor wires `base.Colorizer` to a method that fires `StyleNeeded` for the
  whole document (synchronously triggering `RdlScriptLexer.StyleText` through the existing event,
  which populates the run buffer via the now-real `SetStyling`), then converts each run to a
  `TextSpanStyle` by looking up `Styles[styleIndex].ForeColor`/`.Bold`/`.Underline` — the exact
  indices `ConfigureScintillaStyle` already populates.
- Recoloring is a full-document re-lex on every text change (not Scintilla's real incremental
  restyling model), which is fine at RDL-expression scale.

**SQL and XML stay uncolored, by design, not oversight.** `ScintillaSqlStyle.cs`/
`ScintillaXMLStyle.cs` configure ScintillaNET's **built-in** `Lexer.Sql`/`Lexer.Xml` tokenizers
(`scintilla.Lexer = Lexer.Sql` + `SetKeywords(...)`) — they never call `ConfigureScintillaStyle`
or subscribe `StyleNeeded` at all, so there's no custom lexer to wire through the same mechanism.
`Colorizer` safely returns zero spans for them (no listener means an empty run buffer). Real
support would mean writing a SQL and an XML tokenizer from scratch — legitimately larger, separate
scope, not attempted here.

**Verification:** new `RdlDesign.Forms.Tests` project (Headless backend, same pattern as D2/D3's
test projects), 4 tests exercising the real behavior end to end — not just "it compiles": a valid
`=Fields!Name.Value` expression gets a colored span over the identifier, plain non-expression text
produces zero spans, an unknown field name is styled as an error (red), and a `Scintilla` with no
`ScintillaExprStyle` attached is a safe no-op.

## D6: Designer parity + ReportDesigner shell + RdlMapFile

### Designer round-trip verified end to end, not just compiled

D1-D5 verified "builds clean" and (from D2 onward) "renders a fixture report headlessly." D6 adds
the missing link for RdlDesign.Forms specifically: does the *designer itself* work end to end —
open a report, edit it, save it, and have the result actually render? `RdlDesign.Forms.Tests/
DesignerRoundTripTests.cs` drives exactly that loop on the Headless backend. It doesn't simulate
real mouse-drag report-item placement (would need Majorsilence.Forms' drag-drop machinery driven
interactively — better verified manually, per the plan's own "on Linux desktop, create/save/
preview a report end-to-end" note) — instead it edits the design surface's underlying RDL XML
directly (via `MDIChild.SourceRdl`), which is exactly what a completed drag-drop placement itself
produces, then confirms the design surface retains the edit and the saved result renders through
RdlEngine independently.

Building this one test found two real **runtime** bugs that "0 compile errors" from D4 had no way
to catch:

1. **`TabControl.Controls.Add(TabPage)` didn't register a tab** (Modern.Forms fix, 26.0.14). In
   real WinForms, `TabControl.Controls` and `TabControl.TabPages` are the same collection — ported
   designer-generated `InitializeComponent()` code commonly does `tabControl1.Controls.Add
   (tabPage1)`. Majorsilence.Forms kept them as two separate collections (`TabPages` drives the
   real tab strip), so that call silently added the page as an invisible plain child with no tab
   registered — the very next generated line, an unconditional `SelectedIndex = 0`, then threw
   `ArgumentOutOfRangeException` because the tab strip was still empty. This broke construction of
   *any* migrated Form/UserControl with a `TabControl` whose Designer.cs used `Controls.Add`
   instead of `TabPages.Add` — at least 6 files in RdlDesign.Forms alone use this pattern. Fixed
   upstream via a custom `ControlCollection` override that detects a `TabPage` being added directly
   and redirects to `TabPages.Insert`.
2. **`DesignCtl.ReportSource`'s getter read an uninitialized instance field through a
   confusingly-named type reference.** `RdlDesigner.XmlNewLine` inside `DesignCtl.ReportSource`
   doesn't resolve to the static-looking call it reads as — `RdlDesigner` is `DesignCtl`'s own
   instance field of that name (only ever assigned when hosted inside the full MDI shell, via
   `RdlDesigner.CreateMDIChildAsync`). A `DesignCtl`/`MDIChild` constructed any other way (any
   test, or any future embedding of the design surface outside the MDI shell) hit a
   `NullReferenceException` here — silently swallowed by the getter's own `catch`, surfacing only
   as an unexplained empty `ReportSource` and a `MessageBox` nobody watching a Headless test could
   ever see. Null-guarded with `RdlDesigner?.XmlNewLine`, falling back to Windows-style newlines.

Both bugs pre-date D6 (the field/type-shadowing bug is D4-era migrated code; the TabControl gap is
Modern.Forms's own architecture) but were invisible until something actually *exercised* the
designer's real behavior rather than just checking it compiles — the concrete argument for writing
this kind of test at every migration stage, not just at the end.

### ReportDesigner.Forms: a genuinely trivial shell

Unlike RdlReader/RdlDesign, `ReportDesigner`'s original `Program.cs` (109 lines total in the whole
project) needed **zero logic changes** — single-instance mutex, culture setup, and
`Application.Run(new RdlDesigner(...))` all have exact Majorsilence.Forms equivalents
(`Application.EnableVisualStyles`/`DoEvents`/`Run` all exist with the same shape). Built clean on
the first attempt.

### RdlMapFile.Forms: same catalogue of fixes as D1-D5, no new categories

28 files migrated (13 needed hand-copying — the migrator's usual zero-textual-diff blind spot,
plus binary assets: `App.ico`, four `.gif` toolbar icons). Every fix category was already
catalogued from earlier Track D work — no new gap types surfaced:
- `EventHandler`/`CancelEventHandler`/`ScrollEventHandler`/`LinkLabelLinkClickedEventHandler`
  wrapper mismatches (`new XxxEventHandler(this.Method)` → bare `this.Method`) — bulk-fixed via
  `sed` across four files, following the exact same D1-established pattern.
- `FileDialog.ShowDialog(Form)` async-await threading (`SaveAs()` → `SaveAsAsync()`, bridged
  synchronously for `Save()`'s synchronous callers via `Task.Run(...).GetAwaiter().GetResult()`,
  identical to D4's `MDIChild.FileSave()`).
- Dead Windows-only P/Invoke mousewheel routing (`MapFile.PreFilterMessage`) gutted to a no-op,
  same as D3/D4.
- `MouseEventArgs.Delta` is a `Point` (not `int`) — same fix as D4's `DesignCtl.cs`.
- `ToolStripMenuItem(text, null, handler)` ambiguity — same `(Image)` cast fix as D2/D4.
- `ColorTranslator` ambiguity (`Majorsilence.Forms.Drawing` vs `System.Drawing`, both in scope via
  `using`) — fully-qualified at the one call site, same fix as D4.
- `DoubleBuffered` designer-cruft assignment removed, same as D1.
- `System.Drawing.Design`/`Majorsilence.Forms.Design` using was present in `PropertyBase.cs` but
  actually unused (no `UITypeEditor`/`IWindowsFormsEditorService` reference in this file, unlike
  RdlDesign.Forms) — removed the dead using rather than pulling in D4's `DesignCompat.cs` shim.

### Upstream additions to `../Modern.Forms` this pass required (26.0.14 → 26.0.15)

Verified against the full test suite (2515 tests after 26.0.14's 3 new `TabControl` regression
tests) before each pack:
- **26.0.14**: the `TabControl.Controls.Add(TabPage)` fix described above (a real bug fix, not an
  API-gap addition).
- **26.0.15**: `GridItem` gained `Label`/`Parent`/`GridItems`/`Expanded` (tree-navigation shape,
  for code that walks `PropertyGrid.SelectedGridItem` up to its root or searches children by
  label — still non-functional stubs, since `PropertyGrid` never actually builds a `GridItem` tree;
  making that real is D6-adjacent future work, not attempted here); `ToolStripComboBox` gained
  `DropDownWidth`/`TextChanged`; `FontFamily` gained `GetName(int language)` (always returns
  `Name`, no per-language localized metadata); `GraphicsPath` gained a real `Widen(Pen)`
  (stroke-to-fill via `SKPaint.GetFillPath`, needed so `path.IsVisible(pt)` can hit-test near a
  thin line — a path with zero area never contains any point otherwise).

## D7: CI + packaging + transition messaging

### CI: direct per-project builds, not the full solution, for Linux/macOS

`linux.yml`/`mac.yml` both build the whole repo in one shot via `dotnet build -c Release-DrawingCompat
MajorsilenceReporting.sln`. Investigating why that path felt risky for the new `.Forms` projects
turned up a real, **pre-existing** (not introduced by Track D) quirk in `MajorsilenceReporting.sln`:
`EncryptionProvider`'s `Release-DrawingCompat|Any CPU` solution-config entry has an `ActiveCfg` but
no matching `Build.0` — the same gap exists for the original (Windows-only) `RdlViewer`'s entry too,
git-blame confirms it predates this branch. In practice this had never mattered before, because
nothing that builds under that configuration on Linux/macOS previously needed `EncryptionProvider`
— the original WinForms `RdlViewer`/`RdlReader`/`RdlDesign`/`RdlMapFile` that reference it are
Windows-only TFMs and were never actually built by that CI job in the first place. Track D's new
`.Forms` projects are the first things that (a) build on Linux/macOS *and* (b) reference
`EncryptionProvider`, surfacing the dormant gap for the first time. Locally reproduced: a plain
`dotnet build -c Release-DrawingCompat MajorsilenceReporting.sln` on Linux fails resolving
`EncryptionProvider` from `RdlViewer.Forms`; GitHub Actions' own historical runs are green, so
whatever platform the runner's `dotnet build` resolves to isn't reproduced by a bare local
invocation — the discrepancy wasn't fully run to ground, and hand-editing raw `.sln` GUID
config-platform mappings blind is exactly the kind of "confident but wrong" fix that's worse than
leaving it alone.

**Decision:** rather than depend on that resolution, added a new CI step to `linux.yml`/`mac.yml`
that builds and tests the `.Forms` track directly, bypassing the full-solution path entirely:

```bash
for proj in $(find . -maxdepth 2 -path "*.Forms/*.csproj" -not -path "*.Forms.Tests/*"); do
  dotnet build -c Release "$proj"
done
for proj in $(find . -maxdepth 2 -path "*.Forms.Tests/*.csproj"); do
  dotnet test -c Release "$proj"
done
```

Plain `-c Release` (not `-c Release-DrawingCompat`) is enough — `Directory.Build.props` already
defines `DRAWINGCOMPAT` unconditionally whenever `$(OS) != 'Windows_NT'`, independent of the
configuration name, so Linux/macOS get the SkiaSharp code paths regardless. The `find`-based glob
means every current `.Forms`/`.Forms.Tests` project pair is covered without listing them by name,
and D8's future additions (if any) need no CI edits — matching the plan's own requirement. Verified
locally: all 6 `.Forms` projects and all 4 `.Forms.Tests` projects build/pass cleanly this way.
`windows.yml` needed no equivalent addition — its existing full-solution `build-release.ps1` +
`dotnet test MajorsilenceReporting.sln` steps already cover the `.Forms` track fine on Windows,
where the ambiguity above doesn't reproduce.

### Packaging: `VersionSuffix=preview` already in place from D1-D4

`Majorsilence.WinformUtils.Forms`, `Majorsilence.Reporting.RdlViewer.Forms`, and
`Majorsilence.Reporting.ReportDesigner.Forms` (the three packable libraries) already carry
`<PackageId>`/`<VersionSuffix>preview</VersionSuffix>`/a full `<Description>` from when each was
built (D1/D2/D4) — nothing left to add here. `RdlReader.Forms`, `RdlMapFile.Forms`, and
`ReportDesigner.Forms` (the three executable shells) are `IsPackable=false`, same as their
Windows-only originals — they ship as zips via `build-release.ps1`, not NuGet packages, so
`PackageId`/`VersionSuffix` don't apply to them.

### Transition messaging: README package matrix

Added four `Preview` rows to the root `Readme.md`'s package table (`RdlViewer.Forms`,
`RdlReader.Forms`, `ReportDesigner.Forms`, `RdlMapFile.Forms`), reworded footnote ¹ from "a
cross-platform successor ... is planned" to "... is in preview" (it exists now), and added a new
footnote ³ explaining what "Preview" means here specifically: functionally complete, covered by
headless CI, not yet parity-signed-off (that's D8), not yet on nuget.org, and not yet the
recommended choice over the Windows-only originals until sign-off. Also updated the "Viewer
choices" section and the Apache-2.0-licensed project list (the `.Forms` projects are direct
derivative migrations of already-Apache-2.0 code, not novel new packages, so they belong in that
list alongside their originals rather than the tri-licensed new-package list).

## Package/versioning conventions used (carry forward)

- Package ID gets a `.Forms` suffix: `Majorsilence.WinformUtils.Forms` (folder matches: new
  parallel directory `Majorsilence.WinformUtils.Forms/`, original `Majorsilence.WinformUtils/`
  untouched).
- `<VersionSuffix>preview</VersionSuffix>` until D8's parity sign-off flips it.
- `Description` explicitly says what it's a preview successor to and why (cross-platform vs.
  Windows-only), so anyone browsing NuGet understands the relationship without reading this file.

## D6.x: Runtime-parity punch list (plan for the next session)

Everything below was discovered by actually running `ReportDesigner.Forms` on a Linux desktop and
fixing what broke, one bug at a time. Fixed so far (Majorsilence.Forms 26.0.16–26.0.22, one commit
each in `../Modern.Forms` on `win-compat`, all with regression tests):

1. `TabControl.SelectedIndex = 0` on an empty tab strip crashed at startup (26.0.16).
2. `ComponentResourceManager` never read a normal project's compiled `.resources` binary — every
   `ApplyResources` call was a silent no-op, so no `Dock`/`Size`/`Text` ever applied → totally
   blank window (26.0.17). Reads via `DeserializingResourceReader` now; WinForms-only enum types
   (`DockStyle` etc.) bridge through the embedded `Majorsilence.Forms.WinFormsEnumShims` assembly.
3. `IsMdiContainer`'s MDI client docked over the menu/toolbars/status strip instead of yielding
   (z-order; missing `BringToFront`) (26.0.18).
4. Text in controls shorter than one line's natural height (13px designer-default labels) laid out
   zero lines instead of clipping → invisible label text (26.0.19).
5. Menu dropdowns opened then instantly closed: popup stole activation → parent deactivation
   dismissed it. First attempt (26.0.20, defer flag reset one dispatcher tick) was insufficient
   because the WM delivers focus-loss on its own schedule; real fix (26.0.21) makes popup windows
   non-activating (`ShowActivated=false` on the Avalonia popup host) — how native menus work.
   **Confirmed fixed on a real desktop** (user report after 26.0.21: toolbars visible, no mention
   of menus misbehaving — previously called out explicitly). Former P3 below is resolved; kept as
   a fallback-suspects list only in case it regresses.
6. `ToolStrip.Items` was a facade that never mirrored into the base `MenuBase` collection that
   layout/render/hit-testing consume → both designer toolbars rendered empty (26.0.21). **Confirmed
   fixed on a real desktop** (user screenshot shows both toolbars fully populated).
7. `SplitContainer`/`PictureBox` didn't implement `ISupportInitialize`, which every designer-
   generated `InitializeComponent` casts to (`((ISupportInitialize)(this.splitContainer1))
   .BeginInit()`) — crashed `DialogDatabase` and every other dialog containing either control,
   including plain File > New (26.0.22).

### Remaining gaps, in priority order

**P1 — Toolbar/menu images never load on Linux (buttons currently render text-only).**
The resx entries are typed `System.Drawing.Bitmap`; `System.Drawing.Common` throws
`PlatformNotSupportedException` on all Linux since .NET 7, so `DeserializingResourceReader`'s
per-entry deserialization always fails for them (~74 entries in RdlDesigner.resources alone,
skipped gracefully). Fix in `../Modern.Forms/src/Majorsilence.Forms/ComponentResourceManager.cs`:
recover the raw image bytes without instantiating System.Drawing types, then materialize as
`Majorsilence.Forms.Drawing.Image` (SkiaSharp), the same output type the raw-XML-resx path already
produces (see `BuildImage`). Two implementation options: (a) parse the .resources binary directly —
format is documented in dotnet/runtime `ResourceReader.cs`/`DeserializingResourceReader.cs`
(header → name table → data section; each entry: 7-bit-encoded type index, then for
DeserializingResourceReader v2 user-types a 1-byte `SerializationFormat` + length-prefixed payload;
`ActivatorStream`/`TypeConverterByteArray` payloads for Bitmap/Icon are just the image file bytes);
or (b) reflection into `DeserializingResourceReader` internals (`FindPosForResource`, `_store`) —
acceptable only because `System.Resources.Extensions` is pinned at 8.0.0, but (a) is preferred.
Precedent for (a): `NrbfResourceReader` already hand-parses the NRBF wire format in the same
codebase. Then confirm `TryConvert` bridges `Majorsilence.Forms.Drawing.Image` to
`MenuItem.Image`'s property type, and re-render RdlDesigner headlessly to see icons.

**P2 — `Label.AutoSize` doesn't grow to fit text** ("Fore Colo|" truncation next to the color
pickers). WinForms: an `AutoSize=true` label ignores designer-set `Size` and grows to its text.
Majorsilence.Forms: resx applies both `AutoSize=true` and the stale designed `Size`; nothing
recomputes. Fix in `Label`: when `AutoSize` is true, recompute preferred size from text on
Text/Font/AutoSize change (there's already `GetPreferredSize` machinery; wire it like WinForms'
`CommonProperties`+layout path or simply set bounds from measured text).

**P3 — (resolved; fallback suspects only) if menus ever dismiss again.** In order: (a) clicking a
dropdown *item* — `MenuDropDown` popups chain (`parent_form.Deactivated += Hide`), check nested
dropdown focus behavior; (b) `MenuBase.OnClick`'s release-on-click toggle
(`IsReleaseOnClick && clicked_item == SelectedItem`) firing from a stray duplicated click event;
(c) X11 `override-redirect` — Avalonia maps popups as normal WM windows; if some WM force-focuses
even non-activating windows, consider Avalonia's `Popup`/`PopupRoot` primitives instead of a
`Window` for the popup host.

**P4 — `ToolStripComboBox` inside toolbars** (font family / font size / zoom in `toolStrip1`):
after P1/P2, check they render and drop down; they're `ToolStripItem`-hosted controls, and the
hosting path (item → child Control) may need the same mirroring treatment as ToolStrip items.

**P5 — Continue the real smoke loop:** File → New now constructs `DialogDatabase` without
crashing (verified headlessly); next is actually completing that dialog → design surface →
preview → save. Each step will likely surface the next runtime gap (candidates: `RdlEditPreview`
tab wiring, design-surface paint, the `ScintillaCompat` expression editor). Use the established
loop: reproduce headlessly (`HeadlessRenderer.CapturePng` + control-tree dump — remember bounds
are only valid after a render pass), fix in Modern.Forms with a regression test, bump patch
version, repack to `.local-nuget-feed`, clear the `majorsilence.forms*` nuget cache, rebuild,
re-verify, commit both repos, push `win-compat`.

Note: `ISupportInitialize` was the *only* `System.ComponentModel`/`System.Windows.Forms` interface
cast pattern found across all of RdlDesign.Forms/RdlViewer.Forms/RdlReader.Forms's `*.Designer.cs`
files (grepped explicitly during the 26.0.22 fix), and all four types it targets
(`SplitContainer`, `DataGridView`, `PictureBox`, `NumericUpDown`) now implement it — this specific
class of crash should be fully closed out, not just patched for the one dialog that happened to
crash first.

## D8: Cutover — side-by-side retired, `.Forms` promoted in place (2026-08-22)

The dual-track model this whole document describes (parallel `*.Forms/` folders,
`.Forms`-suffixed/`-preview` NuGet IDs, pending a future parity sign-off) is retired. Per a direct
user decision, D8 turned out not to be a sign-off step — it was a straight promotion:

- Each already-completed `.Forms` project was `git mv`'d over its classic counterpart (classic
  deleted first): `RdlDesign.Forms` → `RdlDesign`, `RdlViewer.Forms` → `RdlViewer`,
  `RdlMapFile.Forms` → `RdlMapFile`, `RdlReader.Forms` → `RdlReader`, `ReportDesigner.Forms` →
  `ReportDesigner`, `Majorsilence.WinformUtils.Forms` → `Majorsilence.WinformUtils`. Same for their
  test projects (`RdlDesign.Forms.Tests` → `RdlDesign.Tests`, etc. — `RdlViewer.Tests` had a real
  classic counterpart to delete first, the other three didn't).
- `PackageId`s and `<VersionSuffix>preview</VersionSuffix>` were dropped back to the original,
  unsuffixed names — these packages now ARE `Majorsilence.Reporting.RdlViewer`,
  `Majorsilence.Reporting.ReportDesigner`, `Majorsilence.WinformUtils`, not previews of them.
  `RdlMapFile`/`RdlReader`/`ReportDesigner` (the `.exe` shells) were never packed, nothing to change
  there beyond the folder rename.
- `MajorsilenceReporting.slnx`'s `/Windows/` folder lost its six classic entries (kept
  `EncryptionProvider` and `LibRdlWpfViewer`, which are unaffected); the `/UI/` folder's five
  `.Forms` paths dropped the suffix; the four flat `.Forms.Tests` entries were renamed.
- `build-release.ps1`: the three now-cross-platform projects (`ReportDesigner`, `RdlReader`,
  `RdlMapFile`) no longer have a `net10.0-windows` build output — the script's `$pTargetFramework`
  variable was folded into the already-existing `$pTargetFrameworkGeneric` (`net10.0`), including
  the output folder/zip names, since it's now unused otherwise.

**Real bugs this cutover unmasked** (pre-existing in the `.Forms` code, just never hit by a
full-solution build before — each project had only ever been built/tested in isolation against its
own dependency chain, not against the now-current state of `RdlEngine`/main after the separate
`main` rebase that replaced the in-repo `Majorsilence.Drawing.Common` project with the
`Majorsilence.Forms.Drawing.Common` NuGet package):
- Several `Majorsilence.Drawing.*` references (the deleted in-repo namespace) needed updating to
  `Majorsilence.Forms.Drawing.*` (the package's namespace) in `RdlViewer/PageDrawing.cs` and
  `RdlDesign/DesignXmlDraw.cs` — bitmaps, graphics, image formats, and a `Color` bridge function
  that's now just an identity conversion (`RdlEngine`'s `StyleInfo` colors are plain
  `System.Drawing.Color` even under `DRAWINGCOMPAT`; the package has no `Color` type of its own).
- `MouseEventArgs.Delta` in Majorsilence.Forms is `int` (matching classic WinForms), not a `Point`
  — `e.Delta.Y`/`B.Delta.Y` (a few call sites in `RdlMapFile/DesignXmlDraw.cs`, `RdlDesign/DesignCtl.cs`,
  `RdlViewer/RdlViewer.cs`) needed to drop the `.Y`.
- ~20 menu `_Click` handlers in `RdlReader.cs`/`RdlDesigner.cs` were typed
  `(object sender, Majorsilence.Forms.MouseEventArgs e)` but wired to a plain `EventHandler`
  delegate — changed to `EventArgs`, matching every other handler in those files (none of them
  actually used mouse-specific members).
- 13 files under `RdlDesign/RdlProperties/` had a redundant `using System.Drawing.Design;`
  alongside `using Majorsilence.Forms.Design;`, making `UITypeEditor`/`UITypeEditorEditStyle`
  ambiguous — removed the stale `System.Drawing.Design` import.
- `RdlDesign/SimpleToggle.cs` overrode `OnClick(MouseEventArgs)`; the base `Control.OnClick` in
  Majorsilence.Forms takes `EventArgs` (matching classic WinForms) — fixed the override signature.
- `RdlDesign/AssemblyInfo.cs`'s `InternalsVisibleTo("RdlDesign.Forms.Tests")` needed updating to
  the renamed test assembly (`RdlDesign.Tests`).

Verified clean: `dotnet build MajorsilenceReporting.slnx` under both `Debug-DrawingCompat` and
`Release-DrawingCompat`, plus `dotnet test` on all four promoted test projects and `ReportTests`.

**Legacy WinForms embedding** for external `System.Windows.Forms` host apps that want a real
`Control` (not a Majorsilence.Forms one) now goes through
[`Majorsilence.Forms.WinForms`](https://github.com/majorsilence/Majorsilence.Forms/tree/main/src/Majorsilence.Forms.WinForms)'s
`MajorsilenceFormsPresenter`/`control.ToWinFormsControl()` — not on nuget.org yet, consumed via the
same `.local-nuget-feed` local-pack pattern as core `Majorsilence.Forms` (see `nuget.config`), except
this package only compiles/packs on Windows, so that pack step has to run there. `LibRdlWpfViewer`
is the reference implementation: it used to declare the classic `RdlViewer` WinForms control
directly in XAML inside a `WindowsFormsHost`; now it constructs the Majorsilence.Forms `RdlViewer`
in code-behind and hosts it via `.ToWinFormsControl()`
(`RdlWpfViewer.xaml.cs`) — the same pattern as
`~/source/repos/Majorsilence.Forms/samples/EmbeddingWinForms/MainForm.cs`. `LibRdlWpfViewer` also
dropped `net48` from its `TargetFrameworks` (`RdlViewer` no longer supports it). This work was done
on Linux and could not be locally compiled/verified (WPF and `Majorsilence.Forms.WinForms` are both
Windows-only) — real verification is the `windows.yml` CI leg, or a Windows machine.

**Explicitly not touched** (per user decision): the legacy net48 sample solutions under `Examples/`
(`SampleApp`, `Sample-Report-Viewer`, `SampleAppHyperLinkCustomAction`, `SampleApp2-SetData`,
`SampleDesignerControl`) directly embed the classic `RdlViewer`/`ReportDesigner` as WinForms
controls in Designer.cs-generated markup. They're standalone `.sln` files, not part of
`MajorsilenceReporting.slnx`/CI, and now reference project folders that no longer contain a
WinForms `Control` subclass — they're stale and won't build as-is. If revisited, they'd need the
same `ToWinFormsControl()` rewrite as `LibRdlWpfViewer`.
