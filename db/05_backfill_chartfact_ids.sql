-- =====================================================================
-- 05 — Backfill the *Id columns from the reference tables by NAME JOIN
-- (the unambiguous source of truth). Re-runnable. 'Ascendant' rows have no
-- tbl_Planets match and correctly stay NULL.
-- =====================================================================
USE [ikiastrro];
GO
-- tbl_ChartResults ----------------------------------------------------
UPDATE cr SET cr.RuleSetId = ra.Id
  FROM dbo.tbl_ChartResults cr
  CROSS JOIN (SELECT TOP 1 Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1) ra
  WHERE cr.RuleSetId IS NULL;

UPDATE cr SET cr.CalculationKind =
        CASE WHEN cr.ChartType = 'VimshottariDasha' THEN 'VimshottariDasha' ELSE 'PositionChart' END
  FROM dbo.tbl_ChartResults cr
  WHERE cr.CalculationKind IS NULL;

UPDATE cr SET cr.ChartTypeId = ct.Id
  FROM dbo.tbl_ChartResults cr
  JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = cr.ChartType
  WHERE cr.ChartTypeId IS NULL;
GO
-- tbl_Chart_KeyDetails ----------------------------------------------------
UPDATE kd SET kd.PlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.Planet
  WHERE kd.PlanetId IS NULL;
UPDATE kd SET kd.SignId = sa.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = kd.Sign
  WHERE kd.SignId IS NULL;
UPDATE kd SET kd.NakshatraLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraLordPlanet
  WHERE kd.NakshatraLordPlanetId IS NULL AND kd.NakshatraLordPlanet IS NOT NULL;
UPDATE kd SET kd.NakshatraSubLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.NakshatraSubLordPlanet
  WHERE kd.NakshatraSubLordPlanetId IS NULL AND kd.NakshatraSubLordPlanet IS NOT NULL;
UPDATE kd SET kd.SignLordPlanetId = p.Id
  FROM dbo.tbl_Chart_KeyDetails kd JOIN dbo.tbl_Planets p ON p.PlanetName = kd.SignLordPlanet
  WHERE kd.SignLordPlanetId IS NULL AND kd.SignLordPlanet IS NOT NULL;
GO
-- tbl_Chart_HouseLords ----------------------------------------------------
UPDATE hl SET hl.HouseSignId = sa.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.HouseSign
  WHERE hl.HouseSignId IS NULL;
UPDATE hl SET hl.LordPlanetId = p.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_Planets p ON p.PlanetName = hl.LordPlanet
  WHERE hl.LordPlanetId IS NULL;
UPDATE hl SET hl.LordPlacedInSignId = sa.Id
  FROM dbo.tbl_Chart_HouseLords hl JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = hl.LordPlacedInSign
  WHERE hl.LordPlacedInSignId IS NULL;
GO
-- tbl_Chart_Conjunctions ----------------------------------------------------
UPDATE c SET c.Planet1Id = p.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_Planets p ON p.PlanetName = c.Planet1
  WHERE c.Planet1Id IS NULL;
UPDATE c SET c.Planet2Id = p.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_Planets p ON p.PlanetName = c.Planet2
  WHERE c.Planet2Id IS NULL;
UPDATE c SET c.SignId = sa.Id
  FROM dbo.tbl_Chart_Conjunctions c JOIN dbo.tbl_SignAttributes sa ON sa.ZodiacEnumValue = c.Sign
  WHERE c.SignId IS NULL;
GO
-- tbl_Chart_Aspects ----------------------------------------------------
UPDATE a SET a.AspectingPlanetId = p.Id
  FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_Planets p ON p.PlanetName = a.AspectingPlanet
  WHERE a.AspectingPlanetId IS NULL;
UPDATE a SET a.AspectedTargetType =
        CASE WHEN a.AspectedTarget = 'Ascendant' THEN 'Ascendant' ELSE 'Planet' END
  FROM dbo.tbl_Chart_Aspects a
  WHERE a.AspectedTargetType IS NULL;
UPDATE a SET a.AspectedPlanetId = p.Id
  FROM dbo.tbl_Chart_Aspects a JOIN dbo.tbl_Planets p ON p.PlanetName = a.AspectedTarget
  WHERE a.AspectedPlanetId IS NULL AND a.AspectedTarget <> 'Ascendant';
GO
-- tbl_Chart_DashaPeriods ----------------------------------------------------
UPDATE dp SET dp.LordId = p.Id
  FROM dbo.tbl_Chart_DashaPeriods dp JOIN dbo.tbl_Planets p ON p.PlanetName = dp.Lord
  WHERE dp.LordId IS NULL;
GO
-- tbl_Fact_PlanetAvastha ----------------------------------------------------
UPDATE av SET av.PlanetId = p.Id
  FROM dbo.tbl_Fact_PlanetAvastha av JOIN dbo.tbl_Planets p ON p.PlanetName = av.Planet
  WHERE av.PlanetId IS NULL;
UPDATE av SET av.ChartTypeId = ct.Id
  FROM dbo.tbl_Fact_PlanetAvastha av JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = av.ChartType
  WHERE av.ChartTypeId IS NULL;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '05_backfill_chartfact_ids.sql', 'backfilled *Id columns by name join'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '05_backfill_chartfact_ids.sql');
GO
PRINT '05 applied: id columns backfilled.';
GO
