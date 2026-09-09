-- 43 — preserve the full selected ayanamsha / engine provenance on chart facts.
-- The catalog includes descriptive labels longer than the original 100-character column.
-- Idempotent and safe for existing chart rows.

IF COL_LENGTH('dbo.tbl_ChartResults', 'Ayanamsha') IS NOT NULL
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN Ayanamsha NVARCHAR(200) NOT NULL;

IF COL_LENGTH('dbo.tbl_ChartResults', 'EngineVersion') IS NOT NULL
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN EngineVersion NVARCHAR(300) NOT NULL;

IF COL_LENGTH('dbo.tbl_ChartResults', 'VargaMethod') IS NOT NULL
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN VargaMethod NVARCHAR(100) NULL;

IF OBJECT_ID('dbo.SchemaMigrations', 'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '43_widen_chart_calculation_labels.sql')
    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('43_widen_chart_calculation_labels.sql', 'Widen chart calculation provenance labels for full ayanamsha and engine descriptions');
