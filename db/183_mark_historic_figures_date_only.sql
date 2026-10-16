-- =====================================================================
-- 183 - Historic figures are DATE-ONLY: no confirmed birth time.
--
-- Wikidata supplies no trustworthy birth time, so every row has TimeOfBirth NULL
-- and TimeQuality UNKNOWN. IsDateOnly makes that explicit for consumers: such rows
-- are never built into charts (no lagna / houses / dasha) and are kept as reference
-- data for a future statistical layer (to be defined). A row becomes chart-eligible
-- only if a rated source later supplies TimeOfBirth (consistency check in 179).
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.tbl_Ref_HistoricFigure', 'IsDateOnly') IS NULL
    ALTER TABLE dbo.tbl_Ref_HistoricFigure
        ADD IsDateOnly AS (CAST(CASE WHEN TimeOfBirth IS NULL THEN 1 ELSE 0 END AS BIT)) PERSISTED;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '183_mark_historic_figures_date_only.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('183_mark_historic_figures_date_only.sql', SYSUTCDATETIME(),
            'tbl_Ref_HistoricFigure.IsDateOnly (computed): no birth time => not chart-built; statistical layer TBD.');
GO
PRINT '183 applied: historic figures marked date-only.';
GO
