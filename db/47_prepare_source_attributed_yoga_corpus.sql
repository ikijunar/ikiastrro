-- =====================================================================
-- 47 — Prepare tbl_Rule_Yoga for source-attributed Raman/PVR variants.
-- OCR text is research input, not implementation evidence, until each
-- entry is checked against its scan page.
-- =====================================================================
USE [ikiastrro];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '47_prepare_source_attributed_yoga_corpus.sql')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_Source WHERE Code = 'SRC_RAMAN_300_COMBINATIONS')
        INSERT dbo.tbl_Dim_Source (Code, Title, Author, Edition, Tradition, Notes)
        VALUES ('SRC_RAMAN_300_COMBINATIONS', N'Three Hundred Important Combinations',
            N'B. V. Raman', N'Ninth edition 1983; tenth-edition Delhi reprint 1994',
            'Raman', N'ISBN 81-208-0843-6 cloth / 81-208-0850-9 paper; complete local DJVU and OCR draft');

    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceLocator') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceLocator VARCHAR(120) NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceEntryNumber') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceEntryNumber SMALLINT NULL;
    IF COL_LENGTH('dbo.tbl_Rule_Yoga', 'SourceVariantCode') IS NULL
        ALTER TABLE dbo.tbl_Rule_Yoga ADD SourceVariantCode VARCHAR(60) NULL;

    IF OBJECT_ID('dbo.CK_Rule_Yoga_SourceEntryNumber', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.tbl_Rule_Yoga ADD CONSTRAINT CK_Rule_Yoga_SourceEntryNumber
            CHECK (SourceEntryNumber IS NULL OR SourceEntryNumber BETWEEN 1 AND 300);');
    IF OBJECT_ID('dbo.UQ_Rule_Yoga_SourceVariant', 'UQ') IS NULL
        EXEC(N'ALTER TABLE dbo.tbl_Rule_Yoga ADD CONSTRAINT UQ_Rule_Yoga_SourceVariant
            UNIQUE (RuleSetId, YogaCode, SourceRefCode, SourceVariantCode);');

    UPDATE dbo.tbl_Rule_Catalog
       SET MethodCodes = 'PREDICATE_SET,SOURCE_VARIANT',
           Purpose = 'Source-attributed yoga definitions: predicates, qualifications, cancellations, outcomes and exact locators.'
     WHERE RuleTableName = 'tbl_Rule_Yoga';

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('47_prepare_source_attributed_yoga_corpus.sql',
        'SRC_RAMAN_300_COMBINATIONS + locator, entry number and source-variant identity for tbl_Rule_Yoga');
END
GO

DECLARE @sourceCount INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_Source WHERE Code = 'SRC_RAMAN_300_COMBINATIONS');
PRINT '47 applied: Raman yoga sources=' + CAST(@sourceCount AS VARCHAR(10))
    + '; predicates remain gated on scan verification.';
GO
