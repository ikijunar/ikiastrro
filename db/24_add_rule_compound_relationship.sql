-- =====================================================================
-- 24 - tbl_Rule_CompoundRelationship: the Panchadha Maitri 2x3 matrix as
-- scored master data.
--
-- Natural relationship (tbl_Rule_NaturalRelationship) x Temporary friendship
-- (tbl_Rule_TemporaryFriendshipDistance) -> Compound (Panchadha) relationship.
-- PVR presents it as a 2x3 grid:
--
--                          TEMPORARY
--                       Friend        Enemy
--   Natural Friend  |  Adhimitra   |   Sama     |
--   Natural Neutral |  Mitra       |   Shatru   |
--   Natural Enemy   |  Sama        |   Adhishatru|
--
-- Each cell carries a RelationshipScore (-2..+2). This is a SEPARATE axis
-- from DignityScore (tbl_Rule_GrahaDignity, -2..+4) - the two are combined
-- only at the interpretation layer with configurable weights, never merged
-- into one stored number.
--
-- DignityEngine.CombineToPanchadha will read this table instead of hard-
-- coding the grid (Phase 2). EnglishName is EXACTLY the DignityStatus label
-- the engine emits, so a consumer can JOIN on kd.DignityStatus.
--
-- RuleSetId 1 (Parashari-Classical): the combination rule is identical
-- across BPHS and PVR. SourceRefCode = SRC_PVR_INTEGRATED records the
-- citation this seed was written from.
--
-- Idempotent: table add is guarded; the 6-row seed is IF NOT EXISTS on the
-- table. tbl_Rule_NaturalRelationship / tbl_Rule_TemporaryFriendshipDistance
-- are NOT modified (their data already matches PVR; the Moon row stays
-- neutral/no-enemies per rammyps).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/24_add_rule_compound_relationship.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: the table ---
IF OBJECT_ID('dbo.tbl_Rule_CompoundRelationship', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_CompoundRelationship (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_CompoundRelationship PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_CompoundRelationship_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        NaturalRelation      VARCHAR(10)  NOT NULL,   -- Friend / Neutral / Enemy (the Naisargika tier)
        IsTemporaryFriend    BIT          NOT NULL,   -- the Tatkalika result for this pairing in the chart
        CompoundCode         VARCHAR(20)  NOT NULL,   -- ADHIMITRA / MITRA / SAMA / SHATRU / ADHISHATRU
        SanskritName         NVARCHAR(40) NOT NULL,
        EnglishName          NVARCHAR(40) NOT NULL,   -- EXACTLY the DignityStatus label: Great Friend / Friend / Neutral / Enemy / Great Enemy
        RelationshipScore    SMALLINT     NOT NULL,   -- ADHIMITRA +2 . MITRA +1 . SAMA 0 . SHATRU -1 . ADHISHATRU -2
        MethodCode           VARCHAR(30)  NULL,       -- 'MATRIX_LOOKUP'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL
                                 CONSTRAINT DF_Rule_CompoundRelationship_IsActive DEFAULT 1,
        CONSTRAINT CK_RuleCompoundRel_Nat   CHECK (NaturalRelation IN ('Friend','Neutral','Enemy')),
        CONSTRAINT CK_RuleCompoundRel_Code  CHECK (CompoundCode IN ('ADHIMITRA','MITRA','SAMA','SHATRU','ADHISHATRU')),
        CONSTRAINT CK_RuleCompoundRel_Score CHECK (RelationshipScore BETWEEN -2 AND 2),
        CONSTRAINT CK_RuleCompoundRel_Json  CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_RuleCompoundRel_Src   CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_CompoundRelationship UNIQUE (RuleSetId, NaturalRelation, IsTemporaryFriend)
    );
END
GO

-- --- Batch 2: seed the 6 cells of the 2x3 matrix ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_CompoundRelationship)
    INSERT dbo.tbl_Rule_CompoundRelationship
        (RuleSetId, NaturalRelation, IsTemporaryFriend, CompoundCode, SanskritName, EnglishName,
         RelationshipScore, MethodCode, SourceRefCode)
    SELECT 1, v.NaturalRelation, v.IsTemporaryFriend, v.CompoundCode, v.SanskritName, v.EnglishName,
           v.RelationshipScore, 'MATRIX_LOOKUP', 'SRC_PVR_INTEGRATED'
    FROM (VALUES
        ('Friend',  CONVERT(BIT,1), 'ADHIMITRA',  N'Adhimitra',  N'Great Friend', CONVERT(SMALLINT, 2)),
        ('Friend',  CONVERT(BIT,0), 'SAMA',       N'Sama',       N'Neutral',      CONVERT(SMALLINT, 0)),
        ('Neutral', CONVERT(BIT,1), 'MITRA',      N'Mitra',      N'Friend',       CONVERT(SMALLINT, 1)),
        ('Neutral', CONVERT(BIT,0), 'SHATRU',     N'Shatru',     N'Enemy',        CONVERT(SMALLINT,-1)),
        ('Enemy',   CONVERT(BIT,1), 'SAMA',       N'Sama',       N'Neutral',      CONVERT(SMALLINT, 0)),
        ('Enemy',   CONVERT(BIT,0), 'ADHISHATRU', N'Adhishatru', N'Great Enemy',  CONVERT(SMALLINT,-2))
    ) v (NaturalRelation, IsTemporaryFriend, CompoundCode, SanskritName, EnglishName, RelationshipScore);
GO

-- --- Batch 3: tbl_Rule_Catalog registration ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_CompoundRelationship')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_CompoundRelationship', 'DIGNITY', 'MATRIX_LOOKUP',
            'Panchadha Maitri: the natural x temporary compound relationship (Adhimitra..Adhishatru) with RelationshipScore -2..+2. Separate axis from tbl_Rule_GrahaDignity; combined only at the interpretation layer.',
            '24_add_rule_compound_relationship.sql');
GO

-- --- Batch 4: ledger + verification ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '24_add_rule_compound_relationship.sql',
       'tbl_Rule_CompoundRelationship (Panchadha Maitri 2x3 matrix + RelationshipScore -2..+2); tbl_Rule_Catalog row; natural/temporary relationship tables unchanged'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '24_add_rule_compound_relationship.sql');
GO

DECLARE @rows INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_CompoundRelationship);
DECLARE @badscore INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_CompoundRelationship
    WHERE RelationshipScore <> CASE CompoundCode WHEN 'ADHIMITRA' THEN 2 WHEN 'MITRA' THEN 1
                                                 WHEN 'SAMA' THEN 0 WHEN 'SHATRU' THEN -1
                                                 WHEN 'ADHISHATRU' THEN -2 END);
PRINT '24 applied: ' + CAST(@rows AS VARCHAR(10)) + ' rows (expect 6), '
    + CAST(@badscore AS VARCHAR(10)) + ' score<>code mismatches (expect 0).';
GO

-- The 6 rows must reproduce DignityEngine.CombineToPanchadha's truth table.
;WITH expected (NaturalRelation, IsTemporaryFriend, EnglishName) AS (
    SELECT * FROM (VALUES
        ('Friend',  CONVERT(BIT,1), N'Great Friend'),
        ('Friend',  CONVERT(BIT,0), N'Neutral'),
        ('Neutral', CONVERT(BIT,1), N'Friend'),
        ('Neutral', CONVERT(BIT,0), N'Enemy'),
        ('Enemy',   CONVERT(BIT,1), N'Neutral'),
        ('Enemy',   CONVERT(BIT,0), N'Great Enemy')
    ) e (NaturalRelation, IsTemporaryFriend, EnglishName)
)
SELECT e.NaturalRelation, e.IsTemporaryFriend, e.EnglishName AS Expected, r.EnglishName AS GotFromTable
FROM expected e
LEFT JOIN dbo.tbl_Rule_CompoundRelationship r
       ON r.RuleSetId = 1 AND r.NaturalRelation = e.NaturalRelation AND r.IsTemporaryFriend = e.IsTemporaryFriend
WHERE r.EnglishName IS NULL OR r.EnglishName <> e.EnglishName;
PRINT '^ CombineToPanchadha truth-table mismatches (expect 0 rows).';
GO
