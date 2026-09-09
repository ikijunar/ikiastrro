---
last_updated: 2026-09-09
workstream: ui
togaf: C — Application Architecture (UI)
safe: Solution Intent — UX
---

# UI workstream — MASTER (App UI)

**Branch** `workstream/ui` · **worktree** `D:\@ClaudeSpace\ikiastrro.wt\ui` ·
**owns** `src/Ikiastrro.Web/`, `tests/Ikiastrro.Web.Tests/`.

`Ikiastrro.Web` — Blazor Server, MudBlazor, warm light brand. Reads persisted rows only
([`../architecture/domain-contracts.md`](../architecture/domain-contracts.md)); never
recomputes.

## Docs

| Doc | For |
|---|---|
| [`wkstream_UI_v1.md`](wkstream_UI_v1.md) | The current full-UI scope — MudBlazor shell, brand override, the four kept surfaces |
| [`brand.md`](brand.md) | Canonical palette, typography, lockup, preserved assets |
| [`design-language.md`](design-language.md) | Token + component authoring rules |
| [`dataviz.md`](dataviz.md) | Charting approach — hand-rolled SVG now, Syncfusion as a deferred option |
| [`components/transit-wheel.md`](components/transit-wheel.md) | Natal ↔ transit wheel spec |
| [`components/south-indian-grid.md`](components/south-indian-grid.md) | The enriched South-Indian chart grid + template page |
| [`components/evidence-tables.md`](components/evidence-tables.md) | The astrologer evidence page |
| [`components/home.md`](components/home.md) | Home / entry screen |
| [`components/chart-catalog.md`](components/chart-catalog.md) | The hand-rolled chart component catalogue + snapshot flow |

## Screen inventory (live routes)

| Route | Page | Shows | State |
|---|---|---|---|
| `/` | `Home` | brand lockup + Ganesha/Navagraha art; name filter over saved people; add-new | verified |
| `/add` | `Add` | birth-details entry form → `ChartGenerationService.GenerateAll` | **in progress** — unstyled native inputs, no Sex field |
| `/charts` | `SavedCharts` | sortable person table + `MiniGrid` thumbnail + inline-confirm delete | verified |
| `/charts/{id}` | `Workspace` | D1 hero (`ChartFrame` grid⇄wheel), `VargaRail` over all 21, D1 positions table, compact dasha strip, birth/computation panel | verified |
| `/charts/{id}/varga/{code}` | `VargaView` | one varga in full — grid + wheel, `VargottamaStrip`, positions, house-lordship + conjunctions disclosure, prev/next | verified |
| `/charts/{id}/south-indian-template` | `SouthIndianTemplate` | the print-style South-Indian D1 template, light/dark toggle | verified |
| `/charts/{id}/timing` | `Timing` | Vimśottari dasha tree + Sade Sati + Gochara | verified |
| `/charts/{id}/evidence` | `AstrologerEvidence` | read-only evidence tables in reading order, chart selector | verified |
| `/charts/{id}/life-weeks` | `LifeWeeks` | 4000-week grid coloured by Mahādaśā | verified |
| `/transit-wheel` · `/transit-wheel/{id}` | `TransitWheel` | fixed natal ring + transit layer, date + Mahā/Antar selectors, comparison table | verified |

## Navigation

Shared MudBlazor header: brand lockup **Iki-Astrro | Where Passion, Purpose & Planets Align.**
Nav order Home · Preferences · Saved Charts. Chart-page person names in sunset orange.

## In flight

- **`FEAT-UI-03`** — restyle `/add` to MudBlazor + shared tokens; add the **Sex** field
  (`tbl_BirthDetails.Sex` exists); geocoding-failure fallback.
- **`FEAT-UI-12`** — Preferences route with an Ayanāṁśa selector defaulting to the active
  `tbl_Rule_Ayanamsa` system default (no route exists yet).

## Planned

- Surface strength (Ṣaḍbala / Bhāva Bala), Chara Karakas, avastha states, and the slow-planet
  transit timeline as each DB+CLI feature lands — the "Missing Web" column of the
  `masterproduct.md` rollup.
- `wkstream_UI_v2` when a full revamp is next warranted.
