# South Indian Chart Template (D1) — design

**Status:** implemented · **Created:** 2026-09-05 · **Branch:** feat/southindian-chart-template
**Features:** none new in PRODUCT.md (UI-only; no calculation-engine changes) — see Non-goals

## Research
Status: [x] complete
| Research doc | Needed for | Status |
|---|---|---|
| `docs/reference-calculations.md` §9 | Sade Sati/Kantaka/Ashtama semantics for the "Round" panels | complete |
| Codebase read of `Engines/Karakas/*` (CharaKaraka, SpecialPointCalculator, UpagrahaCalculator) | What's already computed vs. what's new work | complete |

## 1. Problem
The user supplied a reference screenshot of a denser, single-page South Indian D1 chart layout
(title bar, 3 chronological Sade-Sati "Round" boxes, an 8-karaka strip + Sade-Sati panel in the
center cell, per-cell house/aspect/upagraha rows) and asked for it as a new template — distinct
from the compact `SouthIndianGrid.razor` already used across Workspace/VargaView/PrintChart/
SavedCharts.

## 2. Goals
- A standalone page rendering the reference design for a person's D1 chart, built entirely from
  data this codebase already computes and persists (no new astronomical engines).
- Reuse existing shaping logic (`ChartViewModel`, `WorkspaceData`) and existing per-row fields
  (`CharaKaraka`, `HouseNumberFromLagna/Moon`, `DegreesInSignDecimal`) rather than recomputing
  anything.
- Extract the Sade-Sati window-grouping/age logic (previously private inside `SadeSatiTable.razor`)
  into a shared, reusable place, since this template needs the same grouping bundled differently
  (into 3 chronological "Rounds" rather than one flat list).

## 3. Non-goals
- Does not touch or replace `SouthIndianGrid.razor` — a deliberately separate component
  (2026-09-05 explicit call).
- Does not implement the other 9 classical upagrahas (only Gulika/Maandi exist in this codebase);
  the upagraha row ships with those 2 and is laid out so the rest is a data change later, not a
  redesign.
- No nav link and no print stylesheet yet — standalone route only
  (`/charts/{Id:int}/south-indian-template`).
- No dark-theme integration — this template intentionally uses its own light "chart card" palette,
  not the app's `tokens.css` dark theme.
- D1 only — the title's subtitle ("Personality, Expression, Logic") is fixed text, not
  parametrized per varga.

## 4. Design

**Layout:** a horizontal row of 3 elements — a flanking Round-ONE box, the 4×4 South Indian grid
(fixed sign positions, same convention as `SouthIndianGrid`) with its merged 2×2 center cell, and
a flanking Round-THREE box.

**Per-cell content (`D1TemplateGrid.razor`), top to bottom:**
1. Ras(L)/Hor(L) header tags — shown only above the Pisces/Aries cells (best-guess reproduction of
   the reference image; see Open decisions).
2. A house-triplet row: `[house-from-Lagna] [code] [house-from-Moon]` for every chara-karaka-
   bearing graha and every special-lagna point (AL, A2–A12, HL) in that sign — the same two-axis
   idea `SouthIndianGrid`'s gold/silver house badges already use, generalized here.
3. The sign's English name, and an "Asc" tag if the Ascendant sits there.
4. One line per graha placed in that sign: glyph, `(..)` parens if retrograde, whole degrees in
   sign (`+N`), 🔥 if combust.
5. An upagraha row (`Gk`/`Md` — Gulika/Maandi only).
6. An `ASP: Ju-3,Me-7` row — planets aspecting anything in that sign, with the house-offset number
   always shown.

**Center cell:** the 8 Jaimini chara karakas as 2 rows of 4 (`AK Ra · AmK Ve · BK Sa · MK Ju` /
`PiK Su · PK Mo · GK Ma · DK Me`), then the Round-TWO panel.

**Round panels (`DashaRoundBox.razor`):** each of the 3 chronological Sade-Sati cycles bundles that
cycle's Sade Sati (3 Dhaiyas already folded into one window) + Kantaka Shani + Ashtama Shani
windows. Position is fixed by chronological order (cycle 1 = left, cycle 2 = center, cycle 3 =
right). Independently, whichever cycle's span is nearest to today (past or future) renders its 3
lines as real calendar dates; the other two render `Age N–M` ranges.

**Color theme:** a dedicated light palette (`--tmpl-*` custom properties declared on
`SouthIndianTemplate.razor.css`'s wrapper, read cross-scope by the child components' own
`.razor.css` files — the same inheritance trick `SouthIndianGrid.razor.css` documents for
`--paper`/`--ink`) reproducing the reference image's cream/black/orange/gold/navy look, entirely
separate from the app's dark `tokens.css`.

## 5. Data / schema
No schema changes. Reused fields, all already on `ChartKeyDetail` (persisted for every row,
Graha or special point alike):
- `CharaKaraka` (AK..DK), `HouseNumberFromLagna`, `HouseNumberFromMoon`, `DegreesInSignDecimal`,
  `IsRetrograde`, `IsCombust`, `PointKind`, `Planet`, `Sign`.
- `SadeSatiPeriod` rows via `SadeSatiRepository.GetByBirthDetailId`.

New shared code (no new tables/columns):
- `Ikiastrro.Core.Presentation.SadeSatiRounds` — `Window`/`Round` records, `GroupContiguous`,
  `BuildWindows` (moved from `SadeSatiTable.razor`), `BuildRounds` (new), `FormatAge`/
  `FormatDateSpan`/`FormatLine` (new).
- `ChartViewModel.BuildAspectedByPlain` — sibling to `BuildAspectedByGlyphs`, same per-sign
  grouping, plain "Ju-3" label instead of the dashed-chip "Ma(a)-3" format.
- `Ikiastrro.Web.Components.Charts.TemplatePlanetLine` / `TemplatePointLine` — small DTOs shaping
  raw `ChartKeyDetail` rows for `D1TemplateGrid`.

## 6. Verification
- `dotnet build` on `Ikiastrro.slnx` — 0 warnings, 0 errors.
- Ran the app against the local `ikiastrro` DB (birth-detail id 1, "Ramakrishnan") and loaded
  `/charts/1/south-indian-template` — HTTP 200, all 12 signs render, the center cell shows all 8
  karakas + a Round-TWO panel with real calendar dates (`Nov 2011 – Jan 2020`, etc.), Round-ONE/
  THREE show age ranges, retrograde parens / 🔥 combust / `ASP:` / upagraha rows all populated
  with real data, no server exception.
- Confirmed `/charts/1/timing` (`SadeSatiTable`) still renders correctly after the extraction
  refactor — same windows, same Age column values.
- Found and fixed one real bug during verification: `SS@Round.Index` rendered as literal text
  instead of evaluating (Razor's implicit-expression parsing needs `@(Round.Index)` when directly
  preceded by non-whitespace text) — fixed in `DashaRoundBox.razor`.

## 7. Risks & mitigations
- **Ras(L)/Hor(L) exact meaning** was never fully pinned down (see Open decisions) — mitigated by
  building the generically useful house-triplet mechanism (confirmed) and treating the two header
  labels as a best-guess placement, easy to move/relabel once compared against the source image
  directly.
- **Upagraha row is incomplete** (2 of 11) — mitigated by scoping it as an explicit non-goal and
  laying out the row as a plain joined-code list so adding more is additive.
- **Palette is a first-pass color read of a screenshot**, not sampled pixel values — mitigated by
  isolating all of it into named `--tmpl-*` tokens in one place, so retuning is a one-file edit.

## 8. Open decisions
- Exact intended content of the "1 · code · 1" boxes under Ras(L)/Hor(L) — implemented as
  house-from-Lagna/house-from-Moon around a karaka or special-point code (confirmed mechanism),
  but the reference image showed only one worked example ("1 Ak 1"), so the header placement and
  which codes appear there should be checked against intent once viewed side-by-side.
- Whether this template should eventually get a nav link, a print stylesheet, or be parametrized
  for other divisional charts (D9, ...) — explicitly deferred, not decided against.
