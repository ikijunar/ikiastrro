-- vedic_horo_gen: removes CorrectedTimeOfBirth/CorrectionReason from tbl_BirthDetails.
--
-- Unused in practice (NULL for every person ever entered — confirmed 2026-08-28) and a source of
-- UI confusion (a form field implying "this is how you fix a wrong birth time," when the actual
-- fix path is editing TimeOfBirth directly, as done for Ramakrishnan the same day). Removed at
-- rammyps's explicit request as part of a UI cleanup pass (obsolete "Apply a corrected time of
-- birth" checkbox on the entry form), scoped to also drop the DB columns rather than leave dead
-- storage behind. BirthDetails.EffectiveTimeOfBirth (Core) stays as a property — now just returns
-- TimeOfBirth — so every chart-calculation call site (BirthMomentFactory, IChartCalculator,
-- ChartView.razor) needed zero changes.
USE vedic_horo_gen;
GO

-- Recreate vw_Chart_Consolidated first (it selects bd.CorrectedTimeOfBirth) so dropping the
-- column below doesn't fail on a dependent object.
DROP VIEW IF EXISTS dbo.vw_Chart_Consolidated;
GO
CREATE VIEW dbo.vw_Chart_Consolidated AS
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

-- Simplify usp_Chart_RecomputeJhdExport: no more CorrectedTimeOfBirth to COALESCE with.
CREATE OR ALTER PROCEDURE dbo.usp_Chart_RecomputeJhdExport
    @BirthDetailId INT = NULL,
    @Force         BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Src AS (
        SELECT
            bd.Id            AS BirthDetailId,
            bd.DateOfBirth,
            bd.TimeOfBirth   AS EffTime,
            bd.Latitude,
            bd.Longitude,
            bd.UtcOffset,
            bd.PlaceCity,
            bd.PlaceCountry
        FROM dbo.tbl_BirthDetails bd
        WHERE (@BirthDetailId IS NULL OR bd.Id = @BirthDetailId)
    ),
    OffsetParsed AS (
        SELECT
            *,
            CASE WHEN LEFT(UtcOffset, 1) = '-' THEN -1 ELSE 1 END                                   AS OffsetSign,
            CAST(LEFT(REPLACE(UtcOffset, '-', ''), CHARINDEX(':', REPLACE(UtcOffset, '-', '')) - 1) AS INT) AS OffsetHour,
            CAST(SUBSTRING(REPLACE(UtcOffset, '-', ''), CHARINDEX(':', REPLACE(UtcOffset, '-', '')) + 1, 2) AS INT) AS OffsetMinute
        FROM Src
    ),
    Calc AS (
        SELECT
            BirthDetailId,
            CAST(MONTH(DateOfBirth) AS TINYINT)  AS Month,
            CAST(DAY(DateOfBirth)   AS TINYINT)  AS Day,
            CAST(YEAR(DateOfBirth)  AS SMALLINT) AS Year,
            CAST(DATEPART(HOUR, EffTime) + DATEPART(MINUTE, EffTime) / 100.0 AS DECIMAL(9,6)) AS Time,
            CAST(-1 * OffsetSign * (OffsetHour + OffsetMinute / 100.0) AS DECIMAL(9,6))       AS TimeZone,
            CAST(
                CASE WHEN Longitude >= 0 THEN -1 ELSE 1 END *
                (FLOOR(ABS(Longitude)) + ((ABS(Longitude) - FLOOR(ABS(Longitude))) * 60) / 100.0)
                AS DECIMAL(9,6)
            ) AS Longitude,
            CAST(
                SIGN(Latitude) *
                (FLOOR(ABS(Latitude)) + ((ABS(Latitude) - FLOOR(ABS(Latitude))) * 60) / 100.0)
                AS DECIMAL(9,6)
            ) AS Latitude,
            PlaceCity,
            PlaceCountry
        FROM OffsetParsed
    )
    MERGE dbo.tbl_Chart_JhdExport AS tgt
    USING Calc AS src
        ON tgt.BirthDetailId = src.BirthDetailId
    WHEN MATCHED AND (tgt.IsOverridden = 0 OR @Force = 1) THEN
        UPDATE SET
            Month = src.Month, Day = src.Day, Year = src.Year,
            Time = src.Time, TimeZone = src.TimeZone,
            Longitude = src.Longitude, Latitude = src.Latitude,
            LmtRef1 = src.TimeZone, LmtRef2 = src.TimeZone,
            PlaceCity = src.PlaceCity, PlaceCountry = src.PlaceCountry,
            ComputedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (BirthDetailId, Month, Day, Year, Time, TimeZone, Longitude, Latitude,
                LmtRef1, LmtRef2, PlaceCity, PlaceCountry)
        VALUES (src.BirthDetailId, src.Month, src.Day, src.Year, src.Time, src.TimeZone,
                src.Longitude, src.Latitude, src.TimeZone, src.TimeZone,
                src.PlaceCity, src.PlaceCountry);
END
GO

-- Finally, drop the two columns.
ALTER TABLE dbo.tbl_BirthDetails DROP COLUMN CorrectedTimeOfBirth;
GO
ALTER TABLE dbo.tbl_BirthDetails DROP COLUMN CorrectionReason;
GO
