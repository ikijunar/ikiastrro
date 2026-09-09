-- 44 - Transit position fields for Saturn/Jupiter/Rahu sign events.
-- Additive and idempotent. Existing sign-crossing rows are backfilled at the
-- exact sign boundary; speed remains NULL for historical rows because it was
-- not persisted by the original event loader.
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'LongitudeDegrees') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD LongitudeDegrees DECIMAL(12,8) NULL;
IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'DegreeInSign') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD DegreeInSign DECIMAL(10,8) NULL;
IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'NakshatraId') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD NakshatraId TINYINT NULL;
IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'Pada') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD Pada TINYINT NULL;
IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'SpeedDegreesPerDay') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD SpeedDegreesPerDay DECIMAL(12,8) NULL;
IF COL_LENGTH('dbo.tbl_PlanetSignTransitEvents', 'AyanamsaRuleId') IS NULL
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD AyanamsaRuleId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TransitEvents_Nakshatra')
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents WITH CHECK ADD CONSTRAINT FK_TransitEvents_Nakshatra
        FOREIGN KEY (NakshatraId) REFERENCES dbo.tbl_Nakshatras(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TransitEvents_AyanamsaRule')
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents WITH CHECK ADD CONSTRAINT FK_TransitEvents_AyanamsaRule
        FOREIGN KEY (AyanamsaRuleId) REFERENCES dbo.tbl_Rule_Ayanamsa(Id);
GO

-- Existing rows represent the instant of crossing into SignId: longitude is
-- the first degree of that sign. The default engine ayanamsa is Jagannatha (7).
UPDATE e
SET LongitudeDegrees = CONVERT(DECIMAL(12,8), (e.SignId - 1) * 30.0),
    DegreeInSign = CONVERT(DECIMAL(10,8), 0.0),
    NakshatraId = CONVERT(TINYINT, FLOOR(((e.SignId - 1) * 30.0) / (360.0 / 27.0)) + 1),
    Pada = CONVERT(TINYINT, FLOOR((((e.SignId - 1) * 30.0) % (360.0 / 27.0)) / (360.0 / 108.0)) + 1),
    AyanamsaRuleId = COALESCE(e.AyanamsaRuleId, 7)
FROM dbo.tbl_PlanetSignTransitEvents e
WHERE e.LongitudeDegrees IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TransitEvents_Longitude')
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD CONSTRAINT CK_TransitEvents_Longitude
        CHECK (LongitudeDegrees IS NULL OR (LongitudeDegrees >= 0 AND LongitudeDegrees < 360));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TransitEvents_DegreeInSign')
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD CONSTRAINT CK_TransitEvents_DegreeInSign
        CHECK (DegreeInSign IS NULL OR (DegreeInSign >= 0 AND DegreeInSign < 30));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TransitEvents_Pada')
    ALTER TABLE dbo.tbl_PlanetSignTransitEvents ADD CONSTRAINT CK_TransitEvents_Pada
        CHECK (Pada IS NULL OR Pada BETWEEN 1 AND 4);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TransitEvents_Planet_Sign_Date')
    CREATE INDEX IX_TransitEvents_Planet_Sign_Date
        ON dbo.tbl_PlanetSignTransitEvents (PlanetId, SignId, EventDateTimeUtc);
GO

PRINT '44_add_transit_position_fields: complete';
GO
