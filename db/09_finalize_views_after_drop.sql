-- =====================================================================
-- 09 — Finalize the chart views/TVFs after migration 08 dropped
-- BirthDetailId / ChartType / ComputedAt from the child fact tables.
-- Person + chart type + compute-time now come only from tbl_ChartResults.
-- Run immediately after 08 (08 leaves these four objects broken).
--
-- All four were created under ANSI_NULLS ON / QUOTED_IDENTIFIER OFF and
-- are rebuilt here via the same EXEC dbo.sp_executesql @statement = N'...'
-- idiom migrations 07 and the baseline use (single quotes inside doubled).
-- Idempotent (DROP ... IF EXISTS then CREATE).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/09_finalize_views_after_drop.sql
-- =====================================================================
USE [ikiastrro];
GO

-- ---------------------------------------------------------------------
-- 1. vw_Chart_Consolidated
--    JOIN tbl_BirthDetails  ON bd.Id = kd.BirthDetailId  -> ON bd.Id = cr.BirthDetailId
--    SELECT kd.ComputedAt (child column, dropped)         -> cr.ComputedAt (parent)
--    Everything else already *Id-joined by migration 07.
-- ---------------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
DROP VIEW IF EXISTS dbo.vw_Chart_Consolidated
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
    cr.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = cr.BirthDetailId
LEFT JOIN dbo.tbl_Fact_PlanetAvastha av ON av.ChartResultId = kd.ChartResultId AND av.PlanetId = kd.PlanetId
LEFT JOIN dbo.tbl_Dim_AvasthaState  baaladi   ON baaladi.Id   = av.BaaladiStateId
LEFT JOIN dbo.tbl_Dim_AvasthaState  jagradadi ON jagradadi.Id = av.JagradadiStateId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), '','') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanetId = kd.PlanetId
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, '', '') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1Id = kd.PlanetId
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2Id = kd.PlanetId
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, '' ('', AspectType, '')''), '', '') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanetId = kd.PlanetId
) AspectsCast;
'
GO

-- ---------------------------------------------------------------------
-- 2. vw_Chart_HouseNakshatraSpan
--    hl.BirthDetailId -> cr.BirthDetailId ; hl.ChartType -> ct.Code
--    (new joins: tbl_ChartResults cr, tbl_Dim_ChartType ct)
-- ---------------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
DROP VIEW IF EXISTS dbo.vw_Chart_HouseNakshatraSpan
GO
EXEC dbo.sp_executesql @statement = N'
CREATE VIEW [dbo].[vw_Chart_HouseNakshatraSpan] AS
SELECT
    hl.ChartResultId,
    cr.BirthDetailId,
    ct.Code                    AS ChartType,
    hl.HouseNumber,
    hl.HouseSign,
    sa.Id                      AS HouseSignId,
    hl.LordPlanet,
    n.Id                       AS NakshatraId,
    n.NakshatraName,
    p.PadaNumber,
    p.StartDegree              AS PadaStartDegree,
    p.EndDegree                AS PadaEndDegree,
    lord.PlanetName            AS NakshatraLordName,
    nav.SignName               AS NavamsaSignName
FROM dbo.tbl_Chart_HouseLords hl
JOIN dbo.tbl_ChartResults    cr   ON cr.Id = hl.ChartResultId
JOIN dbo.tbl_Dim_ChartType   ct   ON ct.Id = cr.ChartTypeId
JOIN dbo.tbl_SignAttributes  sa   ON sa.Id = hl.HouseSignId
JOIN dbo.tbl_NakshatraPadas  p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                                 AND p.StartDegree <  sa.Id * 30.0
JOIN dbo.tbl_Nakshatras      n    ON n.Id = p.NakshatraId
JOIN dbo.tbl_Planets         lord ON lord.Id = n.RulingPlanetId
JOIN dbo.tbl_SignAttributes  nav  ON nav.Id = p.NavamsaSignId;
'
GO

-- ---------------------------------------------------------------------
-- 3. vw_Chart_DashaTimeline
--    JOIN tbl_BirthDetails ON bd.Id = dp.BirthDetailId -> ON bd.Id = cr.BirthDetailId
--    (cr joined first now)
-- ---------------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
DROP VIEW IF EXISTS dbo.vw_Chart_DashaTimeline
GO
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[vw_Chart_DashaTimeline] AS
WITH PeriodPath AS (
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(dp.Lord AS NVARCHAR(200)) AS PathLabel
    FROM dbo.tbl_Chart_DashaPeriods dp
    WHERE dp.ParentDashaPeriodId IS NULL
    UNION ALL
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(pp.PathLabel + '' > '' + dp.Lord AS NVARCHAR(200))
    FROM dbo.tbl_Chart_DashaPeriods dp
    JOIN PeriodPath pp ON pp.Id = dp.ParentDashaPeriodId
)
SELECT
    bd.Id                   AS BirthDetailId,
    bd.Name,
    cr.Id                   AS ChartResultId,
    dp.Id                   AS DashaPeriodId,
    dp.ParentDashaPeriodId,
    dp.LevelNumber,
    CASE dp.LevelNumber WHEN 1 THEN ''Mahadasha'' WHEN 2 THEN ''Antardasha'' ELSE ''Pratyantardasha'' END AS LevelName,
    dp.SequenceInParent,
    dp.Lord,
    pp.PathLabel,
    dp.StartDate,
    dp.EndDate,
    dp.StartDayOffset,
    dp.EndDayOffset
FROM dbo.tbl_Chart_DashaPeriods dp
JOIN dbo.tbl_ChartResults cr ON cr.Id = dp.ChartResultId
JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
JOIN PeriodPath pp           ON pp.Id = dp.Id;
'
GO

-- ---------------------------------------------------------------------
-- 4. tvf_Chart_SadeSatiPeriods
--    MoonSign CTE: kd.BirthDetailId / kd.ChartType = 'D1'
--      -> JOIN tbl_ChartResults cr ON cr.Id = kd.ChartResultId
--         WHERE cr.BirthDetailId = @BirthDetailId AND cr.ChartTypeId = 1
--    (sa join already on kd.SignId from migration 07)
-- ---------------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
DROP FUNCTION IF EXISTS dbo.tvf_Chart_SadeSatiPeriods
GO
EXEC dbo.sp_executesql @statement = N'
CREATE FUNCTION [dbo].[tvf_Chart_SadeSatiPeriods] (@BirthDetailId INT)
RETURNS TABLE
AS
RETURN
(
    WITH MoonSign AS (
        SELECT TOP (1) sa.Id AS MoonSignId
        FROM tbl_Chart_KeyDetails kd
        JOIN tbl_ChartResults cr ON cr.Id = kd.ChartResultId
        JOIN tbl_SignAttributes sa ON sa.Id = kd.SignId
        WHERE cr.BirthDetailId = @BirthDetailId AND kd.Planet = ''Moon'' AND cr.ChartTypeId = 1
    ),
    TargetSigns AS (
        SELECT ''SadeSati_Dhaiya1_Rising'' AS PeriodType, 1 AS SortOrder, ((MoonSignId - 1 + 11) % 12) + 1 AS TargetSignId FROM MoonSign
        UNION ALL SELECT ''SadeSati_Dhaiya2_Peak'',    2, ((MoonSignId - 1 + 0)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''SadeSati_Dhaiya3_Setting'', 3, ((MoonSignId - 1 + 1)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''KantakaShani'',             4, ((MoonSignId - 1 + 3)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''AshtamaShani'',             5, ((MoonSignId - 1 + 7)  % 12) + 1 FROM MoonSign
    ),
    SaturnPeriods AS (
        SELECT
            SignId,
            EventDateTimeUtc AS StartDateTimeUtc,
            LEAD(EventDateTimeUtc) OVER (ORDER BY EventDateTimeUtc) AS EndDateTimeUtc
        FROM tbl_PlanetSignTransitEvents
        WHERE PlanetId = 7  -- Saturn
    )
    SELECT
        ts.PeriodType,
        ts.SortOrder,
        sp.StartDateTimeUtc,
        sp.EndDateTimeUtc,   -- NULL = still ongoing / extends past the 2060-12-31 backfill boundary
        sa.SignName AS SaturnSign
    FROM TargetSigns ts
    JOIN SaturnPeriods sp ON sp.SignId = ts.TargetSignId
    JOIN tbl_SignAttributes sa ON sa.Id = sp.SignId
);
'
GO

-- ---------------------------------------------------------------------
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '09_finalize_views_after_drop.sql', 'views/TVFs source person+chart-type+compute-time from tbl_ChartResults'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '09_finalize_views_after_drop.sql');
GO
PRINT '09 applied: views finalized off child parent-identity columns.';
GO
