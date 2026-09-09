---
last_updated: 2026-09-09
workstream: ui
version: v2
status: scoping
togaf: E — Solution increment
safe: Feature (full-surface)
---

# wkstream_UI_v2 — full UI re-do (table-first)

A ground-up rework of the astrologer-facing app on **one pattern: the
[`AstrologerEvidence`](components/evidence-tables.md) page**. [`wkstream_UI_v1.md`](wkstream_UI_v1.md)
stays the description of what is **live** until v2 ships; this doc is the increment.

> **Status: scoping.** The pattern and the design system below are decided. The delivery list
> is fixed (ROADMAP *Now* + the open `FEAT-UI` rows). Route consolidation and which v1-dropped
> surfaces return are the remaining open items — a short pass with the product head. Nothing
> here is built yet.

## The pattern — AstrologerEvidence, everywhere

Every read surface in v2 is the `AstrologerEvidence` shape, restyled to MudBlazor:

- **Page = sticky header + entity/chart selector + a section index (anchor nav) + N
  collapsible sections.** Each section is a table.
- **Tables are the primary representation.** The generic `EvidenceTable` (columns + rows,
  auto-labelled headers, typed value formatting) generalises to a shared `SectionTable`.
- **Every row comes from a persisted view or table** — `vw_Chart_Consolidated`,
  `vw_ChartMoonContext`, `vw_ChartPlanetEvidence`, `vw_ChartShadbala`, `vw_ChartBhavaBala`,
  `vw_ChartYogaEvaluations`, the reference dimension tables. No page recomputes
  ([`../architecture/domain-contracts.md`](../architecture/domain-contracts.md)).
- **A chart selector** switches the position-dependent sections between D1 and any stored varga.
- **Hand-rolled SVG diagrams** (`SouthIndianGrid`, `PolarWheel`, transit wheel)
  are *secondary* — embedded beside the table where a picture aids reading, never the primary
  view. They stay in Codex's scope (see Workstream mechanics) and outside the MudBlazor restyle.

## What v2 must deliver

Each item is one or more table sections on the pattern above.

| From | Feature | As | Issue · Milestone |
|---|---|---|---|
| ROADMAP Now | Divisional charts D2–D60 (`FEAT-VARGA-01`) | the chart selector drives the Positions + Dignity/Avastha + Karaka sections across all 21 vargas | #8 · Divisional charts in the UI |
| ROADMAP Now | Jaimini chara karakas (`FEAT-KARAKA-01`) | a Karakas section — AK…DK → planet, longitude, degree-in-sign, per chart | #9 · Jaimini chara karakas panel |
| ROADMAP Now | Avastha display (`FEAT-AVASTHA-01/02`) | rows in the Dignity & Avastha section — AgeState, WakefulnessState | #10 · Planetary-state (avastha) display |
| ROADMAP Now | Slow-planet transit history (`FEAT-TRANSIT-01`) | a Transit History section — sign-ingress events (planet, from→to, date, retro) with a date-range filter | #11 · Slow-planet transit history view |
| Open `FEAT-UI` | Add / Edit person (`FEAT-UI-03`) | **inline on Home** — `Add New` unhides Name · Sex · DOB · Time · City · Country; completing Country → `/transit-wheel/{id}` | #4 |
| Open `FEAT-UI` | Preferences (`FEAT-UI-12`) | **inline on Home, top-left disclosure** — Ayanāṁśa selector (default *Lahiri*) + Chart Type selector (South Indian default / North Indian; extensible) | #5 |

Also folded in (the "Missing Web" rollup column): Ṣaḍbala / Bhāva Bala already have sections
7–8 in the AstrologerEvidence plan.

## Design system (decided — enforced, no exceptions)

Extends [`brand.md`](brand.md) / [`design-language.md`](design-language.md). v1's
`AstrologerEvidence.razor.css` (hard-coded `.82rem`, `var(--surface,#fff)` fallbacks) is
**not** compliant and is the first thing v2 fixes.

- **Font family** — Manrope only, every element. No second family.
- **Exactly three sizes** — the existing tokens in `tokens.css`, nothing else in the app:
  - `--font-size-display` — page title only
  - `--font-size-tagline` — section titles, table captions, the lockup tagline
  - `--font-size-control` — table cells, controls, body, nav
  Weight, colour and spacing carry all other hierarchy. *(Values may be retuned for a dense
  table app during the pass; the token names do not change.)*
- **Sunset orange (`--brand-sunset` `#F47A24`) is the highlight / background accent** —
  **button backgrounds** (primary actions), active section in the index, selected chart in the
  selector, table row hover / selected, focus ring. **This changes `brand.md`'s current action
  rule** (midnight fill / orange text → orange fill); brand.md is updated when v2 lands.
- **Midnight blue (`--brand-midnight`)** — headings, body text, table structure, nav text.
- **Canvas / surface** — `--brand-canvas` page, `--brand-surface` raised (section cards, rows).
- **Tabular numerals** on every numeric column (degrees, scores, dates, periods).
- **Tokens only** — `var(--…)` from `wwwroot/css/tokens.css`; the MudBlazor theme is wired to
  the same tokens. No raw hex, no named colours, no inline `<style>`, no per-component size
  literals.

## MudBlazor mapping

| v1 | v2 |
|---|---|
| `<table class="dt">` | `MudTable` / `MudSimpleTable` (dense) |
| `<select>` chart picker | `MudSelect` |
| `<details>` / `<summary>` sections | `MudExpansionPanels` / `MudExpansionPanel` |
| hand-rolled sticky top nav | `MudAppBar` in `MudLayout` |
| section index `<nav>` | `MudNavMenu` or an anchor `MudChipSet` |
| `EmptyState` | `MudAlert` / `MudPaper` empty pattern |

## Navigation (v2 route map)

`AstrologerEvidence` stops being a side route and **becomes the person hub** (`/charts/{id}`).
Home absorbs Preferences and Add — no `/preferences`, no `/add`.

- `/` — **Home**: Preferences disclosure (top-left) + searchable Name + saved-people list +
  `Add New` → inline entry fields → `/transit-wheel/{id}`. Full spec:
  [`components/home.md`](components/home.md).
- `/charts` — saved people, one table
- `/charts/{id}` — **the hub**: context · moon/tithi · positions (D1 + varga selector) ·
  dignity & avastha · karakas · shadbala · bhava bala · yoga — all table sections
- `/charts/{id}/timing` — dasha tree + Sade Sati + gochara (tables)
- `/charts/{id}/transits` — transit-history table + the wheel as a secondary visual
- `/charts/{id}/south-indian-template` — the one print-style visual (Codex scope)
- `/transit-wheel/{id}` — the post-add landing (wheel; Codex scope)

Retired: `/preferences` and `/add` (inline on Home); `/charts/{id}/evidence` and
`/charts/{id}/varga/{code}` (folded into the hub); **`/charts/{id}/life-weeks`** — the
4000-week grid is dropped in v2 (the Vimśottari timeline is served by `/charts/{id}/timing`).
`wkstream_UI_v1`'s already-dropped surfaces stay dropped unless the pass re-introduces one.

## Workstream mechanics

- Branch `workstream/ui`, worktree `…\ikiastrro.wt\ui`, path scope `src/Ikiastrro.Web/` +
  `tests/Ikiastrro.Web.Tests/` (`STANDARDS.md` §E.1). **Claude Code is primary** — owns the
  shell, layout, pages, routing, `wwwroot`, tokens, and integration to `master`.
- **Codex — one assigned path scope** (§E.2 AGENT-02): **`src/Ikiastrro.Web/Components/Charts/**`**
  — the hand-rolled SVG diagrams, bounded and golden-snapshot-guarded, outside the MudBlazor
  restyle. *(Proposed subtree; confirm before Codex starts.)* Codex does not touch shell /
  pages / routing / tokens; Claude reviews and integrates every Codex change.

## Verification

- `tests/Ikiastrro.Web.Tests` (bUnit) — table-section snapshots + the SVG golden snapshots
  re-minted as components land (`IKIASTRRO_UPDATE_SNAPSHOTS=1`); each diff noted against its
  `FEAT-…` row.
- A token-lint check (or review gate): no raw hex, no size literals, one font family.
- Live browser smoke test against every route + a dense chart + a recent-birth chart.
- No regression in the 11 `verify-*` CLI modes (UI is read-only over persisted rows).

## Done when

Every row in *What v2 must deliver* is `Web [x]` in `masterproduct.md`; the hub renders all
table sections on the design system with zero token violations; retired routes are gone or
recorded; snapshots re-minted; browser smoke passes; `brand.md` updated for the action-colour
change.
