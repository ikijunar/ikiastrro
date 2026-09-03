# Chara Karakas & Special Points — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Compute and store, for every saved person, the Jaimini Chara Karakas (8-karaka
Ashta) and the special points AL + 12 Bhava Arudhas + Hora Lagna + Gulika + Maandi — the
latter carried through all 21 divisional charts — plus a static combined D1+D9 chart.

**Architecture:** Chara Karakas ride a new `CharaKaraka` column on `tbl_Chart_KeyDetails`,
set on the 8 graha rows of every chart. Special points ride new rows in the same table,
discriminated by a new `PointKind` column, projected into each chart's zodiac with the same
`IVargaSignRule` a planet uses. A new `SpecialPointSeed` channel flows from
`ChartCalculationOrchestrator` (computes the D1 longitudes once) through each
`IChartCalculator` into `VargaChartComputer` / `D1ChartComputer` and out via a new
`ChartAnalyzer` loop. Sunrise/sunset comes from a new `SwissEphemerisProvider.GetSunTimes`.

**Tech Stack:** .NET 8 / C#; SwissEphNet 2.8.0.2 (`swe_rise_trans`); SQL Server (`db/`
numbered idempotent migrations + `dbo.SchemaMigrations` ledger); Dapper; verification via
`dotnet build` + CLI `verify-*` modes (no xUnit project — established project cadence).

**Spec:** `docs/superpowers/specs/2026-09-01-jaimini-chara-karaka-special-points-design.md`

## Global Constraints

- Branch: `feat/ikiastrro-workspace-ui` (work directly on it, as Plan A did).
- Every commit ends with exactly these two trailers:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW
  ```
- Do **not** push unless the user asks.
- Migrations: numbered `NN_<verb>_<noun>.sql` under `db/`, idempotent, self-recording in
  `dbo.SchemaMigrations`; last applied is `13`, so this plan adds `14`. Apply to the live
  `ikiastrro` DB (Windows Auth, `localhost`) with
  `sqlcmd -S localhost -E -d ikiastrro -b -i db/14_*.sql`.
- **Chara Karaka scheme = 8-karaka (Ashta)**: rank the 8 grahas `Sun Moon Mars Mercury
  Jupiter Venus Saturn Rahu` by degree-within-sign descending; **Rahu's key = `30 −
  itsDegreeInSign`**; highest → `AK`, lowest → `DK`; Ketu is not ranked.
- **DB-completeness invariant** (spec `2026-08-31-divisional-chart-completion-design.md` §2):
  every computed value has a typed column; `ResultJson` is a non-authoritative snapshot.
- `PlanetName` enum order is `Sun Moon Mars Mercury Jupiter Venus Saturn Rahu Ketu`;
  `PlanetNames.All9` is that list. `ZodiacName`: Aries=0 … `Capricornus`=9 (Latin spelling)
  … Pisces=11.
- Golden record for every `verify-jaimini` assert: `scratch/Rammy_Jagannatha.txt` (person
  `1_Ramakrishnan`, 22 Apr 1981 05:30 Chennai). Reproduce its printed values exactly.

---

## File Structure

**New — Core:**
- `src/Ikiastrro.Core/Jaimini/CharaKaraka.cs` — the `CharaKaraka` enum.
- `src/Ikiastrro.Core/Jaimini/CharaKarakaCalculator.cs` — the ranking (pure).
- `src/Ikiastrro.Core/SpecialPoints/SpecialPointSeed.cs` — `record (string Code, string
  PointKind, double NirayanaLongitudeDegrees)` — a special point's D1 longitude before any
  varga projection.
- `src/Ikiastrro.Core/SpecialPoints/SpecialPointProjector.cs` — `Project(seeds, rule,
  divisionFactor, lagnaSign) : List<PlanetPosition>` — turns seeds into per-chart
  `PlanetPosition`s (Sign / VargaLongitude / house). Shared by `VargaChartComputer` and
  `D1ChartComputer`.
- `src/Ikiastrro.Core/SpecialPoints/ArudhaCalculator.cs` — the 12 Bhava Arudhas (pure, D1
  only).
- `src/Ikiastrro.Core/SpecialPoints/HoraLagnaCalculator.cs` — Hora Lagna (needs sunrise).
- `src/Ikiastrro.Core/SpecialPoints/UpagrahaCalculator.cs` — Gulika + Maandi (need sunrise).
- `src/Ikiastrro.Core/SpecialPoints/SpecialPointCalculator.cs` — the façade:
  `ComputeSeeds(BirthDetails) : IReadOnlyList<SpecialPointSeed>` (Arudhas from T4; HL/Gulika/
  Maandi from T5).

**Modified — Core:**
- `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs` — `SunTimes` record + `GetSunTimes`.
- `src/Ikiastrro.Core/Models/PlanetPosition.cs` — add `string PointKind` (default `"Graha"`).
- `src/Ikiastrro.Core/Models/ChartKeyDetail.cs` — add `string PointKind`, `string? CharaKaraka`.
- `src/Ikiastrro.Core/Calculators/ChartAnalysisInput.cs` — add `IReadOnlyList<PlanetPosition>
  SpecialPoints { get; init; }`.
- `src/Ikiastrro.Core/Calculators/IChartCalculator.cs` — `ComputeAnalysisInput` gains
  `IReadOnlyList<SpecialPointSeed>? specialPoints`.
- `src/Ikiastrro.Core/Calculators/D1RasiCalculator.cs`,
  `src/Ikiastrro.Core/Calculators/VargaCalculator.cs` — pass the seeds through.
- `src/Ikiastrro.Core/Calculators/D1ChartComputer.cs`,
  `src/Ikiastrro.Core/Calculators/VargaChartComputer.cs` — project seeds into `SpecialPoints`.
- `src/Ikiastrro.Core/Calculators/ChartCalculationOrchestrator.cs` — compute seeds once,
  pass to each calculator.
- `src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs` — emit `ChartKeyDetail` rows for
  `input.SpecialPoints`.

**Modified — Data:**
- `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs` — INSERT `PointKind`, `CharaKaraka`.
- `src/Ikiastrro.Data/ChartGenerationService.cs` — compute the Chara Karaka dict once per
  person, stamp it onto the graha KeyDetail rows.

**Modified — CLI:**
- `src/Ikiastrro.Cli/Program.cs` — new `verify-jaimini` mode; `verify-vargas` /
  `verify-schema` queries made `PointKind`-aware.

**New — DB:**
- `db/14_add_karaka_and_pointkind.sql`.

**Modified — DB:**
- `db/ikiastrro.sql` — fold migration 14 (Task 6); `vw_Chart_Consolidated` += 2 columns.

**New — Web:**
- `src/Ikiastrro.Web/Components/Charts/CombinedD1D9Grid.razor` (+ `.razor.css`).
- `src/Ikiastrro.Web/Components/Shared/PlanetChip.razor` (+ `.razor.css`) — minimal.

**Modified — Web:**
- `src/Ikiastrro.Web/wwwroot/css/tokens.css` — 9 `--planet-*` + 9 `--planet-*-bg`.
- `src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor` — mount the combined grid.

**Docs (Task 8):** `docs/reference-calculations.md`, `ARCHITECTURE.md`,
`docs/dbdesign-star-schema-rules-engine.md`, `master_ikiastrro.md`, `../ikiastrro.md`,
memory `memproj_vedic_horo_gen.md`.

---

## Task 1: Migration 14 — `PointKind` + `CharaKaraka` columns

**Files:**
- Create: `db/14_add_karaka_and_pointkind.sql`
- Modify: `src/Ikiastrro.Cli/Program.cs:124-386` (`verify-vargas` block — filter additions),
  `src/Ikiastrro.Cli/Program.cs:485-557` (`verify-schema` block — the `PlanetId populated`
  rule)

**Interfaces:**
- Produces: `tbl_Chart_KeyDetails.PointKind VARCHAR(12) NOT NULL DEFAULT 'Graha'` (∈
  `Graha|SpecialLagna|Arudha|Upagraha`) and `tbl_Chart_KeyDetails.CharaKaraka VARCHAR(4)
  NULL` (∈ `AK|AmK|BK|MK|PiK|PK|GK|DK`), with `CK_KeyDetails_PointKind`,
  `CK_KeyDetails_CharaKaraka`, `CK_KeyDetails_NonGrahaNulls` check constraints.

- [ ] **Step 1: Write `db/14_add_karaka_and_pointkind.sql`**

```sql
-- =====================================================================
-- 14 — tbl_Chart_KeyDetails gains PointKind (row discriminator: Graha vs the
-- special points AL/A2..A12/HL/Gulika/Maandi) and CharaKaraka (Jaimini 8-karaka
-- label on the 8 graha rows). Feeds C# (ChartAnalyzer) and the Web combined chart.
-- CK_KeyDetails_NonGrahaNulls forbids graha-only analytics on non-Graha rows.
-- Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/14_add_karaka_and_pointkind.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'PointKind') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD PointKind VARCHAR(12) NOT NULL
            CONSTRAINT DF_KeyDetails_PointKind DEFAULT 'Graha';
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'CharaKaraka') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CharaKaraka VARCHAR(4) NULL;
GO
IF OBJECT_ID('dbo.CK_KeyDetails_PointKind', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_PointKind
        CHECK (PointKind IN ('Graha','SpecialLagna','Arudha','Upagraha'));
GO
IF OBJECT_ID('dbo.CK_KeyDetails_CharaKaraka', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_CharaKaraka
        CHECK (CharaKaraka IS NULL
            OR CharaKaraka IN ('AK','AmK','BK','MK','PiK','PK','GK','DK'));
GO
IF OBJECT_ID('dbo.CK_KeyDetails_NonGrahaNulls', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_NonGrahaNulls
        CHECK (PointKind = 'Graha'
            OR (PlanetId IS NULL AND DignityStatus IS NULL AND Nakshatra IS NULL
                AND CharaKaraka IS NULL AND AspectingPlanets IS NULL
                AND IsCombust IS NULL AND NakshatraLordPlanet IS NULL));
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '14_add_karaka_and_pointkind.sql',
       'tbl_Chart_KeyDetails += PointKind, CharaKaraka (+3 CHECKs)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations
                  WHERE ScriptName = '14_add_karaka_and_pointkind.sql');
GO
PRINT '14 applied: tbl_Chart_KeyDetails has PointKind + CharaKaraka.';
GO
```

- [ ] **Step 2: Apply to the live DB**

Run: `sqlcmd -S localhost -E -d ikiastrro -b -i db/14_add_karaka_and_pointkind.sql`
Expected: prints `14 applied…`, no `Msg` lines. Re-run once → same, still no errors
(idempotency).

- [ ] **Step 3: Make `verify-vargas` `PointKind`-aware**

In `src/Ikiastrro.Cli/Program.cs`, inside the `verify-vargas` block (the degree-sanity
`using (var conn …)` section added in Plan A Task 18), every DB query that filters
`kd.Planet <> 'Ascendant'` also gets `AND kd.PointKind = 'Graha'`. There are four such
queries (`all VargaLongitudeDegrees in [0,360)`, `all DegreesInSignDecimal in [0,30)`,
`DegreesInSignDecimal == VargaLongitudeDegrees mod 30`, `varga KeyDetails SignId populated
and in [1,12]`) plus the two DB-completeness queries and the JHora-grid DB loop's
`SELECT … FROM tbl_Chart_KeyDetails … WHERE … Planet` — add `AND PointKind = 'Graha'` to
each. No behaviour change today (no non-Graha rows yet); prevents special-point rows from
polluting the planet-only assertions once Task 4 lands.

- [ ] **Step 4: Scope the `verify-schema` `PlanetId populated` rule**

In the `verify-schema` block, change the check currently reading
`... FROM tbl_Chart_KeyDetails WHERE Planet <> 'Ascendant' AND PlanetId IS NULL` (label
`KeyDetails.PlanetId populated (non-Ascendant)`) to
`... WHERE PointKind = 'Graha' AND Planet <> 'Ascendant' AND PlanetId IS NULL`.

- [ ] **Step 5: Build + verify**

Run:
```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-schema
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-vargas
```
Expected: build 0/0; both `ALL PASS` (unchanged counts — the new column defaults to
`'Graha'` for all existing rows).

- [ ] **Step 6: Commit**

```bash
git add db/14_add_karaka_and_pointkind.sql src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(db): migration 14 — PointKind + CharaKaraka on tbl_Chart_KeyDetails\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 2: Sunrise / sunset — `SwissEphemerisProvider.GetSunTimes` + `verify-jaimini` scaffold

**Files:**
- Modify: `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (new `verify-jaimini` mode, before `verify-schema`)

**Interfaces:**
- Produces: `SwissEphemerisProvider.SunTimes` (`record (DateTimeOffset Sunrise,
  DateTimeOffset Sunset, DateTimeOffset PriorSunrise, bool IsNightBirth)`) and
  `static SunTimes GetSunTimes(BirthDetails birthDetails)`.
- Produces: CLI mode `verify-jaimini` (grows over Tasks 3–5).

- [ ] **Step 1: Confirm `swe_rise_trans` in the package**

Run: `grep -rn "swe_rise_trans" $(dotnet nuget locals global-packages -l | sed 's/.*: //')/swissephnet/2.8.0.2/`
Expected: a signature like
`int swe_rise_trans(double tjd_ut, int ipl, string starname, int epheflag, int rsmi, double[] geopos, double atpress, double attemp, ref double tret, ref string serr)`.
If absent, fall back to the manual sunrise formula in Step 3's alternative (documented
there); do not block.

- [ ] **Step 2: Write the failing assertion first**

Add this block to `src/Ikiastrro.Cli/Program.cs` immediately before
`if (args.Length > 0 && args[0] == "verify-schema")`:

```csharp
// --- One-off check: `dotnet run -- verify-jaimini` ---
// Worked-example assertions for the Jaimini Chara Karakas + special points against
// scratch/Rammy_Jagannatha.txt (person 1_Ramakrishnan). Solution has no unit-test project.
if (args.Length > 0 && args[0] == "verify-jaimini")
{
    var failures = 0;
    void Check(string label, object? actual, object? expected)
    {
        var ok = $"{actual}" == $"{expected}";
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    var ram = birthDetailsRepo.GetAll().First(p => p.Name == "Ramakrishnan");

    // --- Phase 1: sunrise / sunset (Chennai, 22 Apr 1981) ---
    var sun = SwissEphemerisProvider.GetSunTimes(ram);
    Check("sunrise (local HH:mm:ss)", sun.Sunrise.ToString("HH:mm:ss"), "05:56:39");
    Check("sunset (local HH:mm:ss)",  sun.Sunset.ToString("HH:mm:ss"),  "18:18:53");
    Check("night birth (05:30 < sunrise)", sun.IsNightBirth, true);

    Console.WriteLine(failures == 0 ? "\nverify-jaimini: ALL PASS" : $"\nverify-jaimini: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

- [ ] **Step 3: Run it — expect a compile error, then a FAIL**

Run: `dotnet build Ikiastrro.slnx`
Expected: FAIL — `'SwissEphemerisProvider' does not contain a definition for 'GetSunTimes'`.

- [ ] **Step 4: Implement `GetSunTimes`**

In `SwissEphemerisProvider.cs`, add the record near `SiderealPositions`:

```csharp
/// <summary>
/// Sunrise / sunset for a person's birth date & place, and the sunrise immediately
/// BEFORE the birth moment (used for a "night birth" — birth between midnight and
/// the day's sunrise, as 1_Ramakrishnan's 05:30 vs 05:56 sunrise is). All three are
/// local-offset DateTimeOffsets, using BirthDetails.UtcOffset.
/// </summary>
public record SunTimes(
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    DateTimeOffset PriorSunrise,
    bool IsNightBirth);
```

and the method (uses `swe_rise_trans`; disc centre, no refraction — JHora's default; tune
`rsmi` flags in Step 5 against the export):

```csharp
public static SunTimes GetSunTimes(BirthDetails birthDetails)
{
    var moment = BirthMomentFactory.Create(birthDetails);   // local-offset
    var offset = moment.Offset;
    using var sweph = new SwissEph();
    sweph.swe_set_sid_mode(SeSidmLahiri, 0, 0);

    var geopos = new double[] { birthDetails.Longitude, birthDetails.Latitude, 0.0 };

    // Search from just before local midnight of the birth date (in UT).
    var localMidnight = new DateTimeOffset(birthDetails.DateOfBirth.ToDateTime(TimeOnly.MinValue), offset);
    double JdOf(DateTimeOffset t)
    {
        var u = t.ToUniversalTime();
        return sweph.swe_julday(u.Year, u.Month, u.Day,
            u.Hour + u.Minute / 60.0 + u.Second / 3600.0, SwissEph.SE_GREG_CAL);
    }
    DateTimeOffset LocalOf(double jdUt)
    {
        sweph.swe_revjul(jdUt, SwissEph.SE_GREG_CAL, out var y, out var mo, out var d, out var h);
        var whole = (int)h; var min = (int)((h - whole) * 60);
        var sec = (int)Math.Round(((h - whole) * 60 - min) * 60);
        var utc = new DateTimeOffset(y, mo, d, whole, min, 0, TimeSpan.Zero).AddSeconds(sec);
        return utc.ToOffset(offset);
    }
    double Rise(double fromJd, int rsmi)
    {
        double tret = 0; string serr = "";
        var rc = sweph.swe_rise_trans(fromJd, SwissEph.SE_SUN, null,
            SwissEph.SEFLG_MOSEPH, rsmi, geopos, 0.0, 0.0, ref tret, ref serr);
        if (rc < 0) throw new InvalidOperationException($"swe_rise_trans failed: {serr}");
        return tret;
    }
    const int riseFlag = SwissEph.SE_CALC_RISE | SwissEph.SE_BIT_DISC_CENTER | SwissEph.SE_BIT_NO_REFRACTION;
    const int setFlag  = SwissEph.SE_CALC_SET  | SwissEph.SE_BIT_DISC_CENTER | SwissEph.SE_BIT_NO_REFRACTION;

    var midnightJd = JdOf(localMidnight);
    var sunriseJd = Rise(midnightJd, riseFlag);          // first sunrise on/after local midnight
    var sunsetJd  = Rise(sunriseJd, setFlag);            // the sunset that follows it
    var priorSunriseJd = Rise(midnightJd - 1.0, riseFlag); // sunrise the day before

    var sunrise = LocalOf(sunriseJd);
    var sunset  = LocalOf(sunsetJd);
    var prior   = LocalOf(priorSunriseJd);
    var isNight = moment < sunrise;   // birth is before the day's sunrise -> night arc

    return new SunTimes(sunrise, sunset, prior, isNight);
}
```

*(If `swe_rise_trans` / the `SE_*` rise constants are not in the port: implement `Rise` with
the standard hour-angle formula — Sun's sidereal→tropical declination for the date, observer
latitude, `cos H = −tan φ · tan δ`, apparent-solar-time → clock-time via the equation of
time. Keep the same `SunTimes` shape and tune to `05:56:39` the same way.)*

- [ ] **Step 5: Tune to the export, run `verify-jaimini`**

Run: `dotnet build Ikiastrro.slnx && dotnet run --project src/Ikiastrro.Cli --no-build -- verify-jaimini`
Expected: the 3 checks PASS. If sunrise is off by seconds, adjust the `SE_BIT_*` flags
(`SE_BIT_DISC_CENTER` on/off, `SE_BIT_NO_REFRACTION` on/off) until `05:56:39` / `18:18:53`
match; record the winning combination in a code comment.

- [ ] **Step 6: Full sweep + commit**

Run: `for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini; do dotnet run --project src/Ikiastrro.Cli --no-build -- $m | tail -1; done`
Expected: all `ALL PASS`.

```bash
git add src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): SwissEphemerisProvider.GetSunTimes (swe_rise_trans) + verify-jaimini\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 3: `CharaKarakaCalculator` + wire into analytics

**Files:**
- Create: `src/Ikiastrro.Core/Jaimini/CharaKaraka.cs`,
  `src/Ikiastrro.Core/Jaimini/CharaKarakaCalculator.cs`
- Modify: `src/Ikiastrro.Core/Models/ChartKeyDetail.cs`,
  `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs`,
  `src/Ikiastrro.Data/ChartGenerationService.cs`,
  `src/Ikiastrro.Cli/Program.cs` (`verify-jaimini`, `verify-schema`)

**Interfaces:**
- Consumes: D1 degree-in-sign per graha (`positions.PlanetLongitudes[p] % 30` from
  `SwissEphemerisProvider.GetSiderealPositions`).
- Produces: `CharaKarakaCalculator.Assign(IReadOnlyDictionary<PlanetName,double>
  degreeInSignByPlanet) : IReadOnlyDictionary<PlanetName, CharaKaraka>`;
  `ChartKeyDetail.CharaKaraka` (string, e.g. `"AK"`).

- [ ] **Step 1: `CharaKaraka.cs`**

```csharp
namespace Ikiastrro.Core.Jaimini;

/// <summary>Jaimini movable significators, in ranked order (AK strongest, DK weakest).</summary>
public enum CharaKaraka { AK, AmK, BK, MK, PiK, PK, GK, DK }
```

- [ ] **Step 2: `verify-jaimini` — add the karaka asserts (they will FAIL to compile)**

In the `verify-jaimini` block, after the sunrise checks, add:

```csharp
    // --- Chara Karakas (8-karaka Ashta) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var d1 = conn.Query<(string Planet, string? CharaKaraka)>(
            @"SELECT kd.Planet, kd.CharaKaraka
              FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D1' AND kd.CharaKaraka IS NOT NULL")
            .ToDictionary(r => r.CharaKaraka!, r => r.Planet);
        Check("AK  = Rahu",    d1.GetValueOrDefault("AK"),  "Rahu");
        Check("AmK = Venus",   d1.GetValueOrDefault("AmK"), "Venus");
        Check("BK  = Saturn",  d1.GetValueOrDefault("BK"),  "Saturn");
        Check("MK  = Jupiter", d1.GetValueOrDefault("MK"),  "Jupiter");
        Check("PiK = Sun",     d1.GetValueOrDefault("PiK"), "Sun");
        Check("PK  = Moon",    d1.GetValueOrDefault("PK"),  "Moon");
        Check("GK  = Mars",    d1.GetValueOrDefault("GK"),  "Mars");
        Check("DK  = Mercury", d1.GetValueOrDefault("DK"),  "Mercury");
        // karaka label travels to every varga
        var d9ak = conn.ExecuteScalar<string>(
            @"SELECT kd.Planet FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D9' AND kd.CharaKaraka = 'AK'");
        Check("D9 AK label travels", d9ak, "Rahu");
    }
```

Also add a pure-function check right after the sunrise block:

```csharp
    // hand-computed ranking check (independent of the DB)
    var kd = CharaKarakaCalculator.Assign(new Dictionary<PlanetName, double>
    {
        [PlanetName.Sun] = 8.205, [PlanetName.Moon] = 7.293, [PlanetName.Mars] = 3.950,
        [PlanetName.Mercury] = 1.842, [PlanetName.Jupiter] = 8.727, [PlanetName.Venus] = 11.992,
        [PlanetName.Saturn] = 10.956, [PlanetName.Rahu] = 13.059,   // raw; calc reverses Rahu
    });
    Check("Assign: Rahu -> AK", kd[PlanetName.Rahu], Ikiastrro.Core.Jaimini.CharaKaraka.AK);
    Check("Assign: Mercury -> DK", kd[PlanetName.Mercury], Ikiastrro.Core.Jaimini.CharaKaraka.DK);
```

Add `using Ikiastrro.Core.Jaimini;` to `Program.cs` if not present.

- [ ] **Step 3: Build — expect FAIL** (`CharaKarakaCalculator` missing). Confirm.

- [ ] **Step 4: `CharaKarakaCalculator.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Jaimini;

/// <summary>
/// Jaimini 8-karaka (Ashta) assignment from the D1 chart. Ranks the 8 grahas
/// (Sun..Saturn, Rahu) by longitude WITHIN their sign, descending; Rahu's key is
/// (30 - itsDegreeInSign) because it is always retrograde. Highest -> AK, lowest -> DK.
/// Ketu is not ranked.
/// </summary>
public static class CharaKarakaCalculator
{
    private static readonly PlanetName[] Ranked =
    {
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
        PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu
    };

    public static IReadOnlyDictionary<PlanetName, CharaKaraka> Assign(
        IReadOnlyDictionary<PlanetName, double> degreeInSignByPlanet)
    {
        double Key(PlanetName p)
        {
            var deg = degreeInSignByPlanet[p] % 30.0;
            if (deg < 0) deg += 30.0;
            return p == PlanetName.Rahu ? 30.0 - deg : deg;
        }

        var order = Ranked
            .OrderByDescending(Key)
            .ThenBy(p => Array.IndexOf(Ranked, p))   // stable tie-break (never fires on real data)
            .ToArray();

        var result = new Dictionary<PlanetName, CharaKaraka>();
        for (var i = 0; i < order.Length; i++)
            result[order[i]] = (CharaKaraka)i;
        return result;
    }
}
```

- [ ] **Step 5: `ChartKeyDetail.cs` — add the fields**

After `public double VargaLongitudeDegrees { get; set; }` add:

```csharp
    /// <summary>Row discriminator: 'Graha' (a planet or the Ascendant) or one of the special
    /// points 'SpecialLagna' / 'Arudha' / 'Upagraha'. Non-'Graha' rows carry only position
    /// (Sign / longitudes / house); dignity, nakshatra, combustion, aspects, CharaKaraka are NULL.</summary>
    public string PointKind { get; set; } = "Graha";

    /// <summary>Jaimini Chara Karaka label ('AK'..'DK') for this graha in this person's D1 —
    /// the same label on every chart type. NULL for Ketu, the Ascendant, and special points.</summary>
    public string? CharaKaraka { get; set; }
```

- [ ] **Step 6: `ChartKeyDetailsRepository.cs` — INSERT the columns**

Add `PointKind` and `CharaKaraka` to the INSERT column list and `@PointKind`,
`@CharaKaraka` to the VALUES list (Dapper maps from the `ChartKeyDetail` properties).

- [ ] **Step 7: `ChartGenerationService.cs` — compute + stamp the karaka dict**

`PersistAnalytics` becomes:

```csharp
private void PersistAnalytics(int chartResultId, ChartAnalysisInput input,
    IReadOnlyDictionary<string, string> charaKarakaByPlanet)
{
    var (keyDetails, houseLords, conjunctions, aspects) = ChartAnalyzer.Compute(input);
    foreach (var r in keyDetails)
        if (r.PointKind == "Graha" && charaKarakaByPlanet.TryGetValue(r.Planet, out var ck))
            r.CharaKaraka = ck;
    var avasthas = PlanetAvasthaComputer.Compute(input, keyDetails, AvasthaRules);
    // … (rest unchanged: stamp chartResultId, insert all) …
}
```

Add a private helper and call it from the three public methods (`GenerateAll`,
`GenerateMissing`, `RecomputeAnalytics`) — each already computes
`var ctx = SwissEphemerisProvider.GetSiderealPositions(bd)`; reuse `ctx`:

```csharp
private static IReadOnlyDictionary<string, string> CharaKarakaByPlanet(SiderealPositions ctx)
{
    var degIn = new Dictionary<PlanetName, double>();
    foreach (var p in new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
                              PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu })
        degIn[p] = ctx.PlanetLongitudes[p] % 30.0;
    return CharaKarakaCalculator.Assign(degIn)
        .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
}
```

Thread the dict into every `PersistAnalytics(result.Id, input)` call (there are 3).
`PersistCharts` already has `ctx` in scope; `GenerateMissing` and `RecomputeAnalytics` too.
Add `using Ikiastrro.Core.Jaimini;` and `using Ikiastrro.Core.Astro;` (latter already present).

- [ ] **Step 8: `verify-schema` — the karaka-count rule**

In `verify-schema`, add:

```csharp
Check("every position chart has exactly 8 distinct CharaKaraka rows",
    conn.ExecuteScalar<long>(@"
        SELECT COUNT(*) FROM (
            SELECT cr.Id, COUNT(kd.CharaKaraka) n, COUNT(DISTINCT kd.CharaKaraka) d
            FROM dbo.tbl_ChartResults cr
            JOIN dbo.tbl_Chart_KeyDetails kd ON kd.ChartResultId = cr.Id
            WHERE cr.CalculationKind = 'PositionChart'
            GROUP BY cr.Id
        ) x WHERE x.n <> 8 OR x.d <> 8"), 0L);
```

- [ ] **Step 9: Build, regenerate, verify**

```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- recompute-keydetails
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-jaimini
for m in verify-schema verify-vargas verify-avastha verify-functional-nature; do dotnet run --project src/Ikiastrro.Cli --no-build -- $m | tail -1; done
```
Expected: build 0/0; `verify-jaimini` — sunrise + all 8 karaka + travel + Assign checks
PASS; all other `verify-*` ALL PASS.

- [ ] **Step 10: Commit**

```bash
git add src/Ikiastrro.Core/Jaimini src/Ikiastrro.Core/Models/ChartKeyDetail.cs src/Ikiastrro.Data/ChartKeyDetailsRepository.cs src/Ikiastrro.Data/ChartGenerationService.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): Jaimini Chara Karakas (8-karaka) on every chart-key-details row\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 4: `ArudhaCalculator` + special-point channel + AL / A2–A12 in all 21 charts

**Files:**
- Create: `src/Ikiastrro.Core/SpecialPoints/SpecialPointSeed.cs`,
  `.../SpecialPointProjector.cs`, `.../ArudhaCalculator.cs`, `.../SpecialPointCalculator.cs`
- Modify: `src/Ikiastrro.Core/Models/PlanetPosition.cs`,
  `src/Ikiastrro.Core/Calculators/ChartAnalysisInput.cs`,
  `.../IChartCalculator.cs`, `.../D1RasiCalculator.cs`, `.../VargaCalculator.cs`,
  `.../D1ChartComputer.cs`, `.../VargaChartComputer.cs`,
  `.../ChartCalculationOrchestrator.cs`, `.../ChartAnalyzer.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-jaimini`, `verify-schema`)

**Interfaces:**
- Consumes: D1 Lagna sign + degree-in-sign, sign→lord map (`ClassicalDignity.GetSignLord`),
  planet→sign map (from `SwissEphemerisProvider`).
- Produces: `SpecialPointSeed(string Code, string PointKind, double NirayanaLongitudeDegrees)`;
  `SpecialPointCalculator.ComputeSeeds(BirthDetails) : IReadOnlyList<SpecialPointSeed>`;
  `SpecialPointProjector.Project(IEnumerable<SpecialPointSeed>, IVargaSignRule, int
  divisionFactor, ZodiacName lagnaSign) : List<PlanetPosition>`;
  `ChartAnalysisInput.SpecialPoints`; `PlanetPosition.PointKind`.
- `IChartCalculator.ComputeAnalysisInput(BirthDetails, IReadOnlyList<SpecialPointSeed>?
  specialPoints)`.

- [ ] **Step 1: `SpecialPointSeed.cs`**

```csharp
namespace Ikiastrro.Core.SpecialPoints;

/// <summary>A special point's D1 sidereal longitude, before any varga projection.
/// PointKind is 'Arudha' | 'SpecialLagna' | 'Upagraha'. Code: 'AL', 'A2'..'A12',
/// 'HL', 'Gulika', 'Maandi'.</summary>
public sealed record SpecialPointSeed(string Code, string PointKind, double NirayanaLongitudeDegrees);
```

- [ ] **Step 2: `PlanetPosition.cs` — add `PointKind`**

```csharp
    /// <summary>'Graha' for the Ascendant + 9 planets; one of 'SpecialLagna' / 'Arudha' /
    /// 'Upagraha' for a special point projected into this chart. ChartAnalyzer emits a
    /// minimal (position-only) key-details row for non-'Graha' entries.</summary>
    public string PointKind { get; set; } = "Graha";
```

- [ ] **Step 3: `ChartAnalysisInput.cs` — add `SpecialPoints`**

```csharp
public record ChartAnalysisInput(string ChartType, ZodiacName AscendantSign, List<PlanetPosition> Planets)
{
    /// <summary>AL, the 12 Bhava Arudhas, HL, Gulika, Maandi — each projected into THIS
    /// chart's zodiac with the same IVargaSignRule as the planets. Position only (Sign /
    /// VargaLongitudeDegrees / HouseNumber); no dignity/nakshatra/combustion/aspects/karaka.
    /// Empty when special points were not supplied (older callers, verify-vargas).</summary>
    public IReadOnlyList<PlanetPosition> SpecialPoints { get; init; } = Array.Empty<PlanetPosition>();
}
```

- [ ] **Step 4: `SpecialPointProjector.cs`**

```csharp
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>Turns D1 SpecialPointSeeds into per-chart PlanetPositions using the chart's own
/// IVargaSignRule + division factor — the exact transform VargaChartComputer applies to a
/// planet. For D1 pass a factor-1 identity rule (LinearVargaSignRule(1,1)).</summary>
public static class SpecialPointProjector
{
    public static List<PlanetPosition> Project(
        IEnumerable<SpecialPointSeed> seeds, IVargaSignRule rule, int divisionFactor, ZodiacName lagnaSign)
    {
        var list = new List<PlanetPosition>();
        foreach (var s in seeds)
        {
            var sign = rule.SignFor(s.NirayanaLongitudeDegrees);
            var vargaLon = AstroMath.GetVargaLongitude(s.NirayanaLongitudeDegrees, divisionFactor);
            list.Add(new PlanetPosition
            {
                Planet = s.Code,
                PointKind = s.PointKind,
                Sign = sign.ToString(),
                NirayanaLongitudeDegrees = s.NirayanaLongitudeDegrees,
                VargaLongitudeDegrees = vargaLon,
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(vargaLon % 30),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, sign)
            });
        }
        return list;
    }
}
```

- [ ] **Step 5: `ArudhaCalculator.cs`**

```csharp
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>
/// The Arudha pada of each of the 12 houses (Parashara). For house H with whole-sign lord L:
/// let n = signs counted from H's sign to L's sign (inclusive, 1..12); the pada is n signs
/// on from L's sign (inclusive). If the pada lands on H's own sign or the 7th from it, take
/// the 10th sign from the pada. The pada is placed at the natal Lagna's degree-in-sign so it
/// has a longitude for varga projection. A1 is emitted under the code "AL".
/// </summary>
public static class ArudhaCalculator
{
    public static IReadOnlyList<SpecialPointSeed> Compute(ChartAnalysisInput d1)
    {
        var lagnaSign = d1.AscendantSign;
        var asc = d1.Planets.First(p => p.Planet == "Ascendant");
        var lagnaDegInSign = (asc.NirayanaLongitudeDegrees ?? 0) % 30.0;

        var planetSign = d1.Planets
            .Where(p => p.Planet != "Ascendant")
            .ToDictionary(p => Enum.Parse<PlanetName>(p.Planet), p => Enum.Parse<ZodiacName>(p.Sign));

        int SignIndex(ZodiacName z) => (int)z;
        ZodiacName Add(ZodiacName z, int n) => (ZodiacName)(((SignIndex(z) + n) % 12 + 12) % 12);
        int CountInclusive(ZodiacName from, ZodiacName to) => ((SignIndex(to) - SignIndex(from)) % 12 + 12) % 12 + 1;

        var seeds = new List<SpecialPointSeed>();
        for (var house = 1; house <= 12; house++)
        {
            var houseSign = Add(lagnaSign, house - 1);
            var lord = Enum.Parse<PlanetName>(ClassicalDignity.GetSignLord(houseSign));
            var lordSign = planetSign[lord];
            var n = CountInclusive(houseSign, lordSign);
            var pada = Add(lordSign, n - 1);
            // exception: pada == house's own sign (1st) or the 7th from it
            var fromHouse = CountInclusive(houseSign, pada);   // 1..12
            if (fromHouse == 1 || fromHouse == 7) pada = Add(pada, 9);   // 10th sign inclusive
            var longitude = SignIndex(pada) * 30.0 + lagnaDegInSign;
            var code = house == 1 ? "AL" : $"A{house}";
            seeds.Add(new SpecialPointSeed(code, "Arudha", AstroMath.Normalize(longitude)));
        }
        return seeds;
    }
}
```

- [ ] **Step 6: `SpecialPointCalculator.cs` (Arudhas only for now)**

```csharp
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>Computes every special point's D1 longitude for a person. Task 4 wires the
/// Arudhas; Task 5 adds HL / Gulika / Maandi.</summary>
public static class SpecialPointCalculator
{
    public static IReadOnlyList<SpecialPointSeed> ComputeSeeds(BirthDetails birthDetails)
    {
        var d1 = D1ChartComputer.Compute(birthDetails, Array.Empty<SpecialPointSeed>());
        var seeds = new List<SpecialPointSeed>();
        seeds.AddRange(ArudhaCalculator.Compute(d1));
        return seeds;
    }
}
```

- [ ] **Step 7: thread the channel — interface + calculators + computers + orchestrator**

`IChartCalculator.cs`:
```csharp
    ChartAnalysisInput ComputeAnalysisInput(
        BirthDetails birthDetails,
        IReadOnlyList<SpecialPoints.SpecialPointSeed>? specialPoints = null);
```

`D1RasiCalculator.cs`:
```csharp
public ChartAnalysisInput ComputeAnalysisInput(
    BirthDetails birthDetails, IReadOnlyList<SpecialPointSeed>? specialPoints = null)
    => D1ChartComputer.Compute(birthDetails, specialPoints ?? Array.Empty<SpecialPointSeed>());
```

`VargaCalculator.cs`:
```csharp
public ChartAnalysisInput ComputeAnalysisInput(
    BirthDetails birthDetails, IReadOnlyList<SpecialPointSeed>? specialPoints = null)
{
    var input = VargaChartComputer.Compute(birthDetails, _scheme.DivisionFactor, _rule,
        specialPoints ?? Array.Empty<SpecialPointSeed>());
    return input with { ChartType = _chartType };
}
```

`D1ChartComputer.Compute` gains `IReadOnlyList<SpecialPointSeed> seeds` and, before the
`return`:
```csharp
    var specialPoints = SpecialPointProjector.Project(
        seeds, new LinearVargaSignRule(1, 1), 1, ascendantSign);
    return new ChartAnalysisInput("D1", ascendantSign, planetPositions)
        { SpecialPoints = specialPoints };
```
(add `using Ikiastrro.Core.SpecialPoints;`).

`VargaChartComputer.Compute` gains `IReadOnlyList<SpecialPointSeed> seeds` and:
```csharp
    var specialPoints = SpecialPointProjector.Project(seeds, rule, divisionFactor, lagnaSign);
    return new ChartAnalysisInput(ChartType: "", lagnaSign, planetPositions)
        { SpecialPoints = specialPoints };
```

`ChartCalculationOrchestrator.cs`:
```csharp
public IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)> CalculateAll(BirthDetails birthDetails)
{
    var seeds = SpecialPointCalculator.ComputeSeeds(birthDetails);
    var results = new List<(ChartResult, ChartAnalysisInput)>();
    foreach (var calculator in _calculators)
    {
        var input = calculator.ComputeAnalysisInput(birthDetails, seeds);
        var result = calculator.BuildResult(birthDetails, input);
        results.Add((result, input));
    }
    return results;
}

public ChartAnalysisInput ComputeAnalysisInput(string chartType, BirthDetails birthDetails)
{
    var calculator = _calculators.FirstOrDefault(c => c.ChartType == chartType)
        ?? throw new InvalidOperationException($"No calculator registered for chart type '{chartType}'.");
    var seeds = SpecialPointCalculator.ComputeSeeds(birthDetails);
    return calculator.ComputeAnalysisInput(birthDetails, seeds);
}
```
(add `using Ikiastrro.Core.SpecialPoints;`).

- [ ] **Step 8: `ChartAnalyzer.cs` — emit special-point rows**

After the `foreach (var planet in input.Planets)` loop that fills `keyDetails`, add:

```csharp
        foreach (var sp in input.SpecialPoints)
        {
            var spSign = Enum.Parse<ZodiacName>(sp.Sign);
            var spVargaLon = sp.VargaLongitudeDegrees ?? sp.NirayanaLongitudeDegrees ?? 0;
            keyDetails.Add(new ChartKeyDetail
            {
                Planet = sp.Planet,               // "AL", "A2".."A12", later "HL"/"Gulika"/"Maandi"
                PointKind = sp.PointKind,
                Sign = sp.Sign,
                SignId = AstroIds.SignId(spSign),
                NirayanaLongitudeDegrees = sp.NirayanaLongitudeDegrees ?? 0,
                VargaLongitudeDegrees = spVargaLon,
                DegreesInSignDisplay = AstroMath.FormatDegreesMinutesSeconds(spVargaLon % 30),
                DegreesInSignDecimal = Math.Round((decimal)(spVargaLon % 30), 4),
                HouseNumberFromLagna = sp.HouseNumber,
                HouseNumberFromSun = AstroMath.CountFromSignToSign(sunSign, spSign),
                HouseNumberFromMoon = AstroMath.CountFromSignToSign(moonSign, spSign),
                // everything else stays null — CK_KeyDetails_NonGrahaNulls enforces it
            });
        }
```

`placementByPlanet` already filters `k.Planet != "Ascendant"`; special points have distinct
codes so they will not collide, but they are also not grahas — change that filter to
`k.PointKind == "Graha" && k.Planet != "Ascendant"` so house-lord placement lookups can't
pick up a special-point row.

- [ ] **Step 9: `verify-jaimini` — AL assert + channel integrity**

Add to the `verify-jaimini` DB `using` block:

```csharp
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        string SpSign(string chart, string code) => conn.ExecuteScalar<string>(
            @"SELECT kd.Sign FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType=@c AND kd.Planet=@p AND kd.PointKind<>'Graha'",
            new { c = chart, p = code });

        Check("AL (D1) -> Capricornus", SpSign("D1", "AL"), "Capricornus");
        Check("A2..A12 present in D1", conn.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id=cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType='D1' AND kd.PointKind='Arudha'"), 12);
        // channel integrity: AL's D9 sign == NavamsaD9 rule applied to AL's D1 longitude
        var alD1Lon = conn.ExecuteScalar<double>(
            @"SELECT kd.NirayanaLongitudeDegrees FROM dbo.tbl_Chart_KeyDetails kd
              JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId
              JOIN dbo.tbl_BirthDetails bd ON bd.Id=cr.BirthDetailId
              WHERE bd.Name='Ramakrishnan' AND cr.ChartType='D1' AND kd.Planet='AL'");
        var expectedD9 = VargaSignRuleFactory.For("NavamsaD9", 9).SignFor(alD1Lon).ToString();
        Check("AL D9 channel integrity", SpSign("D9", "AL"), expectedD9);
    }
```

- [ ] **Step 10: `verify-schema` — non-Graha-nulls rule**

```csharp
Check("non-Graha KeyDetails carry no graha-only analytics",
    conn.ExecuteScalar<long>(@"
        SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails
        WHERE PointKind <> 'Graha' AND (PlanetId IS NOT NULL OR DignityStatus IS NOT NULL
            OR Nakshatra IS NOT NULL OR CharaKaraka IS NOT NULL OR AspectingPlanets IS NOT NULL
            OR IsCombust IS NOT NULL OR NakshatraLordPlanet IS NOT NULL)"), 0L);
```

- [ ] **Step 11: Build, regenerate, verify**

```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- recompute-keydetails
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-jaimini
for m in verify-schema verify-vargas verify-avastha verify-functional-nature; do dotnet run --project src/Ikiastrro.Cli --no-build -- $m | tail -1; done
```
Expected: build 0/0; `verify-jaimini` sunrise + karakas + AL + A2–A12 + channel integrity
PASS; all other `verify-*` ALL PASS. `recompute-keydetails` idempotent (run twice → same).

- [ ] **Step 12: Commit**

```bash
git add src/Ikiastrro.Core/SpecialPoints src/Ikiastrro.Core/Models/PlanetPosition.cs src/Ikiastrro.Core/Calculators src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): Arudha Lagna + 12 Bhava Arudhas through all 21 vargas\n\nspecial-point seed channel: orchestrator computes D1 longitudes once,\nSpecialPointProjector applies each chart IVargaSignRule, ChartAnalyzer\nemits position-only rows (PointKind <> Graha).\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 5: Hora Lagna + Gulika + Maandi

**Files:**
- Create: `src/Ikiastrro.Core/SpecialPoints/HoraLagnaCalculator.cs`,
  `.../UpagrahaCalculator.cs`
- Modify: `src/Ikiastrro.Core/SpecialPoints/SpecialPointCalculator.cs`,
  `src/Ikiastrro.Cli/Program.cs` (`verify-jaimini`)

**Interfaces:**
- Consumes: `SwissEphemerisProvider.GetSunTimes` (Task 2), `GetSiderealPositions` (Sun's
  sidereal longitude at sunrise), `BirthDetails`.
- Produces: `HoraLagnaCalculator.Compute(BirthDetails, SunTimes) : SpecialPointSeed` (code
  `"HL"`, kind `"SpecialLagna"`); `UpagrahaCalculator.Compute(BirthDetails, SunTimes) :
  (SpecialPointSeed Gulika, SpecialPointSeed Maandi)` (kind `"Upagraha"`).

- [ ] **Step 1: `verify-jaimini` — add the HL/Gulika/Maandi asserts (will FAIL)**

In the special-points DB block:
```csharp
        Check("HL (D1) -> Pisces",       SpSign("D1", "HL"),     "Pisces");
        Check("HL (D9) -> Aquarius",     SpSign("D9", "HL"),     "Aquarius");
        Check("Gulika (D1) -> Libra",    SpSign("D1", "Gulika"), "Libra");
        Check("Maandi (D1) -> Libra",    SpSign("D1", "Maandi"), "Libra");
```

- [ ] **Step 2: `HoraLagnaCalculator.cs`**

```csharp
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>
/// Hora Lagna: coincides with the Lagna longitude at the reference sunrise and advances
/// 30 degrees per hour of elapsed time since it. For a night birth the reference sunrise is
/// the one immediately before birth (SunTimes.PriorSunrise). Verified against
/// scratch/Rammy_Jagannatha.txt: 23 Pi 55' 08" (Pisces; Navamsa Aquarius).
/// </summary>
public static class HoraLagnaCalculator
{
    public static SpecialPointSeed Compute(BirthDetails bd, SwissEphemerisProvider.SunTimes sun)
    {
        var reference = sun.IsNightBirth ? sun.PriorSunrise : sun.Sunrise;
        var birthMoment = BirthMomentFactory.Create(bd);
        var elapsedHours = (birthMoment - reference).TotalHours;

        var atSunrise = SwissEphemerisProvider.GetSiderealPositions(
            reference, bd.Latitude, bd.Longitude);
        var lagnaAtSunrise = atSunrise.AscendantLongitude;

        var hl = AstroMath.Normalize(lagnaAtSunrise + elapsedHours * 30.0);
        return new SpecialPointSeed("HL", "SpecialLagna", hl);
    }
}
```

*(`BirthMomentFactory` is `internal` — either make it `public` or add a
`SpecialPointCalculator`-local helper that rebuilds the `DateTimeOffset` the same way. The
plan's Task 5 Step 3 assumes `BirthMomentFactory` is made `public`; a one-word change,
noted in the commit.)*

- [ ] **Step 3: `UpagrahaCalculator.cs`**

```csharp
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>
/// Gulika and Maandi. The day arc (sunrise->sunset) for a day birth, or the night arc
/// (sunset->next sunrise) for a night birth, is divided into 8 equal parts. The part ruled
/// by Saturn — by the standard weekday day-part / night-part sequence — gives the instant;
/// the Lagna rising then, placed at the natal Lagna's degree-in-sign, is the upagraha.
/// Gulika uses the START of Saturn's part; Maandi the same (JHora prints them a few arc-
/// minutes apart — the exact sub-instant is tuned in Step 5 against Gulika 7 Li 44' /
/// Maandi 18 Li 07'). Verified: both -> Libra for 1_Ramakrishnan.
/// </summary>
public static class UpagrahaCalculator
{
    // day-part rulers, index 0 = the part starting at sunrise, for weekday Sun..Sat (0..6)
    private static readonly int[][] DayParts =
    {
        /* Sun */ new[]{0,6,5,4,3,2,1}, /* Mon */ new[]{1,0,6,5,4,3,2},
        /* Tue */ new[]{2,1,0,6,5,4,3}, /* Wed */ new[]{3,2,1,0,6,5,4},
        /* Thu */ new[]{4,3,2,1,0,6,5}, /* Fri */ new[]{5,4,3,2,1,0,6},
        /* Sat */ new[]{6,5,4,3,2,1,0},
    };
    // night-part rulers start from the lord of the 5th weekday
    private static int[] NightParts(int weekday) => DayParts[(weekday + 4) % 7];
    private const int Saturn = 6;   // weekday index of Saturn's lord row / part ruler

    public static (SpecialPointSeed Gulika, SpecialPointSeed Maandi) Compute(
        BirthDetails bd, SwissEphemerisProvider.SunTimes sun)
    {
        var birth = BirthMomentFactory.Create(bd);
        var weekday = (int)(sun.IsNightBirth ? sun.PriorSunrise : sun.Sunrise).DayOfWeek; // 0=Sun

        DateTimeOffset arcStart, arcEnd;
        int[] parts;
        if (sun.IsNightBirth) { arcStart = sun.PriorSunrise.AddDays(0); arcStart = sun.Sunset.AddDays(-1); arcEnd = sun.Sunrise; parts = NightParts(weekday); }
        else                  { arcStart = sun.Sunrise; arcEnd = sun.Sunset; parts = DayParts[weekday]; }

        var partLen = (arcEnd - arcStart) / 8.0;
        var saturnPartIndex = Array.IndexOf(parts, Saturn);
        var gulikaInstant = arcStart + partLen * saturnPartIndex;          // start of Saturn's part

        SpecialPointSeed Rising(DateTimeOffset t, string code)
        {
            var pos = SwissEphemerisProvider.GetSiderealPositions(t, bd.Latitude, bd.Longitude);
            var asc = bd; // natal lagna degree-in-sign
            var natalLagna = SwissEphemerisProvider.GetSiderealPositions(birth, bd.Latitude, bd.Longitude).AscendantLongitude;
            var lon = AstroMath.Normalize(
                Math.Floor(pos.AscendantLongitude / 30.0) * 30.0 + (natalLagna % 30.0));
            return new SpecialPointSeed(code, "Upagraha", lon);
        }

        return (Rising(gulikaInstant, "Gulika"), Rising(gulikaInstant, "Maandi"));
    }
}
```

*(This is the shape; the day/night arc bounds and whether Maandi uses the end of the part
are finalised in Step 5 against the export. Keep iterating the arc math until both land in
Libra at ~7° and ~18°.)*

- [ ] **Step 4: `SpecialPointCalculator.ComputeSeeds` — add the three**

```csharp
public static IReadOnlyList<SpecialPointSeed> ComputeSeeds(BirthDetails birthDetails)
{
    var d1 = D1ChartComputer.Compute(birthDetails, Array.Empty<SpecialPointSeed>());
    var sun = SwissEphemerisProvider.GetSunTimes(birthDetails);
    var seeds = new List<SpecialPointSeed>();
    seeds.AddRange(ArudhaCalculator.Compute(d1));
    seeds.Add(HoraLagnaCalculator.Compute(birthDetails, sun));
    var (gulika, maandi) = UpagrahaCalculator.Compute(birthDetails, sun);
    seeds.Add(gulika);
    seeds.Add(maandi);
    return seeds;
}
```

- [ ] **Step 5: Build, tune, regenerate, verify**

```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli --no-build -- verify-jaimini
```
Iterate `HoraLagnaCalculator` (reference sunrise, elapsed-time sign) and `UpagrahaCalculator`
(arc bounds, part index, start-vs-end) until `HL -> Pisces/Aquarius`, `Gulika -> Libra`,
`Maandi -> Libra` all PASS. Record the resolved conventions in each class's XML doc.
Then:
```
dotnet run --project src/Ikiastrro.Cli --no-build -- recompute-keydetails
for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini; do dotnet run --project src/Ikiastrro.Cli --no-build -- $m | tail -1; done
```
Expected: all `ALL PASS`; `recompute-keydetails` idempotent.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Core/SpecialPoints src/Ikiastrro.Core/Calculators/BirthMomentFactory.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): Hora Lagna + Gulika + Maandi special points\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 6: Fold migration 14 into the baseline + `vw_Chart_Consolidated` + regenerate

**Files:**
- Modify: `db/ikiastrro.sql`

**Interfaces:**
- Consumes: nothing new. Produces: a from-scratch build that already has `PointKind` +
  `CharaKaraka` + the 3 CHECKs + the 2 view columns.

- [ ] **Step 1: `tbl_Chart_KeyDetails` CREATE TABLE — add the two columns inline**

After `[VargaLongitudeDegrees] [decimal](9, 6) NOT NULL,` add:
```sql
	[PointKind] [varchar](12) NOT NULL CONSTRAINT DF_KeyDetails_PointKind DEFAULT ('Graha'),
	[CharaKaraka] [varchar](4) NULL,
```
and in the constraints block (next to `CK_KeyDetails_VargaLongitude`):
```sql
 CONSTRAINT [CK_KeyDetails_PointKind]     CHECK ([PointKind] IN ('Graha','SpecialLagna','Arudha','Upagraha')),
 CONSTRAINT [CK_KeyDetails_CharaKaraka]   CHECK ([CharaKaraka] IS NULL OR [CharaKaraka] IN ('AK','AmK','BK','MK','PiK','PK','GK','DK')),
 CONSTRAINT [CK_KeyDetails_NonGrahaNulls] CHECK ([PointKind] = 'Graha' OR ([PlanetId] IS NULL AND [DignityStatus] IS NULL AND [Nakshatra] IS NULL AND [CharaKaraka] IS NULL AND [AspectingPlanets] IS NULL AND [IsCombust] IS NULL AND [NakshatraLordPlanet] IS NULL)),
```

- [ ] **Step 2: `vw_Chart_Consolidated` — surface the two columns**

Add `kd.PointKind,` and `kd.CharaKaraka,` to the `SELECT` list (near `kd.VargaLongitudeDegrees`).

- [ ] **Step 3: Update the folded-migrations comment**

In the `tbl_Chart_KeyDetails` header comment, note "folded … + `db/14_add_karaka_and_pointkind.sql`".

- [ ] **Step 4: Scratch-DB rebuild check**

```
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END"
```
Copy `db/ikiastrro.sql` → `db/_scratch_tmp.sql` with `[ikiastrro]`→`[ikiastrro_scratch]` and
`N'ikiastrro'`→`N'ikiastrro_scratch'` (2 refs, top of file only), then
`sqlcmd -S localhost -E -b -i db/_scratch_tmp.sql` (0 `Msg` lines), then:
```
sqlcmd -S localhost -E -d ikiastrro_scratch -Q "SELECT COL_LENGTH('dbo.tbl_Chart_KeyDetails','PointKind') pk, COL_LENGTH('dbo.tbl_Chart_KeyDetails','CharaKaraka') ck, OBJECT_ID('dbo.CK_KeyDetails_NonGrahaNulls') chk, COL_LENGTH('dbo.vw_Chart_Consolidated','CharaKaraka') vck"
```
Expected: all four non-NULL. Then drop `ikiastrro_scratch` + delete `db/_scratch_tmp.sql`.

- [ ] **Step 5: Regenerate all 5 people + full sweep**

```
for n in Ramakrishnan Ramya Ananya Sundari Gobli; do dotnet run --project src/Ikiastrro.Cli --no-build -- compute-all "$n" | tail -1; done
for m in verify-schema verify-vargas verify-avastha verify-functional-nature verify-jaimini; do dotnet run --project src/Ikiastrro.Cli --no-build -- $m | tail -1; done
```
Expected: all `ALL PASS`; every person shows special-point + karaka rows.

- [ ] **Step 6: Commit**

```bash
git add db/ikiastrro.sql
git commit -m "$(printf 'chore(db): fold migration 14 (PointKind + CharaKaraka) into the baseline\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 7: `--planet-*` tokens + minimal `PlanetChip` + `CombinedD1D9Grid`

**Files:**
- Modify: `src/Ikiastrro.Web/wwwroot/css/tokens.css`
- Create: `src/Ikiastrro.Web/Components/Shared/PlanetChip.razor` (+ `.razor.css`),
  `src/Ikiastrro.Web/Components/Charts/CombinedD1D9Grid.razor` (+ `.razor.css`)
- Modify: `src/Ikiastrro.Web/Components/Pages/ChartWorkspace.razor`

**Interfaces:**
- Consumes: `ChartKeyDetail` rows (grahas + `PointKind<>'Graha'`) for D1 and D9 of one
  person; `ChartViewModel.PlanetGlyph`.
- Produces: `PlanetChip` component (`[Parameter] string Planet`); `CombinedD1D9Grid`
  (`[Parameter] IReadOnlyList<ChartKeyDetail> D1KeyDetails, D9KeyDetails`,
  `[Parameter] string LagnaSign`).

- [ ] **Step 1: `tokens.css` — 9 solids + 9 tints**

Append inside `:root` (values from the dataviz skill's categorical palette; the 9 `-bg` are
each ~20% of the solid mixed into `--paper`):

```css
    --planet-sun:     #d98f2b;  --planet-sun-bg:     #2a2338;
    --planet-moon:    #b9c2d0;  --planet-moon-bg:    #23273a;
    --planet-mars:    #d5544a;  --planet-mars-bg:    #2f1f33;
    --planet-mercury: #5fa96f;  --planet-mercury-bg: #1b2937;
    --planet-jupiter: #d7b34a;  --planet-jupiter-bg: #2a2733;
    --planet-venus:   #6fb0c9;  --planet-venus-bg:   #1c2a3a;
    --planet-saturn:  #7f8bd6;  --planet-saturn-bg:  #202339;
    --planet-rahu:    #9085e9;  --planet-rahu-bg:    #23213c;
    --planet-ketu:    #b07fd0;  --planet-ketu-bg:    #262138;
```

Run the dataviz skill's `validate_palette.js` on the 9 solids (all pairs); paste the summary
into a comment above the block. Verify `--ink` (`#ece2cb`) clears WCAG-AA on every `-bg`
(all are near-`--paper`, so it does — note it in the comment).

- [ ] **Step 2: `PlanetChip.razor` (minimal)**

```razor
@* Sanskrit-primary planet name on its identity-colour fill. Minimal build (Spec 1);
   Spec 2 Phase 1 extends it. *@
<span class="pchip" style="--c: var(--planet-@Key); --cbg: var(--planet-@(Key)-bg)">
    <span class="glyph">@Ikiastrro.Core.Calculators.ChartViewModel.PlanetGlyph(Planet)</span>
</span>

@code {
    [Parameter, EditorRequired] public string Planet { get; set; } = "";
    private string Key => Planet.ToLowerInvariant();
}
```

`PlanetChip.razor.css`:
```css
.pchip { display:inline-flex; align-items:center; gap:.25rem;
    padding:.05rem .3rem; border-radius:.35rem;
    background: var(--cbg); border-left: 2px solid var(--c); color: var(--ink); }
.glyph { font-weight:600; }
```

- [ ] **Step 3: `CombinedD1D9Grid.razor` — write the failing mount first**

In `ChartWorkspace.razor`, in the D1 branch after the `<section class="ppt-section">`, add:
```razor
    <section class="combined-section">
        <h2>D1 &oplus; D9 — combined</h2>
        <CombinedD1D9Grid D1KeyDetails="d1KeyDetailsRaw" D9KeyDetails="d9KeyDetailsRaw" LagnaSign="@d1.AscSign" />
    </section>
```
Build → FAIL (`CombinedD1D9Grid` missing, and the two raw lists not loaded). In
`OnParametersSet`, capture the raw KeyDetail lists for D1 and D9 into two fields
`d1KeyDetailsRaw` / `d9KeyDetailsRaw` (they are already fetched as `keyDetails` inside the
`AllTypes` loop — store the D1 and D9 ones).

- [ ] **Step 4: `CombinedD1D9Grid.razor`**

Reuse `SouthIndianGrid`'s cell geometry: a 4×4 CSS grid, the 12 outer cells fixed to signs
Pisces(0,0)…in the established order, centre 2×2 merged. Each cell:
- body: D1 occupants of that sign as `<PlanetChip>` (mini);
- an absolutely-positioned top strip: D9 occupants of that sign, smaller, `--ink-soft`;
- top-left corner: any special-point codes (`PointKind<>'Graha'`) whose D1 sign is this cell —
  `AL A2..A12 HL Gk Md` — in `--ink-faint`, tiny.
- centre: `Outer: Navamsa` / `Inner: Rasi` / `Lagna @LagnaSign`.

`@media (max-width:560px)` — hide the D9 strip + render a note "narrow view: see D1 and D9
grids above"; the workspace already shows D1, and D9 lives in the varga rail (Spec 2). Keep
all colour via `var(--planet-*)`; own `.razor.css`, no inline styles beyond the CSS-var
plumbing.

Data helpers (in `@code`): `IReadOnlyDictionary<string, List<ChartKeyDetail>> BySign(list)`
grouping `PointKind=='Graha' && Planet!='Ascendant'`; `IEnumerable<string>
SpecialCodesInSign(sign)` from the D1 list where `PointKind!='Graha'`.

- [ ] **Step 5: Build + Web smoke**

```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Web --no-build --urls http://localhost:5199 &   # background
sleep 12 && curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5199/charts/1
```
Expected: `200`. Open `http://localhost:5199/charts/1` in a browser (or
`mcp__claude-in-chrome`): the combined grid shows D1 inner + D9 outer, with `AL` in the
Capricorn cell, `HL` in Pisces, `Gk`+`Md` in Libra — matching `scratch/D1-D9-CombinedChart.png`.
Stop the web process.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Web
git commit -m "$(printf 'feat(web): CombinedD1D9Grid (static) + planet palette tokens + minimal PlanetChip\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 8: Documentation

**Files:** `docs/reference-calculations.md`, `ARCHITECTURE.md`,
`docs/dbdesign-star-schema-rules-engine.md`, `master_ikiastrro.md`, `../ikiastrro.md`,
`C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md`

- [ ] **Step 1: `docs/reference-calculations.md`** — new section "Chara Karakas & special
  points": the 8-karaka algorithm (degree-in-sign desc, Rahu reversed, Ketu excluded); the
  special-point set (AL + A2–A12 + HL + Gulika + Maandi), that they are carried through all
  21 vargas via the same `IVargaSignRule`, and the `PointKind` discriminator; the sunrise
  dependency for HL/Gulika/Maandi; `verify-jaimini` as the check. One row per point with its
  `1_Ramakrishnan` sign.

- [ ] **Step 2: `ARCHITECTURE.md`** — `tbl_Chart_KeyDetails` now also holds special-point
  rows (`PointKind`) and a `CharaKaraka` label on grahas; the static combined D1⊕D9 chart on
  the workspace; note the Jaimini rasi dashas / Karakamsa as still-future.

- [ ] **Step 3: `docs/dbdesign-star-schema-rules-engine.md`** — one line in the Fact-table
  notes: `tbl_Chart_KeyDetails` gained `PointKind` (row discriminator) + `CharaKaraka`;
  `CK_KeyDetails_NonGrahaNulls` keeps non-graha rows position-only.

- [ ] **Step 4: `master_ikiastrro.md`** — spec row → `snapshot (implemented)`; add a Plans
  row for this plan (`snapshot (done)` once merged).

- [ ] **Step 5: `../ikiastrro.md`** — dated `2026-09-01` section: Chara Karakas (Ashta) as a
  KeyDetails column; AL + 12 Bhava Arudhas + HL + Gulika + Maandi as `PointKind` rows through
  all 21 vargas; sunrise via `swe_rise_trans`; migration 14; static `CombinedD1D9Grid`;
  verified against the Ramakrishnan JHora export.

- [ ] **Step 6: Memory** — append ~3 sentences to the varga note in
  `memproj_vedic_horo_gen.md`: Chara Karakas + special points shipped 2026-09-01,
  `PointKind` column, migration 14, combined chart; next is the varga-centric Web UI spec.
  Update the `MEMORY.md` one-liner.

- [ ] **Step 7: Commit**

```bash
git add docs/reference-calculations.md docs/dbdesign-star-schema-rules-engine.md ARCHITECTURE.md master_ikiastrro.md
git commit -m "$(printf 'docs: Chara Karakas + special points + combined D1/D9 chart\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```
(`../ikiastrro.md` and memory are outside the repo — save directly, no commit.)

**PLAN COMPLETE / CHECKPOINT:** `dotnet build Ikiastrro.slnx` 0/0 · all five `verify-*`
(`schema`, `vargas`, `avastha`, `functional-nature`, `jaimini`) ALL PASS ·
`recompute-keydetails` idempotent · baseline rebuilds clean with `PointKind` + `CharaKaraka`
· all 5 people have 8 karaka labels per chart + AL/A2–A12/HL/Gulika/Maandi rows in all 21 ·
the workspace combined grid matches `scratch/D1-D9-CombinedChart.png` for `1_Ramakrishnan`.

---

## Self-Review

**1. Spec coverage**

| Spec section | Task(s) |
|---|---|
| §4 Chara Karakas (algorithm, golden values, storage) | Task 3 |
| §5.1 special-point set | Tasks 4 (Arudhas), 5 (HL/Gulika/Maandi) |
| §5.2 Arudha algorithm + golden AL=Capricorn | Task 4 |
| §5.3 sunrise/sunset (`GetSunTimes`, night birth) | Task 2 |
| §5.4 Hora Lagna | Task 5 |
| §5.5 Gulika & Maandi | Task 5 |
| §5.6 carrying points through vargas (`SpecialPointPosition` channel) | Task 4 |
| §6 migration 14 + `verify-schema` rules + view | Tasks 1 (columns + 2 rule tweaks), 3 (karaka-count rule), 4 (non-Graha rule), 6 (baseline fold + view) |
| §7 `CombinedD1D9Grid` | Task 7 |
| §8 build sequencing | Tasks 1–8 map 1:1 to the spec's 7 phases (spec phase 5 = plan Task 6; spec phase 6 = plan Task 7; spec phase 7 = plan Task 8) |
| §9 verification (`verify-jaimini`, golden record, idempotency) | Tasks 2–6 each end with the sweep; Task 6 the full golden-record regen |
| §10.6 `--planet-*` tokens + minimal `PlanetChip` land in this spec | Task 7 |

No gaps.

**2. Placeholder scan**

- Task 2 Step 4 and Task 5 Steps 2–3 carry code that is explicitly "the shape, tuned in the
  next step against the export" — this is the project's established verify-driven cadence
  (Plan A Task 9 corrected D2-US the same way), not a placeholder: the target values
  (`05:56:39`, `HL → Pisces`, `Gulika/Maandi → Libra`) are concrete and the step says
  iterate until they pass.
- Task 5 Step 2 notes `BirthMomentFactory` must be made `public` — a concrete one-word
  change, called out.
- Task 7 Step 1 palette hexes are provisional pending `validate_palette.js`; the step says
  run it and record the result — the same discipline `tokens.css` already documents for
  `--dasha-*`.
- No "TODO" / "similar to Task N" / bare "add error handling".

**3. Type consistency**

- `SpecialPointSeed(string Code, string PointKind, double NirayanaLongitudeDegrees)` —
  defined Task 4 Step 1, consumed by `SpecialPointProjector` (Task 4 Step 4),
  `ArudhaCalculator` (Step 5), `SpecialPointCalculator` (Step 6), `HoraLagnaCalculator` /
  `UpagrahaCalculator` (Task 5).
- `CharaKarakaCalculator.Assign(IReadOnlyDictionary<PlanetName,double>) :
  IReadOnlyDictionary<PlanetName,CharaKaraka>` — defined Task 3 Step 4, called in
  `verify-jaimini` (Step 2) and `ChartGenerationService.CharaKarakaByPlanet` (Step 7); the
  latter `.ToString()`s both key and value to `IReadOnlyDictionary<string,string>` matching
  `PersistAnalytics`'s new param.
- `ChartAnalysisInput.SpecialPoints` (`IReadOnlyList<PlanetPosition>`, `init`) — added Task 4
  Step 3; written by both computers (Step 7), read by `ChartAnalyzer` (Step 8); `with
  { ChartType = ... }` in `VargaCalculator` preserves it.
- `IChartCalculator.ComputeAnalysisInput(BirthDetails, IReadOnlyList<SpecialPointSeed>?)` —
  interface Task 4 Step 7; both implementers + both orchestrator call sites updated in the
  same step; no other implementer exists (`VimshottariDashaCalculator` is not an
  `IChartCalculator`).
- `SwissEphemerisProvider.SunTimes` — defined Task 2 Step 4; consumed by
  `HoraLagnaCalculator` / `UpagrahaCalculator` (Task 5) and `SpecialPointCalculator` (Task 5
  Step 4).
- `PlanetPosition.PointKind` (default `"Graha"`) — added Task 4 Step 2; set by
  `SpecialPointProjector`; read by `ChartAnalyzer`.
- `ChartKeyDetail.PointKind` / `.CharaKaraka` — added Task 3 Step 5; written by
  `ChartAnalyzer` (Tasks 3, 4) and `ChartGenerationService` (Task 3 Step 7); inserted by
  `ChartKeyDetailsRepository` (Task 3 Step 6); migration column Task 1.
- Migration numbering: `14` only; folded in Task 6 (no `15`).

**4. Ambiguity**

- "Which arc for a night birth" in `UpagrahaCalculator` (Task 5 Step 3) — the code sketch
  has a deliberate `arcStart` reassignment to flag the choice; Step 5 says tune against the
  export. The spec's open question #1 owns this; the plan defers it to the verify loop with
  a concrete target.
- `LinearVargaSignRule(1, 1)` as the D1 identity — verified in the divisional-charts spec
  (`AstroMath.GetVargaLongitude(lon,1) == lon`; `SignFor` returns the rasi sign). Stated in
  Task 4 Step 7.
- `verify-schema` "exactly 8 CharaKaraka rows" counts `PositionChart` rows only — Dasha
  `ChartResults` have no KeyDetails, so they are naturally excluded; the `GROUP BY cr.Id`
  with `WHERE CalculationKind='PositionChart'` is explicit.

Fixed inline: none needed beyond the notes above.

---

## Execution Handoff

Plan complete and saved to
`docs/superpowers/plans/2026-09-01-jaimini-chara-karaka-special-points.md`. Two execution
options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, two-stage review between
   tasks, fast iteration.
2. **Inline Execution** — execute the tasks in this session via `superpowers:executing-plans`,
   batched with checkpoints.

Which approach?
