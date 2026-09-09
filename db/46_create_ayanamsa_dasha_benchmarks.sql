-- 46 — durable ayanamsa/Vimshottari benchmark schema and Ramakrishnan P golden record.
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.tbl_Dim_DashaSystems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_DashaSystems
    (
        Id                   INT IDENTITY(1,1) CONSTRAINT PK_Dim_DashaSystems PRIMARY KEY,
        Code                 VARCHAR(40) NOT NULL CONSTRAINT UQ_Dim_DashaSystems_Code UNIQUE,
        DisplayName          NVARCHAR(80) NOT NULL,
        Family               VARCHAR(30) NOT NULL,
        IsConditional        BIT NOT NULL,
        PrimaryUse           NVARCHAR(300) NOT NULL,
        ImplementationStatus VARCHAR(20) NOT NULL,
        SourceRefCode        VARCHAR(40) NOT NULL CONSTRAINT FK_Dim_DashaSystems_Source
                             FOREIGN KEY REFERENCES dbo.tbl_Dim_Source (Code),
        CONSTRAINT CK_Dim_DashaSystems_Family CHECK (Family IN ('NakshatraGraha','Rasi','Specialized')),
        CONSTRAINT CK_Dim_DashaSystems_Status CHECK (ImplementationStatus IN ('Implemented','Planned','Research'))
    );
END;

IF OBJECT_ID('dbo.tbl_Rule_DashaApplicability', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_DashaApplicability
    (
        Id               INT IDENTITY(1,1) CONSTRAINT PK_Rule_DashaApplicability PRIMARY KEY,
        RuleSetId        TINYINT NOT NULL CONSTRAINT FK_Rule_DashaApplicability_RuleSet
                         FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        DashaSystemId    INT NOT NULL CONSTRAINT FK_Rule_DashaApplicability_DashaSystem
                         FOREIGN KEY REFERENCES dbo.tbl_Dim_DashaSystems (Id),
        RuleCode         VARCHAR(60) NOT NULL,
        EvaluationKey    VARCHAR(60) NOT NULL,
        ConditionSummary NVARCHAR(500) NOT NULL,
        SourceRefCode    VARCHAR(40) NOT NULL CONSTRAINT FK_Rule_DashaApplicability_Source
                         FOREIGN KEY REFERENCES dbo.tbl_Dim_Source (Code),
        CONSTRAINT UQ_Rule_DashaApplicability_RuleSetCode UNIQUE (RuleSetId, RuleCode)
    );
END;

IF OBJECT_ID('dbo.tbl_Dim_AyanamsaBenchmarkCases', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_AyanamsaBenchmarkCases
    (
        Id                       INT IDENTITY(1,1) CONSTRAINT PK_Dim_AyanamsaBenchmarkCases PRIMARY KEY,
        Code                     VARCHAR(60) NOT NULL CONSTRAINT UQ_Dim_AyanamsaBenchmarkCases_Code UNIQUE,
        BirthDetailId            INT NULL CONSTRAINT FK_Dim_AyanamsaBenchmarkCases_BirthDetail
                                 FOREIGN KEY REFERENCES dbo.tbl_BirthDetails (Id),
        PersonName               NVARCHAR(200) NOT NULL,
        BirthDate                DATE NOT NULL,
        BirthTime                TIME(0) NOT NULL,
        UtcOffset                VARCHAR(10) NOT NULL,
        LatitudeDegrees          DECIMAL(12,8) NOT NULL,
        LongitudeDegrees         DECIMAL(12,8) NOT NULL,
        ReferenceAyanamsaDegrees DECIMAL(12,8) NOT NULL,
        ReferenceSourceRefCode   VARCHAR(40) NOT NULL CONSTRAINT FK_Dim_AyanamsaBenchmarkCases_Source
                                 FOREIGN KEY REFERENCES dbo.tbl_Dim_Source (Code),
        VimshottariDaysPerYear   DECIMAL(10,6) NOT NULL,
        Notes                    NVARCHAR(500) NULL,
        CreatedAtUtc             DATETIME2(0) NOT NULL CONSTRAINT DF_Dim_AyanamsaBenchmarkCases_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Dim_AyanamsaBenchmarkCases_Latitude CHECK (LatitudeDegrees BETWEEN -90 AND 90),
        CONSTRAINT CK_Dim_AyanamsaBenchmarkCases_Longitude CHECK (LongitudeDegrees BETWEEN -180 AND 180)
    );
END;

IF OBJECT_ID('dbo.tbl_Dim_AyanamsaBenchmarkPositions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_AyanamsaBenchmarkPositions
    (
        Id                      INT IDENTITY(1,1) CONSTRAINT PK_Dim_AyanamsaBenchmarkPositions PRIMARY KEY,
        AyanamsaBenchmarkCaseId INT NOT NULL CONSTRAINT FK_Dim_AyanamsaBenchmarkPositions_Case
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_AyanamsaBenchmarkCases (Id),
        BodyCode                VARCHAR(20) NOT NULL,
        SiderealLongitudeDegrees DECIMAL(12,8) NOT NULL,
        CONSTRAINT UQ_Dim_AyanamsaBenchmarkPositions_CaseBody UNIQUE (AyanamsaBenchmarkCaseId, BodyCode),
        CONSTRAINT CK_Dim_AyanamsaBenchmarkPositions_Longitude CHECK (SiderealLongitudeDegrees >= 0 AND SiderealLongitudeDegrees < 360)
    );
END;

IF OBJECT_ID('dbo.tbl_Dim_DashaBenchmarkPeriods', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_DashaBenchmarkPeriods
    (
        Id                      INT IDENTITY(1,1) CONSTRAINT PK_Dim_DashaBenchmarkPeriods PRIMARY KEY,
        AyanamsaBenchmarkCaseId INT NOT NULL CONSTRAINT FK_Dim_DashaBenchmarkPeriods_Case
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_AyanamsaBenchmarkCases (Id),
        DashaSystemId           INT NOT NULL CONSTRAINT FK_Dim_DashaBenchmarkPeriods_System
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_DashaSystems (Id),
        LevelNumber             TINYINT NOT NULL,
        SequenceNumber          SMALLINT NOT NULL,
        LordCode                VARCHAR(20) NOT NULL,
        StartDate               DATE NOT NULL,
        CONSTRAINT UQ_Dim_DashaBenchmarkPeriods_CaseSystemLevelSequence
            UNIQUE (AyanamsaBenchmarkCaseId, DashaSystemId, LevelNumber, SequenceNumber),
        CONSTRAINT CK_Dim_DashaBenchmarkPeriods_Level CHECK (LevelNumber BETWEEN 1 AND 6)
    );
END;

IF OBJECT_ID('dbo.tbl_Fact_AyanamsaComparisonRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_AyanamsaComparisonRuns
    (
        Id                        INT IDENTITY(1,1) CONSTRAINT PK_Fact_AyanamsaComparisonRuns PRIMARY KEY,
        AyanamsaBenchmarkCaseId   INT NOT NULL CONSTRAINT FK_Fact_AyanamsaComparisonRuns_Case
                                  FOREIGN KEY REFERENCES dbo.tbl_Dim_AyanamsaBenchmarkCases (Id),
        AyanamsaRuleId            INT NOT NULL CONSTRAINT FK_Fact_AyanamsaComparisonRuns_Ayanamsa
                                  FOREIGN KEY REFERENCES dbo.tbl_Rule_Ayanamsa (Id),
        StatusCode                VARCHAR(20) NOT NULL,
        CalculatedAyanamsaDegrees DECIMAL(12,8) NULL,
        AyanamsaErrorArcSeconds   DECIMAL(14,6) NULL,
        LocalMeanTimeCorrectionSeconds INT NULL,
        LocalSiderealTimeHours    DECIMAL(12,8) NULL,
        ComparedBodyCount         TINYINT NOT NULL CONSTRAINT DF_Fact_AyanamsaComparisonRuns_BodyCount DEFAULT 0,
        MeanAbsoluteErrorArcSeconds DECIMAL(14,6) NULL,
        RootMeanSquareErrorArcSeconds DECIMAL(14,6) NULL,
        MaximumErrorArcSeconds    DECIMAL(14,6) NULL,
        MaximumErrorBodyCode      VARCHAR(20) NULL,
        RankNumber                SMALLINT NULL,
        EngineVersion             VARCHAR(200) NOT NULL,
        FailureReason             NVARCHAR(500) NULL,
        CalculatedAtUtc           DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_AyanamsaComparisonRuns_CalculatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Fact_AyanamsaComparisonRuns_Status CHECK (StatusCode IN ('Completed','Unsupported','Failed'))
    );
END;

IF OBJECT_ID('dbo.tbl_Fact_AyanamsaPositionComparisons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_AyanamsaPositionComparisons
    (
        Id                         INT IDENTITY(1,1) CONSTRAINT PK_Fact_AyanamsaPositionComparisons PRIMARY KEY,
        AyanamsaComparisonRunId    INT NOT NULL CONSTRAINT FK_Fact_AyanamsaPositionComparisons_Run
                                   FOREIGN KEY REFERENCES dbo.tbl_Fact_AyanamsaComparisonRuns (Id) ON DELETE CASCADE,
        BodyCode                   VARCHAR(20) NOT NULL,
        TropicalLongitudeDegrees  DECIMAL(12,8) NULL,
        SiderealLongitudeDegrees  DECIMAL(12,8) NOT NULL,
        ReferenceLongitudeDegrees DECIMAL(12,8) NOT NULL,
        SignedErrorArcSeconds      DECIMAL(14,6) NOT NULL,
        AbsoluteErrorArcSeconds    DECIMAL(14,6) NOT NULL,
        EclipticLatitudeDegrees    DECIMAL(12,8) NULL,
        LongitudeSpeedDegreesPerDay DECIMAL(12,8) NULL,
        IsRetrograde               BIT NULL,
        NodeTypeCode               VARCHAR(10) NULL,
        CONSTRAINT UQ_Fact_AyanamsaPositionComparisons_RunBody UNIQUE (AyanamsaComparisonRunId, BodyCode),
        CONSTRAINT CK_Fact_AyanamsaPositionComparisons_NodeType CHECK (NodeTypeCode IS NULL OR NodeTypeCode IN ('Mean','True'))
    );
END;

IF OBJECT_ID('dbo.tbl_Fact_DashaApplicabilityResults', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_DashaApplicabilityResults
    (
        Id                      INT IDENTITY(1,1) CONSTRAINT PK_Fact_DashaApplicabilityResults PRIMARY KEY,
        AyanamsaComparisonRunId INT NOT NULL CONSTRAINT FK_Fact_DashaApplicabilityResults_Run
                                FOREIGN KEY REFERENCES dbo.tbl_Fact_AyanamsaComparisonRuns (Id) ON DELETE CASCADE,
        DashaSystemId           INT NOT NULL CONSTRAINT FK_Fact_DashaApplicabilityResults_System
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_DashaSystems (Id),
        RuleSetId               TINYINT NOT NULL CONSTRAINT FK_Fact_DashaApplicabilityResults_RuleSet
                                FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        IsApplicable            BIT NULL,
        Evidence                NVARCHAR(1000) NULL,
        EvaluatedAtUtc          DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_DashaApplicabilityResults_EvaluatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Fact_DashaApplicabilityResults_RunSystem UNIQUE (AyanamsaComparisonRunId, DashaSystemId)
    );
END;

IF OBJECT_ID('dbo.tbl_Fact_DashaBenchmarkComparisons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_DashaBenchmarkComparisons
    (
        Id                      INT IDENTITY(1,1) CONSTRAINT PK_Fact_DashaBenchmarkComparisons PRIMARY KEY,
        AyanamsaComparisonRunId INT NOT NULL CONSTRAINT FK_Fact_DashaBenchmarkComparisons_Run
                                FOREIGN KEY REFERENCES dbo.tbl_Fact_AyanamsaComparisonRuns (Id) ON DELETE CASCADE,
        DashaBenchmarkPeriodId  INT NOT NULL CONSTRAINT FK_Fact_DashaBenchmarkComparisons_Period
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_DashaBenchmarkPeriods (Id),
        ComputedStartDateTime   DATETIMEOFFSET(0) NOT NULL,
        DifferenceSeconds       BIGINT NOT NULL,
        IsWithinTolerance       BIT NOT NULL,
        CONSTRAINT UQ_Fact_DashaBenchmarkComparisons_RunPeriod UNIQUE (AyanamsaComparisonRunId, DashaBenchmarkPeriodId)
    );
END;
GO

MERGE dbo.tbl_Dim_DashaSystems AS target
USING (VALUES
 ('DASHA_VIMSHOTTARI',N'Vimshottari','NakshatraGraha',0,N'General phalita timing and the principal default nakshatra dasha.','Implemented','SRC_PVR_INTEGRATED'),
 ('DASHA_ASHTOTTARI',N'Ashtottari','NakshatraGraha',1,N'Conditional nakshatra dasha; applicability must be evaluated explicitly.','Planned','SRC_PVR_INTEGRATED'),
 ('DASHA_KALACHAKRA',N'Kalachakra','Specialized',1,N'Nakshatra-sign progression using Savya/Apasavya, Deha, Jiva and paramayush.','Research','SRC_PVR_INTEGRATED'),
 ('DASHA_NARAYANA_D1',N'Narayana Dasha (D1)','Rasi',0,N'Versatile sign-based phalita timing from the D1 chart.','Research','SRC_PVR_INTEGRATED'),
 ('DASHA_SUDASA',N'Sudasa','Rasi',0,N'Prosperity and Lakshmi-related timing based on Sree Lagna.','Research','SRC_PVR_INTEGRATED'),
 ('DASHA_MOOLA',N'Moola Dasha','Specialized',0,N'Roots of events and past-karma indications.','Research','SRC_PVR_INTEGRATED')
) AS source (Code,DisplayName,Family,IsConditional,PrimaryUse,ImplementationStatus,SourceRefCode)
ON target.Code = source.Code
WHEN NOT MATCHED THEN INSERT (Code,DisplayName,Family,IsConditional,PrimaryUse,ImplementationStatus,SourceRefCode)
VALUES (source.Code,source.DisplayName,source.Family,source.IsConditional,source.PrimaryUse,source.ImplementationStatus,source.SourceRefCode);

DECLARE @BirthDetailId INT = (SELECT TOP (1) Id FROM dbo.tbl_BirthDetails WHERE Name IN (N'Ramakrishnan P',N'Ramakrishnan') ORDER BY CASE WHEN Name=N'Ramakrishnan P' THEN 0 ELSE 1 END, Id);
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_AyanamsaBenchmarkCases WHERE Code='BENCH_RAMAKRISHNAN_P_JHORA_1981')
    INSERT dbo.tbl_Dim_AyanamsaBenchmarkCases
      (Code,BirthDetailId,PersonName,BirthDate,BirthTime,UtcOffset,LatitudeDegrees,LongitudeDegrees,
       ReferenceAyanamsaDegrees,ReferenceSourceRefCode,VimshottariDaysPerYear,Notes)
    VALUES
      ('BENCH_RAMAKRISHNAN_P_JHORA_1981',@BirthDetailId,N'Ramakrishnan P','1981-04-22','05:30:01','05:30',
       13.08333333,80.28333333,23.59495278,'SRC_JHORA_EXPORT_RAMAKRISHNAN',365.242500,
       N'Golden record transcribed from the supplied Jagannatha Hora natal export; latitude 13N05, longitude 80E17.');

DECLARE @CaseId INT = (SELECT Id FROM dbo.tbl_Dim_AyanamsaBenchmarkCases WHERE Code='BENCH_RAMAKRISHNAN_P_JHORA_1981');
MERGE dbo.tbl_Dim_AyanamsaBenchmarkPositions AS target
USING (VALUES
 ('Ascendant',0.65736111),('Sun',8.20185556),('Moon',217.20875833),('Mars',3.94327778),
 ('Mercury',1.83256389),('Jupiter',158.72377222),('Venus',11.98317500),('Saturn',160.95951111),
 ('Rahu',102.91464444),('Ketu',282.91464444)
) AS source (BodyCode,SiderealLongitudeDegrees)
ON target.AyanamsaBenchmarkCaseId=@CaseId AND target.BodyCode=source.BodyCode
WHEN NOT MATCHED THEN INSERT (AyanamsaBenchmarkCaseId,BodyCode,SiderealLongitudeDegrees)
VALUES (@CaseId,source.BodyCode,source.SiderealLongitudeDegrees);

DECLARE @VimshottariId INT = (SELECT Id FROM dbo.tbl_Dim_DashaSystems WHERE Code='DASHA_VIMSHOTTARI');
MERGE dbo.tbl_Dim_DashaBenchmarkPeriods AS target
USING (VALUES
 (1,'Saturn','1975-10-17'),(2,'Mercury','1994-10-17'),(3,'Ketu','2011-10-17'),
 (4,'Venus','2018-10-17'),(5,'Sun','2038-10-17'),(6,'Moon','2044-10-16'),
 (7,'Mars','2054-10-17'),(8,'Rahu','2061-10-16'),(9,'Jupiter','2079-10-17')
) AS source (SequenceNumber,LordCode,StartDate)
ON target.AyanamsaBenchmarkCaseId=@CaseId AND target.DashaSystemId=@VimshottariId
   AND target.LevelNumber=1 AND target.SequenceNumber=source.SequenceNumber
WHEN NOT MATCHED THEN INSERT (AyanamsaBenchmarkCaseId,DashaSystemId,LevelNumber,SequenceNumber,LordCode,StartDate)
VALUES (@CaseId,@VimshottariId,1,source.SequenceNumber,source.LordCode,source.StartDate);

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '46_create_ayanamsa_dasha_benchmarks.sql', 'Ayanamsa/Vimshottari benchmark schema, dasha catalogue, and Ramakrishnan P JHora golden record.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName='46_create_ayanamsa_dasha_benchmarks.sql');
GO
