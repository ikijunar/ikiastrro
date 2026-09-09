---
last_updated: 2026-09-09
workstream: database
togaf: C — Data Architecture
safe: Solution Intent (fixed)
---

# Database — rules engine

Classical rules as versioned data. Decision: [`decisions/001-star-schema-rules-engine.md`](../../decisions/001-star-schema-rules-engine.md).

## The three infixes (`STANDARDS.md` §D.1)

| Infix | Holds | Example |
|---|---|---|
| `tbl_Dim_*` | vocabulary / catalogue dimensions | `tbl_Dim_PlanetaryState`, `tbl_Dim_Source`, `tbl_Dim_ChartType` |
| `tbl_Rule_*` | a classical rule, versioned by `RuleSetId` | `tbl_Rule_NaturalRelationship`, `tbl_Rule_VargaScheme` |
| `tbl_Fact_*` | a computed result for one chart | `tbl_Fact_PlanetaryState`, `tbl_Fact_PlanetaryStrength` |

## Versioning contract

- `tbl_Rule_Sets` is the version dimension every rule table hangs off. Active set:
  `Parashari-Classical` (Id 1).
- **A rule row is immutable once any fact references it.** A convention change ships a new
  `RuleSetId` and its full row set — never an `UPDATE`.
- Every `tbl_Rule_*` row carries a **portability tail**: `MethodCode`, `RuleParametersJson`,
  `CalculationNarrative`, `SourceRefCode` (→ `tbl_Dim_Source`), `IsActive`.
- `tbl_Rule_Catalog` is the one-page index of "what a port must reimplement": per table, the
  consuming engine, the `MethodCode` families its rows use, where it was introduced.

## Rule tables

| Table | Rows | Rule captured | Live? |
|---|---|---|---|
| `tbl_Rule_VargaScheme` | 20 | per varga: `DivisionFactor`, `MethodCode`, `SignRuleKind`, `SignRuleKey` | **yes** — orchestrator builds one `VargaCalculator` per row |
| `tbl_Rule_AspectOffset` | 19 | graha dṛṣṭi house offsets | mirror |
| `tbl_Rule_CombustionOrb` | 6 | direct / retrograde combustion orbs per planet | mirror |
| `tbl_Rule_NaturalRelationship` | 42 | Naisargika Maitrī friend/neutral/enemy grid | mirror |
| `tbl_Rule_TemporaryFriendshipDistance` | 12 | Tatkālika Maitrī sign-distance rule | mirror |
| `tbl_Rule_AgeState` | — | Bālādi degree bands + effect fraction | live (`AgeStateCalculator`) |
| `tbl_Rule_WakefulnessState` | — | Jāgradādi dignity → waking-state map | live (`WakefulnessStateCalculator`) |
| `tbl_Rule_GrahaDignity` | — | PVR Table 6 dignity segments + special degrees | live (`PvrDignityEvaluator`) |
| `tbl_Rule_CompoundRelationship` | — | Pañchadhā Maitrī compound tiers | live |
| `tbl_Rule_GrahaAttribute` / `tbl_Dim_GrahaAttribute` | — | normalized graha character grid | seeded |
| `tbl_Rule_DigBala` | — | directional-strength reference points | seeded |
| `tbl_Rule_ShadbalaComponent` / `tbl_Rule_BhavaBalaComponent` | — | PVR-first strength formula profile + provenance | live (`ShadbalaCalculator` / `BhavaBalaCalculator`) |
| `tbl_Rule_VimsopakaWeight` | reserved | four varga-group weights | unseeded |
| `tbl_Rule_SubPlanetSunLongitude` / `SubPlanetTime` / `SubPlanetPartRuler` | — | 11 upagraha longitude/time-point rules (PVR: Gulika = midpoint, Maandi = start) | live (`SubPlanetCalculator`) |
| `tbl_Rule_SpecialLagnaFraction` / `SpecialLagnaTimeRate` | — | HL and other special-lagna rates | live (HL only) |
| `tbl_Rule_Ayanamsa` | 22 | JHora ayanāṁśa catalogue + system default | live (`AyanamsaDefinition`) |
| `tbl_Rule_Yoga` / `tbl_Rule_YogaChartApplicability` / `tbl_Rule_YogaContextRequirement` | — | source-attributed Raman 1–300 + PVR yoga corpus, D1/D9 requirements, sex/day-night/phase/exact-longitude context | in progress |
| `tbl_Rule_Karaka` / `tbl_Rule_HouseSignification` / `tbl_Rule_HouseReferenceMatter` / `tbl_Rule_HouseAttribute` | — | house + karaka reference rules | seeded |
| `tbl_Rule_AgeState` … + reserved: `tbl_Rule_DashaApplicability`, `tbl_Rule_NaturalRelationship` compound tail | — | — | — |

## Divisional-chart portability

`RuleParametersJson` on `tbl_Rule_VargaScheme` carries a `"method"` key so a non-C# port
copies the table and reimplements **three** interpreters, not 20 rule classes:

- `LINEAR_VARGA` — `{factor, stride}` (D3, D4, D12, D60)
- `GRID_VARGA` — a `parts × 12` sampled sign grid (the bespoke `Special` rules)
- `BAND_VARGA` — `{edges, map}` (D30 unequal 5-part)

`verify-rules` proves every `RuleParametersJson` round-trips to its C# rule's output over the
full 360°.

## Facts

`tbl_Fact_*` rows record which `RuleSetId` produced them, so a chart's evidence is traceable
to the exact rule version. Written by the `*Computer` classes inside
`ChartGenerationService.PersistAnalytics`.
