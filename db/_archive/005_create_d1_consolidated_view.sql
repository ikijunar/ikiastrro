-- vedic_horo_gen: consolidated D1 view
-- One wide, human-readable row per planet — joins tbl_BirthDetails + tbl_ChartResults +
-- tbl_D1Chart_keydetails + tbl_D1Chart_HouseLords + tbl_D1Chart_Conjunctions + tbl_D1Chart_Aspects,
-- so a single SELECT gives the complete picture without any manual joins.
-- RulesHouseNumbers / ConjunctWith / Aspects / AspectedBy are all derived (correlated STRING_AGG),
-- so they don't need to live as columns anywhere.
USE vedic_horo_gen;
GO

CREATE OR ALTER VIEW dbo.vw_D1Chart_Consolidated AS
SELECT
    bd.Id                       AS BirthDetailId,
    bd.Name,
    bd.DateOfBirth,
    bd.TimeOfBirth,
    bd.CorrectedTimeOfBirth,
    bd.PlaceCity,
    bd.PlaceCountry,
    cr.Id                       AS ChartResultId,
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
FROM dbo.tbl_D1Chart_keydetails kd
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = kd.BirthDetailId
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), ',') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_D1Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanet = kd.Planet
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, ', ') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_D1Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1 = kd.Planet
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_D1Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2 = kd.Planet
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, ' (', AspectType, ')'), ', ') AS TargetList
    FROM dbo.tbl_D1Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanet = kd.Planet
) AspectsCast;
GO
