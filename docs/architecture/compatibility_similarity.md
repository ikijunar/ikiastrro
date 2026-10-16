# Compatibility (marriage matching) and similarity (horoscope search)

Status: proposal, 2026-10-03. Not yet a decision record. Intended to become `decisions/010` (matching) and `decisions/011` (similarity).

Two features share one set of persisted layers (L0 to L2) and differ only in what they read from them.

| | Compatibility (marriage) | Similarity (horoscope search) |
|---|---|---|
| Question | Are A and B suited? | Which charts look like this one, or fit this pattern? |
| Reads | L3 match profile (a view) | L3b feature atoms (a table) |
| Probe | The seeker, who is persisted | Any saved chart, or a hand-built pattern |
| Searched set | Candidates, which are transient | A reference corpus, which is persisted |

The corpus is persisted because similarity search queries a large set repeatedly. Recomputing thousands of charts per query defeats the purpose. This is a deliberate exception to "candidates are not persisted", and it applies only to the corpus.

## 1. Layers

Each layer reads only the layer above it. Nothing below L0 may call the Swiss Ephemeris.

| Layer | What it is | Where it runs | Persisted |
|---|---|---|---|
| L0 Ephemeris | Raw Swiss Ephemeris output: JD/UTC, ayanamsa, Lagna and cusps, and per body longitude, latitude, speed, retrograde flag | C#, the only ephemeris caller | Yes, existing `tbl_Chart_KeyDetails` raw columns |
| L1 SQL-derived | Arithmetic or lookup over L0 plus reference tables: sign, house, nakshatra and pada, Gana, Yoni, Nadi, Varna, Vasya, Tara, Rajju, dignity, aspects, dasha periods | SQL views, computed columns, backfills | Yes, or computed on read |
| L2 Rule-evaluated | Facts that need rule logic: Kuja dosha, papasamya, Rahu-Ketu dosha, yogas, strengths, karakas, UL and DK | C# engines over L1 | Yes, written once per chart |
| L3 Match profile | One row per saved person joining L1 and L2 for matching | View `vw_Chart_MatchProfile` | No |
| L3b Feature atoms | Flattened comparable facts per chart (section 5) | SQL and C# extractors over L1 and L2 | Yes |
| L4 Family | People, relationships, life events | Tables | Yes |
| L5 Relationship analysis | Kuta scores, doshas, synastry for real relationships only | C# matching engine | Yes, existing relationships only |
| L6 Match search | Seeker criteria, transient candidates, shortlist | In memory plus small tables | Seeker and shortlist only |

## 2. What already exists

- Moon sign, nakshatra and pada in `tbl_Chart_KeyDetails` and `vw_ChartMoonContext`.
- Gana, Yoni animal and gender, Nadi seeded (migration 098, source `SRC_VASUDEV_MATCHING_CHARTS`).
- Varna and sign lord on `tbl_SignAttributes`.
- Natural planetary relationships (7 planets, enough because Moon-sign lords are never nodes).
- House lords, aspects, conjunctions, Vimshottari dashas, D9, chara karakas (DK), Upapada and Arudha special points.
- `BirthDetails.Sex`, needed because several kutas are directional.

## 3. Gaps

Classical kuta data (reference tables, sourced from Vasudev; the repo norm is one cited source, never seeded from memory):

| Need | Used by |
|---|---|
| Vasya sign groups, with the Sagittarius and Capricorn split by degree | Ashtakoota |
| Yoni score matrix (14x14) and enemy pairs | Ashtakoota, Dasakoota |
| Gana, Graha Maitri, Bhakoot, Nadi score matrices, plus Nadi and Bhakoot cancellation exceptions | Ashtakoota |
| Tara groups and good/bad classification | Ashtakoota, Dasakoota |
| Rajju groups with direction, Vedha pairs | South Indian Dasakoota |
| Dina, Mahendra, Stree-Deergha, Rasi, Rasi-Adhipati rules | South Indian Dasakoota |

Chart-level facts to compute (L2):

- Kuja dosha: Mars in houses 1, 2, 4, 7, 8, 12 from Lagna, Moon and Venus, with a cancellation table. Stored as separate flags so cancellation logic is not baked in.
- Papasamya: malefic point score by house from Lagna, compared between the two charts.
- Rahu-Ketu (sarpa) dosha.
- Marriage-house health: 7th house, 7th lord, Venus status.
- Jaimini markers: DK, Upapada Lagna and the 2nd from UL (PVR is already cited in the life-matter reference).
- Navamsa: D9 Lagna and D9 7th.
- Dasha: current and upcoming lords for both people, dasha sandhi.
- Cross-chart synastry (tier 2): Moon-to-Moon, Venus-to-Mars, inter-chart aspects, house overlays.

A second source is needed for the dosha rules (for example PVR or BPHS). Vasudev covers kutas.

## 4. Compatibility design

### 4.1 L4 family and relationships

- `tbl_Dim_RelationType`: spouse, parent-of and child-of, sibling, step, adopted, in-law, and so on. Each row has an inverse type, a role label (father, mother, son, daughter), and a tier: Core (spouse, parent, child), Extended, or Other.
- `tbl_Person_Relationship`: direct edges only (spouse, parent to child), plus `IsAdopted`, `IsStep`, `Start`, `End`, `Status`.
- Siblings, grandparents, uncles and in-laws are derived by a recursive view, `vw_Family`, over those edges. Birth order comes from DOB. Edges stay the source of truth, so tiers cannot go stale.
- `tbl_Person_LifeEvent`: marriage, divorce, child birth, death, with date and place. It enables dasha-at-marriage and dasha-at-child-birth, and it is the main ground-truth table for validation and the Analytics project.

### 4.2 L5 relationship analysis (persisted for real relationships only)

- `tbl_Relationship_Analysis`: `RelationshipId`, scheme, rule-set version, total score, run date.
- `tbl_Relationship_Kuta`: per-kuta score, max, status, reason.
- `tbl_Relationship_Dosha`: dosha comparison and cancellations.
- Synastry rows: cross-chart aspects and house overlays, for parent-child pairs as well as spouses.
- Keyed to the rule-set version, so a rule change is handled by a rebuild by version.
- There is no pair table for arbitrary pairs. A pair result is a pure function of two profiles and the rule tables, so storing it for N people means N-squared rows that go stale.

### 4.3 L6 match seeking

- `tbl_MatchSeeker`: `PersonId` plus criteria (sex sought, age range, scheme, hard filters such as Nadi and Kuja). The seeker already has a persisted full chart.
- Candidates are not persisted. They come from an imported list or CSV and are scored in memory. A live pass computes Lagna and the nine grahas through one Swiss Ephemeris pass and builds an in-memory profile. If a candidate must survive a refresh, use a batch-staging table with an expiry.
- One scorer, two data sources, behind `IChartFacts`:
  - `StoredChartFacts` reads L0 to L2 for saved people and the seeker.
  - `LiveChartFacts` computes from birth data for candidates.
  - `verify-matching` asserts both give the same answer for a saved person.
- `tbl_MatchShortlist`: seeker, candidate birth data, status, note. No scores, so it cannot go stale. Promoting a candidate means "Save as person", which runs full chart generation and persists L0 to L2. They can then become a real relationship and get L5 analysis.

### 4.4 Scoring engine

- `Score(profileA, profileB, scheme, roles)` returns per-kuta score, status, reason, cancellations and source code.
- Roles (bride and groom) come from `Sex`. Same-sex or missing `Sex` makes the user choose roles.
- Schemes: `Ashtakoota8` (36 points), then `Dasakoota10` (South Indian), behind an `IKutaScheme` interface.
- The engine is the only implementation. SQL does cheap pre-filtering only (sex, Nadi mismatch, Kuja match, age range).
- Two tiers:
  - Tier 1, profile-only: kuta score plus dosha comparison, run for the seeker against the whole list and ranked.
  - Tier 2, on demand for one chosen pair: synastry, D9 and UL comparison, dasha compatibility.
- The dosha and synastry layers are shown beside the kuta total and never merged into it.

## 5. Similarity design

### 5.1 Feature atoms (L3b)

Every chart is flattened into small comparable atoms, derived from L1 and L2 only, with no ephemeris call.

- `tbl_Dim_FeatureType`: code, scope (D1 or a varga), extractor (SQL view or C# engine). A new kind of fact is a new row plus an extractor, with no schema change.
- `tbl_Chart_Feature`: `(ChartResultId, FeatureTypeId, SubjectKey, ValueKey, Varga)`, indexed on `(FeatureTypeId, SubjectKey, ValueKey)` as an inverted index.

Feature types, simple to rich:

- Placement: planet in sign, house, nakshatra or pada, Lagna sign.
- Dignity and state: exalted, debilitated, combust, retrograde, vargottama.
- Relations: conjunction sets, mutual aspects, house-lord placements, karaka roles (AK, DK).
- Yogas and strength bands.
- Stelliums at three levels of abstraction:
  - Exact: Sun, Mars, Mercury and Venus in Aries.
  - Relative: the same four in the same house from Lagna.
  - Shape: any N or more grahas sharing a sign, in any sign.
- Event-anchored atoms from `tbl_Person_LifeEvent`, for example the dasha lord at marriage.

### 5.2 Query modes

1. Pattern search (exact): a criteria builder (planet set, sign, house or nakshatra, "at least k of n", varga, dignity, conjunction, yoga, Lagna) that compiles to SQL over the atom index. Patterns are saved in `tbl_Search_Saved` and `tbl_Search_Term`.
2. Seed from a person: pick a chart, the app lists its most distinctive atoms, the user ticks the ones that matter and sets weights, and the app builds the pattern.
3. Similarity ranking (fuzzy): score = sum of weights of shared atoms divided by the sum over the probe's selected atoms. Atom weight = user weight multiplied by rarity, `-log(baseRate)`. Output is a ranked list with chips showing the shared atoms and each atom's base rate.

### 5.3 Base rates

- Sun, Mercury and Venus sit close together, so "three of them in one sign" is common. Rare atoms must count for more.
- `tbl_Feature_BaseRate`: frequency of each atom in a baseline population.
- The baseline is generated in bulk by the CLI from L0 for many birth times spread over decades (sign-level positions do not depend on place), using the existing Swiss path.
- The UI shows "this pattern occurs in about x% of the baseline" so a coincidence is not presented as evidence.
- Patterns are zodiac-dependent. The app is Lahiri-locked (decision 004), so all matches are in Lahiri. Check that any worked example (for instance the four-planet Aries stellium) holds under Lahiri when the data is seeded.

### 5.4 Reference corpus and tags

- `tbl_Dim_PersonSource` separates Personal, Family and Reference (public figures). Reference people carry a Rodden-style data-quality rating and a source citation.
- Check the licence of any dataset before bulk import. The existing CSV and JKD import covers the mechanism.
- `tbl_Person_Tag`: controlled vocabulary for occupation, notable traits and outcomes. This allows the search to run both ways: patterns to people, and charts tagged X to the patterns they share. It connects to the parked base-rate and Validation items in `ROADMAP.md`.
- Privacy: only public figures go in the reference corpus. Personal and family charts are never mixed into a public search unless the user opts in.

### 5.5 Status of the feature layer (2026-10-16)

Built first as views over what is already stored, so there is nothing to refresh:

- `vw_Chart_Feature` (migrations 184, 185): one narrow row per person, chart/varga, feature type, subject and value. Types so far: `YOGA` (presence, one atom per YogaCode however many sources catalogue it), `PLANET_SIGN`, `PLANET_HOUSE`, `PLANET_NAKSHATRA`, `DIGNITY`. A new type is one more `UNION ALL` branch.
- `vw_Feature_BaseRate` (migration 186): how many saved people have each atom. This is the rate in the **saved population only**, not the generated baseline of 5.3; the screens label it "saved people".
- `FeatureCompareRepository` and `FeatureComparer` (Data): compare chosen people (all / shared / differences), overlap of two people, who has this fact, and the rates. A fact is shared only when subject and value both match. Counts, never a similarity score.
- UI: Astro Facts, READINGS, COMPARE (`FeatureCompare`).

Not built, deliberately: the materialised `tbl_Chart_Feature` and `tbl_Dim_FeatureType` (the views read in about a quarter of a second at 70 people; build the table when a C#-only feature, such as a strength band, needs persisting, or when the corpus grows), the generated-baseline `tbl_Feature_BaseRate`, weighted similarity ranking (needs that baseline), and the pattern builder and saved searches (5.2).

## 6. Roadmap

Shared foundation:

1. L4 family and relationship tables, `tbl_Person_LifeEvent`, `vw_Family`.
2. L1 and L2 additions: Vasya, Tara, Rajju, Vedha lookups, Kuja and papasamya facts.

Compatibility track:

3. `vw_Chart_MatchProfile`, `IChartFacts`, the Ashtakoota engine, `verify-matching` against the book's worked examples.
4. Pair UI and L5 persistence for real relationships.
5. Seeker, live candidates, shortlist; then Dasakoota and synastry.

Similarity track:

6. Feature-type dimension, extractors, `tbl_Chart_Feature`, `verify-features` against known charts.
7. Pattern search UI, including "seed from a person".
8. Corpus import, source and rating fields, tags.
9. Base rates and similarity ranking with rarity weights.
10. Outcome-tag statistics, event-anchored atoms, side-by-side wheel compare.

Steps 6 and 7 can run in parallel with step 3 once step 1 is done. Feature work goes on workstream branches: database and CLI work on `workstream/database` and `workstream/cli`, UI on `workstream/ui`.

## 7. Open decisions

1. Corpus source: a named dataset (for example Astro-Databank, subject to licence), or only the user's own imports.
2. Atom depth for v1: placements, stelliums and conjunctions first, with yogas and event atoms later?
3. Order: compatibility first or similarity first. Similarity needs less new reference data and could ship earlier.
4. Schemes: Ashtakoota only first, or both Ashtakoota and Dasakoota.
5. Sources: Vasudev for the kuta matrices, and which second source for the dosha rules.
6. Scope of "kids" in v1: birth order, adopted and step flags and life events are included. Per-child notes or putra-bhava prediction versus actual children are not.

## 8. Build log

### 8.1 Slice 1 (2026-10-03): Ashtakoota calculator in Core

Branch `workstream/cli` (worktree `ikiastrro.wt/cli`), uncommitted at the time of writing.

- `Core/Engines/Matching/AshtakootaCalculator.cs` with `KutaModels.cs`: a pure calculator over a `MatchPerson` (Moon sign, nakshatra number, Gana, Yoni animal, Nadi). Gana, Yoni and Nadi stay inputs read from `tbl_Nakshatras` (migration 098), so the stored table is the single source.
- `DignityEngine.NaturalAttitude(planet, other)`: a small public accessor over the existing natural-friendship table, for Grahamaitra.
- `tests/Ikiastrro.Yoga.Tests/AshtakootaCalculatorTests.cs`: 43 tests built from the book's own examples, all passing. The suite is 473/473.
- No database or UI changes yet. The rule tables are in code, with page citations, following the existing precedent where a C# truth table is checked against reference rows.

### 8.2 What the source book actually says (read from the scanned pages, Ch. VI, pp.65-83)

This corrects assumptions made earlier in this document.

| Factor | What the book gives | How the calculator treats it |
|---|---|---|
| Varna (1) | Rasi groups, planet Varnas, compensation by sign rulers (p.66-67) | Implemented as written. |
| Vashya (2) | A list of "signs - sign" rows (p.67), not the five-class scheme assumed earlier | Implemented as boy's sign in the girl's sign's list. The direction is fixed only by the book's one example. **To confirm.** |
| Dina / Tara (3) | Count from the girl's star to the boy's, remainder 2, 4, 6, 8 or 0 is good (p.68) | Implemented. |
| Yoni (4) | Animal table (p.69), 4 for the same animal, 0 for hostile, "2 to 3" for passable. **No hostile-pair table.** | Same animal 4; different animals left **Unscored**, which widens the total to a min-max range. **Needs a second source.** |
| Grahamaitra (5) | 5 / 4 / 3 / 2 / 0 by the two rulers' attitudes (p.70-71) | Implemented from the existing natural-friendship table. |
| Gana (6) | Nine-way score table (p.72) | Implemented. |
| Rasi / Bhoo (7) | Qualitative verdicts per distance, directional, with exceptions (pp.73-75), maximum 7 | Favourable scores 7, unfavourable 0. **That 7/0 mapping is this project's reading.** |
| Nadi (8) | 8 if different, 0 if same (p.75) | Implemented. |
| Rajju | Five groups, no numeric value (p.76-77) | Reported as Present/Absent, no score. No ascending/descending direction is given. |
| Stree Deergha, Mahendra | Rules with no numeric value (p.77) | Reported as Present/Absent. |
| Vedha | Named as the tenth factor (p.66, p.78). **No table in the scanned chapter.** | Not computed. **Needs a second source.** |
| Dasakoota | The book lists ten factors but scores only the eight Ashtakoota ones | The 10-factor scheme is therefore not a separate scored scheme here. |

The book's pass mark is 18 of 36. Its worked example (Jyeshta boy, Anuradha girl, both Scorpio) totals 28, or 37 of 45 if Rajju is given 9; the calculator reproduces 28.

The book also says Kuta agreement "is not decisive" and should come only after longevity, health, balance of doshas and Kuja dosha have been cleared (p.79). This supports keeping the dosha comparison separate from the Kuta total, as designed above.

### 8.3 Next slices

1. Read the saved Moon nakshatra, Gana, Yoni and Nadi into `MatchPerson` for two saved people, and show the Kuta table on a pair page.
2. A second source for the Yoni hostile pairs and for Vedha; a decision on the Vashya direction and the Rasi 7/0 reading.
3. Kuja dosha, papasamya and Rahu-Ketu dosha (the book's Ch. on Dosha Samya, around p.55-61, is already in the same PDF).
4. The L4 relationship tables and the match-profile view.

## 9. Dasha aspects for compatibility (proposal, 2026-10-03)

### 9.1 What is already stored

`tbl_Chart_DashaPeriods` holds the whole Vimshottari tree for every saved person (892 and 836 rows for the two people checked), under the chart type `VimshottariDasha`. `tvf_Chart_DashaLordRelationship` already joins each period's lord to its D1 sign, nakshatra and KP sub-lords. `tbl_Chart_HouseLords` gives each planet's lordships and placements. `DignityEngine.NaturalAttitude` gives permanent friendship. An Ashtottari calculator exists in Core. No Jaimini (Chara) dasha is persisted.

Spot-check on 2026-10-03 for the two saved charts used in the audit:

| | RamakrishnanP | RameshwariS |
|---|---|---|
| Birth Mahadasha (from the Moon nakshatra lord) | Saturn (Anuradha) | Moon (Shravana) |
| Running Mahadasha | Venus, 2018-10-14 to 2038-10-13 | Saturn, 2026-06-06 to 2045-06-05 |
| Running Antardasha | Rahu, 2025-12-13 to 2028-12-13 | Saturn, 2026-06-06 to 2029-06-09 |
| Running Pratyantar | Jupiter, to 2026-10-20 | Saturn, to 2026-11-27 |
| Running Mahadasha lord's role | Venus rules his 7th (Libra) and sits in his 1st | Saturn is exalted in her 5th |

### 9.2 Aspects worth adding, in build order

1. **Birth-dasha lord pair.** The two Moon-nakshatra lords and how each regards the other (Saturn and Moon here: Saturn regards the Moon an enemy, the Moon regards Saturn neutral). Pure lookup; available now.
2. **Running dasha pair.** Both people's current Mahadasha, Antardasha and Pratyantar lords, the natural attitude between the two Mahadasha lords (Venus and Saturn here: friends both ways), and the dates each period ends.
3. **Each running lord's marriage role in its own chart.** Flag when the Mahadasha or Antardasha lord rules or occupies the 7th, 2nd or 11th (marriage), the 5th (children) or the 6th, 8th, 12th (strain), is Venus or Jupiter, or is the Darakaraka or the Upapada lord. Data exists (house lords, key details, chara karakas, Arudha A12). The marriage-supportive and strain classifications need a cited source before they ship; the library holds *Astrology and timing of Marriage*, PVR, Raman and the Bhrigu texts.
4. **Dasha sandhi.** Whether either person is near a Mahadasha junction (RameshwariS entered a new Mahadasha in June 2026). The width of the window is a rule that needs a source.
5. **Aligned timeline.** The next 25 years of both people's Mahadasha and Antardasha on one axis, coloured by the classification in item 3, with the windows where both are in a supportive period marked. This turns the page from "are they suited" into "when".
6. **Cross-chart dasha overlay (tier 2).** Whether one person's running lord occupies or aspects the other's 7th house, Venus or Moon. Needs the synastry layer.
7. **Event anchoring.** With `tbl_Person_LifeEvent`, record the Mahadasha and Antardasha at each marriage, so the rules in item 3 can be checked against known marriages (and, for the similarity work, against a reference corpus).
8. **Seeker mode.** For one unmarried seeker against a list, compare the seeker's upcoming supportive windows with each candidate's (item 5 computed in memory).
9. **Other dasha systems.** Ashtottari can reuse the calculator already in Core. Jaimini marriage timing uses the Chara dasha of the Upapada and the 7th, which is not built.

### 9.3 Cautions

- Items 3 to 5 depend on classification rules that are not yet sourced. Until they are, show the facts (lord, lordship, placement, dates) without a good-or-bad verdict.
- The Kuta total, the dosha comparison and the dasha picture stay separate on the page, as the book itself keeps Kuta agreement subordinate to the other checks.

## 10. Compatibility page next steps (plan, 2026-10-04)

Reviewed against an external audit of the rendered page (`UI_SVG_Templates/v5-Build/Compatibility-page.png`). The audit's diagnosis is right: the page is a research report with no synthesis or navigation. Two rules limit how far its advice can go: no verdict without a cited source (section 9.3), and the Kuta total, doshas, Saturn and dasha are never merged into one score (section 4.4).

### 10.1 Verdict on each recommendation

| # | Recommendation | Verdict |
|---|---|---|
| 1, 4 | Summary and strength/concern cards | Adopt, derived mechanically from results already computed (Kuta present/absent, dosha balance, Sade Sati overlap, unscored factors). "What this means" is limited to templated factual sentences, since there is no sourced interpretation text. |
| 2 | Sticky section anchors | Adopt. UI only. |
| 3 | Three disclosure levels | Adopt two (summary, detail) now. A middle "interpretation" level needs sourced text, so it waits. |
| 5 | Data-quality panel | Adopt. The inputs are already loaded; birth-time confidence is shown only if it is stored (to check). |
| 6 | D9 comparison | Adopt, facts first (D9 lagna, 7th, 7th lord, Venus/Jupiter, DK). `VargaChartComputer` and the DK exist. Verdicts wait for a source. |
| 7 | Cross-chart synastry | Adopt as its own section, directional ("A's Mars in B's 7th"). Overlays and aspects are pure computation; no verdict. |
| 8 | Interpreted timing | Partly blocked: supportive/strain windows need the sourced classification in 9.2 item 3. Ship the facts and the aligned shared view; mark the rest unresolved. |
| 9 | Dosha cancellation | Source-gated: a second source for the Kuja cancellation table (section 3). Surface what is already computed first. |
| 10 | Pair history and notes | Small part now (promote the existing "recorded as married" note); the rest needs the L4 relationship and life-event tables. |
| 11 | Export and save | "Reverse roles" already exists (Swap). Add print and copy-summary. Do not persist comparisons yet (L5 design, section 4.2). |
| UX | Quieter headers, labels not colour alone, larger small text, legend, mobile cards, focus styles | Adopt all. The page already labels 18/36 as the book's pass mark; keep that wording. |

Not raised by the audit but the cause of the 22-26 score range: Yoni (different animals) is Unscored and Vedha is not computed. PyJHora (`SRC_PYJHORA`) and Maitreya8 both carry a Yoni matrix and PyJHora a Vedha rule, which would close that range. Check them against Vasudev's animal table before adopting.

### 10.2 Build order

| Phase | Work | Needs |
|---|---|---|
| 0 | Close the score range: Yoni matrix and Vedha from PyJHora (cross-checked), decision on Vashya direction and Rasi 7/0 | Check against Vasudev p.69-70; record in sources and a decision |
| 1 | Summary, statement cards, sticky nav, completeness panel, legend, header and colour-meaning cleanup | UI only, no new data |
| 2 | Disclosure (summary and detail), mobile card tables, focus styles, larger source text | UI only |
| 3 | D9 comparison, facts only | A `MatchPerson` D9 extension and tests |
| 4 | Synastry section (overlays, aspects, Moon-Moon, Venus-Mars), directional | New pure calculator and tests |
| 5 | Timing facts and shared view; cancellation surfacing; print and copy-summary | Sources for verdicts, or stay facts-only |
| 6 | Pair history and notes; saved reports | L4 tables, L5 design |

Branches: Phase 0 on `workstream/cli`, phases 1 to 2 on `workstream/ui`, phases 3 to 4 core on `workstream/cli` and UI on `workstream/ui`. The current Rajju, Stree Deergha and Mahendra change (2026-10-04) is uncommitted on `master` and must move to `workstream/cli` first.

### 10.3 Phase 0 result (2026-10-04)

- Yoni for different animals now uses PyJHora's 14x14 matrix. Cross-checks: its 27-nakshatra animal mapping equals migration 098 on every row; the matrix is symmetric, 4 only on the diagonal, 0 on the seven classical enemy pairs. Conflict with Vasudev: the matrix scores some pairs 1, where the book says passable pairs are 2 to 3. Those pairs show as Absent with that note. The Anuradha/Shravana worked pair goes from a 22-26 range to a single 24.
- Vedha is **not** adopted. PyJHora tests `boy + girl star number in {19, 28, 37}`, which also flags pairs such as Ashlesha-Magha (9+10) that are not Vedha pairs in the Tamil lists. It needs an explicit pair table from a cited source first.
- Still open from the Vasudev gaps: the Vashya direction and the Rasi 7/0 reading.

### 10.4 Phase 1 to 3 result (2026-10-04)

- Phase 1 (summary, section bar, data panel, status labels), phase 2 (folded evidence tables, mobile card rows, focus styles) and phase 3 (Navamsa card) are built on `workstream/ui`; the Rajju, Stree Deergha, Mahendra and Yoni changes and `NavamsaCompatibility` are on `workstream/cli`.
- The Navamsa card is facts only: D9 Lagna and 7th, the D9 7th lord, Venus and Jupiter in D9, the Darakaraka (the D1 chara karaka), and which of the D1 7th lord, Venus, Jupiter and Darakaraka are vargottama. All values are stored positions; nothing is recomputed. Verdicts wait for a cited source.
- Not built: the side-by-side mini D9 chart wheels the audit suggested.

### 10.5 Phase 4 and family layers (2026-10-04)

- **Layers** (migration 170, per viewer, derived and never stored): Core is spouse, parents and children; Extended is grandparents (paternal or maternal by the parent they come through) and grandchildren; Lateral is siblings and, later, uncles and aunts, cousins and in-laws. Siblings were Extended in 168; they are Lateral from 170.
- **Synastry** (any pair): each person's Lagna and grahas in the other's houses, Moon-to-Moon and Venus-to-Mars each way, and same-sign / opposite-sign contacts, all directional and from D1 signs only. Key houses depend on the relationship (spouse 1, 5, 7, 8, 12; child 1, 5; parent 1, 4, 9; sibling 1, 3, 11; none suggested for grandparents or grandchildren) and are a display choice, not a verdict.
- **Pair similarity** (any pair): a list of shared facts (Lagna, Moon, Sun, nakshatra and pada, gana, yoni, nadi, each graha in the same D1 or D9 sign, one Lagna being the other's Moon sign). A count, not a score: base rates (5.3) are not built, so it is not offered as evidence.
- **Compatibility page**: for a recorded non-spouse relationship the page names both people by their roles, shows the layer, and hides Kuta, the dosha balance and the D9 7th-house facts unless asked ("Show marriage factors anyway"). A spouse or an unrecorded pair keeps the marriage view.
- Not built: the full similarity module (feature atoms, pattern search, corpus, 5.1 to 5.4); this is the pair-level slice of it.

### 10.6 Phase 5 result (2026-10-04)

- **Dasha changes ahead**: every Mahadasha and Antardasha change for either person in the next 25 years in date order, naming whose period changes and what the new lord rules and where it sits in that person's chart, plus the windows both people share the same lord at the same level. Facts only: no source classifies a period for a marriage, so no supportive or difficult label is given.
- **Dosha matchup**: the afflictions sorted by kind with a "same in both or neither" column, and Mars and Rahu stored D1 dignity shown beside it. The book's one balancing rule (pp.56-58) remains the only cancellation applied; cancellation by dignity, benefic aspect or dispositor is listed as not applied (no cited source).
- **Copy summary and Print** (removed 2026-10-04, see section 10.8; print moves to the Saved charts print module): the summary as plain text with the date and rule set, and a print of the whole comparison (details opened, default-light, app chrome hidden). `CompatibilityRuleSet.Version` is bumped whenever a matching rule changes; nothing is stored per pair.
- Not built: saved comparisons (L5, phase 6) and any sourced supportive/difficult classification of dasha periods.

### 10.7 Phase 6 results (2026-10-04)

- Migration 171: `tbl_Person_LifeEvent` (marriage now; divorce, child birth, death allowed), `tbl_Pair_Note`, `tbl_Pair_SavedReport`. Reports are saved only for pairs with a recorded relationship; arbitrary pairs are never stored. `PairHistoryRepository` holds marriage date, notes, snapshots and the dasha running on a date.
- Pair page layout: left is the man, right the woman (recorded sex, else the elder on the left); no Swap. A board shows each person's panel beside a Kuta dial (`KutaDial`); the small `PairChart` was replaced on 2026-10-04 by the D1 and D9 grids in section 10.8. Per-person facts sit in a left and right column (see 10.8); only connected facts (Kuta, dosha matchup, synastry, shared dasha windows, coinciding Saturn periods) are combined under "The two together".
- Saved charts: a "Family pairs" table of recorded couples (Left, Right, Kuta, Married, Mahadasha now, Children) opens the pair page. Children are the third group, ready for a family view.

### 10.8 Page revisions (2026-10-04)

- Removed from the page: the "Compare X with" label text, the "(left) is the groom" line, the family note ("Recorded as married..."), the "Each person on their own" heading and its sub-line, and the Copy summary and Print buttons. Printing the comparison belongs to the Saved charts print module. `SummaryText` stays for the saved snapshot.
- Summary of findings is now only the bridge (waterfall) chart of the Ashtakoota total (rammyps, 2026-10-04): each scored Kuta steps up by its points, a Total bar closes it against the 36-point scale ("24 / 36"), and a dashed line marks the pass mark of 18. The Favourable / Concern / Unresolved legend, the three finding columns and the caution line are gone from the page (the findings are still computed for the saved snapshot). An unscored Yoni is left out and noted. Not shown for non-marriage pairs.
- Per-person facts are one set of connected tabs driving the left and right columns together: Charts: D1 and D9, Mars and the malefics, Navamsa (D9) facts, Chara karakas, Life matters: marriage, Saturn from the Moon, Running dasha lords.
- Charts: D1 and D9 use `SindUniDtlGrid` (SIND-UNI-DTL: perfect squares, `Ju` direct / `(Ju)` retrograde, several planets per row; see its spec), fed from the stored D1 and D9 `tbl_Chart_KeyDetails` rows.
- Chara karakas: AK to DK from the stored D1 labels, with D1 sign, dignity, house and D9 sign.
- Life matters: marriage: the Key Inference "Marriage" area read from Lagna in D1 (`MarriageMattersReader`, same `LifeMatterStatistics`), no new rule.
- Helps and hinders: PVR step 5 (Ch. 13) via `TargetInfluences` on the 7th house of each person's own chart. A planet in a quadrant, trine or upachaya from the 7th supports it; in a dusthana from it, as its badhaka, or in the badhaka sign obstructs it; both gives Mixed. The 7th lord and the Darakaraka add Favourable, Concern or Unresolved findings. Nothing classifies how one person's karakas help or hinder the other: no source is cited.


### 10.9 Print module (2026-10-04)

- Saved Charts' Print picker (`PrintModuleDialog`) gains a "Compatibility" group: one option per recorded spouse of the person being printed ("With X (wife)"), ticked by default; the group is hidden when no spouse is recorded. The URL carries the partner ids as `cp=` (`/print/{id}?af=...&ki=...&cp=12`).
- `PrintReport` renders each ticked partner as a sheet: a "Compatibility" heading band, then `<Compatibility Id PrintPartner>`. Print mode drops the controls, jump nav and History and notes card, lays out every per-person tab in turn (instead of one tab at a time) and opens every detail; the page's own print rules still apply.
- Other pairs (siblings, parent and child) are not offered yet; the page itself prints any pair with `PrintPartner`.

### 10.10 Family view: father, mother and child (2026-10-08)

- New page `/family/{father}/{mother}/{child}` (`FamilyTrio.razor`), opened by "Open family" on each child in the Family Core table (Home and Saved charts). A switcher cycles the couple's children.
- Three dasha swimlanes (Mahadasha above, Antardasha below) on one 25-year axis, plus a lane marking the windows where all three run the same lord at the same level (`TrioDashaTimeline.SharedByAll`, Core; the pair rule generalised to any number of people).
- Comparison table: Lagna, Sun, Moon sign, nakshatra, pada, gana, yoni, nadi, running Mahadasha and Antardasha, with the child's matches to either parent marked.
- Similarities: `ChartSimilarity.Compare` for child-father, child-mother and father-mother, plus the facts common to all three. A count of shared facts, not a score (base rates are still not built).
- The swimlane drawing moved into the shared `DashaLanes` component, now used by the pair page and the family page.
