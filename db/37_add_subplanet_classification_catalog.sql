-- 37 - Upagraha classification metadata and catalog view.
-- Adds the user-facing distinction between calculation family and planetary
-- association without changing any calculation rule or stored chart result.
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.tbl_Dim_SubPlanets', 'CalculationFamilyCode') IS NULL
    ALTER TABLE dbo.tbl_Dim_SubPlanets
        ADD CalculationFamilyCode VARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.tbl_Dim_SubPlanets', 'AssociationRole') IS NULL
    ALTER TABLE dbo.tbl_Dim_SubPlanets
        ADD AssociationRole VARCHAR(20) NULL;
GO

UPDATE sp
SET CalculationFamilyCode = CASE sp.CalculationType
                                 WHEN 'SUN_LONGITUDE' THEN 'SUN_BASED'
                                 WHEN 'DAY_NIGHT_TIME' THEN 'TIME_BASED'
                             END,
    AssociationRole = CASE sp.CalculationType
                          WHEN 'SUN_LONGITUDE' THEN 'FORMULA_INPUT'
                          WHEN 'DAY_NIGHT_TIME' THEN 'PART_RULER'
                      END
FROM dbo.tbl_Dim_SubPlanets sp
WHERE sp.CalculationFamilyCode IS NULL OR sp.AssociationRole IS NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Dim_SubPlanets_Family')
    ALTER TABLE dbo.tbl_Dim_SubPlanets
        ADD CONSTRAINT CK_Dim_SubPlanets_Family
        CHECK (CalculationFamilyCode IN ('SUN_BASED','TIME_BASED'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Dim_SubPlanets_AssociationRole')
    ALTER TABLE dbo.tbl_Dim_SubPlanets
        ADD CONSTRAINT CK_Dim_SubPlanets_AssociationRole
        CHECK (AssociationRole IN ('FORMULA_INPUT','PART_RULER','PLANETARY_ANALOG'));
GO
IF COL_LENGTH('dbo.tbl_Dim_SubPlanets', 'CalculationFamilyCode') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_SubPlanets WHERE CalculationFamilyCode IS NULL)
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Dim_SubPlanets')
                   AND name = 'CalculationFamilyCode' AND is_nullable = 0)
    ALTER TABLE dbo.tbl_Dim_SubPlanets ALTER COLUMN CalculationFamilyCode VARCHAR(20) NOT NULL;
GO
IF COL_LENGTH('dbo.tbl_Dim_SubPlanets', 'AssociationRole') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_SubPlanets WHERE AssociationRole IS NULL)
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Dim_SubPlanets')
                   AND name = 'AssociationRole' AND is_nullable = 0)
    ALTER TABLE dbo.tbl_Dim_SubPlanets ALTER COLUMN AssociationRole VARCHAR(20) NOT NULL;
GO

CREATE OR ALTER VIEW dbo.vw_SubPlanetCatalog
AS
SELECT sp.Id,
       sp.SubPlanetCode,
       sp.SubPlanetName,
       sp.EnglishMeaning,
       sp.CalculationFamilyCode,
       sp.CalculationType,
       ap.PlanetName AS AssociatedPlanetName,
       sp.AssociationRole,
       sp.NaturalNature,
       sp.SortOrder,
       CASE WHEN sp.CalculationFamilyCode = 'SUN_BASED'
            THEN 'tbl_Rule_SubPlanetSunLongitude'
            ELSE 'tbl_Rule_SubPlanetTime' END AS RuleTableName,
       COALESCE(sl.MethodCode, tm.MethodCode) AS MethodCode,
       COALESCE(sl.CalculationNarrative, tm.CalculationNarrative) AS CalculationNarrative,
       COALESCE(sl.SourceRefCode, tm.SourceRefCode) AS SourceRefCode,
       sp.Notes
FROM dbo.tbl_Dim_SubPlanets sp
JOIN dbo.tbl_Planets ap ON ap.Id = sp.AssociatedPlanetId
OUTER APPLY
(
    SELECT TOP (1) r.MethodCode, r.CalculationNarrative, r.SourceRefCode
    FROM dbo.tbl_Rule_SubPlanetSunLongitude r
    WHERE r.SubPlanetId = sp.Id AND r.IsActive = 1
    ORDER BY r.RuleSetId DESC, r.SequenceNo
) sl
OUTER APPLY
(
    SELECT TOP (1) r.MethodCode, r.CalculationNarrative, r.SourceRefCode
    FROM dbo.tbl_Rule_SubPlanetTime r
    WHERE r.SubPlanetId = sp.Id AND r.IsActive = 1
    ORDER BY r.RuleSetId DESC
) tm;
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '37_add_subplanet_classification_catalog.sql',
       'Adds CalculationFamilyCode and AssociationRole to tbl_Dim_SubPlanets and creates vw_SubPlanetCatalog; no rule formulas or facts changed.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations
                  WHERE ScriptName = '37_add_subplanet_classification_catalog.sql');
GO
