-- 032_add_nakshatra_linkage_to_keydetails.sql
-- Links tbl_Chart_KeyDetails rows to the nakshatra reference tables (product_scope_nakshatra_
-- linkage_and_divisional_charts.md, work-unit 1). All three columns NULL-able only to survive the
-- window between this migration and the next `recompute-keydetails` run; every produced row
-- resolves to a nakshatra. NakshatraId + NakshatraSubLordPlanet are populated for every chart type
-- (real-longitude facts); NakshatraPadaId is D1-only (matches the existing NakshatraPada gating).

IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraId') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraPadaId') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraPadaId INT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'NakshatraSubLordPlanet') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD NakshatraSubLordPlanet VARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartKeyDetails_Nakshatra')
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD CONSTRAINT FK_ChartKeyDetails_Nakshatra
        FOREIGN KEY (NakshatraId) REFERENCES dbo.tbl_Nakshatras(Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartKeyDetails_NakshatraPada')
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD CONSTRAINT FK_ChartKeyDetails_NakshatraPada
        FOREIGN KEY (NakshatraPadaId) REFERENCES dbo.tbl_NakshatraPadas(Id);
GO
