-- vedic_horo_gen: reporting layer connecting Vimshottari Dasha periods to the life calendar
--
-- Two objects, one purpose — "what Dasha was active when," read two different ways:
--   vw_Chart_DashaTimeline  — one row per period (Maha/Antar/Pratyantar), human-readable,
--     with a materialized "Saturn > Mercury > Ketu" path label. For browsing/printing a
--     person's full Dasha sequence.
--   tvf_Chart_LifeWeeks     — one row per week (1-4000, rammyps's "4000 weeks" framing,
--     2026-08-27), the active lord at each of the 3 levels for that week. For the life-in-weeks
--     grid view. A table-valued function (parameterized by BirthDetailId), not a view, since a
--     bare view would need to CROSS JOIN every saved person against 4000 calendar rows to give
--     the same shape — wasteful when the caller always wants exactly one person at a time.
--
-- No function-naming convention existed yet in STANDARDS.md §D (only tbl_/vw_/usp_/IX_ were
-- defined) — `tvf_PascalCase` is introduced here as the parallel for table-valued functions;
-- worth folding into STANDARDS.md if it's kept.
USE vedic_horo_gen;
GO

DROP VIEW IF EXISTS dbo.vw_Chart_DashaTimeline;
GO
CREATE VIEW dbo.vw_Chart_DashaTimeline AS
WITH PeriodPath AS (
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(dp.Lord AS NVARCHAR(200)) AS PathLabel
    FROM dbo.tbl_Chart_DashaPeriods dp
    WHERE dp.ParentDashaPeriodId IS NULL
    UNION ALL
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(pp.PathLabel + ' > ' + dp.Lord AS NVARCHAR(200))
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
    CASE dp.LevelNumber WHEN 1 THEN 'Mahadasha' WHEN 2 THEN 'Antardasha' ELSE 'Pratyantardasha' END AS LevelName,
    dp.SequenceInParent,
    dp.Lord,
    pp.PathLabel,
    dp.StartDate,
    dp.EndDate,
    dp.StartDayOffset,
    dp.EndDayOffset
FROM dbo.tbl_Chart_DashaPeriods dp
JOIN dbo.tbl_BirthDetails bd ON bd.Id = dp.BirthDetailId
JOIN dbo.tbl_ChartResults cr ON cr.Id = dp.ChartResultId
JOIN PeriodPath pp           ON pp.Id = dp.Id;
GO

DROP FUNCTION IF EXISTS dbo.tvf_Chart_LifeWeeks;
GO
-- Resolves the person's own VimshottariDasha ChartResultId first (most recent, if somehow more
-- than one exists) and scopes every period join to that specific ChartResultId — not just
-- BirthDetailId — so a stale, un-deleted prior computation (e.g. after a birth-time correction
-- was recomputed without clearing the old periods first) can never silently double up rows here.
-- OUTER APPLY (not CROSS APPLY) so a person with no Dasha computed yet still returns the full
-- 4000-week grid with NULL lords, instead of an empty result.
CREATE FUNCTION dbo.tvf_Chart_LifeWeeks (@BirthDetailId INT)
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
        WHERE cr.BirthDetailId = bd.Id AND cr.ChartType = 'VimshottariDasha'
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
GO
