-- =====================================================================
-- 23 — tbl_Dim_ChartType += ChartShortDescription: a short "Primary Domain" tag
-- for each chart type (e.g. D1 "Self / Life", D9 "Marriage / Dharma"), distinct
-- from the reader-facing Description column added in migration 22. Seeded for
-- the 16 vargas with a known primary domain; D2-US, D5, D6, D8, D11 have no
-- accepted domain tag yet and stay NULL until one is supplied.
-- Column add is COL_LENGTH-guarded; the seed is a set of UPDATEs (re-run resets
-- to the same values). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/23_add_charttype_shortdescription.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Dim_ChartType', 'ChartShortDescription') IS NULL
    ALTER TABLE dbo.tbl_Dim_ChartType ADD ChartShortDescription VARCHAR(60) NULL;
GO
UPDATE dbo.tbl_Dim_ChartType SET ChartShortDescription = v.Domain
FROM dbo.tbl_Dim_ChartType t
JOIN (VALUES
    ('D1',  'Self / Life'),
    ('D2',  'Wealth'),
    ('D3',  'Siblings / Courage'),
    ('D4',  'Property / Fortune'),
    ('D7',  'Children'),
    ('D9',  'Marriage / Dharma'),
    ('D10', 'Career'),
    ('D12', 'Parents / Ancestors'),
    ('D16', 'Vehicles / Comforts'),
    ('D20', 'Spirituality'),
    ('D24', 'Education'),
    ('D27', 'Strength / Weakness'),
    ('D30', 'Misfortune / Vulnerability'),
    ('D40', 'Maternal lineage'),
    ('D45', 'Paternal lineage / Character'),
    ('D60', 'Karma')
) AS v(Code, Domain) ON v.Code = t.Code
WHERE t.ChartShortDescription IS NULL OR t.ChartShortDescription <> v.Domain;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '23_add_charttype_shortdescription.sql', 'tbl_Dim_ChartType += ChartShortDescription; seeded for 16 vargas'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '23_add_charttype_shortdescription.sql');
GO
PRINT '23 applied: tbl_Dim_ChartType.ChartShortDescription added and seeded.';
GO
