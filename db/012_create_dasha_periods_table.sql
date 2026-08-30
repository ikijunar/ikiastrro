-- vedic_horo_gen: Vimshottari Dasha (Mahadasha / Antardasha / Pratyantardasha) periods
--
-- Deliberately NOT one of the 4 shared chart-analytical tables (tbl_Chart_KeyDetails/
-- HouseLords/Conjunctions/Aspects) — those are planet-position/house-grain and Dasha has no
-- such shape. This is its own table, self-referencing to express the 3-level hierarchy, but
-- still tied to a tbl_ChartResults row (ChartType='VimshottariDasha') so it gets delete-cascade
-- and /charts listing consistency for free, per rammyps's explicit architecture choice
-- (2026-08-27) — see methods_prodmag.md for the decision record.
--
-- One row per period at ANY level (Maha/Antar/Pratyantar) — not 3 separate tables — since all
-- 3 levels share an identical shape (a lord, a date range, a position in a 9-period cycle) and
-- differ only in nesting depth (ParentDashaPeriodId) and duration.
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_DashaPeriods')
BEGIN
    CREATE TABLE dbo.tbl_Chart_DashaPeriods
    (
        Name                NVARCHAR(200)  NULL,       -- denormalized from BirthDetails, so this table reads standalone — deliberately 1st column
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId       INT            NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId       INT            NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        ParentDashaPeriodId INT            NULL REFERENCES dbo.tbl_Chart_DashaPeriods(Id),  -- NULL = Mahadasha (top level)
        LevelNumber         TINYINT        NOT NULL,   -- 1 = Mahadasha, 2 = Antardasha, 3 = Pratyantardasha
        SequenceInParent    TINYINT        NOT NULL,   -- 1-9, this period's position in its parent's 9-lord cycle
        Lord                VARCHAR(20)    NOT NULL,   -- Ketu/Venus/Sun/Moon/Mars/Rahu/Jupiter/Saturn/Mercury
        StartDate           DATETIME2(0)   NOT NULL,   -- real calendar date/time = birth datetime + StartDayOffset
        EndDate             DATETIME2(0)   NOT NULL,
        StartDayOffset      INT            NOT NULL,   -- days since birth (Day 0 = DOB) — joins to tbl_Dim_LifeCalendar
        EndDayOffset        INT            NOT NULL,
        ComputedAt          DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT CK_Chart_DashaPeriods_LevelNumber      CHECK (LevelNumber IN (1, 2, 3)),
        CONSTRAINT CK_Chart_DashaPeriods_SequenceInParent CHECK (SequenceInParent BETWEEN 1 AND 9)
    );

    CREATE INDEX IX_Chart_DashaPeriods_ChartResultId ON dbo.tbl_Chart_DashaPeriods (ChartResultId);
    CREATE INDEX IX_Chart_DashaPeriods_ParentDashaPeriodId ON dbo.tbl_Chart_DashaPeriods (ParentDashaPeriodId);
    -- Primary lookup pattern: "what was active for this person, at this level, covering this day"
    CREATE INDEX IX_Chart_DashaPeriods_BirthDetailId_LevelNumber_StartDayOffset
        ON dbo.tbl_Chart_DashaPeriods (BirthDetailId, LevelNumber, StartDayOffset, EndDayOffset);
END
GO
