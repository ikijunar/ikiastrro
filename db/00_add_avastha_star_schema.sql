-- =====================================================================
-- Avastha layer, slice 1 (Baaladi + Jagradadi) — star-schema tables.
--
--   tbl_Dim_AvasthaState      — vocabulary of avastha states (Dim)
--   tbl_Rule_BaaladiState     — within-sign degree bands + effect fraction (Rule, RuleSetId-scoped)
--   tbl_Rule_JagradadiState   — DignityStatus -> waking-state map        (Rule, RuleSetId-scoped)
--   tbl_Fact_PlanetAvastha    — computed states per planet per chart     (Fact)
--
-- Idempotent (IF NOT EXISTS guards). Run against an existing [ikiastrro] DB, then:
--   dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails
-- A brand-new machine building from db/ikiastrro.sql already gets these.
-- =====================================================================
USE [ikiastrro];
GO

-- 1. tbl_Dim_AvasthaState -------------------------------------------------
IF OBJECT_ID('dbo.tbl_Dim_AvasthaState', 'U') IS NULL
CREATE TABLE dbo.tbl_Dim_AvasthaState (
    Id            TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Dim_AvasthaState PRIMARY KEY,
    AvasthaSystem VARCHAR(12)  NOT NULL,
    StateName     VARCHAR(20)  NOT NULL,
    SequenceOrder TINYINT      NOT NULL,
    Meaning       VARCHAR(200) NULL,
    CONSTRAINT UQ_Dim_AvasthaState UNIQUE (AvasthaSystem, StateName)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_AvasthaState)
INSERT dbo.tbl_Dim_AvasthaState (AvasthaSystem, StateName, SequenceOrder, Meaning) VALUES
    ('Baaladi',   'Baala',    1, 'Infant — quarter effect'),
    ('Baaladi',   'Kumara',   2, 'Child — half effect'),
    ('Baaladi',   'Yuva',     3, 'Youth — full effect'),
    ('Baaladi',   'Vriddha',  4, 'Old — feeble effect'),
    ('Baaladi',   'Mrita',    5, 'Dead — no effect'),
    ('Jagradadi', 'Jagrat',   1, 'Awake — full result (own sign / exaltation / moolatrikona)'),
    ('Jagradadi', 'Swapna',   2, 'Dreaming — middling result (friendly / neutral sign)'),
    ('Jagradadi', 'Sushupti', 3, 'Sleeping — weak result (enemy sign / debilitation)');
GO

-- 2. tbl_Rule_BaaladiState ---------------------------------------------------
IF OBJECT_ID('dbo.tbl_Rule_BaaladiState', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_BaaladiState (
    Id                 TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rule_BaaladiState PRIMARY KEY,
    RuleSetId          TINYINT      NOT NULL CONSTRAINT FK_Rule_BaaladiState_RuleSet   FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    AvasthaStateId     TINYINT      NOT NULL CONSTRAINT FK_Rule_BaaladiState_State     FOREIGN KEY REFERENCES dbo.tbl_Dim_AvasthaState (Id),
    OddSignFromDegree  DECIMAL(4,1) NOT NULL,
    OddSignToDegree    DECIMAL(4,1) NOT NULL,
    EvenSignFromDegree DECIMAL(4,1) NOT NULL,
    EvenSignToDegree   DECIMAL(4,1) NOT NULL,
    EffectFraction     DECIMAL(4,3) NOT NULL,
    CONSTRAINT UQ_Rule_BaaladiState UNIQUE (RuleSetId, AvasthaStateId)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_BaaladiState)
INSERT dbo.tbl_Rule_BaaladiState (RuleSetId, AvasthaStateId, OddSignFromDegree, OddSignToDegree, EvenSignFromDegree, EvenSignToDegree, EffectFraction)
SELECT 1, s.Id, v.OddFrom, v.OddTo, v.EvenFrom, v.EvenTo, v.Frac
FROM (VALUES
    ('Baala',    0.0,  6.0, 24.0, 30.0, 0.250),
    ('Kumara',   6.0, 12.0, 18.0, 24.0, 0.500),
    ('Yuva',    12.0, 18.0, 12.0, 18.0, 1.000),
    ('Vriddha', 18.0, 24.0,  6.0, 12.0, 0.125),
    ('Mrita',   24.0, 30.0,  0.0,  6.0, 0.000)
) v (StateName, OddFrom, OddTo, EvenFrom, EvenTo, Frac)
JOIN dbo.tbl_Dim_AvasthaState s ON s.AvasthaSystem = 'Baaladi' AND s.StateName = v.StateName;
GO

-- 3. tbl_Rule_JagradadiState ----------------------------------------------
IF OBJECT_ID('dbo.tbl_Rule_JagradadiState', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_JagradadiState (
    Id             TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rule_JagradadiState PRIMARY KEY,
    RuleSetId      TINYINT     NOT NULL CONSTRAINT FK_Rule_JagradadiState_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    DignityStatus  VARCHAR(20) NOT NULL,
    AvasthaStateId TINYINT     NOT NULL CONSTRAINT FK_Rule_JagradadiState_State   FOREIGN KEY REFERENCES dbo.tbl_Dim_AvasthaState (Id),
    CONSTRAINT UQ_Rule_JagradadiState UNIQUE (RuleSetId, DignityStatus)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_JagradadiState)
INSERT dbo.tbl_Rule_JagradadiState (RuleSetId, DignityStatus, AvasthaStateId)
SELECT 1, v.DignityStatus, s.Id
FROM (VALUES
    ('Exalted',      'Jagrat'),
    ('Moolatrikona', 'Jagrat'),
    ('Own Sign',     'Jagrat'),
    ('Great Friend', 'Swapna'),
    ('Friend',       'Swapna'),
    ('Neutral',      'Swapna'),
    ('Enemy',        'Sushupti'),
    ('Great Enemy',  'Sushupti'),
    ('Debilitated',  'Sushupti')
) v (DignityStatus, StateName)
JOIN dbo.tbl_Dim_AvasthaState s ON s.AvasthaSystem = 'Jagradadi' AND s.StateName = v.StateName;
GO

-- 4. tbl_Fact_PlanetAvastha ----------------------------------------------
IF OBJECT_ID('dbo.tbl_Fact_PlanetAvastha', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_PlanetAvastha (
        Id                    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fact_PlanetAvastha PRIMARY KEY,
        ChartResultId         INT           NOT NULL CONSTRAINT FK_Fact_PlanetAvastha_ChartResult FOREIGN KEY REFERENCES dbo.tbl_ChartResults (Id),
        BirthDetailId         INT           NOT NULL CONSTRAINT FK_Fact_PlanetAvastha_BirthDetail FOREIGN KEY REFERENCES dbo.tbl_BirthDetails (Id),
        ChartType             NVARCHAR(50)  NOT NULL,
        Planet                VARCHAR(20)   NOT NULL,
        RuleSetId             TINYINT       NOT NULL CONSTRAINT FK_Fact_PlanetAvastha_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        BaaladiStateId        TINYINT       NULL CONSTRAINT FK_Fact_PlanetAvastha_Baaladi   FOREIGN KEY REFERENCES dbo.tbl_Dim_AvasthaState (Id),
        BaaladiEffectFraction DECIMAL(4,3)  NULL,
        JagradadiStateId      TINYINT       NULL CONSTRAINT FK_Fact_PlanetAvastha_Jagradadi FOREIGN KEY REFERENCES dbo.tbl_Dim_AvasthaState (Id),
        ComputedAt            DATETIME2(7)  NOT NULL CONSTRAINT DF_Fact_PlanetAvastha_ComputedAt DEFAULT sysutcdatetime(),
        CONSTRAINT UQ_Fact_PlanetAvastha UNIQUE (ChartResultId, Planet)
    );
    CREATE NONCLUSTERED INDEX IX_Fact_PlanetAvastha_BirthDetailId ON dbo.tbl_Fact_PlanetAvastha (BirthDetailId);
    CREATE NONCLUSTERED INDEX IX_Fact_PlanetAvastha_ChartResultId ON dbo.tbl_Fact_PlanetAvastha (ChartResultId);
END
GO

-- 5. vw_Chart_Consolidated — surface BaaladiState / BaaladiEffectFraction / JagradadiState -----
IF OBJECT_ID('dbo.vw_Chart_Consolidated', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Chart_Consolidated;
GO
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[vw_Chart_Consolidated] AS
SELECT
    bd.Id                       AS BirthDetailId,
    bd.Name,
    bd.DateOfBirth,
    bd.TimeOfBirth,
    bd.PlaceCity,
    bd.PlaceCountry,
    cr.Id                       AS ChartResultId,
    cr.ChartType,
    cr.Ayanamsha,
    cr.HouseSystem,
    cr.EngineVersion,
    kd.Planet,
    kd.NirayanaLongitudeDegrees,
    kd.EclipticLatitudeDegrees,
    kd.SpeedLongitudeDegPerDay,
    kd.IsRetrograde,
    kd.Sign,
    kd.DegreesInSignDecimal,
    kd.DegreesInSignDisplay,
    kd.Nakshatra,
    kd.NakshatraPada,
    kd.NakshatraLordPlanet,
    kd.IsCombust,
    kd.DistanceFromSunDegrees,
    kd.CombustionOrbUsedDegrees,
    kd.HouseNumberFromLagna,
    kd.HouseNumberFromSun,
    kd.HouseNumberFromMoon,
    kd.OwnSigns,
    kd.ExaltationSign,
    kd.DebilitationSign,
    kd.MoolatrikonaSign,
    kd.MoolatrikonaRange,
    kd.SignLordPlanet,
    kd.DignityStatus,
    baaladi.StateName            AS BaaladiState,
    av.BaaladiEffectFraction,
    jagradadi.StateName          AS JagradadiState,
    RulesHouses.HouseList        AS RulesHouseNumbers,
    Conjunct.PlanetList           AS ConjunctWith,
    AspectsCast.TargetList        AS Aspects,
    kd.AspectingPlanets          AS AspectedBy,
    kd.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = kd.BirthDetailId
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
LEFT JOIN dbo.tbl_Fact_PlanetAvastha av ON av.ChartResultId = kd.ChartResultId AND av.Planet = kd.Planet
LEFT JOIN dbo.tbl_Dim_AvasthaState  baaladi   ON baaladi.Id   = av.BaaladiStateId
LEFT JOIN dbo.tbl_Dim_AvasthaState  jagradadi ON jagradadi.Id = av.JagradadiStateId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), '','') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanet = kd.Planet
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, '', '') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1 = kd.Planet
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2 = kd.Planet
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, '' ('', AspectType, '')''), '', '') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanet = kd.Planet
) AspectsCast;';
GO

PRINT 'Avastha slice 1 tables ready. Now run:  dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails';
GO
