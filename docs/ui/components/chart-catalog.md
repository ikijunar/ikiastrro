---
last_updated: 2026-09-09
workstream: ui
togaf: C — component catalogue
---

# Component — hand-rolled chart catalogue

All chart diagrams are hand-drawn inline SVG / CSS grid. Source lives in
`src/Ikiastrro.Web/Components/Charts/` (see that folder's `README.md` for the per-component
projection contract).

| Component | Draws | Notes |
|---|---|---|
| `SouthIndianGrid` | the 4×4 sign-position grid, every chart type | enriched — `PlanetChip` glyphs, dual house badges, aspect strip, `SpecialPointLabels`; details in [`south-indian-grid.md`](south-indian-grid.md) |
| `MiniGrid` | compact glyphs-only `SouthIndianGrid` thumbnail | used on Home + `SavedCharts` |
| `PolarWheel` | 360° sidereal ring, one point per graha | inline `<svg>`; `?aspects=1` draws hand-drawn aspect chords (same-sign pairs filtered) |
| `ChartFrame` | grid ⇄ wheel toggle around `SouthIndianGrid | PolarWheel` | `SegmentedToggle` |
| `VargottamaStrip` | D1/D9 same-sign lit chips | `--vargottama` token |
| `DashaTimeline` | Vimśottari 3-level tree; `Compact` Maha-only mode for the workspace dock | opens the current chain on first render |
| `HouseLordshipTable` / `ConjunctionsTable` / `PlanetPositionsTable` | tables, not charts | MudBlazor + tokens |
| `GocharaPanel` | current transit sign + since/next-change | over `tbl_PlanetSignTransitEvents` / `tvf_PlanetSignAtDate` |
| `LifeWeeks` | 4000-week grid, 52 col/row, `--dasha-*` colours | hover tooltips for exact dates/lords |
| `SadeSatiTable` | Saturn-from-Moon affliction windows, merged + date-ordered | `tvf_Chart_SadeSatiPeriods` |
| `TransitWheel` | natal ↔ transit | over a supplied SVG template; [`transit-wheel.md`](transit-wheel.md) |

## Golden-snapshot flow

Every visual component commits `docs/artifacts/ui/<Component>-sample.svg`, rendered from one
fixed fixture (never changed) so a diff is pure rendering change. Harness:
`tests/Ikiastrro.Web.Tests` (bUnit `ChartSnapshotTests` + `ChartFixture` + `SnapshotAssert`),
run from VS Test Explorer or `dotnet test`. Mint / update with env
`IKIASTRRO_UPDATE_SNAPSHOTS=1`; review the SVG diff before committing. Full flow:
`docs/artifacts/ui/README.md`.

## Revert

`git checkout <ref> -- src/Ikiastrro.Web/Components/Charts/<C>.razor <C>.razor.css`, render,
diff against `git show <ref>:docs/artifacts/ui/<C>-sample.svg`. Byte-match ⇒ done; mismatch ⇒
a token or geometry helper moved — reconcile those (they version by addition, so this is rare).
