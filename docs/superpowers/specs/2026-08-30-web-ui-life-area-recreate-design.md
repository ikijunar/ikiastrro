# Design Spec — Web UI recreate around life-area groups

**Status:** Approved design, pre-implementation
**Owner:** rammyps
**Created:** 2026-08-30
**Approach:** A — in-place incremental rebuild of `Ikiastrro.Web` (see "Approaches considered")
**Related:**
- `ikiastrro/README.md` (current Web state + the "hardcoded to D1+D9" known limitation this closes)
- `ikiastrro/docs/research-horoscope-software-compare.md` (UI research; cross-cutting decisions 1–7)
- `ikiastrro/src/Ikiastrro.Web/Components/DESIGN.md` (design-language rule)
- `ikiastrro/docs/reference-house-lagna-significations.md` (migration 031 design — functional nature)
- `D:\@ClaudeSpace\BookExtracts\how-to-judge-a-horoscope-1.md` (B.V. Raman — functional benefic/malefic per Lagna, p.16–18; house significations p.12–13)
- `docs/scope-jhora-coverage.md` (feature-coverage gap analysis — the "computed but invisible" list this surfaces)
- `D:\@ClaudeSpace\methods_prodmag.md` (PM backlog — mark the life-area UI on ship)

---

## 1. Goal

Recreate `Ikiastrro.Web`'s read/display layer so it (a) **surfaces data already computed but invisible today** (D2/D6/D10/D11 charts, KP sub-lord, Sade Sati, transit history, house→nakshatra span, conjunctions), (b) **re-architects the foundation** (one generic multi-varga renderer; a shared generate-and-store service used by both Web and CLI; deep-linkable section navigation), and (c) **organises a person's chart around four life areas + timing** rather than around chart types. Visual language stays the parchment/serif dark-only look; execution is refined.

**Interpretation is out of scope.** No outcome text, no scoring. A future core Python engine does synthesis. This pass computes and *displays* classical facts (including functional benefic/malefic classification) and organises them by life area.

---

## 2. Decisions locked (from the brainstorming Q&A)

| # | Decision |
|---|---|
| D1 | Effort goes to: surface hidden data + re-architect foundation + UX polish. **Not** an interpretation layer. |
| D2 | Person workspace = **one route, tabbed sections** (`/charts/{id}?tab=…`). |
| D3 | Visual language: **keep parchment/serif dark-only, refine execution.** Add the tokens the research doc names; no reinvention, no light theme. |
| D4 | Charts within a tab: **D1 fixed on the left + one selectable varga slot on the right.** Tables below follow the slot. |
| D5 | Also in scope: **reference-tables browser**, **un-hide conjunctions**, **print/PDF view**. |
| D6 | **Deferred:** North Indian chart style (research doc plan stands, not this pass). |
| D7 | Write path: **extract `ChartGenerationService`; switch both Web and CLI.** |
| D8 | Workspace organised by **five tabs — Personality & Health · Relationships · Career · Money · Timing.** Life-area is the only organising idea; Timing holds Dasha + Transits + Sade Sati. |
| D9 | Each life-area tab: its varga chart(s) + the D1 houses/karakas classically ruling that area + its own position/lordship/conjunction tables. |
| D10 | **Build functional benefic/malefic classification** per planet per Lagna, sourced from the Raman book. Interpretation of it is deferred to the Python engine. |
| D11 | Print/PDF view **stays dark** — no ink-on-white flip; `print-color-adjust: exact`. |

---

## 3. Scope

**In:** everything in §4–§13 below.
**Out:** interpretation/prediction text; ICE-scoring the result; North Indian grid; light theme; auth; any new divisional chart beyond the existing D1/D2/D6/D9/D10/D11; Ashtakavarga/Shadbala UI (those need Core work first — see `docs/scope-jhora-coverage.md`).
**Touches Core:** two renames + two new pure classes (`LagnaFunctionalNature`, `LifeAreaMap`). **Touches Data:** one new service + ~9 new repo read methods + 3 reference repos. **Touches CLI:** `Program.cs` cutover to the shared service + two new modes. **Touches DB:** migration 031 only (no chart-schema change).

---

## 4. Routing

| Route | Component | Notes |
|---|---|---|
| `/` | `Home` | rebuilt as a card gallery (compact D1 grids) + the existing dropdown as secondary jump-to |
| `/add` | `Add` | unchanged form; `HandleSubmit` re-pointed at `ChartGenerationService` |
| `/charts` | `Charts` | kept; minor polish, rendered via `DataTable<T>` |
| `/charts/{id}?tab=<t>&varga=<v>` | `ChartWorkspace` | `t` ∈ `personality-health` · `relationships` · `career` · `money` · `timing` (default `personality-health`). `v` = the right-slot varga (default = the tab's own varga). Unknown `t`/`v` fall back to defaults. |
| `/charts/{id}/life-weeks` | `LifeWeeks` | unchanged |
| `/charts/{id}/print` | `PrintChart` | **new** — flat, all sections expanded, dark, print stylesheet |
| `/reference/{planets\|signs\|nakshatras}` | `Reference` | **new** — person-independent; added to `MainLayout` nav |

Tab and varga state live entirely in the query string (bookmarkable, back-button-correct). `TabBar` renders real `<a href>` anchors.

---

## 5. Component tree (after)

```
Components/
  Layout/      MainLayout (nav gains "Reference")           [edit]
  Pages/       Home [rebuild]  Add [edit]  Charts [edit]
               ChartWorkspace [new]  PrintChart [new]  LifeWeeks [keep]
               Reference [new]  Error [keep]
  Workspace/   WorkspaceHeader [new]  TabBar [new]
    Tabs/      LifeAreaTab [new]  PersonalityHealthTab [new]  RelationshipsTab [new]
               CareerTab [new]  MoneyTab [new]  TimingTab [new]
  Charts/      VargaChartPanel [new]  SouthIndianGrid [keep]  GridPlanetGlyph [keep]
               PlanetPositionsTable [new]  HouseLordshipTable [new]
               ConjunctionsTable [new]  HouseNakshatraSpanTable [new]
               SignificatorHousesTable [new]  SignificatorKarakasTable [new]
               FunctionalNaturePanel [new]
               DashaTimeline [keep]  DashaLegend [keep]  DashaLordColors [keep]
  Timing/      GocharaPanel [new]  SadeSatiTable [new]
  Reference/   PlanetsTable [new]  SignsTable [new]  NakshatraTable [new]
  Shared/      ConfirmDialog [keep]  DeleteIconButton [keep]
               Disclosure [new]  DataTable<T> [new]  PlanetChip [new]  EmptyState [new]
```

**Deleted:** `Charts/ChartView.razor` (+`.css`), `Pages/ChartDetail.razor` (+`.css`).
**Renamed in Core:** `D1ChartViewModel` → `ChartViewModel`; `D1PlanetRow` → `PlanetRow`. Both were already chart-type-generic; rename is mechanical.
**Untouched:** `SouthIndianGrid`, `DashaTimeline`, `DashaLegend`, `DashaLordColors`, `GridPlanetGlyph`, `ConfirmDialog`, `DeleteIconButton`, `LifeWeeks`, `Error`, `App`, `Routes`, `_Imports`.

---

## 6. Generic chart rendering

### 6.1 `LoadedChart` (one shape per chart type, assembled once)

```csharp
record LoadedChart(
    string ChartType,                       // "D1" | "D2" | "D6" | "D9" | "D10" | "D11"
    string Label,                           // "Rasi" | "Hora" | "Shashtamsa" | ...
    string AscendantSign,
    IReadOnlyList<ChartKeyDetail>  KeyDetails,
    IReadOnlyList<ChartHouseLord>  HouseLords,
    IReadOnlyList<ChartAspect>     Aspects,
    IReadOnlyList<ChartConjunction> Conjunctions);
```

`ChartWorkspace.OnParametersSet` builds `IReadOnlyDictionary<string, LoadedChart>` keyed by chart type. To avoid ~24 round-trips (6 chart types × 4 tables), add **`GetByBirthDetailId(int)`** to `ChartKeyDetailsRepository`, `ChartHouseLordsRepository`, `ChartConjunctionsRepository`, `ChartAspectsRepository` (they already have `DeleteByBirthDetailId`, so the column + query pattern exist) — 4 reads total, grouped in memory by `ChartResultId`. `ChartResultsRepository.GetByBirthDetailId` already exists.

### 6.2 `VargaChartPanel.razor`

Parameters: `LoadedChart Chart`, `bool ShowAspectStrips`. Renders one `SouthIndianGrid` using the already-generic `ChartViewModel.BuildPlanetsBySign(Chart.KeyDetails)` and `ChartViewModel.BuildAspectedByGlyphs(Chart.KeyDetails, Chart.Aspects)`, with a `CenterMeta` fragment (Moon's nakshatra, Lagna lord, chart label). **No chart-type conditionals** — replaces every `D1`/`D9` branch and the duplicated `<table>` blocks in the old `ChartView`.

### 6.3 `PlanetPositionsTable.razor`

Columns: Graha · Rasi · **Degree · Nakshatra · Pada** (D1 only — gated on `Chart.ChartType == "D1"`, matching how the current D9 table omits them) · Nakshatra Lord · **KP Sub-lord** (`NakshatraSubLordPlanet`, currently stored, never shown) · Direction · Combust · House · House (Chandra) · **Functional Nature** (§7) · Dignity · Rules · Aspects · Aspected By. Built from `ChartViewModel.BuildPlanetRows` (unchanged) + a functional-nature lookup keyed on the natal D1 Lagna.

### 6.4 `DataTable<T>.razor`

`div.table-scroll` + `<thead>` from a `Column<T>[]` + `RenderFragment<T> RowTemplate`. Replaces the ~6 hand-rolled scroll-wrapper/`<thead>` blocks; single home for sticky header, zebra, responsive overflow. Used by every table component and the `/charts` and `/reference` lists.

---

## 7. Functional benefic / malefic

### 7.1 `LagnaFunctionalNature` (Core, pure — new)

```csharp
enum FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }

record FunctionalNatureResult(
    FunctionalNature Nature,     // heuristic label (see rules)
    int[] RuledHouses,           // house numbers this planet rules FROM the given Lagna (exact)
    bool IsMaraka,               // additionally lord of 2nd or 7th
    bool KendradhipatiDosha,     // natural benefic degraded by owning only an angle
    string Rationale);           // e.g. "Lord of 3rd & 6th — natural malefic house lordships"

static FunctionalNatureResult For(ZodiacName lagnaSign, PlanetName planet);
```

`RuledHouses` is exact: which sign(s) the planet rules (from `ClassicalDignity`/`AstroMath` own-sign data) → house offset via `AstroMath.CountFromSignToSign`. `Nature` is a **documented Parashari heuristic**, not gospel — the authoritative per-Lagna verdict is the book table in §7.2, shown alongside.

**Heuristic rules** (from `how-to-judge-a-horoscope-1.md` lines 567–620):
1. Rules a **kendra (1/4/7/10) AND a trikona (1/5/9)** → `Yogakaraka`. Only possible for Mars (Cancer, Leo), Saturn (Taurus, Libra), Venus (Capricorn, Aquarius) — detected, not table-driven.
2. Else rules **1st** and nothing in {3,6,11} → `Benefic` (`Neutral` if the planet is the Moon).
3. Else rules **5th or 9th** and nothing in {3,6,11} → `Benefic`.
4. Else rules only **4th/7th/10th** → `Malefic` if the planet is a natural benefic (Jupiter/Venus/Mercury/Moon) — *kendradhipati doṣa*, set `KendradhipatiDosha` — else `Benefic`.
5. Else rules any of **3rd/6th/11th** → `Malefic`.
6. Else rules only **2nd / 8th / 12th** → `Malefic`, unless the planet is Sun or Moon → `Neutral`.
7. `IsMaraka = true` additionally whenever the planet rules the 2nd or 7th (independent of `Nature`).
8. **Default / catch-all** — any other combination (mixed kendra + maraka/dusthana with no trikona; e.g. a planet ruling 2nd + 7th) → `Malefic`. This is where the heuristic and the book table (§7.2) most often diverge; the book verdict is shown alongside and is authoritative for display.

Verified by a new CLI mode **`verify-functional-nature`** (worked-example asserts, sibling of `verify-vargas`): e.g. `(Taurus, Saturn) → Yogakaraka, RuledHouses {9,10}`; `(Aries, Mercury) → Malefic, {3,6}`; `(Cancer, Moon) → Neutral, {1}`; `(Libra, Saturn) → Yogakaraka, {4,5}`; `(Capricorn, Venus) → Yogakaraka, {5,10}`; `(Aquarius, Venus) → Yogakaraka, {4,9}`; `(Pisces, Jupiter) → Neutral/Benefic, {1,10}` (document which); exit 1 on any FAIL.

### 7.2 Migration 031 — `tbl_Dim_LagnaFunctionalNature` (the cited cross-check mirror)

Follows the project's existing "table mirrors classical text; engine doesn't read it yet" pattern (`tbl_Rule_*`, `tbl_Dim_*`). 84 rows = 12 Lagnas × 7 classical planets. Seed **verbatim from `how-to-judge-a-horoscope-1.md` p.16–18** (lines 504–562):

| Lagna | Benefics (best → ) | Malefics (worst → ) | Neutrals | Yogakaraka |
|---|---|---|---|---|
| Aries | Jupiter, Mars, Sun | Mercury (3rd+6th), Saturn, Venus | — | — |
| Taurus | Saturn (9th+10th), Mercury, Mars, Sun | Jupiter, Moon | Venus (Lagna lord) | Saturn |
| Gemini | Venus | Mars (6th+11th), Jupiter, Sun | Moon, Mercury | — |
| Cancer | Mars (5th+10th), Jupiter | Venus, Mercury | Saturn, Moon, Sun | Mars |
| Leo | Mars, Sun | Mercury, Venus | Jupiter, Moon, Saturn | — |
| Virgo | Venus | Moon, Mars, Jupiter | Saturn, Sun, Mercury | — |
| Libra | Saturn (4th+5th), Mercury, Venus; Mars = feeble benefic | Sun, Jupiter, Moon | — | Saturn |
| Scorpio | Moon, Jupiter, Sun | Mercury, Venus | Mars, Saturn | — |
| Sagittarius | Mars, Sun | Venus, Saturn, Mercury | Jupiter, Moon | — |
| Capricorn | Venus (5th+10th), Mercury, Saturn | Mars (worst), Jupiter, Moon | Sun (8th lord) | Venus |
| Aquarius | Venus (4th+9th), Sun, Mars | Jupiter, Moon | Mercury | Venus |
| Pisces | Moon, Mars | Saturn, Sun, Venus, Mercury | Jupiter | — |

**Three rows seeded `FunctionalNature = NULL, Notes = 'Not classified in source (How to Judge a Horoscope, Raman, p.16–18)'`** — the book never classifies them: **Aries → Moon**, **Gemini → Saturn**, **Aquarius → Saturn**. Do not guess.

Column shape (aligns with `reference-house-lagna-significations.md` migration 031 sketch):
`Id TINYINT PK`, `LagnaSignId TINYINT FK tbl_SignAttributes`, `PlanetId TINYINT FK tbl_Planets`, `FunctionalNature VARCHAR(12) NULL CHECK (Benefic|Malefic|Neutral|Yogakaraka)`, `Rank TINYINT NULL` (1 = best/worst within its class, per the book's "best benefic"/"worst malefic" phrasing), `Notes VARCHAR(120) NULL`. Migration number **031** (030 reserved for house significations, per that doc). Base-DDL sync + a `SourceCitation` comment in the migration.

`LagnaFunctionalNatureRepository` (Data): `GetForLagna(byte lagnaSignId) → IReadOnlyList<LagnaFunctionalNatureRow>`.

### 7.3 Surfacing in the UI

- **`FunctionalNature` column** on every `PlanetPositionsTable`: a badge showing the `Nature` enum value — Benefic ● / Malefic ● / Neutral ● / **Yogakaraka ★** — plus an additive **▲ Maraka** marker when `IsMaraka` (it is not a `Nature` value). Computed by `LagnaFunctionalNature.For(natalD1Lagna, planet)`; tooltip = `RuledHouses` + `Rationale`. Colours: Benefic → `--dignity-good`, Malefic → `--dignity-enemy`, Neutral → `--dignity-neutral`, Yogakaraka → `--functional-yogakaraka` (new, = `--accent`); the Maraka marker → `--danger`.
- **`FunctionalNaturePanel.razor`** on the Personality & Health tab: all 7 classical grahas, each showing computed `Nature`, `RuledHouses`, one-line `Rationale`, **and Raman's verdict from `tbl_Dim_LagnaFunctionalNature`** where present (the 3 NULL rows show "not classified in source"). Where computed and book disagree, both are shown — expected, and informative for the later engine.

---

## 8. `LifeAreaMap` (Core, static — new)

```csharp
enum LifeArea { PersonalityHealth, Relationships, Career, Money }

record LifeAreaSpec(int[] Houses, PlanetName[] Karakas, string[] Vargas, string DefaultVarga);

static readonly IReadOnlyDictionary<LifeArea, LifeAreaSpec> Specs;
```

Significations sourced from B.V. Raman house significations (`how-to-judge-a-horoscope-1.md` p.12–13) — the same source basis as migration 030 `tbl_Dim_HouseSignification`. A comment TODO: switch to reading `tbl_Dim_HouseSignification` / `tbl_Dim_PlanetHouseKaraka` once migration 030 ships.

| Area | Houses | Karakas | Vargas (default) |
|---|---|---|---|
| Personality & Health | 1, 6, 8, 3 | Sun, Moon, Saturn, *(Lagna lord, dynamic)* | D1, **D6** |
| Relationships | 7, 5, 2, 11, 4 | Venus, Jupiter, Moon, Mercury | **D9** |
| Career | 10, 6, 7, 2, 11, 1 | Sun, Saturn, Mercury, Jupiter | **D10** |
| Money | 2, 11, 9, 5, 12 | Jupiter, Venus, Mercury | D2, **D11** *(slot toggles D2 ⇄ D11)* |

"Lagna lord" as a karaka is resolved per chart (not a fixed planet) — the panel adds it dynamically.

---

## 9. Life-area tab anatomy (`LifeAreaTab.razor`)

Parameterised by `LifeArea Area` + the loaded-charts dictionary. `PersonalityHealthTab` / `RelationshipsTab` / `CareerTab` / `MoneyTab` are each ~5 lines: `<LifeAreaTab Area="LifeArea.Career" Charts="charts" Slot="@varga" />`.

Layout, top to bottom:
1. `<VargaChartPanel Chart=charts["D1"]>` and `<VargaChartPanel Chart=charts[slot]>` side by side. `slot` = `varga` query param, default `Specs[Area].DefaultVarga`. Money's slot selector toggles D2/D11; other areas' slot is fixed to their one varga (D1 always beside it).
2. **`SignificatorHousesTable`** — `charts[slot].HouseLords` filtered to `Specs[Area].Houses`: house · sign · lord · "lord placed in H_n, sign, dignity".
3. **`SignificatorKarakasTable`** — `charts["D1"].KeyDetails` filtered to `Specs[Area].Karakas` (+ dynamic Lagna lord for Personality & Health): planet · rasi · house · dignity · retro · combust · functional nature.
4. **`FunctionalNaturePanel`** — Personality & Health tab only (§7.3).
5. In `<Disclosure>` (collapsed): `PlanetPositionsTable`, `HouseLordshipTable`, `ConjunctionsTable`, `HouseNakshatraSpanTable` — all for `charts[slot]`.

Missing varga → that pane shows `<EmptyState>` ("D6 not computed — run `backfill-charts`"); the rest of the tab still renders.

---

## 10. Timing tab (`TimingTab.razor`)

Not a life area — its own component, no `LifeAreaMap` entry.
1. **Vimshottari Dasha** — `<DashaTimeline Roots="dashaTree" />` (unchanged) + link to `/charts/{id}/life-weeks`.
2. **`GocharaPanel`** (new) — current sidereal sign of Sa/Ju/Ra from `tvf_PlanetSignAtDate(@planetId, GETDATE())`, shown against natal sign + house-from-natal-Moon, with "in since / next change" from `tbl_PlanetSignTransitEvents`. Empty-state if `tbl_PlanetSignTransitEvents` unpopulated.
3. **`SadeSatiTable`** (new) — `tvf_Chart_SadeSatiPeriods(@birthDetailId)`: the 3 Sade Sati Dhaiyas + Kantaka (4th-from-Moon) + Ashtama (8th-from-Moon) periods, each with start/end and an "active now" marker.

---

## 11. Shared write path — `ChartGenerationService` (Data, new)

Owns the "given a persisted `BirthDetails`, compute and store everything" pipeline currently duplicated in `Add.razor` and in five spots of `Ikiastrro.Cli/Program.cs` (`backfill-analytics`, `recompute-keydetails`, `backfill-charts`, `backfill-dasha`, main add flow).

```csharp
class ChartGenerationService   // ctor: ChartCalculationOrchestrator, VimshottariDashaService,
{                              //       ChartResultsRepository + the 4 tbl_Chart_* repos
    GenerationReport GenerateAll(BirthDetails bd);                        // every registered chart type + Vimshottari Dasha
    GenerationReport GenerateMissing(BirthDetails bd);                    // only chart types absent for this person  (= backfill-charts)
    GenerationReport RecomputeAnalytics(BirthDetails bd, string? type);  // re-derive analytics for existing ChartResults  (= backfill-analytics / recompute-keydetails)
}
record GenerationReport(IReadOnlyList<string> ChartTypesWritten, bool DashaWritten, IReadOnlyList<string> Skipped);
```

**Boundaries:**
- Starts from a `BirthDetails` that already has an `Id`. Place resolution + the `BirthDetails` insert **stay in the callers** (async/network; not a data-layer concern).
- Composes `VimshottariDashaService.ComputeAndStore` (already persists + is idempotent — it deletes then re-inserts the `VimshottariDasha` ChartResult + periods). Not absorbed.
- Idempotent per `(BirthDetailId, ChartType)` via the existing `DeleteByBirthDetailId*` methods.
- Wraps each person's persist in **one transaction** — `Add.razor` has none today, so a mid-pipeline failure leaves a half-populated chart.
- Closes a real gap: **`Add.razor` does not compute Dasha today** (only the CLI does) → web-added people have an empty Timing tab until a manual `compute-dasha`. `GenerateAll` fixes it.

**Cutover:**

| Caller | After |
|---|---|
| `Add.razor` `HandleSubmit` | resolve place → insert `BirthDetails` → `gen.GenerateAll(bd)` → navigate |
| CLI main add flow | `gen.GenerateAll(bd)` |
| CLI `backfill-charts` | `gen.GenerateMissing(bd)` |
| CLI `backfill-analytics` / `recompute-keydetails` | `gen.RecomputeAnalytics(bd, filter)` |
| CLI `backfill-dasha` | unchanged (`dashaService.ComputeAndStore`) or `gen` for symmetry |
| CLI `compute-all <name>` | **new mode** — thin wrapper over `GenerateAll` (replaces running `backfill-charts` + `backfill-dasha` separately) |

The CLI is a flat top-level-statements script with no DI container — construct one `ChartGenerationService` near the top from `SqlConnectionFactory`-built repos and reuse it, removing the five suffixed-repo blocks. Web registers it in `Program.cs` DI.

No DB schema change, no Core change (orchestrator/analyzer untouched).

---

## 12. Data sources — every panel

| Panel / view | Source | New work |
|---|---|---|
| Varga grids, planet positions, house lordship, conjunctions | `tbl_Chart_*` via `GetByBirthDetailId` batch reads | 4 repo methods |
| Significator houses / karakas | above, filtered by `LifeAreaMap` | — |
| Functional nature (column + panel) | `LagnaFunctionalNature.For(...)` + `tbl_Dim_LagnaFunctionalNature` | Core class + migration 031 + repo |
| House → rāśi → nakshatra | `vw_Chart_HouseNakshatraSpan` | 1 repo method |
| Dasha timeline / life-weeks | `DashaPeriodsRepository` (exists) | — |
| Gochara | `tvf_PlanetSignAtDate` + `tbl_PlanetSignTransitEvents` | 2 repo methods |
| Sade Sati / Kantaka / Ashtama | `tvf_Chart_SadeSatiPeriods` | 1 repo method |
| Reference browser | `tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras` + `Padas` + `SubLords` | 3 new repos |
| Home card gallery | `BirthDetailsRepository.GetAll` + `ChartKeyDetailsRepository.GetByBirthDetailId` | — |

---

## 13. Cross-cutting UI

### 13.1 Token additions (`tokens.css`; dark-only, one value each per DESIGN.md)
- **`--planet-sun` … `--planet-ketu`** (9 **solid** hues) — the canonical per-graha identity palette. The existing `--dasha-*` block is re-expressed as sharing these exact hues (alias, or a comment stating equivalence) so Dasha, life-weeks, chips and grid all key off one planet palette. Run the dataviz skill's `validate_palette.js` for **all pairs** (any two planets can share a grid cell), not just Vimshottari adjacency; per the `--dasha-*` comment, 9-hue all-pairs CVD separation will not fully clear — colour stays a scan aid, the glyph text is always present.
- **`--planet-sun-bg` … `--planet-ketu-bg`** (9 **tints**) — each ≈ 18–24 % of its solid hue mixed into `--paper`, pre-baked (inspectable, same as `--asc-glow` is a pre-baked rgba). These are the background fills for §13.5; `--ink` stays legible on every one (verified at build).
- **`--cell-fill`, `--lagna-fill`, `--grid-stroke`, `--sign-text`, `--muted-text`** — semantic grid tokens so `SouthIndianGrid` stops referencing `--paper`/`--ink` directly (research doc decision 4).
- **`--functional-yogakaraka`** = value of `--accent` (gold). The only new functional-nature colour; the rest reuse `--dignity-*` / `--danger`.

### 13.2 New shared components
- **`Disclosure.razor`** — `<button aria-expanded>`, `grid-template-rows: 0fr→1fr` animation, `inert` when collapsed (almamesh `Disclosure.tsx` pattern).
- **`DataTable<T>.razor`** — §6.4.
- **`TabBar.razor`** — the 5 tabs as `<a href="?tab=…">`, active from the query param.
- **`PlanetChip.razor`** — Sanskrit-primary / English-secondary name (logic currently inline-static in `ChartView.PlanetDisplayName`/`PlanetSecondaryName` — move here). Renders on its `--planet-<x>-bg` fill with a solid `--planet-<x>` left border (2 px); `--ink` text. One `Planet` param drives both name and colour. This is the single shared unit every planet/lord background in §13.5 uses.
- **`EmptyState.razor`** — icon + message + optional `<code>` CLI hint; one consistent look for every "not computed yet".

### 13.3 Home (card gallery)
Saved people as cards: compact `SouthIndianGrid` (`Compact=true` — built for this, never used) + name/DOB/place, linking to `?tab=personality-health`. Existing `<select>` kept as a secondary jump-to. Cheap via the batch read.

### 13.4 Reference browser (`/reference/*`)
Three read-only pages in `MainLayout` nav:
- `/reference/planets` — `tbl_Planets` (9): name, Sanskrit, natural nature, conditional rule, `VimshottariYears`, rules-sign flag.
- `/reference/signs` — `tbl_SignAttributes` (12): name, Sanskrit, ruling planet, element, modality, gender, exalted/debilitated planet+degree, Moolatrikona range. NULL descriptive fields (`RisingType`, etc.) render `—`.
- `/reference/nakshatras` — `tbl_Nakshatras` (27); each row `Disclosure`-expands to its 4 `tbl_NakshatraPadas` (rasi, navamsa sign) and, nested, its 9 `tbl_NakshatraSubLords`.

Three thin Dapper repos (`PlanetsReferenceRepository`, `SignAttributesRepository`, `NakshatraReferenceRepository`) — plain `SELECT` reads matching the existing repo style. All rendered via `DataTable<T>`.

### 13.5 Chart-element punch — per-planet backgrounds

Goal: the grid and the planet/lord tables should read at a glance, each graha carrying its identity colour. Applied via `--planet-<x>-bg` fills (low-alpha tints; `--ink` text stays legible on the dark theme — verified at build), with the solid `--planet-<x>` reserved for a border/marker. `PlanetChip` (§13.2) is the shared unit.

| Element | Treatment |
|---|---|
| `SouthIndianGrid` planet glyph | glyph sits in a rounded pill filled with `--planet-<x>-bg`; the **dignity dot stays** (different axis), retro/combust markers stay. Compact mode: same pill, no dignity dot. |
| `PlanetPositionsTable` — Graha cell | the planet name as a `PlanetChip` (cell tint only, not the whole 14-column row). |
| `HouseLordshipTable` — Lord cell · `SignificatorHousesTable` — Lord cell | the lord planet as a `PlanetChip` — this is the "planet lords" differentiation called for. "Placed in H_n, sign, dignity" text stays plain. |
| `SignificatorKarakasTable`, `FunctionalNaturePanel` | planet identified by `PlanetChip`. |
| `GocharaPanel` | each transiting graha row headed by its `PlanetChip`. |
| Dasha timeline / life-weeks | already planet-coloured; now via the unified `--planet-*` hues (`--dasha-*` aliased). No behaviour change. |

Additional "punch" tuning, kept within the existing language, finalised with rammyps during Phase 1–2 (precedent: `D1ChartView` is the one piece that got hands-on design):
- **Lagna cell** — lift the current faint `--asc-glow` (0.16 rgba) to a clearer state: inset `--accent-line` ring + slightly raised fill.
- **House-number badge** — filled `--paper-raised` chip instead of bare text.
- **Aspect strip** — keep dashed/faint, but its planet chips adopt the `--planet-*` identity tints so an aspecting graha reads in its own colour.
- **Section headers** — thin `--accent-line` underline on the serif `h2`s for vertical rhythm.
- **Home card gallery** — compact grids inherit the planet pills, making thumbnails scannable.

Non-negotiable: colour is a scan aid, never the sole signal — every planet mark also carries its glyph/name text (the `--dasha-*` comment's established rule, extended to backgrounds).

### 13.6 Print view (`/charts/{id}/print`)
Dedicated route. Every section flat and fully expanded (no tabs, no disclosures): header · all 6 South-Indian grids · all positions/lordship/conjunction tables · `FunctionalNaturePanel` · Dasha (Maha + Antar) · Sade Sati · Gochara. **Stays dark** (D11). `@media print`: hide nav/footer/buttons; `print-color-adjust: exact` + `-webkit-print-color-adjust: exact` so the dark backgrounds render into PDF; `break-inside: avoid` on grids and table rows; `break-before: page` before each major section. "Print / Save as PDF" button in `WorkspaceHeader`.

### 13.7 Empty & error states
Via `<EmptyState>`: no D1 → "run `compute-all {name}`"; varga missing → per-pane message, siblings still render; no Dasha → existing message (Gochara/Sade Sati independent); `tbl_PlanetSignTransitEvents` empty → "run `backfill-planet-transits`". `notFound` (bad id) and the `MainLayout` Blazor error UI unchanged.

---

## 14. Build sequencing

App builds and runs after every phase. Phase 0 is self-contained Core/Data/CLI work with no UI change — the implementation plan may split it into its own document (`…-groundwork`) ahead of the UI plan (`…-ui`).

- **Phase 0 — Core + Data groundwork (no visible UI change).** (1) Renames `D1ChartViewModel`→`ChartViewModel`, `D1PlanetRow`→`PlanetRow`. (2) `LagnaFunctionalNature` + `LifeAreaMap` + `verify-functional-nature` CLI mode. (3) `GetByBirthDetailId` on the 4 chart repos; repos for the Sade Sati / transit / house-nakshatra-span reads; 3 reference repos. (4) `ChartGenerationService` + `Add.razor`/CLI cutover + `compute-all` CLI mode. (5) Migration 031 + `LagnaFunctionalNatureRepository`.
- **Phase 1 — Shared primitives.** `tokens.css` additions — the 9 `--planet-*` solids + 9 `--planet-*-bg` tints + grid semantics + `--functional-yogakaraka` (+ `validate_palette.js` all-pairs run + a build check that `--ink` clears contrast on every `-bg`) — then `DataTable<T>`, `Disclosure`, `TabBar`, `PlanetChip` (with its background treatment), `EmptyState`. Dropped into the *existing* `ChartView`/`ChartDetail` first (including the `SouthIndianGrid` glyph pill) so they're exercised before the workspace exists.
- **Phase 2 — Workspace shell + Personality & Health.** `ChartWorkspace` (replaces `ChartDetail`), `WorkspaceHeader`, `VargaChartPanel`, the four table components, `SignificatorHouses/KarakasTable`, `FunctionalNaturePanel`, `LifeAreaTab` + `PersonalityHealthTab` — all planet/lord cells rendered through `PlanetChip` (§13.5). Lagna-cell / header / aspect-strip punch tuning done here with rammyps. `ChartView.razor` deleted.
- **Phase 3 — Remaining areas.** `Relationships` / `Career` / `Money` tabs; `HouseNakshatraSpanTable` into the shared `LifeAreaTab` body.
- **Phase 4 — Timing tab.** `TimingTab`, `GocharaPanel`, `SadeSatiTable`; `DashaTimeline` moves in unchanged. `ChartDetail.razor` retired.
- **Phase 5 — Non-workspace pages.** Home card gallery; `/reference/*`; `/charts/{id}/print` + `print.css`.
- **Phase 6 — Cleanup & docs.** Dead CSS/params; `dotnet format`; `README.md` (Web section — the "hardcoded to D1+D9" limitation is now removed), `ikiastrro.md` dated entry, `methods_prodmag.md` (mark shipped), memory pointer.

---

## 15. Verification model (no test project)

- **Build gate** after every phase — `dotnet build` clean.
- **CLI assertion modes** — existing `verify-vargas` + new `verify-functional-nature` both pass (exit 0).
- **SQL cross-checks** — migration 031: 84 rows, exactly 3 with `FunctionalNature IS NULL` (Aries/Moon, Gemini/Saturn, Aquarius/Saturn); `backfill-charts` / `backfill-analytics` on the 5 existing people = 0 net new rows (idempotency); `compute-all` on `1_Ramakrishnan` reproduces every previously-verified fact.
- **Golden record** — `1_Ramakrishnan` (22 Apr 1981, Chennai): a ~12-item checklist (Sun exalted Aries; Moon debilitated Scorpio, Anuradha pada 2, H8; Rahu H4 / Ketu H10; Jupiter & Saturn retrograde in Virgo; Mars Moolatrikona; Rahu/Ketu mutual 7th aspect; functional natures for Aries Lagna: Jupiter Benefic, Mercury Malefic(3,6), Venus Malefic, Saturn Malefic, Sun Benefic, Mars Benefic, Moon → "not classified in source") re-confirmed visually after Phase 2 and Phase 4.
- **Live browser smoke test** (the SwissEphNet-rebuild precedent) — add a new person via `/add`: all 6 charts + Dasha present; all 5 tabs render; each life-area tab shows its significator tables; Timing shows Dasha + Gochara + Sade Sati; `/print` renders; `/reference/*` render.
- **Colour** — `validate_palette.js` run recorded for the 9 `--planet-*` hues (all pairs); every `--planet-*-bg` fill passes a WCAG-AA contrast check against `--ink`; the 9 planet pills are visually distinguishable in the grid, in tables, and in the dark `/print` PDF; every planet mark still carries its glyph/name (colour never the sole signal).

---

## 16. Open questions / risks

1. **`ChartAspect` for varga slots** — `ChartWorkspace` must load aspects for every varga, not just D1/D9. `GetByBirthDetailId` on `ChartAspectsRepository` covers it; confirm the aspect rows exist for D2/D6/D10/D11 (they should — `ChartAnalyzer` writes them for every type).
2. **Functional-nature heuristic vs. book disagreements** — expected for mixed-lordship planets (e.g. Libra/Mars: book "feeble benefic", heuristic likely `Maraka`+`Malefic`). The UI shows both; the spec accepts divergence rather than forcing the heuristic to reproduce Raman's judgment. Confirm this is acceptable at review.
3. **`verify-functional-nature` expected values** — the Pisces/Jupiter case (rules 1st + 10th) resolves to `Yogakaraka`? No — Jupiter can't be a kendra+trikona yogakaraka per the book (only Mars/Saturn/Venus); rules 1 (trikona+kendra) and 10 (kendra) → rule 1 gives `Benefic` (1st, nothing in {3,6,11}). Lock the full assertion list during Phase 0.
4. **Migration 031 `Rank` column** — the book phrases "best benefic" / "worst malefic" / "next benefic". Capture as `Rank` 1..n within `(Lagna, class)`, or drop `Rank` and keep only the ordered `Notes` text. Recommend keeping `Rank` (nullable) — cheap, and the Python engine may want it.
5. **`tvf_PlanetSignAtDate` signature** — takes `@PlanetId, @Date`; confirm it's Sa/Ju/Ra only (Mars excluded per the transit-table scope) so Gochara shows exactly those three.

---

## 17. Out of scope / future

- North Indian grid (`NorthIndianGrid.razor`) — research doc plan stands; a later pass.
- Interpretation / prediction text and life-area scoring — the core Python engine.
- Ashtakavarga / Shadbala / Avastha panels — need Core computation first (`docs/scope-jhora-coverage.md` Tiers 3).
- Additional vargas (D3/D7/D12/D30/D60 …) — `docs/scope-jhora-coverage.md` §2.
- Compare-two-people (synastry) view.
- Light theme / theme toggle.
- Switching `LifeAreaMap` / `LagnaFunctionalNature` to read `tbl_Dim_*` instead of hardcoded C# (blocked on migration 030 shipping and the project's open "engine reads reference tables" decision).

---

## Approaches considered

- **A — in-place incremental rebuild (chosen).** Same project, keep the working primitives, rebuild tab-by-tab, app runnable throughout. Lowest risk; honours "keep the design language."
- **B — parallel component tree, cut over at the end.** Justified only when the live app must keep serving during a long migration — not the case for a 2–3 user private tool. More total work.
- **C — new `Ikiastrro.Web2` project.** Re-solves DI/layout/place-resolver/delete-cascade for no gain; fights "keep the design language."
