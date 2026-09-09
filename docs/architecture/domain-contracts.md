---
last_updated: 2026-09-09
togaf: C — Interface catalogue
safe: Solution Intent — cross-stream contract
---

# ikiastrro — domain contracts (DB + CLI ↔ UI)

The interface between the value streams. The **DB + CLI** stream publishes; the **UI** stream
consumes. Neither stream changes the other's paths.

## What the UI may read

Persisted rows only — the UI never runs `Ikiastrro.Core` calculators:

| Surface | Source |
|---|---|
| Birth record | `tbl_BirthDetails` (incl. `Sex`) |
| Chart provenance | `tbl_ChartResults` (`AyanamshaDegrees`, `SiderealTimeHours`, `VargaMethod`, `RuleSetId`, `ChartType`) |
| Planet placement + analytics | `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions` (+ `tbl_Chart_MultiGrahaConjunction*`), `tbl_Chart_Aspects` |
| Planetary states | `tbl_Fact_PlanetaryState` |
| Strength | `tbl_Fact_PlanetaryStrength*`, `tbl_Fact_BhavaStrength*`, `tbl_Fact_Vargottama` |
| Yoga | `tbl_Fact_YogaInputEvaluations`, `tbl_Rule_Yoga` + applicability views |
| Dasha | `tbl_Chart_DashaPeriods`, `vw_Chart_DashaTimeline`, `tvf_Chart_LifeWeeks` |
| Transits / Sade Sati | `tbl_PlanetSignTransitEvents`, `tvf_PlanetSignAtDate`, `tvf_Chart_SadeSatiPeriods`, `tbl_TransitPositionReference` |
| Evidence views | `vw_Chart_Consolidated`, `vw_ChartPlanetEvidence`, `vw_ChartMoonContext`, `vw_ChartShadbala`, `vw_ChartBhavaBala`, `vw_ChartYogaEvaluations` |
| Ayanāṁśa options | `tbl_Rule_Ayanamsa` (system default + catalogue) |
| Terminology | `tbl_Astro_Terminology` (+ `_Text`) via `TerminologyCatalog` |

The Data-layer read repos (`*Repository.GetByBirthDetailId`, `ChartResultsRepository`, …) are
the only sanctioned access; the UI batch-loads through `WorkspaceData`.

## Rules the contract enforces

1. **A calculation the UI needs is a DB + CLI feature first** (`DB` + `Core` + `Verify`), then a
   UI feature (`Web`). The UI never adds a computation.
2. **Chart data must be regenerated after any ayanāṁśa / rule-set / engine change** before the
   UI reflects it — `compute-all <name>` or `backfill-charts` / `recompute-keydetails`. Stored
   rows are a snapshot, not live. (Open: `FEAT-DATA-04`.)
3. **Schema is additive** — a new typed column or table, never a reshape — so a DB feature can
   land before the CLI/UI that reads it, and an old UI keeps working.
4. **`NirayanaLongitudeDegrees` is populated (`NOT NULL`) for every chart type**; the varga
   sign is `IVargaSignRule`, not `FLOOR(VargaLongitudeDegrees/30)`.
5. **Non-`Graha` `PointKind` rows carry no graha-only analytics** (`CK_KeyDetails_NonGrahaNulls`).
6. **Deleting a person cascades** through `BirthDetailDeletionService` (analytics leaves →
   `tbl_ChartResults` → `tbl_BirthDetails`); the UI calls the service, never raw deletes.

## Provenance the UI must surface

Wherever a chart is shown: the ayanāṁśa name + degrees, sidereal time, house system, rule-set
id, and computation timestamp — from `tbl_ChartResults`, not recomputed.
