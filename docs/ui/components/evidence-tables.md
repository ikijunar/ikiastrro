---
last_updated: 2026-09-09
workstream: ui
component: AstrologerEvidence
route: /charts/{id}/evidence
togaf: C — component spec
---

# Component — astrologer evidence tables

One read-only, table-only page for inspecting the calculated and reference evidence already
stored in SQL Server. Follows an astrologer's reading order; adds no interpretation and no
second calculation path — it renders persisted rows and views only
([`../../architecture/domain-contracts.md`](../../architecture/domain-contracts.md)).

## Reading order (sections)

1. **Birth & calculation context** — person, sex, birth moment/place, ayanāṁśa + degrees,
   house system, rule set, computation timestamp.
2. **Moon, tithi & lunar context** — Sun/Moon longitude, elongation, tithi number, paksha,
   waxing / full-Moon state, day/night, calculation-policy provenance.
3. **Planetary positions** — graha / Lagna, rāśi, longitude, house, nakṣatra/pada, motion,
   combustion, sign lord, Chara Karaka. **D1 first, with a chart selector for the vargas.**
4. **Rāśi characteristics** — the 12 `tbl_SignAttributes` rows in zodiac order.
5. **Graha characteristics** — `tbl_Planets` + active `tbl_Rule_GrahaAttribute` values.
6. **Dignity & avastha** — position dignity, combustion/retrogression, age state, wakefulness
   state.
7. **Shadbala** — the seven planet totals, then auditable component rows and minimum strength.
8. **Bhava Bala** — 12-house totals + components.
9. **Yoga evaluations** — per source variant: source, entry number, outcome, and
   `NOT_EVALUATED` reasons where P0 context is missing.

## Sources

`vw_Chart_Consolidated`, `vw_ChartMoonContext`, `vw_ChartPlanetEvidence`, `vw_ChartShadbala`,
`vw_ChartBhavaBala`, `vw_ChartYogaEvaluations`, plus the reference dimension tables. Chart
selector switches section 3+ between D1 and any stored varga.

## Rendering

MudBlazor tables, shared tokens, tabular numerals. No charts on this page — it is the raw
evidence surface that the visual pages summarise.
