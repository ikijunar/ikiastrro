# Groundwork (Plan 1) outcomes — carry-forward for Plan 2 (UI phases)

Plan 1 (`2026-08-30-web-ui-recreate-groundwork.md`) is merged to `master`
(fast-forward `363eeeb..73c0128`, 11 commits). All tasks reviewed + one
whole-branch review + one fix wave, all clean. `dotnet build` 0/0 (CLI and
VS 2026), `verify-vargas` + `verify-functional-nature` pass, `backfill-charts`
idempotent, `compute-all Ramakrishnan` reproduces every reference-chart fact,
migration 031 = 84 rows / 3 NULL / 6 Yogakaraka.

## Interface changes Plan 2 binds to

- `VedicHoroGen.Core.Calculators.ChartViewModel` / `.PlanetRow` (renamed from `D1*`).
- `VedicHoroGen.Core.Calculators.LagnaFunctionalNature.For(ZodiacName, PlanetName) : FunctionalNatureResult`
  (`enum FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }`; record fields
  `Nature, RuledHouses(int[]), IsMaraka(bool), KendradhipatiDosha(bool), Rationale(string)`).
  Throws `ArgumentOutOfRangeException` for Rahu/Ketu, `InvalidOperationException` if sign
  rulership can't be resolved.
- `VedicHoroGen.Core.LifeAreas.LifeArea` (enum) + `.LifeAreaSpec` (record: `int[] Houses,
  string[] Karakas, string[] Vargas, string DefaultVarga`) + `.LifeAreaMap.Specs`.
  **Namespace is `LifeAreas` (plural)** — a `Core.*` consumer that imports both
  `VedicHoroGen.Core` and this must use the plural or fully-qualify.
- `ChartKeyDetailsRepository/ChartHouseLordsRepository/ChartConjunctionsRepository/
  ChartAspectsRepository.GetByBirthDetailId(int)` — every row for a person across all
  chart types, **ordered by `Id` (chart types interleaved)** → group by `ChartResultId`,
  do not assume contiguity.
- `SadeSatiRepository.GetByBirthDetailId(int) : IReadOnlyList<SadeSatiPeriod>`.
- `HouseNakshatraSpanRepository.GetByChartResultId(int) : IReadOnlyList<HouseNakshatraSpanRow>`.
- `PlanetSignTransitEventsRepository.GetSnapshot(PlanetName, DateTime) : PlanetTransitSnapshot?`.
- `PlanetsReferenceRepository.GetAll()`, `SignAttributesRepository.GetAll()`,
  `NakshatraReferenceRepository.GetAll()/GetPadas()/GetSubLords()`.
  Note: `tbl_SignAttributes` has no `RulingPlanetNature` column live; `SignAttributeReference`
  aliases `type_house_element`→`HouseElement`, `type_house_keyattri`→`HouseModality`.
- `ChartGenerationService` (DI-registered in Web) — `GenerateAll/GenerateMissing/
  RecomputeAnalytics(BirthDetails[, string? filter]) : GenerationReport`. `Add.razor`
  already uses `GenerateAll`. New CLI modes: `compute-all <name>`, `compare-functional-nature`.
- Migration `031` `dbo.tbl_Dim_LagnaFunctionalNature` + `LagnaFunctionalNatureRepository.GetForLagna(byte)`.

## Functional-nature: heuristic vs. book table

`LagnaFunctionalNature.For` (computed) and `tbl_Dim_LagnaFunctionalNature` (Raman's book)
**diverge on 17 of 84 cells** (+ 3 the book leaves unclassified). Pattern: every divergence
is a mixed-lordship planet on the Neutral boundary — the heuristic never emits `Neutral`
for these, and it downgrades 5 book-`Benefic` natural malefics (Mars ×3, Mercury, Jupiter)
to `Malefic`. **No polarity contradictions (no Benefic↔Malefic flips), no Yogakaraka flips.**
This is expected/documented. `FunctionalNaturePanel` (spec §7.3) shows both side by side;
`dotnet run --project src/VedicHoroGen.Cli -- compare-functional-nature` prints the full set.

## Parked findings — address in Plan 2 or note as accepted

1. **`GenerateAll` orphans analytics for de-registered chart types.** It clears the 4
   analytics tables by `BirthDetailId` (all types) but deletes `ChartResults` only for
   currently-registered calculators. A `ChartResult` from a removed calculator would keep
   an empty analytics set forever. Not reachable today (no calculator has been removed).
   Add a guard/warning if Plan 2 ever makes the calculator set configurable.
2. **`GenerationReport.Skipped`** is populated by `GenerateMissing` and read by nothing.
   Surface it in a UI/CLI report or drop the field.
3. **No web "regenerate charts" affordance.** The CLI has `compute-all` for post-correction
   recompute; `Add.razor`'s `catch` can leave a `BirthDetails` row with partial charts and
   the Web user has no retry. Add a "Regenerate" action on the chart workspace, or state in
   the spec that web recovery is via the CLI.
4. **`GetByBirthDetailId` ordering** — see interface note above; Plan 2's `ChartWorkspace`
   must group by `ChartResultId`.
5. **`backfill-analytics` and `recompute-keydetails` are now identical code paths** (the
   comment says so). Consolidation was deferred from the fix wave — optional CLI cleanup.
6. Accepted as-is: migration 031 unconditional `DROP TABLE` on re-run; `Id TINYINT IDENTITY`
   (84 rows, cap 255) — fine for a seeded dim table.
7. `SELECT *` in the reference/functional-nature repos relies on Dapper constructor-name
   mapping to positional records (matches sibling repos) — not a defect.

## Verification cadence to keep in Plan 2

No test project. Gates: `dotnet build` 0/0 · `verify-vargas` + `verify-functional-nature`
exit 0 (add UI-relevant assert modes if a pure function warrants it) · the
`1_Ramakrishnan` reference chart as golden record · a live browser smoke test
(machine Application Control blocks direct `.exe` launch — use `dotnet exec` on the built
Web DLL, or `dotnet run --project src/VedicHoroGen.Web`).
