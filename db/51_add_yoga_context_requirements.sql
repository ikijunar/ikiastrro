-- =====================================================================
-- 51 — Normalize non-chart inputs required by source-attributed yogas.
-- Missing required context must yield NOT_EVALUATED, never ABSENT.
-- =====================================================================
USE [ikiastrro];
GO

IF OBJECT_ID('dbo.tbl_Rule_YogaContextRequirement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_YogaContextRequirement
    (
        Id                   INT IDENTITY(1,1) CONSTRAINT PK_Rule_YogaContextRequirement PRIMARY KEY,
        RuleSetId            TINYINT NOT NULL,
        SourceRefCode        VARCHAR(40) NOT NULL,
        SourceVariantCode    VARCHAR(60) NOT NULL,
        RequirementCode      VARCHAR(30) NOT NULL,
        DerivationMethodCode VARCHAR(40) NULL,
        MissingBehavior      VARCHAR(20) NOT NULL CONSTRAINT DF_Rule_YogaContextRequirement_Missing DEFAULT ('NOT_EVALUATED'),
        Notes                NVARCHAR(500) NULL,
        IsActive             BIT NOT NULL CONSTRAINT DF_Rule_YogaContextRequirement_IsActive DEFAULT (1),
        CONSTRAINT FK_Rule_YogaContextRequirement_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id),
        CONSTRAINT FK_Rule_YogaContextRequirement_Source FOREIGN KEY (SourceRefCode) REFERENCES dbo.tbl_Dim_Source (Code),
        CONSTRAINT UQ_Rule_YogaContextRequirement UNIQUE
            (RuleSetId, SourceRefCode, SourceVariantCode, RequirementCode),
        CONSTRAINT CK_Rule_YogaContextRequirement_Code CHECK
            (RequirementCode IN ('BIRTH_DAY_NIGHT','MOON_WAXING','MOON_FULL','SUBJECT_SEX','EXACT_LONGITUDE')),
        CONSTRAINT CK_Rule_YogaContextRequirement_Missing CHECK
            (MissingBehavior IN ('NOT_EVALUATED','OPTIONAL','FALLBACK'))
    );
END
GO

IF OBJECT_ID('dbo.tbl_Rule_Catalog', 'U') IS NULL
    THROW 50051, 'Migration 51 requires dbo.tbl_Rule_Catalog.', 1;

INSERT dbo.tbl_Rule_Catalog
    (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
SELECT 'tbl_Rule_YogaContextRequirement', 'YOGA', 'CONTEXT_REQUIREMENT',
       'Non-chart inputs and derived context required to evaluate each source-attributed yoga variant.',
       '51_add_yoga_context_requirements.sql'
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tbl_Rule_Catalog
    WHERE RuleTableName = 'tbl_Rule_YogaContextRequirement'
);
GO

DECLARE @ruleSetId TINYINT =
(
    SELECT TOP (1) Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1 ORDER BY Id DESC
);

IF @ruleSetId IS NULL
    THROW 50052, 'Migration 51 requires an active rule set.', 1;

INSERT dbo.tbl_Rule_YogaContextRequirement
    (RuleSetId, SourceRefCode, SourceVariantCode, RequirementCode,
     DerivationMethodCode, MissingBehavior, Notes)
SELECT @ruleSetId, 'SRC_RAMAN_300_COMBINATIONS', v.SourceVariantCode,
       v.RequirementCode, v.DerivationMethodCode, 'NOT_EVALUATED', v.Notes
FROM (VALUES
    ('RAMAN_300_025', 'SUBJECT_SEX',     NULL,
     N'Raman states separate male and female branches; sex must be supplied and must not be inferred.'),
    ('RAMAN_300_025', 'BIRTH_DAY_NIGHT', 'ASTRONOMICAL_SUNRISE_SUNSET',
     N'Day or night is part of the source predicate.'),
    ('RAMAN_300_058', 'EXACT_LONGITUDE', NULL,
     N'Deep exaltation requires exact planetary longitude, not exaltation-sign membership alone.'),
    ('RAMAN_300_059', 'EXACT_LONGITUDE', NULL,
     N'Deep exaltation requires exact planetary longitude, not exaltation-sign membership alone.'),
    ('RAMAN_300_066', 'BIRTH_DAY_NIGHT', 'ASTRONOMICAL_SUNRISE_SUNSET',
     N'Day or night is part of the source predicate.'),
    ('RAMAN_300_066', 'MOON_WAXING',     'SUN_MOON_ELONGATION',
     N'Waxing Moon status is part of the source predicate.'),
    ('RAMAN_300_068', 'MOON_FULL',       'SUN_MOON_ELONGATION',
     N'Full-Moon status is part of the source predicate; the evaluator must retain its threshold provenance.')
) v (SourceVariantCode, RequirementCode, DerivationMethodCode, Notes)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tbl_Rule_YogaContextRequirement x
    WHERE x.RuleSetId = @ruleSetId
      AND x.SourceRefCode = 'SRC_RAMAN_300_COMBINATIONS'
      AND x.SourceVariantCode = v.SourceVariantCode
      AND x.RequirementCode = v.RequirementCode
);
GO

CREATE OR ALTER VIEW dbo.vw_YogaContextRequirements
AS
SELECT r.RuleSetId, r.SourceRefCode, s.Title AS SourceTitle,
       r.SourceVariantCode, r.RequirementCode, r.DerivationMethodCode,
       r.MissingBehavior, r.Notes, r.IsActive
FROM dbo.tbl_Rule_YogaContextRequirement r
JOIN dbo.tbl_Dim_Source s ON s.Code = r.SourceRefCode;
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '51_add_yoga_context_requirements.sql',
       'Normalize known yoga sex, day/night, lunar-phase, and exact-longitude requirements.'
WHERE NOT EXISTS
    (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '51_add_yoga_context_requirements.sql');
GO

DECLARE @tracked INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_YogaContextRequirement WHERE IsActive = 1);
PRINT '51 applied: active yoga context requirements=' + CAST(@tracked AS VARCHAR(10));
GO
