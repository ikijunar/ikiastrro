# ikiastrro — Product features & completion

What the software does, grouped by engine / subsystem, with per-dimension completion. IDs are
`FEAT-<AREA>-<NN>`. Checklist: **DB · Core · Verify · Web · Docs** (`% = checked ÷ 5`). Status
ladder: `Planned → Designed → DB → Core → Verified → Web → Done`. Conventions: `STANDARDS.md §M.3`.

## Rollup

| Area | Features | Avg % | Missing DB | Missing Core | Missing Verify | Missing Web | Missing Docs |
|---|---|---|---|---|---|---|---|
| ASTRO_CALC | 2 | 90% | 0 | 0 | 0 | 1 | 0 |
| POSITION | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| VARGA | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| HOUSE | 3 | 60% | 1 | 1 | 1 | 0 | 1 |
| NAKSHATRA | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| DIGNITY | 1 | 100% | 0 | 0 | 0 | 0 | 0 |
| RELATIONSHIP | 4 | 60% | 1 | 1 | 1 | 1 | 1 |
| KARAKA | 4 | 55% | 2 | 2 | 2 | 2 | 2 |
| AVASTHA | 5 | 32% | 3 | 3 | 3 | 5 | 3 |
| DISPOSITOR | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| STRENGTH | 2 | 0% | 2 | 2 | 2 | 2 | 2 |
| DASHA | 2 | 90% | 0 | 0 | 0 | 0 | 0 |
| YOGA | 1 | 0% | 1 | 1 | 1 | 1 | 1 |
| TRANSIT | 2 | 80% | 0 | 0 | 0 | 1 | 0 |
| ENGINE | 2 | 90% | 0 | 0 | 0 | 1 | 0 |
| TERM | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| INFRA | 1 | 80% | 0 | 0 | 0 | 1 | 0 |
| DOCS | 1 | 100% | 0 | 0 | 0 | 0 | 0 |

*(Recompute the rollup from the feature rows whenever a box changes — it is a manual mirror.)*

## Features

### ASTRO_CALC
- **FEAT-ASTRO_CALC-01 · Sidereal positions (Swiss Ephemeris)** — Done · 100%
  Verify `verify-schema` · Spec — (pre-spec) · Plan — (pre-plan)
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-ASTRO_CALC-02 · Sunrise / sunset (`swe_rise_trans`)** — Verified · 80%
  Verify `verify-jaimini` · Spec `2026-09-01-jaimini-…` · Plan `2026-09-01-jaimini-…`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete

### POSITION
- **FEAT-POSITION-01 · D1 (Rāśi) chart** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### VARGA
- **FEAT-VARGA-01 · Divisional charts D1–D60 (21 types)** — Verified · 80%
  Verify `verify-vargas` · Spec `2026-08-31-divisional-chart-completion-design` · Plan `2026-08-31-divisional-charts-plan-a`
  DB [x] · Core [x] · Verify [x] · Web [ ] (only D1/D9 rendered) · Docs [x] · Research: complete

### HOUSE
- **FEAT-HOUSE-01 · Whole-sign houses (Lagna/Sūrya/Chandra) + house lords** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-HOUSE-02 · Functional benefic / malefic by Lagna** — Verified · 80% · Verify `verify-functional-nature`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete
- **FEAT-HOUSE-03 · Bhāva significations + Sthira Kāraka mapping (migration 030)** — Designed · 0%
  Spec `reference-house-lagna-significations` · Research: partial (`SRC_RAMAN_HTJH` extract, 3 unsourced cells)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [x]

### NAKSHATRA
- **FEAT-NAKSHATRA-01 · Nakshatra / pāda / Vimśottari lord / KP sub-lord + reference linkage** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### DIGNITY
- **FEAT-DIGNITY-01 · Pañchadhā Maitrī dignity (9-tier)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### RELATIONSHIP
- **FEAT-RELATIONSHIP-01 · Conjunctions (Yuti)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-02 · Aspects (Graha Dṛṣṭi)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-03 · Combustion (Asta)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-RELATIONSHIP-04 · Compound Maitrī / argala / sambandha** — Planned · 0%
  Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### KARAKA
- **FEAT-KARAKA-01 · Chara Karakas (8-fold Aṣṭa)** — Verified · 80% · Verify `verify-jaimini`
  Spec/Plan `2026-09-01-jaimini-…`
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-KARAKA-02 · Special points (AL + 12 Bhāva Arudhas + HL + Gulika + Maandi)** — Done · 100% · Verify `verify-jaimini`
  DB [x] · Core [x] · Verify [x] · Web [x] (CombinedD1D9Grid) · Docs [x] · Research: complete
- **FEAT-KARAKA-03 · Sthira Kāraka** — Planned · 0% · Research: partial (`SRC_RAMAN_HTJH`)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-KARAKA-04 · Naisargika Kāraka (Sapta vs Aṣṭa — undecided)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### AVASTHA
- **FEAT-AVASTHA-01 · `AgeState` (Bālādi)** — Verified · 80% · Verify `verify-avastha` · Research: complete
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x]
- **FEAT-AVASTHA-02 · `WakefulnessState` (Jāgradādi)** — Verified · 80% · Verify `verify-avastha` · Research: complete
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x]
- **FEAT-AVASTHA-03 · `RadianceState` (Dīptādi)** — Planned · 0% · Research: partial (bands need a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-AVASTHA-04 · `ShameState` (Lajjitādi)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-AVASTHA-05 · `PostureState` (Śayanādi)** — Planned · 0% · Research: not started (needs janma-ghaṭis + a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### DISPOSITOR
- **FEAT-DISPOSITOR-01 · Dispositor chains / final dispositor / mutual reception** — Planned · 0% · Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### STRENGTH
- **FEAT-STRENGTH-01 · Ṣaḍbala (6 components) + Iṣṭa/Kaṣṭa + Bhāva Bala** — Planned · 0% · Research: partial (`SRC_BPHS_27` — needs a cited edition)
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]
- **FEAT-STRENGTH-02 · Vimśopaka Bala (varga strength)** — Planned · 0% · Research: partial
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### DASHA
- **FEAT-DASHA-01 · Vimśottari (3-level, partial-at-birth)** — Done · 100% · Verify `verify-schema`
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete
- **FEAT-DASHA-02 · Life-in-weeks grid (4000-week)** — Verified · 80%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [ ] · Research: complete

### YOGA
- **FEAT-YOGA-01 · Yoga detection (Pañcha Mahāpuruṣa + Rāja/Dhana first slice)** — Planned · 0% · Research: not started
  DB [ ] · Core [ ] · Verify [ ] · Web [ ] · Docs [ ]

### TRANSIT
- **FEAT-TRANSIT-01 · Slow-planet sign-transit history (1930–2060)** — Verified · 80% · Verify (`precheck-planet-transits`)
  DB [x] · Core [x] · Verify [x] · Web [ ] · Docs [x] · Research: complete
- **FEAT-TRANSIT-02 · Sade Sati / Kantaka / Ashtama** — Done · 100%
  DB [x] · Core [x] · Verify [x] · Web [x] · Docs [x] · Research: complete

### TERM
- **FEAT-TERM-01 · Terminology catalogue (Sanskrit / English / Tamil)** — Verified · 80%
  Verify `verify-terminology` · Spec `2026-09-02-engine-organization-terminology-design` §8 · Plan `2026-09-02-engine-organization-terminology` · Research: complete
  DB [x] (`tbl_Astro_Terminology` + `_Text`, migration 17) · Core [x] (`Ikiastrro.Core.Reference.TerminologyCatalog`) · Verify [x] · Web [ ] · Docs [x]
  236 concepts seeded `sa`+`en` (`Latn`); Tamil (`ta`/`Taml`) + Devanagari (`sa`/`Deva`) are pure inserts later — schema needs no rework.

### ENGINE
- **FEAT-ENGINE-01 · Engine-stack layering (`Engines/<Name>/`)** — Verified · 80%
  Verify `verify-schema` + `verify-pipeline` · Spec `2026-09-02-engine-organization-terminology-design` §4–§7 · Plan `2026-09-02-engine-organization-terminology` · Research: complete
  DB [x] (migration 16 avastha→planetary-state rename) · Core [x] (13 engines + `ChartPipeline`/`ChartBundle`; `ChartAnalyzer` split into House/Nakshatra/Relationship engines, behaviour-preserving) · Verify [x] · Web [ ] · Docs [x]
  Reserved seams only (interfaces): Dispositor, Strength, Yoga, Sthira/Naisargika Karaka. `ChartGenerationService.GenerateAll` pipeline adoption deferred.
- **FEAT-ENGINE-02 · Rule-table portability (`tbl_Rule_Catalog` + interpreters)** — Verified · 80%
  Verify `verify-rules` · Spec `2026-09-02-engine-organization-terminology-design` §9 · Plan `2026-09-02-engine-organization-terminology` · Research: complete
  DB [x] (portability tail on 7 `tbl_Rule_*`; `tbl_Rule_Catalog` + 12-row index; 5 reserved rule tables — migration 18) · Core [x] (3 `IVargaMethodInterpreter` families: `LINEAR_VARGA` / `GRID_VARGA` / `BAND_VARGA`; `seed-rule-params` backfills all 20 `RuleParametersJson`) · Verify [x] (every scheme round-trips to its C# rule over the full circle) · Web [x] (n/a) · Docs [x]

### INFRA
- **FEAT-INFRA-01 · Multi-environment config (dev/stage/uat/prod)** — Verified · 80%
  Spec `2026-09-02-engine-organization-terminology-design` §17 · Plan `2026-09-02-project-foundations`
  DB [x] (`tbl_Dim_Source`, `:setvar`) · Core [x] (`SqlConnectionFactory.Create` + CLI `--db` + `:setvar DbName`) · Verify [x] · Web [ ] (per-env `appsettings.{Environment}.json` land with a real stage/uat deploy) · Docs [x] · Research: complete

### DOCS
- **FEAT-DOCS-01 · Documentation taxonomy + `PRODUCT.md` + citation registry** — Done · 100%
  Spec `2026-09-02-engine-organization-terminology-design` §16 · Plan `2026-09-02-project-foundations`
  DB [x] · Core [x] · Verify [x] (`verify-sources`) · Web [x] · Docs [x] · Research: complete
