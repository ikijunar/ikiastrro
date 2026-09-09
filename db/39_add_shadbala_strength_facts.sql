-- =====================================================================
-- 39 — PVR-first Shadbala rule profile and planetary/house strength facts.
--
-- PVR defines the six strength sources and points to Raman's Graha and
-- Bhava Balas for detailed arithmetic. The existing global RuleSetId=1 is
-- retained so unrelated rule consumers are not switched by this migration.
-- Strength rows identify the PVR profile and Raman formula source explicitly.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_Source WHERE Code = 'SRC_RAMAN_GRAHA_BHAVA_BALAS')
BEGIN
    INSERT dbo.tbl_Dim_Source (Code, Title, Author, Edition, Tradition, Notes)
    VALUES ('SRC_RAMAN_GRAHA_BHAVA_BALAS',
            N'Graha and Bhava Balas',
            N'B. V. Raman',
            N'Thirteenth edition, 1992',
            'Raman',
            N'Detailed Shadbala and Bhava Bala arithmetic referred to by PVR strength chapter; local DJVU supplied for this implementation.');
END
GO

IF COL_LENGTH('dbo.tbl_Rule_ShadbalaComponent', 'StrengthProfileCode') IS NULL
    ALTER TABLE dbo.tbl_Rule_ShadbalaComponent ADD StrengthProfileCode VARCHAR(40) NULL;
IF COL_LENGTH('dbo.tbl_Rule_ShadbalaComponent', 'FormulaSourceRefCode') IS NULL
    ALTER TABLE dbo.tbl_Rule_ShadbalaComponent ADD FormulaSourceRefCode VARCHAR(40) NULL;
GO

IF COL_LENGTH('dbo.tbl_Rule_ShadbalaComponent', 'StrengthProfileCode') IS NOT NULL
BEGIN
    UPDATE dbo.tbl_Rule_ShadbalaComponent
       SET StrengthProfileCode = COALESCE(StrengthProfileCode, 'PVR_INTEGRATED_STRENGTH'),
           FormulaSourceRefCode = COALESCE(FormulaSourceRefCode, SourceRefCode)
     WHERE StrengthProfileCode IS NULL OR FormulaSourceRefCode IS NULL;
END
GO

IF OBJECT_ID('dbo.tbl_Rule_BhavaBalaComponent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_BhavaBalaComponent (
        Id                    INT IDENTITY(1,1) CONSTRAINT PK_Rule_BhavaBalaComponent PRIMARY KEY,
        RuleSetId             TINYINT NOT NULL CONSTRAINT FK_Rule_BhavaBalaComponent_RuleSet REFERENCES dbo.tbl_Rule_Sets (Id),
        StrengthProfileCode   VARCHAR(40) NOT NULL,
        ComponentCode         VARCHAR(40) NOT NULL,
        WeightRupas           DECIMAL(9,3) NULL,
        MaxRupas              DECIMAL(9,3) NULL,
        MethodCode            VARCHAR(40) NULL,
        RuleParametersJson    NVARCHAR(MAX) NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        SourceRefCode         VARCHAR(40) NULL,
        FormulaSourceRefCode  VARCHAR(40) NULL,
        IsActive               BIT NOT NULL CONSTRAINT DF_Rule_BhavaBalaComponent_IsActive DEFAULT (1),
        CONSTRAINT CK_Rule_BhavaBalaComponent_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_BhavaBalaComponent_Src CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT CK_Rule_BhavaBalaComponent_FormulaSrc CHECK (FormulaSourceRefCode IS NULL OR FormulaSourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_BhavaBalaComponent UNIQUE (RuleSetId, StrengthProfileCode, ComponentCode)
    );
END
GO

IF OBJECT_ID('dbo.tbl_Fact_PlanetaryStrength', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_PlanetaryStrength (
        Id                    INT IDENTITY(1,1) CONSTRAINT PK_Fact_PlanetaryStrength PRIMARY KEY,
        ChartResultId         INT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrength_ChartResult REFERENCES dbo.tbl_ChartResults (Id),
        PlanetId              TINYINT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrength_Planet REFERENCES dbo.tbl_Planets (Id),
        RuleSetId             TINYINT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrength_RuleSet REFERENCES dbo.tbl_Rule_Sets (Id),
        StrengthProfileCode   VARCHAR(40) NOT NULL,
        FormulaSourceRefCode  VARCHAR(40) NULL,
        SthanaBalaVirupas     DECIMAL(12,3) NOT NULL,
        DigBalaVirupas        DECIMAL(12,3) NOT NULL,
        KalaBalaVirupas       DECIMAL(12,3) NOT NULL,
        CheshtaBalaVirupas    DECIMAL(12,3) NOT NULL,
        NaisargikaBalaVirupas DECIMAL(12,3) NOT NULL,
        DrikBalaVirupas       DECIMAL(12,3) NOT NULL,
        YuddhaBalaVirupas     DECIMAL(12,3) NOT NULL CONSTRAINT DF_Fact_PlanetaryStrength_Yuddha DEFAULT (0),
        ShadbalaVirupas       DECIMAL(12,3) NOT NULL,
        ShadbalaRupas         DECIMAL(12,3) NOT NULL,
        IshtaBala             DECIMAL(12,3) NULL,
        KashtaBala            DECIMAL(12,3) NULL,
        MinimumRequiredRupas  DECIMAL(12,3) NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        ComputedAtUtc         DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_PlanetaryStrength_Computed DEFAULT (sysutcdatetime()),
        CONSTRAINT CK_Fact_PlanetaryStrength_Source CHECK (FormulaSourceRefCode IS NULL OR FormulaSourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Fact_PlanetaryStrength UNIQUE (ChartResultId, PlanetId, StrengthProfileCode)
    );
    CREATE INDEX IX_Fact_PlanetaryStrength_Chart ON dbo.tbl_Fact_PlanetaryStrength (ChartResultId, PlanetId);
END
GO

IF OBJECT_ID('dbo.tbl_Fact_PlanetaryStrengthComponent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_PlanetaryStrengthComponent (
        Id                    INT IDENTITY(1,1) CONSTRAINT PK_Fact_PlanetaryStrengthComponent PRIMARY KEY,
        ChartResultId         INT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrengthComponent_ChartResult REFERENCES dbo.tbl_ChartResults (Id),
        PlanetId              TINYINT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrengthComponent_Planet REFERENCES dbo.tbl_Planets (Id),
        RuleSetId             TINYINT NOT NULL CONSTRAINT FK_Fact_PlanetaryStrengthComponent_RuleSet REFERENCES dbo.tbl_Rule_Sets (Id),
        StrengthProfileCode   VARCHAR(40) NOT NULL,
        BalaCode              VARCHAR(30) NOT NULL,
        SubComponentCode      VARCHAR(40) NOT NULL,
        ValueVirupas          DECIMAL(12,3) NOT NULL,
        UnitCode              VARCHAR(20) NOT NULL CONSTRAINT DF_Fact_PlanetaryStrengthComponent_Unit DEFAULT ('VIRUPA'),
        RuleId                INT NULL CONSTRAINT FK_Fact_PlanetaryStrengthComponent_Rule REFERENCES dbo.tbl_Rule_ShadbalaComponent (Id),
        FormulaSourceRefCode  VARCHAR(40) NULL,
        InputSnapshotJson     NVARCHAR(MAX) NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        ComputedAtUtc         DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_PlanetaryStrengthComponent_Computed DEFAULT (sysutcdatetime()),
        CONSTRAINT CK_Fact_PlanetaryStrengthComponent_Source CHECK (FormulaSourceRefCode IS NULL OR FormulaSourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT CK_Fact_PlanetaryStrengthComponent_Json CHECK (InputSnapshotJson IS NULL OR ISJSON(InputSnapshotJson) = 1),
        CONSTRAINT UQ_Fact_PlanetaryStrengthComponent UNIQUE (ChartResultId, PlanetId, StrengthProfileCode, BalaCode, SubComponentCode)
    );
    CREATE INDEX IX_Fact_PlanetaryStrengthComponent_Chart ON dbo.tbl_Fact_PlanetaryStrengthComponent (ChartResultId, PlanetId, BalaCode);
END
GO

IF OBJECT_ID('dbo.tbl_Fact_BhavaStrength', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_BhavaStrength (
        Id                    INT IDENTITY(1,1) CONSTRAINT PK_Fact_BhavaStrength PRIMARY KEY,
        ChartResultId         INT NOT NULL CONSTRAINT FK_Fact_BhavaStrength_ChartResult REFERENCES dbo.tbl_ChartResults (Id),
        HouseNumber           TINYINT NOT NULL,
        RuleSetId             TINYINT NOT NULL CONSTRAINT FK_Fact_BhavaStrength_RuleSet REFERENCES dbo.tbl_Rule_Sets (Id),
        StrengthProfileCode   VARCHAR(40) NOT NULL,
        FormulaSourceRefCode  VARCHAR(40) NULL,
        BhavaBalaVirupas      DECIMAL(12,3) NOT NULL,
        BhavaBalaRupas        DECIMAL(12,3) NOT NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        ComputedAtUtc         DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_BhavaStrength_Computed DEFAULT (sysutcdatetime()),
        CONSTRAINT CK_Fact_BhavaStrength_House CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_Fact_BhavaStrength_Source CHECK (FormulaSourceRefCode IS NULL OR FormulaSourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Fact_BhavaStrength UNIQUE (ChartResultId, HouseNumber, StrengthProfileCode)
    );
    CREATE INDEX IX_Fact_BhavaStrength_Chart ON dbo.tbl_Fact_BhavaStrength (ChartResultId, HouseNumber);
END
GO

IF OBJECT_ID('dbo.tbl_Fact_BhavaStrengthComponent', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_BhavaStrengthComponent (
        Id                    INT IDENTITY(1,1) CONSTRAINT PK_Fact_BhavaStrengthComponent PRIMARY KEY,
        ChartResultId         INT NOT NULL CONSTRAINT FK_Fact_BhavaStrengthComponent_ChartResult REFERENCES dbo.tbl_ChartResults (Id),
        HouseNumber           TINYINT NOT NULL,
        RuleSetId             TINYINT NOT NULL CONSTRAINT FK_Fact_BhavaStrengthComponent_RuleSet REFERENCES dbo.tbl_Rule_Sets (Id),
        StrengthProfileCode   VARCHAR(40) NOT NULL,
        ComponentCode         VARCHAR(40) NOT NULL,
        ValueVirupas          DECIMAL(12,3) NOT NULL,
        RuleId                INT NULL CONSTRAINT FK_Fact_BhavaStrengthComponent_Rule REFERENCES dbo.tbl_Rule_BhavaBalaComponent (Id),
        FormulaSourceRefCode  VARCHAR(40) NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        ComputedAtUtc         DATETIME2(0) NOT NULL CONSTRAINT DF_Fact_BhavaStrengthComponent_Computed DEFAULT (sysutcdatetime()),
        CONSTRAINT CK_Fact_BhavaStrengthComponent_House CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_Fact_BhavaStrengthComponent_Source CHECK (FormulaSourceRefCode IS NULL OR FormulaSourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Fact_BhavaStrengthComponent UNIQUE (ChartResultId, HouseNumber, StrengthProfileCode, ComponentCode)
    );
    CREATE INDEX IX_Fact_BhavaStrengthComponent_Chart ON dbo.tbl_Fact_BhavaStrengthComponent (ChartResultId, HouseNumber);
END
GO

;WITH seed (BalaCode, SubComponentCode, MaxRupas, MethodCode, Narrative) AS (
    SELECT * FROM (VALUES
        ('STHANA_BALA','UCHCHA_BALA',1.000,'DEBILITATION_DISTANCE','Exaltation-distance strength, stored in virupas.'),
        ('STHANA_BALA','SAPTAVARGAJA_BALA',4.500,'SAPTA_VARGA_DIGNITY','D1, D2, D3, D7, D9, D12 and D30 dignity contribution.'),
        ('STHANA_BALA','OJHA_YUGMA_RASYAMSA_BALA',0.500,'ODD_EVEN_D1_D9','Odd/even sign and amsa sex contribution.'),
        ('STHANA_BALA','KENDRADI_BALA',0.500,'KENDRA_PANAPHARA_APOKLIMA','House angularity contribution.'),
        ('STHANA_BALA','DREKKANA_BALA',0.250,'DREKKANA_GROUP','Drekkana sex/group contribution.'),
        ('DIG_BALA','DIG_BALA',1.000,'FULL_STRENGTH_HOUSE_DISTANCE','Distance from the planet''s full directional-strength house.'),
        ('KALA_BALA','NATHONNATA_BALA',1.000,'DAY_NIGHT_ARC','Day/night temporal strength.'),
        ('KALA_BALA','PAKSHA_BALA',1.000,'MOON_PHASE','Lunar-phase strength.'),
        ('KALA_BALA','TRIBHAGA_BALA',1.000,'DAY_NIGHT_THIRDS','Third-of-day/night lord strength.'),
        ('KALA_BALA','VARSHA_BALA',0.750,'YEAR_LORD','Year lord strength.'),
        ('KALA_BALA','MASA_BALA',0.500,'MONTH_LORD','Month lord strength.'),
        ('KALA_BALA','DINA_BALA',0.750,'WEEKDAY_LORD','Weekday lord strength.'),
        ('KALA_BALA','HORA_BALA',1.000,'PLANETARY_HOUR','Hora lord strength.'),
        ('KALA_BALA','AYANA_BALA',1.000,'SOLAR_DECLINATION','Declination/ayana strength.'),
        ('CHESTA_BALA','CHESTA_BALA',1.000,'MOTION_AND_RETROGRADE','Motional strength.'),
        ('NAISARGIKA_BALA','NAISARGIKA_BALA',1.000,'FIXED_PLANET_VALUE','Permanent natural strength.'),
        ('DRIK_BALA','DRIK_BALA',1.000,'SPUTA_DRISHTI','Aspect strength from benefic and malefic influence.')
    ) s (BalaCode, SubComponentCode, MaxRupas, MethodCode, Narrative)
)
INSERT dbo.tbl_Rule_ShadbalaComponent
    (RuleSetId, BalaCode, SubComponentCode, MaxRupas, MethodCode, CalculationNarrative,
     SourceRefCode, IsActive, StrengthProfileCode, FormulaSourceRefCode)
SELECT 1, BalaCode, SubComponentCode, MaxRupas, MethodCode, Narrative,
       'SRC_PVR_INTEGRATED', 1, 'PVR_INTEGRATED_STRENGTH', 'SRC_RAMAN_GRAHA_BHAVA_BALAS'
FROM seed s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_Rule_ShadbalaComponent r
    WHERE r.RuleSetId = 1
      AND r.StrengthProfileCode = 'PVR_INTEGRATED_STRENGTH'
      AND r.BalaCode = s.BalaCode
      AND r.SubComponentCode = s.SubComponentCode
);
GO

;WITH seed (ComponentCode, MaxRupas, MethodCode, Narrative) AS (
    SELECT * FROM (VALUES
        ('BHAVADHIPATI_BALA',10.000,'HOUSE_LORD_SHADBALA','Strength of the house lord.'),
        ('BHAVA_DIG_BALA',5.000,'HOUSE_DIRECTION','Directional strength of the house.'),
        ('BHAVA_DRIK_BALA',5.000,'HOUSE_ASPECT','Aspect strength on the house midpoint.')
    ) s (ComponentCode, MaxRupas, MethodCode, Narrative)
)
INSERT dbo.tbl_Rule_BhavaBalaComponent
    (RuleSetId, StrengthProfileCode, ComponentCode, MaxRupas, MethodCode,
     CalculationNarrative, SourceRefCode, FormulaSourceRefCode)
SELECT 1, 'PVR_INTEGRATED_STRENGTH', ComponentCode, MaxRupas, MethodCode,
       Narrative, 'SRC_PVR_INTEGRATED', 'SRC_RAMAN_GRAHA_BHAVA_BALAS'
FROM seed s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.tbl_Rule_BhavaBalaComponent r
    WHERE r.RuleSetId = 1 AND r.StrengthProfileCode = 'PVR_INTEGRATED_STRENGTH'
      AND r.ComponentCode = s.ComponentCode
);
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '39_add_shadbala_strength_facts.sql',
       'PVR-first Shadbala/Bhava Bala rule profile, formula provenance, and strength fact tables'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '39_add_shadbala_strength_facts.sql');
GO

PRINT '39 applied: PVR Shadbala rules and strength fact tables are ready.';
GO
