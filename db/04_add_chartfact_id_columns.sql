-- =====================================================================
-- 04 — Add integer-FK columns beside the existing name columns on every
-- chart-fact table (review item 2). All NULLable here; migration 05 backfills,
-- migration 06 tightens to NOT NULL + FK + CHECK.
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_ChartResults', 'RuleSetId') IS NULL
ALTER TABLE dbo.tbl_ChartResults ADD
    RuleSetId       TINYINT      NULL,
    ChartTypeId     TINYINT      NULL,
    CalculationKind VARCHAR(20)  NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'PlanetId') IS NULL
ALTER TABLE dbo.tbl_Chart_KeyDetails ADD
    PlanetId                 TINYINT NULL,
    SignId                   TINYINT NULL,
    NakshatraLordPlanetId    TINYINT NULL,
    NakshatraSubLordPlanetId TINYINT NULL,
    SignLordPlanetId         TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_HouseLords', 'HouseSignId') IS NULL
ALTER TABLE dbo.tbl_Chart_HouseLords ADD
    HouseSignId        TINYINT NULL,
    LordPlanetId       TINYINT NULL,
    LordPlacedInSignId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_Conjunctions', 'Planet1Id') IS NULL
ALTER TABLE dbo.tbl_Chart_Conjunctions ADD
    Planet1Id TINYINT NULL,
    Planet2Id TINYINT NULL,
    SignId    TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_Aspects', 'AspectingPlanetId') IS NULL
ALTER TABLE dbo.tbl_Chart_Aspects ADD
    AspectingPlanetId TINYINT     NULL,
    AspectedTargetType VARCHAR(10) NULL,
    AspectedPlanetId  TINYINT     NULL;
GO
IF COL_LENGTH('dbo.tbl_Chart_DashaPeriods', 'LordId') IS NULL
ALTER TABLE dbo.tbl_Chart_DashaPeriods ADD LordId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Fact_PlanetAvastha', 'PlanetId') IS NULL
ALTER TABLE dbo.tbl_Fact_PlanetAvastha ADD
    PlanetId    TINYINT NULL,
    ChartTypeId TINYINT NULL;
GO
-- transit-event uniqueness (review item 5, additive part).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TransitEvent_Planet_At')
BEGIN
    IF EXISTS (
        SELECT PlanetId, EventDateTimeUtc FROM dbo.tbl_PlanetSignTransitEvents
        GROUP BY PlanetId, EventDateTimeUtc HAVING COUNT(*) > 1)
        THROW 50004, 'tbl_PlanetSignTransitEvents has duplicate (PlanetId, EventDateTimeUtc) rows — dedupe before creating UX_TransitEvent_Planet_At.', 1;
    CREATE UNIQUE INDEX UX_TransitEvent_Planet_At ON dbo.tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc);
END
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '04_add_chartfact_id_columns.sql', 'nullable *Id columns on chart-fact tables + transit unique'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '04_add_chartfact_id_columns.sql');
GO
PRINT '04 applied: *Id columns added (nullable).';
GO
