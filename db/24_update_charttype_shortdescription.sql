-- =====================================================================
-- 24 — tbl_Dim_ChartType.ChartShortDescription: corrected/complete seed.
-- Supersedes the 16-row seed from migration 23 with the full 21-row set
-- (fills in D2-US, D5, D6, D8, D11 that were previously NULL) and revises a
-- few wordings (D1, D10, D40, D45, D60). Every code currently in
-- tbl_Dim_ChartType now has a ChartShortDescription.
-- Idempotent (UPDATE only; the column itself was added in migration 23).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/24_update_charttype_shortdescription.sql
-- =====================================================================
USE [ikiastrro];
GO
UPDATE dbo.tbl_Dim_ChartType SET ChartShortDescription = v.Domain
FROM dbo.tbl_Dim_ChartType t
JOIN (VALUES
    ('D1',    'Self / Overall Life'),
    ('D2',    'Wealth'),
    ('D2-US', 'Wealth / Prosperity'),
    ('D3',    'Siblings / Courage'),
    ('D4',    'Property / Fortune'),
    ('D5',    'Power / Influence'),
    ('D6',    'Health / Disease'),
    ('D7',    'Children'),
    ('D8',    'Longevity / Obstacles'),
    ('D9',    'Marriage / Dharma'),
    ('D10',   'Career / Status'),
    ('D11',   'Struggle / Destruction'),
    ('D12',   'Parents / Ancestors'),
    ('D16',   'Vehicles / Comforts'),
    ('D20',   'Spirituality'),
    ('D24',   'Education'),
    ('D27',   'Strength / Weakness'),
    ('D30',   'Misfortune / Vulnerability'),
    ('D40',   'Maternal Lineage'),
    ('D45',   'Paternal Lineage / Character'),
    ('D60',   'Karma / Past-life Influences')
) AS v(Code, Domain) ON v.Code = t.Code;
-- Unconditional (not WHERE-guarded on ChartShortDescription <> v.Domain): the default
-- collation is case-insensitive, which would silently skip casing-only fixes like
-- 'Maternal lineage' -> 'Maternal Lineage'. Re-running just re-writes the same values.
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '24_update_charttype_shortdescription.sql', 'tbl_Dim_ChartType.ChartShortDescription: full 21-row seed, revises migration 23'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '24_update_charttype_shortdescription.sql');
GO
PRINT '24 applied: tbl_Dim_ChartType.ChartShortDescription updated for all 21 chart types.';
GO
