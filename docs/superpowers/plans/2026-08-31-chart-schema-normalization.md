# Chart-fact Schema Normalization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the per-person chart-fact tables onto integer foreign keys, pin a rule-set version and a controlled chart-type onto every chart, enforce column domains with constraints, and remove parent-identity duplication from the child tables — migrating existing data in place.

**Architecture:** Three sequential phases against a live SQL Server database. Phase 1 is purely additive (new NULLable columns + new tables + backfill) so nothing breaks and the app is untouched. Phase 2 teaches the .NET write path to populate the new columns, then tightens them to `NOT NULL` + FK + `CHECK`. Phase 3 drops the now-redundant `BirthDetailId` / `ChartType` / `ComputedAt` from the child tables and repoints the six delete paths and the remaining views through `tbl_ChartResults`. Each phase ends at a green `verify-schema` checkpoint and is folded into the `db/ikiastrro.sql` baseline.

**Tech Stack:** SQL Server (T-SQL, SMO-scripted baseline), .NET 8 / C#, Dapper, a console CLI with `verify-*` self-check modes (no xUnit/test project — verification is `dotnet build` + CLI modes + a Web smoke check).

**Spec:** `docs/superpowers/specs/2026-08-31-chart-schema-normalization-design.md` — read it first; this plan argues from it.

## Global Constraints

- **Database:** `ikiastrro` on `Server=localhost` via Windows Auth (`Integrated Security=True;TrustServerCertificate=True`). Apply every migration with `sqlcmd -S localhost -E -d ikiastrro -i db/<file>.sql` (or paste into SSMS against `ikiastrro` if `sqlcmd` is absent).
- **Migrate in place.** Never `TRUNCATE` or drop a chart-fact table. Every string→id conversion is a backfill `UPDATE` joining a reference table; column removals happen only in Phase 3 after the ids have settled.
- **Keep the name columns.** New `*Id` columns sit *beside* the existing `Planet` / `Sign` / `Lord` strings; both stay populated. Dropping the names is out of scope.
- **Idempotent migrations.** Guard every DDL statement (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ...)`, `IF NOT EXISTS (SELECT 1 FROM <seed_table>)`). Each script ends by recording itself in `dbo.SchemaMigrations`.
- **Migration numbering restarts at `01`.** The historical `db/00_*.sql` one-offs stay as unnumbered legacy; new scripts are `NN_<verb>_<noun>.sql` in ordinal order.
- **Id arithmetic (verified against the seeds):** `PlanetId = (int)PlanetName + 1` (`PlanetName.Sun` = 0 → `tbl_Planets.Id` = 1 … `Ketu` = 8 → 9). `SignId = (int)ZodiacName + 1` (`ZodiacName.Aries` = 0 → `tbl_SignAttributes.Id` = 1 … `Pisces` = 11 → 12). `tbl_Chart_KeyDetails.Sign` stores `ZodiacName.ToString()`, which equals `tbl_SignAttributes.ZodiacEnumValue` (e.g. `'Capricornus'`, the Latin form) — **not** `SignName`.
- **Chart-type vocabulary:** the six registered calculators emit `ChartType` ∈ `{D1, D2, D6, D9, D10, D11}`; `VimshottariDasha` is the pseudo-type that becomes `CalculationKind = 'VimshottariDasha'` with `ChartTypeId = NULL`.
- **`tbl_Rule_Sets.Id` stays `TINYINT`.** Do not widen it.
- **Commit after every task.** Conventional-commit style, matching the repo's history (`feat(db):`, `refactor(data):`, `chore(db):`). Every commit message ends with the two trailers used elsewhere in this repo's Claude-authored commits.
- **Branch:** work continues on `feat/ikiastrro-workspace-ui`. Do not push unless asked.
- **Baseline fold rule:** after a phase's scripts pass on a real DB, copy their final DDL into `db/ikiastrro.sql` so a from-scratch build produces the same shape. `vw_Chart_Consolidated` must remain the **last** object defined in that file.

---

## File Structure

### Phase 1 — additive (DB + one CLI mode)

| File | Create/Modify | Responsibility |
|---|---|---|
| `db/README.md` | Create | Migration-script convention + the `SchemaMigrations` ledger contract |
| `db/01_create_schema_migrations.sql` | Create | The `dbo.SchemaMigrations` ledger table |
| `db/02_create_dim_charttype.sql` | Create | `tbl_Dim_ChartType` + 6-row seed |
| `db/03_extend_rule_sets_version.sql` | Create | Version columns + "one active" / "name+version" unique indexes on `tbl_Rule_Sets` |
| `db/04_add_chartfact_id_columns.sql` | Create | All new NULLable `*Id` / `CalculationKind` / `AspectedTargetType` columns; transit `UNIQUE` |
| `db/05_backfill_chartfact_ids.sql` | Create | `UPDATE`s that populate every new id column from the reference tables |
| `src/Ikiastrro.Cli/Program.cs` | Modify | New `verify-schema` mode (Phase 1 assertion set) |
| `db/ikiastrro.sql` | Modify | Fold Phase 1 DDL into the baseline |

### Phase 2 — enforce (Core + Data + DB)

| File | Create/Modify | Responsibility |
|---|---|---|
| `src/Ikiastrro.Core/Astro/AstroIds.cs` | Create | `AstroIds.PlanetId(PlanetName)` / `AstroIds.SignId(ZodiacName)` pure helpers + the offset constants |
| `src/Ikiastrro.Core/Models/ChartResult.cs` | Modify | `+ RuleSetId`, `+ ChartTypeId` (`int?`), `+ CalculationKind` |
| `src/Ikiastrro.Core/Models/ChartKeyDetail.cs` | Modify | `+ PlanetId` (`int?`), `+ SignId`, `+ NakshatraLordPlanetId`, `+ NakshatraSubLordPlanetId`, `+ SignLordPlanetId` |
| `src/Ikiastrro.Core/Models/ChartHouseLord.cs` | Modify | `+ HouseSignId`, `+ LordPlanetId`, `+ LordPlacedInSignId` |
| `src/Ikiastrro.Core/Models/ChartConjunction.cs` | Modify | `+ Planet1Id`, `+ Planet2Id`, `+ SignId` |
| `src/Ikiastrro.Core/Models/ChartAspect.cs` | Modify | `+ AspectingPlanetId`, `+ AspectedTargetType`, `+ AspectedPlanetId` (`int?`) |
| `src/Ikiastrro.Core/Dasha/DashaPeriodRecord.cs` | Modify | `+ LordId` |
| `src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs` | Modify | Populate every new `*Id` at row-build time via `AstroIds` |
| `src/Ikiastrro.Core/Models/ChartTypeRow.cs` | Create | Read model for `tbl_Dim_ChartType` |
| `src/Ikiastrro.Data/ChartTypeRepository.cs` | Create | `GetAll()` + a `Code → Id` dictionary |
| `src/Ikiastrro.Data/RuleSetRepository.cs` | Modify | `RuleSet` record + `SelectColumns` gain the version fields |
| `src/Ikiastrro.Data/ChartResultsRepository.cs` | Modify | `Insert` writes `RuleSetId` / `ChartTypeId` / `CalculationKind` |
| `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs` | Modify | INSERT column list + `*Id` params |
| `src/Ikiastrro.Data/ChartHouseLordsRepository.cs` | Modify | INSERT column list + `*Id` params |
| `src/Ikiastrro.Data/ChartConjunctionsRepository.cs` | Modify | INSERT column list + `*Id` params |
| `src/Ikiastrro.Data/ChartAspectsRepository.cs` | Modify | INSERT column list + typed-target params |
| `src/Ikiastrro.Data/DashaPeriodsRepository.cs` | Modify | INSERT column list + `LordId` |
| `src/Ikiastrro.Data/PlanetAvasthaRepository.cs` | Modify | INSERT column list + `PlanetId` |
| `src/Ikiastrro.Data/ChartGenerationService.cs` | Modify | Resolve active `RuleSetId` + map `ChartType → ChartTypeId` + set `CalculationKind` |
| `src/Ikiastrro.Data/VimshottariDashaService.cs` | Modify | Set `CalculationKind = 'VimshottariDasha'` on its `ChartResult` |
| `src/Ikiastrro.Core/Models/AvasthaModels.cs` | Modify | `PlanetAvasthaFact + PlanetId` |
| `db/06_add_chartfact_constraints.sql` | Create | `NOT NULL` + FK + `CHECK` + canonical conjunction + dasha same-chart parent + `tbl_BirthDetails` fixes |
| `db/07_repoint_views_tvfs_to_ids.sql` | Create | Rebuild `vw_Chart_Consolidated`, `vw_Chart_HouseNakshatraSpan`, `tvf_Chart_LifeWeeks`, `tvf_Chart_SadeSatiPeriods` on ids |
| `src/Ikiastrro.Cli/Program.cs` | Modify | Extend `verify-schema` with the Phase 2 assertion set |
| `db/ikiastrro.sql` | Modify | Fold Phase 2 DDL |

### Phase 3 — drop redundancy (DB + Data + Core)

| File | Create/Modify | Responsibility |
|---|---|---|
| `db/08_drop_child_parent_duplication.sql` | Create | Drop `BirthDetailId` / `ChartType` / `ComputedAt` from the 6 child tables |
| `db/09_finalize_views_after_drop.sql` | Create | Repoint `vw_Chart_Consolidated`, `vw_Chart_HouseNakshatraSpan`, `vw_Chart_DashaTimeline`, `tvf_Chart_SadeSatiPeriods` off child `BirthDetailId` |
| `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs` … `PlanetAvasthaRepository.cs` (6 files) | Modify | `DeleteByBirthDetailId` → subquery through `tbl_ChartResults`; drop `BirthDetailId` / `ChartType` from INSERT lists |
| Core models (6 files) | Modify | Remove `BirthDetailId` / `ChartType` / `ComputedAt` properties |
| `src/Ikiastrro.Data/ChartGenerationService.cs` | Modify | Drop the `r.BirthDetailId = …` / `r.ChartType = …` fan-out in `PersistAnalytics` |
| `db/ikiastrro.sql` | Modify | Fold Phase 3 DDL |

---

# PHASE 1 — ADDITIVE

## Task 1: Migration ledger + convention doc

**Files:**
- Create: `db/01_create_schema_migrations.sql`
- Create: `db/README.md`

**Interfaces:**
- Produces: `dbo.SchemaMigrations (ScriptName VARCHAR(120) PK, AppliedAtUtc DATETIME2(0), ScriptHash CHAR(64) NULL, Note VARCHAR(200) NULL)` — every later migration script appends one row.

- [ ] **Step 1: Write `db/01_create_schema_migrations.sql`**

```sql
-- =====================================================================
-- 01 — Schema-migration ledger. First numbered migration; the historical
-- db/00_*.sql one-offs predate it and are not recorded here.
-- Idempotent. Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/01_create_schema_migrations.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.SchemaMigrations', 'U') IS NULL
CREATE TABLE dbo.SchemaMigrations (
    ScriptName    VARCHAR(120)  NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
    AppliedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT sysutcdatetime(),
    ScriptHash    CHAR(64)      NULL,
    Note          VARCHAR(200)  NULL
);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '01_create_schema_migrations.sql', 'ledger bootstrap'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '01_create_schema_migrations.sql');
GO
PRINT '01 applied: dbo.SchemaMigrations ready.';
GO
```

- [ ] **Step 2: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/01_create_schema_migrations.sql`
Expected: `01 applied: dbo.SchemaMigrations ready.`

- [ ] **Step 3: Verify the row landed**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT ScriptName, Note FROM dbo.SchemaMigrations"`
Expected: one row, `01_create_schema_migrations.sql | ledger bootstrap`.

- [ ] **Step 4: Re-apply to prove idempotency**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/01_create_schema_migrations.sql`
Expected: same `PRINT`, still exactly one row (no PK violation).

- [ ] **Step 5: Write `db/README.md`**

```markdown
# Database scripts

`db/ikiastrro.sql` is the **from-scratch baseline** (SMO script-out of the full schema
+ reference/seed data). A fresh machine runs only this file.

## Migrations

Incremental changes are numbered scripts `NN_<verb>_<noun>.sql`, applied in ascending
`NN` order against an existing `ikiastrro` database:

```
sqlcmd -S localhost -E -d ikiastrro -i db/NN_<name>.sql
```

Rules:

- **Idempotent.** Guard every statement (`IF COL_LENGTH`, `IF OBJECT_ID`, `IF NOT EXISTS
  (SELECT 1 FROM sys.indexes WHERE name = …)`, `IF NOT EXISTS (SELECT 1 FROM <seed>)`).
- **Self-recording.** End each script with an insert into `dbo.SchemaMigrations`
  (`WHERE NOT EXISTS`), so `SELECT * FROM dbo.SchemaMigrations` is the applied-history.
- **Never edit an applied migration.** Add a corrective `NN+1` script instead.
- **Fold forward.** Once a script is proven on a real DB, copy its final DDL into
  `db/ikiastrro.sql` so the baseline and the migrated DB converge. `vw_Chart_Consolidated`
  stays the last object in the baseline (it reads tables defined above it).

The historical `db/00_*.sql` one-offs predate this ledger and are not recorded in it.
```

- [ ] **Step 6: Commit**

```bash
git add db/01_create_schema_migrations.sql db/README.md
git commit -m "$(printf 'chore(db): add SchemaMigrations ledger + migration convention\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 2: `tbl_Dim_ChartType` + seed

**Files:**
- Create: `db/02_create_dim_charttype.sql`

**Interfaces:**
- Produces: `dbo.tbl_Dim_ChartType (Id TINYINT PK, Code VARCHAR(20) UNIQUE, DisplayName VARCHAR(40), DivisionalFactor TINYINT NULL, Category VARCHAR(20), DisplayOrder TINYINT)`, seeded rows `Id 1..6` = codes `D1,D2,D6,D9,D10,D11`.

- [ ] **Step 1: Check the Web's varga labels so `DisplayName` matches**

Run: `grep -rniE "navamsa|dasamsa|rudramsa|shashtamsa|hora|rasi" src/Ikiastrro.Web/ | grep -vi ".css"`
Use whatever spelling the UI already shows; if nothing, use the values below.

- [ ] **Step 2: Write `db/02_create_dim_charttype.sql`**

```sql
-- =====================================================================
-- 02 — tbl_Dim_ChartType: controlled vocabulary for ChartResults.ChartTypeId.
-- Seeds the six registered position charts. D3/D7/D12..D60 are future INSERTs
-- (no schema change). Vimshottari Dasha is NOT a chart type — see CalculationKind (migration 04).
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Dim_ChartType', 'U') IS NULL
CREATE TABLE dbo.tbl_Dim_ChartType (
    Id               TINYINT      NOT NULL CONSTRAINT PK_Dim_ChartType PRIMARY KEY,
    Code             VARCHAR(20)  NOT NULL CONSTRAINT UQ_Dim_ChartType_Code UNIQUE,
    DisplayName      VARCHAR(40)  NOT NULL,
    DivisionalFactor TINYINT      NULL,
    Category         VARCHAR(20)  NOT NULL,
    DisplayOrder     TINYINT      NOT NULL,
    CONSTRAINT CK_Dim_ChartType_Factor CHECK (DivisionalFactor IS NULL OR DivisionalFactor BETWEEN 1 AND 60)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType)
INSERT dbo.tbl_Dim_ChartType (Id, Code, DisplayName, DivisionalFactor, Category, DisplayOrder) VALUES
    (1, 'D1',  'Rasi',       1,  'Varga', 1),
    (2, 'D2',  'Hora',       2,  'Varga', 2),
    (3, 'D6',  'Shashtamsa', 6,  'Varga', 3),
    (4, 'D9',  'Navamsa',    9,  'Varga', 4),
    (5, 'D10', 'Dasamsa',    10, 'Varga', 5),
    (6, 'D11', 'Rudramsa',   11, 'Varga', 6);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '02_create_dim_charttype.sql', 'chart-type vocabulary + 6-row seed'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '02_create_dim_charttype.sql');
GO
PRINT '02 applied: tbl_Dim_ChartType ready.';
GO
```

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/02_create_dim_charttype.sql`
Expected: `02 applied: tbl_Dim_ChartType ready.`

- [ ] **Step 4: Verify seed vs. live chart types**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT DISTINCT cr.ChartType FROM dbo.tbl_ChartResults cr WHERE cr.ChartType NOT IN (SELECT Code FROM dbo.tbl_Dim_ChartType) AND cr.ChartType <> 'VimshottariDasha'"`
Expected: **0 rows** (every stored position-chart type has a `Dim` code). If any row appears, add it to the seed and re-apply.

- [ ] **Step 5: Commit**

```bash
git add db/02_create_dim_charttype.sql
git commit -m "$(printf 'feat(db): add tbl_Dim_ChartType vocabulary (review item 4)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 3: `tbl_Rule_Sets` version dimension

**Files:**
- Create: `db/03_extend_rule_sets_version.sql`

**Interfaces:**
- Produces: `tbl_Rule_Sets` gains `VersionNumber INT`, `EffectiveFromUtc DATETIME2(0)`, `EffectiveToUtc DATETIME2(0) NULL`, `CreatedAtUtc DATETIME2(0)`, `SupersedesRuleSetId TINYINT NULL` (self-FK), `SourceReference VARCHAR(500) NULL`, `IsPublished BIT`. Indexes `UX_RuleSets_Name_Version`, `UX_RuleSets_OneActive` (filtered).

- [ ] **Step 1: Confirm exactly one active rule set today**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT Id, RuleSetName, IsActive FROM dbo.tbl_Rule_Sets"`
Expected: one row with `IsActive = 1` (id `1`, `Parashari-Classical`). If more than one is active, fix the data before the filtered unique index can be created.

- [ ] **Step 2: Write `db/03_extend_rule_sets_version.sql`**

```sql
-- =====================================================================
-- 03 — tbl_Rule_Sets becomes a real version dimension (review item 1).
-- Id stays TINYINT (widening would cascade to 5+ FK columns). Immutability
-- of a published set is a convention, not yet enforced.
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Rule_Sets', 'VersionNumber') IS NULL
ALTER TABLE dbo.tbl_Rule_Sets ADD
    VersionNumber       INT           NOT NULL CONSTRAINT DF_RuleSets_Version     DEFAULT (1),
    EffectiveFromUtc    DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_EffFrom     DEFAULT ('2000-01-01T00:00:00'),
    EffectiveToUtc      DATETIME2(0)  NULL,
    CreatedAtUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleSets_CreatedAt   DEFAULT sysutcdatetime(),
    SupersedesRuleSetId TINYINT       NULL CONSTRAINT FK_RuleSets_Supersedes FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    SourceReference     VARCHAR(500)  NULL,
    IsPublished         BIT           NOT NULL CONSTRAINT DF_RuleSets_IsPublished DEFAULT (1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_Name_Version')
CREATE UNIQUE INDEX UX_RuleSets_Name_Version ON dbo.tbl_Rule_Sets (RuleSetName, VersionNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_OneActive')
CREATE UNIQUE INDEX UX_RuleSets_OneActive ON dbo.tbl_Rule_Sets (IsActive) WHERE IsActive = 1;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '03_extend_rule_sets_version.sql', 'rule-set version columns + one-active index'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '03_extend_rule_sets_version.sql');
GO
PRINT '03 applied: tbl_Rule_Sets versioned.';
GO
```

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/03_extend_rule_sets_version.sql`
Expected: `03 applied: tbl_Rule_Sets versioned.`

- [ ] **Step 4: Verify existing `list-rule-sets` / `show-rules` still work**

Run: `dotnet run --project src/Ikiastrro.Cli -- list-rule-sets`
Then: `dotnet run --project src/Ikiastrro.Cli -- show-rules 1`
Expected: both print without error (the new columns are additive; `RuleSetRepository` selects an explicit column list that does not yet include them).

- [ ] **Step 5: Commit**

```bash
git add db/03_extend_rule_sets_version.sql
git commit -m "$(printf 'feat(db): tbl_Rule_Sets version dimension (review item 1)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 4: Add the NULLable `*Id` columns

**Files:**
- Create: `db/04_add_chartfact_id_columns.sql`

**Interfaces:**
- Produces, all NULLable:
  - `tbl_ChartResults`: `RuleSetId TINYINT`, `ChartTypeId TINYINT`, `CalculationKind VARCHAR(20)`
  - `tbl_Chart_KeyDetails`: `PlanetId TINYINT`, `SignId TINYINT`, `NakshatraLordPlanetId TINYINT`, `NakshatraSubLordPlanetId TINYINT`, `SignLordPlanetId TINYINT`
  - `tbl_Chart_HouseLords`: `HouseSignId TINYINT`, `LordPlanetId TINYINT`, `LordPlacedInSignId TINYINT`
  - `tbl_Chart_Conjunctions`: `Planet1Id TINYINT`, `Planet2Id TINYINT`, `SignId TINYINT`
  - `tbl_Chart_Aspects`: `AspectingPlanetId TINYINT`, `AspectedTargetType VARCHAR(10)`, `AspectedPlanetId TINYINT`
  - `tbl_Chart_DashaPeriods`: `LordId TINYINT`
  - `tbl_Fact_PlanetAvastha`: `PlanetId TINYINT`, `ChartTypeId TINYINT`
  - `tbl_PlanetSignTransitEvents`: `UX_TransitEvent_Planet_At` unique index

- [ ] **Step 1: Write `db/04_add_chartfact_id_columns.sql`**

```sql
-- =====================================================================
-- 04 — Add integer-FK columns beside the existing name columns on every
-- chart-fact table (review item 2). All NULLable here; migration 05 backfills,
-- migration 06 tightens to NOT NULL + FK + CHECK.
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_ChartResults', 'RuleSetId') IS NULL
ALTER TABLE dbo.tbl_ChartResults ADD
    RuleSetId       TINYINT      NULL,
    ChartTypeId     TINYINT      NULL,
    CalculationKind VARCHAR(20)  NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'PlanetId') IS NULL
ALTER TABLE dbo.tbl_Chart_KeyDetails ADD
    PlanetId                 TINYINT NULL,
    SignId                   TINYINT NULL,
    NakshatraLordPlanetId    TINYINT NULL,
    NakshatraSubLordPlanetId TINYINT NULL,
    SignLordPlanetId         TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_HouseLords', 'HouseSignId') IS NULL
ALTER TABLE dbo.tbl_Chart_HouseLords ADD
    HouseSignId        TINYINT NULL,
    LordPlanetId       TINYINT NULL,
    LordPlacedInSignId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions', 'Planet1Id') IS NULL
ALTER TABLE dbo.tbl_Chart_Conjunctions ADD
    Planet1Id TINYINT NULL,
    Planet2Id TINYINT NULL,
    SignId    TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_Aspects', 'AspectingPlanetId') IS NULL
ALTER TABLE dbo.tbl_Chart_Aspects ADD
    AspectingPlanetId TINYINT     NULL,
    AspectedTargetType VARCHAR(10) NULL,
    AspectedPlanetId  TINYINT     NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_DashaPeriods', 'LordId') IS NULL
ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD LordId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Fact_PlanetAvastha', 'PlanetId') IS NULL
ALTER TABLE dbo.tbl_Fact_PlanetAvastha ADD
    PlanetId    TINYINT NULL,
    ChartTypeId TINYINT NULL;
GO
-- transit-event uniqueness (review item 5, additive part).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TransitEvent_Planet_At')
BEGIN
    IF EXISTS (
        SELECT PlanetId, EventDateTimeUtc FROM dbo.tbl_PlanetSignTransitEvents
        GROUP BY PlanetId, EventDateTimeUtc HAVING COUNT(*) > 1)
        THROW 50004, 'tbl_PlanetSignTransitEvents has duplicate (PlanetId, EventDateTimeUtc) rows — dedupe before creating UX_TransitEvent_Planet_At.', 1;
    CREATE UNIQUE INDEX UX_TransitEvent_Planet_At ON dbo.tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc);
END
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '04_add_chartfact_id_columns.sql', 'nullable *Id columns on chart-fact tables + transit unique'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '04_add_chartfact_id_columns.sql');
GO
PRINT '04 applied: *Id columns added (nullable).';
GO
```

- [ ] **Step 2: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/04_add_chartfact_id_columns.sql`
Expected: `04 applied: *Id columns added (nullable).` (If it `THROW`s on transit duplicates, dedupe with `;WITH d AS (SELECT *, ROW_NUMBER() OVER (PARTITION BY PlanetId, EventDateTimeUtc ORDER BY Id) rn FROM dbo.tbl_PlanetSignTransitEvents) DELETE FROM d WHERE rn > 1;` then re-apply.)

- [ ] **Step 3: Verify columns exist and are NULL**

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT COUNT(*) AS keydetails_rows, COUNT(PlanetId) AS planetid_populated FROM dbo.tbl_Chart_KeyDetails"`
Expected: `planetid_populated = 0` (backfill is the next task).

- [ ] **Step 4: Verify the app still builds and runs unchanged**

Run: `dotnet build Ikiastrro.slnx`
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-avastha`
Expected: build succeeds; `verify-avastha: ALL PASS` (new NULLable columns don't affect `SELECT *` round-trips because the Dapper models don't have the new properties yet).

- [ ] **Step 5: Commit**

```bash
git add db/04_add_chartfact_id_columns.sql
git commit -m "$(printf 'feat(db): add nullable *Id columns to chart-fact tables (review item 2)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 5: Backfill the id columns

**Files:**
- Create: `db/05_backfill_chartfact_ids.sql`

**Interfaces:**
- Consumes: the columns from Task 4; `tbl_Planets.PlanetName`, `tbl_SignAttributes.ZodiacEnumValue`, `tbl_Dim_ChartType.Code`.
- Produces: every non-Ascendant fact row has its `*Id` populated; `tbl_ChartResults` fully has `RuleSetId` + `CalculationKind` (+ `ChartTypeId` for position charts).

- [ ] **Step 1: Write `db/05_backfill_chartfact_ids.sql`**

```sql
-- =====================================================================
-- 05 — Backfill the *Id columns from the reference tables by NAME JOIN
-- (the unambiguous source of truth). Re-runnable. 'Ascendant' rows have no
-- tbl_Planets match and correctly stay NULL.
-- =====================================================================
USE [ikiastrro];
GO
-- tbl_ChartResults ----------------------------------------------------
UPDATE cr SET cr.RuleSetId = ra.Id
  FROM dbo.tbl_ChartResults cr
  CROSS JOIN (SELECT TOP 1 Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1) ra
  WHERE cr.RuleSetId IS NULL;

UPDATE cr SET cr.CalculationKind =
        CASE WHEN cr.ChartType = 'VimshottariDasha' THEN 'VimshottariDasha' ELSE 'PositionChart' END
  FROM dbo.tbl_ChartResults cr
  WHERE cr.CalculationKind IS NULL;

UPDATE cr SET cr.ChartTypeId = ct.Id
  FROM dbo.tbl_ChartResults cr
  JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = cr.ChartType
  WHERE cr.ChartTypeId IS NULL;
GO
-- tbl_Chart_KeyDetails ----------------------------------------------------
UPDATE kd SET kd.PlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.Planet
  WHERE kd.PlanetId IS NULL;
UPDATE kd SET kd.SignId = sa.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign
  WHERE kd.SignId IS NULL;
UPDATE kd SET kd.NakshatraLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraLordPlanet
  WHERE kd.NakshatraLordPlanetId IS NULL AND kd.NakshatraLordPlanet IS NOT NULL;
UPDATE kd SET kd.NakshatraSubLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraSubLordPlanet
  WHERE kd.NakshatraSubLordPlanetId IS NULL AND kd.NakshatraSubLordPlanet IS NOT NULL;
UPDATE kd SET kd.SignLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.SignLordPlanet
  WHERE kd.SignLordPlanetId IS NULL AND kd.SignLordPlanet IS NOT NULL;
GO
-- tbl_Chart_HouseLords ----------------------------------------------------
UPDATE hl SET hl.HouseSignId = sa.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.HouseSign
  WHERE hl.HouseSignId IS NULL;
UPDATE hl SET hl.LordPlanetId = p.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_Planets p ON p.PlanetName = hl.LordPlanet
  WHERE hl.LordPlanetId IS NULL;
UPDATE hl SET hl.LordPlacedInSignId = sa.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.LordPlacedInSign
  WHERE hl.LordPlacedInSignId IS NULL;
GO
-- tbl_Chart_Conjunctions ----------------------------------------------------
UPDATE c SET c.Planet1Id = p.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_Planets p ON p.PlanetName = c.Planet1
  WHERE c.Planet1Id IS NULL;
UPDATE c SET c.Planet2Id = p.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_Planets p ON p.PlanetName = c.Planet2
  WHERE c.Planet2Id IS NULL;
UPDATE c SET c.SignId = sa.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = c.Sign
  WHERE c.SignId IS NULL;
GO
-- tbl_Chart_Aspects ----------------------------------------------------
UPDATE a SET a.AspectingPlanetId = p.Id
  FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_Planets p ON p.PlanetName = a.AspectingPlanet
  WHERE a.AspectingPlanetId IS NULL;
UPDATE a SET a.AspectedTargetType =
        CASE WHEN a.AspectedTarget = 'Ascendant' THEN 'Ascendant' ELSE 'Planet' END
  FROM dbo.tbl_Chart_Aspects a
  WHERE a.AspectedTargetType IS NULL;
UPDATE a SET a.AspectedPlanetId = p.Id
  FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_Planets p ON p.PlanetName = a.AspectedTarget
  WHERE a.AspectedPlanetId IS NULL AND a.AspectedTarget <> 'Ascendant';
GO
-- tbl_Chart_DashaPeriods ----------------------------------------------------
UPDATE dp SET dp.LordId = p.Id
  FROM dbo.tbl_Chart_DashaPeriods dp JOIN dbo.tbl_Planets p ON p.PlanetName = dp.Lord
  WHERE dp.LordId IS NULL;
GO
-- tbl_Fact_PlanetAvastha ----------------------------------------------------
UPDATE av SET av.PlanetId = p.Id
  FROM dbo.tbl_Fact_PlanetAvastha av JOIN dbo.tbl_Planets p ON p.PlanetName = av.Planet
  WHERE av.PlanetId IS NULL;
UPDATE av SET av.ChartTypeId = ct.Id
  FROM dbo.tbl_Fact_PlanetAvastha av JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = av.ChartType
  WHERE av.ChartTypeId IS NULL;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '05_backfill_chartfact_ids.sql', 'backfilled *Id columns by name join'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '05_backfill_chartfact_ids.sql');
GO
PRINT '05 applied: id columns backfilled.';
GO
```

- [ ] **Step 2: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/05_backfill_chartfact_ids.sql`
Expected: `05 applied: id columns backfilled.`

- [ ] **Step 3: Spot-check the backfill**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PlanetId IS NULL AND Planet <> 'Ascendant') AS kd_null_planet, (SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE SignId IS NULL) AS kd_null_sign, (SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE RuleSetId IS NULL) AS cr_null_ruleset, (SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart' AND ChartTypeId IS NULL) AS cr_pos_null_ct"
```
Expected: **all four counts = 0.** Any non-zero means a name that didn't resolve — inspect with `SELECT DISTINCT Planet FROM dbo.tbl_Chart_KeyDetails WHERE PlanetId IS NULL` etc. and fix the reference data or the join.

- [ ] **Step 4: Commit**

```bash
git add db/05_backfill_chartfact_ids.sql
git commit -m "$(printf 'feat(db): backfill chart-fact *Id columns by name join\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 6: `verify-schema` CLI mode (Phase 1 assertions)

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (add a new `if (args.Length > 0 && args[0] == "verify-schema")` block next to the existing `verify-avastha` block near line 225)

**Interfaces:**
- Consumes: `connectionFactory` (a `SqlConnectionFactory` already in Program.cs scope); Dapper (`using Dapper;` — add if not present at top of file).
- Produces: `dotnet run --project src/Ikiastrro.Cli -- verify-schema` prints `[PASS]`/`[FAIL]` per assertion and `Environment.Exit(0|1)`.

- [ ] **Step 1: Add the `verify-schema` block**

Insert immediately after the closing brace of the `verify-avastha` block:

```csharp
if (args.Length > 0 && args[0] == "verify-schema")
{
    using var conn = connectionFactory.CreateOpenConnection();
    var failures = 0;
    void Check(string label, long violations)
    {
        var ok = violations == 0;
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}: {violations} violation(s)");
        if (!ok) failures++;
    }
    long Count(string sql) => conn.ExecuteScalar<long>(sql);

    // -- backfill completeness (Phase 1) --
    Check("KeyDetails.PlanetId populated (non-Ascendant)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE PlanetId IS NULL AND Planet <> 'Ascendant'"));
    Check("KeyDetails.SignId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE SignId IS NULL"));
    Check("HouseLords ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_HouseLords WHERE HouseSignId IS NULL OR LordPlanetId IS NULL OR LordPlacedInSignId IS NULL"));
    Check("Conjunctions ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE Planet1Id IS NULL OR Planet2Id IS NULL OR SignId IS NULL"));
    Check("Aspects ids populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Aspects WHERE AspectingPlanetId IS NULL OR AspectedTargetType IS NULL OR (AspectedTargetType = 'Planet' AND AspectedPlanetId IS NULL)"));
    Check("DashaPeriods.LordId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods WHERE LordId IS NULL"));
    Check("ChartResults.RuleSetId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE RuleSetId IS NULL"));
    Check("ChartResults position charts have ChartTypeId",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults WHERE CalculationKind = 'PositionChart' AND ChartTypeId IS NULL"));
    Check("PlanetAvastha.PlanetId populated",
        Count("SELECT COUNT(*) FROM dbo.tbl_Fact_PlanetAvastha WHERE PlanetId IS NULL"));

    // -- orphan-id checks --
    Check("KeyDetails.PlanetId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE kd.PlanetId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Planets p WHERE p.Id = kd.PlanetId)"));
    Check("KeyDetails.SignId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE kd.SignId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes s WHERE s.Id = kd.SignId)"));
    Check("ChartResults.ChartTypeId resolves",
        Count("SELECT COUNT(*) FROM dbo.tbl_ChartResults cr WHERE cr.ChartTypeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType ct WHERE ct.Id = cr.ChartTypeId)"));

    // -- domain probes (become CHECK constraints in migration 06) --
    Check("KeyDetails longitude in [0,360)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE NirayanaLongitudeDegrees < 0 OR NirayanaLongitudeDegrees >= 360"));
    Check("KeyDetails degrees-in-sign in [0,30)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE DegreesInSignDecimal IS NOT NULL AND (DegreesInSignDecimal < 0 OR DegreesInSignDecimal >= 30)"));
    Check("KeyDetails houses in [1,12]",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE HouseNumberFromLagna NOT BETWEEN 1 AND 12 OR HouseNumberFromSun NOT BETWEEN 1 AND 12 OR HouseNumberFromMoon NOT BETWEEN 1 AND 12"));
    Check("KeyDetails pada in [1,4]",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails WHERE NakshatraPada IS NOT NULL AND NakshatraPada NOT BETWEEN 1 AND 4"));
    Check("BirthDetails lat/long in range",
        Count("SELECT COUNT(*) FROM dbo.tbl_BirthDetails WHERE Latitude NOT BETWEEN -90 AND 90 OR Longitude NOT BETWEEN -180 AND 180"));
    Check("DashaPeriods start < end",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods WHERE StartDate >= EndDate OR StartDayOffset > EndDayOffset"));

    // -- rule-set sanity --
    Check("exactly one active rule set",
        Count("SELECT ABS(COUNT(*) - 1) FROM dbo.tbl_Rule_Sets WHERE IsActive = 1"));

    Console.WriteLine(failures == 0 ? "\nverify-schema: ALL PASS" : $"\nverify-schema: {failures} FAILURE(S)");
    Environment.Exit(failures == 0 ? 0 : 1);
}
```

- [ ] **Step 2: Ensure `using Dapper;` is at the top of `Program.cs`**

Run: `grep -n "using Dapper;" src/Ikiastrro.Cli/Program.cs`
If absent, add `using Dapper;` with the other `using`s.

- [ ] **Step 3: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds.

- [ ] **Step 4: Run it**

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Expected: `verify-schema: ALL PASS`, process exit code 0. If any `[FAIL]`, fix the data (usually a missed backfill in Task 5) and re-run.

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(cli): verify-schema mode (Phase 1 assertion set)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 7: Fold Phase 1 into the baseline

**Files:**
- Modify: `db/ikiastrro.sql`

- [ ] **Step 1: Add `dbo.SchemaMigrations` DDL** near the top of `db/ikiastrro.sql`, right after the `USE [ikiastrro]` / initial `GO`, using the same guarded `IF OBJECT_ID(...) IS NULL CREATE TABLE ...` form as Task 1's script (no `INSERT` into the ledger in the baseline).

- [ ] **Step 2: Add `tbl_Dim_ChartType` DDL + seed** in the reference-tables region (near `tbl_Dim_AvasthaState`), guarded `IF OBJECT_ID(...) IS NULL` / `IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType)`.

- [ ] **Step 3: Add the seven new columns to the `tbl_Rule_Sets` `CREATE TABLE`** in the baseline (inline in the column list — a fresh build has no rows so no defaults-backfill concern) and add the two `CREATE UNIQUE INDEX` statements after it.

- [ ] **Step 4: Add the new `*Id` / `CalculationKind` / `AspectedTargetType` columns inline** to each affected `CREATE TABLE` in the baseline (`tbl_ChartResults`, `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects`, `tbl_Chart_DashaPeriods`, `tbl_Fact_PlanetAvastha`), still NULLable at this point. Add `CREATE UNIQUE INDEX UX_TransitEvent_Planet_At` after `tbl_PlanetSignTransitEvents`.

- [ ] **Step 5: Rebuild a scratch DB from the baseline and verify**

```
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END"
sqlcmd -S localhost -E -Q "CREATE DATABASE ikiastrro_scratch"
sqlcmd -S localhost -E -d ikiastrro_scratch -i db/ikiastrro.sql
sqlcmd -S localhost -E -d ikiastrro_scratch -Q "SELECT COUNT(*) FROM dbo.tbl_Dim_ChartType; SELECT COL_LENGTH('dbo.tbl_Chart_KeyDetails','PlanetId'), COL_LENGTH('dbo.tbl_Rule_Sets','VersionNumber')"
```
Expected: baseline runs clean; `tbl_Dim_ChartType` has 6 rows; both `COL_LENGTH`s are non-NULL. Then drop `ikiastrro_scratch`.

- [ ] **Step 6: Commit**

```bash
git add db/ikiastrro.sql
git commit -m "$(printf 'chore(db): fold Phase 1 (ids, dim_charttype, rule-set version) into baseline\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

**PHASE 1 CHECKPOINT:** `dotnet build Ikiastrro.slnx` green · `verify-schema` ALL PASS · `verify-avastha` ALL PASS · baseline rebuilds clean. The app writes exactly as before; the new columns are populated for existing rows only.

---

# PHASE 2 — ENFORCE

## Task 8: `AstroIds` helper + Core model `*Id` properties

**Files:**
- Create: `src/Ikiastrro.Core/Astro/AstroIds.cs`
- Modify: `src/Ikiastrro.Core/Models/ChartResult.cs`, `ChartKeyDetail.cs`, `ChartHouseLord.cs`, `ChartConjunction.cs`, `ChartAspect.cs`, `src/Ikiastrro.Core/Dasha/DashaPeriodRecord.cs`, `src/Ikiastrro.Core/Models/AvasthaModels.cs`

**Interfaces:**
- Produces:
  - `Ikiastrro.Core.Astro.AstroIds.PlanetId(PlanetName p) → int` ( `(int)p + 1` ), `AstroIds.SignId(ZodiacName z) → int` ( `(int)z + 1` ), `AstroIds.PlanetIdOrNull(string planetName) → int?` (returns null for `"Ascendant"`, else parses the enum and offsets).
  - `ChartResult`: `int RuleSetId`, `int? ChartTypeId`, `string CalculationKind`.
  - `ChartKeyDetail`: `int? PlanetId`, `int? SignId`, `int? NakshatraLordPlanetId`, `int? NakshatraSubLordPlanetId`, `int? SignLordPlanetId`.
  - `ChartHouseLord`: `int HouseSignId`, `int LordPlanetId`, `int LordPlacedInSignId`.
  - `ChartConjunction`: `int Planet1Id`, `int Planet2Id`, `int SignId`.
  - `ChartAspect`: `int AspectingPlanetId`, `string AspectedTargetType`, `int? AspectedPlanetId`.
  - `DashaPeriodRecord`: `int LordId`.
  - `PlanetAvasthaFact`: `byte? PlanetId`.

- [ ] **Step 1: Write `src/Ikiastrro.Core/Astro/AstroIds.cs`**

```csharp
namespace Ikiastrro.Core.Astro;

/// <summary>
/// Maps the Core enums to their reference-table primary keys.
///   tbl_Planets.Id       = (int)PlanetName + 1   (Sun=0 -> 1 ... Ketu=8 -> 9)
///   tbl_SignAttributes.Id = (int)ZodiacName + 1  (Aries=0 -> 1 ... Pisces=11 -> 12)
/// The DB backfill uses name joins, not this offset — this is the write-path source
/// for freshly computed rows.
/// </summary>
public static class AstroIds
{
    public const int PlanetIdOffset = 1;
    public const int SignIdOffset = 1;

    public static int PlanetId(PlanetName planet) => (int)planet + PlanetIdOffset;

    public static int SignId(ZodiacName sign) => (int)sign + SignIdOffset;

    /// <summary>Null for "Ascendant" (not a graha); otherwise the tbl_Planets.Id for the name.</summary>
    public static int? PlanetIdOrNull(string? planetName) =>
        string.IsNullOrEmpty(planetName) || planetName == "Ascendant"
            ? null
            : Enum.TryParse<PlanetName>(planetName, out var p) ? PlanetId(p) : null;
}
```

- [ ] **Step 2: Add a self-check to `verify-schema` for the offset**

In the `verify-schema` block (`Program.cs`), add near the top:

```csharp
    Check("PlanetId offset matches tbl_Planets",
        Count("SELECT COUNT(*) FROM dbo.tbl_Planets WHERE Id <> (CASE PlanetName WHEN 'Sun' THEN 1 WHEN 'Moon' THEN 2 WHEN 'Mars' THEN 3 WHEN 'Mercury' THEN 4 WHEN 'Jupiter' THEN 5 WHEN 'Venus' THEN 6 WHEN 'Saturn' THEN 7 WHEN 'Rahu' THEN 8 WHEN 'Ketu' THEN 9 END)"));
```

- [ ] **Step 3: Add the properties to each model**

`ChartResult.cs` — add after `public int BirthDetailId { get; set; }`:
```csharp
    /// <summary>FK to tbl_Rule_Sets — the rule-set version this chart was computed under.</summary>
    public int RuleSetId { get; set; } = 1;
    /// <summary>FK to tbl_Dim_ChartType. Null when CalculationKind != "PositionChart" (e.g. VimshottariDasha).</summary>
    public int? ChartTypeId { get; set; }
    /// <summary>"PositionChart" | "VimshottariDasha" — separates divisional charts from the Dasha run.</summary>
    public string CalculationKind { get; set; } = "PositionChart";
```

`ChartKeyDetail.cs` — add beside the matching name properties:
```csharp
    /// <summary>FK to tbl_Planets. Null for the Ascendant/Lagna row.</summary>
    public int? PlanetId { get; set; }
    /// <summary>FK to tbl_SignAttributes.</summary>
    public int? SignId { get; set; }
    public int? NakshatraLordPlanetId { get; set; }
    public int? NakshatraSubLordPlanetId { get; set; }
    public int? SignLordPlanetId { get; set; }
```

`ChartHouseLord.cs`:
```csharp
    public int HouseSignId { get; set; }
    public int LordPlanetId { get; set; }
    public int LordPlacedInSignId { get; set; }
```

`ChartConjunction.cs`:
```csharp
    /// <summary>FK to tbl_Planets. Canonically Planet1Id &lt; Planet2Id.</summary>
    public int Planet1Id { get; set; }
    public int Planet2Id { get; set; }
    public int SignId { get; set; }
```

`ChartAspect.cs`:
```csharp
    public int AspectingPlanetId { get; set; }
    /// <summary>"Planet" | "Ascendant".</summary>
    public string AspectedTargetType { get; set; } = "Planet";
    /// <summary>FK to tbl_Planets; null when AspectedTargetType == "Ascendant".</summary>
    public int? AspectedPlanetId { get; set; }
```

`DashaPeriodRecord.cs` — add after `public string Lord`:
```csharp
    /// <summary>FK to tbl_Planets for Lord.</summary>
    public int LordId { get; set; }
```

`AvasthaModels.cs` — in `PlanetAvasthaFact`, add after `public string Planet`:
```csharp
    /// <summary>FK to tbl_Planets for Planet.</summary>
    public byte? PlanetId { get; set; }
```

- [ ] **Step 4: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds (properties are additive; nothing consumes them yet).

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Core/Astro/AstroIds.cs src/Ikiastrro.Core/Models/ src/Ikiastrro.Core/Dasha/DashaPeriodRecord.cs src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(core): AstroIds helper + *Id properties on chart-fact models\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 9: `ChartAnalyzer` populates the ids

**Files:**
- Modify: `src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs`

**Interfaces:**
- Consumes: `AstroIds` (Task 8); the `PlanetName` / `ZodiacName` values `ChartAnalyzer` already has when building each row.
- Produces: every `ChartKeyDetail`, `ChartHouseLord`, `ChartConjunction`, `ChartAspect` returned by `ChartAnalyzer.Compute` has its `*Id` fields set.

- [ ] **Step 1: Read `ChartAnalyzer.Compute` to find where each row type is constructed**

Run: `grep -n "new ChartKeyDetail\|new ChartHouseLord\|new ChartConjunction\|new ChartAspect" src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs`

- [ ] **Step 2: At each `new ChartKeyDetail { ... }` initializer, add** (using the same `PlanetName`/`ZodiacName` locals already used to set `Planet =` / `Sign =`):
```csharp
    PlanetId = AstroIds.PlanetIdOrNull(planet.ToString()),   // null for Ascendant
    SignId = AstroIds.SignId(sign),
    NakshatraLordPlanetId = AstroIds.PlanetIdOrNull(nakshatraLordName),
    NakshatraSubLordPlanetId = AstroIds.PlanetIdOrNull(nakshatraSubLordName),
    SignLordPlanetId = AstroIds.PlanetIdOrNull(signLordName),
```
(adjust the local names to whatever the method already calls them; if a value is only available as a string, `PlanetIdOrNull` handles it; if only as a `PlanetName`, use `AstroIds.PlanetId(...)`).

- [ ] **Step 3: At `new ChartHouseLord { ... }`, add:**
```csharp
    HouseSignId = AstroIds.SignId(houseSign),
    LordPlanetId = AstroIds.PlanetId(lordPlanet),
    LordPlacedInSignId = AstroIds.SignId(lordPlacedInSign),
```

- [ ] **Step 4: At `new ChartConjunction { ... }`, add** — canonicalize here so `Planet1Id < Planet2Id`:
```csharp
    Planet1Id = Math.Min(AstroIds.PlanetId(planetA), AstroIds.PlanetId(planetB)),
    Planet2Id = Math.Max(AstroIds.PlanetId(planetA), AstroIds.PlanetId(planetB)),
    SignId = AstroIds.SignId(sign),
```
and set `Planet1` / `Planet2` from whichever of `planetA` / `planetB` has the lower id, so the name pair matches the id pair.

- [ ] **Step 5: At `new ChartAspect { ... }`, add:**
```csharp
    AspectingPlanetId = AstroIds.PlanetId(aspectingPlanet),
    AspectedTargetType = aspectedTarget == "Ascendant" ? "Ascendant" : "Planet",
    AspectedPlanetId = AstroIds.PlanetIdOrNull(aspectedTarget),
```

- [ ] **Step 6: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds.

- [ ] **Step 7: Sanity-run a recompute against one person and inspect**

Run: `dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails`
Then: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT TOP 5 Planet, PlanetId, Sign, SignId FROM dbo.tbl_Chart_KeyDetails ORDER BY Id DESC"`
Expected: newly written rows have `PlanetId`/`SignId` matching the names (Sun→1, … ; Ascendant→NULL).

- [ ] **Step 8: Run `verify-schema`**

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Expected: `ALL PASS` (recompute preserved every id).

- [ ] **Step 9: Commit**

```bash
git add src/Ikiastrro.Core/Calculators/ChartAnalyzer.cs
git commit -m "$(printf 'feat(core): ChartAnalyzer populates chart-fact *Id fields\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 10: `ChartTypeRepository` + rule-set plumbing

**Files:**
- Create: `src/Ikiastrro.Core/Models/ChartTypeRow.cs`
- Create: `src/Ikiastrro.Data/ChartTypeRepository.cs`
- Modify: `src/Ikiastrro.Data/RuleSetRepository.cs`

**Interfaces:**
- Produces:
  - `Ikiastrro.Core.Models.ChartTypeRow(int Id, string Code, string DisplayName, int? DivisionalFactor, string Category, int DisplayOrder)`.
  - `Ikiastrro.Data.ChartTypeRepository` : `IReadOnlyList<ChartTypeRow> GetAll()`, `IReadOnlyDictionary<string,int> CodeToId()`.
  - `RuleSet` record gains `int VersionNumber, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsPublished`.

- [ ] **Step 1: Write `src/Ikiastrro.Core/Models/ChartTypeRow.cs`**

```csharp
namespace Ikiastrro.Core.Models;

/// <summary>One row of dbo.tbl_Dim_ChartType — the controlled vocabulary for ChartResults.ChartTypeId.</summary>
public record ChartTypeRow(
    int Id, string Code, string DisplayName, int? DivisionalFactor, string Category, int DisplayOrder);
```

- [ ] **Step 2: Write `src/Ikiastrro.Data/ChartTypeRepository.cs`**

```csharp
using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Dim_ChartType — the fixed chart-type vocabulary. Six rows today (D1/D2/D6/D9/D10/D11).</summary>
public class ChartTypeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartTypeRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<ChartTypeRow> GetAll()
    {
        const string sql = "SELECT CAST(Id AS INT) AS Id, Code, DisplayName, CAST(DivisionalFactor AS INT) AS DivisionalFactor, Category, CAST(DisplayOrder AS INT) AS DisplayOrder FROM dbo.tbl_Dim_ChartType ORDER BY DisplayOrder";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartTypeRow>(sql).ToList();
    }

    public IReadOnlyDictionary<string, int> CodeToId() =>
        GetAll().ToDictionary(r => r.Code, r => r.Id, StringComparer.OrdinalIgnoreCase);
}
```

- [ ] **Step 3: Extend `RuleSetRepository`**

Change the `RuleSet` record and `SelectColumns`:
```csharp
public record RuleSet(
    int Id, string RuleSetName, string? Description,
    int VersionNumber, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsPublished);
```
```csharp
    private const string SelectColumns =
        "CAST(Id AS INT) AS Id, RuleSetName, Description, VersionNumber, EffectiveFromUtc, EffectiveToUtc, IsPublished";
```

- [ ] **Step 4: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds. If `list-rule-sets` / `show-rules` in `Program.cs` construct a `RuleSet` positionally anywhere, fix those call sites to the new shape (grep `new RuleSet(`).

- [ ] **Step 5: Verify**

Run: `dotnet run --project src/Ikiastrro.Cli -- list-rule-sets`
Expected: prints without error; the active set shows `VersionNumber` 1.

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Core/Models/ChartTypeRow.cs src/Ikiastrro.Data/ChartTypeRepository.cs src/Ikiastrro.Data/RuleSetRepository.cs
git commit -m "$(printf 'feat(data): ChartTypeRepository + versioned RuleSet record\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 11: Repositories write the `*Id` columns

**Files:**
- Modify: `src/Ikiastrro.Data/ChartResultsRepository.cs`, `ChartKeyDetailsRepository.cs`, `ChartHouseLordsRepository.cs`, `ChartConjunctionsRepository.cs`, `ChartAspectsRepository.cs`, `DashaPeriodsRepository.cs`, `PlanetAvasthaRepository.cs`

**Interfaces:**
- Consumes: the model `*Id` properties (Task 8).
- Produces: every repository `INSERT` writes the new columns. No delete/read method signatures change in this task.

- [ ] **Step 1: `ChartResultsRepository.Insert`** — extend the column + value lists:
```csharp
        const string sql = """
            INSERT INTO dbo.tbl_ChartResults
                (BirthDetailId, ChartType, ChartTypeId, CalculationKind, RuleSetId,
                 Ayanamsha, HouseSystem, EngineVersion, ResultJson, ComputedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@BirthDetailId, @ChartType, @ChartTypeId, @CalculationKind, @RuleSetId,
                 @Ayanamsha, @HouseSystem, @EngineVersion, @ResultJson, @ComputedAt)
            """;
```

- [ ] **Step 2: `ChartKeyDetailsRepository.InsertAll`** — add to the column list and the `VALUES` list, in matching positions:
```
    , PlanetId, SignId, NakshatraLordPlanetId, NakshatraSubLordPlanetId, SignLordPlanetId
```
```
    , @PlanetId, @SignId, @NakshatraLordPlanetId, @NakshatraSubLordPlanetId, @SignLordPlanetId
```

- [ ] **Step 3: `ChartHouseLordsRepository.InsertAll`** — add `, HouseSignId, LordPlanetId, LordPlacedInSignId` and `, @HouseSignId, @LordPlanetId, @LordPlacedInSignId`.

- [ ] **Step 4: `ChartConjunctionsRepository.InsertAll`** — add `, Planet1Id, Planet2Id, SignId` and `, @Planet1Id, @Planet2Id, @SignId`.

- [ ] **Step 5: `ChartAspectsRepository.InsertAll`** — add `, AspectingPlanetId, AspectedTargetType, AspectedPlanetId` and `, @AspectingPlanetId, @AspectedTargetType, @AspectedPlanetId`.

- [ ] **Step 6: `DashaPeriodsRepository.InsertTree`** — add `LordId` to the `INSERT` column list and `@LordId` to `VALUES`, and in the anonymous parameter object add `LordId = (int)period.Lord + Ikiastrro.Core.Astro.AstroIds.PlanetIdOffset` (`period.Lord` is a `PlanetName`).

- [ ] **Step 7: `PlanetAvasthaRepository.InsertAll`** — add `, PlanetId` and `, @PlanetId`. (The `PlanetAvasthaFact.PlanetId` is set by whatever computes the fact — see `PlanetAvasthaComputer`; add `PlanetId = AstroIds.PlanetIdOrNull(planetName)` where the fact is constructed there. Grep `new PlanetAvasthaFact`.)

- [ ] **Step 8: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds.

- [ ] **Step 9: Full recompute + verify**

Run: `dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails`
Then: `dotnet run --project src/Ikiastrro.Cli -- backfill-dasha` (re-writes dasha trees so `LordId` is populated on existing rows too — check the mode name with `grep -n "backfill-dasha\|compute-dasha" src/Ikiastrro.Cli/Program.cs`; if `backfill-dasha` only fills missing, use `compute-dasha <name>` per person or add a note to re-run `GenerateAll`).
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Expected: `ALL PASS`.

- [ ] **Step 10: Commit**

```bash
git add src/Ikiastrro.Data/
git commit -m "$(printf 'feat(data): repositories write chart-fact *Id columns\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 12: `ChartGenerationService` + `VimshottariDashaService` set the header fields

**Files:**
- Modify: `src/Ikiastrro.Data/ChartGenerationService.cs`
- Modify: `src/Ikiastrro.Data/VimshottariDashaService.cs`

**Interfaces:**
- Consumes: `RuleSetRepository.GetActive()` (Task 10), `ChartTypeRepository.CodeToId()` (Task 10).
- Produces: every `ChartResult` inserted by these two services carries `RuleSetId`, `ChartTypeId` (or null for Dasha), and `CalculationKind`.

- [ ] **Step 1: Inject the two repositories into `ChartGenerationService`**

Add `RuleSetRepository ruleSetRepo` and `ChartTypeRepository chartTypeRepo` constructor parameters + fields. Update the DI registration in `src/Ikiastrro.Cli/Program.cs` and `src/Ikiastrro.Web/Program.cs` (grep `new ChartGenerationService(` and any `AddScoped<ChartGenerationService>` / manual wiring).

- [ ] **Step 2: Resolve once per call**

In `GenerateAll`, `GenerateMissing`, and `RecomputeAnalytics`, near the top:
```csharp
        var activeRuleSetId = _ruleSetRepo.GetActive().Id;
        var codeToChartTypeId = _chartTypeRepo.CodeToId();
```

- [ ] **Step 3: Stamp each `ChartResult` before insert**

Wherever a position-chart `ChartResult` is built/inserted (`PersistCharts`, `GenerateMissing`'s `calc.BuildResult(...)` path), set:
```csharp
        result.RuleSetId = activeRuleSetId;
        result.CalculationKind = "PositionChart";
        result.ChartTypeId = codeToChartTypeId[result.ChartType];
```

- [ ] **Step 4: `VimshottariDashaService.ComputeAndStore`** — on its `new ChartResult { ... }` add:
```csharp
        CalculationKind = "VimshottariDasha",
        ChartTypeId = null,
        RuleSetId = 1,   // Dasha has no rule-set variation yet; pin the base set
```

- [ ] **Step 5: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds.

- [ ] **Step 6: Regenerate one person end-to-end**

Run: `dotnet run --project src/Ikiastrro.Cli -- compute-all <SeededPersonName>` (check the exact mode: `grep -n '"compute-all"' src/Ikiastrro.Cli/Program.cs`)
Then: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT ChartType, ChartTypeId, CalculationKind, RuleSetId FROM dbo.tbl_ChartResults WHERE BirthDetailId = (SELECT MIN(Id) FROM dbo.tbl_BirthDetails)"`
Expected: D1..D11 rows have `ChartTypeId` set + `CalculationKind = PositionChart`; the `VimshottariDasha` row has `ChartTypeId = NULL` + `CalculationKind = VimshottariDasha`; all have `RuleSetId = 1`.

- [ ] **Step 7: `verify-schema`**

Run: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Expected: `ALL PASS`.

- [ ] **Step 8: Commit**

```bash
git add src/Ikiastrro.Data/ChartGenerationService.cs src/Ikiastrro.Data/VimshottariDashaService.cs src/Ikiastrro.Cli/Program.cs src/Ikiastrro.Web/Program.cs
git commit -m "$(printf 'feat(data): stamp RuleSetId/ChartTypeId/CalculationKind on every ChartResult\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 13: Tighten to `NOT NULL` + FK + `CHECK`

**Files:**
- Create: `db/06_add_chartfact_constraints.sql`
- Modify: `src/Ikiastrro.Cli/Program.cs` (extend `verify-schema` with the Phase 2 assertions)

**Interfaces:**
- Consumes: fully backfilled + app-populated id columns (Tasks 5, 9, 11, 12).
- Produces: FKs on every `*Id`; `NOT NULL` where the name column is `NOT NULL`; `CHECK` domains; `CHECK (Planet1Id < Planet2Id)` + `UX_Conjunctions_Result_Pair`; dasha same-chart parent FK; `tbl_BirthDetails` unique-name dropped, lat/long `DECIMAL(9,6)`.

- [ ] **Step 1: Pre-flight — run `verify-schema` and confirm ALL PASS.** The domain probes must be clean before they become constraints. If any fail, fix the data first.

- [ ] **Step 2: Write `db/06_add_chartfact_constraints.sql`**

```sql
-- =====================================================================
-- 06 — Enforce the normalized shape (review items 2 + 5). Assumes migration
-- 05 backfill + the .NET write path (Tasks 9/11/12) have populated every id.
-- =====================================================================
USE [ikiastrro];
GO
-- ChartResults ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartResults_RuleSet')
BEGIN
    UPDATE dbo.tbl_ChartResults SET RuleSetId = (SELECT TOP 1 Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1) WHERE RuleSetId IS NULL;
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN RuleSetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id);
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN CalculationKind VARCHAR(20) NOT NULL;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT DF_ChartResults_CalcKind DEFAULT ('PositionChart') FOR CalculationKind;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_ChartType FOREIGN KEY (ChartTypeId) REFERENCES dbo.tbl_Dim_ChartType (Id);
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT CK_ChartResults_KindType CHECK (
        (CalculationKind = 'PositionChart' AND ChartTypeId IS NOT NULL) OR
        (CalculationKind <> 'PositionChart' AND ChartTypeId IS NULL));
END
GO
-- Chart_KeyDetails ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KeyDetails_Sign')
BEGIN
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN SignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD
        CONSTRAINT FK_KeyDetails_Planet          FOREIGN KEY (PlanetId)                 REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_Sign            FOREIGN KEY (SignId)                   REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT FK_KeyDetails_NakLord         FOREIGN KEY (NakshatraLordPlanetId)    REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_NakSubLord      FOREIGN KEY (NakshatraSubLordPlanetId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_SignLord        FOREIGN KEY (SignLordPlanetId)         REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_KeyDetails_Longitude       CHECK (NirayanaLongitudeDegrees >= 0 AND NirayanaLongitudeDegrees < 360),
        CONSTRAINT CK_KeyDetails_DegInSign       CHECK (DegreesInSignDecimal IS NULL OR (DegreesInSignDecimal >= 0 AND DegreesInSignDecimal < 30)),
        CONSTRAINT CK_KeyDetails_HouseLagna      CHECK (HouseNumberFromLagna BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_HouseSun        CHECK (HouseNumberFromSun BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_HouseMoon       CHECK (HouseNumberFromMoon BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_Pada            CHECK (NakshatraPada IS NULL OR NakshatraPada BETWEEN 1 AND 4);
END
GO
-- Chart_HouseLords ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HouseLords_Lord')
BEGIN
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN HouseSignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN LordPlanetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN LordPlacedInSignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ADD
        CONSTRAINT FK_HouseLords_HouseSign     FOREIGN KEY (HouseSignId)        REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT FK_HouseLords_Lord          FOREIGN KEY (LordPlanetId)       REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_HouseLords_LordInSign    FOREIGN KEY (LordPlacedInSignId) REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT CK_HouseLords_House         CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_HouseLords_LordHouses    CHECK (LordPlacedInHouseFromLagna BETWEEN 1 AND 12 AND LordPlacedInHouseFromSun BETWEEN 1 AND 12 AND LordPlacedInHouseFromMoon BETWEEN 1 AND 12);
END
GO
-- Chart_Conjunctions --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Conjunctions_Planet1')
BEGIN
    -- canonicalize any rows the old data stored out of order
    UPDATE dbo.tbl_Chart_Conjunctions
       SET Planet1Id = Planet2Id, Planet2Id = Planet1Id, Planet1 = Planet2, Planet2 = Planet1
     WHERE Planet1Id > Planet2Id;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN Planet1Id TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN Planet2Id TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN SignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ADD
        CONSTRAINT FK_Conjunctions_Planet1 FOREIGN KEY (Planet1Id) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Conjunctions_Planet2 FOREIGN KEY (Planet2Id) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Conjunctions_Sign    FOREIGN KEY (SignId)    REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT CK_Conjunctions_Canonical CHECK (Planet1Id < Planet2Id),
        CONSTRAINT CK_Conjunctions_House     CHECK (HouseNumberFromLagna BETWEEN 1 AND 12);
    CREATE UNIQUE INDEX UX_Conjunctions_Result_Pair ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1Id, Planet2Id);
END
GO
-- Chart_Aspects -----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Aspects_Aspecting')
BEGIN
    ALTER TABLE dbo.tbl_Chart_Aspects ALTER COLUMN AspectingPlanetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Aspects ALTER COLUMN AspectedTargetType VARCHAR(10) NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Aspects ADD
        CONSTRAINT FK_Aspects_Aspecting FOREIGN KEY (AspectingPlanetId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Aspects_Aspected  FOREIGN KEY (AspectedPlanetId)  REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_Aspects_TargetType CHECK (AspectedTargetType IN ('Planet','Ascendant')),
        CONSTRAINT CK_Aspects_TargetShape CHECK (
            (AspectedTargetType = 'Ascendant' AND AspectedPlanetId IS NULL) OR
            (AspectedTargetType = 'Planet'    AND AspectedPlanetId IS NOT NULL));
END
GO
-- Chart_DashaPeriods ----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DashaPeriods_Lord')
BEGIN
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ALTER COLUMN LordId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD
        CONSTRAINT FK_DashaPeriods_Lord FOREIGN KEY (LordId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_DashaPeriods_Dates   CHECK (StartDate < EndDate),
        CONSTRAINT CK_DashaPeriods_Offsets CHECK (StartDayOffset <= EndDayOffset),
        CONSTRAINT CK_DashaPeriods_Level   CHECK (LevelNumber BETWEEN 1 AND 3);
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT UQ_DashaPeriods_Result_Id UNIQUE (ChartResultId, Id);
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD ParentChartResultId AS ChartResultId PERSISTED;
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT FK_DashaPeriods_ParentSameChart
        FOREIGN KEY (ParentChartResultId, ParentDashaPeriodId) REFERENCES dbo.tbl_Chart_DashaPeriods (ChartResultId, Id);
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT UQ_DashaPeriods_Sibling UNIQUE (ChartResultId, ParentDashaPeriodId, SequenceInParent);
END
GO
-- Fact_PlanetAvastha --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Fact_PlanetAvastha_Planet')
    ALTER TABLE dbo.tbl_Fact_PlanetAvastha ADD CONSTRAINT FK_Fact_PlanetAvastha_Planet FOREIGN KEY (PlanetId) REFERENCES dbo.tbl_Planets (Id);
GO
-- BirthDetails ----------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BirthDetails_Name')
    DROP INDEX UX_BirthDetails_Name ON dbo.tbl_BirthDetails;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BirthDetails_Name')
    CREATE INDEX IX_BirthDetails_Name ON dbo.tbl_BirthDetails (Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BirthDetails_LatLong')
BEGIN
    ALTER TABLE dbo.tbl_BirthDetails ALTER COLUMN Latitude DECIMAL(9,6) NOT NULL;
    ALTER TABLE dbo.tbl_BirthDetails ALTER COLUMN Longitude DECIMAL(9,6) NOT NULL;
    ALTER TABLE dbo.tbl_BirthDetails ADD CONSTRAINT CK_BirthDetails_LatLong
        CHECK (Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180);
END
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '06_add_chartfact_constraints.sql', 'NOT NULL + FK + CHECK + canonical conjunction + dasha parent FK + BirthDetails fixes'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '06_add_chartfact_constraints.sql');
GO
PRINT '06 applied: chart-fact constraints enforced.';
GO
```

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/06_add_chartfact_constraints.sql`
Expected: `06 applied: chart-fact constraints enforced.` A constraint-violation error here means real data breaks an invariant — read the message, fix that data, re-apply (the `IF NOT EXISTS` guards make it safe).

- [ ] **Step 4: `BirthDetails` model — change `Latitude`/`Longitude` to `decimal`?**

The DB column is now `DECIMAL(9,6)`. Dapper maps `decimal` ↔ `decimal`. Check `BirthDetails.cs` — it declares `double Latitude`. `SELECT`ing a `DECIMAL` into a `double` property still works with Dapper, but for cleanliness change both to `double` → keep as `double` (geocoding produces `double`; the implicit narrowing on read is lossless within `DECIMAL(9,6)`). **No code change required**; note this decision in the commit message. If a runtime cast error appears, change the two properties to `decimal` and fix `NominatimPlaceResolver` / `ManualPlaceResolver` call sites.

- [ ] **Step 5: `verify-schema` — add the Phase 2 assertions**

Append to the `verify-schema` block:
```csharp
    // -- Phase 2: constraint-backed invariants --
    Check("every conjunction canonical (Planet1Id < Planet2Id)",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Conjunctions WHERE Planet1Id >= Planet2Id"));
    Check("aspect target shape consistent",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_Aspects WHERE (AspectedTargetType = 'Ascendant' AND AspectedPlanetId IS NOT NULL) OR (AspectedTargetType = 'Planet' AND AspectedPlanetId IS NULL)"));
    Check("no dasha parent points cross-chart",
        Count("SELECT COUNT(*) FROM dbo.tbl_Chart_DashaPeriods c JOIN dbo.tbl_Chart_DashaPeriods p ON p.Id = c.ParentDashaPeriodId WHERE p.ChartResultId <> c.ChartResultId"));
    Check("all six D-chart types present in dim",
        Count("SELECT ABS(COUNT(*) - 6) FROM dbo.tbl_Dim_ChartType"));
```

- [ ] **Step 6: Build + verify**

Run: `dotnet build Ikiastrro.slnx`
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-schema`
Then: `dotnet run --project src/Ikiastrro.Cli -- verify-avastha`
Expected: build green; both `ALL PASS`.

- [ ] **Step 7: Commit**

```bash
git add db/06_add_chartfact_constraints.sql src/Ikiastrro.Cli/Program.cs
git commit -m "$(printf 'feat(db): enforce chart-fact constraints — NOT NULL, FK, CHECK (review items 2, 5)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 14: Repoint views & TVFs to ids

**Files:**
- Create: `db/07_repoint_views_tvfs_to_ids.sql`

**Interfaces:**
- Consumes: the id columns + constraints (Task 13).
- Produces: `vw_Chart_Consolidated`, `vw_Chart_HouseNakshatraSpan`, `tvf_Chart_LifeWeeks`, `tvf_Chart_SadeSatiPeriods` join on `*Id` internally. **Output column names unchanged.**

- [ ] **Step 1: Capture current view/TVF definitions** so the rebuild keeps every output column:

Run: `sqlcmd -S localhost -E -d ikiastrro -Q "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.vw_Chart_Consolidated'))" -y 0`
(Repeat for `vw_Chart_HouseNakshatraSpan`, `tvf_Chart_LifeWeeks`, `tvf_Chart_SadeSatiPeriods`.) These also live in `db/ikiastrro.sql`.

- [ ] **Step 2: Write `db/07_repoint_views_tvfs_to_ids.sql`** — drop-and-recreate each object. For each, keep the exact `SELECT` output list; change only the joins:

- `vw_Chart_Consolidated`: change `LEFT JOIN dbo.tbl_Fact_PlanetAvastha av ON av.ChartResultId = kd.ChartResultId AND av.Planet = kd.Planet` → `... AND av.PlanetId = kd.PlanetId`. In the `Conjunct` `OUTER APPLY`, change `WHERE ChartResultId = kd.ChartResultId AND Planet1 = kd.Planet` / `Planet2 = kd.Planet` to `Planet1Id = kd.PlanetId` / `Planet2Id = kd.PlanetId` and `SELECT Planet2` / `SELECT Planet1` stay (still emitting names). In `RulesHouses`, change `hl.LordPlanet = kd.Planet` → `hl.LordPlanetId = kd.PlanetId`. In `AspectsCast`, change `AspectingPlanet = kd.Planet` → `AspectingPlanetId = kd.PlanetId`.
- `vw_Chart_HouseNakshatraSpan`: change `JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.HouseSign` → `ON sa.Id = hl.HouseSignId`.
- `tvf_Chart_LifeWeeks`: change the dasha-chart lookup `WHERE cr.BirthDetailId = bd.Id AND cr.ChartType = ''VimshottariDasha''` → `... AND cr.CalculationKind = ''VimshottariDasha''`.
- `tvf_Chart_SadeSatiPeriods`: change `JOIN tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign` → `ON sa.Id = kd.SignId` (leave the `kd.BirthDetailId` / `kd.ChartType = ''D1''` filter — Phase 3 rewrites that).

Wrap each `CREATE VIEW` that needs `QUOTED_IDENTIFIER OFF` (i.e. `vw_Chart_Consolidated`) in the same `EXEC dbo.sp_executesql @statement = N'...'` form the baseline uses. End with the `SchemaMigrations` insert + `PRINT '07 applied...'`.

- [ ] **Step 3: Apply it**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/07_repoint_views_tvfs_to_ids.sql`
Expected: `07 applied: views/TVFs on ids.`

- [ ] **Step 4: Compare view output before/after**

Run:
```
sqlcmd -S localhost -E -d ikiastrro -Q "SELECT TOP 20 Planet, Sign, ConjunctWith, RulesHouseNumbers, Aspects, BaaladiState FROM dbo.vw_Chart_Consolidated WHERE ChartResultId = (SELECT MIN(Id) FROM dbo.tbl_ChartResults WHERE CalculationKind='PositionChart') ORDER BY Planet"
```
Expected: same values a pre-migration run produced (spot-check `ConjunctWith` / `Aspects` strings look sane — planet names, comma-joined).

- [ ] **Step 5: Web smoke + life-weeks + sade-sati**

Run: `dotnet run --project src/Ikiastrro.Cli -- show-dasha <SeededPersonName>` (exercises the dasha read path)
Then launch the Web app and open `/charts/{id}` for a seeded person; confirm the consolidated table, dasha timeline, life-weeks grid, and Sade Sati table all render with data.

- [ ] **Step 6: Commit**

```bash
git add db/07_repoint_views_tvfs_to_ids.sql
git commit -m "$(printf 'refactor(db): repoint views/TVFs onto *Id joins (output unchanged)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 15: Fold Phase 2 into the baseline

**Files:**
- Modify: `db/ikiastrro.sql`

- [ ] **Step 1: Inline the FK / `CHECK` / `NOT NULL` from migration 06** into the relevant `CREATE TABLE` statements in `db/ikiastrro.sql` (a fresh build declares them inline — no `ALTER`). Include: `tbl_ChartResults` FKs + `CK_ChartResults_KindType`; `tbl_Chart_KeyDetails` FKs + all `CK_KeyDetails_*`; `tbl_Chart_HouseLords` FKs + `CK_HouseLords_*`; `tbl_Chart_Conjunctions` FKs + `CK_Conjunctions_Canonical` + `UX_Conjunctions_Result_Pair`; `tbl_Chart_Aspects` FKs + `CK_Aspects_*`; `tbl_Chart_DashaPeriods` FKs + `CK_DashaPeriods_*` + `UQ_DashaPeriods_Result_Id` + `ParentChartResultId` computed col + `FK_DashaPeriods_ParentSameChart` + `UQ_DashaPeriods_Sibling`; `tbl_Fact_PlanetAvastha` `FK_Fact_PlanetAvastha_Planet`; `tbl_BirthDetails` `DECIMAL(9,6)` + `CK_BirthDetails_LatLong` + `IX_BirthDetails_Name` (and remove the `UNIQUE`/`UX_BirthDetails_Name` from the baseline).

- [ ] **Step 2: Replace the four view/TVF definitions** in `db/ikiastrro.sql` with the id-joined versions from migration 07. Keep `vw_Chart_Consolidated` last in the file.

- [ ] **Step 3: Rebuild scratch DB and verify**

```
sqlcmd -S localhost -E -Q "IF DB_ID('ikiastrro_scratch') IS NOT NULL BEGIN ALTER DATABASE ikiastrro_scratch SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ikiastrro_scratch; END; CREATE DATABASE ikiastrro_scratch"
sqlcmd -S localhost -E -d ikiastrro_scratch -i db/ikiastrro.sql
sqlcmd -S localhost -E -d ikiastrro_scratch -Q "SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails'); SELECT name FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions')"
```
Expected: baseline runs clean; the FKs and `CK_Conjunctions_Canonical` are present. Drop `ikiastrro_scratch`.

- [ ] **Step 4: Commit**

```bash
git add db/ikiastrro.sql
git commit -m "$(printf 'chore(db): fold Phase 2 (constraints + id-joined views) into baseline\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

**PHASE 2 CHECKPOINT:** `dotnet build` green · `verify-schema` + `verify-avastha` + `verify-vargas` + `verify-functional-nature` all PASS · Web `/charts/{id}` renders · baseline rebuilds clean with constraints. New charts are written fully normalized; existing rows are backfilled and constraint-valid.

---

# PHASE 3 — DROP REDUNDANCY

## Task 16: Grep for consumers of the child-table `ChartType` / `ComputedAt`

**Files:** none (investigation task; its output shapes Tasks 17–19)

- [ ] **Step 1: Find every read of a child-table `ChartType`**

Run: `grep -rn "\.ChartType\b" src/Ikiastrro.Web/ src/Ikiastrro.Cli/` and `grep -rn "ChartType" src/Ikiastrro.Data/*Repository.cs`
Record which reads are on `ChartResult` (fine — stays) vs on `ChartKeyDetail` / `ChartHouseLord` / `ChartConjunction` / `ChartAspect` / `DashaPeriodRecord` / `PlanetAvasthaFact` (must move to reading it from the parent `ChartResult` or the consolidated view).

- [ ] **Step 2: Find every read of a child-table `ComputedAt`**

Run: `grep -rn "ComputedAt" src/Ikiastrro.Web/ src/Ikiastrro.Cli/`
Per spec §11: if any consumer uses a child `ComputedAt` as "when analytics were last recomputed", add `AnalyticsComputedAtUtc DATETIME2(0)` to `tbl_ChartResults` (updated by `RecomputeAnalytics`) instead of dropping the signal. If nothing reads it, drop it.

- [ ] **Step 3: Write findings into this task's checkbox as a comment and proceed.** No commit.

---

## Task 17: Rewrite the six `DeleteByBirthDetailId` methods

**Files:**
- Modify: `src/Ikiastrro.Data/ChartKeyDetailsRepository.cs`, `ChartHouseLordsRepository.cs`, `ChartConjunctionsRepository.cs`, `ChartAspectsRepository.cs`, `DashaPeriodsRepository.cs`, `PlanetAvasthaRepository.cs`

**Interfaces:**
- Produces: each `DeleteByBirthDetailId(int)` deletes via a subquery through `tbl_ChartResults` — no dependency on a child `BirthDetailId` column. Signatures unchanged, so `BirthDetailDeletionService` and `ChartGenerationService` are untouched.

- [ ] **Step 1: In each of the six repos, change the `DeleteByBirthDetailId` SQL** from `DELETE FROM dbo.tbl_Chart_X WHERE BirthDetailId = @BirthDetailId` to:
```csharp
        const string sql = """
            DELETE FROM dbo.tbl_Chart_KeyDetails
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            """;
```
(substitute the correct table name per repo: `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects`, `tbl_Chart_DashaPeriods`, `tbl_Fact_PlanetAvastha`).

- [ ] **Step 2: In each repo, also change `GetByBirthDetailId`** from `WHERE BirthDetailId = @BirthDetailId` to the same subquery form (the Web workspace one-shot load uses these):
```csharp
        const string sql = """
            SELECT * FROM dbo.tbl_Chart_KeyDetails
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            ORDER BY Id
            """;
```

- [ ] **Step 3: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds (the `BirthDetailId` column still exists at this point, so `SELECT *` still maps).

- [ ] **Step 4: Verify delete still cascades**

Run: `dotnet run --project src/Ikiastrro.Cli -- compute-all <SeededPersonName>` then use the CLI's delete path (grep `DeleteBirthDetail` / a `delete` mode) on a throwaway person, then:
`sqlcmd -S localhost -E -d ikiastrro -Q "SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_ChartResults cr WHERE cr.Id = kd.ChartResultId)"`
Expected: `0` orphans.

- [ ] **Step 5: Commit**

```bash
git add src/Ikiastrro.Data/
git commit -m "$(printf 'refactor(data): reach person through ChartResults in delete/get-by-birth paths\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 18: Drop the redundant columns

**Files:**
- Create: `db/08_drop_child_parent_duplication.sql`
- Modify: Core models (`ChartKeyDetail.cs`, `ChartHouseLord.cs`, `ChartConjunction.cs`, `ChartAspect.cs`, `DashaPeriodRecord.cs`, `AvasthaModels.cs`), repo INSERT lists (6 files), `src/Ikiastrro.Data/ChartGenerationService.cs`

**Interfaces:**
- Consumes: Task 17 (delete paths no longer need the columns), Task 16 findings (whether `AnalyticsComputedAtUtc` is added).
- Produces: `tbl_Chart_KeyDetails`, `tbl_Chart_HouseLords`, `tbl_Chart_Conjunctions`, `tbl_Chart_Aspects`, `tbl_Chart_DashaPeriods`, `tbl_Fact_PlanetAvastha` lose `BirthDetailId`, `ChartType`, and per-child `ComputedAt`.

- [ ] **Step 1: (Conditional, from Task 16) add `AnalyticsComputedAtUtc`** to `tbl_ChartResults` if a consumer needs it — a small migration `08a` or fold into `08`:
```sql
IF COL_LENGTH('dbo.tbl_ChartResults', 'AnalyticsComputedAtUtc') IS NULL
    ALTER TABLE dbo.tbl_ChartResults ADD AnalyticsComputedAtUtc DATETIME2(0) NULL;
```
and set it in `ChartGenerationService.RecomputeAnalytics` / `PersistAnalytics`. Skip if Task 16 found no consumer.

- [ ] **Step 2: Write `db/08_drop_child_parent_duplication.sql`**

```sql
-- =====================================================================
-- 08 — Drop parent-identity duplication from the child fact tables
-- (review item 3). Person + chart type now come only from tbl_ChartResults.
-- Requires: migration 07 (views off child BirthDetailId) NOT yet done for
-- vw_Chart_Consolidated / vw_Chart_DashaTimeline — migration 09 handles those,
-- so run 08 and 09 together.
-- =====================================================================
USE [ikiastrro];
GO
-- indexes that lead with a dropped column must go first
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_PlanetAvastha_BirthDetailId')
    DROP INDEX IX_Fact_PlanetAvastha_BirthDetailId ON dbo.tbl_Fact_PlanetAvastha;
GO
DECLARE @sql NVARCHAR(MAX) = N'';
-- drop default constraints on the columns we're about to remove (names vary), then the columns
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables t ON t.object_id = dc.parent_object_id
WHERE t.name IN ('tbl_Chart_KeyDetails','tbl_Chart_HouseLords','tbl_Chart_Conjunctions','tbl_Chart_Aspects','tbl_Chart_DashaPeriods','tbl_Fact_PlanetAvastha')
  AND c.name IN ('BirthDetailId','ChartType','ComputedAt');
EXEC sp_executesql @sql;
GO
-- drop FK on tbl_Fact_PlanetAvastha.BirthDetailId if present
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Fact_PlanetAvastha_BirthDetail')
    ALTER TABLE dbo.tbl_Fact_PlanetAvastha DROP CONSTRAINT FK_Fact_PlanetAvastha_BirthDetail;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_HouseLords','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_HouseLords DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_Conjunctions DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_Aspects','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_Aspects DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_DashaPeriods','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_DashaPeriods DROP COLUMN BirthDetailId, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Fact_PlanetAvastha','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Fact_PlanetAvastha DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '08_drop_child_parent_duplication.sql', 'dropped BirthDetailId/ChartType/ComputedAt from child fact tables'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '08_drop_child_parent_duplication.sql');
GO
PRINT '08 applied: child-table redundancy dropped.';
GO
```
(Note: `tbl_Chart_DashaPeriods` has no `ChartType` column — only `BirthDetailId` + `ComputedAt`.)

- [ ] **Step 3: Do NOT apply 08 yet** — apply it together with migration 09 (Task 19), because dropping `kd.BirthDetailId` breaks `vw_Chart_Consolidated` until 09 repoints it. Continue to the code changes.

- [ ] **Step 4: Remove the properties from the Core models**

From `ChartKeyDetail.cs`, `ChartHouseLord.cs`, `ChartConjunction.cs`, `ChartAspect.cs`: delete `public int BirthDetailId { get; set; }`, `public string ChartType { get; set; } = string.Empty;`, `public DateTime ComputedAt { get; set; } = DateTime.UtcNow;`.
From `DashaPeriodRecord.cs`: (no `BirthDetailId`/`ChartType`/`ComputedAt` — it never had them; skip.)
From `AvasthaModels.cs` `PlanetAvasthaFact`: delete `BirthDetailId`, `ChartType`, `ComputedAt`.

- [ ] **Step 5: Remove the columns from the repo INSERT lists**

In `ChartKeyDetailsRepository`, `ChartHouseLordsRepository`, `ChartConjunctionsRepository`, `ChartAspectsRepository`, `PlanetAvasthaRepository`: delete `BirthDetailId, ChartType, ComputedAt` (and `@BirthDetailId, @ChartType, @ComputedAt`) from the `INSERT` column/value lists. In `DashaPeriodsRepository.InsertTree`: delete `BirthDetailId` + `ComputedAt` from the `INSERT` and the anonymous param object (keep the `birthDetailId` method parameter — it is still used by nothing now; remove it too and update the one caller `ChartGenerationService`/`VimshottariDashaService` if the signature changes — simplest: keep the parameter, stop passing it into SQL).

- [ ] **Step 6: Simplify `ChartGenerationService.PersistAnalytics`**

Delete the fan-out lines that set `r.BirthDetailId` / `r.ChartType` on `keyDetails`, `houseLords`, `conjunctions`, `aspects`, `avasthas`. Keep the `r.ChartResultId = chartResultId` lines. The `birthDetailId` parameter of `PersistAnalytics` may become unused — remove it and update the three call sites in the same file.

- [ ] **Step 7: Build**

Run: `dotnet build Ikiastrro.slnx`
Expected: succeeds. Fix any remaining compile error from a consumer that read `.ChartType` / `.BirthDetailId` off a child model (Task 16 listed them) — repoint to the `ChartResult` / consolidated view.

- [ ] **Step 8: Commit (code only, migration not yet applied)**

```bash
git add src/
git commit -m "$(printf 'refactor: drop BirthDetailId/ChartType/ComputedAt from child fact models (review item 3)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

---

## Task 19: Finalize views, apply 08+09, full regression

**Files:**
- Create: `db/09_finalize_views_after_drop.sql`
- Modify: `db/ikiastrro.sql`

**Interfaces:**
- Consumes: migration 08 script (Task 18), the code changes (Task 18).
- Produces: `vw_Chart_Consolidated`, `vw_Chart_HouseNakshatraSpan`, `vw_Chart_DashaTimeline`, `tvf_Chart_SadeSatiPeriods` source person/chart-type from `tbl_ChartResults`, not from a child column.

- [ ] **Step 1: Write `db/09_finalize_views_after_drop.sql`** — drop-and-recreate each, with:

- `vw_Chart_Consolidated`: `JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId` (was `kd.BirthDetailId`); everything else already id-joined from migration 07.
- `vw_Chart_HouseNakshatraSpan`: add `JOIN dbo.tbl_ChartResults cr ON cr.Id = hl.ChartResultId`; replace `hl.BirthDetailId` / `hl.ChartType` in the `SELECT` with `cr.BirthDetailId` and `ct.Code` (add `JOIN dbo.tbl_Dim_ChartType ct ON ct.Id = cr.ChartTypeId`).
- `vw_Chart_DashaTimeline`: `JOIN dbo.tbl_ChartResults cr ON cr.Id = dp.ChartResultId JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId` (was `bd.Id = dp.BirthDetailId`).
- `tvf_Chart_SadeSatiPeriods`: in the `MoonSign` CTE, `JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId WHERE cr.BirthDetailId = @BirthDetailId AND cr.ChartTypeId = 1` (was `kd.BirthDetailId = @BirthDetailId AND kd.ChartType = 'D1'`); `sa` join already on `kd.SignId` from migration 07.

End with the `SchemaMigrations` insert + `PRINT '09 applied...'`.

- [ ] **Step 2: Apply 08 then 09**

Run: `sqlcmd -S localhost -E -d ikiastrro -i db/08_drop_child_parent_duplication.sql`
Then: `sqlcmd -S localhost -E -d ikiastrro -i db/09_finalize_views_after_drop.sql`
Expected: `08 applied...` then `09 applied...`. If a view fails to create in 09 because it still references a dropped column, fix that reference and re-run 09.

- [ ] **Step 3: Full verification sweep**

Run in order:
```
dotnet build Ikiastrro.slnx
dotnet run --project src/Ikiastrro.Cli -- verify-schema
dotnet run --project src/Ikiastrro.Cli -- verify-avastha
dotnet run --project src/Ikiastrro.Cli -- verify-vargas
dotnet run --project src/Ikiastrro.Cli -- verify-functional-nature
dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails
dotnet run --project src/Ikiastrro.Cli -- verify-schema
```
Expected: build green; every `verify-*` reports `ALL PASS`; recompute completes; second `verify-schema` still `ALL PASS`.

- [ ] **Step 4: Web smoke**

Launch the Web app; open `/charts/{id}` for a seeded person; confirm every section renders (D1/varga tables, consolidated columns, dasha timeline, life-weeks, Sade Sati, house→nakshatra span). Delete a throwaway person via the UI and confirm no error + no orphan rows (`SELECT COUNT(*) FROM dbo.tbl_Chart_KeyDetails kd WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_ChartResults cr WHERE cr.Id = kd.ChartResultId)` → 0).

- [ ] **Step 5: Fold Phase 3 into the baseline**

In `db/ikiastrro.sql`: remove `BirthDetailId` / `ChartType` / `ComputedAt` from the six `CREATE TABLE` statements; remove `IX_Fact_PlanetAvastha_BirthDetailId`; replace the four view/TVF definitions with the migration-09 versions. Rebuild `ikiastrro_scratch` from the baseline (same commands as Task 15 Step 3) and confirm it runs clean and `verify-schema` passes against it after a seed+compute. Drop `ikiastrro_scratch`.

- [ ] **Step 6: Commit**

```bash
git add db/08_drop_child_parent_duplication.sql db/09_finalize_views_after_drop.sql db/ikiastrro.sql
git commit -m "$(printf 'feat(db): drop child-table parent duplication; finalize views (review item 3)\n\nCo-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01MruYVykobiqZ8TKnqFDPZs')"
```

**PHASE 3 CHECKPOINT / DONE:** `dotnet build` green · all `verify-*` PASS · Web renders + delete works · baseline rebuilds clean · `SELECT * FROM dbo.SchemaMigrations` lists `01`–`09`. Review items 1–5 complete.

---

## Self-Review

**1. Spec coverage**

| Spec section | Task(s) |
|---|---|
| §4 ledger + convention | Task 1 |
| §5.1 `tbl_Dim_ChartType` + seed | Task 2 |
| §5.2 `tbl_Rule_Sets` version dimension | Task 3 |
| §5.3 NULLable `*Id` columns | Task 4 |
| §5.4 backfill `UPDATE`s | Task 5 |
| §5.5 transit `UNIQUE` | Task 4 (folded in) |
| §5.6 `verify-schema` mode | Task 6 (Phase 1 set), Task 13 (Phase 2 set) |
| §5.7 fold Phase 1 baseline | Task 7 |
| §6.1 Core models + `ChartAnalyzer` + `ChartTypeRepository` + rule-set plumbing | Tasks 8, 9, 10, 11, 12 |
| §6.2 tighten constraints | Task 13 |
| §6.3 repoint views/TVFs | Task 14 |
| §6.4 fold Phase 2 baseline | Task 15 |
| §7 `PlanetId`/`SignId` derivation | Task 8 (`AstroIds` + verify-schema offset check) |
| §8.1 drop child columns | Task 18 |
| §8.2 rewrite 6 `DeleteByBirthDetailId` | Task 17 |
| §8.3 rewrite `tvf_Chart_SadeSatiPeriods` | Task 19 |
| §8.4 repoint remaining views | Task 19 |
| §8.5 Core model cleanup + `PersistAnalytics` | Task 18 |
| §8.6 fold Phase 3 baseline | Task 19 |
| §9 verification model | every task's verify step; full sweeps at each checkpoint |
| §11 per-child `ComputedAt` question | Task 16 |

No gaps.

**2. Placeholder scan:** No "TBD"/"TODO"/"handle edge cases". Task 16 is a genuine investigation task whose output is consumed by 17–19 (not a placeholder). Task 13 Step 4 and Task 11 Step 7/9 name a fallback ("if a cast error appears…") with the exact remedy — acceptable, not vague.

**3. Type consistency:** `AstroIds.PlanetId`/`SignId`/`PlanetIdOrNull` used consistently in Tasks 8, 9, 11. `ChartResult.RuleSetId`/`ChartTypeId`/`CalculationKind` (Task 8) match the INSERT params (Task 11) and the service stamps (Task 12). `CalculationKind` values `'PositionChart'`/`'VimshottariDasha'` identical in migration 04/05/06, model default (Task 8), service (Task 12), TVF (Task 14). `ChartTypeRepository.CodeToId()` returns `IReadOnlyDictionary<string,int>` (Task 10) and is indexed by `result.ChartType` (Task 12). `verify-schema` uses `long` counts + `Count(string)` helper consistently across Tasks 6/8/13.

**4. Ambiguity:** Migration 08 note clarifies `tbl_Chart_DashaPeriods` has no `ChartType`. Task 18 Step 5 clarifies the `birthDetailId` param handling. Task 13 Step 4 commits to keeping `double` for lat/long with a stated fallback.
