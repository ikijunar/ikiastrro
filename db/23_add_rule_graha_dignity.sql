-- =====================================================================
-- 23 - tbl_Rule_GrahaDignity: axis-A graha dignity as data.
--
-- Replaces the private C# dicts in DignityEngine (ExaltationSign /
-- DebilitationSign / Moolatrikona) with a segmented, source-attributed,
-- rule-set-versioned table. Axis A = EXALTED / MOOLATRIKONA / OWN /
-- DEBILITATED only; the Panchadha Maitri axis (friend/neutral/enemy and
-- the two "great" tiers) stays in tbl_Rule_NaturalRelationship +
-- tbl_Rule_TemporaryFriendshipDistance + DignityEngine.CombineToPanchadha
-- and is NOT touched here.
--
-- Two rule-sets are seeded:
--   RuleSetId 2  PVR-Dignity-Integrated  - rows IsActive = 1 (the set the
--                engine will read). Source: P.V.R. Narasimha Rao,
--                "Vedic Astrology: An Integrated Approach", Table 6 + the
--                7 special-degree notes. docs/research/dignity-pvr-integrated.md
--   RuleSetId 3  BPHS-Dignity-Parashari  - rows IsActive = 0. Mirror of the
--                pre-2026-09 hard-coded DignityEngine behaviour, kept for
--                provenance and one-UPDATE rollback.
-- tbl_Rule_Sets.IsActive stays 0 for both: rule-set 1 (Parashari-Classical)
-- remains the single global active set (UX_RuleSets_OneActive), governing
-- relationships / combustion / varga. Dignity "active" is per row.
--
-- DignityScore ladder (rammyps, 2026-09-04):
--   EXALTED +4 . MOOLATRIKONA +3 . OWN +2 (every own row) . DEBILITATED -2
-- Great Friend -> +1, Great Enemy -> -1 are applied in the engine
-- (CombineToPanchadha), not stored here.
--
-- Idempotent: table / rule-set / catalog / source adds are guarded; the
-- seed does DELETE ... WHERE RuleSetId IN (2,3) then re-INSERT, so a re-run
-- refreshes the reference rows.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/23_add_rule_graha_dignity.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: rule-set rows (Ids 2, 3; both tbl_Rule_Sets.IsActive = 0) ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Sets WHERE Id = 2)
    INSERT dbo.tbl_Rule_Sets (Id, RuleSetName, Description, IsActive, SupersedesRuleSetId, SourceReference)
    VALUES (2, 'PVR-Dignity-Integrated',
            'Graha dignity per PVR Narasimha Rao, Integrated Approach Table 6 + the 7 special-degree notes. Active dignity set via tbl_Rule_GrahaDignity.IsActive=1; rule-set 1 stays the global active set.',
            0, 1, 'docs/research/dignity-pvr-integrated.md');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Sets WHERE Id = 3)
    INSERT dbo.tbl_Rule_Sets (Id, RuleSetName, Description, IsActive)
    VALUES (3, 'BPHS-Dignity-Parashari',
            'Graha dignity mirror of the pre-2026-09 hard-coded DignityEngine dicts (BPHS/Parashari). Inactive; kept for provenance and one-UPDATE rollback.',
            0);
GO

-- --- Batch 2: SRC_PVR_INTEGRATED citation (idempotent MERGE on Code) ---
;WITH src (Code, Title, Author, Edition, Tradition, Notes) AS (
    SELECT * FROM (VALUES
        ('SRC_PVR_INTEGRATED', N'Vedic Astrology: An Integrated Approach', N'P. V. R. Narasimha Rao', NULL, 'PVR Integrated',
         N'Part 1 Chart Analysis, Table 6 (Dignities of Planets) + the 7 special-degree notes. Distinct from SRC_JHORA (same author, desktop software).')
    ) v (Code, Title, Author, Edition, Tradition, Notes)
)
MERGE dbo.tbl_Dim_Source AS tgt
USING src ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET tgt.Title = src.Title, tgt.Author = src.Author,
    tgt.Edition = src.Edition, tgt.Tradition = src.Tradition, tgt.Notes = src.Notes
WHEN NOT MATCHED THEN INSERT (Code, Title, Author, Edition, Tradition, Notes)
    VALUES (src.Code, src.Title, src.Author, src.Edition, src.Tradition, src.Notes);
GO

-- --- Batch 3: the table ---
IF OBJECT_ID('dbo.tbl_Rule_GrahaDignity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_GrahaDignity (
        Id                     INT IDENTITY(1,1) NOT NULL
                                   CONSTRAINT PK_Rule_GrahaDignity PRIMARY KEY,
        RuleSetId              TINYINT      NOT NULL
                                   CONSTRAINT FK_Rule_GrahaDignity_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        PlanetId               TINYINT      NOT NULL
                                   CONSTRAINT FK_Rule_GrahaDignity_Planet  FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        SignId                 TINYINT      NOT NULL
                                   CONSTRAINT FK_Rule_GrahaDignity_Sign    FOREIGN KEY REFERENCES dbo.tbl_SignAttributes (Id),
        DignityTypeCode        VARCHAR(20)  NOT NULL,   -- EXALTED / MOOLATRIKONA / OWN / DEBILITATED (axis A only)
        StartDegree            DECIMAL(5,2) NOT NULL,   -- 0..30; segment within the sign
        EndDegree              DECIMAL(5,2) NOT NULL,   -- > Start, <= 30
        DeepDegree             DECIMAL(5,2) NULL,       -- EXALTED / DEBILITATED classical-7 only; NULL for nodes
        DignityScore           SMALLINT     NOT NULL,   -- EXALTED +4 . MOOLATRIKONA +3 . OWN +2 . DEBILITATED -2
        IsPrimary              BIT          NOT NULL
                                   CONSTRAINT DF_Rule_GrahaDignity_IsPrimary DEFAULT 1,  -- 1 = the planet's "home" own sign; does NOT affect DignityScore
        Mood                   VARCHAR(20)  NULL,       -- constant per DignityTypeCode
        InterpretationTendency NVARCHAR(200) NULL,      -- constant per DignityTypeCode
        Analogy                NVARCHAR(400) NULL,      -- constant per DignityTypeCode (PVR home/office/picnic)
        DignityRationale       NVARCHAR(MAX) NULL,      -- PVR per-planet "why"; NULL where the source is silent
        MethodCode             VARCHAR(30)  NULL,       -- 'SEGMENT_LOOKUP'
        RuleParametersJson     NVARCHAR(MAX) NULL,
        CalculationNarrative   NVARCHAR(MAX) NULL,      -- e.g. the Mars/Aries note-typo record
        SourceRefCode          VARCHAR(40)  NULL,
        IsActive               BIT          NOT NULL
                                   CONSTRAINT DF_Rule_GrahaDignity_IsActive DEFAULT 1,
        CONSTRAINT CK_RuleGrahaDignity_Type    CHECK (DignityTypeCode IN ('EXALTED','MOOLATRIKONA','OWN','DEBILITATED')),
        CONSTRAINT CK_RuleGrahaDignity_Degrees CHECK (StartDegree >= 0 AND EndDegree > StartDegree AND EndDegree <= 30),
        CONSTRAINT CK_RuleGrahaDignity_Deep    CHECK (DeepDegree IS NULL OR (DeepDegree >= 0 AND DeepDegree <= 30)),
        CONSTRAINT CK_RuleGrahaDignity_Score   CHECK (DignityScore BETWEEN -2 AND 4),
        CONSTRAINT CK_RuleGrahaDignity_Json    CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_RuleGrahaDignity_Src     CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_GrahaDignity UNIQUE (RuleSetId, PlanetId, SignId, DignityTypeCode, StartDegree)
    );
    CREATE NONCLUSTERED INDEX IX_Rule_GrahaDignity_Lookup
        ON dbo.tbl_Rule_GrahaDignity (RuleSetId, PlanetId, SignId)
        INCLUDE (StartDegree, EndDegree, DignityTypeCode, DignityScore, DeepDegree);
END
GO

-- --- Batch 4: tbl_Rule_Catalog registration ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_GrahaDignity')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_GrahaDignity', 'DIGNITY', 'SEGMENT_LOOKUP',
            'Graha dignity (axis A): exaltation / debilitation / moolatrikona / own-sign degree segments per rule-set, with deep-degree points and DignityScore. PVR Integrated Approach Table 6 active; BPHS-Parashari mirror inactive.',
            '23_add_rule_graha_dignity.sql');
GO

-- --- Batch 5: seed both rule-sets (DELETE + re-INSERT; idempotent) ---
DELETE FROM dbo.tbl_Rule_GrahaDignity WHERE RuleSetId IN (2, 3);
GO

DECLARE @r_ju_pisces  NVARCHAR(400) = N'Pisces is the 12th house of the natural zodiac; saattwik and ethery - Jupiter is most comfortable here. This is his home, like a peaceful Brahmin doing pooja.';
DECLARE @r_ju_sag     NVARCHAR(400) = N'Sagittarius is the 9th house of the natural zodiac; upholding dharma is Jupiter''s duty. He is like a raaja-purohit who must sometimes take strong decisions.';
DECLARE @r_ju_cancer  NVARCHAR(400) = N'Cancer is the 4th house of the natural zodiac; Jupiter is excited to do imaginative (watery) learning.';
DECLARE @r_ju_cap     NVARCHAR(400) = N'Capricorn is the 10th house of the natural zodiac, taamasik; Jupiter dislikes well-defined taamasik karma - it is against his nature.';
DECLARE @r_me_gemini  NVARCHAR(400) = N'Gemini is the 3rd house of the natural zodiac (communications); intelligent communication is what Mercury is most comfortable with. This is his home.';
DECLARE @r_me_virgo   NVARCHAR(400) = N'Virgo is the 6th house of the natural zodiac (debate and arguments); Mercury loves this official job, so Virgo is both his office (moolatrikona) and his favourite picnic spot (exaltation).';
DECLARE @r_ke_scorpio NVARCHAR(400) = N'Scorpio is the 8th house of the natural zodiac; Ketu is most comfortable with occult activity, so he owns it.';
DECLARE @r_ke_pisces  NVARCHAR(400) = N'Pisces is the 12th house of the natural zodiac; Ketu''s duty is giving upaasana (meditation) and moksha (liberation).';
DECLARE @n_mars_typo  NVARCHAR(400) = N'PVR special-point note as printed reads "first 12 deg of Leo", which contradicts Table 6 (Mars moolatrikona = Aries) and the note itself. Encoded as Aries per Table 6.';

-- Per-type metadata + score (constant per DignityTypeCode).
;WITH meta (DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy) AS (
    SELECT * FROM (VALUES
        ('EXALTED',      CONVERT(SMALLINT,  4), 'Elevated',      N'Performs exceptionally and enthusiastically.', N'Favourite picnic or party - excited and eager, performing at its best.'),
        ('MOOLATRIKONA', CONVERT(SMALLINT,  3), 'Dutiful',       N'Powerful, purposeful and responsible.',        N'Office - executes its formal duty, whether it enjoys the work or not.'),
        ('OWN',          CONVERT(SMALLINT,  2), 'Comfortable',   N'Natural, authentic and relaxed.',              N'Home - most natural, comfortable and at ease.'),
        ('DEBILITATED',  CONVERT(SMALLINT, -2), 'Uncomfortable', N'Struggles to express its natural qualities.',  N'Worst party - unhappy and stuck where it hates to be.')
    ) m (DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy)
),
pvr (PlanetName, SignName, DignityTypeCode, StartDegree, EndDegree, DeepDegree, IsPrimary, DignityRationale, CalcNarr) AS (
    SELECT * FROM (VALUES
        -- Sun
        ('Sun','Aries','EXALTED',        0, 30,  10, 1, CONVERT(NVARCHAR(MAX),NULL), CONVERT(NVARCHAR(MAX),NULL)),
        ('Sun','Leo','MOOLATRIKONA',     0, 20, NULL, 1, NULL, NULL),
        ('Sun','Leo','OWN',             20, 30, NULL, 1, NULL, NULL),
        ('Sun','Libra','DEBILITATED',    0, 30,  10, 1, NULL, NULL),
        -- Moon
        ('Moon','Taurus','EXALTED',      0,  3,   3, 1, NULL, NULL),
        ('Moon','Taurus','MOOLATRIKONA', 3, 30, NULL, 1, NULL, NULL),
        ('Moon','Cancer','OWN',          0, 30, NULL, 1, NULL, NULL),
        ('Moon','Scorpio','DEBILITATED', 0, 30,   3, 1, NULL, NULL),
        -- Mars
        ('Mars','Capricorn','EXALTED',   0, 30,  28, 1, NULL, NULL),
        ('Mars','Aries','MOOLATRIKONA',  0, 12, NULL, 1, NULL, @n_mars_typo),
        ('Mars','Aries','OWN',          12, 30, NULL, 1, NULL, NULL),
        ('Mars','Scorpio','OWN',         0, 30, NULL, 0, NULL, NULL),
        ('Mars','Cancer','DEBILITATED',  0, 30,  28, 1, NULL, NULL),
        -- Mercury
        ('Mercury','Virgo','EXALTED',    0, 15,  15, 1, @r_me_virgo,  NULL),
        ('Mercury','Virgo','MOOLATRIKONA',15,20, NULL, 0, @r_me_virgo, NULL),
        ('Mercury','Virgo','OWN',        20, 30, NULL, 0, @r_me_virgo,  NULL),
        ('Mercury','Gemini','OWN',        0, 30, NULL, 1, @r_me_gemini, NULL),
        ('Mercury','Pisces','DEBILITATED',0,30,  15, 1, NULL, NULL),
        -- Jupiter
        ('Jupiter','Cancer','EXALTED',   0, 30,   5, 1, @r_ju_cancer, NULL),
        ('Jupiter','Sagittarius','MOOLATRIKONA',0,10,NULL,0,@r_ju_sag, NULL),
        ('Jupiter','Sagittarius','OWN', 10, 30, NULL, 0, @r_ju_sag,    NULL),
        ('Jupiter','Pisces','OWN',       0, 30, NULL, 1, @r_ju_pisces, NULL),
        ('Jupiter','Capricorn','DEBILITATED',0,30, 5, 1, @r_ju_cap,    NULL),
        -- Venus
        ('Venus','Pisces','EXALTED',     0, 30,  27, 1, NULL, NULL),
        ('Venus','Libra','MOOLATRIKONA', 0, 15, NULL, 0, NULL, NULL),
        ('Venus','Libra','OWN',         15, 30, NULL, 0, NULL, NULL),
        ('Venus','Taurus','OWN',         0, 30, NULL, 1, NULL, NULL),
        ('Venus','Virgo','DEBILITATED',  0, 30,  27, 1, NULL, NULL),
        -- Saturn
        ('Saturn','Libra','EXALTED',     0, 30,  20, 1, NULL, NULL),
        ('Saturn','Aquarius','MOOLATRIKONA',0,20,NULL,0, NULL, NULL),
        ('Saturn','Aquarius','OWN',      20, 30, NULL, 0, NULL, NULL),
        ('Saturn','Capricorn','OWN',      0, 30, NULL, 1, NULL, NULL),
        ('Saturn','Aries','DEBILITATED', 0, 30,  20, 1, NULL, NULL),
        -- Rahu (PVR node scheme - source-dependent, no deep degree)
        ('Rahu','Gemini','EXALTED',      0, 30, NULL, 1, NULL, NULL),
        ('Rahu','Virgo','MOOLATRIKONA',  0, 30, NULL, 1, NULL, NULL),
        ('Rahu','Aquarius','OWN',        0, 30, NULL, 1, NULL, NULL),
        ('Rahu','Sagittarius','DEBILITATED',0,30,NULL,1, NULL, NULL),
        -- Ketu (PVR node scheme)
        ('Ketu','Sagittarius','EXALTED', 0, 30, NULL, 1, NULL, NULL),
        ('Ketu','Pisces','MOOLATRIKONA', 0, 30, NULL, 1, @r_ke_pisces,  NULL),
        ('Ketu','Scorpio','OWN',         0, 30, NULL, 1, @r_ke_scorpio, NULL),
        ('Ketu','Gemini','DEBILITATED',  0, 30, NULL, 1, NULL, NULL)
    ) v (PlanetName, SignName, DignityTypeCode, StartDegree, EndDegree, DeepDegree, IsPrimary, DignityRationale, CalcNarr)
)
INSERT dbo.tbl_Rule_GrahaDignity
    (RuleSetId, PlanetId, SignId, DignityTypeCode, StartDegree, EndDegree, DeepDegree, DignityScore,
     IsPrimary, Mood, InterpretationTendency, Analogy, DignityRationale, MethodCode, CalculationNarrative,
     SourceRefCode, IsActive)
SELECT 2, p.Id, s.Id, d.DignityTypeCode,
       CONVERT(DECIMAL(5,2), d.StartDegree), CONVERT(DECIMAL(5,2), d.EndDegree), CONVERT(DECIMAL(5,2), d.DeepDegree),
       m.DignityScore, d.IsPrimary, m.Mood, m.InterpretationTendency, m.Analogy, d.DignityRationale,
       'SEGMENT_LOOKUP', d.CalcNarr, 'SRC_PVR_INTEGRATED', 1
FROM pvr d
JOIN dbo.tbl_Planets p        ON p.PlanetName = d.PlanetName
JOIN dbo.tbl_SignAttributes s ON s.SignName  = d.SignName
JOIN meta m                   ON m.DignityTypeCode = d.DignityTypeCode;

-- BPHS-Parashari mirror (RuleSetId 3, IsActive = 0) - reflects the pre-2026-09
-- DignityEngine: no MT split for Moon/Taurus or Mercury/Virgo (their exaltation
-- sign wins the priority check for the whole sign), BPHS node signs, no node
-- own/moolatrikona rows.
;WITH meta (DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy) AS (
    SELECT * FROM (VALUES
        ('EXALTED',      CONVERT(SMALLINT,  4), 'Elevated',      N'Performs exceptionally and enthusiastically.', N'Favourite picnic or party - excited and eager, performing at its best.'),
        ('MOOLATRIKONA', CONVERT(SMALLINT,  3), 'Dutiful',       N'Powerful, purposeful and responsible.',        N'Office - executes its formal duty, whether it enjoys the work or not.'),
        ('OWN',          CONVERT(SMALLINT,  2), 'Comfortable',   N'Natural, authentic and relaxed.',              N'Home - most natural, comfortable and at ease.'),
        ('DEBILITATED',  CONVERT(SMALLINT, -2), 'Uncomfortable', N'Struggles to express its natural qualities.',  N'Worst party - unhappy and stuck where it hates to be.')
    ) m (DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy)
),
bphs (PlanetName, SignName, DignityTypeCode, StartDegree, EndDegree, DeepDegree, IsPrimary) AS (
    SELECT * FROM (VALUES
        ('Sun','Aries','EXALTED',        0, 30,  10, 1),
        ('Sun','Leo','MOOLATRIKONA',     0, 20, NULL, 1),
        ('Sun','Leo','OWN',             20, 30, NULL, 1),
        ('Sun','Libra','DEBILITATED',    0, 30,  10, 1),
        ('Moon','Taurus','EXALTED',      0, 30,   3, 1),
        ('Moon','Cancer','OWN',          0, 30, NULL, 1),
        ('Moon','Scorpio','DEBILITATED', 0, 30,   3, 1),
        ('Mars','Capricorn','EXALTED',   0, 30,  28, 1),
        ('Mars','Aries','MOOLATRIKONA',  0, 12, NULL, 1),
        ('Mars','Aries','OWN',          12, 30, NULL, 1),
        ('Mars','Scorpio','OWN',         0, 30, NULL, 0),
        ('Mars','Cancer','DEBILITATED',  0, 30,  28, 1),
        ('Mercury','Virgo','EXALTED',    0, 30,  15, 1),
        ('Mercury','Gemini','OWN',       0, 30, NULL, 1),
        ('Mercury','Pisces','DEBILITATED',0,30,  15, 1),
        ('Jupiter','Cancer','EXALTED',   0, 30,   5, 1),
        ('Jupiter','Sagittarius','MOOLATRIKONA',0,10,NULL,0),
        ('Jupiter','Sagittarius','OWN', 10, 30, NULL, 0),
        ('Jupiter','Pisces','OWN',       0, 30, NULL, 1),
        ('Jupiter','Capricorn','DEBILITATED',0,30, 5, 1),
        ('Venus','Pisces','EXALTED',     0, 30,  27, 1),
        ('Venus','Libra','MOOLATRIKONA', 0, 15, NULL, 0),
        ('Venus','Libra','OWN',         15, 30, NULL, 0),
        ('Venus','Taurus','OWN',         0, 30, NULL, 1),
        ('Venus','Virgo','DEBILITATED',  0, 30,  27, 1),
        ('Saturn','Libra','EXALTED',     0, 30,  20, 1),
        ('Saturn','Aquarius','MOOLATRIKONA',0,20,NULL,0),
        ('Saturn','Aquarius','OWN',      20, 30, NULL, 0),
        ('Saturn','Capricorn','OWN',      0, 30, NULL, 1),
        ('Saturn','Aries','DEBILITATED', 0, 30,  20, 1),
        ('Rahu','Taurus','EXALTED',      0, 30, NULL, 1),
        ('Rahu','Scorpio','DEBILITATED', 0, 30, NULL, 1),
        ('Ketu','Scorpio','EXALTED',     0, 30, NULL, 1),
        ('Ketu','Taurus','DEBILITATED',  0, 30, NULL, 1)
    ) v (PlanetName, SignName, DignityTypeCode, StartDegree, EndDegree, DeepDegree, IsPrimary)
)
INSERT dbo.tbl_Rule_GrahaDignity
    (RuleSetId, PlanetId, SignId, DignityTypeCode, StartDegree, EndDegree, DeepDegree, DignityScore,
     IsPrimary, Mood, InterpretationTendency, Analogy, DignityRationale, MethodCode, CalculationNarrative,
     SourceRefCode, IsActive)
SELECT 3, p.Id, s.Id, d.DignityTypeCode,
       CONVERT(DECIMAL(5,2), d.StartDegree), CONVERT(DECIMAL(5,2), d.EndDegree), CONVERT(DECIMAL(5,2), d.DeepDegree),
       m.DignityScore, d.IsPrimary, m.Mood, m.InterpretationTendency, m.Analogy, NULL,
       'SEGMENT_LOOKUP', NULL, 'SRC_BPHS', 0
FROM bphs d
JOIN dbo.tbl_Planets p        ON p.PlanetName = d.PlanetName
JOIN dbo.tbl_SignAttributes s ON s.SignName  = d.SignName
JOIN meta m                   ON m.DignityTypeCode = d.DignityTypeCode;
GO

-- --- Batch 6: vw_Dignity_Legend (7-row de-duplicated read of the active set) ---
IF OBJECT_ID('dbo.vw_Dignity_Legend', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Dignity_Legend;
GO
CREATE VIEW dbo.vw_Dignity_Legend AS
SELECT DISTINCT DignityTypeCode, DignityScore, Mood, InterpretationTendency, Analogy
FROM dbo.tbl_Rule_GrahaDignity
WHERE IsActive = 1;
GO

-- --- Batch 7: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '23_add_rule_graha_dignity.sql',
       'tbl_Rule_GrahaDignity (axis-A segments + DignityScore); seed PVR (RuleSetId 2, active) + BPHS mirror (RuleSetId 3, inactive); + SRC_PVR_INTEGRATED, catalog row, vw_Dignity_Legend'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '23_add_rule_graha_dignity.sql');
GO

DECLARE @pvr INT  = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity WHERE RuleSetId = 2);
DECLARE @bphs INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity WHERE RuleSetId = 3);
DECLARE @badscore INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaDignity
    WHERE DignityScore <> CASE DignityTypeCode WHEN 'EXALTED' THEN 4 WHEN 'MOOLATRIKONA' THEN 3
                                               WHEN 'OWN' THEN 2 WHEN 'DEBILITATED' THEN -2 END);
DECLARE @legend INT = (SELECT COUNT(*) FROM dbo.vw_Dignity_Legend);
PRINT '23 applied: ' + CAST(@pvr AS VARCHAR(10)) + ' PVR rows (expect 41), '
    + CAST(@bphs AS VARCHAR(10)) + ' BPHS rows (expect 34), '
    + CAST(@badscore AS VARCHAR(10)) + ' score<>type mismatches (expect 0), '
    + CAST(@legend AS VARCHAR(10)) + ' legend rows (expect 4).';
GO

-- Tiling check: per (RuleSetId, PlanetId, SignId), segments must start at 0,
-- end at 30, and be gap-free / overlap-free.
;WITH seg AS (
    SELECT RuleSetId, PlanetId, SignId, StartDegree, EndDegree,
           LEAD(StartDegree) OVER (PARTITION BY RuleSetId, PlanetId, SignId ORDER BY StartDegree) AS NextStart,
           MIN(StartDegree)  OVER (PARTITION BY RuleSetId, PlanetId, SignId) AS MinStart,
           MAX(EndDegree)    OVER (PARTITION BY RuleSetId, PlanetId, SignId) AS MaxEnd
    FROM dbo.tbl_Rule_GrahaDignity
)
SELECT RuleSetId, PlanetId, SignId, StartDegree, EndDegree, NextStart, MinStart, MaxEnd
FROM seg
WHERE MinStart <> 0 OR MaxEnd <> 30 OR (NextStart IS NOT NULL AND NextStart <> EndDegree);
PRINT '^ tiling violations (expect 0 rows).';
GO
