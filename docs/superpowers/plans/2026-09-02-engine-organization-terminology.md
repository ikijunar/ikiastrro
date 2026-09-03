# Engine Reorganization + Terminology + Rule-Table Portability — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **Execution outcome (2026-09-03):** all 14 tasks landed on `feat/ikiastrro-workspace-ui`, commits `2271eec`..`dd2ad67` (subagent-driven; one fix round each on Task 10 and Task 12). Build 0/0; `verify-schema/vargas/functional-nature/jaimini/avastha/sources/pipeline/terminology/rules` all `ALL PASS`; Web smoke `/charts/1` → 200. Deviations from the plan text, all recorded in the SDD ledger: **(a)** `Ikiastrro.Core.Models` kept plural — `PlanetPosition` + the `*Reference` DB-row records stayed there (minimal blast radius); `Ikiastrro.Core.Reference` holds terminology types only. **(b)** The 4 planned varga interpreters (`LINEAR`/`TABLE`/`BAND`/`WRAPPED_VARGA`) shipped as **3** — `TABLE_VARGA` + `WRAPPED_VARGA` collapsed into `GRID_VARGA` (a `parts × 12` sampled sign grid; a `{"closedForm":…}` pointer isn't portable). The interpreter tag is a `"method"` key inside `RuleParametersJson`; `tbl_Rule_VargaScheme.MethodCode` was left holding its classical-scheme name. **(c)** `ChartGenerationService.GenerateAll` does not yet call `ChartPipeline` (its `ChartBundle` shape lacks per-chart `ChartResult` rows) — `verify-pipeline` is the pipeline's only consumer for now; adoption deferred to a later plan. **(d)** Avastha rename touched table/column/symbol names only — every `StateName`/`AvasthaSystem` row value is unchanged; the English names live as `en` rows in `tbl_Astro_Terminology`. **(e)** `dbo.SchemaMigrations` is keyed by `ScriptName` (not `MigrationId`); the baseline seeds no rows into it. Task 13 (downstream `using` sweep) was a no-op — the sweep happened incrementally in Tasks 1–12.

**Goal:** Re-layer `Ikiastrro.Core` into named engines under `Engines/<Name>/`, add a normalized bilingual terminology table, make every `tbl_Rule_*` table self-describing enough that a port copies table data instead of re-deriving rules from C#, and expose a DB-free `ChartPipeline` façade — all behaviour-preserving.

**Architecture:** One folder + one namespace per engine (`Ikiastrro.Core.Engines.<Name>`), build order bottom-up (Astronomy → Position → DivisionalCharts → Houses → Nakshatras → Dignity → Karakas → PlanetaryStates → Relationships → Dasha), each move gated by `dotnet build` + the existing `verify-*` suite. `ChartAnalyzer` splits into `HouseEngine` / `NakshatraEngine` / `RelationshipEngine` / `DignityEngine` / `CombustionEngine` + a thin `Pipeline/ChartAnalyzer` composer (verbatim lift, no logic edits). Two new terminology tables (`tbl_Astro_Terminology` + `_Text`) seeded `sa`+`en` mechanically from the existing enums / `tbl_Dim_*` / `tbl_Rule_VargaScheme`. A portability tail (`MethodCode`, `RuleParametersJson`, `CalculationNarrative`, `SourceRefCode`, `IsActive`) added to every `tbl_Rule_*`, plus `tbl_Rule_Catalog` and five empty rule tables reserved for Plans 2–4.

**Tech Stack:** .NET 8 / C# (top-level statements in CLI, nullable reference types), SQL Server (Windows Auth `localhost`), Dapper, `sqlcmd` migrations with `:setvar DbName`, SwissEphNet 2.8.0.2, Blazor Server (Web).

**Spec:** `docs/superpowers/specs/2026-09-02-engine-organization-terminology-design.md`

## Global Constraints

- **Branch:** `feat/ikiastrro-workspace-ui`. Work directly on it (no worktree — matches Plan 0). **Do NOT `git push`.**
- **Commit trailer, exactly** (two lines, after a blank line):
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE
  ```
- **Migrations:** 2-digit `NN_<verb>_<noun>.sql` in `db/`. Plan 1 adds **16, 17, 18 only**. Every migration: idempotent guards (`IF OBJECT_ID(...) IS NULL` / `IF COL_LENGTH(...) IS NULL`), self-records one row in `dbo.SchemaMigrations`, and is **folded into `db/ikiastrro.sql`** (the baseline) in the same task — the numbered file is kept.
- **No test project.** Verification is `dotnet build Ikiastrro.slnx` at **0 Warning(s) / 0 Error(s)** plus the `verify-*` CLI modes. `dotnet run --project src/Ikiastrro.Cli -- <mode>` exits 0 on pass.
- **Behaviour-preserving.** These golden-value modes must stay byte-identical: `verify-schema`, `verify-vargas`, `verify-functional-nature`, `verify-jaimini`. `verify-avastha` is updated for **renamed symbols only** — same expected strings ("Baala", "Jagrat", …). New modes: `verify-pipeline`, `verify-terminology`, `verify-rules`.
- **No environment token** (`_dev` / `_stage` / `_prod` …) in any `tbl_` / `vw_` / `usp_` / constraint / index name, or in C#.
- **Citations:** anything citing a source names a `SRC_*` code that resolves in `dbo.tbl_Dim_Source` (shipped in Plan 0). No author / title / edition / page strings inline in a rule row, a code comment, a diagram, or an artifact.
- **Naming (STANDARDS §D.2, hybrid):** English for engine / structural / greenfield names; the canonical single-spelling romanized Sanskrit noun for established Jyotiṣa concepts; every concept bound to a stable ASCII `Code`. Once `TerminologyCatalog` exists, **new** code resolves display strings through it — pre-existing hard-coded strings are not swept in Plan 1 beyond what a task already edits.
- **No `db/` history scrub** here (separately pending).
- **No new astrology math.** No Ṣaḍbala, yogas, dispositor traversal, or new avastha slices — those are Plans 2–4. Plan 1 adds interfaces + empty tables only.

## Decisions (resolved from spec §15 + gaps found in self-review)

| # | Decision | Rationale |
|---|---|---|
| D1 | **Keep `Ikiastrro.Core.Models`** (plural). No `Model/` folder. `PlanetPosition` and the five `*Reference` DB-row records stay in `Models`. | Spec §15.1 recommendation; ~15 downstream `using` sites; `Models` is already "pure DB-row shapes". |
| D2 | **`Dignity` is its own engine** (`Engines/Dignity/`). | Spec §15.2; Ṣaḍbala + Avastha both depend on it directly. |
| D3 | `RuleParametersJson NVARCHAR(MAX) NULL` + `CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1)`. | Spec §15.3; portable, no SQL Server 2025 `JSON` type dependency. |
| D4 | Pada terminology codes: `NAKPADA_<NAK>_<n>` (e.g. `NAKPADA_ASHWINI_1`). | Spec §15.4; readable in a debugger. |
| D5 | Env/DB naming (§15.5) and citation registry (§15.6) — **already shipped in Plan 0** (`INFRASTRUCTURE.md`, `tbl_Dim_Source` + `docs/research/reference-sources.md`). No Plan 1 action. | — |
| G1 | **No new avastha enums.** `BaaladiAvastha` / `JagradadiAvastha` / `PlanetAvasthaComputer` are static calculator classes over DB-driven rule rows — there is no enum to rename. Plan 1 renames the classes, model records, tables, and columns per spec §5.4; DB `StateName` and `AvasthaSystem` row values are **unchanged**; the English names (`Infant`/`Child`/…, `Awake`/`Dreaming`/`Sleeping`) land as `en` rows in `tbl_Astro_Terminology`. `verify-avastha` is updated for the renamed symbols, same expected strings. | Spec §5.4 over-reached; introducing an enum + `StateName` mapping is behaviour-neutral churn beyond P1's extraction mandate (spec §3 non-goal, §14 risk). |
| G2 | `Ikiastrro.Core.Reference` holds **terminology types only** (`TerminologyCatalog`, `TerminologyCode`, `TerminologyRow`, `TerminologyTextRow`). The `*Reference` DB-row records stay in `Models` (D1). | Contains the downstream `using` sweep. |
| G3 | Rule-table citation column: `SourceRefCode VARCHAR(40) NULL` + `CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')`. No cross-table FK to `tbl_Dim_Source.Code`; resolution is enforced by `verify-sources` (extended) and `verify-rules`. | Matches how Plan 0 wired `tbl_Dim_Source` references. |
| G4 | The `ChartAnalyzer` split is a **minimal verbatim lift** — each extracted method is existing lines moved into a named engine method. The interwoven `keyDetails` `foreach` loop stays in the thin `Pipeline/ChartAnalyzer` composer (no clean seam, high risk). | Spec §14 "the split changes a value" risk; the tripwire is a pre/post `tbl_Chart_KeyDetails` diff for person `1_Ramakrishnan`. |

## Migration numbers (Plan 1)

| # | File | Adds |
|---|---|---|
| 16 | `db/16_rename_avastha_to_planetary_state.sql` | `sp_rename` of `tbl_Dim_AvasthaState`→`tbl_Dim_PlanetaryState`, `tbl_Rule_BaaladiState`→`tbl_Rule_AgeState`, `tbl_Rule_JagradadiState`→`tbl_Rule_WakefulnessState`, `tbl_Fact_PlanetAvastha`→`tbl_Fact_PlanetaryState`; column renames on the fact table; `CREATE OR ALTER` any view that referenced the old names |
| 17 | `db/17_create_astro_terminology.sql` | `tbl_Astro_Terminology` + `tbl_Astro_TerminologyText` |
| 18 | `db/18_rule_table_portability.sql` | portability tail on 7 existing `tbl_Rule_*`; `tbl_Rule_Catalog` + seed; 5 empty rule tables (`tbl_Rule_HouseSignification`, `tbl_Rule_Karaka`, `tbl_Rule_ShadbalaComponent`, `tbl_Rule_VimsopakaWeight`, `tbl_Rule_Yoga`) |

---

## File Structure

### New folders under `src/Ikiastrro.Core/`

```
Engines/
  Astronomy/         SwissEphemerisProvider (+ SiderealPositions, SunTimes), AstroMath, AstroIds,
                     PlanetName, ZodiacName, ConstellationName, BirthMomentFactory
  Position/          D1ChartComputer, D1RasiCalculator
  DivisionalCharts/  IVargaSignRule, VargaSignRuleFactory, LinearVargaSignRule + 16 *SignRule,
                     VargaChartComputer, VargaCalculator,
                     IVargaMethodInterpreter + Linear/Table/Band/Wrapped interpreters  (Task 12)
  Houses/            HouseEngine (extracted), LagnaFunctionalNature
  Nakshatras/        NakshatraEngine (extracted)
  Dignity/           DignityEngine (was ClassicalDignity) + DignityResult
  Dispositors/       IDispositorEngine, DispositorChain                (Task 8 — stub)
  Karakas/           CharaKaraka, CharaKarakaCalculator, ArudhaCalculator, HoraLagnaCalculator,
                     UpagrahaCalculator, SpecialPoint{Seed,Projector,Calculator},
                     ISthiraKarakaSource, INaisargikaKarakaSource      (Task 8 — interfaces)
  PlanetaryStates/   AgeStateCalculator (was BaaladiAvastha), WakefulnessStateCalculator
                     (was JagradadiAvastha), PlanetaryStateComputer (was PlanetAvasthaComputer),
                     PlanetaryStateModels (was Models/AvasthaModels.cs)
  Relationships/     RelationshipEngine (was ClassicalRelationships + extracted rows),
                     CombustionEngine (was ClassicalCombustion) + CombustionResult
  Strength/          IStrengthEngine, ShadbalaResult, VimsopakaResult  (Task 8 — stub)
  Dasha/             VimshottariDashaCalculator, DashaPeriod, DashaPeriodRecord, LifeWeek
  Yoga/              IYogaEngine, YogaResult                           (Task 8 — stub)
Pipeline/            ChartPipeline, ChartBundle (Task 7), ChartCalculationOrchestrator,
                    IChartCalculator, ChartAnalysisInput, ChartAnalyzer (thin composer, Task 5)
Reference/           TerminologyCatalog, TerminologyCode, TerminologyRow, TerminologyTextRow  (Task 10)
Presentation/        ChartViewModel (was Calculators/ChartViewModel.cs)
```

### Unchanged folders/namespaces

`Models/` (D1 — incl. `PlanetPosition`, `VargaScheme`, `*Reference`), `Geocoding/`, `LifeAreas/`, `Transits/`.

### Dissolved folders (removed once empty)

`Astro/`, `Calculators/`, `Jaimini/`, `SpecialPoints/`, `Dasha/` (moves to `Engines/Dasha/`).

### Full file-move map

| From | To | Namespace / type change |
|---|---|---|
| `Astro/SwissEphemerisProvider.cs` (+ `SiderealPositions`, `SunTimes`) | `Engines/Astronomy/` | ns → `Ikiastrro.Core.Engines.Astronomy` |
| `Astro/{AstroMath,AstroIds,PlanetName,ZodiacName,ConstellationName}.cs` | `Engines/Astronomy/` | ns only |
| `Astro/{IVargaSignRule,VargaSignRuleFactory,LinearVargaSignRule}.cs` | `Engines/DivisionalCharts/` | ns → `Ikiastrro.Core.Engines.DivisionalCharts` |
| `Astro/{HoraD2ClassicSignRule,HoraD2UmaShambuSignRule,PanchamsaD5SignRule,SaptamsaD7SignRule,AshtamsaD8SignRule,NavamsaD9SignRule,DasamsaD10SignRule,RudramsaD11SignRule,ShashtamsaD6SignRule,ShodasamsaD16SignRule,VimsamsaD20SignRule,SiddhamsaD24SignRule,NakshatramsaD27SignRule,TrimsamsaD30SignRule,KhavedamsaD40SignRule,AkshavedamsaD45SignRule}.cs` (16) | `Engines/DivisionalCharts/` | ns only |
| `Calculators/BirthMomentFactory.cs` | `Engines/Astronomy/` | ns only |
| `Calculators/{D1ChartComputer,D1RasiCalculator}.cs` | `Engines/Position/` | ns → `Ikiastrro.Core.Engines.Position` |
| `Calculators/{VargaChartComputer,VargaCalculator}.cs` | `Engines/DivisionalCharts/` | ns only |
| `Calculators/{ChartCalculationOrchestrator,IChartCalculator,ChartAnalysisInput}.cs` | `Pipeline/` | ns → `Ikiastrro.Core.Pipeline` |
| `Calculators/ChartAnalyzer.cs` | **split** (Task 5) → `Engines/Houses/HouseEngine.cs`, `Engines/Nakshatras/NakshatraEngine.cs`, `Engines/Relationships/RelationshipEngine.cs` (merge), `Pipeline/ChartAnalyzer.cs` (thin) | extract — behaviour identical |
| `Calculators/ClassicalDignity.cs` | `Engines/Dignity/DignityEngine.cs` | type `ClassicalDignity` → `DignityEngine`; `GetHouseSign`/`GetSignLord` move to `HouseEngine` |
| `Calculators/ClassicalRelationships.cs` | `Engines/Relationships/RelationshipEngine.cs` | type `ClassicalRelationships` → `RelationshipEngine`; absorb the row-mapping extracted from `ChartAnalyzer` |
| `Calculators/ClassicalCombustion.cs` | `Engines/Relationships/CombustionEngine.cs` | type `ClassicalCombustion` → `CombustionEngine` |
| `Calculators/LagnaFunctionalNature.cs` | `Engines/Houses/` | ns → `Ikiastrro.Core.Engines.Houses` |
| `Calculators/BaaladiAvastha.cs` | `Engines/PlanetaryStates/AgeStateCalculator.cs` | type `BaaladiAvastha` → `AgeStateCalculator` (Task 6) |
| `Calculators/JagradadiAvastha.cs` | `Engines/PlanetaryStates/WakefulnessStateCalculator.cs` | type `JagradadiAvastha` → `WakefulnessStateCalculator` (Task 6) |
| `Calculators/PlanetAvasthaComputer.cs` | `Engines/PlanetaryStates/PlanetaryStateComputer.cs` | type `PlanetAvasthaComputer` → `PlanetaryStateComputer` (Task 6) |
| `Calculators/ChartViewModel.cs` | `Presentation/ChartViewModel.cs` | ns → `Ikiastrro.Core.Presentation` |
| `Models/AvasthaModels.cs` | `Engines/PlanetaryStates/PlanetaryStateModels.cs` | ns → `Ikiastrro.Core.Engines.PlanetaryStates`; record renames (Task 6) |
| `Jaimini/{CharaKaraka,CharaKarakaCalculator}.cs` | `Engines/Karakas/` | ns → `Ikiastrro.Core.Engines.Karakas` |
| `SpecialPoints/{ArudhaCalculator,HoraLagnaCalculator,SpecialPointCalculator,SpecialPointProjector,SpecialPointSeed,UpagrahaCalculator}.cs` | `Engines/Karakas/` | ns → `Ikiastrro.Core.Engines.Karakas` |
| `Dasha/{DashaPeriod,DashaPeriodRecord,LifeWeek,VimshottariDashaCalculator}.cs` | `Engines/Dasha/` | ns → `Ikiastrro.Core.Engines.Dasha` |
| `Models/*` (21), `Geocoding/*` (3), `LifeAreas/*` (1), `Transits/*` (1) | unchanged | — |

### Downstream `using` rewrites (Task 13 — mechanical)

| Old `using` | New `using`(s) |
|---|---|
| `Ikiastrro.Core.Astro` | `Ikiastrro.Core.Engines.Astronomy` and/or `Ikiastrro.Core.Engines.DivisionalCharts` (per file — add both where a file touches sign rules **and** `AstroMath`) |
| `Ikiastrro.Core.Calculators` | `Ikiastrro.Core.Pipeline` and/or `Ikiastrro.Core.Engines.{Position,DivisionalCharts,Houses,Dignity,Relationships,PlanetaryStates}` and/or `Ikiastrro.Core.Presentation` (per file) |
| `Ikiastrro.Core.Jaimini` | `Ikiastrro.Core.Engines.Karakas` |
| `Ikiastrro.Core.SpecialPoints` | `Ikiastrro.Core.Engines.Karakas` |
| `Ikiastrro.Core.Dasha` | `Ikiastrro.Core.Engines.Dasha` |
| `Ikiastrro.Core.Models`, `.Geocoding`, `.LifeAreas`, `.Transits` | unchanged |

Affected files (from grep, may shift as tasks land — re-grep in Task 13):
`src/Ikiastrro.Cli/Program.cs`; `src/Ikiastrro.Data/{ChartGenerationService.cs, VargaSchemeRepository.cs, AvasthaRuleRepository.cs, PlanetAvasthaRepository.cs, …}`; `src/Ikiastrro.Web/**/*.razor` + `Program.cs` (13 `@using`/`using` sites).

---

## Task 1: Astronomy engine — folder skeleton + pure move

**Files:**
- Create: `src/Ikiastrro.Core/Engines/Astronomy/` (folder)
- Move: `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs`, `AstroMath.cs`, `AstroIds.cs`, `PlanetName.cs`, `ZodiacName.cs`, `ConstellationName.cs` → `Engines/Astronomy/`
- Move: `src/Ikiastrro.Core/Calculators/BirthMomentFactory.cs` → `Engines/Astronomy/`
- Modify: every moved file's `namespace` line → `Ikiastrro.Core.Engines.Astronomy`
- Modify (in-Core references only): any `src/Ikiastrro.Core/**` file that still compiles against `Ikiastrro.Core.Astro` for these 7 types — add `using Ikiastrro.Core.Engines.Astronomy;`

**Interfaces:**
- Consumes: nothing new.
- Produces: namespace `Ikiastrro.Core.Engines.Astronomy` exporting `SwissEphemerisProvider`, `SiderealPositions`, `SunTimes`, `AstroMath`, `AstroIds`, `PlanetName`, `ZodiacName`, `ConstellationName`, `BirthMomentFactory`.

**Note:** `Astro/` still holds the 19 varga sign-rule files after this task — it is emptied in Task 2. Do **not** touch the Cli/Data/Web projects yet (Task 13). This task must leave `src/Ikiastrro.Core` compiling; downstream projects may show `using`-not-found until Task 13 — that is expected and does **not** block this task, because the gate here is `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj`, not the full solution.

- [ ] **Step 1: Create the folder and move the 7 files**

```bash
mkdir -p src/Ikiastrro.Core/Engines/Astronomy
git mv src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Astro/AstroMath.cs             src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Astro/AstroIds.cs              src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Astro/PlanetName.cs            src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Astro/ZodiacName.cs            src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Astro/ConstellationName.cs     src/Ikiastrro.Core/Engines/Astronomy/
git mv src/Ikiastrro.Core/Calculators/BirthMomentFactory.cs src/Ikiastrro.Core/Engines/Astronomy/
```

- [ ] **Step 2: Rewrite the `namespace` line in each moved file**

In all 7 moved files change the single `namespace` declaration to:

```csharp
namespace Ikiastrro.Core.Engines.Astronomy;
```

(`SwissEphemerisProvider.cs` currently declares `namespace Ikiastrro.Core.Astro;` — `SiderealPositions` and `SunTimes` are in the same file and move with it. `BirthMomentFactory.cs` currently declares `namespace Ikiastrro.Core.Calculators;`.)

- [ ] **Step 3: Fix in-Core `using`s**

Grep for the old namespace inside Core only and add the new `using` where needed:

```bash
grep -rl "Ikiastrro.Core.Astro" src/Ikiastrro.Core --include='*.cs' | grep -v '/Engines/Astronomy/'
```

The 19 varga rule files under `Astro/` and the calculators under `Calculators/` that use `AstroMath` / `ZodiacName` etc. get `using Ikiastrro.Core.Engines.Astronomy;` added (keep their own `namespace` unchanged this task). Do **not** delete `using Ikiastrro.Core.Astro;` lines yet if the file still needs sign-rule types from that namespace — those move in Task 2.

- [ ] **Step 4: Build Core only**

Run: `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj`
Expected: **0 Warning(s), 0 Error(s)**. (Solution build will still fail on Cli/Data/Web `using`s — that is Task 13.)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(core): extract Engines/Astronomy — pure namespace move

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 2: DivisionalCharts engine — pure move

**Files:**
- Create: `src/Ikiastrro.Core/Engines/DivisionalCharts/`
- Move → `Engines/DivisionalCharts/`: `Astro/IVargaSignRule.cs`, `Astro/VargaSignRuleFactory.cs`, `Astro/LinearVargaSignRule.cs`, and the 16 concrete `Astro/*SignRule.cs` (`HoraD2ClassicSignRule`, `HoraD2UmaShambuSignRule`, `PanchamsaD5SignRule`, `SaptamsaD7SignRule`, `AshtamsaD8SignRule`, `NavamsaD9SignRule`, `DasamsaD10SignRule`, `RudramsaD11SignRule`, `ShashtamsaD6SignRule`, `ShodasamsaD16SignRule`, `VimsamsaD20SignRule`, `SiddhamsaD24SignRule`, `NakshatramsaD27SignRule`, `TrimsamsaD30SignRule`, `KhavedamsaD40SignRule`, `AkshavedamsaD45SignRule`)
- Move → `Engines/DivisionalCharts/`: `Calculators/VargaChartComputer.cs`, `Calculators/VargaCalculator.cs`
- Modify: every moved file's `namespace` → `Ikiastrro.Core.Engines.DivisionalCharts`; add `using Ikiastrro.Core.Engines.Astronomy;` and `using Ikiastrro.Core.Models;` where the file needs `AstroMath` / `ZodiacName` / `VargaScheme` / `PlanetPosition`
- Delete: the now-empty `src/Ikiastrro.Core/Astro/` folder

**Interfaces:**
- Consumes: `Ikiastrro.Core.Engines.Astronomy` (`AstroMath`, `ZodiacName`), `Ikiastrro.Core.Models` (`VargaScheme`, `PlanetPosition`).
- Produces: namespace `Ikiastrro.Core.Engines.DivisionalCharts` exporting `IVargaSignRule`, `VargaSignRuleFactory`, `LinearVargaSignRule` + 16 rules, `VargaChartComputer`, `VargaCalculator`.

- [ ] **Step 1: Move the 19 rule files + 2 calculators**

```bash
mkdir -p src/Ikiastrro.Core/Engines/DivisionalCharts
git mv src/Ikiastrro.Core/Astro/IVargaSignRule.cs        src/Ikiastrro.Core/Engines/DivisionalCharts/
git mv src/Ikiastrro.Core/Astro/VargaSignRuleFactory.cs  src/Ikiastrro.Core/Engines/DivisionalCharts/
git mv src/Ikiastrro.Core/Astro/*SignRule.cs             src/Ikiastrro.Core/Engines/DivisionalCharts/
git mv src/Ikiastrro.Core/Calculators/VargaChartComputer.cs src/Ikiastrro.Core/Engines/DivisionalCharts/
git mv src/Ikiastrro.Core/Calculators/VargaCalculator.cs    src/Ikiastrro.Core/Engines/DivisionalCharts/
rmdir src/Ikiastrro.Core/Astro
```

- [ ] **Step 2: Rewrite namespaces + usings**

Each moved file: `namespace Ikiastrro.Core.Engines.DivisionalCharts;`. Then per file, ensure the `using` block has `using Ikiastrro.Core.Engines.Astronomy;` (all of them use `AstroMath`/`ZodiacName`) and `using Ikiastrro.Core.Models;` where `VargaScheme` or `PlanetPosition` appears (`VargaCalculator`, `VargaChartComputer`). Remove any leftover `using Ikiastrro.Core.Astro;`.

- [ ] **Step 3: Fix remaining in-Core references**

```bash
grep -rl "Ikiastrro.Core.Astro" src/Ikiastrro.Core --include='*.cs'
```

Expected after fix: **no matches** (the `Astro` namespace is fully retired inside Core). `Calculators/ChartCalculationOrchestrator.cs`, `Calculators/D1RasiCalculator.cs`, `Calculators/ChartAnalyzer.cs` etc. that referenced sign rules or `AstroMath` get `using Ikiastrro.Core.Engines.Astronomy;` / `using Ikiastrro.Core.Engines.DivisionalCharts;`.

- [ ] **Step 4: Build Core**

Run: `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj`
Expected: 0 / 0.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(core): extract Engines/DivisionalCharts; retire Astro namespace

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 3: Position, Karakas, Dasha engines — pure move

**Files:**
- `Engines/Position/`: `git mv Calculators/D1ChartComputer.cs Calculators/D1RasiCalculator.cs` → ns `Ikiastrro.Core.Engines.Position`
- `Engines/Karakas/`: `git mv Jaimini/CharaKaraka.cs Jaimini/CharaKarakaCalculator.cs SpecialPoints/ArudhaCalculator.cs SpecialPoints/HoraLagnaCalculator.cs SpecialPoints/SpecialPointCalculator.cs SpecialPoints/SpecialPointProjector.cs SpecialPoints/SpecialPointSeed.cs SpecialPoints/UpagrahaCalculator.cs` → ns `Ikiastrro.Core.Engines.Karakas`
- `Engines/Dasha/`: `git mv Dasha/DashaPeriod.cs Dasha/DashaPeriodRecord.cs Dasha/LifeWeek.cs Dasha/VimshottariDashaCalculator.cs` → ns `Ikiastrro.Core.Engines.Dasha`
- Delete empty folders: `Jaimini/`, `SpecialPoints/`, `Dasha/`
- Modify: moved files' namespaces + `using` blocks (they reference `Engines.Astronomy`, `Models`, and Karakas cross-references)

**Interfaces:**
- Consumes: `Engines.Astronomy`, `Models`.
- Produces: `Ikiastrro.Core.Engines.Position` (`D1ChartComputer`, `D1RasiCalculator`), `Ikiastrro.Core.Engines.Karakas` (`CharaKaraka`, `CharaKarakaCalculator`, `ArudhaCalculator`, `HoraLagnaCalculator`, `UpagrahaCalculator`, `SpecialPointSeed`, `SpecialPointProjector`, `SpecialPointCalculator`), `Ikiastrro.Core.Engines.Dasha` (`VimshottariDashaCalculator`, `DashaPeriod`, `DashaPeriodRecord`, `LifeWeek`).

- [ ] **Step 1: Move**

```bash
mkdir -p src/Ikiastrro.Core/Engines/Position src/Ikiastrro.Core/Engines/Karakas src/Ikiastrro.Core/Engines/Dasha
git mv src/Ikiastrro.Core/Calculators/D1ChartComputer.cs  src/Ikiastrro.Core/Engines/Position/
git mv src/Ikiastrro.Core/Calculators/D1RasiCalculator.cs src/Ikiastrro.Core/Engines/Position/
git mv src/Ikiastrro.Core/Jaimini/*.cs        src/Ikiastrro.Core/Engines/Karakas/
git mv src/Ikiastrro.Core/SpecialPoints/*.cs  src/Ikiastrro.Core/Engines/Karakas/
git mv src/Ikiastrro.Core/Dasha/*.cs          src/Ikiastrro.Core/Engines/Dasha/
rmdir src/Ikiastrro.Core/Jaimini src/Ikiastrro.Core/SpecialPoints src/Ikiastrro.Core/Dasha
```

- [ ] **Step 2: Namespaces + usings**

`Engines/Position/*` → `namespace Ikiastrro.Core.Engines.Position;`, add `using Ikiastrro.Core.Engines.Astronomy;` + `using Ikiastrro.Core.Models;`.
`Engines/Karakas/*` → `namespace Ikiastrro.Core.Engines.Karakas;`, add `using Ikiastrro.Core.Engines.Astronomy;` + `using Ikiastrro.Core.Models;` where used. `ChartCalculationOrchestrator` (still in `Calculators/`) references `SpecialPointCalculator` — add `using Ikiastrro.Core.Engines.Karakas;` there.
`Engines/Dasha/*` → `namespace Ikiastrro.Core.Engines.Dasha;`.

- [ ] **Step 3: Build Core**

Run: `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj` → 0 / 0.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(core): extract Engines/{Position,Karakas,Dasha} — pure move

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 4: Dignity, Combustion, LagnaFunctionalNature, ChartViewModel — move + type rename

**Files:**
- `Engines/Dignity/DignityEngine.cs` ← `git mv Calculators/ClassicalDignity.cs`; rename `public static class ClassicalDignity` → `DignityEngine`; keep `DignityResult` record in the same file; ns `Ikiastrro.Core.Engines.Dignity`. **Leave `GetHouseSign` / `GetSignLord` in place for now** — they move to `HouseEngine` in Task 5.
- `Engines/Relationships/CombustionEngine.cs` ← `git mv Calculators/ClassicalCombustion.cs`; rename `public static class ClassicalCombustion` → `CombustionEngine`; keep `CombustionResult`; ns `Ikiastrro.Core.Engines.Relationships`.
- `Engines/Houses/LagnaFunctionalNature.cs` ← `git mv Calculators/LagnaFunctionalNature.cs`; ns `Ikiastrro.Core.Engines.Houses`; type name unchanged.
- `Presentation/ChartViewModel.cs` ← `git mv Calculators/ChartViewModel.cs`; ns `Ikiastrro.Core.Presentation`; type name unchanged.
- Modify: `Calculators/ChartAnalyzer.cs` — update the call sites `ClassicalDignity.` → `DignityEngine.`, `ClassicalCombustion.` → `CombustionEngine.`, add `using Ikiastrro.Core.Engines.Dignity;` + `using Ikiastrro.Core.Engines.Relationships;`. (`ChartAnalyzer` itself moves/splits in Task 5.)

**Interfaces:**
- Produces: `Ikiastrro.Core.Engines.Dignity.DignityEngine` (static; `DignityResult Evaluate(string planet, ZodiacName sign, double? degreeInSign, IReadOnlyDictionary<string,ZodiacName> allSigns)`, plus `GetHouseSign`/`GetSignLord` until Task 5), `Ikiastrro.Core.Engines.Relationships.CombustionEngine` (static; `bool IsApplicable(string)`, `CombustionResult Evaluate(string, double, double, bool)`), `Ikiastrro.Core.Engines.Houses.LagnaFunctionalNature`, `Ikiastrro.Core.Presentation.ChartViewModel`.
- Consumes: `Engines.Astronomy`.

- [ ] **Step 1: Move + rename**

```bash
mkdir -p src/Ikiastrro.Core/Engines/Dignity src/Ikiastrro.Core/Engines/Relationships src/Ikiastrro.Core/Engines/Houses src/Ikiastrro.Core/Presentation
git mv src/Ikiastrro.Core/Calculators/ClassicalDignity.cs      src/Ikiastrro.Core/Engines/Dignity/DignityEngine.cs
git mv src/Ikiastrro.Core/Calculators/ClassicalCombustion.cs   src/Ikiastrro.Core/Engines/Relationships/CombustionEngine.cs
git mv src/Ikiastrro.Core/Calculators/LagnaFunctionalNature.cs src/Ikiastrro.Core/Engines/Houses/LagnaFunctionalNature.cs
git mv src/Ikiastrro.Core/Calculators/ChartViewModel.cs        src/Ikiastrro.Core/Presentation/ChartViewModel.cs
```

- [ ] **Step 2: Edit the 4 moved files**

- `DignityEngine.cs`: `namespace Ikiastrro.Core.Engines.Dignity;`; `public static class ClassicalDignity` → `public static class DignityEngine`; `using Ikiastrro.Core.Engines.Astronomy;`.
- `CombustionEngine.cs`: `namespace Ikiastrro.Core.Engines.Relationships;`; `public static class ClassicalCombustion` → `public static class CombustionEngine`; `using Ikiastrro.Core.Engines.Astronomy;`.
- `LagnaFunctionalNature.cs`: `namespace Ikiastrro.Core.Engines.Houses;`; fix its `using`s.
- `ChartViewModel.cs`: `namespace Ikiastrro.Core.Presentation;`; fix its `using`s.

- [ ] **Step 3: Patch `Calculators/ChartAnalyzer.cs` call sites**

Replace `ClassicalDignity.` → `DignityEngine.` and `ClassicalCombustion.` → `CombustionEngine.` throughout; add `using Ikiastrro.Core.Engines.Dignity;`, `using Ikiastrro.Core.Engines.Relationships;`. (`ClassicalRelationships` stays untouched here — Task 5.)

- [ ] **Step 4: Build Core** → `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj` → 0 / 0.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(core): Dignity/Combustion engines + Houses/Presentation moves

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 5: Split `ChartAnalyzer` into engines + thin composer

**Goal:** Extract cleanly-separable blocks of `Calculators/ChartAnalyzer.cs` into `HouseEngine`, `NakshatraEngine`, and `RelationshipEngine` (verbatim lift, no logic edits). The interwoven `keyDetails` `foreach` loop stays in a thin `Pipeline/ChartAnalyzer` composer.

**Files:**
- Create: `src/Ikiastrro.Core/Engines/Houses/HouseEngine.cs`
- Create: `src/Ikiastrro.Core/Engines/Nakshatras/NakshatraEngine.cs`
- Modify: `src/Ikiastrro.Core/Calculators/ClassicalRelationships.cs` → `git mv` to `src/Ikiastrro.Core/Engines/Relationships/RelationshipEngine.cs`; rename type; add the row-mapping methods
- Create: `src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs` (thin composer) ← `git mv Calculators/ChartAnalyzer.cs`
- Delete: the now-empty `src/Ikiastrro.Core/Calculators/` folder **after Task 7** (still holds orchestrator/interface/input until then — do not `rmdir` here)

**Interfaces:**
- Consumes: `Engines.Astronomy` (`AstroMath`, `AstroIds`, `PlanetName`, `ZodiacName`), `Engines.Dignity` (`DignityEngine`), `Engines.Relationships` (`CombustionEngine`), `Models` (`ChartKeyDetail`, `ChartHouseLord`, `ChartConjunction`, `ChartAspect`), `Pipeline` (`ChartAnalysisInput`).
- Produces:
  - `Ikiastrro.Core.Engines.Houses.HouseEngine` (static):
    - `static ZodiacName GetHouseSign(ZodiacName ascendantSign, int houseNumber)` — lifted verbatim from `DignityEngine.GetHouseSign` (moved out of Dignity).
    - `static string GetSignLord(ZodiacName sign)` — lifted verbatim from `DignityEngine.GetSignLord`.
    - `static List<ChartHouseLord> BuildHouseLords(ChartAnalysisInput input, IReadOnlyDictionary<string, ChartKeyDetail> placementByPlanet)` — lifted verbatim from `ChartAnalyzer` lines 157–178, with `ClassicalDignity.GetHouseSign` / `.GetSignLord` calls rewritten to `GetHouseSign` / `GetSignLord` (now local).
  - `Ikiastrro.Core.Engines.Nakshatras.NakshatraEngine` (static):
    - `readonly record struct NakshatraDerivation(string LordPlanet, int NakshatraIndex, string SubLordPlanet, int OverallPadaIndex)`
    - `static NakshatraDerivation ForLongitude(double nirayanaLongitude)` — wraps, verbatim, the four `AstroMath` calls currently inline in `ChartAnalyzer` (`GetNakshatraLord`, `GetNakshatraIndexAndFractionElapsed(...).NakshatraIndex`, `GetNakshatraSubLord`, `GetOverallPadaIndex`).
  - `Ikiastrro.Core.Engines.Relationships.RelationshipEngine` (static) — renamed `ClassicalRelationships`, unchanged bodies of `FindAspects` / `FindConjunctions`; **plus** two new methods lifted verbatim from `ChartAnalyzer` lines 180–213:
    - `static List<ChartConjunction> BuildConjunctionRows(ChartAnalysisInput input)`
    - `static List<ChartAspect> BuildAspectRows(IReadOnlyList<AspectResult> aspectResults)`
  - `Ikiastrro.Core.Pipeline.ChartAnalyzer` (static) — same public signature as today:
    `static (List<ChartKeyDetail> KeyDetails, List<ChartHouseLord> HouseLords, List<ChartConjunction> Conjunctions, List<ChartAspect> Aspects) Compute(ChartAnalysisInput input)`.

- [ ] **Step 1: Record the pre-split golden state**

```bash
dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails 1
sqlcmd -S localhost -E -h-1 -W -s"|" -Q "SET NOCOUNT ON; SELECT Planet,PointKind,Sign,DegreesInSignDecimal,DignityStatus,NakshatraLordPlanet,NakshatraSubLordPlanet,NakshatraPadaId,HouseNumberFromLagna,HouseNumberFromSun,HouseNumberFromMoon,IsCombust,AspectingPlanets FROM ikiastrro.dbo.tbl_Chart_KeyDetails kd JOIN ikiastrro.dbo.tbl_ChartResults r ON r.Id=kd.ChartResultId JOIN ikiastrro.dbo.tbl_BirthDetails b ON b.Id=r.BirthDetailId WHERE b.PersonKey='1_Ramakrishnan' ORDER BY r.ChartTypeId,kd.Id" > /tmp/keydetails_pre.txt
```
(If `sqlcmd` here uses ODBC and the DB literal differs, target `$(DbName)` via the same `:setvar` substitution the scratch check uses — see `INFRASTRUCTURE.md`.)

- [ ] **Step 2: Create `HouseEngine.cs`**

Move `GetHouseSign` and `GetSignLord` out of `Engines/Dignity/DignityEngine.cs` into this file **unchanged** (cut the two `private static`/`public static` methods and their backing tables if any are private to them; if `DignityEngine` still needs `GetSignLord` internally, make `HouseEngine.GetSignLord` `public` and call it from `DignityEngine`). Then add `BuildHouseLords`:

```csharp
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>
/// Whole-sign house geometry: house→sign from the Ascendant, sign→lord, and the per-house
/// lordship rows (tbl_Chart_HouseLords). Extracted verbatim from ChartAnalyzer (2026-09-02
/// engine reorg) — no rule changed; the sign/lord tables moved here from ClassicalDignity
/// because both House and Dignity need "who lords this sign" and House is the lower layer.
/// </summary>
public static class HouseEngine
{
    // <<< GetHouseSign(ZodiacName ascendantSign, int houseNumber): lifted verbatim >>>
    // <<< GetSignLord(ZodiacName sign): lifted verbatim >>>

    public static List<ChartHouseLord> BuildHouseLords(
        ChartAnalysisInput input,
        IReadOnlyDictionary<string, ChartKeyDetail> placementByPlanet)
    {
        var houseLords = new List<ChartHouseLord>();
        for (var houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            var houseSign = GetHouseSign(input.AscendantSign, houseNumber);
            var lordPlanet = GetSignLord(houseSign);
            var lordPlacement = placementByPlanet[lordPlanet];

            houseLords.Add(new ChartHouseLord
            {
                HouseNumber = houseNumber,
                HouseSign = houseSign.ToString(),
                HouseSignId = AstroIds.SignId(houseSign),
                LordPlanet = lordPlanet,
                LordPlanetId = AstroIds.PlanetId(Enum.Parse<PlanetName>(lordPlanet)),
                LordPlacedInHouseFromLagna = lordPlacement.HouseNumberFromLagna,
                LordPlacedInHouseFromSun = lordPlacement.HouseNumberFromSun,
                LordPlacedInHouseFromMoon = lordPlacement.HouseNumberFromMoon,
                LordPlacedInSign = lordPlacement.Sign,
                LordPlacedInSignId = AstroIds.SignId(Enum.Parse<ZodiacName>(lordPlacement.Sign)),
                LordDignityStatus = lordPlacement.DignityStatus
            });
        }
        return houseLords;
    }
}
```

- [ ] **Step 3: Create `NakshatraEngine.cs`**

```csharp
using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.Nakshatras;

/// <summary>
/// Thin wrapper over the AstroMath nakshatra helpers, so "which engine owns nakshatra
/// linkage" has a named home. Extracted verbatim from ChartAnalyzer (2026-09-02).
/// </summary>
public static class NakshatraEngine
{
    public readonly record struct NakshatraDerivation(
        string LordPlanet, int NakshatraIndex, string SubLordPlanet, int OverallPadaIndex);

    public static NakshatraDerivation ForLongitude(double nirayanaLongitude) => new(
        LordPlanet:      AstroMath.GetNakshatraLord(nirayanaLongitude).ToString(),
        NakshatraIndex:  AstroMath.GetNakshatraIndexAndFractionElapsed(nirayanaLongitude).NakshatraIndex,
        SubLordPlanet:   AstroMath.GetNakshatraSubLord(nirayanaLongitude).ToString(),
        OverallPadaIndex: AstroMath.GetOverallPadaIndex(nirayanaLongitude));
}
```

- [ ] **Step 4: `git mv` `ClassicalRelationships.cs` → `RelationshipEngine.cs`, rename, add row builders**

```bash
git mv src/Ikiastrro.Core/Calculators/ClassicalRelationships.cs src/Ikiastrro.Core/Engines/Relationships/RelationshipEngine.cs
```

Edit: `namespace Ikiastrro.Core.Engines.Relationships;`; `public static class ClassicalRelationships` → `public static class RelationshipEngine`; keep `ConjunctionResult` / `AspectResult` records; add `using Ikiastrro.Core.Engines.Astronomy; using Ikiastrro.Core.Models; using Ikiastrro.Core.Pipeline;`. Append the two builders — bodies are `ChartAnalyzer` lines 180–213 lifted verbatim, with `ClassicalRelationships.` self-calls dropped:

```csharp
    public static List<ChartConjunction> BuildConjunctionRows(ChartAnalysisInput input) =>
        FindConjunctions(input)
            .Select(c =>
            {
                var idA = AstroIds.PlanetId(Enum.Parse<PlanetName>(c.Planet1));
                var idB = AstroIds.PlanetId(Enum.Parse<PlanetName>(c.Planet2));
                var (lowName, lowId, highName, highId) = idA <= idB
                    ? (c.Planet1, idA, c.Planet2, idB)
                    : (c.Planet2, idB, c.Planet1, idA);
                return new ChartConjunction
                {
                    Planet1 = lowName, Planet1Id = lowId,
                    Planet2 = highName, Planet2Id = highId,
                    Sign = c.Sign,
                    SignId = AstroIds.SignId(Enum.Parse<ZodiacName>(c.Sign)),
                    HouseNumberFromLagna = c.HouseNumberFromLagna,
                    DegreeSeparation = c.DegreeSeparation
                };
            })
            .ToList();

    public static List<ChartAspect> BuildAspectRows(IReadOnlyList<AspectResult> aspectResults) =>
        aspectResults
            .Select(a => new ChartAspect
            {
                AspectingPlanet = a.AspectingPlanet,
                AspectingPlanetId = AstroIds.PlanetId(Enum.Parse<PlanetName>(a.AspectingPlanet)),
                AspectedTarget = a.AspectedTarget,
                AspectedTargetType = a.AspectedTarget == "Ascendant" ? "Ascendant" : "Planet",
                AspectedPlanetId = AstroIds.PlanetIdOrNull(a.AspectedTarget),
                AspectType = a.AspectType
            })
            .ToList();
```

- [ ] **Step 5: `git mv` `ChartAnalyzer.cs` → `Pipeline/`, reduce to the composer**

```bash
git mv src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs src/Ikiastrro.Core/Pipeline/ChartAnalyzer.cs
```

New file body: `namespace Ikiastrro.Core.Pipeline;`, usings for `Engines.Astronomy`, `Engines.Dignity`, `Engines.Houses`, `Engines.Nakshatras`, `Engines.Relationships`, `Models`. Keep `Compute` and its `keyDetails` + special-points loops **exactly as they are** (lines 24–150), then replace lines 152–215 with:

```csharp
        var placementByPlanet = keyDetails
            .Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant")
            .ToDictionary(k => k.Planet);

        var houseLords   = HouseEngine.BuildHouseLords(input, placementByPlanet);
        var conjunctions = RelationshipEngine.BuildConjunctionRows(input);
        var aspects      = RelationshipEngine.BuildAspectRows(aspectResults);

        return (keyDetails, houseLords, conjunctions, aspects);
```

Inside the `keyDetails` loop, replace the three inline `AstroMath` nakshatra lines with:

```csharp
            var nak = NakshatraEngine.ForLongitude(nirayanaLongitude);
            var nakshatraLordPlanet = nak.LordPlanet;
            var nakshatraIndex = nak.NakshatraIndex;
            var nakshatraSubLordPlanet = nak.SubLordPlanet;
```

and `NakshatraPadaId = isRasiChart ? AstroMath.GetOverallPadaIndex(nirayanaLongitude) + 1 : null,` → `NakshatraPadaId = isRasiChart ? nak.OverallPadaIndex + 1 : null,`. `aspectResults` is still `RelationshipEngine.FindAspects(input)` (rename the `ClassicalRelationships.` call).

- [ ] **Step 6: Build Core** → `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj` → 0 / 0.

- [ ] **Step 7: Behaviour tripwire — rebuild person 1, diff**

```bash
dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails 1
# rerun the Step 1 SELECT into /tmp/keydetails_post.txt
diff /tmp/keydetails_pre.txt /tmp/keydetails_post.txt
```
Expected: **no diff.** If any line differs, the extraction changed a value — revert and re-lift that block verbatim.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(core): split ChartAnalyzer into House/Nakshatra/Relationship engines

Verbatim lift — keyDetails loop stays in the thin Pipeline/ChartAnalyzer composer.
tbl_Chart_KeyDetails for 1_Ramakrishnan diffed pre/post: identical.

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 6: PlanetaryStates engine — rename avastha (C# + migration 16)

**Files:**
- `Engines/PlanetaryStates/AgeStateCalculator.cs` ← `git mv Calculators/BaaladiAvastha.cs`; `public static class BaaladiAvastha` → `AgeStateCalculator`
- `Engines/PlanetaryStates/WakefulnessStateCalculator.cs` ← `git mv Calculators/JagradadiAvastha.cs`; `JagradadiAvastha` → `WakefulnessStateCalculator`
- `Engines/PlanetaryStates/PlanetaryStateComputer.cs` ← `git mv Calculators/PlanetAvasthaComputer.cs`; `PlanetAvasthaComputer` → `PlanetaryStateComputer`
- `Engines/PlanetaryStates/PlanetaryStateModels.cs` ← `git mv Models/AvasthaModels.cs`; record renames (below); ns `Ikiastrro.Core.Engines.PlanetaryStates`
- Create: `db/16_rename_avastha_to_planetary_state.sql`
- Modify: `db/ikiastrro.sql` — fold migration 16 (rename the `CREATE TABLE` blocks + every reference); `src/Ikiastrro.Data/{AvasthaRuleRepository.cs, PlanetAvasthaRepository.cs}` (table + type names); `src/Ikiastrro.Cli/Program.cs` `verify-avastha` block (renamed symbols)

**Record renames** (in `PlanetaryStateModels.cs`):

| Old | New |
|---|---|
| `AvasthaStateRow` | `PlanetaryStateRow` |
| `BaaladiRuleRow` | `AgeStateRuleRow` |
| `JagradadiRuleRow` | `WakefulnessStateRuleRow` |
| `AvasthaRuleSet` | `PlanetaryStateRuleSet` |
| `PlanetAvasthaFact` | `PlanetaryStateFact` |
| field `AvasthaRuleSet.Baaladi` | `PlanetaryStateRuleSet.AgeBands` |
| field `AvasthaRuleSet.JagradadiByDignity` | `PlanetaryStateRuleSet.WakefulnessByDignity` |
| `PlanetAvasthaFact.BaaladiStateId` / `BaaladiEffectFraction` / `JagradadiStateId` | `AgeStateId` / `AgeEffectFraction` / `WakefulnessStateId` |

**Table renames** (migration 16):

| Old table | New table |
|---|---|
| `dbo.tbl_Dim_AvasthaState` | `dbo.tbl_Dim_PlanetaryState` |
| `dbo.tbl_Rule_BaaladiState` | `dbo.tbl_Rule_AgeState` |
| `dbo.tbl_Rule_JagradadiState` | `dbo.tbl_Rule_WakefulnessState` |
| `dbo.tbl_Fact_PlanetAvastha` | `dbo.tbl_Fact_PlanetaryState` |

Column renames on `dbo.tbl_Fact_PlanetaryState`: `BaaladiStateId`→`AgeStateId`, `BaaladiEffectFraction`→`AgeEffectFraction`, `JagradadiStateId`→`WakefulnessStateId`.
**Unchanged:** every `StateName` value ("Baala"…"Mrita", "Jagrat"/"Swapna"/"Sushupti") and every `AvasthaSystem` value ("Baaladi"/"Jagradadi"). Constraint/index internal names are left as-is (cosmetic, not environment-coupled).

**Interfaces:**
- Produces: `Ikiastrro.Core.Engines.PlanetaryStates` exporting `AgeStateCalculator` (`static AgeStateRuleRow For(ZodiacName sign, decimal degreeInSign, IReadOnlyList<AgeStateRuleRow> rules)`, `static bool IsOddSign(ZodiacName)`), `WakefulnessStateCalculator` (`static WakefulnessStateRuleRow? For(string? dignityStatus, IReadOnlyDictionary<string, WakefulnessStateRuleRow> map)`), `PlanetaryStateComputer` (`static List<PlanetaryStateFact> Compute(ChartAnalysisInput input, IReadOnlyList<ChartKeyDetail> keyDetails, PlanetaryStateRuleSet rules)`), and the renamed records.

- [ ] **Step 1: `git mv` + namespace/type renames**

```bash
mkdir -p src/Ikiastrro.Core/Engines/PlanetaryStates
git mv src/Ikiastrro.Core/Calculators/BaaladiAvastha.cs        src/Ikiastrro.Core/Engines/PlanetaryStates/AgeStateCalculator.cs
git mv src/Ikiastrro.Core/Calculators/JagradadiAvastha.cs      src/Ikiastrro.Core/Engines/PlanetaryStates/WakefulnessStateCalculator.cs
git mv src/Ikiastrro.Core/Calculators/PlanetAvasthaComputer.cs src/Ikiastrro.Core/Engines/PlanetaryStates/PlanetaryStateComputer.cs
git mv src/Ikiastrro.Core/Models/AvasthaModels.cs              src/Ikiastrro.Core/Engines/PlanetaryStates/PlanetaryStateModels.cs
```

Apply every rename from the two tables above across these 4 files. Keep XML-doc summaries but update the table names they mention (`tbl_Rule_BaaladiState` → `tbl_Rule_AgeState`, `tbl_Dim_AvasthaState` → `tbl_Dim_PlanetaryState`, `tbl_Fact_PlanetAvastha` → `tbl_Fact_PlanetaryState`). `PlanetAvasthaComputer`'s guard `if (kd.Planet == "Ascendant" || kd.PointKind != "Graha") continue;` stays.

- [ ] **Step 2: Write `db/16_rename_avastha_to_planetary_state.sql`**

```sql
:setvar DbName "ikiastrro"
SET NOCOUNT ON;
USE [$(DbName)];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = '16')
BEGIN
    IF OBJECT_ID('dbo.tbl_Dim_AvasthaState','U')  IS NOT NULL EXEC sp_rename 'dbo.tbl_Dim_AvasthaState',  'tbl_Dim_PlanetaryState';
    IF OBJECT_ID('dbo.tbl_Rule_BaaladiState','U') IS NOT NULL EXEC sp_rename 'dbo.tbl_Rule_BaaladiState', 'tbl_Rule_AgeState';
    IF OBJECT_ID('dbo.tbl_Rule_JagradadiState','U') IS NOT NULL EXEC sp_rename 'dbo.tbl_Rule_JagradadiState','tbl_Rule_WakefulnessState';
    IF OBJECT_ID('dbo.tbl_Fact_PlanetAvastha','U') IS NOT NULL EXEC sp_rename 'dbo.tbl_Fact_PlanetAvastha','tbl_Fact_PlanetaryState';

    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','BaaladiStateId') IS NOT NULL
        EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.BaaladiStateId', 'AgeStateId', 'COLUMN';
    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','BaaladiEffectFraction') IS NOT NULL
        EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.BaaladiEffectFraction', 'AgeEffectFraction', 'COLUMN';
    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','JagradadiStateId') IS NOT NULL
        EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.JagradadiStateId', 'WakefulnessStateId', 'COLUMN';

    INSERT INTO dbo.SchemaMigrations (MigrationId, Description, AppliedAtUtc)
    VALUES ('16', 'Rename avastha star schema -> planetary-state (tables + fact columns; row values unchanged)', SYSUTCDATETIME());
    PRINT '16 applied: avastha -> planetary-state rename.';
END
ELSE PRINT '16 already applied.';
GO
```

Then run: `sqlcmd -S localhost -E -b -i db/16_rename_avastha_to_planetary_state.sql`.
Then grep `db/ikiastrro.sql` for any **view / TVF** selecting from the old names (`grep -n "tbl_Dim_AvasthaState\|tbl_Rule_BaaladiState\|tbl_Rule_JagradadiState\|tbl_Fact_PlanetAvastha\|BaaladiStateId\|JagradadiStateId\|BaaladiEffectFraction" db/ikiastrro.sql`). For each hit that is a `CREATE VIEW` / `CREATE OR ALTER VIEW`, add a matching `CREATE OR ALTER VIEW` to migration 16 with the renamed identifiers. Re-run migration 16.

- [ ] **Step 3: Fold migration 16 into `db/ikiastrro.sql`**

In the baseline: rename the `CREATE TABLE dbo.tbl_Dim_AvasthaState` / `tbl_Rule_BaaladiState` / `tbl_Rule_JagradadiState` / `tbl_Fact_PlanetAvastha` blocks and every downstream reference (seeds, views, constraints that embed the name in their string). The folded baseline defines the **new** names from the start; `StateName` / `AvasthaSystem` seed values stay. Verify: `grep -c "tbl_Dim_AvasthaState\|tbl_Fact_PlanetAvastha" db/ikiastrro.sql` → `0`.

- [ ] **Step 4: Update `Ikiastrro.Data`**

`AvasthaRuleRepository.cs` → rename to `PlanetaryStateRuleRepository.cs` (class `AvasthaRuleRepository` → `PlanetaryStateRuleRepository`, method `GetActiveRuleSet()` returns `PlanetaryStateRuleSet`), SQL `FROM dbo.tbl_Rule_BaaladiState` → `tbl_Rule_AgeState` etc. `PlanetAvasthaRepository.cs` → `PlanetaryStateRepository.cs`, `INSERT INTO dbo.tbl_Fact_PlanetAvastha` → `tbl_Fact_PlanetaryState` with renamed columns. Update the DI registration + `ChartGenerationService` field names/usings. (Full `using` sweep is Task 13; here fix only what these renamed files need to compile.)

- [ ] **Step 5: Rebase `verify-avastha`**

In `src/Ikiastrro.Cli/Program.cs`, the `verify-avastha` block: `new AvasthaRuleRepository(...)` → `new PlanetaryStateRuleRepository(...)`; `BaaladiAvastha.For(...)` → `AgeStateCalculator.For(...)`; `JagradadiAvastha.For(...)` → `WakefulnessStateCalculator.For(...)`; `rules.Baaladi` → `rules.AgeBands`; `rules.JagradadiByDignity` → `rules.WakefulnessByDignity`. **Every `Check(...)` expected string stays exactly as it is** ("Baala", "Kumara", "Yuva", "Vriddha", "Mrita", "Jagrat", "Swapna", "Sushupti", the fractions).

- [ ] **Step 6: Build + verify**

```bash
dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj      # 0/0
dotnet build src/Ikiastrro.Data/Ikiastrro.Data.csproj      # 0/0
dotnet run --project src/Ikiastrro.Cli -- verify-avastha   # ALL PASS
```

- [ ] **Step 7: Fresh scratch rebuild check** (baseline still valid)

```bash
sed 's/:setvar DbName "ikiastrro"/:setvar DbName "ikiastrro_scratch"/' db/ikiastrro.sql > db/_scratch_tmp.sql
sqlcmd -S localhost -E -b -i db/_scratch_tmp.sql && rm db/_scratch_tmp.sql
sqlcmd -S localhost -E -Q "SELECT name FROM ikiastrro_scratch.sys.tables WHERE name LIKE 'tbl_%Avastha%'"   # -> zero rows
sqlcmd -S localhost -E -Q "DROP DATABASE ikiastrro_scratch"
```

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor: PlanetaryStates engine — rename avastha schema (migration 16)

Tables + fact columns renamed; all StateName / AvasthaSystem row values unchanged.
verify-avastha rebased on renamed symbols, same expected values.

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 7: `ChartPipeline` / `ChartBundle` façade

**Files:**
- `Pipeline/ChartCalculationOrchestrator.cs`, `Pipeline/IChartCalculator.cs`, `Pipeline/ChartAnalysisInput.cs` ← `git mv` from `Calculators/`; ns `Ikiastrro.Core.Pipeline`
- Delete: the now-empty `src/Ikiastrro.Core/Calculators/` folder
- Create: `src/Ikiastrro.Core/Pipeline/ChartBundle.cs`, `src/Ikiastrro.Core/Pipeline/ChartPipeline.cs`
- Modify: `src/Ikiastrro.Data/ChartGenerationService.cs` — adopt `ChartPipeline.Run(birth)` for the compute half
- Create: `verify-pipeline` block in `src/Ikiastrro.Cli/Program.cs`

**Interfaces:**
- Consumes: every engine's public surface + `Engines.Astronomy` (`SiderealPositions`, `SunTimes`), `Models` (`BirthDetails`, `ChartKeyDetail`), `Engines.PlanetaryStates` (`PlanetaryStateFact`, `PlanetaryStateRuleSet`), `Engines.Karakas` (`CharaKarakaCalculator`, `SpecialPointCalculator`).
- Produces:

```csharp
namespace Ikiastrro.Core.Pipeline;

public sealed record ChartBundle(
    BirthDetails Birth,
    SiderealPositions Positions,
    SunTimes SunTimes,
    IReadOnlyList<ChartAnalysisInput> Charts,
    IReadOnlyDictionary<string, string> CharaKarakaByPlanet,
    IReadOnlyList<PlanetaryStateFact> States);
    // P2+ add: Dispositors, Strength, Yogas as additive record fields

public sealed class ChartPipeline
{
    public ChartPipeline(
        ChartCalculationOrchestrator orchestrator,
        PlanetaryStateRuleSet planetaryStateRules);

    public ChartBundle Run(BirthDetails birth);   // no I/O
}
```

**Design:** `ChartPipeline.Run` = what `ChartGenerationService` does today between "have `BirthDetails`" and "write rows", minus persistence:
1. `orchestrator.CalculateAll(birth)` → the `(ChartResult, ChartAnalysisInput)` list → keep the `ChartAnalysisInput` list as `Charts`.
2. For each chart input: `ChartAnalyzer.Compute(input)` is **not** re-run here (the orchestrator already carries what persistence needs) — `ChartBundle` exposes the raw `Charts`; callers that want the derived rows call `ChartAnalyzer.Compute` per chart (same as today).
3. `CharaKarakaByPlanet` — lift the existing `ChartGenerationService.CharaKarakaByPlanet(SiderealPositions)` helper into the pipeline (it is pure).
4. `States` — `PlanetaryStateComputer.Compute(d1Input, d1KeyDetails, planetaryStateRules)` for the D1 chart (Age) + every chart (Wakefulness), exactly as `ChartGenerationService` does now.
5. `Positions` / `SunTimes` — from the orchestrator's D1 path / `SwissEphemerisProvider.GetSunTimes` (whatever `ChartGenerationService` calls today).

`ChartGenerationService` keeps its repositories; its `GenerateAll` becomes `var bundle = _pipeline.Run(birth);` then the existing persistence loop over `bundle.Charts` (calling `ChartAnalyzer.Compute` per chart, as now), `bundle.CharaKarakaByPlanet`, `bundle.States`.

- [ ] **Step 1: Move the 3 pipeline files, delete `Calculators/`**

```bash
git mv src/Ikiastrro.Core/Calculators/ChartCalculationOrchestrator.cs src/Ikiastrro.Core/Pipeline/
git mv src/Ikiastrro.Core/Calculators/IChartCalculator.cs              src/Ikiastrro.Core/Pipeline/
git mv src/Ikiastrro.Core/Calculators/ChartAnalysisInput.cs            src/Ikiastrro.Core/Pipeline/
rmdir src/Ikiastrro.Core/Calculators
```
Rewrite their `namespace` → `Ikiastrro.Core.Pipeline`; fix `using`s (`Engines.DivisionalCharts` for `VargaCalculator`, `Engines.Position` for `D1RasiCalculator`, `Engines.Karakas` for `SpecialPointCalculator`, `Models` for `VargaScheme`/`ChartResult`).

- [ ] **Step 2: Write `ChartBundle.cs` + `ChartPipeline.cs`** per the Interfaces block above. `Run` has **no** repository/`SqlConnection` parameter or field.

- [ ] **Step 3: Adopt in `ChartGenerationService.cs`**

Add `private readonly ChartPipeline _pipeline;` (constructed from the injected `ChartCalculationOrchestrator` + `PlanetaryStateRuleSet` — or new it up in the ctor). Replace the inline compute steps in `GenerateAll` / `Recompute` with `var bundle = _pipeline.Run(birth);` and iterate `bundle`. The `PersistAnalytics(int, ChartAnalysisInput, IReadOnlyDictionary<string,string>)` signature is unchanged.

- [ ] **Step 4: `verify-pipeline` CLI mode**

New block in `Program.cs` (after `verify-jaimini`, before `verify-sources`):

```csharp
if (args.Length > 0 && args[0] == "verify-pipeline")
{
    var schemes = new VargaSchemeRepository(connectionFactory).GetActiveSchemes();
    var orch = ChartCalculationOrchestrator.CreateDefault(schemes);
    var psRules = new PlanetaryStateRuleRepository(connectionFactory).GetActiveRuleSet();
    var pipeline = new ChartPipeline(orch, psRules);

    var birth = new BirthDetailsRepository(connectionFactory).GetByPersonKey("1_Ramakrishnan")
        ?? throw new InvalidOperationException("seed person 1_Ramakrishnan not found");

    var bundle = pipeline.Run(birth);

    // Recompute D1 KeyDetails from the bundle and compare to the stored rows for person 1.
    var d1 = bundle.Charts.Single(c => c.ChartType == "D1");
    var (computed, _, _, _) = ChartAnalyzer.Compute(d1);
    var stored = new ChartKeyDetailsRepository(connectionFactory).GetForPersonChart("1_Ramakrishnan", "D1");

    var failures = 0;
    void Check(string label, object? a, object? e)
    {
        var ok = $"{a}" == $"{e}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {a}, expected {e}");
        if (!ok) failures++;
    }
    Check("D1 row count", computed.Count, stored.Count);
    foreach (var s in stored)
    {
        var c = computed.FirstOrDefault(x => x.Planet == s.Planet && x.PointKind == s.PointKind);
        Check($"{s.Planet}/{s.PointKind} Sign", c?.Sign, s.Sign);
        Check($"{s.Planet}/{s.PointKind} DignityStatus", c?.DignityStatus, s.DignityStatus);
        Check($"{s.Planet}/{s.PointKind} HouseFromLagna", c?.HouseNumberFromLagna, s.HouseNumberFromLagna);
    }
    Check("CharaKaraka count", bundle.CharaKarakaByPlanet.Count, 8);
    Check("State rows non-empty", bundle.States.Count > 0, true);

    Console.WriteLine(failures == 0 ? "\nverify-pipeline: ALL PASS" : $"\nverify-pipeline: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

If `ChartKeyDetailsRepository.GetForPersonChart` / `BirthDetailsRepository.GetByPersonKey` don't exist, add thin Dapper methods (single `SELECT`) — this is verification plumbing, not domain logic.

- [ ] **Step 5: Build + verify**

```bash
dotnet build Ikiastrro.slnx                                   # 0/0 (Cli/Data/Web usings for THIS task's types only)
dotnet run --project src/Ikiastrro.Cli -- verify-pipeline     # ALL PASS
dotnet run --project src/Ikiastrro.Cli -- verify-schema       # ALL PASS (unchanged)
```
(Broad solution `using` sweep is Task 13 — but this task's new types must not leave the solution broken; add the `using`s the touched Cli/Data files need.)

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(core): ChartPipeline/ChartBundle DB-free façade + verify-pipeline

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 8: Reserved engine seams — Dispositor / Strength / Yoga / Karaka sources

**Files (all new, interface + minimal stub only — no logic):**
- `src/Ikiastrro.Core/Engines/Dispositors/IDispositorEngine.cs`, `DispositorChain.cs`
- `src/Ikiastrro.Core/Engines/Strength/IStrengthEngine.cs`, `ShadbalaResult.cs`, `VimsopakaResult.cs`
- `src/Ikiastrro.Core/Engines/Yoga/IYogaEngine.cs`, `YogaResult.cs`
- `src/Ikiastrro.Core/Engines/Karakas/ISthiraKarakaSource.cs`, `INaisargikaKarakaSource.cs`

**Interfaces:**

```csharp
// Engines/Dispositors/IDispositorEngine.cs
namespace Ikiastrro.Core.Engines.Dispositors;
using Ikiastrro.Core.Pipeline;

public sealed record DispositorChain(string Planet, IReadOnlyList<string> Chain, string? FinalDispositor, bool InMutualReception);

/// <summary>Sign-lord traversal, final dispositor, mutual reception. Built in Plan 2.</summary>
public interface IDispositorEngine
{
    IReadOnlyList<DispositorChain> Compute(ChartAnalysisInput d1);
}
```

```csharp
// Engines/Strength/IStrengthEngine.cs
namespace Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Pipeline;

public sealed record ShadbalaResult(string Planet, decimal TotalRupas, IReadOnlyDictionary<string, decimal> ComponentRupas);
public sealed record VimsopakaResult(string Planet, string Scheme, decimal Score, decimal Max);

/// <summary>Ṣaḍbala, Iṣṭa/Kaṣṭa, Bhāva Bala, Vimśopaka. Built in Plan 3.</summary>
public interface IStrengthEngine
{
    IReadOnlyList<ShadbalaResult> Shadbala(ChartBundle bundle);
    IReadOnlyList<VimsopakaResult> Vimsopaka(ChartBundle bundle);
}
```

```csharp
// Engines/Yoga/IYogaEngine.cs
namespace Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Pipeline;

public sealed record YogaResult(string YogaCode, string Category, bool Present, bool Cancelled, string? Notes);

/// <summary>Yoga detection (Pañcha Mahāpuruṣa, Rāja, Dhana, Nābhasa, Neechabhaṅga, Parivartana…). Built in Plan 4.</summary>
public interface IYogaEngine
{
    IReadOnlyList<YogaResult> Detect(ChartBundle bundle);
}
```

```csharp
// Engines/Karakas/ISthiraKarakaSource.cs
namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>Sthira (fixed) Karaka: planet -> house significator. Built in Plan 2.</summary>
public interface ISthiraKarakaSource
{
    IReadOnlyDictionary<int, string> HouseSignificatorByHouse();   // 1..12 -> planet
}

// Engines/Karakas/INaisargikaKarakaSource.cs
namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>Naisargika (natural) Karaka ordering (Sapta / Ashta). Built in Plan 2.</summary>
public interface INaisargikaKarakaSource
{
    IReadOnlyList<string> NaturalKarakaOrder();
}
```

`DispositorChain.cs` / `ShadbalaResult.cs` / `VimsopakaResult.cs` / `YogaResult.cs` may hold just the `record` (put each named type in its own file for grep-ability, or fold into the interface file — either is fine, keep it consistent). **No `class ...Engine : I...Engine` implementations in Plan 1** — the interfaces exist so `ChartBundle` and DI can name them; the concrete engines are Plans 2–4.

- [ ] **Step 1: Create the 8 files** exactly as above.
- [ ] **Step 2: Build Core** → `dotnet build src/Ikiastrro.Core/Ikiastrro.Core.csproj` → 0 / 0.
- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat(core): reserve Dispositor/Strength/Yoga/Karaka-source engine seams (P2–P4)

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 9: Terminology tables — migration 17

**Files:**
- Create: `db/17_create_astro_terminology.sql`
- Modify: `db/ikiastrro.sql` — fold the two `CREATE TABLE`s in (after the `tbl_Dim_Source` block, ~line 2340)

- [ ] **Step 1: Write `db/17_create_astro_terminology.sql`**

```sql
:setvar DbName "ikiastrro"
SET NOCOUNT ON;
USE [$(DbName)];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE MigrationId = '17')
BEGIN
    IF OBJECT_ID('dbo.tbl_Astro_Terminology','U') IS NULL
    CREATE TABLE dbo.tbl_Astro_Terminology (
        TerminologyId   INT IDENTITY(1,1) CONSTRAINT PK_Astro_Terminology PRIMARY KEY,
        Category        VARCHAR(30)  NOT NULL,
        Code            VARCHAR(40)  NOT NULL CONSTRAINT UQ_Astro_Terminology_Code UNIQUE,
        ParentCode      VARCHAR(40)  NULL,
        EngineCode      VARCHAR(30)  NULL,
        NumericKey      INT          NULL,
        FormulaSummary  VARCHAR(300) NULL,
        DisplayOrder    INT          NOT NULL CONSTRAINT DF_Astro_Terminology_DisplayOrder DEFAULT 0,
        IsActive        BIT          NOT NULL CONSTRAINT DF_Astro_Terminology_IsActive DEFAULT 1,
        CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
            'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
            'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
            'Dasha','Yoga','Ayanamsa','Concept')),
        CONSTRAINT FK_Astro_Terminology_Parent FOREIGN KEY (ParentCode)
            REFERENCES dbo.tbl_Astro_Terminology (Code)
    );

    IF OBJECT_ID('dbo.tbl_Astro_TerminologyText','U') IS NULL
    CREATE TABLE dbo.tbl_Astro_TerminologyText (
        TerminologyTextId    INT IDENTITY(1,1) CONSTRAINT PK_Astro_TerminologyText PRIMARY KEY,
        TerminologyId        INT           NOT NULL,
        LanguageCode         CHAR(2)       NOT NULL,
        Script               VARCHAR(8)    NOT NULL CONSTRAINT DF_Astro_TerminologyText_Script DEFAULT 'Latn',
        Name                 NVARCHAR(100) NOT NULL,
        TraditionalName      NVARCHAR(100) NULL,
        ShortDescription     NVARCHAR(400) NULL,
        TechnicalDefinition  NVARCHAR(MAX) NULL,
        CalculationMethod    NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)   NULL,
        CONSTRAINT FK_Astro_TerminologyText FOREIGN KEY (TerminologyId)
            REFERENCES dbo.tbl_Astro_Terminology (TerminologyId),
        CONSTRAINT CK_Astro_TerminologyText_Lang   CHECK (LanguageCode IN ('sa','en','ta')),
        CONSTRAINT CK_Astro_TerminologyText_Script CHECK (Script IN ('Latn','Deva','Taml')),
        CONSTRAINT CK_Astro_TerminologyText_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Astro_TerminologyText UNIQUE (TerminologyId, LanguageCode, Script)
    );

    INSERT INTO dbo.SchemaMigrations (MigrationId, Description, AppliedAtUtc)
    VALUES ('17', 'Create tbl_Astro_Terminology + tbl_Astro_TerminologyText (bilingual concept catalogue)', SYSUTCDATETIME());
    PRINT '17 applied: terminology tables created.';
END
ELSE PRINT '17 already applied.';
GO
```

- [ ] **Step 2: Apply** — `sqlcmd -S localhost -E -b -i db/17_create_astro_terminology.sql` → `17 applied`.

- [ ] **Step 3: Fold into `db/ikiastrro.sql`** — paste both `CREATE TABLE` blocks (unguarded, `IF NOT EXISTS`-free is fine in the baseline's own style, or keep the guards — match neighbouring blocks) right after the `tbl_Dim_Source` section. Add a `SchemaMigrations` seed row `('17', …)` alongside the existing folded rows.

- [ ] **Step 4: Scratch rebuild** — `sed`-substitute `:setvar`, `sqlcmd -b`, confirm both tables exist in `ikiastrro_scratch`, drop it.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(db): tbl_Astro_Terminology + _Text bilingual concept catalogue (migration 17)

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 10: Terminology seed + `TerminologyCatalog` + `verify-terminology`

**Files:**
- Create: `src/Ikiastrro.Core/Reference/TerminologyCode.cs` (const strings), `TerminologyRow.cs`, `TerminologyTextRow.cs`, `TerminologyCatalog.cs`
- Create: `src/Ikiastrro.Data/TerminologyRepository.cs` (load all rows)
- Create: `seed-terminology` + `verify-terminology` blocks in `src/Ikiastrro.Cli/Program.cs`
- Modify: `db/ikiastrro.sql` — fold the generated seed (`MERGE` on `Code`) in after Task 9's tables

**Categories to seed** (`sa`/`Latn` + `en`/`Latn` for every row; `NumericKey` = the enum/Dim integer):

| Category | Count | Source of rows | `Code` pattern |
|---|---|---|---|
| Planet | 9 | `PlanetName` enum + `tbl_Planets` | `PLANET_<NAME>` (`PLANET_SUN`…`PLANET_KETU`) |
| Sign | 12 | `ZodiacName` enum + `tbl_SignAttributes` | `SIGN_<NAME>` (`SIGN_ARIES`…; `Capricornus`→`SIGN_CAPRICORN`) |
| House | 12 | 1..12 | `HOUSE_01`…`HOUSE_12` |
| Nakshatra | 27 | `ConstellationName` enum + `tbl_Nakshatras` | `NAK_<NAME>` |
| NakshatraPada | 108 | `tbl_NakshatraPadas` | `NAKPADA_<NAK>_<n>` (D4), `ParentCode = NAK_<NAME>` |
| DivisionalChart | 21 | `tbl_Dim_ChartType` + `tbl_Rule_VargaScheme` | `VARGA_<CHARTTYPE>` (`VARGA_D1`, `VARGA_D2_US`…) |
| Karaka | 8 | `CharaKaraka` enum | `KARAKA_<AK|AMK|BK|MK|PIK|PK|GK|DK>` |
| SpecialPoint | ~15 | `SpecialPointSeed` codes (`AL`, `A2`..`A12`, `HL`, `Gulika`, `Maandi`) | `SPT_<CODE>` |
| AvasthaState | 8 | `tbl_Dim_PlanetaryState` rows | `AVASTHA_<SYSTEM>_<STATENAME>` (e.g. `AVASTHA_BAALADI_BAALA`) |
| DignityState | 9 | distinct `DignityStatus` values | `DIGNITY_<SLUG>` (`DIGNITY_EXALTED`…) |
| Relationship | 6 | Yuti / Dṛṣṭi / Combust / GreatFriend / Friend / … | `REL_<SLUG>` |
| Ayanamsa | 1 | Lahiri | `AYANAMSA_LAHIRI` |

`en` `Name` for AvasthaState rows carries the English gloss (`Infant` for `AVASTHA_BAALADI_BAALA`, `Awake` for `AVASTHA_JAGRADADI_JAGRAT`, …); `sa` `Name` carries the romanized Sanskrit (`Baala`, `Jagrat`). `EngineCode` per spec §4 (`ASTRO_CALC`, `VARGA`, `HOUSE`, `NAKSHATRA`, `DIGNITY`, `KARAKA`, `AVASTHA`, `RELATIONSHIP`, `DASHA`).

- [ ] **Step 1: `TerminologyCode.cs`**

```csharp
namespace Ikiastrro.Core.Reference;

/// <summary>Every terminology Code as a compile-time constant. No bare Code string literal
/// appears anywhere else in C#/Razor — resolve display via TerminologyCatalog.</summary>
public static class TerminologyCode
{
    // Planets
    public const string PlanetSun = "PLANET_SUN";
    public const string PlanetMoon = "PLANET_MOON";
    // … all 9
    // Signs
    public const string SignAries = "SIGN_ARIES";
    public const string SignCapricorn = "SIGN_CAPRICORN";
    // … all 12
    // Karakas
    public const string KarakaAtmakaraka = "KARAKA_AK";
    // … AmK/BK/MK/PiK/PK/GK/DK
    // (Houses, Nakshatras, Vargas, Avastha states, Dignity states, Relationships, Ayanamsa
    //  — one const per Code the C# actually references; the full 200+-row set lives in the DB,
    //  not here. Add a const only when code needs to name that Code.)
}
```

- [ ] **Step 2: `TerminologyRow.cs` / `TerminologyTextRow.cs`**

```csharp
namespace Ikiastrro.Core.Reference;

public sealed record TerminologyRow(
    int TerminologyId, string Category, string Code, string? ParentCode,
    string? EngineCode, int? NumericKey, string? FormulaSummary, int DisplayOrder, bool IsActive);

public sealed record TerminologyTextRow(
    int TerminologyId, string LanguageCode, string Script, string Name,
    string? TraditionalName, string? ShortDescription, string? TechnicalDefinition,
    string? CalculationMethod, string? SourceRefCode);
```

- [ ] **Step 3: `TerminologyCatalog.cs`**

```csharp
namespace Ikiastrro.Core.Reference;

/// <summary>Loaded-once, cached lookup over tbl_Astro_Terminology(+Text). Default language 'sa'.</summary>
public sealed class TerminologyCatalog
{
    private readonly IReadOnlyDictionary<string, TerminologyRow> _byCode;
    private readonly IReadOnlyDictionary<(string Code, string Lang), TerminologyTextRow> _text;

    public TerminologyCatalog(IEnumerable<TerminologyRow> rows, IEnumerable<TerminologyTextRow> text)
    {
        _byCode = rows.ToDictionary(r => r.Code);
        _text = text
            .Where(t => _idToCode.TryGetValue(t.TerminologyId, out _))  // pseudo; join on TerminologyId
            .ToDictionary(t => (_idToCode[t.TerminologyId], t.LanguageCode));
        // implementation detail: build an id->code map from rows first
    }

    public string Label(string code, string lang = "sa") =>
        _text.TryGetValue((code, lang), out var t) ? t.Name
        : _text.TryGetValue((code, "en"), out var en) ? en.Name
        : code;

    public string? Traditional(string code, string lang = "sa") =>
        _text.GetValueOrDefault((code, lang))?.TraditionalName;

    public string? Describe(string code, string lang = "sa") =>
        _text.GetValueOrDefault((code, lang))?.ShortDescription
        ?? _text.GetValueOrDefault((code, "en"))?.ShortDescription;

    public TerminologyRow? Meta(string code) => _byCode.GetValueOrDefault(code);
}
```

- [ ] **Step 4: `TerminologyRepository.cs`** (in `Ikiastrro.Data`) — two `SELECT`s (`tbl_Astro_Terminology`, `tbl_Astro_TerminologyText` joined for the id) returning the two record lists; a `BuildCatalog()` returning `new TerminologyCatalog(rows, text)`.

- [ ] **Step 5: `seed-terminology` CLI mode**

A block that builds every row in code from the enums + Dim tables (per the category table above) and issues idempotent `MERGE dbo.tbl_Astro_Terminology AS tgt USING (…) src ON tgt.Code = src.Code` + a second `MERGE` for `tbl_Astro_TerminologyText` keyed `(TerminologyId, LanguageCode, Script)`. Reads `tbl_Planets` / `tbl_SignAttributes` / `tbl_Nakshatras` / `tbl_NakshatraPadas` / `tbl_Dim_ChartType` / `tbl_Dim_PlanetaryState` via Dapper for the `TraditionalName` / `ShortDescription` prose (fall back to the enum name where a table has none). Print `seed-terminology: N terminology rows, M text rows`.

Run it: `dotnet run --project src/Ikiastrro.Cli -- seed-terminology`.

- [ ] **Step 6: `verify-terminology` CLI mode**

```csharp
if (args.Length > 0 && args[0] == "verify-terminology")
{
    var repo = new TerminologyRepository(connectionFactory);
    var rows = repo.GetTerminology();      // List<TerminologyRow>
    var text = repo.GetTerminologyText();  // List<TerminologyTextRow>
    var failures = 0;
    void Fail(string m) { Console.WriteLine($"  [FAIL] {m}"); failures++; }

    var codes = rows.Select(r => r.Code).ToHashSet();
    // 1. every ZodiacName / PlanetName / CharaKaraka enum value has a Code
    foreach (var p in Enum.GetNames<Ikiastrro.Core.Engines.Astronomy.PlanetName>())
        if (!codes.Contains($"PLANET_{p.ToUpperInvariant()}")) Fail($"no Code for planet {p}");
    foreach (var s in Enum.GetNames<Ikiastrro.Core.Engines.Astronomy.ZodiacName>())
    {
        var slug = s == "Capricornus" ? "CAPRICORN" : s.ToUpperInvariant();
        if (!codes.Contains($"SIGN_{slug}")) Fail($"no Code for sign {s}");
    }
    // 2. every Code has sa + en text
    var textByCodeLang = text.Join(rows, t => t.TerminologyId, r => r.TerminologyId, (t, r) => (r.Code, t.LanguageCode))
                             .ToHashSet();
    foreach (var c in codes)
    {
        if (!textByCodeLang.Contains((c, "sa"))) Fail($"{c}: missing 'sa' text");
        if (!textByCodeLang.Contains((c, "en"))) Fail($"{c}: missing 'en' text");
    }
    // 3. no orphan ParentCode
    foreach (var r in rows.Where(r => r.ParentCode is not null))
        if (!codes.Contains(r.ParentCode!)) Fail($"{r.Code}: orphan ParentCode {r.ParentCode}");
    // 4. every tbl_Dim_ChartType / tbl_Dim_PlanetaryState row maps to a Code
    //    (query the two Dim tables, assert VARGA_* / AVASTHA_* presence)

    Console.WriteLine(failures == 0 ? "\nverify-terminology: ALL PASS" : $"\nverify-terminology: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

- [ ] **Step 7: Fold the seed into `db/ikiastrro.sql`**

Dump the seeded rows to static `MERGE` statements (or `INSERT … SELECT … WHERE NOT EXISTS`) appended after the Task 9 tables, so a fresh baseline install has the full `sa`+`en` catalogue without running the CLI. Keep it regenerable — a comment `-- regenerate via: dotnet run --project src/Ikiastrro.Cli -- seed-terminology && <dump>`.

- [ ] **Step 8: Build + verify**

```bash
dotnet build Ikiastrro.slnx                                   # 0/0
dotnet run --project src/Ikiastrro.Cli -- verify-terminology  # ALL PASS
```

- [ ] **Step 9: Scratch rebuild** — `sed`-substitute `:setvar`, `sqlcmd -b`, assert `SELECT COUNT(*) FROM ikiastrro_scratch.dbo.tbl_Astro_Terminology` ≥ 200 and every row has 2 text rows; drop scratch.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "feat: TerminologyCatalog + seed-terminology + verify-terminology (sa+en)

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 11: Rule-table portability tail + `tbl_Rule_Catalog` + reserved tables — migration 18

**Files:**
- Create: `db/18_rule_table_portability.sql`
- Modify: `db/ikiastrro.sql` — fold migration 18

**Portability tail** — add any missing column to each of: `tbl_Rule_VargaScheme`, `tbl_Rule_AspectOffset`, `tbl_Rule_CombustionOrb`, `tbl_Rule_NaturalRelationship`, `tbl_Rule_TemporaryFriendshipDistance`, `tbl_Rule_AgeState`, `tbl_Rule_WakefulnessState`:

```sql
  MethodCode           VARCHAR(30)   NULL   -- interpreter tag; backfilled in Task 12
  RuleParametersJson   NVARCHAR(MAX) NULL   -- + CHECK (… IS NULL OR ISJSON(…) = 1)
  CalculationNarrative NVARCHAR(MAX) NULL
  SourceRefCode        VARCHAR(40)   NULL   -- + CHECK (… IS NULL OR … LIKE 'SRC[_]%')
  IsActive             BIT NOT NULL CONSTRAINT DF_<table>_IsActive DEFAULT 1
```

Backfill `SourceRefCode` for the rows that have a known source (per spec §9): `tbl_Rule_AspectOffset` → `SRC_BPHS_26`; `tbl_Rule_CombustionOrb` → `SRC_BPHS_COMBUSTION` (add the `SRC_*` row to `tbl_Dim_Source` / `docs/research/reference-sources.md` first **if it does not already exist** — check with `verify-sources`); `tbl_Rule_NaturalRelationship` / `tbl_Rule_TemporaryFriendshipDistance` → `SRC_BPHS_MAITRI` or the existing closest code. Use **only** codes that resolve in `tbl_Dim_Source`; if the right code is missing, add it to migration 15's fold + the `.md` mirror in this task and note it in the commit.

**`tbl_Rule_Catalog`:**

```sql
CREATE TABLE dbo.tbl_Rule_Catalog (
    RuleTableName   VARCHAR(80)  CONSTRAINT PK_Rule_Catalog PRIMARY KEY,
    EngineCode      VARCHAR(30)  NOT NULL,
    MethodCodes     VARCHAR(300) NOT NULL,
    Purpose         VARCHAR(400) NOT NULL,
    IntroducedIn    VARCHAR(40)  NOT NULL
);
```

Seed rows (12):

| RuleTableName | EngineCode | MethodCodes | IntroducedIn |
|---|---|---|---|
| `tbl_Rule_VargaScheme` | `VARGA` | `LINEAR_VARGA,TABLE_VARGA,BAND_VARGA,WRAPPED_VARGA` | `migration 11` |
| `tbl_Rule_AspectOffset` | `RELATIONSHIP` | `OFFSET_LIST` | `baseline` |
| `tbl_Rule_CombustionOrb` | `RELATIONSHIP` | `ORB_PAIR` | `baseline` |
| `tbl_Rule_NaturalRelationship` | `DIGNITY` | `MAP_LOOKUP` | `baseline` |
| `tbl_Rule_TemporaryFriendshipDistance` | `DIGNITY` | `DISTANCE_SET` | `baseline` |
| `tbl_Rule_AgeState` | `AVASTHA` | `BAND_LOOKUP` | `migration 00 / renamed 16` |
| `tbl_Rule_WakefulnessState` | `AVASTHA` | `MAP_LOOKUP` | `migration 00 / renamed 16` |
| `tbl_Rule_HouseSignification` | `HOUSE` | `MAP_LOOKUP` | `migration 18 (empty; P2)` |
| `tbl_Rule_Karaka` | `KARAKA` | `MAP_LOOKUP` | `migration 18 (empty; P2)` |
| `tbl_Rule_ShadbalaComponent` | `STRENGTH` | `WEIGHT_TABLE` | `migration 18 (empty; P3)` |
| `tbl_Rule_VimsopakaWeight` | `STRENGTH` | `WEIGHT_TABLE` | `migration 18 (empty; P3)` |
| `tbl_Rule_Yoga` | `YOGA` | `PREDICATE_SET` | `migration 18 (empty; P4)` |

**Five reserved tables** — minimal skeleton, portability tail included, populated later:

```sql
CREATE TABLE dbo.tbl_Rule_HouseSignification (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_HouseSignification PRIMARY KEY,
    RuleSetId INT NOT NULL,
    HouseNumber TINYINT NOT NULL,
    SignificationCode VARCHAR(40) NOT NULL,
    MethodCode VARCHAR(30) NULL, RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL, SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_HouseSignification_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_HouseSignification_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson)=1),
    CONSTRAINT CK_Rule_HouseSignification_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
-- tbl_Rule_Karaka:            (RuleSetId, KarakaScheme VARCHAR(20), PlanetOrHouse VARCHAR(20), TargetValue VARCHAR(40), OrderIndex TINYINT, ReverseForRahu BIT) + tail
-- tbl_Rule_ShadbalaComponent: (RuleSetId, BalaCode VARCHAR(30), SubComponentCode VARCHAR(40), WeightRupas DECIMAL(6,3), MaxRupas DECIMAL(6,3), LookupJson NVARCHAR(MAX)) + tail
-- tbl_Rule_VimsopakaWeight:   (RuleSetId, SchemeCode VARCHAR(20), VargaChartType VARCHAR(10), Weight DECIMAL(5,2), MaxTotal DECIMAL(6,2)) + tail
-- tbl_Rule_Yoga:              (RuleSetId, YogaCode VARCHAR(40), YogaCategory VARCHAR(30), RequirementJson NVARCHAR(MAX), CancellationJson NVARCHAR(MAX), ResultCode VARCHAR(40)) + tail
```

- [ ] **Step 1: Write `db/18_rule_table_portability.sql`** — one `IF NOT EXISTS (… MigrationId='18')` block: the `IF COL_LENGTH(...) IS NULL ALTER TABLE ... ADD ...` set for the 7 tables (+ the two `CHECK`s per table, added as separate `ALTER TABLE ... ADD CONSTRAINT` guarded by `IF OBJECT_ID('...','C') IS NULL`), then the `SourceRefCode` backfill `UPDATE`s, then `CREATE TABLE dbo.tbl_Rule_Catalog` + its 12-row `MERGE`, then the 5 reserved `CREATE TABLE`s, then the `SchemaMigrations` insert + `PRINT '18 applied …'`.

- [ ] **Step 2: Apply** — `sqlcmd -S localhost -E -b -i db/18_rule_table_portability.sql` → `18 applied`.

- [ ] **Step 3: Fold into `db/ikiastrro.sql`** — add the tail columns + `CHECK`s directly into each `tbl_Rule_*` `CREATE TABLE` block; add the `tbl_Rule_Catalog` table + seed and the 5 reserved tables after them; add the `('18', …)` `SchemaMigrations` row.

- [ ] **Step 4: `verify-sources` still green** — `dotnet run --project src/Ikiastrro.Cli -- verify-sources` → ALL PASS (every backfilled `SourceRefCode` resolves).

- [ ] **Step 5: Scratch rebuild** — `sed`-substitute `:setvar`, `sqlcmd -b`, assert `tbl_Rule_Catalog` has 12 rows and each `tbl_Rule_*` table has a `MethodCode` column; drop scratch.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(db): rule-table portability tail + tbl_Rule_Catalog + reserved rule tables (migration 18)

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 12: Backfill `tbl_Rule_VargaScheme` params + varga interpreters + `verify-rules`

**Files:**
- Create: `src/Ikiastrro.Core/Engines/DivisionalCharts/IVargaMethodInterpreter.cs`, `LinearVargaInterpreter.cs`, `TableVargaInterpreter.cs`, `BandVargaInterpreter.cs`, `WrappedVargaInterpreter.cs`, `VargaMethodInterpreterFactory.cs`
- Create: `seed-rule-params` + `verify-rules` blocks in `src/Ikiastrro.Cli/Program.cs`
- Modify: `db/ikiastrro.sql` — fold the backfilled `MethodCode` + `RuleParametersJson` values as static `UPDATE`s (or into the `tbl_Rule_VargaScheme` seed `MERGE`)

**The interpreter contract** (what a port reimplements — spec §9):

```csharp
namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>Reads a tbl_Rule_VargaScheme row's RuleParametersJson and produces the varga sign
/// for a sidereal longitude — the C#-independent form of a *SignRule class.</summary>
public interface IVargaMethodInterpreter
{
    string MethodCode { get; }
    ZodiacName SignFor(string ruleParametersJson, double siderealLongitude);
}
```

- `LinearVargaInterpreter` (`LINEAR_VARGA`) — JSON `{ "factor": 3, "stride": 4 }`; body = `LinearVargaSignRule`'s formula.
- `TableVargaInterpreter` (`TABLE_VARGA`) — JSON `{ "oddParts": [0,10,8,2,6], "evenParts": [1,5,11,9,7], "partDegrees": 6.0 }`; body = the odd/even part-index lookup shared by `PanchamsaD5SignRule` etc.
- `BandVargaInterpreter` (`BAND_VARGA`) — JSON `{ "bands": [{ "toDeg": 5, "oddSign": 0, "evenSign": … }, …] }`; body = `TrimsamsaD30SignRule`'s unequal bands.
- `WrappedVargaInterpreter` (`WRAPPED_VARGA`) — JSON `{ "closedForm": "GetNavamsaSign" }`; dispatches to the existing static `AstroMath.GetNavamsaSign` / `GetDasamsaSign` / `GetShashtamsaSign` / `GetRudramsaSign` (D6/D9/D10/D11). These stay in C# — the narrative + `SourceRefCode` columns carry the "how" for a port.

**Mapping (21 chart types → `MethodCode`)** — per spec §9:

| MethodCode | Chart types |
|---|---|
| `LINEAR_VARGA` | D1, D3, D4, D12, D60 |
| `TABLE_VARGA` | D2, D2-US, D5, D7, D8, D16, D20, D24, D27, D40, D45 |
| `BAND_VARGA` | D30 |
| `WRAPPED_VARGA` | D6, D9, D10, D11 |

- [ ] **Step 1: Write the 5 interpreter files + factory** (`VargaMethodInterpreterFactory.For(methodCode)` → the right `IVargaMethodInterpreter`).

- [ ] **Step 2: `seed-rule-params` CLI mode** — for each active `tbl_Rule_VargaScheme` row: look up its `ChartType`, decide `MethodCode` from the table above, build `RuleParametersJson` by reading the corresponding `*SignRule` class's constructor args / static arrays (they are `public`/`internal` — expose `internal` where needed, `Ikiastrro.Cli` already sees internals via `InternalsVisibleTo` or make them `public static readonly`), and `UPDATE dbo.tbl_Rule_VargaScheme SET MethodCode=@m, RuleParametersJson=@j, CalculationNarrative=@n WHERE Id=@id`. Idempotent. Print a line per row.

- [ ] **Step 3: Run it** — `dotnet run --project src/Ikiastrro.Cli -- seed-rule-params` → 21 rows updated.

- [ ] **Step 4: `verify-rules` CLI mode**

```csharp
if (args.Length > 0 && args[0] == "verify-rules")
{
    var conn = connectionFactory.Create();
    var failures = 0;
    void Fail(string m) { Console.WriteLine($"  [FAIL] {m}"); failures++; }

    // 1. tbl_Rule_Catalog covers every tbl_Rule_* table
    var ruleTables = conn.Query<string>(
        "SELECT name FROM sys.tables WHERE name LIKE 'tbl_Rule[_]%' AND name <> 'tbl_Rule_Sets' AND name <> 'tbl_Rule_Catalog'").ToList();
    var cataloged = conn.Query<string>("SELECT RuleTableName FROM dbo.tbl_Rule_Catalog").ToHashSet();
    foreach (var t in ruleTables) if (!cataloged.Contains(t)) Fail($"tbl_Rule_Catalog missing row for {t}");

    // 2. every tbl_Rule_VargaScheme row: MethodCode is a known interpreter, JSON parses
    var schemes = conn.Query("SELECT s.Id, ct.ChartType, s.MethodCode, s.RuleParametersJson " +
        "FROM dbo.tbl_Rule_VargaScheme s JOIN dbo.tbl_Dim_ChartType ct ON ct.Id = s.ChartTypeId " +
        "WHERE s.RuleSetId = (SELECT MAX(Id) FROM dbo.tbl_Rule_Sets WHERE IsActive = 1)").ToList();

    foreach (var row in schemes)
    {
        string chartType = row.ChartType, methodCode = row.MethodCode;
        string json = row.RuleParametersJson;
        var interp = VargaMethodInterpreterFactory.For(methodCode);
        var cls = VargaSignRuleFactory.For(chartType);          // the existing C# rule class

        // 3. interpreter(JSON, lon) == C# rule class(lon) for the full circle, 0.5° steps
        for (double lon = 0; lon < 360; lon += 0.5)
        {
            var a = interp.SignFor(json, lon);
            var b = cls.SignFor(lon);
            if (a != b) { Fail($"{chartType} @ {lon:0.0}°: interpreter {a} != class {b}"); break; }
        }
    }

    Console.WriteLine(failures == 0 ? "\nverify-rules: ALL PASS" : $"\nverify-rules: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

(`VargaSignRuleFactory.For(chartType)` — if the factory currently keys on `SignRuleKey` not `ChartType`, add a `ChartType`→rule overload or map via the scheme row's `SignRuleKey`.)

- [ ] **Step 5: Fold the backfill into `db/ikiastrro.sql`** — the 21 `MethodCode` / `RuleParametersJson` / `CalculationNarrative` values as static `UPDATE dbo.tbl_Rule_VargaScheme SET … WHERE Id = …;` after the scheme `MERGE`, with a `-- regenerate via: … seed-rule-params` comment.

- [ ] **Step 6: Build + full verify**

```bash
dotnet build Ikiastrro.slnx                              # 0/0
for m in verify-schema verify-vargas verify-functional-nature verify-jaimini verify-avastha verify-sources verify-pipeline verify-terminology verify-rules; do
  echo "== $m =="; dotnet run --project src/Ikiastrro.Cli -- $m || exit 1
done
```
All nine → `ALL PASS`.

- [ ] **Step 7: Scratch rebuild** — `sed`-substitute, `sqlcmd -b`, assert `SELECT COUNT(*) FROM ikiastrro_scratch.dbo.tbl_Rule_VargaScheme WHERE MethodCode IS NOT NULL` = 21; drop scratch.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: varga rule interpreters + params backfill + verify-rules

Every tbl_Rule_VargaScheme.RuleParametersJson round-trips through its interpreter to the
same sign as its C# *SignRule class, all 21 vargas, full circle.

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 13: Downstream `using` sweep — Cli / Data / Web

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs`
- Modify: every `.cs` in `src/Ikiastrro.Data/` that referenced a moved namespace
- Modify: every `.razor` / `.razor.cs` / `Program.cs` in `src/Ikiastrro.Web/` with an affected `@using` / `using`

**Interfaces:** none — mechanical `using` rewrites per the "Downstream `using` rewrites" table at the top of this plan.

- [ ] **Step 1: Re-grep the current state**

```bash
grep -rn "using Ikiastrro.Core.Astro;\|using Ikiastrro.Core.Calculators;\|using Ikiastrro.Core.Jaimini;\|using Ikiastrro.Core.SpecialPoints;\|using Ikiastrro.Core.Dasha;\|@using Ikiastrro.Core.Astro\|@using Ikiastrro.Core.Calculators\|@using Ikiastrro.Core.Jaimini\|@using Ikiastrro.Core.Dasha" src/Ikiastrro.Cli src/Ikiastrro.Data src/Ikiastrro.Web
```

- [ ] **Step 2: Rewrite each hit**

Per file, replace the old `using` with the specific new namespace(s) the file actually needs (compiler tells you which). Common cases:
- CLI `Program.cs`: `using Ikiastrro.Core.Astro;` → `using Ikiastrro.Core.Engines.Astronomy;` + `using Ikiastrro.Core.Engines.DivisionalCharts;`; `using Ikiastrro.Core.Calculators;` → `using Ikiastrro.Core.Pipeline;` + `using Ikiastrro.Core.Engines.{Position,Dignity,Relationships,Houses,PlanetaryStates};` as needed; `using Ikiastrro.Core.Jaimini;` → `using Ikiastrro.Core.Engines.Karakas;`; `using Ikiastrro.Core.Dasha;` → `using Ikiastrro.Core.Engines.Dasha;`.
- `Ikiastrro.Data`: `ChartGenerationService.cs`, `VargaSchemeRepository.cs`, the renamed `PlanetaryStateRuleRepository.cs` / `PlanetaryStateRepository.cs`, DI wiring in `ServiceCollectionExtensions` (or wherever repos register).
- `Ikiastrro.Web`: the 13 `@using` sites (ChartView, ChartWorkspace, LifeWeeks, Add, Home, `_Imports.razor`). Prefer consolidating shared ones into `src/Ikiastrro.Web/_Imports.razor`.

- [ ] **Step 3: `dotnet` build 0/0**

```bash
dotnet build Ikiastrro.slnx -warnaserror
```
Expected: **0 Warning(s), 0 Error(s)**.

- [ ] **Step 4: VS/MSBuild build 0/0**

```bash
dotnet build Ikiastrro.slnx -p:Configuration=Release
```
(and, if available, a Visual Studio build — the Global Constraint is "both `dotnet` and VS"). Expected 0/0.

- [ ] **Step 5: Full verify sweep + Web smoke**

```bash
for m in verify-schema verify-vargas verify-functional-nature verify-jaimini verify-avastha verify-sources verify-pipeline verify-terminology verify-rules; do
  dotnet run --project src/Ikiastrro.Cli -- $m || exit 1
done
# Web smoke:
dotnet run --project src/Ikiastrro.Web &  WEBPID=$!
sleep 8 && curl -sk -o /dev/null -w '%{http_code}\n' https://localhost:5001/charts/1   # -> 200
kill $WEBPID
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: sweep Cli/Data/Web usings onto Engines/* namespaces; build 0/0

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```

---

## Task 14: Docs — STANDARDS §D.2, ARCHITECTURE, indexes, engine map, memory

**Files:**
- Modify: `D:\@ClaudeSpace\STANDARDS.md` — confirm/extend §D.2 (hybrid naming) with the resolved rules from this plan (English structural; canonical romanized Sanskrit nouns; ASCII `Code`; no hard-coded display strings in new code; `TerminologyCatalog` is the resolver)
- Modify: `ARCHITECTURE.md` — new "Engine stack" section: the 14-engine table (spec §4) with folder + namespace + status, the `ChartPipeline`/`ChartBundle` façade, the rule-table portability model (`tbl_Rule_Catalog` + `MethodCode` interpreters)
- Modify: `master_ikiastrro.md` — flip the spec + this plan to `living`/`done` rows; add `tbl_Astro_Terminology`, `tbl_Rule_Catalog`, `INFRASTRUCTURE.md` doc-map entries if missing
- Modify: `PRODUCT.md` — tick the 5-box checklist for the features this plan touched: FEAT-*-ENGINE-REORG (Core ✅ Verify ✅ Docs ✅), terminology feature (DB ✅ Core ✅ Verify ✅ Docs ✅), rule-portability feature (DB ✅ Verify ✅ Docs ✅); rollup table refreshed
- Regenerate: `docs/artifacts/dotnet_engine_map.md` — the D2 diagram + per-folder tables now grouped by `Engines/<Name>/` (re-render `docs/artifacts/diagrams/*.d2` → `.svg` with `d2 --theme 0 --pad 20`)
- Modify: `D:\@ClaudeSpace\ikiastrro.md` — a 2026-09-02 "Plan 1 complete" section (the 14 tasks, migrations 16–18, the 3 new verify modes, the engine layout)
- Modify: `C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md` + `MEMORY.md` line 4 — Plan 1 done; NEXT = Plan 2 (Dispositor + Sthira/Naisargika Karaka + Dīptādi/Lajjitādi Avastha)

- [ ] **Step 1: STANDARDS §D.2** — read the current §D.2 (added in Plan 0), append a "Resolved in Plan 1" note only if it is not already precise. No new top-level section.
- [ ] **Step 2: ARCHITECTURE.md engine section** — write the table + two short paragraphs. Cite no classical sources inline (link `docs/research/*` if needed).
- [ ] **Step 3: master_ikiastrro.md + PRODUCT.md** — status flips + checklist ticks + rollup.
- [ ] **Step 4: Regenerate the engine map**

```bash
# edit docs/artifacts/dotnet_engine_map.md tables to the Engines/* grouping
d2 --theme 0 --pad 20 docs/artifacts/diagrams/dotnet_engine_map.d2 docs/artifacts/diagrams/dotnet_engine_map.svg
```

- [ ] **Step 5: ikiastrro.md history + memory** (out of repo — not part of the commit).
- [ ] **Step 6: Final full build + verify sweep** (repeat Task 13 Step 3–5) → all green.
- [ ] **Step 7: Commit** (repo files only)

```bash
git add STANDARDS.md 2>/dev/null; git add ARCHITECTURE.md master_ikiastrro.md PRODUCT.md docs/artifacts/
git commit -m "docs: engine stack in ARCHITECTURE; PRODUCT/master indexes; regenerate engine map

$(printf 'Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Nv6jCi8URzcW2gC93A74EE')"
```
(`STANDARDS.md` lives at `D:\@ClaudeSpace\STANDARDS.md` — outside the repo; edit it but do not `git add` a path that is not in the repo.)

---

## Verification summary (whole plan)

| Mode | Task | Expectation |
|---|---|---|
| `dotnet build Ikiastrro.slnx` (+ `-warnaserror`, + Release/VS) | 13, 14 | 0 Warning(s) / 0 Error(s) |
| `verify-schema` | 5, 7, 13 | unchanged golden values |
| `verify-vargas` | 2, 12, 13 | unchanged golden values |
| `verify-functional-nature` | 13 | unchanged |
| `verify-jaimini` | 3, 13 | unchanged |
| `verify-avastha` | 6, 13 | renamed symbols, **same** expected strings |
| `verify-sources` | 11, 13 | every backfilled `SourceRefCode` resolves |
| `verify-pipeline` | 7, 13 | `ChartPipeline.Run` reproduces person 1's D1 `tbl_Chart_KeyDetails` |
| `verify-terminology` | 10, 13 | every enum/Dim value has a `Code`; every `Code` has `sa`+`en`; no orphan `ParentCode` |
| `verify-rules` | 12, 13 | `tbl_Rule_Catalog` covers every `tbl_Rule_*`; every `RuleParametersJson` round-trips to its C# rule class for all 21 vargas |
| scratch-DB rebuild of `db/ikiastrro.sql` | 6, 9, 10, 11, 12 | clean, via the `:setvar`-line `sed` substitution |
| Web smoke `/charts/1` | 13 | HTTP 200 |
| pre/post `tbl_Chart_KeyDetails` diff (person 1) | 5 | no diff |

## Deferred to Plans 2–4 (reserved here, not built)

| Plan | Adds |
|---|---|
| **P2** | `DispositorEngine` impl; Sthira + Naisargika Karaka; Dīptādi + Lajjitādi Avastha slices; populate `tbl_Rule_HouseSignification` (migration 030 scope), `tbl_Rule_Karaka` |
| **P3** | `StrengthEngine` — Ṣaḍbala (6 components), Iṣṭa/Kaṣṭa, Bhāva Bala, Vimśopaka; populate `tbl_Rule_ShadbalaComponent`, `tbl_Rule_VimsopakaWeight`; `tbl_Chart_Strength` |
| **P4** | `YogaEngine` — `PREDICATE_SET` DSL, populate `tbl_Rule_Yoga`; `tbl_Chart_Yoga`; first catalogue slice = Pañcha Mahāpuruṣa + 2–3 Rāja/Dhana |

## Self-Review (done by plan author)

- **Spec coverage:** spec §12 "Plan 1" items 1–9 → Tasks 1–8 (reorg + split + avastha + pipeline + seams) & 13 (using sweep); item 5 (terminology) → Tasks 9–10; item 6 (rule portability) → Tasks 11–12; item 9 (docs) → Task 14. Spec §4 engine table → Task 8 + Task 14 ARCHITECTURE section. Spec §8 DDL → Task 9. Spec §9 DDL + interpreter set → Tasks 11–12. Spec §10 façade → Task 7. Spec §11 verification → the summary table above. No spec §12-P1 item is unassigned.
- **Placeholder scan:** the `<<< lifted verbatim >>>` markers in Task 5 Step 2 point at named methods in the source file with exact line ranges given — the implementer copies existing code, which is the intent, not a placeholder. `TerminologyCode.cs` deliberately lists "one const per Code the C# references" rather than 200+ — the full set is DB data, asserted by `verify-terminology`.
- **Type consistency:** `PlanetaryStateRuleSet` fields renamed consistently (`AgeBands` / `WakefulnessByDignity`) across Task 6's model file, `AgeStateCalculator.For`, `PlanetaryStateComputer.Compute`, the repo, and `verify-avastha`. `ChartBundle` / `ChartPipeline` signatures in Task 7 match their use in `verify-pipeline` (Task 7 Step 4) and Task 8's `IStrengthEngine` / `IYogaEngine` (`Detect(ChartBundle)`). `IVargaMethodInterpreter.SignFor(json, lon)` matches its call in `verify-rules` (Task 12 Step 4).
- **Migration numbers:** 16, 17, 18 — contiguous after Plan 0's 15; each self-records and is folded.
