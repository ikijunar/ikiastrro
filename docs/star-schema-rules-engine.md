# Star Schema + Data-Driven Rules Engine — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure `vedic_horo_gen` around a star schema (dimensions / facts / rules) so the
classical rules currently hardcoded in C# (aspect offsets, combustion orbs, planetary
friendship) become data — versioned via a `RuleSetId`, so changing a rule never silently
alters already-computed chart facts, and never requires a code deployment.

**Architecture:** Three layers. **Dimensions** (`tbl_Dim_*`, mostly already exist: Planets,
SignAttributes, Nakshatras) describe fixed classical vocabulary. **Rules** (`tbl_Rule_*`, new)
hold the parameterized classical logic, each row scoped to a `RuleSetId` so multiple rule
schemes can coexist. **Facts** (`tbl_Fact_*`, renamed from today's `tbl_Chart_*`) store
per-chart computed results, each row recording which `RuleSetId` produced it. C# calculators
(`ClassicalRelationships`, `ClassicalCombustion`, `ClassicalDignity`) stop hardcoding
dictionaries and instead read from `tbl_Rule_*` via new Dapper repositories.

**Tech Stack:** SQL Server (existing `vedic_horo_gen` DB), Dapper (existing pattern in
`Ikiastrro.Data`), .NET 8 / C#, xUnit for the calculator refactor (new — no test project
exists yet in this solution; see Task 6).

**Spec:** This document is both spec and plan — there is no separate upstream spec file. The
one-page decision record behind it (context / decision / consequences) is
`..\decisions\001-star-schema-rules-engine.md`. Prior related design docs:
`D:\@ClaudeSpace\ikiastrro\docs\vedic-reference-tables.md` (the
Planets/SignAttributes/Nakshatras dimension tables this plan builds on).

## Global Constraints

- Table naming stays `tbl_PascalCase` per `STANDARDS.md` §D — this plan adds a `Dim_`/`Fact_`/
  `Rule_` infix (`tbl_Dim_Planets`, `tbl_Fact_ChartKeyDetails`, `tbl_Rule_CombustionOrb`),
  extending the precedent `tbl_Dim_LifeCalendar` already set on 2026-08-27. **Requires a
  STANDARDS.md §D update before Phase 2** (see Task 8) — flag to rammyps, don't self-approve.
- Migration files: `NNN_snake_case_description.sql`, idempotent (`IF NOT EXISTS` guards),
  one logical change per file — matches every migration so far (`019`-`023`).
- Every seeded rule value must be transcribed from the **existing C# source**
  (`ClassicalRelationships.cs`, `ClassicalCombustion.cs`), not re-derived from memory — same
  discipline that caught the 5-row exaltation/debilitation bug in migration `019`.
- Phase 1 (this plan's main body) is purely additive: zero changes to any existing table,
  column, or C# call site. The live Blazor app and CLI are unaffected until Phase 2 is
  explicitly approved.

---

## Why phased this way

The user's ask has two halves with very different risk profiles:

1. **"Rule changes shouldn't impact the overall [system]"** — solved by Phase 1 alone: new
   `tbl_Rule_*` tables + a `RuleSetId` versioning column, read by new Dapper repositories. Zero
   risk to the live app, because nothing existing is touched.
2. **"Split chart details into a star schema"** — renaming `tbl_Chart_KeyDetails` etc. into
   `tbl_Fact_*` shape is a breaking change: `ChartKeyDetailsRepository`, `D1ChartView.razor`,
   and every CLI backfill mode read those exact table/column names today. That's Phase 2,
   scoped below but **not started without explicit go-ahead**, per this session's own
   "confirm before hard-to-reverse/outward-facing changes" discipline — this one is both.

Phase 1 ships the part of the ask with the clearest, fastest payoff and no blast radius. Phase
2 is real work but should be its own confirmed decision, not a rider on this one.

---

## File Structure (Phase 1)

```
ikiastrro/
  db/
    024_create_rule_sets_table.sql              — tbl_Rule_Sets (the version dimension)
    025_create_aspect_offset_rules.sql           — tbl_Rule_AspectOffset
    026_create_combustion_orb_rules.sql          — tbl_Rule_CombustionOrb
    027_create_natural_relationship_rules.sql    — tbl_Rule_NaturalRelationship
    028_create_temporary_friendship_rules.sql    — tbl_Rule_TemporaryFriendshipDistance
  src/
    Ikiastrro.Data/
      RuleSetRepository.cs                       — new: CRUD + "active rule set" lookup
      AspectRuleRepository.cs                     — new
      CombustionRuleRepository.cs                  — new
      NaturalRelationshipRuleRepository.cs         — new
    Ikiastrro.Cli/
      Program.cs                                   — modify: add `list-rule-sets`, `show-rules <name>`
  D:\@ClaudeSpace\
    star-schema-rules-engine.md      — this file
```

---

## Star schema map — every table, old and new

| Layer | Table | Status |
|---|---|---|
| Dimension | `tbl_Planets` | exists (rename to `tbl_Dim_Planets` in Phase 2) |
| Dimension | `tbl_SignAttributes` | exists (→ `tbl_Dim_SignAttributes` in Phase 2) |
| Dimension | `tbl_Nakshatras` / `tbl_NakshatraPadas` / `tbl_NakshatraSubLords` | exist (→ `tbl_Dim_*` in Phase 2) |
| Dimension | `tbl_Dim_LifeCalendar` | exists, already correctly named |
| Dimension | `tbl_Dim_AvasthaState` | **2026-08-31** — avastha-state vocabulary (born correctly named per §D.1) |
| Dimension | `tbl_BirthDetails` | exists (conceptually the "Person" dimension; stays as-is — it's also the transactional entry point, not renamed) |
| **Rule (new, Phase 1)** | `tbl_Rule_Sets` | **this plan** |
| **Rule (new, Phase 1)** | `tbl_Rule_AspectOffset` | **this plan** |
| **Rule (new, Phase 1)** | `tbl_Rule_CombustionOrb` | **this plan** |
| **Rule (new, Phase 1)** | `tbl_Rule_NaturalRelationship` | **this plan** |
| **Rule (new, Phase 1)** | `tbl_Rule_TemporaryFriendshipDistance` | **this plan** |
| Rule | `tbl_Rule_BaaladiState` / `tbl_Rule_JagradadiState` | **2026-08-31** — avastha slice 1 (`RuleSetId` 1); born §D.1-compliant |
| Fact | `tbl_Fact_PlanetAvastha` | **2026-08-31** — avastha slice 1; born as a real `tbl_Fact_*`, carries `RuleSetId` |
| Fact | `tbl_ChartResults` | exists (→ `tbl_Fact_ChartResults` in Phase 2, gains `RuleSetId`) |
| Fact | `tbl_Chart_KeyDetails` | exists (→ `tbl_Fact_ChartKeyDetails` in Phase 2) |
| Fact | `tbl_Chart_HouseLords` | exists (→ `tbl_Fact_ChartHouseLords` in Phase 2) |
| Fact | `tbl_Chart_Conjunctions` | exists (→ `tbl_Fact_ChartConjunctions` in Phase 2) |
| Fact | `tbl_Chart_Aspects` | exists (→ `tbl_Fact_ChartAspects` in Phase 2) |
| Fact | `tbl_Chart_DashaPeriods` | exists (→ `tbl_Fact_ChartDashaPeriods` in Phase 2) |
| Fact | `tbl_PlanetSignTransitEvents` | exists (→ `tbl_Fact_PlanetSignTransitEvents` in Phase 2) |
| Fact (not yet built) | Conjunctions currently have **no rule table** — "same sign" is unconditional in code, no orb threshold. Noted as a future `tbl_Rule_ConjunctionOrb` if an orb-based definition is ever wanted; out of scope here since there's no existing behavior to preserve. |

**The versioning mechanism that actually delivers "rule changes don't impact overall":**
`tbl_Rule_Sets` holds one row per named rule scheme (seeded with exactly one row,
`'Parashari-Classical'`, id 1 — the current hardcoded behavior, transcribed verbatim). Every
other `tbl_Rule_*` table carries a `RuleSetId` FK. In Phase 2, `tbl_ChartResults` gains a
`RuleSetId` column recording which scheme computed that row. Adding a second scheme (e.g. a
different Rahu/Ketu aspect convention) is a **new set of rows**, never an edit to existing
ones — old computed charts stay interpretable exactly as they were, and switching a person's
active scheme is an explicit recompute (`recompute-keydetails`-style), never silent.

---

## Task 1: `tbl_Rule_Sets` — the version dimension every rule table hangs off

**Files:**
- Create: `ikiastrro/db/024_create_rule_sets_table.sql`

**Interfaces:**
- Produces: `tbl_Rule_Sets(Id TINYINT PK, RuleSetName VARCHAR(40) UNIQUE, Description
  VARCHAR(200), IsActive BIT)` — every later rule table's `RuleSetId` FK points here. Row 1 =
  `'Parashari-Classical'`, `IsActive = 1`.

- [ ] **Step 1: Write the migration**

```sql
-- 024_create_rule_sets_table.sql
-- The version dimension for every tbl_Rule_* table. One row per named classical scheme.
-- Seeded with exactly one row transcribing this project's CURRENT hardcoded behavior
-- (ClassicalRelationships.cs / ClassicalCombustion.cs) -- not a new interpretation, a mirror
-- of what's already live, so Phase 2 wiring is a no-op change in computed results.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_Sets')
BEGIN
    CREATE TABLE tbl_Rule_Sets
    (
        Id           TINYINT      NOT NULL PRIMARY KEY,
        RuleSetName  VARCHAR(40)  NOT NULL UNIQUE,
        Description  VARCHAR(200) NULL,
        IsActive     BIT          NOT NULL DEFAULT 0
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_Sets)
BEGIN
    INSERT INTO tbl_Rule_Sets (Id, RuleSetName, Description, IsActive)
    VALUES (1, 'Parashari-Classical',
            'This project''s existing hardcoded rules (ClassicalRelationships.cs / ClassicalCombustion.cs) as of 2026-08-30 -- Rahu/Ketu use Jupiter-style 5th/7th/9th aspects, BPHS/Phaladeepika combustion orbs.',
            1);
END
GO
```

- [ ] **Step 2: Apply and verify**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db/024_create_rule_sets_table.sql`
Then: `sqlcmd -S localhost -E -C -d vedic_horo_gen -Q "SELECT * FROM tbl_Rule_Sets"`
Expected: one row, `Id=1`, `RuleSetName='Parashari-Classical'`, `IsActive=1`.

- [ ] **Step 3: Commit**

```bash
git add db/024_create_rule_sets_table.sql
git commit -m "db: add tbl_Rule_Sets, the version dimension for the rules engine"
```

---

## Task 2: `tbl_Rule_AspectOffset` — transcribes `ClassicalRelationships.AspectOffsets`

**Files:**
- Create: `ikiastrro/db/025_create_aspect_offset_rules.sql`

**Interfaces:**
- Consumes: `tbl_Rule_Sets.Id = 1`, `tbl_Planets.Id` (1-9, Sun..Ketu).
- Produces: `tbl_Rule_AspectOffset(Id INT IDENTITY PK, RuleSetId TINYINT FK, PlanetId TINYINT
  FK, HouseOffset TINYINT, OffsetLabel VARCHAR(10))` — one row per (planet, offset) pair.

- [ ] **Step 1: Write the migration**

```sql
-- 025_create_aspect_offset_rules.sql
-- Transcribes ClassicalRelationships.AspectOffsets verbatim -- every classical graha aspects
-- its own 7th; Mars/Jupiter/Saturn add their specials; Rahu/Ketu use the Jupiter-style
-- 5th/7th/9th convention per rammyps's 2026-08-24 decision.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_AspectOffset')
BEGIN
    CREATE TABLE tbl_Rule_AspectOffset
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId    TINYINT      NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId     TINYINT      NOT NULL REFERENCES tbl_Planets(Id),
        HouseOffset  TINYINT      NOT NULL CHECK (HouseOffset BETWEEN 1 AND 12),
        OffsetLabel  VARCHAR(10)  NOT NULL,
        CONSTRAINT UQ_RuleAspectOffset UNIQUE (RuleSetId, PlanetId, HouseOffset)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_AspectOffset)
BEGIN
    -- PlanetId: 1 Sun,2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn,8 Rahu,9 Ketu
    INSERT INTO tbl_Rule_AspectOffset (RuleSetId, PlanetId, HouseOffset, OffsetLabel)
    VALUES
        (1, 1, 7, '7th'),                                   -- Sun
        (1, 2, 7, '7th'),                                   -- Moon
        (1, 3, 4, '4th'), (1, 3, 7, '7th'), (1, 3, 8, '8th'),   -- Mars
        (1, 4, 7, '7th'),                                   -- Mercury
        (1, 5, 5, '5th'), (1, 5, 7, '7th'), (1, 5, 9, '9th'),   -- Jupiter
        (1, 6, 7, '7th'),                                   -- Venus
        (1, 7, 3, '3rd'), (1, 7, 7, '7th'), (1, 7, 10, '10th'), -- Saturn
        (1, 8, 5, '5th'), (1, 8, 7, '7th'), (1, 8, 9, '9th'),   -- Rahu
        (1, 9, 5, '5th'), (1, 9, 7, '7th'), (1, 9, 9, '9th');   -- Ketu
END
GO
```

- [ ] **Step 2: Apply and verify row count**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db/025_create_aspect_offset_rules.sql`
Then: `sqlcmd -S localhost -E -C -d vedic_horo_gen -Q "SELECT COUNT(*) FROM tbl_Rule_AspectOffset"`
Expected: 19 rows (1+1+3+1+3+1+3+3+3).

- [ ] **Step 3: Commit**

```bash
git add db/025_create_aspect_offset_rules.sql
git commit -m "db: add tbl_Rule_AspectOffset, transcribed from ClassicalRelationships.AspectOffsets"
```

---

## Task 3: `tbl_Rule_CombustionOrb` — transcribes `ClassicalCombustion`'s two orb dictionaries

**Files:**
- Create: `ikiastrro/db/026_create_combustion_orb_rules.sql`

**Interfaces:**
- Produces: `tbl_Rule_CombustionOrb(Id INT IDENTITY PK, RuleSetId TINYINT FK, PlanetId TINYINT
  FK, DirectOrbDegrees DECIMAL(5,2), RetrogradeOrbDegrees DECIMAL(5,2) NULL)` — one row per
  applicable planet (6 rows: Moon, Mars, Mercury, Jupiter, Venus, Saturn — Sun/Rahu/Ketu never
  appear, matching `ClassicalCombustion.IsApplicable`).

- [ ] **Step 1: Write the migration**

```sql
-- 026_create_combustion_orb_rules.sql
-- Transcribes ClassicalCombustion.DirectOrbDegrees + RetrogradeOrbDegrees verbatim.
-- RetrogradeOrbDegrees is NULL for planets with no documented retrograde-specific orb
-- (Moon, Jupiter, Saturn) -- those fall back to DirectOrbDegrees regardless of motion, exactly
-- as the current C# does (RetrogradeOrbDegrees.TryGetValue returning false).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_CombustionOrb')
BEGIN
    CREATE TABLE tbl_Rule_CombustionOrb
    (
        Id                    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId             TINYINT      NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId              TINYINT      NOT NULL REFERENCES tbl_Planets(Id),
        DirectOrbDegrees      DECIMAL(5,2) NOT NULL,
        RetrogradeOrbDegrees  DECIMAL(5,2) NULL,
        CONSTRAINT UQ_RuleCombustionOrb UNIQUE (RuleSetId, PlanetId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_CombustionOrb)
BEGIN
    -- PlanetId: 2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn
    INSERT INTO tbl_Rule_CombustionOrb (RuleSetId, PlanetId, DirectOrbDegrees, RetrogradeOrbDegrees)
    VALUES
        (1, 2, 12.00, NULL),   -- Moon
        (1, 3, 17.00, 8.00),   -- Mars
        (1, 4, 14.00, 12.00),  -- Mercury
        (1, 5, 11.00, NULL),   -- Jupiter
        (1, 6, 10.00, 8.00),   -- Venus
        (1, 7, 15.00, NULL);   -- Saturn
END
GO
```

- [ ] **Step 2: Apply and verify against the C# source side by side**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db/026_create_combustion_orb_rules.sql`
Then diff by eye against `ClassicalCombustion.cs`'s two dictionaries (lines 30-46) — every
value must match exactly; this is the same cross-check discipline as migration `019`.

- [ ] **Step 3: Commit**

```bash
git add db/026_create_combustion_orb_rules.sql
git commit -m "db: add tbl_Rule_CombustionOrb, transcribed from ClassicalCombustion orb dictionaries"
```

---

## Task 4: `tbl_Rule_NaturalRelationship` — transcribes `ClassicalDignity.NaturalRelationship`

**Files:**
- Create: `ikiastrro/db/027_create_natural_relationship_rules.sql`

**Interfaces:**
- Produces: `tbl_Rule_NaturalRelationship(Id INT IDENTITY PK, RuleSetId TINYINT FK, PlanetId
  TINYINT FK, RelatedPlanetId TINYINT FK, RelationshipType VARCHAR(10) CHECK IN
  ('Friend','Neutral','Enemy'))` — 7 classical grahas × 6 other classical grahas = 42 directed
  rows (Naisargika Maitri is **not symmetric** — transcribe exactly as coded, never assume
  Sun→Moon implies Moon→Sun without checking).

- [ ] **Step 1: Write the migration**

```sql
-- 027_create_natural_relationship_rules.sql
-- Transcribes ClassicalDignity.NaturalRelationship verbatim -- 7x6=42 directed pairs (the 7
-- classical grahas only; Rahu/Ketu are explicitly excluded from this table in the C# source,
-- "not part of the Naisargika Maitri table in standard Parashari texts"). Deliberately NOT
-- assumed symmetric -- each pair transcribed from its own source line, not inferred from its
-- reverse.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_NaturalRelationship')
BEGIN
    CREATE TABLE tbl_Rule_NaturalRelationship
    (
        Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId         TINYINT     NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId          TINYINT     NOT NULL REFERENCES tbl_Planets(Id),
        RelatedPlanetId   TINYINT     NOT NULL REFERENCES tbl_Planets(Id),
        RelationshipType  VARCHAR(10) NOT NULL CHECK (RelationshipType IN ('Friend','Neutral','Enemy')),
        CONSTRAINT UQ_RuleNaturalRelationship UNIQUE (RuleSetId, PlanetId, RelatedPlanetId),
        CONSTRAINT CK_RuleNaturalRelationship_NotSelf CHECK (PlanetId <> RelatedPlanetId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_NaturalRelationship)
BEGIN
    -- PlanetId: 1 Sun,2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn
    INSERT INTO tbl_Rule_NaturalRelationship (RuleSetId, PlanetId, RelatedPlanetId, RelationshipType)
    VALUES
    -- Sun: Friends Moon,Mars,Jupiter; Neutral Mercury; Enemies Venus,Saturn
    (1,1,2,'Friend'), (1,1,3,'Friend'), (1,1,5,'Friend'), (1,1,4,'Neutral'), (1,1,6,'Enemy'), (1,1,7,'Enemy'),
    -- Moon: Friends Sun,Mercury; Neutral Mars,Jupiter,Venus,Saturn; no enemies
    (1,2,1,'Friend'), (1,2,4,'Friend'), (1,2,3,'Neutral'), (1,2,5,'Neutral'), (1,2,6,'Neutral'), (1,2,7,'Neutral'),
    -- Mars: Friends Sun,Moon,Jupiter; Neutral Venus,Saturn; Enemy Mercury
    (1,3,1,'Friend'), (1,3,2,'Friend'), (1,3,5,'Friend'), (1,3,6,'Neutral'), (1,3,7,'Neutral'), (1,3,4,'Enemy'),
    -- Mercury: Friends Sun,Venus; Neutral Mars,Jupiter,Saturn; Enemy Moon
    (1,4,1,'Friend'), (1,4,6,'Friend'), (1,4,3,'Neutral'), (1,4,5,'Neutral'), (1,4,7,'Neutral'), (1,4,2,'Enemy'),
    -- Jupiter: Friends Sun,Moon,Mars; Neutral Saturn; Enemies Mercury,Venus
    (1,5,1,'Friend'), (1,5,2,'Friend'), (1,5,3,'Friend'), (1,5,7,'Neutral'), (1,5,4,'Enemy'), (1,5,6,'Enemy'),
    -- Venus: Friends Mercury,Saturn; Neutral Mars,Jupiter; Enemies Sun,Moon
    (1,6,4,'Friend'), (1,6,7,'Friend'), (1,6,3,'Neutral'), (1,6,5,'Neutral'), (1,6,1,'Enemy'), (1,6,2,'Enemy'),
    -- Saturn: Friends Mercury,Venus; Neutral Jupiter; Enemies Sun,Moon,Mars
    (1,7,4,'Friend'), (1,7,6,'Friend'), (1,7,5,'Neutral'), (1,7,1,'Enemy'), (1,7,2,'Enemy'), (1,7,3,'Enemy');
END
GO
```

- [ ] **Step 2: Apply and verify row count + asymmetry spot-check**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db/027_create_natural_relationship_rules.sql`
Then: `sqlcmd -S localhost -E -C -d vedic_horo_gen -Q "SELECT COUNT(*) FROM tbl_Rule_NaturalRelationship"`
Expected: 42 rows. Then spot-check one genuinely asymmetric pair against the C# source directly
(e.g. Sun→Mercury is `'Neutral'` per `Sun`'s tuple, Mercury→Sun is `'Friend'` per `Mercury`'s
tuple — confirms the table isn't accidentally storing a symmetrized version).

- [ ] **Step 3: Commit**

```bash
git add db/027_create_natural_relationship_rules.sql
git commit -m "db: add tbl_Rule_NaturalRelationship, transcribed from ClassicalDignity.NaturalRelationship"
```

---

## Task 5: `tbl_Rule_TemporaryFriendshipDistance` — transcribes the Tatkalika Maitri distance rule

**Files:**
- Create: `ikiastrro/db/028_create_temporary_friendship_rules.sql`

**Interfaces:**
- Produces: `tbl_Rule_TemporaryFriendshipDistance(RuleSetId TINYINT FK, SignDistance TINYINT
  CHECK 1-12, IsFriend BIT, PRIMARY KEY (RuleSetId, SignDistance))` — 12 rows, transcribing
  `IsTemporaryFriend`'s `distance is 2 or 3 or 4 or 10 or 11 or 12` check.

- [ ] **Step 1: Write the migration**

```sql
-- 028_create_temporary_friendship_rules.sql
-- Transcribes ClassicalDignity.IsTemporaryFriend verbatim: friend if sign-distance (1=same
-- sign) is 2,3,4,10,11,12; enemy otherwise (1,5,6,7,8,9 -- note 7 counts as enemy in this
-- classical rule, same as 1/5/6/8/9, despite 7 being the universal aspect house elsewhere).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_TemporaryFriendshipDistance')
BEGIN
    CREATE TABLE tbl_Rule_TemporaryFriendshipDistance
    (
        RuleSetId    TINYINT NOT NULL REFERENCES tbl_Rule_Sets(Id),
        SignDistance TINYINT NOT NULL CHECK (SignDistance BETWEEN 1 AND 12),
        IsFriend     BIT     NOT NULL,
        PRIMARY KEY (RuleSetId, SignDistance)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_TemporaryFriendshipDistance)
BEGIN
    INSERT INTO tbl_Rule_TemporaryFriendshipDistance (RuleSetId, SignDistance, IsFriend)
    VALUES (1,1,0),(1,2,1),(1,3,1),(1,4,1),(1,5,0),(1,6,0),(1,7,0),(1,8,0),(1,9,0),(1,10,1),(1,11,1),(1,12,1);
END
GO
```

- [ ] **Step 2: Apply and verify**

Run: `sqlcmd -S localhost -E -C -d vedic_horo_gen -i db/028_create_temporary_friendship_rules.sql`
Then: `sqlcmd -S localhost -E -C -d vedic_horo_gen -Q "SELECT SignDistance, IsFriend FROM tbl_Rule_TemporaryFriendshipDistance ORDER BY SignDistance"`
Expected: `IsFriend=1` for exactly {2,3,4,10,11,12}, `0` for {1,5,6,7,8,9}.

- [ ] **Step 3: Commit**

```bash
git add db/028_create_temporary_friendship_rules.sql
git commit -m "db: add tbl_Rule_TemporaryFriendshipDistance, transcribed from ClassicalDignity.IsTemporaryFriend"
```

---

## Task 6: Dapper repositories — the CLI-facing read layer

**Files:**
- Create: `src/Ikiastrro.Data/RuleSetRepository.cs`
- Create: `src/Ikiastrro.Data/AspectRuleRepository.cs`
- Create: `src/Ikiastrro.Data/CombustionRuleRepository.cs`
- Create: `src/Ikiastrro.Data/NaturalRelationshipRuleRepository.cs`

**Interfaces:**
- Consumes: `SqlConnectionFactory` (existing constructor-injection pattern, e.g.
  `DashaPeriodsRepository`).
- Produces: `RuleSetRepository.GetActive() : RuleSet` (record `RuleSet(int Id, string
  RuleSetName, string? Description)`), `AspectRuleRepository.GetOffsets(int ruleSetId) :
  IReadOnlyDictionary<PlanetName, int[]>` (same shape `ClassicalRelationships.AspectOffsets`
  already uses — Phase 2's wiring task swaps the hardcoded dictionary for this call with a
  one-line change), `CombustionRuleRepository.GetOrbs(int ruleSetId) :
  IReadOnlyDictionary<PlanetName, (decimal Direct, decimal? Retrograde)>`,
  `NaturalRelationshipRuleRepository.GetRelationships(int ruleSetId) :
  IReadOnlyDictionary<PlanetName, (PlanetName[] Friends, PlanetName[] Neutrals, PlanetName[]
  Enemies)>` (same shape as `ClassicalDignity.NaturalRelationship`).

- [ ] **Step 1: Write `RuleSetRepository.cs`**

```csharp
using Dapper;

namespace Ikiastrro.Data;

public record RuleSet(int Id, string RuleSetName, string? Description);

/// <summary>tbl_Rule_Sets -- the version dimension every other tbl_Rule_* table hangs off.</summary>
public class RuleSetRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RuleSetRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>The one rule set flagged IsActive=1 -- what calculators use unless told otherwise.</summary>
    public RuleSet GetActive()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.QuerySingle<RuleSet>(
            "SELECT Id, RuleSetName, Description FROM dbo.tbl_Rule_Sets WHERE IsActive = 1");
    }

    public IReadOnlyList<RuleSet> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<RuleSet>("SELECT Id, RuleSetName, Description FROM dbo.tbl_Rule_Sets ORDER BY Id").ToList();
    }
}
```

- [ ] **Step 2: Write `AspectRuleRepository.cs`**

```csharp
using Dapper;
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Data;

/// <summary>tbl_Rule_AspectOffset -- returns the same shape ClassicalRelationships.AspectOffsets
/// hardcodes today, so Phase 2's wiring swap is a one-line change at the call site.</summary>
public class AspectRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AspectRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, int[]> GetOffsets(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, r.HouseOffset
            FROM dbo.tbl_Rule_AspectOffset r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            WHERE r.RuleSetId = @RuleSetId
            ORDER BY p.PlanetName, r.HouseOffset
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, int HouseOffset)>(sql, new { RuleSetId = ruleSetId });
        return rows
            .GroupBy(r => Enum.Parse<PlanetName>(r.PlanetName))
            .ToDictionary(g => g.Key, g => g.Select(r => r.HouseOffset).ToArray());
    }
}
```

- [ ] **Step 3: Write `CombustionRuleRepository.cs`**

```csharp
using Dapper;
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Data;

/// <summary>tbl_Rule_CombustionOrb -- same two-dictionary shape ClassicalCombustion hardcodes today.</summary>
public class CombustionRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CombustionRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, (decimal Direct, decimal? Retrograde)> GetOrbs(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, r.DirectOrbDegrees, r.RetrogradeOrbDegrees
            FROM dbo.tbl_Rule_CombustionOrb r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            WHERE r.RuleSetId = @RuleSetId
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, decimal DirectOrbDegrees, decimal? RetrogradeOrbDegrees)>(sql, new { RuleSetId = ruleSetId });
        return rows.ToDictionary(
            r => Enum.Parse<PlanetName>(r.PlanetName),
            r => (r.DirectOrbDegrees, r.RetrogradeOrbDegrees));
    }
}
```

- [ ] **Step 4: Write `NaturalRelationshipRuleRepository.cs`**

```csharp
using Dapper;
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Data;

/// <summary>tbl_Rule_NaturalRelationship -- same (Friends,Neutrals,Enemies) tuple shape
/// ClassicalDignity.NaturalRelationship hardcodes today.</summary>
public class NaturalRelationshipRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public NaturalRelationshipRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, (PlanetName[] Friends, PlanetName[] Neutrals, PlanetName[] Enemies)> GetRelationships(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, rp.PlanetName AS RelatedPlanetName, r.RelationshipType
            FROM dbo.tbl_Rule_NaturalRelationship r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            JOIN dbo.tbl_Planets rp ON rp.Id = r.RelatedPlanetId
            WHERE r.RuleSetId = @RuleSetId
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, string RelatedPlanetName, string RelationshipType)>(sql, new { RuleSetId = ruleSetId });

        return rows
            .GroupBy(r => Enum.Parse<PlanetName>(r.PlanetName))
            .ToDictionary(g => g.Key, g => (
                Friends: g.Where(r => r.RelationshipType == "Friend").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray(),
                Neutrals: g.Where(r => r.RelationshipType == "Neutral").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray(),
                Enemies: g.Where(r => r.RelationshipType == "Enemy").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray()
            ));
    }
}
```

- [ ] **Step 5: Build and smoke-test from the CLI project**

Run: `dotnet build src/Ikiastrro.Data/Ikiastrro.Data.csproj`
Expected: 0 errors. (No test project exists yet in this solution — Task 6's verification is a
build + the manual CLI smoke test in Task 7, not automated xUnit; adding a test project is a
reasonable follow-up but out of scope for this plan, which only wires up reads.)

- [ ] **Step 6: Commit**

```bash
git add src/Ikiastrro.Data/RuleSetRepository.cs src/Ikiastrro.Data/AspectRuleRepository.cs src/Ikiastrro.Data/CombustionRuleRepository.cs src/Ikiastrro.Data/NaturalRelationshipRuleRepository.cs
git commit -m "data: add Dapper repositories reading the new tbl_Rule_* tables"
```

---

## Task 7: CLI inspection commands — `list-rule-sets`, `show-rules <name>`

**Files:**
- Modify: `src/Ikiastrro.Cli/Program.cs` (add two new `args[0]` branches, same pattern as
  `backfill-analytics`/`precheck-planet-transits`)

**Interfaces:**
- Consumes: `RuleSetRepository`, `AspectRuleRepository`, `CombustionRuleRepository`,
  `NaturalRelationshipRuleRepository` (Task 6).

- [ ] **Step 1: Add the two CLI modes**

```csharp
// --- One-off mode: `dotnet run -- list-rule-sets` ---
// Lists every tbl_Rule_Sets row -- which named classical schemes exist, which is active.
if (args.Length > 0 && args[0] == "list-rule-sets")
{
    var ruleSets = new RuleSetRepository(connectionFactory).GetAll();
    var active = new RuleSetRepository(connectionFactory).GetActive();
    foreach (var rs in ruleSets)
    {
        var marker = rs.Id == active.Id ? " [ACTIVE]" : "";
        Console.WriteLine($"{rs.Id}: {rs.RuleSetName}{marker}");
        if (rs.Description is not null) Console.WriteLine($"   {rs.Description}");
    }
    return;
}

// --- One-off mode: `dotnet run -- show-rules <rule-set-id>` ---
// Prints every aspect offset / combustion orb / natural relationship for one rule set -- lets
// you eyeball the full ruleset without opening SSMS, and cross-check a new ruleset's rows
// against the classical text they're meant to encode before ever wiring it into ChartAnalyzer.
if (args.Length > 1 && args[0] == "show-rules" && int.TryParse(args[1], out var ruleSetId))
{
    Console.WriteLine($"--- Aspect offsets (RuleSetId={ruleSetId}) ---");
    foreach (var (planet, offsets) in new AspectRuleRepository(connectionFactory).GetOffsets(ruleSetId))
        Console.WriteLine($"  {planet,-8} {string.Join(", ", offsets.Select(o => $"{o}th"))}");

    Console.WriteLine($"\n--- Combustion orbs (RuleSetId={ruleSetId}) ---");
    foreach (var (planet, orbs) in new CombustionRuleRepository(connectionFactory).GetOrbs(ruleSetId))
        Console.WriteLine($"  {planet,-8} direct={orbs.Direct}°  retrograde={(orbs.Retrograde is { } r ? $"{r}°" : "(same as direct)")}");

    Console.WriteLine($"\n--- Natural relationships (RuleSetId={ruleSetId}) ---");
    foreach (var (planet, rel) in new NaturalRelationshipRuleRepository(connectionFactory).GetRelationships(ruleSetId))
        Console.WriteLine($"  {planet,-8} Friends=[{string.Join(",", rel.Friends)}] Neutrals=[{string.Join(",", rel.Neutrals)}] Enemies=[{string.Join(",", rel.Enemies)}]");
    return;
}
```

- [ ] **Step 2: Build and run manually**

Run: `dotnet build src/Ikiastrro.Cli/Ikiastrro.Cli.csproj`
Run: `dotnet run --project src/Ikiastrro.Cli --no-build -- list-rule-sets`
Expected output: `1: Parashari-Classical [ACTIVE]` plus the description line.
Run: `dotnet run --project src/Ikiastrro.Cli --no-build -- show-rules 1`
Expected: three sections, values matching `ClassicalRelationships.AspectOffsets` /
`ClassicalCombustion`'s two dictionaries / `ClassicalDignity.NaturalRelationship` exactly —
this run **is** the end-to-end verification that migrations 025-027 transcribed correctly.

- [ ] **Step 3: Commit**

```bash
git add src/Ikiastrro.Cli/Program.cs
git commit -m "cli: add list-rule-sets/show-rules for inspecting the new rules tables"
```

---

## Task 8: Document Phase 2 scope (no code — a decision record)

**Files:**
- Modify: `D:\@ClaudeSpace\ikiastrro\docs\star-schema-rules-engine.md` (this file — append the
  section below)
- Modify: `D:\@ClaudeSpace\STANDARDS.md` §D (only if/when Phase 2 is approved — flag the need,
  don't edit yet)

- [ ] **Step 1: Append a "Phase 2 (not started)" section** covering: renaming `tbl_Chart_*` →
  `tbl_Fact_*` and `tbl_Planets`/`tbl_SignAttributes`/`tbl_Nakshatras*` → `tbl_Dim_*`; adding
  `RuleSetId` to `tbl_ChartResults`; converting `ClassicalRelationships`/`ClassicalCombustion`/
  `ClassicalDignity` from static hardcoded classes to reading via the Task 6 repositories
  (requires either constructor injection or a static `Configure(...)` called once at startup —
  decide which when scoping Phase 2); updating every call site in `ChartAnalyzer.cs`,
  `D1ChartComputer.cs`, `D9ChartComputer.cs`, `ChartKeyDetailsRepository.cs`,
  `D1ChartView.razor`/`ChartView.razor.css` (CSS-isolated components binding to current table/
  column names), and every CLI backfill mode that queries `tbl_Chart_*` directly. Flag as
  breaking and requiring its own explicit go-ahead, same as this task's Global Constraints say.
- [ ] **Step 2: Commit**

```bash
git add "D:\@ClaudeSpace\ikiastrro\docs\star-schema-rules-engine.md"
git commit -m "docs: record Phase 2 (fact/dim table rename) scope, not started"
```

---

## Self-Review

**1. Spec coverage:** "star schema" → dimensions already exist, rules layer is this plan's Task
1-5, facts mapped in the table above (Phase 2). "rule changes don't impact overall" → `RuleSetId`
versioning, Task 1. "represent to CLI through proper dapper" → Task 6-7. All three covered.

**2. Placeholder scan:** every migration has full column lists and real transcribed values (not
"TBD"); every repository method has a real SQL string; every CLI step has an expected output to
check against. No placeholders found.

**3. Type consistency:** `AspectRuleRepository.GetOffsets` returns
`IReadOnlyDictionary<PlanetName, int[]>` — matches `ClassicalRelationships.AspectOffsets`'s
existing type exactly (checked against the read source in `ClassicalRelationships.cs:23`) so
Phase 2's eventual swap is mechanical. Same check performed for the other two repositories
against `ClassicalCombustion.cs` and `ClassicalDignity.cs`'s actual field types.

---

## Phase 1 — Implementation record (2026-08-30, executed same day as this plan)

All 8 tasks completed and verified, inline execution:

- **Migrations 024-028 applied** — `tbl_Rule_Sets` (1 row), `tbl_Rule_AspectOffset` (19 rows),
  `tbl_Rule_CombustionOrb` (6 rows), `tbl_Rule_NaturalRelationship` (42 rows),
  `tbl_Rule_TemporaryFriendshipDistance` (12 rows) — every row count matched the plan's
  prediction exactly on first application.
- **Asymmetry spot-check passed**: Sun→Mercury = `Neutral`, Mercury→Sun = `Friend` — confirms
  the table wasn't accidentally seeded as a symmetrized version of the real (asymmetric)
  Naisargika Maitri rule.
- **A real bug caught by the Task 7 smoke test**: `RuleSetRepository` initially failed —
  Dapper's record-constructor materialization requires an exact type match, and
  `tbl_Rule_Sets.Id` (`TINYINT`/byte) didn't match the record's `int Id`. The other three
  repositories (tuple-based, not record-based) didn't hit this — Dapper's tuple deserialization
  tolerates the byte→int widening that record-constructor matching doesn't. Fixed by casting
  `Id` to `INT` in the SQL text rather than weakening the public API to `byte`.
- **`show-rules 1` output diffed by hand against the live C# source** (not just eyeballed at
  write time) — all 7 planets' Friends/Neutrals/Enemies lists, all 9 planets' aspect offsets,
  and all 6 combustion orb pairs matched `ClassicalDignity.NaturalRelationship`,
  `ClassicalRelationships.AspectOffsets`, and `ClassicalCombustion`'s two dictionaries
  character-for-character.
- **`STANDARDS.md` §D.1 added** — every table from 2026-08-30 onward must declare
  `tbl_Dim_*`/`tbl_Rule_*`/`tbl_Fact_*`; every `tbl_Rule_*` table must carry the `RuleSetId` FK
  this plan introduced; pre-2026-08-30 tables are grandfathered (no forced rename).

**Not started (unchanged from the plan):** Phase 2 — renaming existing `tbl_Chart_*`/
`tbl_Planets`/etc. to `tbl_Fact_*`/`tbl_Dim_*`, adding `RuleSetId` to `tbl_ChartResults`, and
actually wiring `ClassicalRelationships`/`ClassicalCombustion`/`ClassicalDignity` to read from
these repositories instead of their hardcoded dictionaries. The rule tables exist and are
verified correct, but the live calculation engine does not read them yet — today's computed
charts still come from the hardcoded C# (which the rule tables now mirror exactly, so there's
no behavioral drift, just no behavioral change either).
