---
last_updated: 2026-10-01
reflects: Read-only WP0.1 audit of the local ikiastrro SQL Server database
---

# IkiAstrro v10 — Phase 0 Data-Readiness Audit

## Audit scope

WP0.1 was run read-only against `ikiastrro` on `localhost\SQLSERVER2025` on 2026-10-01. It assessed whether the current database can support the D1 descriptive population-comparison contract in [`v10-phase0-plan.md`](v10-phase0-plan.md).

No person names, birth details or row-level records were exported. No database or application data was changed.

## Executive finding

**Technical completeness: ready. Statistical and governance readiness: not ready.**

All six saved people have one D1 chart and a complete, internally consistent twelve-house statistics set. They use the same rule set, ayanāṁśa, house system and engine configuration. However:

1. The cohort contains only **6 people**, below Phase 0's minimum `n = 30` even for an exploratory percentile.
2. The schema has **no explicit research/statistical-use eligibility or consent field**.
3. Test/demo/synthetic status is not modelled; a name-pattern check found no obvious examples, but that is not a reliable governance control.

Therefore the current database may be used to build and test the analytics pipeline with clearly labelled development data, but it must not publish reference-population percentiles.

## Population and coverage

| Check | Result | Assessment |
|---|---:|---|
| Saved people | 6 | Insufficient statistical cohort |
| People with any chart | 6 | Complete |
| People with D1 | 6 | Complete |
| D1 chart rows | 6 | Exactly one per person |
| People with D1 statistics | 6 | Complete |
| Complete D1 twelve-house sets | 6 | Complete |
| Incomplete D1 twelve-house sets | 0 | Pass |
| Duplicate names | 0 | Informational only |
| Duplicate birth fingerprints | 0 | Pass for current data |
| Benchmark-linked people | 0 | No benchmark records included through that relationship |
| Obvious test/demo/synthetic name patterns | 0 | Not a substitute for an explicit flag |

Each house-from-Lagna group contains `n = 6`. Under the Phase 0 display gates, every group must return **“insufficient reference data”** rather than a percentile.

## Feature completeness

The D1 statistics table contains 72 rows: 6 people × 12 houses.

| Field | Missing rows | Observed range |
|---|---:|---:|
| Capacity | 0 | 27–100 |
| Consistency | 0 | 0–56 |
| Context | 0 | 25–81 |
| Overall Support (`StrengthPercent`) | 0 | 31–66 |
| SAV | 0 | — |
| Lord BAV | 0 | — |
| Independent Bhava z-score | 0 | — |
| Lord Ṣaḍbala | 0 | — |
| Lord Amsabala | 0 | — |

Every house number 1–12 has six rows and no missing values in the four v10 measures.

Avastha inputs (note added 2026-10-17, outside the audit's measured scope): `tbl_Fact_PlanetaryState` holds state ids for
Jagradadi, Deeptadi and Lajjitadi on every chart type, and Baaladi and Sayanaadi on D1 only. It does not store the reason for
a state; reasons are recomputed from key details, conjunctions and aspects. Per-person coverage of these facts was not
audited here and should be added to the next readiness audit before any avastha feature is proposed for Phase 1.
See `docs/research/domain/argala-avastha-linkage.md`.

## Birth-input completeness

Across the six saved people:

| Input | Missing |
|---|---:|
| Date | 0 |
| Time | 0 |
| Coordinates | 0 |
| UTC offset | 0 |
| IANA time-zone ID | 0 |
| Sex | 0 |

Completeness does not prove accuracy, consent or birth-time rectification. Those remain separate data-quality attributes.

## Configuration and provenance

All six D1 charts use one configuration:

| Property | Value |
|---|---|
| Rule set | `1` — `Parashari-Classical`, version 1, active and published |
| Ayanāṁśa | `Traditional Lahiri` / `AYANAMSA_LAHIRI`, Swiss mode 1 |
| House system | `WholeSign` |
| Engine | `SwissEphNet 2.8.0.2 (Moshier, Traditional Lahiri); PVR upagrahas/rules 1` |
| Calculation kind | `PositionChart` |
| D1 chart computation window | 2026-09-28 through 2026-09-30 |
| Statistics computation window | 2026-10-01 12:27:10 through 12:27:44 UTC |

All 72 D1 statistics rows use `RuleSetId = 1`.

## Integrity checks

| Check | Failures |
|---|---:|
| Statistics rule set differs from chart rule set | 0 |
| House outside 1–12 | 0 |
| Capacity outside 0–100 | 0 |
| Consistency outside 0–100 | 0 |
| Context outside 0–100 | 0 |
| Overall Support outside 0–100 | 0 |
| Statistics timestamp older than its chart | 0 |
| People without exactly one D1 | 0 |

## Governance gaps

A database metadata search found no column representing:

- Consent
- Research eligibility
- Statistical-use permission
- Synthetic/test/demo status

Consequences:

- Saving a chart cannot be treated as permission to include it in research.
- The six current people cannot be assumed eligible for a reference population.
- Name matching cannot safely distinguish production, personal, demonstration or synthetic records.
- Dataset construction needs an explicit eligibility source before person-level extraction begins.

## Readiness scorecard

| Area | Status | Reason |
|---|---|---|
| Canonical D1 source measures | Green | Complete for all current people |
| Twelve-house coverage | Green | 6/6 complete |
| Missingness | Green | No missing v10 measures |
| Rule/configuration consistency | Green | One compatible stratum |
| Row integrity | Green | All checks passed |
| Population size | Red | `n = 6`; minimum exploratory threshold is 30 |
| Research eligibility | Red | Not represented |
| Synthetic/test classification | Red | Not represented |
| Birth-time quality/rectification | Amber | Inputs complete, quality status absent |
| Outcome modelling | Red | No structured outcome dataset; intentionally out of first increment |

## Decision

**No-go for published descriptive percentiles.**

**Go for development-only pipeline work after the cohort/consent contract is approved**, provided all outputs are labelled development data and the UI exercises the insufficient-data state rather than showing a percentile.

## Required next actions

1. Complete WP0.2: define the four measures and provenance fields in a feature dictionary.
2. Complete WP0.3 before person-level Python extraction:
   - explicit research/statistical-use status;
   - synthetic/test/demo classification;
   - anonymous subject-key policy;
   - withdrawal/deletion behaviour.
3. Establish a lawful, documented cohort-acquisition path.
4. Reach at least 30 eligible people for exploratory percentiles and 100 for an unflagged display under the provisional Phase 0 gates.
5. Add a birth-time quality/rectification status before subgroup or higher-varga statistical comparisons.
6. Re-run this audit after eligibility metadata and additional records exist.

## Reproducibility note

The audit queried aggregate counts, nulls, ranges and configuration groupings directly from:

- `tbl_BirthDetails`
- `tbl_ChartResults`
- `tbl_Fact_HouseStrengthStatistics`
- `tbl_Rule_Sets`
- `tbl_Rule_Ayanamsa`
- SQL Server schema metadata

The report intentionally contains no row-level personal data.
