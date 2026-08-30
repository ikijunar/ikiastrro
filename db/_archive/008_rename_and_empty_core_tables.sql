-- vedic_horo_gen: rename BirthDetails -> tbl_BirthDetails, ChartResults -> tbl_ChartResults, and
-- empty all data (a deliberate reset, not a cleanup of a few test rows).
--
-- Order matters: BirthDetails/ChartResults are FK-referenced by all 4 tbl_D1Chart_* tables (and
-- ChartResults is itself referenced FROM BirthDetails indirectly via ChartResults.BirthDetailId),
-- so children must be emptied before parents, or the DELETEs fail on FK violation.
--
-- sp_rename is safe here: SQL Server tracks FK constraints by object_id, not by name text, so
-- existing constraints from the 4 child tables keep working correctly after the rename — only the
-- visible name changes.
USE vedic_horo_gen;
GO

-- 1) Empty child tables first (order among these 4 doesn't matter — none reference each other)
DELETE FROM dbo.tbl_D1Chart_keydetails;
DELETE FROM dbo.tbl_D1Chart_HouseLords;
DELETE FROM dbo.tbl_D1Chart_Conjunctions;
DELETE FROM dbo.tbl_D1Chart_Aspects;

DBCC CHECKIDENT ('dbo.tbl_D1Chart_keydetails', RESEED, 0);
DBCC CHECKIDENT ('dbo.tbl_D1Chart_HouseLords', RESEED, 0);
DBCC CHECKIDENT ('dbo.tbl_D1Chart_Conjunctions', RESEED, 0);
DBCC CHECKIDENT ('dbo.tbl_D1Chart_Aspects', RESEED, 0);
GO

-- 2) Empty ChartResults (safe now — nothing references it anymore)
DELETE FROM dbo.ChartResults;
DBCC CHECKIDENT ('dbo.ChartResults', RESEED, 0);
GO

-- 3) Empty BirthDetails (safe now — ChartResults no longer references it either)
DELETE FROM dbo.BirthDetails;
DBCC CHECKIDENT ('dbo.BirthDetails', RESEED, 0);
GO

-- 4) Rename both tables (guarded — a fresh install using the updated 002_create_tables.sql
--    already creates them under the final names, so this is a no-op there)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ChartResults')
BEGIN
    EXEC sp_rename 'dbo.ChartResults', 'tbl_ChartResults';
END
GO
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'BirthDetails')
BEGIN
    EXEC sp_rename 'dbo.BirthDetails', 'tbl_BirthDetails';
END
GO
