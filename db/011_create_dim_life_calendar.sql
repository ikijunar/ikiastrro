-- vedic_horo_gen: age-relative "life calendar" date dimension
--
-- Standard Kimball-style date dimension (one row per day, sliceable by week/month/year), but
-- age-relative instead of calendar-absolute: DayOffset 0 = day of birth, not a real calendar
-- date. Shared/reusable across every person — a specific person's real calendar date for any
-- row is just their DateOfBirth + DayOffset, computed where needed rather than stored here.
--
-- Bucketing convention (a deliberate simplification, not real Gregorian months/years — there is
-- no universal agreement on "a month of life," so this project fixes one): 7-day weeks (matches
-- rammyps's "assume every person has 4000 weeks" framing, 2026-08-27), 30-day months, 365-day
-- years. Week/Month/Year numbers are each derived independently from DayOffset — they are not
-- forced to align with each other (e.g. a year boundary does not land on a week boundary),
-- which mirrors how real calendars behave too.
--
-- Range: DayOffset 0..43829 (~121 years) — deliberately wider than the "4000 weeks" (~76.9
-- year) framing so it can still join against the full classical 120-year Vimshottari cycle
-- (see 012_create_dasha_periods_table.sql) without gaps. WeekNumber > 4000 rows exist and are
-- valid for Dasha joins; the "life in weeks" UI is what applies the 4000-week display cap, not
-- this table.
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Dim_LifeCalendar')
BEGIN
    CREATE TABLE dbo.tbl_Dim_LifeCalendar
    (
        DayOffset        INT NOT NULL PRIMARY KEY,   -- 0 = day of birth
        WeekNumber       INT NOT NULL,                -- 1-based, 7 days/week
        WeekStartOffset  INT NOT NULL,
        WeekEndOffset    INT NOT NULL,
        MonthNumber      INT NOT NULL,                -- 1-based, 30-day "life month" bucket
        MonthStartOffset INT NOT NULL,
        MonthEndOffset   INT NOT NULL,
        YearNumber       INT NOT NULL,                -- 1-based, 365-day "life year" bucket
        YearStartOffset  INT NOT NULL,
        YearEndOffset    INT NOT NULL
    );

    CREATE INDEX IX_Dim_LifeCalendar_WeekNumber  ON dbo.tbl_Dim_LifeCalendar (WeekNumber);
    CREATE INDEX IX_Dim_LifeCalendar_MonthNumber ON dbo.tbl_Dim_LifeCalendar (MonthNumber);
    CREATE INDEX IX_Dim_LifeCalendar_YearNumber  ON dbo.tbl_Dim_LifeCalendar (YearNumber);
END
GO

-- Seed once. Set-based (recursive CTE), not a WHILE loop — 43,830 rows is small enough for
-- MAXRECURSION 0 to build in one statement.
IF NOT EXISTS (SELECT * FROM dbo.tbl_Dim_LifeCalendar)
BEGIN
    ;WITH Days AS (
        SELECT 0 AS DayOffset
        UNION ALL
        SELECT DayOffset + 1 FROM Days WHERE DayOffset < 43829
    )
    INSERT INTO dbo.tbl_Dim_LifeCalendar
        (DayOffset, WeekNumber, WeekStartOffset, WeekEndOffset,
         MonthNumber, MonthStartOffset, MonthEndOffset,
         YearNumber, YearStartOffset, YearEndOffset)
    SELECT
        DayOffset,
        (DayOffset / 7)   + 1              AS WeekNumber,
        (DayOffset / 7)   * 7               AS WeekStartOffset,
        (DayOffset / 7)   * 7  + 6          AS WeekEndOffset,
        (DayOffset / 30)  + 1               AS MonthNumber,
        (DayOffset / 30)  * 30              AS MonthStartOffset,
        (DayOffset / 30)  * 30 + 29         AS MonthEndOffset,
        (DayOffset / 365) + 1               AS YearNumber,
        (DayOffset / 365) * 365             AS YearStartOffset,
        (DayOffset / 365) * 365 + 364       AS YearEndOffset
    FROM Days
    OPTION (MAXRECURSION 0);
END
GO
