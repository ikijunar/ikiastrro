---
last_updated: 2026-09-09
workstream: ui
version: v1
togaf: E — Solution increment
safe: Feature (full-surface)
---

# wkstream_UI_v1 — the current app UI

The scope of the UI as it stands: a MudBlazor light-theme shell over the warm Iki-Astrro
brand, trimmed to the surfaces the astrologer actually reads. This describes what is **live**.
The full re-do is now in scoping — [`wkstream_UI_v2.md`](wkstream_UI_v2.md); v1 stays the
live-state reference until v2 ships.

## What v1 is

- **App shell** — `MudLayout` / `MudAppBar` / `MudMainContent`, MudBlazor light theme, shared
  tokens. The brand lockup + tagline in the header; nav Home · Preferences · Saved Charts; no
  duplicate nav row on Home.
- **Brand** — warm canvas `#FAF5EA`, midnight blue `#0F2041`, sunset orange `#F47A24`,
  Manrope, three interface type sizes. Primary actions: midnight fill, sunset text + border.
  Preserved: Ganesha illustration, Navagraha arrangement, mountain/footer art, dedication
  footer. Full guide: [`brand.md`](brand.md).
- **The four kept read surfaces** — South-Indian template, transit wheel, astrologer evidence,
  and the Home design — plus the varga-centric workspace (`/charts/{id}`), varga view, timing,
  saved-charts and life-weeks. Full route list: [`MASTER.md`](MASTER.md).
- **Chart rendering stays hand-rolled** inline SVG / CSS grid (`SouthIndianGrid`, `PolarWheel`,
  `MiniGrid`, `ChartFrame`, `LifeWeeks`) — MudBlazor does the chrome, not the diagrams.
  Catalogue + golden-snapshot flow: [`components/chart-catalog.md`](components/chart-catalog.md).

## What v1 deliberately dropped

Life-area tabs, the tabbed `ChartWorkspace` / `TabBar`, the "Life Spiral" radial timeline, the
`AllCharts` template gallery, the reading-profile drawer/lens, `ChartInsights`,
`DispositorTable`, and the `/charts/{id}/print` route. North-Indian chart style is researched,
not built.

## Data flow

`WorkspaceData.Load(...)` — one batch load over the `GetByBirthDetailId` repos → an
`IReadOnlyDictionary<string, LoadedChart>` keyed by chart code that every page renders from.
`GocharaRepository` backs the Timing page's Gochara panel. No page issues its own per-chart
queries; nothing recomputes.

## Verification

`tests/Ikiastrro.Web.Tests` (bUnit) — golden-SVG snapshots per visual component from one fixed
fixture; run from VS Test Explorer or `dotnet test`; mint with `IKIASTRRO_UPDATE_SNAPSHOTS=1`.
Live browser smoke test against `/`, `/charts/{id}`, a dense chart, and a recent-birth chart.

## Open in v1

- `/add` restyle + Sex field (`FEAT-UI-03`).
- Preferences / ayanāṁśa route (`FEAT-UI-12`).
- Outer-label crowding + small mobile text on the transit wheel.
- Pointer-drag on the transit wheel is rolled back (browser-lifecycle issues); date + dasha
  selectors are the movement inputs.
