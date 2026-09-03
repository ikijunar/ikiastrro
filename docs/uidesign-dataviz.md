# ikiastrro — Data-Visualization Specifications

Reference doc for charts, gauges, and infographic surfaces in the **ikiastrro web
workspace** (`Ikiastrro.Web`, Blazor Server, `net8.0`). Living document — update it when
the charting stack or a screen's visualization changes. Companions:
`docs/uidesign-specs.md` (design language, tokens), `docs/techstack-details.md`
(project layout, `ChartGenerationService`), `docs/reference-calculations.md` (the math behind
the numbers being plotted).

Decision recorded 2026-08-31 with rammyps: adopt one charting library across the project
rather than hand-rolling every plot. The North/South Indian chart *diagrams* and the
LifeWeeks grid stay hand-rolled — they are not chart-shaped and no library draws them.

---

## 1. The pick

**Primary data-viz library: Syncfusion Blazor** — Community License (free for individuals
and companies under USD 1M annual revenue; rammyps confirmed eligible 2026-08-31).

Why Syncfusion over the MIT alternatives, for this project:

| Need | Syncfusion | Blazor-ApexCharts (MIT) | LiveCharts2 (MIT) |
|---|---|---|---|
| Native Blazor Server components, no JS framework to manage | yes | JS-interop wrapper | SkiaSharp canvas |
| True polar/radar (360° longitude wheel, Shadbala radar) | `SfPolarRadarChart` | radialBar only, no polar scatter | polar supported, less battle-tested on Server |
| PDF export for `/charts/{id}/print` | built in | none | none |
| Bar / stacked / line / range-bar / heatmap / gauge from one package | yes | yes (no gauge) | yes |
| NuGet footprint | heavy | light | medium |
| Own theme CSS to reconcile with `tokens.css` | yes (mitigated, §4) | minimal | none |

**Fallback: Blazor-ApexCharts.** If Syncfusion's footprint or license registration proves
too heavy, swap to ApexCharts: lighter and prettier defaults, but no polar scatter (keep
hand-rolling the longitude wheel in SVG) and no PDF export (use QuestPDF separately). The
screen mapping in §3 stays the same except the "Polar" rows.

**Unchanged — stay hand-rolled SVG/CSS:**

- `SouthIndianGrid.razor`, North Indian chart style (geometry in `docs/research-horoscope-software-compare.md`)
- `LifeWeeks.razor` — 4000-week grid, Dasha-lord coloured
- `PlanetPositionsTable.razor`, `SadeSatiTable.razor` — tables, not charts

---

## 2. NuGet packages

Added to `Ikiastrro.Web`:

| Package | Purpose |
|---|---|
| `Syncfusion.Blazor.Charts` | `SfChart` (column, stacked, line, range-column, heatmap), `SfPolarRadarChart` |
| `Syncfusion.Blazor.Gauge` | `SfCircularGauge` — single-strength dials (optional, §3) |
| `Syncfusion.Blazor.Themes` | `fluent2` / `bootstrap5` theme stylesheet, scoped per §4 |
| `Syncfusion.Blazor.PdfExport` *(later)* | server-side PDF for the print route |

`Program.cs`:

```csharp
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
    builder.Configuration["Syncfusion:LicenseKey"]);   // user-secrets / env, never committed
builder.Services.AddSyncfusionBlazor();
```

`_Imports.razor`: `@using Syncfusion.Blazor` + the `Charts` / `Gauges` namespaces.
Theme `<link>` goes in `App.razor` (see §4 for scoping).

---

## 3. Screen → chart mapping

| Screen / component | Visualization | Syncfusion type | Data source |
|---|---|---|---|
| Shadbala (per-planet total + 6 sub-strengths) | grouped / stacked column | `SfChart` `ColumnSeries` stacked | `tbl_Fact_*` strength facts |
| Bhava Bala (12 houses) | column | `SfChart` `ColumnSeries` | strength facts |
| Vimsopaka Bala (varga-weighted) | column | `SfChart` `ColumnSeries` | strength facts |
| Ashtakavarga (8 contributors × 12 signs) | heatmap of bindus | `SfChart` `HeatMapSeries` | Ashtakavarga facts |
| Sarvashtakavarga (12-sign totals) | column with reference line | `SfChart` `ColumnSeries` | Ashtakavarga facts |
| `DashaTimeline.razor` (Vimshottari, 3 levels) | horizontal timeline bands | `SfChart` `RangeColumnSeries`, category = level | `tbl_Chart_DashaPeriods` / `vw_Chart_DashaTimeline` |
| Planetary longitude wheel (D1…D11) | 360° polar scatter, one point per graha | `SfPolarRadarChart` `ScatterSeries`, `ValueType="Double"` on 0–360 axis | `tbl_Chart_KeyDetails.longitude` |
| Graha strength radar (compare planets at a glance) | radar | `SfPolarRadarChart` `RadarSeries` | strength facts |
| Single strength dial (e.g. current Dasha-lord Shadbala) — optional | gauge | `SfCircularGauge` | strength facts |
| Transit / `tvf_PlanetSignAtDate` over a date range | line | `SfChart` `LineSeries`, DateTime X-axis | `tvf_PlanetSignAtDate` |
| Sade Sati windows on a timeline (companion to `SadeSatiTable`) | timeline bands | `SfChart` `RangeColumnSeries` | `tvf_Chart_SadeSatiPeriods` |

Render-ready shapes live in Core (`Ikiastrro.Core`), same pattern as `ChartViewModel` —
a `ChartSeriesViewModel` / `StrengthChartViewModel` built by the analytics repos, so Web
only binds. Keeps the charting library out of Core.

---

## 4. Palette + theme reconciliation

Syncfusion ships its own theme CSS, which collides with the "one design language,
everything from `tokens.css`, dark-only" rule in `docs/uidesign-specs.md`. Mitigation:

1. **Theme base:** load `Syncfusion.Blazor.Themes` `fluent2-dark` (closest to the indigo
   dark ground). Reference it once in `App.razor`.
2. **Scope it:** wrap every Syncfusion component in a `.sf-scope` container and, in a
   non-isolated `wwwroot/css/syncfusion-overrides.css`, pin backgrounds/fonts to tokens:
   `--paper`, `--paper-raised`, `--ink`, serif stack for titles. Do **not** let Syncfusion
   CSS reach hand-rolled components (they use bare `table`/`th`/`td` selectors under CSS
   isolation — an unscoped framework sheet would break them).
3. **Chart colours:** never accept Syncfusion defaults. Pass an explicit palette from
   tokens:

   ```razor
   <SfChart Palettes="@(new[] { "#e2ad4f", "#b3a9cf", "#8f87ad", "#f0564a", /* … dignity ramp … */ })">
   ```

   Source these from the same hex values `tokens.css` defines (`--accent`, `--ink-soft`,
   `--aspect-faint`, `--danger`, the 7-tier dignity ramp). If tokens change, update this
   array — there is no runtime bridge from CSS custom properties into the `Palettes` param.
4. **Dark-only:** no `prefers-color-scheme` handling. One theme, matching the rest of the
   app.

---

## 5. Out of scope / deferred

- **Shareable horoscope infographics** (PNG/PDF one-pagers for sharing): not built. When
  needed, compose HTML/SVG and render with QuestPDF (PDF, MIT) or SkiaSharp (PNG)
  server-side — separate from the interactive charting stack above.
- **North Indian chart SVG component:** geometry researched, still hand-rolled work, not a
  Syncfusion job.
- **Animated / real-time charts:** none of the current screens need them; keep
  `EnableAnimation="false"` for print stability.

---

## 6. Chart-component evolution, snapshots & revert

The hand-rolled SVG / CSS-grid components (`src/Ikiastrro.Web/Components/Charts/`,
catalogued in that folder's `README.md`) change often — new attributes, tweaked
geometry, restyled cells. This section is how a past look stays recoverable.

### 6.1 Golden snapshots

Every release commits one rendered sample per visual component:

```
docs/artifacts/ui/<Component>-sample.svg
```

- Rendered from **one fixed fixture** (a single birth chart, defined once, never
  changed) so a diff between two releases is pure rendering change, no data noise.
- Committed on `master`, so `git show v0.5.3:docs/artifacts/ui/PolarWheel-sample.svg`
  returns exactly what that component drew at `v0.5.3` — no checkout, no build.
- `scripts/show-chart-at.ps1 -Chart PolarWheel -Tag v0.5.3` pulls it and opens it.
- The snapshot is the **arbiter of a faithful revert**: a revert is correct iff the
  current render matches `git show <oldtag>:<snapshot>` byte-for-byte.

Generation harness: `tests/Ikiastrro.Web.Tests` (bUnit `ChartSnapshotTests` +
`ChartFixture` + `SnapshotAssert`). Runs from **VS Test Explorer** (WDAC blocks
terminal `dotnet test`); `dotnet build` still compiles it. Mint / update goldens
with env `IKIASTRRO_UPDATE_SNAPSHOTS=1`. Full flow: `docs/artifacts/ui/README.md`.

### 6.2 Two rules that make revert mechanical, not archaeology

A chart component is `.razor` + `.razor.css` + shared geometry (`SvgGeometry` /
`AstroMath`) + `--*` tokens in `tokens.css`. Reverting one file only reproduces an
old look if its dependencies still mean what they did then. So:

1. **Chart tokens are namespaced and additive.** `--wheel-ring`, `--cell-fill`,
   `--vargottama`, … Never repurpose an existing token's meaning. A different look
   ⇒ a **new** token (`--wheel-ring-sq`) or a value change **dated in
   `uidesign-specs.md`**. An old `.razor.css` then still resolves against the
   current `tokens.css`.
2. **Geometry helpers version by addition.** If `AngleToXy` (or any shared
   projection) must change behaviour, add `AngleToXyV2` or a parameter — do not
   silently change the return. Old components keep compiling and rendering.

With both held, a targeted file restore almost always produces a byte-identical
snapshot.

### 6.3 Revert procedure — `vX` back to `vY`

| Fidelity | How |
|---|---|
| Fast | `git checkout vY -- src/Ikiastrro.Web/Components/Charts/<C>.razor <C>.razor.css` — then render and diff against `git show vY:docs/artifacts/ui/<C>-sample.svg`. Match ⇒ done. Mismatch ⇒ a token or helper moved; reconcile those. |
| Guaranteed | `git worktree add ../ikiastrro-vY vY` — copy the component's whole surface (`.razor`, `.razor.css`, the geometry helper, the relevant `tokens.css` lines) into `master`. Brings every dependency at its `vY` state. |
| Cleanest history | `git revert -m 1 <the feature merge that changed it>` — only if that merge was narrow. Find it: `scripts/show-chart-at.ps1 -Chart <C> -History`. |

### 6.4 If you're toggling a look across releases — keep both, don't revert

A recurring "classic vs square wheel" choice is a **variant**, not a mistake to
undo. Add `PolarWheelSquare.razor` (its own `.razor.css`) or a `Shape` parameter,
and let `ChartFrame` (or a `SegmentedToggle` / `?style=` param) pick. Each variant
carries its own golden snapshot. One extra file; zero archaeology. Note the new
variant under its `FEAT-…` row in `CHANGELOG.md`.
