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
| [`wkstream_UI_v1.md`](wkstream_UI_v1.md) | What is **live** — MudBlazor shell, brand override, the four kept surfaces |
| [`wkstream_UI_v2.md`](wkstream_UI_v2.md) | **In scoping** — the full UI re-do: analysis-first IA + ROADMAP *Now* surfacing + `/add` / Preferences |
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
| `/` | `Home` | brand lockup + Ganesha/Navagraha art; searchable name over saved people; inline Preferences (top-left) + inline Add | **v2 rebuild in progress** ([`components/home.md`](components/home.md)) |
| ~~`/add`~~ | — | folded into Home in v2 | retired |
| `/charts` | `SavedCharts` | sortable person table + `MiniGrid` thumbnail + inline-confirm delete | verified |
| `/charts/{id}` | `Workspace` | D1 hero (`ChartFrame` grid⇄wheel), `VargaRail` over all 21, D1 positions table, compact dasha strip, birth/computation panel | verified |
| `/charts/{id}/varga/{code}` | `VargaView` | one varga in full — grid + wheel, `VargottamaStrip`, positions, house-lordship + conjunctions disclosure, prev/next | verified |
| `/charts/{id}/south-indian-template` | `SouthIndianTemplate` | the print-style South-Indian D1 template, light/dark toggle | verified |
| `/charts/{id}/timing` | `Timing` | Vimśottari dasha tree + Sade Sati + Gochara | verified |
| `/charts/{id}/evidence` | `AstrologerEvidence` | read-only evidence tables in reading order, chart selector | verified |
| `/charts/{id}/life-weeks` | `LifeWeeks` | 4000-week grid coloured by Mahādaśā | verified — **retired in v2** |
| `/transit-wheel` · `/transit-wheel/{id}` | `TransitWheel` | fixed natal ring + transit layer, date + Mahā/Antar selectors, comparison table | verified |

## Navigation

Shared MudBlazor header (`MudAppBar`): brand lockup **Iki-Astrro | Where Passion, Purpose &
Planets Align.** Nav order Home · Saved Charts (Preferences is an inline Home control in v2,
not a nav item). Chart-page person names in sunset orange.

## In flight — `wkstream_UI_v2`, screens 1–2 (Home)

- **`FEAT-UI-02`** — Home rebuilt on MudBlazor: `MudAutocomplete` name search; two-column
  canvas layout with Ganesha art right; the three size tokens; sunset-orange button fill.
- **`FEAT-UI-03`** — Add folded into Home: `Add New` unhides Name · Sex · DOB · Time · City ·
  Country; completing Country generates + routes to `/transit-wheel/{id}`.
- **`FEAT-UI-12`** — Preferences disclosure at Home top-left: Ayanāṁśa (default *Lahiri*) +
  Chart Type (South Indian default / North Indian; extensible). `localStorage` for now.

## Planned

- **`wkstream_UI_v2`** — full re-do, now in scoping ([`wkstream_UI_v2.md`](wkstream_UI_v2.md)).
  Absorbs `FEAT-UI-03` / `FEAT-UI-12` and the "Missing Web" column of the `masterproduct.md`
  rollup — divisional charts D2–D60, Chara Karakas, avastha states, the slow-planet transit
  timeline, strength (Ṣaḍbala / Bhāva Bala).
- Codex works one path scope on `workstream/ui` — proposed `src/Ikiastrro.Web/Components/Charts/**`
  (confirm); Claude owns the shell and integrates.
