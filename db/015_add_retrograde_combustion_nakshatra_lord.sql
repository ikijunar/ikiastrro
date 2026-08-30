-- vedic_horo_gen: adds retrograde flag, combustion (Asta) logic, and nakshatra lord to
-- tbl_Chart_KeyDetails — the three gaps identified 2026-08-28 during a manual chart-verification
-- session (see the dated backlog section in ikiastrro.md), re-scored into
-- methods_prodmag.md's Opportunity Backlog the same day (ICE 8.0 / 6.7 / 7.7) and promoted to Now.
--
-- Column-shape rationale:
--   - IsRetrograde and NakshatraLordPlanet are populated for EVERY chart type (D1 and D9 alike) —
--     both are derived purely from the real (D1) NirayanaLongitudeDegrees, which every chart type
--     already carries regardless of which divisional chart is being read (same convention as that
--     column itself — see migration 010's rationale comment). Null only for the Ascendant's
--     IsRetrograde (no retrograde concept for a house-circle point); NakshatraLordPlanet IS
--     populated for the Ascendant (Lagna's nakshatra lord is a real classical value).
--   - IsCombust/DistanceFromSunDegrees/CombustionOrbUsedDegrees are null for the Ascendant, Sun,
--     Rahu, and Ketu (combustion only applies to the 6 planets that can meaningfully be combust —
--     see ClassicalCombustion.cs) and populated identically for D1 and D9 rows, same reasoning as
--     above (real angular separation from the Sun doesn't depend on which varga is being viewed).
USE vedic_horo_gen;
GO

IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraLordPlanet') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraLordPlanet VARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'IsRetrograde') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD IsRetrograde BIT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'IsCombust') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD IsCombust BIT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'DistanceFromSunDegrees') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD DistanceFromSunDegrees DECIMAL(7,4) NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'CombustionOrbUsedDegrees') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CombustionOrbUsedDegrees DECIMAL(5,2) NULL;
GO

-- Recreate the consolidated view to expose the 5 new columns.
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
    kd.NakshatraLordPlanet,
    kd.IsRetrograde,
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
