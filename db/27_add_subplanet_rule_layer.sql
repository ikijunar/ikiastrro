-- =====================================================================
-- 27 - Sub-planet (upagraha) reference layer: master + calculation rules.
--
-- New tables use the English "Planet / Sub Planet" vocabulary (rammyps,
-- 2026-09-04). The existing graha/upagraha table names are NOT renamed.
-- Names follow STANDARDS.md §D: tbl_<Category>_<PascalCase> (no internal
-- underscore after the infix), Dim names plural, Rule names singular, PK
-- column is Id.
--
--   tbl_Dim_SubPlanets             - the 11-row master. Two calculation
--                                    families: SUN_LONGITUDE (Dhuma,
--                                    Vyatipata, Parivesha, Indrachapa,
--                                    Upaketu) and DAY_NIGHT_TIME (Kaala,
--                                    Mrityu, Ardhaprahara, Yamaghantaka,
--                                    Gulika, Maandi). AssociatedPlanetId
--                                    is the interpretive analogy planet,
--                                    NOT identity.
--   tbl_Rule_SubPlanetSunLongitude - the 5-step longitude chain off the
--                                    Sun. Seeded (calc not built in C#).
--   tbl_Rule_SubPlanetPartRuler    - PVR "Table 10": which of the 9 grahas
--                                    (or none) rules each of the 8 equal
--                                    parts of the day / night arc, per
--                                    weekday. 112 rows. This is the exact
--                                    table in "Vedic Astrology: An
--                                    Integrated Approach" p.43-44.
--   tbl_Rule_SubPlanetTime        - the 6 time-based sub-planets: each
--                                    rises at a fraction (0 = start,
--                                    0.5 = middle) of the 1/8 part ruled
--                                    by a specific graha. PVR: Kaala mid
--                                    of Sun's part, Mrityu mid of Mars's,
--                                    Ardhaprahara mid of Mercury's,
--                                    Yamaghantaka mid of Jupiter's, Gulika
--                                    mid of Saturn's, Maandi START of
--                                    Saturn's. The rising Ascendant at that
--                                    instant is the sub-planet longitude.
--
-- No varga projection here - D1 rule data only (deferred per rammyps).
-- RuleSetId 1 (Parashari-Classical), SourceRefCode SRC_PVR_INTEGRATED on
-- every seeded row - the one registered source for the P.V.R. Narasimha
-- Rao corpus (STANDARDS.md §D / §M.4), regardless of derivative naming.
--
-- Idempotent: table / catalog adds guarded; seeds are IF NOT EXISTS on
-- their table.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/27_add_subplanet_rule_layer.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_SubPlanets (master) ---
IF OBJECT_ID('dbo.tbl_Dim_SubPlanets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_SubPlanets (
        Id                 TINYINT       NOT NULL
                               CONSTRAINT PK_Dim_SubPlanets PRIMARY KEY,        -- fixed 11-row domain, like tbl_Planets
        SubPlanetCode      VARCHAR(20)   NOT NULL
                               CONSTRAINT UQ_Dim_SubPlanets_Code UNIQUE,        -- DHUMA, VYATIPATA, ...
        SubPlanetName      VARCHAR(30)   NOT NULL
                               CONSTRAINT UQ_Dim_SubPlanets_Name UNIQUE,        -- app display name (== SpecialPointSeed.Code for Gulika/Maandi)
        EnglishMeaning     NVARCHAR(60)  NULL,                                  -- literal gloss; NULL where there is no common translation
        CalculationType    VARCHAR(20)   NOT NULL,                              -- SUN_LONGITUDE | DAY_NIGHT_TIME
        AssociatedPlanetId TINYINT       NOT NULL
                               CONSTRAINT FK_Dim_SubPlanets_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        NaturalNature      VARCHAR(12)   NULL,                                  -- Malefic | Benefic | Neutral | Mixed; NULL = source silent
        SortOrder          TINYINT       NOT NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_Dim_SubPlanets_IsActive DEFAULT 1,
        Notes              NVARCHAR(400) NULL,
        CONSTRAINT CK_Dim_SubPlanets_CalcType CHECK (CalculationType IN ('SUN_LONGITUDE','DAY_NIGHT_TIME')),
        CONSTRAINT CK_Dim_SubPlanets_Nature   CHECK (NaturalNature IS NULL OR NaturalNature IN ('Malefic','Benefic','Neutral','Mixed'))
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_SubPlanets)
    INSERT dbo.tbl_Dim_SubPlanets
        (Id, SubPlanetCode, SubPlanetName, EnglishMeaning, CalculationType, AssociatedPlanetId, NaturalNature, SortOrder, Notes)
    SELECT v.Id, v.SubPlanetCode, v.SubPlanetName, v.EnglishMeaning, v.CalculationType,
           p.Id, v.NaturalNature, v.SortOrder, v.Notes
    FROM (VALUES
        ( 1,'DHUMA',       'Dhuma',       N'Smoke',        'SUN_LONGITUDE', 'Sun',    'Malefic',  1, CONVERT(NVARCHAR(400),NULL)),
        ( 2,'VYATIPATA',   'Vyatipata',   N'Calamity',     'SUN_LONGITUDE', 'Sun',    'Malefic',  2, NULL),
        ( 3,'PARIVESHA',   'Parivesha',   N'Halo',         'SUN_LONGITUDE', 'Sun',    'Malefic',  3, N'Some traditions class Parivesha (halo) as benefic / neutral.'),
        ( 4,'INDRACHAPA',  'Indrachapa',  N'Rainbow',      'SUN_LONGITUDE', 'Sun',    'Malefic',  4, N'Some traditions class Indrachapa (rainbow) as benefic / neutral.'),
        ( 5,'UPAKETU',     'Upaketu',     N'Sub-Ketu',     'SUN_LONGITUDE', 'Sun',    'Malefic',  5, N'Chain identity: Upaketu + 30 deg = Sun.'),
        ( 6,'KAALA',       'Kaala',       N'Time',         'DAY_NIGHT_TIME','Sun',    'Malefic',  6, NULL),
        ( 7,'MRITYU',      'Mrityu',      N'Death',        'DAY_NIGHT_TIME','Mars',   'Malefic',  7, NULL),
        ( 8,'ARDHAPRAHARA','Ardhaprahara',N'Half-prahara', 'DAY_NIGHT_TIME','Mercury', NULL,      8, N'PVR text spells it "Artha Praharaka" / "Artha Prahara". BPHS: Mercury''s day/night portion.'),
        ( 9,'YAMAGHANTAKA','Yamaghantaka',N'Yama''s bell', 'DAY_NIGHT_TIME','Jupiter', NULL,      9, N'BPHS names Jupiter''s day/night portion Yamaghantaka.'),
        (10,'GULIKA',      'Gulika',      NULL,            'DAY_NIGHT_TIME','Saturn', 'Malefic', 10, N'Ships today via UpagrahaCalculator.cs. See tbl_Rule_SubPlanetTime for the start-vs-middle divergence between the PVR text and the shipped (JHora) convention.'),
        (11,'MAANDI',      'Maandi',      NULL,            'DAY_NIGHT_TIME','Saturn', 'Malefic', 11, N'Ships today via UpagrahaCalculator.cs. Often treated as the same Saturn upagraha as Gulika.')
    ) v (Id, SubPlanetCode, SubPlanetName, EnglishMeaning, CalculationType, AssocPlanetName, NaturalNature, SortOrder, Notes)
    JOIN dbo.tbl_Planets p ON p.PlanetName = v.AssocPlanetName;
GO

-- --- Batch 2: tbl_Rule_SubPlanetSunLongitude ---
IF OBJECT_ID('dbo.tbl_Rule_SubPlanetSunLongitude', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SubPlanetSunLongitude (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SubPlanetSunLongitude PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SubPlanetSunLongitude_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SubPlanetId          TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SubPlanetSunLongitude_SubPlanet FOREIGN KEY REFERENCES dbo.tbl_Dim_SubPlanets (Id),
        SequenceNo           TINYINT      NOT NULL,          -- evaluation order within the chain
        Operation            VARCHAR(16)  NOT NULL,          -- ADD | COMPLEMENT_360
        InputSubPlanetId     TINYINT      NULL               -- NULL => operate on the Sun's nirayana longitude
                                 CONSTRAINT FK_Rule_SubPlanetSunLongitude_Input FOREIGN KEY REFERENCES dbo.tbl_Dim_SubPlanets (Id),
        OffsetDegrees        DECIMAL(9,6) NULL,              -- required for ADD, NULL for COMPLEMENT_360
        NormalizationMethod  VARCHAR(12)  NOT NULL CONSTRAINT DF_Rule_SubPlanetSunLongitude_Norm DEFAULT 'MOD_360',
        MethodCode           VARCHAR(30)  NULL,              -- 'SUN_LONGITUDE_CHAIN'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SubPlanetSunLongitude_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SubPlanetSunLongitude_Op   CHECK (Operation IN ('ADD','COMPLEMENT_360')),
        CONSTRAINT CK_Rule_SubPlanetSunLongitude_Off  CHECK ((Operation = 'ADD' AND OffsetDegrees IS NOT NULL)
                                                          OR (Operation = 'COMPLEMENT_360' AND OffsetDegrees IS NULL)),
        CONSTRAINT CK_Rule_SubPlanetSunLongitude_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SubPlanetSunLongitude_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SubPlanetSunLongitude UNIQUE (RuleSetId, SubPlanetId, SequenceNo)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetSunLongitude)
    INSERT dbo.tbl_Rule_SubPlanetSunLongitude
        (RuleSetId, SubPlanetId, SequenceNo, Operation, InputSubPlanetId, OffsetDegrees,
         NormalizationMethod, MethodCode, CalculationNarrative, SourceRefCode, IsActive)
    SELECT 1, sp.Id, v.SequenceNo, v.Operation, inp.Id,
           CONVERT(DECIMAL(9,6), v.OffsetDegrees), 'MOD_360', 'SUN_LONGITUDE_CHAIN',
           v.Narrative, 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        ('DHUMA',      1, 'ADD',            CONVERT(VARCHAR(20),NULL),  133.333333, N'Dhuma = Sun + 133 deg 20 min.'),
        ('VYATIPATA',  2, 'COMPLEMENT_360', 'DHUMA',                    CONVERT(DECIMAL(9,6),NULL), N'Vyatipata = 360 deg - Dhuma. Identity: Dhuma + Vyatipata = 360.'),
        ('PARIVESHA',  3, 'ADD',            'VYATIPATA',                180.000000, N'Parivesha = Vyatipata + 180 deg.'),
        ('INDRACHAPA', 4, 'COMPLEMENT_360', 'PARIVESHA',                CONVERT(DECIMAL(9,6),NULL), N'Indrachapa = 360 deg - Parivesha. Identity: Parivesha + Indrachapa = 360.'),
        ('UPAKETU',    5, 'ADD',            'INDRACHAPA',               16.666667,  N'Upaketu = Indrachapa + 16 deg 40 min. Validation identity: Upaketu + 30 deg = Sun.')
    ) v (SubPlanetCode, SequenceNo, Operation, InputCode, OffsetDegrees, Narrative)
    JOIN dbo.tbl_Dim_SubPlanets sp ON sp.SubPlanetCode = v.SubPlanetCode
    LEFT JOIN dbo.tbl_Dim_SubPlanets inp ON inp.SubPlanetCode = v.InputCode;
GO

-- --- Batch 3: tbl_Rule_SubPlanetPartRuler (PVR "Table 10") ---
-- The day arc (sunrise->sunset) or night arc (sunset->next sunrise) is split
-- into 8 equal parts. DAY: part 1 is ruled by the weekday lord, then the
-- grahas in weekday order (Sun, Moon, Mars, Mercury, Jupiter, Venus, Saturn);
-- the part right AFTER Saturn's is lord-less, then the sequence wraps to Sun.
-- NIGHT: part 1 is ruled by the 5th graha from the weekday lord (counting the
-- lord as 1), same sequence afterwards. Weekday is the Vedic day (opens at
-- sunrise); Weekday 0 = Sunday to match C# (int)DayOfWeek.
IF OBJECT_ID('dbo.tbl_Rule_SubPlanetPartRuler', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SubPlanetPartRuler (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SubPlanetPartRuler PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SubPlanetPartRuler_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        DayNight             VARCHAR(5)   NOT NULL,          -- DAY | NIGHT
        Weekday              TINYINT      NOT NULL,          -- 0 = Sunday ((int)DayOfWeek)
        PartNumber           TINYINT      NOT NULL,          -- 1..8
        RulingPlanetId       TINYINT      NULL              -- NULL = the lord-less part (always the one after Saturn's)
                                 CONSTRAINT FK_Rule_SubPlanetPartRuler_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        MethodCode           VARCHAR(30)  NULL,             -- 'PART_RULER_LOOKUP'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SubPlanetPartRuler_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SubPlanetPartRuler_DN   CHECK (DayNight IN ('DAY','NIGHT')),
        CONSTRAINT CK_Rule_SubPlanetPartRuler_WD   CHECK (Weekday BETWEEN 0 AND 6),
        CONSTRAINT CK_Rule_SubPlanetPartRuler_Part CHECK (PartNumber BETWEEN 1 AND 8),
        CONSTRAINT CK_Rule_SubPlanetPartRuler_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SubPlanetPartRuler_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SubPlanetPartRuler UNIQUE (RuleSetId, DayNight, Weekday, PartNumber)
    );
    CREATE NONCLUSTERED INDEX IX_Rule_SubPlanetPartRuler_Lookup
        ON dbo.tbl_Rule_SubPlanetPartRuler (RuleSetId, DayNight, Weekday)
        INCLUDE (PartNumber, RulingPlanetId);
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetPartRuler)
    INSERT dbo.tbl_Rule_SubPlanetPartRuler
        (RuleSetId, DayNight, Weekday, PartNumber, RulingPlanetId, MethodCode, SourceRefCode, IsActive)
    SELECT 1, t.DayNight, t.Weekday, x.PartNumber, p.Id, 'PART_RULER_LOOKUP', 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        --  DN       WD  P1      P2      P3      P4      P5      P6      P7      P8
        ('DAY',   0, 'Sun',    'Moon',   'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL),
        ('DAY',   1, 'Moon',   'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun'),
        ('DAY',   2, 'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon'),
        ('DAY',   3, 'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars'),
        ('DAY',   4, 'Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury'),
        ('DAY',   5, 'Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury','Jupiter'),
        ('DAY',   6, 'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury','Jupiter','Venus'),
        ('NIGHT', 0, 'Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury'),
        ('NIGHT', 1, 'Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury','Jupiter'),
        ('NIGHT', 2, 'Saturn', NULL,     'Sun',    'Moon',   'Mars',   'Mercury','Jupiter','Venus'),
        ('NIGHT', 3, 'Sun',    'Moon',   'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL),
        ('NIGHT', 4, 'Moon',   'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun'),
        ('NIGHT', 5, 'Mars',   'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon'),
        ('NIGHT', 6, 'Mercury','Jupiter','Venus',  'Saturn', NULL,     'Sun',    'Moon',   'Mars')
    ) t (DayNight, Weekday, P1, P2, P3, P4, P5, P6, P7, P8)
    CROSS APPLY (VALUES
        (CONVERT(TINYINT,1), t.P1), (2, t.P2), (3, t.P3), (4, t.P4),
        (5, t.P5), (6, t.P6), (7, t.P7), (8, t.P8)
    ) x (PartNumber, PlanetName)
    LEFT JOIN dbo.tbl_Planets p ON p.PlanetName = x.PlanetName;
GO

-- --- Batch 4: tbl_Rule_SubPlanetTime (the 6 time-based sub-planets) ---
-- Each rises at PartFraction of the 1/8 arc part ruled (per Table 10) by
-- RisesInPlanetId; the Ascendant at that instant is the sub-planet longitude.
--   instant = arcStart + (arcEnd - arcStart) * (partIndex + PartFraction) / DivisionCount
IF OBJECT_ID('dbo.tbl_Rule_SubPlanetTime', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SubPlanetTime (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SubPlanetTime PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SubPlanetTime_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SubPlanetId          TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SubPlanetTime_SubPlanet FOREIGN KEY REFERENCES dbo.tbl_Dim_SubPlanets (Id),
        RisesInPlanetId      TINYINT      NOT NULL          -- the graha whose 1/8 part this sub-planet rises in
                                 CONSTRAINT FK_Rule_SubPlanetTime_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        PartFraction         DECIMAL(3,2) NOT NULL,         -- 0.00 = start of that part, 0.50 = middle
        DivisionCount        TINYINT      NOT NULL CONSTRAINT DF_Rule_SubPlanetTime_Div DEFAULT 8,
        MethodCode           VARCHAR(30)  NULL,             -- 'EIGHTH_PART_RULER'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SubPlanetTime_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SubPlanetTime_Frac CHECK (PartFraction >= 0 AND PartFraction < 1),
        CONSTRAINT CK_Rule_SubPlanetTime_Div  CHECK (DivisionCount > 0),
        CONSTRAINT CK_Rule_SubPlanetTime_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SubPlanetTime_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SubPlanetTime UNIQUE (RuleSetId, SubPlanetId)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetTime)
    INSERT dbo.tbl_Rule_SubPlanetTime
        (RuleSetId, SubPlanetId, RisesInPlanetId, PartFraction, DivisionCount, MethodCode, CalculationNarrative, SourceRefCode, IsActive)
    SELECT 1, sp.Id, p.Id, CONVERT(DECIMAL(3,2), v.PartFraction), 8, 'EIGHTH_PART_RULER',
           v.Narrative, 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        ('KAALA',        'Sun',     0.50, N'Kaala rises at the middle of the Sun-ruled 1/8 part.'),
        ('MRITYU',       'Mars',    0.50, N'Mrityu rises at the middle of the Mars-ruled 1/8 part.'),
        ('ARDHAPRAHARA', 'Mercury', 0.50, N'Ardhaprahara (PVR: "Artha Praharaka") rises at the middle of the Mercury-ruled 1/8 part.'),
        ('YAMAGHANTAKA', 'Jupiter', 0.50, N'Yamaghantaka rises at the middle of the Jupiter-ruled 1/8 part.'),
        ('GULIKA',       'Saturn',  0.50, N'PVR text: Gulika rises at the MIDDLE of Saturn''s 1/8 part. NOTE: the shipped UpagrahaCalculator.cs follows the JHora convention and puts Gulika at the START of Saturn''s part (and Maandi at the middle) - the two are swapped relative to this row. verify-jaimini is pinned to the shipped output.'),
        ('MAANDI',       'Saturn',  0.00, N'PVR text: Maandi rises at the BEGINNING of Saturn''s 1/8 part. NOTE: the shipped UpagrahaCalculator.cs puts Maandi at the middle - swapped relative to this row (see Gulika).')
    ) v (SubPlanetCode, PlanetName, PartFraction, Narrative)
    JOIN dbo.tbl_Dim_SubPlanets sp ON sp.SubPlanetCode = v.SubPlanetCode
    JOIN dbo.tbl_Planets        p  ON p.PlanetName     = v.PlanetName;
GO

-- --- Batch 5: tbl_Rule_Catalog registration ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SubPlanetSunLongitude')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SubPlanetSunLongitude', 'SUBPLANET', 'SUN_LONGITUDE_CHAIN',
            'Sun-longitude-derived sub-planets (Dhuma, Vyatipata, Parivesha, Indrachapa, Upaketu): an ordered ADD / COMPLEMENT_360 chain off the Sun''s nirayana longitude. Reference data - engine not yet built.',
            '27_add_subplanet_rule_layer.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SubPlanetPartRuler')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SubPlanetPartRuler', 'SUBPLANET', 'PART_RULER_LOOKUP',
            'PVR "Table 10": the ruling graha (or none) of each of the 8 equal parts of the day / night arc, per weekday. Feeds the EIGHTH_PART_RULER method for the time-based sub-planets; also the ruler sequence for Gulika/Maandi.',
            '27_add_subplanet_rule_layer.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SubPlanetTime')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SubPlanetTime', 'SUBPLANET', 'EIGHTH_PART_RULER',
            'Time-based sub-planets (Kaala, Mrityu, Ardhaprahara, Yamaghantaka, Gulika, Maandi): each rises at PartFraction (0 = start, 0.5 = middle) of the 1/8 arc part ruled by a specific graha; the rising Ascendant at that instant is the longitude. Reference data - only Gulika/Maandi are built in C# (and with start/middle swapped vs this text per JHora).',
            '27_add_subplanet_rule_layer.sql');
GO

-- --- Batch 6: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '27_add_subplanet_rule_layer.sql',
       'tbl_Dim_SubPlanets(11) + tbl_Rule_SubPlanetSunLongitude(5) + tbl_Rule_SubPlanetPartRuler(112, PVR Table 10) + tbl_Rule_SubPlanetTime(6); 3 catalog rows. D1 reference data, engines not built.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '27_add_subplanet_rule_layer.sql');
GO

DECLARE @master INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SubPlanets);
DECLARE @sun    INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetSunLongitude);
DECLARE @ruler  INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetPartRuler);
DECLARE @time   INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetTime);
DECLARE @sunmiss INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SubPlanets sp
    WHERE sp.CalculationType = 'SUN_LONGITUDE'
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetSunLongitude r WHERE r.SubPlanetId = sp.Id));
DECLARE @timemiss INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SubPlanets sp
    WHERE sp.CalculationType = 'DAY_NIGHT_TIME'
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetTime r WHERE r.SubPlanetId = sp.Id));
-- Table 10 shape: exactly one lord-less part per (DayNight, Weekday), and it is
-- the part immediately after Saturn's.
DECLARE @lordless INT = (SELECT COUNT(*) FROM (
    SELECT DayNight, Weekday FROM dbo.tbl_Rule_SubPlanetPartRuler WHERE RulingPlanetId IS NULL
    GROUP BY DayNight, Weekday HAVING COUNT(*) <> 1) z);
DECLARE @notafterSat INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetPartRuler g
    WHERE g.RulingPlanetId IS NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanetPartRuler s
                      JOIN dbo.tbl_Planets p ON p.Id = s.RulingPlanetId AND p.PlanetName = 'Saturn'
                      WHERE s.DayNight = g.DayNight AND s.Weekday = g.Weekday AND s.PartNumber = g.PartNumber - 1));
DECLARE @badsrc INT = (
    (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetSunLongitude WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED')
  + (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetPartRuler     WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED')
  + (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanetTime          WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED'));
PRINT '27 applied: ' + CAST(@master AS VARCHAR(10)) + ' master (expect 11), '
    + CAST(@sun AS VARCHAR(10)) + ' sun-chain (expect 5), '
    + CAST(@ruler AS VARCHAR(10)) + ' part-ruler (expect 112), '
    + CAST(@time AS VARCHAR(10)) + ' time (expect 6), '
    + CAST(@sunmiss AS VARCHAR(10)) + ' SUN_LONGITUDE points w/o a chain (expect 0), '
    + CAST(@timemiss AS VARCHAR(10)) + ' DAY_NIGHT_TIME points w/o a time row (expect 0), '
    + CAST(@lordless AS VARCHAR(10)) + ' (DayNight,Weekday) w/o exactly one lord-less part (expect 0), '
    + CAST(@notafterSat AS VARCHAR(10)) + ' lord-less parts not right after Saturn (expect 0), '
    + CAST(@badsrc AS VARCHAR(10)) + ' seeded rows not SRC_PVR_INTEGRATED (expect 0).';
GO
