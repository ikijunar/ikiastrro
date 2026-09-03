-- =====================================================================
-- 17 — tbl_Astro_Terminology + tbl_Astro_TerminologyText: the bilingual
-- concept catalogue (engine reorg, Plan 1). tbl_Astro_Terminology is the
-- language-neutral registry of engine concepts (planets, signs, houses,
-- nakshatras, vargas, karakas, avastha/dignity states, dashas, yogas,
-- ayanamsas, ...) keyed by a stable Code, with a self-referencing
-- ParentCode hierarchy. tbl_Astro_TerminologyText holds the per-language
-- (sa/en/ta) display + technical text, one row per (concept, language,
-- script). Purely additive; no rows seeded here. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/17_create_astro_terminology.sql
-- =====================================================================
USE [ikiastrro];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '17_create_astro_terminology.sql')
BEGIN
    IF OBJECT_ID('dbo.tbl_Astro_Terminology','U') IS NULL
    CREATE TABLE dbo.tbl_Astro_Terminology (
        TerminologyId   INT IDENTITY(1,1) CONSTRAINT PK_Astro_Terminology PRIMARY KEY,
        Category        VARCHAR(30)  NOT NULL,
        Code            VARCHAR(40)  NOT NULL CONSTRAINT UQ_Astro_Terminology_Code UNIQUE,
        ParentCode      VARCHAR(40)  NULL,
        EngineCode      VARCHAR(30)  NULL,
        NumericKey      INT          NULL,
        FormulaSummary  VARCHAR(300) NULL,
        DisplayOrder    INT          NOT NULL CONSTRAINT DF_Astro_Terminology_DisplayOrder DEFAULT 0,
        IsActive        BIT          NOT NULL CONSTRAINT DF_Astro_Terminology_IsActive DEFAULT 1,
        CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
            'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
            'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
            'Dasha','Yoga','Ayanamsa','Concept')),
        CONSTRAINT FK_Astro_Terminology_Parent FOREIGN KEY (ParentCode)
            REFERENCES dbo.tbl_Astro_Terminology (Code)
    );

    IF OBJECT_ID('dbo.tbl_Astro_TerminologyText','U') IS NULL
    CREATE TABLE dbo.tbl_Astro_TerminologyText (
        TerminologyTextId    INT IDENTITY(1,1) CONSTRAINT PK_Astro_TerminologyText PRIMARY KEY,
        TerminologyId        INT           NOT NULL,
        LanguageCode         CHAR(2)       NOT NULL,
        Script               VARCHAR(8)    NOT NULL CONSTRAINT DF_Astro_TerminologyText_Script DEFAULT 'Latn',
        Name                 NVARCHAR(100) NOT NULL,
        TraditionalName      NVARCHAR(100) NULL,
        ShortDescription     NVARCHAR(400) NULL,
        TechnicalDefinition  NVARCHAR(MAX) NULL,
        CalculationMethod    NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)   NULL,
        CONSTRAINT FK_Astro_TerminologyText FOREIGN KEY (TerminologyId)
            REFERENCES dbo.tbl_Astro_Terminology (TerminologyId),
        CONSTRAINT CK_Astro_TerminologyText_Lang   CHECK (LanguageCode IN ('sa','en','ta')),
        CONSTRAINT CK_Astro_TerminologyText_Script CHECK (Script IN ('Latn','Deva','Taml')),
        CONSTRAINT CK_Astro_TerminologyText_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Astro_TerminologyText UNIQUE (TerminologyId, LanguageCode, Script)
    );

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    SELECT '17_create_astro_terminology.sql',
           'tbl_Astro_Terminology + tbl_Astro_TerminologyText (bilingual concept catalogue)'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '17_create_astro_terminology.sql');

    PRINT '17 applied: terminology tables created.';
END
ELSE
    PRINT '17 already applied.';
GO
