---
last_updated: 2026-09-25
reflects: master @ 802e673 + branch feat/graha-dignity-rule-layer (migrations 22–32, unpushed); Ch.12 row refreshed 2026-09-22 against migrations 074–078
---

# PVR book coverage & reconciliation map

**Canonical source (rammyps, 2026-09-04):** P.V.R. Narasimha Rao, *Vedic Astrology: An
Integrated Approach* — cited as `SRC_PVR_INTEGRATED` (`docs/research/reference-sources.md`).
PDF: `D:\Vedic Astrology\Vedic Astology Books\1_PVR_NarasimhaRao.pdf`. Raw text extract:
`D:\@ClaudeSpace\BookExtracts\pvr-integrated-approach-raw.txt` (`pdftotext -layout`, 16 766 lines).

This table is the running record of how each part of the book maps to what the project has
built, where the project already **diverges deliberately**, and what a reconciliation pass
should check. Work is chapter-by-chapter; update the **Status** and **Next** columns as each
is reconciled. PVR's own 2010 "Looking Back" note says he has since refined several
calculations, so the book is the canonical baseline, not infallible — a deliberate divergence
is fine, but it must be recorded at the divergence (a rule-row `CalculationNarrative`, a spec
note) and here.

Status key: **aligned** = built and matches the book · **partial** = built, gaps or
unverified against the book · **diverges** = built but deliberately differs (see note) ·
**reference-only** = rule data seeded, no engine · **not built**.

## Part 1 — Chart Analysis

| Ch | Book section (pg) | Project artifact(s) | Status | Next |
|---|---|---|---|---|
| 1 | Basic Concepts (3) — coordinates, sign notation, dasa overview, panchanga terms | `AstroMath`, `ZodiacName`; no rule table. Panchanga (tithi / nitya-yoga / karana / paksha / lunar month) **not built** — `tbl_PanchangaAtBirth` sketched only (rammyps notes) | partial | build the Panchanga layer (tithi/yoga/karana as separately-computed attributes) |
| 2 | Rasis (21) — 2.2 characteristics, 2.3 indications | `tbl_SignAttributes` + classification/research fields (migr. 19–21) | partial | reconcile the classification + "indications" columns against §2.2/§2.3; `RisingType` still NULL |
| 3 | Planets (28) — 3.2 characteristics, **3.3 dignities (Table 6 + 7 notes)**, 3.4 relationships | `tbl_Rule_GrahaAttribute` (26), `tbl_Rule_GrahaDignity` RuleSetId 2 (23), `tbl_Rule_NaturalRelationship` + `tbl_Rule_CompoundRelationship` (24–25) | partial / diverges | (a) `tbl_Rule_GrahaDignity` RuleSetId 2 vs Table 6 — **verify node own-signs** (migration has Rahu OWN=Aquarius / Ketu OWN=Scorpio; Table 6 print appears to say Rahu OWN=Scorpio); Mars MT typo already handled. (b) `dignity-pvr-integrated.md` §Divergence (Moon/Mercury MT vs `tbl_SignAttributes`) — confirm against the book's notes 2 & 4. (c) `tbl_Rule_GrahaAttribute` was seeded from a *consolidated worksheet* (`graha-characters-pvr.md`), not §3.2 verbatim — reconcile. (d) `DignityEngine` still hard-coded BPHS (Phase 2 deferred) — book alignment needs Phase 2 to flip the active set to RuleSetId 2. |
| 4 | **Upagrahas (41)** | Migration 27: 11 master rows, 5 Sun rules, 112 part rulers, 6 time rules; `SubPlanetCalculator` and `SubPlanetRuleRepository` | aligned (DB + code) | All 11 computed from rule rows and projected into 21 charts. PVR Gulika = Saturn midpoint; Maandi = start. Read-only `verify-upagrahas` passes. Saved charts require regeneration. |
| 5 | Special Lagnas (45) — Bhaava, Hora, Ghati, Sree | migrations 28–30: `tbl_Dim_SpecialLagnas` (4, + usage/varga columns, `LifeAreaId` FK), `tbl_Rule_SpecialLagnaTimeRate` (BL/HL/GL), `tbl_Rule_SpecialLagnaFraction` (SL); taxonomy `SPT_BL/HL/GL/SL` + calc-vocabulary concepts (sa/en). Engines: `HoraLagnaCalculator` ✓ only; Bhaava / Ghati / Sree **not built** | partial (DB layer added) | DB rule layer seeded vs §5.2–5.7 + §5.6 usage. **Varga correlation:** HL→Wealth/D2, GL→Fame-Power/**D5** (corrected from D10 in migration 30 — D5 Panchamsa *is* GL's own signification; D10 stays a secondary read), SL→Wealth (Sudasa, rasi — no varga), BL none. Special lagnas are *reference points* projected into every varga, not charts (§7.1) — the pairing is a reading hint, not a rule. **Divergence:** BL `DegreesPerMinute` = 0.25 (§5.2 stated rate / classical `ishtakāla ÷ 5` / JHora); §5.2's method step + Example 7 imply 1.0 — book erratum, recorded in the BL row narrative (`UsedInBook = 0`). **Next:** build BhaavaLagna / GhatiLagna / SreeLagna engines; fold the taxonomy addenda into `TerminologySeed.cs`. |
| 6 | Divisional Charts (51) — 6.2 computing, **6.3 significations (Table 11)**, 6.4 planes, 6.6 varga grouping & amsabala | migrations 10–13, `tbl_Rule_VargaScheme`, 21 position chart types, `VargaChartComputer`; **migration 30: `tbl_Dim_LifeArea` (20, Table 11) + `tbl_Dim_ChartType.PrimaryLifeAreaId` (all 21 mapped) + §6.4 plane concepts** | partial | §6.3 Table 11 now modelled — every chart type has a `PrimaryLifeAreaId`, `PlaneOfExistence` per §6.4, `WorkspaceGroupCode` rolls the fine areas onto the 4 Web tabs. **Still:** reconcile the 21 varga sign-rules against §6.2; **§6.6 Varga Grouping + Amsabala not built** (feeds Vimsopaka, Ch 15). |
| 7 | Houses (67) — §7.2 significations, §7.3 references, §7.4 special categories | **migration 31:** `tbl_Dim_House` (12-bhava master — Sanskrit name, purushartha §7.4.1, visible/invisible half §7.4.5, Kala Purusha limb §7.2, the 7 §7.4 category bits + `IsMaraka`); `tbl_Dim_HouseCategory` (8 — kendra/trikona/panaphara/apoklima/upachaya/dusthana/chaturasra + maraka, §7.4.6 effect + deity); `tbl_Rule_HouseSignification` **populated** (121 rows from §7.2, RuleSetId 1, `SRC_PVR_INTEGRATED`; + `SignificationText`/`SignificationCategory`/`DisplayOrder`, `RuleSetId` INT→TINYINT+FK); `tbl_Dim_HouseAttribute`(3)/`tbl_Rule_HouseAttribute`(24 — `GENERAL_CHARACTER` + `CATEGORY_EFFECT`) mirror the graha attribute pair (migr. 26); taxonomy `Category 'HouseCategory'` + 8 `HCAT_*` + 4 `PURUSHARTHA_*` + 2 `ZHALF_*` concepts (sa/en, addendum). **migration 32:** `tbl_Dim_HouseReference` (17 — Lagna, Chandra/Ravi/Paaka/Arudha/Karakamsa lagnas, Ghati/Hora/Bhaava/Sree lagnas, 7 graha lagnas; each with its §7.3 "perspective" + basis + Saturn-transit note); `tbl_Rule_HouseReferenceMatter` (§7.3.9 Table 12, RuleSetId 1); **empty `tbl_Fact_HouseFromReference`** (narrow star-schema — new reference = a dim row, no schema change; `tbl_Chart_KeyDetails.HouseNumberFrom{Lagna,Sun,Moon}` unchanged); `tbl_Rule_HouseAttribute` += 14 `NATURAL_SIGNIFICATOR` rows (house→Table 12 karaka); taxonomy `Category 'HouseReference'` + 6 `HREF_*` + `HREF_GRAHA_LAGNA` (sa/en). `reference-house-lagna-significations.md` (Raman) kept as a cross-check | §7.2/§7.3/§7.4 modelled (migr. 31–32) | fold the migr. 29–32 taxonomy addenda into `TerminologySeed.cs`; build the C# engine that fills `tbl_Fact_HouseFromReference` (house-of-subject-from-reference); reconcile `LifeAreaMap.cs` house lists against `tbl_Dim_House` / `WorkspaceGroupCode` when the UI piece starts; §7.5 whole-sign controversy needs no DB |
| 8 | Karakas (79) — chara, sthira, naisargika | `CharaKarakaCalculator` (Ashta) ✓; `tbl_Rule_Karaka` **reserved / empty**; **naisargika seeded** — migration 086: `tbl_Rule_Naisargika_Karakatwas` (34, full grid) + `tbl_Rule_Naisargika_Karakas` (12, primary per-house reduction), dedicated tables (not `tbl_Rule_Karaka`); Sthira still hard-coded in `LifeAreaMap` (different source, B.V. Raman, unreconciled) | partial | populate `tbl_Rule_Karaka` (chara/sthira) or fold sthira into its own dedicated table on the naisargika pattern; build a Naisargika karaka read engine (Plan 2) |
| 9 | Arudha Padas (85) — AL, bhava arudhas, graha arudhas | `ArudhaCalculator` (AL + 12 bhava arudhas) and `GrahaArudhaCalculator` (9 graha arudhas), computed **inside each divisional chart** from its own placements (§9.2/§9.5, 2026-10-01), stronger co-lord for Sc/Aq (`StrongerCoLord`, §15.5.1); graha arudhas pick the stronger own sign with `StrongerRasiComparator` (§15.5.2), where Sc/Aq count both co-lords in rule 2 (Exercise 26) and use the stronger co-lord in rules 4 and 6; CLI `verify-varga-arudha` matches JHora's AL in all 59 shared charts of the three reference exports | built | — |
| 10 | Aspects & Argalas (100) — graha drishti, rasi drishti, argala | `tbl_Rule_AspectOffset` (graha drishti) ✓; `RasiDrishtiCalculator` + `tbl_Rule_RasiDrishti` (migration 107, 2026-09-17) ✓; **argala + virodhargala built 2026-09-19** — `ArgalaCalculator` + `tbl_Rule_Argala` (migration 127) + `tbl_Fact_Argala` (migration 128) + `ArgalaFactBuilder`/`ArgalaFactRepository` + `backfill-argala` CLI mode, verified against Exercise 16/Chart 5 (`ArgalaCalculatorTests`/`ArgalaFactBuilderTests`/`ArgalaNaturalMaleficTests`, 28/28 pass) and hand-checked against real saved-chart data, including Moon/Mercury's Conditional benefic/malefic status matching `tbl_Planets.NaturalNature` | aligned (engine + DB, rule + fact layers); **wired into `ChartGenerationService`** (`GenerateAll`/`RecomputeAnalytics` rewrite `tbl_Fact_Argala`) and shown in the UI — Astro Facts → Natal → About Houses (`ArgalaTable`, rasi drishti under Signs & Planet Aspects) and Key Inference → "Help or hindrance from other planets" | see [`argala-virodhargala-drishti-lifematters.md`](argala-virodhargala-drishti-lifematters.md) §4; remaining gap is assembling every influence on a matter's target house (drishti + argala + baadhaka) in one Key Inference view |
| 11 | Yogas (p.112) — §11.2–11.10, 98 named headings + 59 numbered combinations | `PvrChapter11YogaEvaluator` + `PvrChapter11NumberedYogaEvaluator`; migration 137 adds hierarchical sets, 68 source-variant inventory rows and 18 Chara-Karaka tags; migration 138 adds Basic Raja, Hari, Hara, the first Brahma/Trimurti form and Kalpadruma/Parijata with D1/D9 applicability | near-complete — named and numbered predicates transcribed; 1 `NOT_EVALUATED` (Daridra 10 Ashtakavarga forward-reference) and 1 partial alternative (Pisces Dhana damaged text) | establish the Daridra 10 source rule and repair Pisces Dhana from a clean scan; then complete magnitude qualifications and UI grouping |
| 12 | Ashtakavarga (145); cross-varga application confirmed p.155 Example 39 | Built — `FEAT-ASHTAKAVARGA-01` (migrations 074–078, `AshtakavargaCalculator`); `verify-ashtakavarga` reproduces the JHora export for `1_Ramakrishnan` exactly, Phase 6 (2026-09-23) proves the per-varga recompute genuinely differs per chart | Verified·80% | UI table over `vw_ChartAshtakavarga` (`Web [ ]`); cross-varga application already shipped and now cited (Example 39, Vajpayee D-10) — see [[ashtakavarga-varga-extension]]; only its comparison chart remains unbuilt Web work |
| 13 | Interpreting Charts (166) — synthesis method | `../../cli/reading/method.md` (partial); **step 5 built 2026-10-01** — `TargetInfluences` (Core) + Key Inference "What acts on it": graha/rāśi dṛṣṭi, Argala, position from the target (quadrant/trine/upachaya/dusthāna), bādhaka §13.3, functional nature §13.2, house or its Āruḍha pada (step 4 as a toggle) | partial | step 4 per-matter Āruḍha choice needs sourced data; step 6 (standard placement results) and yogada not built; reconcile `method.md` against §13 |
| 14 | Longevity (180) — pindayu / nisargayu / amsayu, maraka | §14.3 Rudra, Trishoola, Maheswara (`RudraMaheswara`), §14.4 Method of Three Pairs and §14.5 Eighth Lord Method (`LongevityMethods`), both Core, shown on Astro Facts About Planets (`RudraMaheswaraTable`); checked against Example 47 and Exercise 23. Pindayu / nisargayu / amsayu and the maraka list **not built** | partial | pindayu/nisargayu/amsayu (14.1-14.2); Part 2 ch 22–23 shoola dasas |
| 15 | Strength of Planets & Rasis (187) — shadbala, vimsopaka, ishta/kashta | `tbl_Rule_DigBala` ✓; PVR-first `tbl_Rule_ShadbalaComponent` seeded (17), `tbl_Fact_PlanetaryStrength` + component facts added; §6.6 amsabala reconciled (migration 99): `tbl_Rule_AmsabalaGroup`/`tbl_Rule_AmsabalaName` + `AmsabalaCalculator`, verified against PVR's own Example 27 (Bill Cosby/Jupiter); `tbl_Rule_VimsopakaWeight` seeded from BPHS (migration 157, four schemes) + `VimsopakaCalculator`, shown live in Astro Facts step 6 (2026-10-01) | partial — Core Shadbala foundation + D1 persistence + amsabala + vimsopaka | Complete calendrical precision, Bhava facts, CLI verification |

## Parts 2–6 — sketch (reconcile after Part 1)

| Part | Chapters | Project state | Note |
|---|---|---|---|
| 2 — Dasa Analysis | 16 Vimsottari ✓ · 17 Ashtottari ◐ · 18 Narayana ✓ · 19 Lagna Kendradi Rasi ✓ · 20 Sudasa ✓ · 21 Drigdasa ✓ · 22 Niryaana Shoola · 23 Shoola · 24 Kalachakra | **Vimsottari** built (`VimshottariDashaCalculator`, 3-level, stored). **2026-10-06, computed live on Astro Facts step 9 (PHALITA DASAS), nothing stored:** `RasiDasa` (Core, `Engines/Dasha`) for Narayana, Lagna Kendradi, Sudasa and Drigdasa, with Narayana antardasas (§18.3) and `NarayanaProgression` checked against all 36 sequences of Table 40 and Examples 63-65, 66, 67, 76, 77, 79, 80; `PhalitaDasaReader` applies PVR's own interpretation rules (§18.4, §19.4, §20.3, §21.3); `AshtottariDashaCalculator` (ch.17 periods) with `AshtottariApplicability` (§17.2.3's three views). Not built: Narayana of divisional charts, Sudasa/Kendradi/Drig antardasas (the book gives periods only), the Saturn-and-Ketu-together case, raja/dhana yogas from the dasa lagna, parivraja yogas, AmK argala | ch 22–24 not built; Narayana = "most versatile rasi dasa" per PVR |
| 3 — Transit Analysis | 25 Transits & natal references · 26 miscellaneous | `tbl_PlanetSignTransitEvents` + Gochara panel (Sa/Ju/Ra sign-ingress log) — **partial**. Ch. 26.3 Table 63 (Rasi Gochara Vedha) transcribed for reference: [`gochara-vedha-pvr.md`](gochara-vedha-pvr.md) (2026-09-18) | Table 63 seeded as `tbl_Rule_GocharaVedha` (migration 158) + `GocharaVedhaCalculator`; shown in Astro Facts step 2 as `GocharaTable` (2026-10-01: house from the Moon, verdict, Vedha, own BAV bindus, Saturn phase). Rāhu/Ketu **do** cause Vedha (decided 2026-10-01): §26.3 says a good transit is obstructed by "another planet transiting in the vedha sthana" and its only exceptions are the father–son pairs (Sun–Saturn, Moon–Mercury); Table 63 limits which planets' transits can be blocked (the seven), not which planets block; §26.4 likewise names Rāhu and Ketu among the malefics causing vedha. A seven-planets-only reading belongs to other sources and would need its own cited rule set. §25 techniques (murti, argala on transits) still not built |
| 4 — Tajaka Analysis | 27–31 (varshaphala, muntha, tajaka yogas, patyayini/mudda dasa, sudarsana chakra) | **not built** | whole part. **Harsha Bala** (Tajaka strength: 5 virupas for each of four conditions — happy house, own-gender sign, day/night, exaltation-type dignity) is a gap here, not in Shadbala: it is not a BPHS Sthana sub-component, so `ShadbalaCalculator` correctly omits it and must not add it to the totals (that would break the JHora match). Checked 2026-10-07; rules for the day/night and dignity conditions are unsourced in `docs/research` — get the PVR ch.27–31 / Tajika Neelakanthi page before building, as part of the Varshaphala module |
| 5 — Special Topics | 32 Impact of Birthtime Error · 33 Rational Thinking · 34 Remedial Measures · 35 Mundane · 36 Muhurta · 37 Ethics | **not built** | §32 (birthtime rectification) is the one with engine implications |
| 6 — Real-life Examples | worked charts | — | use as an additional `verify-*` corpus alongside the JHora Ramakrishnan export |

## Reconciliation log

_(append one line per chapter as it is reconciled: date · chapter · what changed · commit)_

- 2026-09-17 — Ch 11 (Yogas), same day as the P0 closure below: transcribed all 59 unnamed
  numbered combinations (§11.7.3 "More Raja Yogas" ×18, §11.8 Raaja Sambandha ×15, §11.9 Dhana
  ×13, §11.10 Daridra ×13) in new `PvrChapter11NumberedYogaEvaluator.cs`, wired into
  `ProductionYogaEngine.cs`, 60 tests in `PvrChapter11NumberedYogaEvaluatorTests.cs`. New
  umbrella codes `YOGA_RAJA_ADVANCED` / `YOGA_RAJA_SAMBANDHA`; Dhana/Daridra items ride as extra
  `SourceVariantCode` rows on the existing `YOGA_DHANA` / `YOGA_DARIDRA` codes. Discovered while
  building this that Ghati/Sree/Bhaava Lagna (which `pvr-coverage.md`'s Ch.5 row had marked "not
  built") were actually completed 2026-09-13 — this doc's Ch.5 row was stale, not the code; not
  yet corrected here pending a dedicated Ch.5 reconciliation pass. Also fixed a real
  null-reference bug in a shared `Dispositor()` helper (see `../yoga-corpus.md` for detail).
  Full suite: 402/404 (2 pre-existing unrelated `PolarGridLagnaSelectTests` failures only).
- 2026-09-17 — Ch 11 (Yogas): implemented all 9 P0 gap yogas confirmed by the 2026-09-13 pass
  — `YOGA_MAALA`, `YOGA_SUBHA`, `YOGA_ASUBHA`, `YOGA_GURU_MANGALA`, `YOGA_CHAMARA`,
  `YOGA_KHADGA`, `YOGA_LAGNAADHI`, `YOGA_SAARADA`, `YOGA_DHARMA_KARMADHIPATI` — in new
  `PvrChapter11YogaEvaluator.cs`, wired into `ProductionYogaEngine.cs`, 10 tests in
  `PvrChapter11YogaEvaluatorTests.cs`. Raises PVR named-yoga coverage to ~96/98 (~98%). See
  `../yoga-corpus.md` "P0 closure notes" for the two predicates needing a specific reading
  choice (`YOGA_LAGNAADHI`'s Lagna-vs-Moon reckoning, `YOGA_DHARMA_KARMADHIPATI`'s reusable
  3-way association helper). Full suite: 342/344 (2 pre-existing unrelated
  `PolarGridLagnaSelectTests` failures only).
- 2026-09-13 — Ch 11 (Yogas): row corrected — `tbl_Rule_Yoga` was **not** empty (stale note
  from before migrations 47–51/079). Counted the chapter directly against the raw extract:
  ~98 named yogas across §11.2–11.7.1 + 58 unnamed numbered combinations across §11.7.3/11.8/
  11.9/11.10 (Raja-continuation/Raja-Sambandha/Dhana/Daridra). Cross-checked all ~98 named
  yogas against `db/079_add_yoga_type_and_rule.sql`'s 146-code predicate list and the engine
  source directly (not just docs): ~87/98 covered (shared `SRC_RAMAN_300_COMBINATIONS`
  corpus). Confirmed missing: Maalaa Yoga (§11.5.2 Dala — not previously tracked anywhere,
  added to `yoga-corpus.md`'s P0 list this pass), Subha, Asubha, Guru-Mangala, Chamara,
  Khadga, Lagnaadhi, Saarada, Dharma-Karmadhipati (already tracked as P0 in `yoga-corpus.md`),
  and Hari/Hara as standalone codes (currently merged into `YOGA_HARIHARA_BRAHMA`). The 58
  unnamed combinations have no per-rule transcription yet — `yoga-corpus.md` now itemises them
  instead of the previous one-line "transcribe after P0" note.
- 2026-09-04 — Ch 4 (Upagrahas): migration 27 rebuilt to the book's Table 10 + §4.3 rise
  points (`tbl_Rule_SubPlanetPartRuler` + `EIGHTH_PART_RULER`), commit `a3c9225`. DB aligned;
  `UpagrahaCalculator.cs` Gulika/Maandi start-vs-middle still on the JHora convention — open.
- 2026-09-04 — Ch 5 (Special Lagnas): migration 28 adds the DB rule layer —
  `tbl_Dim_SpecialLagnas` (4) + `tbl_Rule_SpecialLagnaTimeRate` (Bhaava/Hora/Ghati, one
  `DegreesPerMinute` each: 0.25 / 0.5 / 1.25) + `tbl_Rule_SpecialLagnaFraction` (Sree =
  natal lagna + Moon's nakshatra fraction × 360). BL seeded 0.25/min per §5.2's stated rate
  (its method step + Example 7 give a contradictory 1.0 — recorded as a book erratum in the
  row narrative). HL row pins the shipped `HoraLagnaCalculator.cs` 0.5. Bhaava/Ghati/Sree
  engines still to build. verify-rules / verify-sources / verify-schema / verify-jaimini ALL PASS.
- 2026-09-04 — Ch 5 (Special Lagnas), migration 29: `tbl_Dim_SpecialLagnas` gains 5
  usage/correlation columns (`LifeAreaFocus`, `UsageContext`, `HouseReferenceScope`,
  `DasaLinkage`, `RelatedVargaChartId` → FK `tbl_Dim_ChartType`) backfilled for all 4 rows
  from §5.6 / §5.5 / §7.1. Varga pairing recorded as **soft/interpretive** (§6.3): HL→D2,
  GL→D10, SL→NULL (Sudasa), BL→NULL. Taxonomy: 9 concepts (`SPT_BL/HL/GL/SL` + calc /
  anchor / basis vocabulary) + 18 sa/en text rows, seeded as a hand-maintained ADDENDUM
  after the generated `TERMINOLOGY SEED` block — **`TerminologySeed.cs` does not yet emit
  these; fold on its next pass.** From-empty rebuild clean; verify-terminology / -rules /
  -sources / -schema / -jaimini ALL PASS.
- 2026-09-04 — Ch 7 (Houses), migration 32: §7.3 reference points. `tbl_Dim_HouseReference`
  (17 rows — Lagna + Chandra/Ravi/Paaka/Arudha/Karakamsa lagnas + Ghati/Hora/Bhaava/Sree
  lagnas + 7 graha lagnas; `BasisKind` Ascendant/Planet/SpecialLagna/LagnaLord/Arudha/
  KarakaInVarga, `BasisPlanetId`/`BasisSpecialLagnaId` resolved by join, `Perspective` from
  §7.3, `SaturnTransitEffect` where the book gives one, `AppliesInVarga` Any/Navamsa/Rasi).
  `tbl_Rule_HouseReferenceMatter` = PVR Table 12 (§7.3.9), 14 rows, RuleSetId 1. Empty
  `tbl_Fact_HouseFromReference` — narrow star-schema (`ChartResultId` + `RuleSetId` +
  `ReferenceCode` + `SubjectKind`/`SubjectKey`(+`SubjectPlanetId`) + `ChartTypeId` →
  `HouseNumber`/`SignId`); the agreed answer to "keep house-from-X in a Dim or in KeyDetails":
  **neither** — a Fact table; `tbl_Chart_KeyDetails.HouseNumberFrom{Lagna,Sun,Moon}` stay as
  the common read path. `tbl_Rule_HouseAttribute` += 14 `NATURAL_SIGNIFICATOR` rows (house→
  Table 12 karaka, the inverse view; houses 9 & 11 get Sun P1 / Moon P2). Taxonomy: `Category
  'HouseReference'` + 6 named `HREF_*` + `HREF_GRAHA_LAGNA` + 14 sa/en text (addendum; Ghati/
  Hora/Bhaava/Sree reuse the migr. 29 `SPT_*` concepts). Ledger `Note` fit this time. From-empty
  rebuild clean (17 / 14 / fact / 14 natsig / catalog 23/23 / 7 concepts); verify-schema /
  -sources / -rules / -terminology / -dignity / -avastha ALL PASS.
- 2026-09-04 — Ch 7 (Houses), migration 31: house model on the graha pattern. `tbl_Dim_House`
  (12-bhava master, 1:1 invariant facts + the seven §7.4 category bits + `IsMaraka`);
  `tbl_Dim_HouseCategory` (8 rows, §7.4.6 quick-summary effect + presiding deity). Reserved
  `tbl_Rule_HouseSignification` **populated** — 121 bhava karatvas from §7.2, one row per
  matter, categorised Matter/Person/BodyPart/DerivedHouse, RuleSetId 1, `SRC_PVR_INTEGRATED`;
  `RuleSetId` tightened INT→TINYINT + FK, + `SignificationText`/`SignificationCategory`/
  `DisplayOrder`. `tbl_Dim_HouseAttribute` (3) + `tbl_Rule_HouseAttribute` (24: `GENERAL_CHARACTER`
  ×12 + `CATEGORY_EFFECT` ×12) mirror `tbl_Dim_GrahaAttribute`/`tbl_Rule_GrahaAttribute`;
  `NATURAL_SIGNIFICATOR` catalogued, seeded in migr. 32 from Table 12. Taxonomy: `Category
  'HouseCategory'` added to the CK; 8 `HCAT_*` + 4 `PURUSHARTHA_*` + 2 `ZHALF_*` concepts + 28
  sa/en text rows (addendum — not in `TerminologySeed.cs` yet). §7.3 reference points deferred
  to migration 32. From-empty rebuild clean; verify-schema / -sources / -rules (22/22 catalog) /
  -terminology / -dignity / -avastha ALL PASS.
- 2026-09-04 — Ch 5 / Ch 6 (life-area taxonomy), migration 30: PVR Table 11 (§6.3) modelled
  as `tbl_Dim_LifeArea` (20 spheres, `PlaneOfExistence` per §6.4, `WorkspaceGroupCode` rolling
  onto the 4 planned Web tabs). `tbl_Dim_ChartType += PrimaryLifeAreaId` — all 21 chart types
  mapped (D2-US shares D2's Wealth). `tbl_Dim_SpecialLagnas.LifeAreaFocus` (free text, migr.
  29) → `LifeAreaId` FK; Ghati Lagna's `RelatedVargaChartId` corrected **D-10 → D-5** (D-5
  Panchamsa = GL's own "fame, authority and power" signification; D-10 kept as a secondary
  read in `UsageContext`). Taxonomy `Category 'LifeArea'` added to the CHECK; 20 `LIFEAREA_*`
  + 4 `PLANE_*` concepts + 48 sa/en text rows (addendum, not in `TerminologySeed.cs` yet).
  From-empty rebuild clean; verify-terminology / -rules / -sources / -schema / -jaimini /
  -vargas ALL PASS.

- 2026-09-06: Implemented all eleven upagrahas from migration 27. Seven calculation tests and six existing UI tests pass; `verify-upagrahas` passes for all 21 chart types. Historical JHora naming is superseded for new computations; existing saved charts were not rewritten.
- 2026-09-12 — Ch 8 (Karakas), migration 086: naisargika karaka seeded as two new dedicated
  tables (rammyps's worksheet, `naisargika-karaka-pvr.md`) — `tbl_Rule_Naisargika_Karakatwas`
  (34 rows: graha → matter → house, the full grid) and `tbl_Rule_Naisargika_Karakas` (12 rows:
  house → primary graha + matters summary, a reduction of the grid). Deliberately **not** the
  reserved generic `tbl_Rule_Karaka` (migration 18), which stays empty for a future chara/sthira
  layer. 2 `tbl_Rule_Catalog` rows. Chara karaka stays algorithmic (`CharaKarakaCalculator`,
  not a rule table); sthira karaka stays hard-coded in `LifeAreaMap.cs` from a different source
  (B.V. Raman) and is not reconciled against this migration. Applied to dev with `sqlcmd -f 65001`
  (needed for the Nyāya/Mokṣa diacritics — without it sqlcmd mis-decodes them); verify-rules
  (42/42 tbl_Rule_Catalog coverage) / verify-schema ALL PASS.

- 2026-09-14 — Research pass (no migration yet), `chara-karaka-life-area-pvr.md`: worked
  through rammyps's "chart → primary subject → chara karaka" worksheet against Table 13
  (ch. 8, pg 80–81, karaka → persons shown) and Table 11 (§6.3, chart → life area).
  Chara karaka has zero rule-table presence beyond 087's two PVR_DIRECT rows (DK/spouse,
  PK/child) — never cross-referenced against Table 11. Of the worksheet's 18 chart+subject
  pairs: 2 already match existing 087 rows, ~12 collide with an existing single-naisargika-
  graha row on the same matter (same shape as the DK/PK case — likely wants a second row,
  not a second field, per that precedent), 4 have no matching row at all. Also found: the
  worksheet's D-40 ("maternal lineage") and D-45 ("paternal lineage") don't match PVR's own
  Table 11 wording for those charts (auspicious/inauspicious events; all matters) — that
  lineage reading is a real classical convention but not sourceable to `SRC_PVR_INTEGRATED`
  as-is. Sthira karaka confirmed to have no DB presence and a second, unreconciled
  B.V.-Raman-sourced list already in `LifeAreaMap.cs`. No migration written; decisions on
  table shape (new purpose-built table vs. more 087 rows) and the D-40/D-45 sourcing
  question are open.

- 2026-09-18 — Ch 26.3 (Transit Analysis, Part 3), reference capture only, no migration:
  a pasted Vedha (transit-obstruction) table was checked against Naisargika Karaka — confirmed
  unrelated (static significator assignment vs. dynamic transit technique) and confirmed the
  pasted numbers were not PVR's own (a different, OCR-garbled, unidentified source). Transcribed
  PVR's real Table 63 (Vedha Sthaanas, pg 347–348) instead, into
  [`gochara-vedha-pvr.md`](gochara-vedha-pvr.md). No Sun or Rahu/Ketu row exists in the extract
  for this table — flagged as a gap, not filled by guess.

- 2026-09-19 — Ch 10 (Argalas): built end to end. Pinned the citation (§10.5–10.6, pp.104–107)
  and transcribed the position rule table + Exercise 16's worked answer for Chart 5 (pp.110–111)
  into `argala-virodhargala-drishti-lifematters.md` §4 — the book's own OCR'd answer-key grid was
  too scrambled to transcribe verbatim, so Chart 5's positions were reconstructed instead from
  Exercise 14's (graha-dṛṣṭi) answer table plus the explicit "Ketu is in Aq" note, then
  cross-checked against the still-legible OCR rows for houses 1, 4, 10, 11, 12 (exact match,
  including the 3rd-house malefic exception on house 11). Resolved the two remaining open design
  questions (occupant-strength comparator, target shape) and built `ArgalaCalculator.cs`
  (`src/Ikiastrro.Core/Engines/Houses/`) + `db/127_create_rule_argala.sql` (`tbl_Rule_Argala`, 8
  rows) + `ArgalaCalculatorTests.cs` (8/8 pass, Exercise 16/Chart 5 fixture). `verify-rules`
  clean (no new failures); full `Ikiastrro.Web.Tests` suite 414/416 (the 2 failures are the
  pre-existing unrelated `PolarGridLagnaSelectTests`). Not yet wired into the chart-analysis
  pipeline or UI — the calculator is a standalone, tested primitive with no caller yet.

- 2026-09-19 — Ch 10 (Argalas), same day, fact layer: added `tbl_Fact_Argala` (migration 128,
  not cataloged in `tbl_Rule_Catalog` — that's `tbl_Rule_*`-only, `tbl_Fact_HouseFromReference`
  migration 32 sets the precedent), `ArgalaFactBuilder.cs` (flattens `ArgalaCalculator` into fact
  rows for all 12 houses + all 9 grahas' own signs), `ArgalaFactRepository.cs` (Dapper
  delete-then-reinsert, `BhavaStrengthRepository` pattern), and a `backfill-argala` CLI mode
  (reads `tbl_Chart_KeyDetails` Graha rows per D1 chart — standalone, not yet threaded through
  `ChartGenerationService`). Ran against the 3 real D1 charts in dev: 120/104/124 rows written
  (72 house rows each, an exact-match sanity invariant: 9 placed grahas × 8 positions). Chart
  197's house-1 rows (Lagna Cancer) hand-verified against its actual positions, all 6 correct;
  4–6 sec.10.6 exception rows fired per chart on real data. `ArgalaFactBuilderTests.cs` (4 tests)
  added, full Argala test set 12/12. In passing, found `tbl_Planets.NaturalNature` already
  carries a more precise sourced Benefic/Malefic/**Conditional** classification (Moon, Mercury)
  than `ArgalaCalculator`'s always-benefic simplification for those two — noted as a real
  follow-up, not acted on (needs paksha/conjunction inputs the calculator doesn't take today).

- 2026-09-19 — Ch 10 (Argalas), same day, closed the Moon/Mercury follow-up above: replaced
  `ArgalaCalculator.NaturalMalefics` (static 5-planet set) with `IsNaturalMalefic(planet,
  occupancy)`, matching `tbl_Planets.NaturalNature`'s Conditional status for Moon (malefic when
  waning — whole-sign approximation, >6 signs ahead of the Sun) and Mercury (malefic when
  conjunct a fixed malefic — a conjunction-only reading of `ConditionalRule`'s text). 9 new tests
  (`ArgalaNaturalMaleficTests.cs`, fixed planets + both Moon phases + the same-sign boundary +
  Mercury conjunct-malefic/alone/conjunct-only-benefics + both missing-input fallbacks +
  an end-to-end exception-via-waning-Moon case). Re-ran `backfill-argala`; row/exception counts
  on the 3 real dev charts were unchanged (no 3rd-position occupant in those particular charts
  happened to flip), but spot-checked that Mercury's Conditional status applies consistently
  within a chart (malefic at every appearance in chart 197, benefic at every in chart 241).
  Full Argala test set 28/28.
