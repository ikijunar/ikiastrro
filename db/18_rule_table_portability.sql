-- =====================================================================
-- 18 — Rule-table portability tail + tbl_Rule_Catalog + reserved rule
-- tables (engine reorg, Plan 1). DB-only, additive.
--
--   1. Adds a portability tail (MethodCode, RuleParametersJson,
--      CalculationNarrative, SourceRefCode, IsActive) + two CHECK
--      constraints (ISJSON on the JSON column, SRC_ prefix on the
--      SourceRefCode) to every existing tbl_Rule_* rule table.
--      tbl_Rule_VargaScheme already carries MethodCode — not re-added.
--      tbl_Rule_Sets is a meta/version table, not a rule table — skipped.
--   2. Backfills SourceRefCode for the rule tables whose classical
--      source is already known (codes must resolve in tbl_Dim_Source).
--      tbl_Rule_VargaScheme stays NULL (Task 12 backfills per scheme).
--   3. Creates tbl_Rule_Catalog — a one-page index of "what a port must
--      reimplement": one row per rule table (7 existing + 5 reserved).
--   4. Creates 5 empty reserved rule tables for later plans
--      (HouseSignification/Karaka P2, ShadbalaComponent/VimsopakaWeight
--      P3, Yoga P4), each with the same portability tail + CHECKs.
--
-- Additive only: no DROP, no data change beyond the SourceRefCode
-- backfill UPDATEs. Idempotent (guarded per batch + per object).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/18_rule_table_portability.sql
-- =====================================================================
USE [ikiastrro];
GO

-- --- Batch 1: portability-tail columns on the 7 existing rule tables ---
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql')
BEGIN
    -- tbl_Rule_VargaScheme (already has MethodCode — do NOT re-add)
    IF COL_LENGTH('dbo.tbl_Rule_VargaScheme','RuleParametersJson')   IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_VargaScheme','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_VargaScheme','SourceRefCode')        IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_VargaScheme','IsActive')            IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_VargaScheme_IsActive DEFAULT 1;

    -- tbl_Rule_AspectOffset
    IF COL_LENGTH('dbo.tbl_Rule_AspectOffset','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AspectOffset','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AspectOffset','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AspectOffset','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AspectOffset','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_AspectOffset_IsActive DEFAULT 1;

    -- tbl_Rule_CombustionOrb
    IF COL_LENGTH('dbo.tbl_Rule_CombustionOrb','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_CombustionOrb','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_CombustionOrb','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_CombustionOrb','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_CombustionOrb','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_CombustionOrb_IsActive DEFAULT 1;

    -- tbl_Rule_NaturalRelationship
    IF COL_LENGTH('dbo.tbl_Rule_NaturalRelationship','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_NaturalRelationship','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_NaturalRelationship','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_NaturalRelationship','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_NaturalRelationship','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_NaturalRelationship_IsActive DEFAULT 1;

    -- tbl_Rule_TemporaryFriendshipDistance
    IF COL_LENGTH('dbo.tbl_Rule_TemporaryFriendshipDistance','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_TemporaryFriendshipDistance','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_TemporaryFriendshipDistance','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_TemporaryFriendshipDistance','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_TemporaryFriendshipDistance','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_TemporaryFriendshipDistance_IsActive DEFAULT 1;

    -- tbl_Rule_AgeState
    IF COL_LENGTH('dbo.tbl_Rule_AgeState','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AgeState','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AgeState','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AgeState','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_AgeState','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_AgeState_IsActive DEFAULT 1;

    -- tbl_Rule_WakefulnessState
    IF COL_LENGTH('dbo.tbl_Rule_WakefulnessState','MethodCode')          IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD MethodCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_WakefulnessState','RuleParametersJson')  IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD RuleParametersJson NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_WakefulnessState','CalculationNarrative') IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD CalculationNarrative NVARCHAR(MAX) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_WakefulnessState','SourceRefCode')       IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD SourceRefCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_WakefulnessState','IsActive')           IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD IsActive BIT NOT NULL CONSTRAINT DF_Rule_WakefulnessState_IsActive DEFAULT 1;

    PRINT '18 batch 1: portability-tail columns added.';
END
GO

-- --- Batch 2: CHECK constraints (columns now committed) + SourceRefCode backfill ---
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql')
BEGIN
    IF OBJECT_ID('dbo.CK_RuleVarga_Json','C')     IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD CONSTRAINT CK_RuleVarga_Json     CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleVarga_Src','C')      IS NULL ALTER TABLE dbo.tbl_Rule_VargaScheme   ADD CONSTRAINT CK_RuleVarga_Src      CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleAspect_Json','C')    IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD CONSTRAINT CK_RuleAspect_Json    CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleAspect_Src','C')     IS NULL ALTER TABLE dbo.tbl_Rule_AspectOffset  ADD CONSTRAINT CK_RuleAspect_Src     CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleCombust_Json','C')   IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD CONSTRAINT CK_RuleCombust_Json   CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleCombust_Src','C')    IS NULL ALTER TABLE dbo.tbl_Rule_CombustionOrb ADD CONSTRAINT CK_RuleCombust_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleNatRel_Json','C')    IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD CONSTRAINT CK_RuleNatRel_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleNatRel_Src','C')     IS NULL ALTER TABLE dbo.tbl_Rule_NaturalRelationship ADD CONSTRAINT CK_RuleNatRel_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleTempFri_Json','C')   IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD CONSTRAINT CK_RuleTempFri_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleTempFri_Src','C')    IS NULL ALTER TABLE dbo.tbl_Rule_TemporaryFriendshipDistance ADD CONSTRAINT CK_RuleTempFri_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleAgeState_Json','C')  IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD CONSTRAINT CK_RuleAgeState_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleAgeState_Src','C')   IS NULL ALTER TABLE dbo.tbl_Rule_AgeState ADD CONSTRAINT CK_RuleAgeState_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    IF OBJECT_ID('dbo.CK_RuleWakeState_Json','C') IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD CONSTRAINT CK_RuleWakeState_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1);
    IF OBJECT_ID('dbo.CK_RuleWakeState_Src','C')  IS NULL ALTER TABLE dbo.tbl_Rule_WakefulnessState ADD CONSTRAINT CK_RuleWakeState_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%');

    -- SourceRefCode backfill — codes that already resolve in tbl_Dim_Source
    UPDATE dbo.tbl_Rule_AspectOffset                 SET SourceRefCode = 'SRC_BPHS_26'         WHERE SourceRefCode IS NULL;
    UPDATE dbo.tbl_Rule_CombustionOrb                SET SourceRefCode = 'SRC_BPHS_COMBUSTION' WHERE SourceRefCode IS NULL;
    UPDATE dbo.tbl_Rule_NaturalRelationship          SET SourceRefCode = 'SRC_BPHS'            WHERE SourceRefCode IS NULL;
    UPDATE dbo.tbl_Rule_TemporaryFriendshipDistance  SET SourceRefCode = 'SRC_BPHS'            WHERE SourceRefCode IS NULL;
    UPDATE dbo.tbl_Rule_AgeState                     SET SourceRefCode = 'SRC_BPHS_AVASTHA'    WHERE SourceRefCode IS NULL;
    UPDATE dbo.tbl_Rule_WakefulnessState            SET SourceRefCode = 'SRC_BPHS_AVASTHA'    WHERE SourceRefCode IS NULL;
    -- tbl_Rule_VargaScheme.SourceRefCode intentionally left NULL (Task 12).

    PRINT '18 batch 2: CHECK constraints added, SourceRefCode backfilled.';
END
GO

-- --- Batch 3: tbl_Rule_Catalog + 5 reserved rule tables ---
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql')
BEGIN
    IF OBJECT_ID('dbo.tbl_Rule_Catalog','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_Catalog (
        RuleTableName   VARCHAR(80)  CONSTRAINT PK_Rule_Catalog PRIMARY KEY,
        EngineCode      VARCHAR(30)  NOT NULL,
        MethodCodes     VARCHAR(300) NOT NULL,
        Purpose         VARCHAR(400) NOT NULL,
        IntroducedIn    VARCHAR(40)  NOT NULL
    );

    IF OBJECT_ID('dbo.tbl_Rule_HouseSignification','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_HouseSignification (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_HouseSignification PRIMARY KEY,
        RuleSetId INT NOT NULL,
        HouseNumber TINYINT NOT NULL,
        SignificationCode VARCHAR(40) NOT NULL,
        MethodCode VARCHAR(30) NULL,
        RuleParametersJson NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode VARCHAR(40) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rule_HouseSignification_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_HouseSignification_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_HouseSignification_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );

    IF OBJECT_ID('dbo.tbl_Rule_Karaka','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_Karaka (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_Karaka PRIMARY KEY,
        RuleSetId INT NOT NULL,
        KarakaScheme VARCHAR(20) NOT NULL,
        PlanetOrHouse VARCHAR(20) NULL,
        TargetValue VARCHAR(40) NULL,
        OrderIndex TINYINT NULL,
        ReverseForRahu BIT NULL,
        MethodCode VARCHAR(30) NULL,
        RuleParametersJson NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode VARCHAR(40) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rule_Karaka_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_Karaka_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_Karaka_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );

    IF OBJECT_ID('dbo.tbl_Rule_ShadbalaComponent','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_ShadbalaComponent (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_ShadbalaComponent PRIMARY KEY,
        RuleSetId INT NOT NULL,
        BalaCode VARCHAR(30) NOT NULL,
        SubComponentCode VARCHAR(40) NULL,
        WeightRupas DECIMAL(6,3) NULL,
        MaxRupas DECIMAL(6,3) NULL,
        LookupJson NVARCHAR(MAX) NULL,
        MethodCode VARCHAR(30) NULL,
        RuleParametersJson NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode VARCHAR(40) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rule_ShadbalaComponent_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_ShadbalaComponent_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_ShadbalaComponent_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );

    IF OBJECT_ID('dbo.tbl_Rule_VimsopakaWeight','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_VimsopakaWeight (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_VimsopakaWeight PRIMARY KEY,
        RuleSetId INT NOT NULL,
        SchemeCode VARCHAR(20) NOT NULL,
        VargaChartType VARCHAR(10) NOT NULL,
        Weight DECIMAL(5,2) NULL,
        MaxTotal DECIMAL(6,2) NULL,
        MethodCode VARCHAR(30) NULL,
        RuleParametersJson NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode VARCHAR(40) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rule_VimsopakaWeight_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_VimsopakaWeight_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_VimsopakaWeight_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );

    IF OBJECT_ID('dbo.tbl_Rule_Yoga','U') IS NULL
    CREATE TABLE dbo.tbl_Rule_Yoga (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_Yoga PRIMARY KEY,
        RuleSetId INT NOT NULL,
        YogaCode VARCHAR(40) NOT NULL,
        YogaCategory VARCHAR(30) NULL,
        RequirementJson NVARCHAR(MAX) NULL,
        CancellationJson NVARCHAR(MAX) NULL,
        ResultCode VARCHAR(40) NULL,
        MethodCode VARCHAR(30) NULL,
        RuleParametersJson NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode VARCHAR(40) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Rule_Yoga_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_Yoga_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_Yoga_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );

    PRINT '18 batch 3: tbl_Rule_Catalog + 5 reserved rule tables created.';
END
GO

-- --- Batch 4: seed tbl_Rule_Catalog (12 rows, idempotent) ---
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql')
BEGIN
    MERGE dbo.tbl_Rule_Catalog AS tgt
    USING (VALUES
        ('tbl_Rule_VargaScheme',                'VARGA',        'LINEAR_VARGA,GRID_VARGA,BAND_VARGA', 'Per-rule-set mapping of each divisional chart type to its varga-sign derivation method.', 'migration 11'),
        ('tbl_Rule_AspectOffset',               'RELATIONSHIP', 'OFFSET_LIST',   'Graha drishti: the house offsets each planet aspects, including special aspects for Mars/Jupiter/Saturn.', 'baseline'),
        ('tbl_Rule_CombustionOrb',              'RELATIONSHIP', 'ORB_PAIR',      'Combustion (astangata) orb in degrees per planet, for direct and retrograde motion.', 'baseline'),
        ('tbl_Rule_NaturalRelationship',        'DIGNITY',      'MAP_LOOKUP',    'Naisargika (permanent) friendship: friend/neutral/enemy for each ordered planet pair.', 'baseline'),
        ('tbl_Rule_TemporaryFriendshipDistance','DIGNITY',      'DISTANCE_SET',  'Tatkalika (temporary) friendship: which sign-distances from a planet count as friendly.', 'baseline'),
        ('tbl_Rule_AgeState',                   'AVASTHA',      'BAND_LOOKUP',   'Baaladi avastha (infant..dead) degree bands per odd/even sign, with the effect fraction.', 'migration 00 / renamed 16'),
        ('tbl_Rule_WakefulnessState',           'AVASTHA',      'MAP_LOOKUP',    'Jagradadi avastha (awake/dreaming/sleeping) keyed by the planet''s dignity status.', 'migration 00 / renamed 16'),
        ('tbl_Rule_HouseSignification',         'HOUSE',        'MAP_LOOKUP',    'Reserved: bhava karakatvas — the significations attached to each house.', 'migration 18 (empty; P2)'),
        ('tbl_Rule_Karaka',                     'KARAKA',       'MAP_LOOKUP',    'Reserved: chara / sthira / naisargika karaka assignment schemes.', 'migration 18 (empty; P2)'),
        ('tbl_Rule_ShadbalaComponent',          'STRENGTH',     'WEIGHT_TABLE',  'Reserved: shadbala sub-component weights and maxima, in rupas.', 'migration 18 (empty; P3)'),
        ('tbl_Rule_VimsopakaWeight',            'STRENGTH',     'WEIGHT_TABLE',  'Reserved: vimsopaka bala varga-group weights per scheme (shadvarga..shodasavarga).', 'migration 18 (empty; P3)'),
        ('tbl_Rule_Yoga',                       'YOGA',         'PREDICATE_SET', 'Reserved: yoga definitions — formation predicates, cancellation rules, and result codes.', 'migration 18 (empty; P4)')
    ) AS src (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    ON tgt.RuleTableName = src.RuleTableName
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
        VALUES (src.RuleTableName, src.EngineCode, src.MethodCodes, src.Purpose, src.IntroducedIn);

    PRINT '18 batch 4: tbl_Rule_Catalog seeded.';
END
GO

-- --- Batch 5: self-record ---
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql')
BEGIN
    INSERT dbo.SchemaMigrations (ScriptName, Note)
    SELECT '18_rule_table_portability.sql',
           'rule-table portability tail + tbl_Rule_Catalog + 5 reserved rule tables'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '18_rule_table_portability.sql');

    PRINT '18 applied: portability tail + tbl_Rule_Catalog + reserved rule tables.';
END
ELSE
    PRINT '18 already applied.';
GO

-- Self-heal: correct the VargaScheme catalog row on DBs where migration 18's guarded MERGE already ran.
UPDATE dbo.tbl_Rule_Catalog
   SET MethodCodes = 'LINEAR_VARGA,GRID_VARGA,BAND_VARGA'
 WHERE RuleTableName = 'tbl_Rule_VargaScheme'
   AND MethodCodes <> 'LINEAR_VARGA,GRID_VARGA,BAND_VARGA';
GO
