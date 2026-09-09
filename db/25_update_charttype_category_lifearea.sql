-- =====================================================================
-- 25 — tbl_Dim_ChartType.Category: life-area groupings, replacing the blanket 'Varga' value
-- every row has carried since migration 02. Drives the Web Workspace's varga rail (previously
-- grouped by hardcoded classical Shadvarga/Saptavarga/Dasavarga/Shodasavarga bundles in
-- Ikiastrro.Web's VargaBundles.cs — that hardcode is now replaced by this DB-sourced grouping,
-- rammyps's call, 2026-09-05). Six categories, derived from each chart's own ChartShortDescription
-- (migrations 23/24) rather than invented independently:
--   Self & Personality    — D1, D27, D30, D60
--   Wealth & Resources     — D2, D2-US, D4, D16
--   Health & Vitality      — D6, D8
--   Relationships & Family — D9, D3, D7, D12, D40, D45
--   Career & Status        — D10, D5, D24
--   Spirituality & Struggle — D20, D11
-- Every one of the 21 codes appears in exactly one category. Category widens VARCHAR(20) ->
-- VARCHAR(40) first — "Relationships & Family"/"Spirituality & Struggle" don't fit the old width,
-- sized back when every row just held the 5-char literal 'Varga'. Idempotent (ALTER COLUMN to
-- the same type, then UPDATE only — safe to re-run).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/25_update_charttype_category_lifearea.sql
-- =====================================================================
USE [ikiastrro];
GO
ALTER TABLE dbo.tbl_Dim_ChartType ALTER COLUMN Category VARCHAR(40) NOT NULL;
GO
UPDATE dbo.tbl_Dim_ChartType SET Category = v.LifeArea
FROM dbo.tbl_Dim_ChartType t
JOIN (VALUES
    ('D1',    'Self & Personality'),
    ('D27',   'Self & Personality'),
    ('D30',   'Self & Personality'),
    ('D60',   'Self & Personality'),
    ('D2',    'Wealth & Resources'),
    ('D2-US', 'Wealth & Resources'),
    ('D4',    'Wealth & Resources'),
    ('D16',   'Wealth & Resources'),
    ('D6',    'Health & Vitality'),
    ('D8',    'Health & Vitality'),
    ('D9',    'Relationships & Family'),
    ('D3',    'Relationships & Family'),
    ('D7',    'Relationships & Family'),
    ('D12',   'Relationships & Family'),
    ('D40',   'Relationships & Family'),
    ('D45',   'Relationships & Family'),
    ('D10',   'Career & Status'),
    ('D5',    'Career & Status'),
    ('D24',   'Career & Status'),
    ('D20',   'Spirituality & Struggle'),
    ('D11',   'Spirituality & Struggle')
) AS v(Code, LifeArea) ON v.Code = t.Code
WHERE t.Category <> v.LifeArea;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '25_update_charttype_category_lifearea.sql', 'tbl_Dim_ChartType.Category: 6 life-area groupings, replacing the blanket ''Varga'' value'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '25_update_charttype_category_lifearea.sql');
GO
PRINT '25 applied: tbl_Dim_ChartType.Category updated to 6 life-area groupings for all 21 chart types.';
GO
