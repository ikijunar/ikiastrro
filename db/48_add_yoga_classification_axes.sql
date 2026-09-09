-- =====================================================================
-- 48 — Orthogonal yoga classification axes.
-- Strength is not folded into nature or formation family. Source-stated
-- importance remains distinct from per-chart evaluated strength.
-- =====================================================================
USE [ikiastrro];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '48_add_yoga_classification_axes.sql')
BEGIN
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'FormationFamilyCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD FormationFamilyCode VARCHAR(30) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'OutcomeNatureCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD OutcomeNatureCode VARCHAR(20) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceStrengthClassCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceStrengthClassCode VARCHAR(20) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceCategoryCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceCategoryCode VARCHAR(40) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceCorpusCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceCorpusCode VARCHAR(20) NULL;

    IF OBJECT_ID('dbo.CK_Rule_Yoga_OutcomeNature', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.tbl_Rule_Yoga ADD CONSTRAINT CK_Rule_Yoga_OutcomeNature CHECK
            (OutcomeNatureCode IS NULL OR OutcomeNatureCode IN (''AUSPICIOUS'',''INAUSPICIOUS'',''MIXED'',''CONTEXTUAL''));');
    IF OBJECT_ID('dbo.CK_Rule_Yoga_SourceStrengthClass', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.tbl_Rule_Yoga ADD CONSTRAINT CK_Rule_Yoga_SourceStrengthClass CHECK
            (SourceStrengthClassCode IS NULL OR SourceStrengthClassCode IN (''MAJOR'',''MODERATE'',''MINOR'',''SOURCE_UNSPECIFIED''));');
    IF OBJECT_ID('dbo.CK_Rule_Yoga_SourceCorpus', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.tbl_Rule_Yoga ADD CONSTRAINT CK_Rule_Yoga_SourceCorpus CHECK
            (SourceCorpusCode IS NULL OR SourceCorpusCode IN (''BVR-300'',''PVR-SPECIFIC'',''OTHERS''));');

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('48_add_yoga_classification_axes.sql',
        'tbl_Rule_Yoga formation family, outcome nature, source strength class, source category and source corpus axes');
END
GO
