# Product scope: Nakshatra reference linkage + D2/D6/D10/D11 divisional charts

**Project:** vedic_horo_gen
**Date:** 2026-08-30
**Status:** Design approved in chat; spec pending user review before implementation plan.
**Scope discipline:** DB + CLI only. A completely new Web UI is a later, separate batch — no
Blazor changes here. Timeline chart (4000-week grid overlaid with Sade Sati / Kantaka / Ashtama
Shani) is explicitly the **next** batch, not this one.

Related docs: `ikiastrro.md` (running history), `README.md` (current architecture),
`product_scope_vedic_reference_tables.md` (the nakshatra reference tables this links to),
`STANDARDS.md` (naming), `methods_prodmag.md` (Opportunity Backlog — D6/D11 entries close here).

---

## 1. Problem statement

### 1a. Nakshatra linkage gap (work-unit 1)

The **House → Rāśi → Nakshatra** chain is only partly represented, and chart data cannot join to
the classical nakshatra reference tables:

- **Planet-centric chain works**: `tbl_Chart_KeyDetails` already carries, per planet,
  `HouseNumberFromLagna` + `Sign` + `Nakshatra` + `NakshatraPada` + `NakshatraLordPlanet` +
  `SignLordPlanet`.
- **House-centric chain stops at Rāśi**: `tbl_Chart_HouseLords` gives `HouseNumber → HouseSign →
  LordPlanet`, but nothing decomposes a house/sign into the nakshatra-padas spanning it.
- **Reference ↔ chart is disconnected**: `tbl_Chart_KeyDetails.Nakshatra` is free-text `varchar`,
  not an FK, **and the spelling diverges** — the engine writes VedAstro-style forms (`Aswini`,
  `Chitta`, `Pushyami`, `Sravana`, `Uttara`, `Vishhaka`, `Poorvabhadra`…) while
  `tbl_Nakshatras.NakshatraName` holds the standard forms (`Ashwini`, `Chitra`, `Pushya`,
  `Shravana`, `Uttara Phalguni`, `Vishakha`, `Purva Bhadrapada`…). A join is impossible without a
  name-normalisation step.
- **`tbl_Nakshatras` has no direct sign column** — a whole nakshatra's Rāśi is only inferable via
  its four `tbl_NakshatraPadas` rows or by degree arithmetic.
- **No KP sub-lord is stored per planet** — `tbl_NakshatraSubLords` (243 rows) exists as reference
  only; the Sign → Star → Sub chain is not materialised for any chart.

### 1b. Missing divisional charts (work-unit 2)

The app computes only **D1 (Rasi)** and **D9 (Navamsa)**. The four classical life-area vargas are
absent:

| Life area | Chart(s) | Have? |
|---|---|---|
| Health / body | D1 Rasi, **D6 Shashtamsa** | D1 ✅ · D6 ❌ |
| Money / wealth | **D2 Hora**, **D11 Rudramsa** | ❌ ❌ |
| Relationships / marriage | D9 Navamsa | ✅ (nothing to add) |
| Career / profession | **D10 Dasamsa** | ❌ |

The `IChartCalculator` extension point (README "Extending later") is designed for exactly this and
has one working precedent (D9). No architectural change is required — only new calculator classes
and one orchestrator registration line.

---

## 2. Goals / non-goals

**Goals**

1. `tbl_Chart_KeyDetails.Nakshatra` stores the canonical `tbl_Nakshatras.NakshatraName` spelling,
   sourced from a single place.
2. Real FK links from `tbl_Chart_KeyDetails` to `tbl_Nakshatras` and `tbl_NakshatraPadas`.
3. `NakshatraSubLordPlanet` populated per planet (KP level-2 sub-lord).
4. `tbl_Nakshatras` carries its own `PrimaryRasiId` + `StraddlesSignBoundary`.
5. A house-centric view: `vw_Chart_HouseNakshatraSpan` — per chart, per house, the Rāśi and the
   nakshatra-padas spanning it.
6. D2, D6, D10, D11 computed and stored for every person (new entries automatically; existing
   people via a CLI backfill), each with the full shared analytics (`tbl_Chart_KeyDetails` /
   `HouseLords` / `Conjunctions` / `Aspects`).
7. All varga formulas match **Jagannatha Hora / Traditional Parasara** (`chart_method=1`), sourced
   verbatim from PyJHora (PVR Narasimha Rao's reimplementation).

**Non-goals**

- No Web UI / Blazor changes of any kind.
- No D3, D7, D12, D30, or any other varga this batch. D12 (parents) was named earlier then
  de-scoped when the request narrowed to the four life-areas; it is a trivial later addition via
  the same pattern.
- No timeline chart (next batch).
- No wiring of the `tbl_Rule_*` / reference tables into the live calculation engine — the engine
  keeps its hardcoded classical lookups; this batch only *links* stored results to reference data,
  it does not make the engine *read* reference data.
- D9 `Nakshatra` / `NakshatraPada` name/number stay D1-only (unchanged existing decision).

---

## 3. Work-unit 1 — Nakshatra reference linkage

### 3.1 Name canon (engine → reference spellings)

The `ConstellationName` enum cannot express canonical names (spaces, e.g. `Purva Phalguni`), so the
fix is a name map, not an enum rename:

- **`AstroMath.NakshatraCanonicalNames`** — `IReadOnlyList<string>`, 27 entries, **exactly** equal
  to `tbl_Nakshatras.NakshatraName` ordered by `Id` (1→27). This is the single C#-side source of
  truth for the display name.
- **`AstroMath.GetNakshatraName(ConstellationName n)`** → `NakshatraCanonicalNames[(int)n]`.
- **`D1ChartComputer`** (lines ~30, ~49): replace `nakshatra.ToString()` /
  `ascendantNakshatra.ToString()` with `AstroMath.GetNakshatraName(...)`. These two call sites are
  the only places the nakshatra name string is produced.
- **`ConstellationName.cs`**: doc comment updated — member names are now index-only and no longer
  persisted anywhere; canonical strings live in `AstroMath.NakshatraCanonicalNames`. Enum member
  names themselves left unchanged (internal only; no churn, no risk).

Canonical list (must match migration 021 seed verbatim):

```
1  Ashwini          10 Magha             19 Mula
2  Bharani          11 Purva Phalguni    20 Purva Ashadha
3  Krittika         12 Uttara Phalguni   21 Uttara Ashadha
4  Rohini           13 Hasta             22 Shravana
5  Mrigashira       14 Chitra            23 Dhanishta
6  Ardra            15 Swati             24 Shatabhisha
7  Punarvasu        16 Vishakha          25 Purva Bhadrapada
8  Pushya           17 Anuradha          26 Uttara Bhadrapada
9  Ashlesha         18 Jyeshtha          27 Revati
```

### 3.2 New columns on `tbl_Chart_KeyDetails` — migration `032`

| Column | Type | Null? | FK | Populated for | Derivation |
|---|---|---|---|---|---|
| `NakshatraId` | TINYINT | NULL* | → `tbl_Nakshatras(Id)` | **D1 and every varga** | `nakshatraIndex + 1` from `AstroMath.GetNakshatraIndexAndFractionElapsed(NirayanaLongitudeDegrees)` |

\* Every produced row resolves to a nakshatra, so `NakshatraId` is effectively NOT NULL going
forward; the column is declared NULL-able only to survive the window between applying migration 032
and running `recompute-keydetails` (same pattern as migration 015's added columns).
| `NakshatraPadaId` | INT | NULL | → `tbl_NakshatraPadas(Id)` | **D1 only** | `overallPadaIndex + 1` (0..107 → 1..108) |
| `NakshatraSubLordPlanet` | VARCHAR(10) | NULL | — (name string, like `SignLordPlanet`) | **D1 and every varga** | `AstroMath.GetNakshatraSubLord(NirayanaLongitudeDegrees).ToString()` |

**Gating rule (consistent with existing design):** *nakshatra identity, its lord, and its
sub-lord are facts of the real ecliptic longitude → populated for every chart type. Pada, pada-FK,
and the display name/number are D1-only presentation fields.* This matches how `NakshatraLordPlanet`
is already treated (non-gated) vs. `Nakshatra`/`NakshatraPada` (D1-only).

**Identity-arithmetic assumptions** (documented in code + asserted in verification):
- `tbl_Nakshatras.Id == SequenceNumber` (1..27, seed order) → `NakshatraId = index + 1`.
- `tbl_NakshatraPadas.Id == slot + 1` where `slot = signIndex*9 + segmentIndex = int(lon / (30/9))`
  overall (the IDENTITY column was populated in exact slot order by migration 021).
- If either assumption is ever broken, `ChartAnalyzer` must switch to a repository lookup by degree
  range. For now the arithmetic is kept (keeps `ChartAnalyzer` a pure function) and every produced
  row is cross-checked against an independent degree-range join during verification.

**New `AstroMath` members:**
- `GetOverallPadaIndex(double siderealLongitude) : int` → `(int)(Normalize(lon) / DegreesPerPada)`,
  clamped to 107.
- `GetNakshatraSubLord(double siderealLongitude) : PlanetName` — KP level-2 sub-lord. Within the
  13°20′ nakshatra, nine sub-divisions follow Vimshottari order **starting from the nakshatra's own
  lord**, each of width `(planetVimshottariYears / 120) × 13°20′`. Reuses
  `AstroMath.NakshatraLordOrder` (already present) + a Vimshottari-years lookup (Ketu 7, Venus 20,
  Sun 6, Moon 10, Mars 7, Rahu 18, Jupiter 16, Saturn 19, Mercury 17). Identical construction to
  `tbl_NakshatraSubLords`' seed comment in migration 021 — verification cross-checks the C# output
  against that table's degree ranges for every stored row (must match exactly).

### 3.3 `ChartAnalyzer` changes

In the per-planet loop (`ChartAnalyzer.Compute`), add to each `ChartKeyDetail`:

```csharp
NakshatraId            = (byte)(AstroMath.GetNakshatraIndexAndFractionElapsed(nirayanaLongitude).NakshatraIndex + 1),
NakshatraPadaId        = isRasiChart ? AstroMath.GetOverallPadaIndex(nirayanaLongitude) + 1 : null,
NakshatraSubLordPlanet = AstroMath.GetNakshatraSubLord(nirayanaLongitude).ToString(),
```

`ChartKeyDetail` model gets the three matching properties (`byte? NakshatraId`,
`int? NakshatraPadaId`, `string? NakshatraSubLordPlanet`) with XML-doc comments stating the gating
rule.

### 3.4 `ChartKeyDetailsRepository` changes

`InsertAll` has an explicit column list — add `NakshatraId, NakshatraPadaId, NakshatraSubLordPlanet`
to both the `INSERT (...)` list and the `VALUES (@...)` list. `GetByChartResultId` uses `SELECT *`
so the read path maps automatically once the model has the properties.

### 3.5 `tbl_Nakshatras` sign columns — migration `033`

| Column | Type | Null? | FK | Meaning |
|---|---|---|---|---|
| `PrimaryRasiId` | TINYINT | NULL→NOT NULL after backfill | → `tbl_SignAttributes(Id)` | Rāśi containing the nakshatra's **midpoint** `((StartDegree+EndDegree)/2)` |
| `StraddlesSignBoundary` | BIT | NOT NULL default 0 | — | 1 when the nakshatra's first and last pada have different `RasiId` |

Populated by `UPDATE` from `tbl_NakshatraPadas` (pure data, no code). The nine straddlers must come
out flagged: Krittika, Mrigashira, Punarvasu, Uttara Phalguni, Chitra, Vishakha, Uttara Ashadha,
Dhanishta, Purva Bhadrapada. Base DDL (migration 021) updated in parallel for fresh installs.

### 3.6 `vw_Chart_HouseNakshatraSpan` — migration `034`

Per chart (`ChartResultId`), per house (1..12): the house's Rāśi and the nakshatra-padas spanning
its 30° window. Whole-sign, so each house = exactly one sign = 9 padas (2.25 nakshatras).

Join path:
```
tbl_Chart_HouseLords hl
  JOIN tbl_SignAttributes sa   ON sa.ZodiacEnumValue = hl.HouseSign      -- handles "Capricornus" vs "Capricorn"
  JOIN tbl_NakshatraPadas p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                              AND p.StartDegree <  (sa.Id) * 30.0
  JOIN tbl_Nakshatras n        ON n.Id = p.NakshatraId
  JOIN tbl_Planets lord        ON lord.Id = n.RulingPlanetId
  JOIN tbl_SignAttributes nav  ON nav.Id = p.NavamsaSignId
```
`hl.HouseSign` is the VedAstro enum form (`Capricornus`, etc.); `tbl_SignAttributes.ZodiacEnumValue`
already stores that exact form (`SignName` stores the standard `Capricorn`), so the join keys on
`ZodiacEnumValue`.

Output columns: `ChartResultId, BirthDetailId, ChartType, HouseNumber, HouseSign, HouseSignId,
LordPlanet, NakshatraId, NakshatraName, PadaNumber, PadaStartDegree, PadaEndDegree,
NakshatraLordName, NavamsaSignName`.

Ordering: `HouseNumber, PadaStartDegree`. For a D1 this yields exactly 108 rows; D2 yields only the
Cancer/Leo houses' spans (expected). Any chart type with `tbl_Chart_HouseLords` rows is covered
automatically, including the new vargas.

### 3.7 Apply to existing data

No dedicated CLI mode. `recompute-keydetails` already recomputes each stored D1/D9 ChartResult from
Swiss Ephemeris via the orchestrator + `ChartAnalyzer`, so it picks up both the name fix and the
three new columns. Re-run it after deploying migrations 032–034 and the code. Its banner comment is
updated to mention the nakshatra linkage columns and the name-canon change. (D9 `Nakshatra` stays
NULL — unchanged.)

---

## 4. Work-unit 2 — Divisional charts D2, D6, D10, D11

### 4.1 Varga sign formulas (verbatim from PyJHora `charts.py`, `chart_method=1`)

Notation: `s` = sign index `Aries=0 … Pisces=11`; `d` = degrees within sign `[0,30)`; "odd sign" =
1-indexed odd = `s` even (`const.odd_signs = [0,2,4,6,8,10]`); `const.HOUSE_9 = 8`.

**D2 — Hora** (`_hora_traditional_parasara_chart`, i.e. `D2_CHART_METHOD` value 2, "Traditional
Parasara, only Le & Cn"). **Decided (D-1):** the classical two-sign rule below, *not* PyJHora's
`method=1` "Uma Shambu" 12-sign variant — "Hora chart" means the Sun/Moon (Leo/Cancer) split to a
reader, and JHora-desktop's "Parasara" setting is this.
```
half   = floor(d / 15)                         // 0 or 1
sunHora = (oddSign && half == 0) || (evenSign && half == 1)
result  = sunHora ? Leo(4) : Cancer(3)
```
`AstroMath.GetHoraSign(lon) : ZodiacName`.

**D6 — Shashtamsa** (`shashthamsa_chart`, method 1 "Traditional Parasara"):
```
part = floor(d / 5)                            // 0..5
result = oddSign ? (ZodiacName)(part)          // odd  → Aries..Virgo
                 : (ZodiacName)((part + 6) % 12) // even → Libra..Pisces   (source sign ignored)
```
`AstroMath.GetShashtamsaSign(lon) : ZodiacName`.

**D10 — Dasamsa** (`dasamsa_chart`, method 1 "Traditional Parasara", `dirn = +1`):
```
part = floor(d / 3)                            // 0..9
result = oddSign ? (ZodiacName)((s + part) % 12)
                 : (ZodiacName)((s + part + 8) % 12)   // count from the 9th sign
```
`AstroMath.GetDasamsaSign(lon) : ZodiacName`.

**D11 — Rudramsa** (`rudramsa_chart`, method 1 "Traditional Parasara / Sanjay Rath" — PyJHora
default):
```
part = floor(d / (30.0 / 11))                  // 0..10
result = (ZodiacName)((12 - s + part) % 12)
```
`AstroMath.GetRudramsaSign(lon) : ZodiacName`. (PyJHora carries an author TODO "Check calculation
against PVR book" on this function; it is nonetheless the shipped default and the documented Sanjay
Rath method. Flagged here for the record; verification cross-checks it against a JHora-based
calculator.)

Worked examples (become doc-comment assertions):

| fn | input → expected (four cases each) |
|---|---|
| D2 `GetHoraSign` | Aries 10° → **Leo** · Aries 20° → **Cancer** · Taurus 10° → **Cancer** · Taurus 20° → **Leo** |
| D6 `GetShashtamsaSign` | Aries 2° → **Aries** · Aries 27° → **Virgo** · Taurus 2° → **Libra** · Taurus 27° → **Pisces** |
| D10 `GetDasamsaSign` | Aries 1° → **Aries** · Aries 28° → **Capricorn** · Taurus 1° → **Capricorn** · Taurus 28° → **Libra** |
| D11 `GetRudramsaSign` | Aries 1° → **Aries** · Aries 29° → **Aquarius** · Taurus 1° → **Pisces** · Taurus 29° → **Capricorn** |

All 16 hand-derived from the §4.1 formulas (`part` width for D11 = 30/11 = 2.7273°; e.g. Taurus 29°:
`part = floor(29/2.7273) = 10`, `(12 − 1 + 10) % 12 = 9` = Capricorn). Implementation re-emits these
from the compiled `AstroMath` methods and the doc-comments carry them as inline assertions.

### 4.2 `VargaLongitudeDegrees` (combustion-within-varga)

Populated exactly as D9 does it — `AstroMath.GetVargaLongitude(nirayanaLongitude, N)` with
`N = 2 / 6 / 10 / 11`. Used only by `ClassicalCombustion` for a varga-space distance-to-Sun; the
authoritative varga **sign** is always the §4.1 classical formula, never this linear remap.
Documented as approximate and not classically emphasised for D2/D6.

### 4.3 New calculator classes

Two files per chart, mirroring `D9ChartComputer` / `D9NavamsaCalculator` line-for-line:

| Computer (`static ChartAnalysisInput Compute(BirthDetails)`) | Calculator (`IChartCalculator`) | `ChartType` | varga fn | `N` |
|---|---|---|---|---|
| `D2HoraChartComputer` | `D2HoraCalculator` | `"D2"` | `GetHoraSign` | 2 |
| `D6ShashtamsaChartComputer` | `D6ShashtamsaCalculator` | `"D6"` | `GetShashtamsaSign` | 6 |
| `D10DasamsaChartComputer` | `D10DasamsaCalculator` | `"D10"` | `GetDasamsaSign` | 10 |
| `D11RudramsaChartComputer` | `D11RudramsaCalculator` | `"D11"` | `GetRudramsaSign` | 11 |

Each `Compute`:
1. `BirthMomentFactory.Create` → `SwissEphemerisProvider.GetSiderealPositions`.
2. `vargaLagnaSign = <vargaFn>(positions.AscendantLongitude)`.
3. `Ascendant` `PlanetPosition` first (house 1), then the 9 planets, each:
   `Sign = <vargaFn>(nlon).ToString()`, `NirayanaLongitudeDegrees = nlon`,
   `VargaLongitudeDegrees = AstroMath.GetVargaLongitude(nlon, N)`,
   `HouseNumber = AstroMath.CountFromSignToSign(vargaLagnaSign, vargaSign)`,
   `IsRetrograde = positions.PlanetSpeeds[planet] < 0`.
   `DegreesInSign` / `Nakshatra` / `NakshatraPada` left unset (varga — D1-only concepts).
4. `return new ChartAnalysisInput("<Dn>", vargaLagnaSign, planetPositions)`.

Each `BuildResult`: serialise `{ VargaLagna = new { Sign = analysisInput.AscendantSign.ToString() },
Planets = analysisInput.Planets }`; `Ayanamsha = "Lahiri"`, `HouseSystem = "WholeSign"`,
`EngineVersion = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)"` (identical to D1/D9).

### 4.4 Registration

`ChartCalculationOrchestrator.CreateDefault()`:
```csharp
new(new IChartCalculator[]
{
    new D1RasiCalculator(), new D2HoraCalculator(), new D6ShashtamsaCalculator(),
    new D9NavamsaCalculator(), new D10DasamsaCalculator(), new D11RudramsaCalculator()
});
```
Class doc-comment updated ("D1 + D9 only" → the six). `CalculateAll` (new-entry flow, CLI + Web
write paths) now emits all six for every new person with no other change.

### 4.5 Composition with work-unit 1

`ChartAnalyzer` runs once per chart type, so D2/D6/D10/D11 `tbl_Chart_KeyDetails` rows receive
`NakshatraId` + `NakshatraSubLordPlanet` (degree-derived from the real longitude — same value as
that planet's D1 row) and `NakshatraPadaId = NULL` (varga). `vw_Chart_HouseNakshatraSpan` picks
them up via their `tbl_Chart_HouseLords` rows automatically.

### 4.6 Schema impact

**None.** `tbl_ChartResults` already stores arbitrary `ChartType`; the four analytics tables are
chart-type-generic (`ChartType` column since 2026-08-24); `vw_Chart_Consolidated` already filters
by `ChartType`. Migrations 032–034 belong entirely to work-unit 1.

---

## 5. Work-unit 3 — CLI

### 5.1 New mode: `backfill-charts`

Sibling of `backfill-dasha`. For each saved person, for each registered calculator whose
`ChartType` has no `tbl_ChartResults` row for that person:

```
input   = calculator.ComputeAnalysisInput(person)
result  = calculator.BuildResult(person, input)
result  = chartResultsRepo.Insert(result)                 // returns Id
(kd, hl, cj, asp) = ChartAnalyzer.Compute(input)
stamp ChartResultId = result.Id, BirthDetailId = person.Id on every row
keyDetailsRepo.InsertAll(kd); houseLordsRepo.InsertAll(hl)
if cj.Count > 0 conjunctionsRepo.InsertAll(cj)
if asp.Count > 0 aspectsRepo.InsertAll(asp)
print "Created {person} — {ChartType}: N key-details, ..."
```

Idempotent (skips any `(person, ChartType)` that already exists). Generic — when D3/D7/D12/… are
added later and registered, this mode picks them up with no change. Requires
`ChartCalculationOrchestrator` to expose its calculators:
`public IReadOnlyList<IChartCalculator> Calculators => _calculators;`

### 5.2 Generalise `recompute-keydetails`

Replace the hardcoded filter `if (result.ChartType is not ("D1" or "D9")) continue;` with:
```csharp
var calculableTypes = orchestratorForRecompute.Calculators.Select(c => c.ChartType).ToHashSet();
...
if (!calculableTypes.Contains(result.ChartType)) continue;   // still skips VimshottariDasha
```
So one `recompute-keydetails` run now re-derives KeyDetails (with the work-unit-1 nakshatra
columns + name canon) for D1, D2, D6, D9, D10, D11 alike. Banner comment updated.

### 5.3 Optional: `verify-vargas`

Low-cost repeatable check (no DB writes): prints `GetHoraSign/GetShashtamsaSign/GetDasamsaSign/
GetRudramsaSign` for the §4.1 worked-example longitudes and flags any mismatch against the pinned
expected values. Included because the solution has **no test project** (see §7). Small; drop if it
adds noise.

---

## 6. Migrations summary

| # | File | Content | Base-DDL sync |
|---|---|---|---|
| 032 | `032_add_nakshatra_linkage_to_keydetails.sql` | `tbl_Chart_KeyDetails` + `NakshatraId` (FK `tbl_Nakshatras`), `NakshatraPadaId` (FK `tbl_NakshatraPadas`), `NakshatraSubLordPlanet` VARCHAR(10); all NULL-able | `003_create_d1_keydetails_tables.sql` |
| 033 | `033_add_primary_rasi_to_nakshatras.sql` | `tbl_Nakshatras` + `PrimaryRasiId` (FK `tbl_SignAttributes`), `StraddlesSignBoundary` BIT NOT NULL DEFAULT 0; `UPDATE` from `tbl_NakshatraPadas`; then `PrimaryRasiId` → NOT NULL | `021_create_nakshatra_reference_tables.sql` |
| 034 | `034_create_house_nakshatra_span_view.sql` | `CREATE VIEW vw_Chart_HouseNakshatraSpan` (§3.6) | n/a (view) |

Numbering: 030/031 remain reserved (unapplied) for the house/Lagna significations work. This batch
takes 032–034. `STANDARDS.md` §D.1 infix rule: no new base tables here, so no `tbl_Dim_`/`tbl_Rule_`
/`tbl_Fact_` decision; the new view follows the existing `vw_Chart_*` convention.

Applied live via `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db\03x_*.sql`.

---

## 7. Verification

The solution has **no test project and is not under git** — verification follows the project's
established discipline: build, run, then cross-check computed output against an independent source
and against hand calculation, recorded in `ikiastrro.md`.

### 7.1 Build & apply
- `dotnet build` clean.
- Apply migrations 032–034; confirm columns/FKs/view exist (`INFORMATION_SCHEMA`, `sys.foreign_keys`).

### 7.2 Work-unit 1
1. `recompute-keydetails` → all existing D1/D9 ChartResults re-derived.
2. `SELECT DISTINCT Nakshatra FROM tbl_Chart_KeyDetails WHERE Nakshatra IS NOT NULL` — every value
   present in `tbl_Nakshatras.NakshatraName` (0 unmatched).
3. Cross-join check (0 mismatches), for every `tbl_Chart_KeyDetails` row:
   - `NakshatraId` = the `tbl_Nakshatras` row whose `[StartDegree, EndDegree)` contains
     `NirayanaLongitudeDegrees`.
   - `NakshatraSubLordPlanet` = the `tbl_NakshatraSubLords` row (via `tbl_Planets.PlanetName`) whose
     `[StartDegree, EndDegree)` contains `NirayanaLongitudeDegrees`.
   - D1 rows: `NakshatraPadaId` = the `tbl_NakshatraPadas` row whose `[StartDegree, EndDegree)`
     contains it; varga rows: `NakshatraPadaId IS NULL`.
4. `tbl_Nakshatras`: exactly the nine expected straddlers have `StraddlesSignBoundary = 1`;
   `PrimaryRasiId` non-NULL for all 27 and equals the midpoint's sign.
5. `vw_Chart_HouseNakshatraSpan` for rammyps's D1 (BirthDetailId 2): 108 rows; House 1 (Aries) →
   Ashwini p1–4, Bharani p1–4, Krittika p1; spot-check two more houses by hand.
6. Regression: a `recompute-keydetails` before/after row diff shows changes **only** in
   `Nakshatra` (spelling) + the three new columns — no house/sign/dignity/aspect drift.

### 7.3 Work-unit 2
1. `backfill-charts` → 4 new ChartResults × 5 people = 20, each with ~10 KeyDetails + 12 HouseLords
   + Conjunctions + Aspects.
2. **External cross-check — rammyps (22 Apr 1981, 05:30, Chennai)**, all 9 planets + Lagna:
   - D6, D10, D11 planet signs vs a Jagannatha-Hora-based calculator (primary).
   - D10 additionally vs AstroSage (second source).
   - D2: hand-derived from the stored D1 degrees (odd/even sign × half → Leo/Cancer).
   Any divergence is investigated before the batch is called done (same bar as the SwissEphNet
   cross-check that found the VedAstro ayanamsha bug).
3. Consistency: in D10 and D11, `Rahu` and `Ketu` are exactly 6 houses / opposite signs apart
   (must hold — they are 180° apart in real longitude). **Not** expected for D2 (2-sign chart) or
   D6 (source sign ignored) — noted, not a failure.
4. `verify-vargas` (if built) passes; §4.1 worked-example table recomputed from final code and
   pinned into this doc + the fn doc-comments.
5. Regression: `tbl_Chart_*` rows for D1/D9 unchanged by `backfill-charts` (it only touches
   ChartTypes with no existing rows).
6. `SELECT * FROM vw_Chart_Consolidated WHERE Name = 'Ramakrishnan' AND ChartType = 'D10'` returns
   coherent rows (dignity, rules-houses, conjunct/aspect columns populated).

---

## 8. Documentation updates

- **`ikiastrro.md`** — new dated section (2026-08-30, later): both work-units, the
  PyJHora source citation, the D-1 D2-method decision, verification results.
- **`README.md`** — varga-support matrix (D2/D6/D10/D11 now ✅ at DB level, UI pending); "Extending
  later" (pattern now exercised 5×); "Known limitations" (no varga UI — consistent, deferred);
  new `vw_Chart_HouseNakshatraSpan` + the three `tbl_Chart_KeyDetails` columns in the
  key-details section; `tbl_Nakshatras` sign columns.
- **`memproj_vedic_horo_gen.md`** (memory) — append a paragraph; keep it a pointer.
- **`methods_prodmag.md`** — mark the D6 / D11 Opportunity Backlog entries resolved.
- **`coverage_proj_vedic_horo_gen.md`** — point 6 (house-lord in Navamsa) and the health/wealth
  rows: note D6/D11 now available.
- This spec file committed alongside (project not under git → left in `D:\@ClaudeSpace\` as the
  other `product_scope_*.md` docs are).

---

## 9. Risks & mitigations

| Risk | Mitigation |
|---|---|
| D11 formula (PyJHora author TODO) may not match JHora-desktop exactly | External cross-check in §7.3.2 against a JHora-based calculator before sign-off; formula + source cited so a later correction is a one-line change in `GetRudramsaSign`. |
| Identity-arithmetic (`NakshatraId = index+1`, `NakshatraPadaId = slot+1`) silently wrong if seed order ever changed | §7.2.3 degree-range cross-join catches any mismatch on real data; assumption documented in code with a fallback path noted. |
| `ZodiacEnumValue` join in `vw_Chart_HouseNakshatraSpan` misses a sign if a spelling is off | §7.2.5 row-count check (must be exactly 108 for a D1) fails loudly if any house doesn't join. |
| D2's generic analytics look odd (many conjunctions, only 2 signs) | Expected and documented; D2 analytics are mechanically valid, classically read simply. |
| Name-canon change alters what a future Web UI shows | Acceptable — the canonical spellings are the correct ones; UI is a later batch and will read the corrected data. |

---

## 10. Out of scope (recorded for the next batches)

- **Timeline chart** — `tvf_Chart_LifeTimeline(@BirthDetailId)`: 4000-week grid (from
  `tvf_Chart_LifeWeeks`) left-joined to `tvf_Chart_SadeSatiPeriods` (Sade Sati 3 Dhaiyas + Kantaka
  4th-from-Moon + Ashtama 8th-from-Moon), per-week Dasha lords + Saturn-affliction flags + run
  number (1–3). Open question for that batch: reckon Kantaka/Ashtama from Moon only, or Moon + Lagna.
- **New Web UI** — full redesign covering D1/D9 + the four new vargas + the timeline.
- **D12 (parents)** and any further vargas — one calculator-pair each, same pattern.
