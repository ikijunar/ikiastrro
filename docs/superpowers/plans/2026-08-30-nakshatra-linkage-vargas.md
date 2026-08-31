# Nakshatra Linkage + D2/D6/D10/D11 Divisional Charts — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Link stored chart data to the classical nakshatra reference tables (name canon + FK columns + sub-lord + a house→rāśi→nakshatra view), and add the D2, D6, D10, D11 divisional charts at the DB/CLI level.

**Architecture:** Two independent work-units. (1) Nakshatra linkage: three pure `AstroMath` helpers, three new nullable columns on `tbl_Chart_KeyDetails` (migration 032), two new columns on `tbl_Nakshatras` (033), one new view (034); existing `recompute-keydetails` re-derives stored rows. (2) Divisional charts: four `IChartCalculator` pairs mirroring the existing D9 pair, one orchestrator registration line, a new generic `backfill-charts` CLI mode; **zero schema changes** because `tbl_ChartResults` + the four analytics tables are already chart-type-generic.

**Tech Stack:** .NET 8 / C# (projects: `VedicHoroGen.Core`, `.Data`, `.Cli`, `.Web` — Web untouched), Dapper, MS SQL Server (Windows Auth, `localhost`, DB `vedic_horo_gen`), SwissEphNet. Build/run via `dotnet build` and `dotnet run --project src/VedicHoroGen.Cli -- <mode>`. Migrations applied with `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\NNN_*.sql`.

**Spec:** `docs/scope-nakshatra-linkage-divisional-charts.md` (relocated from workspace root 2026-08-31)

## Global Constraints

- **DB + CLI only. No Blazor / Web UI changes of any kind.** A new Web UI is a later batch.
- **No timeline chart** this batch (next batch). **No D3/D7/D12/D30** (D12 explicitly de-scoped).
- **Not under git; no test project.** "Checkpoint" steps below are verification gates, not commits. If version control is wanted, run `git init` in the project root first and treat each checkpoint as a commit. Pure-function verification is done by a new `verify-vargas` CLI mode (worked-example assertions); DB verification by SQL cross-join queries; varga charts by external cross-check.
- **Migration numbering:** 030 and 031 are reserved (unapplied) for the house/Lagna significations work. This batch uses **032, 033, 034** only.
- **Varga formulas:** Jagannatha Hora / Traditional Parasara (`chart_method=1`), transcribed verbatim from PyJHora (`naturalstupid/PyJHora`, `src/jhora/horoscope/chart/charts.py`). D2 uses the classical two-sign rule (`_hora_traditional_parasara_chart`), **not** PyJHora's `method=1` "Uma Shambu" variant — see spec §4.1 decision D-1.
- **Nakshatra name canon:** the engine adopts `tbl_Nakshatras.NakshatraName` spellings (migration 021 seed). Enum member names in `ConstellationName` are left unchanged (internal indices only).
- **Sign index convention:** `ZodiacName` is 0-indexed `Aries=0 … Pisces=11` and includes the Latin `Capricornus`. "Odd sign" (classical, 1-indexed) = `signIndex` even.
- **DB object naming:** follow existing `tbl_` / `vw_` conventions (`STANDARDS.md`). No new base tables here, so no `tbl_Dim_`/`tbl_Rule_`/`tbl_Fact_` infix decision.
- **`EngineVersion` string** for every new `ChartResult`: `"SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)"` (identical to D1/D9). `Ayanamsha = "Lahiri"`, `HouseSystem = "WholeSign"`.

---

## File Structure

**Work-unit 2 (divisional charts) — new files:**

| File | Responsibility |
|---|---|
| `src/VedicHoroGen.Core/Calculators/D2HoraChartComputer.cs` | Static `Compute(BirthDetails) → ChartAnalysisInput` for D2 |
| `src/VedicHoroGen.Core/Calculators/D2HoraCalculator.cs` | `IChartCalculator` for D2 (`ChartType="D2"`) |
| `src/VedicHoroGen.Core/Calculators/D6ShashtamsaChartComputer.cs` | D6 computer |
| `src/VedicHoroGen.Core/Calculators/D6ShashtamsaCalculator.cs` | D6 `IChartCalculator` |
| `src/VedicHoroGen.Core/Calculators/D10DasamsaChartComputer.cs` | D10 computer |
| `src/VedicHoroGen.Core/Calculators/D10DasamsaCalculator.cs` | D10 `IChartCalculator` |
| `src/VedicHoroGen.Core/Calculators/D11RudramsaChartComputer.cs` | D11 computer |
| `src/VedicHoroGen.Core/Calculators/D11RudramsaCalculator.cs` | D11 `IChartCalculator` |

**Migrations — new files:**

| File | Responsibility |
|---|---|
| `db/032_add_nakshatra_linkage_to_keydetails.sql` | 3 nullable columns + 2 FKs on `tbl_Chart_KeyDetails` |
| `db/033_add_primary_rasi_to_nakshatras.sql` | `PrimaryRasiId` + `StraddlesSignBoundary` on `tbl_Nakshatras`, backfilled |
| `db/034_create_house_nakshatra_span_view.sql` | `vw_Chart_HouseNakshatraSpan` |

**Modified files:**

| File | Change |
|---|---|
| `src/VedicHoroGen.Core/Astro/AstroMath.cs` | + `GetHoraSign`, `GetShashtamsaSign`, `GetDasamsaSign`, `GetRudramsaSign`, `NakshatraCanonicalNames`, `GetNakshatraName`, `GetOverallPadaIndex`, `GetNakshatraSubLord`, `VimshottariYearsByLord` |
| `src/VedicHoroGen.Core/Astro/ConstellationName.cs` | Doc-comment only (names now index-only) |
| `src/VedicHoroGen.Core/Calculators/D1ChartComputer.cs` | Use `AstroMath.GetNakshatraName(...)` instead of `.ToString()` (2 call sites) |
| `src/VedicHoroGen.Core/Dasha/VimshottariDashaCalculator.cs` | `YearsByLord` → `= AstroMath.VimshottariYearsByLord` (DRY) |
| `src/VedicHoroGen.Core/Models/ChartKeyDetail.cs` | + `byte? NakshatraId`, `int? NakshatraPadaId`, `string? NakshatraSubLordPlanet` |
| `src/VedicHoroGen.Core/Calculators/ChartAnalyzer.cs` | Populate the 3 new fields in the per-planet loop |
| `src/VedicHoroGen.Core/Calculators/ChartCalculationOrchestrator.cs` | Register 4 calculators in `CreateDefault()`; add `Calculators` accessor |
| `src/VedicHoroGen.Data/ChartKeyDetailsRepository.cs` | Add 3 columns to `InsertAll`'s column + values lists |
| `src/VedicHoroGen.Cli/Program.cs` | + `verify-vargas` mode; + `backfill-charts` mode; generalize `recompute-keydetails` filter |
| `db/003_create_d1_keydetails_tables.sql` | Base-DDL sync for the 3 new columns (fresh installs) |
| `db/021_create_nakshatra_reference_tables.sql` | Base-DDL sync for the 2 new `tbl_Nakshatras` columns |
| `README.md`, `D:\@ClaudeSpace\ikiastrro.md`, memory, `methods_prodmag.md`, `coverage_proj_vedic_horo_gen.md` | Docs (Task 9) |

---

## Task 1: `AstroMath` varga sign functions + `verify-vargas` CLI mode

**Files:**
- Modify: `src/VedicHoroGen.Core/Astro/AstroMath.cs`
- Modify: `src/VedicHoroGen.Cli/Program.cs` (new `verify-vargas` mode, placed next to the other one-off modes ~line 80+)

**Interfaces:**
- Produces:
  - `AstroMath.GetHoraSign(double siderealLongitude) : ZodiacName`
  - `AstroMath.GetShashtamsaSign(double siderealLongitude) : ZodiacName`
  - `AstroMath.GetDasamsaSign(double siderealLongitude) : ZodiacName`
  - `AstroMath.GetRudramsaSign(double siderealLongitude) : ZodiacName`
  - CLI: `dotnet run --project src/VedicHoroGen.Cli -- verify-vargas` → prints PASS/FAIL per case, exits `1` if any FAIL.
- Consumes: `AstroMath.Normalize`, `AstroMath.GetSignAtLongitude`, `ZodiacName` (all existing).

- [ ] **Step 1: Add the `verify-vargas` mode with the 16 expected cases (will not compile yet)**

In `src/VedicHoroGen.Cli/Program.cs`, immediately after the `using` block's helper functions and before `Console.WriteLine("=== vedic_horo_gen ===");` is too early (needs `connectionFactory` context is NOT required here). Place it right after the `backfill-analytics` block for consistency with the other `if (args.Length > 0 && args[0] == ...)` one-off modes. Insert:

```csharp
// --- One-off check: `dotnet run -- verify-vargas` ---
// Repeatable worked-example assertions for the pure divisional-chart + nakshatra helpers in
// AstroMath (this solution has no unit-test project). Longitudes are absolute sidereal degrees.
if (args.Length > 0 && args[0] == "verify-vargas")
{
    var failures = 0;
    void Check(string label, object actual, object expected)
    {
        var ok = actual.ToString() == expected.ToString();
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: got {actual}, expected {expected}");
        if (!ok) failures++;
    }

    // D2 Hora — classical two-sign (Sun's hora = Leo, Moon's hora = Cancer)
    Check("D2 Aries 10",  AstroMath.GetHoraSign(10),  ZodiacName.Leo);
    Check("D2 Aries 20",  AstroMath.GetHoraSign(20),  ZodiacName.Cancer);
    Check("D2 Taurus 10", AstroMath.GetHoraSign(40),  ZodiacName.Cancer);
    Check("D2 Taurus 20", AstroMath.GetHoraSign(50),  ZodiacName.Leo);

    // D6 Shashtamsa — odd: Aries..Virgo; even: Libra..Pisces (source sign ignored)
    Check("D6 Aries 2",   AstroMath.GetShashtamsaSign(2),   ZodiacName.Aries);
    Check("D6 Aries 27",  AstroMath.GetShashtamsaSign(27),  ZodiacName.Virgo);
    Check("D6 Taurus 2",  AstroMath.GetShashtamsaSign(32),  ZodiacName.Libra);
    Check("D6 Taurus 27", AstroMath.GetShashtamsaSign(57),  ZodiacName.Pisces);

    // D10 Dasamsa — odd: from sign; even: from 9th sign
    Check("D10 Aries 1",   AstroMath.GetDasamsaSign(1),   ZodiacName.Aries);
    Check("D10 Aries 28",  AstroMath.GetDasamsaSign(28),  ZodiacName.Capricornus);
    Check("D10 Taurus 1",  AstroMath.GetDasamsaSign(31),  ZodiacName.Capricornus);
    Check("D10 Taurus 28", AstroMath.GetDasamsaSign(58),  ZodiacName.Libra);

    // D11 Rudramsa — (12 - signIndex + part) % 12, part width 30/11
    Check("D11 Aries 1",   AstroMath.GetRudramsaSign(1),   ZodiacName.Aries);
    Check("D11 Aries 29",  AstroMath.GetRudramsaSign(29),  ZodiacName.Aquarius);
    Check("D11 Taurus 1",  AstroMath.GetRudramsaSign(31),  ZodiacName.Pisces);
    Check("D11 Taurus 29", AstroMath.GetRudramsaSign(59),  ZodiacName.Capricornus);

    Console.WriteLine(failures == 0 ? "\nverify-vargas: ALL PASS" : $"\nverify-vargas: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

- [ ] **Step 2: Build — expect failure**

Run: `dotnet build src/VedicHoroGen.Cli`
Expected: FAIL, `CS0117: 'AstroMath' does not contain a definition for 'GetHoraSign'` (and the other three).

- [ ] **Step 3: Implement the four functions in `AstroMath.cs`**

Add after `GetNavamsaSign` (keep the same doc-comment style — cite the rule + PyJHora source + worked examples):

```csharp
/// <summary>
/// D2 (Hora) sign — classical two-sign Parasara rule (PyJHora _hora_traditional_parasara_chart):
/// the sign's first/second 15° half maps to the Sun's Hora (Leo) or the Moon's Hora (Cancer).
/// Odd signs: 0-15°→Leo, 15-30°→Cancer. Even signs: reversed. So D2 is always Leo or Cancer.
/// Verified: Aries 10°→Leo, Aries 20°→Cancer, Taurus 10°→Cancer, Taurus 20°→Leo.
/// </summary>
public static ZodiacName GetHoraSign(double siderealLongitude)
{
    var normalized = Normalize(siderealLongitude);
    var signIndex = (int)(normalized / DegreesPerSign);
    var degreesInSign = normalized % DegreesPerSign;
    var half = degreesInSign < 15.0 ? 0 : 1;          // 0 = first half, 1 = second half
    var isOddSign = signIndex % 2 == 0;               // 1-indexed odd == 0-indexed even
    var sunHora = (isOddSign && half == 0) || (!isOddSign && half == 1);
    return sunHora ? ZodiacName.Leo : ZodiacName.Cancer;
}

/// <summary>
/// D6 (Shashtamsa) sign — Traditional Parasara (PyJHora shashthamsa_chart method 1). Sign split
/// into six 5° parts. Odd signs: parts map to Aries..Virgo. Even signs: parts map to
/// Libra..Pisces. The source sign itself does not shift the result. Verified: Aries 2°→Aries,
/// Aries 27°→Virgo, Taurus 2°→Libra, Taurus 27°→Pisces.
/// </summary>
public static ZodiacName GetShashtamsaSign(double siderealLongitude)
{
    var normalized = Normalize(siderealLongitude);
    var signIndex = (int)(normalized / DegreesPerSign);
    var degreesInSign = normalized % DegreesPerSign;
    var part = Math.Min((int)(degreesInSign / 5.0), 5);           // 0..5
    var isOddSign = signIndex % 2 == 0;
    var target = isOddSign ? part : (part + 6) % 12;
    return (ZodiacName)target;
}

/// <summary>
/// D10 (Dasamsa) sign — Traditional Parasara (PyJHora dasamsa_chart method 1). Sign split into
/// ten 3° parts. Odd signs: count parts forward from the sign itself. Even signs: count forward
/// from the 9th sign from it (signIndex + 8). Verified: Aries 1°→Aries, Aries 28°→Capricornus,
/// Taurus 1°→Capricornus, Taurus 28°→Libra.
/// </summary>
public static ZodiacName GetDasamsaSign(double siderealLongitude)
{
    var normalized = Normalize(siderealLongitude);
    var signIndex = (int)(normalized / DegreesPerSign);
    var degreesInSign = normalized % DegreesPerSign;
    var part = Math.Min((int)(degreesInSign / 3.0), 9);           // 0..9
    var isOddSign = signIndex % 2 == 0;
    var target = isOddSign ? (signIndex + part) % 12 : (signIndex + part + 8) % 12;
    return (ZodiacName)target;
}

/// <summary>
/// D11 (Rudramsa) sign — Traditional Parasara / Sanjay Rath (PyJHora rudramsa_chart method 1,
/// the shipped default). Sign split into eleven parts of 30/11°. Target = (12 - signIndex + part)
/// mod 12 for every sign. Verified: Aries 1°→Aries, Aries 29°→Aquarius, Taurus 1°→Pisces,
/// Taurus 29°→Capricornus.
/// </summary>
public static ZodiacName GetRudramsaSign(double siderealLongitude)
{
    var normalized = Normalize(siderealLongitude);
    var signIndex = (int)(normalized / DegreesPerSign);
    var degreesInSign = normalized % DegreesPerSign;
    var part = Math.Min((int)(degreesInSign / (DegreesPerSign / 11.0)), 10);   // 0..10
    var target = (12 - signIndex + part) % 12;
    return (ZodiacName)target;
}
```

- [ ] **Step 4: Build + run — expect all PASS**

Run: `dotnet build src/VedicHoroGen.Cli && dotnet run --project src/VedicHoroGen.Cli -- verify-vargas`
Expected: 16 lines all `[PASS]`, final line `verify-vargas: ALL PASS`, exit 0.

- [ ] **Step 5: Checkpoint** — Task 1 complete (varga sign math verified). Not under git; if versioning, `git add -A && git commit -m "feat(core): add D2/D6/D10/D11 varga sign functions + verify-vargas"`.

---

## Task 2: `AstroMath` nakshatra helpers + name canon wiring

**Files:**
- Modify: `src/VedicHoroGen.Core/Astro/AstroMath.cs`
- Modify: `src/VedicHoroGen.Core/Astro/ConstellationName.cs` (doc-comment)
- Modify: `src/VedicHoroGen.Core/Calculators/D1ChartComputer.cs`
- Modify: `src/VedicHoroGen.Core/Dasha/VimshottariDashaCalculator.cs`
- Modify: `src/VedicHoroGen.Cli/Program.cs` (extend `verify-vargas`)

**Interfaces:**
- Produces:
  - `AstroMath.NakshatraCanonicalNames : IReadOnlyList<string>` (27 entries, index 0→"Ashwini" … 26→"Revati")
  - `AstroMath.GetNakshatraName(ConstellationName n) : string`
  - `AstroMath.GetOverallPadaIndex(double siderealLongitude) : int` (0..107)
  - `AstroMath.GetNakshatraSubLord(double siderealLongitude) : PlanetName`
  - `AstroMath.VimshottariYearsByLord : IReadOnlyDictionary<PlanetName,int>`
- Consumes: `AstroMath.NakshatraLordOrder` (existing), `AstroMath.GetNakshatraIndexAndFractionElapsed` (existing), `DegreesPerNakshatra`, `DegreesPerPada` (existing private consts).

- [ ] **Step 1: Add nakshatra assertions to `verify-vargas` (will not compile yet)**

In `Program.cs`, inside the `verify-vargas` block, before the final summary `Console.WriteLine`, add:

```csharp
    // Nakshatra name canon — must match tbl_Nakshatras.NakshatraName exactly
    Check("Name idx0",  AstroMath.GetNakshatraName(ConstellationName.Aswini),   "Ashwini");
    Check("Name idx7",  AstroMath.GetNakshatraName(ConstellationName.Pushyami), "Pushya");
    Check("Name idx13", AstroMath.GetNakshatraName(ConstellationName.Chitta),   "Chitra");
    Check("Name idx21", AstroMath.GetNakshatraName(ConstellationName.Sravana),  "Shravana");
    Check("Name idx11", AstroMath.GetNakshatraName(ConstellationName.Uttara),   "Uttara Phalguni");

    // Overall pada index (0..107): 3°20' each
    Check("Pada@0",     AstroMath.GetOverallPadaIndex(0),      0);
    Check("Pada@10",    AstroMath.GetOverallPadaIndex(10),     3);   // Ashwini pada 4
    Check("Pada@218.7", AstroMath.GetOverallPadaIndex(218.72), 65);  // Anuradha pada 2 -> slot 16*4+1 = 65

    // Sub-lord — Anuradha (idx 16, lord Saturn), 5.389° into nakshatra -> Venus slot
    Check("Sub@218.72", AstroMath.GetNakshatraSubLord(218.72), PlanetName.Venus);
    Check("Sub@0",      AstroMath.GetNakshatraSubLord(0),      PlanetName.Ketu);   // Ashwini's own lord first
```

- [ ] **Step 2: Build — expect failure** (`CS0117` for `NakshatraCanonicalNames`, `GetNakshatraName`, `GetOverallPadaIndex`, `GetNakshatraSubLord`).

Run: `dotnet build src/VedicHoroGen.Cli`

- [ ] **Step 3: Add `VimshottariYearsByLord` + the nakshatra helpers to `AstroMath.cs`**

Add near `NakshatraLordOrder`:

```csharp
/// <summary>
/// Vimshottari dasha years per planet — the classical 120-year total split. Single source of truth
/// for VimshottariDashaCalculator AND the KP nakshatra sub-lord division (GetNakshatraSubLord),
/// same as NakshatraLordOrder is shared. Ordered map, not positional.
/// </summary>
public static readonly IReadOnlyDictionary<PlanetName, int> VimshottariYearsByLord = new Dictionary<PlanetName, int>
{
    [PlanetName.Ketu] = 7, [PlanetName.Venus] = 20, [PlanetName.Sun] = 6, [PlanetName.Moon] = 10,
    [PlanetName.Mars] = 7, [PlanetName.Rahu] = 18, [PlanetName.Jupiter] = 16, [PlanetName.Saturn] = 19,
    [PlanetName.Mercury] = 17
};

/// <summary>
/// The 27 nakshatra names in sidereal order, spelled to match tbl_Nakshatras.NakshatraName exactly
/// (migration 021 seed). Indexed by the ConstellationName enum's integer value. This — not
/// ConstellationName.ToString() — is what gets persisted to tbl_Chart_KeyDetails.Nakshatra, so the
/// stored value joins cleanly to the reference table.
/// </summary>
public static readonly IReadOnlyList<string> NakshatraCanonicalNames = new[]
{
    "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra", "Punarvasu", "Pushya",
    "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni", "Hasta", "Chitra", "Swati",
    "Vishakha", "Anuradha", "Jyeshtha", "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana",
    "Dhanishta", "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
};

/// <summary>Canonical (reference-table) name for a nakshatra enum value.</summary>
public static string GetNakshatraName(ConstellationName nakshatra) => NakshatraCanonicalNames[(int)nakshatra];

/// <summary>Overall pada index 0-107 (each pada = 3°20'), across the whole zodiac. +1 gives tbl_NakshatraPadas.Id.</summary>
public static int GetOverallPadaIndex(double siderealLongitude) =>
    Math.Min((int)(Normalize(siderealLongitude) / DegreesPerPada), 107);

/// <summary>
/// KP nakshatra sub-lord (Vimshottari level 2) at this longitude. Within the 13°20' nakshatra the
/// nine sub-divisions run in Vimshottari order starting from the nakshatra's own lord, each of
/// width (years/120) x 13°20'. Same construction as tbl_NakshatraSubLords' seed and
/// VimshottariDashaCalculator.FindSlot. Verified: 218.72° (Anuradha, +5.39°) -> Venus.
/// </summary>
public static PlanetName GetNakshatraSubLord(double siderealLongitude)
{
    var normalized = Normalize(siderealLongitude);
    var nakshatraIndex = Math.Min((int)(normalized / DegreesPerNakshatra), 26);
    var degreesIntoNakshatra = normalized - nakshatraIndex * DegreesPerNakshatra;
    var startLordIndex = nakshatraIndex % 9;

    var cumulative = 0.0;
    for (var slot = 0; slot < 9; slot++)
    {
        var lord = NakshatraLordOrder[(startLordIndex + slot) % 9];
        var subWidth = VimshottariYearsByLord[lord] / 120.0 * DegreesPerNakshatra;
        if (degreesIntoNakshatra < cumulative + subWidth || slot == 8)
            return lord;
        cumulative += subWidth;
    }
    throw new InvalidOperationException("Unreachable — 9 sub-lord slots span the full nakshatra.");
}
```

- [ ] **Step 4: DRY — point `VimshottariDashaCalculator` at the shared years map**

In `src/VedicHoroGen.Core/Dasha/VimshottariDashaCalculator.cs`, replace the private `YearsByLord` dictionary literal:

```csharp
    private static readonly IReadOnlyDictionary<PlanetName, int> YearsByLord = new Dictionary<PlanetName, int>
    {
        [PlanetName.Ketu] = 7, [PlanetName.Venus] = 20, [PlanetName.Sun] = 6, [PlanetName.Moon] = 10,
        [PlanetName.Mars] = 7, [PlanetName.Rahu] = 18, [PlanetName.Jupiter] = 16, [PlanetName.Saturn] = 19,
        [PlanetName.Mercury] = 17
    };
```

with:

```csharp
    /// <summary>Shared with AstroMath (single source of truth) — same classical 120-year split used by the KP sub-lord division.</summary>
    private static readonly IReadOnlyDictionary<PlanetName, int> YearsByLord = AstroMath.VimshottariYearsByLord;
```

- [ ] **Step 5: Wire `D1ChartComputer` to the canonical name**

In `src/VedicHoroGen.Core/Calculators/D1ChartComputer.cs`, two call sites:
- `Nakshatra = ascendantNakshatra.ToString(),` → `Nakshatra = AstroMath.GetNakshatraName(ascendantNakshatra),`
- `Nakshatra = nakshatra.ToString(),` → `Nakshatra = AstroMath.GetNakshatraName(nakshatra),`

- [ ] **Step 6: Update the `ConstellationName` doc-comment**

In `src/VedicHoroGen.Core/Astro/ConstellationName.cs`, replace the `<summary>` paragraph about "Spelling deliberately matches VedAstro.Library's own ConstellationName enum..." with:

```csharp
/// <summary>
/// The 27 nakshatras, Aswini-first, in sidereal longitude order (each spans 360/27 = 13°20').
/// Member names are indices only — they are NOT persisted anywhere. The canonical display name
/// (and the value stored in tbl_Chart_KeyDetails.Nakshatra) comes from
/// AstroMath.NakshatraCanonicalNames, which matches tbl_Nakshatras.NakshatraName exactly so stored
/// data joins to the reference table. Member spellings are left in the old VedAstro form to avoid
/// churn; do not rely on ToString().
/// </summary>
```

- [ ] **Step 7: Build + run — expect all PASS**

Run: `dotnet build && dotnet run --project src/VedicHoroGen.Cli -- verify-vargas`
Expected: all 26 checks `[PASS]`, `verify-vargas: ALL PASS`.

- [ ] **Step 8: Checkpoint** — Task 2 complete (nakshatra helpers + name canon).

---

## Task 3: Migration 032 + `ChartKeyDetail` columns + `ChartAnalyzer` population

**Files:**
- Create: `db/032_add_nakshatra_linkage_to_keydetails.sql`
- Modify: `db/003_create_d1_keydetails_tables.sql` (base-DDL sync)
- Modify: `src/VedicHoroGen.Core/Models/ChartKeyDetail.cs`
- Modify: `src/VedicHoroGen.Core/Calculators/ChartAnalyzer.cs`
- Modify: `src/VedicHoroGen.Data/ChartKeyDetailsRepository.cs`

**Interfaces:**
- Consumes: `AstroMath.GetNakshatraIndexAndFractionElapsed` (existing), `AstroMath.GetOverallPadaIndex`, `AstroMath.GetNakshatraSubLord` (Task 2).
- Produces: `tbl_Chart_KeyDetails.NakshatraId` (TINYINT NULL FK `tbl_Nakshatras`), `.NakshatraPadaId` (INT NULL FK `tbl_NakshatraPadas`), `.NakshatraSubLordPlanet` (VARCHAR(10) NULL); `ChartKeyDetail.{NakshatraId,NakshatraPadaId,NakshatraSubLordPlanet}` properties.

- [ ] **Step 1: Write migration `db/032_add_nakshatra_linkage_to_keydetails.sql`**

```sql
-- 032_add_nakshatra_linkage_to_keydetails.sql
-- Links tbl_Chart_KeyDetails rows to the nakshatra reference tables (product_scope_nakshatra_
-- linkage_and_divisional_charts.md, work-unit 1). All three columns NULL-able only to survive the
-- window between this migration and the next `recompute-keydetails` run; every produced row
-- resolves to a nakshatra. NakshatraId + NakshatraSubLordPlanet are populated for every chart type
-- (real-longitude facts); NakshatraPadaId is D1-only (matches the existing NakshatraPada gating).

IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraId') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraPadaId') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraPadaId INT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraSubLordPlanet') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraSubLordPlanet VARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartKeyDetails_Nakshatra')
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD CONSTRAINT FK_ChartKeyDetails_Nakshatra
        FOREIGN KEY (NakshatraId) REFERENCES dbo.tbl_Nakshatras(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartKeyDetails_NakshatraPada')
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD CONSTRAINT FK_ChartKeyDetails_NakshatraPada
        FOREIGN KEY (NakshatraPadaId) REFERENCES dbo.tbl_NakshatraPadas(Id);
GO
```

- [ ] **Step 2: Apply migration + confirm**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\032_add_nakshatra_linkage_to_keydetails.sql`
Then: `sqlcmd -S localhost -E -C -d vedic_horo_gen -h-1 -W -Q "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('tbl_Chart_KeyDetails') AND name LIKE 'Nakshatra%';"`
Expected: `Nakshatra`, `NakshatraPada`, `NakshatraLordPlanet`, `NakshatraId`, `NakshatraPadaId`, `NakshatraSubLordPlanet`.

- [ ] **Step 3: Sync base DDL `db/003_create_d1_keydetails_tables.sql`**

Add the three columns to the `CREATE TABLE ... tbl_Chart_KeyDetails` (or its later-migrated form — search the file for the KeyDetails column list) so a fresh install matches: `NakshatraId TINYINT NULL`, `NakshatraPadaId INT NULL`, `NakshatraSubLordPlanet VARCHAR(10) NULL`, plus the two FK constraint lines. If `003` no longer holds the current shape (it may have been superseded by a later migration's DDL), add a one-line comment pointing to `032` as the authoritative shape and leave `003` as-is — do not fabricate a rewrite.

- [ ] **Step 4: Add the three properties to `ChartKeyDetail.cs`**

After `NakshatraLordPlanet`:

```csharp
/// <summary>FK to tbl_Nakshatras — the nakshatra at NirayanaLongitudeDegrees. Populated for D1 and every varga (real-longitude fact), same as NakshatraLordPlanet.</summary>
public byte? NakshatraId { get; set; }

/// <summary>FK to tbl_NakshatraPadas (Id = overall pada slot + 1). D1 only — matches NakshatraPada's gating.</summary>
public int? NakshatraPadaId { get; set; }

/// <summary>KP nakshatra sub-lord (Vimshottari level 2) planet name, e.g. "Venus". Populated for D1 and every varga, like NakshatraLordPlanet. Name string, not an FK (consistent with SignLordPlanet).</summary>
public string? NakshatraSubLordPlanet { get; set; }
```

- [ ] **Step 5: Populate them in `ChartAnalyzer.Compute`**

In the `foreach (var planet in input.Planets)` loop, `nirayanaLongitude` is already computed. Add these locals just after `nakshatraLordPlanet`:

```csharp
            var nakshatraIndex = AstroMath.GetNakshatraIndexAndFractionElapsed(nirayanaLongitude).NakshatraIndex;
            var nakshatraSubLordPlanet = AstroMath.GetNakshatraSubLord(nirayanaLongitude).ToString();
```

Then in the `keyDetails.Add(new ChartKeyDetail { ... })` initializer, after `NakshatraLordPlanet = nakshatraLordPlanet,`:

```csharp
                NakshatraId = (byte)(nakshatraIndex + 1),
                NakshatraPadaId = isRasiChart ? AstroMath.GetOverallPadaIndex(nirayanaLongitude) + 1 : null,
                NakshatraSubLordPlanet = nakshatraSubLordPlanet,
```

- [ ] **Step 6: Add the columns to `ChartKeyDetailsRepository.InsertAll`**

In the `INSERT INTO dbo.tbl_Chart_KeyDetails (...)` column list, after `NakshatraLordPlanet,` add `NakshatraId, NakshatraPadaId, NakshatraSubLordPlanet,`. In the matching `VALUES (...)` list, after `@NakshatraLordPlanet,` add `@NakshatraId, @NakshatraPadaId, @NakshatraSubLordPlanet,`. (Keep both lists aligned — same relative position.)

- [ ] **Step 7: Build**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 8: Re-derive existing D1/D9 rows and verify by cross-join**

Run: `dotnet run --project src/VedicHoroGen.Cli -- recompute-keydetails`
Then run the verification query:

```sql
-- 0 rows expected: NakshatraId disagreeing with a degree-range lookup
SELECT kd.Id, kd.Planet, kd.ChartType, kd.NakshatraId, n.Id AS ExpectedNakshatraId
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_Nakshatras n
  ON (kd.NirayanaLongitudeDegrees >= n.StartDegree AND kd.NirayanaLongitudeDegrees < n.EndDegree)
  OR (n.Id = 27 AND kd.NirayanaLongitudeDegrees >= n.StartDegree)
WHERE kd.NakshatraId <> n.Id;

-- 0 rows expected: sub-lord disagreeing with tbl_NakshatraSubLords
SELECT kd.Id, kd.Planet, kd.NakshatraSubLordPlanet, p.PlanetName AS ExpectedSubLord
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_NakshatraSubLords sl
  ON kd.NirayanaLongitudeDegrees >= sl.StartDegree AND kd.NirayanaLongitudeDegrees < sl.EndDegree
JOIN dbo.tbl_Planets p ON p.Id = sl.SubLordId
WHERE kd.NakshatraSubLordPlanet <> p.PlanetName;

-- 0 rows expected: D1 NakshatraPadaId wrong, or varga NakshatraPadaId not null
SELECT kd.Id, kd.ChartType, kd.NakshatraPadaId
FROM dbo.tbl_Chart_KeyDetails kd
LEFT JOIN dbo.tbl_NakshatraPadas pd
  ON kd.NirayanaLongitudeDegrees >= pd.StartDegree AND kd.NirayanaLongitudeDegrees < pd.EndDegree
WHERE (kd.ChartType = 'D1' AND (kd.NakshatraPadaId IS NULL OR kd.NakshatraPadaId <> pd.Id))
   OR (kd.ChartType <> 'D1' AND kd.NakshatraPadaId IS NOT NULL);

-- 0 rows expected: stored Nakshatra name not found in the reference table
SELECT DISTINCT kd.Nakshatra
FROM dbo.tbl_Chart_KeyDetails kd
WHERE kd.Nakshatra IS NOT NULL
  AND kd.Nakshatra NOT IN (SELECT NakshatraName FROM dbo.tbl_Nakshatras);
```

Expected: all four queries return **0 rows**.

- [ ] **Step 9: Regression spot-check** — confirm previously-verified facts survive:

```sql
SELECT Planet, Sign, Nakshatra, NakshatraPada, HouseNumberFromLagna, DignityStatus
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd ON bd.Id = kd.BirthDetailId
WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D1' AND kd.Planet IN ('Moon','Rahu','Ketu','Sun');
```
Expected: Moon in Scorpio / **Anuradha** / pada 2 / House 8 / Debilitated; Rahu House 4; Ketu House 10; Sun Aries / Exalted. (Nakshatra spelling now `Anuradha`, `Pushya`, etc. — the canonical forms.)

- [ ] **Step 10: Checkpoint** — Task 3 complete (linkage columns live + verified).

---

## Task 4: Migration 033 — `tbl_Nakshatras` sign columns

**Files:**
- Create: `db/033_add_primary_rasi_to_nakshatras.sql`
- Modify: `db/021_create_nakshatra_reference_tables.sql` (base-DDL sync)

**Interfaces:**
- Produces: `tbl_Nakshatras.PrimaryRasiId` (TINYINT NOT NULL FK `tbl_SignAttributes` after backfill), `.StraddlesSignBoundary` (BIT NOT NULL DEFAULT 0).

- [ ] **Step 1: Write `db/033_add_primary_rasi_to_nakshatras.sql`**

```sql
-- 033_add_primary_rasi_to_nakshatras.sql
-- Gives tbl_Nakshatras its own sign columns (previously only inferable via its 4 pada rows).
-- PrimaryRasiId = sign of the nakshatra's midpoint. StraddlesSignBoundary = 1 when the first and
-- last pada fall in different signs (the 9 boundary-spanning nakshatras).

IF COL_LENGTH('dbo.tbl_Nakshatras', 'PrimaryRasiId') IS NULL
    ALTER TABLE dbo.tbl_Nakshatras ADD PrimaryRasiId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Nakshatras', 'StraddlesSignBoundary') IS NULL
    ALTER TABLE dbo.tbl_Nakshatras ADD StraddlesSignBoundary BIT NOT NULL CONSTRAINT DF_Nakshatras_Straddles DEFAULT 0;
GO

UPDATE n
SET PrimaryRasiId = FLOOR(((n.StartDegree + n.EndDegree) / 2.0) / 30.0) + 1
FROM dbo.tbl_Nakshatras n;
GO

UPDATE n
SET StraddlesSignBoundary = CASE WHEN p1.RasiId <> p4.RasiId THEN 1 ELSE 0 END
FROM dbo.tbl_Nakshatras n
JOIN dbo.tbl_NakshatraPadas p1 ON p1.NakshatraId = n.Id AND p1.PadaNumber = 1
JOIN dbo.tbl_NakshatraPadas p4 ON p4.NakshatraId = n.Id AND p4.PadaNumber = 4;
GO

ALTER TABLE dbo.tbl_Nakshatras ALTER COLUMN PrimaryRasiId TINYINT NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Nakshatras_PrimaryRasi')
    ALTER TABLE dbo.tbl_Nakshatras
        ADD CONSTRAINT FK_Nakshatras_PrimaryRasi
        FOREIGN KEY (PrimaryRasiId) REFERENCES dbo.tbl_SignAttributes(Id);
GO
```

- [ ] **Step 2: Apply + verify the 9 straddlers**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\033_add_primary_rasi_to_nakshatras.sql`
Then:

```sql
SELECT Id, NakshatraName, PrimaryRasiId, StraddlesSignBoundary
FROM dbo.tbl_Nakshatras WHERE StraddlesSignBoundary = 1 ORDER BY Id;
```
Expected: exactly 9 rows — Krittika (3), Mrigashira (5), Punarvasu (7), Uttara Phalguni (12), Chitra (14), Vishakha (16), Uttara Ashadha (21), Dhanishta (23), Purva Bhadrapada (25).
Also: `SELECT COUNT(*) FROM dbo.tbl_Nakshatras WHERE PrimaryRasiId IS NULL;` → 0.

- [ ] **Step 3: Sync base DDL `db/021_create_nakshatra_reference_tables.sql`**

In the `CREATE TABLE tbl_Nakshatras (...)` add `PrimaryRasiId TINYINT NULL` and `StraddlesSignBoundary BIT NOT NULL DEFAULT 0` (leave NULL-able in the CREATE; the same file's seed block can be followed by the two `UPDATE` statements from Step 1 + the `ALTER ... NOT NULL` so a fresh install ends in the same state). Add a short comment referencing migration 033.

- [ ] **Step 4: Checkpoint** — Task 4 complete.

---

## Task 5: Migration 034 — `vw_Chart_HouseNakshatraSpan`

**Files:**
- Create: `db/034_create_house_nakshatra_span_view.sql`

**Interfaces:**
- Consumes: `tbl_Chart_HouseLords.HouseSign`, `tbl_SignAttributes.ZodiacEnumValue`, `tbl_NakshatraPadas`, `tbl_Nakshatras`, `tbl_Planets`.
- Produces: `vw_Chart_HouseNakshatraSpan` (columns listed below).

- [ ] **Step 1: Write `db/034_create_house_nakshatra_span_view.sql`**

```sql
-- 034_create_house_nakshatra_span_view.sql
-- House-centric House -> Rasi -> Nakshatra view: per chart, per house, the sign occupying it and
-- the 9 nakshatra-padas spanning that sign's 30 degrees. Whole-sign, so every house is exactly one
-- sign = 9 padas (2.25 nakshatras). Joins tbl_Chart_HouseLords.HouseSign to
-- tbl_SignAttributes.ZodiacEnumValue (the VedAstro enum form, e.g. "Capricornus"), NOT SignName.

IF OBJECT_ID('dbo.vw_Chart_HouseNakshatraSpan', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Chart_HouseNakshatraSpan;
GO

CREATE VIEW dbo.vw_Chart_HouseNakshatraSpan AS
SELECT
    hl.ChartResultId,
    hl.BirthDetailId,
    hl.ChartType,
    hl.HouseNumber,
    hl.HouseSign,
    sa.Id                      AS HouseSignId,
    hl.LordPlanet,
    n.Id                       AS NakshatraId,
    n.NakshatraName,
    p.PadaNumber,
    p.StartDegree              AS PadaStartDegree,
    p.EndDegree                AS PadaEndDegree,
    lord.PlanetName            AS NakshatraLordName,
    nav.SignName               AS NavamsaSignName
FROM dbo.tbl_Chart_HouseLords hl
JOIN dbo.tbl_SignAttributes  sa   ON sa.ZodiacEnumValue = hl.HouseSign
JOIN dbo.tbl_NakshatraPadas  p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                                 AND p.StartDegree <  sa.Id * 30.0
JOIN dbo.tbl_Nakshatras      n    ON n.Id = p.NakshatraId
JOIN dbo.tbl_Planets         lord ON lord.Id = n.RulingPlanetId
JOIN dbo.tbl_SignAttributes  nav  ON nav.Id = p.NavamsaSignId;
GO
```

- [ ] **Step 2: Apply + verify row count and a spot-check**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\034_create_house_nakshatra_span_view.sql`
Then (rammyps = BirthDetailId 2; adjust if different — find with `SELECT Id,Name FROM tbl_BirthDetails`):

```sql
-- Exactly 108 for a D1 (12 houses x 9 padas). Anything else = a HouseSign that failed to join.
SELECT COUNT(*) FROM dbo.vw_Chart_HouseNakshatraSpan v
JOIN dbo.tbl_ChartResults cr ON cr.Id = v.ChartResultId
WHERE v.BirthDetailId = 2 AND cr.ChartType = 'D1';

-- House 1 (Aries) spot-check
SELECT HouseNumber, HouseSign, NakshatraName, PadaNumber, NakshatraLordName, NavamsaSignName
FROM dbo.vw_Chart_HouseNakshatraSpan v
JOIN dbo.tbl_ChartResults cr ON cr.Id = v.ChartResultId
WHERE v.BirthDetailId = 2 AND cr.ChartType = 'D1' AND v.HouseNumber = 1
ORDER BY v.PadaStartDegree;
```
Expected: count = 108; House 1 rows = Ashwini p1..p4, Bharani p1..p4, Krittika p1 (9 rows), with Ashwini's lord `Ketu`.

- [ ] **Step 3: Checkpoint** — Task 5 complete. Work-unit 1 done.

---

## Task 6: The four divisional-chart calculator pairs + orchestrator registration

**Files:**
- Create: `src/VedicHoroGen.Core/Calculators/D2HoraChartComputer.cs`, `D2HoraCalculator.cs`
- Create: `src/VedicHoroGen.Core/Calculators/D6ShashtamsaChartComputer.cs`, `D6ShashtamsaCalculator.cs`
- Create: `src/VedicHoroGen.Core/Calculators/D10DasamsaChartComputer.cs`, `D10DasamsaCalculator.cs`
- Create: `src/VedicHoroGen.Core/Calculators/D11RudramsaChartComputer.cs`, `D11RudramsaCalculator.cs`
- Modify: `src/VedicHoroGen.Core/Calculators/ChartCalculationOrchestrator.cs`

**Interfaces:**
- Consumes: `AstroMath.GetHoraSign / GetShashtamsaSign / GetDasamsaSign / GetRudramsaSign` (Task 1), `AstroMath.GetVargaLongitude` / `CountFromSignToSign` (existing), `SwissEphemerisProvider.GetSiderealPositions`, `BirthMomentFactory.Create`, `PlanetNames.All9`, `ChartAnalysisInput`, `PlanetPosition` (all existing), `IChartCalculator` (existing).
- Produces: `D2HoraCalculator`, `D6ShashtamsaCalculator`, `D10DasamsaCalculator`, `D11RudramsaCalculator` (each `IChartCalculator`, `ChartType` = `"D2"/"D6"/"D10"/"D11"`); `ChartCalculationOrchestrator.Calculators : IReadOnlyList<IChartCalculator>`.

- [ ] **Step 1: Create `D2HoraChartComputer.cs`**

```csharp
using VedicHoroGen.Core.Astro;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>
/// D2 (Hora) computation — classical two-sign Parasara rule via AstroMath.GetHoraSign (every point
/// lands in Leo or Cancer). Mirrors D9ChartComputer's structure: real longitude + varga longitude
/// (x2, for combustion-within-varga only) + Whole-Sign house from the Hora Lagna. Degree-in-sign
/// and nakshatra/pada are D1-only concepts and left unset.
/// </summary>
public static class D2HoraChartComputer
{
    private const int Divisions = 2;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = AstroMath.GetHoraSign(positions.AscendantLongitude);

        var planetPositions = new List<PlanetPosition>
        {
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = lagnaSign.ToString(),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(positions.AscendantLongitude, Divisions),
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var nirayanaLongitude = positions.PlanetLongitudes[planet];
            var vargaSign = AstroMath.GetHoraSign(nirayanaLongitude);
            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = vargaSign.ToString(),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(nirayanaLongitude, Divisions),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, vargaSign),
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput("D2", lagnaSign, planetPositions);
    }
}
```

- [ ] **Step 2: Create `D2HoraCalculator.cs`**

```csharp
using System.Text.Json;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>D2 (Hora) — wealth. IChartCalculator wrapper over D2HoraChartComputer; mirrors D9NavamsaCalculator.</summary>
public class D2HoraCalculator : IChartCalculator
{
    public string ChartType => "D2";

    public ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails) => D2HoraChartComputer.Compute(birthDetails);

    public ChartResult BuildResult(BirthDetails birthDetails, ChartAnalysisInput analysisInput)
    {
        var resultJson = JsonSerializer.Serialize(new
        {
            VargaLagna = new { Sign = analysisInput.AscendantSign.ToString() },
            Planets = analysisInput.Planets
        }, new JsonSerializerOptions { WriteIndented = true });

        return new ChartResult
        {
            BirthDetailId = birthDetails.Id,
            ChartType = ChartType,
            Ayanamsha = "Lahiri",
            HouseSystem = "WholeSign",
            EngineVersion = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)",
            ResultJson = resultJson,
            ComputedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 3: Create the D6/D10/D11 computers**

Same as Step 1, changed per row of this table (repeat the full class each time — do not abbreviate):

| File / class | `Divisions` | varga fn | `ChartAnalysisInput` tag | doc one-liner |
|---|---|---|---|---|
| `D6ShashtamsaChartComputer` | `6` | `AstroMath.GetShashtamsaSign` | `"D6"` | D6 (Shashtamsa) — health/disease. Odd→Aries..Virgo, even→Libra..Pisces. |
| `D10DasamsaChartComputer` | `10` | `AstroMath.GetDasamsaSign` | `"D10"` | D10 (Dasamsa) — career. Odd from sign, even from 9th sign. |
| `D11RudramsaChartComputer` | `11` | `AstroMath.GetRudramsaSign` | `"D11"` | D11 (Rudramsa) — gains/income. (12 - signIndex + part) % 12. |

- [ ] **Step 4: Create the D6/D10/D11 calculators**

Same as Step 2 per row (repeat the full class each time):

| File / class | `ChartType` | computer | doc one-liner |
|---|---|---|---|
| `D6ShashtamsaCalculator` | `"D6"` | `D6ShashtamsaChartComputer` | D6 (Shashtamsa) — health. |
| `D10DasamsaCalculator` | `"D10"` | `D10DasamsaChartComputer` | D10 (Dasamsa) — career. |
| `D11RudramsaCalculator` | `"D11"` | `D11RudramsaChartComputer` | D11 (Rudramsa) — gains. |

- [ ] **Step 5: Register in `ChartCalculationOrchestrator.cs` + add the `Calculators` accessor**

Replace `CreateDefault()`:

```csharp
    /// <summary>Default orchestrator: D1 + D2 + D6 + D9 + D10 + D11 (numeric order). Register new calculators here.</summary>
    public static ChartCalculationOrchestrator CreateDefault() =>
        new(new IChartCalculator[]
        {
            new D1RasiCalculator(), new D2HoraCalculator(), new D6ShashtamsaCalculator(),
            new D9NavamsaCalculator(), new D10DasamsaCalculator(), new D11RudramsaCalculator()
        });
```

Add after the `_calculators` field:

```csharp
    /// <summary>The registered calculators, in registration order — used by the CLI's backfill-charts / recompute-keydetails to enumerate every chart type that has an IChartCalculator (i.e. everything except VimshottariDasha).</summary>
    public IReadOnlyList<IChartCalculator> Calculators => _calculators;
```

Update the class `<summary>` ("v1 registers D1 + D9 only" → the six).

- [ ] **Step 6: Build**

Run: `dotnet build`
Expected: PASS. (No behaviour change yet for existing data — `CalculateAll` now yields six per new entry, but nothing has been re-run.)

- [ ] **Step 7: Checkpoint** — Task 6 complete (calculators exist + registered).

---

## Task 7: CLI — `backfill-charts` + generalize `recompute-keydetails`

**Files:**
- Modify: `src/VedicHoroGen.Cli/Program.cs`

**Interfaces:**
- Consumes: `ChartCalculationOrchestrator.CreateDefault()` / `.Calculators` (Task 6), `ChartResultsRepository.Insert` (returns row with `.Id`), `ChartKeyDetailsRepository.InsertAll`, `ChartHouseLordsRepository.InsertAll`, `ChartConjunctionsRepository.InsertAll`, `ChartAspectsRepository.InsertAll`, `ChartAnalyzer.Compute` (all existing), `birthDetailsRepo.GetAll()` (existing).
- Produces: CLI modes `backfill-charts` and the generalized `recompute-keydetails`.

- [ ] **Step 1: Add the `backfill-charts` mode**

In `Program.cs`, next to the other one-off modes (after `backfill-analytics` / `recompute-keydetails`), insert:

```csharp
// --- One-off backfill mode: `dotnet run -- backfill-charts` ---
// Creates every registered chart type that a saved person is missing a tbl_ChartResults row for,
// plus its full analytics (KeyDetails/HouseLords/Conjunctions/Aspects) via ChartAnalyzer. Sibling
// of backfill-dasha: idempotent (skips (person, ChartType) pairs that already exist), and generic —
// when D3/D7/D12/... are registered later this picks them up with no change. Use after adding a new
// IChartCalculator (e.g. D2/D6/D10/D11, 2026-08-30).
if (args.Length > 0 && args[0] == "backfill-charts")
{
    var chartResultsRepo = new ChartResultsRepository(connectionFactory);
    var keyDetailsRepo = new ChartKeyDetailsRepository(connectionFactory);
    var houseLordsRepo = new ChartHouseLordsRepository(connectionFactory);
    var conjunctionsRepo = new ChartConjunctionsRepository(connectionFactory);
    var aspectsRepo = new ChartAspectsRepository(connectionFactory);
    var orchestrator = ChartCalculationOrchestrator.CreateDefault();

    var created = 0;
    foreach (var person in birthDetailsRepo.GetAll())
    {
        var existingTypes = chartResultsRepo.GetByBirthDetailId(person.Id).Select(r => r.ChartType).ToHashSet();
        foreach (var calculator in orchestrator.Calculators)
        {
            if (existingTypes.Contains(calculator.ChartType)) continue;

            var input = calculator.ComputeAnalysisInput(person);
            var result = calculator.BuildResult(person, input);
            result = chartResultsRepo.Insert(result);   // populates result.Id

            var (keyDetails, houseLords, conjunctions, aspects) = ChartAnalyzer.Compute(input);
            foreach (var row in keyDetails) { row.ChartResultId = result.Id; row.BirthDetailId = person.Id; }
            foreach (var row in houseLords) { row.ChartResultId = result.Id; row.BirthDetailId = person.Id; }
            foreach (var row in conjunctions) { row.ChartResultId = result.Id; row.BirthDetailId = person.Id; }
            foreach (var row in aspects) { row.ChartResultId = result.Id; row.BirthDetailId = person.Id; }

            keyDetailsRepo.InsertAll(keyDetails);
            houseLordsRepo.InsertAll(houseLords);
            if (conjunctions.Count > 0) conjunctionsRepo.InsertAll(conjunctions);
            if (aspects.Count > 0) aspectsRepo.InsertAll(aspects);

            Console.WriteLine($"Created {person.Name} — {calculator.ChartType} (ChartResultId={result.Id}): " +
                              $"{keyDetails.Count} key-details, {houseLords.Count} house-lords, " +
                              $"{conjunctions.Count} conjunctions, {aspects.Count} aspects.");
            created++;
        }
    }
    Console.WriteLine($"\nDone — created {created} new chart(s).");
    return;
}
```

(If a repo type name differs, confirm against the `backfill-analytics` block above it — it instantiates the same set.)

- [ ] **Step 2: Generalize `recompute-keydetails`'s chart-type filter**

In the existing `recompute-keydetails` block, replace:

```csharp
            if (result.ChartType is not ("D1" or "D9")) continue; // no KeyDetails shape for VimshottariDasha etc.
```

with:

```csharp
            if (!calculableTypes.Contains(result.ChartType)) continue; // skip VimshottariDasha (no IChartCalculator / KeyDetails shape)
```

and add, just after `var orchestratorForRecompute = ChartCalculationOrchestrator.CreateDefault();`:

```csharp
    var calculableTypes = orchestratorForRecompute.Calculators.Select(c => c.ChartType).ToHashSet();
```

Update that block's banner comment: it now re-derives KeyDetails for D1, D2, D6, D9, D10, D11 (every type with a calculator), and picks up the nakshatra-linkage columns + canonical names for all of them.

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: PASS.

- [ ] **Step 4: Run `backfill-charts`**

Run: `dotnet run --project src/VedicHoroGen.Cli -- backfill-charts`
Expected: 4 "Created … — D2/D6/D10/D11" lines per person (≈20 total for 5 people), `Done — created 20 new chart(s).`
Re-run once — expected: `Done — created 0 new chart(s).` (idempotent).

- [ ] **Step 5: Run the generalized `recompute-keydetails`**

Run: `dotnet run --project src/VedicHoroGen.Cli -- recompute-keydetails`
Expected: "Recomputed … — D1/D2/D6/D9/D10/D11" lines; count = 6 × number of people.
Re-run the four cross-join verification queries from Task 3 Step 8 — still **0 rows** each (now also covering D2/D6/D10/D11 rows for `NakshatraId` / sub-lord; `NakshatraPadaId` must be NULL for all non-D1).

- [ ] **Step 6: Checkpoint** — Task 7 complete (both work-units materialized for all saved people).

---

## Task 8: Verification pass — external cross-check of the divisional charts

**Files:** none (verification + a findings note appended in Task 9).

- [ ] **Step 1: Dump rammyps's four new charts**

```sql
SELECT cr.ChartType, kd.Planet, kd.Sign, kd.HouseNumberFromLagna, kd.NakshatraSubLordPlanet
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd ON bd.Id = kd.BirthDetailId
WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType IN ('D2','D6','D10','D11')
ORDER BY cr.ChartType, kd.HouseNumberFromLagna;
```

- [ ] **Step 2: Cross-check against an external JHora-based source**

For the same birth data (22 Apr 1981, 05:30, Chennai; Lahiri; Whole Sign), get D6, D10, D11 for all 9 planets + Lagna from a Jagannatha-Hora-based calculator (JHora desktop, or an online varga calculator that states "Parasara / traditional" method). Get D10 from AstroSage as a second source. Record each source's 10 signs beside the DB values.
Pass condition: every sign matches. Any mismatch → stop, note the planet/chart/expected/actual, and check whether it is (a) a formula transcription error in `AstroMath` (fix + re-run `verify-vargas` and `backfill-charts` after clearing the affected rows), or (b) a documented method difference (record it in the spec's risk table and the history note, do not "fix" to match a different convention).

- [ ] **Step 3: D2 hand-check**

For each of the 10 points, take its D1 degree from `tbl_Chart_KeyDetails` (`ChartType='D1'`, `DegreesInSignDecimal`) and its D1 sign; apply the rule (odd sign 0–15°→Leo / 15–30°→Cancer; even sign reversed); confirm it equals the stored D2 `Sign`.

- [ ] **Step 4: Node-opposition consistency**

```sql
-- Rahu and Ketu must be 6 houses apart (opposite signs) in D10 and D11. Expect the difference to be 6.
SELECT cr.ChartType,
       MAX(CASE WHEN kd.Planet='Rahu' THEN kd.HouseNumberFromLagna END) AS RahuHouse,
       MAX(CASE WHEN kd.Planet='Ketu' THEN kd.HouseNumberFromLagna END) AS KetuHouse
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd ON bd.Id = kd.BirthDetailId
WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType IN ('D10','D11')
GROUP BY cr.ChartType;
```
Expected: `ABS(RahuHouse - KetuHouse) = 6` for D10 and D11. (Not asserted for D2 — 2-sign chart — or D6 — source sign ignored.)

- [ ] **Step 5: `vw_Chart_Consolidated` sanity for a varga**

```sql
SELECT Planet, Sign, DignityStatus, RulesHouseNumbers, ConjunctWith, Aspects
FROM dbo.vw_Chart_Consolidated
WHERE Name = 'Ramakrishnan' AND ChartType = 'D10' ORDER BY Planet;
```
Expected: 10 rows, dignity + rules + conjunct/aspect columns populated coherently (no NULLs where D1/D9 have values).

- [ ] **Step 6: Checkpoint** — record the cross-check results verbatim (they go into the history doc in Task 9). If all pass, the batch is functionally complete.

---

## Task 9: Documentation

**Files:**
- Modify: `D:\@ClaudeSpace\ikiastrro.md`
- Modify: `src/VedicHoroGen.Cli/README.md`? No — Modify: `README.md` (project root)
- Modify: `C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md`
- Modify: `D:\@ClaudeSpace\methods_prodmag.md`
- Modify: `D:\@ClaudeSpace\coverage_proj_vedic_horo_gen.md`

- [ ] **Step 1: `ikiastrro.md` — new dated section**

Append a `## 2026-08-30 — Nakshatra reference linkage + D2/D6/D10/D11 divisional charts` section covering: both work-units and the files touched; the PyJHora source citation + that D2 uses the classical two-sign rule (decision D-1); migrations 032–034 (030/031 still reserved); the `verify-vargas` mode; the Task 8 external cross-check results verbatim; the DRY refactor of `VimshottariDashaCalculator.YearsByLord`.

- [ ] **Step 2: `README.md` updates**

- Varga-support matrix / "current scope": D2, D6, D10, D11 now computed & stored (DB + CLI); no varga UI yet.
- "Extending later": note the pattern has now been exercised for D2/D6/D10/D11; the `Calculators` accessor + `backfill-charts` are the standard way to add + populate a new type.
- "Known limitations": `ChartView.razor` still D1/D9-only — D2/D6/D10/D11 have no UI (deliberate; next batch).
- Key-details section: document `NakshatraId`, `NakshatraPadaId`, `NakshatraSubLordPlanet` (+ the D1-only gating of `NakshatraPadaId`) and the name-canon change.
- Reference-tables section: `tbl_Nakshatras.PrimaryRasiId` / `StraddlesSignBoundary`; new `vw_Chart_HouseNakshatraSpan`.
- CLI section: `verify-vargas`, `backfill-charts`, and that `recompute-keydetails` now covers every calculable chart type.

- [ ] **Step 3: memory `memproj_vedic_horo_gen.md`**

Append one concise paragraph (pointer style, not detail): the nakshatra linkage columns + view, the four new vargas (JHora/Traditional-Parasara, PyJHora-sourced), migrations 032–034, `backfill-charts`; full detail in the 2026-08-30 section of `ikiastrro.md` and `docs/scope-nakshatra-linkage-divisional-charts.md`.

- [ ] **Step 4: `methods_prodmag.md` + `coverage_proj_vedic_horo_gen.md`**

- `methods_prodmag.md`: mark the D6 / D11 (and D2 wealth, D10 career) Opportunity Backlog entries resolved / shipped 2026-08-30.
- `coverage_proj_vedic_horo_gen.md`: point 6 (house-lord in Navamsa) note — D6/D10/D11 now also available for cross-varga reading; health (D6) and wealth (D2/D11) rows updated.

- [ ] **Step 5: Checkpoint** — batch complete.

---

## Self-Review

**1. Spec coverage**

| Spec section | Task(s) |
|---|---|
| §3.1 name canon | Task 2 (steps 3, 5, 6) |
| §3.2 new KeyDetails columns (032) | Task 3 |
| §3.3 ChartAnalyzer population | Task 3 step 5 |
| §3.4 repository column list | Task 3 step 6 |
| §3.5 tbl_Nakshatras sign columns (033) | Task 4 |
| §3.6 vw_Chart_HouseNakshatraSpan (034) | Task 5 |
| §3.7 apply to existing data | Task 3 step 8 (D1/D9) + Task 7 step 5 (all types) |
| §4.1 varga formulas | Task 1 step 3 |
| §4.2 VargaLongitudeDegrees | Task 6 (each computer, `GetVargaLongitude(..., Divisions)`) |
| §4.3 calculator classes | Task 6 steps 1–4 |
| §4.4 registration | Task 6 step 5 |
| §4.5 composition with WU1 | Task 7 step 5 verification |
| §5.1 backfill-charts | Task 7 step 1 |
| §5.2 generalize recompute-keydetails | Task 7 step 2 |
| §5.3 verify-vargas | Task 1 step 1 + Task 2 step 1 |
| §6 migrations 032–034 + base DDL sync | Tasks 3 (003), 4 (021), 5 |
| §7 verification | Tasks 3, 5, 7, 8 |
| §8 docs | Task 9 |
| DRY: VimshottariYearsByLord shared | Task 2 step 4 |

No gaps.

**2. Placeholder scan** — Task 6 steps 3–4 use tables to avoid repeating three near-identical classes verbatim, but explicitly instruct "repeat the full class each time — do not abbreviate" and give every varying value; the full template is in steps 1–2. Task 8 step 2's "expected values" come from the executor running an external tool — the pass condition (every sign matches; classify any mismatch as transcription-error vs method-difference) is concrete. No "TBD"/"handle edge cases"/"similar to Task N" left.

**3. Type consistency**
- `GetHoraSign/GetShashtamsaSign/GetDasamsaSign/GetRudramsaSign` — `double → ZodiacName`, used identically in Task 1 (verify), Task 6 (computers).
- `NakshatraId` is `byte?` on the model (Task 3 step 4) and cast `(byte)(nakshatraIndex + 1)` (Task 3 step 5); column is `TINYINT` (Task 3 step 1) — consistent.
- `GetOverallPadaIndex : int` (Task 2) → `NakshatraPadaId` `int?` / `INT` — consistent.
- `GetNakshatraSubLord : PlanetName` (Task 2) → `.ToString()` → `NakshatraSubLordPlanet` `string?` / `VARCHAR(10)` — consistent (planet names ≤ 7 chars).
- `ChartCalculationOrchestrator.Calculators` — defined Task 6 step 5, consumed Task 7 steps 1–2.
- `ChartResultsRepository.Insert` returns the record with `.Id` set (confirmed in the existing repo) — relied on in Task 7 step 1.
- `ChartAnalysisInput(string, ZodiacName, List<PlanetPosition>)` — matches the existing record; Task 6 computers pass `("D2", lagnaSign, planetPositions)` etc.

No inconsistencies found.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-30-nakshatra-linkage-vargas.md` (relocated from workspace root 2026-08-31). Two execution options:

1. **Subagent-Driven (recommended)** — a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
