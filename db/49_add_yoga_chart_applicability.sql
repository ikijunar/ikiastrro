-- =====================================================================
-- 49 — Track which chart(s) each source-attributed yoga variant needs.
-- One row per variant/chart keeps multi-varga requirements normalized.
-- =====================================================================
USE [ikiastrro];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '49_add_yoga_chart_applicability.sql')
BEGIN
    IF OBJECT_ID('dbo.tbl_Rule_YogaChartApplicability', 'U') IS NULL
    CREATE TABLE dbo.tbl_Rule_YogaChartApplicability
    (
        Id                INT IDENTITY(1,1) CONSTRAINT PK_Rule_YogaChartApplicability PRIMARY KEY,
        RuleSetId         TINYINT NOT NULL,
        SourceRefCode     VARCHAR(40) NOT NULL,
        SourceVariantCode VARCHAR(60) NOT NULL,
        ChartTypeId       TINYINT NOT NULL,
        RequirementRole   VARCHAR(20) NOT NULL,
        EvaluationScope   VARCHAR(20) NOT NULL CONSTRAINT DF_Rule_YogaChartApplicability_Scope DEFAULT ('NATAL'),
        MissingBehavior   VARCHAR(20) NOT NULL CONSTRAINT DF_Rule_YogaChartApplicability_Missing DEFAULT ('NOT_EVALUATED'),
        Notes             NVARCHAR(500) NULL,
        IsActive          BIT NOT NULL CONSTRAINT DF_Rule_YogaChartApplicability_IsActive DEFAULT (1),
        CONSTRAINT FK_Rule_YogaChartApplicability_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id),
        CONSTRAINT FK_Rule_YogaChartApplicability_Source FOREIGN KEY (SourceRefCode) REFERENCES dbo.tbl_Dim_Source (Code),
        CONSTRAINT FK_Rule_YogaChartApplicability_ChartType FOREIGN KEY (ChartTypeId) REFERENCES dbo.tbl_Dim_ChartType (Id),
        CONSTRAINT UQ_Rule_YogaChartApplicability UNIQUE (RuleSetId, SourceRefCode, SourceVariantCode, ChartTypeId),
        CONSTRAINT CK_Rule_YogaChartApplicability_Role CHECK (RequirementRole IN ('FOUNDATION','REQUIRED','CONFIRMATORY')),
        CONSTRAINT CK_Rule_YogaChartApplicability_Scope CHECK (EvaluationScope IN ('NATAL','DIVISIONAL','TRANSIT','UNIVERSAL')),
        CONSTRAINT CK_Rule_YogaChartApplicability_Missing CHECK (MissingBehavior IN ('NOT_EVALUATED','OPTIONAL','FALLBACK'))
    );

    INSERT dbo.tbl_Rule_Catalog
        (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    SELECT 'tbl_Rule_YogaChartApplicability', 'YOGA', 'CHART_REQUIREMENT',
        'Required chart inputs for each source-attributed yoga variant.',
        'migration 49'
    WHERE NOT EXISTS
        (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_YogaChartApplicability');

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('49_add_yoga_chart_applicability.sql',
        'Normalized D1/D9 requirement tracking for source-attributed yoga variants');
END
GO

DECLARE @ruleSetId TINYINT = (SELECT TOP (1) Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1 ORDER BY VersionNumber DESC);
DECLARE @d1 TINYINT = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D1');
DECLARE @d9 TINYINT = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D9');

IF @ruleSetId IS NULL OR @d1 IS NULL OR @d9 IS NULL
    THROW 50049, 'Migration 49 requires an active rule set and registered D1/D9 chart types.', 1;

DECLARE @variants TABLE
(
    SourceRefCode VARCHAR(60) NOT NULL,
    SourceVariantCode VARCHAR(60) NOT NULL,
    NeedsD9 BIT NOT NULL
);

DECLARE @n INT = 1;
WHILE @n <= 300
BEGIN
    IF @n NOT IN (49, 51, 52, 53, 54, 79, 81, 87, 89)
        INSERT @variants VALUES
            ('SRC_RAMAN_300_COMBINATIONS', CONCAT('RAMAN_300_', RIGHT(CONCAT('000', @n), 3)),
             CASE WHEN @n IN (28,29,46,57,62,66,68,113,114,131,135,137,153,169,172,175,176,182,183,184,185,186,192,197,199) THEN 1 ELSE 0 END);
    SET @n += 1;
END;

INSERT @variants VALUES
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_049_CLASSICAL',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_049_OBSERVED',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_051_A',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_051_B',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_051_C',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_052_PRIMARY',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_052_RAO',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_053_STRICT',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_053_RELAXED',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_054_NAVAMSA',1),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_054_RASI',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H02',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H03',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H05',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H06',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H08',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H09',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H11',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_079_H12',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_081_H01',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_081_H04',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_081_H07',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_081_H10',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_087_H02',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_087_H03',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_087_H04',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_089_PANAPARA',0),
('SRC_RAMAN_300_COMBINATIONS','RAMAN_300_089_APOKLIMA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_GAJAKESARI',0),
('SRC_PVR_INTEGRATED','PVR_CH11_SUNAPHA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_ANAPHA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_DURADHARA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_KEMADRUMA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_CHANDRA_MANGALA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_VESI',0),
('SRC_PVR_INTEGRATED','PVR_CH11_VOSI',0),
('SRC_PVR_INTEGRATED','PVR_CH11_UBHAYACHARA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_BUDHA_ADITYA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_RUCHAKA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_BHADRA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_HAMSA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_MALAVYA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_SASA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_ADHI',0),
('SRC_PVR_INTEGRATED','PVR_CH11_AMALA',0),
('SRC_PVR_INTEGRATED','PVR_CH11_PARVATA',0);

INSERT dbo.tbl_Rule_YogaChartApplicability
    (RuleSetId, SourceRefCode, SourceVariantCode, ChartTypeId, RequirementRole, EvaluationScope, MissingBehavior, Notes)
SELECT @ruleSetId, v.SourceRefCode, v.SourceVariantCode, @d1, 'FOUNDATION', 'NATAL', 'NOT_EVALUATED',
       N'D1 is the foundational chart for this yoga definition.'
FROM @variants v
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tbl_Rule_YogaChartApplicability x
    WHERE x.RuleSetId = @ruleSetId AND x.SourceRefCode = v.SourceRefCode
      AND x.SourceVariantCode = v.SourceVariantCode AND x.ChartTypeId = @d1
);

INSERT dbo.tbl_Rule_YogaChartApplicability
    (RuleSetId, SourceRefCode, SourceVariantCode, ChartTypeId, RequirementRole, EvaluationScope, MissingBehavior, Notes)
SELECT @ruleSetId, v.SourceRefCode, v.SourceVariantCode, @d9, 'REQUIRED', 'DIVISIONAL', 'NOT_EVALUATED',
       N'D9 placement is required by the source definition; absence must not be reported as yoga absence.'
FROM @variants v
WHERE v.NeedsD9 = 1
  AND NOT EXISTS
(
    SELECT 1 FROM dbo.tbl_Rule_YogaChartApplicability x
    WHERE x.RuleSetId = @ruleSetId AND x.SourceRefCode = v.SourceRefCode
      AND x.SourceVariantCode = v.SourceVariantCode AND x.ChartTypeId = @d9
);
GO

CREATE OR ALTER VIEW dbo.vw_YogaChartApplicability
AS
SELECT a.RuleSetId, a.SourceRefCode, a.SourceVariantCode,
       ct.Code AS ChartCode, ct.DisplayName AS ChartName,
       a.RequirementRole, a.EvaluationScope, a.MissingBehavior, a.Notes, a.IsActive
FROM dbo.tbl_Rule_YogaChartApplicability a
JOIN dbo.tbl_Dim_ChartType ct ON ct.Id = a.ChartTypeId;
GO

DECLARE @tracked INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_YogaChartApplicability);
DECLARE @multiChart INT =
(
    SELECT COUNT(*) FROM
    (
        SELECT RuleSetId, SourceRefCode, SourceVariantCode
        FROM dbo.tbl_Rule_YogaChartApplicability
        GROUP BY RuleSetId, SourceRefCode, SourceVariantCode
        HAVING COUNT(*) > 1
    ) x
);
PRINT '49 applied: chart-requirement rows=' + CAST(@tracked AS VARCHAR(10))
    + '; multi-chart variants=' + CAST(@multiChart AS VARCHAR(10));
GO
