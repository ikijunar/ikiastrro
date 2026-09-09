# South Indian Chart Template (D1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans.

**Goal:** Build the user's reference "dense D1 chart card" design as a new, standalone template,
reusing existing engines/data — no new calculations.
**Architecture:** A new Blazor page (`SouthIndianTemplate.razor`) composing two new components
(`D1TemplateGrid`, `DashaRoundBox`) over data already produced by `WorkspaceData`/`ChartViewModel`/
`SadeSatiRepository`; one new shared helper (`SadeSatiRounds`) extracted from `SadeSatiTable.razor`.
**Tech Stack:** .NET 8 / Blazor Server, existing repositories, no DB changes.
**Spec:** `docs/superpowers/specs/2026-09-05-southindian-chart-template-design.md`

## Research
Status: [x] complete
| Research doc | Needed for | Status |
|---|---|---|
| Codebase read (SouthIndianGrid, ChartKeyDetail, CharaKaraka, SpecialPointCalculator, SadeSatiTable) | Confirming what's already computed vs. new work | complete |

## Global Constraints
- Branch: `feat/southindian-chart-template`
- Commit trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01MnFNJnUZ1M7pCGjyuL96Vz
  ```
- Do not push unless asked.
- No test project — verify via `dotnet build` + running the app against the local DB.

---

## Task 1: Extract shared Sade-Sati grouping/age logic
**Files:** Create `Ikiastrro.Core/Presentation/SadeSatiRounds.cs`; Modify `SadeSatiTable.razor`
**Interfaces:** Consumes `SadeSatiPeriod`. Produces `Window`/`Round` records + `BuildWindows`/
`BuildRounds`/`FormatAge`/`FormatDateSpan`/`FormatLine`.
- [x] Step 1: Move `GroupContiguous`/`Window`/`Age` out of `SadeSatiTable.razor` into
  `SadeSatiRounds` (behavior-preserving).
- [x] Step 2: Add `BuildRounds` — groups Sade Sati/Kantaka/Ashtama windows by chronological index
  into up to 3 `Round`s, flagging the one nearest `asOf` to show dates.
- [x] Step 3: Update `SadeSatiTable.razor` to call the shared helper; confirm `/charts/{id}/timing`
  still renders identically.

## Task 2: Plain aspect-label formatter
**Files:** Modify `Ikiastrro.Core/Presentation/ChartViewModel.cs`
**Interfaces:** Consumes `ChartKeyDetail`/`ChartAspect`. Produces `BuildAspectedByPlain`.
- [x] Step 1: Factor `BuildAspectedByGlyphs`'s per-sign grouping into a private `BuildAspectedBy`
  taking a chip-formatter delegate.
- [x] Step 2: Add `BuildAspectedByPlain` (plain "Ju-3" labels, number always shown) as a sibling.

## Task 3: New template components
**Files:** Create `Components/Charts/D1TemplateCellData.cs`, `D1TemplateGrid.razor(.css)`,
`DashaRoundBox.razor(.css)`
**Interfaces:** Consumes `TemplatePlanetLine`/`TemplatePointLine` DTOs + per-sign dictionaries;
`SadeSatiRounds.Round`. Produces the rendered grid/round-panel markup.
- [x] Step 1: `D1TemplateCellData.cs` — `TemplatePlanetLine`, `TemplatePointLine`.
- [x] Step 2: `D1TemplateGrid.razor` — fixed 12-cell layout + merged center cell; per-cell rows
  (Ras(L)/Hor(L) header, house-triplet row, sign/Asc, planet lines, upagraha row, ASP row).
- [x] Step 3: `DashaRoundBox.razor` — flank/center variants over one `SadeSatiRounds.Round`.
- [x] Step 4: New `--tmpl-*` light palette CSS (declared on the page wrapper, consumed cross-scope
  by both components' `.razor.css`).

## Task 4: Page + wiring
**Files:** Create `Components/Pages/SouthIndianTemplate.razor(.css)`
**Interfaces:** Consumes `WorkspaceData.Load`, `SadeSatiRepository`, `ChartViewModel`,
`SadeSatiRounds`. Route: `/charts/{Id:int}/south-indian-template`.
- [x] Step 1: DI + `OnParametersSet` loading D1 + Sade Sati periods + birth date.
- [x] Step 2: `PlanetsBySign`/`PointsBySign`/`UpagrahasBySign`/`KarakaRows` shaping methods.
- [x] Step 3: Compose title bar + flanking `DashaRoundBox`es + `D1TemplateGrid` with the karaka
  strip + center `DashaRoundBox` as `CenterContent`.
- [x] Step 4: Commit.

## Task 5: Verify
- [x] `dotnet build` — 0 warnings, 0 errors.
- [x] Ran the app (local `ikiastrro` DB, birth-detail id 1) — `/charts/1/south-indian-template`
  HTTP 200, all data populated correctly (screenshot-compared against the reference image).
- [x] `/charts/1/timing` regression check after the `SadeSatiTable` refactor — unchanged.
- [x] Found + fixed a real bug during verification (`SS@Round.Index` not evaluating — needed
  `@(Round.Index)`).

## PRODUCT.md
No FEAT row change — this is Web-UI-only, reusing existing engine features (KARAKA-01/02,
TRANSIT-02) with no calculation-engine work.
