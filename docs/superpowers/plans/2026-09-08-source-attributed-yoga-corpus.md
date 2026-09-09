---
last_updated: 2026-09-08
---

# Source-attributed Raman and PVR yoga corpus

**Status:** Active

## Objective

Implement B. V. Raman's numbered 1–300 combinations and the yoga definitions in
P. V. R. Narasimha Rao's *Vedic Astrology: An Integrated Approach* as independently
attributed, executable rule variants. A shared yoga name does not imply a shared predicate.

## Evidence acquired

- `SRC_RAMAN_300_COMBINATIONS`: user-supplied local 352-page DJVU. Full OCR completed
  2026-09-08 with 352/352 page jobs recorded `OK`; draft:
  `D:\@ClaudeSpace\BookExtracts\300-important-combinations_p1-352_draft.md`.
- Raman scan page 2 identifies the ninth edition (1983), tenth edition (1991), Delhi
  reprint (1994), and ISBNs 81-208-0843-6 / 81-208-0850-9.
- `SRC_PVR_INTEGRATED`: registered local extract
  `D:\@ClaudeSpace\BookExtracts\pvr-integrated-approach-raw.txt`, chapter 11,
  printed pp. 112 onward. PVR recommends Raman's compendium but gives his own definitions.

OCR is discovery evidence only. Before activation, each formation, qualification,
cancellation, printed page, and scan page must be checked against the rendered source page.

## Identity and provenance

- `YogaCode`: normalized concept identity, such as `YOGA_GAJAKESARI`.
- `SourceRefCode`: source work (`SRC_RAMAN_300_COMBINATIONS` or `SRC_PVR_INTEGRATED`).
- `SourceVariantCode`: work-specific formula, preserving disagreements.
- `SourceCorpusCode`: top-level collection: `BVR-300` for Raman's numbered
  combinations, `PVR-SPECIFIC` for PVR-authored variants, and `OTHERS` for
  independently sourced additions outside those two collections.
- `SourceEntryNumber`: Raman's printed 1–300 number; NULL for PVR.
- `SourceLocator`: exact compact locator, e.g. `combination 1; printed p.13; scan p.20`.
- `RequirementJson`: executable predicates only.
- `CancellationJson`: explicit exceptions, loss-of-power rules, and qualifications.
- `CalculationNarrative`: concise paraphrase of interpretation, not copied prose.

## Additional evaluation-input requirements

Yoga formation cannot always be decided from D1 planetary sign/house occupancy alone.
Track these requirements independently from formation family, outcome nature, and
source-stated strength so that missing inputs produce `NOT_EVALUATED`, never a false
`ABSENT`.

| Priority | Requirement | Needed for current corpus | Required behavior |
|---|---|---|---|
| P0 | Chart applicability | Every variant; D9 currently required by Raman 28, 29, 46, 54 Navamsa, 57, 62, 66, and 68 | Resolve required charts before evaluation; list every missing chart |
| P0 | Exact planetary longitude | Raman 58 Jaya and 59 Vidyut (“deep exaltation”) | Do not substitute exaltation sign or a default midpoint when longitude is absent |
| P0 | Birth day/night status | Raman 25 Mahabhagya and 66 Garuda | Derive from astronomical sunrise/sunset when birth time/location are available; otherwise require explicit context |
| P0 | Lunar phase status | Raman 66 Garuda (waxing) and 68 Gola (full Moon) | Derive consistently from Sun–Moon elongation; retain the computed phase and threshold provenance |
| P0 | Subject sex specified by source | Raman 25 Mahabhagya | Preserve Raman's male/day/odd and female/night/even branches; unspecified sex remains unevaluated |
| P1 | Reference frame | Lagna, Moon, Sun, a named planet/lord, or multiple frames | Store explicitly; “kendra” without another stated origin means from Lagna |
| P1 | Dignity/strength qualification | Yogas using strong, weak, exalted, debilitated, moolatrikona, friendly, or powerful | Keep formation match separate from qualification/strength state |
| P1 | Aspect model | Rules using conjunction or graha drishti | Record aspect authority/model and do not silently substitute sign aspect |
| P2 | Source ambiguity/alternative | Conflicting definitions, remarks, or attributed alternatives | Preserve separate `SourceVariantCode` rows and explanatory notes |

### Existing capability audit

Do not create duplicate yoga-only inputs for capabilities already present:

- `ChartBundle.Strengths` contains per-planet total Ṣaḍbala and all six top-level
  bala totals, including `KalaBalaVirupas`; component rows and strength facts are
  already persisted by migration 39.
- “Strongest planet” can be ranked directly by `ShadbalaVirupas`.
- `ChartBundle.SunTimes.IsNightBirth` supplies the day/night decision.
- D1 and varga positions retain exact sidereal longitude.
- The full registered varga set and its sign-rule implementations are available as
  inputs, including D9 and the Shodashavarga prerequisite charts.
- Existing dignity, conjunction, house-lord, and graha-aspect calculations should be
  composed rather than reimplemented inside each yoga family.

Genuine gaps to prioritize:

1. **P0 — Yoga strength policy:** seed authoritative planet-specific minimum Ṣaḍbala
   rūpas and map source words such as “strong”, “weak”, and “powerful” to an explicit,
   versioned test. The database column exists, but no thresholds are populated.
2. **P0 — Vaiśeṣikāṃśa:** implement and persist the grades needed by Raman 130, 137,
   139, 140 and later rules. The Vimśopaka interface and rule table are placeholders
   only; no calculator currently produces these results.
3. **P0 — Lunar phase classification:** centralize Sun–Moon elongation into
   waxing/waning and source-qualified full-Moon states. Pakṣa Bala currently computes
   an internal phase quantity but does not publish reusable phase state.
4. **P0 — Subject sex input:** add an optional, audited value to `BirthDetails` and
   propagate it into `ChartBundle`; do not infer it from name or sign attributes.
5. **P1 — Complete strength arithmetic:** implement the seeded but absent
   Tribhāga/Varṣa/Māsa/Dina/Horā/Ayana Kālabala components, planetary-war adjustment,
   and reconciled Iṣṭa/Kaṣṭa and Cheṣṭā calculations.
6. **P1 — Structured missing requirements:** return stable missing-input codes instead
   of relying only on free-text `NOT_EVALUATED` notes.

### Input-requirement tracking model

- `tbl_Rule_YogaChartApplicability` owns normalized chart inputs with
  `FOUNDATION`, `REQUIRED`, or `CONFIRMATORY` roles.
- A later contextual-requirement rule table should track non-chart inputs with stable
  codes such as `BIRTH_DAY_NIGHT`, `MOON_WAXING`, `MOON_FULL`,
  `SUBJECT_SEX`, and `EXACT_LONGITUDE`.
- Runtime results must report `EvaluationStatus`, all missing requirement codes,
  and the source variant evaluated.
- Derived context must retain calculation provenance: ayanamsa/rule set where
  relevant, phase threshold, and sunrise/sunset method.
- Requirements belong to a source variant, not merely to `YogaCode`, because two
  authorities may define the same named yoga using different inputs.

## Delivery gates

1. Register the Raman source and prepare source-variant fields.
2. Build a 300-row Raman ledger: number, normalized name, printed/scan pages,
   formation, qualifications, outcome summary, and QA state.
3. Visually verify every Raman formula page. Grouped headings still yield one row per number.
4. Build the PVR chapter-11 ledger without forcing a 1:1 Raman mapping.
5. Define and validate the predicate JSON schema before active seeding.
6. Implement evaluator families: occupancy, relative-house, lordship,
   dignity/strength, aspect, exchange, distribution, and compound boolean rules.
7. Verify Raman numbers exactly 1–300 once each; every active row has a source,
   exact locator, valid JSON, and evaluator coverage.
8. Fold proven migrations into the baseline and update project docs/history.

## Completed this session

- Extracted all 352 Raman scan pages through the `cproj_book_to_md` workflow using
  Ubuntu's installed DjVu/Tesseract tools.
- Confirmed the contents enumerates combinations 1–300.
- Mapped PVR chapter 11 and confirmed source-specific variants are required.
- Added migration 47 to prepare provenance and variant identity.
- Added orthogonal formation, nature, source-strength, and source-category axes in
  migration 48.
- Catalogued Raman combinations 1–300 with unique source identities and locators.
  Executable/qualified predicate coverage currently reaches 200; entries 201–300
  remain explicitly `NOT_EVALUATED` pending clause-level visual verification.
- Implemented and tested the initial PVR chapter-11 variants; source alternatives
  remain separate variants.
- Added migration 49 and `vw_YogaChartApplicability` to track each variant's
  foundational, required, or confirmatory chart inputs.
- Added migration 51 and `vw_YogaContextRequirements` to normalize the known
  subject-sex, day/night, lunar-phase, and exact-longitude requirements by source
  variant; applied twice successfully to the local database (7 active rows).
- Seeded D1 foundation applicability for 337 tracked Raman/PVR variants: every variant
  requires D1; Raman 28, 29, 46, 54 Navamsa, 57, 62, 66, and 68 additionally
  require D9. Later D9-dependent rules through 200 are also marked in migration 49;
  multi-varga amsa-grade schemes remain unseeded until source verification.

## Remaining work

- Manually verify and normalize the 300 Raman entries.
- Complete the PVR chapter-11 ledger.
- Finalize predicate JSON and evaluator implementation.
- Seed only verified rows, then run database and engine verification.
- Extend chart applicability as later combinations introduce other vargas.

## Changed files

- `scripts/extract-raman-yogas.sh`
- `db/47_prepare_source_attributed_yoga_corpus.sql`
- `db/48_add_yoga_classification_axes.sql`
- `db/49_add_yoga_chart_applicability.sql`
- `db/51_add_yoga_context_requirements.sql`
- `db/checks/49_yoga_chart_applicability.sql`
- `db/checks/51_yoga_context_requirements.sql`
- `docs/superpowers/plans/2026-09-08-source-attributed-yoga-corpus.md`
- `docs/research/reference-sources.md`

## Validation evidence

- `djvused -e n`: 352 pages.
- OCR progress: 352 lines, no `FAILED` or `TIMEOUT` entries.
- Draft contents reaches `300. Gohanta Yoga`.
- Final-hundred catalogue verification confirms Raman 201–300 exactly once, with
  grouped headings expanded into independently tracked source variants.
- `git diff --check` passed for migration 49 and its tracking query.

## Next concrete step

### 2026-09-09 — Production composition and live backfill

- Added `ProductionYogaEngine : IYogaEngine` as the composition root for verified
  Raman/PVR formations, Raman batches 25–200, and the explicit 201–300 pending ledger.
- Results are keyed by `(SourceRefCode, SourceVariantCode)`; textual/source variants
  remain separate. Context-aware results override only the matching batch identity.
- `YogaInputRepository` now persists the complete detailed output rather than only
  Raman 25/58/59/66/68. Unsupported calculations retain `NOT_EVALUATED` plus a
  structured missing-requirement code.
- `ChartGenerationService` supplies the complete chart set and Shadbala results to
  the production engine for both generation and D1 analytics regeneration.
- Backfilled both saved people with `recompute-keydetails`: 337 rows each, all 300
  Raman entry numbers and 18 PVR variants, with zero duplicate source identities.
  Ananya: 211 evaluated / 126 pending. Ramakrishnan: 212 evaluated / 125 pending.
- Validation: 124 isolated yoga tests passed; Web and CLI builds passed; transactional
  persistence verification passed and rolled back its fixtures.

Production composition, full persistence, and the existing-chart backfill are complete.
The next engine work is implementing the calculations still represented as
`NOT_EVALUATED`, beginning with the visually verified Raman 201–244 family.

### 2026-09-09 — Seven context inputs implemented

User confirmed implementation of all seven migration-51 requirements, including
the existing UI Male/Female selection. Branch: chore/github-pm-templates,
canonical shared worktree D:/@ClaudeSpace/ikiastrro.

- Migration 052 adds nullable BirthDetails.Sex (Male/Female; old rows stay NULL)
  and typed tbl_Fact_YogaInputEvaluations rows tied to D1 ChartResultId.
- BirthDetailsRepository and Home form preserve Sex through save/load.
- YogaInputEvaluator supplies seven input requirements to five variants:
  25 (sex/day-night), 58/59 (longitude), 66 (day-night/waxing), 68 (full Moon).
  ChartBundle exposes the same evaluation path; chart generation persists results.
  D1 analytics regeneration obtains the complete chart set for D9 prerequisites.
- Day/night uses astronomical sunrise/sunset. Corrected after-sunset births
  previously being misclassified as day. Method: disc centre, no refraction.
- Lunar elongation is normalized Moon minus Sun. Waxing is 0 < angle < 180.
  Full Moon uses explicit operational Purnima-tithi policy [168,180) degrees,
  code PURNIMA_TITHI_168_INCLUSIVE_180_EXCLUSIVE_V1. This is an implementation
  interpretation, not a numerical orb attributed to Raman.
- Missing or invalid inputs yield nullable Present, NOT_EVALUATED and stable
  missing codes. Existing charts need regeneration to acquire these new facts.
- Applied migration 052; baseline includes yoga migrations 47–49, 51 and 052.
- Validation: 122 yoga tests passed in tests/Ikiastrro.Yoga.Tests; Web and CLI
  builds passed. Transactional tools/YogaInputVerification passed sex round-trip,
  five persisted variants, repeat replacement and noon/evening/predawn checks;
  all fixture writes rolled back.
- Full Web.Tests compilation is independently blocked by stale references to the
  deleted CombinedD1D9Grid. The separate yoga project isolates domain checks.
- Changed areas: BirthDetails/Repository, Home, SwissEphemerisProvider,
  ChartBundle, YogaInputEvaluator/Repository, ChartGenerationService, DI/CLI
  construction, migration/baseline, tests and this handoff. Uncommitted.

The seven context inputs are complete. Remaining corpus work continues below.

Visually verify and activate Raman 201–300 predicates in coherent families, adding
non-D1 chart-applicability rows whenever a definition requires another varga.
