-- =====================================================================
-- 34 — Query-shape indexes for chart reporting
--
-- Stage 2 of the database-design review. These indexes follow the current
-- repository/view predicates and do not change keys or stored values.
-- =====================================================================
USE [ikiastrro];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChartResults_BirthDetailId_CalculationKind_Id')
    CREATE NONCLUSTERED INDEX IX_ChartResults_BirthDetailId_CalculationKind_Id
        ON dbo.tbl_ChartResults (BirthDetailId, CalculationKind, Id DESC)
        INCLUDE (ChartTypeId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Chart_HouseLords_ChartResultId_LordPlanetId')
    CREATE NONCLUSTERED INDEX IX_Chart_HouseLords_ChartResultId_LordPlanetId
        ON dbo.tbl_Chart_HouseLords (ChartResultId, LordPlanetId)
        INCLUDE (HouseNumber);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Chart_Aspects_ChartResultId_AspectingPlanetId')
    CREATE NONCLUSTERED INDEX IX_Chart_Aspects_ChartResultId_AspectingPlanetId
        ON dbo.tbl_Chart_Aspects (ChartResultId, AspectingPlanetId)
        INCLUDE (AspectedTargetType, AspectedPlanetId, AspectType);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Chart_Conjunctions_ChartResultId_Planet1Id')
    CREATE NONCLUSTERED INDEX IX_Chart_Conjunctions_ChartResultId_Planet1Id
        ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1Id)
        INCLUDE (Planet2Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Chart_Conjunctions_ChartResultId_Planet2Id')
    CREATE NONCLUSTERED INDEX IX_Chart_Conjunctions_ChartResultId_Planet2Id
        ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet2Id)
        INCLUDE (Planet1Id);
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '34_add_query_shape_indexes.sql', 'Reporting indexes for chart result, house lord, aspect, and conjunction predicates'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '34_add_query_shape_indexes.sql');
GO

PRINT '34 applied: query-shape indexes added.';
GO
