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
