# Design Spec — Varga-centric Web UI rebuild

**Status:** Approved design, pre-implementation
**Owner:** rammyps
**Created:** 2026-09-01
**Approach:** fresh structure in-place, keep the design tokens + working primitives + the
Phase-0 (Plan 1) Core/Data groundwork (see "Approaches considered")
**Supersedes:** `docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md`
(life-area framing dropped; its Phase-0 groundwork is kept and reused — see §9)
**Depends on:** `2026-09-01-jaimini-chara-karaka-special-points-design.md` (Spec 1 — this
UI displays its output)
**Related:**
- `src/Ikiastrro.Web/Components/DESIGN.md` (design-language rule + the Syncfusion carve-out)
- `docs/uidesign-dataviz.md` (Syncfusion Blazor pick + screen→chart mapping)
- `docs/uidesign-specs.md` (workspace design tokens/components)
- `docs/reference-calculations.md` §2 (21 chart types, `VargaLongitudeDegrees`, `VargaMethod`)
- `docs/superpowers/plans/groundwork-outcomes-for-plan-2.md` (the interfaces this binds to)
- `scratch/D1-D9-CombinedChart.png` (the combined chart Spec 1 builds)

---

## 1. Goal

Rebuild `Ikiastrro.Web`'s read layer around **the charts themselves** — D1 as the hero, all
20 divisionals one click away, grouped by the classical varga bundles — replacing the
current partial 3-tab `ChartWorkspace` that only handles 6 chart types. Surface the data
computed since that UI was written: the 15 new vargas, `VargaLongitudeDegrees`, per-planet
speed & ecliptic latitude, numeric ayanamsha / sidereal time, `VargaMethod`, birth lat/long,
and (from Spec 1) Chara Karakas + special points + the combined D1+D9 chart. Make it
**chart-first, not table-first**: every screen leads with a diagram.

**Interpretation stays out of scope** — classical facts, displayed and organised. Synthesis
is the future Python engine.

---

## 2. Design

### 2.1 Decisions locked (brainstorming Q&A, 2026-09-01)

| # | Decision |
|---|---|
| U1 | **Organising principle = varga-centric.** D1 hero + a varga rail grouped by Shadvarga / Saptavarga / Dasavarga / Shodasavarga. No life-area tabs. |
| U2 | **Latitude = fresh structure, keep the palette.** New routes/layout of my design; reuse `tokens.css`, `SouthIndianGrid`, `DashaTimeline`, and the Plan-1 Core/Data groundwork. |
| U3 | **Centrepiece = enriched South-Indian grid + a grid⇄wheel toggle.** The wheel is a 360° sidereal polar chart (Syncfusion). North-Indian style stays deferred. |
| U4 | **Scope = whole app** — Home, Saved Charts, Add, workspace, plus new Timing & Print routes. |
| U5 | **Home & Saved Charts stay two pages**, both restyled with mini-diagrams. |
| U6 | **Timing** (Dasha tree + Sade Sati + Gochara) is its **own route**; a compact Dasha strip stays docked on the workspace. |
| U7 | **Technical numbers: clean by default.** One "Birth & computation" disclosure for the per-person/per-chart scalars; a "show technical" toggle adds speed / ecliptic latitude / `VargaLongitudeDegrees` columns to the positions table. |
| U8 | **Home & "Charts" nav buttons get a yellow (`--accent`) background.** |
| U9 | **Syncfusion.Blazor** is added to `Ikiastrro.Web.csproj` now (Community License, already sanctioned in `DESIGN.md`). Used only for the polar wheel (and later Gochara/optional gauges). |

### 2.2 Scope

**In:** §3–§8 below — every route, component, token, and the Syncfusion wiring.
**Out:** interpretation/prediction text; life-area grouping; North-Indian grid; light theme;
auth; Ashtakavarga / Shadbala / Avastha panels (need Core work first); synastry; any chart
type beyond the 21 that exist; Jaimini rasi-dasha views.
**Touches Web:** most of `Components/` — see §5. **Touches Core/Data:** read-only additions
only (`ChartResultsRepository` / `ChartKeyDetailsRepository` batch reads already exist per
Plan 1; a `GocharaRepository` for the transit snapshot). **No DB schema change** in this
spec (Spec 1 owns migration 14). **Touches CLI:** none.

---

## 3. Routing

| Route | Component | Notes |
|---|---|---|
| `/` | `Home` | fast picker: search box + recent people, each a row with a compact D1 `MiniGrid`. "Add someone" button. |
| `/charts` | `SavedCharts` | managed list — sortable (name / DOB / place), `MiniGrid` thumbnail per row, delete via `ConfirmDialog`. |
| `/add` | `Add` | existing form, `HandleSubmit` already on `ChartGenerationService` (Plan 1). Minor restyle only. |
| `/charts/{id}` | `Workspace` | **rebuilt.** D1 hero (`ChartFrame`) + `VargaRail` + combined-chart tile + `PlanetPositionsTable` (D1) + docked Dasha strip + `BirthComputationPanel`. |
| `/charts/{id}/varga/{code}` | `VargaView` | **new.** One divisional in full: `ChartFrame` + its positions / lordship / conjunctions tables + `VargottamaStrip`. `code` ∈ `D2 D2-US D3 … D60`. Prev/next nav within the rail order. |
| `/charts/{id}/timing` | `Timing` | **new.** `DashaTimeline` (full tree) + `SadeSatiTable` + `GocharaPanel` + link to life-weeks. |
| `/charts/{id}/print` | `PrintChart` | **new.** Flat, every section expanded, dark, `@media print` stylesheet. |
| `/charts/{id}/life-weeks` | `LifeWeeks` | unchanged. |

Unknown `{code}` or `{id}` → `EmptyState` / `notFound`, no throw. All view state is in the
route or query string (bookmarkable, back-button correct) — there is no client-only tab state.

---

## 4. Screens

### 4.1 `Workspace` (`/charts/{id}`)

```
┌ nav:  [ Home ]🟡   [ Charts ]🟡                    ikiastrro · dedicated to … ┐
├────────────────────────────────────────────────────────────────────────────┤
│  R. Ramakrishnan   ·   22 Apr 1981  05:30 (+05:30)   ·   Chennai, India     │
│  Chara Karakas:  AK Ra · AmK Ve · BK Sa · MK Ju · PiK Su · PK Mo · GK Ma · DK Me │
│                                                     [ ▸ Birth & computation ] │
├──────────────────────────────────┬─────────────────────────────────────────┤
│   ╭────────────────────────────╮ │  VARGA RAIL                              │
│   │        D1 · RASI           │ │  ▾ Shadvarga (6)                         │
│   │   ┌──┬──┬──┬──┐            │ │    [D2][D3][D9][D12][D30]  · D1          │
│   │   │  grid  ⇄  wheel  │     │ │  ▾ Saptavarga (7)   + [D7]               │
│   │   └──┴──┴──┴──┘            │ │  ▾ Dasavarga (10)   + [D10][D16]         │
│   │                            │ │  ▾ Shodasavarga (16)+ [D2-US][D4][D5]…   │
│   ╰────────────────────────────╯ │  each tile = MiniGrid; click → VargaView │
│   [ D1 ⊕ D9  combined chart ]  ─┼──────────────────────────────────────────┤
│                                  │  Ayanamsha 23°34'50"  ·  LST 19:20:55    │
├──────────────────────────────────┴─────────────────────────────────────────┤
│  Planet positions — D1                            [ show technical ▸ ]      │
│  Graha · Rasi · Degree · Nakshatra · Pada · Nak-Lord · Sub-Lord · Dir ·    │
│         Combust · House · House(Ch) · Dignity · Rules · Aspected-by · Karaka │
├────────────────────────────────────────────────────────────────────────────┤
│  ▬ Vimshottari Dasha  ▏Sat▕▏Merc▕▏Ket▕▏Ven▕…  (Maha only)     → full Timing │
└────────────────────────────────────────────────────────────────────────────┘
```

- **Hero (`ChartFrame` for D1):** the enriched `SouthIndianGrid` (per-planet colour pill,
  degree, retro `(R)` / combust 🔥, special-point corner labels `AL A2…A12 HL Gk Md`,
  house badges) with a **grid ⇄ wheel** segmented toggle. Wheel = `PolarWheel` (§6).
- **`VargaRail`:** the 4 classical bundles as `Disclosure` groups. A varga can appear in
  more than one bundle — it is listed once, in its smallest bundle, with a superscript of
  the larger bundles it also belongs to. Each tile is a `MiniGrid` (compact `SouthIndianGrid`,
  glyphs only) + the code + `VargaMethod` initial. Click → `/charts/{id}/varga/{code}`.
  Missing chart → tile shows `EmptyState` ("run `backfill-charts`").
- **Combined tile:** opens `CombinedD1D9Grid` (Spec 1 §7) inline (a `Disclosure`, collapsed).
- **`BirthComputationPanel`** (disclosure, collapsed): birth lat/long (D-M-S + decimal),
  `IanaTimeZoneId` + `UtcOffset`, sunrise / sunset (Spec 1), ayanamsha° (`AyanamshaDegrees`,
  formatted D-M-S), sidereal time (`SiderealTimeHours` → `HH:MM:SS`), `EngineVersion`, and a
  per-chart table of `ChartType · VargaMethod · MethodSource`.
- **`PlanetPositionsTable`** (D1): default columns as drawn above. `[show technical ▸]`
  reveals **Speed °/day**, **Ecliptic lat °**, **Varga long °** (`VargaLongitudeDegrees` —
  equals nirayana for D1, kept for parity with the varga views). Built from
  `ChartViewModel.BuildPlanetRows` (extended for the `CharaKaraka` cell + the 3 technical
  cells).
- **Docked Dasha strip:** `DashaTimeline` in a compact mode (Mahadasha row only, current
  period marked), linking to `/charts/{id}/timing`.

### 4.2 `VargaView` (`/charts/{id}/varga/{code}`)

Header: chart name + `Dn · <Sanskrit name> · <VargaMethod>` + prev/next chevrons (rail
order). Body:
1. `ChartFrame` for `Dn` (grid ⇄ wheel; wheel plots `VargaLongitudeDegrees`).
2. `PlanetPositionsTable` for `Dn` — **varga-appropriate columns**: Graha · Rasi ·
   Varga-degree (`DegreesInSignDisplay`, now populated for every chart) · House · Dignity ·
   Rules · Aspected-by · Karaka. Nakshatra / Pada columns omitted (varga cells are discrete
   buckets — unchanged rule). `[show technical]` adds Varga long ° / Speed / Ecliptic lat.
3. `HouseLordshipTable` + `ConjunctionsTable` for `Dn` (both in a `Disclosure`).
4. **`VargottamaStrip`** — for each graha, a chip that lights when its `Dn` sign equals its
   D1 sign (true Vargottama for D9; "same-sign-as-D1" for the rest, labelled as such).
5. `EmptyState` if `Dn` is not computed for this person.

### 4.3 `Timing` (`/charts/{id}/timing`)

`DashaTimeline` (full 3-level tree, unchanged) + `DashaLegend` · `SadeSatiTable` (the 3
Dhaiyas + Kantaka + Ashtama, "active now" marker) · **`GocharaPanel`** (new) — Saturn /
Jupiter / Rahu current sidereal sign vs natal sign + house-from-natal-Moon, "in since / next
change" from `tbl_PlanetSignTransitEvents`; `EmptyState` if that table is unpopulated. Link
to `/charts/{id}/life-weeks`.

### 4.4 `Home` (`/`) and `SavedCharts` (`/charts`)

- **`Home`** — a search `<input>` (filters by name prefix, `BirthDetailsRepository`
  already has `SearchByNamePrefix`) over a list of person rows; each row = `MiniGrid` (D1) +
  name / DOB / place, links to `/charts/{id}`. A prominent "＋ Add someone" button. The old
  bare `<select>` is dropped.
- **`SavedCharts`** — the same data as a `DataTable<T>` with sortable Name / DOB / Place
  columns, a `MiniGrid` thumbnail column, and a `DeleteIconButton` per row →
  `ConfirmDialog`. This is the "manage my saved people" screen.

### 4.5 `PrintChart` (`/charts/{id}/print`)

Every section flat and expanded, no `Disclosure`, no rail — a linear document: header +
Chara Karakas · D1 grid · combined D1⊕D9 grid · **all 21** `MiniGrid`s in bundle order ·
D1 positions table (all columns incl. technical) · Dasha (Maha + Antar) · Sade Sati ·
Gochara. **Stays dark.** `@media print`: hide nav/buttons; `print-color-adjust: exact` +
`-webkit-print-color-adjust: exact`; `break-inside: avoid` on grids and table rows;
`break-before: page` before each major section. "Print / Save PDF" button in the workspace
header.

---

## 5. Component tree (after)

```
Components/
  Layout/       MainLayout            [edit — yellow Home/Charts buttons, nav gains no items]
  Pages/        Home [rebuild]  SavedCharts [rebuild, was Charts]  Add [restyle]
                Workspace [rebuild, was ChartWorkspace]  VargaView [new]
                Timing [new]  PrintChart [new]  LifeWeeks [keep]  Error [keep]
  Workspace/    WorkspaceHeader [new]  VargaRail [new]  BirthComputationPanel [new]
  Charts/       SouthIndianGrid [keep + enrich]  GridPlanetGlyph [keep]
                MiniGrid [new — thin wrapper: SouthIndianGrid compact, glyphs only]
                PolarWheel [new — Syncfusion SfPolarChart]
                ChartFrame [new — grid⇄wheel toggle around SouthIndianGrid | PolarWheel]
                CombinedD1D9Grid [new — Spec 1 §7]
                PlanetPositionsTable [rebuild — Karaka + technical columns, varga-aware]
                HouseLordshipTable [new]  ConjunctionsTable [new]  VargottamaStrip [new]
                DashaTimeline [keep]  DashaLegend [keep]  DashaLordColors [keep]
                SadeSatiTable [keep]  GocharaPanel [new]
  Shared/       ConfirmDialog [keep]  DeleteIconButton [keep]
                Disclosure [new]  DataTable<T> [new]  PlanetChip [new]  EmptyState [new]
                SegmentedToggle [new — grid⇄wheel, show-technical]
```

**Deleted:** `Pages/ChartWorkspace.razor` (+`.css`) — replaced by `Workspace`;
`Shared/TabBar.razor` (+`.css`) — no tabs in the varga-centric model.
**Renamed:** `Pages/Charts.razor` → `Pages/SavedCharts.razor`.
**Untouched:** `SouthIndianGrid` public API (only its `.css` and an optional
`SpecialPointLabels` parameter are added), `DashaTimeline`, `DashaLegend`,
`DashaLordColors`, `GridPlanetGlyph`, `ConfirmDialog`, `DeleteIconButton`, `LifeWeeks`,
`Add` markup, `Error`, `App`, `Routes`, `_Imports`.

---

## 6. `PolarWheel` (Syncfusion)

- **Package:** `Syncfusion.Blazor.Charts` (Community License) in `Ikiastrro.Web.csproj`,
  pinned. `Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(...)` in
  `Program.cs` from an env var / user-secret (never committed). `AddSyncfusionBlazor()` in
  DI, `<link>` the Syncfusion theme **only** inside a `.sf-scope` wrapper so it can't leak
  into hand-rolled components (`DESIGN.md` rule).
- **Chart:** `SfPolarChart` / radial scatter — one series of the 9 grahas + Ascendant + the
  special points, each plotted at its longitude (`0–360°`, `0°` = Aries at the top or the
  9-o'clock, TBD in Phase 2 with rammyps). D1 uses `NirayanaLongitudeDegrees`; a `Dn` view
  uses `VargaLongitudeDegrees`. Point marker = the planet's glyph; point colour =
  `--planet-<x>` fed via `Palettes`.
- **Aspect chords** — a `[show aspects]` toggle draws straight lines between an aspecting
  graha and its aspected targets (7th for all, plus the special aspects for Ma/Ju/Sa;
  Ra/Ke Jupiter-style per the project convention). Off by default.
- **Fallback:** if `SfPolarChart` can't render server-side without a JS round-trip that
  fights the dark theme, the wheel degrades to a static inline `<svg>` ring drawn by
  hand from the same data (decision recorded in Phase 2). The grid is always available;
  the wheel is the enhancement.

---

## 7. Visual language

Dark parchment stays. `tokens.css` additions (dark-only, one value each per `DESIGN.md`):

- **`--planet-sun` … `--planet-ketu`** — 9 solid graha-identity hues. Re-express the
  existing `--dasha-*` block as sharing these exact values (alias + equivalence comment) so
  grid, chips, wheel, Dasha and life-weeks key off one palette. Run the dataviz skill's
  `validate_palette.js` **all-pairs** (any two grahas can share a grid cell); 9-hue all-pairs
  CVD separation will not fully clear — colour stays a scan aid, glyph text always present.
- **`--planet-sun-bg` … `--planet-ketu-bg`** — 9 pre-baked low-alpha tints of each hue over
  `--paper`; `--ink` verified legible on every one at build.
- **`--nav-btn`** = `--accent` (the yellow); **`--nav-btn-fg`** = `--paper`. Applied to the
  Home and "Charts" items in `MainLayout` (filled pill, not just link text).
- **`--cell-fill`, `--lagna-fill`, `--grid-stroke`, `--sign-text`, `--muted-text`** —
  semantic grid tokens so `SouthIndianGrid` stops reading `--paper`/`--ink` directly.
- **`--vargottama`** = `--dignity-good` (alias) for the `VargottamaStrip` lit state.

**Shared components' look:** `PlanetChip` = Sanskrit-primary / English-secondary name on
its `--planet-<x>-bg` fill with a 2 px solid `--planet-<x>` left border, `--ink` text; the
single unit every planet/lord/karaka cell and grid glyph uses. `Disclosure` =
`<button aria-expanded>` + `grid-template-rows: 0fr→1fr`, `inert` when closed.
`SegmentedToggle` = 2–3 `<a href>`/`<button>` segments, active filled `--accent`.
`EmptyState` = icon + message + optional `<code>` CLI hint. Every component keeps its own
`ComponentName.razor.css` isolation file — no inline `<style>`, no raw hex (`DESIGN.md`).

---

## 8. Data flow

`Workspace.OnParametersSet` (one batch, per Plan 1's `GetByBirthDetailId` methods):

```
BirthDetailsRepository.GetById(id)
ChartResultsRepository.GetByBirthDetailId(id)          -> 21 PositionChart headers (+ Dasha)
ChartKeyDetailsRepository.GetByBirthDetailId(id)       -> all planet + special-point rows, group by ChartResultId
ChartHouseLordsRepository.GetByBirthDetailId(id)
ChartAspectsRepository.GetByBirthDetailId(id)
ChartConjunctionsRepository.GetByBirthDetailId(id)
DashaPeriodsRepository.GetTreeByBirthDetailId(id)
SadeSatiRepository.GetByBirthDetailId(id)              (Timing only)
GocharaRepository.GetSnapshot(planet, DateTime.UtcNow) (Timing only — new thin repo over tbl_PlanetSignTransitEvents / tvf_PlanetSignAtDate)
```

Assembled into `IReadOnlyDictionary<string, LoadedChart>` keyed by chart code:

```csharp
record LoadedChart(
    string ChartType, string Label, string SanskritName, string? VargaMethod,
    string AscendantSign,
    IReadOnlyList<ChartKeyDetail>   KeyDetails,     // grahas + Ascendant + special points
    IReadOnlyList<ChartHouseLord>   HouseLords,
    IReadOnlyList<ChartAspect>      Aspects,
    IReadOnlyList<ChartConjunction> Conjunctions,
    double? AyanamshaDegrees, double? SiderealTimeHours);
```

`ChartViewModel.BuildPlanetRows` / `BuildAspectedByGlyphs` / `BuildPlanetsBySign` are already
chart-type-generic — extended only for the `CharaKaraka` value and to skip `PointKind <>
'Graha'` rows when building the graha table (special points render as grid corner labels and
an optional "Special points" mini-table, not as planet rows). `VargaRail` groups by a static
`VargaBundles` map. Shape: `Shadvarga = {D1,D2,D3,D9,D12,D30}` (6); `Saptavarga = Shadvarga
∪ {D7}` (7); `Dasavarga = Saptavarga ∪ {D10,D16}` (10); `Shodasavarga` = all 16 classical
(adds D4,D5,D8,D20,D24,D27,D40,D45,D60). The exact membership of each bundle is fixed in
Phase 3 against one cited source (§12.3); `D2-US`, `D11` and any chart outside the cited 16
appear in the rail under an "Extra vargas" group, not in a classical bundle.

---

## 9. Relationship to the superseded life-area spec

`2026-08-30-web-ui-life-area-recreate-design.md` is marked **superseded** by this file. What
carries over vs. what is dropped:

| From the life-area spec | Status here |
|---|---|
| Phase 0 groundwork (`ChartViewModel`/`PlanetRow` rename, `LagnaFunctionalNature`, `LifeAreaMap`, `ChartGenerationService`, 4 batch `GetByBirthDetailId` reads, reference repos, migration 031) | **kept** — merged to the branch already; this UI uses the renames, the batch reads, and `ChartGenerationService` |
| 5 life-area tabs; `LifeAreaTab`/`PersonalityHealthTab`/… ; `SignificatorHouses/KarakasTable` | **dropped** — replaced by the varga rail + `VargaView` |
| `FunctionalNaturePanel` / the `FunctionalNature` column | **deferred** — `LagnaFunctionalNature` still exists in Core; surfacing it is a later, smaller pass (not this spec) |
| `VargaChartPanel`, `Disclosure`, `DataTable<T>`, `PlanetChip`, `EmptyState`, per-planet `--planet-*` tokens, dark `/print` | **kept** — same components, this spec builds them |
| Reference browser (`/reference/*`) | **deferred** — out of scope here; the life-area spec's design stands for a later pass |
| `LifeAreaMap` (Core) | **left in place, unused by the UI** — a later Jaimini/Karaka feature may use it |

---

## 10. Build sequencing (plan phases — app builds & `verify-*` pass after each)

1. **Primitives.** `tokens.css` additions (`--planet-*` + `-bg` + nav + grid semantics +
   `--vargottama`; `validate_palette.js` run recorded; `--ink` contrast check). `PlanetChip`,
   `Disclosure`, `DataTable<T>`, `EmptyState`, `SegmentedToggle`, `MiniGrid`. Enrich
   `SouthIndianGrid` (glyph pill via `PlanetChip`, `SpecialPointLabels` param). Syncfusion
   package + license + `.sf-scope`; `PolarWheel` (or the SVG fallback); `ChartFrame`. Drop
   `PlanetChip`/`MiniGrid`/`ChartFrame` into the *current* `ChartWorkspace` first to exercise
   them. `CombinedD1D9Grid` (Spec 1 §7) lands here.
2. **Workspace.** `Workspace` + `WorkspaceHeader` + `VargaRail` + `BirthComputationPanel` +
   rebuilt `PlanetPositionsTable` + docked Dasha strip. Wheel orientation + aspect-chord
   behaviour finalised with rammyps. Delete `ChartWorkspace`, `TabBar`.
3. **VargaView.** `/charts/{id}/varga/{code}`, `HouseLordshipTable`, `ConjunctionsTable`,
   `VargottamaStrip`, rail prev/next. `VargaBundles` map fixed against a cited 6/7/10/16 list.
4. **Timing.** `/charts/{id}/timing`, `GocharaPanel`, `GocharaRepository`; `SadeSatiTable`
   moves in; `DashaTimeline` full tree.
5. **Non-workspace pages.** `Home` rebuild, `Charts`→`SavedCharts` rebuild via `DataTable<T>`,
   `Add` restyle, yellow nav buttons in `MainLayout`.
6. **Print.** `PrintChart` + `print.css`.
7. **Cleanup & docs.** Dead CSS/params, `dotnet format`; `ARCHITECTURE.md` (Web section —
   the "hardcoded to D1+D9" limitation is gone), `docs/uidesign-specs.md`,
   `master_ikiastrro.md`, `../ikiastrro.md`, memory. Mark the life-area spec superseded in
   `master_ikiastrro.md`.

---

## 11. Verification (no test project)

- **Build gate** after each phase — `dotnet build Ikiastrro.slnx` 0/0 (CLI `dotnet` and, if
  available, VS 2026).
- **`verify-*` CLI** — `verify-schema` / `verify-vargas` / `verify-avastha` /
  `verify-functional-nature` / `verify-jaimini` (Spec 1) all exit 0 — this spec is read-only,
  they must stay green throughout.
- **Colour** — `validate_palette.js` output recorded for the 9 `--planet-*` hues (all pairs);
  every `--planet-*-bg` passes a WCAG-AA contrast check against `--ink`; the 9 planet marks
  are distinguishable in the grid, the wheel, the tables, and the dark `/print` PDF; every
  planet mark also carries its glyph/name (colour never the sole signal).
- **Live browser smoke** (App-Control note: launch via `dotnet run --project
  src/Ikiastrro.Web`, not a direct `.exe`): open `1_Ramakrishnan` —
  - Workspace: D1 hero renders as grid AND wheel; the rail shows all 21 charts grouped;
    every tile opens its `VargaView`; combined D1⊕D9 grid matches `scratch/D1-D9-CombinedChart.png`
    (AL Capricorn, HL Pisces, Gulika+Maandi Libra, karaka labels on the 8 grahas);
    "Birth & computation" shows ayanamsha `23°34'50"`, LST `19:20:55`, sunrise `05:56:39`.
  - `VargaView` for D9: `VargottamaStrip` lights the grahas whose D9 sign == D1 sign;
    positions table shows the varga degree (`DegreesInSignDisplay`), not `0°00'`.
  - `Timing`: Dasha tree, Sade Sati (Moon in Scorpio → the expected Dhaiya chain), Gochara
    for Sa/Ju/Ra.
  - `/print` renders every section, dark, and a browser "Save as PDF" keeps the dark fills.
  - `Home` search filters; `SavedCharts` sorts and deletes; nav Home/Charts are yellow pills.
- **Golden record** — the `1_Ramakrishnan` ~12-item checklist (Sun exalted Aries; Moon
  debilitated Scorpio / Anuradha pada 2 / H8; Rahu H4 / Ketu H10; Ju & Sa retrograde in
  Virgo; Mars Moolatrikona; Ra/Ke mutual 7th aspect) re-confirmed visually after Phase 2 and
  Phase 6.

---

## 12. Open questions / risks

1. **Syncfusion server-side rendering** — Blazor Server + `SfPolarChart` may need a JS
   interop tick before first paint, which can flash a light default theme. Phase 1 decides
   `PolarWheel` (Syncfusion) vs the hand-drawn `<svg>` ring; the grid never depends on it.
2. **Wheel orientation** — Aries at 12 o'clock (Western convention) vs at the 9 o'clock
   ascendant (Jyotish wheel convention). Decide with rammyps in Phase 2.
3. **`VargaBundles` membership** — the classical 6 / 7 / 10 / 16 lists vary slightly by
   author (e.g. is D2 Parashari or D2-US in the Shadvarga?). Fix against one cited source
   (BPHS / PVR Narasimha Rao) in Phase 3; record it in `docs/reference-calculations.md`.
4. **`GocharaRepository` source** — `tvf_PlanetSignAtDate(@planetId, @date)` exists;
   confirm it covers Sa/Ju/Ra (Mars was out of the transit-table scope) so Gochara shows
   exactly those three.
5. **`MiniGrid` × 21 on `/print`** — 21 small SVG grids plus the hero and combined grid is a
   heavy page; verify print pagination and that it renders in < 2 s.
6. **`SouthIndianGrid` enrich vs. keep** — U2 says keep it; enriching its glyph rendering
   (pill via `PlanetChip`) and adding `SpecialPointLabels` is an *edit* to a "keep"
   component. Acceptable — its public parameters stay backward-compatible; the change is
   additive + `.css`.

---

## Approaches considered

- **Fresh structure in-place, keep tokens + primitives (chosen).** New routes/components of
  a varga-centric design, but the same project, the same dark parchment palette, the same
  `SouthIndianGrid`/`DashaTimeline`, and the Plan-1 Core/Data groundwork. Delivers the
  "surprise me" on layout without re-litigating the design language or re-solving
  DI/layout/place-resolution. App runnable after every phase.
- **Evolve the life-area spec.** Least new design work, but keeps a life-area framing the
  user explicitly moved away from, and its component tree (`LifeAreaTab` and five siblings)
  is dead weight under a varga-centric model.
- **Fresh structure AND new visual language.** Maximum "surprise", but throws away a design
  language rammyps has iterated on, forces a full token re-derivation and a Syncfusion
  re-theme, and risks the classical chart diagrams looking unfamiliar. Not worth it for a
  private tool whose look is already settled.
- **New `Ikiastrro.Web2` project.** Re-solves routing/DI/layout/delete-cascade for no gain.


---

## Implementation notes (2026-09-03, plan `2026-09-03-varga-centric-web-ui.md`)

Deviations from this spec, all approved by rammyps (at planning time or the 2026-09-03
Phase-2 gate):

1. **§6 / §9 — Syncfusion cut.** The polar wheel is a hand-drawn inline `<svg>` ring
   (`PolarWheel.razor`), not `SfPolarChart`. No `Syncfusion.Blazor` package was added. A
   later follow-up may swap it in for zoom / tooltips.
2. **§9 override — functional-nature column kept.** `PlanetPositionsTable` retains its
   "Malefic / Benefic" column (`LagnaFunctionalNature`). §9's "deferred" no longer applies.
3. **§4.1 — `BirthComputationPanel` reduced.** Sunrise / sunset **and** the method-source
   line are omitted — no repo or column path exists for `SwissEphemerisProvider.GetSunTimes`
   output or the method-provenance string. Ayanamsha and sidereal time are shown. Deferred.

Restorations decided at the 2026-09-03 Phase-2 gate — the plan had originally cut these, so
they are **not** deviations from the spec's intent:

4. **Wheel aspect-chord overlay** — reimplemented as hand-drawn SVG lines. A new
   `PolarWheel.Chord` record plus an `?aspects=1` query toggle on `/charts/{id}` (Workspace)
   and `/charts/{id}/varga/{code}` (VargaView); a "show aspects / hide aspects" link, wheel
   view only. Same-sign planet pairs are filtered out.
5. **Docked Dasha strip** — `DashaTimeline` gained a `Compact` bool: a Maha-only,
   non-interactive list for the workspace dock. The full interactive tree is unchanged when
   `Compact` is false.

Other notes:

- **`--dasha-*` / `--planet-*` were not re-aliased** into a single block (spec §7). The
  `--planet-*` token set already in `tokens.css` is sufficient; the re-expression is
  cosmetic and was skipped to avoid churn. Revisit if a consolidated palette is wanted.
- **One Data-layer fix outside the Web project.**
  `PlanetSignTransitEventsRepository.GetSnapshot` now casts `SignId` to `tinyint` in its
  `tvf_PlanetSignAtDate` query — a latent `int`→`byte` Dapper mapping bug that this plan
  was the first runtime consumer to exercise (via `GocharaRepository` → the Timing page).
  `db/ikiastrro.sql` and the TVF are untouched; the TVF's own int-promotion is left for a
  later schema pass.
- **`MainLayout.razor.css`** kept its pre-existing serif-gold brand-banner rules; only the
  dead `.site-nav a` / `a:hover` / `a.active` text-link rules were dropped when the nav
  links became `.nav-pill` buttons.
- **`VargaView` prev/next** walks `VargaBundles.RailOrder`; for D9 that renders
  `D3 ‹ D9 › D12` (the Shadvarga block order), not the `D12 ‹ D9 › D30` shown illustratively
  in an early draft of the plan.
- **§4.4 — `Home` search filters client-side.** `Home.razor` loads all people via
  `GetAll()` and filters in-memory with a case-insensitive `Contains`, rather than calling
  `BirthDetailsRepository.SearchByNamePrefix`. Behaviourally a superset (substring, not just
  prefix) and it avoids a round-trip per keystroke; the person list is tens of rows. Revisit
  if the list grows large enough to need server-side paging.
- **§8 — VargaBundles membership corrects the spec.** The spec's §8 grouping put D5 and D8
  in Shodashavarga and treated D60 as an outer addition. The shipped `VargaBundles.Groups`
  follows textbook Shodashavarga instead: Shadvarga (6) / Saptavarga (7) / Dashavarga (10,
  incl. D60) / Shodashavarga (16), with D5/D6/D8/D11 in an "Extra vargas" tail. `RailOrder`
  covers all 21 codes once. Treat `VargaBundles.Groups` as the source of truth over spec §8.

§12.3 (VargaBundles membership) was resolved against classical Shodashavarga plus the
project's varga reference index — see `VargaBundles.Groups` (D2-US sits in Shadvarga).

Final whole-branch review (opus, 2026-09-03) returned 0 Critical / 3 Important / 10 Minor;
all 3 Important (nav-pill CSS isolation scoping, a missing `Charts["D1"]` guard in
`VargaView`, and the rail's D1 tile linking into a self-referential varga route) plus 8 of
the Minors were fixed in `776ace9`. Two Minors are parked as non-blocking: the `Home` filter
note above, and a latent `DataTable`/`Cols` reallocation in `SavedCharts.razor` (harmless
while no captured-state column is sortable).
