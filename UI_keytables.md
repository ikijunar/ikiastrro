---
last_updated: 2026-09-09
---

# Astrologer evidence tables UI

## Objective

Provide one read-only, table-only page at `/charts/{birthDetailId}/evidence` for testing
the calculated and reference evidence already stored in SQL Server. The page follows an
astrologer's reading order and does not add interpretation or a second calculation path.

## Reading order

1. Birth and calculation context: person, sex, birth moment/place, ayanamsa, house system,
   rule set and computation timestamp.
2. Moon, tithi and lunar context: Sun/Moon longitude, elongation, tithi number, paksha,
   waxing/full-Moon state, day/night and calculation-policy provenance.
3. Planetary positions: graha/Lagna, rasi, longitude, house, nakshatra/pada, motion,
   combustion, sign lord and Chara Karaka. D1 first, with a chart selector for vargas.
4. Rasi characteristics: the 12 `tbl_SignAttributes` rows in zodiac order.
5. Graha characteristics: `tbl_Planets` plus active `tbl_Rule_GrahaAttribute` values.
6. Dignity and avastha: position dignity, combustion/retrogression, age state and
   wakefulness state.
7. Shadbala: seven planet totals followed by auditable component rows and minimum strength.
8. Bhava Bala: twelve house totals followed by their three component rows.
9. Structural evidence: house lords, conjunctions and aspects.
10. Vargottama/divisional confirmation.
11. Yoga summary, all source variants, chart requirements and context requirements.
12. Vimshottari Dasha timeline.

## Data design

Migration 053 adds focused, non-multiplying views:

- `vw_ChartMoonContext`
- `vw_ChartPlanetEvidence`
- `vw_ChartShadbala`
- `vw_ChartBhavaBala`
- `vw_ChartYogaEvaluations`

Relationship, reference, component and Dasha tables remain separate queries because joining
their one-to-many rows would multiply evidence. One `AstrologerEvidenceRepository` opens one
connection and returns named table sections. One generic Razor table renders every section.

## UI behavior

- Plain sortable/scrollable tables with section headings and a sticky section index.
- D1 selected first; chart type selection changes chart-specific sections.
- Yoga filters: status, source and text search.
- `PRESENT`, `ABSENT` and `NOT_EVALUATED` remain textual states; missing codes are visible.
- Empty sections show `No calculated data`.
- Technical rule/source/method columns appear at the end for audit.

## Excluded from the reading page

Schema migrations, raw rule catalogues, benchmark fixtures, terminology storage and transit
reference tables are infrastructure. Their relevant rule set, source and method identifiers
are exposed on evidence rows instead.

## Delivery order

1. Migration 053 and view checks.
2. Generic evidence row/table model and repository.
3. `/charts/{id}/evidence` page in the reading order above.
4. Workspace and saved-chart navigation links.
5. Database count checks, render test and documentation/handoff update.

## Acceptance evidence

- Both saved people load from persisted data without recalculation.
- D1/varga positions match direct SQL counts.
- All seven Shadbala and twelve Bhava Bala summaries appear when calculated.
- All 337 yoga variants appear per saved D1 chart with matching status counts.
- Tithi boundary arithmetic is tested at every 12-degree transition.
- No per-row database queries are issued.

## Progress

- [x] Database inventory and reading order agreed.
- [ ] Migration 053 and checks.
- [ ] Repository and generic table renderer.
- [ ] Evidence page and navigation.
- [ ] Verification against live charts.
