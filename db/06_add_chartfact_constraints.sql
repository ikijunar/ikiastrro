-- =====================================================================
-- 06 — Enforce the normalized shape (review items 2 + 5). Assumes migration
-- 05 backfill + the .NET write path (Tasks 9/11/12) have populated every id.
-- =====================================================================
USE [ikiastrro];
GO
-- Batch-level SET options: the PERSISTED computed column ParentChartResultId (added to
-- tbl_Chart_DashaPeriods below) requires QUOTED_IDENTIFIER/ANSI_NULLS ON at creation time, and
-- sqlcmd runs with QUOTED_IDENTIFIER OFF by default. Set here in their own GO-delimited batch so
-- the setting is in effect for every subsequent batch on this connection.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
-- ChartResults ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartResults_RuleSet')
BEGIN
    UPDATE dbo.tbl_ChartResults SET RuleSetId = (SELECT TOP 1 Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1) WHERE RuleSetId IS NULL;
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN RuleSetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id);
    ALTER TABLE dbo.tbl_ChartResults ALTER COLUMN CalculationKind VARCHAR(20) NOT NULL;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT DF_ChartResults_CalcKind DEFAULT ('PositionChart') FOR CalculationKind;
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_ChartType FOREIGN KEY (ChartTypeId) REFERENCES dbo.tbl_Dim_ChartType (Id);
    ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT CK_ChartResults_KindType CHECK (
        (CalculationKind = 'PositionChart' AND ChartTypeId IS NOT NULL) OR
        (CalculationKind <> 'PositionChart' AND ChartTypeId IS NULL));
END
GO
-- Chart_KeyDetails ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_KeyDetails_Sign')
BEGIN
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN SignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD
        CONSTRAINT FK_KeyDetails_Planet          FOREIGN KEY (PlanetId)                 REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_Sign            FOREIGN KEY (SignId)                   REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT FK_KeyDetails_NakLord         FOREIGN KEY (NakshatraLordPlanetId)    REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_NakSubLord      FOREIGN KEY (NakshatraSubLordPlanetId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_KeyDetails_SignLord        FOREIGN KEY (SignLordPlanetId)         REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_KeyDetails_Longitude       CHECK (NirayanaLongitudeDegrees >= 0 AND NirayanaLongitudeDegrees < 360),
        CONSTRAINT CK_KeyDetails_DegInSign       CHECK (DegreesInSignDecimal IS NULL OR (DegreesInSignDecimal >= 0 AND DegreesInSignDecimal < 30)),
        CONSTRAINT CK_KeyDetails_HouseLagna      CHECK (HouseNumberFromLagna BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_HouseSun        CHECK (HouseNumberFromSun BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_HouseMoon       CHECK (HouseNumberFromMoon BETWEEN 1 AND 12),
        CONSTRAINT CK_KeyDetails_Pada            CHECK (NakshatraPada IS NULL OR NakshatraPada BETWEEN 1 AND 4);
END
GO
-- Chart_HouseLords ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HouseLords_Lord')
BEGIN
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN HouseSignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN LordPlanetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ALTER COLUMN LordPlacedInSignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_HouseLords ADD
        CONSTRAINT FK_HouseLords_HouseSign     FOREIGN KEY (HouseSignId)        REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT FK_HouseLords_Lord          FOREIGN KEY (LordPlanetId)       REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_HouseLords_LordInSign    FOREIGN KEY (LordPlacedInSignId) REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT CK_HouseLords_House         CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_HouseLords_LordHouses    CHECK (LordPlacedInHouseFromLagna BETWEEN 1 AND 12 AND LordPlacedInHouseFromSun BETWEEN 1 AND 12 AND LordPlacedInHouseFromMoon BETWEEN 1 AND 12);
END
GO
-- Chart_Conjunctions --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Conjunctions_Planet1')
BEGIN
    -- canonicalize any rows the old data stored out of order
    UPDATE dbo.tbl_Chart_Conjunctions
       SET Planet1Id = Planet2Id, Planet2Id = Planet1Id, Planet1 = Planet2, Planet2 = Planet1
     WHERE Planet1Id > Planet2Id;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN Planet1Id TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN Planet2Id TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ALTER COLUMN SignId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Conjunctions ADD
        CONSTRAINT FK_Conjunctions_Planet1 FOREIGN KEY (Planet1Id) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Conjunctions_Planet2 FOREIGN KEY (Planet2Id) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Conjunctions_Sign    FOREIGN KEY (SignId)    REFERENCES dbo.tbl_SignAttributes (Id),
        CONSTRAINT CK_Conjunctions_Canonical CHECK (Planet1Id < Planet2Id),
        CONSTRAINT CK_Conjunctions_House     CHECK (HouseNumberFromLagna BETWEEN 1 AND 12);
    CREATE UNIQUE INDEX UX_Conjunctions_Result_Pair ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1Id, Planet2Id);
END
GO
-- Chart_Aspects -----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Aspects_Aspecting')
BEGIN
    ALTER TABLE dbo.tbl_Chart_Aspects ALTER COLUMN AspectingPlanetId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Aspects ALTER COLUMN AspectedTargetType VARCHAR(10) NOT NULL;
    ALTER TABLE dbo.tbl_Chart_Aspects ADD
        CONSTRAINT FK_Aspects_Aspecting FOREIGN KEY (AspectingPlanetId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT FK_Aspects_Aspected  FOREIGN KEY (AspectedPlanetId)  REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_Aspects_TargetType CHECK (AspectedTargetType IN ('Planet','Ascendant')),
        CONSTRAINT CK_Aspects_TargetShape CHECK (
            (AspectedTargetType = 'Ascendant' AND AspectedPlanetId IS NULL) OR
            (AspectedTargetType = 'Planet'    AND AspectedPlanetId IS NOT NULL));
END
GO
-- Chart_DashaPeriods ----------------------------------------------------------
-- Each object is guarded independently: the PERSISTED computed column ParentChartResultId needs the
-- batch-level SET options above, and per-object guards keep the migration re-runnable even if an
-- earlier partial run added some of these but not all.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DashaPeriods_Lord')
BEGIN
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ALTER COLUMN LordId TINYINT NOT NULL;
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD
        CONSTRAINT FK_DashaPeriods_Lord FOREIGN KEY (LordId) REFERENCES dbo.tbl_Planets (Id),
        CONSTRAINT CK_DashaPeriods_Dates   CHECK (StartDate < EndDate),
        CONSTRAINT CK_DashaPeriods_Offsets CHECK (StartDayOffset <= EndDayOffset),
        CONSTRAINT CK_DashaPeriods_Level   CHECK (LevelNumber BETWEEN 1 AND 3);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_DashaPeriods_Result_Id')
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT UQ_DashaPeriods_Result_Id UNIQUE (ChartResultId, Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_DashaPeriods') AND name = 'ParentChartResultId')
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD ParentChartResultId AS ChartResultId PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DashaPeriods_ParentSameChart')
    ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD CONSTRAINT FK_DashaPeriods_ParentSameChart
        FOREIGN KEY (ParentChartResultId, ParentDashaPeriodId) REFERENCES dbo.tbl_Chart_DashaPeriods (ChartResultId, Id);
GO
-- UQ_DashaPeriods_Sibling intentionally omitted: SequenceInParent is a classical Vimshottari
-- lord-index label (1..9, deliberately not renumbered), not a within-parent ordinal, so a UNIQUE
-- over (ChartResultId, ParentDashaPeriodId, SequenceInParent) is unsatisfiable once a chart's
-- Mahadasha timeline wraps past one 120-year cycle. Controller ruling 2026-08-31.
GO
-- Fact_PlanetAvastha --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Fact_PlanetAvastha_Planet')
    ALTER TABLE dbo.tbl_Fact_PlanetAvastha ADD CONSTRAINT FK_Fact_PlanetAvastha_Planet FOREIGN KEY (PlanetId) REFERENCES dbo.tbl_Planets (Id);
GO
-- BirthDetails ----------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BirthDetails_Name')
    DROP INDEX UX_BirthDetails_Name ON dbo.tbl_BirthDetails;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BirthDetails_Name')
    CREATE INDEX IX_BirthDetails_Name ON dbo.tbl_BirthDetails (Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_BirthDetails_LatLong')
BEGIN
    ALTER TABLE dbo.tbl_BirthDetails ALTER COLUMN Latitude DECIMAL(9,6) NOT NULL;
    ALTER TABLE dbo.tbl_BirthDetails ALTER COLUMN Longitude DECIMAL(9,6) NOT NULL;
    ALTER TABLE dbo.tbl_BirthDetails ADD CONSTRAINT CK_BirthDetails_LatLong
        CHECK (Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180);
END
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '06_add_chartfact_constraints.sql', 'NOT NULL + FK + CHECK + canonical conjunction + dasha parent FK + BirthDetails fixes'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '06_add_chartfact_constraints.sql');
GO
PRINT '06 applied: chart-fact constraints enforced.';
GO
