-- =====================================================================
-- 181 - tbl_Ref_HistoricFigure.SiteLinkCount: number of Wikipedia language editions
-- the person has on Wikidata. It is the notability measure used to rank people inside
-- a cell and the input to the GeoArea (reach) proxy for the expanded set, so the proxy
-- is auditable. NULL for rows whose count was not recorded.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.tbl_Ref_HistoricFigure', 'SiteLinkCount') IS NULL
    ALTER TABLE dbo.tbl_Ref_HistoricFigure ADD SiteLinkCount SMALLINT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '181_add_historic_figure_sitelinks.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('181_add_historic_figure_sitelinks.sql', SYSUTCDATETIME(),
            'Adds tbl_Ref_HistoricFigure.SiteLinkCount (Wikipedia editions): ranking + GeoArea proxy input.');
GO
PRINT '181 applied: historic figure SiteLinkCount.';
GO
