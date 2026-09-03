# Divisional-Chart Completion — Plan A Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 15 position chart types (D2-US + D3, D4, D5, D7, D8, D12, D16, D20, D24, D27, D30, D40, D45, D60) computed by one database-driven varga engine, with every computed value stored in a typed column.

**Architecture:** A per-planet `IVargaSignRule.SignFor(siderealLongitude) → ZodiacName` strategy, one row per chart type in a new `tbl_Rule_VargaScheme` table (read by C# and, later, Python). One `VargaChartComputer` + one generic `VargaCalculator` replace the six bespoke D2/D6/D9/D10/D11 computer+calculator pairs (their maths lives on behind thin wrapper rules — byte-identical). `tbl_Chart_KeyDetails` gains `VargaLongitudeDegrees`; `DegreesInSignDecimal`/`Display` are un-gated from D1-only; `tbl_ChartResults` gains `VargaMethod` / `AyanamshaDegrees` / `SiderealTimeHours`. `ResultJson` becomes a non-authoritative audit snapshot.

**Tech Stack:** .NET 8 / C#, SQL Server (T-SQL, SMO-scripted baseline `db/ikiastrro.sql`), Dapper, SwissEphNet (Moshier, Lahiri sidereal), a console CLI with `verify-*` self-check modes (no xUnit project — verification is `dotnet build` + CLI modes + a Web smoke).

**Spec:** `docs/superpowers/specs/2026-08-31-divisional-chart-completion-design.md` — read it first; this plan argues from it.

## Global Constraints

- **Database:** `ikiastrro` on `Server=localhost` via Windows Auth. Apply migrations with `sqlcmd -S localhost -E -d ikiastrro -i db/<file>.sql`.
- **Migration numbering continues the schema-normalization chain** (last applied was `09`). New scripts are `10_`–`13_`, `NN_<verb>_<noun>.sql`, in the `db/` folder. Each is **idempotent** (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ...)`, `IF NOT EXISTS (SELECT 1 FROM <seed_table>)`) and ends by recording itself in `dbo.SchemaMigrations` (`WHERE NOT EXISTS`).
- **Apply order = file order:** `10` (dim rows) → `11` (varga scheme) → `12` (provenance + `VargaLongitudeDegrees`) → `13` (un-gate degrees). `12`'s backfill uses an inline `CASE ChartType` for the 6 existing types, so it does not depend on `10`/`11` data — but keeping file order == apply order is the rule.
- **`tbl_Rule_VargaScheme.Id` is `TINYINT`.** `tbl_Dim_ChartType.Id` is `TINYINT`; existing rows are `1..6` (D1, D2, D6, D9, D10, D11). New rows are `7..21`. Ids `22..24` are left free for Plan B (D81/D108/D144).
- **`IVargaSignRule.SignFor(double siderealLongitude)`** — every rule takes the full 0–360 longitude and derives `sign` / `degInRasiSign` / `l` internally, so wrapper rules can call `AstroMath.GetXSign(double)` unchanged.
- **`l`** in every formula below = `(int)(degInRasiSign / (30.0 / N))`, where `degInRasiSign = AstroMath.GetDegreesInSign(lon)` and `sign = (int)(AstroMath.Normalize(lon) / 30)` (0-indexed, Aries = 0).
- **Sign-group predicates** (0-indexed sign): movable ⇔ `sign % 3 == 0`; fixed ⇔ `sign % 3 == 1`; dual ⇔ `sign % 3 == 2`. Odd sign (classical 1,3,5,…) ⇔ `sign % 2 == 0`. Element ⇔ `sign % 4` (0 fire, 1 earth, 2 air, 3 water).
- **`ZodiacName`:** `Aries=0 … Cancer=3 Leo=4 … Scorpio=7 Sagittarius=8 Capricornus=9 Aquarius=10 Pisces=11`. Cast a `% 12` int to `(ZodiacName)`.
- **`DegreesInVargaSign` = `AstroMath.Normalize(siderealLongitude * N) % 30`** for every varga (PyJHora's `d_long`), regardless of how the sign is chosen. `VargaLongitudeDegrees` (persisted) = `AstroMath.Normalize(siderealLongitude * N)`.
- **Commit after every task.** Conventional-commit style (`feat(db):`, `feat(core):`, `refactor(core):`, `chore(db):`). Every commit message ends with the two trailers used elsewhere in this repo's Claude-authored commits:
  ```
  Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW
  ```
- **Branch:** work continues on `feat/ikiastrro-workspace-ui`. Do not push unless asked.
- **Baseline fold rule:** after all four migrations pass on the live DB (Task 19), copy their final DDL into `db/ikiastrro.sql`. `vw_Chart_Consolidated` must remain the **last** object defined in that file.
- **PyJHora reference:** all formulae below are traced to `_research/PyJHora/src/jhora/horoscope/chart/charts.py` (git-ignored vendored source, AGPL — reference only, never copied verbatim). The seed table's `SignRuleKey` per varga is confirmed against the Ramakrishnan Jagannatha Hora export (`D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt`) in Task 17 and re-pointed if it diverges.

---

## File Structure

### Phase 1 — Schema (migrations, additive)

| File | Create/Modify | Responsibility |
|---|---|---|
| `db/10_seed_varga_charttypes.sql` | Create | `tbl_Dim_ChartType` Ids 7–21 (D2-US + 14 vargas) |
| `db/11_create_rule_vargascheme.sql` | Create | `tbl_Rule_VargaScheme` table + 20-row seed (RuleSetId 1) |
| `db/12_add_varga_provenance_columns.sql` | Create | `tbl_ChartResults` + `VargaMethod`/`AyanamshaDegrees`/`SiderealTimeHours`; `tbl_Chart_KeyDetails` + `VargaLongitudeDegrees` (NULLable → backfill → `NOT NULL` + `CHECK`) |
| `db/13_ungate_degrees_in_sign.sql` | Create | Recompute `DegreesInSignDecimal` / `DegreesInSignDisplay` for existing non-D1 rows from `VargaLongitudeDegrees` |

### Phase 2 — Core: varga sign rules

| File | Create/Modify | Responsibility |
|---|---|---|
| `src/Ikiastrro.Core/Astro/IVargaSignRule.cs` | Create | `ZodiacName SignFor(double siderealLongitude)` |
| `src/Ikiastrro.Core/Astro/LinearVargaSignRule.cs` | Create | `ctor(int factor, int stride)` → `(sign + l*stride) % 12` — D3(4), D4(3), D12(1), D60(1) |
| `src/Ikiastrro.Core/Astro/SaptamsaD7SignRule.cs` | Create | odd → `(sign+l)%12`; even → `(sign+l+6)%12` |
| `src/Ikiastrro.Core/Astro/AshtamsaD8SignRule.cs` | Create | movable→`l%12`; fixed→`(l+8)%12`; dual→`(l+4)%12` |
| `src/Ikiastrro.Core/Astro/SiddhamsaD24SignRule.cs` | Create | odd→`(4+l)%12`; even→`(3+l)%12` |
| `src/Ikiastrro.Core/Astro/NakshatramsaD27SignRule.cs` | Create | fire→`l%12`; earth→`(l+3)%12`; air→`(l+6)%12`; water→`(l+9)%12` |
| `src/Ikiastrro.Core/Astro/ShodasamsaD16SignRule.cs` | Create | movable→`l%12`; fixed→`(l+4)%12`; dual→`(l+8)%12` |
| `src/Ikiastrro.Core/Astro/VimsamsaD20SignRule.cs` | Create | movable→`l%12`; dual→`(l+4)%12`; fixed→`(l+8)%12` |
| `src/Ikiastrro.Core/Astro/AkshavedamsaD45SignRule.cs` | Create | movable→`l%12`; fixed→`(l+4)%12`; dual→`(l+8)%12` |
| `src/Ikiastrro.Core/Astro/KhavedamsaD40SignRule.cs` | Create | odd→`l%12`; even→`(l+6)%12` |
| `src/Ikiastrro.Core/Astro/PanchamsaD5SignRule.cs` | Create | odd → `[0,10,8,2,6][l]`; even → `[1,5,11,9,7][l]` |
| `src/Ikiastrro.Core/Astro/TrimsamsaD30SignRule.cs` | Create | odd/even unequal 5-part table on `degInRasiSign` |
| `src/Ikiastrro.Core/Astro/HoraD2ClassicSignRule.cs` | Create | wraps `AstroMath.GetHoraSign` |
| `src/Ikiastrro.Core/Astro/HoraD2UmaShambuSignRule.cs` | Create | Parasara Uma Shambu, ported from PyJHora |
| `src/Ikiastrro.Core/Astro/ShashtamsaD6SignRule.cs` | Create | wraps `AstroMath.GetShashtamsaSign` |
| `src/Ikiastrro.Core/Astro/NavamsaD9SignRule.cs` | Create | wraps `AstroMath.GetNavamsaSign` |
| `src/Ikiastrro.Core/Astro/DasamsaD10SignRule.cs` | Create | wraps `AstroMath.GetDasamsaSign` |
| `src/Ikiastrro.Core/Astro/RudramsaD11SignRule.cs` | Create | wraps `AstroMath.GetRudramsaSign` |
| `src/Ikiastrro.Core/Astro/VargaSignRuleFactory.cs` | Create | `(kind, key, factor) → IVargaSignRule` — the one place a `SignRuleKey` string maps to a class |
| `src/Ikiastrro.Cli/Program.cs` | Modify | `verify-vargas`: add hand-computed `IVargaSignRule` unit checks per rule |

### Phase 3 — Computer, calculator, wiring

| File | Create/Modify | Responsibility |
|---|---|---|
| `src/Ikiastrro.Core/Models/VargaScheme.cs` | Create | Read model for `tbl_Rule_VargaScheme` |
| `src/Ikiastrro.Core/Calculators/VargaChartComputer.cs` | Create | `Compute(BirthDetails, int factor, IVargaSignRule) → ChartAnalysisInput` |
| `src/Ikiastrro.Core/Calculators/VargaCalculator.cs` | Create | Generic `IChartCalculator` — `ctor(string chartType, VargaScheme)` |
| `src/Ikiastrro.Core/Calculators/ChartCalculationOrchestrator.cs` | Modify | `CreateDefault(IReadOnlyList<VargaScheme>)` builds D1 + one `VargaCalculator` per scheme |
| `src/Ikiastrro.Data/VargaSchemeRepository.cs` | Create | `GetAll(int ruleSetId) → IReadOnlyList<VargaScheme>` |
| `src/Ikiastrro.Cli/Program.cs` | Modify | Load schemes, pass to `CreateDefault` |
| `src/Ikiastrro.Web/Program.cs` | Modify | Resolve `VargaSchemeRepository` in the `AddScoped` factory |
| `src/Ikiastrro.Core/Calculators/D2HoraCalculator.cs` … `D11RudramsaChartComputer.cs` (10 files) | Delete | Replaced by scheme rows + `VargaCalculator` + wrapper rules |
| `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs` | Modify | `SiderealPositions` + `AyanamshaDegrees`, `LocalSiderealTimeHours` |
| `src/Ikiastrro.Core/Models/PlanetPosition.cs` | Modify | (already has `VargaLongitudeDegrees`) — no change; noted for context |
| `src/Ikiastrro.Core/Models/ChartKeyDetail.cs` | Modify | `+ double VargaLongitudeDegrees` |
| `src/Ikiastrro.Core/Models/ChartResult.cs` | Modify | `+ string? VargaMethod`, `+ double? AyanamshaDegrees`, `+ double? SiderealTimeHours`; `ResultJson` doc comment |
| `src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs` | Modify | Persist `VargaLongitudeDegrees`; un-gate `DegreesInSignDecimal` / `DegreesInSignDisplay` for every chart type |
| `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs` | Modify | INSERT column list + `@VargaLongitudeDegrees` |
| `src/Ikiastrro.Data/ChartResultsRepository.cs` | Modify | INSERT column list + `@VargaMethod`/`@AyanamshaDegrees`/`@SiderealTimeHours` |
| `src/Ikiastrro.Data/ChartGenerationService.cs` | Modify | Stamp `AyanamshaDegrees`/`SiderealTimeHours` once per person; carry `VargaMethod` from the calculator |

### Phase 4 — Generate, verify, finalize

| File | Create/Modify | Responsibility |
|---|---|---|
| (CLI runs only) | — | `backfill-charts`, `recompute-keydetails` for the 5 seeded people |
| `src/Ikiastrro.Cli/Program.cs` | Modify | `verify-vargas`: JHora-grid match (DB-backed) + degree sanity + Vargottama sections |
| `db/ikiastrro.sql` | Modify | Fold migrations 10–13; extend `vw_Chart_Consolidated` |
| `docs/reference-calculations.md`, `docs/dbdesign-star-schema-rules-engine.md`, `ARCHITECTURE.md`, `master_ikiastrro.md`, `../ikiastrro.md`, memory | Modify | Documentation |

---

# PHASE 1 — SCHEMA

## Task 1: `tbl_Dim_ChartType` — 15 new rows

**Files:**
- Create: `db/10_seed_varga_charttypes.sql`

**Interfaces:**
- Produces: `tbl_Dim_ChartType` rows Id `7..21` with `Code` ∈ `{D2-US, D3, D4, D5, D7, D8, D12, D16, D20, D24, D27, D30, D40, D45, D60}`, each `DivisionalFactor = N`, `Category = 'Varga'`.

- [ ] **Step 1: Confirm the current `tbl_Dim_ChartType` shape and max Id**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT Id, Code, DisplayName, DivisionalFactor, Category, DisplayOrder FROM dbo.tbl_Dim_ChartType ORDER BY Id"`
Expected: 6 rows, Id 1–6, codes `D1,D2,D6,D9,D10,D11`. Note the exact column list and the `DisplayName` style already used (e.g. `Rasi`, `Hora`, `Navamsa`).

- [ ] **Step 2: Write `db/10_seed_varga_charttypes.sql`**

```sql
-- =====================================================================
-- 10 — tbl_Dim_ChartType: D2-US + the 14 Plan-A vargas (Ids 7..21).
-- Ids 22..24 are reserved for Plan B (D81/D108/D144). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/10_seed_varga_charttypes.sql
-- =====================================================================
USE [ikiastrro];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType WHERE Id = 7)
INSERT dbo.tbl_Dim_ChartType (Id, Code, DisplayName, DivisionalFactor, Category, DisplayOrder) VALUES
    ( 7, 'D2-US', 'Hora (Uma Shambu)', 2,  'Varga',  7),
    ( 8, 'D3',    'Drekkana',          3,  'Varga',  3),
    ( 9, 'D4',    'Chaturthamsa',      4,  'Varga',  4),
    (10, 'D5',    'Panchamsa',         5,  'Varga', 17),
    (11, 'D7',    'Saptamsa',          7,  'Varga',  5),
    (12, 'D8',    'Ashtamsa',          8,  'Varga', 18),
    (13, 'D12',   'Dwadasamsa',        12, 'Varga',  8),
    (14, 'D16',   'Shodasamsa',        16, 'Varga',  9),
    (15, 'D20',   'Vimsamsa',          20, 'Varga', 10),
    (16, 'D24',   'Siddhamsa',         24, 'Varga', 11),
    (17, 'D27',   'Nakshatramsa',      27, 'Varga', 12),
    (18, 'D30',   'Trimsamsa',         30, 'Varga', 13),
    (19, 'D40',   'Khavedamsa',        40, 'Varga', 14),
    (20, 'D45',   'Akshavedamsa',      45, 'Varga', 15),
    (21, 'D60',   'Shashtyamsa',       60, 'Varga', 16);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '10_seed_varga_charttypes.sql', 'D2-US + 14 Plan-A varga chart types (Id 7..21)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '10_seed_varga_charttypes.sql');
GO
PRINT '10 applied: tbl_Dim_ChartType has D2-US + 14 vargas.';
GO
```

- [ ] **Step 3: If Step 1 showed a different column list, adjust the `INSERT` accordingly** (e.g. a `DivisionalFactor` NULL check constraint `CK_Dim_ChartType_Factor CHECK (DivisionalFactor BETWEEN 1 AND 60)` — all 15 values are ≤ 60, so no conflict).

- [ ] **Step 4: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/10_seed_varga_charttypes.sql`
Expected: `10 applied: tbl_Dim_ChartType has D2-US + 14 vargas.`

- [ ] **Step 5: Verify**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT COUNT(*) AS n, MIN(Id) AS lo, MAX(Id) AS hi FROM dbo.tbl_Dim_ChartType"`
Expected: `n = 21, lo = 1, hi = 21`.

- [ ] **Step 6: Re-apply to prove idempotency**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/10_seed_varga_charttypes.sql`
Expected: same `PRINT`, still 21 rows.

- [ ] **Step 7: Commit**

```bash
git add db/10_seed_varga_charttypes.sql
git commit -m "$(printf 'feat(db): seed tbl_Dim_ChartType with D2-US + 14 Plan-A vargas\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 2: `tbl_Rule_VargaScheme` table + seed

**Files:**
- Create: `db/11_create_rule_vargascheme.sql`

**Interfaces:**
- Produces: `dbo.tbl_Rule_VargaScheme (Id TINYINT PK, RuleSetId TINYINT FK, ChartTypeId TINYINT FK, DivisionFactor TINYINT, MethodCode VARCHAR(40), MethodSource VARCHAR(200), SignRuleKind VARCHAR(10), SignRuleKey VARCHAR(40), UQ(RuleSetId, ChartTypeId))`, seeded with 20 rows under `RuleSetId = 1` — one per chart type in `{D2, D2-US, D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D16, D20, D24, D27, D30, D40, D45, D60}`. **D1 is not in this table.**

- [ ] **Step 1: Confirm `tbl_Rule_Sets` has an active row Id 1**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT Id, RuleSetName, IsActive FROM dbo.tbl_Rule_Sets"`
Expected: Id `1`, `Parashari-Classical`, `IsActive = 1`.

- [ ] **Step 2: Write `db/11_create_rule_vargascheme.sql`**

```sql
-- =====================================================================
-- 11 — tbl_Rule_VargaScheme: how each varga chart type derives a planet's
-- varga sign, per rule-set. Read by C# (VargaSignRuleFactory) and, later,
-- by the Python comparison layer. D1 is the identity rasi and is NOT here.
-- SignRuleKey names the C# IVargaSignRule; the l-part formulae are in the
-- Plan-A spec §3.2 (traced to PyJHora horoscope/chart/charts.py).
-- Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/11_create_rule_vargascheme.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Rule_VargaScheme', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_VargaScheme (
    Id             TINYINT      NOT NULL CONSTRAINT PK_Rule_VargaScheme PRIMARY KEY,
    RuleSetId      TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_RuleSet   FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    ChartTypeId    TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_ChartType FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
    DivisionFactor TINYINT      NOT NULL,
    MethodCode     VARCHAR(40)  NOT NULL,
    MethodSource   VARCHAR(200) NOT NULL,
    SignRuleKind   VARCHAR(10)  NOT NULL,
    SignRuleKey    VARCHAR(40)  NOT NULL,
    CONSTRAINT UQ_Rule_VargaScheme UNIQUE (RuleSetId, ChartTypeId)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_VargaScheme)
INSERT dbo.tbl_Rule_VargaScheme
    (Id, RuleSetId, ChartTypeId, DivisionFactor, MethodCode, MethodSource, SignRuleKind, SignRuleKey)
SELECT v.Id, 1, ct.Id, v.N, v.MethodCode, v.MethodSource, v.Kind, v.SignRuleKey
FROM (VALUES
    ( 1, 'D2',    2,  'ClassicalTwoSign',    'BPHS two-sign Cn/Le; AstroMath.GetHoraSign',                       'Special', 'HoraD2Classic'),
    ( 2, 'D2-US', 2,  'UmaShambu',           'Parasara Uma Shambu; PyJHora hora_chart d2 default (closest)',     'Special', 'HoraD2UmaShambu'),
    ( 3, 'D3',    3,  'ParasaraTraditional', 'BPHS Drekkana 1/5/9; PyJHora _drekkana_chart_parasara',            'Linear',  'DrekkanaD3'),
    ( 4, 'D4',    4,  'ParasaraTraditional', 'BPHS Chaturthamsa; PyJHora _chaturthamsa_parasara',                'Linear',  'ChaturthamsaD4'),
    ( 5, 'D5',    5,  'ParasaraTraditional', 'BPHS Panchamsa; PyJHora panchamsa_chart method 1',                 'Special', 'PanchamsaD5'),
    ( 6, 'D6',    6,  'ParasaraTraditional', 'BPHS Shashtamsa; AstroMath.GetShashtamsaSign',                     'Special', 'ShashtamsaD6'),
    ( 7, 'D7',    7,  'ParasaraTraditional', 'BPHS Saptamsa odd-self/even-7th; PyJHora saptamsa_chart method 1', 'Special', 'SaptamsaD7'),
    ( 8, 'D8',    8,  'ParasaraTraditional', 'BPHS Ashtamsa; PyJHora ashtamsa_chart method 1',                   'Special', 'AshtamsaD8'),
    ( 9, 'D9',    9,  'ParasaraTraditional', 'BPHS Navamsa; AstroMath.GetNavamsaSign',                           'Special', 'NavamsaD9'),
    (10, 'D10',   10, 'ParasaraTraditional', 'BPHS Dasamsa odd-self/even-9th; AstroMath.GetDasamsaSign',         'Special', 'DasamsaD10'),
    (11, 'D11',   11, 'SanjayRath',          'Sanjay Rath Rudramsa; AstroMath.GetRudramsaSign',                  'Special', 'RudramsaD11'),
    (12, 'D12',   12, 'ParasaraTraditional', 'BPHS Dwadasamsa 12-from-self; PyJHora dwadasamsa_chart method 1',  'Linear',  'DwadasamsaD12'),
    (13, 'D16',   16, 'ParasaraTraditional', 'BPHS Shodasamsa; PyJHora shodasamsa_chart method 1',               'Special', 'ShodasamsaD16'),
    (14, 'D20',   20, 'ParasaraTraditional', 'BPHS Vimsamsa; PyJHora vimsamsa_chart method 1',                   'Special', 'VimsamsaD20'),
    (15, 'D24',   24, 'ParasaraTraditional', 'BPHS Siddhamsa odd-Le/even-Cn; PyJHora chaturvimsamsa_chart m1',   'Special', 'SiddhamsaD24'),
    (16, 'D27',   27, 'ParasaraTraditional', 'BPHS Nakshatramsa by element; PyJHora nakshatramsa_chart m1',      'Special', 'NakshatramsaD27'),
    (17, 'D30',   30, 'ParasaraTraditional', 'BPHS Trimsamsa unequal 5-part; PyJHora trimsamsa_chart method 1',  'Special', 'TrimsamsaD30'),
    (18, 'D40',   40, 'ParasaraTraditional', 'BPHS Khavedamsa odd-Ar/even-Li; PyJHora khavedamsa_chart m1',      'Special', 'KhavedamsaD40'),
    (19, 'D45',   45, 'ParasaraTraditional', 'BPHS Akshavedamsa; PyJHora akshavedamsa_chart method 1',           'Special', 'AkshavedamsaD45'),
    (20, 'D60',   60, 'ParasaraTraditional', 'BPHS Shashtyamsa from-sign; PyJHora shashtyamsa_chart method 1',   'Linear',  'ShashtyamsaD60')
) AS v(Id, Code, N, MethodCode, MethodSource, Kind, SignRuleKey)
JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = v.Code;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '11_create_rule_vargascheme.sql', 'tbl_Rule_VargaScheme + 20-row seed (RuleSetId 1)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '11_create_rule_vargascheme.sql');
GO
PRINT '11 applied: tbl_Rule_VargaScheme ready.';
GO
```

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/11_create_rule_vargascheme.sql`
Expected: `11 applied: tbl_Rule_VargaScheme ready.`

- [ ] **Step 4: Verify — 20 rows, every ChartTypeId resolves, DivisionFactor matches the Dim row**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT COUNT(*) AS rows FROM dbo.tbl_Rule_VargaScheme; SELECT COUNT(*) AS factor_mismatch FROM dbo.tbl_Rule_VargaScheme vs JOIN dbo.tbl_Dim_ChartType ct ON ct.Id = vs.ChartTypeId WHERE ct.DivisionalFactor <> vs.DivisionFactor; SELECT COUNT(*) AS unresolved FROM dbo.tbl_Rule_VargaScheme vs WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType ct WHERE ct.Id = vs.ChartTypeId)"
```
Expected: `rows = 20`, `factor_mismatch = 0`, `unresolved = 0`.

- [ ] **Step 5: Re-apply for idempotency** — same `PRINT`, still 20 rows, table not recreated.

- [ ] **Step 6: Commit**

```bash
git add db/11_create_rule_vargascheme.sql
git commit -m "$(printf 'feat(db): tbl_Rule_VargaScheme — data-driven varga sign rules (RuleSetId 1)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 3: Provenance columns + `VargaLongitudeDegrees`

**Files:**
- Create: `db/12_add_varga_provenance_columns.sql`

**Interfaces:**
- Produces:
  - `tbl_ChartResults`: `VargaMethod VARCHAR(40) NULL`, `AyanamshaDegrees DECIMAL(9,6) NULL`, `SiderealTimeHours DECIMAL(9,6) NULL`.
  - `tbl_Chart_KeyDetails`: `VargaLongitudeDegrees DECIMAL(9,6) NOT NULL` (after backfill), `CHECK (VargaLongitudeDegrees >= 0 AND VargaLongitudeDegrees < 360)`.

- [ ] **Step 1: Confirm which chart types currently have `tbl_Chart_KeyDetails` rows**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT cr.ChartType, COUNT(*) AS rows FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId GROUP BY cr.ChartType ORDER BY cr.ChartType"`
Expected: `D1, D2, D6, D9, D10, D11` (post schema-normalization the child tables have no `ChartType` — it comes via `cr`). No `VimshottariDasha` rows in `tbl_Chart_KeyDetails`.

- [ ] **Step 2: Write `db/12_add_varga_provenance_columns.sql`**

```sql
-- =====================================================================
-- 12 — Provenance columns + VargaLongitudeDegrees.
-- tbl_ChartResults gains numeric ayanamsha + sidereal time + the varga
-- method tag. tbl_Chart_KeyDetails gains VargaLongitudeDegrees, the
-- planet's longitude in this chart's own 360° space ((real × N) mod 360);
-- backfilled from an inline CASE for the 6 existing chart types, then
-- NOT NULL + CHECK. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/12_add_varga_provenance_columns.sql
-- =====================================================================
USE [ikiastrro];
GO
-- tbl_ChartResults provenance ----------------------------------------
IF COL_LENGTH('dbo.tbl_ChartResults', 'VargaMethod') IS NULL
ALTER TABLE dbo.tbl_ChartResults ADD
    VargaMethod       VARCHAR(40)   NULL,
    AyanamshaDegrees  DECIMAL(9,6)  NULL,
    SiderealTimeHours DECIMAL(9,6)  NULL;
GO
-- tbl_Chart_KeyDetails.VargaLongitudeDegrees (nullable first) --------
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'VargaLongitudeDegrees') IS NULL
ALTER TABLE dbo.tbl_Chart_KeyDetails ADD VargaLongitudeDegrees DECIMAL(9,6) NULL;
GO
-- Backfill: (NirayanaLongitudeDegrees * N) mod 360, N by chart type --
UPDATE kd
   SET kd.VargaLongitudeDegrees =
       CAST( ( (kd.NirayanaLongitudeDegrees *
                CASE cr.ChartType
                     WHEN 'D1'  THEN 1  WHEN 'D2'  THEN 2  WHEN 'D6'  THEN 6
                     WHEN 'D9'  THEN 9  WHEN 'D10' THEN 10 WHEN 'D11' THEN 11
                END) % 360.0 + 360.0) % 360.0 AS DECIMAL(9,6))
  FROM dbo.tbl_Chart_KeyDetails kd
  JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
 WHERE kd.VargaLongitudeDegrees IS NULL
   AND cr.ChartType IN ('D1','D2','D6','D9','D10','D11');
GO
-- Any leftover NULL means an unexpected chart type has KeyDetails rows.
IF EXISTS (SELECT 1 FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees IS NULL)
    THROW 50010, 'tbl_Chart_KeyDetails still has NULL VargaLongitudeDegrees after backfill — an unexpected ChartType has rows; extend the CASE.', 1;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails') AND name = 'VargaLongitudeDegrees' AND is_nullable = 1)
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN VargaLongitudeDegrees DECIMAL(9,6) NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KeyDetails_VargaLongitude')
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CONSTRAINT CK_KeyDetails_VargaLongitude
        CHECK (VargaLongitudeDegrees >= 0 AND VargaLongitudeDegrees < 360);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '12_add_varga_provenance_columns.sql', 'ChartResults provenance cols + KeyDetails.VargaLongitudeDegrees NOT NULL + CHECK'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '12_add_varga_provenance_columns.sql');
GO
PRINT '12 applied: provenance + VargaLongitudeDegrees.';
GO
```

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/12_add_varga_provenance_columns.sql`
Expected: `12 applied: provenance + VargaLongitudeDegrees.` If it `THROW`s on 50010, an unexpected chart type has KeyDetails rows — run Step 1's query, add that type to the `CASE`, re-apply.

- [ ] **Step 4: Verify the backfill**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees < 0 OR VargaLongitudeDegrees >= 360) AS out_of_range,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId WHERE cr.ChartType = 'D1' AND ABS(kd.VargaLongitudeDegrees - kd.NirayanaLongitudeDegrees) > 0.0001) AS d1_mismatch,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId WHERE cr.ChartType = 'D9' AND ABS(kd.VargaLongitudeDegrees - (((kd.NirayanaLongitudeDegrees * 9) % 360 + 360) % 360)) > 0.001) AS d9_mismatch"
```
Expected: `out_of_range = 0`, `d1_mismatch = 0`, `d9_mismatch = 0`.

- [ ] **Step 5: Re-apply for idempotency** — same `PRINT`; column already `NOT NULL`, CHECK already present, no re-add.

- [ ] **Step 6: Commit**

```bash
git add db/12_add_varga_provenance_columns.sql
git commit -m "$(printf 'feat(db): ChartResults provenance cols + KeyDetails.VargaLongitudeDegrees\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 4: Un-gate `DegreesInSignDecimal` / `Display` for existing varga rows

**Files:**
- Create: `db/13_ungate_degrees_in_sign.sql`

**Interfaces:**
- Consumes: `tbl_Chart_KeyDetails.VargaLongitudeDegrees` (Task 3).
- Produces: every existing non-D1 `tbl_Chart_KeyDetails` row has `DegreesInSignDecimal = VargaLongitudeDegrees % 30` (rounded to 4 dp) and a matching `DegreesInSignDisplay` string. (Going forward `ChartAnalyzer` writes these directly — Task 14.)

- [ ] **Step 1: Confirm the current state**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT cr.ChartType, COUNT(*) AS rows, COUNT(kd.DegreesInSignDecimal) AS has_decimal FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId GROUP BY cr.ChartType ORDER BY cr.ChartType"`
Expected: for `D2/D6/D9/D10/D11`, `has_decimal` ≈ 0 (D1-only gate today); `D1` has_decimal ≈ rows.

- [ ] **Step 2: Check the `DegreesInSignDisplay` format the app produces**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT TOP 5 DegreesInSignDisplay FROM dbo.tbl_Chart_KeyDetails WHERE DegreesInSignDisplay IS NOT NULL"`
Note the exact form (expected: `D°M'S"` e.g. `12°34'56"`). T-SQL will reproduce it with `FORMAT`/string math below; match whatever you see.

- [ ] **Step 3: Write `db/13_ungate_degrees_in_sign.sql`**

```sql
-- =====================================================================
-- 13 — Un-gate DegreesInSignDecimal / DegreesInSignDisplay from D1-only.
-- For every existing NON-D1 KeyDetails row, set them from
-- VargaLongitudeDegrees % 30 (PyJHora's d_long). D1 rows are left as-is.
-- Going forward ChartAnalyzer writes these directly (Plan-A Task 14).
-- Idempotent (re-run recomputes to the same values).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/13_ungate_degrees_in_sign.sql
-- =====================================================================
USE [ikiastrro];
GO
;WITH v AS (
    SELECT kd.Id,
           CAST(kd.VargaLongitudeDegrees % 30.0 AS DECIMAL(7,4)) AS deg
    FROM dbo.tbl_Chart_KeyDetails kd
    JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
    WHERE cr.ChartType <> 'D1'
)
UPDATE kd
   SET kd.DegreesInSignDecimal = v.deg,
       kd.DegreesInSignDisplay =
           CAST(FLOOR(v.deg) AS VARCHAR(2)) + N'°' +
           RIGHT('0' + CAST(FLOOR((v.deg - FLOOR(v.deg)) * 60) AS VARCHAR(2)), 2) + N'''' +
           RIGHT('0' + CAST(FLOOR((((v.deg - FLOOR(v.deg)) * 60) - FLOOR((v.deg - FLOOR(v.deg)) * 60)) * 60) AS VARCHAR(2)), 2) + N'"'
  FROM dbo.tbl_Chart_KeyDetails kd
  JOIN v ON v.Id = kd.Id;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '13_ungate_degrees_in_sign.sql', 'DegreesInSignDecimal/Display populated for existing non-D1 rows from VargaLongitudeDegrees'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '13_ungate_degrees_in_sign.sql');
GO
PRINT '13 applied: degrees-in-sign un-gated for existing varga rows.';
GO
```

- [ ] **Step 4: If Step 2 showed a different display format** (e.g. no leading zeros, or `′″` prime glyphs), adjust the string expression to match exactly. The **decimal** column is the source of truth; the display string only needs to be consistent with what `ChartAnalyzer` will emit in Task 14 (which uses `AstroMath.FormatDegreesMinutesSeconds`). If in doubt, set `DegreesInSignDisplay = NULL` here and let Task 16's `recompute-keydetails` regenerate every row through `ChartAnalyzer` — note that choice in the commit message.

- [ ] **Step 5: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/13_ungate_degrees_in_sign.sql`
Expected: `13 applied: degrees-in-sign un-gated for existing varga rows.`

- [ ] **Step 6: Verify**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId WHERE cr.ChartType<>'D1' AND kd.DegreesInSignDecimal IS NULL) AS still_null,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE DegreesInSignDecimal IS NOT NULL AND (DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30)) AS out_of_range,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId WHERE cr.ChartType<>'D1' AND ABS(kd.DegreesInSignDecimal - (kd.VargaLongitudeDegrees % 30)) > 0.0002) AS mismatch"
```
Expected: `still_null = 0`, `out_of_range = 0`, `mismatch = 0`.

- [ ] **Step 7: Run `verify-schema`** (its `DegreesInSignDecimal IS NULL OR (>=0 AND <30)` check must still pass, now with far fewer NULLs)

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Expected: `verify-schema: ALL PASS`.

- [ ] **Step 8: Commit**

```bash
git add db/13_ungate_degrees_in_sign.sql
git commit -m "$(printf 'feat(db): un-gate DegreesInSign* for existing varga rows from VargaLongitudeDegrees\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

**PHASE 1 CHECKPOINT:** `SchemaMigrations` lists `01`–`13`. `tbl_Dim_ChartType` has 21 rows, `tbl_Rule_VargaScheme` has 20, `tbl_Chart_KeyDetails.VargaLongitudeDegrees` is `NOT NULL` + range-checked, `DegreesInSignDecimal` populated for every existing row. `verify-schema` ALL PASS. No C# changed yet — the app still computes D1/D2/D6/D9/D10/D11 exactly as before.

---

# PHASE 2 — CORE: VARGA SIGN RULES

## Task 5: `IVargaSignRule` + `LinearVargaSignRule` (D3 / D4 / D12 / D60)

**Files:**
- Create: `src/Ikiastrro.Core/Astro/IVargaSignRule.cs`
- Create: `src/Ikiastrro.Core/Astro/LinearVargaSignRule.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (the `verify-vargas` block)

**Interfaces:**
- Produces:
  - `Ikiastrro.Core.Astro.IVargaSignRule` — `ZodiacName SignFor(double siderealLongitude)`.
  - `Ikiastrro.Core.Astro.LinearVargaSignRule` — `ctor(int factor, int stride)`; `SignFor(lon)` returns `(ZodiacName)(((sign + l*stride) % 12 + 12) % 12)` with `sign`, `l` per Global Constraints.

- [ ] **Step 1: Add the failing `verify-vargas` checks first**

In `src/Ikiastrro.Cli/Program.cs`, inside the `if (args.Length > 0 && args[0] == "verify-vargas")` block, immediately after the existing `// D11 Rudramsa` checks, add:

```csharp
    // --- IVargaSignRule unit checks (hand-computed) ---
    // D3 Drekkana: (sign + l*4)%12, l = deg/10. Aries 12° -> l=1 -> Leo(4)
    Check("rule D3 Ar 12",  new LinearVargaSignRule(3, 4).SignFor(12),  ZodiacName.Leo);
    Check("rule D3 Ar 25",  new LinearVargaSignRule(3, 4).SignFor(25),  ZodiacName.Sagittarius); // l=2 -> 0+8
    Check("rule D3 Ta 2",   new LinearVargaSignRule(3, 4).SignFor(32),  ZodiacName.Taurus);      // sign=1,l=0
    // D4 Chaturthamsa: (sign + l*3)%12, l = deg/7.5. Aries 20° -> l=2 -> Libra(6)
    Check("rule D4 Ar 20",  new LinearVargaSignRule(4, 3).SignFor(20),  ZodiacName.Libra);
    Check("rule D4 Ar 3",   new LinearVargaSignRule(4, 3).SignFor(3),   ZodiacName.Aries);       // l=0
    // D12 Dwadasamsa: (sign + l)%12, l = deg/2.5. Aries 12° -> l=4 -> Leo(4)
    Check("rule D12 Ar 12", new LinearVargaSignRule(12, 1).SignFor(12), ZodiacName.Leo);
    Check("rule D12 Ta 29", new LinearVargaSignRule(12, 1).SignFor(59), ZodiacName.Aries);       // sign=1,l=11 -> 12%12
    // D60 Shashtyamsa: (sign + l)%12, l = deg/0.5. Aries 12° -> l=24 -> 24%12=0 -> Aries
    Check("rule D60 Ar 12", new LinearVargaSignRule(60, 1).SignFor(12), ZodiacName.Aries);
    Check("rule D60 Ar 1",  new LinearVargaSignRule(60, 1).SignFor(1),  ZodiacName.Aries);       // l=2 -> Gemini? recheck below
```

> Note: the last check (`D60 Ar 1°` → `l = 1/0.5 = 2` → `(0+2)%12 = Gemini`). Fix the expected value to `ZodiacName.Gemini` once you have confirmed the arithmetic — this step is where you *write down* the hand computation; correct any expected value that your own calculation disagrees with before moving on.

- [ ] **Step 2: Build — expect it to fail (no `LinearVargaSignRule`)**

Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj`
Expected: FAIL — `error CS0246: The type or namespace name 'LinearVargaSignRule' could not be found`.

- [ ] **Step 3: Write `src/Ikiastrro.Core/Astro/IVargaSignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// Maps a planet's real (nirayana) longitude to its sign in one divisional chart.
/// One implementation per varga scheme (see tbl_Rule_VargaScheme.SignRuleKey);
/// VargaSignRuleFactory builds the right one from a scheme row. Takes the full
/// 0-360° longitude so wrapper rules can defer to AstroMath.GetXSign unchanged.
/// </summary>
public interface IVargaSignRule
{
    ZodiacName SignFor(double siderealLongitude);
}
```

- [ ] **Step 4: Write `src/Ikiastrro.Core/Astro/LinearVargaSignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// The "l-th part, counted <c>stride</c> signs forward from the rasi sign" family:
/// D3 (factor 3, stride 4 → 1st/5th/9th), D4 (4, 3), D12 (12, 1), D60 (60, 1).
/// <c>l = floor(degreesInRasiSign / (30/factor))</c>; result = (rasiSign + l·stride) mod 12.
/// PyJHora: _drekkana_chart_parasara / _chaturthamsa_parasara / dwadasamsa_chart m1 /
/// shashtyamsa_chart m1.
/// </summary>
public sealed class LinearVargaSignRule : IVargaSignRule
{
    private readonly int _factor;
    private readonly int _stride;

    public LinearVargaSignRule(int factor, int stride)
    {
        _factor = factor;
        _stride = stride;
    }

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var rasiSign = (int)(lon / 30);
        var degInSign = lon % 30;
        var l = (int)(degInSign / (30.0 / _factor));
        var idx = ((rasiSign + l * _stride) % 12 + 12) % 12;
        return (ZodiacName)idx;
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj`
Expected: succeeds.

- [ ] **Step 6: Run `verify-vargas`**

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-vargas`
Expected: `verify-vargas: ALL PASS`. If a `rule D…` line fails, recompute that case by hand — the formula in `LinearVargaSignRule` is authoritative; fix the *expected* value in the `Check(...)` call (a wrong hand-computed expectation is the usual cause), not the rule, unless the rule genuinely disagrees with §3.2.

- [ ] **Step 7: Commit**

```bash
git add src/Ikiastrro.Core/Astro/IVargaSignRule.cs src/Ikiastrro.Core/Astro/LinearVargaSignRule.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): IVargaSignRule + LinearVargaSignRule (D3/D4/D12/D60) + verify-vargas checks\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 6: Special rules batch A — D7, D8, D24, D27

**Files:**
- Create: `src/Ikiastrro.Core/Astro/SaptamsaD7SignRule.cs`, `AshtamsaD8SignRule.cs`, `SiddhamsaD24SignRule.cs`, `NakshatramsaD27SignRule.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Produces four `IVargaSignRule` classes in `Ikiastrro.Core.Astro`. Each `SignFor(lon)` computes `rasiSign`, `degInSign`, `l = (int)(degInSign / (30.0/N))` and returns `(ZodiacName)(((expr) % 12 + 12) % 12)` where `expr` is:
  - **SaptamsaD7** (N=7): `rasiSign % 2 == 0 ? rasiSign + l : rasiSign + l + 6`
  - **AshtamsaD8** (N=8): `rasiSign % 3 == 0 ? l : rasiSign % 3 == 1 ? l + 8 : l + 4` (movable/fixed/dual → Aries/Sagittarius/Leo)
  - **SiddhamsaD24** (N=24): `rasiSign % 2 == 0 ? 4 + l : 3 + l` (odd from Leo, even from Cancer)
  - **NakshatramsaD27** (N=27): `(rasiSign % 4) switch { 0 => l, 1 => l + 3, 2 => l + 6, _ => l + 9 }` (fire/earth/air/water → Aries/Cancer/Libra/Capricorn)

- [ ] **Step 1: Add failing `verify-vargas` checks** (after the Task-5 block)

```csharp
    // D7 Saptamsa: odd -> (sign+l)%12 ; even -> (sign+l+6)%12 ; l = deg/(30/7)
    Check("rule D7 Ar 2",   new SaptamsaD7SignRule().SignFor(2),   ZodiacName.Aries);      // sign0 even-idx, l0
    Check("rule D7 Ta 2",   new SaptamsaD7SignRule().SignFor(32),  ZodiacName.Scorpio);    // sign1 (even sign) l0 -> 1+0+6=7
    // D8 Ashtamsa: movable->l ; fixed->l+8 ; dual->l+4 ; l = deg/3.75
    Check("rule D8 Ar 2",   new AshtamsaD8SignRule().SignFor(2),   ZodiacName.Aries);      // sign0 movable l0
    Check("rule D8 Ta 2",   new AshtamsaD8SignRule().SignFor(32),  ZodiacName.Sagittarius);// sign1 fixed l0 -> 8
    Check("rule D8 Ge 2",   new AshtamsaD8SignRule().SignFor(62),  ZodiacName.Leo);        // sign2 dual l0 -> 4
    // D24 Siddhamsa: odd -> 4+l ; even -> 3+l ; l = deg/1.25
    Check("rule D24 Ar 1",  new SiddhamsaD24SignRule().SignFor(1),  ZodiacName.Leo);       // sign0 odd l0 -> 4
    Check("rule D24 Ta 1",  new SiddhamsaD24SignRule().SignFor(31), ZodiacName.Cancer);    // sign1 even l0 -> 3
    // D27 Nakshatramsa: fire->l ; earth->l+3 ; air->l+6 ; water->l+9 ; l = deg/(30/27)
    Check("rule D27 Ar 1",  new NakshatramsaD27SignRule().SignFor(1),  ZodiacName.Aries);   // Aries=fire, l0
    Check("rule D27 Ta 1",  new NakshatramsaD27SignRule().SignFor(31), ZodiacName.Cancer);  // Taurus=earth, l0 -> 3
    Check("rule D27 Ge 1",  new NakshatramsaD27SignRule().SignFor(61), ZodiacName.Libra);   // Gemini=air, l0 -> 6
    Check("rule D27 Cn 1",  new NakshatramsaD27SignRule().SignFor(91), ZodiacName.Capricornus); // Cancer=water, l0 -> 9
```

- [ ] **Step 2: Build — expect FAIL** (missing types). Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj` → `error CS0246`.

- [ ] **Step 3: Write `SaptamsaD7SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D7 Saptamsa — odd signs count the l-th part from the sign itself,
/// even signs from the 7th from it. PyJHora saptamsa_chart method 1
/// (PARASARA_EVEN_START_7TH_GO_FORWARD).</summary>
public sealed class SaptamsaD7SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 7));
        var expr = sign % 2 == 0 ? sign + l : sign + l + 6;
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 4: Write `AshtamsaD8SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D8 Ashtamsa — movable/fixed/dual signs count the l-th part from
/// Aries / Sagittarius / Leo. PyJHora ashtamsa_chart method 1.</summary>
public sealed class AshtamsaD8SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 8));
        var expr = (sign % 3) switch
        {
            0 => l,          // movable -> from Aries
            1 => l + 8,       // fixed   -> from Sagittarius
            _ => l + 4,       // dual    -> from Leo
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 5: Write `SiddhamsaD24SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D24 Siddhamsa (Chaturvimsamsa) — odd signs count the l-th part from
/// Leo, even signs from Cancer. PyJHora chaturvimsamsa_chart method 1.</summary>
public sealed class SiddhamsaD24SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 24));
        var expr = sign % 2 == 0 ? 4 + l : 3 + l;   // 4 = Leo, 3 = Cancer
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 6: Write `NakshatramsaD27SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D27 Nakshatramsa (Bhamsa) — fire/earth/air/water signs count the
/// l-th part from Aries / Cancer / Libra / Capricorn. PyJHora
/// nakshatramsa_chart method 1.</summary>
public sealed class NakshatramsaD27SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 27));
        var expr = (sign % 4) switch
        {
            0 => l,          // fire  -> Aries
            1 => l + 3,       // earth -> Cancer
            2 => l + 6,       // air   -> Libra
            _ => l + 9,       // water -> Capricorn
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 7: Build + run `verify-vargas`**

Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj` then `dotnet run --project src/Ikiastrro.Cli -- verify-vargas`
Expected: succeeds; `verify-vargas: ALL PASS`. Fix any wrong hand-computed expected value (recompute `l` = `floor(degInSign / (30/N))` and the offset).

- [ ] **Step 8: Commit**

```bash
git add src/Ikiastrro.Core/Astro/SaptamsaD7SignRule.cs src/Ikiastrro.Core/Astro/AshtamsaD8SignRule.cs src/Ikiastrro.Core/Astro/SiddhamsaD24SignRule.cs src/Ikiastrro.Core/Astro/NakshatramsaD27SignRule.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): D7/D8/D24/D27 varga sign rules + verify-vargas checks\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 7: Special rules batch B — D16, D20, D40, D45

**Files:**
- Create: `src/Ikiastrro.Core/Astro/ShodasamsaD16SignRule.cs`, `VimsamsaD20SignRule.cs`, `KhavedamsaD40SignRule.cs`, `AkshavedamsaD45SignRule.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Produces four `IVargaSignRule` classes. `expr` (mod 12) with `l = (int)(degInSign / (30.0/N))`:
  - **ShodasamsaD16** (N=16): `(sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 }` (movable/fixed/dual → Aries/Leo/Sagittarius)
  - **VimsamsaD20** (N=20): `(sign % 3) switch { 0 => l, 2 => l + 4, _ => l + 8 }` (movable/**dual**/fixed → Aries/Leo/Sagittarius — dual & fixed offsets swapped vs D16)
  - **KhavedamsaD40** (N=40): `sign % 2 == 0 ? l : l + 6` (odd from Aries, even from Libra)
  - **AkshavedamsaD45** (N=45): identical shape to D16 — `(sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 }`

- [ ] **Step 1: Add failing `verify-vargas` checks**

```csharp
    // D16 Shodasamsa: movable->l ; fixed->l+4 ; dual->l+8 ; l = deg/1.875
    Check("rule D16 Ar 1",  new ShodasamsaD16SignRule().SignFor(1),  ZodiacName.Aries);
    Check("rule D16 Ta 1",  new ShodasamsaD16SignRule().SignFor(31), ZodiacName.Leo);          // fixed l0 -> 4
    Check("rule D16 Ge 1",  new ShodasamsaD16SignRule().SignFor(61), ZodiacName.Sagittarius);  // dual l0 -> 8
    // D20 Vimsamsa: movable->l ; dual->l+4 ; fixed->l+8 ; l = deg/1.5
    Check("rule D20 Ar 1",  new VimsamsaD20SignRule().SignFor(1),  ZodiacName.Aries);
    Check("rule D20 Ta 1",  new VimsamsaD20SignRule().SignFor(31), ZodiacName.Sagittarius);    // fixed l0 -> 8
    Check("rule D20 Ge 1",  new VimsamsaD20SignRule().SignFor(61), ZodiacName.Leo);            // dual l0 -> 4
    // D40 Khavedamsa: odd->l ; even->l+6 ; l = deg/0.75
    Check("rule D40 Ar 1",  new KhavedamsaD40SignRule().SignFor(1),  ZodiacName.Gemini);       // sign0 odd, l = 1/0.75 = 1 -> 1? recheck
    Check("rule D40 Ta 1",  new KhavedamsaD40SignRule().SignFor(31), ZodiacName.Libra);        // even l0 -> 0+6
    // D45 Akshavedamsa: same shape as D16 ; l = deg/(30/45)
    Check("rule D45 Ar 1",  new AkshavedamsaD45SignRule().SignFor(1),  ZodiacName.Aries);      // l = 1/0.6667 = 1 -> recheck
    Check("rule D45 Ta 1",  new AkshavedamsaD45SignRule().SignFor(31), ZodiacName.Leo);        // fixed l0 -> 4
```

> Recompute the `l` for each `SignFor(1)` case (small degree, small N-part) and set the expected sign to match your arithmetic before Step 6.

- [ ] **Step 2: Build — expect FAIL.**

- [ ] **Step 3: Write `ShodasamsaD16SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D16 Shodasamsa (Kalamsa) — movable/fixed/dual signs count the l-th
/// part from Aries / Leo / Sagittarius. PyJHora shodasamsa_chart method 1.</summary>
public sealed class ShodasamsaD16SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 16));
        var expr = (sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 4: Write `VimsamsaD20SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D20 Vimsamsa — movable/dual/fixed signs count the l-th part from
/// Aries / Leo / Sagittarius (dual and fixed offsets are swapped relative to
/// D16/D45). PyJHora vimsamsa_chart method 1.</summary>
public sealed class VimsamsaD20SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 20));
        var expr = (sign % 3) switch
        {
            0 => l,          // movable -> Aries
            2 => l + 4,       // dual    -> Leo
            _ => l + 8,       // fixed   -> Sagittarius
        };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 5: Write `KhavedamsaD40SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D40 Khavedamsa (Chatvarimsamsa) — odd signs count the l-th part from
/// Aries, even signs from Libra. PyJHora khavedamsa_chart method 1.</summary>
public sealed class KhavedamsaD40SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 40));
        var expr = sign % 2 == 0 ? l : l + 6;
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 6: Write `AkshavedamsaD45SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D45 Akshavedamsa — movable/fixed/dual signs count the l-th part from
/// Aries / Leo / Sagittarius (same shape as D16). PyJHora akshavedamsa_chart
/// method 1.</summary>
public sealed class AkshavedamsaD45SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / (30.0 / 45));
        var expr = (sign % 3) switch { 0 => l, 1 => l + 4, _ => l + 8 };
        return (ZodiacName)((expr % 12 + 12) % 12);
    }
}
```

- [ ] **Step 7: Build + `verify-vargas`** → ALL PASS (fix wrong expected values).

- [ ] **Step 8: Commit**

```bash
git add src/Ikiastrro.Core/Astro/ShodasamsaD16SignRule.cs src/Ikiastrro.Core/Astro/VimsamsaD20SignRule.cs src/Ikiastrro.Core/Astro/KhavedamsaD40SignRule.cs src/Ikiastrro.Core/Astro/AkshavedamsaD45SignRule.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): D16/D20/D40/D45 varga sign rules + verify-vargas checks\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 8: Special rules batch C — D5 (lookup) and D30 (unequal 5-part)

**Files:**
- Create: `src/Ikiastrro.Core/Astro/PanchamsaD5SignRule.cs`, `TrimsamsaD30SignRule.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- **PanchamsaD5** (N=5): `l = (int)(degInSign / 6)`; odd sign → `new[]{0,10,8,2,6}[l]`; even sign → `new[]{1,5,11,9,7}[l]` (PyJHora `panchamsa_odd_signs` / `panchamsa_even_signs`).
- **TrimsamsaD30**: sign is chosen from `degInSign` (0–30) against a 5-band table, not an `l`-part:
  - odd sign: `[0,5)→Aries(0)`, `[5,10)→Aquarius(10)`, `[10,18)→Sagittarius(8)`, `[18,25)→Gemini(2)`, `[25,30]→Libra(6)`
  - even sign: `[0,5)→Taurus(1)`, `[5,12)→Virgo(5)`, `[12,20)→Pisces(11)`, `[20,25)→Capricornus(9)`, `[25,30]→Scorpio(7)`
  (PyJHora `trimsamsa_chart` method 1: `odd=[(0,5,0),(5,10,10),(10,18,8),(18,25,2),(25,30,6)]`, `even=[(0,5,1),(5,12,5),(12,20,11),(20,25,9),(25,30,7)]`.)

- [ ] **Step 1: Add failing `verify-vargas` checks**

```csharp
    // D5 Panchamsa: odd -> [Ar,Aq,Sg,Ge,Li][l] ; even -> [Ta,Vi,Pi,Cp,Sc][l] ; l = deg/6
    Check("rule D5 Ar 3",   new PanchamsaD5SignRule().SignFor(3),   ZodiacName.Aries);        // odd l0
    Check("rule D5 Ar 9",   new PanchamsaD5SignRule().SignFor(9),   ZodiacName.Aquarius);     // odd l1
    Check("rule D5 Ta 3",   new PanchamsaD5SignRule().SignFor(33),  ZodiacName.Taurus);       // even l0
    Check("rule D5 Ta 27",  new PanchamsaD5SignRule().SignFor(57),  ZodiacName.Scorpio);      // even l4
    // D30 Trimsamsa: bands on degrees-in-sign (not l-parts)
    Check("rule D30 Ar 3",  new TrimsamsaD30SignRule().SignFor(3),   ZodiacName.Aries);       // odd [0,5)
    Check("rule D30 Ar 7",  new TrimsamsaD30SignRule().SignFor(7),   ZodiacName.Aquarius);    // odd [5,10)
    Check("rule D30 Ar 15", new TrimsamsaD30SignRule().SignFor(15),  ZodiacName.Sagittarius); // odd [10,18)
    Check("rule D30 Ar 22", new TrimsamsaD30SignRule().SignFor(22),  ZodiacName.Gemini);      // odd [18,25)
    Check("rule D30 Ar 28", new TrimsamsaD30SignRule().SignFor(28),  ZodiacName.Libra);       // odd [25,30]
    Check("rule D30 Ta 3",  new TrimsamsaD30SignRule().SignFor(33),  ZodiacName.Taurus);      // even [0,5)
    Check("rule D30 Ta 15", new TrimsamsaD30SignRule().SignFor(45),  ZodiacName.Pisces);      // even [12,20)
    Check("rule D30 Ta 28", new TrimsamsaD30SignRule().SignFor(58),  ZodiacName.Scorpio);     // even [25,30]
```

- [ ] **Step 2: Build — expect FAIL.**

- [ ] **Step 3: Write `PanchamsaD5SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D5 Panchamsa — five equal 6° parts. Odd signs map the l-th part to
/// Aries/Aquarius/Sagittarius/Gemini/Libra; even signs to
/// Taurus/Virgo/Pisces/Capricorn/Scorpio. PyJHora panchamsa_chart method 1
/// (panchamsa_odd_signs / panchamsa_even_signs).</summary>
public sealed class PanchamsaD5SignRule : IVargaSignRule
{
    private static readonly int[] Odd  = { 0, 10, 8, 2, 6 };
    private static readonly int[] Even = { 1, 5, 11, 9, 7 };

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var l = (int)((lon % 30) / 6.0);
        if (l > 4) l = 4;   // guard the 30.0° boundary
        return (ZodiacName)(sign % 2 == 0 ? Odd[l] : Even[l]);
    }
}
```

- [ ] **Step 4: Write `TrimsamsaD30SignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D30 Trimsamsa — the classical unequal five-part scheme. The sign is
/// chosen by which degree band of the rasi sign the planet falls in (odd vs
/// even sign), not by an equal l-part. PyJHora trimsamsa_chart method 1.</summary>
public sealed class TrimsamsaD30SignRule : IVargaSignRule
{
    // (upperExclusive, signIndex) — last band inclusive of 30.0 via the final entry.
    private static readonly (double Max, int Sign)[] Odd =
        { (5, 0), (10, 10), (18, 8), (25, 2), (30.0001, 6) };
    private static readonly (double Max, int Sign)[] Even =
        { (5, 1), (12, 5), (20, 11), (25, 9), (30.0001, 7) };

    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var deg = lon % 30;
        var table = sign % 2 == 0 ? Odd : Even;
        foreach (var (max, s) in table)
            if (deg < max) return (ZodiacName)s;
        return (ZodiacName)table[^1].Sign;   // unreachable; deg is always < 30
    }
}
```

- [ ] **Step 5: Build + `verify-vargas`** → ALL PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Core/Astro/PanchamsaD5SignRule.cs src/Ikiastrro.Core/Astro/TrimsamsaD30SignRule.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): D5 (lookup) + D30 (unequal 5-part) varga sign rules\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 9: Wrapper rules (D2, D6, D9, D10, D11) + D2-US

**Files:**
- Create: `src/Ikiastrro.Core/Astro/HoraD2ClassicSignRule.cs`, `ShashtamsaD6SignRule.cs`, `NavamsaD9SignRule.cs`, `DasamsaD10SignRule.cs`, `RudramsaD11SignRule.cs`, `HoraD2UmaShambuSignRule.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Five one-line wrappers, each `SignFor(lon) => AstroMath.GetXSign(lon)`:
  - `HoraD2ClassicSignRule` → `AstroMath.GetHoraSign`
  - `ShashtamsaD6SignRule` → `AstroMath.GetShashtamsaSign`
  - `NavamsaD9SignRule` → `AstroMath.GetNavamsaSign`
  - `DasamsaD10SignRule` → `AstroMath.GetDasamsaSign`
  - `RudramsaD11SignRule` → `AstroMath.GetRudramsaSign`
- `HoraD2UmaShambuSignRule` — Parasara Uma Shambu. PyJHora `hora_chart` default: it maps each of the 24 half-sign-halves across all 12 signs via `parivritti`-style cycling, **not** the two-sign Cn/Le. Port: `l = (int)(degInSign / 15)` (0 or 1); `sign = (int)(lon/30)`; result index = `(sign * 2 + l) % 12` — i.e. a parivritti-cyclic D2. Confirm against the export's `D-2 (US)` grid in Task 17; if it diverges, re-derive from `_research/PyJHora/src/jhora/horoscope/chart/charts.py` `hora_chart` and update this rule + `tbl_Rule_VargaScheme.MethodSource`.

- [ ] **Step 1: Add `verify-vargas` checks** — the five wrappers must reproduce the existing direct-call checks exactly:

```csharp
    // Wrapper rules must match the AstroMath methods they delegate to
    Check("wrap D2 Ar 10",  new HoraD2ClassicSignRule().SignFor(10),   AstroMath.GetHoraSign(10));
    Check("wrap D2 Ta 20",  new HoraD2ClassicSignRule().SignFor(50),   AstroMath.GetHoraSign(50));
    Check("wrap D6 Ar 2",   new ShashtamsaD6SignRule().SignFor(2),     AstroMath.GetShashtamsaSign(2));
    Check("wrap D6 Ta 27",  new ShashtamsaD6SignRule().SignFor(57),    AstroMath.GetShashtamsaSign(57));
    Check("wrap D9 Ar 3",   new NavamsaD9SignRule().SignFor(3),        AstroMath.GetNavamsaSign(3));
    Check("wrap D9 Sc 21",  new NavamsaD9SignRule().SignFor(231),      AstroMath.GetNavamsaSign(231));
    Check("wrap D10 Ar 28", new DasamsaD10SignRule().SignFor(28),      AstroMath.GetDasamsaSign(28));
    Check("wrap D11 Ta 29", new RudramsaD11SignRule().SignFor(59),     AstroMath.GetRudramsaSign(59));
    // D2-US (Uma Shambu) — parivritti-cyclic D2 (confirmed vs D-2 (US) grid in Task 17)
    Check("rule D2US Ar 5",  new HoraD2UmaShambuSignRule().SignFor(5),  ZodiacName.Aries);   // sign0 half0 -> 0
    Check("rule D2US Ar 20", new HoraD2UmaShambuSignRule().SignFor(20), ZodiacName.Taurus);  // sign0 half1 -> 1
    Check("rule D2US Ta 5",  new HoraD2UmaShambuSignRule().SignFor(35), ZodiacName.Gemini);  // sign1 half0 -> 2
```

- [ ] **Step 2: Build — expect FAIL.**

- [ ] **Step 3: Write the five wrapper rules**

`HoraD2ClassicSignRule.cs`:
```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D2 classical two-sign Hora (odd 1st-half → Leo, 2nd-half → Cancer;
/// even reversed). Wraps AstroMath.GetHoraSign so the maths stays in one place.</summary>
public sealed class HoraD2ClassicSignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetHoraSign(siderealLongitude);
}
```

`ShashtamsaD6SignRule.cs`:
```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D6 Shashtamsa — wraps AstroMath.GetShashtamsaSign (odd → Aries..Virgo,
/// even → Libra..Pisces).</summary>
public sealed class ShashtamsaD6SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetShashtamsaSign(siderealLongitude);
}
```

`NavamsaD9SignRule.cs`:
```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D9 Navamsa — wraps AstroMath.GetNavamsaSign.</summary>
public sealed class NavamsaD9SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetNavamsaSign(siderealLongitude);
}
```

`DasamsaD10SignRule.cs`:
```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D10 Dasamsa — wraps AstroMath.GetDasamsaSign (odd from self, even from 9th).</summary>
public sealed class DasamsaD10SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetDasamsaSign(siderealLongitude);
}
```

`RudramsaD11SignRule.cs`:
```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>D11 Rudramsa — wraps AstroMath.GetRudramsaSign (Sanjay Rath method).</summary>
public sealed class RudramsaD11SignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude) => AstroMath.GetRudramsaSign(siderealLongitude);
}
```

- [ ] **Step 4: Write `HoraD2UmaShambuSignRule.cs`**

```csharp
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// D2 Uma Shambu Hora — Jagannatha Hora's default D2 (tagged "D-2 (US)" in its
/// export). Unlike the classical two-sign Hora it distributes across all 12
/// signs: a parivritti-cyclic D2 (each 15° half is the next sign forward).
/// index = (rasiSign*2 + half) mod 12, half ∈ {0,1}. Ported from PyJHora
/// hora_chart (d2 default); confirmed "closest" against the Ramakrishnan export
/// grid — see tbl_Rule_VargaScheme.MethodSource.
/// </summary>
public sealed class HoraD2UmaShambuSignRule : IVargaSignRule
{
    public ZodiacName SignFor(double siderealLongitude)
    {
        var lon = AstroMath.Normalize(siderealLongitude);
        var sign = (int)(lon / 30);
        var half = (lon % 30) < 15.0 ? 0 : 1;
        return (ZodiacName)(((sign * 2 + half) % 12 + 12) % 12);
    }
}
```

- [ ] **Step 5: Build + `verify-vargas`** → ALL PASS. The `wrap D…` checks compare the rule to `AstroMath.GetXSign` directly, so they cannot fail unless a wrapper has a typo.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Core/Astro/HoraD2ClassicSignRule.cs src/Ikiastrro.Core/Astro/ShashtamsaD6SignRule.cs src/Ikiastrro.Core/Astro/NavamsaD9SignRule.cs src/Ikiastrro.Core/Astro/DasamsaD10SignRule.cs src/Ikiastrro.Core/Astro/RudramsaD11SignRule.cs src/Ikiastrro.Core/Astro/HoraD2UmaShambuSignRule.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): D2/D6/D9/D10/D11 wrapper sign rules + D2-US Uma Shambu\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 10: `VargaSignRuleFactory`

**Files:**
- Create: `src/Ikiastrro.Core/Astro/VargaSignRuleFactory.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Produces: `Ikiastrro.Core.Astro.VargaSignRuleFactory.For(string signRuleKey, int divisionFactor) → IVargaSignRule`. The one place a `tbl_Rule_VargaScheme.SignRuleKey` string maps to a class. Throws `InvalidOperationException` for an unknown key.

- [ ] **Step 1: Add a `verify-vargas` check that every seeded key resolves**

```csharp
    // Every SignRuleKey seeded in tbl_Rule_VargaScheme must resolve
    string[] seededKeys =
    {
        "HoraD2Classic","HoraD2UmaShambu","DrekkanaD3","ChaturthamsaD4","PanchamsaD5",
        "ShashtamsaD6","SaptamsaD7","AshtamsaD8","NavamsaD9","DasamsaD10","RudramsaD11",
        "DwadasamsaD12","ShodasamsaD16","VimsamsaD20","SiddhamsaD24","NakshatramsaD27",
        "TrimsamsaD30","KhavedamsaD40","AkshavedamsaD45","ShashtyamsaD60"
    };
    int[] seededFactors = { 2,2,3,4,5,6,7,8,9,10,11,12,16,20,24,27,30,40,45,60 };
    var factoryFailures = 0;
    for (var i = 0; i < seededKeys.Length; i++)
    {
        try { _ = VargaSignRuleFactory.For(seededKeys[i], seededFactors[i]).SignFor(15.0); }
        catch (Exception ex) { Console.WriteLine($"  [FAIL] factory {seededKeys[i]}: {ex.Message}"); factoryFailures++; }
    }
    Console.WriteLine($"  [{(factoryFailures == 0 ? "PASS" : "FAIL")}] VargaSignRuleFactory resolves all 20 seeded keys");
    if (factoryFailures > 0) failures += factoryFailures;
```

- [ ] **Step 2: Build — expect FAIL** (no `VargaSignRuleFactory`).

- [ ] **Step 3: Write `src/Ikiastrro.Core/Astro/VargaSignRuleFactory.cs`**

```csharp
namespace Ikiastrro.Core.Astro;

/// <summary>
/// Builds the IVargaSignRule for a tbl_Rule_VargaScheme.SignRuleKey. Pure — no
/// I/O. The single switch here is the contract with the seed data: every
/// SignRuleKey the migration inserts must have a case.
/// </summary>
public static class VargaSignRuleFactory
{
    public static IVargaSignRule For(string signRuleKey, int divisionFactor) => signRuleKey switch
    {
        "HoraD2Classic"    => new HoraD2ClassicSignRule(),
        "HoraD2UmaShambu"  => new HoraD2UmaShambuSignRule(),
        "DrekkanaD3"       => new LinearVargaSignRule(3, 4),
        "ChaturthamsaD4"   => new LinearVargaSignRule(4, 3),
        "PanchamsaD5"      => new PanchamsaD5SignRule(),
        "ShashtamsaD6"     => new ShashtamsaD6SignRule(),
        "SaptamsaD7"       => new SaptamsaD7SignRule(),
        "AshtamsaD8"       => new AshtamsaD8SignRule(),
        "NavamsaD9"        => new NavamsaD9SignRule(),
        "DasamsaD10"       => new DasamsaD10SignRule(),
        "RudramsaD11"      => new RudramsaD11SignRule(),
        "DwadasamsaD12"    => new LinearVargaSignRule(12, 1),
        "ShodasamsaD16"    => new ShodasamsaD16SignRule(),
        "VimsamsaD20"      => new VimsamsaD20SignRule(),
        "SiddhamsaD24"     => new SiddhamsaD24SignRule(),
        "NakshatramsaD27"  => new NakshatramsaD27SignRule(),
        "TrimsamsaD30"     => new TrimsamsaD30SignRule(),
        "KhavedamsaD40"    => new KhavedamsaD40SignRule(),
        "AkshavedamsaD45"  => new AkshavedamsaD45SignRule(),
        "ShashtyamsaD60"   => new LinearVargaSignRule(60, 1),
        _ => throw new InvalidOperationException(
            $"No IVargaSignRule for SignRuleKey '{signRuleKey}' (factor {divisionFactor}). " +
            "Add a case in VargaSignRuleFactory or fix the tbl_Rule_VargaScheme seed."),
    };
}
```

- [ ] **Step 4: Build + `verify-vargas`** → `[PASS] VargaSignRuleFactory resolves all 20 seeded keys`; overall `ALL PASS`.

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Core/Astro/VargaSignRuleFactory.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): VargaSignRuleFactory — SignRuleKey -> IVargaSignRule\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

**PHASE 2 CHECKPOINT:** 17 `IVargaSignRule` classes + a factory. `verify-vargas` ALL PASS with hand-computed checks for every rule and a resolve-check for every seeded key. Nothing wired into the chart pipeline yet.

---

# PHASE 3 — COMPUTER, CALCULATOR, WIRING

## Task 11: `VargaChartComputer`

**Files:**
- Create: `src/Ikiastrro.Core/Calculators/VargaChartComputer.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Consumes: `SwissEphemerisProvider.GetSiderealPositions`, `IVargaSignRule`, `AstroMath` (`GetVargaLongitude`, `CountFromSignToSign`, `FormatDegreesMinutesSeconds`, `GetDegreesInSign`), `BirthMomentFactory.Create`.
- Produces: `Ikiastrro.Core.Calculators.VargaChartComputer.Compute(BirthDetails birthDetails, int divisionFactor, IVargaSignRule rule) → ChartAnalysisInput`. The Ascendant is entry 0 (`Planet = "Ascendant"`); each of `PlanetNames.All9` follows. Every `PlanetPosition` has: `Sign` (rule), `VargaLongitudeDegrees = AstroMath.GetVargaLongitude(realLon, N)`, `DegreesInSign` (formatted `VargaLongitudeDegrees % 30`), `NirayanaLongitudeDegrees = realLon`, `EclipticLatitudeDegrees`/`SpeedLongitudeDegPerDay`/`IsRetrograde` passed through (null for the Ascendant), `HouseNumber = AstroMath.CountFromSignToSign(vargaLagnaSign, thisSign)`.

- [ ] **Step 1: Read the reference pattern**

Read `src/Ikiastrro.Core/Calculators/D9ChartComputer.cs` in full — `VargaChartComputer` is the same shape, generalized: the fixed `AstroMath.GetNavamsaSign` / `NavamsaDivisions` become the injected `rule` / `divisionFactor`.

- [ ] **Step 2: Add a failing `verify-vargas` equivalence check** (D9 via `VargaChartComputer` == D9 via `D9ChartComputer`, for a fixed longitude set)

```csharp
    // VargaChartComputer(D9 rule) must reproduce D9ChartComputer's signs
    {
        var d9rule = new NavamsaD9SignRule();
        var mism = 0;
        foreach (var lon in new[] { 8.2, 217.3, 3.95, 1.84, 173.7, 191.96, 41.99, 190.96, 103.06 })
            if (d9rule.SignFor(lon) != AstroMath.GetNavamsaSign(lon)) mism++;
        Check("VargaChartComputer D9-rule vs AstroMath (spot longitudes)", mism, 0);
    }
```

(This is a rule-level check standing in for the computer; the full computer-vs-`D9ChartComputer` comparison is done live in Task 13, once the generic path is wired.)

- [ ] **Step 3: Write `src/Ikiastrro.Core/Calculators/VargaChartComputer.cs`**

```csharp
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// Computes ANY divisional chart from an injected division factor + IVargaSignRule.
/// Replaces the per-varga D6/D9/D10/D11 ChartComputers: same shape (real longitude
/// + varga longitude + Whole-Sign house from the varga Lagna), only the sign rule
/// and the factor vary. Shared with ChartAnalyzer via ChartAnalysisInput.
/// </summary>
public static class VargaChartComputer
{
    public static ChartAnalysisInput Compute(BirthDetails birthDetails, int divisionFactor, IVargaSignRule rule)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = rule.SignFor(positions.AscendantLongitude);
        var lagnaVargaLon = AstroMath.GetVargaLongitude(positions.AscendantLongitude, divisionFactor);

        var planetPositions = new List<PlanetPosition>
        {
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = lagnaSign.ToString(),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                VargaLongitudeDegrees = lagnaVargaLon,
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(lagnaVargaLon % 30),
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var realLon = positions.PlanetLongitudes[planet];
            var vargaSign = rule.SignFor(realLon);
            var vargaLon = AstroMath.GetVargaLongitude(realLon, divisionFactor);
            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = vargaSign.ToString(),
                NirayanaLongitudeDegrees = realLon,
                EclipticLatitudeDegrees = positions.PlanetLatitudes[planet],
                SpeedLongitudeDegPerDay = positions.PlanetSpeeds[planet],
                VargaLongitudeDegrees = vargaLon,
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(vargaLon % 30),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, vargaSign),
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput(chartType: "", lagnaSign, planetPositions);
    }
}
```

> `chartType: ""` — the caller (`VargaCalculator`) supplies the real code. If `ChartAnalysisInput`'s ctor signature differs (positional `record`), match it; the `ChartType` field is set by `VargaCalculator` in Task 13, or pass it into `Compute` as a 4th arg if that reads cleaner — either is fine, but be consistent with how `VargaCalculator` builds its `ChartResult`.

- [ ] **Step 4: Build + `verify-vargas`** → ALL PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Core/Calculators/VargaChartComputer.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): VargaChartComputer — one computer for every divisional chart\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 12: `VargaScheme` model + `VargaSchemeRepository`

**Files:**
- Create: `src/Ikiastrro.Core/Models/VargaScheme.cs`
- Create: `src/Ikiastrro.Data/VargaSchemeRepository.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

**Interfaces:**
- Produces:
  - `Ikiastrro.Core.Models.VargaScheme` — `record VargaScheme(string ChartType, int DivisionFactor, string MethodCode, string SignRuleKind, string SignRuleKey)`.
  - `Ikiastrro.Data.VargaSchemeRepository` — `ctor(SqlConnectionFactory)`; `IReadOnlyList<VargaScheme> GetAll(int ruleSetId)`.

- [ ] **Step 1: Read a reference repository** — `src/Ikiastrro.Data/ChartTypeRepository.cs` (same shape: `SqlConnectionFactory` ctor, one `GetAll` query, Dapper `Query<T>`).

- [ ] **Step 2: Add a failing `verify-vargas` check** (repo returns 20 rows, every SignRuleKey resolves via the factory)

```csharp
    // VargaSchemeRepository: 20 rows for RuleSetId 1, every one resolves
    {
        var schemes = new VargaSchemeRepository(connectionFactory).GetAll(1);
        Check("VargaSchemeRepository row count", schemes.Count, 20);
        var bad = 0;
        foreach (var s in schemes)
            try { _ = VargaSignRuleFactory.For(s.SignRuleKey, s.DivisionFactor); }
            catch { bad++; }
        Check("every scheme SignRuleKey resolves", bad, 0);
    }
```

(`connectionFactory` is already in scope in `Program.cs` — the schema-normalization `verify-schema` block uses it.)

- [ ] **Step 3: Build — expect FAIL.**

- [ ] **Step 4: Write `src/Ikiastrro.Core/Models/VargaScheme.cs`**

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>
/// One row of dbo.tbl_Rule_VargaScheme — how a varga chart type derives a
/// planet's varga sign, under one rule-set. SignRuleKey names the C# rule
/// (VargaSignRuleFactory); SignRuleKind is a descriptive tag ('Linear' /
/// 'Special'). D1 is NOT represented here (identity rasi).
/// </summary>
public record VargaScheme(
    string ChartType,
    int DivisionFactor,
    string MethodCode,
    string SignRuleKind,
    string SignRuleKey);
```

- [ ] **Step 5: Write `src/Ikiastrro.Data/VargaSchemeRepository.cs`**

```csharp
using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Rule_VargaScheme — the data-driven varga sign rules. Read once at
/// startup by ChartCalculationOrchestrator; also the table the Python comparison
/// layer will read to know how each varga sign was chosen.</summary>
public class VargaSchemeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public VargaSchemeRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<VargaScheme> GetAll(int ruleSetId)
    {
        const string sql = """
            SELECT ct.Code            AS ChartType,
                   vs.DivisionFactor  AS DivisionFactor,
                   vs.MethodCode      AS MethodCode,
                   vs.SignRuleKind    AS SignRuleKind,
                   vs.SignRuleKey     AS SignRuleKey
            FROM dbo.tbl_Rule_VargaScheme vs
            JOIN dbo.tbl_Dim_ChartType   ct ON ct.Id = vs.ChartTypeId
            WHERE vs.RuleSetId = @RuleSetId
            ORDER BY ct.DisplayOrder, vs.DivisionFactor
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<VargaScheme>(sql, new { RuleSetId = ruleSetId }).ToList();
    }
}
```

- [ ] **Step 6: Build + `verify-vargas`** → `[PASS] VargaSchemeRepository row count: got 20, expected 20`; `[PASS] every scheme SignRuleKey resolves`; overall `ALL PASS`.

- [ ] **Step 7: Commit**

```bash
git add src/Ikiastrro.Core/Models/VargaScheme.cs src/Ikiastrro.Data/VargaSchemeRepository.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(data): VargaScheme model + VargaSchemeRepository.GetAll\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 13: `VargaCalculator` + orchestrator rewire + delete the 10 old files

**Files:**
- Create: `src/Ikiastrro.Core/Calculators/VargaCalculator.cs`
- Modify: `src/Ikiastrro.Core/Calculators/ChartCalculationOrchestrator.cs`
- Modify: `src/Ikiastrro.Cli/Program.cs` (orchestrator construction)
- Modify: `src/Ikiastrro.Web/Program.cs` (orchestrator DI factory)
- Delete: `src/Ikiastrro.Core/Calculators/D2HoraCalculator.cs`, `D2HoraChartComputer.cs`, `D6ShashtamsaCalculator.cs`, `D6ShashtamsaChartComputer.cs`, `D9NavamsaCalculator.cs`, `D9ChartComputer.cs`, `D10DasamsaCalculator.cs`, `D10DasamsaChartComputer.cs`, `D11RudramsaCalculator.cs`, `D11RudramsaChartComputer.cs`

**Interfaces:**
- Consumes: `VargaChartComputer.Compute`, `VargaSignRuleFactory.For`, `VargaScheme`.
- Produces:
  - `Ikiastrro.Core.Calculators.VargaCalculator : IChartCalculator` — `ctor(string chartType, VargaScheme scheme)`. `ChartType => chartType`. `ComputeAnalysisInput(bd)` → `VargaChartComputer.Compute(bd, scheme.DivisionFactor, VargaSignRuleFactory.For(scheme.SignRuleKey, scheme.DivisionFactor))` with `ChartType` stamped onto the result record. `BuildResult(bd, input)` → a `ChartResult` with `ChartType`, `Ayanamsha = "Lahiri"`, `HouseSystem = "WholeSign"`, `EngineVersion` (copy the string the deleted calculators used), `VargaMethod = scheme.MethodCode`, `ResultJson` (a `{ VargaLagna = { Sign }, Planets }` object — same shape the deleted calculators wrote).
  - `ChartCalculationOrchestrator.CreateDefault(IReadOnlyList<VargaScheme> schemes)` — registers `new D1RasiCalculator()` then `new VargaCalculator(s.ChartType, s)` for each scheme. The old parameterless `CreateDefault()` is removed.

- [ ] **Step 1: Grep every reference to the soon-deleted types**

Run: `grep -rn "D2HoraCalculator\|D2HoraChartComputer\|D6Shashtamsa\|D9NavamsaCalculator\|D9ChartComputer\|D10Dasamsa\|D11Rudramsa" src --include=*.cs`
Record every hit outside the files being deleted (expected: `ChartCalculationOrchestrator.CreateDefault`, possibly a doc-comment in `ChartAnalyzer.cs`, possibly `verify-vargas` — the Task 5–9 checks call `AstroMath.GetXSign` directly, not the computers, so they are fine). Anything else must be repointed in this task.

- [ ] **Step 2: Write `src/Ikiastrro.Core/Calculators/VargaCalculator.cs`**

```csharp
using System.Text.Json;
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// The one IChartCalculator for every divisional chart. Constructed once per
/// tbl_Rule_VargaScheme row by ChartCalculationOrchestrator.CreateDefault.
/// Delegates position maths to VargaChartComputer + the scheme's IVargaSignRule;
/// stamps the scheme's MethodCode onto ChartResult.VargaMethod. Replaces the
/// bespoke D2/D6/D9/D10/D11 calculators (D1 keeps its own D1RasiCalculator).
/// </summary>
public sealed class VargaCalculator : IChartCalculator
{
    private const string EngineVersionString = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)";

    private readonly string _chartType;
    private readonly VargaScheme _scheme;
    private readonly IVargaSignRule _rule;

    public VargaCalculator(string chartType, VargaScheme scheme)
    {
        _chartType = chartType;
        _scheme = scheme;
        _rule = VargaSignRuleFactory.For(scheme.SignRuleKey, scheme.DivisionFactor);
    }

    public string ChartType => _chartType;

    public ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails)
    {
        var input = VargaChartComputer.Compute(birthDetails, _scheme.DivisionFactor, _rule);
        return input with { ChartType = _chartType };   // if ChartAnalysisInput is a positional record; else set the field
    }

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
            ChartType = _chartType,
            Ayanamsha = "Lahiri",
            HouseSystem = "WholeSign",
            EngineVersion = EngineVersionString,
            VargaMethod = _scheme.MethodCode,
            ResultJson = resultJson,
            ComputedAt = DateTime.UtcNow
        };
    }
}
```

> If `ChartAnalysisInput` is a positional `record` its `with { ChartType = ... }` works only if `ChartType` is one of its positional members (it is: `record ChartAnalysisInput(string ChartType, ZodiacName AscendantSign, List<PlanetPosition> Planets)`). Keep it.

- [ ] **Step 3: Rewrite `ChartCalculationOrchestrator.CreateDefault`**

Replace the parameterless `CreateDefault()` with:

```csharp
    /// <summary>D1 + one VargaCalculator per tbl_Rule_VargaScheme row (D2, D2-US,
    /// D3..D60). The scheme rows are loaded by the caller (Cli/Web) and passed in.</summary>
    public static ChartCalculationOrchestrator CreateDefault(IReadOnlyList<VargaScheme> schemes)
    {
        var calculators = new List<IChartCalculator> { new D1RasiCalculator() };
        foreach (var s in schemes)
            calculators.Add(new VargaCalculator(s.ChartType, s));
        return new ChartCalculationOrchestrator(calculators);
    }
```

Update the class doc-comment: "CreateDefault registers D1 + one VargaCalculator per varga scheme row".

- [ ] **Step 4: Update `src/Ikiastrro.Cli/Program.cs`**

Find `var orchestrator = ChartCalculationOrchestrator.CreateDefault();` (near line 83). Replace with:

```csharp
var vargaSchemes = new VargaSchemeRepository(connectionFactory).GetAll(1);
var orchestrator = ChartCalculationOrchestrator.CreateDefault(vargaSchemes);
```

(`connectionFactory` is already constructed above this line — confirm by reading the surrounding code; the schema-normalization `verify-schema` block uses the same variable.)

- [ ] **Step 5: Update `src/Ikiastrro.Web/Program.cs`**

Find `builder.Services.AddScoped(_ => ChartCalculationOrchestrator.CreateDefault());` (near line 30). Replace with:

```csharp
builder.Services.AddScoped(sp =>
{
    var schemes = sp.GetRequiredService<VargaSchemeRepository>().GetAll(1);
    return ChartCalculationOrchestrator.CreateDefault(schemes);
});
```

Ensure `VargaSchemeRepository` is registered in DI — add `builder.Services.AddScoped<VargaSchemeRepository>();` next to the other repository registrations if it is not picked up by an existing assembly scan.

- [ ] **Step 6: Delete the 10 old files**

```bash
git rm src/Ikiastrro.Core/Calculators/D2HoraCalculator.cs src/Ikiastrro.Core/Calculators/D2HoraChartComputer.cs \
       src/Ikiastrro.Core/Calculators/D6ShashtamsaCalculator.cs src/Ikiastrro.Core/Calculators/D6ShashtamsaChartComputer.cs \
       src/Ikiastrro.Core/Calculators/D9NavamsaCalculator.cs src/Ikiastrro.Core/Calculators/D9ChartComputer.cs \
       src/Ikiastrro.Core/Calculators/D10DasamsaCalculator.cs src/Ikiastrro.Core/Calculators/D10DasamsaChartComputer.cs \
       src/Ikiastrro.Core/Calculators/D11RudramsaCalculator.cs src/Ikiastrro.Core/Calculators/D11RudramsaChartComputer.cs
```

- [ ] **Step 7: Build the whole solution**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds. Fix every compile error from Step 1's grep list — repoint doc-comments, remove dead `using`s. `D1RasiCalculator` / `D1ChartComputer` are untouched.

- [ ] **Step 8: Verify the generic path reproduces D2/D6/D9/D10/D11 exactly**

Run: `dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails`
Then:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT cr.ChartType, COUNT(*) AS rows FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId GROUP BY cr.ChartType ORDER BY cr.ChartType"
```
Expected: still exactly `D1, D2, D6, D9, D10, D11` (the 15 new types have no calculator-driven rows yet — `recompute-keydetails` only re-derives existing `tbl_ChartResults`). Row counts unchanged from before Task 13.
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-vargas` and `dotnet run --project src/Ikiastrro.Cli -- verify-schema` → both `ALL PASS`.

- [ ] **Step 9: Spot-check D9 signs are unchanged for the golden record**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT kd.Planet, kd.Sign FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = 'D9' ORDER BY kd.Id"`
Expected: matches the export's Navamsa column (Sun→Gemini, Moon→Virgo, Mars→Taurus, Mercury→Aries, Jupiter→Pisces, Venus→Cancer, Saturn→Aries, Rahu→Libra, Ketu→Aries; Ascendant→Aries). If any differ, the `NavamsaD9SignRule` wrapper or `VargaChartComputer`'s house/lagna handling regressed — diff against the deleted `D9ChartComputer` in git history.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "$(printf 'refactor(core): generic VargaCalculator + scheme-driven orchestrator; delete D2/D6/D9/D10/D11 calculators\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 14: Persist `VargaLongitudeDegrees`; un-gate `DegreesInSign*` in `ChartAnalyzer`

**Files:**
- Modify: `src/Ikiastrro.Core/Models/ChartKeyDetail.cs`
- Modify: `src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs`
- Modify: `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs`

**Interfaces:**
- Produces: `ChartKeyDetail.VargaLongitudeDegrees` (`double`, NOT NULL semantics — always set). `ChartAnalyzer` writes it and un-gated `DegreesInSignDecimal` / `DegreesInSignDisplay` for **every** chart type. `ChartKeyDetailsRepository.InsertAll` writes the new column.

- [ ] **Step 1: Add the model property**

In `src/Ikiastrro.Core/Models/ChartKeyDetail.cs`, add next to `NirayanaLongitudeDegrees`:

```csharp
    /// <summary>The planet's longitude in THIS chart's own 360° space:
    /// (NirayanaLongitudeDegrees × N) mod 360. Equals NirayanaLongitudeDegrees
    /// for D1. Persisted (migration 12) so Vargottama and varga conjunction
    /// tightness are pure SQL.</summary>
    public double VargaLongitudeDegrees { get; set; }
```

- [ ] **Step 2: Update `ChartAnalyzer.Compute`**

Read the current planet loop (around lines 55–100). It has:
```csharp
        var isRasiChart = input.ChartType == "D1";
        ...
        var degreeInSign = (decimal)(nirayanaLongitude % 30);
        ...
            DegreesInSignDisplay = isRasiChart ? planet.DegreesInSign : null,
            DegreesInSignDecimal = isRasiChart ? Math.Round(degreeInSign, 4) : null,
            NirayanaLongitudeDegrees = nirayanaLongitude,
```

Change to (per planet):
```csharp
        var vargaLongitude = isRasiChart
            ? nirayanaLongitude
            : planet.VargaLongitudeDegrees
                ?? throw new InvalidOperationException($"Chart data for {planet.Planet} is missing VargaLongitudeDegrees for chart type '{input.ChartType}'.");
        var degreeInVargaSign = (decimal)(vargaLongitude % 30);
        ...
            DegreesInSignDisplay = isRasiChart
                ? planet.DegreesInSign
                : AstroMath.FormatDegreesMinutesSeconds(vargaLongitude % 30),
            DegreesInSignDecimal = Math.Round(degreeInVargaSign, 4),
            NirayanaLongitudeDegrees = nirayanaLongitude,
            VargaLongitudeDegrees = vargaLongitude,
```

Keep `dignity` still gated to the D1 degree: `ClassicalDignity.Evaluate(..., isRasiChart ? (double?)degreeInSign : null, ...)` — a varga sign is still a discrete bucket for the *dignity* degree-range check. Only the *stored* `DegreesInSign*` fields un-gate. (If `degreeInSign` was the only use of the old variable, keep computing it for that one call; otherwise remove it.)

Do the same for the Ascendant row's `ChartKeyDetail` if `ChartAnalyzer` builds one separately — set its `VargaLongitudeDegrees` from `input.Planets[0].VargaLongitudeDegrees ?? asc real longitude` and its `DegreesInSign*` from `vargaLongitude % 30`.

- [ ] **Step 3: Update `ChartKeyDetailsRepository.InsertAll`**

Add `VargaLongitudeDegrees` to the INSERT column list and `@VargaLongitudeDegrees` to the `VALUES` list, in matching positions — put it right after `NirayanaLongitudeDegrees`.

- [ ] **Step 4: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds.

- [ ] **Step 5: Recompute + verify the existing 6 types still round-trip**

Run: `dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails`
Then:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees IS NULL) AS null_vlong,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId WHERE cr.ChartType='D1' AND ABS(kd.VargaLongitudeDegrees - kd.NirayanaLongitudeDegrees) > 0.0001) AS d1_bad,
  (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id=kd.ChartResultId WHERE cr.ChartType='D9' AND ABS(kd.DegreesInSignDecimal - (kd.VargaLongitudeDegrees % 30)) > 0.0002) AS d9_deg_bad"
```
Expected: all `0`.
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-schema` + `verify-vargas` + `verify-avastha` → `ALL PASS`.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Core/Models/ChartKeyDetail.cs src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs src/Ikiastrro.Data/ChartKeyDetailsRepository.cs
git commit -m "$(printf 'feat(core): persist VargaLongitudeDegrees; un-gate DegreesInSign* for every chart type\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 15: Numeric ayanamsha + sidereal time on `ChartResult`

**Files:**
- Modify: `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs`
- Modify: `src/Ikiastrro.Core/Models/ChartResult.cs`
- Modify: `src/Ikiastrro.Data/ChartResultsRepository.cs`
- Modify: `src/Ikiastrro.Data/ChartGenerationService.cs`

**Interfaces:**
- Produces:
  - `SiderealPositions` gains `double AyanamshaDegrees` and `double LocalSiderealTimeHours` (positional record members appended).
  - `SwissEphemerisProvider.GetSiderealPositions` populates them: `AyanamshaDegrees` via `swe.swe_get_ayanamsa_ut(tjd_ut)` (after the existing `swe_set_sid_mode`), `LocalSiderealTimeHours` = `(swe.swe_sidtime(tjd_ut) + longitude / 15.0)` normalised into `[0, 24)`.
  - `ChartResult` gains `string? VargaMethod`, `double? AyanamshaDegrees`, `double? SiderealTimeHours`; `ResultJson` doc comment updated to "frozen audit snapshot — not the source of truth; every field also has a column".
  - `ChartResultsRepository.Insert` writes the three columns.
  - `ChartGenerationService` computes one `SiderealPositions` per person and stamps `AyanamshaDegrees` / `SiderealTimeHours` on every `ChartResult` before insert (next to where it sets `RuleSetId` / `ChartTypeId` / `CalculationKind`).

- [ ] **Step 1: Extend `SiderealPositions` + the provider**

Read `src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs`. Append two members to the record:

```csharp
public record SiderealPositions(
    double AscendantLongitude,
    IReadOnlyDictionary<PlanetName, double> PlanetLongitudes,
    IReadOnlyDictionary<PlanetName, double> PlanetLatitudes,
    IReadOnlyDictionary<PlanetName, double> PlanetSpeeds,
    double AyanamshaDegrees,
    double LocalSiderealTimeHours);
```

In `GetSiderealPositions`, after the sidereal mode is set and `tjd_ut` computed, before building the return value:

```csharp
        var ayanamsha = sweph.swe_get_ayanamsa_ut(jd);         // jd == tjd_ut used for the planet calls
        var gst = sweph.swe_sidtime(jd);                        // Greenwich apparent sidereal time, hours
        var lst = (gst + longitude / 15.0) % 24.0;
        if (lst < 0) lst += 24.0;
```

and pass `ayanamsha`, `lst` as the two new record args. (Use the exact Julian-day variable the existing `swe_calc_ut` / `swe_houses_ex` calls use — grep the method for `jd` / `tjd`.)

- [ ] **Step 2: Extend `ChartResult`**

In `src/Ikiastrro.Core/Models/ChartResult.cs`, add:

```csharp
    /// <summary>tbl_Rule_VargaScheme.MethodCode for this chart type (e.g.
    /// "ParasaraTraditional"). Null for D1 and non-position calculations.</summary>
    public string? VargaMethod { get; set; }

    /// <summary>Numeric Lahiri ayanamsha at the birth moment, degrees. One value
    /// per person; the same across all of that person's chart rows.</summary>
    public double? AyanamshaDegrees { get; set; }

    /// <summary>Local apparent sidereal time at the birth moment, hours [0,24).</summary>
    public double? SiderealTimeHours { get; set; }
```

Update the `ResultJson` doc comment to: `/// Full computed result as JSON — a FROZEN AUDIT SNAPSHOT, never read back as the source of truth. Every field it holds also has a typed column (tbl_Chart_KeyDetails / tbl_ChartResults).`

- [ ] **Step 3: Update `ChartResultsRepository.Insert`**

Add `VargaMethod, AyanamshaDegrees, SiderealTimeHours` to the INSERT column list and `@VargaMethod, @AyanamshaDegrees, @SiderealTimeHours` to `VALUES`.

- [ ] **Step 4: Stamp ayanamsha/sidereal-time in `ChartGenerationService`**

Read `src/Ikiastrro.Data/ChartGenerationService.cs`. In `GenerateAll` (and `GenerateMissing`, `RecomputeAnalytics` — wherever `ChartResult`s are prepared for insert), after resolving `activeRuleSetId` / `codeToChartTypeId`, compute once:

```csharp
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var ctx = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);
```

and in every place the service sets `result.RuleSetId = activeRuleSetId;` also set:

```csharp
        result.AyanamshaDegrees = ctx.AyanamshaDegrees;
        result.SiderealTimeHours = ctx.LocalSiderealTimeHours;
```

(`VargaMethod` is already on the `ChartResult` from `VargaCalculator.BuildResult` — no service change needed for it. For D1's `ChartResult` from `D1RasiCalculator`, `VargaMethod` stays null.)

- [ ] **Step 5: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds. If `swe_get_ayanamsa_ut` / `swe_sidtime` have a different SwissEphNet name, grep the package: `grep -rniE "ayanamsa|sidtime" ~/.nuget/packages/swissephnet/2.8.0.2/` and use the actual method names.

- [ ] **Step 6: Regenerate one person and check the values**

Run: `dotnet run --project src/Ikiastrro.Cli -- compute-all Ramakrishnan`
Then:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT TOP 3 ChartType, VargaMethod, AyanamshaDegrees, SiderealTimeHours FROM dbo.tbl_ChartResults cr JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId WHERE bd.Name = 'Ramakrishnan' ORDER BY cr.ChartType"
```
Expected: `AyanamshaDegrees` ≈ `23.58` (JHora export: `23-34-49.57` = 23 + 34/60 + 49.57/3600 ≈ 23.5804°); `SiderealTimeHours` ≈ `19.35` (export `19:20:55` ≈ 19.349 h). Both within ~0.05 of those. `VargaMethod` non-null for D2/D6/D9/… and null for D1.

- [ ] **Step 7: Commit**

```bash
git add src/Ikiastrro.Core/Astro/SwissEphemerisProvider.cs src/Ikiastrro.Core/Models/ChartResult.cs src/Ikiastrro.Data/ChartResultsRepository.cs src/Ikiastrro.Data/ChartGenerationService.cs
git commit -m "$(printf 'feat(core): numeric ayanamsha + sidereal time + VargaMethod on ChartResult\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

**PHASE 3 CHECKPOINT:** `dotnet build Ikiastrro.slnx` green. `compute-all` produces D1 + 20 varga chart types. D2/D6/D9/D10/D11 signs unchanged (byte-identical wrappers). `VargaLongitudeDegrees`, un-gated `DegreesInSign*`, `VargaMethod`, `AyanamshaDegrees`, `SiderealTimeHours` all populated. `verify-schema` / `verify-avastha` / `verify-vargas` ALL PASS.

---

# PHASE 4 — GENERATE, VERIFY, FINALIZE

## Task 16: Generate all 21 chart types for every seeded person

**Files:** none (CLI runs).

- [ ] **Step 1: List the seeded people**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT Id, Name FROM dbo.tbl_BirthDetails ORDER BY Id"`
Expected: 5 rows (`Ramakrishnan, Ramya, Ananya, Sundari, Gobli`).

- [ ] **Step 2: Backfill the new chart types for everyone**

Run: `dotnet run --project src/Ikiastrro.Cli -- backfill-charts`
Expected: for each person, "regenerated [D3, D4, D5, D7, D8, D12, D16, D20, D24, D27, D30, D40, D45, D60, D2-US]" (the 15 they were missing) — D1/D2/D6/D9/D10/D11 already present, left alone. Runs in seconds.

- [ ] **Step 3: Re-derive analytics for the new rows**

Run: `dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails`
Expected: "re-derived [D1, D2, D2-US, D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D16, D20, D24, D27, D30, D40, D45, D60]" per person (21 types).

- [ ] **Step 4: Row-count + orphan + FK sanity**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SET NOCOUNT ON;
SELECT (SELECT COUNT(DISTINCT ChartType) FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart') AS position_types;  -- expect 21
SELECT cr.ChartType, COUNT(*) AS keydetail_rows FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId GROUP BY cr.ChartType ORDER BY LEN(cr.ChartType), cr.ChartType;  -- expect 50 per type (5 people × 10 bodies)
SELECT COUNT(*) AS chartresults_missing_chart_type_id FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart' AND ChartTypeId IS NULL;  -- expect 0
SELECT COUNT(*) AS keydetail_orphans FROM dbo.tbl_Chart_KeyDetails kd WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_ChartResults cr WHERE cr.Id = kd.ChartResultId);  -- expect 0
SELECT COUNT(*) AS keydetail_out_of_range FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees < 0 OR VargaLongitudeDegrees >= 360 OR DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30;  -- expect 0"
```
Expected: `position_types = 21`, every `keydetail_rows = 50`, all the "missing / orphans / out_of_range" counts `0`.

- [ ] **Step 5: `verify-schema`** → `ALL PASS`.

- [ ] **Step 6: Commit** (no code — a marker commit so the generation step is a reviewable point)

```bash
git commit --allow-empty -m "$(printf 'chore: generate all 21 position chart types for the 5 seeded people\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 17: `verify-vargas` — JHora export grid match (DB-backed)

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)
- Possibly modify: `db/11_create_rule_vargascheme.sql` + re-apply (if a `SignRuleKey` needs re-pointing)

**Interfaces:**
- Produces: a `verify-vargas` section that, for `1_Ramakrishnan`, reads `tbl_Chart_KeyDetails` for each of D2-US, D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D16, D20, D24, D27, D30, D40, D45, D60 and asserts every planet's `Sign` equals the transcribed Jagannatha Hora export grid.

- [ ] **Step 1: Transcribe the export grids**

Open `D:\@ClaudeSpace\Scratchpad\Rammy_Jagannatha.txt`. Each `+---…---+` block is a South-Indian grid: 12 cells, Aries top-left-of-second-column going clockwise (standard South-Indian fixed layout — Aries is the cell to the right of the top-left corner). The header line inside each block names the chart (`Navamsa D-9`, `D-3 (Trd)`, `D-30`, `D-2 (US)`, …). For each chart, read off which sign each of `As, Su, Mo, Ma, Me, Ju(R), Ve, Sa(R), Ra, Ke` sits in. Build a C# literal:

```csharp
    // Ramakrishnan (22 Apr 1981, Chennai) — Jagannatha Hora export grids.
    // planet order: Ascendant, Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn, Rahu, Ketu
    var jhoraGrid = new Dictionary<string, string[]>
    {
        ["D9"]  = new[] { "Aries","Gemini","Virgo","Taurus","Aries","Pisces","Cancer","Aries","Libra","Aries" },
        ["D3"]  = new[] { /* … from the D-3 (Trd) block … */ },
        ["D4"]  = new[] { /* … */ },
        ["D5"]  = new[] { /* … */ },
        ["D6"]  = new[] { /* … */ },
        ["D7"]  = new[] { /* … */ },
        ["D8"]  = new[] { /* … */ },
        ["D10"] = new[] { /* … */ },
        ["D11"] = new[] { /* … */ },
        ["D12"] = new[] { /* … */ },
        ["D16"] = new[] { /* … */ },
        ["D20"] = new[] { /* … */ },
        ["D24"] = new[] { /* … */ },
        ["D27"] = new[] { /* … */ },
        ["D30"] = new[] { /* … */ },
        ["D40"] = new[] { /* … */ },
        ["D45"] = new[] { /* … */ },
        ["D60"] = new[] { /* … */ },
        ["D2-US"] = new[] { /* … from the D-2 (US) block … */ },
    };
```

Fill every `/* … */` from the file. Use the exact `ZodiacName` spelling (`Capricornus`, not `Capricorn`). The D9 row above is pre-filled from the export's `Navamsa` longitude-table column as a worked example — double-check it against the `D-9` grid block too.

- [ ] **Step 2: Add the DB-backed assertion loop**

```csharp
    // --- JHora export grid match (Ramakrishnan) ---
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var planetOrder = new[] { "Ascendant","Sun","Moon","Mars","Mercury","Jupiter","Venus","Saturn","Rahu","Ketu" };
        foreach (var (chartType, expectedSigns) in jhoraGrid)
        {
            var rows = conn.Query<(string Planet, string Sign)>(
                """
                SELECT kd.Planet, kd.Sign
                FROM dbo.tbl_Chart_KeyDetails kd
                JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
                JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
                WHERE bd.Name = 'Ramakrishnan' AND cr.ChartType = @ct
                """, new { ct = chartType })
                .ToDictionary(r => r.Planet, r => r.Sign);

            for (var i = 0; i < planetOrder.Length; i++)
            {
                if (!rows.TryGetValue(planetOrder[i], out var actual)) { Console.WriteLine($"  [FAIL] {chartType} {planetOrder[i]}: no row"); failures++; continue; }
                Check($"{chartType} {planetOrder[i]}", actual, expectedSigns[i]);
            }
        }
    }
```

- [ ] **Step 3: Build + run**

Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj` then `dotnet run --project src/Ikiastrro.Cli -- verify-vargas`
Expected: most rows PASS on the first go (traditional Parasara = JHora's `(Trd)` default).

- [ ] **Step 4: Re-point any varga that fails the grid**

For a chart type where several planets mismatch:
1. Open `_research/PyJHora/src/jhora/horoscope/chart/charts.py`, find that varga's function, try `chart_method = 2, 3, …` variants against the export by hand (or run PyJHora if a Python env is handy).
2. If a different method reproduces the grid, add its rule as a new `*SignRule` class + a `VargaSignRuleFactory` case, then **update `db/11_create_rule_vargascheme.sql`** — change that row's `SignRuleKey` + `MethodCode` + `MethodSource` — and re-apply:
   `sqlcmd -S localhost -E -d ikiastrro -Q "DELETE FROM dbo.tbl_Rule_VargaScheme WHERE Id = <that row>;"` then re-run the migration (its `IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_VargaScheme)` guards the whole seed — instead, for a single-row fix, run a targeted `UPDATE dbo.tbl_Rule_VargaScheme SET SignRuleKey = '…', MethodCode = '…', MethodSource = '…' WHERE Id = <n>` and also edit the migration file so a fresh build matches).
3. `backfill-charts` + `recompute-keydetails` for that chart type, re-run `verify-vargas`.
4. Record the divergence in this task's checkbox and in the commit message.

If **no** method reproduces the grid (unlikely — only plausible for D2-US), seed the closest and note it in `MethodSource` as `"closest to JHora export; exact method unresolved"`.

- [ ] **Step 5: Full `verify-vargas` green**

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-vargas`
Expected: `verify-vargas: ALL PASS` — the rule unit checks, the factory-resolve check, and every JHora-grid cell.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs db/11_create_rule_vargascheme.sql
git commit -m "$(printf 'test(cli): verify-vargas asserts every varga sign vs the Ramakrishnan JHora export\n\n<note any re-pointed SignRuleKey here>\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 18: `verify-vargas` — degree sanity + Vargottama; full regression

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (`verify-vargas`)

- [ ] **Step 1: Add degree-sanity assertions (DB-backed, all people, all 21 types)**

```csharp
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        long Count(string sql) => conn.ExecuteScalar<long>(sql);
        Check("all VargaLongitudeDegrees in [0,360)",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees < 0 OR VargaLongitudeDegrees >= 360"), 0L);
        Check("all DegreesInSignDecimal in [0,30)",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE DegreesInSignDecimal IS NULL OR DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30"), 0L);
        Check("DegreesInSignDecimal == VargaLongitudeDegrees mod 30",
            Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE ABS(DegreesInSignDecimal - (VargaLongitudeDegrees % 30)) > 0.0002"), 0L);
        // Linear vargas: stored SignId consistent with FLOOR(VargaLongitudeDegrees/30)
        Check("Linear-varga SignId matches FLOOR(VargaLongitudeDegrees/30)",
            Count("""
                SELECT COUNT(*)
                FROM dbo.tbl_Chart_KeyDetails kd
                JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
                JOIN dbo.tbl_Rule_VargaScheme vs ON vs.ChartTypeId = cr.ChartTypeId AND vs.RuleSetId = cr.RuleSetId
                WHERE vs.SignRuleKind = 'Linear'
                  AND kd.SignId IS NOT NULL
                  AND kd.SignId <> (CAST(FLOOR(kd.VargaLongitudeDegrees / 30) AS INT) % 12) + 1
                """), 0L);
    }
```

- [ ] **Step 2: Add a Vargottama report line** (informational — not a hard fail)

```csharp
    using (var conn = connectionFactory.CreateOpenConnection())
    {
        var vargottama = conn.Query<(string Planet, string Sign)>(
            """
            SELECT d1.Planet, d1.Sign
            FROM dbo.tbl_Chart_KeyDetails d1
            JOIN dbo.tbl_ChartResults cr1 ON cr1.Id = d1.ChartResultId
            JOIN dbo.tbl_BirthDetails bd  ON bd.Id  = cr1.BirthDetailId
            JOIN dbo.tbl_ChartResults cr9 ON cr9.BirthDetailId = bd.Id AND cr9.ChartType = 'D9'
            JOIN dbo.tbl_Chart_KeyDetails d9 ON d9.ChartResultId = cr9.Id AND d9.Planet = d1.Planet
            WHERE bd.Name = 'Ramakrishnan' AND cr1.ChartType = 'D1' AND d1.SignId = d9.SignId
            """).ToList();
        Console.WriteLine($"  [INFO] Ramakrishnan D1/D9 Vargottama: {(vargottama.Count == 0 ? "none" : string.Join(", ", vargottama.Select(v => $"{v.Planet}({v.Sign})")))}");
    }
```

Cross-check by eye against the export's `Navamsa` column vs the `Rasi` column (planets whose Rasi == Navamsa sign).

- [ ] **Step 3: Build + `verify-vargas`** → `ALL PASS`.

- [ ] **Step 4: Full regression sweep**

Run in order, expecting the stated result:
```
dotnet build Ikiastrro.slnx                                   # succeeds, 0 warnings
dotnet run --project src/Ikiastrro.Cli -- verify-schema       # ALL PASS
dotnet run --project src/Ikiastrro.Cli -- verify-avastha      # ALL PASS
dotnet run --project src/Ikiastrro.Cli -- verify-vargas       # ALL PASS
dotnet run --project src/Ikiastrro.Cli -- verify-functional-nature  # ALL PASS
dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails
dotnet run --project src/Ikiastrro.Cli -- verify-schema       # ALL PASS (idempotent)
```

- [ ] **Step 5: Web smoke**

Run: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Ikiastrro.Web --launch-profile http` (background), wait ~15 s, then `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5160/charts/1`. Expected: `200`, and the page body (curl without `-o`) contains real chart content (planet/sign tables), not the 4 KB Blazor error page. Stop the server.
The workspace has no UI for the new charts yet — nothing should regress; `/charts/{id}` renders D1+D9 as before.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'test(cli): verify-vargas degree-sanity + Vargottama; full regression green\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 19: Fold migrations 10–13 into the baseline

**Files:**
- Modify: `db/ikiastrro.sql`

- [ ] **Step 1: Add `tbl_Rule_VargaScheme` DDL + seed** to `db/ikiastrro.sql` in the rules-engine region (near `tbl_Rule_Sets` / the other `tbl_Rule_*` tables), guarded `IF OBJECT_ID(...) IS NULL` / `IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_VargaScheme)`. Use the **final** seed from migration 11 (with any Task-17 re-points applied).

- [ ] **Step 2: Add the 15 `tbl_Dim_ChartType` rows** (Ids 7–21) to the existing `tbl_Dim_ChartType` seed block, using the final `DisplayName` values.

- [ ] **Step 3: Add the new columns inline to the `CREATE TABLE`s:**
- `tbl_ChartResults`: `VargaMethod VARCHAR(40) NULL`, `AyanamshaDegrees DECIMAL(9,6) NULL`, `SiderealTimeHours DECIMAL(9,6) NULL`.
- `tbl_Chart_KeyDetails`: `VargaLongitudeDegrees DECIMAL(9,6) NOT NULL` (fresh build has no rows, so `NOT NULL` inline is fine) + `CONSTRAINT CK_KeyDetails_VargaLongitude CHECK (VargaLongitudeDegrees >= 0 AND VargaLongitudeDegrees < 360)`.

- [ ] **Step 4: Update `vw_Chart_Consolidated`** (the last object in the file) — add `kd.VargaLongitudeDegrees` and confirm `kd.DegreesInSignDecimal` / `kd.DegreesInSignDisplay` are already selected (they are — the un-gate is data, not shape). No join change.

- [ ] **Step 5: Rebuild a scratch DB from the baseline and verify**

```
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END"
sqlcmd -S localhost -E -Q "CREATE DATABASE ikiastrro_scratch"
```
Then apply the baseline to `ikiastrro_scratch`. **Note:** `db/ikiastrro.sql` hardcodes `USE [ikiastrro]` near the top — copy it to a scratch file with that line changed to `USE [ikiastrro_scratch]` (and the `CREATE DATABASE [ikiastrro]` guard), apply *that*, from a path with no `/c/...` MSYS prefix (use a `db/_scratch_tmp.sql` inside the repo). Then:
```
sqlcmd -S localhost -E -d ikiastrro_scratch -Q "SELECT (SELECT COUNT(*) FROM dbo.tbl_Dim_ChartType) AS dim, (SELECT COUNT(*) FROM dbo.tbl_Rule_VargaScheme) AS scheme, COL_LENGTH('dbo.tbl_Chart_KeyDetails','VargaLongitudeDegrees') AS vlong_col, COL_LENGTH('dbo.tbl_ChartResults','AyanamshaDegrees') AS ayan_col"
```
Expected: `dim = 21`, `scheme = 20`, both `*_col` non-NULL. Baseline runs with 0 `Msg` errors. Drop `ikiastrro_scratch` and delete `db/_scratch_tmp.sql`.

- [ ] **Step 6: Commit**

```bash
git add db/ikiastrro.sql
git commit -m "$(printf 'chore(db): fold varga migrations 10-13 into the baseline\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

---

## Task 20: Documentation

**Files:**
- Modify: `docs/reference-calculations.md`, `docs/dbdesign-star-schema-rules-engine.md`, `ARCHITECTURE.md`, `master_ikiastrro.md`, `../ikiastrro.md`, memory pointer

- [ ] **Step 1: `docs/reference-calculations.md`** — replace the "Out of scope: D3/D7/D12/D30/D60 and the rest of the Shodashavarga" line with a **Divisional charts** subsection: the 21 chart types, one row per varga with `N`, method (`ParasaraTraditional` etc.), the sign-rule one-liner, and `tbl_Rule_VargaScheme` as the source of truth. Note D81/D108/D144/D150 → Plan B.

- [ ] **Step 2: `docs/dbdesign-star-schema-rules-engine.md`** — add `tbl_Rule_VargaScheme` to the `tbl_Rule_*` inventory: purpose, columns, the `SignRuleKey → C# class` contract via `VargaSignRuleFactory`, and that Python reads it too.

- [ ] **Step 3: `ARCHITECTURE.md`** — update the chart-list sentence ("computes and stores the D1/D2/D6/D9/D10/D11 charts" → "21 position chart types: D1, D2, D2-US, D3–D60"). In "Known limitations", change the "D9 has no degree-in-sign" bullet to note that every varga now stores `VargaLongitudeDegrees` + un-gated `DegreesInSign*` (sign-level display is still the UI choice, but the data is there); add "D81/D108/D144/D150 → Plan B". Add `tbl_Rule_VargaScheme` to the reference-specs table.

- [ ] **Step 4: `master_ikiastrro.md`** — bump the spec row status to `snapshot (Plan A implemented)`; add a Plans row for `docs/superpowers/plans/2026-08-31-divisional-charts-plan-a.md` (status `snapshot (done)` once merged); note Plan B as not-yet-written.

- [ ] **Step 5: `../ikiastrro.md`** — append a dated `2026-08-31` section: 15 new chart types (D2-US + D3–D60), `tbl_Rule_VargaScheme` (data-driven varga rules), one `VargaChartComputer`/`VargaCalculator` replacing 6 bespoke pairs, `VargaLongitudeDegrees` + un-gated `DegreesInSign*` + numeric ayanamsha/sidereal-time, migrations 10–13, verified against the Ramakrishnan JHora export. Note D81/D108/D144/D150 → Plan B.

- [ ] **Step 6: Memory** — update `C:\Users\rammy\.claude\projects\C--Users-rammy\memory\memproj_vedic_horo_gen.md`: the divisional-chart count (now 21), `tbl_Rule_VargaScheme`, `VargaLongitudeDegrees`, and "Plan B = D81/D108/D144 (chart composition) + D150 — spec §1/§6, plan not written". Keep it to ~3 sentences appended to the existing varga note.

- [ ] **Step 7: Commit**

```bash
git add docs/reference-calculations.md docs/dbdesign-star-schema-rules-engine.md ARCHITECTURE.md master_ikiastrro.md
git commit -m "$(printf 'docs: divisional-chart completion (Plan A) — 21 chart types, tbl_Rule_VargaScheme\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01DHUq243ByNgv2ZfXgVbPHW')"
```

(`../ikiastrro.md` and the memory file are outside the repo — save them directly, no commit.)

**PHASE 4 CHECKPOINT / DONE:** `dotnet build Ikiastrro.slnx` green · all `verify-*` PASS · every planet's varga sign matches the Ramakrishnan JHora export for all 21 chart types · `SchemaMigrations` lists `01`–`13` · baseline rebuilds clean with 21 `tbl_Dim_ChartType` + 20 `tbl_Rule_VargaScheme` rows · `VargaLongitudeDegrees` / `DegreesInSign*` / `VargaMethod` / `AyanamshaDegrees` / `SiderealTimeHours` populated for every row · Web renders unchanged. D81/D108/D144/D150 remain for Plan B.

---

## Self-Review

**1. Spec coverage**

| Spec section | Task(s) |
|---|---|
| §1 scope — D2-US + 14 vargas | Tasks 1 (dim), 2 (scheme), 5–10 (rules), 13 (calculator), 16 (generate) |
| §2 DB-completeness invariant | Task 3 (`VargaLongitudeDegrees`), 4 + 14 (un-gate degrees), 15 (`VargaMethod`/ayanamsha/sidereal), 15 (`ResultJson` doc) |
| §3.2 `tbl_Rule_VargaScheme` + seed | Task 2; re-point in Task 17 |
| §3.3 `tbl_Dim_ChartType` 15 rows | Task 1 |
| §3.4 `tbl_ChartResults` provenance | Task 3 (columns), Task 15 (populate) |
| §3.5 `tbl_Chart_KeyDetails` — `VargaLongitudeDegrees` + un-gate | Task 3 (column + backfill), Task 4 (un-gate existing), Task 14 (un-gate forward) |
| §3.6 `ResultJson` non-authoritative | Task 15 Step 2 |
| §3.7 migrations 10–13 + baseline fold | Tasks 1–4, Task 19 |
| §4 code architecture (Approach C) | Tasks 5–13 (rules, factory, computer, calculator, orchestrator, deletes) |
| §5.1 grid match | Task 17 |
| §5.2 PyJHora cross-run | Task 17 Step 4 (referenced when re-pointing) |
| §5.3 degree sanity | Task 18 Step 1 |
| §5.4 Vargottama | Task 18 Step 2 |
| §5.5 regression sweep | Task 18 Step 4 |
| §5.6 Web smoke | Task 18 Step 5 |
| §6 non-goals (D81/D108/D144 → Plan B, etc.) | Not built — stated in Task 20 docs |
| §7 risks | Task 3 (backfill `CASE`), Task 17 (re-point), Task 1 (dim rows before regen) |

No gaps.

**2. Placeholder scan**

- Task 17 Step 1 has `/* … */` in the `jhoraGrid` literal — this is a **transcription instruction with the source file named**, not a code placeholder; the D9 row is filled as a worked example and Step 1 says "fill every `/* … */` from the file". Acceptable (the data cannot be known without reading the export, which is the task).
- Task 5 Step 1 and Task 7 Step 1 flag specific `Check(...)` expected values as "recheck" — deliberate: the step is where the implementer writes down the hand computation, and the rule (not the expectation) is authoritative. The formula each expectation must match is given in the same step.
- No "TODO" / "add error handling" / "similar to Task N" anywhere.

**3. Type consistency**

- `IVargaSignRule.SignFor(double) → ZodiacName` — used identically in every rule (Tasks 5–9), the factory (Task 10), the computer (Task 11), and the calculator (Task 13).
- `VargaScheme(string ChartType, int DivisionFactor, string MethodCode, string SignRuleKind, string SignRuleKey)` — defined in Task 12, consumed in Task 12 (repo), Task 13 (`VargaCalculator` ctor + `CreateDefault`).
- `VargaSignRuleFactory.For(string signRuleKey, int divisionFactor)` — signature identical in Task 10 (definition), Task 12 (verify check), Task 13 (`VargaCalculator` ctor).
- `ChartCalculationOrchestrator.CreateDefault(IReadOnlyList<VargaScheme>)` — one signature, set in Task 13, called in Task 13 (Cli, Web).
- `SiderealPositions` gains `AyanamshaDegrees`, `LocalSiderealTimeHours` (Task 15) — read in Task 15 (`ChartGenerationService`). `ChartResult.AyanamshaDegrees` / `SiderealTimeHours` / `VargaMethod` (Task 15 model) — written by `ChartResultsRepository` (Task 15) and `ChartGenerationService` (Task 15) / `VargaCalculator` (Task 13).
- `ChartKeyDetail.VargaLongitudeDegrees` (`double`) — added Task 14, written by `ChartAnalyzer` (Task 14), inserted by `ChartKeyDetailsRepository` (Task 14), asserted in Tasks 16/18.
- Migration files `10`–`13`; apply order == file order (Global Constraints). `db/11_create_rule_vargascheme.sql` is the one migration edited twice (Task 2 writes it, Task 17 may re-point a row) and folded in Task 19.

**4. Ambiguity**

- "Which sign rule per varga" — §3.2 of the spec gives an explicit formula per varga; this plan turns each into a class with a hand-computed check (Tasks 5–9), and Task 17 confirms every one against the export with a documented re-point path.
- `ChartAnalysisInput` ctor shape — Task 11 Step 3 note + Task 13 Step 2 note both call out the positional-record `with { ChartType = ... }` and say to match the actual signature; consistent.
- `swe_get_ayanamsa_ut` / `swe_sidtime` names — Task 15 Step 5 says grep the SwissEphNet package if the names differ.
