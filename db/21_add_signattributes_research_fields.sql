-- =====================================================================
-- 21 — tbl_SignAttributes: four researched classification columns + seed.
--   Fertility        Barren / Semi-fertile / Fertile
--                    Traditional horary + Vedic Prashna consensus:
--                    water signs (Cn, Sc, Pi) fertile; Ge/Le/Vi barren
--                    (Vedic sources add Ar); Ar & Aq set Barren by choice.
--   SignColour       Classical rasi-varna (Saravali / Phaladeepika tradition).
--   Ritu             Hindu season by the Sun's sidereal transit
--                    (Vasanta -> Grishma -> Varsha -> Sharad -> Hemanta -> Shishira,
--                    two signs each starting Aries). An alternative
--                    shadbala "ritu-lord" mapping exists; not used here.
--   AscensionLength  Short (Capricorn..Gemini) / Long (Cancer..Sagittarius)
--                    for NORTHERN latitudes; reversed in the south.
-- Column adds are COL_LENGTH-guarded; the seed is one authoritative UPDATE
-- via a VALUES CTE (re-run resets to the same values). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/21_add_signattributes_research_fields.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Fertility')       IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Fertility]       varchar(12) NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'SignColour')      IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [SignColour]      varchar(15) NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Ritu')            IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Ritu]            varchar(10) NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'AscensionLength') IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [AscensionLength] varchar(5)  NULL;
GO
;WITH c (Id, Fertility, SignColour, Ritu, AscensionLength) AS (
    SELECT * FROM (VALUES
        ( 1, 'Barren',       'Blood-red',    'Vasanta',  'Short'),
        ( 2, 'Semi-fertile', 'White',        'Vasanta',  'Short'),
        ( 3, 'Barren',       'Green',        'Grishma',  'Short'),
        ( 4, 'Fertile',      'Pale rose',    'Grishma',  'Long'),
        ( 5, 'Barren',       'Pale yellow',  'Varsha',   'Long'),
        ( 6, 'Barren',       'Variegated',   'Varsha',   'Long'),
        ( 7, 'Semi-fertile', 'Blue-black',   'Sharad',   'Long'),
        ( 8, 'Fertile',      'Golden-brown', 'Sharad',   'Long'),
        ( 9, 'Semi-fertile', 'Golden',       'Hemanta',  'Long'),
        (10, 'Semi-fertile', 'Variegated',   'Hemanta',  'Short'),
        (11, 'Barren',       'Deep brown',   'Shishira', 'Short'),
        (12, 'Fertile',      'White',        'Shishira', 'Short')
    ) v (Id, Fertility, SignColour, Ritu, AscensionLength)
)
UPDATE sa
   SET sa.Fertility       = c.Fertility,
       sa.SignColour      = c.SignColour,
       sa.Ritu            = c.Ritu,
       sa.AscensionLength = c.AscensionLength
  FROM dbo.tbl_SignAttributes sa
  JOIN c ON c.Id = sa.Id;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '21_add_signattributes_research_fields.sql', 'tbl_SignAttributes += Fertility/SignColour/Ritu/AscensionLength; seeded for all 12 signs'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '21_add_signattributes_research_fields.sql');
GO
PRINT '21 applied: tbl_SignAttributes research fields added and seeded.';
GO
