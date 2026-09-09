-- =====================================================================
-- 22 — tbl_Dim_ChartType += Description: a short reader-facing phrase for what the
-- chart signifies, shown in the South Indian chart template's dynamic page heading
-- (SouthIndianTemplate.razor, "<Name> — <Code> CHART – <Description>") instead of the
-- hardcoded "Personality, Expression, Logic" string it used to carry. Only D1 is seeded
-- for now (the template is D1-only); every other row's Description stays NULL until that
-- chart gets its own template/description copy — the page falls back to the chart's
-- Code when Description is NULL.
-- Column add is COL_LENGTH-guarded; the seed is a single UPDATE (re-run resets to the
-- same value). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/22_add_charttype_description.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Dim_ChartType', 'Description') IS NULL
    ALTER TABLE dbo.tbl_Dim_ChartType ADD [Description] VARCHAR(120) NULL;
GO
UPDATE dbo.tbl_Dim_ChartType SET [Description] = 'Personality, Expression, Logic' WHERE Code = 'D1';
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '22_add_charttype_description.sql', 'tbl_Dim_ChartType += Description; seeded for D1'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '22_add_charttype_description.sql');
GO
PRINT '22 applied: tbl_Dim_ChartType.Description added and seeded for D1.';
GO
