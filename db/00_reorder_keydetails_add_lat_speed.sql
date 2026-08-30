-- =====================================================================
-- One-time: add EclipticLatitudeDegrees + SpeedLongitudeDegPerDay to
-- tbl_Chart_KeyDetails, and reorder its columns to
--   Planet -> longitude -> latitude -> speed -> retrograde -> sign ->
--   degree -> Nakshatra -> (analytics...).
--
-- SQL Server has no in-place column reorder, so this rebuilds the table
-- (rename -> create -> copy -> drop). tbl_Chart_KeyDetails holds only a
-- few hundred rows (people x chart types x planets), so the copy is
-- instant. The two new columns are seeded NULL; repopulate them with:
--   dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails
--
-- Run against an existing [ikiastrro] database. A brand-new machine
-- building from db/ikiastrro.sql already gets the new shape and skips
-- this file.
--
-- vw_Chart_Consolidated is dropped and recreated with the two new
-- columns added to its SELECT list (body unchanged).
-- =====================================================================
USE [ikiastrro];
GO

-- Table rebuild — skipped if the columns are already present (idempotent). The view
-- recreate further down always runs.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.tbl_Chart_KeyDetails')
             AND name = N'EclipticLatitudeDegrees')
BEGIN
    PRINT 'tbl_Chart_KeyDetails already has EclipticLatitudeDegrees - skipping the rebuild.';
END
ELSE
BEGIN

SET XACT_ABORT ON;
BEGIN TRANSACTION;

EXEC sp_rename 'dbo.tbl_Chart_KeyDetails', 'tbl_Chart_KeyDetails_old';

-- The explicitly-named constraints stay attached to the renamed table; drop them so the new
-- table can reuse the same names. (Unnamed PK / FKs / indexes drop with DROP TABLE _old below;
-- index names are per-table, so those don't collide.)
ALTER TABLE [dbo].[tbl_Chart_KeyDetails_old] DROP CONSTRAINT [DF_ChartKeyDetails_ChartType];
ALTER TABLE [dbo].[tbl_Chart_KeyDetails_old] DROP CONSTRAINT [FK_ChartKeyDetails_Nakshatra];
ALTER TABLE [dbo].[tbl_Chart_KeyDetails_old] DROP CONSTRAINT [FK_ChartKeyDetails_NakshatraPada];

CREATE TABLE [dbo].[tbl_Chart_KeyDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[BirthDetailId] [int] NOT NULL,
	[ChartType] [nvarchar](50) NOT NULL,
	[Planet] [varchar](20) NOT NULL,
	[NirayanaLongitudeDegrees] [float] NOT NULL,
	[EclipticLatitudeDegrees] [float] NULL,
	[SpeedLongitudeDegPerDay] [float] NULL,
	[IsRetrograde] [bit] NULL,
	[Sign] [varchar](20) NOT NULL,
	[DegreesInSignDecimal] [decimal](7, 4) NULL,
	[DegreesInSignDisplay] [varchar](20) NULL,
	[Nakshatra] [varchar](20) NULL,
	[NakshatraId] [tinyint] NULL,
	[NakshatraPada] [tinyint] NULL,
	[NakshatraPadaId] [int] NULL,
	[NakshatraLordPlanet] [varchar](20) NULL,
	[NakshatraSubLordPlanet] [varchar](10) NULL,
	[HouseNumberFromLagna] [tinyint] NOT NULL,
	[HouseNumberFromSun] [tinyint] NOT NULL,
	[HouseNumberFromMoon] [tinyint] NOT NULL,
	[OwnSigns] [varchar](30) NULL,
	[ExaltationSign] [varchar](20) NULL,
	[DebilitationSign] [varchar](20) NULL,
	[MoolatrikonaSign] [varchar](20) NULL,
	[MoolatrikonaRange] [varchar](20) NULL,
	[SignLordPlanet] [varchar](20) NULL,
	[DignityStatus] [varchar](20) NULL,
	[IsCombust] [bit] NULL,
	[DistanceFromSunDegrees] [decimal](7, 4) NULL,
	[CombustionOrbUsedDegrees] [decimal](5, 2) NULL,
	[AspectingPlanets] [varchar](200) NULL,
	[ComputedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_tbl_Chart_KeyDetails] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY];

ALTER TABLE [dbo].[tbl_Chart_KeyDetails] ADD DEFAULT (sysutcdatetime()) FOR [ComputedAt];
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] ADD CONSTRAINT [DF_ChartKeyDetails_ChartType] DEFAULT ('D1') FOR [ChartType];

SET IDENTITY_INSERT [dbo].[tbl_Chart_KeyDetails] ON;

INSERT INTO [dbo].[tbl_Chart_KeyDetails]
    ([Id], [ChartResultId], [BirthDetailId], [ChartType], [Planet],
     [NirayanaLongitudeDegrees], [EclipticLatitudeDegrees], [SpeedLongitudeDegPerDay], [IsRetrograde],
     [Sign], [DegreesInSignDecimal], [DegreesInSignDisplay],
     [Nakshatra], [NakshatraId], [NakshatraPada], [NakshatraPadaId],
     [NakshatraLordPlanet], [NakshatraSubLordPlanet],
     [HouseNumberFromLagna], [HouseNumberFromSun], [HouseNumberFromMoon],
     [OwnSigns], [ExaltationSign], [DebilitationSign], [MoolatrikonaSign], [MoolatrikonaRange],
     [SignLordPlanet], [DignityStatus],
     [IsCombust], [DistanceFromSunDegrees], [CombustionOrbUsedDegrees],
     [AspectingPlanets], [ComputedAt])
SELECT
     [Id], [ChartResultId], [BirthDetailId], [ChartType], [Planet],
     [NirayanaLongitudeDegrees], NULL, NULL, [IsRetrograde],
     [Sign], [DegreesInSignDecimal], [DegreesInSignDisplay],
     [Nakshatra], [NakshatraId], [NakshatraPada], [NakshatraPadaId],
     [NakshatraLordPlanet], [NakshatraSubLordPlanet],
     [HouseNumberFromLagna], [HouseNumberFromSun], [HouseNumberFromMoon],
     [OwnSigns], [ExaltationSign], [DebilitationSign], [MoolatrikonaSign], [MoolatrikonaRange],
     [SignLordPlanet], [DignityStatus],
     [IsCombust], [DistanceFromSunDegrees], [CombustionOrbUsedDegrees],
     [AspectingPlanets], [ComputedAt]
FROM [dbo].[tbl_Chart_KeyDetails_old];

SET IDENTITY_INSERT [dbo].[tbl_Chart_KeyDetails] OFF;

-- indexes (same names/definitions as db/ikiastrro.sql)
CREATE NONCLUSTERED INDEX [IX_Chart_KeyDetails_BirthDetailId_Planet]
    ON [dbo].[tbl_Chart_KeyDetails] ([BirthDetailId] ASC, [Planet] ASC);
CREATE NONCLUSTERED INDEX [IX_Chart_KeyDetails_ChartResultId]
    ON [dbo].[tbl_Chart_KeyDetails] ([ChartResultId] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Chart_KeyDetails_ChartResultId_Planet]
    ON [dbo].[tbl_Chart_KeyDetails] ([ChartResultId] ASC, [Planet] ASC);

-- foreign keys
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] WITH CHECK
    ADD FOREIGN KEY ([BirthDetailId]) REFERENCES [dbo].[tbl_BirthDetails] ([Id]);
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] WITH CHECK
    ADD FOREIGN KEY ([ChartResultId]) REFERENCES [dbo].[tbl_ChartResults] ([Id]);
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] WITH CHECK
    ADD CONSTRAINT [FK_ChartKeyDetails_Nakshatra] FOREIGN KEY ([NakshatraId]) REFERENCES [dbo].[tbl_Nakshatras] ([Id]);
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] WITH CHECK
    ADD CONSTRAINT [FK_ChartKeyDetails_NakshatraPada] FOREIGN KEY ([NakshatraPadaId]) REFERENCES [dbo].[tbl_NakshatraPadas] ([Id]);

DROP TABLE [dbo].[tbl_Chart_KeyDetails_old];

COMMIT TRANSACTION;

END
GO

-- recreate the consolidated view with the two new columns in its SELECT list
IF OBJECT_ID('dbo.vw_Chart_Consolidated', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Chart_Consolidated;
GO
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[vw_Chart_Consolidated] AS
SELECT
    bd.Id                       AS BirthDetailId,
    bd.Name,
    bd.DateOfBirth,
    bd.TimeOfBirth,
    bd.PlaceCity,
    bd.PlaceCountry,
    cr.Id                       AS ChartResultId,
    cr.ChartType,
    cr.Ayanamsha,
    cr.HouseSystem,
    cr.EngineVersion,
    kd.Planet,
    kd.NirayanaLongitudeDegrees,
    kd.EclipticLatitudeDegrees,
    kd.SpeedLongitudeDegPerDay,
    kd.IsRetrograde,
    kd.Sign,
    kd.DegreesInSignDecimal,
    kd.DegreesInSignDisplay,
    kd.Nakshatra,
    kd.NakshatraPada,
    kd.NakshatraLordPlanet,
    kd.IsCombust,
    kd.DistanceFromSunDegrees,
    kd.CombustionOrbUsedDegrees,
    kd.HouseNumberFromLagna,
    kd.HouseNumberFromSun,
    kd.HouseNumberFromMoon,
    kd.OwnSigns,
    kd.ExaltationSign,
    kd.DebilitationSign,
    kd.MoolatrikonaSign,
    kd.MoolatrikonaRange,
    kd.SignLordPlanet,
    kd.DignityStatus,
    RulesHouses.HouseList        AS RulesHouseNumbers,
    Conjunct.PlanetList           AS ConjunctWith,
    AspectsCast.TargetList        AS Aspects,
    kd.AspectingPlanets          AS AspectedBy,
    kd.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = kd.BirthDetailId
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), '','') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanet = kd.Planet
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, '', '') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1 = kd.Planet
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2 = kd.Planet
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, '' ('', AspectType, '')''), '', '') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanet = kd.Planet
) AspectsCast;';
GO

PRINT 'tbl_Chart_KeyDetails rebuilt: +EclipticLatitudeDegrees, +SpeedLongitudeDegPerDay, columns reordered.';
PRINT 'Now run:  dotnet run --project src/Ikiastrro.Cli -- recompute-keydetails';
GO
