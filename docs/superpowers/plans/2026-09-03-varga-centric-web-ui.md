# Varga-centric Web UI rebuild — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `Ikiastrro.Web`'s read layer so D1 is the hero and all 21 computed chart types (D1 + 20 vargas) are one click away, grouped by the classical varga bundles — replacing the current 3-tab `ChartWorkspace` that renders only 6 types behind hard-coded life-area tabs.

**Architecture:** Fresh routes and components of a varga-centric design, in the *same* project, keeping the dark-parchment `tokens.css` palette, the existing `SouthIndianGrid` / `CombinedD1D9Grid` / `DashaTimeline` / `SadeSatiTable` primitives, and the Plan-1 batch repository reads. The polar longitude wheel is a hand-drawn inline `<svg>` ring — **no Syncfusion dependency in this plan** (see Global Constraints). Every screen leads with a diagram. Interpretation stays out of scope — classical facts, displayed and organised.

**Tech Stack:** .NET 8, Blazor Server (interactive server components), Dapper, SQL Server. CSS via per-component Blazor CSS-isolation files (`ComponentName.razor.css`) reading `wwwroot/css/tokens.css` custom properties. No JS. No test project.

**Spec:** `docs/superpowers/specs/2026-09-01-varga-centric-web-ui-design.md` — read it alongside this plan. This plan implements its §10 build sequence (7 phases).

## Global Constraints

Every task's requirements implicitly include this section.

- **Target framework:** `net8.0` (both `Ikiastrro.Web` and `Ikiastrro.Cli`). Do not bump.
- **No new NuGet packages.** The spec's §6 / §9 Syncfusion wiring is **cut** from this plan by decision 2026-09-03 (rammyps): build the wheel as a hand-drawn inline `<svg>` ring instead. Syncfusion may replace it in a later follow-up — Task 1.8 leaves a note, nothing more.
- **`PlanetPositionsTable` keeps its existing "Malefic / Benefic" (functional-nature) column** by decision 2026-09-03 (rammyps), overriding spec §9's "deferred". Task 7.2 adds a stale-note to the spec.
- **CSS rule (`src/Ikiastrro.Web/Components/DESIGN.md`):** no raw hex, no CSS named colours, no inline `<style>` in `.razor` markup. New styling goes in the component's own `ComponentName.razor.css`, reading `var(--token)`. If a token is missing, add it to `tokens.css` (one value — the app is dark-only, no light/dark pair).
- **`ZodiacName` uses the Latin spelling `Capricornus`** internally; display code maps it to `"Capricorn"`. Every component that shows a sign name must apply that one substitution (there is a `Display(string)` helper pattern in the current `ChartWorkspace` / `CombinedD1D9Grid` — copy it).
- **The 21 chart-type codes**, in `tbl_Dim_ChartType.DisplayOrder`: `D1, D2, D6, D9, D10, D11, D2-US, D3, D4, D5, D7, D8, D12, D16, D20, D24, D27, D30, D40, D45, D60`.
- **Key namespaces (post 2026-09-03 engine reorg — the spec predates this):**
  - `Ikiastrro.Core.Presentation` — `ChartViewModel`, `PlanetRow`
  - `Ikiastrro.Core.Models` — `ChartKeyDetail`, `ChartResult`, `ChartHouseLord`, `ChartAspect`, `ChartConjunction`, `BirthDetails`, `SadeSatiPeriod`, `PlanetTransitSnapshot`
  - `Ikiastrro.Core.Engines.Astronomy` — `PlanetName`, `AstroMath`
  - `Ikiastrro.Core.Engines.Houses` — `LagnaFunctionalNature`, `FunctionalNature`
  - `Ikiastrro.Core.Engines.Dasha` — `DashaPeriodRecord`
  - `Ikiastrro.Data` — every `*Repository`, `ChartGenerationService`, `SqlConnectionFactory`
  - `_Imports.razor` already `@using`s all of the above except `Ikiastrro.Core.Engines.Astronomy` and `Ikiastrro.Core.Engines.Houses` — add those two in Task 1.1 so component markup can name `PlanetName` / `LagnaFunctionalNature` unqualified.
- **Per-task verification cycle (there is no test project — this replaces "write a failing test"):**
  1. **Establish the check:** note the exact command / route / DOM observation that is currently absent or wrong.
  2. **Implement** in small edits.
  3. **Green:** `dotnet build Ikiastrro.slnx` reports `0 Warning(s) / 0 Error(s)`, AND the runtime observation from step 1 now holds (browser route renders the described thing, or the `grep`/file check passes).
  4. **Regression gate:** `dotnet run --project src/Ikiastrro.Cli -- verify-vargas` and `-- verify-jaimini` both exit 0 (this plan is read-only w.r.t. Core/DB — they must stay green throughout).
  5. **Commit** with the message given in the task.
- **Running the app for a browser check** (machine Application Control blocks direct `.exe`): `dotnet run --project src/Ikiastrro.Web`, then open `http://localhost:5xxx` (port from console). Golden-record person is **BirthDetailId 1 (Ramakrishnan)** — `/charts/1`.
- **Commit granularity:** one commit per task, on the current branch `feat/signattributes-classifications` unless told otherwise. Prefix messages `feat(web):` / `refactor(web):` / `docs:`.
- **Razor markup quoting:** a component attribute whose C# expression contains a string literal (e.g. a dictionary index `_data.Charts["D9"]`) must be wrapped as `Attr="@(_data.Charts["D9"].X)"` — Razor's `@(...)` balances over the inner quotes. A bare `Attr="_data.Charts["D9"].X"` does not parse. For lambdas containing string literals in an event handler, use a single-quoted attribute: `@onclick='() => Nav.NavigateTo("/add")'`. The code blocks in this plan already follow this; keep it when adapting.

---

## File Structure

**New shared components** — `src/Ikiastrro.Web/Components/Shared/`
- `Disclosure.razor` (+`.css`) — `<button aria-expanded>` + animated `grid-template-rows: 0fr→1fr`, `inert` when closed. One responsibility: collapse/expand a region.
- `EmptyState.razor` (+`.css`) — icon glyph + message + optional `<code>` CLI hint.
- `SegmentedToggle.razor` (+`.css`) — 2–3 segments as `<a href>` (route-driven) or `<button>` (callback-driven), active segment filled `--accent`.
- `DataTable.razor` (+`.css`) — `DataTable<TRow>` generic: column defs with a sort selector, click-to-sort header, renders `RowTemplate`.

**New chart components** — `src/Ikiastrro.Web/Components/Charts/`
- `MiniGrid.razor` (+`.css`) — glyph-only compact South-Indian grid, whole thing is one `<a href>` to a varga view. Not a wrapper over `SouthIndianGrid` (that component's smallest mode still renders house badges + dignity dots + center meta we don't want here); a purpose-built 4×4.
- `PolarWheel.razor` (+`.css`) — inline `<svg>` 360° sidereal ring; markers = planet glyphs at their longitude; colour = `--planet-<x>`. D1 plots `NirayanaLongitudeDegrees`, a `Dn` plots `VargaLongitudeDegrees`.
- `ChartFrame.razor` (+`.css`) — `SegmentedToggle` (grid ⇄ wheel) wrapping `SouthIndianGrid | PolarWheel`; toggle state in a query-string param.
- `HouseLordshipTable.razor` (+`.css`) — 12 rows from `ChartHouseLord`.
- `ConjunctionsTable.razor` (+`.css`) — pairs from `ChartConjunction`.
- `VargottamaStrip.razor` (+`.css`) — per-graha chip, lit when the graha's `Dn` sign == its D1 sign.
- `GocharaPanel.razor` (+`.css`) — Saturn / Jupiter / Rahu current sidereal sign vs natal, "in since / next change".

**New workspace components** — `src/Ikiastrro.Web/Components/Workspace/` (new folder)
- `WorkspaceHeader.razor` (+`.css`) — name, DOB · time · offset · place line, Chara Karakas line, "Birth & computation" `Disclosure` trigger.
- `VargaRail.razor` (+`.css`) — the 4 classical bundles + "Extra vargas" as `Disclosure` groups of `MiniGrid` tiles.
- `BirthComputationPanel.razor` (+`.css`) — the per-person / per-chart scalar disclosure body.
- `WorkspaceData.cs` — plain class (not a component): loads one person's whole chart set in one batch and exposes `IReadOnlyDictionary<string, LoadedChart>`.

**New pages** — `src/Ikiastrro.Web/Components/Pages/`
- `Workspace.razor` (+`.css`) — `/charts/{id}` (replaces `ChartWorkspace.razor`).
- `VargaView.razor` (+`.css`) — `/charts/{id}/varga/{code}`.
- `Timing.razor` (+`.css`) — `/charts/{id}/timing`.
- `PrintChart.razor` (+`.css`) — `/charts/{id}/print`.
- `SavedCharts.razor` (+`.css`) — `/charts` (renamed from `Charts.razor`).

**New data** — `src/Ikiastrro.Data/`
- `GocharaRepository.cs` — thin wrapper: `GetSnapshots(DateTime asOfUtc)` → the 3 slow-planet `PlanetTransitSnapshot`s.

**New static asset** — `src/Ikiastrro.Web/wwwroot/css/`
- `print.css` — `@media print` rules for `/charts/{id}/print`, linked only from that page.

**Modified**
- `src/Ikiastrro.Web/wwwroot/css/tokens.css` — add nav-button + grid-semantic + `--vargottama` tokens.
- `src/Ikiastrro.Web/Components/_Imports.razor` — add 2 `@using`s.
- `src/Ikiastrro.Web/Components/Layout/MainLayout.razor` (+`.css`) — yellow Home / Saved Charts pills; nav gains no items.
- `src/Ikiastrro.Web/Components/Charts/SouthIndianGrid.razor` (+`.css`) — glyph pill via `PlanetChip`; new `SpecialPointLabels` parameter (corner labels); public API otherwise unchanged.
- `src/Ikiastrro.Web/Components/Charts/PlanetPositionsTable.razor` (+`.css`) — Karaka + Sub-Lord + House + House(Chandra) + Rules columns; `[show technical]` toggle adds Speed / Ecliptic-lat / Varga-long; varga-aware column set; keeps Malefic/Benefic.
- `src/Ikiastrro.Web/Components/Pages/Home.razor` (+`.css`) — search input + `MiniGrid` person rows.
- `src/Ikiastrro.Web/Components/Pages/Add.razor` (+`.css`) — minor restyle; copy line "D1 (Rasi) + D9 (Navamsa)" → "all 21 charts".
- `src/Ikiastrro.Web/Program.cs` — register `GocharaRepository`.
- `docs/*` and memory — Task 7.2.

**Deleted**
- `src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor` + `.css`
- `src/Ikiastrro.Web/Components/Shared/TabBar.razor` + `.css`
- `src/Ikiastrro.Web/Components/Pages/Charts.razor` + `.css` (content moves to `SavedCharts.razor`)

---

# Phase 1 — Primitives

App builds and the existing `/charts/{id}` still works after every task. New primitives are exercised by dropping them into the *current* `ChartWorkspace` at the end (Task 1.10) before Phase 2 replaces it.

---

### Task 1.1: Token additions + imports

**Files:**
- Modify: `src/Ikiastrro.Web/wwwroot/css/tokens.css`
- Modify: `src/Ikiastrro.Web/Components/_Imports.razor`

**Interfaces:**
- Consumes: nothing.
- Produces: CSS custom properties `--nav-btn`, `--nav-btn-fg`, `--cell-fill`, `--lagna-fill`, `--grid-stroke`, `--sign-text`, `--muted-text`, `--vargottama`, `--wheel-ring`, `--wheel-tick`. `_Imports` gains `Ikiastrro.Core.Engines.Astronomy` and `Ikiastrro.Core.Engines.Houses`.

- [ ] **Step 1: Establish the check.** `grep -c "\-\-vargottama" src/Ikiastrro.Web/wwwroot/css/tokens.css` → currently `0`.

- [ ] **Step 2: Add tokens.** Append inside the `:root { … }` block in `tokens.css`, before the closing brace:

```css
    /* Nav pills (MainLayout) — filled Home / Saved Charts buttons. --nav-btn is --accent. */
    --nav-btn: #e2ad4f;
    --nav-btn-fg: #1a1730;

    /* Semantic South-Indian grid tokens — so SouthIndianGrid/MiniGrid stop reading --paper/--ink
       directly (DESIGN.md: name the role, not the raw surface). Values equal the existing
       surfaces today; kept separate so the grid can be retuned without touching page chrome. */
    --cell-fill: #1c1a3a;      /* = --paper-raised */
    --lagna-fill: #262247;     /* --paper-raised lifted ~6% toward --accent for the Lagna cell */
    --grid-stroke: #322f57;    /* = --paper-line */
    --sign-text: #b3a9cf;      /* = --ink-soft */
    --muted-text: #7c7398;     /* = --ink-faint */

    /* VargottamaStrip lit state — a graha whose Dn sign equals its D1 sign. Alias of --dignity-good. */
    --vargottama: #7cc98a;

    /* PolarWheel ring + degree ticks. */
    --wheel-ring: #322f57;     /* = --paper-line */
    --wheel-tick: #7c7398;     /* = --ink-faint */
```

- [ ] **Step 3: Add imports.** In `_Imports.razor`, after the `@using Ikiastrro.Core.Presentation` line add:

```razor
@using Ikiastrro.Core.Engines.Astronomy
@using Ikiastrro.Core.Engines.Houses
```

- [ ] **Step 4: Green.** `dotnet build Ikiastrro.slnx` → 0/0. `grep -c "\-\-vargottama" src/Ikiastrro.Web/wwwroot/css/tokens.css` → `1`.

- [ ] **Step 5: Regression gate.** `dotnet run --project src/Ikiastrro.Cli -- verify-vargas` and `-- verify-jaimini` → exit 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/wwwroot/css/tokens.css src/Ikiastrro.Web/Components/_Imports.razor
git commit -m "feat(web): add nav/grid/vargottama/wheel design tokens + Core.Engines imports"
```

---

### Task 1.2: `Disclosure` shared component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Shared/Disclosure.razor`
- Create: `src/Ikiastrro.Web/Components/Shared/Disclosure.razor.css`

**Interfaces:**
- Consumes: nothing.
- Produces: `<Disclosure Title="…" Open="bool" OnToggle="EventCallback<bool>">…body…</Disclosure>`. `Title` is `string`; `Open` defaults `false`; body is `ChildContent`. When closed the body region has `inert` and `aria-hidden`.

- [ ] **Step 1: Establish the check.** No `Disclosure.razor` exists — `ls src/Ikiastrro.Web/Components/Shared/Disclosure.razor` fails.

- [ ] **Step 2: Write the component.**

```razor
@* Collapse/expand a region. Route/parent owns the Open state (bookmarkable pages pass it from a
   query param); this component just renders + raises OnToggle. Animated via grid-template-rows
   0fr->1fr so it works with dynamic-height content and no JS. *@
<div class="disc @(Open ? "open" : "")">
    <button type="button" class="disc-head" aria-expanded="@Open" @onclick="Toggle">
        <span class="disc-tw">@(Open ? "▾" : "▸")</span>
        <span class="disc-title">@Title</span>
    </button>
    <div class="disc-body-wrap" inert="@(!Open)" aria-hidden="@(!Open)">
        <div class="disc-body">@ChildContent</div>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OnToggle { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private async Task Toggle() => await OnToggle.InvokeAsync(!Open);
}
```

- [ ] **Step 3: Write the CSS.**

```css
.disc { border: 1px solid var(--paper-line); border-radius: .4rem; background: var(--paper-raised); }
.disc-head {
    width: 100%; display: flex; gap: .5rem; align-items: center;
    padding: .5rem .75rem; background: none; border: 0; cursor: pointer;
    color: var(--ink); font: inherit; text-align: left;
}
.disc-tw { color: var(--accent); }
.disc-title { font-weight: 600; }
.disc-body-wrap {
    display: grid; grid-template-rows: 0fr; transition: grid-template-rows .18s ease;
}
.disc.open .disc-body-wrap { grid-template-rows: 1fr; }
.disc-body { overflow: hidden; }
.disc.open .disc-body { padding: 0 .75rem .75rem; }
```

- [ ] **Step 4: Green.** `dotnet build Ikiastrro.slnx` → 0/0.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Shared/Disclosure.razor src/Ikiastrro.Web/Components/Shared/Disclosure.razor.css
git commit -m "feat(web): add Disclosure shared component"
```

---

### Task 1.3: `EmptyState` shared component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Shared/EmptyState.razor`
- Create: `src/Ikiastrro.Web/Components/Shared/EmptyState.razor.css`

**Interfaces:**
- Consumes: nothing.
- Produces: `<EmptyState Icon="◦" Message="…" Cli="backfill-charts" />`. `Icon` defaults `"◦"`; `Message` is `string` (required); `Cli` is `string?` — when set, renders `<code>dotnet run --project src/Ikiastrro.Cli -- @Cli</code>`.

- [ ] **Step 1: Establish the check.** No `EmptyState.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Placeholder for a region with no data — a not-yet-computed chart, an empty transit table. *@
<div class="empty">
    <span class="empty-icon" aria-hidden="true">@Icon</span>
    <p class="empty-msg">@Message</p>
    @if (Cli is not null)
    {
        <p class="empty-cli"><code>dotnet run --project src/Ikiastrro.Cli -- @Cli</code></p>
    }
</div>

@code {
    [Parameter] public string Icon { get; set; } = "◦";
    [Parameter, EditorRequired] public string Message { get; set; } = "";
    [Parameter] public string? Cli { get; set; }
}
```

- [ ] **Step 3: Write the CSS.**

```css
.empty {
    display: flex; flex-direction: column; align-items: center; gap: .4rem;
    padding: 1.5rem 1rem; text-align: center; color: var(--muted-text);
    border: 1px dashed var(--paper-line); border-radius: .4rem;
}
.empty-icon { font-size: 1.6rem; color: var(--ink-faint); }
.empty-msg { margin: 0; }
.empty-cli code {
    background: var(--paper); padding: 2px 6px; border-radius: 3px;
    font-size: 11px; color: var(--ink-soft);
}
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Shared/EmptyState.razor src/Ikiastrro.Web/Components/Shared/EmptyState.razor.css
git commit -m "feat(web): add EmptyState shared component"
```

---

### Task 1.4: `SegmentedToggle` shared component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Shared/SegmentedToggle.razor`
- Create: `src/Ikiastrro.Web/Components/Shared/SegmentedToggle.razor.css`

**Interfaces:**
- Consumes: nothing.
- Produces: `<SegmentedToggle Segments="IReadOnlyList<SegmentedToggle.Segment>" ActiveKey="string" />` where `record Segment(string Key, string Label, string Href)`. Rendered as `<a href>` per segment; the segment whose `Key == ActiveKey` gets class `active`. (Route-driven only — no callback variant needed by this plan.)

- [ ] **Step 1: Establish the check.** No `SegmentedToggle.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* 2–3 link segments; the active one is filled --accent. Used for grid⇄wheel and show-technical,
   both of which live in the query string so the view is bookmarkable. *@
<div class="seg" role="group">
    @foreach (var s in Segments)
    {
        <a class="seg-item @(s.Key == ActiveKey ? "active" : "")" href="@s.Href">@s.Label</a>
    }
</div>

@code {
    public record Segment(string Key, string Label, string Href);

    [Parameter, EditorRequired] public IReadOnlyList<Segment> Segments { get; set; } = Array.Empty<Segment>();
    [Parameter, EditorRequired] public string ActiveKey { get; set; } = "";
}
```

- [ ] **Step 3: Write the CSS.**

```css
.seg { display: inline-flex; border: 1px solid var(--paper-line); border-radius: .4rem; overflow: hidden; }
.seg-item {
    padding: .3rem .8rem; font-size: 13px; text-decoration: none;
    color: var(--ink-soft); background: var(--paper-raised);
}
.seg-item + .seg-item { border-left: 1px solid var(--paper-line); }
.seg-item.active { background: var(--accent); color: var(--nav-btn-fg); font-weight: 600; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Shared/SegmentedToggle.razor src/Ikiastrro.Web/Components/Shared/SegmentedToggle.razor.css
git commit -m "feat(web): add SegmentedToggle shared component"
```

---

### Task 1.5: `DataTable<TRow>` shared component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Shared/DataTable.razor`
- Create: `src/Ikiastrro.Web/Components/Shared/DataTable.razor.css`

**Interfaces:**
- Consumes: nothing.
- Produces: `DataTable<TRow>` with parameters `Rows` (`IReadOnlyList<TRow>`), `Columns` (`IReadOnlyList<DataTable<TRow>.Column>` where `record Column(string Header, Func<TRow, object?> Sort, RenderFragment<TRow> Cell)`), `InitialSortHeader` (`string?`). Clicking a header toggles asc/desc on that column's `Sort` selector. Consumed by Task 5.2 (`SavedCharts`).

- [ ] **Step 1: Establish the check.** No `DataTable.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@typeparam TRow
@* Generic sortable table. Column.Sort is the key selector; Column.Cell renders the <td> body.
   Sort is client-side over the already-loaded Rows — these lists are small (saved people). *@
<div class="dt-scroll">
    <table class="dt">
        <thead>
            <tr>
                @foreach (var col in Columns)
                {
                    <th>
                        <button type="button" class="dt-sort @SortClass(col)" @onclick="() => SortBy(col)">
                            @col.Header
                            <span class="dt-arrow">@Arrow(col)</span>
                        </button>
                    </th>
                }
            </tr>
        </thead>
        <tbody>
            @foreach (var row in Sorted())
            {
                <tr>
                    @foreach (var col in Columns)
                    {
                        <td>@col.Cell(row)</td>
                    }
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    public record Column(string Header, Func<TRow, object?> Sort, RenderFragment<TRow> Cell);

    [Parameter, EditorRequired] public IReadOnlyList<TRow> Rows { get; set; } = Array.Empty<TRow>();
    [Parameter, EditorRequired] public IReadOnlyList<Column> Columns { get; set; } = Array.Empty<Column>();
    [Parameter] public string? InitialSortHeader { get; set; }

    private Column? _sortCol;
    private bool _desc;

    protected override void OnParametersSet()
        => _sortCol ??= Columns.FirstOrDefault(c => c.Header == InitialSortHeader) ?? Columns.FirstOrDefault();

    private void SortBy(Column col)
    {
        if (_sortCol == col) _desc = !_desc;
        else { _sortCol = col; _desc = false; }
    }

    private IEnumerable<TRow> Sorted()
    {
        if (_sortCol is null) return Rows;
        var ordered = Rows.OrderBy(r => _sortCol.Sort(r));
        return _desc ? ordered.Reverse() : ordered;
    }

    private string SortClass(Column col) => _sortCol == col ? "on" : "";
    private string Arrow(Column col) => _sortCol == col ? (_desc ? "▼" : "▲") : "";
}
```

- [ ] **Step 3: Write the CSS.**

```css
.dt-scroll { overflow-x: auto; }
.dt { width: 100%; border-collapse: collapse; }
.dt th { text-align: left; border-bottom: 1px solid var(--paper-line); }
.dt td { padding: .4rem .6rem; border-bottom: 1px solid var(--paper-line); }
.dt-sort {
    background: none; border: 0; color: var(--ink-soft); font: inherit; font-weight: 600;
    cursor: pointer; padding: .4rem .6rem; display: inline-flex; gap: .3rem; align-items: center;
}
.dt-sort.on { color: var(--ink); }
.dt-arrow { color: var(--accent); font-size: 10px; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Shared/DataTable.razor src/Ikiastrro.Web/Components/Shared/DataTable.razor.css
git commit -m "feat(web): add generic sortable DataTable component"
```

---

### Task 1.6: `MiniGrid` chart component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/MiniGrid.razor`
- Create: `src/Ikiastrro.Web/Components/Charts/MiniGrid.razor.css`

**Interfaces:**
- Consumes: `ChartViewModel.PlanetGlyph` (`Ikiastrro.Core.Presentation`).
- Produces: `<MiniGrid PlanetsBySign="IReadOnlyDictionary<string, IReadOnlyList<string>>" LagnaSign="string" Href="string?" Caption="string?" />`. `PlanetsBySign` maps internal sign name → planet names. When `Href` is set the whole grid is an `<a>`. `Caption` renders under the grid (e.g. `"D9 · P"`).

- [ ] **Step 1: Establish the check.** No `MiniGrid.razor` exists.

- [ ] **Step 2: Write the component.** Fixed 4×4 South-Indian geometry (same cell coordinates as `SouthIndianGrid`/`CombinedD1D9Grid` — copy the `GridCells` array). Glyphs only, no dignity, no house badges, no center meta.

```razor
@* Thumbnail South-Indian grid: glyphs only, whole grid optionally a link. Used in the VargaRail,
   Home rows, and SavedCharts. Deliberately NOT SouthIndianGrid(Compact) — that still draws house
   badges + dignity dots + a center info cell we don't want at this size. *@
@{
    RenderFragment gridInner =
        @<div class="mini-chart">
            @foreach (var cell in GridCells)
            {
                var planets = PlanetsBySign.TryGetValue(cell.MatchSign, out var list) ? list : Empty;
                <div class="mini-cell @(cell.MatchSign == LagnaSign ? "lagna" : "")"
                     style="grid-column:@cell.Col;grid-row:@cell.Row;">
                    <span class="mini-sign">@cell.Abbr</span>
                    <span class="mini-planets">
                        @foreach (var p in planets)
                        {
                            <span class="mini-g">@ChartViewModel.PlanetGlyph(p)</span>
                        }
                    </span>
                </div>
            }
            <div class="mini-cell mini-center" style="grid-column:2/4;grid-row:2/4;"></div>
        </div>
    ;
}
<div class="mini-root">
    @if (Href is not null)
    {
        <a class="mini-link" href="@Href">@gridInner</a>
    }
    else
    {
        @gridInner
    }
    @if (Caption is not null)
    {
        <span class="mini-caption">@Caption</span>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyDictionary<string, IReadOnlyList<string>> PlanetsBySign { get; set; }
        = new Dictionary<string, IReadOnlyList<string>>();
    [Parameter, EditorRequired] public string LagnaSign { get; set; } = "";
    [Parameter] public string? Href { get; set; }
    [Parameter] public string? Caption { get; set; }

    private static readonly IReadOnlyList<string> Empty = Array.Empty<string>();

    private static readonly (string MatchSign, string Abbr, int Col, int Row)[] GridCells = new[]
    {
        ("Pisces","Pi",1,1), ("Aries","Ar",2,1), ("Taurus","Ta",3,1), ("Gemini","Ge",4,1),
        ("Aquarius","Aq",1,2), ("Cancer","Cn",4,2),
        ("Capricornus","Cp",1,3), ("Leo","Le",4,3),
        ("Sagittarius","Sg",1,4), ("Scorpio","Sc",2,4), ("Libra","Li",3,4), ("Virgo","Vi",4,4)
    };
}
```

- [ ] **Step 3: Write the CSS.**

```css
.mini-root { display: inline-flex; flex-direction: column; gap: .25rem; }
.mini-link { text-decoration: none; color: inherit; display: block; }
.mini-link:hover .mini-chart { outline: 1px solid var(--accent); }
.mini-chart {
    display: grid; grid-template-columns: repeat(4, 1fr); grid-template-rows: repeat(4, 1fr);
    width: 132px; height: 132px; background: var(--grid-stroke); gap: 1px;
}
.mini-cell { background: var(--cell-fill); padding: 2px 3px; overflow: hidden; }
.mini-cell.lagna { background: var(--lagna-fill); }
.mini-center { background: var(--paper); }
.mini-sign { font-size: 8px; color: var(--sign-text); }
.mini-planets { display: flex; flex-wrap: wrap; gap: 1px; }
.mini-g { font-size: 9px; color: var(--ink); font-weight: 600; }
.mini-caption { font-size: 11px; color: var(--muted-text); text-align: center; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/MiniGrid.razor src/Ikiastrro.Web/Components/Charts/MiniGrid.razor.css
git commit -m "feat(web): add MiniGrid thumbnail chart component"
```

---

### Task 1.7: Enrich `SouthIndianGrid` — `PlanetChip` glyphs + `SpecialPointLabels`

**Files:**
- Modify: `src/Ikiastrro.Web/Components/Charts/SouthIndianGrid.razor`
- Modify: `src/Ikiastrro.Web/Components/Charts/SouthIndianGrid.razor.css`

**Interfaces:**
- Consumes: `PlanetChip` (existing), `GridPlanetGlyph` (existing — has `Glyph`, `DignityToken`, `IsRetrograde`, `IsCombust`).
- Produces: unchanged public API **plus** one new optional parameter `SpecialPointLabels` (`IReadOnlyDictionary<string, IReadOnlyList<string>>` — internal sign name → short codes like `AL`, `A7`, `HL`, `Gk`, `Md`). Rendered as a small strip in each cell's top-left. Empty by default → existing callers render identically.

- [ ] **Step 1: Establish the check.** Open `/charts/1` in the running app — D1 grid cells show bare `Su`/`Ma` text glyphs, no `--planet-*` background pill, and no `AL`/`HL` corner labels.

- [ ] **Step 2: Add the parameter** to the `@code` block:

```csharp
    /// <summary>Internal sign name → special-point short codes to print in that cell's top-left
    /// corner (AL / A2..A12 / HL / Gk / Md). Empty → nothing rendered (back-compat).</summary>
    [Parameter] public IReadOnlyDictionary<string, IReadOnlyList<string>> SpecialPointLabels { get; set; }
        = new Dictionary<string, IReadOnlyList<string>>();
```

- [ ] **Step 3: Render the labels.** Inside the `<div class="cell …">`, immediately after the `<span class="house-nums">…</span>` block, add:

```razor
                    @if (SpecialPointLabels.TryGetValue(cell.MatchSign, out var sp) && sp.Count > 0)
                    {
                        <div class="special-labels">
                            @foreach (var code in sp) { <span class="special-label">@code</span> }
                        </div>
                    }
```

- [ ] **Step 4: Swap the glyph rendering to `PlanetChip`.** Replace the inner `<span class="planet-glyph">…</span>` loop body (the `@foreach (var g in glyphs)` block) with:

```razor
                        @foreach (var g in glyphs)
                        {
                            <span class="planet-glyph">
                                <PlanetChip Planet="@g.PlanetName" />
                                @if (g.DignityToken is not null)
                                {
                                    <span class="dot" style="background:var(--dignity-@g.DignityToken)"></span>
                                }
                                <span class="direction">(@(g.IsRetrograde ? "R" : "D"))</span>
                                @if (g.IsCombust) { <span class="combust-icon" title="Combust">🔥</span> }
                            </span>
                        }
```

  `GridPlanetGlyph` currently carries only a 2-letter `Glyph`, not the full planet name `PlanetChip` needs. Add a `PlanetName` field: open `src/Ikiastrro.Web/Components/Charts/GridPlanetGlyph.cs`, add `string PlanetName` as the first positional parameter of the record, and update its one constructor call in `ChartWorkspace.razor` `BuildPlanetsBySign` (line ~240) to pass `k.Planet` first:

```csharp
                    .Select(k => new GridPlanetGlyph(
                        k.Planet,
                        ChartViewModel.PlanetGlyph(k.Planet),
                        ChartViewModel.DignityToken(k.DignityStatus),
                        k.IsRetrograde ?? false,
                        k.IsCombust ?? false))
```

- [ ] **Step 5: CSS.** Append to `SouthIndianGrid.razor.css`:

```css
.special-labels { display: flex; flex-wrap: wrap; gap: 2px; margin-bottom: 2px; }
.special-label {
    font-size: 8px; color: var(--accent); border: 1px solid var(--accent-line);
    border-radius: 2px; padding: 0 2px; line-height: 1.3;
}
.planet-glyph { display: inline-flex; align-items: center; gap: 2px; }
```

- [ ] **Step 6: Green.** Build → 0/0. Reload `/charts/1` — D1 grid glyphs now render as `PlanetChip` pills with a coloured left border; no `SpecialPointLabels` passed yet so no corner labels (that arrives in Phase 2). No layout break.

- [ ] **Step 7: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 8: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/SouthIndianGrid.razor src/Ikiastrro.Web/Components/Charts/SouthIndianGrid.razor.css src/Ikiastrro.Web/Components/Charts/GridPlanetGlyph.cs src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor
git commit -m "feat(web): SouthIndianGrid glyphs via PlanetChip + SpecialPointLabels param"
```

---

### Task 1.8: `PolarWheel` chart component (inline SVG)

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/PolarWheel.razor`
- Create: `src/Ikiastrro.Web/Components/Charts/PolarWheel.razor.css`

**Interfaces:**
- Consumes: `ChartViewModel.PlanetGlyph`.
- Produces: `<PolarWheel Points="IReadOnlyList<PolarWheel.Point>" />` where `record Point(string Label, double LongitudeDegrees, string ColorVar)` — `ColorVar` is a token name like `"--planet-sun"` or `"--accent"` (Ascendant/special points). Draws a 360° ring, 12 sign sectors, degree ticks every 30°, and a glyph marker per point at its longitude. `0°` (Aries) at the **9 o'clock** position, counter-clockwise (Jyotish wheel convention) — see NOTE below.
- **NOTE (spec §12.2 open question):** orientation — Aries at 9 o'clock (chosen default here) vs 12 o'clock. If rammyps wants 12 o'clock, change `AngleToXy`'s `baseDeg` from `180` to `90` and flip the sign of the rotation term. Ask at Phase 2 review.
- **NOTE (Syncfusion):** this SVG ring replaces the spec's `SfPolarChart` for this plan (decision 2026-09-03). A later follow-up may swap in Syncfusion for zoom/tooltips; nothing here blocks that.

- [ ] **Step 1: Establish the check.** No `PolarWheel.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Hand-drawn 360° sidereal longitude ring. Aries 0° at 9 o'clock, increasing counter-clockwise.
   Pure SVG, no JS — renders correctly server-side on the dark theme. Markers cluster-nudge is
   NOT attempted; two near-conjunct planets overlap slightly, same as a paper wheel. *@
<svg class="wheel" viewBox="0 0 320 320" role="img" aria-label="Sidereal longitude wheel">
    <circle cx="160" cy="160" r="150" class="wheel-outer" />
    <circle cx="160" cy="160" r="96" class="wheel-inner" />
    @for (var s = 0; s < 12; s++)
    {
        var (x1, y1) = AngleToXy(s * 30, 96);
        var (x2, y2) = AngleToXy(s * 30, 150);
        <line x1="@F(x1)" y1="@F(y1)" x2="@F(x2)" y2="@F(y2)" class="wheel-spoke" />
        var (lx, ly) = AngleToXy(s * 30 + 15, 124);
        <text x="@F(lx)" y="@F(ly)" class="wheel-signlabel" text-anchor="middle" dominant-baseline="central">@SignAbbr(s)</text>
    }
    @foreach (var p in Points)
    {
        var (px, py) = AngleToXy(p.LongitudeDegrees, 132);
        <text x="@F(px)" y="@F(py)" class="wheel-marker" text-anchor="middle" dominant-baseline="central"
              style="fill:var(@p.ColorVar)">@ChartViewModel.PlanetGlyph(p.Label)</text>
    }
</svg>

@code {
    public record Point(string Label, double LongitudeDegrees, string ColorVar);

    [Parameter, EditorRequired] public IReadOnlyList<Point> Points { get; set; } = Array.Empty<Point>();

    // 0° Aries at 9 o'clock (=180° screen), longitude increases counter-clockwise.
    private static (double X, double Y) AngleToXy(double lonDeg, double radius)
    {
        const double baseDeg = 180;
        var screenDeg = baseDeg - lonDeg;           // counter-clockwise
        var rad = screenDeg * Math.PI / 180.0;
        return (160 + radius * Math.Cos(rad), 160 - radius * Math.Sin(rad));
    }

    private static string F(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static readonly string[] Signs =
        { "Ar","Ta","Ge","Cn","Le","Vi","Li","Sc","Sg","Cp","Aq","Pi" };
    private static string SignAbbr(int i) => Signs[i];
}
```

- [ ] **Step 3: Write the CSS.**

```css
.wheel { width: 100%; max-width: 320px; height: auto; }
.wheel-outer, .wheel-inner { fill: var(--paper-raised); stroke: var(--wheel-ring); stroke-width: 1; }
.wheel-spoke { stroke: var(--wheel-ring); stroke-width: 1; }
.wheel-signlabel { fill: var(--wheel-tick); font-size: 9px; }
.wheel-marker { font-size: 12px; font-weight: 700; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/PolarWheel.razor src/Ikiastrro.Web/Components/Charts/PolarWheel.razor.css
git commit -m "feat(web): add hand-drawn SVG PolarWheel component"
```

---

### Task 1.9: `ChartFrame` — grid ⇄ wheel toggle

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/ChartFrame.razor`
- Create: `src/Ikiastrro.Web/Components/Charts/ChartFrame.razor.css`

**Interfaces:**
- Consumes: `SegmentedToggle`, `SouthIndianGrid`, `PolarWheel`.
- Produces: `<ChartFrame View="string" BaseHref="string" GridContent="RenderFragment" WheelContent="RenderFragment" />`. `View` ∈ `"grid"` | `"wheel"` (from the page's `?view=` query param; default `"grid"`). `BaseHref` is the route without query (e.g. `/charts/1` or `/charts/1/varga/D9`) — the toggle links to `@BaseHref?view=grid` / `@BaseHref?view=wheel`, preserving other query params is out of scope (only `view` and `tech` exist and they're independent enough). Renders `GridContent` or `WheelContent` per `View`.

- [ ] **Step 1: Establish the check.** No `ChartFrame.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Wraps a South-Indian grid and a polar wheel behind a bookmarkable ?view= toggle. The page
   supplies both render fragments already bound to that chart's data; ChartFrame just switches. *@
<div class="frame">
    <div class="frame-bar">
        <SegmentedToggle ActiveKey="@View" Segments="Segments" />
    </div>
    <div class="frame-body">
        @if (View == "wheel") { @WheelContent } else { @GridContent }
    </div>
</div>

@code {
    [Parameter, EditorRequired] public string View { get; set; } = "grid";
    [Parameter, EditorRequired] public string BaseHref { get; set; } = "";
    [Parameter, EditorRequired] public RenderFragment GridContent { get; set; } = null!;
    [Parameter, EditorRequired] public RenderFragment WheelContent { get; set; } = null!;

    private IReadOnlyList<SegmentedToggle.Segment> Segments => new[]
    {
        new SegmentedToggle.Segment("grid",  "Grid",  $"{BaseHref}?view=grid"),
        new SegmentedToggle.Segment("wheel", "Wheel", $"{BaseHref}?view=wheel"),
    };
}
```

- [ ] **Step 3: Write the CSS.**

```css
.frame { display: flex; flex-direction: column; gap: .5rem; }
.frame-bar { display: flex; justify-content: flex-end; }
.frame-body { display: flex; justify-content: center; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/ChartFrame.razor src/Ikiastrro.Web/Components/Charts/ChartFrame.razor.css
git commit -m "feat(web): add ChartFrame grid/wheel toggle wrapper"
```

---

### Task 1.10: Exercise the new primitives in the current `ChartWorkspace`

Proves `PlanetChip`, `MiniGrid`, `PolarWheel`, `ChartFrame`, `Disclosure`, `EmptyState` render with real data before Phase 2 builds the whole page around them. This change is **reverted implicitly** when `ChartWorkspace` is deleted in Task 2.6 — it is scaffolding, kept as its own commit so the diff is legible.

**Files:**
- Modify: `src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor`

**Interfaces:**
- Consumes: everything from Tasks 1.2–1.9.
- Produces: nothing downstream.

- [ ] **Step 1: Establish the check.** `/charts/1` renders the current 3-tab workspace; no wheel, no `MiniGrid`, no `Disclosure` anywhere.

- [ ] **Step 2: Add a scaffold section.** In `ChartWorkspace.razor`, immediately after the `</header>` closing tag of `<header class="identity">`, insert:

```razor
    <section class="scaffold-smoke" style="margin:1rem 0;border:1px dashed var(--accent-line);padding:1rem;">
        <p style="color:var(--ink-faint);font-size:12px;">SCAFFOLD — primitives smoke (removed with ChartWorkspace in Phase 2)</p>
        <ChartFrame View="@(Tab == "wheel" ? "wheel" : "grid")" BaseHref="@($"/charts/{Id}")">
            <GridContent>
                @if (charts.TryGetValue("D1", out var d1s))
                {
                    <SouthIndianGrid AscendantSign="@d1s.AscSign" PlanetsBySign="d1s.PlanetsBySign"
                                     CenterTitle="D1"><CenterMeta><b>D1</b></CenterMeta></SouthIndianGrid>
                }
            </GridContent>
            <WheelContent>
                <PolarWheel Points="SmokeWheelPoints()" />
            </WheelContent>
        </ChartFrame>
        <Disclosure Title="Smoke disclosure" Open="false" OnToggle="_ => {}">
            <EmptyState Message="Nothing here — this is the EmptyState primitive." Cli="backfill-charts" />
        </Disclosure>
    </section>
```

- [ ] **Step 3: Add the helper** to `@code`:

```csharp
    private IReadOnlyList<PolarWheel.Point> SmokeWheelPoints()
    {
        if (!d1KeyDetailsRaw.Any()) return Array.Empty<PolarWheel.Point>();
        return d1KeyDetailsRaw
            .Where(k => k.PointKind == "Graha")
            .Select(k => new PolarWheel.Point(
                k.Planet,
                k.NirayanaLongitudeDegrees,
                k.Planet == "Ascendant" ? "--accent" : $"--planet-{k.Planet.ToLowerInvariant()}"))
            .ToList();
    }
```

- [ ] **Step 4: Green.** Build → 0/0. `dotnet run --project src/Ikiastrro.Web`, open `/charts/1` — the dashed SCAFFOLD box shows the D1 grid with `PlanetChip` glyphs; visiting `/charts/1?tab=wheel` swaps it for the SVG wheel with 10 glyph markers around the ring; the Disclosure toggles open to reveal the EmptyState.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor
git commit -m "feat(web): scaffold-smoke the Phase-1 primitives in ChartWorkspace"
```

---

# Phase 2 — Workspace

Builds the real `/charts/{id}`. Ends by deleting `ChartWorkspace.razor` and `TabBar.razor`.

---

### Task 2.1: `WorkspaceData` loader + `LoadedChart` record

**Files:**
- Create: `src/Ikiastrro.Web/Components/Workspace/WorkspaceData.cs`

**Interfaces:**
- Consumes: `BirthDetailsRepository.GetById(int)`, and the batch reads `ChartResultsRepository.GetByBirthDetailId`, `ChartKeyDetailsRepository.GetByBirthDetailId`, `ChartHouseLordsRepository.GetByBirthDetailId`, `ChartAspectsRepository.GetByBirthDetailId`, `ChartConjunctionsRepository.GetByBirthDetailId` (all return `IReadOnlyList<…>` ordered by `Id`, chart types interleaved — **group by `ChartResultId`, do not assume contiguity**).
- Produces:
  ```csharp
  namespace Ikiastrro.Web.Components.Workspace;

  public sealed record LoadedChart(
      string ChartType, string Label, string SanskritName, string? VargaMethod,
      string AscendantSign, string? MoonSign, string? MoonNakshatra,
      IReadOnlyList<ChartKeyDetail>   KeyDetails,
      IReadOnlyList<ChartHouseLord>   HouseLords,
      IReadOnlyList<ChartAspect>      Aspects,
      IReadOnlyList<ChartConjunction> Conjunctions,
      double? AyanamshaDegrees, double? SiderealTimeHours, string EngineVersion)
  {
      public IReadOnlyList<ChartKeyDetail> Grahas =>
          KeyDetails.Where(k => k.PointKind == "Graha").ToList();
      public IReadOnlyList<ChartKeyDetail> SpecialPoints =>
          KeyDetails.Where(k => k.PointKind != "Graha").ToList();
  }

  public sealed class WorkspaceData
  {
      public BirthDetails Person { get; }
      public IReadOnlyDictionary<string, LoadedChart> Charts { get; }  // keyed by ChartType code
      public bool HasAnyChart => Charts.Count > 0;
      public static WorkspaceData? Load(int id, /* the 6 repos */ ...);
  }
  ```
- The Sanskrit varga name is `tbl_Dim_ChartType.DisplayName` — but there is no repo method that returns it keyed by code that the Web project injects today (`ChartTypeRepository` is not DI-registered in Web). **Register `ChartTypeRepository` in `Program.cs`** (Task 2.6 does the Program.cs edits; note it here) and pass `IReadOnlyList<ChartTypeRow>` into `Load`. `ChartTypeRow` has `Code`, `DisplayName`.

- [ ] **Step 1: Establish the check.** No `WorkspaceData.cs` exists; the current `ChartWorkspace` loads charts one `ChartResultId` at a time in a loop over 6 hard-coded types.

- [ ] **Step 2: Write `WorkspaceData.cs`.**

```csharp
using Ikiastrro.Core.Models;
using Ikiastrro.Data;

namespace Ikiastrro.Web.Components.Workspace;

public sealed record LoadedChart(
    string ChartType, string Label, string SanskritName, string? VargaMethod,
    string AscendantSign, string? MoonSign, string? MoonNakshatra,
    IReadOnlyList<ChartKeyDetail> KeyDetails,
    IReadOnlyList<ChartHouseLord> HouseLords,
    IReadOnlyList<ChartAspect> Aspects,
    IReadOnlyList<ChartConjunction> Conjunctions,
    double? AyanamshaDegrees, double? SiderealTimeHours, string EngineVersion)
{
    public IReadOnlyList<ChartKeyDetail> Grahas => KeyDetails.Where(k => k.PointKind == "Graha").ToList();
    public IReadOnlyList<ChartKeyDetail> SpecialPoints => KeyDetails.Where(k => k.PointKind != "Graha").ToList();
}

public sealed class WorkspaceData
{
    public required BirthDetails Person { get; init; }
    public required IReadOnlyDictionary<string, LoadedChart> Charts { get; init; }
    public bool HasAnyChart => Charts.Count > 0;

    public static WorkspaceData? Load(
        int id,
        BirthDetailsRepository people,
        ChartResultsRepository results,
        ChartKeyDetailsRepository keyDetails,
        ChartHouseLordsRepository houseLords,
        ChartAspectsRepository aspects,
        ChartConjunctionsRepository conjunctions,
        IReadOnlyList<ChartTypeRow> chartTypes)
    {
        var person = people.GetById(id);
        if (person is null) return null;

        var headers = results.GetByBirthDetailId(id)
            .Where(r => r.CalculationKind == "PositionChart")
            .ToList();
        var kdByChart = keyDetails.GetByBirthDetailId(id).GroupBy(k => k.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartKeyDetail>)g.ToList());
        var hlByChart = houseLords.GetByBirthDetailId(id).GroupBy(h => h.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartHouseLord>)g.ToList());
        var asByChart = aspects.GetByBirthDetailId(id).GroupBy(a => a.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartAspect>)g.ToList());
        var cjByChart = conjunctions.GetByBirthDetailId(id).GroupBy(c => c.ChartResultId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ChartConjunction>)g.ToList());
        var sanskritByCode = chartTypes.ToDictionary(t => t.Code, t => t.DisplayName, StringComparer.OrdinalIgnoreCase);

        var empty = Array.Empty<ChartKeyDetail>();
        var charts = new Dictionary<string, LoadedChart>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers)
        {
            var kd = kdByChart.GetValueOrDefault(h.Id, empty);
            var asc = kd.FirstOrDefault(k => k.Planet == "Ascendant")?.Sign;
            if (asc is null) continue;
            var moon = kd.FirstOrDefault(k => k.Planet == "Moon");
            charts[h.ChartType] = new LoadedChart(
                h.ChartType,
                sanskritByCode.TryGetValue(h.ChartType, out var sn) ? $"{sn} · {h.ChartType}" : h.ChartType,
                sanskritByCode.GetValueOrDefault(h.ChartType, h.ChartType),
                h.VargaMethod,
                asc, moon?.Sign, moon?.Nakshatra,
                kd,
                hlByChart.GetValueOrDefault(h.Id, Array.Empty<ChartHouseLord>()),
                asByChart.GetValueOrDefault(h.Id, Array.Empty<ChartAspect>()),
                cjByChart.GetValueOrDefault(h.Id, Array.Empty<ChartConjunction>()),
                h.AyanamshaDegrees, h.SiderealTimeHours, h.EngineVersion);
        }

        return new WorkspaceData { Person = person, Charts = charts };
    }
}
```

- [ ] **Step 3: Green.** Build → 0/0 (nothing consumes it yet — this compiles standalone).
- [ ] **Step 4: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 5: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Workspace/WorkspaceData.cs
git commit -m "feat(web): add WorkspaceData batch loader + LoadedChart record"
```

---

### Task 2.2: `WorkspaceHeader` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Workspace/WorkspaceHeader.razor` + `.css`

**Interfaces:**
- Consumes: `BirthDetails`, `LoadedChart` (for the D1 Chara Karaka line — read `d1.Grahas` where `CharaKaraka is not null`).
- Produces: `<WorkspaceHeader Person="BirthDetails" D1="LoadedChart" ComputationOpen="bool" OnToggleComputation="EventCallback<bool>" />`. Renders the name `<h1>`, the `DOB · time (offset) · City, Country` line, the Chara Karakas line (`AK Ra · AmK Ve · …` in the fixed order `AK AmK BK MK PiK PK GK DK`), and a `Disclosure`-style trigger button labelled "Birth & computation" that raises `OnToggleComputation` (the panel body itself is Task 2.3, rendered by the page).

- [ ] **Step 1: Establish the check.** No `WorkspaceHeader.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Identity band for the workspace + VargaView. Chara Karakas come from the D1 grahas (the label
   is identical on every varga, so D1 is the canonical source). *@
<header class="ws-header">
    <h1>@Person.Name</h1>
    <p class="ws-sub">
        @Person.DateOfBirth.ToString("d MMM yyyy") ·
        @Person.EffectiveTimeOfBirth.ToString("h:mm tt") (@Person.UtcOffset) ·
        @Person.PlaceCity, @Person.PlaceCountry
    </p>
    <p class="ws-karakas">
        <span class="ws-karakas-lbl">Chara Karakas:</span>
        @foreach (var (code, glyph) in KarakaGlyphs())
        {
            <span class="ws-karaka">@code <b>@glyph</b></span>
        }
    </p>
    <button type="button" class="ws-comp-trigger" aria-expanded="@ComputationOpen"
            @onclick="() => OnToggleComputation.InvokeAsync(!ComputationOpen)">
        @(ComputationOpen ? "▾" : "▸") Birth &amp; computation
    </button>
</header>

@code {
    [Parameter, EditorRequired] public BirthDetails Person { get; set; } = null!;
    [Parameter, EditorRequired] public LoadedChart D1 { get; set; } = null!;
    [Parameter] public bool ComputationOpen { get; set; }
    [Parameter] public EventCallback<bool> OnToggleComputation { get; set; }

    private static readonly string[] KarakaOrder = { "AK", "AmK", "BK", "MK", "PiK", "PK", "GK", "DK" };

    private IEnumerable<(string Code, string Glyph)> KarakaGlyphs()
    {
        var byCode = D1.Grahas
            .Where(g => g.CharaKaraka is not null)
            .ToDictionary(g => g.CharaKaraka!, g => ChartViewModel.PlanetGlyph(g.Planet));
        foreach (var code in KarakaOrder)
            if (byCode.TryGetValue(code, out var glyph))
                yield return (code, glyph);
    }
}
```

- [ ] **Step 3: Write the CSS.**

```css
.ws-header { border-bottom: 1px solid var(--paper-line); padding-bottom: .75rem; margin-bottom: 1rem; }
.ws-header h1 { margin: 0 0 .2rem; }
.ws-sub { margin: 0 0 .4rem; color: var(--ink-soft); }
.ws-karakas { margin: 0 0 .5rem; display: flex; flex-wrap: wrap; gap: .5rem; font-size: 13px; }
.ws-karakas-lbl { color: var(--muted-text); }
.ws-karaka { color: var(--ink-soft); }
.ws-karaka b { color: var(--ink); }
.ws-comp-trigger {
    background: none; border: 0; color: var(--accent); cursor: pointer; font: inherit; padding: 0;
}
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Workspace/WorkspaceHeader.razor src/Ikiastrro.Web/Components/Workspace/WorkspaceHeader.razor.css
git commit -m "feat(web): add WorkspaceHeader component"
```

---

### Task 2.3: `BirthComputationPanel` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Workspace/BirthComputationPanel.razor` + `.css`

**Interfaces:**
- Consumes: `BirthDetails`, `IReadOnlyDictionary<string, LoadedChart>` (for the per-chart `VargaMethod` table + the D1 ayanamsha/sidereal scalars).
- Produces: `<BirthComputationPanel Person="BirthDetails" Charts="IReadOnlyDictionary<string, LoadedChart>" />`. Renders: birth lat/long (decimal, 4 dp), `IanaTimeZoneId` + `UtcOffset`, ayanamsha° formatted D-M-S, sidereal time `HH:MM:SS`, `EngineVersion`, and a table `ChartType · VargaMethod` over all loaded charts in `VargaRailOrder` (Task 3.4 defines the order; until then use `Charts.Keys.OrderBy(k => k)`).
- Helpers to add as `private static` in the component:
  - `DmsFromDegrees(double deg)` → `"23°34'50\""` (`d = (int)deg; m = (int)((deg-d)*60); s = (int)round((((deg-d)*60)-m)*60)`).
  - `HmsFromHours(double hours)` → `"19:20:55"` (`TimeSpan.FromHours(hours).ToString(@"hh\:mm\:ss")`).

- [ ] **Step 1: Establish the check.** No `BirthComputationPanel.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* The "clean by default" scalars, shown only when the header's "Birth & computation" disclosure
   is open. Sunrise/sunset are NOT included here — they are Spec-1 SunTimes, not on ChartResult;
   deferred to a later pass (noted in the plan). *@
<div class="bcp">
    <dl class="bcp-grid">
        <dt>Latitude</dt><dd>@Person.Latitude.ToString("0.0000")</dd>
        <dt>Longitude</dt><dd>@Person.Longitude.ToString("0.0000")</dd>
        <dt>Time zone</dt><dd>@(Person.IanaTimeZoneId ?? "—") (@Person.UtcOffset)</dd>
        <dt>Ayanamsha</dt><dd>@Ayanamsha()</dd>
        <dt>Sidereal time</dt><dd>@SiderealTime()</dd>
        <dt>Engine</dt><dd>@Engine()</dd>
    </dl>
    <table class="bcp-methods">
        <thead><tr><th>Chart</th><th>Varga method</th></tr></thead>
        <tbody>
            @foreach (var kv in Charts.OrderBy(k => k.Key))
            {
                <tr><td>@kv.Key</td><td>@(kv.Value.VargaMethod ?? "—")</td></tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter, EditorRequired] public BirthDetails Person { get; set; } = null!;
    [Parameter, EditorRequired] public IReadOnlyDictionary<string, LoadedChart> Charts { get; set; }
        = new Dictionary<string, LoadedChart>();

    private LoadedChart? D1 => Charts.GetValueOrDefault("D1");
    private string Ayanamsha() => D1?.AyanamshaDegrees is double d ? DmsFromDegrees(d) : "—";
    private string SiderealTime() => D1?.SiderealTimeHours is double h ? HmsFromHours(h) : "—";
    private string Engine() => D1?.EngineVersion is { Length: > 0 } e ? e : "—";

    private static string DmsFromDegrees(double deg)
    {
        var d = (int)deg;
        var mFull = (deg - d) * 60;
        var m = (int)mFull;
        var s = (int)Math.Round((mFull - m) * 60);
        if (s == 60) { s = 0; m++; }
        if (m == 60) { m = 0; d++; }
        return $"{d}°{m:00}'{s:00}\"";
    }

    private static string HmsFromHours(double hours) =>
        TimeSpan.FromHours(hours).ToString(@"hh\:mm\:ss");
}
```

- [ ] **Step 3: Write the CSS.**

```css
.bcp { display: flex; flex-wrap: wrap; gap: 1.5rem; font-size: 13px; }
.bcp-grid { display: grid; grid-template-columns: auto auto; gap: .2rem .8rem; margin: 0; }
.bcp-grid dt { color: var(--muted-text); }
.bcp-grid dd { margin: 0; color: var(--ink); }
.bcp-methods { border-collapse: collapse; }
.bcp-methods th, .bcp-methods td { padding: .15rem .5rem; border-bottom: 1px solid var(--paper-line); text-align: left; }
.bcp-methods th { color: var(--muted-text); font-weight: 600; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Workspace/BirthComputationPanel.razor src/Ikiastrro.Web/Components/Workspace/BirthComputationPanel.razor.css
git commit -m "feat(web): add BirthComputationPanel component"
```

> **CONTRADICTION TO RAISE AT REVIEW:** spec §4.1 lists sunrise/sunset in this panel, sourced from "Spec 1". `SwissEphemerisProvider.GetSunTimes` exists in Core but there is no repository and no `ChartResult` column for it — surfacing it means a new read path. This task omits sunrise/sunset. Confirm that deferral is acceptable or schedule a follow-up.

---

### Task 2.4: `VargaRail` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Workspace/VargaRail.razor` + `.css`
- Create: `src/Ikiastrro.Web/Components/Workspace/VargaBundles.cs`

**Interfaces:**
- Consumes: `MiniGrid`, `Disclosure`, `EmptyState`, `IReadOnlyDictionary<string, LoadedChart>`.
- Produces:
  - `VargaBundles` static class:
    ```csharp
    namespace Ikiastrro.Web.Components.Workspace;
    public static class VargaBundles
    {
        // Source: docs/reference-chart-varga-index.md "Groups:" annotations + classical Parashari
        // Shodashavarga membership. D9/D10/D60 not in that doc's rows (they have full guides):
        // D9 -> 6·7·10·16, D10 -> 10·16, D60 -> 6·7·10·16 (classical).
        public static readonly IReadOnlyList<(string Title, IReadOnlyList<string> Codes)> Groups = new[]
        {
            ("Shadvarga (6)",       (IReadOnlyList<string>)new[] { "D1","D2-US","D3","D9","D12","D30" }),
            ("Saptavarga (7)",      new[] { "D7" }),
            ("Dasavarga (10)",      new[] { "D10","D16","D60" }),
            ("Shodasavarga (16)",   new[] { "D4","D20","D24","D27","D40","D45" }),
            ("Extra vargas",        new[] { "D2","D5","D6","D8","D11" }),
        };
        // Rail order = the sequence a prev/next in VargaView walks. Groups top-to-bottom, codes in listed order.
        public static readonly IReadOnlyList<string> RailOrder =
            Groups.SelectMany(g => g.Codes).ToList();
    }
    ```
    Each varga is listed **once**, in its smallest bundle (that is why `D2-US`, `D3`, `D9`, `D12`, `D30` sit under Shadvarga and are *not* repeated under Saptavarga/Dasavarga/Shodasavarga). `D1` shows in the Shadvarga group but is also the hero — the rail marks it "· hero".
  - `<VargaRail PersonId="int" Charts="IReadOnlyDictionary<string, LoadedChart>" />`.

- [ ] **Step 1: Establish the check.** No `VargaRail.razor` exists.

- [ ] **Step 2: Write `VargaBundles.cs`** exactly as above.

- [ ] **Step 3: Write `VargaRail.razor`.**

```razor
@* The 4 classical strength bundles + Extra vargas, each a Disclosure of MiniGrid tiles.
   A varga appears once, in its smallest bundle. Missing chart -> EmptyState tile. *@
<div class="rail">
    @foreach (var (title, codes) in VargaBundles.Groups)
    {
        <Disclosure Title="@title" Open="@(_open.Contains(title))" OnToggle="v => SetOpen(title, v)">
            <div class="rail-tiles">
                @foreach (var code in codes)
                {
                    @if (Charts.TryGetValue(code, out var lc))
                    {
                        <MiniGrid PlanetsBySign="GlyphsBySign(lc)" LagnaSign="@lc.AscendantSign"
                                  Href="@($"/charts/{PersonId}/varga/{code}")"
                                  Caption="@Caption(code, lc)" />
                    }
                    else
                    {
                        <div class="rail-missing">
                            <EmptyState Message="@($"{code} not computed")" Cli="backfill-charts" />
                        </div>
                    }
                }
            </div>
        </Disclosure>
    }
</div>

@code {
    [Parameter, EditorRequired] public int PersonId { get; set; }
    [Parameter, EditorRequired] public IReadOnlyDictionary<string, LoadedChart> Charts { get; set; }
        = new Dictionary<string, LoadedChart>();

    private readonly HashSet<string> _open = new() { "Shadvarga (6)" };
    private void SetOpen(string title, bool open) { if (open) _open.Add(title); else _open.Remove(title); }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> GlyphsBySign(LoadedChart lc) =>
        lc.Grahas.Where(k => k.Planet != "Ascendant")
            .GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet).ToList());

    private static string Caption(string code, LoadedChart lc)
    {
        var method = lc.VargaMethod is { Length: > 0 } m ? $" · {m[0]}" : "";
        return code == "D1" ? $"{code} · hero" : $"{code}{method}";
    }
}
```

- [ ] **Step 4: Write the CSS.**

```css
.rail { display: flex; flex-direction: column; gap: .5rem; }
.rail-tiles { display: flex; flex-wrap: wrap; gap: .75rem; padding-top: .5rem; }
.rail-missing { width: 132px; }
```

- [ ] **Step 5: Green.** Build → 0/0.
- [ ] **Step 6: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 7: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Workspace/VargaRail.razor src/Ikiastrro.Web/Components/Workspace/VargaRail.razor.css src/Ikiastrro.Web/Components/Workspace/VargaBundles.cs
git commit -m "feat(web): add VargaRail + VargaBundles map"
```

> **CONTRADICTION TO RAISE AT REVIEW:** spec §12.3 flagged bundle membership as an open question "fixed against one cited source in Phase 3". This task fixes it now against `docs/reference-chart-varga-index.md` + classical Shodashavarga. If rammyps wants BPHS / PVR Narasimha Rao membership instead (D2 vs D2-US in Shadvarga is the likely difference), `VargaBundles.Groups` is the one place to change.

---

### Task 2.5: Rebuild `PlanetPositionsTable` — Karaka / House / Rules / Sub-Lord / technical toggle / varga-aware

**Files:**
- Modify: `src/Ikiastrro.Web/Components/Charts/PlanetPositionsTable.razor` + `.css`
- Modify: `src/Ikiastrro.Core/Presentation/ChartViewModel.cs` (extend `PlanetRow` + `BuildPlanetRows`)

**Interfaces:**
- Consumes: `LagnaFunctionalNature.For` (kept — decision 2026-09-03), `ChartKeyDetail`.
- Produces:
  - `PlanetRow` gains: `string? CharaKaraka`, `string? NakshatraSubLordPlanet`, `double? SpeedLongitudeDegPerDay`, `double? EclipticLatitudeDegrees`, `double VargaLongitudeDegrees`, `string PointKind`. Add them to the record and populate in `BuildPlanetRows` from the matching `ChartKeyDetail` fields.
  - `<PlanetPositionsTable Rows="IReadOnlyList<PlanetRow>" LagnaSign="string" Variant="string" ShowTechnical="bool" />` where `Variant` ∈ `"d1"` | `"varga"`. `"d1"` shows the full column set; `"varga"` omits Nakshatra / Pada / Nak-Lord / Sub-Lord (varga cells are discrete buckets). `ShowTechnical` (from `?tech=1`) appends **Speed °/day**, **Ecliptic lat °**, **Varga long °**. Special-point rows (`PointKind != "Graha"`) are filtered out here — they render as grid corner labels, not planet rows.

- [ ] **Step 1: Establish the check.** `/charts/1` positions table has no "Karaka", "House", "Rules", or "Sub-Lord" column and no technical toggle.

- [ ] **Step 2: Extend `PlanetRow`.** In `ChartViewModel.cs`, add to the record after `AspectedBy`:

```csharp
    string? AspectedBy,
    string? CharaKaraka,
    string? NakshatraSubLordPlanet,
    double? SpeedLongitudeDegPerDay,
    double? EclipticLatitudeDegrees,
    double VargaLongitudeDegrees,
    string PointKind);
```

  and in `BuildPlanetRows`'s `new PlanetRow(…)` add the matching trailing args:

```csharp
                k.AspectingPlanets,
                k.CharaKaraka,
                k.NakshatraSubLordPlanet,
                k.SpeedLongitudeDegPerDay,
                k.EclipticLatitudeDegrees,
                k.VargaLongitudeDegrees,
                k.PointKind);
```

- [ ] **Step 3: Rewrite the table markup.**

```razor
@using Ikiastrro.Core.Engines.Astronomy
@* Planetary positions for one chart. Variant "d1" = full columns; "varga" drops the nakshatra
   family (discrete buckets). Malefic/Benefic = computed Parashari functional nature for the natal
   Lagna (kept by decision 2026-09-03, overriding spec §9). Special-point rows are excluded. *@
<div class="ppt-scroll">
    <table class="ppt">
        <thead>
            <tr>
                <th>Graha</th><th>Rasi</th><th>Degree</th>
                @if (Variant == "d1")
                {
                    <th>Nakshatra</th><th>Pada</th><th>Nak&nbsp;Lord</th><th>Sub&nbsp;Lord</th>
                }
                <th>Dir</th><th>House</th><th>House&nbsp;(Ch)</th><th>Dignity</th>
                <th>Rules</th><th>Malefic&nbsp;/&nbsp;Benefic</th><th>Karaka</th><th>Aspected&nbsp;by</th>
                @if (ShowTechnical)
                {
                    <th>Speed&nbsp;°/day</th><th>Ecl&nbsp;lat&nbsp;°</th><th>Varga&nbsp;long&nbsp;°</th>
                }
            </tr>
        </thead>
        <tbody>
            @foreach (var row in Rows.Where(r => r.PointKind == "Graha"))
            {
                var fn = Functional(row.Planet);
                <tr class="@(row.Planet == "Ascendant" ? "asc" : "")">
                    <td>@(row.Planet == "Ascendant" ? "Lagna" : row.Planet)</td>
                    <td>@DisplaySign(row.Sign)</td>
                    <td class="mono">@(string.IsNullOrEmpty(row.DegreesInSignDisplay) ? "—" : row.DegreesInSignDisplay)@(row.IsCombust == true ? " 🔥" : "")</td>
                    @if (Variant == "d1")
                    {
                        <td>@(row.Nakshatra ?? "—")</td>
                        <td class="mono">@(row.NakshatraPada?.ToString() ?? "—")</td>
                        <td>@(row.NakshatraLordPlanet ?? "—")</td>
                        <td>@(row.NakshatraSubLordPlanet ?? "—")</td>
                    }
                    <td>@ChartViewModel.DirectionLabel(row.IsRetrograde)</td>
                    <td class="mono">@row.HouseFromLagna</td>
                    <td class="mono">@row.HouseFromMoon</td>
                    <td>
                        @if (row.DignityStatus is null) { <span class="muted">—</span> }
                        else { <span class="pill"><span class="dot" style="background:var(--dignity-@ChartViewModel.DignityToken(row.DignityStatus))"></span>@row.DignityStatus</span> }
                    </td>
                    <td>@(row.RulesHouseNumbers ?? "—")</td>
                    <td>
                        @if (fn is null) { <span class="muted">—</span> }
                        else { <span class="pill"><span class="dot" style="background:@fn.Value.Color"></span>@fn.Value.Code</span> }
                    </td>
                    <td>@(row.CharaKaraka ?? "—")</td>
                    <td class="mono asp">@(row.AspectedBy ?? "—")</td>
                    @if (ShowTechnical)
                    {
                        <td class="mono">@(row.SpeedLongitudeDegPerDay?.ToString("0.0000") ?? "—")</td>
                        <td class="mono">@(row.EclipticLatitudeDegrees?.ToString("0.0000") ?? "—")</td>
                        <td class="mono">@row.VargaLongitudeDegrees.ToString("0.0000")</td>
                    }
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<PlanetRow> Rows { get; set; } = Array.Empty<PlanetRow>();
    [Parameter, EditorRequired] public string LagnaSign { get; set; } = "";
    [Parameter] public string Variant { get; set; } = "d1";
    [Parameter] public bool ShowTechnical { get; set; }

    private static string DisplaySign(string sign) => sign == "Capricornus" ? "Capricorn" : sign;

    private (string Code, string Color)? Functional(string planet)
    {
        if (!Enum.TryParse<PlanetName>(planet, out var p) || p is PlanetName.Rahu or PlanetName.Ketu)
            return null;
        if (!Enum.TryParse<ZodiacName>(LagnaSign, out var lagna))
            return null;
        var r = LagnaFunctionalNature.For(lagna, p);
        var isBenefic = r.Nature is FunctionalNature.Benefic or FunctionalNature.Yogakaraka;
        var prefix = isBenefic ? "B" : r.Nature == FunctionalNature.Malefic ? "M" : "N";
        var color = isBenefic ? "var(--dignity-good)"
            : r.Nature == FunctionalNature.Malefic ? "var(--dignity-enemy)" : "var(--dignity-neutral)";
        return ($"{prefix}-[{string.Join("&", r.RuledHouses)}]", color);
    }
}
```

- [ ] **Step 4: Update callers.** `ChartWorkspace.razor` (still live until Task 2.6) calls `<PlanetPositionsTable Rows="d1PlanetRows" LagnaSign="@d1.AscSign" />` — that still compiles (`Variant` defaults `"d1"`, `ShowTechnical` defaults false). No change needed; the new columns just appear.

- [ ] **Step 5: CSS.** Append to `PlanetPositionsTable.razor.css`:

```css
.ppt th, .ppt td { white-space: nowrap; }
.ppt .mono { font-variant-numeric: tabular-nums; }
```

- [ ] **Step 6: Green.** Build → 0/0. `/charts/1` positions table now shows Karaka (Sun→PiK, Rahu→AK, …), House, House(Ch), Rules, Sub-Lord columns; `/charts/1?tech=1` — wait, `ChartWorkspace` doesn't wire `tech` yet, so just confirm the default columns render. `verify-jaimini` still 0 (it reads the DB, not this table).

- [ ] **Step 7: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 8: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/PlanetPositionsTable.razor src/Ikiastrro.Web/Components/Charts/PlanetPositionsTable.razor.css src/Ikiastrro.Core/Presentation/ChartViewModel.cs
git commit -m "feat(web): PlanetPositionsTable — Karaka/House/Rules/SubLord + technical toggle, varga-aware"
```

---

### Task 2.6: `Workspace` page + delete `ChartWorkspace` / `TabBar`

**Files:**
- Create: `src/Ikiastrro.Web/Components/Pages/Workspace.razor` + `.css`
- Modify: `src/Ikiastrro.Web/Program.cs` (register `ChartTypeRepository`, `GocharaRepository` — the latter created in Phase 4; register it now to keep DI edits in one commit or defer to Task 4.1, choose deferred)
- Delete: `src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor` + `.css`
- Delete: `src/Ikiastrro.Web/Components/Shared/TabBar.razor` + `.css`

**Interfaces:**
- Consumes: `WorkspaceData.Load`, `WorkspaceHeader`, `BirthComputationPanel`, `VargaRail`, `ChartFrame`, `SouthIndianGrid`, `PolarWheel`, `PlanetPositionsTable`, `CombinedD1D9Grid`, `DashaTimeline`, `Disclosure`, `EmptyState`, `DeleteIconButton`, `ConfirmDialog`, `DashaPeriodsRepository.GetTreeByBirthDetailId`, `BirthDetailDeletionService`.
- Produces: route `/charts/{Id:int}` with query params `?view=grid|wheel` and `?tech=0|1`.

- [ ] **Step 1: Establish the check.** `/charts/1` renders the old `ChartWorkspace` with the SCAFFOLD box from Task 1.10.

- [ ] **Step 2: Register `ChartTypeRepository`.** In `Program.cs`, after `builder.Services.AddScoped<ChartTypeRepository>();` — it is already registered. Confirm; no edit needed. (`GocharaRepository` registration is Task 4.1.)

- [ ] **Step 3: Write `Workspace.razor`.**

```razor
@page "/charts/{Id:int}"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject ChartHouseLordsRepository ChartHouseLordsRepo
@inject ChartAspectsRepository ChartAspectsRepo
@inject ChartConjunctionsRepository ChartConjunctionsRepo
@inject ChartTypeRepository ChartTypeRepo
@inject DashaPeriodsRepository DashaPeriodsRepo
@inject BirthDetailDeletionService DeletionService
@inject NavigationManager Nav
@using Ikiastrro.Web.Components.Workspace

<PageTitle>@(_data?.Person.Name ?? "Chart") — ikiastrro</PageTitle>

@if (_data is null)
{
    <EmptyState Message="No saved chart found for that person." />
}
else
{
    <div class="ws-top">
        <a class="ws-back" href="/charts">← All saved charts</a>
        <div class="ws-actions">
            <a class="ws-print" href="@($"/charts/{Id}/print")">Print / Save PDF</a>
            <DeleteIconButton Title="@($"Delete {_data.Person.Name}'s chart")" OnClick="() => _confirmingDelete = true" />
        </div>
    </div>

    <WorkspaceHeader Person="_data.Person" D1="D1" ComputationOpen="_compOpen"
                     OnToggleComputation="v => _compOpen = v" />
    @if (_compOpen)
    {
        <div class="ws-comp"><BirthComputationPanel Person="_data.Person" Charts="_data.Charts" /></div>
    }

    <div class="ws-cols">
        <div class="ws-hero">
            <ChartFrame View="@View" BaseHref="@($"/charts/{Id}")">
                <GridContent>
                    <SouthIndianGrid AscendantSign="@D1.AscendantSign" MoonSign="@D1.MoonSign"
                                     PlanetsBySign="D1PlanetsBySign()"
                                     AspectedByGlyphs="ChartViewModel.BuildAspectedByGlyphs(D1.KeyDetails, D1.Aspects)"
                                     SpecialPointLabels="SpecialLabels(D1)"
                                     CenterTitle="D1 · Rasi">
                        <CenterMeta><b>Lagna</b> @Disp(D1.AscendantSign)</CenterMeta>
                    </SouthIndianGrid>
                </GridContent>
                <WheelContent>
                    <PolarWheel Points="WheelPoints(D1, varga: false)" />
                </WheelContent>
            </ChartFrame>

            @if (_data.Charts.ContainsKey("D9"))
            {
                <Disclosure Title="D1 ⊕ D9 — combined" Open="_combinedOpen" OnToggle="v => _combinedOpen = v">
                    <CombinedD1D9Grid D1KeyDetails="D1.KeyDetails" D9KeyDetails="@(_data.Charts["D9"].KeyDetails)"
                                      LagnaSign="@D1.AscendantSign" />
                </Disclosure>
            }
        </div>

        <div class="ws-rail">
            <VargaRail PersonId="Id" Charts="_data.Charts" />
        </div>
    </div>

    <section class="ws-ppt">
        <div class="ws-ppt-head">
            <h2>Planet positions — D1</h2>
            <SegmentedToggle ActiveKey="@(Tech ? "on" : "off")" Segments="TechSegments()" />
        </div>
        <PlanetPositionsTable Rows="D1Rows()" LagnaSign="@D1.AscendantSign" Variant="d1" ShowTechnical="Tech" />
    </section>

    <section class="ws-dasha">
        <div class="ws-dasha-head">
            <h3>Vimshottari Dasha</h3>
            <a href="@($"/charts/{Id}/timing")">Full timing →</a>
        </div>
        @if (_dashaRoots.Count > 0)
        {
            <DashaTimeline Roots="_dashaRoots" />
        }
        else
        {
            <EmptyState Message="No Dasha computed." Cli="@($"compute-dasha {_data.Person.Name}")" />
        }
    </section>

    <ConfirmDialog IsOpen="_confirmingDelete"
                   Message="@($"Delete {_data.Person.Name}'s chart and all computed data? This can't be undone.")"
                   IsBusy="_isDeleting" OnConfirm="HandleDelete" OnCancel="() => _confirmingDelete = false" />
}

@code {
    [Parameter] public int Id { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "view")] public string? ViewParam { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "tech")] public string? TechParam { get; set; }

    private WorkspaceData? _data;
    private IReadOnlyList<DashaPeriodRecord> _dashaRoots = Array.Empty<DashaPeriodRecord>();
    private bool _compOpen, _combinedOpen, _confirmingDelete, _isDeleting;

    private string View => ViewParam == "wheel" ? "wheel" : "grid";
    private bool Tech => TechParam == "1";
    private LoadedChart D1 => _data!.Charts["D1"];

    protected override void OnParametersSet()
    {
        _confirmingDelete = false;
        _data = WorkspaceData.Load(Id, BirthDetailsRepo, ChartResultsRepo, ChartKeyDetailsRepo,
            ChartHouseLordsRepo, ChartAspectsRepo, ChartConjunctionsRepo, ChartTypeRepo.GetAll());
        if (_data is not null && !_data.Charts.ContainsKey("D1")) _data = null; // no D1 → treat as not found
        _dashaRoots = _data is null ? Array.Empty<DashaPeriodRecord>() : DashaPeriodsRepo.GetTreeByBirthDetailId(Id);
    }

    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;

    private IReadOnlyDictionary<string, IReadOnlyList<GridPlanetGlyph>> D1PlanetsBySign() =>
        D1.Grahas.Where(k => k.Planet != "Ascendant")
            .GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GridPlanetGlyph>)g.Select(k => new GridPlanetGlyph(
                k.Planet, ChartViewModel.PlanetGlyph(k.Planet), ChartViewModel.DignityToken(k.DignityStatus),
                k.IsRetrograde ?? false, k.IsCombust ?? false)).ToList());

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> SpecialLabels(LoadedChart lc) =>
        lc.SpecialPoints.GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet switch
            {
                "Gulika" => "Gk", "Maandi" => "Md", _ => k.Planet
            }).ToList());

    private IReadOnlyList<PlanetRow> D1Rows() =>
        ChartViewModel.BuildPlanetRows(D1.KeyDetails, D1.HouseLords, D1.Aspects);

    private IReadOnlyList<PolarWheel.Point> WheelPoints(LoadedChart lc, bool varga) =>
        lc.KeyDetails.Select(k => new PolarWheel.Point(
            k.Planet,
            varga ? k.VargaLongitudeDegrees : k.NirayanaLongitudeDegrees,
            k.PointKind != "Graha" ? "--accent"
              : k.Planet == "Ascendant" ? "--accent"
              : $"--planet-{k.Planet.ToLowerInvariant()}")).ToList();

    private IReadOnlyList<SegmentedToggle.Segment> TechSegments() => new[]
    {
        new SegmentedToggle.Segment("off", "Clean", $"/charts/{Id}?view={View}&tech=0"),
        new SegmentedToggle.Segment("on",  "Technical", $"/charts/{Id}?view={View}&tech=1"),
    };

    private void HandleDelete()
    {
        if (_data is null) return;
        _isDeleting = true;
        DeletionService.DeleteBirthDetail(_data.Person.Id);
        Nav.NavigateTo("/charts");
    }
}
```

- [ ] **Step 4: Write `Workspace.razor.css`** — a two-column layout (`ws-cols` → hero left, rail right; stack under 900px), spacing, and the small header/actions rows. Match `ChartWorkspace.razor.css`'s spacing scale. Minimum:

```css
.ws-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: .5rem; }
.ws-back, .ws-print { color: var(--accent); text-decoration: none; font-size: 13px; }
.ws-actions { display: flex; gap: .75rem; align-items: center; }
.ws-comp { margin: 0 0 1rem; padding: .75rem; border: 1px solid var(--paper-line); border-radius: .4rem; }
.ws-cols { display: grid; grid-template-columns: minmax(0, 360px) 1fr; gap: 1.5rem; align-items: start; }
.ws-hero { display: flex; flex-direction: column; gap: 1rem; }
.ws-ppt { margin-top: 1.5rem; }
.ws-ppt-head, .ws-dasha-head { display: flex; justify-content: space-between; align-items: baseline; }
.ws-dasha { margin-top: 1.5rem; }
@media (max-width: 900px) { .ws-cols { grid-template-columns: 1fr; } }
```

- [ ] **Step 5: Delete the old files.**

```bash
git rm src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor.css
git rm src/Ikiastrro.Web/Components/Shared/TabBar.razor src/Ikiastrro.Web/Components/Shared/TabBar.razor.css
```

- [ ] **Step 6: Green.** Build → 0/0 (the `GridPlanetGlyph` 5-arg ctor from Task 1.7 is what `D1PlanetsBySign` uses — confirm it matches). `dotnet run --project src/Ikiastrro.Web`, `/charts/1`:
  - Header shows name, `22 Apr 1981 · 5:30 AM (+05:30) · Chennai, India`, Chara Karakas `AK Ra · AmK Ve · BK Sa · MK Ju · PiK Su · PK Mo · GK Ma · DK Me`.
  - Hero D1 grid renders with `PlanetChip` glyphs + special-point corner labels (`AL` in Capricorn, `HL` in Pisces, `Gk`/`Md` in Libra).
  - `?view=wheel` → SVG wheel with markers.
  - Rail: 5 Disclosure groups; Shadvarga open showing D1·hero, D2-US, D3, D9, D12, D30 tiles; every tile links to `/charts/1/varga/<code>`.
  - Combined D1⊕D9 disclosure opens and matches `docs/artifacts/reference-charts/D1-D9-CombinedChart.png` for occupancy (data parity, not pixels).
  - Positions table has the Karaka column; `?tech=1` adds Speed / Ecl lat / Varga long.
  - Dasha strip renders; "Full timing →" links to `/charts/1/timing` (404 until Phase 4 — acceptable, note it).
  - `/charts/999` → "No saved chart found".

- [ ] **Step 7: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 8: Commit.**

```bash
git add -A src/Ikiastrro.Web/Components/Pages/Workspace.razor src/Ikiastrro.Web/Components/Pages/Workspace.razor.css
git commit -m "feat(web): varga-centric Workspace page; delete ChartWorkspace + TabBar"
```

---

# Phase 3 — VargaView

---

### Task 3.1: `HouseLordshipTable` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/HouseLordshipTable.razor` + `.css`

**Interfaces:**
- Consumes: `ChartHouseLord`.
- Produces: `<HouseLordshipTable Rows="IReadOnlyList<ChartHouseLord>" />`. Columns: House · Sign · Lord · Placed in (Lagna) · Placed in (Chandra) · Lord's dignity (dignity pill via `ChartViewModel.DignityToken`). Rows ordered by `HouseNumber`.

- [ ] **Step 1: Establish the check.** No `HouseLordshipTable.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* House lordship for one chart — the classical "lord of the Nth house is placed in…" reading. *@
<div class="hlt-scroll">
    <table class="hlt">
        <thead>
            <tr><th>House</th><th>Sign</th><th>Lord</th><th>Placed in (Lagna)</th><th>Placed in (Chandra)</th><th>Lord's dignity</th></tr>
        </thead>
        <tbody>
            @foreach (var r in Rows.OrderBy(r => r.HouseNumber))
            {
                <tr>
                    <td class="mono">@r.HouseNumber</td>
                    <td>@Disp(r.HouseSign)</td>
                    <td>@r.LordPlanet</td>
                    <td>House @r.LordPlacedInHouseFromLagna, @Disp(r.LordPlacedInSign)</td>
                    <td class="mono">House @r.LordPlacedInHouseFromMoon</td>
                    <td>
                        @if (r.LordDignityStatus is null) { <span class="muted">—</span> }
                        else { <span class="pill"><span class="dot" style="background:var(--dignity-@ChartViewModel.DignityToken(r.LordDignityStatus))"></span>@r.LordDignityStatus</span> }
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<ChartHouseLord> Rows { get; set; } = Array.Empty<ChartHouseLord>();
    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;
}
```

- [ ] **Step 3: CSS.**

```css
.hlt-scroll { overflow-x: auto; }
.hlt { width: 100%; border-collapse: collapse; }
.hlt th, .hlt td { padding: .35rem .6rem; border-bottom: 1px solid var(--paper-line); text-align: left; white-space: nowrap; }
.hlt th { color: var(--muted-text); font-weight: 600; }
.hlt .mono { font-variant-numeric: tabular-nums; }
.hlt .pill { display: inline-flex; align-items: center; gap: .3rem; }
.hlt .dot { width: .5rem; height: .5rem; border-radius: 50%; display: inline-block; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/HouseLordshipTable.razor src/Ikiastrro.Web/Components/Charts/HouseLordshipTable.razor.css
git commit -m "feat(web): add HouseLordshipTable component"
```

---

### Task 3.2: `ConjunctionsTable` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/ConjunctionsTable.razor` + `.css`

**Interfaces:**
- Consumes: `ChartConjunction`.
- Produces: `<ConjunctionsTable Rows="IReadOnlyList<ChartConjunction>" />`. Columns: Pair (`Planet1 · Planet2`) · Sign · House · Separation (`DegreeSeparation` → `"4.26°"`, or `"—"` when null — it is null for every varga). `EmptyState` when `Rows` is empty.

- [ ] **Step 1: Establish the check.** No `ConjunctionsTable.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Graha Yuti — pairs sharing a sign. DegreeSeparation is only meaningful for D1 (null in vargas). *@
@if (Rows.Count == 0)
{
    <EmptyState Message="No conjunctions in this chart." />
}
else
{
    <div class="cjt-scroll">
        <table class="cjt">
            <thead><tr><th>Pair</th><th>Sign</th><th>House</th><th>Separation</th></tr></thead>
            <tbody>
                @foreach (var r in Rows)
                {
                    <tr>
                        <td>@r.Planet1 · @r.Planet2</td>
                        <td>@Disp(r.Sign)</td>
                        <td class="mono">@r.HouseNumberFromLagna</td>
                        <td class="mono">@(r.DegreeSeparation is decimal d ? $"{d:0.00}°" : "—")</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

@code {
    [Parameter, EditorRequired] public IReadOnlyList<ChartConjunction> Rows { get; set; } = Array.Empty<ChartConjunction>();
    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;
}
```

- [ ] **Step 3: CSS.**

```css
.cjt-scroll { overflow-x: auto; }
.cjt { width: 100%; border-collapse: collapse; }
.cjt th, .cjt td { padding: .35rem .6rem; border-bottom: 1px solid var(--paper-line); text-align: left; white-space: nowrap; }
.cjt th { color: var(--muted-text); font-weight: 600; }
.cjt .mono { font-variant-numeric: tabular-nums; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/ConjunctionsTable.razor src/Ikiastrro.Web/Components/Charts/ConjunctionsTable.razor.css
git commit -m "feat(web): add ConjunctionsTable component"
```

---

### Task 3.3: `VargottamaStrip` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/VargottamaStrip.razor` + `.css`

**Interfaces:**
- Consumes: `ChartKeyDetail` (D1 grahas + Dn grahas), `ChartViewModel.PlanetGlyph`.
- Produces: `<VargottamaStrip D1Grahas="IReadOnlyList<ChartKeyDetail>" DnGrahas="IReadOnlyList<ChartKeyDetail>" DnCode="string" />`. One chip per graha (exclude Ascendant); chip is **lit** (`--vargottama` border + bg tint) when the graha's Dn `Sign` equals its D1 `Sign`. Label: for `DnCode == "D9"` the lit state is "Vargottama"; for any other code the tooltip reads "same sign as D1".

- [ ] **Step 1: Establish the check.** No `VargottamaStrip.razor` exists.

- [ ] **Step 2: Write the component.**

```razor
@* Lights each graha whose Dn sign == its D1 sign. True "Vargottama" only for D9; for other vargas
   it is "same-sign-as-D1", labelled as such in the tooltip. *@
<div class="vgs">
    <span class="vgs-lbl">@(DnCode == "D9" ? "Vargottama" : "Same sign as D1"):</span>
    @foreach (var g in Chips())
    {
        <span class="vgs-chip @(g.Lit ? "lit" : "")" title="@(g.Lit ? Tip : "different sign")">
            @ChartViewModel.PlanetGlyph(g.Planet)
        </span>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<ChartKeyDetail> D1Grahas { get; set; } = Array.Empty<ChartKeyDetail>();
    [Parameter, EditorRequired] public IReadOnlyList<ChartKeyDetail> DnGrahas { get; set; } = Array.Empty<ChartKeyDetail>();
    [Parameter, EditorRequired] public string DnCode { get; set; } = "";

    private string Tip => DnCode == "D9" ? "Vargottama (D9 sign = D1 sign)" : $"{DnCode} sign = D1 sign";

    private IEnumerable<(string Planet, bool Lit)> Chips()
    {
        var d1 = D1Grahas.Where(k => k.Planet != "Ascendant").ToDictionary(k => k.Planet, k => k.Sign);
        foreach (var k in DnGrahas.Where(k => k.Planet != "Ascendant"))
            yield return (k.Planet, d1.TryGetValue(k.Planet, out var s) && s == k.Sign);
    }
}
```

- [ ] **Step 3: CSS.**

```css
.vgs { display: flex; flex-wrap: wrap; gap: .4rem; align-items: center; }
.vgs-lbl { color: var(--muted-text); font-size: 13px; }
.vgs-chip {
    font-size: 12px; font-weight: 600; padding: .1rem .4rem; border-radius: .3rem;
    border: 1px solid var(--paper-line); color: var(--ink-faint);
}
.vgs-chip.lit { border-color: var(--vargottama); color: var(--ink); background: color-mix(in srgb, var(--vargottama) 18%, transparent); }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/VargottamaStrip.razor src/Ikiastrro.Web/Components/Charts/VargottamaStrip.razor.css
git commit -m "feat(web): add VargottamaStrip component"
```

---

### Task 3.4: `VargaView` page

**Files:**
- Create: `src/Ikiastrro.Web/Components/Pages/VargaView.razor` + `.css`

**Interfaces:**
- Consumes: `WorkspaceData.Load`, `WorkspaceHeader` (reused for the identity band — pass the person + D1), `ChartFrame`, `SouthIndianGrid`, `PolarWheel`, `PlanetPositionsTable` (`Variant="varga"`), `HouseLordshipTable`, `ConjunctionsTable`, `VargottamaStrip`, `Disclosure`, `EmptyState`, `VargaBundles.RailOrder`.
- Produces: route `/charts/{Id:int}/varga/{Code}` with `?view=` and `?tech=`. `Code` is a string route segment (accepts `D2-US` — the hyphen is fine in a path segment). Unknown `Code` or `Id` → `EmptyState`.

- [ ] **Step 1: Establish the check.** `/charts/1/varga/D9` → 404 (no such route).

- [ ] **Step 2: Write `VargaView.razor`.**

```razor
@page "/charts/{Id:int}/varga/{Code}"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject ChartHouseLordsRepository ChartHouseLordsRepo
@inject ChartAspectsRepository ChartAspectsRepo
@inject ChartConjunctionsRepository ChartConjunctionsRepo
@inject ChartTypeRepository ChartTypeRepo
@using Ikiastrro.Web.Components.Workspace

<PageTitle>@Code — @(_data?.Person.Name ?? "Chart") — ikiastrro</PageTitle>

@if (_data is null || !_data.Charts.TryGetValue(Code, out var dn))
{
    <p class="vv-back"><a href="@($"/charts/{Id}")">← Workspace</a></p>
    <EmptyState Message="@($"{Code} is not computed for this person.")" Cli="backfill-charts" />
}
else
{
    <div class="vv-nav">
        <a href="@($"/charts/{Id}")">← Workspace</a>
        <span class="vv-prevnext">
            @if (Prev() is string p) { <a href="@($"/charts/{Id}/varga/{p}?view={View}")">‹ @p</a> }
            <b>@Code · @dn.SanskritName · @(dn.VargaMethod ?? "—")</b>
            @if (Next() is string n) { <a href="@($"/charts/{Id}/varga/{n}?view={View}")">@n ›</a> }
        </span>
    </div>

    <WorkspaceHeader Person="_data.Person" D1="@(_data.Charts["D1"])" ComputationOpen="false"
                     OnToggleComputation="_ => {}" />

    <ChartFrame View="@View" BaseHref="@($"/charts/{Id}/varga/{Code}")">
        <GridContent>
            <SouthIndianGrid AscendantSign="@dn.AscendantSign"
                             PlanetsBySign="PlanetsBySign(dn)"
                             AspectedByGlyphs="ChartViewModel.BuildAspectedByGlyphs(dn.KeyDetails, dn.Aspects)"
                             SpecialPointLabels="SpecialLabels(dn)"
                             CenterTitle="@($"{Code} · {dn.SanskritName}")">
                <CenterMeta><b>Lagna</b> @Disp(dn.AscendantSign)</CenterMeta>
            </SouthIndianGrid>
        </GridContent>
        <WheelContent><PolarWheel Points="WheelPoints(dn)" /></WheelContent>
    </ChartFrame>

    <section class="vv-vg"><VargottamaStrip D1Grahas="@(_data.Charts["D1"].Grahas)" DnGrahas="dn.Grahas" DnCode="@Code" /></section>

    <section class="vv-ppt">
        <div class="vv-ppt-head">
            <h2>Planet positions — @Code</h2>
            <SegmentedToggle ActiveKey="@(Tech ? "on" : "off")" Segments="TechSegments()" />
        </div>
        <PlanetPositionsTable Rows="Rows(dn)" LagnaSign="@dn.AscendantSign" Variant="varga" ShowTechnical="Tech" />
    </section>

    <Disclosure Title="House lordship & conjunctions" Open="_tablesOpen" OnToggle="v => _tablesOpen = v">
        <HouseLordshipTable Rows="dn.HouseLords" />
        <div style="height:1rem"></div>
        <ConjunctionsTable Rows="dn.Conjunctions" />
    </Disclosure>
}

@code {
    [Parameter] public int Id { get; set; }
    [Parameter] public string Code { get; set; } = "";
    [Parameter, SupplyParameterFromQuery(Name = "view")] public string? ViewParam { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "tech")] public string? TechParam { get; set; }

    private WorkspaceData? _data;
    private bool _tablesOpen;

    private string View => ViewParam == "wheel" ? "wheel" : "grid";
    private bool Tech => TechParam == "1";

    protected override void OnParametersSet() =>
        _data = WorkspaceData.Load(Id, BirthDetailsRepo, ChartResultsRepo, ChartKeyDetailsRepo,
            ChartHouseLordsRepo, ChartAspectsRepo, ChartConjunctionsRepo, ChartTypeRepo.GetAll());

    private string? Prev()
    {
        var i = VargaBundles.RailOrder.ToList().IndexOf(Code);
        return i > 0 ? VargaBundles.RailOrder[i - 1] : null;
    }
    private string? Next()
    {
        var order = VargaBundles.RailOrder.ToList();
        var i = order.IndexOf(Code);
        return i >= 0 && i < order.Count - 1 ? order[i + 1] : null;
    }

    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;

    private IReadOnlyDictionary<string, IReadOnlyList<GridPlanetGlyph>> PlanetsBySign(LoadedChart lc) =>
        lc.Grahas.Where(k => k.Planet != "Ascendant")
            .GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GridPlanetGlyph>)g.Select(k => new GridPlanetGlyph(
                k.Planet, ChartViewModel.PlanetGlyph(k.Planet), ChartViewModel.DignityToken(k.DignityStatus),
                k.IsRetrograde ?? false, k.IsCombust ?? false)).ToList());

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> SpecialLabels(LoadedChart lc) =>
        lc.SpecialPoints.GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet switch
            { "Gulika" => "Gk", "Maandi" => "Md", _ => k.Planet }).ToList());

    private static IReadOnlyList<PlanetRow> Rows(LoadedChart lc) =>
        ChartViewModel.BuildPlanetRows(lc.KeyDetails, lc.HouseLords, lc.Aspects);

    private IReadOnlyList<PolarWheel.Point> WheelPoints(LoadedChart lc) =>
        lc.KeyDetails.Select(k => new PolarWheel.Point(
            k.Planet, k.VargaLongitudeDegrees,
            k.PointKind != "Graha" || k.Planet == "Ascendant" ? "--accent"
              : $"--planet-{k.Planet.ToLowerInvariant()}")).ToList();

    private IReadOnlyList<SegmentedToggle.Segment> TechSegments() => new[]
    {
        new SegmentedToggle.Segment("off", "Clean", $"/charts/{Id}/varga/{Code}?view={View}&tech=0"),
        new SegmentedToggle.Segment("on",  "Technical", $"/charts/{Id}/varga/{Code}?view={View}&tech=1"),
    };
}
```

- [ ] **Step 3: Write `VargaView.razor.css`** — spacing for `.vv-nav`, `.vv-prevnext` (flex, gap), `.vv-ppt-head` (flex space-between). Link colour `var(--accent)`.

```css
.vv-nav { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
.vv-nav a { color: var(--accent); text-decoration: none; }
.vv-prevnext { display: flex; gap: .75rem; align-items: baseline; }
.vv-vg { margin: 1rem 0; }
.vv-ppt { margin-top: 1rem; }
.vv-ppt-head { display: flex; justify-content: space-between; align-items: baseline; }
```

- [ ] **Step 4: Green.** Build → 0/0. `dotnet run`:
  - `/charts/1/varga/D9` — header identity band; D9 grid; `VargottamaStrip` labelled "Vargottama:" lighting the grahas whose D9 sign == D1 sign (per the golden record: check Sun→Ge, Moon→Vi etc. against `Rammy_Jagannatha.txt` Navamsa column — lit only where Dn==D1); positions table `Variant="varga"` shows the varga degree (`DegreesInSignDisplay`, non-zero — migration 13 un-gated it), no Nakshatra column; prev/next chevrons walk `D12 ‹ D9 › D30` (Shadvarga order from `VargaBundles.RailOrder`).
  - `/charts/1/varga/D2-US` route resolves (hyphen in segment OK).
  - `/charts/1/varga/D999` → EmptyState.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/VargaView.razor src/Ikiastrro.Web/Components/Pages/VargaView.razor.css
git commit -m "feat(web): add VargaView page with prev/next rail navigation"
```

---

# Phase 4 — Timing

---

### Task 4.1: `GocharaRepository`

**Files:**
- Create: `src/Ikiastrro.Data/GocharaRepository.cs`
- Modify: `src/Ikiastrro.Web/Program.cs`

**Interfaces:**
- Consumes: `PlanetSignTransitEventsRepository.GetSnapshot(PlanetName, DateTime)` (returns `PlanetTransitSnapshot?`).
- Produces:
  ```csharp
  namespace Ikiastrro.Data;
  public sealed class GocharaRepository
  {
      public GocharaRepository(PlanetSignTransitEventsRepository transits);
      // Saturn, Jupiter, Rahu — the 3 slow grahas the transit table covers (Mars is out of scope).
      public IReadOnlyList<PlanetTransitSnapshot> GetSnapshots(DateTime asOfUtc);
  }
  ```

- [ ] **Step 1: Establish the check.** No `GocharaRepository.cs` exists.

- [ ] **Step 2: Write the repo.**

```csharp
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>Current sidereal position of the slow grahas (Saturn / Jupiter / Rahu) for the Gochara
/// panel. Thin wrapper over PlanetSignTransitEventsRepository — Mars was outside the transit-table
/// backfill scope, so it is deliberately not here.</summary>
public sealed class GocharaRepository
{
    private static readonly PlanetName[] Slow = { PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Rahu };
    private readonly PlanetSignTransitEventsRepository _transits;

    public GocharaRepository(PlanetSignTransitEventsRepository transits) => _transits = transits;

    public IReadOnlyList<PlanetTransitSnapshot> GetSnapshots(DateTime asOfUtc) =>
        Slow.Select(p => _transits.GetSnapshot(p, asOfUtc))
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();
}
```

- [ ] **Step 3: Register in DI.** In `Program.cs`, add after `builder.Services.AddScoped<PlanetaryStateRepository>();`:

```csharp
builder.Services.AddScoped<PlanetSignTransitEventsRepository>();
builder.Services.AddScoped<GocharaRepository>();
```

  (`PlanetSignTransitEventsRepository` is not currently registered in Web — it must be, for `GocharaRepository` to resolve.)

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Data/GocharaRepository.cs src/Ikiastrro.Web/Program.cs
git commit -m "feat(data): add GocharaRepository over PlanetSignTransitEvents"
```

---

### Task 4.2: `GocharaPanel` component

**Files:**
- Create: `src/Ikiastrro.Web/Components/Charts/GocharaPanel.razor` + `.css`

**Interfaces:**
- Consumes: `PlanetTransitSnapshot` (has `Planet`, `SignId`, `InSignSinceUtc`, `MotionDirection`, `NextChangeUtc`), the natal Moon sign (to count house-from-natal-Moon), `AstroMath.CountFromSignToSign`.
- Produces: `<GocharaPanel Snapshots="IReadOnlyList<PlanetTransitSnapshot>" NatalMoonSign="string" />`. Per graha: current sign (map `SignId` → name — see note), house from natal Moon, "in since" date, "next change" date. `EmptyState` (Cli `backfill-planet-transits`) when `Snapshots` is empty.
- **`SignId` → name:** `PlanetTransitSnapshot` carries `byte SignId` (1..12, FK to `tbl_SignAttributes`), not a sign name. The `ZodiacName` enum order is the standard Aries..Pisces. Map with `(ZodiacName)(SignId - 1)` **only if** the enum's numeric order matches `tbl_SignAttributes.Id`. Verify in Step 1; if it does not match, add a 12-entry `static readonly string[]` in the component keyed by `SignId`.

- [ ] **Step 1: Establish the check + verify the SignId mapping.** No `GocharaPanel.razor` exists. Run:

```
sqlcmd -S localhost -E -d ikiastrro -h -1 -W -s "|" -Q "SET NOCOUNT ON; SELECT Id, type_en_name FROM dbo.tbl_SignAttributes ORDER BY Id;"
```

  Record the 12 rows. Compare to `enum ZodiacName` (`src/Ikiastrro.Core/Engines/Astronomy/`). Use whichever mapping matches; the component below assumes an explicit array (safe regardless).

- [ ] **Step 2: Write the component.**

```razor
@* Gochara — where the slow grahas sit sidereally now, relative to the natal Moon. Sa/Ju/Ra only. *@
@if (Snapshots.Count == 0)
{
    <EmptyState Message="Transit events not backfilled." Cli="backfill-planet-transits" />
}
else
{
    <table class="gp">
        <thead><tr><th>Graha</th><th>Sign now</th><th>House from Moon</th><th>In since</th><th>Next change</th></tr></thead>
        <tbody>
            @foreach (var s in Snapshots)
            {
                var sign = SignName(s.SignId);
                <tr>
                    <td>@s.Planet</td>
                    <td>@Disp(sign) <span class="gp-dir">(@s.MotionDirection)</span></td>
                    <td class="mono">@HouseFromMoon(sign)</td>
                    <td class="mono">@s.InSignSinceUtc.ToString("d MMM yyyy")</td>
                    <td class="mono">@(s.NextChangeUtc?.ToString("d MMM yyyy") ?? "—")</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    [Parameter, EditorRequired] public IReadOnlyList<PlanetTransitSnapshot> Snapshots { get; set; } = Array.Empty<PlanetTransitSnapshot>();
    [Parameter, EditorRequired] public string NatalMoonSign { get; set; } = "";

    // Index = SignId (1..12). Confirm order against tbl_SignAttributes in Step 1.
    private static readonly string[] SignById =
        { "", "Aries","Taurus","Gemini","Cancer","Leo","Virgo","Libra","Scorpio","Sagittarius","Capricornus","Aquarius","Pisces" };
    private static string SignName(byte id) => id >= 1 && id <= 12 ? SignById[id] : "?";

    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;

    private string HouseFromMoon(string sign)
    {
        if (!Enum.TryParse<ZodiacName>(NatalMoonSign, out var moon) || !Enum.TryParse<ZodiacName>(sign, out var target))
            return "—";
        return AstroMath.CountFromSignToSign(moon, target).ToString();
    }
}
```

- [ ] **Step 3: CSS.**

```css
.gp { width: 100%; border-collapse: collapse; }
.gp th, .gp td { padding: .35rem .6rem; border-bottom: 1px solid var(--paper-line); text-align: left; white-space: nowrap; }
.gp th { color: var(--muted-text); font-weight: 600; }
.gp .mono { font-variant-numeric: tabular-nums; }
.gp-dir { color: var(--muted-text); font-size: 12px; }
```

- [ ] **Step 4: Green.** Build → 0/0.
- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.
- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Charts/GocharaPanel.razor src/Ikiastrro.Web/Components/Charts/GocharaPanel.razor.css
git commit -m "feat(web): add GocharaPanel component"
```

---

### Task 4.3: `Timing` page

**Files:**
- Create: `src/Ikiastrro.Web/Components/Pages/Timing.razor` + `.css`

**Interfaces:**
- Consumes: `BirthDetailsRepository`, `DashaPeriodsRepository.GetTreeByBirthDetailId`, `SadeSatiRepository.GetByBirthDetailId`, `GocharaRepository.GetSnapshots`, `ChartKeyDetailsRepository` (for the natal Moon sign — read D1's `GetByBirthDetailId` and pick `Planet == "Moon"` with the D1 `ChartResultId`; simpler: reuse `WorkspaceData.Load` and read `Charts["D1"].MoonSign`), `DashaTimeline`, `SadeSatiTable`, `GocharaPanel`, `EmptyState`.
- Produces: route `/charts/{Id:int}/timing`.

- [ ] **Step 1: Establish the check.** `/charts/1/timing` → 404.

- [ ] **Step 2: Write `Timing.razor`.**

```razor
@page "/charts/{Id:int}/timing"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject ChartHouseLordsRepository ChartHouseLordsRepo
@inject ChartAspectsRepository ChartAspectsRepo
@inject ChartConjunctionsRepository ChartConjunctionsRepo
@inject ChartTypeRepository ChartTypeRepo
@inject DashaPeriodsRepository DashaPeriodsRepo
@inject SadeSatiRepository SadeSatiRepo
@inject GocharaRepository GocharaRepo
@using Ikiastrro.Web.Components.Workspace

<PageTitle>Timing — @(_data?.Person.Name ?? "Chart") — ikiastrro</PageTitle>

@if (_data is null)
{
    <EmptyState Message="No saved chart found for that person." />
}
else
{
    <p class="tm-back"><a href="@($"/charts/{Id}")">← Workspace</a></p>
    <h1>@_data.Person.Name — timing</h1>

    <section class="tm-sec">
        <h2>Vimshottari Dasha</h2>
        @if (_dasha.Count > 0) { <DashaTimeline Roots="_dasha" /> }
        else { <EmptyState Message="No Dasha computed." Cli="@($"compute-dasha {_data.Person.Name}")" /> }
    </section>

    <section class="tm-sec">
        <h2>Sade Sati, Kantaka &amp; Ashtama</h2>
        <SadeSatiTable Periods="_sadeSati"
                       BirthDate="_data.Person.DateOfBirth.ToDateTime(_data.Person.EffectiveTimeOfBirth)" />
    </section>

    <section class="tm-sec">
        <h2>Gochara — slow grahas now</h2>
        <GocharaPanel Snapshots="_gochara" NatalMoonSign="@(_data.Charts.GetValueOrDefault("D1")?.MoonSign ?? "")" />
    </section>

    <p><a href="@($"/charts/{Id}/life-weeks")">Life in weeks →</a></p>
}

@code {
    [Parameter] public int Id { get; set; }

    private WorkspaceData? _data;
    private IReadOnlyList<DashaPeriodRecord> _dasha = Array.Empty<DashaPeriodRecord>();
    private IReadOnlyList<SadeSatiPeriod> _sadeSati = Array.Empty<SadeSatiPeriod>();
    private IReadOnlyList<PlanetTransitSnapshot> _gochara = Array.Empty<PlanetTransitSnapshot>();

    protected override void OnParametersSet()
    {
        _data = WorkspaceData.Load(Id, BirthDetailsRepo, ChartResultsRepo, ChartKeyDetailsRepo,
            ChartHouseLordsRepo, ChartAspectsRepo, ChartConjunctionsRepo, ChartTypeRepo.GetAll());
        if (_data is null) return;
        _dasha = DashaPeriodsRepo.GetTreeByBirthDetailId(Id);
        _sadeSati = SadeSatiRepo.GetByBirthDetailId(Id);
        _gochara = GocharaRepo.GetSnapshots(DateTime.UtcNow);
    }
}
```

- [ ] **Step 3: CSS.**

```css
.tm-back a { color: var(--accent); text-decoration: none; }
.tm-sec { margin: 1.5rem 0; }
.tm-sec h2 { margin-bottom: .5rem; }
```

- [ ] **Step 4: Green.** Build → 0/0. `/charts/1/timing`:
  - Full Dasha tree (Sat maha open on the current period).
  - `SadeSatiTable` — Moon in Scorpio → the expected Dhaiya chain (compare to `Rammy_Jagannatha.txt`: Moon `7 Sc 17'`).
  - `GocharaPanel` — 3 rows Sa/Ju/Ra with current sign + house-from-Moon, OR the EmptyState if transits unbackfilled (run `precheck-planet-transits` to know which; either outcome passes this task).
  - "Life in weeks →" links to the existing `/charts/1/life-weeks`.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/Timing.razor src/Ikiastrro.Web/Components/Pages/Timing.razor.css
git commit -m "feat(web): add Timing page (Dasha + Sade Sati + Gochara)"
```

---

# Phase 5 — Non-workspace pages

---

### Task 5.1: `Home` rebuild — search + `MiniGrid` rows

**Files:**
- Modify: `src/Ikiastrro.Web/Components/Pages/Home.razor`
- Create: `src/Ikiastrro.Web/Components/Pages/Home.razor.css`

**Interfaces:**
- Consumes: `BirthDetailsRepository.GetAll()`, and to render each row's D1 `MiniGrid` — `ChartResultsRepository` + `ChartKeyDetailsRepository` (batch: for the listed people, load D1 grahas). To keep it one query, `ChartKeyDetailsRepository` has no "all people D1" method — call `WorkspaceData.Load` per person is wasteful for a list. Instead: for each person id, `ChartResultsRepo.GetByBirthDetailId(id)` → D1 `ChartResultId` → `ChartKeyDetailsRepo.GetByChartResultId(rid)`. Acceptable for a saved-people list (tens of rows).
- Produces: route `/` — a name-filter `<input>` + person rows (`MiniGrid` + name/DOB/place), "＋ Add someone" button. The old `<select>` is removed.

- [ ] **Step 1: Establish the check.** `/` shows a bare `<select>` dropdown.

- [ ] **Step 2: Write `Home.razor`.**

```razor
@page "/"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject NavigationManager Nav

<PageTitle>ikiastrro</PageTitle>

<h1>ikiastrro</h1>
<p>Pick a saved person to open their charts, or add someone new.</p>

@if (_people.Count == 0)
{
    <p class="muted">No charts saved yet — add your first person.</p>
}
else
{
    <input class="home-search" type="search" placeholder="Filter by name…" @bind="_filter" @bind:event="oninput" />
    <div class="home-rows">
        @foreach (var row in Filtered())
        {
            <a class="home-row" href="@($"/charts/{row.Person.Id}")">
                <MiniGrid PlanetsBySign="row.Glyphs" LagnaSign="@row.Lagna" />
                <span class="home-meta">
                    <b>@row.Person.Name</b>
                    <span>@row.Person.DateOfBirth.ToString("d MMM yyyy") · @row.Person.EffectiveTimeOfBirth.ToString("h:mm tt")</span>
                    <span>@row.Person.PlaceCity, @row.Person.PlaceCountry</span>
                </span>
            </a>
        }
    </div>
}

<button class="btn-add" type="button" @onclick='() => Nav.NavigateTo("/add")'>＋ Add someone</button>

@code {
    private record Row(BirthDetails Person, string Lagna, IReadOnlyDictionary<string, IReadOnlyList<string>> Glyphs);

    private readonly List<Row> _people = new();
    private string _filter = "";

    protected override void OnInitialized()
    {
        foreach (var p in BirthDetailsRepo.GetAll())
        {
            var d1 = ChartResultsRepo.GetByBirthDetailId(p.Id).FirstOrDefault(r => r.ChartType == "D1");
            if (d1 is null) { _people.Add(new Row(p, "", Empty)); continue; }
            var kd = ChartKeyDetailsRepo.GetByChartResultId(d1.Id);
            var lagna = kd.FirstOrDefault(k => k.Planet == "Ascendant")?.Sign ?? "";
            var glyphs = kd.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant")
                .GroupBy(k => k.Sign)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet).ToList());
            _people.Add(new Row(p, lagna, glyphs));
        }
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Empty =
        new Dictionary<string, IReadOnlyList<string>>();

    private IEnumerable<Row> Filtered() => string.IsNullOrWhiteSpace(_filter)
        ? _people
        : _people.Where(r => r.Person.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 3: Write `Home.razor.css`.**

```css
.home-search {
    display: block; width: 100%; max-width: 360px; margin: 1rem 0;
    padding: .4rem .6rem; background: var(--paper-raised); border: 1px solid var(--paper-line);
    border-radius: .4rem; color: var(--ink); font: inherit;
}
.home-rows { display: flex; flex-direction: column; gap: .75rem; }
.home-row {
    display: flex; gap: 1rem; align-items: center; text-decoration: none; color: inherit;
    padding: .5rem; border: 1px solid var(--paper-line); border-radius: .5rem;
}
.home-row:hover { border-color: var(--accent); }
.home-meta { display: flex; flex-direction: column; gap: .15rem; }
.home-meta b { color: var(--ink); }
.home-meta span { color: var(--ink-soft); font-size: 13px; }
.btn-add {
    margin-top: 1.25rem; padding: .5rem .9rem; background: var(--accent); color: var(--nav-btn-fg);
    border: 0; border-radius: .4rem; font: inherit; font-weight: 600; cursor: pointer;
}
```

- [ ] **Step 4: Green.** Build → 0/0. `/` — person rows each with a D1 `MiniGrid` thumbnail; typing in the filter narrows the list; clicking a row opens `/charts/{id}`; "＋ Add someone" → `/add`.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/Home.razor src/Ikiastrro.Web/Components/Pages/Home.razor.css
git commit -m "feat(web): rebuild Home with name filter + MiniGrid person rows"
```

---

### Task 5.2: `Charts` → `SavedCharts` rebuild via `DataTable<T>`

**Files:**
- Create: `src/Ikiastrro.Web/Components/Pages/SavedCharts.razor` + `.css`
- Delete: `src/Ikiastrro.Web/Components/Pages/Charts.razor` + `.css`

**Interfaces:**
- Consumes: `DataTable<TRow>`, `MiniGrid`, `ConfirmDialog`, `DeleteIconButton`, `BirthDetailsRepository`, `BirthDetailDeletionService`, `ChartResultsRepository`, `ChartKeyDetailsRepository`.
- Produces: route `/charts` (unchanged path; component renamed). Sortable Name / DOB / Place columns, a `MiniGrid` thumbnail column, delete per row.

- [ ] **Step 1: Establish the check.** `/charts` renders `Charts.razor` (plain unsorted table).

- [ ] **Step 2: Write `SavedCharts.razor`.**

```razor
@page "/charts"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject BirthDetailDeletionService DeletionService

<PageTitle>Saved Charts — ikiastrro</PageTitle>
<h1>Saved charts</h1>

@if (_rows.Count == 0)
{
    <p class="muted">No charts saved yet.</p>
}
else
{
    <DataTable TRow="Row" Rows="_rows" InitialSortHeader="Name" Columns="Cols" />
}

<ConfirmDialog IsOpen="@(_pendingDelete is not null)"
               Message="@($"Delete {_pendingDelete?.Person.Name}'s chart and all computed data? This can't be undone.")"
               IsBusy="_isDeleting" OnConfirm="HandleDelete" OnCancel="() => _pendingDelete = null" />

@code {
    private record Row(BirthDetails Person, string Lagna, IReadOnlyDictionary<string, IReadOnlyList<string>> Glyphs);

    private readonly List<Row> _rows = new();
    private Row? _pendingDelete;
    private bool _isDeleting;

    private IReadOnlyList<DataTable<Row>.Column> Cols => new[]
    {
        new DataTable<Row>.Column("Chart", _ => 0, r => @<MiniGrid PlanetsBySign="@r.Glyphs" LagnaSign="@r.Lagna" Href="@($"/charts/{r.Person.Id}")" />),
        new DataTable<Row>.Column("Name", r => r.Person.Name, r => @<a href="@($"/charts/{r.Person.Id}")">@r.Person.Name</a>),
        new DataTable<Row>.Column("Date of Birth", r => r.Person.DateOfBirth, r => @<text>@r.Person.DateOfBirth.ToString("d MMM yyyy")</text>),
        new DataTable<Row>.Column("Place", r => r.Person.PlaceCity, r => @<text>@r.Person.PlaceCity, @r.Person.PlaceCountry</text>),
        new DataTable<Row>.Column("", _ => 0, r => @<DeleteIconButton Title="@($"Delete {r.Person.Name}")" OnClick="() => _pendingDelete = r" />),
    };

    protected override void OnInitialized() => Load();

    private void Load()
    {
        _rows.Clear();
        foreach (var p in BirthDetailsRepo.GetAll())
        {
            var d1 = ChartResultsRepo.GetByBirthDetailId(p.Id).FirstOrDefault(r => r.ChartType == "D1");
            var kd = d1 is null ? Array.Empty<ChartKeyDetail>() : ChartKeyDetailsRepo.GetByChartResultId(d1.Id);
            var lagna = kd.FirstOrDefault(k => k.Planet == "Ascendant")?.Sign ?? "";
            var glyphs = kd.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant")
                .GroupBy(k => k.Sign)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet).ToList());
            _rows.Add(new Row(p, lagna, glyphs));
        }
    }

    private void HandleDelete()
    {
        if (_pendingDelete is null) return;
        _isDeleting = true;
        DeletionService.DeleteBirthDetail(_pendingDelete.Person.Id);
        _pendingDelete = null;
        _isDeleting = false;
        Load();
    }
}
```

- [ ] **Step 3: CSS** — `SavedCharts.razor.css`: `.muted { color: var(--muted-text); }` plus any table cell alignment. Keep minimal; `DataTable.razor.css` owns the table look.

- [ ] **Step 4: Delete `Charts.razor`.**

```bash
git rm src/Ikiastrro.Web/Components/Pages/Charts.razor src/Ikiastrro.Web/Components/Pages/Charts.razor.css
```

- [ ] **Step 5: Green.** Build → 0/0. `/charts` — sortable table; clicking "Name" / "Date of Birth" / "Place" headers re-sorts; each row has a `MiniGrid` thumbnail linking to the workspace; delete → `ConfirmDialog` → row disappears.

- [ ] **Step 6: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 7: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/SavedCharts.razor src/Ikiastrro.Web/Components/Pages/SavedCharts.razor.css
git commit -m "feat(web): rebuild Saved Charts via sortable DataTable + MiniGrid; drop Charts.razor"
```

---

### Task 5.3: `Add` restyle + yellow nav pills

**Files:**
- Modify: `src/Ikiastrro.Web/Components/Pages/Add.razor`
- Modify: `src/Ikiastrro.Web/Components/Layout/MainLayout.razor`
- Create: `src/Ikiastrro.Web/Components/Layout/MainLayout.razor.css` (if not present)

**Interfaces:**
- Consumes: nothing new.
- Produces: `MainLayout` nav — "Home" and "Saved Charts" as filled `--accent` pills. `Add.razor` copy line updated; no logic change (`ChartGenerationService.GenerateAll` already generates all 21).

- [ ] **Step 1: Establish the check.** Nav links are plain text; `Add.razor` line reads "generate and store D1 (Rasi) + D9 (Navamsa) charts."

- [ ] **Step 2: Update `Add.razor` copy.** Replace the `<p>Enter birth details to generate and store D1 (Rasi) + D9 (Navamsa) charts.</p>` line with:

```razor
<p>Enter birth details to compute and store all 21 charts (D1 + 20 vargas) and Vimshottari Dasha.</p>
```

- [ ] **Step 3: Update `MainLayout.razor`.** Give the two `NavLink`s a class:

```razor
    <NavLink class="nav-pill" href="/" Match="NavLinkMatch.All">Home</NavLink>
    <NavLink class="nav-pill" href="/charts">Saved Charts</NavLink>
```

- [ ] **Step 4: Write `MainLayout.razor.css`.**

```css
.site-nav { display: flex; align-items: center; gap: .75rem; padding: .6rem 1rem; border-bottom: 1px solid var(--paper-line); }
.brand { margin-right: auto; }
.brand-name { font-weight: 700; color: var(--ink); }
.brand-ded { color: var(--muted-text); font-size: 12px; margin-left: .4rem; }
.nav-pill {
    padding: .3rem .8rem; border-radius: .4rem; text-decoration: none; font-weight: 600;
    background: var(--nav-btn); color: var(--nav-btn-fg);
}
.nav-pill.active { outline: 2px solid var(--accent-line); }
```

- [ ] **Step 5: Green.** Build → 0/0. Every page's top nav shows two yellow pills; `/add` copy mentions "all 21 charts". Add a person end-to-end (or confirm the form still submits) — `GenerateAll` writes 21 `ChartResults`.

- [ ] **Step 6: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 7: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/Add.razor src/Ikiastrro.Web/Components/Layout/MainLayout.razor src/Ikiastrro.Web/Components/Layout/MainLayout.razor.css
git commit -m "feat(web): yellow nav pills + Add copy for all 21 charts"
```

---

# Phase 6 — Print

---

### Task 6.1: `PrintChart` page + `print.css`

**Files:**
- Create: `src/Ikiastrro.Web/Components/Pages/PrintChart.razor` + `.css`
- Create: `src/Ikiastrro.Web/wwwroot/css/print.css`

**Interfaces:**
- Consumes: `WorkspaceData.Load`, `SouthIndianGrid`, `CombinedD1D9Grid`, `MiniGrid`, `PlanetPositionsTable`, `HouseLordshipTable`, `ConjunctionsTable`, `DashaTimeline`, `SadeSatiTable`, `GocharaPanel`, `VargaBundles.RailOrder`, plus `DashaPeriodsRepository`, `SadeSatiRepository`, `GocharaRepository`.
- Produces: route `/charts/{Id:int}/print` — a flat, expanded, dark document. No `Disclosure`, no rail navigation. `print.css` linked from this page only, via `<link rel="stylesheet" href="css/print.css" media="print" />` in the page markup (Blazor allows a `<link>` in a page component's markup; it is hoisted to `<head>` in .NET 8 with `<HeadContent>`). Use `<HeadContent>`.

- [ ] **Step 1: Establish the check.** `/charts/1/print` → 404.

- [ ] **Step 2: Write `PrintChart.razor`.**

```razor
@page "/charts/{Id:int}/print"
@inject BirthDetailsRepository BirthDetailsRepo
@inject ChartResultsRepository ChartResultsRepo
@inject ChartKeyDetailsRepository ChartKeyDetailsRepo
@inject ChartHouseLordsRepository ChartHouseLordsRepo
@inject ChartAspectsRepository ChartAspectsRepo
@inject ChartConjunctionsRepository ChartConjunctionsRepo
@inject ChartTypeRepository ChartTypeRepo
@inject DashaPeriodsRepository DashaPeriodsRepo
@inject SadeSatiRepository SadeSatiRepo
@inject GocharaRepository GocharaRepo
@using Ikiastrro.Web.Components.Workspace

<HeadContent>
    <link rel="stylesheet" href="css/print.css" media="print" />
</HeadContent>

<PageTitle>@(_data?.Person.Name ?? "Chart") — print — ikiastrro</PageTitle>

@if (_data is null)
{
    <EmptyState Message="No saved chart found." />
}
else
{
    var d1 = _data.Charts["D1"];
    <div class="print-doc">
        <header class="print-head">
            <h1>@_data.Person.Name</h1>
            <p>@_data.Person.DateOfBirth.ToString("d MMMM yyyy") · @_data.Person.EffectiveTimeOfBirth.ToString("h:mm tt") (@_data.Person.UtcOffset) · @_data.Person.PlaceCity, @_data.Person.PlaceCountry</p>
            <p class="print-karakas">
                @foreach (var g in d1.Grahas.Where(g => g.CharaKaraka is not null))
                { <span>@g.CharaKaraka @ChartViewModel.PlanetGlyph(g.Planet)</span> }
            </p>
        </header>

        <section class="print-sec">
            <h2>D1 · Rasi</h2>
            <SouthIndianGrid AscendantSign="@d1.AscendantSign" MoonSign="@d1.MoonSign"
                             PlanetsBySign="Glyphs(d1)" SpecialPointLabels="Special(d1)"
                             CenterTitle="D1"><CenterMeta><b>Lagna</b> @Disp(d1.AscendantSign)</CenterMeta></SouthIndianGrid>
        </section>

        @if (_data.Charts.ContainsKey("D9"))
        {
            <section class="print-sec">
                <h2>D1 ⊕ D9</h2>
                <CombinedD1D9Grid D1KeyDetails="d1.KeyDetails" D9KeyDetails="@(_data.Charts["D9"].KeyDetails)" LagnaSign="@d1.AscendantSign" />
            </section>
        }

        <section class="print-sec">
            <h2>All vargas</h2>
            <div class="print-minis">
                @foreach (var code in VargaBundles.RailOrder)
                {
                    @if (_data.Charts.TryGetValue(code, out var lc))
                    {
                        <MiniGrid PlanetsBySign="Glyphs(lc)" LagnaSign="@lc.AscendantSign" Caption="@code" />
                    }
                }
            </div>
        </section>

        <section class="print-sec">
            <h2>Planet positions — D1</h2>
            <PlanetPositionsTable Rows="Rows(d1)" LagnaSign="@d1.AscendantSign" Variant="d1" ShowTechnical="true" />
        </section>

        <section class="print-sec">
            <h2>House lordship — D1</h2>
            <HouseLordshipTable Rows="d1.HouseLords" />
        </section>

        <section class="print-sec">
            <h2>Conjunctions — D1</h2>
            <ConjunctionsTable Rows="d1.Conjunctions" />
        </section>

        <section class="print-sec">
            <h2>Vimshottari Dasha</h2>
            @if (_dasha.Count > 0) { <DashaTimeline Roots="_dasha" /> }
        </section>

        <section class="print-sec">
            <h2>Sade Sati</h2>
            <SadeSatiTable Periods="_sadeSati" BirthDate="_data.Person.DateOfBirth.ToDateTime(_data.Person.EffectiveTimeOfBirth)" />
        </section>

        <section class="print-sec">
            <h2>Gochara</h2>
            <GocharaPanel Snapshots="_gochara" NatalMoonSign="@(d1.MoonSign ?? "")" />
        </section>
    </div>
}

@code {
    [Parameter] public int Id { get; set; }

    private WorkspaceData? _data;
    private IReadOnlyList<DashaPeriodRecord> _dasha = Array.Empty<DashaPeriodRecord>();
    private IReadOnlyList<SadeSatiPeriod> _sadeSati = Array.Empty<SadeSatiPeriod>();
    private IReadOnlyList<PlanetTransitSnapshot> _gochara = Array.Empty<PlanetTransitSnapshot>();

    protected override void OnParametersSet()
    {
        _data = WorkspaceData.Load(Id, BirthDetailsRepo, ChartResultsRepo, ChartKeyDetailsRepo,
            ChartHouseLordsRepo, ChartAspectsRepo, ChartConjunctionsRepo, ChartTypeRepo.GetAll());
        if (_data is null || !_data.Charts.ContainsKey("D1")) { _data = null; return; }
        _dasha = DashaPeriodsRepo.GetTreeByBirthDetailId(Id);
        _sadeSati = SadeSatiRepo.GetByBirthDetailId(Id);
        _gochara = GocharaRepo.GetSnapshots(DateTime.UtcNow);
    }

    private static string Disp(string s) => s == "Capricornus" ? "Capricorn" : s;

    private static IReadOnlyDictionary<string, IReadOnlyList<GridPlanetGlyph>> Glyphs(LoadedChart lc) =>
        lc.Grahas.Where(k => k.Planet != "Ascendant").GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<GridPlanetGlyph>)g.Select(k => new GridPlanetGlyph(
                k.Planet, ChartViewModel.PlanetGlyph(k.Planet), ChartViewModel.DignityToken(k.DignityStatus),
                k.IsRetrograde ?? false, k.IsCombust ?? false)).ToList());

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Special(LoadedChart lc) =>
        lc.SpecialPoints.GroupBy(k => k.Sign)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(k => k.Planet switch
            { "Gulika" => "Gk", "Maandi" => "Md", _ => k.Planet }).ToList());

    private static IReadOnlyList<PlanetRow> Rows(LoadedChart lc) =>
        ChartViewModel.BuildPlanetRows(lc.KeyDetails, lc.HouseLords, lc.Aspects);
}
```

  **NOTE:** `MiniGrid` uses `IReadOnlyList<string>` glyph maps; the grid components above use `GridPlanetGlyph` maps. `Glyphs(lc)` returns the latter — for the `MiniGrid` calls, add a second helper `GlyphNames(LoadedChart lc)` returning `IReadOnlyDictionary<string, IReadOnlyList<string>>` (copy from `VargaRail.GlyphsBySign`) and use it in the `print-minis` loop. Fix this when wiring — the build will catch the type mismatch.

- [ ] **Step 3: Write `PrintChart.razor.css`** — screen look (dark, linear, generous spacing):

```css
.print-doc { max-width: 900px; margin: 0 auto; display: flex; flex-direction: column; gap: 2rem; }
.print-head { border-bottom: 1px solid var(--paper-line); padding-bottom: 1rem; }
.print-karakas { display: flex; flex-wrap: wrap; gap: .5rem; color: var(--ink-soft); }
.print-sec h2 { margin-bottom: .75rem; }
.print-minis { display: flex; flex-wrap: wrap; gap: 1rem; }
```

- [ ] **Step 4: Write `wwwroot/css/print.css`.**

```css
@media print {
    .site-nav, .blazor-error-ui, #blazor-error-ui { display: none !important; }
    body { background: #14132a !important; color: #ece2cb !important; }
    * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
    .print-sec { break-before: page; }
    .print-head { break-after: avoid; }
    .print-minis > * , table tr, .chart, .mini-chart, .cd9-chart, .wheel { break-inside: avoid; }
}
```

  `print.css` may use raw hex — it is a print-media override that must not depend on `:root` custom properties surviving the print pipeline; note this exception in the file's top comment (DESIGN.md's no-raw-hex rule is about the token system, not `@media print` fallbacks).

- [ ] **Step 5: Green.** Build → 0/0. `/charts/1/print` — every section rendered flat and expanded, dark; browser Print preview (Ctrl+P) shows page breaks before each section and keeps the dark fills; renders in < 2 s.

- [ ] **Step 6: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 7: Commit.**

```bash
git add src/Ikiastrro.Web/Components/Pages/PrintChart.razor src/Ikiastrro.Web/Components/Pages/PrintChart.razor.css src/Ikiastrro.Web/wwwroot/css/print.css
git commit -m "feat(web): add PrintChart page + print stylesheet"
```

---

# Phase 7 — Cleanup & docs

---

### Task 7.1: Dead code sweep + `dotnet format`

**Files:**
- Modify: any file with an unused `using`, dead parameter, or stale comment introduced by Phases 1–6.
- Modify: `src/Ikiastrro.Web/Components/_Imports.razor` if any added `@using` turned out unused.

**Interfaces:** none.

- [ ] **Step 1: Establish the check.** `dotnet build Ikiastrro.slnx` currently 0/0 but may carry unused usings under a stricter analyzer; `dotnet format --verify-no-changes` (if configured) reports formatting drift.

- [ ] **Step 2: Grep for leftovers.**
  - `grep -rn "SCAFFOLD" src/Ikiastrro.Web` → must be **zero** (Task 1.10's scaffold went away with `ChartWorkspace` in Task 2.6; confirm).
  - `grep -rn "TabBar\|ChartWorkspace\|Charts.razor" src/Ikiastrro.Web` → zero (all deleted/renamed).
  - `grep -rn "D1ChartView" src/Ikiastrro.Web` → note any stale doc-comment references; fix text only.

- [ ] **Step 3: Run `dotnet format Ikiastrro.slnx`.** Review the diff — accept whitespace/using-sort changes, revert anything semantic.

- [ ] **Step 4: Green.** Build → 0/0. `dotnet run --project src/Ikiastrro.Web` and click through `/`, `/charts`, `/charts/1`, `/charts/1?view=wheel`, `/charts/1/varga/D9`, `/charts/1/timing`, `/charts/1/print`, `/add` — no error UI on any.

- [ ] **Step 5: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 6: Commit.**

```bash
git add -A
git commit -m "refactor(web): dead-code sweep + dotnet format after UI rebuild"
```

---

### Task 7.2: Docs + spec stale-notes + memory

**Files:**
- Modify: `ARCHITECTURE.md` (Web section — the "hardcoded to D1+D9" / 6-chart-type limitation is gone; describe the varga-centric routes).
- Modify: `docs/uidesign-specs.md` (new components + tokens).
- Modify: `docs/superpowers/specs/2026-09-01-varga-centric-web-ui-design.md` — add a dated **Implementation notes** block at the end recording the 3 deviations: (1) Syncfusion cut, SVG wheel instead; (2) Malefic/Benefic column kept, §9 overridden; (3) sunrise/sunset omitted from `BirthComputationPanel`, deferred.
- Modify: `D:\@ClaudeSpace\ikiastrro.md` (project history log) and `master_ikiastrro.md` if present.
- Modify: memory file `memproj_vedic_horo_gen.md` — update the "NEXT" list (varga-centric Web UI now DONE; note branch + commit range).

**Interfaces:** none.

- [ ] **Step 1: Establish the check.** `grep -n "D1+D9\|hardcoded\|only handles 6" ARCHITECTURE.md` → finds the stale Web description.

- [ ] **Step 2: Update `ARCHITECTURE.md`.** Replace the Web-layer paragraph describing the 3-tab workspace with a description of: `/` (filter + MiniGrid rows), `/charts` (sortable DataTable), `/charts/{id}` (D1 hero ChartFrame + VargaRail over all 21 + positions table + docked Dasha), `/charts/{id}/varga/{code}`, `/charts/{id}/timing`, `/charts/{id}/print`. Note the hand-drawn SVG `PolarWheel` (no Syncfusion) and the `WorkspaceData` batch loader.

- [ ] **Step 3: Append to the spec** (`2026-09-01-varga-centric-web-ui-design.md`), at the very end:

```markdown

---

## Implementation notes (2026-09-03, plan `2026-09-03-varga-centric-web-ui.md`)

Three deviations from this spec, approved by rammyps at planning time:

1. **§6 / §9 Syncfusion cut.** The polar wheel is a hand-drawn inline `<svg>` ring
   (`PolarWheel.razor`), not `SfPolarChart`. No `Syncfusion.Blazor` package was added.
   A later follow-up may swap it in for zoom/tooltips.
2. **§9 override — functional-nature column kept.** `PlanetPositionsTable` retains its
   "Malefic / Benefic" column (`LagnaFunctionalNature`). §9's "deferred" no longer applies.
3. **§4.1 — sunrise/sunset omitted** from `BirthComputationPanel`. No repo/column path
   exists for `SwissEphemerisProvider.GetSunTimes` output; deferred to a later pass.

§12.3 (VargaBundles membership) was resolved against `docs/reference-chart-varga-index.md`
+ classical Shodashavarga — see `VargaBundles.Groups`.
```

- [ ] **Step 4: Update `docs/uidesign-specs.md`** — add the new shared/chart/workspace components and the new tokens from Task 1.1 to whatever component/token inventory that doc keeps.

- [ ] **Step 5: Update the project history log** `D:\@ClaudeSpace\ikiastrro.md` with a dated entry: varga-centric Web UI rebuild, branch `feat/signattributes-classifications`, commit range, 7 phases, all 21 charts rendered, `verify-*` green throughout.

- [ ] **Step 6: Update memory.** Edit `C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md`: move "varga-centric Web UI rebuild" from NEXT to DONE with the date and commit range; note `ChartView.razor` / `ChartWorkspace` replaced by `Workspace.razor` + `VargaView` + `Timing` + `PrintChart`.

- [ ] **Step 7: Green.** Docs only — no build impact, but run `dotnet build Ikiastrro.slnx` → 0/0 to be safe.

- [ ] **Step 8: Regression gate.** `verify-vargas` + `verify-jaimini` → 0.

- [ ] **Step 9: Commit.**

```bash
git add ARCHITECTURE.md docs/uidesign-specs.md docs/superpowers/specs/2026-09-01-varga-centric-web-ui-design.md
git commit -m "docs: varga-centric Web UI — architecture, spec implementation notes, uidesign-specs"
```

(The `D:\@ClaudeSpace\ikiastrro.md` and memory file edits are outside the repo — no commit; just save.)

---

## Verification (whole plan)

Run after Phase 7, on `/charts/1` (BirthDetailId 1, Ramakrishnan), against `docs/artifacts/reference-charts/Rammy_Jagannatha.txt`:

- **Build:** `dotnet build Ikiastrro.slnx` → `0 Warning(s) 0 Error(s)` (CLI; VS 2026 if available).
- **CLI regression:** `verify-vargas`, `verify-jaimini`, `verify-avastha`, `verify-functional-nature`, `verify-pipeline`, `verify-schema` all exit 0 (this plan is read-only w.r.t. Core/DB).
- **Workspace:** D1 hero renders as grid AND wheel; rail shows all 21 charts in 5 groups; every tile opens its `VargaView`; combined D1⊕D9 grid matches the PNG for occupancy (AL Capricorn, HL Pisces, Gulika+Maandi Libra, karaka labels on 8 grahas); "Birth & computation" shows ayanamsha `23°34'50"`, sidereal time `19:20:55`.
- **VargaView D9:** `VargottamaStrip` lights the grahas whose D9 sign == D1 sign (cross-check the Navamsa column in the reference file); positions table shows the varga degree, not `0°00'`; prev/next walks the Shadvarga order.
- **Timing:** Dasha tree; Sade Sati chain for Moon in Scorpio; Gochara for Sa/Ju/Ra (or EmptyState if transits unbackfilled).
- **Print:** every section, dark, page breaks; browser "Save as PDF" keeps dark fills.
- **Home/SavedCharts:** filter narrows the list; sort columns work; delete works; nav Home/Saved Charts are yellow pills.
- **Golden-record spot checks** (visual, D1): Sun exalted Aries; Moon debilitated Scorpio / Anuradha pada 2 / H8; Rahu H4 / Ketu H10; Jupiter & Saturn retrograde in Virgo; Mars Moolatrikona Aries.

---

## Self-review (completed at write time)

**Spec coverage** — every §3 route has a task (`/`→5.1, `/charts`→5.2, `/add`→5.3, `/charts/{id}`→2.6, `/varga/{code}`→3.4, `/timing`→4.3, `/print`→6.1, `/life-weeks` untouched). §4 screens → Phases 2/3/4/5/6. §5 component tree → every new component has a create task; every "keep" component is untouched; `ChartWorkspace`/`TabBar` deleted in 2.6, `Charts`→`SavedCharts` in 5.2. §6 wheel → 1.8 (SVG, Syncfusion cut per decision). §7 visual language → 1.1 tokens + per-component `.css`. §8 data flow → 2.1 `WorkspaceData`. §10 sequencing → the 7 phases map 1:1. §11 verification → the block above.

**Known deviations from the spec, all flagged in-task and in Task 7.2:** Syncfusion cut (SVG wheel); Malefic/Benefic column kept (§9 overridden); sunrise/sunset omitted from `BirthComputationPanel`; `--dasha-*`/`--planet-*` NOT re-aliased into one block (the `--planet-*` set already exists in `tokens.css` and is sufficient — §7's re-expression is cosmetic and skipped to avoid churn; note if rammyps wants it).

**Type consistency** — `LoadedChart` (2.1) is the one shape every page loads; `PlanetRow` extended once (2.5) and every caller uses the trailing-optional-args form; `GridPlanetGlyph` gains `PlanetName` as arg 1 in 1.7 and every `new GridPlanetGlyph(...)` call site (ChartWorkspace 1.7, Workspace 2.6, VargaView 3.4, PrintChart 6.1) passes it; `SegmentedToggle.Segment` and `PolarWheel.Point` and `DataTable<T>.Column` are nested records referenced consistently.

**Placeholder scan** — no TBD/TODO; the two "fix when wiring" notes (PrintChart `MiniGrid` glyph-map type; `GocharaPanel` SignId order) are explicit verification steps with the exact command to resolve them, not deferred work.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-03-varga-centric-web-ui.md`. Two execution options:

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
