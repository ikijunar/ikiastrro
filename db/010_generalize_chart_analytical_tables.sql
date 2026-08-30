-- vedic_horo_gen: generalize the 4 D1-only analytical tables so every chart type (D1, D9, and any
-- future divisional chart — D2, D10, D12, ...) shares them, discriminated by ChartResultId/ChartType.
--
-- This is a live-DB upgrade migration, not folded into the base DDL files (003/006) the way earlier
-- renames were: those tables already hold real generated chart data, and this rename touches 4 tables
-- + a view + every C# model/repository referencing them, so it's kept as its own explicit, reviewable
-- step rather than rewriting already-battle-tested historical DDL.
--
-- Column-shape rationale (see ChartKeyDetail.cs / ChartConjunction.cs for the full explanation):
--   - NirayanaLongitudeDegrees stays NOT NULL for every chart type — it's the same real ecliptic
--     longitude regardless of which divisional chart is reading it.
--   - DegreesInSignDisplay/DegreesInSignDecimal become nullable — meaningful only for D1 (a varga
--     sign is a discrete bucket, not a continuous 30° span).
--   - Conjunctions.DegreeSeparation becomes nullable — meaningful only for D1 (two grahas "conjunct"
--     in the same D9 sign can sit far apart in real longitude; that number wouldn't mean "tightness").
USE vedic_horo_gen;
GO

-- 1) Rename the 4 tables
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_keydetails')
   AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_KeyDetails')
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_keydetails', 'tbl_Chart_KeyDetails';
END
GO
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_HouseLords')
   AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_HouseLords')
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_HouseLords', 'tbl_Chart_HouseLords';
END
GO
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_Conjunctions')
   AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_Conjunctions')
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_Conjunctions', 'tbl_Chart_Conjunctions';
END
GO
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_Aspects')
   AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_Aspects')
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_Aspects', 'tbl_Chart_Aspects';
END
GO

-- 2) Rename their indexes to match (cosmetic — SQL Server doesn't require this, matches project's
--    naming-consistency practice elsewhere, e.g. the tbl_/vw_ conventions in STANDARDS.md)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_D1KeyDetails_ChartResultId' AND object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails'))
    EXEC sp_rename 'dbo.tbl_Chart_KeyDetails.IX_D1KeyDetails_ChartResultId', 'IX_Chart_KeyDetails_ChartResultId', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_D1KeyDetails_BirthDetailId_Planet' AND object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails'))
    EXEC sp_rename 'dbo.tbl_Chart_KeyDetails.IX_D1KeyDetails_BirthDetailId_Planet', 'IX_Chart_KeyDetails_BirthDetailId_Planet', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_D1HouseLords_ChartResultId' AND object_id = OBJECT_ID('dbo.tbl_Chart_HouseLords'))
    EXEC sp_rename 'dbo.tbl_Chart_HouseLords.IX_D1HouseLords_ChartResultId', 'IX_Chart_HouseLords_ChartResultId', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_D1HouseLords_ChartResultId_HouseNumber' AND object_id = OBJECT_ID('dbo.tbl_Chart_HouseLords'))
    EXEC sp_rename 'dbo.tbl_Chart_HouseLords.UX_D1HouseLords_ChartResultId_HouseNumber', 'UX_Chart_HouseLords_ChartResultId_HouseNumber', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_D1Conjunctions_ChartResultId' AND object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions'))
    EXEC sp_rename 'dbo.tbl_Chart_Conjunctions.IX_D1Conjunctions_ChartResultId', 'IX_Chart_Conjunctions_ChartResultId', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_D1Aspects_ChartResultId' AND object_id = OBJECT_ID('dbo.tbl_Chart_Aspects'))
    EXEC sp_rename 'dbo.tbl_Chart_Aspects.IX_D1Aspects_ChartResultId', 'IX_Chart_Aspects_ChartResultId', 'INDEX';
GO

-- 3) Add ChartType (denormalized from ChartResults, same rationale as the existing Name column —
--    read standalone without a join). Existing rows are all D1, hence the default backfill.
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'ChartType') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD ChartType NVARCHAR(50) NOT NULL CONSTRAINT DF_ChartKeyDetails_ChartType DEFAULT 'D1';
GO
IF COL_LENGTH('dbo.tbl_Chart_HouseLords', 'ChartType') IS NULL
    ALTER TABLE dbo.tbl_Chart_HouseLords ADD ChartType NVARCHAR(50) NOT NULL CONSTRAINT DF_ChartHouseLords_ChartType DEFAULT 'D1';
GO
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions', 'ChartType') IS NULL
    ALTER TABLE dbo.tbl_Chart_Conjunctions ADD ChartType NVARCHAR(50) NOT NULL CONSTRAINT DF_ChartConjunctions_ChartType DEFAULT 'D1';
GO
IF COL_LENGTH('dbo.tbl_Chart_Aspects', 'ChartType') IS NULL
    ALTER TABLE dbo.tbl_Chart_Aspects ADD ChartType NVARCHAR(50) NOT NULL CONSTRAINT DF_ChartAspects_ChartType DEFAULT 'D1';
GO

-- 4) DegreesInSignDisplay/DegreesInSignDecimal: D1-only concepts going forward, make nullable
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails') AND name = 'DegreesInSignDisplay' AND is_nullable = 0)
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN DegreesInSignDisplay VARCHAR(20) NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails') AND name = 'DegreesInSignDecimal' AND is_nullable = 0)
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN DegreesInSignDecimal DECIMAL(7,4) NULL;
GO

-- 5) DegreeSeparation: D1-only concept going forward (see rationale above), make nullable
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions') AND name = 'DegreeSeparation' AND is_nullable = 0)
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN DegreeSeparation DECIMAL(7,4) NULL;
GO

-- 6) Recreate the consolidated view under its new name, against the new table names, with ChartType exposed
DROP VIEW IF EXISTS dbo.vw_D1Chart_Consolidated;
GO
DROP VIEW IF EXISTS dbo.vw_Chart_Consolidated;
GO
CREATE VIEW dbo.vw_Chart_Consolidated AS
SELECT
    bd.Id                       AS BirthDetailId,
    bd.Name,
    bd.DateOfBirth,
    bd.TimeOfBirth,
    bd.CorrectedTimeOfBirth,
    bd.PlaceCity,
    bd.PlaceCountry,
    cr.Id                       AS ChartResultId,
    cr.ChartType,
    cr.Ayanamsha,
    cr.HouseSystem,
    cr.EngineVersion,
    kd.Planet,
    kd.Sign,
    kd.DegreesInSignDisplay,
    kd.DegreesInSignDecimal,
    kd.NirayanaLongitudeDegrees,
    kd.Nakshatra,
    kd.NakshatraPada,
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
    RulesHouses.HouseList        AS RulesHouseNumbers,
    Conjunct.PlanetList           AS ConjunctWith,
    AspectsCast.TargetList        AS Aspects,
    kd.AspectingPlanets          AS AspectedBy,
    kd.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = kd.BirthDetailId
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), ',') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanet = kd.Planet
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, ', ') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1 = kd.Planet
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2 = kd.Planet
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, ' (', AspectType, ')'), ', ') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanet = kd.Planet
) AspectsCast;
GO
