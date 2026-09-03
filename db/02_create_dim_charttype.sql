-- =====================================================================
-- 02 — tbl_Dim_ChartType: controlled vocabulary for ChartResults.ChartTypeId.
-- Seeds the six registered position charts. D3/D7/D12..D60 are future INSERTs
-- (no schema change). Vimshottari Dasha is NOT a chart type — see CalculationKind (migration 04).
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Dim_ChartType', 'U') IS NULL
CREATE TABLE dbo.tbl_Dim_ChartType (
    Id               TINYINT      NOT NULL CONSTRAINT PK_Dim_ChartType PRIMARY KEY,
    Code             VARCHAR(20)  NOT NULL CONSTRAINT UQ_Dim_ChartType_Code UNIQUE,
    DisplayName      VARCHAR(40)  NOT NULL,
    DivisionalFactor TINYINT      NULL,
    Category         VARCHAR(20)  NOT NULL,
    DisplayOrder     TINYINT      NOT NULL,
    CONSTRAINT CK_Dim_ChartType_Factor CHECK (DivisionalFactor IS NULL OR DivisionalFactor BETWEEN 1 AND 60)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_ChartType)
INSERT dbo.tbl_Dim_ChartType (Id, Code, DisplayName, DivisionalFactor, Category, DisplayOrder) VALUES
    (1, 'D1',  'Rasi',       1,  'Varga', 1),
    (2, 'D2',  'Hora',       2,  'Varga', 2),
    (3, 'D6',  'Shashtamsa', 6,  'Varga', 3),
    (4, 'D9',  'Navamsa',    9,  'Varga', 4),
    (5, 'D10', 'Dasamsa',    10, 'Varga', 5),
    (6, 'D11', 'Rudramsa',   11, 'Varga', 6);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '02_create_dim_charttype.sql', 'chart-type vocabulary + 6-row seed'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '02_create_dim_charttype.sql');
GO
PRINT '02 applied: tbl_Dim_ChartType ready.';
GO
