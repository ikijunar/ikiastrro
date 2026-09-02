-- =====================================================================
-- 16 — Rename the avastha star schema to "planetary-state" (engine reorg, Plan 1).
--   tbl_Dim_AvasthaState    -> tbl_Dim_PlanetaryState
--   tbl_Rule_BaaladiState   -> tbl_Rule_AgeState
--   tbl_Rule_JagradadiState -> tbl_Rule_WakefulnessState
--   tbl_Fact_PlanetAvastha  -> tbl_Fact_PlanetaryState
--   fact cols: BaaladiStateId->AgeStateId, BaaladiEffectFraction->AgeEffectFraction,
--              JagradadiStateId->WakefulnessStateId
-- StateName / AvasthaSystem row values UNCHANGED. Rebuilds vw_Chart_Consolidated. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/16_rename_avastha_to_planetary_state.sql
-- =====================================================================
USE [ikiastrro];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '16_rename_avastha_to_planetary_state.sql')
BEGIN
    IF OBJECT_ID('dbo.tbl_Dim_AvasthaState','U')    IS NOT NULL EXEC sp_rename 'dbo.tbl_Dim_AvasthaState',    'tbl_Dim_PlanetaryState';
    IF OBJECT_ID('dbo.tbl_Rule_BaaladiState','U')   IS NOT NULL EXEC sp_rename 'dbo.tbl_Rule_BaaladiState',   'tbl_Rule_AgeState';
    IF OBJECT_ID('dbo.tbl_Rule_JagradadiState','U') IS NOT NULL EXEC sp_rename 'dbo.tbl_Rule_JagradadiState', 'tbl_Rule_WakefulnessState';
    IF OBJECT_ID('dbo.tbl_Fact_PlanetAvastha','U')  IS NOT NULL EXEC sp_rename 'dbo.tbl_Fact_PlanetAvastha',  'tbl_Fact_PlanetaryState';

    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','BaaladiStateId')        IS NOT NULL EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.BaaladiStateId',        'AgeStateId',         'COLUMN';
    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','BaaladiEffectFraction') IS NOT NULL EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.BaaladiEffectFraction', 'AgeEffectFraction',  'COLUMN';
    IF COL_LENGTH('dbo.tbl_Fact_PlanetaryState','JagradadiStateId')      IS NOT NULL EXEC sp_rename 'dbo.tbl_Fact_PlanetaryState.JagradadiStateId',      'WakefulnessStateId', 'COLUMN';

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    SELECT '16_rename_avastha_to_planetary_state.sql',
           'rename avastha star schema -> planetary-state (tables + fact cols; row values unchanged)'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '16_rename_avastha_to_planetary_state.sql');

    PRINT '16 applied: avastha -> planetary-state rename.';
END
ELSE
    PRINT '16 already applied.';
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
CREATE OR ALTER VIEW dbo.vw_Chart_Consolidated AS
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
    kd.PointKind,
    kd.CharaKaraka,
    kd.NirayanaLongitudeDegrees,
    kd.VargaLongitudeDegrees,
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
    ageState.StateName           AS AgeState,
    av.AgeEffectFraction,
    wakeState.StateName          AS WakefulnessState,
    RulesHouses.HouseList        AS RulesHouseNumbers,
    Conjunct.PlanetList           AS ConjunctWith,
    AspectsCast.TargetList        AS Aspects,
    kd.AspectingPlanets          AS AspectedBy,
    cr.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = cr.BirthDetailId
LEFT JOIN dbo.tbl_Fact_PlanetaryState av ON av.ChartResultId = kd.ChartResultId AND av.PlanetId = kd.PlanetId
LEFT JOIN dbo.tbl_Dim_PlanetaryState  ageState  ON ageState.Id  = av.AgeStateId
LEFT JOIN dbo.tbl_Dim_PlanetaryState  wakeState ON wakeState.Id = av.WakefulnessStateId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), ',') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanetId = kd.PlanetId
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, ', ') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1Id = kd.PlanetId
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2Id = kd.PlanetId
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, ' (', AspectType, ')'), ', ') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanetId = kd.PlanetId
) AspectsCast;
GO
