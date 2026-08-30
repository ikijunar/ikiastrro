-- 023_create_sade_sati_dhaiya_function.sql
-- tvf_Chart_SadeSatiPeriods(@BirthDetailId) -- Saturn-from-natal-Moon affliction periods:
-- Sade Sati's three Dhaiyas (12th/1st/2nd from Moon) plus Kantaka Shani (4th from Moon) and
-- Ashtama Shani (8th from Moon). Built entirely from tables that already exist -- no new
-- classical reference data needed, unlike Ashtakavarga (separately scoped, not built here).
--
-- House-from-Moon counting matches AstroMath.CountFromSignToSign's convention (same sign = 1):
-- TargetSignId(N) = ((MoonSignId - 1 + N - 1) % 12) + 1, for N in {12,1,2,4,8}.
--
-- Known boundary gap: tbl_PlanetSignTransitEvents' first row per planet is the first CROSSING
-- after 1930-01-01, not an entry for whatever sign Saturn was already in when the backfill
-- window opened -- so the period Saturn occupied from 1930-01-01 to its first recorded crossing
-- (1931-04-11) has no start date and won't appear as a row. Negligible in practice (nobody using
-- this needs Sade Sati data for age 0-1). The final period per planet has EndDateTimeUtc = NULL,
-- meaning "still ongoing / extends past the 2060-12-31 backfill boundary," not an unknown value.

IF OBJECT_ID('tvf_Chart_SadeSatiPeriods', 'IF') IS NOT NULL
    DROP FUNCTION tvf_Chart_SadeSatiPeriods;
GO

CREATE FUNCTION tvf_Chart_SadeSatiPeriods (@BirthDetailId INT)
RETURNS TABLE
AS
RETURN
(
    WITH MoonSign AS (
        SELECT TOP (1) sa.Id AS MoonSignId
        FROM tbl_Chart_KeyDetails kd
        JOIN tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign
        WHERE kd.BirthDetailId = @BirthDetailId AND kd.Planet = 'Moon' AND kd.ChartType = 'D1'
    ),
    TargetSigns AS (
        SELECT 'SadeSati_Dhaiya1_Rising' AS PeriodType, 1 AS SortOrder, ((MoonSignId - 1 + 11) % 12) + 1 AS TargetSignId FROM MoonSign
        UNION ALL SELECT 'SadeSati_Dhaiya2_Peak',    2, ((MoonSignId - 1 + 0)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT 'SadeSati_Dhaiya3_Setting', 3, ((MoonSignId - 1 + 1)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT 'KantakaShani',             4, ((MoonSignId - 1 + 3)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT 'AshtamaShani',             5, ((MoonSignId - 1 + 7)  % 12) + 1 FROM MoonSign
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
GO
