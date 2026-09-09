---
last_updated: 2026-09-09
workstream: cli
togaf: C — Application Architecture
---

# CLI — command catalogue

`dotnet run --project src/Ikiastrro.Cli -- <mode> [args]`. No args → the interactive add flow.
Optional `--db <name>` overrides the target database. `dotnet build` / `dotnet run` /
`dotnet test` run from the terminal.

## Entry & generation

| Mode | Purpose |
|---|---|
| *(no args)* | Prompt Name / DOB / time / place (+ optional corrected time), resolve lat-long-offset, run the production yoga composition, compute + store all 21 charts + Vimśottari dasha via `ChartGenerationService`, print |
| `compute-all <name>` | Regenerate every chart type + dasha for one saved person (e.g. after a birth-time correction or an ayanāṁśa change) |
| `compute-dasha <name>` / `show-dasha <name>` | Recompute + print / print stored dasha tree |
| `compute-all` variants | `backfill-charts` (add any missing chart type, idempotent) · `backfill-analytics` / `recompute-keydetails` (re-derive the analytics tables) · `backfill-dasha` (bulk) |
| `recompute-keydetails` | Re-derive `tbl_Chart_KeyDetails` for every calculable chart type (e.g. after a column is added) |

## Reference data

| Mode | Purpose |
|---|---|
| `precheck-planet-transits` / `backfill-planet-transits` | Dry-run vs real fill of `tbl_PlanetSignTransitEvents` (Saturn / Jupiter / Rahu, 1930–2060) |
| `backfill-planet-transits` | ~27 s; Saturn ~109 rows, Jupiter ~229, Rahu 84 (zero re-entries — a correctness signal) |
| `seed-terminology` / `verify-terminology` | Seed `tbl_Astro_Terminology` from the catalogue / assert coverage (`--emit-sql` prints without writing) |
| `seed-rule-params` | Backfill `RuleParametersJson` on the 20 varga schemes |
| `list-rule-sets` / `show-rules <id>` | Inspect the `tbl_Rule_*` layer |
| `backfill-analytics` | Fill analytics for `tbl_ChartResults` rows that already exist |

## Verification (the regression gate — exit 1 on any FAIL)

| Mode | Checks | Now |
|---|---|---|
| `verify-schema` | FK integrity, id population, range invariants, conjunction-group consistency | **PASS** |
| `verify-pipeline` | `ChartPipeline` end-to-end against seed person 1, no DB write | **PASS** |
| `verify-rules` | every `RuleParametersJson` round-trips to its C# rule over 360° | **PASS** |
| `verify-sources` | every `SourceRefCode` resolves in `tbl_Dim_Source` | **PASS** |
| `verify-dignity` | PVR dignity tiling, `DignityScore` / `RelationshipScore` ladders, `tbl_SignAttributes` cross-check | **PASS** (2 documented PVR divergences) |
| `verify-terminology` | terminology coverage | **PASS** |
| `verify-avastha` | Bālādi / Jāgradādi worked examples | **PASS** |
| `verify-functional-nature` | `LagnaFunctionalNature` worked examples | **PASS** |
| `verify-upagrahas` | live rule loading + in-memory upagraha output, all 21 charts | **PASS** |
| `verify-vargas` | hand-computed `IVargaSignRule` checks + **the JHora export grid** (180 cells) | **PASS** (on the Lahiri default, `FEAT-DATA-04`) |
| `verify-jaimini` | HL / Gulika / Maandi longitudes + Chara Karakas vs the JHora export | **PASS** |

## Tests

`tests/Ikiastrro.Yoga.Tests` (124) — yoga engine, shadbala, sub-planet, transit selection.
`tests/Ikiastrro.Web.Tests` (166, bUnit) — golden-SVG snapshots of the hand-rolled chart
components (`docs/artifacts/ui/README.md`; mint with `IKIASTRRO_UPDATE_SNAPSHOTS=1`).

## Golden record

**`1_Ramakrishnan`** (22 Apr 1981, 05:30, Chennai; Aries Lagna, Moon debilitated in Scorpio)
— a ~12-item fact checklist re-confirmed after any engine or UI change.
