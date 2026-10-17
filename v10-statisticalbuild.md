---
last_updated: 2026-10-04
reflects: v10 statistical foundation plus planned family similarity and linked-dasha analysis using the existing family model
---

# IkiAstrro v10 — Statistical Build Plan

## Build status — 2026-10-02

The first guarded implementation slice is operational:

- migration 163 adds explicit opt-in analytics subjects, anonymous D1 feature extraction,
  versioned dataset/run metadata and persisted descriptive comparisons;
- existing saved people remain unenrolled, so saving a chart never implies research consent;
- `src/Ikiastrro.Analytics` validates cohorts and builds deterministic leave-one-person-out
  descriptive comparisons;
- percentiles are suppressed below 30 measured reference people;
- the first development run completed successfully with zero eligible subjects and zero
  comparisons, which is the correct privacy-safe result for the current database.
- `validate-dataset` and `describe` now share a hard publication gate: exactly one compatible
  D1 chart, twelve distinct houses, complete 0–100 feature values and consistent provenance per
  subject; invalid cohorts stop before a dataset or run is created.
- migration 168 adds `vw_AnalyticsHouseFeaturesV2`, enforcing the same consent, single-D1,
  twelve-house, numeric-range and rule-set checks at the SQL publication boundary; Python retains
  its independent gate as defense in depth.
- Key Inference reads the latest completed run through a C# repository and shows a dedicated
  population-evidence card. Unenrolled, ineligible, no-run, insufficient-data and available
  states are distinct; missing percentiles are never rendered as zero.
- local master commit `f5ac02e` changes the strength inputs, so migration 165 and dataset
  contract v2 isolate post-correction comparisons from historical v1 data.
- Saved Charts now provides explicit cohort administration for research status, record
  classification and birth-time quality. Eligibility requires both Research classification and
  a checked explicit-permission confirmation; existing people remain unenrolled.
- local master commit `32ecbfc` already handles analytics during full database reset. The
  per-person deletion path now complements it by deleting comparisons and the analytics-subject
  row before chart and birth-detail removal.

Key Inference population cards remain gated on an approved eligible cohort and a completed run
containing comparisons. The current product state is “insufficient reference data.”

The next phase turns Astro Facts into the trusted feature/evidence layer, while Key Inference becomes a statistical interpretation layer over many charts.

> C# computes astrological facts → SQL Server stores and version-controls them → Python analyses populations and trains and validates models → Key Inference presents the results with provenance and uncertainty.

This builds on the existing Astro Facts and Key Inference implementations and the saved house statistics introduced in migration 156.

## Important distinction

Before building models, define what “statistical” means:

1. **Chart-relative statistics** — percentiles, z-scores and unusual features compared with a chart corpus. This needs many birth charts, but not known life outcomes.
2. **Outcome-associated inference** — whether combinations are associated with career, marriage, health, wealth or timing outcomes. This requires structured, dated, independently collected outcome data.
3. **Prediction** — estimating an unseen person’s outcome. This is the hardest stage and should come only after out-of-sample validation.

Birth charts alone support the first category. They cannot establish outcome accuracy or causation.

## Family view extension

The completed v10 statistical model must reuse the family model already introduced by migration
168; it must not create parallel `FamilyCore` or `FamilyExtended` tables. The authoritative source
is `tbl_Person_Relationship` plus `tbl_Dim_RelationType`, exposed through `vw_PersonFamily` with
its `Core` and `Extended` tiers.

The first Core-family acceptance fixture is RamakrishnanP (father), RameshwariS (mother) and
AnanyaR (child). These saved-chart names are test fixtures only. Analytics datasets and persisted
results must use anonymous person and derived family keys, never names.

The statistical family view must provide:

- each member's individual comparison with the eligible reference population;
- symmetric pairwise and whole-family **similarity** over versioned canonical Astro Facts;
- role-aware **compatibility** as a separate measure—similarity must not be presented as
  relationship quality or outcome prediction;
- one shared calendar axis connecting each member's versioned Mahadasha and Antardasha periods,
  including active lords and their linked Astro Facts or compatibility evidence;
- Core/Extended filtering directly from `vw_PersonFamily.Tier`, while preserving the relationship
  role, adoption/step flags and status already represented by the existing model.

Family analytics views may derive a stable anonymous `FamilyKey` and pair/overlap rows from those
existing objects, but must not duplicate relationship truth in another operational table. Every
result must retain the feature schema, similarity method and weights, dasha method/version,
astrology `RuleSetId`, dataset/run version and computation timestamp.

Statistical safeguards:

- every included member independently passes consent, eligibility and provenance gates;
- suppress a required-member family aggregate rather than silently changing its membership;
- keep all members of one derived family in the same train, validation or holdout partition;
- account for within-family dependence and never count pairwise rows as independent people;
- treat overlapping dasha periods as temporal co-occurrence, not causal evidence;
- make no hereditary or outcome claim from the three-person acceptance fixture.

Acceptance criterion: the Core fixture resolves the three people and their roles from the existing
relationship model, renders all three pairwise similarity and compatibility comparisons with
separate labels, and aligns their linked dasha periods on one time axis with full provenance and
no name-bearing analytics export.

## Phase 0 — Define the statistical contract

Create a short design specification before implementation. Decide:

- Unit of observation: one person, one chart, one life matter or one person-period.
- One narrow first target.
- Population eligibility and exclusion rules.
- Outcome definition and measurement method.
- Which Astro Facts are candidate inputs.
- Whether the first release is descriptive, associative or predictive.
- Minimum sample size and minimum subgroup size.
- Privacy, consent, retention and anonymisation rules.

Recommended first target: **descriptive population comparison for the existing Key Inference strength axes**. This is achievable with current data and avoids prematurely claiming predictive validity.

Deliverable: `Statistical Analysis Contract v1`.

## Phase 1 — Stabilise Astro Facts as the feature foundation

Do not let Python reinterpret raw planetary positions independently. Python should consume canonical features produced by the existing Core/Data calculations.

Create versioned SQL views such as:

- `vw_AnalyticsPerson`
- `vw_AnalyticsChart`
- `vw_AnalyticsPlanetFeatures`
- `vw_AnalyticsHouseFeatures`
- `vw_AnalyticsLifeMatterFeatures`
- `vw_AnalyticsOutcome`

The feature views should include:

- Anonymous person key
- Chart/result ID and chart type
- Rule-set ID
- Calculation/configuration version
- Ayanāṁśa and house-system settings
- Feature code and numeric value
- Missing-value reason
- Computed timestamp

`tbl_Fact_HouseStrengthStatistics` is the starting point. Extend the same pattern to the inputs Key Inference needs:

- Ṣaḍbala
- Amsabala
- SAV and BAV
- Independent Bhava Bala
- Argala
- Dignity and planetary condition
- Cross-varga confirmation
- Yoga or relationship evidence only after its meaning is sufficiently standardised

Acceptance criterion: given the same `ChartResultId` and `RuleSetId`, C# and the analytics view always return the same feature values.

### Avasthas and argala (added 2026-10-17)

Full note: [`docs/research/domain/argala-avastha-linkage.md`](docs/research/domain/argala-avastha-linkage.md).

"Dignity and planetary condition" above is the avastha input. What each set actually uses (PVR §15.4):

| Set | Inputs | Overlaps with |
|---|---|---|
| Jagradadi (alertness) | Dignity only | Dignity score: a pure re-coding |
| Deeptadi (9 states) | Dignity, sign-lord friendship, conjunction with malefics, sign lord a malefic, closeness to the Sun | Dignity, conjunction counts, combustion |
| Lajjitadi (6 states) | House position (5th), conjunctions, aspects received, friendship with joining/aspecting planets, watery sign, dignity | Dignity, conjunctions, aspects |
| Baaladi (age) | Degree in sign, odd/even sign | None (D1 only) |
| Sayanaadi (12 states) | Nakshatra, planet index, navamsa, Moon's nakshatra, birth ghati, lagna | None (D1 only) |

Rules for the build:

1. **Counted once.** Argala Context keeps its current definition (counts of holds, contested and obstructed pairs). Avasthas
   must never be folded into Context, Capacity or Consistency; they enter as their own planetary-condition features. This
   is the same reason Independent Bhava Bala excludes the lord's Ṣaḍbala.
2. **Collinearity in modelling.** Jagradadi is dignity recoded, and Deeptadi and Lajjitadi contain dignity, conjunctions
   and aspects. Phase 4 descriptives may show them side by side; a Phase 6 model must not enter dignity and Jagradadi
   (or Deeptadi's tier) as independent predictors, and must report the overlap.
3. **Chart scope.** Jagradadi, Deeptadi and Lajjitadi exist for every varga; Baaladi and Sayanaadi are D1-only and are
   excluded from any cross-varga feature.
4. **Reasons are not persisted.** `PlanetaryStateFact` carries state ids only; per-state reasons (`StateReason`) are
   recomputed by `PlanetaryStateComputer` from the chart's key details, conjunctions and aspects. A feature snapshot
   (`tbl_Fact_AnalyticsFeatureSnapshots`) that needs a reason must store it with the snapshot.
5. **Argala x avastha interaction** is a Phase 4 descriptive question first (how often does a one-planet argala win rest
   on a sleeping or damaged planet?), not a model input. The proposed features are in the feature dictionary.
6. **Sourcing labels.** Delivery levels and the damaged-house results are PVR §15.4; lane delivery (strongest planet),
   the deciding-planet test and the caveat wording are `PROJECT_SYNTHESIS` and must be labelled so in any Key Inference
   surface that uses them.

## Phase 2 — Add a statistical dataset layer

Keep research datasets separate from operational chart tables. Suggested concepts:

- `tbl_Dim_AnalyticsDatasets` — reproducible cohort and inclusion criteria.
- `tbl_Dim_OutcomeDefinitions` — exact meaning and measurement of an outcome.
- `tbl_Fact_OutcomeObservations` — anonymised observations, dates, source, confidence and verification status.
- `tbl_Fact_AnalyticsFeatureSnapshots` — optional immutable snapshot of the inputs used by a model run.
- `tbl_Fact_AnalyticsRuns` — code version, dataset version, rule set, parameters, timestamps and status.
- `tbl_Fact_ModelMetrics` — validation metrics by model, target and subgroup.
- `tbl_Fact_StatisticalInferences` — results surfaced by Key Inference.

Use an anonymous research subject key instead of names. Keep birth details and outcome records inaccessible to exported analysis files unless explicitly required.

## Phase 3 — Create the Python analytics project

Add a proper Python package:

```text
src/Ikiastrro.Analytics/
  pyproject.toml
  src/ikiastrro_analytics/
    config.py
    sql.py
    datasets.py
    validation.py
    descriptive.py
    models.py
    persistence.py
  tests/
  notebooks/
```

Recommended libraries:

- `pyodbc` or SQLAlchemy for SQL Server
- `pandas` and `numpy` for dataset construction
- `scipy` and `statsmodels` for statistical tests and confidence intervals
- `scikit-learn` for predictive experiments
- `joblib` for model artifacts
- `pytest` for tests

Use batch integration initially:

```text
SQL analytics views
        ↓
Python dataset validation
        ↓
Descriptive/model analysis
        ↓
Metrics and inference rows written to SQL
        ↓
C# repository
        ↓
Key Inference page
```

Do not introduce a Python HTTP service initially. Database-mediated batch integration is easier to reproduce, test and operate. Add a service later only if genuine interactive scoring becomes necessary.

## Phase 4 — Deliver descriptive statistics first

The first Python release should answer:

- How does this chart compare with the reference population?
- What percentile is each strength axis in?
- Is a value genuinely unusual or merely above average?
- How stable is the estimate?
- How many comparable records exist?
- Are subgroup results materially different?

For every displayed result, retain:

- Sample size
- Mean/median and distribution
- Percentile
- Standard deviation or robust spread
- Confidence interval
- Missing-data rate
- Cohort definition
- Dataset version
- Rule-set version
- Computation timestamp

This permits defensible statements such as: “House-support context is at the 76th percentile among 1,284 comparable D1 charts.”

## Phase 5 — Redesign Key Inference around two evidence levels

Preserve the current Astro Facts links and add population evidence beside the existing chart evidence.

A Key Inference result should show:

- **Chart evidence:** the person’s Capacity, Consistency and Context values.
- **Population comparison:** percentile and reference distribution.
- **Inference:** plain-language interpretation.
- **Reliability:** sample size, confidence interval and missing inputs.
- **Evidence drill-down:** contributing Astro Facts.
- **Provenance:** dataset, rule set, method and run version.

Recommended hierarchy:

```text
Question
  ├─ Personal chart evidence
  ├─ Population comparison
  ├─ Strongest supporting and limiting factors
  ├─ Statistical confidence and sample size
  └─ Open underlying Astro Facts
```

Never show a percentage without naming what it represents. “72% strength”, “72nd percentile” and “72% predicted probability” are different quantities.

## Phase 6 — Add outcome modelling only after data quality is proven

Start with interpretable baselines:

1. Population/base-rate model
2. Logistic or ordinal regression
3. Regularised regression
4. Decision tree or gradient boosting as a challenger

Do not begin with a complex black-box model.

Validation requirements:

- Split by person, never by chart row.
- Use untouched holdout data.
- Use time-based validation when outcomes have dates.
- Prevent D1/D9/etc. rows from the same person leaking across train and test sets.
- Keep every member of a family in the same train, validation or test partition.
- Compare every model against a simple baseline.
- Report calibration, not just accuracy.
- Report subgroup performance.
- Correct for multiple hypothesis testing when testing many factors.
- Treat associations as associations, not causal findings.

For classification, track ROC-AUC, precision/recall, Brier score and calibration. For ordinal or continuous outcomes, use MAE/RMSE and residual analysis.

## Phase 7 — Reproducibility and operationalisation

Add CLI commands along these lines:

```text
analytics validate-dataset
analytics build-snapshot
analytics describe
analytics train
analytics evaluate
analytics publish
analytics compare-runs
```

Every published inference should be traceable to:

- Git commit
- Python environment/lock file
- Dataset version
- Outcome-definition version
- SQL migration version
- Astrology `RuleSetId`
- Feature-schema version
- Model parameters
- Validation results

A failed or underperforming model must not replace the currently published model.

## Delivery sequence

| Increment | Outcome |
|---|---|
| 1. Statistical contract | One defined population and one narrow analytical question |
| 2. Feature views | Python-ready, versioned Astro Facts dataset |
| 3. Python foundation | Repeatable SQL extraction, validation and persistence |
| 4. Descriptive comparison | Percentiles, distributions, sample sizes and confidence |
| 5. Key Inference UI | Personal evidence plus population evidence and provenance |
| 6. Outcome pilot | One carefully defined labelled outcome and baseline model |
| 7. Validation gate | Independent holdout, calibration and subgroup checks |
| 8. Controlled publication | Versioned model results available to the web application |
| 9. Family view | Reused Core/Extended relationships, similarity, compatibility and linked dasha periods |

## Recommended first milestone

> For every Key Inference house/life-matter reading, compare Capacity, Consistency, Context and Overall Support with an eligible reference population, showing percentile, sample size and provenance.

This reuses the present implementation, produces genuine statistical value, and creates the infrastructure needed for later outcome research without overclaiming what the current data can prove.
