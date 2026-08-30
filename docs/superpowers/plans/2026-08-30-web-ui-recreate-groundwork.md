# Web UI Recreate — Groundwork (Plan 1 of 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the Core / Data / CLI / DB groundwork the life-area Web UI depends on — renames, the functional benefic/malefic computation, batch and new read repositories, a shared chart-generation service, and migration 031 — with no visible UI change.

**Architecture:** Additive. Two mechanical renames in Core; one new pure Core class (`LagnaFunctionalNature`) verified by a CLI assertion mode in the mould of `verify-vargas`; new read methods on existing Dapper repositories plus a few new thin ones; one new `Ikiastrro.Data` service (`ChartGenerationService`) that both the Blazor `Add.razor` and the CLI call in place of their duplicated compute-and-store pipelines; one new DB migration. No chart-schema change.

**Tech Stack:** .NET 8 / C# (`Ikiastrro.Core`, `.Data`, `.Cli`, `.Web`), Dapper, MS SQL Server (Windows Auth, `localhost`, DB `vedic_horo_gen`), SwissEphNet. Build/run: `dotnet build`, `dotnet run --project src/Ikiastrro.Cli -- <mode>`. Migrations: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\NNN_*.sql`.

**Spec:** `docs/superpowers/specs/2026-08-30-web-ui-life-area-recreate-design.md` (this plan implements its **Phase 0** — §2, §5 renames, §6.1, §7, §8, §11, §12; the UI Phases 1–6 are Plan 2).

## Global Constraints

- **Not under git; no test project.** "Checkpoint" steps are verification gates, not commits. If version control is wanted, run `git init` in the project root first and treat each checkpoint as a commit.
- **Pure-function verification** is done by CLI assertion modes (`verify-vargas` exists; this plan adds `verify-functional-nature`) — worked-example asserts, `Environment.Exit(1)` on any failure. **DB verification** is `sqlcmd` cross-check queries.
- **No UI change in this plan.** `Add.razor` is edited only to call the new service (behaviour-equivalent + it now also computes Dasha). No `.razor` markup, no CSS.
- **Migration numbering:** `030` stays reserved (unapplied) for house significations; this plan uses **`031`** only. `032`–`034` are already applied.
- **ID conventions (verified in existing repos):** `tbl_Planets.Id = (int)PlanetName + 1` (Sun=1 … Saturn=7, Rahu=8, Ketu=9). `tbl_SignAttributes.Id = (int)ZodiacName + 1` (Aries=1 … Capricornus=10 … Pisces=12).
- **`ZodiacName`** (`src/Ikiastrro.Core/Astro/ZodiacName.cs`): `Aries=0 … Capricornus=9 … Pisces=11`. The Latin `Capricornus` spelling is deliberate.
- **`PlanetName`** (`src/Ikiastrro.Core/Astro/PlanetName.cs`): `Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Rahu, Ketu` (0–8). `PlanetNames.All9` is the ordered list.
- **`EngineVersion`** string for any new `ChartResult`: reuse whatever the calculators already emit — this plan does not create `ChartResult` rows by hand.
- **Transactions:** the repo layer opens a connection per call and no write method accepts an injected transaction. Rather than overload every repo, `ChartGenerationService` uses **delete-all-first-then-regenerate** ordering so any partial failure is fully recovered by re-running the same call. A true cross-repo DB transaction is deferred (would require injecting a connection into every `tbl_Chart_*` write method) — noted against spec §11.

---

## File Structure

**Core**
- `src/Ikiastrro.Core/Calculators/D1ChartViewModel.cs` → **rename to** `ChartViewModel.cs` (class `D1ChartViewModel` → `ChartViewModel`; record `D1PlanetRow` → `PlanetRow`).
- `src/Ikiastrro.Core/Calculators/LagnaFunctionalNature.cs` — **new.** Pure Parashari functional-nature heuristic + yogakaraka detection.
- `src/Ikiastrro.Core/LifeArea/LifeAreaMap.cs` — **new.** `enum LifeArea` + static per-area spec (houses / karakas / vargas). Consumed by Plan 2 only, delivered here so Plan 2 binds to a real type.

**Data**
- `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs`, `ChartHouseLordsRepository.cs`, `ChartConjunctionsRepository.cs`, `ChartAspectsRepository.cs` — **modify:** add `GetByBirthDetailId(int)`.
- `src/Ikiastrro.Data/PlanetSignTransitEventsRepository.cs` — **modify:** add `GetCurrentSign` + `GetNextChange`.
- `src/Ikiastrro.Data/SadeSatiRepository.cs` — **new.** `tvf_Chart_SadeSatiPeriods`.
- `src/Ikiastrro.Data/HouseNakshatraSpanRepository.cs` — **new.** `vw_Chart_HouseNakshatraSpan`.
- `src/Ikiastrro.Data/PlanetsReferenceRepository.cs`, `SignAttributesRepository.cs`, `NakshatraReferenceRepository.cs` — **new.** Plain reads over `tbl_Planets` / `tbl_SignAttributes` / `tbl_Nakshatras`(+`Padas`+`SubLords`).
- `src/Ikiastrro.Data/LagnaFunctionalNatureRepository.cs` — **new.** `GetForLagna(byte)` over `tbl_Dim_LagnaFunctionalNature`.
- `src/Ikiastrro.Data/ChartGenerationService.cs` — **new.** `GenerateAll` / `GenerateMissing` / `RecomputeAnalytics`.

**Core Models (new records — one file each under `src/Ikiastrro.Core/Models/`)**
- `SadeSatiPeriod.cs`, `HouseNakshatraSpanRow.cs`, `PlanetTransitSnapshot.cs`, `PlanetReference.cs`, `SignAttributeReference.cs`, `NakshatraReference.cs`, `NakshatraPadaReference.cs`, `NakshatraSubLordReference.cs`, `LagnaFunctionalNatureRow.cs`.

**CLI**
- `src/Ikiastrro.Cli/Program.cs` — **modify:** add `verify-functional-nature` mode; construct one `ChartGenerationService`; route `backfill-analytics`, `backfill-charts`, `recompute-keydetails`, the main add flow through it; add `compute-all <name>` mode.

**Web**
- `src/Ikiastrro.Web/Program.cs` — **modify:** register `ChartGenerationService` + its dependencies in DI.
- `src/Ikiastrro.Web/Components/Pages/Add.razor` — **modify:** `HandleSubmit` calls `ChartGenerationService.GenerateAll` instead of the inline pipeline.
- `src/Ikiastrro.Web/Components/Charts/ChartView.razor` — **modify:** update `D1ChartViewModel` / `D1PlanetRow` references to the new names (Task 1).

**DB**
- `db/031_create_lagna_functional_nature.sql` — **new.**

---

## Task 1: Rename `D1ChartViewModel` → `ChartViewModel`, `D1PlanetRow` → `PlanetRow`

**Files:**
- Rename: `src/Ikiastrro.Core/Calculators/D1ChartViewModel.cs` → `src/Ikiastrro.Core/Calculators/ChartViewModel.cs`
- Modify: that file's contents (class + record + any internal self-references)
- Modify: `src/Ikiastrro.Web/Components/Charts/ChartView.razor` (all `D1ChartViewModel.` and `D1PlanetRow` references)
- Modify: any other referencing file surfaced by the grep in Step 1

**Interfaces:**
- Produces: `Ikiastrro.Core.Calculators.ChartViewModel` (was `D1ChartViewModel`) with the same static members — `BuildPlanetRows`, `BuildAspectedByGlyphs`, `DignityToken`, `PlanetGlyph`, `DirectionLabel`, `CombustionLabel` (names unchanged, only the containing type renamed); `Ikiastrro.Core.Calculators.PlanetRow` (was `D1PlanetRow`) with the same fields.

- [ ] **Step 1: Enumerate every reference**

Run: `grep -rn "D1ChartViewModel\|D1PlanetRow" src/`
Expected: hits in `Calculators/D1ChartViewModel.cs` and `Web/Components/Charts/ChartView.razor` (and possibly `Web/Components/Charts/GridPlanetGlyph.cs` — check). Record the full list.

- [ ] **Step 2: Rename the file and its types**

```bash
git mv src/Ikiastrro.Core/Calculators/D1ChartViewModel.cs src/Ikiastrro.Core/Calculators/ChartViewModel.cs 2>/dev/null \
  || mv src/Ikiastrro.Core/Calculators/D1ChartViewModel.cs src/Ikiastrro.Core/Calculators/ChartViewModel.cs
```

In `ChartViewModel.cs`: replace `class D1ChartViewModel` → `class ChartViewModel`; `record D1PlanetRow` → `record PlanetRow` (and its constructor name if positional); every in-file `D1PlanetRow` → `PlanetRow`; every in-file `D1ChartViewModel.` → `ChartViewModel.`. Update the XML doc-comment summary if it names "D1" as a limitation ("the D1 chart's view model" → "a chart's view model — chart-type-agnostic").

- [ ] **Step 3: Update the Web references**

In `src/Ikiastrro.Web/Components/Charts/ChartView.razor`, replace every `D1ChartViewModel.` → `ChartViewModel.` and every `D1PlanetRow` → `PlanetRow`. Apply the same to any other file from Step 1.

- [ ] **Step 4: Build the whole solution**

Run: `dotnet build`
Expected: PASS, 0 errors. (This is the only verification — the rename is behaviour-preserving and the compiler proves completeness.)

- [ ] **Step 5: Checkpoint** — Task 1 complete. Not under git; if versioning: `git add -A && git commit -m "refactor(core): rename D1ChartViewModel/D1PlanetRow to chart-type-agnostic names"`.

---

## Task 2: `LagnaFunctionalNature` (Core) + `verify-functional-nature` CLI mode

**Files:**
- Create: `src/Ikiastrro.Core/Calculators/LagnaFunctionalNature.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (new `verify-functional-nature` mode, next to the existing `verify-vargas` block — search for `args[0] == "verify-vargas"`)

**Interfaces:**
- Consumes: `ClassicalDignity.GetSignLord(ZodiacName) : string` (returns a plain planet name like `"Saturn"`); `AstroMath.CountFromSignToSign(ZodiacName from, ZodiacName to) : int` (1-based, same sign = 1); `ZodiacName`, `PlanetName` enums.
- Produces:
  - `enum Ikiastrro.Core.Calculators.FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }`
  - `record Ikiastrro.Core.Calculators.FunctionalNatureResult(FunctionalNature Nature, int[] RuledHouses, bool IsMaraka, bool KendradhipatiDosha, string Rationale)`
  - `static FunctionalNatureResult LagnaFunctionalNature.For(ZodiacName lagnaSign, PlanetName planet)` — defined for the 7 classical planets (`Sun`…`Saturn`); throws `ArgumentOutOfRangeException` for `Rahu`/`Ketu`.
  - CLI: `dotnet run --project src/Ikiastrro.Cli -- verify-functional-nature` → PASS/FAIL per case, exit `1` if any FAIL.

- [ ] **Step 1: Add the `verify-functional-nature` mode (will not compile yet)**

In `src/Ikiastrro.Cli/Program.cs`, immediately after the closing brace of the `if (args.Length > 0 && args[0] == "verify-vargas") { … }` block, insert:

```csharp
// --- One-off check: `dotnet run -- verify-functional-nature` ---
// Worked-example assertions for LagnaFunctionalNature (Parashari functional benefic/malefic
// heuristic + yogakaraka detection). Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-functional-nature")
{
    var failures = 0;
    void Check(string label, object actual, object expected)
    {
        var ok = actual.ToString() == expected.ToString();
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }
    string Houses(FunctionalNatureResult r) => "{" + string.Join(",", r.RuledHouses) + "}";

    var taSa = LagnaFunctionalNature.For(ZodiacName.Taurus, PlanetName.Saturn);
    Check("Taurus/Saturn nature", taSa.Nature, FunctionalNature.Yogakaraka);
    Check("Taurus/Saturn houses", Houses(taSa), "{9,10}");

    var arMe = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Mercury);
    Check("Aries/Mercury nature", arMe.Nature, FunctionalNature.Malefic);
    Check("Aries/Mercury houses", Houses(arMe), "{3,6}");

    var cnMo = LagnaFunctionalNature.For(ZodiacName.Cancer, PlanetName.Moon);
    Check("Cancer/Moon nature", cnMo.Nature, FunctionalNature.Neutral);   // Moon as Lagna lord
    Check("Cancer/Moon houses", Houses(cnMo), "{1}");

    Check("Libra/Saturn",     LagnaFunctionalNature.For(ZodiacName.Libra, PlanetName.Saturn).Nature,      FunctionalNature.Yogakaraka);
    Check("Cancer/Mars",      LagnaFunctionalNature.For(ZodiacName.Cancer, PlanetName.Mars).Nature,       FunctionalNature.Yogakaraka);
    Check("Leo/Mars",         LagnaFunctionalNature.For(ZodiacName.Leo, PlanetName.Mars).Nature,          FunctionalNature.Yogakaraka);
    Check("Capricornus/Venus",LagnaFunctionalNature.For(ZodiacName.Capricornus, PlanetName.Venus).Nature, FunctionalNature.Yogakaraka);
    Check("Aquarius/Venus",   LagnaFunctionalNature.For(ZodiacName.Aquarius, PlanetName.Venus).Nature,    FunctionalNature.Yogakaraka);

    var arJu = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Jupiter);
    Check("Aries/Jupiter nature", arJu.Nature, FunctionalNature.Benefic);   // rules 9th (trikona) + 12th
    Check("Aries/Jupiter houses", Houses(arJu), "{9,12}");

    var arMo = LagnaFunctionalNature.For(ZodiacName.Aries, PlanetName.Moon);
    Check("Aries/Moon nature", arMo.Nature, FunctionalNature.Malefic);      // natural benefic owning only the 4th
    Check("Aries/Moon kendradhipati", arMo.KendradhipatiDosha, true);

    var liVe = LagnaFunctionalNature.For(ZodiacName.Libra, PlanetName.Venus);
    Check("Libra/Venus maraka", liVe.IsMaraka, true);                       // rules 2nd + 7th

    Console.WriteLine(failures == 0 ? "\nverify-functional-nature: ALL PASS" : $"\nverify-functional-nature: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

Add `using Ikiastrro.Core.Calculators;` at the top of `Program.cs` if not already present.

- [ ] **Step 2: Build — expect failure**

Run: `dotnet build src/Ikiastrro.Cli`
Expected: FAIL — `CS0246`/`CS0103` for `LagnaFunctionalNature`, `FunctionalNature`, `FunctionalNatureResult`.

- [ ] **Step 3: Implement `LagnaFunctionalNature.cs`**

```csharp
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Core.Calculators;

/// <summary>Parashari functional nature of a planet with respect to a Lagna — Benefic / Malefic /
/// Neutral / Yogakaraka — derived from which houses the planet rules from that Lagna.
/// A documented heuristic (B.V. Raman, "How to Judge a Horoscope" Vol. 1, p.16-18, "Benefics and
/// Malefics for each Lagna"; general rules p.14-15). It will diverge from Raman's explicit per-Lagna
/// verdict for mixed-lordship planets — the seeded table tbl_Dim_LagnaFunctionalNature (migration
/// 031) carries that verdict and is authoritative for display; this class is the computable
/// baseline. Rahu/Ketu are out of scope (no sign rulership).</summary>
public static class LagnaFunctionalNature
{
    private static readonly HashSet<PlanetName> NaturalBenefics = new()
        { PlanetName.Jupiter, PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon };

    private static readonly int[] Kendras  = { 4, 7, 10 };   // the 1st is a kendra too but never on its own confers yogakaraka
    private static readonly int[] Trikonas = { 5, 9 };       // the 1st is a trikona too, same caveat
    private static readonly int[] Dusthanas3611 = { 3, 6, 11 };

    public static FunctionalNatureResult For(ZodiacName lagnaSign, PlanetName planet)
    {
        if (planet is PlanetName.Rahu or PlanetName.Ketu)
            throw new ArgumentOutOfRangeException(nameof(planet), "Functional nature is defined for the 7 classical planets only.");

        var ruledHouses = Enumerable.Range(0, 12)
            .Select(i => (ZodiacName)i)
            .Where(sign => ClassicalDignity.GetSignLord(sign) == planet.ToString())
            .Select(sign => AstroMath.CountFromSignToSign(lagnaSign, sign))
            .OrderBy(h => h)
            .ToArray();

        var isMaraka = ruledHouses.Contains(2) || ruledHouses.Contains(7);
        var hasKendra  = ruledHouses.Intersect(Kendras).Any();
        var hasTrikona = ruledHouses.Intersect(Trikonas).Any();
        var hasBad     = ruledHouses.Intersect(Dusthanas3611).Any();
        var isNaturalBenefic = NaturalBenefics.Contains(planet);

        FunctionalNature nature;
        var kendradhipatiDosha = false;
        string why;

        if (hasKendra && hasTrikona)
        {
            nature = FunctionalNature.Yogakaraka;
            why = $"Rules a kendra and a trikona ({string.Join(" & ", ruledHouses)}) — Yogakaraka";
        }
        else if (ruledHouses.Contains(1) && !hasBad)
        {
            nature = planet == PlanetName.Moon ? FunctionalNature.Neutral : FunctionalNature.Benefic;
            why = planet == PlanetName.Moon ? "Lagna lord and the Moon — Neutral" : "Lagna lord — Benefic";
        }
        else if ((ruledHouses.Contains(5) || ruledHouses.Contains(9)) && !hasBad)
        {
            nature = FunctionalNature.Benefic;
            why = $"Trikona lord ({string.Join(" & ", ruledHouses)}) — Benefic";
        }
        else if (!hasBad && ruledHouses.All(h => Kendras.Contains(h)) && ruledHouses.Length > 0)
        {
            if (isNaturalBenefic) { nature = FunctionalNature.Malefic; kendradhipatiDosha = true;
                why = $"Natural benefic owning only a kendra ({string.Join(" & ", ruledHouses)}) — kendradhipati dosha"; }
            else { nature = FunctionalNature.Benefic;
                why = $"Natural malefic owning a kendra ({string.Join(" & ", ruledHouses)}) — Benefic"; }
        }
        else if (hasBad)
        {
            nature = FunctionalNature.Malefic;
            why = $"Lord of {string.Join(" & ", ruledHouses.Intersect(Dusthanas3611))} — malefic house lordship";
        }
        else if (ruledHouses.Length > 0 && ruledHouses.All(h => h is 2 or 8 or 12))
        {
            nature = planet is PlanetName.Sun or PlanetName.Moon ? FunctionalNature.Neutral : FunctionalNature.Malefic;
            why = $"Lord of {string.Join(" & ", ruledHouses)}" + (nature == FunctionalNature.Neutral ? " — luminary, Neutral" : " — Malefic");
        }
        else
        {
            nature = FunctionalNature.Malefic;   // default catch-all: mixed kendra + maraka/dusthana, no trikona
            why = $"Mixed lordship ({string.Join(" & ", ruledHouses)}) — Malefic (heuristic default; see Raman table)";
        }

        return new FunctionalNatureResult(nature, ruledHouses, isMaraka, kendradhipatiDosha, why);
    }
}

/// <summary>Functional (Lagna-relative) nature classes. Distinct from natural benefic/malefic.</summary>
public enum FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }

/// <param name="RuledHouses">1-based house numbers this planet rules from the given Lagna (1 or 2 entries).</param>
/// <param name="IsMaraka">Additionally lord of the 2nd or 7th (independent of Nature).</param>
/// <param name="KendradhipatiDosha">Natural benefic degraded by owning only an angle.</param>
public record FunctionalNatureResult(
    FunctionalNature Nature, int[] RuledHouses, bool IsMaraka, bool KendradhipatiDosha, string Rationale);
```

- [ ] **Step 4: Build + run — expect all PASS**

Run: `dotnet build src/Ikiastrro.Cli && dotnet run --project src/Ikiastrro.Cli -- verify-functional-nature`
Expected: every line `[PASS]`, final line `verify-functional-nature: ALL PASS`, exit 0.
If `Aries/Jupiter houses` fails: check `AstroMath.CountFromSignToSign(Aries, Sagittarius)` = 9 and `(Aries, Pisces)` = 12 — the expected `{9,12}` assumes Jupiter rules Sagittarius + Pisces (it does). If `Leo/Mars` fails as `Benefic` not `Yogakaraka`: Mars rules Aries (9th from Leo) + Scorpio (4th from Leo) → `{4,9}` → hasKendra ∧ hasTrikona → Yogakaraka. Confirm `GetSignLord` returns `"Mars"` exactly (case-sensitive `planet.ToString()`).

- [ ] **Step 5: Checkpoint** — Task 2 complete.

---

## Task 2b: `LifeAreaMap` (Core, static data)

**Files:**
- Create: `src/Ikiastrro.Core/LifeArea/LifeAreaMap.cs`

**Interfaces:**
- Produces:
  - `enum Ikiastrro.Core.LifeArea.LifeArea { PersonalityHealth, Relationships, Career, Money }`
  - `record Ikiastrro.Core.LifeArea.LifeAreaSpec(int[] Houses, string[] Karakas, string[] Vargas, string DefaultVarga)` — `Karakas` are plain planet-name strings (`"Sun"`), matching `ChartKeyDetail.Planet`; `Vargas`/`DefaultVarga` are chart-type strings (`"D6"`).
  - `static IReadOnlyDictionary<LifeArea, LifeAreaSpec> LifeAreaMap.Specs`
- Consumes: nothing. Pure reference data (B.V. Raman house significations, `how-to-judge-a-horoscope-1.md` p.12-13; same basis as the reserved migration 030). No Phase-0 caller — delivered here so Plan 2's `LifeAreaTab` binds to a real type.

- [ ] **Step 1: Create `LifeAreaMap.cs`**

```csharp
namespace Ikiastrro.Core.LifeArea;

/// <summary>The four life-area groupings the Web workspace is organised around.</summary>
public enum LifeArea { PersonalityHealth, Relationships, Career, Money }

/// <param name="Houses">D1 houses classically ruling this area (1-based).</param>
/// <param name="Karakas">Sthira karaka planets for this area — plain names, matching ChartKeyDetail.Planet.</param>
/// <param name="Vargas">Divisional charts shown on this tab; DefaultVarga is the right-slot default.</param>
public record LifeAreaSpec(int[] Houses, string[] Karakas, string[] Vargas, string DefaultVarga);

/// <summary>
/// Static map of life area → its classical houses / karakas / vargas. Sourced from B.V. Raman,
/// "How to Judge a Horoscope" Vol. 1, p.12-13 (house significations) — the same source basis as the
/// reserved migration 030 (tbl_Dim_HouseSignification). TODO: switch to reading tbl_Dim_* once
/// migration 030 ships and the "engine reads reference tables" decision is made.
/// </summary>
public static class LifeAreaMap
{
    public static readonly IReadOnlyDictionary<LifeArea, LifeAreaSpec> Specs = new Dictionary<LifeArea, LifeAreaSpec>
    {
        [LifeArea.PersonalityHealth] = new(
            Houses: new[] { 1, 6, 8, 3 },
            Karakas: new[] { "Sun", "Moon", "Saturn" },
            Vargas: new[] { "D1", "D6" }, DefaultVarga: "D6"),
        [LifeArea.Relationships] = new(
            Houses: new[] { 7, 5, 2, 11, 4 },
            Karakas: new[] { "Venus", "Jupiter", "Moon", "Mercury" },
            Vargas: new[] { "D9" }, DefaultVarga: "D9"),
        [LifeArea.Career] = new(
            Houses: new[] { 10, 6, 7, 2, 11, 1 },
            Karakas: new[] { "Sun", "Saturn", "Mercury", "Jupiter" },
            Vargas: new[] { "D10" }, DefaultVarga: "D10"),
        [LifeArea.Money] = new(
            Houses: new[] { 2, 11, 9, 5, 12 },
            Karakas: new[] { "Jupiter", "Venus", "Mercury" },
            Vargas: new[] { "D2", "D11" }, DefaultVarga: "D2"),
    };
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/Ikiastrro.Core`
Expected: PASS. (Pure data — no behaviour to assert beyond compilation; `LifeAreaTab` exercises it in Plan 2.)

- [ ] **Step 3: Checkpoint** — Task 2b complete.

---

## Task 3: `GetByBirthDetailId` batch reads on the 4 chart analytics repos

**Files:**
- Modify: `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs`
- Modify: `src/Ikiastrro.Data/ChartHouseLordsRepository.cs`
- Modify: `src/Ikiastrro.Data/ChartConjunctionsRepository.cs`
- Modify: `src/Ikiastrro.Data/ChartAspectsRepository.cs`

**Interfaces:**
- Produces on each repo: `IReadOnlyList<T> GetByBirthDetailId(int birthDetailId)` returning every row for that person across **all** chart types, ordered by `Id` — where `T` is `ChartKeyDetail` / `ChartHouseLord` / `ChartConjunction` / `ChartAspect` respectively.
- Consumes: nothing new (mirrors each repo's existing `GetByChartResultId` + `DeleteByBirthDetailId`).

- [ ] **Step 1: Add the method to `ChartKeyDetailsRepository`**

After `GetByChartResultId`, add:

```csharp
    /// <summary>Every KeyDetails row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<ChartKeyDetail> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_KeyDetails WHERE BirthDetailId = @BirthDetailId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartKeyDetail>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }
```

- [ ] **Step 2: Repeat for the other three repos**

Same method, changing type + table:
- `ChartHouseLordsRepository` → `ChartHouseLord`, `dbo.tbl_Chart_HouseLords`
- `ChartConjunctionsRepository` → `ChartConjunction`, `dbo.tbl_Chart_Conjunctions`
- `ChartAspectsRepository` → `ChartAspect`, `dbo.tbl_Chart_Aspects`

(Copy the full method into each — do not abbreviate.)

- [ ] **Step 3: Build**

Run: `dotnet build src/Ikiastrro.Data`
Expected: PASS.

- [ ] **Step 4: Verify against the per-chart-result reads (SQL parity)**

Run:

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
DECLARE @b INT = (SELECT Id FROM tbl_BirthDetails WHERE Name = 'Ramakrishnan');
SELECT 'KeyDetails' t, COUNT(*) via_person FROM tbl_Chart_KeyDetails WHERE BirthDetailId=@b
UNION ALL SELECT 'KeyDetails-viaCR', COUNT(*) FROM tbl_Chart_KeyDetails kd
  JOIN tbl_ChartResults cr ON cr.Id=kd.ChartResultId WHERE cr.BirthDetailId=@b;"
```

Expected: the two counts are **equal** (the `GetByBirthDetailId` filter and the join-through-ChartResults return the same set). Repeat the pattern for `tbl_Chart_HouseLords` / `_Conjunctions` / `_Aspects` if desired.

- [ ] **Step 5: Checkpoint** — Task 3 complete.

---

## Task 4: New read repos — Sade Sati, transit snapshot, house-nakshatra span

**Files:**
- Create: `src/Ikiastrro.Core/Models/SadeSatiPeriod.cs`
- Create: `src/Ikiastrro.Core/Models/HouseNakshatraSpanRow.cs`
- Create: `src/Ikiastrro.Core/Models/PlanetTransitSnapshot.cs`
- Create: `src/Ikiastrro.Data/SadeSatiRepository.cs`
- Create: `src/Ikiastrro.Data/HouseNakshatraSpanRepository.cs`
- Modify: `src/Ikiastrro.Data/PlanetSignTransitEventsRepository.cs`

**Interfaces:**
- Consumes: `tvf_Chart_SadeSatiPeriods(@BirthDetailId)` — columns `PeriodType VARCHAR`, `SortOrder INT`, `StartDateTimeUtc DATETIME2 NULL`, `EndDateTimeUtc DATETIME2 NULL`, `SaturnSign VARCHAR`. `vw_Chart_HouseNakshatraSpan` — columns `ChartResultId, BirthDetailId, ChartType, HouseNumber, HouseSign, HouseSignId, LordPlanet, NakshatraId, NakshatraName, PadaNumber, PadaStartDegree, PadaEndDegree, NakshatraLordName, NavamsaSignName`. `tvf_PlanetSignAtDate(@PlanetId, @AsOfDateUtc)` — columns `SignId TINYINT, EventDateTimeUtc DATETIME2, MotionDirection VARCHAR`; handles `@PlanetId = 9` (Ketu) internally.
- Produces:
  - `record SadeSatiPeriod(string PeriodType, int SortOrder, DateTime? StartDateTimeUtc, DateTime? EndDateTimeUtc, string SaturnSign)`
  - `record HouseNakshatraSpanRow(int ChartResultId, int BirthDetailId, string ChartType, int HouseNumber, string HouseSign, byte HouseSignId, string LordPlanet, byte NakshatraId, string NakshatraName, byte PadaNumber, decimal PadaStartDegree, decimal PadaEndDegree, string NakshatraLordName, string NavamsaSignName)`
  - `record PlanetTransitSnapshot(PlanetName Planet, byte SignId, DateTime InSignSinceUtc, string MotionDirection, DateTime? NextChangeUtc)`
  - `SadeSatiRepository.GetByBirthDetailId(int) : IReadOnlyList<SadeSatiPeriod>`
  - `HouseNakshatraSpanRepository.GetByChartResultId(int) : IReadOnlyList<HouseNakshatraSpanRow>`
  - `PlanetSignTransitEventsRepository.GetSnapshot(PlanetName planet, DateTime asOfUtc) : PlanetTransitSnapshot?` (null when no crossing recorded on/before `asOfUtc`)

- [ ] **Step 1: Create the three model records**

`src/Ikiastrro.Core/Models/SadeSatiPeriod.cs`:

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>One row of tvf_Chart_SadeSatiPeriods — a Saturn-from-natal-Moon affliction window.
/// PeriodType ∈ SadeSati_Dhaiya1_Rising / SadeSati_Dhaiya2_Peak / SadeSati_Dhaiya3_Setting /
/// KantakaShani / AshtamaShani. EndDateTimeUtc null = ongoing / past the 2060 backfill boundary.</summary>
public record SadeSatiPeriod(
    string PeriodType, int SortOrder, DateTime? StartDateTimeUtc, DateTime? EndDateTimeUtc, string SaturnSign);
```

`src/Ikiastrro.Core/Models/HouseNakshatraSpanRow.cs`:

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>One row of vw_Chart_HouseNakshatraSpan — a (house → sign → nakshatra-pada) slice.
/// Whole-sign, so each house has exactly 9 pada rows (2.25 nakshatras).</summary>
public record HouseNakshatraSpanRow(
    int ChartResultId, int BirthDetailId, string ChartType, int HouseNumber, string HouseSign,
    byte HouseSignId, string LordPlanet, byte NakshatraId, string NakshatraName, byte PadaNumber,
    decimal PadaStartDegree, decimal PadaEndDegree, string NakshatraLordName, string NavamsaSignName);
```

`src/Ikiastrro.Core/Models/PlanetTransitSnapshot.cs`:

```csharp
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Core.Models;

/// <summary>Where a slow planet (Saturn/Jupiter/Rahu/Ketu) sits sidereally as of a date, plus when it
/// last entered that sign and when it next leaves — assembled from tbl_PlanetSignTransitEvents.</summary>
public record PlanetTransitSnapshot(
    PlanetName Planet, byte SignId, DateTime InSignSinceUtc, string MotionDirection, DateTime? NextChangeUtc);
```

- [ ] **Step 2: Create `SadeSatiRepository`**

```csharp
using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tvf_Chart_SadeSatiPeriods (migration 023) — read-only.</summary>
public class SadeSatiRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public SadeSatiRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<SadeSatiPeriod> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT PeriodType, SortOrder, StartDateTimeUtc, EndDateTimeUtc, SaturnSign " +
                           "FROM dbo.tvf_Chart_SadeSatiPeriods(@BirthDetailId) ORDER BY SortOrder, StartDateTimeUtc";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<SadeSatiPeriod>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }
}
```

- [ ] **Step 3: Create `HouseNakshatraSpanRepository`**

```csharp
using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>vw_Chart_HouseNakshatraSpan (migration 034) — read-only.</summary>
public class HouseNakshatraSpanRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public HouseNakshatraSpanRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<HouseNakshatraSpanRow> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.vw_Chart_HouseNakshatraSpan WHERE ChartResultId = @ChartResultId " +
                           "ORDER BY HouseNumber, PadaStartDegree";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<HouseNakshatraSpanRow>(sql, new { ChartResultId = chartResultId }).ToList();
    }
}
```

- [ ] **Step 4: Add `GetSnapshot` to `PlanetSignTransitEventsRepository`**

After `CountByPlanet`, add:

```csharp
    /// <summary>Sidereal sign of a slow planet as of a date, with the entry date and the next
    /// crossing after it. Ketu (PlanetName.Ketu → PlanetId 9) is resolved inside tvf_PlanetSignAtDate
    /// from Rahu's events; the "next change" query applies the same 8↔9 remap. Returns null if no
    /// crossing was recorded on or before <paramref name="asOfUtc"/> (person predates 1930, or the
    /// transit table is not backfilled).</summary>
    public PlanetTransitSnapshot? GetSnapshot(PlanetName planet, DateTime asOfUtc)
    {
        var planetId = (int)planet + 1;                    // Sun=1 … Ketu=9
        var eventsPlanetId = planetId == 9 ? 8 : planetId; // Ketu's rows live under Rahu
        using var connection = _connectionFactory.CreateOpenConnection();

        var current = connection.QuerySingleOrDefault<(byte SignId, DateTime EventDateTimeUtc, string MotionDirection)?>(
            "SELECT TOP (1) SignId, EventDateTimeUtc, MotionDirection FROM dbo.tvf_PlanetSignAtDate(@PlanetId, @AsOf)",
            new { PlanetId = planetId, AsOf = asOfUtc });
        if (current is null) return null;

        var next = connection.ExecuteScalar<DateTime?>(
            "SELECT MIN(EventDateTimeUtc) FROM dbo.tbl_PlanetSignTransitEvents " +
            "WHERE PlanetId = @P AND EventDateTimeUtc > @AsOf",
            new { P = eventsPlanetId, AsOf = asOfUtc });

        return new PlanetTransitSnapshot(planet, current.Value.SignId, current.Value.EventDateTimeUtc,
            current.Value.MotionDirection, next);
    }
```

Add `using Ikiastrro.Core.Models;` to the file if absent.

- [ ] **Step 5: Build**

Run: `dotnet build src/Ikiastrro.Data`
Expected: PASS. (If Dapper rejects the tuple `QuerySingleOrDefault<(...)?>`, fall back to a private nested `record CurrentRow(byte SignId, DateTime EventDateTimeUtc, string MotionDirection);` and query that.)

- [ ] **Step 6: Verify with `sqlcmd`**

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
DECLARE @b INT = (SELECT Id FROM tbl_BirthDetails WHERE Name='Ramakrishnan');
SELECT COUNT(*) AS sadesati_rows FROM tvf_Chart_SadeSatiPeriods(@b);
SELECT * FROM tvf_PlanetSignAtDate(7, SYSUTCDATETIME());   -- Saturn now
DECLARE @cr INT = (SELECT Id FROM tbl_ChartResults WHERE BirthDetailId=@b AND ChartType='D1');
SELECT COUNT(*) AS span_rows FROM vw_Chart_HouseNakshatraSpan WHERE ChartResultId=@cr;"
```

Expected: `sadesati_rows` > 0; the Saturn row returns one `SignId`; `span_rows` = **108** (12 houses × 9 padas) for a D1.

- [ ] **Step 7: Checkpoint** — Task 4 complete.

---

## Task 5: Reference-data repos (`tbl_Planets`, `tbl_SignAttributes`, `tbl_Nakshatras` + Padas + SubLords)

**Files:**
- Create: `src/Ikiastrro.Core/Models/PlanetReference.cs`, `SignAttributeReference.cs`, `NakshatraReference.cs`, `NakshatraPadaReference.cs`, `NakshatraSubLordReference.cs`
- Create: `src/Ikiastrro.Data/PlanetsReferenceRepository.cs`, `SignAttributesRepository.cs`, `NakshatraReferenceRepository.cs`

**Interfaces:**
- Produces: `PlanetsReferenceRepository.GetAll() : IReadOnlyList<PlanetReference>`; `SignAttributesRepository.GetAll() : IReadOnlyList<SignAttributeReference>`; `NakshatraReferenceRepository.GetAll() : IReadOnlyList<NakshatraReference>`, `.GetPadas() : IReadOnlyList<NakshatraPadaReference>`, `.GetSubLords() : IReadOnlyList<NakshatraSubLordReference>`.
- Consumes: tables from migrations 019 / 021 / 022 / 033.

- [ ] **Step 1: Get the authoritative column lists**

Run:

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
SELECT 'tbl_Planets', name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.tbl_Planets')
UNION ALL SELECT 'tbl_SignAttributes', name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.tbl_SignAttributes')
UNION ALL SELECT 'tbl_Nakshatras', name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.tbl_Nakshatras')
UNION ALL SELECT 'tbl_NakshatraPadas', name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.tbl_NakshatraPadas')
UNION ALL SELECT 'tbl_NakshatraSubLords', name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.tbl_NakshatraSubLords')
ORDER BY 1, 2;"
```

Use the result as the source of truth for the record properties below (the DDL in the migrations is the expected shape; confirm nothing drifted).

- [ ] **Step 2: Create the model records**

Match property names to columns exactly (Dapper maps by name). Expected shapes (confirm against Step 1):

```csharp
// PlanetReference.cs
namespace Ikiastrro.Core.Models;
public record PlanetReference(
    byte Id, string PlanetName, string PlanetNameSanskrit, string NaturalNature,
    string? ConditionalRule, bool RulesSign, byte VimshottariYears, byte VimshottariSequenceOrder);

// SignAttributeReference.cs
namespace Ikiastrro.Core.Models;
public record SignAttributeReference(
    byte Id, string SignName, string SignNameSanskrit, string ZodiacEnumValue, byte RulingPlanetId,
    string Gender, string Direction, string? RisingType, string SymbolAnimalType, string SymbolDescription,
    string KalapurushaBodyPart, byte? ExaltedPlanetId, decimal? ExaltedDegree, byte? DebilitatedPlanetId,
    decimal? DebilitatedDegree, byte? MooltrikonaPlanetId, decimal? MooltrikonaRangeStart, decimal? MooltrikonaRangeEnd);
// NOTE: tbl_SignAttributes also carries RulingPlanetNature, type_house_element, type_house_keyattri
// (snake_case). Add matching properties [Column("type_house_element")]-style OR select-alias them in the
// repo SQL (`type_house_element AS Element`) and add plain props. Confirm from Step 1 and pick one.

// NakshatraReference.cs
namespace Ikiastrro.Core.Models;
public record NakshatraReference(
    byte Id, string NakshatraName, decimal StartDegree, decimal EndDegree, byte RulingPlanetId,
    byte SequenceNumber, string? RulingDeity, string? Symbol, string? Guna, string? Gana,
    string? YoniAnimal, string? YoniGender, string? Nadi, string? Varna, string? Tatva, string? Direction,
    byte? PrimaryRasiId, bool StraddlesSignBoundary);

// NakshatraPadaReference.cs
namespace Ikiastrro.Core.Models;
public record NakshatraPadaReference(
    int Id, byte NakshatraId, byte PadaNumber, decimal StartDegree, decimal EndDegree, byte RasiId, byte NavamsaSignId);

// NakshatraSubLordReference.cs
namespace Ikiastrro.Core.Models;
public record NakshatraSubLordReference(
    int Id, byte NakshatraId, byte SubSequenceNumber, byte SubLordId, decimal StartDegree, decimal EndDegree);
```

If `type_house_element` / `type_house_keyattri` / `RulingPlanetNature` exist (they should — migration 019), alias them in SQL: `SELECT ..., type_house_element AS HouseElement, type_house_keyattri AS HouseModality, RulingPlanetNature ...` and add `string? HouseElement, string? HouseModality, string RulingPlanetNature` to `SignAttributeReference`.

- [ ] **Step 3: Create the three repos**

```csharp
// PlanetsReferenceRepository.cs
using Dapper;
using Ikiastrro.Core.Models;
namespace Ikiastrro.Data;
public class PlanetsReferenceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public PlanetsReferenceRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
    public IReadOnlyList<PlanetReference> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetReference>("SELECT * FROM dbo.tbl_Planets ORDER BY Id").ToList();
    }
}
```

```csharp
// SignAttributesRepository.cs — same shape; SELECT with the snake_case aliases from Step 2 if needed.
using Dapper;
using Ikiastrro.Core.Models;
namespace Ikiastrro.Data;
public class SignAttributesRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public SignAttributesRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
    public IReadOnlyList<SignAttributeReference> GetAll()
    {
        const string sql = "SELECT *, type_house_element AS HouseElement, type_house_keyattri AS HouseModality " +
                           "FROM dbo.tbl_SignAttributes ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<SignAttributeReference>(sql).ToList();
    }
}
```

```csharp
// NakshatraReferenceRepository.cs
using Dapper;
using Ikiastrro.Core.Models;
namespace Ikiastrro.Data;
public class NakshatraReferenceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public NakshatraReferenceRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
    public IReadOnlyList<NakshatraReference> GetAll()
    {
        using var c = _connectionFactory.CreateOpenConnection();
        return c.Query<NakshatraReference>("SELECT * FROM dbo.tbl_Nakshatras ORDER BY Id").ToList();
    }
    public IReadOnlyList<NakshatraPadaReference> GetPadas()
    {
        using var c = _connectionFactory.CreateOpenConnection();
        return c.Query<NakshatraPadaReference>("SELECT * FROM dbo.tbl_NakshatraPadas ORDER BY NakshatraId, PadaNumber").ToList();
    }
    public IReadOnlyList<NakshatraSubLordReference> GetSubLords()
    {
        using var c = _connectionFactory.CreateOpenConnection();
        return c.Query<NakshatraSubLordReference>("SELECT * FROM dbo.tbl_NakshatraSubLords ORDER BY NakshatraId, SubSequenceNumber").ToList();
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build src/Ikiastrro.Data`
Expected: PASS.

- [ ] **Step 5: Verify row counts**

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
SELECT (SELECT COUNT(*) FROM tbl_Planets), (SELECT COUNT(*) FROM tbl_SignAttributes),
       (SELECT COUNT(*) FROM tbl_Nakshatras), (SELECT COUNT(*) FROM tbl_NakshatraPadas),
       (SELECT COUNT(*) FROM tbl_NakshatraSubLords);"
```

Expected: `9  12  27  108  243`.

- [ ] **Step 6: Checkpoint** — Task 5 complete.

---

## Task 6: `ChartGenerationService` (Data)

**Files:**
- Create: `src/Ikiastrro.Data/ChartGenerationService.cs`
- Create: `src/Ikiastrro.Core/Models/GenerationReport.cs`

**Interfaces:**
- Consumes: `ChartCalculationOrchestrator` — `CalculateAll(BirthDetails) : IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)>`, `ComputeAnalysisInput(string chartType, BirthDetails) : ChartAnalysisInput`, `Calculators : IReadOnlyList<IChartCalculator>` (each has `.ChartType`). `ChartAnalyzer.Compute(ChartAnalysisInput) : (List<ChartKeyDetail> KeyDetails, List<ChartHouseLord> HouseLords, List<ChartConjunction> Conjunctions, List<ChartAspect> Aspects)`. `VimshottariDashaService.ComputeAndStore(BirthDetails) : (ChartResult Result, List<DashaPeriod> Tree)` — self-manages its own delete + insert. `ChartResultsRepository` — `InsertAll`, `GetByBirthDetailId`, `DeleteByBirthDetailIdAndChartType`. The 4 `tbl_Chart_*` repos — `InsertAll`, `DeleteByBirthDetailId`, `DeleteByChartResultId`.
- Produces:
  - `record Ikiastrro.Core.Models.GenerationReport(IReadOnlyList<string> ChartTypesWritten, bool DashaWritten, IReadOnlyList<string> Skipped)`
  - `ChartGenerationService.GenerateAll(BirthDetails) : GenerationReport`
  - `ChartGenerationService.GenerateMissing(BirthDetails) : GenerationReport`
  - `ChartGenerationService.RecomputeAnalytics(BirthDetails, string? chartTypeFilter) : GenerationReport`

- [ ] **Step 1: Create `GenerationReport.cs`**

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>Outcome of a ChartGenerationService run — what was (re)written, whether Dasha ran, what was left alone.</summary>
public record GenerationReport(IReadOnlyList<string> ChartTypesWritten, bool DashaWritten, IReadOnlyList<string> Skipped);
```

- [ ] **Step 2: Create `ChartGenerationService.cs`**

```csharp
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>
/// The one place "compute and store every chart type + Vimshottari Dasha for a persisted BirthDetails"
/// lives. Replaces the pipeline previously copy-pasted in Add.razor and five spots of the CLI.
///
/// Boundaries: the caller has already inserted the BirthDetails row (it has an Id) and resolved
/// place/lat-long. Idempotent per person via delete-first-then-regenerate — a partial failure is
/// fully recovered by calling the same method again (there is no cross-repo DB transaction; the
/// repo layer opens a connection per call, so that would need every write method to accept an
/// injected transaction — deferred, see the plan's Global Constraints).
/// </summary>
public class ChartGenerationService
{
    private readonly ChartCalculationOrchestrator _orchestrator;
    private readonly VimshottariDashaService _dashaService;
    private readonly ChartResultsRepository _chartResultsRepo;
    private readonly ChartKeyDetailsRepository _keyDetailsRepo;
    private readonly ChartHouseLordsRepository _houseLordsRepo;
    private readonly ChartConjunctionsRepository _conjunctionsRepo;
    private readonly ChartAspectsRepository _aspectsRepo;

    public ChartGenerationService(
        ChartCalculationOrchestrator orchestrator, VimshottariDashaService dashaService,
        ChartResultsRepository chartResultsRepo, ChartKeyDetailsRepository keyDetailsRepo,
        ChartHouseLordsRepository houseLordsRepo, ChartConjunctionsRepository conjunctionsRepo,
        ChartAspectsRepository aspectsRepo)
    {
        _orchestrator = orchestrator;
        _dashaService = dashaService;
        _chartResultsRepo = chartResultsRepo;
        _keyDetailsRepo = keyDetailsRepo;
        _houseLordsRepo = houseLordsRepo;
        _conjunctionsRepo = conjunctionsRepo;
        _aspectsRepo = aspectsRepo;
    }

    /// <summary>Every registered chart type + Vimshottari Dasha, replacing whatever exists.</summary>
    public GenerationReport GenerateAll(BirthDetails birthDetails)
    {
        // Delete-first: analytics tables never hold Dasha rows, so a blanket delete is safe;
        // ChartResults are removed per chart type so the VimshottariDasha result row is left for
        // VimshottariDashaService to manage.
        _keyDetailsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _houseLordsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _conjunctionsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _aspectsRepo.DeleteByBirthDetailId(birthDetails.Id);
        foreach (var calc in _orchestrator.Calculators)
            _chartResultsRepo.DeleteByBirthDetailIdAndChartType(birthDetails.Id, calc.ChartType);

        var written = PersistCharts(birthDetails, _orchestrator.CalculateAll(birthDetails));

        _dashaService.ComputeAndStore(birthDetails);
        return new GenerationReport(written, DashaWritten: true, Skipped: Array.Empty<string>());
    }

    /// <summary>Only the chart types this person is currently missing (+ Dasha if missing).</summary>
    public GenerationReport GenerateMissing(BirthDetails birthDetails)
    {
        var existing = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id).Select(r => r.ChartType).ToHashSet();
        var toBuild = _orchestrator.Calculators.Where(c => !existing.Contains(c.ChartType)).Select(c => c.ChartType).ToList();

        var written = new List<string>();
        foreach (var chartType in toBuild)
        {
            var input = _orchestrator.ComputeAnalysisInput(chartType, birthDetails);
            var calc = _orchestrator.Calculators.First(c => c.ChartType == chartType);
            var result = calc.BuildResult(birthDetails, input);
            _chartResultsRepo.InsertAll(new[] { result });   // populates result.Id
            PersistAnalytics(birthDetails.Id, result.Id, input);
            written.Add(chartType);
        }

        var dashaWritten = false;
        if (!existing.Contains("VimshottariDasha")) { _dashaService.ComputeAndStore(birthDetails); dashaWritten = true; }

        var skipped = _orchestrator.Calculators.Select(c => c.ChartType).Where(existing.Contains).ToList();
        return new GenerationReport(written, dashaWritten, skipped);
    }

    /// <summary>Re-derive the 4 analytics tables for ChartResults that already exist (optionally one type).</summary>
    public GenerationReport RecomputeAnalytics(BirthDetails birthDetails, string? chartTypeFilter)
    {
        var results = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id)
            .Where(r => _orchestrator.Calculators.Any(c => c.ChartType == r.ChartType))
            .Where(r => chartTypeFilter is null || r.ChartType == chartTypeFilter)
            .ToList();

        var written = new List<string>();
        foreach (var result in results)
        {
            var input = _orchestrator.ComputeAnalysisInput(result.ChartType, birthDetails);
            _keyDetailsRepo.DeleteByChartResultId(result.Id);
            _houseLordsRepo.DeleteByChartResultId(result.Id);
            _conjunctionsRepo.DeleteByChartResultId(result.Id);
            _aspectsRepo.DeleteByChartResultId(result.Id);
            PersistAnalytics(birthDetails.Id, result.Id, input);
            written.Add(result.ChartType);
        }
        return new GenerationReport(written, DashaWritten: false, Skipped: Array.Empty<string>());
    }

    private List<string> PersistCharts(BirthDetails bd, IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)> computed)
    {
        _chartResultsRepo.InsertAll(computed.Select(c => c.Result));   // populates each Result.Id
        foreach (var (result, input) in computed)
            PersistAnalytics(bd.Id, result.Id, input);
        return computed.Select(c => c.Result.ChartType).ToList();
    }

    private void PersistAnalytics(int birthDetailId, int chartResultId, ChartAnalysisInput input)
    {
        var (keyDetails, houseLords, conjunctions, aspects) = ChartAnalyzer.Compute(input);
        foreach (var r in keyDetails)    { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in houseLords)    { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in conjunctions)  { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in aspects)       { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        _keyDetailsRepo.InsertAll(keyDetails);
        _houseLordsRepo.InsertAll(houseLords);
        if (conjunctions.Count > 0) _conjunctionsRepo.InsertAll(conjunctions);
        if (aspects.Count > 0) _aspectsRepo.InsertAll(aspects);
    }
}
```

> **If `DeleteByChartResultId` does not exist on `ChartHouseLordsRepository` / `ChartConjunctionsRepository` / `ChartAspectsRepository`** (only `ChartKeyDetailsRepository` was confirmed to have it), add it to each — one method, mirroring `ChartKeyDetailsRepository.DeleteByChartResultId`:
> ```csharp
> public void DeleteByChartResultId(int chartResultId)
> {
>     const string sql = "DELETE FROM dbo.tbl_Chart_HouseLords WHERE ChartResultId = @ChartResultId"; // adjust table name per repo
>     using var connection = _connectionFactory.CreateOpenConnection();
>     connection.Execute(sql, new { ChartResultId = chartResultId });
> }
> ```

- [ ] **Step 3: Build**

Run: `dotnet build src/Ikiastrro.Data`
Expected: PASS.

- [ ] **Step 4: Checkpoint** — Task 6 complete. Full verification happens in Task 7 (when a caller exercises it).

---

## Task 7: Wire `ChartGenerationService` into the CLI + Web, add `compute-all`

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs`
- Modify: `src/Ikiastrro.Web/Program.cs`
- Modify: `src/Ikiastrro.Web/Components/Pages/Add.razor`

**Interfaces:**
- Consumes: `ChartGenerationService` (Task 6).
- Produces: CLI mode `compute-all <name>`; `Add.razor` and the CLI add/backfill flows delegate to the service.

- [ ] **Step 1: CLI — construct one service near the composition root**

In `src/Ikiastrro.Cli/Program.cs`, near the bottom where the main add-flow repos are created (search for `var chartResultsRepo = new ChartResultsRepository(connectionFactory);`), add — reusing the vars already there where possible:

```csharp
var vimshottariDashaService = new VimshottariDashaService(
    new ChartResultsRepository(connectionFactory), new DashaPeriodsRepository(connectionFactory));
var chartGenerationService = new ChartGenerationService(
    orchestrator, vimshottariDashaService,
    new ChartResultsRepository(connectionFactory),
    new ChartKeyDetailsRepository(connectionFactory),
    new ChartHouseLordsRepository(connectionFactory),
    new ChartConjunctionsRepository(connectionFactory),
    new ChartAspectsRepository(connectionFactory));
```

(If `orchestrator` / `vimshottariDashaService` are already constructed above for the main flow, reuse those instead of re-newing.)

- [ ] **Step 2: CLI — replace the `backfill-analytics` body**

Find `if (args.Length > 0 && args[0] == "backfill-analytics")`. Replace the inner per-person loop that calls `ChartAnalyzer.Compute` + `InsertAll` with:

```csharp
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var report = chartGenerationService.RecomputeAnalytics(person, chartTypeFilter: null);
        Console.WriteLine($"  {person.Name}: recomputed analytics for [{string.Join(", ", report.ChartTypesWritten)}]");
    }
    return;
```

Keep the mode's opening `Console.WriteLine` banner. Delete the now-unused `...ForBackfill` repo locals in that block.

- [ ] **Step 3: CLI — replace the `backfill-charts` body**

Find `if (args.Length > 0 && args[0] == "backfill-charts")`. Replace its per-person loop with:

```csharp
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var report = chartGenerationService.GenerateMissing(person);
        Console.WriteLine(report.ChartTypesWritten.Count == 0 && !report.DashaWritten
            ? $"  {person.Name}: nothing missing"
            : $"  {person.Name}: created [{string.Join(", ", report.ChartTypesWritten)}]{(report.DashaWritten ? " + Dasha" : "")}");
    }
    return;
```

Delete the block's `...ForCharts` repo locals.

- [ ] **Step 4: CLI — replace the `recompute-keydetails` body**

Find `if (args.Length > 0 && args[0] == "recompute-keydetails")`. Replace its loop with a `RecomputeAnalytics` call (this widens it from KeyDetails-only to all 4 analytics tables — an idempotent no-op difference; note it in the mode's banner text):

```csharp
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var report = chartGenerationService.RecomputeAnalytics(person, chartTypeFilter: null);
        Console.WriteLine($"  {person.Name}: re-derived [{string.Join(", ", report.ChartTypesWritten)}]");
    }
    return;
```

- [ ] **Step 5: CLI — replace the main add-flow persist block**

Near the bottom of `Program.cs`, find the sequence `var chartResults = orchestrator.CalculateAll(birthDetails);` … through the `foreach` that calls `ChartAnalyzer.Compute` … and `vimshottariDashaService.ComputeAndStore(birthDetails);`. Replace that whole sequence with:

```csharp
var report = chartGenerationService.GenerateAll(birthDetails);
Console.WriteLine($"Stored: [{string.Join(", ", report.ChartTypesWritten)}]{(report.DashaWritten ? " + Vimshottari Dasha" : "")}");
```

Leave the subsequent "print the results" code that reads back from the repos.

- [ ] **Step 6: CLI — add the `compute-all` mode**

Next to the other one-off modes (e.g. after `compute-dasha`), add:

```csharp
// `dotnet run -- compute-all <name>` — regenerate every chart type + Dasha for one saved person
// (replaces running backfill-charts + backfill-dasha separately, e.g. after a birth-time correction).
if (args.Length > 1 && args[0] == "compute-all")
{
    var person = birthDetailsRepo.GetAll().FirstOrDefault(p =>
        string.Equals(p.Name, args[1], StringComparison.OrdinalIgnoreCase));
    if (person is null) { Console.WriteLine($"No saved person named '{args[1]}'."); return; }

    var report = chartGenerationService.GenerateAll(person);
    Console.WriteLine($"{person.Name}: regenerated [{string.Join(", ", report.ChartTypesWritten)}]"
                      + (report.DashaWritten ? " + Vimshottari Dasha" : ""));
    return;
}
```

Place it **before** the fall-through interactive add flow, alongside the other `args[0] == "..."` blocks.

- [ ] **Step 7: Web — register the service in DI**

In `src/Ikiastrro.Web/Program.cs`, wherever repos and `ChartCalculationOrchestrator` are registered, add (matching the existing lifetime — `AddScoped` if that's the pattern):

```csharp
builder.Services.AddScoped<VimshottariDashaService>();       // if not already registered
builder.Services.AddScoped<ChartGenerationService>();
```

Confirm `ChartCalculationOrchestrator`, `ChartResultsRepository`, and the 4 `tbl_Chart_*` repos are already registered (they are — `Add.razor`/`ChartDetail.razor` inject them). Register any that aren't.

- [ ] **Step 8: Web — simplify `Add.razor.HandleSubmit`**

In `src/Ikiastrro.Web/Components/Pages/Add.razor`:
- Add `@inject ChartGenerationService ChartGenerationService` near the other `@inject` lines; remove `@inject ChartCalculationOrchestrator Orchestrator` and the four `@inject Chart*Repository` lines that become unused (keep `BirthDetailsRepo`).
- Replace the block from `// --- Calculate + store every registered chart type ...` through the end of the `foreach ((result, input) in chartResults)` loop with:

```csharp
            // --- Store input, then compute + store every chart type + Vimshottari Dasha ---
            BirthDetailsRepo.Insert(birthDetails);
            ChartGenerationService.GenerateAll(birthDetails);

            Nav.NavigateTo($"/charts/{birthDetails.Id}");
```

(The existing `BirthDetailsRepo.Insert(birthDetails);` line above the old pipeline is folded in here — make sure it appears exactly once.)

- [ ] **Step 9: Build the whole solution**

Run: `dotnet build`
Expected: PASS, 0 errors. Remove any now-unused `using` lines the compiler warns about in `Add.razor` / `Program.cs`.

- [ ] **Step 10: Verify idempotency + a regenerate**

```
dotnet run --project src/Ikiastrro.Cli -- backfill-charts
```
Expected: every existing person prints `nothing missing` (no new rows — the 5 saved people already have D1/D2/D6/D9/D10/D11 + Dasha).

```
dotnet run --project src/Ikiastrro.Cli -- compute-all Ramakrishnan
```
Then:

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
DECLARE @b INT = (SELECT Id FROM tbl_BirthDetails WHERE Name='Ramakrishnan');
SELECT ChartType, COUNT(*) FROM tbl_ChartResults WHERE BirthDetailId=@b GROUP BY ChartType ORDER BY ChartType;
SELECT Planet, Sign, Nakshatra, NakshatraPada, HouseNumberFromLagna, DignityStatus
FROM tbl_Chart_KeyDetails kd JOIN tbl_ChartResults cr ON cr.Id=kd.ChartResultId
WHERE cr.BirthDetailId=@b AND cr.ChartType='D1' AND kd.Planet IN ('Moon','Sun','Rahu','Ketu') ORDER BY kd.Planet;"
```

Expected: one row each for `D1, D2, D6, D9, D10, D11, VimshottariDasha`; **Moon** = Scorpio / Anuradha / pada 2 / House 8 / Debilitated; **Sun** Aries / Exalted; **Rahu** House 4; **Ketu** House 10. (Nakshatra spelling `Anuradha`/`Pushya` — the canonical forms from the 2026-08-30 nakshatra batch.)

- [ ] **Step 11: Web smoke (manual, optional but recommended)**

Run: `dotnet run --project src/Ikiastrro.Web`, open the app, `/add` a throwaway person, submit. Expected: redirects to `/charts/{id}`, the existing D1+D9 view renders, and — new — the Dasha section is populated (previously empty for web-added people). Delete the throwaway person via the UI afterwards.

- [ ] **Step 12: Checkpoint** — Task 7 complete. The write path is now single-sourced.

---

## Task 8: Migration 031 — `tbl_Dim_LagnaFunctionalNature` + repository

**Files:**
- Create: `db/031_create_lagna_functional_nature.sql`
- Create: `src/Ikiastrro.Core/Models/LagnaFunctionalNatureRow.cs`
- Create: `src/Ikiastrro.Data/LagnaFunctionalNatureRepository.cs`

**Interfaces:**
- Produces: table `dbo.tbl_Dim_LagnaFunctionalNature` (84 rows); `record LagnaFunctionalNatureRow(byte Id, byte LagnaSignId, byte PlanetId, string? FunctionalNature, byte? Rank, string? Notes)`; `LagnaFunctionalNatureRepository.GetForLagna(byte lagnaSignId) : IReadOnlyList<LagnaFunctionalNatureRow>`.
- Consumes: `tbl_SignAttributes.Id`, `tbl_Planets.Id`.

- [ ] **Step 1: Write `db/031_create_lagna_functional_nature.sql`**

```sql
-- 031_create_lagna_functional_nature.sql
-- tbl_Dim_LagnaFunctionalNature — B.V. Raman's functional benefic/malefic classification per Lagna,
-- transcribed verbatim from "How to Judge a Horoscope" Vol. 1, p.16-18 ("Benefics and Malefics for
-- each Lagna") + the Yogakarakas section (p.9-10). 84 rows = 12 Lagnas x 7 classical planets.
-- Rahu/Ketu are not classified by this source (no sign rulership) and are not seeded.
-- Three rows the book never classifies are seeded FunctionalNature = NULL, not guessed:
--   Aries -> Moon, Gemini -> Saturn, Aquarius -> Saturn.
-- This is a cross-check reference mirror; the engine computes functional nature in
-- LagnaFunctionalNature.cs (a heuristic that will diverge from this table for mixed-lordship planets).
-- Migration 030 stays reserved (unapplied) for house significations.

IF OBJECT_ID('dbo.tbl_Dim_LagnaFunctionalNature', 'U') IS NOT NULL
    DROP TABLE dbo.tbl_Dim_LagnaFunctionalNature;
GO

CREATE TABLE dbo.tbl_Dim_LagnaFunctionalNature
(
    Id               TINYINT      NOT NULL IDENTITY(1,1) PRIMARY KEY,
    LagnaSignId      TINYINT      NOT NULL REFERENCES dbo.tbl_SignAttributes(Id),
    PlanetId         TINYINT      NOT NULL REFERENCES dbo.tbl_Planets(Id),
    FunctionalNature VARCHAR(12)  NULL CHECK (FunctionalNature IN ('Benefic','Malefic','Neutral','Yogakaraka')),
    Rank             TINYINT      NULL,   -- 1 = "best benefic" / "worst malefic" within (Lagna, nature) per the book's phrasing
    Notes            VARCHAR(120) NULL,
    CONSTRAINT UQ_LagnaFunctionalNature UNIQUE (LagnaSignId, PlanetId)
);
GO

-- PlanetId: Sun=1 Moon=2 Mars=3 Mercury=4 Jupiter=5 Venus=6 Saturn=7
-- LagnaSignId: Aries=1 Taurus=2 Gemini=3 Cancer=4 Leo=5 Virgo=6 Libra=7 Scorpio=8 Sagittarius=9 Capricorn=10 Aquarius=11 Pisces=12
INSERT INTO dbo.tbl_Dim_LagnaFunctionalNature (LagnaSignId, PlanetId, FunctionalNature, Rank, Notes) VALUES
-- Aries
(1,5,'Benefic',1,'Best benefic'),(1,3,'Benefic',2,'Next benefic'),(1,1,'Benefic',3,NULL),
(1,4,'Malefic',1,'Greatest malefic — lord of 3rd & 6th'),(1,7,'Malefic',2,NULL),(1,6,'Malefic',3,NULL),
(1,2,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Taurus
(2,7,'Yogakaraka',1,'Best benefic — owns 9th & 10th'),(2,4,'Benefic',NULL,NULL),(2,3,'Benefic',NULL,NULL),(2,1,'Benefic',NULL,NULL),
(2,5,'Malefic',NULL,'Evil'),(2,2,'Malefic',NULL,'Evil'),(2,6,'Neutral',NULL,'Lagna lord'),
-- Gemini
(3,6,'Benefic',1,'Most beneficial'),(3,3,'Malefic',1,'Most malefic — lord of 6th & 11th'),
(3,5,'Malefic',NULL,'Evil'),(3,1,'Malefic',NULL,'Evil'),(3,2,'Neutral',NULL,NULL),(3,4,'Neutral',NULL,NULL),
(3,7,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Cancer
(4,3,'Yogakaraka',1,'Lord of 5th & 10th'),(4,5,'Benefic',NULL,NULL),
(4,6,'Malefic',NULL,'Evil'),(4,4,'Malefic',NULL,'Evil'),(4,7,'Neutral',NULL,NULL),(4,2,'Neutral',NULL,NULL),(4,1,'Neutral',NULL,NULL),
-- Leo
(5,3,'Yogakaraka',1,'Most auspicious; lord of 4th & 9th (Yogakarakas section)'),(5,1,'Benefic',NULL,NULL),
(5,4,'Malefic',NULL,NULL),(5,6,'Malefic',NULL,NULL),(5,5,'Neutral',NULL,NULL),(5,2,'Neutral',NULL,NULL),(5,7,'Neutral',NULL,NULL),
-- Virgo
(6,6,'Benefic',1,'Best benefic'),
(6,2,'Malefic',NULL,'Evil'),(6,3,'Malefic',NULL,'Evil'),(6,5,'Malefic',NULL,'Evil'),
(6,7,'Neutral',NULL,NULL),(6,1,'Neutral',NULL,NULL),(6,4,'Neutral',NULL,NULL),
-- Libra
(7,7,'Yogakaraka',1,'Best benefic — lord of 4th & 5th'),(7,4,'Benefic',NULL,NULL),(7,6,'Benefic',NULL,NULL),
(7,3,'Benefic',NULL,'Feeble benefic'),(7,1,'Malefic',NULL,NULL),(7,5,'Malefic',NULL,NULL),(7,2,'Malefic',NULL,NULL),
-- Scorpio
(8,2,'Benefic',1,'Best benefic'),(8,5,'Benefic',NULL,NULL),(8,1,'Benefic',NULL,NULL),
(8,4,'Malefic',NULL,'Evil'),(8,6,'Malefic',NULL,'Evil'),(8,3,'Neutral',NULL,NULL),(8,7,'Neutral',NULL,NULL),
-- Sagittarius
(9,3,'Benefic',NULL,NULL),(9,1,'Benefic',NULL,NULL),
(9,6,'Malefic',NULL,'Evil'),(9,7,'Malefic',NULL,'Evil'),(9,4,'Malefic',NULL,'Evil'),(9,5,'Neutral',NULL,NULL),(9,2,'Neutral',NULL,NULL),
-- Capricorn
(10,6,'Yogakaraka',1,'Most powerful benefic — lord of 5th & 10th'),(10,4,'Benefic',NULL,NULL),(10,7,'Benefic',NULL,NULL),
(10,3,'Malefic',1,'Worst'),(10,5,'Malefic',NULL,'Evil'),(10,2,'Malefic',NULL,'Evil'),(10,1,'Neutral',NULL,'8th lord — becomes neutral'),
-- Aquarius
(11,6,'Yogakaraka',1,'Lord of 4th & 9th'),(11,1,'Benefic',NULL,NULL),(11,3,'Benefic',NULL,NULL),
(11,5,'Malefic',NULL,NULL),(11,2,'Malefic',NULL,NULL),(11,4,'Neutral',NULL,NULL),
(11,7,NULL,NULL,'Not classified in source (How to Judge a Horoscope, Raman, p.16-18)'),
-- Pisces
(12,2,'Benefic',NULL,NULL),(12,3,'Benefic',NULL,NULL),
(12,7,'Malefic',NULL,NULL),(12,1,'Malefic',NULL,NULL),(12,6,'Malefic',NULL,NULL),(12,4,'Malefic',NULL,NULL),(12,5,'Neutral',NULL,NULL);
GO
```

- [ ] **Step 2: Apply the migration**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\031_create_lagna_functional_nature.sql`
Expected: no errors.

- [ ] **Step 3: Verify shape**

```
sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "
SELECT COUNT(*) AS total, SUM(CASE WHEN FunctionalNature IS NULL THEN 1 ELSE 0 END) AS nulls,
       SUM(CASE WHEN FunctionalNature='Yogakaraka' THEN 1 ELSE 0 END) AS yogakarakas FROM tbl_Dim_LagnaFunctionalNature;
SELECT s.SignName, p.PlanetName, f.FunctionalNature, f.Rank, f.Notes
FROM tbl_Dim_LagnaFunctionalNature f
JOIN tbl_SignAttributes s ON s.Id=f.LagnaSignId JOIN tbl_Planets p ON p.Id=f.PlanetId
WHERE (s.SignName='Taurus' AND p.PlanetName='Saturn') OR (s.SignName='Aries' AND p.PlanetName IN ('Mercury','Moon'))
ORDER BY s.SignName, p.PlanetName;"
```

Expected: `total=84`, `nulls=3`, `yogakarakas=6`; Taurus/Saturn = `Yogakaraka` rank 1; Aries/Mercury = `Malefic` rank 1; Aries/Moon = `NULL` with the "Not classified in source" note.

- [ ] **Step 4: Create the model + repo**

`src/Ikiastrro.Core/Models/LagnaFunctionalNatureRow.cs`:

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>One row of tbl_Dim_LagnaFunctionalNature (migration 031) — Raman's functional
/// classification of one planet for one Lagna. FunctionalNature null = the source does not classify it.</summary>
public record LagnaFunctionalNatureRow(byte Id, byte LagnaSignId, byte PlanetId, string? FunctionalNature, byte? Rank, string? Notes);
```

`src/Ikiastrro.Data/LagnaFunctionalNatureRepository.cs`:

```csharp
using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Dim_LagnaFunctionalNature (migration 031) — read-only reference mirror.</summary>
public class LagnaFunctionalNatureRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public LagnaFunctionalNatureRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <summary>All 7 classical planets' rows for one Lagna sign (tbl_SignAttributes.Id).</summary>
    public IReadOnlyList<LagnaFunctionalNatureRow> GetForLagna(byte lagnaSignId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Dim_LagnaFunctionalNature WHERE LagnaSignId = @LagnaSignId ORDER BY PlanetId";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<LagnaFunctionalNatureRow>(sql, new { LagnaSignId = lagnaSignId }).ToList();
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 6: Cross-check the heuristic vs. the book table (informational)**

```
dotnet run --project src/Ikiastrro.Cli -- verify-functional-nature
```
Still exits 0. Then eyeball: for Aries Lagna the heuristic gives Jupiter=Benefic, Mercury=Malefic, Sun=Benefic, Saturn=Malefic, Venus=Malefic, Mars=Benefic, Moon=Malefic — matching migration 031 except Moon (book = NULL, heuristic = Malefic, expected divergence per spec §7.3). Record any other divergences in the Task 8 checkpoint note for Plan 2's `FunctionalNaturePanel` to display side by side.

- [ ] **Step 7: Checkpoint** — Task 8 complete. Plan 1 done: run `dotnet build` (clean) + `verify-vargas` + `verify-functional-nature` (both exit 0) as the final gate.

---

## Self-Review

**Spec coverage (Phase 0 items):**
- §5 renames (`D1ChartViewModel`/`D1PlanetRow`) → Task 1. ✅
- §7.1 `LagnaFunctionalNature` + `verify-functional-nature` → Task 2. ✅
- §7.2 migration 031 + repo → Task 8. ✅
- §6.1 `GetByBirthDetailId` batch reads → Task 3. ✅
- §12 Sade Sati / transit / house-nakshatra-span repos → Task 4. ✅
- §13.4 reference repos → Task 5. ✅
- §8 `LifeAreaMap` → Task 2b. ✅
- §11 `ChartGenerationService` + cutover + `compute-all` → Tasks 6, 7. ✅
- §14 Phase 0 "verify-functional-nature CLI mode" → Task 2. ✅

**Placeholder scan:** Task 5 Step 2 leaves the `type_house_*` column handling as "confirm from Step 1 and pick one" — acceptable because Step 1 produces the authoritative list and both handling options are spelled out. No `TBD`/`TODO`/"implement later" elsewhere. Every code step has real code.

**Type consistency:** `ChartGenerationService` method names (`GenerateAll`/`GenerateMissing`/`RecomputeAnalytics`) and `GenerationReport` fields (`ChartTypesWritten`/`DashaWritten`/`Skipped`) are used identically in Tasks 6 and 7. `FunctionalNatureResult` fields (`Nature`/`RuledHouses`/`IsMaraka`/`KendradhipatiDosha`/`Rationale`) match between Task 2's implementation and its CLI asserts. `LagnaFunctionalNatureRow` fields match between Task 8's model and the migration columns. `PlanetTransitSnapshot` fields match between Task 4's model and `GetSnapshot`.

**All spec Phase 0 items have a task.** One deviation from spec §11 recorded in Global Constraints: no cross-repo DB transaction (repo layer opens a connection per call); `ChartGenerationService` uses delete-first-then-regenerate so partial failures self-heal on re-run.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-30-web-ui-recreate-groundwork.md`. Two execution options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — tasks run in this session via executing-plans, batch execution with checkpoints.

Which approach? (Plan 2 — the UI Phases 1–6 — will be written after this plan executes, so its tasks bind to the real signatures this plan produces.)
