USE [ikiastrro];
GO
SET XACT_ABORT ON;
GO
IF COL_LENGTH('dbo.tbl_BirthDetails', 'Sex') IS NULL
    ALTER TABLE dbo.tbl_BirthDetails ADD Sex VARCHAR(6) NULL;
GO
IF OBJECT_ID('dbo.CK_BirthDetails_Sex', 'C') IS NULL
    ALTER TABLE dbo.tbl_BirthDetails ADD CONSTRAINT CK_BirthDetails_Sex
    CHECK (Sex IS NULL OR Sex IN ('Male','Female'));
GO
IF OBJECT_ID('dbo.tbl_Fact_YogaInputEvaluations', 'U') IS NULL
CREATE TABLE dbo.tbl_Fact_YogaInputEvaluations
(
    Id INT IDENTITY(1,1) CONSTRAINT PK_Fact_YogaInputEvaluations PRIMARY KEY,
    ChartResultId INT NOT NULL,
    RuleSetId TINYINT NOT NULL,
    SourceRefCode VARCHAR(40) NOT NULL,
    SourceVariantCode VARCHAR(60) NOT NULL,
    YogaCode VARCHAR(40) NOT NULL,
    SourceLocator VARCHAR(120) NOT NULL,
    Present BIT NULL,
    EvaluationStatus VARCHAR(20) NOT NULL,
    MissingRequirementCodesJson NVARCHAR(MAX) NOT NULL,
    SubjectSex VARCHAR(6) NULL,
    IsNightBirth BIT NULL,
    ElongationDegrees FLOAT NULL,
    IsWaxingMoon BIT NULL,
    IsFullMoon BIT NULL,
    LunarPhasePolicyCode VARCHAR(60) NULL,
    SunriseMethodCode VARCHAR(60) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    ComputedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Fact_YogaInputEvaluations_ChartResult FOREIGN KEY (ChartResultId)
        REFERENCES dbo.tbl_ChartResults(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Fact_YogaInputEvaluations_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets(Id),
    CONSTRAINT FK_Fact_YogaInputEvaluations_Source FOREIGN KEY (SourceRefCode) REFERENCES dbo.tbl_Dim_Source(Code),
    CONSTRAINT UQ_Fact_YogaInputEvaluations UNIQUE (ChartResultId, SourceRefCode, SourceVariantCode),
    CONSTRAINT CK_Fact_YogaInputEvaluations_Json CHECK (ISJSON(MissingRequirementCodesJson) = 1),
    CONSTRAINT CK_Fact_YogaInputEvaluations_Status CHECK
        ((EvaluationStatus = 'NOT_EVALUATED' AND Present IS NULL)
         OR (EvaluationStatus = 'EVALUATED' AND Present IS NOT NULL))
);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '052_add_birth_sex_and_yoga_input_facts.sql',
       'Optional user-entered sex and auditable evaluation of seven yoga context requirements.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName='052_add_birth_sex_and_yoga_input_facts.sql');
GO
