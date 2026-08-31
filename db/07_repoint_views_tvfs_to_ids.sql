-- =====================================================================
-- 07 — Repoint the chart views/TVFs onto the integer *Id joins added in
-- migrations 04/05 and enforced (NOT NULL + FK) in 06. Every object is
-- DROP-and-RECREATEd so a re-run is safe. The SELECT output lists are
-- byte-identical to the pre-migration definitions (the Web layer binds by
-- column name); ONLY the join/where predicates change from name-equality
-- to id-equality. All four objects were created under
-- QUOTED_IDENTIFIER OFF / ANSI_NULLS ON — preserved here via the same
-- EXEC dbo.sp_executesql @statement = N'...' idiom the baseline uses
-- (single quotes inside the body are doubled).
-- Idempotent. Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/07_repoint_views_tvfs_to_ids.sql
-- =====================================================================
USE [ikiastrro];
GO

-- ---------------------------------------------------------------------
-- 1. vw_Chart_Consolidated
--    Joins changed to *Id equality (SELECT list unchanged — still emits
--    the Planet / Sign name strings and the aggregated name lists):
--      tbl_Fact_PlanetAvastha  av.Planet      = kd.Planet  -> av.PlanetId       = kd.PlanetId
--      RulesHouses  OUTER APPLY hl.LordPlanet  = kd.Planet  -> hl.LordPlanetId   = kd.PlanetId
--      Conjunct     OUTER APPLY Planet1        = kd.Planet  -> Planet1Id         = kd.PlanetId
--      Conjunct     OUTER APPLY Planet2        = kd.Planet  -> Planet2Id         = kd.PlanetId
--      AspectsCast  OUTER APPLY AspectingPlanet= kd.Planet  -> AspectingPlanetId = kd.PlanetId
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
    kd.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = kd.BirthDetailId
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
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
--    JOIN tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.HouseSign
--      -> ON sa.Id = hl.HouseSignId
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
    hl.BirthDetailId,
    hl.ChartType,
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
JOIN dbo.tbl_SignAttributes  sa   ON sa.Id = hl.HouseSignId
JOIN dbo.tbl_NakshatraPadas  p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                                 AND p.StartDegree <  sa.Id * 30.0
JOIN dbo.tbl_Nakshatras      n    ON n.Id = p.NakshatraId
JOIN dbo.tbl_Planets         lord ON lord.Id = n.RulingPlanetId
JOIN dbo.tbl_SignAttributes  nav  ON nav.Id = p.NavamsaSignId;
'
GO

-- ---------------------------------------------------------------------
-- 3. tvf_Chart_LifeWeeks
--    dasha-chart lookup: cr.ChartType = 'VimshottariDasha'
--      -> cr.CalculationKind = 'VimshottariDasha'
-- ---------------------------------------------------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
DROP FUNCTION IF EXISTS dbo.tvf_Chart_LifeWeeks
GO
EXEC dbo.sp_executesql @statement = N'-- Resolves the person''s own VimshottariDasha ChartResultId first (most recent, if somehow more
-- than one exists) and scopes every period join to that specific ChartResultId -- not just
-- BirthDetailId -- so a stale, un-deleted prior computation (e.g. after a birth-time correction
-- was recomputed without clearing the old periods first) can never silently double up rows here.
-- OUTER APPLY (not CROSS APPLY) so a person with no Dasha computed yet still returns the full
-- 4000-week grid with NULL lords, instead of an empty result.
CREATE FUNCTION [dbo].[tvf_Chart_LifeWeeks] (@BirthDetailId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        lc.WeekNumber,
        DATEADD(DAY, lc.WeekStartOffset, bd.DateOfBirth) AS WeekStartDate,
        DATEADD(DAY, lc.WeekEndOffset,   bd.DateOfBirth) AS WeekEndDate,
        maha.Lord       AS MahaLord,
        antar.Lord      AS AntarLord,
        pratyantar.Lord AS PratyantarLord
    FROM dbo.tbl_BirthDetails bd
    OUTER APPLY (
        SELECT TOP 1 cr.Id
        FROM dbo.tbl_ChartResults cr
        WHERE cr.BirthDetailId = bd.Id AND cr.CalculationKind = ''VimshottariDasha''
        ORDER BY cr.Id DESC
    ) dashaChart(ChartResultId)
    CROSS JOIN dbo.tbl_Dim_LifeCalendar lc
    LEFT JOIN dbo.tbl_Chart_DashaPeriods maha
        ON maha.ChartResultId = dashaChart.ChartResultId AND maha.LevelNumber = 1
        AND lc.WeekStartOffset BETWEEN maha.StartDayOffset AND maha.EndDayOffset
    LEFT JOIN dbo.tbl_Chart_DashaPeriods antar
        ON antar.ChartResultId = dashaChart.ChartResultId AND antar.LevelNumber = 2
        AND lc.WeekStartOffset BETWEEN antar.StartDayOffset AND antar.EndDayOffset
    LEFT JOIN dbo.tbl_Chart_DashaPeriods pratyantar
        ON pratyantar.ChartResultId = dashaChart.ChartResultId AND pratyantar.LevelNumber = 3
        AND lc.WeekStartOffset BETWEEN pratyantar.StartDayOffset AND pratyantar.EndDayOffset
    WHERE bd.Id = @BirthDetailId
      AND lc.WeekNumber BETWEEN 1 AND 4000
      AND lc.DayOffset = lc.WeekStartOffset   -- one row per week, not one per day
);
'
GO

-- ---------------------------------------------------------------------
-- 4. tvf_Chart_SadeSatiPeriods
--    JOIN tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign
--      -> ON sa.Id = kd.SignId
--    (the kd.BirthDetailId / kd.ChartType = 'D1' filter is LEFT AS-IS — Task 19 rewrites it)
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
        JOIN tbl_SignAttributes sa ON sa.Id = kd.SignId
        WHERE kd.BirthDetailId = @BirthDetailId AND kd.Planet = ''Moon'' AND kd.ChartType = ''D1''
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
SELECT '07_repoint_views_tvfs_to_ids.sql', 'views/TVFs join on *Id internally; output columns unchanged'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '07_repoint_views_tvfs_to_ids.sql');
GO
PRINT '07 applied: views/TVFs on ids.';
GO
