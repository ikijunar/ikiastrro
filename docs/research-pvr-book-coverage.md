---
last_updated: 2026-09-06
reflects: master @ 802e673 + branch feat/graha-dignity-rule-layer (migrations 22–32, unpushed)
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
| 8 | Karakas (79) — chara, sthira, naisargika | `CharaKarakaCalculator` (Ashta) ✓; `tbl_Rule_Karaka` **reserved / empty**; Sthira/Naisargika hard-coded in `LifeAreaMap` only | partial | populate `tbl_Rule_Karaka` from §8; build Sthira + Naisargika karaka engines (Plan 2) |
| 9 | Arudha Padas (85) — AL, bhava arudhas, graha arudhas | `ArudhaCalculator` — AL + 12 bhava arudhas ✓ | partial | reconcile vs §9 (exception rules for the 1st/7th, same-sign/opposite); check whether graha arudhas are wanted |
| 10 | Aspects & Argalas (100) — graha drishti, rasi drishti, argala | `tbl_Rule_AspectOffset` (graha drishti) ✓; **rasi drishti + argala not built** | partial | build rasi-drishti (movable→fixed etc.) + argala + virodha-argala per §10 |
| 11 | Yogas (112) | `tbl_Rule_Yoga` **reserved / empty** | reference-only (empty) | own plan — populate `tbl_Rule_Yoga` (formation predicates + cancellation + result codes) from §11 |
| 12 | Ashtakavarga (145) | **not built**; `_research/jyotishganit` supplies the algorithm | not built | build BAV/SAV + reductions per §12 |
| 13 | Interpreting Charts (166) — synthesis method | `reference-chart-reading-method.md` (partial) | partial | reconcile the reading method against §13 |
| 14 | Longevity (180) — pindayu / nisargayu / amsayu, maraka | `tvf_Chart_SadeSatiPeriods` (unrelated); **ayur methods not built** | not built | build per §14 (+ Part 2 ch 22–23 shoola dasas) |
| 15 | Strength of Planets & Rasis (187) — shadbala, vimsopaka, ishta/kashta | `tbl_Rule_DigBala` ✓; PVR-first `tbl_Rule_ShadbalaComponent` seeded (17), `tbl_Fact_PlanetaryStrength` + component facts added; `tbl_Rule_VimsopakaWeight` remains reserved | partial — Core Shadbala foundation + D1 persistence | Complete calendrical precision, Bhava facts, CLI verification, and Vimsopaka after §6.6 amsabala reconciliation |

## Parts 2–6 — sketch (reconcile after Part 1)

| Part | Chapters | Project state | Note |
|---|---|---|---|
| 2 — Dasa Analysis | 16 Vimsottari ✓ · 17 Ashtottari · 18 Narayana · 19 Lagna Kendradi Rasi · 20 Sudasa · 21 Drigdasa · 22 Niryaana Shoola · 23 Shoola · 24 Kalachakra | only **Vimsottari** built (`VimshottariDashaCalculator`, 3-level) | ch 17–24 not built; Narayana = "most versatile rasi dasa" per PVR |
| 3 — Transit Analysis | 25 Transits & natal references · 26 miscellaneous | `tbl_PlanetSignTransitEvents` + Gochara panel (Sa/Ju/Ra sign-ingress log) — **partial** | §25 techniques (vedha, murti, argala on transits) not built |
| 4 — Tajaka Analysis | 27–31 (varshaphala, muntha, tajaka yogas, patyayini/mudda dasa, sudarsana chakra) | **not built** | whole part |
| 5 — Special Topics | 32 Impact of Birthtime Error · 33 Rational Thinking · 34 Remedial Measures · 35 Mundane · 36 Muhurta · 37 Ethics | **not built** | §32 (birthtime rectification) is the one with engine implications |
| 6 — Real-life Examples | worked charts | — | use as an additional `verify-*` corpus alongside the JHora Ramakrishnan export |

## Reconciliation log

_(append one line per chapter as it is reconciled: date · chapter · what changed · commit)_

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
