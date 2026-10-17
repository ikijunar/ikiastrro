---
last_updated: 2026-10-01
reflects: Phase 0 planning contract for the IkiAstrro v10 statistical build
---

# IkiAstrro v10 — Phase 0 Statistical Contract Plan

## 1. Purpose

Phase 0 defines what IkiAstrro means by a statistical inference before adding Python, analytics tables, models or new Key Inference UI. Its output is a signed-off, testable contract for the first statistical increment.

The immediate objective is **descriptive population comparison**, not life-outcome prediction:

> Compare a person's existing Key Inference strength evidence with the same evidence in an eligible reference population, while displaying the comparison population, sample size, uncertainty and calculation provenance.

This uses the existing `LifeMatterStatistics` definitions and `tbl_Fact_HouseStrengthStatistics` foundation. No Phase 0 work changes the astrology calculators or their existing strength bands.

## 2. Phase 0 decisions

| Decision | v10 Phase 0 contract |
|---|---|
| Statistical level | Descriptive population comparison |
| First analytical question | “How unusual is this house-strength evidence among comparable charts?” |
| Primary unit | One person × one chart type × one house-from-Lagna |
| Independence unit | Person; twelve houses from one person are not twelve independent people |
| First chart scope | D1 only |
| First measures | Capacity, Consistency, Context and Overall Support |
| Reference comparison | Same chart type and same house-from-Lagna |
| Primary outputs | Percentile, robust z-score, reference median/IQR, sample size and missingness |
| Product surface | Key Inference, with links to the canonical Astro Facts evidence |
| Outcome claims | Prohibited in the first increment |
| Python role | Analyse canonical SQL/C# features; never recalculate astrology rules |

### Why D1 first

D1 gives one clearly comparable base population, is present for every valid saved person and avoids mixing unlike divisional-chart meanings. Other vargas become eligible only after D1's extraction, exclusion and display rules have been validated end to end.

## 3. Definitions

- **Strength score:** the existing IkiAstrro chart-relative value produced by `LifeMatterStatistics`; it is not a probability.
- **Percentile:** the person's rank within an explicitly named reference population.
- **Robust z-score:** distance from the reference median, scaled by a robust spread estimator; it is a population comparison, not an astrological rule.
- **Confidence interval:** uncertainty around an estimated population statistic; it is not confidence that an astrological outcome will occur.
- **Inference:** a plain-language summary of chart evidence and its population comparison.
- **Prediction:** an estimate of a separately measured real-world outcome. v10 Phase 0 does not produce predictions.
- **Association:** a measured relationship between a feature and a defined outcome. It is unavailable until structured outcome data exists.

UI copy must never use “accuracy,” “chance,” “probability,” “will,” or “outcome” for descriptive comparison results.

## 4. Reference population contract

### 4.1 Inclusion criteria

A person may enter the v10 D1 reference population only when:

1. The record is explicitly eligible for statistical/research use.
2. A valid D1 `ChartResult` exists.
3. The D1 calculation has a recorded `RuleSetId` and calculation provenance.
4. A complete saved house-strength-statistics run exists for all twelve signs/houses.
5. Required inputs are numeric or carry an explicit missing reason.
6. There is one selected calculation snapshot per person for the dataset version.

### 4.2 Exclusion criteria

Exclude test fixtures, demonstrations, duplicates and synthetic people unless explicitly labelled; records without research eligibility; invalid or unresolved birth inputs; partial or failed generation runs; incompatible feature-contract versions; and duplicate calculations of one person.

Birth-time uncertainty remains a quality field. The first release may include it in the overall population, but must report its distribution and complete a sensitivity audit before subgroup comparisons are published.

### 4.3 Scored-person handling

When scoring a person already in the dataset, use a leave-one-person-out comparison so the subject does not contribute to their own benchmark.

### 4.4 Version isolation

Every dataset records its dataset and feature-contract versions, `RuleSetId`, calculation-code version or Git commit, ayanāṁśa/configuration identity and build timestamp. The first increment uses one approved rule/configuration stratum; it does not silently pool incompatible versions.

## 5. Measures and statistical rules

### 5.1 Source measures

The first dataset reads the canonical saved equivalents of `CapacityPercent`, `ConsistencyPercent`, `ContextPercent` and `StrengthPercent` (Overall Support). Python verifies and analyses these values; it does not reproduce their astrological formula.

### 5.2 Comparison groups

For the D1 MVP, compare each value against records with the same `ChartType = D1`, `HouseFromLagna`, and approved dataset/feature/rule-set version. Do not pool all twelve houses merely to increase sample size.

### 5.3 Descriptive outputs

For every measure and group, calculate eligible and non-missing counts, missing rate, median, IQR, selected percentiles, diagnostic mean and standard deviation, robust z-score, empirical percentile rank, and a bootstrap confidence interval for the reference median when published. Version the deterministic random seed and bootstrap configuration.

### 5.4 Tie and percentile rule

Use the mid-rank empirical percentile for ties:

```text
percentile = 100 × (count below + 0.5 × count equal) / non-missing count
```

Store enough precision for reproducibility; round only in presentation.

### 5.5 Sample-size display gates

| Eligible non-missing people | Treatment |
|---:|---|
| `< 30` | No percentile; show “insufficient reference data” |
| `30–99` | Exploratory percentile with a small-sample warning |
| `≥ 100` | Percentile without the small-sample warning |

These are product safeguards, not claims that 100 observations automatically make a result scientifically valid.

### 5.6 Missing data

- Never convert missing values to zero.
- Record a controlled missing-reason code.
- Publish the denominator beside every statistic.
- Do not impute values in the first descriptive increment.
- Flag a group as incomplete when more than 20% of a measure is missing.

## 6. Key Inference presentation contract

The first statistical card shows the existing personal score and verbal band; population percentile labelled as such; reference median and IQR; eligible and measured counts; data-sufficiency status; dataset, rule-set and generated-date provenance; and a link to the underlying Astro Facts evidence.

Example:

> **Context: 63% — Good**
> 76th reference-population percentile · median 51% · middle 50%: 43–59% · 184 measured people
> Describes relative chart strength, not the probability of an outcome.

The personal score and population percentile must use distinct labels, typography and help text.

## 7. Privacy and governance contract

Phase 0 must resolve research eligibility before Phase 1 exports person-level data:

- Add an explicit research/statistical-use status; do not infer consent from a saved chart.
- Use a non-identifying subject key in analytics datasets.
- Exclude name, free-text notes and exact birth-place text from Python extracts.
- Exact birth inputs are unnecessary when analysing already computed features.
- Restrict result publication for groups that could expose individuals.
- Withdrawing or deleting a person invalidates future dataset builds.
- Record who or what created each dataset and when.
- Never commit raw person-level exports or model data to Git.

## 8. Outcome research gate

Outcome modelling cannot begin until a separate contract defines one observable outcome, measurement scale, observation window, source, verification status, recording time, missing/censored cases, confounders, minimum cohort/event counts and holdout strategy.

Free-text biographies and classical interpretations are not outcome labels and cannot be silently converted into training targets.

## 9. Phase 0 work packages

### WP0.1 — Inventory and data-readiness audit

Count saved people, D1 charts and complete 12-house statistic sets; identify test/demo/synthetic records; measure missingness; inventory rule/configuration coverage; and confirm whether research eligibility is recorded. Current inspection suggests it is not.

Deliverable: read-only readiness report, with no person-level export.

### WP0.2 — Feature dictionary

For each field, record a stable feature code, name, type/unit/range, canonical C#/SQL source, formula owner, missing conditions, chart scope, version dependencies and interpretation direction.

Deliverable: `Feature Dictionary v1` for the four D1 measures and provenance fields.

Addendum (2026-10-17): candidate avastha and argala-delivery features are recorded as *proposed, not frozen* in the feature
dictionary. Agree the counted-once rule and the collinearity handling (`docs/research/domain/argala-avastha-linkage.md`) before any is added to Phase 1.

### WP0.3 — Dataset and privacy decision

Define research-eligibility states, anonymous subject keys, test/demo exclusion, deletion/rebuild rules, and person-level access.

Deliverable: approved cohort and privacy rules.

### WP0.4 — Statistical-method specification

Confirm comparison strata, percentile ties, robust-z and bootstrap methods, sample/missingness gates and deterministic run parameters. Provide hand-checkable examples.

### WP0.5 — UI language and evidence contract

Approve labels for score, percentile, uncertainty and sample size; non-prediction disclaimer; Astro Facts targets; and insufficient/stale-data states.

### WP0.6 — Phase 1 implementation backlog

Convert the contract into scoped database, Python, C# repository, UI and test tasks. Assign files to their owning workstreams before implementation.

Backlog items from the Argala / avastha work (2026-10-17), each with its owning workstream:

- **UI:** unified Argala table, left-chart overlay, avastha definitions and per-state reasons (built, `workstream/ui`).
- **Core:** `StateReason` and `Evaluate`/`Explain` in the Deeptadi and Lajjitadi calculators (built); persist reasons only if a
  feature snapshot needs them.
- **Database:** decide whether `tbl_Fact_PlanetaryState` needs per-chart Baaladi/Sayanaadi columns kept D1-only, and whether
  the proposed delivery features become analytics-view columns.
- **Python:** descriptive tables for "argala win rests on a sleeping or damaged planet" before any modelling.
- **Research:** audit the house-lord interpretation text (partly from B.V. Raman) against PVR before the reading uses it.

## 10. Acceptance criteria

- [ ] The first analytical question and D1 scope are approved.
- [ ] Unit of observation and person-level independence are documented.
- [ ] Cohort inclusion/exclusion and leave-one-person-out rules are approved.
- [ ] Research eligibility and privacy handling are approved.
- [ ] The four measures have a complete feature dictionary.
- [ ] Percentile, tie, missingness and sample-size rules have worked examples.
- [ ] UI terminology separates score, percentile and probability.
- [ ] Outcome/prediction claims are excluded from the first increment.
- [ ] A read-only data-readiness audit has been reviewed.
- [ ] Phase 1 tasks have owners, acceptance tests and dependency order.

No database migration, Python package or statistical UI should start before these gates are satisfied.

## 11. Recommended execution order

1. Run WP0.1 against the current database.
2. Draft WP0.2 from `LifeMatterStatistics` and `vw_ChartHouseStrengthStatistics`.
3. Resolve WP0.3, especially explicit research eligibility.
4. Finalise WP0.4 with hand-calculated fixtures.
5. Approve WP0.5 copy and states.
6. Produce the Phase 1 implementation backlog in WP0.6.

The immediate next action is the read-only WP0.1 data-readiness audit.
