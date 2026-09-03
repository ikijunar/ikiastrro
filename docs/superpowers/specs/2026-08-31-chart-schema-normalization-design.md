# Design Spec — Chart-fact schema normalization (review items 1–5)

**Status:** Approved design, pre-implementation
**Owner:** rammyps
**Created:** 2026-08-31
**Approach:** A — staged/additive, three phases (see "Approaches considered")
**Source:** `D:\@ChatGPT\ikiastrro\codex-db-design-review.md` — prioritised action list, items 1–5
**Related:**
- `ikiastrro/db/ikiastrro.sql` (consolidated baseline this modifies)
- `ikiastrro/db/00_add_avastha_star_schema.sql` (the freshest migration; its `tbl_Fact_PlanetAvastha` shares the anti-patterns fixed here — coordinate)
- `D:\@ChatGPT\ikiastrro\research_varga_karaka_avastha.md` (the Varga/Karaka/Avastha layering this normalization unblocks)
- `ikiastrro/docs/dbdesign-star-schema-rules-engine.md` (the `tbl_Rule_*` / `tbl_Rule_Sets` versioning dimension item 1 completes)
- `ikiastrro/docs/process-codex-review.md` (how the review was produced)
- `ikiastrro/src/Ikiastrro.Data/ChartGenerationService.cs` (the single write path all fact tables flow through)

---

## 1. Goal

Normalize the per-person chart-fact tables so that (a) relationships are expressed by **integer FKs**, not name strings; (b) every computed chart **pins the rule-set version** it was produced under; (c) "chart type" is a **controlled vocabulary** that scales to D2–D60 and no longer conflates position charts with the Vimshottari Dasha run; (d) column **domains are enforced** by CHECK/UNIQUE constraints; and (e) the child fact tables stop **duplicating** parent identity.

Human-readable name columns stay (populated alongside the new `*Id` columns) so views, the Web layer, and standalone reads are undisturbed. Data is migrated **in place** — existing rows are preserved and backfilled, never truncated.

**Out of scope:** the new Karaka / Avastha / Varga-position fact tables from review item 10 (separate plan); the wider `tbl_SignAttributes` dignity-rule extraction (item 8); widening `tbl_Rule_Sets.Id`; any interpretation logic.

---

## 2. Decisions locked (from the brainstorming Q&A)

| # | Decision |
|---|---|
| D1 | **Real data, migrate in place.** Every string→id conversion is a backfill `UPDATE` joining the reference tables; column drops are staged. No truncate-and-recompute. |
| D2 | **One plan, all five items**, executed in dependency order across three phases. |
| D3 | **Keep the name columns.** Add `*Id` columns beside them; both stay populated. Dropping the names is a possible future pass, not this one. |
| D4 | **Add a minimal migration ledger now** — `dbo.SchemaMigrations` + a numbering convention. Pulls part of review item 7 forward because items 1–5 add ~10 scripts. |
| D5 | **Item 3 = full drop, sequenced last.** After every `*Id` column has landed and settled, drop the redundant `BirthDetailId` / `ChartType` / `ComputedAt` from the child fact tables and rewrite the six `DeleteByBirthDetailId` methods to reach the person through `tbl_ChartResults`. |
| D6 | Approach **A** (staged/additive, 3 phases) over big-bang or per-table slices. |

---

## 3. Scope

**In:** everything in §5–§10.

**Out:** review items 6–13; new Karaka/Avastha/Varga fact tables (item 10); `tbl_SignAttributes` dignity extraction (item 8); `tbl_Rule_Sets.Id` `TINYINT`→`SMALLINT`; `UtcOffset` type change; any index-tuning beyond the two the constraints imply (`UNIQUE` on conjunction pair, `UNIQUE` on transit event).

**Touches DB:** `tbl_ChartResults`, `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects`, `tbl_Chart_DashaPeriods`, `tbl_BirthDetails`, `tbl_Rule_Sets`, `tbl_PlanetSignTransitEvents`, `tbl_Fact_PlanetAvastha` (coordinate). New: `dbo.SchemaMigrations`, `tbl_Dim_ChartType`. Views: `vw_Chart_Consolidated`, `vw_Chart_HouseNakshatraSpan`, `vw_Chart_DashaTimeline`. TVFs: `tvf_Chart_SadeSatiPeriods` (rewrite in Phase 3), `tvf_Chart_LifeWeeks` (minor, Phase 2).

**Touches Core:** `Models/ChartResult.cs`, `ChartKeyDetail.cs`, `ChartHouseLord.cs`, `ChartConjunction.cs`, `ChartAspect.cs`, `Dasha/DashaPeriod*.cs`; `Calculators/ChartAnalyzer.cs` (populate ids); a `PlanetId`/`SignId` offset constant.

**Touches Data:** every `Chart*Repository` INSERT column list; the 6 `DeleteByBirthDetailId` methods (Phase 3); `ChartResultsRepository.Insert`; `RuleSetRepository` (record + one query); new `ChartTypeRepository`; `ChartGenerationService` (resolve active rule set + chart-type id).

**Touches CLI:** `Program.cs` — one new mode `verify-schema`; re-run of `recompute-keydetails` / `backfill-*` after each phase.

---

## 4. Migration ledger & numbering (item 4 / item 7-partial)

New table, created by the first migration of Phase 1:

```sql
CREATE TABLE dbo.SchemaMigrations (
    ScriptName    VARCHAR(120)  NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
    AppliedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT sysutcdatetime(),
    ScriptHash    CHAR(64)      NULL,     -- SHA-256 of the script text, for drift detection
    Note          VARCHAR(200)  NULL
);
```

Convention (documented in `db/README.md`, new):

- Migrations are `NN_<verb>_<noun>.sql`, `NN` a zero-padded ordinal **restarting at `01`** now that a ledger exists (the historical `00_*.sql` one-offs stay as-is, unnumbered legacy).
- Each script: `USE [ikiastrro]; GO`, idempotent guards (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS (SELECT 1 ...)`), and a final `INSERT dbo.SchemaMigrations (ScriptName, Note) SELECT '<file>', '<summary>' WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName='<file>');`.
- After a phase's scripts are proven, their DDL is folded into `db/ikiastrro.sql` so a fresh build produces the final shape; `vw_Chart_Consolidated` stays the last object defined in that file.
- No runner is built in this plan — scripts are still applied by hand in ordinal order; the ledger just records what ran.

---

## 5. Phase 1 — additive (nothing breaks)

Every change here is backward-compatible: new columns are NULLable, no constraints tighten, the app is untouched, names remain authoritative.

### 5.1 `tbl_Dim_ChartType` + seed (item 4)

```sql
CREATE TABLE dbo.tbl_Dim_ChartType (
    Id               TINYINT      NOT NULL CONSTRAINT PK_Dim_ChartType PRIMARY KEY,
    Code             VARCHAR(20)  NOT NULL CONSTRAINT UQ_Dim_ChartType_Code UNIQUE,
    DisplayName      VARCHAR(40)  NOT NULL,
    DivisionalFactor TINYINT      NULL,
    Category         VARCHAR(20)  NOT NULL,   -- 'Varga' for now
    DisplayOrder     TINYINT      NOT NULL,
    CONSTRAINT CK_Dim_ChartType_Factor CHECK (DivisionalFactor IS NULL OR DivisionalFactor BETWEEN 1 AND 60)
);
```

Seed = the six calculators that exist today (`D1RasiCalculator` … `D11RudramsaCalculator`). `DisplayName` values below are illustrative — Task 2 aligns them with whatever label the Web layer already shows for each varga:

| Id | Code | DisplayName | DivisionalFactor | Category | DisplayOrder |
|---|---|---|---|---|---|
| 1 | D1 | Rāśi | 1 | Varga | 1 |
| 2 | D2 | Horā | 2 | Varga | 2 |
| 3 | D6 | Ṣaṣṭhāṁśa | 6 | Varga | 3 |
| 4 | D9 | Navāṁśa | 9 | Varga | 4 |
| 5 | D10 | Daśāṁśa | 10 | Varga | 5 |
| 6 | D11 | Rudrāṁśa | 11 | Varga | 6 |

D3/D12–D60 are future `INSERT`s — no schema change, which preserves the intent of `ChartResult.cs`'s "new chart type = new row, never a schema change" comment.

### 5.2 `tbl_Rule_Sets` becomes a real version dimension (item 1)

Add columns (all with safe defaults so the existing `Id=1` row is valid immediately):

```sql
ALTER TABLE dbo.tbl_Rule_Sets ADD
    VersionNumber       INT           NOT NULL CONSTRAINT DF_RuleSets_Version      DEFAULT (1),
    EffectiveFromUtc    DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_EffFrom      DEFAULT ('2000-01-01'),
    EffectiveToUtc      DATETIME2(0)  NULL,
    CreatedAtUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_CreatedAt    DEFAULT sysutcdatetime(),
    SupersedesRuleSetId TINYINT       NULL CONSTRAINT FK_RuleSets_Supersedes FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    SourceReference     VARCHAR(500)  NULL,
    IsPublished         BIT           NOT NULL CONSTRAINT DF_RuleSets_IsPublished  DEFAULT (1);

CREATE UNIQUE INDEX UX_RuleSets_Name_Version ON dbo.tbl_Rule_Sets (RuleSetName, VersionNumber);
CREATE UNIQUE INDEX UX_RuleSets_OneActive    ON dbo.tbl_Rule_Sets (IsActive) WHERE IsActive = 1;
```

`Id` stays `TINYINT` (widening cascades to 5+ existing FK columns on `tbl_Rule_*` and `tbl_Fact_*` — deferred, noted in §11). Immutability is a **convention**, not yet enforced: a new version is a new row + a full child-rule set; historical rows are never updated.

### 5.3 New NULLable `*Id` columns on the fact tables (item 2)

One migration per table. `TINYINT` for planet/sign ids (domain ≤ 27). `NULL` allowed in Phase 1; tightened in Phase 2 where the name column is `NOT NULL`.

| Table | New columns | Notes |
|---|---|---|
| `tbl_ChartResults` | `RuleSetId TINYINT NULL`, `ChartTypeId TINYINT NULL`, `CalculationKind VARCHAR(20) NULL` | `CalculationKind` ∈ `'PositionChart'` \| `'VimshottariDasha'` |
| `tbl_Chart_KeyDetails` | `PlanetId TINYINT NULL`, `SignId TINYINT NULL`, `NakshatraLordPlanetId TINYINT NULL`, `NakshatraSubLordPlanetId TINYINT NULL`, `SignLordPlanetId TINYINT NULL` | `NakshatraId`, `NakshatraPadaId` already exist (migration 032). Lagna row: `PlanetId` stays NULL. |
| `tbl_Chart_HouseLords` | `HouseSignId TINYINT NULL`, `LordPlanetId TINYINT NULL`, `LordPlacedInSignId TINYINT NULL` | |
| `tbl_Chart_Conjunctions` | `Planet1Id TINYINT NULL`, `Planet2Id TINYINT NULL`, `SignId TINYINT NULL` | canonical order enforced in Phase 2 |
| `tbl_Chart_Aspects` | `AspectingPlanetId TINYINT NULL`, `AspectedTargetType VARCHAR(10) NULL`, `AspectedPlanetId TINYINT NULL` | `AspectedTargetType` ∈ `'Planet'` \| `'Ascendant'`; `AspectedPlanetId` NULL when Ascendant |
| `tbl_Chart_DashaPeriods` | `LordId TINYINT NULL` | |
| `tbl_Fact_PlanetAvastha` | `PlanetId TINYINT NULL`, `ChartTypeId TINYINT NULL` | **coordinate with the avastha session** |

### 5.4 Backfill `UPDATE`s

Source of truth = the **name-string join** (unambiguous; the enum-value alignment is a Core-side concern, §7). Examples:

```sql
-- KeyDetails
UPDATE kd SET kd.PlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.Planet;   -- 'Ascendant' -> no match -> stays NULL
UPDATE kd SET kd.SignId = sa.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign;
UPDATE kd SET kd.NakshatraLordPlanetId  = p.Id FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraLordPlanet;
UPDATE kd SET kd.NakshatraSubLordPlanetId = p.Id FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraSubLordPlanet;
UPDATE kd SET kd.SignLordPlanetId       = p.Id FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.SignLordPlanet;

-- ChartResults
UPDATE cr SET cr.RuleSetId = (SELECT Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1) FROM dbo.tbl_ChartResults cr;
UPDATE cr SET cr.CalculationKind = CASE WHEN cr.ChartType = 'VimshottariDasha' THEN 'VimshottariDasha' ELSE 'PositionChart' END FROM dbo.tbl_ChartResults cr;
UPDATE cr SET cr.ChartTypeId = ct.Id FROM dbo.tbl_ChartResults cr JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = cr.ChartType;  -- Dasha rows stay NULL

-- HouseLords / Conjunctions / Aspects / DashaPeriods — analogous name joins.
-- Aspects target type:
UPDATE a SET a.AspectedTargetType = CASE WHEN a.AspectedTarget = 'Ascendant' THEN 'Ascendant' ELSE 'Planet' END FROM dbo.tbl_Chart_Aspects a;
UPDATE a SET a.AspectedPlanetId = p.Id FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_Planets p ON p.PlanetName = a.AspectedTarget;
```

Conjunction canonicalization is deferred to Phase 2 (it reorders rows).

### 5.5 `tbl_PlanetSignTransitEvents` uniqueness (item 5, additive part)

```sql
CREATE UNIQUE INDEX UX_TransitEvent_Planet_At ON dbo.tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc);
```
If this fails, the backfill has duplicates — dedupe first (keep lowest `Id`), record in the ledger note.

### 5.6 `verify-schema` CLI mode

New `dotnet run --project src/Ikiastrro.Cli -- verify-schema`. Read-only; prints a table of checks and exits non-zero on any failure. Assertions:

1. No fact-table row where a `NOT NULL` name column has a NULL `*Id` (excluding `Planet = 'Ascendant'`).
2. Every `tbl_ChartResults` row has `RuleSetId` and `CalculationKind`; `CalculationKind = 'PositionChart'` ⇒ `ChartTypeId IS NOT NULL`.
3. No `*Id` value that fails to resolve to a row in its reference table (orphan check).
4. Domain probes (run as queries in Phase 1, become constraints in Phase 2): longitude `[0,360)`, `DegreesInSignDecimal [0,30)`, houses `[1,12]`, pada `[1,4]`, lat `[-90,90]`, long `[-180,180]`, dasha `StartDate < EndDate` and `StartDayOffset <= EndDayOffset`.
5. Exactly one `tbl_Rule_Sets` row with `IsActive = 1`.
6. Every conjunction row (Phase 2+) has `Planet1Id < Planet2Id`.

### 5.7 Fold Phase 1 DDL into `db/ikiastrro.sql`; run `verify-schema` green.

---

## 6. Phase 2 — enforce

### 6.1 Core + Data write-path (so new rows arrive populated)

- **Models:** add the `*Id` properties to `ChartResult`, `ChartKeyDetail`, `ChartHouseLord`, `ChartConjunction`, `ChartAspect`, `DashaPeriodRecord`/`DashaPeriod`; add `AspectedTargetType` + `int? AspectedPlanetId` to `ChartAspect`; add `RuleSetId`, `ChartTypeId`, `CalculationKind` to `ChartResult`. Type each property `int` where its column ends Phase 2 `NOT NULL`, `int?` where it stays NULLable (`ChartKeyDetail.PlanetId` — the Lagna row; `ChartResult.ChartTypeId` — Dasha rows; `ChartAspect.AspectedPlanetId` — Ascendant target).
- **`ChartAnalyzer.Compute`** already works from `PlanetName` / `ZodiacName` enums — have it set the `*Id` fields at row-build time using the offset constant from §7. Names still set exactly as today.
- **New `ChartTypeRepository`** (reads `tbl_Dim_ChartType`; `IReadOnlyList<ChartTypeRow> GetAll()` + a `Code → Id` map).
- **`ChartGenerationService`**: resolve `RuleSetRepository.GetActive().Id` once per `GenerateAll`/`RecomputeAnalytics` call; map each `calc.ChartType` → `ChartTypeId`; pass both into `ChartResultsRepository.Insert`. Set `CalculationKind` (`'PositionChart'` for orchestrator calculators; `'VimshottariDasha'` in `VimshottariDashaService`).
- **Every `Chart*Repository` INSERT** column list extended to write the `*Id` columns alongside the names.
- **`RuleSetRepository`**: extend the `RuleSet` record + `SelectColumns` with the new fields; `GetActive()` unchanged in behavior.

### 6.2 Tighten constraints (migration `NN_add_chartfact_constraints.sql`)

Once `verify-schema` confirms 100% backfill and clean domains:

- `tbl_ChartResults`: `RuleSetId` → `NOT NULL` + `FK` → `tbl_Rule_Sets`; `CalculationKind` → `NOT NULL DEFAULT 'PositionChart'`; `ChartTypeId` → `FK` → `tbl_Dim_ChartType`; `CHECK ((CalculationKind = 'PositionChart' AND ChartTypeId IS NOT NULL) OR (CalculationKind <> 'PositionChart' AND ChartTypeId IS NULL))`.
- `tbl_Chart_KeyDetails`: `PlanetId` FK (NULLable — Lagna); `SignId`, `SignLordPlanetId`, `NakshatraLordPlanetId`, `NakshatraSubLordPlanetId` FKs; `NakshatraId`/`NakshatraPadaId` FKs (add now if missing). CHECKs: `NirayanaLongitudeDegrees >= 0 AND NirayanaLongitudeDegrees < 360`; `DegreesInSignDecimal IS NULL OR (DegreesInSignDecimal >= 0 AND DegreesInSignDecimal < 30)`; `HouseNumberFromLagna/Sun/Moon BETWEEN 1 AND 12`; `NakshatraPada IS NULL OR NakshatraPada BETWEEN 1 AND 4`.
- `tbl_Chart_HouseLords`: `HouseSignId`, `LordPlanetId`, `LordPlacedInSignId` → `NOT NULL` + FK. `CHECK HouseNumber BETWEEN 1 AND 12`; `LordPlacedInHouseFromLagna/Sun/Moon BETWEEN 1 AND 12`.
- `tbl_Chart_Conjunctions`: canonicalize first —
  ```sql
  UPDATE dbo.tbl_Chart_Conjunctions
     SET Planet1Id = Planet2Id, Planet2Id = Planet1Id, Planet1 = Planet2, Planet2 = Planet1
   WHERE Planet1Id > Planet2Id;
  ```
  then `Planet1Id`, `Planet2Id`, `SignId` → `NOT NULL` + FK; `CHECK (Planet1Id < Planet2Id)`; `CREATE UNIQUE INDEX UX_Conjunctions_Result_Pair ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1Id, Planet2Id)`; `CHECK HouseNumberFromLagna BETWEEN 1 AND 12`.
- `tbl_Chart_Aspects`: `AspectingPlanetId` → `NOT NULL` + FK; `AspectedTargetType` → `NOT NULL` + `CHECK (AspectedTargetType IN ('Planet','Ascendant'))`; `CHECK ((AspectedTargetType = 'Ascendant' AND AspectedPlanetId IS NULL) OR (AspectedTargetType = 'Planet' AND AspectedPlanetId IS NOT NULL))`; FK on `AspectedPlanetId`.
- `tbl_Chart_DashaPeriods`: `LordId` → `NOT NULL` + FK; `CHECK (StartDate < EndDate)`; `CHECK (StartDayOffset <= EndDayOffset)`; `CHECK (LevelNumber BETWEEN 1 AND 3)`; `UNIQUE (ChartResultId, ParentDashaPeriodId, SequenceInParent)`; same-chart parent —
  ```sql
  ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT UQ_DashaPeriods_Result_Id UNIQUE (ChartResultId, Id);
  ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD ParentChartResultId AS ChartResultId PERSISTED;
  ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT FK_DashaPeriods_ParentSameChart
      FOREIGN KEY (ParentChartResultId, ParentDashaPeriodId) REFERENCES dbo.tbl_Chart_DashaPeriods (ChartResultId, Id);
  ```
- `tbl_BirthDetails`: `DROP` `UX_BirthDetails_Name`; `CREATE INDEX IX_BirthDetails_Name ON dbo.tbl_BirthDetails (Name)` (non-unique); `ALTER COLUMN Latitude DECIMAL(9,6) NOT NULL`, `Longitude DECIMAL(9,6) NOT NULL`; `CHECK (Latitude BETWEEN -90 AND 90)`, `CHECK (Longitude BETWEEN -180 AND 180)`.
- `tbl_Fact_PlanetAvastha`: `PlanetId` → FK (coordinate with the avastha session).

### 6.3 Repoint views & TVFs to ids (output columns unchanged)

- `vw_Chart_Consolidated`: internal joins switch to ids — `av.PlanetId = kd.PlanetId` (was `av.Planet = kd.Planet`); conjunction `OUTER APPLY` matches on `Planet1Id`/`Planet2Id`; house-lord `OUTER APPLY` on `LordPlanetId = kd.PlanetId`. Still `SELECT`s the same names.
- `vw_Chart_HouseNakshatraSpan`: `JOIN tbl_SignAttributes sa ON sa.Id = hl.HouseSignId` (was `sa.ZodiacEnumValue = hl.HouseSign`).
- `tvf_Chart_LifeWeeks`: dasha-chart lookup switches `cr.ChartType = 'VimshottariDasha'` → `cr.CalculationKind = 'VimshottariDasha'`. (Its dasha joins already key on `ChartResultId` — untouched.)
- `tvf_Chart_SadeSatiPeriods`: `sa.ZodiacEnumValue = kd.Sign` → `sa.Id = kd.SignId`. Person/chart-type filter stays on `kd.BirthDetailId` / `kd.ChartType = 'D1'` **for now** — rewritten in Phase 3.
- `vw_Chart_DashaTimeline`: no id change needed yet (uses `dp.Lord` string; person via `dp.BirthDetailId` — rewritten in Phase 3).

### 6.4 Fold Phase 2 DDL into baseline; run `recompute-keydetails` + `backfill-*`; `verify-schema` green; every existing `verify-*` mode green.

---

## 7. `PlanetId` / `SignId` derivation (Core)

`ZodiacName` is 0-indexed (`Aries = 0` … `Pisces = 11`); `tbl_SignAttributes.Id` is 1-based → `SignId = (int)zodiac + 1`.
`PlanetName`'s int values and `tbl_Planets.Id` (Saturn = 7, confirmed by `tvf_Chart_SadeSatiPeriods`) — **Task 1 verifies the exact offset** by reading `PlanetName.cs` and the `tbl_Planets` seed. The offset lives as a single `const` (e.g. `AstroMath.PlanetIdOffset`) used by `ChartAnalyzer`. The **backfill migrations do not rely on it** — they join on the name string — so a wrong assumption surfaces only as a `ChartAnalyzer` unit check, not corrupt data.

---

## 8. Phase 3 — drop the redundancy (item 3)

Only after Phases 1–2 are folded into the baseline, `verify-schema` is green, and the avastha session's work is merged.

### 8.1 Drop columns (migration `NN_drop_child_parent_duplication.sql`)

From `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects`, `tbl_Chart_DashaPeriods`, `tbl_Fact_PlanetAvastha`: drop `BirthDetailId`, `ChartType`, and the per-child `ComputedAt` (the parent `tbl_ChartResults.ComputedAt` is authoritative). Drop dependent indexes first; recreate any that were purely `ChartResultId`-leading.

### 8.2 Rewrite the six `DeleteByBirthDetailId` methods

```sql
DELETE FROM dbo.tbl_Chart_KeyDetails
 WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId);
```
Same shape for `ChartHouseLords`, `ChartConjunctions`, `ChartAspects`, `PlanetAvastha`, `DashaPeriods`. `BirthDetailDeletionService`'s FK-safe order is unchanged (children before `tbl_ChartResults` before `tbl_BirthDetails`).

### 8.3 Rewrite `tvf_Chart_SadeSatiPeriods`

`MoonSign` CTE joins `tbl_ChartResults cr ON cr.Id = kd.ChartResultId` and filters `cr.BirthDetailId = @BirthDetailId AND cr.ChartTypeId = 1` (D1). Sign lookup uses `kd.SignId`.

### 8.4 Repoint the remaining views off child `BirthDetailId`

- `vw_Chart_Consolidated`: `JOIN tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId` (was `kd.BirthDetailId`).
- `vw_Chart_HouseNakshatraSpan`: source `BirthDetailId` / chart type via a `tbl_ChartResults` join; drop `hl.BirthDetailId` / `hl.ChartType` from the `SELECT` (replace with `cr.BirthDetailId`, `ct.Code`).
- `vw_Chart_DashaTimeline`: `JOIN tbl_ChartResults cr ON cr.Id = dp.ChartResultId JOIN tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId` (was `dp.BirthDetailId`).

### 8.5 Core model cleanup

Remove `BirthDetailId`, `ChartType`, `ComputedAt` from `ChartKeyDetail`, `ChartHouseLord`, `ChartConjunction`, `ChartAspect`, `DashaPeriodRecord`, and the avastha row model. `ChartGenerationService.PersistAnalytics` stops the `r.BirthDetailId = …` / `r.ChartType = …` fan-out loops; keeps setting `r.ChartResultId`. Web/CLI reads that used the child `ChartType` now read it from the `ChartResult` / `vw_Chart_Consolidated`.

### 8.6 Fold into baseline; full regression (§9).

---

## 9. Verification model (no test project)

Run after **each** phase, all must pass:

| Check | Command |
|---|---|
| Schema invariants | `dotnet run --project src/Ikiastrro.Cli -- verify-schema` |
| Vargas still compute | `-- verify-vargas` |
| Functional nature | `-- verify-functional-nature` |
| Avastha | `-- verify-avastha` |
| Recompute writes clean rows | `-- recompute-keydetails` then `-- verify-schema` |
| Rule-set inspection | `-- list-rule-sets`, `-- show-rules 1` |
| Build | `dotnet build Ikiastrro.slnx` |
| Web smoke | load `/charts/{id}` for a seeded person; consolidated view + dasha timeline render |

`verify-schema` is the new gate; §5.6 lists its assertions. A phase is "done" only when its DDL is in `db/ikiastrro.sql` **and** a from-scratch build of that file passes `verify-schema`.

---

## 10. Build sequencing (hand-off to writing-plans)

**Phase 1**
1. Verify `PlanetName`↔`tbl_Planets.Id` offset; write `db/README.md` convention + `01_create_schema_migrations.sql`.
2. `02_create_dim_charttype.sql` (+ seed).
3. `03_extend_rule_sets_version.sql`.
4. `04_add_chartfact_id_columns.sql` (+ `tbl_Fact_PlanetAvastha`, + transit `UNIQUE`).
5. `05_backfill_chartfact_ids.sql`.
6. `verify-schema` CLI mode.
7. Fold Phase 1 into `db/ikiastrro.sql`; green from-scratch.

**Phase 2**
8. Core model `*Id` fields + `ChartAnalyzer` id population + offset const.
9. `ChartTypeRepository`; `RuleSet` record + query; `ChartGenerationService` + `ChartResultsRepository.Insert` plumbing; every `Chart*Repository` INSERT list.
10. `06_add_chartfact_constraints.sql` (NOT NULL + FK + CHECK, conjunction canonicalize, dasha parent FK, BirthDetails index/type/CHECK).
11. `07_repoint_views_tvfs_to_ids.sql`.
12. Fold Phase 2 into baseline; `recompute-keydetails` + `backfill-*`; full §9.

**Phase 3** (after avastha merge)
13. `08_drop_child_parent_duplication.sql`.
14. Rewrite 6 `DeleteByBirthDetailId`; `tvf_Chart_SadeSatiPeriods`; remaining view repoints (`09_finalize_views_after_drop.sql`).
15. Core model cleanup + `ChartGenerationService.PersistAnalytics` simplification.
16. Fold Phase 3 into baseline; full §9 regression.

---

## 11. Open questions / risks

- **Concurrent editor — RESOLVED (2026-08-31).** The `avastha-baaladi-jagradadi-star` session has ended. Its slice-1 work is committed (`eb3b180`), the working tree is clean, `db/00_add_avastha_star_schema.sql` and the baseline's avastha section are verified consistent (formatting-only diff), and `db/ikiastrro.sql`'s dynamic-SQL blocks are well-formed (Codex's "malformed baseline" note was a bundling artifact). `tbl_Fact_PlanetAvastha` is now owned by this plan — its `*Id` adds (§5.3) and Phase-3 column drops proceed without external coordination. Still re-check `git status` at the start of each phase in case a later avastha slice opens.
- **Deferred avastha/karaka work that may land later:** slices 2–6 in `docs/research-topic-coverage.md` (Deeptadi/Lajjitadi, Chara Karaka, Sayanadi, synthesis view) and **migration 030** (`tbl_Dim_PlanetHouseKaraka` / `tbl_Dim_PlanetSignification`, "designed but not applied"). Migration 030 is Naisargika-Karaka *reference* data — it does not touch the chart-fact tables normalized here, so interaction is limited to both sides adding `tbl_Dim_*` rows.
- **Stale review base.** Codex reviewed the 274 KB baseline; the current one is 281 KB (avastha folded in). Findings still hold; this spec targets the current file.
- **`tbl_Rule_Sets.Id` stays `TINYINT`** — 255-version ceiling. Accepted for now; widening is its own migration touching 5+ FK columns.
- **`float` → `decimal(9,6)`** on `tbl_BirthDetails` lat/long: `decimal(9,6)` covers ±999.999999 with 6 dp (~0.1 m) — enough for ±180°. Existing `float` values are re-read and rounded on `ALTER COLUMN`; verify no seeded person shifts sign/nakshatra (`verify-vargas`).
- **Lagna rows carry `PlanetId = NULL`** across `KeyDetails` and as an aspect target — every consumer already `LEFT JOIN`s or filters `'Ascendant'`; `verify-schema` excludes them from the not-null assertion.
- **No cross-repo transaction** (pre-existing). Backfills are single `UPDATE`s (atomic). The app write path's transaction story is unchanged by this plan.
- **`vw_Chart_HouseNakshatraSpan`** currently exposes `hl.BirthDetailId` / `hl.ChartType` directly; Phase 3 changes its output column source (same values, via `cr`). Any Web code binding those columns by name is unaffected; by position would break — grep before Phase 3.
- **Per-child `ComputedAt` semantics.** `recompute-keydetails` / `RecomputeAnalytics` re-derive the child rows *without* re-inserting `tbl_ChartResults`, so a child `ComputedAt` currently records "when analytics were last recomputed", which the parent's does not. Phase 3 (§8.1) drops it. Decide during planning: is that timestamp used anywhere (grep `ComputedAt` in `Ikiastrro.Web`)? If yes, keep one `AnalyticsComputedAt` column on `tbl_ChartResults` updated by the recompute path instead of dropping the signal outright.

---

## 12. Approaches considered

- **A — staged/additive, 3 phases (chosen).** Phase 1 is pure addition (revertible, app untouched); Phase 2 enforces once backfill is proven; Phase 3 isolates the risky column drops. Clean `verify-schema` gate between phases. Cost: ~9 migration scripts, three baseline folds.
- **B — big-bang single migration + single app cutover.** One script, one code change. Rejected: not revertible against real data, a single enormous review surface, and no "nothing breaks yet" checkpoint — a mistake anywhere means restoring from backup.
- **C — per-table vertical slices** (KeyDetails end-to-end, then HouseLords, …). Coherent per table, but rewrites each of the three shared views/TVFs once per slice, interleaves enforcement risk with additive work, and never reaches a state where the whole schema is half-migrated safely. Rejected.
