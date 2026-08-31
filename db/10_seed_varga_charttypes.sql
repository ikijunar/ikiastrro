-- =====================================================================
-- 10 — tbl_Dim_ChartType: D2-US + the 14 Plan-A vargas (Ids 7..21).
-- Ids 22..24 are reserved for Plan B (D81/D108/D144). DisplayOrder
-- continues 7..21 after the existing 1..6 rows. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/10_seed_varga_charttypes.sql
-- =====================================================================
USE [ikiastrro];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType WHERE Id = 7)
INSERT dbo.tbl_Dim_ChartType (Id, Code, DisplayName, DivisionalFactor, Category, DisplayOrder) VALUES
    ( 7, 'D2-US', 'Hora (Uma Shambu)', 2,  'Varga',  7),
    ( 8, 'D3',    'Drekkana',          3,  'Varga',  8),
    ( 9, 'D4',    'Chaturthamsa',      4,  'Varga',  9),
    (10, 'D5',    'Panchamsa',         5,  'Varga', 10),
    (11, 'D7',    'Saptamsa',          7,  'Varga', 11),
    (12, 'D8',    'Ashtamsa',          8,  'Varga', 12),
    (13, 'D12',   'Dwadasamsa',        12, 'Varga', 13),
    (14, 'D16',   'Shodasamsa',        16, 'Varga', 14),
    (15, 'D20',   'Vimsamsa',          20, 'Varga', 15),
    (16, 'D24',   'Siddhamsa',         24, 'Varga', 16),
    (17, 'D27',   'Nakshatramsa',      27, 'Varga', 17),
    (18, 'D30',   'Trimsamsa',         30, 'Varga', 18),
    (19, 'D40',   'Khavedamsa',        40, 'Varga', 19),
    (20, 'D45',   'Akshavedamsa',      45, 'Varga', 20),
    (21, 'D60',   'Shashtyamsa',       60, 'Varga', 21);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '10_seed_varga_charttypes.sql', 'D2-US + 14 Plan-A varga chart types (Id 7..21)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '10_seed_varga_charttypes.sql');
GO
PRINT '10 applied: tbl_Dim_ChartType has D2-US + 14 vargas.';
GO
