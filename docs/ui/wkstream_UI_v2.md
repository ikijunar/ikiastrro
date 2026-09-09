---
last_updated: 2026-09-09
workstream: ui
version: v2
status: scoping
togaf: E — Solution increment
safe: Feature (full-surface)
---

# wkstream_UI_v2 — full UI re-do

A ground-up rework of the astrologer-facing app. [`wkstream_UI_v1.md`](wkstream_UI_v1.md)
stays the description of what is **live** until v2 ships; this doc is the increment.

> **Status: scoping.** The delivery list below is fixed (it is ROADMAP *Now* plus the open
> `FEAT-UI` rows). The **design decisions** section is open and needs a short pass with the
> product head before build starts. Nothing here is built yet.

## Why a re-do (not more component docs)

- v1 deliberately dropped most of the analytical surfaces — `ChartInsights`, `DispositorTable`,
  life-area tabs, the `AllCharts` gallery, the reading-profile lens — to ship a clean chart-first
  shell. The product is an **evidence model**, not a chart renderer (`masterproduct.md` →
  Product intent); the information architecture should lead with analysis, not with a chart grid.
- The ROADMAP *Now* tier is entirely UI-surfacing of engine features that are verified but not
  shown. That is more than a component can carry — it changes navigation and page structure.
- One coherent pass is cheaper than six bolt-ons and one snapshot re-mint per bolt-on.

## What v2 must deliver

| From | Feature | Issue | Milestone |
|---|---|---|---|
| ROADMAP Now | Divisional charts D2–D60 in the UI (`FEAT-VARGA-01`) | #8 | Divisional charts in the UI |
| ROADMAP Now | Jaimini chara karakas panel (`FEAT-KARAKA-01`) | #9 | Jaimini chara karakas panel |
| ROADMAP Now | Planetary-state / avastha display (`FEAT-AVASTHA-01/02`) | #10 | Planetary-state (avastha) display |
| ROADMAP Now | Slow-planet transit history view (`FEAT-TRANSIT-01`) | #11 | Slow-planet transit history view |
| Open `FEAT-UI` | Add/Edit form — MudBlazor restyle + Sex field (`FEAT-UI-03`) | #4 | — |
| Open `FEAT-UI` | Preferences / ayanāṁśa route (`FEAT-UI-12`) | #5 | — |

Every existing live route in [`MASTER.md`](MASTER.md) is re-homed or replaced — none is dropped
without a decision recorded below.

## Design decisions (open — the pass)

1. **Information architecture / navigation** — analysis-first vs the current chart-first shell.
   What the top-level nav is; whether the varga workspace stays the hub.
2. **Re-introductions** — which v1-dropped surfaces come back (`ChartInsights`, `DispositorTable`,
   reading-profile, life-area grouping) and which stay retired.
3. **Chart rendering** — keep hand-rolled inline SVG / CSS grid (`design-language.md`,
   `dataviz.md`); Syncfusion stays a deferred option, not a dependency. (Default: keep.)
4. **Evidence surfacing** — how `vw_Chart*` evidence views drive the new pages.
5. **Routing & deep-linking** — route shape for 21 vargas + panels; shareable URLs.
6. **Mobile** — the v1 open items (transit-wheel outer-label crowding, small mobile text) are
   fixed as part of v2, not carried forward.

## Constraints carried from v1 (not up for change)

- **MudBlazor** is the component system for chrome / forms / tables / dialogs — light theme,
  warm Iki-Astrro brand ([`brand.md`](brand.md), [`design-language.md`](design-language.md)).
- **Tokens, not raw values** (`wwwroot/css/tokens.css`); token changes are additive.
- **CSS isolation** per component (`Component.razor.css`).
- **Chart diagrams stay hand-rolled** — MudBlazor does chrome, not diagrams.
- **UI reads persisted rows only** and never recomputes — `tbl_Chart_*`, `tbl_Fact_*`, `vw_*`,
  `tbl_Rule_Ayanamsa` ([`../architecture/domain-contracts.md`](../architecture/domain-contracts.md)).
  A calculation the UI needs is a DB + CLI feature first.
- **Data flow** — one batch load (`WorkspaceData.Load` contract) → a dictionary keyed by chart
  code; no per-chart queries in pages.

## Workstream mechanics

- Branch `workstream/ui`, worktree `…\ikiastrro.wt\ui`, path scope `src/Ikiastrro.Web/` +
  `tests/Ikiastrro.Web.Tests/` (`STANDARDS.md` §E.1). **Claude Code is primary** — owns the
  shell, layout, pages, routing, `wwwroot`, and integration to `master`.
- **Codex — one assigned path scope** (§E.2 AGENT-02): **`src/Ikiastrro.Web/Components/Charts/**`**
  — the hand-rolled chart components, bounded and golden-snapshot-guarded. *(Proposed split;
  confirm the subtree before Codex starts.)* Codex does not touch shell / pages / routing /
  tokens; Claude reviews and integrates every Codex change.

## Verification

- `tests/Ikiastrro.Web.Tests` (bUnit) golden-SVG snapshots — **re-minted** as v2 components land
  (`IKIASTRRO_UPDATE_SNAPSHOTS=1`); each snapshot diff noted against its `FEAT-…` row.
- Live browser smoke test against every re-homed route + a dense chart + a recent-birth chart.
- No regression in the 11 `verify-*` CLI modes (UI is read-only over persisted rows).

## Done when

Every row in *What v2 must deliver* is `Web [x]` in `masterproduct.md`, every live route is
re-homed or a drop is recorded here, snapshots are re-minted, and the browser smoke passes.
