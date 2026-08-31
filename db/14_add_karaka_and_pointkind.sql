-- =====================================================================
-- 14 — tbl_Chart_KeyDetails gains PointKind (row discriminator: Graha vs the
-- special points AL/A2..A12/HL/Gulika/Maandi) and CharaKaraka (Jaimini 8-karaka
-- label on the 8 graha rows). Feeds C# (ChartAnalyzer) and the Web combined chart.
-- CK_KeyDetails_NonGrahaNulls forbids graha-only analytics on non-Graha rows.
-- Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/14_add_karaka_and_pointkind.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'PointKind') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails
        ADD PointKind VARCHAR(12) NOT NULL
            CONSTRAINT DF_KeyDetails_PointKind DEFAULT 'Graha';
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'CharaKaraka') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CharaKaraka VARCHAR(4) NULL;
GO
IF OBJECT_ID('dbo.CK_KeyDetails_PointKind', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_PointKind
        CHECK (PointKind IN ('Graha','SpecialLagna','Arudha','Upagraha'));
GO
IF OBJECT_ID('dbo.CK_KeyDetails_CharaKaraka', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_CharaKaraka
        CHECK (CharaKaraka IS NULL
            OR CharaKaraka IN ('AK','AmK','BK','MK','PiK','PK','GK','DK'));
GO
IF OBJECT_ID('dbo.CK_KeyDetails_NonGrahaNulls', 'C') IS NULL
    ALTER TABLE dbo.tbl_Chart_KeyDetails WITH CHECK
        ADD CONSTRAINT CK_KeyDetails_NonGrahaNulls
        CHECK (PointKind = 'Graha'
            OR (PlanetId IS NULL AND DignityStatus IS NULL AND Nakshatra IS NULL
                AND CharaKaraka IS NULL AND AspectingPlanets IS NULL
                AND IsCombust IS NULL AND NakshatraLordPlanet IS NULL));
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '14_add_karaka_and_pointkind.sql',
       'tbl_Chart_KeyDetails += PointKind, CharaKaraka (+3 CHECKs)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations
                  WHERE ScriptName = '14_add_karaka_and_pointkind.sql');
GO
PRINT '14 applied: tbl_Chart_KeyDetails has PointKind + CharaKaraka.';
GO
