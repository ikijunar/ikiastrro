---
last_updated: 2026-09-09
workstream: ui
togaf: C — UI standards
---

# UI — data visualization

## Current approach: hand-rolled inline SVG / CSS

Every chart surface in the app is hand-drawn — no charting library is referenced by
`Ikiastrro.Web`:

| Surface | Component |
|---|---|
| South-Indian chart grid (D1…D60) | `SouthIndianGrid` (shared, enriched) |
| 360° sidereal longitude wheel | `PolarWheel` (inline `<svg>` ring; optional `?aspects=1` chords) |
| Compact chart thumbnail | `MiniGrid` |
| Grid ⇄ wheel toggle frame | `ChartFrame` |
| 4000-week life calendar | `LifeWeeks` (52-col grid, `--dasha-*` colours) |
| Natal ↔ transit wheel | `TransitWheel` (over a supplied SVG template) |

Render-ready shapes live in `Ikiastrro.Core` (`ChartViewModel` pattern) — Web only binds.
Palettes come from `tokens.css` values, never library defaults. Colour is always a scan aid,
never the sole signal — every mark carries text too.

## Deferred option: Syncfusion Blazor

A single charting library (Syncfusion Blazor, Community License) was evaluated for
strength bars, Ashtakavarga heatmaps, dasha timelines and a polar wheel. **It was not
taken** — the hand-rolled SVG stands and no Syncfusion package is referenced. It remains a
documented option for a later dataviz pass if the strength/Ashtakavarga screens need it;
the fallback within that option is Blazor-ApexCharts (MIT). North-Indian chart style is
researched, still hand-rolled work either way.

## Golden snapshots

Every visual component commits one rendered `docs/artifacts/ui/<Component>-sample.svg` from a
single fixed fixture, so a diff between two points is pure rendering change. The snapshot is
the arbiter of a faithful revert (`git show <ref>:docs/artifacts/ui/<C>-sample.svg`). Harness:
`tests/Ikiastrro.Web.Tests` (`docs/artifacts/ui/README.md`).
