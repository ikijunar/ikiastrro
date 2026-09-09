---
last_updated: 2026-09-09
workstream: database
togaf: C — Data Architecture
---

# Database — schema

SQL Server, Windows Auth. Database `ikiastrro`. Connection via
`SqlConnectionFactory` (connection-per-call). Dapper, hand-written SQL, no ORM, no unit of
work. One repository per table/view in `src/Ikiastrro.Data/`.

## Migration policy

- **Baseline** `db/ikiastrro.sql` — whole schema + reference/master seed + the
  `tbl_Dim_LifeCalendar` day dimension.
- **Forward changes** are numbered scripts `db/NN_<slug>.sql`, applied in order, each
  appending its `ScriptName` to `dbo.SchemaMigrations`. Active range `22`–`054`.
- **Never edit an applied migration.** A change is a new script. A rule change is a new
  `RuleSetId`, not an `UPDATE`.
- `db/_archive/` holds the pre-consolidation `001..034` chain (frozen, historical).
- `db/checks/` — read-only inspection scripts (e.g. `check_ayanamsa_dasha_benchmarks.sql`).

## Table groups

| Group | Tables | Filled by |
|---|---|---|
| Input | `tbl_BirthDetails` (incl. `Sex`) | user (CLI / `Add.razor`) |
| Chart results | `tbl_ChartResults` — one row per person × chart type, one per dasha run; carries `AyanamshaDegrees`, `SiderealTimeHours`, `VargaMethod`, `RuleSetId`, `ResultJson` (frozen audit snapshot) | engine |
| Chart analytics (chart-type-generic, keyed by `ChartResultId` + `ChartType`) | `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_MultiGrahaConjunction` / `…Member`, `tbl_Chart_Aspects` | `ChartAnalyzer` |
| Dasha | `tbl_Chart_DashaPeriods` (self-referencing via `ParentDashaPeriodId`, 3 levels, age-relative + absolute dates) | `VimshottariDashaService` |
| Reference / master | `tbl_Planets` (9), `tbl_SignAttributes` (12), `tbl_Nakshatras` (27), `tbl_NakshatraPadas` (108), `tbl_NakshatraSubLords` (243, KP L1–L2), `tbl_PlanetSignTransitEvents` (Sa/Ju/Ra sign-crossing log 1930–2060), `tbl_TransitPositionReference` | seed / CLI backfill |
| Rules engine (versioned; every row carries `RuleSetId`) | see [`rules-engine.md`](rules-engine.md) | seed |
| Dimensions | `tbl_Dim_LifeCalendar`, `tbl_Dim_PlanetaryState`, `tbl_Dim_ChartType`, `tbl_Dim_Source`, `tbl_Dim_LifeArea` / `House` / `HouseCategory` / `HouseReference` / `SubPlanets` / `SpecialLagnas` / `DivisionalSubject` / `InterpretationDimension` / `GrahaAttribute`, `tbl_Dim_AyanamsaBenchmark*`, `tbl_Dim_DashaSystems` / `DashaBenchmarkPeriods` | seed / CTE |
| Facts (per chart, star-schema) | `tbl_Fact_PlanetaryState`, `tbl_Fact_PlanetaryStrength` / `…Component`, `tbl_Fact_BhavaStrength` / `…Component`, `tbl_Fact_Vargottama`, `tbl_Fact_YogaInputEvaluations`, `tbl_Fact_HouseFromReference`, `tbl_Fact_Ayanamsa*` / `Dasha*Comparisons` | computers via `ChartGenerationService` |

## Chart-generic analytics — the design

`tbl_Chart_*` store *computed results for a person's chart* (vs `tbl_Rule_*` / `tbl_Dim_*`
which store the classical rules). Every one carries a `ChartType` column alongside
`ChartResultId` / `BirthDetailId`, so it reads standalone.

- **`tbl_Chart_KeyDetails`** — one row per planet (+ Ascendant) per chart. Grouped columns:
  keys → planet → `NirayanaLongitudeDegrees` / `VargaLongitudeDegrees` (`Normalize(realLon × N)`)
  / `EclipticLatitudeDegrees` / `SpeedLongitudeDegPerDay` / `IsRetrograde` → `Sign` /
  `DegreesInSignDecimal` (`= VargaLongitudeDegrees mod 30`, populated every chart type) →
  nakṣatra block → 3 house reckonings → dignity (`DignityStatus`: Exalted · Moolatrikona ·
  Own Sign · Great Friend · Friend · Neutral · Enemy · Great Enemy · Debilitated) → combustion
  → aspecting planets → `CharaKaraka` (`AK`…`DK`) → `PointKind` (`Graha` / `SpecialLagna` /
  `Arudha` / `Upagraha`).
- **`tbl_Chart_HouseLords`** — per house (1–12): occupying sign, its ruler, and where that
  ruler sits (house / sign / dignity), from Lagna, Sun and Moon.
- **`tbl_Chart_Conjunctions`** + **`tbl_Chart_MultiGrahaConjunction` / `…Member`** — pair rows
  plus an explicit ≥ 2-graha group layer (per-planet degree / longitude / dignity / retrograde
  / combust once, D1 span + per-member orb, `MemberKey` for yoga-subset matching).
- **`tbl_Chart_Aspects`** — one row per directional graha dṛṣṭi (7th from every graha; Mars
  +4/8, Jupiter +5/9, Saturn +3/10; Rahu/Ketu use the Jupiter-style 5/7/9 convention).

**D1-gated** (only meaningful for a continuous-degree rasi chart): `Nakshatra` /
`NakshatraPada` display, conjunction `DegreeSeparation`. Everything else is populated for
every chart type.

## Views & functions

- Views: `vw_Chart_Consolidated`, `vw_Chart_DashaTimeline`, `vw_Chart_HouseNakshatraSpan`,
  `vw_KetuSignTransitEvents`, `vw_NakshatraPadaDetails`, `vw_ChartPlanetEvidence`,
  `vw_ChartMoonContext`, `vw_ChartShadbala`, `vw_ChartBhavaBala`, `vw_ChartYogaEvaluations`,
  `vw_YogaChartApplicability`, `vw_YogaContextRequirements`, `vw_Dignity_Legend`.
- Functions: `fn_GetNakshatraRulingPlanetId` (scalar); `tvf_Chart_LifeWeeks(@BirthDetailId)`,
  `tvf_Chart_SadeSatiPeriods(@BirthDetailId)`, `tvf_PlanetSignAtDate(@PlanetId, @AsOfUtc)`
  (inline TVFs).

## Reference / master data

Seeded and cross-checked against the engine's hard-coded lookups, **not yet read by the
engine**. Deliberately NULL pending a cited source: `tbl_SignAttributes.RisingType`;
`tbl_Nakshatras` Guna / Gana / Yoni / Nadi / Varna / Tatva / Direction. Full design and
sourcing status live with the domain research (`docs/research/domain/`).
