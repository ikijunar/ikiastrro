-- 36 — tbl_Rule_Ayanamsa
-- JHora ayanamsa calculation options. Default: Jagannatha (Spica in Chitra,
-- fixed solar rotation plane), as requested for ikiastrro.
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '36_create_rule_ayanamsa.sql')
BEGIN
    IF OBJECT_ID('dbo.tbl_Rule_Ayanamsa', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tbl_Rule_Ayanamsa
        (
            Id                  INT IDENTITY(1,1) CONSTRAINT PK_Rule_Ayanamsa PRIMARY KEY,
            RuleSetId           TINYINT NOT NULL CONSTRAINT FK_Rule_Ayanamsa_RuleSet
                                FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
            Code                VARCHAR(40) NOT NULL,
            DisplayName         NVARCHAR(160) NOT NULL,
            SwissSiderealMode   INT NULL,
            IsTropical          BIT NOT NULL CONSTRAINT DF_Rule_Ayanamsa_IsTropical DEFAULT 0,
            IsImplemented       BIT NOT NULL CONSTRAINT DF_Rule_Ayanamsa_IsImplemented DEFAULT 0,
            IsDefault           BIT NOT NULL CONSTRAINT DF_Rule_Ayanamsa_IsDefault DEFAULT 0,
            CorrectionDirection VARCHAR(8) NOT NULL CONSTRAINT DF_Rule_Ayanamsa_CorrectionDirection DEFAULT 'Subtract',
            CorrectionDegrees   DECIMAL(12,8) NOT NULL CONSTRAINT DF_Rule_Ayanamsa_CorrectionDegrees DEFAULT 0,
            SourceRefCode       VARCHAR(40) NULL CONSTRAINT FK_Rule_Ayanamsa_Source
                                FOREIGN KEY REFERENCES dbo.tbl_Dim_Source (Code),
            CONSTRAINT UQ_Rule_Ayanamsa_RuleSetCode UNIQUE (RuleSetId, Code),
            CONSTRAINT CK_Rule_Ayanamsa_CorrectionDirection CHECK (CorrectionDirection IN ('Add','Subtract')),
            CONSTRAINT CK_Rule_Ayanamsa_CorrectionDegrees CHECK (CorrectionDegrees >= 0 AND CorrectionDegrees <= 360),
            CONSTRAINT CK_Rule_Ayanamsa_TropicalMode CHECK (IsTropical = 0 OR SwissSiderealMode IS NULL)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Rule_Ayanamsa_Default'
                   AND object_id = OBJECT_ID('dbo.tbl_Rule_Ayanamsa'))
        CREATE UNIQUE INDEX UX_Rule_Ayanamsa_Default ON dbo.tbl_Rule_Ayanamsa (RuleSetId) WHERE IsDefault = 1;

    INSERT dbo.tbl_Rule_Ayanamsa
        (RuleSetId, Code, DisplayName, SwissSiderealMode, IsTropical, IsImplemented, IsDefault, SourceRefCode)
    VALUES
      (1,'AYANAMSA_TRUE_LAHIRI',N'True Lahiri/Chitrapaksha',27,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_LAHIRI',N'Traditional Lahiri',1,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_PUSHYA_PAKSHA',N'Pushya-paksha ayanamsa',29,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_RAMAN',N'Raman',3,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_KP',N'Krishnamoorthy (KP)',5,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_FIXED_STAR_CUSTOM',N'Fixed star based CUSTOM ayanamsa',NULL,0,0,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_JAGANNATHA',N'Jagannatha (Spica in the middle of Chitra always, fixed solar rotation plane)',26,0,1,1,'SRC_PYJHORA'),
      (1,'AYANAMSA_ROHINI_PAKSHA',N'Rohini-paksha ayanamsa',NULL,0,0,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_SRI_SURYA_SIDDHANTA',N'Sri Surya Siddhanta',21,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_DEVA_DATTA',N'Deva-datta',NULL,0,0,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_USHA_SHASHI',N'Usha-Shashi',4,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_YUKTESHWAR',N'Yukteshwar',7,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_JN_BHASIN',N'JN Bhasin',8,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_CHANDRA_HARI',N'Chandra Hari',NULL,0,0,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_FAGAN',N'Fagan',0,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_DELUCE',N'Deluce',2,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_DJWHAL_KHUL',N'Djwhal Khul',6,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_ALDEBARAN_15_TAU',N'Aldebaran at 15Ta0',14,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_GALACTIC_CENTER',N'Galaxy center at 0Sg0',17,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_HIPPARCHOS',N'Hipparchos',15,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_SASSANIAN',N'Sassanian',16,0,1,0,'SRC_PYJHORA'),
      (1,'AYANAMSA_TROPICAL',N'Tropical (sayana)',NULL,1,1,0,'SRC_PYJHORA');

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('36_create_rule_ayanamsa.sql', 'JHora ayanamsa rule table; Jagannatha default');
    PRINT '36 applied: tbl_Rule_Ayanamsa created and seeded.';
END
ELSE PRINT '36 already applied.';
GO
