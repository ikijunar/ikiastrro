---
last_updated: 2026-10-06
workstream: ui
component: AstroFacts
route: /astro-facts/{id}
togaf: C — component spec
---

# Component — Astro Facts

> **Chart (2026-10-01):** the natal chart beside the step tabs and the All Charts cards stay on `SindUniDtlGrid` (the master chart, themed through the page tokens). The Spl Lagnas **grid** view is **SIND-UNI-3** with the special-lagna bars, the same view as Key Inference's chart; see [`spec_SIND-UNI_GridChart.md`](spec_SIND-UNI_GridChart.md). The chart toolbar's **KEY INFERENCE →** and each All Charts card open that chart in Key Inference.

`AstroFacts.razor` — **all 6 in-page steps built**, plus step 7 (All Charts) as the separate
`/charts/{id}` route (its heading literally reads "7. ALL CHARTS" so the numbering stays
consistent across the two routes). Final step order (2026-09-16/17, superseding the round-1
6-step plan below): 1 D1-Transit, 2 About, 3 Strength (3.1 Planet Strength / 3.2 House Strength /
3.3 Asthavarga / 3.4 Amsabala), 4 Spl Lagnas (was "Karakas"), 5 Yogas, 6 Vargas. This is the
spec-of-record for the **round-2 redesign** (2026-09-11), which
restructures the original flat "ASTRO FACTS header, 8 sub-tabs" shape into a **numbered UX
flow**. Mockups:

- Round 1 (flat tabs, frozen): [`../../artifacts/ui/v2-mockup/chart-evidence-hub.html`](../../artifacts/ui/v2-mockup/chart-evidence-hub.html) `#astro-facts`
- **Round 2 (this spec, under review):** [`../../artifacts/ui/v2-mockup/astro-facts-v2.html`](../../artifacts/ui/v2-mockup/astro-facts-v2.html)

Design rule for this page: **one step, ideally one chart (hand-rolled SVG — bar / stacked
bar / donut, no library, same discipline as `design-language.md`) + one primary table.**
Bent on purpose at several steps, each noted there: About Houses and About Planets are
table-only (no chart), Strength runs two short tables with different columns instead of one,
and Planet-Chart shares one chart + one grid across four related views of the same 16-varga
dataset, with the rest as inline tags rather than more tables.

`TIME PERIOD (DASHA)` and `SATURN TIME PERIOD` are **unchanged** by this round — still
separate headers alongside this flow, not steps in it.

**2026-09-17 — per-step spec series started:** step detail is moving out of this file's flow
table into one dedicated `specs_KI_<step>.md` per step. Step 4 is the first to move (see its row
below); the rest stay inline here until split out the same way in later work.

## The flow

| Step | Chart | Table | Source |
|---|---|---|---|
| **1 · D1 / Transit** | D1 South-Indian grid (D1 Birth tab) · natal+transit wheel with date/dasha selector (Current Transit tab) | D1 position table only (Planet · Sign · Degree · Nakṣatra · Pāda · Nakṣatra/Sub-lord chain · direction · house from Lagna/Moon) + D1 Birth / Current Transit toggle; analytical role/condition columns belong exclusively to 2.2 | `vw_ChartPlanetEvidence` (D1) · `tbl_TransitPositionReference` |
| **2 · Transit — Gochara judgement** (2026-10-01) | — | `GocharaTable` under the transit positions, rows slowest first (Saturn, Jupiter, Rāhu, Ketu, Moon, Venus, Mars, Mercury, Sun): each transiting planet's house from the natal Moon, **Reads as** (Good / Good, blocked / Not favourable — Rāhu read by Saturn's row and Ketu by Mars's per PVR ch.25, the nodes never blocking each other), the Vedha house and the planets blocking it, and its own D1 BAV bindus in the sign it crosses (banded by `StrengthBands.BhinnaAshtakavargaBindus`, shown beside the verdict, not counted). A flag line names Saturn's phase from the Moon (Sāḍe Sātī rising/peak/setting, Kaṇṭaka, Aṣṭama — the `tvf_Chart_SadeSatiPeriods` offsets). Follows the date/daśā picker. Computed live by `GocharaReading` (Core) | `tbl_Rule_GocharaVedha` (migration 158) via `TransitStrengthRuleRepository` + `GocharaVedhaCalculator` + `vw_ChartAshtakavarga` |
| **1.4 · Relationships** | `GrahaDrishtiMatrix` — D1/D9/D10 selector, focus-body summary, heat matrix, discrete ordinal badges, and selected-cell evidence breakdown | Sphuṭa percentage and discrete aspect metadata remain visually distinct; chart-matched conjunction groups follow the matrix. This is the sole full-detail relationship owner. | `vw_ChartGrahaDrishtiStrengths` via `GrahaDrishtiStrengthRepository` + `tbl_Chart_MultiGrahaConjunction(+Member)` |
| **1.2 · About Houses** | — | House Lord Placement (with a **Life matters** column linking each house's matters into Key Inference — `LifeMatterHouseIndex`, see `specs_key_inference_page.md` "Links to and from Astro Facts"); House Lord Key Findings; **What acts on each house** (2026-10-01, `HouseVerdictTable`/`HouseVerdicts`: per house, the lord and its functional nature, benefics and malefics in or aspecting it, Raman's count verdict from `HouseBeneficMaleficCalculator`, and PVR step 5's helping / hindering planets from `TargetInfluences` with the same Argala facts as Key Inference — follows the chart picker, computed live); Argala &amp; Virodhargala | `tbl_Chart_HouseLords` + `vw_ChartHouseLordInterpretation` + `tbl_Fact_Argala` + `tbl_Rule_LifeMatterFocus` |
| — supporting cards | — | Arudha padas (A1…A12, AL) · Upagrahas (11) · Special Lagnas — **computed, previously never surfaced** | `tbl_Chart_KeyDetails` `PointKind IN ('Arudha','Upagraha','SpecialLagna')` |
| **1.3 · About Planets** | — (closeness-to-exaltation bar dropped 2026-09-14; see note below) | functional nature + ruled houses + independent ownership flags (māraka, bādhaka, dusthāna, triṣaḍāya, Kendrādhipati doṣa) + rationale disclosure + dignity + Chara Kāraka + exaltation point + Δ + closeness, one row per planet/point | `tbl_Chart_KeyDetails` (+ Moon pañchāṅga facts card from `vw_ChartMoonContext`) |
| **1.1 · Overview** | — | `PlanetPositionsD1Transposed` — the default natal key table, with grahas as columns and Degree / Direction / House / Rāśi / Rāśi Lord / Nakshatra / Nakshatra Lord / Sub-Lord Chain / Nakshatra Pāda rows | `tbl_Chart_KeyDetails` via `ChartViewModel.BuildPlanetRows` |
| **1.2 · About Signs & Nakshatras** (new 2026-09-22) | — | `RasiNakshatraTable` — one row per graha (+ Lagna): Sign, Nakshatra, Lord Relation, Combined Character, expand-to-reveal Main Significations / Potential Benefits / Potential Disadvantages / Judgment Note / Aspecting Signs. Closes `FEAT-NAKSHATRA-02`'s last open item — the 36-row Rāśi×Nakṣatra combination table had zero Web/CLI consumer before this | `tbl_Rule_RasiNakshatraCombination` (migration 124) via `RasiNakshatraCombinationRepository.GetAll`, joined client-side against each graha's own `SignId`/`NakshatraId` from `tbl_Chart_KeyDetails` |
| **3 · Strength** | 3.1 `PlanetStrengthChart` — %-of-minimum bar (Performance) or a component-composition stacked bar (Composition toggle), plus a per-graha expandable Bala breakdown. 3.2 `HouseStrengthChart` — Rūpas bar (scale is dynamic — see 2026-09-17 note below, not the fixed 0–9 this spec originally called for), House order/Strength rank toggle, per-house expandable breakdown. 3.3 `AshtakavargaChart` — Varga dropdown (all generated Dn charts; independently persisted SAV/BAV + Piṇḍa for the selected varga) + Sarvāṣṭakavarga bar (House order/Strength rank toggle) + Bhinnāṣṭakavarga grid (7×12) + Piṇḍa table | Bar and table are one component each (not chart+table separately — the bar sits inline in the row) | `vw_ChartShadbala` + `tbl_Fact_PlanetaryStrengthComponent` · `vw_ChartBhavaBala` + `tbl_Fact_BhavaStrengthComponent` (both via `PlanetaryStrengthRepository`/`BhavaStrengthRepository`'s `GetSummaryByBirthDetailId`/`GetComponentsByBirthDetailId`) · `vw_ChartAshtakavarga` + `tbl_Fact_AshtakavargaPinda` (`AshtakavargaRepository`) |
| **4 · Spl Lagnas** | See [`specs_KI_spllagna.md`](specs_KI_spllagna.md) — full detail, including the proposed `SouthIndianGrid_Micro` Grid view, moved there 2026-09-17 | See [`specs_KI_spllagna.md`](specs_KI_spllagna.md) | See [`specs_KI_spllagna.md`](specs_KI_spllagna.md) |
| **5 · Yogas** (2026-10-02) | — (no chart column: the D1 grid and chart picker were dropped from this tab) | **Yoga Summary** card: the present yogas counted Good / Bad / Mixed / Depends (`OutcomeNatureCode`) with a split bar — a tile filters the table to that nature. **Yogas Present**: Set · Yoga · Variant · Good / Bad · (Status under "Show all") · Rule · Inference · Interpretation · Source, every header sortable, default order Set → Lagna/Sun/Moon/Combination type → variant | `vw_ChartYogaEvaluations` (+ `tbl_Rule_Yoga`, migration 159) via `YogaEvaluationRepository` |
| **6 · Vargas** | `AmsabalaTable` (2026-09-16 redesign, superseding the original flat `VargaLordsTable` **and** the standalone "3.4 Amsabala" Strength sub-tab) — Vargottama/Shadvarga/Saptavarga/Dasavarga/Shodasavarga scheme selector. Vargottama: D1 Sign/D9 Sign/Match per graha. Each of the 4 varga-group schemes: collapsed numeric split (GoodCount/GroupSize + %) per graha, expanding to a stacked equal-width bar (one segment per varga in that scheme, full 7-tier dignity colour — Exalted/Moolatrikona/Own/Great Friend/Friend/Enemy/Great Enemy — empty for Neutral/Debilitated) | Expanding a graha's bar also reveals a varga-lords detail table underneath it (Varga/Sign/Lord/Dignity/0–1 score) — the "chart doubles as summary, table is the drill-down" shape | `vw_ChartAmsabala` + `tbl_Rule_AmsabalaGroup`/`tbl_Rule_AmsabalaName` (`AmsabalaRepository`/`AmsabalaSchemeRepository`) + `tbl_Fact_Vargottama` (`VargottamaRepository.GetByBirthDetailId`) + `WorkspaceData.Charts` (Grahas/HouseLords, for the lords table and live dignity) — no new repository |
| **2 · Transit — natal columns & Saturn from the Moon** (2026-10-03) | Wheel shades Saturn's signs from the natal Moon on the transit ring — Sāḍe Sātī (12th, 1st, 2nd) in the weak colour, Kaṇṭaka Śani (4th) and Aṣṭama Śani (8th, hatched) in the moderate colour — outlines the one Saturn occupies on the chosen date, and names that phase under the date (♄ badge); a legend sits under the wheel | Transit positions table gains a **NATAL (D1)** column group per planet: natal sign and degree (R when retrograde), nakṣatra and pāda, house from the Lagna, dignity. `SaturnFromMoonPanel` under the Gochara table: Saturn's phase on the chosen date plus every Sāḍe Sātī / Kaṇṭaka / Aṣṭama window of this life (`SadeSatiTable`), the window covering the date highlighted. All three read `GocharaReading.PhaseOf` (Kaṇṭaka = 4th only, as `tvf_Chart_SadeSatiPeriods`) | `vw_ChartPlanetEvidence` (D1) via `Natal_Transit_Comp_WheelRepository` · `tvf_Chart_SadeSatiPeriods` via `SadeSatiRepository` |
| **2 · Transit — Gochara reading** (2026-10-04) | — | `GocharaInferencePanel` above `GocharaTable`: the slow planets (Saturn first, then Jupiter, Rāhu, Ketu) each get a net tier — Supportive / Mild / Mixed / Obstructed / Unfavourable, PVR verdict × own BAV bindus band (project heuristic, `GocharaInferenceBuilder.TierOf`) — a line built from the row's own facts, and the next sign crossing with its house from the Moon and verdict before Vedha. Saturn phase flag on top; the five fast planets sit behind an expand control. **Natal and dasha qualifiers (2026-10-04):** each slow planet carries a house-promise chip (lord Ṣaḍbala band + independent Bhava Bala band of the natal house crossed), a Dasha-linked chip when it is a running dasha lord, rules the house crossed, or the dasha lord sits in the sign crossed, and a "Natal house and dasha" expander; a headline line names the running dasha (`GocharaQualifierBuilder`, fed from D1 + `_shadbala` + `_bhavaBalaComponents` + the date/dasha selector). Moon-reference caveat at the top. Design: `architecture/transit_gochara_inference.md` | `GocharaReading` + `tbl_PlanetSignTransitEvents` (next crossing sign via `PlanetTransitSnapshot.NextChangeSignId`) |
| **2 · Transit — chart style switch** (2026-10-06) | Switch above the wheel: Polar wheel (default) / Sudarsana chakra (`SudarsanaChakra`): three rings of the 12 signs, Lagna inner, Moon middle, Sun outer, each counting houses from its own reference, every graha in each ring, coloured per theme (--tripod-sun/-moon/-lagna tokens); the chosen mahadasa/antardasa/pratyantar lord is highlighted in the Sun/Moon/Lagna ring (PVR: Sun for mahadasa, Moon for antardasa, Lagna for pratyantardasa). Facts only. | — |
| **6 · Vargas — Vimśopaka Bala** (2026-10-02) | — | `VimsopakaTable` under `AmsabalaTable`, following its scheme pill (Vargottama keeps the last varga scheme shown): ranked bars like Planet Strength — per graha (seven, no nodes) the 20-point score on a 0–20 track with a 10 (all-neutral) reference line, Strong / Moderate / Weak colours and pill from `StrengthBands.VimsopakaScore` (≥15 / ≥10, project heuristic, migration 160), each row opening to its per-varga sign, dignity, factor, weight and points. A scheme a person lacks a varga for shows a note. Computed live (`VimsopakaRead` → `VimsopakaCalculator`), nothing stored | `tbl_Rule_VimsopakaWeight` (migration 157, BPHS) + `tbl_Rule_StrengthBand` via `TransitStrengthRuleRepository` + `WorkspaceData.Charts` |
| **1.3 · Benefic / malefic by school** (2026-10-13) | — | `PlanetBeneficMaleficTable` in the About Planets tab under the dignity table; follows the chart picker (D1 or any varga, houses from that chart's own Lagna). One row per graha: Natural (Moon by paksha, Mercury by company, PVR §3.2.2, with a note where BPHS reads it differently); Functional by school (PVR, BPHS ch.34, Raman) — the schools disagree on what a kendra lordship does; House (final): the occupied house's nature from its lord, the planets in it (conjunctions) and the planets aspecting it, each by functional nature, equal weight (`HouseBeneficMaleficCalculator`, Raman); Sign (dignity, sign lord's nature); Nakṣatra (star lord's nature and house, KP sub-lord). A dash means the school states nothing. No blended verdict. A second grid gives PVR's functional nature for every stored varga. Computed live in Core (`PlanetBeneficMaleficReader`), nothing stored per chart. Research and open items: `docs/research/domain/planet-benefic-malefic-perspectives.md` | `tbl_Dim_BeneficMaleficSchools` + `tbl_Rule_BeneficMaleficSchool` (migration 180) via `BeneficMaleficSchoolRepository`; `LagnaFunctionalNature` (PVR Table 30); `tbl_Chart_KeyDetails` |
| **1.3 · Rudra, Trishoola & Maheswara** (2026-10-06) | — | `RudraMaheswaraTable` in the About Planets tab after Mrityu bhāga & Puṣkara (D1 only): Rudra (the stronger of the 8th lords from the Lagna and the 7th by PVR Table 32, plus the weaker one when it is afflicted), the three Trishoola rasis, Maheswara (8th lord from the Atma Karaka, with the node, own-sign and 6th-house exceptions), then the two longevity methods as categories only: Method of Three Pairs (Lagna lord and 8th lord, Moon and Saturn, Lagna and Hora Lagna, with Table 34's paramaayush) and the Eighth Lord Method. Computed live from D1 key details, nothing stored. PVR §14.3-14.5, pp.182-185; the card carries the book's caution that these are not infallible | `RudraMaheswara` and `LongevityMethods` (Core, `Engines/KeyInfo`) over `StrongerCoLord` and `StrongerRasiComparator`; `tbl_Chart_KeyDetails` (D1 grahas and HL) |
| **1.3 · Śayanādi working** (2026-10-06) | — | `SayanadiWorkingTable` under the Planet states table (About Planets tab, D1 only): the formula `((C × P × A) + M + G + L) mod 12` (PVR §15.4.4, Table 36) with the chart's M, G (whole Janma ghaṭīs + 1) and L, then per graha C, P, A, C×P×A, the total, the mod-12 value and the state, so every input behind the stored state is visible. The Planet states table's Śayanādi column now also shows the final value as `n/12`. Same Core `PostureStateCalculator` the stored state comes from; recomputed from the stored D1 key details and the birth pañchāṅga, nothing new stored. The Cheṣṭā / Dṛṣṭi / Viceṣṭā refinement is not built | `PostureStateCalculator` (Core); `tbl_Chart_KeyDetails` (D1), `tbl_Chart_Panchanga.JanmaGhatis` via `PanchangaRepository`; `tbl_Dim_PlanetaryState` |
| **1.1 · Avasthas column** (2026-10-06) | — | `PlanetPositionsTable` (Planet positions, D1 only) gains one **Avasthas (PVR §15.4)** column, off by default and turned on by the page for D1: per graha the Bālādi state with its effect %, Jāgradādi, every Dīptādi and Lajjitādi state, and Śayanādi with its final value as `n/12`, each with PVR's meaning (`AvasthaPvrNotes`, §15.4.1-15.4.3), the planet's stored PVR §15.4.4 Śayanādi result, and the house results PVR names (Lajjita in the 5th: losses related to progeny; Kshobhita in the 7th: loss of spouse; Kshudhita or Kshobhita: the house's significations are affected). Read from the same stored states as the Planet states table; nothing new stored. The role and condition columns remain in About Planets | `tbl_Fact_PlanetaryState` via `PlanetaryStateRepository`, `tbl_Dim_PlanetaryState`, `tbl_Rule_PostureStateInterpretation`; `AvasthaPvrNotes` (Core) |
| **1.2 · Stronger co-lord** (2026-10-06) | — | `HouseLordshipTable` (House lord placement, About Houses) shows, under the Lord for the Scorpio and Aquarius houses, which co-lord rules in this chart (Mars or Ketu, Saturn or Rahu) and the rule that decided it, e.g. "joined by more planets (2 against 1)". The Lord column itself stays the stored single lord; the note is the PVR §15.5.1 reading the arudha pada, Rudra and longevity engines already use. Computed live from the chart being shown (`StrongerCoLord.Explain`, Core); nothing stored | `StrongerCoLord` (Core, `Engines/Karakas`) over `LoadedChart.ToAnalysisInput()` |
| **1.2 · Stronger rasi & Narayana dasa seed** (2026-10-06) | — | `StrongerRasiTable` closes the About Houses tab, for the chart shown: the Narayana dasa seed (the stronger of the Lagna and the 7th, with the PVR §15.5.2 rule that decided it) and the twelve dasa rasis in order (normal, Saturn or Ketu progression, forward or backward); the stronger rasi on each of the six rasi-and-7th axes with its rule, the Lagna axis marked; and the stronger of each of Mars's, Mercury's, Jupiter's, Venus's and Saturn's two rasis (the rasi used for the graha ārūḍha). Dasa lengths are not computed. Nothing stored | `StrongerRasiComparator.Explain` (Core, `Engines/Karakas`), `NarayanaProgression` (Core, `Engines/Dasha`, checked against all 36 sequences of PVR Table 40 and Examples 63-65) over `LoadedChart.ToAnalysisInput()` |
| **9 · Phalita dasas** (2026-10-06) | — | `PhalitaDasaPanel`, the PHALITA DASAS master step (`?step=9` or `phalita`), five tabs over D1: **Narayana**, **Lagna Kendradi Rasi**, **Sudasa**, **Drigdasa** (`RasiDasa`) and **Ashtottari**. Each rasi dasa shows where it starts and why (the stronger of the Lagna and 7th with the PVR §15.5.2 rule that decided, Sree Lagna for Sudasa, the 9th for Drigdasa), the order of the twelve rasis, the running dasa on an as-of date with how its length was counted, and the two cycles of dated periods; Narayana adds the running dasa's twelve antardasas. Under each, **what PVR reads in the running dasa** (`PhalitaDasaReader`): the book's own interpretation rules for that dasa (Narayana: the dasa lagna and paaka rasi principles and the A10, upapada and GL references; Sudasa: HL, GL and AL; Lagna Kendradi: AK, GL, Lagna lord; Drigdasa: AL, Lagna and 7th), each marked as holding or not with its stated result and what was found, plus the rules PVR gives that are not yet tested. Ashtottari shows PVR's three views on when it applies (all charts; Rahu not in the Lagna and in a quadrant or trine from the Lagna lord; day birth in Krishna paksha or night birth in Shukla), the running Maha and Antardasa, and the periods. Facts only, no score or verdict. Computed live from D1 key details, special lagnas and the pañchāṅga; nothing stored | `RasiDasa`, `PhalitaDasaReader`, `NarayanaProgression`, `AshtottariApplicability`, `AshtottariDashaCalculator` (Core, `Engines/Dasha`); `StrongerRasiComparator`, `StrongerCoLord`; `tbl_Chart_KeyDetails` (D1, SL, HL, GL, AL, A10, A12), `tbl_Chart_Panchanga` |
| **10 · Dasha matters** (2026-10-06) | — | `DashaMattersTable`, the DASHA MATTERS master step (`?step=10`, `dashamatters` or `dasas`), from Core `DashaMatters` (PVR §16.5.1, p.214). One row per PVR example (5th lord in D7; 8th lord in D1; planet exalted in GL in D10; exalted planet 12th from AK in D9; 7th lord in D9; planet with Rahu in the 9th in D4; exalted planet aspecting HL from the 11th from AL; planet joined by Moon and Saturn in the 8th in D30; well-disposed planet aspecting A3 in D10): the planets meeting it, the placement, and their Vimshottari mahadasas. Facts only. Charts not stored are named ("Needs the D7 chart"). Open readings (exalted by sign and degree, AK's D9 sign, rasi drishti, well-disposed = exalted/own/Moolatrikona/friend) are stated in the row. |
| **7 · All Charts** | — | Every stored divisional chart as a plain South-Indian grid (unchanged from before this round) | separate `/charts/{id}` route (`AllCharts.razor`), heading reads "7. ALL CHARTS" |
| **8 · D1-Promise** (2026-10-04) | — | `D1PromisePanel`: every life matter's natal promise read from the Lagna in D1, one row per Lagna house of the matter, grouped by life-matter category with a verdict filter. Each row: verdict (Strong positive / Positive, conditional / Mixed / Weak, limited / Adverse / Indeterminate), confidence, the lord / karaka / other-influence directions (L K I with + − ± ·) and the old strength % as a technical index; opening a row lists the testimonies by role with direction and capacity, contradictions, missing evidence and refinements. Read on first open (`LifeMatterPromiseService.ReadAll`). Matters read from a special point or another lagna are listed as not read. To be folded into Key Inference later; opens at `?step=promise` | `D1PromiseEngine` (Core) via `LifeMatterPromiseAdapter` / `LifeMatterPromiseService` (Data); `docs/architecture/key_inference_promise.md` |

## Information ownership and deduplication

Each fact has **one full-detail owner** in Astro Facts. Other tabs may show only the minimum
context needed to identify or explain their own result; they must not reproduce the owner's full
columns or recompute the fact independently.

| Information | Canonical owner | Context allowed elsewhere |
|---|---|---|
| D1 placement, longitude, nakṣatra/pāda, motion, house | 1.1 D1 Birth Chart | Planet/sign/house identifiers needed to label another result |
| House lordship, occupants, aspects, conjunctions, findings, argala | 2.1 About Houses | A compact house/lord label in strength or yoga evidence |
| Planetary functional role, owned houses, māraka/bādhaka/dusthāna/triṣaḍāya, Kendrādhipati doṣa, dignity, kāraka | 2.2 About Planets | A compact dignity/role cue only when it directly explains another score or finding |
| Rāśi–nakṣatra combined interpretation | 2.3 Sign & Nakshatras | Sign/nakṣatra as row identity only |
| Quantified planet/house strength | 3 Strength | Summary score in downstream evidence; component detail stays here |
| Special reference lagnas | 4 Spl Lagnas | Reference label in a rule finding |
| Yoga rule, source, result, interpretation | 5 Yogas | Yoga name/status summary only |
| Cross-varga comparison and confirmation | 6 Vargas | Varga name/sign needed to support a finding |

Implementation consequence: `PlanetPositionsTable` exposes `ShowAnalysis`; Astro Facts 1.1
sets it false and also hides dignity. `PlanetDignityTable` is the sole full-detail planetary-role
surface and calls `LagnaFunctionalNature` once per classical planet. The old 84-row database
mirror must not return; if persisted UI provenance is later required, persist computed chart facts
from Core rather than duplicating the rule in SQL.
## New fields — sourcing status

| Field | Where | Status |
|---|---|---|
| **Exaltation point** | 2.2 | Classical Uchcha Bindu (Sun 10° Ari · Moon 3° Tau · Mars 28° Cap · Mercury 15° Vir · Jupiter 5° Can · Venus 27° Pis · Saturn 20° Lib). Rahu/Ketu excluded — no classical exaltation point. **Already DB-backed** — `tbl_Rule_GrahaDignity` (EXALTED rows, PVR-cited) and `tbl_SignAttributes.ExaltedDegree`; `ChartViewModel.BuildExaltationRows` reads `AstroMath.DeepExaltationPoints`, the shared copy CLI `verify-dignity` checks against both. No separate `tbl_Rule_Exaltation` — it would duplicate them. |
| **Δ from exaltation / Closeness %** | 2.2 | Derived: `Δ = min(|natal − exalt|, 360 − |natal − exalt|)` in absolute zodiacal degrees; `closeness% = round((1 − Δ/180) × 100)` (100% = exact exaltation, 0% = exact debilitation point). Computed from `tbl_Chart_KeyDetails.NirayanaLongitudeDegrees` + the exaltation rule table above — no new fact table needed, a read-time calculation. |
| **Yoga Type** (Sun / Moon / Lagna / combination — which reference point the yoga is judged from) | 6 | **DB-backed (`db/079`).** `tbl_Rule_Yoga.FormationFamilyCode`, exposed as `vw_ChartYogaEvaluations.YogaTypeCode`; constrained to `SUN`/`MOON`/`LAGNA`/`COMBINATION`. Seeded for 146 of 223 `YogaCode`s from the evaluator predicate — the rest read `NULL` (no predicate exists to derive from; see [`yoga.md`](yoga.md#2-yogas-round-2-columns--source-is-column-1-variant-dropped)). |
| **Yoga Rule** (one-line classical rule, e.g. *"7th lord in 5th"*) | 6 | **DB-backed (`db/079`).** New `tbl_Rule_Yoga.ShortFormationRule` column, exposed as `vw_ChartYogaEvaluations.YogaRule`; short-form, hand-transcribed per `YogaCode` from the actual predicate (not `CalculationNarrative`, which stays prose-length and unused here). Same 146/223 coverage as Type. |
| **Yoga Variant** | 6 | **Dropped from the table** (was `SourceVariantCode`) — stays in the underlying data, just not a visible column. `Source` (`SourceRefCode`) is column 1. |

## Also computed, now surfaced (was hidden pending this redesign)

Carried over from the round-1 review's "also computed" panel — each now has a home in the
flow above instead of sitting in a side panel:

| Data | New home | Source |
|---|---|---|
| Arudha padas (A1…A12, AL) | 2.1 supporting card | `tbl_Chart_KeyDetails · PointKind='Arudha'` |
| Upagrahas (Gulika, Māndi, +9) | 2.1 supporting card | `tbl_Chart_KeyDetails · PointKind='Upagraha'` |
| Special Lagnas (Hora Lagna, …) | 2.1 supporting card | `tbl_Chart_KeyDetails · PointKind='SpecialLagna'` |
| Per-planet nakṣatra + pāda | 1 · D1 position table | `tbl_Chart_KeyDetails.Nakshatra` / `NakshatraPada` |
| Directional aspects (graha dṛṣṭi) | 2.1, per house | `tbl_Chart_Aspects` |
| Conjunction groups (≥ 2 grahas) | 2.1, per house | `tbl_Chart_MultiGrahaConjunction(+Member)` |
| Planetary avasthās (Bālādi, Jāgradādi, …) | About Planets — `PlanetaryStateTable` | `tbl_Fact_PlanetaryState` |

## Open questions

- ~~Does step 1 replace the dedicated `/transit-wheel/{id}` landing, or stay a duplicate light
  view~~ **Decided (2026-09-14):** the standalone `/transit-wheel/{id}` page is gone. Its wheel
  chart, date/dasha selector, and Gochara table now render inside this page's own "Current
  Transit" tab (`Natal_Transit_Comp_WheelRepository`/`TransitSelection`/
  `Natal_Transit_Comp_WheelMath` reused as-is); the "D1 Birth" tab keeps the plain South-Indian
  grid + position table. `MainLayout`'s standalone TRANSIT nav tab was removed to match — Home's
  "open person" / post-generate navigation now lands on `/astro-facts/{id}`.
- ~~`tbl_Rule_Exaltation`~~ **Dropped (2026-10-01):** the exaltation points were already in the
  database (see the sourcing-status table). The `tbl_Rule_Yoga` Type/Rule fields landed in
  `db/079`.
- **Decided (2026-09-14):** 2.1 About Houses built as three tables, not the mockup's one merged
  "house lords & occupancy" table. Top row, side by side: `AspectsTable` (left, aspects grouped
  by the house they land in) and `HouseConjunctionsTable` (right, multi-graha groups per house —
  new, distinct from the pair-based `ConjunctionsTable` VargaView already uses). Below that,
  "House Lord Placement" (`HouseLordshipTable` + its new optional `Occupants` column) and "House
  Lord Key Findings" (new `HouseLordFindingsTable`, one row per `BranchCode`) reading
  `vw_ChartHouseLordInterpretation` (db/094) — the classical claims-per-house-lord-placement view
  that had no UI consumer yet. The Arudha/Upagraha/Special-Lagna supporting cards and the
  occupancy bar chart from the mockup are not built.
- **Built (2026-09-19) — 2.1 gains a 4th table, "Argala & Virodhargala"** (new `ArgalaTable`,
  `src/Ikiastrro.Web/Components/Charts/`): one row per house, the same 8-position shape as the
  book's own Exercise 16 worked table (2nd/4th/11th/5th argala, 12th/10th/3rd/9th virodhargala —
  see `argala-virodhargala-drishti-lifematters.md` §4), plus a Net column (`ArgalaCalculator
  .Compare`'s count-then-dignity-sum verdict) — **column order House · Sign · Net · (argala ·
  virodhargala offsets), Net moved up from last to 3rd 2026-09-22 (rammyps's correction)**.
  **Reads `tbl_Fact_Argala` (2026-10-01)** through `ArgalaFacts.ForChart` — the same rows Life
  Matters reads, so the two pages cannot disagree. `ChartGenerationService` persists D1's rows;
  a varga (or a D1 not regenerated since migration 128) gets the same `ArgalaFactBuilder` output
  computed live. The Net column's count comparison uses `ArgalaCalculator.Compare`'s flat-list
  overload over those rows.
  New `ArgalaRuleRepository.GetArgalaSignificanceNotes` (`tbl_Rule_Argala.SignificanceNote`,
  migration 129) feeds the 4 argala column headers' hover tooltips; the 4 virodhargala columns
  have none (PVR gives no parallel gloss for them in sec.10.7). The 3rd-from column shows a small
  "→arg" badge when the sec.10.6 "2+ malefics in 3rd" exception fires for that house; a row whose
  target sign holds Ketu gets a `*` (anti-zodiacal counting, sec.10.6 note).
- **Built (2026-09-14):** 2.2 About Planets — Moon-context fact chips (new
  `ChartMoonContextRepository` over `vw_ChartMoonContext`, loaded outside `WorkspaceData` like
  the Current Transit tab's Gochara/Dasha, since only this page needs it) above the "Planets —
  dignity, kāraka, exaltation" table (new `PlanetDignityTable`), reading one new
  `ChartViewModel.BuildExaltationRows` (Core) over `lc.KeyDetails` — the classical Uchcha Bindu
  constants from the sourcing-status table above, read (since 2026-10-01) from the shared
  `AstroMath.DeepExaltationPoints`. Rahu/Ketu appear in the
  table with "—" in the three exaltation-derived columns.
- **Dropped (2026-09-14, rammyps):** the closeness-to-exaltation bar chart (`ExaltationClosenessChart`)
  built alongside the table above was removed from the page the same day — 2.2 is table-only
  now, joining 2.1 as an exception to this page's "one chart + one table" design rule. The
  component, its golden snapshot, and its `ChartFixture.ExaltationD1` fixture were deleted
  outright (nothing else referenced them); `ChartViewModel.BuildExaltationRows`/`ExaltationRow`
  stayed — the table still reads them.
- **Built (2026-09-14) — three-level tab restructure, rammyps's directive:** a new outer
  "master" `MudTabs` (`Position="Position.Left"`) holds one panel per step — "D1-TRANSIT" (step
  1) and "2. ABOUT" (step 2) — as a vertical rail on the page's left edge; each panel opens with
  its own `ki-stephead` (Step N of 6 pill + heading) to its right, then that step's own inner
  horizontal `MudTabs` (D1 Birth/Current Transit, or 2.1/2.2) and all its content. Every tab at
  every level now follows the new app-wide fill convention — see `design-language.md` "Tabs" —
  instead of MudBlazor's default text-plus-underline look. `ChartViewModel`'s classical
  exaltation constants also back-filled a real bug found while building 2.2:
  `ShadbalaCalculator.DeepExaltation`'s Venus entry was 327° (Aquarius 27°) instead of the
  classical 357° (Pisces 27°) every other reference in the codebase uses — fixed; a 30° error
  isolated to Venus's Uccha/Ishta/Kashta Bala, now visible in 3.1's per-planet breakdown.
- **Built (2026-09-14) — Step 3 "Strength," rammyps's directive to match the round-2 mockup
  PNGs closely (not the flatter original spec above):** `PlanetStrengthChart` (3.1) and
  `HouseStrengthChart` (3.2), new `Components/Charts/*.razor` + own `.razor.css` (no golden
  snapshot — table-shaped like PlanetDignityTable et al., not a standalone SVG chart module).
  Both read new typed repository methods — `PlanetaryStrengthRepository`/
  `BhavaStrengthRepository`'s `GetSummaryByBirthDetailId`/`GetComponentsByBirthDetailId` — over
  `vw_ChartShadbala`/`vw_ChartBhavaBala` plus the two `tbl_Fact_*Component` tables (previously
  only reachable through `AstrologerEvidenceRepository`'s generic dynamic-row query). Status
  thresholds (Planet: ≥100%/80–99%/&lt;80% Strong/Moderate/Weak; House: ≥7/5–6.99/&lt;5 Rūpas)
  are now `StrengthBands` (Core), mirrored in `tbl_Rule_StrengthBand` (migration 154, checked by
  `verify-strength`) — see "2026-10-01 — strength cut-offs" below. The `--bala-*`
  component-category palette stays presentation-only. 3.1 ranks strongest-first
  (PercentOfMinimum desc, not the mockup's unexplained order) and offers a Performance/
  Composition bar toggle (Composition reuses the per-row expand's stacked-Bala-share bar,
  clamped so a negative Dṛk/other share never draws past 100% of the track — a real bug hit
  building this, see `PlanetStrengthChart.SharePercent`'s comment). 3.2's Rank column always
  reflects Bhava Bala strength even when "House order" is toggled — only display order changes.
  Two rendering pitfalls worth remembering for the next chart-in-a-table component: Razor's
  "email address" heuristic silently drops `@expr` when glued directly to a preceding word
  character with a `.` later in the same token (`H@row.HouseNumber` rendered as literal text —
  fixed with `H@(row.HouseNumber)`); and a `<span>` bar/track needs an explicit `display:block`
  (or a flex/grid parent, which auto-blockifies it) or its `width`/`height` are silently ignored
  as an inline element (`HouseStrengthChart`'s `.hsc-track` hit this, `PlanetStrengthChart`'s
  `.psc-track` was saved by its `.psc-trackrow{display:flex}` parent).
- **Built (2026-09-16/17) — steps 3.4 Amsabala, 5 Yogas, 6 Vargas added; step 4 renamed Karakas
  → Spl Lagnas; per-panel headings dropped app-wide; chart-control convention introduced:**
  - **3.3 Asthavarga bug:** the same Razor "email address" heuristic noted above hit
    `AshtakavargaChart`'s `<small>H@HouseNumber(...)</small>` too (`H` immediately before `@`
    with no separator) — house numbers rendered as literal unevaluated text
    (`H@HouseNumber(row.SignNumber)`, clipped by the row's fixed width to just "H@House").
    Fixed the same way, `H@(HouseNumber(...))`.
  - **3.4 Amsabala** (new): scheme-tabbed Vargottama/Shadvarga/Saptavarga/Dasavarga/
    Shodasavarga. Vargottama shows D1 sign/D9 sign/Match per graha
    (`VargottamaRepository.GetByBirthDetailId`, new — the repository only had a write path
    before). The 4 varga-group schemes render a subtext ("N charts compared (Rasi chart with
    D-x,D-y,…)") and a stacked equal-width bar per graha, built from `tbl_Rule_AmsabalaGroup`'s
    actual seeded membership rather than a hardcoded copy of it — one segment per varga in that
    scheme; a segment is coloured/named only when that specific varga is Exalted or
    Own/Moolatrikona/Great Friend for that graha (`ChartViewModel.DignityToken`, read live off
    `WorkspaceData.Charts`, not from `AmsabalaRow.Detail`'s `*`-marked good/not-good boolean —
    the full dignity tier needed distinguishing "very good" from "fine"), non-matching vargas
    render as an unlabelled empty slot so the row still reads as "N out of GroupSize".
  - **5 Yogas** (new, `YogaEvaluationTable`): supersedes the coverage-donut + full-variant-list
    plan in [`yoga.md`](yoga.md) — see that file for the as-built shape (a single "Yogas
    Present" table, deduplicated and sorted by type).
  - **6 Vargas** (new, `VargaLordsTable`): needed no new repository — `WorkspaceData.Charts`
    already carries every generated chart type's `Grahas` (sign) and `HouseLords` (sign → lord),
    the same data `AllCharts.razor`'s grid already reads.
  - **4 Spl Lagnas:** renamed from "4. Karakas" — the embedded `KarakaPolarWheel` already titled
    itself "Special Lagnas & Karakas". Its own heading was then dropped too (2026-09-17, single
    tab under this step) along with its Chart-selector `<label>`/"CHART" caption; the Chart
    `<select>`, and the Wheel/Grid toggle (renamed from Polar-Wheel/SouthIND-Grid) now centre
    together where the heading used to sit. Planets were missing from the polar wheel entirely —
    `PolarGridLagnaSelect` received `PlanetPlacements` and even converted it to `PlanetsBySign`
    for the `south` grid view, but the `polar` SVG branch never referenced it; fixed by drawing
    each sector's planet glyphs (`ChartViewModel.PlanetGlyph`, dignity-coloured) at radius 260,
    between the sign ring (292) and the special-Lagna house-count ring (224). Hit the same
    Razor gotcha this file already had a workaround for once: a literal SVG `<text>` as the
    *first* tag inside an `@if(){ }` block (right after a `var` statement) is parsed as Razor's
    own reserved `<text>` markup-transition tag, which can't carry attributes
    (`RZ1023: "<text>" and "</text>" tags cannot contain attributes`) — wrap it in a `<g>` first,
    same fix the existing `pgls-point` block already used.
  - **Per-panel headings dropped everywhere** (D1-Transit/About/Strength's own `<h1>`, and each
    chart component's card `<h2>`+subtitle where it duplicated the tab pill above it) — the tab
    pill already names the step, so every panel now opens straight into its content. Where a
    panel lost its only spacing (the old heading's margin), `.ki-panelbody > .ki-tabs:first-child`
    carries a `margin-top` instead. Ashtakavarga/Amsabala kept their per-card `<h2>`s (they
    distinguish sub-sections a single step-level tab can't) but uppercased them and dropped
    their subtitle `<p>`s as redundant.
  - **Chart-control convention** (see `design-language.md` "Chart controls") replaced every
    in-chart toggle's ad hoc styling (`PlanetStrengthChart`/`HouseStrengthChart`'s toggle used
    to be a one-off midnight-bg/gold-text pair, not the app's tab tokens) with one dark-navy
    segmented-pill treatment, right-aligned and smaller than the card heading, reused identically
    by `AshtakavargaChart`, `PlanetStrengthChart`, `HouseStrengthChart`, `AmsabalaTable`'s scheme
    selector, and `KarakaPolarWheel`'s Wheel/Grid toggle + Chart `<select>`.
  - **House Strength Rūpas scale bug:** the bar/axis/column-header hardcoded a 0–9 max
    (`BarWidthPercent(...) / 9m`), but Bhavadhipati Bala alone is the house lord's whole
    Shadbala, which routinely clears 9 Rūpas for a strong lord — observed up to ~10.9 in the
    live database, silently clipped to 100% width before this fix. `ScaleMax` now rounds the
    actual max among the displayed houses up to a whole Rūpa (floor 1), and the axis/column
    header follow it instead of a fixed constant.
  - **Saved Charts hero image** moved from `saturn-rings-brand.png` to
    `ganesha-9planet-brand.png` — see [`saved-people.md`](saved-people.md) for the accompanying
    layout fix (the old full-bleed-left/flush-right hero had an asymmetric gap and cropped the
    art with `object-fit:cover`; now a normal symmetric grid gutter with `object-fit:contain`).
- **Built (2026-09-16 evening) — step 6 Vargas redesign, retiring `VargaLordsTable` and merging
  3.4 Amsabala into it (rammyps's directive):** the flat all-D-chart sign+lord table was
  reported as visually "mismatched" — on inspection its per-cell "same sign as D1" highlighting
  was actually correct, but the architecture wasn't what rammyps wanted: two different depths of
  the same Vargottama/Shadvarga/Saptavarga/Dasavarga/Shodasavarga breakdown living in two
  different steps (3.4 and 6). Consolidated into one scheme-tabbed view at step 6, `AmsabalaTable`
  relocated from 3.4 (that Strength sub-tab is now gone; a `"3.4"`/`"amsabala"` deep link routes
  to step 6 instead). Three additions beyond the relocation:
  1. **7-tier dignity coloring** (was binary good/not-good): `ChartViewModel.DignityTierToken`
     (new, `src/Ikiastrro.Core/Presentation/ChartViewModel.cs`) maps the 9-tier `DignityStatus`
     to 7 named tiers — Exalted/Moolatrikona/Own/Great Friend/Friend/Enemy/Great Enemy — added
     deliberately as a *new* function, not a change to the existing `DignityToken` (which every
     other dot-coloured view in the app — the D1 grid, polar wheel, etc. — still depends on for
     its 7-CSS-token collapse). Neutral/Debilitated aren't in rammyps's 7-tier list and still
     render as an unlabelled empty slot, same convention the bar already used. 3 new
     `--dignity-moolatrikona`/`--dignity-own`/`--dignity-great-friend` tokens (`tokens.css`)
     split back out of the old single `--dignity-good` — placeholder hex values, to be tuned
     live via claude-in-chrome against the running app.
  2. **Expand-to-detail interaction**: each graha row now collapses to just its numeric split
     ("N/GroupSize · P%") by default; clicking it expands to reveal the stacked bar and a new
     varga-lords detail table underneath (Varga/Sign/Lord/Dignity/Score) — the table reuses the
     old `VargaLordsTable.Cell()`'s exact per-chart sign+lord lookup, scoped to just the active
     scheme's chart-type membership instead of every generated D-chart.
  3. **0–1 normalized dignity score**: `ChartViewModel.DignityScoreNormalized` (new) —
     `(DignityScore + 4) / 8.0` over the existing -4..+4 scale — shown in the new detail table,
     for later statistical use (see `docs/research/domain/stat_strength.md`, the broader
     Shadbala/Amsabala/Bhavabala/Ashtakavarga normalization framework this is phase 0 of).

## 2026-09-23 — Natal/Transit navigation revision

- Master item 1 is **Natal Charts**, with a persisted-chart dropdown defaulting to D1.
- The default detail tab is 1.1 Overview (the restored key table), followed by 1.2 About Signs & Nakshatras, 1.3 About Houses, 1.4 About Planets, and 1.5 Relationships.
- Relationships includes graha drishti, rasi drishti, rasi/graha dispositor chains, and conjunctions for the selected chart.
- Master item 2 is **Transit Chart**. Its centered full-width wheel remains unchanged and has no collapse interaction.

## 2026-09-24 — master tab rail, chart toolbar, 7th step, nakshatra abbreviations

(Supersedes the 2026-09-23 "Collapse/Expand control was removed" line above — rammyps asked for
it back three revisions later; it turned out to add value after all.)

- **Master tab rail now really is evenly spread** across the 7 steps. The prior grid-stretch CSS
  targeted `.mud-tabs-toolbar-wrapper`/`-toolbar-inner`, which this MudBlazor version never
  renders (confirmed live via devtools), so it was inert and MudTabs' natural left-packed flex
  layout is what actually shipped. Corrected to the real DOM
  (`.mud-tabs-tabbar-wrapper` > each tab's `.mud-tooltip-root.mud-tooltip-inline` wrapper) — this
  does NOT trip MudTabs' overflow/scroll-arrow JS into "arrows + 2 tabs" mode, which the earlier
  investigation (2026-09-14) had worried it would.
- **7. ALL CHARTS is now its own master tab**, not just the separate `/charts/{id}` page. Both
  render the same `AllChartsGrid` component (`Components/Charts/**`) off the same already-loaded
  `WorkspaceData` — no extra query for the embedded tab. The standalone `/charts/{id}` route
  still exists (linked from the top app-bar's "ALL CHARTS") and still works for a direct
  person-scoped **Print** — see that section's own note.
- **Natal Chart picker moved.** It now sits directly above the D1 grid inside `.ki-gridwrap`
  (left-aligned with the chart, not spanning the full two-column layout), and dropped its visible
  "NATAL CHART" label — the label was redundant once the control sat right on top of what it
  controls (an `aria-label` covers it for screen readers). Still the documented dark-navy/cream
  "chart control" look (design-language.md "Chart controls").
- **Collapse/Expand Chart is back**, next to the picker in the same toolbar — same
  `_natalChartCollapsed` mechanism as before its 2026-09-23 removal.
- **Nakshatra column in 1.1 Overview now abbreviates** (`ChartViewModel.NakshatraShort`, 4
  letters, full name on the cell's `title` tooltip) instead of the full name. Full names up to
  "Purva Bhadrapada" (16 chars) were forcing either a horizontal scrollbar or a smaller font than
  the rest of the page in this 10-graha-column table — the fixed small font this table already
  uses (2026-09-23) stays consistent across every row now.

## 2026-09-23 (later) — sub-tab consolidation, moon context moved to chart view, tab palette flattened

Supersedes the "2026-09-23 — Natal/Transit navigation revision" section's sub-tab list above.

- **Master item 2 renamed "2. TRANSIT CHART" → "2. TRANSIT CHARTS"** — plural, matching "1.
  NATAL CHARTS" (was inconsistently singular).
- **Chart toolbar reordered**: EXPAND/COLLAPSE CHART now sits to the *left* of the chart-type
  dropdown (was right); the dropdown itself shrank (`max-width: 130px`, smaller font/padding —
  was `flex: 1`, stretching to fill the toolbar).
- **Natal Charts' 5 sub-tabs collapsed to 4:**
  - **1.1 GENERAL DETAILS** (renamed from "1.1 Overview") — now combines the old 1.1 Overview
    (`PlanetPositionsD1Transposed`) and 1.2 About Signs & Nakshatras (`RasiNakshatraTable`)
    tables, one after the other in a single tab, instead of two separate tabs.
  - **1.2 ABOUT PLANETS** (moved up from 1.4) — the Moon-context fact chips (Tithi/Pakṣa/
    Elongation/Birth/Moon nakṣatra + `LunarPhaseCard`) that used to open this tab were pulled
    out entirely. It now contains `PlanetDignityTable` under "Planets — dignity, kāraka,
    exaltation" followed by `PlanetaryStateTable`: Bālādi capacity + effect fraction,
    Jāgradādi availability, and Śayanādi activity for all nine grahas. Every state shows its
    Dim meaning; Śayanādi additionally resolves the source-grounded planet × state reading
    from `tbl_Rule_PostureStateInterpretation` (108 rows), keeping conditional clauses and
    `SRC_PVR_INTEGRATED` §15.4.4 provenance visible. The table explicitly treats the three
    systems as independent lenses, not a composite score or deterministic prediction.
    The Moon-context block moved to the **chart view** itself — a new
    `.ki-chart-moonfacts` block inside `.ki-gridwrap`, directly under the South-Indian grid —
    so it's visible under the chart no matter which sub-tab is open, instead of being one click
    away inside a single tab.
  - **1.3 ABOUT HOUSES** — unchanged content (House Lord Placement / House Lord Key Findings /
    Argala & Virodhargala), same tab index (2) it already had.
  - **1.4 ASPECTED (%)** (renamed from "1.5 Relationships") — unchanged content (Graha Dṛṣṭi
    strength, Rāśi Dṛṣṭi, Rāśi & graha dispositors, Conjunctions).
  - `AstroFacts.razor`'s `Step` query-param routing updated to match: `overview`/`general`/
    `generaldetails`/`signs`/`nakshatras` all resolve to the new combined tab 0; `planets` → 1;
    `houses` → 2 (unchanged); `relationships`/`aspected` → 3.
  - `?chart=` (2026-10-01, any case, e.g. `?step=houses&chart=D9`) opens the chart picker on
    that chart when the person has it — the target of Key Inference' *… in Astro Facts* links.
- **Every nested sub-tab strip (1.1–1.4, and 3.1–3.3) now wraps its label onto two lines**
  instead of growing the pill wide — `.ki-subtabs ::deep .mud-tab` caps `max-width: 130px`,
  allows `white-space: normal`, and drops to `0.72×` the base control font size.
- **Tab palette flattened app-wide**: `--tab-active-bg`/`--tab-inactive-bg` both now resolve to
  `--brand-midnight` (dark navy) and `--tab-active-fg`/`--tab-inactive-fg` both to
  `--brand-sunset` (orange) — one flat look for every tab, active or not, replacing the
  sunset-fill-vs-cream-fill distinction from 2026-09-14. See `design-language.md` "Tabs".
- **Saved Charts (`/charts`) gained a per-row Print action**, `PrintIconButton`, to the right of
  the existing Delete icon button in each person's row-actions cell. It opens that person's
  `/charts/{id}?print=1` in a new tab; `AllCharts.razor` auto-fires `window.print()` once loaded
  when `print=1` is present, keeping printing scoped to one person (same reasoning as
  `AllChartsGrid`'s own Print button — see that component's comment).

## 2026-09-23 (evening) — further iteration: more two-line labels, tab palette reverted, Moon facts dropped, planet table reverted

Rapid follow-up round after the section above, done interactively (rammyps reviewing each step
live against a running instance before moving to the next):

- **Master rail relabeled and two-lined further**: "3. STRENGTH" → "3. ALL STRENGTH", "5. YOGAS"
  → "5. ALL YOGAS", "6. VARGAS" → "6. VARGA CHARTS" (all wrap via inline padding narrowing the
  text column within each step's already-even grid width). "4. SPL LAGNAS", "5. ALL YOGAS" and
  "7. ALL CHARTS" are short enough to otherwise fit on one line at that width, so their `Text` in
  `AstroFacts.razor` carries an explicit `"\n"` at the word break rammyps wanted ("4. SPL" /
  "LAGNAS", "5. ALL" / "YOGAS", "7. ALL" / "CHARTS") — `.ki-mastertabs ::deep .ki-master-button`
  switched from `white-space: normal` to `white-space: pre-line` so that literal newline renders
  as a forced break instead of collapsing to a space (labels with no `"\n"` still wrap naturally
  when they don't fit).
- **Sub-tabs 1.2/1.3/1.4 got the same explicit-break treatment**: `Text` is now `"1.2 ABOUT\n
  PLANETS"`, `"1.3 ABOUT\nHOUSES"`, `"1.4 ASPECTED\n(%)"` — `.ki-subtabs ::deep .mud-tab` also
  moved to `white-space: pre-line` for the same reason.
- **Moon-context fact chips dropped entirely** (the `<dl class="ki-facts">` that briefly lived
  under `LunarPhaseCard` in the chart view per the section above) — rammyps's call: they were
  pure duplication, not new information. Tithi/Pakṣa/Elongation already read off
  `LunarPhaseCard` itself (its kicker, phase heading, and own Elongation/Illumination/Pakṣa Bala
  `<dl>`); Moon nakṣatra already has a full-detail home in 1.1 General Details' Sign & Nakshatra
  Combination table. Only `<LunarPhaseCard>` remains in `.ki-chart-moonfacts` now. The now-unused
  `.ki-facts`/`.ki-fact`/`.ki-moon-support` CSS and the `moonGraha`/`NakPadaLordByPlanet`-adjacent
  Moon nakṣatra lookup were removed with it.
- **1.1 General Details' planet-positions table reverted from `PlanetPositionsD1Transposed` back
  to `PlanetPositionsTable`** (`Variant="d1"`, `ShowAnalysis="false"`) — rammyps's call, the
  transposed planets-as-columns layout (added 2026-09-23 earlier the same day) was harder to
  read than the plain planets-as-rows table it replaced. `PlanetPositionsD1Transposed` (with its
  Nak Pada Lord column, read off the person's own D9 chart) still exists as a component but is
  now unused; `PlanetPositionsTable`'s only other caller remains `VargaView` (`Variant="varga"`).
- **Tab palette reverted**: `--tab-active-bg`/`--tab-inactive-bg` back to `--brand-sunset`
  (orange) and `--tab-active-fg`/`--tab-inactive-fg` back to `--brand-midnight` (dark navy) — the
  midnight-fill/sunset-text flip from earlier the same day didn't stick; still one flat look for
  every tab level in the app.

- **Built (2026-09-25) — natal chart kept visible on Strength/Yogas/Vargas; tab active-state
  color bug fixed; header nav palette flipped; rank/score badge convention introduced:**
  - **Chart column extended past NATAL CHARTS**: the toolbar (collapse/expand + chart picker) +
    `SindUniDtlGrid` that NATAL CHARTS already showed on the left is now also shown on
    STRENGTH, YOGAS and VARGAS (rammyps's call — those three read the chart while it's on
    screen too). Extracted into a shared `ChartColumn` `RenderFragment` in `AstroFacts.razor`
    (same `_natalChartCollapsed`/`_natalChart` state as NATAL CHARTS, so the collapse toggle and
    chart-picker selection are shared, not reset per tab) and wrapped each of the three panels'
    existing content in the same `.ki-natal-layout`/`.ki-gridwrap`/`.ki-natal-detail` grid NATAL
    CHARTS already used. NATAL CHARTS' own inline copy was left untouched rather than refactored
    onto the new fragment, to avoid touching already-shipped, working markup for its own sake.
    TRANSIT CHARTS, SPL LAGNAS and ALL CHARTS were deliberately left alone (SPL LAGNAS already
    has its own dedicated chart view; ALL CHARTS *is* a chart grid).
  - **Real bug found and fixed: no tab anywhere on this page ever showed an active-state color.**
    `.ki-steptab[aria-selected="true"]` never matched anything — Blazor's bool attribute binding
    (`aria-selected="@(_activeTab == X)"`) renders the attribute with an *empty* value when true
    and omits it entirely when false, the same convention as `disabled`/`checked`; it never emits
    the literal string `"true"`. Selector changed to the bare `[aria-selected]` (presence check),
    which is what Blazor's own serialization actually produces. This affected every master tab
    and every sub-tab rail on the page, not something newly introduced — a pre-existing, silent
    styling no-op since the tab strip was rewritten off MudTabs (2026-09-23 evening, above).
  - **Header nav (`HOME`/`ASTRO FACTS`/`NUMEROLOGY`) active state flipped** (rammyps's call):
    `.ik-headtab.is-here` in `MainLayout.razor.css` now reads `background: var(--brand-midnight)`
    / `color: var(--brand-sunset)` — the inverse of the sub-tab rail's sunset-bg/midnight-text.
    Distinct component/token from the `--tab-active-bg` flip-and-revert noted directly above this
    entry — that one was the step-tab rail itself, which keeps its sunset-fill look; only the
    outer header nav changed here.
  - **Rank/score badge convention** (green/sunset-orange/red fills, sunrise-orange text on
    Strong/Weak, midnight text on Moderate) applied to `PlanetStrengthChart`/`HouseStrengthChart`
    rank badges, and `AshtakavargaChart`'s Sarvāṣṭavarga bar+value (cited `docs/research/domain/
    transit-events.md` SAV &gt;30/25-30/&lt;25 threshold) and Bhinnāṣṭavarga cells (existing
    presentation-only 0/1-5/≥6 bindu band, recolored). `--status-moderate` itself changed from an
    unrelated amber `#c9820a` to `var(--brand-sunset)` in `tokens.css`, so every existing
    `--status-moderate` consumer picked up the new color automatically. Piṇḍa (Rāśi/Graha/Śodhya)
    was deliberately left unbanded — those are multiplier/remainder values used to locate a
    target nakshatra/sign, not strength scores, and there's no cited threshold for what a "strong
    Piṇḍa" would even mean. Full convention + the compound-selector specificity trap hit building
    this: `docs/ui/design-language.md` "Rank/score badge convention."
  every tab, active or not, just the opposite colour pairing. See `design-language.md` "Tabs".

## 2026-10-01 — strength cut-offs sourced and shared

- One set of Strong / Moderate / Weak cut-offs for the whole app: `StrengthBands`
  (`src/Ikiastrro.Core/Engines/Strength/StrengthBands.cs`), mirrored in `tbl_Rule_StrengthBand`
  (migration 154) with one `SourceRefCode` per boundary; CLI `verify-strength` checks the two agree.
  3.1 Planet Strength, 3.2 House Strength, 3.3's Sarvāṣṭakavarga bar and Key Inference all read it.

| Scale | Strong from | Moderate from | Source |
|---|---|---|---|
| Ṣaḍbala % of required minimum | 100% | 80% | 100% = Parāśara's required minimum (`SRC_BPHS_27`); 80% is ours (`SRC_IKIASTRRO_SYNTHESIS`) |
| Bhava Bala Rūpas | 7 | 5 | both ours (`SRC_IKIASTRRO_SYNTHESIS`) — no classical cut-off found |
| Sarvāṣṭakavarga bindus | 31 (above 30) | 25 | `SRC_PVR_INTEGRATED` (`docs/research/domain/transit-events.md`) |

- Key Inference used to call Ṣaḍbala Strong at 110% and Weak under 90%, so the same planet could
  read differently on the two pages; rammyps chose 100% / 80% everywhere. Its middle label is now
  MOD (was ADEQ).
- The 5-band Capacity scale (75/90/110/130%, `stat_strength.md` §1.1) in 3.1's Capacity column and
  `PlanetaryStateTable` is a separate scale and is unchanged.

- **Fixed (2026-10-01): the 1.4 "Rāśi & graha dispositors" table was always empty.** Its
  `ChartType` and `AscendantSign` string parameters were written without `@`
  (`AscendantSign="relationshipChart.AscendantSign"`), so Razor passed that literal text, no sign
  parsed, and the table rendered a header with no rows. `DispositorTable` now also shows an
  `EmptyState` instead of a silent empty table (`DispositorTableTests`).

## 2026-10-02 — JHora gap step 1: stored-but-hidden facts surfaced

Gap review against Jagannatha Hora's Basics / Strengths views (`cproj_win_app_explorer/explorer_output/jhora`).
Step 1 shows facts that were already computed and stored; no new calculation or migration.

- **1.1 General** gains `SpecialPointsTable` three times, under Sign &amp; Nakshatra combination:
  **Upagrahas** (all 11, with degree, nakṣatra and pada derived from the stored longitude),
  **Ārūḍha padas** (AL, A2–A12 with pada names, A12 shown as Upapada) and **Graha ārūḍhas** (GA_*).
  Ārūḍhas show sign and house only — they are stored at the Lagna's degree, which means nothing.
  This builds the "supporting cards" for Upagrahas and Arudhas that this spec listed as not built;
  Special Lagnas stay with step 4.
- **1.2 About Planets** gains `PlanetMotionTable`, "Motion, combustion &amp; planetary war", under the
  dignity table: direction, speed °/day, ecliptic latitude, distance from the Sun, the combustion
  orb used and the stored combust flag, and Graha Yuddha (wins vs / loses to, with the orb).
  Speed and latitude went here, not into 1.1's already wide positions table.
- **`GrahaYuddha`** (Core, `Engines/Strength`) now owns war detection — the five tara grahas, 1° orb,
  more northern latitude wins. `ShadbalaCalculator`'s Yuddha Bala and the new table both call it.
- **Ishta / Kashta phala** were held back at first (wrong stored values) and fixed the same day — see below.

## 2026-10-02 — Cheṣṭā Bala and Ishta / Kashta fixed

- **Cheṣṭā Bala** was a placeholder (60 retrograde, 30 direct, 0 for Sun and Moon). Now `CheshtaBala`
  (Core, `Engines/Strength`): Mars–Saturn from the Cheṣṭā kendra on Raman's Ujjain-1900 mean motions
  (SRC_RAMAN_GRAHA_BHAVA_BALAS, constants as transcribed in PyJHora); the Moon is her Pakṣa Bala
  (elongation / 3); the Sun is his Ayana Bala, (24° + declination) × 60/48, per BPHS.
- **Ishta / Kashta**: Ishta = √(Uchcha × Cheṣṭā), Kashta = √((60 − Uchcha) × (60 − Cheṣṭā)) (Kashta
  was stored as 60 − Ishta). Shown in 3.1's per-planet breakdown header ("Ishta · Kashta").
- **Against JHora (1_Ramakrishnan, `verify-strength` Phase 1b):** Cheṣṭā within 0.2 virūpas for the
  five tara grahas, the Moon exact; Ishta/Kashta within 0.15 for all but the Sun. JHora's Sun Cheṣṭā is
  40.40, ours 45.12 — JHora's method for the Sun is undocumented, so BPHS is followed.
- Every Shadbala total moved (Cheṣṭā is one of the six balas); all 8 people were regenerated with
  `rebuild-all` and `backfill-strength-statistics`.
- **Pakṣa and Ayana Bala fixed (same day).** `PakshaBala`: e = Moon–Sun elongation (0–180°);
  Jupiter/Venus e/3, Sun/Mars/Saturn 60 − e/3, the Moon always e/3 doubled (as JHora does, even
  waning), Mercury by his sign-mates (more benefics or alone → benefic; tie → nearest decides).
  `AyanaBala`, new in Kāla Bala (`AYANA_BALA`, already in the db/39 catalog): (24° ± kranti) × 60/48,
  north adding for Sun/Mars/Jupiter/Venus, south for Moon/Saturn, Mercury always; the Sun's doubled.
  Kranti is the declination of the ecliptic longitude — with the planet's latitude the Moon was 6
  virūpas off JHora, without it all seven are within 0.5. `verify-strength` checks both against JHora.
- **Nathonnata, Abda and Māsa Bala fixed (same day).** Nathonnata is graded by the hour from apparent
  midnight (`NathonnataBala`; 0.62 off JHora, whose midnight sits ~7½ min earlier). Abda
  (`VARSHA_BALA`, 15) and Māsa (`MASA_BALA`, 30) lords come from Raman's ahargana
  (`AbdaMasaLords`) and match JHora. Kāla Bala is now within 1 virūpa of JHora for all seven planets.
- **Sthāna Bala fixed (same day) — see "Saptavargaja, Oja-Yugma and D2/D3/D7" below.**

## 2026-10-02 — Saptavargaja, Oja-Yugma and D2/D3/D7 on JHora's schemes

- **Saptavargaja** (`ShadbalaCalculator.AddSthana`): the compound relationship with each varga's
  sign lord is now the Rāśi chart's in every varga (temporary friendship counted from both
  planets' D1 signs — `PvrDignityEvaluator.Evaluate`'s new `relationshipFromSign`), and
  moolatrikoṇa scores 45 in D1 only; in the other six vargas an MT sign scores as own sign if the
  planet rules it, else by the lord relationship. It used to read each varga's own positions and
  apply MT degree ranges to varga degrees.
- **Oja-Yugma Rāśi-Aṃśa**: 15 for the D1 sign + 15 for the D9 sign (Moon and Venus in even signs,
  every other graha — Mercury and Saturn included — in odd). It used to score 30 or 0 from D1 alone
  and gave Mercury the even-sign rule.
- **D2, D3 and D7 switched app-wide to JHora's defaults** — Uma-Shambhu Hora, Uma-Shambhu Drekkana,
  Saptāṃśa 7-1 (migration 164, [decision 005](../../../decisions/005-d2-d3-d7-jhora-default-schemes.md)).
  All 8 people regenerated (`rebuild-all`, `backfill-strength-statistics`).
- **Against JHora (1_RamakrishnanP, `verify-strength` Phase 1c):** Uchcha, Saptavargaja, Oja-Yugma,
  Kendrādi and Drekkana match for all seven grahas, except **Venus's Drekkana Bala** (ours 15 per
  BPHS — female grahas in the middle drekkana — JHora 0; Raman's order would break Saturn's 0
  instead), printed as a known gap until a second chart settles JHora's rule.

## 2026-10-02 — 3.1 Planet Strength is one stacked-bar chart

- Replaces the Performance/Composition toggle and the per-planet expand panel. One row per graha,
  strongest first; its bar stacks the six balas + Yuddha in rūpas on **one axis shared by every
  planet**, with that planet's **required-minimum tick** (dashed) through it.
- Negative values (Dṛk Bala) draw **left of zero**, hatched; the axis only extends below zero when a
  planet needs it. Geometry is `StackedBarLayout` (Web, unit-tested).
- **BALAS / SUB-COMPONENTS** switch (chart-control pill) re-splits every bar into its stored
  `tbl_Fact_PlanetaryStrengthComponent` rows, later ones fading toward the card within their bala's
  colour. Hovering a bala segment lists its sub-components in virūpas.
- Row end: Rūpas · Min. · % Min. · Capacity · Ishta · Kashta · Status. On phones the bar takes its
  own line under the planet and Min./Capacity hide.
- Colours read `--card-fg`/`--card-bg` (not `--brand-midnight`) so cosmic-dark stays legible.

## 2026-10-02 — 3.2 House Strength is a stacked-bar chart too

- Same shape as 3.1: one bar per house stacking Bhāvādhipati / Bhava Dig / Bhava Dṛk in rūpas on one
  shared axis (`StackedBarLayout`), negative Dṛk left of zero, dashed lines at the Moderate (5) and
  Strong (7) cut-offs. The expand panel is gone; its "Independent support" (Dig + Dṛk) is now the
  **Indep.** column. House order / Strength rank toggle kept (rank stays by strength). The rank badge's
  colour carries Strong / Moderate / Weak. On phones the bar takes its own line.

## 2026-10-02 — JHora gap step 2: Yogi, nava tārā, Mrityu bhāga, Puṣkara, Bhṛgu Bindu, Varṇada

Computed live from the **D1** key details (whichever chart the picker shows — these tables never follow it) by Core `Engines/KeyInfo` —
nothing stored, no regeneration needed. `KeyInfoTests` checks every figure against JHora's
1_Ramakrishnan views.

- **1.1 General → "Yogi & sensitive points (D1)"** (`SensitivePointsTable`): Yogi point (Sun + Moon +
  93°20′; Yogi = its nakṣatra lord, Sahayogi = its sign lord), Avayogi point (+186°40′), Bhṛgu Bindu
  (Rahu→Moon midpoint), and Varṇada V1–V12 (BPHS count rule on Lagna + Hora Lagna, the Lagna's degree
  kept as JHora does).
- **1.1 General → "Nava tārā (D1)"** (`NavaTaraTable`): the nine tārās from the Moon and from the Lagna.
- **1.2 About Planets → "Mrityu bhāga & Puṣkara (D1)"** (`MrityuPushkaraTable`): grahas, Māndi and the
  Lagna — in Mrityu bhāga (BPHS table) and its distance, in a Puṣkara navāṃśa, in the Puṣkara bhāga
  (Jātaka Pārijāta degrees, as JHora) and its distance. "In" = inside that single degree.
- **Known gap:** JHora's Mrityu bhāga for the Moon in Scorpio is ~23° where the BPHS table says 14°.
  House-cusp rows (JHora lists 2nd–12th) are not shown.

## 2026-10-02 — Natal tab layout: SPECIAL POINTS sub-tab, jump links, D1 tags

- **GENERAL** is now Birth Pañchāṅga, Planet positions and Sign &amp; Nakshatra combination only.
  Upagrahas, Ārūḍha padas, Graha ārūḍhas, Yogi &amp; sensitive points and Nava tārā moved to a new
  **SPECIAL POINTS** sub-tab, second in the rail (`_activeTab` 4, so the older indices and
  `?step=` links keep their meaning; `?step=specialpoints|upagrahas|arudhas` opens it).
- **Jump links** (`.ki-jump`) head every natal sub-tab with four or more sections — Special points,
  About planets, About houses, Signs &amp; planet aspects — and scroll the heading to just under the
  fixed app bar, moving focus to it (`wwwroot/js/scroll.js`). Not printed.
- **D1 tag.** Sections read from the D1 rāśi chart whatever the picker shows carry a "D1" tag in
  their heading (replacing the old "(D1)" suffix): Yogi &amp; sensitive points, Nava tārā, Moon
  context, Motion/combustion/planetary war, Mrityu bhāga &amp; Puṣkara, Planet states. While the
  picker is on another chart a note beside the tag says so ("always the rāśi chart — not the D9
  in the picker"). Every other natal section follows the picker.
- **Fixed with it:** Motion, combustion &amp; planetary war and the Planet states table read the
  picked chart's key details — the avasthā table showed D1 states beside the picked varga's
  dignity. Both now read D1. Section ids and titles live in one `NatalSection` catalog in
  `AstroFacts.razor`.

## 2026-10-02 — JHora gap step 3: Śodhita, Vaiśeṣikāṃśa, Ṣaṣṭyaṃśa deities; Key Inference links

- **Direct Key Inference links removed** — the chart toolbar's "KEY INFERENCE →", each All Charts
  card's "Read Dn in Key Inference →" and the Spl Lagnas grid's link. Astro Facts reaches Key
  Inference only through the life matters in About Houses' House lord placement table.
- **Person switch** keeps the picker's chart, falling back to D1 when the new person lacks it (the
  Natal panel used to go blank). The Spl Lagnas grid's pinned sign clears.
- **3.3 Ashtavarga → "Śodhita Aṣṭakavarga"** card between Bhinnāṣṭavarga and Piṇḍa: each graha's
  BAV after Ṭrikoṇa then Ekādhipatya Śodhana (Core `AshtakavargaCalculator`, the reduction the
  Piṇḍa already used), occupancy from the selected chart. All seven rows match JHora's SoAV view
  for RamakrishnanP. The last row is the column sum ("Σ reduced"); JHora's SoAV "SAV" box is built
  some other, undocumented way and does not equal it.
- **6 Vargas → Aṁśabala rows show the Vaiśeṣikāṃśa grade** (`AmsabalaRow.AmsaName`, already stored).
  Counts match JHora's "Vaiseshikamsas in four varga schemes" for Shadvarga and Saptavarga; Dasavarga
  and Shodasavarga differ for Jupiter and Saturn — JHora's D10 (5-8) / D60 (RvAr) variants, not yet
  reconciled. Rahu and Ketu are not graded (JHora grades them).
- **6 Vargas → "Ṣaṣṭyaṃśa (D60) deities"** (`ShashtiamsaTable`, D1 tag): part, deity and nature for
  the Lagna and nine grahas, live from the D1 degree. Names and natures follow
  [decision 006](../../../decisions/006-shashtiamsa-names-bphs-jhora.md); all nine bodies but the
  nodes match JHora.
- **KP sub-lords** were already in 1.1's Planet positions (Sub Lord, chain L2–L7). They match JHora
  through the prati-sub; from the sookshma down the few arcseconds left between our positions and
  JHora's (decision 009) can move them.

## 2026-10-07 — JHora gap step 4 surfaced: special tārās, lattā, special tithis, sphuṭas, sahams, malicious divisions

Computed live from the **D1** key details by Core `Engines/KeyInfo` and `Engines/Karakas` (nothing stored; engines are checked against
JHora in `Ikiastrro.Yoga.Tests.KeyInfoJhoraStep4Tests`). Six new tables follow **Nava tārā** on the SPECIAL POINTS sub-tab, each with
a one-line plain-words summary above it, a collapsed "What is this?" guide (`AstroFactsGuides`), a D1 tag and a jump link.

- **Special tārās** (`SpecialTaraTable`) — eleven, from the Moon and the Lagna side by side, on the 28-nakṣatra circle with Abhijit.
  Summary names the Naidhana and Abhiṣeka stars.
- **Lattā nakṣatras** (`LattaTable`) — each graha's occupied and kicked star, and who sits in the kicked star. Summary lists the "struck" planets.
  JHora's "Aspected Stars" column is not reproduced (undocumented rule).
- **Special tithis** (`SpecialTithiTable`) — all 36 with tithi, half, % left, lord and Nityā devī. Summary gives the Janma, Mṛtyu and Dhana tithis.
- **Sphuṭas** (`SphutaTable`) — the 14 rows (13 sphuṭas + Yogi/Avayoga), with a "Made from" formula column. Summary names the Prāṇa/Deha/Mṛtyu signs.
  **Gulika naming:** the formulas use JHora's Gulika, which this app stores as **Māndi** (the two upagraha names are swapped against JHora;
  checked on Ananya: app Māndi 28°22′49″ Taurus = JHora Gulika 28°24′30″). `KeyInfoInputs` reads the `Maandi` row.
- **Sahams** (`SahamsTable`) — the 36, day or night from the Pañchāṅga's `IsNightBirth`. Summary says which formula direction applied and lists sahams in the 6th, 8th or 12th house.
- **64th navamsa & 22nd drekkana** (`MaliciousDivisionsTable`) — lords from the Lagna and the Moon; the 22nd drekkana in all five D3 schemes.

Shared plumbing: `KeyInfoInputs` (D1 positions, house-of) and `KeyInfoFormat` (sign / degree / star text). Planet chips in these tables carry the
planet's name beside the glyph (the glyph alone is unreadable for the lord columns).

**Chart picker width.** `.ki-chart-picker` was a fixed 27ch, leaving a ragged right edge against the 320–420px chart column. It now takes the
rest of the toolbar row beside EXPAND/COLLAPSE CHART, so the toolbar spans exactly the chart's width (measured: picker right edge = chart right edge, 420px).
Any new chart dropdown should use the same class so it inherits this.

**Known:** values for Ananya differ from JHora's export by a few arc-minutes in the sums that include Sun, Moon, Rahu or the Lagna
(Moon−Sun ≈ 3′, Rahu−Sun ≈ 25′) — input differences, not the formulas, which match JHora exactly when fed JHora's own longitudes.

## 2026-10-07 — Spl Lagnas: dropdown system, special points on the wheel and the grid

The READINGS ▸ SPL LAGNAS chart (page `KarakaPolarWheel`, Wheel = `PolarGridLagnaSelect`, Grid = `SindUni3Grid`) now carries every special
point, not just the special lagnas.

- **One stage, one width.** `.kpw-stage` (740px) holds the controls and the chart, so the chart picker + Wheel/Grid row and the dropdown
  row line up with the chart's own edges. Phones wrap the dropdowns two to a row.
- **Dropdown system — `ChartMenu`.** A multi-select dropdown: button with the on-count, a panel of checkboxes under group headings, Find box
  (long lists), per-group all/none, menu-wide All / None and named presets. No JS (state + backdrop; Escape closes). Four menus:
  **Lagnas** (the 12 `SindUniGlyphs.SpecialLagnas`; presets *Wheel rings*, *Classic*), **Upagrahas** (11 upagrahas + Bhṛgu Bindu + Varṇada V1–V12),
  **Sphuṭas** (14), **Sahams** (10 life-area + 26 others; preset *Key 10*). Both views read the same selections, so the grid's own lagna-chip
  row and the wheel's lagna checkboxes are hidden (`HideLagnaToolbar` / `HideControls`); the old controls still work where the components are used alone.
  Default on: the classic lagnas and the *Essentials* points (both Gulika points, Bhṛgu Bindu, Prāṇa/Deha/Mṛtyu sphuṭas, Punya/Vivaha/Karma sahams).
- **`PointCatalog`** builds every point from the D1 key details with the Core engines (`Sphutas`, `SahamTable`, `SensitivePoints`) and places it on
  the picked chart: a point keeps its D1 longitude and takes the sign that longitude falls in under that varga's rule
  (`VargaSchemeRepository` → `VargaSignRuleFactory`), house from that chart's Lagna. Group colours: `--pg-upagraha/-sensitive/-sphuta/-saham`.
- **Grid:** coloured chips in the cell's tag row; the cap shrinks with the planets in the cell (a "+n" chip holds the rest); the centre card of a
  hovered or pinned sign gets a *Points here* list of everything in it with degree and house.
- **Wheel:** a points ring between the planets and the lagna rings (the lagna rings share what is left). Chips are horizontal text, so the grid
  they sit in follows the sector: rows of five on top/bottom sectors, columns of three on the side ones; "+n" past capacity; each chip's `<title>` names it.
- **Below the chart:** a one-line summary of how many points are shown, the Special Lagnas table, and a *Special points on this chart* table.
- The tab's plain-words guide (`karakas`) explains the dropdowns, chip colours and the divisional-chart rule.
- **Known:** `PolarGridLagnaSelectTests` has two older tests (12 sectors; one active ring by default) that predate the five-ring redesign and fail on
  master as well; they were not touched.
