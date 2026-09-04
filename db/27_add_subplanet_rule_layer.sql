-- =====================================================================
-- 27 - Sub-planet (upagraha) reference layer: master + calculation rules.
--
-- New tables use the English "Planet / Sub Planet" vocabulary (rammyps,
-- 2026-09-04). The existing graha/upagraha table names are NOT renamed.
--
--   tbl_SubPlanet                    - the 11-row master. Two calculation
--                                     families: SUN_LONGITUDE (Dhuma,
--                                     Vyatipata, Parivesha, Indrachapa,
--                                     Upaketu) and DAY_NIGHT_TIME (Kaala,
--                                     Mrityu, Ardhaprahara, Yamaghantaka,
--                                     Gulika, Maandi). AssociatedPlanetId
--                                     is the interpretive analogy planet,
--                                     NOT identity - a sub-planet is never
--                                     one of the nine grahas.
--   tbl_Rule_SubPlanet_SunLongitude - the 5-step longitude chain off the
--                                     Sun. Seeded (calc not built in C#).
--   tbl_Rule_SubPlanet_Time         - per weekday / day-night rising-value
--                                     table for the four time-based points
--                                     whose calc is NOT built. Gulika and
--                                     Maandi already ship (the 8-part arc
--                                     in Core/Engines/Karakas/UpagrahaCalculator.cs)
--                                     so they get NO rows here yet; the
--                                     Method column + PartOffset are in
--                                     place for a later migration to fold
--                                     that method in without a schema change.
--
-- No varga projection here - D1 rule data only (deferred per rammyps).
-- RuleSetId 1 (Parashari-Classical), SourceRefCode SRC_PVR_INTEGRATED on
-- every seeded row (P.V.R. Narasimha Rao, Integrated Approach - the
-- classical Parashari formulas / rising-value tables).
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

-- --- Batch 1: tbl_SubPlanet (master) ---
IF OBJECT_ID('dbo.tbl_SubPlanet', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_SubPlanet (
        SubPlanetId        TINYINT       NOT NULL
                               CONSTRAINT PK_SubPlanet PRIMARY KEY,
        SubPlanetCode      VARCHAR(20)   NOT NULL
                               CONSTRAINT UQ_SubPlanet_Code UNIQUE,        -- DHUMA, VYATIPATA, ...
        SubPlanetName      VARCHAR(30)   NOT NULL
                               CONSTRAINT UQ_SubPlanet_Name UNIQUE,        -- app display name (== SpecialPointSeed.Code for Gulika/Maandi)
        EnglishMeaning     NVARCHAR(60)  NULL,                             -- literal gloss; NULL where there is no common translation
        CalculationType    VARCHAR(20)   NOT NULL,                         -- SUN_LONGITUDE | DAY_NIGHT_TIME
        AssociatedPlanetId TINYINT       NOT NULL
                               CONSTRAINT FK_SubPlanet_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        NaturalNature      VARCHAR(12)   NULL,                             -- Malefic | Benefic | Neutral | Mixed; NULL = source silent
        SortOrder          TINYINT       NOT NULL,
        IsActive           BIT           NOT NULL CONSTRAINT DF_SubPlanet_IsActive DEFAULT 1,
        Notes              NVARCHAR(400) NULL,
        CONSTRAINT CK_SubPlanet_CalcType CHECK (CalculationType IN ('SUN_LONGITUDE','DAY_NIGHT_TIME')),
        CONSTRAINT CK_SubPlanet_Nature   CHECK (NaturalNature IS NULL OR NaturalNature IN ('Malefic','Benefic','Neutral','Mixed'))
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SubPlanet)
    INSERT dbo.tbl_SubPlanet
        (SubPlanetId, SubPlanetCode, SubPlanetName, EnglishMeaning, CalculationType, AssociatedPlanetId, NaturalNature, SortOrder, Notes)
    SELECT v.SubPlanetId, v.SubPlanetCode, v.SubPlanetName, v.EnglishMeaning, v.CalculationType,
           p.Id, v.NaturalNature, v.SortOrder, v.Notes
    FROM (VALUES
        ( 1,'DHUMA',       'Dhuma',       N'Smoke',        'SUN_LONGITUDE', 'Sun',    'Malefic',  1, CONVERT(NVARCHAR(400),NULL)),
        ( 2,'VYATIPATA',   'Vyatipata',   N'Calamity',     'SUN_LONGITUDE', 'Sun',    'Malefic',  2, NULL),
        ( 3,'PARIVESHA',   'Parivesha',   N'Halo',         'SUN_LONGITUDE', 'Sun',    'Malefic',  3, N'Some traditions class Parivesha (halo) as benefic / neutral.'),
        ( 4,'INDRACHAPA',  'Indrachapa',  N'Rainbow',      'SUN_LONGITUDE', 'Sun',    'Malefic',  4, N'Some traditions class Indrachapa (rainbow) as benefic / neutral.'),
        ( 5,'UPAKETU',     'Upaketu',     N'Sub-Ketu',     'SUN_LONGITUDE', 'Sun',    'Malefic',  5, N'Chain identity: Upaketu + 30 deg = Sun.'),
        ( 6,'KAALA',       'Kaala',       N'Time',         'DAY_NIGHT_TIME','Sun',    'Malefic',  6, NULL),
        ( 7,'MRITYU',      'Mrityu',      N'Death',        'DAY_NIGHT_TIME','Mars',   'Malefic',  7, NULL),
        ( 8,'ARDHAPRAHARA','Ardhaprahara',N'Half-prahara', 'DAY_NIGHT_TIME','Mercury', NULL,      8, N'BPHS names Mercury''s day/night portion Ardhaprahara.'),
        ( 9,'YAMAGHANTAKA','Yamaghantaka',N'Yama''s bell', 'DAY_NIGHT_TIME','Jupiter', NULL,      9, N'BPHS names Jupiter''s day/night portion Yamaghantaka.'),
        (10,'GULIKA',      'Gulika',      NULL,            'DAY_NIGHT_TIME','Saturn', 'Malefic', 10, N'Ships today via the 8-part arc method in UpagrahaCalculator.cs (start of Saturn''s 1/8 part). No tbl_Rule_SubPlanet_Time row yet.'),
        (11,'MAANDI',      'Maandi',      NULL,            'DAY_NIGHT_TIME','Saturn', 'Malefic', 11, N'Ships today via the 8-part arc method (middle of Saturn''s 1/8 part). Often treated as the same Saturn upagraha as Gulika.')
    ) v (SubPlanetId, SubPlanetCode, SubPlanetName, EnglishMeaning, CalculationType, AssocPlanetName, NaturalNature, SortOrder, Notes)
    JOIN dbo.tbl_Planets p ON p.PlanetName = v.AssocPlanetName;
GO

-- --- Batch 2: tbl_Rule_SubPlanet_SunLongitude ---
IF OBJECT_ID('dbo.tbl_Rule_SubPlanet_SunLongitude', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SubPlanet_SunLongitude (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SubPlanet_SunLongitude PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SPSun_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SubPlanetId          TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SPSun_SubPlanet FOREIGN KEY REFERENCES dbo.tbl_SubPlanet (SubPlanetId),
        SequenceNo           TINYINT      NOT NULL,          -- evaluation order within the chain
        Operation            VARCHAR(16)  NOT NULL,          -- ADD | COMPLEMENT_360
        InputSubPlanetId     TINYINT      NULL               -- NULL => operate on the Sun's nirayana longitude
                                 CONSTRAINT FK_Rule_SPSun_Input FOREIGN KEY REFERENCES dbo.tbl_SubPlanet (SubPlanetId),
        OffsetDegrees        DECIMAL(9,6) NULL,              -- required for ADD, NULL for COMPLEMENT_360
        NormalizationMethod  VARCHAR(12)  NOT NULL CONSTRAINT DF_Rule_SPSun_Norm DEFAULT 'MOD_360',
        MethodCode           VARCHAR(30)  NULL,              -- 'SUN_LONGITUDE_CHAIN'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SPSun_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SPSun_Op    CHECK (Operation IN ('ADD','COMPLEMENT_360')),
        CONSTRAINT CK_Rule_SPSun_Off   CHECK ((Operation = 'ADD' AND OffsetDegrees IS NOT NULL)
                                           OR (Operation = 'COMPLEMENT_360' AND OffsetDegrees IS NULL)),
        CONSTRAINT CK_Rule_SPSun_Json  CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SPSun_Src   CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SPSun UNIQUE (RuleSetId, SubPlanetId, SequenceNo)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanet_SunLongitude)
    INSERT dbo.tbl_Rule_SubPlanet_SunLongitude
        (RuleSetId, SubPlanetId, SequenceNo, Operation, InputSubPlanetId, OffsetDegrees,
         NormalizationMethod, MethodCode, CalculationNarrative, SourceRefCode, IsActive)
    SELECT 1, sp.SubPlanetId, v.SequenceNo, v.Operation, inp.SubPlanetId,
           CONVERT(DECIMAL(9,6), v.OffsetDegrees), 'MOD_360', 'SUN_LONGITUDE_CHAIN',
           v.Narrative, 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        ('DHUMA',      1, 'ADD',            CONVERT(VARCHAR(20),NULL),  133.333333, N'Dhuma = Sun + 133 deg 20 min.'),
        ('VYATIPATA',  2, 'COMPLEMENT_360', 'DHUMA',                    CONVERT(DECIMAL(9,6),NULL), N'Vyatipata = 360 deg - Dhuma. Identity: Dhuma + Vyatipata = 360.'),
        ('PARIVESHA',  3, 'ADD',            'VYATIPATA',                180.000000, N'Parivesha = Vyatipata + 180 deg.'),
        ('INDRACHAPA', 4, 'COMPLEMENT_360', 'PARIVESHA',                CONVERT(DECIMAL(9,6),NULL), N'Indrachapa = 360 deg - Parivesha. Identity: Parivesha + Indrachapa = 360.'),
        ('UPAKETU',    5, 'ADD',            'INDRACHAPA',               16.666667,  N'Upaketu = Indrachapa + 16 deg 40 min. Validation identity: Upaketu + 30 deg = Sun.')
    ) v (SubPlanetCode, SequenceNo, Operation, InputCode, OffsetDegrees, Narrative)
    JOIN dbo.tbl_SubPlanet sp ON sp.SubPlanetCode = v.SubPlanetCode
    LEFT JOIN dbo.tbl_SubPlanet inp ON inp.SubPlanetCode = v.InputCode;
GO

-- --- Batch 3: tbl_Rule_SubPlanet_Time ---
IF OBJECT_ID('dbo.tbl_Rule_SubPlanet_Time', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SubPlanet_Time (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SubPlanet_Time PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SPTime_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SubPlanetId          TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SPTime_SubPlanet FOREIGN KEY REFERENCES dbo.tbl_SubPlanet (SubPlanetId),
        AssociatedPlanetId   TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SPTime_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        DayNight             VARCHAR(5)   NOT NULL,          -- DAY | NIGHT
        Weekday              TINYINT      NOT NULL,          -- 0 = Sunday ((int)DayOfWeek); the Vedic day opens at sunrise
        RisingValue          TINYINT      NULL,              -- RISING_VALUE method: parts of DivisionBase from the anchor
        DivisionBase         TINYINT      NOT NULL CONSTRAINT DF_Rule_SPTime_Div DEFAULT 32,
        Anchor               VARCHAR(8)   NOT NULL,          -- SUNRISE (day) | SUNSET (night)
        Method               VARCHAR(20)  NOT NULL CONSTRAINT DF_Rule_SPTime_Method DEFAULT 'RISING_VALUE',
        PartOffset           DECIMAL(3,2) NULL,              -- EIGHTH_PART_ARC only: 0.00 start / 0.50 mid of the ruler's 1/8 part
        MethodCode           VARCHAR(30)  NULL,
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SPTime_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SPTime_DN     CHECK (DayNight IN ('DAY','NIGHT')),
        CONSTRAINT CK_Rule_SPTime_WD     CHECK (Weekday BETWEEN 0 AND 6),
        CONSTRAINT CK_Rule_SPTime_Anchor CHECK (Anchor IN ('SUNRISE','SUNSET')),
        CONSTRAINT CK_Rule_SPTime_Div    CHECK (DivisionBase > 0),
        CONSTRAINT CK_Rule_SPTime_Method CHECK (Method IN ('RISING_VALUE','EIGHTH_PART_ARC')),
        CONSTRAINT CK_Rule_SPTime_Shape  CHECK ((Method = 'RISING_VALUE'    AND RisingValue IS NOT NULL AND PartOffset IS NULL)
                                             OR (Method = 'EIGHTH_PART_ARC' AND PartOffset IS NOT NULL)),
        CONSTRAINT CK_Rule_SPTime_Rise   CHECK (RisingValue IS NULL OR RisingValue BETWEEN 0 AND 32),
        CONSTRAINT CK_Rule_SPTime_Json   CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SPTime_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SPTime UNIQUE (RuleSetId, SubPlanetId, DayNight, Weekday, Method)
    );
    CREATE NONCLUSTERED INDEX IX_Rule_SPTime_Lookup
        ON dbo.tbl_Rule_SubPlanet_Time (RuleSetId, SubPlanetId, DayNight, Weekday)
        INCLUDE (RisingValue, DivisionBase, Anchor, Method);
END
GO
-- Rising-value table (PVR / classical Parashari): 1/32 parts of the arc from
-- the anchor. Instant  =  Anchor + ArcDuration * RisingValue / DivisionBase,
--   ArcDuration = Sunset - Sunrise (DAY, anchor SUNRISE)
--               = NextSunrise - Sunset (NIGHT, anchor SUNSET)
-- Sub-planet longitude = the sidereal Ascendant rising at that instant.
-- Only the four points whose calc is NOT yet built are seeded here; Gulika
-- and Maandi ship via the 8-part arc method and are intentionally omitted.
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanet_Time)
    INSERT dbo.tbl_Rule_SubPlanet_Time
        (RuleSetId, SubPlanetId, AssociatedPlanetId, DayNight, Weekday, RisingValue,
         DivisionBase, Anchor, Method, MethodCode, SourceRefCode, IsActive)
    SELECT 1, sp.SubPlanetId, sp.AssociatedPlanetId, r.DayNight, w.Weekday,
           CONVERT(TINYINT,
               CASE w.Weekday WHEN 0 THEN r.V0 WHEN 1 THEN r.V1 WHEN 2 THEN r.V2 WHEN 3 THEN r.V3
                              WHEN 4 THEN r.V4 WHEN 5 THEN r.V5 ELSE r.V6 END),
           32,
           CASE r.DayNight WHEN 'DAY' THEN 'SUNRISE' ELSE 'SUNSET' END,
           'RISING_VALUE', 'RISING_VALUE', 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        --  Code            DN       W0  W1  W2  W3  W4  W5  W6
        ('KAALA',        'DAY',    2, 30, 26, 22, 18, 14, 10),
        ('KAALA',        'NIGHT', 14, 10,  6,  2, 30, 26, 22),
        ('MRITYU',       'DAY',   10,  6,  2, 30, 26, 22, 18),
        ('MRITYU',       'NIGHT', 22, 18, 14, 10,  6,  2, 30),
        ('ARDHAPRAHARA', 'DAY',   14, 10,  6,  2, 30, 26, 22),
        ('ARDHAPRAHARA', 'NIGHT', 26, 22, 18, 14, 10,  6,  2),
        ('YAMAGHANTAKA', 'DAY',   18, 14, 10,  6,  2, 30, 26),
        ('YAMAGHANTAKA', 'NIGHT',  2, 30, 26, 22, 18, 14, 10)
    ) r (SubPlanetCode, DayNight, V0, V1, V2, V3, V4, V5, V6)
    JOIN dbo.tbl_SubPlanet sp ON sp.SubPlanetCode = r.SubPlanetCode
    CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6)) w (Weekday);
GO

-- --- Batch 4: tbl_Rule_Catalog registration ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SubPlanet_SunLongitude')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SubPlanet_SunLongitude', 'SUBPLANET', 'SUN_LONGITUDE_CHAIN',
            'Sun-longitude-derived sub-planets (Dhuma, Vyatipata, Parivesha, Indrachapa, Upaketu): an ordered ADD / COMPLEMENT_360 chain off the Sun''s nirayana longitude. Reference data - engine not yet built.',
            '27_add_subplanet_rule_layer.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SubPlanet_Time')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SubPlanet_Time', 'SUBPLANET', 'RISING_VALUE,EIGHTH_PART_ARC',
            'Time-based sub-planets (Kaala, Mrityu, Ardhaprahara, Yamaghantaka; Gulika/Maandi later): rising-value / 32 of the day or night arc from sunrise/sunset -> Ascendant at that instant. Reference data for the four unbuilt points; Gulika/Maandi ship via the 8-part arc in UpagrahaCalculator.cs.',
            '27_add_subplanet_rule_layer.sql');
GO

-- --- Batch 5: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '27_add_subplanet_rule_layer.sql',
       'tbl_SubPlanet (11) + tbl_Rule_SubPlanet_SunLongitude (5) + tbl_Rule_SubPlanet_Time (56); 2 tbl_Rule_Catalog rows. D1 reference data, engines not built.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '27_add_subplanet_rule_layer.sql');
GO

DECLARE @master INT = (SELECT COUNT(*) FROM dbo.tbl_SubPlanet);
DECLARE @sun    INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanet_SunLongitude);
DECLARE @time   INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanet_Time);
DECLARE @sunmiss INT = (SELECT COUNT(*) FROM dbo.tbl_SubPlanet sp
    WHERE sp.CalculationType = 'SUN_LONGITUDE'
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SubPlanet_SunLongitude r WHERE r.SubPlanetId = sp.SubPlanetId));
DECLARE @timebad INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanet_Time r
    JOIN dbo.tbl_SubPlanet sp ON sp.SubPlanetId = r.SubPlanetId
    WHERE sp.CalculationType <> 'DAY_NIGHT_TIME');
DECLARE @timecov INT = (SELECT COUNT(*) FROM dbo.tbl_SubPlanet sp
    WHERE sp.CalculationType = 'DAY_NIGHT_TIME' AND sp.SubPlanetCode NOT IN ('GULIKA','MAANDI')
      AND (SELECT COUNT(*) FROM dbo.tbl_Rule_SubPlanet_Time r WHERE r.SubPlanetId = sp.SubPlanetId) <> 14);
PRINT '27 applied: ' + CAST(@master AS VARCHAR(10)) + ' master rows (expect 11), '
    + CAST(@sun AS VARCHAR(10)) + ' sun-chain rows (expect 5), '
    + CAST(@time AS VARCHAR(10)) + ' time rows (expect 56), '
    + CAST(@sunmiss AS VARCHAR(10)) + ' SUN_LONGITUDE points with no chain (expect 0), '
    + CAST(@timebad AS VARCHAR(10)) + ' time rows on a non-time point (expect 0), '
    + CAST(@timecov AS VARCHAR(10)) + ' unbuilt time points not covering 14 rows (expect 0).';
GO
