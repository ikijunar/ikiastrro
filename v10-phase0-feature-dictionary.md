---
last_updated: 2026-10-01
reflects: WP0.2 feature dictionary for the IkiAstrro v10 D1 descriptive-statistics increment
---

# IkiAstrro v10 — Phase 0 Feature Dictionary v1

## Scope

This dictionary defines the four numeric features approved for the first D1 descriptive population comparison. The canonical implementation is `Ikiastrro.Data.Statistics.LifeMatterStatistics`; persisted values are written by `HouseStrengthStatisticsService` to `tbl_Fact_HouseStrengthStatistics` and exposed by `vw_ChartHouseStrengthStatistics`.

Python consumes the persisted values. It must not independently implement the astrology formulas.

## Critical scope distinction

The persisted rows are **lord-only house baselines**:

- Capacity uses the sign lord's Ṣaḍbala only.
- Consistency uses the sign lord's Amsabala only.
- Context describes the sign/house.
- Overall Support averages those three axes.

The live Key Inference life-matter score can additionally include the matter's kāraka planets in Capacity and Consistency. Therefore a persisted house-baseline percentile is not automatically the percentile of the selected matter's live score.

For the first increment, the UI must label the statistical comparison **“house baseline compared with similar D1 houses.”** A later feature contract may define and persist matter-specific observations keyed by life-matter code.

## Observation identity

One statistical observation is uniquely identified by:

```text
BirthDetailId × ChartResultId × ChartType × HouseFromLagna × RuleSetId
```

For v1:

- `ChartType` must equal `D1`.
- Each person contributes at most one observation to each house-from-Lagna comparison group.
- `SignId` and `LordPlanetId` are explanatory dimensions, not additional independent observations.
- Statistical independence and train/test splitting operate at person level.

## Feature catalogue

| Stable code | Display name | SQL column | Storage | Unit/range | Higher means |
|---|---|---|---|---|---|
| `KI_D1_HOUSE_CAPACITY_V1` | Capacity | `Capacity` | `TINYINT NULL` | index, 0–100 | Greater capacity of the house's ruling planet by Ṣaḍbala |
| `KI_D1_HOUSE_CONSISTENCY_V1` | Consistency | `Consistency` | `TINYINT NULL` | percentage/index, 0–100 | Ruling planet is favourable in a greater share of the Ṣoḍaśavarga scheme |
| `KI_D1_HOUSE_CONTEXT_V1` | Context | `Context` | `TINYINT NULL` | index, 0–100 | More supportive combined house/sign setting |
| `KI_D1_HOUSE_SUPPORT_V1` | Overall Support | `StrengthPercent` | `TINYINT NULL` | index, 0–100 | Higher mean of the readable Capacity, Consistency and Context axes |

These values describe relative chart strength. None is a probability, outcome, accuracy score or causal estimate.

## Feature definitions

### `KI_D1_HOUSE_CAPACITY_V1`

**Canonical C# property:** `HouseStatistics.Capacity`
**Persisted input:** `LordShadbalaPercent`
**Raw source:** `vw_ChartShadbala.PercentOfMinimum` through `PlanetaryStrengthRepository`

For a saved lord-only row:

```text
lord index = clamp(round_away_from_zero(LordShadbalaPercent / 200 × 100), 0, 100)
Capacity = rounded mean of available planet indices
```

Because the saved row has no matter kārakas, its available planet set contains only the sign lord. The required Ṣaḍbala minimum maps to index 50.

**Missing when:** the lord has no readable Ṣaḍbala value.
**Interpretation direction:** higher means more of the configured minimum-relative planetary capacity; it does not mean a life outcome is more likely.

### `KI_D1_HOUSE_CONSISTENCY_V1`

**Canonical C# property:** `HouseStatistics.Consistency`
**Persisted input:** `LordAmsabalaPercent`
**Raw source:** `vw_ChartAmsabala` through `AmsabalaRepository`
**Scheme:** `SHODASAVARGA` only

```text
planet percentage = round_away_from_zero(100 × GoodCount / GroupSize)
Consistency = rounded mean of available planet percentages
```

For a saved lord-only row, the mean contains only the sign lord. The implementation deliberately does not average the overlapping Ṣaḍvarga, Saptavarga, Daśavarga and Ṣoḍaśavarga schemes.

**Missing when:** no valid `SHODASAVARGA` row exists for the lord, or `GroupSize <= 0`.
**Interpretation direction:** higher means favourable dignity occurs in more charts of the canonical varga scheme.

### `KI_D1_HOUSE_CONTEXT_V1`

**Canonical C# property:** `HouseStatistics.Context`
**Persisted inputs:** SAV, lord BAV, independent Bhava Bala z-score and Argala counts

Context is the rounded mean of the available component indices:

| Component | Persisted source | Index formula |
|---|---|---|
| SAV | `SavBindus` | `clamp(round(SAV / 56 × 100), 0, 100)` |
| Lord BAV | `LordBavBindus` | `clamp(round(BAV / 8 × 100), 0, 100)` |
| Independent Bhava Bala | `IndependentBhavaZ` | `clamp(round(50 + 10z), 0, 100)` |
| Argala | `ArgalaHolds`, `ArgalaContested`, `ArgalaObstructed` plus pair-level availability during computation | Holds = 1, contested = 0.5, obstructed = 0; mean judged-pair share × 100 |

Independent Bhava Bala is Dig Bala plus Drik Bala, with the lord's Ṣaḍbala contribution excluded to avoid counting the lord in both Capacity and Context. Its z-score is computed across that chart's twelve houses using population standard deviation; if all values are equal every house receives `z = 0`.

Argala uses the four standard intervention/obstruction pairs and the applicable malefic-third exception. When Argala facts exist but no pair can be judged, its index is 50.

**Missing behavior:** the mean ignores unavailable components; Context is null only when no component is readable. Python must use the persisted Context value and must not attempt to reconstruct it from the three stored Argala counts because pair-level judged availability is not fully represented by those counts alone.

### `KI_D1_HOUSE_SUPPORT_V1`

**Canonical C# property:** `HouseStatistics.StrengthPercent`
**SQL column:** `StrengthPercent`

```text
Overall Support = round_away_from_zero(mean(non-null Capacity, Consistency, Context))
```

Each readable axis receives equal weight regardless of how many component signals its own formula contains.

**Missing when:** all three axes are missing.
**Interpretation direction:** higher means stronger combined evidence under this presentation scale; it is not a probability.

## Proposed features: avasthas and argala (2026-10-17, not persisted, not frozen)

Nothing in this section is part of Feature Dictionary v1; the four D1 measures above are unchanged. These are candidates
for Phase 1, recorded here so the definitions are agreed before any column is added. Background and sourcing:
`docs/research/domain/argala-avastha-linkage.md`.

| Proposed code | Grain | Definition | Scale | Source | Missing when |
|---|---|---|---|---|---|
| `KI_PLANET_ALERTNESS_V1` | planet x chart | Jagradadi state: Jagrat = 2, Swapna = 1, Sushupti = 0 | Ordinal, no weights (PVR gives only full / medium / negligible) | PVR §15.4.2 | No dignity for the planet |
| `KI_PLANET_DAMAGE_FLAGS_V1` | planet x chart | Which of Lajjita, Kshudhita, Kshobhita hold | Three booleans | PVR §15.4.3 | No conjunction/aspect rows for the chart |
| `KI_HOUSE_ARGALA_DELIVERY_V1` | house x chart | Per lane, the best alertness among the argala planets; per house, the count of lanes by delivery | Ordinal counts | PROJECT_SYNTHESIS on PVR §15.4.2 | No argala planet, or no alertness for any |
| `KI_HOUSE_ARGALA_DECIDED_BY_WEAK_V1` | house x chart | The argala result is a one-planet margin (or was level and decided by dignity) and a deciding planet is Sushupti or damaged | Boolean | PROJECT_SYNTHESIS | Outcome is Quiet, Idle or Tie |

Rules for these features:

- They **do not alter** `KI_D1_HOUSE_CONTEXT_V1`. Its Argala component stays the share of judged pairs that hold; the
  winner stays count first, dignity second (PVR §10.7).
- `KI_PLANET_ALERTNESS_V1` is a recoding of dignity. It must be flagged as redundant with the dignity score in any model.
- Baaladi and Sayanaadi are D1-only and are not proposed as features here; if added later they carry `ChartScope = D1`.
- A missing value is never replaced by zero (see Shared numeric rules). Sushupti is a real zero on the ordinal scale;
  "alertness not available" is a missing reason.

## Shared numeric rules

`LifeMatterStatistics` applies these rules before persistence:

- Intermediate ratio indices are clamped to 0–100.
- Means ignore null values.
- A mean is null when no input is readable.
- Integer rounding uses `MidpointRounding.AwayFromZero`.
- Presentation rounding must not change the stored value used for comparison.

## Required dimensions and provenance

| Field | Source | Requirement |
|---|---|---|
| Anonymous subject key | Future analytics eligibility layer | Required before Python person-level extraction |
| `BirthDetailId` | View/table relationship | Internal join only; not an exported public identifier |
| `ChartResultId` | Statistics table | Required |
| `ChartType` | `tbl_ChartResults` | Must be `D1` for v1 |
| `HouseFromLagna` | Statistics table | Required, 1–12; defines comparison stratum |
| `SignId` / Sign | Statistics table/view | Required for audit and explanation |
| `LordPlanetId` / Lord | Statistics table/view | Required for audit and explanation |
| `RuleSetId` | Statistics and chart | Required and must match |
| Ayanāṁśa | `tbl_ChartResults.Ayanamsha` | Required dataset stratum |
| House system | `tbl_ChartResults.HouseSystem` | Required dataset stratum |
| Engine version | `tbl_ChartResults.EngineVersion` | Required provenance |
| Calculation kind | `tbl_ChartResults.CalculationKind` | Must be `PositionChart` |
| Chart computed time | `tbl_ChartResults.ComputedAt` | Required provenance |
| Statistics computed time | `ComputedAtUtc` | Required provenance; must not predate chart |
| Feature-contract version | Dataset definition | Required; `V1` for these codes |

## Missing-reason requirement

The operational fact table currently stores nulls but no controlled missing-reason code. The analytics extraction contract must derive or add one of:

- `SOURCE_NOT_COMPUTED`
- `SOURCE_NOT_APPLICABLE`
- `SOURCE_PARTIAL`
- `CALCULATION_FAILED`
- `VERSION_INCOMPATIBLE`
- `UNKNOWN`

No missing value may be replaced by zero. The extraction must publish both eligible and measured denominators.

## Known contract risks

1. `HouseStrengthStatisticsService` persists lord-only statistics, while live matter readings may add kārakas. The two must not share an unlabeled percentile.
2. `HouseStrengthStatisticsService.InsertAll` currently receives the active rule set from `RuleSetRepository`, not the individual chart's `RuleSetId`. The audit found no mismatch today, but Phase 1 must either enforce equality or persist the chart's rule set directly.
3. `ComputedAtUtc` versions a row in time but does not identify a Git commit or explicit feature-contract version.
4. The view exposes `Name`; Python's analytics view must exclude it and use an anonymous subject key.
5. Null component reasons are not persisted.
6. The current cohort contains only six people, so all statistical display remains in the insufficient-data state.
7. Avasthas overlap dignity, conjunctions and aspects (see the proposed-features section). Any future feature that combines
   an avastha with one of those inputs double counts it unless the overlap is removed first.

## WP0.2 acceptance checks

- [x] Four v1 features have stable codes.
- [x] Canonical C# and SQL owners are identified.
- [x] Units, ranges, formulas and rounding are documented.
- [x] Missing behavior is documented.
- [x] D1 and lord-only scope is explicit.
- [x] Required provenance is listed.
- [x] Live matter-specific scores are distinguished from persisted house baselines.
- [x] Phase 1 contract risks are recorded.

WP0.2 is complete. WP0.3—research eligibility, anonymisation and deletion governance—is the next Phase 0 gate.
