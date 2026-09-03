-- vedic_horo_gen: fixes from the 2026-08-28 schema review (findings 1-4; finding 5 was
-- confirmed still in use by the CLI's `show-chart` ResultJson dump, so left alone).
--
-- 1. Drop the redundant Name column carried by all 5 chart-analytical tables. It's a
--    denormalized, insert-time-only copy of tbl_BirthDetails.Name (set by the CLI/service
--    layer, never read back — vw_Chart_Consolidated and vw_Chart_DashaTimeline both join
--    tbl_BirthDetails.Name directly instead) with no trigger keeping it in sync, so a person
--    rename silently leaves every already-computed chart row stale. Application code updated
--    alongside this migration to stop populating it.
-- 2. Add a BirthDetailId index to the 3 tables missing one — BirthDetailDeletionService
--    deletes by BirthDetailId on all 5 analytical tables, but only KeyDetails/DashaPeriods
--    had an index leading with that column.
-- 3. Add UNIQUE constraints matching each table's natural key, mirroring the guard
--    tbl_Chart_HouseLords already has (UX_Chart_HouseLords_ChartResultId_HouseNumber) —
--    verified no existing duplicate rows before adding these.
-- 4. Reshape the DashaPeriods composite index to match what tvf_Chart_LifeWeeks actually
--    joins on (ChartResultId, LevelNumber, StartDayOffset/EndDayOffset range), not
--    BirthDetailId — the old index is dropped and replaced rather than kept alongside,
--    since nothing queries DashaPeriods by BirthDetailId+LevelNumber+StartDayOffset.
USE vedic_horo_gen;
GO

-- 1. Drop redundant Name column ---------------------------------------------------------
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails') AND name = 'Name')
    ALTER TABLE dbo.tbl_Chart_KeyDetails DROP COLUMN Name;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_HouseLords') AND name = 'Name')
    ALTER TABLE dbo.tbl_Chart_HouseLords DROP COLUMN Name;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions') AND name = 'Name')
    ALTER TABLE dbo.tbl_Chart_Conjunctions DROP COLUMN Name;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_Aspects') AND name = 'Name')
    ALTER TABLE dbo.tbl_Chart_Aspects DROP COLUMN Name;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_DashaPeriods') AND name = 'Name')
    ALTER TABLE dbo.tbl_Chart_DashaPeriods DROP COLUMN Name;
GO

-- 2. BirthDetailId indexes on the 3 tables that were missing one -----------------------
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chart_Aspects_BirthDetailId' AND object_id = OBJECT_ID('dbo.tbl_Chart_Aspects'))
    CREATE NONCLUSTERED INDEX IX_Chart_Aspects_BirthDetailId ON dbo.tbl_Chart_Aspects (BirthDetailId);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chart_Conjunctions_BirthDetailId' AND object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions'))
    CREATE NONCLUSTERED INDEX IX_Chart_Conjunctions_BirthDetailId ON dbo.tbl_Chart_Conjunctions (BirthDetailId);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chart_HouseLords_BirthDetailId' AND object_id = OBJECT_ID('dbo.tbl_Chart_HouseLords'))
    CREATE NONCLUSTERED INDEX IX_Chart_HouseLords_BirthDetailId ON dbo.tbl_Chart_HouseLords (BirthDetailId);
GO

-- 3. UNIQUE constraints on each table's natural key -------------------------------------
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Chart_KeyDetails_ChartResultId_Planet' AND object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails'))
    CREATE UNIQUE INDEX UX_Chart_KeyDetails_ChartResultId_Planet ON dbo.tbl_Chart_KeyDetails (ChartResultId, Planet);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Chart_Conjunctions_ChartResultId_Planet1_Planet2' AND object_id = OBJECT_ID('dbo.tbl_Chart_Conjunctions'))
    CREATE UNIQUE INDEX UX_Chart_Conjunctions_ChartResultId_Planet1_Planet2 ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1, Planet2);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_Chart_Aspects_ChartResultId_AspectingPlanet_AspectedTarget' AND object_id = OBJECT_ID('dbo.tbl_Chart_Aspects'))
    CREATE UNIQUE INDEX UX_Chart_Aspects_ChartResultId_AspectingPlanet_AspectedTarget ON dbo.tbl_Chart_Aspects (ChartResultId, AspectingPlanet, AspectedTarget);
GO

-- 4. Reshape DashaPeriods composite index to match tvf_Chart_LifeWeeks's join ------------
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chart_DashaPeriods_BirthDetailId_LevelNumber_StartDayOffset' AND object_id = OBJECT_ID('dbo.tbl_Chart_DashaPeriods'))
    DROP INDEX IX_Chart_DashaPeriods_BirthDetailId_LevelNumber_StartDayOffset ON dbo.tbl_Chart_DashaPeriods;
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Chart_DashaPeriods_ChartResultId_LevelNumber_StartDayOffset' AND object_id = OBJECT_ID('dbo.tbl_Chart_DashaPeriods'))
    CREATE NONCLUSTERED INDEX IX_Chart_DashaPeriods_ChartResultId_LevelNumber_StartDayOffset
        ON dbo.tbl_Chart_DashaPeriods (ChartResultId, LevelNumber, StartDayOffset, EndDayOffset);
GO
