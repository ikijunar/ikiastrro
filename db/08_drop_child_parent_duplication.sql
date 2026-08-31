-- =====================================================================
-- 08 — Drop parent-identity duplication from the child fact tables
-- (review item 3). Person + chart type now come only from tbl_ChartResults.
--
-- Requires: migration 09 (views off child BirthDetailId/ChartType) is NOT
-- yet applied for vw_Chart_Consolidated / vw_Chart_HouseNakshatraSpan /
-- vw_Chart_DashaTimeline / tvf_Chart_SadeSatiPeriods — run 08 and 09
-- together (Task 19). None of those objects are schema-bound, so the
-- column drops below succeed; the views just break until 09 recreates them.
--
-- Idempotent. Drops, in order, everything that pins these columns:
--   1. non-key indexes that include BirthDetailId / ChartType / ComputedAt
--   2. foreign keys on BirthDetailId (auto-named FK__* — discovered dynamically)
--   3. default constraints on the three columns (auto-named DF__* — dynamic)
--   4. the columns themselves
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/08_drop_child_parent_duplication.sql
-- =====================================================================
USE [ikiastrro];
GO

DECLARE @childTables TABLE (name SYSNAME);
INSERT @childTables (name) VALUES
    ('tbl_Chart_KeyDetails'), ('tbl_Chart_HouseLords'), ('tbl_Chart_Conjunctions'),
    ('tbl_Chart_Aspects'), ('tbl_Chart_DashaPeriods'), ('tbl_Fact_PlanetAvastha');

DECLARE @targetCols TABLE (name SYSNAME);
INSERT @targetCols (name) VALUES ('BirthDetailId'), ('ChartType'), ('ComputedAt');

DECLARE @sql NVARCHAR(MAX) = N'';

-- 1. Indexes (non-PK, non-unique-constraint) that touch a target column ----
SELECT @sql = @sql + N'DROP INDEX ' + QUOTENAME(i.name) + N' ON '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.name IN (SELECT name FROM @childTables)
  AND i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.type <> 0   -- has a name, is a real B-tree
  AND EXISTS (
      SELECT 1 FROM sys.index_columns ic
      JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
      WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
        AND c.name IN (SELECT name FROM @targetCols));

-- 2. Foreign keys on a target column (all auto-named FK__*) ----------------
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id = fk.parent_object_id
WHERE t.name IN (SELECT name FROM @childTables)
  AND EXISTS (
      SELECT 1 FROM sys.foreign_key_columns fkc
      JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
      WHERE fkc.constraint_object_id = fk.object_id
        AND c.name IN (SELECT name FROM @targetCols));

-- 3. Default constraints on the three columns (auto-named DF__* + explicit) -
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables t ON t.object_id = dc.parent_object_id
WHERE t.name IN (SELECT name FROM @childTables)
  AND c.name IN (SELECT name FROM @targetCols);

IF @sql <> N''
BEGIN
    PRINT '08: dropping blocking indexes / FKs / default constraints:';
    PRINT @sql;
    EXEC sys.sp_executesql @sql;
END
GO

-- 4. Drop the columns -----------------------------------------------------
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_HouseLords','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_HouseLords DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_Conjunctions DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Chart_Aspects','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_Aspects DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO
-- tbl_Chart_DashaPeriods has no ChartType column — only BirthDetailId + ComputedAt.
IF COL_LENGTH('dbo.tbl_Chart_DashaPeriods','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Chart_DashaPeriods DROP COLUMN BirthDetailId, ComputedAt;
GO
IF COL_LENGTH('dbo.tbl_Fact_PlanetAvastha','BirthDetailId') IS NOT NULL
    ALTER TABLE dbo.tbl_Fact_PlanetAvastha DROP COLUMN BirthDetailId, ChartType, ComputedAt;
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '08_drop_child_parent_duplication.sql', 'dropped BirthDetailId/ChartType/ComputedAt from child fact tables'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '08_drop_child_parent_duplication.sql');
GO
PRINT '08 applied: child-table redundancy dropped.';
GO
